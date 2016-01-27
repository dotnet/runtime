// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        // TODO: #3562 - Replace names with the ones from the Windows SDK.

        [StructLayout(LayoutKind.Sequential)]
        internal struct IPOptions
        {
            internal byte ttl;
            internal byte tos;
            internal byte flags;
            internal byte optionsSize;
            internal IntPtr optionsData;

            internal IPOptions(PingOptions options)
            {
                ttl = 128;
                tos = 0;
                flags = 0;
                optionsSize = 0;
                optionsData = IntPtr.Zero;

                if (options != null)
                {
                    this.ttl = (byte)options.Ttl;

                    if (options.DontFragment)
                    {
                        flags = 2;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IcmpEchoReply
        {
            internal uint address;
            internal uint status;
            internal uint roundTripTime;
            internal ushort dataSize;
            internal ushort reserved;
            internal IntPtr data;
            internal IPOptions options;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Ipv6Address
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            internal byte[] Goo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            // Replying address.
            internal byte[] Address;
            internal uint ScopeID;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Icmp6EchoReply
        {
            internal Ipv6Address Address;
            // Reply IP_STATUS.
            internal uint Status;
            // RTT in milliseconds.
            internal uint RoundTripTime;
            internal IntPtr data;
            // internal IPOptions options;
            // internal IntPtr data; data os after tjos
        }

        internal enum IcmpV4Type
        {
            // Can be mapped:
            ICMP4_ECHO_REPLY = 0, // Echo Reply.
            ICMP4_DST_UNREACH = 3, // Destination Unreachable.
            ICMP4_SOURCE_QUENCH = 4, // Source Quench.
            ICMP4_TIME_EXCEEDED = 11, // Time Exceeded.
            ICMP4_PARAM_PROB = 12, // Parameter Problem.

            // Un-mappable:
            ICMP4_REDIRECT = 5, // Redirect.
            ICMP4_ECHO_REQUEST = 8, // Echo Request.
            ICMP4_ROUTER_ADVERT = 9, // Router Advertisement.
            ICMP4_ROUTER_SOLICIT = 10, // Router Solicitation.
            ICMP4_TIMESTAMP_REQUEST = 13, // Time-stamp Request.
            ICMP4_TIMESTAMP_REPLY = 14, // Time-stamp Reply.
            ICMP4_MASK_REQUEST = 17, // Address Mask Request.
            ICMP4_MASK_REPLY = 18, // Address Mask Reply.
        }

        internal enum IcmpV4Code
        {
            ICMP4_UNREACH_NET = 0,
            ICMP4_UNREACH_HOST = 1,
            ICMP4_UNREACH_PROTOCOL = 2,
            ICMP4_UNREACH_PORT = 3,
            ICMP4_UNREACH_FRAG_NEEDED = 4,
            ICMP4_UNREACH_SOURCEROUTE_FAILED = 5,
            ICMP4_UNREACH_NET_UNKNOWN = 6,
            ICMP4_UNREACH_HOST_UNKNOWN = 7,
            ICMP4_UNREACH_ISOLATED = 8,
            ICMP4_UNREACH_NET_ADMIN = 9,
            ICMP4_UNREACH_HOST_ADMIN = 10,
            ICMP4_UNREACH_NET_TOS = 11,
            ICMP4_UNREACH_HOST_TOS = 12,
            ICMP4_UNREACH_ADMIN = 13,
        }

        internal sealed class SafeCloseIcmpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeCloseIcmpHandle() : base(true)
            {
            }

            override protected bool ReleaseHandle()
            {
                return Interop.IpHlpApi.IcmpCloseHandle(handle);
            }
        }

        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static SafeCloseIcmpHandle IcmpCreateFile();

        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static SafeCloseIcmpHandle Icmp6CreateFile();

        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static bool IcmpCloseHandle(IntPtr handle);

        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static uint IcmpSendEcho2(SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext,
            uint ipAddress, [In] SafeLocalAllocHandle data, ushort dataSize, ref IPOptions options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);

        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static uint IcmpSendEcho2(SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
            uint ipAddress, [In] SafeLocalAllocHandle data, ushort dataSize, ref IPOptions options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);

        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static uint Icmp6SendEcho2(SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext,
            byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalAllocHandle data, ushort dataSize, ref IPOptions options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);

        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static uint Icmp6SendEcho2(SafeCloseIcmpHandle icmpHandle, IntPtr Event, IntPtr apcRoutine, IntPtr apcContext,
            byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalAllocHandle data, ushort dataSize, ref IPOptions options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);
    }
}
