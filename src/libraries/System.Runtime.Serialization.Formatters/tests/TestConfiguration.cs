// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class TestConfiguration
    {
        internal const string EnableBinaryFormatterSwitchName = "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";

        public static readonly bool IsBinaryFormatterSupported;
        public static readonly bool IsBinaryFormatterEnabled;
        public static readonly bool IsFeatureSwitchRespected;

        static TestConfiguration()
        {
            if (!PlatformDetection.IsBinaryFormatterSupported)
            {
                return;
            }

            IsBinaryFormatterSupported = true;
            IsBinaryFormatterEnabled = true;

            if (PlatformDetection.IsNetFramework)
            {
                Console.WriteLine("BinaryFormatter is always enabled by the platform (netfx).");
                return;
            }

            IsFeatureSwitchRespected = true;

            if (!AppContext.TryGetSwitch(EnableBinaryFormatterSwitchName, out IsBinaryFormatterEnabled))
            {
                IsBinaryFormatterEnabled = false;
            }

            Console.WriteLine($"BinaryFormatter is enabled in AppConfig: {IsBinaryFormatterEnabled}");
        }
    }
}
