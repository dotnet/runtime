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
        public const int PingTimeout = 200;

        public const string PayloadAsString = "'Post hoc ergo propter hoc'. 'After it, therefore because of it'. It means one thing follows the other, therefore it was caused by the other. But it's not always true. In fact it's hardly ever true.";
        public static readonly byte[] PayloadAsBytes = Encoding.UTF8.GetBytes(TestSettings.PayloadAsString);

        public static Task<IPAddress> GetLocalIPAddress()
        {
            return ResolveHost(LocalHost, AddressFamily);
        }

        public static AddressFamily AddressFamily
        {
            get
            {
                return AddressFamily.InterNetworkV6;
            }
        }

        private static async Task<IPAddress> ResolveHost(string host, AddressFamily family)
        {
            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(host);

            foreach (IPAddress address in hostEntry.AddressList)
            {
                if (address.AddressFamily == family)
                {
                    return address;
                }
            }

            return null;
        }
    }
}
