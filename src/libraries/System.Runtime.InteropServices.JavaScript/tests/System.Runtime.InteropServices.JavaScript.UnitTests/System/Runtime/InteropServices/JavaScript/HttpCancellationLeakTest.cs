// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    // Regression coverage for https://github.com/dotnet/runtime/issues/133667
    // Cancelling a response body read must not leave a faulted, never-awaited JS-promise Task behind.
    public class HttpCancellationLeakTest
    {
        // XHarness hosts NetCoreServer as middleware and passes its address in this variable.
        private static Uri EchoUri(string query = "")
            => new Uri($"http://{Environment.GetEnvironmentVariable("DOTNET_TEST_HTTPHOST")}/Echo.ashx{query}");

        private static readonly HttpRequestOptionsKey<bool> EnableStreamingResponse = new("WebAssemblyEnableStreamingResponse");

        // The bug lives in BrowserHttpReadStream, so the buffered path would prove nothing.
        // StreamContent is only used for the streaming path; it hands out a ReadOnlyStream wrapper
        // over BrowserHttpReadStream.
        private static async Task<Stream> OpenStreamedResponse(HttpClient client, string query, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, EchoUri(query));
            request.Options.Set(EnableStreamingResponse, true);

            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.IsType<StreamContent>(response.Content);

            return await response.Content.ReadAsStreamAsync();
        }

        private static readonly List<Exception> s_unobserved = new();

        private static void OnUnobserved(object sender, UnobservedTaskExceptionEventArgs e)
        {
            s_unobserved.Add(e.Exception);
            e.SetObserved();
        }

        // The orphaned promise only faults once the aborted fetch rejects, and the event only fires
        // once the Task is finalized, so both need a chance to happen.
        private static async Task<string[]> CollectUnobservedJSExceptions(Func<Task> body)
        {
            s_unobserved.Clear();
            TaskScheduler.UnobservedTaskException += OnUnobserved;
            try
            {
                await body();

                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(50);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
            finally
            {
                TaskScheduler.UnobservedTaskException -= OnUnobserved;
            }

            return s_unobserved
                .SelectMany(e => e is AggregateException ae ? ae.Flatten().InnerExceptions.Cast<Exception>() : new[] { e })
                .Select(e => $"{e.GetType().Name}: {e.Message}")
                .ToArray();
        }

        // Guards the assertion used by the tests below: if dropping a rejected JS promise does not
        // raise UnobservedTaskException on this platform, those tests prove nothing.
        [Fact]
        public async Task OrphanedRejectedJsPromise_RaisesUnobservedTaskException()
        {
            string[] unobserved = await CollectUnobservedJSExceptions(async () =>
            {
                await JavaScriptTestHelper.InitializeAsync();
                _ = JavaScriptTestHelper.Reject("intentionally orphaned");
                await Task.Delay(50);
            });

            Assert.NotEmpty(unobserved);
        }

        [Fact]
        public async Task CancelBeforeFirstBodyRead_DoesNotOrphanJsPromise()
        {
            string[] unobserved = await CollectUnobservedJSExceptions(async () =>
            {
                // the controller registers Abort on the *request* token, so the same token has to
                // be used for the read or the fetch is never aborted and nothing rejects
                using var cts = new CancellationTokenSource();

                using var client = new HttpClient();
                using Stream stream = await OpenStreamedResponse(client, "", cts.Token);

                cts.Cancel();

                var buffer = new byte[1024];
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => stream.ReadAsync(buffer, 0, buffer.Length, cts.Token));
            });

            Assert.Empty(unobserved);
        }

        [Fact]
        public async Task CancelBetweenStreamedBodyReads_DoesNotOrphanJsPromise()
        {
            // Echo.ashx?delay1sec writes 10 bytes, flushes, waits a second, then writes the rest,
            // so the second read is guaranteed to still be in flight when the token is cancelled.
            string[] unobserved = await CollectUnobservedJSExceptions(async () =>
            {
                using var cts = new CancellationTokenSource();

                using var client = new HttpClient();
                using Stream stream = await OpenStreamedResponse(client, "?delay1sec", cts.Token);

                var buffer = new byte[1024 * 1024];

                int first = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                Assert.True(first > 0);

                cts.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => stream.ReadAsync(buffer, 0, buffer.Length, cts.Token));
            });

            Assert.Empty(unobserved);
        }

        [Fact]
        public async Task ReadWithoutCancellation_DoesNotOrphanJsPromise()
        {
            string[] unobserved = await CollectUnobservedJSExceptions(async () =>
            {
                using var client = new HttpClient();
                using Stream stream = await OpenStreamedResponse(client, "", CancellationToken.None);

                var buffer = new byte[1024];
                while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) { }
            });

            Assert.Empty(unobserved);
        }
    }
}
