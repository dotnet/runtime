// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public static class PauseDataTests
{
    private const int TimeoutMilliseconds = 30_000;
    private const int BufferCapacity = 4_096;
    private const int OverflowCollections = 5_000;
    private const int BackgroundNodeCount = 500_000;
    private const int BackgroundAttempts = 5;
    private static int s_finalizerThread;
    private static byte[]? s_backgroundAllocationSink;

    [SkipOnCoreClr("The buffer overflow checks require controlled collections.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<ArgumentNullException>("callback", () => GC.RegisterPauseDataCallback(null!));
        CreateFinalizableObject();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        Assert.NotEqual(0, Volatile.Read(ref s_finalizerThread));

        VerifyPayloadAndSubscribers();
        VerifySelfDisposal();
        VerifyOverflowAndNonblockingDisposal();
        VerifyHistogram();
        if (Environment.GetEnvironmentVariable("DOTNET_gcConcurrent") == "1")
        {
            VerifyBackgroundPauses();
        }
    }

    private static void VerifyPayloadAndSubscribers()
    {
        List<GCPauseData> first = new(32);
        List<GCPauseData> second = new(32);
        int callbackThread = 0;
        using IDisposable firstRegistration = GC.RegisterPauseDataCallback((pause, _) =>
        {
            lock (first)
            {
                first.Add(pause);
                callbackThread = Environment.CurrentManagedThreadId;
            }
        });
        using IDisposable secondRegistration = GC.RegisterPauseDataCallback((pause, _) =>
        {
            lock (second)
            {
                second.Add(pause);
            }
        });

        for (int generation = 0; generation <= GC.MaxGeneration; generation++)
        {
            GC.Collect(generation, GCCollectionMode.Forced, blocking: true);
            GCMemoryInfo info = GC.GetGCMemoryInfo(generation == GC.MaxGeneration ? GCKind.FullBlocking : GCKind.Ephemeral);
            GCPauseData firstPause = WaitForPause(first, info.Index);
            GCPauseData secondPause = WaitForPause(second, info.Index);
            Assert.Equal(generation, firstPause.Generation);
            Assert.False(firstPause.Concurrent);
            Assert.Equal(info.PauseDurations[0], firstPause.Duration);
            Assert.Equal(firstPause, secondPause);
            Assert.True(firstPause.SequenceNumber > 0);
        }

        lock (first)
        {
            Assert.Equal(Volatile.Read(ref s_finalizerThread), callbackThread);
            for (int i = 1; i < first.Count; i++)
            {
                Assert.True(first[i].SequenceNumber > first[i - 1].SequenceNumber);
            }
        }
    }

    private static GCPauseData WaitForPause(List<GCPauseData> pauses, long collectionIndex)
    {
        GCPauseData result = default;
        Assert.True(SpinWait.SpinUntil(() =>
        {
            lock (pauses)
            {
                foreach (GCPauseData pause in pauses)
                {
                    if (pause.CollectionIndex == collectionIndex)
                    {
                        result = pause;
                        return true;
                    }
                }
            }

            return false;
        }, TimeoutMilliseconds));
        GC.WaitForPendingFinalizers();
        lock (pauses)
        {
            int matches = 0;
            foreach (GCPauseData pause in pauses)
            {
                if (pause.CollectionIndex == collectionIndex)
                {
                    matches++;
                }
            }
            Assert.Equal(1, matches);
        }
        return result;
    }

    private static void VerifyBackgroundPauses()
    {
        BackgroundNode[] live = new BackgroundNode[BackgroundNodeCount];
        for (int i = 0; i < live.Length; i++)
        {
            live[i] = new BackgroundNode
            {
                Next = i > 0 ? live[i - 1] : null,
                Other = i > 1 ? live[i / 2] : null
            };
        }
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();

        List<GCPauseData> pauses = new(128);
        long baseline = 0;
        long missedTotal = 0;
        int backgroundMeasurements = 0;
        int invalidBackgroundTags = 0;
        Func<bool> accountingMatches = () =>
        {
            lock (pauses)
            {
                long ticks = 0;
                foreach (GCPauseData pause in pauses)
                {
                    ticks += pause.Duration.Ticks;
                }
                return ticks == GC.GetTotalPauseDuration().Ticks - baseline;
            }
        };
        using MeterListener listener = new();
        listener.InstrumentPublished = (instrument, subscriber) =>
        {
            if (instrument.Meter.Name == "System.Runtime.Detailed.GC" && instrument.Name == "dotnet.gc.pause.duration")
            {
                subscriber.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
        {
            string? generation = null;
            string? type = null;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "gc.heap.generation")
                {
                    generation = tag.Value as string;
                }
                if (tag.Key == "gc.pause.type")
                {
                    type = tag.Value as string;
                }
            }
            if (type == "background")
            {
                if (generation != "gen2")
                {
                    Interlocked.Increment(ref invalidBackgroundTags);
                }
                Interlocked.Increment(ref backgroundMeasurements);
            }
        });
        using IDisposable registration = GC.RegisterPauseDataCallback((pause, missed) =>
        {
            lock (pauses)
            {
                pauses.Add(pause);
            }
            Interlocked.Add(ref missedTotal, missed);
        });
        try
        {
            listener.Start();
            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            WaitForPause(pauses, GC.GetGCMemoryInfo(GCKind.Ephemeral).Index);
            lock (pauses)
            {
                pauses.Clear();
                baseline = GC.GetTotalPauseDuration().Ticks;
            }

            bool overlapObserved = false;
            for (int attempt = 0; attempt < BackgroundAttempts && !overlapObserved; attempt++)
            {
                long previousIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
                GC.Collect(2, GCCollectionMode.Forced, blocking: false);
                long deadline = Environment.TickCount64 + TimeoutMilliseconds;
                do
                {
                    for (int i = 0; i < 256; i++)
                    {
                        Volatile.Write(ref s_backgroundAllocationSink, new byte[16 * 1024]);
                    }
                    Assert.True(Environment.TickCount64 < deadline);
                }
                while (GC.GetGCMemoryInfo(GCKind.Background).Index <= previousIndex);
                long backgroundIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                WaitForPause(pauses, GC.GetGCMemoryInfo(GCKind.Ephemeral).Index);
                Assert.True(SpinWait.SpinUntil(accountingMatches, TimeoutMilliseconds));
                lock (pauses)
                {
                    int backgroundCount = 0;
                    long first = long.MaxValue;
                    long last = 0;
                    foreach (GCPauseData pause in pauses)
                    {
                        if (pause.Concurrent && pause.CollectionIndex == backgroundIndex)
                        {
                            Assert.Equal(2, pause.Generation);
                            backgroundCount++;
                            first = Math.Min(first, pause.SequenceNumber);
                            last = Math.Max(last, pause.SequenceNumber);
                        }
                    }
                    Assert.True(backgroundCount >= 2);
                    foreach (GCPauseData pause in pauses)
                    {
                        overlapObserved |= !pause.Concurrent && pause.SequenceNumber > first && pause.SequenceNumber < last;
                    }
                    Console.WriteLine($"Background contributions={backgroundCount}, foreground overlap={overlapObserved}");
                }
            }
            Assert.Equal(0, Interlocked.Read(ref missedTotal));
            Assert.True(Volatile.Read(ref backgroundMeasurements) >= 2);
            Assert.Equal(0, Volatile.Read(ref invalidBackgroundTags));
        }
        finally
        {
            registration.Dispose();
            listener.Dispose();
            GC.WaitForPendingFinalizers();
            s_backgroundAllocationSink = null;
            GC.KeepAlive(live);
        }
    }

    private sealed class BackgroundNode
    {
        public BackgroundNode? Next;
        public BackgroundNode? Other;
    }

    private static void VerifySelfDisposal()
    {
        using ManualResetEventSlim completed = new();
        int armed = 0;
        int calls = 0;
        IDisposable? registration = null;
        registration = GC.RegisterPauseDataCallback((_, _) =>
        {
            if (Volatile.Read(ref armed) == 0)
            {
                return;
            }

            Interlocked.Increment(ref calls);
            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            registration!.Dispose();
            completed.Set();
        });

        try
        {
            Volatile.Write(ref armed, 1);
            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            Assert.True(completed.Wait(TimeoutMilliseconds));
        }
        finally
        {
            registration.Dispose();
            GC.WaitForPendingFinalizers();
        }

        Assert.Equal(1, Volatile.Read(ref calls));
    }

    private static void VerifyOverflowAndNonblockingDisposal()
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        using ManualResetEventSlim disposed = new();
        using ManualResetEventSlim sawLoss = new();
        int armed = 0;
        int blocked = 0;
        int expired = 0;
        long missedTotal = 0;
        Thread? disposer = null;
        IDisposable blocker = GC.RegisterPauseDataCallback((_, _) =>
        {
            if (Volatile.Read(ref armed) != 0 && Interlocked.Exchange(ref blocked, 1) == 0)
            {
                entered.Set();
                if (!release.Wait(TimeoutMilliseconds * 3))
                {
                    Volatile.Write(ref expired, 1);
                }
            }
        });
        IDisposable observer = GC.RegisterPauseDataCallback((_, missed) =>
        {
            Interlocked.Add(ref missedTotal, missed);
            if (missed > 0)
            {
                sawLoss.Set();
            }
        });

        try
        {
            Volatile.Write(ref armed, 1);
            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            Assert.True(entered.Wait(TimeoutMilliseconds));
            for (int i = 0; i < OverflowCollections; i++)
            {
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
            }

            disposer = new Thread(() =>
            {
                blocker.Dispose();
                disposed.Set();
            });
            disposer.Start();
            Assert.True(disposed.Wait(TimeoutMilliseconds));
            release.Set();
            Assert.True(sawLoss.Wait(TimeoutMilliseconds));
            Assert.True(Interlocked.Read(ref missedTotal) >= OverflowCollections - BufferCapacity);
            Assert.Equal(0, Volatile.Read(ref expired));
        }
        finally
        {
            release.Set();
            blocker.Dispose();
            observer.Dispose();
            if (disposer is not null)
            {
                Assert.True(disposer.Join(TimeoutMilliseconds));
            }
            GC.WaitForPendingFinalizers();
        }
    }

    private static void VerifyHistogram()
    {
        using ManualResetEventSlim completed = new();
        using MeterListener listener = new();
        Instrument? histogram = null;
        int callbackThread = 0;
        double duration = -1;
        listener.InstrumentPublished = (instrument, subscriber) =>
        {
            if (instrument.Meter.Name == "System.Runtime.Detailed.GC" && instrument.Name == "dotnet.gc.pause.duration")
            {
                histogram = instrument;
                subscriber.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            string? generation = null;
            string? type = null;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == "gc.heap.generation")
                {
                    generation = tag.Value as string;
                }
                else if (tag.Key == "gc.pause.type")
                {
                    type = tag.Value as string;
                }
            }

            if (generation == "gen1" && type == "blocking")
            {
                duration = value;
                callbackThread = Environment.CurrentManagedThreadId;
                listener.DisableMeasurementEvents(instrument);
                completed.Set();
            }
        });

        try
        {
            listener.Start();
            Assert.NotNull(histogram);
            Assert.IsType<Histogram<double>>(histogram);
            Assert.Equal("s", histogram.Unit);
            GC.Collect(1, GCCollectionMode.Forced, blocking: true);
            Assert.True(completed.Wait(TimeoutMilliseconds));
            Assert.True(duration >= 0);
            Assert.Equal(Volatile.Read(ref s_finalizerThread), callbackThread);
            Assert.False(histogram.Enabled);
        }
        finally
        {
            listener.Dispose();
            GC.WaitForPendingFinalizers();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateFinalizableObject() => _ = new FinalizerMarker();

    private sealed class FinalizerMarker
    {
        ~FinalizerMarker() => Volatile.Write(ref s_finalizerThread, Environment.CurrentManagedThreadId);
    }
}
