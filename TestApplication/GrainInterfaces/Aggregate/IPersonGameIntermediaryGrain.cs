using GrainInterfaces.Models;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces.Aggregate
{
    public interface IPersonGameIntermediaryGrain : IGrainWithIntegerKey
    {
        //[OneWay] - Don't make this oneway. We need to know if the message doesn't make it into the system.
        Task AddPersonToGameAsync(JoinGameMessage joinGameMessage);
    }
}
