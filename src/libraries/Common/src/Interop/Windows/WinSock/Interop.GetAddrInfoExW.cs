// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        internal const int WSA_INVALID_HANDLE = 6;
        internal const int WSA_E_CANCELLED = 10111;

        internal const string GetAddrInfoExCancelFunctionName = "GetAddrInfoExCancel";

        internal const int NS_ALL = 0;

        [GeneratedDllImport(Libraries.Ws2_32, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static unsafe partial int GetAddrInfoExW(
            string pName,
            string? pServiceName,
            int dwNamespace,
            IntPtr lpNspId,
            AddressInfoEx* pHints,
            AddressInfoEx** ppResult,
            IntPtr timeout,
            NativeOverlapped* lpOverlapped,
            delegate* unmanaged<int, int, NativeOverlapped*, void> lpCompletionRoutine,
            IntPtr* lpNameHandle);

        [GeneratedDllImport(Libraries.Ws2_32, ExactSpelling = true)]
        internal static unsafe partial int GetAddrInfoExCancel(IntPtr* lpHandle);

        [GeneratedDllImport(Libraries.Ws2_32, ExactSpelling = true)]
        internal static unsafe partial void FreeAddrInfoExW(AddressInfoEx* pAddrInfo);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct AddressInfoEx
        {
            internal AddressInfoHints ai_flags;
            internal AddressFamily ai_family;
            internal int ai_socktype;
            internal int ai_protocol;
            internal IntPtr ai_addrlen;
            internal IntPtr ai_canonname;    // Ptr to the canonical name - check for NULL
            internal byte* ai_addr;          // Ptr to the sockaddr structure
            internal IntPtr ai_blob;         // Unused ptr to blob data about provider
            internal IntPtr ai_bloblen;
            internal IntPtr ai_provider;     // Unused ptr to the namespace provider guid
            internal AddressInfoEx* ai_next; // Next structure in linked list
        }
    }
}
