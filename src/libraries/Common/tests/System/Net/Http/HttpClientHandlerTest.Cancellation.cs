// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.DotNet.XUnitExtensions;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandler_Cancellation_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false, CancellationMode.Token)]
        [InlineData(true, CancellationMode.Token)]
        public async Task PostAsync_CancelDuringRequestContentSend_TaskCanceledQuickly(bool chunkedTransfer, CancellationMode mode)
        {
            if (LoopbackServerFactory.Version >= HttpVersion20.Value && chunkedTransfer)
            {
                // There is no chunked encoding in HTTP/2 and later
                return;
            }

            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            var serverRelease = new TaskCompletionSource<bool>();
            await LoopbackServerFactory.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        client.Timeout = Timeout.InfiniteTimeSpan;
                        var cts = new CancellationTokenSource();

                        var waitToSend = new TaskCompletionSource<bool>();
                        var contentSending = new TaskCompletionSource<bool>();
                        var req = new HttpRequestMessage(HttpMethod.Post, uri) { Version = UseVersion };
                        req.Content = new ByteAtATimeContent(int.MaxValue, waitToSend.Task, contentSending, millisecondDelayBetweenBytes: 1);
                        req.Headers.TransferEncodingChunked = chunkedTransfer;

                        Task<HttpResponseMessage> resp = client.SendAsync(TestAsync, req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                        waitToSend.SetResult(true);
                        await Task.WhenAny(contentSending.Task, resp);
                        if (!resp.IsCompleted)
                        {
                            Cancel(mode, client, cts);
                        }
                        await ValidateClientCancellationAsync(() => resp);
                    }
                }
                finally
                {
                    serverRelease.SetResult(true);
                }
            }, async server =>
            {
                try
                {
                    await server.AcceptConnectionAsync(connection => serverRelease.Task);
                }
                catch { };  // Ignore any closing errors since we did not really process anything.
            });
        }

        [Theory]
        [MemberData(nameof(OneBoolAndCancellationMode))]
        public async Task GetAsync_CancelDuringResponseHeadersReceived_TaskCanceledQuickly(bool connectionClose, CancellationMode mode)
        {
            if (LoopbackServerFactory.Version >= HttpVersion20.Value && connectionClose)
            {
                // There is no Connection header in HTTP/2 and later
                return;
            }

            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                var cts = new CancellationTokenSource();

                await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
                {
                    var partialResponseHeadersSent = new TaskCompletionSource<bool>();
                    var clientFinished = new TaskCompletionSource<bool>();

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestDataAsync();
                        await connection.SendResponseAsync(HttpStatusCode.OK, content: null, isFinal: false);

                        partialResponseHeadersSent.TrySetResult(true);
                        await clientFinished.Task;
                    });

                    await ValidateClientCancellationAsync(async () =>
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, url) { Version = UseVersion };
                        req.Headers.ConnectionClose = connectionClose;

                        Task<HttpResponseMessage> getResponse = client.SendAsync(TestAsync, req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                        await partialResponseHeadersSent.Task;
                        Cancel(mode, client, cts);
                        await getResponse;
                    });

                    try
                    {
                        clientFinished.SetResult(true);
                        await serverTask;
                    } catch { }
                });
            }
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/25760")]
        [MemberData(nameof(TwoBoolsAndCancellationMode))]
        public async Task GetAsync_CancelDuringResponseBodyReceived_Buffered_TaskCanceledQuickly(bool chunkedTransfer, bool connectionClose, CancellationMode mode)
        {
            if (LoopbackServerFactory.Version >= HttpVersion20.Value && (chunkedTransfer || connectionClose))
            {
                // There is no chunked encoding or connection header in HTTP/2 and later
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                var cts = new CancellationTokenSource();

                await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
                {
                    var responseHeadersSent = new TaskCompletionSource<bool>();
                    var clientFinished = new TaskCompletionSource<bool>();

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        var headers = new List<HttpHeaderData>();
                        headers.Add(chunkedTransfer ? new HttpHeaderData("Transfer-Encoding", "chunked") : new HttpHeaderData("Content-Length", "20"));
                        if (connectionClose)
                        {
                            headers.Add(new HttpHeaderData("Connection", "close"));
                        }

                        await connection.ReadRequestDataAsync();
                        await connection.SendResponseAsync(HttpStatusCode.OK, headers: headers, content: "123", isFinal: false);
                        responseHeadersSent.TrySetResult(true);
                        await clientFinished.Task;
                    });

                    await ValidateClientCancellationAsync(async () =>
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, url) { Version = UseVersion };
                        req.Headers.ConnectionClose = connectionClose;

                        Task<HttpResponseMessage> getResponse = client.SendAsync(TestAsync, req, HttpCompletionOption.ResponseContentRead, cts.Token);
                        await responseHeadersSent.Task;
                        await Task.Delay(1); // make it more likely that client will have started processing response body
                        Cancel(mode, client, cts);
                        await getResponse;
                    });

                    try
                    {
                        clientFinished.SetResult(true);
                        await serverTask;
                    } catch { }
                });
            }
        }

        [Theory]
        [MemberData(nameof(ThreeBools))]
        public async Task GetAsync_CancelDuringResponseBodyReceived_Unbuffered_TaskCanceledQuickly(bool chunkedTransfer, bool connectionClose, bool readOrCopyToAsync)
        {
            if (LoopbackServerFactory.Version >= HttpVersion20.Value && (chunkedTransfer || connectionClose))
            {
                // There is no chunked encoding or connection header in HTTP/2 and later
                return;
            }

            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                var cts = new CancellationTokenSource();

                await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
                {
                    var clientFinished = new TaskCompletionSource<bool>();

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        var headers = new List<HttpHeaderData>();
                        headers.Add(chunkedTransfer ? new HttpHeaderData("Transfer-Encoding", "chunked") : new HttpHeaderData("Content-Length", "20"));
                        if (connectionClose)
                        {
                            headers.Add(new HttpHeaderData("Connection", "close"));
                        }

                        await connection.ReadRequestDataAsync();
                        await connection.SendResponseAsync(HttpStatusCode.OK, headers: headers, isFinal: false);
                        await clientFinished.Task;
                    });

                    var req = new HttpRequestMessage(HttpMethod.Get, url) { Version = UseVersion };
                    req.Headers.ConnectionClose = connectionClose;
                    Task<HttpResponseMessage> getResponse = client.SendAsync(TestAsync, req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    await ValidateClientCancellationAsync(async () =>
                    {
                        HttpResponseMessage resp = await getResponse;
                        Stream respStream = await resp.Content.ReadAsStreamAsync(TestAsync);
                        Task readTask = readOrCopyToAsync ?
                            respStream.ReadAsync(new byte[1], 0, 1, cts.Token) :
                            respStream.CopyToAsync(Stream.Null, 10, cts.Token);
                        cts.Cancel();
                        await readTask;
                    });

                    try
                    {
                        clientFinished.SetResult(true);
                        await serverTask;
                    } catch { }
                });
            }
        }
        [Theory]
        [InlineData(CancellationMode.CancelPendingRequests, false)]
        [InlineData(CancellationMode.DisposeHttpClient, false)]
        [InlineData(CancellationMode.CancelPendingRequests, true)]
        [InlineData(CancellationMode.DisposeHttpClient, true)]
        public async Task GetAsync_CancelPendingRequests_DoesntCancelReadAsyncOnResponseStream(CancellationMode mode, bool copyToAsync)
        {
            if (IsWinHttpHandler && UseVersion >= HttpVersion20.Value)
            {
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;

                await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
                {
                    var clientReadSomeBody = new TaskCompletionSource<bool>();
                    var clientFinished = new TaskCompletionSource<bool>();

                    var responseContentSegment = new string('s', 3000);
                    int responseSegments = 4;
                    int contentLength = responseContentSegment.Length * responseSegments;

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestDataAsync();
                        await connection.SendResponseAsync(HttpStatusCode.OK, headers: new HttpHeaderData[] { new HttpHeaderData("Content-Length", contentLength.ToString()) }, isFinal: false);
                        for (int i = 0; i < responseSegments; i++)
                        {
                            await connection.SendResponseBodyAsync(responseContentSegment, isFinal: i == responseSegments - 1);
                            if (i == 0)
                            {
                                await clientReadSomeBody.Task;
                            }
                        }

                        await clientFinished.Task;
                    });


                    using (HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    using (Stream respStream = await resp.Content.ReadAsStreamAsync(TestAsync))
                    {
                        var result = new MemoryStream();
                        int b = respStream.ReadByte();
                        Assert.NotEqual(-1, b);
                        result.WriteByte((byte)b);

                        Cancel(mode, client, null); // should not cancel the operation, as using ResponseHeadersRead
                        clientReadSomeBody.SetResult(true);

                        if (copyToAsync)
                        {
                            await respStream.CopyToAsync(result, 10, new CancellationTokenSource().Token);
                        }
                        else
                        {
                            byte[] buffer = new byte[10];
                            int bytesRead;
                            while ((bytesRead = await respStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                result.Write(buffer, 0, bytesRead);
                            }
                        }

                        Assert.Equal(contentLength, result.Length);
                    }

                    clientFinished.SetResult(true);
                    await serverTask;
                });
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "MaxConnectionsPerServer is not supported on Browser")]
        public async Task MaxConnectionsPerServer_WaitingConnectionsAreCancelable()
        {
            if (LoopbackServerFactory.Version >= HttpVersion20.Value)
            {
                // HTTP/2 does not use connection limits.
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.MaxConnectionsPerServer = 1;
                client.Timeout = Timeout.InfiniteTimeSpan;

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    var serverAboutToBlock = new TaskCompletionSource<bool>();
                    var blockServerResponse = new TaskCompletionSource<bool>();

                    Task serverTask1 = server.AcceptConnectionAsync(async connection1 =>
                    {
                        await connection1.ReadRequestHeaderAsync();
                        await connection1.WriteStringAsync($"HTTP/1.1 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\n");
                        serverAboutToBlock.SetResult(true);
                        await blockServerResponse.Task;
                        await connection1.WriteStringAsync("Content-Length: 5\r\n\r\nhello");
                    });

                    Task get1 = client.GetAsync(url);
                    await serverAboutToBlock.Task;

                    var cts = new CancellationTokenSource();
                    Task get2 = ValidateClientCancellationAsync(() => client.GetAsync(url, cts.Token));
                    Task get3 = ValidateClientCancellationAsync(() => client.GetAsync(url, cts.Token));

                    Task get4 = client.GetAsync(url);

                    cts.Cancel();
                    await get2;
                    await get3;

                    blockServerResponse.SetResult(true);
                    await new[] { get1, serverTask1 }.WhenAllOrAnyFailed();

                    Task serverTask4 = server.AcceptConnectionSendResponseAndCloseAsync();

                    await new[] { get4, serverTask4 }.WhenAllOrAnyFailed();
                });
            }
        }

        [Fact]
        public async Task SendAsync_Cancel_CancellationTokenPropagates()
        {
            TaskCompletionSource<bool> clientCanceled = new TaskCompletionSource<bool>();
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    using (HttpClient client = CreateHttpClient())
                    {
                        OperationCanceledException ex = null;
                        try
                        {
                            await client.GetAsync(uri, cts.Token);
                        }
                        catch (OperationCanceledException e)
                        {
                            ex = e;
                        }
                        Assert.True(ex != null, "Expected OperationCancelledException, but no exception was thrown.");

                        Assert.True(cts.Token.IsCancellationRequested, "cts token IsCancellationRequested");

                        // .NET Framework has bug where it doesn't propagate token information.
                        Assert.True(ex.CancellationToken.IsCancellationRequested, "exception token IsCancellationRequested");

                        clientCanceled.SetResult(true);
                    }
                },
                async server =>
                {
                    Task serverTask = server.HandleRequestAsync();
                    await clientCanceled.Task;
                });
        }

        public static IEnumerable<object[]> PostAsync_Cancel_CancellationTokenPassedToContent_MemberData()
        {
            // Note: For HTTP2, the actual token will be a linked token and will not be an exact match for the original token.
            // Verify that it behaves as expected by cancelling it and validating that cancellation propagates.

            // StreamContent
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                var actualToken = new StrongBox<CancellationToken>();
                bool called = false;
                var content = new StreamContent(new DelegateStream(
                    canReadFunc: () => true,
                    readAsyncFunc: async (buffer, offset, count, cancellationToken) =>
                    {
                        int result = 1;
                        if (called)
                        {
                            result = 0;
                            Assert.False(cancellationToken.IsCancellationRequested);
                            tokenSource.Cancel();

                            // Wait for cancellation to occur.  It should be very quickly after it's been requested.
                            var tcs = new TaskCompletionSource<bool>();
                            using (cancellationToken.Register(() => tcs.SetResult(true)))
                            {
                                await tcs.Task;
                            }
                        }

                        called = true;
                        return result;
                    }
                ));
                yield return new object[] { content, tokenSource };
            }

            // MultipartContent
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                var actualToken = new StrongBox<CancellationToken>();
                bool called = false;
                var content = new MultipartContent();
                content.Add(new StreamContent(new DelegateStream(
                    canReadFunc: () => true,
                    canSeekFunc: () => true,
                    lengthFunc: () => 1,
                    positionGetFunc: () => 0,
                    positionSetFunc: _ => {},
                    readAsyncFunc: async (buffer, offset, count, cancellationToken) =>
                    {
                        int result = 1;
                        if (called)
                        {
                            result = 0;
                            Assert.False(cancellationToken.IsCancellationRequested);
                            tokenSource.Cancel();

                            // Wait for cancellation to occur.  It should be very quickly after it's been requested.
                            var tcs = new TaskCompletionSource<bool>();
                            using (cancellationToken.Register(() => tcs.SetResult(true)))
                            {
                                await tcs.Task;
                            }
                        }

                        called = true;
                        return result;
                    }
                )));
                yield return new object[] { content, tokenSource };
            }

            // MultipartFormDataContent
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                var actualToken = new StrongBox<CancellationToken>();
                bool called = false;
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(new DelegateStream(
                    canReadFunc: () => true,
                    canSeekFunc: () => true,
                    lengthFunc: () => 1,
                    positionGetFunc: () => 0,
                    positionSetFunc: _ => {},
                    readAsyncFunc: async (buffer, offset, count, cancellationToken) =>
                    {
                        int result = 1;
                        if (called)
                        {
                            result = 0;
                            Assert.False(cancellationToken.IsCancellationRequested);
                            tokenSource.Cancel();

                            // Wait for cancellation to occur.  It should be very quickly after it's been requested.
                            var tcs = new TaskCompletionSource<bool>();
                            using (cancellationToken.Register(() => tcs.SetResult(true)))
                            {
                                await tcs.Task;
                            }
                        }

                        called = true;
                        return result;
                    }
                )));
                yield return new object[] { content, tokenSource };
            }
        }

