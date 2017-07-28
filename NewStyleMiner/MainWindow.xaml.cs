using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using NiceHashMiner.Configs;
using NiceHashMiner;
using MahApps.Metro.Controls.Dialogs;
using NewStyleMiner.ViewModels;
using NewStyleMiner.AdditionalDialogs;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace NewStyleMiner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public bool IsClose { get; set; }
        private readonly NotifyIcon _notifyIcon;

        public MainWindow()
        {
            IsClose = false;
            _notifyIcon = new NotifyIcon();
            _notifyIcon.MouseDoubleClick += NotifyIconOnMouseDoubleClick;
            var menuItem1 = new MenuItem
            {
                Index = 0,
                Text = "Exit"
            };
            menuItem1.Click += MenuItem1OnClick;

            _notifyIcon.ContextMenu = new ContextMenu();
            _notifyIcon.ContextMenu.MenuItems.Add(menuItem1);
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/bCash;component/Resources/icon_.ico")).Stream;
            _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            _notifyIcon.Visible = true;
            if (!ConfigManager.GeneralConfigIsFileExist())
            {
                Helpers.ConsolePrint("NICEHASH", "No config file found. Running NiceHash Miner for the first time. Choosing a default language.");
                var showDlg = new ChoosingLanguageWindow();
                showDlg.ShowDialog();
            }

            // Init languages
            International.Initialize(ConfigManager.GeneralConfig.Language);
            InitializeComponent();
            ((MainViewModel)DataContext).SetDialogCoordinator(DialogCoordinator.Instance);
        }

        private void NotifyIconOnMouseDoubleClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Button == MouseButtons.Left)
            {
                if (IsVisible)
                {
                    if (WindowState == WindowState.Minimized)
                        WindowState = WindowState.Normal;
                    Activate();
                }
                else
                {
                    Show();
                }
            }
        }

        private void MenuItem1OnClick(object sender, EventArgs eventArgs)
        {
            IsClose = true;
            Close();
        }

        private void MainWindowDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!IsClose)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _notifyIcon.Dispose();
            }
        }
    }
}
