// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

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

    public class WebWorkerTest : WebWorkerTestBase
    {
        #region Executors

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
            TaskCompletionSource canceled = new TaskCompletionSource();
            SynchronizationContext capturedSynchronizationContext = null;
            TaskCompletionSource jswReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource sendReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource postReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceledTask = JSWebWorker.RunAsync(() =>
            {
                capturedSynchronizationContext = SynchronizationContext.Current;
                jswReady.SetResult();

                // blocking the worker, so that JSSynchronizationContext could enqueue next tasks
                Thread.ForceBlockingWait(static (b) => ((ManualResetEventSlim)b).Wait(), blocker);

                return never.Task;
            }, cts.Token);

            await jswReady.Task;
            Assert.Equal("System.Runtime.InteropServices.JavaScript.JSSynchronizationContext", capturedSynchronizationContext!.GetType().FullName);

            var shouldNotHitSend = false;
            var shouldNotHitPost = false;
            var hitAfterPost = false;
            var hitAfterSend = false;

            var canceledSend = Task.Run(() =>
            {
                sendReady.SetResult();
                // this will be blocked until blocker.Set()
                try
                {
                    capturedSynchronizationContext.Send(_ =>
                    {
                        // then it should get canceled and not executed
                        shouldNotHitSend = true;
                    }, null);
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
                hitAfterSend = true;
                return Task.FromException(new Exception("Should be unreachable"));
            });

            var canceledPost = Task.Run(() =>
            {
                try
                {
                    capturedSynchronizationContext.Post(_ =>
                    {
                        // then it should get canceled and not executed
                        shouldNotHitPost = true;
                    }, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unexpected exception " + ex);
                    postReady.SetException(ex);
                    return Task.FromException(ex);
                }
                hitAfterPost = true;
                postReady.SetResult();
                return Task.CompletedTask;
            });

            // make sure that jobs got the chance to enqueue
            await postReady.Task;
            await sendReady.Task;
            await Task.Delay(200); // make sure that 

            // this could should be delivered immediately
            cts.Cancel();

            // now we release helpers to use capturedSynchronizationContext
            canceled.SetResult();

            // this will unblock the current pending work item
            blocker.Set();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledSend);
            await canceledPost; // this shouldn't throw

            Assert.False(shouldNotHitSend);
            Assert.False(shouldNotHitPost);
            Assert.True(hitAfterPost);
            Assert.False(hitAfterSend);
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/96628#issuecomment-1907602744")]
        [Fact]
        // this will say something like `JSSynchronizationContext is still installed on worker 0x4ff0030.` in the console during shutdown.
        public async Task JSWebWorker_Abandon_Running()
        {
            TaskCompletionSource never = new TaskCompletionSource();
            TaskCompletionSource ready = new TaskCompletionSource();

#pragma warning disable CS4014
            // intentionally not awaiting it
            JSWebWorker.RunAsync(() =>
            {
                ready.SetResult();
                return never.Task;
            }, CancellationToken.None);
#pragma warning restore CS4014

            await ready.Task;

            // it should not get collected
            GC.Collect();

            // it should not prevent mono and the test suite from exiting
        }

        [Fact]
        // this will say something like `JSSynchronizationContext is still installed on worker 0x4ff0030.` in the console during shutdown.
        public async Task JSWebWorker_Abandon_Running_JS()
        {
            TaskCompletionSource ready = new TaskCompletionSource();

#pragma warning disable CS4014
            // intentionally not awaiting it
            JSWebWorker.RunAsync(async () =>
            {
                await WebWorkerTestHelper.CreateDelay();
                var never = WebWorkerTestHelper.JSDelay(int.MaxValue);
                ready.SetResult();
                await never;
            }, CancellationToken.None);
#pragma warning restore CS4014

            await ready.Task;

            // it should not get collected
            GC.Collect();

            // it should not prevent mono and the test suite from exiting
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task Executor_Propagates(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
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

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task Executor_Propagates_After_Delay(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
            bool hit = false;
            var failedTask = executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.CreateDelay(), cts.Token);
                await WebWorkerTestHelper.JSDelay(10);

                hit = true;
                throw new InvalidOperationException("Test");
            }, cts.Token);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await failedTask);
            Assert.True(hit);
            Assert.Equal("Test", ex.Message);
        }

        #endregion

        #region Console, Yield, Delay, Timer

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedConsole(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(() =>
            {
                Console.WriteLine("C# Hello from ManagedThreadId: " + Environment.CurrentManagedThreadId);
                Console.Clear();
                return Task.CompletedTask;
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSConsole(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(() =>
            {
                WebWorkerTestHelper.Log("JS Hello from ManagedThreadId: " + Environment.CurrentManagedThreadId + " NativeThreadId: " + WebWorkerTestHelper.NativeThreadId);
                return Task.CompletedTask;
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task NativeThreadId(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
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
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                TaskCompletionSource tcs = new TaskCompletionSource();

                await ForceBlockingWaitAsync(async () =>
                {

                    using var timer = new Timer(_ =>
                    {
                        Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                        Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                        tcs.SetResult();
                        hit = true;
                    }, null, 100, Timeout.Infinite);

                    await tcs.Task;
                });
            }, cts.Token);

            Assert.True(hit);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSDelay_ContinueWith(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
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
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.CreateDelay(), cts.Token);

                await WebWorkerTestHelper.JSDelay(10).ConfigureAwait(true);

                executor.AssertAwaitCapturedContext();
            }, cts.Token);
        }

        static async Task ForceBlockingWaitAsync(Func<Task> action)
        {
            var flag = Thread.ThrowOnBlockingWaitOnJSInteropThread;
            try
            {
                Thread.ThrowOnBlockingWaitOnJSInteropThread = false;

                await action();
            }
            finally
            {
                Thread.ThrowOnBlockingWaitOnJSInteropThread = flag;
            }
        }


        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay_ContinueWith(Executor executor)
        {
            var hit = false;
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await ForceBlockingWaitAsync(async () =>
                {
                    await Task.Delay(10, cts.Token).ContinueWith(_ =>
                    {
                        hit = true;
                    }, TaskContinuationOptions.ExecuteSynchronously);
                });
            }, cts.Token);
            Assert.True(hit);
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay_ConfigureAwait_True(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await ForceBlockingWaitAsync(async () =>
                {
                    await Task.Delay(10, cts.Token).ConfigureAwait(true);
                });

                executor.AssertAwaitCapturedContext();
            }, cts.Token);
        }

        [Theory, MemberData(nameof(GetTargetThreadsAndBlockingCalls))]
        public async Task WaitAssertsOnJSInteropThreads(Executor executor, NamedCall method)
        {
            CancellationTokenSource? cts = null;
            try
            {
                Thread.ForceBlockingWait((_) => cts = CreateTestCaseTimeoutSource(), null);
                await executor.Execute(Task () =>
                {
                    Exception? exception = null;
                    try
                    {
                        method.Call(cts.Token);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    executor.AssertBlockingWait(exception);

                    return Task.CompletedTask;
                }, cts.Token);
            }
            finally
            {
                cts?.Dispose();
            }
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedYield(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await Task.Yield();

                executor.AssertAwaitCapturedContext();
            }, cts.Token);
        }

        #endregion

        #region Thread Affinity

        [Theory, MemberData(nameof(GetTargetThreads2x))]
        public async Task JSObject_CapturesAffinity(Executor executor1, Executor executor2)
        {
            using var cts = CreateTestCaseTimeoutSource();

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

    }
}
