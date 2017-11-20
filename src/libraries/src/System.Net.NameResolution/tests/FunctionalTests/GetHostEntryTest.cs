// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class GetHostEntryTest
    {
        [Fact]
        public async Task Dns_GetHostEntryAsync_IPAddress_Ok()
        {
            IPAddress localIPAddress = await TestSettings.GetLocalIPAddress();

            await TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(localIPAddress));
        }

        [Theory]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public Task Dns_GetHostEntry_HostString_Ok(string hostName) => TestGetHostEntryAsync(() => Task.FromResult(Dns.GetHostEntry(hostName)));

        [Theory]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public Task Dns_GetHostEntryAsync_HostString_Ok(string hostName) => TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(hostName));

        [Fact]
        public Task Dns_GetHostEntryAsync_IPString_Ok() => TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(TestSettings.LocalIPString));

        private static async Task TestGetHostEntryAsync(Func<Task<IPHostEntry>> getHostEntryFunc)
        {
            Task<IPHostEntry> hostEntryTask1 = getHostEntryFunc();
            Task<IPHostEntry> hostEntryTask2 = getHostEntryFunc();

            await TestSettings.WhenAllOrAnyFailedWithTimeout(hostEntryTask1, hostEntryTask2);

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
        public Task Dns_GetHostEntryAsync_NullStringHost_Fail() => Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((string)null));

        [Fact]
        public Task Dns_GetHostEntryAsync_NullIPAddressHost_Fail() => Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((IPAddress)null));

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

        [Fact]
        public void DnsBeginGetHostEntry_BadName_Throws()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostEntry("BadName", null, null);
            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostEntry(asyncObject));
        }

        [Fact]
        public void DnsBeginGetHostEntry_BadIpString_Throws()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostEntry("0.0.1.1", null, null);
            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostEntry(asyncObject));
        }

        [Fact]
        public void DnsBeginGetHostEntry_MachineName_MatchesGetHostEntry()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostEntry(TestSettings.LocalHost, null, null);
            IPHostEntry results = Dns.EndGetHostEntry(asyncObject);
            IPHostEntry entry = Dns.GetHostEntry(TestSettings.LocalHost);

            Assert.Equal(entry.HostName, results.HostName);
            Assert.Equal(entry.AddressList, results.AddressList);
        }

        [Fact]
        public void DnsBeginGetHostEntry_Loopback_MatchesGetHostEntry()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostEntry(IPAddress.Loopback, null, null);
            IPHostEntry results = Dns.EndGetHostEntry(asyncObject);
            IPHostEntry entry = Dns.GetHostEntry(IPAddress.Loopback);

            Assert.Equal(entry.HostName, results.HostName);

            Assert.Equal(entry.AddressList, results.AddressList);
        }

        [Fact]
        public void DnsGetHostEntry_BadName_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry("BadName"));
        }

        [Fact]
        public void DnsGetHostEntry_BadIpString_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry("0.0.1.1"));
        }

        [Fact]
        public void DnsGetHostEntry_HostAlmostTooLong254Chars_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(
                "Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Are"));
        }

        [Fact]
        public void DnsGetHostEntry_HostAlmostTooLong254CharsAndDot_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(
                "Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Are."));
        }

        [Fact]
        public void DnsGetHostEntry_HostTooLong255Chars_Throws()
        {
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Dns.GetHostEntry(
                "Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Aret"));
        }

        [Fact]
        public void DnsGetHostEntry_LocalHost_ReturnsFqdnAndLoopbackIPs()
        {
            IPHostEntry entry = Dns.GetHostEntry("localhost");

            Assert.NotNull(entry.HostName);
            Assert.True(entry.HostName.Length > 0, "Empty host name");

            Assert.True(entry.AddressList.Length >= 1, "No local IPs");
            foreach (IPAddress addr in entry.AddressList)
                Assert.True(IPAddress.IsLoopback(addr), "Not a loopback address: " + addr);
        }

        [Fact]
        public void DnsGetHostEntry_LoopbackIP_MatchesGetHostEntryLoopbackString()
        {
            IPHostEntry ipEntry = Dns.GetHostEntry(IPAddress.Loopback);
            IPHostEntry stringEntry = Dns.GetHostEntry(IPAddress.Loopback.ToString());

            Assert.Equal(ipEntry.HostName, stringEntry.HostName);
            Assert.Equal(ipEntry.AddressList, stringEntry.AddressList);
        }

        [Fact]
        public void DnsGetHostEntry_UnknownUnicodeHost_HostNotFound()
        {
            // This would succeed if the name was registered in DNS
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry("Test-新-Unicode"));
        }

        [Fact]
        public void DnsGetHostEntry_UnknownPunicodeHost_HostNotFound()
        {
            // This would succeed if the name was registered in DNS            
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry("xn--test--unicode-0b01a"));
        }
    }
}
