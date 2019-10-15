// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class LdapDirectoryIdentifierTests
    {
        [Theory]
        [InlineData(null, new string[0])]
        [InlineData("", new string[] { "" })]
        [InlineData("server", new string[] { "server" })]
        public void Ctor_Server(string server, string[] expectedServers)
        {
            var identifier = new LdapDirectoryIdentifier(server);
            Assert.False(identifier.Connectionless);
            Assert.False(identifier.FullyQualifiedDnsHostName);
            Assert.Equal(389, identifier.PortNumber);
            Assert.Equal(expectedServers, identifier.Servers);
        }

        [Theory]
        [InlineData(null, 389, new string[0])]
        [InlineData("", -1, new string[] { "" })]
        [InlineData("server", int.MaxValue, new string[] { "server" })]
        public void Ctor_Server_PortNumber(string server, int portNumber, string[] expectedServers)
        {
            var identifier = new LdapDirectoryIdentifier(server, portNumber);
            Assert.False(identifier.Connectionless);
            Assert.False(identifier.FullyQualifiedDnsHostName);
            Assert.Equal(portNumber, identifier.PortNumber);
            Assert.Equal(expectedServers, identifier.Servers);
        }

        [Theory]
        [InlineData(null, true, false, new string[0])]
        [InlineData("", false, true, new string[] { "" })]
        [InlineData("server", true, true, new string[] { "server" })]
        public void Ctor_Server_FullQualifiedDnsHostName_Conectionless(string server, bool fullyQualifiedDnsHostName, bool connectionless, string[] expectedServers)
        {
            var identifier = new LdapDirectoryIdentifier(server, fullyQualifiedDnsHostName, connectionless);
            Assert.Equal(connectionless, identifier.Connectionless);
            Assert.Equal(fullyQualifiedDnsHostName, identifier.FullyQualifiedDnsHostName);
            Assert.Equal(389, identifier.PortNumber);
            Assert.Equal(expectedServers, identifier.Servers);
        }

        [Theory]
        [InlineData(null, -1, true, false, new string[0])]
        [InlineData("", 389, false, true, new string[] { "" })]
        [InlineData("server", int.MaxValue, true, true, new string[] { "server" })]
        public void Ctor_PortNumber_Server_FullQualifiedDnsHostName_Conectionless(string server, int portNumber, bool fullyQualifiedDnsHostName, bool connectionless, string[] expectedServers)
        {
            var identifier = new LdapDirectoryIdentifier(server, portNumber, fullyQualifiedDnsHostName, connectionless);
            Assert.Equal(connectionless, identifier.Connectionless);
            Assert.Equal(fullyQualifiedDnsHostName, identifier.FullyQualifiedDnsHostName);
            Assert.Equal(portNumber, identifier.PortNumber);
            Assert.Equal(expectedServers, identifier.Servers);
        }

        [Theory]
        [InlineData(null, false, true)]
        [InlineData(new string[0], true, false)]
        [InlineData(new string[] { "server" }, true, true)]
        [InlineData(new string[] { "server", null }, false, false)]
        public void Ctor_Servers_FullQualifiedDnsHostName_Conectionless(string[] servers, bool fullyQualifiedDnsHostName, bool connectionless)
        {
            var identifier = new LdapDirectoryIdentifier(servers, fullyQualifiedDnsHostName, connectionless);
            Assert.Equal(connectionless, identifier.Connectionless);
            Assert.Equal(fullyQualifiedDnsHostName, identifier.FullyQualifiedDnsHostName);
            Assert.Equal(389, identifier.PortNumber);
            Assert.NotSame(servers, identifier.Servers);
            Assert.Equal(servers ?? Array.Empty<string>(), identifier.Servers);
        }

        [Fact]
        public void Ctor_ServerHasSpaceInName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapDirectoryIdentifier("se rver"));
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapDirectoryIdentifier("se rver", 0));
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapDirectoryIdentifier("se rver", false, false));
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapDirectoryIdentifier("se rver", 0, false, false));
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapDirectoryIdentifier(new string[] { "se rver" }, false, false));
            AssertExtensions.Throws<ArgumentException>(null, () => new LdapDirectoryIdentifier(new string[] { "se rver" }, 0, false, false));
        }
    }
}
