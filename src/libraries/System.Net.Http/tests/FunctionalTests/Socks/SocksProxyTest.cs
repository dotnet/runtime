// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests.Socks
{
    public abstract class SocksProxyTest : HttpClientHandlerTestBase
    {
        private class Credentials : ICredentials
        {
            private readonly string _username, _password;

            public Credentials(string username, string password)
            {
                _username = username;
                _password = password;
            }

            public NetworkCredential? GetCredential(Uri uri, string authType)
                => new NetworkCredential(_username, _password);
        }

        public SocksProxyTest(ITestOutputHelper helper) : base(helper) { }

        [Fact]
        public async Task TestSocks4Async() => await TestLoopbackAsync("socks4", useSsl: false, useAuth: false, "localhost");

        [Fact]
        public async Task TestSocks4IPAsync() => await TestLoopbackAsync("socks4", useSsl: false, useAuth: false, "127.0.0.1");

        [Fact]
        public async Task TestSocks4SslAsync() => await TestLoopbackAsync("socks4", useSsl: true, useAuth: false, "localhost");

        [Fact]
        public async Task TestSocks4AuthAsync() => await TestLoopbackAsync("socks4", useSsl: false, useAuth: true, "localhost");

        [Fact]
        public async Task TestSocks4aAsync() => await TestLoopbackAsync("socks4a", useSsl: false, useAuth: false, "localhost");

        [Fact]
        public async Task TestSocks4aIPAsync() => await TestLoopbackAsync("socks4a", useSsl: false, useAuth: false, "127.0.0.1");

        [Fact]
        public async Task TestSocks4aSslAsync() => await TestLoopbackAsync("socks4a", useSsl: true, useAuth: false, "localhost");

        [Fact]
        public async Task TestSocks5Async() => await TestLoopbackAsync("socks5", useSsl: false, useAuth: false, "localhost");

        [Fact]
        public async Task TestSocks5IPAsync() => await TestLoopbackAsync("socks5", useSsl: false, useAuth: false, "127.0.0.1");

        [Fact]
        public async Task TestSocks5SslAsync() => await TestLoopbackAsync("socks5", useSsl: true, useAuth: false, "localhost");

        [Fact]
        public async Task TestSocks5AuthAsync() => await TestLoopbackAsync("socks5", useSsl: false, useAuth: true, "localhost");

        private async Task TestLoopbackAsync(string schema, bool useSsl, bool useAuth, string host)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async url =>
                {
                    using LoopbackSocksServer proxy = useAuth ? LoopbackSocksServer.Create("DOTNET", "424242") : LoopbackSocksServer.Create();
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    using HttpClient client = CreateHttpClient(handler);

                    handler.Proxy = new WebProxy($"{schema}://localhost:{proxy.Port}");
                    handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

                    if (useAuth)
                    {
                        handler.Proxy.Credentials = new Credentials("DOTNET", "424242");
                    }

                    var request = new HttpRequestMessage(HttpMethod.Get, new UriBuilder(url) { Host = host }.Uri) { Version = UseVersion };

                    if (UseVersion == HttpVersion.Version20 && !useSsl)
                    {
                        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact; // H2C
                    }

                    using HttpResponseMessage response = await client.SendAsync(request);
                    string responseString = await response.Content.ReadAsStringAsync();

                    Assert.Equal("Echo", responseString);
                },
                async server => await server.HandleRequestAsync(content: "Echo"),
                options: new GenericLoopbackOptions { UseSsl = useSsl });
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
