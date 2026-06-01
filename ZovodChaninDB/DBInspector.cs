using Microsoft.Data.SqlClient;

namespace DBInspector
{
    public class DBAccount
    {
        private bool isZero = false;
        public string Name { get; set; }
        public string ID { get; set; }
        public string Password { get; set; }
        public string Role{ get; set; }
        public string Group{ get; set; }

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
        private static readonly object _lock = new object();
        private SqlConnection SQLConnection;
        private string Connection = "Server = DESKTOP-99N705J;Database=ZovodChanin;Integrated Security = True; Encrypt=False";

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
            SQLConnection = new SqlConnection(Connection);
            SQLConnection.Open();
        }
        public DBAccount Register(string ID , string Password) 
        {
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
        public DBAccount GetAccountByID(string ID) 
        {
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
        public bool CheckGroupByID(string ID , string gr) 
        {
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
        public DBAccount[] GetAllAccounts() 
        {
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
        public static bool checkConnecttion()
        {
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
    }
}

