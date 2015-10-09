// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal static class InteropIPAddressExtensions
    {
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
