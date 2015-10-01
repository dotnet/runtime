// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal const int IPv4AddressBytes = 4;
        internal const int IPv6AddressBytes = 16;

        internal const int INET_ADDRSTRLEN = 22;
        internal const int INET6_ADDRSTRLEN = 65;

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int Ipv6StringToAddress(string address, string port, byte[] buffer, int bufferLength, out uint scope);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int Ipv4StringToAddress(string address, byte[] buffer, int bufferLength, out ushort port);

        [DllImport(Libraries.SystemNative)]
        internal unsafe static extern int IpAddressToString(byte* address, int addressLength, bool isIpv6, byte* str, int stringLength, uint scope = 0);

        internal unsafe static uint IpAddressToString(byte[] address, bool isIpV6, System.Text.StringBuilder addressString, uint scope = 0)
        {
            Debug.Assert(address != null);
            Debug.Assert((address.Length == IPv4AddressBytes) || (address.Length == IPv6AddressBytes));

            int err;
            fixed (byte* rawAddress = address)
            {
                int bufferLength = isIpV6 ? INET6_ADDRSTRLEN : INET_ADDRSTRLEN;
                byte* buffer = stackalloc byte[bufferLength];
                err = IpAddressToString(rawAddress, address.Length, isIpV6, buffer, bufferLength, scope);
                if (err == 0)
                {
                    addressString.Append(Marshal.PtrToStringAnsi((IntPtr)buffer));
                }
            }

            return unchecked((uint)err);
        }
    }
}
