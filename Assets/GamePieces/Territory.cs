using Assets.Enums;
using Enums;
using GamePieces;
using System;
using System.Collections.Generic;
using UnityEngine;

// Runs in Edit Mode too, so the territory's color follows its player and IsNeutral while editing the scene.
// One Territory sits on each territory mesh of the WorldMap. It holds the territory's game state, which GameManager
// reads for movement, MapEditor edits, and MapSaveLoad saves and loads through ToData/FromData.
[ExecuteAlways]
public class Territory : MonoBehaviour
{
    // The player value meaning "nobody owns this territory". Territories with it are drawn in NeutralColor.
    public const string NoPlayer = "None";

    // Valid values for player: no player, or any of the countries a piece can belong to.
    // Checked by IsValidPlayer (OnValidate, FromData) and shown as the Player choices in the MapEditor.
    public static readonly string[] ValidPlayers = { NoPlayer, "US", "UK", "Russia", "Germany", "Japan" };

    // Traditional unit colors for each country. Looked up by TerritoryColor when the territory has an owner.
    public static readonly Dictionary<string, Color> CountryColors = new Dictionary<string, Color>
    {
        { "US", new Color32(0x4F, 0x7A, 0x2F, 0xFF) },      // Green
        { "UK", new Color32(0xC9, 0xB0, 0x86, 0xFF) },      // Tan / beige
        { "Russia", new Color32(0x7A, 0x1F, 0x24, 0xFF) },  // Maroon / dark red
        { "Germany", new Color32(0x3A, 0x3A, 0x3A, 0xFF) }, // Very dark gray
        { "Japan", new Color32(0xCC, 0x55, 0x00, 0xFF) },   // Burnt orange
    };

    // Color of neutral territories and territories with no player.
    public static readonly Color NeutralColor = new Color32(0xC8, 0xC8, 0xC8, 0xFF); // Light gray

    // Shader property IDs for the material's main color, cached once. ApplyColor sets whichever the material has.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP shaders
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in shaders

    // Optional ID. MapSaveLoad uses it as a fallback (after the hierarchy path) to match saved data to this territory.
    public string UUID;

    // Display name, shown in the MapEditor and in GameManager move messages. Neighbors are saved by this name,
    // so it should be unique; MapSaveLoad also falls back to it when matching saved territories.
    public string Name = "Unnamed";
    
    // Production value (IPCs) of the territory. Only stored, edited in the MapEditor, and saved for now.
    public int IPC;
    
    // While true, the territory is drawn in NeutralColor whatever its player is.
    public bool IsNeutral = true;
    
    // Owning country, one of ValidPlayers. Sets the territory's color, and the MapEditor gives new pieces this owner.
    [Tooltip("Must be one of: None, US, UK, Russia, Germany, Japan.")]
    public string player = NoPlayer;
    
    // Adjacent territories. GameManager.ValidMovesForPiece walks these, so a territory with none can't be reached.
    public List<Territory> Neighbors = new List<Territory>();
    
    // Pieces standing on this territory. Kept in sync by GameManager.MovePiece, the MapEditor, and
    // MapSaveLoad.SyncPieceLists; Piece.OnDestroy (via GameManager.UnregisterPiece) removes destroyed pieces.
    public List<PieceBase> Pieces = new List<PieceBase>();
    
    // Port objects placed for this territory with the MapEditor. Not parented to the territory (see FromData).
    public List<GameObject> Ports = new List<GameObject>();

    // Saved form of a territory, written by ToData and applied by FromData. MapSaveLoad stores a list of these
    // in the map JSON file, using Unity's JsonUtility (hence [Serializable] and public fields).
    // Neighbors are written by name (direct references would be cyclic), and pieces are
    // written as plain data since Piece components can only live on GameObjects.
    [Serializable]
    public class TerritoryData
    {
        // Hierarchy path of the territory's GameObject (e.g. "WorldMap/Curve.048"). It doesn't change when the
        // territory's values are edited, so it is used to find the territory again when loading.
        public string SceneObject;

        // Copies of the Territory fields of the same name.
        public string UUID;
        
        public string Name;
        
        public int IPC;
        
        public bool IsNeutral;
        
        public string player;
        
        // Names of the neighboring territories; FromData looks them up among the Territories in the scene.
        public List<string> Neighbors = new List<string>();
        
        // One entry per piece on the territory, with its prefab name, transform, and stats.
        public List<PieceData> Pieces = new List<PieceData>();
    }

    // Saved form of one Piece from Territory.Pieces. The fields mirror Piece's public fields; FromData copies
    // them back onto the recreated piece.
    [Serializable]
    public class PieceData
    {
        public string UUID;
        // Name of the piece's GameObject, used to find the prefab it was made from when loading.
        public string Prefab;

