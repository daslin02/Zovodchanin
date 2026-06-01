using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ZNetwork;
namespace ZovodchaninClient
{
    public class Client : ZNetClient 
    {
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

                    break; // Нормальная отмена
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
            var msg = js.ReadResponseDate(data);
            Console.WriteLine($"{msg.Name} {msg.Text} {msg.SenderID}");
        }
        /// <summary>
        /// функция  отвечает за регистрацию пользователя на сервере
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Password"></param>
        public virtual void Register(string ID , string Password) 
        {
            ZJSON js = new ZJSON();
            string msg = js.CreateDateForServer(ID, Password, "Register");
            Console.WriteLine (msg);
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
