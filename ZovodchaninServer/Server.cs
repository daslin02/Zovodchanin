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

namespace ZovodchaninServer
{
    public class Server : ZnetServer
    {
        DB dataBase = DB.Instance;
        public Server() 
        {
            PostRegister += PostRegisterFunc;
        }
        private void PostRegisterFunc(string SenderID) 
        {
            ZJSON js = new ZJSON();
            DBAccount acc = dataBase.GetAccountByID(SenderID );
            string msg = js.CreateDateForClients(SenderID, acc.Name, acc.Role, "Register--Success", acc.Group);
            Console.WriteLine($"отправленно на ID {SenderID}");
            SendDate(msg);
        }
        public override bool Register(string senderID, string text, string group, string SenderIP)
        {
            DBAccount acc =  dataBase.Register(senderID, text);
            Console.WriteLine("регистрация...");
            if (acc.IsValid()) 
            {
                Console.WriteLine("Успешно");
                return true;
            }
            Console.WriteLine("Провал");
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
        public override void SendDate(string JsonData)
        {
            ZJSON js = new ZJSON();
            byte[] data = Encoding.UTF8.GetBytes(JsonData);
            foreach ((string ID, string IP, TcpClient Client) in ListConnection)
            {
                try
                {
                    if (Client.Client.Connected)
                    {
                        var datejs = js.ReadResponseDate(JsonData);
                        
                        Console.WriteLine( $"Results: {dataBase.CheckGroupByID(ID, datejs.Group)}");
                        if (dataBase.CheckGroupByID(ID , datejs.Group)) 
                        {
                            
                            Console.WriteLine("Send date Succeseful");
                            Console.WriteLine($"Отправленно на {IP} c {ID} в группе {datejs.Group}");

                            NetworkStream stream = Client.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                        else if (datejs.Group == "register") 
                        {
                            Console.WriteLine($"Регистрация {IP} c {ID} ");
                            Console.WriteLine(datejs.SenderID, datejs.Group, datejs.Text);
                            NetworkStream stream = Client.GetStream();
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки {ID}: {ex.Message}");
                }
            }
        }
        protected override async void ReceivedClientMessage(string senderID, string text, string group, long timestap, NetworkStream stream , TcpClient client)
        {
            ZJSON js = new ZJSON();
            string IP = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
            if (isValidClient(IP, senderID))
            {
                string msg = js.CreateDateForClients(senderID, "PIDOR", "HUESOS", "SUCSSES", group);
                SendDate(msg);
                return;
            }
            else 
            {
                return;
            }
        }
    }


    internal class Programm
    {
        
        private static void Main() 
        {
            DBInspector.DB dataBase;
            dataBase = DB.Instance;

            if (!DB.checkConnecttion()) 
            {
                Console.WriteLine("Don't open SQL-Server");
                return;
            }
            dataBase.Connect();


            Server  MyServer = new Server();
            MyServer.CreateZNetDate("127.0.0.1", 6739);
            MyServer.StartListening();
            ZJSON js = new ZJSON();

            while (true) 
            {
                string Value = Console.ReadLine().ToLower();
                if (Value == "exit")
                {
                    MyServer.Close();
                    break;
                }
                else if (Value.Split(' ')[0] == "send")
                {
                    // update
                    string msg = js.CreateDateForClients("01231", "Egor", "Admin", "Hello Clients", "general01");
                    MyServer.SendDate(msg);

                }
                else if (Value.Split(' ')[0] == "accid") 
                {
                    DBAccount acc;
                    acc = dataBase.GetAccountByID(Value.Split(" ")[1]);
                    if (acc.IsValid())
                    {
                        Console.WriteLine($"------ACCOUNT SUCCESS-------- \n ID: {acc.ID} \n Name: {acc.Name} \n Password: {acc.Password}");
                    }
                    else Console.WriteLine("NO FOUND ACCOUNT") ;
                }
                else if (Value == "testmsg") 
                {
                    string msg = js.CreateDateForClients("01231", "Egor", "Admin", "Hello Clients", "all");
                    MyServer.SendAll(msg);
                    Console.WriteLine("Send All Account");
                }

                
            }
        }
    }
}
