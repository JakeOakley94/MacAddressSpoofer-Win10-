using System;
using System.IO;

namespace OakSoft_MACSpoofer.Classes
{
    public static class Logger
    {
        public static readonly string logDirectory = $@"{Environment.SpecialFolder.LocalApplicationData}\MAC\Log";
        public static readonly string logPath = $@"{logDirectory}\log.txt";
        public static bool hasCheckedDirectory = false;

        public static void CheckLogPath()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            if (!File.Exists(logPath))
            {
                File.Create(logPath).Close();
            }
            hasCheckedDirectory = true;
        }

        public static void AppendToLog(string message)
        {
            using (StreamWriter sw = new StreamWriter(logPath, true))
            {
                sw.WriteLine($"{DateTime.Now}: The following erorr occurred: {message}");
            }
        }

        public static void AppendToLogInfo(string message)
        {
            using (StreamWriter sw = new StreamWriter(logPath, true))
            {
                sw.WriteLine($"{DateTime.Now}: {message}");
            }
        }
    }
}
