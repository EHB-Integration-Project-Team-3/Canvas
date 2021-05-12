using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQSender
{
    [XmlRoot(ElementName = "event")]
    public class Event
    {
        public Event(int id, string title, string description, string locationName, string locationAddress, DateTime startAt, DateTime endAt, int contextId, string contextType, DateTime createdAt, DateTime updatedAt) 
        {
            Id = id;
            Title = title;
            Description = description;
            LocationName = locationName;
            LocationAddress = locationAddress;
            StartAt = startAt;
            EndAt = endAt;
            ContextId = contextId;
            ContextType = contextType;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            EntityVersion = 15;
            Header = new Header();
            Header.Source = "CANVAS";
        }
        public Event()
        {
            EntityVersion = 15;
            Header = new Header();
            Header.Source = "CANVAS";
        }

        [XmlElement("header")]
        public Header Header { get; set; }
        [XmlIgnore]
        public int Id { get; set; }
        [XmlElement("uuid")]
        public string UUID { get; set; }

        [XmlElement("entityVersion")]
        public int EntityVersion { get; set; }
        [XmlElement("title")]
        public string Title { get; set; }
        [XmlElement("organiserId")]
        public string OrganiserId { get; set; }
        [XmlElement("description")]
        public string Description { get; set; }
        [XmlElement("start")]
        public DateTime StartAt { get; set; }
        [XmlElement("end")]
        public DateTime EndAt { get; set; }
        [XmlIgnore]
        public string LocationName { get; set; }
        [XmlElement("location")]
        public string LocationAddress { get; set; }
        [XmlIgnore]
        public int ContextId { get; set; }
        [XmlIgnore]
        public string ContextType { get; set; }
        [XmlIgnore]
        public DateTime CreatedAt { get; set; }
        [XmlIgnore]
        public DateTime UpdatedAt { get; set; }


    }


    [Serializable]
    public class Header
    {
        
        [XmlElement("method")]
        public string Method { get; set; }

        [XmlElement("source")]
        public string Source { get; set; }
    }
}