﻿// Licensed to the .NET Foundation under one or more agreements.
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
    // JSWebWorker.RunAsync with CancellationToken
    // JSExport 2x
    // JSExport async
    // timer
    // GC + finalizer + dispose
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
            return Enum.GetValues<ExecutorType>().Select(type => new object[] { new Executor(type) });
        }

        public static IEnumerable<object[]> GetTargetThreads2x2()
        {
            return Enum.GetValues<ExecutorType>().SelectMany(
                type1 => Enum.GetValues<ExecutorType>().Select(
                    type2 => new object[] { new Executor(type1), new Executor(type2) }));
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
                await executor.StickyAwait(WebWorkerTestHelper.InitializeAsync());

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
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ThreadingTimer(Executor executor)
        {
            await executor.Execute(async () =>
            {
                TaskCompletionSource tcs = new TaskCompletionSource();
                executor.AssertTargetThread();

                using var timer = new Threading.Timer(_ =>
                {
                    Assert.NotEqual(1, Environment.CurrentManagedThreadId);
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                    tcs.SetResult();
                }, null, 100, Timeout.Infinite);

                await tcs.Task;
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSDelay_ContinueWith(Executor executor)
        {
            await executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.CreateDelay());

                await WebWorkerTestHelper.Delay(1).ContinueWith(_ =>
                {
                    // continue on the context of the target JS interop
                    executor.AssertInteropThread();
                }, TaskContinuationOptions.ExecuteSynchronously);
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task JSDelay_ConfigureAwait_True(Executor executor)
        {
            await executor.Execute(async () =>
            {
                await executor.StickyAwait(WebWorkerTestHelper.CreateDelay());

                await WebWorkerTestHelper.Delay(1).ConfigureAwait(true);

                executor.AssertAwaitCapturedContext();
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay_ContinueWith(Executor executor)
        {
            await executor.Execute(async () =>
            {
                executor.AssertTargetThread();
                await Task.Delay(1).ContinueWith(_ =>
                {
                    // continue on the context of the Timer's thread pool thread
                    Assert.True(Thread.CurrentThread.IsThreadPoolThread);
                }, TaskContinuationOptions.ExecuteSynchronously);
            });
        }


        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedDelay_ConfigureAwait_True(Executor executor)
        {
            await executor.Execute(async () =>
            {
                executor.AssertTargetThread();

                await Task.Delay(1).ConfigureAwait(true);

                executor.AssertAwaitCapturedContext();
            });
        }

        [Theory, MemberData(nameof(GetTargetThreads))]
        public async Task ManagedYield(Executor executor)
        {
            await executor.Execute(async () =>
            {
                executor.AssertTargetThread();

                await Task.Yield();

                executor.AssertAwaitCapturedContext();
            });
        }

        #endregion

    }
}
