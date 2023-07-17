using Coinbase.Pro;
using Coinbase.Pro.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;


namespace Bifrost.PhilosophersStone
{
    public class lapis_philosophorum
    {
        public static string DATA_FILEPATH = Directory.GetCurrentDirectory() + @"/assets/";
        public static string WEBDATA_FILEPATH = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.ToString()).ToString() + @"/wwwroot/data/";
        public async Task<bool> CreatePretrainedModel(string trainingdatapath, string outputmodelpath, string input, int horizon, int windowsize)
        {
            var successful = false;
            try
            {
                MLContext mlContext = new MLContext();
                IDataView trainingDataView = mlContext.Data.LoadFromTextFile<CandleData>(path: trainingdatapath, hasHeader: true, separatorChar: ',', allowQuoting: true, allowSparse: false);
                IEstimator<ITransformer> forecastEstimator = null;

                if (horizon == 24)
                    forecastEstimator = BuildDimension1TrainingPipeline(mlContext, trainingdatapath, input, horizon, windowsize);
                if (horizon == 96)
                    forecastEstimator = BuildDimension2TrainingPipeline(mlContext, trainingdatapath, input, horizon, windowsize);
                if (horizon == 288)
                    forecastEstimator = BuildDimension3TrainingPipeline(mlContext, trainingdatapath, input, horizon, windowsize);

                ITransformer forecastTransformer = forecastEstimator.Fit(trainingDataView);
                TimeSeriesPredictionEngine<CandleData, ForecastData> forecastEngine = forecastTransformer.CreateTimeSeriesEngine<CandleData, ForecastData>(mlContext);
                forecastEngine.CheckPoint(mlContext, outputmodelpath);
                successful = true;
            }
            catch (Exception Error)
            {
                Console.WriteLine(Error);
            }

            await Task.CompletedTask;
            return successful;
        }

        //Use the AI Trainer.mbconfig to hypertune the parameters for the training pipelines!
        public IEstimator<ITransformer> BuildDimension1TrainingPipeline(MLContext mlContext, string trainingdatapath, string inputcolumn, int horizon, int windowsize)
        {
            var seriepoints = File.ReadAllLines(trainingdatapath).Count();
            var trainer = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "PredictedPrice",
                inputColumnName: inputcolumn,
                windowSize: 12,
                seriesLength: seriepoints,
                trainSize: 1676,
                horizon: horizon,
               
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: "LB",
                confidenceUpperBoundColumn: "UB");

            return trainer;
        }
        public IEstimator<ITransformer> BuildDimension2TrainingPipeline(MLContext mlContext, string trainingdatapath, string inputcolumn, int horizon, int windowsize)
        {
            var seriepoints = File.ReadAllLines(trainingdatapath).Count();
            var trainer = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "PredictedPrice",
                inputColumnName: inputcolumn,
                windowSize: 47, 
                seriesLength: seriepoints, 
                trainSize: 1344, 
                horizon: 96,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: "LB",
                confidenceUpperBoundColumn: "UB");

            return trainer;
        }
        public IEstimator<ITransformer> BuildDimension3TrainingPipeline(MLContext mlContext, string trainingdatapath, string inputcolumn, int horizon, int windowsize)
        {
     
            var seriepoints = File.ReadAllLines(trainingdatapath).Count();
            var trainer = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "PredictedPrice",
                inputColumnName: inputcolumn,
                windowSize: 429, 
                seriesLength: seriepoints, 
                trainSize: 4032, 
                horizon: 288,

                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: "LB",
                confidenceUpperBoundColumn: "UB");

            
            return trainer;
        }

        public void Evaluate(IDataView testData, ITransformer model, MLContext mlContext)
        {
            IDataView predictions = model.Transform(testData);
            IEnumerable<float> actual =
                (IEnumerable<float>)mlContext.Data.CreateEnumerable<CandleData>(testData, true)
                    .Select(observed => observed.High);
            IEnumerable<float> forecast =
                (IEnumerable<float>)mlContext.Data.CreateEnumerable<ForecastData>(predictions, true)
                    .Select(prediction => prediction.PredictedPrice[0]);
            var metrics = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);

            var MAE = metrics.Average(error => Math.Abs(error));
            var RMSE = Math.Sqrt(metrics.Average(error => Math.Pow(error, 2)));
            Console.WriteLine("Evaluation Metrics");
            Console.WriteLine("---------------------");
            Console.WriteLine($"Mean Absolute Error: {MAE:F3}");
            Console.WriteLine($"Root Mean Squared Error: {RMSE:F3}\n");
        }
        public ForecastData GenerateForecast(string currencypair, string input, int horizon, int windowsize)
        {
            var currency = currencypair.Split('-')[0];
            string trainingdatapath = DATA_FILEPATH + currency + horizon + ".csv";
            MLContext mlContext = new MLContext();
            IDataView trainingDataView = mlContext.Data.LoadFromTextFile<CandleData>(path: trainingdatapath, hasHeader: true, separatorChar: ',', allowQuoting: true, allowSparse: false);

            IEstimator<ITransformer> forecastEstimator = null;
            
            if(horizon == 24)
                forecastEstimator = BuildDimension1TrainingPipeline(mlContext, trainingdatapath, input, horizon, windowsize);
            if(horizon == 96)
                forecastEstimator = BuildDimension2TrainingPipeline(mlContext, trainingdatapath, input, horizon, windowsize);
            if(horizon == 288)
                forecastEstimator = BuildDimension3TrainingPipeline(mlContext, trainingdatapath, input, horizon, windowsize);

            ITransformer forecastTransformer = forecastEstimator.Fit(trainingDataView);

            TimeSeriesPredictionEngine<CandleData, ForecastData> forecastEngine = forecastTransformer.CreateTimeSeriesEngine<CandleData, ForecastData>(mlContext);

            ForecastData prediction = forecastEngine.Predict(horizon: horizon, confidenceLevel: 0.95f);

            return prediction;
        }
      
    }

}


