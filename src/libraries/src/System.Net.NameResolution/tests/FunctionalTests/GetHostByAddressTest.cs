// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0618 // use of obsolete methods

using System.Net.Sockets;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class GetHostByAddressTest
    {
        [Fact]

        public void DnsObsoleteGetHostByAddress_BadIPString_Throws()
        {
            Assert.Throws<FormatException>(() => Dns.GetHostByAddress("badIPString"));
        }

        [Fact]
        public void DnsObsoleteGetHostByAddress_LoopbackString_ReturnsSameAsLoopbackIP()
        {
            IPHostEntry stringEntry = Dns.GetHostByAddress(IPAddress.Loopback.ToString());
            IPHostEntry entry = Dns.GetHostByAddress(IPAddress.Loopback);

            Assert.Equal(entry.HostName, stringEntry.HostName);
            Assert.Equal(entry.AddressList, stringEntry.AddressList);
        }

        [Fact]
        public void DnsObsoleteGetHostByAddress_UnknownIP_Throws()
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostByAddress("0.0.1.1"));
        }
    }
}
