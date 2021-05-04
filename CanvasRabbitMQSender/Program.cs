using System;
using System.Collections.Generic;
using Npgsql;
using System.IO;
using System.Data.SqlClient;
using System.Text;
using System.Timers;
using System.Xml;
using RabbitMQ.Client;
using MySql.Data.MySqlClient;

namespace CanvasRabbitMQSender
{
    class Program
    {
        private static string constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
        private static Timer aTimer;
        private static List<Event> newEvents = new List<Event>();
        private static List<Event> newCourseEvents = new List<Event>();

        public static void Main()
        {
            SetTimer();

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            Console.ReadLine();
            aTimer.Stop();
            aTimer.Dispose();

            Console.WriteLine("Terminating the application...");
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(5000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs arg)
        {
            //try
            //{
            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {
                connection.Open();
                string nowMinus5 = DateTime.Now.Subtract(TimeSpan.FromSeconds(5)).ToString("yyyy-MM-dd HH:mm:ss");

                string sql = "SELECT id, title, description, location_name, location_address, start_at, end_at, context_id, context_type, uuid FROM public.calendar_events where updated_at > '"+ nowMinus5+ "'";

                Console.WriteLine(sql);
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
                            newEvent = new Event(
                                reader.GetInt32(0),
                                title,
                                description,
                                locationName,
                                locationAddress,
                                reader.GetDateTime(5),
                                reader.GetDateTime(6),
                                reader.GetInt32(7),
                                reader.GetString(8));
                            if (!reader.IsDBNull(9))
                            {
                                newEvent.UUID = reader.GetString(9);
                            }
                            else
                            {
                                newEvent.UUID = MakeUUID(reader.GetInt32(0));
                            }
                            newEvents.Add(newEvent);
                            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
                            Console.WriteLine("Found event: " + newEvent.Title);
                        }
                    }
                }
                foreach (var Event in newEvents)
                {
                    if (Event.ContextType.ToLower() == "course")
                    {
                        sql = "SELECT uuid, muuid FROM public.courses where id = " + (Event.ContextId.ToString());
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
                                        Event.OrganiserId = MakeUUID(reader.GetString(0));
                                    }
                                }
                            }
                        }
                        newCourseEvents.Add(Event);
                    }
                    else
                    {
                        //sql = "SELECT uuid FROM public.users where id = " + (newEvent.ContextId.ToString());
                    }

                    //using (SqlCommand command = new SqlCommand(sql, connection))
                    //{
                    //    using (SqlDataReader reader = command.ExecuteReader())
                    //    {
                    //        while (reader.Read())
                    //        {
                    //            newEvent.OrganiserId = reader.GetString(0);
                    //        }
                    //    }
                    //}
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
                string xml = ConvertToXml(Event);
                //send it to rabbitMQ
                var factory = new ConnectionFactory() { HostName = "10.3.17.62"};
                //factory.UserName = "canvas";
                //factory.Password = "ubuntu123";
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    //channel.ExchangeDeclare(exchange: "event-exchange", type: ExchangeType.Fanout);
                    var body = Encoding.UTF8.GetBytes(xml);

                    channel.BasicPublish(exchange: "event-exchange",
                                         routingKey: "",
                                         basicProperties: null,
                                         body: body);
                    Console.WriteLine(" [x] Sent {0}", xml);
                }
            }
            newCourseEvents.Clear();
            Console.WriteLine("Complete");
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

                    writer.WriteStartDocument();
                    writer.WriteStartElement("Event");
                    writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "event.xsd");
                    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    writer.WriteElementString("uuid", Event.UUID);
                    writer.WriteElementString("entityVersion", "15");
                    writer.WriteElementString("title", Event.Title);
                    writer.WriteElementString("organiserId", Event.OrganiserId);
                    writer.WriteElementString("description", Event.Description);
                    writer.WriteElementString("start", Event.StartAt.ToString());
                    writer.WriteElementString("end", Event.EndAt.ToString());
                    writer.WriteStartElement("Location");
                    writer.WriteElementString("locationName", Event.LocationName);
                    //writer.WriteElementString("locationAddress", Event.LocationAddress);
                    if (Event.LocationAddress.Contains('%'))// formaat: straatnaam % huisnr % postcode % stad
                    {
                        string[] address = Event.LocationAddress.Split('%');
                        if (address.Length == 4)
                        {
                            writer.WriteElementString("streetname", address[0]);
                            writer.WriteElementString("number", address[1]);
                            writer.WriteElementString("city", address[3]);
                            writer.WriteElementString("postalcode", address[2]);
                        }
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();

                }
                return sw.ToString();
            }
        }
        public static string MakeUUID(int id)//events
        {
            string uuid;
            uuid = GetUUID();
            string sql = "UPDATE public.calendar_events SET uuid = @uuid where id = @id";
                using (NpgsqlConnection connection = new NpgsqlConnection(constring))
                using (NpgsqlCommand command = connection.CreateCommand())
                {

                command.Parameters.AddWithValue("@uuid", uuid);

                command.Parameters.AddWithValue("@id", id);
                command.CommandText = sql;

                    connection.Open();

                    command.ExecuteNonQuery();

                    connection.Close();
            }
            return uuid;
        }
        public static string MakeUUID(string id)//courses
        {
            string uuid;
            uuid = GetUUID();
                string sql = "UPDATE public.courses SET muuid = @uuid where uuid = @id";
                using (NpgsqlConnection connection = new NpgsqlConnection(constring))
                using (NpgsqlCommand command = connection.CreateCommand())
            {

                command.Parameters.AddWithValue("@uuid", uuid);

                command.Parameters.AddWithValue("@id", id);
                command.CommandText = sql;

                    connection.Open();

                    command.ExecuteNonQuery();

                    connection.Close();
            }
            return uuid;
        }
        public static string GetUUID()
        {
            string constring1 = "Server=10.3.17.63,3306; User ID = muuid; Password = muuid;";
            string constring2 = "Server=10.3.17.64,3306; User ID = muuid; Password = muuid;";
            string sql = "SELECT UUID()";
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
            }
            catch (Exception a)
            {
                Console.WriteLine("Failed to connect to first server");
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
                }
                catch (Exception b)
                {
                    Console.WriteLine("Failed to connect to second server");
                    Console.WriteLine(b.ToString());
                    throw;
                }
                throw;
            }
            return uuid;
        }
    }
}