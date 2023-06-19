// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class HostAliasesTest : FileCleanupTestBase
    {
        private const string AliasesEnvironmentVariableName = "DOTNET_SYSTEM_NET_HOSTALIASES";
        private static readonly IPAddress[] s_hostNameAddresses = Dns.GetHostAddresses(Dns.GetHostName());

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AllHookedEntrypoints_LookupsFindExpectedData()
        {
            string hostsPath = CreateTestFile();
            File.WriteAllText(hostsPath, $"""
                # This is a sample hosts aliases file
                #
                rhino.acme.com    {Dns.GetHostName()}       # anything
                something.else.internal       {IPAddress.Loopback}
                 ##
                example rhino.acme.com
                """);

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[AliasesEnvironmentVariableName] = hostsPath;

            RemoteExecutor.Invoke(async () =>
            {
                Assert.Equal(s_hostNameAddresses, Dns.GetHostAddresses("rhino.acme.com"));
                Assert.Equal(s_hostNameAddresses, Dns.GetHostAddresses("example"));
                Assert.Equal(new[] { IPAddress.Loopback }, Dns.GetHostAddresses("something.else.internal"));

                Assert.Equal(s_hostNameAddresses, await Dns.GetHostAddressesAsync("rhino.acme.com"));
                Assert.Equal(s_hostNameAddresses, await Dns.GetHostAddressesAsync("example"));
                Assert.Equal(new[] { IPAddress.Loopback }, await Dns.GetHostAddressesAsync("something.else.internal"));

                Assert.Equal(s_hostNameAddresses, await Dns.GetHostAddressesAsync("rhino.acme.com", AddressFamily.Unspecified));
                Assert.Equal(s_hostNameAddresses, await Dns.GetHostAddressesAsync("example", AddressFamily.Unspecified));
                Assert.Equal(new[] { IPAddress.Loopback }, await Dns.GetHostAddressesAsync("something.else.internal", AddressFamily.Unspecified));

                Assert.Equal(s_hostNameAddresses, await Dns.GetHostAddressesAsync("rhino.acme.com", CancellationToken.None));
                Assert.Equal(s_hostNameAddresses, await Dns.GetHostAddressesAsync("example", CancellationToken.None));
                Assert.Equal(new[] { IPAddress.Loopback }, await Dns.GetHostAddressesAsync("something.else.internal", CancellationToken.None));

                Assert.Equal(s_hostNameAddresses, Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses("rhino.acme.com", null, null)));
                Assert.Equal(s_hostNameAddresses, Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses("example", null, null)));
                Assert.Equal(new[] { IPAddress.Loopback }, Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses("something.else.internal", null, null)));

                IPHostEntry expectedEntry = Dns.GetHostEntry(Dns.GetHostName());

                AssertExpected(Dns.GetHostEntry("rhino.acme.com"));
                AssertExpected(Dns.GetHostEntry("rhino.acme.com", AddressFamily.Unspecified));
                AssertExpected(await Dns.GetHostEntryAsync("rhino.acme.com"));
                AssertExpected(await Dns.GetHostEntryAsync("rhino.acme.com", AddressFamily.Unspecified));
                AssertExpected(await Dns.GetHostEntryAsync("rhino.acme.com", CancellationToken.None));
                AssertExpected(Dns.EndGetHostEntry(Dns.BeginGetHostEntry("rhino.acme.com", null, null)));

#pragma warning disable CS0618 // Type or member is obsolete
                AssertExpected(Dns.GetHostByName("rhino.acme.com"));
                AssertExpected(Dns.Resolve("rhino.acme.com"));
                AssertExpected(Dns.EndResolve(Dns.BeginResolve("rhino.acme.com", null, null)));
                AssertExpected(Dns.EndGetHostByName(Dns.BeginGetHostByName("rhino.acme.com", null, null)));
#pragma warning restore CS0618

                void AssertExpected(IPHostEntry actual)
                {
                    Assert.Equal(expectedEntry.HostName, actual.HostName);
                    Assert.Equal(expectedEntry.AddressList, actual.AddressList);
                    Assert.Equal(expectedEntry.Aliases, actual.Aliases);
                }

            }, options).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("""127.0.0.1 127.0.0.1""")]
        [InlineData("""
            127.0.0.1 abc
            abc 127.0.0.1
            """)]
        [InlineData("""
            127.0.0.1 abc
            abc def
            def 127.0.0.1
            """)]
        [InlineData("""
            yz0 127.0.0.1
            def ghi
            abc def
            ghi jkl
            vwx yz0
            127.0.0.1 abc
            jkl mno
            pqr stu
            mno pqr
            stu vwx
            """)]
        public void Cycles_DontCauseHangs(string contents)
        {
            string hostsPath = CreateTestFile();
            File.WriteAllText(hostsPath, contents);

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[AliasesEnvironmentVariableName] = hostsPath;

            RemoteExecutor.Invoke(async () =>
            {
                Assert.Equal(new[] { IPAddress.Parse("127.0.0.1") }, Dns.GetHostAddresses("127.0.0.1"));
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_HostString_Ok();
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_IPString_Ok();
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DuplicateEntries_Ignored()
        {
            string hostsPath = CreateTestFile();
            File.WriteAllText(hostsPath, $"""
                rhino.acme.com    {Dns.GetHostName()}       # anything
                rhino.acme.com  {Guid.NewGuid().ToString("N")}
                rhino.acme.com  asdfasdfasdfasdfasdfasdfasdfasdfasdf
                """);

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[AliasesEnvironmentVariableName] = hostsPath;

            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(s_hostNameAddresses, Dns.GetHostAddresses("rhino.acme.com"));
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MissingAliasesFile_DnsNotImpacted()
        {
            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[AliasesEnvironmentVariableName] = Guid.NewGuid().ToString("N");

            RemoteExecutor.Invoke(async () =>
            {
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_HostString_Ok();
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_IPString_Ok();
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EmptyAliasesFile_DnsNotImpacted()
        {
            string hostsPath = CreateTestFile();

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[AliasesEnvironmentVariableName] = hostsPath;

            RemoteExecutor.Invoke(async () =>
            {
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_HostString_Ok();
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_IPString_Ok();
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CorruptedAliasesFile_DnsNotImpacted()
        {
            string hostsPath = CreateTestFile();
            File.WriteAllBytes(hostsPath, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });

            var options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables[AliasesEnvironmentVariableName] = hostsPath;

            RemoteExecutor.Invoke(async () =>
            {
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_HostString_Ok();
                await GetHostAddressesTest.Dns_GetHostAddressesAsync_IPString_Ok();
            }, options).Dispose();
        }
    }
}
