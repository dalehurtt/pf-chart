using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PFCharter {
    class DailyStockValues {
        public decimal AdjustedClose { get; }
        public decimal Close { get; }
        public DateTime Date { get; }
        public decimal High { get; }
        public decimal Low { get; }
        public decimal Open { get; }
        public decimal Volume { get; }

        public DailyStockValues (string date, string open, string high, string low, string close, string adjclose, string volume) {
            try {
                Date = Convert.ToDateTime (date);
                Open = Convert.ToDecimal (open);
                High = Convert.ToDecimal (high);
                Low = Convert.ToDecimal (low);
                Close = Convert.ToDecimal (close);
                AdjustedClose = Convert.ToDecimal (adjclose);
                Volume = Convert.ToDecimal (volume);
            }
            catch { }
        }

        public static DailyStockValues FromCsv (string csvLineAsString) {
            DailyStockValues values = null;
            try {
                string [] parsed = csvLineAsString.Split (',');
                values = new DailyStockValues (parsed [0], parsed [1], parsed [2], parsed [3], parsed [4], parsed [5], parsed [6]);
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "DailyStockValues.FromCsv");
            }
            return values;
        }
    }
}
