// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets
{
    // Defines constants used by the Socket.Shutdown method.
    public enum SocketShutdown
    {
        // Shutdown sockets for receive.
        Receive = 0x00,

        // Shutdown socket for send.
        Send = 0x01,

        // Shutdown socket for both send and receive.
        Both = 0x02,
    }
}
