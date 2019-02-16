using System;

namespace ExtractDiff.Core
{
    public class Logger
    {
        public static void LogInformation(string message)
        {
            Console.WriteLine($"{DateTime.UtcNow:ddMMyyyy HH:mm}: INFO: {message}");
        }
        public static void LogError(string message, Exception e)
        {
            Console.WriteLine($"{DateTime.UtcNow:ddMMyyyy HH:mm}: ERROR: {message}");
            Console.WriteLine($"{DateTime.UtcNow:ddMMyyyy HH:mm}: ERROR: {e}");
        }
    }
}
