using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PFCharter {
    static class PFCharter {

        private static bool isDir = false;

        static void Main (string [] args) {
            if (args.Length == 0) {
                Utils.WriteToConsole ("Usage:\n\nPFCharter.exe [directory]|[file name and path]", true, "Main");
                Environment.Exit (1);
            }
            string passedValue = args [0];
            string dirName = passedValue.GetDirName ();
            if (isDir) {

            }
            else {
                if (dirName == string.Empty) {

                }
                else {
                    /*FileInfo fi = new FileInfo (passedValue);
                    string fileName = fi.FullName;
                    var values = ReadStockFile (fileName);*/
                    var values = new FileInfo (passedValue)
                        .FullName
                        .ReadStockFile ();
                    Utils.WriteToConsole ($"{values.Count} prices found.", false, "Main");
                    values.GetHighLowAndIncrement ();
                }
            }
        }

        private static string GetDirName (this string dirName) {
            try {
                if (Directory.Exists (dirName)) {
                    isDir = true;
                    return dirName;
                }
                else {
                    isDir = false;
                    var fi = new FileInfo (dirName);
                    return fi.DirectoryName;
                }
            }
            catch (Exception ex) {
                Utils.WriteToConsole (Utils.ExToString (ex), true, "GetDirName");
                isDir = false;
                return string.Empty;
            }
        }

        private static void GetHighLowAndIncrement (this List<DailyStockValues> values) {
            decimal high = 0, low = 0;
            foreach (DailyStockValues value in values) {
                if (value.High > high) high = value.High;
                if (value.Low < high) low = value.Low;
            }

            List<decimal> scale = new List<decimal> ();
            decimal scaleHigh = high * 1.01m;
            scale.Add (scaleHigh);
            decimal ctr = high;
            while (ctr >= low) {
                scale.Add (ctr);
                ctr -= (ctr * 0.01m);
            }
            scale.Add (ctr);

            /*foreach (var ele in scale) {
                Console.WriteLine ($"{ele:F2}");
            }*/
        }

        private static List<DailyStockValues> ReadStockFile (this string fileName) {
            List<DailyStockValues> values = null;
            try {
                values = File.ReadAllLines (fileName)
                    .Skip (1)
                    .Select (v => DailyStockValues.FromCsv (v))
                    .ToList ();
            }
            catch (Exception ex) {

            }
            return values;
        }
    }
}
