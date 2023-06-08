// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests
{
    public class TaskToAsyncResultTests
    {
        [Fact]
        public void InvalidArguments_ThrowExceptions()
        {
            AssertExtensions.Throws<ArgumentNullException>("task", () => TaskToAsyncResult.Begin(null, null, null));
            AssertExtensions.Throws<ArgumentNullException>("task", () => TaskToAsyncResult.Begin(null, iar => { }, "test"));

            AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => TaskToAsyncResult.End(null));
            AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => TaskToAsyncResult.End<int>(null));

            AssertExtensions.Throws<ArgumentException>("asyncResult", () => TaskToAsyncResult.End(new NonTaskIAsyncResult()));
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => TaskToAsyncResult.End<int>(new NonTaskIAsyncResult()));
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => TaskToAsyncResult.End<int>(Task.FromResult((long)42)));

            AssertExtensions.Throws<ArgumentException>("asyncResult", () => TaskToAsyncResult.Unwrap(new NonTaskIAsyncResult()));
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => TaskToAsyncResult.Unwrap<int>(new NonTaskIAsyncResult()));
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => TaskToAsyncResult.Unwrap<int>(Task.FromResult((long)42)));
        }

        [Fact]
        public async Task BeginFromTask_UnwrapTask_EndFromTask_Roundtrips()
        {
            var tcs = new TaskCompletionSource<int>();
            object state = new object();

            IAsyncResult ar = TaskToAsyncResult.Begin(tcs.Task, null, state);
            Assert.NotNull(ar);
            Assert.Same(state, ar.AsyncState);
            Assert.NotNull(ar.AsyncWaitHandle);
            Assert.False(ar.CompletedSynchronously);
            Assert.False(ar.IsCompleted);

            Assert.Same(tcs.Task, TaskToAsyncResult.Unwrap(ar));
            Assert.Same(tcs.Task, TaskToAsyncResult.Unwrap<int>(ar));

            tcs.SetResult(42);
            await tcs.Task;

            Assert.True(ar.IsCompleted);
            Assert.False(ar.CompletedSynchronously);
            Assert.True(ar.AsyncWaitHandle.WaitOne(0));

            TaskToAsyncResult.End(ar);
            Assert.Equal(42, TaskToAsyncResult.End<int>(ar));
        }

        [Fact]
        public void BeginFromTask_CompletedSynchronously_CallbackInvokedSynchronously()
        {
            Task<int> t = Task.FromResult(42);
            object state = new object();

            int id = Environment.CurrentManagedThreadId;

            IAsyncResult arCallback = null;
            IAsyncResult ar = TaskToAsyncResult.Begin(t, iar =>
            {
                arCallback = iar;

                Assert.True(iar.CompletedSynchronously);
                Assert.True(iar.IsCompleted);
                Assert.Same(state, iar.AsyncState);

                Assert.Equal(id, Environment.CurrentManagedThreadId);

                Assert.Equal(42, TaskToAsyncResult.End<int>(iar));
            }, state);

            Assert.Same(ar, arCallback);
            Assert.True(ar.CompletedSynchronously);
            Assert.True(ar.IsCompleted);
            Assert.Same(state, ar.AsyncState);
        }

        [Fact]
        public async Task BeginFromTask_CompletedAsynchronously_CallbackInvokedAsynchronously()
        {
            var tcs = new TaskCompletionSource();
            var invoked = new TaskCompletionSource();

            var tl = new ThreadLocal<int>();
            tl.Value = 42;
            IAsyncResult ar = TaskToAsyncResult.Begin(tcs.Task, iar =>
            {
                Assert.NotEqual(42, tl.Value);
                Assert.False(iar.CompletedSynchronously);
                Assert.True(iar.IsCompleted);
                Assert.Null(iar.AsyncState);
                invoked.SetResult();
            }, null);
            tl.Value = 0;

            Assert.False(invoked.Task.IsCompleted);
            Assert.False(ar.CompletedSynchronously);
            Assert.False(ar.IsCompleted);
            Assert.Null(ar.AsyncState);
            Assert.NotNull(ar.AsyncWaitHandle);
            Assert.False(ar.AsyncWaitHandle.WaitOne(0));

            tcs.SetResult();
            await invoked.Task;

            Assert.False(ar.CompletedSynchronously);
            Assert.True(ar.IsCompleted);
            Assert.Null(ar.AsyncState);
            Assert.NotNull(ar.AsyncWaitHandle);
            Assert.True(ar.AsyncWaitHandle.WaitOne(0));
        }

        [Fact]
        public void EndFromTask_PropagatesExceptions()
        {
            IAsyncResult ar = TaskToAsyncResult.Begin(Task.FromException(new FormatException()), null, null);
            Assert.Throws<FormatException>(() => TaskToAsyncResult.End(ar));

            ar = TaskToAsyncResult.Begin(Task.FromException<int>(new FormatException()), null, null);
            Assert.Throws<FormatException>(() => TaskToAsyncResult.End<int>(ar));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task WithFromAsync_IAsyncResult_Roundtrips()
        {
            var tcs = new TaskCompletionSource();
            var invoked = new TaskCompletionSource();
            _ = Task.Factory.FromAsync(TaskToAsyncResult.Begin(tcs.Task, null, null), iar =>
            {
                invoked.SetResult();
            });
            tcs.SetResult();
            await invoked.Task;
        }

        [Fact]
        public async Task WithFromAsync_Delegate_Roundtrips()
        {
            var tcs = new TaskCompletionSource();
            var invoked = new TaskCompletionSource();
            _ = Task.Factory.FromAsync(
                (callback, state) => TaskToAsyncResult.Begin(tcs.Task, callback, state),
                iar => invoked.SetResult(),
                new object());
            tcs.SetResult();
            await invoked.Task;
        }
    }

    internal sealed class NonTaskIAsyncResult : IAsyncResult
    {
        public object? AsyncState { get; set; }
        public WaitHandle AsyncWaitHandle { get; set; }
        public bool CompletedSynchronously { get; set; }
        public bool IsCompleted { get; set; }
    }
}
