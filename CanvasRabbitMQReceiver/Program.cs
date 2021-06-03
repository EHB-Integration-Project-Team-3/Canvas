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
            var channel3 = connection.CreateModel();

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
                            if (UpdateMUUID(canvasEvent.UUID, canvasEvent.EntityVersion)) { UpdateEvent(canvasEvent); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case "DELETE":
                        try
                        {
                            DeleteEvent(canvasEvent);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
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
                        
                            try
                            {
                                if (UpdateMUUID(canvasUser.UUID, canvasUser.EntityVersion)) { UpdateUser(canvasUser); }
                                
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        



                        break;
                    case "DELETE":
                        try
                        {
                            DeleteUser(canvasUser);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

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
            //Attendance Consumer
            /*channel3.QueueDeclare(queue: "to-canvas_attendance-queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumerAttendance = new EventingBasicConsumer(channel3);
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
                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case "UPDATE":
                        
                        {
                            try
                            {
                                
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

                Console.WriteLine(" [x] Received Attendance ");
            };
            channel.BasicConsume(queue: "to-canvas_attendance-queue",
                                 autoAck: true,
                                 consumer: consumerAttendance);*/





            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();






        }


        static void CreateEvent(Event canvasEvent)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlEvent = "INSERT INTO public.calendar_events(id,user_id,context_code,context_id,start_at,end_at,context_type,title,location_name,location_address,workflow_state,created_at,updated_at,root_account_id,uuid) VALUES " +
                "(nextval ('calendar_events_id_seq'::regclass),@user_id,@context_code,@context_id,@start_at,@end_at,@context_type,@title,@location_name,@location_address,@workflow_state,@created_at,@updated_at,@root_account_id,@uuid)";
            String sqlSection = "INSERT INTO public.course_sections(id,course_id,root_account_id,name,created_at,updated_at,workflow_state) VALUES " +
                "(nextval ('course_sections_id_seq'::regclass),@course_id,@root_account_id,@name,@created_at,@updated_at,@workflow_state)";

            int courseNumber = 5;
            int rootAccId = 2;
            int localId = FetchLocalId(canvasEvent.OrganiserId,0);


            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("@user_id", localId);
                    command.Parameters.AddWithValue("@context_code", "course_5");
                    command.Parameters.AddWithValue("@context_id", courseNumber);
                    command.Parameters.AddWithValue("@start_at", canvasEvent.StartAt);
                    command.Parameters.AddWithValue("@end_at", canvasEvent.EndAt);
                    command.Parameters.AddWithValue("@context_type", "Course");
                    command.Parameters.AddWithValue("@title", canvasEvent.Title);
                    command.Parameters.AddWithValue("@location_name", canvasEvent.LocationName);
                    command.Parameters.AddWithValue("@location_address", canvasEvent.LocationAddress);
                    command.Parameters.AddWithValue("@workflow_state", "active");
                    command.Parameters.AddWithValue("@created_at", canvasEvent.CreatedAt);
                    command.Parameters.AddWithValue("@updated_at", canvasEvent.UpdatedAt);
                    command.Parameters.AddWithValue("@root_account_id", rootAccId);
                    command.Parameters.AddWithValue("@uuid", canvasEvent.UUID);


                    command.CommandText = sqlEvent;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
                using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
                {

                    using (NpgsqlCommand command = connectionSection.CreateCommand())
                    {
                        command.Parameters.AddWithValue("@context_id", courseNumber);
                        command.Parameters.AddWithValue("@root_account_id", rootAccId);
                        command.Parameters.AddWithValue("@name", canvasEvent.Title);
                        command.Parameters.AddWithValue("@created_at", canvasEvent.CreatedAt);
                        command.Parameters.AddWithValue("@updated_at", canvasEvent.UpdatedAt);
                        command.Parameters.AddWithValue("@workflow_state", "active");

                        command.CommandText = sqlSection;
                        connectionSection.Open();
                        command.ExecuteNonQuery();
                        connectionSection.Close();

                    }
                }
            

            InsertMUUID("USER", canvasEvent.UUID, localId);



        }
        static void UpdateEvent(Event canvasEvent)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlEvent = "UPDATE public.calendar_events SET user_id=@user_id,start_at=@start_at,end_at=@end_at,title=@title,location_name=@location_name,location_address=@location_address,updated_at=@updated_at,entityversion=entityversion+1" +
                "WHERE uuid=@uuid";
            String sqlSection = "UPDATE course_sections SET updated_at=@updated_at,name=@name" +
                "WHERE name=@oldname";

            int localId = FetchLocalId(canvasEvent.UUID,0);
            String localName = FetchLocalName(canvasEvent.UUID);

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("@user_id", localId);
                    command.Parameters.AddWithValue("@start_at", canvasEvent.StartAt);
                    command.Parameters.AddWithValue("@end_at", canvasEvent.EndAt);
                    command.Parameters.AddWithValue("@title", canvasEvent.Title);
                    command.Parameters.AddWithValue("@location_name", canvasEvent.LocationName);
                    command.Parameters.AddWithValue("@location_address", canvasEvent.LocationAddress);
                    command.Parameters.AddWithValue("@updated_at", canvasEvent.UpdatedAt);
                    command.Parameters.AddWithValue("@uuid", canvasEvent.UUID);
                    command.CommandText = sqlEvent;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }

            using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connectionSection.CreateCommand())
                {
                    command.Parameters.AddWithValue("@name", canvasEvent.Title);
                    command.Parameters.AddWithValue("@updated_at", canvasEvent.UpdatedAt);
                    command.Parameters.AddWithValue("@oldname", localName);
                    command.CommandText = sqlSection;
                    connectionSection.Open();
                    command.ExecuteNonQuery();
                    connectionSection.Close();
                }
            }


            }
            static void DeleteEvent(Event canvasEvent) {

            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlEvent = "UPDATE public.calendar_events SET deleted_at=@deleted_at,updated_at,@updated_at,workflow_state=@workflow_state" +
                "WHERE uuid=@uuid";
            String sqlSection = "UPDATE public.course_sections SET @updated_at,workflow_state=@workflow_state WHERE name=@name";
            int localId = FetchLocalId(canvasEvent.UUID,0);

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                   
                    command.Parameters.AddWithValue("@deleted_at", canvasEvent.LocationAddress);
                    command.Parameters.AddWithValue("@updated_at", canvasEvent.UpdatedAt);
                    command.Parameters.AddWithValue("@workflow_state", "deleted");
                    command.Parameters.AddWithValue("@uuid", canvasEvent.UUID);

                    command.CommandText = sqlEvent;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connectionSection.CreateCommand())
                {

                    command.Parameters.AddWithValue("@deleted_at", canvasEvent.LocationAddress);
                    command.Parameters.AddWithValue("@updated_at", canvasEvent.UpdatedAt);
                    command.Parameters.AddWithValue("@workflow_state", "deleted");
                    command.Parameters.AddWithValue("@name", canvasEvent.Title);
                    command.CommandText = sqlSection;
                    connectionSection.Open();
                    command.ExecuteNonQuery();
                    connectionSection.Close();
                }
            }
        }

            static void CreateUser(User canvasUser) {
                String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
                String sqlCreate = "INSERT INTO public.users(id,name,sortable_name,workflow_state,muuid,created_at,updated_at,short_name,muuid) VALUES " +
        "(nextval ('users_id_seq'::regclass),@name,@sortable_name,@workflow_state,@uuid,@created_at,@updated_at,@short_name,@muuid)";


            using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connectionSection.CreateCommand())
                {

                    command.Parameters.AddWithValue("@name", canvasUser.Lastname + " " + canvasUser.Firstname);
                    command.Parameters.AddWithValue("@sortable_name", canvasUser.Lastname + ", " + canvasUser.Firstname);
                    command.Parameters.AddWithValue("@workflow_state", "registered");
                    command.Parameters.AddWithValue("@created_at", canvasUser.CreatedAt);
                    command.Parameters.AddWithValue("@updated_at", canvasUser.UpdatedAt);
                    command.Parameters.AddWithValue("@short_name", canvasUser.Firstname + ", " + canvasUser.Lastname);
                    command.Parameters.AddWithValue("@muuid", canvasUser.UUID);

                    command.CommandText = sqlCreate;
                    connectionSection.Open();
                    command.ExecuteNonQuery();
                    connectionSection.Close();
                }
            }
            int localId = FetchLocalId(canvasUser.UUID, 1);

            InsertMUUID("USER", canvasUser.UUID, localId);

        }
        static void UpdateUser(User canvasUser)
            {
                String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
                String sqlUpdate = "UPDATE public.users SET name=@name,sortable_name=@sortable_name,workflow_state=@workflow_state,created_at=@created_at,updated_at=@updated_at,short_name=@short_name" +
                    "WHERE muuid=@uuid";

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {

                    command.Parameters.AddWithValue("@name", canvasUser.Lastname + " " + canvasUser.Firstname);
                    command.Parameters.AddWithValue("@sortable_name", canvasUser.Lastname + ", " + canvasUser.Firstname);
                    command.Parameters.AddWithValue("@updated_at", canvasUser.UpdatedAt);
                    command.Parameters.AddWithValue("@short_name", canvasUser.Firstname + ", " + canvasUser.Lastname);
                    command.Parameters.AddWithValue("@uuid", canvasUser.UUID);

                    command.CommandText = sqlUpdate;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }
            static void DeleteUser(User canvasUser) {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";

            String sqlDelete = "UPDATE public.users SET deleted_at=@deleted_at,updated_at,@updated_at,workflow_state=@workflow_state" +
        "WHERE muuid=@uuid";

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {

                    command.Parameters.AddWithValue("@deleted_at", canvasUser.UpdatedAt);
                    command.Parameters.AddWithValue("@workflow_state", "deleted");
                    command.Parameters.AddWithValue("@updated_at", canvasUser.UpdatedAt);
                    command.Parameters.AddWithValue("@short_name", canvasUser.Firstname + ", " + canvasUser.Lastname);
                    command.Parameters.AddWithValue("@uuid", canvasUser.UUID);

                    command.CommandText = sqlDelete;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }


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

        public static bool UpdateMUUID(string uuid, int entityVersion)
        {
            string constring1 = "Server=10.3.17.63,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
            string constring2 = "Server=10.3.17.64,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
            string sql = "update master set EntityVersion = @entityVersion where UUID = @uuid and Source = @source and EntityVersion = @entityVersion - 1 and" +
  "(select EntityVersion from master where Source = @source and UUID = @uuid) < @entityVersion and (select EntityVersion from master where Source = @source and UUID = @uuid) < @entityVersion";
            String source = "CANVAS";
            Boolean success = false;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(constring1))
                {
                    connection.Open();
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.Parameters.AddWithValue("@Uuid", uuid);
                        command.Parameters.AddWithValue("@entityVersion", entityVersion);
                        command.Parameters.AddWithValue("@Source", source);

                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                    success = true;
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
                            command.Parameters.AddWithValue("@entityVersion", entityVersion);
                            command.Parameters.AddWithValue("@Source", source);

                            command.ExecuteNonQuery();
                        }
                        connection.Close();
                        success = true;

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
            return success;
        }


        public static void DeleteMUUID(String uuid) {
            string constring1 = "Server=10.3.17.63,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
            string constring2 = "Server=10.3.17.64,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
            string sql = "delete from master where UUID = @uuid AND Source = @source";
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

        public static int FetchLocalId(String uuid,int type) {
            String location = "public.users";
            if (type < 1) { location = "public.calendar_events"; }
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlFetchId = "SELECT id FROM "+location+" WHERE  uuid = @uuid";
            int id = 1;
            
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
        public static String FetchLocalName(String uuid)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlFetchId = "SELECT title FROM calendar_events WHERE  uuid = @uuid";
            String name = "";
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
                            name = reader.GetString(0);
                        }

                    }
                    connection.Close();
                }
            }
            return name;
        }

 
    }
    } 
