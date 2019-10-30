// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public static class FakeRegistry
    {
        public static void Reset()
        {
            WinInetProxySettings.RegistryKeyMissing = false;

            WinInetProxySettings.AutoDetect = false;
            WinInetProxySettings.AutoConfigUrl = null;
            WinInetProxySettings.Proxy = null;
            WinInetProxySettings.ProxyBypass = null;
        }

        public static class WinInetProxySettings
        {
            public static bool RegistryKeyMissing { get; set; }

            public static bool AutoDetect { get; set; }

            public static string AutoConfigUrl { get; set; }

            public static string Proxy { get; set; }

            public static string ProxyBypass { get; set; }
        }
    }
}
