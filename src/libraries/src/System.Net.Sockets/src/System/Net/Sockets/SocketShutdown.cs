// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
