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
        // V2 (RuntimeAsync) events.
        RuntimeAsync_CreateAsyncContext = 1,
        RuntimeAsync_ResumeAsyncContext = 2,
        RuntimeAsync_SuspendAsyncContext = 3,
        RuntimeAsync_CompleteAsyncContext = 4,
        RuntimeAsync_UnwindAsyncException = 5,
        RuntimeAsync_CreateAsyncCallstack = 6,
        RuntimeAsync_ResumeAsyncCallstack = 7,
        RuntimeAsync_SuspendAsyncCallstack = 8,
        RuntimeAsync_ResumeAsyncMethod = 9,
        RuntimeAsync_CompleteAsyncMethod = 10,

        // V1 (TaskAsync) events.
        TaskAsync_CreateAsyncContext = 11,
        TaskAsync_ResumeAsyncContext = 12,
        TaskAsync_SuspendAsyncContext = 13,
        TaskAsync_CompleteAsyncContext = 14,
        TaskAsync_UnwindAsyncException = 15,
        TaskAsync_ResumeAsyncCallstack = 16,
        TaskAsync_ResumeAsyncMethod = 17,
        TaskAsync_CompleteAsyncMethod = 18,
        TaskAsync_AppendAsyncCallstack = 19,

        // Neutral profiler events.
        ResetAsyncThreadContext = 20,
        ResetAsyncContinuationWrapperIndex = 21,
        AsyncProfilerMetadata = 22,
        AsyncProfilerSyncClock = 23,
    }

    public enum AsyncCallstackType : byte
    {
        Compiler = 0x1,
        Runtime = 0x2,
    }

    internal readonly record struct EventManifestEntry(AsyncEventID EventId, byte Version, byte FieldSize);

    internal static class EventManifest
    {
        // Default manifest matching the current runtime emit; replaced when the parser
        // observes an AsyncProfilerMetadata event carrying a per-event manifest.
        // Dense, ordered by ascending (byte)AsyncEventID; lookups use (byte)id - 1.
        public static readonly EventManifestEntry[] DefaultEntries = BuildDefaultEntries();

        private static EventManifestEntry[] BuildDefaultEntries()
        {
            const byte NoPayload = 0;
            const byte BytePayloadLength = 1;
            const byte UShortPayloadLength = 2;

            return
            [
                new EventManifestEntry(AsyncEventID.RuntimeAsync_CreateAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_ResumeAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_SuspendAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_CompleteAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_UnwindAsyncException, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_CreateAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_ResumeAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_SuspendAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_ResumeAsyncMethod, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.RuntimeAsync_CompleteAsyncMethod, 1, NoPayload),

                new EventManifestEntry(AsyncEventID.TaskAsync_CreateAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.TaskAsync_ResumeAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.TaskAsync_SuspendAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.TaskAsync_CompleteAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.TaskAsync_UnwindAsyncException, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.TaskAsync_ResumeAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.TaskAsync_ResumeAsyncMethod, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.TaskAsync_CompleteAsyncMethod, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.TaskAsync_AppendAsyncCallstack, 1, UShortPayloadLength),

                new EventManifestEntry(AsyncEventID.ResetAsyncThreadContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.ResetAsyncContinuationWrapperIndex, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.AsyncProfilerMetadata, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.AsyncProfilerSyncClock, 1, BytePayloadLength),
            ];
        }
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

        // V1 (TaskAsync_*) async-task instrumentation is opt-out on NativeAOT to avoid
        // ~100KB of per-state-machine generic instantiation overhead. Tests that depend
        // on V1 events must be gated on these properties so they are skipped on NAOT.
        public static bool IsTaskAsyncInstrumentationSupported =>
            IsRuntimeAsyncSupported && !PlatformDetection.IsNativeAot;

        public static bool IsTaskAsyncInstrumentationAndThreadingSupported =>
            IsRuntimeAsyncAndThreadingSupported && !PlatformDetection.IsNativeAot;

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

        // The public 1-arg MethodBase.GetMethodFromHandle resolves the method but then deliberately
        // throws ArgumentException when the declaring type is generic. The internal
        // RuntimeType.GetMethodBase(RuntimeMethodHandle.GetMethodInfo()) it calls underneath has no
        // such guard, so we invoke it directly to name V1 frames on generic declaring types.
        private static readonly MethodInfo? s_runtimeMethodHandleGetMethodInfo =
            typeof(RuntimeMethodHandle).GetMethod("GetMethodInfo", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo? s_runtimeTypeGetMethodBase =
            // typeof(object) is a RuntimeType instance; its runtime type is System.RuntimeType.
            typeof(object).GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "GetMethodBase"
                    && m.GetParameters() is { Length: 1 } p
                    && p[0].ParameterType.Name == "IRuntimeMethodInfo");

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
                    MethodBase? method = null;
                    try
                    {
                        method = MethodBase.GetMethodFromHandle(handle);
                    }
                    catch (ArgumentException)
                    {
                        // The 1-arg GetMethodFromHandle cannot resolve handles whose declaring
                        // type is generic (e.g. xUnit's TestClassRunner<TTestCase>+<...>d__N);
                        // it requires the 2-arg overload with an explicit declaring type, which
                        // we cannot recover from a bare MethodId. Real ETW/EventPipe consumers
                        // get the declaring type from method-metadata rundown events.
                        //
                        // As a test-only fallback, resolve via the internal
                        // RuntimeType.GetMethodBase the public API uses before its generic guard,
                        // which does name methods on generic declaring types. This mirrors what a
                        // rundown-backed consumer would achieve.
                        method = ResolveCompilerMethodOnGenericType(handle);
                    }

                    if (method?.DeclaringType is Type declaringType)
                    {
                        string methodName = declaringType.Name;

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

        // Test-only fallback used when a compiler (V1) MethodId handle cannot be resolved via the
        // 1-arg MethodBase.GetMethodFromHandle because its declaring type is generic. Calls the
        // internal RuntimeType.GetMethodBase(handle.GetMethodInfo()) the public API uses underneath,
        // which has no generic-declaring-type guard, so it names methods on generic declaring types.
        // Returns null if the internals are unavailable (e.g. NativeAOT) or resolution throws.
        private static MethodBase? ResolveCompilerMethodOnGenericType(RuntimeMethodHandle handle)
        {
            if (s_runtimeMethodHandleGetMethodInfo is null || s_runtimeTypeGetMethodBase is null)
            {
                return null;
            }

            try
            {
                object? methodInfo = s_runtimeMethodHandleGetMethodInfo.Invoke(handle, null);
                if (methodInfo is null)
                {
                    return null;
                }

                return (MethodBase?)s_runtimeTypeGetMethodBase.Invoke(null, new object[] { methodInfo });
            }
            catch (Exception)
            {
                return null;
            }
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

        // Reads the per-event payload length prefix (0, 1, or 2 bytes depending on the event manifest)
        // and advances the index past it. Returns the declared payload length so callers can validate
        // that downstream parsers consume exactly the indicated bytes.
        private static int ReadPayloadLengthPrefix(ReadOnlySpan<byte> buffer, AsyncEventID eventId, ref int index)
        {
            int eventIdIndex = (byte)eventId - 1;
            int payloadLengthFieldSize = (uint)eventIdIndex < (uint)EventManifest.DefaultEntries.Length
                ? EventManifest.DefaultEntries[eventIdIndex].FieldSize
                : 0;

            if (payloadLengthFieldSize == 0)
            {
                return 0;
            }
            if (payloadLengthFieldSize == 1)
            {
                return buffer[index++];
            }

            int payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(index));
            index += 2;
            return payloadLength;
        }

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

                int payloadLength = ReadPayloadLengthPrefix(buffer, eventId, ref index);
                int payloadStartIndex = index;
                if (!visitor(eventId, baseTimestamp, buffer, ref index))
                {
                    break;
                }

                Assert.Equal(payloadLength, index - payloadStartIndex);
            }
        }

        private static bool SkipEventPayload(AsyncEventID eventId, ReadOnlySpan<byte> buffer, ref int index)
        {
            switch (eventId)
            {
                case AsyncEventID.RuntimeAsync_CreateAsyncContext:
                case AsyncEventID.TaskAsync_CreateAsyncContext:
                {
                    ReadCompressedUInt64(buffer, ref index); // parentDispatcherId
                    ReadCompressedUInt64(buffer, ref index); // dispatcherId
                    return true;
                }
                case AsyncEventID.RuntimeAsync_ResumeAsyncContext:
                case AsyncEventID.TaskAsync_ResumeAsyncContext:
                {
                    ReadCompressedUInt64(buffer, ref index); // dispatcherId
                    return true;
                }
                case AsyncEventID.RuntimeAsync_SuspendAsyncContext:
                case AsyncEventID.RuntimeAsync_CompleteAsyncContext:
                case AsyncEventID.RuntimeAsync_ResumeAsyncMethod:
                case AsyncEventID.RuntimeAsync_CompleteAsyncMethod:
                case AsyncEventID.TaskAsync_SuspendAsyncContext:
                case AsyncEventID.TaskAsync_CompleteAsyncContext:
                case AsyncEventID.TaskAsync_ResumeAsyncMethod:
                case AsyncEventID.TaskAsync_CompleteAsyncMethod:
                case AsyncEventID.ResetAsyncThreadContext:
                case AsyncEventID.ResetAsyncContinuationWrapperIndex:
                {
                    return true;
                }
                case AsyncEventID.RuntimeAsync_UnwindAsyncException:
                case AsyncEventID.TaskAsync_UnwindAsyncException:
                {
                    ReadCompressedUInt32(buffer, ref index);
                    return true;
                }
                case AsyncEventID.RuntimeAsync_CreateAsyncCallstack:
                case AsyncEventID.RuntimeAsync_ResumeAsyncCallstack:
                case AsyncEventID.RuntimeAsync_SuspendAsyncCallstack:
                case AsyncEventID.TaskAsync_ResumeAsyncCallstack:
                case AsyncEventID.TaskAsync_AppendAsyncCallstack:
                {
                    SkipCallstackPayload(buffer, ref index, eventId, CallstackTypeFromEventId(eventId));
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

        private static void SkipCallstackPayload(ReadOnlySpan<byte> buffer, ref int index, AsyncEventID eventId, AsyncCallstackType callstackType)
        {
            ReadCallstackPayload(buffer, ref index, eventId, callstackType, out _, out _, out _, out _, out _);
        }

        private static void ReadCallstackPayload(ReadOnlySpan<byte> buffer, ref int index, AsyncEventID eventId,
            out byte frameCount, out List<(ulong MethodId, int State)> frames)
        {
            ReadCallstackPayload(buffer, ref index, eventId, AsyncCallstackType.Runtime, out _, out _, out _, out frameCount, out frames);
        }

        private static void ReadCallstackPayload(ReadOnlySpan<byte> buffer, ref int index, AsyncEventID eventId, AsyncCallstackType callstackType,
            out ulong parentDispatcherId, out ulong dispatcherId, out byte continuationIndex, out byte frameCount, out List<(ulong MethodId, int State)> frames)
        {
            index++; // Reserved callstack ID (for future callstack interning).
            continuationIndex = buffer[index++];
            frameCount = buffer[index++];
            // parentDispatcherId is only present on RuntimeAsync_CreateAsyncCallstack.
            parentDispatcherId = eventId == AsyncEventID.RuntimeAsync_CreateAsyncCallstack
                ? ReadCompressedUInt64(buffer, ref index)
                : 0;
            dispatcherId = ReadCompressedUInt64(buffer, ref index);
            frames = new List<(ulong, int)>(frameCount);

            if (frameCount == 0)
            {
                // Cached callstack reference (frame data resolved out-of-band by callstack id).
                return;
            }

            bool readState = callstackType == AsyncCallstackType.Compiler;

            ulong currentMethodId = ReadCompressedUInt64(buffer, ref index);
            int state = readState ? ReadCompressedInt32(buffer, ref index) : 0;
            frames.Add((currentMethodId, state));

            for (int i = 1; i < frameCount; i++)
            {
                long delta = ReadCompressedInt64(buffer, ref index);
                state = readState ? ReadCompressedInt32(buffer, ref index) : 0;
                currentMethodId = (ulong)((long)currentMethodId + delta);
                frames.Add((currentMethodId, state));
            }
        }

        // Derive callstack type from the event id:
        // V1 (TaskAsync_*) events carry compiler-built state machine frames;
        // V2 (RuntimeAsync_*) events carry runtime-async frames.
        private static AsyncCallstackType CallstackTypeFromEventId(AsyncEventID eventId)
            => eventId is AsyncEventID.TaskAsync_ResumeAsyncCallstack
                       or AsyncEventID.TaskAsync_AppendAsyncCallstack
                ? AsyncCallstackType.Compiler
                : AsyncCallstackType.Runtime;

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

            // Per-event manifest: [count: byte] followed by [id, version, payloadLengthFieldSize] triples.
            byte manifestCount = buffer[index++];
            index += manifestCount * 3;
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
            Deserializer.ReadUInt32(buffer, ref index, out uint asyncThreadContextId);
            Deserializer.ReadUInt64(buffer, ref index, out ulong threadId);
            Deserializer.ReadUInt32(buffer, ref index, out uint eventCount);
            Deserializer.ReadUInt64(buffer, ref index, out ulong startTs);
            Deserializer.ReadUInt64(buffer, ref index, out ulong endTs);

            return new EventBufferHeader(buffer[0], totalSize, asyncThreadContextId, threadId, eventCount, startTs, endTs);
        }

        private sealed class ParsedEvent
        {
            public AsyncEventID EventId { get; init; }
            public long Timestamp { get; init; }
            public ulong OsThreadId { get; init; }

            public ulong ParentDispatcherId { get; init; }

            public ulong DispatcherId { get; init; }

            public AsyncCallstackType CallstackType { get; init; }
            public byte ContinuationIndex { get; init; }
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
            private Dictionary<ulong, List<ParsedEvent>>? _byDispatcherId;
            private Dictionary<ulong, ulong>? _parentOfDispatcher;
            private Dictionary<ulong, List<ulong>>? _childrenOfDispatcher;
            private Dictionary<ulong, DispatcherKind>? _dispatcherKind;

            // V1 (TaskAsync_*) and V2 (RuntimeAsync_*) dispatchers can coexist on the same logical
            // thread (e.g., a V1 test runner hosting a V2 test body). The runtime captures cross-kind
            // parent links in that case, but a single test typically wants to walk only its own kind's
            // subtree. Tracking the kind per dispatcher lets the chain walk stop at V1<->V2 boundaries
            // by default.
            internal enum DispatcherKind { Unknown, V1, V2 }

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

            // Get events grouped by DispatcherId (the dispatcher that produced the event), each group in timestamp order.
            public Dictionary<ulong, List<ParsedEvent>> ByDispatcherId()
            {
                if (_byDispatcherId is not null)
                {
                    return _byDispatcherId;
                }

                _byDispatcherId = new Dictionary<ulong, List<ParsedEvent>>();
                foreach (var evt in _events)
                {
                    if (evt.DispatcherId == 0)
                    {
                        continue;
                    }

                    if (!_byDispatcherId.TryGetValue(evt.DispatcherId, out var list))
                    {
                        list = new List<ParsedEvent>();
                        _byDispatcherId[evt.DispatcherId] = list;
                    }

                    list.Add(evt);
                }

                return _byDispatcherId;
            }

            // Get events for a specific DispatcherId in timestamp order.
            public List<ParsedEvent> ForDispatcher(ulong dispatcherId) =>
                ByDispatcherId().TryGetValue(dispatcherId, out var list) ? list : new List<ParsedEvent>();

            // Walks Create events to build a dispatcher tree (parent and children maps).
            // A parent edge is recorded only when the parent dispatcher itself has an observed
            // Create event. If the parent was created before profiler attach (or otherwise not
            // observed), the dispatcher is treated as a root: this prevents unrelated dispatchers
            // that share an unobserved ancestor (e.g., the test harness) from being merged into
            // one tree.
            private void EnsureDispatcherTree()
            {
                if (_parentOfDispatcher is not null)
                {
                    return;
                }

                _parentOfDispatcher = new Dictionary<ulong, ulong>();
                _childrenOfDispatcher = new Dictionary<ulong, List<ulong>>();
                _dispatcherKind = new Dictionary<ulong, DispatcherKind>();

                HashSet<ulong> dispatchersWithCreate = new HashSet<ulong>();
                foreach (var evt in _events)
                {
                    bool isCreate = evt.EventId is
                        AsyncEventID.TaskAsync_CreateAsyncContext or
                        AsyncEventID.RuntimeAsync_CreateAsyncContext or
                        AsyncEventID.RuntimeAsync_CreateAsyncCallstack;

                    if (isCreate && evt.DispatcherId != 0)
                    {
                        dispatchersWithCreate.Add(evt.DispatcherId);
                    }
                }

                foreach (var evt in _events)
                {
                    bool isCreate = evt.EventId is
                        AsyncEventID.TaskAsync_CreateAsyncContext or
                        AsyncEventID.RuntimeAsync_CreateAsyncContext or
                        AsyncEventID.RuntimeAsync_CreateAsyncCallstack;

                    if (!isCreate || evt.DispatcherId == 0)
                    {
                        continue;
                    }

                    DispatcherKind kind = evt.EventId switch
                    {
                        AsyncEventID.TaskAsync_CreateAsyncContext => DispatcherKind.V1,
                        AsyncEventID.RuntimeAsync_CreateAsyncContext or
                        AsyncEventID.RuntimeAsync_CreateAsyncCallstack => DispatcherKind.V2,
                        _ => DispatcherKind.Unknown,
                    };

                    if (kind != DispatcherKind.Unknown)
                    {
                        _dispatcherKind![evt.DispatcherId] = kind;
                    }

                    ulong parent = evt.ParentDispatcherId;
                    if (parent == 0 || !dispatchersWithCreate.Contains(parent))
                    {
                        // Parent not observed in this stream; treat this dispatcher as a root.
                        _parentOfDispatcher[evt.DispatcherId] = 0;
                        continue;
                    }

                    _parentOfDispatcher[evt.DispatcherId] = parent;

                    if (!_childrenOfDispatcher.TryGetValue(parent, out var kids))
                    {
                        kids = new List<ulong>();
                        _childrenOfDispatcher[parent] = kids;
                    }

                    if (!kids.Contains(evt.DispatcherId))
                    {
                        kids.Add(evt.DispatcherId);
                    }
                }
            }

            private DispatcherKind KindOf(ulong dispatcherId) =>
                _dispatcherKind is not null && _dispatcherKind.TryGetValue(dispatcherId, out var k)
                    ? k
                    : DispatcherKind.Unknown;

            // Returns the root DispatcherId for the chain containing dispatcherId, by walking parent pointers.
            // By default the climb stops at V1<->V2 boundaries so a V2 marker doesn't get pulled up into
            // an enclosing V1 test-runner dispatcher (and vice versa). Pass crossKinds: true to walk to
            // the absolute root regardless of dispatcher kind.
            public ulong RootOfDispatcher(ulong dispatcherId, bool crossKinds = false)
            {
                EnsureDispatcherTree();

                DispatcherKind startKind = KindOf(dispatcherId);
                ulong current = dispatcherId;
                while (_parentOfDispatcher!.TryGetValue(current, out ulong parent) && parent != 0)
                {
                    if (!crossKinds && startKind != DispatcherKind.Unknown && KindOf(parent) != startKind)
                    {
                        break;
                    }
                    current = parent;
                }

                return current;
            }

            // Returns the set of all dispatcher ids in the same chain (connected component) as dispatcherId.
            // Walks up to the root via parent pointers, then collects all descendants via BFS. By default
            // the walk is restricted to dispatchers of the same kind (V1 or V2) as dispatcherId so that a
            // single test's chain doesn't span unrelated cross-kind activity (e.g., a V1 xunit test runner
            // hosting a V2 test body). Pass crossKinds: true to walk the full connected component.
            public HashSet<ulong> ChainDispatcherIds(ulong dispatcherId, bool crossKinds = false)
            {
                EnsureDispatcherTree();

                DispatcherKind startKind = KindOf(dispatcherId);
                ulong root = RootOfDispatcher(dispatcherId, crossKinds);

                var chain = new HashSet<ulong> { root };
                var queue = new Queue<ulong>();
                queue.Enqueue(root);

                while (queue.Count > 0)
                {
                    ulong cur = queue.Dequeue();
                    if (_childrenOfDispatcher!.TryGetValue(cur, out var kids))
                    {
                        foreach (var kid in kids)
                        {
                            if (!crossKinds && startKind != DispatcherKind.Unknown && KindOf(kid) != startKind)
                            {
                                continue;
                            }

                            if (chain.Add(kid))
                            {
                                queue.Enqueue(kid);
                            }
                        }
                    }
                }

                return chain;
            }

            // Returns all events whose DispatcherId is in the same chain as dispatcherId, in timestamp order.
            // The chain is restricted to the same dispatcher kind by default (see ChainDispatcherIds).
            public List<ParsedEvent> ChainEventsFromDispatcher(ulong dispatcherId, bool crossKinds = false)
            {
                HashSet<ulong> chain = ChainDispatcherIds(dispatcherId, crossKinds);
                return _events.Where(e => chain.Contains(e.DispatcherId)).ToList();
            }

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
                var openByDispatcherId = new Dictionary<ulong, int>();

                foreach (var evt in _events)
                {
                    switch (evt.EventId)
                    {
                        case AsyncEventID.RuntimeAsync_SuspendAsyncContext:
                        case AsyncEventID.RuntimeAsync_CompleteAsyncContext:
                        case AsyncEventID.TaskAsync_SuspendAsyncContext:
                        case AsyncEventID.TaskAsync_CompleteAsyncContext:
                        {
                            openByDispatcherId.Remove(evt.DispatcherId);
                            break;
                        }
                        case AsyncEventID.RuntimeAsync_ResumeAsyncCallstack:
                        case AsyncEventID.TaskAsync_ResumeAsyncCallstack:
                        {
                            var merged = new ParsedEvent
                            {
                                EventId = evt.EventId,
                                Timestamp = evt.Timestamp,
                                OsThreadId = evt.OsThreadId,
                                ParentDispatcherId = evt.ParentDispatcherId,
                                DispatcherId = evt.DispatcherId,
                                CallstackType = evt.CallstackType,
                                ContinuationIndex = evt.ContinuationIndex,
                                FrameCount = evt.FrameCount,
                                Frames = new List<(ulong MethodId, int State)>(evt.Frames),
                            };

                            openByDispatcherId[evt.DispatcherId] = result.Count;
                            result.Add(merged);

                            break;
                        }
                        case AsyncEventID.TaskAsync_AppendAsyncCallstack:
                        {
                            if (openByDispatcherId.TryGetValue(evt.DispatcherId, out int idx))
                            {
                                ParsedEvent existing = result[idx];
                                var combinedFrames = new List<(ulong MethodId, int State)>(existing.Frames);
                                combinedFrames.AddRange(evt.Frames);

                                result[idx] = new ParsedEvent
                                {
                                    EventId = existing.EventId,
                                    Timestamp = existing.Timestamp,
                                    OsThreadId = existing.OsThreadId,
                                    ParentDispatcherId = existing.ParentDispatcherId,
                                    DispatcherId = existing.DispatcherId,
                                    CallstackType = existing.CallstackType,
                                    ContinuationIndex = existing.ContinuationIndex,
                                    FrameCount = (byte)Math.Min(combinedFrames.Count, byte.MaxValue),
                                    Frames = combinedFrames,
                                };
                            }

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
                ulong currentDispatcherId = 0;
                ulong currentParentDispatcherId = 0;
                var dispatcherStack = new Stack<(ulong DispatcherId, ulong ParentDispatcherId)>();
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

                    int payloadLength = ReadPayloadLengthPrefix(buffer, eventId, ref index);
                    int payloadStartIndex = index;

                    ParsedEvent evt = eventId switch
                    {
                        AsyncEventID.RuntimeAsync_CreateAsyncContext or AsyncEventID.TaskAsync_CreateAsyncContext =>
                            ParseCreateContextEvent(eventId, baseTimestamp, osThreadId, buffer, ref index),

                        AsyncEventID.RuntimeAsync_ResumeAsyncContext or AsyncEventID.TaskAsync_ResumeAsyncContext =>
                            ParseResumeContextEvent(eventId, baseTimestamp, osThreadId, buffer, ref index,
                                ref currentDispatcherId, ref currentParentDispatcherId, dispatcherStack),

                        AsyncEventID.RuntimeAsync_CompleteAsyncContext or AsyncEventID.TaskAsync_CompleteAsyncContext or
                        AsyncEventID.RuntimeAsync_SuspendAsyncContext or AsyncEventID.TaskAsync_SuspendAsyncContext =>
                            ParseEndContextEvent(eventId, baseTimestamp, osThreadId,
                                ref currentDispatcherId, ref currentParentDispatcherId, dispatcherStack),

                        AsyncEventID.RuntimeAsync_ResumeAsyncMethod or AsyncEventID.RuntimeAsync_CompleteAsyncMethod or
                        AsyncEventID.TaskAsync_ResumeAsyncMethod or AsyncEventID.TaskAsync_CompleteAsyncMethod =>
                            new ParsedEvent
                            {
                                EventId = eventId,
                                Timestamp = baseTimestamp,
                                OsThreadId = osThreadId,
                                ParentDispatcherId = currentParentDispatcherId,
                                DispatcherId = currentDispatcherId,
                            },

                        AsyncEventID.ResetAsyncThreadContext or AsyncEventID.ResetAsyncContinuationWrapperIndex =>
                            ParseResetEvent(eventId, baseTimestamp, osThreadId,
                                ref currentDispatcherId, ref currentParentDispatcherId, dispatcherStack),

                        AsyncEventID.RuntimeAsync_CreateAsyncCallstack or AsyncEventID.RuntimeAsync_ResumeAsyncCallstack or
                        AsyncEventID.RuntimeAsync_SuspendAsyncCallstack or
                        AsyncEventID.TaskAsync_ResumeAsyncCallstack or AsyncEventID.TaskAsync_AppendAsyncCallstack =>
                            ParseCallstackEvent(eventId, baseTimestamp, osThreadId, buffer, ref index),

                        AsyncEventID.RuntimeAsync_UnwindAsyncException or AsyncEventID.TaskAsync_UnwindAsyncException =>
                            ParseUnwindEvent(eventId, baseTimestamp, osThreadId, currentDispatcherId, currentParentDispatcherId, buffer, ref index),

                        AsyncEventID.AsyncProfilerMetadata =>
                            ParseMetadataEvent(baseTimestamp, osThreadId, currentDispatcherId, currentParentDispatcherId, buffer, ref index),

                        AsyncEventID.AsyncProfilerSyncClock =>
                            ParseSyncClockEvent(baseTimestamp, osThreadId, currentDispatcherId, currentParentDispatcherId, buffer, ref index),

                        _ => ParseUnknownEvent(eventId, baseTimestamp, osThreadId, currentDispatcherId, currentParentDispatcherId, buffer, ref index)
                    };

                    Assert.Equal(payloadLength, index - payloadStartIndex);

                    allEvents.Add(evt);
                }
            });

            return new ParsedEventStream(allEvents);

            static ParsedEvent ParseCreateContextEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                ulong parentDispatcherId = ReadCompressedUInt64(buffer, ref index);
                ulong dispatcherId = ReadCompressedUInt64(buffer, ref index);
                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = parentDispatcherId,
                    DispatcherId = dispatcherId,
                };
            }

            static ParsedEvent ParseResumeContextEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ReadOnlySpan<byte> buffer, ref int index,
                ref ulong currentDispatcherId, ref ulong currentParentDispatcherId,
                Stack<(ulong DispatcherId, ulong ParentDispatcherId)> dispatcherStack)
            {
                ulong dispatcherId = ReadCompressedUInt64(buffer, ref index);

                dispatcherStack.Push((currentDispatcherId, currentParentDispatcherId));
                currentDispatcherId = dispatcherId;
                currentParentDispatcherId = 0;

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = 0,
                    DispatcherId = dispatcherId,
                };
            }

            static ParsedEvent ParseEndContextEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ref ulong currentDispatcherId, ref ulong currentParentDispatcherId,
                Stack<(ulong DispatcherId, ulong ParentDispatcherId)> dispatcherStack)
            {
                ulong endingDispatcherId = currentDispatcherId;
                ulong endingParentDispatcherId = currentParentDispatcherId;

                if (dispatcherStack.Count > 0)
                {
                    var top = dispatcherStack.Pop();
                    currentDispatcherId = top.DispatcherId;
                    currentParentDispatcherId = top.ParentDispatcherId;
                }
                else
                {
                    currentDispatcherId = 0;
                    currentParentDispatcherId = 0;
                }

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = endingParentDispatcherId,
                    DispatcherId = endingDispatcherId,
                };
            }

            static ParsedEvent ParseResetEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ref ulong currentDispatcherId, ref ulong currentParentDispatcherId,
                Stack<(ulong DispatcherId, ulong ParentDispatcherId)> dispatcherStack)
            {
                ulong prevDispatcherId = currentDispatcherId;
                ulong prevParentDispatcherId = currentParentDispatcherId;
                if (eventId == AsyncEventID.ResetAsyncThreadContext)
                {
                    dispatcherStack.Clear();
                    currentDispatcherId = 0;
                    currentParentDispatcherId = 0;
                }

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = prevParentDispatcherId,
                    DispatcherId = prevDispatcherId,
                };
            }

            static ParsedEvent ParseCallstackEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                AsyncCallstackType callstackType = CallstackTypeFromEventId(eventId);
                ReadCallstackPayload(buffer, ref index, eventId, callstackType, out ulong parentDispatcherId, out ulong dispatcherId, out byte continuationIndex, out byte frameCount, out var frames);

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = parentDispatcherId,
                    DispatcherId = dispatcherId,
                    CallstackType = callstackType,
                    ContinuationIndex = continuationIndex,
                    FrameCount = frameCount,
                    Frames = frames
                };
            }

            static ParsedEvent ParseUnwindEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ulong currentDispatcherId, ulong currentParentDispatcherId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                uint unwindCount = ReadCompressedUInt32(buffer, ref index);

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = currentParentDispatcherId,
                    DispatcherId = currentDispatcherId,
                    UnwindFrameCount = unwindCount
                };
            }

            static ParsedEvent ParseMetadataEvent(long timestamp, ulong osThreadId,
                ulong currentDispatcherId, ulong currentParentDispatcherId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                ReadMetadataPayload(buffer, ref index, out ulong freq, out ulong qpcSync, out ulong utcSync, out uint bufSize, out byte wrapperCount);

                return new ParsedEvent
                {
                    EventId = AsyncEventID.AsyncProfilerMetadata,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = currentParentDispatcherId,
                    DispatcherId = currentDispatcherId,
                    Metadata = new MetadataFromBuffer(freq, qpcSync, utcSync, bufSize, wrapperCount)
                };
            }

            static ParsedEvent ParseSyncClockEvent(long timestamp, ulong osThreadId,
                ulong currentDispatcherId, ulong currentParentDispatcherId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                ulong qpcSync = ReadCompressedUInt64(buffer, ref index);
                ulong utcSync = ReadCompressedUInt64(buffer, ref index);

                return new ParsedEvent
                {
                    EventId = AsyncEventID.AsyncProfilerSyncClock,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = currentParentDispatcherId,
                    DispatcherId = currentDispatcherId,
                    SyncClockQpc = qpcSync,
                    SyncClockUtc = utcSync
                };
            }

            static ParsedEvent ParseUnknownEvent(AsyncEventID eventId, long timestamp, ulong osThreadId,
                ulong currentDispatcherId, ulong currentParentDispatcherId,
                ReadOnlySpan<byte> buffer, ref int index)
            {
                SkipEventPayload(eventId, buffer, ref index);

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = currentParentDispatcherId,
                    DispatcherId = currentDispatcherId,
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

        private static Task<CollectedEvents> CollectValueTaskEventsAsync(EventKeywords keywords, Func<ValueTask> scenario)
            => CollectEventsAsync(keywords, () => scenario().AsTask());

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
        // Handles both V1 (TaskAsync_*) and V2 (RuntimeAsync) event ids.
        private static void AssertCallstackSimulationReachesZero(ParsedEventStream stream, string markerMethodName)
        {
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.RuntimeAsync_ResumeAsyncCallstack, markerMethodName);
            if (resumeStacks.Count == 0)
            {
                resumeStacks = stream.CallstacksWithMarker(AsyncEventID.TaskAsync_ResumeAsyncCallstack, markerMethodName);
            }
            Assert.True(resumeStacks.Count >= 1, $"Expected at least one resume callstack with marker '{markerMethodName}'");

            ulong dispatcherId = resumeStacks[0].DispatcherId;

            var sequence = stream.ChainEventsFromDispatcher(dispatcherId);
            int stackDepth = 0;

            foreach (var evt in sequence)
            {
                switch (evt.EventId)
                {
                    case AsyncEventID.RuntimeAsync_ResumeAsyncCallstack:
                    case AsyncEventID.TaskAsync_ResumeAsyncCallstack:
                    {
                        stackDepth = (int)evt.FrameCount;
                        break;
                    }
                    case AsyncEventID.RuntimeAsync_CompleteAsyncMethod:
                    case AsyncEventID.TaskAsync_CompleteAsyncMethod:
                    {
                        if (stackDepth > 0)
                        {
                            stackDepth--;
                        }

                        break;
                    }
                    case AsyncEventID.RuntimeAsync_UnwindAsyncException:
                    case AsyncEventID.TaskAsync_UnwindAsyncException:
                    {
                        stackDepth = Math.Max(0, stackDepth - (int)evt.UnwindFrameCount);
                        break;
                    }
                }
            }

            Assert.Equal(0, stackDepth);
        }

        private static void AssertExactlyOneCreateAndComplete(ParsedEventStream stream, ulong dispatcherId, string chainName)
        {
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();
            int creates = ids.Count(id => id is AsyncEventID.RuntimeAsync_CreateAsyncContext or AsyncEventID.TaskAsync_CreateAsyncContext);
            int completes = ids.Count(id => id is AsyncEventID.RuntimeAsync_CompleteAsyncContext or AsyncEventID.TaskAsync_CompleteAsyncContext);
            Assert.True(creates == 1, $"Expected exactly 1 CreateAsyncContext for {chainName} (DispatcherId {dispatcherId}), got {creates}");
            Assert.True(completes == 1, $"Expected exactly 1 CompleteAsyncContext for {chainName} (DispatcherId {dispatcherId}), got {completes}");
        }

        // V1-friendly variant: V1's per-MoveNext dispatcher model emits one Create per await
        // suspension, so a method with N awaits produces N dispatchers / N Creates within the
        // same dispatcher tree. The invariant we can still assert is creates == completes (both >= 1).
        private static void AssertCreateEqualsCompleteInChain(ParsedEventStream stream, ulong dispatcherId, string chainName)
        {
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();
            int creates = ids.Count(id => id == AsyncEventID.TaskAsync_CreateAsyncContext);
            int completes = ids.Count(id => id == AsyncEventID.TaskAsync_CompleteAsyncContext);
            Assert.True(creates >= 1, $"Expected at least 1 TaskAsync_CreateAsyncContext for {chainName} (DispatcherId {dispatcherId}), got {creates}");
            Assert.True(creates == completes, $"Expected TaskAsync_CreateAsyncContext count == TaskAsync_CompleteAsyncContext count for {chainName} (DispatcherId {dispatcherId}), got {creates} creates and {completes} completes");
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
                Deserializer.ReadUInt32(buffer, ref index, out uint asyncThreadContextId);
                Deserializer.ReadUInt64(buffer, ref index, out ulong osThreadId);
                Deserializer.ReadUInt32(buffer, ref index, out uint totalEventCount);
                Deserializer.ReadUInt64(buffer, ref index, out ulong startTimestamp);
                Deserializer.ReadUInt64(buffer, ref index, out ulong endTimestamp);

                Console.WriteLine($"TotalSize: {totalSize}");
                Console.WriteLine($"AsyncThreadContextId: {asyncThreadContextId}");
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

                    _ = ReadPayloadLengthPrefix(buffer, eventId, ref index);
                    int payloadStart = index;
                    try
                    {
                        index += eventId switch
                        {
                            AsyncEventID.RuntimeAsync_CreateAsyncContext => OutputCreateAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.RuntimeAsync_ResumeAsyncContext => OutputResumeAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.RuntimeAsync_SuspendAsyncContext => OutputSuspendAsyncContextEvent(),
                            AsyncEventID.RuntimeAsync_CompleteAsyncContext => OutputCompleteAsyncContextEvent(),
                            AsyncEventID.RuntimeAsync_UnwindAsyncException => OutputUnwindAsyncExceptionEvent(buffer.Slice(index)),
                            AsyncEventID.RuntimeAsync_CreateAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.RuntimeAsync_ResumeAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.RuntimeAsync_SuspendAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.RuntimeAsync_ResumeAsyncMethod => OutputResumeAsyncMethodEvent(),
                            AsyncEventID.RuntimeAsync_CompleteAsyncMethod => OutputCompleteAsyncMethodEvent(),

                            AsyncEventID.TaskAsync_CreateAsyncContext => OutputCreateAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.TaskAsync_ResumeAsyncContext => OutputResumeAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.TaskAsync_SuspendAsyncContext => OutputSuspendAsyncContextEvent(),
                            AsyncEventID.TaskAsync_CompleteAsyncContext => OutputCompleteAsyncContextEvent(),
                            AsyncEventID.TaskAsync_UnwindAsyncException => OutputUnwindAsyncExceptionEvent(buffer.Slice(index)),
                            AsyncEventID.TaskAsync_ResumeAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.TaskAsync_ResumeAsyncMethod => OutputResumeAsyncMethodEvent(),
                            AsyncEventID.TaskAsync_CompleteAsyncMethod => OutputCompleteAsyncMethodEvent(),
                            AsyncEventID.TaskAsync_AppendAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),

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
                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong parentDispatcherId);
                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong dispatcherId);
                Console.WriteLine($"  ParentDispatcherId: {parentDispatcherId}");
                Console.WriteLine($"  DispatcherId: {dispatcherId}");
                return index;
            }

            private static int OutputResumeAsyncContextEvent(ReadOnlySpan<byte> buffer)
            {
                int index = 0;
                Deserializer.ReadCompressedUInt64(buffer, ref index, out ulong dispatcherId);
                Console.WriteLine($"  DispatcherId: {dispatcherId}");
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

                byte manifestCount = buffer[index++];
                Console.WriteLine($"  EventManifestCount: {manifestCount}");
                for (int i = 0; i < manifestCount; i++)
                {
                    byte id = buffer[index++];
                    byte version = buffer[index++];
                    byte payloadLengthFieldSize = buffer[index++];
                    Console.WriteLine($"    [{(AsyncEventID)id}] version={version} payloadLengthFieldSize={payloadLengthFieldSize}");
                }

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

            private static int OutputAsyncCallstackEvent(AsyncEventID eventId, ReadOnlySpan<byte> buffer)
            {
                ulong parentDispatcherId = 0;
                ulong dispatcherId;
                byte callstackId;
                byte continuationIndex;
                byte asyncCallstackLength;
                int index = 0;

                AsyncCallstackType type = (eventId is AsyncEventID.TaskAsync_ResumeAsyncCallstack
                                                   or AsyncEventID.TaskAsync_AppendAsyncCallstack)
                    ? AsyncCallstackType.Compiler
                    : AsyncCallstackType.Runtime;
                callstackId = buffer[index++];
                continuationIndex = buffer[index++];
                asyncCallstackLength = buffer[index++];
                if (eventId == AsyncEventID.RuntimeAsync_CreateAsyncCallstack)
                {
                    Deserializer.ReadCompressedUInt64(buffer, ref index, out parentDispatcherId);
                }
                Deserializer.ReadCompressedUInt64(buffer, ref index, out dispatcherId);

                if (eventId == AsyncEventID.RuntimeAsync_CreateAsyncCallstack)
                {
                    Console.WriteLine($"  ParentDispatcherId: {parentDispatcherId}");
                }
                Console.WriteLine($"  DispatcherId: {dispatcherId}");
                Console.WriteLine($"  CallstackId: {callstackId}");
                Console.WriteLine($"  ContinuationIndex: {continuationIndex}");
                Console.WriteLine($"  Length: {asyncCallstackLength}");

                if (asyncCallstackLength == 0)
                {
                    return index;
                }

                ulong previousMethodId;
                ulong currentMethodId;
                int state = 0;

                bool readState = type == AsyncCallstackType.Compiler;

                Deserializer.ReadCompressedUInt64(buffer, ref index, out currentMethodId);
                if (readState)
                {
                    Deserializer.ReadCompressedInt32(buffer, ref index, out state);
                }

                OutputAsyncFrame(type, currentMethodId, state, 0);

                for (int i = 1; i < asyncCallstackLength; i++)
                {
                    previousMethodId = currentMethodId;
                    Deserializer.ReadCompressedInt64(buffer, ref index, out long methodIdDelta);
                    if (readState)
                    {
                        Deserializer.ReadCompressedInt32(buffer, ref index, out state);
                    }
                    currentMethodId = previousMethodId + (ulong)methodIdDelta;
                    OutputAsyncFrame(type, currentMethodId, state, i);
                }

                return index;
            }

            private static void OutputAsyncFrame(AsyncCallstackType type, ulong methodId, int state, int frameIndex)
            {
                string asyncMethodName = GetMethodNameFromMethodId(type, methodId) ?? "??";
                if (type == AsyncCallstackType.Compiler)
                {
                    Console.WriteLine($"    [{frameIndex}] {asyncMethodName} (0x{methodId:X}) (state={state})");
                }
                else
                {
                    Console.WriteLine($"    [{frameIndex}] {asyncMethodName} (0x{methodId:X})");
                }
            }
        }
    }
}
