// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal const int NI_MAXHOST = 1025;
        internal const int NI_MAXSERV = 32;

        internal enum GetAddrInfoErrorFlags : int
        {
            EAI_AGAIN = 1,      // Temporary failure in name resolution.
            EAI_BADFLAGS = 2,   // Invalid value for `ai_flags' field.
            EAI_FAIL = 3,       // Non-recoverable failure in name resolution.
            EAI_FAMILY = 4,     // 'ai_family' not supported.
            EAI_NONAME = 5,     // NAME or SERVICE is unknown.
        }

        internal enum GetHostErrorCodes : int
        {
            HOST_NOT_FOUND = 1,
            TRY_AGAIN = 2,
            NO_RECOVERY = 3,
            NO_DATA = 4,
            NO_ADDRESS = NO_DATA,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct PalIpAddress
        {
            internal byte* Address;     // Buffer to fit IPv4 or IPv6 address
            internal int Count;         // Number of bytes in the address buffer
            internal bool IsIpv6;       // If this is an IPv6 Address or IPv4
            private fixed byte padding[3];
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HostEntry
        {
            internal byte* CanonicalName;       // Canonical Name of the Host
            internal PalIpAddress* Addresses;   // List of IP Addresses associated with this host
            internal int Count;                 // Number of IP Addresses associated with this host
            private int reserved;
        }

        [DllImport(Libraries.SystemNative)]
        internal static unsafe extern int GetHostEntriesForName(string address, HostEntry** entry);

        [DllImport(Libraries.SystemNative)]
        internal static unsafe extern void FreeHostEntriesForName(HostEntry* entry);
    }
}
