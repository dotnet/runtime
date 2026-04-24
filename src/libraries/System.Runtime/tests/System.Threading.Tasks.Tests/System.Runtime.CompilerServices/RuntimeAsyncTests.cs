// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class RuntimeAsyncTests
    {
        private static bool IsRemoteExecutorAndRuntimeAsyncSupported => RemoteExecutor.IsSupported && PlatformDetection.IsRuntimeAsyncSupported;

        private static readonly FieldInfo s_asyncDebuggingEnabledField = GetCorLibClassStaticField("System.Threading.Tasks.Task", "s_asyncDebuggingEnabled");
        private static readonly FieldInfo s_taskTimestampsField = GetCorLibClassStaticField("System.Threading.Tasks.Task", "s_runtimeAsyncTaskTimestamps");
        private static readonly FieldInfo s_continuationTimestampsField = GetCorLibClassStaticField("System.Threading.Tasks.Task", "s_runtimeAsyncContinuationTimestamps");
        private static readonly FieldInfo s_activeTasksField = GetCorLibClassStaticField("System.Threading.Tasks.Task", "s_currentActiveTasks");
        private static readonly FieldInfo s_activeFlagsField = GetCorLibClassStaticField("System.Runtime.CompilerServices.AsyncInstrumentation", "s_activeFlags");

        private static readonly object s_debuggerLock = new object();
        private static TestEventListener? s_debuggerTplInstance;

        // AsyncDebugger(0x2000000) | all event flags(0x7F)
        private const uint EnabledInstrumentationFlags = 0x200007F;
        private const uint DisabledInstrumentationFlags = 0x0;
        private const uint UninitializedInstrumentationFlags = 0x80000000;

        private static void AttachDebugger()
        {
            // Simulate a debugger attach to process, creating TPL event source session + setting s_asyncDebuggingEnabled.
            lock (s_debuggerLock)
            {
                uint flags = Convert.ToUInt32(s_activeFlagsField.GetValue(null));
                Assert.True(flags == UninitializedInstrumentationFlags || flags == DisabledInstrumentationFlags, $"ActiveFlags equals {flags}, expected {UninitializedInstrumentationFlags} || {DisabledInstrumentationFlags}");

                s_debuggerTplInstance = new TestEventListener("System.Threading.Tasks.TplEventSource", EventLevel.Verbose);
                s_asyncDebuggingEnabledField.SetValue(null, true);

                // Initialize flags and collections.
                Func().GetAwaiter().GetResult();

                flags = Convert.ToUInt32(s_activeFlagsField.GetValue(null));
                Assert.True(flags == EnabledInstrumentationFlags, $"ActiveFlags equals {flags}, expected {EnabledInstrumentationFlags}");

                var activeTasks = (Dictionary<int, Task>)s_activeTasksField.GetValue(null);
                Assert.True(activeTasks != null, "Expected active tasks dictionary to be initialized");

                var taskTimestamps = (Dictionary<int, long>)s_taskTimestampsField.GetValue(null);
                Assert.True(taskTimestamps != null, "Expected tasks timestamps dictionary to be initialized");

                var continuationTimestamps = (Dictionary<object, long>)s_continuationTimestampsField.GetValue(null);
                Assert.True(continuationTimestamps != null, "Expected continuation timestamps dictionary to be initialized");
            }
        }

        private static void DetachDebugger()
        {
            // Simulate a debugger detach from process.
            lock (s_debuggerLock)
            {
                s_asyncDebuggingEnabledField.SetValue(null, false);
                s_debuggerTplInstance?.Dispose();
                s_debuggerTplInstance = null;

                uint flags = Convert.ToUInt32(s_activeFlagsField.GetValue(null));
                Assert.True(flags == DisabledInstrumentationFlags, $"ActiveFlags equals {flags}, expected {DisabledInstrumentationFlags}");
            }
        }

        private static FieldInfo GetCorLibClassStaticField(string className, string fieldName)
        {
            Type? classType = typeof(object).Assembly.GetType(className);
            if (classType == null)
            {
                throw new InvalidOperationException($"Type '{className}' doesn't exist in System.Private.CoreLib.");
            }

            FieldInfo? field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                throw new InvalidOperationException($"Expected static field '{fieldName}' to exist on type '{className}'.");
            }

            return field;
        }

        private static int GetTaskTimestampCount() =>
            s_taskTimestampsField.GetValue(null) is Dictionary<int, long> dict ? dict.Count : 0;

        private static int GetContinuationTimestampCount() =>
            s_continuationTimestampsField.GetValue(null) is Dictionary<object, long> dict ? dict.Count : 0;

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task Func()
        {
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task FuncThatThrows()
        {
            await DeepThrow1();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepThrow1()
        {
            await DeepThrow2();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepThrow2()
        {
            await Task.Yield();
            throw new InvalidOperationException("test exception");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task<int> FuncWithResult()
        {
            await Task.Yield();
            return 42;
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task OuterFuncThatCatches()
        {
            try
            {
                await MiddleFuncThatPropagates();
            }
            catch (InvalidOperationException)
            {
            }
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task MiddleFuncThatPropagates()
        {
            await InnerFuncThatThrows();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task InnerFuncThatThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("inner exception");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task FuncThatCancels(CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task FuncThatWaitsTwice(TaskCompletionSource tcs1, TaskCompletionSource tcs2)
        {
            await tcs1.Task;
            await tcs2.Task;
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task FuncThatInspectsContinuationTimestamps(TaskCompletionSource tcs, Action callback)
        {
            await tcs.Task;
            callback();
        }

        static void ValidateTimestampsCleared()
        {
            // some other tasks may be created by the runtime, so this is just using a reasonably small upper bound
            Assert.InRange(GetTaskTimestampCount(), 0, 10);
            Assert.InRange(GetContinuationTimestampCount(), 0, 10);
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_TaskCompleted()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                for (int i = 0; i < 1000; i++)
                {
                    await Func();
                }

                ValidateTimestampsCleared();

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_ExceptionCleanup()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                for (int i = 0; i < 1000; i++)
                {
                    try
                    {
                        await FuncThatThrows();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }

                ValidateTimestampsCleared();

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_DebuggerDetach()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                var activeTasks = (Dictionary<int, Task>)s_activeTasksField.GetValue(null);

                // Use an in-flight task to deterministically verify tracking is active
                var tcs = new TaskCompletionSource();
                Task inflight = FuncThatWaitsTwice(tcs, new TaskCompletionSource());

                lock (activeTasks)
                {
                    Assert.True(activeTasks.ContainsKey(inflight.Id),
                        "Expected in-flight task to be tracked while debugger is attached");
                }

                // Complete the first await so it resumes and sets a task timestamp
                tcs.SetResult();
                var taskTimestamps = (Dictionary<int, long>)s_taskTimestampsField.GetValue(null);

                bool seenTimestamp = SpinWait.SpinUntil(() =>
                {
                    lock (taskTimestamps)
                    {
                        return taskTimestamps.ContainsKey(inflight.Id);
                    }
                }, timeout: TimeSpan.FromSeconds(5));

                Assert.True(seenTimestamp, "Timed out waiting for task timestamp");

                // Simulate debugger detach — the flag sync should detect the mismatch
                // and disable the Debugger flags without crashing.
                // Timestamps are not cleared (matches existing behavior).
                DetachDebugger();

                // Run one task to trigger the flag sync that detects the mismatch
                await Func();

                // Now start a new in-flight task after detach - it should NOT be tracked
                var tcsPost = new TaskCompletionSource();
                Task postDetach = FuncThatWaitsTwice(tcsPost, new TaskCompletionSource());

                lock (activeTasks)
                {
                    Assert.False(activeTasks.ContainsKey(postDetach.Id),
                        "Expected in-flight task NOT to be tracked after debugger detach");
                }

                lock (taskTimestamps)
                {
                    Assert.False(taskTimestamps.ContainsKey(postDetach.Id),
                        "Expected no task timestamp for post-detach in-flight task");
                }

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_ValueTypeResult()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                for (int i = 0; i < 1000; i++)
                {
                    int result = await FuncWithResult();
                    Assert.Equal(42, result);
                }

                ValidateTimestampsCleared();

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_HandledExceptionPartialUnwind()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                for (int i = 0; i < 1000; i++)
                {
                    await OuterFuncThatCatches();
                }

                ValidateTimestampsCleared();

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_CancellationCleanup()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                using var cts = new CancellationTokenSource();
                cts.Cancel();

                for (int i = 0; i < 1000; i++)
                {
                    try
                    {
                        await FuncThatCancels(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                ValidateTimestampsCleared();

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_TimestampsTrackedWhileInFlight()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                var tcs1 = new TaskCompletionSource();
                var tcs2 = new TaskCompletionSource();
                Task inflight = FuncThatWaitsTwice(tcs1, tcs2);

                // Task is suspended on tcs1 — should be in active tasks
                var activeTasks = (Dictionary<int, Task>)s_activeTasksField.GetValue(null);

                lock (activeTasks)
                {
                    Assert.True(activeTasks.ContainsKey(inflight.Id), "Expected suspended task to be in s_currentActiveTasks");
                }

                // Resume from first suspension — this triggers DispatchContinuations which sets timestamps
                tcs1.SetResult();

                // Poll until the dispatch loop has resumed and re-suspended on tcs2,
                // which sets the task timestamp via ResumeRuntimeAsyncMethod.
                var taskTimestamps = (Dictionary<int, long>)s_taskTimestampsField.GetValue(null);

                bool seenTimestamp = SpinWait.SpinUntil(() =>
                {
                    lock (taskTimestamps)
                    {
                        return taskTimestamps.ContainsKey(inflight.Id);
                    }
                }, timeout: TimeSpan.FromSeconds(5));

                Assert.True(seenTimestamp, "Timed out waiting for task timestamp to appear after resume");

                // Now the task has been through one resume cycle and is suspended again on tcs2.
                lock (taskTimestamps)
                {
                    Assert.True(taskTimestamps[inflight.Id] > 0, "Expected non-zero task timestamp");
                }

                // Complete the task
                tcs2.SetResult();
                await inflight;

                // After completion the task should be removed from all collections
                lock (activeTasks)
                {
                    Assert.False(activeTasks.ContainsKey(inflight.Id), "Expected completed task to be removed from s_currentActiveTasks");
                }

                lock (taskTimestamps)
                {
                    Assert.False(taskTimestamps.ContainsKey(inflight.Id), "Expected task timestamp to be removed after completion");
                }

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_ContinuationTimestampObservedDuringResume()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AttachDebugger();

                bool continuationTimestampObserved = false;

                var tcs = new TaskCompletionSource();
                Task inflight = FuncThatInspectsContinuationTimestamps(tcs, () =>
                {
                    // This callback runs inside the resumed async method body, after
                    // ResumeRuntimeAsyncMethod but before SuspendRuntimeAsyncContext/CompleteRuntimeAsyncMethod.
                    // The continuation timestamp for the current continuation should still be in the dictionary.
                    var continuationTimestamps = (Dictionary<object, long>)s_continuationTimestampsField.GetValue(null);
                    lock (continuationTimestamps)
                    {
                        continuationTimestampObserved = continuationTimestamps.Count > 0;
                    }
                });

                tcs.SetResult();
                await inflight;

                Assert.True(continuationTimestampObserved, "Expected continuation timestamp to be present during resumed method execution");

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_InFlightInstrumentationUpgrade()
        {
            RemoteExecutor.Invoke(async () =>
            {
                // Start a multi-await task WITHOUT instrumentation enabled.
                var tcs1 = new TaskCompletionSource();
                var tcs2 = new TaskCompletionSource();
                Task inflight = FuncThatWaitsTwice(tcs1, tcs2);

                // Task is now suspended at first await with no instrumentation.
                // Attach the debugger mid-flight — this enables instrumentation.
                AttachDebugger();

                var activeTasks = (Dictionary<int, Task>)s_activeTasksField.GetValue(null);
                var taskTimestamps = (Dictionary<int, long>)s_taskTimestampsField.GetValue(null);

                // The in-flight task was NOT tracked at creation (no instrumentation then).
                lock (activeTasks)
                {
                    Assert.False(activeTasks.ContainsKey(inflight.Id),
                        "Expected in-flight task NOT to be tracked (started before instrumentation)");
                }

                // Resume the first await — the dispatch loop's InstrumentCheckPoint should
                // detect that instrumentation is now active and transition to the instrumented path.
                tcs1.SetResult();

                // Wait for the task to suspend at the second await.
                bool seenTimestamp = SpinWait.SpinUntil(() =>
                {
                    lock (taskTimestamps)
                    {
                        return taskTimestamps.ContainsKey(inflight.Id);
                    }
                }, timeout: TimeSpan.FromSeconds(5));

                Assert.True(seenTimestamp,
                    "Expected task timestamp after mid-flight instrumentation upgrade (InstrumentCheckPoint transition)");

                // Complete the second await and let the task finish.
                tcs2.SetResult();
                await inflight;

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_TplEvents()
        {
            RemoteExecutor.Invoke(() =>
            {
                const int TraceOperationBeginId = 14;
                const int TraceOperationEndId = 15;
                const int TraceSynchronousWorkBeginId = 17;
                const int TraceSynchronousWorkEndId = 18;

                AttachDebugger();

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                using (var listener = new TestEventListener("System.Threading.Tasks.TplEventSource", EventLevel.Verbose))
                {
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Func().GetAwaiter().GetResult();
                        }
                    });
                }

                Assert.Contains(events, e => e.EventId == TraceOperationBeginId);
                Assert.Contains(events, e => e.EventId == TraceOperationEndId);
                Assert.Contains(events, e => e.EventId == TraceSynchronousWorkBeginId);
                Assert.Contains(events, e => e.EventId == TraceSynchronousWorkEndId);

                DetachDebugger();

            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_NoTplEventsWithoutDebugger()
        {
            RemoteExecutor.Invoke(() =>
            {
                const int TraceOperationBeginId = 14;
                const string RuntimeAsyncTaskOperationName = "System.Runtime.CompilerServices.AsyncHelpers+RuntimeAsyncTask";

                // Enable TplEventSource WITHOUT setting s_asyncDebuggingEnabled.
                // The AsyncDebugger guard should prevent the V2 async instrumentation
                // from emitting any TPL causality events.
                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                using (var listener = new TestEventListener("System.Threading.Tasks.TplEventSource", EventLevel.Verbose))
                {
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Func().GetAwaiter().GetResult();
                        }
                    });
                }

                // TraceOperationBegin with the RuntimeAsyncTask operation name is uniquely
                // emitted by V2 async instrumentation. It must not appear without a debugger.
                Assert.DoesNotContain(events, e =>
                    e.EventId == TraceOperationBeginId &&
                    e.Payload?.Count > 1 &&
                    e.Payload[1] is string name &&
                    name == RuntimeAsyncTaskOperationName);

            }).Dispose();
        }
    }
}
