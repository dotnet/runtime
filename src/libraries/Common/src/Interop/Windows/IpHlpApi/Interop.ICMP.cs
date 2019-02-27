// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

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
        internal extern static uint Icmp6SendEcho2(SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext,
            byte[] sourceSocketAddress, byte[] destSocketAddress, [In] SafeLocalAllocHandle data, ushort dataSize, ref IPOptions options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);
    }
}
