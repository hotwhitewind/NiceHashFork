﻿using System;
using System.Collections.Generic;
using NiceHashMiner.Configs;
using NiceHashMiner.Devices;
using NiceHashMiner.Enums;
using NiceHashMiner.Miners;
using NiceHashMiner.Interfaces;
using NiceHashMiner.Miners.Grouping;
using NiceHashMiner;
using System.Windows;
using System.Threading.Tasks;

namespace NewStyleMiner.Utils
{
    public class BenchmarkHelp : IBenchmarkComunicator, IBenchmarkCalculation
    {
        private TaskCompletionSource<bool> _tcs;

        private bool _inBenchmark = false;
        private int _bechmarkCurrentIndex = 0;
        private int _bechmarkedSuccessCount = 0;
        private int _benchmarkAlgorithmsCount = 0;
        private AlgorithmBenchmarkSettingsType _algorithmOption = AlgorithmBenchmarkSettingsType.SelectedUnbenchmarkedAlgorithms;

        private List<Miner> _benchmarkMiners;
        private Miner _currentMiner;
        private List<Tuple<ComputeDevice, Queue<Algorithm>>> _benchmarkDevicesAlgorithmQueue;

        private bool ExitWhenFinished = false;
        //private AlgorithmType _singleBenchmarkType = AlgorithmType.NONE;

        //private Timer _benchmarkingTimer;
        //private int dotCount = 0;

        public bool StartMining { get; private set; }

        private struct DeviceAlgo
        {
            public string Device { get; set; }
            public string Algorithm { get; set; }
        }
        private List<DeviceAlgo> _benchmarkFailedAlgoPerDev;

        private enum BenchmarkSettingsStatus : int
        {
            NONE = 0,
            TODO,
            DISABLED_NONE,
            DISABLED_TODO
        }
        private Dictionary<string, BenchmarkSettingsStatus> _benchmarkDevicesAlgorithmStatus;
        private ComputeDevice _currentDevice;
        private Algorithm _currentAlgorithm;

        private string CurrentAlgoName;

        // CPU benchmarking helpers
        private class CPUBenchmarkStatus
        {
            private class benchmark
            {
                public benchmark(int lt, double bench)
                {
                    LessTreads = lt;
                    Benchmark = bench;
                }
                public readonly int LessTreads;
                public readonly double Benchmark;
            }
            public CPUBenchmarkStatus(int max_threads)
            {
                _max_threads = max_threads;
            }

            public bool HasTest()
            {
                return _cur_less_threads < _max_threads;
            }

            public void SetNextSpeed(double speed)
            {
                if (HasTest())
                {
                    _benchmarks.Add(new benchmark(_cur_less_threads, speed));
                    ++_cur_less_threads;
                }
            }

            public void FindFastest()
            {
                _benchmarks.Sort((a, b) => -a.Benchmark.CompareTo(b.Benchmark));
            }
            public double GetBestSpeed()
            {
                return _benchmarks[0].Benchmark;
            }
            public int GetLessThreads()
            {
                return _benchmarks[0].LessTreads;
            }

            private readonly int _max_threads;
            private int _cur_less_threads = 0;
            private List<benchmark> _benchmarks = new List<benchmark>();
            public int LessTreads { get { return _cur_less_threads; } }
            public int Time;
        }
        private CPUBenchmarkStatus __CPUBenchmarkStatus = null;

        private class ClaymoreZcashStatus
        {
            private const int MAX_BENCH = 2;
            private readonly string[] ASM_MODES = new string[] { " -asm 1", " -asm 0" };

            private double[] speeds = new double[] { 0.0d, 0.0d };
            private int CurIndex = 0;
            private readonly string originalExtraParams;

            public ClaymoreZcashStatus(string oep)
            {
                originalExtraParams = oep;
            }

            public bool HasTest()
            {
                return CurIndex < MAX_BENCH;
            }

            public void SetSpeed(double speed)
            {
                if (HasTest())
                {
                    speeds[CurIndex] = speed;
                }
            }

            public void SetNext()
            {
                CurIndex += 1;
            }

            public string GetTestExtraParams()
            {
                if (HasTest())
                {
                    return originalExtraParams + ASM_MODES[CurIndex];
                }
                return originalExtraParams;
            }

            private int FastestIndex()
            {
                int maxIndex = 0;
                double maxValue = speeds[maxIndex];
                for (int i = 1; i < speeds.Length; ++i)
                {
                    if (speeds[i] > maxValue)
                    {
                        maxIndex = i;
                        maxValue = speeds[i];
                    }
                }

                return 0;
            }

