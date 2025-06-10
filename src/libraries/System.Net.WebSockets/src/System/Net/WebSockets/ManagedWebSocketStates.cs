// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.WebSockets
{
    [Flags]
    internal enum ManagedWebSocketStates
    {
        None = 0,

        // WebSocketState.None = 0       -- this state is invalid for the managed implementation
        // WebSocketState.Connecting = 1 -- this state is invalid for the managed implementation
        Open = 0x04,           // WebSocketState.Open = 2
        CloseSent = 0x08,      // WebSocketState.CloseSent = 3
        CloseReceived = 0x10,  // WebSocketState.CloseReceived = 4
        Closed = 0x20,         // WebSocketState.Closed = 5
        Aborted = 0x40,        // WebSocketState.Aborted = 6

        All = Open | CloseSent | CloseReceived | Closed | Aborted
    }
}
