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

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_EventSequenceOrderMarker()
        {
            // Use Task.Delay (not Task.Yield) so the dispatcher has predictable scheduling latency.
            // The marker is the leaf await, so there is no parent-registration race to worry about.
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_EventSequenceOrder()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_EventSequenceOrderMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_EventSequenceOrderMarker));
            Assert.True(markerCallstacks.Count > 0, $"Expected at least one merged resume callstack with {nameof(TaskAsync_EventSequenceOrderMarker)}");

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_SuspendResumeCompleteEventsMarker()
        {
            // Three sequential Delays produce three Suspend/Resume cycles on the same context
            // (Create reuses the active dispatcher's context id and emits Suspend on each subsequent yield).
            // Using Task.Delay (not Task.Yield) avoids the dispatcher-vs-registration race; the marker
            // is the inner box, so it is reliably present in the Resume callstack at walk time.
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_SuspendResumeCompleteEvents()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_SuspendResumeCompleteEventsMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_SuspendResumeCompleteEventsMarker));
            Assert.True(markerCallstacks.Count > 0, $"Expected at least one callstack with {nameof(TaskAsync_SuspendResumeCompleteEventsMarker)}");

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx1 = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx1 > createIdx, "Expected first ResumeAsyncContext after Create");

            int suspendIdx1 = ids.IndexOf(AsyncEventID.SuspendAsyncContext, resumeIdx1 + 1);
            Assert.True(suspendIdx1 > resumeIdx1, "Expected first SuspendAsyncContext after first Resume");

            int resumeIdx2 = ids.IndexOf(AsyncEventID.ResumeAsyncContext, suspendIdx1 + 1);
            Assert.True(resumeIdx2 > suspendIdx1, "Expected second ResumeAsyncContext after first Suspend");

            int suspendIdx2 = ids.IndexOf(AsyncEventID.SuspendAsyncContext, resumeIdx2 + 1);
            Assert.True(suspendIdx2 > resumeIdx2, "Expected second SuspendAsyncContext after second Resume");

            int resumeIdx3 = ids.IndexOf(AsyncEventID.ResumeAsyncContext, suspendIdx2 + 1);
            Assert.True(resumeIdx3 > suspendIdx2, "Expected third ResumeAsyncContext after second Suspend");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx3 + 1);
            Assert.True(completeIdx > resumeIdx3, "Expected CompleteAsyncContext after third Resume");
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

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_HandledException_EmitsUnwindAndCompleteMarker()
        {
            await TaskAsync_ExceptionHandled();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_HandledException_EmitsUnwindAndCompleteMarker());
            });

            //DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_HandledException_EmitsUnwindAndCompleteMarker));
            Assert.True(markerCallstacks.Count > 0, $"Expected at least one callstack with {nameof(TaskAsync_HandledException_EmitsUnwindAndCompleteMarker)}");

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int unwindIdx = ids.IndexOf(AsyncEventID.UnwindAsyncException, resumeIdx + 1);
            Assert.True(unwindIdx > resumeIdx, "Expected UnwindAsyncException after Resume");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, unwindIdx + 1);
            Assert.True(completeIdx > unwindIdx, "Expected CompleteAsyncContext after Unwind");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker()
        {
            await TaskAsync_UnhandledExceptionOuter();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => TaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker));
            Assert.True(markerCallstacks.Count > 0, $"Expected at least one callstack with {nameof(TaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker)}");

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int unwindIdx1 = ids.IndexOf(AsyncEventID.UnwindAsyncException, resumeIdx + 1);
            Assert.True(unwindIdx1 > resumeIdx, "Expected first UnwindAsyncException after Resume");

            int unwindIdx2 = ids.IndexOf(AsyncEventID.UnwindAsyncException, unwindIdx1 + 1);
            Assert.True(unwindIdx2 > unwindIdx1, "Expected second UnwindAsyncException after first Unwind");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, unwindIdx2 + 1);
            Assert.True(completeIdx > unwindIdx2, "Expected CompleteAsyncContext after second Unwind");
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
                    RunScenarioAndFlush(() => TaskAsync_UnhandledExceptionOuter());
                }
                catch (InvalidOperationException)
                {
                }
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

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_ResumeAsyncCallstackEmitted()
        {
            //System.Diagnostics.Debugger.Launch();

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_DeepChain());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var callstacks = stream.All
                .Where(e => e.EventId == AsyncEventID.ResumeAsyncCallstack)
                .ToList();

            Assert.NotEmpty(callstacks);
            Assert.All(callstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in resume callstack");
                Assert.True(cs.Frames[0].MethodId != 0, "Expected non-zero methodId in first frame");
            });
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_CallstackDepthMarker()
        {
            await TaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CallstackDepthMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_CallstackDepthMarker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with TaskAsync_CallstackDepthMarker");

            // TaskAsync_CallstackDepthMarker → Level1 → Level2 → Level3: deepest callstack should have exactly 4 frames
            Assert.Equal(4, markerCallstacks[0].FrameCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_DistinctMethodIdsMarker()
        {
            await TaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackFramesHaveDistinctMethodIds()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_DistinctMethodIdsMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_DistinctMethodIdsMarker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with TaskAsync_DistinctMethodIdsMarker");

            // Frames in the same callstack should have distinct methodIds (different async methods)
            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            Assert.Equal(methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_StateRoot()
        {
            await Task.Yield();
            await TaskAsync_StateMiddle();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_StateMiddle()
        {
            await Task.Yield();
            await Task.Yield();
            await TaskAsync_StateLeaf();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_StateLeaf()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_CallstackStatesMarker()
        {
            await TaskAsync_StateRoot();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackFramesHaveDistinctStates()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CallstackStatesMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_CallstackStatesMarker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with marker");

            // The deepest callstack (on the final Delay resume) should have 4 frames:
            // Leaf (state=3), Middle (state=2), Root (state=1), Marker (state=0)
            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            Assert.Equal(4, deepest.FrameCount);

            // Each frame should have a different state reflecting its suspend point
            var states = deepest.Frames.Select(f => f.State).ToList();
            Assert.Equal(0, states[0]); // Leaf: suspended at 1st await (state=0)
            Assert.Equal(2, states[1]); // Middle: suspended at 3rd await (state=2)
            Assert.Equal(1, states[2]); // Root: suspended at 2nd await (state=1)
            Assert.Equal(0, states[3]); // Marker: suspended at 1st await (state=0)
        }

        // --- Yield at each level scenario ---
        // Each frame yields after calling its child, causing separate resume events
        // with progressively shrinking callstacks as outer frames complete.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_YieldEachLevel_Marker()
        {
            await TaskAsync_YieldEachLevel_Level1();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_YieldEachLevel_Level1()
        {
            await TaskAsync_YieldEachLevel_Level2();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_YieldEachLevel_Level2()
        {
            await TaskAsync_YieldEachLevel_Level3();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_YieldEachLevel_Level3()
        {
            await Task.Delay(100);
            await Task.Yield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_YieldAtEachLevel_CallstackShrinks()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_YieldEachLevel_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_YieldEachLevel_Marker));

            // After Task.Delay resumes: full chain (Level3, Level2, Level1, Marker) = 4 frames
            // After Level3's yield resumes: Level3 completes, chain is (Level2, Level1, Marker) = 3 frames
            // After Level2's yield resumes: Level2 completes, chain is (Level1, Marker) = 2 frames
            Assert.Contains(markerCallstacks, cs => cs.FrameCount == 4);
            Assert.Contains(markerCallstacks, cs => cs.FrameCount == 3);
            Assert.Contains(markerCallstacks, cs => cs.FrameCount == 2);
        }

        // --- Append callstack race scenario ---
        // Forces the chain-growth race where the parent registers as a continuation
        // AFTER the child's dispatcher has already walked the callstack but BEFORE
        // the dispatcher hits its next suspend/complete point. This is the exact
        // window the AppendAsyncCallstack mechanism is designed to fill in.
        //
        // Uses a SemaphoreSlim to deterministically order events independent of TP
        // scheduling latency (a pure Thread.Sleep approach is unreliable because the
        // TP dispatch latency for D1 can exceed the parent's sleep window).
        //
        // Order of events:
        //   1. Parent calls Child; Child suspends at first Yield; D1 is created and queued.
        //   2. Parent calls s_appendRace_proceed.Wait() — blocks.
        //   3. D1 picked up by TP. D1.Resume walks chain → 1 frame (Child), because
        //      Parent hasn't done `await t` yet (it's still in Wait()).
        //   4. D1 calls Child.MoveNext; Child resumes past the await and calls Release().
        //   5. Parent unblocks, does `await t` — registers Parent on Child.Task.
        //   6. Meanwhile Child does Thread.Sleep(200) holding D1 alive.
        //   7. Child hits second Yield → SuspendAsyncContext on D1 → Append check sees
        //      Parent now registered → emits AppendAsyncCallstack with Parent's frame.

        private static SemaphoreSlim s_appendRace_proceed;

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_AppendRace_Child()
        {
            await Task.Yield();
            // Inside D1.MoveNext now; Resume callstack walk already happened.
            s_appendRace_proceed.Release();
            Thread.Sleep(200);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_AppendRace_Parent()
        {
            Task t = TaskAsync_AppendRace_Child();
            s_appendRace_proceed.Wait();
            await t;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_AppendCallstack_FiresOnLateParentRegistration()
        {
            // System.Diagnostics.Debugger.Launch();
            s_appendRace_proceed = new SemaphoreSlim(0, 1);

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_AppendRace_Parent());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The initial Resume on the TP thread should walk only Child (race: Parent not registered yet).
            var childOnlyResumes = stream
                .OfType(AsyncEventID.ResumeAsyncCallstack)
                .Where(e => e.FrameCount == 1 && e.HasMarkerFrame(nameof(TaskAsync_AppendRace_Child)))
                .ToList();
            Assert.NotEmpty(childOnlyResumes);

            // After Parent registers and Child hits its next suspend/complete hook,
            // an AppendAsyncCallstack should fire with the Parent frame.
            var appendsWithParent = stream
                .OfType(AsyncEventID.AppendAsyncCallstack)
                .Where(e => e.HasMarkerFrame(nameof(TaskAsync_AppendRace_Parent)))
                .ToList();
            Assert.NotEmpty(appendsWithParent);
        }

        // --- Negative: Append should NOT fire when the chain is already complete at Resume time ---
        // The deep-chain marker awaits Level1→Level2→Level3, where Level3 awaits Task.Delay(100).
        // The 100ms delay gives all parent continuations ample time to register before the
        // dispatcher walks the chain. The walker should terminate at a non-box (Task.Run wrapper)
        // and set LastContinuation = null, so subsequent ResumeAsyncMethod/Suspend/Complete hooks
        // for the inline cascade short-circuit and emit no Append events.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_CompleteChain_NoAppendMarker()
        {
            await TaskAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CompleteChain_DoesNotEmitAppendEvents()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CompleteChain_NoAppendMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Sanity: the marker frame must appear in the initial Resume callstack (full chain captured).
            var markerCallstacks = stream
                .OfType(AsyncEventID.ResumeAsyncCallstack)
                .Where(e => e.HasMarkerFrame(nameof(TaskAsync_CompleteChain_NoAppendMarker)))
                .ToList();
            Assert.True(markerCallstacks.Count > 0,
                $"Expected initial Resume callstack to contain {nameof(TaskAsync_CompleteChain_NoAppendMarker)} (full chain at walk time)");

            // No Append events should fire — the chain was complete at Resume time.
            var appendEvents = stream
                .OfType(AsyncEventID.AppendAsyncCallstack)
                .ToList();
            Assert.True(appendEvents.Count == 0,
                $"Expected zero AppendAsyncCallstack events for a complete-chain scenario, got {appendEvents.Count}");
        }

        // --- Custom SynchronizationContext scenario ---
        // Validates that when a non-default SynchronizationContext is active during an await,
        // the dispatcher wrapping path is taken (TaskAwaiter.UnsafeOnCompletedInternal wraps the box)
        // and the continuation flows through the custom context's Post back into the dispatcher's
        // MoveNext. Standard Resume/Suspend/Complete events should fire normally; the marker frame
        // must be visible in the Resume callstack.

        private sealed class InlinePostSynchronizationContext : SynchronizationContext
        {
            private int _postCount;
            public int PostCount => _postCount;

            public override void Post(SendOrPostCallback d, object? state)
            {
                Interlocked.Increment(ref _postCount);
                d(state);
            }
        }

        private static InlinePostSynchronizationContext? s_taskAsyncSyncContextCtx;

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_SyncContextMarker()
        {
            // Install a non-default SynchronizationContext on this thread so the await captures it.
            // The await's continuation will be routed via SynchronizationContextAwaitTaskContinuation,
            // which wraps the box in an AsyncTaskDispatcher and posts back to the context.
            //
            // Note: the await may resume on a different thread (the SyncContext's Post may run on
            // the timer thread or another worker). Only restore the previous context if we resumed
            // on the same thread, to avoid polluting an unrelated thread's SynchronizationContext.
            int callerThreadId = Environment.CurrentManagedThreadId;
            SynchronizationContext? prev = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(s_taskAsyncSyncContextCtx);
            try
            {
                await Task.Delay(100);
            }
            finally
            {
                if (Environment.CurrentManagedThreadId == callerThreadId)
                {
                    SynchronizationContext.SetSynchronizationContext(prev);
                }
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CustomSyncContext_EmitsContextEventsAndCallstack()
        {
            s_taskAsyncSyncContextCtx = new InlinePostSynchronizationContext();

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_SyncContextMarker());
            });

            // DumpAllEvents(events);

            // The custom SyncContext should have received at least one Post for the await continuation.
            Assert.True(s_taskAsyncSyncContextCtx.PostCount > 0,
                $"Expected custom SynchronizationContext to receive at least one Post, got {s_taskAsyncSyncContextCtx.PostCount}");

            var stream = ParseAllEvents(events);

            // The marker frame should appear in the Resume callstack (or via Append if the chain raced).
            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_SyncContextMarker));
            Assert.True(markerCallstacks.Count > 0,
                $"Expected merged Resume callstack containing {nameof(TaskAsync_SyncContextMarker)}");

            // Verify the standard Create → Resume → Complete sequence fired for our context.
            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext for the custom SyncContext scenario");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        // --- Custom TaskScheduler scenario ---
        // Validates that when a non-default TaskScheduler is active (the marker runs on a custom
        // scheduler via Task.Factory.StartNew(..., scheduler)), the dispatcher wrapping path is taken
        // and the continuation flows through the custom scheduler's QueueTask back into the dispatcher's
        // MoveNext via TaskSchedulerAwaitTaskContinuation. Standard Resume/Suspend/Complete events
        // should fire normally.

        private sealed class InlineRunTaskScheduler : TaskScheduler
        {
            private int _queuedCount;
            public int QueuedCount => _queuedCount;

            protected override void QueueTask(Task task)
            {
                Interlocked.Increment(ref _queuedCount);
                TryExecuteTask(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;

            protected override IEnumerable<Task>? GetScheduledTasks() => null;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_TaskSchedulerMarker()
        {
            // We rely on the caller having scheduled this method on a custom TaskScheduler via
            // Task.Factory.StartNew, so TaskScheduler.InternalCurrent is the custom scheduler at
            // the moment of await. The await's continuation gets routed through that scheduler.
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack()
        {
            var scheduler = new InlineRunTaskScheduler();

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                // Schedule the marker on our custom scheduler so TaskScheduler.InternalCurrent
                // is the custom scheduler when its first await is registered. Unwrap+GetResult
                // blocks until the async chain completes, then RunScenarioAndFlush's pattern is
                // inlined here (we can't reuse RunScenarioAndFlush because it uses Task.Run).
                try
                {
                    Task.Factory.StartNew(
                        () => TaskAsync_TaskSchedulerMarker(),
                        CancellationToken.None,
                        TaskCreationOptions.None,
                        scheduler).Unwrap().GetAwaiter().GetResult();
                }
                finally
                {
                    Thread.Sleep(50);
                    SendFlushCommand();
                }
            });

            // DumpAllEvents(events);

            // The custom scheduler must have received at least one QueueTask call (for the outer
            // task). The continuation may or may not also be queued depending on whether the runtime
            // inlines it on the timer thread; what matters for this test is that the dispatcher's
            // events fired for the async context.
            Assert.True(scheduler.QueuedCount >= 1,
                $"Expected custom TaskScheduler to receive at least one QueueTask call, got {scheduler.QueuedCount}");

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_TaskSchedulerMarker));
            Assert.True(markerCallstacks.Count > 0,
                $"Expected merged Resume callstack containing {nameof(TaskAsync_TaskSchedulerMarker)}");

            // Verify standard Create → Resume → Complete sequence for our context.
            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext for the custom TaskScheduler scenario");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        // --- ValueTask scenario methods ---

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_Level1()
        {
            await ValueTaskAsync_Level2();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_Level2()
        {
            await ValueTaskAsync_Level3();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_Level3()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_Marker()
        {
            await ValueTaskAsync_Level1();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_EventSequenceOrderMarker()
        {
            await ValueTaskAsync_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_EventSequenceOrder()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_EventSequenceOrderMarker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(ValueTaskAsync_EventSequenceOrderMarker));
            Assert.True(markerCallstacks.Count > 0, $"Expected at least one callstack with {nameof(ValueTaskAsync_EventSequenceOrderMarker)}");

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_MethodEventsEmitted()
        {
            var events = CollectEvents(MethodKeywords | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // ValueTask chain of 4 methods: Marker → Level1 → Level2 → Level3
            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncMethod or AsyncEventID.CompleteAsyncMethod)
                .Select(e => e.EventId)
                .ToList();

            int resumeCount = methodEvents.Count(id => id == AsyncEventID.ResumeAsyncMethod);
            int completeCount = methodEvents.Count(id => id == AsyncEventID.CompleteAsyncMethod);

            Assert.True(resumeCount >= 4, $"Expected at least 4 ResumeAsyncMethod events for ValueTask chain, got {resumeCount}");
            Assert.True(completeCount >= 4, $"Expected at least 4 CompleteAsyncMethod events for ValueTask chain, got {completeCount}");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(ValueTaskAsync_Marker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with ValueTaskAsync_Marker");

            // ValueTaskAsync_Marker → Level1 → Level2 → Level3: deepest should have 4 frames
            Assert.Equal(4, markerCallstacks[0].FrameCount);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_CallstackFramesHaveDistinctMethodIds()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(ValueTaskAsync_Marker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with ValueTaskAsync_Marker");

            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            Assert.Equal(methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_InnerThrows()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("valuetask inner throw");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_ExceptionHandled()
        {
            try
            {
                await ValueTaskAsync_InnerThrows();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_HandledException_EmitsUnwindAndCompleteMarker()
        {
            await ValueTaskAsync_ExceptionHandled();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_HandledException_EmitsUnwindAndCompleteMarker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(ValueTaskAsync_HandledException_EmitsUnwindAndCompleteMarker));
            Assert.True(markerCallstacks.Count > 0, $"Expected at least one callstack with {nameof(ValueTaskAsync_HandledException_EmitsUnwindAndCompleteMarker)}");

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int unwindIdx = ids.IndexOf(AsyncEventID.UnwindAsyncException, resumeIdx + 1);
            Assert.True(unwindIdx > resumeIdx, "Expected UnwindAsyncException after Resume");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, unwindIdx + 1);
            Assert.True(completeIdx > unwindIdx, "Expected CompleteAsyncContext after Unwind");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_UnhandledOuter()
        {
            await ValueTaskAsync_UnhandledInner();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_UnhandledInner()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("valuetask unhandled inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async ValueTask ValueTaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker()
        {
            await ValueTaskAsync_UnhandledOuter();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => ValueTaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker().AsTask());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(ValueTaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker));
            Assert.True(markerCallstacks.Count > 0, $"Expected at least one callstack with {nameof(ValueTaskAsync_UnhandledException_EmitsUnwindAndCompleteMarker)}");

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int unwindIdx1 = ids.IndexOf(AsyncEventID.UnwindAsyncException, resumeIdx + 1);
            Assert.True(unwindIdx1 > resumeIdx, "Expected first UnwindAsyncException after Resume");

            int unwindIdx2 = ids.IndexOf(AsyncEventID.UnwindAsyncException, unwindIdx1 + 1);
            Assert.True(unwindIdx2 > unwindIdx1, "Expected second UnwindAsyncException after first Unwind");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, unwindIdx2 + 1);
            Assert.True(completeIdx > unwindIdx2, "Expected CompleteAsyncContext after second Unwind");
        }

        // --- Negative: no events when profiler is disabled ---
        // Validates that the InstrumentCheckPoint + AsyncProfiler flag short-circuit kicks in
        // before the listener is attached, so async work done with no listener leaves no
        // context-level events behind. After attachment, only background metadata events should
        // be present (no Create/Resume/Suspend/Complete from prior or current work).

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_NoEventsWhenDisabledScenario()
        {
            await Task.Delay(50);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_NoEventsWhenDisabled()
        {
            // Run async work WITHOUT a listener attached. No keywords enabled → no events emitted.
            for (int i = 0; i < 50; i++)
            {
                RunScenario(() => TaskAsync_NoEventsWhenDisabledScenario());
            }

            // Now attach a listener but don't perform any V1 async work — verify no stale events
            // from the previous work leaked through.
            var events = CollectEvents(CoreKeywords, () => { /* no-op */ });

            var ids = ParseAllEvents(events).EventIds;
            int contextEvents = ids.Count(id =>
                id == AsyncEventID.CreateAsyncContext ||
                id == AsyncEventID.ResumeAsyncContext ||
                id == AsyncEventID.SuspendAsyncContext ||
                id == AsyncEventID.CompleteAsyncContext);

            Assert.Equal(0, contextEvents);
        }

        // --- Keyword gatekeeping ---
        // Validates that each individual keyword only enables its corresponding event type.
        // Auto-emitted infrastructure events (ResetAsyncThreadContext, AsyncProfilerMetadata)
        // are always allowed. ResumeAsyncCallstackKeyword controls both ResumeAsyncCallstack
        // AND AppendAsyncCallstack (V1 emits Append events under the same keyword as Resume).

        public static IEnumerable<object[]> TaskAsyncKeywordGatekeepingData()
        {
            yield return new object[] { (long)CreateAsyncContextKeyword,    new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CreateAsyncContext } };
            yield return new object[] { (long)ResumeAsyncContextKeyword,    new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeAsyncContext } };
            yield return new object[] { (long)SuspendAsyncContextKeyword,   new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.SuspendAsyncContext } };
            yield return new object[] { (long)CompleteAsyncContextKeyword,  new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CompleteAsyncContext } };
            yield return new object[] { (long)UnwindAsyncExceptionKeyword,  new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.UnwindAsyncException } };
            yield return new object[] { (long)ResumeAsyncCallstackKeyword,  new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeAsyncCallstack, AsyncEventID.AppendAsyncCallstack } };
            yield return new object[] { (long)ResumeAsyncMethodKeyword,     new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeAsyncMethod } };
            yield return new object[] { (long)CompleteAsyncMethodKeyword,   new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CompleteAsyncMethod } };
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_KeywordGatekeepingMarker()
        {
            // Exercise multiple event types: exception unwind, multiple suspends, method invocations.
            try
            {
                await TaskAsync_InnerThrows();
            }
            catch (InvalidOperationException) { }
            await Task.Delay(50);
        }

        [ConditionalTheory(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        [MemberData(nameof(TaskAsyncKeywordGatekeepingData))]
        public void TaskAsync_KeywordGatekeeping(long keywordValue, AsyncEventID[] allowedEventIds)
        {
            EventKeywords kw = (EventKeywords)keywordValue;
            var allowed = new HashSet<AsyncEventID>(allowedEventIds);

            var events = CollectEvents(kw, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_KeywordGatekeepingMarker());
            });

            var stream = ParseAllEvents(events);
            var unexpected = stream.EventIds.Where(id => !allowed.Contains(id)).ToList();

            Assert.True(unexpected.Count == 0,
                $"Keyword 0x{(long)kw:X}: unexpected event IDs [{string.Join(", ", unexpected)}], allowed [{string.Join(", ", allowed)}]");
        }

        // --- Fork/join (WhenAll) test ---
        // Validates V1 dispatcher behavior under a fork-join pattern: a single outer task awaits
        // multiple parallel branches via Task.WhenAll. Each branch is its own async chain that
        // completes on a (potentially) different ThreadPool thread. The outer resumes only after
        // all branches have completed. This exercises:
        //   1. Multi-branch chain tracking — each branch produces its own Create/Resume/Complete.
        //   2. Concurrent Append safety — branches may complete on different threads simultaneously.
        //   3. Outer resume after fan-in — the marker's Resume callstack reconstructs correctly
        //      after WhenAll's join releases the outer continuation.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAllBranchA() => await Task.Delay(100);

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAllBranchB() => await Task.Delay(120);

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAllBranchC() => await Task.Delay(140);

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAllMarker()
        {
            await Task.WhenAll(
                TaskAsync_WhenAllBranchA(),
                TaskAsync_WhenAllBranchB(),
                TaskAsync_WhenAllBranchC());
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_WhenAll_TracksAllBranchesAndJoin()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_WhenAllMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The outer marker must resume after WhenAll's join releases it. Its callstack
            // should contain the marker frame (proves the outer dispatcher was tracked and
            // the resume callstack reconstruction works through the WhenAll join point).
            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAllMarker));
            Assert.True(markerCallstacks.Count > 0,
                $"Expected at least one Resume callstack containing {nameof(TaskAsync_WhenAllMarker)} after WhenAll join");

            // Each branch is its own async chain; its inner await of Task.Delay produces a
            // Resume callstack containing the branch frame.
            var branchACallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAllBranchA));
            var branchBCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAllBranchB));
            var branchCCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAllBranchC));
            Assert.True(branchACallstacks.Count > 0, $"Expected Resume callstack for {nameof(TaskAsync_WhenAllBranchA)}");
            Assert.True(branchBCallstacks.Count > 0, $"Expected Resume callstack for {nameof(TaskAsync_WhenAllBranchB)}");
            Assert.True(branchCCallstacks.Count > 0, $"Expected Resume callstack for {nameof(TaskAsync_WhenAllBranchC)}");

            // Every Create must be balanced by a Complete — fork-join must not leak dispatcher
            // contexts, and concurrent branch completion must not double-Create.
            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            Assert.Equal(createCount, completeCount);

            // We expect at least 4 Create events: 3 branches + 1 outer marker (the outer's
            // await of WhenAll wraps its box). More is fine — internal infrastructure tasks
            // (WhenAll's join task, Task.Delay continuations) may also wrap depending on
            // SyncContext/Scheduler state. The lower bound proves all our user-visible chains
            // were tracked.
            Assert.True(createCount >= 4,
                $"Expected at least 4 CreateAsyncContext events (3 branches + outer), got {createCount}");

            // The outer marker's chain should fire the standard Create → Resume → Complete
            // sequence on its own TaskId, in that order.
            ulong markerTaskId = markerCallstacks[0].TaskId;
            var markerIds = stream.ForTask(markerTaskId).Select(e => e.EventId).ToList();

            int createIdx = markerIds.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext for the WhenAll outer marker");

            int resumeIdx = markerIds.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create on the outer marker");

            int completeIdx = markerIds.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume on the outer marker");

            // The outer should be created exactly once (no double-wrap regression).
            int createCountForMarker = markerIds.Count(id => id == AsyncEventID.CreateAsyncContext);
            Assert.Equal(1, createCountForMarker);
        }

        // --- Fork/join (WhenAny) test ---
        // Validates V1 dispatcher behavior under WhenAny: outer resumes when the FIRST branch
        // completes; the remaining branches continue running in the background. This is
        // structurally different from WhenAll because:
        //   1. Outer is resumed mid-fan-in, while sibling branches are still alive.
        //   2. The outer dispatcher may be resumed MORE THAN ONCE (here: once after WhenAny,
        //      then again after WhenAll on the slow branches), exercising the resume-of-same-
        //      context cycle without re-Creating the dispatcher.
        //   3. Branch dispatcher lifetimes are independent of the outer's WhenAny return —
        //      we still observe their Create/Resume/Complete events when they finish later.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAnyFast() => await Task.Delay(50);

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAnySlow1() => await Task.Delay(400);

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAnySlow2() => await Task.Delay(600);

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_WhenAnyMarker()
        {
            Task fast = TaskAsync_WhenAnyFast();
            Task slow1 = TaskAsync_WhenAnySlow1();
            Task slow2 = TaskAsync_WhenAnySlow2();

            await Task.WhenAny(fast, slow1, slow2);

            // Ensure the slow branches actually complete before the scenario ends so their
            // Create/Resume/Complete events are observable in the trace. This also forces a
            // second suspend/resume cycle on the outer marker.
            await Task.WhenAll(slow1, slow2);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_WhenAny_TracksAllBranchesWithIndependentLifetimes()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_WhenAnyMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The outer marker is resumed at least once (after WhenAny releases it). Its
            // callstack must contain the marker frame.
            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAnyMarker));
            Assert.True(markerCallstacks.Count > 0,
                $"Expected at least one Resume callstack containing {nameof(TaskAsync_WhenAnyMarker)} after WhenAny");

            // All branches — including the slow ones whose completion the outer is no longer
            // strictly waiting on after WhenAny returned — must produce their own Resume
            // callstacks. This proves their dispatcher lifetimes are tracked independently.
            var fastCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAnyFast));
            var slow1Callstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAnySlow1));
            var slow2Callstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_WhenAnySlow2));
            Assert.True(fastCallstacks.Count > 0, $"Expected Resume callstack for {nameof(TaskAsync_WhenAnyFast)}");
            Assert.True(slow1Callstacks.Count > 0, $"Expected Resume callstack for {nameof(TaskAsync_WhenAnySlow1)}");
            Assert.True(slow2Callstacks.Count > 0, $"Expected Resume callstack for {nameof(TaskAsync_WhenAnySlow2)}");

            // Every Create must be balanced by a Complete — concurrent fan-in with independent
            // sibling lifetimes must not leak or double-count dispatcher contexts.
            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            Assert.Equal(createCount, completeCount);

            // At least 4 Creates: 3 branches + 1 outer marker.
            Assert.True(createCount >= 4,
                $"Expected at least 4 CreateAsyncContext events (3 branches + outer), got {createCount}");

            // The outer marker's chain: exactly one Create, at least two Resumes (one after
            // WhenAny, one after WhenAll on the slow branches), then Complete. This validates
            // resume-of-same-context cycles without re-Creating the dispatcher (no double-wrap).
            ulong markerTaskId = markerCallstacks[0].TaskId;
            var markerEvents = stream.ForTask(markerTaskId);
            var markerIds = markerEvents.Select(e => e.EventId).ToList();

            int createCountForMarker = markerIds.Count(id => id == AsyncEventID.CreateAsyncContext);
            Assert.Equal(1, createCountForMarker);

            int resumeCountForMarker = markerIds.Count(id => id == AsyncEventID.ResumeAsyncContext);
            // Resume count is timing-sensitive: ideally the outer suspends/resumes twice (after
            // WhenAny, then after the subsequent WhenAll on the slow branches). But under load,
            // by the time WhenAny returns and we reach the WhenAll, the slow tasks may have
            // already completed — in which case WhenAll returns synchronously without a second
            // suspend/resume. Either shape is correct runtime behavior; we only require >=1
            // (proves the outer was resumed at all).
            Assert.True(resumeCountForMarker >= 1,
                $"Expected outer marker to be resumed at least once, got {resumeCountForMarker}");

            // Note: We don't assert an exact count of CompleteAsyncContext for the marker. V1's
            // Suspend/Complete events don't carry an explicit TaskId in the wire format; the parser
            // recovers it from the active context. The parser pops on Complete but NOT on Suspend,
            // so when the marker suspends (awaiting WhenAll on the slow branches) and a sibling
            // branch's Complete then fires, the parser misattributes that Complete to the still-
            // active marker context. So the parsed Complete count for the marker may be >1 even
            // though the runtime emitted exactly one Complete for it. The overall Create==Complete
            // balance check above already covers the no-leak guarantee.
            int completeCountForMarker = markerIds.Count(id => id == AsyncEventID.CompleteAsyncContext);
            Assert.True(completeCountForMarker >= 1, "Expected at least one CompleteAsyncContext for the outer marker");

            // First event for the marker is its Create; last is a Complete.
            Assert.Equal(AsyncEventID.CreateAsyncContext, markerIds[0]);
            Assert.Equal(AsyncEventID.CompleteAsyncContext, markerIds[^1]);
        }

        // --- Callstack cap / overflow / stress tests ---
        // Mirrors the V2 versions but uses V1 (Task-based) recursive chains. The walker shares
        // the same buffer/cap infrastructure (byte FrameCount, rent-on-overflow fallback) so these
        // tests guard the same code paths from the V1 entry point.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_RecursiveChain(int depth)
        {
            if (depth <= 1)
            {
                await Task.Delay(100);
                return;
            }
            await TaskAsync_RecursiveChain(depth - 1);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_RecursiveChainMarker(int depth)
        {
            await TaskAsync_RecursiveChain(depth);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackDepthCappedAtMaxFrames()
        {
            // Build a chain deeper than the 255-frame cap (byte FrameCount). The deepest
            // ResumeAsyncCallstack should clamp at byte.MaxValue without crashing.
            const int requestedDepth = 300;

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_RecursiveChainMarker(requestedDepth));
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.OfType(AsyncEventID.ResumeAsyncCallstack).ToList();
            Assert.True(callstacks.Count >= 1, "Expected at least one callstack");

            // Walker caps frames at byte.MaxValue. Requested depth is 300, capped to 255.
            var deepest = callstacks.MaxBy(cs => cs.FrameCount);
            Assert.Equal(byte.MaxValue, deepest!.FrameCount);
            Assert.Equal((int)deepest.FrameCount, deepest.Frames.Count);

            // Every captured frame should resolve to a managed method.
            foreach (var (methodId, _) in deepest.Frames)
            {
                Assert.True(methodId != 0, "Frame has zero MethodId");
                var method = GetMethodNameFromMethodId(deepest.CallstackType, methodId);
                Assert.True(method is not null, $"MethodId 0x{methodId:X} does not resolve to a managed method");
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackStressWithVaryingDepths()
        {
            // Stress test: many V1 async invocations with varying chain depths. Varying sizes
            // place some callstacks at buffer boundaries, naturally exercising the overflow/rewind
            // path in the shared callstack emission code.
            const int iterations = 50;
            int[] depths = new int[iterations];
            var rng = new Random(42);
            for (int i = 0; i < iterations; i++)
                depths[i] = rng.Next(1, 60);

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    for (int i = 0; i < iterations; i++)
                        await TaskAsync_RecursiveChainMarker(depths[i]);
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream
                .OfType(AsyncEventID.ResumeAsyncCallstack)
                .Where(e => e.HasMarkerFrame(nameof(TaskAsync_RecursiveChainMarker)))
                .ToList();

            // Every emitted callstack must have valid frame data.
            foreach (var cs in callstacks)
            {
                Assert.True(cs.FrameCount > 0, "Callstack has 0 frames");
                Assert.Equal((int)cs.FrameCount, cs.Frames.Count);
                for (int f = 0; f < cs.Frames.Count; f++)
                {
                    var (methodId, _) = cs.Frames[f];
                    Assert.True(methodId != 0, $"Frame {f} has zero MethodId");
                    var method = GetMethodNameFromMethodId(cs.CallstackType, methodId);
                    Assert.True(method is not null, $"Frame {f}: MethodId 0x{methodId:X} does not resolve to a managed method");
                }
            }

            // We expect at least one marker callstack per iteration (some may not be the deepest
            // due to mid-chain dispatcher walks). Use >= as the strict count varies with timing.
            Assert.True(callstacks.Count >= iterations,
                $"Expected at least {iterations} callstacks with marker, got {callstacks.Count}");

            // Verify multiple buffer flushes occurred — proves the buffer machinery is exercised.
            int bufferCount = 0;
            ForEachEventBufferPayload(events, _ => bufferCount++);
            Assert.True(bufferCount >= 3, $"Expected at least 3 buffer flushes, got {bufferCount}");
        }

        // --- ConfigureAwait(false) chain test ---
        // Validates that ConfigureAwait(false) at every level of a chain does NOT break the
        // dispatcher cascade or cause the box to be wrapped more than once. ConfigureAwait(false)
        // routes through ConfiguredTaskAwaitable instead of TaskAwaiter, which has its own
        // UnsafeOnCompletedInternal path. A regression here would either drop chain frames or
        // emit multiple Create events per logical async method.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_ConfigureAwaitFalseLeaf()
        {
            await Task.Delay(100).ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_ConfigureAwaitFalseMid()
        {
            await TaskAsync_ConfigureAwaitFalseLeaf().ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_ConfigureAwaitFalseMarker()
        {
            await TaskAsync_ConfigureAwaitFalseMid().ConfigureAwait(false);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_ConfigureAwaitFalse_DoesNotBreakCascadeOrDoubleWrap()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_ConfigureAwaitFalseMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The full chain (Leaf -> Mid -> Marker) must appear in the Resume callstack.
            // For ConfigureAwait(false) box-to-box chains with no SyncContext/Scheduler, the
            // runtime takes the inline-cascade optimization: a single dispatcher walks the
            // entire chain instead of wrapping each level. This is the OPTIMAL trace shape —
            // minimum dispatcher overhead, full chain visibility via the callstack.
            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_ConfigureAwaitFalseMarker));
            Assert.True(markerCallstacks.Count > 0,
                $"Expected at least one Resume callstack containing {nameof(TaskAsync_ConfigureAwaitFalseMarker)} (cascade not broken)");

            // The deepest callstack should include all 3 chain frames — proves the chain walk
            // crossed every ConfigureAwait(false) level without dropping any.
            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            var frameNames = deepest.Frames
                .Select(f => GetMethodNameFromMethodId(deepest.CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();
            Assert.Contains(nameof(TaskAsync_ConfigureAwaitFalseLeaf), frameNames);
            Assert.Contains(nameof(TaskAsync_ConfigureAwaitFalseMid), frameNames);
            Assert.Contains(nameof(TaskAsync_ConfigureAwaitFalseMarker), frameNames);

            // Every Create must be balanced by a Complete — no leaks.
            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            Assert.Equal(createCount, completeCount);

            // Strongest no-double-wrap check: the cascade optimization should produce exactly
            // 1 dispatcher for the entire chain (the leaf's non-box Task.Delay wrapping). A
            // regression that re-introduced per-level wrapping would push this to 3+.
            Assert.Equal(1, createCount);
        }

        // --- Faulted task test ---
        // Validates that an async method that throws (and whose exception is caught upstream)
        // still produces a clean trace: balanced Create/Complete events, an UnwindAsyncException
        // event reflecting the unwound frames, and the marker's Resume callstack present.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_FaultedInner()
        {
            await Task.Delay(50);
            throw new InvalidOperationException("test fault");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_FaultedMarker()
        {
            try
            {
                await TaskAsync_FaultedInner();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_FaultedTask_BalancedEventsAndUnwindEmitted()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | UnwindAsyncExceptionKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_FaultedMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The marker must still resume and complete — exception propagation does not orphan
            // the dispatcher.
            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_FaultedMarker));
            Assert.True(markerCallstacks.Count > 0,
                $"Expected at least one Resume callstack containing {nameof(TaskAsync_FaultedMarker)}");

            // No leak: every dispatcher that was Created must Complete, even on the fault path.
            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            Assert.Equal(createCount, completeCount);

            // The runtime emits UnwindAsyncException when an async method completes with an
            // exception (AsyncTaskMethodBuilder<T>.SetException path).
            int unwindCount = stream.OfType(AsyncEventID.UnwindAsyncException).Count();
            Assert.True(unwindCount > 0,
                "Expected at least one UnwindAsyncException event for the faulted inner task");
        }

        // --- Cancellation test ---
        // Validates that cancellation (OperationCanceledException flowing through the chain)
        // produces a well-formed trace with balanced events and the cancelled chain's marker
        // visible in a Resume callstack.

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_CancelledInner(CancellationToken ct)
        {
            await Task.Delay(5000, ct);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static async Task TaskAsync_CancelledMarker()
        {
            using var cts = new CancellationTokenSource();
            Task inner = TaskAsync_CancelledInner(cts.Token);
            cts.CancelAfter(50);
            try
            {
                await inner;
            }
            catch (OperationCanceledException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_Cancellation_BalancedEvents()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | UnwindAsyncExceptionKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CancelledMarker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The marker must resume and produce a callstack — cancellation propagation does
            // not orphan the dispatcher chain.
            var markerCallstacks = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_CancelledMarker));
            Assert.True(markerCallstacks.Count > 0,
                $"Expected at least one Resume callstack containing {nameof(TaskAsync_CancelledMarker)}");

            // No leak: every Create must be balanced by a Complete on the cancellation path.
            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            Assert.Equal(createCount, completeCount);

            // At least 2 Creates: inner cancelled task + outer marker. Both must Complete.
            Assert.True(createCount >= 2,
                $"Expected at least 2 CreateAsyncContext events (inner + marker), got {createCount}");
        }
    }
}