            public string GetFastestExtraParams()
            {
                return originalExtraParams + ASM_MODES[FastestIndex()];
            }
            public double GetFastestTime()
            {
                return speeds[FastestIndex()];
            }

            public int Time = 180;
        }
        private ClaymoreZcashStatus __ClaymoreZcashStatus = null;

        // CPU sweet spots
        private List<AlgorithmType> CPUAlgos = new List<AlgorithmType>() {
            AlgorithmType.CryptoNight
        };

        //private static Color DISABLED_COLOR = Color.DarkGray;
        //private static Color BENCHMARKED_COLOR = Color.LightGreen;
        //private static Color UNBENCHMARKED_COLOR = Color.LightBlue;
        //public void LviSetColor(ListViewItem lvi)
        //{
        //    var CDevice = lvi.Tag as ComputeDevice;
        //    if (CDevice != null && _benchmarkDevicesAlgorithmStatus != null)
        //    {
        //        var uuid = CDevice.UUID;
        //        if (!CDevice.Enabled)
        //        {
        //            lvi.BackColor = DISABLED_COLOR;
        //        }
        //        else
        //        {
        //            switch (_benchmarkDevicesAlgorithmStatus[uuid])
        //            {
        //                case BenchmarkSettingsStatus.TODO:
        //                case BenchmarkSettingsStatus.DISABLED_TODO:
        //                    lvi.BackColor = UNBENCHMARKED_COLOR;
        //                    break;
        //                case BenchmarkSettingsStatus.NONE:
        //                case BenchmarkSettingsStatus.DISABLED_NONE:
        //                    lvi.BackColor = BENCHMARKED_COLOR;
        //                    break;
        //            }
        //        }
        //        //// enable disable status, NOT needed
        //        //if (cdvo.IsEnabled && _benchmarkDevicesAlgorithmStatus[uuid] >= BenchmarkSettingsStatus.DISABLED_NONE) {
        //        //    _benchmarkDevicesAlgorithmStatus[uuid] -= 2;
        //        //} else if (!cdvo.IsEnabled && _benchmarkDevicesAlgorithmStatus[uuid] <= BenchmarkSettingsStatus.TODO) {
        //        //    _benchmarkDevicesAlgorithmStatus[uuid] += 2;
        //        //}
        //    }
        //}

