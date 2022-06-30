// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#pragma warning disable CS0618 // using Obsolete Address API because it's the more efficient option in this case
                byte firstByte = (byte)address.Address;
#pragma warning restore CS0618
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
            IPAddress ipAddress = new IPAddress(new ReadOnlySpan<byte>(addressInfo->AddressBytes, addressInfo->NumAddressBytes));
            return ipAddress;
        }
    }
}
