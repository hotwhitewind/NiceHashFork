using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using NiceHashMiner;
using NiceHashMiner.Configs;
using NiceHashMiner.Devices;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.CompilerServices;
using NewStyleMiner.ViewModels;
using NewStyleMiner.AdditionalDialogs;

namespace NewStyleMiner.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        #region Properties
        private IDialogCoordinator _dialogCoordinator;
        public string Title
        {
            get { return International.GetText("Form_Main_settings"); }
        }

        public string SaveButtonCaption
        {
            get { return International.GetText("Form_Settings_buttonSaveText"); }
        }
        public string ResetButtonCaption
        {
            get { return International.GetText("Form_Settings_buttonDefaultsText"); }
        }
        public string CloseButtonCaption
        {
            get { return International.GetText("Form_Settings_buttonCloseNoSaveText"); }
        }

        public string CommonPageCaption
        {
            get { return International.GetText("FormSettings_Tab_General"); }
        }
        public string AlgoritmPageCaption
        {
            get { return International.GetText("FormSettings_Tab_Devices_Algorithms"); }
        }

        private bool _isInitFinished = false;
        public bool _isChange = false;
        public bool IsChange
        {
            get { return _isChange; }
            set
            {
                if (_isInitFinished)
                {
                    _isChange = value;
                }
                else
                {
                    _isChange = false;
                }
            }
        }
        public bool IsChangeSaved { get; private set; }
        public bool IsRestartNeeded { get; private set; }

        // most likely we wil have settings only per unique devices
        bool ShowUniqueDeviceList = true;

        ComputeDevice _selectedComputeDevice;

        #endregion

        public void SetDialogCoordinator(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
        }

        private void CloseWindow(Window window)
        {
            window?.Close();
        }

        public void InitViewModel()
        {
            IsChange = false;
            IsChangeSaved = false;

            // backup settings
            ConfigManager.CreateBackup();
            _isInitFinished = true;
        }

        public SettingsViewModel(CommonSettingsModel commonSettingsModel)
        {
            commonSettingsModel.PropertyChanged += Instance_PropertyChanged;
            DefaultButtonCommand = new DelegateCommand<object>(async (c) =>
            {
                var mySetting = new MetroDialogSettings()
                {
                    AffirmativeButtonText = International.GetText("Global_Yes"),
                    NegativeButtonText = International.GetText("Global_No"),
                    AnimateShow = true,
                    AnimateHide = false
                };

                var result = await _dialogCoordinator.ShowMessageAsync(this, International.GetText("Form_Settings_buttonDefaultsTitle"), International.GetText("Form_Settings_buttonDefaultsMsg"), MessageDialogStyle.AffirmativeAndNegative, mySetting);

                if (result == MessageDialogResult.Affirmative)
                {
                    IsChange = true;
                    IsChangeSaved = true;
                    ConfigManager.GeneralConfig.SetDefaults();

                    International.Initialize(ConfigManager.GeneralConfig.Language);
                }
                CloseWindow((Window)c);
            });
            SaveAndCloseButtonCommand = new DelegateCommand<object>(async (c) =>
            {
                var mySetting = new MetroDialogSettings()
                {
                    AffirmativeButtonText = International.GetText("Global_OK"),
                    AnimateShow = true,
                    AnimateHide = false,
                };

                await _dialogCoordinator.ShowMessageAsync(this, International.GetText("Form_Settings_buttonSaveTitle"),
                                        International.GetText("Form_Settings_buttonSaveMsg"),
                                        MessageDialogStyle.Affirmative, mySetting);

                IsChange = true;
                IsChangeSaved = true;
                CloseWindow((Window)c);
            });

            CloseButtonCommand = new DelegateCommand<object>(async (c) =>
            {
                IsChangeSaved = false;
                if (IsChange && !IsChangeSaved)
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_Yes"),
                        NegativeButtonText = International.GetText("Global_No"),
                        AnimateShow = true,
                        AnimateHide = false,
                    };

                    var result = await _dialogCoordinator.ShowMessageAsync(this, International.GetText("Form_Settings_buttonCloseNoSaveTitle"), International.GetText("Form_Settings_buttonCloseNoSaveMsg"),
                                                           MessageDialogStyle.AffirmativeAndNegative, mySetting);

                    if (result == MessageDialogResult.Negative)
                    {
                        return;
                    }
                    else
                        CloseWindow((Window)c);
                }
                CloseWindow((Window)c);
            });

            WindowClosing = new DelegateCommand<object>(
                (args) => {
                    // check restart parameters change
                    IsRestartNeeded = ConfigManager.IsRestartNeeded();

                    if (IsChangeSaved)
                    {
                        ConfigManager.GeneralConfigFileCommit();
                        ConfigManager.CommitBenchmarks();
                        International.Initialize(ConfigManager.GeneralConfig.Language);
                    }
                    else
                    {
                        ConfigManager.RestoreBackup();
                    }
                });
        }

        public void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsChange = true;
        }

        #region Commands

        public ICommand WindowClosing { get;}
        public ICommand DefaultButtonCommand { get; }
        public ICommand SaveAndCloseButtonCommand { get; }
        public ICommand CloseButtonCommand { get; }

        #endregion
    }
}
