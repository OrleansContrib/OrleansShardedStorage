# Orleans Sharded Table Storage Provider

This project will split grain data fairly evenly over a number of table storage accounts. 

This can help increase throughput.

It's only experimental. There could well be issues with it!

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
List<AzureTableStorageConnection> grainStores = new List<AzureTableStorageConnection>();

for (int i = 0; i < 1000; i++)
{
    var storageAcctName = config.GetValue<string>($"StorageAccount:{i}:AcctName");
    var sasToken = config.GetValue<string>($"StorageAccount:{i}:Sas");
    if (!String.IsNullOrEmpty(storageAcctName))
    {
        grainStores.Add(new AzureTableStorageConnection(storageAcctName, sasToken));
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
        .AddAzureShardedTableGrainStorage("LoadTestNumbersTableStorage1", opt => {
            opt.ConnectionStrings = grainStores;
        })

```

Note: You can also use AddAzureShardedTableGrainStorageAsDefault

'LoadTestNumbersTableStorage1' is the same reference as used in the grain e.g. 


```
public class NumberStoreGrain : Orleans.Grain, INumberStoreGrain
{
    private readonly ILogger logger;
    private readonly IPersistentState<NumberInfo> _state;

    public NumberStoreGrain(ILogger<NumberStoreGrain> logger,
        [PersistentState("numberinfo", "LoadTestNumbersTableStorage1")] IPersistentState<NumberInfo> state
        )
    {
        this.logger = logger;
        this._state = state;
    }
    // ....
}


```

Options are available to override the Table name. The table will be created on startup.

The sharding works by taking a hash of the grainreference and using a modulus of that to work out where to store information for that grain.



