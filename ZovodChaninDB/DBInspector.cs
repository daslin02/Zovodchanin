using Microsoft.Data.SqlClient;
using System.Data.SQLite;
using System.IO;

namespace DBInspector
{
    public class DBAccount
    {
        private bool isZero = false;
        public string Name { get; set; }
        public string ID { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Group { get; set; }

        public DBAccount SetZero()
        {
            Name = "Null";
            ID = "Null";
            Password = "Null";
            isZero = true;
            Role = "null";
            Group = "null";
            return this;
        }
        public bool IsValid()
        {
            return !isZero;
        }
    }

    public class DB
    {
        public static DB _instance;
        public static bool iSLocalDataBase = false;
        private static readonly object _lock = new object();
        private SqlConnection SQLConnection;
        private SQLiteConnection LocalConnection;
        private string Connection = "Server = DESKTOP-99N705J;Database=ZovodChanin;Integrated Security = True; Encrypt=False";
        private string LocalConnectionString = "Data Source=LocalDB.db;Version=3;";

        // table
        public string AccountTable = "UserAccount";

        private DB()
        {
        }

        public static DB Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DB();
                        }
                    }
                }
                return _instance;
            }
        }

        public void Connect()
        {
            if (iSLocalDataBase)
            {
                Console.WriteLine("[SQLite] Conect local DataBase");
                ConnectLocal();
                return;
            }
            SQLConnection = new SqlConnection(Connection);
            SQLConnection.Open();
        }

        /// <summary>
        /// Create by needed  and connect Database ,table
        /// </summary>
        private void ConnectLocal()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalDB.db");

            bool needCreateTables = false;

            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"[SQLite] Create local DataBase at: {dbPath}");
                SQLiteConnection.CreateFile(dbPath);
                needCreateTables = true;
            }
            else
            {
                Console.WriteLine($"[SQLite] БД существует, проверяем таблицы...");
                needCreateTables = !CheckIfTablesExist(dbPath);
            }

            LocalConnection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            LocalConnection.Open();

            if (needCreateTables)
            {
                Console.WriteLine($"[SQLite] Создаём таблицы...");
                CreateLocalTables(LocalConnection);
            }

            Console.WriteLine($"[SQLite] Готово! Таблицы есть: {!needCreateTables}");
        }
        /// <summary>
        /// check if tabel don't create 
        /// </summary>
        /// <param name="dbPath"></param>
        /// <returns></returns>
        private bool CheckIfTablesExist(string dbPath)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name='UserAccount'";
                    using (var cmd = new SQLiteCommand(sql, connection))
                    {
                        var result = cmd.ExecuteScalar();
                        bool exists = result != null;
                        Console.WriteLine($"[SQLite] Таблица UserAccount существует: {exists}");
                        return exists;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] Ошибка проверки таблиц: {ex.Message}");
                return false;
            }
        }

        private void CreateLocalTables(SQLiteConnection connection)
        {
            try
            {
                string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS UserAccount (
                ID TEXT PRIMARY KEY,
                UserName TEXT NOT NULL,
                Password TEXT NOT NULL,
                Role TEXT DEFAULT 'user',
                Groups TEXT DEFAULT ''
            )";

                using (var cmd = new SQLiteCommand(createTableQuery, connection))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"[SQLite] Таблица UserAccount создана");
                }

                string checkQuery = "SELECT COUNT(*) FROM UserAccount";
                using (var cmd = new SQLiteCommand(checkQuery, connection))
                {
                    var count = cmd.ExecuteScalar();
                    Console.WriteLine($"[SQLite] В таблице UserAccount записей: {count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SQLite] ОШИБКА создания таблиц: {ex.Message}");
                throw;
            }
        }

        private DBAccount RegisterLocal(string ID, string Password)
        {
            try
            {
                string sql = $"SELECT * FROM {AccountTable} WHERE ID = @id";
                using (var cmd = new SQLiteCommand(sql, LocalConnection))
                {
                    cmd.Parameters.AddWithValue("@id", ID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string userName = reader["UserName"].ToString();
                            string userId = reader["ID"].ToString();
                            string pass = reader["Password"].ToString();
                            string role = reader["Role"].ToString();
                            string group = reader["Groups"].ToString();

                            DBAccount acc = new DBAccount();
                            acc.ID = userId;
                            acc.Name = userName;
                            acc.Password = pass;
                            acc.Role = role;
                            acc.Group = group;

                            if (Password == pass && ID == userId)
                            {
                                return acc;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local Register Error: {ex.Message}");
            }
            return new DBAccount().SetZero();
        }

        public DBAccount Register(string ID, string Password)
        {
            if (iSLocalDataBase)
            {
                return RegisterLocal(ID, Password);
            }
            string sql = $"SELECT * FROM {AccountTable} WHERE ID = {ID}";
            SqlCommand cmd;
            using (cmd = new SqlCommand(sql, SQLConnection)) ;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    string userName = reader["UserName"].ToString();
                    string userId = reader["ID"].ToString();
                    string pass = reader["Password"].ToString();
                    string role = reader["Role"].ToString();
                    string group = reader["Groups"].ToString();

                    DBAccount acc = new DBAccount();
                    acc.ID = userId;
                    acc.Name = userName;
                    acc.Password = pass;
                    acc.Role = role;
                    acc.Group = group;

                    if (Password == pass && ID == userId)
                    {
                        return acc;
                    }
                    return new DBAccount().SetZero();
                }
            }
            return new DBAccount().SetZero();
        }

        private DBAccount GetAccountByIDLocal(string ID)
        {
            try
            {
                string sql = $"SELECT * FROM {AccountTable} WHERE ID = @id";
                using (var cmd = new SQLiteCommand(sql, LocalConnection))
                {
                    cmd.Parameters.AddWithValue("@id", ID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string userName = reader["UserName"].ToString();
                            string userId = reader["ID"].ToString();
                            string pass = reader["Password"].ToString();
                            string role = reader["Role"].ToString();
                            string group = reader["Groups"].ToString();

                            DBAccount acc = new DBAccount();
                            acc.ID = userId;
                            acc.Name = userName;
                            acc.Password = pass;
                            acc.Role = role;
                            acc.Group = group;
                            return acc;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local GetAccount Error: {ex.Message}");
            }
            return new DBAccount().SetZero();
        }

        public DBAccount GetAccountByID(string ID)
        {
            if (iSLocalDataBase)
            {
                return GetAccountByIDLocal(ID);
            }
            string sql = $"SELECT * FROM {AccountTable} WHERE ID = {ID}";
            SqlCommand cmd;
            using (cmd = new SqlCommand(sql, SQLConnection)) ;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    string userName = reader["UserName"].ToString();
                    string userId = reader["ID"].ToString();
                    string pass = reader["Password"].ToString();
                    string role = reader["Role"].ToString();
                    string group = reader["Groups"].ToString();
                    DBAccount acc = new DBAccount();
                    acc.ID = userId;
                    acc.Name = userName;
                    acc.Password = pass;
                    acc.Role = role;
                    acc.Group = group;
                    return acc;
                }
            }
            return new DBAccount().SetZero();
        }

        private bool CheckGroupByIDLocal(string ID, string gr)
        {
            try
            {
                string sql = $"SELECT Groups FROM {AccountTable} WHERE ID = @id";
                using (var cmd = new SQLiteCommand(sql, LocalConnection))
                {
                    cmd.Parameters.AddWithValue("@id", ID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string group = reader["Groups"].ToString();
                            foreach (string str in group.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (gr == str)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local CheckGroup Error: {ex.Message}");
            }
            return false;
        }

        public bool CheckGroupByID(string ID, string gr)
        {
            if (iSLocalDataBase)
            {
                return CheckGroupByIDLocal(ID, gr);
            }
            string sql = $"SELECT * FROM {AccountTable} WHERE ID = {ID}";
            SqlCommand cmd;
            using (cmd = new SqlCommand(sql, SQLConnection)) ;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    string userName = reader["UserName"].ToString();
                    string userId = reader["ID"].ToString();
                    string pass = reader["Password"].ToString();
                    string role = reader["Role"].ToString();
                    string group = reader["Groups"].ToString();

                    DBAccount acc = new DBAccount();
                    acc.ID = userId;
                    acc.Name = userName;
                    acc.Password = pass;
                    acc.Role = role;
                    acc.Group = group;

                    foreach (string str in group.Split(";"))
                    {
                        if (gr == str)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private DBAccount[] GetAllAccountsLocal()
        {
            List<DBAccount> accountsList = new List<DBAccount>();
            try
            {
                string sql = $"SELECT ID, UserName, Password FROM {AccountTable}";
                using (var cmd = new SQLiteCommand(sql, LocalConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string userName = reader["UserName"].ToString();
                            string userId = reader["ID"].ToString();
                            string pass = reader["Password"].ToString();
                            DBAccount acc = new DBAccount();
                            acc.ID = userId;
                            acc.Name = userName;
                            acc.Password = pass;
                            accountsList.Add(acc);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local GetAllAccounts Error: {ex.Message}");
            }
            return accountsList.ToArray();
        }

        public DBAccount[] GetAllAccounts()
        {
            if (iSLocalDataBase)
            {
                return GetAllAccountsLocal();
            }
            List<DBAccount> accountsList = new List<DBAccount>();

            string sql = $"SELECT * FROM {AccountTable}";
            SqlCommand cmd;
            using (cmd = new SqlCommand(sql, SQLConnection)) ;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string userName = reader["UserName"].ToString();
                    string userId = reader["ID"].ToString();
                    string pass = reader["Password"].ToString();
                    DBAccount acc = new DBAccount();
                    acc.ID = userId;
                    acc.Name = userName;
                    acc.Password = pass;
                    accountsList.Add(acc);
                }
            }
            return accountsList.ToArray();
        }

        private static bool checkConnecttionLocal()
        {
            try
            {
                using (var connection = new SQLiteConnection(Instance.LocalConnectionString))
                {
                    connection.Open();
                    connection.Close();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local Connection Error: {ex.Message}");
                return false;
            }
        }

        public static bool checkConnecttion()
        {
            if (iSLocalDataBase)
            {
                return checkConnecttionLocal();
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(Instance.Connection))
                {
                    connection.Open();
                    connection.Close();
                    return true;
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Ошибка SQL: {ex.Message}");
                Console.WriteLine($"Номер ошибки: {ex.Number}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }


        public void AddTestUserLocal(string id, string name, string password, string role = "user", string groups = "")
        {
            if (!iSLocalDataBase) return;

            try
            {
                string sql = @"INSERT OR REPLACE INTO UserAccount (ID, UserName, Password, Role, Groups) 
                               VALUES (@id, @name, @pass, @role, @groups)";
                using (var cmd = new SQLiteCommand(sql, LocalConnection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@pass", password);
                    cmd.Parameters.AddWithValue("@role", role);
                    cmd.Parameters.AddWithValue("@groups", groups);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddTestUser Error: {ex.Message}");
            }
        }
    
    private bool CanUserWriteToGroupLocal(string ID, string groupName)
        {
            try
            {
                string sql = $"SELECT Groups, Role FROM {AccountTable} WHERE ID = @id";
                using (var cmd = new SQLiteCommand(sql, LocalConnection))
                {
                    cmd.Parameters.AddWithValue("@id", ID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string groups = reader["Groups"].ToString();
                            string role = reader["Role"].ToString();

                           
                            if (role.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                                role.Equals("moderator", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }

                            foreach (string str in groups.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (groupName.Equals(str.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local CanUserWriteToGroup Error: {ex.Message}");
            }
            return false;
        }

        private bool CanUserWriteToGroupSSMS(string ID, string groupName)
        {
            try
            {
                string sql = $"SELECT Groups, Role FROM {AccountTable} WHERE ID = @id";
                using (var cmd = new SqlCommand(sql, SQLConnection))
                {
                    cmd.Parameters.AddWithValue("@id", ID);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string groups = reader["Groups"].ToString();
                            string role = reader["Role"].ToString();

                            // Администраторы и модераторы могут писать в любые группы
                            if (role.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                                role.Equals("moderator", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }

                            // Проверяем, есть ли группа в списке доступа
                            foreach (string str in groups.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (groupName.Equals(str.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CanUserWriteToGroup Error: {ex.Message}");
            }
            return false;
        }


        public bool CanUserWriteToGroup(string ID, string groupName)
        {
            if (iSLocalDataBase)
            {
                return CanUserWriteToGroupLocal(ID, groupName);
            }
            else
            {
                return CanUserWriteToGroupSSMS(ID, groupName);
            }
        }


        public bool CanUserReadGroup(string ID, string groupName)
        {
            return CheckGroupByID(ID, groupName);
        }
    } 
}