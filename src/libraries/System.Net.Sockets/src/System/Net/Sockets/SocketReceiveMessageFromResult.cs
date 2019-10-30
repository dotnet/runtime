// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // Review note: RemoteEndPoint definition includes the Address and Port.
    // PacketInformation includes Address and Interface (physical interface number).
    // The redundancy could be removed by replacing RemoteEndPoint with Port.

    public struct SocketReceiveMessageFromResult
    {
        public int ReceivedBytes;
        public SocketFlags SocketFlags;
        public EndPoint RemoteEndPoint;
        public IPPacketInformation PacketInformation;
    }

    // Alternative:
    //    public struct SocketReceiveMessageFromResult
    //    {
    //        public int ReceivedBytes;
    //        public SocketFlags SocketFlags;
    //        public IPAddress Address;
    //        public int Port;
    //        public int Interface;
    //    }
}
