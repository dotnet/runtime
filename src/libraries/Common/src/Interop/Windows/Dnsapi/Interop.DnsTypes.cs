// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Dnsapi
    {
        // DNS_QUERY_REQUEST (v1) — Win8 / Server 2012+
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_QUERY_REQUEST
        {
            public uint Version;
            public IntPtr QueryName;    // PCWSTR
            public ushort QueryType;
            public ulong QueryOptions;
            public DNS_ADDR_ARRAY* pDnsServerList;
            public uint InterfaceIndex;
            public IntPtr pQueryCompletionCallback; // PDNS_QUERY_COMPLETION_ROUTINE
            public IntPtr pQueryContext;
        }

        // DNS_QUERY_REQUEST3 — Win11 Build 22000+
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_QUERY_REQUEST3
        {
            public uint Version;
            public IntPtr QueryName;
            public ushort QueryType;
            public ulong QueryOptions;
            public DNS_ADDR_ARRAY* pDnsServerList;
            public uint InterfaceIndex;
            public IntPtr pQueryCompletionCallback;
            public IntPtr pQueryContext;
            public int IsNetworkQueryRequired;   // BOOL
            public uint RequiredNetworkIndex;
            public uint cCustomServers;
            public DNS_CUSTOM_SERVER* pCustomServers;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_QUERY_RESULT
        {
            public uint Version;
            public int QueryStatus;
            public ulong QueryOptions;
            public IntPtr pQueryRecords;     // DNS_RECORD*
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_QUERY_CANCEL
        {
            public fixed byte Reserved[32];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_ADDR
        {
            // SOCKET_ADDRESS-like: 32 bytes of SOCKADDR_STORAGE-ish + extras.
            // DnsApi documents this struct as 64 bytes total with the first 32
            // being the SOCKADDR (IPv4/IPv6 SOCKADDR fits within).
            public fixed byte MaxSa[32];
            public uint DnsAddrUserDword0;
            public uint DnsAddrUserDword1;
            public uint DnsAddrUserDword2;
            public uint DnsAddrUserDword3;
            public uint DnsAddrUserDword4;
            public uint DnsAddrUserDword5;
            public uint DnsAddrUserDword6;
            public uint DnsAddrUserDword7;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_ADDR_ARRAY
        {
            public uint MaxCount;
            public uint AddrCount;
            public uint Tag;
            public ushort Family;
            public ushort WordReserved;
            public uint Flags;
            public uint MatchFlag;
            public uint Reserved1;
            public uint Reserved2;
            // followed by AddrCount entries of DNS_ADDR
            // (we allocate the trailing array contiguously)
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_CUSTOM_SERVER
        {
            public uint dwServerType;       // DNS_CUSTOM_SERVER_TYPE_*
            public ulong ullFlags;
            public IntPtr pwszTemplate;     // PCWSTR (DoH only)
            public fixed byte ServerAddr[32]; // SOCKADDR
        }

        // ---- DNS_RECORD (variable layout: header + Data union) ----
        // We declare the fixed header layout and read the data area as a byte blob,
        // re-interpreting per record type. The Data field begins at offset 24 on 32-bit
        // and at offset 24 on 64-bit pointer layouts as documented; we use a
        // Sequential struct with explicit Next/pName pointers.
        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_RECORD_HEADER
        {
            public IntPtr pNext;        // DNS_RECORD*
            public IntPtr pName;        // PCWSTR
            public ushort wType;
            public ushort wDataLength;  // not always reliable; use type to interpret
            public uint Flags;          // contains Section in the low bits
            public uint dwTtl;
            public uint dwReserved;
            // followed by Data union
        }

        // ---- Section field within DNS_RECORD.Flags ----
        // The Section is the lowest 2 bits of the DW_FLAGS field.
        internal const uint DNSREC_SECTION_MASK = 0x3;
        internal const uint DNSREC_QUESTION = 0;
        internal const uint DNSREC_ANSWER = 1;
        internal const uint DNSREC_AUTHORITY = 2;
        internal const uint DNSREC_ADDITIONAL = 3;

        // ---- Data unions ----
        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_A_DATA
        {
            public uint IpAddress; // network byte order
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_AAAA_DATA
        {
            public Ip6AddressBytes Ip6Address;
        }

        [System.Runtime.CompilerServices.InlineArray(16)]
        internal struct Ip6AddressBytes
        {
            private byte _element0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_PTR_DATA
        {
            public IntPtr pNameHost; // PCWSTR
        }

        // Same shape as DNS_PTR_DATA — Windows uses DNS_PTR_DATA for NS/CNAME too,
        // but typed aliases keep call sites self-documenting.
#pragma warning disable CS0649 // fields populated via native marshalling
        internal struct DNS_CNAME_DATA
        {
            public IntPtr pNameHost;
        }

        internal struct DNS_NS_DATA
        {
            public IntPtr pNameHost;
        }
#pragma warning restore CS0649

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_MX_DATA
        {
            public IntPtr pNameExchange; // PCWSTR
            public ushort wPreference;
            public ushort Pad;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_SRV_DATA
        {
            public IntPtr pNameTarget; // PCWSTR
            public ushort wPriority;
            public ushort wWeight;
            public ushort wPort;
            public ushort Pad;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct DNS_TXT_DATA
        {
            public uint dwStringCount;
            // followed by dwStringCount entries of PCWSTR
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DNS_SOA_DATA
        {
            public IntPtr pNamePrimaryServer;   // PCWSTR
            public IntPtr pNameAdministrator;   // PCWSTR
            public uint dwSerialNo;
            public uint dwRefresh;
            public uint dwRetry;
            public uint dwExpire;
            public uint dwDefaultTtl;
        }
    }
}
