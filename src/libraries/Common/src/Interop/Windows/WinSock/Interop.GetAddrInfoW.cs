// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        [DllImport(Interop.Libraries.Ws2_32, ExactSpelling = true, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        internal static extern unsafe int GetAddrInfoW(
            [In] string pNameName,
            [In] string? pServiceName,
            [In] AddressInfo* pHints,
            [Out] AddressInfo** ppResult);

        [DllImport(Interop.Libraries.Ws2_32, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe void FreeAddrInfoW(AddressInfo* info);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal unsafe struct AddressInfo
        {
            internal AddressInfoHints ai_flags;
            internal AddressFamily ai_family;
            internal int ai_socktype;
            internal int ai_protocol;
            internal IntPtr ai_addrlen;
            internal sbyte* ai_canonname;   // Ptr to the canonical name - check for NULL
            internal byte* ai_addr;         // Ptr to the sockaddr structure
            internal AddressInfo* ai_next;  // Ptr to the next AddressInfo structure
        }
    }
}
