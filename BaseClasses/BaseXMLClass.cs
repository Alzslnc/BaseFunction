using System;
using System.IO;
using System.Xml.Serialization;

namespace BaseFunction
{
    public static class BaseXMLClass
    {        
        public static object GetSerialisationResult(string path, Type type, bool isName = false)
        {
            if (isName) path = Path.Combine(new FileInfo(type.Assembly.Location).DirectoryName, path);          
            if (!File.Exists(path)) return null;
            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    return new XmlSerializer(type).Deserialize(reader);
                }
            }
            catch { return null; }
        }
        public static bool SetSerialisationResult(string path, object toSerialise, bool isName = false)
        {
            Type type = toSerialise.GetType();
            if (isName) path = Path.Combine(new FileInfo(type.Assembly.Location).DirectoryName, path);
            try
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    new XmlSerializer(type).Serialize(writer, toSerialise);
                }
                return true;
            }
            catch { return false; }
        }
    }
}
