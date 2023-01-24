// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.Functional.Tests;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.NetworkInformation.Tests
{
    [PlatformSpecific(TestPlatforms.Linux)]
    [SkipOnPlatform(TestPlatforms.LinuxBionic, "Bionic is not normal Linux, has no normal /proc")]
    public class IPInterfacePropertiesTest_Linux
    {
        private readonly ITestOutputHelper _log;

        public IPInterfacePropertiesTest_Linux(ITestOutputHelper output)
        {
            _log = output;
        }

        [Fact]
        public async Task IPInfoTest_AccessAllProperties_NoErrors()
        {
            await Task.Run(() =>
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    _log.WriteLine("Nic: " + nic.Name);
                    _log.WriteLine("- Speed:" + nic.Speed);
                    _log.WriteLine("- Supports IPv4: " + nic.Supports(NetworkInterfaceComponent.IPv4));
                    _log.WriteLine("- Supports IPv6: " + nic.Supports(NetworkInterfaceComponent.IPv6));
                    Assert.False(nic.IsReceiveOnly);

                    IPInterfaceProperties ipProperties = nic.GetIPProperties();

                    Assert.NotNull(ipProperties);

                    Assert.Throws<PlatformNotSupportedException>(() => ipProperties.AnycastAddresses);

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

                    Assert.Throws<PlatformNotSupportedException>(() => ipProperties.IsDynamicDnsEnabled);

                    Assert.NotNull(ipProperties.MulticastAddresses);
                    _log.WriteLine("- Multicast Addresses: " + ipProperties.MulticastAddresses.Count);
                    foreach (IPAddressInformation multi in ipProperties.MulticastAddresses)
                    {
                        _log.WriteLine("-- " + multi.Address.ToString());
                        Assert.Throws<PlatformNotSupportedException>(() => multi.IsDnsEligible);
                        Assert.Throws<PlatformNotSupportedException>(() => multi.IsTransient);
                    }

                    Assert.NotNull(ipProperties.UnicastAddresses);
                    _log.WriteLine("- Unicast Addresses: " + ipProperties.UnicastAddresses.Count);
                    foreach (UnicastIPAddressInformation uni in ipProperties.UnicastAddresses)
                    {
                        _log.WriteLine("-- " + uni.Address.ToString());
                        Assert.Throws<PlatformNotSupportedException>(() => uni.AddressPreferredLifetime);
                        Assert.Throws<PlatformNotSupportedException>(() => uni.AddressValidLifetime);
                        Assert.Throws<PlatformNotSupportedException>(() => uni.DhcpLeaseLifetime);
                        Assert.Throws<PlatformNotSupportedException>(() => uni.DuplicateAddressDetectionState);

                        Assert.NotNull(uni.IPv4Mask);
                        _log.WriteLine("--- IPv4 Mask: " + uni.IPv4Mask);
                        Assert.Throws<PlatformNotSupportedException>(() => uni.IsDnsEligible);
                        Assert.Throws<PlatformNotSupportedException>(() => uni.IsTransient);
                        Assert.Throws<PlatformNotSupportedException>(() => uni.PrefixOrigin);
                        Assert.Throws<PlatformNotSupportedException>(() => uni.SuffixOrigin);

                        // Prefix Length
                        _log.WriteLine("--- PrefixLength: " + uni.PrefixLength);
                        Assert.True(uni.PrefixLength > 0);
                        Assert.True((uni.Address.AddressFamily == AddressFamily.InterNetwork ? 33 : 129) > uni.PrefixLength);
                    }

                    Assert.NotNull(ipProperties.WinsServersAddresses);
                    _log.WriteLine("- Wins Addresses: " + ipProperties.WinsServersAddresses.Count);
                    foreach (IPAddress wins in ipProperties.WinsServersAddresses)
                    {
                        _log.WriteLine("-- " + wins.ToString());
                    }
                }
            }).WaitAsync(TestHelper.PassingTestTimeout);
        }

        [Fact]
        public async Task IPInfoTest_AccessAllIPv4Properties_NoErrors()
        {
            await Task.Run(() =>
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    _log.WriteLine("Nic: " + nic.Name);

                    IPInterfaceProperties ipProperties = nic.GetIPProperties();

                    _log.WriteLine("IPv4 Properties:");

                    IPv4InterfaceProperties ipv4Properties = ipProperties.GetIPv4Properties();

                    _log.WriteLine("Index: " + ipv4Properties.Index);
                    Assert.Throws<PlatformNotSupportedException>(() => ipv4Properties.IsAutomaticPrivateAddressingActive);
                    Assert.Throws<PlatformNotSupportedException>(() => ipv4Properties.IsAutomaticPrivateAddressingEnabled);
                    Assert.Throws<PlatformNotSupportedException>(() => ipv4Properties.IsDhcpEnabled);
                    _log.WriteLine("IsForwardingEnabled: " + ipv4Properties.IsForwardingEnabled);
                    _log.WriteLine("Mtu: " + ipv4Properties.Mtu);
                    _log.WriteLine("UsesWins: " + ipv4Properties.UsesWins);
                }
            }).WaitAsync(TestHelper.PassingTestTimeout);
        }

        [Fact]
        public async Task IPInfoTest_AccessAllIPv6Properties_NoErrors()
        {
            await Task.Run(() =>
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    _log.WriteLine("Nic: " + nic.Name);

                    IPInterfaceProperties ipProperties = nic.GetIPProperties();

                    _log.WriteLine("IPv6 Properties:");

                    IPv6InterfaceProperties ipv6Properties = ipProperties.GetIPv6Properties();

                    if (ipv6Properties == null)
                    {
                        _log.WriteLine("IPv6Properties is null");
                        continue;
                    }

                    _log.WriteLine("Index: " + ipv6Properties.Index);
                    _log.WriteLine("Mtu: " + ipv6Properties.Mtu);
                    _log.WriteLine("Scope: " + ipv6Properties.GetScopeId(ScopeLevel.Link));
                }
            }).WaitAsync(TestHelper.PassingTestTimeout);
        }

        [Trait("IPv6", "true")]
        [ConditionalFact(typeof(Socket), nameof(Socket.OSSupportsIPv6))]
        public async Task IPv6ScopeId_AccessAllValues_Success()
        {
            await Task.Run(() =>
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

                    IPv6InterfaceProperties ipv6Properties = ipProperties.GetIPv6Properties();

                    Array values = Enum.GetValues(typeof(ScopeLevel));
                    foreach (ScopeLevel level in values)
                    {
                        _log.WriteLine("-- Level: " + level + "; " + ipv6Properties.GetScopeId(level));
                    }
                }
            }).WaitAsync(TestHelper.PassingTestTimeout);
        }

        [Fact]
        [Trait("IPv4", "true")]
        public async Task IPInfoTest_IPv4Loopback_ProperAddress()
        {
            await Task.Run(() =>
            {
                Assert.True(Capability.IPv4Support());

                _log.WriteLine("Loopback IPv4 index: " + NetworkInterface.LoopbackInterfaceIndex);

                NetworkInterface loopback = NetworkInterface.GetAllNetworkInterfaces().First(ni => ni.Name == "lo");
                Assert.NotNull(loopback);

                foreach (UnicastIPAddressInformation unicast in loopback.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.Equals(IPAddress.Loopback))
                    {
                        Assert.Equal(IPAddress.Parse("255.0.0.0"), unicast.IPv4Mask);
                        Assert.Equal(8, unicast.PrefixLength);
                        break;
                    }
                }
            }).WaitAsync(TestHelper.PassingTestTimeout);
        }

        [Trait("IPv6", "true")]
        [ConditionalFact(typeof(Socket), nameof(Socket.OSSupportsIPv6))]
        public async Task IPInfoTest_IPv6Loopback_ProperAddress()
        {
            await Task.Run(() =>
            {
                Assert.True(Capability.IPv6Support());

                _log.WriteLine("Loopback IPv6 index: " + NetworkInterface.IPv6LoopbackInterfaceIndex);

                NetworkInterface loopback = NetworkInterface.GetAllNetworkInterfaces().First(ni => ni.Name == "lo");
                Assert.NotNull(loopback);

                foreach (UnicastIPAddressInformation unicast in loopback.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.Equals(IPAddress.IPv6Loopback))
                    {
                        Assert.Equal(128, unicast.PrefixLength);
                        break;
                    }
                }
            }).WaitAsync(TestHelper.PassingTestTimeout);
        }
    }
}
