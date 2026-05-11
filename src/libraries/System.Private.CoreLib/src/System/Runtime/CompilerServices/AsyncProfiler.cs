// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Runtime.CompilerServices.AsyncProfilerEventSource;
using Serializer = System.Runtime.CompilerServices.AsyncProfiler.EventBuffer.Serializer;

namespace System.Runtime.CompilerServices
{
    internal static partial class AsyncProfiler
    {
        [Flags]
        public enum AsyncCallstackType : byte
        {
            Compiler = 0x1,
            Runtime = 0x2,
            Cached = 0x80
        }

        internal enum AsyncEventID : byte
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
            AsyncProfilerSyncClock = 14
        }

        internal ref struct Info
        {
            public object? Context;
            public ref nint ContinuationTable;
            public uint ContinuationIndex;
        }

        internal static void InitInfo(ref Info info)
        {
            info.Context = null;
            info.ContinuationIndex = 0;
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
                            long[] wrapperIPs = ContinuationWrapper.GetContinuationWrapperIPs();

                            // Metadata payload:
                            // [qpcFrequency (compressed uint64)]
                            // [qpcSync (compressed uint64)]
                            // [utcSync (compressed uint64)]
                            // [eventBufferSize (compressed uint32)]
                            // [wrapperCount byte]
                            // [wrapperIP0 (compressed uint64)] ... [wrapperIPn (compressed uint64)]
                            const int MaxStaticEventPayloadSize = Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt32Size + 1;
                            int maxDynamicEventPayloadSize = wrapperIPs.Length * Serializer.MaxCompressedUInt64Size;

                            ref EventBuffer eventBuffer = ref context.EventBuffer;
                            if (Serializer.AsyncEventHeader(context, ref eventBuffer, AsyncEventID.AsyncProfilerMetadata, MaxStaticEventPayloadSize + maxDynamicEventPayloadSize))
                            {
                                SyncClock(out long utcTimeSync, out long qpcSync);

                                Span<byte> payloadSpan = eventBuffer.Data.AsSpan(eventBuffer.Index, MaxStaticEventPayloadSize + maxDynamicEventPayloadSize);
                                int payloadSpanIndex = 0;

                                payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)Stopwatch.Frequency);
                                payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)qpcSync);
                                payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)utcTimeSync);
                                payloadSpanIndex += Serializer.WriteCompressedUInt32(payloadSpan.Slice(payloadSpanIndex), EventBufferSize);

                                payloadSpan[payloadSpanIndex++] = (byte)wrapperIPs.Length;

                                for (int i = 0; i < wrapperIPs.Length; i++)
                                {
                                    payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)wrapperIPs[i]);
                                }

                                eventBuffer.Index += payloadSpanIndex;

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
                    const int MaxEventPayloadSize = Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt64Size;

                    ref EventBuffer eventBuffer = ref transientContext.EventBuffer;
                    if (Serializer.AsyncEventHeader(transientContext, ref eventBuffer, AsyncEventID.AsyncProfilerSyncClock, MaxEventPayloadSize))
                    {
                        SyncClock(out long utcTimeSync, out long qpcSync);

                        Span<byte> payloadSpan = eventBuffer.Data.AsSpan(eventBuffer.Index, MaxEventPayloadSize);
                        int payloadSpanIndex = 0;

                        payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)qpcSync);
                        payloadSpanIndex += Serializer.WriteCompressedUInt64(payloadSpan.Slice(payloadSpanIndex), (ulong)utcTimeSync);

                        eventBuffer.Index += payloadSpanIndex;

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
                flags |= IsEnabled.CreateAsyncContextEvent(ActiveEventKeywords) || IsEnabled.CreateAsyncCallstackEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CreateAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.ResumeAsyncContextEvent(ActiveEventKeywords) || IsEnabled.ResumeAsyncCallstackEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.ResumeAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.SuspendAsyncContextEvent(ActiveEventKeywords) || IsEnabled.SuspendAsyncCallstackEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.SuspendAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CompleteAsyncContextEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CompleteAsyncContext : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.UnwindAsyncExceptionEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.UnwindAsyncException : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.ResumeAsyncMethodEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.ResumeAsyncMethod : AsyncInstrumentation.Flags.Disabled;
                flags |= IsEnabled.CompleteAsyncMethodEvent(ActiveEventKeywords) ? AsyncInstrumentation.Flags.CompleteAsyncMethod : AsyncInstrumentation.Flags.Disabled;

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

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, AsyncEventID eventID, int maxEventPayloadSize)
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    long delta = currentTimestamp - context.LastEventTimestamp;
                    return AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, maxEventPayloadSize);
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, AsyncEventID eventID, int maxEventPayloadSize)
                {
                    long delta = currentTimestamp - context.LastEventTimestamp;
                    return AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, maxEventPayloadSize);
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, long delta, AsyncEventID eventID, int maxEventPayloadSize, out AsyncEventHeaderRollbackData rollbackData)
                {
                    byte[] buffer = eventBuffer.Data;
                    int index = eventBuffer.Index;
                    long previousTimestamp = context.LastEventTimestamp;

                    if ((index + MaxAsyncEventHeaderSize + maxEventPayloadSize) <= buffer.Length && delta >= 0)
                    {
                        context.LastEventTimestamp = currentTimestamp;
                    }
                    else
                    {
                        // Event is too big for buffer, drop it.
                        if (MaxAsyncEventHeaderSize + maxEventPayloadSize > buffer.Length)
                        {
                            rollbackData = default;
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

                    headerSpan[headerSpanIndex++] = (byte)eventID; // eventID
                    headerSpanIndex += WriteCompressedUInt64(headerSpan.Slice(headerSpanIndex), (ulong)delta); // Timestamp delta from last event

                    eventBuffer.Index += headerSpanIndex;
                    eventBuffer.EventCount++;

                    return true;
                }

                public static bool AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, long delta, AsyncEventID eventID, int maxEventPayloadSize)
                {
                    byte[] buffer = eventBuffer.Data;
                    int index = eventBuffer.Index;

                    if ((index + MaxAsyncEventHeaderSize + maxEventPayloadSize) <= buffer.Length && delta >= 0)
                    {
                        context.LastEventTimestamp = currentTimestamp;
                    }
                    else
                    {
                        // Event is too big for buffer, drop it.
                        if (MaxAsyncEventHeaderSize + maxEventPayloadSize > buffer.Length)
                        {
                            return false;
                        }

                        context.Flush();

                        delta = 0;
                        index = eventBuffer.Index;
                    }

                    Span<byte> headerSpan = buffer.AsSpan(index, MaxAsyncEventHeaderSize);
                    int headerSpanIndex = 0;

                    headerSpan[headerSpanIndex++] = (byte)eventID; // eventID
                    headerSpanIndex += WriteCompressedUInt64(headerSpan.Slice(headerSpanIndex), (ulong)delta); // Timestamp delta from last event

                    eventBuffer.Index += headerSpanIndex;
                    eventBuffer.EventCount++;

                    return true;
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

                context.InUse = true;
                if (context.BlockContext)
                {
                    WaitOnBlockedAsyncThreadContext(context);
                }

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
                    Log.AsyncEvents(eventBuffer.Data.AsSpan(0, eventBuffer.Index));
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

        internal static partial class CreateAsyncContext
        {
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, ulong id)
            {
                const int MaxEventPayloadSize = Serializer.MaxCompressedUInt64Size;

                ref EventBuffer eventBuffer = ref context.EventBuffer;
                if (Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, AsyncEventID.CreateAsyncContext, MaxEventPayloadSize))
                {
                    eventBuffer.Index += Serializer.WriteCompressedUInt64(eventBuffer.Data.AsSpan(eventBuffer.Index, MaxEventPayloadSize), id);
                }
            }
        }

        internal static partial class ResumeAsyncContext
        {
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, ulong id)
            {
                const int MaxEventPayloadSize = Serializer.MaxCompressedUInt64Size;

                ref EventBuffer eventBuffer = ref context.EventBuffer;
                if (Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, AsyncEventID.ResumeAsyncContext, MaxEventPayloadSize))
                {
                    eventBuffer.Index += Serializer.WriteCompressedUInt64(eventBuffer.Data.AsSpan(eventBuffer.Index, MaxEventPayloadSize), id);
                }
            }
        }

        internal static partial class SuspendAsyncContext
        {
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, AsyncEventID.SuspendAsyncContext, 0);
            }
        }

        internal static partial class CompleteAsyncContext
        {
            public static void Complete(ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                if (IsEnabled.CompleteAsyncContextEvent(context.ActiveEventKeywords))
                {
                    EmitEvent(context, Stopwatch.GetTimestamp());
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, AsyncEventID.CompleteAsyncContext, 0);
            }
        }

        internal static partial class AsyncMethodException
        {
            public static void Unhandled(ref Info info, uint unwindedFrames)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                EventKeywords activeEventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.UnwindAsyncExceptionEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, unwindedFrames);
                    }

                    if (IsEnabled.CompleteAsyncContextEvent(activeEventKeywords))
                    {
                        CompleteAsyncContext.EmitEvent(context, currentTimestamp);
                    }
                }

                AsyncThreadContext.Release(context);
            }

            public static void Handled(ref Info info, uint unwindedFrames)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);
                if (IsEnabled.UnwindAsyncExceptionEvent(context.ActiveEventKeywords))
                {
                    EmitEvent(context, Stopwatch.GetTimestamp(), unwindedFrames);
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, uint unwindedFrames)
            {
                // unwinded frames
                const int MaxEventPayloadSize = Serializer.MaxCompressedUInt32Size;

                ref EventBuffer eventBuffer = ref context.EventBuffer;
                if (Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, AsyncEventID.UnwindAsyncException, MaxEventPayloadSize))
                {
                    eventBuffer.Index += Serializer.WriteCompressedUInt32(eventBuffer.Data.AsSpan(eventBuffer.Index, MaxEventPayloadSize), unwindedFrames);
                }
            }
        }

        internal static partial class ResumeAsyncMethod
        {
            public static void Resume(ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);
                if (IsEnabled.ResumeAsyncMethodEvent(context.ActiveEventKeywords))
                {
                    EmitEvent(context);
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResumeAsyncMethod, 0);
            }
        }

        internal static partial class CompleteAsyncMethod
        {
            public static void Complete(ref Info info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);
                if (IsEnabled.CompleteAsyncMethodEvent(context.ActiveEventKeywords))
                {
                    EmitEvent(context);
                }

                AsyncThreadContext.Release(context);
            }

            public static void EmitEvent(AsyncThreadContext context)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.CompleteAsyncMethod, 0);
            }
        }

        internal static partial class ContinuationWrapper
        {
            public const byte COUNT = 32;
            public const byte COUNT_MASK = COUNT - 1;

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

                SyncPoint.Check(context);
                if (IsEnabled.AnyAsyncEvents(context.ActiveEventKeywords))
                {
                    EmitEvent(context);
                }

                AsyncThreadContext.Release(context);
            }

            private static void EmitEvent(AsyncThreadContext context)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResetAsyncContinuationWrapperIndex, 0);
            }
        }

        private static partial class SyncPoint
        {
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

            private static void EmitEvent(AsyncThreadContext context)
            {
                Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResetAsyncThreadContext, 0);
            }
        }

        private static class IsEnabled
        {
            public static bool CreateAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.CreateAsyncContext) != 0;
            public static bool ResumeAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.ResumeAsyncContext) != 0;
            public static bool SuspendAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.SuspendAsyncContext) != 0;
            public static bool CompleteAsyncContextEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.CompleteAsyncContext) != 0;
            public static bool UnwindAsyncExceptionEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.UnwindAsyncException) != 0;
            public static bool CreateAsyncCallstackEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.CreateAsyncCallstack) != 0;
            public static bool ResumeAsyncCallstackEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.ResumeAsyncCallstack) != 0;
            public static bool SuspendAsyncCallstackEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.SuspendAsyncCallstack) != 0;
            public static bool ResumeAsyncMethodEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.ResumeAsyncMethod) != 0;
            public static bool CompleteAsyncMethodEvent(EventKeywords eventKeywords) => (eventKeywords & Keywords.CompleteAsyncMethod) != 0;
            public static bool AnyAsyncEvents(EventKeywords eventKeywords) => (eventKeywords & AsyncEventKeywords) != 0;
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
    }
}
