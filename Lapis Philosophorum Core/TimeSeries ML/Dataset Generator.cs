using Coinbase.Pro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bifrost.PhilosophersStone
{
    public class DatasetGenerator
    {
        public static CoinbaseProClient AICoinBaseClient { get; private set; }
        public static async Task<bool> BuildDailyDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();

            //Manually typed dates to prevent Coinbase API errors 
            DateTime past = new DateTime(2017, 01, 01);
            DateTime past1 = new DateTime(2017, 06, 01);
            DateTime past2 = new DateTime(2018, 01, 01);
            DateTime past3 = new DateTime(2018, 06, 01);
            DateTime past4 = new DateTime(2019, 01, 01);
            DateTime past5 = new DateTime(2019, 06, 01);
            DateTime past6 = new DateTime(2020, 01, 01);
            DateTime past7 = new DateTime(2020, 06, 01);
            DateTime past8 = new DateTime(2021, 01, 01);
            DateTime past9 = new DateTime(2021, 06, 01);
            DateTime past10 = new DateTime(2022, 01, 01);
            DateTime past11 = new DateTime(2022, 06, 01);
            DateTime past12 = new DateTime(2023, 01, 01);
            DateTime past13 = new DateTime(2023, 06, 01);
            DateTime now = DateTime.Today;
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                //API rate-limited -- 1 sec delay
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past, past1, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past1, past2, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past2, past3, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past3, past4, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past4, past5, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past5, past6, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past6, past7, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past7, past8, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past8, past9, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past9, past10, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past10, past11, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past11, past12, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past12, past13, 86400));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past13, now, 86400));

                try { File.Delete(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", "time,high,low,open,close,volume" + Environment.NewLine);

                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.LocalDateTime + ", " + data.High + ", " + data.Low + ", " + data.Open + ", " + data.Close + ", " + data.Volume;
                        File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                Console.WriteLine("DataSet Complete");
                successful = true;
            }
            catch (Exception Error)
            {

                Console.WriteLine(Error);
                successful = false;

            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task<bool> Build6HourlyDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
            DateTime beginTime = DateTime.Now.Subtract(TimeSpan.FromDays(150));
            DateTime endTime = beginTime.AddDays(30);
            int iteration = 1;
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                while (iteration != 6)
                {
                    //API rate-limited -- 1 sec delay
                    dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 21600));
                    await Task.Delay(1010);
                    if (iteration != 5)
                    {
                        beginTime = endTime;
                        endTime = beginTime.AddDays(30);
                    }
                    else
                    {
                        beginTime = endTime;
                        endTime = DateTime.Now;
                        dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 21600));
                        break;
                    }
                    iteration++;
                }
                try { File.Delete(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", "time,high,low,open,close,volume" + Environment.NewLine);
                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.Date.Date.ToShortDateString() + ", " + data.High + ", " + data.Low + ", " + data.Open + ", " + data.Close + ", " + data.Volume;
                        File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                successful = true;
                Console.WriteLine("DataSet Complete");
            }
            catch (Exception Error)
            {
                successful = false;
                Console.WriteLine(Error);
            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task<bool> BuildHourlyDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
            DateTime beginTime = DateTime.Now.Subtract(TimeSpan.FromDays(84));
            DateTime endTime = beginTime.AddDays(7);
            int iteration = 1;
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                while (iteration != 13)
                {
                    //API rate-limited -- 1 sec delay
                    dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 3600));
                    await Task.Delay(1010);
                    if (iteration != 12)
                    {
                        beginTime = endTime;
                        endTime = beginTime.AddDays(7);
                    }
                    else
                    {
                        beginTime = endTime;
                        endTime = DateTime.Now;
                        dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 3600));
                        break;
                    }
                    iteration++;
                }
                try { File.Delete(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", "date,high,low,open,close,volume" + Environment.NewLine);
                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.LocalDateTime + ", " + Math.Round((decimal)data.High) + ", " + Math.Round((decimal)data.Low) + ", " + Math.Round((decimal)data.Open) + ", " + Math.Round((decimal)data.Close) + ", " + Math.Round((decimal)data.Volume);
                        File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                successful = true;
                Console.WriteLine("DataSet Complete");
            }
            catch (Exception Error)
            {
                successful = false;
                Console.WriteLine(Error);
            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task<bool> Build15minDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
            DateTime beginTime = DateTime.Now.Subtract(TimeSpan.FromDays(14));
            DateTime endTime = beginTime.AddDays(1);
            int iteration = 1;
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                //API rate-limited -- 1 sec delay
                while (iteration != 15)
                {
                    //API rate-limited -- 1 sec delay
                    dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 900));
                    await Task.Delay(1010);
                    if (iteration != 14)
                    {
                        beginTime = endTime;
                        endTime = beginTime.AddDays(1);
                    }
                    else
                    {
                        beginTime = endTime;
                        endTime = DateTime.Now;
                        dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 900));
                        break;
                    }
                    iteration++;
                }
                try { File.Delete(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", "date,high,low,open,close,volume" + Environment.NewLine);
                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.ToUnixTimeSeconds() + ", " + Math.Round((decimal)data.High) + ", " + Math.Round((decimal)data.Low) + ", " + Math.Round((decimal)data.Open) + ", " + Math.Round((decimal)data.Close) + ", " + Math.Round((decimal)data.Volume);
                        File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                successful = true;
                Console.WriteLine("DataSet Complete");
            }
            catch (Exception Error)
            {
                successful = false;
                Console.WriteLine(Error);
            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task<bool> Build5minDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
            DateTime beginTime = DateTime.Now.Subtract(TimeSpan.FromDays(14));
            DateTime endTime = beginTime.AddDays(1);
            int iteration = 1;
           
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                while(iteration != 15) 
                {
                    //API rate-limited -- 1 sec delay
                    dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 300));
                    await Task.Delay(1010);
                    if (iteration != 14)
                    {
                        beginTime = endTime;
                        endTime = beginTime.AddDays(1);
                    }
                    else 
                    {
                        beginTime = endTime;
                        endTime = DateTime.Now;
                        dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, beginTime, endTime, 300));
                        break;
                    }
                    iteration++;
                }

                await Task.Delay(50);
                try { File.Delete(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", "date,high,low,open,close,volume" + Environment.NewLine);
                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.ToUnixTimeSeconds() + ", " + Math.Round((decimal)data.High) + ", " + Math.Round((decimal)data.Low) + ", " + Math.Round((decimal)data.Open) + ", " + Math.Round((decimal)data.Close) + ", " + Math.Round((decimal)data.Volume);
                        File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                successful = true;
                Console.WriteLine("DataSet Complete");
            }
            catch (Exception Error)
            {
                successful = false;
                Console.WriteLine(Error);
            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task CleanDataset(string currency, int horizon)
        {
            try
            {
                var dataset = File.ReadAllLines(lapis_philosophorum.DATA_FILEPATH + currency + horizon + ".csv");
                string[] newdataset = new string[dataset.Length];
                int count = 0;
                foreach (var entry in dataset)
                {

                    if (!newdataset.Contains(entry))
                    {
                        newdataset[count] = entry;
                    }
                    count++;
                }
                File.Delete(lapis_philosophorum.DATA_FILEPATH + currency + horizon + ".csv");
                File.AppendAllLines(lapis_philosophorum.DATA_FILEPATH + currency + horizon + ".csv", newdataset);
                Console.WriteLine("Cleanse Finished!");
            }
            catch (Exception fg)
            {
                Console.WriteLine(fg);
            }
            await Task.CompletedTask;
        }
    }
}
