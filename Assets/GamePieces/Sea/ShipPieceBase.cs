using Assets.Enums;
using GamePieces;

namespace Assets.Models
{
    public class ShipPieceBase : PieceBase
    {
        public PieceCategory PieceCategory { get; set; } = PieceCategory.Sea;
    }
}
