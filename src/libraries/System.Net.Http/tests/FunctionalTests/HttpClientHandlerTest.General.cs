// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandlerTest_General : HttpClientHandlerTestBase
    {
        public HttpClientHandlerTest_General(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public Task SendAsync_Null_ThrowsArgumentNullException() =>
            Assert.ThrowsAsync<ArgumentNullException>(() => new TestHttpClientHandler().SendNullAsync());

        public static bool SupportsSyncSend => PlatformDetection.IsNotMobile && PlatformDetection.IsNotBrowser;

        [ConditionalFact(nameof(SupportsSyncSend))]
        public void Send_Null_ThrowsArgumentNullException() => Assert.Throws<ArgumentNullException>(() => new TestHttpClientHandler().SendNull());

        private class TestHttpClientHandler : HttpClientHandler
        {
            public Task SendNullAsync() => base.SendAsync(null, default);
            public void SendNull() => base.Send(null, default);
        }
    }
}
