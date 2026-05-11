// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Linq;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    // Mirrors AsyncProfiler.AsyncEventID from the runtime (which is internal and inaccessible from tests).
    public enum AsyncEventID : byte
    {
        CreateAsyncContext = 1,
        ResumeAsyncContext = 2,
        SuspendAsyncContext = 3,
        CompleteAsyncContext = 4,
        UnwindAsyncException = 5,
        CreateAsyncCallstack = 6,
        ResumeAsyncCallstack = 7,
        SuspendAsyncCallstack = 8,
        ResumeAsyncMethod = 9,
        CompleteAsyncMethod = 10,
        ResetAsyncThreadContext = 11,
        ResetAsyncContinuationWrapperIndex = 12,
        AsyncProfilerMetadata = 13,
        AsyncProfilerSyncClock = 14,
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/127951", TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
    public class AsyncProfilerTests
    {
        // The test scenarios drive async work via Task.Run(...).GetAwaiter().GetResult() (see
        // RunScenarioAndFlush / RunScenario), which requires synchronous blocking waits. On
        // single-threaded WASM this throws PlatformNotSupportedException from
        // RuntimeFeature.ThrowIfMultithreadingIsNotSupported(), so gate the tests on both
        // runtime async support and threading support.
        // Some tests rely on GetMethodFromNativeIP which is not supported on NativeAOT.
        public static bool IsRuntimeAsyncAndThreadingSupported =>
            PlatformDetection.IsRuntimeAsyncSupported && PlatformDetection.IsMultithreadingSupported && PlatformDetection.IsNotNativeAot;

        private const string AsyncProfilerEventSourceName = "System.Runtime.CompilerServices.AsyncProfilerEventSource";
        private const int AsyncEventsId = 1;
        private const int HeaderSize = 1 + sizeof(uint) + sizeof(uint) + sizeof(ulong) + sizeof(uint) + sizeof(ulong) + sizeof(ulong);

        // AsyncProfilerEventSource Keywords matching the event source definition
        private const EventKeywords CreateAsyncContextKeyword = (EventKeywords)0x1;
        private const EventKeywords ResumeAsyncContextKeyword = (EventKeywords)0x2;
        private const EventKeywords SuspendAsyncContextKeyword = (EventKeywords)0x4;
        private const EventKeywords CompleteAsyncContextKeyword = (EventKeywords)0x8;
        private const EventKeywords UnwindAsyncExceptionKeyword = (EventKeywords)0x10;
        private const EventKeywords CreateAsyncCallstackKeyword = (EventKeywords)0x20;
        private const EventKeywords ResumeAsyncCallstackKeyword = (EventKeywords)0x40;
        private const EventKeywords SuspendAsyncCallstackKeyword = (EventKeywords)0x80;
        private const EventKeywords ResumeAsyncMethodKeyword = (EventKeywords)0x100;
        private const EventKeywords CompleteAsyncMethodKeyword = (EventKeywords)0x200;

        private const EventKeywords AllKeywords =
            CreateAsyncContextKeyword | ResumeAsyncContextKeyword | SuspendAsyncContextKeyword |
            CompleteAsyncContextKeyword | UnwindAsyncExceptionKeyword |
            CreateAsyncCallstackKeyword | ResumeAsyncCallstackKeyword | SuspendAsyncCallstackKeyword |
            ResumeAsyncMethodKeyword | CompleteAsyncMethodKeyword;

        private const EventKeywords CoreKeywords =
            CreateAsyncContextKeyword | ResumeAsyncContextKeyword | SuspendAsyncContextKeyword | CompleteAsyncContextKeyword;

        private const EventKeywords MethodKeywords =
            ResumeAsyncMethodKeyword | CompleteAsyncMethodKeyword;

        private const EventKeywords CallstackKeywords =
            CreateAsyncContextKeyword | CreateAsyncCallstackKeyword |
            ResumeAsyncContextKeyword | ResumeAsyncCallstackKeyword | CompleteAsyncContextKeyword |
            CompleteAsyncMethodKeyword | UnwindAsyncExceptionKeyword;

        private static readonly MethodInfo s_getMethodFromNativeIP =
            typeof(StackFrame).GetMethod("GetMethodFromNativeIP", BindingFlags.Static | BindingFlags.NonPublic)!;

        private static MethodBase? GetMethodFromNativeIP(ulong nativeIP)
            => (MethodBase?)s_getMethodFromNativeIP.Invoke(null, new object[] { (IntPtr)nativeIP });

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task Func()
        {
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task FuncChained()
        {
            await FuncInner();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task FuncInner()
        {
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task OuterCatches()
        {
            try
            {
                await InnerThrows();
            }
            catch (InvalidOperationException)
            {
            }
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task InnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("inner");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepOuterCatches()
        {
            try
            {
                await DeepMiddle();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepMiddle()
        {
            await DeepInnerThrows();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepInnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("deep inner");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepUnhandledOuter()
        {
            await DeepUnhandledMiddle();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepUnhandledMiddle()
        {
            await DeepUnhandledInnerThrows();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task DeepUnhandledInnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("deep unhandled");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task RecursiveFunc(int depth)
        {
            if (depth <= 1)
            {
                await Task.Yield();
                return;
            }
            await RecursiveFunc(depth - 1);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task WrapperTestA(List<(string MethodName, int WrapperSlot)> captures)
        {
            await WrapperTestB(captures);
            captures.Add((nameof(WrapperTestA), GetCurrentWrapperSlot(nameof(WrapperTestA))));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task WrapperTestB(List<(string MethodName, int WrapperSlot)> captures)
        {
            await WrapperTestC(captures);
            captures.Add((nameof(WrapperTestB), GetCurrentWrapperSlot(nameof(WrapperTestB))));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        static async Task WrapperTestC(List<(string MethodName, int WrapperSlot)> captures)
        {
            await Task.Yield();
            captures.Add((nameof(WrapperTestC), GetCurrentWrapperSlot(nameof(WrapperTestC))));
        }

        private static TestEventListener CreateListener(EventKeywords keywords)
        {
            var listener = new TestEventListener();
            listener.AddSource(AsyncProfilerEventSourceName, EventLevel.Informational, keywords);
            return listener;
        }

        private static void SendFlushCommand()
        {
            const int FlushCommand = 1;
            foreach (EventSource source in EventSource.GetSources())
            {
                if (source.Name == AsyncProfilerEventSourceName)
                {
                    EventSource.SendCommand(source, (EventCommand)FlushCommand, null);
                    return;
                }
            }
        }

        private static ulong GetCurrentOSThreadId()
        {
            return (ulong)typeof(Thread)
                .GetProperty("CurrentOSThreadId", BindingFlags.Static | BindingFlags.NonPublic)!
                .GetValue(null)!;
        }

        private static int GetCurrentWrapperSlot(string resumedMethodName)
        {
            var st = new StackTrace();
            for (int i = 0; i < st.FrameCount - 1; i++)
            {
                string? name = st.GetFrame(i)?.GetMethod()?.Name;
                if (name is not null && name.Contains(resumedMethodName))
                {
                    // The next frame should be the Continuation_Wrapper_N that dispatched this method.
                    string? wrapperName = st.GetFrame(i + 1)?.GetMethod()?.Name;
                    if (wrapperName is not null && wrapperName.StartsWith("Continuation_Wrapper_", StringComparison.Ordinal))
                    {
                        return int.Parse(wrapperName.Substring("Continuation_Wrapper_".Length));
                    }
                    return -1;
                }
            }
            return -1;
        }

        private delegate bool EventVisitor(AsyncEventID eventId, ReadOnlySpan<byte> buffer, ref int index);

        private delegate bool EventVisitorWithTimestamp(AsyncEventID eventId, long timestamp, ReadOnlySpan<byte> buffer, ref int index);

        private static void ParseEventBuffer(ReadOnlySpan<byte> buffer, EventVisitor visitor)
        {
            ParseEventBuffer(buffer, (AsyncEventID eventId, long _, ReadOnlySpan<byte> buf, ref int idx) =>
                visitor(eventId, buf, ref idx));
        }

        private static void ParseEventBuffer(ReadOnlySpan<byte> buffer, EventVisitorWithTimestamp visitor)
        {
            EventBufferHeader? header = ParseEventBufferHeader(buffer);
            if (header is null)
                return;

            int index = HeaderSize;
            long baseTimestamp = (long)header.Value.StartTimestamp;

            while (index < buffer.Length)
            {
                if (index + 2 > buffer.Length)
                    break;

                AsyncEventID eventId = (AsyncEventID)buffer[index++];

                long delta = (long)ReadCompressedUInt64(buffer, ref index);
                baseTimestamp += delta;

                if (!visitor(eventId, baseTimestamp, buffer, ref index))
                    break;
            }
        }

        private static bool SkipEventPayload(AsyncEventID eventId, ReadOnlySpan<byte> buffer, ref int index)
        {
            switch (eventId)
            {
                case AsyncEventID.CreateAsyncContext:
                case AsyncEventID.ResumeAsyncContext:
                    ReadCompressedUInt64(buffer, ref index);
                    return true;
                case AsyncEventID.SuspendAsyncContext:
                case AsyncEventID.CompleteAsyncContext:
                case AsyncEventID.ResumeAsyncMethod:
                case AsyncEventID.CompleteAsyncMethod:
                case AsyncEventID.ResetAsyncThreadContext:
                case AsyncEventID.ResetAsyncContinuationWrapperIndex:
                    return true;
                case AsyncEventID.AsyncProfilerMetadata:
                    SkipMetadataPayload(buffer, ref index);
                    return true;
                case AsyncEventID.AsyncProfilerSyncClock:
                    ReadCompressedUInt64(buffer, ref index); // qpcSync
                    ReadCompressedUInt64(buffer, ref index); // utcSync
                    return true;
                case AsyncEventID.UnwindAsyncException:
                    ReadCompressedUInt32(buffer, ref index);
                    return true;
                case AsyncEventID.CreateAsyncCallstack:
                case AsyncEventID.ResumeAsyncCallstack:
                case AsyncEventID.SuspendAsyncCallstack:
                    SkipCallstackPayload(buffer, ref index);
                    return true;
                default:
                    return false;
            }
        }

        private static uint ReadCompressedUInt32(ReadOnlySpan<byte> buffer, ref int index)
        {
            EventBuffer.Deserializer.ReadCompressedUInt32(buffer, ref index, out uint value);
            return value;
        }

        private static ulong ReadCompressedUInt64(ReadOnlySpan<byte> buffer, ref int index)
        {
            EventBuffer.Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong value);
            return value;
        }

        private static void SkipCallstackPayload(ReadOnlySpan<byte> buffer, ref int index)
        {
            ReadCallstackPayload(buffer, ref index, out _, out _);
        }

        private static void ReadCallstackPayload(ReadOnlySpan<byte> buffer, ref int index,
            out byte frameCount, out List<(ulong NativeIP, int State)> frames)
        {
            ReadCallstackPayload(buffer, ref index, out _, out frameCount, out frames);
        }

        private static void ReadCallstackPayload(ReadOnlySpan<byte> buffer, ref int index,
            out ulong taskId, out byte frameCount, out List<(ulong NativeIP, int State)> frames)
        {
            index++; // type
            index++; // callstack ID (reserved)
            frameCount = buffer[index++];
            taskId = ReadCompressedUInt64(buffer, ref index);
            frames = new List<(ulong, int)>(frameCount);

            if (frameCount == 0)
                return;

            ulong currentNativeIP = ReadCompressedUInt64(buffer, ref index);
            int state = ReadCompressedInt32(buffer, ref index);
            frames.Add((currentNativeIP, state));

            for (int i = 1; i < frameCount; i++)
            {
                long delta = ReadCompressedInt64(buffer, ref index);
                state = ReadCompressedInt32(buffer, ref index);
                currentNativeIP = (ulong)((long)currentNativeIP + delta);
                frames.Add((currentNativeIP, state));
            }
        }

        private static int ReadCompressedInt32(ReadOnlySpan<byte> buffer, ref int index)
        {
            EventBuffer.Deserializer.ReadCompressedInt32(buffer, ref index, out int value);
            return value;
        }

        private static long ReadCompressedInt64(ReadOnlySpan<byte> buffer, ref int index)
        {
            EventBuffer.Deserializer.ReadCompressedInt64(buffer, ref index, out long value);
            return value;
        }

        private static void SkipMetadataPayload(ReadOnlySpan<byte> buffer, ref int index)
        {
            ReadMetadataPayload(buffer, ref index, out _, out _, out _, out _, out _);
        }

        private static void ReadMetadataPayload(ReadOnlySpan<byte> buffer, ref int index,
            out ulong qpcFrequency, out ulong qpcSync, out ulong utcSync, out uint eventBufferSize, out long[] wrapperIPs)
        {
            qpcFrequency = ReadCompressedUInt64(buffer, ref index);
            qpcSync = ReadCompressedUInt64(buffer, ref index);
            utcSync = ReadCompressedUInt64(buffer, ref index);
            eventBufferSize = ReadCompressedUInt32(buffer, ref index);
            byte wrapperCount = buffer[index++];
            wrapperIPs = new long[wrapperCount];
            for (int i = 0; i < wrapperCount; i++)
            {
                wrapperIPs[i] = (long)ReadCompressedUInt64(buffer, ref index);
            }
        }

        private record struct MetadataFromBuffer(ulong QpcFrequency, ulong QpcSync, ulong UtcSync, uint EventBufferSize, long[] WrapperIPs);

        private static List<MetadataFromBuffer> CollectMetadataFromBuffer(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            var metadataList = new List<MetadataFromBuffer>();
            ForEachEventBufferPayload(events, buffer =>
            {
                ParseEventBuffer(buffer, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId == AsyncEventID.AsyncProfilerMetadata)
                    {
                        ReadMetadataPayload(buf, ref idx, out ulong freq, out ulong qpcSync, out ulong utcSync, out uint bufSize, out long[] ips);
                        metadataList.Add(new MetadataFromBuffer(freq, qpcSync, utcSync, bufSize, ips));
                        return true;
                    }
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });
            return metadataList;
        }

        private static ulong ParseOsThreadId(ReadOnlySpan<byte> buffer)
        {
            return ParseEventBufferHeader(buffer)?.OsThreadId ?? 0;
        }

        private readonly record struct EventBufferHeader(byte Version, uint TotalSize, uint AsyncThreadContextId, ulong OsThreadId, uint EventCount, ulong StartTimestamp, ulong EndTimestamp);

        private static EventBufferHeader? ParseEventBufferHeader(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < HeaderSize || buffer[0] != 1)
                return null;

            int index = 1;
            EventBuffer.Deserializer.ReadUInt32(buffer, ref index, out uint totalSize);
            EventBuffer.Deserializer.ReadUInt32(buffer, ref index, out uint contextId);
            EventBuffer.Deserializer.ReadUInt64(buffer, ref index, out ulong threadId);
            EventBuffer.Deserializer.ReadUInt32(buffer, ref index, out uint eventCount);
            EventBuffer.Deserializer.ReadUInt64(buffer, ref index, out ulong startTs);
            EventBuffer.Deserializer.ReadUInt64(buffer, ref index, out ulong endTs);

            return new EventBufferHeader(buffer[0], totalSize, contextId, threadId, eventCount, startTs, endTs);
        }

        private static List<AsyncEventID> CollectAsyncEventIds(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            var allEventIds = new List<AsyncEventID>();
            ForEachEventBufferPayload(events, buffer =>
            {
                ParseEventBuffer(buffer, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    allEventIds.Add(eventId);
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });
            return allEventIds;
        }

        private static List<(AsyncEventID EventId, long Timestamp)> CollectAsyncEventIdsWithTimestamps(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            var allEvents = new List<(AsyncEventID EventId, long Timestamp)>();
            ForEachEventBufferPayload(events, buffer =>
            {
                ParseEventBuffer(buffer, (AsyncEventID eventId, long timestamp, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    allEvents.Add((eventId, timestamp));
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });
            allEvents.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return allEvents;
        }

        private static HashSet<ulong> CollectOsThreadIds(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            var threadIds = new HashSet<ulong>();
            ForEachEventBufferPayload(events, buffer =>
            {
                ulong tid = ParseOsThreadId(buffer);
                if (tid != 0)
                    threadIds.Add(tid);
            });
            return threadIds;
        }

        private static List<uint> CollectUnwindFrameCounts(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            var frameCounts = new List<uint>();
            ForEachEventBufferPayload(events, buffer =>
            {
                ParseEventBuffer(buffer, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId == AsyncEventID.UnwindAsyncException)
                    {
                        frameCounts.Add(ReadCompressedUInt32(buf, ref idx));
                        return true;
                    }
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });
            return frameCounts;
        }

        private static List<(ulong TaskId, byte FrameCount, List<(ulong NativeIP, int State)> Frames)> CollectCallstacks(
            ConcurrentQueue<EventWrittenEventArgs> events)
        {
            return CollectCallstacks(events, AsyncEventID.ResumeAsyncCallstack, threadId: null);
        }

        private static List<(ulong TaskId, byte FrameCount, List<(ulong NativeIP, int State)> Frames)> CollectCallstacks(
            ConcurrentQueue<EventWrittenEventArgs> events, ulong? threadId)
        {
            return CollectCallstacks(events, AsyncEventID.ResumeAsyncCallstack, threadId);
        }

        private static List<(ulong TaskId, byte FrameCount, List<(ulong NativeIP, int State)> Frames)> CollectCallstacks(
            ConcurrentQueue<EventWrittenEventArgs> events, AsyncEventID callstackEventId)
        {
            return CollectCallstacks(events, callstackEventId, threadId: null);
        }

        private static List<(ulong TaskId, byte FrameCount, List<(ulong NativeIP, int State)> Frames)> CollectCallstacks(
            ConcurrentQueue<EventWrittenEventArgs> events, AsyncEventID callstackEventId, ulong? threadId)
        {
            var callstacks = new List<(ulong, byte, List<(ulong, int)>)>();
            ForEachEventBufferPayload(events, buffer =>
            {
                if (threadId.HasValue)
                {
                    ulong tid = ParseOsThreadId(buffer);
                    if (tid != threadId.Value)
                        return;
                }

                ParseEventBuffer(buffer, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId == callstackEventId)
                    {
                        ReadCallstackPayload(buf, ref idx, out ulong taskId, out byte frameCount, out var frames);
                        callstacks.Add((taskId, frameCount, frames));
                        return true;
                    }
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });
            return callstacks;
        }

        private static (byte FrameCount, List<(ulong NativeIP, int State)> Frames)? FindCallstackAfterTimestamp(
            ConcurrentQueue<EventWrittenEventArgs> events, ulong threadId, long afterTimestamp)
        {
            (byte FrameCount, List<(ulong, int)> Frames)? best = null;
            long bestTimestamp = long.MaxValue;

            ForEachEventBufferPayload(events, buffer =>
            {
                ulong tid = ParseOsThreadId(buffer);
                if (tid != threadId)
                    return;

                ParseEventBuffer(buffer, (AsyncEventID eventId, long timestamp, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId == AsyncEventID.ResumeAsyncCallstack)
                    {
                        ReadCallstackPayload(buf, ref idx, out byte frameCount, out var frames);
                        if (timestamp >= afterTimestamp && timestamp < bestTimestamp)
                        {
                            bestTimestamp = timestamp;
                            best = (frameCount, frames);
                        }
                        return true;
                    }
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });

            return best;
        }

        private delegate void EventBufferPayloadAction(ReadOnlySpan<byte> payload);

        private static void ForEachEventBufferPayload(ConcurrentQueue<EventWrittenEventArgs> events, EventBufferPayloadAction action)
        {
            foreach (var e in events)
            {
                if (e.EventId == AsyncEventsId && e.Payload is { Count: >= 1 } && e.Payload[0] is byte[] rawPayload)
                {
                    action(rawPayload);
                }
            }
        }

        // Uncomment at callsite to dump all collected event buffers to console for diagnostics:
        private static void DumpCollectedEvents(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            ForEachEventBufferPayload(events, buffer => EventBuffer.OutputEventBuffer(buffer));
        }

        private static void RunScenarioAndFlush(Func<Task> scenario)
        {
            Task.Run(scenario).GetAwaiter().GetResult();
            SendFlushCommand();
        }

        private static void RunScenario(Func<Task> scenario)
        {
            Task.Run(scenario).GetAwaiter().GetResult();
        }

        private static ConcurrentQueue<EventWrittenEventArgs> CollectEvents(EventKeywords keywords, Action callback)
        {
            return CollectEvents(keywords, (_, _) => callback());
        }

        private static ConcurrentQueue<EventWrittenEventArgs> CollectEvents(EventKeywords keywords, Action<ConcurrentQueue<EventWrittenEventArgs>, EventKeywords> callback)
        {
            var events = new ConcurrentQueue<EventWrittenEventArgs>();
            using (var listener = CreateListener(keywords))
            {
                listener.RunWithCallback(events.Enqueue, () =>
                {
                    SendFlushCommand();
                    events.Clear();
                    callback(events, keywords);
                });
            }
            return events;
        }

        private static void AssertCallstackSimulationReachesZero(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            var eventIds = CollectAsyncEventIds(events);
            var frameCounts = CollectUnwindFrameCounts(events);
            var callstacks = CollectCallstacks(events);

            int stackDepth = 0;
            int unwindIdx = 0;
            int callstackIdx = 0;

            foreach (AsyncEventID id in eventIds)
            {
                switch (id)
                {
                    case AsyncEventID.ResumeAsyncCallstack:
                        if (callstackIdx < callstacks.Count)
                            stackDepth = callstacks[callstackIdx++].FrameCount;
                        break;
                    case AsyncEventID.CompleteAsyncMethod:
                        if (stackDepth > 0)
                            stackDepth--;
                        break;
                    case AsyncEventID.UnwindAsyncException:
                        if (unwindIdx < frameCounts.Count)
                            stackDepth = Math.Max(0, stackDepth - (int)frameCounts[unwindIdx++]);
                        break;
                }
            }

            Assert.True(callstackIdx > 0, "Expected at least one ResumeAsyncCallstack event");
            Assert.Equal(0, stackDepth);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_EventBufferHeaderFormat()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            int buffersChecked = 0;
            ForEachEventBufferPayload(events, buffer =>
            {
                EventBufferHeader? parsed = ParseEventBufferHeader(buffer);
                Assert.NotNull(parsed);
                EventBufferHeader header = parsed.Value;

                Assert.Equal(1, header.Version);
                Assert.Equal((uint)buffer.Length, header.TotalSize);
                Assert.True(header.AsyncThreadContextId > 0, "Async thread context ID should be positive");
                Assert.True(header.OsThreadId != 0, "OS thread ID should be non-zero");
                Assert.True(header.StartTimestamp > 0, "Start timestamp should be positive");
                Assert.True(header.EndTimestamp >= header.StartTimestamp,
                    $"End timestamp ({header.EndTimestamp}) should be >= start timestamp ({header.StartTimestamp})");

                int eventCount = 0;
                ParseEventBuffer(buffer, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    eventCount++;
                    return SkipEventPayload(eventId, buf, ref idx);
                });

                Assert.Equal(header.EventCount, (uint)eventCount);
                Assert.True(header.EventCount > 0, "Expected at least one event in buffer");

                buffersChecked++;
            });

            Assert.True(buffersChecked > 0, "Expected at least one buffer");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_EventsEmitted()
        {
            var events = CollectEvents(AllKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            Assert.True(events.Count > 0, "Expected at least one AsyncEvents event to be emitted");
            Assert.Contains(events, e => e.EventId == AsyncEventsId);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_SuspendResumeCompleteEvents()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    // If not Yield here there won't be a SuspendAsyncContext.
                    // First call is a regular sync invocation (no continuation chain).
                    // Yield in Func will create an RuntimeAsyncTask with continuation chain
                    // and schedule on thread pool. When chain is resumed there will be
                    // ResumeAsyncContext and CompleteAsyncContext since the chain won't suspend again.
                    // The first Yield fixes that creating and schedule the RuntimeAsyncTask and Func
                    // will be called from the dispatch loop triggering the expected sequence of events.
                    await Task.Yield();
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);

            Assert.Contains(AsyncEventID.ResumeAsyncContext, eventIds);
            Assert.Contains(AsyncEventID.SuspendAsyncContext, eventIds);
            Assert.Contains(AsyncEventID.CompleteAsyncContext, eventIds);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_ContextEventIdLifecycle()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Task.Yield();
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var createIds = new List<ulong>();
            var resumeIds = new List<ulong>();

            ForEachEventBufferPayload(events, buffer =>
            {
                ParseEventBuffer(buffer, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId == AsyncEventID.CreateAsyncContext)
                    {
                        createIds.Add(ReadCompressedUInt64(buf, ref idx));
                        return true;
                    }
                    if (eventId == AsyncEventID.ResumeAsyncContext)
                    {
                        resumeIds.Add(ReadCompressedUInt64(buf, ref idx));
                        return true;
                    }
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });

            Assert.True(createIds.Count > 0, "Expected at least one CreateAsyncContext with id");
            Assert.True(resumeIds.Count > 0, "Expected at least one ResumeAsyncContext with id");

            Assert.All(createIds, id => Assert.True(id > 0, "CreateAsyncContext id should be non-zero"));
            Assert.All(resumeIds, id => Assert.True(id > 0, "ResumeAsyncContext id should be non-zero"));

            foreach (ulong resumeId in resumeIds)
            {
                Assert.Contains(resumeId, createIds);
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_ResumeCompleteMethodEvents()
        {
            var events = CollectEvents(MethodKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await FuncChained();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);

            Assert.Contains(AsyncEventID.ResumeAsyncMethod, eventIds);
            Assert.Contains(AsyncEventID.CompleteAsyncMethod, eventIds);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_EventSequenceOrder()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                // Same scenario as SuspendResumeCompleteEvents; here we verify ordering.
                RunScenarioAndFlush(async () =>
                {
                    await Task.Yield();
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var sortedEvents = CollectAsyncEventIdsWithTimestamps(events);
            var coreEvents = sortedEvents.FindAll(e => e.EventId == AsyncEventID.ResumeAsyncContext || e.EventId == AsyncEventID.SuspendAsyncContext || e.EventId == AsyncEventID.CompleteAsyncContext);

            Assert.Equal(AsyncEventID.ResumeAsyncContext, coreEvents[0].EventId);
            Assert.Equal(AsyncEventID.SuspendAsyncContext, coreEvents[1].EventId);
            Assert.Equal(AsyncEventID.ResumeAsyncContext, coreEvents[2].EventId);
            Assert.Equal(AsyncEventID.CompleteAsyncContext, coreEvents[3].EventId);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CreateAsyncContextEmittedOnFirstAwait()
        {
            var events = CollectEvents(CreateAsyncContextKeyword | CompleteAsyncContextKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);
            Assert.Contains(AsyncEventID.CreateAsyncContext, eventIds);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CreateAsyncCallstackEmittedOnFirstAwait()
        {
            var events = CollectEvents(CreateAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var callstacks = CollectCallstacks(events, AsyncEventID.CreateAsyncCallstack);

            Assert.NotEmpty(callstacks);
            Assert.All(callstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in create callstack");
                Assert.True(cs.TaskId != 0, "Expected non-zero task ID in create callstack");
                Assert.True(cs.Frames[0].NativeIP != 0, "Expected non-zero NativeIP in first frame");
            });
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CreateCallstackDepthMatchesChain()
        {
            var events = CollectEvents(CreateAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                // FuncChained -> FuncInner -> lambda: create callstack at FuncInner's
                // first await should reflect the 3-level chain.
                RunScenarioAndFlush(async () =>
                {
                    await FuncChained();
                });
            });

            // DumpCollectedEvents(events);

            var callstacks = CollectCallstacks(events, AsyncEventID.CreateAsyncCallstack);

            Assert.NotEmpty(callstacks);
            Assert.Contains(callstacks, cs => cs.FrameCount == 3);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_SuspendAsyncCallstackEmittedOnAwait()
        {
            var events = CollectEvents(SuspendAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    // First Yield pushes execution into the dispatch loop.
                    // Then Func()'s Yield triggers a suspend inside the loop
                    // where the SuspendAsyncCallstack event is emitted.
                    await Task.Yield();
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var callstacks = CollectCallstacks(events, AsyncEventID.SuspendAsyncCallstack);

            Assert.NotEmpty(callstacks);
            Assert.All(callstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in suspend callstack");
                Assert.True(cs.TaskId != 0, "Expected non-zero task ID in suspend callstack");
                Assert.True(cs.Frames[0].NativeIP != 0, "Expected non-zero NativeIP in first frame");
            });
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_SuspendCallstackDepthMatchesChain()
        {
            var events = CollectEvents(SuspendAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                // FuncChained -> FuncInner -> lambda: 3 levels deep when FuncInner suspends.
                RunScenarioAndFlush(async () =>
                {
                    await Task.Yield();
                    await FuncChained();
                });
            });

            // DumpCollectedEvents(events);

            var callstacks = CollectCallstacks(events, AsyncEventID.SuspendAsyncCallstack);

            Assert.NotEmpty(callstacks);
            Assert.Contains(callstacks, cs => cs.FrameCount == 3);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_SuspendCallstackPrecedesComplete()
        {
            // Use a single-level async method so all events belong to the same context.
            // This avoids ordering ambiguity from nested async calls.
            var events = CollectEvents(SuspendAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    // First Yield pushes into dispatch loop; second Yield triggers suspend.
                    await Task.Yield();
                    await Task.Yield();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIdsWithTimestamps(events);

            int suspendIdx = eventIds.FindIndex(e => e.EventId == AsyncEventID.SuspendAsyncCallstack);
            int completeIdx = eventIds.FindIndex(e => e.EventId == AsyncEventID.CompleteAsyncContext);

            Assert.True(suspendIdx >= 0, "Expected SuspendAsyncCallstack event");
            Assert.True(completeIdx >= 0, "Expected CompleteAsyncContext event");
            Assert.True(suspendIdx < completeIdx,
                $"SuspendAsyncCallstack (index {suspendIdx}) should precede CompleteAsyncContext (index {completeIdx})");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_SuspendCallstackDeeperThanInitialResume()
        {
            // After the initial Yield, the first resume is at the lambda level (depth 1).
            // Then FuncChained -> FuncInner builds the full chain and suspends at depth 3.
            // The suspend callstack should be deeper than the initial resume.
            var events = CollectEvents(
                ResumeAsyncCallstackKeyword | SuspendAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Task.Yield();
                    await FuncChained();
                });
            });

            // DumpCollectedEvents(events);

            var resumeStacks = CollectCallstacks(events, AsyncEventID.ResumeAsyncCallstack);
            var suspendStacks = CollectCallstacks(events, AsyncEventID.SuspendAsyncCallstack);

            Assert.NotEmpty(resumeStacks);
            Assert.NotEmpty(suspendStacks);

            // The shallowest resume is after the initial Yield (just the lambda).
            // The deepest suspend captures the full chain (FuncInner -> FuncChained -> lambda).
            // Use min/max to avoid cross-buffer ordering dependence.
            byte minResumeDepth = resumeStacks.Min(cs => cs.FrameCount);
            byte maxSuspendDepth = suspendStacks.Max(cs => cs.FrameCount);

            Assert.True(maxSuspendDepth > minResumeDepth,
                $"Suspend callstack depth ({maxSuspendDepth}) should be deeper than shallowest resume callstack depth ({minResumeDepth})");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CreateCallstackPrecedesResumeCallstack()
        {
            var events = CollectEvents(CreateAsyncContextKeyword | CreateAsyncCallstackKeyword | ResumeAsyncContextKeyword | ResumeAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            // Collect all callstack events with their task IDs sorted by timestamp.
            var callstackEvents = new List<(AsyncEventID EventId, ulong TaskId, long Timestamp)>();
            ForEachEventBufferPayload(events, buffer =>
            {
                ParseEventBuffer(buffer, (AsyncEventID eventId, long timestamp, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId is AsyncEventID.CreateAsyncCallstack or AsyncEventID.ResumeAsyncCallstack)
                    {
                        ReadCallstackPayload(buf, ref idx, out ulong taskId, out byte _, out _);
                        callstackEvents.Add((eventId, taskId, timestamp));
                        return true;
                    }
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });
            callstackEvents.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // For each task that has both Create and Resume, verify Create comes first.
            var taskIds = callstackEvents.Where(e => e.EventId == AsyncEventID.CreateAsyncCallstack).Select(e => e.TaskId).ToHashSet();
            Assert.NotEmpty(taskIds);

            foreach (ulong taskId in taskIds)
            {
                int createIdx = callstackEvents.FindIndex(e => e.EventId == AsyncEventID.CreateAsyncCallstack && e.TaskId == taskId);
                int resumeIdx = callstackEvents.FindIndex(e => e.EventId == AsyncEventID.ResumeAsyncCallstack && e.TaskId == taskId);

                Assert.True(createIdx >= 0, $"Expected CreateAsyncCallstack for task {taskId}");
                Assert.True(resumeIdx >= 0, $"Expected ResumeAsyncCallstack for task {taskId}");
                Assert.True(createIdx < resumeIdx,
                    $"For task {taskId}: CreateAsyncCallstack (index {createIdx}) should precede ResumeAsyncCallstack (index {resumeIdx})");
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CreateAndFirstResumeCallstacksMatch()
        {
            var events = CollectEvents(CreateAsyncCallstackKeyword | ResumeAsyncCallstackKeyword | CompleteAsyncContextKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var createStacks = CollectCallstacks(events, AsyncEventID.CreateAsyncCallstack);
            var resumeStacks = CollectCallstacks(events, AsyncEventID.ResumeAsyncCallstack);

            Assert.NotEmpty(createStacks);
            Assert.NotEmpty(resumeStacks);

            foreach (var (taskId, _, createFrames) in createStacks)
            {
                var matchingResume = resumeStacks.FirstOrDefault(r => r.TaskId == taskId);
                Assert.True(matchingResume.Frames is not null,
                    $"Expected a ResumeAsyncCallstack for task {taskId}");

                Assert.Equal(createFrames.Count, matchingResume.Frames!.Count);
                for (int i = 0; i < createFrames.Count; i++)
                {
                    Assert.Equal(createFrames[i].NativeIP, matchingResume.Frames[i].NativeIP);
                }
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackEmittedOnResume()
        {
            var events = CollectEvents(CallstackKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var callstacks = CollectCallstacks(events);

            Assert.NotEmpty(callstacks);
            Assert.All(callstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in callstack");
                Assert.True(cs.TaskId != 0, "Expected non-zero task ID in resume callstack");
                Assert.True(cs.Frames[0].NativeIP != 0, "Expected non-zero NativeIP in first frame");
            });
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackDepthMatchesChain()
        {
            var events = CollectEvents(CallstackKeywords, () =>
            {
                // FuncChained -> FuncInner -> lambda: 3 levels deep after FuncInner yields.
                RunScenarioAndFlush(async () =>
                {
                    await FuncChained();
                });
            });

            // DumpCollectedEvents(events);

            var callstacks = CollectCallstacks(events);

            Assert.NotEmpty(callstacks);
            Assert.Contains(callstacks, cs => cs.FrameCount == 3);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackSimulation_NormalCompletion()
        {
            var events = CollectEvents(CallstackKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await FuncChained();
                });
            });

            // DumpCollectedEvents(events);

            AssertCallstackSimulationReachesZero(events);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackSimulation_HandledException()
        {
            var events = CollectEvents(CallstackKeywords, () =>
            {
                // DeepOuterCatches -> DeepMiddle -> DeepInnerThrows: exception is caught
                // within the chain. Unwind pops 2 frames, execution resumes in outer.
                RunScenarioAndFlush(async () =>
                {
                    await DeepOuterCatches();
                });
            });

            // DumpCollectedEvents(events);

            AssertCallstackSimulationReachesZero(events);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackSimulation_UnhandledException()
        {
            var events = CollectEvents(CallstackKeywords, () =>
            {
                // DeepUnhandledOuter -> DeepUnhandledMiddle -> DeepUnhandledInnerThrows:
                // no catch in the chain. Unwind pops all 3 frames, task faults.
                Task task = Task.Run(DeepUnhandledOuter);
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (InvalidOperationException)
                {
                }
                SendFlushCommand();
            });

            // DumpCollectedEvents(events);

            AssertCallstackSimulationReachesZero(events);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_UnhandledExceptionUnwind()
        {
            var events = CollectEvents(UnwindAsyncExceptionKeyword | CoreKeywords, () =>
            {
                // lambda -> DeepUnhandledOuter -> DeepUnhandledMiddle -> DeepUnhandledInnerThrows (4 levels).
                // No try/catch in the chain — UnwindToPossibleHandler returns null,
                // triggering the unhandled exception path which faults the task.
                // unwindedFrames starts at 1 (current) + walks 2 more continuations = 3.
                try
                {
                    RunScenario(async () =>
                    {
                        await DeepUnhandledOuter();
                    });
                }
                catch (InvalidOperationException)
                {
                }

                SendFlushCommand();
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);
            var frameCounts = CollectUnwindFrameCounts(events);

            Assert.Contains(AsyncEventID.ResumeAsyncContext, eventIds);
            Assert.Contains(AsyncEventID.UnwindAsyncException, eventIds);
            Assert.Contains(AsyncEventID.CompleteAsyncContext, eventIds);

            Assert.NotEmpty(frameCounts);
            Assert.All(frameCounts, count => Assert.Equal(4u, count));
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_HandledExceptionUnwind()
        {
            var events = CollectEvents(UnwindAsyncExceptionKeyword | CoreKeywords, () =>
            {
                // DeepOuterCatches -> DeepMiddle -> DeepInnerThrows (3 levels).
                // DeepOuterCatches has try/catch — UnwindToPossibleHandler finds the handler.
                // unwindedFrames starts at 1 (current) + walks 1 to find handler = 2.
                RunScenarioAndFlush(async () =>
                {
                    await DeepOuterCatches();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);
            var frameCounts = CollectUnwindFrameCounts(events);

            Assert.Contains(AsyncEventID.ResumeAsyncContext, eventIds);
            Assert.Contains(AsyncEventID.UnwindAsyncException, eventIds);
            Assert.Contains(AsyncEventID.CompleteAsyncContext, eventIds);

            Assert.NotEmpty(frameCounts);
            Assert.All(frameCounts, count => Assert.Equal(2u, count));
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexMatchesCallstack()
        {
            var captures = new List<(string MethodName, int WrapperSlot)>();
            ulong scenarioThreadId = 0;
            long scenarioTimestamp = 0;

            var events = CollectEvents(CallstackKeywords, () =>
            {
                // Capture a timestamp just before the scenario runs.
                // The callstack event closest after this timestamp on the
                // scenario thread is the one we want — simulating how a CPU
                // sampler would correlate a sample with a callstack.
                scenarioTimestamp = Stopwatch.GetTimestamp();

                // WrapperTestA -> WrapperTestB -> WrapperTestC.
                // Each method captures which Continuation_Wrapper_N dispatched it.
                RunScenarioAndFlush(async () =>
                {
                    await WrapperTestA(captures);
                    scenarioThreadId = GetCurrentOSThreadId();
                });
            });

            // DumpCollectedEvents(events);

            Assert.True(scenarioThreadId != 0, "Failed to capture scenario thread ID");
            Assert.True(captures.Count == 3, $"Expected 3 wrapper captures, got {captures.Count}");
            Assert.All(captures, c => Assert.True(c.WrapperSlot >= 0, $"{c.MethodName} did not find Continuation_Wrapper_N on stack (slot={c.WrapperSlot})"));

            int slotC = captures.First(c => c.MethodName == nameof(WrapperTestC)).WrapperSlot;
            int slotB = captures.First(c => c.MethodName == nameof(WrapperTestB)).WrapperSlot;
            int slotA = captures.First(c => c.MethodName == nameof(WrapperTestA)).WrapperSlot;

            Assert.Equal(slotC + 1, slotB);
            Assert.Equal(slotB + 1, slotA);

            var chainStack = FindCallstackAfterTimestamp(events, scenarioThreadId, scenarioTimestamp);

            Assert.True(chainStack.HasValue, "No callstack found after scenario timestamp on scenario thread");
            Assert.True(chainStack.Value.FrameCount == 4, $"Expected callstack with 4 frames, got {chainStack.Value.FrameCount}");

            var resolvedNames = new List<string>();
            foreach (var (nativeIP, _) in chainStack.Value.Frames)
            {
                var method = GetMethodFromNativeIP(nativeIP);
                resolvedNames.Add(method?.Name ?? "<unknown>");
            }

            Assert.Equal(nameof(WrapperTestC), resolvedNames[slotC]);
            Assert.Equal(nameof(WrapperTestB), resolvedNames[slotB]);
            Assert.Equal(nameof(WrapperTestA), resolvedNames[slotA]);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexResetEmitted()
        {
            var events = CollectEvents(AllKeywords, () =>
            {
                // Recursive chain 34 levels deep crosses the 32-slot boundary,
                // triggering at least one ResetAsyncContinuationWrapperIndex event.
                RunScenarioAndFlush(async () =>
                {
                    await RecursiveFunc(34);
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);

            Assert.Contains(AsyncEventID.ResetAsyncContinuationWrapperIndex, eventIds);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexNoResetUnder32()
        {
            var events = CollectEvents(AllKeywords, () =>
            {
                // A shallow chain stays within the first 32 slots —
                // no reset event should be emitted.
                RunScenarioAndFlush(async () =>
                {
                    await RecursiveFunc(2);
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);

            Assert.DoesNotContain(AsyncEventID.ResetAsyncContinuationWrapperIndex, eventIds);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_PeriodicTimerFlush()
        {
            var events = CollectEvents(CoreKeywords, (collectedEvents, _) =>
            {
                // Run scenario — do NOT flush explicitly afterwards.
                RunScenario(async () =>
                {
                    await Func();
                });

                // Wait for the periodic flush timer (1s interval) to detect the idle buffer and flush it automatically.
                Thread.Sleep(1000);

                // Poll to make sure the expected buffer got flush.
                bool flushed = SpinWait.SpinUntil(() =>
                {
                    var ids = CollectAsyncEventIds(collectedEvents);
                    return ids.Exists(id => id == AsyncEventID.ResumeAsyncContext || id == AsyncEventID.SuspendAsyncContext || id == AsyncEventID.CompleteAsyncContext);
                }, TimeSpan.FromSeconds(20));

                Assert.True(flushed, "Expected periodic timer to flush buffer with core lifecycle events within timeout");
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);
            int coreEventCount = eventIds.FindAll(id => id == AsyncEventID.ResumeAsyncContext || id == AsyncEventID.SuspendAsyncContext || id == AsyncEventID.CompleteAsyncContext).Count;

            Assert.True(coreEventCount > 0, "Expected periodic timer to flush buffer with core lifecycle events");
        }

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
            var workerEvents = new ConcurrentQueue<EventWrittenEventArgs>();

            using (var listener = CreateListener(CoreKeywords))
            {
                listener.RunWithCallback(e =>
                {
                    if (!workerIdReady.IsSet)
                        return;
                    if (e.EventId != AsyncEventsId || e.Payload is null || e.Payload.Count == 0)
                        return;
                    if (e.Payload[0] is not byte[] payload)
                        return;
                    EventBufferHeader? header = ParseEventBufferHeader(payload);
                    if (header is not null && header.Value.OsThreadId == workerOsThreadId)
                        workerEvents.Enqueue(e);
                }, () =>
                {
                    SendFlushCommand();

                    var thread = new Thread(() =>
                    {
                        workerOsThreadId = GetCurrentOSThreadId();
                        workerIdReady.Set();

                        // First batch: generate events on this thread's profiler context.
                        Func().GetAwaiter().GetResult();
                        firstBatchDone.Set();

                        // Wait for the flush to deliver our first buffer before generating more events.
                        bool flushed = firstFlushSeen.Wait(TimeSpan.FromSeconds(20));
                        Assert.True(flushed, "Expected first flush of core lifecycle events within timeout");

                        // Second batch: generate more events on the same thread's context.
                        Func().GetAwaiter().GetResult();
                    });

                    thread.IsBackground = true;
                    thread.Start();

                    // Wait for the worker to finish its first batch, then force flush.
                    firstBatchDone.Wait(TimeSpan.FromSeconds(20));
                    SendFlushCommand();

                    // Poll for first buffer from our worker thread.
                    bool firstFlush = SpinWait.SpinUntil(() => workerEvents.Count >= 1, TimeSpan.FromSeconds(20));
                    Assert.True(firstFlush, "Expected periodic timer to flush core lifecycle events within timeout");

                    firstFlushSeen.Set();

                    // Wait for the worker to finish its second batch.
                    bool joined = thread.Join(TimeSpan.FromSeconds(20));
                    Assert.True(joined, "Expected worker thread to terminate within timeout after second batch of work");

                    // Force a flush to deliver the second batch.
                    SendFlushCommand();

                    // Poll for second buffer from our worker thread.
                    bool secondFlush = SpinWait.SpinUntil(() => workerEvents.Count >= 2, TimeSpan.FromSeconds(20));
                    Assert.True(secondFlush, "Expected periodic timer to flush core lifecycle events within timeout");
                });
            }

            // DumpCollectedEvents(workerEvents);

            Assert.True(workerOsThreadId != 0, "Failed to capture worker OS thread ID");

            // The key assertion: find buffers that contain CreateAsyncContext events (our work batches).
            // There must be at least 2 such buffers (one per Func() call), and ALL of them must
            // have the worker's OsThreadId — proving the timer flush didn't corrupt the header.
            int workBufferCount = 0;
            foreach (EventWrittenEventArgs e in workerEvents)
            {
                if (e.EventId != AsyncEventsId || e.Payload is null || e.Payload.Count == 0)
                    continue;
                if (e.Payload[0] is not byte[] payload)
                    continue;

                bool hasCreateEvent = false;
                ParseEventBuffer(payload, (AsyncEventID eventId, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId == AsyncEventID.CreateAsyncContext)
                        hasCreateEvent = true;
                    return SkipEventPayload(eventId, buf, ref idx);
                });

                if (hasCreateEvent)
                {
                    workBufferCount++;
                    EventBufferHeader? header = ParseEventBufferHeader(payload);
                    Assert.NotNull(header);
                    Assert.Equal(workerOsThreadId, header.Value.OsThreadId);
                }
            }

            Assert.True(workBufferCount >= 2, $"Expected at least 2 buffers with CreateAsyncContext from the worker thread, got {workBufferCount}");
        }


        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_DeadThreadFlush()
        {
            var events = CollectEvents(CoreKeywords, (collectedEvents, _) =>
            {
                // Spawn a dedicated thread that runs async work then exits.
                // Its thread-local buffer becomes orphaned when the thread dies.
                var thread = new Thread(() =>
                {
                    RunScenario(async () =>
                    {
                        await Func();
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
                    var ids = CollectAsyncEventIds(collectedEvents);
                    return ids.Exists(id => id == AsyncEventID.ResumeAsyncContext || id == AsyncEventID.SuspendAsyncContext || id == AsyncEventID.CompleteAsyncContext);
                }, TimeSpan.FromSeconds(20));

                Assert.True(flushed, "Expected periodic timer to flush buffer with core lifecycle events within timeout");
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);
            int coreEventCount = eventIds.FindAll(id => id == AsyncEventID.ResumeAsyncContext || id == AsyncEventID.SuspendAsyncContext || id == AsyncEventID.CompleteAsyncContext).Count;

            Assert.True(coreEventCount > 0, "Expected periodic timer to flush dead thread's buffer");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_NoSyncClockEventBeforeInterval()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);

            Assert.DoesNotContain(AsyncEventID.AsyncProfilerSyncClock, eventIds);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_NoEventsWhenDisabled()
        {
            // Run async work WITHOUT a listener attached
            Task.Run(async () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    await Func();
                }
            }).GetAwaiter().GetResult();

            // Now attach listener and verify no stale events are emitted
            var events = CollectEvents(CoreKeywords, () =>
            {
                // Don't run any async work - just check nothing comes through from before
                Thread.Sleep(100);
            });

            // DumpCollectedEvents(events);

            // There may be a ResetAsyncThreadContext from the SyncPoint when keywords change,
            // but there should be no suspend/resume/complete events from the earlier work.
            var eventIds = CollectAsyncEventIds(events);
            int contextEvents = eventIds.FindAll(id => id == AsyncEventID.ResumeAsyncContext || id == AsyncEventID.SuspendAsyncContext || id == AsyncEventID.CompleteAsyncContext).Count;

            Assert.Equal(0, contextEvents);
        }

        public static IEnumerable<object[]> KeywordGatekeepingData()
        {
            yield return new object[] { (long)CreateAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CreateAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)ResumeAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.ResumeAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)SuspendAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.SuspendAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)CompleteAsyncContextKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CompleteAsyncContext, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)UnwindAsyncExceptionKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.UnwindAsyncException, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)CreateAsyncCallstackKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CreateAsyncCallstack, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)ResumeAsyncCallstackKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.ResumeAsyncCallstack, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)SuspendAsyncCallstackKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.SuspendAsyncCallstack, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)ResumeAsyncMethodKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.ResumeAsyncMethod, AsyncEventID.AsyncProfilerMetadata } };
            yield return new object[] { (long)CompleteAsyncMethodKeyword, new AsyncEventID[] { AsyncEventID.ResetAsyncThreadContext, AsyncEventID.CompleteAsyncMethod, AsyncEventID.AsyncProfilerMetadata } };
        }

        [ConditionalTheory(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        [MemberData(nameof(KeywordGatekeepingData))]
        public void RuntimeAsync_KeywordGatekeeping(long keywordValue, AsyncEventID[] allowedEventIds)
        {
            EventKeywords kw = (EventKeywords)keywordValue;
            var allowed = new HashSet<AsyncEventID>(allowedEventIds);

            var events = CollectEvents(kw, () =>
            {
                // Run a scenario that exercises all event types: resume, suspend,
                // complete, method events, callstacks, and exception unwinds.
                // Only the events matching the enabled keyword should be emitted.
                RunScenarioAndFlush(async () =>
                {
                    await OuterCatches();
                    await FuncChained();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);
            var unexpected = eventIds.FindAll(id => !allowed.Contains(id));

            Assert.True(unexpected.Count == 0,
                $"Keyword 0x{(long)kw:X}: unexpected event IDs [{string.Join(", ", unexpected)}], " +
                $"allowed [{string.Join(", ", allowed)}]");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_ResetAsyncThreadContextEvent()
        {
            var events = CollectEvents(CoreKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var eventIds = CollectAsyncEventIds(events);

            Assert.Contains(AsyncEventID.ResetAsyncThreadContext, eventIds);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_MetadataEventEmittedOnEnable()
        {
            var events = CollectEvents(AllKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var metadataList = CollectMetadataFromBuffer(events);
            Assert.True(metadataList.Count >= 1, "Expected at least one metadata event in buffer");

            MetadataFromBuffer meta = metadataList[0];
            Assert.True(meta.QpcFrequency > 0, $"QPC frequency should be positive, got {meta.QpcFrequency}");
            Assert.True(meta.QpcSync > 0, $"QPC sync timestamp should be positive, got {meta.QpcSync}");
            Assert.True(meta.UtcSync > 0, $"UTC sync timestamp should be positive, got {meta.UtcSync}");
            Assert.True(meta.EventBufferSize > 0, $"Event buffer size should be positive, got {meta.EventBufferSize}");
            Assert.True(meta.WrapperIPs.Length > 0, "Wrapper IPs array should not be empty");
            Assert.All(meta.WrapperIPs, ip => Assert.True(ip != 0, "Each wrapper IP should be non-zero"));
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_MetadataEventEmittedOnceAcrossThreads()
        {
            const int threadCount = 8;

            var events = CollectEvents(AllKeywords, () =>
            {
                using var barrier = new Barrier(threadCount);
                var tasks = new Task[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    tasks[i] = Task.Factory.StartNew(() =>
                    {
                        barrier.SignalAndWait();
                        Func().GetAwaiter().GetResult();
                    }, TaskCreationOptions.LongRunning);
                }
                Task.WaitAll(tasks);
                SendFlushCommand();
            });

            // DumpCollectedEvents(events);

            var metadataList = CollectMetadataFromBuffer(events);
            Assert.True(metadataList.Count == 1, $"Expected exactly 1 metadata event across {threadCount} threads, got {metadataList.Count}");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackNativeIPDeltaRoundtrip()
        {
            // Verify that delta-encoded NativeIPs in callstacks roundtrip correctly,
            // including both positive and negative deltas. With multiple distinct async
            // methods at different JIT-assigned addresses, the deltas between consecutive
            // NativeIPs will naturally span both directions. This exercises the full
            // zigzag + LEB128 encode/decode path through the production serializer.
            var events = CollectEvents(CallstackKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    // Run several different call chains to maximize address variation.
                    await FuncChained();
                    await DeepOuterCatches();
                    await RecursiveFunc(10);
                });
            });

            var callstacks = CollectCallstacks(events);
            Assert.NotEmpty(callstacks);

            // Find callstacks with 3+ frames — enough depth for meaningful deltas.
            var deepCallstacks = callstacks.Where(cs => cs.FrameCount >= 3).ToList();
            Assert.True(deepCallstacks.Count > 0,
                "Expected at least one callstack with 3+ frames for delta verification");

            bool hasPositiveDelta = false;
            bool hasNegativeDelta = false;

            foreach (var cs in deepCallstacks)
            {
                for (int i = 0; i < cs.Frames.Count; i++)
                {
                    var (nativeIP, _) = cs.Frames[i];
                    Assert.True(nativeIP != 0, $"Frame {i} has zero NativeIP");
                    var method = GetMethodFromNativeIP(nativeIP);
                    Assert.True(method is not null,
                        $"Frame {i}: NativeIP 0x{nativeIP:X} does not resolve to a managed method");

                    if (i > 0)
                    {
                        long delta = (long)(cs.Frames[i].NativeIP - cs.Frames[i - 1].NativeIP);
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
            Assert.True(hasPositiveDelta || hasNegativeDelta,
                "Expected at least one non-zero NativeIP delta across all callstack frames");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackStressWithVaryingDepths()
        {
            // Stress test: run many async calls with varying callstack depths.
            // Varying sizes mean some callstacks will land at buffer boundaries,
            // naturally exercising the overflow/rewind path in callstack emission.
            // RecursiveFunc(d) produces exactly d frames on the chain. The lambda
            // that calls it adds one more frame, so the total callstack depth is d + 1.
            const int iterations = 200;
            int[] depths = new int[iterations];
            var rng = new Random(42);
            for (int i = 0; i < iterations; i++)
                depths[i] = rng.Next(1, 120);

            var events = CollectEvents(CallstackKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    for (int i = 0; i < iterations; i++)
                        await RecursiveFunc(depths[i]);
                });
            });

            // DumpCollectedEvents(events);

            // Collect all resume callstacks with timestamps, sorted by timestamp.
            var callstacksWithTimestamp = new List<(long Timestamp, byte FrameCount, List<(ulong NativeIP, int State)> Frames)>();
            ForEachEventBufferPayload(events, buffer =>
            {
                ParseEventBuffer(buffer, (AsyncEventID eventId, long timestamp, ReadOnlySpan<byte> buf, ref int idx) =>
                {
                    if (eventId == AsyncEventID.ResumeAsyncCallstack)
                    {
                        ReadCallstackPayload(buf, ref idx, out byte frameCount, out var frames);
                        callstacksWithTimestamp.Add((timestamp, frameCount, frames));
                        return true;
                    }
                    return SkipEventPayload(eventId, buf, ref idx);
                });
            });

            callstacksWithTimestamp.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Verify all callstacks have valid frame data that resolves to managed methods.
            foreach (var cs in callstacksWithTimestamp)
            {
                Assert.True(cs.FrameCount > 0, "Callstack has 0 frames");
                Assert.Equal(cs.FrameCount, cs.Frames.Count);
                for (int f = 0; f < cs.Frames.Count; f++)
                {
                    var (nativeIP, _) = cs.Frames[f];
                    Assert.True(nativeIP != 0, $"Frame {f} has zero NativeIP");
                    var method = GetMethodFromNativeIP(nativeIP);
                    Assert.True(method is not null,
                        $"Frame {f}: NativeIP 0x{nativeIP:X} does not resolve to a managed method");
                }
            }

            // One resume callstack per iteration; find our sequence at the end
            // (earlier entries may be from metadata/warmup).
            Assert.True(callstacksWithTimestamp.Count >= iterations,
                $"Expected at least {iterations} callstacks, got {callstacksWithTimestamp.Count}");

            int startOffset = callstacksWithTimestamp.Count - iterations;
            for (int i = 0; i < iterations; i++)
            {
                int expected = depths[i] + 1;
                int actual = callstacksWithTimestamp[startOffset + i].FrameCount;
                Assert.True(actual == expected,
                    $"Iteration {i}: expected depth {expected} (RecursiveFunc({depths[i]}) + lambda), got {actual}");
            }

            // Verify multiple buffer flushes occurred.
            int bufferCount = 0;
            ForEachEventBufferPayload(events, _ => bufferCount++);
            Assert.True(bufferCount >= 3, $"Expected at least 3 buffer flushes, got {bufferCount}");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackOverflowPathProducesValidFrames()
        {
            // Targeted test: run random-depth callstacks until we detect the overflow
            // path was exercised, then validate the affected callstack.
            // The overflow path fires when a large callstack doesn't fit inline in the
            // remaining buffer space — the code rewinds, flushes, and re-writes the
            // callstack as the first event in a fresh buffer.
            bool overflowDetected = false;
            var rng = new Random(42);

            for (int attempt = 0; attempt < 10 && !overflowDetected; attempt++)
            {
                int iterations = 500;
                int[] depths = new int[iterations];
                for (int i = 0; i < iterations; i++)
                    depths[i] = rng.Next(50, 250);

                var events = CollectEvents(ResumeAsyncCallstackKeyword, () =>
                {
                    RunScenarioAndFlush(async () =>
                    {
                        for (int i = 0; i < iterations; i++)
                            await RecursiveFunc(depths[i]);
                    });
                });

                // Check each buffer: if the first event is a large ResumeAsyncCallstack,
                // the overflow path flushed the previous buffer and re-wrote here.
                ForEachEventBufferPayload(events, buffer =>
                {
                    if (overflowDetected)
                        return;

                    int index = HeaderSize;
                    if (index + 2 > buffer.Length)
                        return;

                    AsyncEventID firstEvent = (AsyncEventID)buffer[index++];
                    ReadCompressedUInt64(buffer, ref index);
                    if (firstEvent != AsyncEventID.ResumeAsyncCallstack)
                        return;

                    ReadCallstackPayload(buffer, ref index, out byte frameCount, out var frames);
                    if (frameCount <= 30)
                        return;

                    overflowDetected = true;

                    Assert.Equal(frameCount, frames.Count);
                    for (int f = 0; f < frames.Count; f++)
                    {
                        var (nativeIP, _) = frames[f];
                        Assert.True(nativeIP != 0, $"Overflow callstack frame {f} has zero NativeIP");
                        var method = GetMethodFromNativeIP(nativeIP);
                        Assert.True(method is not null,
                            $"Overflow callstack frame {f}: NativeIP 0x{nativeIP:X} does not resolve to a managed method");
                    }
                });
            }

            Assert.True(overflowDetected,
                "Failed to trigger callstack buffer overflow after 10 attempts");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_CallstackDepthCappedAtMaxFrames()
        {
            // Verify that callstack depth is capped when the continuation chain
            // exceeds the maximum frame count (255, limited by byte storage).
            // RecursiveFunc(300) produces a 300-deep chain + 1 lambda = 301 frames.
            const int requestedDepth = 300;

            var events = CollectEvents(ResumeAsyncCallstackKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await RecursiveFunc(requestedDepth);
                });
            });

            // DumpCollectedEvents(events);

            var callstacks = CollectCallstacks(events);
            Assert.True(callstacks.Count >= 1, "Expected at least one callstack");

            // Find the callstack from our deep RecursiveFunc call.
            // The max frame count is capped at 255 (byte.MaxValue) since the
            // CaptureRuntimeAsyncCallstackState.Count is a byte.
            // RecursiveFunc(300) + 1 lambda = 301 frames, capped to 255.
            var deepest = callstacks.MaxBy(cs => cs.FrameCount);
            Assert.Equal(255, deepest.FrameCount);
            Assert.Equal(deepest.FrameCount, deepest.Frames.Count);

            // Verify all frames are valid.
            foreach (var (nativeIP, _) in deepest.Frames)
            {
                Assert.True(nativeIP != 0, "Frame has zero NativeIP");
                var method = GetMethodFromNativeIP(nativeIP);
                Assert.True(method is not null,
                    $"NativeIP 0x{nativeIP:X} does not resolve to a managed method");
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_MetadataWrapperIPsMatchMethods()
        {
            var events = CollectEvents(AllKeywords, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await Func();
                });
            });

            // DumpCollectedEvents(events);

            var metadataList = CollectMetadataFromBuffer(events);
            Assert.True(metadataList.Count >= 1, "Expected at least one metadata event in buffer");

            long[] wrapperIPs = metadataList[0].WrapperIPs;

            Type? cwType = typeof(object).Assembly.GetType("System.Runtime.CompilerServices.AsyncProfiler+ContinuationWrapper");
            Assert.NotNull(cwType);

            for (int i = 0; i < wrapperIPs.Length; i++)
            {
                string expectedName = $"Continuation_Wrapper_{i}";
                MethodInfo? method = cwType.GetMethod(expectedName, BindingFlags.NonPublic | BindingFlags.Static);
                Assert.True(method is not null, $"Expected method '{expectedName}' to exist on ContinuationWrapper type");

                System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
                long expectedIP = method.MethodHandle.GetFunctionPointer().ToInt64();

                Assert.True(wrapperIPs[i] == expectedIP,
                    $"Wrapper IP mismatch at index {i}: metadata has 0x{wrapperIPs[i]:X}, " +
                    $"method '{expectedName}' has 0x{expectedIP:X}");
            }
        }
    }

    internal static class EventBuffer
    {
        public static int OutputEventBuffer(ReadOnlySpan<byte> buffer)
        {
            Console.WriteLine("--- AsyncEvents ---");

            int index = 0;

            if ((uint)buffer.Length < 1)
            {
                Console.WriteLine("Buffer too small.");
                Console.WriteLine("----------------------------------");
                return index;
            }

            byte version = buffer[index++];
            Console.WriteLine($"Version: {version}");

            if (version != 1)
            {
                Console.WriteLine($"Unsupported version: {version}");
                Console.WriteLine("----------------------------------");
                return index;
            }

            Deserializer.ReadUInt32(buffer, ref index, out uint totalSize);
            Deserializer.ReadUInt32(buffer, ref index, out uint contextId);
            Deserializer.ReadUInt64(buffer, ref index, out ulong osThreadId);
            Deserializer.ReadUInt32(buffer, ref index, out uint totalEventCount);
            Deserializer.ReadUInt64(buffer, ref index, out ulong startTimestamp);
            Deserializer.ReadUInt64(buffer, ref index, out ulong endTimestamp);

            Console.WriteLine($"TotalSize (bytes): {totalSize}");
            Console.WriteLine($"AsyncThreadContextId: {contextId}");
            Console.WriteLine($"OSThreadId: {osThreadId}");
            Console.WriteLine($"TotalEventCount: {totalEventCount}");
            Console.WriteLine($"StartTimestamp: 0x{startTimestamp:X16}");
            Console.WriteLine($"EndTimestamp: 0x{endTimestamp:X16}");

            int eventCount = 0;
            ulong currentTimestamp = startTimestamp;

            while (index < buffer.Length)
            {
                if (index + 2 > buffer.Length)
                {
                    Console.WriteLine($"Trailing bytes: {buffer.Length - index} (incomplete entry header).");
                    break;
                }

                AsyncEventID eventId = (AsyncEventID)buffer[index++];

                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong delta);
                currentTimestamp += delta;

                Console.WriteLine($"Entry[{eventCount}]: Timestamp=0x{currentTimestamp:X16}, EventId={eventId}");

                int payloadStart = index;
                try
                {
                    index += eventId switch
                    {
                        AsyncEventID.CreateAsyncContext => OutputCreateAsyncContextEvent(buffer.Slice(index)),
                        AsyncEventID.ResumeAsyncContext => OutputResumeAsyncContextEvent(buffer.Slice(index)),
                        AsyncEventID.SuspendAsyncContext => OutputSuspendAsyncContextEvent(),
                        AsyncEventID.CompleteAsyncContext => OutputCompleteAsyncContextEvent(),
                        AsyncEventID.UnwindAsyncException => OutputUnwindAsyncExceptionEvent(buffer.Slice(index)),
                        AsyncEventID.CreateAsyncCallstack => OutputAsyncCallstackEvent("CreateAsyncCallstack", buffer.Slice(index)),
                        AsyncEventID.ResumeAsyncCallstack => OutputAsyncCallstackEvent("ResumeAsyncCallstack", buffer.Slice(index)),
                        AsyncEventID.SuspendAsyncCallstack => OutputAsyncCallstackEvent("SuspendAsyncCallstack", buffer.Slice(index)),
                        AsyncEventID.ResumeAsyncMethod => OutputResumeAsyncMethodEvent(),
                        AsyncEventID.CompleteAsyncMethod => OutputCompleteAsyncMethodEvent(),
                        AsyncEventID.ResetAsyncThreadContext => OutputResetAsyncThreadContextEvent(),
                        AsyncEventID.ResetAsyncContinuationWrapperIndex => OutputResetAsyncContinuationWrapperIndexEvent(),
                        AsyncEventID.AsyncProfilerMetadata => OutputAsyncProfilerMetadataEvent(buffer.Slice(index)),
                        _ => throw new InvalidOperationException($"Unknown eventId {eventId}."),
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Failed decoding entry payload at offset {payloadStart}: {ex.GetType().Name}: {ex.Message}");
                    break;
                }

                eventCount++;
            }

            Console.WriteLine($"TotalEntriesDecoded: {eventCount}");
            Console.WriteLine("----------------------------------");

            return index;
        }

        private static int OutputCreateAsyncContextEvent(ReadOnlySpan<byte> buffer)
        {
            int index = 0;
            Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong id);
            Console.WriteLine("--- CreateAsyncContext ---");
            Console.WriteLine($"  ID: {id}");
            Console.WriteLine("----------------------------");
            return index;
        }

        private static int OutputResumeAsyncContextEvent(ReadOnlySpan<byte> buffer)
        {
            int index = 0;
            Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong id);
            Console.WriteLine("--- ResumeAsyncContext ---");
            Console.WriteLine($"  ID: {id}");
            Console.WriteLine("----------------------------");
            return index;
        }

        private static int OutputSuspendAsyncContextEvent()
        {
            Console.WriteLine("--- SuspendAsyncContext ---");
            Console.WriteLine("----------------------------");
            return 0;
        }

        private static int OutputCompleteAsyncContextEvent()
        {
            Console.WriteLine("--- CompleteAsyncContext ---");
            Console.WriteLine("----------------------------");
            return 0;
        }

        private static int OutputUnwindAsyncExceptionEvent(ReadOnlySpan<byte> buffer)
        {
            uint unwindedFrames;
            int index = 0;

            Deserializer.ReadCompressedUInt32(buffer, ref index, out unwindedFrames);
            index += OutputUnwindAsyncExceptionEvent(unwindedFrames);

            return index;
        }

        private static int OutputUnwindAsyncExceptionEvent(uint unwindedFrames)
        {
            Console.WriteLine("--- UnwindAsyncException ---");
            Console.WriteLine($"Unwinded Frames: {unwindedFrames}");
            Console.WriteLine("----------------------------");
            return 0;
        }

        private static int OutputResumeAsyncMethodEvent()
        {
            Console.WriteLine("--- ResumeAsyncMethod ---");
            Console.WriteLine("----------------------------");
            return 0;
        }

        private static int OutputCompleteAsyncMethodEvent()
        {
            Console.WriteLine("--- CompleteAsyncMethod ---");
            Console.WriteLine("----------------------------");
            return 0;
        }

        private static int OutputResetAsyncContinuationWrapperIndexEvent()
        {
            Console.WriteLine("--- ResetAsyncContinuationWrapperIndex ---");
            Console.WriteLine("----------------------------");
            return 0;
        }

        private static int OutputResetAsyncThreadContextEvent()
        {
            Console.WriteLine("--- ResetAsyncThreadContext ---");
            Console.WriteLine("----------------------------");
            return 0;
        }

        private static int OutputAsyncProfilerMetadataEvent(ReadOnlySpan<byte> buffer)
        {
            int index = 0;
            Console.WriteLine("--- AsyncProfilerMetadata ---");

            Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong qpcFrequency);
            Console.WriteLine($"  QPCFrequency: {qpcFrequency}");

            Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong qpcSync);
            Console.WriteLine($"  QPCSync: {qpcSync}");

            Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong utcSync);
            Console.WriteLine($"  UTCSync: {utcSync}");

            Deserializer.ReadCompressedUInt32(buffer, ref index, out uint eventBufferSize);
            Console.WriteLine($"  EventBufferSize: {eventBufferSize}");

            byte wrapperCount = buffer[index++];
            Console.WriteLine($"  WrapperCount: {wrapperCount}");

            for (int i = 0; i < wrapperCount; i++)
            {
                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong ip);
                Console.WriteLine($"  Wrapper[{i}]: 0x{ip:X16}");
            }

            Console.WriteLine("----------------------------");
            return index;
        }

        private static int OutputAsyncCallstackEvent(string eventName, ReadOnlySpan<byte> buffer)
        {
            ulong id;
            byte type;
            byte callstackId;
            byte asyncCallstackLength;
            int index = 0;

            type = buffer[index++];
            callstackId = buffer[index++];
            asyncCallstackLength = buffer[index++];
            Deserializer.ReadCompressedUInt64(buffer, ref index, out id);

            Console.WriteLine($"--- {eventName} ---");
            Console.WriteLine($"ID: {id}");
            Console.WriteLine($"Type: {type}");
            Console.WriteLine($"CallstackId: {callstackId}");
            Console.WriteLine($"Length: {asyncCallstackLength}");

            if (asyncCallstackLength == 0)
            {
                return index;
            }

            ulong previousNativeIP;
            ulong currentNativeIP;
            int state;

            Deserializer.ReadCompressedUInt64(buffer, ref index, out currentNativeIP);
            Deserializer.ReadCompressedInt32(buffer, ref index, out state);

            OutputAsyncFrame(currentNativeIP, state, 0);

            for (int i = 1; i < asyncCallstackLength; i++)
            {
                previousNativeIP = currentNativeIP;
                Deserializer.ReadCompressedInt64(buffer, ref index, out long nativeIPDelta);
                Deserializer.ReadCompressedInt32(buffer, ref index, out state);
                currentNativeIP = previousNativeIP + (ulong)nativeIPDelta;
                OutputAsyncFrame(currentNativeIP, state, i);
            }

            return index;
        }

        private static readonly MethodInfo? s_getMethodFromNativeIP =
            typeof(StackFrame).GetMethod("GetMethodFromNativeIP", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        private static string ResolveAsyncMethodName(nint nativeIP)
        {
            if (s_getMethodFromNativeIP is not null)
            {
                try
                {
                    MethodBase? method = s_getMethodFromNativeIP.Invoke(null, [nativeIP]) as MethodBase;
                    return method?.Name ?? string.Empty;
                }
                catch
                {
                }
            }

            return string.Empty;
        }

        private static void OutputAsyncFrame(ulong nativeIP, int state, int frameIndex)
        {
            string asyncMethodName = ResolveAsyncMethodName((nint)nativeIP);
            asyncMethodName = !string.IsNullOrEmpty(asyncMethodName) ? asyncMethodName : $"??";
            string nativeIPString = $"0x{nativeIP:X}";
            Console.WriteLine($"  Frame {frameIndex}: AsyncMethod = {asyncMethodName}, NativeIP = {nativeIPString}, State = {state}");
        }

        internal static class Deserializer
        {
            public static void ReadInt32(ReadOnlySpan<byte> buffer, ref int index, out int value)
            {
                uint uValue;
                ReadUInt32(buffer, ref index, out uValue);
                value = (int)uValue;
            }

            public static void ReadCompressedInt32(ReadOnlySpan<byte> buffer, ref int index, out int value)
            {
                uint uValue;
                ReadCompressedUInt32(buffer, ref index, out uValue);
                value = ZigzagDecodeInt32(uValue);
            }

            public static void ReadUInt32(ReadOnlySpan<byte> buffer, ref int index, out uint value)
            {
                value = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(index));
                index += 4;
            }

            public static void ReadCompressedUInt32(ReadOnlySpan<byte> buffer, ref int index, out uint value)
            {
                int shift = 0;
                byte b;

                value = 0;
                do
                {
                    b = buffer[index++];
                    value |= (uint)(b & 0x7F) << shift;
                    shift += 7;
                } while ((b & 0x80) != 0);
            }

            public static void ReadInt64(ReadOnlySpan<byte> buffer, ref int index, out long value)
            {
                ulong uValue;
                ReadUInt64(buffer, ref index, out uValue);
                value = (long)uValue;
            }

            public static void ReadCompressedInt64(ReadOnlySpan<byte> buffer, ref int index, out long value)
            {
                ulong uValue;
                ReadCompressedUInt64(buffer, ref index, out uValue);
                value = ZigzagDecodeInt64(uValue);
            }

            public static void ReadUInt64(ReadOnlySpan<byte> buffer, ref int index, out ulong value)
            {
                value = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(index));
                index += 8;
            }

            public static void ReadCompressedUInt64(ReadOnlySpan<byte> buffer, ref int index, out ulong value)
            {
                int shift = 0;
                byte b;

                value = 0;
                do
                {
                    b = buffer[index++];
                    value |= (ulong)(b & 0x7F) << shift;
                    shift += 7;
                } while ((b & 0x80) != 0);
            }

            private static int ZigzagDecodeInt32(uint value) => (int)((value >> 1) ^ (~(value & 1) + 1));

            private static long ZigzagDecodeInt64(ulong value) => (long)((value >> 1) ^ (~(value & 1) + 1));
        }
    }
}
