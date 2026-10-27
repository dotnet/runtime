// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public sealed class TaskArgumentValidationTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void CompletedTaskWaitsPreserveResults(int millisecondsTimeout)
        {
            Assert.True(Task.CompletedTask.Wait(millisecondsTimeout));
            Assert.True(Task.WaitAll([Task.CompletedTask], millisecondsTimeout));
            Assert.True(Task.WaitAll([], millisecondsTimeout));
            Assert.Equal(0, Task.WaitAny([Task.CompletedTask], millisecondsTimeout));
            Assert.Equal(-1, Task.WaitAny([], millisecondsTimeout));

            var error = new InvalidOperationException();
            Task faulted = Task.FromException(error);
            Assert.Same(error, Assert.Throws<AggregateException>(() => faulted.Wait(millisecondsTimeout)).InnerException);
            Assert.Same(error, Assert.Throws<AggregateException>(() => Task.WaitAll([faulted], millisecondsTimeout)).InnerException);
            Assert.Equal(0, Task.WaitAny([faulted], millisecondsTimeout));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(20)]
        public void IncompleteTaskWaitsWithoutThreadsHonorFiniteTimeouts(int millisecondsTimeout)
        {
            var source = new TaskCompletionSource<int>();
            Task pending = source.Task;

            if (millisecondsTimeout != Timeout.Infinite)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                Assert.False(pending.Wait(millisecondsTimeout));
                Assert.InRange(stopwatch.Elapsed.TotalMilliseconds, Math.Max(0, millisecondsTimeout - 1), double.MaxValue);
                stopwatch.Restart();
                Assert.False(Task.WaitAll([pending, Task.CompletedTask], millisecondsTimeout));
                Assert.InRange(stopwatch.Elapsed.TotalMilliseconds, Math.Max(0, millisecondsTimeout - 1), double.MaxValue);
                stopwatch.Restart();
                Assert.Equal(-1, Task.WaitAny([pending], millisecondsTimeout));
                Assert.InRange(stopwatch.Elapsed.TotalMilliseconds, Math.Max(0, millisecondsTimeout - 1), double.MaxValue);
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => pending.Wait(millisecondsTimeout));
                Assert.Throws<PlatformNotSupportedException>(() => Task.WaitAll([pending, Task.CompletedTask], millisecondsTimeout));
                Assert.Throws<PlatformNotSupportedException>(() => Task.WaitAny([pending], millisecondsTimeout));
            }
            Assert.Throws<PlatformNotSupportedException>(() => pending.GetAwaiter().GetResult());
            Assert.Equal(1, Task.WaitAny([pending, Task.CompletedTask], millisecondsTimeout));

            Assert.Throws<ArgumentException>("tasks", () => Task.WaitAll([null, Task.CompletedTask, pending], millisecondsTimeout));
            Assert.Throws<ArgumentException>("tasks", () => Task.WaitAny([Task.CompletedTask, pending, null], millisecondsTimeout));

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            if (millisecondsTimeout == 0)
            {
                Assert.False(pending.Wait(millisecondsTimeout, cancellation.Token));
            }
            else if (millisecondsTimeout != Timeout.Infinite)
            {
                Assert.Throws<OperationCanceledException>(() => pending.Wait(millisecondsTimeout, cancellation.Token));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => pending.Wait(millisecondsTimeout, cancellation.Token));
            }
            Assert.Throws<OperationCanceledException>(() =>
                pending.Wait(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellation.Token));
            Assert.Throws<OperationCanceledException>(() => Task.WaitAll([pending], millisecondsTimeout, cancellation.Token));
            Assert.Throws<OperationCanceledException>(() => Task.WaitAny([pending], millisecondsTimeout, cancellation.Token));

            source.SetResult(42);
            Assert.True(pending.Wait(millisecondsTimeout));
            Assert.Equal(42, source.Task.Result);
        }

        [Fact]
        public void InfiniteTaskWaitsCanExecuteInline()
        {
            var scheduler = new InlineWaitTaskScheduler(canInline: true);
            int executions = 0;
            var first = new Task(() => executions++);
            first.Start(scheduler);
            first.Wait();
            Assert.Equal(1, executions);

            var second = new Task(() => executions++);
            second.Start(scheduler);
            Task.WaitAll([first, second]);
            Assert.Equal(2, executions);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void RunSynchronouslyWithoutInliningRetainsQueuedTask()
        {
            var scheduler = new InlineWaitTaskScheduler(canInline: false);
            int executions = 0;
            var task = new Task(() => executions++);
            Assert.Throws<PlatformNotSupportedException>(() => task.RunSynchronously(scheduler));
            Assert.Equal(TaskStatus.WaitingToRun, task.Status);
            Assert.Equal(0, executions);
            Assert.True(scheduler.ExecuteQueuedTask());
            Assert.True(task.Wait(0));
            Assert.Equal(1, executions);
        }

        private sealed class InlineWaitTaskScheduler(bool canInline) : TaskScheduler
        {
            private Task _queuedTask;

            protected override void QueueTask(Task task) => _queuedTask = task;

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) =>
                canInline && TryExecuteTask(task);

            protected override IEnumerable<Task> GetScheduledTasks() =>
                _queuedTask is null ? [] : [_queuedTask];

            public bool ExecuteQueuedTask() => TryExecuteTask(_queuedTask);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData((long)int.MaxValue + 1)]
        public void Task_Wait_ArgumentOutOfRange(long milliseconds)
        {
            TimeSpan timeout = TimeSpan.FromMilliseconds(milliseconds);
            Task task = Task.Run(static () => {});
            Assert.Throws<ArgumentOutOfRangeException>("timeout", () => task.Wait(timeout));
        }
    }
}
