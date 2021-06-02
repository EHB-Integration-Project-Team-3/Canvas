using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CanvasRabbitMQSender.UserRepo
{
    [XmlRoot(ElementName = "user")]
    public class User
    {
        
        public User()
        {
            Header = new HeaderUser();
            Header.Source = "CANVAS";
        }
        
        public User(int id, string sortable_name, string role, DateTime createdAt, DateTime updatedAt, bool deleted)
        {
            Id = id;
            Sortable_name = sortable_name;
            var naam = sortable_name.Split(' ', ',');
            Firstname = naam[naam.Length - 1];
            Lastname = naam[0];
            Email = Firstname + "." + Lastname + "@ipwt3.onmicrosoft.com";
            Role = role;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Deleted = deleted;
            EntityVersion = 1;
            Header = new HeaderUser();
            Header.Source = "CANVAS";
        }

        [XmlElement("header")]
        public HeaderUser Header { get; set; }

        [XmlElement("uuid")]
        public string UUID { get; set; }

        [XmlElement("entityVersion")]
        public int EntityVersion { get; set; }

        [XmlIgnore]
        public int Id { get; set; }

        [XmlIgnore]
        public string Sortable_name { get; set; }

        [XmlElement("lastName")]
        public string Lastname { get; set; }

        [XmlElement("firstName")]
        public string Firstname { get; set; }

        [XmlElement("emailAddress")]
        public string Email { get; set; }

        [XmlElement("role")]
        public string Role { get; set; }

        [XmlIgnore]
        public DateTime CreatedAt { get; set; }

        [XmlIgnore]
        public DateTime UpdatedAt { get; set; }

        [XmlIgnore]
        public bool Deleted { get; set; }

    }
}

