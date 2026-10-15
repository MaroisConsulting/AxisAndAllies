using Assets.Enums;
using Models;

namespace Assets.Models
{
    public class LandPieceBase : PieceModelBase
    {
        public PieceCategory PieceCategory { get; set; } = PieceCategory.Land;

    }
}
