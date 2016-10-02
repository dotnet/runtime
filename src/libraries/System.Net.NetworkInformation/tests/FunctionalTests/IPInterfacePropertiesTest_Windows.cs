// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Net.Test.Common;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.NetworkInformation.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class IPInterfacePropertiesTest_Windows
    {
        private readonly ITestOutputHelper _log;

        public IPInterfacePropertiesTest_Windows()
        {
            _log = TestLogging.GetInstance();
        }

        [Fact]
        public void IPInfoTest_AccessAllProperties_NoErrors()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                _log.WriteLine("Nic: " + nic.Name);
                _log.WriteLine("- Supports IPv4: " + nic.Supports(NetworkInterfaceComponent.IPv4));
                _log.WriteLine("- Supports IPv6: " + nic.Supports(NetworkInterfaceComponent.IPv6));

                IPInterfaceProperties ipProperties = nic.GetIPProperties();

                Assert.NotNull(ipProperties);

                Assert.NotNull(ipProperties.AnycastAddresses);
                _log.WriteLine("- Anycast Addresses: " + ipProperties.AnycastAddresses.Count);
                foreach (IPAddressInformation anyAddr in ipProperties.AnycastAddresses)
                {
                    _log.WriteLine("-- " + anyAddr.Address.ToString());
                    _log.WriteLine("--- Dns Eligible: " + anyAddr.IsDnsEligible);
                    _log.WriteLine("--- Transient: " + anyAddr.IsTransient);
                }

                Assert.NotNull(ipProperties.DhcpServerAddresses);
                _log.WriteLine("- Dhcp Server Addresses: " + ipProperties.DhcpServerAddresses.Count);
                foreach (IPAddress dhcp in ipProperties.DhcpServerAddresses)
                {
                    _log.WriteLine("-- " + dhcp.ToString());
                }

                Assert.NotNull(ipProperties.DnsAddresses);
                _log.WriteLine("- Dns Addresses: " + ipProperties.DnsAddresses.Count);
                foreach (IPAddress dns in ipProperties.DnsAddresses)
                {
                    _log.WriteLine("-- " + dns.ToString());
                }

                Assert.NotNull(ipProperties.DnsSuffix);
                _log.WriteLine("- Dns Suffix: " + ipProperties.DnsSuffix);

                Assert.NotNull(ipProperties.GatewayAddresses);
                _log.WriteLine("- Gateway Addresses: " + ipProperties.GatewayAddresses.Count);
                foreach (GatewayIPAddressInformation gateway in ipProperties.GatewayAddresses)
                {
                    _log.WriteLine("-- " + gateway.Address.ToString());
                }

                _log.WriteLine("- Dns Enabled: " + ipProperties.IsDnsEnabled);

                _log.WriteLine("- Dynamic Dns Enabled: " + ipProperties.IsDynamicDnsEnabled);

                Assert.NotNull(ipProperties.MulticastAddresses);
                _log.WriteLine("- Multicast Addresses: " + ipProperties.MulticastAddresses.Count);
                foreach (IPAddressInformation multi in ipProperties.MulticastAddresses)
                {
                    _log.WriteLine("-- " + multi.Address.ToString());
                    _log.WriteLine("--- Dns Eligible: " + multi.IsDnsEligible);
                    _log.WriteLine("--- Transient: " + multi.IsTransient);
                }

                Assert.NotNull(ipProperties.UnicastAddresses);
                _log.WriteLine("- Unicast Addresses: " + ipProperties.UnicastAddresses.Count);
                foreach (UnicastIPAddressInformation uni in ipProperties.UnicastAddresses)
                {
                    _log.WriteLine("-- " + uni.Address.ToString());
                    _log.WriteLine("--- Preferred Lifetime: " + uni.AddressPreferredLifetime);
                    _log.WriteLine("--- Valid Lifetime: " + uni.AddressValidLifetime);
                    _log.WriteLine("--- Dhcp lease Lifetime: " + uni.DhcpLeaseLifetime);
                    _log.WriteLine("--- Duplicate Address Detection State: " + uni.DuplicateAddressDetectionState);

                    Assert.NotNull(uni.IPv4Mask);
                    _log.WriteLine("--- IPv4 Mask: " + uni.IPv4Mask);
                    _log.WriteLine("--- Dns Eligible: " + uni.IsDnsEligible);
                    _log.WriteLine("--- Transient: " + uni.IsTransient);
                    _log.WriteLine("--- Prefix Origin: " + uni.PrefixOrigin);
                    _log.WriteLine("--- Suffix Origin: " + uni.SuffixOrigin);

                    // Prefix Length
                    _log.WriteLine("--- Prefix Length: " + uni.PrefixLength);
                    Assert.NotEqual(0, uni.PrefixLength);
                }

                Assert.NotNull(ipProperties.WinsServersAddresses);
                _log.WriteLine("- Wins Addresses: " + ipProperties.WinsServersAddresses.Count);
                foreach (IPAddress wins in ipProperties.WinsServersAddresses)
                {
                    _log.WriteLine("-- " + wins.ToString());
                }
            }
        }

        [Fact]
        public void IPInfoTest_AccessAllIPv4Properties_NoErrors()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                _log.WriteLine("Nic: " + nic.Name);

                IPInterfaceProperties ipProperties = nic.GetIPProperties();

                _log.WriteLine("IPv4 Properties:");

                if (!nic.Supports(NetworkInterfaceComponent.IPv4))
                {
                    var nie = Assert.Throws<NetworkInformationException>(() => ipProperties.GetIPv4Properties());
                    Assert.Equal(SocketError.ProtocolNotSupported, (SocketError)nie.ErrorCode);
                    continue;
                }

                IPv4InterfaceProperties ipv4Properties = ipProperties.GetIPv4Properties();

                _log.WriteLine("Index: " + ipv4Properties.Index);
                _log.WriteLine("IsAutomaticPrivateAddressingActive: " + ipv4Properties.IsAutomaticPrivateAddressingActive);
                _log.WriteLine("IsAutomaticPrivateAddressingEnabled: " + ipv4Properties.IsAutomaticPrivateAddressingEnabled);
                _log.WriteLine("IsDhcpEnabled: " + ipv4Properties.IsDhcpEnabled);
                _log.WriteLine("IsForwardingEnabled: " + ipv4Properties.IsForwardingEnabled);
                _log.WriteLine("Mtu: " + ipv4Properties.Mtu);
                _log.WriteLine("UsesWins: " + ipv4Properties.UsesWins);
            }
        }

        [Fact]
        public void IPInfoTest_AccessAllIPv6Properties_NoErrors()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                _log.WriteLine("Nic: " + nic.Name);

                IPInterfaceProperties ipProperties = nic.GetIPProperties();

                _log.WriteLine("IPv6 Properties:");

                if (!nic.Supports(NetworkInterfaceComponent.IPv6))
                {
                    var nie = Assert.Throws<NetworkInformationException>(() => ipProperties.GetIPv6Properties());
                    Assert.Equal(SocketError.ProtocolNotSupported, (SocketError)nie.ErrorCode);
                    continue;
                }

                IPv6InterfaceProperties ipv6Properties = ipProperties.GetIPv6Properties();

                if (ipv6Properties == null)
                {
                    _log.WriteLine("IPv6Properties is null");
                    continue;
                }

                _log.WriteLine("Index: " + ipv6Properties.Index);
                _log.WriteLine("Mtu: " + ipv6Properties.Mtu);
                _log.WriteLine("ScopeID: " + ipv6Properties.GetScopeId(ScopeLevel.Link));
            }
        }

        [Fact]
        [Trait("IPv6", "true")]
        public void IPv6ScopeId_GetLinkLevel_MatchesIndex()
        {
            Assert.True(Capability.IPv6Support());

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProperties = nic.GetIPProperties();

                if (!nic.Supports(NetworkInterfaceComponent.IPv6))
                {
                    continue;
                }

                IPv6InterfaceProperties ipv6Properties = ipProperties.GetIPv6Properties();
                // This is not officially guaranteed by Windows, but it's what gets used.
                Assert.Equal(ipv6Properties.Index, ipv6Properties.GetScopeId(ScopeLevel.Link));
            }
        }

        [Fact]
        [Trait("IPv6", "true")]
        public void IPv6ScopeId_AccessAllValues_Success()
        {
            Assert.True(Capability.IPv6Support());

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                _log.WriteLine("Nic: " + nic.Name);

                if (!nic.Supports(NetworkInterfaceComponent.IPv6))
                {
                    continue;
                }

                IPInterfaceProperties ipProperties = nic.GetIPProperties();

                _log.WriteLine("- IPv6 Scope levels:");

                IPv6InterfaceProperties ipv6Properties = ipProperties.GetIPv6Properties();

                Array values = Enum.GetValues(typeof(ScopeLevel));
                foreach (ScopeLevel level in values)
                {
                    _log.WriteLine("-- Level: " + level + "; " + ipv6Properties.GetScopeId(level));
                }
            }
        }
    }
}
