// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests.Socks
{
    public abstract class SocksProxyTest : HttpClientHandlerTestBase
    {
        public SocksProxyTest(ITestOutputHelper helper) : base(helper) { }

        private static string[] Hosts(string socksScheme) => socksScheme == "socks5"
            ? new[] { "localhost", "127.0.0.1", "::1" }
            : new[] { "localhost", "127.0.0.1" };

        public static IEnumerable<object[]> TestLoopbackAsync_MemberData() =>
            from scheme in new[] { "socks4", "socks4a", "socks5" }
            from useSsl in BoolValues
            from useAuth in BoolValues
            from host in Hosts(scheme)
            select new object[] { scheme, useSsl, useAuth, host };

        [Theory]
        [MemberData(nameof(TestLoopbackAsync_MemberData))]
        public async Task TestLoopbackAsync(string scheme, bool useSsl, bool useAuth, string host)
        {
            if (useSsl && UseVersion == HttpVersion.Version20 && !PlatformDetection.SupportsAlpn)
            {
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using LoopbackSocksServer proxy = useAuth ? LoopbackSocksServer.Create("DOTNET", "424242") : LoopbackSocksServer.Create();
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    using HttpClient client = CreateHttpClient(handler);

                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    handler.Proxy = new WebProxy($"{scheme}://localhost:{proxy.Port}");
                    handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

                    if (useAuth)
                    {
                        handler.Proxy.Credentials = new NetworkCredential("DOTNET", "424242");
                    }

                    uri = new UriBuilder(uri) { Host = host }.Uri;

                    Assert.Equal("Echo", await client.GetStringAsync(uri));
                },
                async server => await server.HandleRequestAsync(content: "Echo"),
                options: new GenericLoopbackOptions
                {
                    UseSsl = useSsl,
                    Address = host == "::1" ? IPAddress.IPv6Loopback : IPAddress.Loopback
                });
        }

        public static IEnumerable<object[]> TestExceptionalAsync_MemberData()
        {
            foreach (string scheme in new[] { "socks4", "socks4a" })
                yield return new object[] { scheme, "::1", false, null };
            foreach (string scheme in new[] { "socks4", "socks4a", "socks5" })
            {
                yield return new object[] { scheme, "localhost", true, null };
                yield return new object[] { scheme, "localhost", true, new NetworkCredential("bad_username", "bad_password") };
            }
        }

        [Theory]
        [MemberData(nameof(TestExceptionalAsync_MemberData))]
        public async Task TestExceptionalAsync(string scheme, string host, bool useAuth, ICredentials? credentials)
        {
            using LoopbackSocksServer proxy = useAuth ? LoopbackSocksServer.Create("DOTNET", "424242") : LoopbackSocksServer.Create();
            using HttpClientHandler handler = CreateHttpClientHandler();
            using HttpClient client = CreateHttpClient(handler);

            handler.Proxy = new WebProxy($"{scheme}://localhost:{proxy.Port}")
            {
                Credentials = credentials
            };

            // SocksException is not public
            await Assert.ThrowsAnyAsync<IOException>(() => client.GetStringAsync($"http://{host}"));

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using LoopbackSocksServer proxy = useAuth ? LoopbackSocksServer.Create("DOTNET", "424242") : LoopbackSocksServer.Create();
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    using HttpClient client = CreateHttpClient(handler);

                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

                    handler.Proxy = new WebProxy($"{scheme}://localhost:{proxy.Port}")
                    {
                        Credentials = credentials
                    };

                    uri = new UriBuilder(uri) { Host = host }.Uri;

                    // SocksException is not public
                    var ex = await Assert.ThrowsAnyAsync<IOException>(() => client.GetStringAsync(uri));
                    Assert.Equal("SocksException", ex.GetType().Name);
                },
                async server => await server.HandleRequestAsync(content: "Echo"),
                options: new GenericLoopbackOptions
                {
                    Address = host == "::1" ? IPAddress.IPv6Loopback : IPAddress.Loopback
                });
        }
    }


    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocksProxyTest_Http1 : SocksProxyTest
    {
        public SocksProxyTest_Http1(ITestOutputHelper helper) : base(helper) { }

        protected override Version UseVersion => HttpVersion.Version11;
    }


    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocksProxyTest_Http2 : SocksProxyTest
    {
        public SocksProxyTest_Http2(ITestOutputHelper helper) : base(helper) { }

        protected override Version UseVersion => HttpVersion.Version20;
    }
}
