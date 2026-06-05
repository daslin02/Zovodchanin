using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Zovodchanin
{
    public partial class RegistrationPage : Page
    {
        private bool isDarkTheme = false;
        
        
        public RegistrationPage()
        {
            InitializeComponent();
   
        }
        public void SetTheme(bool isDark) 
        {
            isDarkTheme = isDark;
            if (isDark)
            {
                // dark style
                Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                Resources["CardBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                Resources["InputBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                Resources["ButtonBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                Resources["ThemeButtonBrush"] = new SolidColorBrush(Color.FromRgb(70, 70, 70));
                ThemeIcon.Text = "☀️";
            }
            else
            {
                // White style
                Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                Resources["CardBrush"] = new SolidColorBrush(Colors.White);
                Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                Resources["InputBrush"] = new SolidColorBrush(Colors.White);
                Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                Resources["ButtonBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                Resources["ThemeButtonBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                ThemeIcon.Text = "🌙";
            }
        }
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            SetTheme(isDarkTheme);
        }
        public bool GetTheme() { return isDarkTheme; }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var mw = Application.Current.MainWindow as MainWindow;

            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                mw?.ShowToast("Ведите ID");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                mw?.ShowToast("Ведите Пароль");
                return;
            }
            mw.Register(txtUsername.Text, txtPassword.Password);
        }
    }
}