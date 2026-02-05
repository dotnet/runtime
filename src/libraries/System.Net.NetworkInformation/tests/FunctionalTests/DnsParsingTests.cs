// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class DnsParsingTests : FileCleanupTestBase
    {
        [Theory]
        [InlineData("NetworkFiles/resolv.conf", "fake.suffix.net")]
        [InlineData("NetworkFiles/resolv_nonewline.conf", "fake.suffix.net")]
        [InlineData("NetworkFiles/resolv_domain.conf", "domain.suffix.net")]
        [InlineData("NetworkFiles/resolv_domain_before_search.conf", "search.suffix.net")]
        [InlineData("NetworkFiles/resolv_search_before_domain.conf", "domain.suffix.net")]
        [InlineData("NetworkFiles/resolv_multiple_search.conf", "last.suffix.net")]
        [InlineData("NetworkFiles/resolv_multiple_domains_search.conf", "last.suffix.net")]
        [InlineData("NetworkFiles/resolv_subdomain_comment.conf", "correct.domain.net")]
        [InlineData("NetworkFiles/resolv_search_multiple_domains.conf", "foo3.com")]
        [InlineData("NetworkFiles/resolv_midline_keyword.conf", "valid.suffix.net")]
        public void DnsSuffixParsing(string file, string expectedSuffix)
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings(file, fileName);

            string suffix = StringParsingHelpers.ParseDnsSuffixFromResolvConfFile(File.ReadAllText(fileName));
            Assert.Equal(expectedSuffix, suffix);
        }

        [Theory]
        [InlineData("NetworkFiles/resolv.conf")]
        [InlineData("NetworkFiles/resolv_nonewline.conf")]
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
