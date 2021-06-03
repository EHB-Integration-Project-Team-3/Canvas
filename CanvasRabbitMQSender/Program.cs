using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Linq;
using Npgsql;
using System.IO;
using System.Text;
using System.Timers;
using System.Xml;
using RabbitMQ.Client;
using MySql.Data.MySqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using CanvasRabbitMQSender.UserRepo;

namespace CanvasRabbitMQSender
{
    public class Program
    {
        public static List<User> users = new List<User>();
        private static string constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
        private static Timer timerSendEvents;
        private static Timer timerHeartbeat;
        private static List<Event> newEvents = new List<Event>();
        private static List<Event> newCourseEvents = new List<Event>();

        public static void Main()
        {
            //send User
            GetUserFromDB.SetUserTimer();
            //send events
            timerSendEvents = new System.Timers.Timer(5000);
            timerSendEvents.Elapsed += TimedEventSendEvent;
            timerSendEvents.AutoReset = true;
            timerSendEvents.Enabled = true;
            //heartbeat
            timerHeartbeat = new System.Timers.Timer(1000);
            timerHeartbeat.Elapsed += TimedHeartBeat;
            timerHeartbeat.AutoReset = true;
            timerHeartbeat.Enabled = true;
            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            Console.ReadLine();
            timerSendEvents.Stop();
            timerSendEvents.Dispose();
            timerHeartbeat.Stop();
            timerHeartbeat.Dispose();
            GetUserFromDB.usertimer.Stop();
            GetUserFromDB.usertimer.Dispose();
            Console.WriteLine("Terminating the application...");
        }

        public async static void TimedHeartBeat(Object source, ElapsedEventArgs arg)
        {
            Heartbeat heartbeat = new Heartbeat();
            //string xml = ConvertToXml(Event);
            heartbeat.Header.Status = await CheckIfOnline();
            heartbeat.TimeStamp = DateTime.Now;
            string xml = XmlController.SerializeToXmlString<Heartbeat>(heartbeat);
            //send it to rabbitMQ
            var factory = new ConnectionFactory() { HostName = "10.3.17.61" };
            factory.UserName = "guest";
            factory.Password = "guest";
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var body = Encoding.UTF8.GetBytes(xml);

                channel.BasicPublish(
                                     exchange: "",
                                     routingKey: "to-monitoring_heartbeat-queue",
                                     basicProperties: null,
                                     body: body);
                //Console.WriteLine(" [x] Sent {0}", xml);
            }
            newCourseEvents.Clear();
            Console.WriteLine("Send heartbeat: " + heartbeat.Header.Status);
        }

