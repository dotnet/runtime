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

        // Feature switch: System.Net.Http.UseAndroidSystemProxy
        //
        // Defaults to true. Apps may opt out (set to "false" via
        // <RuntimeHostConfigurationOption>) for the following reasons:
        //
        //   * Back-compat with apps that previously ran on SocketsHttpHandler
        //     (i.e. UseNativeHttpHandler=false) and got env-var-only proxy
        //     behavior. Suddenly honoring the system proxy could change the
        //     destination of their HTTP requests; this switch is the escape
        //     hatch.
        //   * Trimming: when set to false at publish time, the linker can
        //     trim the AndroidPlatformProxy class, the Interop P/Invoke
        //     declarations, and (transitively) leave the native pal_proxy
        //     code referenced only by other paths.
        //   * Testing / debugging where pure env-var behavior is required.
        [FeatureSwitchDefinition("System.Net.Http.UseAndroidSystemProxy")]
        private static bool UseAndroidSystemProxy { get; } =
            RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.Http.UseAndroidSystemProxy",
                defaultValue: true);
    }
}
