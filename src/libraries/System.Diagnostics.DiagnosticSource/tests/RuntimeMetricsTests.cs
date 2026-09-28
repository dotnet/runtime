// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;
using PauseSample = (ulong Duration, ulong Index, int Generation, int Kind);

namespace System.Diagnostics.Metrics.Tests
{
    public class RuntimeMetricsTests(ITestOutputHelper output)
    {
        private const string GreaterThanZeroMessage = "Expected value to be greater than zero.";
        private const string GreaterThanOrEqualToZeroMessage = "Expected value to be greater than or equal to zero.";

        private static readonly string[] s_genNames = ["gen0", "gen1", "gen2", "loh", "poh"];

        // On some platforms and AoT scenarios, the JIT may not be in use. Some assertions will consider zero as a valid in such cases.
        private static bool s_jitHasRun = JitInfo.GetCompiledMethodCount() > 0;

        private static readonly Func<bool> s_forceGc = () =>
        {
            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            return GC.GetGCMemoryInfo().Index > 0;
        };

        private static readonly Func<long, (bool, string?)> s_longGreaterThanZero = v => v > 0
            ? (true, null)
            : (false, $"{GreaterThanZeroMessage} Actual value was: {v}.");

        private static readonly Func<long, (bool, string?)> s_longGreaterThanOrEqualToZero = v => v >= 0
            ? (true, null)
            : (false, $"{GreaterThanOrEqualToZeroMessage} Actual value was: {v}.");

        private static readonly Func<double, (bool, string?)> s_doubleGreaterThanZero = v => v > 0
            ? (true, null)
            : (false, $"{GreaterThanZeroMessage} Actual value was: {v}.");

        private static readonly Func<double, (bool, string?)> s_doubleGreaterThanOrEqualToZero = v => v >= 0
            ? (true, null)
            : (false, $"{GreaterThanOrEqualToZeroMessage} Actual value was: {v}.");

        private readonly ITestOutputHelper _output = output;

        public static bool IsCoreClrRemoteExecutorSupported => PlatformDetection.IsCoreCLR && RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDuration(bool serverGc)
        {
            RemoteInvokeOptions options = CreateGCPauseOptions(serverGc);
            RemoteExecutor.Invoke(static serverMode =>
            {
                Assert.Equal(bool.Parse(serverMode) && Environment.ProcessorCount > 1, GCSettings.IsServerGC);
                using InstrumentRecorder<double> recorder = new("dotnet.gc.pause.duration");
                Assert.Equal("s", Assert.IsType<Histogram<double>>(recorder.Instrument).Unit);

                for (int generation = 0; generation <= GC.MaxGeneration; generation++)
                {
                    GC.Collect(generation, GCCollectionMode.Forced, blocking: true);
                }

                WaitFor(() => recorder.Measurements.Select(m => PauseTag(m, "gc.heap.generation")).Distinct().Count() == 3);
                Measurement<double>[] measurements = recorder.Measurements;
                double totalPauseSeconds = GC.GetTotalPauseDuration().TotalSeconds;
                foreach (Measurement<double> measurement in measurements)
                {
                    Assert.Equal(2, measurement.Tags.Length);
                    Assert.True(PauseTag(measurement, "gc.heap.generation") is "gen0" or "gen1" or "gen2");
                    Assert.Equal("blocking", PauseTag(measurement, "gc.pause.type"));
                    Assert.False(double.IsNaN(measurement.Value));
                    Assert.InRange(measurement.Value, 0, totalPauseSeconds);
                }
            }, serverGc.ToString(), options).Dispose();
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDurationMatchesNativeAccounting(bool serverGc)
        {
            RemoteInvokeOptions options = CreateGCPauseOptions(serverGc);
            RemoteExecutor.Invoke(static () =>
            {
                using PauseRecorder recorder = new();
                for (int generation = 0; generation <= GC.MaxGeneration; generation++)
                {
                    GC.Collect(generation, GCCollectionMode.Forced, blocking: true);
                    GCMemoryInfo info = GC.GetGCMemoryInfo();
                    PauseSample sample = Assert.Single(recorder.WaitForCollection(info.Index));
                    Assert.Equal(info.Generation, sample.Generation);
                    Assert.Equal(0, sample.Kind);
                    Assert.Equal(info.PauseDurations[0].Ticks, checked((long)sample.Duration * 10));
                }
            }, options).Dispose();
        }

        private static RemoteInvokeOptions CreateGCPauseOptions(bool serverGc, bool concurrent = false)
        {
            RemoteInvokeOptions options = new();
            options.StartInfo.Environment["DOTNET_gcServer"] = serverGc ? "1" : "0";
            options.StartInfo.Environment["DOTNET_gcConcurrent"] = concurrent ? "1" : "0";
            return options;
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationListenerLifecycle()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using MeterListener first = new();
                using MeterListener second = new();
                Histogram<double>? histogram = null;
                ObservableCounter<long>? dropped = null;
                first.InstrumentPublished = (instrument, _) =>
                {
                    if (instrument.Meter.Name == "System.Runtime")
                    {
                        if (instrument.Name == "dotnet.gc.pause.duration")
                        {
                            histogram = Assert.IsType<Histogram<double>>(instrument);
                        }
                        else if (instrument.Name == "dotnet.gc.pause.dropped")
                        {
                            dropped = Assert.IsType<ObservableCounter<long>>(instrument);
                        }
                    }
                };
                first.Start();
                Assert.NotNull(histogram);
                Assert.NotNull(dropped);
                Assert.Equal("{measurement}", dropped.Unit);
                Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));

