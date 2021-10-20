// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal static class HttpUtilities
    {
        internal static bool IsSupportedScheme(string scheme) =>
            IsSupportedNonSecureScheme(scheme) ||
            IsSupportedSecureScheme(scheme);

        internal static bool IsSupportedNonSecureScheme(string scheme) =>
            string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) || IsNonSecureWebSocketScheme(scheme);

        internal static bool IsSupportedSecureScheme(string scheme) =>
            string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) || IsSecureWebSocketScheme(scheme);

        internal static bool IsNonSecureWebSocketScheme(string scheme) =>
            string.Equals(scheme, "ws", StringComparison.OrdinalIgnoreCase);

        internal static bool IsSecureWebSocketScheme(string scheme) =>
            string.Equals(scheme, "wss", StringComparison.OrdinalIgnoreCase);

        internal static bool IsSupportedProxyScheme(string scheme) =>
            string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) || IsSocksScheme(scheme);

        internal static bool IsSocksScheme(string scheme) =>
            string.Equals(scheme, "socks5", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scheme, "socks4a", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scheme, "socks4", StringComparison.OrdinalIgnoreCase);
    }
}
