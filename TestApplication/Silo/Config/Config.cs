using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silo.Config
{

    public class Rootobject
    {
        public Settings SiloConfig { get; set; }
    }

    public class Settings
    {
        public static string SettingsName = "Settings";
        public string Test { get; set; }

        public string SimpleTableStorageConn { get; set; }
        public StorageNameAndConnection[] TableStorageAccounts { get; set; }
        public StorageNameAndConnection[] BlobStorageAccounts { get; set; }
    }


    public class StorageNameAndConnection
    {
        public string Name { get; set; }
        public string SasToken { get; set; }
    }

}
