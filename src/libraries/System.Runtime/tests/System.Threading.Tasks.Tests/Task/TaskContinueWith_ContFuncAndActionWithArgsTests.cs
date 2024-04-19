// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class TaskContinueWith_ContFuncAndActionWithArgsTests
    {
        #region Member Variables

        private static TaskContinuationOptions s_onlyOnRanToCompletion =
            TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted;
        private static TaskContinuationOptions s_onlyOnCanceled =
            TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.NotOnFaulted;
        private static TaskContinuationOptions s_onlyOnFaulted =
            TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.NotOnCanceled;

        #endregion

        #region Test Methods

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskTask_State()
        {
            RunContinueWithTaskTask_State_Helper(TaskContinuationOptions.None);
            RunContinueWithTaskTask_State_Helper(s_onlyOnRanToCompletion);

            RunContinueWithTaskTask_State_Helper(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskTask_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskFuture_State()
        {
            RunContinueWithTaskFuture_State_Helper(TaskContinuationOptions.None);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnRanToCompletion);

            RunContinueWithTaskFuture_State_Helper(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureTask_State()
        {
            RunContinueWithFutureTask_State_Helper(TaskContinuationOptions.None);
            RunContinueWithFutureTask_State_Helper(s_onlyOnRanToCompletion);

            RunContinueWithFutureTask_State_Helper(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureTask_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureFuture_State()
        {
            RunContinueWithFutureFuture_State_Helper(TaskContinuationOptions.None);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnRanToCompletion);

            RunContinueWithFutureFuture_State_Helper(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskTask_State_FaultedCanceled()
        {
            RunContinueWithTaskTask_State_Helper(s_onlyOnCanceled);
            RunContinueWithTaskTask_State_Helper(s_onlyOnFaulted);

            RunContinueWithTaskTask_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskTask_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskFuture_State_FaultedCanceled()
        {
            RunContinueWithTaskFuture_State_Helper(s_onlyOnCanceled);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnFaulted);

            RunContinueWithTaskFuture_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureTask_State_FaultedCanceled()
        {
            RunContinueWithFutureTask_State_Helper(s_onlyOnCanceled);
            RunContinueWithFutureTask_State_Helper(s_onlyOnFaulted);

            RunContinueWithFutureTask_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureTask_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureFuture_State_FaultedCanceled()
        {
            RunContinueWithFutureFuture_State_Helper(s_onlyOnCanceled);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnFaulted);

            RunContinueWithFutureFuture_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskTask_State_OnException()
        {
            RunContinueWithTaskTask_State_Helper(TaskContinuationOptions.None, true);
            RunContinueWithTaskTask_State_Helper(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskTask_State_Helper(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskTask_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskFuture_State_OnException()
        {
            RunContinueWithTaskFuture_State_Helper(TaskContinuationOptions.None, true);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskFuture_State_Helper(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureTask_State_OnException()
        {
            RunContinueWithFutureTask_State_Helper(TaskContinuationOptions.None, true);
            RunContinueWithFutureTask_State_Helper(s_onlyOnRanToCompletion, true);

            RunContinueWithFutureTask_State_Helper(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureTask_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureFuture_State_OnException()
        {
            RunContinueWithFutureFuture_State_Helper(TaskContinuationOptions.None, true);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnRanToCompletion, true);

            RunContinueWithFutureFuture_State_Helper(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskTask_State_FaultedCanceled_OnException()
        {
            RunContinueWithTaskTask_State_Helper(s_onlyOnCanceled, true);
            RunContinueWithTaskTask_State_Helper(s_onlyOnFaulted, true);

            RunContinueWithTaskTask_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskTask_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskFuture_State_FaultedCanceled_OnException()
        {
            RunContinueWithTaskFuture_State_Helper(s_onlyOnCanceled, true);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnFaulted, true);

            RunContinueWithTaskFuture_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskFuture_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureTask_State_FaultedCanceled_OnException()
        {
            RunContinueWithFutureTask_State_Helper(s_onlyOnCanceled, true);
            RunContinueWithFutureTask_State_Helper(s_onlyOnFaulted, true);

            RunContinueWithFutureTask_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureTask_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithFutureFuture_State_FaultedCanceled_OnException()
        {
            RunContinueWithFutureFuture_State_Helper(s_onlyOnCanceled, true);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnFaulted, true);

            RunContinueWithFutureFuture_State_Helper(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureFuture_State_Helper(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithPreCancelTests_State()
        {
            Action<Task, bool, string> EnsureCompletionStatus = delegate (Task task, bool shouldBeCompleted, string message)
            {
                if (task.IsCompleted != shouldBeCompleted)
                {
                    Assert.Fail(string.Format(string.Format("RunContinueWithPreCancelTests_State: > FAILED.  {0}.", message)));
                }
            };

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            ManualResetEvent mres = new ManualResetEvent(false);
            // Pre-increment the dontCounts for pre-canceled continuations to make final check easier
            // (i.e., all counts should be 1 at end).
            int[] doneCount = { 0, 0, 1, 0, 1, 0 };
            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true

            Task t1 = new Task(delegate { doneCount[0]++; });
            Task c1 = t1.ContinueWith((_, obj) => { doneCount[1]++; }, stateParam);
            Task c2 = c1.ContinueWith((_, obj) => { mres.WaitOne(); doneCount[2]++; }, stateParam, cts.Token);
            Task c3 = c2.ContinueWith((_, obj) => { mres.WaitOne(); doneCount[3]++; }, stateParam);
            Task c4 = c3.ContinueWith((_, obj) => { mres.WaitOne(); doneCount[4]++; }, stateParam,
                cts.Token, TaskContinuationOptions.LazyCancellation, TaskScheduler.Default);
            Task c5 = c4.ContinueWith((_, obj) => { mres.WaitOne(); doneCount[5]++; }, stateParam);

            EnsureCompletionStatus(c2, true, "  c2 should have completed (canceled) upon construction");
            EnsureCompletionStatus(c4, false, "  c4 should NOT have completed (canceled) upon construction");
            EnsureCompletionStatus(t1, false, "  t1 should NOT have completed before being started");
            EnsureCompletionStatus(c1, false, "  c1 should NOT have completed before antecedent completed");
            EnsureCompletionStatus(c3, false, "  c3 should NOT have completed before mres was set");
            EnsureCompletionStatus(c5, false, "  c5 should NOT have completed before mres was set");

            // These should be done already.  And Faulted.

            Exception exception = null;

            try
            {
                c2.Wait();
                Assert.Fail(string.Format("RunContinueWithPreCancelTests_State:  Expected c2.Wait to throw AE/TCE"));
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            EnsureExceptionIsAEofTCE(exception, "RunContinueWithPreCancelTests_State:  Expected c2.Wait to throw AE/TCE");

            mres.Set();
            Debug.WriteLine("RunContinueWithPreCancelTests_State:  Waiting for tasks to complete... if we hang here, something went wrong.");
            c3.Wait();

            try
            {
                c4.Wait();
                Assert.Fail(string.Format("RunContinueWithPreCancelTests_State:  Expected c4.Wait to throw AE/TCE"));
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            EnsureExceptionIsAEofTCE(exception, "RunContinueWithPreCancelTests_State:  Expected c4.Wait to throw AE/TCE");
            c5.Wait();

            EnsureCompletionStatus(t1, false, " t1 should NOT have completed (post-mres.Set()) before being started");
            EnsureCompletionStatus(c1, false, " c1 should NOT have completed (post-mres.Set()) before antecedent completed");

            t1.Start();
            c1.Wait();

            for (int i = 0; i < 6; i++)
            {
                if (doneCount[i] != 1)
                {
                    Assert.Fail(string.Format("RunContinueWithPreCancelTests_State: > FAILED.  doneCount[{0}] should be 1, is {1}", i, doneCount[i]));
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinuationChainingTest_State()
        {
            int x = 0;
            int y = 0;

            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true
            Task t1 = new Task(delegate { x = 1; });
            Task t2 = t1.ContinueWith(delegate (Task t, object obj) { y = 1; }, stateParam);
            Task<int> t3 = t2.ContinueWith(delegate (Task t, object obj) { return 5; }, stateParam);
            Task<int> t4 = t3.ContinueWith(delegate (Task<int> t, Object obj) { return Task<int>.Factory.StartNew(delegate { return 10; }); }, stateParam).Unwrap();
            Task<string> t5 = t4.ContinueWith(delegate (Task<int> t, Object obj) { return Task<string>.Factory.StartNew(delegate { for (int i = 0; i < 400; i++) ; return "worked"; }); }, stateParam).Unwrap();

            try
            {
                t1.Start();
                if (!t5.Result.Equals("worked"))
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest_State:    > FAILED! t5.Result should be \"worked\", is {0}", t5.Result));
                }
                if (t4.Result != 10)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest_State:    > FAILED! t4.Result should be 10, is {0}", t4.Result));
                }
                if (t3.Result != 5)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest_State:    > FAILED! t3.Result should be 5, is {0}", t3.Result));
                }
                if (y != 1)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest_State:    > FAILED! y should be 1, is {0}", y));
                }
                if (x != 1)
                {
                    Assert.Fail(string.Format("RunContinuationChainingTest_State:    > FAILED! x should be 1, is {0}", x));
                }
            }
            catch (Exception e)
            {
                Assert.Fail(string.Format("RunContinuationChainingTest_State:    > FAILED! Exception = {0}", e));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithOnDisposedTaskTest_State()
        {
            Task t1 = Task.Factory.StartNew(delegate { });
            t1.Wait();
            //t1.Dispose();

            try
            {
                string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true
                Task t2 = t1.ContinueWith((completedTask, obj) => { }, stateParam);
            }
            catch
            {
                Assert.Fail(string.Format("RunContinueWithOnDisposedTaskTest_State:    > FAILED!  should NOT have seen an exception."));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithParamsTest_State_Cancellation()
        {
            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true

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

                c1b = child1.ContinueWith((_, obj) => { }, stateParam, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.AttachedToParent);
                c1c = child1.ContinueWith((_, obj) => { }, stateParam, TaskContinuationOptions.AttachedToParent);

                c2b = child2.ContinueWith((_, obj) => { }, stateParam, TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
                c2c = child2.ContinueWith((_, obj) => { }, stateParam, TaskContinuationOptions.AttachedToParent);

                cts.Cancel(); // should cancel the unstarted child task
                child2.Start();
            });

            container.Start();
            try { container.Wait(); }
            catch { }

            if (c1b.Status != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest_State    > FAILED.  Continuation task w/NotOnCanceled should have been canceled when antecedent was canceled."));
            }
            if (c1c.Status != TaskStatus.RanToCompletion)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest_State    > FAILED.  Continuation task w/ canceled antecedent should have run to completion."));
            }
            if (c2b.Status != TaskStatus.Canceled)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest_State    > FAILED.  Continuation task w/NotOnRanToCompletion should have been canceled when antecedent completed."));
            }
            c2c.Wait();
            if (c2c.Status != TaskStatus.RanToCompletion)
            {
                Assert.Fail(string.Format("RunContinueWithParamsTest_State    > FAILED.  Continuation task w/ completed antecedent should have run to completion."));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithParamsTest_State_IllegalParameters()
        {
            Task t1 = new Task(delegate { });
            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true

            try
            {
                Task t2 = t1.ContinueWith((ooo, obj) => { }, stateParam, (TaskContinuationOptions)0x1000000);
                Assert.Fail(string.Format("RunContinueWithParamsTest_State    > FAILED.  Should have seen exception from illegal continuation options."));
            }
            catch { }

            try
            {
                Task t2 = t1.ContinueWith((ooo, obj) => { }, stateParam, TaskContinuationOptions.LongRunning | TaskContinuationOptions.ExecuteSynchronously);
                Assert.Fail(string.Format("RunContinueWithParamsTest_State    > FAILED.  Should have seen exception when combining LongRunning and ExecuteSynchronously"));
            }
            catch { }

            try
            {
                Task t2 = t1.ContinueWith((ooo, obj) => { }, stateParam,
                            TaskContinuationOptions.NotOnRanToCompletion |
                            TaskContinuationOptions.NotOnFaulted |
                            TaskContinuationOptions.NotOnCanceled);
                Assert.Fail(string.Format("RunContinueWithParamsTest_State    > FAILED.  Should have seen exception from illegal NotOnAny continuation options."));
            }
            catch (Exception)
            {
            }

            t1.Start();
            t1.Wait();
        }

        #endregion

        #region Helper Methods

        // Chains a Task continuation to a Task.
        private static void RunContinueWithTaskTask_State_Helper(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;

            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true

            if (runNegativeCases)
            {
                RunContinueWithBase_ExceptionCases(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith(delegate (Task f, object obj) { Debug.WriteLine("Inside"); ran = true; }, stateParam, options);
                    },
                    delegate { return ran; },
                    false
                );
            }
            else
            {
                RunContinueWithBase(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith(delegate (Task f, object obj) { Debug.WriteLine("Inside"); ran = true; }, stateParam, options);
                    },
                    delegate { return ran; },
                    false
                );
            }
        }

        // Chains a Task<T> continuation to a Task, with a Func<Task, T>.
        private static void RunContinueWithTaskFuture_State_Helper(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;

            Debug.WriteLine("* RunContinueWithTaskFuture_StateA(Object, options={0})", options);
            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true

            if (runNegativeCases)
            {
                RunContinueWithBase_ExceptionCases(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith<int>(delegate (Task f, object obj) { ran = true; return 5; }, stateParam, options);
                    },
                    delegate { return ran; },
                    false
                );
            }
            else
            {
                RunContinueWithBase(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith<int>(delegate (Task f, object obj) { ran = true; return 5; }, stateParam, options);
                    },
                    delegate { return ran; },
                    false
                );
            }
        }

        // Chains a Task continuation to a Task<T>.
        private static void RunContinueWithFutureTask_State_Helper(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;

            Debug.WriteLine("* RunContinueWithFutureTask_State(Object, options={0})", options);
            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true

            if (runNegativeCases)
            {
                RunContinueWithBase_ExceptionCases(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith(delegate (Task f, object obj) { ran = true; }, stateParam, options);
                    },
                    delegate { return ran; },
                    true
                );
            }
            else
            {
                RunContinueWithBase(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith(delegate (Task f, object obj) { ran = true; }, stateParam, options);
                    },
                    delegate { return ran; },
                    true
                    );
            }
        }

        // Chains a Task<U> continuation to a Task<T>, with a Func<Task<T>, U>.
        private static void RunContinueWithFutureFuture_State_Helper(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;

            Debug.WriteLine("* RunContinueWithFutureFuture_StateA(Object, options={0})", options);
            string stateParam = "test"; //used as a state parameter for the continuation if the useStateParam is true
            if (runNegativeCases)
            {
                RunContinueWithBase_ExceptionCases(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith<int>(delegate (Task f, object obj) { ran = true; return 5; }, stateParam, options);
                    },
                    delegate { return ran; },
                    true
                );
            }
            else
            {
                RunContinueWithBase(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith<int>(delegate (Task f, object obj) { ran = true; return 5; }, stateParam, options);
                    },
                    delegate { return ran; },
                    true
                    );
            }
        }

        // Base logic for RunContinueWithXXXYYY() methods
        private static void RunContinueWithBase(
            TaskContinuationOptions options,
            Action initRan,
            Func<Task, Task> continuationMaker,
            Func<bool> ranValue,
            bool taskIsFuture)
        {
            Debug.WriteLine("    >> (1) ContinueWith after task finishes Successfully.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnRanToCompletion) == 0;
                Task task;
                if (taskIsFuture) task = Task<string>.Factory.StartNew(() => "");
                else task = Task.Factory.StartNew(delegate { });
                task.Wait();

                initRan();
                Debug.WriteLine("Init Action Ran");
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }
                Debug.WriteLine("Finished Wait");
                if (expect != ranValue() || expect == cancel)
                {
                    Assert.Fail(string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }
            Debug.WriteLine("    >> (2) ContinueWith before task finishes Successfully.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnRanToCompletion) == 0;
                ManualResetEvent mre = new ManualResetEvent(false);
                Task task;
                if (taskIsFuture) task = Task<string>.Factory.StartNew(() => { mre.WaitOne(); return ""; });
                else task = Task.Factory.StartNew(delegate { mre.WaitOne(); });

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);

                mre.Set();
                task.Wait();

                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    Assert.Fail(string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }
        }

        // Base logic for RunContinueWithXXXYYY() methods
        private static void RunContinueWithBase_ExceptionCases(
            TaskContinuationOptions options,
            Action initRan,
            Func<Task, Task> continuationMaker,
            Func<bool> ranValue,
            bool taskIsFuture)
        {
            Debug.WriteLine("    >> (3) ContinueWith after task finishes Exceptionally.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnFaulted) == 0;
                Task task;
                if (taskIsFuture) task = Task<string>.Factory.StartNew(delegate { throw new Exception("Boom"); });
                else task = Task.Factory.StartNew(delegate { throw new Exception("Boom"); });
                try { task.Wait(); }
                catch (AggregateException) { /*swallow(ouch)*/ }
                Debug.WriteLine("S3 caught e1.");
                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }
                Debug.WriteLine("S3 finished wait");
                if (expect != ranValue() || expect == cancel)
                {
                    Assert.Fail(string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }
            Debug.WriteLine("    >> (4) ContinueWith before task finishes Exceptionally.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnFaulted) == 0;
                ManualResetEvent mre = new ManualResetEvent(false);
                Task task;
                if (taskIsFuture) task = Task<string>.Factory.StartNew(delegate { mre.WaitOne(); throw new Exception("Boom"); });
                else task = Task.Factory.StartNew(delegate { mre.WaitOne(); throw new Exception("Boom"); });

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);

                mre.Set();
                try { task.Wait(); }
                catch (AggregateException) { /*swallow(ouch)*/ }

                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    Assert.Fail(string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }
            Debug.WriteLine("    >> (5) ContinueWith after task becomes Aborted.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnCanceled) == 0;
                // Create a task that will transition into Canceled state
                CancellationTokenSource cts = new CancellationTokenSource();
                Task task;
                ManualResetEvent cancellationMRE = new ManualResetEvent(false);
                if (taskIsFuture) task = Task<string>.Factory.StartNew(() => { cancellationMRE.WaitOne(); throw new OperationCanceledException(cts.Token); }, cts.Token);
                else task = Task.Factory.StartNew(delegate { cancellationMRE.WaitOne(); throw new OperationCanceledException(cts.Token); }, cts.Token);
                cts.Cancel();
                cancellationMRE.Set();

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    Assert.Fail(string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue, cancel));
                }
            }
            Debug.WriteLine("    >> (6) ContinueWith before task becomes Aborted.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnCanceled) == 0;

                // Create a task that will transition into Canceled state
                Task task;
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                ManualResetEvent cancellationMRE = new ManualResetEvent(false);

                if (taskIsFuture)
                    task = Task<string>.Factory.StartNew(() => { cancellationMRE.WaitOne(); throw new OperationCanceledException(ct); }, ct);
                else
                    task = Task.Factory.StartNew(delegate { cancellationMRE.WaitOne(); throw new OperationCanceledException(ct); }, ct);

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);

                cts.Cancel();
                cancellationMRE.Set();

                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    Assert.Fail(string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }
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

        #endregion
    }
}
