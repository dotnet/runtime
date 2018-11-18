// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0618 // use of obsolete methods

using System.Net.Sockets;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class GetHostByNameTest
    {
        [Fact]
        public void DnsObsoleteBeginGetHostByName_BadName_Throws()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostByName("BadName", null, null);
            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostByName(asyncObject));
        }

        [Fact]
        public void DnsObsoleteBeginGetHostByName_IPv4String_ReturnsOnlyGivenIP()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostByName(IPAddress.Loopback.ToString(), null, null);
            IPHostEntry entry = Dns.EndGetHostByName(asyncObject);

            Assert.Equal(IPAddress.Loopback.ToString(), entry.HostName);
            Assert.Equal(1, entry.AddressList.Length);
            Assert.Equal(IPAddress.Loopback, entry.AddressList[0]);
        }

        [Fact]
        public void DnsObsoleteBeginGetHostByName_MachineNameWithIPv4_MatchesGetHostByName()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostByName(TestSettings.LocalHost, null, null);
            IPHostEntry result = Dns.EndGetHostByName(asyncObject);
            IPHostEntry entry = Dns.GetHostByName(TestSettings.LocalHost);

            Assert.Equal(entry.HostName, result.HostName);
            Assert.Equal(entry.AddressList, result.AddressList);
        }

        [Fact]
        public void DnsObsoleteGetHostByName_HostAlmostTooLong254Chars_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostByName(
                "Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Are"));
        }

        [Fact]
        public void DnsObsoleteGetHostByName_HostAlmostTooLong254CharsAndDot_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostByName(
                "Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Are."));
        }

        [Fact]
        public void DnsObsoleteGetHostByName_HostTooLong255Chars_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Dns.GetHostByName(
                "Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Aret"));
        }

        [Fact]
        public void DnsObsoleteGetHostByName_LocalHost_ReturnsLoopback()
        {
            IPHostEntry entry = Dns.GetHostByName(TestSettings.LocalHost);

            Assert.True(entry.AddressList.Length > 0);
            Assert.Contains(IPAddress.Loopback, entry.AddressList);
        }

        [Fact]
        public void DnsObsoleteGetHostByName_BadName_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostByName("BadName"));
        }

        [Fact]
        public void DnsObsoleteGetHostByName_IPv4String_ReturnsOnlyGivenIP()
        {
            IPHostEntry entry = Dns.GetHostByName(IPAddress.Loopback.ToString());

            Assert.Equal(IPAddress.Loopback.ToString(), entry.HostName);
            Assert.Equal(1, entry.AddressList.Length);
            Assert.Equal(IPAddress.Loopback, entry.AddressList[0]);
        }

        [Fact]
        public void DnsObsoleteGetHostByName_IPv6String_ReturnsOnlyGivenIP()
        {
            IPHostEntry entry = Dns.GetHostByName(IPAddress.IPv6Loopback.ToString());

            Assert.Equal(IPAddress.IPv6Loopback.ToString(), entry.HostName);
            Assert.Equal(1, entry.AddressList.Length);
            Assert.Equal(IPAddress.IPv6Loopback, entry.AddressList[0]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotArm64Process))] // [ActiveIssue(32797)]
        public void DnsObsoleteGetHostByName_EmptyString_ReturnsHostName()
        {
            IPHostEntry entry = Dns.GetHostByName("");

            // DNS labels should be compared as case insensitive for ASCII characters. See RFC 4343.
            Assert.Contains(Dns.GetHostName(), entry.HostName, StringComparison.OrdinalIgnoreCase);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotArm64Process))] // [ActiveIssue(32797)]
        public void DnsObsoleteBeginEndGetHostByName_EmptyString_ReturnsHostName()
        {
            IPHostEntry entry = Dns.EndGetHostByName(Dns.BeginGetHostByName("", null, null));

            // DNS labels should be compared as case insensitive for ASCII characters. See RFC 4343.
            Assert.Contains(Dns.GetHostName(), entry.HostName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
