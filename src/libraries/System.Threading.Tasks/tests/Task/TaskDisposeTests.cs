// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public static class TaskDisposeTests
    {
        [Fact]
        public static void Dispose_BeforeComplete()
        {
            // Verify that a task can only be disposed after it has completed
            var endTask = new ManualResetEvent(false);
            var task = new Task(() => { endTask.WaitOne(); });
            Assert.Throws<InvalidOperationException>(() => task.Dispose());
            task.Start();
            Assert.Throws<InvalidOperationException>(() => task.Dispose());
            endTask.Set();
            task.Wait();
            task.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { var wh = ((IAsyncResult)task).AsyncWaitHandle; });

            // A task may also be disposed after it is canceled
            endTask.Reset();
            var cts = new CancellationTokenSource();
            task = new Task(() => { endTask.WaitOne(); }, cts.Token);
            cts.Cancel();
            task.Dispose();
        }

        [Fact]
        public static void Dispose_InContinuation()
        {
            // Verify that a task can be disposed by a continuation
            var endTask = new ManualResetEvent(false);
            var task = new Task(() => { });
            var task2 =
                task.ContinueWith(completedTask =>
                {
                    completedTask.Dispose();
                    endTask.WaitOne();
                });
            task.Start();
            endTask.Set();
            ((IAsyncResult)task2).AsyncWaitHandle.WaitOne();
            task2.Dispose();
        }

        [Fact]
        public static void Dispose_ThenAddContinuation()
        {
            // Verify that a continuation can be added after a task is completed and disposed
            var task = new Task(() => { });
            task.Start();
            task.Wait();
            task.Dispose();
            var task2 = task.ContinueWith(completedTask => { });
            task2.Wait();
            task2.Dispose();
        }

        [Fact]
        public static void Dispose_ThenUseTaskCompletionSource()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetResult(1);
            tcs.Task.Dispose();
            Assert.Throws<InvalidOperationException>(() => tcs.SetResult(2));
            Assert.Throws<InvalidOperationException>(() => tcs.SetCanceled());
            Assert.Throws<InvalidOperationException>(() => tcs.SetException(new Exception()));
            Assert.Equal(1, tcs.Task.Result);
        }
    }
}
