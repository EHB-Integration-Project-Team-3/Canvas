using System;
using System.Xml.Serialization;

namespace CanvasRabbitMQReceiver.UserRepo
{
    [Serializable]
    class HeaderUser
    {
        [XmlElement("method")]
        public string Method { get; set; }

        [XmlElement("source")]
        public string Source { get; set; }
    }
}

