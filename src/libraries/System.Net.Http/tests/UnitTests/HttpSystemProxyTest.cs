// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;
using System.Net.Http.WinHttpHandlerUnitTests;

namespace System.Net.Http.Tests
{
    public class HttpSystemProxyTest : RemoteExecutorTestBase
    {
        private readonly ITestOutputHelper _output;
        private const string FakeProxyString = "http://proxy.contoso.com";
        private readonly Uri fakeProxyUri = new Uri("http://proxy.contoso.com");
        private readonly Uri fooHttp = new Uri("http://foo.com");
        private readonly Uri fooHttps = new Uri("https://foo.com");

        public HttpSystemProxyTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void HttpProxy_SystemProxy_Loaded()
        {
            IWebProxy p;

            FakeRegistry.Reset();
            Assert.False(HttpSystemProxy.TryCreate(out p));

            FakeRegistry.WinInetProxySettings.Proxy = FakeProxyString;
            WinInetProxyHelper proxyHelper = new WinInetProxyHelper();

            Assert.True(HttpSystemProxy.TryCreate(out p));
            Assert.NotNull(p);
            Assert.Equal(fakeProxyUri, p.GetProxy(fooHttp));
            Assert.Equal(fakeProxyUri, p.GetProxy(fooHttps));
        }
    }
}
