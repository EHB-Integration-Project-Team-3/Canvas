using MySql.Data.MySqlClient;
using Npgsql;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args) {
            string constring1 = "Server=10.3.17.63,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
            string constring2 = "Server=10.3.17.64,3306;Database=masteruuid;User ID = muuid; Password = muuid;";
            string sql = "INSERT INTO master (UUID,Source_EntityId,EntityType,Source " +
                "VALUES(UUID_TO_BIN(@uuid),@source_entityid,@entitytype,@source); ";

            String source = "CANVAS";
            String uuid = "test";
            String localId = "test";
            String type = "TEST";
            try
            {
                using (MySqlConnection connection = new MySqlConnection(constring1))
                {
                    MySqlCommand command = new MySqlCommand(sql, connection);
                    connection.Open();
                    /*
                    command.Parameters.Add("@uuid", MySqlDbType.VarChar);
                    command.Parameters.Add("@source_entityid", MySqlDbType.VarChar);
                    command.Parameters.Add("@entitytype", MySqlDbType.VarChar);
                    command.Parameters.Add("@source", MySqlDbType.VarChar);

                    command.Parameters["@uuid"].Value = uuid;
                    command.Parameters["@source_entityid"].Value = localId;
                    command.Parameters["@entitytype"].Value = type;
                    command.Parameters["@source"].Value = source;*/
                    command.Parameters.AddWithValue("@uuid",uuid);
                    command.Parameters.AddWithValue("@source_entityid",localId);
                    command.Parameters.AddWithValue("@entitytype",type);
                    command.Parameters.AddWithValue("@source",source);

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
                        command.Parameters.Add("@uuid", MySqlDbType.VarChar);
                        command.Parameters.Add("@source_entityid", MySqlDbType.VarChar);
                        command.Parameters.Add("@entitytype", MySqlDbType.VarChar);
                        command.Parameters.Add("@source", MySqlDbType.VarChar);

                        command.Parameters["@uuid"].Value = uuid;
                        command.Parameters["@source_entityid"].Value = localId;
                        command.Parameters["@entitytype"].Value = type;
                        command.Parameters["@source"].Value = source;
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
    }
}
