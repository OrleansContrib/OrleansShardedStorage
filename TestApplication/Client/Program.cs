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
    int iterationsTbl = 100;
    for (int i = 0; i < iterationsTbl; i++)
    {
        var friend = client.GetGrain<ISmallDataGrain>(i);
        var response = await friend.SayHello($"Yo SM {i}!");
        Console.WriteLine($"{response}");
    }

    // Calls to LargeDataGrain (which uses blob storage)
    int iterationsBlob = 100;
    for (int i = 0; i < iterationsBlob; i++)
    {
        var friend = client.GetGrain<ILargeDataGrain>(i.ToString());
        var response = await friend.SayHello($"Hi LG {i}!");
        Console.WriteLine($"{response}");
    }


}