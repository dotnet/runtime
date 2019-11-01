// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class GetHostAddressesTest
    {
        [Fact]
        public Task Dns_GetHostAddressesAsync_HostString_Ok() => TestGetHostAddressesAsync(() => Dns.GetHostAddressesAsync(TestSettings.LocalHost));

        [Fact]
        public Task Dns_GetHostAddressesAsync_IPString_Ok() => TestGetHostAddressesAsync(() => Dns.GetHostAddressesAsync(TestSettings.LocalIPString));

        private static async Task TestGetHostAddressesAsync(Func<Task<IPAddress[]>> getHostAddressesFunc)
        {
            Task<IPAddress[]> hostEntryTask1 = getHostAddressesFunc();
            Task<IPAddress[]> hostEntryTask2 = getHostAddressesFunc();

            await TestSettings.WhenAllOrAnyFailedWithTimeout(hostEntryTask1, hostEntryTask2);

            IPAddress[] list1 = hostEntryTask1.Result;
            IPAddress[] list2 = hostEntryTask2.Result;

            Assert.NotNull(list1);
            Assert.NotNull(list2);

            Assert.Equal(list1.Length, list2.Length);

            var set = new HashSet<IPAddress>();
            for (int i = 0; i < list1.Length; i++)
            {
                Assert.Equal(list1[i], list2[i]);
                Assert.True(set.Add(list1[i]), "Multiple entries for address " + list1[i]);
            }
        }

        [Fact]
        public async Task Dns_GetHostAddressesAsync_LongHostNameEndsInDot_Ok()
        {
            int maxHostName = 255;
            string longHostName = new string('a', maxHostName - 1);
            string longHostNameWithDot = longHostName + ".";

            SocketException ex = await Assert.ThrowsAnyAsync<SocketException>(
                () => Dns.GetHostAddressesAsync(longHostNameWithDot));

            Assert.Equal(SocketError.HostNotFound, ex.SocketErrorCode);
        }

        [Fact]
        public async Task Dns_GetHostAddressesAsync_LongHostNameDoesNotEndInDot_Fail()
        {
            int maxHostName = 255;
            string longHostName = new string('a', maxHostName);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Dns.GetHostAddressesAsync(longHostName));
        }

        [Fact]
        public async Task Dns_GetHostAddressesAsync_NullHost_Fail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostAddressesAsync(null));
        }

        [Fact]
        public void DnsBeginGetHostAddresses_BadName_Throws()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses("BadName", null, null);
            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostAddresses(asyncObject));
        }

        [Fact]
        public void DnsBeginGetHostAddresses_BadIpString_ReturnsAddress()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses("0.0.1.1", null, null);
            IPAddress[] results = Dns.EndGetHostAddresses(asyncObject);

            Assert.Equal(1, results.Length);
            Assert.Equal(IPAddress.Parse("0.0.1.1"), results[0]);
        }

        [Fact]
        public void DnsBeginGetHostAddresses_MachineName_MatchesGetHostAddresses()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses(TestSettings.LocalHost, null, null);
            IPAddress[] results = Dns.EndGetHostAddresses(asyncObject);
            IPAddress[] addresses = Dns.GetHostAddresses(TestSettings.LocalHost);
            Assert.Equal(addresses, results);
        }

        [Fact]
        public void DnsGetHostAddresses_IPv4String_ReturnsSameIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(IPAddress.Loopback.ToString());
            Assert.Equal(1, addresses.Length);
            Assert.Equal(IPAddress.Loopback, addresses[0]);
        }

        [Fact]
        public void DnsGetHostAddresses_IPv6String_ReturnsSameIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(IPAddress.IPv6Loopback.ToString());
            Assert.Equal(1, addresses.Length);
            Assert.Equal(IPAddress.IPv6Loopback, addresses[0]);
        }

        [Fact]
        public void DnsGetHostAddresses_LocalHost_ReturnsSameAsGetHostEntry()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(TestSettings.LocalHost);
            IPHostEntry ipEntry = Dns.GetHostEntry(TestSettings.LocalHost);

            Assert.Equal(ipEntry.AddressList, addresses);
        }
    }
}
