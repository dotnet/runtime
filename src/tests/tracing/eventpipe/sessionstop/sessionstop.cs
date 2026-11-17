// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Xunit;

namespace Tracing.Tests.SessionStop;

public static class SessionStop
{
    private const string ProviderName = "System.Runtime.CompilerServices.AsyncProfilerEventSource";
    private const long ContextKeywords = 0xF;
    private const int HeaderSize = 1 + sizeof(uint) + sizeof(uint) + sizeof(ulong) + sizeof(uint) + sizeof(ulong) + sizeof(ulong);

    private enum AsyncEventId : byte
    {
        CreateRuntimeAsyncContext = 1,
        ResumeRuntimeAsyncContext = 2,
        SuspendRuntimeAsyncContext = 3,
        CompleteRuntimeAsyncContext = 4,
        ResetAsyncThreadContext = 20,
        ResetAsyncContinuationWrapperIndex = 21,
        AsyncProfilerMetadata = 22,
        AsyncProfilerSyncClock = 23,
    }

    private sealed class InlineSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object state) => callback(state);
    }

    [ActiveIssue("Diagnostic IPC is not supported on these platforms.", TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
    [SkipOnCoreClr("Tracing tests routinely time out under stress.", RuntimeTestModes.AnyGCStress | RuntimeTestModes.AnyJitStress)]
    [Fact]
    public static void TestEntryPoint()
    {
        Console.WriteLine("Validating async-profiler context lifecycle at EventPipe session stop.");
        var client = new DiagnosticsClient(Environment.ProcessId);
        EventPipeProvider[] providers = [new EventPipeProvider(ProviderName, EventLevel.Informational, ContextKeywords)];
        var configuration = new EventPipeSessionConfiguration(providers, circularBufferSizeMB: 1, requestRundown: false, requestStackwalk: false);
        using EventPipeSession session = client.StartEventPipeSession(configuration);
        using var source = new EventPipeEventSource(session.EventStream);
        var buffers = new List<byte[]>();
        source.Dynamic.All += traceEvent =>
        {
            if (traceEvent.ProviderName == ProviderName && (int)traceEvent.ID == 1)
            {
                ReadOnlySpan<byte> payload = traceEvent.EventData();
                Assert.True(payload.Length >= sizeof(int), "Missing async-profiler buffer length.");
                int bufferLength = BinaryPrimitives.ReadInt32LittleEndian(payload);
                Assert.Equal(payload.Length - sizeof(int), bufferLength);
                buffers.Add(payload.Slice(sizeof(int)).ToArray());
            }
        };
        Task reader = Task.Run(() => source.Process());

        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        SynchronizationContext previousContext = SynchronizationContext.Current;
        Task workload;
        try
        {
            // Complete each gate inline so the second await must suspend before its gate is released.
            SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
            workload = RunScenario(first.Task, second.Task);
            Assert.False(workload.IsCompleted);
            first.SetResult();
            Assert.False(workload.IsCompleted);
            second.SetResult();
            workload.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
        session.Stop();

        Assert.True(reader.Wait(TimeSpan.FromSeconds(30)), "EventPipe reader did not finish after session stop.");
        Assert.Equal(0, source.EventsLost);
        AsyncEventId[] actual = ReadContextEvents(buffers, (ulong)workload.Id);
        AsyncEventId[] expected =
        [
            AsyncEventId.CreateRuntimeAsyncContext,
            AsyncEventId.ResumeRuntimeAsyncContext,
            AsyncEventId.SuspendRuntimeAsyncContext,
            AsyncEventId.ResumeRuntimeAsyncContext,
            AsyncEventId.CompleteRuntimeAsyncContext,
        ];
        Console.WriteLine($"Task {workload.Id}: {string.Join(", ", actual)} ({buffers.Count} profiler packets).");
        Assert.Equal(expected, actual);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task RunScenario(Task first, Task second)
    {
        await first;
        await second;
    }

    private static AsyncEventId[] ReadContextEvents(List<byte[]> buffers, ulong dispatcherId)
    {
        var events = new List<(ulong Timestamp, AsyncEventId EventId)>();
        var dispatchers = new Dictionary<uint, Stack<ulong>>();
        foreach (byte[] bytes in buffers)
        {
            Assert.True(bytes.Length >= HeaderSize, "Incomplete async-profiler packet header.");
            Assert.Equal(1, bytes[0]);
            Assert.Equal((uint)bytes.Length, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(1)));
        }

        // A periodic flush may split a context across packets; retain each thread's dispatcher stack.
        foreach (byte[] bytes in buffers.OrderBy(buffer => BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(21))))
        {
            ReadOnlySpan<byte> buffer = bytes;
            uint threadContextId = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(5));
            uint eventCount = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(17));
            ulong timestamp = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(21));
            if (!dispatchers.TryGetValue(threadContextId, out Stack<ulong> stack))
            {
                stack = new Stack<ulong>();
                dispatchers.Add(threadContextId, stack);
            }

            int index = HeaderSize;
            for (uint i = 0; i < eventCount; i++)
            {
                AsyncEventId eventId = (AsyncEventId)buffer[index++];
                timestamp = checked(timestamp + ReadCompressedUInt64(buffer, ref index));
                int payloadLength = eventId switch
                {
                    AsyncEventId.CreateRuntimeAsyncContext or AsyncEventId.ResumeRuntimeAsyncContext or AsyncEventId.AsyncProfilerSyncClock => buffer[index++],
                    AsyncEventId.AsyncProfilerMetadata => ReadMetadataLength(buffer, ref index),
                    AsyncEventId.SuspendRuntimeAsyncContext or AsyncEventId.CompleteRuntimeAsyncContext or
                    AsyncEventId.ResetAsyncThreadContext or AsyncEventId.ResetAsyncContinuationWrapperIndex => 0,
                    _ => throw new InvalidOperationException($"Unexpected async-profiler event {eventId} with context-only keywords."),
                };
                int payloadEnd = checked(index + payloadLength);
                Assert.True(payloadEnd <= buffer.Length, "Incomplete async-profiler event payload.");
                ReadOnlySpan<byte> payload = buffer.Slice(index, payloadLength);
                int payloadIndex = 0;
                ulong currentDispatcher = 0;
                switch (eventId)
                {
                    case AsyncEventId.CreateRuntimeAsyncContext:
                        ReadCompressedUInt64(payload, ref payloadIndex);
                        currentDispatcher = ReadCompressedUInt64(payload, ref payloadIndex);
                        Assert.Equal(payloadLength, payloadIndex);
                        break;
                    case AsyncEventId.ResumeRuntimeAsyncContext:
                        currentDispatcher = ReadCompressedUInt64(payload, ref payloadIndex);
                        Assert.Equal(payloadLength, payloadIndex);
                        stack.Push(currentDispatcher);
                        break;
                    case AsyncEventId.SuspendRuntimeAsyncContext:
                    case AsyncEventId.CompleteRuntimeAsyncContext:
                        if (stack.Count > 0)
                        {
                            currentDispatcher = stack.Pop();
                        }
                        break;
                    case AsyncEventId.ResetAsyncThreadContext:
                        stack.Clear();
                        break;
                }
                if (currentDispatcher == dispatcherId)
                {
                    events.Add((timestamp, eventId));
                }
                index = payloadEnd;
            }
            Assert.Equal(buffer.Length, index);
        }
        return events.OrderBy(entry => entry.Timestamp).Select(entry => entry.EventId).ToArray();
    }

    private static int ReadMetadataLength(ReadOnlySpan<byte> buffer, ref int index)
    {
        ushort length = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(index));
        index += sizeof(ushort);
        return length;
    }

    private static ulong ReadCompressedUInt64(ReadOnlySpan<byte> buffer, ref int index)
    {
        ulong value = 0;
        for (int shift = 0; shift < 64; shift += 7)
        {
            byte current = buffer[index++];
            Assert.True(shift < 63 || current <= 1, "Invalid async-profiler compressed integer.");
            value |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0)
            {
                return value;
            }
        }
        throw new InvalidOperationException("Invalid async-profiler compressed integer.");
    }
}