#if !NETFRAMEWORK
        [ActiveIssue("https://github.com/dotnet/runtime/issues/41531")]
        [OuterLoop("Uses Task.Delay")]
        [Theory]
        [MemberData(nameof(PostAsync_Cancel_CancellationTokenPassedToContent_MemberData))]
        public async Task PostAsync_Cancel_CancellationTokenPassedToContent(HttpContent content, CancellationTokenSource cancellationTokenSource)
        {
            if (IsWinHttpHandler)
            {
                return;
            }
            // Skipping test for a sync scenario becasue DelegateStream drops the original cancellationToken when it calls Read/Write methods.
            // As a result, ReadAsyncFunc receives default in cancellationToken, which will never get signaled through the cancellationTokenSource.
            if (!TestAsync)
            {
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using (var invoker = new HttpMessageInvoker(CreateHttpClientHandler()))
                    using (var req = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content, Version = UseVersion })
                    try
                    {
                        using (HttpResponseMessage resp = await invoker.SendAsync(TestAsync, req, cancellationTokenSource.Token))
                        {
                            Assert.Equal("Hello World", await resp.Content.ReadAsStringAsync());
                        }
                    }
                    catch (OperationCanceledException) { }
                },
                async server =>
                {
                    try
                    {
                        await server.HandleRequestAsync(content: "Hello World");
                    }
                    catch (Exception) { }
                });
        }
