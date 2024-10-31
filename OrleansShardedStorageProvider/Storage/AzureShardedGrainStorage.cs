using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
using OrleansShardedStorageProvider.Models;
using OrleansShardedStorageProvider.Providers;
using System.Diagnostics;
using System.Text;

namespace OrleansShardedStorageProvider.Storage
{

	public class AzureShardedGrainStorage : AzureShardedGrainStorageBase, IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
	{
		// ServiceId and Options are stored in the base class.
		private readonly string _name;
		private readonly ILogger _logger;
		private List<TableClient> _tableClients = new List<TableClient>();
		private List<BlobContainerClient> _blobClients = new List<BlobContainerClient>();
		private StorageType _storageType = StorageType.TableStorage;
		private int _defaultTimeoutTime = 20000; // 20s - Less than Orleans Grain 30s DEFAULT_ACTIVATION_TIMEOUT

		/// <summary>
		/// Creates a new instance of the <see cref="AzureShardedStorage"/> type.
		/// </summary>
		public AzureShardedGrainStorage(
			string name,
			AzureShardedStorageOptions options,
			IGrainStorageSerializer grainStorageSerializer,
			IOptions<ClusterOptions> clusterOptions,
			ILogger<AzureShardedGrainStorage> logger) :
			base(clusterOptions, options)
		{
			_name = name;
			_logger = logger;
			
		}

		/// <inheritdoc />
		public void Participate(ISiloLifecycle lifecycle)
		{
			var name = OptionFormattingUtilities.Name<AzureShardedGrainStorage>(_name);
			lifecycle.Subscribe(name, _options.InitStage, Init, Close);
		}

		private async Task Init(CancellationToken cancellationToken)
		{
			var timer = Stopwatch.StartNew();

			// This is required to give more detailed logging if it errors.
			int exConfigIdx = -1;

			try
			{
				if (_logger.IsEnabled(LogLevel.Debug))
				{
					_logger.LogDebug(
						"ShardedGrainStorage {Name} is initializing: ServiceId={ServiceId}",
						 _name,
						 _serviceId);
				}

				// Own content


				var initMsg = string.Format("Init: Name={0} ServiceId={1}", this._name, this._serviceId);

				this._logger.LogInformation($"Azure File Storage Grain Storage {this._name} is initializing: {initMsg}");

				foreach (var storage in this._options.ConnectionStrings)
				{
					exConfigIdx++;

					if (storage.StorageType == StorageType.TableStorage)
					{
						_storageType = StorageType.TableStorage;

						var shareClient = String.IsNullOrEmpty(storage.SasToken) ?
							new TableServiceClient(storage.BaseTableUri, new DefaultAzureCredential()) :
							new TableServiceClient(storage.BaseTableUri, new AzureSasCredential(storage.SasToken));

						var table = await shareClient.CreateTableIfNotExistsAsync(storage.TableOrContainerName);


						var tableClient = new TableClient(
									storage.TableStorageUri,
									new AzureSasCredential(storage.SasToken));

						this._tableClients.Add(tableClient);
					}
					else if (storage.StorageType == StorageType.BlobStorage)
					{
						_storageType = StorageType.BlobStorage;

						BlobServiceClient blobServiceClient = (null == storage.SasCredential) ?
							new BlobServiceClient(storage.BaseBlobUri, new DefaultAzureCredential()) :
							new BlobServiceClient(storage.BaseBlobUri, storage.SasCredential);

						var containerClient = blobServiceClient.GetBlobContainerClient(storage.TableOrContainerName);
						await containerClient.CreateIfNotExistsAsync();

						this._blobClients.Add(containerClient);
					}
					else
					{
						throw new NotImplementedException("type not implmeneted");
					}
				}


				//end own content

				if (_logger.IsEnabled(LogLevel.Debug))
				{
					timer.Stop();
					_logger.LogDebug(
						"Init: Name={Name} ServiceId={ServiceId}, initialized in {ElapsedMilliseconds} ms",
						_name,
						_serviceId,
						timer.Elapsed.TotalMilliseconds.ToString("0.00"));
				}
			}
			catch (Exception ex)
			{
				timer.Stop();

				string whereString = "where unknown placeholder";
				if (exConfigIdx >= 0)
				{
					var excon = this._options.ConnectionStrings[exConfigIdx];
					var exStorageAcct = excon.AccountName;
					var exStorageType = excon.StorageType.ToString();
					whereString = $"CN:{exConfigIdx},Name:{exStorageAcct},Type:{exStorageType}. ";
				}

				this._logger.LogError($"{whereString}. Initialization failed for provider {this._name} of type {this.GetType().Name} in stage {this._options.InitStage} in {timer.Elapsed.TotalMilliseconds.ToString()} Milliseconds.", ex);


				throw new AzureShardedStorageException($"{ex.GetType()}: {ex.Message}");
			}
		}

