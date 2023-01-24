// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation.Tests
{
    internal static class TestSettings
    {
        public static readonly string LocalHost = "localhost";
        public static readonly string UnreachableAddress = "192.0.2.0"; // TEST-NET-1
        public const int PingTimeout = 10 * 1000;

        public const string PayloadAsString = "'Post hoc ergo propter hoc'. 'After it, therefore because of it'. It means one thing follows the other, therefore it was caused by the other. But it's not always true. In fact it's hardly ever true.";

        // By default, FreeBSD supports buffer only up to 56 bytes
        public static readonly byte[] PayloadAsBytes = Encoding.UTF8.GetBytes(OperatingSystem.IsFreeBSD() ? TestSettings.PayloadAsString.Substring(0, 55) : TestSettings.PayloadAsString);

        public static readonly byte[] PayloadAsBytesShort = "ABCDEF0123456789"u8.ToArray();

        public static IPAddress[] GetLocalIPAddresses()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(LocalHost);
            return hostEntry.AddressList;
        }

        public static async Task<IPAddress[]> GetLocalIPAddressesAsync()
        {
            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(LocalHost);
            return hostEntry.AddressList;
        }

        public static IPAddress GetLocalIPAddress(AddressFamily addressFamily = AddressFamily.Unspecified)
        {
            IPAddress[] addressList = GetLocalIPAddresses();
            return GetIPAddressForHost(addressList, addressFamily);
        }

        public static async Task<IPAddress> GetLocalIPAddressAsync(AddressFamily addressFamily = AddressFamily.Unspecified)
        {
            IPAddress[] addressList = await GetLocalIPAddressesAsync();
            return GetIPAddressForHost(addressList, addressFamily);
        }

        private static IPAddress GetIPAddressForHost(IPAddress[] addressList, AddressFamily addressFamily = AddressFamily.Unspecified)
        {
            foreach (IPAddress address in addressList)
            {
                if (address.AddressFamily == addressFamily || (addressFamily == AddressFamily.Unspecified && address.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    return address;
                }
            }

            // If there's no IPv6 addresses, just take the first (IPv4) address.
            if (addressFamily == AddressFamily.Unspecified)
            {
                if (addressList.Length > 0)
                {
                    return addressList[0];
                }

                throw new InvalidOperationException("Unable to discover any addresses for the local host.");
            }

            return addressFamily == AddressFamily.InterNetwork ? IPAddress.Loopback : IPAddress.IPv6Loopback;
        }
    }
}
