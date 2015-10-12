// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if SYSTEM_NET_SOCKETS_DLL
namespace System.Net.Sockets
#else
namespace System.Net.Internals
#endif
{
    // Specifies the type of socket an instance of the System.Net.Sockets.Socket class represents.
    public enum SocketType
    {
        Stream = 1, // stream socket
        Dgram = 2, // datagram socket
        Raw = 3, // raw-protocol interface
        Rdm = 4, // reliably-delivered message
        Seqpacket = 5, // sequenced packet stream
        Unknown = -1, // Unknown socket type
    }
}
