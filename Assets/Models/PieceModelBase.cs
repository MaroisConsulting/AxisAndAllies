using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class PieceModelBase
    {
        public int Cost { get; set; }
        public int Move { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int HitsRequiredToDestroy { get; set; }
        public Countries Owner { get; set; } = 0;
        public List<TerritoryModel> AdjacentTerritories { get; set; }
    }
}
