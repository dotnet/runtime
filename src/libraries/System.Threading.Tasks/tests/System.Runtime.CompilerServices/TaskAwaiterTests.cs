// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class TaskAwaiterTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(false, null)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(true, null)]
        public static void OnCompleted_CompletesInAnotherSynchronizationContext(bool generic, bool? continueOnCapturedContext)
        {
            SynchronizationContext origCtx = SynchronizationContext.Current;
            try
            {
                // Create a context that tracks operations, and set it as current
                var validateCtx = new ValidateCorrectContextSynchronizationContext();
                Assert.Equal(0, validateCtx.PostCount);
                SynchronizationContext.SetSynchronizationContext(validateCtx);

                // Create a not-completed task and get an awaiter for it
                var mres = new ManualResetEventSlim();
                var tcs = new TaskCompletionSource<object>();

                // Hook up a callback
                bool postedInContext = false;
                Action callback = () =>
                {
                    postedInContext = ValidateCorrectContextSynchronizationContext.t_isPostedInContext;
                    mres.Set();
                };
                if (generic)
                {
                    if (continueOnCapturedContext.HasValue) tcs.Task.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(callback);
                    else tcs.Task.GetAwaiter().OnCompleted(callback);
                }
                else
                {
                    if (continueOnCapturedContext.HasValue) ((Task)tcs.Task).ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(callback);
                    else ((Task)tcs.Task).GetAwaiter().OnCompleted(callback);
                }
                Assert.False(mres.IsSet, "Callback should not yet have run.");

                // Complete the task in another context and wait for the callback to run
                Task.Run(() => tcs.SetResult(null));
                mres.Wait();

                // Validate the callback ran and in the correct context
                bool shouldHavePosted = !continueOnCapturedContext.HasValue || continueOnCapturedContext.Value;
                Assert.Equal(shouldHavePosted ? 1 : 0, validateCtx.PostCount);
                Assert.Equal(shouldHavePosted, postedInContext);
            }
            finally
            {
                // Reset back to the original context
                SynchronizationContext.SetSynchronizationContext(origCtx);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(false, null)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(true, null)]
        public static void OnCompleted_CompletesInAnotherTaskScheduler(bool generic, bool? continueOnCapturedContext)
        {
            SynchronizationContext origCtx = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null); // get off xunit's SynchronizationContext to avoid interactions with await

                var quwi = new QUWITaskScheduler();
                RunWithSchedulerAsCurrent(quwi, delegate
                {
                    Assert.True(TaskScheduler.Current == quwi, "Expected to be on target scheduler");

                    // Create the not completed task and get its awaiter
                    var mres = new ManualResetEventSlim();
                    var tcs = new TaskCompletionSource<object>();

                    // Hook up the callback
                    bool ranOnScheduler = false;
                    Action callback = () =>
                    {
                        ranOnScheduler = (TaskScheduler.Current == quwi);
                        mres.Set();
                    };
                    if (generic)
                    {
                        if (continueOnCapturedContext.HasValue) tcs.Task.ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(callback);
                        else tcs.Task.GetAwaiter().OnCompleted(callback);
                    }
                    else
                    {
                        if (continueOnCapturedContext.HasValue) ((Task)tcs.Task).ConfigureAwait(continueOnCapturedContext.Value).GetAwaiter().OnCompleted(callback);
                        else ((Task)tcs.Task).GetAwaiter().OnCompleted(callback);
                    }
                    Assert.False(mres.IsSet, "Callback should not yet have run.");

                    // Complete the task in another scheduler and wait for the callback to run
                    Task.Run(delegate { tcs.SetResult(null); });
                    mres.Wait();

                    // Validate the callback ran on the right scheduler
                    bool shouldHaveRunOnScheduler = !continueOnCapturedContext.HasValue || continueOnCapturedContext.Value;
                    Assert.Equal(shouldHaveRunOnScheduler, ranOnScheduler);
                });
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(origCtx);
            }
        }

        [Fact]
        public static void GetResult_Completed_Success()
        {
            Task task = Task.CompletedTask;
            task.GetAwaiter().GetResult();
            task.ConfigureAwait(false).GetAwaiter().GetResult();
            task.ConfigureAwait(true).GetAwaiter().GetResult();

            const string expectedResult = "42";
            Task<string> taskOfString = Task.FromResult(expectedResult);
            Assert.Equal(expectedResult, taskOfString.GetAwaiter().GetResult());
            Assert.Equal(expectedResult, taskOfString.ConfigureAwait(false).GetAwaiter().GetResult());
            Assert.Equal(expectedResult, taskOfString.ConfigureAwait(true).GetAwaiter().GetResult());
        }

        [OuterLoop]
        [Fact]
        public static void GetResult_NotCompleted_BlocksUntilCompletion()
        {
            var tcs = new TaskCompletionSource<bool>();

            // Kick off tasks that should all block
            var tasks = new[] {
                Task.Run(() => tcs.Task.GetAwaiter().GetResult()),
                Task.Run(() => ((Task)tcs.Task).GetAwaiter().GetResult()),
                Task.Run(() => tcs.Task.ConfigureAwait(false).GetAwaiter().GetResult()),
                Task.Run(() => ((Task)tcs.Task).ConfigureAwait(false).GetAwaiter().GetResult())
            };
            Assert.Equal(-1, Task.WaitAny(tasks, 100)); // "Tasks should not have completed"

            // Now complete the tasks, after which all the tasks should complete successfully.
            tcs.SetResult(true);
            Task.WaitAll(tasks);
        }

        [Fact]
        public static void GetResult_CanceledTask_ThrowsCancellationException()
        {
            // Validate cancellation
            Task<string> canceled = Task.FromCanceled<string>(new CancellationToken(true));

            // Task.GetAwaiter and Task<T>.GetAwaiter
            Assert.Throws<TaskCanceledException>(() => ((Task)canceled).GetAwaiter().GetResult());
            Assert.Throws<TaskCanceledException>(() => canceled.GetAwaiter().GetResult());

            // w/ ConfigureAwait false and true
            Assert.Throws<TaskCanceledException>(() => ((Task)canceled).ConfigureAwait(false).GetAwaiter().GetResult());
            Assert.Throws<TaskCanceledException>(() => ((Task)canceled).ConfigureAwait(true).GetAwaiter().GetResult());
            Assert.Throws<TaskCanceledException>(() => canceled.ConfigureAwait(false).GetAwaiter().GetResult());
            Assert.Throws<TaskCanceledException>(() => canceled.ConfigureAwait(true).GetAwaiter().GetResult());
        }

        [Fact]
        public static void GetResult_FaultedTask_OneException_ThrowsOriginalException()
        {
            var exception = new ArgumentException("uh oh");
            Task<string> task = Task.FromException<string>(exception);

            // Task.GetAwaiter and Task<T>.GetAwaiter
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => ((Task)task).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => task.GetAwaiter().GetResult()));

            // w/ ConfigureAwait false and true
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => ((Task)task).ConfigureAwait(false).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => ((Task)task).ConfigureAwait(true).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => task.ConfigureAwait(false).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => task.ConfigureAwait(true).GetAwaiter().GetResult()));
        }

        [Fact]
        public static void GetResult_FaultedTask_MultipleExceptions_ThrowsFirstException()
        {
            var exception = new ArgumentException("uh oh");
            var tcs = new TaskCompletionSource<string>();
            tcs.SetException(new Exception[] { exception, new InvalidOperationException("uh oh") });
            Task<string> task = tcs.Task;

            // Task.GetAwaiter and Task<T>.GetAwaiter
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => ((Task)task).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => task.GetAwaiter().GetResult()));

            // w/ ConfigureAwait false and true
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => ((Task)task).ConfigureAwait(false).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => ((Task)task).ConfigureAwait(true).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => task.ConfigureAwait(false).GetAwaiter().GetResult()));
            Assert.Same(exception, AssertExtensions.Throws<ArgumentException>(null, () => task.ConfigureAwait(true).GetAwaiter().GetResult()));
        }

        [Fact]
        public static void AwaiterAndAwaitableEquality()
        {
            var completed = new TaskCompletionSource<string>();
            Task task = completed.Task;

            // TaskAwaiter
            task.GetAwaiter().Equals(task.GetAwaiter());

            // ConfiguredTaskAwaitable
            Assert.Equal(task.ConfigureAwait(false), task.ConfigureAwait(false));
            Assert.NotEqual(task.ConfigureAwait(false), task.ConfigureAwait(true));
            Assert.NotEqual(task.ConfigureAwait(true), task.ConfigureAwait(false));

            // ConfiguredTaskAwaitable<T>
            Assert.Equal(task.ConfigureAwait(false), task.ConfigureAwait(false));
            Assert.NotEqual(task.ConfigureAwait(false), task.ConfigureAwait(true));
            Assert.NotEqual(task.ConfigureAwait(true), task.ConfigureAwait(false));

            // ConfiguredTaskAwaitable.ConfiguredTaskAwaiter
            Assert.Equal(task.ConfigureAwait(false).GetAwaiter(), task.ConfigureAwait(false).GetAwaiter());
            Assert.NotEqual(task.ConfigureAwait(false).GetAwaiter(), task.ConfigureAwait(true).GetAwaiter());
            Assert.NotEqual(task.ConfigureAwait(true).GetAwaiter(), task.ConfigureAwait(false).GetAwaiter());

            // ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter
            Assert.Equal(task.ConfigureAwait(false).GetAwaiter(), task.ConfigureAwait(false).GetAwaiter());
            Assert.NotEqual(task.ConfigureAwait(false).GetAwaiter(), task.ConfigureAwait(true).GetAwaiter());
            Assert.NotEqual(task.ConfigureAwait(true).GetAwaiter(), task.ConfigureAwait(false).GetAwaiter());
        }

        [Fact]
        public static void BaseSynchronizationContext_SameAsNoSynchronizationContext()
        {
            var quwi = new QUWITaskScheduler();
            SynchronizationContext origCtx = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                RunWithSchedulerAsCurrent(quwi, delegate
                {
                    ManualResetEventSlim mres = new ManualResetEventSlim();
                    var tcs = new TaskCompletionSource<object>();
                    var awaiter = ((Task)tcs.Task).GetAwaiter();

                    bool ranOnScheduler = false;
                    bool ranWithoutSyncCtx = false;
                    awaiter.OnCompleted(() =>
                    {
                        ranOnScheduler = (TaskScheduler.Current == quwi);
                        ranWithoutSyncCtx = SynchronizationContext.Current == null;
                        mres.Set();
                    });
                    Assert.False(mres.IsSet, "Callback should not yet have run.");

                    Task.Run(delegate { tcs.SetResult(null); });
                    mres.Wait();

                    Assert.True(ranOnScheduler, "Should have run on scheduler");
                    Assert.True(ranWithoutSyncCtx, "Should have run with a null sync ctx");
                });
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(origCtx);
            }
        }

        [Theory]
        [MemberData(nameof(CanceledTasksAndExpectedCancellationExceptions))]
        public static void OperationCanceledException_PropagatesThroughCanceledTask(int lineNumber, Task task, OperationCanceledException expected)
        {
            var caught = Assert.ThrowsAny<OperationCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.Same(expected, caught);
        }

        public static IEnumerable<object[]> CanceledTasksAndExpectedCancellationExceptions()
        {
            var cts = new CancellationTokenSource();
            var oce = new OperationCanceledException(cts.Token);

            // Scheduled Task
            Task<int> generic = Task.Run<int>(new Func<int>(() =>
            {
                cts.Cancel();
                throw oce;
            }), cts.Token);
            yield return new object[] { LineNumber(), generic, oce };

            Task nonGeneric = generic;

            // WhenAll Task and Task<int>
            yield return new object[] { LineNumber(), Task.WhenAll(generic), oce };
            yield return new object[] { LineNumber(), Task.WhenAll(generic, Task.FromResult(42)), oce };
            yield return new object[] { LineNumber(), Task.WhenAll(Task.FromResult(42), generic), oce };
            yield return new object[] { LineNumber(), Task.WhenAll(generic, generic, generic), oce };
            yield return new object[] { LineNumber(), Task.WhenAll(nonGeneric), oce };
            yield return new object[] { LineNumber(), Task.WhenAll(nonGeneric, Task.FromResult(42)), oce };
            yield return new object[] { LineNumber(), Task.WhenAll(Task.FromResult(42), nonGeneric), oce };
            yield return new object[] { LineNumber(), Task.WhenAll(nonGeneric, nonGeneric, nonGeneric), oce };

            // Task.Run Task and Task<int> with unwrapping
            yield return new object[] { LineNumber(), Task.Run(() => generic), oce };
            yield return new object[] { LineNumber(), Task.Run(() => nonGeneric), oce };

            // A FromAsync Task and Task<int>
            yield return new object[] { LineNumber(), Task.Factory.FromAsync(generic, new Action<IAsyncResult>(ar => { throw oce; })), oce };
            yield return new object[] { LineNumber(), Task<int>.Factory.FromAsync(nonGeneric, new Func<IAsyncResult, int>(ar => { throw oce; })), oce };

            // AsyncTaskMethodBuilder
            var atmb = new AsyncTaskMethodBuilder();
            atmb.SetException(oce);
            yield return new object[] { LineNumber(), atmb.Task, oce };
        }

        private static int LineNumber([CallerLineNumber]int lineNumber = 0) => lineNumber;

        private class ValidateCorrectContextSynchronizationContext : SynchronizationContext
        {
            [ThreadStatic]
            internal static bool t_isPostedInContext;

            internal int PostCount;
            internal int SendCount;

            public override void Post(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref PostCount);
                Task.Run(() =>
                {
                    try
                    {
                        t_isPostedInContext = true;
                        d(state);
                    }
                    finally
                    {
                        t_isPostedInContext = false;
                    }
                });
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref SendCount);
                d(state);
            }
        }

        /// <summary>A scheduler that queues to the TP and tracks the number of times QueueTask and TryExecuteTaskInline are invoked.</summary>
        private class QUWITaskScheduler : TaskScheduler
        {
            private int _queueTaskCount;
            private int _tryExecuteTaskInlineCount;

            public int QueueTaskCount { get { return _queueTaskCount; } }
            public int TryExecuteTaskInlineCount { get { return _tryExecuteTaskInlineCount; } }

            protected override IEnumerable<Task> GetScheduledTasks() { return null; }

            protected override void QueueTask(Task task)
            {
                Interlocked.Increment(ref _queueTaskCount);
                Task.Run(() => TryExecuteTask(task));
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                Interlocked.Increment(ref _tryExecuteTaskInlineCount);
                return TryExecuteTask(task);
            }
        }

        /// <summary>Runs the action with TaskScheduler.Current equal to the specified scheduler.</summary>
        private static void RunWithSchedulerAsCurrent(TaskScheduler scheduler, Action action)
        {
            var t = new Task(action);
            t.RunSynchronously(scheduler);
            t.GetAwaiter().GetResult();
        }
    }
}
