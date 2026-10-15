using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class InfoChecker : MonoBehaviour
{
    public TMP_InputField territoryInput;
    public TMP_Text statusText;
    public GameObject map;
    public Button importButton;
    public Button exportButton;
    public Color outlineColor = Color.yellow;
    [Min(0.001f)] public float outlineWidth = 0.03f;

    [Min(0f), Tooltip("Maximum mouse movement in pixels allowed for a selection click.")]
    public float selectionDragThreshold = 5f;

    private bool clickPending;
    private Territory pressedTerritory;
    private Vector2 pressPosition;
    private Camera viewCamera;
    private Territory selected;
    private TerritorySelectionOutline outline;
    private bool browsing;
    public bool IsBrowsing => browsing;
    private string folder;
    private string[] directories = Array.Empty<string>();
    private string[] files = Array.Empty<string>();
    private string browserMessage = "";
    private Vector2 scroll;
    private string lastDisplay;
    private bool invalidDraft;

    // Unity calls this when the component loads. Cache the camera and configure the JSON input field as
    // plain, multiline text.
    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
        if (territoryInput != null)
        {
            territoryInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            territoryInput.contentType = TMP_InputField.ContentType.Standard;
            territoryInput.richText = false;
            if (territoryInput.textComponent != null) territoryInput.textComponent.richText = false;
        }
    }
    // Unity calls this when the component becomes enabled. Connect input editing and the import/export
    // buttons to their handlers.
    private void OnEnable()
    {
        if (territoryInput != null) territoryInput.onEndEdit.AddListener(ApplyTerritoryEdit);
        if (importButton != null) importButton.onClick.AddListener(Import);
        if (exportButton != null) exportButton.onClick.AddListener(Export);
    }
    // Unity calls this when the component becomes disabled. Remove UI listeners, close the browser, and
    // clear the pending click and selection.
    private void OnDisable()
    {
        if (territoryInput != null) territoryInput.onEndEdit.RemoveListener(ApplyTerritoryEdit);
        if (importButton != null) importButton.onClick.RemoveListener(Import);
        if (exportButton != null) exportButton.onClick.RemoveListener(Export);
        browsing = false;
        CancelSelectionClick();
        Select(null);
    }
    // Unity calls this each frame. Refresh the JSON display and select territories on completed clicks,
    // ignoring UI interaction and drags.
    private void Update()
    {
        RefreshDisplay();
        Mouse mouse = Mouse.current;
        if (browsing || mouse == null ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            CancelSelectionClick();
            return;
        }

        Vector2 position = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            pressedTerritory = TerritoryAt(position);
            pressPosition = position;
            clickPending = true;
        }
        if (!clickPending) return;

        float threshold = Mathf.Max(0f, selectionDragThreshold);
        if ((position - pressPosition).sqrMagnitude > threshold * threshold)
        {
            // Once a drag starts, returning to the press position is still a drag.
            CancelSelectionClick();
            return;
        }
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            Territory releasedTerritory = TerritoryAt(position);
            if (releasedTerritory == pressedTerritory)
                Select(releasedTerritory); // A click on empty space clears selection.
            CancelSelectionClick();
        }
        else if (!mouse.leftButton.isPressed)
        {
            CancelSelectionClick();
        }
    }

    // Called by Update for mouse press and release positions. Raycast from the camera and return the hit
    // territory, or null for empty space.
    private Territory TerritoryAt(Vector2 position)
    {
        Ray ray = viewCamera.ScreenPointToRay(position);
        return Physics.Raycast(ray, out RaycastHit hit)
            ? hit.collider.GetComponentInParent<Territory>() : null;
    }

    // Called by Update, OnDisable, and OnApplicationFocus. Forget the pending mouse press so it cannot
    // become a selection click.
    private void CancelSelectionClick()
    {
        clickPending = false;
        pressedTerritory = null;
    }

    // Unity calls this when application focus changes. Cancel a pending selection click when focus is
    // lost.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) CancelSelectionClick();
    }

    // Called by Update and OnDisable. Hide the previous outline, show the new territory outline, and reset
    // the JSON display; null clears selection.
    private void Select(Territory territory)
    {
        if (outline != null) outline.SetVisible(false);
        selected = territory;
        outline = null;
        if (selected != null)
        {
            selected.FindNeighbors();
            outline = selected.GetComponent<TerritorySelectionOutline>();
            if (outline == null) outline = selected.gameObject.AddComponent<TerritorySelectionOutline>();
            outline.Show(outlineColor, outlineWidth);
        }
        invalidDraft = false;
        lastDisplay = null;
        RefreshDisplay();
    }
    // Called by Update, Select, and ImportFromPath. Display the selected model as JSON without overwriting
    // an active edit or invalid draft.
    private void RefreshDisplay()
    {
        if (territoryInput == null) return;
        territoryInput.interactable = selected != null && selected.model != null;
        // Never overwrite an in-progress edit during Update.
        if (territoryInput.isFocused || invalidDraft) return;
        string json = selected == null || selected.model == null ? "" :
            JsonConvert.SerializeObject(selected.model, DisplaySettings());
        if (json == lastDisplay) return;
        territoryInput.SetTextWithoutNotify(json);
        lastDisplay = json;
    }
    // Registered by OnEnable as the input field's end-edit handler. Parse and normalize the edited JSON
    // before replacing the selected model; preserve invalid drafts.
    public void ApplyTerritoryEdit(string json)
    {
        if (selected == null || json == lastDisplay) return;
        try
        {
            var draft = JObject.Parse(json);
            JToken neighborNames = draft[nameof(Models.TerritoryModel.Neighbors)];
            draft.Remove(nameof(Models.TerritoryModel.Neighbors));
            var updated = JsonConvert.DeserializeObject<Models.TerritoryModel>(draft.ToString(), TerritoryMapJson.Settings);
            if (updated == null) throw new InvalidDataException("Enter a territory JSON object.");
            updated.Neighbors = neighborNames == null ? selected.model.Neighbors : ResolveNeighbors(neighborNames);
            // Validate before replacing the live model.
            string normalized = JsonConvert.SerializeObject(updated, DisplaySettings());
            bool neighborsChanged = !SameNeighbors(selected.model.Neighbors, updated.Neighbors);
            selected.model = updated;
            if (neighborsChanged) selected.NeighborsManuallyEdited = true;
            invalidDraft = false;
            lastDisplay = normalized;
            territoryInput.SetTextWithoutNotify(normalized);
            Report("Updated " + selected.name + ". Export to retain changes after Play Mode.");
        }
        catch (Exception e)
        {
            // Keep the invalid draft visible so the user can correct it.
            invalidDraft = true;
            Report("Edit failed: " + e.Message);
        }
    }
    private static bool SameNeighbors(List<Models.TerritoryModel> first, List<Models.TerritoryModel> second)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first == null || second == null || first.Count != second.Count) return false;
        for (int i = 0; i < first.Count; i++)
            if (!ReferenceEquals(first[i], second[i])) return false;
        return true;
    }
    // Only the UI uses this resolver; map import/export retains full model data.
    private static JsonSerializerSettings DisplaySettings()
    {
        var settings = TerritoryMapJson.Settings;
        settings.ContractResolver = new NeighborNameResolver();
        return settings;
    }

    private class NeighborNameResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization serialization)
        {
            JsonProperty property = base.CreateProperty(member, serialization);
            if (member.DeclaringType == typeof(Models.TerritoryModel) && member.Name == nameof(Models.TerritoryModel.Neighbors))
                property.Converter = new NeighborNameConverter();
            return property;
        }
    }

    private class NeighborNameConverter : JsonConverter
    {
        public override bool CanConvert(Type type) => type == typeof(List<Models.TerritoryModel>);
        public override bool CanRead => false;
        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
            => throw new NotSupportedException();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            writer.WriteStartArray();
            foreach (var neighbor in (List<Models.TerritoryModel>)value)
                writer.WriteValue(neighbor?.Name);
            writer.WriteEndArray();
        }
    }

    private List<Models.TerritoryModel> ResolveNeighbors(JToken names)
    {
        if (names.Type == JTokenType.Null) return null;
        if (!(names is JArray array)) throw new InvalidDataException("Neighbors must be an array of territory names.");
        var candidates = new HashSet<Models.TerritoryModel>();
        candidates.Add(selected.model);
        if (selected.model.Neighbors != null)
            foreach (var neighbor in selected.model.Neighbors) if (neighbor != null) candidates.Add(neighbor);
        if (map != null)
            foreach (var territory in map.GetComponentsInChildren<Territory>(true))
                if (territory.model != null) candidates.Add(territory.model);
        var neighbors = new List<Models.TerritoryModel>();
        foreach (JToken token in array)
        {
            if (token.Type == JTokenType.Null) { neighbors.Add(null); continue; }
            if (token.Type != JTokenType.String) throw new InvalidDataException("Each neighbor must be a territory name.");
            string name = token.Value<string>();
            int index = neighbors.Count;
            // Retain the exact reference for unchanged entries, even when names are duplicated.
            if (selected.model.Neighbors != null && index < selected.model.Neighbors.Count &&
                selected.model.Neighbors[index]?.Name == name)
            { neighbors.Add(selected.model.Neighbors[index]); continue; }
            Models.TerritoryModel match = null;
            foreach (var candidate in candidates)
            {
                if (candidate.Name != name) continue;
                if (match != null) throw new InvalidDataException("Ambiguous neighbor name: " + name);
                match = candidate;
            }
            if (match == null) throw new InvalidDataException("Unknown neighbor name: " + name);
            neighbors.Add(match);
        }
        return neighbors;
    }
    // Called by Export, Import, and ImportFromPath. Check that the map is assigned and collect its
    // descendants for export or import validation.
    private List<TerritoryMapJson.Entry> CollectChildren()
    {
        if (map == null) throw new InvalidOperationException("Assign the Map GameObject in the Inspector.");
        var entries = new List<TerritoryMapJson.Entry>();
        Collect(map.transform, "", entries);
        return entries;
    }
    // Called by CollectChildren and recursively by itself. Record descendant names, sibling-index paths,
    // and territory models, skipping owned outline helpers.
    private static void Collect(Transform parent, string prefix, List<TerritoryMapJson.Entry> entries)
    {
        TerritorySelectionOutline selectionOutline = parent.GetComponent<TerritorySelectionOutline>();
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (selectionOutline != null && selectionOutline.OwnsTransform(child)) continue;
            string path = prefix + i;
            Territory component = child.GetComponent<Territory>();
            entries.Add(new TerritoryMapJson.Entry
            {
                Name = child.name,
                Path = path,
                Territory = component != null ? component.model : null,
                NeighborsManuallyEdited = component != null && component.NeighborsManuallyEdited
            });
            Collect(child, path + "/", entries);
        }
    }
    // Registered by OnEnable as the export button handler. Write the collected map data to a uniquely
    // named JSON file in Downloads and report the result.
    public void Export()
    {
        try
        {
            var data = new TerritoryMapJson.MapData { Children = CollectChildren() };
            string json = JsonConvert.SerializeObject(data, TerritoryMapJson.Settings);
            string downloads = DesktopDownloads.GetPath();
            Directory.CreateDirectory(downloads);
            string path = Path.Combine(downloads, "Territories-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".json");
            File.WriteAllText(path, json);
            Report("Exported to " + path);
        }
        catch (Exception e) { Report("Export failed: " + e.Message); }
    }
    // Registered by OnEnable as the import button handler. Validate the map assignment and open the JSON
    // file browser at Downloads.
    public void Import()
    {
        try
        {
            CollectChildren(); // Validate configuration before opening the browser.
            Navigate(DesktopDownloads.GetPath());
            browsing = true;
        }
        catch (Exception e) { Report("Import failed: " + e.Message); }
    }
    // Called by OnGUI when a JSON file is chosen. Validate the file against the map before applying
    // models, then refresh the display and report the result.
    public void ImportFromPath(string path)
    {
        try
        {
            var current = CollectChildren();
            var data = JsonConvert.DeserializeObject<TerritoryMapJson.MapData>(File.ReadAllText(path), TerritoryMapJson.Settings);
            if (data == null || data.Version != 1 || data.Children == null) throw new InvalidDataException("Unsupported territory JSON file.");
            var entries = TerritoryMapJson.ValidateEntries(current, data.Children);
            var changes = new List<(Territory component, Models.TerritoryModel model, bool neighborsManuallyEdited)>();
            // Validation finishes before changing any live territory.
            foreach (var entry in entries)
            {
                if (entry.Territory == null) continue;
                Transform child = map.transform;
                foreach (string index in entry.Path.Split('/')) child = child.GetChild(int.Parse(index));
                Territory component = child.GetComponent<Territory>();
                if (component == null) throw new InvalidDataException("No Territory component on " + entry.Name);
                changes.Add((component, entry.Territory, entry.NeighborsManuallyEdited));
            }
            foreach (var change in changes)
            {
                change.component.model = change.model;
                change.component.NeighborsManuallyEdited = change.neighborsManuallyEdited;
            }
            browsing = false;
            invalidDraft = false;
            lastDisplay = null;
            RefreshDisplay();
            Report("Imported " + changes.Count + " territories from " + path);
        }
        catch (Exception e) { Report("Import failed: " + e.Message); browserMessage = e.Message; }
    }
    // Called by ApplyTerritoryEdit, Export, Import, and ImportFromPath. Show feedback in the status label,
    // or log it when no label is assigned.
    private void Report(string message)
    {
        if (statusText != null) { statusText.richText = false; statusText.text = message; }
        else Debug.Log(message, this);
    }
    // Called by Import and OnGUI. List sorted subfolders and JSON files for the browser, or retain the
    // current listing and show a navigation error.
    private void Navigate(string path)
    {
        try
        {
            string fullPath = Path.GetFullPath(path);
            string[] nextDirectories = Directory.GetDirectories(fullPath);
            string[] nextFiles = Array.FindAll(Directory.GetFiles(fullPath), f => string.Equals(Path.GetExtension(f), ".json", StringComparison.OrdinalIgnoreCase));
            Array.Sort(nextDirectories); Array.Sort(nextFiles);
            folder = fullPath; directories = nextDirectories; files = nextFiles;
            scroll = Vector2.zero; browserMessage = "";
        }
        catch (Exception e) { browserMessage = e.Message; }
    }
    // Unity calls this for immediate-mode GUI events. Draw the import browser and handle folder
    // navigation, file selection, and cancellation.
    private void OnGUI()
    {
        if (!browsing) return;
        float width = Mathf.Min(700, Screen.width - 20);
        float height = Mathf.Min(550, Screen.height - 20);
        GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height), GUI.skin.box);
        GUILayout.Label("Import territory JSON");
        folder = GUILayout.TextField(folder ?? "");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Go")) Navigate(folder);
        if (GUILayout.Button("Parent folder"))
        {
            try { var parent = Directory.GetParent(folder); if (parent != null) Navigate(parent.FullName); }
            catch (Exception e) { browserMessage = e.Message; }
        }
        if (GUILayout.Button("Cancel")) browsing = false;
        GUILayout.EndHorizontal();
        GUILayout.Label(browserMessage);
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (string directory in directories) if (GUILayout.Button("[Folder] " + Path.GetFileName(directory))) { Navigate(directory); break; }
        foreach (string file in files) if (GUILayout.Button(Path.GetFileName(file))) { ImportFromPath(file); break; }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
