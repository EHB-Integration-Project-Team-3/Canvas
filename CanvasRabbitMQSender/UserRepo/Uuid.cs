using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace CanvasRabbitMQReceiver.UserRepo
{
    class Uuid
    {
        private static string constring = "Host=10.3.17.67; Database = canvas_development; User ID = postgres; Password = ubuntu123;";
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





        public static string MakeUserUUID(int id) //USER
        {
            string uuid;
            uuid = GetUUID();
            string sql = "UPDATE public.users SET MUUID = @uuid where id = @id";
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


        public static string MakeUserUUID(string id)//USER
        {
            string uuid;
            uuid = GetUUID();
            string sql = "UPDATE public.users SET MUUID = @uuid where uuid = @id";
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
    }
}
