// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Test.Common;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_Http3 : HttpClientHandlerTestBase
    {
        protected override Version UseVersion => HttpVersion.Version30;

        public HttpClientHandlerTest_Http3(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(10)] // 2 bytes settings value.
        [InlineData(100)] // 4 bytes settings value.
        [InlineData(10_000_000)] // 8 bytes settings value.
        public async Task ClientSettingsReceived_Success(int headerSizeLimit)
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();

                (Http3LoopbackStream settingsStream, Http3LoopbackStream requestStream) = await connection.AcceptControlAndRequestStreamAsync();

                using (settingsStream)
                using (requestStream)
                {
                    Assert.False(settingsStream.CanWrite, "Expected unidirectional control stream.");

                    long? streamType = await settingsStream.ReadIntegerAsync();
                    Assert.Equal(Http3LoopbackStream.ControlStream, streamType);

                    List<(long settingId, long settingValue)> settings = await settingsStream.ReadSettingsAsync();
                    (long settingId, long settingValue) = Assert.Single(settings);

                    Assert.Equal(Http3LoopbackStream.MaxHeaderListSize, settingId);
                    Assert.Equal(headerSizeLimit * 1024L, settingValue);

                    await requestStream.ReadRequestDataAsync();
                    await requestStream.SendResponseAsync();
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();
                handler.MaxResponseHeadersLength = headerSizeLimit;

                using HttpClient client = CreateHttpClient(handler);
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };
                using HttpResponseMessage response = await client.SendAsync(request);
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55957")]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task SendMoreThanStreamLimitRequests_Succeeds(int streamLimit)
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer(new Http3Options(){ MaxBidirectionalStreams = streamLimit });

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                for (int i = 0; i < streamLimit + 1; ++i)
                {
                    using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                    await stream.HandleRequestAsync();
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                for (int i = 0; i < streamLimit + 1; ++i)
                {
                    HttpRequestMessage request = new()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = server.Address,
                        Version = HttpVersion30,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    };
                    using var response = await client.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(10));
                }
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56000")]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task SendStreamLimitRequestsConcurrently_Succeeds(int streamLimit)
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer(new Http3Options(){ MaxBidirectionalStreams = streamLimit });

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                for (int i = 0; i < streamLimit; ++i)
                {
                    using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                    await stream.HandleRequestAsync();
                }
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                var tasks = new Task<HttpResponseMessage>[streamLimit];
                Parallel.For(0, streamLimit, i =>
                {
                    HttpRequestMessage request = new()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = server.Address,
                        Version = HttpVersion30,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    };

                    tasks[i] = client.SendAsync(request);
                });

                var responses = await Task.WhenAll(tasks);
                foreach (var response in responses)
                {
                    response.Dispose();
                }
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55901")]
        public async Task SendMoreThanStreamLimitRequestsConcurrently_LastWaits(int streamLimit)
        {
            // This combination leads to a hang manifesting in CI only. Disabling it until there's more time to investigate.
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/53688")]
            if (this.UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            using Http3LoopbackServer server = CreateHttp3LoopbackServer(new Http3Options(){ MaxBidirectionalStreams = streamLimit });
            var lastRequestContentStarted = new TaskCompletionSource();

            Task serverTask = Task.Run(async () =>
            {
                // Read the first streamLimit requests, keep the streams open to make the last one wait.
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                var streams = new Http3LoopbackStream[streamLimit];
                for (int i = 0; i < streamLimit; ++i)
                {
                    Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                    var body = await stream.ReadRequestDataAsync();
                    streams[i] = stream;
                }

                // Make the last request running independently.
                var lastRequest = Task.Run(async () => {
                    using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                    await stream.HandleRequestAsync();
                });

                // All the initial streamLimit streams are still opened so the last request cannot started yet.
                Assert.False(lastRequestContentStarted.Task.IsCompleted);

                // Reply to the first streamLimit requests.
                for (int i = 0; i < streamLimit; ++i)
                {
                    await streams[i].SendResponseAsync();
                    streams[i].Dispose();
                    // After the first request is fully processed, the last request should unblock and get processed.
                    if (i == 0)
                    {
                        await lastRequestContentStarted.Task;
                    }
                }
                await lastRequest;
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                // Fire out the first streamLimit requests in parallel, no waiting for the responses yet.
                var countdown = new CountdownEvent(streamLimit);
                var tasks = new Task<HttpResponseMessage>[streamLimit];
                Parallel.For(0, streamLimit, i =>
                {
                    HttpRequestMessage request = new()
                    {
                        Method = HttpMethod.Post,
                        RequestUri = server.Address,
                        Version = HttpVersion30,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                        Content = new StreamContent(new DelegateStream(
                            canReadFunc: () => true,
                            readFunc: (buffer, offset, count) =>
                            {
                                countdown.Signal();
                                return 0;
                            }))
                    };

                    tasks[i] = client.SendAsync(request);
                });

                // Wait for the first streamLimit request to get started.
                countdown.Wait();

                // Fire out the last request, that should wait until the server fully handles at least one request.
                HttpRequestMessage last = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = new StreamContent(new DelegateStream(
                        canReadFunc: () => true,
                        readFunc: (buffer, offset, count) =>
                        {
                            lastRequestContentStarted.SetResult();
                            return 0;
                        }))
                };
                var lastTask = client.SendAsync(last);

                // Wait for all requests to finish. Whether the last request was pending is checked on the server side.
                var responses = await Task.WhenAll(tasks);
                foreach (var response in responses)
                {
                    response.Dispose();
                }
                await lastTask;
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Fact]
        public async Task ReservedFrameType_Throws()
        {
            const int ReservedHttp2PriorityFrameId = 0x2;
            const long UnexpectedFrameErrorCode = 0x105;

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();

                await stream.SendFrameAsync(ReservedHttp2PriorityFrameId, new byte[8]);

                QuicConnectionAbortedException ex = await Assert.ThrowsAsync<QuicConnectionAbortedException>(async () =>
                {
                    await stream.HandleRequestAsync();
                    using Http3LoopbackStream stream2 = await connection.AcceptRequestStreamAsync();
                });

                Assert.Equal(UnexpectedFrameErrorCode, ex.ErrorCode);
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(request));
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Fact]
        public async Task RequestSentResponseDisposed_ThrowsOnServer()
        {
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1024));

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                HttpRequestData request = await stream.ReadRequestDataAsync();
                await stream.SendResponseHeadersAsync();

                Stopwatch sw = Stopwatch.StartNew();
                bool hasFailed = false;
                while (sw.Elapsed < TimeSpan.FromSeconds(15))
                {
                    try
                    {
                        await stream.SendResponseBodyAsync(data, isFinal: false);
                    }
                    catch (QuicStreamAbortedException)
                    {
                        hasFailed = true;
                        break;
                    }
                }
                Assert.True(hasFailed, $"Expected {nameof(QuicStreamAbortedException)}, instead ran successfully for {sw.Elapsed}");
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                var stream = await response.Content.ReadAsStreamAsync();
                byte[] buffer = new byte[512];
                for (int i = 0; i < 5; ++i)
                {
                    var count = await stream.ReadAsync(buffer);
                }

                // We haven't finished reading the whole respose, but we're disposing it, which should turn into an exception on the server-side.
                response.Dispose();
                await serverTask;
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Fact]
        public async Task RequestSendingResponseDisposed_ThrowsOnServer()
        {
            byte[] data = Encoding.UTF8.GetBytes(new string('a', 1024));

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                HttpRequestData request = await stream.ReadRequestDataAsync(false);
                await stream.SendResponseHeadersAsync();

                Stopwatch sw = Stopwatch.StartNew();
                bool hasFailed = false;
                while (sw.Elapsed < TimeSpan.FromSeconds(15))
                {
                    try
                    {
                        var (frameType, payload) = await stream.ReadFrameAsync();
                        Assert.Equal(Http3LoopbackStream.DataFrame, frameType);
                    }
                    catch (QuicStreamAbortedException)
                    {
                        hasFailed = true;
                        break;
                    }
                }
                Assert.True(hasFailed, $"Expected {nameof(QuicStreamAbortedException)}, instead ran successfully for {sw.Elapsed}");
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = new ByteAtATimeContent(60*4, Task.CompletedTask, new TaskCompletionSource<bool>(), 250)
                };

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                var stream = await response.Content.ReadAsStreamAsync();

                // We haven't finished sending the whole request, but we're disposing the response, which should turn into an exception on the server-side.
                response.Dispose();
                await serverTask;
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Fact]
        public async Task ServerCertificateCustomValidationCallback_Succeeds()
        {
            // Mock doesn't make use of cart validation callback.
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            HttpRequestMessage? callbackRequest = null;
            int invocationCount = 0;

            var httpClientHandler = CreateHttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (request, _, _, _) =>
            {
                callbackRequest = request;
                ++invocationCount;
                return true;
            };

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();
            using HttpClient client = CreateHttpClient(httpClientHandler);

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                await stream.HandleRequestAsync();
                using Http3LoopbackStream stream2 = await connection.AcceptRequestStreamAsync();
                await stream2.HandleRequestAsync();
            });

            var request = new HttpRequestMessage(HttpMethod.Get, server.Address);
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version30, response.Version);
            Assert.Same(request, callbackRequest);
            Assert.Equal(1, invocationCount);

            // Second request, the callback shouldn't be hit at all.
            callbackRequest = null;

            request = new HttpRequestMessage(HttpMethod.Get, server.Address);
            request.Version = HttpVersion.Version30;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version30, response.Version);
            Assert.Null(callbackRequest);
            Assert.Equal(1, invocationCount);

            await serverTask;
        }

        [Fact]
        public async Task EmptyCustomContent_FlushHeaders()
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer();
            TaskCompletionSource headersReceived = new TaskCompletionSource();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();

                // Receive headers and unblock the client.
                await stream.ReadRequestDataAsync(false);
                headersReceived.SetResult();

                await stream.ReadRequestBodyAsync();
                await stream.SendResponseAsync();
            });

            Task clientTask = Task.Run(async () =>
            {
                StreamingHttpContent requestContent = new StreamingHttpContent();

                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = requestContent
                };

                Task<HttpResponseMessage> responseTask = client.SendAsync(request);

                Stream requestStream = await requestContent.GetStreamAsync();
                await requestStream.FlushAsync();

                await headersReceived.Task;

                requestContent.CompleteStream();

                using HttpResponseMessage response = await responseTask;

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Fact]
        public async Task DisposeHttpClient_Http3ConnectionIsClosed()
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                HttpRequestData request = await connection.ReadRequestDataAsync();
                await connection.SendResponseAsync();

                await connection.WaitForClientDisconnectAsync(refuseNewRequests: false);
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                using HttpResponseMessage response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Return and let the HttpClient be disposed
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsMsQuicSupported))]
        [MemberData(nameof(InteropUris))]
        public async Task Public_Interop_ExactVersion_Success(string uri)
        {
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            using HttpResponseMessage response = await client.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(20));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, response.Version.Major);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsMsQuicSupported))]
        [MemberData(nameof(InteropUrisWithContent))]
        public async Task Public_Interop_ExactVersion_BufferContent_Success(string uri)
        {
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead).WaitAsync(TimeSpan.FromSeconds(20));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(3, response.Version.Major);

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
        }

        [OuterLoop]
        [ConditionalTheory(nameof(IsMsQuicSupported))]
        [MemberData(nameof(InteropUris))]
        public async Task Public_Interop_Upgrade_Success(string uri)
        {
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            // Create the handler manually without passing in useVersion = Http3 to avoid using VersionHttpClientHandler,
            // because it overrides VersionPolicy on each request with RequestVersionExact (bypassing Alt-Svc code path completely).
            using HttpClient client = CreateHttpClient(CreateHttpClientHandler(quicImplementationProvider: UseQuicImplementationProvider));

            // First request uses HTTP/1 or HTTP/2 and receives an Alt-Svc either by header or (with HTTP/2) by frame.

            using (HttpRequestMessage requestA = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            })
            {
                using HttpResponseMessage responseA = await client.SendAsync(requestA).WaitAsync(TimeSpan.FromSeconds(20));
                Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
                Assert.NotEqual(3, responseA.Version.Major);
            }

            // Second request uses HTTP/3.

            using (HttpRequestMessage requestB = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri, UriKind.Absolute),
                Version = HttpVersion.Version30,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            })
            {
                using HttpResponseMessage responseB = await client.SendAsync(requestB).WaitAsync(TimeSpan.FromSeconds(20));

                Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
                Assert.Equal(3, responseB.Version.Major);
            }
        }

        public enum CancellationType
        {
            Dispose,
            CancellationToken
        }

        [ConditionalTheory(nameof(IsMsQuicSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56194")]
        [InlineData(CancellationType.Dispose)]
        [InlineData(CancellationType.CancellationToken)]
        public async Task ResponseCancellation_ServerReceivesCancellation(CancellationType type)
        {
            if (UseQuicImplementationProvider != QuicImplementationProviders.MsQuic)
            {
                return;
            }

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            using var clientDone = new SemaphoreSlim(0);
            using var serverDone = new SemaphoreSlim(0);

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();

                HttpRequestData request = await stream.ReadRequestDataAsync().ConfigureAwait(false);

                int contentLength = 2*1024*1024;
                var headers = new List<HttpHeaderData>();
                headers.Append(new HttpHeaderData("Content-Length", contentLength.ToString(CultureInfo.InvariantCulture)));

                await stream.SendResponseHeadersAsync(HttpStatusCode.OK, headers).ConfigureAwait(false);
                await stream.SendDataFrameAsync(new byte[1024]).ConfigureAwait(false);

                await clientDone.WaitAsync();

                // It is possible that PEER_RECEIVE_ABORTED event will arrive with a significant delay after peer calls AbortReceive
                // In that case even with synchronization via semaphores, first writes after peer aborting may "succeed" (get SEND_COMPLETE event)
                // We are asserting that PEER_RECEIVE_ABORTED would still arrive eventually

                var ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => SendDataForever(stream).WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal((type == CancellationType.CancellationToken ? 268 : 0xffffffff), ex.ErrorCode);

                serverDone.Release();
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                using HttpRequestMessage request = new()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = server.Address,
                        Version = HttpVersion30,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    };
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).WaitAsync(TimeSpan.FromSeconds(10));

                Stream stream = await response.Content.ReadAsStreamAsync();

                int bytesRead = await stream.ReadAsync(new byte[1024]);
                Assert.Equal(1024, bytesRead);

                var cts = new CancellationTokenSource(200);

                if (type == CancellationType.Dispose)
                {
                    cts.Token.Register(() => response.Dispose());
                }
                CancellationToken readCt = type == CancellationType.CancellationToken ? cts.Token : default;

                Exception ex = await Assert.ThrowsAnyAsync<Exception>(() => stream.ReadAsync(new byte[1024], cancellationToken: readCt).AsTask());

                if (type == CancellationType.CancellationToken)
                {
                    Assert.IsType<OperationCanceledException>(ex);
                }
                else
                {
                    var ioe = Assert.IsType<IOException>(ex);
                    var hre = Assert.IsType<HttpRequestException>(ioe.InnerException);
                    Assert.IsType<QuicOperationAbortedException>(hre.InnerException);
                }

                clientDone.Release();
                await serverDone.WaitAsync();
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(20_000);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56265")]
        public async Task ResponseCancellation_BothCancellationTokenAndDispose_Success()
        {
            if (UseQuicImplementationProvider != QuicImplementationProviders.MsQuic)
            {
                return;
            }

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            using var clientDone = new SemaphoreSlim(0);
            using var serverDone = new SemaphoreSlim(0);

            Task serverTask = Task.Run(async () =>
            {
                using Http3LoopbackConnection connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();

                HttpRequestData request = await stream.ReadRequestDataAsync().ConfigureAwait(false);

                int contentLength = 2*1024*1024;
                var headers = new List<HttpHeaderData>();
                headers.Append(new HttpHeaderData("Content-Length", contentLength.ToString(CultureInfo.InvariantCulture)));

                await stream.SendResponseHeadersAsync(HttpStatusCode.OK, headers).ConfigureAwait(false);
                await stream.SendDataFrameAsync(new byte[1024]).ConfigureAwait(false);

                await clientDone.WaitAsync();

                // It is possible that PEER_RECEIVE_ABORTED event will arrive with a significant delay after peer calls AbortReceive
                // In that case even with synchronization via semaphores, first writes after peer aborting may "succeed" (get SEND_COMPLETE event)
                // We are asserting that PEER_RECEIVE_ABORTED would still arrive eventually

                var ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => SendDataForever(stream).WaitAsync(TimeSpan.FromSeconds(20)));
                // exact error code depends on who won the race
                Assert.True(ex.ErrorCode == 268 /* cancellation */ || ex.ErrorCode == 0xffffffff /* disposal */, $"Expected 268 or 0xffffffff, got {ex.ErrorCode}");

                serverDone.Release();
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                using HttpRequestMessage request = new()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = server.Address,
                        Version = HttpVersion30,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    };
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).WaitAsync(TimeSpan.FromSeconds(20));

                Stream stream = await response.Content.ReadAsStreamAsync();

                int bytesRead = await stream.ReadAsync(new byte[1024]);
                Assert.Equal(1024, bytesRead);

                var cts = new CancellationTokenSource(200);
                cts.Token.Register(() => response.Dispose());

                Exception ex = await Assert.ThrowsAnyAsync<Exception>(() => stream.ReadAsync(new byte[1024], cancellationToken: cts.Token).AsTask());

                // exact exception depends on who won the race
                if (ex is not OperationCanceledException)
                {
                    var ioe = Assert.IsType<IOException>(ex);
                    var hre = Assert.IsType<HttpRequestException>(ioe.InnerException);
                    Assert.IsType<QuicOperationAbortedException>(hre.InnerException);
                }

                clientDone.Release();
                await serverDone.WaitAsync();
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(200_000);
        }

        private static async Task SendDataForever(Http3LoopbackStream stream)
        {
            var buf = new byte[100];
            while (true)
            {
                await stream.SendDataFrameAsync(buf);
            }
        }

        [ConditionalFact(nameof(IsMsQuicSupported))]
        public async Task Alpn_H3_Success()
        {
            // Mock doesn't use ALPN.
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            var options = new Http3Options() { Alpn = SslApplicationProtocol.Http3.ToString() };
            using Http3LoopbackServer server = CreateHttp3LoopbackServer(options);

            Http3LoopbackConnection connection = null;
            Task serverTask = Task.Run(async () =>
            {
                connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();
                await stream.HandleRequestAsync();
            });

            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new()
            {
                Method = HttpMethod.Get,
                RequestUri = server.Address,
                Version = HttpVersion30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            HttpResponseMessage response = await client.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(10));
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version30, response.Version);

            await serverTask;
            Assert.NotNull(connection);

            SslApplicationProtocol negotiatedAlpn = ExtractMsQuicNegotiatedAlpn(connection);
            Assert.Equal(new SslApplicationProtocol("h3"), negotiatedAlpn);
            connection.Dispose();
        }

        [ConditionalFact(nameof(IsMsQuicSupported))]
        public async Task Alpn_NonH3_NegotiationFailure()
        {
            // Mock doesn't use ALPN.
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            var options = new Http3Options() { Alpn = "h3-29" }; // anything other than "h3"
            using Http3LoopbackServer server = CreateHttp3LoopbackServer(options);

            using var clientDone = new SemaphoreSlim(0);

            Task serverTask = Task.Run(async () =>
            {
                // ALPN handshake handled by transport, app level will not get any notification
                await clientDone.WaitAsync();
            });

            Task clientTask = Task.Run(async () =>
            {
                using HttpClient client = CreateHttpClient();

                using HttpRequestMessage request = new()
                    {
                        Method = HttpMethod.Get,
                        RequestUri = server.Address,
                        Version = HttpVersion30,
                        VersionPolicy = HttpVersionPolicy.RequestVersionExact
                    };

                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Contains("ALPN_NEG_FAILURE", ex.Message);

                clientDone.Release();
            });

            await new[] { clientTask, serverTask }.WhenAllOrAnyFailed(200_000);
        }

        private SslApplicationProtocol ExtractMsQuicNegotiatedAlpn(Http3LoopbackConnection loopbackConnection)
        {
            // TODO: rewrite after object structure change
            // current structure:
            // Http3LoopbackConnection -> private QuicConnection _connection
            // QuicConnection -> private QuicConnectionProvider _provider (= MsQuicConnection)
            // MsQuicConnection -> private SslApplicationProtocol _negotiatedAlpnProtocol

            FieldInfo quicConnectionField = loopbackConnection.GetType().GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(quicConnectionField);
            object quicConnection = quicConnectionField.GetValue(loopbackConnection);
            Assert.NotNull(quicConnection);
            Assert.Equal("QuicConnection", quicConnection.GetType().Name);

            FieldInfo msQuicConnectionField = quicConnection.GetType().GetField("_provider", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(msQuicConnectionField);
            object msQuicConnection = msQuicConnectionField.GetValue(quicConnection);
            Assert.NotNull(msQuicConnection);
            Assert.Equal("MsQuicConnection", msQuicConnection.GetType().Name);

            FieldInfo alpnField = msQuicConnection.GetType().GetField("_negotiatedAlpnProtocol", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(alpnField);
            object alpn = alpnField.GetValue(msQuicConnection);
            Assert.NotNull(alpn);
            Assert.IsType<SslApplicationProtocol>(alpn);

            return (SslApplicationProtocol)alpn;
        }

        [ConditionalTheory(nameof(IsMsQuicSupported))]
        [MemberData(nameof(StatusCodesTestData))]
        public async Task StatusCodes_ReceiveSuccess(HttpStatusCode statusCode, bool qpackEncode)
        {
            using Http3LoopbackServer server = CreateHttp3LoopbackServer();

            Http3LoopbackConnection connection = null;
            Task serverTask = Task.Run(async () =>
            {
                connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                using Http3LoopbackStream stream = await connection.AcceptRequestStreamAsync();

                HttpRequestData request = await stream.ReadRequestDataAsync().ConfigureAwait(false);

                if (qpackEncode)
                {
                    await stream.SendResponseHeadersWithEncodedStatusAsync(statusCode).ConfigureAwait(false);
                }
                else
                {
                    await stream.SendResponseHeadersAsync(statusCode).ConfigureAwait(false);
                }
            });

            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new()
            {
                Method = HttpMethod.Get,
                RequestUri = server.Address,
                Version = HttpVersion30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
            HttpResponseMessage response = await client.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Equal(statusCode, response.StatusCode);

            await serverTask;
            Assert.NotNull(connection);
            connection.Dispose();
        }

        [Theory]
        [InlineData(1)] // frame fits into Http3RequestStream buffer
        [InlineData(10)]
        [InlineData(100)] // frame doesn't fit into Http3RequestStream buffer
        [InlineData(1000)]
        public async Task EchoServerStreaming_DifferentMessageSize_Success(int messageSize)
        {
            int iters = 5;
            var message = new byte[messageSize];
            var readBuffer = new byte[5 * messageSize]; // bigger than message
            var random = new Random(0);

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();
            Http3LoopbackConnection connection = null;
            Http3LoopbackStream serverStream = null;

            Task serverTask = Task.Run(async () =>
            {
                connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                serverStream = await connection.AcceptRequestStreamAsync();

                HttpRequestData requestData = await serverStream.ReadRequestDataAsync(readBody: false).WaitAsync(TimeSpan.FromSeconds(30));

                await serverStream.SendResponseHeadersAsync().ConfigureAwait(false);

                while (true)
                {
                    (long? frameType, byte[] payload) = await serverStream.ReadFrameAsync();
                    if (frameType == null)
                    {
                        // EOS
                        break;
                    }
                    // echo back
                    await serverStream.SendDataFrameAsync(payload).WaitAsync(TimeSpan.FromSeconds(30));
                }
                // send FIN
                await serverStream.SendResponseBodyAsync(Array.Empty<byte>(), isFinal: true);
            });

            StreamingHttpContent requestContent = new StreamingHttpContent();

            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = server.Address,
                Version = HttpVersion30,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                Content = requestContent
            };

            var responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).WaitAsync(TimeSpan.FromSeconds(10));

            Stream requestStream = await requestContent.GetStreamAsync().WaitAsync(TimeSpan.FromSeconds(10));;
            // Send headers
            await requestStream.FlushAsync();

            using HttpResponseMessage response = await responseTask;

            var responseStream = await response.Content.ReadAsStreamAsync();

            for (int i = 0; i < iters; ++i)
            {
                random.NextBytes(message);
                await requestStream.WriteAsync(message).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                await requestStream.FlushAsync();

                int bytesRead = await responseStream.ReadAsync(readBuffer).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(bytesRead, messageSize);
                Assert.Equal(message, readBuffer[..bytesRead]);
            }
            // Send FIN
            requestContent.CompleteStream();
            // Receive FIN
            Assert.Equal(0, await responseStream.ReadAsync(readBuffer).AsTask().WaitAsync(TimeSpan.FromSeconds(10)));

            await serverTask.WaitAsync(TimeSpan.FromSeconds(60));

            serverStream.Dispose();
            Assert.NotNull(connection);
            connection.Dispose();
        }

        [ConditionalFact(nameof(IsMsQuicSupported))]
        [OuterLoop("Uses Task.Delay")]
        public async Task RequestContentStreaming_Timeout_BothClientAndServerReceiveCancellation()
        {
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            var message = new byte[1024];
            var random = new Random(0);
            random.NextBytes(message);

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();
            Http3LoopbackConnection connection = null;
            Http3LoopbackStream serverStream = null;

            Task serverTask = Task.Run(async () =>
            {
                connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                serverStream = await connection.AcceptRequestStreamAsync();

                await serverStream.HandleRequestAsync();
            });

            Task clientTask = Task.Run(async () =>
            {
                StreamingHttpContent requestContent = new StreamingHttpContent();

                using HttpClient client = CreateHttpClient();
                client.Timeout = TimeSpan.FromSeconds(10); // set some timeout; big enough to send headers and first chunk of content
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = requestContent
                };

                var responseTask = client.SendAsync(request);

                Stream requestStream = await requestContent.GetStreamAsync().WaitAsync(TimeSpan.FromSeconds(10)); // the stream is Http3WriteStream
                // Send headers
                await requestStream.FlushAsync();

                await requestStream.WriteAsync(message);
                await requestStream.FlushAsync();

                await Task.Delay(TimeSpan.FromSeconds(11)); // longer than client.Timeout

                // Http3WriteStream is disposed after cancellation fired
                await Assert.ThrowsAsync<ObjectDisposedException>(() => requestStream.WriteAsync(message).AsTask());
                // client is properly canceled on timeout
                var tce = await Assert.ThrowsAsync<TaskCanceledException>(() => responseTask);
                Assert.IsType<TimeoutException>(tce.InnerException);
            });

            await clientTask.WaitAsync(TimeSpan.FromSeconds(120));

            // server receives cancellation
            var ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => serverTask.WaitAsync(TimeSpan.FromSeconds(120)));
            Assert.Equal(268 /*H3_REQUEST_CANCELLED (0x10C)*/, ex.ErrorCode);

            Assert.NotNull(serverStream);
            serverStream.Dispose();
            Assert.NotNull(connection);
            connection.Dispose();
        }

        [ConditionalFact(nameof(IsMsQuicSupported))]
        public async Task RequestContentStreaming_Cancellation_BothClientAndServerReceiveCancellation()
        {
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            var message = new byte[1024];
            var random = new Random(0);
            random.NextBytes(message);

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();
            Http3LoopbackConnection connection = null;
            Http3LoopbackStream serverStream = null;

            Task serverTask = Task.Run(async () =>
            {
                connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                serverStream = await connection.AcceptRequestStreamAsync();

                await serverStream.HandleRequestAsync();
            });

            Task clientTask = Task.Run(async () =>
            {
                StreamingHttpContent requestContent = new StreamingHttpContent();

                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = requestContent
                };

                var cts = new CancellationTokenSource();

                var responseTask = client.SendAsync(request, cts.Token);

                Stream requestStream = await requestContent.GetStreamAsync().WaitAsync(TimeSpan.FromSeconds(10)); // the stream is Http3WriteStream
                // Send headers
                await requestStream.FlushAsync();

                await requestStream.WriteAsync(message);
                await requestStream.FlushAsync();

                cts.Cancel();
                await Task.Delay(250);

                // Http3WriteStream is disposed after cancellation fired
                await Assert.ThrowsAsync<ObjectDisposedException>(() => requestStream.WriteAsync(message).AsTask());
                // client is properly canceled
                await Assert.ThrowsAsync<TaskCanceledException>(() => responseTask);
            });

            await clientTask.WaitAsync(TimeSpan.FromSeconds(120));

            // server receives cancellation
            var ex = await Assert.ThrowsAsync<QuicStreamAbortedException>(() => serverTask.WaitAsync(TimeSpan.FromSeconds(120)));
            Assert.Equal(268 /*H3_REQUEST_CANCELLED (0x10C)*/, ex.ErrorCode);

            Assert.NotNull(serverStream);
            serverStream.Dispose();
            Assert.NotNull(connection);
            connection.Dispose();
        }

        [ConditionalFact(nameof(IsMsQuicSupported))]
        public async Task DuplexStreaming_RequestCTCancellation_DoesNotApply()
        {
            if (UseQuicImplementationProvider == QuicImplementationProviders.Mock)
            {
                return;
            }

            var message = new byte[1024];
            var random = new Random(0);
            random.NextBytes(message);

            using Http3LoopbackServer server = CreateHttp3LoopbackServer();
            Http3LoopbackConnection connection = null;
            Http3LoopbackStream serverStream = null;

            Task serverTask = Task.Run(async () =>
            {
                connection = (Http3LoopbackConnection)await server.EstablishGenericConnectionAsync();
                serverStream = await connection.AcceptRequestStreamAsync();

                HttpRequestData requestData = await serverStream.ReadRequestDataAsync(readBody: false).WaitAsync(TimeSpan.FromSeconds(30));

                await serverStream.SendResponseHeadersAsync().ConfigureAwait(false);

                // read all the content after sending back the headers
                await serverStream.ReadRequestBodyAsync();

                // send FIN
                await serverStream.SendResponseBodyAsync(Array.Empty<byte>(), isFinal: true);
            });

            Task clientTask = Task.Run(async () =>
            {
                StreamingHttpContent requestContent = new StreamingHttpContent();

                using HttpClient client = CreateHttpClient();
                using HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = server.Address,
                    Version = HttpVersion30,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = requestContent
                };

                var cts = new CancellationTokenSource();

                var responseTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                Stream requestStream = await requestContent.GetStreamAsync().WaitAsync(TimeSpan.FromSeconds(10)); // the stream is Http3WriteStream
                // Send headers
                await requestStream.FlushAsync();

                using HttpResponseMessage response = await responseTask;

                // start streaming
                Stream responseStream = await response.Content.ReadAsStreamAsync();

                await requestStream.WriteAsync(message);
                await requestStream.FlushAsync();

                // cancelling after SendAsync finished should not apply -- nothing should happen
                cts.Cancel();
                await Task.Delay(250);

                // streaming successfully continues after CT fired
                await requestStream.WriteAsync(message);
                await requestStream.FlushAsync();

                // send FIN
                requestContent.CompleteStream();
                // Receive FIN
                Assert.Equal(0, await responseStream.ReadAsync(new byte[1]));
            });

            await clientTask.WaitAsync(TimeSpan.FromSeconds(120));

            await serverTask.WaitAsync(TimeSpan.FromSeconds(120));

            Assert.NotNull(serverStream);
            serverStream.Dispose();
            Assert.NotNull(connection);
            connection.Dispose();
        }

        public static TheoryData<HttpStatusCode, bool> StatusCodesTestData()
        {
            var statuses = Enum.GetValues(typeof(HttpStatusCode)).Cast<HttpStatusCode>().Where(s => s >= HttpStatusCode.OK); // exclude informational
            var data = new TheoryData<HttpStatusCode, bool>();
            foreach (var status in statuses)
            {
                data.Add(status, true);
                data.Add(status, false);
            }
            return data;
        }

        /// <summary>
        /// These are public interop test servers for various QUIC and HTTP/3 implementations,
        /// taken from https://github.com/quicwg/base-drafts/wiki/Implementations and https://bagder.github.io/HTTP3-test/.
        /// </summary>
        public static TheoryData<string> InteropUris() =>
            new TheoryData<string>
            {
                { "https://www.litespeedtech.com/" }, // LiteSpeed
                { "https://quic.tech:8443/" }, // Cloudflare
                { "https://quic.aiortc.org:443/" }, // aioquic
                { "https://h2o.examp1e.net/" } // h2o/quicly
            };

        /// <summary>
        /// These are public interop test servers for various QUIC and HTTP/3 implementations,
        /// taken from https://github.com/quicwg/base-drafts/wiki/Implementations and https://bagder.github.io/HTTP3-test/.
        /// </summary>
        public static TheoryData<string> InteropUrisWithContent() =>
            new TheoryData<string>
            {
                { "https://cloudflare-quic.com/" }, // Cloudflare with content
                // This endpoint is consistently failing.
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/63009")]
                //{ "https://pgjones.dev/" }, // aioquic with content
            };
    }

    internal class StreamingHttpContent : HttpContent
    {
        private readonly TaskCompletionSource _completeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<Stream> _getStreamTcs = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw new NotSupportedException();
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            _getStreamTcs.TrySetResult(stream);
            await _completeTcs.Task.WaitAsync(cancellationToken);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        public Task<Stream> GetStreamAsync()
        {
            return _getStreamTcs.Task;
        }

        public void CompleteStream()
        {
            _completeTcs.TrySetResult();
        }
    }
}
