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

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    // TODO test:
    // JSWebWorker.RunAsync with CancellationToken
    // JSExport 2x
    // JSExport async
    // timer
    // GC + finalizer
    // lock
    // thread allocation, many threads
    // TLS
    // ProxyContext flow, child thread, child task
    // use JSObject after JSWebWorker finished
    // JSWebWorker JS setTimeout till after close
    // WS on JSWebWorker
    // Yield will hit event loop 3x
    // HTTP continue on TP
    // WS continue on TP
    // event pipe
    // FS

    public class WebWorkerTest
    {
        #region executor threads

        public static IEnumerable<object[]> GetTargetThreads()
        {
            yield return new object[] { new Executor(ExecutorType.Main) };
            yield return new object[] { new Executor(ExecutorType.JSWebWorker) };
            yield return new object[] { new Executor(ExecutorType.ThreadPool) };
            yield return new object[] { new Executor(ExecutorType.NewThread) };
            yield break;
        }

        public static IEnumerable<object[]> GetTargetThreads2x2()
        {
            yield return new object[] { new Executor(ExecutorType.Main), new Executor(ExecutorType.Main) };
            yield return new object[] { new Executor(ExecutorType.JSWebWorker), new Executor(ExecutorType.Main) };
            yield return new object[] { new Executor(ExecutorType.ThreadPool), new Executor(ExecutorType.Main) };
            yield return new object[] { new Executor(ExecutorType.NewThread), new Executor(ExecutorType.Main) };

            yield return new object[] { new Executor(ExecutorType.Main), new Executor(ExecutorType.JSWebWorker) };
            yield return new object[] { new Executor(ExecutorType.JSWebWorker), new Executor(ExecutorType.JSWebWorker) };
            yield return new object[] { new Executor(ExecutorType.ThreadPool), new Executor(ExecutorType.JSWebWorker) };
            yield return new object[] { new Executor(ExecutorType.NewThread), new Executor(ExecutorType.JSWebWorker) };

            yield return new object[] { new Executor(ExecutorType.Main), new Executor(ExecutorType.ThreadPool) };
            yield return new object[] { new Executor(ExecutorType.JSWebWorker), new Executor(ExecutorType.ThreadPool) };
            yield return new object[] { new Executor(ExecutorType.ThreadPool), new Executor(ExecutorType.ThreadPool) };
            yield return new object[] { new Executor(ExecutorType.NewThread), new Executor(ExecutorType.ThreadPool) };

            yield return new object[] { new Executor(ExecutorType.Main), new Executor(ExecutorType.NewThread) };
            yield return new object[] { new Executor(ExecutorType.JSWebWorker), new Executor(ExecutorType.NewThread) };
            yield return new object[] { new Executor(ExecutorType.ThreadPool), new Executor(ExecutorType.NewThread) };
            yield return new object[] { new Executor(ExecutorType.NewThread), new Executor(ExecutorType.NewThread) };
            yield break;
        }

        #endregion

        #region Console, Yield, Delay

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedConsole(Executor executor)
        {
            await executor.Execute(() =>
            {
                Console.WriteLine("C# Hello from ManagedThreadId: " + Environment.CurrentManagedThreadId);
                return Task.CompletedTask;
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSConsole(Executor executor)
        {
            await executor.Execute(() =>
            {
                WebWorkerTestHelper.Log("JS Hello from ManagedThreadId: " + Environment.CurrentManagedThreadId + " NativeThreadId: " + WebWorkerTestHelper.NativeThreadId);
                return Task.CompletedTask;
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task NativeThreadId(Executor executor)
        {
            await executor.Execute(async () =>
            {
                await WebWorkerTestHelper.InitializeAsync();

                var jsTid = WebWorkerTestHelper.GetTid();
                var csTid = WebWorkerTestHelper.NativeThreadId;
                if(executor.Type== ExecutorType.Main || executor.Type == ExecutorType.JSWebWorker)
                {
                    Assert.Equal(jsTid, csTid);
                }
                else
                {
                    Assert.NotEqual(jsTid, csTid);
                }

                await WebWorkerTestHelper.DisposeAsync();
            });
        }


        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSDelay(Executor executor)
        {
            await executor.Execute(async () =>
            {
                var currentTID = Environment.CurrentManagedThreadId;
                executor.AssertTargetThread();
                await WebWorkerTestHelper.CreateDelay();

                await WebWorkerTestHelper.Delay(1).ContinueWith(_ =>
                {
                    // continue on the context of the target JS interop
                    switch (executor.Type)
                    {
                        case ExecutorType.JSWebWorker:
                            Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                            Assert.Equal(currentTID, Environment.CurrentManagedThreadId);
                            Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                            break;
                        case ExecutorType.Main:
                            Assert.Equal(1, Environment.CurrentManagedThreadId);
                            Assert.Equal(currentTID, Environment.CurrentManagedThreadId);
                            Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                            break;
                        case ExecutorType.NewThread:
                            Assert.Equal(1, Environment.CurrentManagedThreadId);
                            Assert.NotEqual(currentTID, Environment.CurrentManagedThreadId);
                            Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                            break;
                        case ExecutorType.ThreadPool:
                            Assert.Equal(1, Environment.CurrentManagedThreadId);
                            Assert.NotEqual(currentTID, Environment.CurrentManagedThreadId);
                            Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                            break;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

                await WebWorkerTestHelper.Delay(1).ConfigureAwait(true);
                // continue on captured context
                switch (executor.Type)
                {
                    case ExecutorType.JSWebWorker:
                        Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                        Assert.Equal(currentTID, Environment.CurrentManagedThreadId);
                        Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                        break;
                    case ExecutorType.Main:
                        Assert.Equal(1, Environment.CurrentManagedThreadId);
                        Assert.Equal(currentTID, Environment.CurrentManagedThreadId);
                        Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                        break;
                    case ExecutorType.NewThread:
                        // the actual new thread is now blocked in .Wait() and so this is running on TP
                        Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                        Assert.NotEqual(currentTID, Environment.CurrentManagedThreadId);
                        Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                        break;
                    case ExecutorType.ThreadPool:
                        Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                        Assert.Equal(currentTID, Environment.CurrentManagedThreadId);
                        Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                        break;
                }
            });
        }


        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay(Executor executor)
        {
            await executor.Execute(async () =>
            {
                executor.AssertTargetThread();
                await WebWorkerTestHelper.CreateDelay();

                await Task.Delay(1).ContinueWith(_ =>
                {
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                }, TaskContinuationOptions.ExecuteSynchronously);

                await Task.Delay(1).ConfigureAwait(true);
                if (executor.Type == ExecutorType.Main)
                {
                    Assert.Equal(1, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                }
                else
                {
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                }
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedYield(Executor executor)
        {
            await executor.Execute(async () =>
            {
                var currentTID = Environment.CurrentManagedThreadId;
                executor.AssertTargetThread();

                await Task.Yield();
                if (executor.Type == ExecutorType.Main)
                {
                    Assert.Equal(1, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                }
                else if (executor.Type == ExecutorType.JSWebWorker)
                {
                    Assert.Equal(currentTID, Environment.CurrentManagedThreadId);
                    Assert.False(Thread.CurrentThread.IsThreadPoolThread);
                }
                else
                {
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                }
            });
        }

        #endregion

    }
}
