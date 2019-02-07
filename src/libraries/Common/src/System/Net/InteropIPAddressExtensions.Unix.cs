// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Sockets;

namespace System.Net
{
    internal static class InteropIPAddressExtensions
    {
        public static unsafe Interop.Sys.IPAddress GetNativeIPAddress(this IPAddress ipAddress)
        {
            var nativeIPAddress = default(Interop.Sys.IPAddress);

            ipAddress.TryWriteBytes(new Span<byte>(nativeIPAddress.Address, Interop.Sys.IPv6AddressBytes), out int bytesWritten);
            Debug.Assert(bytesWritten == sizeof(uint) || bytesWritten == Interop.Sys.IPv6AddressBytes, $"Unexpected length: {bytesWritten}");

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                nativeIPAddress.IsIPv6 = true;
                nativeIPAddress.ScopeId = (uint)ipAddress.ScopeId;
            }

            return nativeIPAddress;
        }

        public static unsafe IPAddress GetIPAddress(this Interop.Sys.IPAddress nativeIPAddress)
        {
            if (!nativeIPAddress.IsIPv6)
            {
                uint address = *(uint*)nativeIPAddress.Address;
                return new IPAddress((long)address);
            }
            else
            {
                return new IPAddress(
                    new ReadOnlySpan<byte>(nativeIPAddress.Address, Interop.Sys.IPv6AddressBytes),
                    (long)nativeIPAddress.ScopeId);
            }
        }
    }
}
