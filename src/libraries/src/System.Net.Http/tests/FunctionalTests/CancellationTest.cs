// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class CancellationTest
    {
        private readonly ITestOutputHelper _output;

        public CancellationTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [ActiveIssue(10504)]
        [OuterLoop] // includes seconds of delay
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task GetAsync_ResponseContentRead_CancelUsingTimeoutOrToken_TaskCanceledQuickly(
            bool useTimeout, bool startResponseBody)
        {
            var cts = new CancellationTokenSource(); // ignored if useTimeout==true
            TimeSpan timeout = useTimeout ? new TimeSpan(0, 0, 1) : Timeout.InfiniteTimeSpan;
            CancellationToken cancellationToken = useTimeout ? CancellationToken.None : cts.Token;

            using (var client = new HttpClient() { Timeout = timeout })
            {
                var triggerResponseWrite = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var triggerRequestCancel = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    Task serverTask = LoopbackServer.AcceptSocketAsync(server, async (socket, stream, reader, writer) =>
                    {
                        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) ;
                        await writer.WriteAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "Content-Length: 16000\r\n" +
                            "\r\n" +
                            (startResponseBody ? "less than 16000 bytes" : ""));

                        await Task.Delay(1000);
                        triggerRequestCancel.SetResult(true); // allow request to cancel
                        await triggerResponseWrite.Task; // pause until we're released
                        
                        return null;
                    });

                    var stopwatch = Stopwatch.StartNew();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    {
                        Task<HttpResponseMessage> getResponse = client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken);
                        await triggerRequestCancel.Task;
                        cts.Cancel();
                        await getResponse;
                    });
                    stopwatch.Stop();
                    _output.WriteLine("GetAsync() completed at: {0}", stopwatch.Elapsed.ToString());

                    triggerResponseWrite.SetResult(true);
                    Assert.True(stopwatch.Elapsed < new TimeSpan(0, 0, 10), "Elapsed time should be short");
                });
            }
        }

        [ActiveIssue(9075, TestPlatforms.AnyUnix)] // recombine this test into the subsequent one when issue is fixed
        [OuterLoop] // includes seconds of delay
        [Fact]
        public Task ReadAsStreamAsync_ReadAsync_Cancel_BodyNeverStarted_TaskCanceledQuickly()
        {
            return ReadAsStreamAsync_ReadAsync_Cancel_TaskCanceledQuickly(false);
        }

        [OuterLoop] // includes seconds of delay
        [Theory]
        [InlineData(true)]
        public async Task ReadAsStreamAsync_ReadAsync_Cancel_TaskCanceledQuickly(bool startResponseBody)
        {
            using (var client = new HttpClient())
            {
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    var triggerResponseWrite = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                    Task serverTask = LoopbackServer.AcceptSocketAsync(server, async (socket, stream, reader, writer) =>
                    {
                        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) ;
                        await writer.WriteAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "Content-Length: 16000\r\n" +
                            "\r\n" +
                            (startResponseBody ? "20 bytes of the body" : ""));

                        await triggerResponseWrite.Task; // pause until we're released
                        
                        return null;
                    });

                    using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        // Read all expected content
                        byte[] buffer = new byte[20];
                        if (startResponseBody)
                        {
                            int totalRead = 0;
                            int bytesRead;
                            while (totalRead < 20 && (bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                totalRead += bytesRead;
                            }
                        }

                        // Now do a read that'll need to be canceled
                        var stopwatch = Stopwatch.StartNew();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(
                            () => responseStream.ReadAsync(buffer, 0, buffer.Length, new CancellationTokenSource(1000).Token));
                        stopwatch.Stop();

                        triggerResponseWrite.SetResult(true);
                        _output.WriteLine("ReadAsync() completed at: {0}", stopwatch.Elapsed.ToString());
                        Assert.True(stopwatch.Elapsed < new TimeSpan(0, 0, 10), "Elapsed time should be short");
                    }
                });
            }
        }
    }
}
