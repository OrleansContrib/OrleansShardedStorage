using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
using System.Diagnostics;
using System.Text;

namespace OrleansShardedStorageProvider
{
    public class AzureShardedGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly string _serviceId;
        private readonly string _name;
        private readonly ILogger _logger;
        private readonly AzureShardedStorageOptions _options;
        private List<TableClient> _tableClients = new List<TableClient>();
        private List<BlobContainerClient> _blobClients = new List<BlobContainerClient>();
        private StorageType _storageType = StorageType.TableStorage;


        public AzureShardedGrainStorage(string name, AzureShardedStorageOptions options, IOptions<ClusterOptions> clusterOptions, ILoggerFactory loggerFactory)
        {
            this._name = name;
            var loggerName = $"{typeof(AzureShardedGrainStorage).FullName}.{name}";
            this._logger = loggerFactory.CreateLogger(loggerName);
            this._options = options;
            this._serviceId = clusterOptions.Value.ServiceId;
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe(OptionFormattingUtilities.Name<AzureShardedGrainStorage>(this._name), this._options.InitStage, this.Init, this.Close);
        }


        public async Task Init(CancellationToken ct)
        {
            var stopWatch = Stopwatch.StartNew();

            // This is required to give more detailed logging if it errors.
            int exConfigIdx = -1;

            try
            {
                var initMsg = string.Format("Init: Name={0} ServiceId={1}", this._name, this._serviceId);

                this._logger.LogInformation($"Azure File Storage Grain Storage {this._name} is initializing: {initMsg}");

                foreach (var storage in this._options.ConnectionStrings)
                {
                    exConfigIdx++;

                    if (storage.StorageType == StorageType.TableStorage)
                    {
                        _storageType = StorageType.TableStorage;

                        var shareClient = new TableServiceClient(
                        storage.BaseTableUri,
                        new AzureSasCredential(storage.SasToken));

                        var table = await shareClient.CreateTableIfNotExistsAsync(storage.TableOrContainerName);


                        var tableClient = new TableClient(
                                    storage.TableStorageUri,
                                    new AzureSasCredential(storage.SasToken));

                        this._tableClients.Add(tableClient);
                    }
                    else if (storage.StorageType == StorageType.BlobStorage)
                    {
                        _storageType = StorageType.BlobStorage;

                        BlobServiceClient blobServiceClient = new BlobServiceClient(storage.BaseBlobUri, storage.SasCredential);

                        var containerClient = blobServiceClient.GetBlobContainerClient(storage.TableOrContainerName);
                        await containerClient.CreateIfNotExistsAsync();

                        this._blobClients.Add(containerClient);
                    }
                    else
                    {
                        throw new NotImplementedException("type not implmeneted");
                    }
                }

                stopWatch.Stop();
                this._logger.LogInformation($"Initializing provider {this._name} of type {this.GetType().Name} in stage {this._options.InitStage} took {stopWatch.ElapsedMilliseconds} Milliseconds.");
            }
            catch (Exception exc)
            {
                stopWatch.Stop();

                string whereString = "where unknown placeholder";
                if (exConfigIdx >= 0)
                {
                    var excon = this._options.ConnectionStrings[exConfigIdx];
                    var exStorageAcct = excon.AccountName;
                    var exStorageType = excon.StorageType.ToString();
                    whereString = $"CN:{exConfigIdx},Name:{exStorageAcct},Type:{exStorageType}. ";
                }

                this._logger.LogError($"{whereString}. Initialization failed for provider {this._name} of type {this.GetType().Name} in stage {this._options.InitStage} in {stopWatch.ElapsedMilliseconds} Milliseconds.", exc);
                throw;
            }
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (this._storageType == StorageType.TableStorage &&
                (this._tableClients == null || !this._tableClients.Any())) throw new ArgumentException("GrainState collection not initialized.");

            // This is required to give more detailed logging if it errors.
            int exConfigIdx = -1;

            try
            {
                var pk = GetKeyString(grainReference);
                var connectionIndex = GetShardNumberFromKey(grainReference);
                var rowKey = SanitizeTableProperty(grainType);
                exConfigIdx = connectionIndex;

                if (this._storageType == StorageType.TableStorage)
                {
                    // NOTE: This will error if the row doesn't exist - it's disputed functionality from the Azure team
                    //       In orleans, they just swallow the error, so we're doing the same
                    //       See discussion here - https://github.com/Azure/azure-sdk-for-net/issues/16251
                    //       and Orleans Code here - {orleans}\src\Azure\Shared\Storage\AzureTableDataManager.cs Method ReadSingleTableEntryAsync
                    //       This is quicker than Query once a row is there, so what we lose to start, we more than gain in speed later. Don't change it!
                    var res = await _tableClients[connectionIndex].GetEntityAsync<TableEntity>(pk, rowKey);

                    if (res != null)
                    {
                        var stringData = res.Value["StringData"].ToString();

                        if (!String.IsNullOrWhiteSpace(stringData))
                        {
                            using (JsonTextReader jsonReader =
                            new JsonTextReader(new StringReader(stringData)))
                            {
                                JsonSerializer ser = new JsonSerializer();
                                grainState.State = ser.Deserialize(jsonReader, grainState.State.GetType());
                            }
                        }

                        grainState.RecordExists = grainState.State != null;
                        grainState.ETag = res.Value.ETag.ToString();
                    }

                    if (grainState.State == null)
                    {
                        grainState.State = Activator.CreateInstance(grainState.State.GetType());
                        grainState.RecordExists = true;
                    }
                }
                else if (this._storageType == StorageType.BlobStorage)
                {
                    var key = pk + "_" + rowKey;
                    var containerClient = _blobClients[connectionIndex];
                    BlobClient blobClient = containerClient.GetBlobClient(key);

                    var exists = await blobClient.ExistsAsync();

                    if (exists)
                    {
                        var download = await blobClient.DownloadContentAsync();
                        BinaryData binData = download.Value.Content;

                        //var bytes = binData.ToArray();
                        var stringData = Encoding.UTF8.GetString(binData);

                        if (!String.IsNullOrWhiteSpace(stringData))
                        {
                            using (JsonTextReader jsonReader =
                            new JsonTextReader(new StringReader(stringData)))
                            {
                                JsonSerializer ser = new JsonSerializer();
                                grainState.State = ser.Deserialize(jsonReader, grainState.State.GetType());
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
                    string grainRef = $"Failure reading state for Grain Type {grainType} with Id {grainReference}.";
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

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (this._storageType == StorageType.TableStorage && (this._tableClients == null || !this._tableClients.Any())) throw new ArgumentException("GrainState collection not initialized.");
            if (this._storageType == StorageType.BlobStorage && (this._blobClients == null || !this._blobClients.Any())) throw new ArgumentException("GrainState collection not initialized.");

            // This is required to give more detailed logging if it errors.
            int exConfigIdx = -1;

            try
            {
                string pk = GetKeyString(grainReference);
                var connectionIndex = GetShardNumberFromKey(grainReference);
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
                string grainRef = $"Failure WRITING state for Grain Type {grainType} with Id {grainReference}.";
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

                this._logger.LogError(overall, $"Failure writing state for Grain Type {grainType} with Id {grainReference}.");
                throw; // Definitely throw this error.
            }
        }


        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (this._tableClients == null || !this._tableClients.Any()) throw new ArgumentException("GrainState collection not initialized.");
            int exConfigIdx = -1;

            try
            {
                var pk = GetKeyString(grainReference);
                var connectionIndex = GetShardNumberFromKey(grainReference);
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
                string grainRef = $"Failure CLEARING state for Grain Type {grainType} with Id {grainReference}.";
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

                this._logger.LogError(overall, $"Failure clearing state for Grain Type {grainType} with Id {grainReference}.");
                throw;
            }
        }

        public Task Close(CancellationToken ct)
        {
            if (this._storageType == StorageType.TableStorage)
            {
                this._tableClients = null;
            }
            else if (this._storageType == StorageType.BlobStorage)
            {

            }
            else
            {
                throw new NotImplementedException("type not implemented for read");
            }


            return Task.CompletedTask;
        }





        #region "Utils"

        private int GetShardNumberFromKey(GrainReference grainReference)
        {
            var hash = grainReference.GetHashCode();
            var storageNum = Math.Abs(hash % this._options.ConnectionStrings.Count());

            return storageNum;
        }


        private const string KeyStringSeparator = "__";

        private string GetKeyString(GrainReference grainReference)
        {
            var key = $"{this._serviceId}{KeyStringSeparator}{grainReference.ToKeyString()}";

            return SanitizeTableProperty(key);
        }

        public string SanitizeTableProperty(string key)
        {
            // Remove any characters that can't be used in Azure PartitionKey or RowKey values
            // http://www.jamestharpe.com/web-development/azure-table-service-character-combinations-disallowed-in-partitionkey-rowkey/
            key = key
                .Replace('/', '_')        // Forward slash
                .Replace('\\', '_')       // Backslash
                .Replace('#', '_')        // Pound sign
                .Replace('?', '_');       // Question mark

            if (key.Length >= 1024)
                throw new ArgumentException(string.Format("Key length {0} is too long to be an Azure table key. Key={1}", key.Length, key));

            return key;
        }


        #endregion
    }

    public static class AzureShardedGrainStorageFactory
    {
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            var options = services.GetRequiredService<IOptionsMonitor<AzureShardedStorageOptions>>().Get(name);
            return ActivatorUtilities.CreateInstance<AzureShardedGrainStorage>(services, options, name);
        }
    }
}
