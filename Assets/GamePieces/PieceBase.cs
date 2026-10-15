using Assets.Enums;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GamePieces
{
    /// <summary>
    /// Base class for all game pieces on the board. Holds the piece's combat stats,
    /// ownership, and its current position on the map.
    /// </summary>
    public class PieceBase : MonoBehaviour
    {
        /// <summary>
        /// Gets or sets the unique identifier for this piece instance.
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Gets or sets the purchase cost of this piece in IPCs.
        /// </summary>
        public int Cost { get; set; }

        /// <summary>
        /// Gets or sets the number of territories (or sea zones) this piece can move per turn.
        /// </summary>
        public int Move { get; set; }

        /// <summary>
        /// Gets or sets the attack value. A die roll at or below this value scores a hit when attacking.
        /// </summary>
        public int Attack { get; set; }

        /// <summary>
        /// Gets or sets the defense value. A die roll at or below this value scores a hit when defending.
        /// </summary>
        public int Defense { get; set; }

        /// <summary>
        /// Gets or sets the number of hits this piece must take before it is destroyed.
        /// </summary>
        public int HitsRequiredToDestroy { get; set; }

        /// <summary>
        /// Gets or sets the number of hits this piece has taken so far.
        /// </summary>
        public int TotalHitsSustained { get; set; }

        /// <summary>
        /// Gets or sets the country that owns this piece.
        /// </summary>
        public Countries Owner { get; set; } = 0;

        /// <summary>
        /// Gets or sets the territory or sea zone this piece currently occupies.
        /// </summary>
        public Territory CurrentTerritory { get; set; }

        /// <summary>
        /// Gets or sets the territories adjacent to <see cref="CurrentTerritory"/>.
        /// </summary>
        public List<Territory> AdjacentTerritories { get; set; }

        /// <summary>
        /// Gets or sets the broad category of this piece (for example, land, sea, or air).
        /// </summary>
        public PieceCategory PieceCategory { get; set; }

        /// <summary>
        /// Gets or sets the specific type of this piece.
        /// </summary>
        public PieceType PieceType { get; set; }
    }
}
