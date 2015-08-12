// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests
{
    public class TaskAwaiterTests
    {
        // awaiting tasks
        [Fact]
        [OuterLoop]
        public static void RunAsyncTaskAwaiterTests()
        {
            var completed = new TaskCompletionSource<string>();
            Task task = completed.Task;
            Task<string> taskOfString = completed.Task;
            completed.SetResult("42");

            {
                // IsCompleted/OnCompleted on a non-completed Task with SyncContext and completes the task in another context
                var vccsc = new ValidateCorrectContextSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(vccsc);
                ManualResetEventSlim mres = new ManualResetEventSlim();
                var tcs = new TaskCompletionSource<object>();
                int result = 1;
                var awaiter = ((Task)tcs.Task).GetAwaiter();
                Assert.False(awaiter.IsCompleted, "     > FAILURE. Awaiter on non-completed task should not be IsCompleted");
                awaiter.OnCompleted(() =>
                {
                    Assert.True(ValidateCorrectContextSynchronizationContext.IsPostedInContext, "     > FAILURE. Continuation should be running in captured sync context.");
                    result = 2;
                    mres.Set();
                });
                Assert.True(result == 1, "     > FAILURE. Await continuation should not run until task completes.");
                Task.Run(delegate { tcs.SetResult(null); });
                mres.Wait();
                awaiter.GetResult();
                Assert.True(result == 2, "     > FAILURE. Await continuation should have completed.");
                Assert.True(vccsc.PostCount == 1, "     > FAILURE. Await continuation should have posted to the target context.");
                SynchronizationContext.SetSynchronizationContext(null);
            }

            {
                // IsCompleted/OnCompleted on a non-completed Task with TaskScheduler and completes the task in another context
                var quwi = new QUWITaskScheduler();
                RunWithSchedulerAsCurrent(quwi, delegate
                {
                    ManualResetEventSlim mres = new ManualResetEventSlim();
                    var tcs = new TaskCompletionSource<object>();
                    int result = 1;
                    var awaiter = ((Task)tcs.Task).GetAwaiter();
                    Assert.False(awaiter.IsCompleted, "     > FAILURE. Awaiter on non-completed task should not be IsCompleted");
                    awaiter.OnCompleted(() =>
                    {
                        Assert.True(TaskScheduler.Current == quwi, "     > FAILURE. Continuation should be running in task scheduler.");
                        result = 2;
                        mres.Set();
                    });
                    Assert.True(result == 1, "     > FAILURE. Await continuation should not run until task completes.");
                    Task.Run(delegate { tcs.SetResult(null); });
                    mres.Wait();
                    awaiter.GetResult();
                    Assert.True(result == 2, "     > FAILURE. Await continuation should have completed.");
                });
            }

            {
                // Configured IsCompleted/OnCompleted on a non-completed Task with SyncContext
                for (int iter = 0; iter < 2; iter++)
                {
                    Task.Factory.StartNew(() =>
                    {
                        bool continueOnCapturedContext = iter == 0;
                        SynchronizationContext.SetSynchronizationContext(new ValidateCorrectContextSynchronizationContext());
                        ManualResetEventSlim mres = new ManualResetEventSlim();
                        var tcs = new TaskCompletionSource<object>();
                        int result = 1;
                        var awaiter = ((Task)tcs.Task).ConfigureAwait(continueOnCapturedContext).GetAwaiter();
                        Assert.False(awaiter.IsCompleted, "     > FAILURE. Configured awaiter on non-completed task should not be IsCompleted");
                        awaiter.OnCompleted(() =>
                        {
                            Assert.True(ValidateCorrectContextSynchronizationContext.IsPostedInContext == continueOnCapturedContext,
                            "     > FAILURE. Continuation should have been posted to context iff continueOnCapturedContext == true.");
                            //
                            //    Assert.True(Environment.StackTrace.Contains("SetResult") != continueOnCapturedContext,
                            //    "     > FAILURE. Continuation should have been executed synchronously iff continueOnCapturedContext == false.");
                            result = 2;
                            mres.Set();
                        });
                        Assert.True(result == 1, "     > FAILURE. Await continuation should not have run before task completed.");
                        Task.Factory.StartNew(() => tcs.SetResult(null));
                        mres.Wait();
                        awaiter.GetResult();
                        Assert.True(result == 2, "     > FAILURE. Await continuation should now have completed.");
                        SynchronizationContext.SetSynchronizationContext(null);
                    }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Wait();
                }
            }

            {
                // IsCompleted/OnCompleted on a non-completed Task<TResult> with SyncContext and completes the task in another context
                var vccsc = new ValidateCorrectContextSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(vccsc);
                var mres = new ManualResetEventSlim();
                var tcs = new TaskCompletionSource<object>();
                int result = 1;
                var awaiter = ((Task<object>)tcs.Task).GetAwaiter();
                Assert.False(awaiter.IsCompleted, "     > FAILURE. Awaiter on non-completed Task<TResult> should not be IsCompleted");
                awaiter.OnCompleted(() =>
                {
                    Assert.True(ValidateCorrectContextSynchronizationContext.IsPostedInContext, "     > FAILURE. Await continuation should have posted to target context");
                    result = 2;
                    mres.Set();
                });
                Assert.True(result == 1, "     > FAILURE. Await continuation should not have run before task completed.");
                Task.Run(delegate { tcs.SetResult(null); });
                mres.Wait();
                awaiter.GetResult();
                Assert.True(result == 2, "     > FAILURE. Await continuation should now have completed.");
                Assert.True(vccsc.PostCount == 1, "     > FAILURE. Await continuation should have posted to the target context");
                SynchronizationContext.SetSynchronizationContext(null);
            }

            {
                // Configured IsCompleted/OnCompleted on a non-completed Task<TResult> with SyncContext
                for (int iter = 0; iter < 2; iter++)
                {
                    Task.Factory.StartNew(() =>
                    {
                        bool continueOnCapturedContext = iter == 0;
                        SynchronizationContext.SetSynchronizationContext(new ValidateCorrectContextSynchronizationContext());
                        var mres = new ManualResetEventSlim();
                        var tcs = new TaskCompletionSource<object>();
                        int result = 1;
                        var awaiter = tcs.Task.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
                        Assert.False(awaiter.IsCompleted, "     > FAILURE. Configured awaiter on non-completed Task<TResult> should not be IsCompleted");
                        awaiter.OnCompleted(() =>
                        {
                            Assert.True(
                               ValidateCorrectContextSynchronizationContext.IsPostedInContext == continueOnCapturedContext,
                               "     > FAILURE. Await continuation should have posted to target context iff continueOnCapturedContext == true");
                            // Assert.True(
                            //    Environment.StackTrace.Contains("SetResult") != continueOnCapturedContext,
                            //    "     > FAILURE. Await continuation should have executed inline iff continueOnCapturedContext == false");
                            result = 2;
                            mres.Set();
                        });
                        Assert.True(result == 1, "     > FAILURE. Await continuation should not have run before task completed.");
                        Task.Factory.StartNew(() => tcs.SetResult(null));
                        mres.Wait();
                        awaiter.GetResult();
                        Assert.True(result == 2, "     > FAILURE. Await continuation should now have completed.");
                        SynchronizationContext.SetSynchronizationContext(null);
                    }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Wait();
                }
            }

            {
                // Validate successful GetResult
                task.GetAwaiter().GetResult();
                task.ConfigureAwait(false).GetAwaiter().GetResult();
                task.ConfigureAwait(true).GetAwaiter().GetResult();

                Assert.Equal(taskOfString.GetAwaiter().GetResult(), "42");
                Assert.Equal(taskOfString.ConfigureAwait(false).GetAwaiter().GetResult(), "42");
                Assert.Equal(taskOfString.ConfigureAwait(true).GetAwaiter().GetResult(), "42");
            }

            {
                // Validate GetResult blocks until completion
                var tcs = new TaskCompletionSource<bool>();

                // Kick off tasks that should all block
                var t1 = Task.Factory.StartNew(() => tcs.Task.GetAwaiter().GetResult());
                var t2 = Task.Factory.StartNew(() => ((Task)tcs.Task).GetAwaiter().GetResult());
                var t3 = Task.Factory.StartNew(() => tcs.Task.ConfigureAwait(false).GetAwaiter().GetResult());
                var t4 = Task.Factory.StartNew(() => ((Task)tcs.Task).ConfigureAwait(false).GetAwaiter().GetResult());
                var allTasks = new Task[] { t1, t2, t3, t4 };

                // Wait with timeout should return false
                bool waitCompleted;
                try
                {
                    waitCompleted = Task.WaitAll(allTasks, 4000);
                    Assert.False(waitCompleted, "     > Expected tasks to not be completed");
                }
                catch (Exception exc)
                {
                    Assert.True(false, string.Format("     > Did not expect an exception: " + exc));
                }

                // Now complete the tasks
                tcs.SetResult(true);

                // All tasks should complete successfully
                waitCompleted = Task.WaitAll(allTasks, 4000);
                Assert.True(waitCompleted,
                    "After completion, excepted all GetResult calls to completed successfully");
                foreach (var taskToCheck in allTasks)
                {
                    Assert.True(taskToCheck.Status == TaskStatus.RanToCompletion, "Task was not run to completion. Excepted all GetResult calls to completed successfully");
                }
            }
        }

        [Fact]
        public static void RunAsyncTaskAwaiterTests_Exceptions()
        {
            // Validate cancellation
            var canceled = new TaskCompletionSource<string>();
            canceled.SetCanceled();

            // Task.GetAwaiter and Task<T>.GetAwaiter
            Assert.Throws<TaskCanceledException>(() => { ((Task)canceled.Task).GetAwaiter().GetResult(); });
            Assert.Throws<TaskCanceledException>(() => { ((Task<string>)canceled.Task).GetAwaiter().GetResult(); });

            // w/ ConfigureAwait false and true
            Assert.Throws<TaskCanceledException>(() => { ((Task)canceled.Task).ConfigureAwait(false).GetAwaiter().GetResult(); });
            Assert.Throws<TaskCanceledException>(() => { ((Task)canceled.Task).ConfigureAwait(true).GetAwaiter().GetResult(); });
            Assert.Throws<TaskCanceledException>(() => { ((Task<string>)canceled.Task).ConfigureAwait(false).GetAwaiter().GetResult(); });
            Assert.Throws<TaskCanceledException>(() => { ((Task<string>)canceled.Task).ConfigureAwait(true).GetAwaiter().GetResult(); });
        }

        [Fact]
        public static void AsyncTaskAwaiterTests_EqualityTests()
        {
            var completed = new TaskCompletionSource<string>();
            Task task = completed.Task;

            // Validate awaiter and awaitable equality

            // TaskAwaiter
            task.GetAwaiter().Equals(task.GetAwaiter());
            // ConfiguredTaskAwaitable
            Assert.Equal(((Task)task).ConfigureAwait(false), ((Task)task).ConfigureAwait(false));
            Assert.NotEqual(((Task)task).ConfigureAwait(false), ((Task)task).ConfigureAwait(true));
            Assert.NotEqual(((Task)task).ConfigureAwait(true), ((Task)task).ConfigureAwait(false));

            // ConfiguredTaskAwaitable<T>
            Assert.Equal(task.ConfigureAwait(false), ((Task)task).ConfigureAwait(false));
            Assert.NotEqual(task.ConfigureAwait(false), ((Task)task).ConfigureAwait(true));
            Assert.NotEqual(task.ConfigureAwait(true), ((Task)task).ConfigureAwait(false));

            // ConfiguredTaskAwaitable.ConfiguredTaskAwaiter
            Assert.Equal(((Task)task).ConfigureAwait(false).GetAwaiter(), ((Task)task).ConfigureAwait(false).GetAwaiter());
            Assert.NotEqual(((Task)task).ConfigureAwait(false).GetAwaiter(), ((Task)task).ConfigureAwait(true).GetAwaiter());
            Assert.NotEqual(((Task)task).ConfigureAwait(true).GetAwaiter(), ((Task)task).ConfigureAwait(false).GetAwaiter());

            // ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter
            Assert.Equal(task.ConfigureAwait(false).GetAwaiter(), ((Task)task).ConfigureAwait(false).GetAwaiter());
            Assert.NotEqual(task.ConfigureAwait(false).GetAwaiter(), ((Task)task).ConfigureAwait(true).GetAwaiter());
            Assert.NotEqual(task.ConfigureAwait(true).GetAwaiter(), ((Task)task).ConfigureAwait(false).GetAwaiter());
        }

        [Fact]
        public static void AsyncTaskAwaiterTests_NegativeTests()
        {
            {
                // Validate GetResult on a single exception
                var oneException = new TaskCompletionSource<string>();
                oneException.SetException(new ArgumentException("uh oh"));

                // Task.GetAwaiter and Task<T>.GetAwaiter
                Assert.Throws<ArgumentException>(() => { ((Task)oneException.Task).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task<string>)oneException.Task).GetAwaiter().GetResult(); });

                // w/ ConfigureAwait false and true
                Assert.Throws<ArgumentException>(() => { ((Task)oneException.Task).ConfigureAwait(false).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task)oneException.Task).ConfigureAwait(true).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task<string>)oneException.Task).ConfigureAwait(false).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task<string>)oneException.Task).ConfigureAwait(true).GetAwaiter().GetResult(); });
            }

            {
                // Validate GetResult on multiple exceptions
                var multiException = new TaskCompletionSource<string>();
                multiException.SetException(new Exception[] { new ArgumentException("uh oh"), new InvalidOperationException("uh oh") });

                // Task.GetAwaiter and Task<T>.GetAwaiter
                Assert.Throws<ArgumentException>(() => { ((Task)multiException.Task).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task<string>)multiException.Task).GetAwaiter().GetResult(); });

                // w/ ConfigureAwait false and true
                Assert.Throws<ArgumentException>(() => { ((Task)multiException.Task).ConfigureAwait(false).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task)multiException.Task).ConfigureAwait(true).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task<string>)multiException.Task).ConfigureAwait(false).GetAwaiter().GetResult(); });
                Assert.Throws<ArgumentException>(() => { ((Task<string>)multiException.Task).ConfigureAwait(true).GetAwaiter().GetResult(); });
            }
        }

        [Fact]
        public static void RunAsyncTaskAwaiterAdditionalBehaviorsTests()
        {
            // Test that base SynchronizationContext is treated the same as no SynchronizationContext for awaits
            {
                var quwi = new QUWITaskScheduler();
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                RunWithSchedulerAsCurrent(quwi, delegate
                {
                    ManualResetEventSlim mres = new ManualResetEventSlim();
                    var tcs = new TaskCompletionSource<object>();
                    int result = 1;
                    var awaiter = ((Task)tcs.Task).GetAwaiter();
                    Assert.False(awaiter.IsCompleted, "     > FAILURE. Awaiter on non-completed task should not be IsCompleted");
                    Assert.True(SynchronizationContext.Current != null, "     > FAILURE. Expected a current SyncCtx but found null");
                    awaiter.OnCompleted(() =>
                    {
                        Assert.True(TaskScheduler.Current == quwi, "     > FAILURE. Continuation should be running in task scheduler.");
                        Assert.True(SynchronizationContext.Current == null, "     > FAILURE. Expected no current SyncCtx but found " + SynchronizationContext.Current);
                        result = 2;
                        mres.Set();
                    });
                    Assert.True(result == 1, "     > FAILURE. Await continuation should not run until task completes.");
                    Task.Run(delegate { tcs.SetResult(null); });
                    mres.Wait();
                    awaiter.GetResult();
                    Assert.True(result == 2, "     > FAILURE. Await continuation should have completed.");
                });
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        [Fact]
        public static void AsyncTaskAwaiterTests_CTandOCE()
        {
            // Test that CancellationToken is correctly flowed through await
            {
                var amb = AsyncTaskMethodBuilder.Create();
                var cts = new CancellationTokenSource();
                var oce = new OperationCanceledException(cts.Token);
                amb.SetException(oce);
                try
                {
                    amb.Task.GetAwaiter().GetResult();
                    Assert.True(false, string.Format("     > FAILURE. Faulted task's GetResult should have thrown."));
                }
                catch (OperationCanceledException oceToCheck)
                {
                    Assert.True(oceToCheck.CancellationToken == cts.Token, "     > FAILURE. The tasks token should have equaled the provided token.");
                }
                catch (Exception exc)
                {
                    Assert.True(false, string.Format("     > FAILURE. Exception an OCE rather than a " + exc.GetType()));
                }
            }

            // Test that original OCE is propagated through await
            {
                var tasks = new List<Task>();

                var cts = new CancellationTokenSource();
                var oce = new OperationCanceledException(cts.Token);

                // A Task that throws an exception to cancel
                var b = new Barrier(2);
                Task<int> t2 = Task<int>.Factory.StartNew(() =>
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                    throw oce;
                }, cts.Token);
                Task t1 = t2;
                b.SignalAndWait(); // make sure task is started before we cancel
                cts.Cancel();
                b.SignalAndWait(); // release task to complete
                tasks.Add(t2);

                // A WhenAll Task
                tasks.Add(Task.WhenAll(t1));
                tasks.Add(Task.WhenAll(t1, Task.FromResult(42)));
                tasks.Add(Task.WhenAll(Task.FromResult(42), t1));
                tasks.Add(Task.WhenAll(t1, t1, t1));

                // A WhenAll Task<int[]>
                tasks.Add(Task.WhenAll(t2));
                tasks.Add(Task.WhenAll(t2, Task.FromResult(42)));
                tasks.Add(Task.WhenAll(Task.FromResult(42), t2));
                tasks.Add(Task.WhenAll(t2, t2, t2));

                // A Task.Run Task and Task<int>
                tasks.Add(Task.Run(() => t1));
                tasks.Add(Task.Run(() => t2));

                // A FromAsync Task and Task<int>
                tasks.Add(Task.Factory.FromAsync(t1, new Action<IAsyncResult>(ar => { throw oce; })));
                tasks.Add(Task<int>.Factory.FromAsync(t2, new Func<IAsyncResult, int>(ar => { throw oce; })));

                // Test each kind of task
                foreach (var task in tasks)
                {
                    ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                    try
                    {
                        if (task is Task<int>)
                        {
                            ((Task<int>)task).GetAwaiter().GetResult();
                        }
                        else if (task is Task<int[]>)
                        {
                            ((Task<int[]>)task).GetAwaiter().GetResult();
                        }
                        else
                        {
                            task.GetAwaiter().GetResult();
                        }
                        Assert.True(false, "     > FAILURE. Expected an OCE to be thrown.");
                    }
                    catch (Exception exc)
                    {
                        Assert.True(
                           Object.ReferenceEquals(oce, exc),
                           "     > FAILURE. The thrown exception was not the original instance.");
                    }
                }
            }
        }

        #region Helper Methods / Classes

        private class ValidateCorrectContextSynchronizationContext : SynchronizationContext
        {
            [ThreadStatic]
            internal static bool IsPostedInContext;

            internal int PostCount;
            internal int SendCount;

            public override void Post(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref PostCount);
                Task.Run(() =>
                {
                    IsPostedInContext = true;
                    d(state);
                    IsPostedInContext = false;
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
            t.Wait();
        }

        #endregion
    }
}
