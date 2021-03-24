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
        public SocksProxyTest(ITestOutputHelper helper) : base(helper) { }

        [Theory]
        [InlineData("socks4", true)]
        [InlineData("socks4", false)]
        [InlineData("socks4a", true)]
        [InlineData("socks4a", false)]
        [InlineData("socks5", true)]
        [InlineData("socks5", false)]
        public async Task TestLoopbackAsync(string schema, bool useSsl)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async url =>
                {
                    using LoopbackSocksServer proxy = LoopbackSocksServer.Create();
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    using HttpClient client = CreateHttpClient(handler);

                    handler.Proxy = new WebProxy($"{schema}://localhost:{proxy.Port}");
                    handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

                    var request = new HttpRequestMessage(HttpMethod.Get, url)
                    {
                        Version = UseVersion,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact // Needed for H2C
                    };

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
