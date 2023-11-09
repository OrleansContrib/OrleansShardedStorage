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
    public class PersonGrain : Grain, IPersonGrain
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<PersonGrainState> _state;

        // Could possibly use sharded table storage if this stays light, but having a list makes it risky.
        public PersonGrain(ILogger<PersonGrain> logger,
            [PersistentState("person", "ShardedBlobStorageStore")] IPersistentState<PersonGrainState> state
            )
        {
            _logger = logger;
            _state = state;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            return base.OnActivateAsync(cancellationToken);
        }


        async Task<string> IPersonGrain.ConfirmGameJoined(JoinGameMessage joinGameMessage)
        {
            this._state.State.Name = joinGameMessage.Name;
            this._state.State.GameGuids.Add(joinGameMessage.GameGuid);
            await this._state.WriteStateAsync();
            return "OK";
        }


        Task<List<Guid>> IPersonGrain.GetJoinedGames()
        {
            return Task.FromResult(this._state.State.GameGuids);
        }
    }


    public class PersonGrainState
    {
        // The ID for this person is the grain Key

        [Id(0)]
        public List<Guid> GameGuids { get; set; } = new List<Guid>();

        [Id(1)]
        public string Name { get; set; }
    }

}
