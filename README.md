# Orleans Sharded Storage Provider (TABLES AND BLOBS) 

This is for Orleans 7+. See here for the deprecated [3.6.2 version](https://github.com/JsAndDotNet/OrleansShardedStorage/tree/Orleans-3.6.2).

---

# What is it?

This project is about splitting data between storage accounts to get past the [Request Limitations per account](https://learn.microsoft.com/en-us/azure/storage/common/scalability-targets-standard-account), which can easily be hit in high throughput applications.

It can handle Table Storage and Blob Storage.

# How does it work?

`OrleansShardedStorageProvider` will split grain data fairly evenly over a number of table storage accounts to help increase throughput. 

It does this by taking a hash of the grainreference and using a modulus of that to work out where to store information for that grain.

## WARNINGS

It is experimental - I take no responsibility for any bugs in it!

IMPORTANT: Unlike Orleans Standard Libraries; for table storage, this will not split data up over multiple columns - large data will break saving to table storage!

Many of the Orleans classes/methods (such as `AzureTableDataManager`) are internal or private, so I can't access them. As such, this is fully home-brewed.

----
----


## How to Set up the Test Application


This repo consists of the main `OrleansShardedStorageProvider` class library and a `TestApplication` (4 projects: Silo, Client, Grains, GrainInterfaces) to show how to use it. 

It is not yet deployed as a NuGet Package.

To run the test application:

1. Open `OrleansShardedStorage.sln` in VS2022 (>=17.4.1).

2. To get up and running, you will need to create a few Storage Accounts in Azure. `OrleansShardedStorageProvider\create-multiple-storage-accounts.ps1` will create 6 storage accounts and output a list you can put in the sample app config. You will need to update some info at the top of this script to get it to work!

```
...
{
        "Name": "account2",
        "SasToken": "?sastoken..."
}
...
```

3. In either `secrets.json` or `appsettings.json` for `TestApplication\Silo`, add 3 names/sas tokens to `TableStorageAccounts` and 3 to `BlobStorageAccounts`. The final config will look something like:

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

6. The client will call/create 100 grains that save to table storage and 100 that save to blob storage. If you view your storage accounts in `Microsoft Azure Storage Explorer` (or equivalent), you will see the data has been split across multiple accounts. 

7. Stop the Client/Silo and start again. If you put breakpoints in the grains `OnActivateAsync`, you will see data loaded back into the grain.

>NOTE: Running in Visual Studio in debug is slow. To run at rull speed you need to be in Release and run the exe's outside VS.


## Setting Up Your Own Application

1. First you'll need to copy the `OrleansShardedStorageProvider` into the same folder as your solution (there's no Nuget Package yet, because I don't want the support calls :smile:).

2. In Visual Studio, Add an existing project reference to `OrleansShardedStorageProvider.csproj`.

3. In the silo, you will need to load in a set of storage account names/sas tokens using code as shown below.

```
List<AzureShardedStorageConnection> tableGrainStores = new List<AzureShardedStorageConnection>();
foreach(var row in yourListOfStorageAccountsAndSasTokens)
{
    tableGrainStores.Add(new AzureShardedStorageConnection(row.Name, row.SasToken, StorageType.TableStorage));
}
```

4. Now in the hostbuilder, add the `AddAzureShardedGrainStorage` with a name (e.g. "ShardedTableStorageStore") and the connectionstrings:

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



