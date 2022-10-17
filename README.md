# Orleans Sharded Storage Provider (TABLES AND BLOBS)

This project will split grain data fairly evenly over a number of table storage accounts. 

This can help increase throughput.

It can handle Table Storage and Blob Storage.

It is experimental - I take no responsibility for any bugs in it!

WARNING: Unlike Orleans Standard Libraries, for table storage, this will not split data up over multiple columns - large data will break!

Many of the Orleans methods are internal or private, so I can't access them. As such, this is fully home-brewed.

## How to Use


WARNING: You cannot change the storage accounts or the number of storage accounts after initial setup! Doing so WILL lead to a loss of data.


In the Silo project you will need a secrets.json file that looks like this (add as many accounts/sas as you like!):

```
{
  "StorageAccount:0:AcctName": "orleansstorage1",
  "StorageAccount:0:Sas": "?sv=2020-08-04&ss=bfqt&srt=sco&sp=rwdlacupitfx&se=20.......",
  "StorageAccount:1:AcctName": "orleansstorage2",
  "StorageAccount:1:Sas": "?sv=2020-08-04&ss=bfqt&srt=sco&sp=rwdlacupitfx&se......."
}
```


In the SILO, make a list of connections like so:

```
List<AzureShardedStorageConnection> grainStores = new List<AzureShardedStorageConnection>();

    for (int i = 0; i < 1000; i++)
    {
        var storageAcctName = config.GetValue<string>($"StorageAccount:{i}:AcctName");
        var sasToken = config.GetValue<string>($"StorageAccount:{i}:Sas");
        if (!String.IsNullOrEmpty(storageAcctName))
        {
            // NOTE: Overloads available! Can use Table or Blob Storage :)
            grainStores.Add(new AzureShardedStorageConnection(storageAcctName, sasToken, StorageType.TableStorage));
        }
        else
        {
            break; // No more config values
        }
    }
```


Then add it to the silo:

```
siloBuilder
        .AddAzureShardedGrainStorage("ShardedStorageX", opt => {
            opt.ConnectionStrings = grainStores;
        })

```

(You can also use AddAzureShardedGrainStorageAsDefault)

where 'ShardedStorageX' is the same reference as used in the grain e.g. 




```
public class NumberStoreGrain : Orleans.Grain, INumberStoreGrain
{
    private readonly ILogger logger;
    private readonly IPersistentState<NumberInfo> _state;

    public NumberStoreGrain(ILogger<NumberStoreGrain> logger,
        [PersistentState("numberinfo", "ShardedStorageX")] IPersistentState<NumberInfo> state
        )
    {
        this.logger = logger;
        this._state = state;
    }
    // ....
}


```

Options are available to override the Table (or Container name if using Blobs) name. 

The table/ container will be created on startup.

## How it works

The sharding works by taking a hash of the grainreference and using a modulus of that to work out where to store information for that grain.



