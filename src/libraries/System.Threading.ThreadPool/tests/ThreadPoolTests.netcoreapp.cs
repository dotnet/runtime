// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
