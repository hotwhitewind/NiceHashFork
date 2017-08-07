using System.Windows.Input;
using NiceHashMiner;
using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;
using NewStyleMiner.AdditionalDialogs;
using System.Timers;
using System;
using NiceHashMiner.Devices;
using System.Management;
using NiceHashMiner.Configs;
using NiceHashMiner.Miners;
using NiceHashMiner.Enums;
using System.Collections.Generic;
using System.Globalization;
using NiceHashMiner.Utils;
using NewStyleMiner.Utils;
using System.Threading.Tasks;
using LiveCharts;
using LiveCharts.Configurations;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using static NewStyleMiner.Utils.PaySystemHelper;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

namespace NewStyleMiner.ViewModels
{
    public class MainViewModel : ViewModelBase, LoadingViewModel.IAfterInitializationCaller
    {
        #region Field

        private Timer MinerStatsCheck;
        private Timer UpdateCheck;
        private Timer SMACheck;
        private Timer BalanceCheck;
        private Timer SMAMinerCheck;
        private Timer BitcoinExchangeCheck;
        private Timer StartupTimer;
        private Timer IdleCheck;

        private bool _showWarningNiceHashData;
        private bool _demoMode;
        private static String _worker = "worker1";
        private Random R;

        public string CurrentBitcoinAddress => ConfigManager.GeneralConfig.BitcoinAddress == "" ? "1F573CSKQZNCTHRgDpGAMXwkV1GiiBxAEL" : ConfigManager.GeneralConfig.BitcoinAddress;

        private bool IsManuallyStarted = false;

        private IDialogCoordinator _dialogCoordinator;
        private double _axisMax;
        private double _axisMin;
        private MainWindow _mainWindow;
        #endregion

        public MainViewModel()
        {
            CommonSettingsModel = new CommonSettingsModel();
            SettingsViewModel = new SettingsViewModel(CommonSettingsModel);
            LoadingViewModel = new LoadingViewModel(this);
            _yAsixTitle = "mBTC/Day";
            var mapper = Mappers.Xy<MeasureModel>()
    .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
    .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //the values property will store our values array
            ChartValues = new ChartValues<MeasureModel>();

            //lets set how to display the X Labels
            DateTimeFormatter = value => new DateTime((long)value).ToString("hh:mm:ss");

            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(60).Ticks;
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerMinute;

            SetAxisLimits(DateTime.Now);

            ConfigManager.GeneralConfig.WorkerName = _worker;
            ConfigManager.GeneralConfig.Use3rdPartyMiners = Use3rdPartyMiners.YES;
            LoadingViewModel.OnShow += LoadingViewModel_OnShow;

            OpenSettingsWindow = new DelegateCommand<object>(async (c) =>
            {
                SettingsViewModel.InitViewModel();
                var setting = new Settings();
                setting.ShowDialog();
                
                OnPropertyChanged("");
                _mainWindow.IsClose = !ConfigManager.GeneralConfig.HideToTray;
                if (SettingsViewModel.IsChange && SettingsViewModel.IsChangeSaved && SettingsViewModel.IsRestartNeeded)
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    await _dialogCoordinator.ShowMessageAsync(this, International.GetText("Form_Main_Restart_Required_Title"), International.GetText("Form_Main_Restart_Required_Msg"), MessageDialogStyle.Affirmative, mySetting);
                    Process.Start(Application.ResourceAssembly.Location);
                    Application.Current.Shutdown();
                }
            });
            PayCommand = new DelegateCommand<object>(async (args) =>
            {
                await GetPayFromServer();
            });
            
            WindowLoaded = new DelegateCommand<object>(async (args) =>
            {
                _mainWindow = (MainWindow) args;
                _mainWindow.IsClose = !ConfigManager.GeneralConfig.HideToTray;
                // general loading indicator
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Warning_with_Exclamation"), International.GetText("Warning_Antivirus"), MessageDialogStyle.Affirmative, mySetting);
                }));

