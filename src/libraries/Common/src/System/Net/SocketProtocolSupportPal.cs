// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net
{
    internal static partial class SocketProtocolSupportPal
    {
        public static bool OSSupportsIPv6 { get; } = !IsIPV6Disabled && IsSupported(AddressFamily.InterNetworkV6);
        public static bool OSSupportsIPv4 { get; } = IsSupported(AddressFamily.InterNetwork);
        public static bool OSSupportsUnixDomainSockets { get; } = IsSupported(AddressFamily.Unix);

        private static bool IsIPV6Disabled
        {
            get
            {
                const string DisableIpv6AppContextSettingName = "System.Net.Sockets.DisableIPv6";
                const string DisableIpv6EnvironmentVariableName = "DOTNET_SYSTEM_NET_SOCKETS_DISABLEIPV6";

                if (AppContext.TryGetSwitch(DisableIpv6AppContextSettingName, out bool disableIpV6))
                {
                    return disableIpV6;
                }

                string? envVar = Environment.GetEnvironmentVariable(DisableIpv6EnvironmentVariableName);
                return envVar != null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1"));
            }
        }
    }
}
