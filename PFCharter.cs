using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Charts {
    public static class PFCharter {

        private static string [] tickers;
        private static string inputFile;
        private static string outputFile;
        private static string dirPath;
        public static string currentTicker;

        private static readonly decimal percentageChange = 0.015M;

        static void Main () {
            try {
                var appSettings = ConfigurationManager.AppSettings;
                if (System.Environment.OSVersion.Platform == PlatformID.Unix) {
                    dirPath = appSettings ["dirpathmac"];
                }
                else {
                    dirPath = appSettings ["dirpath"];
                }
                string tickersValue = appSettings ["tickers"];
                tickers = tickersValue.Split (',');
                foreach (string ticker in tickers) {
                    currentTicker = ticker;
                    inputFile = $"{dirPath}{ticker}.csv";
                    outputFile = $"{dirPath}{ticker}-pf.csv";

                    inputFile.GetPathInfo ()
                    .ReadStockFile ()
                    .CalculateAllPoints ()
                    .OutputPointValuesAsCsv (outputFile);
                }
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "Main");
            }
        }

        private static (List<DailyPointValues>, List<decimal>) CalculateAllPoints (this List<DailyStockValues> values) {
            List<DailyPointValues> points = null;
            List<decimal> scale = null;
            try {
                scale = values.GetHighLowAndIncrement ()
                    .OutputScale ();

                var cnt = values.Count;
                points = new List<DailyPointValues> (cnt);

                DailyPointValues lastPoints = new DailyPointValues {
                    Close = 0,
                    HighLow = 0,
                    HighLowIndex = -1,
                    Point = 0,
                    PointIndex = -1,
                    Signal = PointSignal.Unknown,
                    Target = 0,
                    TargetIndex = -1
                };

                for (var i = 0; i < cnt; i++) {
                    DailyPointValues newPoints = values [i].CalculatePoint (scale, lastPoints);
                    lastPoints = newPoints;
                    points.Add (newPoints);
                }
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "CalculateAllPoints");
            }
            return (points, scale);
        }

        private static DailyPointValues CalculatePoint (this DailyStockValues value, List<decimal> scale, DailyPointValues lastPoints) {
            DailyPointValues newPoints = null;
            decimal newPoint;
            int newIndex;
            try {
                (newPoint, newIndex) = FindPointValue (scale, value.Close, lastPoints.Signal);

                newPoints = new DailyPointValues {
                    Close = value.Close,
                    Date = value.Date
                };

                switch (lastPoints.Signal) {
                    case PointSignal.Unknown:
                        // This is the first data point.
                        if (lastPoints.PointIndex == -1) {
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Unknown;
                            newPoints.TargetIndex = -1;
                            newPoints.Target = 0;
                        }
                        // The Close went up.
                        else if (lastPoints.Close < value.Close) {
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Up;
                            newPoints.TargetIndex = newIndex + 3;
                            newPoints.Target = scale [newPoints.TargetIndex];
                        }
                        // The Close went down.
                        else if (lastPoints.Close > value.Close) {
                            // Because Unknown is rounded as if it were Up, we need to adjust it back down.
                            newIndex -= 1;
                            newPoint = scale [newIndex];
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Down;
                            newPoints.TargetIndex = newIndex >= 3 ? newIndex - 3 : 0;
                            newPoints.Target = scale [newPoints.TargetIndex];
                        }
                        // The Close was the same.
                        else {
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Unknown;
                            newPoints.TargetIndex = -1;
                            newPoints.Target = 0;
                        }
                        break;

                    case PointSignal.Buy:
                    case PointSignal.Up:
                        // The Close went below the Target.
                        if (value.Close <= lastPoints.Target) {
                            var newTargetIndex = newIndex >= 3 ? newIndex - 3 : 0;
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Down;
                            newPoints.TargetIndex = newTargetIndex;
                            newPoints.Target = scale [newTargetIndex];
                        }
                        // The Close went up.
                        else if (value.Close > lastPoints.HighLow) {
                            var newTargetIndex = newIndex + 3;
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Up;
                            newPoints.TargetIndex = newTargetIndex;
                            newPoints.Target = scale [newTargetIndex];
                        }
                        // The Close was the same or not high enough to make the Target.
                        else {
                            newPoints.HighLowIndex = lastPoints.HighLowIndex;
                            newPoints.HighLow = lastPoints.HighLow;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Up;
                            newPoints.TargetIndex = lastPoints.TargetIndex;
                            newPoints.Target = lastPoints.Target;
                        }
                        break;

                    case PointSignal.Sell:
                    case PointSignal.Down:
                        // The Close went above the Target.
                        if (value.Close >= lastPoints.Target) {
                            var newTargetIndex = newIndex + 3;
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Up;
                            newPoints.TargetIndex = newTargetIndex;
                            newPoints.Target = scale [newTargetIndex];
                        }
                        // The Close went down.
                        else if (value.Close < lastPoints.HighLow) {
                            var newTargetIndex = newIndex >= 3 ? newIndex - 3 : 0;
                            newPoints.HighLowIndex = newIndex;
                            newPoints.HighLow = newPoint;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Down;
                            newPoints.TargetIndex = newTargetIndex;
                            newPoints.Target = scale [newTargetIndex];
                        }
                        // The Close was the same or not high enough to make the Target.
                        else {
                            newPoints.HighLowIndex = lastPoints.HighLowIndex;
                            newPoints.HighLow = lastPoints.HighLow;
                            newPoints.PointIndex = newIndex;
                            newPoints.Point = newPoint;
                            newPoints.Signal = PointSignal.Down;
                            newPoints.TargetIndex = lastPoints.TargetIndex;
                            newPoints.Target = lastPoints.Target;
                        }
                        break;
                }
            }
            catch (Exception ex) {
                Utils.WriteToConsole ($"{Utils.ExToString (ex)} {currentTicker}", true, "CalculatePoint");
            }
            return newPoints;
        }

        private static (decimal, int) FindPointValue (List<decimal> scale, decimal close, PointSignal lastSignal) {
            try {
                int cnt = scale.Count;
                decimal lastPoint = 0;
                int lastIndex = -1;

                switch (lastSignal) {
                    case PointSignal.Buy:
                    case PointSignal.Up:
                    case PointSignal.Unknown:
                        for (int i = cnt - 1; i >= 0; i--) {
                            if (close < scale [i]) {
                                lastPoint = scale [i + 1];
                                lastIndex = i + 1;
                                break;
                            }
                        }
                        break;

                    default:
                        for (int i = 0; i < cnt; i++) {
                            if (close > scale [i]) {
                                lastPoint = scale [i - 1];
                                lastIndex = i - 1;
                                break;
                            }
                        }
                        break;
                }
                return (lastPoint, lastIndex);
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "FindPointValue");
                return (close, -1);
            }
        }

        private static List<decimal> GetHighLowAndIncrement (this List<DailyStockValues> values) {
            List<decimal> scale = null;
            try {
                decimal high = 0, low = 0;
                foreach (DailyStockValues value in values) {
                    if (value.High > high)
                        high = value.High;
                    if ((low == 0 && value.Low < high) || value.Low < low)
                        low = value.Low;
                }

                scale = new List<decimal> ();
                decimal scaleHigh = high * (1 + percentageChange);
                scale.Add (scaleHigh);
                decimal ctr = high;
                while (ctr >= low) {
                    scale.Add (ctr);
                    ctr -= (ctr * percentageChange);
                }
                scale.Add (ctr);

            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "GetHighLowAndIncrement");
            }
            return scale;
        }

        private static PathInfo GetPathInfo (this string filePath) {
            try {
                string dirName = string.Empty, fileName = string.Empty, fullPath = string.Empty;
                FileInfo fi = new FileInfo (filePath);
                if (fi.Exists) {
                    dirName = fi.DirectoryName;
                    fileName = fi.Name;
                    fullPath = filePath;
                    return new PathInfo {
                        DirName = dirName,
                        FileName = fileName,
                        FullPath = fullPath
                    };
                }
                else
                    throw new Exception ($"{filePath} does not exist.");
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "GetPathInfo");
                return null;
            }
        }

        private static List<DailyPointValues> OutputPointValuesAsCsv (this (List<DailyPointValues> values, List<decimal> scale) tup, string filePath) {
            try {
                using StreamWriter file = new StreamWriter (filePath);
                file.WriteLine (tup.values [0].PrintHeader ());
                foreach (DailyPointValues value in tup.values) {
                    file.WriteLine (value.ToCsv ());
                }
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "OutputPointValuesAsCsv");
            }
            return tup.values;
        }

        private static List<decimal> OutputScale (this List<decimal> scale) {
            try {
                string filePath = $"{dirPath}{currentTicker}-scale.csv";
                using StreamWriter file = new StreamWriter (filePath);
                file.WriteLine ("Index,Point");
                int ctr = 0;
                foreach (decimal val in scale) {
                    file.WriteLine ($"{ctr++},{val:F2}");
                }
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "OutputScale");
            }
            return scale;
        }

        private static List<DailyStockValues> ReadStockFile (this PathInfo pi) {
            List<DailyStockValues> values = null;
            try {
                values = File.ReadAllLines (pi.FullPath)
                    .Skip (1)
                    .Select (v => DailyStockValues.FromCsv (v))
                    .ToList ();
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "ReadStockFile");
            }
            return values;
        }
    }
}
