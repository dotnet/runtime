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

        internal const int NUM_BYTES_IN_IPV6_ADDRESS = 16;
        internal const int MAX_IP_ADDRESS_BYTES = 16;

        internal enum GetAddrInfoErrorFlags : int
        {
            EAI_AGAIN = 1,      // Temporary failure in name resolution.
            EAI_BADFLAGS = 2,   // Invalid value for `ai_flags' field.
            EAI_FAIL = 3,       // Non-recoverable failure in name resolution.
            EAI_FAMILY = 4,     // 'ai_family' not supported.
            EAI_NONAME = 5,     // NAME or SERVICE is unknown.
            EAI_BADARG = 6,     // One or more input arguments were invalid.
            EAI_NOMORE = 7,     // No more entries are present in the list.
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
        internal unsafe struct IPAddress
        {
            internal fixed byte Address[MAX_IP_ADDRESS_BYTES]; // Buffer to fit an IPv4 or IPv6 address
            internal uint IsIPv6;                              // Non-zero if this is an IPv6 address; zero for IPv4.
            internal uint ScopeId;                             // Scope ID (IPv6 only)
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct HostEntry
        {
            internal byte* CanonicalName;     // Canonical Name of the Host
            internal void* AddressListHandle; // Handle for socket address list
            internal int IPAddressCount;      // Number of IP addresses in the list
            private int Padding;              // Pad out to 8-byte alignment
        }

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe int GetHostEntryForName(string address, HostEntry* entry);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe int GetNextIPAddress(void** addressListHandle, IPAddress* endPoint);

        [DllImport(Libraries.SystemNative)]
        internal static extern unsafe void FreeHostEntry(HostEntry* entry);
    }
}
