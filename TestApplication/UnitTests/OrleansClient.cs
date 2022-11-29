using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    internal class OrleansClient
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
