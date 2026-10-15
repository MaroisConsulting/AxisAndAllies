using Enums;
using System.Collections.Generic;

namespace GamePieces
{
    public class PlayerModel
    {
        public Countries Country { get; set; }
        
        public string PlayerName { get; set; }
        
        public bool IsCurrentTurn { get; set; }

        public List<Territory> Territories;
    }
}
