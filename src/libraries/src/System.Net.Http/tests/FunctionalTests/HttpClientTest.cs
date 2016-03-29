// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientTest
    {
        [Fact]
        public void DefaultRequestHeaders_Idempotent()
        {
            using (var client = new HttpClient())
            {
                Assert.NotNull(client.DefaultRequestHeaders);
                Assert.Same(client.DefaultRequestHeaders, client.DefaultRequestHeaders);
            }
        }

        [Fact]
        public void BaseAddress_Roundtrip_Equal()
        {
            using (var client = new HttpClient())
            {
                Assert.Null(client.BaseAddress);

                client.BaseAddress = HttpTestServers.RemoteEchoServer;
                Assert.Equal(HttpTestServers.RemoteEchoServer, client.BaseAddress);

                client.BaseAddress = null;
                Assert.Null(client.BaseAddress);
            }
        }

        [Fact]
        public void BaseAddress_InvalidUri_Throws()
        {
            using (var client = new HttpClient())
            {
                Assert.Throws<ArgumentException>("value", () => client.BaseAddress = new Uri("ftp://onlyhttpsupported"));
                Assert.Throws<ArgumentException>("value", () => client.BaseAddress = new Uri("/onlyabsolutesupported", UriKind.Relative));
            }
        }

        [Fact]
        public void Timeout_Roundtrip_Equal()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);

                client.Timeout = TimeSpan.FromSeconds(1);
                Assert.Equal(TimeSpan.FromSeconds(1), client.Timeout);
            }
        }

        [Fact]
        public void Timeout_OutOfRange_Throws()
        {
            using (var client = new HttpClient())
            {
                Assert.Throws<ArgumentOutOfRangeException>("value", () => client.Timeout = TimeSpan.FromSeconds(-2));
                Assert.Throws<ArgumentOutOfRangeException>("value", () => client.Timeout = TimeSpan.FromSeconds(0));
                Assert.Throws<ArgumentOutOfRangeException>("value", () => client.Timeout = TimeSpan.FromSeconds(int.MaxValue));
            }
        }

        [Fact]
        public void MaxResponseContentBufferSize_Roundtrip_Equal()
        {
            using (var client = new HttpClient())
            {
                client.MaxResponseContentBufferSize = 1;
                Assert.Equal(1, client.MaxResponseContentBufferSize);

                client.MaxResponseContentBufferSize = int.MaxValue;
                Assert.Equal(int.MaxValue, client.MaxResponseContentBufferSize);
            }
        }

        [Fact]
        public void MaxResponseContentBufferSize_OutOfRange_Throws()
        {
            using (var client = new HttpClient())
            {
                Assert.Throws<ArgumentOutOfRangeException>("value", () => client.MaxResponseContentBufferSize = -1);
                Assert.Throws<ArgumentOutOfRangeException>("value", () => client.MaxResponseContentBufferSize = 0);
                Assert.Throws<ArgumentOutOfRangeException>("value", () => client.MaxResponseContentBufferSize = 1 + (long)int.MaxValue);
            }
        }

        [Fact]
        public async Task MaxResponseContentBufferSize_TooSmallForContent_Throws()
        {
            using (var client = new HttpClient())
            {
                client.MaxResponseContentBufferSize = 1;
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync(HttpTestServers.RemoteEchoServer));
            }
        }

        [Fact]
        public async Task Properties_CantChangeAfterOperation_Throws()
        {
            using (var client = new HttpClient())
            {
                (await client.GetAsync(HttpTestServers.RemoteEchoServer)).Dispose();
                Assert.Throws<InvalidOperationException>(() => client.BaseAddress = null);
                Assert.Throws<InvalidOperationException>(() => client.Timeout = TimeSpan.FromSeconds(1));
                Assert.Throws<InvalidOperationException>(() => client.MaxResponseContentBufferSize = 1);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("/something.html")]
        public void GetAsync_NoBaseAddress_InvalidUri_ThrowsException(string uri)
        {
            using (var client = new HttpClient())
            {
                Assert.Throws<InvalidOperationException>(() => { client.GetAsync(uri == null ? null : new Uri(uri, UriKind.RelativeOrAbsolute)); });
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("/")]
        public async Task GetAsync_BaseAddress_ValidUri_Success(string uri)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri($"http://{HttpTestServers.Host}");
                using (HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task GetContentAsync_ErrorStatusCode_ExpectedExceptionThrown()
        {
            using (var client = new HttpClient())
            {
                const int statusCode = 418;
                string url = HttpTestServers.StatusCodeUri(false, statusCode).ToString();

                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync(url));
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetByteArrayAsync(url));
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStreamAsync(url));
            }
        }

        [Fact]
        public async Task GetContentAsync_NullResponse_Throws()
        {
            using (var client = new HttpClient(new CustomResponseHandler { Response = null }))
            {
                await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.GetStringAsync(HttpTestServers.RemoteEchoServer));
            }
        }

        [Fact]
        public async Task GetContentAsync_NullResponseContent_ReturnsDefaultValue()
        {
            using (var client = new HttpClient(new CustomResponseHandler { Response = new HttpResponseMessage() { Content = null } }))
            {
                Assert.Same(string.Empty, await client.GetStringAsync(HttpTestServers.RemoteEchoServer));
                Assert.Same(Array.Empty<byte>(), await client.GetByteArrayAsync(HttpTestServers.RemoteEchoServer));
                Assert.Same(Stream.Null, await client.GetStreamAsync(HttpTestServers.RemoteEchoServer));
            }
        }

        [Fact]
        public async Task GetAsync_InvalidUrl_ExpectedExceptionThrown()
        {
            using (var client = new HttpClient())
            {
                string url = "http://" + Guid.NewGuid().ToString("N");
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync(url));
            }
        }

        [Fact]
        public async Task GetPutPostAsync_Canceled_Throws()
        {
            using (var client = new HttpClient())
            {
                string delayUri = HttpTestServers.DelayResponseServer(60).ToString();
                var content = new ByteArrayContent(new byte[1]);
                var cts = new CancellationTokenSource();

                Task t1 = client.GetAsync(delayUri, cts.Token);
                Task t2 = client.GetAsync(delayUri, HttpCompletionOption.ResponseContentRead, cts.Token);
                Task t3 = client.PostAsync(delayUri, content, cts.Token);
                Task t4 = client.PutAsync(delayUri, content, cts.Token);

                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t1);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t2);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t3);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t4);
            }
        }

        [Fact]
        public void SendAsync_NullRequest_ThrowsException()
        {
            using (var client = new HttpClient())
            {
                Assert.Throws<ArgumentNullException>("request", () => { client.SendAsync(null); });
            }
        }

        [Fact]
        public async Task SendAsync_DuplicateRequest_ThrowsException()
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, HttpTestServers.RemoteEchoServer))
            {
                (await client.SendAsync(request)).Dispose();
                Assert.Throws<InvalidOperationException>(() => { client.SendAsync(request); });
            }
        }

        [Fact]
        public void Dispose_UseAfterDispose_Throws()
        {
            var client = new HttpClient();
            client.Dispose();

            Assert.Throws<ObjectDisposedException>(() => client.BaseAddress = null);
            Assert.Throws<ObjectDisposedException>(() => client.CancelPendingRequests());
            Assert.Throws<ObjectDisposedException>(() => { client.DeleteAsync(HttpTestServers.RemoteEchoServer); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetAsync(HttpTestServers.RemoteEchoServer); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetByteArrayAsync(HttpTestServers.RemoteEchoServer); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetStreamAsync(HttpTestServers.RemoteEchoServer); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetStringAsync(HttpTestServers.RemoteEchoServer); });
            Assert.Throws<ObjectDisposedException>(() => { client.PostAsync(HttpTestServers.RemoteEchoServer, new ByteArrayContent(new byte[1])); });
            Assert.Throws<ObjectDisposedException>(() => { client.PutAsync(HttpTestServers.RemoteEchoServer, new ByteArrayContent(new byte[1])); });
            Assert.Throws<ObjectDisposedException>(() => { client.SendAsync(new HttpRequestMessage(HttpMethod.Get, HttpTestServers.RemoteEchoServer)); });
            Assert.Throws<ObjectDisposedException>(() => { client.Timeout = TimeSpan.FromSeconds(1); });
        }

        [Fact]
        public async Task DeleteAsync_NotDeletable_NoExceptionThrown()
        {
            using (var client = new HttpClient())
            {
                string url = HttpTestServers.RemoteEchoServer.ToString();

                using (HttpResponseMessage response = await client.DeleteAsync(url))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                using (HttpResponseMessage response = await client.DeleteAsync(url, CancellationToken.None))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public void CancelAllPending_AllPendingOperationsCanceled()
        {
            using (var client = new HttpClient())
            {
                string url = HttpTestServers.DelayResponseServer(60);
                Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 3).Select(_ => client.GetAsync(url)).ToArray();
                client.CancelPendingRequests();
                Assert.All(tasks, task => Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult()));
            }
        }

        [Fact]
        public void Timeout_TooShort_AllPendingOperationsCanceled()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMilliseconds(1);
                string url = HttpTestServers.DelayResponseServer(60);
                Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 3).Select(_ => client.GetAsync(url)).ToArray();
                Assert.All(tasks, task => Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult()));
            }
        }

        [Fact]
        [OuterLoop]
        public void Timeout_SetTo60AndGetResponseFromServerWhichTakes40_Success()
        {
            // TODO: This server path will change once the final test infrastructure is in place (Issue #1477).
            const string SlowServer = "http://httpbin.org/drip?numbytes=1&duration=1&delay=40&code=200";
            
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                var response = client.GetAsync(SlowServer).GetAwaiter().GetResult();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceLogging()
        {
            bool requestLogged = false;
            Guid requestGuid = Guid.Empty;
            bool responseLogged = false;
            Guid responseGuid = Guid.Empty;

            var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(
                kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        Assert.NotNull(kvp.Value);
                        GetPropertyValueFromAnonymousTypeInstance<HttpRequestMessage>(kvp.Value, "Request");
                        requestGuid = GetPropertyValueFromAnonymousTypeInstance<Guid>(kvp.Value, "LoggingRequestId");

                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        Assert.NotNull(kvp.Value);

                        GetPropertyValueFromAnonymousTypeInstance<HttpResponseMessage>(kvp.Value, "Response");
                        responseGuid = GetPropertyValueFromAnonymousTypeInstance<Guid>(kvp.Value, "LoggingRequestId");

                        responseLogged = true;
                    }
                });

            using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
            {
                diagnosticListenerObserver.Enable();
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(HttpTestServers.RemoteEchoServer).Result;
                }

                Assert.True(requestLogged, "Request was not logged.");
                // Poll with a timeout since logging response is not synchronized with returning a response.
                WaitForTrue(() => responseLogged, TimeSpan.FromSeconds(1), "Response was not logged within 1 second timeout.");
                Assert.Equal(requestGuid, responseGuid);
                diagnosticListenerObserver.Disable();
            }
        }

        /// <remarks>
        /// This test must be in the same test collection as any others testing HttpClient/WinHttpHandler
        /// DiagnosticSources, since the global logging mechanism makes them conflict inherently.
        /// </remarks>
        [Fact]
        public void SendAsync_ExpectedDiagnosticSourceNoLogging()
        {
            bool requestLogged = false;
            bool responseLogged = false;

            var diagnosticListenerObserver = new FakeDiagnosticListenerObserver(
                kvp =>
                {
                    if (kvp.Key.Equals("System.Net.Http.Request"))
                    {
                        requestLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Net.Http.Response"))
                    {
                        responseLogged = true;
                    }
                });

            using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(HttpTestServers.RemoteEchoServer).Result;
                }

                Assert.False(requestLogged, "Request was logged while logging disabled.");
                // TODO: Waiting for one second is not ideal, but how else be reasonably sure that
                // some logging message hasn't slipped through?
                WaitForFalse(() => responseLogged, TimeSpan.FromSeconds(1), "Response was logged while logging disabled.");
            }
        }

        private void WaitForTrue(Func<bool> p, TimeSpan timeout, string message)
        {
            // Assert that spin doesn't time out.
            Assert.True(System.Threading.SpinWait.SpinUntil(p, timeout), message);
        }

        private void WaitForFalse(Func<bool> p, TimeSpan timeout, string message)
        {
            // Assert that spin times out.
            Assert.False(System.Threading.SpinWait.SpinUntil(p, timeout), message);
        }

        private T GetPropertyValueFromAnonymousTypeInstance<T>(object obj, string propertyName)
        {
            Type t = obj.GetType();

            PropertyInfo p = t.GetRuntimeProperty(propertyName);

            object propertyValue = p.GetValue(obj);
            Assert.NotNull(propertyValue);
            Assert.IsAssignableFrom<T>(propertyValue);

            return (T)propertyValue;
        }

        private sealed class CustomResponseHandler : HttpMessageHandler
        {
            public HttpResponseMessage Response;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult<HttpResponseMessage>(Response);
            }
        }
    }
}
