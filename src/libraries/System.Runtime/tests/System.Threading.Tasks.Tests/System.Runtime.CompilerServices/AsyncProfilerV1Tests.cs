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
    // Tests for V1 (Task-based AsyncStateMachineBox) async profiler event emission.
    // All scenario methods use [RuntimeAsyncMethodGeneration(false)] to ensure they
    // exercise the legacy Task-based async path even if the default changes in the future.
    // Most tests use sync CollectEvents with RunScenarioAndFlush to isolate the V1 chain
    // on a threadpool thread, ensuring dispatcher finally blocks complete before flush.
    // Requires threading support.
    public partial class AsyncProfilerTests
    {
        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_SingleYield()
        {
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_DeepChain()
        {
            await TaskAsync_Level1();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_Level1()
        {
            await TaskAsync_Level2();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_Level2()
        {
            await TaskAsync_Level3();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_Level3()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_ExceptionHandled()
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
        private static async Task TaskAsync_InnerThrows()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_UnhandledExceptionOuter()
        {
            await TaskAsync_UnhandledExceptionInner();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_UnhandledExceptionInner()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("unhandled inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_RecursiveChain(int depth)
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
        private static async ValueTask ValueTaskAsync_Level1()
        {
            await ValueTaskAsync_Level2();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_Level2()
        {
            await ValueTaskAsync_Level3();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_Level3()
        {
            await Task.Delay(100);
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
        private static async Task TaskAsync_EventSequenceOrder_Marker()
        {
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_EventSequenceOrder()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_EventSequenceOrder_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_EventSequenceOrder_Marker));
            Assert.NotEmpty(markerCallstacks);

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
        private static async Task TaskAsync_SuspendResumeCompleteEvents_Marker()
        {
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_SuspendResumeCompleteEvents()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_SuspendResumeCompleteEvents_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_SuspendResumeCompleteEvents_Marker));
            Assert.NotEmpty(markerCallstacks);

            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext");

            int resumeCount = ids.Count(id => id == AsyncEventID.ResumeAsyncContext);
            Assert.True(resumeCount >= 1, "Expected at least one ResumeAsyncContext");

            int completeCount = ids.Count(id => id == AsyncEventID.CompleteAsyncContext);
            Assert.True(completeCount >= 1, "Expected at least one CompleteAsyncContext");

            // Expected ResumeAsyncContext and CompleteAsyncContext counts to match.
            Assert.Equal(resumeCount, completeCount);

            // Expected no SuspendAsyncContext events.
            Assert.DoesNotContain(AsyncEventID.SuspendAsyncContext, ids);
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
        private static async Task TaskAsync_HandledException_EmitsUnwindAndComplete_Marker()
        {
            await TaskAsync_ExceptionHandled();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_HandledException_EmitsUnwindAndComplete_Marker());
            });

            //DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_HandledException_EmitsUnwindAndComplete_Marker));
            Assert.NotEmpty(markerCallstacks);

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
        private static async Task TaskAsync_UnhandledException_EmitsUnwindAndComplete_Marker()
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
                    RunScenarioAndFlush(() => TaskAsync_UnhandledException_EmitsUnwindAndComplete_Marker());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_UnhandledException_EmitsUnwindAndComplete_Marker));
            Assert.NotEmpty(markerCallstacks);

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


        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_MethodEventCountMatchesChainDepth_Marker()
        {
            await TaskAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_MethodEventCountMatchesChainDepth()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | MethodKeywords | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_MethodEventCountMatchesChainDepth_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Marker -> DeepChain -> Level1 -> Level2 -> Level3
            const int ExpectedChainDepth = 5;

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_MethodEventCountMatchesChainDepth_Marker));
            Assert.NotEmpty(markerCallstacks);

            Assert.Equal(ExpectedChainDepth, markerCallstacks[0].Frames.Count);

            ulong chainTaskId = markerCallstacks[0].TaskId;
            var chainEvents = stream.ForTask(chainTaskId);

            int resumeCount = chainEvents.Count(e => e.EventId == AsyncEventID.ResumeAsyncMethod);
            Assert.Equal(ExpectedChainDepth, resumeCount);

            int completeCount = chainEvents.Count(e => e.EventId == AsyncEventID.CompleteAsyncMethod);
            Assert.Equal(ExpectedChainDepth, completeCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_HandledException_MethodEventsWithUnwind_Marker()
        {
            await TaskAsync_ExceptionHandled();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_HandledException_MethodEventsWithUnwind()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | MethodKeywords | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_HandledException_MethodEventsWithUnwind_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_HandledException_MethodEventsWithUnwind_Marker));

            Assert.NotEmpty(markerCallstacks);

            ulong chainTaskId = markerCallstacks[0].TaskId;
            var chainEvents = stream.ForTask(chainTaskId);

            var sequence = chainEvents
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncMethod
                    or AsyncEventID.CompleteAsyncMethod
                    or AsyncEventID.UnwindAsyncException)
                .Select(e => e.EventId)
                .ToList();

            // Exactly one Unwind expected on the chain's context (InnerThrows throws once).
            Assert.Equal(1, sequence.Count(id => id == AsyncEventID.UnwindAsyncException));

            // Around the Unwind: the throwing method's Resume precedes it, and the catching
            // method's Resume -> Complete pair follows.
            int unwindIdx = sequence.IndexOf(AsyncEventID.UnwindAsyncException);
            Assert.Equal(AsyncEventID.ResumeAsyncMethod, sequence[unwindIdx - 1]);
            Assert.Equal(AsyncEventID.ResumeAsyncMethod, sequence[unwindIdx + 1]);
            Assert.Equal(AsyncEventID.CompleteAsyncMethod, sequence[unwindIdx + 2]);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_UnhandledException_MethodEventsWithUnwind_Marker()
        {
            await TaskAsync_UnhandledExceptionOuter();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_UnhandledException_MethodEventsWithUnwind()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | MethodKeywords | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => TaskAsync_UnhandledException_MethodEventsWithUnwind_Marker());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Marker -> UnhandledExceptionOuter -> UnhandledExceptionInner
            const int ExpectedChainDepth = 3;

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_UnhandledException_MethodEventsWithUnwind_Marker));
            Assert.NotEmpty(markerCallstacks);

            ulong chainTaskId = markerCallstacks[0].TaskId;
            var chainEvents = stream.ForTask(chainTaskId);

            var sequence = chainEvents
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncMethod
                    or AsyncEventID.CompleteAsyncMethod
                    or AsyncEventID.UnwindAsyncException)
                .Select(e => e.EventId)
                .ToList();

            // Every method in the chain unwinds (no catch); no CompleteAsyncMethod expected.
            Assert.Equal(ExpectedChainDepth, sequence.Count(id => id == AsyncEventID.ResumeAsyncMethod));
            Assert.Equal(ExpectedChainDepth, sequence.Count(id => id == AsyncEventID.UnwindAsyncException));
            Assert.Equal(0, sequence.Count(id => id == AsyncEventID.CompleteAsyncMethod));

            // Per-method ordering: each Resume is immediately followed by its Unwind.
            for (int i = 0; i < sequence.Count; i += 2)
            {
                Assert.Equal(AsyncEventID.ResumeAsyncMethod, sequence[i]);
                Assert.Equal(AsyncEventID.UnwindAsyncException, sequence[i + 1]);
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_ResumeAsyncCallstackEmitted_Marker()
        {
            await TaskAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_ResumeAsyncCallstackEmitted()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_ResumeAsyncCallstackEmitted_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_ResumeAsyncCallstackEmitted_Marker));
            Assert.NotEmpty(markerCallstacks);

            Assert.All(markerCallstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in resume callstack");
                Assert.True(cs.Frames[0].MethodId != 0, "Expected non-zero methodId in first frame");
            });
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackDepthMatchesChainDepth_Marker()
        {
            await TaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CallstackDepthMatchesChainDepth_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_CallstackDepthMatchesChainDepth_Marker));
            Assert.NotEmpty(markerCallstacks);

            // TaskAsync_CallstackDepthMarker -> Level1 -> Level2 -> Level3: deepest callstack should have exactly 4 frames
            Assert.Equal(4, markerCallstacks[0].FrameCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackFramesHaveDistinctMethodIds_Marker()
        {
            await TaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackFramesHaveDistinctMethodIds()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CallstackFramesHaveDistinctMethodIds_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_CallstackFramesHaveDistinctMethodIds_Marker));
            Assert.NotEmpty(markerCallstacks);

            // Frames in the same callstack should have distinct methodIds (different async methods)
            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            Assert.Equal(methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackFramesHaveDistinctStates_Root_Marker()
        {
            await Task.Yield();
            await TaskAsync_CallstackFramesHaveDistinctStates_Middle_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackFramesHaveDistinctStates_Middle_Marker()
        {
            await Task.Yield();
            await Task.Yield();
            await TaskAsync_CallstackFramesHaveDistinctStates_Leaf_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackFramesHaveDistinctStates_Leaf_Marker()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackFramesHaveDistinctStates_Marker()
        {
            await TaskAsync_CallstackFramesHaveDistinctStates_Root_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackFramesHaveDistinctStates()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CallstackFramesHaveDistinctStates_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_CallstackFramesHaveDistinctStates_Marker));
            Assert.NotEmpty(markerCallstacks);

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

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_YieldAtEachLevel_CallstackShrinks_Level1_Marker()
        {
            await TaskAsync_YieldAtEachLevel_CallstackShrinks_Level2_Marker();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_YieldAtEachLevel_CallstackShrinks_Level2_Marker()
        {
            await TaskAsync_YieldAtEachLevel_CallstackShrinks_Level3_Marker();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_YieldAtEachLevel_CallstackShrinks_Level3_Marker()
        {
            await Task.Delay(100);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_YieldAtEachLevel_CallstackShrinks_Marker()
        {
            await TaskAsync_YieldAtEachLevel_CallstackShrinks_Level1_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_YieldAtEachLevel_CallstackShrinks()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_YieldAtEachLevel_CallstackShrinks_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_YieldAtEachLevel_CallstackShrinks_Marker));

            // After Task.Delay resumes: full chain (Level3, Level2, Level1, Marker) = 4 frames
            // After Level3's yield resumes: Level3 completes, chain is (Level2, Level1, Marker) = 3 frames
            // After Level2's yield resumes: Level2 completes, chain is (Level1, Marker) = 2 frames
            Assert.Contains(markerCallstacks, cs => cs.FrameCount == 4);
            Assert.Contains(markerCallstacks, cs => cs.FrameCount == 3);
            Assert.Contains(markerCallstacks, cs => cs.FrameCount == 2);
        }

        private static SemaphoreSlim s_appendRace_proceed;

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_AppendCallstack_FiresOnLateParentRegistration_Child_Marker()
        {
            await Task.Yield();
            s_appendRace_proceed.Release();
            Thread.Sleep(200);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker()
        {
            Task t = TaskAsync_AppendCallstack_FiresOnLateParentRegistration_Child_Marker();
            s_appendRace_proceed.Wait();
            await t;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_AppendCallstack_FiresOnLateParentRegistration()
        {
            s_appendRace_proceed = new SemaphoreSlim(0, 1);

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The initial Resume should only include TaskAsync_AppendRace_Child (race: Parent not registered yet).
            var childOnlyResumes = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_AppendCallstack_FiresOnLateParentRegistration_Child_Marker));
            Assert.NotEmpty(childOnlyResumes);

            // After Parent registers and Child hits its next complete hook,
            // an AppendAsyncCallstack should fire with the Parent frame.
            var appendsWithParent = stream.CallstacksWithMarker(AsyncEventID.AppendAsyncCallstack, nameof(TaskAsync_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker));
            Assert.NotEmpty(appendsWithParent);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CompleteChain_DoesNotEmitAppendEvents_Marker()
        {
            await TaskAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CompleteChain_DoesNotEmitAppendEvents()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_CompleteChain_DoesNotEmitAppendEvents_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Sanity: the marker frame must appear in the initial Resume callstack (full chain captured).
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_CompleteChain_DoesNotEmitAppendEvents_Marker));
            Assert.NotEmpty(markerCallstacks);

            // No Append events should fire on this context -- the chain was complete at Resume time.
            ulong chainTaskId = markerCallstacks[0].TaskId;
            var appendEvents = stream.ForTask(chainTaskId)
                .Where(e => e.EventId == AsyncEventID.AppendAsyncCallstack)
                .ToList();
            Assert.Empty(appendEvents);
        }

        private static InlinePostSynchronizationContext? s_taskAsyncSyncContextCtx;

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker()
        {
            // Install a non-default SynchronizationContext on this thread so the await captures it.
            // The await's continuation will be routed via SynchronizationContextAwaitTaskContinuation,
            // which wraps the box in an AsyncTaskDispatcher and posts back to the context.
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
                RunScenarioAndFlush(() => TaskAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker());
            });

            // DumpAllEvents(events);

            // The custom SyncContext should have received at least one Post for the await continuation.
            Assert.True(s_taskAsyncSyncContextCtx.PostCount > 0,
                $"Expected custom SynchronizationContext to receive at least one Post, got {s_taskAsyncSyncContextCtx.PostCount}");

            var stream = ParseAllEvents(events);

            // The marker frame should appear in the Resume callstack.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker));
            Assert.NotEmpty(markerCallstacks);

            // Verify the standard Create -> Resume -> Complete sequence fired for our context.
            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext for the custom SyncContext scenario");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker()
        {
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack()
        {
            var scheduler = new InlineRunTaskScheduler();

            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                try
                {
                    Task.Factory.StartNew(
                        () => TaskAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker(),
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

            // The custom scheduler must have received at least one QueueTask call.
            Assert.True(scheduler.QueuedCount >= 1,
                $"Expected custom TaskScheduler to receive at least one QueueTask call, got {scheduler.QueuedCount}");

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker));
            Assert.NotEmpty(markerCallstacks);

            // Verify standard Create -> Resume -> Complete sequence for our context.
            ulong taskId = markerCallstacks[0].TaskId;
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext for the custom TaskScheduler scenario");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_NoEventsWhenDisabled_Marker()
        {
            await Task.Delay(50);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_NoEventsWhenDisabled()
        {
            for (int i = 0; i < 50; i++)
            {
                RunScenario(() => TaskAsync_NoEventsWhenDisabled_Marker());
            }

            // Now attach a listener but don't perform any V1 async work.
            var events = CollectEvents(CoreKeywords, () => { });

            var ids = ParseAllEvents(events).EventIds;
            int contextEvents = ids.Count(id =>
                id == AsyncEventID.CreateAsyncContext ||
                id == AsyncEventID.ResumeAsyncContext ||
                id == AsyncEventID.SuspendAsyncContext ||
                id == AsyncEventID.CompleteAsyncContext);

            Assert.Equal(0, contextEvents);
        }

        public static IEnumerable<object[]> TaskAsyncKeywordGatekeepingData()
        {
            yield return new object[] { (long)CreateAsyncContextKeyword,    new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CreateAsyncContext } };
            yield return new object[] { (long)ResumeAsyncContextKeyword,    new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeAsyncContext } };
            yield return new object[] { (long)CompleteAsyncContextKeyword,  new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CompleteAsyncContext } };
            yield return new object[] { (long)UnwindAsyncExceptionKeyword,  new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.UnwindAsyncException } };
            yield return new object[] { (long)ResumeAsyncCallstackKeyword,  new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeAsyncCallstack, AsyncEventID.AppendAsyncCallstack } };
            yield return new object[] { (long)ResumeAsyncMethodKeyword,     new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeAsyncMethod } };
            yield return new object[] { (long)CompleteAsyncMethodKeyword,   new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CompleteAsyncMethod } };
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_KeywordGatekeeping_Marker()
        {
            // Exercise multiple event types: exception unwind, multiple completes, method invocations.
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
                RunScenarioAndFlush(() => TaskAsync_NoEventsWhenDisabled_Marker());
            });

            var stream = ParseAllEvents(events);
            var unexpected = stream.EventIds.Where(id => !allowed.Contains(id)).ToList();

            Assert.True(unexpected.Count == 0,
                $"Keyword 0x{(long)kw:X}: unexpected event IDs [{string.Join(", ", unexpected)}], allowed [{string.Join(", ", allowed)}]");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAll_TracksAllBranches_BranchA_Marker()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAll_TracksAllBranches_BranchB_Marker()
        {
            await Task.Delay(120);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAll_TracksAllBranches_BranchC_Marker()
        {
            await Task.Delay(140);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAll_TracksAllBranches_Marker()
        {
            await Task.WhenAll(
                TaskAsync_WhenAll_TracksAllBranches_BranchA_Marker(),
                TaskAsync_WhenAll_TracksAllBranches_BranchB_Marker(),
                TaskAsync_WhenAll_TracksAllBranches_BranchC_Marker());
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_WhenAll_TracksAllBranches()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_WhenAll_TracksAllBranches_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAll_TracksAllBranches_Marker));
            Assert.NotEmpty(markerCallstacks);

            // Each branch is its own async chain; its inner await of Task.Delay produces a Resume callstack containing the branch frame.
            var branchACallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAll_TracksAllBranches_BranchA_Marker));
            var branchBCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAll_TracksAllBranches_BranchB_Marker));
            var branchCCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAll_TracksAllBranches_BranchC_Marker));
            Assert.NotEmpty(branchACallstacks);
            Assert.NotEmpty(branchBCallstacks);
            Assert.NotEmpty(branchCCallstacks);

            // Each tracked chain (3 branches + outer marker) must see exactly one Create and one Complete on its own TaskId.
            AssertExactlyOneCreateAndComplete(stream, branchACallstacks[0].TaskId, nameof(TaskAsync_WhenAll_TracksAllBranches_BranchA_Marker));
            AssertExactlyOneCreateAndComplete(stream, branchBCallstacks[0].TaskId, nameof(TaskAsync_WhenAll_TracksAllBranches_BranchB_Marker));
            AssertExactlyOneCreateAndComplete(stream, branchCCallstacks[0].TaskId, nameof(TaskAsync_WhenAll_TracksAllBranches_BranchC_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].TaskId, nameof(TaskAsync_WhenAll_TracksAllBranches_Marker));

            // The outer marker's chain should fire the standard Create -> Resume -> Complete sequence on its own TaskId, in that order.
            ulong markerTaskId = markerCallstacks[0].TaskId;
            var markerIds = stream.ForTask(markerTaskId).Select(e => e.EventId).ToList();

            int createIdx = markerIds.IndexOf(AsyncEventID.CreateAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext for the WhenAll outer marker");

            int resumeIdx = markerIds.IndexOf(AsyncEventID.ResumeAsyncContext, createIdx + 1);
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after Create on the outer marker");

            int completeIdx = markerIds.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx + 1);
            Assert.True(completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume on the outer marker");

            // The outer should be created exactly once.
            int createCountForMarker = markerIds.Count(id => id == AsyncEventID.CreateAsyncContext);
            Assert.Equal(1, createCountForMarker);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAny_TracksAllBranches_Fast_Marker()
        {
            await Task.Delay(50);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAny_TracksAllBranches_Slow1_Marker()
        {
            await Task.Delay(400);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAny_TracksAllBranches_Slow2_Marker()
        {
            await Task.Delay(600);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WhenAny_TracksAllBranches_Marker()
        {
            Task fast = TaskAsync_WhenAny_TracksAllBranches_Fast_Marker();
            Task slow1 = TaskAsync_WhenAny_TracksAllBranches_Slow1_Marker();
            Task slow2 = TaskAsync_WhenAny_TracksAllBranches_Slow2_Marker();

            await Task.WhenAny(fast, slow1, slow2);
            await Task.WhenAll(slow1, slow2);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_WhenAny_TracksAllBranches()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_WhenAny_TracksAllBranches_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAny_TracksAllBranches_Marker));
            Assert.NotEmpty(markerCallstacks);

            // All branches - including the slow ones whose completion the outer is no longer
            // strictly waiting on after WhenAny returned -- must produce their own Resume
            // callstacks. This proves their dispatcher lifetimes are tracked independently.
            var fastCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAny_TracksAllBranches_Fast_Marker));
            var slow1Callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAny_TracksAllBranches_Slow1_Marker));
            var slow2Callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WhenAny_TracksAllBranches_Slow2_Marker));
            Assert.NotEmpty(fastCallstacks);
            Assert.NotEmpty(slow1Callstacks);
            Assert.NotEmpty(slow2Callstacks);

            // Each tracked chain (3 branches + outer marker) must see exactly one Create and one Complete on its own TaskId.
            AssertExactlyOneCreateAndComplete(stream, fastCallstacks[0].TaskId, nameof(TaskAsync_WhenAny_TracksAllBranches_Fast_Marker));
            AssertExactlyOneCreateAndComplete(stream, slow1Callstacks[0].TaskId, nameof(TaskAsync_WhenAny_TracksAllBranches_Slow1_Marker));
            AssertExactlyOneCreateAndComplete(stream, slow2Callstacks[0].TaskId, nameof(TaskAsync_WhenAny_TracksAllBranches_Slow2_Marker));
            AssertCreateEqualsCompleteForTask(stream, markerCallstacks[0].TaskId, nameof(TaskAsync_WhenAny_TracksAllBranches_Marker));

            // The outer marker's chain: exactly one Create, at least two Resumes (one after
            // WhenAny, one after WhenAll on the slow branches), then Complete.
            ulong markerTaskId = markerCallstacks[0].TaskId;
            var markerEvents = stream.ForTask(markerTaskId);
            var markerIds = markerEvents.Select(e => e.EventId).ToList();

            int resumeCountForMarker = markerIds.Count(id => id == AsyncEventID.ResumeAsyncContext);
            Assert.True(resumeCountForMarker >= 1,
                $"Expected outer marker to be resumed at least once, got {resumeCountForMarker}");

            int completeCountForMarker = markerIds.Count(id => id == AsyncEventID.CompleteAsyncContext);
            Assert.True(completeCountForMarker >= 1, "Expected at least one CompleteAsyncContext for the outer marker");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackDepthCappedAtMaxFrames_Marker(int depth)
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
                RunScenarioAndFlush(() => TaskAsync_CallstackDepthCappedAtMaxFrames_Marker(requestedDepth));
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // With the cap at byte.MaxValue, the initial Resume walks the first 255 frames from
            // the leaf. The marker sits at the top of the chain (deeper than 255), so it appears
            // in a subsequent AppendAsyncCallstack carrying the remaining frames. Use the merged
            // view to validate the chain as a whole.
            var mergedMarker = stream.MergedResumeCallstacksWithMarker(nameof(TaskAsync_CallstackDepthCappedAtMaxFrames_Marker));
            Assert.NotEmpty(mergedMarker);

            var deepest = mergedMarker.MaxBy(cs => cs.Frames.Count);
            Assert.NotNull(deepest);

            // Walker should have captured at least the full requested depth across Resume+Appends.
            Assert.True(deepest.Frames.Count >= requestedDepth,
                $"Expected merged frame count >= {requestedDepth}, got {deepest.Frames.Count}");

            // Each individual event clamps its own FrameCount at byte.MaxValue (wire format limit).
            ulong chainTaskId = deepest.TaskId;
            var perEventCallstacks = stream.ForTask(chainTaskId)
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncCallstack or AsyncEventID.AppendAsyncCallstack)
                .ToList();
            Assert.NotEmpty(perEventCallstacks);
            Assert.All(perEventCallstacks, cs => Assert.True(cs.FrameCount <= byte.MaxValue));

            // At least one event must have hit the cap (otherwise the test isn't exercising it).
            Assert.Contains(perEventCallstacks, cs => cs.FrameCount == byte.MaxValue);

            // Every captured frame should resolve to a managed method.
            foreach (var (methodId, _) in deepest.Frames)
            {
                Assert.True(methodId != 0, "Frame has zero MethodId");
                var method = GetMethodNameFromMethodId(deepest.CallstackType, methodId);
                Assert.True(method is not null, $"MethodId 0x{methodId:X} does not resolve to a managed method");
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_CallstackStressWithVaryingDepths_Marker(int depth)
        {
            await TaskAsync_RecursiveChain(depth);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_CallstackStressWithVaryingDepths()
        {
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
                        await TaskAsync_CallstackStressWithVaryingDepths_Marker(depths[i]);
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_CallstackStressWithVaryingDepths_Marker));
            Assert.NotEmpty(markerCallstacks);

            // Every emitted callstack must have valid frame data.
            foreach (var cs in markerCallstacks)
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

            // We expect at least one marker callstack per iteration.
            Assert.True(markerCallstacks.Count >= iterations,
                $"Expected at least {iterations} callstacks with marker, got {markerCallstacks.Count}");

            // Verify multiple buffer flushes occurred -- proves the buffer machinery is exercised.
            int bufferCount = 0;
            ForEachEventBufferPayload(events, _ => bufferCount++);
            Assert.True(bufferCount >= 3, $"Expected at least 3 buffer flushes, got {bufferCount}");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WaitThenYield_BalancesResumeAndComplete_WaitYield_Marker(Task gate)
        {
            await gate;
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_WaitThenYield_BalancesResumeAndComplete_Marker()
        {
            await Task.Yield();

            var tcs = new TaskCompletionSource();
            Task b1 = TaskAsync_WaitThenYield_BalancesResumeAndComplete_WaitYield_Marker(tcs.Task);
            Task b2 = TaskAsync_WaitThenYield_BalancesResumeAndComplete_WaitYield_Marker(tcs.Task);

            tcs.SetResult();

            await Task.WhenAll(b1, b2);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_WaitThenYield_BalancesResumeAndComplete()
        {
            var events = CollectEvents(CoreKeywords | ResumeAsyncCallstackKeyword, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_WaitThenYield_BalancesResumeAndComplete_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            int resumeCount = stream.OfType(AsyncEventID.ResumeAsyncContext).Count();
            int suspendCount = stream.OfType(AsyncEventID.SuspendAsyncContext).Count();

            // At least one root Create event.
            Assert.True(createCount >= 1,
                $"Expected at least one CreateAsyncContext event, got {createCount}");

            Assert.Equal(createCount, completeCount);
            Assert.Equal(resumeCount, completeCount);

            // Does not emit Suspend events.
            Assert.Equal(0, suspendCount);

            Assert.True(createCount >= 3,
                $"Expected fan-out chain to produce at least 3 CreateAsyncContext events (root + 2 child wraps), got {createCount}");

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_WaitThenYield_BalancesResumeAndComplete_Marker));
            Assert.NotEmpty(markerCallstacks);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_ConfigureAwaitFalse_Leaf_Marker()
        {
            await Task.Delay(100).ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_ConfigureAwaitFalse_Mid_Marker()
        {
            await TaskAsync_ConfigureAwaitFalse_Leaf_Marker().ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_ConfigureAwaitFalse_Marker()
        {
            await TaskAsync_ConfigureAwaitFalse_Mid_Marker().ConfigureAwait(false);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_ConfigureAwaitFalse()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_ConfigureAwaitFalse_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_ConfigureAwaitFalse_Marker));
            Assert.NotEmpty(markerCallstacks);

            var frameNames = markerCallstacks[0].Frames
                .Select(f => GetMethodNameFromMethodId(markerCallstacks[0].CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();

            Assert.Contains(nameof(TaskAsync_ConfigureAwaitFalse_Leaf_Marker), frameNames);
            Assert.Contains(nameof(TaskAsync_ConfigureAwaitFalse_Mid_Marker), frameNames);
            Assert.Contains(nameof(TaskAsync_ConfigureAwaitFalse_Marker), frameNames);

            // ConfigureAwait(false) on a sequential await chain collapses Leaf -> Mid -> Marker into one
            // continuation chain, so exactly one Create / one Complete is expected on the marker's TaskId.
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].TaskId, nameof(TaskAsync_ConfigureAwaitFalse_Marker));
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_FaultedTask_Inner_Marker()
        {
            await Task.Delay(50);
            throw new InvalidOperationException("test fault");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_FaultedTask_Marker()
        {
            try
            {
                await TaskAsync_FaultedTask_Inner_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void TaskAsync_FaultedTask()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | UnwindAsyncExceptionKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_FaultedTask_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_FaultedTask_Marker));
            Assert.NotEmpty(markerCallstacks);

            var innerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_FaultedTask_Inner_Marker));
            Assert.NotEmpty(innerCallstacks);

            // Every dispatcher that was Created must Complete, even on the fault path.
            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            Assert.Equal(createCount, completeCount);

            // Both inner (faulting) and outer marker chains must see exactly one Create and one Complete on their own TaskId.
            AssertExactlyOneCreateAndComplete(stream, innerCallstacks[0].TaskId, nameof(TaskAsync_FaultedTask_Inner_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].TaskId, nameof(TaskAsync_FaultedTask_Marker));

            // The unwind must be attributed to the inner faulting chain.
            ulong innerTaskId = innerCallstacks[0].TaskId;
            int unwindCountForInner = stream.ForTask(innerTaskId).Count(e => e.EventId == AsyncEventID.UnwindAsyncException);
            Assert.True(unwindCountForInner >= 1,
                $"Expected at least one UnwindAsyncException on the faulted inner chain (TaskId {innerTaskId}), got {unwindCountForInner}");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_TaskCancellation_Inner_Marker(CancellationToken ct)
        {
            await Task.Delay(5000, ct);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_TaskCancellation_Marker()
        {
            using var cts = new CancellationTokenSource();
            Task inner = TaskAsync_TaskCancellation_Inner_Marker(cts.Token);
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
        public void TaskAsync_TaskCancellation()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | UnwindAsyncExceptionKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => TaskAsync_TaskCancellation_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_TaskCancellation_Marker));
            Assert.NotEmpty(markerCallstacks);

            var innerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_TaskCancellation_Inner_Marker));
            Assert.NotEmpty(innerCallstacks);

            // Inner cancelled task + outer marker must each see exactly one Create and one Complete on their own TaskId.
            AssertExactlyOneCreateAndComplete(stream, innerCallstacks[0].TaskId, nameof(TaskAsync_TaskCancellation_Inner_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].TaskId, nameof(TaskAsync_TaskCancellation_Marker));
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_EventSequenceOrder_Marker()
        {
            await ValueTaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_EventSequenceOrder()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_EventSequenceOrder_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(ValueTaskAsync_EventSequenceOrder_Marker));
            Assert.NotEmpty(markerCallstacks);

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
        private static async ValueTask ValueTaskAsync_MethodEventsEmitted_Marker()
        {
            await ValueTaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_MethodEventsEmitted()
        {
            var events = CollectEvents(MethodKeywords | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_MethodEventsEmitted_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeAsyncMethod or AsyncEventID.CompleteAsyncMethod)
                .Select(e => e.EventId)
                .ToList();

            int resumeCount = methodEvents.Count(id => id == AsyncEventID.ResumeAsyncMethod);
            int completeCount = methodEvents.Count(id => id == AsyncEventID.CompleteAsyncMethod);

            // Marker -> Level1 -> Level2 -> Level3
            Assert.True(resumeCount >= 4, $"Expected at least 4 ResumeAsyncMethod events for ValueTask chain, got {resumeCount}");
            Assert.True(completeCount >= 4, $"Expected at least 4 CompleteAsyncMethod events for ValueTask chain, got {completeCount}");
        }


        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_CallstackDepthMatchesChainDepth_Marker()
        {
            await ValueTaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_CallstackDepthMatchesChainDepth_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(ValueTaskAsync_CallstackDepthMatchesChainDepth_Marker));
            Assert.NotEmpty(markerCallstacks);

            // Marker -> Level1 -> Level2 -> Level3
            Assert.Equal(4, markerCallstacks[0].FrameCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_CallstackFramesHaveDistinctMethodIds_Marker()
        {
            await ValueTaskAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_CallstackFramesHaveDistinctMethodIds()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_CallstackFramesHaveDistinctMethodIds_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(ValueTaskAsync_CallstackFramesHaveDistinctMethodIds_Marker));
            Assert.NotEmpty(markerCallstacks);

            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            Assert.Equal(methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_HandledException_EmitsUnwindAndComplete_InnerThrows_Marker()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("valuetask inner throw");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_HandledException_EmitsUnwindAndComplete_Handled_Marker()
        {
            try
            {
                await ValueTaskAsync_HandledException_EmitsUnwindAndComplete_InnerThrows_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_HandledException_EmitsUnwindAndComplete_Marker()
        {
            await ValueTaskAsync_HandledException_EmitsUnwindAndComplete_Handled_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => ValueTaskAsync_HandledException_EmitsUnwindAndComplete_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(ValueTaskAsync_HandledException_EmitsUnwindAndComplete_Marker));
            Assert.NotEmpty(markerCallstacks);

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
        private static async ValueTask ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete_UnhandledOuter_Marker()
        {
            await ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete_UnhandledInner_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete_UnhandledInner_Marker()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("valuetask unhandled inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete_Marker()
        {
            await ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete_UnhandledOuter_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeAsyncCallstackKeyword | CoreKeywords | UnwindAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete_Marker().AsTask());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(ValueTaskAsync_UnhandledException_EmitsUnwindAndComplete_Marker));
            Assert.NotEmpty(markerCallstacks);

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

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_SingleThread_ChainEventsAndCallstack_Inner_Marker(Task gate)
        {
            await gate;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_SingleThread_ChainEventsAndCallstack_Mid_Marker(Task gate)
        {
            await TaskAsync_SingleThread_ChainEventsAndCallstack_Inner_Marker(gate);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task TaskAsync_SingleThread_ChainEventsAndCallstack_Marker(Task gate)
        {
            await TaskAsync_SingleThread_ChainEventsAndCallstack_Mid_Marker(gate);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task TaskAsync_SingleThread_ChainEventsAndCallstack()
        {
            var events = await CollectEventsAsync(ResumeAsyncCallstackKeyword | CoreKeywords | MethodKeywords, async () =>
            {
                var tcs = new TaskCompletionSource();
                Task chain = TaskAsync_SingleThread_ChainEventsAndCallstack_Marker(tcs.Task);
                tcs.SetResult();
                await chain;
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The marker frame must appear in a Resume callstack -- proves the chain was walkable.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(TaskAsync_SingleThread_ChainEventsAndCallstack_Marker));
            Assert.NotEmpty(markerCallstacks);

            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            var frameNames = deepest.Frames
                .Select(f => GetMethodNameFromMethodId(deepest.CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();
            Assert.Contains(nameof(TaskAsync_SingleThread_ChainEventsAndCallstack_Inner_Marker), frameNames);
            Assert.Contains(nameof(TaskAsync_SingleThread_ChainEventsAndCallstack_Mid_Marker), frameNames);
            Assert.Contains(nameof(TaskAsync_SingleThread_ChainEventsAndCallstack_Marker), frameNames);

            // Create balanced by Complete on the synchronous cascade path.
            int createCount = stream.OfType(AsyncEventID.CreateAsyncContext).Count();
            int completeCount = stream.OfType(AsyncEventID.CompleteAsyncContext).Count();
            Assert.Equal(createCount, completeCount);

            // Inline-cascade optimization: exactly 1 Create for the entire chain.
            Assert.Equal(1, createCount);

            int methodResumeCount = stream.OfType(AsyncEventID.ResumeAsyncMethod).Count();
            int methodCompleteCount = stream.OfType(AsyncEventID.CompleteAsyncMethod).Count();

            // Method-level events should be balanced.
            Assert.Equal(methodResumeCount, methodCompleteCount);

            // No AppendAsyncCallstack events should be emitted in this scenario.
            int appendCount = stream.OfType(AsyncEventID.AppendAsyncCallstack).Count();
            Assert.Equal(0, appendCount);
        }
    }
}
