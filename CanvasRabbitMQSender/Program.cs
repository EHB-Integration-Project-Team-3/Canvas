﻿using System;
using System.Collections.Generic;
using Npgsql;
using System.IO;
using System.Data.SqlClient;
using System.Text;
using System.Timers;
using System.Xml;
using RabbitMQ.Client;
using MySql.Data.MySqlClient;
using System.Net.Http;
using System.Threading.Tasks;

namespace CanvasRabbitMQSender
{
    class Program
    {
        private static string constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
        private static Timer timerSendEvents;
        private static Timer timerHeartbeat;
        private static List<Event> newEvents = new List<Event>();
        private static List<Event> newCourseEvents = new List<Event>();

        public static void Main()
        {
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
            Console.WriteLine("Terminating the application...");
        }
        public async static void TimedHeartBeat(Object source, ElapsedEventArgs arg)
        {
            Heartbeat heartbeat = new Heartbeat();
            //string xml = ConvertToXml(Event);
            heartbeat.Header.Status = await CheckIfOnline();
            string xml = Xmlcontroller.SerializeToXmlString<Heartbeat>(heartbeat);
            //send it to rabbitMQ
            var factory = new ConnectionFactory() { HostName = "10.3.17.62" };
            factory.UserName = "guest";
            factory.Password = "guest";
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "to-monitoring_heartbeat-queue",
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);
                var body = Encoding.UTF8.GetBytes(xml);

                channel.BasicPublish(exchange: "",
                                     routingKey: "",
                                     basicProperties: null,
                                     body: body);
                Console.WriteLine(" [x] Sent {0}", xml);
            }
            newCourseEvents.Clear();
            Console.WriteLine("Complete");
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

                string sql = "SELECT id, title, description, location_name, location_address, start_at, end_at, context_id, context_type, uuid, created_at, updated_at, deleted_at FROM public.calendar_events  where updated_at > now() - interval '5 second'";

                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
                                reader.GetDateTime(11)
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
                //string xml = ConvertToXml(Event);
                string xml = Xmlcontroller.SerializeToXmlString<Event>(Event);
                //send it to rabbitMQ
                var factory = new ConnectionFactory() { HostName = "10.3.17.62"};
                factory.UserName = "guest";
                factory.Password = "guest";
                using (var connection = factory.CreateConnection())
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
    }
}