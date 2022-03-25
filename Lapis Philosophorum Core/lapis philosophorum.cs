using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coinbase.Pro;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using Newtonsoft.Json;
using Telegram.Bot;


namespace lapis_philosophorum
{
    public class lapis_philosophorum
    {
        private static string DATA_FILEPATH = Directory.GetCurrentDirectory();
        private static string MODEL_FILEPATH = Directory.GetCurrentDirectory();
      
        public static CoinbaseProClient AICoinBaseClient { get; private set; }
        public static TelegramBotClient telegramBotClient { get; set; }
        public static async Task<bool> CreateModel(string trainingdatapath, string outputmodelpath, string input, int horizon, int windowsize)
        {
            var successful = false;
            try
            {
                MLContext mlContext = new MLContext();
                IDataView trainingDataView = mlContext.Data.LoadFromTextFile<DefinedDataset>(
                                                path: trainingdatapath,
                                                hasHeader: true,
                                                separatorChar: ',',
                                                allowQuoting: true,
                                                allowSparse: false);
                IEstimator<ITransformer> forecastEstimator = BuildTrainingPipeline(mlContext, trainingdatapath, input, horizon, windowsize);
                ITransformer forecastTransformer = forecastEstimator.Fit(trainingDataView);
                TimeSeriesPredictionEngine<DefinedDataset, DefinedForecastset> forecastEngine = forecastTransformer.CreateTimeSeriesEngine<DefinedDataset, DefinedForecastset>(mlContext);
                forecastEngine.CheckPoint(mlContext, outputmodelpath);
                successful = true;
            }
            catch (Exception aa)
            {
                Console.WriteLine(aa);
            }

            await Task.CompletedTask;
            return successful;
        }
        public static IEstimator<ITransformer> BuildTrainingPipeline(MLContext mlContext, string trainingdatapath, string inputcolumn, int horizon, int windowsize)
        {
            Microsoft.ML.Transforms.ColumnSelectingEstimator dataProcessPipeline = null;
            dataProcessPipeline = mlContext.Transforms.SelectColumns("date","high", "low", "open", "close");
            var seriepoints = File.ReadAllLines(trainingdatapath).Count();
            var trainer = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "Future24HPrice",
                inputColumnName: inputcolumn,
                windowSize: windowsize,
                seriesLength: seriepoints,
                trainSize: seriepoints,
                horizon: horizon,
                confidenceLevel: 0.61f,
                confidenceLowerBoundColumn: "LB",
                confidenceUpperBoundColumn: "UB");

            var trainingPipeline = dataProcessPipeline.Append(trainer);
            return trainingPipeline;
        }
        static void Evaluate(IDataView testData, ITransformer model, MLContext mlContext)
        {
            IDataView predictions = model.Transform(testData);
            IEnumerable<float> actual =
                (IEnumerable<float>)mlContext.Data.CreateEnumerable<DefinedDataset>(testData, true)
                    .Select(observed => observed.High);
            IEnumerable<float> forecast =
                (IEnumerable<float>)mlContext.Data.CreateEnumerable<DefinedForecastset>(predictions, true)
                    .Select(prediction => prediction.Future24HPrice[0]);
            var metrics = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);

