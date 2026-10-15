using Assets.Enums;
using Models;

namespace Assets.Models
{
    public class ShipModelBase : PieceModelBase
    {
        public PieceCategory PieceCategory { get; set; } = PieceCategory.Sea;

        public int TotalHitsSustained { get; set; }
    }
}
