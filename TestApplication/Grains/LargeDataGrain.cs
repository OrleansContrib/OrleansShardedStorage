using GrainInterfaces;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using System.Reflection;

namespace Grains
{
    public class LargeDataGrain : Orleans.Grain, ILargeDataGrain
    {
        private readonly ILogger _logger;
        private readonly IPersistentState<LargeDataGrainState> _state;

        public LargeDataGrain(ILogger<LargeDataGrain> logger,
            [PersistentState("smalldata", "ShardedBlobStorageStore")] IPersistentState<LargeDataGrainState> state
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
                _logger.LogInformation($"-->LOADING LG {grainId} - New");
            }
            else
            {
                _logger.LogInformation($"-->LOADING LG {grainId} - msg={this._state?.State?.LastMessage}");
            }

            if(this._state.State.PreviousMessages == null)
            {
                this._state.State.PreviousMessages = new List<string>();
            }

            return base.OnActivateAsync(cancellationToken);
        }

        async Task<string> ILargeDataGrain.SayHello(string greeting)
        {
            this._state.State.LastMessage = greeting;
            this._state.State.PreviousMessages.Add(greeting);
            this._state.State.ReceivedUtc= DateTime.UtcNow;
            await this._state.WriteStateAsync();
            var thisId = this.GetGrainId();
            _logger.LogInformation($"{this.GetGrainId()}: {greeting}");
            return $"\n Client said: '{greeting}', so LG Grain {thisId} says: What ho!";
        }
    }


    [GenerateSerializer]
    public class LargeDataGrainState
    {
        [Id(0)]
        public string LastMessage { get; set; }
        [Id(1)]
        public DateTime ReceivedUtc { get; set; }
        [Id(2)]
        public List<string> PreviousMessages { get; set; }
    }
}
