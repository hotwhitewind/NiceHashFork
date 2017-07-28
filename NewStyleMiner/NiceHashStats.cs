using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using NiceHashMiner.Enums;
using NiceHashMiner.Miners;
using Newtonsoft.Json.Linq;



namespace NiceHashMiner
{
#pragma warning disable 649
    public class NiceHashSMA
    {
        public int port;
        public string name;
        public int algo;
        public double paying;
    }
#pragma warning restore 649

    class NiceHashStats
    {
#pragma warning disable 649
        //class nicehash_global_stats
        //{
        //    public double profitability_above_ltc;
        //    public double price;
        //    public double profitability_ltc;
        //    public int algo;
        //    public double speed;
        //}

        public static double RUB_COIN = -1;
        public class nicehash_stats
        {
            public double balance;
            public double balance_unexchanged;
            public double balance_immature;
            public double balance_confirmed;
            public double accepted_speed;
            public double rejected_speed;
            public int algo;
        }

        public class mine_result
        {
            public string warn;
            public bool error;
            public string link;
            public string wallet;
        }

        public class rate_result
        {
            public string warn;
            public bool error;
        }

        public class cursrub_result
        {
            public double rub;
            public bool error;
        }

        public class CurrencyBalance
        {
            public string name;
            public double value;
            public string symbol;
        }

        public class mine_balance
        {
            public bool access;
            public List<CurrencyBalance> currency;
            public bool error;
            public string warn;
        }

        public class mine_access
        {
            public bool access;
            public string wallet;
        }

        //public class 
        public class nicehash_result_2
        {
            public NiceHashSMA[] simplemultialgo;
        }

        public class nicehash_json_2
        {
            public nicehash_result_2 result;
            public string method;
        }

        class nicehash_result<T>
        {
            public T[] stats;
        }
        
        class nicehash_json<T>
        {
            public nicehash_result<T> result;
            public string method;
        }

        class nicehash_json_T<T> {
            public T result;
            public string method;
        }

        class nicehash_error
        {
            public string error;
            public string method;
        }

        public class nicehashminer_version
        {
            public string version;
            public string download_link;
        }
#pragma warning restore 649


