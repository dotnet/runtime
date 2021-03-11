// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests.Socks
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public sealed class SocksProxyTest : HttpClientHandlerTestBase
    {
        public SocksProxyTest(ITestOutputHelper helper) : base(helper)
        {
        }

        [Theory]
        [InlineData("socks4")]
        [InlineData("socks4a")]
        [InlineData("socks5")]
        public async Task TestLoopbackHttp1Async(string schema)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async url =>
            {
                using (var proxy = LoopbackSocksServer.Create())
                {
                    using (var handler = CreateHttpClientHandler())
                    using (var client = CreateHttpClient())
                    {
                        handler.Proxy = new WebProxy($"{schema}://localhost:{proxy.Port}");
                        string response = await client.GetStringAsync(url);
                        Assert.Equal("Echo", response);
                    }
                }
            },
            async server => await server.HandleRequestAsync(content: "Echo"));
        }
    }
}
