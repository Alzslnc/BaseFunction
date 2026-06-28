using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace BaseFunction
{
    public static class BaseXMLClass
    {

        // Безопасный UTF-8 без BOM, который прочитают абсолютно все фреймворки
        private static readonly Encoding SafeEncoding = new UTF8Encoding(false);
        private static string GetSafePath(string path, bool isName)
        {
            if (!isName) return path;
            // AppContext.BaseDirectory — единственный универсальный способ для .NET Framework и .NET 8/10 (включая Single File)
            return Path.Combine(AppContext.BaseDirectory, path);
        }
        // ==========================================
        // НОВЫЕ GENERIC МЕТОДЫ (для нового кода)
        // ==========================================

        public static T GetSerialisationResult<T>(string path, bool isName = false) where T : class
        {
            path = GetSafePath(path, isName);
            if (!File.Exists(path)) return null;
            try
            {
                using (var reader = new StreamReader(path, SafeEncoding))
                using (var xmlReader = XmlReader.Create(reader))
                {
                    return new XmlSerializer(typeof(T)).Deserialize(xmlReader) as T;
                }
            }
            catch { return null; }
        }

        public static bool SetSerialisationResult<T>(string path, T toSerialise, bool isName = false) where T : class
        {
            if (toSerialise == null) return false;
            path = GetSafePath(path, isName);
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Явно передаем SafeEncoding, чтобы исключить проблемы с BOM-маркерами
                using (var writer = new StreamWriter(path, false, SafeEncoding))
                {
                    new XmlSerializer(typeof(T)).Serialize(writer, toSerialise);
                }
                return true;
            }
            catch { return false; }
        }
        public static object GetSerialisationResult(string path, Type type, bool isName = false)
        {
            path = GetSafePath(path, isName);
            if (!File.Exists(path)) return null;
            try
            {
                using (var reader = new StreamReader(path, SafeEncoding))
                using (var xmlReader = XmlReader.Create(reader))
                {
                    return new XmlSerializer(type).Deserialize(xmlReader);
                }
            }
            catch { return null; }
        }    
        public static bool SetSerialisationResult(string path, object toSerialise, bool isName = false)
        {
            if (toSerialise == null) return false;
            path = GetSafePath(path, isName);
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using (var writer = new StreamWriter(path, false, SafeEncoding))
                {
                    new XmlSerializer(toSerialise.GetType()).Serialize(writer, toSerialise);
                }
                return true;
            }
            catch { return false; }
        }   
        /// <summary>
        /// Универсальное чтение XML с использованием преднастроенного сериализатора (Generic-версия)
        /// </summary>
        public static T GetSerialisationResult<T>(string path, XmlSerializer serializer, bool isName = false) where T : class
        {
            if (serializer == null) return null;
            path = GetSafePath(path, isName);
            if (!File.Exists(path)) return null;

            try
            {
                using (var reader = new StreamReader(path))
                using (var xmlReader = XmlReader.Create(reader))
                {
                    return serializer.Deserialize(xmlReader) as T;
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// Универсальная запись XML с использованием преднастроенного сериализатора (Generic-версия)
        /// </summary>
        public static bool SetSerialisationResult<T>(string path, T toSerialise, XmlSerializer serializer, bool isName = false) where T : class
        {
            if (toSerialise == null || serializer == null) return false;
            path = GetSafePath(path, isName);
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using (var writer = new StreamWriter(path, false, SafeEncoding))
                {
                    serializer.Serialize(writer, toSerialise);
                }
                return true;
            }
            catch { return false; }
        }

        // === НОВЫЕ ЧИСТЫЕ ПЕРЕГРУЗКИ (Принимают готовый сериализатор извне) ===

        /// <summary>
        /// Универсальное чтение XML с использованием преднастроенного сериализатора
        /// </summary>
        public static object GetSerialisationResult(string path, XmlSerializer serializer, bool isName = false)
        {
            // Используем AppDomain для универсального определения базовой директории
            if (isName) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            if (!File.Exists(path)) return null;
            try
            {
                using (var reader = new StreamReader(path, SafeEncoding))
                using (XmlReader xmlReader = XmlReader.Create(reader))
                {
                    return serializer.Deserialize(xmlReader);
                }
            }
            catch { return null; }
        }

        /// <summary>
        /// Универсальная запись XML с использованием преднастроенного сериализатора
        /// </summary>
        public static bool SetSerialisationResult(string path, object toSerialise, XmlSerializer serializer, bool isName = false)
        {
            Type type = toSerialise.GetType();
            if (isName) path = Path.Combine(new FileInfo(type.Assembly.Location).DirectoryName, path);
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, toSerialise);
                }
                return true;
            }
            catch { return false; }
        }
    }
}
