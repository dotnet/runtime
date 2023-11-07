// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
// TPL namespaces
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    //
    // ContinueWith tests.
    //
    public static class TaskContinueWithTests
    {
        #region ContinueWith Tests

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithAsyncStateCheckTests()
        {
            Task t = new Task(() => { });
            var c1 = t.ContinueWith(_ => { });
            var c2 = t.ContinueWith(_ => { return 42; });

            Task<int> f = new Task<int>(() => 1);
            var c3 = f.ContinueWith(_ => { });
            var c4 = f.ContinueWith(antecedent => antecedent.Result);

            t.Start();
            f.Start();

            Task.WaitAll(c1, c2, c3, c4);

            Assert.True(c1.AsyncState == null, "RunContinueWithAsyncStateCheckTests: task=>task continuation leaks state");
            Assert.True(c2.AsyncState == null, "RunContinueWithAsyncStateCheckTests: task=>future continuation leaks state");
            Assert.True(c3.AsyncState == null, "RunContinueWithAsyncStateCheckTests: future=>task continuation leaks state");
            Assert.True(c4.AsyncState == null, "RunContinueWithAsyncStateCheckTests: future=>future continuation leaks state");
        }

        // Stresses on multiple continuations from a single antecedent
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [SkipOnCoreClr("Stress test", ~RuntimeConfiguration.Release)]
        [SkipOnMono("Stress test")]
        public static void RunContinueWithStressTestsNoState()
        {
            int numIterations = 3;
            for (int iteration = 0; iteration < numIterations; iteration++)
            {
                int numToCancel = 5000 * (iteration + 1);
                int numToComplete = 10000;

                // All "leftover" continuations will be immediately scheduled.
                // All "cancel" continuations will be immediately canceled.
                ContinueWithTortureTest(numToCancel, numToComplete, completeAfter: 0, cancelAfter: 0);

                // Some "leftover" continuations will be queued when antecedent completes
                // All "cancel" continuations will be immediately canceled
                ContinueWithTortureTest(numToCancel, numToComplete, completeAfter: 100, cancelAfter: 0);

                // Some "leftover" continuations will be queued when antecedent completes
                // Some "cancel" continuations will be queued when the token is signaled
                ContinueWithTortureTest(numToCancel, numToComplete, completeAfter: 1000, cancelAfter: 100);

                // All "leftover" continuations should be queued when antecedent completes
                // There may or may not be leftover "cancel" continuations when the antecedent completes
                ContinueWithTortureTest(numToCancel, numToComplete, completeAfter: 10000, cancelAfter: 9900);

                // All continuations should be queued when antecedent completes
                ContinueWithTortureTest(numToCancel, numToComplete, completeAfter: 10000, cancelAfter: 10000);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithPreCancelTests()
        {
            Action<Task, bool, string> EnsureCompletionStatus = delegate (Task task, bool shouldBeCompleted, string message)
            {
                if (task.IsCompleted != shouldBeCompleted)
                {
                    Assert.Fail(string.Format("    > FAILED.  {0}.", message));
                }
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            ManualResetEvent mres = new ManualResetEvent(false);
            // Pre-increment the dontCounts for pre-canceled continuations to make final check easier
            // (i.e., all counts should be 1 at end).
            int[] doneCount = { 0, 0, 1, 0, 1, 0 };


            Task t1 = new Task(delegate { doneCount[0]++; });
            Task c1 = t1.ContinueWith(_ => { doneCount[1]++; });
            Task c2 = c1.ContinueWith(_ => { mres.WaitOne(); doneCount[2]++; }, cts.Token);
            Task c3 = c2.ContinueWith(_ => { mres.WaitOne(); doneCount[3]++; });

            Task c4 = c3.ContinueWith(_ => { mres.WaitOne(); doneCount[4]++; }, cts.Token,
                TaskContinuationOptions.LazyCancellation, TaskScheduler.Default);

            Task c5 = c4.ContinueWith(_ => { mres.WaitOne(); doneCount[5]++; });
            EnsureCompletionStatus(c2, true, "RunContinueWithPreCancelTests: c2 should have completed (canceled) upon construction");
            EnsureCompletionStatus(c4, false, "RunContinueWithPreCancelTests: c4 should NOT have completed (canceled) upon construction");
            EnsureCompletionStatus(t1, false, "RunContinueWithPreCancelTests: t1 should NOT have completed before being started");
            EnsureCompletionStatus(c1, false, "RunContinueWithPreCancelTests: c1 should NOT have completed before antecedent completed");
            EnsureCompletionStatus(c3, false, "RunContinueWithPreCancelTests: c3 should NOT have completed before mres was set");
            EnsureCompletionStatus(c5, false, "RunContinueWithPreCancelTests: c5 should NOT have completed before mres was set");

            // These should be done already.  And Faulted.
            EnsureTaskCanceledExceptionThrown(() => { c2.Wait(); }, "RunContinueWithPreCancelTests: Expected c2.Wait to throw AE/TCE");

            mres.Set();
            Debug.WriteLine("RunContinueWithPreCancelTests: Waiting for tasks to complete... if we hang here, something went wrong.");
            c3.Wait();
            EnsureTaskCanceledExceptionThrown(() => { c4.Wait(); }, "RunContinueWithPreCancelTests: Expected c4.Wait to throw AE/TCE");
            c5.Wait();

            EnsureCompletionStatus(t1, false, "RunContinueWithPreCancelTests: t1 should NOT have completed (post-mres.Set()) before being started");
            EnsureCompletionStatus(c1, false, "RunContinueWithPreCancelTests: c1 should NOT have completed (post-mres.Set()) before antecedent completed");

            t1.Start();
            c1.Wait();

            for (int i = 0; i < 6; i++)
            {
                if (doneCount[i] != 1)
                {
                    Assert.Fail(string.Format(string.Format("RunContinueWithPreCancelTests: > FAILED.  doneCount[{0}] should be 1, is {1}", i, doneCount[i])));
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinuationChainingTest()
        {
            int x = 0;
            int y = 0;

            Task t1 = new Task(delegate { x = 1; });
            Task t2 = t1.ContinueWith(delegate (Task t) { y = 1; });
            Task<int> t3 = t2.ContinueWith(delegate (Task t) { return 5; });
            Task<int> t4 = t3.ContinueWith(delegate (Task<int> t) { return Task<int>.Factory.StartNew(delegate { return 10; }); }).Unwrap();
            Task<string> t5 = t4.ContinueWith(delegate (Task<int> t) { return Task<string>.Factory.StartNew(delegate { for (int i = 0; i < 400; i++) ; return "worked"; }); }).Unwrap();

            try
            {
                t1.Start();
                if (!t5.Result.Equals("worked"))
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest: > FAILED! t5.Result should be \"worked\", is {0}", t5.Result));
                }
                if (t4.Result != 10)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest: > FAILED! t4.Result should be 10, is {0}", t4.Result));
                }
                if (t3.Result != 5)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest: > FAILED! t3.Result should be 5, is {0}", t3.Result));
                }
                if (y != 1)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest: > FAILED! y should be 1, is {0}", y));
                }
                if (x != 1)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest: > FAILED! x should be 1, is {0}", x));
                }
            }
            catch (Exception e)
            {
                Assert.Fail(string.Format("RunContinuationChainingTest: > FAILED! Exception = {0}", e));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithParamsTest_Cancellation()
        {
            //
            // Test whether parentage/cancellation is working correctly
            //
            Task c1b = null, c1c = null;
            Task c2b = null, c2c = null;

            Task container = new Task(delegate
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                Task child1 = new Task(delegate { }, cts.Token, TaskCreationOptions.AttachedToParent);
                Task child2 = new Task(delegate { }, TaskCreationOptions.AttachedToParent);

                c1b = child1.ContinueWith((_) => { }, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.AttachedToParent);
                c1c = child1.ContinueWith((_) => { }, TaskContinuationOptions.AttachedToParent);

                c2b = child2.ContinueWith((_) => { }, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
                c2c = child2.ContinueWith((_) => { }, TaskContinuationOptions.AttachedToParent);

                cts.Cancel(); // should cancel the unstarted child task
                child2.Start();
            });

            container.Start();
            try { container.Wait(); }
            catch { }

            if (c1b.Status != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest: > FAILED.  Continuation task w/NotOnCanceled should have been canceled when antecedent was canceled."));
            }
            if (c1c.Status != TaskStatus.RanToCompletion)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest: > FAILED.  Continuation task w/ canceled antecedent should have run to completion."));
            }
            if (c2b.Status != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest: > FAILED.  Continuation task w/NotOnRanToCompletion should have been canceled when antecedent completed."));
            }
            c2c.Wait();
            if (c2c.Status != TaskStatus.RanToCompletion)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest: > FAILED.  Continuation task w/ completed antecedent should have run to completion."));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithParamsTest_IllegalArgs()
        {
            Task t1 = new Task(delegate { });

            try
            {
                Task t2 = t1.ContinueWith((ooo) => { }, (TaskContinuationOptions)0x1000000);
                Assert.Fail(string.Format("RunContinueWithParamsTest: > FAILED.  Should have seen exception from illegal continuation options."));
            }
            catch { }

            try
            {
                Task t2 = t1.ContinueWith((ooo) => { }, TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously);
                Assert.Fail(string.Format("RunContinueWithParamsTest: > FAILED.  Should have seen exception when combining LongRunning and ExecuteSynchronously"));
            }
            catch { }

            try
            {
                Task t2 = t1.ContinueWith((ooo) => { },
                            TaskContinuationOptions.NotOnRanToCompletion |
                            TaskContinuationOptions.NotOnFaulted |
                            TaskContinuationOptions.NotOnCanceled);
                Assert.Fail(string.Format("RunContinueWithParamsTest: > FAILED.  Should have seen exception from illegal NotOnAny continuation options."));
            }
            catch (Exception)
            { }

            t1.Start();
            t1.Wait();
        }

        // Test what happens when you cancel a task in the middle of a continuation chain.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void RunContinuationCancelTest(bool useTimeSpan)
        {
            bool t1Ran = false;
            bool t3Ran = false;

            Task t1 = new Task(delegate { t1Ran = true; });

            CancellationTokenSource ctsForT2 = new CancellationTokenSource();
            Task t2 = t1.ContinueWith((ContinuedTask) =>
            {
                Assert.Fail(string.Format("RunContinuationCancelTest: > Failed!  t2 should not have run."));
            }, ctsForT2.Token);

            Task t3 = t2.ContinueWith((ContinuedTask) =>
            {
                t3Ran = true;
            });

            // Cancel the middle task in the chain.  Should fire off t3.
            ctsForT2.Cancel();

            // Start the first task in the chain.  Should hold off from kicking off (canceled) t2.
            t1.Start();

            if (useTimeSpan)
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(5000);
                t1.Wait(timeout);
                t3.Wait(timeout);
            }
            else
            {
                t1.Wait(5000); // should be more than enough time for either of these
                t3.Wait(5000);
            }

            if (!t1Ran)
            {
                Assert.Fail(string.Format("RunContinuationCancelTest: > Failed!  t1 should have run."));
            }

            if (!t3Ran)
            {
                Assert.Fail(string.Format("RunContinuationCancelTest: > Failed!  t3 should have run."));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithExceptionTestsNoState()
        {
            //
            // Test exceptional behavior for continuations off of Tasks
            //
            Task t1 = Task.Factory.StartNew(() => { });
            t1.Wait();

            Assert.Throws<ArgumentNullException>(
               () => { t1.ContinueWith((Action<Task>)null); });

            Assert.Throws<ArgumentNullException>(
               () => { t1.ContinueWith(_ => { }, (TaskScheduler)null); });

            Assert.Throws<ArgumentNullException>(
               () => { t1.ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

            Assert.Throws<ArgumentNullException>(
               () => { t1.ContinueWith((Func<Task, int>)null); });

            Assert.Throws<ArgumentNullException>(
               () => { t1.ContinueWith(_ => 5, (TaskScheduler)null); });

            Assert.Throws<ArgumentNullException>(
               () => { t1.ContinueWith(_ => 5, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });
            //
            // Test exceptional behavior for continuations off of Task<int>s
            //
            Task<int> f1 = Task<int>.Factory.StartNew(() => 10);
            f1.Wait();

            Assert.Throws<ArgumentNullException>(
               () => { f1.ContinueWith((Action<Task<int>>)null); });

            Assert.Throws<ArgumentNullException>(
               () => { f1.ContinueWith(_ => { }, (TaskScheduler)null); });

            Assert.Throws<ArgumentNullException>(
               () => { f1.ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });

            Assert.Throws<ArgumentNullException>(
               () => { f1.ContinueWith((Func<Task<int>, int>)null); });

            Assert.Throws<ArgumentNullException>(
               () => { f1.ContinueWith(_ => 5, (TaskScheduler)null); });

            Assert.Throws<ArgumentNullException>(
               () => { f1.ContinueWith(_ => 5, CancellationToken.None, TaskContinuationOptions.None, (TaskScheduler)null); });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithAllParamsTestsNoState()
        {
            for (int i = 0; i < 2; i++)
            {
                bool preCanceled = (i == 0);
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                if (preCanceled) cts.Cancel();

                for (int j = 0; j < 2; j++)
                {
                    bool longRunning = (j == 0);
                    TaskContinuationOptions tco = longRunning ? TaskContinuationOptions.LongRunning : TaskContinuationOptions.None;

                    for (int k = 0; k < 2; k++)
                    {
                        bool antecedentIsFuture = (k == 0);
                        Task antecedent = null;

                        for (int z = 0; z < 2; z++)
                        {
                            bool preCompletedTask = (z == 0);
                            if (preCompletedTask)
                            {
                                if (antecedentIsFuture) antecedent = Task<int>.Factory.StartNew(() => 5);
                                else antecedent = Task.Factory.StartNew(() => { });
                                antecedent.Wait();
                            }


                            for (int x = 0; x < 2; x++)
                            {
                                bool continuationIsFuture = (x == 0);

                                //
                                // Test ContinueWith() overloads that take all parameters
                                //
                                {
                                    Task continuation = null;

                                    if (!preCompletedTask)
                                    {
                                        if (antecedentIsFuture) antecedent = new Task<int>(() => 5);
                                        else antecedent = new Task(() => { });
                                    }

                                    if (continuationIsFuture)
                                    {
                                        if (antecedentIsFuture)
                                        {
                                            //Debug.WriteLine(" - Future = {2}Future.CW(func, ct({0}), tco({1}), TS.Default)", preCanceled ? "signaled" : "unsignaled", tco, preCompletedTask ? "C" : "U");
                                            continuation = ((Task<int>)antecedent).ContinueWith(_ => 5, ct, tco, TaskScheduler.Default);
                                        }
                                        else
                                        {
                                            //Debug.WriteLine(" - Future = {2}Task.CW(func, ct({0}), tco({1}), TS.Default)", preCanceled ? "signaled" : "unsignaled", tco, preCompletedTask ? "C" : "U");
                                            continuation = antecedent.ContinueWith(_ => 5, ct, tco, TaskScheduler.Default);
                                        }
                                    }
                                    else
                                    {
                                        if (antecedentIsFuture)
                                        {
                                            //Debug.WriteLine(" - Task = {2}Future.CW(action, ct({0}), tco({1}), TS.Default)", preCanceled ? "signaled" : "unsignaled", tco, preCompletedTask ? "C" : "U");
                                            continuation = ((Task<int>)antecedent).ContinueWith(_ => { }, ct, tco, TaskScheduler.Default);
                                        }
                                        else
                                        {
                                            //Debug.WriteLine(" - Task = {2}Task.CW(action, ct({0}), tco({1}), TS.Default)", preCanceled ? "signaled" : "unsignaled", tco, preCompletedTask ? "C" : "U");
                                            continuation = antecedent.ContinueWith(_ => { }, ct, tco, TaskScheduler.Default);
                                        }
                                    }

                                    // Start the antecedent, if necessary, or the continuation will never run.
                                    if (!preCompletedTask) antecedent.Start();

                                    Exception ex = null;
                                    int result = 0;

                                    try
                                    {
                                        continuation.Wait();
                                        if (continuationIsFuture) result = ((Task<int>)continuation).Result;
                                    }
                                    catch (Exception e)
                                    {
                                        ex = e;
                                    }

                                    Assert.True((continuation.Status == TaskStatus.RanToCompletion) || preCanceled,
                                       "RunContinueWithAllParamsTestsNoState: Expected continuation end as RanToCompletion unless pre-canceled");
                                    Assert.True((continuation.Status == TaskStatus.Canceled) || !preCanceled,
                                       "RunContinueWithAllParamsTestsNoState: Expected continuation to end as Canceled when pre-canceled");
                                    Assert.True((ex == null) || preCanceled,
                                       "RunContinueWithAllParamsTestsNoState: Got Wait() exception w/o pre-cancellation");
                                    Assert.True(continuation.CreationOptions == (TaskCreationOptions)tco,
                                       "RunContinueWithAllParamsTestsNoState: mis-matched CreationOptions");
                                    Assert.True((result == 5) || (!continuationIsFuture || preCanceled),
                                       "RunContinueWithAllParamsTestsNoState: Expected valid result from non-canceled Future continuation");
                                    if (preCanceled)
                                    {
                                        Assert.True(
                                           (ex != null) &&
                                           (ex.GetType() == typeof(AggregateException)) &&
                                           (((AggregateException)ex).InnerExceptions[0].GetType() == typeof(TaskCanceledException)),
                                           "RunContinueWithAllParamsTestsNoState: Expected AE(TCE) for Wait after pre-cancellation");
                                    }
                                }

                                //
                                // Test ContinueWith() overloads that take CancellationToken
                                //
                                {
                                    Task continuation = null;
                                    if (!preCompletedTask)
                                    {
                                        if (antecedentIsFuture) antecedent = new Task<int>(() => 5);
                                        else antecedent = new Task(() => { });
                                    }


                                    if (continuationIsFuture)
                                    {
                                        if (antecedentIsFuture)
                                        {
                                            //Debug.WriteLine(" - Future = {1}Future.CW(func, ct({0}))", preCanceled ? "signaled" : "unsignaled", preCompletedTask ? "C" : "U");
                                            continuation = ((Task<int>)antecedent).ContinueWith(_ => 5, ct);
                                        }
                                        else
                                        {
                                            //Debug.WriteLine(" - Future = {1}Task.CW(func, ct({0}))", preCanceled ? "signaled" : "unsignaled", preCompletedTask ? "C" : "U");
                                            continuation = antecedent.ContinueWith(_ => 5, ct);
                                        }
                                    }
                                    else
                                    {
                                        if (antecedentIsFuture)
                                        {
                                            //Debug.WriteLine(" - Task = {1}Future.CW(action, ct({0}))", preCanceled ? "signaled" : "unsignaled", preCompletedTask ? "C" : "U");
                                            continuation = ((Task<int>)antecedent).ContinueWith(_ => { }, ct);
                                        }
                                        else
                                        {
                                            //Debug.WriteLine(" - Task = {1}Task.CW(action, ct({0}))", preCanceled ? "signaled" : "unsignaled", preCompletedTask ? "C" : "U");
                                            continuation = antecedent.ContinueWith(_ => { }, ct);
                                        }
                                    }

                                    // Start the antecedent, if necessary, or the continuation will never run.
                                    if (!preCompletedTask) antecedent.Start();

                                    Exception ex = null;
                                    int result = 0;

                                    try
                                    {
                                        continuation.Wait();
                                        if (continuationIsFuture) result = ((Task<int>)continuation).Result;
                                    }
                                    catch (Exception e)
                                    {
                                        ex = e;
                                    }

                                    Assert.True((continuation.Status == TaskStatus.RanToCompletion) || preCanceled,
                                       "RunContinueWithAllParamsTestsNoState overloads: Expected continuation end as RanToCompletion unless pre-canceled");
                                    Assert.True((continuation.Status == TaskStatus.Canceled) || !preCanceled,
                                       "RunContinueWithAllParamsTestsNoState overloads: Expected continuation to end as Canceled when pre-canceled");
                                    Assert.True((ex == null) || preCanceled,
                                       "RunContinueWithAllParamsTestsNoState overloads: Got Wait() exception w/o pre-cancellation");
                                    Assert.True((result == 5) || (!continuationIsFuture || preCanceled),
                                       "RunContinueWithAllParamsTestsNoState overloads: Expected valid result from non-canceled Future continuation");
                                    if (preCanceled)
                                    {
                                        Assert.True(
                                           (ex != null) &&
                                           (ex.GetType() == typeof(AggregateException)) &&
                                           (((AggregateException)ex).InnerExceptions[0].GetType() == typeof(TaskCanceledException)),
                                           "RunContinueWithAllParamsTestsNoState overloads: Expected AE(TCE) for Wait after pre-cancellation");
                                    }
                                }

                                //
                                // Test ContinueWith() overloads that take TaskCreationOptions
                                //
                                {
                                    Task continuation = null;
                                    if (!preCompletedTask)
                                    {
                                        if (antecedentIsFuture) antecedent = new Task<int>(() => 5);
                                        else antecedent = new Task(() => { });
                                    }


                                    if (continuationIsFuture)
                                    {
                                        if (antecedentIsFuture)
                                        {
                                            continuation = ((Task<int>)antecedent).ContinueWith(_ => 5, tco);
                                        }
                                        else
                                        {
                                            continuation = antecedent.ContinueWith(_ => 5, tco);
                                        }
                                    }
                                    else
                                    {
                                        if (antecedentIsFuture)
                                        {
                                            continuation = ((Task<int>)antecedent).ContinueWith(_ => { }, tco);
                                        }
                                        else
                                        {
                                            continuation = antecedent.ContinueWith(_ => { }, tco);
                                        }
                                    }

                                    // Start the antecedent, if necessary, or the continuation will never run.
                                    if (!preCompletedTask) antecedent.Start();

                                    Exception ex = null;
                                    int result = 0;

                                    try
                                    {
                                        continuation.Wait();
                                        if (continuationIsFuture) result = ((Task<int>)continuation).Result;
                                    }
                                    catch (Exception e)
                                    {
                                        ex = e;
                                    }

                                    Assert.True(continuation.Status == TaskStatus.RanToCompletion,
                                       "RunContinueWithAllParamsTestsNoState: Expected continuation to end as RanToCompletion");
                                    Assert.True(ex == null,
                                       "RunContinueWithAllParamsTestsNoState: Got Wait() exception");
                                    Assert.True(continuation.CreationOptions == (TaskCreationOptions)tco,
                                       "RunContinueWithAllParamsTestsNoState: Mis-matched CreationOptions");
                                    Assert.True((result == 5) || (!continuationIsFuture),
                                       "RunContinueWithAllParamsTestsNoState: Expected valid result from Future continuation");
                                }

                                //
                                // The ContinueWith overloads that take a TaskScheduler are already being tested in RunContinueWithTMTests().
                                // So I won't add a block of such tests here.
                                //
                            }
                        }
                    }
                }
            }
        }

        // Make sure that cancellation works for monadic versions of ContinueWith()
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunUnwrapTests()
        {
            Task taskRoot = null;
            Task<int> futureRoot = null;

            Task<int> c1 = null;
            Task<int> c2 = null;
            Task<int> c3 = null;
            Task<int> c4 = null;
            Task c5 = null;
            Task c6 = null;
            Task c7 = null;
            Task c8 = null;

            //
            // Basic functionality tests
            //
            taskRoot = new Task(delegate { });
            futureRoot = new Task<int>(delegate { return 10; });
            ManualResetEvent mres = new ManualResetEvent(false);
            Action<Task, bool, string> checkCompletionState = delegate (Task ctask, bool shouldBeCompleted, string scenario)
            {
                if (ctask.IsCompleted != shouldBeCompleted)
                {
                    Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  {0} expected IsCompleted = {1}", scenario, shouldBeCompleted));
                }
            };

            c1 = taskRoot.ContinueWith((antecedent) => { return Task<int>.Factory.StartNew(delegate { mres.WaitOne(); return 1; }); }).Unwrap();
            c2 = futureRoot.ContinueWith((antecedent) => { return Task<int>.Factory.StartNew(delegate { mres.WaitOne(); return 2; }); }).Unwrap();
            var v3 = new Task<Task<int>>(delegate { return Task<int>.Factory.StartNew(delegate { mres.WaitOne(); return 3; }); });
            c3 = v3.Unwrap();
            c4 = Task.Factory.ContinueWhenAll(new Task[] { taskRoot, futureRoot }, completedTasks =>
            {
                int sum = 0;
                for (int i = 0; i < completedTasks.Length; i++)
                {
                    Task tmp = completedTasks[i];
                    if (tmp is Task<int>) sum += ((Task<int>)tmp).Result;
                }
                return Task.Factory.StartNew(delegate { mres.WaitOne(); return sum; });
            }).Unwrap();
            c5 = taskRoot.ContinueWith((antecedent) => { return Task.Factory.StartNew(delegate { mres.WaitOne(); }); }).Unwrap();
            c6 = futureRoot.ContinueWith((antecedent) => { return Task.Factory.StartNew(delegate { mres.WaitOne(); }); }).Unwrap();
            var v7 = new Task<Task>(delegate { return Task.Factory.StartNew(delegate { mres.WaitOne(); }); });
            c7 = v7.Unwrap();
            c8 = Task.Factory.ContinueWhenAny(new Task[] { taskRoot, futureRoot }, winner =>
            {
                return Task.Factory.StartNew(delegate { mres.WaitOne(); });
            }).Unwrap();

            //Debug.WriteLine(" Testing that Unwrap() products do not complete before antecedent starts...");
            checkCompletionState(c1, false, "Task ==> Task<T>, antecedent unstarted");
            checkCompletionState(c2, false, "Task<T> ==> Task<T>, antecedent unstarted");
            checkCompletionState(c3, false, "StartNew ==> Task<T>, antecedent unstarted");
            checkCompletionState(c4, false, "ContinueWhenAll => Task<T>, antecedent unstarted");
            checkCompletionState(c5, false, "Task ==> Task, antecedent unstarted");
            checkCompletionState(c6, false, "Task<T> ==> Task, antecedent unstarted");
            checkCompletionState(c7, false, "StartNew ==> Task, antecedent unstarted");
            checkCompletionState(c8, false, "ContinueWhenAny => Task, antecedent unstarted");

            taskRoot.Start();
            futureRoot.Start();
            v3.Start();
            v7.Start();

            //Debug.WriteLine(" Testing that Unwrap() products do not complete before proxy source completes...");
            checkCompletionState(c1, false, "Task ==> Task<T>, source task incomplete");
            checkCompletionState(c2, false, "Task<T> ==> Task<T>, source task incomplete");
            checkCompletionState(c3, false, "StartNew ==> Task<T>, source task incomplete");
            checkCompletionState(c4, false, "ContinueWhenAll => Task<T>, source task incomplete");
            checkCompletionState(c5, false, "Task ==> Task, source task incomplete");
            checkCompletionState(c6, false, "Task<T> ==> Task, source task incomplete");
            checkCompletionState(c7, false, "StartNew ==> Task, source task incomplete");
            checkCompletionState(c8, false, "ContinueWhenAny => Task, source task incomplete");

            mres.Set();
            Debug.WriteLine("RunUnwrapTests:  Waiting on Unwrap() products... If we hang, something is wrong.");
            Task.WaitAll(new Task[] { c1, c2, c3, c4, c5, c6, c7, c8 });

            //Debug.WriteLine("    Testing that Unwrap() products have consistent completion state...");
            checkCompletionState(c1, true, "Task ==> Task<T>, Unwrapped task complete");
            checkCompletionState(c2, true, "Task<T> ==> Task<T>, Unwrapped task complete");
            checkCompletionState(c3, true, "StartNew ==> Task<T>, Unwrapped task complete");
            checkCompletionState(c4, true, "ContinueWhenAll => Task<T>, Unwrapped task complete");
            checkCompletionState(c5, true, "Task ==> Task, Unwrapped task complete");
            checkCompletionState(c6, true, "Task<T> ==> Task, Unwrapped task complete");
            checkCompletionState(c7, true, "StartNew ==> Task, Unwrapped task complete");
            checkCompletionState(c8, true, "ContinueWhenAny => Task, Unwrapped task complete");

            if (c1.Result != 1)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected c1.Result = 1, got {0}", c1.Result));
            }

            if (c2.Result != 2)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected c2.Result = 2, got {0}", c2.Result));
            }

            if (c3.Result != 3)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected c3.Result = 3, got {0}", c3.Result));
            }

            if (c4.Result != 10)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected c4.Result = 10, got {0}", c4.Result));
            }

            ////
            //// Test against buggy schedulers
            ////
            //
            //// More specifically, ensure that inline execution via synchronous continuations
            //// causes the predictable exception from the NonInliningTaskScheduler.
            //
            //Task<Task> t1 = null;
            //Task t2 = null;
            //Task hanging1 = new TaskFactory(new NonInliningTaskScheduler()).StartNew(() =>
            //{
            //    // To avoid fast-path optimizations in Unwrap, ensure that both inner
            //    // and outer tasks are not completed before Unwrap is called.  (And a
            //    // good way to do this is to ensure that they are not even started!)
            //    Task inner = new Task(() => { });
            //    t1 = new Task<Task>(() => inner, TaskCreationOptions.AttachedToParent);
            //    t2 = t1.Unwrap();
            //    t1.Start();
            //    inner.Start();
            //});
            //
            //Debug.WriteLine("Buggy Scheduler Test 1 about to wait -- if we hang, we have a problem...");
            //
            //// Wait for task to complete, but do *not* inline it.
            //((IAsyncResult)hanging1).AsyncWaitHandle.WaitOne();
            //
            //try
            //{
            //    hanging1.Wait();
            //    Assert.Fail(string.Format("    > FAILED. Expected an exception."));
            //    return false;
            //}
            //catch (Exception e) { }
            //
            //Task hanging2 = new TaskFactory(new NonInliningTaskScheduler()).StartNew(() =>
            //{
            //    // To avoid fast-path optimizations in Unwrap, ensure that both inner
            //    // and outer tasks are not completed before Unwrap is called.  (And a
            //    // good way to do this is to ensure that they are not even started!)
            //    Task<int> inner = new Task<int>(() => 10);
            //    Task<Task<int>> f1 = new Task<Task<int>>(() => inner, TaskCreationOptions.AttachedToParent);
            //    Task<int> f2 = f1.Unwrap();
            //    f1.Start();
            //    inner.Start();
            //});
            //
            //Debug.WriteLine("Buggy Scheduler Test 2 about to wait -- if we hang, we have a problem...");
            //
            //// Wait for task to complete, but do *not* inline it.
            //((IAsyncResult)hanging2).AsyncWaitHandle.WaitOne();
            //
            //try
            //{
            //    hanging2.Wait();
            //    Assert.Fail(string.Format("    > FAILED. Expected an exception."));
            //    return false;
            //}
            //catch (Exception e) {  }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunUnwrapTests_ExceptionTests()
        {
            Task taskRoot = null;
            Task<int> futureRoot = null;

            Task<int> c1 = null;
            Task<int> c2 = null;
            Task<int> c3 = null;
            Task<int> c4 = null;
            Task c5 = null;
            Task c6 = null;
            Task c7 = null;
            Task c8 = null;

            Action doExc = delegate { throw new Exception("some exception"); };
            //
            // Exception tests
            //
            taskRoot = new Task(delegate { });
            futureRoot = new Task<int>(delegate { return 10; });
            c1 = taskRoot.ContinueWith(delegate (Task t) { doExc(); return Task<int>.Factory.StartNew(delegate { return 1; }); }).Unwrap();
            c2 = futureRoot.ContinueWith(delegate (Task<int> t) { doExc(); return Task<int>.Factory.StartNew(delegate { return 2; }); }).Unwrap();
            c3 = taskRoot.ContinueWith(delegate (Task t) { return Task<int>.Factory.StartNew(delegate { doExc(); return 3; }); }).Unwrap();
            c4 = futureRoot.ContinueWith(delegate (Task<int> t) { return Task<int>.Factory.StartNew(delegate { doExc(); return 4; }); }).Unwrap();
            c5 = taskRoot.ContinueWith(delegate (Task t) { doExc(); return Task.Factory.StartNew(delegate { }); }).Unwrap();
            c6 = futureRoot.ContinueWith(delegate (Task<int> t) { doExc(); return Task.Factory.StartNew(delegate { }); }).Unwrap();
            c7 = taskRoot.ContinueWith(delegate (Task t) { return Task.Factory.StartNew(delegate { doExc(); }); }).Unwrap();
            c8 = futureRoot.ContinueWith(delegate (Task<int> t) { return Task.Factory.StartNew(delegate { doExc(); }); }).Unwrap();
            taskRoot.Start();
            futureRoot.Start();

            Action<Task, string> excTest = delegate (Task ctask, string scenario)
            {
                try
                {
                    ctask.Wait();
                    Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Exception in {0} did not throw on Wait().", scenario));
                }
                catch (AggregateException) { }
                catch (Exception)
                {
                    Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Exception in {0} threw wrong exception.", scenario));
                }
                if (ctask.Status != TaskStatus.Faulted)
                {
                    Assert.Fail(string.Format("RunUnwrapTests: > FAILED. Exception in {0} resulted in wrong status: {1}", scenario, ctask.Status));
                }
            };

            excTest(c1, "Task->Task<int> outer delegate");
            excTest(c2, "Task<int>->Task<int> outer delegate");
            excTest(c3, "Task->Task<int> inner delegate");
            excTest(c4, "Task<int>->Task<int> inner delegate");
            excTest(c5, "Task->Task outer delegate");
            excTest(c6, "Task<int>->Task outer delegate");
            excTest(c7, "Task->Task inner delegate");
            excTest(c8, "Task<int>->Task inner delegate");

            try
            {
                taskRoot.Wait();
                futureRoot.Wait();
            }
            catch (Exception e)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Exception thrown while waiting for task/futureRoots used for exception testing: {0}", e));
            }


            //
            // Exception handling
            //
            var c = Task.Factory.StartNew(() => { }).ContinueWith(_ =>
                Task.Factory.StartNew(() =>
                {
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #1"); }, TaskCreationOptions.AttachedToParent);
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #2"); }, TaskCreationOptions.AttachedToParent);
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #3"); }, TaskCreationOptions.AttachedToParent);
                    Task.Factory.StartNew(delegate { throw new Exception("uh oh #4"); }, TaskCreationOptions.AttachedToParent);
                    return 1;
                })
            ).Unwrap();

            try
            {
                c.Wait();
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Monadic continuation w/ excepted children failed to throw an exception."));
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Count != 4)
                {
                    Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Monadic continuation w/ faulted children had {0} inner exceptions, expected 4", ae.InnerExceptions.Count));
                    Assert.Fail(string.Format("RunUnwrapTests: > Exception = {0}", ae));
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunUnwrapTests_CancellationTests()
        {
            Task taskRoot = null;
            Task<int> futureRoot = null;

            Task<int> c1 = null;
            Task<int> c2 = null;
            Task c5 = null;
            Task c6 = null;
            int c1val = 0;
            int c2val = 0;
            int c5val = 0;
            int c6val = 0;

            //
            // Cancellation tests
            //
            CancellationTokenSource ctsForContainer = new CancellationTokenSource();
            CancellationTokenSource ctsForC1 = new CancellationTokenSource();
            CancellationTokenSource ctsForC2 = new CancellationTokenSource();
            CancellationTokenSource ctsForC5 = new CancellationTokenSource();
            CancellationTokenSource ctsForC6 = new CancellationTokenSource();

            ManualResetEvent mres = new ManualResetEvent(false);

            taskRoot = new Task(delegate { });
            futureRoot = new Task<int>(delegate { return 20; });
            Task container = Task.Factory.StartNew(delegate
            {
                c1 = taskRoot.ContinueWith(delegate (Task antecedent)
                {
                    Task<int> rval = new Task<int>(delegate { c1val = 1; return 10; });
                    return rval;
                }, ctsForC1.Token).Unwrap();

                c2 = futureRoot.ContinueWith(delegate (Task<int> antecedent)
                {
                    Task<int> rval = new Task<int>(delegate { c2val = 1; return 10; });
                    return rval;
                }, ctsForC2.Token).Unwrap();

                c5 = taskRoot.ContinueWith(delegate (Task antecedent)
                {
                    Task rval = new Task(delegate { c5val = 1; });
                    return rval;
                }, ctsForC5.Token).Unwrap();

                c6 = futureRoot.ContinueWith(delegate (Task<int> antecedent)
                {
                    Task rval = new Task(delegate { c6val = 1; });
                    return rval;
                }, ctsForC6.Token).Unwrap();

                mres.Set();

                ctsForContainer.Cancel();
            }, ctsForContainer.Token);

            // Wait for c1, c2 to get initialized.
            mres.WaitOne();

            ctsForC1.Cancel();
            try
            {
                c1.Wait();
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected Wait() to throw after cancellation of Task->Task<int>."));
            }
            catch { }
            TaskStatus ts = c1.Status;
            if (ts != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Direct cancellation of returned Task->Task<int> did not work -- status = {0}", ts));
            }

            ctsForC2.Cancel();
            try
            {
                c2.Wait();
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected Wait() to throw after cancellation of Task<int>->Task<int>."));
            }
            catch { }
            ts = c2.Status;
            if (ts != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Direct cancellation of returned Task<int>->Task<int> did not work -- status = {0}", ts));
            }

            ctsForC5.Cancel();
            try
            {
                c5.Wait();
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected Wait() to throw after cancellation of Task->Task."));
            }
            catch { }
            ts = c5.Status;
            if (ts != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Direct cancellation of returned Task->Task did not work -- status = {0}", ts));
            }

            ctsForC6.Cancel();
            try
            {
                c6.Wait();
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Expected Wait() to throw after cancellation of Task<int>->Task."));
            }
            catch { }
            ts = c6.Status;
            if (ts != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Direct cancellation of returned Task<int>->Task did not work -- status = {0}", ts));
            }

            Debug.WriteLine("RunUnwrapTests: Waiting for container... if we deadlock, cancellations are not being cleaned up properly.");
            container.Wait();

            taskRoot.Start();
            futureRoot.Start();

            try
            {
                taskRoot.Wait();
                futureRoot.Wait();
            }
            catch (Exception e)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Exception thrown when root tasks were started and waited upon: {0}", e));
            }

            if (c1val != 0)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Cancellation of Task->Task<int> failed to stop internal continuation"));
            }

            if (c2val != 0)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Cancellation of Task<int>->Task<int> failed to stop internal continuation"));
            }

            if (c5val != 0)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Cancellation of Task->Task failed to stop internal continuation"));
            }

            if (c6val != 0)
            {
                Assert.Fail(string.Format("RunUnwrapTests: > FAILED.  Cancellation of Task<int>->Task failed to stop internal continuation"));
            }
        }

        // Test what happens when you cancel a task in the middle of a continuation chain.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinuationCancelTest_State()
        {
            bool t1Ran = false;
            bool t3Ran = false;

            Task t1 = new Task(delegate { t1Ran = true; });
            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true
            CancellationTokenSource ctsForT2 = new CancellationTokenSource();
            Task t2 = t1.ContinueWith((ContinuedTask, obj) =>
            {
                Assert.Fail(string.Format("RunContinuationCancelTest_State    > Failed!  t2 should not have run."));
            }, stateParam, ctsForT2.Token);


            Task t3 = t2.ContinueWith((ContinuedTask) =>
            {
                t3Ran = true;
            });

            // Cancel the middle task in the chain.  Should fire off t3.
            ctsForT2.Cancel();

            // Start the first task in the chain.  Should hold off from kicking off (canceled) t2.
            t1.Start();

            t1.Wait(5000); // should be more than enough time for either of these
            t3.Wait(5000);

            if (!t1Ran)
            {
                Assert.Fail(string.Format("RunContinuationCancelTest_State    > Failed!  t1 should have run."));
            }

            if (!t3Ran)
            {
                Assert.Fail(string.Format("RunContinuationCancelTest_State    > Failed!  t3 should have run."));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void TestNoDeadlockOnContinueWith()
        {
            Debug.WriteLine("TestNoDeadlockOnContinueWith:  shouldn't deadlock if it passes.");
            const int ITERATIONS = 1000;
            var tasks = new Task<int>[ITERATIONS];

            for (int i = 0; i < ITERATIONS; i++)
            {
                tasks[i] = Choose(CancellationToken.None);
            }

            try { Task.WaitAll(tasks); }
            catch (AggregateException ae) { ae.Handle(e => e is TaskCanceledException); }
            Debug.WriteLine("Success!");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunLazyCancellationTests()
        {
            for (int i = 0; i < 2; i++)
            {
                bool useLazyCancellation = (i == 0);
                TaskContinuationOptions options = useLazyCancellation ? TaskContinuationOptions.LazyCancellation : TaskContinuationOptions.None;

                for (int j = 0; j < 3; j++)
                {
                    bool useContinueWith = (j == 0);
                    bool useContinueWhenAny = (j == 1);
                    bool useContinueWhenAll = (j == 2);
                    string type = useContinueWith ? "ContinueWith" : useContinueWhenAny ? "ContinueWhenAny" : "ContinueWhenAll";
                    Debug.WriteLine("    ** Options = " + options + ", continuation type = " + type);

                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        ManualResetEventSlim mres1 = new ManualResetEventSlim(false);
                        ManualResetEventSlim mres2 = new ManualResetEventSlim(false);
                        Task t1 = new Task(() => { });

                        Task c1 = null;
                        if (useContinueWith)
                        {
                            c1 = t1.ContinueWith(_ => { mres1.Set(); mres2.Wait(); }, cts.Token, options, TaskScheduler.Default);
                        }
                        else if (useContinueWhenAny)
                        {
                            c1 = Task.Factory.ContinueWhenAny(new Task[] { t1 }, _ => { mres1.Set(); mres2.Wait(); }, cts.Token, options, TaskScheduler.Default);
                        }
                        else
                        {
                            c1 = Task.Factory.ContinueWhenAll(new Task[] { t1 }, _ => { mres1.Set(); mres2.Wait(); }, cts.Token, options, TaskScheduler.Default);
                        }

                        t1.Start();
                        mres1.Wait(); // Wait for continuation to start
                        cts.Cancel(); // Cancel the continuation
                        mres2.Set();  // Allow continuation to end

                        Debug.WriteLine("T3:  About to wait on c1.");
                        try
                        {
                            c1.Wait();
                        }
                        catch (Exception e)
                        {
                            Assert.Fail(string.Format("RunLazyCancellationTests: ERROR. Did not expect c1.Wait() to throw an exception, got " + e.ToString()));
                        }
                    }
                    Debug.WriteLine("Finished successfully.");
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunLazyCancellationTests_Negative()
        {
            for (int i = 0; i < 2; i++)
            {
                bool useLazyCancellation = (i == 0);
                TaskContinuationOptions options = useLazyCancellation ? TaskContinuationOptions.LazyCancellation : TaskContinuationOptions.None;

                for (int j = 0; j < 3; j++)
                {
                    bool useContinueWith = (j == 0);
                    bool useContinueWhenAny = (j == 1);
                    bool useContinueWhenAll = (j == 2);
                    string type = useContinueWith ? "ContinueWith" : useContinueWhenAny ? "ContinueWhenAny" : "ContinueWhenAll";
                    Debug.WriteLine("    ** Options = " + options + ", continuation type = " + type);

                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        Task t1 = new Task(() => { });
                        cts.Cancel();

                        Task c1 = null;
                        if (useContinueWith)
                        {
                            c1 = t1.ContinueWith(_ => { }, cts.Token, options, TaskScheduler.Default);
                        }
                        else if (useContinueWhenAny)
                        {
                            c1 = Task.Factory.ContinueWhenAny(new Task[] { t1 }, _ => { }, cts.Token, options, TaskScheduler.Default);
                        }
                        else
                        {
                            c1 = Task.Factory.ContinueWhenAll(new Task[] { t1 }, _ => { }, cts.Token, options, TaskScheduler.Default);
                        }

                        Assert.True(c1.IsCompleted != useLazyCancellation,
                           "RunLazyCancellationTests: Before t1.Start(), c1.IsCompleted = " + c1.IsCompleted);

                        t1.Start();
                        Debug.WriteLine("T1:  About to wait on c1.");
                        EnsureTaskCanceledExceptionThrown(() => { c1.Wait(); },
                           "RunLazyCancellationTests:  Expected TCE on c1.Wait after antecedent started");
                    }
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        Task t1 = new Task(() => { });

                        Task c1 = null;
                        if (useContinueWith)
                        {
                            c1 = t1.ContinueWith(_ => { }, cts.Token, options, TaskScheduler.Default);
                        }
                        else if (useContinueWhenAny)
                        {
                            c1 = Task.Factory.ContinueWhenAny(new Task[] { t1 }, _ => { }, cts.Token, options, TaskScheduler.Default);
                        }
                        else
                        {
                            c1 = Task.Factory.ContinueWhenAll(new Task[] { t1 }, _ => { }, cts.Token, options, TaskScheduler.Default);
                        }
                        cts.Cancel();

                        Assert.True(c1.IsCompleted != useLazyCancellation,
                           "RunLazyCancellationTests: Before t1.Start(), c1.IsCompleted = " + c1.IsCompleted);

                        t1.Start();
                        Debug.WriteLine("T2:  About to wait on c1.");
                        EnsureTaskCanceledExceptionThrown(() => { c1.Wait(); },
                           "RunLazyCancellationTests:  Expected TCE on c1.Wait after antecedent started");
                    }
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2084", TestRuntimes.Mono)]
        public static void LongContinuationChain_ContinueWith_DoesNotStackOverflow()
        {
            const int DiveDepth = 12_000;

            var tcs = new TaskCompletionSource();
            var t = (Task)tcs.Task;
            for (int i = 0; i < DiveDepth; i++)
            {
                t = t.ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            tcs.TrySetResult();
            t.Wait();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void LongContinuationChain_Unwrap_DoesNotStackOverflow()
        {
            const int DiveDepth = 12_000;

            Func<long, Task<long>> func = null;
            func = iterationsRemaining =>
            {
                --iterationsRemaining;
                return iterationsRemaining > 0 ?
                    Task.Factory.StartNew(() => func(iterationsRemaining)).Unwrap() :
                    Task.FromResult(iterationsRemaining);
            };
            func(DiveDepth).Wait();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void LongContinuationChain_Await_DoesNotStackOverflow()
        {
            const int DiveDepth = 12_000;

            Func<int, Task<int>> func = null;
            func = async count =>
            {
                return ++count < DiveDepth ?
                    await await Task.Factory.StartNew(() => func(count), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default) :
                    count;
            };
            func(0).Wait();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestNoDeadlockOnContinueWithExecuteSynchronously(bool useWaitAll)
        {
            // Verify that Task.Wait can return before all continuations scheduled
            // with ExecuteSynchronously complete

            Task task1 = new Task(() => { });

            var barrier = new Barrier(2);
            Task task2 = task1.ContinueWith((_task) =>
            {
                barrier.SignalAndWait(); // alert caller that we've started running
                barrier.SignalAndWait(); // wait for caller to be done waiting
            }, TaskContinuationOptions.ExecuteSynchronously);

            task1.Start();
            barrier.SignalAndWait(); // wait for task to start running

            // Wait should return once the task is complete, regardless of what other
            // continuations were scheduled off of it.
            if (useWaitAll)
            {
                Task.WaitAll(task1);
            }
            else
            {
                task1.Wait();
            }

            barrier.SignalAndWait(); // alert task that we're done waiting
            task2.Wait();
        }

        #endregion

        #region Helper Methods

        private static void ContinueWithTortureTest(int numCanceled, int numLeftover, int completeAfter, int cancelAfter)
        {
            //Debug.WriteLine("    - ContinueWithTortureTest(numCanceled={0}, numLeftover={1}, completeAfter={2}, cancelAfter={3})",
            //    numCanceled, numLeftover, completeAfter, cancelAfter);

            // The TCS mechanism gives us an easy way to start (and complete) antecedent
            TaskCompletionSource antecedentTcs = new TaskCompletionSource();
            Task antecedent = antecedentTcs.Task;

            int normalCount = 0; // incremented by "normal" or "leftover" continuations
            int canceledCount = 0; // incremented by "cancel" continuations
            CancellationTokenSource cts = new CancellationTokenSource(); // CTS to cancel

            // These TCS/continuation combos will serve to initiate antecedent completion or CTS signaling asynchronously
            TaskCompletionSource completionTcs = new TaskCompletionSource();
            completionTcs.Task.ContinueWith(_ => { antecedentTcs.TrySetResult(); }, TaskContinuationOptions.PreferFairness);
            TaskCompletionSource cancellationTcs = new TaskCompletionSource();
            cancellationTcs.Task.ContinueWith(_ => { cts.Cancel(); }, TaskContinuationOptions.PreferFairness);

            // Keep track of continuations so that you can wait on them
            Task[] normalContinuations = new Task[numLeftover];
            Task[] cancelContinuations = new Task[numCanceled];

            // Take early action if either threshold is set at 0
            if (completeAfter == 0) antecedentTcs.TrySetResult();
            if (cancelAfter == 0) cts.Cancel();

            // These are the actions to take in "to be run" and "to be canceled" continuations, respectively
            Action<Task> normalAction = (task) => { Interlocked.Increment(ref normalCount); };
            Action<Task> cancelAction = (task) => { Interlocked.Increment(ref canceledCount); };

            // Simultaneously start adding both "to be run" continuations and "to be canceled" continuations
            Task taskA = Task.Factory.StartNew(
                () =>
                {
                    for (int i = 0; i < numCanceled; i++)
                    {
                        // Use both synchronous and asynchronous continuations
                        TaskContinuationOptions tco = ((i % 2) == 0) ? TaskContinuationOptions.None : TaskContinuationOptions.ExecuteSynchronously;

                        // The cancelAction should run exactly once per "to be canceled" continuation -- either in the first continuation or,
                        // if the first continuation is canceled, in the second continuation.
                        cancelContinuations[i] = antecedent.ContinueWith(cancelAction, cts.Token, tco, TaskScheduler.Default)
                            .ContinueWith(cancelAction, tco | TaskContinuationOptions.OnlyOnCanceled);
                    }
                });

            Task taskB = Task.Factory.StartNew(
                () =>
                {
                    for (int i = 0; i < numLeftover; i++)
                    {
                        // Use both synchronous and asynchronous continuations
                        TaskContinuationOptions tco = ((i % 2) == 0) ? TaskContinuationOptions.None : TaskContinuationOptions.ExecuteSynchronously;
                        normalContinuations[i] = antecedent.ContinueWith(normalAction, tco);

                        // If you've hit completeAfter or cancelAfter, take the appropriate action
                        if ((i + 1) == completeAfter) completionTcs.TrySetResult(); // Asynchronously completes the antecedent
                        if ((i + 1) == cancelAfter) cancellationTcs.TrySetResult(); // Asynchronously initiates cancellation of "to be canceled" tasks
                    }
                });

            Task.WaitAll(taskA, taskB);
            Task.WaitAll(normalContinuations);

            try
            {
                Task.WaitAll(cancelContinuations);
            }
            catch (AggregateException ae)
            {
                // We may get AE<TCE> from WaitAll on cancelContinuations.  If so, just eat it.
                EnsureExceptionIsAEofTCE(ae,
                   "ContinueWithTortureTest: > FAILED.  Did not expect anything exception AE<TCE> from cancelContinuations.Wait()");
            }

            Assert.True(normalCount == numLeftover,
               "ContinueWithTortureTest: > FAILED! normalCount mismatch (exp " + numLeftover + " got " + normalCount + ")");
            Assert.True(canceledCount == numCanceled,
               "ContinueWithTortureTest: > FAILED! canceledCount mismatch (exp + " + numCanceled + " got " + canceledCount + ")");
        }

        // Ensures that the specified action throws a AggregateException wrapping a TaskCanceledException
        private static void EnsureTaskCanceledExceptionThrown(Action action, string message)
        {
            Exception exception = null;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            EnsureExceptionIsAEofTCE(exception, message);
        }

        // Ensures that the specified exception is an AggregateException wrapping a TaskCanceledException
        private static void EnsureExceptionIsAEofTCE(Exception exception, string message)
        {
            if (exception == null)
            {
                Assert.Fail(string.Format(message + " (no exception thrown)"));
            }
            else if (exception.GetType() != typeof(AggregateException))
            {
                Assert.Fail(string.Format(message + " (didn't throw aggregate exception)"));
            }
            else if (((AggregateException)exception).InnerException.GetType() != typeof(TaskCanceledException))
            {
                exception = ((AggregateException)exception).InnerException;
                Assert.Fail(string.Format(message + " (threw " + exception.GetType().Name + " instead of TaskCanceledException)"));
            }
        }

        private static Task<int> Choose(CancellationToken cancellationToken)
        {
            // Set up completion structures
            //var boxedCompleted = new StrongBox<Task>(); // Acts as both completion marker and sync obj for targets
            var result = new TaskCompletionSource<int>();

            // Set up teardown cancellation.  We will request cancellation when a) the supplied options token
            // has cancellation requested or b) when we actually complete somewhere in order to tear down
            // the rest of our configured set up.
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task branch1 = CreateChooseBranch<int>(cts, result, TaskScheduler.Default);
            Task branch2 = CreateChooseBranch<int>(cts, result, TaskScheduler.Default);
            Task.Factory.ContinueWhenAll(new[] { branch1, branch2 }, tasks =>
            {
                result.TrySetCanceled();
                cts.Dispose();
            }, CancellationToken.None, (TaskContinuationOptions)TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            // Return the task representing the choice
            return result.Task;
        }

        private static Task CreateChooseBranch<T>(CancellationTokenSource cts,
            TaskCompletionSource<int> result, TaskScheduler scheduler)
        {
            // If the cancellation token is already canceled, there is no need to create and link a target.
            // Instead, directly return a canceled task
            if (cts.IsCancellationRequested)
            {
                return Task.FromCanceled(cts.Token);
            }
            else
            {
                // WE ARE CREATING A BUNCH OF TASKS THAT SHARE THE SAME CANCELLATION TOKEN
                var t = Task<T>.Factory.StartNew(() => { return default(T); }, cts.Token);
                t.ContinueWith(delegate { }, cts.Token, (TaskContinuationOptions)TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                return Task<T>.Factory.StartNew(() => { if (!cts.IsCancellationRequested) cts.Cancel(); return default(T); }, cts.Token);
            }
        }

        #endregion
    }
}
