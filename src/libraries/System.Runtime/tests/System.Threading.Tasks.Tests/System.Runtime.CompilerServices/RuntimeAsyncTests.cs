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

        // NOTE: This depends on private implementation details generally only used by the debugger.
        // If those ever change, this test will need to be updated as well.
        private static FieldInfo AsyncDebuggingEnabledField => typeof(Task).GetField("s_asyncDebuggingEnabled", BindingFlags.NonPublic | BindingFlags.Static);
        private static FieldInfo TaskTimestampsField => typeof(Task).GetField("s_runtimeAsyncTaskTimestamps", BindingFlags.NonPublic | BindingFlags.Static);
        private static FieldInfo ContinuationTimestampsField => typeof(Task).GetField("s_runtimeAsyncContinuationTimestamps", BindingFlags.NonPublic | BindingFlags.Static);
        private static FieldInfo ActiveTasksField => typeof(Task).GetField("s_currentActiveTasks", BindingFlags.NonPublic | BindingFlags.Static);

        private static int GetTaskTimestampCount() =>
            TaskTimestampsField.GetValue(null) is Dictionary<int, long> dict ? dict.Count : 0;

        private static int GetContinuationTimestampCount() =>
            ContinuationTimestampsField.GetValue(null) is Dictionary<object, long> dict ? dict.Count : 0;

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
                AsyncDebuggingEnabledField.SetValue(null, true);

                for (int i = 0; i < 1000; i++)
                {
                    await Func();
                }

                ValidateTimestampsCleared();
            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_ExceptionCleanup()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AsyncDebuggingEnabledField.SetValue(null, true);

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
            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_DebuggerDetach()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AsyncDebuggingEnabledField.SetValue(null, true);

                // Run one task to ensure lazy-initialized collections are created
                await Func();

                var activeTasks = (Dictionary<int, Task>)ActiveTasksField.GetValue(null);

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
                var taskTimestamps = (Dictionary<int, long>)TaskTimestampsField.GetValue(null);

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
                AsyncDebuggingEnabledField.SetValue(null, false);

                // Run one task to trigger the flag sync that detects the mismatch
                await Func();

                // Now start a new in-flight task after detach — it should NOT be tracked
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
                AsyncDebuggingEnabledField.SetValue(null, true);

                for (int i = 0; i < 1000; i++)
                {
                    int result = await FuncWithResult();
                    Assert.Equal(42, result);
                }

                ValidateTimestampsCleared();
            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_HandledExceptionPartialUnwind()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AsyncDebuggingEnabledField.SetValue(null, true);

                for (int i = 0; i < 1000; i++)
                {
                    await OuterFuncThatCatches();
                }

                ValidateTimestampsCleared();
            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_CancellationCleanup()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AsyncDebuggingEnabledField.SetValue(null, true);

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
            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_TimestampsTrackedWhileInFlight()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AsyncDebuggingEnabledField.SetValue(null, true);

                // Run one task to ensure lazy-initialized collections are created
                await Func();

                var tcs1 = new TaskCompletionSource();
                var tcs2 = new TaskCompletionSource();
                Task inflight = FuncThatWaitsTwice(tcs1, tcs2);

                // Task is suspended on tcs1 — should be in active tasks
                var activeTasks = (Dictionary<int, Task>)ActiveTasksField.GetValue(null);

                lock (activeTasks)
                {
                    Assert.True(activeTasks.ContainsKey(inflight.Id), "Expected suspended task to be in s_currentActiveTasks");
                }

                // Resume from first suspension — this triggers DispatchContinuations which sets timestamps
                tcs1.SetResult();

                // Poll until the dispatch loop has resumed and re-suspended on tcs2,
                // which sets the task timestamp via ResumeRuntimeAsyncMethod.
                var taskTimestamps = (Dictionary<int, long>)TaskTimestampsField.GetValue(null);

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
            }).Dispose();
        }

        [ConditionalFact(typeof(RuntimeAsyncTests), nameof(IsRemoteExecutorAndRuntimeAsyncSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124072", typeof(PlatformDetection), nameof(PlatformDetection.IsInterpreter))]
        public void RuntimeAsync_ContinuationTimestampObservedDuringResume()
        {
            RemoteExecutor.Invoke(async () =>
            {
                AsyncDebuggingEnabledField.SetValue(null, true);

                bool continuationTimestampObserved = false;

                var tcs = new TaskCompletionSource();
                Task inflight = FuncThatInspectsContinuationTimestamps(tcs, () =>
                {
                    // This callback runs inside the resumed async method body, after
                    // ResumeRuntimeAsyncMethod but before SuspendRuntimeAsyncContext/CompleteRuntimeAsyncMethod.
                    // The continuation timestamp for the current continuation should still be in the dictionary.
                    var continuationTimestamps = (Dictionary<object, long>)ContinuationTimestampsField.GetValue(null);
                    lock (continuationTimestamps)
                    {
                        continuationTimestampObserved = continuationTimestamps.Count > 0;
                    }
                });

                tcs.SetResult();
                await inflight;

                Assert.True(continuationTimestampObserved, "Expected continuation timestamp to be present during resumed method execution");
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
            }).Dispose();
        }
    }
}
