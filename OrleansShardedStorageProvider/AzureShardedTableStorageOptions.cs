using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansShardedStorageProvider
{

    public class AzureShardedTableStorageOptions
    {
        //[Redact] -- stops any logging of this info
        public List<AzureTableStorageConnection> ConnectionStrings { get; set; }

        public int InitStage { get; set; } = DEFAULT_INIT_STAGE;

        public const int DEFAULT_INIT_STAGE = ServiceLifecycleStage.ApplicationServices;
    }
}
