using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQReceiver
{
    class Heartbeat
    {
        public Heartbeat()
        {
            Header = new HeaderHeartbeat();
            Header.Source = "CANVAS";
        }

        [XmlElement("header")]
        public HeaderHeartbeat Header { get; set; }

        [XmlElement("timeStamp")]
        public DateTime TimeStamp { get; set; }


    }


    [Serializable]
    public class HeaderHeartbeat
    {

        [XmlElement("status")]
        public string Status { get; set; }

        [XmlElement("source")]
        public string Source { get; set; }
    }
}

