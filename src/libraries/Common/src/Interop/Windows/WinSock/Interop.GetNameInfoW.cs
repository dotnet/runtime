// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [Flags]
        internal enum NameInfoFlags
        {
            NI_NOFQDN = 0x01, /* Only return nodename portion for local hosts */
            NI_NUMERICHOST = 0x02, /* Return numeric form of the host's address */
            NI_NAMEREQD = 0x04, /* Error if the host's name not in DNS */
            NI_NUMERICSERV = 0x08, /* Return numeric form of the service (port #) */
            NI_DGRAM = 0x10, /* Service is a datagram service */
        }

        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial SocketError GetNameInfoW(
            byte* pSockaddr,
            int SockaddrLength,
            char* pNodeBuffer,
            int NodeBufferSize,
            char* pServiceBuffer,
            int ServiceBufferSize,
            int Flags);
    }
}
