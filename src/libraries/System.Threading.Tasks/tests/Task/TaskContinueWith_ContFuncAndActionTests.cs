// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class TaskContinueWith_ContFuncAndActionTests
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

        [Fact]
        public static void RunContinueWithTestsNoState_NoneCompleted()
        {
            RunContinueWithTaskTask(TaskContinuationOptions.None);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion);

            RunContinueWithTaskTask(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [Fact]
        public static void RunContinueWithTaskFuture_NoneCompleted()
        {
            RunContinueWithTaskFuture(TaskContinuationOptions.None);
            RunContinueWithTaskFuture(s_onlyOnRanToCompletion);

            RunContinueWithTaskFuture(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskFuture(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [Fact]
        public static void RunContinueWithFutureTask_NoneCompleted()
        {
            RunContinueWithFutureTask(TaskContinuationOptions.None);
            RunContinueWithFutureTask(s_onlyOnRanToCompletion);

            RunContinueWithFutureTask(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [Fact]
        public static void RunContinueWithFutureFuture_NoneCompleted()
        {
            RunContinueWithFutureFuture(TaskContinuationOptions.None);
            RunContinueWithFutureFuture(s_onlyOnRanToCompletion);

            RunContinueWithFutureFuture(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureFuture(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [Fact]
        public static void RunContinueWithTestsNoState_FaultedCanceled()
        {
            RunContinueWithTaskTask(s_onlyOnCanceled);
            RunContinueWithTaskTask(s_onlyOnFaulted);

            RunContinueWithTaskTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [Fact]
        public static void RunContinueWithTaskFuture_FaultedCanceled()
        {
            RunContinueWithTaskFuture(s_onlyOnCanceled);
            RunContinueWithTaskFuture(s_onlyOnFaulted);

            RunContinueWithTaskFuture(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskFuture(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [Fact]
        public static void RunContinueWithFutureTask_FaultedCanceled()
        {
            RunContinueWithFutureTask(s_onlyOnCanceled);
            RunContinueWithFutureTask(s_onlyOnFaulted);

            RunContinueWithFutureTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [Fact]
        public static void RunContinueWithFutureFuture_FaultedCanceled()
        {
            RunContinueWithFutureFuture(s_onlyOnCanceled);
            RunContinueWithFutureFuture(s_onlyOnFaulted);

            RunContinueWithFutureFuture(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithFutureFuture(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        // Exception tests.
        [Fact]
        public static void RunContinueWithTestsNoState_NoneCompleted_OnException()
        {
            RunContinueWithTaskTask(TaskContinuationOptions.None, true);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskTask(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [Fact]
        public static void RunContinueWithTaskFuture_NoneCompleted_OnException()
        {
            RunContinueWithTaskFuture(TaskContinuationOptions.None, true);
            RunContinueWithTaskFuture(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskFuture(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskFuture(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [Fact]
        public static void RunContinueWithFutureTask_NoneCompleted_OnException()
        {
            RunContinueWithFutureTask(TaskContinuationOptions.None, true);
            RunContinueWithFutureTask(s_onlyOnRanToCompletion, true);

            RunContinueWithFutureTask(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [Fact]
        public static void RunContinueWithFutureFuture_NoneCompleted_OnException()
        {
            RunContinueWithFutureFuture(TaskContinuationOptions.None, true);
            RunContinueWithFutureFuture(s_onlyOnRanToCompletion, true);

            RunContinueWithFutureFuture(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureFuture(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [Fact]
        public static void RunContinueWithTestsNoState_FaultedCanceled_OnException()
        {
            RunContinueWithTaskTask(s_onlyOnCanceled, true);
            RunContinueWithTaskTask(s_onlyOnFaulted, true);

            RunContinueWithTaskTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }
        [Fact]
        public static void RunContinueWithTaskFuture_FaultedCanceled_OnException()
        {
            RunContinueWithTaskFuture(s_onlyOnCanceled, true);
            RunContinueWithTaskFuture(s_onlyOnFaulted, true);

            RunContinueWithTaskFuture(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskFuture(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [Fact]
        public static void RunContinueWithFutureTask_FaultedCanceled_OnException()
        {
            RunContinueWithFutureTask(s_onlyOnCanceled, true);
            RunContinueWithFutureTask(s_onlyOnFaulted, true);

            RunContinueWithFutureTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [Fact]
        public static void RunContinueWithFutureFuture_FaultedCanceled_OnException()
        {
            RunContinueWithFutureFuture(s_onlyOnCanceled, true);
            RunContinueWithFutureFuture(s_onlyOnFaulted, true);

            RunContinueWithFutureFuture(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithFutureFuture(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        #endregion

        #region Helper Methods

        // Chains a Task continuation to a Task.
        public static void RunContinueWithTaskTask(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;
            if (runNegativeCases)
            {
                RunContinueWithBase_NegativeCases(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith(delegate (Task f) { ran = true; }, options);
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
                        return t.ContinueWith(delegate (Task f) { ran = true; }, options);
                    },
                    delegate { return ran; },
                    false
                );
            }
        }

        // Chains a Task<T> continuation to a Task, with a Func<Task, T>.
        public static void RunContinueWithTaskFuture(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;
            if (runNegativeCases)
            {
                RunContinueWithBase_NegativeCases(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith<int>(delegate (Task f) { ran = true; return 5; }, options);
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
                        return t.ContinueWith<int>(delegate (Task f) { ran = true; return 5; }, options);
                    },
                    delegate { return ran; },
                    false
                );
            }
        }

        // Chains a Task continuation to a Task<T>.
        public static void RunContinueWithFutureTask(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;
            if (runNegativeCases)
            {
                RunContinueWithBase_NegativeCases(options,
                delegate { ran = false; },
                delegate (Task t)
                {
                    return t.ContinueWith(delegate (Task f) { ran = true; }, options);
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
                    return t.ContinueWith(delegate (Task f) { ran = true; }, options);
                },
                delegate { return ran; },
                true
                );
            }
        }

        // Chains a Task<U> continuation to a Task<T>, with a Func<Task<T>, U>.
        public static void RunContinueWithFutureFuture(TaskContinuationOptions options, bool runNegativeCases = false)
        {
            bool ran = false;
            if (runNegativeCases)
            {
                RunContinueWithBase_NegativeCases(options,
                    delegate { ran = false; },
                    delegate (Task t)
                    {
                        return t.ContinueWith<int>(delegate (Task f) { ran = true; return 5; }, options);
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
                        return t.ContinueWith<int>(delegate (Task f) { ran = true; return 5; }, options);
                    },
                    delegate { return ran; },
                    true
                );
            }
        }

        // Base logic for RunContinueWithXXXYYY() methods
        public static void RunContinueWithBase(
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
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    Assert.True(false, string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
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
                    Assert.True(false, string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }
        }

        // Base logic for RunContinueWithXXXYYY() methods
        public static void RunContinueWithBase_NegativeCases(
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

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

                if (expect != ranValue() || expect == cancel)
                {
                    Assert.True(false, string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
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
                    Assert.True(false, string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
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
                    Assert.True(false, string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue, cancel));
                }
            }

            //Debug.WriteLine("    >> (6) ContinueWith before task becomes Aborted.");
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
                    Assert.True(false, string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }
        }

        #endregion
    }
}
