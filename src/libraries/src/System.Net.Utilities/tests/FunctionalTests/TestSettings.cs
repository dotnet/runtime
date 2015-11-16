// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Utilities.Tests
{
    internal static class TestSettings
    {
        public static readonly string LocalHost = "localhost";
        public const int PingTimeout = 1000;

        public const string PayloadAsString = "'Post hoc ergo propter hoc'. 'After it, therefore because of it'. It means one thing follows the other, therefore it was caused by the other. But it's not always true. In fact it's hardly ever true.";
        public static readonly byte[] PayloadAsBytes = Encoding.UTF8.GetBytes(TestSettings.PayloadAsString);

        public static Task<IPAddress> GetLocalIPAddress()
        {
            return ResolveHost(LocalHost);
        }

        private static async Task<IPAddress> ResolveHost(string host)
        {
            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(host);
            IPAddress ret = null;

            foreach (IPAddress address in hostEntry.AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    ret = address;
                }
            }

            // If there's no IPv6 addresses, just take the first (IPv4) address.
            if (ret == null)
            {
                ret = hostEntry.AddressList[0];
            }

            if (ret != null)
            {
                return ret;
            }

            throw new InvalidOperationException("Unable to discover any addresses for host " + host);
        }
    }
}
