// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Linq;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    // Mirrors AsyncProfiler.AsyncEventID from the runtime (which is internal and inaccessible from tests).
    public enum AsyncEventID : byte
    {
        // Runtime (RuntimeAsync) events.
        CreateRuntimeAsyncContext = 1,
        ResumeRuntimeAsyncContext = 2,
        SuspendRuntimeAsyncContext = 3,
        CompleteRuntimeAsyncContext = 4,
        UnwindRuntimeAsyncException = 5,
        CreateRuntimeAsyncCallstack = 6,
        ResumeRuntimeAsyncCallstack = 7,
        SuspendRuntimeAsyncCallstack = 8,
        ResumeRuntimeAsyncMethod = 9,
        CompleteRuntimeAsyncMethod = 10,

        // StateMachine (StateMachineAsync) events.
        CreateStateMachineAsyncContext = 11,
        ResumeStateMachineAsyncContext = 12,
        SuspendStateMachineAsyncContext = 13,
        CompleteStateMachineAsyncContext = 14,
        UnwindStateMachineAsyncException = 15,
        ResumeStateMachineAsyncCallstack = 16,
        ResumeStateMachineAsyncMethod = 17,
        CompleteStateMachineAsyncMethod = 18,
        AppendStateMachineAsyncCallstack = 19,

        // Neutral profiler events.
        ResetAsyncThreadContext = 20,
        ResetAsyncContinuationWrapperIndex = 21,
        AsyncProfilerMetadata = 22,
        AsyncProfilerSyncClock = 23,
    }

    public enum AsyncCallstackType : byte
    {
        StateMachine = 0x1,
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
                new EventManifestEntry(AsyncEventID.CreateRuntimeAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.ResumeRuntimeAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.SuspendRuntimeAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.CompleteRuntimeAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.UnwindRuntimeAsyncException, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.CreateRuntimeAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.ResumeRuntimeAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.SuspendRuntimeAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.ResumeRuntimeAsyncMethod, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.CompleteRuntimeAsyncMethod, 1, NoPayload),

                new EventManifestEntry(AsyncEventID.CreateStateMachineAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.ResumeStateMachineAsyncContext, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.SuspendStateMachineAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.CompleteStateMachineAsyncContext, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.UnwindStateMachineAsyncException, 1, BytePayloadLength),
                new EventManifestEntry(AsyncEventID.ResumeStateMachineAsyncCallstack, 1, UShortPayloadLength),
                new EventManifestEntry(AsyncEventID.ResumeStateMachineAsyncMethod, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.CompleteStateMachineAsyncMethod, 1, NoPayload),
                new EventManifestEntry(AsyncEventID.AppendStateMachineAsyncCallstack, 1, UShortPayloadLength),

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

        // StateMachine (StateMachineAsync_*) async-task instrumentation is the classic compiler-generated
        // state machine (V1). It does not require runtime-async (V2), so it is supported on every runtime
        // (CoreCLR and Mono) except NativeAOT, where it is opt-out to avoid ~100KB of per-state-machine
        // generic instantiation overhead. Tests that depend on StateMachine events must be gated on these
        // properties so they are skipped on NAOT.
        public static bool IsStateMachineAsyncSupported =>
            !PlatformDetection.IsNativeAot;

        public static bool IsStateMachineAsyncAndThreadingSupported =>
            IsStateMachineAsyncSupported && PlatformDetection.IsMultithreadingSupported;

        // Gate for tests that exercise a mixed V1 (StateMachine) + V2 (RuntimeAsync) chain: they need both
        // instrumentation paths, and V1 is disabled on NativeAOT, so require both conditions.
        public static bool IsStateMachineAsyncAndRuntimeAsyncAndThreadingSupported =>
            IsStateMachineAsyncAndThreadingSupported && IsRuntimeAsyncAndThreadingSupported;

        // Alias so tests that additionally require CoreCLR can list the exclusion as an extra ConditionalFact
        // condition alongside the shared gates.
        public static bool IsNotMonoRuntime => PlatformDetection.IsNotMonoRuntime;

        private const string AsyncProfilerEventSourceName = "System.Runtime.CompilerServices.AsyncProfilerEventSource";
        private const string WrapperNameTemplate = "Continuation_Wrapper_{0}";
        private static readonly string WrapperNamePrefix = WrapperNameTemplate.Substring(0, WrapperNameTemplate.IndexOf("{0}", StringComparison.Ordinal));

        private const int AsyncEventsId = 1;
        private const int HeaderSize = 1 + sizeof(uint) + sizeof(uint) + sizeof(ulong) + sizeof(uint) + sizeof(ulong) + sizeof(ulong);

        // AsyncProfilerEventSource Keywords matching the event source definition
        private const EventKeywords CreateRuntimeAsyncContextKeyword = (EventKeywords)0x1;
        private const EventKeywords ResumeRuntimeAsyncContextKeyword = (EventKeywords)0x2;
        private const EventKeywords SuspendRuntimeAsyncContextKeyword = (EventKeywords)0x4;
        private const EventKeywords CompleteRuntimeAsyncContextKeyword = (EventKeywords)0x8;
        private const EventKeywords UnwindRuntimeAsyncExceptionKeyword = (EventKeywords)0x10;
        private const EventKeywords CreateRuntimeAsyncCallstackKeyword = (EventKeywords)0x20;
        private const EventKeywords ResumeRuntimeAsyncCallstackKeyword = (EventKeywords)0x40;
        private const EventKeywords SuspendRuntimeAsyncCallstackKeyword = (EventKeywords)0x80;
        private const EventKeywords ResumeRuntimeAsyncMethodKeyword = (EventKeywords)0x100;
        private const EventKeywords CompleteRuntimeAsyncMethodKeyword = (EventKeywords)0x200;
        private const EventKeywords CreateStateMachineAsyncContextKeyword = (EventKeywords)0x400;
        private const EventKeywords ResumeStateMachineAsyncContextKeyword = (EventKeywords)0x800;
        private const EventKeywords SuspendStateMachineAsyncContextKeyword = (EventKeywords)0x1000;
        private const EventKeywords CompleteStateMachineAsyncContextKeyword = (EventKeywords)0x2000;
        private const EventKeywords UnwindStateMachineAsyncExceptionKeyword = (EventKeywords)0x4000;
        private const EventKeywords ResumeStateMachineAsyncCallstackKeyword = (EventKeywords)0x8000;
        private const EventKeywords ResumeStateMachineAsyncMethodKeyword = (EventKeywords)0x10000;
        private const EventKeywords CompleteStateMachineAsyncMethodKeyword = (EventKeywords)0x20000;

        private const EventKeywords AllRuntimeAsyncKeywords =
            CreateRuntimeAsyncContextKeyword |
            ResumeRuntimeAsyncContextKeyword |
            SuspendRuntimeAsyncContextKeyword |
            CompleteRuntimeAsyncContextKeyword |
            UnwindRuntimeAsyncExceptionKeyword |
            CreateRuntimeAsyncCallstackKeyword |
            ResumeRuntimeAsyncCallstackKeyword |
            SuspendRuntimeAsyncCallstackKeyword |
            ResumeRuntimeAsyncMethodKeyword |
            CompleteRuntimeAsyncMethodKeyword;

        private const EventKeywords AllStateMachineAsyncKeywords =
            CreateStateMachineAsyncContextKeyword |
            ResumeStateMachineAsyncContextKeyword |
            SuspendStateMachineAsyncContextKeyword |
            CompleteStateMachineAsyncContextKeyword |
            UnwindStateMachineAsyncExceptionKeyword |
            ResumeStateMachineAsyncCallstackKeyword |
            ResumeStateMachineAsyncMethodKeyword |
            CompleteStateMachineAsyncMethodKeyword;

        private const EventKeywords AllKeywords =
            AllRuntimeAsyncKeywords |
            AllStateMachineAsyncKeywords;

        private const EventKeywords RuntimeAsyncCoreKeywords =
            CreateRuntimeAsyncContextKeyword |
            ResumeRuntimeAsyncContextKeyword |
            SuspendRuntimeAsyncContextKeyword |
            CompleteRuntimeAsyncContextKeyword;

        private const EventKeywords StateMachineAsyncCoreKeywords =
            CreateStateMachineAsyncContextKeyword |
            ResumeStateMachineAsyncContextKeyword |
            SuspendStateMachineAsyncContextKeyword |
            CompleteStateMachineAsyncContextKeyword;

        private const EventKeywords RuntimeAsyncMethodKeywords =
            ResumeRuntimeAsyncMethodKeyword |
            CompleteRuntimeAsyncMethodKeyword;

        private const EventKeywords StateMachineAsyncMethodKeywords =
            ResumeStateMachineAsyncMethodKeyword |
            CompleteStateMachineAsyncMethodKeyword;

        private const EventKeywords RuntimeAsyncCallstackKeywords =
            CreateRuntimeAsyncContextKeyword |
            CreateRuntimeAsyncCallstackKeyword |
            ResumeRuntimeAsyncContextKeyword |
            ResumeRuntimeAsyncCallstackKeyword |
            SuspendRuntimeAsyncContextKeyword |
            SuspendRuntimeAsyncCallstackKeyword |
            CompleteRuntimeAsyncContextKeyword |
            CompleteRuntimeAsyncMethodKeyword |
            UnwindRuntimeAsyncExceptionKeyword;

        private const EventKeywords StateMachineAsyncCallstackKeywords =
            CreateStateMachineAsyncContextKeyword |
            ResumeStateMachineAsyncContextKeyword |
            SuspendStateMachineAsyncContextKeyword |
            ResumeStateMachineAsyncCallstackKeyword |
            CompleteStateMachineAsyncContextKeyword |
            CompleteStateMachineAsyncMethodKeyword |
            UnwindStateMachineAsyncExceptionKeyword;

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
                if (s_getMethodFromNativeIPMethod is not null)
                {
                    MethodBase? method = (MethodBase?)s_getMethodFromNativeIPMethod.Invoke(null, new object[] { (IntPtr)methodId });
                    if (callstackType == AsyncCallstackType.Runtime)
                    {
                        return method?.Name;
                    }
                    else
                    {
                        return ExtractStateMachineMethodName(method?.DeclaringType?.Name);
                    }
                }

                if (s_stackFrameFromIPCtor is not null)
                {
                    StackFrame frame = (StackFrame)s_stackFrameFromIPCtor.Invoke(new object[] { (IntPtr)methodId, false })!;
                    DiagnosticMethodInfo? diagInfo = DiagnosticMethodInfo.Create(frame);
                    if (callstackType == AsyncCallstackType.Runtime)
                    {
                        return diagInfo?.Name;
                    }
                    else
                    {
                        return ExtractStateMachineMethodName(diagInfo?.DeclaringTypeName);
                    }
                }

                // Mono fallback (no managed IP->method API): resolve via the reverse method-id map.
                return ResolveStateMachineMethodNameFromId(methodId);
            }

            return null;
        }

        // Mono has no managed IP->method API, so the reflective resolvers above return null there and a
        // frame's method id (a native code IP) can't be mapped back to a name. As a fallback we build a
        // reverse map from method id -> async method name by scanning every compiler-generated async state
        // machine reachable from the test class and computing the SAME id the runtime emits for a
        // StateMachine frame, i.e. the native code of MoveNext (see AsyncStateMachineDiagnostics<T>.
        // ResolveMethodId). This covers frame name resolution and the console dump uniformly on Mono.
        //
        // The IntPtr overload of GetNativeCodeInternal only exists on Mono (CoreCLR's takes an
        // IRuntimeMethodInfo), so this reflection yields null on CoreCLR/NativeAOT and the fallback is a
        // no-op there, leaving the robust IP->name resolution untouched.
        private static readonly MethodInfo? s_getNativeCodeInternalMethod =
            typeof(RuntimeMethodHandle).GetMethod(
                "GetNativeCodeInternal",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(IntPtr) },
                null);

        // Set of (resolved name, state machine MoveNext handle) for every async state machine reachable
        // from the test class, built once. This includes not just async methods declared on the test class
        // but also async lambdas and local functions, whose compiler-generated state machines are nested
        // types (under the test class or its display classes) implementing IAsyncStateMachine. Any of these
        // can appear as a continuation frame, so all must be covered.
        private static readonly Lazy<(string Name, IntPtr MoveNextHandle)[]> s_stateMachineMoveNextMethods =
            new(BuildStateMachineMoveNextMethods);

        // Resolved map: state machine frame method id (native code IP of MoveNext) -> async method name.
        // Filled lazily on the first resolve miss, and eagerly by tests (SnapshotStateMachineMethodIdFor) that
        // need to capture a method's tier-0 id before re-tiering; additive and keyed by address.
        private static readonly ConcurrentDictionary<ulong, string> s_methodIdToName = new();

        private static (string Name, IntPtr MoveNextHandle)[] BuildStateMachineMoveNextMethods()
        {
            var result = new List<(string, IntPtr)>();
            CollectStateMachineMoveNextMethods(typeof(AsyncProfilerTests), result);
            return result.ToArray();
        }

        // Recursively walks nested types looking for compiler-generated async state machines (types that
        // implement IAsyncStateMachine) and records their MoveNext, named the same way IP->name resolution
        // does on other runtimes (ExtractStateMachineMethodName over the state machine type name) so the
        // fallback is indistinguishable from the primary resolver.
        private static void CollectStateMachineMoveNextMethods(Type type, List<(string, IntPtr)> result)
        {
            foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!nested.ContainsGenericParameters
                    && typeof(System.Runtime.CompilerServices.IAsyncStateMachine).IsAssignableFrom(nested))
                {
                    MethodInfo? moveNext = nested.GetMethod(
                        "MoveNext",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (moveNext is not null)
                    {
                        result.Add((ExtractStateMachineMethodName(nested.Name) ?? nested.Name, moveNext.MethodHandle.Value));
                    }
                }

                CollectStateMachineMoveNextMethods(nested, result);
            }
        }

        // Records the CURRENT native code start of every known state machine MoveNext into the id->name map.
        // Used by the resolve path, which only has a raw frame address and so must consider every method.
        // A MoveNext has no native code start until it has actually run, and a resume frame's method id is the
        // code start of whatever version was current when the runtime first froze that id. The map is additive
        // and keyed by address. No-op except on Mono.
        private static void SnapshotStateMachineMethodIds()
        {
            if (s_getNativeCodeInternalMethod is null)
            {
                return;
            }

            foreach ((string methodName, IntPtr moveNextHandle) in s_stateMachineMoveNextMethods.Value)
            {
                object? nativeCode = s_getNativeCodeInternalMethod.Invoke(null, new object[] { moveNextHandle });
                if (nativeCode is IntPtr ip && ip != IntPtr.Zero)
                {
                    s_methodIdToName.TryAdd((ulong)(nuint)ip, methodName);
                }
            }
        }

        // Records the CURRENT native code start of a single async method's compiler-generated state machine
        // MoveNext (found via its [AsyncStateMachine] attribute) into the id->name map, keyed to the async
        // method's name so it matches normal resolution. On Mono the interpreter re-tiers a method after
        // enough calls, replacing its code start, while the resume frames keep the initial (tier-0) id frozen
        // at first run; a test that calls a method enough to trigger re-tiering can snapshot its id up front,
        // while the method is still at its tier-0 version, so the frozen id stays resolvable afterwards.
        // No-op except on Mono.
        private static void SnapshotStateMachineMethodIdFor(MethodInfo asyncMethod)
        {
            if (s_getNativeCodeInternalMethod is null)
            {
                return;
            }

            Type? stateMachineType = asyncMethod
                .GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>()?.StateMachineType;
            MethodInfo? moveNext = stateMachineType?.GetMethod(
                "MoveNext",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (moveNext is null)
            {
                return;
            }

            object? nativeCode = s_getNativeCodeInternalMethod.Invoke(null, new object[] { moveNext.MethodHandle.Value });
            if (nativeCode is IntPtr ip && ip != IntPtr.Zero)
            {
                s_methodIdToName.TryAdd((ulong)(nuint)ip, asyncMethod.Name);
            }
        }

        // Mono fallback: resolve a StateMachine frame's method id to the async method name via the reverse
        // map. On a miss we snapshot the current native code starts and retry, filling the map lazily as
        // methods run (a MoveNext has no code start until it has first run). No-op except on Mono.
        private static string? ResolveStateMachineMethodNameFromId(ulong methodId)
        {
            if (s_getNativeCodeInternalMethod is null || methodId == 0)
            {
                return null;
            }

            if (s_methodIdToName.TryGetValue(methodId, out string? name))
            {
                return name;
            }

            SnapshotStateMachineMethodIds();

            return s_methodIdToName.TryGetValue(methodId, out name) ? name : null;
        }

        // The compiler generates a state machine type named "<AsyncMethodName>d__N" (possibly nested
        // and namespace-qualified). Extract the original async method name from between the angle
        // brackets. Returns the input unchanged when it contains no angle brackets, or null when null.
        private static string? ExtractStateMachineMethodName(string? declaringTypeName)
        {
            if (declaringTypeName is null)
            {
                return null;
            }

            int start = declaringTypeName.IndexOf('<');
            int end = declaringTypeName.IndexOf('>');

            start++;
            if (start > 0 && end > start)
            {
                return declaringTypeName.Substring(start, end - start);
            }

            return declaringTypeName;
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
                case AsyncEventID.CreateRuntimeAsyncContext:
                case AsyncEventID.CreateStateMachineAsyncContext:
                {
                    ReadCompressedUInt64(buffer, ref index); // parentDispatcherId
                    ReadCompressedUInt64(buffer, ref index); // dispatcherId
                    return true;
                }
                case AsyncEventID.ResumeRuntimeAsyncContext:
                case AsyncEventID.ResumeStateMachineAsyncContext:
                {
                    ReadCompressedUInt64(buffer, ref index); // dispatcherId
                    return true;
                }
                case AsyncEventID.SuspendRuntimeAsyncContext:
                case AsyncEventID.CompleteRuntimeAsyncContext:
                case AsyncEventID.ResumeRuntimeAsyncMethod:
                case AsyncEventID.CompleteRuntimeAsyncMethod:
                case AsyncEventID.SuspendStateMachineAsyncContext:
                case AsyncEventID.CompleteStateMachineAsyncContext:
                case AsyncEventID.ResumeStateMachineAsyncMethod:
                case AsyncEventID.CompleteStateMachineAsyncMethod:
                case AsyncEventID.ResetAsyncThreadContext:
                case AsyncEventID.ResetAsyncContinuationWrapperIndex:
                {
                    return true;
                }
                case AsyncEventID.UnwindRuntimeAsyncException:
                case AsyncEventID.UnwindStateMachineAsyncException:
                {
                    ReadCompressedUInt32(buffer, ref index);
                    return true;
                }
                case AsyncEventID.CreateRuntimeAsyncCallstack:
                case AsyncEventID.ResumeRuntimeAsyncCallstack:
                case AsyncEventID.SuspendRuntimeAsyncCallstack:
                case AsyncEventID.ResumeStateMachineAsyncCallstack:
                case AsyncEventID.AppendStateMachineAsyncCallstack:
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
            // parentDispatcherId is only present on CreateRuntimeAsyncCallstack.
            parentDispatcherId = eventId == AsyncEventID.CreateRuntimeAsyncCallstack
                ? ReadCompressedUInt64(buffer, ref index)
                : 0;
            dispatcherId = ReadCompressedUInt64(buffer, ref index);
            frames = new List<(ulong, int)>(frameCount);

            if (frameCount == 0)
            {
                // Cached callstack reference (frame data resolved out-of-band by callstack id).
                return;
            }

            bool readState = callstackType == AsyncCallstackType.StateMachine;

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
        // StateMachine (StateMachineAsync*) events carry compiler-built state machine frames;
        // Runtime (RuntimeAsync_*) events carry runtime-async frames.
        private static AsyncCallstackType CallstackTypeFromEventId(AsyncEventID eventId)
            => eventId is AsyncEventID.ResumeStateMachineAsyncCallstack
                       or AsyncEventID.AppendStateMachineAsyncCallstack
                ? AsyncCallstackType.StateMachine
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

            // StateMachine (StateMachineAsync_*) and Runtime (RuntimeAsync_*) dispatchers can coexist on the same logical
            // thread (e.g., a StateMachine test runner hosting a Runtime test body). The runtime captures cross-kind
            // parent links in that case, but a single test typically wants to walk only its own kind's
            // subtree. Tracking the kind per dispatcher lets the chain walk stop at StateMachine<->Runtime boundaries
            // by default.
            internal enum DispatcherKind { Unknown, StateMachine, Runtime }

            public ParsedEventStream(List<ParsedEvent> events)
            {
                // Stable sort by timestamp: events that share a Stopwatch tick keep their original
                // parse (emission) order, which preserves each thread's relative event order. The
                // input list is built in emission order by the parser. List<T>.Sort is an UNSTABLE
                // sort and would scramble same-timestamp events, breaking the ordering-sensitive
                // assertions (e.g. Suspend-before-Complete, Create-before-Resume) on platforms where
                // many events share a timestamp (e.g. single-threaded WASM). Enumerable.OrderBy is a
                // documented stable sort.
                _events = events.OrderBy(e => e.Timestamp).ToList();
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
                        AsyncEventID.CreateStateMachineAsyncContext or
                        AsyncEventID.CreateRuntimeAsyncContext or
                        AsyncEventID.CreateRuntimeAsyncCallstack;

                    if (isCreate && evt.DispatcherId != 0)
                    {
                        dispatchersWithCreate.Add(evt.DispatcherId);
                    }
                }

                foreach (var evt in _events)
                {
                    bool isCreate = evt.EventId is
                        AsyncEventID.CreateStateMachineAsyncContext or
                        AsyncEventID.CreateRuntimeAsyncContext or
                        AsyncEventID.CreateRuntimeAsyncCallstack;

                    if (!isCreate || evt.DispatcherId == 0)
                    {
                        continue;
                    }

                    DispatcherKind kind = evt.EventId switch
                    {
                        AsyncEventID.CreateStateMachineAsyncContext => DispatcherKind.StateMachine,
                        AsyncEventID.CreateRuntimeAsyncContext or
                        AsyncEventID.CreateRuntimeAsyncCallstack => DispatcherKind.Runtime,
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
            // By default the climb stops at StateMachine<->Runtime boundaries so a Runtime marker doesn't get pulled up into
            // an enclosing StateMachine test-runner dispatcher (and vice versa). Pass crossKinds: true to walk to
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
            // the walk is restricted to dispatchers of the same kind (StateMachine or Runtime) as dispatcherId so that a
            // single test's chain doesn't span unrelated cross-kind activity (e.g., a StateMachine xunit test runner
            // hosting a Runtime test body). Pass crossKinds: true to walk the full connected component.
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
            // StateMachine dispatchers may emit a partial Resume callstack when the parent continuation hasn't yet
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
                        case AsyncEventID.SuspendRuntimeAsyncContext:
                        case AsyncEventID.CompleteRuntimeAsyncContext:
                        case AsyncEventID.SuspendStateMachineAsyncContext:
                        case AsyncEventID.CompleteStateMachineAsyncContext:
                        {
                            openByDispatcherId.Remove(evt.DispatcherId);
                            break;
                        }
                        case AsyncEventID.ResumeRuntimeAsyncCallstack:
                        case AsyncEventID.ResumeStateMachineAsyncCallstack:
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
                        case AsyncEventID.AppendStateMachineAsyncCallstack:
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
                // StateMachine (StateMachineAsync_*) and Runtime (RuntimeAsync_*) are independent context dimensions that can
                // interleave within a single thread's buffer (e.g. a StateMachine test-runner dispatcher active
                // while a Runtime test runs, which always happens on single-threaded WASM where everything
                // shares one thread). Context-scoped events (Resume/Suspend/Complete context, the
                // method events, and unwind events) omit the dispatcher id from the wire and inherit it
                // from the enclosing Resume context, so each kind needs its own current-context stack.
                // A single shared stack would let a StateMachineAsync_* Resume/Complete hijack the current
                // dispatcher and mis-attribute interleaved RuntimeAsync_* events (and vice versa).
                ulong v2CurrentDispatcherId = 0;
                ulong v2CurrentParentDispatcherId = 0;
                ulong v1CurrentDispatcherId = 0;
                ulong v1CurrentParentDispatcherId = 0;
                var v2DispatcherStack = new Stack<(ulong DispatcherId, ulong ParentDispatcherId)>();
                var v1DispatcherStack = new Stack<(ulong DispatcherId, ulong ParentDispatcherId)>();
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
                        AsyncEventID.CreateRuntimeAsyncContext or AsyncEventID.CreateStateMachineAsyncContext =>
                            ParseCreateContextEvent(eventId, baseTimestamp, osThreadId, buffer, ref index),

                        AsyncEventID.ResumeRuntimeAsyncContext =>
                            ParseResumeContextEvent(eventId, baseTimestamp, osThreadId, buffer, ref index,
                                ref v2CurrentDispatcherId, ref v2CurrentParentDispatcherId, v2DispatcherStack),

                        AsyncEventID.ResumeStateMachineAsyncContext =>
                            ParseResumeContextEvent(eventId, baseTimestamp, osThreadId, buffer, ref index,
                                ref v1CurrentDispatcherId, ref v1CurrentParentDispatcherId, v1DispatcherStack),

                        AsyncEventID.CompleteRuntimeAsyncContext or AsyncEventID.SuspendRuntimeAsyncContext =>
                            ParseEndContextEvent(eventId, baseTimestamp, osThreadId,
                                ref v2CurrentDispatcherId, ref v2CurrentParentDispatcherId, v2DispatcherStack),

                        AsyncEventID.CompleteStateMachineAsyncContext or AsyncEventID.SuspendStateMachineAsyncContext =>
                            ParseEndContextEvent(eventId, baseTimestamp, osThreadId,
                                ref v1CurrentDispatcherId, ref v1CurrentParentDispatcherId, v1DispatcherStack),

                        AsyncEventID.ResumeRuntimeAsyncMethod or AsyncEventID.CompleteRuntimeAsyncMethod =>
                            new ParsedEvent
                            {
                                EventId = eventId,
                                Timestamp = baseTimestamp,
                                OsThreadId = osThreadId,
                                ParentDispatcherId = v2CurrentParentDispatcherId,
                                DispatcherId = v2CurrentDispatcherId,
                            },

                        AsyncEventID.ResumeStateMachineAsyncMethod or AsyncEventID.CompleteStateMachineAsyncMethod =>
                            new ParsedEvent
                            {
                                EventId = eventId,
                                Timestamp = baseTimestamp,
                                OsThreadId = osThreadId,
                                ParentDispatcherId = v1CurrentParentDispatcherId,
                                DispatcherId = v1CurrentDispatcherId,
                            },

                        AsyncEventID.ResetAsyncThreadContext or AsyncEventID.ResetAsyncContinuationWrapperIndex =>
                            ParseResetEvent(eventId, baseTimestamp, osThreadId,
                                ref v2CurrentDispatcherId, ref v2CurrentParentDispatcherId, v2DispatcherStack,
                                ref v1CurrentDispatcherId, ref v1CurrentParentDispatcherId, v1DispatcherStack),

                        AsyncEventID.CreateRuntimeAsyncCallstack or AsyncEventID.ResumeRuntimeAsyncCallstack or
                        AsyncEventID.SuspendRuntimeAsyncCallstack or
                        AsyncEventID.ResumeStateMachineAsyncCallstack or AsyncEventID.AppendStateMachineAsyncCallstack =>
                            ParseCallstackEvent(eventId, baseTimestamp, osThreadId, buffer, ref index),

                        AsyncEventID.UnwindRuntimeAsyncException =>
                            ParseUnwindEvent(eventId, baseTimestamp, osThreadId, v2CurrentDispatcherId, v2CurrentParentDispatcherId, buffer, ref index),

                        AsyncEventID.UnwindStateMachineAsyncException =>
                            ParseUnwindEvent(eventId, baseTimestamp, osThreadId, v1CurrentDispatcherId, v1CurrentParentDispatcherId, buffer, ref index),

                        AsyncEventID.AsyncProfilerMetadata =>
                            ParseMetadataEvent(baseTimestamp, osThreadId, 0, 0, buffer, ref index),

                        AsyncEventID.AsyncProfilerSyncClock =>
                            ParseSyncClockEvent(baseTimestamp, osThreadId, 0, 0, buffer, ref index),

                        _ => ParseUnknownEvent(eventId, baseTimestamp, osThreadId, 0, 0, buffer, ref index)
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
                ref ulong v2CurrentDispatcherId, ref ulong v2CurrentParentDispatcherId,
                Stack<(ulong DispatcherId, ulong ParentDispatcherId)> v2DispatcherStack,
                ref ulong v1CurrentDispatcherId, ref ulong v1CurrentParentDispatcherId,
                Stack<(ulong DispatcherId, ulong ParentDispatcherId)> v1DispatcherStack)
            {
                // ResetAsyncThreadContext is a thread-level reset and clears both kind contexts.
                if (eventId == AsyncEventID.ResetAsyncThreadContext)
                {
                    v2DispatcherStack.Clear();
                    v2CurrentDispatcherId = 0;
                    v2CurrentParentDispatcherId = 0;
                    v1DispatcherStack.Clear();
                    v1CurrentDispatcherId = 0;
                    v1CurrentParentDispatcherId = 0;
                }

                return new ParsedEvent
                {
                    EventId = eventId,
                    Timestamp = timestamp,
                    OsThreadId = osThreadId,
                    ParentDispatcherId = 0,
                    DispatcherId = 0,
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

        // Runs a scenario whose synchronous prefix may install a custom SynchronizationContext (or
        // TaskScheduler) on the running thread. On multi-threaded platforms the prefix runs on a
        // dedicated, short-lived thread so that any context it installs -- and leaves in place when
        // the await suspends -- dies with that throwaway thread instead of leaking onto a shared
        // thread-pool thread. On single-threaded platforms (e.g. WASM) threads can't be created and
        // the await resumes on the same (only) thread, so the scenario's own same-thread-guarded
        // finally restores the context; there the scenario is simply run inline.
        private static Task RunIsolatedScenarioAsync(Func<Task> scenario)
        {
            if (!PlatformDetection.IsMultithreadingSupported)
            {
                return scenario();
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                try
                {
                    scenario().GetAwaiter().GetResult();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            })
            {
                IsBackground = true,
                Name = "AsyncProfilerIsolatedScenario"
            };
            thread.Start();
            return tcs.Task;
        }

        private static void RunScenarioAndFlush(Func<Task> scenario)
        {
            // StateMachine (task-based) async: the dispatcher's finally block emits CompleteAsyncContext
            // after inner.MoveNext() returns, but MoveNext() already set the task result which
            // unblocks this thread. Brief sleep ensures the pool thread's finally completes.
            // Runtime (runtime-async) does not have this issue -- Complete fires inside the dispatch
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

        // Compact, single-string summary of the parsed event stream for CI failure diagnostics.
        // CI surfaces only the assertion message (no console access), so failing assertions embed
        // this so the event sequence shows up directly in the error. Events are listed in GLOBAL
        // timestamp order (so cross-thread interleaving -- e.g. a StateMachine test-runner dispatcher
        // interleaving with Runtime events on single-threaded WASM -- is visible), each annotated with a
        // relative timestamp (+ticks from the first event) and its OS thread.
        // Format: "[+<deltaTicks>,T<osThreadId>]<EventId>(d<DispatcherId>[,p<ParentDispatcherId>][,f<FrameCount>]) ..."
        // Compact, machine-readable summary of the parsed event stream for CI failure diagnostics.
        // CI surfaces only the assertion message (no console access), so failing assertions embed
        // this so the event sequence shows up directly in the error. Emitted as CSV (one event per
        // line, header first) in GLOBAL timestamp order so it is both human-readable and trivially
        // importable into a SQL table / spreadsheet (e.g. SQLite .import). Columns:
        //   seq    - global index in timestamp order
        //   delta  - relative timestamp (ticks from the first event); reveals inline-burst clusters vs scheduling gaps
        //   thread - OS thread id; reveals thread hops / cross-thread interleaving (e.g. StateMachine runner vs Runtime on single-threaded WASM)
        //   event  - AsyncEventID name
        //   disp   - DispatcherId
        //   parent - ParentDispatcherId (0 if none; non-zero shows StateMachine<->Runtime parent capture)
        //   frames - callstack FrameCount (0 for non-callstack events)
        //   unwind - UnwindFrameCount (for UnwindAsyncException events)
        private static string DescribeEvents(ParsedEventStream stream)
        {
            // stream.All is already stably sorted by timestamp (ties keep emission/thread order).
            var ordered = stream.All;
            long t0 = ordered.Count > 0 ? ordered[0].Timestamp : 0;
            const string Header = "seq,delta,thread,event,disp,parent,frames,unwind";
            // Emit a strictly increasing delta so the CSV stays correctly ordered if it is re-sorted
            // by the delta column downstream (e.g. after importing into a table): consecutive events
            // that shared a timestamp get successive deltas instead of duplicates. A real timing gap
            // still shows as a large jump, so cluster-vs-gap readability is preserved.
            var rows = new List<string>(ordered.Count);
            long prevDelta = long.MinValue;
            for (int i = 0; i < ordered.Count; i++)
            {
                var e = ordered[i];
                long delta = e.Timestamp - t0;
                if (delta <= prevDelta)
                {
                    delta = prevDelta + 1;
                }
                prevDelta = delta;
                rows.Add($"{i},{delta},{e.OsThreadId},{e.EventId},{e.DispatcherId},{e.ParentDispatcherId},{e.FrameCount},{e.UnwindFrameCount}");
            }
            return "----- BEGIN ASYNC EVENTS (CSV) -----" + Environment.NewLine
                + Header + Environment.NewLine
                + string.Join(Environment.NewLine, rows) + Environment.NewLine
                + "----- END ASYNC EVENTS -----";
        }

        // Lets the stream-aware asserts accept EITHER an already-parsed ParsedEventStream or the raw
        // CollectedEvents (parsed lazily, only on failure). Tests that have a parsed stream pass it
        // directly; tests that only have the collected events pass those. The implicit conversions
        // keep the assert helpers to a single overload set.
        private readonly struct EventDump
        {
            private readonly ParsedEventStream _stream;
            private readonly CollectedEvents _events;

            private EventDump(ParsedEventStream stream, CollectedEvents events)
            {
                _stream = stream;
                _events = events;
            }

            public static implicit operator EventDump(ParsedEventStream stream) => new EventDump(stream, null);
            public static implicit operator EventDump(CollectedEvents events) => new EventDump(null, events);

            public string Describe() => DescribeEvents(_stream ?? ParseAllEvents(_events));
        }

        // Stream-aware assert helpers: thin wrappers over the corresponding xunit asserts that, on
        // failure, append the compact event stream (DescribeEvents) to xunit's own failure message.
        // CI surfaces only the assertion string (no console), so these make a failing assert
        // self-diagnosing. The dump is computed lazily -- only when the assertion actually fails --
        // so passing asserts pay nothing, and xunit's native message (e.g. Equal's expected/actual
        // diff) is preserved. The first argument accepts either a ParsedEventStream or CollectedEvents.
        // Every test that has either in scope should use these instead of the raw Assert.* methods so
        // any CI failure carries the event trace.
        private static void Wrap(EventDump dump, Action assert)
        {
            try
            {
                assert();
            }
            catch (Xunit.Sdk.XunitException ex)
            {
                throw new Xunit.Sdk.XunitException($"{ex.Message}{Environment.NewLine}{dump.Describe()}");
            }
        }

        private static void AssertTrue(EventDump dump, bool condition, string message = null) => Wrap(dump, () => Assert.True(condition, message));

        private static void AssertFalse(EventDump dump, bool condition, string message = null) => Wrap(dump, () => Assert.False(condition, message));

        private static void AssertNotEmpty<T>(EventDump dump, IEnumerable<T> collection) => Wrap(dump, () => Assert.NotEmpty(collection));

        private static void AssertEmpty<T>(EventDump dump, IEnumerable<T> collection) => Wrap(dump, () => Assert.Empty(collection));

        private static void AssertEqual<T>(EventDump dump, T expected, T actual) => Wrap(dump, () => Assert.Equal(expected, actual));

        private static void AssertNotNull(EventDump dump, object @object) => Wrap(dump, () => Assert.NotNull(@object));

        private static void AssertSingle<T>(EventDump dump, IEnumerable<T> collection) => Wrap(dump, () => Assert.Single(collection));

        private static void AssertContains<T>(EventDump dump, T expected, IEnumerable<T> collection) => Wrap(dump, () => Assert.Contains(expected, collection));

        private static void AssertContains(EventDump dump, string expectedSubstring, string actualString) => Wrap(dump, () => Assert.Contains(expectedSubstring, actualString));

        private static void AssertContains<T>(EventDump dump, IEnumerable<T> collection, Predicate<T> filter) => Wrap(dump, () => Assert.Contains(collection, filter));

        private static void AssertDoesNotContain<T>(EventDump dump, T expected, IEnumerable<T> collection) => Wrap(dump, () => Assert.DoesNotContain(expected, collection));

        private static void AssertAll<T>(EventDump dump, IEnumerable<T> collection, Action<T> action) => Wrap(dump, () => Assert.All(collection, action));


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
        // Handles both StateMachine (StateMachineAsync_*) and Runtime (RuntimeAsync) event ids.
        private static void AssertCallstackSimulationReachesZero(ParsedEventStream stream, string markerMethodName)
        {
            var resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeRuntimeAsyncCallstack, markerMethodName);
            if (resumeStacks.Count == 0)
            {
                resumeStacks = stream.CallstacksWithMarker(AsyncEventID.ResumeStateMachineAsyncCallstack, markerMethodName);
            }
            AssertTrue(stream, resumeStacks.Count >= 1, $"Expected at least one resume callstack with marker '{markerMethodName}'");

            ulong dispatcherId = resumeStacks[0].DispatcherId;

            var sequence = stream.ChainEventsFromDispatcher(dispatcherId);
            int stackDepth = 0;

            foreach (var evt in sequence)
            {
                switch (evt.EventId)
                {
                    case AsyncEventID.ResumeRuntimeAsyncCallstack:
                    case AsyncEventID.ResumeStateMachineAsyncCallstack:
                    {
                        stackDepth = (int)evt.FrameCount;
                        break;
                    }
                    case AsyncEventID.CompleteRuntimeAsyncMethod:
                    case AsyncEventID.CompleteStateMachineAsyncMethod:
                    {
                        if (stackDepth > 0)
                        {
                            stackDepth--;
                        }

                        break;
                    }
                    case AsyncEventID.UnwindRuntimeAsyncException:
                    case AsyncEventID.UnwindStateMachineAsyncException:
                    {
                        stackDepth = Math.Max(0, stackDepth - (int)evt.UnwindFrameCount);
                        break;
                    }
                }
            }

            AssertTrue(stream, stackDepth == 0, $"Expected callstack simulation for '{markerMethodName}' (DispatcherId {dispatcherId}) to reach 0, got {stackDepth}");
        }

        private static void AssertExactlyOneCreateAndComplete(ParsedEventStream stream, ulong dispatcherId, string chainName)
        {
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();
            int creates = ids.Count(id => id is AsyncEventID.CreateRuntimeAsyncContext or AsyncEventID.CreateStateMachineAsyncContext);
            int completes = ids.Count(id => id is AsyncEventID.CompleteRuntimeAsyncContext or AsyncEventID.CompleteStateMachineAsyncContext);
            AssertTrue(stream, creates == 1, $"Expected exactly 1 CreateAsyncContext for {chainName} (DispatcherId {dispatcherId}), got {creates}");
            AssertTrue(stream, completes == 1, $"Expected exactly 1 CompleteAsyncContext for {chainName} (DispatcherId {dispatcherId}), got {completes}");
        }

        // StateMachine dispatcher model with reuse: a single dispatcher spans all of a method's yields
        // (it resumes/suspends multiple times) and is created + completed exactly once. So within a
        // dispatcher tree every Create is balanced by exactly one Complete; Suspends are interior events.
        private static void AssertCreateBalancesSuspendAndCompleteInChain(ParsedEventStream stream, ulong dispatcherId, string chainName)
        {
            var ids = stream.ChainEventsFromDispatcher(dispatcherId).Select(e => e.EventId).ToList();
            int creates = ids.Count(id => id == AsyncEventID.CreateStateMachineAsyncContext);
            int resumes = ids.Count(id => id == AsyncEventID.ResumeStateMachineAsyncContext);
            int suspends = ids.Count(id => id == AsyncEventID.SuspendStateMachineAsyncContext);
            int completes = ids.Count(id => id == AsyncEventID.CompleteStateMachineAsyncContext);
            AssertTrue(stream, creates >= 1, $"Expected at least 1 CreateStateMachineAsyncContext for {chainName} (DispatcherId {dispatcherId}), got {creates}");
            AssertTrue(stream, creates == completes, $"Expected CreateStateMachineAsyncContext count == CompleteStateMachineAsyncContext count for {chainName} (DispatcherId {dispatcherId}), got {creates} creates, {completes} completes");
            AssertTrue(stream, resumes == completes + suspends, $"Expected ResumeStateMachineAsyncContext count == Complete + Suspend count for {chainName} (DispatcherId {dispatcherId}), got {resumes} resumes, {completes} completes, {suspends} suspends");
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
                            AsyncEventID.CreateRuntimeAsyncContext => OutputCreateAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.ResumeRuntimeAsyncContext => OutputResumeAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.SuspendRuntimeAsyncContext => OutputSuspendAsyncContextEvent(),
                            AsyncEventID.CompleteRuntimeAsyncContext => OutputCompleteAsyncContextEvent(),
                            AsyncEventID.UnwindRuntimeAsyncException => OutputUnwindAsyncExceptionEvent(buffer.Slice(index)),
                            AsyncEventID.CreateRuntimeAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.ResumeRuntimeAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.SuspendRuntimeAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.ResumeRuntimeAsyncMethod => OutputResumeAsyncMethodEvent(),
                            AsyncEventID.CompleteRuntimeAsyncMethod => OutputCompleteAsyncMethodEvent(),

                            AsyncEventID.CreateStateMachineAsyncContext => OutputCreateAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.ResumeStateMachineAsyncContext => OutputResumeAsyncContextEvent(buffer.Slice(index)),
                            AsyncEventID.SuspendStateMachineAsyncContext => OutputSuspendAsyncContextEvent(),
                            AsyncEventID.CompleteStateMachineAsyncContext => OutputCompleteAsyncContextEvent(),
                            AsyncEventID.UnwindStateMachineAsyncException => OutputUnwindAsyncExceptionEvent(buffer.Slice(index)),
                            AsyncEventID.ResumeStateMachineAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),
                            AsyncEventID.ResumeStateMachineAsyncMethod => OutputResumeAsyncMethodEvent(),
                            AsyncEventID.CompleteStateMachineAsyncMethod => OutputCompleteAsyncMethodEvent(),
                            AsyncEventID.AppendStateMachineAsyncCallstack => OutputAsyncCallstackEvent(eventId, buffer.Slice(index)),

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

                AsyncCallstackType type = (eventId is AsyncEventID.ResumeStateMachineAsyncCallstack
                                                   or AsyncEventID.AppendStateMachineAsyncCallstack)
                    ? AsyncCallstackType.StateMachine
                    : AsyncCallstackType.Runtime;
                callstackId = buffer[index++];
                continuationIndex = buffer[index++];
                asyncCallstackLength = buffer[index++];
                if (eventId == AsyncEventID.CreateRuntimeAsyncCallstack)
                {
                    Deserializer.ReadCompressedUInt64(buffer, ref index, out parentDispatcherId);
                }
                Deserializer.ReadCompressedUInt64(buffer, ref index, out dispatcherId);

                if (eventId == AsyncEventID.CreateRuntimeAsyncCallstack)
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

                bool readState = type == AsyncCallstackType.StateMachine;

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
                if (type == AsyncCallstackType.StateMachine)
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
