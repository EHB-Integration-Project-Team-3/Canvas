using Microsoft.VisualStudio.TestTools.UnitTesting;
using CanvasRabbitMQSender;
using CanvasRabbitMQSender.UserRepo;
using System;

namespace CanvasRabitMQSenderTests
{
    [TestClass]
    public class UnitTest1
    {
        private Event objectEvent = new Event()
        {
            Id = 1,
            Title = "EventTitle",
            Description = "EventDesc",
            LocationName = "locationName",
            LocationAddress = "LocationAddress",
            StartAt = (new DateTime(2021, 05, 28, 20, 45, 00)),
            EndAt = (new DateTime(2021, 05, 28, 21, 45, 00)),
            EntityVersion = 1,
            ContextType = "Course-Section",
            CreatedAt = (new DateTime(2021, 05, 27, 21, 45, 00)),
            UpdatedAt = (new DateTime(2021, 05, 27, 21, 45, 00)),
            ContextId = 1,
            UUID = "30dd879c-ee2f-11db-8314-0800200c9a62",
            OrganiserId = "30dd879c-ee2f-11db-8314-0800200c9a61",
            Header = new HeaderEvent()
            {
                Method = "CREATE",
                Source = "CANVAS"
            }
        };
            
        private User objectUser = new User() { 
            Header = new HeaderUser() { 
                Method = "CREATE", 
                Source = "CANVAS" 
            }, 
            Lastname = "Horemans", 
            Firstname = "Sander", 
            EntityVersion = 1, 
            Role = "lecturer", 
            Email = "sanderhoremans@ipwt3.onmicrosoft.com", 
            UUID = "30dd879c-ee2f-11db-8314-0800200c9a61"
        }; 
        private Heartbeat objectHeartbeat = new Heartbeat() {
            TimeStamp = new System.DateTime(2021,05,28,20,45,00), 
            Header = new HeaderHeartbeat() { 
                Status = "ONLINE", 
                Source = "CANVAS"
            } 
        };
        private string xmlHeartbeat = "<?xml version=\"1.0\"?>\r\n" +
            "<heartbeat xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  " +
            "<header>\r\n    " +
            "<status>ONLINE</status>\r\n    " +
            "<source>CANVAS</source>\r\n  " +
            "</header>\r\n  " +
            "<timeStamp>2021-05-28T20:45:00</timeStamp>\r\n" +
            "</heartbeat>";
        private string xmlUser = "<?xml version=\"1.0\"?>\r\n" +
            "<user xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  " +
            "<header>\r\n    " +
            "<method>CREATE</method>\r\n    " +
            "<source>CANVAS</source>\r\n  " +
            "</header>\r\n  " +
            "<uuid>30dd879c-ee2f-11db-8314-0800200c9a61</uuid>\r\n  " +
            "<entityVersion>1</entityVersion>\r\n  " +
            "<lastName>Horemans</lastName>\r\n  " +
            "<firstName>Sander</firstName>\r\n  " +
            "<emailAddress>sanderhoremans@ipwt3.onmicrosoft.com</emailAddress>\r\n  " +
            "<role>lecturer</role>\r\n</user>";
        private string xmlEvent = "<?xml version=\"1.0\"?>\r\n" +
            "<event xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  " +
            "<header>\r\n    " +
            "<method>CREATE</method>\r\n    " +
            "<source>CANVAS</source>\r\n  " +
            "</header>\r\n  " +
            "<uuid>30dd879c-ee2f-11db-8314-0800200c9a62</uuid>\r\n  " +
            "<entityVersion>1</entityVersion>\r\n  " +
            "<title>EventTitle</title>\r\n  " +
            "<organiserId>30dd879c-ee2f-11db-8314-0800200c9a61</organiserId>\r\n  " +
            "<description>EventDesc</description>\r\n  " +
            "<start>2021-05-28T20:45:00</start>\r\n  " +
            "<end>2021-05-28T21:45:00</end>\r\n  " +
            "<location>LocationAddress</location>\r\n" +
            "</event>";

        [TestMethod]
        public void TestEventToXML()
        {
            string xml = XmlController.SerializeToXmlString<Event>(objectEvent);
            Assert.AreEqual(xml.Replace("\n","").Replace("\r", "").Replace(" ", ""), xmlEvent.Replace("\n", "").Replace("\r", "").Replace(" ", ""));
        }
        [TestMethod]
        public void TestUserToXML()
        {
            string xml = XmlController.SerializeToXmlString<User>(objectUser);
            Assert.AreEqual(xml.Replace("\n", " ").Replace("\r", "").Replace(" ", ""), xmlUser.Replace("\n", "").Replace("\r", "").Replace(" ", ""));
        }
        [TestMethod]
        public void TestHeartbeatToXML()
        {
            string xml = XmlController.SerializeToXmlString<Heartbeat>(objectHeartbeat);
            Assert.AreEqual(xml.Replace("\n", "").Replace("\r", "").Replace(" ", ""), xmlHeartbeat.Replace("\n", "").Replace("\r", "").Replace(" ", ""));
        }
        [TestMethod]
        public void TestEventFromXML()
        {
            Event desEvent = XmlController.DeserializeXmlString<Event>(xmlEvent);
            Assert.AreEqual(desEvent.ToString(), objectEvent.ToString());
        }
        [TestMethod]
        public void TestUserFromXML()
        {
            User user = XmlController.DeserializeXmlString<User>(xmlUser);
            Assert.AreEqual(user.ToString(), objectUser.ToString());
        }
        [TestMethod]
        public void TestHeartbeatFromXML()
        {
            Object heartbeat = XmlController.DeserializeXmlString<Heartbeat>(xmlHeartbeat);
            Assert.AreEqual(heartbeat.ToString(), objectHeartbeat.ToString());
        }
        [TestMethod]
        public void CheckHeartbeatXSD() {
            Assert.IsTrue(!Program.XSDValidatie(xmlHeartbeat, "heartbeat.xsd"));
        }
        [TestMethod]
        public void CheckUserXSD()
        {
            Assert.IsTrue(!Program.XSDValidatie(xmlUser, "user.xsd"));
        }
        [TestMethod]
        public void CheckEventXSD()
        {
            Assert.IsTrue(!Program.XSDValidatie(xmlEvent, "event.xsd"));
        }
        [TestMethod]
        public void CheckHeartbeatXSDvanObject()
        {
            Assert.IsTrue(!Program.XSDValidatie(XmlController.SerializeToXmlString<Heartbeat>(objectHeartbeat), "heartbeat.xsd"));
        }
        [TestMethod]
        public void CheckUserXSDvanObject()
        {
            Assert.IsTrue(!Program.XSDValidatie(XmlController.SerializeToXmlString<User>(objectUser), "user.xsd"));
        }
        [TestMethod]
        public void CheckEventXSDvanObject()
        {
            Assert.IsTrue(!Program.XSDValidatie(XmlController.SerializeToXmlString<Event>(objectEvent), "event.xsd"));
        }
    }
}
