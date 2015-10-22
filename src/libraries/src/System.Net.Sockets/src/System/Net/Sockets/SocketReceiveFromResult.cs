// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets
{
    public struct SocketReceiveFromResult
    {
        public int ReceivedBytes;
        public EndPoint RemoteEndPoint;
    }

    // Alternative:
    //    public struct SocketReceiveFromResult
    //    {
    //        public int ReceivedBytes;
    //        public IPAddress Address;
    //        public int Port;
    //    }
}
