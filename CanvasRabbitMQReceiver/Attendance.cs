using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQReceiver
{
    [XmlRoot(ElementName = "Attendance")]
    public class Attendance
    {

        public Attendance() {}

        public Attendance(AttendanceHeader attendanceHeader, String uuid, String creatorId, String attendeeId, String eventId)
        {
            AttendanceHeader = attendanceHeader;
            Uuid = uuid;
            CreatorId = creatorId;
            AttendeeId = attendeeId;
            EventId = eventId;
        }

            [XmlElement("header")]
            public AttendanceHeader AttendanceHeader { get; set; }

            [XmlElement("uuid")]
            public string Uuid { get; set; }

            [XmlElement("creatorId")]
            public string CreatorId { get; set; }

            [XmlElement("attendeeId")]
            public string AttendeeId { get; set; }

            [XmlElement("eventId")]
            public string EventId { get; set; }
        
}
    
    
    [Serializable]
    public class AttendanceHeader {
        
        [XmlElement("method")]
        public string Methode { get; set;}
    }
}
