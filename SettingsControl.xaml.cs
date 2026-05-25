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
            SelectComboBoxItem(cbOnStartup, Plugin.Settings.OnStartup);
            SelectComboBoxItem(cbOnShutdown, Plugin.Settings.OnShutdown);
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

        private void cbOnStartup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            Plugin.Settings.OnStartup = GetSelectedComboBoxText(cbOnStartup);
        }

        private void cbOnShutdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Plugin == null)
            {
                return;
            }

            Plugin.Settings.OnShutdown = GetSelectedComboBoxText(cbOnShutdown);
        }

        private static string GetSelectedComboBoxText(ComboBox comboBox)
        {
            var item = comboBox.SelectedItem as ComboBoxItem;
            return item == null || item.Content == null ? "" : item.Content.ToString();
        }

        private static void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                var itemValue = item.Content == null ? "" : item.Content.ToString();
                if (itemValue == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }
    }
}
