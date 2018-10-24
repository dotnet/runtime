// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.ThreadPools.Tests
{
    public partial class ThreadPoolTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void QueueUserWorkItem_PreferLocal_InvalidArguments_Throws(bool preferLocal)
        {
            AssertExtensions.Throws<ArgumentNullException>("callBack", () => ThreadPool.QueueUserWorkItem(null, new object(), preferLocal));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueueUserWorkItem_PreferLocal_NullValidForState(bool preferLocal)
        {
            var tcs = new TaskCompletionSource<int>();
            ThreadPool.QueueUserWorkItem(s => tcs.SetResult(84), (object)null, preferLocal);
            Assert.Equal(84, await tcs.Task);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueueUserWorkItem_PreferLocal_ReferenceTypeStateObjectPassedThrough(bool preferLocal)
        {
            var tcs = new TaskCompletionSource<int>();
            ThreadPool.QueueUserWorkItem(s => s.SetResult(84), tcs, preferLocal);
            Assert.Equal(84, await tcs.Task);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueueUserWorkItem_PreferLocal_ValueTypeStateObjectPassedThrough(bool preferLocal)
        {
            var tcs = new TaskCompletionSource<int>();
            ThreadPool.QueueUserWorkItem(s => s.tcs.SetResult(s.value), (tcs, value: 42), preferLocal);
            Assert.Equal(42, await tcs.Task);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueueUserWorkItem_PreferLocal_RunsAsynchronously(bool preferLocal)
        {
            await Task.Factory.StartNew(() =>
            {
                int origThread = Environment.CurrentManagedThreadId;
                var tcs = new TaskCompletionSource<int>();
                ThreadPool.QueueUserWorkItem(s => s.SetResult(Environment.CurrentManagedThreadId), tcs, preferLocal);
                Assert.NotEqual(origThread, tcs.Task.GetAwaiter().GetResult());
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueueUserWorkItem_PreferLocal_ExecutionContextFlowed(bool preferLocal)
        {
            var tcs = new TaskCompletionSource<int>();
            var asyncLocal = new AsyncLocal<int>() { Value = 42 };
            ThreadPool.QueueUserWorkItem(s => s.SetResult(asyncLocal.Value), tcs, preferLocal);
            asyncLocal.Value = 0;
            Assert.Equal(42, await tcs.Task);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnsafeQueueUserWorkItem_IThreadPoolWorkItem_Invalid_Throws(bool preferLocal)
        {
            AssertExtensions.Throws<ArgumentNullException>("callBack", () => ThreadPool.UnsafeQueueUserWorkItem(null, preferLocal));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("callBack", () => ThreadPool.UnsafeQueueUserWorkItem(new InvalidWorkItemAndTask(() => { }), preferLocal));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_ManyIndividualItems_AllInvoked(bool preferLocal)
        {
            TaskCompletionSource<bool>[] tasks = Enumerable.Range(0, 100).Select(_ => new TaskCompletionSource<bool>()).ToArray();
            for (int i = 0; i < tasks.Length; i++)
            {
                int localI = i;
                ThreadPool.UnsafeQueueUserWorkItem(new SimpleWorkItem(() =>
                {
                    tasks[localI].TrySetResult(true);
                }), preferLocal);
            }
            await Task.WhenAll(tasks.Select(t => t.Task));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_SameObjectReused_AllInvoked(bool preferLocal)
        {
            const int Iters = 100;
            int remaining = Iters;
            var tcs = new TaskCompletionSource<bool>();
            var workItem = new SimpleWorkItem(() =>
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    tcs.TrySetResult(true);
                }
            });
            for (int i = 0; i < Iters; i++)
            {
                ThreadPool.UnsafeQueueUserWorkItem(workItem, preferLocal);
            }
            await tcs.Task;
            Assert.Equal(0, remaining);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnsafeQueueUserWorkItem_IThreadPoolWorkItem_ExecutionContextNotFlowed(bool preferLocal)
        {
            var al = new AsyncLocal<int> { Value = 42 };
            var tcs = new TaskCompletionSource<bool>();
            ThreadPool.UnsafeQueueUserWorkItem(new SimpleWorkItem(() =>
            {
                Assert.Equal(0, al.Value);
                tcs.TrySetResult(true);
            }), preferLocal);
            await tcs.Task;
            Assert.Equal(42, al.Value);
        }

        private sealed class SimpleWorkItem : IThreadPoolWorkItem
        {
            private readonly Action _action;
            public SimpleWorkItem(Action action) => _action = action;
            public void Execute() => _action();
        }

        private sealed class InvalidWorkItemAndTask : Task, IThreadPoolWorkItem
        {
            public InvalidWorkItemAndTask(Action action) : base(action) { }
            public void Execute() { }
        }
    }
}
