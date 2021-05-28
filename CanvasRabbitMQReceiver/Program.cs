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
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
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
                        catch
                        {
                            Console.WriteLine("create event " +canvasEvent.Title + " already exists");
                        }
                        //catch { CreateEvent(canvasEvent); }


                        break;
                    case "UPDATE":
                        try
                        {
                            if (CheckUpdateMUUID(canvasEvent.UUID, canvasEvent.Header.Source) == false) { UpdateEvent(canvasEvent); }
                        }
                        catch
                        {
                            Console.WriteLine("update event " + canvasEvent.Title + " no such event or already updated");
                        }

                        //catch { UpdateEvent(canvasEvent); }

                        break;
                    case "DELETE":
                        Console.WriteLine("delete failed");

                        break;
                    default:
                        Console.WriteLine("header reading failed");
                        break;

                }



                Console.WriteLine(" [x] Received Event " + canvasEvent.Header.Method);
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
                Console.WriteLine(message);
                switch (canvasUser.Header.Method)
                {
                    case "CREATE":
                        try
                        {
                            if (CheckMUUID(canvasUser.UUID) == false) { CreateUser(canvasUser); }
                        }
                        catch
                        {
                            Console.WriteLine("create user " + canvasUser.Lastname + " " + canvasUser.Firstname + " already exists");
                        }

                        //catch { CreateUser(canvasUser); }
                        break;
                    case "UPDATE":
                        if (CheckUpdateMUUID(canvasUser.UUID, canvasUser.Header.Source) == false) {
                            try
                            {
                                UpdateUser(canvasUser);
                                //if (CheckMUUID(canvasUser.UUID) == false) { UpdateUser(canvasUser); }
                            }
                            catch
                            {
                                Console.WriteLine("update user " + canvasUser.Lastname + " " + canvasUser.Firstname + " not found or already up to date");
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

                Console.WriteLine(" [x] Received User " + canvasUser.Header.Method);
            };
            channel.BasicConsume(queue: "to-canvas_user-queue",
                                 autoAck: true,
                                 consumer: consumer2);





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
            var userId = 1;
            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {
                NpgsqlCommand fetchId = new NpgsqlCommand(sqlFetchId, connection);
                var organiserUUID = fetchId.Parameters.Add("@uuid", NpgsqlTypes.NpgsqlDbType.Text);
                connection.Open();
                fetchId.Parameters.Add("@uuid", NpgsqlTypes.NpgsqlDbType.Text);
                organiserUUID.Value = canvasEvent.OrganiserId;
                fetchId.Prepare();

                Console.WriteLine(fetchId);

                using (NpgsqlDataReader reader = fetchId.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetOrdinal("id");
                        userId = reader.GetInt32(id);
                    }

                }

                connection.Close();


                NpgsqlCommand command1 = new NpgsqlCommand(sqlCreate, connection);
                var context_id = command1.Parameters.Add("@context_id", NpgsqlTypes.NpgsqlDbType.Bigint);
                var context_type = command1.Parameters.Add("@context_type", NpgsqlTypes.NpgsqlDbType.Text);
                var title = command1.Parameters.Add("@title", NpgsqlTypes.NpgsqlDbType.Text);
                var location_name = command1.Parameters.Add("@location_name", NpgsqlTypes.NpgsqlDbType.Text);
                var location_address = command1.Parameters.Add("@location_address", NpgsqlTypes.NpgsqlDbType.Text);
                var start_at = command1.Parameters.Add("@start_at", NpgsqlTypes.NpgsqlDbType.Date);
                var end_at = command1.Parameters.Add("@end_at", NpgsqlTypes.NpgsqlDbType.Date);
                var workflow_state = command1.Parameters.Add("@workflow_state", NpgsqlTypes.NpgsqlDbType.Text);
                var user_id = command1.Parameters.Add("@user_id", NpgsqlTypes.NpgsqlDbType.Bigint);
                var created_at = command1.Parameters.Add("@created_at", NpgsqlTypes.NpgsqlDbType.Date);
                var updated_at = command1.Parameters.Add("@updated_at", NpgsqlTypes.NpgsqlDbType.Date);
                var context_code = command1.Parameters.Add("@context_code", NpgsqlTypes.NpgsqlDbType.Text);
                var root_account_id = command1.Parameters.Add("@root_account_id", NpgsqlTypes.NpgsqlDbType.Bigint);

                NpgsqlCommand command2 = new NpgsqlCommand(sqlSection, connection);


                connection.Open();

                context_id.Value = courseNumber;
                context_type.Value = "Course";
                title.Value = canvasEvent.Title;
                location_name.Value = canvasEvent.LocationName;
                location_address.Value = canvasEvent.LocationAddress;
                workflow_state.Value = "active";
                start_at.Value = canvasEvent.StartAt;
                end_at.Value = canvasEvent.EndAt;
                user_id.Value = userId;
                created_at.Value = canvasEvent.CreatedAt;
                updated_at.Value = canvasEvent.UpdatedAt;
                context_code.Value = "Events-Desiderius";
                root_account_id.Value = rootAccId;
                command1.Prepare();

                command1.ExecuteNonQuery();

                var course_id = command2.Parameters.Add("@course_id", NpgsqlTypes.NpgsqlDbType.Bigint);
                root_account_id = command2.Parameters.Add("@root_account_id", NpgsqlTypes.NpgsqlDbType.Bigint);
                var name = command2.Parameters.Add("@name", NpgsqlTypes.NpgsqlDbType.Text);
                created_at = command2.Parameters.Add("@created_at", NpgsqlTypes.NpgsqlDbType.Date);
                updated_at = command2.Parameters.Add("@updated_at", NpgsqlTypes.NpgsqlDbType.Date);
                workflow_state = command2.Parameters.Add("@workflow_state", NpgsqlTypes.NpgsqlDbType.Text);
                course_id.Value = courseNumber;
                root_account_id.Value = rootAccId;
                name.Value = canvasEvent.Title;
                created_at.Value = canvasEvent.CreatedAt;
                updated_at.Value = canvasEvent.UpdatedAt;
                workflow_state.Value = "active";
                command2.Prepare();

                command2.ExecuteNonQuery();

                connection.Close();
                //var test = DateTime.Now.AddSeconds(-5);
                //InsertMUUID("EVENT", canvasEvent.UUID);


            }

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
                /*name.Value = canvasUser.Firstname + ", " + canvasUser.Lastname;
                sortable_name.Value = canvasUser.Sortable_name;
                workflow_state.Value = "registered";
                created_at.Value = canvasUser.CreatedAt;
                updated_at.Value = canvasUser.UpdatedAt;
                short_name.Value = canvasUser.Firstname + " " + canvasUser.Lastname;
                uuid.Value = canvasUser.UUID;*/

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






            /*static void Stuff()
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
            updated_at.Value = DateTime.Now.AddSeconds(-5);
            context_code.Value = "course_2";
            root_account_id.Value = 2;
            command.ExecuteNonQuery();
            connection.Close();


        }

    }*/

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

            static void InsertMUUID(String type, string uuid) {
                string constring1 = "Server=10.3.17.63,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string constring2 = "Server=10.3.17.64,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
                string sql = "INSERT INTO master (UUID,Source_EntityId,EntityType,Source " +
                    "VALUES(UUID_TO_BIN(@uuid),@source_entityid,@entitytype,@source); ";

                String source = "CANVAS";
                int source_id = 3;

                try
                {
                    using (MySqlConnection connection = new MySqlConnection(constring1))
                    {
                        MySqlCommand command = new MySqlCommand(sql, connection);
                        connection.Open();
                        //command.Prepare();
                        command.Parameters.AddWithValue("@uuid", uuid);
                        command.Parameters.AddWithValue("@source_entityid", source_id);
                        command.Parameters.AddWithValue("@entitytype", type);
                        command.Parameters.AddWithValue("@source", source);
                        command.Prepare();

                        command.ExecuteNonQuery();
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
                            command.Parameters.AddWithValue("@uuid", uuid);
                            command.Parameters.AddWithValue("@source_entityid", source_id);
                            command.Parameters.AddWithValue("@entitytype", type);
                            command.Parameters.AddWithValue("@source", source);
                            command.Prepare();

                            command.ExecuteNonQuery();
                            connection.Close();
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

        }
    } 
