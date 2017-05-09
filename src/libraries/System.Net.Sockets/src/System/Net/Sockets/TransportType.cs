// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    // Defines the transport type allowed for the socket.
    internal enum TransportType
    {
        // Udp connections are allowed.
        Udp = 1,
        Connectionless = Udp,

        // TCP connections are allowed.
        Tcp = 2,
        ConnectionOriented = Tcp,

        // Any connection is allowed.
        All = 3
    }
}
