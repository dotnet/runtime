// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0618 // use of obsolete methods

using System.Net.Sockets;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class ResolveTest
    {
        [Fact]
        public void DnsObsoleteBeginResolve_BadName_Throws()
        {
            IAsyncResult asyncObject = Dns.BeginResolve("BadName", null, null);
            Assert.ThrowsAny<SocketException>(() => Dns.EndResolve(asyncObject));
        }

        [Fact]
        public void DnsObsoleteBeginResolve_BadIPv4String_ReturnsOnlyGivenIP()
        {
            IAsyncResult asyncObject = Dns.BeginResolve("0.0.1.1", null, null);
            IPHostEntry entry = Dns.EndResolve(asyncObject);

            Assert.Equal("0.0.1.1", entry.HostName);
            Assert.Equal(1, entry.AddressList.Length);
            Assert.Equal(IPAddress.Parse("0.0.1.1"), entry.AddressList[0]);
        }

        [Fact]
        public void DnsObsoleteBeginResolve_Loopback_MatchesResolve()
        {
            IAsyncResult asyncObject = Dns.BeginResolve(IPAddress.Loopback.ToString(), null, null);
            IPHostEntry results = Dns.EndResolve(asyncObject);
            IPHostEntry entry = Dns.Resolve(IPAddress.Loopback.ToString());

            Assert.Equal(entry.HostName, results.HostName);
            Assert.Equal(entry.AddressList, results.AddressList);
        }

        [Fact]
        public void DnsObsoleteResolve_BadName_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.Resolve("BadName"));
        }

        [Fact]
        public void DnsObsoleteResolve_BadIP_ReturnsIPasNameAndIP()
        {
            IPHostEntry entry = Dns.Resolve("0.0.1.1");

            Assert.Equal("0.0.1.1", entry.HostName);
            Assert.Equal(1, entry.AddressList.Length);
            Assert.Equal(IPAddress.Parse("0.0.1.1"), entry.AddressList[0]);
        }
    }
}
