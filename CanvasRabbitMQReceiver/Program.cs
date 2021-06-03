using MySql.Data.MySqlClient;
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
            var consumerEvent = new EventingBasicConsumer(channel);
            consumerEvent.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(message);
                Event canvasEvent;
                canvasEvent = Xmlcontroller.DeserializeXmlString<Event>(message);
                switch (canvasEvent.Header.Method) {
                    case "CREATE":
                        try
                        {
                            if (CheckMUUID(canvasEvent.UUID) == false) { CreateEvent(canvasEvent); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }


                        break;
                    case "UPDATE":
                        try
                        {
                            if (CheckUpdateMUUID(canvasEvent.UUID, canvasEvent.Header.Source) == false) { UpdateEvent(canvasEvent); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case "DELETE":
                        Console.WriteLine("delete failed");

                        break;
                    default:
                        Console.WriteLine("header reading failed");
                        break;

                }



                Console.WriteLine(" [x] Received Event " + canvasEvent.Header.Method + " " + canvasEvent.Title);
            };
            channel.BasicConsume(queue: "to-canvas_event-queue",
                                 autoAck: true,
                                 consumer: consumerEvent);

            //User Consumer
            channel2.QueueDeclare(queue: "to-canvas_user-queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumerUser = new EventingBasicConsumer(channel2);
            consumerUser.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                User canvasUser;

                canvasUser = Xmlcontroller.DeserializeXmlString<User>(message);
                Console.WriteLine(message);
                switch (canvasUser.Header.Method)
                {
                    case "CREATE":
                        try
                        {
                            if (CheckMUUID(canvasUser.UUID) == false) { CreateUser(canvasUser); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case "UPDATE":
                        if (CheckUpdateMUUID(canvasUser.UUID, canvasUser.Header.Source) == false) {
                            try
                            {
                                UpdateUser(canvasUser);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }



                        break;
                    case "DELETE":
                        Console.WriteLine("delete failed");

                        break;
                    default:
                        Console.WriteLine("header reading failed");
                        break;

                }

                Console.WriteLine(" [x] Received User " + canvasUser.Header.Method + " " + canvasUser.Lastname + " " + canvasUser.Firstname);
            };
            channel.BasicConsume(queue: "to-canvas_user-queue",
                                 autoAck: true,
                                 consumer: consumerUser);
            //User Consumer
            channel2.QueueDeclare(queue: "to-canvas_attendance-queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumerAttendance = new EventingBasicConsumer(channel2);
            consumerAttendance.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Attendance canvasAttendance;

                canvasAttendance = Xmlcontroller.DeserializeXmlString<Attendance>(message);
                Console.WriteLine(message);
                switch (canvasAttendance.Header.Method)
                {
                    case "CREATE":
                        try
                        {
                            if (CheckMUUID(canvasUser.UUID) == false) { CreateUser(canvasUser); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case "UPDATE":
                        if (CheckUpdateMUUID(canvasUser.UUID, canvasUser.Header.Source) == false)
                        {
                            try
                            {
                                UpdateUser(canvasUser);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }



                        break;
                    case "DELETE":
                        Console.WriteLine("delete failed");

                        break;
                    default:
                        Console.WriteLine("header reading failed");
                        break;

                }

                Console.WriteLine(" [x] Received Attendance " + canvasUser.Header.Method);
            };
            channel.BasicConsume(queue: "to-canvas_attendance-queue",
                                 autoAck: true,
                                 consumer: consumerAttendance);





            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();






        }


        static void CreateEvent(Event canvasEvent)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlFetchId = "SELECT id FROM users WHERE  uuid = @uuid";
            String sqlCreate = "INSERT INTO public.calendar_events(id,user_id,context_code,context_id,start_at,end_at,context_type,title,location_name,location_address,workflow_state,created_at,updated_at,root_account_id) VALUES " +
                "(nextval ('calendar_events_id_seq'::regclass),@user_id,@context_code,@context_id,@start_at,@end_at,@context_type,@title,@location_name,@location_address,@workflow_state,@created_at,@updated_at,@root_account_id)";
            String sqlSection = "INSERT INTO public.course_sections(id,course_id,root_account_id,name,created_at,updated_at,workflow_state) VALUES " +
                "(nextval ('course_sections_id_seq'::regclass),@course_id,@root_account_id,@name,@created_at,@updated_at,@workflow_state)";

            int courseNumber = 5;
            int rootAccId = 2;
            int userId = 1;
            

        }
        static void UpdateEvent(Event canvasEvent)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlUpdate = "UPDATE public.calendar_events SET user_id=@user_id,context_code=@context_code,context_id=@context_id,start_at=@start_at,end_at=@end_at,context_type=@context_type,title=@title,location_name=@location_name,location_address=@location_address,workflow_state=@workflow_state,created_at=@created_at,updated_at=@updated_at,root_account_id" +
                "WHERE uuid=@uuid";

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                NpgsqlCommand command = new NpgsqlCommand(sqlUpdate, connection);

                var name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                var sortable_name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                var workflow_state = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                var created_at = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                var updated_at = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                var short_name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                var uuid = command.Parameters.Add("@uuid", NpgsqlTypes.NpgsqlDbType.Text);

                connection.Open();
                name.Value = canvasUser.Firstname + ", " + canvasUser.Lastname;
                sortable_name.Value = canvasUser.Sortable_name;
                workflow_state.Value = "registered";
                created_at.Value = canvasUser.CreatedAt;
                updated_at.Value = canvasUser.UpdatedAt;
                short_name.Value = canvasUser.Firstname + " " + canvasUser.Lastname;
                uuid.Value = canvasUser.UUID;

                command.Prepare();

                //root_account_id.Value = 2;
                command.ExecuteNonQuery();
                connection.Close();
            }
        }
            static void DeleteEvent(Event canvasEvent) { }

            static void CreateUser(User canvasUser) {
                String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
                String sqlCreate = "INSERT INTO public.users(id,name,sortable_name,workflow_state,uuid,created_at,updated_at,short_name) VALUES " +
        "(nextval ('users_id_seq'::regclass),@name,@sortable_name,@workflow_state,@uuid,@created_at,@updated_at,@short_name)";

                using (NpgsqlConnection connection = new NpgsqlConnection(constring))
                {

                    NpgsqlCommand command = new NpgsqlCommand(sqlCreate, connection);

                    var name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var sortable_name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var workflow_state = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var uuid = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var created_at = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var updated_at = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var short_name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    //var muuid = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);

                    connection.Open();
                    name.Value = canvasUser.Firstname + ", " + canvasUser.Lastname;
                    sortable_name.Value = canvasUser.Sortable_name;
                    workflow_state.Value = "registered";
                    uuid.Value = canvasUser.UUID;
                    created_at.Value = canvasUser.CreatedAt;
                    updated_at.Value = canvasUser.UpdatedAt;
                    short_name.Value = canvasUser.Firstname + " " + canvasUser.Lastname;
                    command.Prepare();

                    //muuid.Value = canvasUser.m;


                    //root_account_id.Value = 2;
                    command.ExecuteNonQuery();
                    connection.Close();
                    //InsertMUUID("USER",canvasUser.UUID);
                }
            }
            static void UpdateUser(User canvasUser)
            {
                String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
                String sqlUpdate = "UPDATE public.users SET name=@name,sortable_name=@sortable_name,workflow_state=@workflow_state,created_at=@created_at,updated_at=@updated_at,short_name=@short_name" +
                    "WHERE uuid=@uuid";

                using (NpgsqlConnection connection = new NpgsqlConnection(constring))
                {

                    NpgsqlCommand command = new NpgsqlCommand(sqlUpdate, connection);

                    var name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var sortable_name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var workflow_state = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var created_at = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var updated_at = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var short_name = command.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Text);
                    var uuid = command.Parameters.Add("@uuid", NpgsqlTypes.NpgsqlDbType.Text);

                    connection.Open();
                    name.Value = canvasUser.Firstname + ", " + canvasUser.Lastname;
                    sortable_name.Value = canvasUser.Sortable_name;
                    workflow_state.Value = "registered";
                    created_at.Value = canvasUser.CreatedAt;
                    updated_at.Value = canvasUser.UpdatedAt;
                    short_name.Value = canvasUser.Firstname + " " + canvasUser.Lastname;
                    uuid.Value = canvasUser.UUID;

                    command.Prepare();

                    //root_account_id.Value = 2;
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            //InsertMUUID("USER",canvasUser.UUID);}
            static void DeleteUser(User canvasUser) { }


            public static Boolean CheckMUUID(String uuid) {
                string constring1 = "Server=10.3.17.63,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string constring2 = "Server=10.3.17.64,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string sql = "SELECT * FROM master WHERE UUID = UUID_TO_BIN(@val) AND Source = 'CANVAS'; ";
                Boolean uuidFound = false;
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(constring1))
                    {
                        MySqlCommand command = new MySqlCommand(sql, connection);
                        connection.Open();
                        //command.Prepare();
                        command.Parameters.AddWithValue("@val", uuid);
                        command.Prepare();


                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                uuidFound = true;
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
                            MySqlCommand command = new MySqlCommand(sql, connection);
                            connection.Open();
                            //command.Prepare();
                            command.Parameters.AddWithValue("@val", uuid);
                            command.Prepare();


                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    uuidFound = true;
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
                return uuidFound;
            }

            public static void InsertMUUID(String type, string uuid,int sourceId) {
                string constring1 = "Server=10.3.17.63,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string constring2 = "Server=10.3.17.64,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string sql = "INSERT INTO master(UUID, Source_EntityId, EntityType, Source )VALUES(UUID_TO_BIN(@Uuid), @Id, @Type, @Source); ";

                String source = "CANVAS";


            try
            {
                using (MySqlConnection connection = new MySqlConnection(constring1))
                {
                    connection.Open();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.Parameters.AddWithValue("@Uuid", uuid);
                        command.Parameters.AddWithValue("@Type", type);
                        command.Parameters.AddWithValue("@Id", sourceId);
                        command.Parameters.AddWithValue("@Source", source);

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
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = sql;
                            command.Parameters.AddWithValue("@Uuid", uuid);
                            command.Parameters.AddWithValue("@Type", type);
                            command.Parameters.AddWithValue("@Id", sourceId);
                            command.Parameters.AddWithValue("@Source", source);

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

            }

            public static Boolean CheckUpdateMUUID(String uuid, String source) {
                string constring1 = "Server=10.3.17.63,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string constring2 = "Server=10.3.17.64,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string sql1 = "SELECT EntityVersion from master WHERE UUID = @uuid AND Source = @source";
                string sql2 = "SELECT EntityVersion from master WHERE UUID = @uuid AND Source = @source";
                int version1 = 0;
                int version2 = 0;

                try
                {
                    using (MySqlConnection connection = new MySqlConnection(constring1))
                    {
                        MySqlCommand command1 = new MySqlCommand(sql1, connection);
                        MySqlCommand command2 = new MySqlCommand(sql2, connection);

                        connection.Open();
                        //command1.Prepare();
                        command1.Parameters.AddWithValue("@uuid", uuid);
                        command1.Parameters.AddWithValue("@source", source);
                        command1.Prepare();
                        MySqlDataReader reader1 = command1.ExecuteReader();
                        version1 = reader1.GetOrdinal("EntityVersion");

                        //command2.Prepare();
                        command2.Parameters.AddWithValue("@uuid", uuid);
                        command2.Parameters.AddWithValue("@source", "CANVAS");
                        command2.Prepare();
                        MySqlDataReader reader2 = command2.ExecuteReader();
                        version2 = reader2.GetOrdinal("EntityVersion");

                        connection.Close();

                        return version1.Equals(version2);

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
                            MySqlCommand command1 = new MySqlCommand(sql1, connection);
                            MySqlCommand command2 = new MySqlCommand(sql2, connection);

                            connection.Open();
                            //command1.Prepare();
                            command1.Parameters.AddWithValue("@uuid", uuid);
                            command1.Parameters.AddWithValue("@source", source);
                            command1.Prepare();
                            MySqlDataReader reader1 = command1.ExecuteReader();
                            version1 = reader1.GetOrdinal("EntityVersion");

                            //command2.Prepare();
                            command2.Parameters.AddWithValue("@uuid", uuid);
                            command2.Parameters.AddWithValue("@source", "CANVAS");
                            command2.Prepare();
                            MySqlDataReader reader2 = command2.ExecuteReader();
                            version2 = reader2.GetOrdinal("EntityVersion");

                            connection.Close();

                            return version1.Equals(version2);
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
            }

        public static void DeleteMUUID() { }

        public static int FetchLocalId(String uuid) {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlFetchId = "SELECT id FROM users WHERE  uuid = @uuid";

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("@uuid", uuid);
                    command.CommandText = sqlFetchId;
                    connection.Open();

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            id= reader.GetInt32(0);

                        }

                    }
                    connection.Close();
                }
            }return id;
        }
        }
    } 
