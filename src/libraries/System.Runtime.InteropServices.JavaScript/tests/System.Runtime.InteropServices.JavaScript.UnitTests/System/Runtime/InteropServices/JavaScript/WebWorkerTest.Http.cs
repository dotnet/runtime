// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Xunit;
using System.IO;
using System.Text;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class WebWorkerHttpTest : WebWorkerTestBase
    {
        #region HTTP

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task HttpClient_ContentInSameThread(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
            var uri = WebWorkerTestHelper.GetOriginUrl() + "/test.json";

            await executor.Execute(async () =>
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Assert.Contains("hello", body);
                Assert.Contains("world", body);
            }, cts.Token);
        }

        private static HttpRequestOptionsKey<bool> WebAssemblyEnableStreamingRequestKey = new("WebAssemblyEnableStreamingRequest");
        private static HttpRequestOptionsKey<bool> WebAssemblyEnableStreamingResponseKey = new("WebAssemblyEnableStreamingResponse");
        private static string HelloJson = "{'hello':'world'}".Replace('\'', '"');
        private static string EchoStart = "{\"Method\":\"POST\",\"Url\":\"/Echo.ashx";

        private async Task HttpClient_ActionInDifferentThread(string url, Executor executor1, Executor executor2, Func<HttpResponseMessage, Task> e2Job)
        {
            using var cts = CreateTestCaseTimeoutSource();

            var e1Job = async (Task e2done, TaskCompletionSource<HttpResponseMessage> e1State) =>
            {
                using var ms = new MemoryStream();
                await ms.WriteAsync(Encoding.UTF8.GetBytes(HelloJson));

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Options.Set(WebAssemblyEnableStreamingResponseKey, true);
                req.Content = new StreamContent(ms);
                using var client = new HttpClient();
                var pr = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                using var response = await pr;

                // share the state with the E2 continuation
                e1State.SetResult(response);

                await e2done;
            };
            await ActionsInDifferentThreads<HttpResponseMessage>(executor1, executor2, e1Job, e2Job, cts);
        }

        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public async Task HttpClient_ContentInDifferentThread(Executor executor1, Executor executor2)
        {
            var url = WebWorkerTestHelper.LocalHttpEcho + "?guid=" + Guid.NewGuid();
            await HttpClient_ActionInDifferentThread(url, executor1, executor2, async (HttpResponseMessage response) =>
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Assert.StartsWith(EchoStart, body);
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public async Task HttpClient_CancelInDifferentThread(Executor executor1, Executor executor2)
        {
            var url = WebWorkerTestHelper.LocalHttpEcho + "?delay10sec=true&guid=" + Guid.NewGuid();
            await HttpClient_ActionInDifferentThread(url, executor1, executor2, async (HttpResponseMessage response) =>
            {
                await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    var promise = response.Content.ReadAsStringAsync(cts.Token);
                    cts.Cancel();
                    await promise;
                });
            });
        }

        #endregion
    }
}
