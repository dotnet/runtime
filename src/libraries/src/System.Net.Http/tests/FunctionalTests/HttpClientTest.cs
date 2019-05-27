// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public sealed partial class HttpClientTest
    {
        [Fact]
        public void Dispose_MultipleTimes_Success()
        {
            var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage())));
            client.Dispose();
            client.Dispose();
        }

        [Fact]
        public void DefaultRequestHeaders_Idempotent()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                Assert.NotNull(client.DefaultRequestHeaders);
                Assert.Same(client.DefaultRequestHeaders, client.DefaultRequestHeaders);
            }
        }

        [Fact]
        public void BaseAddress_Roundtrip_Equal()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                Assert.Null(client.BaseAddress);

                Uri uri = new Uri(CreateFakeUri());
                client.BaseAddress = uri;
                Assert.Equal(uri, client.BaseAddress);

                client.BaseAddress = null;
                Assert.Null(client.BaseAddress);
            }
        }

        [Fact]
        public void BaseAddress_InvalidUri_Throws()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                AssertExtensions.Throws<ArgumentException>("value", () => client.BaseAddress = new Uri("ftp://onlyhttpsupported"));
                AssertExtensions.Throws<ArgumentException>("value", () => client.BaseAddress = new Uri("/onlyabsolutesupported", UriKind.Relative));
            }
        }

        [Fact]
        public void Timeout_Roundtrip_Equal()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
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
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => client.Timeout = TimeSpan.FromSeconds(-2));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => client.Timeout = TimeSpan.FromSeconds(0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => client.Timeout = TimeSpan.FromSeconds(int.MaxValue));
            }
        }

        [Fact]
        public void MaxResponseContentBufferSize_Roundtrip_Equal()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
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
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => client.MaxResponseContentBufferSize = -1);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => client.MaxResponseContentBufferSize = 0);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => client.MaxResponseContentBufferSize = 1 + (long)int.MaxValue);
            }
        }

        [Theory]
        [InlineData(1, 2, true)]
        [InlineData(1, 127, true)]
        [InlineData(254, 255, true)]
        [InlineData(10, 256, true)]
        [InlineData(1, 440, true)]
        [InlineData(2, 1, false)]
        [InlineData(2, 2, false)]
        [InlineData(1000, 1000, false)]
        public async Task MaxResponseContentBufferSize_ThrowsIfTooSmallForContent(int maxSize, int contentLength, bool exceptionExpected)
        {
            var content = new CustomContent(async s =>
            {
                await s.WriteAsync(TestHelper.GenerateRandomContent(contentLength));
            });

            var handler = new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage() { Content = content }));

            using (var client = new HttpClient(handler))
            {
                client.MaxResponseContentBufferSize = maxSize;

                if (exceptionExpected)
                {
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(CreateFakeUri()));
                }
                else
                {
                    await client.GetAsync(CreateFakeUri());
                }
            }
        }

        [Fact]
        public async Task Properties_CantChangeAfterOperation_Throws()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r,c) => Task.FromResult(new HttpResponseMessage()))))
            {
                (await client.GetAsync(CreateFakeUri())).Dispose();
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
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                Assert.Throws<InvalidOperationException>(() => { client.GetAsync(uri == null ? null : new Uri(uri, UriKind.RelativeOrAbsolute)); });
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("/")]
        public async Task GetAsync_BaseAddress_ValidUri_Success(string uri)
        {
            using (var client = new HttpClient(new CustomResponseHandler((r,c) => Task.FromResult(new HttpResponseMessage()))))
            {
                client.BaseAddress = new Uri(CreateFakeUri());
                using (HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetContentAsync_ErrorStatusCode_ExpectedExceptionThrown(bool withResponseContent)
        {
            using (var client = new HttpClient(new CustomResponseHandler(
                (r,c) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = withResponseContent ? new ByteArrayContent(new byte[1]) : null
                }))))
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync(CreateFakeUri()));
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetByteArrayAsync(CreateFakeUri()));
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStreamAsync(CreateFakeUri()));
            }
        }

        [Fact]
        public async Task GetContentAsync_NullResponse_Throws()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r,c) => Task.FromResult<HttpResponseMessage>(null))))
            {
                await Assert.ThrowsAnyAsync<InvalidOperationException>(() => client.GetStringAsync(CreateFakeUri()));
            }
        }

        [Fact]
        public async Task GetContentAsync_NullResponseContent_ReturnsDefaultValue()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r,c) => Task.FromResult(new HttpResponseMessage() { Content = null }))))
            {
                Assert.Same(string.Empty, await client.GetStringAsync(CreateFakeUri()));
                Assert.Same(Array.Empty<byte>(), await client.GetByteArrayAsync(CreateFakeUri()));
                Assert.Same(Stream.Null, await client.GetStreamAsync(CreateFakeUri()));
            }
        }

        [Fact]
        public async Task GetContentAsync_SerializingContentThrows_Synchronous_Throws()
        {
            var e = new FormatException();
            using (var client = new HttpClient(new CustomResponseHandler(
                (r, c) => Task.FromResult(new HttpResponseMessage() { Content = new CustomContent(stream => { throw e; }) }))))
            {
                Assert.Same(e, await Assert.ThrowsAsync<FormatException>(() => client.GetStringAsync(CreateFakeUri())));
                Assert.Same(e, await Assert.ThrowsAsync<FormatException>(() => client.GetByteArrayAsync(CreateFakeUri())));
                Assert.Same(e, await Assert.ThrowsAsync<FormatException>(() => client.GetStreamAsync(CreateFakeUri())));
            }
        }

        [Fact]
        public async Task GetContentAsync_SerializingContentThrows_Asynchronous_Throws()
        {
            var e = new FormatException();
            using (var client = new HttpClient(new CustomResponseHandler(
                (r, c) => Task.FromResult(new HttpResponseMessage() { Content = new CustomContent(stream => Task.FromException(e)) }))))
            {
                Assert.Same(e, await Assert.ThrowsAsync<FormatException>(() => client.GetStringAsync(CreateFakeUri())));
                Assert.Same(e, await Assert.ThrowsAsync<FormatException>(() => client.GetByteArrayAsync(CreateFakeUri())));
                Assert.Same(e, await Assert.ThrowsAsync<FormatException>(() => client.GetStreamAsync(CreateFakeUri())));
            }
        }

        [Fact]
        public async Task GetPutPostDeleteAsync_Canceled_Throws()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                var content = new ByteArrayContent(new byte[1]);
                var cts = new CancellationTokenSource();

                Task t1 = client.GetAsync(CreateFakeUri(), cts.Token);
                Task t2 = client.GetAsync(CreateFakeUri(), HttpCompletionOption.ResponseContentRead, cts.Token);
                Task t3 = client.PostAsync(CreateFakeUri(), content, cts.Token);
                Task t4 = client.PutAsync(CreateFakeUri(), content, cts.Token);
                Task t5 = client.DeleteAsync(CreateFakeUri(), cts.Token);

                cts.Cancel();

                await Assert.ThrowsAsync<TaskCanceledException>(() => t1);
                await Assert.ThrowsAsync<TaskCanceledException>(() => t2);
                await Assert.ThrowsAsync<TaskCanceledException>(() => t3);
                await Assert.ThrowsAsync<TaskCanceledException>(() => t4);
                await Assert.ThrowsAsync<TaskCanceledException>(() => t5);
            }
        }

        [Fact]
        public async Task GetPutPostDeleteAsync_Success()
        {
            Action<HttpResponseMessage> verify = message => { using (message) Assert.Equal(HttpStatusCode.OK, message.StatusCode); };
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                verify(await client.GetAsync(CreateFakeUri()));
                verify(await client.GetAsync(CreateFakeUri(), CancellationToken.None));
                verify(await client.GetAsync(CreateFakeUri(), HttpCompletionOption.ResponseContentRead));
                verify(await client.GetAsync(CreateFakeUri(), HttpCompletionOption.ResponseContentRead, CancellationToken.None));

                verify(await client.PostAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])));
                verify(await client.PostAsync(CreateFakeUri(), new ByteArrayContent(new byte[1]), CancellationToken.None));

                verify(await client.PutAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])));
                verify(await client.PutAsync(CreateFakeUri(), new ByteArrayContent(new byte[1]), CancellationToken.None));

                verify(await client.DeleteAsync(CreateFakeUri()));
                verify(await client.DeleteAsync(CreateFakeUri(), CancellationToken.None));
            }
        }

        [Fact]
        public void GetAsync_CustomException_Synchronous_ThrowsException()
        {
            var e = new FormatException();
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => { throw e; })))
            {
                FormatException thrown = Assert.Throws<FormatException>(() => { client.GetAsync(CreateFakeUri()); });
                Assert.Same(e, thrown);
            }
        }

        [Fact]
        public async Task GetAsync_CustomException_Asynchronous_ThrowsException()
        {
            var e = new FormatException();
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromException<HttpResponseMessage>(e))))
            {
                FormatException thrown = await Assert.ThrowsAsync<FormatException>(() => client.GetAsync(CreateFakeUri()));
                Assert.Same(e, thrown);
            }
        }

        [Fact]
        public void SendAsync_NullRequest_ThrowsException()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r,c) => Task.FromResult<HttpResponseMessage>(null))))
            {
                AssertExtensions.Throws<ArgumentNullException>("request", () => { client.SendAsync(null); });
            }
        }

        [Fact]
        public async Task SendAsync_DuplicateRequest_ThrowsException()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            using (var request = new HttpRequestMessage(HttpMethod.Get, CreateFakeUri()))
            {
                (await client.SendAsync(request)).Dispose();
                Assert.Throws<InvalidOperationException>(() => { client.SendAsync(request); });
            }
        }

        [Fact]
        public async Task SendAsync_RequestContentNotDisposed()
        {
            var content = new ByteArrayContent(new byte[1]);
            using (var request = new HttpRequestMessage(HttpMethod.Get, CreateFakeUri()) { Content = content }) 
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                await client.SendAsync(request);
                await content.ReadAsStringAsync(); // no exception
            }
        }

        [Fact]
        public void Dispose_UseAfterDispose_Throws()
        {
            var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage())));
            client.Dispose();

            Assert.Throws<ObjectDisposedException>(() => client.BaseAddress = null);
            Assert.Throws<ObjectDisposedException>(() => client.CancelPendingRequests());
            Assert.Throws<ObjectDisposedException>(() => { client.DeleteAsync(CreateFakeUri()); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetAsync(CreateFakeUri()); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetByteArrayAsync(CreateFakeUri()); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetStreamAsync(CreateFakeUri()); });
            Assert.Throws<ObjectDisposedException>(() => { client.GetStringAsync(CreateFakeUri()); });
            Assert.Throws<ObjectDisposedException>(() => { client.PostAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])); });
            Assert.Throws<ObjectDisposedException>(() => { client.PutAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])); });
            Assert.Throws<ObjectDisposedException>(() => { client.SendAsync(new HttpRequestMessage(HttpMethod.Get, CreateFakeUri())); });
            Assert.Throws<ObjectDisposedException>(() => { client.Timeout = TimeSpan.FromSeconds(1); });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CancelAllPending_AllPendingOperationsCanceled(bool withInfiniteTimeout)
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                if (withInfiniteTimeout)
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;
                }
                Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 3).Select(_ => client.GetAsync(CreateFakeUri())).ToArray();
                client.CancelPendingRequests();
                Assert.All(tasks, task => Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult()));
            }
        }

        [Fact]
        public void Timeout_TooShort_AllPendingOperationsCanceled()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                client.Timeout = TimeSpan.FromMilliseconds(1);
                Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 3).Select(_ => client.GetAsync(CreateFakeUri())).ToArray();
                Assert.All(tasks, task => Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult()));
            }
        }

        [Fact]
        [OuterLoop("One second delay in getting server's response")]
        public async Task Timeout_SetTo30AndGetResponseQuickly_Success()
        {
            var handler = new CustomResponseHandler(async (r, c) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
                return new HttpResponseMessage();
            });

            using (var client = new HttpClient(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                await client.GetAsync(CreateFakeUri());
            }
        }

        private static string CreateFakeUri() => $"http://{Guid.NewGuid().ToString("N")}";

        private static async Task<T> WhenCanceled<T>(CancellationToken cancellationToken)
        {
            await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            return default(T);
        }

        private sealed class CustomResponseHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _func;

            public CustomResponseHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> func) { _func = func; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _func(request, cancellationToken);
            }
        }

        private sealed class CustomContent : HttpContent
        {
            private readonly Func<Stream, Task> _func;

            public CustomContent(Func<Stream, Task> func) { _func = func; }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return _func(stream);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }
    }
}
