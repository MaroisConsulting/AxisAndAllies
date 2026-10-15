using Assets.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class SettingsManager
    {
        private static readonly string _path = Path.Combine(Application.persistentDataPath, "aasettings.json");

        public static GameSettings Current { get; private set; } = new();

        public static async Task LoadAsync()
        {
            try
            {
                Current = await Serialization.DeserializeFromFileAsync<GameSettings>(_path) ?? new();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Settings file unreadable, using defaults: {ex.Message}");
                Current = new();
            }
        }

        public static Task SaveAsync()
        {
            return Serialization.SerializeToFileAsync(Current, _path);
        }
    }
}
