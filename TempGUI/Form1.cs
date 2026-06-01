using ZNetwork;
using ZovodchaninClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TempGUI
{
    public partial class Form1 : Form
    {
        Client client;
        ZJSON js;
        private bool bIsReg = false;
        public Form1()
        {
            InitializeComponent();

            client = new Client();
            js = new ZJSON();

            client.DataReceived += GuiReadDate;
            client.ErrorPrint += Pr;
            bool result =  client.Connect("127.0.0.1", 6739);
        }
        private void Pr(string msg) 
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Pr), msg);
                return;
            }

            Console.WriteLine($"[ERROR] {msg}");
            label1.Text += "\n"+ msg;
            textBox3.Text += "\n"+ msg;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (bIsReg) return;
            string ID = textBox1.Text == string.Empty ? "null" : textBox1.Text ;
            string Password =  textBox2.Text == string.Empty ? "null" : textBox2.Text;

            client.Register(ID, Password);
            
        }
        private void GuiReadDate(string str) 
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(GuiReadDate), str);
                return;
            }
            label1.Text += str;
            var msg = js.ReadResponseDate(str);
            
        }
    }
}
