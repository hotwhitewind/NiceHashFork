using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewStyleMiner.Utils
{
    static public class PaySystemHelper
    {
        public enum PaySystemType { QIWI, VISA };
        public static Dictionary<PaySystemType, Tuple<string, string, string, string>> PaySystemDictionary { get; } = new Dictionary<PaySystemType, Tuple<string, string, string, string>>()
        {
            { PaySystemType.QIWI, new Tuple<string, string, string, string>("#-###-###-##-##", "\\d{10,10}", "Resources\\paymentsicon\\qiwi.png","QIWI")},
            { PaySystemType.VISA, new Tuple<string, string, string, string>("####-####-####-####", "\\d{16,16}", "Resources\\paymentsicon\\visa.png", "VISA/MasterCard")}
        }; 
    }
}
