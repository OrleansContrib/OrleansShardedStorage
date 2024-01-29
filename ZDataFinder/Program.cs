// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using OrleansShardedStorageProvider;
using ZDataFinder;
using ZDataFinder.Config;

var config = new ConfigurationBuilder()
       .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
       .AddJsonFile("appsettings.json")
       .AddUserSecrets<Program>() //secrets override appsettings.json
       .Build();


Console.WriteLine("This application will find data across blob storage (and later table storage accounts)");


var settings = config.GetSection(Settings.SettingsName).Get<Settings>();

List<AzureShardedStorageConnection> tableGrainStores = new List<AzureShardedStorageConnection>();
if (settings?.TableStorageAccounts != null && settings.TableStorageAccounts.Any())
{
    foreach (var row in settings.TableStorageAccounts)
    {
        tableGrainStores.Add(new AzureShardedStorageConnection(row.Name, row.SasToken, StorageType.TableStorage));
    }
}

List<AzureShardedStorageConnection> blobGrainStores = new List<AzureShardedStorageConnection>();
if (settings?.BlobStorageAccounts != null && settings.BlobStorageAccounts.Any())
{
    foreach (var row in settings.BlobStorageAccounts)
    {
        blobGrainStores.Add(new AzureShardedStorageConnection(row.Name, row.SasToken, StorageType.BlobStorage));
    }
}



var options = new AzureShardedStorageOptions();
options.ConnectionStrings = tableGrainStores;
options.ConnectionStrings.AddRange(blobGrainStores);



BlobManager bmgr = new BlobManager();
await bmgr.Init(options);



var input = "";

do
{
    Console.WriteLine("Enter the guid you wish to find the data location of");
    input = Console.ReadLine();
}
while (String.IsNullOrWhiteSpace(input));


var location = await bmgr.GetStorageAccountFromBlobKeyPart(input);

if (!String.IsNullOrWhiteSpace(location))
{
    Console.WriteLine($"Storage location name = {location}");
}
else
{
    Console.WriteLine("Couldn't find the data.");
}

Console.ReadLine();




