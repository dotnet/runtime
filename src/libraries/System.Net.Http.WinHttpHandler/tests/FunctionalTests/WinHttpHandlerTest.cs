// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Functional.Tests;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

// Can't use "WinHttpHandler.Functional.Tests" in namespace as it won't compile.
// WinHttpHandler is a class and not a namespace and can't be part of namespace paths.
namespace System.Net.Http.WinHttpHandlerFunctional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public class WinHttpHandlerTest
    {
        private const string SlowServer = "http://httpbin.org/drip?numbytes=1&duration=1&delay=40&code=200";

        private readonly ITestOutputHelper _output;

        public WinHttpHandlerTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [OuterLoop]
        [Fact]
        public void SendAsync_SimpleGet_Success()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                var response = client.GetAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer).Result;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _output.WriteLine(responseContent);
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(CookieUsePolicy.UseInternalCookieStoreOnly, "cookieName1", "cookieValue1")]
        [InlineData(CookieUsePolicy.UseSpecifiedCookieContainer, "cookieName2", "cookieValue2")]
        public async Task GetAsync_RedirectResponseHasCookie_CookieSentToFinalUri(
            CookieUsePolicy cookieUsePolicy,
            string cookieName,
            string cookieValue)
        {
            Uri uri = System.Net.Test.Common.Configuration.Http.RemoteHttp11Server.RedirectUriForDestinationUri(302, System.Net.Test.Common.Configuration.Http.RemoteEchoServer, 1);
            var handler = new WinHttpHandler();
            handler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
            handler.CookieUsePolicy = cookieUsePolicy;
            if (cookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer)
            {
                handler.CookieContainer = new CookieContainer();
            }

            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add(
                    "X-SetCookie",
                    string.Format("{0}={1};Path=/", cookieName, cookieValue));
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(JsonMessageContainsKeyValue(responseText, cookieName, cookieValue));
                }
            }
        }

        [OuterLoop]
        [Fact]
        public async Task SendAsync_SlowServerAndCancel_ThrowsTaskCanceledException()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                var cts = new CancellationTokenSource();
                Task<HttpResponseMessage> t = client.GetAsync(SlowServer, cts.Token);

                await Task.Delay(500);
                cts.Cancel();

                AggregateException ag = Assert.Throws<AggregateException>(() => t.Wait());
                Assert.IsType<TaskCanceledException>(ag.InnerException);
            }
        }

        [OuterLoop]
        [Fact]
        public async void SendAsync_SlowServerRespondsAfterDefaultReceiveTimeout_ThrowsHttpRequestException()
        {
            var handler = new WinHttpHandler();
            using (var client = new HttpClient(handler))
            {
                var triggerResponseWrite = new TaskCompletionSource<bool>();
                var triggerRequestWait = new TaskCompletionSource<bool>();

                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.SendResponseAsync($"HTTP/1.1 200 OK\r\nContent-Length: 16000\r\n\r\n");

                        triggerRequestWait.SetResult(true);
                        await triggerResponseWrite.Task;
                    });

                    HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
                    {
                        Task<HttpResponseMessage> t = client.GetAsync(url);
                        await triggerRequestWait.Task;
                        var _ = await t;
                    });
                    Assert.IsType<IOException>(ex.InnerException);
                    Assert.NotNull(ex.InnerException.InnerException);
                    Assert.Contains("The operation timed out", ex.InnerException.InnerException.Message);

                    triggerResponseWrite.SetResult(true);
                });
            }
        }

        [Fact]
        public async Task SendAsync_GetUsingChunkedEncoding_ThrowsHttpRequestException()
        {
            // WinHTTP doesn't support GET requests with a request body that uses
            // chunked encoding. This test pins this behavior and verifies that the
            // error handling is working correctly.
            var server = new Uri("http://www.microsoft.com"); // No network I/O actually happens.
            var request = new HttpRequestMessage(HttpMethod.Get, server);
            request.Content = new StringContent("Request body");
            request.Headers.TransferEncodingChunked = true;

            var handler = new WinHttpHandler();
            using (HttpClient client = new HttpClient(handler))
            {
                HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1607OrGreater))]
        public async Task GetAsync_SetCookieContainerMultipleCookies_CookiesSent()
        {
            var cookies = new Cookie[]
            {
                new Cookie("hello", "world"),
                new Cookie("foo", "bar"),
                new Cookie("ABC", "123")
            };

            WinHttpHandler handler = new WinHttpHandler();
            var cookieContainer = new CookieContainer();

            foreach (Cookie c in cookies)
            {
                cookieContainer.Add(Configuration.Http.Http2RemoteEchoServer, c);
            }

            handler.CookieContainer = cookieContainer;
            handler.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
            handler.ServerCertificateValidationCallback = (m, cert, chain, err) => true;
            string payload = "Cookie Test";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.Http2RemoteEchoServer) { Version = HttpVersion20.Value };
            request.Content = new StringContent(payload);
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(HttpVersion20.Value, response.Version);
                string responsePayload = await response.Content.ReadAsStringAsync();
                var responseContent = Newtonsoft.Json.JsonConvert
                    .DeserializeAnonymousType(responsePayload, new { Method = "_", BodyContent = "_", Cookies = new Dictionary<string, string>() });
                Assert.Equal("POST", responseContent.Method);
                Assert.Equal(payload, responseContent.BodyContent);
                Assert.Equal(cookies.ToDictionary(c => c.Name, c => c.Value), responseContent.Cookies);
            };
        }

        [OuterLoop("Uses delays")]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version1607OrGreater))]
        public async Task SendAsync_MultipleHttp2ConnectionsEnabled_CreateAdditionalConnections()
        {
            // Warm up thread pool because the full .NET Framework calls synchronous Stream.Read() and we need to delay those calls thus threads will get blocked.
            ThreadPool.GetMinThreads(out _, out int completionPortThreads);
            ThreadPool.SetMinThreads(401, completionPortThreads);
            using var handler = new WinHttpHandler();
            handler.EnableMultipleHttp2Connections = true;
            handler.ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            const int maxActiveStreamsLimit = 100 * 3;
            const string payloadText = "Multiple HTTP/2 connections test.";
            TaskCompletionSource<bool> delaySource = new TaskCompletionSource<bool>();
            using var client = new HttpClient(handler);
            List<(Task<HttpResponseMessage> task, DelayedStream stream)> requests = new List<(Task<HttpResponseMessage> task, DelayedStream stream)>();
            for (int i = 0; i < maxActiveStreamsLimit; i++)
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.Http2RemoteEchoServer) { Version = HttpVersion20.Value };
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadText);
                DelayedStream content = new DelayedStream(payloadBytes, delaySource.Task);
                request.Content = new StreamContent(content);
                requests.Add((client.SendAsync(request, HttpCompletionOption.ResponseContentRead), content));
            }

            HttpRequestMessage aboveLimitRequest = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.Http2RemoteEchoServer) { Version = HttpVersion20.Value };
            aboveLimitRequest.Content = new StringContent($"{payloadText}-{maxActiveStreamsLimit + 1}");
            Task<HttpResponseMessage> aboveLimitResponseTask = client.SendAsync(aboveLimitRequest, HttpCompletionOption.ResponseContentRead);

            await aboveLimitResponseTask.WaitAsync(TestHelper.PassingTestTimeout);
            await VerifyResponse(aboveLimitResponseTask, $"{payloadText}-{maxActiveStreamsLimit + 1}");

            delaySource.SetResult(true);

            await Task.WhenAll(requests.Select(r => r.task).ToArray()).WaitAsync(TimeSpan.FromSeconds(15));

            foreach ((Task<HttpResponseMessage> task, DelayedStream stream) in requests)
            {
                Assert.True(task.IsCompleted);
                HttpResponseMessage response = task.Result;
                Assert.True(response.IsSuccessStatusCode);
                Assert.Equal(HttpVersion20.Value, response.Version);
                string responsePayload = await response.Content.ReadAsStringAsync().WaitAsync(TestHelper.PassingTestTimeout);
                Assert.Contains(payloadText, responsePayload);
            }
        }

        [OuterLoop("Uses external service")]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows10Version2004OrGreater))]
        public async Task SendAsync_UseTcpKeepAliveOptions()
        {
            using var handler = new WinHttpHandler()
            {
                TcpKeepAliveEnabled = true,
                TcpKeepAliveTime = TimeSpan.FromSeconds(1),
                TcpKeepAliveInterval = TimeSpan.FromMilliseconds(500)
            };

            using var client = new HttpClient(handler);

            var response = client.GetAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer).Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string responseContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine(responseContent);

            // Uncomment this to observe an exchange of "TCP Keep-Alive" and "TCP Keep-Alive ACK" packets:
            // await Task.Delay(5000);
        }

        private async Task VerifyResponse(Task<HttpResponseMessage> task, string payloadText)
        {
            Assert.True(task.IsCompleted);
            HttpResponseMessage response = task.Result;
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(HttpVersion20.Value, response.Version);
            string responsePayload = await response.Content.ReadAsStringAsync().WaitAsync(TestHelper.PassingTestTimeout);
            Assert.Contains(payloadText, responsePayload);
        }

        public static bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            string pattern = string.Format(@"""{0}"": ""{1}""", key, value);
            return message.Contains(pattern);
        }

        private sealed class DelayedStream : MemoryStream
        {
            private readonly Task _delayTask;
            private readonly TaskCompletionSource<bool> _waitReadSource = new TaskCompletionSource<bool>();
            private bool _delayed;

            public Task WaitRead => _waitReadSource.Task;

            public DelayedStream(byte[] buffer, Task delayTask)
                : base(buffer)
            {
                _delayTask = delayTask;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (!_delayed)
                {
                    _waitReadSource.SetResult(true);
                    _delayTask.Wait();
                    _delayed = true;
                }
                return base.Read(buffer, offset, count);
            }
        }
    }
}
