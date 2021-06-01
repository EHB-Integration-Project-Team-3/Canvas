using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQSender
{

    [XmlRoot(ElementName = "attendance")]
    public class Attendance
    {
        public Attendance()
        {
            Header = new HeaderAttendance();
            Header.Source = "CANVAS";
            Header.Source = "CREATE";
        }

        [XmlElement("header")]
        public HeaderAttendance Header { get; set; }
        [XmlIgnore]
        public string id { get; set; }
        [XmlElement("uuid")]
        public string Uuid { get; set; }
        [XmlElement("userId")]
        public string UserId { get; set; }
        [XmlElement("eventId")]
        public string EventId { get; set; }



    }
    [Serializable]
    public class HeaderAttendance
    {

        [XmlElement("method")]
        public string Method { get; set; }

        [XmlElement("source")]
        public string Source { get; set; }
    }
}