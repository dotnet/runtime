// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if SYSTEM_NET_SOCKETS_DLL
namespace System.Net.Sockets
{
    public
#else
namespace System.Net.Internals
{
    internal
#endif
    // Specifies the type of socket an instance of the System.Net.Sockets.Socket class represents.
    enum SocketType
    {
        Stream = 1, // stream socket
        Dgram = 2, // datagram socket
        Raw = 3, // raw-protocol interface
        Rdm = 4, // reliably-delivered message
        Seqpacket = 5, // sequenced packet stream
        Unknown = -1, // Unknown socket type
    }
}
