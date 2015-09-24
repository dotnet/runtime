// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets
{
    public enum SocketAsyncOperation
    {
        None = 0,
        Accept,
        Connect,
        Disconnect,
        Receive,
        ReceiveFrom,
        ReceiveMessageFrom,
        Send,
        SendPackets,
        SendTo
    }
}
