// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace System.Threading.Tasks.Tests
{
    public class AsyncTaskMethodBuilderTests
    {
        // Test captured sync context with successful completion (SetResult)
        [Fact]
        public static void VoidMethodBuilder_TrackedContext()
        {
            SynchronizationContext previousContext = SynchronizationContext.Current;
            try
            {
                var trackedContext = new TrackOperationsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(trackedContext);

                // TrackedCount should increase as Create() is called, and decrease as SetResult() is called.

                // Completing in opposite order as created.
                var avmb1 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(1, trackedContext.TrackedCount);
                var avmb2 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(2, trackedContext.TrackedCount);
                avmb2.SetResult();
                Assert.Equal(1, trackedContext.TrackedCount);
                avmb1.SetResult();
                Assert.Equal(0, trackedContext.TrackedCount);

                // Completing in same order as created
                avmb1 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(1, trackedContext.TrackedCount);
                avmb2 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(2, trackedContext.TrackedCount);
                avmb1.SetResult();
                Assert.Equal(1, trackedContext.TrackedCount);
                avmb2.SetResult();
                Assert.Equal(0, trackedContext.TrackedCount);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        // Test not having a sync context with successful completion (SetResult)
        [Fact]
        public static void VoidMethodBuilder_NoContext()
        {
            SynchronizationContext previousContext = SynchronizationContext.Current;
            try
            {
                // Make sure not having a sync context doesn't cause us to blow up
                SynchronizationContext.SetSynchronizationContext(null);
                var avmb = AsyncVoidMethodBuilder.Create();
                avmb.SetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        // AsyncTaskMethodBuilder
        [Fact]
        public static void TaskMethodBuilder_Basic()
        {
            // Creating a task builder, building it, completing it successfully
            {
                var atmb = AsyncTaskMethodBuilder.Create();
                var t = atmb.Task;
                Assert.Equal(TaskStatus.WaitingForActivation, t.Status);
                atmb.SetResult();
                Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            }
        }

        [Fact]
        public static void TaskMethodBuilder_DoesNotTouchSyncContext()
        {
            // Verify that AsyncTaskMethodBuilder is not touching sync context
            SynchronizationContext previousContext = SynchronizationContext.Current;
            try
            {
                var trackedContext = new TrackOperationsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(trackedContext);

                var atmb = AsyncTaskMethodBuilder.Create();
                Assert.Equal(0, trackedContext.TrackedCount);
                atmb.SetResult();
                Assert.Equal(0, trackedContext.TrackedCount);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        // AsyncTaskMethodBuilder<T>

        [Fact]
        public static void TaskMethodBuilderT_Basic()
        {
            // Creating a task builder, building it, completing it successfully
            var atmb = AsyncTaskMethodBuilder<int>.Create();
            var t = atmb.Task;
            Assert.Equal(TaskStatus.WaitingForActivation, t.Status);
            atmb.SetResult(43);
            Assert.Equal(TaskStatus.RanToCompletion, t.Status);
            Assert.Equal(43, t.Result);
        }

        [Fact]
        public static void TaskMethodBuilderT_DoesNotTouchSyncContext()
        {
            // Verify that AsyncTaskMethodBuilder<T> is not touching sync context
            SynchronizationContext previousContext = SynchronizationContext.Current;
            try
            {
                var trackedContext = new TrackOperationsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(trackedContext);

                var atmb = AsyncTaskMethodBuilder<string>.Create();
                Assert.Equal(0, trackedContext.TrackedCount);
                atmb.SetResult("async");
                Assert.Equal(0, trackedContext.TrackedCount);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        // Incorrect usage for AsyncTaskMethodBuilder
        [Fact]
        public static void TaskMethodBuilder_IncorrectUsage()
        {
            var atmb = new AsyncTaskMethodBuilder();
            Assert.Throws<ArgumentNullException>(() => { atmb.SetException(null); });
        }

        // Incorrect usage for AsyncVoidMethodBuilder
        [Fact]
        public static void VoidMethodBuilder_IncorrectUsage()
        {
            var avmb = AsyncVoidMethodBuilder.Create();
            Assert.Throws<ArgumentNullException>(() => { avmb.SetException(null); });
            avmb.SetResult();
        }

        // Creating a task builder, building it, completing it successfully, and making sure it can't be reset
        [Fact]
        public static void TaskMethodBuilder_CantBeReset()
        {
            var atmb = AsyncTaskMethodBuilder.Create();
            atmb.SetResult();
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        // Incorrect usage for AsyncTaskMethodBuilder<T>
        [Fact]
        public static void TaskMethodBuilderT_IncorrectUsage()
        {
            var atmb = new AsyncTaskMethodBuilder<int>();
            Assert.Throws<ArgumentNullException>(() => { atmb.SetException(null); });
        }

        // Creating a task builder <T>, building it, completing it successfully, and making sure it can't be reset
        [Fact]
        public static void TaskMethodBuilderT_CantBeReset()
        {
            var atmb = AsyncTaskMethodBuilder<int>.Create();
            atmb.SetResult(43);
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(44); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        // Creating a task builder, building it, completing it faulted, and making sure it can't be reset
        [Fact]
        public static void TaskMethodBuilder_SetException_CantBeReset0()
        {
            var atmb = AsyncTaskMethodBuilder.Create();
            var t = atmb.Task;
            Assert.Equal(TaskStatus.WaitingForActivation, t.Status);
            atmb.SetException(new InvalidCastException());
            Assert.Equal(TaskStatus.Faulted, t.Status);
            Assert.True(t.Exception.InnerException is InvalidCastException, "Wrong exception found in builder (ATMB, build then fault)");
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        // Creating a task builder, completing it faulted, building it, and making sure it can't be reset
        [Fact]
        public static void TaskMethodBuilder_SetException_CantBeReset1()
        {
            var atmb = AsyncTaskMethodBuilder.Create();
            atmb.SetException(new InvalidCastException());
            var t = atmb.Task;
            Assert.Equal(TaskStatus.Faulted, t.Status);
            Assert.True(t.Exception.InnerException is InvalidCastException, "Wrong exception found in builder (ATMB, fault then build)");
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        // Test cancellation
        [Fact]
        public static void TaskMethodBuilder_Cancellation()
        {
            var atmb = AsyncTaskMethodBuilder.Create();
            var oce = new OperationCanceledException();
            atmb.SetException(oce);
            var t = atmb.Task;
            Assert.Equal(TaskStatus.Canceled, t.Status);

            OperationCanceledException caught = Assert.Throws<OperationCanceledException>(() =>
            {
                t.GetAwaiter().GetResult();
            });
            Assert.Same(oce, caught);
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        [Fact]
        public static void AsyncMethodBuilderCreate_SetExceptionTest2()
        {
            // Test captured sync context with exceptional completion

            SynchronizationContext previousContext = SynchronizationContext.Current;
            try
            {
                var trackedContext = new TrackOperationsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(trackedContext);

                // Completing in opposite order as created
                var avmb1 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(1, trackedContext.TrackedCount);
                var avmb2 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(2, trackedContext.TrackedCount);
                avmb2.SetException(new InvalidOperationException("uh oh 1"));
                Assert.Equal(1, trackedContext.TrackedCount);
                avmb1.SetException(new InvalidCastException("uh oh 2"));
                Assert.Equal(0, trackedContext.TrackedCount);

                Assert.Equal(2, trackedContext.PostExceptions.Count);
                Assert.IsType<InvalidOperationException>(trackedContext.PostExceptions[0]);
                Assert.IsType<InvalidCastException>(trackedContext.PostExceptions[1]);

                // Completing in same order as created
                var avmb3 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(1, trackedContext.TrackedCount);
                var avmb4 = AsyncVoidMethodBuilder.Create();
                Assert.Equal(2, trackedContext.TrackedCount);
                avmb3.SetException(new InvalidOperationException("uh oh 3"));
                Assert.Equal(1, trackedContext.TrackedCount);
                avmb4.SetException(new InvalidCastException("uh oh 4"));
                Assert.Equal(0, trackedContext.TrackedCount);

                Assert.Equal(4, trackedContext.PostExceptions.Count);
                Assert.IsType<InvalidOperationException>(trackedContext.PostExceptions[2]);
                Assert.IsType<InvalidCastException>(trackedContext.PostExceptions[3]);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        // Creating a task builder, building it, completing it faulted, and making sure it can't be reset
        [Fact]
        public static void TaskMethodBuilderT_SetExceptionTest0()
        {
            var atmb = AsyncTaskMethodBuilder<int>.Create();
            var t = atmb.Task;
            Assert.Equal(TaskStatus.WaitingForActivation, t.Status);
            atmb.SetException(new InvalidCastException());
            Assert.Equal(TaskStatus.Faulted, t.Status);
            Assert.IsType<InvalidCastException>(t.Exception.InnerException);
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(44); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        // Creating a task builder, completing it faulted, building it, and making sure it can't be reset
        [Fact]
        public static void TaskMethodBuilderT_SetExceptionTest1()
        {
            var atmb = AsyncTaskMethodBuilder<int>.Create();
            atmb.SetException(new InvalidCastException());
            var t = atmb.Task;
            Assert.Equal(TaskStatus.Faulted, t.Status);
            Assert.IsType<InvalidCastException>(t.Exception.InnerException);
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(44); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        // Test cancellation
        [Fact]
        public static void TaskMethodBuilderT_Cancellation()
        {
            var atmb = AsyncTaskMethodBuilder<int>.Create();
            var oce = new OperationCanceledException();
            atmb.SetException(oce);
            var t = atmb.Task;
            Assert.Equal(TaskStatus.Canceled, t.Status);

            OperationCanceledException e = Assert.Throws<OperationCanceledException>(() =>
            {
                t.GetAwaiter().GetResult();
            });
            Assert.Same(oce, e);
            Assert.Throws<InvalidOperationException>(() => { atmb.SetResult(44); });
            Assert.Throws<InvalidOperationException>(() => { atmb.SetException(new Exception()); });
        }

        [Fact]
        public static void TaskMethodBuilder_TaskIsCached()
        {
            var atmb = new AsyncTaskMethodBuilder();
            var t1 = atmb.Task;
            var t2 = atmb.Task;
            Assert.NotNull(t1);
            Assert.NotNull(t2);
            Assert.Same(t1, t2);
        }

        [Fact]
        public static void TaskMethodBuilderT_TaskIsCached()
        {
            var atmb = new AsyncTaskMethodBuilder<int>();
            var t1 = atmb.Task;
            var t2 = atmb.Task;
            Assert.NotNull(t1);
            Assert.NotNull(t2);
            Assert.Same(t1, t2);
        }

        [Fact]
        public static void TaskMethodBuilder_UsesCompletedCache()
        {
            var atmb1 = new AsyncTaskMethodBuilder();
            var atmb2 = new AsyncTaskMethodBuilder();
            atmb1.SetResult();
            atmb2.SetResult();
            Assert.Same(atmb1.Task, atmb2.Task);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TaskMethodBuilderBoolean_UsesCompletedCache(bool result)
        {
            TaskMethodBuilderT_UsesCompletedCache(result, true);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(5, true)]
        [InlineData(-5, false)]
        [InlineData(42, false)]
        public static void TaskMethodBuilderInt32_UsesCompletedCache(int result, bool shouldBeCached)
        {
            TaskMethodBuilderT_UsesCompletedCache(result, shouldBeCached);
        }

        [Fact]
        public static void TaskMethodBuilderDecimal_DoesntUseCompletedCache()
        {
            TaskMethodBuilderT_UsesCompletedCache(0m, shouldBeCached: false);
            TaskMethodBuilderT_UsesCompletedCache(0.0m, shouldBeCached: false);
            TaskMethodBuilderT_UsesCompletedCache(42m, shouldBeCached: false);
        }

        [Theory]
        [InlineData((string)null, true)]
        [InlineData("test", false)]
        public static void TaskMethodBuilderRef_UsesCompletedCache(string result, bool shouldBeCached)
        {
            TaskMethodBuilderT_UsesCompletedCache(result, shouldBeCached);
        }

        private static void TaskMethodBuilderT_UsesCompletedCache<T>(T result, bool shouldBeCached)
        {
            var atmb1 = new AsyncTaskMethodBuilder<T>();
            var atmb2 = new AsyncTaskMethodBuilder<T>();

            atmb1.SetResult(result);
            atmb2.SetResult(result);

            Assert.Equal(shouldBeCached, object.ReferenceEquals(atmb1.Task, atmb2.Task));
            if (result != null)
            {
                Assert.Equal(result.ToString(), atmb1.Task.Result.ToString());
                Assert.Equal(result.ToString(), atmb2.Task.Result.ToString());
            }
        }

        [Fact]
        public static void Tcs_ValidateFaultedTask()
        {
            var tcs = new TaskCompletionSource<int>();
            try { throw new InvalidOperationException(); }
            catch (Exception e) { tcs.SetException(e); }
            ValidateFaultedTask(tcs.Task);
        }

        [Fact]
        public static void TaskMethodBuilder_ValidateFaultedTask()
        {
            var atmb = AsyncTaskMethodBuilder.Create();
            try { throw new InvalidOperationException(); }
            catch (Exception e) { atmb.SetException(e); }
            ValidateFaultedTask(atmb.Task);
        }

        [Fact]
        public static void TaskMethodBuilderT_ValidateFaultedTask()
        {
            var atmbtr = AsyncTaskMethodBuilder<object>.Create();
            try { throw new InvalidOperationException(); }
            catch (Exception e) { atmbtr.SetException(e); }
            ValidateFaultedTask(atmbtr.Task);
        }

        [Fact]
        public static void TrackedSyncContext_ValidateException()
        {
            SynchronizationContext previousContext = SynchronizationContext.Current;
            try
            {
                var tosc = new TrackOperationsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(tosc);
                var avmb = AsyncVoidMethodBuilder.Create();
                try { throw new InvalidOperationException(); }
                catch (Exception exc) { avmb.SetException(exc); }
                Assert.NotEmpty(tosc.PostExceptions);
                ValidateException(tosc.PostExceptions[0]);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        // Running tasks with exceptions.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void FaultedTaskExceptions()
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
                ValidateFaultedTask(tasks[i]);
            }

            ((IAsyncResult)twa1).AsyncWaitHandle.WaitOne();
            ((IAsyncResult)twa2).AsyncWaitHandle.WaitOne();
            Exception ignored = twa1.Exception;
            ignored = twa2.Exception;
        }

        // Test that OCEs don't result in the unobserved event firing
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CancellationDoesntResultInEventFiring()
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

            // This test may be run concurrently with other tests in the suite,
            // which can be problematic as TaskScheduler.UnobservedTaskException
            // is global state.  The handler is carefully written to be non-problematic
            // if it happens to be set during the execution of another test that has
            // an unobserved exception.
            EventHandler<UnobservedTaskExceptionEventArgs> handler =
                (s, e) => Assert.DoesNotContain(oce, e.Exception.InnerExceptions);
            TaskScheduler.UnobservedTaskException += handler;
            ((IAsyncResult)t1).AsyncWaitHandle.WaitOne();
            t1 = null;
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            TaskScheduler.UnobservedTaskException -= handler;
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/30122")]
        [Fact]
        public static async Task AsyncMethodsDropsStateMachineAndExecutionContextUponCompletion()
        {
            // Create a finalizable object that'll be referenced by both an async method's
            // captured ExecutionContext and its state machine, invoke the method, wait for it,
            // and then hold on to the resulting task while forcing GCs and finalizers.
            // We want to make sure that holding on to the resulting Task doesn't keep
            // that finalizable object alive.

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Task t = null;

            Thread runner = new Thread(() =>
            {
                async Task YieldOnceAsync(object s)
                {
                    await Task.Yield();
                    GC.KeepAlive(s); // keep s referenced by the state machine
                }

                var state = new InvokeActionOnFinalization { Action = () => tcs.SetResult() };
                var al = new AsyncLocal<object>() { Value = state }; // ensure the object is stored in ExecutionContext
                t = YieldOnceAsync(state); // ensure the object is stored in the state machine
                al.Value = null;
            }) { IsBackground = true };

            runner.Start();
            runner.Join();

            await t; // wait for the async method to complete and clear out its state
            await Task.Yield(); // ensure associated state is not still on the stack as part of the antecedent's execution

            for (int i = 0; i < 2; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
            }
            catch (Exception e)
            {
                Environment.FailFast("Look at the created dump", e);
            }

            GC.KeepAlive(t); // ensure the object is stored in the state machine
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DroppedIncompleteStateMachine_RaisesIncompleteAsyncMethodEvent()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("System.Threading.Tasks.TplEventSource", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        NeverCompletes();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.WaitForPendingFinalizers();
                    });

                    // To help diagnose https://github.com/dotnet/runtime/issues/2198
                    // Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    var sb = new StringBuilder();
                    foreach (EventWrittenEventArgs ev in events)
                    {
                        if (ev.EventId == 0)
                        {
                            sb.AppendLine("Events contained unexpected event:")
                              .AppendLine($"ActivityId: {ev.ActivityId}")
                              .AppendLine($"Channel: {ev.Channel}")
                              .AppendLine($"EventId: {ev.EventId}")
                              .AppendLine($"EventName: {ev.EventName}")
                              .AppendLine($"EventSource: {ev.EventSource}")
                              .AppendLine($"Keywords: {ev.Keywords}")
                              .AppendLine($"Level: {ev.Level}")
                              .AppendLine($"Message: {ev.Message}")
                              .AppendLine($"Opcode: {ev.Opcode}")
                              .AppendLine($"OSThreadId: {ev.OSThreadId}")
                              .AppendLine($"Payload: {(ev.Payload != null ? string.Join(", ", ev.Payload) : "(null)")}")
                              .AppendLine($"PayloadNames: {(ev.PayloadNames != null ? string.Join(", ", ev.PayloadNames) : "(null)")}")
                              .AppendLine($"RelatedActivityId: {ev.RelatedActivityId}")
                              .AppendLine($"Tags: {ev.Tags}")
                              .AppendLine($"Task: {ev.Task}")
                              .AppendLine($"TimeStamp: {ev.TimeStamp}")
                              .AppendLine($"Version: {ev.Version}")
                              .AppendLine();
                        }
                    }
                    if (sb.Length > 0)
                    {
                        throw new XunitException(sb.ToString());
                    }

                    EventWrittenEventArgs iam = events.SingleOrDefault(e => e.EventName == "IncompleteAsyncMethod");
                    Assert.NotNull(iam);
                    Assert.NotNull(iam.Payload);

                    string description = iam.Payload[0] as string;
                    Assert.NotNull(description);
                    Assert.Contains(nameof(NeverCompletesAsync), description);
                    Assert.Contains("__state", description);
                    Assert.Contains("local1", description);
                    Assert.Contains("local2", description);
                    Assert.Contains("42", description);
                    Assert.Contains("stored data", description);
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AsyncTaskMethodBuilder_Completed_RemovedFromTracking()
        {
            RemoteExecutor.Invoke(() =>
            {
                // NOTE: This depends on private implementation details generally only used by the debugger.
                // If those ever change, this test will need to be updated as well.
                typeof(Task).GetField("s_asyncDebuggingEnabled", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);

                for (int i = 0; i < 1000; i++)
                {
                    static async Task YieldAsync(TaskCompletionSource tcs) => await tcs.Task;

                    TaskCompletionSource tcs = new();
                    Task t = YieldAsync(tcs);
                    tcs.SetResult();
                    t.Wait();
                }

                int activeCount = ((dynamic)typeof(Task).GetField("s_currentActiveTasks", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Count;
                Assert.InRange(activeCount, 0, 10); // some other tasks may be created by the runtime, so this is just using a reasonably small upper bound
            }).Dispose();
        }

        #region Helper Methods / Classes

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void NeverCompletes() { var ignored = NeverCompletesAsync(); }

        private static async Task NeverCompletesAsync()
        {
            int local1 = 42;
            string local2 = "stored data";
            await new TaskCompletionSource().Task; // await will never complete
            GC.KeepAlive(local1);
            GC.KeepAlive(local2);
        }

        private static void ValidateFaultedTask(Task t)
        {
            ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
            Assert.True(t.IsFaulted);
            Exception e = Assert.ThrowsAny<Exception>(() =>
            {
                t.GetAwaiter().GetResult();
            });
            ValidateException(e);
        }

        private static void ValidateException(Exception e)
        {
            Assert.NotNull(e);
            Assert.NotNull(e.StackTrace);
            Assert.Matches(@"---.+---", e.StackTrace);
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
