// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

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

        [DllImport(Interop.Libraries.Ws2_32, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        internal static extern SocketError GetNameInfoW(
            [In]         byte[] sa,
            [In]         int salen,
            [In, Out]     StringBuilder host,
            [In]         int hostlen,
            [In, Out]     StringBuilder serv,
            [In]         int servlen,
            [In]         int flags);
    }
}
