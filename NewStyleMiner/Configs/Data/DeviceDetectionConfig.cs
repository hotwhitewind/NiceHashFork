using System;
using System.Collections.Generic;
using System.Text;

namespace NiceHashMiner.Configs.Data
{ 
    /// <summary>
    /// DeviceDetectionConfig is used to enable/disable detection of certain GPU type devices 
    /// </summary>
    /// 
    [Serializable]
    public class DeviceDetectionConfig {
        public bool DisableDetectionAMD;
        public bool DisableDetectionNVIDIA;

        public DeviceDetectionConfig()
        {
            DisableDetectionAMD = false;
            DisableDetectionNVIDIA = false;
        }
    }
}
