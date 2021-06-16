// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;

namespace System.Net
{
    internal static class SecurityProtocol
    {
        public const SslProtocols DefaultSecurityProtocols =
#if !NETSTANDARD2_0 && !NETSTANDARD2_1 && !NETFRAMEWORK
            SslProtocols.Tls13 |
#endif
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        public const SslProtocols SystemDefaultSecurityProtocols = SslProtocols.None;
    }
}
