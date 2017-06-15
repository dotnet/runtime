// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class AddressParsingTests : FileCleanupTestBase
    {
        [Fact]
        public void GatewayAddressParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("route", fileName);
            List<GatewayIPAddressInformation> gatewayAddresses = StringParsingHelpers.ParseGatewayAddressesFromRouteFile(fileName, "wlan0");
            Assert.Equal(3, gatewayAddresses.Count);

            Assert.Equal(StringParsingHelpers.ParseHexIPAddress("0180690A"), gatewayAddresses[0].Address);
            Assert.Equal(StringParsingHelpers.ParseHexIPAddress("01234567"), gatewayAddresses[1].Address);
            Assert.Equal(StringParsingHelpers.ParseHexIPAddress("FEDCBA98"), gatewayAddresses[2].Address);
        }

        [Fact]
        public void DhcpServerAddressParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("dhclient.leases", fileName);
            List<IPAddress> dhcpServerAddresses = StringParsingHelpers.ParseDhcpServerAddressesFromLeasesFile(fileName, "wlan0");
            Assert.Equal(1, dhcpServerAddresses.Count);
            Assert.Equal(IPAddress.Parse("10.105.128.4"), dhcpServerAddresses[0]);
        }

        [Fact]
        public void WinsServerAddressParsing()
        {
            string fileName = GetTestFilePath();
            FileUtil.NormalizeLineEndings("smb.conf", fileName);

            List<IPAddress> winsServerAddresses = StringParsingHelpers.ParseWinsServerAddressesFromSmbConfFile(fileName);
            Assert.Equal(1, winsServerAddresses.Count);
            Assert.Equal(IPAddress.Parse("255.1.255.1"), winsServerAddresses[0]);
        }
    }
}
