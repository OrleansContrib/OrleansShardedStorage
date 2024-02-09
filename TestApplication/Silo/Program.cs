using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;

using Orleans.Runtime;

using OrleansShardedStorageProvider.Models;
using OrleansShardedStorageProvider.Providers;
using OrleansShardedStorageProvider.Hosting;
using Silo.Config;

try
{
    var host = await StartSiloAsync();
    Console.WriteLine("\n\n Press Enter to terminate...\n\n");
    Console.ReadLine();

    await host.StopAsync();

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return 1;
}

static async Task<IHost> StartSiloAsync()
{
    var config = new ConfigurationBuilder()
           .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
           .AddJsonFile("appsettings.json")
           .AddUserSecrets<Program>() //secrets override appsettings.json
           .Build();


    var s = config.GetValue<string>("Settings:Test");

    var settings = config.GetSection(Settings.SettingsName).Get<Settings>();

    List<AzureShardedStorageConnection> tableGrainStores = new List<AzureShardedStorageConnection>();
    if(settings?.TableStorageAccounts != null && settings.TableStorageAccounts.Any())
    {
        foreach(var row in settings.TableStorageAccounts)
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

    // NOTE: If you get 'Server failed to authenticate the request.', the SAS token is invalid. Use the Portal to create new ones.

    var builder = new HostBuilder()
        .UseOrleans(c =>
        {
            c.UseLocalhostClustering()
            .AddAzureShardedGrainStorage("ShardedTableStorageStore", opt =>
            {
                opt.ConnectionStrings = tableGrainStores;
            })
            .AddAzureShardedGrainStorage("ShardedBlobStorageStore", opt =>
            {
                opt.ConnectionStrings = blobGrainStores;
            })
            //.AddAzureTableGrainStorage(name: "SimpleTableStorage1", o =>
            //{
            //    o.ConfigureTableServiceClient(settings.SimpleTableStorageConn);
            //})
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev";
                options.ServiceId = "OrleansBasics";
            })
            .ConfigureLogging(
                logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information)
            );
        });

    var host = builder.Build();
    await host.StartAsync();

    return host;
}