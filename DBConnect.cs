
using System;
using MySql.Data.MySqlClient;

namespace Util
{
    public class DBConnect
    {
        private string server;
        private string database;
        private string uid;
        private string password;
        private MySqlConnection _connection;

        public MySqlConnection Connection { get => _connection; private set => _connection = value; }

        public DBConnect()
        {
            Initialize();            
        }

        private void Initialize()
        {
            this.server = "localhost";
            this.database = "demodb";
            this.uid = "carloseduardo";
            this.password = "";

            string connectionString = "SERVER=" + this.server + ";" + "DATABASE=" +
            this.database + ";" + "UID=" + this.uid + ";" + "PASSWORD=" + this.password + ";";

            this.Connection = new MySqlConnection(connectionString);
        }
    }
}