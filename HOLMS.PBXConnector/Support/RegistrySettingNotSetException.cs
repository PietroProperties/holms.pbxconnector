using System;

namespace HOLMS.PBXConnector.Support {
    public class RegistrySettingNotSetException : Exception {
        public RegistrySettingNotSetException(string ExpectedKey, string ExpectedValueName) :
            base( ExceptionMessage(ExpectedKey, ExpectedValueName)) { }
        private static string ExceptionMessage(string expectedKey, string expectedValueName) {
            return $"Service configuration setting in the registry at {expectedKey} with value name " +
            $"{expectedValueName} has not been set. Please configure this registry value before " +
            "starting the HOLMS application service runner.";
        }
    }
}
