using GrainInterfaces.Aggregate;
using GrainInterfaces.Models;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grains.Aggregate
{
    public class GameGrain : Grain, IGameGrain
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<GameGrainState> _state;

        public GameGrain(ILogger<GameGrain> logger,
            [PersistentState("game", "ShardedBlobStorageStore")] IPersistentState<GameGrainState> state
            )
        {
            _logger = logger;
            _state = state;
        }


        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            return base.OnActivateAsync(cancellationToken);
        }


        async Task IGameGrain.AddPeopleToGameAsync(List<JoinGameMessage> joinGameMessages)
        {
            foreach(var row in joinGameMessages)
            {
                this._state.State.PeopleInGame.Add(new PersonInGame() { PersonGuid = row.PersonGuid, Name = row.Name });
                this._state.State.Count = this._state.State.Count + 1;
            }

            await this._state.WriteStateAsync();
        }

        Task<int> IGameGrain.GetCountOfPeopleInGame()
        {
            // WARNING: Do not use LINQ on these types of queries. Make sure the numbers are ready up front!
            //          e.g. you could have a count as an it that gets updated when people join the game.
            var count = this._state.State.Count;
            return Task.FromResult(count);
        }
    }

    [GenerateSerializer]
    public class GameGrainState
    {
        // ID for the game is the Grain ID

        [Id(0)]
        public List<PersonInGame> PeopleInGame { get; set; } = new List<PersonInGame>();

        /// <summary>
        /// NOTE: We store a separate count to make querying faster. PeopleInGame.Count() would be slower.
        /// </summary>
        [Id(1)]
        public int Count { get; set; }
    }

    [GenerateSerializer]
    public class PersonInGame
    {
        [Id(0)]
        public Guid PersonGuid { get; set; }

        [Id(1)]
        public string Name { get; set; }
    }
}
