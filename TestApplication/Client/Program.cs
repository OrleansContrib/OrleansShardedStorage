using Orleans.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GrainInterfaces;
using Orleans.Serialization.Invocation;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

try
{
    var host = await StartClientAsync();
    var client = host.Services.GetRequiredService<IClusterClient>();

    await DoClientWorkAsync(client);
    Console.ReadKey();

    return 0;
}
catch (Exception e)
{
    Console.WriteLine($"\nException while trying to run client: {e.Message}");
    Console.WriteLine("Make sure the silo the client is trying to connect to is running.");
    Console.WriteLine("\nPress any key to exit.");
    Console.ReadKey();
    return 1;
}

static async Task<IHost> StartClientAsync()
{
    var builder = new HostBuilder()
        .UseOrleansClient(client =>
        {
            client.UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                });
        })
        .ConfigureLogging(logging => logging.AddConsole());

    var host = builder.Build();
    await host.StartAsync();

    Console.WriteLine("Client successfully connected to silo host \n");

    return host;
}

static async Task DoClientWorkAsync(IClusterClient client)
{
    Console.WriteLine($"NOTE: As with all Orleans stuff. Run Client+Silo in 'Release', outside of Visual Studio for fastest results");

    // Calls to SmallDataGrain (which uses table storage)

    StringBuilder sb = new StringBuilder();

    int iterationsTbl = 30;
    for (int i = 0; i < iterationsTbl; i++)
    {
        var grain = client.GetGrain<ISmallDataGrain>(i);
        var id = grain.GetGrainId().ToString();

        var test = $"[InlineData({i}, \"{id}\")]";
        sb.AppendLine(test);
        
    }

    sb.AppendLine();
    sb.AppendLine();


    // Calls to SmallDataWithGuidKeyGrain (which uses blob storage)
    List<string> idsGuidGrains = new List<string>();

    List<Guid> guidIds = new List<Guid>()
    {
        new Guid("418eccf6-3ffa-4e03-8b86-989b8a9564a2"),
        new Guid("080563f5-bb61-413f-8737-16af064705aa"),
        new Guid("08765ce0-4254-4915-b26e-f93859ac4cac"),
        new Guid("5757f716-fa3d-4119-9dda-ffe1775e6e9b"),
        new Guid("5a475386-0fc6-431a-9937-52ed777d65b9"),
        new Guid("55830085-e13c-4032-9721-f8bf27f7a255"),
        new Guid("5a2f83db-13a5-4c03-8a86-6dc0d63aff96"),
        new Guid("efe6b946-f7c0-4ead-ae78-532f6bd21612"),
        new Guid("490d018a-1ebc-435e-a046-99537651dd24"),
        new Guid("29706b5f-be6c-474d-b7ee-d0da9785eea0"),
        new Guid("7227cf04-6886-4ec0-9936-83a2fc4a26ed"),
        new Guid("e7e2b5ed-18c5-4e32-a3ec-701a29d4d87f"),
        new Guid("b1adbde2-6128-4f9c-ba63-c16b4127cb0e"),
        new Guid("fab0a861-bc6a-40d3-be54-945e57032c70"),
        new Guid("e212f313-70ae-4150-ab12-a90e19b845ad"),
        new Guid("8cb5cf94-13f7-49fc-b565-812a7075e45d"),
        new Guid("fd634b5e-05b4-4f71-b6e9-b61f965430b3"),
        new Guid("898aa0ab-fee5-4b4e-82f8-e86b49fe8ad1"),
        new Guid("f2904a47-6e7f-4e91-abc8-4684eec41172"),
        new Guid("aab55616-c731-4281-80fb-7cf704a30d9a"),
        new Guid("170d9e08-d07c-4120-a4eb-da612b1466ed"),
        new Guid("79d295d6-b9b1-45fb-ac62-38589d8fda89"),
        new Guid("98d1bfe4-0c72-438b-a637-61cd1923e02b"),
        new Guid("047cc294-8672-4e5d-9177-d56dcf98ef23"),
        new Guid("56f8d1f5-3198-4325-b39b-10c132d5efe2"),
        new Guid("e43f4b20-c6f0-4200-8bdf-03eb7ed15987"),
        new Guid("752ee328-d93a-4ec6-904a-756772e999a3"),
        new Guid("64b09c53-50e5-4213-b23e-bdd62d6ec0b9"),
        new Guid("c1dc1d33-8aa5-4413-af3d-cb4f937f922a"),
        new Guid("c8a32e90-6d77-4ee3-bbf9-4f1d23b32b27")
    };




    foreach (var i in guidIds)
    {
        var grain = client.GetGrain<ILargeDataGrain>(i.ToString());
        var id = grain.GetGrainId().ToString();

        var test = $"[InlineData(new Guid(\"{i}\"), {id})]";
        sb.AppendLine(test);
    }

    sb.AppendLine();
    sb.AppendLine();

    // Calls to LargeDataGrain (which uses blob storage)

    List<string> strings= new List<string>()
    {
        "bBIVZS1jpTou7Wqsdfhgbfds",
        "cJ0yDVot74jnYIu",
        "zWdvQZe6WXesWZk",
        "klNxQmXw2nKVhkd",
        "ubQqHByZnof",
        "cIqxoet6FTxeIYy",
        "2G7FH8YWfL0fP7S",
        "zw88L",
        "JAJ7fBrCdCF9zzrsadfgjhmnbfdsaqw345SDFG",
        "zv9lJ6L5BhniS6s",
        "Jl8c4e2iwznodWr",
        "3cihIvR1KCczBKWasdfgkj",
        "nsz5ce712kVYBQB",
        "lyHl9r1KVwIP4jy",
        "dYy0KJDPKO5Z3cs",
        "7sFE6lCjKQ6rZI0WQERTGFVCX",
        "TOWX2QnUS3E0gU0",
        "VJmXr0QYWaTwy5V",
        "gth11LyHjUWAwHv",
        "5sbjJbiYKZ2AEbj",
        "xfsrt5kGC6G8dps",
        "V5LQUXLB3w30yNZadsfghjm",
        "8wHJgvjkZenqqK7",
        "laZP6HNbNJDWc7B",
        "GRRRRRRRRRRR",
        "MIOWMIOWWOOFWOOF",
        "2983WzauiXjwrBHsRGasgAV",
        "GKYkBfWKeU6cyz2",
        "Mz4oPtfcsSqGhLY",
        "DPAqY2f7spYdZDb",
    };

    foreach (var i in strings)
    {
        var grain = client.GetGrain<ILargeDataGrain>(i.ToString());
        var id = grain.GetGrainId().ToString();

        var test = $"[InlineData(\"{i}\", \"{id}\")]";
        sb.AppendLine(test);

    }

    var everything = sb.ToString();

}