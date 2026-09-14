// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    internal enum HttpConnectionKind : byte
    {
        Http,               // Non-secure connection with no proxy.
        Https,              // Secure connection with no proxy.
        Proxy,              // HTTP proxy usage for non-secure (HTTP) requests using HTTP/1.1.
        ProxyTunnel,        // Non-secure (HTTP/WS) connection using CONNECT tunneling through proxy. Used for cleartext HTTP/2 (h2c) and non-secure WebSockets.
        SslProxyTunnel,     // HTTP proxy usage for secure (HTTPS/WSS) requests using SSL and proxy CONNECT.
        ProxyConnect,       // Connection used for proxy CONNECT. Tunnel will be established on top of this.
        SocksTunnel,        // SOCKS proxy usage for HTTP requests.
        SslSocksTunnel      // SOCKS proxy usage for HTTPS requests.
    }
}
