using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Enums
{
    [Description("Defines the phases of a player's turn")]
    public enum Phases
    {
        [Description("Purchase Units")]
        PurchaseUnits,

        [Description("Prepare For Combat")]
        PrepareForCombat,

        [Description("Combat")]
        Combat,

        [Description("Non Combat")]
        NonCombat,

        [Description("Mobilization")]
        Mobilization
    }
}
