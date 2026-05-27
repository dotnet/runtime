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
        public static bool IsRuntimeAsyncAndThreadingSupported =>
            PlatformDetection.IsRuntimeAsyncSupported && PlatformDetection.IsMultithreadingSupported;

        // Gate for tests that can run without threading (e.g., single-threaded WASM).
        // These tests use async Task methods with await instead of Task.Run blocking.
        public static bool IsRuntimeAsyncSupported => PlatformDetection.IsRuntimeAsyncSupported;

        private const string AsyncProfilerEventSourceName = "System.Runtime.CompilerServices.AsyncProfilerEventSource";
        private const string WrapperNameTemplate = "Continuation_Wrapper_{0}";
        private static readonly string WrapperNamePrefix = WrapperNameTemplate.Substring(0, WrapperNameTemplate.IndexOf("{0}", StringComparison.Ordinal));

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
            ResumeAsyncContextKeyword | ResumeAsyncCallstackKeyword |
            SuspendAsyncContextKeyword | SuspendAsyncCallstackKeyword |
            CompleteAsyncContextKeyword | CompleteAsyncMethodKeyword | UnwindAsyncExceptionKeyword;

        // CoreCLR has StackFrame.GetMethodFromNativeIP (static, non-public).
        // NativeAOT lacks that but has an internal StackFrame(IntPtr, bool) constructor;
        // we resolve the name via DiagnosticMethodInfo.Create(frame).
        private static readonly MethodInfo? s_getMethodFromNativeIPMethod =
            typeof(StackFrame).GetMethod("GetMethodFromNativeIP", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly ConstructorInfo? s_stackFrameFromIPCtor =
            s_getMethodFromNativeIPMethod is null
                ? typeof(StackFrame).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IntPtr), typeof(bool) }, null)
                : null;

        internal static string? GetMethodNameFromNativeIP(ulong nativeIP)
        {
            if (s_getMethodFromNativeIPMethod is not null)
            {
                var method = (MethodBase?)s_getMethodFromNativeIPMethod.Invoke(null, new object[] { (IntPtr)nativeIP });
                return method?.Name;
            }

            if (s_stackFrameFromIPCtor is not null)
            {
                var frame = (StackFrame)s_stackFrameFromIPCtor.Invoke(new object[] { (IntPtr)nativeIP, false })!;
                var diagInfo = DiagnosticMethodInfo.Create(frame);
                return diagInfo?.Name;
            }

            return null;
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SingleAsyncYield()
        {
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task ChainedAsyncYield()
        {
            await InnerAsyncYield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task InnerAsyncYield()
        {
            await Task.Yield();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task InnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("inner");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
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
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task DeepMiddle()
        {
            await DeepInnerThrows();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task DeepInnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("deep inner");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task DeepUnhandledOuter()
        {
            await DeepUnhandledMiddle();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task DeepUnhandledMiddle()
        {
            await DeepUnhandledInnerThrows();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task DeepUnhandledInnerThrows()
        {
            await Task.Yield();
            throw new InvalidOperationException("deep unhandled");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task RecursiveAsyncChain(int depth)
        {
            if (depth <= 1)
            {
                await Task.Yield();
                return;
            }
            await RecursiveAsyncChain(depth - 1);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task WrapperTestA(List<(string MethodName, int WrapperSlot)> captures)
        {
            await WrapperTestB(captures);
            captures.Add((nameof(WrapperTestA), GetCurrentWrapperSlot(nameof(WrapperTestA))));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task WrapperTestB(List<(string MethodName, int WrapperSlot)> captures)
        {
            await WrapperTestC(captures);
            captures.Add((nameof(WrapperTestB), GetCurrentWrapperSlot(nameof(WrapperTestB))));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
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
                string? name = GetFrameMethodName(st.GetFrame(i));
                if (name is not null && name.Contains(resumedMethodName))
                {
                    // Scan subsequent frames, skipping unresolvable stubs (e.g. delegate invoke thunks on NativeAOT).
                    for (int j = i + 1; j < st.FrameCount; j++)
                    {
                        string? wrapperName = GetFrameMethodName(st.GetFrame(j));
                        if (wrapperName is null)
                            continue;
                        if (wrapperName.StartsWith(WrapperNamePrefix, StringComparison.Ordinal))
                        {
                            string wrapperSuffix = wrapperName.Substring(WrapperNamePrefix.Length);
                            return int.TryParse(wrapperSuffix, out int wrapperSlot) ? wrapperSlot : -1;
                        }
                        break;
                    }
                    return -1;
                }
            }
            return -1;
        }

        private static string? GetFrameMethodName(StackFrame? frame)
        {
            if (frame is null)
                return null;
            string? name = frame.GetMethod()?.Name;
            if (name is null)
            {
                name = DiagnosticMethodInfo.Create(frame)?.Name;
            }
            return name;
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
            out ulong qpcFrequency, out ulong qpcSync, out ulong utcSync, out uint eventBufferSize, out byte wrapperCount)
        {
            qpcFrequency = ReadCompressedUInt64(buffer, ref index);
            qpcSync = ReadCompressedUInt64(buffer, ref index);
            utcSync = ReadCompressedUInt64(buffer, ref index);
            eventBufferSize = ReadCompressedUInt32(buffer, ref index);
            wrapperCount = buffer[index++];
        }

        private record struct MetadataFromBuffer(ulong QpcFrequency, ulong QpcSync, ulong UtcSync, uint EventBufferSize, byte WrapperCount);

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

        private sealed class ParsedEvent
        {
            public AsyncEventID EventId { get; init; }
            public long Timestamp { get; init; }
            public ulong OsThreadId { get; init; }

            /// <summary>
            /// The Task.Id associated with this event. For context events (Create/Resume),
            /// this is the payload ID. For callstack events, this is the ID from the callstack
            /// header. For other events, this is the active task ID at the time of emission.
            /// </summary>
            public ulong TaskId { get; init; }

            // Callstack events (Create/Resume/Suspend): frames
            public byte FrameCount { get; init; }
            public List<(ulong NativeIP, int State)> Frames { get; init; } = [];

            // UnwindAsyncException: frame count unwound
            public uint UnwindFrameCount { get; init; }

            // Metadata
            public MetadataFromBuffer? Metadata { get; init; }

            // SyncClock
            public ulong SyncClockQpc { get; init; }
            public ulong SyncClockUtc { get; init; }

            /// <summary>
            /// Returns true if any frame in this event's callstack resolves to a method
            /// whose name contains the specified marker string.
            /// </summary>
            public bool HasMarkerFrame(string markerMethodName)
            {
                if (Frames.Count == 0)
                    return false;
                foreach (var (nativeIP, _) in Frames)
                {
                    var methodName = GetMethodNameFromNativeIP(nativeIP);
                    if (methodName is not null && methodName.Contains(markerMethodName, StringComparison.Ordinal))
                        return true;
                }
                return false;
            }
        }

        private sealed class ParsedEventStream
        {
            private readonly List<ParsedEvent> _events;
            private Dictionary<ulong, List<ParsedEvent>>? _byTaskId;

            public ParsedEventStream(List<ParsedEvent> events)
            {
                _events = events;
                _events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            }

            /// <summary>All events in timestamp order.</summary>
            public IReadOnlyList<ParsedEvent> All => _events;

            /// <summary>All distinct event IDs present in the stream.</summary>
            public IEnumerable<AsyncEventID> EventIds => _events.Select(e => e.EventId).Distinct();

            /// <summary>Filter events by event ID, in timestamp order.</summary>
            public IEnumerable<ParsedEvent> OfType(AsyncEventID eventId) =>
                _events.Where(e => e.EventId == eventId);

            /// <summary>Filter events by multiple event IDs, in timestamp order.</summary>
            public IEnumerable<ParsedEvent> OfTypes(params AsyncEventID[] eventIds)
            {
                var set = new HashSet<AsyncEventID>(eventIds);
                return _events.Where(e => set.Contains(e.EventId));
            }

            /// <summary>Get events grouped by Task.Id, each group in timestamp order.</summary>
            public Dictionary<ulong, List<ParsedEvent>> ByTaskId()
            {
                if (_byTaskId is not null)
                    return _byTaskId;

                _byTaskId = new Dictionary<ulong, List<ParsedEvent>>();
                foreach (var evt in _events)
                {
                    if (evt.TaskId == 0)
                        continue;
                    if (!_byTaskId.TryGetValue(evt.TaskId, out var list))
                    {
                        list = new List<ParsedEvent>();
                        _byTaskId[evt.TaskId] = list;
                    }
                    list.Add(evt);
                }
                return _byTaskId;
            }

            /// <summary>Get events for a specific Task.Id in timestamp order.</summary>
            public List<ParsedEvent> ForTask(ulong taskId) =>
                ByTaskId().TryGetValue(taskId, out var list) ? list : new List<ParsedEvent>();

            /// <summary>
            /// Get callstack events (of specified type) that contain the marker method in their frames.
            /// Results are in timestamp order.
            /// </summary>
            public List<ParsedEvent> CallstacksWithMarker(AsyncEventID callstackEventId, string markerMethodName) =>
                _events.Where(e => e.EventId == callstackEventId && e.HasMarkerFrame(markerMethodName)).ToList();

            /// <summary>
            /// Get callstack events (of specified type) that contain the marker method,
            /// taking only the first match per Task.Id (deepest chain by timestamp).
            /// </summary>
            public List<ParsedEvent> CallstacksWithMarkerFirstPerTask(AsyncEventID callstackEventId, string markerMethodName)
            {
                var matched = CallstacksWithMarker(callstackEventId, markerMethodName);
                var seen = new HashSet<ulong>();
                var result = new List<ParsedEvent>();
                foreach (var evt in matched)
                {
                    if (evt.TaskId != 0 && seen.Add(evt.TaskId))
                        result.Add(evt);
                }
                return result;
            }

            /// <summary>Get all metadata events.</summary>
            public List<MetadataFromBuffer> MetadataEvents =>
                _events.Where(e => e.Metadata.HasValue).Select(e => e.Metadata!.Value).ToList();

            /// <summary>Get distinct OS thread IDs across all events.</summary>
            public HashSet<ulong> OsThreadIds => new(_events.Select(e => e.OsThreadId).Where(id => id != 0));
        }

        private static ParsedEventStream ParseAllEvents(CollectedEvents events)
        {
            var allEvents = new List<ParsedEvent>();

            ForEachEventBufferPayload(events.Events, buffer =>
            {
                EventBufferHeader? header = ParseEventBufferHeader(buffer);
                if (header is null)
                    return;

                ulong osThreadId = header.Value.OsThreadId;
                ulong currentTaskId = 0;
                int index = HeaderSize;
                long baseTimestamp = (long)header.Value.StartTimestamp;

                while (index < buffer.Length)
                {
                    if (index + 2 > buffer.Length)
                        break;

                    AsyncEventID eventId = (AsyncEventID)buffer[index++];
                    long delta = (long)ReadCompressedUInt64(buffer, ref index);
                    baseTimestamp += delta;

                    ParsedEvent evt = eventId switch
                    {
                        AsyncEventID.CreateAsyncContext or AsyncEventID.ResumeAsyncContext =>
                            ParseContextEvent(eventId, baseTimestamp, osThreadId, buffer, ref index, ref currentTaskId),

                        AsyncEventID.SuspendAsyncContext or AsyncEventID.CompleteAsyncContext or
                        AsyncEventID.ResumeAsyncMethod or AsyncEventID.CompleteAsyncMethod =>
                            new ParsedEvent
                            {
                                EventId = eventId,
                                Timestamp = baseTimestamp,
                                OsThreadId = osThreadId,
                                TaskId = currentTaskId
                            },

                        AsyncEventID.ResetAsyncThreadContext or AsyncEventID.ResetAsyncContinuationWrapperIndex =>
                            ParseResetEvent(eventId, baseTimestamp, osThreadId, ref currentTaskId),

                        AsyncEventID.CreateAsyncCallstack or AsyncEventID.ResumeAsyncCallstack or
                        AsyncEventID.SuspendAsyncCallstack =>
                            ParseCallstackEvent(eventId, baseTimestamp, osThreadId, buffer, ref index),

                        AsyncEventID.UnwindAsyncException =>
                            ParseUnwindEvent(baseTimestamp, osThreadId, currentTaskId, buffer, ref index),

                        AsyncEventID.AsyncProfilerMetadata =>
                            ParseMetadataEvent(baseTimestamp, osThreadId, currentTaskId, buffer, ref index),

                        AsyncEventID.AsyncProfilerSyncClock =>
                            ParseSyncClockEvent(baseTimestamp, osThreadId, currentTaskId, buffer, ref index),

                        _ => ParseUnknownEvent(eventId, baseTimestamp, osThreadId, currentTaskId, buffer, ref index)
                    };

                    allEvents.Add(evt);
                }
            });

            return new ParsedEventStream(allEvents);

            static ParsedEvent ParseContextEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ReadOnlySpan<byte> buffer, ref int index, ref ulong currentTaskId)
            {
                ulong id = ReadCompressedUInt64(buffer, ref index);
                currentTaskId = id;
                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = id
                };
            }

            static ParsedEvent ParseResetEvent(AsyncEventID eventId, long timestamp, ulong osThreadId, ref ulong currentTaskId)
            {
                ulong prevTaskId = currentTaskId;
                if (eventId == AsyncEventID.ResetAsyncThreadContext)
                    currentTaskId = 0;
                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = prevTaskId
                };
            }

            static ParsedEvent ParseCallstackEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                ReadCallstackPayload(buffer, ref index, out ulong taskId, out byte frameCount, out var frames);
                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = taskId,
                    FrameCount = frameCount,
                    Frames = frames
                };
            }

            static ParsedEvent ParseUnwindEvent(long timestamp, ulong osThreadId, ulong currentTaskId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                uint unwindCount = ReadCompressedUInt32(buffer, ref index);
                return new ParsedEvent
                {
                    EventId = AsyncEventID.UnwindAsyncException,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = currentTaskId,
                    UnwindFrameCount = unwindCount
                };
            }

            static ParsedEvent ParseMetadataEvent(long timestamp, ulong osThreadId, ulong currentTaskId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                ReadMetadataPayload(buffer, ref index, out ulong freq, out ulong qpcSync, out ulong utcSync, out uint bufSize, out byte wrapperCount);
                return new ParsedEvent
                {
                    EventId = AsyncEventID.AsyncProfilerMetadata,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = currentTaskId,
                    Metadata = new MetadataFromBuffer(freq, qpcSync, utcSync, bufSize, wrapperCount)
                };
            }

            static ParsedEvent ParseSyncClockEvent(long timestamp, ulong osThreadId, ulong currentTaskId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                ulong qpcSync = ReadCompressedUInt64(buffer, ref index);
                ulong utcSync = ReadCompressedUInt64(buffer, ref index);
                return new ParsedEvent
                {
                    EventId = AsyncEventID.AsyncProfilerSyncClock,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = currentTaskId,
                    SyncClockQpc = qpcSync,
                    SyncClockUtc = utcSync
                };
            }

            static ParsedEvent ParseUnknownEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ulong currentTaskId, ReadOnlySpan<byte> buffer, ref int index)
            {
                SkipEventPayload(eventId, buffer, ref index);
                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = currentTaskId
                };
            }
        }

        private delegate void EventBufferPayloadAction(ReadOnlySpan<byte> payload);

        private static void ForEachEventBufferPayload(CollectedEvents events, EventBufferPayloadAction action)
        {
            ForEachEventBufferPayload(events.Events, action);
        }

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
        private static void DumpAllEvents(CollectedEvents events)
        {
            ForEachEventBufferPayload(events.Events, buffer => EventBuffer.OutputEventBuffer(buffer));
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

        private sealed class CollectedEvents
        {
            public ConcurrentQueue<EventWrittenEventArgs> Events { get; } = new();
        }

        private static async Task<CollectedEvents> CollectEventsAsync(EventKeywords keywords, Func<Task> scenario)
        {
            var result = new CollectedEvents();
            using (var listener = CreateListener(keywords))
            {
                await listener.RunWithCallbackAsync(result.Events.Enqueue, async () =>
                {
                    SendFlushCommand();
                    result.Events.Clear();
                    // Clear SynchronizationContext so RuntimeAsync continuations don't capture
                    // xunit's context, which would cause per-frame re-queuing instead of inlining.
                    var prevCtx = SynchronizationContext.Current;
                    int originalThreadId = Environment.CurrentManagedThreadId;
                    SynchronizationContext.SetSynchronizationContext(null);
                    try
                    {
                        await scenario().ConfigureAwait(false);
                    }
                    finally
                    {
                        // Only restore the SynchronizationContext if we're still on the same thread.
                        // ConfigureAwait(false) may resume on a different thread pool thread, and
                        // setting the original thread's context there would be incorrect.
                        if (Environment.CurrentManagedThreadId == originalThreadId)
                        {
                            SynchronizationContext.SetSynchronizationContext(prevCtx);
                        }
                    }
                    SendFlushCommand();
                }).ConfigureAwait(false);
            }
            return result;
        }

        private static CollectedEvents CollectEvents(EventKeywords keywords, Action callback)
        {
            return CollectEvents(keywords, (_, _) => callback());
        }

        private static CollectedEvents CollectEvents(EventKeywords keywords, Action<CollectedEvents, EventKeywords> callback)
        {
            var result = new CollectedEvents();
            using (var listener = CreateListener(keywords))
            {
                listener.RunWithCallback(result.Events.Enqueue, () =>
                {
                    SendFlushCommand();
                    result.Events.Clear();
                    callback(result, keywords);
                });
            }
            return result;
        }

        /// <summary>
        /// Returns true if any callstack event contains all expected method names as frames,
        /// appearing in the given order (index 0 = innermost/deepest frame).
        /// </summary>
        private static bool HasCallstackWithExpectedFrames(List<ParsedEvent> callstacks, string[] expectedFrames)
        {
            foreach (var cs in callstacks)
            {
                var resolvedNames = cs.Frames
                    .Select(f => GetMethodNameFromNativeIP(f.NativeIP))
                    .ToList();

                int matchIndex = 0;
                for (int i = 0; i < resolvedNames.Count && matchIndex < expectedFrames.Length; i++)
                {
                    if (resolvedNames[i] is not null && resolvedNames[i]!.Contains(expectedFrames[matchIndex], StringComparison.Ordinal))
                        matchIndex++;
                }

                if (matchIndex == expectedFrames.Length)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// For a given context, simulates the async callstack depth by walking events in order:
        /// ResumeAsyncCallstack sets the depth to frame count, CompleteAsyncMethod decrements,
        /// UnwindAsyncException subtracts unwound frames. Asserts depth reaches zero.
        /// </summary>
        private static void AssertCallstackSimulationReachesZero(ParsedEventStream stream, string markerMethodName)
        {
            // Find context ID via marker on a resume callstack
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, markerMethodName);
            Assert.True(resumeStacks.Count >= 1, $"Expected at least one resume callstack with marker '{markerMethodName}'");

            ulong taskId = resumeStacks[0].TaskId;

            var sequence = stream.ForTask(taskId);
            int stackDepth = 0;

            foreach (var evt in sequence)
            {
                switch (evt.EventId)
                {
                    case AsyncEventID.ResumeAsyncCallstack:
                        stackDepth = (int)evt.FrameCount;
                        break;
                    case AsyncEventID.CompleteAsyncMethod:
                        if (stackDepth > 0)
                            stackDepth--;
                        break;
                    case AsyncEventID.UnwindAsyncException:
                        stackDepth = Math.Max(0, stackDepth - (int)evt.UnwindFrameCount);
                        break;
                }
            }

            Assert.Equal(0, stackDepth);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_EventBufferHeaderFormat()
        {
            var events = await CollectEventsAsync(CoreKeywords, SingleAsyncYield);

            // DumpAllEvents(events);

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
                Assert.True(header.EndTimestamp >= header.StartTimestamp, $"End timestamp ({header.EndTimestamp}) should be >= start timestamp ({header.StartTimestamp})");

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

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_EventsEmitted()
        {
            var events = await CollectEventsAsync(AllKeywords, SingleAsyncYield);

            // DumpAllEvents(events);

            Assert.True(events.Events.Count > 0, "Expected at least one AsyncEvents event to be emitted");
            Assert.Contains(events.Events, e => e.EventId == AsyncEventsId);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SuspendResumeCompleteMarker()
        {
            await Task.Yield();
            await SingleAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendResumeCompleteEvents()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SuspendResumeCompleteMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find our context via marker callstack.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(SuspendResumeCompleteMarker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with SuspendResumeCompleteMarker");

            ulong taskId = markerCallstacks[0].TaskId;
            var taskEvts = stream.ForTask(taskId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            Assert.Contains(AsyncEventID.ResumeAsyncContext, ids);
            Assert.Contains(AsyncEventID.SuspendAsyncContext, ids);
            Assert.Contains(AsyncEventID.CompleteAsyncContext, ids);
        }


        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task ContextLifecycleMarker()
        {
            await Task.Yield();
            await SingleAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ContextEventIdLifecycle()
        {
            var events = await CollectEventsAsync(CallstackKeywords, ContextLifecycleMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find events in the context that contains our marker method.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(ContextLifecycleMarker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with ContextLifecycleMarker");

            ulong taskId = markerCallstacks[0].TaskId;
            Assert.True(taskId > 0, "Context ID should be non-zero");

            var taskEvts = stream.ForTask(taskId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            int createIdx = ids.IndexOf(AsyncEventID.CreateAsyncContext);
            int resumeIdx = ids.IndexOf(AsyncEventID.ResumeAsyncContext);
            Assert.True(createIdx >= 0, "Expected CreateAsyncContext in context events");
            Assert.True(resumeIdx > createIdx, "Expected ResumeAsyncContext after CreateAsyncContext");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ResumeCompleteMethodEvents()
        {
            var events = await CollectEventsAsync(MethodKeywords, ChainedAsyncYield);

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            Assert.Contains(AsyncEventID.ResumeAsyncMethod, ids);
            Assert.Contains(AsyncEventID.CompleteAsyncMethod, ids);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task EventSequenceOrderMarker()
        {
            await Task.Yield();
            await SingleAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_EventSequenceOrder()
        {
            var events = await CollectEventsAsync(CallstackKeywords, EventSequenceOrderMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find our context via marker callstack.
            var markerCallstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(EventSequenceOrderMarker));
            Assert.True(markerCallstacks.Count > 0, "Expected at least one callstack with EventSequenceOrderMarker");

            ulong taskId = markerCallstacks[0].TaskId;
            var taskEvts = stream.ForTask(taskId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            // Verify the expected lifecycle sequence exists in order.
            int resumeIdx1 = ids.IndexOf(AsyncEventID.ResumeAsyncContext);
            Assert.True(resumeIdx1 >= 0, "Expected first ResumeAsyncContext");

            int suspendIdx = ids.IndexOf(AsyncEventID.SuspendAsyncContext, resumeIdx1 + 1);
            Assert.True(suspendIdx > resumeIdx1, "Expected SuspendAsyncContext after first Resume");

            int resumeIdx2 = ids.IndexOf(AsyncEventID.ResumeAsyncContext, suspendIdx + 1);
            Assert.True(resumeIdx2 > suspendIdx, "Expected second ResumeAsyncContext after Suspend");

            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext, resumeIdx2 + 1);
            Assert.True(completeIdx > resumeIdx2, "Expected CompleteAsyncContext after second Resume");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateAsyncContextEmittedOnFirstAwait()
        {
            var events = await CollectEventsAsync(CreateAsyncContextKeyword | CompleteAsyncContextKeyword, SingleAsyncYield);

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;
            Assert.Contains(AsyncEventID.CreateAsyncContext, ids);
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task CreateCallstackMarker()
        {
            await SingleAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateAsyncCallstackEmittedOnFirstAwait()
        {
            var events = await CollectEventsAsync(CallstackKeywords, CreateCallstackMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createCallstacks = stream.CallstacksWithMarker(AsyncEventID.CreateAsyncCallstack, nameof(CreateCallstackMarker));

            Assert.NotEmpty(createCallstacks);
            Assert.All(createCallstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in create callstack");
                Assert.True(cs.TaskId != 0, "Expected non-zero task ID in create callstack");
                Assert.True(cs.Frames[0].NativeIP != 0, "Expected non-zero NativeIP in first frame");
            });
        }


        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task CreateCallstackDepthMarker()
        {
            await ChainedAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateCallstackDepthMatchesChain()
        {
            var events = await CollectEventsAsync(CallstackKeywords, CreateCallstackDepthMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createCallstacks = stream.CallstacksWithMarker(AsyncEventID.CreateAsyncCallstack, nameof(CreateCallstackDepthMarker));

            // The expected [NoInlining] frames in order (innermost first):
            // InnerAsyncYield -> ChainedAsyncYield -> CreateCallstackDepthMarker
            Assert.NotEmpty(createCallstacks);
            string[] expectedFrames = [nameof(InnerAsyncYield), nameof(ChainedAsyncYield), nameof(CreateCallstackDepthMarker)];
            Assert.True(
                HasCallstackWithExpectedFrames(createCallstacks, expectedFrames),
                $"Expected callstack to contain frames [{string.Join(", ", expectedFrames)}] in order");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SuspendCallstackMarker()
        {
            await Task.Yield();
            await SingleAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendAsyncCallstackEmittedOnAwait()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SuspendCallstackMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var suspendCallstacks = stream.CallstacksWithMarker(AsyncEventID.SuspendAsyncCallstack, nameof(SuspendCallstackMarker));

            Assert.NotEmpty(suspendCallstacks);
            Assert.All(suspendCallstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in suspend callstack");
                Assert.True(cs.TaskId != 0, "Expected non-zero task ID in suspend callstack");
                Assert.True(cs.Frames[0].NativeIP != 0, "Expected non-zero NativeIP in first frame");
            });
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SuspendDepthMarker()
        {
            await Task.Yield();
            await ChainedAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendCallstackDepthMatchesChain()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SuspendDepthMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var suspendCallstacks = stream.CallstacksWithMarker(AsyncEventID.SuspendAsyncCallstack, nameof(SuspendDepthMarker));

            // The expected [NoInlining] frames in order (innermost first):
            // InnerAsyncYield -> ChainedAsyncYield -> SuspendDepthMarker
            Assert.NotEmpty(suspendCallstacks);
            string[] expectedFrames = [nameof(InnerAsyncYield), nameof(ChainedAsyncYield), nameof(SuspendDepthMarker)];
            Assert.True(
                HasCallstackWithExpectedFrames(suspendCallstacks, expectedFrames),
                $"Expected callstack to contain frames [{string.Join(", ", expectedFrames)}] in order");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SuspendPrecedesCompleteMarker()
        {
            await Task.Yield();
            await InnerAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendCallstackPrecedesComplete()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SuspendPrecedesCompleteMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);

            // Find the suspend callstack via marker to get the context ID
            var suspendStacks = stream.CallstacksWithMarker(AsyncEventID.SuspendAsyncCallstack, nameof(SuspendPrecedesCompleteMarker));
            Assert.True(suspendStacks.Count >= 1, $"Expected at least one suspend callstack with marker, got {suspendStacks.Count}");

            ulong taskId = suspendStacks[0].TaskId;
            Assert.True(taskId > 0, "Expected non-zero context ID");

            var taskEvts = stream.ForTask(taskId);
            var ids = taskEvts.Select(e => e.EventId).ToList();

            int suspendIdx = ids.IndexOf(AsyncEventID.SuspendAsyncCallstack);
            int completeIdx = ids.IndexOf(AsyncEventID.CompleteAsyncContext);

            Assert.True(suspendIdx >= 0, "Expected SuspendAsyncCallstack in context events");
            Assert.True(completeIdx >= 0, "Expected CompleteAsyncContext in context events");
            Assert.True(suspendIdx < completeIdx, $"SuspendAsyncCallstack (index {suspendIdx}) should precede CompleteAsyncContext (index {completeIdx})");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SuspendDeeperMarker()
        {
            await Task.Yield();
            await InnerAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_SuspendCallstackDeeperThanInitialResume()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SuspendDeeperMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(SuspendDeeperMarker));
            var suspendStacks = stream.CallstacksWithMarker(AsyncEventID.SuspendAsyncCallstack, nameof(SuspendDeeperMarker));

            Assert.True(resumeStacks.Count >= 1, $"Expected at least one resume callstack with marker, got {resumeStacks.Count}");
            Assert.True(suspendStacks.Count >= 1, $"Expected at least one suspend callstack with marker, got {suspendStacks.Count}");

            // First resume (after initial Yield) should be shallow, first suspend (InnerAsyncYield's Yield) should be deeper
            var firstResume = resumeStacks[0];
            var firstSuspend = suspendStacks[0];

            Assert.True(firstSuspend.FrameCount > firstResume.FrameCount, $"First suspend callstack depth ({firstSuspend.FrameCount}) should be deeper than first resume callstack depth ({firstResume.FrameCount})");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task CreatePrecedesResumeMarker()
        {
            await Task.Yield();
            await InnerAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateCallstackPrecedesResumeCallstack()
        {
            var events = await CollectEventsAsync(CallstackKeywords, CreatePrecedesResumeMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createStacks = stream.CallstacksWithMarker(AsyncEventID.CreateAsyncCallstack, nameof(CreatePrecedesResumeMarker));
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(CreatePrecedesResumeMarker));

            Assert.NotEmpty(createStacks);
            Assert.NotEmpty(resumeStacks);

            // For each task that has both Create and Resume callstacks, verify Create timestamp precedes Resume.
            int matchedPairs = 0;
            foreach (var create in createStacks)
            {
                var matchingResume = resumeStacks.FirstOrDefault(r => r.TaskId == create.TaskId);
                if (matchingResume is null)
                    continue;

                matchedPairs++;
                Assert.True(create.Timestamp <= matchingResume.Timestamp, $"For task {create.TaskId}: CreateAsyncCallstack (ts {create.Timestamp}) should precede ResumeAsyncCallstack (ts {matchingResume.Timestamp})");
            }

            Assert.True(matchedPairs >= 1, $"Expected at least one matching Create/Resume callstack pair, but found {matchedPairs}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task CreateResumeMatchMarker()
        {
            await Task.Yield();
            await InnerAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CreateAndFirstResumeCallstacksMatch()
        {
            var events = await CollectEventsAsync(CallstackKeywords, CreateResumeMatchMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var createStacks = stream.CallstacksWithMarker(AsyncEventID.CreateAsyncCallstack, nameof(CreateResumeMatchMarker));
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(CreateResumeMatchMarker));

            Assert.NotEmpty(createStacks);
            Assert.NotEmpty(resumeStacks);

            // For each create callstack, find the first resume with the same task ID and verify frames match.
            int matchedPairs = 0;
            foreach (var create in createStacks)
            {
                var matchingResume = resumeStacks.FirstOrDefault(r => r.TaskId == create.TaskId);
                if (matchingResume is null)
                    continue;

                matchedPairs++;
                Assert.Equal(create.Frames.Count, matchingResume.Frames.Count);
                for (int i = 0; i < create.Frames.Count; i++)
                {
                    Assert.Equal(create.Frames[i].NativeIP, matchingResume.Frames[i].NativeIP);
                }
            }

            Assert.True(matchedPairs >= 1, $"Expected at least one matching Create/Resume callstack pair, but found {matchedPairs}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task CallstackOnResumeMarker()
        {
            await Task.Yield();
            await InnerAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackEmittedOnResume()
        {
            var events = await CollectEventsAsync(CallstackKeywords, CallstackOnResumeMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(CallstackOnResumeMarker));

            Assert.NotEmpty(callstacks);
            Assert.All(callstacks, cs =>
            {
                Assert.True(cs.FrameCount > 0, "Expected at least one frame in callstack");
                Assert.True(cs.TaskId != 0, "Expected non-zero task ID in resume callstack");
                Assert.True(cs.Frames[0].NativeIP != 0, "Expected non-zero NativeIP in first frame");
            });
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task CallstackDepthMarker()
        {
            await Task.Yield();
            await InnerAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackDepthMatchesChain()
        {
            var events = await CollectEventsAsync(CallstackKeywords, CallstackDepthMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(CallstackDepthMarker));

            // The expected [NoInlining] frames in order (innermost first):
            // InnerAsyncYield -> CallstackDepthMarker
            Assert.NotEmpty(callstacks);
            string[] expectedFrames = [nameof(InnerAsyncYield), nameof(CallstackDepthMarker)];
            Assert.True(
                HasCallstackWithExpectedFrames(callstacks, expectedFrames),
                $"Expected callstack to contain frames [{string.Join(", ", expectedFrames)}] in order");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SimulationNormalMarker()
        {
            await Task.Yield();
            await InnerAsyncYield();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackSimulation_NormalCompletion()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SimulationNormalMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            AssertCallstackSimulationReachesZero(stream, nameof(SimulationNormalMarker));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SimulationHandledMarker()
        {
            await DeepOuterCatches();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackSimulation_HandledException()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SimulationHandledMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            AssertCallstackSimulationReachesZero(stream, nameof(SimulationHandledMarker));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SimulationUnhandledMarker()
        {
            await DeepUnhandledOuter();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task SimulationUnhandledMarkerCatcher()
        {
            try
            {
                await SimulationUnhandledMarker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackSimulation_UnhandledException()
        {
            var events = await CollectEventsAsync(CallstackKeywords, SimulationUnhandledMarkerCatcher);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            AssertCallstackSimulationReachesZero(stream, nameof(SimulationUnhandledMarker));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task UnhandledUnwindMarker()
        {
            await DeepUnhandledOuter();
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task UnhandledUnwindCatcher()
        {
            try
            {
                await UnhandledUnwindMarker();
            }
            catch (InvalidOperationException)
            {
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_UnhandledExceptionUnwind()
        {
            var events = await CollectEventsAsync(CallstackKeywords, UnhandledUnwindCatcher);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(UnhandledUnwindMarker));
            Assert.True(resumeStacks.Count >= 1, $"Expected at least one resume callstack with marker '{nameof(UnhandledUnwindMarker)}'");

            ulong taskId = resumeStacks[0].TaskId;

            var taskEvts = stream.ForTask(taskId);
            var eventIds = taskEvts.Select(e => e.EventId).ToList();

            Assert.Contains(AsyncEventID.ResumeAsyncContext, eventIds);
            Assert.Contains(AsyncEventID.UnwindAsyncException, eventIds);
            Assert.Contains(AsyncEventID.CompleteAsyncContext, eventIds);

            // Verify unwind frame count for this task
            // UnhandledUnwindMarker -> DeepUnhandledOuter -> DeepUnhandledMiddle -> DeepUnhandledInnerThrows, 4 frames deep after the initial resume.
            var unwindEvents = taskEvts.Where(e => e.EventId == AsyncEventID.UnwindAsyncException).ToList();
            Assert.NotEmpty(unwindEvents);
            Assert.All(unwindEvents, e => Assert.Equal(4u, e.UnwindFrameCount));
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task HandledUnwindMarker()
        {
            await DeepOuterCatches();
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_HandledExceptionUnwind()
        {
            var events = await CollectEventsAsync(CallstackKeywords, HandledUnwindMarker);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(HandledUnwindMarker));
            Assert.True(resumeStacks.Count >= 1, $"Expected at least one resume callstack with marker '{nameof(HandledUnwindMarker)}'");

            ulong taskId = resumeStacks[0].TaskId;

            var taskEvts = stream.ForTask(taskId);
            var eventIds = taskEvts.Select(e => e.EventId).ToList();

            Assert.Contains(AsyncEventID.ResumeAsyncContext, eventIds);
            Assert.Contains(AsyncEventID.UnwindAsyncException, eventIds);
            Assert.Contains(AsyncEventID.CompleteAsyncContext, eventIds);

            // Verify unwind frame count for this task
            // DeepMiddle -> DeepInnerThrows, 2 frames deep after the initial resume.
            var unwindEvents = taskEvts.Where(e => e.EventId == AsyncEventID.UnwindAsyncException).ToList();
            Assert.NotEmpty(unwindEvents);
            Assert.All(unwindEvents, e => Assert.Equal(2u, e.UnwindFrameCount));
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

            var events = CollectEvents(ResumeAsyncCallstackKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await WrapperTestA(captures);
                });
            });

            // DumpAllEvents(events);

            Assert.True(captures.Count == 3, $"Expected 3 wrapper captures, got {captures.Count}");

            Assert.All(captures, c => Assert.True(c.WrapperSlot >= 0, $"{c.MethodName} did not find wrapper frame on stack (slot={c.WrapperSlot})"));

            int slotC = captures.First(c => c.MethodName == nameof(WrapperTestC)).WrapperSlot;
            int slotB = captures.First(c => c.MethodName == nameof(WrapperTestB)).WrapperSlot;
            int slotA = captures.First(c => c.MethodName == nameof(WrapperTestA)).WrapperSlot;

            Assert.Equal(slotC + 1, slotB);
            Assert.Equal(slotB + 1, slotA);
        }

        // Requires threading:
        // Same comment as RuntimeAsync_WrapperIndexMatchesCallstack regarding Task.Run and wrapper index behavior.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexResetEmitted()
        {
            var events = CollectEvents(AllKeywords, () =>
            {
                // Recursive chain 34 levels deep crosses the 32-slot boundary,
                // triggering at least one ResetAsyncContinuationWrapperIndex event.
                RunScenarioAndFlush(async () =>
                {
                    await RecursiveAsyncChain(34);
                });
            });

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            Assert.Contains(AsyncEventID.ResetAsyncContinuationWrapperIndex, ids);
        }

        // Requires threading:
        // Same comment as RuntimeAsync_WrapperIndexMatchesCallstack regarding Task.Run and wrapper index behavior.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_WrapperIndexNoResetUnder32()
        {
            var events = CollectEvents(AllKeywords, () =>
            {
                // A shallow chain stays within the first 32 slots —
                // no reset event should be emitted.
                RunScenarioAndFlush(async () =>
                {
                    await RecursiveAsyncChain(2);
                });
            });

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            Assert.DoesNotContain(AsyncEventID.ResetAsyncContinuationWrapperIndex, ids);
        }

        // Requires threading:
        // The periodic flush timer runs on a background thread.
        // On single-threaded runtimes there is no background thread to fire the timer.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_PeriodicTimerFlush()
        {
            static bool IsRequestedEvent(AsyncEventID id) =>
                id == AsyncEventID.CreateAsyncContext ||
                id == AsyncEventID.ResumeAsyncContext ||
                id == AsyncEventID.SuspendAsyncContext ||
                id == AsyncEventID.CompleteAsyncContext;

            var events = CollectEvents(CoreKeywords, (collectedEvents, _) =>
            {
                // Run scenario - do NOT flush explicitly afterwards.
                RunScenario(async () =>
                {
                    await SingleAsyncYield();
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

            Assert.True(coreEventCount > 0, "Expected periodic timer to flush buffer with core lifecycle events");
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
                        events.Events.Enqueue(e);
                }, () =>
                {
                    SendFlushCommand();

                    var thread = new Thread(() =>
                    {
                        workerOsThreadId = GetCurrentOSThreadId();
                        workerIdReady.Set();

                        // First batch: generate events on this thread's profiler context.
                        SingleAsyncYield().GetAwaiter().GetResult();
                        firstBatchDone.Set();

                        // Wait for the flush to deliver our first buffer before generating more events.
                        bool flushed = firstFlushSeen.Wait(TimeSpan.FromSeconds(20));
                        Assert.True(flushed, "Expected first flush of core lifecycle events within timeout");

                        // Second batch: generate more events on the same thread's context.
                        SingleAsyncYield().GetAwaiter().GetResult();
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

            Assert.True(workerOsThreadId != 0, "Failed to capture worker OS thread ID");

            // The key assertion: find buffers that contain CreateAsyncContext events (our work batches).
            // There must be at least 2 such buffers (one per SingleAsyncYield() call), and ALL of them must
            // have the worker's OsThreadId - proving the timer flush didn't corrupt the header.
            var stream = ParseAllEvents(events);
            var createEvents = stream.OfType(AsyncEventID.CreateAsyncContext).ToList();
            Assert.True(createEvents.Count >= 2, $"Expected at least 2 CreateAsyncContext events from the worker thread, got {createEvents.Count}");
            Assert.All(createEvents, e => Assert.Equal(workerOsThreadId, e.OsThreadId));
        }

        // Requires threading:
        // Spawns a dedicated thread that exits, then waits for the background flush timer
        // to detect and flush the orphaned thread-local buffer.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncAndThreadingSupported))]
        public void RuntimeAsync_DeadThreadFlush()
        {
            static bool IsRequestedEvent(AsyncEventID id) =>
                id == AsyncEventID.CreateAsyncContext ||
                id == AsyncEventID.ResumeAsyncContext ||
                id == AsyncEventID.SuspendAsyncContext ||
                id == AsyncEventID.CompleteAsyncContext;

            var events = CollectEvents(CoreKeywords, (collectedEvents, _) =>
            {
                // Spawn a dedicated thread that runs async work then exits.
                // Its thread-local buffer becomes orphaned when the thread dies.
                var thread = new Thread(() =>
                {
                    RunScenario(async () =>
                    {
                        await SingleAsyncYield();
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

            Assert.True(coreEventCount > 0, "Expected periodic timer to flush dead thread's buffer");
        }

        // This test is sensitive to event noise - it asserts a specific clock event is absent.
        // It cannot run in parallel with other async profiler scenarios that might produce
        // clock events. Test parallelization is disabled via XunitAssemblyAttributes.cs.
        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_NoSyncClockEventBeforeInterval()
        {
            var events = await CollectEventsAsync(CoreKeywords, SingleAsyncYield);

            var ids = ParseAllEvents(events).EventIds;

            Assert.DoesNotContain(AsyncEventID.AsyncProfilerSyncClock, ids);
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
                await SingleAsyncYield();
            }

            // Now attach listener but don't run any RuntimeAsync work inside —
            // just call a synchronous no-op. Verify no stale events from above leak through.
            var events = await CollectEventsAsync(CoreKeywords, () => Task.CompletedTask);

            // There may be meta data related events, but there should be no suspend/resume/complete events from the earlier work.
            var ids = ParseAllEvents(events).EventIds;

            int contextEvents = ids.Count(id => id == AsyncEventID.ResumeAsyncContext || id == AsyncEventID.SuspendAsyncContext || id == AsyncEventID.CompleteAsyncContext);

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

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task KeywordGatekeepingMarker()
        {
            await OuterCatches();
            await ChainedAsyncYield();
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
            var events = await CollectEventsAsync(kw, KeywordGatekeepingMarker);

            var stream = ParseAllEvents(events);
            var unexpected = stream.EventIds.Where(id => !allowed.Contains(id)).ToList();

            Assert.True(unexpected.Count == 0, $"Keyword 0x{(long)kw:X}: unexpected event IDs [{string.Join(", ", unexpected)}], " + $"allowed [{string.Join(", ", allowed)}]");
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_ResetAsyncThreadContextEvent()
        {
            var events = await CollectEventsAsync(CoreKeywords, SingleAsyncYield);

            // DumpAllEvents(events);

            var ids = ParseAllEvents(events).EventIds;

            Assert.Contains(AsyncEventID.ResetAsyncThreadContext, ids);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_MetadataEventEmittedOnEnable()
        {
            var events = await CollectEventsAsync(AllKeywords, SingleAsyncYield);

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var metadataList = stream.MetadataEvents;
            Assert.True(metadataList.Count >= 1, "Expected at least one metadata event in buffer");

            MetadataFromBuffer meta = metadataList[0];
            Assert.True(meta.QpcFrequency > 0, $"QPC frequency should be positive, got {meta.QpcFrequency}");
            Assert.True(meta.QpcSync > 0, $"QPC sync timestamp should be positive, got {meta.QpcSync}");
            Assert.True(meta.UtcSync > 0, $"UTC sync timestamp should be positive, got {meta.UtcSync}");
            Assert.True(meta.EventBufferSize > 0, $"Event buffer size should be positive, got {meta.EventBufferSize}");
            Assert.True(meta.WrapperCount > 0, "Wrapper count should be positive");
        }

        // Requires threading:
        // Spawns 8 threads with a Barrier to verify metadata is
        // emitted exactly once under concurrent enable pressure.
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
                        SingleAsyncYield().GetAwaiter().GetResult();
                    }, TaskCreationOptions.LongRunning);
                }
                Task.WaitAll(tasks);
                SendFlushCommand();
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var metadataList = stream.MetadataEvents;
            Assert.True(metadataList.Count == 1, $"Expected exactly 1 metadata event across {threadCount} threads, got {metadataList.Count}");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task NativeIPDeltaRoundtripMarker()
        {
            await ChainedAsyncYield();
            await DeepOuterCatches();
            await RecursiveAsyncChain(10);
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_CallstackNativeIPDeltaRoundtrip()
        {
            // Verify that delta-encoded NativeIPs in callstacks roundtrip correctly,
            // including both positive and negative deltas. With multiple distinct async
            // methods at different JIT-assigned addresses, the deltas between consecutive
            // NativeIPs will naturally span both directions. This exercises the full
            // zigzag + LEB128 encode/decode path through the production serializer.
            var events = await CollectEventsAsync(CallstackKeywords, NativeIPDeltaRoundtripMarker);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(NativeIPDeltaRoundtripMarker));
            Assert.NotEmpty(callstacks);

            // Find callstacks with 3+ frames — enough depth for meaningful deltas.
            var deepCallstacks = callstacks.Where(cs => cs.FrameCount >= 3).ToList();

            Assert.True(deepCallstacks.Count > 0, "Expected at least one callstack with 3+ frames for delta verification");

            bool hasPositiveDelta = false;
            bool hasNegativeDelta = false;

            foreach (var cs in deepCallstacks)
            {
                for (int i = 0; i < cs.Frames.Count; i++)
                {
                    var (nativeIP, _) = cs.Frames[i];
                    Assert.True(nativeIP != 0, $"Frame {i} has zero NativeIP");

                    var method = GetMethodNameFromNativeIP(nativeIP);
                    Assert.True(method is not null, $"Frame {i}: NativeIP 0x{nativeIP:X} does not resolve to a managed method");

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
            Assert.True(hasPositiveDelta || hasNegativeDelta, "Expected at least one non-zero NativeIP delta across all callstack frames");
        }

        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static async Task CallstackStressMarker(int depth)
        {
            await RecursiveAsyncChain(depth);
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
            // lambda -> CallstackStressMarker(d) -> RecursiveAsyncChain(d) produces d + 2 frames.
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
                        await CallstackStressMarker(depths[i]);
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.CallstacksWithMarker(AsyncEventID.ResumeAsyncCallstack, nameof(CallstackStressMarker));

            // Verify all callstacks have valid frame data that resolves to managed methods.
            foreach (var cs in callstacks)
            {
                Assert.True(cs.FrameCount > 0, "Callstack has 0 frames");
                Assert.Equal((int)cs.FrameCount, cs.Frames.Count);
                for (int f = 0; f < cs.Frames.Count; f++)
                {
                    var (nativeIP, _) = cs.Frames[f];
                    Assert.True(nativeIP != 0, $"Frame {f} has zero NativeIP");

                    var method = GetMethodNameFromNativeIP(nativeIP);
                    Assert.True(method is not null, $"Frame {f}: NativeIP 0x{nativeIP:X} does not resolve to a managed method");
                }
            }

            // One resume callstack per iteration (marker filters out noise).
            // lambda -> CallstackStressMarker -> RecursiveAsyncChain(d) produces d + 2 frames.
            Assert.True(callstacks.Count >= iterations, $"Expected at least {iterations} callstacks with marker, got {callstacks.Count}");

            for (int i = 0; i < iterations; i++)
            {
                // lambda + CallstackStressMarker + RecursiveAsyncChain(d) = d + 2
                int expected = depths[i] + 2;
                int actual = callstacks[i].FrameCount;
                Assert.True(actual == expected, $"Iteration {i}: expected depth {expected} (lambda -> CallstackStressMarker -> RecursiveAsyncChain({depths[i]})), got {actual}");
            }

            // Verify multiple buffer flushes occurred.
            int bufferCount = 0;
            ForEachEventBufferPayload(events, _ => bufferCount++);
            Assert.True(bufferCount >= 3, $"Expected at least 3 buffer flushes, got {bufferCount}");
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
            // remaining buffer space — the code rewinds, flushes the partial buffer,
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

                var events = CollectEvents(AllKeywords, () =>
                {
                    RunScenarioAndFlush(async () =>
                    {
                        for (int i = 0; i < iterations; i++)
                            await RecursiveAsyncChain(depths[i]);
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

                    uint contextId = header.Value.AsyncThreadContextId;

                    // Parse the first event in this buffer.
                    int index = HeaderSize;
                    if (index >= buffer.Length)
                        return;

                    AsyncEventID firstId = (AsyncEventID)buffer[index++];
                    // Skip timestamp delta
                    ReadCompressedUInt64(buffer, ref index);

                    byte frameCount = 0;
                    if (firstId == AsyncEventID.ResumeAsyncCallstack ||
                        firstId == AsyncEventID.CreateAsyncCallstack ||
                        firstId == AsyncEventID.SuspendAsyncCallstack)
                    {
                        // Callstack payload: type(1) + callstackId(1) + frameCount(1) + compressed taskId + frames...
                        index++; // type byte
                        index++; // callstack ID (reserved)
                        if (index < buffer.Length)
                            frameCount = buffer[index];
                    }

                    if (!buffersByContext.TryGetValue(contextId, out var list))
                    {
                        list = new List<(int, AsyncEventID, byte)>();
                        buffersByContext[contextId] = list;
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
                            (next.FirstEventId == AsyncEventID.ResumeAsyncCallstack ||
                             next.FirstEventId == AsyncEventID.CreateAsyncCallstack ||
                             next.FirstEventId == AsyncEventID.SuspendAsyncCallstack) &&
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
                    var largeCallstacks = stream.OfType(AsyncEventID.ResumeAsyncCallstack)
                        .Where(e => e.FrameCount > 30)
                        .ToList();

                    foreach (var cs in largeCallstacks)
                    {
                        Assert.Equal((int)cs.FrameCount, cs.Frames.Count);
                        for (int f = 0; f < cs.Frames.Count; f++)
                        {
                            var (nativeIP, _) = cs.Frames[f];
                            Assert.True(nativeIP != 0, $"Overflow callstack frame {f} has zero NativeIP");

                            var method = GetMethodNameFromNativeIP(nativeIP);
                            Assert.True(method is not null, $"Overflow callstack frame {f}: NativeIP 0x{nativeIP:X} does not resolve to a managed method");
                        }
                    }
                }
            }

            Assert.True(overflowDetected, "Failed to trigger callstack buffer overflow after 10 attempts — " +
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
            // RecursiveAsyncChain(300) produces a 300-deep chain + 1 lambda = 301 frames.
            const int requestedDepth = 300;

            var events = CollectEvents(ResumeAsyncCallstackKeyword, () =>
            {
                RunScenarioAndFlush(async () =>
                {
                    await RecursiveAsyncChain(requestedDepth);
                });
            });

            // DumpAllEvents(events);

            var stream = ParseAllEvents(events);
            var callstacks = stream.OfType(AsyncEventID.ResumeAsyncCallstack).ToList();
            Assert.True(callstacks.Count >= 1, "Expected at least one callstack");

            // Find the callstack from our deep RecursiveAsyncChain call.
            // The max frame count is capped at 255 (byte.MaxValue) since the
            // CaptureRuntimeAsyncCallstackState.Count is a byte.
            // RecursiveAsyncChain(300) + 1 lambda = 301 frames, capped to 255.
            var deepest = callstacks.MaxBy(cs => cs.FrameCount);
            Assert.Equal(255, (int)deepest!.FrameCount);
            Assert.Equal((int)deepest.FrameCount, deepest.Frames.Count);

            // Verify all frames are valid.
            foreach (var (nativeIP, _) in deepest.Frames)
            {
                Assert.True(nativeIP != 0, "Frame has zero NativeIP");
                var method = GetMethodNameFromNativeIP(nativeIP);
                Assert.True(method is not null, $"NativeIP 0x{nativeIP:X} does not resolve to a managed method");
            }
        }

        [ConditionalFact(typeof(AsyncProfilerTests), nameof(IsRuntimeAsyncSupported))]
        public async Task RuntimeAsync_MetadataMatchesWrapperMethods()
        {
            var events = await CollectEventsAsync(AllKeywords, SingleAsyncYield);

            var stream = ParseAllEvents(events);
            var metadataList = stream.MetadataEvents;
            Assert.True(metadataList.Count >= 1, "Expected at least one metadata event in buffer");

            MetadataFromBuffer meta = metadataList[0];
            Assert.True(meta.WrapperCount > 0, "Expected positive wrapper count in metadata");

            // On CoreCLR, verify via reflection that the contract-defined template produces names matching real methods.
            // This catches accidental renames of wrapper methods without updating the contract.
            if (PlatformDetection.IsCoreCLR)
            {
                Type? wrapperType = typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder)
                    .Assembly.GetType("System.Runtime.CompilerServices.AsyncProfiler+ContinuationWrapper");
                Assert.NotNull(wrapperType);
                for (int i = 0; i < meta.WrapperCount; i++)
                {
                    string expectedName = string.Format(WrapperNameTemplate, i);
                    var method = wrapperType.GetMethod(expectedName,
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    Assert.True(method is not null, $"Expected method '{expectedName}' not found on ContinuationWrapper type");
                }

                // Verify that the wrapper count matches the actual number of wrapper methods on the type.
                int actualCount = wrapperType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                    .Count(m => m.Name.StartsWith(WrapperNamePrefix, StringComparison.Ordinal));
                Assert.Equal(meta.WrapperCount, actualCount);
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

        private static void OutputAsyncFrame(ulong nativeIP, int state, int frameIndex)
        {
            string asyncMethodName = AsyncProfilerTests.GetMethodNameFromNativeIP(nativeIP) ?? "??";
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
