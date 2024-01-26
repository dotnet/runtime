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
using System.Runtime.CompilerServices;

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

        protected CancellationTokenSource CreateTestCaseTimeoutSource([CallerMemberName] string memberName = "")
        {
            var start = DateTime.Now;
            var cts = new CancellationTokenSource(TimeoutMilliseconds);
            cts.Token.Register(() =>
            {
                var end = DateTime.Now;
                Console.WriteLine($"Unexpected test case {memberName} timeout after {end - start} ManagedThreadId:{Environment.CurrentManagedThreadId}");
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

        #endregion

        #region Console, Yield, Delay, Timer

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedConsole(Executor executor)
        {
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(() =>
            {
                Console.WriteLine("C# Hello from ManagedThreadId: " + Environment.CurrentManagedThreadId);
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

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay_ContinueWith(Executor executor)
        {
            var hit = false;
            using var cts = CreateTestCaseTimeoutSource();
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
            using var cts = CreateTestCaseTimeoutSource();
            await executor.Execute(async () =>
            {
                await Task.Delay(10, cts.Token).ConfigureAwait(true);

                executor.AssertAwaitCapturedContext();
            }, cts.Token);
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

        protected async Task ActionsInDifferentThreads<T>(Executor executor1, Executor executor2, Func<Task, TaskCompletionSource<T>, Task> e1Job, Func<T, Task> e2Job, CancellationTokenSource cts)
        {
            TaskCompletionSource<T> job1ReadyTCS = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource job2DoneTCS = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var e1Done = false;
            var e2Done = false;
            var e1Failed = false;
            Task e1;
            Task e2;
            T r1;

            async Task ActionsInDifferentThreads1()
            {
                try
                {
                    await e1Job(job2DoneTCS.Task, job1ReadyTCS);
                    if (!job1ReadyTCS.Task.IsCompleted)
                    {
                        job1ReadyTCS.SetResult(default);
                    }
                    await job2DoneTCS.Task;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ActionsInDifferentThreads1 failed\n" + ex);
                    job1ReadyTCS.SetResult(default);
                    e1Failed = true;
                    throw;
                }
                finally
                {
                    e1Done = true;
                }
            }

            async Task ActionsInDifferentThreads2()
            {
                try
                {
                    await e2Job(r1);
                }
                finally
                {
                    e2Done = true;
                }
            }


            e1 = executor1.Execute(ActionsInDifferentThreads1, cts.Token);
            r1 = await job1ReadyTCS.Task.ConfigureAwait(true);
            if (e1Failed || e1.IsFaulted)
            {
                await e1;
            }
            e2 = executor2.Execute(ActionsInDifferentThreads2, cts.Token);

            try
            {
                await e2;
                job2DoneTCS.SetResult();
                await e1;
            }
            catch (Exception ex)
            {
                job2DoneTCS.TrySetException(ex);
                if (ex is OperationCanceledException oce && cts.Token.IsCancellationRequested)
                {
                    throw;
                }
                Console.WriteLine("ActionsInDifferentThreads failed with: \n" + ex);
                if (!e1Done || !e2Done)
                {
                    Console.WriteLine("ActionsInDifferentThreads canceling!");
                    cts.Cancel();
                }
                throw;
            }
        }

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
