using Microsoft.VisualBasic;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static ZNetwork.ZJSON;

namespace ZNetwork
{
    public class ZJSON
    {
       public MessageSerializer serializer = new MessageSerializer();

        private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        /// <summary>
        /// base class from message
        /// </summary>
        public class BaseMassage
        {
            public string TypeMessage { get; set; }
        }
        /// <summary>
        /// class For send info to server Register
        /// </summary>
        public class MessageRequestRegister : BaseMassage
        {
            public MessageRequestRegister() => TypeMessage = nameof(MessageRequestRegister);
            public string login { get; set; }
            public string Password { get; set; }
        }
        /// <summary>
        /// class for send info user Register
        /// </summary>
        public class MessageResponseRegister : BaseMassage
        {
            public MessageResponseRegister() => TypeMessage = nameof(MessageResponseRegister);
            public bool iSSuccses { get; set; }
            public string ID { get; set; } = "";
            public string Name { get; set; }
            public string Roles { get; set; }
            public string Groups { get; set; }

        }
        /// <summary>
        /// class SystemMessage From server
        /// </summary>
        public class MessageSystemInfo : BaseMassage
        {
            public MessageSystemInfo() => TypeMessage = nameof(MessageSystemInfo);

            public string Code { get; set; } // code error
            public string info { get; set; } // description info of error

        }
        /// <summary>
        /// Send message Client 
        /// </summary>
        public class MessageSendData : BaseMassage
        {
            public MessageSendData() => TypeMessage = nameof(MessageSendData);
            public string Message { get; set; }

            public string ID { get; set; }
            /// <summary>
            /// this is Group Replicated Message
            /// </summary>
            public string Channel { get; set; }
        }
        /// <summary>
        /// Received message on client 
        /// </summary>
        public class MessageReceivedData : BaseMassage
        {
            public MessageReceivedData() => TypeMessage = nameof(MessageReceivedData);
            public string NameSender { get; set; }
            /// <summary>
            /// this is Group Replicated Message
            /// </summary>
            public string Channels { get; set; }
            /// <summary>
            /// Time Send message
            /// </summary>
            public DateTime SendTime { get; set; } = DateTime.Now;
            public string RolesSender { get; set; }
            public string Message { get; set; }

        }
        /// <summary>
        /// Service for work serialization or deserialization
        /// </summary>
        public class MessageSerializer
        {
            private readonly JsonSerializerOptions _options;

            public MessageSerializer()
            {
                _options = new JsonSerializerOptions
                {
                    //PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // name property camelCase
                    PropertyNamingPolicy = null,
                    WriteIndented = false, // compact json for network
                    IncludeFields = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // don't send null property
                };
            }

            /// <summary>
            /// Serialize any message into json
            /// </summary>
            public string Serialize(BaseMassage message)
            {
                if (message == null)
                    throw new ArgumentNullException(nameof(message));

                // auto correct TypeMessage
                if (string.IsNullOrEmpty(message.TypeMessage))
                {
                    message.TypeMessage = message.GetType().Name;
                }

                return JsonSerializer.Serialize(message, message.GetType(), _options);
            }

            /// <summary>
            /// deserialization any hson into message
            /// </summary>
            public BaseMassage? Deserialize(string json)
            {
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                try
                {
                    // parse json so get TypeMessage (RootElement)
                    using JsonDocument doc = JsonDocument.Parse(json);
                    JsonElement root = doc.RootElement;

                    // try get TypeMessage
                    if (!root.TryGetProperty("typeMessage", out JsonElement typeElement) &&
                        !root.TryGetProperty("TypeMessage", out typeElement))
                    {
                        throw new JsonException("Message doesn't contain TypeMessage field");
                    }

                    string messageType = typeElement.GetString() ?? string.Empty;

                    // deserialiaze Type class message
                    return messageType switch
                    {
                        nameof(MessageRequestRegister) => JsonSerializer.Deserialize<MessageRequestRegister>(json, _options),
                        nameof(MessageResponseRegister) => JsonSerializer.Deserialize<MessageResponseRegister>(json, _options),
                        nameof(MessageSystemInfo) => JsonSerializer.Deserialize<MessageSystemInfo>(json, _options),
                        nameof(MessageSendData) => JsonSerializer.Deserialize<MessageSendData>(json, _options),
                        nameof(MessageReceivedData) => JsonSerializer.Deserialize<MessageReceivedData>(json, _options),
                        _ => throw new NotSupportedException($"Unknown message type: {messageType}")
                    };
                }
                catch (JsonException ex)
                {
                    throw new JsonException($"Failed to deserialize message: {ex.Message}", ex);
                }
            }
        }
        /// <summary>
        /// this create message for send
        /// </summary>
        /// <param name="msg">class message inherited from  BaseMessage</param>
        /// <returns>data format json on string</returns>
        public string CreateMessage(BaseMassage msg) 
        {
            string json = serializer.Serialize(msg);
            return json;
        }
        /// <summary>
        /// convert json string into class
        /// </summary>
        /// <param name="msg">json on string</param>
        /// <returns>return class inherited from BaseMessage </returns>
        public BaseMassage? DeserializeMessage(string msg)
        {
           return serializer.Deserialize(msg);
        }
            public class RequestsDate
            {
                public string SenderID { get; set; } = "";
                public string Text { get; set; } = "";
                public string Group { get; set; } = "";
                public long Timestamp { get; set; }

