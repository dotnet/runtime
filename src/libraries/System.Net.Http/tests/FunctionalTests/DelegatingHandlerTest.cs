// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class DelegatingHandlerTest
    {
        [Fact]
        public void Ctor_CreateDispose_Success()
        {
            MockHandler handler = new MockHandler();
            Assert.Null(handler.InnerHandler);
            handler.Dispose();
        }

        [Fact]
        public void Ctor_CreateDisposeAssign_ThrowsObjectDisposedException()
        {
            MockHandler handler = new MockHandler();
            Assert.Null(handler.InnerHandler);
            handler.Dispose();
            Assert.Throws<ObjectDisposedException>(() => handler.InnerHandler = new MockTransportHandler());
        }

        [Fact]
        public void Ctor_NullInnerHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MockHandler(null));
        }

        [Fact]
        public void Ctor_SetNullInnerHandler_ThrowsArgumentNullException()
        {
            MockHandler handler = new MockHandler();
            Assert.Throws<ArgumentNullException>(() => handler.InnerHandler = null);
        }

        [Fact]
        public void SendAsync_WithoutSettingInnerHandlerCallMethod_ThrowsInvalidOperationException()
        {
            MockHandler handler = new MockHandler();

            Assert.Throws<InvalidOperationException>(() => 
                { Task t = handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None); });
        }

        [Fact]
        public async Task SendAsync_SetInnerHandlerCallMethod_InnerHandlerSendIsCalled()
        {
            var handler = new MockHandler();
            var transport = new MockTransportHandler();
            handler.InnerHandler = transport;

            using (HttpResponseMessage response = await handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None))
            {
                Assert.NotNull(response);
                Assert.Equal(1, handler.SendAsyncCount);
                Assert.Equal(1, transport.SendAsyncCount);

                Assert.Throws<InvalidOperationException>(() => handler.InnerHandler = transport);
                Assert.Equal(transport, handler.InnerHandler);
            }
        }

        [Fact]
        public async Task SendAsync_SetInnerHandlerTwiceCallMethod_SecondInnerHandlerSendIsCalled()
        {
            var handler = new MockHandler();
            var transport1 = new MockTransportHandler();
            var transport2 = new MockTransportHandler();
            handler.InnerHandler = transport1;
            handler.InnerHandler = transport2;

            using (HttpResponseMessage response = await handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None))
            {
                Assert.NotNull(response);
                Assert.Equal(1, handler.SendAsyncCount);
                Assert.Equal(0, transport1.SendAsyncCount);
                Assert.Equal(1, transport2.SendAsyncCount);
            }
        }

        [Fact]
        public void SendAsync_NullRequest_ThrowsArgumentNullException()
        {
            var transport = new MockTransportHandler();
            var handler = new MockHandler(transport);

            Assert.Throws<ArgumentNullException>(() => 
                { Task t = handler.TestSendAsync(null, CancellationToken.None); });
        }

        [Fact]
        public void SendAsync_Disposed_Throws()
        {
            var transport = new MockTransportHandler();
            var handler = new MockHandler(transport);
            handler.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
                { Task t = handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None); });
            Assert.Throws<ObjectDisposedException>(() => handler.InnerHandler = new MockHandler());
            Assert.Equal(transport, handler.InnerHandler);
        }

        [Fact]
        public async Task SendAsync_CallMethod_InnerHandlerSendAsyncIsCalled()
        {
            var transport = new MockTransportHandler();
            var handler = new MockHandler(transport);

            await handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None);

            Assert.Equal(1, handler.SendAsyncCount);
            Assert.Equal(1, transport.SendAsyncCount);
        }

        [Fact]
        public void SendAsync_CallMethodWithoutSettingInnerHandler_ThrowsInvalidOperationException()
        {
            var handler = new MockHandler();

            Assert.Throws<InvalidOperationException>(() => 
                { Task t = handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None); });
        }

        [Fact]
        public async Task SendAsync_SetInnerHandlerAfterCallMethod_ThrowsInvalidOperationException()
        {
            var transport = new MockTransportHandler();
            var handler = new MockHandler(transport);

            await handler.TestSendAsync(new HttpRequestMessage(), CancellationToken.None);

            Assert.Equal(1, handler.SendAsyncCount);
            Assert.Equal(1, transport.SendAsyncCount);

            Assert.Throws<InvalidOperationException>(() => handler.InnerHandler = transport);
        }

        [Fact]
        public void Dispose_CallDispose_OverriddenDisposeMethodCalled()
        {
            var innerHandler = new MockTransportHandler();
            var handler = new MockHandler(innerHandler);
            handler.Dispose();

            Assert.Equal(1, handler.DisposeCount);
            Assert.Equal(1, innerHandler.DisposeCount);
        }

        [Fact]
        public void Dispose_CallDisposeMultipleTimes_OverriddenDisposeMethodCalled()
        {
            var innerHandler = new MockTransportHandler();
            var handler = new MockHandler(innerHandler);
            handler.Dispose();
            handler.Dispose();
            handler.Dispose();

            Assert.Equal(3, handler.DisposeCount);
            Assert.Equal(1, innerHandler.DisposeCount);
        }

        #region Helper methods
        private class MockHandler : DelegatingHandler
        {
            public int SendAsyncCount { get; private set; }
            public int DisposeCount { get; private set; }

            public MockHandler() : base()
            {
            }

            public MockHandler(HttpMessageHandler innerHandler) : base(innerHandler)
            {
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SendAsyncCount++;
                return base.SendAsync(request, cancellationToken);
            }

            public Task<HttpResponseMessage> TestSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return SendAsync(request, cancellationToken);
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }
        }

        private class MockTransportHandler : HttpMessageHandler
        {
            public int SendAsyncCount { get; private set; }
            public int DisposeCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SendAsyncCount++;
                return Task.FromResult(new HttpResponseMessage());
            }

            public Task<HttpResponseMessage> TestSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return SendAsync(request, cancellationToken);
            }
            
            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }
        }
        #endregion
    }
}
