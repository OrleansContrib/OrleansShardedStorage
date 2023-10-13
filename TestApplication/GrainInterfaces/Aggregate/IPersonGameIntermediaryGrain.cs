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
        [OneWay]
        Task AddPersonToGameAsync(JoinGameMessage joinGameMessage);
    }
}
