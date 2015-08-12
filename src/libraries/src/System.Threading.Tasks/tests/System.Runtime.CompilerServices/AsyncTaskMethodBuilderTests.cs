// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class AsyncTaskMethodBuilderTests
    {
        // building tasks
        [Fact]
        public static void RunAsyncMethodBuilderTests()
        {
            // AsyncVoidMethodBuilder
            {
                // Test captured sync context with successful completion (SetResult)
                {
                    SynchronizationContext previousContext = SynchronizationContext.Current;
                    var trackedContext = new TrackOperationsSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(trackedContext);

                    // Completing in opposite order as created
                    var avmb1 = AsyncVoidMethodBuilder.Create();
                    Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Should have been one active builder (SetResult, opposite order).");
                    var avmb2 = AsyncVoidMethodBuilder.Create();
                    Assert.True(trackedContext.TrackedCount == 2, "RunAsyncMethodBuilderTests > FAILED: Should have been two active builders (SetResult, opposite order).");
                    avmb2.SetResult();
                    Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have decremented count (SetResult, opposite order).");
                    avmb1.SetResult();
                    Assert.True(trackedContext.TrackedCount == 0, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have returned count to 0 (SetResult, opposite order).");

                    // Completing in same order as created
                    avmb1 = AsyncVoidMethodBuilder.Create();
                    Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Should have been one active builder (SetResult, same order).");
                    avmb2 = AsyncVoidMethodBuilder.Create();
                    Assert.True(trackedContext.TrackedCount == 2, "RunAsyncMethodBuilderTests > FAILED: Should have been two active builders (SetResult, same order).");
                    avmb1.SetResult();
                    Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have decremented count (SetResult, same order).");
                    avmb2.SetResult();
                    Assert.True(trackedContext.TrackedCount == 0, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have returned count to 0 (SetResult, same order).");

                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }

                // Test not having a sync context with successful completion (SetResult)
                {
                    SynchronizationContext previousContext = SynchronizationContext.Current;
                    try
                    {
                        // Make sure not having a sync context doesn't cause us to blow up
                        SynchronizationContext.SetSynchronizationContext(null);
                        var avmb = AsyncVoidMethodBuilder.Create();
                        avmb.SetResult();
                    }
                    catch (Exception)
                    {
                        Assert.True(false, string.Format("RunAsyncMethodBuilderTests > FAILURE.  Sync context caused us to blow up with Create/SetResult"));
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(previousContext);
                    }
                }
            }

            // AsyncTaskMethodBuilder
            {
                // Creating a task builder, building it, completing it successfully
                {
                    var atmb = AsyncTaskMethodBuilder.Create();
                    var t = atmb.Task;
                    Assert.True(t.Status == TaskStatus.WaitingForActivation, "    > FAILURE. Builder should still be active (ATMB, build then set");
                    atmb.SetResult();
                    Assert.True(t.Status == TaskStatus.RanToCompletion, "Task status should equal RanToCompletion");
                }

                // Verify that AsyncTaskMethodBuilder is not touching sync context
                {
                    SynchronizationContext previousContext = SynchronizationContext.Current;
                    var trackedContext = new TrackOperationsSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(trackedContext);

                    var atmb = AsyncTaskMethodBuilder.Create();
                    Assert.True(trackedContext.TrackedCount == 0, "    > FAILURE. Builder should not interact with the sync context (before ATMB set)");
                    atmb.SetResult();
                    Assert.True(trackedContext.TrackedCount == 0, "    > FAILURE. Builder should not interact with the sync context (after ATMB set)");
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }

            // AsyncTaskMethodBuilder<T>
            {
                // Creating a task builder, building it, completing it successfully
                {
                    var atmb = AsyncTaskMethodBuilder<int>.Create();
                    var t = atmb.Task;
                    Assert.True(t.Status == TaskStatus.WaitingForActivation, "    > FAILURE. Builder should still be active (ATMBT, build then set)");
                    atmb.SetResult(43);
                    Assert.True(t.Status == TaskStatus.RanToCompletion, "    > FAILURE. Builder should have successfully completed (ATMBT, build then set)");
                    Assert.True(t.Result == 43, "    > FAILURE. Builder should completed with the set result (ATMBT, build then set)");
                }

                // Verify that AsyncTaskMethodBuilder<T> is not touching sync context
                {
                    SynchronizationContext previousContext = SynchronizationContext.Current;
                    var trackedContext = new TrackOperationsSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(trackedContext);

                    var atmb = AsyncTaskMethodBuilder<string>.Create();
                    Assert.True(trackedContext.TrackedCount == 0, "    > FAILURE. Builder should not interact with the sync context (before ATMBT set)");
                    atmb.SetResult("async");
                    Assert.True(trackedContext.TrackedCount == 0, "    > FAILURE. Builder should not interact with the sync context (after ATMBT set)");

                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }
        }

        [Fact]
        [ActiveIssue("Hangs")]
        public static void RunAsyncMethodBuilderTests_NegativeTests()
        {
            // Incorrect usage for AsyncVoidMethodBuilder
            {
                var atmb = new AsyncTaskMethodBuilder();
                Assert.Throws<ArgumentNullException>(
                   () => { atmb.SetException(null); });
            }

            // Incorrect usage for AsyncTaskMethodBuilder
            {
                var avmb = AsyncVoidMethodBuilder.Create();
                Assert.Throws<ArgumentNullException>(
                 () => { avmb.SetException(null); });
            }

            // Creating a task builder, building it, completing it successfully, and making sure it can't be reset
            {
                var atmb = AsyncTaskMethodBuilder.Create();
                atmb.SetResult();
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }

            // Incorrect usage for AsyncTaskMethodBuilder<T>
            {
                var atmb = new AsyncTaskMethodBuilder<int>();
                Assert.Throws<ArgumentNullException>(
                   () => { atmb.SetException(null); });
            }

            // Creating a task builder <T>, building it, completing it successfully, and making sure it can't be reset
            {
                var atmb = AsyncTaskMethodBuilder<int>.Create();
                atmb.SetResult(43);
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(44); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }
        }

        [Fact]
        public static void AsyncMethodBuilderCreate_SetExceptionTest()
        {
            // Creating a task builder, building it, completing it faulted, and making sure it can't be reset
            {
                var atmb = AsyncTaskMethodBuilder.Create();
                var t = atmb.Task;
                Assert.True(t.Status == TaskStatus.WaitingForActivation, "    > FAILURE. Builder should still be active (ATMB, build then fault)");
                atmb.SetException(new InvalidCastException());
                Assert.True(t.Status == TaskStatus.Faulted, "    > FAILURE. Builder should be faulted after an exception occurs (ATMB, build then fault)");
                Assert.True(t.Exception.InnerException is InvalidCastException, "    > FAILURE. Wrong exception found in builder (ATMB, build then fault)");
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }

            // Creating a task builder, completing it faulted, building it, and making sure it can't be reset
            {
                var atmb = AsyncTaskMethodBuilder.Create();
                atmb.SetException(new InvalidCastException());
                var t = atmb.Task;
                Assert.True(t.Status == TaskStatus.Faulted, "    > FAILURE. Builder should be faulted after an exception occurs (ATMB, fault then build)");
                Assert.True(t.Exception.InnerException is InvalidCastException, "    > FAILURE. Wrong exception found in builder (ATMB, fault then build)");
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }

            // Test cancellation
            {
                var atmb = AsyncTaskMethodBuilder.Create();
                var oce = new OperationCanceledException();
                atmb.SetException(oce);
                var t = atmb.Task;
                Assert.True(t.Status == TaskStatus.Canceled, "    > FAILURE. Builder should be canceled from an unhandled OCE (ATMB cancellation)");
                try
                {
                    t.GetAwaiter().GetResult();
                    Assert.True(false, "    > FAILURE. Excepted GetResult to throw. (ATMB cancellation)");
                }
                catch (Exception exc)
                {
                    Assert.True(Object.ReferenceEquals(oce, exc), "    > FAILURE. Excepted original OCE to be thrown. (ATMB cancellation)");
                }
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }
        }

        [Fact]
        public static void AsyncMethodBuilderCreate_SetExceptionTest2()
        {
            // Test captured sync context with exceptional completion

            SynchronizationContext previousContext = SynchronizationContext.Current;

            var trackedContext = new TrackOperationsSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(trackedContext);

            // Completing in opposite order as created
            var avmb1 = AsyncVoidMethodBuilder.Create();
            Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Should have been one active builder (SetException, opposite order).");
            var avmb2 = AsyncVoidMethodBuilder.Create();
            Assert.True(trackedContext.TrackedCount == 2, "RunAsyncMethodBuilderTests > FAILED: Should have been two active builders (SetException, opposite order).");
            avmb2.SetException(new InvalidOperationException("uh oh 1"));
            Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have decremented count (SetException, opposite order).");
            avmb1.SetException(new InvalidCastException("uh oh 2"));
            Assert.True(trackedContext.TrackedCount == 0, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have returned count to 0 (SetException, opposite order).");

            Assert.True(
            trackedContext.PostExceptions.Count == 2 &&
            trackedContext.PostExceptions[0] is InvalidOperationException &&
            trackedContext.PostExceptions[1] is InvalidCastException,
            "    > FAILED: Expected two exceptions of the right types.");

            // Completing in same order as created
            var avmb3 = AsyncVoidMethodBuilder.Create();
            Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Should have been one active builder (SetException, same order).");
            var avmb4 = AsyncVoidMethodBuilder.Create();
            Assert.True(trackedContext.TrackedCount == 2, "RunAsyncMethodBuilderTests > FAILED: Should have been two active builders (SetException, same order).");
            avmb3.SetException(new InvalidOperationException("uh oh 3"));
            Assert.True(trackedContext.TrackedCount == 1, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have decremented count (SetException, same order).");
            avmb4.SetException(new InvalidCastException("uh oh 4"));
            Assert.True(trackedContext.TrackedCount == 0, "RunAsyncMethodBuilderTests > FAILED: Completed builder should have returned count to 0 (SetException, same order).");

            Assert.True(
            trackedContext.PostExceptions.Count == 4 &&
            trackedContext.PostExceptions[2] is InvalidOperationException &&
            trackedContext.PostExceptions[3] is InvalidCastException,
            "RunAsyncMethodBuilderTests > FAILED: Expected two more exceptions of the right types.");

            SynchronizationContext.SetSynchronizationContext(previousContext);
        }

        [Fact]
        public static void AsyncMethodBuilderTCreate_SetExceptionTest()
        {
            // Creating a task builder, building it, completing it faulted, and making sure it can't be reset
            {
                var atmb = AsyncTaskMethodBuilder<int>.Create();
                var t = atmb.Task;
                Assert.True(t.Status == TaskStatus.WaitingForActivation, "    > FAILURE. Builder should still be active (ATMBT, build then fault)");
                atmb.SetException(new InvalidCastException());
                Assert.True(t.Status == TaskStatus.Faulted, "    > FAILURE. Builder should be faulted after an exception occurs (ATMBT, build then fault)");
                Assert.True(t.Exception.InnerException is InvalidCastException, "    > FAILURE. Wrong exception found in builder (ATMBT, build then fault)");
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(44); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }

            // Creating a task builder, completing it faulted, building it, and making sure it can't be reset
            {
                var atmb = AsyncTaskMethodBuilder<int>.Create();
                atmb.SetException(new InvalidCastException());
                var t = atmb.Task;
                Assert.True(t.Status == TaskStatus.Faulted, "    > FAILURE. Builder should be faulted after an exception occurs (ATMBT, fault then build)");
                Assert.True(t.Exception.InnerException is InvalidCastException, "    > FAILURE. Wrong exception found in builder (ATMBT, fault then build)");
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(44); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }

            // Test cancellation
            {
                var atmb = AsyncTaskMethodBuilder<int>.Create();
                var oce = new OperationCanceledException();
                atmb.SetException(oce);
                var t = atmb.Task;
                Assert.True(t.Status == TaskStatus.Canceled, "    > FAILURE. Builder should be canceled from an unhandled OCE (ATMBT cancellation)");
                try
                {
                    t.GetAwaiter().GetResult();
                    Assert.True(false, "    > FAILURE. Excepted GetResult to throw. (ATMBT cancellation)");
                }
                catch (Exception exc)
                {
                    Assert.True(Object.ReferenceEquals(oce, exc), "    > FAILURE. Excepted original OCE to be thrown. (ATMBT cancellation)");
                }
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetResult(44); });
                Assert.Throws<InvalidOperationException>(
                   () => { atmb.SetException(new Exception()); });
            }
        }

        // random other stuff, e.g. exception dispatch info
        [Fact]
        public static void RunAsyncAdditionalBehaviorsTests()
        {
            {
                var atmb = new AsyncTaskMethodBuilder();
                var t1 = atmb.Task;
                var t2 = atmb.Task;
                Assert.True(t1 != null, "     > FAILURE. Task should have been initialized.");
                Assert.True(t2 != null, "     > FAILURE. Task should have been initialized.");
                Assert.True(t1 == t2, "     > FAILURE. Task should be cached once initialized.");
            }
            {
                var atmb = new AsyncTaskMethodBuilder<int>();
                var t1 = atmb.Task;
                var t2 = atmb.Task;
                Assert.True(t1 != null, "     > FAILURE. Task should have been initialized.");
                Assert.True(t2 != null, "     > FAILURE. Task should have been initialized.");
                Assert.True(t1 == t2, "     > FAILURE. Task should be cached once initialized.");
            }
        }

        [Fact]
        public static void RunAsyncAdditionalBehaviorsTests_NegativeCases()
        {
            // Test ExceptionDispatchInfo usage
            //if (Thread.CurrentThread.CurrentCulture.ThreeLetterWindowsLanguageName == "ENU") // only on ENU because of string comparisons
            //{
            {
                var tcs = new TaskCompletionSource<int>();
                try { throw new InvalidOperationException(); }
                catch (Exception e) { tcs.SetException(e); }
                Assert.True(ValidateFaultedTask(tcs.Task), "     > FAILURE. Task's stack trace is incorrect");
            }

            {
                var atmb = AsyncTaskMethodBuilder.Create();
                try { throw new InvalidOperationException(); }
                catch (Exception e) { atmb.SetException(e); }
                Assert.True(ValidateFaultedTask(atmb.Task), "     > FAILURE. Task's stack trace is incorrect");
            }

            {
                var atmbtr = AsyncTaskMethodBuilder<object>.Create();
                try { throw new InvalidOperationException(); }
                catch (Exception e) { atmbtr.SetException(e); }
                Assert.True(ValidateFaultedTask(atmbtr.Task), "     > FAILURE. Task's stack trace is incorrect");
            }

            {
                SynchronizationContext previousContext = SynchronizationContext.Current;
                var tosc = new TrackOperationsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(tosc);
                var avmb = AsyncVoidMethodBuilder.Create();
                try { throw new InvalidOperationException(); }
                catch (Exception exc) { avmb.SetException(exc); }
                Assert.True(
                   tosc.PostExceptions.Count > 0 && ValidateException(tosc.PostExceptions[0]),
                   "     > FAILURE. AVMB task should have posted exceptions to the sync context");
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        [Fact]
        public static void RunAsyncAdditionalBehaviorsTests_NegativeCases2()
        {
            // Running tasks with exceptions.
            {
                var twa1 = Task.Run(() => { throw new Exception("uh oh"); });
                var twa2 = Task.Factory.StartNew(() => { throw new Exception("uh oh"); });
                var tasks = new Task[]
                    {
                        Task.Run(() => { throw new Exception("uh oh"); }),
                        Task.Factory.StartNew<int>(() => { throw new Exception("uh oh"); }),
                        Task.WhenAll(Task.Run(() => { throw new Exception("uh oh"); }), Task.Run(() => { throw new Exception("uh oh"); })),
                        Task.WhenAll<int>(Task.Run(new Func<int>(() => { throw new Exception("uh oh"); })), Task.Run(new Func<int>(() => { throw new Exception("uh oh"); }))),
                        Task.WhenAny(twa1, twa2).Unwrap(),
                        Task.WhenAny<int>(Task.Run(new Func<Task<int>>(() => { throw new Exception("uh oh"); }))).Unwrap(),
                        Task.Factory.StartNew(() => Task.Factory.StartNew(() => { throw new Exception("uh oh"); })).Unwrap(),
                        Task.Factory.StartNew<Task<int>>(() => Task.Factory.StartNew<int>(() => { throw new Exception("uh oh"); })).Unwrap(),
                        Task.Run(() => Task.Run(() => { throw new Exception("uh oh"); })),
                        Task.Run(() => Task.Run(new Func<int>(() => { throw new Exception("uh oh"); }))),
                        Task.Run(new Func<Task>(() => { throw new Exception("uh oh"); })),
                        Task.Run(new Func<Task<int>>(() => { throw new Exception("uh oh"); }))
                    };

                for (int i = 0; i < tasks.Length; i++)
                {
                    var task = tasks[i];
                    Assert.True(ValidateFaultedTask(task), "     > FAILURE. Task " + i + " didn't fault with the right stack trace.");
                }

                ((IAsyncResult)twa1).AsyncWaitHandle.WaitOne();
                ((IAsyncResult)twa2).AsyncWaitHandle.WaitOne();
                Exception ignored = twa1.Exception;
                ignored = twa2.Exception;
            }

            // Test that OCEs don't result in the unobserved event firing
            {
                var cts = new CancellationTokenSource();
                var oce = new OperationCanceledException(cts.Token);

                // A Task that throws an exception to cancel
                var b = new Barrier(2);
                Task t1 = Task.Factory.StartNew(() =>
                {
                    b.SignalAndWait();
                    b.SignalAndWait();
                    throw oce;
                }, cts.Token);
                b.SignalAndWait(); // make sure task is started before we cancel
                cts.Cancel();
                b.SignalAndWait(); // release task to complete

                // A TCS task
                var tcs = new TaskCompletionSource<int>();
                tcs.SetCanceled();
                Task t2 = tcs.Task;

                EventHandler<UnobservedTaskExceptionEventArgs> handler = (s, e) =>
                {
                    Assert.True(false, string.Format("     > OCE shouldn't have resulted in unobserved event firing with " + e.Exception.ToString()));
                };
                TaskScheduler.UnobservedTaskException += handler;

                ((IAsyncResult)t1).AsyncWaitHandle.WaitOne();
                ((IAsyncResult)t2).AsyncWaitHandle.WaitOne();
                t1 = null;
                t2 = null;

                for (int i = 0; i < 10; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                TaskScheduler.UnobservedTaskException -= handler;
            }
        }

        #region Helper Methods / Classes

        private static bool ValidateFaultedTask(Task t)
        {
            ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
            bool localPassed = t.IsFaulted;
            try
            {
                t.GetAwaiter().GetResult();
                Debug.WriteLine("     > FAILURE. Faulted task's GetResult should have thrown.");
            }
            catch (Exception e) { localPassed &= ValidateException(e); }
            return localPassed;
        }

        private static bool ValidateException(Exception e)
        {
            return e != null && e.StackTrace != null && e.StackTrace.Contains("End of stack trace");
        }

        private class TrackOperationsSynchronizationContext : SynchronizationContext
        {
            private int _trackedCount;
            private int _postCount;
            //ConcurrentQueue
            private List<Exception> _postExceptions = new List<Exception>();

            public int TrackedCount { get { return _trackedCount; } }
            public List<Exception> PostExceptions
            {
                get
                {
                    List<Exception> returnValue;
                    lock (_postExceptions)
                    {
                        returnValue = new List<Exception>(_postExceptions);
                        return returnValue;
                    }
                }
            }
            public int PostCount { get { return _postCount; } }

            public override void OperationStarted() { Interlocked.Increment(ref _trackedCount); }
            public override void OperationCompleted() { Interlocked.Decrement(ref _trackedCount); }

            public override void Post(SendOrPostCallback callback, object state)
            {
                try
                {
                    Interlocked.Increment(ref _postCount);
                    callback(state);
                }
                catch (Exception exc)
                {
                    lock (_postExceptions)
                    {
                        _postExceptions.Add(exc);
                    }
                }
            }
        }

        #endregion
    }
}
