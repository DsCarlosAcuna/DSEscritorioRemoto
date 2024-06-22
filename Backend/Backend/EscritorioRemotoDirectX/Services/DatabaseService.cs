using System;
using Microsoft.Data.Sqlite;

namespace EscritorioRemotoDirectX.Services
{
    public class DatabaseService
    {
        private string _connectionString;

        public DatabaseService(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
        }

        public (string ip, int port) GetConnectionDetails(string pcName, string username, string password)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT Pc.ip, Pc.port
                    FROM User
                    JOIN UserPc ON User.id = UserPc.userId
                    JOIN Pc ON Pc.id = UserPc.pcId
                    WHERE User.name = @userName AND User.password = @password AND Pc.name = @pcName";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@pcName", pcName);
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string ip = reader["ip"].ToString();
                            int port = Convert.ToInt32(reader["port"]);
                            return (ip, port);
                        }
                        else
                        {
                            throw new Exception("Credenciales o nombre de PC no válidos");
                        }
                    }
                }
            }
        }
    }
}
