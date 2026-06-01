using ZNetwork;
using ZovodchaninClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TempGUI
{

    public partial class Form1 : Form
    {
        class UserInfo
        {
            public bool iSEdit = false;
            public string ID { get; set; }
            public string Name { get; set; }
            public string Roles { get; set; }
            public string Groups { get; set; }

        }

        Client client;
        ZJSON js;
        UserInfo userInfo = new UserInfo();
        private bool bIsReg = false;
        public Form1()
        {
            InitializeComponent();

            client = new Client();
            js = new ZJSON();

            client.DataReceived += GuiReadDate;
            client.ErrorPrint += Pr;
            bool result = client.Connect("127.0.0.1", 6739);
        }
        private void Pr(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Pr), msg);
                return;
            }

            Console.WriteLine($"[ERROR] {msg}");
            label1.Text += "\n" + msg;
            textBox3.Text += "\n" + msg;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (bIsReg) return;
            string ID = textBox1.Text == string.Empty ? "null" : textBox1.Text;
            string Password = textBox2.Text == string.Empty ? "null" : textBox2.Text;

            client.Register(ID, Password);

        }
        private void GuiReadDate(string str)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(GuiReadDate), str);
                return;
            }

            ZJSON js = new ZJSON();
            ZJSON.BaseMassage? msg = js.DeserializeMessage(str);

            if (msg == null)
            {
                Console.WriteLine("[CLIENT] Failed to deserialize message");
                return;
            }

            switch (msg)
            {
                case ZJSON.MessageResponseRegister registerInfo:
                    if (registerInfo.iSSuccses)
                    {
                        // Registration successful
                        label1.Text += $"\n[REGISTRATION SUCCESS] ID: {registerInfo.ID}, Name: {registerInfo.Name}, Roles: {registerInfo.Roles}\n";
                        textBox3.Text += $"\n[REGISTRATION SUCCESS] ID: {userInfo.ID}, Name: {userInfo.Name}, Roles: {userInfo.Roles}\n";

                        // Save user info
                        if (!userInfo.iSEdit) 
                        {
                            userInfo.ID = registerInfo.ID;
                            userInfo.Name = registerInfo.Name;
                            userInfo.Roles = registerInfo.Roles;
                            userInfo.Groups = registerInfo.Groups;
                            userInfo.iSEdit = true;
                        }

                    }
                    else
                    {
                        // Registration failed
                        label1.Text += $"\n[REGISTRATION FAILED] Please try again\n";
                    }
                    break;

                case ZJSON.MessageSystemInfo systemInfo:
                    // System message from server
                    label1.Text += $"\n[SYSTEM] {systemInfo.Code}: {systemInfo.info}\n";

                    // Handle specific system codes
                    switch (systemInfo.Code)
                    {
                        case "SERVER_MSG":
                        case "SERVER_NOTIFICATION":
                            // Just display as is
                            break;
                        case "NEW_USER":
                            label1.Text += $"\n*** A new user joined the chat! ***\n";
                            break;
                        case "USER_LEFT":
                            label1.Text += $"\n*** A user left the chat ***\n";
                            break;
                        default:
                            // Unknown system code
                            break;
                    }
                    break;

                case ZJSON.MessageReceivedData receivedData:
                    // Incoming message from another user
                    string timeStr = receivedData.SendTime.ToString("HH:mm:ss");
                    label1.Text += $"\n[{timeStr}] {receivedData.NameSender} ({receivedData.RolesSender}): {receivedData.Channels}\n";

                    // Play notification sound (optional)
                    // PlayNotificationSound();
                    break;

                case ZJSON.MessageSendData sendData:
                    // Message sent by current user (echo from server)
                    label1.Text += $"\n[ME -> {sendData.Channel}]: {sendData.Message}\n";
                    break;

                case ZJSON.MessageRequestRegister:
                    // Should not receive this on client
                    label1.Text += $"\n[ERROR] Received registration request on client\n";
                    break;

                default:
                    label1.Text += $"\n[UNKNOWN MESSAGE] Type: {msg.TypeMessage}\n";
                    break;
            }

        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            label1.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ZJSON.MessageSendData cls = new ZJSON.MessageSendData
            {
                ID = userInfo.ID,
                Message = "Hello everyone!!!",
                Channel = userInfo.Groups.Split(';')[0],
            };

            cls.ID = userInfo.ID;
            string msg = js.CreateMessage(cls);
            textBox3.Text = cls.ID;
            client.SendCustomData(msg);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox3.Text = $"ID {userInfo.ID} --- Name {userInfo.Name} --- Groups {userInfo.Groups} --- Roles {userInfo.Roles}";
        }
    }
}
