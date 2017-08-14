using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NiceHashMiner;
using NiceHashMiner.Utils;
using Newtonsoft.Json;
using System.Globalization;
using NiceHashMiner.Configs;
using System.Diagnostics;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using NewStyleMiner.Utils;

namespace NewStyleMiner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Globals.JsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Culture = CultureInfo.InvariantCulture
            };

            // #1 first initialize config
            ConfigManager.InitializeConfig();
            //проверим реестр
            try
            {
                var regkey = Registry.CurrentUser.OpenSubKey(@"Software\bCash");
                if (regkey != null)
                {
                    var valueid = regkey.GetValue("Identification Number");
                    var valuepay = regkey.GetValue("Pay System");
                    if (valueid != null && valuepay != null)
                    {
                        switch (valuepay.ToString())
                        {
                            case "VISA/MasterCard":
                                ConfigManager.GeneralConfig.IdentificationString = valueid.ToString();
                                if (ConfigManager.GeneralConfig.IdentificationString.Length != 16)
                                    ConfigManager.GeneralConfig.IdentificationString = "";
                                ConfigManager.GeneralConfig.PaySystem = PaySystemHelper.PaySystemType.VISA;
                                break;
                            case "QIWI":
                                if (ConfigManager.GeneralConfig.IdentificationString.Length != 11)
                                    ConfigManager.GeneralConfig.IdentificationString = "";
                                ConfigManager.GeneralConfig.PaySystem = PaySystemHelper.PaySystemType.QIWI;
                                break;
                        }
                    }
                    Registry.CurrentUser.DeleteSubKey(@"Software\bCash");
                }
            }
            catch
            {
                
            }
            // #2 check if multiple instances are allowed
            bool startProgram = true;
            if (ConfigManager.GeneralConfig.AllowMultipleInstances == false)
            {
                try
                {
                    Process current = Process.GetCurrentProcess();
                    foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            startProgram = false;
                        }
                    }
                }
                catch { }
            }

            if (startProgram)
            {
                if (ConfigManager.GeneralConfig.LogToFile)
                {
                    Logger.ConfigureWithFile();
                }

                if (ConfigManager.GeneralConfig.DebugConsole)
                {
                    Helpers.AllocConsole();
                }

                // init active display currency after config load
                ExchangeRateAPI.ActiveDisplayCurrency = ConfigManager.GeneralConfig.DisplayCurrency;

                Helpers.ConsolePrint("NICEHASH", "Starting up NiceHashMiner");
                //bool tosChecked = ConfigManager.GeneralConfig.agreedWithTOS == Globals.CURRENT_TOS_VER;

                // check WMI
                if (Helpers.IsWMIEnabled())
                {
                    var mainDlg = new MainWindow();
                    mainDlg.ShowDialog();
                }
                else
                {
                    MessageBox.Show(International.GetText("Program_WMI_Error_Text"),
                                                            International.GetText("Program_WMI_Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
