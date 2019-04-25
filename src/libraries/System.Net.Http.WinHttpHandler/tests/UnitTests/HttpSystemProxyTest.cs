// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.WinHttpHandlerUnitTests
{
    public class HttpSystemProxyTest
    {
        private const string ManualSettingsProxyHost = "myproxy.local";
        private const string ManualSettingsProxyBypassList = "localhost;*.local";

        private readonly ITestOutputHelper _output;

        public HttpSystemProxyTest(ITestOutputHelper output)
        {
            _output = output;
            TestControl.ResetAll();
        }

        public static IEnumerable<object[]> ManualSettingsMemberData()
        {
            yield return new object[] { new Uri("http://example.org"), false };
            yield return new object[] { new Uri("http://example.local"), true };
            yield return new object[] { new Uri("http://localhost"), true };
        }

        [Fact]
        public void TryCreate_WinInetProxySettingsAllOff_ReturnsFalse()
        {
            Assert.False(HttpSystemProxy.TryCreate(out IWebProxy webProxy));
        }

        [Theory]
        [MemberData(nameof(ManualSettingsMemberData))]
        public void GetProxy_BothAutoDetectAndManualSettingsButFailedAutoDetect_ManualSettingsUsed(
            Uri destination, bool bypassProxy)
        {
            FakeRegistry.WinInetProxySettings.AutoDetect = true;
            FakeRegistry.WinInetProxySettings.Proxy = ManualSettingsProxyHost;
            FakeRegistry.WinInetProxySettings.ProxyBypass = ManualSettingsProxyBypassList;
            TestControl.PACFileNotDetectedOnNetwork = true;

            Assert.True(HttpSystemProxy.TryCreate(out IWebProxy webProxy));
            
            // The first GetProxy() call will try using WinInetProxyHelper (and thus WinHTTP) since AutoDetect is on.
            Uri proxyUri1 = webProxy.GetProxy(destination);
            
            // The second GetProxy call will skip using WinHTTP since AutoDetect is on but
            // there was a recent AutoDetect failure. This tests the codepath in HttpSystemProxy
            // which queries WinInetProxyHelper.RecentAutoDetectionFailure.
            Uri proxyUri2 = webProxy.GetProxy(destination);

            if (bypassProxy)
            {
                Assert.Null(proxyUri1);
                Assert.Null(proxyUri2);
            }
            else
            {
                Assert.Equal(ManualSettingsProxyHost, proxyUri1.Host);
                Assert.Equal(ManualSettingsProxyHost, proxyUri2.Host);
            }
        }

        [Theory]
        [MemberData(nameof(ManualSettingsMemberData))]
        public void GetProxy_ManualSettingsOnly_ManualSettingsUsed(
            Uri destination, bool bypassProxy)
        {
            FakeRegistry.WinInetProxySettings.Proxy = ManualSettingsProxyHost;
            FakeRegistry.WinInetProxySettings.ProxyBypass = ManualSettingsProxyBypassList;

            Assert.True(HttpSystemProxy.TryCreate(out IWebProxy webProxy));
            Uri proxyUri = webProxy.GetProxy(destination);
            if (bypassProxy)
            {
                Assert.Null(proxyUri);
            }
            else
            {
                Assert.Equal(ManualSettingsProxyHost, proxyUri.Host);
            }
        }

        [Fact]
        public void IsBypassed_ReturnsFalse()
        {
            FakeRegistry.WinInetProxySettings.AutoDetect = true;
            Assert.True(HttpSystemProxy.TryCreate(out IWebProxy webProxy));
            Assert.False(webProxy.IsBypassed(new Uri("http://www.microsoft.com/")));
        }
    }
}
