// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Runtime.CompilerServices.AsyncProfilerBufferedEventSource;

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
            AsyncProfilerMetadata = 13
        }

        internal ref struct Info
        {
            public object? Context;
            public ref nint ContinuationTable;
            public uint ContinuationIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InitInfo(ref Info info)
        {
            info.Context = null;
            info.ContinuationIndex = 0;
            ContinuationWrapper.InitInfo(ref info);
        }

        internal static partial class Config
        {
            public static readonly Lock ConfigLock = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Changed(AsyncThreadContext context) => context.ConfigRevision != Revision;

            public static void Update(EventLevel logLevel, EventKeywords eventKeywords)
            {
                lock (ConfigLock)
                {
                    Revision++;

                    ActiveEventKeywords = 0;
                    if (logLevel >= EventLevel.Informational)
                    {
                        ActiveEventKeywords = eventKeywords;
                    }

                    string? eventBufferSizeEnv = System.Environment.GetEnvironmentVariable("DOTNET_AsyncProfilerBufferedEventSource_EventBufferSize");
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
                            // [qpcFrequency (8 bytes)]
                            // [qpcSync (8 bytes)]
                            // [utcSync (8 bytes)]
                            // [eventBufferSize (4 bytes)]
                            // [wrapperCount byte]
                            // [wrapperIP0 (8 bytes)] ... [wrapperIPn (8 bytes)]
                            int maxEventPayloadSize = sizeof(long) + sizeof(long) + sizeof(long) + sizeof(uint) + 1 + (wrapperIPs.Length * sizeof(long));

                            ref EventBuffer eventBuffer = ref context.EventBuffer;
                            if (EventBuffer.Serializer.AsyncEventHeader(context, ref eventBuffer, AsyncEventID.AsyncProfilerMetadata, maxEventPayloadSize) >= 0)
                            {
                                byte[] buffer = eventBuffer.Data;
                                ref int index = ref eventBuffer.Index;

                                long qpcFrequency = Stopwatch.Frequency;
                                long qpcSync = Stopwatch.GetTimestamp();
                                long utcSync = DateTime.UtcNow.ToFileTimeUtc();

                                EventBuffer.Serializer.WriteUInt64(buffer, ref index, (ulong)qpcFrequency);
                                EventBuffer.Serializer.WriteUInt64(buffer, ref index, (ulong)qpcSync);
                                EventBuffer.Serializer.WriteUInt64(buffer, ref index, (ulong)utcSync);
                                EventBuffer.Serializer.WriteUInt32(buffer, ref index, EventBufferSize);
                                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), index++) = (byte)wrapperIPs.Length;

                                for (int i = 0; i < wrapperIPs.Length; i++)
                                {
                                    EventBuffer.Serializer.WriteUInt64(buffer, ref index, (ulong)wrapperIPs[i]);
                                }
                            }

                            // Force flush to deliver metadata event promptly.
                            context.Flush();

                            s_metadataRevision = Revision;
                        }
                    }
                }
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
                public const int HeaderSize = 37;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteInt32(Span<byte> buffer, ref int index, int value)
                {
                    WriteUInt32(buffer, ref index, (uint)value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteInt32(byte[] buffer, ref int index, int value)
                {
                    WriteUInt32(buffer, ref index, (uint)value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedInt32(Span<byte> buffer, ref int index, int value)
                {
                    WriteCompressedUInt32(buffer, ref index, ZigzagEncodeInt32(value));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedInt32(byte[] buffer, ref int index, int value)
                {
                    WriteCompressedUInt32(buffer, ref index, ZigzagEncodeInt32(value));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteUInt32(Span<byte> buffer, ref int index, uint value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length - sizeof(uint)));
                    WriteUInt32(ref MemoryMarshal.GetReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteUInt32(byte[] buffer, ref int index, uint value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length - sizeof(uint)));
                    WriteUInt32(ref MemoryMarshal.GetArrayDataReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void WriteUInt32(ref byte buffer, ref int index, uint value)
                {
                    if (!BitConverter.IsLittleEndian)
                        value = BinaryPrimitives.ReverseEndianness(value);

                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref buffer, index), value);
                    index += 4;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedUInt32(Span<byte> buffer, ref int index, uint value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length - MaxCompressedUInt32Size));
                    WriteCompressedUInt32(ref MemoryMarshal.GetReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedUInt32(byte[] buffer, ref int index, uint value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length - MaxCompressedUInt32Size));
                    WriteCompressedUInt32(ref MemoryMarshal.GetArrayDataReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void WriteCompressedUInt32(ref byte buffer, ref int index, uint value)
                {
                    while (value > 0x7Fu)
                    {
                        Unsafe.Add(ref buffer, index++) = (byte)((uint)value | ~0x7Fu);
                        value >>= 7;
                    }
                    Unsafe.Add(ref buffer, index++) = (byte)value;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteInt64(Span<byte> buffer, ref int index, long value)
                {
                    WriteUInt64(buffer, ref index, (ulong)value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteInt64(byte[] buffer, ref int index, long value)
                {
                    WriteUInt64(buffer, ref index, (ulong)value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedInt64(Span<byte> buffer, ref int index, long value)
                {
                    WriteCompressedUInt64(buffer, ref index, ZigzagEncodeInt64(value));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedInt64(byte[] buffer, ref int index, long value)
                {
                    WriteCompressedUInt64(buffer, ref index, ZigzagEncodeInt64(value));
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteUInt64(Span<byte> buffer, ref int index, ulong value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length - sizeof(ulong)));
                    WriteUInt64(ref MemoryMarshal.GetReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteUInt64(byte[] buffer, ref int index, ulong value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length -sizeof(ulong)));
                    WriteUInt64(ref MemoryMarshal.GetArrayDataReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void WriteUInt64(ref byte buffer, ref int index, ulong value)
                {
                    if (!BitConverter.IsLittleEndian)
                        value = BinaryPrimitives.ReverseEndianness(value);

                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref buffer, index), value);
                    index += sizeof(ulong);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedUInt64(Span<byte> buffer, ref int index, ulong value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length - MaxCompressedUInt64Size));
                    WriteCompressedUInt64(ref MemoryMarshal.GetReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void WriteCompressedUInt64(byte[] buffer, ref int index, ulong value)
                {
                    Debug.Assert((uint)index <= (uint)(buffer.Length - MaxCompressedUInt64Size));
                    WriteCompressedUInt64(ref MemoryMarshal.GetArrayDataReference(buffer), ref index, value);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static void WriteCompressedUInt64(ref byte buffer, ref int index, ulong value)
                {
                    while (value > 0x7Fu)
                    {
                        Unsafe.Add(ref buffer, index++) = (byte)((uint)value | ~0x7Fu);
                        value >>= 7;
                    }

                    Unsafe.Add(ref buffer, index++) = (byte)value;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static uint ZigzagEncodeInt32(int value) => (uint)((value << 1) ^ (value >> 31));

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static ulong ZigzagEncodeInt64(long value) => (ulong)((value << 1) ^ (value >> 63));

                public static void Header(AsyncThreadContext context, ref EventBuffer eventBuffer)
                {
                    byte[] buffer = eventBuffer.Data;
                    ref int index = ref eventBuffer.Index;
                    long currentTimestamp = Stopwatch.GetTimestamp();

                    index = 0;
                    eventBuffer.EventCount = 0;
                    context.LastEventTimestamp = currentTimestamp;

                    //Write header to buffer
                    if (buffer.Length >= HeaderSize)
                    {
                        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), index++) = 1; // Version
                        WriteUInt32(buffer, ref index, 0); // Total size in bytes, will be updated on flush.
                        WriteUInt32(buffer, ref index, context.AsyncThreadContextId); // Async Thread Context ID
                        WriteUInt64(buffer, ref index, Thread.CurrentOSThreadId); // OS Thread ID
                        WriteUInt32(buffer, ref index, 0); // Total event count, will be updated on flush.
                        WriteUInt64(buffer, ref index, (ulong)currentTimestamp); // Start timestamp
                        WriteUInt64(buffer, ref index, 0); // End timestamp, will be updated on flush.
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, AsyncEventID eventID, int maxEventPayloadSize)
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    long delta = currentTimestamp - context.LastEventTimestamp;
                    return AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, maxEventPayloadSize);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static int AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, AsyncEventID eventID, int maxEventPayloadSize)
                {
                    long delta = currentTimestamp - context.LastEventTimestamp;
                    return AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, maxEventPayloadSize);
                }

                public static int AsyncEventHeader(AsyncThreadContext context, ref EventBuffer eventBuffer, long currentTimestamp, long delta, AsyncEventID eventID, int maxEventPayloadSize)
                {
                    const int maxEventHeaderSize = MaxCompressedUInt64Size + sizeof(byte);

                    byte[] buffer = eventBuffer.Data;
                    ref int index = ref eventBuffer.Index;

                    int asyncHeaderIndex = index;

                    if ((index + maxEventHeaderSize + maxEventPayloadSize) <= buffer.Length && delta >= 0)
                    {
                        context.LastEventTimestamp = currentTimestamp;
                    }
                    else
                    {
                        // Event is too big for buffer, drop it.
                        if (maxEventHeaderSize + maxEventPayloadSize > buffer.Length)
                        {
                            return -1;
                        }

                        context.Flush();

                        delta = 0;
                        asyncHeaderIndex = 0;
                    }

                    WriteCompressedUInt64(buffer, ref index, (ulong)delta); //Timestamp delta from last event
                    Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), index++) = (byte)eventID; // eventID

                    eventBuffer.EventCount++;
                    return asyncHeaderIndex;
                }

                public static void RemoveAsyncEventHeader(AsyncThreadContext context, int savedAsyncEventHeaderIndex)
                {
                    if (context.EventBuffer.EventCount > 0)
                    {
                        context.EventBuffer.Index = savedAsyncEventHeaderIndex;
                        context.EventBuffer.EventCount--;
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void CallstackHeader(ref EventBuffer eventBuffer, ulong id, AsyncCallstackType type, byte callstackFrameCount)
                {
                    byte[] buffer = eventBuffer.Data;
                    ref int index = ref eventBuffer.Index;

                    WriteCompressedUInt64(buffer, ref index, id);

                    ref byte dst = ref MemoryMarshal.GetArrayDataReference(buffer);

                    Unsafe.Add(ref dst, index++) = (byte)type;
                    Unsafe.Add(ref dst, index++) = 0; // Reserved callstack ID for future callstack interning.
                    Unsafe.Add(ref dst, index++) = callstackFrameCount;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public static void CallstackData(ref EventBuffer eventBuffer, ReadOnlySpan<byte> callstackData, int callstackDataByteCount)
                {
                    ref int index = ref eventBuffer.Index;
                    Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(eventBuffer.Data), index), ref MemoryMarshal.GetReference(callstackData), (uint)callstackDataByteCount);
                    index += callstackDataByteCount;
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
            }

            private EventBuffer _eventBuffer;

            public long LastEventTimestamp;

            public EventKeywords ActiveEventKeywords;

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
                        _eventBuffer.Data = AllocBuffer();
                        EventBuffer.Serializer.Header(this, ref _eventBuffer);
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
                    context.InUse = false;
                    lock (AsyncThreadContextCache.CacheLock) { ; }
                    context.InUse = true;
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

                return Create();
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

                context = Get();
                info.Context = t_asyncThreadContext;

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

                ref EventBuffer eventBuffer = ref EventBuffer;

                if (eventBuffer.EventCount == 0)
                {
                    return;
                }

                int index = 1; // Skip version

                // Fill in total size in header before flushing.
                EventBuffer.Serializer.WriteUInt32(eventBuffer.Data, ref index, (uint)eventBuffer.Index);

                index += sizeof(uint) + sizeof(ulong); // Skip AsyncThreadContextId and OSThreadId

                // Fill in event count in header before flushing.
                EventBuffer.Serializer.WriteUInt32(eventBuffer.Data, ref index, eventBuffer.EventCount);

                index += sizeof(ulong); // Skip start timestamp

                // Fill in end timestamp in header before flushing.
                EventBuffer.Serializer.WriteUInt64(eventBuffer.Data, ref index, (ulong)LastEventTimestamp);

                try
                {
                    EmitEvent(eventBuffer.Data.AsSpan().Slice(0, eventBuffer.Index));
                }
                catch
                {
                    // AsyncProfiler can't throw, ignore exception and lose buffer.
                }

                EventBuffer.Serializer.Header(this, ref eventBuffer);
            }

            private static void EmitEvent(Span<byte> buffer)
            {
                Log.AsyncEvents(buffer);
            }

            private static AsyncThreadContext Create()
            {
                AsyncThreadContext context = new AsyncThreadContext();
                AsyncThreadContextCache.Add(context);
                t_asyncThreadContext = context;
                return context;
            }

            private static byte[] AllocBuffer()
            {
                try
                {
                    return new byte[Config.EventBufferSize];
                }
                catch
                {
                    // Async Profiler can't throw, ignore exception and use empty buffer.
                    // This will cause event to drop and attempt to reallocate buffer on next event.
                    return Array.Empty<byte>();
                }
            }

            [ThreadStatic]
            private static AsyncThreadContext? t_asyncThreadContext;
        }

        internal static partial class CreateAsyncContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, ulong id)
            {
                const int maxEventPayloadSize = EventBuffer.Serializer.MaxCompressedUInt64Size;

                if (EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.CreateAsyncContext, maxEventPayloadSize) >= 0)
                {
                    EventBuffer.Serializer.WriteCompressedUInt64(context.EventBuffer.Data, ref context.EventBuffer.Index, id);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, ulong id)
            {
                const int maxEventPayloadSize = EventBuffer.Serializer.MaxCompressedUInt64Size;

                if (EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, AsyncEventID.CreateAsyncContext, maxEventPayloadSize) >= 0)
                {
                    EventBuffer.Serializer.WriteCompressedUInt64(context.EventBuffer.Data, ref context.EventBuffer.Index, id);
                }
            }
        }

        internal static partial class ResumeAsyncContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, ulong id)
            {
                const int maxEventPayloadSize = EventBuffer.Serializer.MaxCompressedUInt64Size;

                if (EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResumeAsyncContext, maxEventPayloadSize) >= 0)
                {
                    EventBuffer.Serializer.WriteCompressedUInt64(context.EventBuffer.Data, ref context.EventBuffer.Index, id);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, ulong id)
            {
                const int maxEventPayloadSize = EventBuffer.Serializer.MaxCompressedUInt64Size;

                if (EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, AsyncEventID.ResumeAsyncContext, maxEventPayloadSize) >= 0)
                {
                    EventBuffer.Serializer.WriteCompressedUInt64(context.EventBuffer.Data, ref context.EventBuffer.Index, id);
                }
            }
        }

        internal static partial class SuspendAsyncContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp)
            {
                EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, AsyncEventID.SuspendAsyncContext, 0);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp)
            {
                EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, currentTimestamp, AsyncEventID.CompleteAsyncContext, 0);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, uint unwindedFrames)
            {
                // unwinded frames
                const int maxEventPayloadSize = EventBuffer.Serializer.MaxCompressedUInt32Size;

                ref EventBuffer eventBuffer = ref context.EventBuffer;

                if (EventBuffer.Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, AsyncEventID.UnwindAsyncException, maxEventPayloadSize) >= 0)
                {
                    EventBuffer.Serializer.WriteCompressedUInt32(eventBuffer.Data, ref eventBuffer.Index, unwindedFrames);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context)
            {
                EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResumeAsyncMethod, 0);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context)
            {
                EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.CompleteAsyncMethod, 0);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void EmitEvent(AsyncThreadContext context)
            {
                EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResetAsyncContinuationWrapperIndex, 0);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void EmitEvent(AsyncThreadContext context)
            {
                EventBuffer.Serializer.AsyncEventHeader(context, ref context.EventBuffer, AsyncEventID.ResetAsyncThreadContext, 0);
            }
        }

        private static partial class AsyncCallstack
        {
#pragma warning disable CA1823
            private const int ASYNC_METHOD_INFO_SIZE = EventBuffer.Serializer.MaxCompressedUInt64Size + EventBuffer.Serializer.MaxCompressedUInt32Size;
#pragma warning restore CA1823
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
                    s_flushTimer.Change(ASYNC_THREAD_CONTEXT_CACHE_FLUSH_TIMER_INTERVAL_MS, Timeout.Infinite);
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
                    s_cleanupTimer?.Change(ASYNC_THREAD_CONTEXT_CACHE_CLEANUP_TIMER_INTERVAL_MS, Timeout.Infinite);
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
                        s_cleanupTimer?.Change(ASYNC_THREAD_CONTEXT_CACHE_CLEANUP_TIMER_INTERVAL_MS, Timeout.Infinite);
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
                        s_flushTimer?.Change(ASYNC_THREAD_CONTEXT_CACHE_FLUSH_TIMER_INTERVAL_MS, Timeout.Infinite);
                    }
                    else
                    {
                        // Start cleanup timer.
                        s_cleanupTimer?.Change(ASYNC_THREAD_CONTEXT_CACHE_CLEANUP_TIMER_INTERVAL_MS, Timeout.Infinite);
                    }
                }
            }

            private static void FlushCore(bool force)
            {
                // Make sure all dead threads are flushed and removed from the cache.
                for (int i = s_cache.Count - 1; i >= 0; i--)
                {
                    var contextHolder = s_cache[i];
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

                // Look at live threads, only flush if forced or contexts that have been idle for 250 milliseconds.
                long idleWriteTimestamp = Stopwatch.GetTimestamp() - (Stopwatch.Frequency / 4);

                // Additionally, reclaim buffers for contexts that have been idle for 30 seconds to avoid keeping
                // large buffers around indefinitely for threads that are no longer running async code.
                long idleReclaimBufferTimestamp = Stopwatch.GetTimestamp() - Stopwatch.Frequency * 30;

                // Spin wait timeout, 100 milliseconds.
                long spinWaitTimeout = Stopwatch.Frequency / 10;

                foreach (var contextHolder in s_cache)
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

            private const int ASYNC_THREAD_CONTEXT_CACHE_FLUSH_TIMER_INTERVAL_MS = 1000;
            private static Timer? s_flushTimer;

            private const int ASYNC_THREAD_CONTEXT_CACHE_CLEANUP_TIMER_INTERVAL_MS = 30000;
            private static Timer? s_cleanupTimer;

            private static List<AsyncThreadContextHolder> s_cache = new List<AsyncThreadContextHolder>();
        }

    }
}
