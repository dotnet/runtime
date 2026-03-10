// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net
{
    internal static partial class SocketProtocolSupportPal
    {
        public static bool OSSupportsIPv6 { get; } = IsSupported(AddressFamily.InterNetworkV6) && !LocalAppContextSwitches.DisableIPv6;
        public static bool OSSupportsIPv4 { get; } = IsSupported(AddressFamily.InterNetwork);
        public static bool OSSupportsUnixDomainSockets { get; } = IsSupported(AddressFamily.Unix);
    }
}
