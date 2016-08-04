// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class DnsTests
    {
        [Fact]
        public void Dns_GetHostAddressesAsync_HostString_Ok()
        {
            TestGetHostAddressesAsync(() => Dns.GetHostAddressesAsync(TestSettings.LocalHost));
        }

        [Fact]
        public void Dns_GetHostAddressesAsync_IPString_Ok()
        {
            TestGetHostAddressesAsync(() => Dns.GetHostAddressesAsync(TestSettings.LocalIPString));
        }
        
        private static void TestGetHostAddressesAsync(Func<Task<IPAddress[]>> getHostAddressesFunc)
        {
            Task<IPAddress[]> hostEntryTask1 = getHostAddressesFunc();
            Task<IPAddress[]> hostEntryTask2 = getHostAddressesFunc();

            Task.WaitAll(hostEntryTask1, hostEntryTask2);

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
        public async Task Dns_GetHostEntryAsync_IPAddress_Ok()
        {
            IPAddress localIPAddress = await TestSettings.GetLocalIPAddress();

            TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(localIPAddress));
        }

        [Fact]
        public void Dns_GetHostEntryAsync_HostString_Ok()
        {
            TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(TestSettings.LocalHost));
        }

        [Fact]
        public void Dns_GetHostEntryAsync_IPString_Ok()
        {
            TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(TestSettings.LocalIPString));
        }

        [Fact]
        public void Dns_GetHostName_Ok()
        {
            Assert.False(string.IsNullOrWhiteSpace(Dns.GetHostName()));
        }

        private static void TestGetHostEntryAsync(Func<Task<IPHostEntry>> getHostEntryFunc)
        {
            Task<IPHostEntry> hostEntryTask1 = getHostEntryFunc();
            Task<IPHostEntry> hostEntryTask2 = getHostEntryFunc();

            Task.WaitAll(hostEntryTask1, hostEntryTask2);

            IPAddress[] list1 = hostEntryTask1.Result.AddressList;
            IPAddress[] list2 = hostEntryTask2.Result.AddressList;

            Assert.NotNull(list1);
            Assert.NotNull(list2);

            Assert.Equal(list1.Length, list2.Length);
            for (var i = 0; i < list1.Length; i++)
            {
                Assert.Equal(list1[i], list2[i]);
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
        public async Task Dns_GetHostEntryAsync_NullStringHost_Fail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((string)null));
        }
        
        [Fact]
        public async Task Dns_GetHostEntryAsync_NullIPAddressHost_Fail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((IPAddress)null));
        }

        public static IEnumerable<object[]> GetInvalidAddresses()
        {
            yield return new object[] { IPAddress.Any };
            yield return new object[] { IPAddress.IPv6Any };
            yield return new object[] { IPAddress.IPv6None };
        }

        [Theory]
        [MemberData(nameof(GetInvalidAddresses))]
        public async Task Dns_GetHostEntryAsync_AnyIPAddress_Fail(IPAddress address)
        {
            string addressString = address.ToString();

            await Assert.ThrowsAsync<ArgumentException>(() => Dns.GetHostEntryAsync(address));
            await Assert.ThrowsAsync<ArgumentException>(() => Dns.GetHostEntryAsync(addressString));
        }

        public static IEnumerable<object[]> GetNoneAddresses()
        {
            yield return new object[] { IPAddress.None };
        }

        [ActiveIssue(10345, PlatformID.AnyUnix)]
        [Theory]
        [MemberData(nameof(GetNoneAddresses))]
        public async Task Dns_GetHostEntryAsync_NoneIPAddress_Fail(IPAddress address)
        {
            string addressString = address.ToString();

            await Assert.ThrowsAsync<SocketException>(() => Dns.GetHostEntryAsync(address));
            await Assert.ThrowsAsync<SocketException>(() => Dns.GetHostEntryAsync(addressString));
        }
    }
}
