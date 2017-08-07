using NiceHashMiner.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

namespace NiceHashMiner.Utils {
    public class MinersDownloader {
        private const string TAG = "MinersDownloader";

        DownloadSetup _downloadSetup;

        private WebClient _webClient;
        private Stopwatch _stopwatch;
        Thread _UnzipThread = null;
        private TaskCompletionSource<bool> _tcs; 
        bool isDownloadSizeInit = false;
        private IDialogCoordinator _dialogCoordinator;

        IMinerUpdateIndicator _minerUpdateIndicator;

        public MinersDownloader(DownloadSetup downloadSetup) {
            _downloadSetup = downloadSetup;
        }

        public async Task Start(IMinerUpdateIndicator minerUpdateIndicator, IDialogCoordinator dialogCoordinator) {
            _minerUpdateIndicator = minerUpdateIndicator;
            _tcs = new TaskCompletionSource<bool>();
            _dialogCoordinator = dialogCoordinator;
            //if something not right delete previous and download new
            try
            {
                if (File.Exists(_downloadSetup.BinsZipLocation))
                {
                    File.Delete(_downloadSetup.BinsZipLocation);
                }
                if (Directory.Exists(_downloadSetup.ZipedFolderName))
                {
                    Directory.Delete(_downloadSetup.ZipedFolderName, true);
                }
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("MinersDownloadManager", ex.Message);
            }
            await Download();
        }

        // #2 download the file
        private async Task Download() {
            _minerUpdateIndicator.SetTitle(International.GetText("MinersDownloadManager_Title_Downloading"));
            _stopwatch = new Stopwatch();
            using (_webClient = new WebClient()) {
                _webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                _webClient.DownloadFileCompleted += async (sender, e) =>  await DownloadCompleted(sender, e); //new AsyncCompletedEventHandler

                Uri downloadURL = new Uri(_downloadSetup.BinsDownloadURL);

                _stopwatch.Start();
                try
                {
                    await _webClient.DownloadFileTaskAsync(downloadURL, _downloadSetup.BinsZipLocation);
                } catch (Exception ex) {
                    Helpers.ConsolePrint("MinersDownloadManager", ex.Message);
                }
            }
        }

        #region Download delegates

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e) {
            if (!isDownloadSizeInit) {
                isDownloadSizeInit = true;
                //_minerUpdateIndicator.SetMaxProgressValue((int)(e.TotalBytesToReceive / 1024));
            }

            // Calculate download speed and output it to labelSpeed.
            var speedString = string.Format("{0} kb/s", (e.BytesReceived / 1024d / _stopwatch.Elapsed.TotalSeconds).ToString("0.00"));

            // Show the percentage on our label.
            var percString = e.ProgressPercentage.ToString() + "%";

            // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
            var labelDownloaded = string.Format("{0} MB / {1} MB",
                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));

            _minerUpdateIndicator.SetMsg(
                //(int)(e.BytesReceived / 1024d),
                String.Format("{0}   {1}   {2}", speedString, percString,labelDownloaded));

        }

        // The event that will trigger when the WebClient is completed
        private async Task DownloadCompleted(object sender, AsyncCompletedEventArgs e) {
            _stopwatch.Stop();
            _stopwatch = null;

            if (e.Cancelled == true) {
                // TODO handle Cancelled
                Helpers.ConsolePrint(TAG, "DownloadCompleted Cancelled");
            } else {
                // TODO handle Success
                Helpers.ConsolePrint(TAG, "DownloadCompleted Success");
                // wait one second for binary to exist
                System.Threading.Thread.Sleep(1000);
                // extra check dirty
                int try_count = 50;
                while (!File.Exists(_downloadSetup.BinsZipLocation) && try_count > 0) { --try_count; }
                try
                {
                    await UnzipStart();
                }
                catch
                {
                }
            }
        }

        #endregion Download delegates


        public Task UnzipStart()
        {
            _minerUpdateIndicator.SetTitle(International.GetText("MinersDownloadManager_Title_Settup"));
            UnzipThreadRoutine();
            return _tcs.Task;
        }

        private void UnzipThreadRoutine() {
            try {
                if (File.Exists(_downloadSetup.BinsZipLocation))
                {
                    _minerUpdateIndicator.SetMsg(International.GetText("MinersDownloadManager_Title_Settup_Unzipping"));

                    Helpers.ConsolePrint(TAG, _downloadSetup.BinsZipLocation + " already downloaded");
                    Helpers.ConsolePrint(TAG, "unzipping");

                    // if using other formats as zip are returning 0
                    FileInfo fileArchive = new FileInfo(_downloadSetup.BinsZipLocation);
                    FileStream archFile = new FileStream(_downloadSetup.BinsZipLocation, FileMode.Open, FileAccess.Read);
                    var archive = new ZipArchive(archFile);// _downloadSetup.BinsZipLocation);
                    //_minerUpdateIndicator.SetMaxProgressValue(100);
                    long SizeCount = 0;

                    archive.ExtractToDirectory(fileArchive.DirectoryName);
                    //foreach (var entry in archive.Entries)
                    //{
                    //    //if (!entryIsDirectory)
                    //    {
                    //        SizeCount += entry.CompressedLength;
                    //        Helpers.ConsolePrint(TAG, entry.Name);
                    //        ////entry.WriteToDirectory("", ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    //        //entry.ExtractToFile("", true);
                    //        //archive.ExtractToDirectory(Application.ResourceAssembly.Location);
                    //        double prog = ((double)(SizeCount) / (double)(fileArchive.Length) * 100);
                    //        _minerUpdateIndicator.SetMsg(/*(int)prog, */String.Format(International.GetText("MinersDownloadManager_Title_Settup_Unzipping"), prog.ToString("F2")));
                    //    }
                    //}
                    archive.Dispose();
                    // after unzip stuff
                    // remove bins zip
                    try
                    {
                        if (File.Exists(_downloadSetup.BinsZipLocation))
                        {
                            File.Delete(_downloadSetup.BinsZipLocation);
                        }
                    }
                    catch (Exception e)
                    {
                        Helpers.ConsolePrint("MinersDownloader.UnzipThreadRoutine", "Cannot delete exception: " + e.Message);
                        _tcs.SetException(e);
                    }
                }
                else
                {
                    Helpers.ConsolePrint(TAG, String.Format("UnzipThreadRoutine {0} file not found", _downloadSetup.BinsZipLocation));
                }
                _tcs.SetResult(true);
            }
            catch (Exception e) {
                Helpers.ConsolePrint(TAG, "UnzipThreadRoutine has encountered an error: " + e.Message);
                _tcs.SetException(e);
            }
        }
    }
}