        // World-space position and rotation of the piece when it was saved; used where the piece is recreated.
        public Vector3 Position;
        
        public Quaternion Rotation = Quaternion.identity;
        // Piece stats; see Piece for what each one means.
        
        public int Cost;
        public int Move;
        public int Attack;
        public int Defense;
        public int HitsRequiredToDestroy;
        
        // Owning country; FromData skips pieces whose owner isn't in Piece.ValidOwners.
        public Countries Owner;
        
        // Land, Air, or Sea; a missing or invalid value is replaced with Land by FromData.
        public PieceType PieceType;
    }

    /// <summary>
    /// Returns the values of all public attributes as JSON. Neighbors are written by name.
    /// </summary>
    public string ToJson(bool prettyPrint = true)
    {
        return JsonUtility.ToJson(ToData(), prettyPrint);
    }

    /// <summary>
    /// Returns the values of all public attributes as serializable data. Neighbors are written by name.
    /// </summary>
    public TerritoryData ToData()
    {
        // Called by MapSaveLoad.Save for every territory in the scene.
        var data = new TerritoryData
        {
            SceneObject = HierarchyPath(),
            UUID = UUID,
            Name = Name,
            IPC = IPC,
            IsNeutral = IsNeutral,
            player = player
        };

        // Null entries (e.g. a neighbor deleted from the scene) are skipped rather than saved.
        if (Neighbors != null)
        {
            foreach (var neighbor in Neighbors)
            {
                if (neighbor != null)
                    data.Neighbors.Add(neighbor.Name);
            }
        }

        if (Pieces != null)
        {
            foreach (var piece in Pieces)
            {
                if (piece == null)
                    continue;

                // The GameObject name doubles as the prefab name, since placed pieces keep their prefab's name.
                data.Pieces.Add(new PieceData
                {
                    UUID = piece.UUID,
                    Prefab = piece.gameObject.name,
                    Position = piece.transform.position,
                    Rotation = piece.transform.rotation,
                    Cost = piece.Cost,
                    Move = piece.Move,
                    Attack = piece.Attack,
                    Defense = piece.Defense,
                    HitsRequiredToDestroy = piece.HitsRequiredToDestroy,
                    Owner = piece.Owner,
                    PieceType = piece.PieceType,
                });
            }
        }

        return data;
    }

    /// <summary>
    /// Sets all public attributes from JSON produced by <see cref="ToJson"/>.
    /// The optional callbacks are passed on to <see cref="FromData"/>.
    /// </summary>
    public void FromJson(string json, Func<PieceData, PieceBase> createPiece = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning($"{nameof(Territory)}.{nameof(FromJson)} received empty JSON.", this);
            return;
        }

