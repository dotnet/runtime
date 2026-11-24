// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;
using TestLibrary;

namespace Tracing.Tests
{
    public sealed class NativeRuntimeEventSourceTest
    {
        private const ulong MemoryPressure = 1_234_567;
        private const ulong WarmupMemoryPressure = 65_531;

        [ActiveIssue("Memory-pressure tracing is not enabled on Mono", TestRuntimes.Mono)]
        [ActiveIssue("WASM doesn't support diagnostics tracing", TestPlatforms.Browser | TestPlatforms.Wasi)]
        [SkipOnCoreClr("Tracing tests routinely time out with GC stress.", RuntimeTestModes.AnyGCStress)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [InlineData(EventLevel.Informational, 1L)]
        [InlineData(EventLevel.Verbose, 0x10000L)]
        public static void MemoryPressureEvents(EventLevel disabledLevel, long disabledKeywords)
        {
            const EventKeywords GCKeyword = (EventKeywords)1;
            const int Iterations = 10;
            const int TimeoutMilliseconds = 30_000;

            using MemoryPressureListener listener = new();
            using MemoryPressureListener disabledListener = new();
            EventSource source = EventSource.GetSources().Single(source => source.Name == "Microsoft-Windows-DotNETRuntime");
            listener.EnableEvents(source, EventLevel.Verbose, GCKeyword);
            disabledListener.EnableEvents(source, disabledLevel, (EventKeywords)disabledKeywords);

            long deadline = Environment.TickCount64 + TimeoutMilliseconds;
            while (!listener.WarmupReceived && Environment.TickCount64 < deadline)
            {
                GC.AddMemoryPressure((long)WarmupMemoryPressure);
                GC.RemoveMemoryPressure((long)WarmupMemoryPressure);
                Thread.Sleep(10);
            }
            Assert.True(listener.WarmupReceived, "Timed out waiting for memory-pressure events.");

            for (int i = 0; i < Iterations; i++)
            {
                GC.AddMemoryPressure((long)MemoryPressure);
                GC.RemoveMemoryPressure((long)MemoryPressure);
            }

            // A native GC event also waits for asynchronously forwarded pressure events.
            listener.ExpectedCollectionCount = GC.CollectionCount(0) + 1;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            deadline = Environment.TickCount64 + TimeoutMilliseconds;
            while (!listener.CollectionCompleted && Environment.TickCount64 < deadline)
            {
                Thread.Sleep(10);
            }
            Assert.True(listener.CollectionCompleted, "Timed out waiting for the collection-end event.");

            EventWrittenEventArgs[] events = listener.Events.ToArray();
            Assert.Equal(Iterations * 2, events.Length);
            Assert.Equal(Iterations, events.Count(eventData => eventData.EventId == 200));
            Assert.Equal(Iterations, events.Count(eventData => eventData.EventId == 201));
            foreach (EventWrittenEventArgs eventData in events)
            {
                Assert.Equal(eventData.EventId == 200 ? "IncreaseMemoryPressure" : "DecreaseMemoryPressure", eventData.EventName);
                Assert.Equal(0, eventData.Version);
                Assert.Equal(EventLevel.Verbose, eventData.Level);
                Assert.Equal(GCKeyword, eventData.Keywords & GCKeyword);
                Assert.Equal((EventTask)1, eventData.Task);
                Assert.Equal((EventOpcode)eventData.EventId, eventData.Opcode);
                Assert.Equal(2, eventData.Payload.Count);
                Assert.Equal(MemoryPressure, Assert.IsType<ulong>(eventData.Payload[0]));
                Assert.Equal(listener.RuntimeInstanceId, Assert.IsType<ushort>(eventData.Payload[1]));
                Assert.Equal(eventData.EventId == 200 ? "BytesAllocated" : "BytesFreed", eventData.PayloadNames[0]);
                Assert.Equal("ClrInstanceID", eventData.PayloadNames[1]);
            }
            Assert.Empty(disabledListener.Events);
        }

        private sealed class MemoryPressureListener : EventListener
        {
            private int _warmupReceived;
            private int _collectionCompleted;

            public readonly ConcurrentQueue<EventWrittenEventArgs> Events = new();
            public volatile int ExpectedCollectionCount = int.MaxValue;

            public bool WarmupReceived => Volatile.Read(ref _warmupReceived) != 0;
            public bool CollectionCompleted => Volatile.Read(ref _collectionCompleted) != 0;
            public ushort RuntimeInstanceId { get; private set; }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventId == 2 &&
                    eventData.Payload?[0] is uint collectionCount &&
                    eventData.Payload[1] is uint depth &&
                    eventData.Payload[2] is ushort instanceId &&
                    depth == 2 &&
                    collectionCount >= (uint)ExpectedCollectionCount)
                {
                    RuntimeInstanceId = instanceId;
                    Volatile.Write(ref _collectionCompleted, 1);
                }
                else if (eventData.EventId is 200 or 201 && eventData.Payload?[0] is ulong bytes)
                {
                    if (bytes == WarmupMemoryPressure)
                    {
                        Volatile.Write(ref _warmupReceived, 1);
                    }
                    else if (bytes == MemoryPressure)
                    {
                        Events.Enqueue(eventData);
                    }
                }
            }
        }

