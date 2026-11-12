// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Async2SynchronizationContext
{
    [Fact]
    public static async Task TestSyncContextContinue()
    {
        SynchronizationContext.SetSynchronizationContext(new MySyncContext());
        await TestSyncContextContinueAsync();
    }

    private static async Task TestSyncContextContinueAsync()
    {
        MySyncContext context = (MySyncContext)SynchronizationContext.Current;
        await WrappedYieldToThreadPool(suspend: false);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: true);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: true).ConfigureAwait(true);
        Assert.Same(context, SynchronizationContext.Current);

        await WrappedYieldToThreadPool(suspend: false).ConfigureAwait(false);
        Assert.Same(context, SynchronizationContext.Current);

        // Currently disabled since ConfigureAwait does not become a runtime async call,
        // and this has a race condition until it does (where the callee finishes before
        // we check IsCompleted on the awaiter).
        //await WrappedYieldToThreadPool(suspend: true).ConfigureAwait(false);
        //Assert.Null(SynchronizationContext.Current);
        //
        //await WrappedYieldToThreadWithCustomSyncContext();
        //Assert.Null(SynchronizationContext.Current);
    }

    private static async Task WrappedYieldToThreadPool(bool suspend)
    {
        if (suspend)
        {
            await Task.Yield();
        }
    }

    private static async Task WrappedYieldToThreadWithCustomSyncContext()
    {
        Assert.Null(SynchronizationContext.Current);
        await new YieldToThreadWithCustomSyncContext();
        Assert.True(SynchronizationContext.Current is MySyncContext { });
    }

    private class MySyncContext : SynchronizationContext
    {
        public int NumPosts;

        public override void Post(SendOrPostCallback d, object state)
        {
            NumPosts++;
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                SynchronizationContext prevContext = Current;
                try
                {
                    SetSynchronizationContext(this);
                    d(state);
                }
                finally
                {
                    SetSynchronizationContext(prevContext);
                }
            }, null);
        }
    }

    private struct YieldToThreadWithCustomSyncContext : ICriticalNotifyCompletion
    {
        public YieldToThreadWithCustomSyncContext GetAwaiter() => this;

        public void UnsafeOnCompleted(Action continuation)
        {
            new Thread(state =>
            {
                SynchronizationContext.SetSynchronizationContext(new MySyncContext());
                continuation();
            }).Start();
        }

        public void OnCompleted(Action continuation)
        {
            throw new NotImplementedException();
        }

        public bool IsCompleted => false;

        public void GetResult() { }
    }

    [Fact]
    public static async Task TestSyncContextSaveRestore()
    {
        SynchronizationContext.SetSynchronizationContext(new SyncContextWithoutRestore());
        await TestSyncContextSaveRestoreAsync();
    }

    private static async Task TestSyncContextSaveRestoreAsync()
    {
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
        await ClearSyncContext();
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
    }

    private static async Task ClearSyncContext()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }

    [Fact]
    public static async Task TestSyncContextNotRestored()
    {
        SynchronizationContext.SetSynchronizationContext(new SyncContextWithoutRestore());
        await TestSyncContextNotRestoredAsync();
    }

    private static async Task TestSyncContextNotRestoredAsync()
    {
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
        await SuspendThenClearSyncContext();
        Assert.Null(SynchronizationContext.Current);
    }

    private static async Task SuspendThenClearSyncContext()
    {
        Assert.True(SynchronizationContext.Current is SyncContextWithoutRestore);
        SyncContextWithoutRestore syncCtx = (SyncContextWithoutRestore)SyncContextWithoutRestore.Current;
        Assert.Equal(0, syncCtx.NumPosts);

        await Task.Yield();
        Assert.Null(SynchronizationContext.Current);
        Assert.Equal(1, syncCtx.NumPosts);
    }

    private class SyncContextWithoutRestore : SynchronizationContext
    {
        public int NumPosts;

        public override void Post(SendOrPostCallback d, object state)
        {
            NumPosts++;
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                d(state);
            }, null);
        }
    }

    [Fact]
    public static async Task TestContinueOnCorrectSyncContextAsync()
    {
        MySyncContext context1 = new MySyncContext();
        MySyncContext context2 = new MySyncContext();

        SynchronizationContext.SetSynchronizationContext(context1);
        await SetContext(context2, suspend: false);
        Assert.True(SynchronizationContext.Current == context1);

        await SetContext(context2, suspend: true);
        Assert.True(SynchronizationContext.Current == context1);
    }

    private static async Task SetContext(SynchronizationContext context, bool suspend)
    {
        SynchronizationContext.SetSynchronizationContext(context);

        if (suspend)
            await Task.Yield();
    }

    [Fact]
    public static async Task TestNoSyncContextInRuntimeCallableThunk()
    {
        SynchronizationContext.SetSynchronizationContext(new MySyncContext());
        Task task = TestNoSyncContextInRuntimeCallableThunkAsync();
#pragma warning disable xUnit1030 // Keep the test's own continuation from posting to the context under test.
        await task.ConfigureAwait(false);
#pragma warning restore xUnit1030
    }

    private static async Task TestNoSyncContextInRuntimeCallableThunkAsync()
    {
        MySyncContext syncCtx = (MySyncContext)SynchronizationContext.Current;
        await Task.Delay(100).ConfigureAwait(false);
        Assert.Equal(0, syncCtx.NumPosts);
    }

    [Fact]
    public static async Task TestAsyncToSyncContextSwitchAsync()
    {
        MySyncContext context1 = new MySyncContext();
        MySyncContext context2 = new MySyncContext();
        SynchronizationContext.SetSynchronizationContext(context1);
        await SyncMethodThatSwitchesContext(context2);
        Assert.Same(context2, SynchronizationContext.Current);
    }

    private static Task SyncMethodThatSwitchesContext(MySyncContext context)
    {
        SynchronizationContext.SetSynchronizationContext(context);
        return Task.CompletedTask;
    }

    public static IEnumerable<object[]> TaskContinuationContextCases()
    {
        foreach (bool useScheduler in new[] { false, true })
        {
            foreach (bool runAsynchronously in new[] { false, true })
            {
                foreach (int awaiterCount in new[] { 1, 3 })
                {
                    for (int shape = 0; shape < 5; shape++)
                    {
                        yield return new object[] { useScheduler, runAsynchronously, awaiterCount, shape };
                    }
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TaskContinuationContextCases))]
    public static void TaskContinuationsPostDirectlyToCapturedContext(
        bool useScheduler, bool runAsynchronously, int awaiterCount, int shape)
    {
        SynchronizationContext previousContext = SynchronizationContext.Current;
        var context = new QueuedSyncContext();
        var source = new TaskCompletionSource<int>(
            runAsynchronously ? TaskCreationOptions.RunContinuationsAsynchronously : TaskCreationOptions.None);
        var local = new AsyncLocal<int>();
        var awaiters = new Task[awaiterCount];

        try
        {
            SynchronizationContext.SetSynchronizationContext(useScheduler ? null : context);
            if (useScheduler)
            {
                var scheduler = new QueuedTaskScheduler(context);
                Task registration = Task.Factory.StartNew(RegisterAwaiters, CancellationToken.None, TaskCreationOptions.None, scheduler);
                context.Drain();
                registration.GetAwaiter().GetResult();
            }
            else
            {
                RegisterAwaiters();
            }

            SynchronizationContext.SetSynchronizationContext(null);
            int postsBeforeCompletion = context.PostCount;
            source.SetResult(42);

            bool captureContext = shape != 4;
            Assert.Equal(captureContext ? awaiterCount : 0, context.Count);
            if (captureContext)
            {
                foreach (Task awaiter in awaiters)
                {
                    Assert.False(awaiter.IsCompleted);
                }
            }

            SynchronizationContext.SetSynchronizationContext(useScheduler ? null : context);
            context.Drain();
            Assert.True(Task.WaitAll(awaiters, TimeSpan.FromSeconds(30)));
            Assert.Equal(0, context.Count);
            // The stored-task wrapper and its caller each capture the non-inlining scheduler.
            int postsPerAwaiter = useScheduler && shape == 3 ? 2 : 1;
            Assert.Equal(postsBeforeCompletion + (captureContext ? awaiterCount * postsPerAwaiter : 0), context.PostCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }

        void RegisterAwaiters()
        {
            for (int i = 0; i < awaiters.Length; i++)
            {
                local.Value = i;
                awaiters[i] = AwaitTask(i);
            }
        }

        async Task AwaitTask(int expectedLocal)
        {
            switch (shape)
            {
                case 0:
                    await source.Task;
                    break;
                case 1:
                    await GetTask(source);
                    break;
                case 2:
                    Assert.Equal(42, await source.Task.ConfigureAwait(true));
                    break;
                case 3:
                    await AwaitStoredTask(source.Task);
                    break;
                default:
                    await source.Task.ConfigureAwait(false);
                    break;
            }

            Assert.Equal(expectedLocal, local.Value);
            Assert.Same(shape != 4 && !useScheduler ? context : null, SynchronizationContext.Current);
            Assert.Equal(shape != 4 && useScheduler, TaskScheduler.Current is QueuedTaskScheduler);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task GetTask(TaskCompletionSource<int> source) => source.Task;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task AwaitStoredTask(Task task) => await task;

    [Fact]
    public static void TaskContinuationWithLowStackUsesThreadPool()
    {
        SynchronizationContext previousContext = SynchronizationContext.Current;
        var source = new TaskCompletionSource<int>();
        var completingThread = new Thread(() => CompleteWithLowStack(source), maxStackSize: 256 * 1024)
        {
            IsBackground = true
        };
        var context = new QueuedSyncContext(completingThread);

        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            Task awaiter = AwaitTask();
            SynchronizationContext.SetSynchronizationContext(null);
            completingThread.Start();
            Assert.True(completingThread.Join(TimeSpan.FromSeconds(30)));

            Assert.True(SpinWait.SpinUntil(() => context.Count != 0, TimeSpan.FromSeconds(30)));
            Assert.False(awaiter.IsCompleted);
            SynchronizationContext.SetSynchronizationContext(context);
            context.Drain(postedFromCompletingThread: false);
            awaiter.GetAwaiter().GetResult();
            Assert.Equal(0, context.Count);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }

        async Task AwaitTask()
        {
            await GetTask(source);
            Assert.Equal(42, source.Task.Result);
            Assert.Same(context, SynchronizationContext.Current);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CompleteWithLowStack(TaskCompletionSource<int> source)
    {
        if (RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            CompleteWithLowStack(source);
            GC.KeepAlive(source);
        }
        else
        {
            source.SetResult(42);
        }
    }

    private sealed class QueuedSyncContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object State, int ThreadId)> _queue = new();
        private readonly int _threadId;
        private int _postCount;

        public QueuedSyncContext(Thread completingThread = null) =>
            _threadId = completingThread?.ManagedThreadId ?? Environment.CurrentManagedThreadId;

        public int Count => _queue.Count;
        public int PostCount => Volatile.Read(ref _postCount);

        public override void Post(SendOrPostCallback d, object state)
        {
            Interlocked.Increment(ref _postCount);
            _queue.Enqueue((d, state, Environment.CurrentManagedThreadId));
        }

        public void Drain(bool postedFromCompletingThread = true)
        {
            while (_queue.TryDequeue(out var item))
            {
                Assert.Equal(postedFromCompletingThread, _threadId == item.ThreadId);
                item.Callback(item.State);
            }
        }
    }

    private sealed class QueuedTaskScheduler : TaskScheduler
    {
        private readonly QueuedSyncContext _context;

        public QueuedTaskScheduler(QueuedSyncContext context) => _context = context;

        protected override void QueueTask(Task task) => _context.Post(_ => TryExecuteTask(task), null);

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

        protected override IEnumerable<Task> GetScheduledTasks() => null;
    }
}