                public RequestsDate()
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
            }

            public class ResponseDate
            {
                public string SenderID { get; set; } = "";
                public string Name { get; set; } = "";
                public string Role { get; set; } = "";
                public string Text { get; set; } = "";
                public string Group { get; set; } = "";
                public long Timestamp { get; set; }
                public string Type { get; set; } = "message";

                public ResponseDate()
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
            }

            public string CreateDateForServer(string senderID, string text, string group)
            {
                try
                {
                    var requestData = new RequestsDate
                    {
                        SenderID = senderID,
                        Text = text,
                        Group = group,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    //string jsonString = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
                    //{
                    //    WriteIndented = true  
                    //});
                    string jsonString = JsonSerializer.Serialize(requestData, _jsonOptions);
                    return jsonString;
                }
                catch (Exception ex)
                {
                    return "{}";
                }
            }


            public string CreateDateForClients(string senderID, string name, string role, string text, string group)
            {
                try
                {
                    var responseData = new ResponseDate
                    {
                        SenderID = senderID,
                        Name = name,
                        Role = role,
                        Text = text,
                        Group = group,
                        Type = "message",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    //string jsonString = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
                    //{
                    //    WriteIndented = true
                    //});
                    string jsonString = JsonSerializer.Serialize(responseData, _jsonOptions);
                    return jsonString;
                }
                catch (Exception ex)
                {
                    return "{}";
                }
            }

            public string CreateSystemMessage(string text, string group)
            {
                try
                {
                    var systemMessage = new ResponseDate
                    {
                        SenderID = "system",
                        Name = "Система",
                        Role = "admin",
                        Text = text,
                        Group = group,
                        Type = "system",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    return JsonSerializer.Serialize(systemMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZJSON] Ошибка создания системного сообщения: {ex.Message}");
                    return "{}";
                }
            }

            public string CreateErrorMessage(string errorText, string group)
            {
                try
                {
                    var errorMessage = new ResponseDate
                    {
                        SenderID = "error",
                        Name = "Ошибка",
                        Role = "system",
                        Text = errorText,
                        Group = group,
                        Type = "error",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    return JsonSerializer.Serialize(errorMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZJSON] Ошибка создания сообщения об ошибке: {ex.Message}");
                    return "{}";
                }
            }
            /// <summary>
            /// Parsing Message from Clients
            /// </summary>
            /// <param name="jsonDate"></param>
            /// <returns></returns>
            public RequestsDate ReadRequestDate(string jsonDate)
            {
                try
                {
                    if (string.IsNullOrEmpty(jsonDate))
                    {
                        Console.WriteLine("[ZJSON] Ошибка: пустой JSON");
                        return new RequestsDate();
                    }
                    var requestData = JsonSerializer.Deserialize<RequestsDate>(jsonDate);

                    if (requestData != null)
                    {

                        return requestData;
                    }

                    return new RequestsDate();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[ZJSON] Ошибка парсинга JSON: {ex.Message}");
                    Console.WriteLine($"[ZJSON] Некорректный JSON: {jsonDate}");
                    return new RequestsDate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZJSON] Общая ошибка: {ex.Message}");
                    return new RequestsDate();
                }
            }

            /// <summary>
            /// parserd message from Server
            /// </summary>
            /// <param name="jsonDate"></param>
            /// <returns></returns>
            public ResponseDate ReadResponseDate(string jsonDate)
            {
                try
                {
                    if (string.IsNullOrEmpty(jsonDate))
                    {
                        Console.WriteLine("[ZJSON] Ошибка: пустой JSON");
                        return new ResponseDate();
                    }

                    var responseData = JsonSerializer.Deserialize<ResponseDate>(jsonDate);

                    if (responseData != null)
                    {
                        return responseData;
                    }

                    return new ResponseDate();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[ZJSON] Ошибка парсинга JSON: {ex.Message}");
                    return new ResponseDate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZJSON] Общая ошибка: {ex.Message}");
                    return new ResponseDate();
                }
            }

            // universal old method ser 
            public string SerializeToJson<T>(T data)
            {
                try
                {
                    return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZJSON] Ошибка сериализации: {ex.Message}");
                    return "{}";
                }
            }

            // universal Old method des
            public T? DeserializeFromJson<T>(string jsonDate)
            {
                try
                {
                    if (string.IsNullOrEmpty(jsonDate))
                        return default(T);

                    return JsonSerializer.Deserialize<T>(jsonDate);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ZJSON] Ошибка десериализации: {ex.Message}");
                    return default(T);
                }
            }
        }
    
    
public class ZNet 
    {
        protected TcpListener? _listener;
        protected TcpClient? _client;
        protected NetworkStream? _stream;
        protected bool _isConnected = false;
        protected bool _isRunning = false;
        public class ZNetDate 
        {
            public string IP { get; set; } = "";
            public int Port { get; set; }
        }
        public virtual void SendDate(string JsonDate) 
        {
            
            
        }
        public virtual string ReadDate() 
        {
            return "";
        }
        protected IPAddress GetLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return IPAddress.Loopback;
        }
        public virtual void Close()
        {
            _isRunning = false;
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            _listener?.Stop();
        }

    }
    public class ZnetServer : ZNet
    {

        ZNetDate? NetData;
        public List<(string ID, string IP , TcpClient Client)> ListConnection = new List<(string ID, string IP , TcpClient Client)>();
        public event Action<string> PostRegister;
        public void CreateZNetDate(string IP , int Port) 
        {
            NetData = new ZNetDate();
            NetData.IP = IP;
            NetData.Port = Port;
        }
        public void StartListening(int port = 8888)
        {

            try
            {
                IPAddress localIP;
                ZNetDate serverInfo;  

                if (NetData != null && !string.IsNullOrEmpty(NetData.IP) && NetData.Port > 0)
                {
                    Console.WriteLine("[СЕРВЕР] Используются предустановленные данные подключения");
                    localIP = IPAddress.Parse(NetData.IP); 
                    serverInfo = NetData;
                }
                else
                {
                    Console.WriteLine("[СЕРВЕР] Предустановленные данные не найдены. Создаем локальные...");
                    localIP = GetLocalIP();
                    serverInfo = new ZNetDate
                    {
                        IP = localIP.ToString(),
                        Port = port
                    };
                }
                _listener = new TcpListener(localIP, serverInfo.Port);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine($"[СЕРВЕР] Запущен на {serverInfo.IP}:{serverInfo.Port}");
                Console.WriteLine($"[СЕРВЕР] Ожидание подключений...");

                Task.Run(() => AcceptClientsLoop());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[СЕРВЕР] Ошибка запуска: {ex.Message}");
            }
        }

        private async Task AcceptClientsLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient newClient = await _listener!.AcceptTcpClientAsync();
                    Console.WriteLine($"[СЕРВЕР] Клиент подключен: {newClient.Client.RemoteEndPoint}");
                    string clientIP = ((IPEndPoint)newClient.Client.RemoteEndPoint!).Address.ToString();

                    ListConnection.Add((ID: "null", IP: clientIP , newClient));

                    Task.Run(() => HandleClient(newClient));
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"[СЕРВЕР] Ошибка принятия клиента: {ex.Message}");
                }
            }
        }
        public bool isValidClient(string clientIP, string clientID) 
        {
            string id = FindIDByIP(clientIP);
            if (id == clientID) 
            {
                return true;
            }
            return false;
        
        }
        public virtual bool Register(string senderID, string text, string group, string SenderIP) 
        {
            return false;
        }
        protected async virtual void ReceivedClientMessage(string senderID, string text, string group, long timestap, NetworkStream stream , TcpClient client)
        {
            
        
        }
        /// <summary>
        /// listen and handle message from Clients
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private async Task HandleClient(TcpClient client)
        {
            string clientIP = "";
            string clientID = "null";

            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[4096];
                ZJSON jsonHelper = new ZJSON();

                
                while (client.Connected)
                {
                    try
                    {
                        clientIP = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
                        clientID = FindIDByIP(clientIP);
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        BaseMassage? message = jsonHelper.DeserializeMessage(receivedData);

                        if (message == null)
                        {
                            Console.WriteLine("[SERVER] Failed to deserialize message");
                            continue;
                        }

                        switch (message)
                        {
                            case MessageRequestRegister registerRequest:
                                string login = registerRequest.login;
                                string password = registerRequest.Password;
                                string group = "Register";

                                if (clientID == "null")
                                {
                                    if (Register(login, password, group, clientIP))
                                    {
                                        UpdateIDByIP(clientIP, login, client);

                                        PostRegister?.Invoke(login);

                                        var successResponse = new MessageResponseRegister
                                        {
                                            iSSuccses = true,
                                            ID = login, 
                                            Name = login,
                                            Roles = "User",
                                            Groups = "General"
                                        };
                                        string responseJson = jsonHelper.CreateMessage(successResponse);
                                        SendDateByIp(clientIP, responseJson);
                                    }
                                    else
                                    {
                                        var errorResponse = new MessageResponseRegister
                                        {
                                            iSSuccses = false,
                                            ID = "",
                                            Name = "",
                                            Roles = "",
                                            Groups = ""
                                        };
                                        string responseJson = jsonHelper.CreateMessage(errorResponse);
                                        SendDateByIp(clientIP, responseJson);
                                    }
                                }
                                else
                                {
                                    var systemMessage = new MessageSystemInfo
                                    {
                                        Code = "ALREADY_REGISTERED",
                                        info = "Client already registered"
                                    };
                                    string responseJson = jsonHelper.CreateMessage(systemMessage);
                                    SendDateByIp(clientIP, responseJson);
                                }
                                break;

                            case MessageSendData sendData:
                                string senderID = sendData.ID;
                                string messageText = sendData.Message;
                                string channel = sendData.Channel;

                                ReceivedClientMessage(senderID, messageText, channel, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), stream, client);
                                break;

                            case MessageSystemInfo systemInfo:
                                Console.WriteLine($"[SERVER] System message from {clientID}: {systemInfo.Code} - {systemInfo.info}");
                                break;

                            default:
                                Console.WriteLine($"[SERVER] Unknown message type: {message.TypeMessage}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SERVER] Error handling client: {ex.Message}");
                        break;
                    }
                }
            }
            
            RemoveClientConnection(clientIP, clientID);
            Console.WriteLine("[SERVER] Client disconnected");
        }
        /// <summary>
        /// Remove user from List
        /// </summary>
        private void RemoveClientConnection(string clientIP, string clientID)
        {
            lock (ListConnection)
            {
                // find and Remove ID
                var connectionToRemove = ListConnection.FirstOrDefault(c => c.IP == clientIP);
                if (connectionToRemove != default)
                {
                    ListConnection.Remove(connectionToRemove);
                    Console.WriteLine($"[SERVER] Removed client {connectionToRemove.ID} (IP: {connectionToRemove.IP}) from connection list");
                }

                if (clientID == "null")
                { 
                    for (int i = ListConnection.Count - 1; i >= 0; i--)
                    {
                        if (ListConnection[i].IP == clientIP && ListConnection[i].ID == "null")
                        {
                            Console.WriteLine($"[SERVER] Removing unauthorized client with IP: {ListConnection[i].IP}");
                            ListConnection.RemoveAt(i);
                        }
                    }
                }

                if (clientID != "null")
                {
                    var connectionsWithSameID = ListConnection.Where(c => c.ID == clientID).ToList();
                    foreach (var conn in connectionsWithSameID)
                    {
                        if (conn.IP != clientIP)
                        {
                            ListConnection.Remove(conn);
                            Console.WriteLine($"[SERVER] Removed duplicate client {conn.ID} (IP: {conn.IP}) from connection list");
                        }
                    }
                }
            }
        }

        public void SendDateByIp(string IP , string date) 
        {
            ZJSON js = new ZJSON();
            byte[] data = Encoding.UTF8.GetBytes(date);

            foreach ((string ID, string locIP, TcpClient Client) in ListConnection)
            {
                try
                {
                    if (Client.Client.Connected)
                    {
                        if (locIP == IP) 
                        {
                            NetworkStream stream =  Client.GetStream();
                            stream.Write(data ,0, data.Length);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки {ID}: {ex.Message}");
                }
            }
        }
        public override void SendDate(string JsonData)
        {
            
        }

        public override void Close()
        {
            base.Close();
            Console.WriteLine("[СЕРВЕР] Остановлен");
        }
        public void RemoveByIP(string ip)
        {
            int removed = ListConnection.RemoveAll(x => x.IP == ip);
            if (removed > 0)
                Console.WriteLine($"[СЕРВЕР] Удален клиент с IP: {ip}");
            else
                Console.WriteLine($"[СЕРВЕР] Клиент с IP {ip} не найден");
        }

        // Изменение ID по IP
        public void UpdateIDByIP(string ip, string newID , TcpClient Client)
        {
            var index = ListConnection.FindIndex(x => x.IP == ip);
            if (index != -1)
            {
                var old = ListConnection[index];
                ListConnection[index] = (newID, old.IP , Client);
                Console.WriteLine($"[СЕРВЕР] ID изменен: {old.ID} -> {newID} для IP {ip}");
            }
            else
            {
                Console.WriteLine($"[СЕРВЕР] Клиент с IP {ip} не найден");
            }
        }

        /// <summary>
        /// delete object by id 
        /// </summary>
        /// <param name="id"></param>
        public void RemoveByID(string id)
        {
            int removed = ListConnection.RemoveAll(x => x.ID == id);
            if (removed > 0)
                Console.WriteLine($"[СЕРВЕР] Удален клиент с ID: {id}");
            else
                Console.WriteLine($"[СЕРВЕР] Клиент с ID {id} не найден");
        }

        /// <summary>
        /// Edit IP by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newIP"></param>
        /// <param name="Client"></param>
        public void UpdateIPByID(string id, string newIP , TcpClient Client)
        {
            var index = ListConnection.FindIndex(x => x.ID == id);
            if (index != -1)
            {
                var old = ListConnection[index];
                ListConnection[index] = (old.ID, newIP , Client);
                Console.WriteLine($"[СЕРВЕР] IP изменен: {old.IP} -> {newIP} для ID {id}");
            }
            else
            {
                Console.WriteLine($"[СЕРВЕР] Клиент с ID {id} не найден");
            }
        }

        // Поиск ID по IP
        public string FindIDByIP(string ip)
        {
            var client = ListConnection.Find(x => x.IP == ip);
            return client.ID;
        }

        // Поиск IP по ID
        public string FindIPByID(string id)
        {
            var client = ListConnection.Find(x => x.ID == id);
            return client.IP;
        }

        // Проверка существует ли IP
        public bool HasIP(string ip)
        {
            return ListConnection.Exists(x => x.IP == ip);
        }

        // Проверка существует ли ID
        public bool HasID(string id)
        {
            return ListConnection.Exists(x => x.ID == id);
        }
    }

    public class ZNetClient : ZNet
    {
        public virtual bool Connect(string serverIP, int port = 8888)
        {
            try
            {
                Console.WriteLine($"[КЛИЕНТ] Подключение к {serverIP}:{port}...");

                _client = new TcpClient();
                _client.Connect(serverIP, port);
                _stream = _client.GetStream();
                _isConnected = true;

                Console.WriteLine("[КЛИЕНТ] Подключен к серверу!");

                Task.Run(() => ReadDataLoop());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[КЛИЕНТ] Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ConnectAsync(string serverIP, int port = 8888)
        {
            try
            {
                Console.WriteLine($"[КЛИЕНТ] Подключение к {serverIP}:{port}...");

                _client = new TcpClient();
                await _client.ConnectAsync(serverIP, port);
                _stream = _client.GetStream();
                _isConnected = true;

                Console.WriteLine("[КЛИЕНТ] Подключен к серверу!");

                Task.Run(() => ReadDataLoop());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[КЛИЕНТ] Ошибка подключения: {ex.Message}");
                return false;
            }
        }
        protected virtual void OnDateReceived(string data) 
        {

        }
        private async Task ReadDataLoop()
        {
            byte[] buffer = new byte[4096];

            while (_isConnected && _client != null && _client.Connected)
            {
                try
                {
                    if (_stream!.DataAvailable)
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            OnDateReceived(data);
                        }
                    }
                    await Task.Delay(100);
                }
                catch
                {
                    break;
                }
            }
        }
        public override void SendDate(string JsonDate)
        {
            
        }

        public void SendCustomData(string data)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[КЛИЕНТ] Нет подключения к серверу");
                return;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[КЛИЕНТ] Ошибка отправки: {ex.Message}");
            }
        }

        public override string ReadDate()
        {
            return "";
        }

        public async Task<string> ReadDataAsync()
        {
            if (!_isConnected || _stream == null)
            {
                return "Нет подключения";
            }

            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }

                return "";
            }
            catch (Exception ex)
            {
                return $"Ошибка чтения: {ex.Message}";
            }
        }

        public override void Close()
        {
            base.Close();
            Console.WriteLine("[КЛИЕНТ] Отключен от сервера");
        }
    }
}

    