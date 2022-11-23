# Orleans Sharded Storage Provider (TABLES AND BLOBS) 

This is for Orleans 7+. See here for the deprecated [3.6.2 version](https://github.com/JsAndDotNet/OrleansShardedStorage/tree/Orleans-3.6.2).

---

# What is it?

Azure Storage has [Request Limitations per storage account](https://learn.microsoft.com/en-us/azure/storage/common/scalability-targets-standard-account), which can easily be hit in high throughput applications. 

This code will '[shard](https://learn.microsoft.com/en-us/azure/architecture/patterns/sharding)' Orleans grains over multiple storage accounts to avoid bottlenecks. This is useful if you're running/updating many thousands of the same type of grain at once.

It can handle Table Storage and Blob Storage.

# How does it work?

`OrleansShardedStorageProvider` will split grain data fairly evenly over a number of table storage accounts to help increase throughput. 

It does this by taking a hash of the grain reference and using a modulus of that to work out where to store information for that grain.

## WARNINGS

**You cannot change the number of storage accounts at a later date. What you start with is what you're stuck with!** This is common for sharded solutions.

It is experimental - I take no responsibility for any bugs in it!

**Unlike Orleans Standard Libraries; for table storage, this will not split data up over multiple columns - large data will break saving to table storage (so use Blob storage as a work around)!**

Many of the Orleans library classes/methods (such as `AzureTableDataManager`) are internal or private, so I can't access them. As such, this is fully home-brewed. Here is a list of files referred to in making this `OrleansShardedStorageProvider\OrleansRefs.txt`.

----
----


## How to Set up the Test Application


This repo consists of the main `OrleansShardedStorageProvider` class library and a `TestApplication` (4 projects: Silo, Client, Grains, GrainInterfaces) to show how to use it. 

It is not yet deployed as a NuGet Package.

To run the test application:

1. Open `OrleansShardedStorage.sln` in VS2022 (>=17.4.1).

2. To get up and running, you will need to create a few Storage Accounts in Azure. You can do this in the Azure Portal or using the provide script. `OrleansShardedStorageProvider\create-multiple-storage-accounts.ps1` will create 6 storage accounts and output a list you can put in the sample app config. You will need to update some info at the top of this script to get it to work! If it creates the resource group, the output seems to stick, so wait 5 mins and it should be done.

3. In `TestApplication\Silo`, update `appsettings.json`. Add 3 names/sastoken pairs to `TableStorageAccounts` and 3 to `BlobStorageAccounts`. (Note: You could use appsettings as a template and update `secrets.json`) The final config will look something like:

```
{
  "Settings": {
    "Test": "Val",
    "SimpleTableStorageConn": "not used here",
    "TableStorageAccounts": [
      {
        "Name": "mystorageacc1",
        "SasToken": "?sv=2021-06-08&ss=bfqt&..."
      },
      {
        "Name": "mystorageacc2",
        "SasToken": "?sv=2021-06-08&ss=bfqt&sr..."
      },
      {
        "Name": "mystorageacc3",
        "SasToken": "?sv=2021-06-08&ss=bfqt&srt=sc..."
      }
    ],
    "BlobStorageAccounts": [
      {
        "Name": "mystorageacc4",
        "SasToken": "?sv=2021-06-08&ss=bf..."
      },
      {
        "Name": "mystorageacc5",
        "SasToken": "?sv=2021-06-08&ss=bf..."
      },
      {
        "Name": "mystorageacc6",
        "SasToken": "?sv=2021-06-08&ss=bfq..."
      }
    ]
  }
}


```


4. Run `TestApplication\Silo\Silo.csproj`.

5. Run `TestApplication\Client\Client.csproj`

>NOTE: The client will be slow because it's running in debug and I haven't multi-threaded it for ease for understanding.

6. The client will call/create 100 grains that save to table storage and 100 that save to blob storage. If you view your storage accounts in `Microsoft Azure Storage Explorer` (or equivalent), you will see the data has been split across multiple accounts.

>The Table storage splits (33,33,34) and Blob Storage splits (34,35,31)

7. Stop the Client/Silo and start again. If you put breakpoints in the grains `OnActivateAsync`, you will see data loaded back into the grain.

>NOTE: Running in Visual Studio in debug is slow. To run at rull speed you need to be in Release and run the exe's outside VS.

8. END

## Setting Up Your Own Application

1. First you'll need to copy the `OrleansShardedStorageProvider` into the same folder as your solution (there's no Nuget Package yet, because I don't want the support :smile: ).

2. In Visual Studio, Add an existing project reference to `OrleansShardedStorageProvider.csproj`.

3. In the silo, you will need to load in a set of storage account names/sas tokens using code as shown below.

```
List<AzureShardedStorageConnection> tableGrainStores = new List<AzureShardedStorageConnection>();
foreach(var row in yourListOfStorageAccountsAndSasTokens)
{
    tableGrainStores.Add(new AzureShardedStorageConnection(row.Name, row.SasToken, StorageType.TableStorage));
}
```

4. Now in the `HostBuilder`, add the `AddAzureShardedGrainStorage` with a name (e.g. "ShardedTableStorageStore") and the connectionstrings:

```
var builder = new HostBuilder()
        .UseOrleans(c =>
        {
            c.UseLocalhostClustering()
            // --- This bit here!
            .AddAzureShardedGrainStorage("ShardedTableStorageStore", opt =>
            {
                opt.ConnectionStrings = tableGrainStores;
            })
            // ---
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
```

5. In the `Grain`, add a NAMED persistent storage (e.g. "ShardedTableStorageStore") as you would usually.

```
public class SmallDataGrain : Orleans.Grain, ISmallDataGrain
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<SmallDataGrainState> _state;

        public SmallDataGrain(ILogger<SmallDataGrain> logger,
            [PersistentState("smalldata", "ShardedTableStorageStore")] IPersistentState<SmallDataGrainState> state
            )
        {
            _logger = logger;
            _state = state;
        }

    }

```


6. END



