// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Dnssd
    {
        internal const uint kDNSServiceFlagsMoreComing = 0x1;
        internal const uint kDNSServiceFlagsAdd = 0x2;
        internal const uint kDNSServiceFlagsReturnIntermediates = 0x1000;
        internal const uint kDNSServiceFlagsTimeout = 0x10000;

        internal const int kDNSServiceErr_NoError = 0;
        internal const int kDNSServiceErr_Unknown = -65537;
        internal const int kDNSServiceErr_NoSuchName = -65538;
        internal const int kDNSServiceErr_NoMemory = -65539;
        internal const int kDNSServiceErr_BadParam = -65540;
        internal const int kDNSServiceErr_Unsupported = -65544;
        internal const int kDNSServiceErr_Refused = -65553;
        internal const int kDNSServiceErr_NoSuchRecord = -65554;
        internal const int kDNSServiceErr_ServiceNotRunning = -65563;
        internal const int kDNSServiceErr_Timeout = -65568;
        internal const int kDNSServiceErr_DefunctConnection = -65569;
        internal const int kDNSServiceErr_PolicyDenied = -65570;
        internal const int kDNSServiceErr_NotPermitted = -65571;

        internal const ushort kDNSServiceClass_IN = 1;

        internal const ushort kDNSServiceType_A = 1;
        internal const ushort kDNSServiceType_NS = 2;
        internal const ushort kDNSServiceType_CNAME = 5;
        internal const ushort kDNSServiceType_PTR = 12;
        internal const ushort kDNSServiceType_MX = 15;
        internal const ushort kDNSServiceType_TXT = 16;
        internal const ushort kDNSServiceType_AAAA = 28;
        internal const ushort kDNSServiceType_SRV = 33;

        [LibraryImport(Libraries.libSystem, StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int DNSServiceQueryRecord(
            out IntPtr sdRef,
            uint flags,
            uint interfaceIndex,
            string fullname,
            ushort rrtype,
            ushort rrclass,
            delegate* unmanaged[Cdecl]<IntPtr, uint, uint, int, byte*, ushort, ushort, ushort, void*, uint, IntPtr, void> callBack,
            IntPtr context);

        [LibraryImport(Libraries.libSystem)]
        internal static partial int DNSServiceRefSockFD(IntPtr sdRef);

        [LibraryImport(Libraries.libSystem)]
        internal static partial int DNSServiceProcessResult(IntPtr sdRef);

        [LibraryImport(Libraries.libSystem)]
        internal static partial void DNSServiceRefDeallocate(IntPtr sdRef);
    }
}
