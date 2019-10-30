// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        public const int MAX_HOSTNAME_LEN = 128;
        public const int MAX_DOMAIN_NAME_LEN = 128;
        public const int MAX_SCOPE_ID_LEN = 256;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct FIXED_INFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_HOSTNAME_LEN + 4)]
            public string hostName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_DOMAIN_NAME_LEN + 4)]
            public string domainName;
            public IntPtr currentDnsServer; // IpAddressList*
            public IP_ADDR_STRING DnsServerList;
            public uint nodeType;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_SCOPE_ID_LEN + 4)]
            public string scopeId;
            public bool enableRouting;
            public bool enableProxy;
            public bool enableDns;
        }
    }
}
