using Assets.Enums;
using GamePieces;

namespace Assets.Models
{
    public class Industry : PieceBase
    {
        public PieceCategory PieceCategory { get; set; } = PieceCategory.Other;

        public Industry()
        {
            PieceType = PieceType.Tank;
        }
    }
}