		/// <inheritdoc />
		public async Task ReadStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
		{
			if (this._storageType == StorageType.TableStorage &&
				(this._tableClients == null || !this._tableClients.Any())) throw new ArgumentException("GrainState collection not initialized.");

			// This is required to give more detailed logging if it errors.
			int exConfigIdx = -1;

			try
			{
				// NOTE: grainId does not always match the number expected for int keys, but they are consistent
				var pk = GetKeyString(grainId);
				var connectionIndex = GetShardNumberFromKey(pk);
				var rowKey = SanitizeTableProperty(grainType);
				exConfigIdx = connectionIndex;

				if (this._storageType == StorageType.TableStorage)
				{
					// NOTE: This will error if the row doesn't exist - it's disputed functionality from the Azure team
					//       In orleans, they just swallow the error, so we're doing the same
					//       See discussion here - https://github.com/Azure/azure-sdk-for-net/issues/16251
					//       and Orleans Code here - {orleans}\src\Azure\Shared\Storage\AzureTableDataManager.cs Method ReadSingleTableEntryAsync
					//       This is quicker than Query once a row is there, so what we lose to start, we more than gain in speed later. Don't change it!

					var cts = new CancellationTokenSource(_defaultTimeoutTime);
					var res = await _tableClients[connectionIndex].GetEntityAsync<TableEntity>(pk, rowKey, null, cts.Token);
					if (res != null)
					{
						var stringData = res.Value["StringData"].ToString();

						if (!String.IsNullOrWhiteSpace(stringData))
						{
							using (JsonTextReader jsonReader =
							new JsonTextReader(new StringReader(stringData)))
							{
								JsonSerializer ser = new JsonSerializer();
								grainState.State = ser.Deserialize<T>(jsonReader);
							}
						}

						grainState.RecordExists = grainState.State != null;
						grainState.ETag = res.Value.ETag.ToString();
					}

					if (grainState.State == null)
					{
						grainState.State = Activator.CreateInstance<T>();
						grainState.RecordExists = true;
					}
				}
				else if (this._storageType == StorageType.BlobStorage)
				{
					var key = pk + "_" + rowKey;
					var containerClient = _blobClients[connectionIndex];
					BlobClient blobClient = containerClient.GetBlobClient(key);

					var cts = new CancellationTokenSource(_defaultTimeoutTime);
					var exists = await blobClient.ExistsAsync(cts.Token);

					if (exists)
					{
						var cts2 = new CancellationTokenSource(_defaultTimeoutTime);
						var download = await blobClient.DownloadContentAsync(cts2.Token);
						BinaryData binData = download.Value.Content;

						//var bytes = binData.ToArray();
						var stringData = Encoding.UTF8.GetString(binData);

						if (!String.IsNullOrWhiteSpace(stringData))
						{
							using (JsonTextReader jsonReader =
							new JsonTextReader(new StringReader(stringData)))
							{
								JsonSerializer ser = new JsonSerializer();
								grainState.State = ser.Deserialize<T>(jsonReader);
							}
						}

						grainState.RecordExists = grainState.State != null;
						// Note: ETag is important for optimistic concurrency
						grainState.ETag = download.Value.Details.ETag.ToString();
					}
				}
				else
				{
					throw new NotImplementedException("type not implemented for read");
				}
			}
			catch (Exception exc)
			{
				var errorString = exc.ToString();

				// See comments above for GetEntityAsync error details
				if (errorString.Contains("The specified resource does not exist") ||
					errorString.Contains("The specified blob does not exist"))
				{
					// We expect this error. There's nothing we can do about it. See comments above.
				}
				else
				{
					string grainRef = $"Failure reading state for Grain Type {grainType} with Id {grainId}.";
					string whereMsg = "unknown location placeholder." + grainRef;
					if (exConfigIdx >= 0)
					{
						var conx = this._options.ConnectionStrings[exConfigIdx];
						var exAcctName = conx.AccountName;
						var exAcctType = conx.StorageType.ToString();
						var exTblCtrName = conx.TableOrContainerName;

						whereMsg = $"Idx:{exConfigIdx},Acct:{exAcctName},Type:{exAcctType},TblCtr:{exTblCtrName}. {grainRef}. ";
					}

					var overall = whereMsg + exc.ToString();

					this._logger.LogError(overall, grainRef);
					throw;
				}
			}
		}

