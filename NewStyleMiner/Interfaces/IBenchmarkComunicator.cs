using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NiceHashMiner.Interfaces {
    public interface IBenchmarkComunicator {

        void SetCurrentStatus(string status);
        Task OnBenchmarkComplete(bool success, string status);
    }
}
