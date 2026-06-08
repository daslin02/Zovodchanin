using System.Reflection.Emit;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZNetwork;
using ZovodchaninClient;
namespace Zovodchanin
{

    class UserInfo
    {
        public bool iSEdit = false;
        public string ID { get; set; }
        public string Name { get; set; }
        public string Roles { get; set; }
        public string Groups { get; set; }

    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Client client = new Client();

        RegistrationPage RegPage = new RegistrationPage();
        MainPage MP;

        private ZJSON.MessageSerializer _msgSer;
        private System.Windows.Threading.DispatcherTimer _toastTimer;

        UserInfo userInfo; 

        public MainWindow()
        {
            client.Connect("127.0.0.1" , 6739);
            client.DataReceived += ReadData;

            _msgSer = new ZJSON.MessageSerializer();

            InitializeComponent();
            MainFrame.Navigate(RegPage);

            

            _toastTimer = new System.Windows.Threading.DispatcherTimer();
            _toastTimer.Interval = TimeSpan.FromSeconds(3); 
            _toastTimer.Tick += ToastTimer_Tick;

        }
        public string GetUserID() 
        {
            return userInfo.ID;
        }
        private void ReadData(string data) 
        {
            Application.Current.Dispatcher.Invoke(new Action(() => {
                ZJSON.BaseMassage? msg =  _msgSer.Deserialize(data);
                
                switch (msg) 
                {
                    case ZJSON.MessageResponseRegister response:
                        if (response.iSSuccses) 
                        {
                            if (userInfo == null) 
                            {
                                userInfo = new UserInfo();
                                userInfo.ID = response.ID;
                                userInfo.Name = response.Name;
                                userInfo.Roles = response.Roles;
                                userInfo.Groups = response.Groups;
                            }   
                            ShowToast("Добро пожаловать", false);
                            MP = new MainPage();
                            MP.SetTheme(RegPage.GetTheme());
                            MainFrame.Navigate(MP);

                            foreach(string chatName in userInfo.Groups.Split(';')) 
                            {
                                MP.ChatListAddChat(chatName);
                            }
                            
                            break;
                        }
                        else 
                        {
                            ShowToast("не верный ID или пароль", true);
                        }
                        break;
                    case ZJSON.MessageReceivedData data:
                        if (userInfo == null) break;
                        MP?.ChatAddMessage(data.NameSender, data.Message , data.SendTime);
                        break;
                }
                
            }));
        
        }
        public void Register(string ID , string password) 
        {
            client.Register(ID, password);
        }
        public void ShowToast(string message, bool isError = true)
        {
            _toastTimer.Stop();

            ToastMessage.Text = message;

            if (isError)
            {
                ToastNotification.Background = new SolidColorBrush(Color.FromRgb(255, 82, 82)); // Красный
                ToastIcon.Text = "⚠️";
            }
            else
            {
                ToastNotification.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Зеленый
                ToastIcon.Text = "✅";
            }

            ToastNotification.Visibility = Visibility.Visible;

            DoubleAnimation showAnimation = new DoubleAnimation
            {
                From = -100,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ToastTransform.BeginAnimation(TranslateTransform.YProperty, showAnimation);

            _toastTimer.Start();
        }
        private void ToastTimer_Tick(object sender, EventArgs e)
        {
            HideToast();
        }

        private void CloseToast_Click(object sender, RoutedEventArgs e)
        {
            HideToast();
        }
        private void HideToast()
        {
            _toastTimer.Stop();

            DoubleAnimation hideAnimation = new DoubleAnimation
            {
                From = 0,
                To = -100,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            hideAnimation.Completed += (s, e) =>
            {
                ToastNotification.Visibility = Visibility.Collapsed;
            };

            ToastTransform.BeginAnimation(TranslateTransform.YProperty, hideAnimation);
        }

    }
}