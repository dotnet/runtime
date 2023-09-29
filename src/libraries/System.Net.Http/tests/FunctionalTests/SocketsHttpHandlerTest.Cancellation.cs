// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
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

                using (var handler = CreateHttpClientHandler(allowAllCertificates: true))
                using (var client = CreateHttpClient(handler))
                {
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
                            return await DefaultConnectCallback(context.DnsEndPoint, token);
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

        [OuterLoop("We wait for PendingConnectionTimeout which defaults to 5 seconds.")]
        [Fact]
        public async Task CancelPendingRequest_DropsStalledConnectionAttempt()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            await CancelPendingRequest_DropsStalledConnectionAttempt_Impl(UseVersion.ToString());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CancelPendingRequest_DropsStalledConnectionAttempt_CustomPendingConnectionTimeout()
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_PENDINGCONNECTIONTIMEOUTONREQUESTCOMPLETION"] = "42";

            RemoteExecutor.Invoke(CancelPendingRequest_DropsStalledConnectionAttempt_Impl, UseVersion.ToString(), options).Dispose();
        }

        private static async Task CancelPendingRequest_DropsStalledConnectionAttempt_Impl(string versionString)
        {
            using var requestCts = new CancellationTokenSource();
            var requestCanceledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (context, cancellation) =>
                {
                    requestCts.Cancel();
                    await Assert.ThrowsAsync<TaskCanceledException>(() => Task.Delay(-1, cancellation)).WaitAsync(TestHelper.PassingTestTimeout);
                    requestCanceledTcs.SetResult();
                    cancellation.ThrowIfCancellationRequested();
                    throw new UnreachableException();
                }
            };

            using var client = CreateHttpClient(handler, versionString);

            await Assert.ThrowsAnyAsync<TaskCanceledException>(() => client.GetAsync("https://dummy", requestCts.Token)).WaitAsync(TestHelper.PassingTestTimeout);

            await requestCanceledTcs.Task.WaitAsync(TestHelper.PassingTestTimeout);
        }

        [OuterLoop("We wait for PendingConnectionTimeout which defaults to 5 seconds.")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(20_000)]
        [InlineData(Timeout.Infinite)]
        public void PendingConnectionTimeout_HighValue_PendingConnectionIsNotCancelled(int timeout)
        {
            if (UseVersion == HttpVersion.Version30)
            {
                // HTTP3 does not support ConnectCallback
                return;
            }

            RemoteExecutor.Invoke(static async (versionString, timoutStr) =>
            {
                // Setup "infinite" timeout of int.MaxValue milliseconds
                AppContext.SetData("System.Net.SocketsHttpHandler.PendingConnectionTimeoutOnRequestCompletion", int.Parse(timoutStr));

                using var requestCts = new CancellationTokenSource();
                var connectionTestTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                using var handler = new SocketsHttpHandler
                {
                    ConnectCallback = async (context, cancellation) =>
                    {
                        requestCts.Cancel();

                        try
                        {
                            // Give PendingConnectionTimeout a chance to cancel the connection.
                            // 6 seconds is higher than the default 5 seconds
                            await Task.Delay(6_000, cancellation);
                            connectionTestTcs.SetResult();
                        }
                        catch (Exception ex)
                        {
                            connectionTestTcs.SetException(ex);
                        }

                        return Stream.Null;
                    }
                };

                using var client = CreateHttpClient(handler, versionString);

                await Assert.ThrowsAnyAsync<TaskCanceledException>(() => client.GetAsync("https://dummy", requestCts.Token)).WaitAsync(TestHelper.PassingTestTimeout);

                await connectionTestTcs.Task.WaitAsync(TestHelper.PassingTestTimeout);
            }, UseVersion.ToString(), timeout.ToString()).Dispose();
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task RequestSent_HandlerDisposed_RequestIsUnaffected(bool post)
        {
            byte[] postContent = "Hello world"u8.ToArray();

            TaskCompletionSource serverReceivedRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                using HttpClientHandler handler = CreateHttpClientHandler();
                using HttpClient client = CreateHttpClient(handler);

                using HttpRequestMessage request = CreateRequest(post ? HttpMethod.Post : HttpMethod.Get, uri, UseVersion);

                if (post)
                {
                    request.Content = new StreamContent(new DelegateDelegatingStream(new MemoryStream())
                    {
                        CanSeekFunc = () => false,
                        CopyToFunc = (destination, _) =>
                        {
                            destination.Flush();
                            Assert.True(serverReceivedRequest.Task.Wait(TestHelper.PassingTestTimeout));
                            destination.Write(postContent);
                        },
                        CopyToAsyncFunc = async (destination, _, ct) =>
                        {
                            await destination.FlushAsync(ct);
                            await serverReceivedRequest.Task.WaitAsync(ct);
                            await destination.WriteAsync(postContent, ct);
                        }
                    });
                }

                Task<HttpResponseMessage> clientTask = client.SendAsync(TestAsync, request);
                await serverReceivedRequest.Task.WaitAsync(TestHelper.PassingTestTimeout);

                handler.Dispose();
                await Task.Delay(1); // Give any potential disposal/cancellation some time to propagate

                await clientTask;
            },
            async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestDataAsync(readBody: false);
                    serverReceivedRequest.SetResult();

                    if (post)
                    {
                        byte[] received = await connection.ReadRequestBodyAsync();
                        Assert.Equal(postContent, received);
                    }

                    await connection.SendResponseAsync();
                });
            });
        }
    }
}
