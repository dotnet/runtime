// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class DnsParsingTests
    {
        [Fact]
        public static void DnsSuffixParsing()
        {
            FileUtil.NormalizeLineEndings("resolv.conf", "resolv.conf_normalized0");

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile("resolv.conf_normalized0");
            Assert.Equal("fake.suffix.net", suffix);
        }

        [Fact]
        public static void DnsAddressesParsing()
        {
            FileUtil.NormalizeLineEndings("resolv.conf", "resolv.conf_normalized1");

            var dnsAddresses = StringParsingHelpers.ParseDnsAddressesFromResolvConfFile("resolv.conf_normalized1");
            Assert.Equal(1, dnsAddresses.Count);
            Assert.Equal(IPAddress.Parse("127.0.1.1"), dnsAddresses[0]);
        }
    }
}
