// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace System.Diagnostics.Metrics.Tests
{
    public class RuntimeMetricsTests(ITestOutputHelper output)
    {
        private const string GreaterThanZeroMessage = "Expected value to be greater than zero.";
        private const string GreaterThanOrEqualToZeroMessage = "Expected value to be greater than or equal to zero.";

        private static readonly string[] s_genNames = ["gen0", "gen1", "gen2", "loh", "poh"];

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
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

            foreach (Measurement<long> measurement in measurements.Where(m => m.Value >= 1))
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

        // TODO - Uncomment once an implementation for https://github.com/dotnet/runtime/issues/104844 is available.
        //[Fact]
        //public void CpuTime()
        //{
        //    using InstrumentRecorder<double> instrumentRecorder = new("dotnet.process.cpu.time");

        //    instrumentRecorder.RecordObservableInstruments();

        //    bool[] foundCpuModes = [false, false];

        //    foreach (Measurement<double> measurement in instrumentRecorder.GetMeasurements().Where(m => m.Value >= 0))
        //    {
        //        var tags = measurement.Tags.ToArray();
        //        var tag = tags.SingleOrDefault(k => k.Key == "cpu.mode");

        //        if (tag.Key is not null)
        //        {
        //            Assert.True(tag.Value is string, "Expected CPU mode tag to be a string.");

        //            string tagValue = (string)tag.Value;

        //            switch (tagValue)
        //            {
        //                case "user":
        //                    foundCpuModes[0] = true;
        //                    break;
        //                case "system":
        //                    foundCpuModes[1] = true;
        //                    break;
        //                default:
        //                    Assert.Fail($"Unexpected CPU mode tag value '{tagValue}'.");
        //                    break;
        //            }
        //        }
        //    }

        //    for (int i = 0; i < foundCpuModes.Length; i++)
        //    {
        //        var mode = i == 0 ? "user" : "system";
        //        Assert.True(foundCpuModes[i], $"Expected to find a measurement for '{mode}' CPU mode.");
        //    }
        //}

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
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

        public static IEnumerable<object[]> LongMeasurements => new List<object[]>
        {
            new object[] { "dotnet.process.memory.working_set", s_longGreaterThanZero, null },
            new object[] { "dotnet.assembly.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.process.cpu.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.gc.heap.total_allocated", s_longGreaterThanZero, null },
            new object[] { "dotnet.gc.last_collection.memory.committed_size", s_longGreaterThanZero, s_forceGc },
            new object[] { "dotnet.gc.pause.time", s_doubleGreaterThanOrEqualToZero, s_forceGc }, // may be zero if no GC has occurred
            new object[] { "dotnet.jit.compiled_il.size", s_longGreaterThanZero, null },
            new object[] { "dotnet.jit.compiled_methods", s_longGreaterThanZero, null },
            new object[] { "dotnet.jit.compilation.time", s_doubleGreaterThanZero, null },
            new object[] { "dotnet.monitor.lock_contentions", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.thread_pool.thread.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.thread_pool.work_item.count", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.thread_pool.queue.length", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.timer.count", s_longGreaterThanOrEqualToZero, null },
        };

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [MemberData(nameof(LongMeasurements))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
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

            if (GC.GetGCMemoryInfo().Index == 0)
            {
                // No GC has occurred which can be the case on some platforms.
                Assert.Empty(measurements);
                return;
            }

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
