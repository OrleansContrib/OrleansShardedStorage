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
        ///  (this is also the key for the person grain)
        /// </summary>
        [Id(0)]
        public Guid PersonGuid { get; set; }

        /// <summary>
        /// The ID of the game the person will join.
        /// (this is also the key for the game grain)
        /// </summary>
        [Id(1)]
        public Guid GameGuid { get; set; }

        [Id(2)]
        public string Name { get; set; }
    }
}