        [ActiveIssue("Build doesn't include diagnostics tracing", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAnyAOT))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/92727", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter), nameof(PlatformDetection.IsArm64Process), nameof(PlatformDetection.IsNotWindows))]
        [ActiveIssue("WASM doesn't support diagnostics tracing", TestPlatforms.Browser)]
        [SkipOnCoreClr("This test is sensitive to JIT optimizations.", RuntimeTestModes.AnyJitOptimizationStress)]
        [SkipOnCoreClr("Tracing tests routinely time out with JIT stress and GC stress.", RuntimeTestModes.AnyGCStress)]
        [Fact]
        public static void TestEntryPoint()
        {
            // Access ArrayPool.Shared.Rent() before the test to avoid the deadlock reported
            // in https://github.com/dotnet/runtime/issues/86233. This is a real issue,
            // but only seen if you have a short lived EventListener and create EventSources
            // in your OnEventWritten callback so we don't expect customers to hit it.
            byte[] localBuffer = ArrayPool<byte>.Shared.Rent(10);
            Console.WriteLine($"buffer length={localBuffer.Length}");

            // Create deaf listener
            Listener.Level = EventLevel.Critical;
            Listener.EnableKeywords = EventKeywords.None;
            using (Listener noEventsListener = new("NoEvents"))
            {
                using (NativeOverlappedClass nativeOverlappedClass = new())
                {
                    // Create an EventListener.
                    Listener.Level = EventLevel.Verbose;
                    const EventKeywords EventKeywordThreading = (EventKeywords)65536;
                    Listener.EnableKeywords = (EventKeywords)0x4c14fccbd | EventKeywordThreading;

                    // Check for events e.g. ThreadPoolIODequeue = 64
                    // At least some of these events can be found in "src\libraries\System.Private.CoreLib\src\System\Threading\NativeRuntimeEventSource.PortableThreadPool.NativeSinks.cs"
                    Listener.TargetEventIds(63, 64, 65);

                    using (Listener listener = new())
                    {
                        CancellationTokenSource cts = new();

                        // Trigger the allocator task.
                        Task.Run(() =>
                        {
                            while (!cts.IsCancellationRequested)
                            {
                                for (int i = 0; i < 1000; i++)
                                {
                                    GC.KeepAlive(new object());
                                }

                                Thread.Sleep(10);
                            }
                        });

                        // If on Windows, attempt some Overlapped IO (triggers ThreadPool events)
                        DoOverlappedIO();

                        // Trigger EventId 63 and 64
                        nativeOverlappedClass.ThreadPoolQueue();

                        // Generate some GC events.
                        GC.Collect(2, GCCollectionMode.Forced);

                        Stopwatch sw = Stopwatch.StartNew();

                        while (sw.Elapsed <= TimeSpan.FromSeconds(45))
                        {
                            Thread.Sleep(100);

                            if ((listener.EventsLeft <= 0) || (!OperatingSystem.IsWindows() && listener.EventCount > 0))
                            {
                                break;
                            }
                        }

                        cts.Cancel();

                        Assert2.True("listener.EventCount > 0", listener.EventCount > 0);

                        if (OperatingSystem.IsWindows())
                        {
                            StringBuilder stringBuilder = new();
                            foreach (var e in listener.GetFailedTargetEvents())
                            {
                                stringBuilder.Append((stringBuilder.Length > 0) ? ", " : "");
                                stringBuilder.Append(e.Key);
                            }
                            Assert2.True($"At least one of the EventIds ({stringBuilder}) where heard.", stringBuilder.Length < 1);
                        }
                    }
                }

                // Generate some more GC events.
                GC.Collect(2, GCCollectionMode.Forced);

                // Ensure that we've seen no events.
                Assert2.True("noEventsListener.EventCount == 0", noEventsListener.EventCount == 0);
            }
        }

