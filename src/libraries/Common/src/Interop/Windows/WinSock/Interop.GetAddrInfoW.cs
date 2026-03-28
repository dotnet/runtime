// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int GetAddrInfoW(
            string pNameName,
            string? pServiceName,
            AddressInfo* pHints,
            AddressInfo** ppResult);

        [RequiresUnsafe]
        [LibraryImport(Interop.Libraries.Ws2_32, SetLastError = true)]
        internal static unsafe partial void FreeAddrInfoW(AddressInfo* info);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal unsafe struct AddressInfo
        {
            internal AddressInfoHints ai_flags;
            internal AddressFamily ai_family;
            internal int ai_socktype;
            internal int ai_protocol;
            internal nuint ai_addrlen;
            internal sbyte* ai_canonname;   // Ptr to the canonical name - check for NULL
            internal byte* ai_addr;         // Ptr to the sockaddr structure
            internal AddressInfo* ai_next;  // Ptr to the next AddressInfo structure
        }
    }
}
