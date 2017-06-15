// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class DnsParsingTests : FileCleanupTestBase
    {
        [InlineData("resolv.conf")]
        [InlineData("resolv_nonewline.conf")]
        [Theory]
        public void DnsSuffixParsing(string file)
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings(file, fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(fileName);
            Assert.Equal("fake.suffix.net", suffix);
        }

        [InlineData("resolv.conf")]
        [InlineData("resolv_nonewline.conf")]
        [Theory]
        public void DnsAddressesParsing(string file)
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings(file, fileName);

            var dnsAddresses = StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(fileName);
            Assert.Equal(1, dnsAddresses.Count);
            Assert.Equal(IPAddress.Parse("127.0.1.1"), dnsAddresses[0]);
        }
    }
}
