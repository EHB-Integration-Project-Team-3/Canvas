using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;


namespace CanvasRabbitMQReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "10.3.17.61" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            var channel2 = connection.CreateModel();

            //Event consumer
            channel.QueueDeclare(queue: "to-canvas_event-queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Event canvasEvent;
                canvasEvent = Xmlcontroller.DeserializeXmlString<Event>(message);


                Console.WriteLine(" [x] Received", canvasEvent.Header.Method);
            };
            channel.BasicConsume(queue: "to-canvas_event-queue",
                                 autoAck: true,
                                 consumer: consumer);

            //User Consumer
            channel2.QueueDeclare(queue: "to-canvas_user-queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumer2 = new EventingBasicConsumer(channel2);
            consumer2.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                User canvasUser;
                canvasUser = Xmlcontroller.DeserializeXmlString<User>(message);


                Console.WriteLine(" [x] Received", canvasUser.Header.Method);
            };
            channel.BasicConsume(queue: "to-canvas_user-queue",
                                 autoAck: true,
                                 consumer: consumer2);





            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();






        }


        static void TestStuff()
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sql = "SELECT context_id,start_at,end_at,context_type,title,location_name,location_address,workflow_state FROM public.calendar_events ";
            String sql2 = "INSERT INTO public.calendar_events(id,user_id,context_code,context_id,start_at,end_at,context_type,title,location_name,location_address,workflow_state,created_at,updated_at,root_account_id) VALUES " +
                "(nextval ('calendar_events_id_seq'::regclass),@user_id,@context_code,@context_id,@start_at,@end_at,@context_type,@title,@location_name,@location_address,@workflow_state,@created_at,@updated_at,@root_account_id)";
            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {
                /*NpgsqlCommand command = new NpgsqlCommand(
                sql, connection);
                connection.Open();
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(String.Format("{0}, {1},{2},{3},{4},{5},{6},{7},{8}",
                            reader[0], reader[1],reader[2], reader[3], reader[4], reader[5], reader[6], reader[7], reader[8]));
                    }
                    Console.WriteLine(DateTime.Now.AddDays(1));
                }*/


                /*NpgsqlCommand command = new NpgsqlCommand(sql2, connection);
                var context_id = command.Parameters.Add("@context_id",NpgsqlTypes.NpgsqlDbType.Bigint);
                var context_type = command.Parameters.Add("@context_type",NpgsqlTypes.NpgsqlDbType.Text);
                var title = command.Parameters.Add("@title", NpgsqlTypes.NpgsqlDbType.Text);
                var location_name = command.Parameters.Add("@location_name", NpgsqlTypes.NpgsqlDbType.Text);
                var location_address = command.Parameters.Add("@location_address", NpgsqlTypes.NpgsqlDbType.Text);
                var start_at = command.Parameters.Add("@start_at",NpgsqlTypes.NpgsqlDbType.Date);
                var end_at = command.Parameters.Add("@end_at", NpgsqlTypes.NpgsqlDbType.Date);
                var workflow_state = command.Parameters.Add("@workflow_state",NpgsqlTypes.NpgsqlDbType.Text);
                var user_id = command.Parameters.Add("@user_id", NpgsqlTypes.NpgsqlDbType.Bigint);
                var created_at = command.Parameters.Add("@created_at", NpgsqlTypes.NpgsqlDbType.Date);
                var updated_at = command.Parameters.Add("@updated_at", NpgsqlTypes.NpgsqlDbType.Date);
                var context_code = command.Parameters.Add("@context_code", NpgsqlTypes.NpgsqlDbType.Text);
                var root_account_id = command.Parameters.Add("@root_account_id", NpgsqlTypes.NpgsqlDbType.Bigint);


                connection.Open();
                command.Prepare();
                context_id.Value = 2 ;
                context_type.Value = "Course";
                title.Value = "JOSKEdd";
                location_name.Value = "My house";
                location_address.Value = "You know this";
                workflow_state.Value = "active";
                start_at.Value = DateTime.Now.AddDays(1);
                end_at.Value = DateTime.Now.AddDays(1).AddHours(2);
                user_id.Value = 2;
                created_at.Value = DateTime.Now;
                updated_at.Value = DateTime.Now;
                context_code.Value = "course_2";
                root_account_id.Value = 2;
                command.ExecuteNonQuery();
                connection.Close();*/

                
            }

        }
    }  }