		/// <inheritdoc />
		public async Task WriteStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
		{
			if (this._storageType == StorageType.TableStorage && (this._tableClients == null || !this._tableClients.Any())) throw new ArgumentException("GrainState collection not initialized.");
			if (this._storageType == StorageType.BlobStorage && (this._blobClients == null || !this._blobClients.Any())) throw new ArgumentException("GrainState collection not initialized.");

			// This is required to give more detailed logging if it errors.
			int exConfigIdx = -1;

			try
			{
				// NOTE: grainId does not always match the number expected for int keys, but they are consistent
				var pk = GetKeyString(grainId);
				var connectionIndex = GetShardNumberFromKey(pk);
				var rowKey = SanitizeTableProperty(grainType);
				exConfigIdx = connectionIndex;

				if (this._storageType == StorageType.TableStorage)
				{
					var entity = new TableEntity(pk, rowKey)
					{
						ETag = new ETag(grainState.ETag),
					};

					JsonSerializer ser = new JsonSerializer();
					StringBuilder sb = new StringBuilder();
					using (StringWriter sw = new StringWriter(sb))
					using (JsonWriter writer = new JsonTextWriter(sw))
					{
						ser.Serialize(writer, grainState.State);
					}

					entity["StringData"] = sb.ToString();

					// TODO: LEARN - Should we check ETag and decide whether to update based on it???
					var opResult = await this._tableClients[connectionIndex].UpsertEntityAsync(entity);
					// Note: ETag is important for optimistic concurrency
					grainState.ETag = opResult.Headers.ETag.GetValueOrDefault().ToString();
					grainState.RecordExists = true;
				}
				else if (this._storageType == StorageType.BlobStorage)
				{
					JsonSerializer ser = new JsonSerializer();
					StringBuilder sb = new StringBuilder();
					using (StringWriter sw = new StringWriter(sb))
					using (JsonWriter writer = new JsonTextWriter(sw))
					{
						ser.Serialize(writer, grainState.State);
					}

					var rawContent = sb.ToString();
					var bytes = Encoding.UTF8.GetBytes(rawContent);
					BinaryData binaryData = new BinaryData(bytes);

					var key = pk + "_" + rowKey;
					var containerClient = _blobClients[connectionIndex];
					BlobClient blobClient = containerClient.GetBlobClient(key);
					var upload = await blobClient.UploadAsync(binaryData, overwrite: true);
					// Note: ETag is important for optimistic concurrency
					grainState.ETag = upload.Value.ETag.ToString();
					grainState.RecordExists = true;
				}
				else
				{
					throw new NotImplementedException("type not implemented for read");
				}
			}
			catch (Exception exc)
			{
				string grainRef = $"Failure WRITING state for Grain Type {grainType} with Id {grainId}.";
				string whereMsg = "unknown location placeholder." + grainRef;
				if (exConfigIdx >= 0)
				{
					var conx = this._options.ConnectionStrings[exConfigIdx];
					var exAcctName = conx.AccountName;
					var exAcctType = conx.StorageType.ToString();
					var exTblCtrName = conx.TableOrContainerName;

					whereMsg = $"Idx:{exConfigIdx},Acct:{exAcctName},Type:{exAcctType},TblCtr:{exTblCtrName}. {grainRef}. ";
				}

				var overall = whereMsg + exc.ToString();

				this._logger.LogError(overall, $"Failure writing state for Grain Type {grainType} with Id {grainId}.");
				throw; // Definitely throw this error.
			}
		}



		/// <inheritdoc />
		public async Task ClearStateAsync<T>(string grainType, GrainId grainId, IGrainState<T> grainState)
		{
			if (this._tableClients == null || !this._tableClients.Any()) throw new ArgumentException("GrainState collection not initialized.");
			int exConfigIdx = -1;

			try
			{
				var pk = GetKeyString(grainId);
				var connectionIndex = GetShardNumberFromKey(pk);
				var rowKey = SanitizeTableProperty(grainType);
				exConfigIdx = connectionIndex;

				if (this._storageType == StorageType.TableStorage)
				{
					var res = await _tableClients[connectionIndex].GetEntityAsync<TableEntity>(pk, rowKey);
					if (res != null)
					{
						// NOTE: May wish to update entity with empty data?
						await _tableClients[connectionIndex].DeleteEntityAsync(pk, rowKey);
					}
				}
				else if (this._storageType == StorageType.BlobStorage)
				{
					var key = pk + rowKey;
					var containerClient = _blobClients[connectionIndex];
					BlobClient blobClient = containerClient.GetBlobClient(key);
					await blobClient.DeleteIfExistsAsync();
				}
				else
				{
					throw new NotImplementedException("type not implemented for read");
				}


			}
			catch (Exception exc)
			{
				string grainRef = $"Failure CLEARING state for Grain Type {grainType} with Id {grainId}.";
				string whereMsg = "unknown location placeholder." + grainRef;
				if (exConfigIdx >= 0)
				{
					var conx = this._options.ConnectionStrings[exConfigIdx];
					var exAcctName = conx.AccountName;
					var exAcctType = conx.StorageType.ToString();
					var exTblCtrName = conx.TableOrContainerName;

					whereMsg = $"Idx:{exConfigIdx},Acct:{exAcctName},Type:{exAcctType},TblCtr:{exTblCtrName}. {grainRef}. ";
				}

				var overall = whereMsg + exc.ToString();

				this._logger.LogError(overall, $"Failure clearing state for Grain Type {grainType} with Id {grainId}.");
				throw;
			}
		}

		private async Task Close(CancellationToken cancellationToken)
		{
			if (this._storageType == StorageType.TableStorage)
			{
				this._tableClients = null;
			}
			else if (this._storageType == StorageType.BlobStorage)
			{
				this._blobClients = null;
			}
			else
			{
				throw new NotImplementedException("type not implemented for read");
			}
		}
	}


}
