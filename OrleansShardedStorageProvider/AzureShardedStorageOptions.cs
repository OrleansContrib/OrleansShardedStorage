using Orleans;
using Orleans.Storage;

namespace OrleansShardedStorageProvider
{

    public class AzureShardedStorageOptions : IStorageProviderSerializerOptions
    {
        //[Redact] -- stops any logging of this info
        public List<AzureShardedStorageConnection> ConnectionStrings { get; set; }

        public int InitStage { get; set; } = DEFAULT_INIT_STAGE;
       
        public const int DEFAULT_INIT_STAGE = ServiceLifecycleStage.ApplicationServices;

        public IGrainStorageSerializer GrainStorageSerializer { get; set; }
    }
}
