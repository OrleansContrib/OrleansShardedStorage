using GrainInterfaces.Models;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces.Aggregate
{
    public interface IGameConfirmPersonAddedIntermediary : IGrainWithIntegerKey
    {
        [OneWay] // This will make the interaction quicker, as it's effectively fire and forget.
        Task NotifyPersonOfGameConfirmationAsync(List<JoinGameMessage> joinGameMessages);
    }
}
