// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using OrleansShardedStorageProvider;
using OrleansShardedStorageProvider.Models;
using OrleansShardedStorageProvider.Providers;
using OrleansShardedStorageProvider.Storage;
using System.Diagnostics;
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



var serviceId = "OrleansBasics";
var grainName = "smalldata";
var grainId = "1F";


ClusterOptions cSettings = new ClusterOptions();
cSettings.ServiceId = serviceId;
IOptions<ClusterOptions> cOpts = Options.Create(cSettings);


AzureShardedGrainStorageBase agb = new AzureShardedGrainStorageBase(cOpts, options);

var key = $"{serviceId}{agb.GetKeyStringSeparator()}{grainName}_{grainId}";
var storageNumberFromKey = agb.GetShardNumberFromKey(key);
Console.WriteLine($"storage location from key = {storageNumberFromKey}");

















StorageDataFinder finder = new StorageDataFinder();
await finder.Init(options);


var input = "";

do
{
    Console.WriteLine("Enter the all (or part of) the guid you wish to find the data location of");
    input = Console.ReadLine();
}
while (String.IsNullOrWhiteSpace(input));

Stopwatch st = new Stopwatch();
st.Start();

// NOTE: Use 'true' to just get one result back (fastest)
//       Use 'false' to potentially get multiple results back
var locationsConcurrentBag = await finder.GetStorageAccountFromBlobKeyPart(input, true);

st.Stop();

if (locationsConcurrentBag.Any())
{
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine($"FOUND: Storage locations in {st.ElapsedMilliseconds}ms");
    foreach(var res in locationsConcurrentBag)
    {
        Console.WriteLine($"Location: {res}");
    }

    Console.WriteLine();
    Console.WriteLine();
}
else
{
    Console.WriteLine("Couldn't find the data.");
}

Console.ReadLine();




