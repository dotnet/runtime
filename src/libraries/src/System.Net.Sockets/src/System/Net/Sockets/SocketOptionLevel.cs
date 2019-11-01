// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // Defines socket option levels for the Socket class.
    public enum SocketOptionLevel
    {
        // Indicates socket options apply to the socket itself.
        Socket = 0xffff,

        // Indicates socket options apply to IP sockets.
        IP = ProtocolType.IP,

        // Indicates socket options apply to IPv6 sockets.
        IPv6 = ProtocolType.IPv6,

        // Indicates socket options apply to Tcp sockets.
        Tcp = ProtocolType.Tcp,

        // Indicates socket options apply to Udp sockets.
        Udp = ProtocolType.Udp,
    }
}
