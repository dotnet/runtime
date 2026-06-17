// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    // Tests for Runtime (runtime-async) async profiler event emission. All scenario methods use
    // [RuntimeAsyncMethodGeneration(true)] to ensure they exercise the runtime-async path.
    // Runtime emits Create/Resume/Suspend/Complete callstacks natively from the runtime dispatch
    // loop, unlike StateMachine which uses the AsyncStateMachineBox dispatcher wrapper.
    public partial class AsyncProfilerTests
    {
        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_SingleYield()
        {
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ChainedYield()
        {
            await RuntimeAsync_InnerYield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_InnerYield()
        {
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepChain_Level3()
        {
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepChain_Level2()
        {
            await RuntimeAsync_DeepChain_Level3();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepChain_Level1()
        {
            await RuntimeAsync_DeepChain_Level2();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepChain()
        {
            await RuntimeAsync_DeepChain_Level1();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_OuterCatches()
        {
            try
            {
                await RuntimeAsync_InnerThrows();
            }
            catch (InvalidOperationException)
            {
            }
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_InnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("inner");
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepOuterCatches()
        {
            try
            {
                await RuntimeAsync_DeepMiddle();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepMiddle()
        {
            await RuntimeAsync_DeepInnerThrows();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepInnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("deep inner");
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepUnhandledOuter()
        {
            await RuntimeAsync_DeepUnhandledMiddle();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepUnhandledMiddle()
        {
            await RuntimeAsync_DeepUnhandledInnerThrows();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_DeepUnhandledInnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("deep unhandled");
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_RecursiveChain(int depth)
        {
            if (depth <= 1)
            {
                await Task.Yield();
                return;
            }
            await RuntimeAsync_RecursiveChain(depth - 1);
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WrapperTestA(List<(string MethodName, int WrapperSlot)> captures)
        {
            await RuntimeAsync_WrapperTestB(captures);
            captures.Add((nameof(RuntimeAsync_WrapperTestA), GetCurrentWrapperSlot(nameof(RuntimeAsync_WrapperTestA))));
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WrapperTestB(List<(string MethodName, int WrapperSlot)> captures)
        {
            await RuntimeAsync_WrapperTestC(captures);
            captures.Add((nameof(RuntimeAsync_WrapperTestB), GetCurrentWrapperSlot(nameof(RuntimeAsync_WrapperTestB))));
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WrapperTestC(List<(string MethodName, int WrapperSlot)> captures)
        {
            await Task.Yield();
            captures.Add((nameof(RuntimeAsync_WrapperTestC), GetCurrentWrapperSlot(nameof(RuntimeAsync_WrapperTestC))));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_Level1()
        {
            await RuntimeAsync_ValueTask_Level2();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_Level2()
        {
            await RuntimeAsync_ValueTask_Level3();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_Level3()
        {
            await Task.Yield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_EventBufferHeaderFormat()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCoreKeywords, RuntimeAsync_SingleYield);

            // DumpAllEvents(events);

            int buffersChecked = 0;
            ForEachEventBufferPayload(events, buffer =>
            {
                EventBufferHeader? parsed = ParseEventBufferHeader(buffer);
                AssertNotNull(events, parsed);
                EventBufferHeader header = parsed.Value;

                AssertEqual(events, 1, header.Version);
                AssertEqual(events, (uint)buffer.Length, header.TotalSize);
                AssertTrue(events, header.AsyncThreadContextId > 0, "Async thread context ID should be positive");
                AssertTrue(events, header.OsThreadId != 0, "OS thread ID should be non-zero");
                AssertTrue(events, header.StartTimestamp > 0, "Start timestamp should be positive");
                AssertTrue(events, header.EndTimestamp >= header.StartTimestamp, $"End timestamp ({header.EndTimestamp}) should be >= start timestamp ({header.StartTimestamp})");

                int eventCount = 0;
                ParseEventBuffer(buffer, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    eventCount++;
                    return SkipEventPayload(eventId, buf, ref idx);
                });

                AssertEqual(events, header.EventCount, (uint)eventCount);
                AssertTrue(events, header.EventCount > 0, "Expected at least one event in buffer");

                buffersChecked++;
            });

            AssertTrue(events, buffersChecked > 0, "Expected at least one buffer");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_EventsEmitted()
        {
            var events = await CollectEventsAsync(AllRuntimeAsyncKeywords, RuntimeAsync_SingleYield);

            // DumpAllEvents(events);

            AssertTrue(events, events.Events.Count > 0, "Expected at least one AsyncEvents event to be emitted");
            AssertContains(events, events.Events, e => e.EventId == AsyncEventsId);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_SuspendResumeCompleteEvents_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_SingleYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendResumeCompleteEvents()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_SuspendResumeCompleteEvents_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find our context via marker callstack.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_SuspendResumeCompleteEvents_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var taskEvts = stream.ChainEventsFromDispatcher(dispatcherId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            AssertContains(stream, AsyncEventID.ResumeRuntimeAsyncContext, ids);
            AssertContains(stream, AsyncEventID.SuspendRuntimeAsyncContext, ids);
            AssertContains(stream, AsyncEventID.CompleteRuntimeAsyncContext, ids);
        }


        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ContextEventIdLifecycle_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_SingleYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ContextEventIdLifecycle()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_ContextEventIdLifecycle_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find events in the context that contains our marker method.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_ContextEventIdLifecycle_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            AssertTrue(stream, dispatcherId > 0, "DispatcherId should be non-zero");

            var taskEvts = stream.ChainEventsFromDispatcher(dispatcherId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateRuntimeAsyncContext);
            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateAsyncContext in context events");
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeAsyncContext after CreateAsyncContext");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ResumeCompleteMethodEvents()
        {
            var events = await CollectEventsAsync(RuntimeAsyncMethodKeywords, RuntimeAsync_ChainedYield);

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            AssertContains(events, AsyncEventID.ResumeRuntimeAsyncMethod, ids);
            AssertContains(events, AsyncEventID.CompleteRuntimeAsyncMethod, ids);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_EventSequenceOrder_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_SingleYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_EventSequenceOrder()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_EventSequenceOrder_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find our context via marker callstack.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_EventSequenceOrder_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var taskEvts = stream.ChainEventsFromDispatcher(dispatcherId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            // Verify the expected lifecycle sequence exists in order.
            int resumeIdx1 = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext);
            AssertTrue(stream, resumeIdx1 >= 0, "Expected first ResumeAsyncContext");

            int suspendIdx = ids.IndexOf(AsyncEventID.SuspendRuntimeAsyncContext, resumeIdx1 + 1);
            AssertTrue(stream, suspendIdx > resumeIdx1, "Expected SuspendAsyncContext after first Resume");

            int resumeIdx2 = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext, suspendIdx + 1);
            AssertTrue(stream, resumeIdx2 > suspendIdx, "Expected second ResumeAsyncContext after Suspend");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext, resumeIdx2 + 1);
            AssertTrue(stream, completeIdx > resumeIdx2, "Expected CompleteAsyncContext after second Resume");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateAsyncContextEmittedOnFirstAwait()
        {
            var events = await CollectEventsAsync(CreateRuntimeAsyncContextKeyword | CompleteRuntimeAsyncContextKeyword, RuntimeAsync_SingleYield);

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;
            AssertContains(events, AsyncEventID.CreateRuntimeAsyncContext, ids);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CreateAsyncCallstackEmittedOnFirstAwait_Marker()
        {
            await RuntimeAsync_SingleYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateAsyncCallstackEmittedOnFirstAwait()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CreateAsyncCallstackEmittedOnFirstAwait_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createCallstacks = stream.CallstacksWithMarker(AsyncEventID.CreateRuntimeAsyncCallstack, nameof(RuntimeAsync_CreateAsyncCallstackEmittedOnFirstAwait_Marker));

            AssertNotEmpty(stream, createCallstacks);
            AssertAll(stream, createCallstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in create callstack");
                Assert.True(cs.DispatcherId != 0, "Expected non-zero dispatcher ID in create callstack");
                Assert.True(cs.Frames[0].MethodId != 0, "Expected non-zero MethodId in first frame");
            });
        }


        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CreateCallstackDepthMatchesChain_Marker()
        {
            await RuntimeAsync_ChainedYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateCallstackDepthMatchesChain()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CreateCallstackDepthMatchesChain_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createCallstacks = stream.CallstacksWithMarker(AsyncEventID.CreateRuntimeAsyncCallstack, nameof(RuntimeAsync_CreateCallstackDepthMatchesChain_Marker));

            // The expected [NoInlining] frames in order (innermost first):
            // RuntimeAsync_InnerYield -> RuntimeAsync_ChainedYield -> RuntimeAsync_CreateCallstackDepthMatchesChain_Marker
            AssertNotEmpty(stream, createCallstacks);
            string[] expectedFrames = [nameof(RuntimeAsync_InnerYield), nameof(RuntimeAsync_ChainedYield), nameof(RuntimeAsync_CreateCallstackDepthMatchesChain_Marker)];
            AssertTrue(
                stream,
                HasCallstackWithExpectedFrames(createCallstacks, expectedFrames),
                $"Expected callstack to contain frames [{string.Join(", ", expectedFrames)}] in order");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_SuspendAsyncCallstackEmittedOnAwait_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_SingleYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendAsyncCallstackEmittedOnAwait()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_SuspendAsyncCallstackEmittedOnAwait_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var suspendCallstacks = stream.CallstacksWithMarker(AsyncEventID.SuspendRuntimeAsyncCallstack, nameof(RuntimeAsync_SuspendAsyncCallstackEmittedOnAwait_Marker));

            AssertNotEmpty(stream, suspendCallstacks);
            AssertAll(stream, suspendCallstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in suspend callstack");
                Assert.True(cs.DispatcherId != 0, "Expected non-zero dispatcher ID in suspend callstack");
                Assert.True(cs.Frames[0].MethodId != 0, "Expected non-zero MethodId in first frame");
            });
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_SuspendCallstackDepthMatchesChain_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_ChainedYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendCallstackDepthMatchesChain()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_SuspendCallstackDepthMatchesChain_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var suspendCallstacks = stream.CallstacksWithMarker(AsyncEventID.SuspendRuntimeAsyncCallstack, nameof(RuntimeAsync_SuspendCallstackDepthMatchesChain_Marker));

            // The expected [NoInlining] frames in order (innermost first):
            // RuntimeAsync_InnerYield -> RuntimeAsync_ChainedYield -> RuntimeAsync_SuspendCallstackDepthMatchesChain_Marker
            AssertNotEmpty(stream, suspendCallstacks);
            string[] expectedFrames = [nameof(RuntimeAsync_InnerYield), nameof(RuntimeAsync_ChainedYield), nameof(RuntimeAsync_SuspendCallstackDepthMatchesChain_Marker)];
            AssertTrue(
                stream,
                HasCallstackWithExpectedFrames(suspendCallstacks, expectedFrames),
                $"Expected callstack to contain frames [{string.Join(", ", expectedFrames)}] in order");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_SuspendCallstackPrecedesComplete_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_InnerYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendCallstackPrecedesComplete()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_SuspendCallstackPrecedesComplete_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find the suspend callstack via marker to get the context ID
            var suspendStacks = stream.CallstacksWithMarker(AsyncEventID.SuspendRuntimeAsyncCallstack, nameof(RuntimeAsync_SuspendCallstackPrecedesComplete_Marker));
            AssertNotEmpty(stream, suspendStacks);

            ulong dispatcherId = suspendStacks[0].DispatcherId;
            AssertTrue(stream, dispatcherId > 0, "Expected non-zero dispatcher ID");

            var taskEvts = stream.ChainEventsFromDispatcher(dispatcherId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            int suspendIdx = ids.IndexOf(AsyncEventID.SuspendRuntimeAsyncCallstack);
            int completeIdx = ids.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext);

            AssertTrue(stream, suspendIdx >= 0, "Expected SuspendAsyncCallstack in context events");
            AssertTrue(stream, completeIdx >= 0, "Expected CompleteAsyncContext in context events");
            AssertTrue(stream, suspendIdx < completeIdx, $"SuspendAsyncCallstack (index {suspendIdx}) should precede CompleteAsyncContext (index {completeIdx})");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_SuspendCallstackDeeperThanInitialResume_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_InnerYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendCallstackDeeperThanInitialResume()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_SuspendCallstackDeeperThanInitialResume_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_SuspendCallstackDeeperThanInitialResume_Marker));
            var suspendStacks = stream.CallstacksWithMarker(AsyncEventID.SuspendRuntimeAsyncCallstack, nameof(RuntimeAsync_SuspendCallstackDeeperThanInitialResume_Marker));

            AssertNotEmpty(stream, resumeStacks);
            AssertNotEmpty(stream, suspendStacks);

            // First resume (after initial Yield) should be shallow, first suspend (RuntimeAsync_InnerYield's Yield) should be deeper
            var firstResume = resumeStacks[0];
            var firstSuspend = suspendStacks[0];

            AssertTrue(stream, firstSuspend.FrameCount > firstResume.FrameCount, $"First suspend callstack depth ({firstSuspend.FrameCount}) should be deeper than first resume callstack depth ({firstResume.FrameCount})");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CreateCallstackPrecedesResumeCallstack_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_InnerYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateCallstackPrecedesResumeCallstack()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CreateCallstackPrecedesResumeCallstack_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createStacks = stream.CallstacksWithMarker(AsyncEventID.CreateRuntimeAsyncCallstack, nameof(RuntimeAsync_CreateCallstackPrecedesResumeCallstack_Marker));
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CreateCallstackPrecedesResumeCallstack_Marker));

            AssertNotEmpty(stream, createStacks);
            AssertNotEmpty(stream, resumeStacks);

            // For each task that has both Create and Resume callstacks, verify Create timestamp precedes Resume.
            int matchedPairs = 0;
            foreach (var create in createStacks)
            {
                var matchingResume = resumeStacks.FirstOrDefault(r => r.DispatcherId == create.DispatcherId);
                if (matchingResume is null)
                    continue;

                matchedPairs++;
                AssertTrue(stream, create.Timestamp <= matchingResume.Timestamp, $"For dispatcher {create.DispatcherId}: CreateAsyncCallstack (ts {create.Timestamp}) should precede ResumeAsyncCallstack (ts {matchingResume.Timestamp})");
            }

            AssertTrue(stream, matchedPairs >= 1, $"Expected at least one matching Create/Resume callstack pair, but found {matchedPairs}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CreateAndFirstResumeCallstacksMatch_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_InnerYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateAndFirstResumeCallstacksMatch()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CreateAndFirstResumeCallstacksMatch_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createStacks = stream.CallstacksWithMarker(AsyncEventID.CreateRuntimeAsyncCallstack, nameof(RuntimeAsync_CreateAndFirstResumeCallstacksMatch_Marker));
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CreateAndFirstResumeCallstacksMatch_Marker));

            AssertNotEmpty(stream, createStacks);
            AssertNotEmpty(stream, resumeStacks);

            // For each create callstack, find the first resume with the same dispatcher ID and verify frames match.
            int matchedPairs = 0;
            foreach (var create in createStacks)
            {
                var matchingResume = resumeStacks.FirstOrDefault(r => r.DispatcherId == create.DispatcherId);
                if (matchingResume is null)
                    continue;

                matchedPairs++;
                AssertEqual(stream, create.Frames.Count, matchingResume.Frames.Count);
                for (int i = 0; i < create.Frames.Count; i++)
                {
                    AssertEqual(stream, create.Frames[i].MethodId, matchingResume.Frames[i].MethodId);
                }
            }

            AssertTrue(stream, matchedPairs >= 1, $"Expected at least one matching Create/Resume callstack pair, but found {matchedPairs}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackEmittedOnResume_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_InnerYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackEmittedOnResume()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CallstackEmittedOnResume_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CallstackEmittedOnResume_Marker));

            AssertNotEmpty(stream, callstacks);
            AssertAll(stream, callstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in callstack");
                Assert.True(cs.DispatcherId != 0, "Expected non-zero dispatcher ID in resume callstack");
                Assert.True(cs.Frames[0].MethodId != 0, "Expected non-zero MethodId in first frame");
            });
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackDepthMatchesChain_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_InnerYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackDepthMatchesChain()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CallstackDepthMatchesChain_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CallstackDepthMatchesChain_Marker));

            // The expected [NoInlining] frames in order (innermost first):
            // RuntimeAsync_InnerYield -> RuntimeAsync_CallstackDepthMatchesChain_Marker
            AssertNotEmpty(stream, callstacks);
            string[] expectedFrames = [nameof(RuntimeAsync_InnerYield), nameof(RuntimeAsync_CallstackDepthMatchesChain_Marker)];
            AssertTrue(
                stream,
                HasCallstackWithExpectedFrames(callstacks, expectedFrames),
                $"Expected callstack to contain frames [{string.Join(", ", expectedFrames)}] in order");
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_MethodEventCountMatchesChainDepth_Marker()
        {
            await RuntimeAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_MethodEventCountMatchesChainDepth()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords | RuntimeAsyncMethodKeywords, RuntimeAsync_MethodEventCountMatchesChainDepth_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Marker -> DeepChain -> Level1 -> Level2 -> Level3
            const int ExpectedChainDepth = 5;

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_MethodEventCountMatchesChainDepth_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            AssertEqual(stream, ExpectedChainDepth, markerCallstacks[0].Frames.Count);

            ulong chainDispatcherId = markerCallstacks[0].DispatcherId;
            var chainEvents = stream.ChainEventsFromDispatcher(chainDispatcherId);

            int resumeCount = chainEvents.Count(e => e.EventId == AsyncEventID.ResumeRuntimeAsyncMethod);
            AssertEqual(stream, ExpectedChainDepth, resumeCount);

            int completeCount = chainEvents.Count(e => e.EventId == AsyncEventID.CompleteRuntimeAsyncMethod);
            AssertEqual(stream, ExpectedChainDepth, completeCount);
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackFramesHaveDistinctMethodIds_Marker()
        {
            await RuntimeAsync_DeepChain();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackFramesHaveDistinctMethodIds()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CallstackFramesHaveDistinctMethodIds_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CallstackFramesHaveDistinctMethodIds_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Frames in the same callstack should have distinct methodIds (one per async method in the chain).
            var deepest = markerCallstacks.MaxBy(cs => cs.FrameCount)!;
            var methodIds = deepest.Frames.Select(f => f.MethodId).ToList();
            AssertEqual(stream, methodIds.Count, methodIds.Distinct().Count());
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Level3()
        {
            await Task.Delay(100);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Level2()
        {
            await RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Level3();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Level1()
        {
            await RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Level2();
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Marker()
        {
            await RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_YieldAtEachLevel_CallstackShrinks()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_YieldAtEachLevel_CallstackShrinks_Marker));

            // After Task.Delay resumes: full chain (Level3, Level2, Level1, Marker) = 4 frames
            // After Level3's yield resumes: Level3 completes, chain is (Level2, Level1, Marker) = 3 frames
            // After Level2's yield resumes: Level2 completes, chain is (Level1, Marker) = 2 frames
            AssertContains(stream, markerCallstacks, cs => cs.FrameCount == 4);
            AssertContains(stream, markerCallstacks, cs => cs.FrameCount == 3);
            AssertContains(stream, markerCallstacks, cs => cs.FrameCount == 2);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackSimulation_NormalCompletion_Marker()
        {
            await Task.Yield();
            await RuntimeAsync_InnerYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackSimulation_NormalCompletion()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CallstackSimulation_NormalCompletion_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            AssertCallstackSimulationReachesZero(stream, nameof(RuntimeAsync_CallstackSimulation_NormalCompletion_Marker));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackSimulation_HandledException_Marker()
        {
            await RuntimeAsync_DeepOuterCatches();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackSimulation_HandledException()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CallstackSimulation_HandledException_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            AssertCallstackSimulationReachesZero(stream, nameof(RuntimeAsync_CallstackSimulation_HandledException_Marker));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackSimulation_UnhandledException_Marker()
        {
            await RuntimeAsync_DeepUnhandledOuter();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackSimulation_UnhandledException_Catcher_Marker()
        {
            try
            {
                await RuntimeAsync_CallstackSimulation_UnhandledException_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackSimulation_UnhandledException()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CallstackSimulation_UnhandledException_Catcher_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            AssertCallstackSimulationReachesZero(stream, nameof(RuntimeAsync_CallstackSimulation_UnhandledException_Marker));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_UnhandledExceptionUnwind_Marker()
        {
            await RuntimeAsync_DeepUnhandledOuter();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_UnhandledExceptionUnwind_Catcher_Marker()
        {
            try
            {
                await RuntimeAsync_UnhandledExceptionUnwind_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_UnhandledExceptionUnwind()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_UnhandledExceptionUnwind_Catcher_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_UnhandledExceptionUnwind_Marker));
            AssertNotEmpty(stream, resumeStacks);

            ulong dispatcherId = resumeStacks[0].DispatcherId;

            var taskEvts = stream.ChainEventsFromDispatcher(dispatcherId);
            var eventIds = taskEvts.Select(e => e.EventId).ToList();

            AssertContains(stream, AsyncEventID.ResumeRuntimeAsyncContext, eventIds);
            AssertContains(stream, AsyncEventID.UnwindRuntimeAsyncException, eventIds);
            AssertContains(stream, AsyncEventID.CompleteRuntimeAsyncContext, eventIds);

            // Verify unwind frame count for this task
            // Marker -> RuntimeAsync_DeepUnhandledOuter -> RuntimeAsync_DeepUnhandledMiddle -> RuntimeAsync_DeepUnhandledInnerThrows, 4 frames deep after the initial resume.
            var unwindEvents = taskEvts.Where(e => e.EventId == AsyncEventID.UnwindRuntimeAsyncException).ToList();
            AssertNotEmpty(stream, unwindEvents);
            AssertAll(stream, unwindEvents, e => Assert.Equal(4u, e.UnwindFrameCount));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_HandledExceptionUnwind_Marker()
        {
            await RuntimeAsync_DeepOuterCatches();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_HandledExceptionUnwind()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_HandledExceptionUnwind_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_HandledExceptionUnwind_Marker));
            AssertNotEmpty(stream, resumeStacks);

            ulong dispatcherId = resumeStacks[0].DispatcherId;

            var taskEvts = stream.ChainEventsFromDispatcher(dispatcherId);
            var eventIds = taskEvts.Select(e => e.EventId).ToList();

            AssertContains(stream, AsyncEventID.ResumeRuntimeAsyncContext, eventIds);
            AssertContains(stream, AsyncEventID.UnwindRuntimeAsyncException, eventIds);
            AssertContains(stream, AsyncEventID.CompleteRuntimeAsyncContext, eventIds);

            // Verify unwind frame count for this task
            // RuntimeAsync_DeepMiddle -> RuntimeAsync_DeepInnerThrows, 2 frames deep after the initial resume.
            var unwindEvents = taskEvts.Where(e => e.EventId == AsyncEventID.UnwindRuntimeAsyncException).ToList();
            AssertNotEmpty(stream, unwindEvents);
            AssertAll(stream, unwindEvents, e => Assert.Equal(2u, e.UnwindFrameCount));
        }

        // Requires threading:
        // Wrapper index tests use RunScenarioAndFlush (Task.Run) to escape xunit's
        // AsyncTestSyncContext. With a SynchronizationContext present, each level in the
        // async chain re-dispatches through the sync context, creating a separate
        // DispatchContinuations call that resets the wrapper index to 0. Task.Run ensures
        // the entire continuation chain executes in a single dispatch loop where the
        // wrapper index increments sequentially across resumptions.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexMatchesCallstack()
        {
            var captures = new List<(string MethodName, int WrapperSlot)>();

            var events = CollectEvents(ResumeRuntimeAsyncCallstackKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await RuntimeAsync_WrapperTestA(captures);
                });
            });

            // DumpAllEvents(events);

            AssertTrue(events, captures.Count == 3, $"Expected 3 wrapper captures, got {captures.Count}");

            AssertAll(events, captures, c => Assert.True(c.WrapperSlot >= 0, $"{c.MethodName} did not find wrapper frame on stack (slot={c.WrapperSlot})"));

            int slotC = captures.First(c => c.MethodName == nameof(RuntimeAsync_WrapperTestC)).WrapperSlot;
            int slotB = captures.First(c => c.MethodName == nameof(RuntimeAsync_WrapperTestB)).WrapperSlot;
            int slotA = captures.First(c => c.MethodName == nameof(RuntimeAsync_WrapperTestA)).WrapperSlot;

            AssertEqual(events, slotC + 1, slotB);
            AssertEqual(events, slotB + 1, slotA);
        }

        // Requires threading:
        // Same comment as RuntimeAsync_WrapperIndexMatchesCallstack regarding Task.Run and wrapper index behavior.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexResetEmitted()
        {
            var events = CollectEvents(AllRuntimeAsyncKeywords, () =>
            {
                // Recursive chain 34 levels deep crosses the 32-slot boundary,
                // triggering at least one ResetAsyncContinuationWrapperIndex event.
                RunScenarioAndFlush(async () =>
                {
                    await RuntimeAsync_RecursiveChain(34);
                });
            });

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            AssertContains(events, AsyncEventID.ResetAsyncContinuationWrapperIndex, ids);
        }

        // Requires threading:
        // Same comment as RuntimeAsync_WrapperIndexMatchesCallstack regarding Task.Run and wrapper index behavior.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexNoResetUnder32()
        {
            var events = CollectEvents(AllRuntimeAsyncKeywords, () =>
            {
                // A shallow chain stays within the first 32 slots --
                // no reset event should be emitted.
                RunScenarioAndFlush(async () =>
                {
                    await RuntimeAsync_RecursiveChain(2);
                });
            });

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            AssertDoesNotContain(events, AsyncEventID.ResetAsyncContinuationWrapperIndex, ids);
        }

        // Requires threading:
        // The periodic flush timer runs on a background thread.
        // On single-threaded runtimes there is no background thread to fire the timer.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_PeriodicTimerFlush()
        {
            static bool IsRequestedEvent(AsyncEventID id) =>
                id == AsyncEventID.CreateRuntimeAsyncContext ||
                id == AsyncEventID.ResumeRuntimeAsyncContext ||
                id == AsyncEventID.SuspendRuntimeAsyncContext ||
                id == AsyncEventID.CompleteRuntimeAsyncContext;

            var events = CollectEvents(RuntimeAsyncCoreKeywords, (collectedEvents, _) =>
            {
                // Run scenario - do NOT flush explicitly afterwards.
                RunScenario(async () =>
                {
                    await RuntimeAsync_SingleYield();
                });

                // Wait for the periodic flush timer (1s interval) to detect the idle buffer and flush it automatically.
                Thread.Sleep(1000);

                // Poll to make sure the expected buffer got flush.
                bool flushed = SpinWait.SpinUntil(() =>
                {
                    var stream = ParseAllEvents(collectedEvents);
                    return stream.EventIds.Any(id => IsRequestedEvent(id));
                }, TimeSpan.FromSeconds(20));

                Assert.True(flushed, "Expected periodic timer to flush buffer with core lifecycle events within timeout");
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            int coreEventCount = stream.EventIds.Count(id => IsRequestedEvent(id));

            AssertTrue(stream, coreEventCount > 0, "Expected periodic timer to flush buffer with core lifecycle events");
        }

        // Requires threading:
        // Verifies the background flush timer preserves the owning thread's OS thread ID,
        // which needs both a dedicated worker thread and the timer thread.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_PeriodicTimerFlush_PreservesOwnerThreadId()
        {
            // This test verifies that when the background flush timer flushes a thread's buffer,
            // the new header written afterwards preserves the owning thread's OS thread ID
            // (not the timer thread's ID).
            //
            // Strategy: run async work on a dedicated thread so its profiler context gets events.
            // Between two batches of work, wait for the flush timer to fire. Both buffer flushes
            // from the dedicated thread should carry the same OsThreadId.

            ulong workerOsThreadId = 0;
            var workerIdReady = new ManualResetEventSlim(false);
            var firstBatchDone = new ManualResetEventSlim(false);
            var firstFlushSeen = new ManualResetEventSlim(false);
            var events = new CollectedEvents();

            using (var listener = CreateListener(RuntimeAsyncCoreKeywords))
            {
                listener.RunWithCallback(e =>
                {
                    if (!workerIdReady.IsSet)
                    {
                        return;
                    }

                    if (e.EventId != AsyncEventsId || e.Payload is null || e.Payload.Count == 0)
                    {
                        return;
                    }

                    if (e.Payload[0] is not byte[] payload)
                    {
                        return;
                    }

                    EventBufferHeader? header = ParseEventBufferHeader(payload);
                    if (header is not null && header.Value.OsThreadId == workerOsThreadId)
                    {
                        events.Events.Enqueue(e);
                    }
                }, () =>
                {
                    SendFlushCommand();

                    var thread = new Thread(() =>
                    {
                        workerOsThreadId = GetCurrentOSThreadId();
                        workerIdReady.Set();

                        // First batch: generate events on this thread's profiler context.
                        RuntimeAsync_SingleYield().GetAwaiter().GetResult();
                        firstBatchDone.Set();

                        // Wait for the flush to deliver our first buffer before generating more events.
                        bool flushed = firstFlushSeen.Wait(TimeSpan.FromSeconds(20));
                        Assert.True(flushed, "Expected first flush of core lifecycle events within timeout");

                        // Second batch: generate more events on the same thread's context.
                        RuntimeAsync_SingleYield().GetAwaiter().GetResult();
                    });

                    thread.IsBackground = true;
                    thread.Start();

                    // Wait for the worker to finish its first batch, then force flush.
                    firstBatchDone.Wait(TimeSpan.FromSeconds(20));
                    SendFlushCommand();

                    // Poll for first buffer from our worker thread.
                    bool firstFlush = SpinWait.SpinUntil(() => events.Events.Count >= 1, TimeSpan.FromSeconds(20));
                    Assert.True(firstFlush, "Expected periodic timer to flush core lifecycle events within timeout");

                    firstFlushSeen.Set();

                    // Wait for the worker to finish its second batch.
                    bool joined = thread.Join(TimeSpan.FromSeconds(20));
                    Assert.True(joined, "Expected worker thread to terminate within timeout after second batch of work");

                    // Force a flush to deliver the second batch.
                    SendFlushCommand();

                    // Poll for second buffer from our worker thread.
                    bool secondFlush = SpinWait.SpinUntil(() => events.Events.Count >= 2, TimeSpan.FromSeconds(20));
                    Assert.True(secondFlush, "Expected periodic timer to flush core lifecycle events within timeout");
                });
            }

            // DumpAllEvents(events);

            AssertTrue(events, workerOsThreadId != 0, "Failed to capture worker OS thread ID");

            // The key assertion: find buffers that contain CreateAsyncContext events (our work batches).
            // There must be at least 2 such buffers (one per RuntimeAsync_SingleYield() call), and ALL of them must
            // have the worker's OsThreadId - proving the timer flush didn't corrupt the header.
            var stream = ParseAllEvents(events);
            var createEvents = stream.OfType(AsyncEventID.CreateRuntimeAsyncContext).ToList();
            AssertTrue(stream, createEvents.Count >= 2, $"Expected at least 2 CreateAsyncContext events from the worker thread, got {createEvents.Count}");
            AssertAll(stream, createEvents, e => Assert.Equal(workerOsThreadId, e.OsThreadId));
        }

        // Requires threading:
        // Spawns a dedicated thread that exits, then waits for the background flush timer
        // to detect and flush the orphaned thread-local buffer.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_DeadThreadFlush()
        {
            static bool IsRequestedEvent(AsyncEventID id) =>
                id == AsyncEventID.CreateRuntimeAsyncContext ||
                id == AsyncEventID.ResumeRuntimeAsyncContext ||
                id == AsyncEventID.SuspendRuntimeAsyncContext ||
                id == AsyncEventID.CompleteRuntimeAsyncContext;

            var events = CollectEvents(RuntimeAsyncCoreKeywords, (collectedEvents, _) =>
            {
                // Spawn a dedicated thread that runs async work then exits.
                // Its thread-local buffer becomes orphaned when the thread dies.
                var thread = new Thread(() =>
                {
                    RunScenario(async () =>
                    {
                        await RuntimeAsync_SingleYield();
                    });
                });

                thread.IsBackground = true;
                thread.Start();
                bool joined = thread.Join(TimeSpan.FromSeconds(20));
                Assert.True(joined, "Expected worker thread to terminate within timeout before waiting for orphaned buffer flush");

                // Do NOT send a flush command.
                // Wait for the periodic flush timer to detect the dead thread and flush its orphaned buffer.
                Thread.Sleep(1000);

                // Poll to make sure the expected buffer got flush.
                bool flushed = SpinWait.SpinUntil(() =>
                {
                    var stream = ParseAllEvents(collectedEvents);
                    return stream.EventIds.Any(id => IsRequestedEvent(id));
                }, TimeSpan.FromSeconds(20));

                Assert.True(flushed, "Expected periodic timer to flush buffer with core lifecycle events within timeout");
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            int coreEventCount = stream.EventIds.Count(id => IsRequestedEvent(id));

            AssertTrue(stream, coreEventCount > 0, "Expected periodic timer to flush dead thread's buffer");
        }

        // This test is sensitive to event noise - it asserts a specific clock event is absent.
        // It cannot run in parallel with other async profiler scenarios that might produce
        // clock events. Test parallelization is disabled via XunitAssemblyAttributes.cs.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_NoSyncClockEventBeforeInterval()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCoreKeywords, RuntimeAsync_SingleYield);

            var ids = ParseAllEvents(events).EventIds;

            AssertDoesNotContain(events, AsyncEventID.AsyncProfilerSyncClock, ids);
        }

        // This test is sensitive to event noise - it asserts zero context events appear
        // after enabling the listener. It cannot run in parallel with other async profiler
        // scenarios that might produce events on the same thread context.
        // Test parallelization is already disabled via XunitAssemblyAttributes.cs.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_NoEventsWhenDisabled()
        {
            // Run async work WITHOUT a listener attached
            for (int i = 0; i < 50; i++)
            {
                await RuntimeAsync_SingleYield();
            }

            // Now attach listener but don't run any RuntimeAsync work inside --
            // just call a synchronous no-op. Verify no stale events from above leak through.
            var events = await CollectEventsAsync(RuntimeAsyncCoreKeywords, () => Task.CompletedTask);

            // There may be meta data related events, but there should be no suspend/resume/complete events from the earlier work.
            var ids = ParseAllEvents(events).EventIds;

            int contextEvents = ids.Count(id => id == AsyncEventID.ResumeRuntimeAsyncContext || id == AsyncEventID.SuspendRuntimeAsyncContext || id == AsyncEventID.CompleteRuntimeAsyncContext);

            AssertEqual(events, 0, contextEvents);
        }

        public static IEnumerable<object[]> KeywordGatekeepingData()
        {
            yield return new object[] { (long)CreateRuntimeAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CreateRuntimeAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)ResumeRuntimeAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.ResumeRuntimeAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)SuspendRuntimeAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.SuspendRuntimeAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)CompleteRuntimeAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CompleteRuntimeAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)UnwindRuntimeAsyncExceptionKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.UnwindRuntimeAsyncException, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)CreateRuntimeAsyncCallstackKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CreateRuntimeAsyncCallstack, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)ResumeRuntimeAsyncCallstackKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.ResumeRuntimeAsyncCallstack, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)SuspendRuntimeAsyncCallstackKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.SuspendRuntimeAsyncCallstack, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)ResumeRuntimeAsyncMethodKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.ResumeRuntimeAsyncMethod, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)CompleteRuntimeAsyncMethodKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CompleteRuntimeAsyncMethod, AsyncEventID.AsyncProfilerMetadata } };
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_KeywordGatekeeping_Marker()
        {
            await RuntimeAsync_OuterCatches();
            await RuntimeAsync_ChainedYield();
            await StateMachineAsync_SingleYield();
        }

        // This test is sensitive to event noise - it asserts that ONLY the expected event
        // types appear for a given keyword. It cannot run in parallel with other async
        // profiler scenarios that might produce events on the same thread context.
        // Test parallelization is already disabled via XunitAssemblyAttributes.cs.
        [ConditionalTheory(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        [MemberData(nameof(KeywordGatekeepingData))]
        public async Task RuntimeAsync_KeywordGatekeeping(long keywordValue, AsyncEventID[] allowedEventIds)
        {
            EventKeywords kw = (EventKeywords)keywordValue;
            var allowed = new HashSet<AsyncEventID>(allowedEventIds);

            // Run a scenario that exercises all event types: resume, suspend,
            // complete, method events, callstacks, and exception unwinds.
            // Only the events matching the enabled keyword should be emitted.
            var events = await CollectEventsAsync(kw, RuntimeAsync_KeywordGatekeeping_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var unexpected = stream.EventIds.Where(id => !allowed.Contains(id)).ToList();

            AssertTrue(stream, unexpected.Count == 0, $"Keyword 0x{(long)kw:X}: unexpected event IDs [{string.Join(", ", unexpected)}], " + $"allowed [{string.Join(", ", allowed)}]");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ResetAsyncThreadContextEvent()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCoreKeywords, RuntimeAsync_SingleYield);

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            AssertContains(events, AsyncEventID.ResetAsyncThreadContext, ids);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_MetadataEventEmittedOnEnable()
        {
            var events = await CollectEventsAsync(AllRuntimeAsyncKeywords, RuntimeAsync_SingleYield);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var metadataList = stream.MetadataEvents;
            AssertTrue(stream, metadataList.Count >= 1, "Expected at least one metadata event in buffer");

            MetadataFromBuffer meta = metadataList[0];
            AssertTrue(stream, meta.QpcFrequency > 0, $"QPC frequency should be positive, got {meta.QpcFrequency}");
            AssertTrue(stream, meta.QpcSync > 0, $"QPC sync timestamp should be positive, got {meta.QpcSync}");
            AssertTrue(stream, meta.UtcSync > 0, $"UTC sync timestamp should be positive, got {meta.UtcSync}");
            AssertTrue(stream, meta.EventBufferSize > 0, $"Event buffer size should be positive, got {meta.EventBufferSize}");
            AssertTrue(stream, meta.WrapperCount > 0, "Wrapper count should be positive");
        }

        // Requires threading:
        // Spawns 8 threads with a Barrier to verify metadata is
        // emitted exactly once under concurrent enable pressure.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_MetadataEventEmittedOnceAcrossThreads()
        {
            const int threadCount = 8;

            var events = CollectEvents(AllRuntimeAsyncKeywords, () =>
            {
                using var barrier = new Barrier(threadCount);
                var tasks = new Task[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    tasks[i] = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        RuntimeAsync_SingleYield().GetAwaiter().GetResult();
                    }, TaskCreationOptions.LongRunning);
                }
                Task.WaitAll(tasks);
                SendFlushCommand();
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var metadataList = stream.MetadataEvents;
            AssertTrue(stream, metadataList.Count == 1, $"Expected exactly 1 metadata event across {threadCount} threads, got {metadataList.Count}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackNativeIPDeltaRoundtrip_Marker()
        {
            await RuntimeAsync_ChainedYield();
            await RuntimeAsync_DeepOuterCatches();
            await RuntimeAsync_RecursiveChain(10);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackNativeIPDeltaRoundtrip()
        {
            // Verify that delta-encoded NativeIPs in callstacks roundtrip correctly,
            // including both positive and negative deltas. With multiple distinct async
            // methods at different JIT-assigned addresses, the deltas between consecutive
            // NativeIPs will naturally span both directions. This exercises the full
            // zigzag + LEB128 encode/decode path through the production serializer.
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_CallstackNativeIPDeltaRoundtrip_Marker);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CallstackNativeIPDeltaRoundtrip_Marker));
            AssertNotEmpty(stream, callstacks);

            // Find callstacks with 3+ frames -- enough depth for meaningful deltas.
            var deepCallstacks = callstacks.Where(cs => cs.FrameCount >= 3).ToList();

            AssertTrue(stream, deepCallstacks.Count > 0, "Expected at least one callstack with 3+ frames for delta verification");

            bool hasPositiveDelta = false;
            bool hasNegativeDelta = false;

            foreach (var cs in deepCallstacks)
            {
                for (int i = 0; i < cs.Frames.Count; i++)
                {
                    var (methodId, _) = cs.Frames[i];
                    AssertTrue(stream, methodId != 0, $"Frame {i} has zero MethodId");

                    var method = GetMethodNameFromMethodId(cs.CallstackType, methodId);
                    AssertTrue(stream, method is not null, $"Frame {i}: MethodId 0x{methodId:X} does not resolve to a managed method");

                    if (i > 0)
                    {
                        long delta = (long)(cs.Frames[i].MethodId - cs.Frames[i - 1].MethodId);
                        if (delta > 0)
                            hasPositiveDelta = true;
                        else if (delta < 0)
                            hasNegativeDelta = true;
                    }
                }
            }

            // With multiple distinct async methods at different addresses, we expect
            // both positive and negative deltas. If the JIT happens to lay out all
            // methods monotonically (extremely unlikely), at minimum we must see
            // non-zero deltas proving the encoding works.
            AssertTrue(stream, hasPositiveDelta || hasNegativeDelta, "Expected at least one non-zero NativeIP delta across all callstack frames");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CallstackStressWithVaryingDepths_Marker(int depth)
        {
            await RuntimeAsync_RecursiveChain(depth);
        }

        // Requires threading:
        // The recursive async chain must execute in a single dispatch
        // loop (no sync context) to produce full-depth callstacks. Under xunit's
        // AsyncTestSyncContext, each await re-dispatches, fragmenting the chain.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackStressWithVaryingDepths()
        {
            // Stress test: run many async calls with varying callstack depths.
            // Varying sizes mean some callstacks will land at buffer boundaries,
            // naturally exercising the overflow/rewind path in callstack emission.
            // lambda -> Marker(d) -> RuntimeAsync_RecursiveChain(d) produces d + 2 frames.
            const int iterations = 200;
            int[] depths = new int[iterations];
            var rng = new Random(42);
            for (int i = 0; i < iterations; i++)
                depths[i] = rng.Next(1, 120);

            var events = CollectEvents(RuntimeAsyncCallstackKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    for (int i = 0; i < iterations; i++)
                        await RuntimeAsync_CallstackStressWithVaryingDepths_Marker(depths[i]);
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CallstackStressWithVaryingDepths_Marker));

            // Verify all callstacks have valid frame data that resolves to managed methods.
            foreach (var cs in callstacks)
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

            // One resume callstack per iteration (marker filters out noise).
            // lambda -> Marker -> RuntimeAsync_RecursiveChain(d) produces d + 2 frames.
            AssertTrue(stream, callstacks.Count >= iterations, $"Expected at least {iterations} callstacks with marker, got {callstacks.Count}");

            for (int i = 0; i < iterations; i++)
            {
                // lambda + Marker + RuntimeAsync_RecursiveChain(d) = d + 2
                int expected = depths[i] + 2;
                int actual = callstacks[i].FrameCount;
                AssertTrue(stream, actual == expected, $"Iteration {i}: expected depth {expected} (lambda -> Marker -> RuntimeAsync_RecursiveChain({depths[i]})), got {actual}");
            }

            // Verify multiple buffer flushes occurred.
            int bufferCount = 0;
            ForEachEventBufferPayload(events, _ => bufferCount++);
            AssertTrue(stream, bufferCount >= 3, $"Expected at least 3 buffer flushes, got {bufferCount}");
        }

        // Requires threading:
        // Deep recursive chains must execute in a single dispatch loop (no sync context)
        // to produce full-depth callstacks that trigger overflow.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackOverflowPathProducesValidFrames()
        {
            // Targeted test: run random-depth callstacks until we detect the overflow
            // path was exercised, then validate the affected callstack.
            // The overflow path fires when a large callstack doesn't fit inline in the
            // remaining buffer space -- the code rewinds, flushes the partial buffer,
            // and re-writes the callstack as the first event in a fresh buffer.
            //
            // To prove overflow occurred we check consecutive buffer pairs:
            //   Buffer N: not full (has remaining capacity, but not enough for the next callstack)
            //   Buffer N+1: first event is a large callstack
            // This proves the runtime detected insufficient space, rewound, and flushed.
            bool overflowDetected = false;
            var rng = new Random(42);

            for (int attempt = 0; attempt < 10 && !overflowDetected; attempt++)
            {
                int iterations = 500;
                int[] depths = new int[iterations];
                for (int i = 0; i < iterations; i++)
                    depths[i] = rng.Next(50, 250);

                var events = CollectEvents(AllRuntimeAsyncKeywords, () =>
                {
                    RunScenarioAndFlush(async () =>
                    {
                        for (int i = 0; i < iterations; i++)
                            await RuntimeAsync_RecursiveChain(depths[i]);
                    });
                });

                // Get buffer capacity from metadata.
                var stream = ParseAllEvents(events);
                var metadataList = stream.MetadataEvents;
                if (metadataList.Count == 0)
                    continue;
                uint bufferCapacity = metadataList[0].EventBufferSize;

                // Collect per-buffer info grouped by async thread context.
                // Consecutive buffers from the same context represent the overflow sequence.
                var buffersByContext = new Dictionary<uint, List<(int UsedSize, AsyncEventID FirstEventId, byte FirstFrameCount)>>();
                ForEachEventBufferPayload(events, buffer =>
                {
                    EventBufferHeader? header = ParseEventBufferHeader(buffer);
                    if (header is null)
                        return;

                    uint threadContextId = header.Value.AsyncThreadContextId;

                    // Parse the first event in this buffer.
                    int index = HeaderSize;
                    if (index >= buffer.Length)
                        return;

                    AsyncEventID firstId = (AsyncEventID)buffer[index++];
                    // Skip timestamp delta
                    ReadCompressedUInt64(buffer, ref index);
                    // Skip the payload-length prefix (UShort for callstack events).
                    _ = ReadPayloadLengthPrefix(buffer, firstId, ref index);

                    byte frameCount = 0;
                    if (firstId == AsyncEventID.ResumeRuntimeAsyncCallstack ||
                        firstId == AsyncEventID.CreateRuntimeAsyncCallstack ||
                        firstId == AsyncEventID.SuspendRuntimeAsyncCallstack)
                    {
                        // Callstack payload: callstackId(1) + continuationIndex(1) + frameCount(1) + compressed parentDispatcherId + compressed dispatcherId + frames...
                        index++; // callstack ID (reserved)
                        index++; // continuation index
                        if (index < buffer.Length)
                            frameCount = buffer[index];
                    }

                    if (!buffersByContext.TryGetValue(threadContextId, out var list))
                    {
                        list = new List<(int, AsyncEventID, byte)>();
                        buffersByContext[threadContextId] = list;
                    }
                    list.Add((buffer.Length, firstId, frameCount));
                });

                // Look for overflow evidence within the same thread context:
                // buffer N not full, buffer N+1 starts with large callstack.
                foreach (var bufferInfos in buffersByContext.Values)
                {
                    for (int i = 0; i < bufferInfos.Count - 1; i++)
                    {
                        var current = bufferInfos[i];
                        var next = bufferInfos[i + 1];

                        Assert.True((uint)current.UsedSize <= bufferCapacity, $"Buffer used size {current.UsedSize} exceeds capacity {bufferCapacity}.");

                        uint remaining = bufferCapacity - (uint)current.UsedSize;
                        bool currentNotFull = remaining > 0;
                        bool nextStartsWithLargeCallstack =
                            (next.FirstEventId == AsyncEventID.ResumeRuntimeAsyncCallstack ||
                             next.FirstEventId == AsyncEventID.CreateRuntimeAsyncCallstack ||
                             next.FirstEventId == AsyncEventID.SuspendRuntimeAsyncCallstack) &&
                            next.FirstFrameCount > 30;

                        if (currentNotFull && nextStartsWithLargeCallstack)
                        {
                            overflowDetected = true;
                            break;
                        }
                    }

                    if (overflowDetected)
                        break;
                }

                // Validate all large callstacks in the stream have correct frames.
                if (overflowDetected)
                {
                    var largeCallstacks = stream.OfType(AsyncEventID.ResumeRuntimeAsyncCallstack)
                        .Where(e => e.FrameCount > 30)
                        .ToList();

                    foreach (var cs in largeCallstacks)
                    {
                        Assert.Equal((int)cs.FrameCount, cs.Frames.Count);
                        for (int f = 0; f < cs.Frames.Count; f++)
                        {
                            var (methodId, _) = cs.Frames[f];
                            Assert.True(methodId != 0, $"Overflow callstack frame {f} has zero MethodId");

                            var method = GetMethodNameFromMethodId(cs.CallstackType, methodId);
                            Assert.True(method is not null, $"Overflow callstack frame {f}: MethodId 0x{methodId:X} does not resolve to a managed method");
                        }
                    }
                }
            }

            Assert.True(overflowDetected, "Failed to trigger callstack buffer overflow after 10 attempts -- " +
                "no consecutive buffer pair found where buffer N has remaining capacity and buffer N+1 starts with a large callstack");
        }

        // Requires threading:
        // Deep recursive chains must execute in a single dispatch
        // loop (no sync context) to produce chains exceeding the 255-frame cap.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackDepthCappedAtMaxFrames()
        {
            // Verify that callstack depth is capped when the continuation chain
            // exceeds the maximum frame count (255, limited by byte storage).
            // RuntimeAsync_RecursiveChain(300) produces a 300-deep chain + 1 lambda = 301 frames.
            const int requestedDepth = 300;

            var events = CollectEvents(ResumeRuntimeAsyncCallstackKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await RuntimeAsync_RecursiveChain(requestedDepth);
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.OfType(AsyncEventID.ResumeRuntimeAsyncCallstack).ToList();
            AssertTrue(stream, callstacks.Count >= 1, "Expected at least one callstack");

            // Find the callstack from our deep RuntimeAsync_RecursiveChain call.
            // The max frame count is capped at 255 (byte.MaxValue) since the
            // CaptureRuntimeAsyncCallstackState.Count is a byte.
            // RuntimeAsync_RecursiveChain(300) + 1 lambda = 301 frames, capped to 255.
            var deepest = callstacks.MaxBy(cs => cs.FrameCount);
            AssertEqual(stream, 255, (int)deepest!.FrameCount);
            AssertEqual(stream, (int)deepest.FrameCount, deepest.Frames.Count);

            // Verify all frames are valid.
            foreach (var (methodId, _) in deepest.Frames)
            {
                AssertTrue(stream, methodId != 0, "Frame has zero MethodId");
                var method = GetMethodNameFromMethodId(deepest.CallstackType, methodId);
                AssertTrue(stream, method is not null, $"MethodId 0x{methodId:X} does not resolve to a managed method");
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_MetadataMatchesWrapperMethods()
        {
            var events = await CollectEventsAsync(AllRuntimeAsyncKeywords, RuntimeAsync_SingleYield);

            var stream = ParseAllEvents(events);
            var metadataList = stream.MetadataEvents;
            AssertTrue(stream, metadataList.Count >= 1, "Expected at least one metadata event in buffer");

            MetadataFromBuffer meta = metadataList[0];
            AssertTrue(stream, meta.WrapperCount > 0, "Expected positive wrapper count in metadata");

            // On CoreCLR, verify via reflection that the contract-defined template produces names matching real methods.
            // This catches accidental renames of wrapper methods without updating the contract.
            if (PlatformDetection.IsCoreCLR)
            {
                Type? wrapperType = typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder)
                    .Assembly.GetType("System.Runtime.CompilerServices.AsyncProfiler+ContinuationWrapper");
                AssertNotNull(stream, wrapperType);
                for (int i = 0; i < meta.WrapperCount; i++)
                {
                    string expectedName = string.Format(WrapperNameTemplate, i);
                    var method = wrapperType.GetMethod(expectedName,
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    AssertTrue(stream, method is not null, $"Expected method '{expectedName}' not found on ContinuationWrapper type");
                }

                // Verify that the wrapper count matches the actual number of wrapper methods on the type.
                int actualCount = wrapperType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    .Count(m => m.Name.StartsWith(WrapperNamePrefix, StringComparison.Ordinal));
                AssertEqual(stream, meta.WrapperCount, actualCount);
            }
        }


        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAll_TracksAllBranches_BranchA_Marker()
        {
            await Task.Delay(100);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAll_TracksAllBranches_BranchB_Marker()
        {
            await Task.Delay(120);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAll_TracksAllBranches_BranchC_Marker()
        {
            await Task.Delay(140);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAll_TracksAllBranches_Marker()
        {
            await Task.WhenAll(
                RuntimeAsync_WhenAll_TracksAllBranches_BranchA_Marker(),
                RuntimeAsync_WhenAll_TracksAllBranches_BranchB_Marker(),
                RuntimeAsync_WhenAll_TracksAllBranches_BranchC_Marker());
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_WhenAll_TracksAllBranches()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_WhenAll_TracksAllBranches_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Each branch awaits a (staggered) Task.Delay, so it always suspends and produces its own
            // tracked chain with a Resume callstack containing the branch frame.
            var branchACallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAll_TracksAllBranches_BranchA_Marker));
            var branchBCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAll_TracksAllBranches_BranchB_Marker));
            var branchCCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAll_TracksAllBranches_BranchC_Marker));
            AssertNotEmpty(stream, branchACallstacks);
            AssertNotEmpty(stream, branchBCallstacks);
            AssertNotEmpty(stream, branchCCallstacks);

            // Because every branch suspends on a pending Task.Delay, Task.WhenAll is always pending when
            // the outer marker awaits it, so the marker also suspends and gets its own tracked chain.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAll_TracksAllBranches_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Each tracked chain (3 branches + outer marker) must see exactly one Create and one Complete on its own DispatcherId.
            AssertExactlyOneCreateAndComplete(stream, branchACallstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAll_TracksAllBranches_BranchA_Marker));
            AssertExactlyOneCreateAndComplete(stream, branchBCallstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAll_TracksAllBranches_BranchB_Marker));
            AssertExactlyOneCreateAndComplete(stream, branchCCallstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAll_TracksAllBranches_BranchC_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAll_TracksAllBranches_Marker));

            // The outer marker's chain should fire Create -> Resume -> Complete in that order.
            ulong markerDispatcherId = markerCallstacks[0].DispatcherId;
            var markerIds = stream.ChainEventsFromDispatcher(markerDispatcherId).Select(e => e.EventId).ToList();

            int createIdx = markerIds.IndexOf(AsyncEventID.CreateRuntimeAsyncContext);
            int resumeIdx = markerIds.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeAsyncContext after Create on the outer marker");

            int completeIdx = markerIds.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume on the outer marker");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAny_TracksAllBranches_Fast_Marker()
        {
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAny_TracksAllBranches_Slow1_Marker()
        {
            await Task.Delay(200);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAny_TracksAllBranches_Slow2_Marker()
        {
            await Task.Delay(300);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_WhenAny_TracksAllBranches_Marker()
        {
            Task fast = RuntimeAsync_WhenAny_TracksAllBranches_Fast_Marker();
            Task slow1 = RuntimeAsync_WhenAny_TracksAllBranches_Slow1_Marker();
            Task slow2 = RuntimeAsync_WhenAny_TracksAllBranches_Slow2_Marker();

            await Task.WhenAny(fast, slow1, slow2);
            await Task.WhenAll(slow1, slow2);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_WhenAny_TracksAllBranches()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_WhenAny_TracksAllBranches_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // All branches - including the slow ones whose completion the outer is no longer
            // strictly waiting on after WhenAny returned - must produce their own Resume callstacks.
            var fastCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Fast_Marker));
            var slow1Callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Slow1_Marker));
            var slow2Callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Slow2_Marker));
            AssertNotEmpty(stream, fastCallstacks);
            AssertNotEmpty(stream, slow1Callstacks);
            AssertNotEmpty(stream, slow2Callstacks);

            // Each tracked chain (3 branches + outer marker) must see exactly one Create and one Complete on its own DispatcherId.
            AssertExactlyOneCreateAndComplete(stream, fastCallstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Fast_Marker));
            AssertExactlyOneCreateAndComplete(stream, slow1Callstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Slow1_Marker));
            AssertExactlyOneCreateAndComplete(stream, slow2Callstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Slow2_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(RuntimeAsync_WhenAny_TracksAllBranches_Marker));

            // The outer marker should also be resumed at least once.
            ulong markerDispatcherId = markerCallstacks[0].DispatcherId;
            var markerIds = stream.ChainEventsFromDispatcher(markerDispatcherId).Select(e => e.EventId).ToList();
            int resumeCountForMarker = markerIds.Count(id => id == AsyncEventID.ResumeRuntimeAsyncContext);
            AssertTrue(stream, resumeCountForMarker >= 1, $"Expected outer marker to be resumed at least once, got {resumeCountForMarker}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ConfigureAwaitFalse_Leaf_Marker()
        {
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ConfigureAwaitFalse_Mid_Marker()
        {
            await RuntimeAsync_ConfigureAwaitFalse_Leaf_Marker().ConfigureAwait(false);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ConfigureAwaitFalse_Marker()
        {
            await RuntimeAsync_ConfigureAwaitFalse_Mid_Marker().ConfigureAwait(false);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public async Task RuntimeAsync_ConfigureAwaitFalse()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_ConfigureAwaitFalse_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_ConfigureAwaitFalse_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var deepest = markerCallstacks.OrderByDescending(cs => cs.FrameCount).First();

            var frameNames = deepest.Frames
                .Select(f => GetMethodNameFromMethodId(deepest.CallstackType, f.MethodId))
                .Where(n => n is not null)
                .ToList();

            AssertContains(stream, nameof(RuntimeAsync_ConfigureAwaitFalse_Leaf_Marker), frameNames);
            AssertContains(stream, nameof(RuntimeAsync_ConfigureAwaitFalse_Mid_Marker), frameNames);
            AssertContains(stream, nameof(RuntimeAsync_ConfigureAwaitFalse_Marker), frameNames);

            // ConfigureAwait(false) on a sequential await chain collapses Leaf -> Mid -> Marker into one
            // continuation chain, so exactly one Create / one Complete is expected on the marker's DispatcherId.
            AssertExactlyOneCreateAndComplete(stream, deepest.DispatcherId, nameof(RuntimeAsync_ConfigureAwaitFalse_Marker));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_TaskCancellation_Inner_Marker(CancellationToken ct)
        {
            await Task.Delay(5000, ct);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_TaskCancellation_Marker()
        {
            using var cts = new CancellationTokenSource();
            Task inner = RuntimeAsync_TaskCancellation_Inner_Marker(cts.Token);
            cts.CancelAfter(50);
            try
            {
                await inner;
            }
            catch (OperationCanceledException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_TaskCancellation()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_TaskCancellation_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_TaskCancellation_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var innerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_TaskCancellation_Inner_Marker));
            AssertNotEmpty(stream, innerCallstacks);

            // Inner cancelled task + outer marker must each see exactly one Create and one Complete on their own DispatcherId.
            AssertExactlyOneCreateAndComplete(stream, innerCallstacks[0].DispatcherId, nameof(RuntimeAsync_TaskCancellation_Inner_Marker));
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(RuntimeAsync_TaskCancellation_Marker));
        }

        private static InlinePostSynchronizationContext? s_runtimeAsyncSyncContextCtx;

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker()
        {
            // Install a non-default SynchronizationContext on this thread so the await captures it.
            // The await's continuation will be routed via SynchronizationContextAwaitTaskContinuation,
            // which the Runtime runtime-async dispatch loop should honor when resuming the chain.
            int callerThreadId = Environment.CurrentManagedThreadId;
            SynchronizationContext? prev = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(s_runtimeAsyncSyncContextCtx);
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

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CustomSyncContext_EmitsContextEventsAndCallstack()
        {
            s_runtimeAsyncSyncContextCtx = new InlinePostSynchronizationContext();

            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, () => RunIsolatedScenarioAsync(RuntimeAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker));

            // DumpAllEvents(events);

            // The custom SyncContext should have received at least one Post for the await continuation.
            AssertTrue(events, s_runtimeAsyncSyncContextCtx.PostCount > 0,
                $"Expected custom SynchronizationContext to receive at least one Post, got {s_runtimeAsyncSyncContextCtx.PostCount}");

            var stream = ParseAllEvents(events);

            // The marker frame should appear in the Resume callstack.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // The marker's chain should see exactly one Create and one Complete on its own DispatcherId.
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(RuntimeAsync_CustomSyncContext_EmitsContextEventsAndCallstack_Marker));

            // Verify the standard Create -> Resume -> Complete sequence fired in order for our context.
            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateRuntimeAsyncContext);
            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker()
        {
            await Task.Delay(100);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack()
        {
            var scheduler = new InlineRunTaskScheduler();

            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, async () =>
            {
                // Start the marker on the custom scheduler so the resulting Task is queued through it.
                await Task.Factory.StartNew(
                    () => RuntimeAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker(),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    scheduler).Unwrap();
            });

            // DumpAllEvents(events);

            // The custom scheduler must have received at least one QueueTask call.
            AssertTrue(events, scheduler.QueuedCount >= 1,
                $"Expected custom TaskScheduler to receive at least one QueueTask call, got {scheduler.QueuedCount}");

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // The marker's chain should see exactly one Create and one Complete on its own DispatcherId.
            AssertExactlyOneCreateAndComplete(stream, markerCallstacks[0].DispatcherId, nameof(RuntimeAsync_CustomTaskScheduler_EmitsContextEventsAndCallstack_Marker));

            // Verify the standard Create -> Resume -> Complete sequence fired in order for our context.
            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateRuntimeAsyncContext);
            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_EventSequenceOrder_Marker()
        {
            await RuntimeAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ValueTask_EventSequenceOrder()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, async () => await RuntimeAsync_ValueTask_EventSequenceOrder_Marker());

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_ValueTask_EventSequenceOrder_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateRuntimeAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext, resumeIdx + 1);
            AssertTrue(stream, completeIdx > resumeIdx, "Expected CompleteAsyncContext after Resume");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_MethodEventsEmitted_Marker()
        {
            await RuntimeAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ValueTask_MethodEventsEmitted()
        {
            var events = await CollectEventsAsync(RuntimeAsyncMethodKeywords | RuntimeAsyncCoreKeywords, async () => await RuntimeAsync_ValueTask_MethodEventsEmitted_Marker());

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var methodEvents = stream.All
                .Where(e => e.EventId is AsyncEventID.ResumeRuntimeAsyncMethod or AsyncEventID.CompleteRuntimeAsyncMethod)
                .Select(e => e.EventId)
                .ToList();

            int resumeCount = methodEvents.Count(id => id == AsyncEventID.ResumeRuntimeAsyncMethod);
            int completeCount = methodEvents.Count(id => id == AsyncEventID.CompleteRuntimeAsyncMethod);

            // Marker -> Level1 -> Level2 -> Level3
            AssertTrue(stream, resumeCount >= 4, $"Expected at least 4 ResumeAsyncMethod events for ValueTask chain, got {resumeCount}");
            AssertTrue(stream, completeCount >= 4, $"Expected at least 4 CompleteAsyncMethod events for ValueTask chain, got {completeCount}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_CallstackDepthMatchesChainDepth_Marker()
        {
            await RuntimeAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ValueTask_CallstackDepthMatchesChainDepth()
        {
            var events = await CollectValueTaskEventsAsync(RuntimeAsyncCallstackKeywords, RuntimeAsync_ValueTask_CallstackDepthMatchesChainDepth_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_ValueTask_CallstackDepthMatchesChainDepth_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            // Marker -> Level1 -> Level2 -> Level3.
            AssertEqual(stream, 4, markerCallstacks[0].FrameCount);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_CallstackFramesHaveDistinctMethodIds_Marker()
        {
            await RuntimeAsync_ValueTask_Level1();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ValueTask_CallstackFramesHaveDistinctMethodIds()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords, async () => await RuntimeAsync_ValueTask_CallstackFramesHaveDistinctMethodIds_Marker());

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_ValueTask_CallstackFramesHaveDistinctMethodIds_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            var methodIds = markerCallstacks[0].Frames.Select(f => f.MethodId).ToList();
            AssertEqual(stream, methodIds.Count, methodIds.Distinct().Count());
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_HandledException_InnerThrows_Marker()
        {
            await Task.Yield();
            throw new InvalidOperationException("valuetask inner throw");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_HandledException_Handled_Marker()
        {
            try
            {
                await RuntimeAsync_ValueTask_HandledException_InnerThrows_Marker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Marker()
        {
            await RuntimeAsync_ValueTask_HandledException_Handled_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ValueTask_HandledException_EmitsUnwindAndComplete()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords | UnwindRuntimeAsyncExceptionKeyword, async () => await RuntimeAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Marker());

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_ValueTask_HandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateRuntimeAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int unwindIdx = ids.IndexOf(AsyncEventID.UnwindRuntimeAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx > resumeIdx, "Expected UnwindAsyncException after Resume");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext, unwindIdx + 1);
            AssertTrue(stream, completeIdx > unwindIdx, "Expected CompleteAsyncContext after Unwind");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_UnhandledException_UnhandledOuter_Marker()
        {
            await RuntimeAsync_ValueTask_UnhandledException_UnhandledInner_Marker();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_UnhandledException_UnhandledInner_Marker()
        {
            await Task.Yield();
            throw new InvalidOperationException("valuetask unhandled inner");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async ValueTask RuntimeAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_Marker()
        {
            await RuntimeAsync_ValueTask_UnhandledException_UnhandledOuter_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete()
        {
            var events = await CollectEventsAsync(RuntimeAsyncCallstackKeywords | UnwindRuntimeAsyncExceptionKeyword, async () =>
            {
                try
                {
                    await RuntimeAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_Marker();
                }
                catch (InvalidOperationException)
                {
                }
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, nameof(RuntimeAsync_ValueTask_UnhandledException_EmitsUnwindAndComplete_Marker));
            AssertNotEmpty(stream, markerCallstacks);

            ulong dispatcherId = markerCallstacks[0].DispatcherId;
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateRuntimeAsyncContext);
            AssertTrue(stream, createIdx >= 0, "Expected CreateAsyncContext");

            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeRuntimeAsyncContext, createIdx + 1);
            AssertTrue(stream, resumeIdx > createIdx, "Expected ResumeAsyncContext after Create");

            int unwindIdx = ids.IndexOf(AsyncEventID.UnwindRuntimeAsyncException, resumeIdx + 1);
            AssertTrue(stream, unwindIdx > resumeIdx, "Expected UnwindAsyncException after Resume");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteRuntimeAsyncContext, unwindIdx + 1);
            AssertTrue(stream, completeIdx > unwindIdx, "Expected CompleteAsyncContext after Unwind");
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplaysPendingV2Chain_Inner_Marker()
        {
            using var dummy = new TestEventListener();
            dummy.AddSource(AsyncProfilerEventSourceName, EventLevel.Informational, EventKeywords.None);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplaysPendingV2Chain_Mid_Marker()
        {
            await RuntimeAsync_ResetContext_ReplaysPendingV2Chain_Inner_Marker();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplaysPendingV2Chain_Outer_Marker()
        {
            await RuntimeAsync_ResetContext_ReplaysPendingV2Chain_Mid_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ResetContext_ReplaysPendingV2Chain()
        {
            var events = await CollectEventsAsync(AllRuntimeAsyncKeywords, RuntimeAsync_ResetContext_ReplaysPendingV2Chain_Outer_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Locate the Runtime dispatcher driving the marker chain via its ResumeAsyncCallstack
            // events (which carry the marker frames in their callstack).
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, "RuntimeAsync_ResetContext_ReplaysPendingV2Chain");
            AssertNotEmpty(stream, markerCallstacks);

            // Thread-scoped replay assertion: at least one OS thread must have seen two
            // ResetAsyncThreadContext events AND show a marker ResumeAsyncCallstack plus
            // matching ResumeAsyncContext after its most recent reset. Multiple threads
            // in the trace can accumulate two resets (e.g. the test thread re-enables on
            // its initial event then again later), so we have to look at each candidate
            // thread, not just the first one. The thread that actually drove the replay
            // is the one the Runtime continuation resumed onto.
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
                    .Where(e => e.EventId == AsyncEventID.ResumeRuntimeAsyncContext
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

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplaysMultipleDispatchers_Inner_Marker(
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

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplaysMultipleDispatchers_Outer_Marker(TaskCompletionSource innerDone)
        {
            await innerDone.Task;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public async Task RuntimeAsync_ResetContext_ReplaysMultipleDispatchers()
        {
            var events = await CollectEventsAsync(AllRuntimeAsyncKeywords, async () =>
            {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var block = new ManualResetEventSlim(false);
                var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var innerDone = new TaskCompletionSource();

                Task inner = RuntimeAsync_ResetContext_ReplaysMultipleDispatchers_Inner_Marker(gate.Task, block, () => blocked.SetResult(), innerDone);
                Task outer = RuntimeAsync_ResetContext_ReplaysMultipleDispatchers_Outer_Marker(innerDone);

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

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The replay must emit at least one ResumeAsyncCallstack carrying a Runtime marker chain.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, "RuntimeAsync_ResetContext_ReplaysMultipleDispatchers");
            AssertNotEmpty(stream, markerCallstacks);

            // Decisive proof: find an OS thread where a single ResetAsyncThreadContext is
            // followed by >= 2 ResumeAsyncContext events before the next reset. A normal Runtime
            // dispatch loop emits only one ResumeAsyncContext per dispatcher entry, so two
            // in one reset window can only come from the walker traversing two stacked
            // asyncDispatcherInfo entries on t_current.
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
                        .Count(e => e.EventId == AsyncEventID.ResumeRuntimeAsyncContext
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
                "proving the reset-replay walker traversed multiple stacked Runtime dispatcher infos.");
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplayResumeCompleteBalance_Inner_Marker()
        {
            using var dummy = new TestEventListener();
            dummy.AddSource(AsyncProfilerEventSourceName, EventLevel.Informational, EventKeywords.None);
            await Task.Yield();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplayResumeCompleteBalance_Mid_Marker()
        {
            await RuntimeAsync_ResetContext_ReplayResumeCompleteBalance_Inner_Marker();
        }

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplayResumeCompleteBalance_Outer_Marker()
        {
            await RuntimeAsync_ResetContext_ReplayResumeCompleteBalance_Mid_Marker();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ResetContext_ReplayResumeCompleteBalance()
        {
            var events = await CollectEventsAsync(AllRuntimeAsyncKeywords, RuntimeAsync_ResetContext_ReplayResumeCompleteBalance_Outer_Marker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Locate the replayed marker ResumeAsyncCallstack (the reset replays the suspended
            // Outer->Mid->Inner chain, producing a callstack carrying the marker frames).
            var markerCallstacks = stream.CallstacksWithMarker(
                AsyncEventID.ResumeRuntimeAsyncCallstack, "RuntimeAsync_ResetContext_ReplayResumeCompleteBalance");
            AssertNotEmpty(stream, markerCallstacks);

            // Resume/Complete balance across the replay boundary: the whole Outer->Mid->Inner chain
            // is a single Runtime dispatcher whose deepest replayed callstack has N frames, so N async
            // methods must each eventually complete. Balance is NOT preserved per reset epoch by
            // design (a Resume can land in one epoch and its Complete in a later epoch once a config
            // change bumps the revision mid-chain), so the count must be reconstructed over the whole
            // trace, scoped to the dispatcher rather than to a single epoch. Assert the marker
            // dispatcher emits at least N CompleteAsyncMethod events across the entire trace. Using
            // >= keeps the assertion robust against additional method events from the harness.
            var deepest = markerCallstacks.OrderByDescending(c => c.FrameCount).First();
            ulong markerDispatcherId = deepest.DispatcherId;

            int completeMethodCount = stream.ChainEventsFromDispatcher(markerDispatcherId)
                .Count(e => e.EventId == AsyncEventID.CompleteRuntimeAsyncMethod);

            AssertTrue(stream, completeMethodCount >= deepest.FrameCount,
                $"Expected at least {deepest.FrameCount} CompleteRuntimeAsyncMethod events across the " +
                $"trace for marker dispatcher {markerDispatcherId} to balance the replayed callstack of depth " +
                $"{deepest.FrameCount}, but found {completeMethodCount}.");
        }

        // StateMachine async inner. When it calls innerDone.SetResult() it inline-resumes
        // the Runtime outer while this StateMachine dispatcher's AsyncStateMachineDispatcherInfo is still on t_current,
        // co-stacking a StateMachine and a Runtime dispatcher info at the moment the reset replay walker runs.
        [RuntimeAsyncMethodGeneration(false)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplaysMixedV1V2ChainInAddressOrder_V1Inner_Marker(
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

        [RuntimeAsyncMethodGeneration(true)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task RuntimeAsync_ResetContext_ReplaysMixedV1V2ChainInAddressOrder_V2Outer_Marker(TaskCompletionSource innerDone)
        {
            await innerDone.Task;
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public async Task RuntimeAsync_ResetContext_ReplaysMixedV1V2ChainInAddressOrder()
        {
            var events = await CollectEventsAsync(AllKeywords, async () =>
            {
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var block = new ManualResetEventSlim(false);
                var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                // No RunContinuationsAsynchronously: innerDone.SetResult() inline-resumes the Runtime
                // outer while the StateMachine inner dispatcher is still on t_current, producing a mixed
                // StateMachine+Runtime chain for the reset replay walker to merge.
                var innerDone = new TaskCompletionSource();

                Task inner = RuntimeAsync_ResetContext_ReplaysMixedV1V2ChainInAddressOrder_V1Inner_Marker(gate.Task, block, () => blocked.SetResult(), innerDone);
                Task outer = RuntimeAsync_ResetContext_ReplaysMixedV1V2ChainInAddressOrder_V2Outer_Marker(innerDone);

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

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // The merge walker traverses both the StateMachine (AsyncStateMachineDispatcherInfo) and Runtime
            // (AsyncDispatcherInfo) t_current chains, recursing on the smaller-address node first
            // to emit oldest-first. Decisive proof that both branches of the merge ran: within a
            // single reset window on one thread, both a ResumeStateMachineAsyncContextKeyword (StateMachine) and a
            // ResumeRuntimeAsyncContext (Runtime) are replayed.
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

                for (int i = 0; i < orderedResets.Count && !found; i++)
                {
                    long windowStart = orderedResets[i].Timestamp;
                    long windowEnd = (i + 1 < orderedResets.Count)
                        ? orderedResets[i + 1].Timestamp
                        : long.MaxValue;

                    bool hasV1Resume = stream.All.Any(e => e.EventId == AsyncEventID.ResumeStateMachineAsyncContext
                        && e.OsThreadId == threadId && e.Timestamp >= windowStart && e.Timestamp < windowEnd);
                    bool hasV2Resume = stream.All.Any(e => e.EventId == AsyncEventID.ResumeRuntimeAsyncContext
                        && e.OsThreadId == threadId && e.Timestamp >= windowStart && e.Timestamp < windowEnd);

                    if (hasV1Resume && hasV2Resume)
                    {
                        found = true;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            AssertTrue(stream, found,
                "Expected a single ResetAsyncThreadContext window containing both a ResumeStateMachineAsyncContextKeyword " +
                "(StateMachine) and a ResumeRuntimeAsyncContext (Runtime), proving the reset-replay merge walker traversed " +
                "a mixed StateMachine+Runtime chain and exercised both branches of its address-ordered recursion.");
        }
    }
}
