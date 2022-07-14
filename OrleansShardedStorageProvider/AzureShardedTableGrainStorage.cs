using Azure;
using Azure.Data.Tables;
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
    public class AzureShardedTableGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly string _serviceId;
        private readonly string _name;
        private readonly ILogger _logger;
        private readonly AzureShardedTableStorageOptions _options;
        internal List<TableServiceClient> _shareClients = new List<TableServiceClient>();
        private List<TableClient> _tableClients = new List<TableClient>();


        public AzureShardedTableGrainStorage(string name, AzureShardedTableStorageOptions options, IOptions<ClusterOptions> clusterOptions, ILoggerFactory loggerFactory)
        {
            this._name = name;
            var loggerName = $"{typeof(AzureShardedTableGrainStorage).FullName}.{name}";
            this._logger = loggerFactory.CreateLogger(loggerName);
            this._options = options;
            this._serviceId = clusterOptions.Value.ServiceId;
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            lifecycle.Subscribe(OptionFormattingUtilities.Name<AzureShardedTableGrainStorage>(this._name), this._options.InitStage, this.Init, this.Close);
        }


        public async Task Init(CancellationToken ct)
        {
            var stopWatch = Stopwatch.StartNew();

            try
            {
                var initMsg = string.Format("Init: Name={0} ServiceId={1}", this._name, this._serviceId);

                this._logger.LogInformation($"Azure File Storage Grain Storage {this._name} is initializing: {initMsg}");


                foreach(var storage in this._options.ConnectionStrings)
                {
                    var shareClient = new TableServiceClient(
                        storage.BaseTableUri,
                        new AzureSasCredential(storage.SasToken));

                    var table = await shareClient.CreateTableIfNotExistsAsync(storage.TableName);

                    var tableClient = new TableClient(
                                storage.TableStorageUri,
                                new AzureSasCredential(storage.SasToken));

                    this._tableClients.Add(tableClient);
                }

                stopWatch.Stop();
                this._logger.LogInformation($"Initializing provider {this._name} of type {this.GetType().Name} in stage {this._options.InitStage} took {stopWatch.ElapsedMilliseconds} Milliseconds.");
            }
            catch (Exception exc)
            {
                stopWatch.Stop();
                this._logger.LogError($"Initialization failed for provider {this._name} of type {this.GetType().Name} in stage {this._options.InitStage} in {stopWatch.ElapsedMilliseconds} Milliseconds.", exc);
                throw;
            }
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (this._tableClients == null || !this._tableClients.Any()) throw new ArgumentException("GrainState collection not initialized.");

            try
            {
                var pk = GetKeyString(grainReference);
                var connectionIndex = GetShardNumberFromKey(grainReference);
                var rowKey = SanitizeTableProperty(grainType);


                // NOTE: This will error if the row doesn't exist - it's disputed functionality from the Azure team
                //       In orleans, they just swallow the error, so we're doing the same
                //       See discussion here - https://github.com/Azure/azure-sdk-for-net/issues/16251
                //       and Orleans Code here - {orleans}\src\Azure\Shared\Storage\AzureTableDataManager.cs Method ReadSingleTableEntryAsync
                //       This is quicker than Query once a row is there, so what we lose to start, we more than gain in speed later. Don't change it!
                var res = await _tableClients[connectionIndex].GetEntityAsync<TableEntity>(pk, rowKey);

                if (res != null)
                {
                    var stringData = res.Value["StringData"].ToString();

                    if(!String.IsNullOrWhiteSpace(stringData))
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
            catch (Exception exc)
            {
                // See comments above for GetEntityAsync error details
                if (!exc.ToString().Contains("The specified resource does not exist"))
                {
                    this._logger.LogError(exc, $"Failure reading state for Grain Type {grainType} with Id {grainReference}.");
                    throw;//?
                }
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (this._tableClients == null || !this._tableClients.Any()) throw new ArgumentException("GrainState collection not initialized.");

            try
            {
                
                string pk = GetKeyString(grainReference);
                var connectionIndex = GetShardNumberFromKey( grainReference);
                var rowKey = SanitizeTableProperty(grainType);
                var entity = new TableEntity(pk, rowKey)
                {
                    ETag = new ETag(grainState.ETag),
                };

                //entity["ETag"] = grainState.ETag;
                //entity["TimestampUtc"] = DateTime.UtcNow;

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
                var eTag = opResult.Headers.ETag.GetValueOrDefault().ToString();
                grainState.ETag = eTag;
                grainState.RecordExists = true;
            }
            catch (Exception exc)
            {
                this._logger.LogError(exc, $"Failure writing state for Grain Type {grainType} with Id {grainReference}.");
                throw;
            }
        }


        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            if (this._tableClients == null || !this._tableClients.Any()) throw new ArgumentException("GrainState collection not initialized.");

            try
            {
                var pk = GetKeyString(grainReference);
                var connectionIndex = GetShardNumberFromKey(grainReference);
                var rowKey = SanitizeTableProperty(grainType);
                var res = await _tableClients[connectionIndex].GetEntityAsync<TableEntity>(pk, rowKey);
                if(res != null)
                {
                    // NOTE: May wish to update entity with empty data?
                    await _tableClients[connectionIndex].DeleteEntityAsync(pk, rowKey);
                }
            }
            catch (Exception exc)
            {
                this._logger.LogError(exc, $"Failure clearing state for Grain Type {grainType} with Id {grainReference}.");
                throw;
            }
        }

        public Task Close(CancellationToken ct)
        {
            this._shareClients = null;
            this._tableClients = null;
            return Task.CompletedTask;
        }


        #region "Utils"

        private int GetShardNumberFromKey(GrainReference grainReference)
        {
            var hash = grainReference.GetHashCode();
            var storageNum = Math.Abs(hash % this._options.ConnectionStrings.Count());
            return storageNum;
        }


        private const string KeyStringSeparator = "_";

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

    public static class AzureFileGrainStorageFactory
    {
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            var options = services.GetRequiredService<IOptionsMonitor<AzureShardedTableStorageOptions>>().Get(name);
            return ActivatorUtilities.CreateInstance<AzureShardedTableGrainStorage>(services, options, name);
        }
    }
}
