using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace CommunicationSystemServer
{
    public class SQL //сделать асинхронными методы
    {
        ~SQL() => CloseDB();

        private static MySqlConnection database_connection;

        public static bool ConnectDB()
        {
            try
            {

                var builder = new MySqlConnectionStringBuilder
                {
                    Server = "server",
                    Database = "database_name",
                    UserID = "user",
                    Password = "password",
                    CharacterSet = "utf8",
                    SslMode = MySqlSslMode.Required,
                    ConnectionLifeTime = 0
                };

                database_connection = new MySqlConnection(builder.ConnectionString);
                database_connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                return false;
            }

        }

        public static void CloseDB()
        {
            if (database_connection != null)
                database_connection.Close();
        }

        public static List<List<string>> FullRead(string sql)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            List<List<string>> table_grid = new List<List<string>>();
            MySqlCommand command = new MySqlCommand(sql, database_connection);
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                List<string> row = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row.Add(reader[i].ToString());

                table_grid.Add(row);
            }

            reader.Close();
            return table_grid;
        }

        private static int SQLCounter(string sql)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            int counter = 0;
            MySqlCommand command = new MySqlCommand(sql, database_connection);
            MySqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
                counter++;
            reader.Close();

            return counter;
        }

        private static void SQLExecute(string sql) {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            new MySqlCommand(sql, database_connection).ExecuteNonQuery(); 
        }

        public static string Read(string sql)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            MySqlCommand command = new MySqlCommand(sql, database_connection);
            MySqlDataReader reader = command.ExecuteReader();
            reader.Read();
            string temp = reader[0].ToString();
            reader.Close();
            return temp;
        }

        public static void ChangeIP(string id, string IP)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            SQLExecute($"UPDATE Accounts SET remoteIP = '{IP}' WHERE id = {id}");
        }

        public static void AddIPToDB(string ip)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            SQLExecute($"DELETE FROM Servers WHERE IP LIKE '{ip}'");
            SQLExecute($"INSERT INTO Servers (id, IP, Workload) VALUES (NULL, '{ip}', 0)");
        }

        public static void DeleteIPFromDB(string ip)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            SQLExecute($"DELETE FROM Servers WHERE IP LIKE '{ip}'");
        }

        public static void UpdateWorkload(string IP, int bytes)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            SQLExecute($"UPDATE Servers SET Workload = {bytes} WHERE IP LIKE '{IP}'");
        }

        public static void ChangeIDArray(string UniqueKey, string idArr)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            SQLExecute($"UPDATE Lessons SET IdArr = {idArr} WHERE UniqueKey = {UniqueKey}");
        }

        public static void AddLink(string ClientID, string ClientIP, string ClientPort, string RoomID, string ServerIP, bool isDownload)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            //ClientID	ClientIP	ClientPort	RoomID	ServerIP	isDownload
            if (!isDownload)
                SQLExecute($"INSERT INTO Link_ClientSystem (id, ClientID, ClientIP, ClientPort, RoomID, ServerIP, isDownload) VALUES (NULL, {ClientID}, '{ClientIP}', {ClientPort}, {RoomID}, '{ServerIP}', 0)");
            else SQLExecute($"INSERT INTO Link_ClientSystem (id, ClientID, ClientIP, ClientPort, RoomID, ServerIP, isDownload) VALUES (NULL, {ClientID}, '{ClientIP}', {ClientPort}, {RoomID}, '{ServerIP}', 1)");

        }
        public static void DeleteLink(string ClientID)
        {
            if (database_connection.State != System.Data.ConnectionState.Open)
            {
                ConnectDB();
            }
            SQLExecute($"DELETE FROM Link_ClientSystem WHERE ClientID = {ClientID}");
        }
    }
}
