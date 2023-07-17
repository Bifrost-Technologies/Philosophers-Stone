using Bifrost.PhilosophersStone;
using Microsoft.ML.Transforms.TimeSeries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;


namespace Bifrost.PhilosophersStone
{
    public static class CognitiveService
    {  
        /// <summary>
        /// Neural Tree - Invoked to initialize and run the cognitive service
        /// </summary>
        public static async Task NeuralTree()
        {
            bool living = true;
            //Neural-Tree
            while (living)
            {
                Console.WriteLine("Watching 9 dimensions for 3 coins resulting in 3 paths per coin with 9 paths total");
                lapis_philosophorum D1 = new lapis_philosophorum();
                lapis_philosophorum D2 = new lapis_philosophorum();
                lapis_philosophorum D3 = new lapis_philosophorum();
                lapis_philosophorum D4 = new lapis_philosophorum();
                lapis_philosophorum D5 = new lapis_philosophorum();
                lapis_philosophorum D6 = new lapis_philosophorum();
                lapis_philosophorum D7 = new lapis_philosophorum();
                lapis_philosophorum D8 = new lapis_philosophorum();
                lapis_philosophorum D9 = new lapis_philosophorum();

                //Horizon & Windowsize needs to be evaluated and tweaked to work perfectly - Use the AI Trainer to hypertune the parameters for the training pipelines
                await AnnounceForecast(D1,"BTC-USD", DimensionType.Hourly, 24, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D2,"BTC-USD", DimensionType.Minute15, 96, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D3,"BTC-USD", DimensionType.Minute5, 288, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D4, "ETH-USD", DimensionType.Hourly, 24, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D5, "ETH-USD", DimensionType.Minute15, 96, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D6,"ETH-USD", DimensionType.Minute5, 288, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D7, "SOL-USD", DimensionType.Hourly, 24, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D8, "SOL-USD", DimensionType.Minute15, 96, 288);
                await Task.Delay(5000);
                await AnnounceForecast(D9,"SOL-USD", DimensionType.Minute5, 288, 288);
                await Task.Delay(300000);
                //Cognitive loop every 5 minutes
            }
        }
        public static async Task AnnounceForecast(lapis_philosophorum ForecastEngine, string currencypair, DimensionType dimensionType, int horizon, int windowsize)
        {
            try
            {
                Console.WriteLine("Forecasting started for " + currencypair + " | Horizon: " + horizon + " | " + DateTime.Now);
                float[] forecasthighset;
                float[] forecastlowset;
                if (!Directory.Exists(lapis_philosophorum.DATA_FILEPATH))
                    Directory.CreateDirectory(lapis_philosophorum.DATA_FILEPATH);
                var successful = false;
                if (dimensionType == DimensionType.Daily)
                    successful = await DatasetGenerator.BuildDailyDataset(currencypair, horizon);
                if (dimensionType == DimensionType.SixHourly)
                    successful = await DatasetGenerator.Build6HourlyDataset(currencypair, horizon);
                if (dimensionType == DimensionType.Hourly)
                    successful = await DatasetGenerator.BuildHourlyDataset(currencypair, horizon);
                if (dimensionType == DimensionType.Minute15)
                    successful = await DatasetGenerator.Build15minDataset(currencypair, horizon);
                if (dimensionType == DimensionType.Minute5)
                    successful = await DatasetGenerator.Build5minDataset(currencypair, horizon);
                
                if (successful == true)
                {
                    var currency = currencypair.Split('-')[0];
                    await DatasetGenerator.CleanDataset(currency, horizon);

                    try
                    {
                        var forecasthighdata = ForecastEngine.GenerateForecast(currencypair, "high", horizon, windowsize);
                        forecasthighset = forecasthighdata.PredictedPrice;
                      
                        var forecastlowdata = ForecastEngine.GenerateForecast(currencypair, "low", horizon, windowsize);
                        forecastlowset = forecastlowdata.PredictedPrice;

                        var days = 0;
                        var today = DateTimeOffset.UtcNow;

                        Candle[] candles = new Candle[horizon];

                        while (days != horizon)
                        {   
                            if (dimensionType == DimensionType.Daily)
                                today = today.AddDays(1);
                            if (dimensionType == DimensionType.SixHourly)
                                today = today.AddHours(6);
                            if (dimensionType == DimensionType.Hourly)
                                today = today.AddHours(1);
                            if (dimensionType == DimensionType.Minute5)
                                today = today.AddMinutes(5);
                            if (dimensionType == DimensionType.Minute15)
                                today = today.AddMinutes(15);

                            string timestamp = today.ToUnixTimeSeconds().ToString();
                            string candletime = timestamp;
                            var forecastcandle = new Candle
                            {
                                date = candletime,
                                high = Convert.ToInt32(forecasthighset[days]),
                                low = Convert.ToInt32(forecastlowset[days]),
                            };
                            candles[days] = forecastcandle;

                            days++;
                        }

                        string CandlejsonString = JsonConvert.SerializeObject(candles);

                        File.WriteAllText(lapis_philosophorum.WEBDATA_FILEPATH + currencypair + dimensionType.ToString() + ".json", CandlejsonString);
                        Console.WriteLine("Forecasting Finished for " + currencypair + " | Horizon: " + horizon + " | " + DateTime.Now);
                    }
                    catch (Exception Error)
                    {
                        Console.WriteLine(Error);
                    }
                }
            }
            catch (Exception Error)
            {
                Console.WriteLine(Error);
                File.AppendAllText(lapis_philosophorum.DATA_FILEPATH + "learning-errors.log", Error + Environment.NewLine + Environment.NewLine);
                await Task.Delay(10000);
            }
            await Task.Delay(2000);
        }

    
    }
}
