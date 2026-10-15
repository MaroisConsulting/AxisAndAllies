using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace Assets.Scripts
{
    public class Serialization
    {
        private static readonly JsonSerializerSettings _settings = new()
        {
            Formatting = Newtonsoft.Json.Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,          // preserves derived types (e.g. PieceBase subclasses)
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        #region String
        public static string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, typeof(T), _settings);
        }

        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
        #endregion

        #region File
        public static async Task SerializeToFileAsync<T>(T value, string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(path, Serialize(value));
        }

        public static async Task<T> DeserializeFromFileAsync<T>(string path)
        {
            if (!File.Exists(path))
                return default;

            var json = await File.ReadAllTextAsync(path);
            return Deserialize<T>(json);
        }
        #endregion    
    }
}