                _isMainWindowEnable = false;
                LoadingViewModel.LoadingLabelTitle = International.GetText("Form_Loading_label_LoadingText");
                LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_GetNewStyleBalance");
                var loadingForm = new LoadingForm();
                loadingForm.Show();
            });

            WindowClosing = new DelegateCommand<object>(async (args) =>
            {
                if (_mainWindow.IsClose == false) return;
                if (IsManuallyStarted)
                {
                    IsManuallyStarted = false;
                    await StopMining();
                }
                ConfigManager.GeneralConfigFileCommit();
                LoadingViewModel.CloseTrigger = true;
            });

            StartMiningProcess = new DelegateCommand<object>(async (args) =>
            {
                if (IsManuallyStarted)
                {
                    IsManuallyStarted = false;
                    await StopMining();
                }
                else
                {
                    IsManuallyStarted = true;
                    StartMiningReturnType res = await StartMining(true);
                    if (res == StartMiningReturnType.ShowNoMining)
                    {
                        IsManuallyStarted = false;
                        await StopMining();
                        var mySetting = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = International.GetText("Global_OK"),
                            AnimateShow = true,
                            AnimateHide = false
                        };

                        var result = await _dialogCoordinator.ShowMessageAsync(this, International.GetText("Warning_with_Exclamation"), International.GetText("Form_Main_StartMiningReturnedFalse"), MessageDialogStyle.Affirmative, mySetting);
                    }
                    if (res == StartMiningReturnType.IgnoreMsg)
                        IsManuallyStarted = false;
                }
                OnPropertyChanged("");
            });
            ComputeDeviceManager.SystemSpecs.QueryAndLog();
            ManagementObjectCollection moc = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem").Get();
            foreach (var mo in moc)
            {
                var totalRam = long.Parse(mo["TotalVisibleMemorySize"].ToString()) / 1024;
                var pageFileSize = (long.Parse(mo["TotalVirtualMemorySize"].ToString()) / 1024) - totalRam;
                Helpers.ConsolePrint("NICEHASH", "Total RAM: " + totalRam + "MB");
                Helpers.ConsolePrint("NICEHASH", "Page File Size: " + pageFileSize + "MB");
            }

            R = new Random((int)DateTime.Now.Ticks);
        }

        #region Event
        private void LoadingViewModel_OnShow()
        {
            StartupTimer = new Timer();
            StartupTimer.Elapsed += async (sender, e) => await StartupTimer_Tick(sender, e);
            StartupTimer.Interval = 200;
            StartupTimer.Start();
        }
        #endregion

        #region Method

        private async Task StartupTimer_Tick(object sender, EventArgs e)
        {
            StartupTimer.Stop();
            StartupTimer = null;
            InitMainConfigGUIData();

            // Internals Init
            // TODO add loading step
            LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_CPU");

            MinersSettingsManager.Init();

            if (!Helpers.InternalCheckIsWow64())
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Error_with_Exclamation"),
                        International.GetText("Form_Main_x64_Support_Only"), MessageDialogStyle.Affirmative, mySetting);

                    Application.Current.Shutdown();
                }));
            }

            await ComputeDeviceManager.Query.QueryDevices(LoadingViewModel, this, _dialogCoordinator);

            /////////// from here on we have our devices and Miners initialized
            ConfigManager.AfterDeviceQueryInitialization();
            LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_SaveConfig");
            
            
            await BalanceCheck_Tick(null, null); // update currency changes

            MinerStatsCheck = new Timer();
            MinerStatsCheck.Elapsed += async (sender2, e2) => await MinerStatsCheck_Tick(sender2, e2);//new ElapsedEventHandler(MinerStatsCheck_Tick);
            MinerStatsCheck.Interval = ConfigManager.GeneralConfig.MinerAPIQueryInterval * 1000;

            SMAMinerCheck = new Timer();
            SMAMinerCheck.Elapsed += new ElapsedEventHandler(SMAMinerCheck_Tick);
            SMAMinerCheck.Interval = ConfigManager.GeneralConfig.SwitchMinSecondsFixed * 1000 + R.Next(ConfigManager.GeneralConfig.SwitchMinSecondsDynamic * 1000);
            if (ComputeDeviceManager.Group.ContainsAMD_GPUs)
            {
                SMAMinerCheck.Interval = (ConfigManager.GeneralConfig.SwitchMinSecondsAMD + ConfigManager.GeneralConfig.SwitchMinSecondsFixed) * 1000 + R.Next(ConfigManager.GeneralConfig.SwitchMinSecondsDynamic * 1000);
            }

            UpdateCheck = new Timer();
            UpdateCheck.Elapsed += async (sender3, e3) => await UpdateCheck_Tick(sender3, e3);//new ElapsedEventHandler(UpdateCheck_Tick);
            UpdateCheck.Interval = 1000 * 3600 * 24; // every 24 hour
            UpdateCheck.Start();
            await UpdateCheck_Tick(null, null);

            LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_GetNewStyleSMA");

            SMACheck = new Timer();
            SMACheck.Elapsed += async (sender1, e1) => await SMACheck_Tick(sender1, e1);//new ElapsedEventHandler(SMACheck_Tick);
            SMACheck.Interval = 60 * 1000 * 2; // every 2 minutes
            SMACheck.Start();

            //increase timeout
            if (Globals.IsFirstNetworkCheckTimeout)
            {
                while (!Helpers.WebRequestTestGoogle() && Globals.FirstNetworkCheckTimeoutTries > 0)
                {
                    --Globals.FirstNetworkCheckTimeoutTries;
                }
            }

            await SMACheck_Tick(null, null);

            LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_GetBTCRate");

            BitcoinExchangeCheck = new Timer();
            BitcoinExchangeCheck.Elapsed += async (sender3, e3) => await BitcoinExchangeCheck_Tick(sender3, e3);//new ElapsedEventHandler(BitcoinExchangeCheck_Tick);
            BitcoinExchangeCheck.Interval = 1000 * 3601; // every 1 hour and 1 second
            BitcoinExchangeCheck.Start();
            await BitcoinExchangeCheck_Tick(null, null);

            LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_GetNewStyleBalance");


            BalanceCheck = new Timer();
            BalanceCheck.Elapsed += async (sender2, e2) => await BalanceCheck_Tick(sender2, e2);
            BalanceCheck.Interval = 61 * 1000 * 5; // every ~5 minutes
            BalanceCheck.Start();
            await BalanceCheck_Tick(null, null);
            
            LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_SetEnvironmentVariable");

            Helpers.SetDefaultEnvironmentVariables();

            LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_SetWindowsErrorReporting");


            Helpers.DisableWindowsErrorReporting(ConfigManager.GeneralConfig.DisableWindowsErrorReporting);

            if (ConfigManager.GeneralConfig.NVIDIAP0State)
            {
                LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_NVIDIAP0State");
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "nvidiasetp0state.exe",
                        Verb = "runas",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.WaitForExit();
                        if (p.ExitCode != 0)
                            Helpers.ConsolePrint("NICEHASH", "nvidiasetp0state returned error code: " + p.ExitCode.ToString());
                        else
                            Helpers.ConsolePrint("NICEHASH", "nvidiasetp0state all OK");
                    }
                }
                catch (Exception ex)
                {
                    Helpers.ConsolePrint("NICEHASH", "nvidiasetp0state error: " + ex.Message);
                }
            }

            var runVcRed = !MinersExistanceChecker.IsMinersBinsInit() && !ConfigManager.GeneralConfig.DownloadInit;
            //standard miners check scope
            {
                // check if download needed
                if (!MinersExistanceChecker.IsMinersBinsInit() && !ConfigManager.GeneralConfig.DownloadInit)
                {
                    LoadingViewModel.MinerDownloader = new MinersDownloader(MinersDownloadManager.StandardDlSetup);
                    LoadingViewModel.IsMinerDownload = true;
                    await LoadingViewModel.StartMinerDownloads(_dialogCoordinator);
                }

                // check if files are mising
                if (!MinersExistanceChecker.IsMinersBinsInit())
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var mySetting = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = International.GetText("Global_Yes"),
                            NegativeButtonText = International.GetText("Global_No"),
                            AnimateShow = true,
                            AnimateHide = false
                        };

                        var result = _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Warning_with_Exclamation"), International.GetText("Form_Main_bins_folder_files_missing"), MessageDialogStyle.AffirmativeAndNegative, mySetting);
                        if (result == MessageDialogResult.Affirmative)
                        {
                            ConfigManager.GeneralConfig.DownloadInit = false;
                            ConfigManager.GeneralConfigFileCommit();
                            Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        }
                    }));
                }
                else if (!ConfigManager.GeneralConfig.DownloadInit)
                {
                    // all good
                    ConfigManager.GeneralConfig.DownloadInit = true;
                    ConfigManager.GeneralConfigFileCommit();
                }
            }

            //3rdparty miners check scope #2
            {
                // check if download needed
                if (ConfigManager.GeneralConfig.Use3rdPartyMiners == Use3rdPartyMiners.YES)
                {
                    if (!MinersExistanceChecker.IsMiners3rdPartyBinsInit() && !ConfigManager.GeneralConfig.DownloadInit3rdParty)
                    {
                        LoadingViewModel.MinerDownloader = new MinersDownloader(MinersDownloadManager.ThirdPartyDlSetup);
                        LoadingViewModel.IsMinerDownload = true;
                        await LoadingViewModel.StartMinerDownloads(_dialogCoordinator);
                    }
                    // check if files are mising
                    if (!MinersExistanceChecker.IsMiners3rdPartyBinsInit())
                    {
                        var mySetting = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = International.GetText("Global_Yes"),
                            NegativeButtonText = International.GetText("Global_No"),
                            AnimateShow = true,
                            AnimateHide = false
                        };

                        await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var result = _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Warning_with_Exclamation"), International.GetText("Form_Main_bins_folder_files_missing"), MessageDialogStyle.AffirmativeAndNegative, mySetting);
                            if (result == MessageDialogResult.Affirmative)
                            {
                                ConfigManager.GeneralConfig.DownloadInit = false;
                                ConfigManager.GeneralConfigFileCommit();
                                Process.Start(Application.ResourceAssembly.Location);
                                Application.Current.Shutdown();
                            }
                        }
                        ));
                    }
                    else if (!ConfigManager.GeneralConfig.DownloadInit3rdParty)
                    {
                        // all good
                        ConfigManager.GeneralConfig.DownloadInit3rdParty = true;
                        ConfigManager.GeneralConfigFileCommit();
                    }
                }
            }

            if (runVcRed)
            {
                //Helpers.InstallVcRedist();
            }

            LoadingViewModel.LoadingLabelText = International.GetText("Benchmark_Start");

            //бечмарк
            if (!ConfigManager.GeneralConfig.IsBenchmarkFirstTimeDoing)
            {
                await BencmarkDevices();
            }

            LoadingViewModel.FinishLoading();
            IsMainWindowEnable = true;
            // no bots please
            if (ConfigManager.GeneralConfigHwidLoadFromFile() && !ConfigManager.GeneralConfigHwidOK())
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_Yes"),
                        NegativeButtonText = International.GetText("Global_No"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    var result = _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Warning_with_Exclamation"), International.GetText("Form_Main_msgbox_anti_botnet_msgbox"), MessageDialogStyle.AffirmativeAndNegative, mySetting);

                    if (result == MessageDialogResult.Negative)
                    {
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        // users agrees he installed it so commit changes
                        ConfigManager.GeneralConfigFileCommit();
                    }
                }));
            }
            else
            {
                if (ConfigManager.GeneralConfig.AutoStartMining)
                {
                    // well this is started manually as we want it to start at runtime
                    IsManuallyStarted = true;
                    if (await StartMining(true) != StartMiningReturnType.StartMining)
                    {
                        IsManuallyStarted = false;
                        await StopMining();
                    }
                }
            }
        }

        private async Task BencmarkDevices()
        {
            ComputeDeviceManager.Group.EnableCpuGroup();
            //бечмарк
            var benchmark = new BenchmarkHelp();
            if (benchmark.NeedBenchmark())
                await benchmark.StartBenchmark();
            // disable all pending benchmark
            foreach (var cDev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
            {
                foreach (var algorithm in cDev.GetAlgorithmSettings())
                {
                    algorithm.ClearBenchmarkPending();
                }
            }

            // save already benchmarked algorithms
            ConfigManager.CommitBenchmarks();
            // check devices without benchmarks
            foreach (var cdev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
            {
                if (cdev.Enabled)
                {
                    bool Enabled = false;
                    foreach (var algo in cdev.GetAlgorithmSettings())
                    {
                        if (algo.BenchmarkSpeed > 0)
                        {
                            Enabled = true;
                            break;
                        }
                    }
                    cdev.Enabled = Enabled;
                }
            }
            ComputeDeviceManager.Group.UncheckedCPU();
            ConfigManager.GeneralConfig.IsBenchmarkFirstTimeDoing = true;
            ConfigManager.GeneralConfigFileCommit();
        }

        async Task BitcoinExchangeCheck_Tick(object sender, EventArgs e)
        {
            Helpers.ConsolePrint("NICEHASH", "Bitcoin rate get");
            await ExchangeRateAPI.UpdateAPI(_worker);
            var br = ExchangeRateAPI.GetUSDExchangeRate();
            if (br > 0) Globals.BitcoinUSDRate = br;
            Helpers.ConsolePrint("NICEHASH", "Current Bitcoin rate: " + Globals.BitcoinUSDRate.ToString("F2", CultureInfo.InvariantCulture));
            await NiceHashStats.GetCursRub();
            var bs = NiceHashStats.GetRUBExchangeRate();
            if (bs > 0) Globals.BitcoinRUBRate = bs;
        }

        async Task SMACheck_Tick(object sender, EventArgs e)
        {
            var worker = CurrentBitcoinAddress + "." + _worker;
            Helpers.ConsolePrint("NICEHASH", "SMA get");
            Dictionary<AlgorithmType, NiceHashSMA> t = null;

            for (var i = 0; i < 5; i++)
            {
                t = await NiceHashStats.GetAlgorithmRates(worker);
                if (t != null)
                {
                    Globals.NiceHashData = t;
                    break;
                }

                Helpers.ConsolePrint("NICEHASH", "SMA get failed .. retrying");
                System.Threading.Thread.Sleep(1000);
                t = await NiceHashStats.GetAlgorithmRates(worker);
            }

            if (t == null && Globals.NiceHashData == null && _showWarningNiceHashData)
            {
                _showWarningNiceHashData = false;
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_Yes"),
                        NegativeButtonText = International.GetText("Global_No"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    var dialogResult = _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Form_Main_msgbox_NoInternetTitle"), International.GetText("Form_Main_msgbox_NoInternetMsg"), MessageDialogStyle.AffirmativeAndNegative, mySetting);
                    switch (dialogResult)
                    {
                        case MessageDialogResult.Affirmative:
                            return;
                        case MessageDialogResult.Negative:
                            Application.Current.Shutdown();
                            break;
                    }
                }));
            }
        }

        //апдейт пока не работает, не понятно откуда обновлять
        async Task UpdateCheck_Tick(object sender, EventArgs e)
        {
            Helpers.ConsolePrint("NICEHASH", "Version get");

            var ver = await NiceHashStats.GetNewVersion();

            if (ver == null) return;
            var programVersion = new Version(Globals.AppVersion);
            var onlineVersion = new Version(ver.version);
            var ret = programVersion.CompareTo(onlineVersion);

            if (ret < 0)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };
                    var message = string.Format(International.GetText("Form_Main_new_version_released"), ver.version, ver.download_link);

                    _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Warning_with_Exclamation"),
                        message, MessageDialogStyle.Affirmative, mySetting);
                }));
            }
        }

        private void SMAMinerCheck_Tick(object sender, EventArgs e)
        {
            SMAMinerCheck.Interval = ConfigManager.GeneralConfig.SwitchMinSecondsFixed * 1000 + R.Next(ConfigManager.GeneralConfig.SwitchMinSecondsDynamic * 1000);
            if (ComputeDeviceManager.Group.ContainsAMD_GPUs)
            {
                SMAMinerCheck.Interval = (ConfigManager.GeneralConfig.SwitchMinSecondsAMD + ConfigManager.GeneralConfig.SwitchMinSecondsFixed) * 1000 + R.Next(ConfigManager.GeneralConfig.SwitchMinSecondsDynamic * 1000);
            }

#if (SWITCH_TESTING)
            SMAMinerCheck.Interval = MiningDevice.SMAMinerCheckInterval;
#endif
            MinersManager.SwichMostProfitableGroupUpMethod(Globals.NiceHashData);
        }

        private async Task MinerStatsCheck_Tick(object sender, EventArgs e)
        {
            //получение скорости и отправка на сервер рейтов
            MinersManager.MinerStatsCheck(Globals.NiceHashData);
            await UpdateGlobalRate();
        }

        public void AfterLoadComplete()
        {
            IdleCheck = new Timer();
            IdleCheck.Elapsed += async (sender, e) => await IdleCheck_Tick(sender, e);
            IdleCheck.Interval = 500;
            IdleCheck.Start();
        }

        private async Task IdleCheck_Tick(object sender, EventArgs e)
        {
            if (!ConfigManager.GeneralConfig.StartMiningWhenIdle || IsManuallyStarted) return;

            var msIdle = Helpers.GetIdleTime();

            try
            {
                if (MinerStatsCheck.Enabled)
                {
                    if (msIdle < (ConfigManager.GeneralConfig.MinIdleSeconds * 1000))
                    {
                        await StopMining();
                        Helpers.ConsolePrint("NICEHASH", "Resumed from idling");
                    }
                }
                else
                {
                    if ((msIdle > (ConfigManager.GeneralConfig.MinIdleSeconds * 1000)))
                    {
                        Helpers.ConsolePrint("NICEHASH", "Entering idling state");
                        if (await StartMining(false) != StartMiningReturnType.StartMining)
                        {
                            await StopMining();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Helpers.ConsolePrint("IdleCheck exception", "MinerStatsCheck exception:" + ex.Message);
            }
        }

        private void InitMainConfigGUIData()
        {
            _showWarningNiceHashData = true;
            _demoMode = false;
            // init active display currency after config load
            ExchangeRateAPI.ActiveDisplayCurrency = ConfigManager.GeneralConfig.DisplayCurrency;
        }

        async Task BalanceCheck_Tick(object sender, EventArgs e)
        {
            if (await VerifyMiningAddress(false))
            {
                //_currentBalance = (int)await NiceHashStats.GetBalance(CurrentBitcoinAddress, CurrentBitcoinAddress + "." + _worker);
                //заглушка на баланс
                var retBalance = await NiceHashStats.GetNewBalance(IdentificatorText);
                if (retBalance != null)
                {
                    Helpers.ConsolePrint("NICEHASH", "Balance get");
                    var curBalance = retBalance.currency.First(c => c.name.Equals("RUB"));
                    //здесь будет код получения баланса с сервера и соответсвенно вывода его она экран
                    if (curBalance != null)
                    {
                        BalanceAmount = $"{curBalance.value} {curBalance.symbol}";
                        Helpers.ConsolePrint("CurrencyConverter",
                            "Using CurrencyConverter" + ConfigManager.GeneralConfig.DisplayCurrency);
                    }
                }
            }
        }

        private async Task<bool> VerifyMiningAddress(bool showError)
        {
            if (!BitcoinAddress.ValidateBitcoinAddress(CurrentBitcoinAddress) && showError)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_Yes"),
                        NegativeButtonText = International.GetText("Global_No"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    var result = _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Error_with_Exclamation"), International.GetText("Form_Main_msgbox_InvalidBTCAddressMsg"), MessageDialogStyle.AffirmativeAndNegative, mySetting);

                    if (result == MessageDialogResult.Affirmative)
                        System.Diagnostics.Process.Start(Links.NHM_BTC_Wallet_Faq);
                }));
                return false;
            }
            if (!BitcoinAddress.ValidateWorkerName(_worker) && showError)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    _dialogCoordinator.ShowModalMessageExternal(this,
                        International.GetText("Error_with_Exclamation"),
                        International.GetText("Form_Main_msgbox_InvalidWorkerNameMsg"), MessageDialogStyle.Affirmative,
                        mySetting);
                }));
                return false;
            }
            return true;
        }

        public void SetDialogCoordinator(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
        }

        private async Task UpdateGlobalRate()
        {
            var totalRate = MinersManager.GetTotalRate();

            var now = DateTime.Now;


            //lets only use the last 150 values

            if (ConfigManager.GeneralConfig.AutoScaleBTCValues && totalRate < 0.1)
            {
                YAsixTitle = "mBTC/Day";
                ChartValues.Add(new MeasureModel
                {
                    DateTime = now,
                    Value = totalRate * 1000
                });
            }
            else
            {
                YAsixTitle = "BTC/Day";
                ChartValues.Add(new MeasureModel
                {
                    DateTime = now,
                    Value = totalRate
                });
            }

            SetAxisLimits(now);
            if (ChartValues.Count > 150) ChartValues.RemoveAt(0);
            //насчет профита надо спросить, где он будет браться
            if (Globals.BitcoinRUBRate > 0)
            {
                CalcProfitAndView(ExchangeRateAPI.ConvertToActiveCurrency(totalRate * Globals.BitcoinRUBRate / (60 * 24)));
            }
            else
            {
                CalcProfitAndView(ExchangeRateAPI.ConvertToActiveCurrency(totalRate * Globals.BitcoinUSDRate / (60 * 24)));
            }
            OnPropertyChanged("");
            //здесь надо вставить код отправки данных на сервер и обновление графика

            await NiceHashStats.SendRateValue(IdentificatorText, Math.Round(totalRate, 15));
        }

        private void CalcProfitAndView(double amountPerMinute)
        {
            DataGridSource = new ObservableCollection<AmountRate>();
            DataGridSource.Add(new AmountRate()
            {
                AmountType = DayCaption,
                AmountCount = Math.Round(amountPerMinute * 60 * 24, 2)
            });
            DataGridSource.Add(new AmountRate()
            {
                AmountType = WeekCaption,
                AmountCount = Math.Round(amountPerMinute * 60 * 24 * 7, 2)
            });
            DataGridSource.Add(new AmountRate()
            {
                AmountType = MonthCaption,
                AmountCount = Math.Round(amountPerMinute * 60 * 24 * 30, 2)
            });
            DataGridSource.Add(new AmountRate()
            {
                AmountType = YearCaption,
                AmountCount = Math.Round(amountPerMinute * 60 * 24 * 30 * 12, 2)
            });
        }

        private async Task GetPayFromServer()
        {
            //запрос на выплату и ожидание ответа

            if (String.IsNullOrEmpty(IdentificatorText))
            {
                //выведем сообщение что пользователь не зарегистрирован
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    _dialogCoordinator.ShowModalMessageExternal(this,
                        International.GetText("Error_with_Exclamation"), International.GetText("User_Not_Register"),
                        MessageDialogStyle.Affirmative, mySetting);
                }));
            }
            else
            {
                await CheckAndRegisterUser();

                var res = await NiceHashStats.SendPayRequest(IdentificatorText);
                //так здесь мы посмотрим что пользователь незарегистрирован и в этом случае зарегаем его
                if (!res)
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    _dialogCoordinator.ShowModalMessageExternal(this,
                        International.GetText("Error_with_Exclamation"), International.GetText("Pay_Request_Error"),
                        MessageDialogStyle.Affirmative, mySetting);
                }
                else
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_OK"),
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    _dialogCoordinator.ShowModalMessageExternal(this,
                        International.GetText("Warning_with_Exclamation"), International.GetText("Pay_Request_Success"),
                        MessageDialogStyle.Affirmative, mySetting);
                }
            }
        }

        private async Task CheckAndRegisterUser()
        {
            if (!string.IsNullOrEmpty(IdentificatorText))
            {
                //сначала пошлем запрос что пользователя нет

                var result2 = await NiceHashStats.GetAccess(IdentificatorText);

                //если нет, то зарегистрируем
                if (result2 != null)
                {
                    if (result2.access)
                    {
                        //тогда просто обновим кошелек
                        ConfigManager.GeneralConfig.BitcoinAddress = result2.wallet;
                    }
                    else
                    {


                        var result1 = await NiceHashStats.RegisterNewUser(IdentificatorText);
                        if (result1.error)
                        {
                            await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                var mySetting = new MetroDialogSettings()
                                {
                                    AffirmativeButtonText = International.GetText("Global_OK"),
                                    AnimateShow = true,
                                    AnimateHide = false
                                };

                                _dialogCoordinator.ShowModalMessageExternal(this,
                                    International.GetText("Error_with_Exclamation"),
                                    International.GetText("User_Error_All"),
                                    MessageDialogStyle.Affirmative, mySetting);
                            }));
                        }
                        else
                        {
                            ConfigManager.GeneralConfig.BitcoinAddress = result1.wallet;
                            ConfigManager.GeneralConfigFileCommit();
                        }
                    }
                }
                else
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var mySetting = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = International.GetText("Global_OK"),
                            AnimateShow = true,
                            AnimateHide = false
                        };

                        _dialogCoordinator.ShowModalMessageExternal(this,
                            International.GetText("Error_with_Exclamation"),
                            International.GetText("User_Error_Access"),
                            MessageDialogStyle.Affirmative, mySetting);
                    }));
                }
            }
        }
        #endregion

        #region Mining control porcedure

        private enum StartMiningReturnType
        {
            StartMining,
            ShowNoMining,
            IgnoreMsg
        }

        private async Task<StartMiningReturnType> StartMining(bool showWarnings)
        {
            if (string.IsNullOrEmpty(IdentificatorText))
            {
                if (showWarnings)
                {
                    var result = MessageDialogResult.Negative;
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var mySetting = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = International.GetText("Global_Yes"),
                            NegativeButtonText = International.GetText("Global_No"),
                            AnimateShow = true,
                            AnimateHide = false
                        };

                        result = _dialogCoordinator.ShowModalMessageExternal(this,
                            International.GetText("Form_Main_DemoModeTitle"),
                            International.GetText("Form_Main_DemoModeMsg"), MessageDialogStyle.AffirmativeAndNegative,
                            mySetting);
                    }));
                    if (result == MessageDialogResult.Affirmative)
                    {
                        _demoMode = true;
                    }
                    else
                    {
                        return StartMiningReturnType.IgnoreMsg;
                    }

                }
                else
                {
                    return StartMiningReturnType.IgnoreMsg;
                    ;
                }
            }
            else
            {
                await CheckAndRegisterUser();

                if (!(await VerifyMiningAddress(true)))
                    return StartMiningReturnType.IgnoreMsg;
            }
            //здесь надо послать запрос на регистрацию пользователя


            if (Globals.NiceHashData == null)
            {
                if (showWarnings)
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var mySetting = new MetroDialogSettings()
                        {
                            AffirmativeButtonText = International.GetText("Global_OK"),
                            AnimateShow = true,
                            AnimateHide = false
                        };

                        _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Error_with_Exclamation"), International.GetText("Form_Main_msgbox_NullNewStyleDataMsg"), MessageDialogStyle.Affirmative, mySetting);
                    }));
                }
                return StartMiningReturnType.IgnoreMsg;
            }


            // Check if there are unbenchmakred algorithms

            // Check if the user has run benchmark first
            //поменять если не прошел ни один
            if (!IsBencmark())
            {
                var res = MessageDialogResult.Negative;
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var mySetting = new MetroDialogSettings()
                    {
                        AffirmativeButtonText = International.GetText("Global_Yes"),
                        NegativeButtonText = International.GetText("Global_No"),
                        
                        AnimateShow = true,
                        AnimateHide = false
                    };

                    res = _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Warning_with_Exclamation"), International.GetText("EnabledUnbenchmarkedAlgorithmsWarning"), MessageDialogStyle.AffirmativeAndNegative, mySetting);
                }));
                if (res == MessageDialogResult.Affirmative)
                {
                    //LoadingViewModel.LoadingLabelTitle = International.GetText("Form_Loading_label_LoadingText");
                    //LoadingViewModel.LoadingLabelText = International.GetText("Form_Main_loadtext_GetNewStyleBalance");
                    //var loadingForm = new LoadingForm();
                    //loadingForm.Show();
                    //LoadingViewModel.LoadingLabelText = International.GetText("Benchmark_Start");
                    //await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    //{

                    //    var ctrl = _dialogCoordinator.ShowProgressAsync(this, International.GetText("Benchmark_Start"),
                    //            International.GetText("Form_Loading_label_LoadingText"))
                    //        .Result;
                    //    BencmarkDevices();
                    //    ctrl.CloseAsync();
                    //}));

                    var ctrl = await _dialogCoordinator.ShowProgressAsync(this, International.GetText("Benchmark_Start"),
                            International.GetText("Form_Loading_label_LoadingText"))
                        ;
                    await BencmarkDevices();
                    await ctrl.CloseAsync();

                    //LoadingViewModel.FinishLoading();

                    if (!IsBencmark())
                    {
                        await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var mySetting = new MetroDialogSettings()
                            {
                                AffirmativeButtonText = International.GetText("Global_OK"),
                                AnimateShow = true,
                                AnimateHide = false
                            };

                            _dialogCoordinator.ShowModalMessageExternal(this, International.GetText("Error_with_Exclamation"), International.GetText("Benchmark_Error"), MessageDialogStyle.Affirmative, mySetting);
                        }));
                        return StartMiningReturnType.IgnoreMsg;
                    }
                }
                else
                    return StartMiningReturnType.IgnoreMsg;
            }

            var btcAdress = CurrentBitcoinAddress;
            var isMining = MinersManager.StartInitialize(null, Globals.MiningLocation[ConfigManager.GeneralConfig.ServiceLocation], _worker, btcAdress);

            ConfigManager.GeneralConfigFileCommit();

            SMAMinerCheck.Interval = 100;
            SMAMinerCheck.Start();
            MinerStatsCheck.Start();

            return isMining ? StartMiningReturnType.StartMining : StartMiningReturnType.ShowNoMining;
        }

        private bool IsBencmark()
        {
            var isBenchInit = false;
            foreach (var cdev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
            {
                if (cdev.Enabled)
                {
                    foreach (var algo in cdev.GetAlgorithmSettings())
                    {
                        if (algo.Enabled)
                        {
                            if (algo.BenchmarkSpeed != 0)
                            {
                                isBenchInit = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isBenchInit;
        }

        private async Task StopMining()
        {
            MinerStatsCheck.Stop();
            SMAMinerCheck.Stop();

            MinersManager.StopAllMiners();

            if (_demoMode)
            {
                _demoMode = false;
            }

            await UpdateGlobalRate();
        }


        #endregion

        #region Properties

        private bool _isMainWindowEnable;

        public bool IsMainWindowEnable
        {
            get { return _isMainWindowEnable; }
            set
            {
                _isMainWindowEnable = value;
                OnPropertyChanged("IsMainWindowEnable");
            }
        }

        public ObservableCollection<AmountRate> DataGridSource { get; private set; }

        public string StartButtonCaption => !IsManuallyStarted ? International.GetText("Form_Main_start").ToUpper() : International.GetText("Form_Main_stop").ToUpper();

        public string PayButtonCaption => International.GetText("Form_Main_Pay").ToUpper();

        public string MinPayingSummCaption => International.GetText("MinPayingSumm");

        public string BalanceCaption => International.GetText("Form_Main_balance").ToUpper();

        public string AmountCaption => International.GetText("Form_Main_Pay_Amount");

        public string AmountRequsiteCaption => International.GetText("Form_Main_Pay_Requisite");

        public string TelNumberCaption => International.GetText("Form_Main_TelNumber");

        public string StatisticCaption => International.GetText("Form_Main_Statistic");

        public string ExpectedRevenueCaption => International.GetText("Form_Main_Expected_Revenue");

        public string MinuteCaption => International.GetText("MainMinute");

        public string HoureCaption => International.GetText("MainHoure");

        public string DayCaption => International.GetText("MainDay");

        public string WeekCaption => International.GetText("MainWeek");

        public string MonthCaption => International.GetText("MainMonth");

        public string YearCaption => International.GetText("MainYear");


        public ChartValues<MeasureModel> ChartValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisUnit { get; set; }

        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }
        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }

        private string _yAsixTitle;
        public string YAsixTitle
        {
            get { return _yAsixTitle; }
            set { _yAsixTitle = value; OnPropertyChanged("YAsixTitle"); }
        }

        private string _balanceAmount;

        public string BalanceAmount
        {
            get { return _balanceAmount; }
            set
            {
                _balanceAmount = value;
                OnPropertyChanged("BalanceAmount");
            }
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(300).Ticks; // and 5 minutes behind
        }

        //private string _payRequestText;
        //public string PayRequestText
        //{
        //    get
        //    {
        //        return _payRequestText;
        //    }
        //    set
        //    {
        //        _payRequestText = value;
        //        OnPropertyChanged("PayRequestText");
        //    }
        //}

        //private string _identificatorText;
        public string IdentificatorText
        {
            get
            {
                return ConfigManager.GeneralConfig.IdentificationString;
            }
            set
            {
                if (Regex.IsMatch(value,
                    PaySystemHelper.PaySystemDictionary[(PaySystemType) CurentPaySystemIndex].Item2) || value == "")
                {
                    ConfigManager.GeneralConfig.IdentificationString = value;
                    OnPropertyChanged("IdentificatorText");
                }
            }
        }

        public string MaskIdentificatorText => PaySystemHelper.PaySystemDictionary[(PaySystemType)CurentPaySystemIndex].Item1;

        public class Employed
        {
            public string ImageSource { set; get; }    
        }

        public List<string> PaySystem
        {
            get
            {
                //return Enum.GetNames(typeof(PaySystemType)).ToList();
                return PaySystemHelper.PaySystemDictionary.Values.Select(tuple => tuple.Item4).ToList();
                //return PaySystemHelper.PaySystemDictionary.Values.Select(tuple => new Employed {ImageSource = tuple.Item3}).ToList();
            }
        }

        public int CurentPaySystemIndex
        {
            get
            {
                return (int)ConfigManager.GeneralConfig.PaySystem;
            }
            set
            {
                ConfigManager.GeneralConfig.PaySystem = (PaySystemType) value;
                OnPropertyChanged("CurentPaySystemIndex");
                OnPropertyChanged("MaskIdentificatorText");
            }
        }
        #endregion

        #region Commands

        public ICommand OpenSettingsWindow { get; }
        public ICommand WindowLoaded { get; }
        public ICommand WindowClosing { get; }
        public ICommand StartMiningProcess { get; }
        public ICommand PayCommand { get; }
        #endregion

        #region Nested ViewModels
        public SettingsViewModel SettingsViewModel { get; }
        public LoadingViewModel LoadingViewModel { get; }
        public CommonSettingsModel CommonSettingsModel { get; }
        #endregion
    }
}
