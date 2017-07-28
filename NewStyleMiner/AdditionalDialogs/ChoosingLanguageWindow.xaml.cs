using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NiceHashMiner.Enums;
using NiceHashMiner;
using NiceHashMiner.Configs;

namespace NewStyleMiner
{
    /// <summary>
    /// Логика взаимодействия для ChoosingLanguageWindow.xaml
    /// </summary>
    public partial class ChoosingLanguageWindow : MetroWindow
    {

        public ChoosingLanguageWindow()
        {
            InitializeComponent();
            Dictionary<LanguageType, string> lang = International.GetAvailableLanguages();
            LangCB.DataContext = lang.Values;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.GeneralConfig.Language = (LanguageType)LangCB.SelectedIndex;
            ConfigManager.GeneralConfigFileCommit();
            this.Close();
        }
    }
}
