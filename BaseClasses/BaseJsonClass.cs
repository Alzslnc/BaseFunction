using System.IO;
using System.Text.Json;

namespace BaseFunction
{ 
    public static class JsonStorageHelper
    {
        // Base universal settings (without AutoCAD specific converters)
        private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Loads an object from JSON (uses base settings)
        /// </summary>
        public static T GetSerializationResult<T>(string path, bool isName = false) where T : class
        {
            return GetSerializationResult<T>(path, _defaultOptions, isName);
        }

        /// <summary>
        /// Loads an object from JSON using custom settings (e.g. with custom converters)
        /// </summary>
        public static T GetSerializationResult<T>(string path, JsonSerializerOptions options, bool isName = false) where T : class
        {
            if (isName)
            {
                string assemblyDir = Path.GetDirectoryName(typeof(JsonStorageHelper).Assembly.Location);
                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    path = Path.Combine(assemblyDir, path);
                }
            }

            if (!File.Exists(path)) return null;

            try
            {
                string jsonString = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(jsonString, options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Saves an object to JSON (uses base settings)
        /// </summary>
        public static bool SetSerializationResult<T>(string path, T toSerialize, bool isName = false) where T : class
        {
            return SetSerializationResult<T>(path, toSerialize, _defaultOptions, isName);
        }

        /// <summary>
        /// Saves an object to JSON using custom settings (e.g. with custom converters)
        /// </summary>
        public static bool SetSerializationResult<T>(string path, T toSerialize, JsonSerializerOptions options, bool isName = false) where T : class
        {
            if (toSerialize == null) return false;

            if (isName)
            {
                string assemblyDir = Path.GetDirectoryName(toSerialize.GetType().Assembly.Location);
                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    path = Path.Combine(assemblyDir, path);
                }
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonString = JsonSerializer.Serialize(toSerialize, options);
                File.WriteAllText(path, jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
