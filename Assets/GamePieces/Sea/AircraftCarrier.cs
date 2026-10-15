using Assets.Enums;

namespace Assets.Models
{
    public class AircraftCarrier : ShipPieceBase
    {
        public int TotalPlanesEmbarked { get; set; }

        public AircraftCarrier()
        {
            PieceType = PieceType.AircraftCarrier;
        }
    }
}
