using System;
using System.Collections.Generic;
using System.Linq;
using NiceHashMiner;
using NiceHashMiner.Enums;
using NiceHashMiner.Devices;
using NiceHashMiner.Miners.Parsing;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner.Configs;

namespace NewStyleMiner.ViewModels
{
    public class CommonSettingsModel : ViewModelBase
    {
        public List<string> AvailDevices
        {
            get
            {
                var list = new List<string>();
                var isCpu = false;
                var isGpu = false;
                foreach(var dev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                {
                    if (dev.NameCount.ToLower().Contains("cpu") && !isCpu)
                    {
                        list.Add(International.GetText("CommonPage_Processor"));
                        isCpu = true;
                    }
                    if (dev.NameCount.ToLower().Contains("gpu") && !isGpu)
                    {
                        isGpu = true;
                        list.Add(International.GetText("CommonPage_VideoCard"));
                    }
                }
                if(list.Count > 1)
                    list.Add(International.GetText("CommonPage_AllDevices"));
                return list;
            }
        }

        public string Devices
        {
            get { return International.GetText("CommonPage_DevicesCombobox"); }
        }
        public string HideToTray => International.GetText("HideToTray");

        public string Location => International.GetText("Service_Location");

        public string Language => International.GetText("Form_Settings_General_Language");

        public string AutostartMining => International.GetText("Form_Settings_General_AutoStartMining");

        public string DisableFindingNVIDIA => String.Format(International.GetText("Form_Settings_General_DisableDetection"), "NVIDIA");

        public string DisableFindingAMD => String.Format(International.GetText("Form_Settings_General_DisableDetection"), "AMD");

        public string StartMiningWhenIdle => International.GetText("Form_Settings_General_StartMiningWhenIdle");

        public string ShowDriverVersionWarning => International.GetText("Form_Settings_General_ShowDriverVersionWarning");

        public string DisableWindowsErrorReporting => International.GetText("Form_Settings_General_DisableWindowsErrorReporting");

        public string NVIDIAP0State => International.GetText("Form_Settings_General_NVIDIAP0State");

        public string DisableAMDTempControl => International.GetText("Form_Settings_General_DisableAMDTempControl");

        public string DisableDefaultOptimizations => International.GetText("Form_Settings_Text_DisableDefaultOptimizations");

        public string AllowMultipleInstances_Text => International.GetText("Form_Settings_General_AllowMultipleInstances_Text");

        public string TooltipLocation => International.GetText("Form_Settings_ToolTip_ServiceLocation");

        public string TooltipDevices => International.GetText("CommonPage_DevicesComboboxHint");

        public string TooltipLanguage => International.GetText("Form_Settings_ToolTip_Language");

        public string TooltipAutostartMining => International.GetText("Form_Settings_ToolTip_checkBox_AutoStartMining");

        public string TooltipDisableFindingNVIDIA => string.Format(International.GetText("Form_Settings_ToolTip_checkBox_DisableDetection"), "NVIDIA");

        public string TooltipDisableFindingAMD => string.Format(International.GetText("Form_Settings_ToolTip_checkBox_DisableDetection"), "AMD");

        public string TooltipStartMiningWhenIdle => International.GetText("Form_Settings_ToolTip_checkBox_StartMiningWhenIdle");

        public string TooltipShowDriverVersionWarning => International.GetText("Form_Settings_ToolTip_checkBox_ShowDriverVersionWarning");

        public string TooltipDisableWindowsErrorReporting => International.GetText("Form_Settings_ToolTip_checkBox_DisableWindowsErrorReporting");

        public string TooltipNVIDIAP0State => International.GetText("Form_Settings_ToolTip_checkBox_NVIDIAP0State");

        public string TooltipDisableAMDTempControl => International.GetText("Form_Settings_ToolTip_DisableAMDTempControl");

        public string TooltipDisableDefaultOptimizations => International.GetText("Form_Settings_ToolTip_DisableDefaultOptimizations");

        public string TooltipAllowMultipleInstances_Text => International.GetText("Form_Settings_General_AllowMultipleInstances_ToolTip");

        public string ToolTipHideToTray => International.GetText("ToolTipHideToTray");

        public List<string> Languages
        {
            get
            {
                var lang = International.GetAvailableLanguages();
                return lang.Values.ToList();
            }
        }

        public bool AutoStartManingCheck
        {
            get
            {
                return NiceHashMiner.Configs.ConfigManager.GeneralConfig.AutoStartMining;
            }
            set
            {
                NiceHashMiner.Configs.ConfigManager.GeneralConfig.AutoStartMining = value;
                OnPropertyChanged("AutoStartManing");
            }
        }

        public int DeviceSelectIndex
        {
            get
            {
                //если есть CPU то он будет первый в списке
                //потом видеокарта
                //потом все устройства
                bool isCPUEnabled = false;
                bool isVideoEnabled = false;
                int allDevice = 0;
                if(ComputeDeviceManager.Avaliable.AllAvaliableDevices.Any(c => c.DeviceType == DeviceType.CPU))
                {
                    allDevice++;
                }
                if (ComputeDeviceManager.Avaliable.AllAvaliableDevices.Any(c => c.DeviceType == DeviceType.NVIDIA || c.DeviceType == DeviceType.AMD))
                {
                    allDevice++;
                }

                if (allDevice == 0)
                    return -1;

                foreach (var dev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                {
                    if(dev.DeviceType == DeviceType.CPU && dev.Enabled)
                    {
                        isCPUEnabled = true;
                    }
                    if ((dev.DeviceType == DeviceType.AMD || dev.DeviceType == DeviceType.NVIDIA) && dev.Enabled)
                    {
                        isVideoEnabled = true;
                    }
                }

                if(isCPUEnabled && isVideoEnabled)
                    return 2;
                if (!isCPUEnabled && isVideoEnabled)
                {
                    if (allDevice == 2)
                        return 1;
                }
                if (!isCPUEnabled || isVideoEnabled) return 0;
                if (allDevice == 2)
                    return 0;
                return 0;
            }
            set
            {
                var allDevice = 0;
                if (ComputeDeviceManager.Avaliable.AllAvaliableDevices.Any(c => c.DeviceType == DeviceType.CPU))
                {
                    allDevice++;
                }
                if (ComputeDeviceManager.Avaliable.AllAvaliableDevices.Any(c => c.DeviceType == DeviceType.NVIDIA || c.DeviceType == DeviceType.AMD))
                {
                    allDevice++;
                }
                if(allDevice == 2)
                {
                    //то есть выбор
                    if(value == 0)
                    {
                        //это процессор
                        foreach (var dev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                        {
                            if (dev.DeviceType == DeviceType.CPU)
                            {
                                dev.Enabled = true;
                            }
                            if (dev.DeviceType == DeviceType.AMD || dev.DeviceType == DeviceType.NVIDIA)
                            {
                                dev.Enabled = false;
                            }
                        }
                    }
                    if(value == 1)
                    {
                        foreach (var dev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                        {
                            if (dev.DeviceType == DeviceType.AMD || dev.DeviceType == DeviceType.NVIDIA)
                            {
                                dev.Enabled = true;
                            }
                            if (dev.DeviceType == DeviceType.CPU)
                            {
                                dev.Enabled = false;
                            }
                        }
                    }
                    if(value == 2)
                    {
                        foreach (var dev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                        {
                            dev.Enabled = true;
                        }
                    }
                }
            }
        }

        public int LocationSelectIndex
        {
            get { return (int)NiceHashMiner.Configs.ConfigManager.GeneralConfig.ServiceLocation; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.ServiceLocation = value; OnPropertyChanged(); }
        }

        public int LanguageSelectIndex
        {
            get { return (int)NiceHashMiner.Configs.ConfigManager.GeneralConfig.Language; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.Language = (LanguageType)value; OnPropertyChanged(); }
        }

        public bool DisableFindingNVIDIACheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.DeviceDetection.DisableDetectionNVIDIA; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.DeviceDetection.DisableDetectionNVIDIA = value; OnPropertyChanged(); }
        }

        public bool DisableFindingAMDCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.DeviceDetection.DisableDetectionAMD; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.DeviceDetection.DisableDetectionAMD = value; OnPropertyChanged(); }
        }

        public bool StartMiningWhenIdleCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.StartMiningWhenIdle; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.StartMiningWhenIdle = value; OnPropertyChanged(); }
        }

        public bool ShowDriverVersionWarningCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.ShowDriverVersionWarning; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.ShowDriverVersionWarning = value; OnPropertyChanged(); }
        }

        public bool DisableWindowsErrorReportingCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.DisableWindowsErrorReporting; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.DisableWindowsErrorReporting = value; OnPropertyChanged(); }
        }

        public bool NVIDIAP0StateCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.NVIDIAP0State; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.NVIDIAP0State = value; OnPropertyChanged(); }
        }

        public bool DisableAMDTempControlCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.DisableAMDTempControl; }
            set
            {
                NiceHashMiner.Configs.ConfigManager.GeneralConfig.DisableAMDTempControl = value;
                OnPropertyChanged();
                foreach (var cDev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                {
                    if (cDev.DeviceType == DeviceType.AMD)
                    {
                        foreach (var algorithm in cDev.GetAlgorithmSettings())
                        {
                            if (algorithm.NiceHashID != AlgorithmType.DaggerHashimoto)
                            {
                                algorithm.ExtraLaunchParameters += AmdGpuDevice.TemperatureParam;
                                algorithm.ExtraLaunchParameters = ExtraLaunchParametersParser.ParseForMiningPair(
                                    new MiningPair(cDev, algorithm), algorithm.NiceHashID, DeviceType.AMD, false);
                            }
                        }
                    }
                }
            }
        }

        public bool DisableDefaultOptimizationsCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.DisableDefaultOptimizations; }
            set
            {
                NiceHashMiner.Configs.ConfigManager.GeneralConfig.DisableDefaultOptimizations = value;
                OnPropertyChanged();
                if (ConfigManager.GeneralConfig.DisableDefaultOptimizations)
                {
                    foreach (var cDev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                    {
                        foreach (var algorithm in cDev.GetAlgorithmSettings())
                        {
                            algorithm.ExtraLaunchParameters = "";
                            if (cDev.DeviceType == DeviceType.AMD && algorithm.NiceHashID != AlgorithmType.DaggerHashimoto)
                            {
                                algorithm.ExtraLaunchParameters += AmdGpuDevice.TemperatureParam;
                                algorithm.ExtraLaunchParameters = ExtraLaunchParametersParser.ParseForMiningPair(
                                    new MiningPair(cDev, algorithm), algorithm.NiceHashID, cDev.DeviceType, false);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var cDev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
                    {
                        if (cDev.DeviceType == DeviceType.CPU) continue; // cpu has no defaults
                        var deviceDefaultsAlgoSettings = GroupAlgorithms.CreateForDeviceList(cDev);
                        foreach (var defaultAlgoSettings in deviceDefaultsAlgoSettings)
                        {
                            var toSetAlgo = cDev.GetAlgorithm(defaultAlgoSettings.MinerBaseType, defaultAlgoSettings.NiceHashID, defaultAlgoSettings.SecondaryNiceHashID);
                            if (toSetAlgo != null)
                            {
                                toSetAlgo.ExtraLaunchParameters = defaultAlgoSettings.ExtraLaunchParameters;
                                toSetAlgo.ExtraLaunchParameters = ExtraLaunchParametersParser.ParseForMiningPair(
                                    new MiningPair(cDev, toSetAlgo), toSetAlgo.NiceHashID, cDev.DeviceType, false);
                            }
                        }
                    }
                }
            }
        }

        public bool AllowMultipleInstances_TextCheck
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.AllowMultipleInstances; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.AllowMultipleInstances = value; OnPropertyChanged(); }
        }

        public bool HideToTray_Check
        {
            get { return NiceHashMiner.Configs.ConfigManager.GeneralConfig.HideToTray; }
            set { NiceHashMiner.Configs.ConfigManager.GeneralConfig.HideToTray = value; OnPropertyChanged(); }
        }

        public CommonSettingsModel()
        {
        }
    }
}
