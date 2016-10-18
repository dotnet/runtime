// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;

namespace System.Net
{
    internal static class SecurityProtocol
    {
        // SSLv2 and SSLv3 are considered insecure and will not be supported by the underlying implementations.
        public const SslProtocols AllowedSecurityProtocols =
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        public const SslProtocols DefaultSecurityProtocols =
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        public const SslProtocols SystemDefaultSecurityProtocols = SslProtocols.None;

        public static void ThrowOnNotAllowed(SslProtocols protocols, bool allowNone = true)
        {
            if ((!allowNone && (protocols == SslProtocols.None)) || ((protocols & ~AllowedSecurityProtocols) != 0))
            {
                throw new NotSupportedException(SR.net_securityprotocolnotsupported);
            }
        }
    }
}
