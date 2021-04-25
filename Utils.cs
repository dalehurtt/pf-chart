using System;

namespace Charts
{
    public static class Utils {

        public static string ExToString (Exception ex) {
            return $"{ex.Message} @ {ex.StackTrace}";
        }

        public static void WriteToConsole (string msg, bool error, string caller) {
            if (error) Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine ($"{caller}: {msg}");
            if (error) Console.ResetColor ();
        }
    }
}
