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

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal("fake.suffix.net", suffix);
        }

        [Fact]
        public void DnsSuffixParsing_DomainKeyword()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/resolv_domain.conf", fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal("domain.suffix.net", suffix);
        }

        [Fact]
        public void DnsSuffixParsing_DomainBeforeSearch_SearchWins()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/resolv_domain_before_search.conf", fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal("search.suffix.net", suffix);
        }

        [Fact]
        public void DnsSuffixParsing_SearchBeforeDomain_DomainWins()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/resolv_search_before_domain.conf", fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal("domain.suffix.net", suffix);
        }

        [Fact]
        public void DnsSuffixParsing_MultipleSearchDirectives_LastWins()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/resolv_multiple_search.conf", fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal("last.suffix.net", suffix);
        }

        [Fact]
        public void DnsSuffixParsing_MultipleDomainAndSearch_LastWins()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("NetworkFiles/resolv_multiple_domains_search.conf", fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal("last.suffix.net", suffix);
        }

        [InlineData("NetworkFiles/resolv.conf")]
        [InlineData("NetworkFiles/resolv_nonewline.conf")]
        [Theory]
        public void DnsAddressesParsing(string file)
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings(file, fileName);

            List<IPAddress> dnsAddresses = StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal(1, dnsAddresses.Count);
            Assert.Equal(IPAddress.Parse("127.0.1.1"), dnsAddresses[0]);
        }
    }
}
