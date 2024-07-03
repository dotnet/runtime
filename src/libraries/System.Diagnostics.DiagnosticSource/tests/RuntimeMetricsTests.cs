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

            var token = cts.Token;
            token.Register(() => Assert.Fail("Timed out waiting for measurements."));

            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            instrumentRecorder.RecordObservableInstruments();

            (bool success, IReadOnlyList<Measurement<long>> measurements) = await WaitForMeasurements(instrumentRecorder, GC.MaxGeneration + 1, token);

            Assert.True(success, "Expected to receive at least 1 measurement per generation.");

            bool[] foundGenerations = new bool[GC.MaxGeneration + 1];
            for (int i = 0; i < GC.MaxGeneration + 1; i++)
            {
                foundGenerations[i] = false;
            }

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
                            Assert.Fail("Unexpected generation tag value.");
                            break;
                    }
                }
            }

            foreach (var found in foundGenerations)
            {
                Assert.True(found, "Expected to find a measurement for each generation (0, 1 and 2).");
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
