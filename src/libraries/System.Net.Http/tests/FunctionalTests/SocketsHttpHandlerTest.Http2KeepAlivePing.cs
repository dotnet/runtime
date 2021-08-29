// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [Collection(nameof(NonParallelTestCollection))]
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
        private int _sendPingResponse = 1;

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
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(1),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    KeepAlivePingDelay = Timeout.InfiniteTimeSpan
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

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
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter
                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may have received one RTT PING in response to HEADERS, but should receive no KeepAlive PING
                Assert.True(_pingCounter <= 1);
                Interlocked.Exchange(ref _pingCounter, 0); // reset the counter

                // Finish the response:
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may have received one RTT PING in response to HEADERS, but should receive no KeepAlive PING
                Assert.True(_pingCounter <= 1);

                await TerminateLoopbackConnectionAsync();
            }).WaitAsync(TestTimeout);
        }

        [OuterLoop("Runs long")]
        [Theory]
        [InlineData(HttpKeepAlivePingPolicy.Always)]
        [InlineData(HttpKeepAlivePingPolicy.WithActiveRequests)]
        public async Task KeepAliveConfigured_KeepAlivePingsAreSentAccordingToPolicy(HttpKeepAlivePingPolicy policy)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                    KeepAlivePingPolicy = policy,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(1)
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

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
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();
                Interlocked.Exchange(ref _pingCounter, 0); // reset the PING counter

                // Simulate inactive period:
                await Task.Delay(5_000);

                // We may receive one RTT PING in response to HEADERS.
                // Upon that, we expect to receive at least 1 keep alive PING:
                Assert.True(_pingCounter > 1);

                // Finish the response:
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));
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
            }, NoAutoPingResponseHttp2Options).WaitAsync(TestTimeout);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task KeepAliveConfigured_NoPingResponseDuringActiveStream_RequestShouldFail()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(1.5),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(1)
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Actual request:
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));

                // Let connection live until server finishes:
                await _serverFinished.Task;
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();

                DisablePingResponse();

                // Simulate inactive period:
                await Task.Delay(6_000);

                // Finish the response:
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options).WaitAsync(TestTimeout);
        }

        [OuterLoop("Runs long")]
        [Fact]
        public async Task HttpKeepAlivePingPolicy_Always_NoPingResponseBetweenStreams_SecondRequestShouldFail()
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                SocketsHttpHandler handler = new SocketsHttpHandler()
                {
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(1.5),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(1)
                };
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };

                using HttpClient client = new HttpClient(handler);
                client.DefaultRequestVersion = HttpVersion.Version20;

                // Warmup request to create connection:
                HttpResponseMessage response0 = await client.GetAsync(uri);
                Assert.Equal(HttpStatusCode.OK, response0.StatusCode);

                // Second request should fail:
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(uri));

                // Let connection live until server finishes:
                await _serverFinished.Task;
            },
            async server =>
            {
                await EstablishConnectionAsync(server);

                // Warmup the connection.
                int streamId1 = await ReadRequestHeaderAsync();
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId1));

                DisablePingResponse();

                // Simulate inactive period:
                await Task.Delay(6_000);

                // Request under the test scope.
                int streamId2 = await ReadRequestHeaderAsync();

                // Finish the response:
                await GuardConnetionWriteAsync(() => _connection.SendDefaultResponseAsync(streamId2));

                await TerminateLoopbackConnectionAsync();
            }, NoAutoPingResponseHttp2Options).WaitAsync(TestTimeout);
        }

        private async Task ProcessIncomingFramesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Frame frame = await _connection.ReadFrameAsync(cancellationToken);

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
                            Interlocked.Increment(ref _pingCounter);

                            if (_sendPingResponse > 0)
                            {
                                await GuardConnetionWriteAsync(() => _connection.SendPingAckAsync(pingFrame.Data, cancellationToken), cancellationToken);
                            }
                        }
                    }
                    else if (frame is WindowUpdateFrame windowUpdateFrame)
                    {
                        _output?.WriteLine($"Received WINDOW_UPDATE");
                    }
                    else if (frame is not null)
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
            _connection.Dispose();
        }

        private void DisablePingResponse() => Interlocked.Exchange(ref _sendPingResponse, 0);

        private async Task EstablishConnectionAsync(Http2LoopbackServer server)
        {
            _connection = await server.EstablishConnectionAsync();
            _incomingFramesTask = ProcessIncomingFramesAsync(_incomingFramesCts.Token);
        }

        private async Task TerminateLoopbackConnectionAsync()
        {
            _serverFinished.SetResult();
            _incomingFramesCts.Cancel();
            await _incomingFramesTask;
        }

        private async Task GuardConnetionWriteAsync(Func<Task> action, CancellationToken cancellationToken = default)
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
