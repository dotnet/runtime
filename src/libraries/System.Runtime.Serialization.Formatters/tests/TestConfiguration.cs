// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

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
            Assembly assembly = typeof(BinaryFormatter).Assembly;
            AssemblyName name = assembly.GetName();
            Version assemblyVersion = name.Version;

            if (!PlatformDetection.IsBinaryFormatterSupported)
            {
                Console.WriteLine("BinaryFormatter is disabled by the platform");
                return;
            }

            if (PlatformDetection.IsNetFramework)
            {
                IsBinaryFormatterSupported = IsBinaryFormatterEnabled = true;
                Console.WriteLine("BinaryFormatter is always enabled by the platform (netfx).");
                return;
            }

            // Version 8.1 is the version in the shared runtime (.NET 9+) that has the type disabled with no config.
            // Assembly versions beyond 8.1 are the fully functional version from NuGet.
            // Assembly versions before 8.1 probably won't be encountered by this test library.

            if (assemblyVersion.Major == 8 && assemblyVersion.Minor == 1)
            {
                IsBinaryFormatterEnabled = false;
            }
            else
            {
                IsBinaryFormatterSupported = IsBinaryFormatterEnabled = true;

                IsFeatureSwitchRespected = true;

                if (!AppContext.TryGetSwitch(EnableBinaryFormatterSwitchName, out IsBinaryFormatterEnabled))
                {
                    IsBinaryFormatterEnabled = false;
                }
            }

            Console.WriteLine($"BinaryFormatter is from assembly version {assemblyVersion}, enabled={IsBinaryFormatterEnabled} supported={IsBinaryFormatterSupported}");
        }
    }
}
