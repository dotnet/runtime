// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    /// <summary>
    /// Tests for V1 (Task-based AsyncStateMachineBox) async profiler event emission.
    /// All scenario methods use [RuntimeAsyncMethodGeneration(false)] to ensure they
    /// exercise the legacy Task-based async path even if the default changes in the future.
    /// Tests use sync CollectEvents with RunScenarioAndFlush to isolate the V1 chain
    /// on a threadpool thread, ensuring dispatcher finally blocks complete before flush.
    /// Requires threading support.
    /// </summary>
    public partial class AsyncProfilerTests
    {
        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_SingleYield()
        {
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_DeepChain()
        {
            await TaskAsync_Level1();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_Level1()
        {
            await TaskAsync_Level2();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_Level2()
        {
            await TaskAsync_Level3();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_Level3()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_MultiYield()
        {
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_ExceptionHandled()
        {
            try
            {
                await TaskAsync_InnerThrows();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_InnerThrows()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("v1 inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_UnhandledExceptionOuter()
        {
            await TaskAsync_UnhandledExceptionInner();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_UnhandledExceptionInner()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("v1 unhandled inner");
        }

        private static ulong AssertCompleteContextChain(ParsedEventStream stream, params AsyncEventID[] expectedSequence)
        {
            var byTask = stream.ByTaskId();
            var candidates = new List<string>();

            foreach (var (taskId, events) in byTask)
            {
                var contextEvents = events
                    .Where(e => e.EventId is AsyncEventID.CreateAsyncContext
                        or AsyncEventID.ResumeAsyncContext
                        or AsyncEventID.SuspendAsyncContext
                        or AsyncEventID.CompleteAsyncContext
                        or AsyncEventID.UnwindAsyncException)
                    .Select(e => e.EventId)
                    .ToList();

                candidates.Add($"  TaskId={taskId}: [{string.Join(", ", contextEvents)}]");

                if (contextEvents.Count != expectedSequence.Length)
                    continue;

                bool match = true;
                for (int i = 0; i < expectedSequence.Length; i++)
                {
                    if (contextEvents[i] != expectedSequence[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    return taskId;
            }

            string expected = string.Join(", ", expectedSequence);
            string found = string.Join(Environment.NewLine, candidates);
            Assert.Fail($"No context found with expected chain [{expected}].\nContexts found:\n{found}");
            return 0; // unreachable
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_EventsEmitted()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_SingleYield());
            });

            // DumpAllEvents(events);

            Assert.True(events.Events.Count > 0, "Expected at least one AsyncEvents event to be emitted");
            Assert.Contains(events.Events, e => e.EventId == AsyncEventsId);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CreateAsyncContextEmittedOnFirstAwait()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_SingleYield());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var creates = stream.OfType(AsyncEventID.CreateAsyncContext).ToList();
            Assert.True(creates.Count >= 1, $"Expected at least 1 CreateAsyncContext, got {creates.Count}");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_EventSequenceOrder()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_SingleYield());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            AssertCompleteContextChain(stream,
                AsyncEventID.CreateAsyncContext,
                AsyncEventID.ResumeAsyncContext,
                AsyncEventID.CompleteAsyncContext);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_SuspendResumeCompleteEvents()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_MultiYield());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            ulong taskId = AssertCompleteContextChain(stream,
                AsyncEventID.CreateAsyncContext,
                AsyncEventID.ResumeAsyncContext,
                AsyncEventID.SuspendAsyncContext,
                AsyncEventID.ResumeAsyncContext,
                AsyncEventID.SuspendAsyncContext,
                AsyncEventID.ResumeAsyncContext,
                AsyncEventID.CompleteAsyncContext);

            var taskEvents = stream.ForTask(taskId);
            foreach (var evt in taskEvents)
            {
                if (evt.EventId is AsyncEventID.ResumeAsyncContext or AsyncEventID.CreateAsyncContext)
                    Assert.Equal(taskId, evt.TaskId);
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_ResumeCompleteMethodEvents()
        {
            var events = CollectEvents(MethodKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_SingleYield());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var ids = stream.EventIds;

            Assert.Contains(AsyncEventID.ResumeAsyncMethod, ids);
            Assert.Contains(AsyncEventID.CompleteAsyncMethod, ids);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_ExceptionHandled());
            });

            DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            AssertCompleteContextChain(stream,
                AsyncEventID.CreateAsyncContext,
                AsyncEventID.ResumeAsyncContext,
                AsyncEventID.UnwindAsyncException,
                AsyncEventID.CompleteAsyncContext);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenario(() => TaskAsync_UnhandledExceptionOuter());
                }
                catch (InvalidOperationException)
                {
                }
                SendFlushCommand();
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            AssertCompleteContextChain(stream,
                AsyncEventID.CreateAsyncContext,
                AsyncEventID.ResumeAsyncContext,
                AsyncEventID.UnwindAsyncException,
                AsyncEventID.UnwindAsyncException,
                AsyncEventID.CompleteAsyncContext);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_MethodEventCountMatchesChainDepth()
        {
            var events = CollectEvents(MethodKeywords | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_DeepChain());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // DeepChain → Level1 → Level2 → Level3 = 4 methods
            // Level3 uses Task.Delay to ensure full chain is built before resume.
            // On resume: Level3, Level2, Level1, DeepChain each resume and complete = 4 pairs.
            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncMethod or AsyncEventID.CompleteAsyncMethod)
                .Select(e => e.EventId)
                .ToList();

            int resumeCount = methodEvents.Count(id => id == AsyncEventID.ResumeAsyncMethod);
            int completeCount = methodEvents.Count(id => id == AsyncEventID.CompleteAsyncMethod);

            Assert.True(resumeCount >= 4, $"Expected at least 4 ResumeAsyncMethod events, got {resumeCount}");
            Assert.True(completeCount >= 4, $"Expected at least 4 CompleteAsyncMethod events, got {completeCount}");

            // Verify interleaved ordering: each Resume is followed by its matching Complete
            // Check the last 8 events (4 Resume/Complete pairs from the inner chain)
            var tail = methodEvents.Skip(methodEvents.Count - 8).ToList();
            for (int i = 0; i < tail.Count; i += 2)
            {
                Assert.Equal(AsyncEventID.ResumeAsyncMethod, tail[i]);
                Assert.Equal(AsyncEventID.CompleteAsyncMethod, tail[i + 1]);
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_HandledException_MethodEventsWithUnwind()
        {
            var events = CollectEvents(MethodKeywords | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_ExceptionHandled());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // ExceptionHandled → InnerThrows (2 methods)
            // InnerThrows resumes, throws → Unwind
            // ExceptionHandled resumes (catches), completes
            // Expected method events: Resume, Unwind, Resume, Complete
            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncMethod
                    or AsyncEventID.CompleteAsyncMethod
                    or AsyncEventID.UnwindAsyncException)
                .Select(e => e.EventId)
                .ToList();

            var tail = methodEvents.Skip(methodEvents.Count - 4).ToList();
            Assert.Equal(4, tail.Count);
            Assert.Equal(AsyncEventID.ResumeAsyncMethod, tail[0]);
            Assert.Equal(AsyncEventID.UnwindAsyncException, tail[1]);
            Assert.Equal(AsyncEventID.ResumeAsyncMethod, tail[2]);
            Assert.Equal(AsyncEventID.CompleteAsyncMethod, tail[3]);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_UnhandledException_MethodEventsWithUnwind()
        {
            var events = CollectEvents(MethodKeywords | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenario(() => TaskAsync_UnhandledExceptionOuter());
                }
                catch (InvalidOperationException)
                {
                }
                SendFlushCommand();
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // UnhandledExceptionOuter → UnhandledExceptionInner (2 methods, neither catches)
            // Inner resumes, throws → Unwind
            // Outer resumes (continuation), propagates → Unwind
            // No CompleteAsyncMethod for either — both unwind
            // Expected method events: Resume, Unwind, Resume, Unwind
            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncMethod
                    or AsyncEventID.CompleteAsyncMethod
                    or AsyncEventID.UnwindAsyncException)
                .Select(e => e.EventId)
                .ToList();

            var tail = methodEvents.Skip(methodEvents.Count - 4).ToList();
            Assert.Equal(4, tail.Count);
            Assert.Equal(AsyncEventID.ResumeAsyncMethod, tail[0]);
            Assert.Equal(AsyncEventID.UnwindAsyncException, tail[1]);
            Assert.Equal(AsyncEventID.ResumeAsyncMethod, tail[2]);
            Assert.Equal(AsyncEventID.UnwindAsyncException, tail[3]);
        }
    }
}
