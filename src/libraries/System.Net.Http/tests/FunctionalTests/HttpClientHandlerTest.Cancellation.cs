// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandler_Cancellation_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_Cancellation_Test(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(false, CancellationMode.Token)]
        [InlineData(true, CancellationMode.Token)]
        public async Task PostAsync_CancelDuringRequestContentSend_TaskCanceledQuickly(bool chunkedTransfer, CancellationMode mode)
        {
            if (!UseSocketsHttpHandler)
            {
                // Issue #27063: hangs / doesn't cancel
                return;
            }

            var serverRelease = new TaskCompletionSource<bool>();
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                try
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        client.Timeout = Timeout.InfiniteTimeSpan;
                        var cts = new CancellationTokenSource();

                        var waitToSend = new TaskCompletionSource<bool>();
                        var contentSending = new TaskCompletionSource<bool>();
                        var req = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new ByteAtATimeContent(int.MaxValue, waitToSend.Task, contentSending) };
                        req.Headers.TransferEncodingChunked = chunkedTransfer;

                        Task<HttpResponseMessage> resp = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                        waitToSend.SetResult(true);
                        await contentSending.Task;
                        Cancel(mode, client, cts);
                        await ValidateClientCancellationAsync(() => resp);
                    }
                }
                finally
                {
                    serverRelease.SetResult(true);
                }
            }, server => server.AcceptConnectionAsync(connection => serverRelease.Task));
        }

        [Theory]
        [MemberData(nameof(TwoBoolsAndCancellationMode))]
        public async Task GetAsync_CancelDuringResponseHeadersReceived_TaskCanceledQuickly(bool chunkedTransfer, bool connectionClose, CancellationMode mode)
        {
            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                var cts = new CancellationTokenSource();

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    var partialResponseHeadersSent = new TaskCompletionSource<bool>();
                    var clientFinished = new TaskCompletionSource<bool>();

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(
                            $"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\n"); // missing final \r\n so headers don't complete

                        partialResponseHeadersSent.TrySetResult(true);
                        await clientFinished.Task;
                    });

                    await ValidateClientCancellationAsync(async () =>
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.ConnectionClose = connectionClose;

                        Task<HttpResponseMessage> getResponse = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
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

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Test needs to be rewritten to work on UAP due to WinRT differences")]
        [Theory]
        [MemberData(nameof(TwoBoolsAndCancellationMode))]
        public async Task GetAsync_CancelDuringResponseBodyReceived_Buffered_TaskCanceledQuickly(bool chunkedTransfer, bool connectionClose, CancellationMode mode)
        {
            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                var cts = new CancellationTokenSource();

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    var responseHeadersSent = new TaskCompletionSource<bool>();
                    var clientFinished = new TaskCompletionSource<bool>();

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(
                            $"HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            (!chunkedTransfer ? "Content-Length: 20\r\n" : "") +
                            (connectionClose ? "Connection: close\r\n" : "") +
                            $"\r\n123"); // "123" is part of body and could either be chunked size or part of content-length bytes, both incomplete

                        responseHeadersSent.TrySetResult(true);
                        await clientFinished.Task;
                    });

                    await ValidateClientCancellationAsync(async () =>
                    {
                        var req = new HttpRequestMessage(HttpMethod.Get, url);
                        req.Headers.ConnectionClose = connectionClose;

                        Task<HttpResponseMessage> getResponse = client.SendAsync(req, HttpCompletionOption.ResponseContentRead, cts.Token);
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
            if (IsNetfxHandler || IsCurlHandler)
            {
                // doesn't cancel
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
                var cts = new CancellationTokenSource();

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    var clientFinished = new TaskCompletionSource<bool>();

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(
                            $"HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            (!chunkedTransfer ? "Content-Length: 20\r\n" : "") +
                            (connectionClose ? "Connection: close\r\n" : "") +
                            $"\r\n");

                        await clientFinished.Task;
                    });

                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.ConnectionClose = connectionClose;
                    Task<HttpResponseMessage> getResponse = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                    await ValidateClientCancellationAsync(async () =>
                    {
                        HttpResponseMessage resp = await getResponse;
                        Stream respStream = await resp.Content.ReadAsStreamAsync();
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
        [InlineData(CancellationMode.DisposeHttpClient, true)]
        [InlineData(CancellationMode.CancelPendingRequests, false)]
        [InlineData(CancellationMode.DisposeHttpClient, true)]
        public async Task GetAsync_CancelPendingRequests_DoesntCancelReadAsyncOnResponseStream(CancellationMode mode, bool copyToAsync)
        {
            if (IsNetfxHandler)
            {
                // throws ObjectDisposedException as part of Stream.CopyToAsync/ReadAsync
                return;
            }
            if (IsCurlHandler)
            {
                // Issue #27065
                // throws OperationCanceledException from Stream.CopyToAsync/ReadAsync
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                client.Timeout = Timeout.InfiniteTimeSpan;

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    var clientReadSomeBody = new TaskCompletionSource<bool>();
                    var clientFinished = new TaskCompletionSource<bool>();

                    var responseContentSegment = new string('s', 3000);
                    int responseSegments = 4;
                    int contentLength = responseContentSegment.Length * responseSegments;

                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(
                            $"HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            $"Content-Length: {contentLength}\r\n" +
                            $"\r\n");

                        for (int i = 0; i < responseSegments; i++)
                        {
                            await connection.Writer.WriteAsync(responseContentSegment);
                            if (i == 0)
                            {
                                await clientReadSomeBody.Task;
                            }
                        }

                        await clientFinished.Task;
                    });


                    using (HttpResponseMessage resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    using (Stream respStream = await resp.Content.ReadAsStreamAsync())
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

        [ActiveIssue(32000)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WinRT stack can't set MaxConnectionsPerServer < 2")]
        [Fact]
        public async Task MaxConnectionsPerServer_WaitingConnectionsAreCancelable()
        {
            if (IsNetfxHandler)
            {
                // Throws HttpRequestException wrapping a WebException for the canceled request
                // instead of throwing an OperationCanceledException or a canceled WebException directly.
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = new HttpClient(handler))
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
                        await connection1.Writer.WriteAsync($"HTTP/1.1 200 OK\r\nConnection: close\r\nDate: {DateTimeOffset.UtcNow:R}\r\n");
                        serverAboutToBlock.SetResult(true);
                        await blockServerResponse.Task;
                        await connection1.Writer.WriteAsync("Content-Length: 5\r\n\r\nhello");
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
                        catch(OperationCanceledException e)
                        {
                            ex = e;
                        }
                        Assert.True(ex != null, "Expected OperationCancelledException, but no exception was thrown.");

                        Assert.True(cts.Token.IsCancellationRequested, "cts token IsCancellationRequested");

                        if (!PlatformDetection.IsFullFramework)
                        {
                            // .NET Framework has bug where it doesn't propagate token information.
                            Assert.True(ex.CancellationToken.IsCancellationRequested, "exception token IsCancellationRequested");
                        }
                        clientCanceled.SetResult(true);
                    }
                },
                async server =>
                {
                    Task serverTask = server.HandleRequestAsync();
                    await clientCanceled.Task;
                });
        }

        private async Task ValidateClientCancellationAsync(Func<Task> clientBodyAsync)
        {
            var stopwatch = Stopwatch.StartNew();
            Exception error = await Record.ExceptionAsync(clientBodyAsync);
            stopwatch.Stop();

            Assert.NotNull(error);

            if (IsNetfxHandler)
            {
                Assert.True(
                    error is WebException we && we.Status == WebExceptionStatus.RequestCanceled ||
                    error is OperationCanceledException,
                    "Expected cancellation exception, got:" + Environment.NewLine + error);
            }
            else
            {
                Assert.True(
                    error is OperationCanceledException,
                    "Expected cancellation exception, got:" + Environment.NewLine + error);
            }

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

        private static readonly bool[] s_bools = new[] { true, false };

        public static IEnumerable<object[]> TwoBoolsAndCancellationMode() =>
            from first in s_bools
            from second in s_bools
            from mode in new[] { CancellationMode.Token, CancellationMode.CancelPendingRequests, CancellationMode.DisposeHttpClient, CancellationMode.Token | CancellationMode.CancelPendingRequests }
            select new object[] { first, second, mode };

        public static IEnumerable<object[]> ThreeBools() =>
            from first in s_bools
            from second in s_bools
            from third in s_bools
            select new object[] { first, second, third };

        private sealed class ByteAtATimeContent : HttpContent
        {
            private readonly Task _waitToSend;
            private readonly TaskCompletionSource<bool> _startedSend;
            private readonly int _length;

            public ByteAtATimeContent(int length, Task waitToSend, TaskCompletionSource<bool> startedSend)
            {
                _length = length;
                _waitToSend = waitToSend;
                _startedSend = startedSend;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                await _waitToSend;
                _startedSend.SetResult(true);

                var buffer = new byte[1] { 42 };
                for (int i = 0; i < _length; i++)
                {
                    await stream.WriteAsync(buffer);
                    await stream.FlushAsync();
                    await Task.Delay(1);
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _length;
                return true;
            }
        }
    }
}
