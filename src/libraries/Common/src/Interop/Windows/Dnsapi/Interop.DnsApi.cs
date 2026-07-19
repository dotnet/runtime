// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Dnsapi
    {
        // ---- Query types we use ----
        internal const ushort DNS_TYPE_A = 0x0001;
        internal const ushort DNS_TYPE_NS = 0x0002;
        internal const ushort DNS_TYPE_CNAME = 0x0005;
        internal const ushort DNS_TYPE_SOA = 0x0006;
        internal const ushort DNS_TYPE_PTR = 0x000c;
        internal const ushort DNS_TYPE_MX = 0x000f;
        internal const ushort DNS_TYPE_TEXT = 0x0010;
        internal const ushort DNS_TYPE_AAAA = 0x001c;
        internal const ushort DNS_TYPE_SRV = 0x0021;

        // ---- DnsQueryEx return codes / Win32 error codes ----
        internal const int DNS_REQUEST_PENDING = 9506;
        internal const int ERROR_SUCCESS = 0;
        internal const int DNS_INFO_NO_RECORDS = 9501;
        internal const int DNS_ERROR_RCODE_FORMAT_ERROR = 9001;
        internal const int DNS_ERROR_RCODE_SERVER_FAILURE = 9002;
        internal const int DNS_ERROR_RCODE_NAME_ERROR = 9003;
        internal const int DNS_ERROR_RCODE_NOT_IMPLEMENTED = 9004;
        internal const int DNS_ERROR_RCODE_REFUSED = 9005;

        // ---- DnsQueryEx options ----
        internal const ulong DNS_QUERY_STANDARD = 0x00000000;

        // ---- Query request versions ----
        internal const uint DNS_QUERY_REQUEST_VERSION1 = 0x1;

        // ---- DNS_ADDR address family marker — addresses are stored in SOCKADDR form ----
        internal const ushort AF_INET = 2;
        internal const ushort AF_INET6 = 23;

        // ---- DnsFreeType for DnsFree ----
        internal const int DnsFreeRecordList = 1;

        [LibraryImport(Libraries.Dnsapi, EntryPoint = "DnsQueryEx")]
        internal static unsafe partial int DnsQueryEx(
            DNS_QUERY_REQUEST* pQueryRequest,
            DNS_QUERY_RESULT* pQueryResults,
            DNS_QUERY_CANCEL* pCancelHandle);

        [LibraryImport(Libraries.Dnsapi, EntryPoint = "DnsCancelQuery")]
        internal static unsafe partial int DnsCancelQuery(DNS_QUERY_CANCEL* pCancelHandle);

        [LibraryImport(Libraries.Dnsapi, EntryPoint = "DnsFree")]
        internal static partial void DnsFree(IntPtr pData, int freeType);
    }
}
