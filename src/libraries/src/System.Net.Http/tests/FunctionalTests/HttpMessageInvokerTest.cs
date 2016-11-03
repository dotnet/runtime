// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpMessageInvokerTest
    {
        [Fact]
        public void Constructor_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new HttpMessageInvoker(null));
            Assert.Throws<ArgumentNullException>(() => new HttpMessageInvoker(null, false));
        }

        [Fact]
        public void SendAsync_Null_ThrowsArgumentNullException()
        {
            using (var invoker = new HttpMessageInvoker(new MockHandler()))
            {
                Assert.Throws<ArgumentNullException>(() => { Task t = invoker.SendAsync(null, CancellationToken.None); });
            }
        }

        [Fact]
        public async Task SendAsync_Request_HandlerInvoked()
        {
            var handler = new MockHandler();
            using (var invoker = new HttpMessageInvoker(handler))
            using (HttpResponseMessage response = await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None))
            {
                Assert.NotNull(response);
                Assert.Equal(1, handler.SendAsyncCount);
            }
        }
        
        [Fact]
        public void Dispose_DisposeHandler_HandlerDisposed()
        {
            var handler = new MockHandler();
            var invoker = new HttpMessageInvoker(handler);

            invoker.Dispose();
            Assert.Equal(1, handler.DisposeCount);

            Assert.Throws<ObjectDisposedException>(() => { Task t = invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None); });
            Assert.Equal(0, handler.SendAsyncCount);


            handler = new MockHandler();
            invoker = new HttpMessageInvoker(handler, true);

            invoker.Dispose();
            Assert.Equal(1, handler.DisposeCount);

            Assert.Throws<ObjectDisposedException>(() => { Task t = invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None); });
            Assert.Equal(0, handler.SendAsyncCount);
        }

        [Fact]
        public void Dispose_DontDisposeHandler_HandlerNotDisposed()
        {
            var handler = new MockHandler();
            var invoker = new HttpMessageInvoker(handler, false);

            invoker.Dispose();
            Assert.Equal(0, handler.DisposeCount);

            Assert.Throws<ObjectDisposedException>(() => { Task t = invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None); });
            Assert.Equal(0, handler.SendAsyncCount);
        }

        #region Helpers

        private class MockHandler : HttpMessageHandler
        {
            public int DisposeCount { get; private set; }
            public int SendAsyncCount { get; private set; }

            public MockHandler()
            {
            }
                        
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                SendAsyncCount++;

                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }
        }

        #endregion Helepers
    }
}
