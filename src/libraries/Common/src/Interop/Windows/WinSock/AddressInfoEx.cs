// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Internals;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AddressInfoEx
    {
        internal AddressInfoHints ai_flags;
        internal AddressFamily ai_family;
        internal SocketType ai_socktype;
        internal ProtocolFamily ai_protocol;
        internal int ai_addrlen;
        internal IntPtr ai_canonname;   // Ptr to the canonical name - check for NULL
        internal byte* ai_addr;         // Ptr to the sockaddr structure
        internal IntPtr ai_blob;         // Unused ptr to blob data about provider
        internal int ai_bloblen;
        internal IntPtr ai_provider; // Unused ptr to the namespace provider guid
        internal AddressInfoEx* ai_next;        // Next structure in linked list
    }
}
