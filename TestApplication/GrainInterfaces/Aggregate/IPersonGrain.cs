using GrainInterfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces.Aggregate
{
    public interface IPersonGrain : Orleans.IGrainWithGuidKey
    {
        Task<string> ConfirmGameJoined(JoinGameMessage joinGameMessage);

        Task<List<Guid>> GetJoinedGames();
    }
}
