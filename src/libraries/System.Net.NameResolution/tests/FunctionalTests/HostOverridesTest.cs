// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class HostAliasesTest : FileCleanupTestBase
    {
        private const string HostsEnvironmentVariableName = "DOTNET_SYSTEM_NET_HOSTS";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AllHookedEntrypoints_LookupsFindExpectedData()
        {
            string hostsPath = CreateTestFile();
            File.WriteAllText(hostsPath, $"""
                # This is a sample HOSTS file
                #
                102.54.94.97     rhino.acme.com  # something
                 38.25.63.10     x.acme.com      # something else
                ::1              example

                nonparsableip
                .....
                1.2..3 test

                ::1  invalid$$$host$@
                1.1.1.1 firstnameonline    secondnameonline   thirdnameonline secondnameonline

                1.2.3.4          example2
                ::2          example2
                ::1  localhost
                ::1  anothermappedto1
                """);

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[HostsEnvironmentVariableName] = hostsPath;
            options.TimeOut = -1;

            RemoteExecutor.Invoke(async () =>
            {
                // Validate name-to-addresses lookups
                await AssertExpectedAddresses("rhino.acme.com", new[] { IPAddress.Parse("102.54.94.97") });
                await AssertExpectedAddresses("example", new[] { IPAddress.IPv6Loopback });
                await AssertExpectedAddresses("example2", new[] { IPAddress.Parse("1.2.3.4"), IPAddress.Parse("::2") });
                await AssertExpectedAddresses("localhost", new[] { IPAddress.Parse("::1") });
                await AssertExpectedAddresses("firstnameonline", new[] { IPAddress.Parse("1.1.1.1") });
                await AssertExpectedAddresses("secondnameonline", new[] { IPAddress.Parse("1.1.1.1") });
                await AssertExpectedAddresses("thirdnameonline", new[] { IPAddress.Parse("1.1.1.1") });
                await AssertExpectedAddressesFiltered("rhino.acme.com", new[] { IPAddress.Parse("102.54.94.97") }, AddressFamily.InterNetwork);
                await AssertExpectedAddressesFiltered("example", new[] { IPAddress.IPv6Loopback }, AddressFamily.InterNetworkV6);
                await AssertExpectedAddressesFiltered("example2", new[] { IPAddress.Parse("1.2.3.4") }, AddressFamily.InterNetwork);
                await AssertExpectedAddressesFiltered("example2", new[] { IPAddress.Parse("1.2.3.4"), IPAddress.Parse("::2") }, AddressFamily.Unspecified);
                await AssertExpectedAddressesFiltered("example2", new[] { IPAddress.Parse("::2") }, AddressFamily.InterNetworkV6);
                await AssertExpectedAddressesFiltered("localhost", new[] { IPAddress.Parse("::1") }, AddressFamily.InterNetworkV6);
                await AssertExpectedAddressesFiltered("anothermappedto1", new[] { IPAddress.Parse("::1") }, AddressFamily.InterNetworkV6);
                static async Task AssertExpectedAddresses(string hostName, IPAddress[] expected)
                {
                    Assert.Equal(expected, Dns.GetHostAddresses(hostName));
                    Assert.Equal(expected, await Dns.GetHostAddressesAsync(hostName));
                    Assert.Equal(expected, await Dns.GetHostAddressesAsync(hostName, AddressFamily.Unspecified));
                    Assert.Equal(expected, await Dns.GetHostAddressesAsync(hostName, CancellationToken.None));
                    Assert.Equal(expected, Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(hostName, null, null)));
                }
                static async Task AssertExpectedAddressesFiltered(string hostName, IPAddress[] expected, AddressFamily addressFamily)
                {
                    Assert.Equal(expected, await Dns.GetHostAddressesAsync(hostName, addressFamily));
                }

                IPHostEntry expectedEntry = new IPHostEntry { HostName = "rhino.acme.com", AddressList = new[] { IPAddress.Parse("102.54.94.97") }, Aliases = Array.Empty<string>() };
                AssertExpectedHostEntries(expectedEntry, Dns.GetHostEntry("rhino.acme.com"));
                AssertExpectedHostEntries(expectedEntry, Dns.GetHostEntry("rhino.acme.com", AddressFamily.Unspecified));
                AssertExpectedHostEntries(expectedEntry, await Dns.GetHostEntryAsync("rhino.acme.com"));
                AssertExpectedHostEntries(expectedEntry, await Dns.GetHostEntryAsync("rhino.acme.com", AddressFamily.Unspecified));
                AssertExpectedHostEntries(expectedEntry, await Dns.GetHostEntryAsync("rhino.acme.com", CancellationToken.None));
                AssertExpectedHostEntries(expectedEntry, Dns.EndGetHostEntry(Dns.BeginGetHostEntry("rhino.acme.com", null, null)));
                AssertExpectedHostEntries(expectedEntry, Dns.GetHostEntry(IPAddress.Parse("102.54.94.97")));
                AssertExpectedHostEntries(expectedEntry, await Dns.GetHostEntryAsync(IPAddress.Parse("102.54.94.97")));
#pragma warning disable CS0618 // Type or member is obsolete
                AssertExpectedHostEntries(expectedEntry, Dns.GetHostByName("rhino.acme.com"));
                AssertExpectedHostEntries(expectedEntry, Dns.Resolve("rhino.acme.com"));
                AssertExpectedHostEntries(expectedEntry, Dns.EndResolve(Dns.BeginResolve("rhino.acme.com", null, null)));
                AssertExpectedHostEntries(expectedEntry, Dns.EndGetHostByName(Dns.BeginGetHostByName("rhino.acme.com", null, null)));
                AssertExpectedHostEntries(expectedEntry, Dns.GetHostByAddress(IPAddress.Parse("102.54.94.97")));
#pragma warning restore CS0618

                AssertExpectedHostEntries(new IPHostEntry { HostName = "example", AddressList = new[] { IPAddress.Parse("::1") }, Aliases = Array.Empty<string>() }, Dns.GetHostEntry("example"));
                AssertExpectedHostEntries(new IPHostEntry { HostName = "localhost", AddressList = new[] { IPAddress.Parse("::1") }, Aliases = Array.Empty<string>() }, Dns.GetHostEntry("localhost"));
                AssertExpectedHostEntries(new IPHostEntry { HostName = "anothermappedto1", AddressList = new[] { IPAddress.Parse("::1") }, Aliases = Array.Empty<string>() }, Dns.GetHostEntry("anothermappedto1"));
                AssertExpectedHostEntries(new IPHostEntry { HostName = "example", AddressList = new[] { IPAddress.Parse("::1") }, Aliases = new[] { "localhost", "anothermappedto1" } }, Dns.GetHostEntry(IPAddress.Parse("::1")));
                AssertExpectedHostEntries(new IPHostEntry { HostName = "firstnameonline", AddressList = new[] { IPAddress.Parse("1.1.1.1") }, Aliases = new[] { "secondnameonline", "thirdnameonline" } }, Dns.GetHostEntry(IPAddress.Parse("1.1.1.1")));

                static void AssertExpectedHostEntries(IPHostEntry expected, IPHostEntry actual)
                {
                    Assert.Equal(expected.HostName, actual.HostName);
                    Assert.Equal(expected.AddressList, actual.AddressList);
                    Assert.Equal(expected.Aliases, actual.Aliases);
                }
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MissingHostsFile_DnsNotImpacted()
        {
            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[HostsEnvironmentVariableName] = Guid.NewGuid().ToString("N");

            RemoteExecutor.Invoke(async () =>
            {
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_HostString_Ok();
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_IPString_Ok();
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EmptyHostsFile_DnsNotImpacted()
        {
            string hostsPath = CreateTestFile();

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[HostsEnvironmentVariableName] = hostsPath;

            RemoteExecutor.Invoke(async () =>
            {
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_HostString_Ok();
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_IPString_Ok();
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CorruptedHostsFile_DnsNotImpacted()
        {
            string hostsPath = CreateTestFile();
            File.WriteAllBytes(hostsPath, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[HostsEnvironmentVariableName] = hostsPath;

            RemoteExecutor.Invoke(async () =>
            {
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_HostString_Ok();
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_IPString_Ok();
            }, options).Dispose();
        }
    }
}
