// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests.Socks
{
    public abstract class SocksProxyTest : HttpClientHandlerTestBase
    {
        public SocksProxyTest(ITestOutputHelper helper) : base(helper)
        {
        }

        [Theory]
        [InlineData("socks4")]
        [InlineData("socks4a")]
        [InlineData("socks5")]
        public async Task TestLoopbackAsync(string schema)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async url =>
            {
                using (var proxy = LoopbackSocksServer.Create())
                {
                    using (var handler = CreateHttpClientHandler())
                    using (var client = CreateHttpClient())
                    {
                        client.DefaultRequestVersion = UseVersion;
                        handler.Proxy = new WebProxy($"{schema}://localhost:{proxy.Port}");

                        string response = await client.GetStringAsync(url);
                        Assert.Equal("Echo", response);
                    }
                }
            },
            async server => await server.HandleRequestAsync(content: "Echo"));
        }

        [Theory]
        [InlineData("socks4")]
        [InlineData("socks4a")]
        [InlineData("socks5")]
        public async Task TestLoopbackHttpsAsync(string schema)
        {
            using var cert = TestHelper.CreateServerSelfSignedCertificate();

            await LoopbackServerFactory.CreateClientAndServerAsync(async url =>
            {
                using (var proxy = LoopbackSocksServer.Create())
                {
                    using (var handler = CreateHttpClientHandler())
                    using (var client = CreateHttpClient())
                    {
                        client.DefaultRequestVersion = UseVersion;
                        handler.Proxy = new WebProxy($"{schema}://localhost:{proxy.Port}");
                        handler.ServerCertificateCustomValidationCallback = (_1, _2, _3, _4) => true;

                        string response = await client.GetStringAsync($"https://{cert.GetNameInfo(X509NameType.SimpleName, false)}:{url.Port}/");
                        Assert.Equal("Echo", response);
                    }
                }
            },
            async server => await server.HandleRequestAsync(content: "Echo"),
            options: new GenericLoopbackOptions
            {
                UseSsl = true,
                Certificate = cert
            });
        }
    }


    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocksProxyTest_Http1 : SocksProxyTest
    {
        public SocksProxyTest_Http1(ITestOutputHelper helper) : base(helper)
        {
        }

        protected override Version UseVersion => HttpVersion.Version11;
    }


    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocksProxyTest_Http2 : SocksProxyTest
    {
        public SocksProxyTest_Http2(ITestOutputHelper helper) : base(helper)
        {
        }

        protected override Version UseVersion => HttpVersion.Version20;
    }
}
