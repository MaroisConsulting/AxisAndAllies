using Assets.Enums;
using GamePieces;

namespace Assets.Models
{
    public class LandPieceBase : PieceBase
    {
        public PieceCategory PieceCategory { get; set; } = PieceCategory.Land;
    }
}
