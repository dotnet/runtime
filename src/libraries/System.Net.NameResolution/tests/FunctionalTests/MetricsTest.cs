// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class MetricsTest
    {
        private const string DnsLookupDuration = "dns.lookup.duration";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task ResolveValidHostName_MetricsRecorded()
        {
            await RemoteExecutor.Invoke(async () =>
            {
                const string ValidHostName = "localhost";

                using var recorder = new InstrumentRecorder<double>(DnsLookupDuration);

                await Dns.GetHostEntryAsync(ValidHostName);
                await Dns.GetHostAddressesAsync(ValidHostName);

                Dns.GetHostEntry(ValidHostName);
                Dns.GetHostAddresses(ValidHostName);

                Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ValidHostName, null, null));
                Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(ValidHostName, null, null));

                double[] measurements = GetMeasurementsForHostname(recorder, ValidHostName);

                Assert.Equal(6, measurements.Length);
                Assert.All(measurements, m => Assert.True(m > double.Epsilon));
            }).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static async Task DurationHistogram_HasBucketSizeHints()
        {
            await RemoteExecutor.Invoke(async () =>
            {
                const string ValidHostName = "localhost";

                using var recorder = new InstrumentRecorder<double>(DnsLookupDuration);
                recorder.VerifyHistogramBucketBoundaries = b => Assert.True(b.Count > 2);

                await Dns.GetHostEntryAsync(ValidHostName);

                Assert.Equal(1, recorder.MeasurementCount);
            }).DisposeAsync();
        }

        [Fact]
        public static async Task ResolveInvalidHostName_MetricsRecorded()
        {
            const string InvalidHostName = $"invalid...example.com...{nameof(ResolveInvalidHostName_MetricsRecorded)}";

            using var recorder = new InstrumentRecorder<double>(DnsLookupDuration);

            await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostEntryAsync(InvalidHostName));
            await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostAddressesAsync(InvalidHostName));

            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(InvalidHostName));
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostAddresses(InvalidHostName));

            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostEntry(Dns.BeginGetHostEntry(InvalidHostName, null, null)));
            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(InvalidHostName, null, null)));

            double[] measurements = GetMeasurementsForHostname(recorder, InvalidHostName, "host_not_found");

            Assert.Equal(6, measurements.Length);
            Assert.All(measurements, m => Assert.True(m > double.Epsilon));
        }

        private static double[] GetMeasurementsForHostname(InstrumentRecorder<double> recorder, string hostname, string? expectedErrorType = null)
        {
            return recorder
                .GetMeasurements()
                .Where(m =>
                {
                    KeyValuePair<string, object?>[] tags = m.Tags.ToArray();
                    if (!tags.Any(t => t.Key == "dns.question.name" && t.Value is string hostnameTag && hostnameTag == hostname))
                    {
                        return false;
                    }
                    string? actualErrorType = tags.FirstOrDefault(t => t.Key == "error.type").Value as string;
                    return expectedErrorType == actualErrorType;
                })
                .Select(m => m.Value)
                .ToArray();
        }

        private sealed class InstrumentRecorder<T> : IDisposable where T : struct
        {
            private readonly MeterListener _meterListener = new();
            private readonly ConcurrentQueue<Measurement<T>> _values = new();

            public Action<IReadOnlyList<T>> VerifyHistogramBucketBoundaries;
            public int MeasurementCount => _values.Count;

            public InstrumentRecorder(string instrumentName)
            {
                _meterListener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "System.Net.NameResolution" && instrument.Name == instrumentName)
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
                if (VerifyHistogramBucketBoundaries is not null)
                {
                    Histogram<T> histogram = (Histogram<T>)instrument;
                    IReadOnlyList<T> boundaries = histogram.Advice.HistogramBucketBoundaries;
                    Assert.NotNull(boundaries);
                    VerifyHistogramBucketBoundaries(boundaries);
                }
            }
            public IReadOnlyList<Measurement<T>> GetMeasurements() => _values.ToArray();
            public void Dispose() => _meterListener.Dispose();
        }
    }
}
