// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTestsNoState_NoneCompleted()
        {
            RunContinueWithTaskTask(TaskContinuationOptions.None);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion);

            RunContinueWithTaskTask(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskToTaskWithResult_NoneCompleted()
        {
            RunContinueWithTaskToTaskWithResult(TaskContinuationOptions.None);
            RunContinueWithTaskToTaskWithResult(s_onlyOnRanToCompletion);

            RunContinueWithTaskToTaskWithResult(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskToTaskWithResult(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTask_NoneCompleted()
        {
            RunContinueWithTaskWithResultToTask(TaskContinuationOptions.None);
            RunContinueWithTaskWithResultToTask(s_onlyOnRanToCompletion);

            RunContinueWithTaskWithResultToTask(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskWithResultToTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTaskWithResult_NoneCompleted()
        {
            RunContinueWithTaskWithResultToTaskWithResult(TaskContinuationOptions.None);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnRanToCompletion);

            RunContinueWithTaskWithResultToTaskWithResult(TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTestsNoState_FaultedCanceled()
        {
            RunContinueWithTaskTask(s_onlyOnCanceled);
            RunContinueWithTaskTask(s_onlyOnFaulted);

            RunContinueWithTaskTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskToTaskWithResult_FaultedCanceled()
        {
            RunContinueWithTaskToTaskWithResult(s_onlyOnCanceled);
            RunContinueWithTaskToTaskWithResult(s_onlyOnFaulted);

            RunContinueWithTaskToTaskWithResult(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskToTaskWithResult(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTask_FaultedCanceled()
        {
            RunContinueWithTaskWithResultToTask(s_onlyOnCanceled);
            RunContinueWithTaskWithResultToTask(s_onlyOnFaulted);

            RunContinueWithTaskWithResultToTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskWithResultToTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTaskWithResult_FaultedCanceled()
        {
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnCanceled);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnFaulted);

            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        // Exception tests.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTestsNoState_NoneCompleted_OnException()
        {
            RunContinueWithTaskTask(TaskContinuationOptions.None, true);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskTask(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskToTaskWithResult_NoneCompleted_OnException()
        {
            RunContinueWithTaskToTaskWithResult(TaskContinuationOptions.None, true);
            RunContinueWithTaskToTaskWithResult(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskToTaskWithResult(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskToTaskWithResult(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTask_NoneCompleted_OnException()
        {
            RunContinueWithTaskWithResultToTask(TaskContinuationOptions.None, true);
            RunContinueWithTaskWithResultToTask(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskWithResultToTask(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskWithResultToTask(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTaskWithResult_NoneCompleted_OnException()
        {
            RunContinueWithTaskWithResultToTaskWithResult(TaskContinuationOptions.None, true);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnRanToCompletion, true);

            RunContinueWithTaskWithResultToTaskWithResult(TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTestsNoState_FaultedCanceled_OnException()
        {
            RunContinueWithTaskTask(s_onlyOnCanceled, true);
            RunContinueWithTaskTask(s_onlyOnFaulted, true);

            RunContinueWithTaskTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskToTaskWithResult_FaultedCanceled_OnException()
        {
            RunContinueWithTaskToTaskWithResult(s_onlyOnCanceled, true);
            RunContinueWithTaskToTaskWithResult(s_onlyOnFaulted, true);

            RunContinueWithTaskToTaskWithResult(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskToTaskWithResult(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTask_FaultedCanceled_OnException()
        {
            RunContinueWithTaskWithResultToTask(s_onlyOnCanceled, true);
            RunContinueWithTaskWithResultToTask(s_onlyOnFaulted, true);

            RunContinueWithTaskWithResultToTask(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskWithResultToTask(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void RunContinueWithTaskWithResultToTaskWithResult_FaultedCanceled_OnException()
        {
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnCanceled, true);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnFaulted, true);

            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, true);
            RunContinueWithTaskWithResultToTaskWithResult(s_onlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, true);
        }

        #endregion

        #region Helper Methods

        // Chains a Task continuation to a Task.
        private static void RunContinueWithTaskTask(TaskContinuationOptions options, bool runNegativeCases = false)
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
        private static void RunContinueWithTaskToTaskWithResult(TaskContinuationOptions options, bool runNegativeCases = false)
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
        private static void RunContinueWithTaskWithResultToTask(TaskContinuationOptions options, bool runNegativeCases = false)
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
        private static void RunContinueWithTaskWithResultToTaskWithResult(TaskContinuationOptions options, bool runNegativeCases = false)
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
        private static void RunContinueWithBase(
            TaskContinuationOptions options,
            Action initRan,
            Func<Task, Task> continuationMaker,
            Func<bool> ranValue,
            bool taskHasResult)
        {
            Debug.WriteLine("    >> (1) ContinueWith after task finishes Successfully.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnRanToCompletion) == 0;
                Task task;
                if (taskHasResult) task = Task<string>.Factory.StartNew(() => "");
                else task = Task.Factory.StartNew(delegate { });
                task.Wait();

                initRan();
                bool cancel = false;
                Task cont = continuationMaker(task);
                try { cont.Wait(); }
                catch (AggregateException ex) { if (ex.InnerExceptions[0] is TaskCanceledException) cancel = true; }

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
                if (taskHasResult) task = Task<string>.Factory.StartNew(() => { mre.WaitOne(); return ""; });
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
        private static void RunContinueWithBase_NegativeCases(
            TaskContinuationOptions options,
            Action initRan,
            Func<Task, Task> continuationMaker,
            Func<bool> ranValue,
            bool taskHasResult)
        {
            Debug.WriteLine("    >> (3) ContinueWith after task finishes Exceptionally.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnFaulted) == 0;
                Task task;
                if (taskHasResult) task = Task<string>.Factory.StartNew(delegate { throw new Exception("Boom"); });
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
                    Assert.Fail(string.Format("RunContinueWithBase: >> Failed: continuation didn't run or get canceled when expected: ran = {0}, cancel = {1}", ranValue(), cancel));
                }
            }

            Debug.WriteLine("    >> (4) ContinueWith before task finishes Exceptionally.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnFaulted) == 0;
                ManualResetEvent mre = new ManualResetEvent(false);
                Task task;
                if (taskHasResult) task = Task<string>.Factory.StartNew(delegate { mre.WaitOne(); throw new Exception("Boom"); });
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
                if (taskHasResult) task = Task<string>.Factory.StartNew(() => { cancellationMRE.WaitOne(); throw new OperationCanceledException(cts.Token); }, cts.Token);
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

            //Debug.WriteLine("    >> (6) ContinueWith before task becomes Aborted.");
            {
                bool expect = (options & TaskContinuationOptions.NotOnCanceled) == 0;

                // Create a task that will transition into Canceled state
                Task task;
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                ManualResetEvent cancellationMRE = new ManualResetEvent(false);

                if (taskHasResult)
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

        #endregion
    }
}
