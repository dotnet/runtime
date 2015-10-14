// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Sockets;

namespace System.Net
{
    internal static class InteropIPAddressExtensions
    {
        public static unsafe Interop.Sys.IPAddress GetNativeIPAddress(this IPAddress ipAddress)
        {
            var nativeIPAddress = default(Interop.Sys.IPAddress);

            byte[] bytes = ipAddress.GetAddressBytes();
            Debug.Assert(bytes.Length == sizeof(uint) || bytes.Length == Interop.Sys.IPv6AddressBytes);

            for (int i = 0; i < bytes.Length && i < Interop.Sys.IPv6AddressBytes; i++)
            {
                nativeIPAddress.Address[i] = bytes[i];
            }
            
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
                byte[] address = new byte[Interop.Sys.IPv6AddressBytes];
                for (int b = 0; b < Interop.Sys.IPv6AddressBytes; b++)
                {
                    address[b] = nativeIPAddress.Address[b];
                }

                return new IPAddress(address, (long)nativeIPAddress.ScopeId);
            }
        }
    }
}
