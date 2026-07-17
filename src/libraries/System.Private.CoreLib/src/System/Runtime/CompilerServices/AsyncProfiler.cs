// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if CORECLR || NATIVEAOT
#define RUNTIME_ASYNC_SUPPORTED
#endif

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serializer = System.Runtime.CompilerServices.AsyncProfiler.EventBuffer.Serializer;

namespace System.Runtime.CompilerServices
{
    internal static partial class AsyncProfiler
    {
        internal enum AsyncEventID : byte
        {
            // V2 (RuntimeAsync) events.
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

            // V1 (StateMachineAsync) events.
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

        // Per-event manifest entry: schema version + payload length-prefix size.
        // The runtime emits this table in the AsyncProfilerMetadata event so parsers can
        // discover the current version of each event id and skip payloads of events they do
        // not understand by reading a fixed-width little-endian length prefix that precedes
        // the payload. A FieldSize of NoPayload indicates the event carries no payload at all
        // (no prefix written). Bumping an entry's Version signals any schema change.
        internal readonly struct EventManifestEntry
        {
            public enum PayloadLengthFieldSize : byte
            {
                NoPayload = 0,
                Byte = 1,
                UShort = 2
            }

            public readonly AsyncEventID EventId;
            public readonly byte Version;
            public readonly PayloadLengthFieldSize FieldSize;

            public EventManifestEntry(AsyncEventID eventId, byte version, PayloadLengthFieldSize fieldSize)
            {
                EventId = eventId;
                Version = version;
                FieldSize = fieldSize;
            }
        }

        internal static class EventManifest
        {
            // Per-event payload size.
            public const EventManifestEntry.PayloadLengthFieldSize CreateAsyncContextPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.Byte;
            public const EventManifestEntry.PayloadLengthFieldSize ResumeAsyncContextPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.Byte;
            public const EventManifestEntry.PayloadLengthFieldSize SuspendAsyncContextPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.NoPayload;
            public const EventManifestEntry.PayloadLengthFieldSize CompleteAsyncContextPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.NoPayload;
            public const EventManifestEntry.PayloadLengthFieldSize UnwindAsyncExceptionPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.Byte;
            public const EventManifestEntry.PayloadLengthFieldSize CallstackPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.UShort;
            public const EventManifestEntry.PayloadLengthFieldSize CreateAsyncCallstackPayloadLengthFieldSize = CallstackPayloadLengthFieldSize;
            public const EventManifestEntry.PayloadLengthFieldSize ResumeAsyncCallstackPayloadLengthFieldSize = CallstackPayloadLengthFieldSize;
            public const EventManifestEntry.PayloadLengthFieldSize SuspendAsyncCallstackPayloadLengthFieldSize = CallstackPayloadLengthFieldSize;
            public const EventManifestEntry.PayloadLengthFieldSize ResumeAsyncMethodPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.NoPayload;
            public const EventManifestEntry.PayloadLengthFieldSize CompleteAsyncMethodPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.NoPayload;
            public const EventManifestEntry.PayloadLengthFieldSize AppendAsyncCallstackPayloadLengthFieldSize = CallstackPayloadLengthFieldSize;

            public const EventManifestEntry.PayloadLengthFieldSize ResetAsyncThreadContextPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.NoPayload;
            public const EventManifestEntry.PayloadLengthFieldSize ResetAsyncContinuationWrapperIndexPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.NoPayload;
            public const EventManifestEntry.PayloadLengthFieldSize AsyncProfilerMetadataPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.UShort;
            public const EventManifestEntry.PayloadLengthFieldSize AsyncProfilerSyncClockPayloadLengthFieldSize = EventManifestEntry.PayloadLengthFieldSize.Byte;

            public static readonly EventManifestEntry[] Entries = BuildEntries();

            public static EventManifestEntry.PayloadLengthFieldSize GetPayloadLengthFieldSize(AsyncEventID eventID)
            {
                Debug.Assert(eventID == Entries[(byte)eventID - 1].EventId);
                return Entries[(byte)eventID - 1].FieldSize;
            }

            private static EventManifestEntry[] BuildEntries()
            {
                // Entries must be appended in order of ascending (byte)AsyncEventID so that
                // Entries[(byte)id - 1] yields the entry for id. BuildEntries asserts this
                // invariant after construction.
                EventManifestEntry[] entries =
                [
                    new EventManifestEntry(AsyncEventID.CreateRuntimeAsyncContext, 1, CreateAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.ResumeRuntimeAsyncContext, 1, ResumeAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.SuspendRuntimeAsyncContext, 1, SuspendAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.CompleteRuntimeAsyncContext, 1, CompleteAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.UnwindRuntimeAsyncException, 1, UnwindAsyncExceptionPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.CreateRuntimeAsyncCallstack, 1, CreateAsyncCallstackPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.ResumeRuntimeAsyncCallstack, 1, ResumeAsyncCallstackPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.SuspendRuntimeAsyncCallstack, 1, SuspendAsyncCallstackPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.ResumeRuntimeAsyncMethod, 1, ResumeAsyncMethodPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.CompleteRuntimeAsyncMethod, 1, CompleteAsyncMethodPayloadLengthFieldSize),

                    new EventManifestEntry(AsyncEventID.CreateStateMachineAsyncContext, 1, CreateAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.ResumeStateMachineAsyncContext, 1, ResumeAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.SuspendStateMachineAsyncContext, 1, SuspendAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.CompleteStateMachineAsyncContext, 1, CompleteAsyncContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.UnwindStateMachineAsyncException, 1, UnwindAsyncExceptionPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.ResumeStateMachineAsyncCallstack, 1, ResumeAsyncCallstackPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.ResumeStateMachineAsyncMethod, 1, ResumeAsyncMethodPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.CompleteStateMachineAsyncMethod, 1, CompleteAsyncMethodPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.AppendStateMachineAsyncCallstack, 1, AppendAsyncCallstackPayloadLengthFieldSize),

                    new EventManifestEntry(AsyncEventID.ResetAsyncThreadContext, 1, ResetAsyncThreadContextPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.ResetAsyncContinuationWrapperIndex, 1, ResetAsyncContinuationWrapperIndexPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.AsyncProfilerMetadata, 1, AsyncProfilerMetadataPayloadLengthFieldSize),
                    new EventManifestEntry(AsyncEventID.AsyncProfilerSyncClock, 1, AsyncProfilerSyncClockPayloadLengthFieldSize),
                ];

#if DEBUG
                for (int i = 0; i < entries.Length; i++)
                {
                    Debug.Assert((byte)entries[i].EventId == i + 1, "EventManifest.Entries must be dense and ordered by ascending AsyncEventID starting at 1.");
                }
#endif

                return entries;
            }
        }

        internal ref struct Info
        {
            public object? Context;
            public object? CurrentContinuation;
            public bool CurrentContinuationCompleted;
            public ref nint ContinuationTable;
            public uint ContinuationIndex;
        }

        internal static void InitInfo(ref Info info)
        {
            info.Context = null;
            info.CurrentContinuation = null;
            info.CurrentContinuationCompleted = false;
            ContinuationWrapper.InitInfo(ref info);
        }

        internal static partial class Config
        {
            public static readonly Lock ConfigLock = new();

            public static bool Changed(AsyncThreadContext context) => context.ConfigRevision != Revision;

            public static void Update(EventLevel logLevel, EventKeywords eventKeywords)
            {
                lock (ConfigLock)
                {
                    Revision++;

                    ActiveEventKeywords = 0;
                    if (logLevel == EventLevel.LogAlways || logLevel >= EventLevel.Informational)
                    {
                        ActiveEventKeywords = eventKeywords;
                    }

                    string? eventBufferSizeEnv = System.Environment.GetEnvironmentVariable("DOTNET_AsyncProfilerEventSource_EventBufferSize");
                    if (eventBufferSizeEnv != null && uint.TryParse(eventBufferSizeEnv, out uint eventBufferSize))
                    {
                        eventBufferSize = Math.Max(eventBufferSize, 1024);
                        EventBufferSize = Math.Min(eventBufferSize, 64 * 1024 - 256);
                    }

                    if (IsEnabled.AnyAsyncEvents(ActiveEventKeywords))
                    {
                        AsyncThreadContextCache.EnableFlushTimer();
                        AsyncThreadContextCache.DisableCleanupTimer();
                    }
                    else
                    {
                        AsyncThreadContextCache.DisableFlushTimer();
                        AsyncThreadContextCache.EnableCleanupTimer();
                    }

                    // Writer thread access both ActiveFlags and Revision without explicit acquire/release semantics,
                    // but Flags will be read before calling AcquireAsyncThreadContext that includes one volatile read
                    // acting as the load barrier for ActiveFlags and Revision.
                    Interlocked.MemoryBarrier();

                    UpdateFlags();
                }
            }

