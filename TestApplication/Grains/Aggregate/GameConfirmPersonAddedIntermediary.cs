using GrainInterfaces.Aggregate;
using GrainInterfaces.Models;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grains.Aggregate
{

    [Reentrant]
    [StatelessWorker]
    public class GameConfirmPersonAddedIntermediary : Grain, IGameConfirmPersonAddedIntermediary
    {
        private IGrainTimer? _timer;
        private List<JoinGameMessage> _messagesStore = new List<JoinGameMessage>();
        private bool _isTimerRunning;
        private readonly ILogger<GameConfirmPersonAddedIntermediary> _logger;

        public GameConfirmPersonAddedIntermediary(ILogger<GameConfirmPersonAddedIntermediary> logger)
        {
            _logger = logger;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // Note: You can play with shortening the intermediary time if you want to speed things up, but that causes extra traffic
            // so you have to weigh up the pros and the cons.
            _timer = this.RegisterGrainTimer(SendConfirmationToPerson, new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromMilliseconds(100),
                Period = TimeSpan.FromMilliseconds(100),
                Interleave = true
            });
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

        Task IGameConfirmPersonAddedIntermediary.NotifyPersonOfGameConfirmationAsync(List<JoinGameMessage> joinGameMessages)
        {
            _messagesStore.AddRange(joinGameMessages);
            return Task.CompletedTask;
        }


        private async Task SendConfirmationToPerson(CancellationToken cancellationToken)
        {
            if(!this._messagesStore.Any() || _isTimerRunning || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _isTimerRunning = true;

            var allMessages = this._messagesStore.ToList();
            _messagesStore.Clear();

            foreach(var joinMessage in  allMessages)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var grain = this.GrainFactory.GetGrain<IPersonGrain>(joinMessage.PersonGuid);
                await grain.ConfirmGameJoined(joinMessage);
            }

            _isTimerRunning = false;
        }

    }
}
