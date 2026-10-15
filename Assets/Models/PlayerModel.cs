using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class PlayerModel
    {
        public Countries Country { get; set; }
        
        public string PlayerName { get; set; }
        
        public bool IsCurrentTurn { get; set; }

        public List<TerritoryModel> Territories;
    }
}
