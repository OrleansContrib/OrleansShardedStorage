using Orleans.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GrainInterfaces;
using Orleans.Serialization.Invocation;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using OrleansCodeGen.Orleans.Core.Internal;
using GrainInterfaces.Models;
using GrainInterfaces.Aggregate;
using System.Diagnostics;
using System;

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
	Console.WriteLine($"NOTE: This is a load test.");
	Console.WriteLine("It is NOT using the Reduce Pattern to find breaking points directly.");
	Console.WriteLine("See standard client for reduce tests.");

	Console.WriteLine();
	Console.WriteLine("Enter the number of grains to create and hit enter.");
	var numberString = Console.ReadLine();
	Console.WriteLine();

	if (!String.IsNullOrWhiteSpace(numberString))
	{
		int number = int.Parse(numberString); // It's a test console. No input checking.

		Console.WriteLine("Start Test Prep");

		List<JoinGameMessage> messagesForPeople = new List<JoinGameMessage>();

		var game1 = Guid.NewGuid();

		for (int i = 0; i < number; i++)
		{
			messagesForPeople.Add(new JoinGameMessage()
			{
				PersonGuid = Guid.NewGuid(),
				GameGuid = game1,
				Name = $"Game 1, Person {i}"
			});
		}

		Console.WriteLine("Warm Up Grains");
		Stopwatch st = new Stopwatch();
		st.Start();

		var warmUpTask = async (JoinGameMessage msg) =>
		{
			var grain = client.GetGrain<IPersonGrain>(msg.PersonGuid);
			await grain.WarmUp();
		};

		List<Task> warmUpTasks = new List<Task>();
		foreach (var pMsg in messagesForPeople)
		{
			warmUpTasks.Add(warmUpTask(pMsg));
		}

		//await Task.WhenAll(warmUpTasks);
		Task.WaitAll(warmUpTasks.ToArray());

		st.Stop();
		Console.WriteLine($"Warmed up {number} grains in {st.ElapsedMilliseconds}ms");
		Console.WriteLine($"Start main test");
		st.Reset();
		st.Start();

		List<Task<string>> sendTasks = new List<Task<string>>();

		var sendPersonDataTask = async (JoinGameMessage msg) =>
		{
			var grain = client.GetGrain<IPersonGrain>(msg.PersonGuid);
			var result = await grain.ConfirmGameJoined(msg);
			return result;
		};

		foreach (var pMsg in messagesForPeople)
		{
			sendTasks.Add(sendPersonDataTask(pMsg));
		}

		var results = await Task.WhenAll(sendTasks);

		st.Stop();

		Console.WriteLine($"{results.Count()} sends/saves in {st.ElapsedMilliseconds}ms");

		Console.WriteLine();
		Console.WriteLine("Done");

	}
	else
	{
		Console.WriteLine("No input. Press enter again to exit the console.");
	}

	Console.ReadLine();

}