            public static void EmitAsyncProfilerMetadataIfNeeded(AsyncThreadContext context)
            {
                if (s_metadataRevision != Revision)
                {
                    lock (s_metadataRevisionLock)
                    {
                        if (s_metadataRevision != Revision)
                        {
                            // Metadata payload:
                            // [qpcFrequency (compressed uint64)]
                            // [qpcSync (compressed uint64)]
                            // [utcSync (compressed uint64)]
                            // [eventBufferSize (compressed uint32)]
                            // [wrapperCount byte]
                            // [eventManifestEntryCount byte] -- number of entries that follow
                            // [for each manifest entry: eventId (byte), version (byte), payloadLengthFieldSize (byte)]
                            EventManifestEntry[] entries = EventManifest.Entries;
                            Debug.Assert(entries.Length <= byte.MaxValue);
                            int maxPayloadLength =
                                Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt32Size + 1
                                + 1 + entries.Length * 3;

                            ref EventBuffer eventBuffer = ref context.EventBuffer;
                            if (Serializer.AsyncEventHeader(context, ref eventBuffer, AsyncEventID.AsyncProfilerMetadata, EventManifest.AsyncProfilerMetadataPayloadLengthFieldSize, maxPayloadLength, out int payloadLengthFieldOffset))
                            {
                                SyncClock(out long utcTimeSync, out long qpcSync);

                                Span<byte> payloadSpan = eventBuffer.Data.AsSpan(eventBuffer.Index, maxPayloadLength);
                                int payloadSpanIndex = 0;

                                payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)Stopwatch.Frequency);
                                payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)qpcSync);
                                payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)utcTimeSync);
                                payloadSpanIndex += Serializer.WriteCompressedUInt32(payloadSpan.Slice(payloadSpanIndex), EventBufferSize);

                                payloadSpan[payloadSpanIndex++] = ContinuationWrapper.COUNT;

                                // Event manifest: one entry per defined AsyncEventID, three bytes each.
                                payloadSpan[payloadSpanIndex++] = (byte)entries.Length;
                                for (int i = 0; i < entries.Length; i++)
                                {
                                    EventManifestEntry entry = entries[i];
                                    payloadSpan[payloadSpanIndex++] = (byte)entry.EventId;
                                    payloadSpan[payloadSpanIndex++] = entry.Version;
                                    payloadSpan[payloadSpanIndex++] = (byte)entry.FieldSize;
                                }

                                eventBuffer.Index += payloadSpanIndex;

                                Serializer.WriteAsyncEventPayloadLength(ref eventBuffer, AsyncEventID.AsyncProfilerMetadata, EventManifest.AsyncProfilerMetadataPayloadLengthFieldSize, payloadLengthFieldOffset);

                                // Force flush to deliver event promptly.
                                context.Flush();

