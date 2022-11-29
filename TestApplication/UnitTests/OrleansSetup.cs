using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Orleans.Configuration;
using OrleansShardedStorageProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    internal class OrleansSetup
    {

        internal static async Task<IHost> StartClientAsync()
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


    }
}
