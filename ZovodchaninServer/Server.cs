using DBInspector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ZNetwork;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static ZNetwork.ZJSON;

namespace ZovodchaninServer
{
    public class ChatHistoryMessage
    {
        public string id { get; set; }
        public string userId { get; set; }
        public string userName { get; set; }
        public string role { get; set; }
        public string text { get; set; }
        public string timestamp { get; set; }
        public long timestampUnix { get; set; }
    }
    public class Server : ZnetServer
    {
        DB dataBase = DB.Instance;
        public Server() 
        {
            PostRegister += PostRegisterFunc;
        }
        private void PostRegisterFunc(string SenderID)
        {
            // Get account data from database
            DBAccount acc = dataBase.GetAccountByID(SenderID);

            // Create registration response message using new serialization system
            var response = new MessageResponseRegister
            {
                iSSuccses = true,
                ID = SenderID,
                Name = acc.Name,
                Roles = acc.Role,
                Groups = acc.Group
            };


            // Serialize to JSON
            ZJSON js = new ZJSON();
            string msg = js.CreateMessage(response);

            Console.WriteLine($"Sent to ID {SenderID}");
            SendDate(msg);
        }
        public override bool Register(string senderID, string text, string group, string SenderIP)
        {
            DBAccount acc =  dataBase.Register(senderID, text);
            if (acc.IsValid()) 
            {
                return true;
            }
            return false;
        }
        public void SendAll(string jsondate) 
        {
            ZJSON js = new ZJSON();
            byte[] data = Encoding.UTF8.GetBytes(jsondate);
            foreach ((string ID, string IP, TcpClient Client) in ListConnection) 
            {
                try
                {
                    if (Client.Client.Connected)
                    {
                        var datejs = js.ReadResponseDate(jsondate);

                        DBAccount acc = dataBase.GetAccountByID(ID);
                        Console.WriteLine($"ID : {acc.ID}  Name: {acc.Name}");
                        NetworkStream stream = Client.GetStream();
                        stream.Write(data, 0, data.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки {ID}: {ex.Message}");
                }
            }
        }
        public override async void SendDate(string JsonData)
        {
            ZJSON js = new ZJSON();
            byte[] data = Encoding.UTF8.GetBytes(JsonData);

            // Deserialize message to determine its type and properties
            BaseMassage? message = js.DeserializeMessage(JsonData);

            if (message == null)
            {
                Console.WriteLine("[SERVER] Failed to deserialize message for sending");
                return;
            }

            var sendTasks = new List<Task>();

            foreach ((string ID, string IP, TcpClient Client) in ListConnection)
            {
                try
                {
                    if (Client.Client.Connected)
                    {
                        bool shouldSend = false;
                        string targetGroup = "";
                        string logMessage = "";

                        // Handle different message types
                        switch (message)
                        {
                            case MessageResponseRegister registerResponse:
                                if (registerResponse.ID == ID)
                                {
                                    shouldSend = true;
                                    logMessage = $"[REGISTRATION] To {IP} (ID: {ID}) - Success: {registerResponse.iSSuccses}";
                                }
                                break;

                            case MessageSendData sendData:
                                bool canUserWrite = dataBase.CanUserWriteToGroup(ID, targetGroup);
                                if (canUserWrite) 
                                {
                                    shouldSend = true;
                                    logMessage = $"[MESSAGE] To {IP} (ID: {ID}) in group: {targetGroup}";
                                }
                                break;

                            case MessageReceivedData receivedData:
                                targetGroup = receivedData.Channels;
                                if (dataBase.CheckGroupByID(ID, targetGroup))
                                {
                                    shouldSend = true;
                                    logMessage = $"[NOTIFICATION] To {IP} (ID: {ID}) in channel: {targetGroup}";
                                }
                                break;

                            case MessageSystemInfo systemInfo:
                                shouldSend = true;
                                logMessage = $"[SYSTEM] To {IP} (ID: {ID}) - Code: {systemInfo.Code}";
                                break;

                            default:
                                shouldSend = false;
                                break;
                        }

                        if (shouldSend)
                        {
                            NetworkStream stream = Client.GetStream();
                            await stream.WriteAsync(data, 0, data.Length);
                            Console.WriteLine($"[SENT] {logMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to send to {ID}: {ex.Message}");
                }
            }
        }
        protected override async void ReceivedClientMessage(string senderID, string text, string group, long timestamp, NetworkStream stream, TcpClient client)
        {
            ZJSON js = new ZJSON();
            string IP = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();

            if (senderID == FindIDByIP(IP)) return;
            if (isValidClient(IP, senderID))
            {
                // Create proper response message using new serialization system
                DBAccount acc = dataBase.GetAccountByID(senderID);

                MessageReceivedData response = new MessageReceivedData
                {
                    NameSender = senderID,
                    Channels = group,
                    SendTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                    RolesSender = acc.Role,
                    Message = text
                };

                // Serialize and send
                string msg = js.CreateMessage(response);
                SendDate(msg);

                //  Save message to chat history 
                await SaveMessageToHistory(senderID, text, group, DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime, acc.Role);

                // Log the action
                Console.WriteLine($"[MESSAGE] Processed message from {senderID} in group {group}");
                return;
            }
            else
            {
                // Send error message for invalid client
                var errorMsg = new MessageSystemInfo
                {
                    Code = "INVALID_CLIENT",
                    info = $"Client {senderID} is not valid or IP mismatch"
                };

                string msg = js.CreateMessage(errorMsg);
                SendDate(msg);

                Console.WriteLine($"[WARNING] Invalid client attempt: {senderID} from IP {IP}");
                return;
            }
        }
        // ADDED METHOD: Save message to history 
        /// <summary>
        /// Saves message to daily JSON file for the channel
        /// </summary>
        private async Task SaveMessageToHistory(string senderID, string text, string channel, DateTime sendTime, string role)
        {
            try
            {
                // Create base directory for chat history
                string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatHistory");
                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                // Create channel directory
                string channelPath = Path.Combine(basePath, SanitizeFileName(channel));
                if (!Directory.Exists(channelPath))
                    Directory.CreateDirectory(channelPath);

                // Generate filename based on date
                string dateStr = sendTime.ToString("yyyy-MM-dd");
                string filePath = Path.Combine(channelPath, $"{dateStr}.json");

                // Message object to save
                var messageObject = new
                {
                    id = Guid.NewGuid().ToString(),
                    userId = senderID,
                    userName = senderID,
                    role = role,
                    text = text,
                    timestamp = sendTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    timestampUnix = new DateTimeOffset(sendTime).ToUnixTimeSeconds()
                };

                // Load existing messages or create new list
                List<object> messages = new List<object>();
                if (File.Exists(filePath))
                {
                    string existingJson = await File.ReadAllTextAsync(filePath);
                    var existingData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
                    if (existingData != null && existingData.ContainsKey("messages"))
                    {
                        // Extract existing messages
                        var existingMessages = System.Text.Json.JsonSerializer.Deserialize<List<object>>(existingData["messages"].ToString());
                        if (existingMessages != null)
                            messages = existingMessages;
                    }
                }

                // Add new message
                messages.Add(messageObject);

                // Prepare full history object
                var historyData = new
                {
                    channel = channel,
                    date = dateStr,
                    lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    totalMessages = messages.Count,
                    messages = messages
                };

                // Save to file
                string jsonContent = System.Text.Json.JsonSerializer.Serialize(historyData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, jsonContent);

                // Update database record
                long fileSize = new FileInfo(filePath).Length;
                dataBase.SaveChatHistoryRecord(channel, sendTime.Date, filePath, messages.Count, fileSize);

                Console.WriteLine($"[HISTORY] Saved message from {senderID} to {channel} - Total messages today: {messages.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HISTORY ERROR] Failed to save message: {ex.Message}");
            }
        }
        /// <summary>
        /// Removes invalid characters from filename
        /// </summary>
        private string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
        public async Task<List<ChatHistoryMessage>> GetChatHistory(string channel, DateTime date)
        {
            var messages = new List<ChatHistoryMessage>();

            try
            {
                string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ChatHistory");
                string channelPath = Path.Combine(basePath, SanitizeFileName(channel));
                string dateStr = date.ToString("yyyy-MM-dd");
                string filePath = Path.Combine(channelPath, $"{dateStr}.json");

                if (File.Exists(filePath))
                {
                    string jsonContent = await File.ReadAllTextAsync(filePath);
                    var historyData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                    if (historyData != null && historyData.ContainsKey("messages"))
                    {
                        var messagesList = System.Text.Json.JsonSerializer.Deserialize<List<ChatHistoryMessage>>(historyData["messages"].ToString());
                        if (messagesList != null)
                            messages = messagesList;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HISTORY ERROR] Failed to get chat history: {ex.Message}");
            }

            return messages;
        }
        /// <summary>
        /// Loads messages from last N days for a channel
        /// </summary>
        public async Task<List<ChatHistoryMessage>> GetRecentMessages(string channel, int days = 7)
        {
            var allMessages = new List<ChatHistoryMessage>();

            for (int i = 0; i < days; i++)
            {
                DateTime date = DateTime.Now.Date.AddDays(-i);
                var messages = await GetChatHistory(channel, date);
                allMessages.AddRange(messages);
            }

            // Sort by timestamp
            return allMessages.OrderBy(m => m.timestamp).ToList();
        }


    }


    internal class Programm
    {
        private static async Task Main()
        {
            DBInspector.DB dataBase;
            DB.iSLocalDataBase = true;
            dataBase = DB.Instance;

            if (!DB.checkConnecttion())
            {
                Console.WriteLine("Can't open SQL-Server connection");
                return;
            }
            dataBase.Connect();

            Server MyServer = new Server();
            MyServer.CreateZNetDate("127.0.0.1", 6739);
            MyServer.StartListening();

            ZJSON js = new ZJSON();

            Console.WriteLine("Server started. Type 'help' for commands.");

            while (true)
            {
                string? Value = await Task.Run(() => Console.ReadLine());
                if (string.IsNullOrEmpty(Value)) continue;

                string[] commandParts = Value.ToLower().Split(' ');
                string command = commandParts[0];

                switch (command)
                {
                    case "exit":
                        MyServer.Close();
                        Console.WriteLine("Server stopped");
                        return;

                    case "send":
                        var sendData = new MessageSendData
                        {
                            ID = "Server",
                            Message = "Hello Clients",
                            Channel = "general01"
                        };
                        string msg = js.CreateMessage(sendData);
                        MyServer.SendDate(msg);
                        Console.WriteLine("[SERVER] Message sent to general01 group");
                        break;

                    case "sendto":
                        if (commandParts.Length >= 3)
                        {
                            string group = commandParts[1];
                            string message = string.Join(" ", commandParts.Skip(2));

                            var groupMsg = new MessageReceivedData
                            {
                                Channels = group,
                                NameSender = "Server",
                                SendTime = DateTime.Now,
                                RolesSender = "SERVER",
                                Message = message
                            };

                            string jsonMsg = js.CreateMessage(groupMsg);
                            MyServer.SendDate(jsonMsg);
                            Console.WriteLine($"[SERVER] Message sent to group: {group}");
                        }
                        else
                        {
                            Console.WriteLine("[USAGE] sendto [group] [message]");
                        }
                        break;

                    case "accid":
                        if (commandParts.Length >= 2)
                        {
                            string accountId = commandParts[1];
                            DBAccount acc = dataBase.GetAccountByID(accountId);

                            if (acc.IsValid())
                            {
                                Console.WriteLine($"\n--- ACCOUNT FOUND ---");
                                Console.WriteLine($"ID:       {acc.ID}");
                                Console.WriteLine($"Name:     {acc.Name}");
                                Console.WriteLine($"Password: {acc.Password}");
                                Console.WriteLine($"Role:     {acc.Role}");
                                Console.WriteLine($"Group:    {acc.Group}");
                                Console.WriteLine($"----------------------\n");
                            }
                            else
                            {
                                Console.WriteLine($"Account with ID '{accountId}' not found");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[USAGE] accid [account_id]");
                        }
                        break;

                    case "testmsg":
                        var broadcastMsg = new MessageSendData
                        {
                            ID = "System",
                            Message = "Test broadcast message from server",
                            Channel = "all"
                        };
                        string broadcastJson = js.CreateMessage(broadcastMsg);
                        MyServer.SendAll(broadcastJson);
                        Console.WriteLine("[SERVER] Test message broadcasted to all clients");
                        break;

                    case "system":
                        if (commandParts.Length >= 2)
                        {
                            string systemMessage = string.Join(" ", commandParts.Skip(1));

                            var sysMsg = new MessageSystemInfo
                            {
                                Code = "SERVER_NOTIFICATION",
                                info = systemMessage
                            };

                            string sysJson = js.CreateMessage(sysMsg);
                            MyServer.SendAll(sysJson);
                            Console.WriteLine($"[SYSTEM] Notification sent: {systemMessage}");
                        }
                        else
                        {
                            Console.WriteLine("[USAGE] system [message]");
                        }
                        break;

                    case "list":
                        // Show connected clients
                        Console.WriteLine($"\n--- Connected Clients: {MyServer.ListConnection.Count} ---");
                        foreach (var client in MyServer.ListConnection)
                        {
                            Console.WriteLine($"ID: {client.ID}, IP: {client.IP}");
                        }
                        Console.WriteLine("----------------------------------------\n");
                        break;

                    case "help":
                        Console.WriteLine("\n=== SERVER COMMANDS ===");
                        Console.WriteLine("exit                    - Stop the server");
                        Console.WriteLine("send                    - Send test message to general01 group");
                        Console.WriteLine("sendto [group] [msg]    - Send message to specific group");
                        Console.WriteLine("accid [id]              - Get account info by ID");
                        Console.WriteLine("testmsg                 - Broadcast test message to all clients");
                        Console.WriteLine("system [message]        - Send system message to all clients");
                        Console.WriteLine("list                    - Show connected clients");
                        Console.WriteLine("help                    - Show this help");
                        Console.WriteLine("=======================\n");
                        break;

                    default:
                        Console.WriteLine($"Unknown command: '{command}'. Type 'help' for available commands.");
                        break;
                }
            }
        }
    }
}