        var data = JsonUtility.FromJson<TerritoryData>(json);
        if (data != null)
        {
            FromData(data, createPiece);
        }
    }

    /// <summary>
    /// Sets all public attributes from data produced by <see cref="ToData"/>.
    /// Neighbor names are resolved against Territory objects currently in the scene;
    /// names with no matching Territory are skipped with a warning.
    /// Each piece is made by <paramref name="createPiece"/> (e.g. instantiating its prefab); without one, a bare
    /// GameObject with a Piece component is created. The piece's saved stats are then applied to it.
    /// Called by MapSaveLoad.Load, which passes callbacks that instantiate the matching prefabs by name.
    /// </summary>
    public void FromData(TerritoryData data, Func<PieceData, PieceBase> createPiece = null)
    {
        // The new player/IsNeutral values are picked up by Update, which recolors the territory.
        UUID = data.UUID;
        Name = data.Name;
        IPC = data.IPC;
        IsNeutral = data.IsNeutral;
        if (IsValidPlayer(data.player))
        {
            player = data.player;
        }
        else
        {
            Debug.LogWarning($"Territory '{Name}': player '{data.player}' is not valid; using '{NoPlayer}'.", this);
            player = NoPlayer;
        }

        ClearPieces();

        if (data.Pieces != null)
        {
            foreach (var pieceData in data.Pieces)
            {
                if (pieceData == null)
                {
                    continue;
                }

                // Not parented to the territory, whose scale would distort the piece.
                var piece = createPiece != null ? createPiece(pieceData) : CreateBarePiece(pieceData);
                if (piece == null)
                {
                    continue;
                }

                piece.UUID = pieceData.UUID;
                piece.Cost = pieceData.Cost;
                piece.Move = pieceData.Move;
                piece.Attack = pieceData.Attack;
                piece.Defense = pieceData.Defense;
                piece.HitsRequiredToDestroy = pieceData.HitsRequiredToDestroy;
                piece.Owner = pieceData.Owner;
                piece.CurrentTerritory = this;

                Pieces.Add(piece);
            }
        }


        Neighbors = new List<Territory>();
        if (data.Neighbors != null && data.Neighbors.Count > 0)
        {
            // Index every other territory in the scene by Name once, rather than searching per neighbor.
            // If two territories share a name, the last one found wins.
            var territoriesByName = new Dictionary<string, Territory>();
            foreach (var territory in FindObjectsByType<Territory>(FindObjectsSortMode.None))
            {
                if (territory != this && !string.IsNullOrEmpty(territory.Name))
                    territoriesByName[territory.Name] = territory;
            }

            foreach (var neighborName in data.Neighbors)
            {
                if (territoriesByName.TryGetValue(neighborName, out var neighbor))
                    Neighbors.Add(neighbor);
                else
                    Debug.LogWarning($"Territory '{Name}': neighbor '{neighborName}' was not found in the scene.", this);
            }
        }
    }

    /// <summary>
    /// Returns this GameObject's path in the scene hierarchy, e.g. "WorldMap/Curve.048".
    /// Saved as TerritoryData.SceneObject by ToData; MapSaveLoad.Load compares it first when matching saved data.
    /// </summary>
    public string HierarchyPath()
    {
        string path = gameObject.name;
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        return path;
    }


    private static PieceBase CreateBarePiece(PieceData data)
    {
        var pieceObject = new GameObject(string.IsNullOrEmpty(data.Prefab) ? "Piece" : data.Prefab);
        pieceObject.transform.SetPositionAndRotation(data.Position, data.Rotation);
        return pieceObject.AddComponent<PieceBase>();
    }

    // True if value is one of ValidPlayers. Used by OnValidate and FromData to reject unknown players.
    public static bool IsValidPlayer(string value)
    {
        return Array.IndexOf(ValidPlayers, value) >= 0;
    }

    // Resets an invalid player typed into the Inspector to "None".
    // Unity calls OnValidate in the Editor when the script loads or an Inspector value changes.
    private void OnValidate()
    {
        if (!IsValidPlayer(player))
        {
            Debug.LogWarning($"Territory '{Name}': '{player}' is not a valid player. Valid players are: {string.Join(", ", ValidPlayers)}.", this);
            player = NoPlayer;
        }
    }

    // ---------- Color ----------

    // The player and IsNeutral values the current color was applied for. Update compares against them so the
    // renderer is only recolored when one of them actually changes.
    private string coloredPlayer;
    private bool coloredNeutral;
    // False until ApplyColor has run once, so the first Update always colors the territory.
    private bool colorApplied;

    // Runs when the component is enabled, in Edit Mode as well as Play Mode ([ExecuteAlways]).
    private void OnEnable()
    {
        ApplyColor();
    }

    // Recolor whenever player or IsNeutral changes, whether from the Inspector, the Map Editor, or loading.
    // In Edit Mode, Unity only calls Update when something in the scene changes, which is enough here.
    private void Update()
    {
        if (!colorApplied || player != coloredPlayer || IsNeutral != coloredNeutral)
            ApplyColor();
    }

    /// <summary>
    /// The color this territory should be: its player's country color, or light gray if it is neutral or has no player.
    /// </summary>
    public Color TerritoryColor()
    {
        if (!IsNeutral && player != NoPlayer && CountryColors.TryGetValue(player, out Color color))
            return color;
        return NeutralColor;
    }

    /// <summary>
    /// Colors this territory's MeshRenderer to match <see cref="TerritoryColor"/>. A MaterialPropertyBlock is
    /// used, so the shared (imported) material is left unchanged and nothing extra is saved in the scene.
    /// </summary>
    public void ApplyColor()
    {
        // Recorded first, so a territory without a usable renderer isn't retried every frame.
        coloredPlayer = player;
        coloredNeutral = IsNeutral;
        colorApplied = true;

        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null || meshRenderer.sharedMaterial == null)
            return;

        Color color = TerritoryColor();
        var block = new MaterialPropertyBlock();
        // Start from the renderer's current block so any other per-renderer overrides are kept.
        meshRenderer.GetPropertyBlock(block);
        // Set whichever color property the shader has, so URP and built-in materials both work.
        if (meshRenderer.sharedMaterial.HasProperty(BaseColorId))
            block.SetColor(BaseColorId, color);
        if (meshRenderer.sharedMaterial.HasProperty(ColorId))
            block.SetColor(ColorId, color);
        meshRenderer.SetPropertyBlock(block);
    }

    // Only unlists the current pieces; destroying their GameObjects is left to the caller.
    // Called by FromData before it recreates the saved pieces (MapSaveLoad.Load destroys the old ones first).
    private void ClearPieces()
    {
        Pieces = new List<PieceBase>();
    }
}
