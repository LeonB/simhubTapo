using System.Windows;
using System.Windows.Controls;

namespace LeonB.Tapo
{
    /// <summary>
    /// Logique d'interaction pour SettingsControlDemo.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public Tapoer Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();            
        }

        public SettingsControl(Tapoer plugin) : this()
        {
            this.Plugin = plugin;
            tbUser.Text = Plugin.Settings.Username;
            tbPassword.Password = Plugin.Settings.Password;
            tbIP.Text = Plugin.Settings.IP;
        }

        private void tbUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            Plugin.Settings.Username = tbUser.Text;
        }

        private void tbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Plugin.Settings.Password = tbPassword.Password;
        }

        private void tbIP_TextChanged(object sender, TextChangedEventArgs e)
        {
            Plugin.Settings.IP = tbIP.Text;
        }
    }
}
