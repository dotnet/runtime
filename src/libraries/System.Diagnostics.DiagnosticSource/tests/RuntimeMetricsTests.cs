// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Metrics.Tests
{
    public class RuntimeMetricsTests
    {
        [Fact]
        public async Task GcCollectionsCount()
        {
            using InstrumentRecorder<long> instrumentRecorder = new("dotnet.gc.collections.count");
            using CancellationTokenSource cts = new(1000);

            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            instrumentRecorder.RecordObservableInstruments();

            (bool success, IReadOnlyList<Measurement<long>> measurements) = await WaitForMeasurements(instrumentRecorder, 3, cts.Token);

            Assert.True(success, "Expected to receive at least 3 measurements.");

            int count = 0;
            for (int i = GC.MaxGeneration; i >= 0; i--)
            {
                Measurement<long> measurement = measurements[count++];
                Assert.True(measurement.Value > 0, $"Gen {i} count should be greater than zero.");

                var tags = measurement.Tags.ToArray();

                Assert.Equal(1, tags.Length);
                VerifyTag(tags, "gc.heap.generation", $"gen{i}");
            }
        }

        private static async Task<(bool, IReadOnlyList<Measurement<T>>)> WaitForMeasurements<T>(InstrumentRecorder<T> instrumentRecorder,
            int expected, CancellationToken cancellationToken) where T : struct
        {
            IReadOnlyList<Measurement<T>> measurements;
            while ((measurements = instrumentRecorder.GetMeasurements()).Count < 3)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return (false, measurements);
                }

                await Task.Delay(50, cancellationToken);
            }

            return (true, measurements);
        }

        private static void VerifyTag<T>(KeyValuePair<string, object?>[] tags, string name, T value)
        {
            if (value is null)
            {
                Assert.DoesNotContain(tags, t => t.Key == name);
            }
            else
            {
                Assert.Equal(value, (T)tags.Single(t => t.Key == name).Value);
            }
        }

        protected sealed class InstrumentRecorder<T> : IDisposable where T : struct
        {
            private readonly MeterListener _meterListener = new();
            private readonly ConcurrentQueue<Measurement<T>> _values = new();

            public InstrumentRecorder(string instrumentName)
            {
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

            private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
                _values.Enqueue(new Measurement<T>(measurement, tags));

            public IReadOnlyList<Measurement<T>> GetMeasurements() => _values.ToArray();

            public void RecordObservableInstruments() => _meterListener.RecordObservableInstruments();

            public void Dispose() => _meterListener.Dispose();
        }
    }
}
