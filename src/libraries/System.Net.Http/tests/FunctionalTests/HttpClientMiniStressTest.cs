// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public sealed class SocketsHttpHandler_HttpClientMiniStress_NoVersion : HttpClientMiniStress
    {
        public SocketsHttpHandler_HttpClientMiniStress_NoVersion(ITestOutputHelper output) : base(output) { }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [OuterLoop]
        [InlineData(1000000)]
        public void CreateAndDestroyManyClients(int numClients)
        {
            for (int i = 0; i < numClients; i++)
            {
                CreateHttpClient().Dispose();
            }
        }
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsMsQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientMiniStress_Http3_MsQuic : HttpClientMiniStress
    {
        public SocketsHttpHandler_HttpClientMiniStress_Http3_MsQuic(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
        protected override QuicImplementationProvider UseQuicImplementationProvider => QuicImplementationProviders.MsQuic;
    }

    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsMockQuicSupported))]
    public sealed class SocketsHttpHandler_HttpClientMiniStress_Http3_Mock : HttpClientMiniStress
    {
        public SocketsHttpHandler_HttpClientMiniStress_Http3_Mock(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version30;
        protected override QuicImplementationProvider UseQuicImplementationProvider => QuicImplementationProviders.Mock;
    }

    public sealed class SocketsHttpHandler_HttpClientMiniStress_Http2 : HttpClientMiniStress
    {
        public SocketsHttpHandler_HttpClientMiniStress_Http2(ITestOutputHelper output) : base(output) { }
        protected override Version UseVersion => HttpVersion.Version20;
    }

    public sealed class SocketsHttpHandler_HttpClientMiniStress_Http11 : HttpClientMiniStress
    {
        public SocketsHttpHandler_HttpClientMiniStress_Http11(ITestOutputHelper output) : base(output) { }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [OuterLoop]
        [MemberData(nameof(PostStressOptions))]
        public async Task ManyClients_ManyPosts_Async(int numRequests, int dop, int numBytes)
        {
            (List<HttpHeaderData> headers, string content) = CreateResponse("abcdefghijklmnopqrstuvwxyz");
            await ForCountAsync(numRequests, dop, async i =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    await CreateServerAndPostAsync(client, numBytes, headers, content);
                }
            });
        }

        private async Task CreateServerAndPostAsync(HttpClient client, int numBytes, List<HttpHeaderData> headers, string content)
        {
            string responseText = "HTTP/1.1 200 OK\r\n" + string.Join("\r\n", headers.Select(h => h.Name + ": " + h.Value)) + "\r\n\r\n" + content;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                var content = new ByteArrayContent(new byte[numBytes]);
                Task<HttpResponseMessage> postAsync = client.PostAsync(url, content);

                await server.AcceptConnectionAsync(async connection =>
                {
                    byte[] postData = new byte[numBytes];
                    while (!string.IsNullOrEmpty(await connection.ReadLineAsync().ConfigureAwait(false)));
                    Assert.Equal(numBytes, await connection.ReadBlockAsync(postData, 0, numBytes));

                    await connection.WriteStringAsync(responseText).ConfigureAwait(false);
                    connection.Socket.Shutdown(SocketShutdown.Send);
                });

                (await postAsync.ConfigureAwait(false)).Dispose();
            });
        }
    }

    [Collection(nameof(HttpClientMiniStress))]
    [SkipOnPlatform(TestPlatforms.Browser, "System.Net.Security is not supported on Browser")]
    public abstract class HttpClientMiniStress : HttpClientHandlerTestBase
    {
        public HttpClientMiniStress(ITestOutputHelper output) : base(output) { }

        protected override HttpClient CreateHttpClient() =>
            CreateHttpClient(
                new SocketsHttpHandler()
                {
                    SslOptions = new SslClientAuthenticationOptions()
                    {
                        RemoteCertificateValidationCallback = delegate { return true; },
                    }
                });

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [OuterLoop]
        [MemberData(nameof(GetStressOptions))]
        public void SingleClient_ManyGets_Sync(int numRequests, int dop, HttpCompletionOption completionOption)
        {
            (List<HttpHeaderData> headers, string content) = CreateResponse("abcd");
            using (HttpClient client = CreateHttpClient())
            {
                Parallel.For(0, numRequests, new ParallelOptions { MaxDegreeOfParallelism = dop, TaskScheduler = new ThreadPerTaskScheduler() }, _ =>
                {
                    CreateServerAndGet(client, completionOption, headers, content);
                });
            }
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [OuterLoop]
        [MemberData(nameof(GetStressOptions))]
        public async Task SingleClient_ManyGets_Async(int numRequests, int dop, HttpCompletionOption completionOption)
        {
            (List<HttpHeaderData> headers, string content) = CreateResponse("abcdefghijklmnopqrstuvwxyz");
            using (HttpClient client = CreateHttpClient())
            {
                await ForCountAsync(numRequests, dop, i => CreateServerAndGetAsync(client, completionOption, headers, content));
            }
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [OuterLoop]
        [MemberData(nameof(GetStressOptions))]
        public void ManyClients_ManyGets(int numRequests, int dop, HttpCompletionOption completionOption)
        {
            (List<HttpHeaderData> headers, string content) = CreateResponse("abcdefghijklmnopqrstuvwxyz");
            Parallel.For(0, numRequests, new ParallelOptions { MaxDegreeOfParallelism = dop, TaskScheduler = new ThreadPerTaskScheduler() }, _ =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    CreateServerAndGet(client, completionOption, headers, content);
                }
            });
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [OuterLoop]
        [InlineData(5000)]
        public async Task MakeAndFaultManyRequests(int numRequests)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    Task<string>[] tasks =
                        (from i in Enumerable.Range(0, numRequests)
                         select client.GetStringAsync(url))
                         .ToArray();

                    Assert.All(tasks, t =>
                        Assert.True(t.IsFaulted || t.Status == TaskStatus.WaitingForActivation, $"Unexpected status {t.Status}"));

                    server.Dispose();

                    foreach (Task<string> task in tasks)
                    {
                        await Assert.ThrowsAnyAsync<HttpRequestException>(() => task);
                    }
                }
            }, new LoopbackServer.Options { ListenBacklog = numRequests });
        }

        public static IEnumerable<object[]> GetStressOptions()
        {
            foreach (int numRequests in new[] { 5000 }) // number of requests
                foreach (int dop in new[] { 1, 32 }) // number of threads
                    foreach (var completionoption in new[] { HttpCompletionOption.ResponseContentRead, HttpCompletionOption.ResponseHeadersRead })
                        yield return new object[] { numRequests, dop, completionoption };
        }

        private void CreateServerAndGet(HttpClient client, HttpCompletionOption completionOption, List<HttpHeaderData> headers, string content)
        {
            LoopbackServerFactory.CreateServerAsync((server, url) =>
            {
                Task<HttpResponseMessage> getAsync = client.GetAsync(url, completionOption);

                new Task[] {
                    getAsync,
                    server.HandleRequestAsync(HttpStatusCode.OK, headers, content)
                }.WhenAllOrAnyFailed().GetAwaiter().GetResult();

                getAsync.Result.Dispose();
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();
        }

        private async Task CreateServerAndGetAsync(HttpClient client, HttpCompletionOption completionOption, List<HttpHeaderData> headers, string content)
        {
            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                Task<HttpResponseMessage> getAsync = client.GetAsync(url, completionOption);

                await new Task[]
                {
                    getAsync,
                    server.HandleRequestAsync(HttpStatusCode.OK, headers, content),
                }.WhenAllOrAnyFailed().ConfigureAwait(false);

                getAsync.Result.Dispose();
            });
        }

        public static IEnumerable<object[]> PostStressOptions()
        {
            foreach (int numRequests in new[] { 5000 }) // number of requests
                foreach (int dop in new[] { 1, 32 }) // number of threads
                    foreach (int numBytes in new[] { 0, 100 }) // number of bytes to post
                        yield return new object[] { numRequests, dop, numBytes };
        }

        [ConditionalFact(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [OuterLoop]
        public async Task UnreadResponseMessage_Collectible()
        {
            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Func<Task<WeakReference>> getAsync = () => client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ContinueWith(t => new WeakReference(t.Result));
                    Task<WeakReference> wrt = getAsync();

                    await server.HandleRequestAsync(content: new string('a', 16 * 1024));

                    WeakReference wr = wrt.GetAwaiter().GetResult();
                    Assert.True(SpinWait.SpinUntil(() =>
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        return !wr.IsAlive;
                    }, 10 * 1000), "Response object should have been collected");
                }
            });
        }

        protected (List<HttpHeaderData> Headers, string Content) CreateResponse(string asciiBody)
        {
            var headers = new List<HttpHeaderData>();
            if (UseVersion.Major < 2)
            {
                headers.Add(new HttpHeaderData("Content-Length", asciiBody.Length.ToString()));
            }
            return (headers, asciiBody);
        }

        protected static Task ForCountAsync(int count, int dop, Func<int, Task> bodyAsync)
        {
            var sched = new ThreadPerTaskScheduler();
            int nextAvailableIndex = 0;
            return Task.WhenAll(Enumerable.Range(0, dop).Select(_ => Task.Factory.StartNew(async delegate
            {
                int index;
                while ((index = Interlocked.Increment(ref nextAvailableIndex) - 1) < count)
                {
                    try { await bodyAsync(index); }
                    catch
                    {
                        Volatile.Write(ref nextAvailableIndex, count); // avoid any further iterations
                        throw;
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.None, sched).Unwrap()));
        }

        private sealed class ThreadPerTaskScheduler : TaskScheduler
        {
            protected override void QueueTask(Task task) =>
                Task.Factory.StartNew(() => TryExecuteTask(task), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => TryExecuteTask(task);

            protected override IEnumerable<Task> GetScheduledTasks() => null;
        }
    }
}
