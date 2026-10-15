using Assets.Enums;
using Models;

namespace Assets.Models.Air
{
    public class AirPieceBase : PieceModelBase
    {
        public PieceCategory PieceCategory { get; set; } = PieceCategory.Air;
    }
}
