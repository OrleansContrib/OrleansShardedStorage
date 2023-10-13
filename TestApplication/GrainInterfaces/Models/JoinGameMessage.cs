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
        /// <summary>
        ///  This is the ID of the person in the game.
        /// </summary>
        [Id(0)]
        public Guid PersonGuid { get; set; }

        [Id(1)]
        public Guid GameGuid { get; set; }

        [Id(2)]
        public string Name { get; set; }
    }
}
