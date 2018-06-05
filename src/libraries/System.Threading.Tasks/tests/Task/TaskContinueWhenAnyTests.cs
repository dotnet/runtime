// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class TaskContinueWhenAnyTests
    {
        #region TaskFactory.ContinueWhenAny tests

        [Fact]
        public static void RunContinueWhenAnyTests()
        {
            TaskCompletionSource<int> tcs = null;
            ManualResetEvent mre1 = null;
            ManualResetEvent mre2 = null;
            Task[] antecedents;
            Task continuation = null;

            for (int i = 0; i < 2; i++)
            {
                bool antecedentsAreFutures = (i == 0);
                for (int j = 0; j < 2; j++)
                {
                    bool continuationIsFuture = (j == 0);
                    for (int k = 0; k < 2; k++)
                    {
                        bool preCanceledToken = (k == 0);
                        CancellationTokenSource cts = new CancellationTokenSource();
                        CancellationToken ct = cts.Token;
                        if (preCanceledToken)
                            cts.Cancel();

                        for (int x = 0; x < 2; x++)
                        {
                            bool longRunning = (x == 0);
                            TaskContinuationOptions tco = longRunning ? TaskContinuationOptions.LongRunning : TaskContinuationOptions.None;

                            for (int y = 0; y < 2; y++)
                            {
                                bool preCompletedTask = (y == 0);

                                for (int z = 0; z < 2; z++)
                                {
                                    bool useFutureFactory = (z == 0);

                                    // This would be a nonsensical combination
                                    if (useFutureFactory && !continuationIsFuture)
                                        continue;

                                    //Assert.True(false, string.Format(" - Test Task{5}.Factory.ContinueWhenAny(Task{0}[]({1} completed), {2}, ct({3}), {4}, ts.Default)",
                                    //    antecedentsAreFutures ? "<int>" : "",
                                    //    preCompletedTask ? 1 : 0,
                                    //    continuationIsFuture ? "func" : "action",
                                    //    preCanceledToken ? "signaled" : "unsignaled",
                                    //    tco,
                                    //    useFutureFactory ? "<int>" : ""));

                                    TaskScheduler ts = TaskScheduler.Default;

                                    if (antecedentsAreFutures)
                                        antecedents = new Task<int>[3];
                                    else
                                        antecedents = new Task[3];

                                    tcs = new TaskCompletionSource<int>();
                                    mre1 = new ManualResetEvent(false);
                                    mre2 = new ManualResetEvent(false);
                                    continuation = null;




                                    if (antecedentsAreFutures)
                                    {
                                        antecedents[0] = new Task<int>(() => { mre2.WaitOne(); return 0; });
                                        antecedents[1] = new Task<int>(() => { mre1.WaitOne(); return 1; });
                                        antecedents[2] = new Task<int>(() => { mre2.WaitOne(); return 2; });
                                    }
                                    else
                                    {
                                        antecedents[0] = new Task(() => { mre2.WaitOne(); tcs.TrySetResult(0); });
                                        antecedents[1] = new Task(() => { mre1.WaitOne(); tcs.TrySetResult(1); });
                                        antecedents[2] = new Task(() => { mre2.WaitOne(); tcs.TrySetResult(2); });
                                    }

                                    if (preCompletedTask)
                                    {
                                        mre1.Set();
                                        antecedents[1].Start();
                                        antecedents[1].Wait();
                                    }

                                    if (continuationIsFuture)
                                    {
                                        if (antecedentsAreFutures)
                                        {
                                            if (useFutureFactory)
                                            {
                                                continuation = Task<int>.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => { tcs.TrySetResult(t.Result); return 10; }, ct, tco, ts);
                                            }
                                            else
                                            {
                                                continuation = Task.Factory.ContinueWhenAny<int, int>((Task<int>[])antecedents, t => { tcs.TrySetResult(t.Result); return 10; }, ct, tco, ts);
                                            }
                                        }
                                        else // antecedents are tasks
                                        {
                                            if (useFutureFactory)
                                            {
                                                continuation = Task<int>.Factory.ContinueWhenAny(antecedents, _ => 10, ct, tco, ts);
                                            }
                                            else
                                            {
                                                continuation = Task.Factory.ContinueWhenAny<int>(antecedents, _ => 10, ct, tco, ts);
                                            }
                                        }
                                    }
                                    else // continuation is task
                                    {
                                        if (antecedentsAreFutures)
                                        {
                                            continuation = Task.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => tcs.TrySetResult(t.Result), ct, tco, ts);
                                        }
                                        else
                                        {
                                            continuation = Task.Factory.ContinueWhenAny(antecedents, _ => { }, ct, tco, ts);
                                        }
                                    }

                                    // If we have a pre-canceled token, the continuation should have completed by now
                                    Assert.False(preCanceledToken && !continuation.IsCompleted, "    > FAILED.  Continuation should complete early on pre-canceled ct");

                                    // Slightly different than the previous assert:
                                    //    We should only have completed by now if we have a preCanceledToken or a preCompletedTask
                                    Assert.True(!continuation.IsCompleted || preCompletedTask || preCanceledToken, "    > FAILED! Continuation should fire early only if (preCanceledToken or preCompletedTask)(1).");

                                    // Kick off our antecedents array
                                    startTaskArray(antecedents);
                                    //Thread.Sleep(50);

                                    // re-assert that the only way that the continuation should have completed by now is preCompletedTask or preCanceledToken
                                    Assert.True(!continuation.IsCompleted || preCompletedTask || preCanceledToken, "    > FAILED! Continuation should fire early only if (preCanceledToken or preCompletedTask)(2).");

                                    // signal mre1 if we have not done so already
                                    if (!preCompletedTask)
                                        mre1.Set();

                                    Exception ex = null;
                                    int result = 0;

                                    try
                                    {
                                        if (continuationIsFuture)
                                            result = ((Task<int>)continuation).Result;
                                        else
                                            continuation.Wait();
                                    }
                                    catch (Exception e)
                                    {
                                        ex = e;
                                    }

                                    Assert.True((ex == null) == !preCanceledToken,
                                       "RunContinueWhenAnyTests: > FAILED! continuation.Wait() should throw exception iff preCanceledToken");
                                    if (preCanceledToken)
                                    {
                                        if (ex == null)
                                        {
                                            Assert.True(false, string.Format("RunContinueWhenAnyTests: > FAILED!  Expected AE<TCE> from continuation.Wait() (no exception thrown)"));
                                            ;
                                        }
                                        else if (ex.GetType() != typeof(AggregateException))
                                        {
                                            Assert.True(false, string.Format("RunContinueWhenAnyTests: > FAILED!  Expected AE<TCE> from continuation.Wait() (didn't throw aggregate exception)"));
                                        }
                                        else if (((AggregateException)ex).InnerException.GetType() != typeof(TaskCanceledException))
                                        {
                                            ex = ((AggregateException)ex).InnerException;
                                            Assert.True(false, string.Format("RunContinueWhenAnyTests: > FAILED!  Expected AE<TCE> from continuation.Wait() (threw " + ex.GetType().Name + " instead of TaskCanceledException)"));
                                        }
                                    }

                                    Assert.True(preCanceledToken || (tcs.Task.Result == 1),
                                       "RunContinueWhenAnyTests: > FAILED!  Wrong task was recorded as completed.");
                                    Assert.True((result == 10) || !continuationIsFuture || preCanceledToken,
                                       "RunContinueWhenAnyTests:> FAILED! continuation yielded wrong result");

                                    Assert.Equal((continuation.CreationOptions & TaskCreationOptions.LongRunning) != 0, longRunning);
                                    Assert.True((continuation.CreationOptions == TaskCreationOptions.None) || longRunning, "continuation CreationOptions should be None unless longRunning is true");

                                    // Allow remaining antecedents to finish
                                    mre2.Set();

                                    // Make sure that you wait for the antecedents to complete.
                                    // When this line wasn't here, antecedent completion could sneak into
                                    // the next loop iteration, causing tcs to be set to 0 or 2 instead of 1,
                                    // resulting in intermittent test failures.
                                    Task.WaitAll(antecedents);

                                    // We don't need to call this for every combination of i/j/k/x/y/z.  So only
                                    // call under these conditions.
                                    if (preCanceledToken && longRunning && preCompletedTask)
                                    {
                                        TestContinueWhenAnyException(antecedents, useFutureFactory, continuationIsFuture);
                                    }
                                } //end z-loop (useFutureFactory)
                            } // end y-loop (preCompletedTask)
                        } // end x-loop (longRunning)
                    } // end k-loop (preCanceledToken)
                } // end j-loop (continuationIsFuture)
            } // end i-loop (antecedentsAreFutures)
        }

        public static void TestContinueWhenAnyException(Task[] antecedents, bool FutureFactory, bool continuationIsFuture)
        {
            bool antecedentsAreFutures = (antecedents as Task<int>[]) != null;

            Debug.WriteLine(" * Test Exceptions in TaskFactory{0}.ContinueWhenAny(Task{1}[],Task{2})",
                FutureFactory ? "<TResult>" : "",
                antecedentsAreFutures ? "<TResult>" : "",
                continuationIsFuture ? "<TResult>" : "");

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            cts.Cancel();

            Task t1 = Task.Factory.StartNew(() => { });
            Task<int> f1 = Task<int>.Factory.StartNew(() => 10);
            Task[] dummyTasks = new Task[] { t1 };
            Task<int>[] dummyFutures = new Task<int>[] { f1 };

            if (FutureFactory) //TaskFactory<TResult> methods
            {
                if (antecedentsAreFutures)
                {
                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => 0, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                    Assert.Throws<ArgumentOutOfRangeException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => 0, TaskContinuationOptions.NotOnFaulted); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>(null, t => 0); });

                    var cFuture = Task.Factory.ContinueWhenAny<int, int>((Task<int>[])antecedents, t => 0, ct);
                    CheckForCorrectCT(cFuture, ct);
                    antecedents[0] = null;

                    Assert.Throws<ArgumentException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => 0); });


                    AssertExtensions.Throws<ArgumentException>("tasks", () => Task<int>.Factory.ContinueWhenAny(new Task<int>[0], t => 0));

                    //
                    // Test for exception on null continuation function
                    //
                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>(dummyFutures, (Func<Task<int>, int>)null); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>(dummyFutures, (Func<Task<int>, int>)null, CancellationToken.None); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>(dummyFutures, (Func<Task<int>, int>)null, TaskContinuationOptions.None); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny<int>(dummyFutures, (Func<Task<int>, int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });
                }
                else //antecedents are tasks
                {
                    var dummy = Task.Factory.StartNew(delegate { });
                    Assert.Throws<ArgumentOutOfRangeException>(
                       () => { Task<int>.Factory.ContinueWhenAny(new Task[] { dummy }, t => 0, TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously); });
                    dummy.Wait();


                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny(antecedents, t => 0, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                    Assert.Throws<ArgumentOutOfRangeException>(
                       () => { Task<int>.Factory.ContinueWhenAny(antecedents, t => 0, TaskContinuationOptions.NotOnFaulted); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny(null, t => 0); });

                    var cTask = Task.Factory.ContinueWhenAny(antecedents, t => 0, ct);
                    CheckForCorrectCT(cTask, ct);
                    antecedents[0] = null;

                    Assert.Throws<ArgumentException>(
                       () => { Task<int>.Factory.ContinueWhenAny(antecedents, (t) => 0); });


                    AssertExtensions.Throws<ArgumentException>("tasks", () => Task<int>.Factory.ContinueWhenAny(new Task[0], t => 0));

                    //
                    // Test for exception on null continuation function
                    //
                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny(dummyTasks, (Func<Task, int>)null); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny(dummyTasks, (Func<Task, int>)null, CancellationToken.None); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny(dummyTasks, (Func<Task, int>)null, TaskContinuationOptions.None); });

                    Assert.Throws<ArgumentNullException>(
                       () => { Task<int>.Factory.ContinueWhenAny(dummyTasks, (Func<Task, int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });
                }
            }
            else //TaskFactory methods
            {
                //test exceptions
                if (continuationIsFuture)
                {
                    if (antecedentsAreFutures)
                    {
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>((Task<int>[])antecedents, t => 0, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                        Assert.Throws<ArgumentOutOfRangeException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>((Task<int>[])antecedents, t => 0, TaskContinuationOptions.NotOnFaulted); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>(null, t => 0); });

                        var cTask = Task.Factory.ContinueWhenAny<int, int>((Task<int>[])antecedents, t => 0, ct);
                        CheckForCorrectCT(cTask, ct);
                        antecedents[0] = null;


                        Assert.Throws<ArgumentException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>((Task<int>[])antecedents, t => 0); });

                        AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAny(new Task<int>[0], t => 0));

                        //
                        // Test for exception on null continuation function
                        //
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>(dummyFutures, (Func<Task<int>, int>)null); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>(dummyFutures, (Func<Task<int>, int>)null, CancellationToken.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>(dummyFutures, (Func<Task<int>, int>)null, TaskContinuationOptions.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int, int>(dummyFutures, (Func<Task<int>, int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });
                    }

                    else // antecedents are tasks
                    {
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(antecedents, t => 0, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                        Assert.Throws<ArgumentOutOfRangeException>(
                           () => { Task.Factory.ContinueWhenAny<int>(antecedents, t => 0, TaskContinuationOptions.NotOnFaulted); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(null, t => 0); });

                        var cTask = Task.Factory.ContinueWhenAny(antecedents, delegate (Task t) { }, ct);
                        CheckForCorrectCT(cTask, ct);
                        antecedents[0] = null;

                        Assert.Throws<ArgumentException>(
                           () => { Task.Factory.ContinueWhenAny<int>(antecedents, t => 0); });


                        AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAny(new Task[0], t => 0));

                        //
                        // Test for exception on null continuation function
                        //
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyTasks, (Func<Task, int>)null); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyTasks, (Func<Task, int>)null, CancellationToken.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyTasks, (Func<Task, int>)null, TaskContinuationOptions.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyTasks, (Func<Task, int>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });
                    }
                }

                else //Continuation is task
                {
                    if (antecedentsAreFutures)
                    {
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                        Assert.Throws<ArgumentOutOfRangeException>(
                           () => { Task.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => { }, TaskContinuationOptions.NotOnFaulted); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(null, t => { }); });

                        var cTask = Task.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => { }, ct);
                        CheckForCorrectCT(cTask, ct);
                        antecedents[0] = null;


                        Assert.Throws<ArgumentException>(
                            () => { Task.Factory.ContinueWhenAny<int>((Task<int>[])antecedents, t => { }); });

                        AssertExtensions.Throws<ArgumentException>("tasks", () => Task.Factory.ContinueWhenAny(new Task<int>[] { }, t => { }));

                        //
                        // Test for exception on null continuation action
                        //
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyFutures, (Action<Task<int>>)null); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyFutures, (Action<Task<int>>)null, CancellationToken.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyFutures, (Action<Task<int>>)null, TaskContinuationOptions.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny<int>(dummyFutures, (Action<Task<int>>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });
                    }
                    else // antecedents are tasks
                    {
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny(antecedents, t => { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

                        Assert.Throws<ArgumentOutOfRangeException>(
                           () => { Task.Factory.ContinueWhenAny(antecedents, t => { }, TaskContinuationOptions.NotOnFaulted); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny(null, t => { }); });

                        var task = Task.Factory.ContinueWhenAny(antecedents, t => { }, ct);
                        CheckForCorrectCT(task, ct);
                        antecedents[0] = null;


                        Assert.Throws<ArgumentException>(
                           () => { Task.Factory.ContinueWhenAny(antecedents, t => { }); });

                        AssertExtensions.Throws<ArgumentException>("tasks",() => Task.Factory.ContinueWhenAny(new Task[0], t => { }));

                        //
                        // Test for exception on null continuation action
                        //
                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny(dummyTasks, (Action<Task>)null); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny(dummyTasks, (Action<Task>)null, CancellationToken.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny(dummyTasks, (Action<Task>)null, TaskContinuationOptions.None); });

                        Assert.Throws<ArgumentNullException>(
                           () => { Task.Factory.ContinueWhenAny(dummyTasks, (Action<Task>)null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default); });
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        // used in ContinueWhenAll/ContinueWhenAny tests
        public static void startTaskArray(Task[] tasks)
        {
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].Status == TaskStatus.Created)
                    tasks[i].Start();
            }
        }

        public static void CheckForCorrectCT(Task canceledTask, CancellationToken correctToken)
        {
            try
            {
                canceledTask.Wait();
                Assert.True(false, string.Format("    > FAILED!  Pre-canceled result did not throw from Wait()"));
            }
            catch (AggregateException ae)
            {
                ae.Flatten().Handle(e =>
                {
                    var tce = e as TaskCanceledException;
                    if (tce == null)
                    {
                        Assert.True(false, string.Format("    > FAILED!  Pre-canceled result threw non-TCE from Wait()"));
                    }
                    else if (tce.CancellationToken != correctToken)
                    {
                        Assert.True(false, string.Format("    > FAILED!  Pre-canceled result threw TCE w/ wrong token"));
                    }

                    return true;
                });
            }
        }

        #endregion
    }
}
