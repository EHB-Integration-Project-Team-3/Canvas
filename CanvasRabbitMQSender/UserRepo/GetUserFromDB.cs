using Npgsql;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace CanvasRabbitMQSender.UserRepo
{
    class GetUserFromDB
    {
        public static List<User> users = new List<User>();
        private static readonly string constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
        public static Timer usertimer;

        public static void SetUserTimer()
        {
            // Create a timer with a two second interval.
            GetUserFromDB.usertimer = new Timer(5000);

            // Hook up the Elapsed event for the timer.
            usertimer.Elapsed += GetAndPushUser;
            usertimer.AutoReset = true;
            usertimer.Enabled = true;
        }

        public static void GetAndPushUser(Object source, ElapsedEventArgs e)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString: constring))
            {
                connection.Open();
                string nowMinus5 = DateTime.Now.Subtract(TimeSpan.FromSeconds(5)).ToString("yyyy-MM-dd HH:mm:ss");
                var sqlcmd = "SELECT id, name, sortable_name, uuid, created_at, updated_at, muuid, workflow_state FROM public.users where updated_at > now() - interval '5 second' and id != 1";
                Console.WriteLine(nowMinus5);

                User user;
                using var cmd = new NpgsqlCommand(sqlcmd, connection);
                NpgsqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    user = new User(
                                dr.GetInt32(0),
                                dr.GetString(dr.GetOrdinal("sortable_name")),
                                "lecturer",
                                dr.GetDateTime(4).AddHours(2),
                                Convert.ToDateTime(dr["updated_at"]).AddHours(2),
                                false);


                      
                    //Update
                      if (!dr.IsDBNull(dr.GetOrdinal("muuid")))
                      {
                        user.UUID = dr.GetString(dr.GetOrdinal("muuid"));
                      }
                      else
                      {
                         user.UUID = Program.MakeUserUUID(dr.GetInt32(0));
                      }
                    if (dr.GetString(dr.GetOrdinal("workflow_state")) == "deleted") {
                        user.Header.Method = "DELETE";

                    } else { 
                        if (user.CreatedAt == user.UpdatedAt)
                        {
                            user.Header.Method = "CREATE";
                        }
                        else
                        {
                            user.Header.Method = "UPDATE";
                        }
                    }

                    
                    user.Header.Method = "CREATE";
                    //user.UUID = Uuid.GetUUID();
                    users.Add(user);
                    Console.WriteLine(user.UUID);
                }

                connection.Close();
            }



            foreach (var user in users)
            {
                string xml = XmlController.SerializeToXmlString<User>(user);

                if (Program.XSDValidatie(xml, "user.xsd"))
                {
                    continue;
                }
                if (user.CreatedAt == user.UpdatedAt || Program.CheckUpdateEntityVersion(user.UUID, user.EntityVersion))
                {
                    //string xml = UserConvertToXml.convertToXml(user);
                    //string xml = Xmlcontroller.SerializeToXmlString(user);
                    var factory = new ConnectionFactory() { HostName = "10.3.17.61" };
                    factory.UserName = "guest";
                    factory.Password = "guest";
                    using (var connection = factory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        var body = Encoding.UTF8.GetBytes(xml);


                        channel.BasicPublish(exchange: "user-exchange",
                                             routingKey: "to-canvas_user-queue",
                                             basicProperties: null,
                                             body: body);
                        Console.WriteLine(" [x] Sent {0}", xml);
                    }
                }
            }
            users.Clear();
            Console.WriteLine("Complete");

            Console.ReadLine();
        }
    }
}
