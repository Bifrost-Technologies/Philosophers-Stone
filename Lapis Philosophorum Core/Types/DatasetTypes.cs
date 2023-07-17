using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bifrost.PhilosophersStone
{
    public class ForecastData
    {
        public float[] PredictedPrice { get; set; }
        public float[] LB { get; set; }
        public float[] UB { get; set; }
    }
    public class CandleData
    {
        [ColumnName("date"), LoadColumn(0)]
        public DateTime Date { get; set; }

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
    public enum DimensionType 
    {   
        Daily = 0,

        SixHourly = 1,

        Hourly = 2,

        Minute15 = 3,

        Minute5 = 4,
    }
}