        private static unsafe void DoOverlappedIO()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            Console.WriteLine("DOOVERLAPPEDIO");
            Overlapped overlapped = new();
            NativeOverlapped* pOverlap = overlapped.Pack(null, null);
            Overlapped.Free(pOverlap);
        }

        private static class Assert2
        {
            public static void True(string name, bool condition)
            {
                if (!condition)
                {
                    throw new Exception(
                        string.Format("Condition '{0}' is not true", name));
                }
            }
        }
    }

    internal sealed unsafe class NativeOverlappedClass : IDisposable
    {
        private bool disposedValue;
        private readonly NativeOverlapped* nativeOverlapped;

        public NativeOverlappedClass()
        {
            if (OperatingSystem.IsWindows())
            {
                nativeOverlapped = new Overlapped().Pack(null, null);
            }
        }

        public bool ThreadPoolQueue()
        {
            return OperatingSystem.IsWindows() && ThreadPool.UnsafeQueueNativeOverlapped(nativeOverlapped);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (OperatingSystem.IsWindows())
                {
                    Overlapped.Free(nativeOverlapped);
                }

                disposedValue = true;
            }
        }

        ~NativeOverlappedClass()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal sealed class Listener : EventListener
    {
        public static string EventSourceName = "Microsoft-Windows-DotNETRuntime";
        public static EventLevel Level = EventLevel.Verbose;
        public static EventKeywords EnableKeywords = EventKeywords.All;

        public int EventCount { get; private set; } = 0;
        public int EventsLeft { get; private set; } = 0;

        private static readonly ConcurrentBag<int> targetEventIds = new();
        private static readonly (string, string) defaultEventSourceNameName = ("Failed to listen", "Was not heard or didn't fire");

        private readonly string name = "";
        private readonly ConcurrentDictionary<int, (string, string)> eventIdSourceNameNames = new();

        public Listener(string name = nameof(Listener))
        {
            this.name = $"({name}) ";
        }

        public static void TargetEventIds(params int[] ids)
        {
            targetEventIds.Clear();

            foreach (int id in ids)
            {
                targetEventIds.Add(id);
            }
        }
        public IEnumerable<KeyValuePair<int, (string, string)>> GetFailedTargetEvents()
        {
            foreach (KeyValuePair<int, (string, string)> e in eventIdSourceNameNames)
            {
                if (e.Value == defaultEventSourceNameName)
                {
                    yield return e;
                }
            }
        }
        public override void Dispose()
        {
            base.Dispose();

            foreach (KeyValuePair<int, (string, string)> e in eventIdSourceNameNames)
            {
                WriteLine(e);
            }

            WriteLine("\n");
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if ((eventSource.Name == EventSourceName) && eventIdSourceNameNames.IsEmpty)
            {
                foreach (var targetEventId in targetEventIds)
                {
                    eventIdSourceNameNames[targetEventId] = defaultEventSourceNameName;
                }
                EventsLeft = eventIdSourceNameNames.Count;

                EnableEvents(eventSource, Level, EnableKeywords);
            }
        }
        protected override void OnEventWritten(EventWrittenEventArgs eventWrittenEventArgs)
        {
            EventCount++;

            KeyValuePair<int, (string SourceName, string Name)> e = new(
                eventWrittenEventArgs.EventId,
                (eventWrittenEventArgs.EventSource.Name, eventWrittenEventArgs.EventName ?? ""));

            EventsLeft -= (eventIdSourceNameNames.TryGetValue(e.Key, out (string, string) value) && value == defaultEventSourceNameName) ? 1 : 0;
            eventIdSourceNameNames[e.Key] = e.Value;

            WriteLine(e);

            WriteLine($"OSThreadId = {eventWrittenEventArgs.OSThreadId}", ConsoleColor.Yellow);

            WriteLine($"TimeStamp: {eventWrittenEventArgs.TimeStamp.ToLocalTime()}");
            WriteLine($"local time: {DateTime.Now}");
            WriteLine($"Difference: {DateTime.UtcNow - eventWrittenEventArgs.TimeStamp}");

            for (int i = 0; (i < eventWrittenEventArgs.PayloadNames?.Count) && (i < eventWrittenEventArgs.Payload?.Count); i++)
            {
                WriteLine($"{eventWrittenEventArgs.PayloadNames[i]} = {eventWrittenEventArgs.Payload[i]}", ConsoleColor.Magenta);
            }

            WriteLine("\n");
        }

        private static bool ConsoleForegroundColorNotSupported =>
            OperatingSystem.IsAndroid() ||
            OperatingSystem.IsIOS() ||
            OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser() ||
            OperatingSystem.IsWasi();

        private ConsoleColor ConsoleForegroundColor
        {
            get
            {
                if (ConsoleForegroundColorNotSupported)
                    return (ConsoleColor)(-1);
                
                return Console.ForegroundColor;
            }
            set
            {
                if (ConsoleForegroundColorNotSupported)
                    return;
                
                Console.ForegroundColor = value;
            }
        }
        
        private void Write(object? o = null, ConsoleColor? consoleColor = null)
        {
            ConsoleColor foregroundColor = ConsoleForegroundColor;

            if (o is KeyValuePair<int, (string, string)> e)
            {
                ConsoleForegroundColor = ConsoleColor.Cyan;
                
                Console.Write(name);

                ConsoleForegroundColor = (e.Value != defaultEventSourceNameName) ? ConsoleColor.Green : ConsoleColor.Red;
            }
            else if (consoleColor != null)
            {
                ConsoleForegroundColor = (ConsoleColor)consoleColor;
            }
            Console.Write(o);

            ConsoleForegroundColor = foregroundColor;
        }
        private void WriteLine(object? o = null, ConsoleColor? consoleColor = null)
        {
            Write(o, consoleColor);
            Console.WriteLine();
        }
    }
}
