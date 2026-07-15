// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    // Tests for StateMachine (Task-based AsyncStateMachineBox) async profiler event emission.
    // All scenario methods use [RuntimeAsyncMethodGeneration(false)] to ensure they
    // exercise the legacy Task-based async path even if the default changes in the future.
    // Most tests use sync CollectEvents with RunScenarioAndFlush to isolate the StateMachine chain
    // on a threadpool thread, ensuring dispatcher finally blocks complete before flush.
    // Requires threading support.
    public partial class AsyncProfilerTests
    {
        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_SingleYield()
        {
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_DeepChain()
        {
            await StateMachineAsync_Level1();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_Level1()
        {
            await StateMachineAsync_Level2();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_Level2()
        {
            await StateMachineAsync_Level3();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_Level3()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ExceptionHandled()
        {
            try
            {
                await StateMachineAsync_InnerThrows();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_InnerThrows()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_UnhandledExceptionOuter()
        {
            await StateMachineAsync_UnhandledExceptionInner();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_UnhandledExceptionInner()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("unhandled inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_RecursiveChain(int depth)
        {
            if (depth <= 1)
            {
                await Task.Delay(100);
                return;
            }
            await StateMachineAsync_RecursiveChain(depth - 1);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_RecursiveChainGated(int depth, Task gate)
        {
            if (depth <= 1)
            {
                await gate;
                return;
            }
            await StateMachineAsync_RecursiveChainGated(depth - 1, gate);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_Level1()
        {
            await StateMachineAsync_ValueTask_Level2();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_Level2()
        {
            await StateMachineAsync_ValueTask_Level3();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_Level3()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_Level1()
        {
            await StateMachineAsync_PoolingValueTask_Level2();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_Level2()
        {
            await StateMachineAsync_PoolingValueTask_Level3();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_Level3()
        {
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_EventsEmitted()
        {
            var events = CollectEvents(StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_SingleYield());
            });

            // DumpAllEvents(events);

            AssertTrue(events, events.Events.Count > 0, "Expected at least one AsyncEvents event to be emitted");
            AssertContains(events, events.Events, e => e.EventId == AsyncEventsId);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CreateAsyncContextEmittedOnFirstAwait()
        {
            var events = CollectEvents(StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_SingleYield());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var creates = stream.OfType(AsyncEventID.CreateStateMachineAsyncContext).ToList();
            AssertTrue(stream, creates.Count >= 1, $"Expected at least 1 CreateStateMachineAsyncContextKeyword, got {creates.Count}");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_EventSequenceOrder_Marker()
        {
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_EventSequenceOrder()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_EventSequenceOrder_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_EventSequenceOrder_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume");

            // A single-await method completes on its only resume, so its dispatcher must report
            // Complete and never Suspend (validates the complete side of the IsCompleted classification).
            int suspendCount = ids.Count(id => id == AsyncEventID.SuspendStateMachineAsyncContext);
            AssertEqual(stream, 0, suspendCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_SuspendResumeCompleteEvents_Marker()
        {
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_SuspendResumeCompleteEvents()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_SuspendResumeCompleteEvents_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_SuspendResumeCompleteEvents_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeCount = ids.Count(id => id == AsyncEventID.ResumeStateMachineAsyncContext);
            AssertTrue(stream, resumeCount >= 1, "Expected at least one ResumeStateMachineAsyncContext");

            int completeCount = ids.Count(id => id == AsyncEventID.CompleteStateMachineAsyncContext);
            AssertTrue(stream, completeCount >= 1, "Expected at least one CompleteStateMachineAsyncContext");

            int suspendCount = ids.Count(id => id == AsyncEventID.SuspendStateMachineAsyncContext);

            // The 3-await marker yields before completing, so at least one Suspend must be emitted.
            AssertTrue(stream, suspendCount >= 1, "Expected at least one SuspendStateMachineAsyncContext");

            // Each resume ends in exactly one suspend (yielded) or one complete (finished).
            AssertEqual(stream, resumeCount, completeCount + suspendCount);

            // A suspension must sit between a resume and a later resume (Resume -> Suspend -> Resume),
            // and the chain must end in Complete (terminal) after the final resume.
            int firstResumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext);
            int firstSuspendIdx = ids.IndexOf(AsyncEventID.SuspendStateMachineAsyncContext, firstResumeIdx + 1);
            AssertTrue(stream, firstSuspendIdx > firstResumeIdx, "Expected SuspendStateMachineAsyncContext after a Resume");

            int resumeAfterSuspendIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, firstSuspendIdx + 1);
            AssertTrue(stream, resumeAfterSuspendIdx > firstSuspendIdx, "Expected a ResumeStateMachineAsyncContext after a Suspend");

            int completeIdx = ids.LastIndexOf(AsyncEventID.CompleteStateMachineAsyncContext);
            AssertTrue(stream, completeIdx > firstSuspendIdx, "Expected the terminal CompleteStateMachineAsyncContext after suspensions");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_InlineResume_CompletesWithoutSuspend_Marker(Task gate, StrongBox<int> resumedThreadId)
        {
            await gate;
            resumedThreadId.Value = Environment.CurrentManagedThreadId;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_InlineResume_CompletesWithoutSuspend()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    var resumedThreadId = new StrongBox<int>();
                    var gate = new TaskCompletionSource();
                    Task marker = StateMachineAsync_InlineResume_CompletesWithoutSuspend_Marker(gate.Task, resumedThreadId);

                    int setResultThreadId = Environment.CurrentManagedThreadId;
                    gate.SetResult();

                    Assert.True(resumedThreadId.Value == setResultThreadId,
                        $"Expected inline resume on the SetResult thread {setResultThreadId}, but the marker resumed on thread {resumedThreadId.Value} (0 = did not resume synchronously)");

                    await marker;
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_InlineResume_CompletesWithoutSuspend_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var chain = stream.ChainEventsFromDispatcher(dispatcherId);
            var ids = chain.Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume");

            // Completing inline on the only resume must not emit a Suspend.
            int suspendCount = ids.Count(id => id == AsyncEventID.SuspendStateMachineAsyncContext);
            AssertEqual(stream, 0, suspendCount);

            ParsedEvent resumeEvt = chain.First(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext);
            ParsedEvent completeEvt = chain.First(e => e.EventId == AsyncEventID.CompleteStateMachineAsyncContext);
            AssertTrue(stream, resumeEvt.OsThreadId != 0, "Expected a valid OS thread id on the Resume event");
            AssertEqual(stream, resumeEvt.OsThreadId, completeEvt.OsThreadId);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_InlineResume_ReSuspends_Marker(Task gate1, StrongBox<int> resumedThreadId, Task gate2)
        {
            await gate1;
            resumedThreadId.Value = Environment.CurrentManagedThreadId;
            await gate2;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_InlineResume_ReSuspends()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    var resumedThreadId = new StrongBox<int>();
                    var gate1 = new TaskCompletionSource();
                    var gate2 = new TaskCompletionSource();
                    Task marker = StateMachineAsync_InlineResume_ReSuspends_Marker(gate1.Task, resumedThreadId, gate2.Task);

                    int setResultThreadId = Environment.CurrentManagedThreadId;
                    gate1.SetResult();

                    Assert.True(resumedThreadId.Value == setResultThreadId,
                        $"Expected inline resume on the gate1.SetResult thread {setResultThreadId}, but the marker resumed on thread {resumedThreadId.Value} (0 = did not resume synchronously)");

                    gate2.SetResult();
                    await marker;
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_InlineResume_ReSuspends_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // The marker resumes under two dispatchers (one per gate). The first one re-suspends, so identify
            // it as the marker dispatcher whose own events contain a Suspend.
            var markerDispatcherIds = markerCallstacks.Select(c => c.DispatcherId).Distinct().ToList();

            List<ParsedEvent>? suspendingEvents = null;
            foreach (ulong id in markerDispatcherIds)
            {
                var own = stream.All.Where(e => e.DispatcherId == id).OrderBy(e => e.Timestamp).ToList();
                if (own.Any(e => e.EventId == AsyncEventID.SuspendStateMachineAsyncContext))
                {
                    suspendingEvents = own;
                    break;
                }
            }

            AssertTrue(stream, suspendingEvents != null, "Expected a marker dispatcher that re-suspended inline");

            var d1Ids = suspendingEvents!.Select(e => e.EventId).ToList();

            int createIdx = d1Ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext on the re-suspending dispatcher");

            int resumeIdx = d1Ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int suspendIdx = d1Ids.IndexOf(AsyncEventID.SuspendStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, suspendIdx > resumeIdx, "Expected SuspendStateMachineAsyncContext after Resume (inline re-suspension)");

            ParsedEvent resumeEvt = suspendingEvents!.First(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext);
            ParsedEvent suspendEvt = suspendingEvents!.First(e => e.EventId == AsyncEventID.SuspendStateMachineAsyncContext);
            AssertTrue(stream, resumeEvt.OsThreadId != 0, "Expected a valid OS thread id on the Resume event");
            AssertEqual(stream, resumeEvt.OsThreadId, suspendEvt.OsThreadId);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_PoolResume_ReSuspends_Marker(Task gate)
        {
            await gate;
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolResume_ReSuspends()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    Task marker = StateMachineAsync_PoolResume_ReSuspends_Marker(gate.Task);
                    gate.SetResult();
                    await marker;
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolResume_ReSuspends_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // The marker resumes under two dispatchers (the gate hop and the Task.Delay hop). The first one
            // re-suspends, so identify it as the marker dispatcher whose own events contain a Suspend.
            var markerDispatcherIds = markerCallstacks.Select(c => c.DispatcherId).Distinct().ToList();

            List<ParsedEvent>? suspendingEvents = null;
            foreach (ulong id in markerDispatcherIds)
            {
                var own = stream.All.Where(e => e.DispatcherId == id).OrderBy(e => e.Timestamp).ToList();
                if (own.Any(e => e.EventId == AsyncEventID.SuspendStateMachineAsyncContext))
                {
                    suspendingEvents = own;
                    break;
                }
            }

            AssertTrue(stream, suspendingEvents != null, "Expected a marker dispatcher that re-suspended on the pool");

            var ids = suspendingEvents!.Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext on the re-suspending dispatcher");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int suspendIdx = ids.IndexOf(AsyncEventID.SuspendStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, suspendIdx > resumeIdx, "Expected SuspendStateMachineAsyncContext after Resume (pool re-suspension)");

            ParsedEvent resumeEvt = suspendingEvents!.First(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext);
            ParsedEvent suspendEvt = suspendingEvents!.First(e => e.EventId == AsyncEventID.SuspendStateMachineAsyncContext);
            AssertTrue(stream, resumeEvt.OsThreadId != 0, "Expected a valid OS thread id on the Resume event");
            AssertEqual(stream, resumeEvt.OsThreadId, suspendEvt.OsThreadId);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ResumeCompleteMethodEvents()
        {
            var events = CollectEvents(StateMachineAsyncMethodKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_SingleYield());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var ids = stream.EventIds;

            AssertContains(stream, AsyncEventID.ResumeStateMachineAsyncMethod, ids);
            AssertContains(stream, AsyncEventID.CompleteStateMachineAsyncMethod, ids);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_HandledException_EmitsUnwindAndComplete_Marker()
        {
            await StateMachineAsync_ExceptionHandled();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_HandledException_EmitsUnwindAndComplete_Marker());
            });

            //DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_HandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int unwindIdx = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx > resumeIdx, "Expected UnwindStateMachineAsyncException after Resume");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, unwindIdx + 1);
            AssertTrue(stream, completeIdx > unwindIdx, "Expected CompleteStateMachineAsyncContext after Unwind");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_UnhandledException_EmitsUnwindAndComplete_Marker()
        {
            await StateMachineAsync_UnhandledExceptionOuter();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => StateMachineAsync_UnhandledException_EmitsUnwindAndComplete_Marker());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_UnhandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int unwindIdx1 = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx1 > resumeIdx, "Expected first UnwindStateMachineAsyncException after Resume");

            int unwindIdx2 = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, unwindIdx1 + 1);
            AssertTrue(stream, unwindIdx2 > unwindIdx1, "Expected second UnwindStateMachineAsyncException after first Unwind");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, unwindIdx2 + 1);
            AssertTrue(stream, completeIdx > unwindIdx2, "Expected CompleteStateMachineAsyncContext after second Unwind");
        }


        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_MethodEventCountMatchesChainDepth_Marker()
        {
            await StateMachineAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_MethodEventCountMatchesChainDepth()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncMethodKeywords | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_MethodEventCountMatchesChainDepth_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Marker -> DeepChain -> Level1 -> Level2 -> Level3
            const int ExpectedChainDepth = 5;

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_MethodEventCountMatchesChainDepth_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            AssertEqual(stream, ExpectedChainDepth, markerCallstacks[0].Frames.Count);

            ulong leafDispatcherId = markerCallstacks[0].DispatcherId;
            var chainEvents = stream.ChainEventsFromDispatcher(leafDispatcherId);

            int resumeCount = chainEvents.Count(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncMethod);
            AssertEqual(stream, ExpectedChainDepth, resumeCount);

            int completeCount = chainEvents.Count(e => e.EventId == AsyncEventID.CompleteStateMachineAsyncMethod);
            AssertEqual(stream, ExpectedChainDepth, completeCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_HandledException_MethodEventsWithUnwind_Marker()
        {
            await StateMachineAsync_ExceptionHandled();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_HandledException_MethodEventsWithUnwind()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncMethodKeywords | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_HandledException_MethodEventsWithUnwind_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_HandledException_MethodEventsWithUnwind_Marker));

            AssertNotEmpty(stream, markerCallstacks);

            ulong leafDispatcherId = markerCallstacks[0].DispatcherId;
            var chainEvents = stream.ChainEventsFromDispatcher(leafDispatcherId);

            var sequence = chainEvents
                .Where(e => e.EventId is AsyncEventID.ResumeStateMachineAsyncMethod
                    or AsyncEventID.CompleteStateMachineAsyncMethod
                    or AsyncEventID.UnwindStateMachineAsyncException)
                .Select(e => e.EventId)
                .ToList();

            // Exactly one Unwind expected on the chain (InnerThrows throws once).
            AssertEqual(stream, 1, sequence.Count(id => id == AsyncEventID.UnwindStateMachineAsyncException ));

            // Around the Unwind: the throwing method's Resume precedes it, and the catching
            // method's Resume -> Complete pair follows.
            int unwindIdx = sequence.IndexOf(AsyncEventID.UnwindStateMachineAsyncException);
            AssertEqual(stream, AsyncEventID.ResumeStateMachineAsyncMethod, sequence[unwindIdx - 1]);
            AssertEqual(stream, AsyncEventID.ResumeStateMachineAsyncMethod, sequence[unwindIdx + 1]);
            AssertEqual(stream, AsyncEventID.CompleteStateMachineAsyncMethod, sequence[unwindIdx + 2]);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_UnhandledException_MethodEventsWithUnwind_Marker()
        {
            await StateMachineAsync_UnhandledExceptionOuter();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_UnhandledException_MethodEventsWithUnwind()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncMethodKeywords | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => StateMachineAsync_UnhandledException_MethodEventsWithUnwind_Marker());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Marker -> UnhandledExceptionOuter -> UnhandledExceptionInner
            const int ExpectedChainDepth = 3;

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_UnhandledException_MethodEventsWithUnwind_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong leafDispatcherId = markerCallstacks[0].DispatcherId;
            var chainEvents = stream.ChainEventsFromDispatcher(leafDispatcherId);

            var sequence = chainEvents
                .Where(e => e.EventId is AsyncEventID.ResumeStateMachineAsyncMethod
                    or AsyncEventID.CompleteStateMachineAsyncMethod
                    or AsyncEventID.UnwindStateMachineAsyncException)
                .Select(e => e.EventId)
                .ToList();

            // Every method in the chain unwinds (no catch); no CompleteAsyncMethod expected.
            AssertEqual(stream, ExpectedChainDepth, sequence.Count(id => id == AsyncEventID.ResumeStateMachineAsyncMethod));
            AssertEqual(stream, ExpectedChainDepth, sequence.Count(id => id == AsyncEventID.UnwindStateMachineAsyncException));
            AssertEqual(stream, 0, sequence.Count(id => id == AsyncEventID.CompleteStateMachineAsyncMethod));

            // Per-method ordering: each Resume is immediately followed by its Unwind.
            for (int i = 0; i < sequence.Count; i += 2)
            {
                AssertEqual(stream, AsyncEventID.ResumeStateMachineAsyncMethod, sequence[i]);
                AssertEqual(stream, AsyncEventID.UnwindStateMachineAsyncException, sequence[i + 1]);
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResumeAsyncCallstackEmitted_Marker()
        {
            await StateMachineAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ResumeAsyncCallstackEmitted()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ResumeAsyncCallstackEmitted_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ResumeAsyncCallstackEmitted_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            AssertAll(stream, markerCallstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in resume callstack");
                Assert.True(cs.Frames[0].MethodId != 0, "Expected non-zero methodId in first frame");
            });
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackDepthMatchesChainDepth_Marker()
        {
            await StateMachineAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_CallstackDepthMatchesChainDepth_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_CallstackDepthMatchesChainDepth_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // StateMachineAsync_CallstackDepthMarker -> Level1 -> Level2 -> Level3: deepest callstack should have exactly 4 frames
            AssertEqual(stream, 4, markerCallstacks[0].FrameCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackFramesHaveDistinctMethodIds_Marker()
        {
            await StateMachineAsync_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CallstackFramesHaveDistinctMethodIds()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_CallstackFramesHaveDistinctMethodIds_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_CallstackFramesHaveDistinctMethodIds_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Frames in the same callstack should have distinct methodIds (different async methods)
            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            AssertEqual(stream, methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackFramesHaveDistinctStates_Root_Marker()
        {
            await Task.Yield();
            await StateMachineAsync_CallstackFramesHaveDistinctStates_Middle_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackFramesHaveDistinctStates_Middle_Marker()
        {
            await Task.Yield();
            await Task.Yield();
            await StateMachineAsync_CallstackFramesHaveDistinctStates_Leaf_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackFramesHaveDistinctStates_Leaf_Marker()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackFramesHaveDistinctStates_Marker()
        {
            await StateMachineAsync_CallstackFramesHaveDistinctStates_Root_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CallstackFramesHaveDistinctStates()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_CallstackFramesHaveDistinctStates_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_CallstackFramesHaveDistinctStates_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // The deepest callstack (on the final Delay resume) should have 4 frames:
            // Leaf (state=0), Middle (state=2), Root (state=1), Marker (state=0)
            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            AssertEqual(stream, 4, deepest.FrameCount);

            // Each frame's state should reflect its suspend point (values may repeat across frames).
            var states = deepest.Frames.Select(f => f.State).ToList();
            AssertEqual(stream, 0, states[0]); // Leaf: suspended at 1st await (state=0)
            AssertEqual(stream, 2, states[1]); // Middle: suspended at 3rd await (state=2)
            AssertEqual(stream, 1, states[2]); // Root: suspended at 2nd await (state=1)
            AssertEqual(stream, 0, states[3]); // Marker: suspended at 1st await (state=0)
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Level1_Marker()
        {
            await StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Level2_Marker();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Level2_Marker()
        {
            await StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Level3_Marker();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Level3_Marker()
        {
            await Task.Delay(100);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Marker()
        {
            await StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Level1_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_YieldAtEachLevel_CallstackShrinks()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_YieldAtEachLevel_CallstackShrinks_Marker));

            // After Task.Delay resumes: full chain (Level3, Level2, Level1, Marker) = 4 frames
            // After Level3's yield resumes: Level3 completes, chain is (Level2, Level1, Marker) = 3 frames
            // After Level2's yield resumes: Level2 completes, chain is (Level1, Marker) = 2 frames
            AssertContains(stream, markerCallstacks, cs => cs.FrameCount == 4);
            AssertContains(stream, markerCallstacks, cs => cs.FrameCount == 3);
            AssertContains(stream, markerCallstacks, cs => cs.FrameCount == 2);
        }

        private static SemaphoreSlim s_appendRace_proceed;

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_AppendCallstack_FiresOnLateParentRegistration_Child_Marker()
        {
            await Task.Yield();
            s_appendRace_proceed.Release();
            Thread.Sleep(200);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker()
        {
            Task t = StateMachineAsync_AppendCallstack_FiresOnLateParentRegistration_Child_Marker();
            Assert.True(s_appendRace_proceed.Wait(TimeSpan.FromSeconds(20)), "Timed out waiting for child to reach append-race checkpoint");
            await t;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_AppendCallstack_FiresOnLateParentRegistration()
        {
            s_appendRace_proceed = new SemaphoreSlim(0, 1);

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The initial Resume should only include StateMachineAsync_AppendRace_Child (race: Parent not registered yet).
            var childOnlyResumes = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_AppendCallstack_FiresOnLateParentRegistration_Child_Marker));
            AssertNotEmpty(stream, childOnlyResumes);

            // After Parent registers and Child hits its next complete hook,
            // an AppendAsyncCallstack should fire with the Parent frame.
            var appendsWithParent = stream.CallstacksWithMarker(AsyncEventID.AppendStateMachineAsyncCallstack, nameof(StateMachineAsync_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker));
            AssertNotEmpty(stream, appendsWithParent);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CompleteChain_DoesNotEmitAppendEvents_Marker()
        {
            await StateMachineAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CompleteChain_DoesNotEmitAppendEvents()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_CompleteChain_DoesNotEmitAppendEvents_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Sanity: the marker frame must appear in the initial Resume callstack (full chain captured).
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_CompleteChain_DoesNotEmitAppendEvents_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // No Append events should fire on this chain -- the chain was complete at Resume time.
            ulong chainDispatcherId = markerCallstacks[0].DispatcherId;
            var appendEvents = stream.ChainEventsFromDispatcher(chainDispatcherId)
                .Where(e => e.EventId == AsyncEventID.AppendStateMachineAsyncCallstack)
                .ToList();
            AssertEmpty(stream, appendEvents);
        }

        private static InlinePostSynchronizationContext? s_stateMachineAsyncSyncContextCtx;

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker()
        {
            // Install a non-default SynchronizationContext on this thread so the await captures it.
            // The await's continuation will be routed via SynchronizationContextAwaitTaskContinuation,
            // which wraps the box in an AsyncStateMachineDispatcher and posts back to the context.
            int callerThreadId = Environment.CurrentManagedThreadId;
            SynchronizationContext? prev = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(s_stateMachineAsyncSyncContextCtx);
            try
            {
                await Task.Delay(100);
            }
            finally
            {
                // The await may resume on a different thread; only restore on the original (install)
                // thread. On multi-threaded platforms this method runs on a dedicated throwaway
                // thread (see RunIsolatedScenarioAsync), so a skipped restore can't leak the context
                // onto a shared thread-pool thread. On single-threaded platforms the resume is on
                // this same thread, so the restore always runs here.
                if (Environment.CurrentManagedThreadId == callerThreadId)
                {
                    SynchronizationContext.SetSynchronizationContext(prev);
                }
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CustomSyncContext_EmitsContextEventsAndCallstack()
        {
            s_stateMachineAsyncSyncContextCtx = new InlinePostSynchronizationContext();

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => RunIsolatedScenarioAsync(StateMachineAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker));
            });

            // DumpAllEvents(events);

            // The custom SyncContext should have received at least one Post for the await continuation.
            AssertTrue(events, s_stateMachineAsyncSyncContextCtx.PostCount > 0,
                $"Expected custom SynchronizationContext to receive at least one Post, got {s_stateMachineAsyncSyncContextCtx.PostCount}");

            var stream = ParseAllEvents(events);

            // The marker frame should appear in the Resume callstack.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Verify the standard Create -> Resume -> Complete sequence fired for our chain.
            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext for the custom SyncContext scenario");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker()
        {
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack()
        {
            var scheduler = new InlineRunTaskScheduler();

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                try
                {
                    Task.Factory.StartNew(
                        () => StateMachineAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker(),
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
            AssertTrue(events, scheduler.QueuedCount >= 1,
                $"Expected custom TaskScheduler to receive at least one QueueTask call, got {scheduler.QueuedCount}");

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Verify standard Create -> Resume -> Complete sequence for our chain.
            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext for the custom TaskScheduler scenario");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume");
        }

        // Custom awaiters that post their continuation directly to the thread pool, bypassing the
        // Task/ValueTask/Yield "known awaiter" fast paths (ITaskAwaiter / IConfiguredTaskAwaiter /
        // IStateMachineBoxAwareAwaiter). They exercise the builder's fallback completion paths:
        // the ICriticalNotifyCompletion awaiter routes through AwaitUnsafeOnCompleted's else-branch,
        // the INotifyCompletion-only awaiter routes through AwaitOnCompleted. Both must still create a
        // dispatcher so the await is represented in the V1 event stream.
        private sealed class DirectPostCriticalAwaitable
        {
            public DirectPostCriticalAwaiter GetAwaiter() => default;
        }

        private readonly struct DirectPostCriticalAwaiter : ICriticalNotifyCompletion
        {
            public bool IsCompleted => false;
            public void GetResult() { }
            public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);
            public void UnsafeOnCompleted(Action continuation) =>
                ThreadPool.QueueUserWorkItem(static c => c(), continuation, preferLocal: false);
        }

        private sealed class DirectPostNotifyAwaitable
        {
            public DirectPostNotifyAwaiter GetAwaiter() => default;
        }

        private readonly struct DirectPostNotifyAwaiter : INotifyCompletion
        {
            public bool IsCompleted => false;
            public void GetResult() { }
            public void OnCompleted(Action continuation) =>
                ThreadPool.QueueUserWorkItem(static c => c(), continuation, preferLocal: false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_Critical_Marker()
        {
            await new DirectPostCriticalAwaitable();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_Notify_Marker()
        {
            await new DirectPostNotifyAwaitable();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_PoolingCritical_Marker()
        {
            await new DirectPostCriticalAwaitable();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_PoolingNotify_Marker()
        {
            await new DirectPostNotifyAwaitable();
        }

        // Covers all four builder fallback sites that wrap the box for an opaque custom awaiter:
        //  - Task + ICriticalNotifyCompletion -> AsyncTaskMethodBuilderT.AwaitUnsafeOnCompleted else-branch
        //  - Task + INotifyCompletion         -> AsyncTaskMethodBuilderT.AwaitOnCompleted
        //  - pooling + ICriticalNotifyCompletion -> PoolingAsyncValueTaskMethodBuilderT.AwaitUnsafeOnCompleted (delegates to the shared else-branch)
        //  - pooling + INotifyCompletion         -> PoolingAsyncValueTaskMethodBuilderT.AwaitOnCompleted (its own create site)
        [ConditionalTheory(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete(bool pooling, bool criticalNotifyCompletion)
        {
            (string markerName, Func<Task> scenario) = (pooling, criticalNotifyCompletion) switch
            {
                (false, true) => (nameof(StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_Critical_Marker), (Func<Task>)StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_Critical_Marker),
                (false, false) => (nameof(StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_Notify_Marker), StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_Notify_Marker),
                (true, true) => (nameof(StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_PoolingCritical_Marker), () => StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_PoolingCritical_Marker().AsTask()),
                _ => (nameof(StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_PoolingNotify_Marker), () => StateMachineAsync_CustomAwaiter_EmitsCreateResumeComplete_PoolingNotify_Marker().AsTask()),
            };

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(scenario);
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // A custom awaiter that posts its continuation directly (bypassing the Task/ValueTask/Yield
            // fast paths) must still produce a dispatcher: the marker frame appears in a Resume callstack
            // and the chain emits the standard Create -> Resume -> Complete sequence. Without the fallback
            // dispatcher creation the marker is absent from the stream and this assertion fails.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, markerName);
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext for the custom awaiter scenario");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_NoEventsWhenDisabled_Marker()
        {
            await Task.Delay(50);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_NoEventsWhenDisabled()
        {
            for (int i = 0; i < 50; i++)
            {
                RunScenario(() => StateMachineAsync_NoEventsWhenDisabled_Marker());
            }

            // Now attach a listener but don't perform any StateMachine async work.
            var events = CollectEvents(StateMachineAsyncCoreKeywords, () => { });

            var ids = ParseAllEvents(events).EventIds;
            int contextEvents = ids.Count(id =>
                id == AsyncEventID.CreateStateMachineAsyncContext ||
                id == AsyncEventID.ResumeStateMachineAsyncContext ||
                id == AsyncEventID.CompleteStateMachineAsyncContext);

            AssertEqual(events, 0, contextEvents);
        }

        public static IEnumerable<object[]> StateMachineAsyncKeywordGatekeepingData()
        {
            yield return new object[] { (long)CreateStateMachineAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CreateStateMachineAsyncContext } };
            yield return new object[] { (long)ResumeStateMachineAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeStateMachineAsyncContext } };
            yield return new object[] { (long)SuspendStateMachineAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.SuspendStateMachineAsyncContext } };
            yield return new object[] { (long)CompleteStateMachineAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CompleteStateMachineAsyncContext } };
            yield return new object[] { (long)UnwindStateMachineAsyncExceptionKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.UnwindStateMachineAsyncException } };
            yield return new object[] { (long)ResumeStateMachineAsyncCallstackKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeStateMachineAsyncCallstack, AsyncEventID.AppendStateMachineAsyncCallstack } };
            yield return new object[] { (long)ResumeStateMachineAsyncMethodKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.ResumeStateMachineAsyncMethod } };
            yield return new object[] { (long)CompleteStateMachineAsyncMethodKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.AsyncProfilerMetadata, AsyncEventID.CompleteStateMachineAsyncMethod } };
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_KeywordGatekeeping_Marker()
        {
            // Exercise multiple event types: exception unwind, multiple completes, method invocations.
            try
            {
                await StateMachineAsync_InnerThrows();
            }
            catch (InvalidOperationException) { }
            await StateMachineAsync_SingleYield();
            await Task.Delay(50);
        }

        [ConditionalTheory(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        [MemberData(nameof(StateMachineAsyncKeywordGatekeepingData))]
        public void StateMachineAsync_KeywordGatekeeping(long keywordValue, AsyncEventID[] allowedEventIds)
        {
            EventKeywords kw = (EventKeywords)keywordValue;
            var allowed = new HashSet<AsyncEventID>(allowedEventIds);

            var events = CollectEvents(kw, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_KeywordGatekeeping_Marker());
            });

            var stream = ParseAllEvents(events);
            var unexpected = stream.EventIds.Where(id => !allowed.Contains(id)).ToList();

            AssertTrue(stream, unexpected.Count == 0,
                $"Keyword 0x{(long)kw:X}: unexpected event IDs [{string.Join(", ", unexpected)}], allowed [{string.Join(", ", allowed)}]");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAll_TracksAllBranches_BranchA_Marker()
        {
            await Task.Delay(100);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAll_TracksAllBranches_BranchB_Marker()
        {
            await Task.Delay(120);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAll_TracksAllBranches_BranchC_Marker()
        {
            await Task.Delay(140);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAll_TracksAllBranches_Marker()
        {
            await Task.WhenAll(
                StateMachineAsync_WhenAll_TracksAllBranches_BranchA_Marker(),
                StateMachineAsync_WhenAll_TracksAllBranches_BranchB_Marker(),
                StateMachineAsync_WhenAll_TracksAllBranches_BranchC_Marker());
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_WhenAll_TracksAllBranches()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_WhenAll_TracksAllBranches_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAll_TracksAllBranches_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Each branch is its own async chain; its inner await of Task.Delay produces a Resume callstack containing the branch frame.
            var branchACallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAll_TracksAllBranches_BranchA_Marker));
            var branchBCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAll_TracksAllBranches_BranchB_Marker));
            var branchCCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAll_TracksAllBranches_BranchC_Marker));
            AssertNotEmpty(stream, branchACallstacks);
            AssertNotEmpty(stream, branchBCallstacks);
            AssertNotEmpty(stream, branchCCallstacks);

            // Each tracked chain (3 branches + outer marker) must see exactly one Create and one Complete in its own dispatcher tree.
            AssertExactlyOneCreateAndComplete(stream, branchACallstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAll_TracksAllBranches_BranchA_Marker));
            AssertExactlyOneCreateAndComplete(stream, branchBCallstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAll_TracksAllBranches_BranchB_Marker));
            AssertExactlyOneCreateAndComplete(stream, branchCCallstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAll_TracksAllBranches_BranchC_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAll_TracksAllBranches_Marker));

            // The outer marker's chain should fire the standard Create -> Resume -> Complete sequence in its own dispatcher tree, in that order.
            ulong markerDispatcherId = markerCallstacks[0].DispatcherId;
            var markerIds = stream.ChainEventsFromDispatcher(markerDispatcherId).Select(e => e.EventId).ToList();

            int createIdx = markerIds.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext for the WhenAll outer marker");

            int resumeIdx = markerIds.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create on the outer marker");

            int completeIdx = markerIds.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume on the outer marker");

            // The outer should be created exactly once.
            int createCountForMarker = markerIds.Count(id => id == AsyncEventID.CreateStateMachineAsyncContext);
            AssertEqual(stream, 1, createCountForMarker);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAny_TracksAllBranches_Fast_Marker()
        {
            await Task.Delay(50);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAny_TracksAllBranches_Slow1_Marker()
        {
            await Task.Delay(400);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAny_TracksAllBranches_Slow2_Marker()
        {
            await Task.Delay(600);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WhenAny_TracksAllBranches_Marker()
        {
            Task fast = StateMachineAsync_WhenAny_TracksAllBranches_Fast_Marker();
            Task slow1 = StateMachineAsync_WhenAny_TracksAllBranches_Slow1_Marker();
            Task slow2 = StateMachineAsync_WhenAny_TracksAllBranches_Slow2_Marker();

            await Task.WhenAny(fast, slow1, slow2);
            await Task.WhenAll(slow1, slow2);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_WhenAny_TracksAllBranches()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_WhenAny_TracksAllBranches_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // All branches - including the slow ones whose completion the outer is no longer
            // strictly waiting on after WhenAny returned -- must produce their own Resume
            // callstacks. This proves their dispatcher lifetimes are tracked independently.
            var fastCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Fast_Marker));
            var slow1Callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Slow1_Marker));
            var slow2Callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Slow2_Marker));
            AssertNotEmpty(stream, fastCallstacks);
            AssertNotEmpty(stream, slow1Callstacks);
            AssertNotEmpty(stream, slow2Callstacks);

            // Each tracked chain (3 branches + outer marker) must see exactly one Create and one Complete in its own dispatcher tree.
            AssertExactlyOneCreateAndComplete(stream, fastCallstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Fast_Marker));
            AssertExactlyOneCreateAndComplete(stream, slow1Callstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Slow1_Marker));
            AssertExactlyOneCreateAndComplete(stream, slow2Callstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Slow2_Marker));
            AssertCreateBalancesSuspendAndCompleteInChain(stream, markerCallstacks[0].DispatcherId, nameof(StateMachineAsync_WhenAny_TracksAllBranches_Marker));

            // The outer marker's chain: exactly one Create, at least two Resumes (one after
            // WhenAny, one after WhenAll on the slow branches), then Complete.
            ulong markerDispatcherId = markerCallstacks[0].DispatcherId;
            var markerEvents = stream.ChainEventsFromDispatcher(markerDispatcherId);
            var markerIds = markerEvents.Select(e => e.EventId).ToList();

            int resumeCountForMarker = markerIds.Count(id => id == AsyncEventID.ResumeStateMachineAsyncCallstack);
            AssertTrue(stream, resumeCountForMarker >= 1,
                $"Expected outer marker to be resumed at least once, got {resumeCountForMarker}");

            int completeCountForMarker = markerIds.Count(id => id == AsyncEventID.CompleteStateMachineAsyncContext);
            AssertTrue(stream, completeCountForMarker >= 1, "Expected at least one CompleteStateMachineAsyncContext for the outer marker");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackDepthCappedAtMaxFrames_Marker(int depth)
        {
            await StateMachineAsync_RecursiveChain(depth);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CallstackDepthCappedAtMaxFrames()
        {
            // Build a chain deeper than the 255-frame cap (byte FrameCount). The deepest
            // ResumeAsyncCallstack should clamp at byte.MaxValue without crashing.
            const int requestedDepth = 300;

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_CallstackDepthCappedAtMaxFrames_Marker(requestedDepth));
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The chain is deeper than the 255-frame cap, so the initial Resume clamps at byte.MaxValue and
            // the dropped frames are NOT backfilled by a later Append: the cap is a hard limit. The marker
            // sits at the top of the chain (deeper than 255) and is therefore truncated away, so scope to the
            // recursive method instead.
            var mergedChain = stream.MergedResumeCallstacks()
                .Where(cs => cs.Frames.Any(f => GetMethodNameFromMethodId(cs.CallstackType, f.MethodId) == nameof(StateMachineAsync_RecursiveChain)))
                .ToList();
            AssertNotEmpty(stream, mergedChain);

            // Hard cap: no merged Resume+Append chain may exceed the cap. If Append backfilled the truncated
            // frames, the merged count would climb back toward the requested depth.
            AssertAll(stream, mergedChain, cs => Assert.True(cs.Frames.Count <= byte.MaxValue,
                $"Merged frame count {cs.Frames.Count} exceeds the {byte.MaxValue}-frame cap"));

            // The deepest chain must actually reach the cap (otherwise the test isn't exercising truncation).
            var deepest = mergedChain.MaxBy(cs => cs.Frames.Count);
            AssertNotNull(stream, deepest);
            AssertEqual(stream, (int)byte.MaxValue, deepest!.Frames.Count);
            AssertEqual(stream, (int)deepest.FrameCount, deepest.Frames.Count);

            // Every captured frame should resolve to a managed method.
            foreach (var (methodId, _) in deepest.Frames)
            {
                AssertTrue(stream, methodId != 0, "Frame has zero MethodId");
                var method = GetMethodNameFromMethodId(deepest.CallstackType, methodId);
                AssertTrue(stream, method is not null, $"MethodId 0x{methodId:X} does not resolve to a managed method");
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CallstackOverflow_PreservesFullDepth()
        {
            const int Depth = 300;
            const int MaxFrames = byte.MaxValue;
            const int Iterations = 128;

            // Warm up the recursive method so its state machine id is frozen at its tier-0 version, then
            // snapshot that id before tracing. (Snapshot is a no-op on non-Mono, where ids resolve
            // reflectively; the warmup itself runs on all runtimes.)
            var warmupGate = new TaskCompletionSource();
            Task warmup = StateMachineAsync_RecursiveChainGated(2, warmupGate.Task);
            warmupGate.SetResult();
            warmup.GetAwaiter().GetResult();
            SnapshotStateMachineMethodIdFor(typeof(AsyncProfilerTests).GetMethod(nameof(StateMachineAsync_RecursiveChainGated), BindingFlags.NonPublic | BindingFlags.Static)!);

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        var gate = new TaskCompletionSource();
                        Task chain = StateMachineAsync_RecursiveChainGated(Depth, gate.Task);
                        gate.SetResult();
                        await chain;
                    }
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The deep chains overflow the per-thread buffer many times over, so the rent/overflow path is
            // exercised. The regression check: every chain resume must carry the full (capped) frame count.
            // Scope to our recursive chains by method rather than by frame count -- a callstack truncated by
            // the overflow bug has fewer frames but still consists of the recursive method, so it stays in
            // scope and is caught.
            var chainCallstacks = stream.OfType(AsyncEventID.ResumeStateMachineAsyncCallstack)
                .Where(cs => cs.Frames.Any(f => GetMethodNameFromMethodId(cs.CallstackType, f.MethodId) == nameof(StateMachineAsync_RecursiveChainGated)))
                .ToList();
            AssertNotEmpty(stream, chainCallstacks);

            int leafDepth = chainCallstacks.Max(cs => (int)cs.FrameCount);
            AssertEqual(stream, MaxFrames, leafDepth);
            foreach (var cs in chainCallstacks)
            {
                AssertEqual(stream, leafDepth, (int)cs.FrameCount);
                AssertEqual(stream, (int)cs.FrameCount, cs.Frames.Count);
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackStressWithVaryingDepths_Recurse(int depth)
        {
            if (depth <= 1)
            {
                await Task.Delay(100);
                return;
            }
            await StateMachineAsync_CallstackStressWithVaryingDepths_Recurse(depth - 1);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_CallstackStressWithVaryingDepths_Marker(int depth)
        {
            await StateMachineAsync_CallstackStressWithVaryingDepths_Recurse(depth);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_CallstackStressWithVaryingDepths()
        {
            const int iterations = 100;
            int[] depths = new int[iterations];
            var rng = new Random(42);
            for (int i = 0; i < iterations; i++)
                depths[i] = rng.Next(1, 60);

            // Warm up the recursive method so its state machine id is frozen at its tier-0 version, then
            // snapshot that id before tracing. (Snapshot is a no-op on non-Mono, where ids resolve
            // reflectively; the warmup itself runs on all runtimes.)
            StateMachineAsync_CallstackStressWithVaryingDepths_Recurse(2).GetAwaiter().GetResult();
            SnapshotStateMachineMethodIdFor(typeof(AsyncProfilerTests).GetMethod(nameof(StateMachineAsync_CallstackStressWithVaryingDepths_Recurse), BindingFlags.NonPublic | BindingFlags.Static)!);

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    for (int i = 0; i < iterations; i++)
                        await StateMachineAsync_CallstackStressWithVaryingDepths_Marker(depths[i]);
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_CallstackStressWithVaryingDepths_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Every emitted callstack must have valid frame data.
            foreach (var cs in markerCallstacks)
            {
                AssertTrue(stream, cs.FrameCount > 0, "Callstack has 0 frames");
                AssertEqual(stream, (int)cs.FrameCount, cs.Frames.Count);
                for (int f = 0; f < cs.Frames.Count; f++)
                {
                    var (methodId, _) = cs.Frames[f];
                    AssertTrue(stream, methodId != 0, $"Frame {f} has zero MethodId");
                    var method = GetMethodNameFromMethodId(cs.CallstackType, methodId);
                    AssertTrue(stream, method is not null, $"Frame {f}: MethodId 0x{methodId:X} does not resolve to a managed method");
                }
            }

            // We expect at least one marker callstack per iteration.
            AssertTrue(stream, markerCallstacks.Count >= iterations,
                $"Expected at least {iterations} callstacks with marker, got {markerCallstacks.Count}");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WaitThenYield_BalancesResumeAndComplete_WaitYield_Marker(Task gate)
        {
            await gate;
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_WaitThenYield_BalancesResumeAndComplete_Marker()
        {
            await Task.Yield();

            var tcs = new TaskCompletionSource();
            Task b1 = StateMachineAsync_WaitThenYield_BalancesResumeAndComplete_WaitYield_Marker(tcs.Task);
            Task b2 = StateMachineAsync_WaitThenYield_BalancesResumeAndComplete_WaitYield_Marker(tcs.Task);

            tcs.SetResult();

            await Task.WhenAll(b1, b2);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_WaitThenYield_BalancesResumeAndComplete()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_WaitThenYield_BalancesResumeAndComplete_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Locate the marker's logical dispatcher so the balance check is scoped to this scenario's
            // chain and not polluted by unrelated dispatcher activity from other threads (e.g. the
            // xunit runner's RunAsync state machine being replayed by ResetAsyncThreadContext and
            // completing while this test's listener is still active).
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_WaitThenYield_BalancesResumeAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);
            ulong markerDispatcherId = markerCallstacks[0].DispatcherId;

            var taskEvents = stream.ChainEventsFromDispatcher(markerDispatcherId);
            int createCount = taskEvents.Count(e => e.EventId == AsyncEventID.CreateStateMachineAsyncContext);
            int completeCount = taskEvents.Count(e => e.EventId == AsyncEventID.CompleteStateMachineAsyncContext);
            int resumeCount = taskEvents.Count(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext);
            int suspendCount = taskEvents.Count(e => e.EventId == AsyncEventID.SuspendStateMachineAsyncContext);

            // At least one root Create event.
            AssertTrue(stream, createCount >= 1,
                $"Expected at least one CreateStateMachineAsyncContext event, got {createCount}");

            // Each resume cycle ends in exactly one suspend or one complete (model-agnostic).
            AssertEqual(stream, resumeCount, completeCount + suspendCount);

            // With dispatcher reuse a single dispatcher spans all of a method's yields: it may suspend
            // multiple times (interior) but is created and completed exactly once, so creates == completes.
            AssertEqual(stream, createCount, completeCount);

            AssertTrue(stream, createCount >= 3,
                $"Expected fan-out chain to produce at least 3 CreateStateMachineAsyncContext events (root + 2 child wraps), got {createCount}");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ConfigureAwaitFalse_Leaf_Marker()
        {
            await Task.Delay(100).ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ConfigureAwaitFalse_Mid_Marker()
        {
            await StateMachineAsync_ConfigureAwaitFalse_Leaf_Marker().ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ConfigureAwaitFalse_Marker()
        {
            await StateMachineAsync_ConfigureAwaitFalse_Mid_Marker().ConfigureAwait(false);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ConfigureAwaitFalse()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ConfigureAwaitFalse_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ConfigureAwaitFalse_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var frameNames = markerCallstacks[0].Frames
                .Select(f => GetMethodNameFromMethodId(markerCallstacks[0].CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();

            AssertContains(stream, nameof(StateMachineAsync_ConfigureAwaitFalse_Leaf_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_ConfigureAwaitFalse_Mid_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_ConfigureAwaitFalse_Marker), frameNames);

            // ConfigureAwait(false) on a sequential await chain collapses Leaf -> Mid -> Marker into one
            // continuation chain, so exactly one Create / one Complete is expected in the marker's dispatcher tree.
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(StateMachineAsync_ConfigureAwaitFalse_Marker));
        }

        // Generic async chain. Each method is generic over T and uses its parameter after the await, so the
        // compiler emits a generic state machine (<Marker>d__N`1). Instantiated with a reference type the JIT
        // reaches the async body through the shared (__Canon) generic code, whose per-instantiation MethodDesc
        // is an instantiating (wrapper) stub. The V1 methodId is the native code start of MoveNext, so
        // RuntimeMethodHandle_GetNativeCode must peel wrapper stubs to the shared body's code for the id to map
        // back to a managed method; otherwise a frame would carry a stub thunk address that resolves to null.
        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T> StateMachineAsync_GenericChain_Leaf_Marker<T>(T value)
        {
            await Task.Delay(100).ConfigureAwait(false);
            return value;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T> StateMachineAsync_GenericChain_FramesResolveToSharedBody_Mid_Marker<T>(T value)
        {
            T result = await StateMachineAsync_GenericChain_Leaf_Marker<T>(value).ConfigureAwait(false);
            return result;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<T> StateMachineAsync_GenericChain_FramesResolveToSharedBody_Marker<T>(T value)
        {
            T result = await StateMachineAsync_GenericChain_FramesResolveToSharedBody_Mid_Marker<T>(value).ConfigureAwait(false);
            return result;
        }

        // CoreCLR-only: the primary IP->name resolver (StackFrame.GetMethodFromNativeIP) exists only on CoreCLR,
        // and the Mono reverse-map fallback (CollectStateMachineMoveNextMethods) deliberately skips generic state
        // machines (ContainsGenericParameters), so a generic frame can't be named on Mono without proper rundown
        // or JIT-map data.
        //
        // A reference type (string) reaches the shared __Canon body through an instantiating (wrapper) stub that
        // RuntimeMethodHandle_GetNativeCode must peel; a value type (int) is fully specialized into its own code
        // (no wrapper stub) and resolves directly. Both must symbolize to their managed names.
        [ConditionalTheory(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported), nameof(IsNotMonoRuntime))]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        public void StateMachineAsync_GenericChain_FramesResolveToSharedBody(Type argType)
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                if (argType == typeof(int))
                {
                    RunScenarioAndFlush(() => StateMachineAsync_GenericChain_FramesResolveToSharedBody_Marker<int>(42));
                }
                else
                {
                    RunScenarioAndFlush(() => StateMachineAsync_GenericChain_FramesResolveToSharedBody_Marker<string>("value"));
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_GenericChain_FramesResolveToSharedBody_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var frameNames = markerCallstacks[0].Frames
                .Select(f => GetMethodNameFromMethodId(markerCallstacks[0].CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();

            // Every generic-chain frame must resolve to its managed name.
            AssertContains(stream, nameof(StateMachineAsync_GenericChain_Leaf_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_GenericChain_FramesResolveToSharedBody_Mid_Marker), frameNames);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_FaultedTask_Inner_Marker()
        {
            await Task.Delay(50);
            throw new InvalidOperationException("test fault");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_FaultedTask_Marker()
        {
            try
            {
                await StateMachineAsync_FaultedTask_Inner_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_FaultedTask()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | UnwindStateMachineAsyncExceptionKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_FaultedTask_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_FaultedTask_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var innerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_FaultedTask_Inner_Marker));
            AssertNotEmpty(stream, innerCallstacks);

            // Both inner (faulting) and outer marker chains must see exactly one Create and one Complete in their own dispatcher tree.
            // (Scoped to each chain's dispatcher; a whole-stream Create/Complete balance would be polluted by unrelated dispatcher
            // activity from other threads -- e.g. the xunit runner's state machine completing while this test's listener is active --
            // and by Completes that can fall outside the captured window when a resume is scheduled rather than inlined.)
            AssertExactlyOneCreateAndComplete(stream, innerCallstacks[0].DispatcherId, nameof(StateMachineAsync_FaultedTask_Inner_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(StateMachineAsync_FaultedTask_Marker));

            // The unwind must be attributed to the inner faulting chain.
            ulong innerDispatcherId = innerCallstacks[0].DispatcherId;
            int unwindCountForInner = stream.ChainEventsFromDispatcher(innerDispatcherId).Count(e => e.EventId == AsyncEventID.UnwindStateMachineAsyncException);
            AssertTrue(stream, unwindCountForInner >= 1,
                $"Expected at least one UnwindAsyncException on the faulted inner chain (DispatcherId {innerDispatcherId}), got {unwindCountForInner}");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_TaskCancellation_Inner_Marker(CancellationToken ct)
        {
            await Task.Delay(5000, ct);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_TaskCancellation_Marker()
        {
            using var cts = new CancellationTokenSource();
            Task inner = StateMachineAsync_TaskCancellation_Inner_Marker(cts.Token);
            cts.CancelAfter(50);
            try
            {
                await inner;
            }
            catch (OperationCanceledException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_TaskCancellation()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | UnwindStateMachineAsyncExceptionKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_TaskCancellation_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_TaskCancellation_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var innerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_TaskCancellation_Inner_Marker));
            AssertNotEmpty(stream, innerCallstacks);

            // Inner cancelled task + outer marker must each see exactly one Create and one Complete in their own dispatcher tree.
            AssertExactlyOneCreateAndComplete(stream, innerCallstacks[0].DispatcherId, nameof(StateMachineAsync_TaskCancellation_Inner_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(StateMachineAsync_TaskCancellation_Marker));
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_EventSequenceOrder_Marker()
        {
            await StateMachineAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ValueTask_EventSequenceOrder()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ValueTask_EventSequenceOrder_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ValueTask_EventSequenceOrder_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_MethodEventsEmitted_Marker()
        {
            await StateMachineAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ValueTask_MethodEventsEmitted()
        {
            var events = CollectEvents(StateMachineAsyncMethodKeywords | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ValueTask_MethodEventsEmitted_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeStateMachineAsyncMethod or AsyncEventID.CompleteStateMachineAsyncMethod)
                .Select(e => e.EventId)
                .ToList();

            int resumeCount = methodEvents.Count(id => id == AsyncEventID.ResumeStateMachineAsyncMethod);
            int completeCount = methodEvents.Count(id => id == AsyncEventID.CompleteStateMachineAsyncMethod);

            // Marker -> Level1 -> Level2 -> Level3
            AssertTrue(stream, resumeCount >= 4, $"Expected at least 4 ResumeStateMachineAsyncMethod events for ValueTask chain, got {resumeCount}");
            AssertTrue(stream, completeCount >= 4, $"Expected at least 4 CompleteStateMachineAsyncMethod events for ValueTask chain, got {completeCount}");
        }


        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_CallstackDepthMatchesChainDepth_Marker()
        {
            await StateMachineAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ValueTask_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ValueTask_CallstackDepthMatchesChainDepth_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ValueTask_CallstackDepthMatchesChainDepth_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Marker -> Level1 -> Level2 -> Level3
            AssertEqual(stream, 4, markerCallstacks[0].FrameCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_CallstackFramesHaveDistinctMethodIds_Marker()
        {
            await StateMachineAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ValueTask_CallstackFramesHaveDistinctMethodIds()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ValueTask_CallstackFramesHaveDistinctMethodIds_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ValueTask_CallstackFramesHaveDistinctMethodIds_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            AssertEqual(stream, methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete_InnerThrows_Marker()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("valuetask inner throw");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Handled_Marker()
        {
            try
            {
                await StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete_InnerThrows_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Marker()
        {
            await StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Handled_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int unwindIdx = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx > resumeIdx, "Expected UnwindStateMachineAsyncException after Resume");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, unwindIdx + 1);
            AssertTrue(stream, completeIdx > unwindIdx, "Expected CompleteStateMachineAsyncContext after Unwind");
        }


        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledOuter_Marker()
        {
            await StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledInner_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledInner_Marker()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("valuetask unhandled inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_Marker()
        {
            await StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledOuter_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_Marker().AsTask());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int unwindIdx1 = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx1 > resumeIdx, "Expected first UnwindStateMachineAsyncException after Resume");

            int unwindIdx2 = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, unwindIdx1 + 1);
            AssertTrue(stream, unwindIdx2 > unwindIdx1, "Expected second UnwindStateMachineAsyncException after first Unwind");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, unwindIdx2 + 1);
            AssertTrue(stream, completeIdx > unwindIdx2, "Expected CompleteStateMachineAsyncContext after second Unwind");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_UsesPoolingBox()
        {
            ValueTask pending = StateMachineAsync_PoolingValueTask_Level3();

            try
            {
                Assert.False(pending.IsCompleted, "Expected a pending ValueTask so a pooling box was rented");

                object? backing = typeof(ValueTask).GetField("_obj", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pending);
                Assert.True(backing is IValueTaskSource, $"Expected the pending ValueTask to be backed by an IValueTaskSource pooling box, got {backing?.GetType().Name ?? "null"}");
                Assert.False(backing is Task, "A pending ValueTask backed by a Task means the default (non-pooling) builder was used");
            }
            finally
            {
                pending.AsTask().GetAwaiter().GetResult();
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_EventSequenceOrder_Marker()
        {
            await StateMachineAsync_PoolingValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_EventSequenceOrder()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_EventSequenceOrder_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_EventSequenceOrder_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteStateMachineAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_MethodEventsEmitted_Marker()
        {
            await StateMachineAsync_PoolingValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_MethodEventsEmitted()
        {
            var events = CollectEvents(StateMachineAsyncMethodKeywords | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_MethodEventsEmitted_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeStateMachineAsyncMethod or AsyncEventID.CompleteStateMachineAsyncMethod)
                .Select(e => e.EventId)
                .ToList();

            int resumeCount = methodEvents.Count(id => id == AsyncEventID.ResumeStateMachineAsyncMethod);
            int completeCount = methodEvents.Count(id => id == AsyncEventID.CompleteStateMachineAsyncMethod);

            // Marker -> Level1 -> Level2 -> Level3
            AssertTrue(stream, resumeCount >= 4, $"Expected at least 4 ResumeStateMachineAsyncMethod events for pooling ValueTask chain, got {resumeCount}");
            AssertTrue(stream, completeCount >= 4, $"Expected at least 4 CompleteStateMachineAsyncMethod events for pooling ValueTask chain, got {completeCount}");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_SuspendResumeCompleteEvents_Marker()
        {
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_SuspendResumeCompleteEvents()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_SuspendResumeCompleteEvents_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_SuspendResumeCompleteEvents_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int resumeCount = ids.Count(id => id == AsyncEventID.ResumeStateMachineAsyncContext);
            int suspendCount = ids.Count(id => id == AsyncEventID.SuspendStateMachineAsyncContext);
            int completeCount = ids.Count(id => id == AsyncEventID.CompleteStateMachineAsyncContext);

            AssertTrue(stream, suspendCount >= 1, "Expected at least one SuspendStateMachineAsyncContext for the re-suspending pooling method");

            // Each resume ends in exactly one suspend (yielded) or one complete (finished).
            AssertEqual(stream, resumeCount, completeCount + suspendCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_CallstackDepthMatchesChainDepth_Marker()
        {
            await StateMachineAsync_PoolingValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_CallstackDepthMatchesChainDepth_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_CallstackDepthMatchesChainDepth_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Marker -> Level1 -> Level2 -> Level3.
            AssertEqual(stream, 4, markerCallstacks[0].FrameCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_CallstackFramesHaveDistinctMethodIds_Marker()
        {
            await StateMachineAsync_PoolingValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_CallstackFramesHaveDistinctMethodIds()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_CallstackFramesHaveDistinctMethodIds_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_CallstackFramesHaveDistinctMethodIds_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            AssertEqual(stream, methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete_InnerThrows_Marker()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("pooling valuetask inner throw");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete_Handled_Marker()
        {
            try
            {
                await StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete_InnerThrows_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete_Marker()
        {
            await StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete_Handled_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_HandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int unwindIdx = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx > resumeIdx, "Expected UnwindStateMachineAsyncException after Resume");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, unwindIdx + 1);
            AssertTrue(stream, completeIdx > unwindIdx, "Expected CompleteStateMachineAsyncContext after Unwind");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledOuter_Marker()
        {
            await StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledInner_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledInner_Marker()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("pooling valuetask unhandled inner");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete_Marker()
        {
            await StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete_UnhandledOuter_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | UnwindStateMachineAsyncExceptionKeyword, () =>
            {
                try
                {
                    RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete_Marker().AsTask());
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_UnhandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateStateMachineAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateStateMachineAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeStateMachineAsyncContext after Create");

            int unwindIdx1 = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx1 > resumeIdx, "Expected first UnwindStateMachineAsyncException after Resume");

            int unwindIdx2 = ids.IndexOf(AsyncEventID.UnwindStateMachineAsyncException, unwindIdx1 + 1);
            AssertTrue(stream, unwindIdx2 > unwindIdx1, "Expected second UnwindStateMachineAsyncException after first Unwind");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, unwindIdx2 + 1);
            AssertTrue(stream, completeIdx > unwindIdx2, "Expected CompleteStateMachineAsyncContext after second Unwind");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Leaf_Marker()
        {
            await Task.Delay(100).ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Mid_Marker()
        {
            await StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Leaf_Marker().ConfigureAwait(false);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Marker()
        {
            await StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Mid_Marker().ConfigureAwait(false);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var frameNames = markerCallstacks[0].Frames
                .Select(f => GetMethodNameFromMethodId(markerCallstacks[0].CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();

            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Leaf_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Mid_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Marker), frameNames);

            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(StateMachineAsync_PoolingValueTask_ConfigureAwaitFalse_Marker));
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private static async ValueTask<int> StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Leaf_Marker()
        {
            await Task.Delay(100).ConfigureAwait(false);
            return 1;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private static async ValueTask<int> StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Mid_Marker()
        {
            return await StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Leaf_Marker().ConfigureAwait(false) + 1;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private static async ValueTask<int> StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Marker()
        {
            return await StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Mid_Marker().ConfigureAwait(false) + 1;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var frameNames = markerCallstacks[0].Frames
                .Select(f => GetMethodNameFromMethodId(markerCallstacks[0].CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();

            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Leaf_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Mid_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Marker), frameNames);

            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(StateMachineAsync_PoolingValueTaskOfT_ConfigureAwaitFalse_Marker));
        }

        private static SemaphoreSlim s_poolingAppendRace_proceed;

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_AppendCallstack_FiresOnLateParentRegistration_Child_Marker()
        {
            await Task.Yield();
            s_poolingAppendRace_proceed.Release();
            Thread.Sleep(200);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker()
        {
            ValueTask t = StateMachineAsync_PoolingValueTask_AppendCallstack_FiresOnLateParentRegistration_Child_Marker();
            Assert.True(s_poolingAppendRace_proceed.Wait(TimeSpan.FromSeconds(20)), "Timed out waiting for child to reach append-race checkpoint");
            await t;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_AppendCallstack_FiresOnLateParentRegistration()
        {
            s_poolingAppendRace_proceed = new SemaphoreSlim(0, 1);

            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTask_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var childOnlyResumes = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_AppendCallstack_FiresOnLateParentRegistration_Child_Marker));
            AssertNotEmpty(stream, childOnlyResumes);

            var appendsWithParent = stream.CallstacksWithMarker(AsyncEventID.AppendStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_AppendCallstack_FiresOnLateParentRegistration_Parent_Marker));
            AssertNotEmpty(stream, appendsWithParent);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private static async ValueTask<int> StateMachineAsync_PoolingValueTaskOfT_Level3()
        {
            await Task.Delay(100);
            return 1;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private static async ValueTask<int> StateMachineAsync_PoolingValueTaskOfT_Level2()
        {
            return await StateMachineAsync_PoolingValueTaskOfT_Level3() + 1;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private static async ValueTask<int> StateMachineAsync_PoolingValueTaskOfT_Level1()
        {
            return await StateMachineAsync_PoolingValueTaskOfT_Level2() + 1;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private static async ValueTask<int> StateMachineAsync_PoolingValueTaskOfT_CallstackDepthMatchesChainDepth_Marker()
        {
            return await StateMachineAsync_PoolingValueTaskOfT_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTaskOfT_CallstackDepthMatchesChainDepth()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_PoolingValueTaskOfT_CallstackDepthMatchesChainDepth_Marker().AsTask());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTaskOfT_CallstackDepthMatchesChainDepth_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Marker -> Level1 -> Level2 -> Level3.
            AssertEqual(stream, 4, markerCallstacks[0].FrameCount);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_InlineReentrantCompletion_Inner_Marker(Task innerGate)
        {
            await innerGate;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_InlineReentrantCompletion_Outer_Marker(
            TaskCompletionSource innerGate, StrongBox<int> resumedThreadId, Task resumeGate, Task finalGate)
        {
            await resumeGate;
            resumedThreadId.Value = Environment.CurrentManagedThreadId;
            innerGate.SetResult();
            await finalGate;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_PoolingValueTask_InlineReentrantCompletion()
        {
            var events = CollectEvents(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    var resumedThreadId = new StrongBox<int>();
                    var innerGate = new TaskCompletionSource();
                    var resumeGate = new TaskCompletionSource();
                    var finalGate = new TaskCompletionSource();

                    // Inner suspends on innerGate (pending pooling box + registered continuation).
                    ValueTask inner = StateMachineAsync_PoolingValueTask_InlineReentrantCompletion_Inner_Marker(innerGate.Task);

                    // Outer suspends on resumeGate.
                    ValueTask outer = StateMachineAsync_PoolingValueTask_InlineReentrantCompletion_Outer_Marker(
                        innerGate, resumedThreadId, resumeGate.Task, finalGate.Task);

                    // Resume Outer inline; during its resume it completes Inner inline (the re-entrant
                    // completion under test), then re-suspends on finalGate.
                    int setResultThreadId = Environment.CurrentManagedThreadId;
                    resumeGate.SetResult();

                    Assert.True(resumedThreadId.Value == setResultThreadId,
                        $"Expected inline resume on thread {setResultThreadId}, got {resumedThreadId.Value} (0 = no sync resume)");

                    finalGate.SetResult();
                    await outer;
                    await inner;
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(
                AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_InlineReentrantCompletion_Outer_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // The Outer frame resumes after resumeGate, fires Inner's inline completion, then RE-SUSPENDS on
            // finalGate before finally completing. With dispatcher reuse this is one dispatcher that Suspends
            // (interior) and later Completes. A mis-attributed inline completion of Inner would flip
            // CurrentContinuationCompleted on the Outer frame during its first resume, making it Complete
            // before (or instead of) that Suspend. So: Outer must re-suspend, and its Complete must come after.
            var markerDispatcherIds = markerCallstacks.Select(c => c.DispatcherId).Distinct().ToList();

            bool foundReSuspend = false;
            foreach (ulong id in markerDispatcherIds)
            {
                var ids = stream.All.Where(e => e.DispatcherId == id).OrderBy(e => e.Timestamp).Select(e => e.EventId).ToList();
                int resumeIdx = ids.IndexOf(AsyncEventID.ResumeStateMachineAsyncContext);
                if (resumeIdx < 0)
                {
                    continue;
                }

                int suspendIdx = ids.IndexOf(AsyncEventID.SuspendStateMachineAsyncContext, resumeIdx + 1);
                if (suspendIdx > resumeIdx)
                {
                    foundReSuspend = true;
                    int completeIdx = ids.IndexOf(AsyncEventID.CompleteStateMachineAsyncContext, resumeIdx + 1);
                    AssertTrue(stream, completeIdx > suspendIdx,
                        "Outer did not complete after it re-suspended; inline inner completion was mis-attributed to it");
                }
            }

            AssertTrue(stream, foundReSuspend, "Expected the Outer frame to re-suspend on finalGate (Suspend event)");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplaysPendingV1Chain_Inner_Marker()
        {
            using var dummy = new TestEventListener();
            dummy.AddSource(AsyncProfilerEventSourceName, EventLevel.Informational, EventKeywords.None);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplaysPendingV1Chain_Mid_Marker()
        {
            await StateMachineAsync_ResetContext_ReplaysPendingV1Chain_Inner_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplaysPendingV1Chain_Outer_Marker()
        {
            await StateMachineAsync_ResetContext_ReplaysPendingV1Chain_Mid_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ResetContext_ReplaysPendingV1Chain()
        {
            var events = CollectEvents(AllStateMachineAsyncKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ResetContext_ReplaysPendingV1Chain_Outer_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Locate the StateMachine dispatcher driving the marker chain via its ResumeStateMachineAsyncCallstack
            // events (which carry the marker frames in their callstack).
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, "StateMachineAsync_ResetContext_ReplaysPendingV1Chain");
            AssertNotEmpty(stream, markerCallstacks);

            // Thread-scoped replay assertion: at least one OS thread must have seen two
            // ResetAsyncThreadContext events AND show a marker ResumeStateMachineAsyncCallstack plus
            // matching ResumeStateMachineAsyncContext after its most recent reset. Multiple threads
            // in the trace can accumulate two resets (e.g. the test thread re-enables on
            // its initial event then again later), so we have to look at each candidate
            // thread, not just the first one. The thread that actually drove the replay
            // is the one the StateMachine continuation resumed onto.
            var resetsByThread = stream.All
                .Where(e => e.EventId == AsyncEventID.ResetAsyncThreadContext && e.OsThreadId != 0)
                .GroupBy(e => e.OsThreadId)
                .Where(g => g.Count() >= 2)
                .ToList();
            AssertNotEmpty(stream, resetsByThread);

            bool found = false;
            foreach (var threadResets in resetsByThread)
            {
                ulong threadId = threadResets.Key;
                long lastResetTimestamp = threadResets.Max(e => e.Timestamp);

                var postResetMarkerCallstacks = markerCallstacks
                    .Where(c => c.OsThreadId == threadId && c.Timestamp >= lastResetTimestamp)
                    .ToList();
                if (postResetMarkerCallstacks.Count == 0)
                {
                    continue;
                }

                var replayDispatcherIds = postResetMarkerCallstacks.Select(c => c.DispatcherId).ToHashSet();
                var postResetResumeContext = stream.All
                    .Where(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext
                                && e.OsThreadId == threadId
                                && e.Timestamp >= lastResetTimestamp
                                && replayDispatcherIds.Contains(e.DispatcherId))
                    .ToList();
                if (postResetResumeContext.Count == 0)
                {
                    continue;
                }

                found = true;
                break;
            }

            AssertTrue(stream, found,
                "Expected at least one OS thread with >= 2 ResetAsyncThreadContext events followed by a marker ResumeAsyncCallstack and matching ResumeAsyncContext.");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplaysMultipleDispatchers_Inner_Marker(
            Task gate,
            ManualResetEventSlim block,
            Action onBlocked,
            TaskCompletionSource innerDone)
        {
            await gate;
            onBlocked();
            block.Wait();
            innerDone.SetResult();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplaysMultipleDispatchers_Outer_Marker(TaskCompletionSource innerDone)
        {
            await innerDone.Task;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ResetContext_ReplaysMultipleDispatchers()
        {
            var events = CollectEvents(AllStateMachineAsyncKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var block = new ManualResetEventSlim(false);
                    var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var innerDone = new TaskCompletionSource();

                    Task inner = StateMachineAsync_ResetContext_ReplaysMultipleDispatchers_Inner_Marker(gate.Task, block, () => blocked.SetResult(), innerDone);
                    Task outer = StateMachineAsync_ResetContext_ReplaysMultipleDispatchers_Outer_Marker(innerDone);

                    _ = Task.Run(() => gate.SetResult());

                    await blocked.Task;

                    var dummy = new TestEventListener();
                    try
                    {
                        dummy.AddSource(AsyncProfilerEventSourceName, EventLevel.Informational, EventKeywords.None);

                        block.Set();

                        await outer;
                        await inner;
                    }
                    finally
                    {
                        dummy.Dispose();
                    }
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The replay must emit at least one ResumeAsyncCallstack carrying the StateMachine marker chain.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, "StateMachineAsync_ResetContext_ReplaysMultipleDispatchers");
            AssertNotEmpty(stream, markerCallstacks);

            // The decisive proof: find an OS thread where a single ResetAsyncThreadContext
            // event is followed by >= 2 ResumeAsyncContext events before the next reset
            // (or end of trace). A normal dispatcher resume can only emit one Resume
            // per MoveNext invocation; observing >= 2 in a single reset window proves
            // the walker traversed multiple nested dispatchers from a single ResetContext.
            var resetsByThread = stream.All
                .Where(e => e.EventId == AsyncEventID.ResetAsyncThreadContext && e.OsThreadId != 0)
                .GroupBy(e => e.OsThreadId)
                .ToList();
            AssertNotEmpty(stream, resetsByThread);

            bool found = false;
            foreach (var threadResets in resetsByThread)
            {
                ulong threadId = threadResets.Key;
                var orderedResets = threadResets.OrderBy(e => e.Timestamp).ToList();

                for (int i = 0; i < orderedResets.Count; i++)
                {
                    long windowStart = orderedResets[i].Timestamp;
                    long windowEnd = (i + 1 < orderedResets.Count)
                        ? orderedResets[i + 1].Timestamp
                        : long.MaxValue;

                    int resumeContextCount = stream.All
                        .Count(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncCallstack
                                    && e.OsThreadId == threadId
                                    && e.Timestamp >= windowStart
                                    && e.Timestamp < windowEnd);

                    if (resumeContextCount >= 2)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            AssertTrue(stream, found,
                "Expected at least one OS thread with a ResetAsyncThreadContext event " +
                "followed by >= 2 ResumeAsyncContext events in the same reset window, " +
                "proving the reset-replay walker traversed multiple nested dispatchers.");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplayResumeCompleteBalance_Inner_Marker()
        {
            using var dummy = new TestEventListener();
            dummy.AddSource(AsyncProfilerEventSourceName, EventLevel.Informational, EventKeywords.None);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplayResumeCompleteBalance_Mid_Marker()
        {
            await StateMachineAsync_ResetContext_ReplayResumeCompleteBalance_Inner_Marker();
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_ResetContext_ReplayResumeCompleteBalance_Outer_Marker()
        {
            await StateMachineAsync_ResetContext_ReplayResumeCompleteBalance_Mid_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncAndThreadingSupported))]
        public void StateMachineAsync_ResetContext_ReplayResumeCompleteBalance()
        {
            var events = CollectEvents(AllStateMachineAsyncKeywords, () =>
            {
                RunScenarioAndFlush(() => StateMachineAsync_ResetContext_ReplayResumeCompleteBalance_Outer_Marker());
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Locate the replayed marker ResumeAsyncCallstack: the reset (triggered by the dummy
            // listener inside the inner marker) replays the suspended StateMachine chain, producing a
            // callstack carrying the marker frames.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, "StateMachineAsync_ResetContext_ReplayResumeCompleteBalance");
            AssertNotEmpty(stream, markerCallstacks);

            // Resume/Complete balance across the replay boundary. StateMachine uses a per-MoveNext dispatcher
            // model, so the marker chain spans a dispatcher tree whose deepest replayed callstack
            // has N frames; each of those N resumed async methods must eventually complete. Balance
            // is NOT preserved per reset epoch by design (a Resume can land in one epoch and its
            // Complete in a later epoch once a config change bumps the revision mid-chain), so the
            // count is reconstructed over the whole trace, scoped to the marker dispatcher's chain.
            // Using >= keeps the assertion robust against additional method events from the harness.
            var deepest = markerCallstacks.OrderByDescending(c => c.FrameCount).First();
            ulong leafDispatcherId = deepest.DispatcherId;

            int completeMethodCount = stream.ChainEventsFromDispatcher(leafDispatcherId)
                .Count(e => e.EventId == AsyncEventID.CompleteStateMachineAsyncMethod);

            AssertTrue(stream, completeMethodCount >= deepest.FrameCount,
                $"Expected at least {deepest.FrameCount} CompleteStateMachineAsyncMethod events across the " +
                $"trace for marker dispatcher chain {leafDispatcherId} to balance the replayed callstack of " +
                $"depth {deepest.FrameCount}, but found {completeMethodCount}.");
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_SingleThread_ChainEventsAndCallstack_Inner_Marker(Task gate)
        {
            await gate;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_SingleThread_ChainEventsAndCallstack_Mid_Marker(Task gate)
        {
            await StateMachineAsync_SingleThread_ChainEventsAndCallstack_Inner_Marker(gate);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task StateMachineAsync_SingleThread_ChainEventsAndCallstack_Marker(Task gate)
        {
            await StateMachineAsync_SingleThread_ChainEventsAndCallstack_Mid_Marker(gate);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncSupported))]
        public async Task StateMachineAsync_SingleThread_ChainEventsAndCallstack()
        {
            var events = await CollectEventsAsync(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | StateMachineAsyncMethodKeywords, async () =>
            {
                var tcs = new TaskCompletionSource();
                Task chain = StateMachineAsync_SingleThread_ChainEventsAndCallstack_Marker(tcs.Task);
                tcs.SetResult();
                await chain;
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_SingleThread_ChainEventsAndCallstack_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            var frameNames = deepest.Frames
                .Select(f => GetMethodNameFromMethodId(deepest.CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();
            AssertContains(stream, nameof(StateMachineAsync_SingleThread_ChainEventsAndCallstack_Inner_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_SingleThread_ChainEventsAndCallstack_Mid_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_SingleThread_ChainEventsAndCallstack_Marker), frameNames);

            foreach (ulong dispatcherId in markerCallstacks.Select(c => c.DispatcherId).Distinct())
            {
                var dispatcherEvents = stream.All
                    .Where(e => e.DispatcherId == dispatcherId)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                ParsedEvent? createEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.CreateStateMachineAsyncContext);
                ParsedEvent? resumeEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext);
                ParsedEvent? completeEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.CompleteStateMachineAsyncContext);

                if (createEvt is not null && resumeEvt is not null)
                {
                    AssertTrue(stream, createEvt.Timestamp <= resumeEvt.Timestamp,
                        $"Dispatcher {dispatcherId}: CreateStateMachineAsyncContext must precede ResumeStateMachineAsyncContext.");
                }

                if (resumeEvt is not null && completeEvt is not null)
                {
                    AssertTrue(stream, resumeEvt.Timestamp <= completeEvt.Timestamp,
                        $"Dispatcher {dispatcherId}: CompleteStateMachineAsyncContext must follow ResumeStateMachineAsyncContext.");
                }
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Inner_Marker(Task gate)
        {
            await gate;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Mid_Marker(Task gate)
        {
            await StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Inner_Marker(gate);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async ValueTask StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Marker(Task gate)
        {
            await StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Mid_Marker(gate);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncSupported))]
        public async Task StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack()
        {
            var events = await CollectEventsAsync(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | StateMachineAsyncMethodKeywords, async () =>
            {
                var tcs = new TaskCompletionSource();
                ValueTask chain = StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Marker(tcs.Task);
                tcs.SetResult();
                await chain;
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            var frameNames = deepest.Frames
                .Select(f => GetMethodNameFromMethodId(deepest.CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();
            AssertContains(stream, nameof(StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Inner_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Mid_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_ValueTask_SingleThread_ChainEventsAndCallstack_Marker), frameNames);

            foreach (ulong dispatcherId in markerCallstacks.Select(c => c.DispatcherId).Distinct())
            {
                var dispatcherEvents = stream.All
                    .Where(e => e.DispatcherId == dispatcherId)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                ParsedEvent? createEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.CreateStateMachineAsyncContext);
                ParsedEvent? resumeEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext);
                ParsedEvent? completeEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.CompleteStateMachineAsyncContext);

                if (createEvt is not null && resumeEvt is not null)
                {
                    AssertTrue(stream, createEvt.Timestamp <= resumeEvt.Timestamp,
                        $"Dispatcher {dispatcherId}: CreateStateMachineAsyncContext must precede ResumeStateMachineAsyncContext.");
                }

                if (resumeEvt is not null && completeEvt is not null)
                {
                    AssertTrue(stream, resumeEvt.Timestamp <= completeEvt.Timestamp,
                        $"Dispatcher {dispatcherId}: CompleteStateMachineAsyncContext must follow ResumeStateMachineAsyncContext.");
                }
            }
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Inner_Marker(Task gate)
        {
            await gate;
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Mid_Marker(Task gate)
        {
            await StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Inner_Marker(gate);
        }

        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private static async ValueTask StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Marker(Task gate)
        {
            await StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Mid_Marker(gate);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsStateMachineAsyncSupported))]
        public async Task StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack()
        {
            var events = await CollectEventsAsync(ResumeStateMachineAsyncCallstackKeyword | StateMachineAsyncCoreKeywords | StateMachineAsyncMethodKeywords, async () =>
            {
                var tcs = new TaskCompletionSource();
                ValueTask chain = StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Marker(tcs.Task);
                tcs.SetResult();
                await chain;
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, nameof(StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            var frameNames = deepest.Frames
                .Select(f => GetMethodNameFromMethodId(deepest.CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();
            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Inner_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Mid_Marker), frameNames);
            AssertContains(stream, nameof(StateMachineAsync_PoolingValueTask_SingleThread_ChainEventsAndCallstack_Marker), frameNames);

            foreach (ulong dispatcherId in markerCallstacks.Select(c => c.DispatcherId).Distinct())
            {
                var dispatcherEvents = stream.All
                    .Where(e => e.DispatcherId == dispatcherId)
                    .OrderBy(e => e.Timestamp)
                    .ToList();

                ParsedEvent? createEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.CreateStateMachineAsyncContext);
                ParsedEvent? resumeEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext);
                ParsedEvent? completeEvt = dispatcherEvents.FirstOrDefault(e => e.EventId == AsyncEventID.CompleteStateMachineAsyncContext);

                if (createEvt is not null && resumeEvt is not null)
                {
                    AssertTrue(stream, createEvt.Timestamp <= resumeEvt.Timestamp,
                        $"Dispatcher {dispatcherId}: CreateStateMachineAsyncContext must precede ResumeStateMachineAsyncContext.");
                }

                if (resumeEvt is not null && completeEvt is not null)
                {
                    AssertTrue(stream, resumeEvt.Timestamp <= completeEvt.Timestamp,
                        $"Dispatcher {dispatcherId}: CompleteStateMachineAsyncContext must follow ResumeStateMachineAsyncContext.");
                }
            }
        }
    }
}
