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
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
#pragma warning restore SYSLIB0039

        public const SslProtocols SystemDefaultSecurityProtocols = SslProtocols.None;
    }
}
