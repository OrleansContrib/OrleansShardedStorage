using GrainInterfaces.Aggregate;
using GrainInterfaces.Models;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using OrleansCodeGen.Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grains.Aggregate
{

    [Reentrant]
    [StatelessWorker]
    public class PersonGameIntermediaryGrain : Grain, IPersonGameIntermediaryGrain
    {
        private IDisposable? _timer;
        private Queue<JoinGameMessage> _queue = new Queue<JoinGameMessage>();
        private bool _isTimerRunning;
        private readonly ILogger<PersonGameIntermediaryGrain> _logger;

        public PersonGameIntermediaryGrain(ILogger<PersonGameIntermediaryGrain> logger)
        {
            _logger = logger;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // Note: You can play with shortening the intermediary time if you want to speed things up, but that causes extra traffic
            // so you have to weigh up the pros and the cons.
            _timer = RegisterTimer(AddPeopleToGames, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
            return base.OnActivateAsync(cancellationToken);
        }

        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            if(_timer != null)
            {
                _timer?.Dispose();
            }
            
            return base.OnDeactivateAsync(reason, cancellationToken);
        }

        Task IPersonGameIntermediaryGrain.AddPersonToGameAsync(JoinGameMessage joinGameMessage)
        {
            _queue.Enqueue(joinGameMessage);
            return Task.CompletedTask;
        }


        private async Task AddPeopleToGames(object e)
        {
            if(!this._queue.Any() || _isTimerRunning)
            {
                return;
            }

            _isTimerRunning = true;

            var allMessages = this._queue.ToList();
            _queue.Clear();

            var gameGuids = allMessages.Select(_ => _.GameGuid).Distinct();

            foreach(var gameGuid in gameGuids)
            {
                var grain = this.GrainFactory.GetGrain<IGameGrain>(gameGuid);
                var messagesForGrain = allMessages.Where(_ => _.GameGuid == gameGuid).ToList();
                await grain.AddPeopleToGameAsync(messagesForGrain);
            }

            _isTimerRunning = false;
        }

    }
}
