using System.Collections.Generic;
using Enums;
using Models;
using Newtonsoft.Json;
using UnityEngine;

public class Territory : MonoBehaviour
{
    [TextArea(3, 12)]
    [Tooltip("JSON values for the territory. Leave blank to use the default model. Applied in Awake.")]
    public string modelString;

    public TerritoryModel model = CreateDefaultModel();

    // Saved with map entries so manual removals (including an empty list) survive import.
    public bool NeighborsManuallyEdited { get; set; }

    [Min(0.001f)]
    [Tooltip("Maximum gap in world units for a territory to count as a neighbor.")]
    public float neighborDistance = 0.03f;

    // Called only on selection by InfoChecker. Return neighboring territory GameObjects and
    // replace this model's neighbor list with their live models.
    public List<GameObject> FindNeighbors()
    {
        var neighbors = new List<GameObject>();
        if (NeighborsManuallyEdited)
        {
            // The manually edited list is authoritative; do not rediscover removed neighbors.
            if (model?.Neighbors != null)
            {
                var models = new HashSet<TerritoryModel>(model.Neighbors);
                foreach (Territory territory in UnityEngine.Object.FindObjectsByType<Territory>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (territory != this && territory.model != null && models.Contains(territory.model))
                        neighbors.Add(territory.gameObject);
            }
            return neighbors;
        }
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (model == null || meshCollider == null || !meshCollider.enabled ||
            !meshCollider.gameObject.activeInHierarchy || meshCollider.sharedMesh == null)
        {
            Debug.LogWarning("Cannot find neighbors without a model and an active MeshCollider.", this);
            return neighbors;
        }

        Mesh mesh = meshCollider.sharedMesh;
        if (!mesh.isReadable)
        {
            Debug.LogWarning("Enable Read/Write on the territory mesh to find neighbors. Existing neighbors were retained.", this);
            return neighbors;
        }

        // Capsule queries support non-convex target meshes and cover whole edges, including
        // long edges where checking only vertices would miss a neighboring territory.
        Physics.SyncTransforms();
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = meshCollider.transform.TransformPoint(vertices[i]);
        int[] triangles = mesh.triangles;
        var checkedEdges = new HashSet<ulong>();
        var found = new HashSet<Territory>();
        float distance = Mathf.Max(0.001f, neighborDistance);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            CheckNeighborEdge(triangles[i], triangles[i + 1], vertices, distance, checkedEdges, found, neighbors);
            CheckNeighborEdge(triangles[i + 1], triangles[i + 2], vertices, distance, checkedEdges, found, neighbors);
            CheckNeighborEdge(triangles[i + 2], triangles[i], vertices, distance, checkedEdges, found, neighbors);
        }

        var neighborModels = new List<TerritoryModel>(neighbors.Count);
        foreach (GameObject neighbor in neighbors)
            neighborModels.Add(neighbor.GetComponent<Territory>().model);
        model.Neighbors = neighborModels;
        return neighbors;
    }

    private void CheckNeighborEdge(int first, int second, Vector3[] vertices, float distance,
        HashSet<ulong> checkedEdges, HashSet<Territory> found, List<GameObject> neighbors)
    {
        uint low = (uint)Mathf.Min(first, second);
        uint high = (uint)Mathf.Max(first, second);
        if (!checkedEdges.Add(((ulong)low << 32) | high)) return;

        Vector3 start = vertices[first];
        Vector3 end = vertices[second];
        Collider[] hits = (start - end).sqrMagnitude < 0.00000001f
            ? Physics.OverlapSphere(start, distance, Physics.AllLayers, QueryTriggerInteraction.Collide)
            : Physics.OverlapCapsule(start, end, distance, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (Collider hit in hits)
        {
            Territory territory = hit.GetComponentInParent<Territory>();
            if (territory == null || territory == this || territory.model == null || !found.Add(territory)) continue;
            neighbors.Add(territory.gameObject);
        }
    }

    // Used by the model field initializer and Awake. Create a fresh unnamed territory with zero income and
    // empty neighbor and piece lists.
    private static TerritoryModel CreateDefaultModel() => new TerritoryModel(
        name: "Unnamed",
        ipc: 0,
        isNeutral: false,
        player: Countries.US,
        neighbors: new List<TerritoryModel>(),
        pieces: new List<PieceModelBase>());

    // Unity calls this when the component loads. Apply Inspector JSON to a fresh default model;
    // InfoChecker.cs later reads and edits the resulting model.
    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(modelString))
            return;

        try
        {
            // Populate a fresh model so omitted fields retain their defaults and
            // invalid JSON cannot partially modify the active model.
            TerritoryModel configuredModel = CreateDefaultModel();
            JsonConvert.PopulateObject(modelString, configuredModel, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                MissingMemberHandling = MissingMemberHandling.Error,
                NullValueHandling = NullValueHandling.Ignore
            });
            model = configuredModel;
        }
        catch (JsonException exception)
        {
            Debug.LogError($"Invalid modelString on {name}. Using the default territory model. {exception.Message}", this);
        }
    }
}
