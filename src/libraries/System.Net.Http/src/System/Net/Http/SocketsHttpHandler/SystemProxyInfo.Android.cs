// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    internal static partial class SystemProxyInfo
    {
        public static IWebProxy ConstructSystemProxy()
        {
            // 1. Honor HTTP_PROXY / HTTPS_PROXY / NO_PROXY env vars first.
            //    This matches the Unix convention and lets dev/test flows
            //    (e.g. Charles/Fiddler) keep working unchanged.
            if (HttpEnvironmentProxy.TryCreate(out IWebProxy? envProxy))
            {
                return envProxy;
            }

            // 2. Defer to the Android platform proxy via java.net.ProxySelector.
            //    Wi-Fi proxy, MDM-deployed proxy, PAC scripts, and per-network
            //    or VPN-attached ProxyInfo are all surfaced through this path.
            if (UseAndroidSystemProxy)
            {
                return new AndroidPlatformProxy();
            }

            return new HttpNoProxy();
        }

        [FeatureSwitchDefinition("System.Net.Http.UseAndroidSystemProxy")]
        private static bool UseAndroidSystemProxy =>
            RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.Http.UseAndroidSystemProxy",
                defaultValue: true);
    }
}
