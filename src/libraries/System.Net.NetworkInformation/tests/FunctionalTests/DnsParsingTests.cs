// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class DnsParsingTests : FileCleanupTestBase
    {
        [InlineData("NetworkFiles/resolv.conf")]
        [InlineData("NetworkFiles/resolv_nonewline.conf")]
        [Theory]
        public void DnsSuffixParsing(string file)
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings(file, fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(StringParsingHelpers.ReadResolvConfFile(fileName));
            Assert.Equal("fake.suffix.net", suffix);
        }

        [InlineData("NetworkFiles/resolv.conf")]
        [InlineData("NetworkFiles/resolv_nonewline.conf")]
        [Theory]
        public void DnsAddressesParsing(string file)
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings(file, fileName);

            List<IPAddress> dnsAddresses = StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(StringParsingHelpers.ReadResolvConfFile(fileName));
            Assert.Equal(1, dnsAddresses.Count);
            Assert.Equal(IPAddress.Parse("127.0.1.1"), dnsAddresses[0]);
        }

        [InlineData("resolv.conf")]
        [InlineData("etc/resolv.conf")]
        [Theory]
        public void ReadResolvConfFile_MissingFileOrDirectory_ReturnsNull(string relativePath)
        {
            string fileName = Path.Combine(TestDirectory, relativePath);

            Assert.Null(StringParsingHelpers.ReadResolvConfFile(fileName));
        }
    }
}
