// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        internal const int IP_STATUS_BASE = 11000;

        [StructLayout(LayoutKind.Sequential)]
        internal struct IP_OPTION_INFORMATION
        {
            internal byte ttl;
            internal byte tos;
            internal byte flags;
            internal byte optionsSize;
            internal IntPtr optionsData;

            internal IP_OPTION_INFORMATION(PingOptions? options)
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
        internal struct ICMP_ECHO_REPLY
        {
            internal uint address;
            internal uint status;
            internal uint roundTripTime;
            internal ushort dataSize;
            internal ushort reserved;
            internal IntPtr data;
            internal IP_OPTION_INFORMATION options;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct IPV6_ADDRESS_EX
        {
            internal ushort port;
            internal uint flowinfo;

            // Replying address.
            private fixed byte _Address[16];
            internal byte[] Address => MemoryMarshal.CreateReadOnlySpan(ref _Address[0], 16).ToArray();

            internal uint ScopeID;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ICMPV6_ECHO_REPLY
        {
            internal IPV6_ADDRESS_EX Address;
            // Reply IP_STATUS.
            internal uint Status;
            // RTT in milliseconds.
            internal uint RoundTripTime;
        }

        internal sealed class SafeCloseIcmpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeCloseIcmpHandle() : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return Interop.IpHlpApi.IcmpCloseHandle(handle);
            }
        }

        [LibraryImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal static partial SafeCloseIcmpHandle IcmpCreateFile();

        [LibraryImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal static partial SafeCloseIcmpHandle Icmp6CreateFile();

        [LibraryImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IcmpCloseHandle(IntPtr handle);

        [LibraryImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal static partial uint IcmpSendEcho2(SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext,
            uint ipAddress, SafeLocalAllocHandle data, ushort dataSize, ref IP_OPTION_INFORMATION options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);

        [LibraryImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal static partial uint Icmp6SendEcho2(SafeCloseIcmpHandle icmpHandle, SafeWaitHandle Event, IntPtr apcRoutine, IntPtr apcContext,
            byte[] sourceSocketAddress, byte[] destSocketAddress, SafeLocalAllocHandle data, ushort dataSize, ref IP_OPTION_INFORMATION options, SafeLocalAllocHandle replyBuffer, uint replySize, uint timeout);
    }
}
