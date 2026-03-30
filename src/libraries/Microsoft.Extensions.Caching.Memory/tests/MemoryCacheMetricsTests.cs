// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
{
    public class MemoryCacheMetricsTests
    {
        [Fact]
        public void Constructor_WithMeterFactory_CreatesInstruments()
        {
            using var meterFactory = new TestMeterFactory();
            using var meterListener = new MeterListener();
            var measurements = new List<(string name, long value, KeyValuePair<string, object?>[] tags)>();

            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Microsoft.Extensions.Caching.Memory.MemoryCache")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
            {
                measurements.Add((instrument.Name, value, tags.ToArray()));
            });
            meterListener.Start();

            using var cache = new MemoryCache(
                new MemoryCacheOptions { TrackStatistics = true, Name = "test-cache" },
                loggerFactory: null,
                meterFactory: meterFactory);

            cache.Set("key", "value");
            cache.TryGetValue("key", out _);
            cache.TryGetValue("missing", out _);

            meterListener.RecordObservableInstruments();

            Assert.Contains(measurements, m =>
                m.name == "cache.requests" &&
                m.tags.Any(t => t.Key == "cache.request.type" && (string?)t.Value == "hit") &&
                m.tags.Any(t => t.Key == "cache.name" && (string?)t.Value == "test-cache"));

            Assert.Contains(measurements, m =>
                m.name == "cache.requests" &&
                m.tags.Any(t => t.Key == "cache.request.type" && (string?)t.Value == "miss") &&
                m.tags.Any(t => t.Key == "cache.name" && (string?)t.Value == "test-cache"));

            Assert.Contains(measurements, m => m.name == "cache.entries");
        }

        [Fact]
        public void Constructor_WithNullMeterFactory_StillTracksStatistics()
        {
            using var cache = new MemoryCache(
                new MemoryCacheOptions { TrackStatistics = true },
                loggerFactory: null,
                meterFactory: null);

            cache.Set("key", "value");
            cache.TryGetValue("key", out _);

            MemoryCacheStatistics? stats = cache.GetCurrentStatistics();
            Assert.NotNull(stats);
            Assert.Equal(1, stats.TotalHits);
        }

        [Fact]
        public void Constructor_WithNullMeterFactory_UsesSharedMeter()
        {
            using var meterListener = new MeterListener();
            var measurements = new List<(string name, long value)>();

            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Microsoft.Extensions.Caching.Memory.MemoryCache")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
            {
                measurements.Add((instrument.Name, value));
            });
            meterListener.Start();

            using var cache = new MemoryCache(
                new MemoryCacheOptions { TrackStatistics = true },
                loggerFactory: null,
                meterFactory: null);

            cache.Set("key", "value");
            cache.TryGetValue("key", out _);
            cache.Compact(1.0);

            meterListener.RecordObservableInstruments();

            Assert.Contains(measurements, m => m.name == "cache.requests");
            Assert.Contains(measurements, m => m.name == "cache.evictions");
            Assert.Contains(measurements, m => m.name == "cache.entries");
        }

        [Fact]
        public void Constructor_WithNullLoggerFactory_DoesNotThrow()
        {
            using var cache = new MemoryCache(
                new MemoryCacheOptions(),
                loggerFactory: null,
                meterFactory: null);

            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void TrackStatisticsFalse_NoInstrumentsCreated()
        {
            using var meterFactory = new TestMeterFactory();

            using var cache = new MemoryCache(
                new MemoryCacheOptions { TrackStatistics = false },
                loggerFactory: null,
                meterFactory: meterFactory);

            Assert.Empty(meterFactory.Meters);
        }

        [Fact]
        public void Name_DefaultsToDefault()
        {
            var options = new MemoryCacheOptions();
            Assert.Equal("Default", options.Name);
        }

        [Fact]
        public void Name_CanBeCustomized()
        {
            var options = new MemoryCacheOptions { Name = "my-cache" };
            Assert.Equal("my-cache", options.Name);
        }

        [Fact]
        public void EvictionInstrument_ReflectsCompaction()
        {
            using var meterFactory = new TestMeterFactory();
            using var meterListener = new MeterListener();
            var evictionMeasurements = new List<long>();

            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Microsoft.Extensions.Caching.Memory.MemoryCache" &&
                    instrument.Name == "cache.evictions")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
            {
                evictionMeasurements.Add(value);
            });
            meterListener.Start();

            using var cache = new MemoryCache(
                new MemoryCacheOptions { TrackStatistics = true },
                loggerFactory: null,
                meterFactory: meterFactory);

            for (int i = 0; i < 5; i++)
            {
                cache.Set($"key{i}", $"value{i}");
            }

            cache.Compact(1.0);

            meterListener.RecordObservableInstruments();

            Assert.Contains(evictionMeasurements, v => v == 5);
        }

        [Fact]
        public void DisposedCache_ReturnsNoMeasurements()
        {
            using var meterFactory = new TestMeterFactory();

            var cache = new MemoryCache(
                new MemoryCacheOptions { TrackStatistics = true },
                loggerFactory: null,
                meterFactory: meterFactory);

            Meter factoryMeter = meterFactory.Meters[0];

            using var meterListener = new MeterListener();
            var measurements = new List<(string name, long value)>();

            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, factoryMeter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
            {
                measurements.Add((instrument.Name, value));
            });
            meterListener.Start();

            cache.Set("key", "value");

            // Verify instruments publish before Dispose
            meterListener.RecordObservableInstruments();
            Assert.NotEmpty(measurements);

            measurements.Clear();
            cache.Dispose();

            // Verify instruments return empty after Dispose
            meterListener.RecordObservableInstruments();
            Assert.Empty(measurements);
        }

        [Fact]
        public void MeterOptions_IncludesCacheNameTag()
        {
            using var meterFactory = new TestMeterFactory();

            using var cache = new MemoryCache(
                new MemoryCacheOptions { TrackStatistics = true, Name = "my-cache" },
                loggerFactory: null,
                meterFactory: meterFactory);

            Assert.Single(meterFactory.Meters);
            Meter meter = meterFactory.Meters[0];
            Assert.Equal("Microsoft.Extensions.Caching.Memory.MemoryCache", meter.Name);
            Assert.Contains(meter.Tags, t => t.Key == "cache.name" && (string?)t.Value == "my-cache");
        }

        [Fact]
        public void EvictionCount_NotOvercounted_WhenEntryAlreadyRemoved()
        {
            using var meterFactory = new TestMeterFactory();
            using var meterListener = new MeterListener();
            var evictionMeasurements = new List<long>();

            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Microsoft.Extensions.Caching.Memory.MemoryCache" &&
                    instrument.Name == "cache.evictions")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
            {
                evictionMeasurements.Add(value);
            });
            meterListener.Start();

            var clock = new Microsoft.Extensions.Internal.TestClock();
            using var cache = new MemoryCache(
                new MemoryCacheOptions
                {
                    TrackStatistics = true,
                    Clock = clock,
                    ExpirationScanFrequency = TimeSpan.Zero
                },
                loggerFactory: null,
                meterFactory: meterFactory);

            // Add entries with short expiration
            for (int i = 0; i < 3; i++)
            {
                cache.Set($"key{i}", $"value{i}", new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = clock.UtcNow + TimeSpan.FromMinutes(1)
                });
            }

            // Advance time past expiration
            clock.Add(TimeSpan.FromMinutes(2));

            // Access triggers expiration removal for one entry
            cache.TryGetValue("key0", out _);

            // Compact tries to remove all (including already-removed key0)
            cache.Compact(1.0);

            MemoryCacheStatistics? stats = cache.GetCurrentStatistics();
            Assert.NotNull(stats);
            Assert.Equal(3, stats.TotalEvictions);

            meterListener.RecordObservableInstruments();
            Assert.Contains(evictionMeasurements, v => v == 3);
        }

        private sealed class TestMeterFactory : IMeterFactory
        {
            private readonly List<Meter> _meters = new();

            public IReadOnlyList<Meter> Meters => _meters;

            public Meter Create(MeterOptions options)
            {
                var meter = new Meter(options);
                _meters.Add(meter);
                return meter;
            }

            public void Dispose()
            {
                foreach (var meter in _meters)
                {
                    meter.Dispose();
                }
            }
        }
    }
}
#endif