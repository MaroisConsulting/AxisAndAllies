using Assets.Enums;
using Models;

namespace Assets.Models
{
    public class IndustryModel : PieceModelBase
    {
        public PieceCategory PieceCategory { get; set; } = PieceCategory.Other;

        // The amount of damage inflicted by enemy bombing runs
        public int Damage { get; set; } = 0;
    }
}