            var MAE = metrics.Average(error => Math.Abs(error));
            var RMSE = Math.Sqrt(metrics.Average(error => Math.Pow(error, 2)));
            Console.WriteLine("Evaluation Metrics");
            Console.WriteLine("---------------------");
            Console.WriteLine($"Mean Absolute Error: {MAE:F3}");
            Console.WriteLine($"Root Mean Squared Error: {RMSE:F3}\n");
        }
        public static async Task<float[]> GenerateForecast(string modelpath, string currencypair, string input, int horizon)
        {
            Random rnd = new Random();
            var ci = new CultureInfo("en-us");
            MLContext mlContext = new MLContext();
            float[] forecast = null;
            ITransformer forecaster;
            using (var file = File.OpenRead(modelpath))
            {
                forecaster = mlContext.Model.Load(file, out DataViewSchema schema);
            }
            TimeSeriesPredictionEngine<DefinedDataset, DefinedForecastset> forecastEngine = forecaster.CreateTimeSeriesEngine<DefinedDataset, DefinedForecastset>(mlContext);
            DefinedForecastset originalSalesPrediction = forecastEngine.Predict(horizon: horizon, confidenceLevel: 0.61f);

            if (input == "high")
                forecast = originalSalesPrediction.Future24HPrice;
            if (input == "low")
                forecast = originalSalesPrediction.Future24HPrice;
            if (input == "open")
                forecast = originalSalesPrediction.Future24HPrice;
            if (input == "close")
                forecast = originalSalesPrediction.Future24HPrice;

            await Task.CompletedTask;
            return forecast;
        }
        public static async Task AnnounceForecast(string currencypair, int horizon, int windowsize, string chatid, bool institutionalmode)
        {
            bool living = true;
            while (living == true)
            {
                if ((DateTime.Now.Minute == 55 && windowsize == 364 && currencypair == "BTC-USD") | (DateTime.Now.Minute == 55 && windowsize == 91 && currencypair == "BTC-USD") | (DateTime.Now.Minute == 57 && windowsize == 288 && currencypair == "BTC-USD") | (DateTime.Now.Minute == 55 && windowsize == 288 && currencypair == "ETH-USD") | (DateTime.Now.Minute == 53 && windowsize == 288 && currencypair == "SOL-USD") | (DateTime.Now.Minute == 51 && windowsize == 90 && currencypair == "SOL-USD") | (DateTime.Now.Minute == 49 && windowsize == 288 && currencypair == "AAVE-USD") | (DateTime.Now.Minute == 47 && windowsize == 288 && currencypair == "MKR-USD") | (DateTime.Now.Minute == 45 && windowsize == 288 && currencypair == "DASH-USD") | (DateTime.Now.Minute == 43 && windowsize == 288 && currencypair == "COMP-USD") | (DateTime.Now.Minute == 10 && windowsize == 90 && currencypair == "BTC-USD") | (DateTime.Now.Minute == 15 && windowsize == 90 && currencypair == "ETH-USD"))
                {
                    try
                    {
                        Console.WriteLine("Forecasting started for " + currencypair + " | Horizon: " + horizon + " | " + DateTime.Now);
                        float[] forecasthighset;
                        float[] forecastlowset;
                        float[] forecastopenset;
                        float[] forecastcloseset;
                        var successful = false;
                        if (windowsize == 91) 
                            successful = await BuildDailyDataset(currencypair, horizon);
                        if (windowsize == 90)
                            successful = await BuildDailyDataset(currencypair, horizon);
                        if (windowsize == 48)
                            successful = await Build6HourlyDataset(currencypair, horizon);
                        if (windowsize == 288)
                            successful = await BuildHourlyDataset(currencypair, horizon);
                        if (windowsize == 672)
                            successful = await Build15minDataset(currencypair, horizon);
                        if (windowsize == 364)
                            successful = await Build6HourlyDataset(currencypair, horizon);
                        if (successful != true)
                            continue;
                        else
                        {
                            await Task.Delay(10000);
                            var currency = currencypair.Split('-')[0];
                            await CleanDataset(currency, horizon);
                            try{ File.Delete(MODEL_FILEPATH + @"/assets/" + currency + horizon + "1" + ".zip"); } catch(Exception aa){ Console.WriteLine(aa); }
                            try { File.Delete(MODEL_FILEPATH + @"/assets/" + currency + horizon + "2" + ".zip"); } catch (Exception aa) { Console.WriteLine(aa); }
                            try { File.Delete(MODEL_FILEPATH + @"/assets/" + currency + horizon + "3" + ".zip"); } catch (Exception aa) { Console.WriteLine(aa); }
                            try { File.Delete(MODEL_FILEPATH + @"/assets/" + currency + horizon + "4" + ".zip"); } catch (Exception aa) { Console.WriteLine(aa); }
                            try
                            {
                                bool learningSuccessful1 = false;
                                bool learningSuccessful2 = false;
                                bool learningSuccessful3 = false;
                                bool learningSuccessful4 = false;

                                learningSuccessful1 = await CreateModel(DATA_FILEPATH + @"/assets/" + currency + horizon + ".csv", MODEL_FILEPATH + @"/assets/" + currency + horizon + "1" + ".zip", "high", horizon, windowsize);
                                learningSuccessful2 = await CreateModel(DATA_FILEPATH + @"/assets/" + currency + horizon + ".csv", MODEL_FILEPATH + @"/assets/" + currency + horizon + "2" + ".zip", "low", horizon, windowsize);
                                learningSuccessful3 = await CreateModel(DATA_FILEPATH + @"/assets/" + currency + horizon + ".csv", MODEL_FILEPATH + @"/assets/" + currency + horizon + "3" + ".zip", "open", horizon, windowsize);
                                learningSuccessful4 = await CreateModel(DATA_FILEPATH + @"/assets/" + currency + horizon + ".csv", MODEL_FILEPATH + @"/assets/" + currency + horizon + "4" + ".zip", "close", horizon, windowsize);
                                if(learningSuccessful1 == false | learningSuccessful2 == false | learningSuccessful3 == false | learningSuccessful4 == false)
                                    continue;
                                forecasthighset = await GenerateForecast(MODEL_FILEPATH + @"/assets/" + currency + horizon + "1" + ".zip", currency, "high", horizon);
                                await Task.Delay(300);
                                forecastlowset = await GenerateForecast(MODEL_FILEPATH + @"/assets/" + currency + horizon + "2" + ".zip", currency, "low", horizon);
                                await Task.Delay(300);
                                forecastopenset = await GenerateForecast(MODEL_FILEPATH + @"/assets/" + currency + horizon + "3" + ".zip", currency, "open", horizon);
                                await Task.Delay(300);
                                forecastcloseset = await GenerateForecast(MODEL_FILEPATH + @"/assets/" + currency + horizon + "4" + ".zip", currency, "close", horizon);
                                await Task.Delay(300);

                                string direction = "bearish";
                                var days = 0;
                                string forecasttype = "";
                                var today = DateTime.Now;
                                var forecaststring1 = "";
                                var forecaststring2 = "";
                                var forecaststring3 = "";
                                var forecaststring4 = "";
                                var forecaststring5 = "";
                                var forecaststring6 = "";
                                var finalforecast = "";
                                string signal = "";
                                string signalreadable = "";
                                string buyprice = "";
                                string sellprice = "";
                                var positivetrendpoints = 0;
                                var negativetrendpoints = 0;
                                var profitrate = 1.0;
                                Candle[] candles = new Candle[horizon];
                                string finalforecast2 = horizon + " day Forecast for " + currencypair + Environment.NewLine + Environment.NewLine;
                                bool malignant = false; 
                                bool signaltrade = false;
                                float differencee = forecasthighset[0] / 100;
                                float change2 = (forecasthighset.Max() - forecasthighset[0]) / differencee;
                                if (forecasthighset.Max() > (forecasthighset[0]) & change2 >= profitrate)
                                    signaltrade = true;
                                while (days != horizon)
                                {
                                    if (windowsize == 672)
                                        today = today.AddMinutes(15);
                                    if (windowsize == 288)
                                        today = today.AddHours(1);
                                    if (windowsize == 90 | windowsize == 91)
                                        today = today.AddDays(1);
                                    if (windowsize == 48)
                                        today = today.AddHours(6);
                                    if (windowsize == 364)
                                        today = today.AddHours(6);

                                    bool trimhigh = false;
                                    bool trimlow = false;
                                    bool trimopen = false;
                                    bool trimclose = false;
                                    float difference = forecasthighset[0] / 100;
                                    float change = (forecasthighset.Max() - forecasthighset[0]) / difference;
                                    var selltrigger = (forecasthighset[0] / 100) * profitrate;
                                    if (forecasthighset[days].ToString().Contains("."))
                                        trimhigh = true;
                                    if (forecastlowset[days].ToString().Contains("."))
                                        trimlow = true;
                                    if (forecastopenset[days].ToString().Contains("."))
                                        trimopen = true;
                                    if (forecastcloseset[days].ToString().Contains("."))
                                        trimclose = true;
                                    if (forecastopenset[days] > forecastcloseset[days])
                                        direction = "Bearish";
                                    if (forecastcloseset[days] > forecastopenset[days])
                                        direction = "Bullish";

                                    string high = "";
                                    string low = "";
                                    string open = "";
                                    string close = "";
                                    string oldopen = "";
                                    if (forecasthighset[days] != forecasthighset[0])
                                    {
                                        if (forecastlowset[days] > forecastlowset[days - 1])
                                            positivetrendpoints++;
                                        if (forecastlowset[days] < forecastlowset[days - 1])
                                            negativetrendpoints++;
                                    }
                                    if (forecastcloseset[days] != forecastcloseset[0])
                                        oldopen = forecastcloseset[days - 1].ToString();
                                    if (trimhigh == true)
                                        high = forecasthighset[days].ToString("F2");
                                    if (trimlow == true)
                                        low = forecastlowset[days].ToString("F2");
                                    if (trimopen == true)
                                        open = forecastopenset[days].ToString("F2");
                                    if (trimclose == true)
                                        close = forecastcloseset[days].ToString("F2");
                                    if (trimhigh == false)
                                        high = forecasthighset[days].ToString();
                                    if (trimlow == false)
                                        low = forecastlowset[days].ToString();
                                    if (trimopen == false)
                                        open = forecastopenset[days].ToString();
                                    if (trimclose == false)
                                        close = forecastcloseset[days].ToString();
                                    if (windowsize != 91)
                                    {
                                        forecaststring1 = "High: $" + high + " | ";
                                        forecaststring2 = "Low: $" + low + Environment.NewLine;
                                        forecaststring3 = "Open: $" + open + " | ";
                                        forecaststring4 = "Close: $" + close + Environment.NewLine;
                                    }
                                    else
                                    {
                                        forecaststring1 = "High: $" + high + "|";
                                        forecaststring2 = "Low: $" + low + "|";
                                        forecaststring3 = "Open: $" + open + "|";
                                        forecaststring4 = "Close: $" + close + "|";
                                        forecaststring5 = "Market Direction: " + direction + "|";
                                    }

                                    string timestamp = today.ToString();
                                    string candletime = today.ToString(@"yyyy-MM-dd");
                                    var forecastcandle = new Candle
                                    {
                                        date = candletime,
                                        high = Convert.ToDecimal(forecasthighset[days]),
                                        low = Convert.ToDecimal(forecastlowset[days]),
                                        open = Convert.ToDecimal(forecastopenset[days]),
                                        close = Convert.ToDecimal(forecastcloseset[days])
                                    };
                                    candles[days] = forecastcandle;
                                    if (windowsize == 90)
                                    {
                                        timestamp = today.ToShortDateString();
                                        forecasttype = "Daily Candle";
                                    }
                                    if (windowsize == 672)
                                        forecasttype = "15 Minute Candle";
                                    if (windowsize == 48)
                                        forecasttype = "6 Hour Candle";
                                    if (windowsize == 288)
                                        forecasttype = "Hourly Candle";
                                    if (signaltrade == true & forecasthighset[0] == forecasthighset[days] & positivetrendpoints >= negativetrendpoints)
                                    {
                                        if (!finalforecast.Contains("Buy"))
                                        {
                                            forecaststring6 = " | "+ "Buy Here";
                                            signal = DateTime.Now + "|" + currencypair + "|" + forecasttype + "|" + "Buy|" + high + "=" + today + "|";
                                            signalreadable = currencypair + " | " + forecasttype + " | " + "Buy | $" + high + " at " + today + " >> ";
                                        }
                                    }
                                    if (signaltrade == true & forecasthighset[days] >= forecasthighset.Max())
                                    {
                                        if (!finalforecast.Contains("Sell"))
                                        {
                                            forecaststring6 = " | " + "Sell Here - Profit: " + change + "%";
                                            signal = signal + "Sell |" + high + "=" + today + "|" + (change).ToString("F2");
                                            signalreadable = signalreadable + " Sell | $" + high + " at " + today + " | Profit: " + (change).ToString("F2") + "%";
                                            if (change >= 25.0 | change < profitrate)
                                            {
                                                malignant = true;
                                            }
                                        }
                                    }
                                    if (signaltrade != true)
                                    {
                                        var onepercent = forecasthighset[0] / 100;
                                        if (days != 0)
                                        {
                                            var a = Math.Round((forecasthighset[days] - forecasthighset[0]) / onepercent, 1);
                                            if (a <= -20 & forecasttype == "Hourly Candle")
                                            {
                                                malignant = true;
                                            }
                                            if (malignant != true)
                                            {
                                                if (!finalforecast.Contains("Crash"))
                                                {
                                                    if (a <= -1.0 & a >= -2.9)
                                                    {
                                                        signal = DateTime.Now + " | " + currencypair + " | " + forecasttype + " | " + "Market Dip Detected" + " | " + "Current Value: $" + forecasthighset[0] + "| Predicted Value: $" + forecasthighset[days] + "| Change: " + a + "%";
                                                        forecaststring6 = " | " + "Change in Price: " + a + "% : Dip Detected";
                                                    }
                                                    if (a <= -3.0 & a >= -6.9)
                                                    {
                                                        signal = DateTime.Now + " | " + currencypair + " | " + forecasttype + " | " + "Market Drop Detected" + " | " + "Current Value: $" + forecasthighset[0] + "| Predicted Value: $" + forecasthighset[days] + "| Change: " + a + "%";
                                                        forecaststring6 = " | " + "Change in Price: " + a + "% : Drop Detected";
                                                    }
                                                    if (a <= -7.0)
                                                    {
                                                        signal = DateTime.Now + " | " + currencypair + " | " + forecasttype + " | " + "Market Crash Detected - Entering insurance mode!" + " | " + "Current Value: $" + forecasthighset[0] + "| Predicted Value: $" + forecasthighset[days] + "| Change: " + a + "%" + "|" + "Exit Deadline: " + today;
                                                        forecaststring6 = " | " + "Change in Price: " + a + "% : Crash Detected";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finalforecast = finalforecast + Environment.NewLine + Environment.NewLine + timestamp + Environment.NewLine + forecaststring1 + forecaststring2 + forecaststring3 + forecaststring4 + forecaststring5 + forecaststring6;
                                    finalforecast2 = finalforecast2 + today.ToShortTimeString() + Environment.NewLine + forecaststring1 + forecaststring2 + Environment.NewLine;
                                    days++;
                                }

                                if (finalforecast.Contains("Buy") & finalforecast.Contains("Sell") & malignant == false)
                                {
                                    await telegramBotClient.SendTextMessageAsync(chatId: chatid, text: "ViventusAI ♾️" + currencypair + " " + forecasttype + " Forecast" + Environment.NewLine + finalforecast);
                                    if (forecasttype.Contains("Hourly"))
                                    {
                                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + "signals.viventus", signal + Environment.NewLine);
                                        //await telegramBotClient.SendTextMessageAsync(chatId: chatid, text: signalreadable);
                                    }
                                    if (forecasttype == "Daily Candle")
                                    { 
                                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + "signals2.viventus", signal + Environment.NewLine);
                                    }
                                }
                                string CandlejsonString = JsonConvert.SerializeObject(candles);    
                                File.WriteAllText(Directory.GetCurrentDirectory()+@"/assets/" + currencypair + windowsize + ".json", CandlejsonString);
                                File.WriteAllText(Directory.GetCurrentDirectory() + "/assets/" + currencypair + windowsize + ".forecast", finalforecast);
                                Console.WriteLine("Forecasting finished for " + currencypair + " | Horizon: " + horizon + " | " + DateTime.Now);
                                if (finalforecast.Contains("Crash") | finalforecast.Contains("Dip") | finalforecast.Contains("Drop"))
                                {
                                    if (forecasttype == "Hourly Candle")
                                        await telegramBotClient.SendTextMessageAsync(chatId: chatid, text: "ViventusAI ♾️" + currencypair + " " + forecasttype + " Downtrend Forecast" + Environment.NewLine + finalforecast);
                                    if (forecasttype == "6 Hour Candle")
                                        await telegramBotClient.SendTextMessageAsync(chatId: chatid, text: "ViventusAI ♾️" + currencypair + " " + forecasttype + " Downtrend Forecast" + Environment.NewLine + finalforecast);
                                    if (forecasttype == "15 Minute Candle")
                                        await telegramBotClient.SendTextMessageAsync(chatId: chatid, text: "ViventusAI ♾️" + currencypair + " " + forecasttype + " Downtrend Forecast" + Environment.NewLine + finalforecast);
                                    if (forecasttype == "Daily Candle")
                                        await telegramBotClient.SendTextMessageAsync(chatId: chatid, text: "ViventusAI ♾️" + currencypair + " " + forecasttype + " Downtrend Forecast" + Environment.NewLine + finalforecast);
                                    if (forecasttype.Contains("Hourly"))
                                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + "hourly-signals.viventus", signal + Environment.NewLine);
                                    if (forecasttype == "Daily Candle")
                                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + "daily-signals.viventus", signal + Environment.NewLine);
                                }
                            }
                            catch (Exception aa)
                            {
                                Console.WriteLine(aa);
                            }
                        }
                    }
                    catch (Exception aa)
                    {
                        Console.WriteLine(aa);
                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + "learning-errors.viventus", aa + Environment.NewLine + Environment.NewLine);
                    }
                    await Task.Delay(60000);
                }
                await Task.Delay(2000);
            }
        }
        public static async Task ClearSignalsPulse()
        {
            bool living = true;
            while (living)
            {
                await Task.Delay(7200000);
                if (File.Exists(Directory.GetCurrentDirectory() + @"\assets\signals.viventus"))
                    File.Delete(Directory.GetCurrentDirectory() + @"\assets\signals.viventus");

                File.AppendAllText(Directory.GetCurrentDirectory() + @"\assets\signals.viventus", Environment.NewLine);
            }
        }
        public static async Task ClearSignalsPulse2()
        {
            bool living = true;
            while (living)
            {
                await Task.Delay(129600000);
                if (File.Exists(Directory.GetCurrentDirectory() + @"\assets\signals2.viventus"))
                    File.Delete(Directory.GetCurrentDirectory() + @"\assets\signals2.viventus");

                File.AppendAllText(Directory.GetCurrentDirectory() + @"\assets\signals2.viventus", Environment.NewLine);
            }
        }
        public static async Task<bool> BuildDailyDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
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
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past9, now, 86400));

                try { File.Delete(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", "time,high,low,open,close,volume" + Environment.NewLine);

                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.Date.Date.ToShortDateString() + ", " + data.High + ", " + data.Low + ", " + data.Open + ", " + data.Close + ", " + data.Volume;
                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                Console.WriteLine("DataSet Complete");
                successful = true;
            }
            catch (Exception aa)
            {

                Console.WriteLine(aa);
                successful = false;
         
            }
            await Task.Delay(1000);
            return successful;
        }
        public static async Task<bool> Build6HourlyDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
            DateTime past1 = DateTime.Today.Subtract(TimeSpan.FromDays(150));
            DateTime past2 = past1.AddDays(30);
            DateTime past3 = past2.AddDays(30);
            DateTime past4 = past3.AddDays(30);
            DateTime past5 = past4.AddDays(30);
            DateTime past6 = past5.AddDays(30);
            DateTime now = DateTime.Today;
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                //API rate-limited -- 1 sec delay
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past1, past2, 21600));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past2, past3, 21600));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past3, past4, 21600));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past4, past5, 21600));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past5, past6, 21600));
                await Task.Delay(1010); 
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past6, now, 21600));
                await Task.Delay(1010);
                try { File.Delete(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv"); } catch{ }
                File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", "time,high,low,open,close,volume" + Environment.NewLine);
                foreach(var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.Date.Date.ToShortDateString() + ", " + data.High + ", " + data.Low + ", " + data.Open + ", " + data.Close + ", " + data.Volume;
                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                successful = true;
                Console.WriteLine("DataSet Complete");
            }
            catch (Exception aa)
            {
                successful = false;
                Console.WriteLine(aa);
            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task<bool> BuildHourlyDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
            DateTime past = DateTime.Now.Subtract(TimeSpan.FromDays(28));
            DateTime past2 = past.AddDays(7);
            DateTime past3 = past2.AddDays(7);
            DateTime past4 = past3.AddDays(7);
            DateTime now = DateTime.Now;
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                //API rate-limited -- 1 sec delay
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past, past2, 3600));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past2, past3, 3600));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past3, past4, 3600));
                await Task.Delay(1200);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past4, now, 3600));
                await Task.Delay(1200);
                try { File.Delete(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", "time,high,low,open,close,volume" + Environment.NewLine);
                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.Date.Date.ToShortDateString() + ", " + data.High + ", " + data.Low + ", " + data.Open + ", " + data.Close + ", " + data.Volume;
                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                successful = true;
                Console.WriteLine("DataSet Complete");
            }
            catch (Exception aa)
            {
                successful = false;
                Console.WriteLine(aa);
            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task<bool> Build15minDataset(string currencypair, int horizon)
        {
            var successful = false;
            var basepair = currencypair.Split('-')[0];
            AICoinBaseClient = new CoinbaseProClient();
            DateTime past = DateTime.Now.Subtract(TimeSpan.FromDays(14));
            DateTime past2 = past.AddDays(1);
            DateTime past3 = past2.AddDays(1);
            DateTime past4 = past3.AddDays(1);
            DateTime past5 = past4.AddDays(1);
            DateTime past6 = past5.AddDays(1);
            DateTime past7 = past6.AddDays(1);
            DateTime past8 = past7.AddDays(1);
            DateTime past9 = past8.AddDays(1);
            DateTime past10 = past9.AddDays(1);
            DateTime past11 = past10.AddDays(1);
            DateTime past12 = past11.AddDays(1);
            DateTime past13 = past12.AddDays(1);
            DateTime past14 = past13.AddDays(1);
            DateTime now = DateTime.Now;
            List<List<Coinbase.Pro.Models.Candle>> dataset = new List<List<Coinbase.Pro.Models.Candle>>();
            try
            {
                //API rate-limited -- 1 sec delay
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past, past2, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past2, past3, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past3, past4, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past4, past5, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past5, past6, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past6, past7, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past7, past8, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past8, past9, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past9, past10, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past10, past11, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past11, past12, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past12, past13, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past13, past14, 900));
                await Task.Delay(1010);
                dataset.Add(await AICoinBaseClient.MarketData.GetHistoricRatesAsync(currencypair, past14, now, 900));
                await Task.Delay(50);
                try { File.Delete(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv"); } catch { }
                File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", "time,high,low,open,close,volume" + Environment.NewLine);
                foreach (var piece in dataset)
                {
                    piece.Reverse();
                    foreach (var data in piece)
                    {
                        string datastring = data.Time.Date.Date.ToShortDateString() + ", " + data.High + ", " + data.Low + ", " + data.Open + ", " + data.Close + ", " + data.Volume;
                        File.AppendAllText(Directory.GetCurrentDirectory() + "/assets/" + basepair + horizon + ".csv", datastring + Environment.NewLine);
                    }
                }
                successful = true;
                Console.WriteLine("DataSet Complete");
            }
            catch (Exception aa)
            {
                successful = false;
                Console.WriteLine(aa);
            }
            await Task.CompletedTask;
            return successful;
        }
        public static async Task CleanDataset(string currency, int horizon)
        {
            try
            {
                var dataset = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + "/assets/" + currency + horizon + ".csv");
                string[] newdataset = new string[dataset.Length];
                int count = 0;
                foreach (var entry in dataset)
                {

                    if (!newdataset.Contains(entry))
                    {
                        newdataset[count] = entry;

                        count++;
                    }
                }
                System.IO.File.Delete(Directory.GetCurrentDirectory() + "/assets/" + currency + horizon + ".csv");
                System.IO.File.AppendAllLines(Directory.GetCurrentDirectory() + "/assets/" + currency + horizon + ".csv", newdataset);
                Console.WriteLine("Cleanse Finished!");
            }
            catch (Exception fg)
            {
                Console.WriteLine(fg);
            }
            await Task.CompletedTask;
        }
      
    }   
    public class DefinedForecastset
        {
            public float[] Future24HPrice { get; set; }
            public float[] LB { get; set; }
            public float[] UB { get; set; }
        }
    public class DefinedDataset
        {
            [ColumnName("date"), LoadColumn(0)]
            public string Date { get; set; }

            [ColumnName("high"), LoadColumn(1)]
            public float High { get; set; }

            [ColumnName("low"), LoadColumn(2)]
            public float Low { get; set; }

            [ColumnName("open"), LoadColumn(3)]
            public float Open { get; set; }

            [ColumnName("close"), LoadColumn(4)]
            public float Close { get; set; }

            [ColumnName("volume"), LoadColumn(5)]
            public float Volume { get; set; }
    }
    public class Candle 
    {
        public string date { get; set; }
        public decimal open { get; set; } 
        public decimal high { get; set; }
        public decimal low { get; set; }
        public decimal close { get; set; } 
    }

}


