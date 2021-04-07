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

        [Theory]
        [InlineData("socks4", true, false)]
        [InlineData("socks4", false, false)]
        [InlineData("socks4", false, true)]
        [InlineData("socks4a", true, false)]
        [InlineData("socks4a", false, false)]
        [InlineData("socks5", true, false)]
        [InlineData("socks5", false, false)]
        [InlineData("socks5", false, true)]
        public async Task TestLoopbackAsync(string schema, bool useSsl, bool useAuth)
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

                    var request = new HttpRequestMessage(HttpMethod.Get, url) { Version = UseVersion };

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
