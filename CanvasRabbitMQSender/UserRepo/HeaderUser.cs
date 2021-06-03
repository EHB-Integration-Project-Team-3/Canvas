using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQReceiver.UserRepo
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
