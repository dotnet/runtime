// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace System.Diagnostics.Tests
{
    // Decoding half of the trace-id tests: the listener that collects the AsyncProfilerEventSource stream, the
    // per-thread buffer parser and its event manifest, and the timeline helpers that reconstruct the sparse
    // (osThreadId, timestamp, traceId) step function the scenarios in AsyncProfilerTraceIdTests.cs assert on.
    public partial class AsyncProfilerTraceIdTests
    {
        private const int AsyncEventsEventId = 1;         // AsyncProfilerEventSource.ASYNC_EVENTS_ID

        // Async event ids as emitted into the per-thread buffer. Mirrors AsyncProfiler.AsyncEventID in the
        // runtime (internal, inaccessible from tests); TraceIdChanged (24) is the record this test decodes.
        private enum AsyncEventId : byte
        {
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
            CreateStateMachineAsyncContext = 11,
            ResumeStateMachineAsyncContext = 12,
            SuspendStateMachineAsyncContext = 13,
            CompleteStateMachineAsyncContext = 14,
            UnwindStateMachineAsyncException = 15,
            ResumeStateMachineAsyncCallstack = 16,
            ResumeStateMachineAsyncMethod = 17,
            CompleteStateMachineAsyncMethod = 18,
            AppendStateMachineAsyncCallstack = 19,
            ResetAsyncThreadContext = 20,
            ResetAsyncContinuationWrapperIndex = 21,
            AsyncProfilerMetadata = 22,
            AsyncProfilerSyncClock = 23,
            TraceIdChanged = 24,
        }

        // Number of bytes of little-endian length prefix that precede an event's payload. The numeric value is
        // also the size in bytes of that prefix, so it doubles as the amount to advance past it.
        private enum PayloadPrefix : byte
        {
            None = 0,
            Byte = 1,
            UShort = 2,
        }

        private sealed record ChangePoint(ulong OsThreadId, ulong Timestamp, byte[] TraceId)
        {
            public bool IsCleared => TraceId.All(static b => b == 0);
            public string TraceIdHex => Convert.ToHexStringLower(TraceId);
        }

        // Captures the buffered TraceId records (TraceIdChanged, inside the AsyncEvents blob) into one ordered
        // set of change-points, the timeline a consumer reconstructs.
        private sealed class ChangePointListener : EventListener
        {
            public readonly ConcurrentQueue<ChangePoint> Points = new();
            public volatile EventSource? Source;

            // EventListener's base constructor invokes OnEventSourceCreated for already-created sources before
            // any derived field initializer runs, so it only captures the source; the test enables it later.
            protected override void OnEventSourceCreated(EventSource source)
            {
                if (source.Name == AsyncProfilerSourceName)
                {
                    Source = source;
                }
            }

            public void Enable(EventKeywords keywords) =>
                EnableEvents(Source!, EventLevel.Informational, keywords);

            public void Disable() => DisableEvents(Source!);

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventId == AsyncEventsEventId && eventData.Payload is { Count: > 0 }
                    && eventData.Payload[0] is byte[] block)
                {
                    ParseBlock(block, Points);
                }
            }
        }

        // Groups change-points by OS thread id, orders by timestamp, and collapses consecutive equal trace ids
        // into the sparse step function the timeline reconstructs.
        private static Dictionary<ulong, List<ChangePoint>> BuildPerThreadTimelines(List<ChangePoint> points)
        {
            Dictionary<ulong, List<ChangePoint>> byThread = new();
            foreach (IGrouping<ulong, ChangePoint> group in points.GroupBy(p => p.OsThreadId))
            {
                List<ChangePoint> stepFunction = new();
                foreach (ChangePoint p in group.OrderBy(p => p.Timestamp))
                {
                    if (stepFunction.Count == 0 || stepFunction[^1].TraceIdHex != p.TraceIdHex)
                    {
                        stepFunction.Add(p);
                    }
                }
                byThread[group.Key] = stepFunction;
            }
            return byThread;
        }

        // The active trace id at 'timestamp' is the last change-point at or before it (null before the first).
        private static string? Resolve(List<ChangePoint> timeline, ulong timestamp)
        {
            string? active = null;
            foreach (ChangePoint p in timeline)
            {
                if (p.Timestamp > timestamp)
                {
                    break;
                }
                active = p.IsCleared ? null : p.TraceIdHex;
            }
            return active;
        }

        private static string Describe(IEnumerable<ChangePoint> points) =>
            $"[{string.Join(",", points.Select(p => $"{(p.IsCleared ? "0" : p.TraceIdHex.Substring(0, 4))}@{p.Timestamp}"))}]";

        // Parses the per-thread block written by AsyncProfiler.EventBuffer.Serializer and pulls out the
        // TraceIdChanged records:
        //   Block header (37 bytes): version(1) size(u32) asyncThreadContextId(u32) osThreadId(u64)
        //                            eventCount(u32) startTimestamp(u64) endTimestamp(u64)
        //   Per event: eventId(1) deltaTicks(varint) [lengthPrefix(PayloadPrefix bytes)] [payload]
        private const int HeaderSize = 37;
        private const int OsThreadIdOffset = 9;
        private const int EventCountOffset = 17;
        private const int StartTimestampOffset = 21;

        private static void ParseBlock(byte[] block, ConcurrentQueue<ChangePoint> sink)
        {
            if (block.Length < HeaderSize)
            {
                return;
            }

            ulong osThreadId = BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(OsThreadIdOffset));
            uint eventCount = BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan(EventCountOffset));
            ulong timestamp = BinaryPrimitives.ReadUInt64LittleEndian(block.AsSpan(StartTimestampOffset));

            int pos = HeaderSize;
            for (uint i = 0; i < eventCount && pos < block.Length; i++)
            {
                byte eventId = block[pos++];
                timestamp += ReadVarint(block, ref pos);

                if (eventId >= s_payloadPrefixByEventId.Length)
                {
                    return; // unknown id: cannot know its framing, bail on this block
                }

                PayloadPrefix prefix = s_payloadPrefixByEventId[eventId];
                int payloadLength = prefix switch
                {
                    PayloadPrefix.Byte => block[pos],
                    PayloadPrefix.UShort => BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(pos)),
                    _ => 0,
                };
                pos += (int)prefix;

                if ((AsyncEventId)eventId == AsyncEventId.TraceIdChanged)
                {
                    byte[] traceId = block.AsSpan(pos, payloadLength).ToArray();
                    sink.Enqueue(new ChangePoint(osThreadId, timestamp, traceId));
                }

                pos += payloadLength;
            }
        }

        private static ulong ReadVarint(byte[] buffer, ref int pos)
        {
            ulong result = 0;
            int shift = 0;
            while (true)
            {
                byte b = buffer[pos++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
            }
            return result;
        }

        // Payload framing per async event id, mirroring AsyncProfiler's emit so the parser can skip events it
        // does not care about and locate the TraceIdChanged payload. Indexed by (byte)AsyncEventId.
        private static readonly PayloadPrefix[] s_payloadPrefixByEventId = BuildPayloadPrefixTable();

        private static PayloadPrefix[] BuildPayloadPrefixTable()
        {
            PayloadPrefix[] table = new PayloadPrefix[(int)AsyncEventId.TraceIdChanged + 1];
            table[(int)AsyncEventId.CreateRuntimeAsyncContext] = PayloadPrefix.Byte;
            table[(int)AsyncEventId.ResumeRuntimeAsyncContext] = PayloadPrefix.Byte;
            table[(int)AsyncEventId.SuspendRuntimeAsyncContext] = PayloadPrefix.None;
            table[(int)AsyncEventId.CompleteRuntimeAsyncContext] = PayloadPrefix.None;
            table[(int)AsyncEventId.UnwindRuntimeAsyncException] = PayloadPrefix.Byte;
            table[(int)AsyncEventId.CreateRuntimeAsyncCallstack] = PayloadPrefix.UShort;
            table[(int)AsyncEventId.ResumeRuntimeAsyncCallstack] = PayloadPrefix.UShort;
            table[(int)AsyncEventId.SuspendRuntimeAsyncCallstack] = PayloadPrefix.UShort;
            table[(int)AsyncEventId.ResumeRuntimeAsyncMethod] = PayloadPrefix.None;
            table[(int)AsyncEventId.CompleteRuntimeAsyncMethod] = PayloadPrefix.None;
            table[(int)AsyncEventId.CreateStateMachineAsyncContext] = PayloadPrefix.Byte;
            table[(int)AsyncEventId.ResumeStateMachineAsyncContext] = PayloadPrefix.Byte;
            table[(int)AsyncEventId.SuspendStateMachineAsyncContext] = PayloadPrefix.None;
            table[(int)AsyncEventId.CompleteStateMachineAsyncContext] = PayloadPrefix.None;
            table[(int)AsyncEventId.UnwindStateMachineAsyncException] = PayloadPrefix.Byte;
            table[(int)AsyncEventId.ResumeStateMachineAsyncCallstack] = PayloadPrefix.UShort;
            table[(int)AsyncEventId.ResumeStateMachineAsyncMethod] = PayloadPrefix.None;
            table[(int)AsyncEventId.CompleteStateMachineAsyncMethod] = PayloadPrefix.None;
            table[(int)AsyncEventId.AppendStateMachineAsyncCallstack] = PayloadPrefix.UShort;
            table[(int)AsyncEventId.ResetAsyncThreadContext] = PayloadPrefix.None;
            table[(int)AsyncEventId.ResetAsyncContinuationWrapperIndex] = PayloadPrefix.None;
            table[(int)AsyncEventId.AsyncProfilerMetadata] = PayloadPrefix.UShort;
            table[(int)AsyncEventId.AsyncProfilerSyncClock] = PayloadPrefix.Byte;
            table[(int)AsyncEventId.TraceIdChanged] = PayloadPrefix.Byte;   // 16-byte trace id, byte length prefix
            return table;
        }
    }
}