#endif

        private async Task ValidateClientCancellationAsync(Func<Task> clientBodyAsync)
        {
            var stopwatch = Stopwatch.StartNew();
            Exception error = await Record.ExceptionAsync(clientBodyAsync);
            stopwatch.Stop();

            Assert.NotNull(error);

            Assert.True(
                    error is OperationCanceledException,
                    "Expected cancellation exception, got:" + Environment.NewLine + error);

            Assert.True(stopwatch.Elapsed < new TimeSpan(0, 0, 60), $"Elapsed time {stopwatch.Elapsed} should be less than 60 seconds, was {stopwatch.Elapsed.TotalSeconds}");
        }

        private static void Cancel(CancellationMode mode, HttpClient client, CancellationTokenSource cts)
        {
            if ((mode & CancellationMode.Token) != 0)
            {
                cts?.Cancel();
            }

            if ((mode & CancellationMode.CancelPendingRequests) != 0)
            {
                client?.CancelPendingRequests();
            }

            if ((mode & CancellationMode.DisposeHttpClient) != 0)
            {
                client?.Dispose();
            }
        }

        [Flags]
        public enum CancellationMode
        {
            Token = 0x1,
            CancelPendingRequests = 0x2,
            DisposeHttpClient = 0x4
        }

        public static IEnumerable<object[]> OneBoolAndCancellationMode() =>
            from first in BoolValues
            from mode in new[] { CancellationMode.Token, CancellationMode.CancelPendingRequests, CancellationMode.DisposeHttpClient, CancellationMode.Token | CancellationMode.CancelPendingRequests }
            select new object[] { first, mode };

        public static IEnumerable<object[]> TwoBoolsAndCancellationMode() =>
            from first in BoolValues
            from second in BoolValues
            from mode in new[] { CancellationMode.Token, CancellationMode.CancelPendingRequests, CancellationMode.DisposeHttpClient, CancellationMode.Token | CancellationMode.CancelPendingRequests }
            select new object[] { first, second, mode };

        public static IEnumerable<object[]> ThreeBools() =>
            from first in BoolValues
            from second in BoolValues
            from third in BoolValues
            select new object[] { first, second, third };
    }
}
