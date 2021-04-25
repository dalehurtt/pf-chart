using System;

namespace Charts
{
    public class DailyPointValues {
        //public int Change { get; set; }
        public decimal Close { get; set; }
        public DateTime Date { get; set; }
        public decimal HighLow { get; set; }
        public int HighLowIndex { get; set; }
        public decimal Point { get; set; }
        public int PointIndex { get; set; }
        public PointSignal Signal { get; set; }
        public decimal Target { get; set; }
        public int TargetIndex { get; set; }

        public string ToCsv() {
            return $"{Date:yyyy-MM-dd},{Close:F2},{Point:F2},{Signal},{HighLow:F2},{Target:F2}";
        }

        public string PrintHeader () {
            return $"Date,Close,Point,Signal,High/Low,Target";
        }
    }

    public enum PointSignal
    {
        Buy,
        Up,
        Sell,
        Down,
        Unknown
    }
}
