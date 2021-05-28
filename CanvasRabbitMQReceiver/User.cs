using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQReceiver
{
    [XmlRoot(ElementName = "user")]
    public class User
    {

        public User() { }

        public User(int id, string sortable_name, string email, string role, DateTime createdAt, DateTime updatedAt, bool deleted)
        {
            Id = id;
            Sortable_name = sortable_name;
            Email = email;
            Role = role;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Deleted = deleted;
            EntityVersion = 12;
            Header = new HeaderUser();
            Header.Source = "CANVAS";
        }


        [XmlIgnore]
        public int Id { get; set; }

        [XmlIgnore]
        public string Sortable_name { get; set; }

        [XmlElement("lastName")]
        public string Lastname { get; set; }

        [XmlElement("firstName")]
        public string Firstname { get; set; }

        [XmlElement("email")]
        public string Email { get; set; }

        [XmlElement("role")]
        public string Role { get; set; }

        [XmlElement("uuid")]
        public string UUID { get; set; }

        [XmlIgnore]
        public DateTime CreatedAt { get; set; }

        [XmlIgnore]
        public DateTime UpdatedAt { get; set; }

        [XmlIgnore]
        public bool Deleted { get; set; }

        [XmlElement("header")]
        public HeaderUser Header { get; set; }

        [XmlElement("entityVersion")]
        public int EntityVersion { get; set; }
    }
}

