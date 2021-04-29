using System;
using System.Data;
using System.Linq;
using System.Management;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace OakSoft_MACSpoofer.Classes
{
   public class Adapter
    {
        public ManagementObject _adapter;
        public string _adapterName;
        public string _customName;
        public int _devnum;

        public NetworkInterface ManagedAdapter => NetworkInterface.GetAllNetworkInterfaces().Where(nic => nic.Description == _adapterName).FirstOrDefault();
        public string Mac
        {
            get
            {
                try
                {
                    return BitConverter.ToString(this.ManagedAdapter.GetPhysicalAddress().GetAddressBytes()).Replace("-", "").ToUpper();
                }
                catch { return null; }
            }
        }

        public string RegistryKey => string.Format(@"SYSTEM\ControlSet001\Control\Class\{{4D36E972-E325-11CE-BFC1-08002BE10318}}\{0:D4}", _devnum);

        public string RegistryMac
        {
            get
            {
                try
                {
                    using (RegistryKey regkey = Registry.LocalMachine.OpenSubKey(this.RegistryKey, false))
                    {
                        return regkey.GetValue("NetworkAddress").ToString();
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        public Adapter(ManagementObject adapter, string adapterName, string customName, int devNum)
        {
            _adapter = adapter;
            _adapterName = adapterName;
            _customName = customName;
            _devnum = devNum;
        }

        public Adapter(NetworkInterface i) : this(i.Description) { }

        public Adapter(string adapterName)
        {
            _adapterName = adapterName;

            var searcher = new ManagementObjectSearcher("select * from win32_networkadapter where Name='" + _adapterName + "'");
            var found = searcher.Get();
            _adapter = found.Cast<ManagementObject>().FirstOrDefault();

            // Extract adapter number; this should correspond to the keys under
            // HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}
            try
            {
                var match = Regex.Match(_adapter.Path.RelativePath, "\\\"(\\d+)\\\"$");
                _devnum = int.Parse(match.Groups[1].Value);
            }
            catch
            {
                return;
            }

            // Find the name the user gave to it in "Network Adapters"
            _customName = NetworkInterface.GetAllNetworkInterfaces().Where(i => i.Description == _adapterName).Select(i => " (" + i.Name + ")").FirstOrDefault();
        }

        public bool SetRegistryMac(string value)
        {
            bool shouldReenable = false;

            try
            {
                // If the value is not the empty string, we want to set NetworkAddress to it,
                // so it had better be valid
                if (value.Length > 0 && !Adapter.IsValidMac(value, false))
                {
                    throw new Exception(value + " is not a valid mac address");
                }
                
                using (RegistryKey regkey = Registry.LocalMachine.OpenSubKey(RegistryKey, true))
                {
                    if (regkey == null)
                    {
                        throw new Exception("Failed to open the registry key");
                    }

                    if (regkey.GetValue("AdapterModel") as string != _adapterName && regkey.GetValue("DriverDesc") as string != _adapterName)
                    {
                        throw new Exception("Adapter not found in registry");
                    }

                    // Attempt to disable the adepter
                    var result = (uint)_adapter.InvokeMethod("Disable", null);
                    if (result != 0)
                    {
                        throw new Exception("Failed to disable network adapter.");
                    }

                    // If we're here the adapter has been disabled, so we set the flag that will re-enable it in the finally block
                    shouldReenable = true;

                    // If we're here everything is OK; update or clear the registry value
                    if (value.Length > 0)
                    {
                        regkey.SetValue("NetworkAddress", value, RegistryValueKind.String);
                    }
                    else
                    {
                        regkey.DeleteValue("NetworkAddress");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                // log here.
                return false;
            }

            finally
            {
                if (shouldReenable)
                {
                    uint result = (uint)_adapter.InvokeMethod("Enable", null);
                    if (result != 0)
                    {
                        // log failed to enable.
                    }
                }
            }
        }

        public override string ToString()
        {
            return _adapterName + _customName;
        }

        public static string GetNewMac()
        {
            var random = new Random();
            byte[] bytes = new byte[6];
            random.NextBytes(bytes);
            // Set second bit to 1
            bytes[0] = (byte)(bytes[0] | 0x02);
            // Set first bit to 0
            bytes[0] = (byte)(bytes[0] & 0xfe);
            return MacToString(bytes);
        }

        public static bool IsValidMac(string mac, bool actual)
        {
            // 6 bytes == 12 hex characters (without dashes/dots/anything else)
            if (mac.Length != 12) return false;

            // Should be uppercase
            if (mac != mac.ToUpper()) return false;

            // Should not contain anything other than hexadecimal digits
            if (!Regex.IsMatch(mac, "^[0-9A-F]*$")) return false;

            if (actual) return true;

            // If we're here, then the second character should be a 2, 6, A or E
            char c = mac[1];
            return (c == '2' || c == '6' || c == 'A' || c == 'E');
        }

        public static bool IsValidMac(byte[] bytes, bool actual)
        {
            return IsValidMac(Adapter.MacToString(bytes), actual);
        }

        public static string MacToString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }
    }
}
