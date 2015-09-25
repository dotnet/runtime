// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    // Defines the transport type allowed for the socket.
    public enum TransportType
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
