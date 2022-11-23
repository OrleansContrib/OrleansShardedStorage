using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using System.Reflection;

namespace Grains
{
    public class SmallDataGrain : Orleans.Grain, ISmallDataGrain
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<SmallDataGrainState> _state;

        public SmallDataGrain(ILogger<SmallDataGrain> logger,
            [PersistentState("smalldata", "ShardedTableStorageStore")] IPersistentState<SmallDataGrainState> state
            )
        {
            _logger = logger;
            _state = state;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var grainId = this.GetGrainId();

            if (String.IsNullOrWhiteSpace(this._state?.State?.LastMessage))
            {
                _logger.LogInformation($"-->LOADING SM {grainId} - New");
            }
            else
            {
                _logger.LogInformation($"-->LOADING SM {grainId} - msg={this._state?.State?.LastMessage}");
            }
            return base.OnActivateAsync(cancellationToken);
        }

        async Task<string> ISmallDataGrain.SayHello(string greeting)
        {
            this._state.State.LastMessage = greeting;
            this._state.State.ReceivedUtc= DateTime.UtcNow;
            await this._state.WriteStateAsync();
            var thisId = this.GetGrainId();
            _logger.LogInformation($"{this.GetGrainId()}: {greeting}");
            return $"\n Client said: '{greeting}', so SM Grain {thisId} says: Sup!";
        }
    }


    [GenerateSerializer]
    public class SmallDataGrainState
    {
        [Id(0)]
        public string LastMessage { get; set; }
        [Id(1)]
        public DateTime ReceivedUtc { get; set; }
    }
}
