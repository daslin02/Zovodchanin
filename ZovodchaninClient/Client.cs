using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ZNetwork;
using static ZNetwork.ZJSON;
namespace ZovodchaninClient
{
    public class Client : ZNetClient 
    {
        ZJSON js = new ZJSON();
        private CancellationTokenSource _cts;
        private Task _readTask;

        public event Action<string> DataReceived;
        public event Action<string> ErrorPrint;

        public override bool Connect(string serverIP, int port = 8888)
        {
            bool result = base.Connect(serverIP, port);
            
            _cts = new CancellationTokenSource();
            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
            return result;

        }
        private async Task ReadLoopAsync(CancellationToken token)
        {
            
            while (_isConnected && !token.IsCancellationRequested)
            {
                try
                {
                    if (_stream == null) break;


                    byte[] buffer = new byte[4096];

                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead > 0)
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        DataReceived?.Invoke(data);
                        //OnDateReceived(data);
                    }
                }
                catch (OperationCanceledException)
                {

                    break; 
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    DataReceived?.Invoke($"ERROR: {ex.Message}");
                    break;
                }
            }
        }
        public override void SendDate(string JsonDate)
        {
            if (!_isConnected || _stream == null)
            {
                Console.WriteLine("[КЛИЕНТ] Нет подключения к серверу");
                return;
            }

            try
            {
                Console.Write("Введите сообщение для сервера: ");
                string? message = Console.ReadLine();

                if (!string.IsNullOrEmpty(message))
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    _stream.Write(data, 0, data.Length);
                    Console.WriteLine("[КЛИЕНТ] Данные отправлены");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[КЛИЕНТ] Ошибка отправки: {ex.Message}");
            }
        }

        public override string ReadDate()
        {
            if (!_isConnected || _stream == null)
            {
                return "Нет подключения";
            }

            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);

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
        
        /// <summary>
        /// Func called where getting message from Server
        /// </summary>
        /// <param name="data"></param>
        protected override void OnDateReceived(string data) 
        {
            ZJSON js = new ZJSON();
            BaseMassage? message = js.DeserializeMessage(data);

            switch (message)
            {
                case MessageResponseRegister registerResponse:
                    Console.WriteLine($"[REGISTRATION RESPONSE] Success: {registerResponse.iSSuccses}, ID: {registerResponse.ID}, Name: {registerResponse.Name}");
                    break;

                case MessageSystemInfo systemInfo:
                    Console.WriteLine($"[SYSTEM] Code: {systemInfo.Code}, Info: {systemInfo.info}");
                    break;

                case MessageReceivedData receivedData:
                    Console.WriteLine($"[MESSAGE RECEIVED] From: {receivedData.NameSender}, Channel: {receivedData.Channels}, Text: {receivedData.Channels}, Time: {receivedData.SendTime}");
                    break;

                case MessageSendData sendData:
                    Console.WriteLine($"[MESSAGE SEND] ID: {sendData.ID}, Channel: {sendData.Channel}, Message: {sendData.Message}");
                    break;

                case MessageRequestRegister requestRegister:
                    Console.WriteLine($"[REGISTRATION REQUEST] Login: {requestRegister.login}");
                    break;

                default:
                    Console.WriteLine($"[UNKNOWN] Type: {message.TypeMessage}");
                    break;
            }
        }
        /// <summary>
        /// функция  отвечает за регистрацию пользователя на сервере
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Password"></param>
        public virtual void Register(string ID , string Password) 
        {
            // Create registration request message
            var registerRequest = new MessageRequestRegister
            {
                login = ID,
                Password = Password
                // TypeMessage will be set automatically in constructor
            };

            // Serialize to JSON
            string msg = js.CreateMessage(registerRequest);
            Console.WriteLine($"[REGISTRATION REQUEST] Sending: {msg}");

            // Send to server
            SendCustomData(msg);
        }

    }
    internal class Programm
    {
        public static void MainA() 
        {
            Client MyClient = new Client();

            ZJSON js = new ZJSON();
            MyClient.Connect("127.0.0.1", 6739);
            while (true) 
            {
                string value = Console.ReadLine();
                string msg = js.CreateDateForServer(value.Split("--")[0], value.Split("--")[1], value.Split("--")[2]);
                MyClient.SendCustomData(msg);
            }

        
        }

    }
}
