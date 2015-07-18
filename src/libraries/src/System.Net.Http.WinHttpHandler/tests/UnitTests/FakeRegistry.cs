// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
