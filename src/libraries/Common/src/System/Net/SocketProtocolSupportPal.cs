// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net
{
    internal static partial class SocketProtocolSupportPal
    {
        private const string DisableIPv6AppCtxSwitch = "System.Net.DisableIPv6";
        private const string DisableIPv6EnvironmentVariable = "DOTNET_SYSTEM_NET_DISABLEIPV6";

        public static bool OSSupportsIPv6 { get; } = IsSupported(AddressFamily.InterNetworkV6) && !IsIPv6Disabled();
        public static bool OSSupportsIPv4 { get; } = IsSupported(AddressFamily.InterNetwork);
        public static bool OSSupportsUnixDomainSockets { get; } = IsSupported(AddressFamily.Unix);

        private static bool IsIPv6Disabled()
        {
            // First check for the AppContext switch, giving it priority over the environment variable.
            if (AppContext.TryGetSwitch(DisableIPv6AppCtxSwitch, out bool disabled))
            {
                return disabled;
            }

            // AppContext switch wasn't used. Check the environment variable.
            string? envVar = Environment.GetEnvironmentVariable(DisableIPv6EnvironmentVariable);

            if (envVar is not null)
            {
                return envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