                first.EnableMeasurementEvents(dropped);
                first.RecordObservableInstruments();
                Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                first.EnableMeasurementEvents(histogram);
                Assert.Equal(1, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                first.EnableMeasurementEvents(histogram, new object());
                Assert.Equal(1, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                second.EnableMeasurementEvents(histogram);
                Assert.Equal(1, Volatile.Read(ref GetGCPauseRegistrationCount(null)));

                first.DisableMeasurementEvents(histogram);
                Assert.Equal(1, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                second.Dispose();
                Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));

                first.EnableMeasurementEvents(histogram);
                Assert.Equal(1, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                histogram.Meter.Dispose();
                Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationOverflow()
        {
            RemoteExecutor.Invoke(static () =>
            {
                const int Capacity = 4096;
                const int Collections = 2 * Capacity;
                using ManualResetEventSlim entered = new();
                using ManualResetEventSlim release = new();
                using ManualResetEventSlim returned = new();
                int delivered = 0;
                using IDisposable registration = RegisterGCPauseObserver(null, (duration, index, generation, kind) =>
                {
                    if (Interlocked.Increment(ref delivered) == 1)
                    {
                        entered.Set();
                        try
                        {
                            Assert.True(release.Wait(TimeSpan.FromSeconds(30)));
                        }
                        finally
                        {
                            returned.Set();
                        }
                    }
                });
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                Assert.True(entered.Wait(TimeSpan.FromSeconds(30)));
                try
                {
                    long initialDropped = GetDroppedGCPauseCount(null);
                    for (int i = 0; i < Collections; i++)
                    {
                        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                    }
                    long dropped = GetDroppedGCPauseCount(null) - initialDropped;
                    Assert.InRange(dropped, Collections - Capacity, Collections);
                }
                finally
                {
                    release.Set();
                    Assert.True(returned.Wait(TimeSpan.FromSeconds(30)));
                }

                Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref delivered) > 1, TimeSpan.FromSeconds(30)));
                long totalDropped = GetDroppedGCPauseCount(null);
                registration.Dispose();
                Assert.Equal(totalDropped, GetDroppedGCPauseCount(null));
                Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Register")]
        private static extern IDisposable RegisterGCPauseObserver(
            [UnsafeAccessorType("System.GCPauseReporting, System.Private.CoreLib")] object? target,
            Action<ulong, ulong, int, int> callback);

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDurationBackground(bool serverGc)
        {
            RemoteInvokeOptions options = CreateGCPauseOptions(serverGc, concurrent: true);
            if (serverGc)
            {
                // Keep heap-count changes from converting the nonblocking request into a blocking collection.
                options.StartInfo.Environment["DOTNET_GCHeapCount"] = "4";
                options.StartInfo.Environment["DOTNET_GCDynamicAdaptationMode"] = "0";
            }
            RemoteExecutor.Invoke(static () =>
            {
                GCPauseNode? root = null;
                for (int i = 0; i < 1_000_000; i++)
                {
                    root = new GCPauseNode(root);
                }
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                long previousIndex = GC.GetGCMemoryInfo(GCKind.Background).Index;
                using PauseRecorder recorder = new();

                long deadline = Environment.TickCount64 + 30_000;
                while (true)
                {
                    long blockingIndex = GC.GetGCMemoryInfo(GCKind.FullBlocking).Index;
                    GC.Collect(2, GCCollectionMode.Forced, blocking: false);
                    if (GC.GetGCMemoryInfo(GCKind.FullBlocking).Index == blockingIndex)
                    {
                        break;
                    }
                    Assert.True(Environment.TickCount64 < deadline, "The nonblocking request repeatedly selected a blocking collection.");
                }
                deadline = Environment.TickCount64 + 30_000;
                GCMemoryInfo info;
                do
                {
                    for (int i = 0; i < 1024; i++)
                    {
                        GC.KeepAlive(new byte[1024]);
                    }
                    info = GC.GetGCMemoryInfo(GCKind.Background);
                    if (info.Index <= previousIndex)
                    {
                        Assert.True(Environment.TickCount64 < deadline, "The background collection did not complete.");
                        Thread.Sleep(1);
                    }
                }
                while (info.Index <= previousIndex);

                Assert.True(info.Concurrent);
                Assert.Equal(2, info.Generation);
                PauseSample[] background = recorder.WaitForCollection(info.Index, minimumCount: 2);
                Assert.All(background, sample =>
                {
                    Assert.Equal(2, sample.Generation);
                    Assert.Equal(1, sample.Kind);
                });
                Assert.Contains(background, sample => checked((long)sample.Duration * 10) == info.PauseDurations[1].Ticks);
                Assert.Equal(0, GetDroppedGCPauseCount(null));
                GC.KeepAlive(root);
            }, options).Dispose();
        }

        private sealed class GCPauseNode(GCPauseNode? next)
        {
            internal readonly GCPauseNode? Next = next;
        }

        private static void WaitFor(Func<bool> condition) =>
            Assert.True(SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(30)), "Timed out waiting for GC pause delivery.");

        private static string PauseTag(Measurement<double> measurement, string key) =>
            Assert.IsType<string>(measurement.Tags.ToArray().Single(tag => tag.Key == key).Value);

        private sealed class PauseRecorder : IDisposable
        {
            private readonly ConcurrentQueue<PauseSample> _samples = new();
            private readonly IDisposable _registration;

            internal PauseRecorder() =>
                _registration = RegisterGCPauseObserver(null, (duration, index, generation, kind) =>
                    _samples.Enqueue((duration, index, generation, kind)));

            internal PauseSample[] WaitForCollection(long index, int minimumCount = 1)
            {
                WaitFor(() => _samples.Count(sample => sample.Index == (ulong)index) >= minimumCount);
                return _samples.Where(sample => sample.Index == (ulong)index).ToArray();
            }

            public void Dispose() => _registration.Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationRegistrationsDoNotReplayBacklog()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using ManualResetEventSlim entered = new();
                using ManualResetEventSlim release = new();
                int firstCalls = 0;
                ConcurrentQueue<ulong> secondIndices = new();
                using IDisposable first = RegisterGCPauseObserver(null, (duration, index, generation, kind) =>
                {
                    if (Interlocked.Increment(ref firstCalls) == 1)
                    {
                        entered.Set();
                        Assert.True(release.Wait(TimeSpan.FromSeconds(30)));
                    }
                });
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                Assert.True(entered.Wait(TimeSpan.FromSeconds(30)));
                IDisposable? second = null;
                try
                {
                    GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                    ulong oldIndex = (ulong)GC.GetGCMemoryInfo().Index;
                    second = RegisterGCPauseObserver(null, (duration, index, generation, kind) =>
                    {
                        secondIndices.Enqueue(index);
                    });
                    Assert.Equal(2, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                    GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                    ulong newIndex = (ulong)GC.GetGCMemoryInfo().Index;
                    release.Set();
                    Assert.True(SpinWait.SpinUntil(() => secondIndices.Contains(newIndex), TimeSpan.FromSeconds(30)));
                    Assert.DoesNotContain(oldIndex, secondIndices);
                    Assert.Contains(newIndex, secondIndices);
                    first.Dispose();
                    Assert.Equal(1, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                    second.Dispose();
                    Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                }
                finally
                {
                    release.Set();
                    second?.Dispose();
                }
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationCallbackCanCollectAndDispose()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using ManualResetEventSlim completed = new();
                using MeterListener listener = new();
                int entered = 0;
                listener.InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == "dotnet.gc.pause.duration")
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<double>((instrument, duration, tags, state) =>
                {
                    if (Interlocked.CompareExchange(ref entered, 1, 0) == 0)
                    {
                        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                        listener.Dispose();
                        completed.Set();
                    }
                });
                listener.Start();
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                Assert.True(completed.Wait(TimeSpan.FromSeconds(30)));
                Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationRapidResubscription()
        {
            RemoteExecutor.Invoke(static () =>
            {
                for (int i = 0; i < 32; i++)
                {
                    TaskCompletionSource<ulong> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    using (RegisterGCPauseObserver(null, (duration, index, generation, kind) => observed.TrySetResult(index)))
                    {
                        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                        Assert.True(observed.Task.Wait(TimeSpan.FromSeconds(30)), "A re-enabled producer did not wake its consumer.");
                    }
                    Assert.Equal(0, Volatile.Read(ref GetGCPauseRegistrationCount(null)));
                }
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetDroppedCount")]
        private static extern long GetDroppedGCPauseCount(
            [UnsafeAccessorType("System.GCPauseReporting, System.Private.CoreLib")] object? target);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "s_registrationCount")]
        private static extern ref int GetGCPauseRegistrationCount(
            [UnsafeAccessorType("System.GCPauseReporting, System.Private.CoreLib")] object? target);

        [Fact]
        public void GcCollectionsCount()
        {
            using InstrumentRecorder<long> instrumentRecorder = new("dotnet.gc.collections");

            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            instrumentRecorder.RecordObservableInstruments();

            bool[] foundGenerations = new bool[GC.MaxGeneration + 1];
            for (int i = 0; i < GC.MaxGeneration + 1; i++)
            {
                foundGenerations[i] = false;
            }

            var measurements = instrumentRecorder.GetMeasurements();

            var gensExpected = GC.MaxGeneration + 1;
            Assert.True(measurements.Count >= gensExpected, $"Expected to find at least one measurement for each generation ({gensExpected}) " +
                $"but received {measurements.Count} measurements.");

            foreach (Measurement<long> measurement in measurements)
            {
                var tags = measurement.Tags.ToArray();
                var tag = tags.SingleOrDefault(k => k.Key == "gc.heap.generation");

                if (tag.Key is not null)
                {
                    Assert.True(tag.Value is string, "Expected generation tag to be a string.");

                    string tagValue = (string)tag.Value;

                    switch (tagValue)
                    {
                        case "gen0":
                            foundGenerations[0] = true;
                            break;
                        case "gen1":
                            foundGenerations[1] = true;
                            break;
                        case "gen2":
                            foundGenerations[2] = true;
                            break;
                        default:
                            Assert.Fail($"Unexpected generation tag value '{tagValue}'.");
                            break;
                    }
                }
            }

            for (int i = 0; i < foundGenerations.Length; i++)
            {
                var generation = i switch
                {
                    0 => "gen0",
                    1 => "gen1",
                    2 => "gen2",
                    _ => throw new InvalidOperationException("Unexpected generation.")
                };

                Assert.True(foundGenerations[i], $"Expected to find a measurement for '{generation}'.");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        public void CpuTime()
        {
           using InstrumentRecorder<double> instrumentRecorder = new("dotnet.process.cpu.time");

           instrumentRecorder.RecordObservableInstruments();

           bool[] foundCpuModes = [false, false];

           foreach (Measurement<double> measurement in instrumentRecorder.GetMeasurements())
           {
               var tags = measurement.Tags.ToArray();
               var tag = tags.SingleOrDefault(k => k.Key == "cpu.mode");

               if (tag.Key is not null)
               {
                   Assert.True(tag.Value is string, "Expected CPU mode tag to be a string.");

                   string tagValue = (string)tag.Value;

                   switch (tagValue)
                   {
                       case "user":
                           foundCpuModes[0] = true;
                           break;
                       case "system":
                           foundCpuModes[1] = true;
                           break;
                       default:
                           Assert.Fail($"Unexpected CPU mode tag value '{tagValue}'.");
                           break;
                   }
               }
           }

           for (int i = 0; i < foundCpuModes.Length; i++)
           {
               var mode = i == 0 ? "user" : "system";
               Assert.True(foundCpuModes[i], $"Expected to find a measurement for '{mode}' CPU mode.");
           }
        }

        [Fact]
        public void ExceptionsCount()
        {
            // We inject an exception into the MeterListener callback here, so we can test that we don't recursively record exceptions.
            using InstrumentRecorder<long> instrumentRecorder = new("dotnet.exceptions", injectException: true);

            try
            {
                throw new RuntimeMeterException();
            }
            catch
            {
                // Ignore the exception.
            }

            var measurements = instrumentRecorder.GetMeasurements();

            AssertExceptions(measurements, 1);

            try
            {
                throw new RuntimeMeterException();
            }
            catch
            {
                // Ignore the exception.
            }

            measurements = instrumentRecorder.GetMeasurements();

            AssertExceptions(measurements, 2);

            static void AssertExceptions(IReadOnlyList<Measurement<long>> measurements, int expectedCount)
            {
                int foundExpectedExceptions = 0;
                int foundUnexpectedExceptions = 0;

                foreach (Measurement<long> measurement in measurements)
                {
                    var tags = measurement.Tags.ToArray();
                    var tag = tags.Single(k => k.Key == "error.type");

                    Assert.NotNull(tag.Key);
                    Assert.NotNull(tag.Value);

                    if (tag.Value is not string tagValue)
                    {
                        Assert.Fail("Expected error type tag to be a string.");
                        return;
                    }

                    if (tagValue == nameof(RuntimeMeterException))
                    {
                        foundExpectedExceptions++;
                    }
                    else if (tagValue == nameof(InstrumentRecorderException))
                    {
                        foundUnexpectedExceptions++;
                    }
                }

                Assert.Equal(expectedCount, foundExpectedExceptions);
                Assert.Equal(0, foundUnexpectedExceptions);
            }
        }

        public static IEnumerable<object[]> Measurements => new List<object[]>
        {
            new object[] { "dotnet.process.memory.working_set", s_longGreaterThanZero, null },
            new object[] { "dotnet.assembly.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.process.cpu.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.gc.heap.total_allocated", s_longGreaterThanZero, null },
            new object[] { "dotnet.gc.last_collection.memory.committed_size", s_longGreaterThanZero, s_forceGc },
            new object[] { "dotnet.gc.pause.time", s_doubleGreaterThanOrEqualToZero, s_forceGc }, // may be zero if no GC has occurred
            new object[] { "dotnet.jit.compiled_il.size", s_jitHasRun ? s_longGreaterThanZero : s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.jit.compiled_methods", s_jitHasRun ? s_longGreaterThanZero : s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.jit.compilation.time", s_jitHasRun ? s_doubleGreaterThanZero : s_doubleGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.monitor.lock_contentions", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.thread_pool.thread.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.thread_pool.work_item.count", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.thread_pool.queue.length", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.timer.count", s_longGreaterThanOrEqualToZero, null },
        };

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [MemberData(nameof(Measurements))]
        public void ValidateMeasurements<T>(string metricName, Func<T, (bool, string?)>? valueAssertion, Func<bool>? beforeRecord)
            where T : struct
        {
            ValidateSingleMeasurement(metricName, valueAssertion, beforeRecord);
        }

        private static void ValidateSingleMeasurement<T>(string metricName, Func<T, (bool, string?)>? valueAssertion = null, Func<bool>? beforeRecord = null)
            where T : struct
        {
            using InstrumentRecorder<T> instrumentRecorder = new(metricName);

            var shouldContinue = beforeRecord?.Invoke() ?? true;

            if (!shouldContinue)
                return;

            instrumentRecorder.RecordObservableInstruments();
            var measurements = instrumentRecorder.GetMeasurements();
            Assert.Single(measurements);

            if (valueAssertion is not null)
            {
                var (isExpected, message) = valueAssertion(measurements[0].Value);
                Assert.True(isExpected, message);
            }
        }

        [Theory]
        [InlineData("dotnet.gc.last_collection.heap.size")]
        [InlineData("dotnet.gc.last_collection.heap.fragmentation.size")]
        public void HeapTags(string metricName) => EnsureAllHeapTags(metricName);

        private void EnsureAllHeapTags(string metricName)
        {
            using InstrumentRecorder<long> instrumentRecorder = new(metricName);

            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            instrumentRecorder.RecordObservableInstruments();
            var measurements = instrumentRecorder.GetMeasurements();

            bool[] foundGenerations = new bool[s_genNames.Length];
            for (int i = 0; i < 5; i++)
            {
                foundGenerations[i] = false;
            }

            var gensExpected = GC.MaxGeneration + 1;
            Assert.True(measurements.Count >= gensExpected, $"Expected to find at least one measurement for each generation ({gensExpected}) " +
                $"but received {measurements.Count} measurements.");

            foreach (Measurement<long> measurement in measurements)
            {
                var tags = measurement.Tags.ToArray();
                var tag = tags.SingleOrDefault(k => k.Key == "gc.heap.generation");

                if (tag.Key is not null)
                {
                    Assert.True(tag.Value is string, "Expected generation tag to be a string.");

                    string tagValue = (string)tag.Value;

                    var index = Array.FindIndex(s_genNames, x => x == tagValue);

                    if (index == -1)
                        Assert.Fail($"Unexpected generation tag value '{tagValue}'.");

                    foundGenerations[index] = true;
                }
            }

            for (int i = 0; i < foundGenerations.Length; i++)
            {
                Assert.True(foundGenerations[i], $"Expected to find a measurement for '{s_genNames[i]}'.");
            }
        }

        [Fact]
        public void ThreadPoolQueueLengthIsUpDownCounter()
        {
            using MeterListener listener = new();
            Instrument? instrument = null;

            listener.InstrumentPublished = (inst, l) =>
            {
                if (inst.Meter.Name == "System.Runtime" && inst.Name == "dotnet.thread_pool.queue.length")
                {
                    instrument = inst;
                }
            };

            listener.Start();

            Assert.NotNull(instrument);
            Assert.IsType<ObservableUpDownCounter<long>>(instrument);
        }

        private sealed class RuntimeMeterException() : Exception { }

        private sealed class InstrumentRecorderException() : Exception { }

        private sealed class InstrumentRecorder<T> : IDisposable where T : struct
        {
            private readonly MeterListener _meterListener = new();
            private readonly ConcurrentQueue<Measurement<T>> _values = new();
            private readonly bool _injectException;

            public Instrument? Instrument { get; private set; }
            public Measurement<T>[] Measurements => _values.ToArray();

            public InstrumentRecorder(string instrumentName, bool injectException = false)
            {
                _injectException = injectException;
                _meterListener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == instrumentName)
                    {
                        Instrument = instrument;
                        listener.EnableMeasurementEvents(instrument);
                    }
                };
                _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
                _meterListener.Start();
            }

            private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
            {
                _values.Enqueue(new Measurement<T>(measurement, tags));

                if (_injectException)
                {
                    try
                    {
                        throw new InstrumentRecorderException();
                    }
                    catch
                    {
                        // Ignore the exception.
                    }
                }
            }

            public IReadOnlyList<Measurement<T>> GetMeasurements()
            {
                // Wait enough time for all the measurements to be enqueued via the
                // OnMeasurementRecorded callback. This value seems to be sufficient.
                Thread.Sleep(100);
                return _values.ToArray();
            }

            public void RecordObservableInstruments() => _meterListener.RecordObservableInstruments();

            public void Dispose() => _meterListener.Dispose();
        }
    }
}
