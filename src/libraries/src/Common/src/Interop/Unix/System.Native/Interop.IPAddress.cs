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
        internal static extern int IPv6StringToAddress(string address, string port, byte[] buffer, int bufferLength, out uint scope);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int IPv4StringToAddress(string address, byte[] buffer, int bufferLength, out ushort port);

        [DllImport(Libraries.SystemNative)]
        internal unsafe static extern int IPAddressToString(byte* address, int addressLength, bool isIPv6, byte* str, int stringLength, uint scope = 0);

        internal unsafe static uint IPAddressToString(byte[] address, bool isIPv6, System.Text.StringBuilder addressString, uint scope = 0)
        {
            Debug.Assert(address != null);
            Debug.Assert((address.Length == IPv4AddressBytes) || (address.Length == IPv6AddressBytes));

            int err;
            fixed (byte* rawAddress = address)
            {
                int bufferLength = isIPv6 ? INET6_ADDRSTRLEN : INET_ADDRSTRLEN;
                byte* buffer = stackalloc byte[bufferLength];
                err = IPAddressToString(rawAddress, address.Length, isIPv6, buffer, bufferLength, scope);
                if (err == 0)
                {
                    addressString.Append(Marshal.PtrToStringAnsi((IntPtr)buffer));
                }
            }

            return unchecked((uint)err);
        }
    }
}
