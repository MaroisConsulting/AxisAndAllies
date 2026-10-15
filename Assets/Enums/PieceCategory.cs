using System.ComponentModel;

namespace Assets.Enums
{
    [Description("Describes the categories of pieces used in the game")]
    public enum PieceCategory
    {
        [Description("Land")]
        Land,

        [Description("Air")]
        Air,

        [Description("Sea")]
        Sea,

        [Description("Other")]
        Other
    }
}
