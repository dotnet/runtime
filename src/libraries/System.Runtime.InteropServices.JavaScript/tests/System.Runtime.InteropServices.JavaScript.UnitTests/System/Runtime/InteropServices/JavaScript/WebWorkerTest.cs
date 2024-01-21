// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Xunit;
using System.IO;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Linq;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    // TODO test:
    // JSExport 2x
    // JSExport async
    // lock
    // thread allocation, many threads
    // ProxyContext flow, child thread, child task
    // use JSObject after JSWebWorker finished, especially HTTP
    // WS on JSWebWorker
    // HTTP continue on TP
    // event pipe
    // FS
    // JS setTimeout till after JSWebWorker close
    // synchronous .Wait for JS setTimeout on the same thread -> deadlock problem **7)**

    public class WebWorkerTest : IAsyncLifetime
    {
        const int TimeoutMilliseconds = 5000;

        public static bool _isWarmupDone;

        public async Task InitializeAsync()
        {
            if (_isWarmupDone)
            {
                return;
            }
            await Task.Delay(500);
            _isWarmupDone = true;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        #region Executors

        private CancellationTokenSource CreateTestCaseTimeoutSource()
        {
            var cts = new CancellationTokenSource(TimeoutMilliseconds);
            cts.Token.Register(() =>
            {
                Console.WriteLine($"Unexpected test case timeout at {DateTime.Now.ToString("u")} ManagedThreadId:{Environment.CurrentManagedThreadId}");
            });
            return cts;
        }

        public static IEnumerable<object[]> GetTargetThreads()
        {
            return Enum.GetValues<ExecutorType>().Select(type => new object[] { new Executor(type) });
        }

        public static IEnumerable<object[]> GetSpecificTargetThreads()
        {
            yield return new object[] { new Executor(ExecutorType.JSWebWorker), new Executor(ExecutorType.Main) };
            yield break;
        }

        public static IEnumerable<object[]> GetTargetThreads2x()
        {
            return Enum.GetValues<ExecutorType>().SelectMany(
                type1 => Enum.GetValues<ExecutorType>().Select(
                    type2 => new object[] { new Executor(type1), new Executor(type2) }));
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task Executor_Cancellation(Executor executor)
        {
            var cts = new CancellationTokenSource();

            TaskCompletionSource ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceledTask = executor.Execute(() =>
            {
                TaskCompletionSource never = new TaskCompletionSource();
                ready.SetResult();
                return never.Task;
            }, cts.Token);

            await ready.Task;

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSDelay_Cancellation(Executor executor)
        {
            var cts = new CancellationTokenSource();
            TaskCompletionSource ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceledTask = executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.CreateDelay(), cts.Token);

                var never = WebWorkerTestHelper.JSDelay(int.MaxValue);
                ready.SetResult();
                await never;
            }, cts.Token);

            await ready.Task;

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledTask);
        }

        [Fact]
        public async Task JSSynchronizationContext_Send_Post_Items_Cancellation()
        {
            var cts = new CancellationTokenSource();

            ManualResetEventSlim blocker = new ManualResetEventSlim(false);
            TaskCompletionSource never = new TaskCompletionSource();
            SynchronizationContext capturedSynchronizationContext = null;
            TaskCompletionSource jswReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource sendReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource postReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceledTask = JSWebWorker.RunAsync(() =>
            {
                capturedSynchronizationContext = SynchronizationContext.Current;
                jswReady.SetResult();

                // blocking the worker, so that JSSynchronizationContext could enqueue next tasks
                blocker.Wait();

                return never.Task;
            }, cts.Token);

            await jswReady.Task;
            Assert.Equal("System.Runtime.InteropServices.JavaScript.JSSynchronizationContext", capturedSynchronizationContext!.GetType().FullName);

            var shouldNotHitSend = false;
            var shouldNotHitPost = false;
            var hitAfterPost = false;

            var canceledSend = Task.Run(() =>
            {
                // this will be blocked until blocker.Set()
                sendReady.SetResult();
                capturedSynchronizationContext.Send(_ =>
                {
                    // then it should get canceled and not executed
                    shouldNotHitSend = true;
                }, null);
                return Task.CompletedTask;
            });

            var canceledPost = Task.Run(() =>
            {
                postReady.SetResult();
                capturedSynchronizationContext.Post(_ =>
                {
                    // then it should get canceled and not executed
                    shouldNotHitPost = true;
                }, null);
                hitAfterPost = true;
                return Task.CompletedTask;
            });

            // make sure that jobs got the chance to enqueue
            await sendReady.Task;
            await postReady.Task;
            await Task.Delay(100);

            // this could should be delivered immediately
            cts.Cancel();

            // this will unblock the current pending work item
            blocker.Set();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledSend);
            await canceledPost; // this shouldn't throw

            Assert.False(shouldNotHitSend);
            Assert.False(shouldNotHitPost);
            Assert.True(hitAfterPost);
        }

        [Fact]
        public async Task JSSynchronizationContext_Send_Post_To_Canceled()
        {
            var cts = new CancellationTokenSource();

            TaskCompletionSource never = new TaskCompletionSource();
            SynchronizationContext capturedSynchronizationContext = null;
            TaskCompletionSource jswReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            JSObject capturedGlobalThis = null;

            var canceledTask = JSWebWorker.RunAsync(() =>
            {
                capturedSynchronizationContext = SynchronizationContext.Current;
                capturedGlobalThis = JSHost.GlobalThis;
                jswReady.SetResult();
                return never.Task;
            }, cts.Token);

            await jswReady.Task;
            Assert.Equal("System.Runtime.InteropServices.JavaScript.JSSynchronizationContext", capturedSynchronizationContext!.GetType().FullName);

            cts.Cancel();

            // give it chance to dispose the thread
            await Task.Delay(100);

            Assert.True(capturedGlobalThis.IsDisposed);

            var shouldNotHitSend = false;
            var shouldNotHitPost = false;

            Assert.Throws<ObjectDisposedException>(() =>
            {
                capturedGlobalThis.HasProperty("document");
            });

            Assert.Throws<ObjectDisposedException>(() =>
            {
                capturedSynchronizationContext.Send(_ =>
                {
                    // then it should get canceled and not executed
                    shouldNotHitSend = true;
                }, null);
            });

            Assert.Throws<ObjectDisposedException>(() =>
            {
                capturedSynchronizationContext.Post(_ =>
                {
                    // then it should get canceled and not executed
                    shouldNotHitPost = true;
                }, null);
            });

            Assert.False(shouldNotHitSend);
            Assert.False(shouldNotHitPost);
        }

        [Fact]
        public async Task JSWebWorker_Abandon_Running()
        {
            var cts = new CancellationTokenSource();

            TaskCompletionSource never = new TaskCompletionSource();
            TaskCompletionSource ready = new TaskCompletionSource();

#pragma warning disable CS4014
            // intentionally not awaiting it
            JSWebWorker.RunAsync(() =>
            {
                ready.SetResult();
                return never.Task;
            }, cts.Token);
#pragma warning restore CS4014

            await ready.Task;

            // it should not get collected
            GC.Collect();

            // it should not prevent mono and the test suite from exiting
        }

        [Fact]
        public async Task JSWebWorker_Abandon_Running_JS()
        {
            var cts = new CancellationTokenSource();

            TaskCompletionSource ready = new TaskCompletionSource();

#pragma warning disable CS4014
            // intentionally not awaiting it
            JSWebWorker.RunAsync(async () =>
            {
                await WebWorkerTestHelper.CreateDelay();
                var never = WebWorkerTestHelper.JSDelay(int.MaxValue);
                ready.SetResult();
                await never;
            }, cts.Token);
#pragma warning restore CS4014

            await ready.Task;

            // it should not get collected
            GC.Collect();

            // it should not prevent mono and the test suite from exiting
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task Executor_Propagates(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            bool hit = false;
            var failedTask = executor.Execute(() =>
            {
                hit = true;
                throw new InvalidOperationException("Test");
            }, cts.Token);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => failedTask);
            Assert.True(hit);
            Assert.Equal("Test", ex.Message);
        }

        #endregion

        #region Console, Yield, Delay, Timer

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedConsole(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(() =>
            {
                Console.WriteLine("C# Hello from ManagedThreadId: " + Environment.CurrentManagedThreadId);
                return Task.CompletedTask;
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSConsole(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(() =>
            {
                WebWorkerTestHelper.Log("JS Hello from ManagedThreadId: " + Environment.CurrentManagedThreadId + " NativeThreadId: " + WebWorkerTestHelper.NativeThreadId);
                return Task.CompletedTask;
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task NativeThreadId(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.InitializeAsync(), cts.Token);

                var jsTid = WebWorkerTestHelper.GetTid();
                var csTid = WebWorkerTestHelper.NativeThreadId;
                if (executor.Type == ExecutorType.Main || executor.Type == ExecutorType.JSWebWorker)
                {
                    Assert.Equal(jsTid, csTid);
                }
                else
                {
                    Assert.NotEqual(jsTid, csTid);
                }

                await WebWorkerTestHelper.DisposeAsync();
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ThreadingTimer(Executor executor)
        {
            var hit = false;
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                TaskCompletionSource tcs = new TaskCompletionSource();

                using var timer = new Timer(_ =>
                {
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                    tcs.SetResult();
                    hit = true;
                }, null, 100, Timeout.Infinite);

                await tcs.Task;
            }, cts.Token);

            Assert.True(hit);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSDelay_ContinueWith(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.CreateDelay(), cts.Token);

                await WebWorkerTestHelper.JSDelay(10).ContinueWith(_ =>
                {
                    // continue on the context of the target JS interop
                    executor.AssertInteropThread();
                }, TaskContinuationOptions.ExecuteSynchronously);
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSDelay_ConfigureAwait_True(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.CreateDelay(), cts.Token);

                await WebWorkerTestHelper.JSDelay(10).ConfigureAwait(true);

                executor.AssertAwaitCapturedContext();
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay_ContinueWith(Executor executor)
        {
            var hit = false;
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await Task.Delay(10, cts.Token).ContinueWith(_ =>
                {
                    hit = true;
                }, TaskContinuationOptions.ExecuteSynchronously);
            }, cts.Token);
            Assert.True(hit);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay_ConfigureAwait_True(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await Task.Delay(10, cts.Token).ConfigureAwait(true);

                executor.AssertAwaitCapturedContext();
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedYield(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await Task.Yield();

                executor.AssertAwaitCapturedContext();
            }, cts.Token);
        }

        #endregion

        #region Thread Affinity

        private async Task ActionsInDifferentThreads<T>(Executor executor1, Executor executor2, Func<Task, TaskCompletionSource<T>, Task> e1Job, Func<T, Task> e2Job, CancellationTokenSource cts)
        {
            TaskCompletionSource<T> readyTCS = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource doneTCS = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var e1 = executor1.Execute(async () =>
            {
                await e1Job(doneTCS.Task, readyTCS);
                if (!readyTCS.Task.IsCompleted)
                {
                    readyTCS.SetResult(default);
                }
                await doneTCS.Task;
            }, cts.Token);

            var r1 = await readyTCS.Task.ConfigureAwait(true);

            var e2 = executor2.Execute(async () =>
            {
                await e2Job(r1);

            }, cts.Token);

            try
            {
                await e2;
                doneTCS.SetResult();
                await e1;
            }
            catch (Exception)
            {
                cts.Cancel();
                throw;
            }
        }

        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public async Task JSObject_CapturesAffinity(Executor executor1, Executor executor2)
        {
            var cts = CreateTestCaseTimeoutSource();

            var e1Job = async (Task e2done, TaskCompletionSource<JSObject> e1State) =>
            {
                await WebWorkerTestHelper.InitializeAsync();

                executor1.AssertAwaitCapturedContext();

                var jsState = await WebWorkerTestHelper.PromiseState();

                // share the state with the E2 continuation
                e1State.SetResult(jsState);

                await e2done;

                // cleanup
                await WebWorkerTestHelper.DisposeAsync();
            };

            var e2Job = async (JSObject e1State) =>
            {
                bool valid = await WebWorkerTestHelper.PromiseValidateState(e1State);
                Assert.True(valid);
            };

            await ActionsInDifferentThreads<JSObject>(executor1, executor2, e1Job, e2Job, cts);
        }

        #endregion

        #region WebSocket

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task WebSocketClient_ContentInSameThread(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();

            var uri = new Uri(WebWorkerTestHelper.LocalWsEcho + "?guid=" + Guid.NewGuid());
            var message = "hello";
            var send = Encoding.UTF8.GetBytes(message);
            var receive = new byte[100];

            await executor.Execute(async () =>
            {
                using var client = new ClientWebSocket();
                await client.ConnectAsync(uri, CancellationToken.None);
                await client.SendAsync(send, WebSocketMessageType.Text, true, CancellationToken.None);

                var res = await client.ReceiveAsync(receive, CancellationToken.None);
                Assert.Equal(WebSocketMessageType.Text, res.MessageType);
                Assert.True(res.EndOfMessage);
                Assert.Equal(send.Length, res.Count);
                Assert.Equal(message, Encoding.UTF8.GetString(receive, 0, res.Count));
            }, cts.Token);
        }


        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public Task WebSocketClient_ResponseCloseInDifferentThread(Executor executor1, Executor executor2)
        {
            var cts = CreateTestCaseTimeoutSource();

            var uri = new Uri(WebWorkerTestHelper.LocalWsEcho + "?guid=" + Guid.NewGuid());
            var message = "hello";
            var send = Encoding.UTF8.GetBytes(message);
            var receive = new byte[100];

            var e1Job = async (Task e2done, TaskCompletionSource<ClientWebSocket> e1State) =>
            {
                using var client = new ClientWebSocket();
                await client.ConnectAsync(uri, CancellationToken.None);
                await client.SendAsync(send, WebSocketMessageType.Text, true, CancellationToken.None);

                // share the state with the E2 continuation
                e1State.SetResult(client);
                await e2done;
            };

            var e2Job = async (ClientWebSocket client) =>
            {
                var res = await client.ReceiveAsync(receive, CancellationToken.None);
                Assert.Equal(WebSocketMessageType.Text, res.MessageType);
                Assert.True(res.EndOfMessage);
                Assert.Equal(send.Length, res.Count);
                Assert.Equal(message, Encoding.UTF8.GetString(receive, 0, res.Count));

                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            };

            return ActionsInDifferentThreads<ClientWebSocket>(executor1, executor2, e1Job, e2Job, cts);
        }

        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public Task WebSocketClient_CancelInDifferentThread(Executor executor1, Executor executor2)
        {
            var cts = new CancellationTokenSource();

            var uri = new Uri(WebWorkerTestHelper.LocalWsEcho + "?guid=" + Guid.NewGuid());
            var message = ".delay5sec"; // this will make the loopback server slower
            var send = Encoding.UTF8.GetBytes(message);
            var receive = new byte[100];

            var e1Job = async (Task e2done, TaskCompletionSource<ClientWebSocket> e1State) =>
            {
                using var client = new ClientWebSocket();
                await client.ConnectAsync(uri, CancellationToken.None);
                await client.SendAsync(send, WebSocketMessageType.Text, true, CancellationToken.None);

                // share the state with the E2 continuation
                e1State.SetResult(client);
                await e2done;
            };

            var e2Job = async (ClientWebSocket client) =>
            {
                CancellationTokenSource cts2 = new CancellationTokenSource();
                var resTask = client.ReceiveAsync(receive, cts2.Token);
                cts2.Cancel();
                var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resTask);
                Assert.Equal(cts2.Token, ex.CancellationToken);
            };

            return ActionsInDifferentThreads<ClientWebSocket>(executor1, executor2, e1Job, e2Job, cts);
        }

        #endregion

        #region HTTP

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task HttpClient_ContentInSameThread(Executor executor)
        {
            var cts = CreateTestCaseTimeoutSource();
            var uri = WebWorkerTestHelper.GetOriginUrl() + "/_framework/blazor.boot.json";

            await executor.Execute(async () =>
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Assert.StartsWith("{", body);
            }, cts.Token);
        }

        private static HttpRequestOptionsKey<bool> WebAssemblyEnableStreamingRequestKey = new("WebAssemblyEnableStreamingRequest");
        private static HttpRequestOptionsKey<bool> WebAssemblyEnableStreamingResponseKey = new("WebAssemblyEnableStreamingResponse");
        private static string HelloJson = "{'hello':'world'}".Replace('\'', '"');
        private static string EchoStart = "{\"Method\":\"POST\",\"Url\":\"/Echo.ashx";

        private Task HttpClient_ActionInDifferentThread(string url, Executor executor1, Executor executor2, Func<HttpResponseMessage, Task> e2Job)
        {
            var cts = CreateTestCaseTimeoutSource();

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
            return ActionsInDifferentThreads<HttpResponseMessage>(executor1, executor2, e1Job, e2Job, cts);
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
