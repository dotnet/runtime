// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class GetHostEntryTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Dns_GetHostEntryAsync_IPAddress_Ok()
        {
            IPAddress localIPAddress = await TestSettings.GetLocalIPAddress();
            await TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(localIPAddress));
        }


        public static bool GetHostEntryWorks =
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/27622")]
            PlatformDetection.IsNotArmNorArm64Process &&
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/1488", TestPlatforms.OSX)]
            !PlatformDetection.IsOSX &&
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/51377", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
            !PlatformDetection.IsiOS && !PlatformDetection.IstvOS && !PlatformDetection.IsMacCatalyst;

        [ConditionalTheory(nameof(GetHostEntryWorks))]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public async Task Dns_GetHostEntry_HostString_Ok(string hostName)
        {
            try
            {
                await TestGetHostEntryAsync(() => Task.FromResult(Dns.GetHostEntry(hostName)));
            }
            catch (Exception ex) when (hostName == "")
            {
                // Additional data for debugging sporadic CI failures https://github.com/dotnet/runtime/issues/1488
                string actualHostName = Dns.GetHostName();
                string etcHosts = "";
                Exception getHostEntryException = null;
                Exception etcHostsException = null;

                try
                {
                    Dns.GetHostEntry(actualHostName);
                }
                catch (Exception e2)
                {
                    getHostEntryException = e2;
                }

                try
                {
                    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    {
                        etcHosts = File.ReadAllText("/etc/hosts");
                    }
                }
                catch (Exception e2)
                {
                    etcHostsException = e2;
                }

                throw new Exception(
                    $"Failed for empty hostname.{Environment.NewLine}" +
                    $"Dns.GetHostName() == {actualHostName}{Environment.NewLine}" +
                    $"{nameof(getHostEntryException)}=={getHostEntryException}{Environment.NewLine}" +
                    $"{nameof(etcHostsException)}=={etcHostsException}{Environment.NewLine}" +
                    $"/etc/host =={Environment.NewLine}{etcHosts}",
                    ex);
            }
        }

        [ConditionalTheory(nameof(GetHostEntryWorks))]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public async Task Dns_GetHostEntryAsync_HostString_Ok(string hostName)
        {
            await TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(hostName));
        }

        [Fact]
        public async Task Dns_GetHostEntryAsync_IPString_Ok() =>
            await TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(TestSettings.LocalIPString));

        private static async Task TestGetHostEntryAsync(Func<Task<IPHostEntry>> getHostEntryFunc)
        {
            Task<IPHostEntry> hostEntryTask1 = getHostEntryFunc();
            Task<IPHostEntry> hostEntryTask2 = getHostEntryFunc();

            await TestSettings.WhenAllOrAnyFailedWithTimeout(hostEntryTask1, hostEntryTask2);

            IPAddress[] list1 = hostEntryTask1.Result.AddressList;
            IPAddress[] list2 = hostEntryTask2.Result.AddressList;

            Assert.NotNull(list1);
            Assert.NotNull(list2);
            Assert.Equal<IPAddress>(list1, list2);
        }

        public static bool GetHostEntry_DisableIPv6_Condition = GetHostEntryWorks && RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(GetHostEntry_DisableIPv6_Condition))]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public void Dns_GetHostEntry_DisableIPv6_ExcludesIPv6Addresses(string hostnameOuter)
        {
            RemoteExecutor.Invoke(RunTest, hostnameOuter).Dispose();

            static void RunTest(string hostnameInner)
            {
                AppContext.SetSwitch("System.Net.DisableIPv6", true);
                IPHostEntry entry = Dns.GetHostEntry(hostnameInner);
                foreach (IPAddress address in entry.AddressList)
                {
                    Assert.NotEqual(AddressFamily.InterNetworkV6, address.AddressFamily);
                }
            }
        }

        [ConditionalTheory(nameof(GetHostEntry_DisableIPv6_Condition))]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public void Dns_GetHostEntryAsync_DisableIPv6_ExcludesIPv6Addresses(string hostnameOuter)
        {
            RemoteExecutor.Invoke(RunTest, hostnameOuter).Dispose();

            static async Task RunTest(string hostnameInner)
            {
                AppContext.SetSwitch("System.Net.DisableIPv6", true);
                IPHostEntry entry = await Dns.GetHostEntryAsync(hostnameInner);
                foreach (IPAddress address in entry.AddressList)
                {
                    Assert.NotEqual(AddressFamily.InterNetworkV6, address.AddressFamily);
                }
            }
        }

        [Fact]
        public async Task Dns_GetHostEntry_NullStringHost_Fail()
        {
            Assert.Throws<ArgumentNullException>(() => Dns.GetHostEntry((string)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((string)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, (string)null, null));
        }

        [Fact]
        public async Task Dns_GetHostEntryAsync_NullIPAddressHost_Fail()
        {
            Assert.Throws<ArgumentNullException>(() => Dns.GetHostEntry((IPAddress)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((IPAddress)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, (IPAddress)null, null));
        }

        public static IEnumerable<object[]> GetInvalidAddresses()
        {
            yield return new object[] { IPAddress.Any };
            yield return new object[] { IPAddress.IPv6Any };
            yield return new object[] { IPAddress.IPv6None };
        }

        [Theory]
        [MemberData(nameof(GetInvalidAddresses))]
        public async Task Dns_GetHostEntry_AnyIPAddress_Fail(IPAddress address)
        {
            Assert.Throws<ArgumentException>(() => Dns.GetHostEntry(address));
            Assert.Throws<ArgumentException>(() => Dns.GetHostEntry(address.ToString()));

            await Assert.ThrowsAsync<ArgumentException>(() => Dns.GetHostEntryAsync(address));
            await Assert.ThrowsAsync<ArgumentException>(() => Dns.GetHostEntryAsync(address.ToString()));

            await Assert.ThrowsAsync<ArgumentException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address, null));
            await Assert.ThrowsAsync<ArgumentException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address.ToString(), null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task DnsGetHostEntry_MachineName_AllVariationsMatch()
        {
            IPHostEntry syncResult = Dns.GetHostEntry(TestSettings.LocalHost);
            IPHostEntry apmResult = Dns.EndGetHostEntry(Dns.BeginGetHostEntry(TestSettings.LocalHost, null, null));
            IPHostEntry asyncResult = await Dns.GetHostEntryAsync(TestSettings.LocalHost);

            Assert.Equal(syncResult.HostName, apmResult.HostName);
            Assert.Equal(syncResult.HostName, asyncResult.HostName);

            Assert.Equal(syncResult.AddressList, apmResult.AddressList);
            Assert.Equal(syncResult.AddressList, asyncResult.AddressList);
        }

        [Fact]
        public async Task DnsGetHostEntry_Loopback_AllVariationsMatch()
        {
            IPHostEntry syncResult = Dns.GetHostEntry(IPAddress.Loopback);
            IPHostEntry apmResult = Dns.EndGetHostEntry(Dns.BeginGetHostEntry(IPAddress.Loopback, null, null));
            IPHostEntry asyncResult = await Dns.GetHostEntryAsync(IPAddress.Loopback);

            Assert.Equal(syncResult.HostName, apmResult.HostName);
            Assert.Equal(syncResult.HostName, asyncResult.HostName);

            Assert.Equal(syncResult.AddressList, apmResult.AddressList);
            Assert.Equal(syncResult.AddressList, asyncResult.AddressList);
        }

        [Theory]
        [InlineData("BadName")] // unknown name
        [InlineData("0.0.1.1")] // unknown address
        [InlineData("Test-\u65B0-Unicode")] // unknown unicode name
        [InlineData("xn--test--unicode-0b01a")] // unknown punicode name
        [InlineData("Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Are")] // very long name but not too long
        public async Task DnsGetHostEntry_BadName_ThrowsSocketException(string hostNameOrAddress)
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(hostNameOrAddress));
            await Assert.ThrowsAnyAsync<SocketException>(() => Dns.GetHostEntryAsync(hostNameOrAddress));
            await Assert.ThrowsAnyAsync<SocketException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, hostNameOrAddress, null));
        }

        [Theory]
        [InlineData("Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Aret")]
        public async Task DnsGetHostEntry_BadName_ThrowsArgumentOutOfRangeException(string hostNameOrAddress)
        {
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Dns.GetHostEntry(hostNameOrAddress));
            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => Dns.GetHostEntryAsync(hostNameOrAddress));
            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, hostNameOrAddress, null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task DnsGetHostEntry_LocalHost_ReturnsFqdnAndLoopbackIPs(int mode)
        {
            IPHostEntry entry = mode switch
            {
                0 => Dns.GetHostEntry("localhost"),
                1 => await Dns.GetHostEntryAsync("localhost"),
                _ => await Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, "localhost", null)
            };

            Assert.NotNull(entry.HostName);
            Assert.True(entry.HostName.Length > 0, "Empty host name");
            Assert.True(entry.AddressList.Length >= 1, "No local IPs");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), "Not a loopback address: " + addr));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task DnsGetHostEntry_LoopbackIP_MatchesGetHostEntryLoopbackString(int mode)
        {
            IPAddress address = IPAddress.Loopback;

            IPHostEntry ipEntry = mode switch
            {
                0 => Dns.GetHostEntry(address),
                1 => await Dns.GetHostEntryAsync(address),
                _ => await Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address, null)
            };
            IPHostEntry stringEntry = mode switch
            {
                0 => Dns.GetHostEntry(address.ToString()),
                1 => await Dns.GetHostEntryAsync(address.ToString()),
                _ => await Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address.ToString(), null)
            };

            Assert.Equal(ipEntry.HostName, stringEntry.HostName);
            Assert.Equal(ipEntry.AddressList, stringEntry.AddressList);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(AddressFamilySpecificTestData))]
        public async Task DnsGetHostEntry_LocalHost_AddressFamilySpecific(bool useAsync, string host, AddressFamily addressFamily)
        {
            IPHostEntry entry =
                useAsync ? await Dns.GetHostEntryAsync(host, addressFamily) :
                Dns.GetHostEntry(host, addressFamily);

            Assert.All(entry.AddressList, address => Assert.Equal(addressFamily, address.AddressFamily));
        }

        public static TheoryData<bool, string, AddressFamily> AddressFamilySpecificTestData =>
            new TheoryData<bool, string, AddressFamily>()
            {
                // async, hostname, af
                { false, TestSettings.IPv4Host, AddressFamily.InterNetwork },
                { false, TestSettings.IPv6Host, AddressFamily.InterNetworkV6 },
                { true, TestSettings.IPv4Host, AddressFamily.InterNetwork },
                { true, TestSettings.IPv6Host, AddressFamily.InterNetworkV6 }
            };

        [Fact]
        public async Task DnsGetHostEntry_PreCancelledToken_Throws()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Dns.GetHostEntryAsync(TestSettings.LocalHost, cts.Token));
            Assert.Equal(cts.Token, oce.CancellationToken);
        }
    }

    // Cancellation tests are sequential to reduce the chance of timing issues.
    [Collection(nameof(DisableParallelization))]
    public class GetHostEntryTest_Cancellation
    {
        [Fact]
        [OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33378", TestPlatforms.AnyUnix)] // Cancellation of an outstanding getaddrinfo is not supported on *nix.
        [SkipOnCoreClr("JitStress interferes with cancellation timing", RuntimeTestModes.JitStress | RuntimeTestModes.JitStressRegs)]
        public async Task DnsGetHostEntry_PostCancelledToken_Throws()
        {
            // Windows 7 name resolution is synchronous and does not respect cancellation.
            if (PlatformDetection.IsWindows7)
                return;

            using var cts = new CancellationTokenSource();

            Task task = Dns.GetHostEntryAsync(TestSettings.UncachedHost, cts.Token);

            // This test might flake if the cancellation token takes too long to trigger:
            // It's a race between the DNS server getting back to us and the cancellation processing.
            cts.Cancel();

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(cts.Token, oce.CancellationToken);
        }
    }
}
