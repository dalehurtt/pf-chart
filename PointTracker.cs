using System;
namespace Charts {
    public class PointTracker {
        public decimal LastPoint { get; set; }
        //public decimal LowestPoint { get; set; }
        //public decimal HighestPoint { get; set; }

        public int LastIndex { get; set; }
        public int LowestIndex { get; set; }
        public int HighestIndex { get; set; }
        public int PreviousHighIndex { get; set; }
        public int PreviousLowIndex { get; set; }
        public int TargetIndex { get; set; }
    }
}
