using Assets.Enums;
using Assets.Scripts;
using Enums;
using Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class GameManager
{
    #region Private Fields
    #endregion

    #region Properties
    public static List<PlayerModel> Players { get; set; }
    
    public static PlayerModel CurrentPlayer { get; set; }
    
    public static PieceModelBase SelectedPiece { get; set; }
    
    public static TerritoryModel SelectedTerrory { get; set; }

    public static List<PlayerModel> Allies { get; set; }
    
    public static List<PlayerModel> Axis { get; set; }

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
    public static void SelectionMade(GameObject gameObject)
    {
        var name = gameObject.name;
        Debug.Log($"Selection is '{name}'");

        var piece = gameObject.GetComponent<Piece>();
        if (piece != null)
        {
            var pieceModel = piece.PieceModel;
            SelectedPiece = pieceModel;
            Debug.Log($"Piece '{pieceModel}' selected");
        }

        var territory = gameObject.GetComponent<Territory>();
        if (territory != null)
        {
            var territoryModel = territory.model;
            SelectedTerrory = territoryModel;
            Debug.Log($"Territory '{territoryModel}' selected");
        }
    }

    public static void DestinationSelected(GameObject gameObject)
    {
        var territory = gameObject.GetComponent<Territory>();
        if (territory != null)
        {
            var territoryModel = territory.model;
            SelectedTerrory = territoryModel;
            Debug.Log($"Destination territory '{territoryModel.Name}' selected");
        }
    }
    #endregion
}
