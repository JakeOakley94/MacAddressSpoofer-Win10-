using OakSoft_MACSpoofer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Timers;

namespace OakSoft_MACSpoofer
{
    public class Program
    {
        public static Timer timer;

        static void Main(string[] args)
        {
            Logger.CheckLogPath();
            Logger.AppendToLogInfo("Starting Spoofer Service");
            SetupTimer();
            SpoofMac();
        }

        private static void SetupTimer()
        {
            timer = new Timer();
            timer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
            timer.Enabled = true;
            timer.Elapsed += Timer_Elapsed;
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.CheckLogPath();
            Logger.AppendToLogInfo("Starting Spoofing Process");
            SpoofMac();
            timer.Stop();
            timer.Start();
        }

        private static void SpoofMac()
        {
            var adapters = GetNetworkAdapters();

            foreach (var adapter in adapters)
            {
                adapter.SetRegistryMac(Adapter.GetNewMac());
            }
        }

        private static List<Adapter> GetNetworkAdapters()
        {
            List<Adapter> toReturn = new List<Adapter>();

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces().Where(a => Adapter.IsValidMac(a.GetPhysicalAddress().GetAddressBytes(), true)).OrderByDescending(a => a.Speed))
            {
                toReturn.Add(new Adapter(adapter));
            }

            return toReturn;
        }


    }
}
