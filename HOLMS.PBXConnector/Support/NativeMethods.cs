using System;
using Microsoft.Win32;

namespace HOLMS.PBXConnector.Support {
    internal class NativeMethods {

        private const string KeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\HOLMS\PBXConnector";

        //Just for PBX Connector, we ignore NotSet fields
        public static string GetStringRegistryEntry(string valuename) {
            var keyvalue = (string)Registry.GetValue(KeyPath, valuename, null);
            if (keyvalue == null) {
                throw new RegistrySettingNotFoundException(KeyPath, valuename);
            }
            return keyvalue;
        }

        public static bool GetDWordRegistryEntry(string valuename) {
            var keyvalue = Registry.GetValue(KeyPath, valuename, null);
            if (keyvalue == null) {
                throw new RegistrySettingNotFoundException(KeyPath, valuename);
            }
            return (int)keyvalue != 0;
        }

        public static Guid GetWindowsMachineGuid() {
            // With gratitute to http://www.nextofwindows.com/the-best-way-to-uniquely-identify-a-windows-machine
            // This will be the same for every user, and won't change until/unless windows is reinstalled
            var mgstr = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography",
                "MachineGuid", null);
            return new Guid(mgstr);
        }
    }
}
