// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class DnsParsingTests
    {
        [InlineData("resolv.conf")]
        [InlineData("resolv_nonewline.conf")]
        [Theory]
        public static void DnsSuffixParsing(string file)
        {
            FileUtil.NormalizeLineEndings(file, file + "_normalized0");

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(file + "_normalized0");
            Assert.Equal("fake.suffix.net", suffix);
        }

        [InlineData("resolv.conf")]
        [InlineData("resolv_nonewline.conf")]
        [Theory]
        public static void DnsAddressesParsing(string file)
        {
            FileUtil.NormalizeLineEndings(file, file + "_normalized1");

            var dnsAddresses = StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(file + "_normalized1");
            Assert.Equal(1, dnsAddresses.Count);
            Assert.Equal(IPAddress.Parse("127.0.1.1"), dnsAddresses[0]);
        }
    }
}
