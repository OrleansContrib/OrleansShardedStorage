using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrainInterfaces.Models
{
    [GenerateSerializer]
    public class JoinGameMessage
    {
        [Id(0)]
        public Guid PersonId { get; set; }

        [Id(1)]
        public Guid GameGuid { get; set; }

        [Id(2)]
        public string Name { get; set; }
    }
}
