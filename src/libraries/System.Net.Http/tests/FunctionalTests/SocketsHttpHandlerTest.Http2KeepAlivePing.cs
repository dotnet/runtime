// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(SocketsHttpHandler_Http2KeepAlivePing_Test), nameof(IsSupported))]
    public sealed class SocketsHttpHandler_Http2KeepAlivePing_Test : HttpClientHandlerTestBase
    {
        public static readonly bool IsSupported = PlatformDetection.SupportsAlpn && PlatformDetection.IsNotBrowser;

        protected override Version UseVersion => HttpVersion20.Value;

        private int _pingCounter;
        private Http2LoopbackConnection _connection;
        private SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1);
        private Channel<Frame> _framesChannel = Channel.CreateUnbounded<Frame>();
        private CancellationTokenSource _incomingFramesCts = new CancellationTokenSource();
        private Task _incomingFramesTask;
        private TaskCompletionSource _serverFinished = new TaskCompletionSource();
        private bool _sendPingResponse = true;

        private static Http2Options NoAutoPingResponseHttp2Options => new Http2Options() { EnableTransparentPingResponse = false };

        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

        public SocketsHttpHandler_Http2KeepAlivePing_Test(ITestOutputHelper output) : base(output)
        {
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAlivePingDelay_Infinite_NoKeepAlivePingIsSent()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(1);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;
                handler.KeepAlivePingDelay = Timeout.InfiniteTimeSpan;

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                await client.GetStringAsync(uri);

                // Actual request:
                await client.GetStringAsync(uri);

                // Let connection live until server finishes:
                await _serverFinished.Task.WaitAsync(TestTimeout);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter
                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may have received one RTT PING in response to HEADERS, but should receive no KeepAlive PING
                Assert.True(_pingCounter <= 1);
                Interlocked.Exchange(ref _pingCounter, 0); // reset the counter

                // Finish the response:
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may have received one RTT PING in response to HEADERS, but should receive no KeepAlive PING
                Assert.True(_pingCounter <= 1);

                await TerminateLoopbackConnectionAsync();
            });
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAliveConfigured_KeepAlivePingsAreSentEvenWith0ConnectionLifetime()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(10);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);
                handler.PooledConnectionLifetime = TimeSpan.Zero;

                // The connection will get recycled with lifetime == 0 after the first request.
                // So use a long response to wait for the PINGs.
                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.Timeout = TestHelper.PassingTestTimeout;

                using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Slowly read from the response stream.
                var responseStream = response.Content.ReadAsStream();
                var buffer = new byte[64];
                int bytes;
                while ((bytes = await responseStream.ReadAsync(buffer)) > 0) ;
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                int streamId = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendResponseHeadersAsync(streamId, endStream: false));

                for (int i = 0; i <= 2; ++i)
                {
                    // Wait for the PINGs to be sent.
                    await Task.Delay(2_500);

                    // Send some data.
                    await GuardConnectionWriteAsync(() => _connection.SendResponseBodyAsync(streamId, new byte[64], i == 2));
                }

                // Check received PINGs via the counter.
                int pingCounter = Interlocked.Exchange(ref _pingCounter, 0);
                Assert.True(pingCounter > 0);

                await TerminateLoopbackConnectionAsync();

                List<Frame> unexpectedFrames = new List<Frame>();
                while (_framesChannel.Reader.Count > 0)
                {
                    Frame unexpectedFrame = await _framesChannel.Reader.ReadAsync();
                    unexpectedFrames.Add(unexpectedFrame);
                }

                Assert.False(unexpectedFrames.Any(), "Received unexpected frames: \n" + string.Join('\n', unexpectedFrames.Select(f => f.ToString()).ToArray()));
            }, NoAutoPingResponseHttp2Options);
        }

        [OuterLoop("Runs long")]
        [Theory]
        [InlineData(HttpKeepAlivePingPolicy.Always)]
        [InlineData(HttpKeepAlivePingPolicy.WithActiveRequests)]
        public async Task KeepAliveConfigured_KeepAlivePingsAreSentAccordingToPolicy(HttpKeepAlivePingPolicy policy)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(10);
                handler.KeepAlivePingPolicy = policy;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Actual request:
                HttpResponseMessage response1 = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

                // Let connection live until server finishes:
                await _serverFinished.Task.WaitAsync(TestTimeout);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();
                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may receive one RTT PING in response to HEADERS.
                // Upon that, we expect to receive at least 1 keep alive PING:
                Assert.True(_pingCounter > 1);

                // Finish the response:
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));
                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter

                if (policy == HttpKeepAlivePingPolicy.Always)
                {
                    // Simulate inactive period:
                    await Task.Delay(5_000);

                    // We may receive one RTT PING in response to HEADERS.
                    // Upon that, we expect to receive at least 1 keep alive PING:
                    Assert.True(_pingCounter > 1);
                }
                else
                {
                    // We should receive no more KeepAlive PINGs
                    Assert.True(_pingCounter <= 1);
                }

                await TerminateLoopbackConnectionAsync();

                List<Frame> unexpectedFrames = new List<Frame>();
                while (_framesChannel.Reader.Count > 0)
                {
                    Frame unexpectedFrame = await _framesChannel.Reader.ReadAsync();
                    unexpectedFrames.Add(unexpectedFrame);
                }

                Assert.False(unexpectedFrames.Any(), "Received unexpected frames: \n" + string.Join('\n', unexpectedFrames.Select(f => f.ToString()).ToArray()));
            }, NoAutoPingResponseHttp2Options);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAliveConfigured_NoPingResponseDuringActiveStream_RequestShouldFail()
        {
            _sendPingResponse = false;

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(1.5);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Actual request:
                Exception ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));

                // The request should fail due to the connection being torn down due to KeepAlivePingTimeout.
                HttpProtocolException pex = Assert.IsType<HttpProtocolException>(ex.InnerException);
                Assert.Equal(HttpRequestError.HttpProtocolError, pex.HttpRequestError);
                Assert.Contains("KeepAlivePingDelay", pex.Message);

                // Let connection live until server finishes:
                await _serverFinished.Task;
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                // Wait for the client to disconnect due to hitting the KeepAliveTimeout
                await _incomingFramesTask;

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task HttpKeepAlivePingPolicy_Always_NoPingResponseBetweenStreams_SecondRequestShouldFail()
        {
            _sendPingResponse = false;

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(1.5);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Simulate inactive period by waiting until server finishes:
                await _serverFinished.Task;
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                // Wait for the client to disconnect due to hitting the KeepAliveTimeout
                await _incomingFramesTask;

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAliveConfigured_ConnectionShutDownByGoAway_KeepAlivePingsAreStillSent()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(10);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.Timeout = TestHelper.PassingTestTimeout;

                using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                await response.Content.ReadAsStream().CopyToAsync(Stream.Null);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                int streamId = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendResponseHeadersAsync(streamId, endStream: false));

                // Tell the client that the connection is going away, but that this stream will still be processed.
                // This removes the connection from the pool, but it must keep sending PINGs while the request is active.
                await GuardConnectionWriteAsync(() => _connection.SendGoAway(streamId));

                await WaitForKeepAlivePingsAndFinishResponseAsync(streamId);

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAliveConfigured_NoPingResponseAfterGoAway_RequestShouldFail()
        {
            // This is the user-visible symptom of a connection no longer being pinged after a GOAWAY frame:
            // a long-lived request on a connection whose transport silently died would hang forever.
            _sendPingResponse = false;

            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(1.5);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.Timeout = TestHelper.PassingTestTimeout;

                using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // The request must be torn down once the server stops responding to the keep alive PINGs.
                HttpProtocolException ex = await Assert.ThrowsAsync<HttpProtocolException>(() => response.Content.ReadAsStream().CopyToAsync(Stream.Null));
                Assert.Equal(HttpRequestError.HttpProtocolError, ex.HttpRequestError);
                Assert.Contains("KeepAlivePingDelay", ex.Message);

                await _serverFinished.Task.WaitAsync(TestTimeout);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                int streamId = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendResponseHeadersAsync(streamId, endStream: false));

                await GuardConnectionWriteAsync(() => _connection.SendGoAway(streamId));

                // Wait for the client to disconnect due to hitting the KeepAliveTimeout.
                await _incomingFramesTask;

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAliveConfigured_ConnectionAtMaxConcurrentStreams_KeepAlivePingsAreStillSent()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(10);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.Timeout = TestHelper.PassingTestTimeout;

                // Warmup request, ensuring that the connection picked up the server's MaxConcurrentStreams setting.
                using HttpResponseMessage warmupResponse = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, warmupResponse.StatusCode);

                // This request occupies the connection's only stream.
                using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // A second request can't be served until the first one completes. Attempting it removes the
                // connection from the pool, but it must keep sending PINGs while the first request is active.
                Task<HttpResponseMessage> queuedRequest = client.GetAsync(uri);

                await response.Content.ReadAsStream().CopyToAsync(Stream.Null);

                using HttpResponseMessage queuedResponse = await queuedRequest;
                Assert.Equal(HttpStatusCode.OK, queuedResponse.StatusCode);
            },
            async server =>
            {
                await EstablishConnectionAsync(server, new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 1 });

                int warmupStreamId = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(warmupStreamId));

                int streamId = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendResponseHeadersAsync(streamId, endStream: false));

                await WaitForKeepAlivePingsAndFinishResponseAsync(streamId);

                // The queued request is only sent out after the previous one completed.
                int queuedStreamId = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendDefaultResponseAsync(queuedStreamId));

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAliveConfigured_HandlerDisposedWithActiveRequest_KeepAlivePingsAreStillSent()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = CreateSocketsHttpHandler(allowAllCertificates: true);
                handler.KeepAlivePingTimeout = TimeSpan.FromSeconds(10);
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests;
                handler.KeepAlivePingDelay = TimeSpan.FromSeconds(1);

                using HttpClient client = new HttpClient(handler, disposeHandler: false);
                client.DefaultRequestVersion = HttpVersion.Version20;
                client.Timeout = TestHelper.PassingTestTimeout;

                using HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Disposing the handler doesn't affect requests that are already in flight,
                // so they must keep sending keep alive PINGs.
                handler.Dispose();

                await response.Content.ReadAsStream().CopyToAsync(Stream.Null);
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                int streamId = await ReadRequestHeaderAsync();
                await GuardConnectionWriteAsync(() => _connection.SendResponseHeadersAsync(streamId, endStream: false));

                await WaitForKeepAlivePingsAndFinishResponseAsync(streamId);

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options);
        }

        /// <summary>Waits for keep alive PINGs to arrive while the request is active, then completes the response.</summary>
        private async Task WaitForKeepAlivePingsAndFinishResponseAsync(int streamId)
        {
            Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter

            Stopwatch stopwatch = Stopwatch.StartNew();
            bool receivedPings = false;

            // Don't send anything on the stream while waiting -- every frame the client receives
            // resets its ping timer, so the connection must stay quiet for the PINGs to be sent.
            while (stopwatch.Elapsed < TestHelper.PassingTestTimeout)
            {
                if (Volatile.Read(ref _pingCounter) > 0)
                {
                    receivedPings = true;
                    break;
                }

                await Task.Delay(100);
            }

            Assert.True(receivedPings, "Timed out waiting for keep alive PINGs.");

            // Finish the response.
            await GuardConnectionWriteAsync(() => _connection.SendResponseBodyAsync(streamId, new byte[64], isFinal: true));
        }

        private async Task ProcessIncomingFramesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Frame frame = await _connection.ReadFrameAsync(cancellationToken);

                    if (frame is null)
                    {
                        break;
                    }

                    if (frame is PingFrame pingFrame)
                    {
                        if (pingFrame.AckFlag)
                        {
                            _output?.WriteLine($"Received unexpected PING ACK ({pingFrame.Data})");
                            await _framesChannel.Writer.WriteAsync(frame, cancellationToken);
                        }
                        else
                        {
                            _output?.WriteLine($"Received PING ({pingFrame.Data})");
                            if (pingFrame.Data >= 0)
                            {
                                // Increment only for real PINGs, not for RTT PINGs.
                                Interlocked.Increment(ref _pingCounter);
                            }

                            if (_sendPingResponse)
                            {
                                await GuardConnectionWriteAsync(() => _connection.SendPingAckAsync(pingFrame.Data, cancellationToken), cancellationToken);
                            }
                        }
                    }
                    else if (frame is WindowUpdateFrame windowUpdateFrame)
                    {
                        _output?.WriteLine($"Received WINDOW_UPDATE");
                    }
                    else
                    {
                        //_output?.WriteLine($"Received {frame}");
                        await _framesChannel.Writer.WriteAsync(frame, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            _output?.WriteLine("ProcessIncomingFramesAsync finished");
            await _connection.DisposeAsync();
        }

        private async Task EstablishConnectionAsync(Http2LoopbackServer server, params SettingsEntry[] settingsEntries)
        {
            _connection = await server.EstablishConnectionAsync(settingsEntries);
            _incomingFramesTask = ProcessIncomingFramesAsync(_incomingFramesCts.Token);
        }

        private async Task TerminateLoopbackConnectionAsync()
        {
            _serverFinished.SetResult();
            _incomingFramesCts.Cancel();
            await _incomingFramesTask;
        }

        private async Task GuardConnectionWriteAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            await _writeSemaphore.WaitAsync(cancellationToken);
            await action();
            _writeSemaphore.Release();
        }

        private async Task<HeadersFrame> ReadRequestHeaderFrameAsync(bool expectEndOfStream = true, CancellationToken cancellationToken = default)
        {
            // Receive HEADERS frame for request.
            Frame frame = await _framesChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (frame == null)
            {
                throw new IOException("Failed to read Headers frame.");
            }

            Assert.Equal(FrameType.Headers, frame.Type);
            Assert.Equal(FrameFlags.EndHeaders, frame.Flags & FrameFlags.EndHeaders);
            if (expectEndOfStream)
            {
                Assert.Equal(FrameFlags.EndStream, frame.Flags & FrameFlags.EndStream);
            }
            return (HeadersFrame)frame;
        }

        private async Task<int> ReadRequestHeaderAsync(bool expectEndOfStream = true, CancellationToken cancellationToken = default)
        {
            HeadersFrame frame = await ReadRequestHeaderFrameAsync(expectEndOfStream, cancellationToken);
            return frame.StreamId;
        }
    }
}
