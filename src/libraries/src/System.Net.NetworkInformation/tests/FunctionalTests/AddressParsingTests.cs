// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class AddressParsingTests
    {
        [Fact]
        public static void GatewayAddressParsing()
        {
            FileUtil.NormalizeLineEndings("route", "route_normalized1");
            Collection<GatewayIPAddressInformation> gatewayAddresses = StringParsingHelpers.ParseGatewayAddressesFromRouteFile("route_normalized1", "wlan0");
            Assert.Equal(3, gatewayAddresses.Count);

            Assert.Equal(StringParsingHelpers.ParseHexIPAddress("0180690A"), gatewayAddresses[0].Address);
            Assert.Equal(StringParsingHelpers.ParseHexIPAddress("01234567"), gatewayAddresses[1].Address);
            Assert.Equal(StringParsingHelpers.ParseHexIPAddress("FEDCBA98"), gatewayAddresses[2].Address);
        }

        [Fact]
        public static void DhcpServerAddressParsing()
        {
            FileUtil.NormalizeLineEndings("dhclient.leases", "dhclient.leases_normalized0");
            Collection<IPAddress> dhcpServerAddresses = StringParsingHelpers.ParseDhcpServerAddressesFromLeasesFile("dhclient.leases_normalized0", "wlan0");
            Assert.Equal(1, dhcpServerAddresses.Count);
            Assert.Equal(IPAddress.Parse("10.105.128.4"), dhcpServerAddresses[0]);
        }

        [Fact]
        public static void WinsServerAddressParsing()
        {
            FileUtil.NormalizeLineEndings("smb.conf", "smb.conf_normalized");

            Collection<IPAddress> winsServerAddresses = StringParsingHelpers.ParseWinsServerAddressesFromSmbConfFile("smb.conf_normalized");
            Assert.Equal(1, winsServerAddresses.Count);
            Assert.Equal(IPAddress.Parse("255.1.255.1"), winsServerAddresses[0]);
        }
    }
}
