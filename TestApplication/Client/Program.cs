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
	Console.WriteLine();
	Console.WriteLine("Run the simple tests? (Y/N)");
	var key = Console.ReadKey().Key;
	if (key == ConsoleKey.Y)
	{
		// Calls to SmallDataGrain (which uses table storage)
		int iterationsTbl = 100;
		for (int i = 0; i < iterationsTbl; i++)
		{
			var friend = client.GetGrain<ISmallDataGrain>(i);
			var response = await friend.SayHello($"Yo Small Data Grain {i}!");
			Console.WriteLine($"{response}");
		}

		// Calls to LargeDataGrain (which uses blob storage)
		int iterationsBlob = 100;
		for (int i = 0; i < iterationsBlob; i++)
		{
			var friend = client.GetGrain<ILargeDataGrain>(i.ToString());
			var response = await friend.SayHello($"Hi Large Data Grain {i}!");
			Console.WriteLine($"{response}");
		}
	}


	Console.WriteLine();
	Console.WriteLine("Run the adding people to games tests? (Y/N)");
	key = Console.ReadKey().Key;

	if (key == ConsoleKey.Y)
	{
		Console.WriteLine("Start Test");

		// Create a list of 100 people. Create 2 games.
		// Add 100 people to each game
		// See how long it takes to send the messages and for them to settle (i.e. be counted in the game).
		// Note: You can play with shortening the intermediary time if you want to speed things up, but that causes extra traffic
		// so you have to weigh up the pros and the cons.
		List<Guid> people = new List<Guid>();

		for (int i = 0; i < 100; i++)
		{
			people.Add(Guid.NewGuid());
		}

		var game1 = Guid.NewGuid();
		var game2 = Guid.NewGuid();
		var games = new List<Guid>() { game1, game2 };


		List<JoinGameMessage> messages = new List<JoinGameMessage>();

		for (int g = 0; g < games.Count(); g++)
		{
			for (int i = 0; i < people.Count(); i++)
			{
				messages.Add(new JoinGameMessage()
				{
					PersonGuid = people[i],
					GameGuid = games[g],
					Name = $"Game {g}, Person {i}"
				});
			}
		}


		Stopwatch st = new Stopwatch();
		st.Start();

		// Send the messages
		for (int i = 0; i < messages.Count(); i++)
		{
			var msg = messages[i];

			// NOTE: We add to the intermediary grain, rather than the Person grain to avoid a 2 state system
			//       The Person will be notified by the Game grain later.
			var person = client.GetGrain<IPersonGameIntermediaryGrain>(0);
			await person.AddPersonToGameAsync(msg);
			Console.WriteLine($"Adding Person={msg.PersonGuid} to Game={msg.GameGuid}");
		}

		Console.WriteLine();
		Console.WriteLine($"Eventual Consistency Check - Make sure everyone was registered into their games", ConsoleColor.Blue);
		Console.WriteLine();

		List<Guid> completeSets = new List<Guid>();

		while (completeSets.Count() != games.Count())
		{
			foreach (var game in games)
			{
				if (!completeSets.Contains(game))
				{
					var gameGrain = client.GetGrain<IGameGrain>(game);
					var count = await gameGrain.GetCountOfPeopleInGame();

					if (count == people.Count())
					{
						completeSets.Add(game);
						Console.WriteLine($"Game: {game} complete. Count = {count}", ConsoleColor.White);
					}
					else
					{
						Console.WriteLine($"Game: {game} not yet ready. Count = {count}", ConsoleColor.White);
					}
				}

				await Task.Delay(20);
			}
		}

		st.Stop();

		Console.WriteLine($"ALL {games.Count()} games, with {people.Count()} people each ready in {st.ElapsedMilliseconds}", ConsoleColor.Green);
		Console.WriteLine();
		Console.WriteLine("Print the joins confirmed to people? (Y/N)");

		//Note: This person/game stuff is all eventually consistent, so if you were quick pressing Y, there could be some missing confirmations (unlikely if you added a small number)

		key = Console.ReadKey().Key;

		if (key == ConsoleKey.Y)
		{

			var peopleList = messages.Select(_ => _.PersonGuid).Distinct().ToList();

			int errorCount = 0;

			foreach (var p in peopleList)
			{
				var personGrain = client.GetGrain<IPersonGrain>(p);

				List<Guid> gamesJoined = new List<Guid>();
				int tryCount = 0;
				do
				{
					// NOTE: Because we're eventually consistent in getting the confirmations back to the
					//		 people in the game, we might need to check the person grain a few times until it's been properly updated.
					gamesJoined = await personGrain.GetJoinedGames();
					if(gamesJoined.Count() == games.Count())
					{
						tryCount = 5;
					}
					else
					{
						if(tryCount == 4) errorCount++;

						tryCount++;
						await Task.Delay(200);
					}

				}
				while (tryCount < 5);

				var gamesString = String.Join(",", gamesJoined);
				Console.WriteLine($"P={p},Games={gamesString}");
			}

			Console.WriteLine($"There were {errorCount} errors");
			Console.WriteLine();
			Console.WriteLine("Done");

		}
	}
	else
	{
		Console.WriteLine("Key not 'Y', so exited. Press enter again to exit the console.");
	}

	Console.ReadLine();

}
