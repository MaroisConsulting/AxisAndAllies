using Assets.Enums;
using Assets.Scripts;
using Enums;
using GamePieces;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Scripts
{
    public static class GameManager
    {
        #region Properties
        public static List<PlayerModel> Players { get; set; }

        public static List<PlayerModel> Allies { get; set; }

        public static List<PlayerModel> Axis { get; set; }

        public static PlayerModel CurrentPlayer { get; set; }

        public static PieceBase SelectedPiece { get; set; }

        public static Territory SelectedTerrory { get; set; }

        public static Phases CurrentPhase { get; set; }

        public static SettingsManager Settings { get; private set; }
        #endregion

        #region CTOR
        static GameManager()
        {
            Settings = new SettingsManager();
        }
        #endregion

        #region Methods
        public static void DestinationSelected(GameObject gameObject)
        {
            var territory = gameObject.GetComponent<Territory>();
            if (territory != null)
            {
                SelectedTerrory = territory;
                Debug.Log($"Destination territory '{territory.Name}' selected");
            }
        }

        public static void TerritorySelected(Territory territory)
        {
            //TODO: Show the territory info pane along the bottom edge

            SelectedTerrory = territory;
        }

        public static void PieceSelected(PieceBase piece)
        {
            //TODO: Show the piece info pane along the bottom edge

            SelectedPiece = piece;
        }
        #endregion
    }
}

