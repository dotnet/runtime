// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Text;
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


        [Fact]
        public void Test()
        //public async void Test()
        {
            /*var listener = new HttpEventListener();
            // Not needed in clear text scenario, just a remnant of the original test.
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = delegate { return true; }
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(100),
                DefaultRequestVersion = HttpVersion.Version20,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            var response = await client.GetAsync($"http://localhost:5001/sendBytes?length={BYTE_LENGTH}");

            Console.WriteLine(response.StatusCode);*/



            Console.WriteLine("Buffer size 8MB");

           // var listener = new HttpEventListener();

            var timer = Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < 3; ++i)
            {
                timer.Restart();
                using (var handler = new SocketsHttpHandler())
                {
                    var result = TestHandler(handler, new Version(2, 0));
                    result.Wait();
                    timer.Stop();

                    Console.WriteLine($"SocketsHttpHandler (Success: {result.Result}) HTTP/2.0 in {timer.ElapsedMilliseconds}ms ({BYTE_LENGTH / timer.ElapsedMilliseconds / 1000:N3} MB/s)");
                }
            }
        }

        const uint BYTE_LENGTH = 26_214_400; // 25MB

        static HttpRequestMessage GenerateRequestMessage(Version httpVersion, uint bytes)
        {
            // Replace the URL below with the URL of server that can generate an arbitrary number of bytes
            return new HttpRequestMessage(HttpMethod.Get, $"<URL>&length={bytes}")
            {
                Version = httpVersion
            };
        }

        static async Task<bool> TestHandler(HttpMessageHandler handler, Version httpVersion)
        {
            using (var client = new HttpClient(handler, false))
            {
                var message = GenerateRequestMessage(httpVersion, BYTE_LENGTH);
                var response = await client.SendAsync(message);

                return response.IsSuccessStatusCode;
            }
        }
    }
    internal sealed class HttpEventListener : EventListener
    {

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http")
                EnableEvents(eventSource, EventLevel.LogAlways);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var sb = new StringBuilder().Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}[{eventData.EventName}] ");
            for (int i = 0; i < eventData.Payload?.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
            }
            Console.WriteLine(sb.ToString());
        }
    }
}
