// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientMiniStress : HttpClientHandlerTestBase
    {
        public HttpClientMiniStress(ITestOutputHelper output) : base(output) { }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [MemberData(nameof(GetStressOptions))]
        public void SingleClient_ManyGets_Sync(int numRequests, int dop, HttpCompletionOption completionOption)
        {
            string responseText = CreateResponse("abcdefghijklmnopqrstuvwxyz");
            using (HttpClient client = CreateHttpClient())
            {
                Parallel.For(0, numRequests, new ParallelOptions { MaxDegreeOfParallelism = dop, TaskScheduler = new ThreadPerTaskScheduler() }, _ =>
                {
                    CreateServerAndGet(client, completionOption, responseText);
                });
            }
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        public async Task SingleClient_ManyGets_Async(int numRequests, int dop, HttpCompletionOption completionOption)
        {
            string responseText = CreateResponse("abcdefghijklmnopqrstuvwxyz");
            using (HttpClient client = CreateHttpClient())
            {
                await ForCountAsync(numRequests, dop, i => CreateServerAndGetAsync(client, completionOption, responseText));
            }
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [MemberData(nameof(GetStressOptions))]
        public void ManyClients_ManyGets(int numRequests, int dop, HttpCompletionOption completionOption)
        {
            string responseText = CreateResponse("abcdefghijklmnopqrstuvwxyz");
            Parallel.For(0, numRequests, new ParallelOptions { MaxDegreeOfParallelism = dop, TaskScheduler = new ThreadPerTaskScheduler() }, _ =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    CreateServerAndGet(client, completionOption, responseText);
                }
            });
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [MemberData(nameof(PostStressOptions))]
        public async Task ManyClients_ManyPosts_Async(int numRequests, int dop, int numBytes)
        {
            string responseText = CreateResponse("");
            await ForCountAsync(numRequests, dop, async i =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    await CreateServerAndPostAsync(client, numBytes, responseText);
                }
            });
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        [InlineData(1000000)]
        public void CreateAndDestroyManyClients(int numClients)
        {
            for (int i = 0; i < numClients; i++)
            {
                CreateHttpClient().Dispose();
            }
        }

        [ConditionalTheory(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
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

        private static void CreateServerAndGet(HttpClient client, HttpCompletionOption completionOption, string responseText)
        {
            LoopbackServer.CreateServerAsync((server, url) =>
            {
                Task<HttpResponseMessage> getAsync = client.GetAsync(url, completionOption);

                server.AcceptConnectionAsync(connection => 
                {
                    while (!string.IsNullOrEmpty(connection.ReadLine())) ;

                    connection.Writer.Write(responseText);
                    connection.Socket.Shutdown(SocketShutdown.Send);

                    return Task.CompletedTask;
                }).GetAwaiter().GetResult();

                getAsync.GetAwaiter().GetResult().Dispose();
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();
        }

        private static async Task CreateServerAndGetAsync(HttpClient client, HttpCompletionOption completionOption, string responseText)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                Task<HttpResponseMessage> getAsync = client.GetAsync(url, completionOption);

                await server.AcceptConnectionAsync(async connection => 
                {
                    while (!string.IsNullOrEmpty(await connection.ReadLineAsync().ConfigureAwait(false))) ;

                    await connection.Writer.WriteAsync(responseText).ConfigureAwait(false);
                    connection.Socket.Shutdown(SocketShutdown.Send);
                });

                (await getAsync.ConfigureAwait(false)).Dispose();
            });
        }

        public static IEnumerable<object[]> PostStressOptions()
        {
            foreach (int numRequests in new[] { 5000 }) // number of requests
                foreach (int dop in new[] { 1, 32 }) // number of threads
                    foreach (int numBytes in new[] { 0, 100 }) // number of bytes to post
                        yield return new object[] { numRequests, dop, numBytes };
        }

        private static async Task CreateServerAndPostAsync(HttpClient client, int numBytes, string responseText)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                var content = new ByteArrayContent(new byte[numBytes]);
                Task<HttpResponseMessage> postAsync = client.PostAsync(url, content);

                await server.AcceptConnectionAsync(async connection => 
                {
                    byte[] postData = new byte[numBytes];
                    while (!string.IsNullOrEmpty(await connection.ReadLineAsync().ConfigureAwait(false))) ;
                    Assert.Equal(numBytes, await connection.ReadBlockAsync(postData, 0, numBytes));

                    await connection.Writer.WriteAsync(responseText).ConfigureAwait(false);
                    connection.Socket.Shutdown(SocketShutdown.Send);
                });

                (await postAsync.ConfigureAwait(false)).Dispose();
            });
        }

        [ConditionalFact(typeof(TestEnvironment), nameof(TestEnvironment.IsStressModeEnabled))]
        public async Task UnreadResponseMessage_Collectible()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Func<Task<WeakReference>> getAsync = () => client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ContinueWith(t => new WeakReference(t.Result));
                    Task<WeakReference> wrt = getAsync();

                    await server.AcceptConnectionAsync(async connection =>
                    {
                        while (!string.IsNullOrEmpty(await connection.ReadLineAsync())) ;
                        await connection.Writer.WriteAsync(CreateResponse(new string('a', 32 * 1024)));

                        WeakReference wr = wrt.GetAwaiter().GetResult();
                        Assert.True(SpinWait.SpinUntil(() =>
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                            return !wr.IsAlive;
                        }, 10 * 1000), "Response object should have been collected");
                    });
                }
            });
        }

        private static string CreateResponse(string asciiBody) =>
            $"HTTP/1.1 200 OK\r\n" +
            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
            "Content-Type: text/plain\r\n" +
            $"Content-Length: {asciiBody.Length}\r\n" +
            "\r\n" +
            $"{asciiBody}";

        private static Task ForCountAsync(int count, int dop, Func<int, Task> bodyAsync)
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
