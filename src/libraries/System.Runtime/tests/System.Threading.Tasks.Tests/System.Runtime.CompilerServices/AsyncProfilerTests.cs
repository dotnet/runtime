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
        AppendAsyncCallstack = 15,
    }

    //Mirrors AsyncProfiler.AsyncCallstackType from the runtime (which is internal and inaccessible from tests).
    public enum AsyncCallstackType : byte
    {
        Compiler = 0x1,
        Runtime = 0x2,
        Cached = 0x80
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/127951", TestPlatforms.Android | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
    public partial class AsyncProfilerTests
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
        private static readonly MethodInfo? s_getMethodFromNativeIPMethod =
            typeof(StackFrame).GetMethod("GetMethodFromNativeIP", BindingFlags.Static | BindingFlags.NonPublic);

        // NativeAOT has DiagnosticMethodInfo.Create(StackFrame) (instance, non-public).
        private static readonly ConstructorInfo? s_stackFrameFromIPCtor =
            s_getMethodFromNativeIPMethod is null
                ? typeof(StackFrame).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(IntPtr), typeof(bool) }, null)
                : null;

        private static string? GetMethodNameFromMethodId(AsyncCallstackType callstackType, ulong methodId)
        {
            if (methodId != 0)
            {
                if (callstackType == AsyncCallstackType.Runtime)
                {
                    if (s_getMethodFromNativeIPMethod is not null)
                    {
                        MethodBase? method = (MethodBase?)s_getMethodFromNativeIPMethod.Invoke(null, new object[] { (IntPtr)methodId });
                        return method?.Name;
                    }

                    if (s_stackFrameFromIPCtor is not null)
                    {
                        StackFrame frame = (StackFrame)s_stackFrameFromIPCtor.Invoke(new object[] { (IntPtr)methodId, false })!;
                        DiagnosticMethodInfo? diagInfo = DiagnosticMethodInfo.Create(frame);
                        return diagInfo?.Name;
                    }
                }
                else if (callstackType == AsyncCallstackType.Compiler)
                {
                    System.RuntimeMethodHandle handle = RuntimeMethodHandle.FromIntPtr((IntPtr)methodId);
                    MethodBase? method = MethodBase.GetMethodFromHandle(handle);
                    if (method != null)
                    {
                        string methodName = method.DeclaringType.Name;

                        int start = methodName.IndexOf('<');
                        int end = methodName.IndexOf('>');

                        start++;
                        if (start > 0 && end > start)
                        {
                            methodName = methodName.Substring(start, end - start);
                        }

                        return methodName;
                    }
                }
            }

            return null;
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
                    for (int j = i + 1; j < st.FrameCount; j++)
                    {
                        string? wrapperName = GetFrameMethodName(st.GetFrame(j));
                        if (wrapperName is null)
                        {
                            continue;
                        }

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
            {
                return null;
            }

            string? name = frame.GetMethod()?.Name;
            if (name is null)
            {
                name = DiagnosticMethodInfo.Create(frame)?.Name;
            }

            return name;
        }

        private delegate bool EventVisitor(AsyncEventID eventId, ReadOnlySpan<byte> buffer, ref int index);

        private static void ParseEventBuffer(ReadOnlySpan<byte> buffer, EventVisitor visitor)
        {
            ParseEventBuffer(buffer, (AsyncEventID eventId, long _, ReadOnlySpan<byte> buf, ref int idx) =>
                visitor(eventId, buf, ref idx));
        }

        private delegate bool EventVisitorWithTimestamp(AsyncEventID eventId, long timestamp, ReadOnlySpan<byte> buffer, ref int index);

        private static void ParseEventBuffer(ReadOnlySpan<byte> buffer, EventVisitorWithTimestamp visitor)
        {
            EventBufferHeader? header = ParseEventBufferHeader(buffer);
            if (header is null)
            {
                return;
            }

            int index = HeaderSize;
            long baseTimestamp = (long)header.Value.StartTimestamp;

            while (index < buffer.Length)
            {
                if (index + 2 > buffer.Length)
                {
                    break;
                }

                AsyncEventID eventId = (AsyncEventID)buffer[index++];

                long delta = (long)ReadCompressedUInt64(buffer, ref index);
                baseTimestamp += delta;

                if (!visitor(eventId, baseTimestamp, buffer, ref index))
                {
                    break;
                }
            }
        }

        private static bool SkipEventPayload(AsyncEventID eventId, ReadOnlySpan<byte> buffer, ref int index)
        {
            switch (eventId)
            {
                case AsyncEventID.CreateAsyncContext:
                case AsyncEventID.ResumeAsyncContext:
                {
                    ReadCompressedUInt64(buffer, ref index);
                    return true;
                }
                case AsyncEventID.SuspendAsyncContext:
                case AsyncEventID.CompleteAsyncContext:
                case AsyncEventID.ResumeAsyncMethod:
                case AsyncEventID.CompleteAsyncMethod:
                case AsyncEventID.ResetAsyncThreadContext:
                case AsyncEventID.ResetAsyncContinuationWrapperIndex:
                {
                    return true;
                }
                case AsyncEventID.AsyncProfilerMetadata:
                {
                    SkipMetadataPayload(buffer, ref index);
                    return true;
                }
                case AsyncEventID.AsyncProfilerSyncClock:
                {
                    ReadCompressedUInt64(buffer, ref index); // qpcSync
                    ReadCompressedUInt64(buffer, ref index); // utcSync
                    return true;
                }
                case AsyncEventID.UnwindAsyncException:
                {
                    ReadCompressedUInt32(buffer, ref index);
                    return true;
                }
                case AsyncEventID.CreateAsyncCallstack:
                case AsyncEventID.ResumeAsyncCallstack:
                case AsyncEventID.SuspendAsyncCallstack:
                case AsyncEventID.AppendAsyncCallstack:
                {
                    SkipCallstackPayload(buffer, ref index);
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        private static uint ReadCompressedUInt32(ReadOnlySpan<byte> buffer, ref int index)
        {
            Deserializer.ReadCompressedUInt32(buffer, ref index, out uint value);
            return value;
        }

        private static ulong ReadCompressedUInt64(ReadOnlySpan<byte> buffer, ref int index)
        {
            Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong value);
            return value;
        }

        private static void SkipCallstackPayload(ReadOnlySpan<byte> buffer, ref int index)
        {
            ReadCallstackPayload(buffer, ref index, out _, out _);
        }

        private static void ReadCallstackPayload(ReadOnlySpan<byte> buffer, ref int index,
            out byte frameCount, out List<(ulong MethodId, int State)> frames)
        {
            ReadCallstackPayload(buffer, ref index, out _, out _, out frameCount, out frames);
        }

        private static void ReadCallstackPayload(ReadOnlySpan<byte> buffer, ref int index,
            out ulong taskId, out AsyncCallstackType callstackType, out byte frameCount, out List<(ulong MethodId, int State)> frames)
        {
            callstackType = (AsyncCallstackType)buffer[index++];
            index++;
            frameCount = buffer[index++];
            taskId = ReadCompressedUInt64(buffer, ref index);
            frames = new List<(ulong, int)>(frameCount);

            if (frameCount == 0)
            {
                return;
            }

            ulong currentMethodId = ReadCompressedUInt64(buffer, ref index);
            int state = ReadCompressedInt32(buffer, ref index);
            frames.Add((currentMethodId, state));

            for (int i = 1; i < frameCount; i++)
            {
                long delta = ReadCompressedInt64(buffer, ref index);
                state = ReadCompressedInt32(buffer, ref index);
                currentMethodId = (ulong)((long)currentMethodId + delta);
                frames.Add((currentMethodId, state));
            }
        }

        private static int ReadCompressedInt32(ReadOnlySpan<byte> buffer, ref int index)
        {
            Deserializer.ReadCompressedInt32(buffer, ref index, out int value);
            return value;
        }

        private static long ReadCompressedInt64(ReadOnlySpan<byte> buffer, ref int index)
        {
            Deserializer.ReadCompressedInt64(buffer, ref index, out long value);
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
            {
                return null;
            }

            int index = 1;
            Deserializer.ReadUInt32(buffer, ref index, out uint totalSize);
            Deserializer.ReadUInt32(buffer, ref index, out uint contextId);
            Deserializer.ReadUInt64(buffer, ref index, out ulong threadId);
            Deserializer.ReadUInt32(buffer, ref index, out uint eventCount);
            Deserializer.ReadUInt64(buffer, ref index, out ulong startTs);
            Deserializer.ReadUInt64(buffer, ref index, out ulong endTs);

            return new EventBufferHeader(buffer[0], totalSize, contextId, threadId, eventCount, startTs, endTs);
        }

        private sealed class ParsedEvent
        {
            public AsyncEventID EventId { get; init; }
            public long Timestamp { get; init; }
            public ulong OsThreadId { get; init; }

            public ulong TaskId { get; init; }

            public AsyncCallstackType CallstackType { get; init; }
            public byte FrameCount { get; init; }
            public List<(ulong MethodId, int State)> Frames { get; init; } = [];

            public uint UnwindFrameCount { get; init; }

            public MetadataFromBuffer? Metadata { get; init; }

            public ulong SyncClockQpc { get; init; }
            public ulong SyncClockUtc { get; init; }

            public bool HasMarkerFrame(string markerMethodName)
            {
                if (Frames.Count == 0)
                {
                    return false;
                }

                foreach (var (methodId, _) in Frames)
                {
                    string? methodName = GetMethodNameFromMethodId(CallstackType, methodId);
                    if (methodName is not null && methodName.Contains(markerMethodName, StringComparison.Ordinal))
                    {
                        return true;
                    }
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

            // All events in timestamp order.
            public IReadOnlyList<ParsedEvent> All => _events;

            // All distinct event IDs present in the stream.
            public IEnumerable<AsyncEventID> EventIds => _events.Select(e => e.EventId).Distinct();

            // Filter events by event ID, in timestamp order.
            public IEnumerable<ParsedEvent> OfType(AsyncEventID eventId) =>
                _events.Where(e => e.EventId == eventId);

            // Filter events by multiple event IDs, in timestamp order.
            public IEnumerable<ParsedEvent> OfTypes(params AsyncEventID[] eventIds)
            {
                var set = new HashSet<AsyncEventID>(eventIds);
                return _events.Where(e => set.Contains(e.EventId));
            }

            // Get events grouped by Task.Id, each group in timestamp order.
            public Dictionary<ulong, List<ParsedEvent>> ByTaskId()
            {
                if (_byTaskId is not null)
                {
                    return _byTaskId;
                }

                _byTaskId = new Dictionary<ulong, List<ParsedEvent>>();
                foreach (var evt in _events)
                {
                    if (evt.TaskId == 0)
                    {
                        continue;
                    }

                    if (!_byTaskId.TryGetValue(evt.TaskId, out var list))
                    {
                        list = new List<ParsedEvent>();
                        _byTaskId[evt.TaskId] = list;
                    }

                    list.Add(evt);
                }

                return _byTaskId;
            }

            // Get events for a specific Task.Id in timestamp order.
            public List<ParsedEvent> ForTask(ulong taskId) =>
                ByTaskId().TryGetValue(taskId, out var list) ? list : new List<ParsedEvent>();

            // Get callstack events (of specified type) that contain the marker method in their frames.
            // Results are in timestamp order.
            public List<ParsedEvent> CallstacksWithMarker(AsyncEventID callstackEventId, string markerMethodName) =>
                _events.Where(e => e.EventId == callstackEventId && e.HasMarkerFrame(markerMethodName)).ToList();

            // Reconstructs full resume callstacks by merging each ResumeAsyncCallstack with any subsequent
            // AppendAsyncCallstack events for the same context, up until the next Suspend/Complete on that
            // context.
            //
            // V1 dispatchers may emit a partial Resume callstack when the parent continuation hasn't yet
            // registered (race between dispatcher pickup and parent's AwaitUnsafeOnCompleted). Frames that
            // register later are emitted as AppendAsyncCallstack at the next hook point. Merging produces
            // the complete chain that was observable during the dispatcher's lifetime.
            //
            // Returns one ParsedEvent per Resume, with Frames and FrameCount reflecting the merged total.
            public List<ParsedEvent> MergedResumeCallstacks()
            {
                var result = new List<ParsedEvent>();
                var openByTaskId = new Dictionary<ulong, int>();

                foreach (var evt in _events)
                {
                    switch (evt.EventId)
                    {
                        case AsyncEventID.ResumeAsyncCallstack:
                        {
                            var merged = new ParsedEvent
                            {
                                EventId = evt.EventId,
                                Timestamp = evt.Timestamp,
                                OsThreadId = evt.OsThreadId,
                                TaskId = evt.TaskId,
                                CallstackType = evt.CallstackType,
                                FrameCount = evt.FrameCount,
                                Frames = new List<(ulong MethodId, int State)>(evt.Frames),
                            };

                            openByTaskId[evt.TaskId] = result.Count;
                            result.Add(merged);

                            break;
                        }
                        case AsyncEventID.AppendAsyncCallstack:
                        {
                            if (openByTaskId.TryGetValue(evt.TaskId, out int idx))
                            {
                                ParsedEvent existing = result[idx];
                                var combinedFrames = new List<(ulong MethodId, int State)>(existing.Frames);
                                combinedFrames.AddRange(evt.Frames);

                                result[idx] = new ParsedEvent
                                {
                                    EventId = existing.EventId,
                                    Timestamp = existing.Timestamp,
                                    OsThreadId = existing.OsThreadId,
                                    TaskId = existing.TaskId,
                                    CallstackType = existing.CallstackType,
                                    FrameCount = (byte)Math.Min(combinedFrames.Count, byte.MaxValue),
                                    Frames = combinedFrames,
                                };
                            }

                            break;
                        }
                        case AsyncEventID.SuspendAsyncContext:
                        case AsyncEventID.CompleteAsyncContext:
                        {
                            openByTaskId.Remove(evt.TaskId);
                            break;
                        }
                    }
                }

                return result;
            }

            // Get merged resume callstacks (Resume + subsequent Appends) that contain the marker method
            // in any of their merged frames.
            public List<ParsedEvent> MergedResumeCallstacksWithMarker(string markerMethodName) =>
                MergedResumeCallstacks().Where(e => e.HasMarkerFrame(markerMethodName)).ToList();

            // Get callstack events (of specified type) that contain the marker method,
            // taking only the first match per Task.Id (deepest chain by timestamp).
            public List<ParsedEvent> CallstacksWithMarkerFirstPerTask(AsyncEventID callstackEventId, string markerMethodName)
            {
                List<ParsedEvent> matched = CallstacksWithMarker(callstackEventId, markerMethodName);
                var seen = new HashSet<ulong>();
                var result = new List<ParsedEvent>();

                foreach (var evt in matched)
                {
                    if (evt.TaskId != 0 && seen.Add(evt.TaskId))
                    {
                        result.Add(evt);
                    }
                }

                return result;
            }

            // Get all metadata events.
            public List<MetadataFromBuffer> MetadataEvents =>
                _events.Where(e => e.Metadata.HasValue).Select(e => e.Metadata!.Value).ToList();

            // Get distinct OS thread IDs across all events.
            public HashSet<ulong> OsThreadIds => new(_events.Select(e => e.OsThreadId).Where(id => id != 0));
        }

        private static ParsedEventStream ParseAllEvents(CollectedEvents events)
        {
            var allEvents = new List<ParsedEvent>();

            ForEachEventBufferPayload(events.Events, buffer =>
            {
                EventBufferHeader? header = ParseEventBufferHeader(buffer);
                if (header is null)
                {
                    return;
                }

                ulong osThreadId = header.Value.OsThreadId;
                ulong currentTaskId = 0;
                var taskIdStack = new Stack<ulong>();
                int index = HeaderSize;
                long baseTimestamp = (long)header.Value.StartTimestamp;

                while (index < buffer.Length)
                {
                    if (index + 2 > buffer.Length)
                    {
                        break;
                    }

                    AsyncEventID eventId = (AsyncEventID)buffer[index++];
                    long delta = (long)ReadCompressedUInt64(buffer, ref index);
                    baseTimestamp += delta;

                    ParsedEvent evt = eventId switch
                    {
                        AsyncEventID.CreateAsyncContext or AsyncEventID.ResumeAsyncContext =>
                            ParseContextEvent(eventId, baseTimestamp, osThreadId, buffer, ref index, ref currentTaskId, taskIdStack),

                        AsyncEventID.CompleteAsyncContext =>
                            ParseCompleteContextEvent(baseTimestamp, osThreadId, ref currentTaskId, taskIdStack),

                        AsyncEventID.SuspendAsyncContext or
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
                        AsyncEventID.SuspendAsyncCallstack or AsyncEventID.AppendAsyncCallstack =>
                            ParseCallstackEvent(eventId, baseTimestamp, osThreadId, buffer, ref index, ref currentTaskId, taskIdStack),

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
                ReadOnlySpan<byte> buffer, ref int index, ref ulong currentTaskId, Stack<ulong> taskIdStack)
            {
                ulong id = ReadCompressedUInt64(buffer, ref index);
                if (eventId == AsyncEventID.ResumeAsyncContext && id != currentTaskId)
                {
                    taskIdStack.Push(currentTaskId);
                }

                currentTaskId = id;

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = id
                };
            }

            static ParsedEvent ParseCompleteContextEvent(long timestamp, ulong osThreadId,
                ref ulong currentTaskId, Stack<ulong> taskIdStack)
            {
                ulong completedTaskId = currentTaskId;
                currentTaskId = taskIdStack.Count > 0 ? taskIdStack.Pop() : 0;

                return new ParsedEvent
                {
                    EventId = AsyncEventID.CompleteAsyncContext,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = completedTaskId
                };
            }

            static ParsedEvent ParseResetEvent(AsyncEventID eventId, long timestamp, ulong osThreadId, ref ulong currentTaskId)
            {
                ulong prevTaskId = currentTaskId;
                if (eventId == AsyncEventID.ResetAsyncThreadContext)
                {
                    currentTaskId = 0;
                }

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = prevTaskId
                };
            }

            static ParsedEvent ParseCallstackEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ReadOnlySpan<byte> buffer, ref int index, ref ulong currentTaskId, Stack<ulong> taskIdStack)
            {
                ReadCallstackPayload(buffer, ref index, out ulong taskId, out AsyncCallstackType callstackType, out byte frameCount, out var frames);
                if (taskId != currentTaskId)
                {
                    taskIdStack.Push(currentTaskId);
                    currentTaskId = taskId;
                }

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    TaskId = taskId,
                    CallstackType = callstackType,
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
            EventBuffer.DumpAllEvents(events);
        }

        private static void RunScenarioAndFlush(Func<Task> scenario)
        {
            // V1 (task-based) async: the dispatcher's finally block emits CompleteAsyncContext
            // after inner.MoveNext() returns, but MoveNext() already set the task result which
            // unblocks this thread. Brief sleep ensures the pool thread's finally completes.
            // V2 (runtime-async) does not have this issue -- Complete fires inside the dispatch
            // loop before the task is signaled.
            //
            // Clear SynchronizationContext so RuntimeAsync continuations don't capture
            // xunit's context, which would cause per-frame re-queuing instead of inlining.
            SynchronizationContext? prevCtx = SynchronizationContext.Current;
            int originalThreadId = Environment.CurrentManagedThreadId;
            SynchronizationContext.SetSynchronizationContext(null);

            try
            {
                Task.Run(scenario).GetAwaiter().GetResult();
            }
            finally
            {
                Thread.Sleep(50);

                // Only restore the SynchronizationContext if we're still on the same thread.
                // ConfigureAwait(false) may resume on a different thread pool thread, and
                // setting the original thread's context there would be incorrect.
                if (Environment.CurrentManagedThreadId == originalThreadId)
                {
                    SynchronizationContext.SetSynchronizationContext(prevCtx);
                }

                SendFlushCommand();
            }
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
                    SynchronizationContext? prevCtx = SynchronizationContext.Current;
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

                        // Post-flush inside finally so buffered events from before an exception
                        // still reach the listener (otherwise on a scenario throw the trace would
                        // be truncated, hiding what happened up to the failure point).
                        SendFlushCommand();
                    }
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

        private static bool HasCallstackWithExpectedFrames(List<ParsedEvent> callstacks, string[] expectedFrames)
        {
            foreach (var cs in callstacks)
            {
                var resolvedNames = cs.Frames
                    .Select(f => GetMethodNameFromMethodId(cs.CallstackType, f.MethodId))
                    .ToList();

                int matchIndex = 0;
                for (int i = 0; i < resolvedNames.Count && matchIndex < expectedFrames.Length; i++)
                {
                    if (resolvedNames[i] is not null && resolvedNames[i]!.Contains(expectedFrames[matchIndex], StringComparison.Ordinal))
                    {
                        matchIndex++;
                    }
                }

                if (matchIndex == expectedFrames.Length)
                {
                    return true;
                }
            }
            return false;
        }

        // For a given context, simulates the async callstack depth by walking events in order:
        // ResumeAsyncCallstack sets the depth to frame count, CompleteAsyncMethod decrements,
        // UnwindAsyncException subtracts unwound frames. Asserts depth reaches zero.
        private static void AssertCallstackSimulationReachesZero(ParsedEventStream stream, string markerMethodName)
        {
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
                    {
                        stackDepth = (int)evt.FrameCount;
                        break;
                    }
                    case AsyncEventID.CompleteAsyncMethod:
                    {
                        if (stackDepth > 0)
                        {
                            stackDepth--;
                        }

                        break;
                    }
                    case AsyncEventID.UnwindAsyncException:
                    {
                        stackDepth = Math.Max(0, stackDepth - (int)evt.UnwindFrameCount);
                        break;
                    }
                }
            }

            Assert.Equal(0, stackDepth);
        }

        private static void AssertExactlyOneCreateAndComplete(ParsedEventStream stream, ulong taskId, string chainName)
        {
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();
            int creates = ids.Count(id => id == AsyncEventID.CreateAsyncContext);
            int completes = ids.Count(id => id == AsyncEventID.CompleteAsyncContext);
            Assert.True(creates == 1, $"Expected exactly 1 CreateAsyncContext for {chainName} (TaskId {taskId}), got {creates}");
            Assert.True(completes == 1, $"Expected exactly 1 CompleteAsyncContext for {chainName} (TaskId {taskId}), got {completes}");
        }

        // V1-friendly variant: V1's per-MoveNext dispatcher model emits one Create per await
        // suspension, so a method with N awaits produces N dispatchers / N Creates on the same
        // TaskId. The invariant we can still assert is creates == completes (both >= 1).
        private static void AssertCreateEqualsCompleteForTask(ParsedEventStream stream, ulong taskId, string chainName)
        {
            var ids = stream.ForTask(taskId).Select(e => e.EventId).ToList();
            int creates = ids.Count(id => id == AsyncEventID.CreateAsyncContext);
            int completes = ids.Count(id => id == AsyncEventID.CompleteAsyncContext);
            Assert.True(creates >= 1, $"Expected at least 1 CreateAsyncContext for {chainName} (TaskId {taskId}), got {creates}");
            Assert.True(creates == completes, $"Expected CreateAsyncContext count == CompleteAsyncContext count for {chainName} (TaskId {taskId}), got {creates} creates and {completes} completes");
        }

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

        private static class Deserializer
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

        private static class EventBuffer
        {
            public static void DumpAllEvents(CollectedEvents events)
            {
                ForEachEventBufferPayload(events.Events, buffer => EventBuffer.OutputEventBuffer(buffer));
                OutputFooter();
            }

            private static int OutputEventBuffer(ReadOnlySpan<byte> buffer)
            {
                OutputHeader("Async Event Buffer");

                int index = 0;

                if ((uint)buffer.Length < 1)
                {
                    Console.WriteLine("Buffer too small.");
                    OutputFooter();
                    return index;
                }

                byte version = buffer[index++];
                Console.WriteLine($"Version: {version}");

                if (version != 1)
                {
                    Console.WriteLine($"Unsupported version: {version}");
                    OutputFooter();
                    return index;
                }

                Deserializer.ReadUInt32(buffer, ref index, out uint totalSize);
                Deserializer.ReadUInt32(buffer, ref index, out uint contextId);
                Deserializer.ReadUInt64(buffer, ref index, out ulong osThreadId);
                Deserializer.ReadUInt32(buffer, ref index, out uint totalEventCount);
                Deserializer.ReadUInt64(buffer, ref index, out ulong startTimestamp);
                Deserializer.ReadUInt64(buffer, ref index, out ulong endTimestamp);

                Console.WriteLine($"TotalSize: {totalSize}");
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

                    OutputHeader(eventCount, eventId, currentTimestamp);

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
                            AsyncEventID.CreateAsyncCallstack => OutputAsyncCallstackEvent(buffer.Slice(index)),
                            AsyncEventID.ResumeAsyncCallstack => OutputAsyncCallstackEvent(buffer.Slice(index)),
                            AsyncEventID.SuspendAsyncCallstack => OutputAsyncCallstackEvent(buffer.Slice(index)),
                            AsyncEventID.AppendAsyncCallstack => OutputAsyncCallstackEvent(buffer.Slice(index)),
                            AsyncEventID.ResumeAsyncMethod => OutputResumeAsyncMethodEvent(),
                            AsyncEventID.CompleteAsyncMethod => OutputCompleteAsyncMethodEvent(),
                            AsyncEventID.ResetAsyncThreadContext => OutputResetAsyncThreadContextEvent(),
                            AsyncEventID.ResetAsyncContinuationWrapperIndex => OutputResetAsyncContinuationWrapperIndexEvent(),
                            AsyncEventID.AsyncProfilerMetadata => OutputAsyncProfilerMetadataEvent(buffer.Slice(index)),
                            AsyncEventID.AsyncProfilerSyncClock => OutputAsyncProfilerSyncClockEvent(buffer.Slice(index)),
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

                return index;
            }

            private const int OutputEventSeparatorWidth = 80;

            private static void OutputHeader()
            {
                Console.WriteLine($"{new string('-', OutputEventSeparatorWidth)}");
            }

            private static void OutputHeader(string header) => Console.WriteLine($"{FormatCenteredLabel(header)}");

            private static void OutputHeader(int eventCount, AsyncEventID id, ulong timestamp) =>
                Console.WriteLine($"[{eventCount}] {id} (0x{timestamp:X16})");

            private static string FormatCenteredLabel(string label)
            {
                int totalDashes = Math.Max(6, OutputEventSeparatorWidth - label.Length - 2);
                int leftDashes = totalDashes / 2;
                int rightDashes = totalDashes - leftDashes;
                return $"{new string('-', leftDashes)} {label} {new string('-', rightDashes)}";
            }

            private static void OutputFooter()
            {
                Console.WriteLine(new string('-', OutputEventSeparatorWidth));
            }

            private static int OutputCreateAsyncContextEvent(ReadOnlySpan<byte> buffer)
            {
                int index = 0;
                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong id);
                Console.WriteLine($"  ID: {id}");
                return index;
            }

            private static int OutputResumeAsyncContextEvent(ReadOnlySpan<byte> buffer)
            {
                int index = 0;
                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong id);
                Console.WriteLine($"  ID: {id}");
                return index;
            }

            private static int OutputSuspendAsyncContextEvent()
            {
                return 0;
            }

            private static int OutputCompleteAsyncContextEvent()
            {
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
                Console.WriteLine($"  Unwinded Frames: {unwindedFrames}");
                return 0;
            }

            private static int OutputResumeAsyncMethodEvent()
            {
                return 0;
            }

            private static int OutputCompleteAsyncMethodEvent()
            {
                return 0;
            }

            private static int OutputResetAsyncContinuationWrapperIndexEvent()
            {
                return 0;
            }

            private static int OutputResetAsyncThreadContextEvent()
            {
                return 0;
            }

            private static int OutputAsyncProfilerMetadataEvent(ReadOnlySpan<byte> buffer)
            {
                int index = 0;

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

                return index;
            }

            private static int OutputAsyncProfilerSyncClockEvent(ReadOnlySpan<byte> buffer)
            {
                int index = 0;

                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong qpcSync);
                Console.WriteLine($"  QPCSync: {qpcSync}");

                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong utcSync);
                Console.WriteLine($"  UTCSync: {utcSync}");

                return index;
            }

            private static int OutputAsyncCallstackEvent(ReadOnlySpan<byte> buffer)
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

                Console.WriteLine($"  ID: {id}");
                Console.WriteLine($"  Type: {type}");
                Console.WriteLine($"  CallstackId: {callstackId}");
                Console.WriteLine($"  Length: {asyncCallstackLength}");

                if (asyncCallstackLength == 0)
                {
                    return index;
                }

                ulong previousMethodId;
                ulong currentMethodId;
                int state;

                Deserializer.ReadCompressedUInt64(buffer, ref index, out currentMethodId);
                Deserializer.ReadCompressedInt32(buffer, ref index, out state);

                OutputAsyncFrame((AsyncCallstackType)type, currentMethodId, state, 0);

                for (int i = 1; i < asyncCallstackLength; i++)
                {
                    previousMethodId = currentMethodId;
                    Deserializer.ReadCompressedInt64(buffer, ref index, out long methodIdDelta);
                    Deserializer.ReadCompressedInt32(buffer, ref index, out state);
                    currentMethodId = previousMethodId + (ulong)methodIdDelta;
                    OutputAsyncFrame((AsyncCallstackType)type, currentMethodId, state, i);
                }

                return index;
            }

            private static void OutputAsyncFrame(AsyncCallstackType type, ulong methodId, int state, int frameIndex)
            {
                string asyncMethodName = GetMethodNameFromMethodId(type, methodId) ?? "??";
                Console.WriteLine($"    [{frameIndex}] {asyncMethodName} (0x{methodId:X}) (state={state})");
            }
        }
    }
}