        public async static Task<string> CheckIfOnline()
        {
            HttpClient client = new HttpClient();
            var checkingResponse = await client.GetAsync("http://10.3.17.67:3000/");
            if (!checkingResponse.IsSuccessStatusCode)
            {
                return "ERROR";
            }
            else
            {
                return "ONLINE";
            }
        }
        public static void TimedEventSendEvent(Object source, ElapsedEventArgs arg)
        {
            //try
            //{
            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {
                connection.Open();

                string sql = "SELECT id, title, description, location_name, location_address, start_at, end_at, context_id, context_type, uuid, created_at, updated_at, deleted_at, user_id, entityversion FROM public.calendar_events  where updated_at > now() - interval '5 second'";
                Event newEvent;
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string title = "";
                            string description = "";
                            string locationAddress = "";
                            string locationName = "";
                            bool deleted = false;

                            if (!reader.IsDBNull(1))
                            {
                                title = reader.GetString(1);
                            }
                            if (!reader.IsDBNull(2))
                            {
                                description = reader.GetString(2);
                            }
                            if (!reader.IsDBNull(3))
                            {
                                locationName = reader.GetString(3);
                            }
                            if (!reader.IsDBNull(4))
                            {
                                locationAddress = reader.GetString(4);
                            }
                            if (!reader.IsDBNull(12))
                            {
                                deleted = true;
                            }
                            newEvent = new Event(
                                reader.GetInt32(0),
                                title,
                                description,
                                locationName,
                                locationAddress,
                                reader.GetDateTime(5).AddHours(2),
                                reader.GetDateTime(6).AddHours(2),
                                reader.GetInt32(7),
                                reader.GetString(8),
                                reader.GetDateTime(10),
                                reader.GetDateTime(11),
                                reader.GetInt32(13),
                                reader.GetInt32(14)
                                );
                            Console.WriteLine(newEvent.UpdatedAt.ToString());
                            if (!reader.IsDBNull(9))
                            {
                                newEvent.UUID = reader.GetString(9);
                            }
                            else
                            {
                                newEvent.UUID = MakeUUID(reader.GetInt32(0));
                            }

                            if (newEvent.UpdatedAt == newEvent.CreatedAt)
                            {
                                newEvent.Header.Method = "CREATE";
                            }
                            else
                            {
                                if (deleted)
                                {
                                    newEvent.Header.Method = "DELETE";
                                }
                                else
                                {
                                    newEvent.Header.Method = "UPDATE";
                                }
                            }
                            newEvents.Add(newEvent);
                            Console.WriteLine("Found event: " + newEvent.Title);
                        }
                    }
                }
                foreach (var Event in newEvents)
                {
                    if (Event.ContextType.ToLower() == "course" || Event.ContextType.ToLower() == "CourseSection" || Event.ContextType.ToLower() == "Group")
                    {
                        //sql = "SELECT uuid, muuid FROM public.courses where id = " + (Event.ContextId.ToString());
                        sql = "SELECT id, muuid FROM public.users where id = " + (Event.User_id.ToString());
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            using (NpgsqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(1))
                                    {
                                        Event.OrganiserId = reader.GetString(1);
                                    }
                                    else
                                    {
                                        Event.OrganiserId = MakeUserUUID(reader.GetInt32(0));
                                    }
                                }
                            }
                        }
                        newCourseEvents.Add(Event);
                    }
                }
                connection.Close();
            }
            //}
            //catch (Exception ex)
            //{
            //Console.WriteLine(ex.ToString());
            //}
            newEvents.Clear();
            foreach (var Event in newCourseEvents)
            {
                string xml = XmlController.SerializeToXmlString<Event>(Event);
                if (XSDValidatie(xml, "event.xsd"))
                {
                    continue;
                }
                if (Event.CreatedAt == Event.UpdatedAt || CheckUpdateEntityVersion(Event.UUID, Event.EntityVersion))//MUUID not working
                {
                    //string xml = ConvertToXml(Event);
                    //send it to rabbitMQ
                    var factory = new ConnectionFactory() { HostName = "10.3.17.61" };
                    factory.UserName = "guest";
                    factory.Password = "guest";
                    using (var connection = factory.CreateConnection())
                    {
                        using (var channel = connection.CreateModel())
                        {
                            var body = Encoding.UTF8.GetBytes(xml);
                            channel.BasicPublish(exchange: "event-exchange",
                                                 routingKey: "",
                                                 basicProperties: null,
                                                 body: body);
                            Console.WriteLine(" [x] Sent {0}", xml);
                        }
                    }
                }
            }

            newCourseEvents.Clear();
        }

        public static string ConvertToXml(Event Event)
        {
            //convert to xml
            using (var sw = new StringWriter())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.Encoding = Encoding.UTF8;
                using (var writer = XmlWriter.Create(sw, settings))
                {
                    // Build Xml with xw.

                    writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
                    writer.WriteStartElement("event");
                    writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "event.xsd");
                    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    writer.WriteStartElement("header");
                    writer.WriteElementString("method", Event.Header.Method);
                    writer.WriteElementString("source", "CANVAS");
                    writer.WriteEndElement();
                    writer.WriteElementString("uuid", Event.UUID);
                    writer.WriteElementString("entityVersion", "15");
                    writer.WriteElementString("title", Event.Title);
                    writer.WriteElementString("organiserId", Event.OrganiserId);
                    writer.WriteElementString("description", Event.Description);
                    writer.WriteElementString("start", Event.StartAt.ToString());
                    writer.WriteElementString("end", Event.EndAt.ToString());
                    writer.WriteElementString("location", Event.LocationAddress);
                    //writer.WriteElementString("locationAddress", Event.LocationAddress);
                    //if (Event.LocationAddress.Contains('%'))// formaat: straatnaam % huisnr % postcode % stad
                    //{
                    //    string[] address = Event.LocationAddress.Split('%');
                    //    if (address.Length == 4)
                    //    {
                    //        writer.WriteStartElement("Location");
                    //        writer.WriteElementString("streetname", address[0]);
                    //        writer.WriteElementString("number", address[1]);
                    //        writer.WriteElementString("city", address[3]);
                    //        writer.WriteElementString("postalcode", address[2]);
                    //        writer.WriteEndElement();
                    //    }
                    //}
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();

                }
                return sw.ToString();
            }
        }
        public static string MakeUUID(int id) //events
        {
            string uuid;
            uuid = GetUUID("event", id);
            string sql = "UPDATE public.calendar_events SET uuid = @uuid where id = @id";
            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            using (NpgsqlCommand command = connection.CreateCommand())
            {
                command.Parameters.AddWithValue("uuid", uuid);
                command.Parameters.AddWithValue("id", id);
                command.CommandText = sql;
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            return uuid;
        }
        public static string MakeUserUUID(int id)//users
        {
            string uuid = null;
            string sql = "select muuid from public.users where id = @id";

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {
                connection.Open();
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("id", id);
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                uuid = reader.GetString(0);
                            }
                        }
                    }
                }
                connection.Close();
            }
            if (String.IsNullOrEmpty(uuid))
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(constring))
                {
                    connection.Open();
                    uuid = GetUUID("user", id);
                    sql = "UPDATE public.users SET muuid = @uuid where id = @id";
                    using (NpgsqlCommand command = connection.CreateCommand())
                    {
                        command.Parameters.AddWithValue("uuid", uuid);
                        command.Parameters.AddWithValue("id", id);
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            return uuid;

        }
        public static string GetUUID(string type, int id)
        {
            string constring1 = "Server=10.3.17.63,3306; User ID = muuid; Password = muuid;  database=masteruuid;";
            string constring2 = "Server=10.3.17.64,3306; User ID = muuid; Password = muuid;  database=masteruuid;";
            string sql = "SELECT UUID()";
            string sql2 = "INSERT INTO master (UUID,Source_EntityId,EntityType,Source)VALUES(UUID_TO_BIN(@uuid),'@id','@type','Canvas'); ";
            string uuid = "";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(constring1))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(sql, connection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                uuid = reader.GetString(0);
                            }
                        }
                    }
                    connection.Close();
                }
                using (MySqlConnection connection = new MySqlConnection(constring1))
                {
                    connection.Open();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Parameters.AddWithValue("uuid", uuid);
                        command.Parameters.AddWithValue("type", type);
                        command.Parameters.AddWithValue("id", id);
                        command.CommandText = sql2;
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception a)
            {
                Console.WriteLine("Failed to connect to first server for muuid");
                Console.WriteLine(a.ToString());
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(constring2))
                    {
                        connection.Open();
                        using (MySqlCommand command = new MySqlCommand(sql, connection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    uuid = reader.GetString(0);
                                }
                            }
                        }
                        connection.Close();
                    }
                    using (MySqlConnection connection = new MySqlConnection(constring1))
                    {
                        connection.Open();
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Parameters.AddWithValue("uuid", uuid);
                            command.Parameters.AddWithValue("type", type);
                            command.Parameters.AddWithValue("id", id);
                            command.CommandText = sql2;
                            command.ExecuteNonQuery();
                        }
                        connection.Close();
                    }
                }
                catch (Exception b)
                {
                    Console.WriteLine("Failed to connect to second server for muuid");
                    Console.WriteLine(b.ToString());
                    throw;
                }
                throw;
            }
            return uuid;
        }

        public static bool CheckUpdateEntityVersion(string uuid, int entityversion)
        {
            string constring1 = "Server=10.3.17.63,3306; User ID = muuid; Password = muuid; database=masteruuid;";
            string constring2 = "Server=10.3.17.64,3306; User ID = muuid; Password = muuid; database=masteruuid;";
            string sql = "update master set EntityVersion = @entityversion " +
                "where Source = @MyService and " +
                "EntityVersion = @entityversion - 1 and " +
                "UUID = UUID_TO_BIN(@uuid) and " +
                "(NOT EXISTS (select EntityVersion from masteruuid.master where Source = @ServiceX and UUID = UUID_TO_BIN(@uuid)) or " +
                "(select EntityVersion from masteruuid.master where Source = @ServiceX and UUID = UUID_TO_BIN(@uuid)) < @entityversion) and " +
                "(NOT EXISTS (select EntityVersion from masteruuid.master where Source = @ServiceY and UUID = BIN(@uuid)) or" +
                "(select EntityVersion from masteruuid.master where Source = @ServiceY and UUID = UUID_TO_BIN(@uuid)) < @entityversion);";
            bool edited = false;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(constring1))
                {
                    connection.Open();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.Parameters.AddWithValue("entityversion", entityversion);
                        command.Parameters.AddWithValue("uuid", uuid);
                        command.Parameters.AddWithValue("MyService", "Canvas");
                        command.Parameters.AddWithValue("ServiceX", "Frontend");
                        command.Parameters.AddWithValue("Servicey", "Planning");
                        command.CommandText = sql;
                        int editeds = command.ExecuteNonQuery();
                        edited = command.ExecuteNonQuery() == 1;
                    }
                    connection.Close();
                }
            }
            catch (Exception a)
            {
                Console.WriteLine("Failed to connect to first server for muuid");
                Console.WriteLine(a.ToString());
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(constring2))
                    {
                        connection.Open();
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.Parameters.AddWithValue("entityversion", entityversion);
                            command.Parameters.AddWithValue("uuid", uuid);
                            command.Parameters.AddWithValue("MyService", "Canvas");
                            command.Parameters.AddWithValue("ServiceX", "Frontend");
                            command.Parameters.AddWithValue("Servicey", "Planning");
                            command.CommandText = sql;
                            edited = command.ExecuteNonQuery() == 1;
                        }
                        connection.Close();
                    }
                }
                catch (Exception b)
                {
                    Console.WriteLine("Failed to connect to second server for muuid");
                    Console.WriteLine(b.ToString());
                    throw;
                }
                throw;
            }
            return edited;
        }
        public static bool XSDValidatie(string xml, string xsd)
        {
            XmlSchemaSet xmlSchema = new XmlSchemaSet();
            try
            {
                xmlSchema.Add("", Environment.CurrentDirectory + "/" + xsd);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                xmlSchema.Add("", Environment.CurrentDirectory + "/../../../" + xsd);
            }

            bool validationErrors = false;
            XDocument doc = XDocument.Parse(xml);

            doc.Validate(xmlSchema, (sender, args) =>
            {
                Console.WriteLine("Error Message: " + args.Message);
                validationErrors = true;
            });
            return validationErrors;
        }
    }
}