                                s_metadataRevision = Revision;
                                s_lastSyncClockEventTimestamp = Stopwatch.GetTimestamp();
                            }
                        }
                    }
                }
            }

            public static void EmitSyncClockEventIfNeeded()
            {
                long currentTimestamp = Stopwatch.GetTimestamp();
                if (s_lastSyncClockEventTimestamp == 0)
                {
                    s_lastSyncClockEventTimestamp = currentTimestamp;
                    return;
                }

                if (currentTimestamp - s_lastSyncClockEventTimestamp < s_intervalBetweenSyncClockEvent)
                {
                    return;
                }

                s_lastSyncClockEventTimestamp = currentTimestamp;

                if (IsEnabled.AnyAsyncEvents(ActiveEventKeywords))
                {
                    AsyncThreadContext transientContext = AsyncThreadContext.AcquireTransient();

                    // SyncClock payload:
                    // [qpcSync (compressed uint64)]
                    // [utcSync (compressed uint64)]
                    const int MaxPayloadLength = Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt64Size;

                    ref EventBuffer eventBuffer = ref transientContext.EventBuffer;
                    if (Serializer.AsyncEventHeader(transientContext, ref eventBuffer, AsyncEventID.AsyncProfilerSyncClock, EventManifest.AsyncProfilerSyncClockPayloadLengthFieldSize, MaxPayloadLength, out int payloadLengthFieldOffset))
                    {
                        SyncClock(out long utcTimeSync, out long qpcSync);

                        Span<byte> payloadSpan = eventBuffer.Data.AsSpan(eventBuffer.Index, MaxPayloadLength);
                        int payloadSpanIndex = 0;

                        payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)qpcSync);
                        payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)utcTimeSync);

                        eventBuffer.Index += payloadSpanIndex;

                        Serializer.WriteAsyncEventPayloadLength(ref eventBuffer, AsyncEventID.AsyncProfilerSyncClock, EventManifest.AsyncProfilerSyncClockPayloadLengthFieldSize, payloadLengthFieldOffset);

                        // Force flush to deliver event promptly.
                        transientContext.Flush();
                    }

                    AsyncThreadContext.Release(transientContext);
                }
            }

            private static void SyncClock(out long utcTimeSync, out long qpcSync)
            {
                long qpcDiff = long.MaxValue;

                utcTimeSync = 0;
                qpcSync = 0;

                // Run calibration loop to find the closest QPC timestamp to UTC timestamp.
                // This is a best effort to minimize the max error between QPC and UTC timestamps.
                for (int i = 0; i < 10; i++)
                {
                    long qpc1 = Stopwatch.GetTimestamp();
                    long utcTime = DateTime.UtcNow.ToFileTimeUtc();
                    long qpc2 = Stopwatch.GetTimestamp();
                    long diff = qpc2 - qpc1;

                    if (diff < qpcDiff)
                    {
                        utcTimeSync = utcTime;
                        qpcSync = qpc1;
                        qpcDiff = diff;
                    }
                }

                // QPC and UTC clocks are not guaranteed to be perfectly linear, so this is a best effort to minimize the max error.
                // Both QPC and DateTime.UtcNow should have a 100ns resolution (or better). If latency getting QPC and UTC time is small enough,
                // the error introduced by non-linearity should be within 100ns.
                qpcSync += qpcDiff / 2;
            }

            private static void UpdateFlags()
            {
                AsyncInstrumentation.Flags flags = AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CreateRuntimeAsyncContextEvent(ActiveEventKeywords) || IsEnabled.CreateRuntimeAsyncCallstackEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CreateAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.ResumeRuntimeAsyncContextEvent(ActiveEventKeywords) || IsEnabled.ResumeRuntimeAsyncCallstackEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.ResumeAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.SuspendRuntimeAsyncContextEvent(ActiveEventKeywords) || IsEnabled.SuspendRuntimeAsyncCallstackEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.SuspendAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CompleteRuntimeAsyncContextEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CompleteAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.UnwindRuntimeAsyncExceptionEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.UnwindAsyncException : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.ResumeRuntimeAsyncMethodEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.ResumeAsyncMethod : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CompleteRuntimeAsyncMethodEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CompleteAsyncMethod : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CreateStateMachineAsyncContextEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CreateAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.ResumeStateMachineAsyncContextEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.ResumeAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.SuspendStateMachineAsyncContextEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.SuspendAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CompleteStateMachineAsyncContextEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CompleteAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.UnwindStateMachineAsyncExceptionEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.UnwindAsyncException : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.ResumeStateMachineAsyncCallstackEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.ResumeAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.ResumeStateMachineAsyncMethodEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.ResumeAsyncMethod : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CompleteStateMachineAsyncMethodEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CompleteAsyncMethod : AsyncInstrumentation.Flags.Disabled;

                AsyncInstrumentation.UpdateAsyncProfilerFlags(flags);
            }

            public static void CaptureState()
            {
                AsyncThreadContextCache.Flush(true);
            }

            public static EventKeywords ActiveEventKeywords { get; private set; }

            public static uint Revision { get; private set; }

            // Use 16KB - 256 event buffer as default. 256 bytes reserved for event header.
            // 16KB events pack cleanly into a 64KB ETW/EventPipe/UserEvents buffer.
            public static uint EventBufferSize { get; private set; } = 16 * 1024 - 256;

            private static readonly Lock s_metadataRevisionLock = new();

            private static uint s_metadataRevision;

            private static long s_lastSyncClockEventTimestamp;

            private static readonly long s_intervalBetweenSyncClockEvent = Stopwatch.Frequency * 60; // 1 minute
        }

        internal struct EventBuffer
        {
            public byte[] Data;

            public int Index;

            public uint EventCount;

            public static class Serializer
            {
                public const int MaxCompressedUInt32Size = 5;
                public const int MaxCompressedInt32Size = 5;
                public const int MaxCompressedUInt64Size = 10;
                public const int MaxCompressedInt64Size = 10;
                public const int MaxEventHeaderSize = 37;
                public const int MaxAsyncEventHeaderSize = 11;

                public ref struct AsyncEventHeaderRollbackData
                {
                    public int Index;
                    public uint EventCount;
                    public long LastEventTimestamp;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int WriteCompressedInt32(Span<byte> buffer, int value)
                {
                    return WriteCompressedUInt32(buffer, ZigzagEncodeInt32(value));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int WriteCompressedUInt32(Span<byte> buffer, uint value)
                {
                    if (buffer.Length < MaxCompressedUInt32Size)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
                    }

                    ref byte dst = ref MemoryMarshal.GetReference(buffer);
                    int index = 0;

                    while (value > 0x7Fu)
                    {
                        Unsafe.Add(ref dst, index++) = (byte)((uint)value | ~0x7Fu);
                        value >>= 7;
                    }

                    Unsafe.Add(ref dst, index++) = (byte)value;
                    return index;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int WriteCompressedInt64(Span<byte> buffer, long value)
                {
                    return WriteCompressedUInt64(buffer, ZigzagEncodeInt64(value));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int WriteCompressedUInt64(Span<byte> buffer, ulong value)
                {
                    if (buffer.Length < MaxCompressedUInt64Size)
                    {
                        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
                    }

                    ref byte dst = ref MemoryMarshal.GetReference(buffer);
                    int index = 0;

                    while (value > 0x7Fu)
                    {
                        Unsafe.Add(ref dst, index++) = (byte)((uint)value | ~0x7Fu);
                        value >>= 7;
                    }

                    Unsafe.Add(ref dst, index++) = (byte)value;
                    return index;
                }

                public static uint ZigzagEncodeInt32(int value) => (uint)((value << 1) ^ (value >> 31));

                public static ulong ZigzagEncodeInt64(long value) => (ulong)((value << 1) ^ (value >> 63));

                public static void Header(AsyncThreadContext context, ref EventBuffer eventBuffer)
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();

                    eventBuffer.Index = 0;
                    eventBuffer.EventCount = 0;
                    context.LastEventTimestamp = currentTimestamp;

                    Span<byte> headerSpan = eventBuffer.Data.AsSpan(0, MaxEventHeaderSize);
                    int headerSpanIndex = 0;

                    // Version
                    headerSpan[headerSpanIndex++] = 1;

                    // Total size in bytes, will be updated on flush.
                    BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(headerSpanIndex), 0);
                    headerSpanIndex += sizeof(uint);

                    // Async Thread Context ID
                    BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(headerSpanIndex), context.AsyncThreadContextId);
                    headerSpanIndex += sizeof(uint);

                    // OS Thread ID
                    BinaryPrimitives.WriteUInt64LittleEndian(headerSpan.Slice(headerSpanIndex), context.OsThreadId);
                    headerSpanIndex += sizeof(ulong);

                    // Total event count, will be updated on flush.
                    BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(headerSpanIndex), 0);
                    headerSpanIndex += sizeof(uint);

                    // Start timestamp
                    BinaryPrimitives.WriteUInt64LittleEndian(headerSpan.Slice(headerSpanIndex), (ulong)currentTimestamp);
                    headerSpanIndex += sizeof(ulong);

                    // End timestamp, will be updated on flush.
                    BinaryPrimitives.WriteUInt64LittleEndian(headerSpan.Slice(headerSpanIndex), 0);
                    headerSpanIndex += sizeof(ulong);

                    eventBuffer.Index = headerSpanIndex;
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, AsyncEventID eventID, EventManifestEntry.PayloadLengthFieldSize payloadLengthFieldSize, int maxPayloadLength, out int payloadLengthFieldOffset)
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    long delta = currentTimestamp - context.LastEventTimestamp;
                    return AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, payloadLengthFieldSize, maxPayloadLength, out payloadLengthFieldOffset);
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, AsyncEventID eventID)
                {
                    Debug.Assert(EventManifest.GetPayloadLengthFieldSize(eventID) == EventManifestEntry.PayloadLengthFieldSize.NoPayload);
                    return AsyncEventHeader(context, ref eventBuffer, eventID, EventManifestEntry.PayloadLengthFieldSize.NoPayload, 0, out _);
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, AsyncEventID eventID, EventManifestEntry.PayloadLengthFieldSize payloadLengthFieldSize, int maxPayloadLength, out int payloadLengthFieldOffset)
                {
                    long delta = currentTimestamp - context.LastEventTimestamp;
                    return AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, payloadLengthFieldSize, maxPayloadLength, out payloadLengthFieldOffset);
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, AsyncEventID eventID)
                {
                    Debug.Assert(EventManifest.GetPayloadLengthFieldSize(eventID) == EventManifestEntry.PayloadLengthFieldSize.NoPayload);
                    return AsyncEventHeader(context, ref eventBuffer, currentTimestamp, eventID, EventManifestEntry.PayloadLengthFieldSize.NoPayload, 0, out _);
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, long delta, AsyncEventID eventID, EventManifestEntry.PayloadLengthFieldSize payloadLengthFieldSize, int maxPayloadLength, out int payloadLengthFieldOffset, out AsyncEventHeaderRollbackData rollbackData)
                {
                    Debug.Assert(payloadLengthFieldSize == EventManifest.GetPayloadLengthFieldSize(eventID));

                    byte[] buffer = eventBuffer.Data;
                    int index = eventBuffer.Index;
                    long previousTimestamp = context.LastEventTimestamp;

                    int reservedSize = MaxAsyncEventHeaderSize + (byte)payloadLengthFieldSize + maxPayloadLength;

                    if ((index + reservedSize) <= buffer.Length && delta >= 0)
                    {
                        context.LastEventTimestamp = currentTimestamp;
                    }
                    else
                    {
                        // Event is too big for buffer, drop it.
                        if (reservedSize > buffer.Length)
                        {
                            rollbackData = default;
                            payloadLengthFieldOffset = 0;
                            return false;
                        }

                        context.Flush();

                        previousTimestamp = context.LastEventTimestamp;
                        delta = 0;
                        index = eventBuffer.Index;
                    }

                    // Capture state after potential flush but before writing the header.
                    rollbackData = new AsyncEventHeaderRollbackData
                    {
                        Index = index,
                        EventCount = eventBuffer.EventCount,
                        LastEventTimestamp = previousTimestamp,
                    };

                    Span<byte> headerSpan = buffer.AsSpan(index, MaxAsyncEventHeaderSize);
                    int headerSpanIndex = 0;

                    headerSpan[headerSpanIndex++] = (byte)eventID;
                    headerSpanIndex += WriteCompressedUInt64(headerSpan.Slice(headerSpanIndex), (ulong)delta); // Timestamp delta from last event

                    eventBuffer.Index += headerSpanIndex;

                    payloadLengthFieldOffset = eventBuffer.Index;
                    eventBuffer.Index += (byte)payloadLengthFieldSize;

                    eventBuffer.EventCount++;

                    return true;
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, long delta, AsyncEventID eventID, EventManifestEntry.PayloadLengthFieldSize payloadLengthFieldSize, int maxPayloadLength, out int payloadLengthFieldOffset)
                {
                    Debug.Assert(payloadLengthFieldSize == EventManifest.GetPayloadLengthFieldSize(eventID));

                    byte[] buffer = eventBuffer.Data;
                    int index = eventBuffer.Index;

                    int reservedSize = MaxAsyncEventHeaderSize + (byte)payloadLengthFieldSize + maxPayloadLength;

                    if ((index + reservedSize) <= buffer.Length && delta >= 0)
                    {
                        context.LastEventTimestamp = currentTimestamp;
                    }
                    else
                    {
                        // Event is too big for buffer, drop it.
                        if (reservedSize > buffer.Length)
                        {
                            payloadLengthFieldOffset = 0;
                            return false;
                        }

                        context.Flush();

                        delta = 0;
                        index = eventBuffer.Index;
                    }

                    Span<byte> headerSpan = buffer.AsSpan(index, MaxAsyncEventHeaderSize);
                    int headerSpanIndex = 0;

                    headerSpan[headerSpanIndex++] = (byte)eventID;
                    headerSpanIndex += WriteCompressedUInt64(headerSpan.Slice(headerSpanIndex), (ulong)delta); // Timestamp delta from last event

                    eventBuffer.Index += headerSpanIndex;

                    payloadLengthFieldOffset = eventBuffer.Index;
                    eventBuffer.Index += (byte)payloadLengthFieldSize;

                    eventBuffer.EventCount++;

                    return true;
                }

                public static void WriteAsyncEventPayloadLength(ref EventBuffer eventBuffer, AsyncEventID eventID, EventManifestEntry.PayloadLengthFieldSize payloadLengthFieldSize, int payloadLengthFieldOffset)
                {
                    Debug.Assert(payloadLengthFieldSize == EventManifest.GetPayloadLengthFieldSize(eventID));

                    int payloadLength = eventBuffer.Index - payloadLengthFieldOffset - (byte)payloadLengthFieldSize;

                    if (payloadLengthFieldSize == EventManifestEntry.PayloadLengthFieldSize.Byte)
                    {
                        Debug.Assert(payloadLength <= byte.MaxValue);
                        eventBuffer.Data[payloadLengthFieldOffset] = (byte)payloadLength;
                    }
                    else
                    {
                        Debug.Assert(payloadLengthFieldSize == EventManifestEntry.PayloadLengthFieldSize.UShort);
                        Debug.Assert(payloadLength <= ushort.MaxValue);
                        BinaryPrimitives.WriteUInt16LittleEndian(eventBuffer.Data.AsSpan(payloadLengthFieldOffset), (ushort)payloadLength);
                    }
                }

                public static void RollbackAsyncEventHeader(AsyncThreadContext context, in AsyncEventHeaderRollbackData rollbackData)
                {
                    ref EventBuffer eventBuffer = ref context.EventBuffer;
                    eventBuffer.Index = rollbackData.Index;
                    eventBuffer.EventCount = rollbackData.EventCount;
                    context.LastEventTimestamp = rollbackData.LastEventTimestamp;
                }
            }
        }

        internal sealed class AsyncThreadContext
        {
            private static uint s_nextAsyncThreadContextId;

            public AsyncThreadContext()
            {
                _eventBuffer.Data = Array.Empty<byte>();
                AsyncThreadContextId = Interlocked.Increment(ref s_nextAsyncThreadContextId);
                OsThreadId = Thread.CurrentOSThreadId;
            }

            private EventBuffer _eventBuffer;

            public long LastEventTimestamp;

            public EventKeywords ActiveEventKeywords;

            public readonly ulong OsThreadId;

            public readonly uint AsyncThreadContextId;

            public uint ConfigRevision;

            public volatile bool InUse;

            public volatile bool BlockContext;

            public ref EventBuffer EventBuffer
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (_eventBuffer.Data.Length == 0)
                    {
                        InitializeBuffer();
                    }

                    Debug.Assert(InUse || BlockContext);
                    return ref _eventBuffer;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncThreadContext Acquire(ref Info info)
            {
                AsyncThreadContext context = Get(ref info);
                Debug.Assert(!context.InUse);

                context.InUse = true;
                if (context.BlockContext)
                {
                    WaitOnBlockedAsyncThreadContext(context);
                }

                return context;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Acquire(AsyncThreadContext context)
            {
                Debug.Assert(!context.InUse);

                context.InUse = true;
                if (context.BlockContext)
                {
                    WaitOnBlockedAsyncThreadContext(context);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Release(AsyncThreadContext context)
            {
                Debug.Assert(context.InUse);
                context.InUse = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncThreadContext Get()
            {
                AsyncThreadContext? context = t_asyncThreadContext;
                if (context != null)
                {
                    return context;
                }

                return CreateAsyncThreadContext();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncThreadContext Get(ref Info info)
            {
                Debug.Assert(info.Context == null || info.Context is AsyncThreadContext);

                AsyncThreadContext? context = Unsafe.As<AsyncThreadContext?>(info.Context);
                if (context != null)
                {
                    return context;
                }

                return GetAsyncThreadContext(ref info);
            }

            public static AsyncThreadContext AcquireTransient()
            {
                AsyncThreadContext? context;
                if (t_asyncThreadContext != null)
                {
                    context = Get();
                    Debug.Assert(!context.InUse);
                }
                else
                {
                    context = new AsyncThreadContext();
                    context.ConfigRevision = Config.Revision;
                    context.ActiveEventKeywords = Config.ActiveEventKeywords;
                }

                Acquire(context);
                return context;
            }

            public void Reclaim()
            {
                Debug.Assert(InUse || BlockContext);

                _eventBuffer.Data = Array.Empty<byte>();
                _eventBuffer.Index = 0;
                _eventBuffer.EventCount = 0;
            }

            public void Flush()
            {
                Debug.Assert(InUse || BlockContext);

                if (_eventBuffer.EventCount == 0)
                {
                    return;
                }

                ref EventBuffer eventBuffer = ref EventBuffer;

                Span<byte> headerSpan = eventBuffer.Data.AsSpan(0, Serializer.MaxEventHeaderSize);

                int spanIndex = 1; // Skip version

                // Fill in total size in header before flushing.
                BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(spanIndex), (uint)eventBuffer.Index);
                spanIndex += sizeof(uint);

                spanIndex += sizeof(uint) + sizeof(ulong); // Skip AsyncThreadContextId and OSThreadId

                // Fill in event count in header before flushing.
                BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(spanIndex), eventBuffer.EventCount);
                spanIndex += sizeof(uint);

                spanIndex += sizeof(ulong); // Skip start timestamp

                // Fill in end timestamp in header before flushing.
                BinaryPrimitives.WriteUInt64LittleEndian(headerSpan.Slice(spanIndex), (ulong)LastEventTimestamp);

                try
                {
                    AsyncProfilerEventSource.Log.AsyncEvents(eventBuffer.Data.AsSpan(0, eventBuffer.Index));
                }
                catch
                {
                    // AsyncProfiler can't throw, ignore exception and lose buffer.
                }

                Serializer.Header(this, ref eventBuffer);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void InitializeBuffer()
            {
                try
                {
                    _eventBuffer.Data = new byte[Config.EventBufferSize];
                    Serializer.Header(this, ref _eventBuffer);
                }
                catch
                {
                    // Async Profiler can't throw, ignore exception and use empty buffer.
                    // This will cause event to drop and attempt to reallocate buffer on next event.
                    _eventBuffer.Data = Array.Empty<byte>();
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void WaitOnBlockedAsyncThreadContext(AsyncThreadContext context)
            {
                context.InUse = false;
                // Intentionally acquire and release CacheLock to wait for the flush thread
                // to finish any work that is currently synchronized on this lock.
                lock (AsyncThreadContextCache.CacheLock) { }
                context.InUse = true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static AsyncThreadContext GetAsyncThreadContext(ref Info info)
            {
                AsyncThreadContext context = Get();
                info.Context = t_asyncThreadContext;
                return context;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static AsyncThreadContext CreateAsyncThreadContext()
            {
                AsyncThreadContext context = new AsyncThreadContext();
                AsyncThreadContextCache.Add(context);
                t_asyncThreadContext = context;
                return context;
            }

            [ThreadStatic]
            private static AsyncThreadContext? t_asyncThreadContext;
        }

        internal static partial class DispatcherIds
        {
            public static ulong GetDispatcherId(Task dispatcher) => (ulong)dispatcher.Id;

            public static ulong GetDispatcherId(ref AsyncStateMachineDispatcherInfo info)
            {
                if (info.Dispatcher != null)
                {
                    return GetDispatcherId(info.Dispatcher);
                }
                return 0;
            }

#if !RUNTIME_ASYNC_SUPPORTED
            public static unsafe ulong CaptureParentDispatcherId()
            {
                AsyncStateMachineDispatcherInfo* info = AsyncStateMachineDispatcherInfo.t_current;
                if (info == null)
                {
                    return 0;
                }

                AsyncStateMachineDispatcher? parent = info->Dispatcher;
                return parent is not null ? (ulong)parent.Id : 0;
            }
#endif
        }

        internal static partial class CreateAsyncContext
        {
            public static void Create(AsyncStateMachineDispatcher dispatcher, ref Info info, ulong parentDispatcherId, ulong dispatcherId)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                EventKeywords activeEventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.ResumeStateMachineAsyncCallstackEvent(activeEventKeywords))
                    {
                        ResumeAsyncContext.Append(dispatcher, context, currentTimestamp);
                    }

                    if (IsEnabled.CreateStateMachineAsyncContextEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, parentDispatcherId, dispatcherId, AsyncEventID.CreateStateMachineAsyncContext);
                    }
                }

                AsyncThreadContext.Release(context);
            }

            public static void Create(ulong parentDispatcherId, ulong dispatcherId)
            {
                Info info = default;
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                if (IsEnabled.CreateStateMachineAsyncContextEvent(context.ActiveEventKeywords))
                {
                    EmitEvent(context, Stopwatch.GetTimestamp(), parentDispatcherId, dispatcherId, AsyncEventID.CreateStateMachineAsyncContext);
                }

                AsyncThreadContext.Release(context);
            }

            public static void Append(AsyncStateMachineDispatcher dispatcher, ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                EventKeywords activeEventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(activeEventKeywords) && IsEnabled.ResumeStateMachineAsyncCallstackEvent(activeEventKeywords))
                {
                    ResumeAsyncContext.Append(dispatcher, context, Stopwatch.GetTimestamp());
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, ulong parentDispatcherId, ulong dispatcherId, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.CreateRuntimeAsyncContext || eventID == AsyncEventID.CreateStateMachineAsyncContext);

                const int MaxPayloadLength = 2 * Serializer.MaxCompressedUInt64Size;

                ref EventBuffer eventBuffer = ref context.EventBuffer;
                if (Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, eventID, EventManifest.CreateAsyncContextPayloadLengthFieldSize, MaxPayloadLength, out int payloadLengthFieldOffset))
                {
                    Span<byte> payload = eventBuffer.Data.AsSpan(eventBuffer.Index, MaxPayloadLength);
                    int offset = 0;
                    offset += Serializer.WriteCompressedUInt64(payload.Slice(offset), parentDispatcherId);
                    offset += Serializer.WriteCompressedUInt64(payload.Slice(offset), dispatcherId);
                    eventBuffer.Index += offset;
                    Serializer.WriteAsyncEventPayloadLength(ref eventBuffer, eventID, EventManifest.CreateAsyncContextPayloadLengthFieldSize, payloadLengthFieldOffset);
                }
            }
        }

        internal static partial class ResumeAsyncContext
        {
            public static void Resume(ref AsyncStateMachineDispatcherInfo info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info.AsyncProfilerInfo);

                Resume(ref info, context, DispatcherIds.GetDispatcherId(ref info), context.ActiveEventKeywords);

                AsyncThreadContext.Release(context);
            }

            public static void Resume(ref AsyncStateMachineDispatcherInfo info, AsyncThreadContext context, ulong dispatcherId, EventKeywords activeEventKeywords)
            {
                if (SyncPoint.Check(context))
                {
                    return;
                }

                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.ResumeStateMachineAsyncContextEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, dispatcherId, AsyncEventID.ResumeStateMachineAsyncContext);
                    }

                    if (IsEnabled.ResumeStateMachineAsyncCallstackEvent(activeEventKeywords))
                    {
                        AsyncCallstack.EmitEvent(ref info, context, currentTimestamp, dispatcherId);
                    }
                }
            }

            public static void Append(AsyncStateMachineDispatcher dispatcher, AsyncThreadContext context, long currentTimestamp)
            {
                if (IsEnabled.ResumeStateMachineAsyncCallstackEvent(context.ActiveEventKeywords) && dispatcher.ContinuationChainChanged)
                {
                    AsyncCallstack.EmitEvent(dispatcher, context, dispatcher.NextContinuationForDiagnostics, currentTimestamp, AsyncEventID.AppendStateMachineAsyncCallstack, DispatcherIds.GetDispatcherId(dispatcher));
                }
            }

            public static void Append(AsyncStateMachineDispatcher dispatcher, IAsyncStateMachineBox enteringBox, AsyncThreadContext context, long currentTimestamp)
            {
                if (IsEnabled.ResumeStateMachineAsyncCallstackEvent(context.ActiveEventKeywords) && dispatcher.ReachedLastContinuation)
                {
                    if (!ReferenceEquals(enteringBox, dispatcher.LastContinuation))
                    {
                        AsyncCallstack.EmitEvent(dispatcher, context, enteringBox, currentTimestamp, AsyncEventID.AppendStateMachineAsyncCallstack, DispatcherIds.GetDispatcherId(dispatcher));
                    }
                }
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, ulong dispatcherId, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.ResumeRuntimeAsyncContext || eventID == AsyncEventID.ResumeStateMachineAsyncContext);

                const int MaxPayloadLength = Serializer.MaxCompressedUInt64Size;

                ref EventBuffer eventBuffer = ref context.EventBuffer;
                if (Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, eventID, EventManifest.ResumeAsyncContextPayloadLengthFieldSize, MaxPayloadLength, out int payloadLengthFieldOffset))
                {
                    Span<byte> payload = eventBuffer.Data.AsSpan(eventBuffer.Index, MaxPayloadLength);
                    int offset = 0;
                    offset += Serializer.WriteCompressedUInt64(payload.Slice(offset), dispatcherId);
                    eventBuffer.Index += offset;
                    Serializer.WriteAsyncEventPayloadLength(ref eventBuffer, eventID, EventManifest.ResumeAsyncContextPayloadLengthFieldSize, payloadLengthFieldOffset);
                }
            }
        }

        internal static partial class SuspendAsyncContext
        {
            public static void Suspend(AsyncStateMachineDispatcher dispatcher, ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                EventKeywords activeEventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.ResumeStateMachineAsyncCallstackEvent(activeEventKeywords))
                    {
                        ResumeAsyncContext.Append(dispatcher, context, currentTimestamp);
                    }

                    if (IsEnabled.SuspendStateMachineAsyncContextEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, AsyncEventID.SuspendStateMachineAsyncContext);
                    }
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.SuspendRuntimeAsyncContext || eventID == AsyncEventID.SuspendStateMachineAsyncContext);
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, eventID);
            }
        }

        internal static partial class CompleteAsyncContext
        {
            public static void Complete(AsyncStateMachineDispatcher dispatcher, ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                EventKeywords activeEventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.ResumeStateMachineAsyncCallstackEvent(activeEventKeywords))
                    {
                        ResumeAsyncContext.Append(dispatcher, context, currentTimestamp);
                    }

                    if (IsEnabled.CompleteStateMachineAsyncContextEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, AsyncEventID.CompleteStateMachineAsyncContext);
                    }
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.CompleteRuntimeAsyncContext || eventID == AsyncEventID.CompleteStateMachineAsyncContext);
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, eventID);
            }
        }

        internal static partial class AsyncMethodException
        {
            public static void UnwindFrames(ref AsyncStateMachineDispatcherInfo info, uint unwindedFrames)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info.AsyncProfilerInfo);

                if (IsEnabled.UnwindStateMachineAsyncExceptionEvent(context.ActiveEventKeywords))
                {
                    EmitEvent(context, Stopwatch.GetTimestamp(), unwindedFrames, AsyncEventID.UnwindStateMachineAsyncException);
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, uint unwindedFrames, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.UnwindRuntimeAsyncException ||  eventID == AsyncEventID.UnwindStateMachineAsyncException);

                // unwinded frames
                const int MaxPayloadLength = Serializer.MaxCompressedUInt32Size;

                ref EventBuffer eventBuffer = ref context.EventBuffer;
                if (Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, eventID, EventManifest.UnwindAsyncExceptionPayloadLengthFieldSize, MaxPayloadLength, out int payloadLengthFieldOffset))
                {
                    eventBuffer.Index += Serializer.WriteCompressedUInt32(eventBuffer.Data.AsSpan(eventBuffer.Index, MaxPayloadLength), unwindedFrames);
                    Serializer.WriteAsyncEventPayloadLength(ref eventBuffer, eventID, EventManifest.UnwindAsyncExceptionPayloadLengthFieldSize, payloadLengthFieldOffset);
                }
            }
        }

        internal static partial class ResumeAsyncMethod
        {
            public static void Resume(AsyncStateMachineDispatcher dispatcher, IAsyncStateMachineBox box, ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                EventKeywords activeEventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.ResumeStateMachineAsyncCallstackEvent(activeEventKeywords))
                    {
                        ResumeAsyncContext.Append(dispatcher, box, context, currentTimestamp);
                    }

                    if (IsEnabled.ResumeStateMachineAsyncMethodEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, AsyncEventID.ResumeStateMachineAsyncMethod);
                    }
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.ResumeRuntimeAsyncMethod || eventID == AsyncEventID.ResumeStateMachineAsyncMethod);
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, eventID);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.ResumeRuntimeAsyncMethod || eventID == AsyncEventID.ResumeStateMachineAsyncMethod);
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, eventID);
            }
        }

        internal static partial class CompleteAsyncMethod
        {
            public static void Complete(ref AsyncStateMachineDispatcherInfo info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info.AsyncProfilerInfo);

                if (IsEnabled.CompleteStateMachineAsyncMethodEvent(context.ActiveEventKeywords))
                {
                    EmitEvent(context, AsyncEventID.CompleteStateMachineAsyncMethod);
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, AsyncEventID eventID)
            {
                Debug.Assert(eventID == AsyncEventID.CompleteRuntimeAsyncMethod || eventID == AsyncEventID.CompleteStateMachineAsyncMethod);
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, eventID);
            }
        }

        internal static partial class ContinuationWrapper
        {
            /// <summary>
            /// Number of distinct wrapper methods. The wrapper index rotates modulo this value.
            /// </summary>
            public const byte COUNT = 32;
            public const byte COUNT_MASK = COUNT - 1;

#if !RUNTIME_ASYNC_SUPPORTED
            public static void InitInfo(ref Info info)
            {
                info.ContinuationTable = ref Unsafe.NullRef<nint>();
                info.ContinuationIndex = 0;
            }
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IncrementIndex(ref Info info)
            {
                info.ContinuationIndex++;
                if ((info.ContinuationIndex & COUNT_MASK) == 0)
                {
                    ResetIndex(ref info);
                }
            }

            public static void UnwindIndex(ref Info info, uint unwindedFrames)
            {
                uint oldIndex = info.ContinuationIndex;
                info.ContinuationIndex += unwindedFrames;

                if ((oldIndex & ~COUNT_MASK) != (info.ContinuationIndex & ~COUNT_MASK))
                {
                    ResetIndex(ref info);
                }
            }

            private static void ResetIndex(ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                if (IsEnabled.AnyAsyncEvents(context.ActiveEventKeywords))
                {
                    EmitEvent(context);
                }

                AsyncThreadContext.Release(context);
            }

            private static void EmitEvent(AsyncThreadContext context)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResetAsyncContinuationWrapperIndex);
            }
        }

        internal static partial class SyncPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Check()
            {
                AsyncThreadContext context = AsyncThreadContext.Get();
                if (Config.Changed(context))
                {
                    CheckSlow(context);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Check(ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Get(ref info);
                if (Config.Changed(context))
                {
                    CheckSlow(context);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Check(AsyncThreadContext context)
            {
                if (Config.Changed(context))
                {
                    ResetContext(context);
                    return true;
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void CheckSlow(AsyncThreadContext context)
            {
                AsyncThreadContext.Acquire(context);
                ResetContext(context);
                AsyncThreadContext.Release(context);
            }

            private static void ResetContext(AsyncThreadContext context)
            {
                context.Flush();

                context.ConfigRevision = Config.Revision;
                context.ActiveEventKeywords = Config.ActiveEventKeywords;

                if (IsEnabled.AnyAsyncEvents(context.ActiveEventKeywords))
                {
                    Config.EmitAsyncProfilerMetadataIfNeeded(context);
                    EmitEvent(context);
                }

                ResumeAsyncCallstacks(context);
            }

#if !RUNTIME_ASYNC_SUPPORTED
            private static unsafe void ResumeAsyncCallstacks(AsyncThreadContext context)
            {
                ResumeStateMachineAsyncCallstacks(context);
            }

            private static unsafe void ResumeStateMachineAsyncCallstacks(AsyncThreadContext context)
            {
                //Write recursively all the resume async callstack events.
                AsyncStateMachineDispatcherInfo* info = AsyncStateMachineDispatcherInfo.t_current;
                if (info != null)
                {
                    ResumeStateMachineAsyncCallstacks(info, context);
                }
            }

            private static unsafe void ResumeStateMachineAsyncCallstacks(AsyncStateMachineDispatcherInfo* info, AsyncThreadContext context)
            {
                if (info != null)
                {
                    ResumeStateMachineAsyncCallstacks(info->Next, context);
                    ResumeAsyncContext.Resume(ref *info, context, DispatcherIds.GetDispatcherId(ref *info), Config.ActiveEventKeywords);
                }
            }
#endif

            private static void EmitEvent(AsyncThreadContext context)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResetAsyncThreadContext);
            }
        }

        private static class IsEnabled
        {
            public static bool CreateRuntimeAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.CreateRuntimeAsyncContext) != 0;
            public static bool ResumeRuntimeAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.ResumeRuntimeAsyncContext) != 0;
            public static bool SuspendRuntimeAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.SuspendRuntimeAsyncContext) != 0;
            public static bool CompleteRuntimeAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.CompleteRuntimeAsyncContext) != 0;
            public static bool UnwindRuntimeAsyncExceptionEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.UnwindRuntimeAsyncException) != 0;
            public static bool CreateRuntimeAsyncCallstackEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.CreateRuntimeAsyncCallstack) != 0;
            public static bool ResumeRuntimeAsyncCallstackEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.ResumeRuntimeAsyncCallstack) != 0;
            public static bool SuspendRuntimeAsyncCallstackEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.SuspendRuntimeAsyncCallstack) != 0;
            public static bool ResumeRuntimeAsyncMethodEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.ResumeRuntimeAsyncMethod) != 0;
            public static bool CompleteRuntimeAsyncMethodEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.CompleteRuntimeAsyncMethod) != 0;
            public static bool CreateStateMachineAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.CreateStateMachineAsyncContext) != 0;
            public static bool ResumeStateMachineAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.ResumeStateMachineAsyncContext) != 0;
            public static bool SuspendStateMachineAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.SuspendStateMachineAsyncContext) != 0;
            public static bool CompleteStateMachineAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.CompleteStateMachineAsyncContext) != 0;
            public static bool UnwindStateMachineAsyncExceptionEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.UnwindStateMachineAsyncException) != 0;
            public static bool ResumeStateMachineAsyncCallstackEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.ResumeStateMachineAsyncCallstack) != 0;
            public static bool ResumeStateMachineAsyncMethodEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.ResumeStateMachineAsyncMethod) != 0;
            public static bool CompleteStateMachineAsyncMethodEvent(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.Keywords.CompleteStateMachineAsyncMethod) != 0;
            public static bool AnyAsyncEvents(EventKeywords eventKeywords) => (eventKeywords & AsyncProfilerEventSource.AsyncEventKeywords) != 0;
        }

        private static class AsyncThreadContextCache
        {
            public static Lock CacheLock { get; private set; } = new Lock();

            public static void Add(AsyncThreadContext context)
            {
                AsyncThreadContextHolder contextHolder = new AsyncThreadContextHolder(context, Thread.CurrentThread);
                lock (CacheLock)
                {
                    s_cache.Add(contextHolder);
                }
            }

            public static void Flush(bool force)
            {
                lock (CacheLock)
                {
                    FlushCore(force);
                }
            }

            public static void EnableFlushTimer()
            {
                lock (CacheLock)
                {
                    s_flushTimer ??= new Timer(PeriodicFlush, null, Timeout.Infinite, Timeout.Infinite, false);
                    s_flushTimer.Change(AsyncThreadContextCacheFlushTimerIntervalMs, Timeout.Infinite);
                }
            }

            public static void DisableFlushTimer()
            {
                lock (CacheLock)
                {
                    s_flushTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            public static void EnableCleanupTimer()
            {
                lock (CacheLock)
                {
                    s_cleanupTimer ??= new Timer(Cleanup, null, Timeout.Infinite, Timeout.Infinite, false);
                    s_cleanupTimer?.Change(AsyncThreadContextCacheCleanupTimerIntervalMs, Timeout.Infinite);
                }
            }

            public static void DisableCleanupTimer()
            {
                lock (CacheLock)
                {
                    s_cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }

            private static void Cleanup(object? state)
            {
                _ = state;

                lock (CacheLock)
                {
                    FlushCore(true);

                    if (s_cache.Count > 0)
                    {
                        // Restart cleanup timer.
                        s_cleanupTimer?.Change(AsyncThreadContextCacheCleanupTimerIntervalMs, Timeout.Infinite);
                    }
                }
            }

            private static void PeriodicFlush(object? state)
            {
                _ = state;

                lock (CacheLock)
                {
                    FlushCore(false);

                    if (IsEnabled.AnyAsyncEvents(Config.ActiveEventKeywords))
                    {
                        // Restart flush timer.
                        s_flushTimer?.Change(AsyncThreadContextCacheFlushTimerIntervalMs, Timeout.Infinite);
                    }
                    else
                    {
                        // Start cleanup timer.
                        s_cleanupTimer?.Change(AsyncThreadContextCacheCleanupTimerIntervalMs, Timeout.Infinite);
                    }
                }
            }

            private static void FlushCore(bool force)
            {
                // Make sure all dead threads are flushed and removed from the cache.
                for (int i = s_cache.Count - 1; i >= 0; i--)
                {
                    AsyncThreadContextHolder contextHolder = s_cache[i];
                    if (!contextHolder.OwnerThread.TryGetTarget(out Thread? target) || !target.IsAlive)
                    {
                        // Thread is dead, flush its buffer and remove from cache.
                        AsyncThreadContext context = contextHolder.Context;

                        Debug.Assert(!context.InUse);
                        context.InUse = true;

                        context.Flush();

                        context.Reclaim();
                        s_cache.RemoveAt(i);

                        context.InUse = false;
                    }
                }

                long frequency = Stopwatch.Frequency;

                // Look at live threads, only flush if forced or contexts that have been idle for 250 milliseconds.
                long idleWriteTimestamp = Stopwatch.GetTimestamp() - (frequency / 4);

                // Additionally, reclaim buffers for contexts that have been idle for 30 seconds to avoid keeping
                // large buffers around indefinitely for threads that are no longer running async code.
                long idleReclaimBufferTimestamp = Stopwatch.GetTimestamp() - frequency * 30;

                // Spin wait timeout, 100 milliseconds.
                long spinWaitTimeout = frequency / 10;

                foreach (AsyncThreadContextHolder contextHolder in s_cache)
                {
                    AsyncThreadContext context = contextHolder.Context;

                    // Read LastEventTimestamp without atomics, could cause teared reads but not critical.
                    long lastEventWriteTimestamp = context.LastEventTimestamp;
                    if (force || lastEventWriteTimestamp < idleWriteTimestamp)
                    {
                        context.BlockContext = true;
                        SpinWait sw = default;
                        long timeout = Stopwatch.GetTimestamp() + spinWaitTimeout;
                        while (context.InUse)
                        {
                            sw.SpinOnce();
                            if (Stopwatch.GetTimestamp() > timeout)
                            {
                                // AsyncThreadContext has been busy for too long, skip flushing this time.
                                // NOTE, this should not happen under normal conditions, contexts are only
                                // held InUse for a very short time writing events. If this do happen then
                                // then write probably triggered a flush or thread have been preempted for
                                // a long time while holding the context. Either way, skipping flush this time
                                // should be ok, as the next flush will pick it up and flushing is best effort.
                                break;
                            }
                        }

                        if (!context.InUse)
                        {
                            context.Flush();

                            if (force || lastEventWriteTimestamp < idleReclaimBufferTimestamp)
                            {
                                context.Reclaim();
                            }
                        }

                        context.BlockContext = false;
                    }
                }

                Config.EmitSyncClockEventIfNeeded();
            }

            private sealed class AsyncThreadContextHolder
            {
                public AsyncThreadContextHolder(AsyncThreadContext context, Thread ownerThread)
                {
                    Context = context;
                    OwnerThread = new WeakReference<Thread>(ownerThread);
                }

                public readonly AsyncThreadContext Context;
                public readonly WeakReference<Thread> OwnerThread;
            }

            private const int AsyncThreadContextCacheFlushTimerIntervalMs = 1000;
            private static Timer? s_flushTimer;

            private const int AsyncThreadContextCacheCleanupTimerIntervalMs = 30000;
            private static Timer? s_cleanupTimer;

            private static List<AsyncThreadContextHolder> s_cache = new List<AsyncThreadContextHolder>();
        }

        private static partial class AsyncCallstack
        {
            private const int MaxStateMachineAsyncMethodFrameSize = Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt32Size;

            private const int AsyncStateMachineCompletedState = -2;

            private interface ICaptureAsyncCallstack
            {
                bool Capture(byte[] buffer, ref int index, out byte count);

                static abstract int MaxAsyncMethodFrameSize {  get; }
            }

            private ref struct CaptureStateMachineAsyncCallstackState : ICaptureAsyncCallstack
            {
                public object? Continuation;
                public object? LastContinuation;
                public ulong LastMethodId;
                public byte Count;

                public bool Capture(byte[] buffer, ref int index, out byte count)
                {
                    bool result = CaptureStateMachineAsyncCallstack(buffer, ref index, ref this);
                    count = Count;
                    return result;
                }

                public static int MaxAsyncMethodFrameSize => MaxStateMachineAsyncMethodFrameSize;
            }

            private static bool IsTruncated(in CaptureStateMachineAsyncCallstackState state) =>
                state.Count == byte.MaxValue && state.Continuation != null;

            public static void EmitEvent(ref AsyncStateMachineDispatcherInfo info, AsyncThreadContext context, long currentTimestamp, ulong dispatcherId)
            {
                if (info.Dispatcher == null)
                {
                    return;
                }

                IAsyncStateMachineBox? box = ResolveAsyncStateMachineBox(info.AsyncProfilerInfo.CurrentContinuation);

                CaptureStateMachineAsyncCallstackState state = default;
                state.Continuation = box;

                EmitAsyncCallstack(context, currentTimestamp, currentTimestamp - context.LastEventTimestamp, AsyncEventID.ResumeStateMachineAsyncCallstack, 0, dispatcherId, ref state);

                info.Dispatcher.LastContinuation = IsTruncated(in state) ? null : ResolveAsyncStateMachineBox(state.LastContinuation);
                info.Dispatcher.ReachedLastContinuation = false;
            }

            public static void EmitEvent(AsyncStateMachineDispatcher dispatcher, AsyncThreadContext context, object? continuation, long currentTimestamp, AsyncEventID eventID, ulong dispatcherId)
            {
                Debug.Assert(eventID == AsyncEventID.ResumeStateMachineAsyncCallstack || eventID == AsyncEventID.AppendStateMachineAsyncCallstack);

                if (continuation != null)
                {
                    IAsyncStateMachineBox? box = ResolveAsyncStateMachineBox(continuation);
                    if (box != null)
                    {
                        CaptureStateMachineAsyncCallstackState state = default;
                        state.Continuation = box;

                        EmitAsyncCallstack(context, currentTimestamp, currentTimestamp - context.LastEventTimestamp, eventID, 0, dispatcherId, ref state);

                        dispatcher.LastContinuation = IsTruncated(in state) ? null : ResolveAsyncStateMachineBox(state.LastContinuation);
                    }
                    else
                    {
                        dispatcher.LastContinuation = null;
                    }

                    dispatcher.ReachedLastContinuation = false;
                }
            }

            private static bool CaptureStateMachineAsyncCallstack(byte[] buffer, ref int index, ref CaptureStateMachineAsyncCallstackState state)
            {
                if (index > buffer.Length)
                {
                    return false;
                }

                if (state.Continuation == null)
                {
                    return true;
                }

                int remainingFrames = (buffer.Length - index) / MaxStateMachineAsyncMethodFrameSize;
                if (remainingFrames == 0)
                {
                    return false;
                }

                byte maxAsyncCallstackFrames = (byte)Math.Min(byte.MaxValue, state.Count + remainingFrames);

                Span<byte> callstackSpan = buffer.AsSpan(index);
                int callstackSpanIndex = 0;
                ulong previousMethodId;
                ulong currentMethodId = state.LastMethodId;

                while (state.Count < maxAsyncCallstackFrames && state.Continuation != null)
                {
                    if (state.Continuation is AsyncStateMachineDispatcher)
                    {
                        state.Continuation = null;
                        break;
                    }

                    state.LastContinuation = state.Continuation;

                    if (!GetFrameDiagnosticsData(state.Continuation, out ulong frameMethodId, out int frameState, out object? nextContinuation))
                    {
                        state.Continuation = nextContinuation;
                        continue;
                    }

                    if (frameState == AsyncStateMachineCompletedState)
                    {
                        state.Continuation = nextContinuation;
                        continue;
                    }

                    previousMethodId = currentMethodId;
                    currentMethodId = frameMethodId;

                    if (state.Count == 0)
                    {
                        callstackSpanIndex += Serializer.WriteCompressedUInt64(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedUInt64Size), currentMethodId);
                    }
                    else
                    {
                        callstackSpanIndex += Serializer.WriteCompressedInt64(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedInt64Size), (long)(currentMethodId - previousMethodId));
                    }

                    callstackSpanIndex += Serializer.WriteCompressedInt32(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedInt32Size), frameState);

                    state.Count++;
                    state.Continuation = nextContinuation;
                }

                state.LastMethodId = currentMethodId;
                index += callstackSpanIndex;

                return state.Continuation == null || state.Count == byte.MaxValue;
            }

            private static bool GetFrameDiagnosticsData(object? continuationObject, out ulong methodId, out int state, out object? nextContinuation)
            {
                IAsyncStateMachineBox? box = ResolveAsyncStateMachineBox(continuationObject);
                if (box != null)
                {
                    return box.GetDiagnosticData(out methodId, out state, out nextContinuation);
                }

                methodId = 0;
                state = -1;
                nextContinuation = null;

                return false;
            }

            private static IAsyncStateMachineBox? ResolveAsyncStateMachineBox(object? continuationObject)
            {
                if (continuationObject == null)
                {
                    return null;
                }

                if (continuationObject is IAsyncStateMachineBox box)
                {
                    return box;
                }

                if (continuationObject is Action action)
                {
                    return AsyncMethodBuilderCore.TryGetStateMachineBox(action);
                }

                if (continuationObject is List<object?> list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        IAsyncStateMachineBox? resolved = ResolveAsyncStateMachineBox(list[i]);
                        if (resolved is not null)
                        {
                            return resolved;
                        }
                    }
                }

                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void EmitAsyncCallstack<T>(AsyncThreadContext context, long currentTimestamp, long delta, AsyncEventID eventID, ulong parentDispatcherId, ulong dispatcherId, ref T captureCallstack)
                where T : ICaptureAsyncCallstack, allows ref struct
            {
                EmitAsyncCallstack(context, currentTimestamp, delta, eventID, parentDispatcherId, dispatcherId, 0, ref captureCallstack);
            }

            private static void EmitAsyncCallstack<T>(AsyncThreadContext context, long currentTimestamp, long delta, AsyncEventID eventID, ulong parentDispatcherId, ulong dispatcherId, byte continuationIndex, ref T captureCallstack)
                where T : ICaptureAsyncCallstack, allows ref struct
            {
                Debug.Assert(eventID == AsyncEventID.CreateRuntimeAsyncCallstack ||
                    eventID == AsyncEventID.ResumeRuntimeAsyncCallstack ||
                    eventID == AsyncEventID.SuspendRuntimeAsyncCallstack ||
                    eventID == AsyncEventID.ResumeStateMachineAsyncCallstack ||
                    eventID == AsyncEventID.AppendStateMachineAsyncCallstack);

                ref EventBuffer eventBuffer = ref context.EventBuffer;

                // Static callstack payload: callstackId (1) + continuationIndex (1) + frameCount (1) + parentDispatcherId + dispatcherId (max 10 bytes compressed each).
                const int MaxPayloadLength = sizeof(byte) + sizeof(byte) + sizeof(byte) + 2 * Serializer.MaxCompressedUInt64Size;

                if (Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, EventManifest.CallstackPayloadLengthFieldSize, MaxPayloadLength, out int payloadLengthFieldOffset, out Serializer.AsyncEventHeaderRollbackData rollbackData))
                {
                    int frameCountOffset = CallstackHeader(ref eventBuffer, eventID, parentDispatcherId, dispatcherId, continuationIndex, 0);

                    byte[] buffer = eventBuffer.Data;
                    int startIndex = eventBuffer.Index;
                    int currentIndex = startIndex;

                    if (!captureCallstack.Capture(buffer, ref currentIndex, out byte count))
                    {
                        int maxReEmitOverhead = Serializer.MaxEventHeaderSize + Serializer.MaxAsyncEventHeaderSize + (byte)EventManifest.CallstackPayloadLengthFieldSize + MaxPayloadLength;
                        int maxCallstackBytes = Math.Min(byte.MaxValue * T.MaxAsyncMethodFrameSize, eventBuffer.Data.Length - maxReEmitOverhead);

                        byte[]? rentedArray = RentArray(maxCallstackBytes);
                        if (rentedArray != null)
                        {
                            int length = currentIndex - startIndex;
                            int index = length;

                            Buffer.BlockCopy(buffer, startIndex, rentedArray, 0, length);
                            captureCallstack.Capture(rentedArray, ref index, out count);

                            // Rollback async event header before flushing.
                            Serializer.RollbackAsyncEventHeader(context, in rollbackData);
                            context.Flush();

                            // Write the callstack again.
                            if (Serializer.AsyncEventHeader(context, ref eventBuffer, context.LastEventTimestamp, 0, eventID, EventManifest.CallstackPayloadLengthFieldSize, MaxPayloadLength + index, out payloadLengthFieldOffset))
                            {
                                CallstackHeader(ref eventBuffer, eventID, parentDispatcherId, dispatcherId, continuationIndex, count);
                                CallstackData(ref eventBuffer, rentedArray, index);
                                Serializer.WriteAsyncEventPayloadLength(ref eventBuffer, eventID, EventManifest.CallstackPayloadLengthFieldSize, payloadLengthFieldOffset);
                            }

                            ArrayPool<byte>.Shared.Return(rentedArray);
                        }
                        else
                        {
                            // Rollback async event header since we can't write the callstack.
                            Serializer.RollbackAsyncEventHeader(context, in rollbackData);
                        }
                    }
                    else
                    {
                        // Patch frame count in the event buffer using the offset from CallstackHeader.
                        eventBuffer.Data[frameCountOffset] = count;
                        eventBuffer.Index += currentIndex - startIndex;
                        Serializer.WriteAsyncEventPayloadLength(ref eventBuffer, eventID, EventManifest.CallstackPayloadLengthFieldSize, payloadLengthFieldOffset);
                    }
                }
            }

            private static int CallstackHeader(ref EventBuffer eventBuffer, AsyncEventID eventID, ulong parentDispatcherId, ulong dispatcherId, byte continuationIndex, byte callstackFrameCount)
            {
                // Callstack header layout: callstackId (1 byte, reserved) + continuationIndex (1 byte) + frameCount (1 byte) + parentDispatcherId + dispatcherId (max 10 bytes compressed each).
                const int MaxCallstackHeaderSize = sizeof(byte) + sizeof(byte) + sizeof(byte) + 2 * Serializer.MaxCompressedUInt64Size;

                ref int index = ref eventBuffer.Index;

                Span<byte> callstackHeaderSpan = eventBuffer.Data.AsSpan(index, MaxCallstackHeaderSize);
                int spanIndex = 0;

                callstackHeaderSpan[spanIndex++] = 0; // Reserved callstack ID for future callstack interning.
                callstackHeaderSpan[spanIndex++] = continuationIndex;

                int frameCountOffset = index + spanIndex;
                callstackHeaderSpan[spanIndex++] = callstackFrameCount;

                if (eventID == AsyncEventID.CreateRuntimeAsyncCallstack)
                {
                    spanIndex += Serializer.WriteCompressedUInt64(callstackHeaderSpan.Slice(spanIndex), parentDispatcherId);
                }

                spanIndex += Serializer.WriteCompressedUInt64(callstackHeaderSpan.Slice(spanIndex), dispatcherId);
                eventBuffer.Index += spanIndex;

                return frameCountOffset;
            }

            private static void CallstackData(ref EventBuffer eventBuffer, byte[] callstackData, int callstackDataByteCount)
            {
                ref int index = ref eventBuffer.Index;
                Buffer.BlockCopy(callstackData, 0, eventBuffer.Data, index, callstackDataByteCount);
                index += callstackDataByteCount;
            }

            private static byte[]? RentArray(int minimumLength)
            {
                byte[]? rentedArray = null;
                try
                {
                    rentedArray = ArrayPool<byte>.Shared.Rent(minimumLength);
                }
                catch
                {
                    //AsyncProfiler can't throw, return null if renting fails.
                }

                return rentedArray;
            }
        }
    }
}
