using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZNetwork;

namespace Zovodchanin
{
    public partial class MainPage : Page
    {
        private bool isDarkTheme = false;

        // Collection to hold chat messages for data binding
        public ObservableCollection<ChatMessage> Messages { get; set; }

        private string _currentSelectedChat = "Error";
        public MainPage()
        {
            InitializeComponent();

            // Initialize empty collections
            Messages = new ObservableCollection<ChatMessage>();
            MessagesItemsControl.ItemsSource = Messages;
            ChatListBox.SelectionChanged += ChatListBox_SelectionChanged;
        }
        /// <summary>
        /// Gets the currently selected chat name
        /// </summary>
        public string GetCurrentSelectedChat()
        {
            return _currentSelectedChat;
        }
        private void ChatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChatListBox.SelectedItem != null)
            {
                _currentSelectedChat = ChatListBox.SelectedItem.ToString();
                Console.WriteLine($"[UI] Switched to chat: {_currentSelectedChat}");

                // Optional: Clear messages when switching chats
                // ChatClear();

                // Optional: Load history for this chat
                // LoadChatHistory(_currentSelectedChat);
            }
        }

        /// <summary>
        /// Adds a new message to the central chat area with current time.
        /// </summary>
        public void ChatAddMessage(string userName, string text , DateTime time)
        {
            Messages.Add(new ChatMessage
            {
                UserName = userName,
                Text = text,
                Time = time.ToString(),
                FullTimestamp = DateTime.Now
            });

            // Scroll to the bottom after adding a message
            Dispatcher.InvokeAsync(() =>
            {
                ChatScrollViewer.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Adds a new message with custom time.
        /// </summary>
        public void ChatAddMessageWithTime(string userName, string text, DateTime timestamp)
        {
            Messages.Add(new ChatMessage
            {
                UserName = userName,
                Text = text,
                Time = timestamp.ToString("HH:mm"),
                FullTimestamp = timestamp
            });

            // Scroll to the bottom after adding a message
            Dispatcher.InvokeAsync(() =>
            {
                ChatScrollViewer.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Adds a new chat button to the left list.
        /// </summary>
        public void ChatListAddChat(string chatName)
        {
            ChatListBox.Items.Add(chatName);
        }

        /// <summary>
        /// Clears all chats from the left list.
        /// </summary>
        public void ChatListClear()
        {
            ChatListBox.Items.Clear();
        }

        /// <summary>
        /// Clears all messages from the central chat area.
        /// </summary>
        public void ChatClear()
        {
            Messages.Clear();
        }

        // ---------------------------------------------------------------------
        // UI EVENT HANDLERS
        // ---------------------------------------------------------------------

        public void SetTheme(bool isDark)
        {
            isDarkTheme = isDark;
            if (isDarkTheme)
            {
                // Apply Dark Theme resources
                Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(24, 26, 27));
                Resources["SidebarBrush"] = new SolidColorBrush(Color.FromRgb(36, 37, 38));
                Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(227, 227, 227));
                Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(179, 179, 179));
                Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(55, 57, 58));
                Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 132, 255));
                Resources["MessageBubbleBrush"] = new SolidColorBrush(Color.FromRgb(55, 57, 58));
                Resources["HoverBrush"] = new SolidColorBrush(Color.FromRgb(45, 47, 48));
                Resources["InputBrush"] = new SolidColorBrush(Color.FromRgb(55, 57, 58));
                Resources["TimeBrush"] = new SolidColorBrush(Color.FromRgb(150, 152, 156));
                ThemeIcon.Text = "☀️";
            }
            else
            {
                // Apply Light Theme resources
                Resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
                Resources["SidebarBrush"] = new SolidColorBrush(Color.FromRgb(240, 242, 245));
                Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(101, 103, 107));
                Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(228, 230, 235));
                Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 132, 255));
                Resources["MessageBubbleBrush"] = new SolidColorBrush(Color.FromRgb(228, 230, 235));
                Resources["HoverBrush"] = new SolidColorBrush(Color.FromRgb(228, 230, 235));
                Resources["InputBrush"] = new SolidColorBrush(Color.FromRgb(240, 242, 245));
                Resources["TimeBrush"] = new SolidColorBrush(Color.FromRgb(138, 141, 145));
                ThemeIcon.Text = "🌙";
            }
        }

        public bool GetTheme() { return isDarkTheme; }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            SetTheme(isDarkTheme);
        }

        private void ChatButton_Click(object sender, RoutedEventArgs e)
        {
            // Visual feedback: select the clicked item in the ListBox
            if (sender is Button button && button.DataContext is string chatName)
            {
                ChatListBox.SelectedItem = chatName;

                // Optional: Clear chat when switching, depending on your logic
                // ChatClear(); 
            }
        }

        private void MessageInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SendMessage();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

            SendMessage();
        }

        private void SendMessage()
        {
            string text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Add message using the requested method structure
            ChatAddMessage("Вы", text , DateTime.Now);

            var MW = Application.Current.MainWindow as MainWindow;
            ZJSON.MessageSendData data = new ZJSON.MessageSendData
            {
                ID = MW.GetUserID(),
                Message = text,
                Channel = GetCurrentSelectedChat()
            };
            ZJSON.MessageSerializer ser = new ZJSON.MessageSerializer();
            MW.client.SendCustomData(ser.Serialize(data));

            MessageInput.Clear();
            MessageInput.Focus();
        }
    }

    // Helper class for message data binding with time support
    public class ChatMessage
    {
        public string UserName { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }          // Formatted time (HH:mm)
        public DateTime FullTimestamp { get; set; } // Full timestamp for sorting/filtering
    }
}