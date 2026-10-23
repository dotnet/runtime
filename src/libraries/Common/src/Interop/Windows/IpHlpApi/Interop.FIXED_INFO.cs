// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        public const int MAX_HOSTNAME_LEN = 128;
        public const int MAX_DOMAIN_NAME_LEN = 128;
        public const int MAX_SCOPE_ID_LEN = 256;

        [StructLayout(LayoutKind.Sequential)]
        public struct FIXED_INFO
        {
            private HostNameBuffer _hostName;
            public string HostName => CreateString(_hostName);

            private DomainNameBuffer _domainName;
            public string DomainName => CreateString(_domainName);

            public IntPtr currentDnsServer; // IpAddressList*
            public IP_ADDR_STRING DnsServerList;
            public uint nodeType;

            private ScopeIdBuffer _scopeId;
            public string ScopeId => CreateString(_scopeId);

            public uint enableRouting;
            public uint enableProxy;
            public uint enableDns;

            private static unsafe string CreateString(ReadOnlySpan<byte> buffer)
            {
                int terminator = buffer.IndexOf((byte)0);
                fixed (byte* ptr = buffer)
                {
                    return Marshal.PtrToStringAnsi((IntPtr)ptr, (terminator >= 0) ? terminator : buffer.Length);
                }
            }

            [InlineArray(MAX_HOSTNAME_LEN + 4)]
            private struct HostNameBuffer
            {
                private byte _element0;
            }

            [InlineArray(MAX_DOMAIN_NAME_LEN + 4)]
            private struct DomainNameBuffer
            {
                private byte _element0;
            }

            [InlineArray(MAX_SCOPE_ID_LEN + 4)]
            private struct ScopeIdBuffer
            {
                private byte _element0;
            }
        }
    }
}
