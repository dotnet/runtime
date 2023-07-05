// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        public const int MAX_HOSTNAME_LEN = 128;
        public const int MAX_DOMAIN_NAME_LEN = 128;
        public const int MAX_SCOPE_ID_LEN = 256;

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct FIXED_INFO
        {
            private fixed byte _hostName[MAX_HOSTNAME_LEN + 4];
            public string HostName => CreateString(ref _hostName[0], MAX_HOSTNAME_LEN + 4);

            private fixed byte _domainName[MAX_DOMAIN_NAME_LEN + 4];
            public string DomainName => CreateString(ref _domainName[0], MAX_DOMAIN_NAME_LEN + 4);

            public IntPtr currentDnsServer; // IpAddressList*
            public IP_ADDR_STRING DnsServerList;
            public uint nodeType;

            private fixed byte _scopeId[MAX_SCOPE_ID_LEN + 4];
            public string ScopeId => CreateString(ref _scopeId[0], MAX_SCOPE_ID_LEN + 4);

            public uint enableRouting;
            public uint enableProxy;
            public uint enableDns;

            private static string CreateString(ref byte firstByte, int maxLength)
            {
                fixed (byte* ptr = &firstByte)
                {
                    int terminator = new ReadOnlySpan<byte>(ptr, maxLength).IndexOf((byte)0);
                    return Marshal.PtrToStringAnsi((IntPtr)ptr, (terminator >= 0) ? terminator : maxLength);
                }
            }
        }
    }
}
