// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http
{
    internal enum HttpConnectionKind : byte
    {
        Http,
        Https,
        Proxy,
        ProxyTunnel,        // Non-secure connection tunneled through proxy.
        SslProxyTunnel,     // SSL connection tunneled through proxy.
        ProxyConnect        // Connection used for proxy CONNECT. Tunnel will be established on top of this.
    }
}