        public BenchmarkHelp(BenchmarkPerformanceType benchmarkPerformanceType = BenchmarkPerformanceType.Standard,
            bool autostart = false)
        {
            StartMining = false;
            _tcs = new TaskCompletionSource<bool>();
            // clear prev pending statuses
            foreach (var dev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
            {
                foreach (var algo in dev.GetAlgorithmSettings())
                {
                    algo.ClearBenchmarkPendingFirst();
                }
            }


            // use this to track miner benchmark statuses
            _benchmarkMiners = new List<Miner>();
            // current failed new list
            _benchmarkFailedAlgoPerDev = new List<DeviceAlgo>();
            // disable gui controls
            // set benchmark pending status
            //StartBenchmark();
        }

        //private void StartStopBtn_Click(object sender, EventArgs e)
        //{
        //    if (_inBenchmark)
        //    {
        //        StopButonClick();
        //    }
        //    else if (StartButonClick())
        //    {
        //    }
        //}

        //// TODO add list for safety and kill all miners
        //private void StopButonClick()
        //{
        //    _benchmarkingTimer.Stop();
        //    _inBenchmark = false;
        //    Helpers.ConsolePrint("FormBenchmark", "StopButonClick() benchmark routine stopped");
        //    //// copy benchmarked
        //    //CopyBenchmarks();
        //    if (_currentMiner != null)
        //    {
        //        _currentMiner.BenchmarkSignalQuit = true;
        //        _currentMiner.InvokeBenchmarkSignalQuit();
        //    }
        //}

        //private bool StartButonClick()
        //{
        //    CalcBenchmarkDevicesAlgorithmQueue();
        //    // device selection check scope
        //    {
        //        bool noneSelected = true;
        //        foreach (var cDev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
        //        {
        //            if (cDev.Enabled)
        //            {
        //                noneSelected = false;
        //                break;
        //            }
        //        }
        //        if (noneSelected)
        //        {
        //            MessageBox.Show(International.GetText("FormBenchmark_No_Devices_Selected_Msg"),
        //                International.GetText("FormBenchmark_No_Devices_Selected_Title"),
        //                MessageBoxButtons.OK);
        //            return false;
        //        }
        //    }
        //    // device todo benchmark check scope
        //    {
        //        bool nothingToBench = true;
        //        foreach (var statusKpv in _benchmarkDevicesAlgorithmStatus)
        //        {
        //            if (statusKpv.Value == BenchmarkSettingsStatus.TODO)
        //            {
        //                nothingToBench = false;
        //                break;
        //            }
        //        }
        //        if (nothingToBench)
        //        {
        //            MessageBox.Show(International.GetText("FormBenchmark_Nothing_to_Benchmark_Msg"),
        //                International.GetText("FormBenchmark_Nothing_to_Benchmark_Title"),
        //                MessageBoxButtons.OK);
        //            return false;
        //        }
        //    }

        //    // current failed new list
        //    _benchmarkFailedAlgoPerDev = new List<DeviceAlgo>();
        //    // disable gui controls
        //    // set benchmark pending status
        //    foreach (var deviceAlgosTuple in _benchmarkDevicesAlgorithmQueue)
        //    {
        //        foreach (var algo in deviceAlgosTuple.Item2)
        //        {
        //            algo.SetBenchmarkPending();
        //        }
        //    }
        //    StartBenchmark();

        //    return true;
        //}

        public void CalcBenchmarkDevicesAlgorithmQueue()
        {

            _benchmarkAlgorithmsCount = 0;
            _benchmarkDevicesAlgorithmStatus = new Dictionary<string, BenchmarkSettingsStatus>();
            _benchmarkDevicesAlgorithmQueue = new List<Tuple<ComputeDevice, Queue<Algorithm>>>();
            foreach (var cDev in ComputeDeviceManager.Avaliable.AllAvaliableDevices)
            {
                var algorithmQueue = new Queue<Algorithm>();
                foreach (var algo in cDev.GetAlgorithmSettings())
                {
                    if (ShoulBenchmark(algo))
                    {
                        algorithmQueue.Enqueue(algo);
                        algo.SetBenchmarkPendingNoMsg();
                    }
                    else
                    {
                        algo.ClearBenchmarkPending();
                    }
                }


                BenchmarkSettingsStatus status;
                if (cDev.Enabled)
                {
                    _benchmarkAlgorithmsCount += algorithmQueue.Count;
                    status = algorithmQueue.Count == 0 ? BenchmarkSettingsStatus.NONE : BenchmarkSettingsStatus.TODO;
                    _benchmarkDevicesAlgorithmQueue.Add(
                    new Tuple<ComputeDevice, Queue<Algorithm>>(cDev, algorithmQueue)
                    );
                }
                else
                {
                    status = algorithmQueue.Count == 0 ? BenchmarkSettingsStatus.DISABLED_NONE : BenchmarkSettingsStatus.DISABLED_TODO;
                }
                _benchmarkDevicesAlgorithmStatus[cDev.UUID] = status;
            }
        }

        private bool ShoulBenchmark(Algorithm algorithm)
        {
            bool isBenchmarked = algorithm.BenchmarkSpeed > 0 ? true : false;
            if (_algorithmOption == AlgorithmBenchmarkSettingsType.SelectedUnbenchmarkedAlgorithms
                && !isBenchmarked && algorithm.Enabled)
            {
                return true;
            }
            if (_algorithmOption == AlgorithmBenchmarkSettingsType.UnbenchmarkedAlgorithms && !isBenchmarked)
            {
                return true;
            }
            if (_algorithmOption == AlgorithmBenchmarkSettingsType.ReBecnhSelectedAlgorithms && algorithm.Enabled)
            {
                return true;
            }
            if (_algorithmOption == AlgorithmBenchmarkSettingsType.AllAlgorithms)
            {
                return true;
            }

            return false;
        }

        public bool NeedBenchmark()
        {
            CalcBenchmarkDevicesAlgorithmQueue();
            {
                bool nothingToBench = true;
                foreach (var statusKpv in _benchmarkDevicesAlgorithmStatus)
                {
                    if (statusKpv.Value == BenchmarkSettingsStatus.TODO)
                    {
                        nothingToBench = false;
                        break;
                    }
                }
                if (nothingToBench)
                {
                    //MessageBox.Show(International.GetText("FormBenchmark_Nothing_to_Benchmark_Msg"),
                    //    International.GetText("FormBenchmark_Nothing_to_Benchmark_Title"),
                    //    MessageBoxButtons.OK);
                    return false;
                }
            }
            foreach (var deviceAlgosTuple in _benchmarkDevicesAlgorithmQueue)
            {
                foreach (var algo in deviceAlgosTuple.Item2)
                {
                    algo.SetBenchmarkPending();
                }
            }
            return true;
        }

        public Task StartBenchmark()
        {
            _inBenchmark = true;
            _bechmarkCurrentIndex = -1;
            NextBenchmark();
            return _tcs.Task;
        }

        void NextBenchmark()
        {
            ++_bechmarkCurrentIndex;
            if (_bechmarkCurrentIndex >= _benchmarkAlgorithmsCount)
            {
                EndBenchmark();
                return;
            }

            Tuple<ComputeDevice, Queue<Algorithm>> currentDeviceAlgosTuple;
            Queue<Algorithm> algorithmBenchmarkQueue;
            while (_benchmarkDevicesAlgorithmQueue.Count > 0)
            {
                currentDeviceAlgosTuple = _benchmarkDevicesAlgorithmQueue[0];
                _currentDevice = currentDeviceAlgosTuple.Item1;
                algorithmBenchmarkQueue = currentDeviceAlgosTuple.Item2;
                if (algorithmBenchmarkQueue.Count != 0)
                {
                    _currentAlgorithm = algorithmBenchmarkQueue.Dequeue();
                    break;
                }
                else
                {
                    _benchmarkDevicesAlgorithmQueue.RemoveAt(0);
                }
            }

            if (_currentDevice != null && _currentAlgorithm != null)
            {
                _currentMiner = MinerFactory.CreateMiner(_currentDevice, _currentAlgorithm);
                if (_currentAlgorithm.MinerBaseType == MinerBaseType.XmrStackCPU && _currentAlgorithm.NiceHashID == AlgorithmType.CryptoNight && string.IsNullOrEmpty(_currentAlgorithm.ExtraLaunchParameters) && _currentAlgorithm.ExtraLaunchParameters.Contains("enable_ht=true") == false)
                {
                    __CPUBenchmarkStatus = new CPUBenchmarkStatus(Globals.ThreadsPerCPU);
                    _currentAlgorithm.LessThreads = __CPUBenchmarkStatus.LessTreads;
                }
                else
                {
                    __CPUBenchmarkStatus = null;
                }
                if (_currentAlgorithm.MinerBaseType == MinerBaseType.ClaymoreAMD && _currentAlgorithm.NiceHashID == AlgorithmType.Equihash && _currentAlgorithm.ExtraLaunchParameters != null && !_currentAlgorithm.ExtraLaunchParameters.Contains("-asm"))
                {
                    __ClaymoreZcashStatus = new ClaymoreZcashStatus(_currentAlgorithm.ExtraLaunchParameters);
                    _currentAlgorithm.ExtraLaunchParameters = __ClaymoreZcashStatus.GetTestExtraParams();
                }
                else
                {
                    __ClaymoreZcashStatus = null;
                }
            }

            if (_currentMiner != null && _currentAlgorithm != null)
            {
                _benchmarkMiners.Add(_currentMiner);
                CurrentAlgoName = AlgorithmNiceHashNames.GetName(_currentAlgorithm.NiceHashID);
                _currentMiner.InitBenchmarkSetup(new MiningPair(_currentDevice, _currentAlgorithm));

                var time = ConfigManager.GeneralConfig.BenchmarkTimeLimits
                    .GetBenchamrktime(BenchmarkPerformanceType.Quick, _currentDevice.DeviceGroupType);
                //currentConfig.TimeLimit = time;
                if (__CPUBenchmarkStatus != null)
                {
                    __CPUBenchmarkStatus.Time = time;
                }
                if (__ClaymoreZcashStatus != null)
                {
                    __ClaymoreZcashStatus.Time = time;
                }

                // dagger about 4 minutes
                var showWaitTime = _currentAlgorithm.NiceHashID == AlgorithmType.DaggerHashimoto ? 4 * 60 : time;

               // dotCount = 0;
                //_benchmarkingTimer.Start();

                _currentMiner.BenchmarkStart(time, this);
            }
            else
            {
                NextBenchmark();
            }
        }

        void EndBenchmark()
        {
            //_benchmarkingTimer.Stop();
            _inBenchmark = false;
            Helpers.ConsolePrint("FormBenchmark", "EndBenchmark() benchmark routine finished");

            //CopyBenchmarks();

            // check if all ok
            if (_benchmarkFailedAlgoPerDev.Count == 0 && StartMining == false)
            {
                //MessageBox.Show(
                //    International.GetText("FormBenchmark_Benchmark_Finish_Succes_MsgBox_Msg"),
                //    International.GetText("FormBenchmark_Benchmark_Finish_MsgBox_Title"),
                //    MessageBoxButtons.OK);
            }
            else if (StartMining == false)
            {
                //var result = MessageBox.Show(
                //    International.GetText("FormBenchmark_Benchmark_Finish_Fail_MsgBox_Msg"),
                //    International.GetText("FormBenchmark_Benchmark_Finish_MsgBox_Title"),
                //    MessageBoxButtons.RetryCancel);

                //if (result == System.Windows.Forms.DialogResult.Retry)
                //{
                //    StartButonClick();
                //    return;
                //}
                //else /*Cancel*/
                //{
                    // get unbenchmarked from criteria and disable
                    CalcBenchmarkDevicesAlgorithmQueue();
                    foreach (var deviceAlgoQueue in _benchmarkDevicesAlgorithmQueue)
                    {
                        foreach (var algorithm in deviceAlgoQueue.Item2)
                        {
                            algorithm.Enabled = false;
                        }
                    }
                //}
            }
            _tcs.SetResult(true);
        }

        public async Task OnBenchmarkComplete(bool success, string status)
        {
            if (!_inBenchmark) return;
            await Application.Current.Dispatcher.BeginInvoke(new Action(()=>
            {
                _bechmarkedSuccessCount += success ? 1 : 0;
                bool rebenchSame = false;
                if (success && __CPUBenchmarkStatus != null && CPUAlgos.Contains(_currentAlgorithm.NiceHashID) && _currentAlgorithm.MinerBaseType == MinerBaseType.XmrStackCPU)
                {
                    __CPUBenchmarkStatus.SetNextSpeed(_currentAlgorithm.BenchmarkSpeed);
                    rebenchSame = __CPUBenchmarkStatus.HasTest();
                    _currentAlgorithm.LessThreads = __CPUBenchmarkStatus.LessTreads;
                    if (rebenchSame == false)
                    {
                        __CPUBenchmarkStatus.FindFastest();
                        _currentAlgorithm.BenchmarkSpeed = __CPUBenchmarkStatus.GetBestSpeed();
                        _currentAlgorithm.LessThreads = __CPUBenchmarkStatus.GetLessThreads();
                    }
                }

                if (__ClaymoreZcashStatus != null && _currentAlgorithm.MinerBaseType == MinerBaseType.ClaymoreAMD && _currentAlgorithm.NiceHashID == AlgorithmType.Equihash)
                {
                    if (__ClaymoreZcashStatus.HasTest())
                    {
                        _currentMiner = MinerFactory.CreateMiner(_currentDevice, _currentAlgorithm);
                        rebenchSame = true;
                        //System.Threading.Thread.Sleep(1000*60*5);
                        __ClaymoreZcashStatus.SetSpeed(_currentAlgorithm.BenchmarkSpeed);
                        __ClaymoreZcashStatus.SetNext();
                        _currentAlgorithm.ExtraLaunchParameters = __ClaymoreZcashStatus.GetTestExtraParams();
                        Helpers.ConsolePrint("ClaymoreAMD_Equihash", _currentAlgorithm.ExtraLaunchParameters);
                        _currentMiner.InitBenchmarkSetup(new MiningPair(_currentDevice, _currentAlgorithm));
                    }

                    if (__ClaymoreZcashStatus.HasTest() == false)
                    {
                        rebenchSame = false;
                        // set fastest mode
                        _currentAlgorithm.BenchmarkSpeed = __ClaymoreZcashStatus.GetFastestTime();
                        _currentAlgorithm.ExtraLaunchParameters = __ClaymoreZcashStatus.GetFastestExtraParams();
                    }
                }

                //if (!rebenchSame)
                //{
                //    _benchmarkingTimer.Stop();
                //}

                if (!success && !rebenchSame)
                {
                    // add new failed list
                    _benchmarkFailedAlgoPerDev.Add(
                    new DeviceAlgo()
                    {
                        Device = _currentDevice.Name,
                        Algorithm = _currentAlgorithm.AlgorithmName
                    });
                }
                else if (!rebenchSame)
                {
                    // set status to empty string it will return speed
                    _currentAlgorithm.ClearBenchmarkPending();
                }
                if (rebenchSame)
                {
                    if (__CPUBenchmarkStatus != null)
                    {
                        _currentMiner.BenchmarkStart(__CPUBenchmarkStatus.Time, this);
                    }
                    else if (__ClaymoreZcashStatus != null)
                    {
                        _currentMiner.BenchmarkStart(__ClaymoreZcashStatus.Time, this);
                    }
                }
                else
                {
                    NextBenchmark();
                }
            }));
        }

        public void SetCurrentStatus(string status)
        {
            //this.Invoke((MethodInvoker)delegate
            //{
            //    algorithmsListView1.SetSpeedStatus(_currentDevice, _currentAlgorithm, getDotsWaitString());
            //}
        }
    }
}
