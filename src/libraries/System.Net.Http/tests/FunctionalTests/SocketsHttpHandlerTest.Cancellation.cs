// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public abstract class SocketsHttpHandler_Cancellation_Test : HttpClientHandler_Cancellation_Test
    {
        protected SocketsHttpHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        private async Task ValidateConnectTimeout(HttpMessageInvoker invoker, Uri uri, int minElapsed, int maxElapsed)
        {
            var sw = Stopwatch.StartNew();
            var oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                invoker.SendAsync(TestAsync, new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion }, default));
            sw.Stop();

            Assert.IsType<TimeoutException>(oce.InnerException);
            Assert.InRange(sw.ElapsedMilliseconds, minElapsed, maxElapsed);
        }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_TimesOutSSLAuth_Throws()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                return;
            }

            var releaseServer = new TaskCompletionSource();
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectTimeout = TimeSpan.FromSeconds(1);

                    await ValidateConnectTimeout(invoker, new UriBuilder(uri) { Scheme = "https" }.Uri, 500, 85_000);

                    releaseServer.SetResult();
                }
            }, server => releaseServer.Task); // doesn't establish SSL connection
        }

        [OuterLoop]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectTimeout_ConnectCallbackTimesOut_Throws(bool useSsl)
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectTimeout = TimeSpan.FromSeconds(1);
                    socketsHandler.ConnectCallback = async (context, token) => { await Task.Delay(-1, token); return null; };

                    await ValidateConnectTimeout(invoker, uri, 500, 85_000);
                }
            }, server => Task.CompletedTask, // doesn't actually connect to server
            options: new GenericLoopbackOptions() { UseSsl = useSsl });
        }

        [OuterLoop]
        [Fact]
        public async Task ConnectTimeout_PlaintextStreamFilterTimesOut_Throws()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support PlaintextStreamFilter
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectTimeout = TimeSpan.FromSeconds(1);
                    socketsHandler.ConnectCallback = (context, token) => new ValueTask<Stream>(new MemoryStream());
                    socketsHandler.PlaintextStreamFilter = async (context, token) => { await Task.Delay(-1, token); return null; };

                    await ValidateConnectTimeout(invoker, uri, 500, 85_000);
                }
            }, server => Task.CompletedTask, // doesn't actually connect to server
            options: new GenericLoopbackOptions() { UseSsl = false });
        }

        [OuterLoop]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69870", TestPlatforms.Android)]
        public async Task ConnectionFailure_AfterInitialRequestCancelled_SecondRequestSucceedsOnNewConnection(bool useSsl)
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            if (!TestAsync)
            {
                // Test relies on ordering of async operations, so we can't test the sync case
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                int connectCount = 0;

                TaskCompletionSource tcsFirstConnectionInitiated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource tcsFirstRequestCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                using (var handler = CreateHttpClientHandler())
                using (var client = CreateHttpClient(handler))
                {
                    handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    socketsHandler.ConnectCallback = async (context, token) =>
                    {
                        // Note we force serialization of connection creation by waiting on tcsFirstConnectionInitiated below,
                        // so we don't need to worry about concurrent access to connectCount.
                        bool isFirstConnection = connectCount == 0;
                        connectCount++;

                        Assert.True(connectCount <= 2);

                        if (isFirstConnection)
                        {
                            tcsFirstConnectionInitiated.SetResult();
                        }
                        else
                        {
                            Assert.True(tcsFirstConnectionInitiated.Task.IsCompletedSuccessfully);
                        }

                        // Wait until first request is cancelled and has completed
                        await tcsFirstRequestCanceled.Task;

                        if (isFirstConnection)
                        {
                            // Fail the first connection attempt
                            throw new Exception("Failing first connection");
                        }
                        else
                        {
                            // Succeed the second connection attempt
                            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                            await socket.ConnectAsync(context.DnsEndPoint, token);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                    };

                    using CancellationTokenSource cts = new CancellationTokenSource();
                    Task<HttpResponseMessage> t1 = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion, VersionPolicy = HttpVersionPolicy.RequestVersionExact }, cts.Token);

                    // Wait for the connection attempt to be initiated before we send the second request, to avoid races in connection creation
                    await tcsFirstConnectionInitiated.Task;
                    Task<HttpResponseMessage> t2 = client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Version = UseVersion, VersionPolicy = HttpVersionPolicy.RequestVersionExact }, default);

                    // Cancel the first message and wait for it to complete
                    cts.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t1);

                    // Signal connections to proceed
                    tcsFirstRequestCanceled.SetResult();

                    // Second request should succeed, even though the first connection failed
                    HttpResponseMessage resp2 = await t2;
                    Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
                    Assert.Equal("Hello world", await resp2.Content.ReadAsStringAsync());
                }
            }, async server =>
            {
                await server.AcceptConnectionSendResponseAndCloseAsync(content: "Hello world");
            },
            options: new GenericLoopbackOptions() { UseSsl = useSsl });
        }

        [Fact]
        public async Task RequestsCanceled_NoConnectionAttemptForCanceledRequests()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            bool seenRequest1 = false;
            bool seenRequest2 = false;
            bool seenRequest3 = false;

            var uri = new Uri("https://example.com");
            HttpRequestMessage request1 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
            HttpRequestMessage request2 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);
            HttpRequestMessage request3 = CreateRequest(HttpMethod.Get, uri, UseVersion, exactVersion: true);

            TaskCompletionSource connectCallbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource connectCallbackGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using HttpClientHandler handler = CreateHttpClientHandler();
            handler.MaxConnectionsPerServer = 1;
            GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = async (context, cancellation) =>
            {
                if (context.InitialRequestMessage == request1) seenRequest1 = true;
                if (context.InitialRequestMessage == request2) seenRequest2 = true;
                if (context.InitialRequestMessage == request3) seenRequest3 = true;

                connectCallbackEntered.TrySetResult();

                await connectCallbackGate.Task.WaitAsync(TestHelper.PassingTestTimeout);

                throw new Exception("No connection");
            };
            using HttpClient client = CreateHttpClient(handler);

            Task request1Task = client.SendAsync(TestAsync, request1);
            await connectCallbackEntered.Task.WaitAsync(TestHelper.PassingTestTimeout);
            Assert.True(seenRequest1);

            using var request2Cts = new CancellationTokenSource();
            Task request2Task = client.SendAsync(TestAsync, request2, request2Cts.Token);
            Assert.False(seenRequest2);

            Task request3Task = client.SendAsync(TestAsync, request3);
            Assert.False(seenRequest2);
            Assert.False(seenRequest3);

            request2Cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(() => request2Task).WaitAsync(TestHelper.PassingTestTimeout);
            Assert.False(seenRequest2);
            Assert.False(seenRequest3);

            connectCallbackGate.SetResult();

            await Assert.ThrowsAsync<HttpRequestException>(() => request1Task).WaitAsync(TestHelper.PassingTestTimeout);
            await Assert.ThrowsAsync<HttpRequestException>(() => request3Task).WaitAsync(TestHelper.PassingTestTimeout);

            Assert.False(seenRequest2);
            Assert.True(seenRequest3);
        }

        [OuterLoop("Incurs significant delay")]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69870", TestPlatforms.Android)]
        public async Task Expect100Continue_WaitsExpectedPeriodOfTimeBeforeSendingContent()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using (var handler = CreateHttpClientHandler())
                using (var invoker = new HttpMessageInvoker(handler))
                {
                    var socketsHandler = GetUnderlyingSocketsHttpHandler(handler);
                    TimeSpan delay = TimeSpan.FromSeconds(3);
                    socketsHandler.Expect100ContinueTimeout = delay;

                    var tcs = new TaskCompletionSource<bool>();
                    var content = new SetTcsContent(new MemoryStream(new byte[1]), tcs);
                    var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content, Version = UseVersion };
                    request.Headers.ExpectContinue = true;

                    long start = Environment.TickCount64;
                    (await invoker.SendAsync(TestAsync, request, default)).Dispose();
                    long elapsed = content.Ticks - start;
                    Assert.True(elapsed >= delay.TotalMilliseconds);
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.HandleRequestAsync();
                });
            });
        }

        private sealed class SetTcsContent : StreamContent
        {
            private readonly TaskCompletionSource<bool> _tcs;
            public long Ticks;

            public SetTcsContent(Stream stream, TaskCompletionSource<bool> tcs) : base(stream) => _tcs = tcs;

            protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
                SerializeToStreamAsync(stream, context).GetAwaiter().GetResult();

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                Ticks = Environment.TickCount64;
                _tcs.SetResult(true);
                return base.SerializeToStreamAsync(stream, context);
            }
        }
    }

    [Collection(nameof(DisableParallelization))] // Reduces chance of timing-related issues
    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public class SocketsHttpHandler_Cancellation_Test_NonParallel : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_Cancellation_Test_NonParallel(ITestOutputHelper output) : base(output)
        {
        }

        // [OuterLoop("Incurs significant delay.")] // TODO: Uncomment when ready
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("1.1", 10_000, 1_000, 100)]
        [InlineData("2.0", 10_000, 1_000, 100)]
        [InlineData("1.1", 20_000, 10_000, null)]
        [InlineData("2.0", 20_000, 10_000, null)]
        public static void CancelPendingRequest_DropsStalledConnectionAttempt(string versionString, int firstConnectionDelayMs, int requestTimeoutMs, int? pendingConnectionTimeoutOnRequestCompletion)
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            if (pendingConnectionTimeoutOnRequestCompletion is not null)
            {
                options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_PENDINGCONNECTIONTIMEOUTONREQUESTCOMPLETION"] = pendingConnectionTimeoutOnRequestCompletion.ToString();
            }

            RemoteExecutor.Invoke(CancelPendingRequest_DropsStalledConnectionAttempt_Impl, versionString, firstConnectionDelayMs.ToString(), requestTimeoutMs.ToString(), options).Dispose();
        }

        private static async Task CancelPendingRequest_DropsStalledConnectionAttempt_Impl(string versionString, string firstConnectionDelayMsString, string requestTimeoutMsString)
        {
            var version = Version.Parse(versionString);
            LoopbackServerFactory factory = GetFactoryForVersion(version);

            const int AttemptCount = 3;
            int firstConnectionDelayMs = int.Parse(firstConnectionDelayMsString);
            int requestTimeoutMs = int.Parse(requestTimeoutMsString);
            bool firstConnection = true;

            using CancellationTokenSource cts0 = new CancellationTokenSource(requestTimeoutMs);

            await factory.CreateClientAndServerAsync(async uri =>
            {
                using var handler = CreateHttpClientHandler(version);
                GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = DoConnect;
                using var client = new HttpClient(handler) { DefaultRequestVersion = version };

                await Assert.ThrowsAnyAsync<TaskCanceledException>(async () =>
                {
                    await client.GetAsync(uri, cts0.Token);
                });

                for (int i = 0; i < AttemptCount; i++)
                {
                    using var cts1 = new CancellationTokenSource(requestTimeoutMs);
                    using var response = await client.GetAsync(uri, cts1.Token);
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    for (int i = 0; i < AttemptCount; i++)
                    {
                        await connection.ReadRequestDataAsync();
                        await connection.SendResponseAsync();
                        connection.CompleteRequestProcessing();
                    }
                });
            });

            async ValueTask<Stream> DoConnect(SocketsHttpConnectionContext ctx, CancellationToken cancellationToken)
            {
                if (firstConnection)
                {
                    firstConnection = false;
                    await Task.Delay(100, cancellationToken); // Wait for the request to be pushed to the queue
                    cts0.Cancel(); // cancel the first request faster than RequestTimeoutMs
                    await Task.Delay(firstConnectionDelayMs, cancellationToken); // Simulate stalled connection
                }
                var s = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await s.ConnectAsync(ctx.DnsEndPoint, cancellationToken);

                return new NetworkStream(s, ownsSocket: true);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void PendingConnectionTimeout_Infinite_DoesNotCancelConections()
        {
            //RemoteInvokeOptions options = new RemoteInvokeOptions();
            ////options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_PENDINGCONNECTIONTIMEOUTONREQUESTCOMPLETION"] = int.MaxValue.ToString();
            //RemoteExecutor.Invoke(PendingConnectionTimeout_Infinite_DoesNotCancelConections_Impl, options).Dispose();
            PendingConnectionTimeout_Infinite_DoesNotCancelConections_Impl().GetAwaiter().GetResult();
        }

        private async Task PendingConnectionTimeout_Infinite_DoesNotCancelConections_Impl()
        {
            bool connected = false;
            CancellationTokenSource cts = new CancellationTokenSource();

            await new Http11LoopbackServerFactory().CreateClientAndServerAsync(async uri =>
            {
                using var handler = CreateHttpClientHandler(HttpVersion.Version11);
                GetUnderlyingSocketsHttpHandler(handler).ConnectCallback = DoConnect;
                using var client = new HttpClient(handler) { DefaultRequestVersion = HttpVersion.Version11 };

                await Assert.ThrowsAnyAsync<TaskCanceledException>(() => client.GetAsync(uri, cts.Token));
            },
            server => server.AcceptConnectionAsync(_ => Task.CompletedTask));

            async ValueTask<Stream> DoConnect(SocketsHttpConnectionContext ctx, CancellationToken cancellationToken)
            {
                var s = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                cancellationToken.Register(() => _output.WriteLine("yo cancelled"));
                await Task.Delay(500, cancellationToken);
                cts.Cancel();

                _output.WriteLine("0");
                // Ideally we should wait here for infinite amount of time, this is obviously impossible. 
                // This test is mostly here to cover the int.MaxValue == infinite special case.
                await Task.Delay(16_000, cancellationToken);
                _output.WriteLine("1");
                await s.ConnectAsync(ctx.DnsEndPoint, cancellationToken);
                _output.WriteLine("2");
                connected = true;
                return new NetworkStream(s, ownsSocket: true);
            }

            Assert.True(connected);
        }
    }
}
