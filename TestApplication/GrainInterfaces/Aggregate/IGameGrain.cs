using GrainInterfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces.Aggregate
{
    public interface IGameGrain : IGrainWithGuidKey
    {
        Task AddPeopleToGameAsync(List<JoinGameMessage> joinGameMessages);

        Task<int> GetCountOfPeopleInGame();

        // If you put a query here, remember you should return an object containing a list, not a list itself.
    }
}
