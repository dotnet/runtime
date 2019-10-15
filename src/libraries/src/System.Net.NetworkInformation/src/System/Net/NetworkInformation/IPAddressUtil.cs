// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.NetworkInformation
{
    internal static class IPAddressUtil
    {
        /// <summary>
        /// Returns a value indicating whether the given IPAddress is a multicast address.
        /// </summary>
        /// <param name="address">The address to test.</param>
        /// <returns>True if the address is a multicast address; false otherwise.</returns>
        public static bool IsMulticast(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return address.IsIPv6Multicast;
            }
            else
            {
                byte firstByte = address.GetAddressBytes()[0];
                return firstByte >= 224 && firstByte <= 239;
            }
        }

        /// <summary>
        /// Copies the address bytes out of the given native info's buffer and constructs a new IPAddress.
        /// </summary>
        /// <param name="addressInfo">A pointer to a native IpAddressInfo structure.</param>
        /// <returns>A new IPAddress created with the information in the native structure.</returns>
        public static unsafe IPAddress GetIPAddressFromNativeInfo(Interop.Sys.IpAddressInfo* addressInfo)
        {
            byte[] ipBytes = new byte[addressInfo->NumAddressBytes];
            fixed (byte* ipArrayPtr = ipBytes)
            {
                Buffer.MemoryCopy(addressInfo->AddressBytes, ipArrayPtr, ipBytes.Length, ipBytes.Length);
            }
            IPAddress ipAddress = new IPAddress(ipBytes);
            return ipAddress;
        }
    }
}
