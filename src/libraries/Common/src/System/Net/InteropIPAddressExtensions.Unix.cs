// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;

namespace System.Net
{
    internal static class InteropIPAddressExtensions
    {
        public static Interop.Sys.IPAddress GetNativeIPAddress(this IPAddress ipAddress)
        {
            var nativeIPAddress = default(Interop.Sys.IPAddress);

            ipAddress.TryWriteBytes(nativeIPAddress.Address, out int bytesWritten);
            Debug.Assert(bytesWritten == sizeof(uint) || bytesWritten == Interop.Sys.IPv6AddressBytes, $"Unexpected length: {bytesWritten}");

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                nativeIPAddress.IsIPv6 = true;
                nativeIPAddress.ScopeId = (uint)ipAddress.ScopeId;
            }

            return nativeIPAddress;
        }

        public static IPAddress GetIPAddress(this Interop.Sys.IPAddress nativeIPAddress)
        {
            if (!nativeIPAddress.IsIPv6)
            {
                uint address = BitConverter.ToUInt32(nativeIPAddress.Address);
                return new IPAddress((long)address);
            }
            else
            {
                return new IPAddress(nativeIPAddress.Address, (long)nativeIPAddress.ScopeId);
            }
        }
    }
}
