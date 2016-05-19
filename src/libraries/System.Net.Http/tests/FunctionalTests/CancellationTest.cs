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
        // TODO: Issue #8692. Move this test server capability to Azure test server or Loopback server.
        private const string FastHeadersSlowBodyHost = "<TODO>";
        private const int FastHeadersSlowBodyPort = 1337;
        private const int ResponseBodyReadDelayInMilliseconds = 15000; // 15 seconds.
        private const int ResponseBodyLength = 1024;

        private static Uri s_fastHeadersSlowBodyServer = new Uri(string.Format(
            "http://{0}:{1}/?slow={2}&length={3}",
            FastHeadersSlowBodyHost,
            FastHeadersSlowBodyPort,
            ResponseBodyReadDelayInMilliseconds,
            ResponseBodyLength));
            
        private readonly ITestOutputHelper _output;

        public CancellationTest(ITestOutputHelper output)
        {
            _output = output;
            _output.WriteLine(s_fastHeadersSlowBodyServer.ToString());
        }

        [ActiveIssue(8663)]
        [Fact]
        public async Task GetIncludesReadingResponseBody_CancelUsingTimeout_TaskCanceledQuickly()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 0, 1);

                Task<HttpResponseMessage> getResponse =
                    client.GetAsync(s_fastHeadersSlowBodyServer, HttpCompletionOption.ResponseContentRead);

                stopwatch.Restart();
                await Assert.ThrowsAsync<TaskCanceledException>(
                    () => getResponse);
                stopwatch.Stop();
                _output.WriteLine("GetAsync() completed at: {0}", stopwatch.Elapsed.ToString());
                Assert.True(stopwatch.Elapsed < new TimeSpan(0,0,3), "Elapsed time should be short");
            }
        }

        [ActiveIssue(8663)]
        [Fact]
        public async Task GetIncludesReadingResponseBody_CancelUsingToken_TaskCanceledQuickly()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var cts = new CancellationTokenSource();
            using (var client = new HttpClient())
            {
                Task<HttpResponseMessage> getResponse =
                    client.GetAsync(s_fastHeadersSlowBodyServer, HttpCompletionOption.ResponseContentRead, cts.Token);

                Task ignore = Task.Delay(new TimeSpan(0, 0, 1)).ContinueWith(_ =>
                {
                    _output.WriteLine("Calling cts.Cancel() at: {0}", stopwatch.Elapsed.ToString());
                    cts.Cancel();
                });

                stopwatch.Restart();
                await Assert.ThrowsAsync<TaskCanceledException>(
                    () => getResponse);
                stopwatch.Stop();
                _output.WriteLine("GetAsync() completed at: {0}", stopwatch.Elapsed.ToString());
                Assert.True(stopwatch.Elapsed < new TimeSpan(0,0,3), "Elapsed time should be short");
            }
        }

        [ActiveIssue(8692)]
        [Fact]
        public async Task ResponseStreamRead_CancelUsingToken_TaskCanceledQuickly()
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            using (var client = new HttpClient())
            using (HttpResponseMessage response =
                await client.GetAsync(s_fastHeadersSlowBodyServer, HttpCompletionOption.ResponseHeadersRead))
            {
                stopwatch.Stop();
                _output.WriteLine("Time to get headers: {0}", stopwatch.Elapsed.ToString());
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var cts = new CancellationTokenSource();

                Stream stream = await response.Content.ReadAsStreamAsync();
                byte[] buffer = new byte[ResponseBodyLength];
                
                Task ignore = Task.Delay(new TimeSpan(0,0,1)).ContinueWith(_ =>
                {
                    _output.WriteLine("Calling cts.Cancel() at: {0}", stopwatch.Elapsed.ToString());
                    cts.Cancel();
                });

                stopwatch.Restart();
                await Assert.ThrowsAsync<TaskCanceledException>(
                    () => stream.ReadAsync(buffer, 0, buffer.Length, cts.Token));
                stopwatch.Stop();
                _output.WriteLine("ReadAsync() completed at: {0}", stopwatch.Elapsed.ToString());
                Assert.True(stopwatch.Elapsed < new TimeSpan(0,0,3), "Elapsed time should be short");
            }
        }
    }
}
