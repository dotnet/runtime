// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
