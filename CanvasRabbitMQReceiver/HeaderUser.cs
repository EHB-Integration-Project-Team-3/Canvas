using System;
using System.Xml.Serialization;

namespace CanvasRabbitMQReceiver
{
    [Serializable]
    public class HeaderUser
    {
        [XmlElement("method")]
        public string Method { get; set; }

        [XmlElement("source")]
        public string Source { get; set; }
    }
}

