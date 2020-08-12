// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static System.Net.Test.Common.Configuration.Http;

namespace System.Net.Http.Functional.Tests
{
    public sealed class HttpClientTest : HttpClientHandlerTestBase
    {
        public HttpClientTest(ITestOutputHelper output) : base(output) { }

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
                HttpRequestException ex;

                ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync(CreateFakeUri()));
                Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);

                ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetByteArrayAsync(CreateFakeUri()));
                Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);

                ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStreamAsync(CreateFakeUri()));
                Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
            }
        }

        [Fact]
        [OuterLoop("Failing connection attempts take long on windows")]
        public async Task GetContentAsync_WhenCanNotConnect_ExceptionContainsHostInfo()
        {
            using Socket portReserver = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            portReserver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            IPEndPoint ep = (IPEndPoint)portReserver.LocalEndPoint;

            using var client = CreateHttpClient();

            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStreamAsync($"http://localhost:{ep.Port}"));
            Assert.Contains($"localhost:{ep.Port}", ex.Message);
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

                Stream s = await client.GetStreamAsync(CreateFakeUri());
                Assert.NotNull(s);
                Assert.Equal(-1, s.ReadByte());
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
            static void Verify(HttpResponseMessage message)
            {
                using (message)
                {
                    Assert.Equal(HttpStatusCode.OK, message.StatusCode);
                }
            }

            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                Verify(await client.GetAsync(CreateFakeUri()));
                Verify(await client.GetAsync(CreateFakeUri(), CancellationToken.None));
                Verify(await client.GetAsync(CreateFakeUri(), HttpCompletionOption.ResponseContentRead));
                Verify(await client.GetAsync(CreateFakeUri(), HttpCompletionOption.ResponseContentRead, CancellationToken.None));

                Verify(await client.PostAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])));
                Verify(await client.PostAsync(CreateFakeUri(), new ByteArrayContent(new byte[1]), CancellationToken.None));

                Verify(await client.PutAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])));
                Verify(await client.PutAsync(CreateFakeUri(), new ByteArrayContent(new byte[1]), CancellationToken.None));

                Verify(await client.DeleteAsync(CreateFakeUri()));
                Verify(await client.DeleteAsync(CreateFakeUri(), CancellationToken.None));
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
        public async Task GetStringAsync_Success()
        {
            string content = Guid.NewGuid().ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    string received = await httpClient.GetStringAsync(uri);
                    Assert.Equal(content, received);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content);
                });
        }

        [Fact]
        public async Task GetStringAsync_CanBeCanceled_AlreadyCanceledCts()
        {
            var onClientFinished = new SemaphoreSlim(0, 1);

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => httpClient.GetStringAsync(uri, cts.Token));
                    onClientFinished.Release();
                },
                async server =>
                {
                    Assert.True(await onClientFinished.WaitAsync(5000), "OnClientFinished timed out");
                });
        }

        [Fact]
        public async Task GetStringAsync_CanBeCanceled()
        {
            var cts = new CancellationTokenSource();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => httpClient.GetStringAsync(uri, cts.Token));
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        cts.Cancel();
                        try
                        {
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                        }
                        catch { }
                    });
                });
        }

        [Fact]
        public async Task GetByteArrayAsync_Success()
        {
            string content = Guid.NewGuid().ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    byte[] receivedBytes = await httpClient.GetByteArrayAsync(uri);
                    string received = Encoding.UTF8.GetString(receivedBytes);
                    Assert.Equal(content, received);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content);
                });
        }

        [Fact]
        public async Task GetByteArrayAsync_CanBeCanceled_AlreadyCanceledCts()
        {
            var onClientFinished = new SemaphoreSlim(0, 1);

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => httpClient.GetByteArrayAsync(uri, cts.Token));
                    onClientFinished.Release();
                },
                async server =>
                {
                    Assert.True(await onClientFinished.WaitAsync(5000), "OnClientFinished timed out");
                });
        }

        [Fact]
        public async Task GetByteArrayAsync_CanBeCanceled()
        {
            var cts = new CancellationTokenSource();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => httpClient.GetByteArrayAsync(uri, cts.Token));
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        cts.Cancel();
                        try
                        {
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                        }
                        catch { }
                    });
                });
        }

        [Fact]
        public async Task GetStreamAsync_Success()
        {
            string content = Guid.NewGuid().ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    Stream receivedStream = await httpClient.GetStreamAsync(uri);
                    using var ms = new MemoryStream();
                    await receivedStream.CopyToAsync(ms);
                    byte[] receivedBytes = ms.ToArray();
                    string received = Encoding.UTF8.GetString(receivedBytes);
                    Assert.Equal(content, received);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content);
                });
        }

        [Fact]
        public async Task GetStreamAsync_CanBeCanceled_AlreadyCanceledCts()
        {
            var onClientFinished = new SemaphoreSlim(0, 1);

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => httpClient.GetStreamAsync(uri, cts.Token));
                    onClientFinished.Release();
                },
                async server =>
                {
                    Assert.True(await onClientFinished.WaitAsync(5000), "OnClientFinished timed out");
                });
        }

        [Fact]
        public async Task GetStreamAsync_CanBeCanceled()
        {
            var cts = new CancellationTokenSource();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => httpClient.GetStreamAsync(uri, cts.Token));
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        cts.Cancel();
                        try
                        {
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                        }
                        catch { }
                    });
                });
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
            Assert.Throws<ObjectDisposedException>(() => { client.Send(new HttpRequestMessage(HttpMethod.Get, CreateFakeUri())); });
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

        [Theory]
        [InlineData(HttpCompletionOption.ResponseContentRead)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead)]
        public void Timeout_TooShort_AllPendingOperationsCanceled(HttpCompletionOption completionOption)
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                client.Timeout = TimeSpan.FromMilliseconds(10);
                Task<HttpResponseMessage>[] tasks = Enumerable.Range(0, 3).Select(_ => client.GetAsync(CreateFakeUri(), completionOption)).ToArray();
                Assert.All(tasks, task => {
                    OperationCanceledException e = Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult());
                    TimeoutException timeoutException = (TimeoutException)e.InnerException;
                    Assert.NotNull(timeoutException);
                    Assert.NotNull(timeoutException.InnerException);
                });
            }
        }

        [Theory]
        [InlineData(HttpCompletionOption.ResponseContentRead)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead)]
        public async Task Timeout_CallerCanceledTokenAfterTimeout_TimeoutIsNotDetected(HttpCompletionOption completionOption)
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                client.Timeout = TimeSpan.FromMilliseconds(0.01);
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;
                cts.Cancel();
                Task<HttpResponseMessage> task = client.GetAsync(CreateFakeUri(), completionOption, token);
                OperationCanceledException e = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
                Assert.Null(e.InnerException);
            }
        }

        [Theory]
        [InlineData(HttpCompletionOption.ResponseContentRead)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead)]
        public void Timeout_CallerCanceledTokenBeforeTimeout_TimeoutIsNotDetected(HttpCompletionOption completionOption)
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                client.Timeout = TimeSpan.FromDays(1);
                CancellationTokenSource cts = new CancellationTokenSource();
                Task<HttpResponseMessage> task = client.GetAsync(CreateFakeUri(), completionOption, cts.Token);
                cts.Cancel();
                OperationCanceledException e = Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult());
                Assert.Null(e.InnerException);
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

        [Fact]
        public void DefaultProxy_SetNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => HttpClient.DefaultProxy = null );
        }

        [Fact]
        public void DefaultProxy_Get_ReturnsNotNull()
        {
            IWebProxy proxy = HttpClient.DefaultProxy;
            Assert.NotNull(proxy);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DefaultProxy_SetGet_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy proxy = new WebProxy("http://localhost:3128/");
                HttpClient.DefaultProxy = proxy;
                Assert.True(Object.ReferenceEquals(proxy, HttpClient.DefaultProxy));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DefaultProxy_Credentials_SetGet_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy proxy = HttpClient.DefaultProxy;
                ICredentials nc = proxy.Credentials;

                proxy.Credentials = null;
                Assert.Null(proxy.Credentials);

                proxy.Credentials = nc;
                Assert.Same(nc, proxy.Credentials);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public async Task PatchAsync_Canceled_Throws()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                var content = new ByteArrayContent(new byte[1]);
                var cts = new CancellationTokenSource();

                Task t1 = client.PatchAsync(CreateFakeUri(), content, cts.Token);

                cts.Cancel();

                await Assert.ThrowsAsync<TaskCanceledException>(() => t1);
            }
        }

        [Fact]
        public async Task PatchAsync_Success()
        {
            static void Verify(HttpResponseMessage message)
            {
                using (message)
                {
                    Assert.Equal(HttpStatusCode.OK, message.StatusCode);
                }
            }

            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                Verify(await client.PatchAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])));
                Verify(await client.PatchAsync(CreateFakeUri(), new ByteArrayContent(new byte[1]), CancellationToken.None));
            }
        }

        [Fact]
        public void Dispose_UsePatchAfterDispose_Throws()
        {
            var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage())));
            client.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { client.PatchAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])); });
        }

        [Theory]
        [InlineData(HttpCompletionOption.ResponseContentRead)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead)]
        public void Send_SingleThread_Succeeds(HttpCompletionOption completionOption)
        {
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;

            var client = new HttpClient(new CustomResponseHandler((r, c) => 
            {
                Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
                return Task.FromResult(new HttpResponseMessage()
                    {
                        Content = new CustomContent(stream =>
                        {
                            Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
                        })
                    });
            }));
            using (client)
            {
                HttpResponseMessage response = client.Send(new HttpRequestMessage(HttpMethod.Get, CreateFakeUri())
                    {
                        Content = new CustomContent(stream =>
                        {
                            Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
                        })
                    }, completionOption);
                    
                Stream contentStream = response.Content.ReadAsStream();
                Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
            }
        }

        [Theory]
        [InlineData(HttpCompletionOption.ResponseContentRead)]
        [InlineData(HttpCompletionOption.ResponseHeadersRead)]
        public async Task Send_SingleThread_Loopback_Succeeds(HttpCompletionOption completionOption)
        {
            string content = "Test content";

            ManualResetEventSlim mres = new ManualResetEventSlim();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    try
                    {
                        // To prevent deadlock
                        await Task.Yield();

                        int currentThreadId = Thread.CurrentThread.ManagedThreadId;

                        using HttpClient httpClient = CreateHttpClient();

                        HttpResponseMessage response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, uri) {
                            Content = new CustomContent(stream =>
                            {
                                Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);
                                stream.Write(Encoding.UTF8.GetBytes(content));
                            })
                        }, completionOption);

                        Stream contentStream = response.Content.ReadAsStream();
                        Assert.Equal(currentThreadId, Thread.CurrentThread.ManagedThreadId);                        
                        using (StreamReader sr = new StreamReader(contentStream))
                        {
                            Assert.Equal(content, sr.ReadToEnd());
                        }
                    }
                    finally
                    {
                        mres.Set();
                    }
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendResponseAsync(content: content);

                        // To keep the connection open until the response is fully read.
                        mres.Wait();
                    });
                });
        }

        [Fact]
        [OuterLoop]
        public async Task Send_CancelledRequestContent_Throws()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var sendTask = Task.Run(() => {
                        using HttpClient httpClient = CreateHttpClient();
                        httpClient.Timeout = TimeSpan.FromMinutes(2);

                        HttpResponseMessage response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, uri) {
                            Content = new CustomContent(new Action<Stream>(stream =>
                            {
                                for (int i = 0; i < 100; ++i)
                                {
                                    stream.Write(new byte[] { 0xff });
                                    stream.Flush();
                                    Thread.Sleep(TimeSpan.FromSeconds(0.1));
                                }
                            }))
                        }, cts.Token);
                    });

                    TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => sendTask);
                    Assert.IsNotType<TimeoutException>(ex.InnerException);
                },
                async server =>
                { 
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        try
                        {
                            await connection.ReadRequestHeaderAsync();
                            cts.Cancel();                        
                            await connection.ReadRequestBodyAsync();
                        }
                        catch { }
                    });
                });
        }

        [Fact]
        [OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39056")]
        public async Task Send_TimeoutRequestContent_Throws()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var sendTask = Task.Run(() => {
                        using HttpClient httpClient = CreateHttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(0.5);

                        HttpResponseMessage response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, uri) {
                            Content = new CustomContent(new Action<Stream>(stream =>
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                                for (int i = 0; i < 100; ++i)
                                {
                                    stream.Write(new byte[] { 0xff });
                                    stream.Flush();
                                    Thread.Sleep(TimeSpan.FromSeconds(0.1));
                                }
                            }))
                        });
                    });

                    TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => sendTask);
                    Assert.IsType<TimeoutException>(ex.InnerException);
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        try
                        {
                            await connection.ReadRequestHeaderAsync();
                            await connection.ReadRequestBodyAsync();
                        }
                        catch { }
                    });
                });
        }

        [Fact]
        [OuterLoop]
        public async Task Send_CancelledResponseContent_Throws()
        {
            string content = "Test content";

            CancellationTokenSource cts = new CancellationTokenSource();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var sendTask = Task.Run(() => {
                        using HttpClient httpClient = CreateHttpClient();
                        httpClient.Timeout = TimeSpan.FromMinutes(2);

                        HttpResponseMessage response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, uri) {
                            Content = new CustomContent(stream =>
                            {
                                stream.Write(Encoding.UTF8.GetBytes(content));
                            })
                        }, cts.Token);
                    });

                    TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => sendTask);
                    Assert.IsNotType<TimeoutException>(ex.InnerException);
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        try
                        {
                            await connection.ReadRequestDataAsync();
                            await connection.SendResponseAsync(headers: new List<HttpHeaderData>() {
                                new HttpHeaderData("Content-Length", (content.Length * 100).ToString())
                            });
                            cts.Cancel();
                            for (int i = 0; i < 100; ++i)
                            {
                                await connection.Writer.WriteLineAsync(content);
                                await Task.Delay(TimeSpan.FromSeconds(0.1));
                            }
                        }
                        catch { }
                    });
                }); 
        }

        [Fact]
        [OuterLoop]
        public async Task Send_TimeoutResponseContent_Throws()
        {
            string content = "Test content";

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var sendTask = Task.Run(() => {
                        using HttpClient httpClient = CreateHttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(0.5);
                        HttpResponseMessage response = httpClient.Send(new HttpRequestMessage(HttpMethod.Get, uri));
                    });

                    TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => sendTask);
                    Assert.IsType<TimeoutException>(ex.InnerException);
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        try
                        {
                            await connection.ReadRequestDataAsync();
                            await connection.SendResponseAsync(headers: new List<HttpHeaderData>() {
                                new HttpHeaderData("Content-Length", (content.Length * 100).ToString())
                            });
                            for (int i = 0; i < 100; ++i)
                            {
                                await connection.Writer.WriteLineAsync(content);
                                await connection.Writer.FlushAsync();
                                await Task.Delay(TimeSpan.FromSeconds(0.1));
                            }
                        }
                        catch { }
                    });
                }); 
        }

        public static IEnumerable<object[]> VersionSelectionMemberData()
        {
            var serverOptions = new GenericLoopbackOptions();
            // Either we support SSL (ALPN), or we're testing only clear text.
            foreach (var useSsl in BoolValues.Where(b => serverOptions.UseSsl || !b))
            {
                yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionOrLower, HttpVersion.Version11, useSsl, HttpVersion.Version11 };
                yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionExact, HttpVersion.Version11, useSsl, HttpVersion.Version11 };
                yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion.Version11, useSsl, HttpVersion.Version11 };
                yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionOrLower, HttpVersion.Version20, useSsl, useSsl ? (object)HttpVersion.Version11 : typeof(HttpRequestException) };
                yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionExact, HttpVersion.Version20, useSsl, useSsl ? (object)HttpVersion.Version11 : typeof(HttpRequestException) };
                yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion.Version20, useSsl, useSsl ? (object)HttpVersion.Version20 : typeof(HttpRequestException) };
                if (QuicConnection.IsQuicSupported)
                {
                    yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionOrLower, HttpVersion30, useSsl, HttpVersion.Version11 };
                    yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionExact, HttpVersion30, useSsl, HttpVersion.Version11 };
                    yield return new object[] { HttpVersion.Version11, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion30, useSsl, useSsl ? HttpVersion30 : HttpVersion.Version11 };
                }

                yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionOrLower, HttpVersion.Version11, useSsl, HttpVersion.Version11 };
                yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionExact, HttpVersion.Version11, useSsl, typeof(HttpRequestException) };
                yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion.Version11, useSsl, typeof(HttpRequestException) };
                yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionOrLower, HttpVersion.Version20, useSsl, useSsl ? (object)HttpVersion.Version20 : typeof(HttpRequestException) };
                yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionExact, HttpVersion.Version20, useSsl, HttpVersion.Version20 };
                yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion.Version20, useSsl, HttpVersion.Version20 };
                if (QuicConnection.IsQuicSupported)
                {
                    yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionOrLower, HttpVersion30, useSsl, useSsl ? HttpVersion.Version20 : HttpVersion.Version11 };
                    yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionExact, HttpVersion30, useSsl, HttpVersion.Version20 };
                    yield return new object[] { HttpVersion.Version20, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion30, useSsl, useSsl ? (object)HttpVersion30 : typeof(HttpRequestException) };
                }

                if (QuicConnection.IsQuicSupported)
                {
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionOrLower, HttpVersion.Version11, useSsl, useSsl ? HttpVersion30 : HttpVersion.Version11 };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionExact, HttpVersion.Version11, useSsl, typeof(HttpRequestException) };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion.Version11, useSsl, typeof(HttpRequestException) };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionOrLower, HttpVersion.Version20, useSsl, useSsl ? HttpVersion30 : HttpVersion.Version11 };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionExact, HttpVersion.Version20, useSsl, typeof(HttpRequestException) };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion.Version20, useSsl, typeof(HttpRequestException) };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionOrLower, HttpVersion30, useSsl, useSsl ? HttpVersion30 : HttpVersion.Version11 };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionExact, HttpVersion30, useSsl, useSsl ? (object)HttpVersion30 : typeof(HttpRequestException) };
                    yield return new object[] { HttpVersion30, HttpVersionPolicy.RequestVersionOrHigher, HttpVersion30, useSsl, useSsl ? (object)HttpVersion30 : typeof(HttpRequestException) };
                }
            }
        }

        [Theory]
        [MemberData(nameof(VersionSelectionMemberData))]
        public async Task SendAsync_CorrectVersionSelected_LoopbackServer(Version requestVersion, HttpVersionPolicy versionPolicy, Version serverVersion, bool useSsl, object expectedResult)
        {
            await HttpAgnosticLoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri)
                    {
                        Version = requestVersion,
                        VersionPolicy = versionPolicy
                    };
                    
                    using HttpClientHandler handler = CreateHttpClientHandler();
                    if (useSsl)
                    {
                        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }
                    using HttpClient client = CreateHttpClient(handler);
                    if (expectedResult is Type type)
                    {
                        Exception exception = await Assert.ThrowsAnyAsync<Exception>(() => client.SendAsync(request));
                        Assert.IsType(type, exception);
                        _output.WriteLine("Client expected exception: " + exception.ToString());
                    }
                    else
                    {
                        HttpResponseMessage response = await client.SendAsync(request);
                        Assert.Equal(expectedResult, response.Version);
                    }
                },
                async server =>
                {
                    try
                    {
                        HttpRequestData requestData = await server.AcceptConnectionSendResponseAndCloseAsync();
                        Assert.Equal(expectedResult, requestData.Version);
                    }
                    catch (Exception ex) when (expectedResult is Type)
                    {
                        _output.WriteLine("Server exception: " + ex.ToString());
                    }
                }, httpOptions: new HttpAgnosticOptions()
                {
                    UseSsl = useSsl,
                    ClearTextVersion = serverVersion,
                    SslApplicationProtocols = serverVersion.Major >= 2 ? new List<SslApplicationProtocol>{ SslApplicationProtocol.Http2, SslApplicationProtocol.Http11 } : null
                });
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [MemberData(nameof(VersionSelectionMemberData))]
        public async Task SendAsync_CorrectVersionSelected_ExternalServer(Version requestVersion, HttpVersionPolicy versionPolicy, Version serverVersion, bool useSsl, object expectedResult)
        {
            RemoteServer remoteServer = null;
            if (serverVersion == HttpVersion.Version11)
            {
                remoteServer = useSsl ? RemoteSecureHttp11Server : RemoteHttp11Server;
            }
            if (serverVersion == HttpVersion.Version20)
            {
                remoteServer = useSsl ? RemoteHttp2Server : null;
            }
            // No remote server that could serve the requested version.
            if (remoteServer == null)
            {
                _output.WriteLine($"Skipping test: No remote server that could serve the requested version.");
                return;
            }


            var request = new HttpRequestMessage(HttpMethod.Get, remoteServer.EchoUri)
            {
                Version = requestVersion,
                VersionPolicy = versionPolicy
            };

            using HttpClient client = CreateHttpClient();
            if (expectedResult is Type type)
            {
                Exception exception = await Assert.ThrowsAnyAsync<Exception>(() => client.SendAsync(request));
                Assert.IsType(type, exception);
                _output.WriteLine(exception.ToString());
            }
            else
            {
                HttpResponseMessage response = await client.SendAsync(request);
                Assert.Equal(expectedResult, response.Version);
            }
        }

        [Fact]
        public void DefaultRequestVersion_InitialValueExpected()
        {
            using (var client = new HttpClient())
            {
                Assert.Equal(HttpVersion.Version11, client.DefaultRequestVersion);
                Assert.Same(client.DefaultRequestVersion, client.DefaultRequestVersion);
            }
        }

        [Fact]
        public void DefaultRequestVersion_Roundtrips()
        {
            using (var client = new HttpClient())
            {
                for (int i = 3; i < 5; i++)
                {
                    var newVersion = new Version(i, i, i, i);
                    client.DefaultRequestVersion = newVersion;
                    Assert.Same(newVersion, client.DefaultRequestVersion);
                }
            }
        }

        [Fact]
        public void DefaultRequestVersion_InvalidArgument_Throws()
        {
            using (var client = new HttpClient())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => client.DefaultRequestVersion = null);
                client.DefaultRequestVersion = new Version(1, 0); // still usable after
                Assert.Equal(new Version(1, 0), client.DefaultRequestVersion);
            }
        }

        [Fact]
        public async Task DefaultRequestVersion_SetAfterUse_Throws()
        {
            var handler = new StoreMessageHttpMessageInvoker();
            using (var client = new HttpClient(handler))
            {
                await client.GetAsync("http://doesntmatter", HttpCompletionOption.ResponseHeadersRead);
                Assert.Throws<InvalidOperationException>(() => client.DefaultRequestVersion = new Version(1, 1));
            }
        }

        [Fact]
        public async Task DefaultRequestVersion_UsedInCreatedMessages()
        {
            var handler = new StoreMessageHttpMessageInvoker();
            using (var client = new HttpClient(handler))
            {
                var version = new Version(1, 2, 3, 4);
                client.DefaultRequestVersion = version;
                await client.GetAsync("http://doesntmatter", HttpCompletionOption.ResponseHeadersRead);
                Assert.Same(version, handler.Message.Version);
            }
        }

        private sealed class StoreMessageHttpMessageInvoker : HttpMessageHandler
        {
            public HttpRequestMessage Message;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Message = request;
                return Task.FromResult(new HttpResponseMessage());
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

            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _func(request, cancellationToken).GetAwaiter().GetResult();
            }
        }

        private sealed class CustomContent : HttpContent
        {
            private readonly Func<Stream, Task> _serializeAsync;
            private readonly Action<Stream> _serializeSync;

            public CustomContent(Func<Stream, Task> serializeAsync) { _serializeAsync = serializeAsync; }

            public CustomContent(Action<Stream> serializeSync) { _serializeSync = serializeSync; }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                Debug.Assert(_serializeAsync != null);
                return _serializeAsync(stream);
            }

            protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
            {
                Debug.Assert(_serializeSync != null);
                _serializeSync(stream);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }

        public abstract class HttpClientSendTest : HttpClientHandlerTestBase
        {
            protected HttpClientSendTest(ITestOutputHelper output) : base(output) { }


            [Fact]
            public async Task Send_NullRequest_ThrowsException()
            {
                using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult<HttpResponseMessage>(null))))
                {
                    await AssertExtensions.ThrowsAsync<ArgumentNullException>("request", () => client.SendAsync(TestAsync, null));
                }
            }

            [Fact]
            public async Task Send_DuplicateRequest_ThrowsException()
            {
                using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult<HttpResponseMessage>(new HttpResponseMessage()))))
                using (var request = new HttpRequestMessage(HttpMethod.Get, CreateFakeUri()))
                {
                    (await client.SendAsync(TestAsync, request)).Dispose();
                    await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(TestAsync, request));
                }
            }

            [Fact]
            public async Task Send_RequestContentNotDisposed()
            {
                var content = new ByteArrayContent(new byte[1]);
                using (var request = new HttpRequestMessage(HttpMethod.Get, CreateFakeUri()) { Content = content })
                using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult<HttpResponseMessage>(new HttpResponseMessage()))))
                {
                    await client.SendAsync(TestAsync, request);
                    await content.ReadAsStringAsync(); // no exception
                }
            }
        }
    }

    public sealed class HttpClientSendTest_Async : HttpClientTest.HttpClientSendTest
    {
        public HttpClientSendTest_Async(ITestOutputHelper output) : base(output) { }
    }

    public sealed class HttpClientSendTest_Sync : HttpClientTest.HttpClientSendTest
    {
        public HttpClientSendTest_Sync(ITestOutputHelper output) : base(output) { }
        protected override bool TestAsync => false;
    }
}
