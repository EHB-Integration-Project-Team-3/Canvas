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
                @event canvasEvent;
                canvasEvent = Xmlcontroller.DeserializeXmlString<@event>(message);
                switch (canvasEvent.header.method) {
                    case eventHeaderMethod.CREATE:
                        try
                        {
                            if (CheckMUUID(canvasEvent.uuid) == false) { CreateEvent(canvasEvent); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }


                        break;
                    case eventHeaderMethod.UPDATE:
                        try
                        {
                            if (UpdateMUUID(canvasEvent.uuid, canvasEvent.entityVersion)) { UpdateEvent(canvasEvent); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case eventHeaderMethod.DELETE:
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



                Console.WriteLine(" [x] Received Event " + canvasEvent.header.method + " " + canvasEvent.title);
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
                user canvasUser;

                canvasUser = Xmlcontroller.DeserializeXmlString<user>(message);
                Console.WriteLine(message);
                switch (canvasUser.header.method)
                {
                    case userHeaderMethod.CREATE:
                        try
                        {

                            if (CheckMUUID(canvasUser.uuid) == false) { CreateUser(canvasUser); }
                            else { createUserOnlyEnrollment(canvasUser); }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case userHeaderMethod.UPDATE:
                        
                            try
                            {
                                if (UpdateMUUID(canvasUser.uuid, canvasUser.entityVersion)) { UpdateUser(canvasUser); }
                                
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        



                        break;
                    case userHeaderMethod.DELETE:
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

                Console.WriteLine(" [x] Received User " + canvasUser.header.method + " " + canvasUser.lastName + " " + canvasUser.firstName);
            };
            channel.BasicConsume(queue: "to-canvas_user-queue",
                                 autoAck: true,
                                 consumer: consumerUser);
            
            //Attendance Consumer
            channel3.QueueDeclare(queue: "to-canvas_attendance-queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumerAttendance = new EventingBasicConsumer(channel3);
            consumerAttendance.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                attendance canvasAttendance;

                canvasAttendance = Xmlcontroller.DeserializeXmlString<attendance>(message);
                Console.WriteLine(message);
                switch (canvasAttendance.header.method)
                {
                    case attendanceHeaderMethod.CREATE:
                        try
                        {
                            if (CheckMUUID(canvasAttendance.uuid) == false)
                            {
                                CreateAttendence(canvasAttendance);
                            }   }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        break;
                    case attendanceHeaderMethod.DELETE:
                        
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
                    
                    default:
                        Console.WriteLine("header reading failed");
                        break;

                }

                Console.WriteLine(" [x] Received Attendance ");
            };
            channel.BasicConsume(queue: "to-canvas_attendance-queue",
                                 autoAck: true,
                                 consumer: consumerAttendance);





            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();






        }


        static void CreateEvent(@event canvasEvent)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlEvent = "INSERT INTO public.calendar_events(id,user_id,context_code,context_id,start_at,end_at,context_type,title,location_name,location_address,workflow_state,created_at,updated_at,root_account_id,uuid) VALUES " +
                "(nextval ('calendar_events_id_seq'::regclass),@user_id,@context_code,@context_id,@start_at,@end_at,@context_type,@title,@location_name,@location_address,@workflow_state,@created_at,@updated_at,@root_account_id,@uuid)";
            String sqlSection = "INSERT INTO public.course_sections(id,course_id,root_account_id,name,created_at,updated_at,workflow_state) VALUES " +
                "(nextval ('course_sections_id_seq'::regclass),(select id from courses where name = 'Events'),@root_account_id,@name,@created_at,@updated_at,@workflow_state)";

            int courseNumber = 5;
            int rootAccId = 2;
            int localId = FetchLocalId(canvasEvent.organiserId,0);


            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("@user_id", localId);
                    command.Parameters.AddWithValue("@context_code", "course_5");
                    command.Parameters.AddWithValue("@context_id", courseNumber);
                    command.Parameters.AddWithValue("@start_at", canvasEvent.start);
                    command.Parameters.AddWithValue("@end_at", canvasEvent.end);
                    command.Parameters.AddWithValue("@context_type", "Course");
                    command.Parameters.AddWithValue("@title", canvasEvent.title);
                    command.Parameters.AddWithValue("@location_name", canvasEvent.location);
                    command.Parameters.AddWithValue("@location_address", canvasEvent.location);
                    command.Parameters.AddWithValue("@workflow_state", "active");
                    command.Parameters.AddWithValue("@created_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@root_account_id", rootAccId);
                    command.Parameters.AddWithValue("@uuid", canvasEvent.uuid);


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
                        command.Parameters.AddWithValue("@name", canvasEvent.title);
                        command.Parameters.AddWithValue("@created_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@workflow_state", "active");

                        command.CommandText = sqlSection;
                        connectionSection.Open();
                        command.ExecuteNonQuery();
                        connectionSection.Close();

                    }
                }
            

            InsertMUUID("USER", canvasEvent.uuid, localId);



        }
        static void UpdateEvent(@event canvasEvent)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlEvent = "UPDATE public.calendar_events SET user_id=@user_id,start_at=@start_at,end_at=@end_at,title=@title,location_name=@location_name,location_address=@location_address,updated_at=@updated_at,entityversion=entityversion+1" +
                            "WHERE uuid=@uuid";
            String sqlSection = "UPDATE course_sections SET updated_at=@updated_at,name=@name WHERE name=@oldname";
            

            int localId = FetchLocalId(canvasEvent.uuid,0);
            String localName = FetchLocalName(canvasEvent.uuid);

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("@user_id", localId);
                    command.Parameters.AddWithValue("@start_at", canvasEvent.start);
                    command.Parameters.AddWithValue("@end_at", canvasEvent.end);
                    command.Parameters.AddWithValue("@title", canvasEvent.title);
                    command.Parameters.AddWithValue("@location_name", canvasEvent.location);
                    command.Parameters.AddWithValue("@location_address", canvasEvent.location);
                    command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@uuid", canvasEvent.uuid);
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
                    command.Parameters.AddWithValue("@name", canvasEvent.title);
                    command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@oldname", localName);
                    command.CommandText = sqlSection;
                    connectionSection.Open();
                    command.ExecuteNonQuery();
                    connectionSection.Close();
                }
            }


            }
            static void DeleteEvent(@event canvasEvent) {

            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlEvent = "UPDATE public.calendar_events SET deleted_at=@deleted_at,updated_at=@updated_at,workflow_state=@workflow_state WHERE uuid=@uuid";
            String sqlSection = "UPDATE public.course_sections SET updated_at=@updated_at,workflow_state=@workflow_state WHERE name=@name";
            int localId = FetchLocalId(canvasEvent.uuid,0);

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                   
                    command.Parameters.AddWithValue("@deleted_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@workflow_state", "deleted");
                    command.Parameters.AddWithValue("@uuid", canvasEvent.uuid);

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

                    command.Parameters.AddWithValue("@deleted_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@workflow_state", "deleted");
                    command.Parameters.AddWithValue("@name", canvasEvent.title);
                    command.CommandText = sqlSection;
                    connectionSection.Open();
                    command.ExecuteNonQuery();
                    connectionSection.Close();
                }
            }
            DeleteMUUID(canvasEvent.uuid);
        }

            static void CreateUser(@user canvasUser) {
                String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
                String sqlCreate = "INSERT INTO public.users(id,name,sortable_name,workflow_state,uuid,created_at,updated_at,short_name) VALUES " +
        "(nextval ('users_id_seq'::regclass),@name,@sortable_name,@workflow_state,@uuid,@created_at,@updated_at,@short_name)";
            
            String sqlEnroll = "INSERT INTO enrollments(id,user_id,course_id,type,uuid,workflow_state,created_at,updated_at,course_section_id,root_account_id,role_id) VALUES " +
    "(nextval ('users_id_seq'::regclass),(select id from users where uuid=@useruuid),(select id from courses where name='Events'),@type,@uuid,@workflow_state,@created_at,@updated_at," +
    "(select id from course_sections where name=@eventuuid),@root_account_id,@role_id)";


            String roleTitle = "student";
            int roleId = 19;

            if (roleTitle.Equals("student")) { roleTitle = "StudentEnrollment";}
            else { roleTitle = "TeacherEnrollment"; roleId = 20; }

            try
            {
                using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
                {

                    using (NpgsqlCommand command = connectionSection.CreateCommand())
                    {

                        command.Parameters.AddWithValue("@name", canvasUser.lastName + " " + canvasUser.firstName);
                        command.Parameters.AddWithValue("@sortable_name", canvasUser.lastName + ", " + canvasUser.firstName);
                        command.Parameters.AddWithValue("@workflow_state", "registered");
                        command.Parameters.AddWithValue("@created_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@short_name", canvasUser.firstName + ", " + canvasUser.lastName);
                        command.Parameters.AddWithValue("@uuid", canvasUser.uuid);

                        command.CommandText = sqlCreate;
                        connectionSection.Open();
                        command.ExecuteNonQuery();
                        connectionSection.Close();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            try
            {
                using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
                {
                    try { }catch(Exception ex) { Console.WriteLine(ex); }
                    using (NpgsqlCommand command = connectionSection.CreateCommand())
                    {

                        command.Parameters.AddWithValue("@useruuid", canvasUser.uuid);
                        command.Parameters.AddWithValue("@type", roleTitle);
                        command.Parameters.AddWithValue("@uuid", canvasUser.uuid);
                        command.Parameters.AddWithValue("@workflow_state", "active");
                        command.Parameters.AddWithValue("@created_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@eventuuid", "Events-Desiderius");
                        command.Parameters.AddWithValue("@root_account_id", 2);
                        command.Parameters.AddWithValue("@role_id", roleId);



                        command.CommandText = sqlEnroll;
                        connectionSection.Open();
                        //command.ExecuteNonQuery();
                        using (NpgsqlDataReader reader = command.ExecuteReader()) { while (reader.Read()) { Console.WriteLine(reader.GetInt32(0)); } }
                        connectionSection.Close();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }


            int localId = FetchLocalId(canvasUser.uuid, 1);

            InsertMUUID("USER", canvasUser.uuid, localId);

        }

        static void createUserOnlyEnrollment(@user canvasUser) {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            
            String sqlEnroll = "INSERT INTO enrollments(id,user_id,course_id,type,uuid,workflow_state,created_at,updated_at,course_section_id,root_account_id,role_id) VALUES " +
    "(nextval ('users_id_seq'::regclass),(select id from users where uuid=@useruuid),(select id from courses where name='Events'),@type,@uuid,@workflow_state,@created_at,@updated_at," +
    "(select id from course_sections where name=@eventuuid),@root_account_id,@role_id)";


            String roleTitle = "student";
            int roleId = 19;

            try
            {
                using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
                {
                    try { } catch (Exception ex) { Console.WriteLine(ex); }
                    using (NpgsqlCommand command = connectionSection.CreateCommand())
                    {

                        command.Parameters.AddWithValue("@useruuid", canvasUser.uuid);
                        command.Parameters.AddWithValue("@type", roleTitle);
                        command.Parameters.AddWithValue("@uuid", canvasUser.uuid);
                        command.Parameters.AddWithValue("@workflow_state", "active");
                        command.Parameters.AddWithValue("@created_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@eventuuid", "Events-Desiderius");
                        command.Parameters.AddWithValue("@root_account_id", 2);
                        command.Parameters.AddWithValue("@role_id", roleId);



                        command.CommandText = sqlEnroll;
                        connectionSection.Open();
                        //command.ExecuteNonQuery();
                        using (NpgsqlDataReader reader = command.ExecuteReader()) { while (reader.Read()) { Console.WriteLine(reader.GetInt32(0)); } }
                        connectionSection.Close();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }
        static void UpdateUser(user canvasUser)
            {
                String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
                String sqlUpdate = "UPDATE public.users SET " +
                "name=@name," +
                "sortable_name=@sortable_name," +
                "updated_at=@updated_at," +
                "short_name=@short_name," +
                "entityversion=entityversion+1 " +
                "WHERE uuid=@uuid";

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {

                using (NpgsqlCommand command = connection.CreateCommand())
                {

                    command.Parameters.AddWithValue("@name", canvasUser.lastName + " " + canvasUser.firstName);
                    command.Parameters.AddWithValue("@sortable_name", canvasUser.lastName + ", " + canvasUser.firstName);
                    command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                    command.Parameters.AddWithValue("@short_name", canvasUser.firstName + ", " + canvasUser.lastName);
                    command.Parameters.AddWithValue("@uuid", canvasUser.uuid);

                    command.CommandText = sqlUpdate;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }
        static void DeleteUser(@user canvasUser) {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";

            String sqlDelete = "UPDATE public.users SET deleted_at=@deleted_at,updated_at=@updated_at,workflow_state=@workflow_state" +
        " WHERE uuid=@uuid";

            using (NpgsqlConnection connection = new NpgsqlConnection(constring))
            {
                try
                {
                    using (NpgsqlCommand command = connection.CreateCommand())
                    {

                        command.Parameters.AddWithValue("@deleted_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@workflow_state", "deleted");
                        command.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        command.Parameters.AddWithValue("@uuid", canvasUser.uuid);

                        command.CommandText = sqlDelete;
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }

            }
        }
        static void CreateAttendence(attendance attendance)
        {
            String constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
            String sqlCreate = "INSERT INTO enrollments(id,user_id,course_id,type,uuid,workflow_state,created_at,updated_at,course_section_id,root_account_id,role_id) VALUES " +
    "(nextval ('users_id_seq'::regclass),(select id from users where uuid=@useruuid),(select id from courses where name='Events'),(select type from enrollments where user_id=@userid limit 1),@uuid,@workflow_state,@created_at,@updated_at,(select id from course_sections where name=(select title from calendar_events where uuid=@eventuuid)),@root_account_id,(select role_id from enrollments where user_id=@userid limit 1))";

            

            using (NpgsqlConnection connectionSection = new NpgsqlConnection(constring))
            {
                try
                {
                    using (NpgsqlCommand command = connectionSection.CreateCommand())
                    {

                        command.Parameters.AddWithValue("@useruuid", attendance.attendeeId);
                        command.Parameters.AddWithValue("@uuid", attendance.uuid);
                        command.Parameters.AddWithValue("@workflow_state", "active");
                        command.Parameters.AddWithValue("@created_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@updated_at", DateTime.Now.AddSeconds(-10).AddHours(-2));
                        command.Parameters.AddWithValue("@eventuuid", attendance.eventId);
                        command.Parameters.AddWithValue("@root_account_id", 2);



                        command.CommandText = sqlCreate;
                        connectionSection.Open();
                        command.ExecuteNonQuery();
                        connectionSection.Close();
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }

            }
            int localId = FetchLocalId(attendance.uuid, 2);

            InsertMUUID("ATTENDANCE", attendance.uuid, localId);

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
                        command.Parameters.AddWithValue("@uuid", uuid);
                        command.Parameters.AddWithValue("@entityVersion", entityVersion);
                        command.Parameters.AddWithValue("@source", source);

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
                            command.Parameters.AddWithValue("@uuid", uuid);
                            command.Parameters.AddWithValue("@entityVersion", entityVersion);
                            command.Parameters.AddWithValue("@source", source);

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
                        command.Parameters.AddWithValue("@uuid", uuid);
                        command.Parameters.AddWithValue("@source", source);

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
                            command.Parameters.AddWithValue("@uuid", uuid);
                            command.Parameters.AddWithValue("@source", source);

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
            if (type > 1) { location = "public.enrollments"; }
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
