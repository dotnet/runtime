// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Runtime.InteropServices;
#if !SYSTEM_NET_SOCKETS_DLL
using SocketType = System.Net.Internals.SocketType;
using ProtocolFamily = System.Net.Internals.ProtocolFamily;
#endif

namespace System.Net.Sockets
{
    // data structures and types needed for getaddrinfo calls.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal unsafe struct AddressInfo
    {
        internal AddressInfoHints ai_flags;
        internal AddressFamily ai_family;
        internal SocketType ai_socktype;
        internal ProtocolFamily ai_protocol;
        internal int ai_addrlen;
        internal sbyte* ai_canonname;   // Ptr to the canonical name - check for NULL
        internal byte* ai_addr;         // Ptr to the sockaddr structure
        internal AddressInfo* ai_next;  // Ptr to the next AddressInfo structure
    }
}
