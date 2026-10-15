using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using GamePieces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

// No hand-maintained list of TerritoryModel fields: Json.NET discovers new members.
public static class TerritoryMapJson
{
    public class MapData
    {
        public int Version = 1;
        public List<Entry> Children = new List<Entry>();
    }

    public class Entry
    {
        public string Name;
        // Sibling indices disambiguate duplicate names at any hierarchy depth.
        public string Path;
        public Territory Territory;
        public bool NeighborsManuallyEdited;
    }

    // Ignore only old outline-only entries absent from the real map. New exports
    // exclude owned helper objects by identity, rather than by their names.
    // Called by InfoChecker.ImportFromPath in InfoChecker.cs. Check paths, names, and territory presence
    // against the live map; return accepted entries, ignoring legacy outline helpers.
    public static List<Entry> ValidateEntries(List<Entry> current, List<Entry> imported)
    {
        var byPath = current.ToDictionary(entry => entry.Path);
        var exportedByPath = new Dictionary<string, Entry>();
        foreach (var entry in imported)
        {
            if (entry == null || entry.Path == null || exportedByPath.ContainsKey(entry.Path))
                throw new InvalidDataException("The file contains missing or duplicate map paths.");
            exportedByPath.Add(entry.Path, entry);
        }
        var accepted = new List<Entry>();
        foreach (var entry in imported)
        {
            if (!byPath.TryGetValue(entry.Path, out var target))
            {
                int slash = entry.Path.LastIndexOf('/');
                string parentPath = slash < 0 ? "" : entry.Path.Substring(0, slash);
                if (entry.Territory == null &&
                    (entry.Name == "Selection mask" || entry.Name == "Selection border" || entry.Name == "Selection outline") &&
                    byPath.TryGetValue(parentPath, out var parent) && parent.Territory != null &&
                    exportedByPath.TryGetValue(parentPath, out var exportedParent) && exportedParent.Name == parent.Name)
                    continue;
                throw new InvalidDataException("No map object matches '" + entry.Name + "' at path " + entry.Path + ".");
            }
            if (target.Name != entry.Name)
                throw new InvalidDataException("Map path " + entry.Path + " expects '" + target.Name + "', but the file contains '" + entry.Name + "'.");
            if ((target.Territory == null) != (entry.Territory == null))
                throw new InvalidDataException("Territory data does not match the component on '" + entry.Name + "'.");
            accepted.Add(entry);
        }
        if (accepted.Count != current.Count)
            throw new InvalidDataException("The file is missing map children.");
        return accepted;
    }

    // Used by InfoChecker.cs for territory editing and map import/export. Create JSON settings that
    // preserve model references, replace collections, and restrict model types.
    public static JsonSerializerSettings Settings => new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = new ModelBinder(),
        MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        MissingMemberHandling = MissingMemberHandling.Error,
        MaxDepth = 128
    };

    // Import may instantiate only types already defined in this game's model assembly.
    private class ModelBinder : ISerializationBinder
    {
        private static readonly Dictionary<string, Type> Allowed = typeof(Territory).Assembly
            .GetTypes().Where(t => t.IsPublic && !t.IsAbstract && !t.ContainsGenericParameters &&
                (t.Namespace == "Models" || t.Namespace == "Assets.Models"))
            .ToDictionary(t => t.FullName, t => t);

        // Called by Json.NET during deserialization through Settings, including InfoChecker imports and edits.
        // Resolve only allowed concrete model types; reject other names.
        public Type BindToType(string assemblyName, string typeName)
        {
            if (Allowed.TryGetValue(typeName, out Type type)) return type;
            throw new JsonSerializationException("Unsupported model type: " + typeName);
        }

        // Called by Json.NET when writing type metadata through Settings for InfoChecker exports and JSON
        // display/edit normalization. Write the full type name without an assembly name.
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.FullName;
        }
    }
}