        public static async Task<Dictionary<AlgorithmType, NiceHashSMA>> GetAlgorithmRates(string worker)
        {
            string r1 = await GetNiceHashAPIData(Links.NHM_API_info, worker);
            if (r1 == null) return null;

            nicehash_json_2 nhjson_current;
            try
            {
                nhjson_current = await JsonConvert.DeserializeObjectAsync<nicehash_json_2>(r1, Globals.JsonSettings);
                Dictionary<AlgorithmType, NiceHashSMA> ret = new Dictionary<AlgorithmType, NiceHashSMA>();
                NiceHashSMA[] temp = nhjson_current.result.simplemultialgo;
                if (temp != null) {
                    foreach (var sma in temp) {
                        ret.Add((AlgorithmType)sma.algo, sma);
                    }
                    return ret;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<nicehash_stats> GetStats(string btc, int algo, string worker)
        {
            string r1 = await GetNiceHashAPIData(Links.NHM_API_stats + btc, worker);
            if (r1 == null) return null;

            nicehash_json<nicehash_stats> nhjson_current;
            try
            {
                nhjson_current = await JsonConvert.DeserializeObjectAsync<nicehash_json<nicehash_stats>>(r1, Globals.JsonSettings);
                for (int i = 0; i < nhjson_current.result.stats.Length; i++)
                {
                    if (nhjson_current.result.stats[i].algo == algo)
                        return nhjson_current.result.stats[i];
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<mine_balance> GetNewBalance(string ID)
        {
            double balance = 0;
            string param = String.Format("get_balance\\{0}", ID);
            string r1 = await GetNewStyleApi(Links.NewBalanceTestApi, param);
            if (r1 != null)
            {
                mine_balance nhjson_current;

                try
                {
                    nhjson_current = await JsonConvert.DeserializeObjectAsync<mine_balance>(r1, Globals.JsonSettings);
                }
                catch
                {
                    return null;
                }
                if (nhjson_current.access == true && nhjson_current.error == false)
                    return nhjson_current;
                else
                    return null;
            }
            return null;
        }

        public static async Task<mine_result> RegisterNewUser(string ID)
        {
            string param = String.Format("?register_id={0}", ID);
            string r1 = await GetNewStyleApi(Links.NewBalanceTestApi, param);
            if (r1 != null)
            {
                mine_result nhjson_current;
                try
                {

                    nhjson_current = await JsonConvert.DeserializeObjectAsync<mine_result>(r1, Globals.JsonSettings);


                    //for (int i = 0; i < nhjson_current.result.stats.Length; i++)
                    //{
                    //    if (nhjson_current.result.stats[i].algo != 999)
                    //    {
                    //        balance += nhjson_current.result.stats[i].balance;
                    //    }
                    //    else if (nhjson_current.result.stats[i].algo == 999)
                    //    {
                    //        balance += nhjson_current.result.stats[i].balance_confirmed;
                    //    }
                    //}
                        return nhjson_current;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        
        public static async Task<bool> SendRateValue(string ID, double globalRate)
        {
            string param = String.Format("?globalrate={0}&id={1}", globalRate, ID);
            string r1 = await GetNewStyleApi(Links.NewBalanceTestApi, param);
            if (r1 != null)
            {
                rate_result nhjson_current;
                try
                {
                    nhjson_current = await JsonConvert.DeserializeObjectAsync<rate_result>(r1, Globals.JsonSettings);

                    //for (int i = 0; i < nhjson_current.result.stats.Length; i++)
                    //{
                    //    if (nhjson_current.result.stats[i].algo != 999)
                    //    {
                    //        balance += nhjson_current.result.stats[i].balance;
                    //    }
                    //    else if (nhjson_current.result.stats[i].algo == 999)
                    //    {
                    //        balance += nhjson_current.result.stats[i].balance_confirmed;
                    //    }
                    //}
                }
                catch
                {
                    return false;
                }
                return nhjson_current.error;
            }


            return false;
        }

        public static async Task<bool> SendPayRequest(string ID)
        {
            int res = 0;

            string param = String.Format("pay_out\\id={0}", ID);
            string r1 = await GetNewStyleApi(Links.NewBalanceTestApi, param);
            if (r1 != null)
            {
                rate_result nhjson_current;
                try
                {
                    nhjson_current = await JsonConvert.DeserializeObjectAsync<rate_result>(r1, Globals.JsonSettings);

                    //for (int i = 0; i < nhjson_current.result.stats.Length; i++)
                    //{
                    //    if (nhjson_current.result.stats[i].algo != 999)
                    //    {
                    //        balance += nhjson_current.result.stats[i].balance;
                    //    }
                    //    else if (nhjson_current.result.stats[i].algo == 999)
                    //    {
                    //        balance += nhjson_current.result.stats[i].balance_confirmed;
                    //    }
                    //}
                }
                catch
                {
                    return false;
                }
                return nhjson_current.error;
            }

            return false;
        }

        //public static async Task<double> GetBalance(string btc, string worker)
        //{
        //    double balance = 0;

        //    string r1 = await GetNiceHashAPIData(Links.NHM_API_stats + btc, worker);
        //    if (r1 != null)
        //    {
        //        nicehash_json<nicehash_stats> nhjson_current;
        //        try
        //        {
        //            nhjson_current = JsonConvert.DeserializeObject<nicehash_json<nicehash_stats>>(r1, Globals.JsonSettings);
        //            for (int i = 0; i < nhjson_current.result.stats.Length; i++)
        //            {
        //                if (nhjson_current.result.stats[i].algo != 999)
        //                {
        //                    balance += nhjson_current.result.stats[i].balance;
        //                }
        //                else if (nhjson_current.result.stats[i].algo == 999)
        //                {
        //                    balance += nhjson_current.result.stats[i].balance_confirmed;
        //                }
        //            }
        //        }
        //        catch { }
        //    }

        //    return balance;
        //}


        public static async Task<mine_access> GetAccess(string id)
        {
            string param = String.Format("get_access\\{0}", id);
            string r1 = await GetNewStyleApi(Links.NewBalanceTestApi, param);

            if (r1 == null) return null;

            mine_access nhjson;
            try
            {
                nhjson = await JsonConvert.DeserializeObjectAsync<mine_access>(r1, Globals.JsonSettings);

                return nhjson;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<nicehashminer_version> GetNewVersion()
        {
            double balance = 0;
            string param = String.Format("version_check");
            string r1 = await GetNewStyleApi(Links.NewBalanceTestApi, param);

            if (r1 == null) return null;

            nicehashminer_version nhjson;
            try
            {
                nhjson = await JsonConvert.DeserializeObjectAsync<nicehashminer_version>(r1, Globals.JsonSettings);
                return nhjson;
            }
            catch
            {
                return null;
            }
        }

        public static async Task GetCursRub()
        {
            double balance = 0;
            string param = String.Format("cursrub_check");
            string r1 = null;
            try
            {
                r1 = await GetNewStyleApi(Links.NewBalanceTestApi, param);
            }
            catch 
            {
                
            }
            if (r1 == null) return;

            cursrub_result nhjson;
            try
            {
                nhjson = await JsonConvert.DeserializeObjectAsync<cursrub_result>(r1, Globals.JsonSettings);
                if (!nhjson.error)
                {
                    RUB_COIN = nhjson.rub;
                }
            }
            catch
            {
            }
        }

        public static double GetRUBExchangeRate()
        {
            if (RUB_COIN > 0)
            {
                return RUB_COIN;
            }
            return 0.0;
        }

        //public static async Task<string> GetVersion(string worker)
        //{
        //    string r1 = await GetNiceHashAPIData(Links.NHM_API_version, worker);
        //    if (r1 == null) return null;

        //    nicehashminer_version nhjson;
        //    try
        //    {
        //        nhjson = await JsonConvert.DeserializeObjectAsync<nicehashminer_version>(r1, Globals.JsonSettings);
        //        return nhjson.version;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}


        public static async Task<string> GetNewStyleApi(string URL, string param)
        {
            string ResponseFromServer;
            try
            {
                URL += param;
                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create(URL);

                WR.UserAgent = "NewStyleMiner/" + Application.ProductVersion;
                WR.Timeout = 30 * 1000;
                WebResponse Response = await WR.GetResponseAsync();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 20 * 1000;
                StreamReader Reader = new StreamReader(SS);
                ResponseFromServer = await Reader.ReadToEndAsync();
                if (ResponseFromServer.Length == 0 || ResponseFromServer[0] != '{')
                    throw new Exception("Not JSON!");
                Reader.Close();
                Response.Close();
            }
            catch (WebException ex)
            {
                Helpers.ConsolePrint("NICEHASH", ex.Message);
                return null;
            }

            return ResponseFromServer;
        }

        public static async Task<string> GetNiceHashAPIData(string URL, string worker)
        {
            string ResponseFromServer;
            try
            {
                string ActiveMinersGroup = MinersManager.GetActiveMinersGroup();

                HttpWebRequest WR = (HttpWebRequest)WebRequest.Create(URL);
                WR.UserAgent = "NiceHashMiner/" + Application.ProductVersion;
                if (worker.Length > 64) worker = worker.Substring(0, 64);
                WR.Headers.Add("NiceHash-Worker-ID", worker);
                WR.Headers.Add("NHM-Active-Miners-Group", ActiveMinersGroup);
                WR.Timeout = 30 * 1000;
                WebResponse Response = await WR.GetResponseAsync();
                Stream SS = Response.GetResponseStream();
                SS.ReadTimeout = 20 * 1000;
                StreamReader Reader = new StreamReader(SS);
                ResponseFromServer = await Reader.ReadToEndAsync();
                if (ResponseFromServer.Length == 0 || ResponseFromServer[0] != '{')
                    throw new Exception("Not JSON!");
                Reader.Close();
                Response.Close();
            }
            catch (Exception ex)
            {
                Helpers.ConsolePrint("NICEHASH", ex.Message);
                return null;
            }

            return ResponseFromServer;
        }
    }
}
