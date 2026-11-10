// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

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

#if NET11_0_OR_GREATER
        public static bool CanTestPauseCallbacks => RemoteExecutor.IsSupported && (PlatformDetection.IsCoreCLR || PlatformDetection.IsNativeAot);

        [ConditionalTheory(typeof(RuntimeMetricsTests), nameof(CanTestPauseCallbacks))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void DetailedGcPauseDuration_ListenerLifecycle(int stopMode)
        {
            RemoteExecutor.Invoke(mode =>
            {
                using MeterListener listener = new();
                using ManualResetEventSlim recorded = new();
                Instrument? histogram = null;
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
                    bool generationFound = false;
                    bool typeFound = false;
                    foreach (KeyValuePair<string, object?> tag in tags)
                    {
                        generationFound |= tag.Key == "gc.heap.generation" && Equals(tag.Value, "gen1");
                        typeFound |= tag.Key == "gc.pause.type" && Equals(tag.Value, "blocking");
                    }

                    if (!generationFound || !typeFound)
                    {
                        return;
                    }

                    duration = value;
                    switch (mode)
                    {
                        case "0":
                            listener.DisableMeasurementEvents(instrument);
                            break;
                        case "1":
                            listener.Dispose();
                            break;
                        case "2":
                            instrument.Meter.Dispose();
                            break;
                    }
                    recorded.Set();
                });
                try
                {
                    listener.Start();
                    Assert.NotNull(histogram);
                    Assert.IsType<Histogram<double>>(histogram);
                    Assert.Equal("s", histogram.Unit);
                    GC.Collect(1, GCCollectionMode.Forced, blocking: true);
                    Assert.True(recorded.Wait(TimeSpan.FromSeconds(30)));
                    Assert.True(duration >= 0);
                    Assert.False(histogram.Enabled);
                }
                finally
                {
                    listener.Dispose();
                    GC.WaitForPendingFinalizers();
                }
            }, stopMode.ToString(System.Globalization.CultureInfo.InvariantCulture)).Dispose();
        }
#endif

        private sealed class RuntimeMeterException() : Exception { }

        private sealed class InstrumentRecorderException() : Exception { }

        private sealed class InstrumentRecorder<T> : IDisposable where T : struct
        {
            private readonly MeterListener _meterListener = new();
            private readonly ConcurrentQueue<Measurement<T>> _values = new();
            private readonly bool _injectException;

            public InstrumentRecorder(string instrumentName, bool injectException = false)
            {
                _injectException = injectException;
                _meterListener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == instrumentName)
                    {
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
