// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.WinHttpHandlerUnitTests;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Tests
{
    public class HttpWindowsProxyTest
    {
        private readonly ITestOutputHelper _output;
        private const string FakeProxyString = "http://proxy.contoso.com";
        private const string insecureProxyUri = "http://proxy.insecure.com";
        private const string secureProxyUri = "http://proxy.secure.com";
        private const string secureAndInsecureProxyUri = "http://proxy.secure-and-insecure.com";
        private const string fooHttp = "http://foo.com";
        private const string fooHttps = "https://foo.com";
        private const string fooWs = "ws://foo.com";
        private const string fooWss = "wss://foo.com";

        public HttpWindowsProxyTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(ProxyParsingData))]
        public async Task HttpProxy_WindowsProxy_Manual_Loaded(string rawProxyString, string rawInsecureUri, string rawSecureUri)
        {
            await RemoteExecutor.Invoke((proxyString, insecureProxy, secureProxy) =>
            {
                FakeRegistry.Reset();

                FakeRegistry.WinInetProxySettings.Proxy = proxyString;
                WinInetProxyHelper proxyHelper = new WinInetProxyHelper();
                Assert.Null(proxyHelper.AutoConfigUrl);
                Assert.Equal(proxyString, proxyHelper.Proxy);
                Assert.False(proxyHelper.AutoSettingsUsed);
                Assert.True(proxyHelper.ManualSettingsUsed);

                IWebProxy p = new HttpWindowsProxy(proxyHelper);

                Assert.Equal(!string.IsNullOrEmpty(insecureProxy) ? new Uri(insecureProxy) : null, p.GetProxy(new Uri(fooHttp)));
                Assert.Equal(!string.IsNullOrEmpty(secureProxy) ? new Uri(secureProxy) : null, p.GetProxy(new Uri(fooHttps)));
                Assert.Equal(!string.IsNullOrEmpty(insecureProxy) ? new Uri(insecureProxy) : null, p.GetProxy(new Uri(fooWs)));
                Assert.Equal(!string.IsNullOrEmpty(secureProxy) ? new Uri(secureProxy) : null, p.GetProxy(new Uri(fooWss)));
            }, rawProxyString, rawInsecureUri ?? string.Empty, rawSecureUri ?? string.Empty).DisposeAsync();
        }

        public static TheoryData<string, string, string> ProxyParsingData =>
           new TheoryData<string, string, string>
           {
                { "http://proxy.secure-and-insecure.com", secureAndInsecureProxyUri, secureAndInsecureProxyUri },
                { "http=http://proxy.insecure.com", insecureProxyUri, null },
                { "http=proxy.insecure.com", insecureProxyUri, null },
                { "http=http://proxy.insecure.com", insecureProxyUri, null },
                { "https://proxy.secure.com", secureProxyUri, secureProxyUri },
                { "https=proxy.secure.com", null, secureProxyUri },
                { "https=https://proxy.secure.com", null, secureProxyUri },
                { "http=https://proxy.secure.com", secureProxyUri, null },
                { "https=http://proxy.insecure.com", null, insecureProxyUri },
                { "proxy.secure-and-insecure.com", secureAndInsecureProxyUri, secureAndInsecureProxyUri },
           };

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(ProxyPacParsingData))]
        public async Task HttpProxy_WindowsProxy_PAC_Loaded(string rawProxyString, string rawInsecureUri, string rawSecureUri)
        {
            await RemoteExecutor.Invoke((proxyString, insecureProxy, secureProxy) =>
            {
                TestControl.ResetAll();

                FakeRegistry.WinInetProxySettings.AutoConfigUrl = "http://127.0.0.1/proxy.pac";
                WinInetProxyHelper proxyHelper = new WinInetProxyHelper();
                Assert.Null(proxyHelper.Proxy);
                Assert.Equal(FakeRegistry.WinInetProxySettings.AutoConfigUrl, proxyHelper.AutoConfigUrl);
                Assert.False(proxyHelper.ManualSettingsUsed);
                Assert.True(proxyHelper.AutoSettingsUsed);

                IWebProxy p = new HttpWindowsProxy(proxyHelper);

                // With a HttpWindowsProxy created configured to use auto-config, now set Proxy so when it
                // attempts to resolve a proxy, it resolves our string.
                FakeRegistry.WinInetProxySettings.Proxy = proxyString;
                proxyHelper = new WinInetProxyHelper();
                Assert.Equal(proxyString, proxyHelper.Proxy);

                Assert.Equal(!string.IsNullOrEmpty(insecureProxy) ? new Uri(insecureProxy) : null, p.GetProxy(new Uri(fooHttp)));
                Assert.Equal(!string.IsNullOrEmpty(secureProxy) ? new Uri(secureProxy) : null, p.GetProxy(new Uri(fooHttps)));
                Assert.Equal(!string.IsNullOrEmpty(insecureProxy) ? new Uri(insecureProxy) : null, p.GetProxy(new Uri(fooWs)));
                Assert.Equal(!string.IsNullOrEmpty(secureProxy) ? new Uri(secureProxy) : null, p.GetProxy(new Uri(fooWss)));
            }, rawProxyString, rawInsecureUri ?? string.Empty, rawSecureUri ?? string.Empty).DisposeAsync();
        }

        public static TheoryData<string, string, string> ProxyPacParsingData =>
            new TheoryData<string, string, string>
            {
                { "http://proxy.insecure.com", insecureProxyUri, null },
                { "http=http://proxy.insecure.com", insecureProxyUri, null },
                { "http=proxy.insecure.com", insecureProxyUri, null },
                { "http://proxy.insecure.com http://proxy.wrong.com", insecureProxyUri, null },
                { "https=proxy.secure.com http=proxy.insecure.com", insecureProxyUri, secureProxyUri },
                { "https://proxy.secure.com\nhttp://proxy.insecure.com", insecureProxyUri, secureProxyUri },
                { "https=proxy.secure.com\nhttp=proxy.insecure.com", insecureProxyUri, secureProxyUri },
                { "https://proxy.secure.com;http://proxy.insecure.com", insecureProxyUri, secureProxyUri },
                { "https=proxy.secure.com;http=proxy.insecure.com", insecureProxyUri, secureProxyUri },
                { ";http=proxy.insecure.com;;", insecureProxyUri, null },
                { "    http=proxy.insecure.com    ", insecureProxyUri, null },
                { "http=proxy.insecure.com;http=proxy.wrong.com", insecureProxyUri, null },
                { "http=http://proxy.insecure.com", insecureProxyUri, null },
                { "https://proxy.secure.com", null, secureProxyUri },
                { "https=proxy.secure.com", null, secureProxyUri },
                { "https=https://proxy.secure.com", null, secureProxyUri },
                { "http=https://proxy.secure.com", null, secureProxyUri },
                { "https=http://proxy.insecure.com", insecureProxyUri, null },
                { "proxy.secure-and-insecure.com", secureAndInsecureProxyUri, secureAndInsecureProxyUri },
            };

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("localhost:1234", "http://localhost:1234/")]
        [InlineData("123.123.123.123", "http://123.123.123.123/")]
        public async Task HttpProxy_WindowsProxy_Loaded(string rawProxyString, string expectedUri)
        {
            await RemoteExecutor.Invoke((proxyString, expectedString) =>
            {
                FakeRegistry.Reset();

                FakeRegistry.WinInetProxySettings.Proxy = proxyString;
                WinInetProxyHelper proxyHelper = new WinInetProxyHelper();

                IWebProxy p = new HttpWindowsProxy(proxyHelper);
                Assert.Equal(expectedString, p.GetProxy(new Uri(fooHttp)).ToString());
                Assert.Equal(expectedString, p.GetProxy(new Uri(fooHttps)).ToString());
            }, rawProxyString, expectedUri).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("http://localhost/", true)]
        [InlineData("http://127.0.0.1/", true)]
        [InlineData("http://128.0.0.1/", false)]
        [InlineData("http://[::1]/", true)]
        [InlineData("http://foo/", true)]
        [InlineData("http://www.foo.com/", true)]
        [InlineData("http://WWW.FOO.COM/", true)]
        [InlineData("http://foo.com/", false)]
        [InlineData("http://bar.com/", true)]
        [InlineData("http://BAR.COM/", true)]
        [InlineData("http://162.1.1.1/", true)]
        [InlineData("http://[2a01:5b40:0:248::52]/", false)]
        [InlineData("http://[2002::11]/", true)]
        [InlineData("http://[2607:f8b0:4005:80a::200e]/", true)]
        [InlineData("http://[2607:f8B0:4005:80A::200E]/", true)]
        [InlineData("http://b\u00e9b\u00e9.eu/", true)]
        [InlineData("http://www.b\u00e9b\u00e9.eu/", true)]
        public async Task HttpProxy_Local_Bypassed(string name, bool shouldBypass)
        {
            await RemoteExecutor.Invoke((url, expected) =>
            {
                bool expectedResult = Boolean.Parse(expected);

                FakeRegistry.Reset();
                FakeRegistry.WinInetProxySettings.Proxy = insecureProxyUri;
                FakeRegistry.WinInetProxySettings.ProxyBypass = "23.23.86.44;*.foo.com;<local>;BAR.COM; ; 162*;[2002::11];[*:f8b0:4005:80a::200e]; http://www.xn--mnchhausen-9db.at;http://*.xn--bb-bjab.eu;http://xn--bb-bjab.eu;";

                IWebProxy p = new HttpWindowsProxy();

                Uri u = new Uri(url);
                Assert.Equal(expectedResult, p.GetProxy(u) == null);
           }, name, shouldBypass.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("", 0)]
        [InlineData(" ", 0)]
        [InlineData(" ; ;  ", 0)]
        [InlineData("http://127.0.0.1/", 1)]
        [InlineData("[::]", 1)]
        public async Task HttpProxy_Local_Parsing(string bypass, int count)
        {
            await RemoteExecutor.Invoke((bypassValue, expected) =>
            {
                int expectedCount = Convert.ToInt32(expected);

                FakeRegistry.Reset();
                FakeRegistry.WinInetProxySettings.Proxy = insecureProxyUri;
                FakeRegistry.WinInetProxySettings.ProxyBypass = bypassValue;

                IWebProxy p = new HttpWindowsProxy();

                HttpWindowsProxy sp = p as HttpWindowsProxy;
                Assert.NotNull(sp);

                if (expectedCount > 0)
                {
                    Assert.Equal(expectedCount, sp.BypassList.Count);
                }
                else
                {
                    Assert.Null(sp.BypassList);
                }
           }, bypass, count.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(HttpProxy_Multi_Data))]
        public async Task HttpProxy_Multi_Success(string proxyConfig, string url, string expected)
        {
            await RemoteExecutor.Invoke((proxyConfigValue, urlValue, expectedValue) =>
            {
                Uri requestUri = new Uri(urlValue);
                string[] expectedUris = expectedValue.Split(';', StringSplitOptions.RemoveEmptyEntries);

                TestControl.ResetAll();
                FakeRegistry.WinInetProxySettings.AutoConfigUrl = "http://dummy.com";

                IWebProxy p = new HttpWindowsProxy();
                HttpWindowsProxy wp = Assert.IsType<HttpWindowsProxy>(p);

                // Now that HttpWindowsProxy has been constructed to use autoconfig,
                // set Proxy which will be used by Fakes for all the per-URL calls.
                FakeRegistry.WinInetProxySettings.Proxy = proxyConfigValue;

                MultiProxy multi = wp.GetMultiProxy(requestUri);

                for (int i = 0; i < expectedUris.Length; ++i)
                {
                    // Both the current enumerator and the proxy globally should move to the next proxy.
                    Assert.True(multi.ReadNext(out Uri uri, out _));
                    Assert.Equal(new Uri(expectedUris[i]), uri);
                    Assert.Equal(new Uri(expectedUris[i]), p.GetProxy(requestUri));
                }

                Assert.False(multi.ReadNext(out _, out _));
            }, proxyConfig, url, expected).DisposeAsync();
        }

        public static IEnumerable<object[]> HttpProxy_Multi_Data()
        {
           yield return new object[] { "http://proxy.com", "http://request.com", "http://proxy.com" };
           yield return new object[] { "http://proxy.com https://secure-proxy.com", "http://request.com", "http://proxy.com" };
           yield return new object[] { "http://proxy-a.com https://secure-proxy.com http://proxy-b.com", "http://request.com", "http://proxy-a.com;http://proxy-b.com" };
           yield return new object[] { "http://proxy-a.com https://secure-proxy.com http://proxy-b.com", "https://request.com", "http://secure-proxy.com" };
           yield return new object[] { "http://proxy-a.com https://secure-proxy-a.com http://proxy-b.com  https://secure-proxy-b.com  https://secure-proxy-c.com", "https://request.com", "http://secure-proxy-a.com;http://secure-proxy-b.com;http://secure-proxy-c.com" };
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task HttpProxy_Multi_ConcurrentUse_Success(bool manualConfig)
        {
            const string MultiProxyConfig = "http://proxy-a.com http://proxy-b.com http://proxy-c.com";

            await RemoteExecutor.Invoke(manualValue =>
            {
                bool manual = bool.Parse(manualValue);

                Uri requestUri = new Uri("http://request.com");
                Uri firstProxy = new Uri("http://proxy-a.com");
                Uri secondProxy = new Uri("http://proxy-b.com");
                Uri thirdProxy = new Uri("http://proxy-c.com");

                TestControl.ResetAll();

                if (manual)
                {
                    FakeRegistry.WinInetProxySettings.Proxy = MultiProxyConfig;
                }
                else
                {
                    FakeRegistry.WinInetProxySettings.AutoConfigUrl = "http://dummy.com";
                }

                IWebProxy p = new HttpWindowsProxy();
                HttpWindowsProxy wp = Assert.IsType<HttpWindowsProxy>(p);

                if (!manual)
                {
                    // Now that HttpWindowsProxy has been constructed to use autoconfig,
                    // set Proxy which will be used by Fakes for all the per-URL calls.
                    FakeRegistry.WinInetProxySettings.Proxy = MultiProxyConfig;
                }

                MultiProxy multiA = wp.GetMultiProxy(requestUri);
                MultiProxy multiB = wp.GetMultiProxy(requestUri);

                // Assert first proxy is returned across all three methods.
                Assert.True(multiA.ReadNext(out Uri proxyA, out _));
                Assert.True(multiB.ReadNext(out Uri proxyB, out _));
                Assert.Equal(firstProxy, proxyA);
                Assert.Equal(firstProxy, proxyB);
                Assert.Equal(firstProxy, p.GetProxy(requestUri));

                // Assert second proxy is returned across all three methods.
                Assert.True(multiA.ReadNext(out proxyA, out _));
                Assert.True(multiB.ReadNext(out proxyB, out _));
                Assert.Equal(secondProxy, proxyA);
                Assert.Equal(secondProxy, proxyB);
                Assert.Equal(secondProxy, p.GetProxy(requestUri));

                // Assert third proxy is returned from multiA.
                Assert.True(multiA.ReadNext(out proxyA, out _));
                Assert.Equal(thirdProxy, proxyA);
                Assert.Equal(thirdProxy, p.GetProxy(requestUri));

                // Enumerating multiA once more should exhaust all of our proxies.
                // So, multiB, still on secondProxy, should now also be exhausted because
                // when it tries thirdProxy it will see it marked as failed.
                Assert.False(multiA.ReadNext(out proxyA, out _));
                Assert.False(multiB.ReadNext(out proxyB, out _));

                // GetProxy should now return the proxy closest to being turned back on, which should be firstProxy.
                Assert.Equal(firstProxy, p.GetProxy(requestUri));

                // Enumerating a new MultiProxy should again return the proxy closed to being turned back on, and no others.
                MultiProxy multiC = wp.GetMultiProxy(requestUri);
                Assert.True(multiC.ReadNext(out Uri proxyC, out _));
                Assert.Equal(firstProxy, proxyC);
                Assert.False(multiC.ReadNext(out proxyC, out _));
            }, manualConfig.ToString()).DisposeAsync();
        }
    }
}
