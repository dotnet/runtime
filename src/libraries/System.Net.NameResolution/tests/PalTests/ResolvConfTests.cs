// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Net.NameResolution.PalTests
{
    public class ResolvConfTests
    {
        private static List<IPEndPoint> Parse(string contents)
        {
            using StringReader reader = new StringReader(contents);
            return ResolvConf.Parse(reader);
        }

        [Fact]
        public void Parse_SingleNameserver_ReturnsEndpointWithPort53()
        {
            List<IPEndPoint> servers = Parse("nameserver 192.0.2.1\n");

            IPEndPoint server = Assert.Single(servers);
            Assert.Equal(IPAddress.Parse("192.0.2.1"), server.Address);
            Assert.Equal(53, server.Port);
        }

        [Fact]
        public void Parse_MultipleNameservers_PreservesOrder()
        {
            List<IPEndPoint> servers = Parse(
                "nameserver 192.0.2.1\n" +
                "nameserver 192.0.2.2\n" +
                "nameserver 192.0.2.3\n");

            Assert.Equal(3, servers.Count);
            Assert.Equal(IPAddress.Parse("192.0.2.1"), servers[0].Address);
            Assert.Equal(IPAddress.Parse("192.0.2.2"), servers[1].Address);
            Assert.Equal(IPAddress.Parse("192.0.2.3"), servers[2].Address);
        }

        [Fact]
        public void Parse_IPv6Nameserver_IsParsed()
        {
            List<IPEndPoint> servers = Parse("nameserver 2001:db8::1\n");

            IPEndPoint server = Assert.Single(servers);
            Assert.Equal(IPAddress.Parse("2001:db8::1"), server.Address);
            Assert.Equal(53, server.Port);
        }

        [Theory]
        [InlineData("# nameserver 192.0.2.1\n")]
        [InlineData("; nameserver 192.0.2.1\n")]
        [InlineData("\n   \n\t\n")]
        [InlineData("search example.com\noptions ndots:2\ndomain example.com\n")]
        public void Parse_NonNameserverContent_ReturnsEmpty(string contents)
        {
            Assert.Empty(Parse(contents));
        }

        [Fact]
        public void Parse_IgnoresOtherDirectivesAndComments()
        {
            List<IPEndPoint> servers = Parse(
                "# This is a comment\n" +
                "domain example.com\n" +
                "search example.com sub.example.com\n" +
                "nameserver 192.0.2.10\n" +
                "; trailing comment\n" +
                "options ndots:1 timeout:2\n" +
                "nameserver 192.0.2.20\n");

            Assert.Equal(2, servers.Count);
            Assert.Equal(IPAddress.Parse("192.0.2.10"), servers[0].Address);
            Assert.Equal(IPAddress.Parse("192.0.2.20"), servers[1].Address);
        }

        [Fact]
        public void Parse_TextAfterAddress_IsIgnored()
        {
            List<IPEndPoint> servers = Parse("nameserver 192.0.2.1 # primary resolver\n");

            IPEndPoint server = Assert.Single(servers);
            Assert.Equal(IPAddress.Parse("192.0.2.1"), server.Address);
        }

        [Fact]
        public void Parse_TabSeparatedNameserver_IsParsed()
        {
            List<IPEndPoint> servers = Parse("nameserver\t192.0.2.1\n");

            IPEndPoint server = Assert.Single(servers);
            Assert.Equal(IPAddress.Parse("192.0.2.1"), server.Address);
        }

        [Theory]
        [InlineData("nameserver\n")]
        [InlineData("nameserver \n")]
        [InlineData("nameserver not-an-ip\n")]
        [InlineData("nameserverextra 192.0.2.1\n")]
        public void Parse_InvalidNameserverLines_AreIgnored(string contents)
        {
            Assert.Empty(Parse(contents));
        }

        [Fact]
        public void Parse_ValidAndInvalidMixed_ReturnsOnlyValid()
        {
            List<IPEndPoint> servers = Parse(
                "nameserver not-an-ip\n" +
                "nameserver 192.0.2.1\n");

            IPEndPoint server = Assert.Single(servers);
            Assert.Equal(IPAddress.Parse("192.0.2.1"), server.Address);
        }
    }
}
