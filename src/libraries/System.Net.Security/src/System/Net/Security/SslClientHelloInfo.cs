// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;

namespace System.Net.Security
{
    /// <summary>
    /// This struct contains information from received TLS Client Hello frame.
    /// </summary>
    public readonly struct SslClientHelloInfo
    {
        public readonly string ServerName { get; }
        public readonly SslProtocols SslProtocols { get; }

        internal SslClientHelloInfo(string serverName, SslProtocols sslProtocols)
        {
            ServerName = serverName;
            SslProtocols = sslProtocols;
        }
    }
}
