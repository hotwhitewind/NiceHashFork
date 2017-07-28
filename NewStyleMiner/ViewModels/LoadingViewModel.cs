using NiceHashMiner.Interfaces;
using NiceHashMiner.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NewStyleMiner.ViewModels
{ 
    public class LoadingViewModel : ViewModelBase, IMinerUpdateIndicator
    {
        public interface IAfterInitializationCaller
        {
            void AfterLoadComplete();
        }

        private readonly IAfterInitializationCaller AfterInitCaller;
        public delegate void _OnShow();
        public event _OnShow OnShow;

        public LoadingViewModel(IAfterInitializationCaller initcaller)
        {
            AfterInitCaller = initcaller;
            _minerDownloader = null;
            _isMinerDownload = false;
            WindowLoaded = new DelegateCommand<object>((args) =>
            {
                OnShow();
            });
        }

        private bool _closeTrigger;
        public bool CloseTrigger
        {
            get { return this._closeTrigger; }
            set { this._closeTrigger = value; OnPropertyChanged("CloseTrigger"); }
        }

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChange = delegate { };
        protected static void OnStaticPropertyChanged(string propertyName)
        {
            StaticPropertyChange?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        private string _loadingLabelText = "Loading...";

        public  string LoadingLabelText
        {
            get { return _loadingLabelText; }
            set
            {
                _loadingLabelText = value;
                OnPropertyChanged("LoadingLabelText");
            }
        }

        private  string _loadingLabelTitle = "";

        public  string LoadingLabelTitle
        {
            get { return _loadingLabelTitle; }
            set
            {
                _loadingLabelTitle = value;
                OnPropertyChanged("LoadingLabelTitle");
            }
        }

        public void FinishLoading()
        {
            CloseTrigger = true;
            AfterInitCaller.AfterLoadComplete();
        }

        public void SetMsg(string msg)
        {
             LoadingLabelText = msg;
        }

        public void SetTitle(string title)
        {
            LoadingLabelTitle = title;
        }

        public void FinishMsg(bool success)
        {
            if (success)
            {
                LoadingLabelText = "Init Finished!";
            }
            else
            {
                LoadingLabelText = "Init Failed!";
            }
            //System.Threading.Thread.Sleep(1000);
            //CloseTrigger = true;
        }

        private bool _isMinerDownload;
        public bool IsMinerDownload
        {
            get
            {
                return _isMinerDownload;
            }
            set
            {
                _isMinerDownload = value;
            }
        }

        private MinersDownloader _minerDownloader;
        public MinersDownloader MinerDownloader
        {
            get
            {
                return _minerDownloader;
            }
            set
            {
                _minerDownloader = value;
            }
        }

        public async Task StartMinerDownloads()
        {
            if (_isMinerDownload && _minerDownloader != null)
            {
                await _minerDownloader.Start(this);
            }
        }

        public ICommand WindowLoaded { get; private set; }
    }
}
