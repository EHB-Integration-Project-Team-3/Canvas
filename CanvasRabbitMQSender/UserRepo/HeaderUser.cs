using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQSender.UserRepo
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
