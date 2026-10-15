using System.ComponentModel;

namespace Assets.Enums
{
    [Description("Describes the type of pieces used in the game")]
    public enum PieceType
    {
        [Description("Infantry")]
        Infantry,

        [Description("Artillery")]
        Artillery,

        [Description("Tank")]
        Tank,

        [Description("AntiAircraft Artillery")]
        AntiAircraftArtillery,

        [Description("Submarine")]
        Submarine,

        [Description("Transport")]
        Transport,

        [Description("Destroyer")]
        Destroyer,

        [Description("Cruiser")]
        Cruiser,

        [Description("Aircraft Carrier")]
        AircraftCarrier,

        [Description("Battleship")]
        Battleship,

        [Description("Fighter")]
        Fighter,

        [Description("Bomber")]
        Bomber,

        [Description("Industry")]
        Industry
    }
}
