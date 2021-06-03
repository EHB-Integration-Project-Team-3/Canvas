using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace CanvasRabbitMQReceiver
{
    public static class XmlController
    {
        public static string SerializeToXmlString<T>(T data)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                XmlTextWriter writer = new XmlTextWriter(stream, null);
                writer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, data);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return "";
            }
        }

        public static T DeserializeXmlString<T>(string xml)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                StringReader reader = new StringReader(xml);
                return (T)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return default;
            }
        }
    }
}