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
            tbPassword.Text = Plugin.Settings.Password;
            tbSAIN.Text = Plugin.Settings.sAIN;
        }

        private void tbUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            Plugin.Settings.Username = tbUser.Text;
        }

        private void tbPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            Plugin.Settings.Password = tbPassword.Text;
        }

        private void tbSAIN_TextChanged(object sender, TextChangedEventArgs e)
        {
            Plugin.Settings.sAIN = tbSAIN.Text;
        }
    }
}
