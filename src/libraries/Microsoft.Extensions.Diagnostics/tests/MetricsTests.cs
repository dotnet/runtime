// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Metrics.Tests
{
    public class MetricsTests
    {
        [Fact]
        public void FactoryDITest()
        {
            ServiceCollection services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => sp.GetRequiredService<IMeterFactory>());

            services.AddMetrics();

            sp = services.BuildServiceProvider();
            using IMeterFactory meterFactory = sp.GetRequiredService<IMeterFactory>();
            Assert.NotNull(meterFactory);

            MeterOptions options = new MeterOptions("name")
            {
                Version = "version",
                Tags = new TagList() { { "key1", "value1" }, { "key2", "value2" } }
            };

            Meter meter1 = meterFactory.Create(options);
            Assert.Same(meterFactory, meter1.Scope);

            Meter meter2 = meterFactory.Create(options.Name, options.Version, options.Tags); // calling the extension method
            Assert.Same(meterFactory, meter2.Scope);

            Assert.Same(meter1, meter2);

            Meter meter3 = meterFactory.Create(options.Name, options.Version, new TagList() { { "key1", "value1" }, { "key2", "value1" } });
            Assert.Same(meterFactory, meter3.Scope);

            Assert.NotSame(meter1, meter3);

            //
            // Test meter creation with unordered tags
            //

            object o = new object();
            TagList l1 = new TagList() { { "z", "a" }, { "y", "b" }, { "x", "c" }, { "w", o }, { "N", null }, { "c", "d" }, { "q", "d" }, { "c", null } };
            List<KeyValuePair<string, object?>> l2 = new List<KeyValuePair<string, object?>>()
            {
                new KeyValuePair<string, object?>("y", "b"), new KeyValuePair<string, object?>("c", null), new KeyValuePair<string, object?>("N", null),
                new KeyValuePair<string, object?>("x", "c"), new KeyValuePair<string, object?>("w", o), new KeyValuePair<string, object?>("z", "a"),
                new KeyValuePair<string, object?>("c", "d"), new KeyValuePair<string, object?>("q", "d")
            };
            HashSet<KeyValuePair<string, object?>> l3 = new HashSet<KeyValuePair<string, object?>>()
            {
                new KeyValuePair<string, object?>("q", "d"), new KeyValuePair<string, object?>("c", null), new KeyValuePair<string, object?>("N", null),
                new KeyValuePair<string, object?>("w", o), new KeyValuePair<string, object?>("c", "d"), new KeyValuePair<string, object?>("x", "c"),
                new KeyValuePair<string, object?>("z", "a"), new KeyValuePair<string, object?>("y", "b")
            };

            Meter meter4 = meterFactory.Create("name4", "4", l1);
            Meter meter5 = meterFactory.Create("name4", "4", l2);
            Meter meter6 = meterFactory.Create("name4", "4", l3);

            Assert.Same(meter4, meter5);
            Assert.Same(meter4, meter6);

            KeyValuePair<string, object?>[] t1 = meter4.Tags.ToArray();
            Assert.Equal(l1.Count, t1.Length);
            t1[0] = new KeyValuePair<string, object?>(t1[0].Key, "newValue"); // change value of one item;
            Meter meter7 = meterFactory.Create("name4", "4", t1);
            Assert.NotSame(meter4, meter7);

            //
            // Ensure the tags in the meter are sorted
            //
            t1 = meter4.Tags.ToArray();
            for (int i = 0; i < t1.Length - 1; i++)
            {
                Assert.True(string.Compare(t1[i].Key, t1[i + 1].Key, StringComparison.Ordinal) <= 0);
            }
        }

        [Fact]
        public void NegativeTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMetrics();
            var sp = services.BuildServiceProvider();
            using IMeterFactory meterFactory = sp.GetRequiredService<IMeterFactory>();

            Assert.Throws<ArgumentNullException>(() => meterFactory.Create(name: null));
            Assert.Throws<InvalidOperationException>(() => meterFactory.Create(new MeterOptions("name") { Name = "SomeName", Scope = new object() }));

            Meter meter = meterFactory.Create(new MeterOptions("name") { Name = "SomeName", Scope = meterFactory });
            Assert.Equal(meterFactory, meter.Scope);
        }

        [Fact]
        public void MeterDisposeTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMetrics();
            var sp = services.BuildServiceProvider();
            IMeterFactory meterFactory = sp.GetRequiredService<IMeterFactory>();

            Meter meter = meterFactory.Create("DisposableMeter");

            Counter<int> counter = meter.CreateCounter<int>("MyCounter");
            InstrumentRecorder<int> recorder = new InstrumentRecorder<int>(counter);
            counter.Add(10);
            Assert.Equal(1, recorder.GetMeasurements().Count());
            Assert.Equal(10, recorder.GetMeasurements().ElementAt(0).Value);
            meter.Dispose(); // should be no-op
            counter.Add(20);
            Assert.Equal(2, recorder.GetMeasurements().Count());
            Assert.Equal(20, recorder.GetMeasurements().ElementAt(1).Value);

            meter.Dispose(); // dispose again, should be no-op too
            counter.Add(30);
            Assert.Equal(3, recorder.GetMeasurements().Count());
            Assert.Equal(30, recorder.GetMeasurements().ElementAt(2).Value);

            // Now dispose the factory, the meter should be disposed too
            meterFactory.Dispose();
            counter.Add(40); // recorder shouldn't observe this value as the meter created this instrument is disposed
            Assert.Equal(3, recorder.GetMeasurements().Count());
        }

        [Fact]
        public void InstrumentRecorderTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMetrics();
            var sp = services.BuildServiceProvider();
            using IMeterFactory meterFactory = sp.GetRequiredService<IMeterFactory>();

            MeterOptions options = new MeterOptions("name")
            {
                Version = "version",
                Tags = new TagList() { { "key1", "value1" }, { "key2", "value2" } }
            };

            Meter meter = meterFactory.Create("MyMeter", "1.0.0", new TagList() { { "key1", "value1" }, { "key2", "value2" } });
            Assert.Same(meterFactory, meter.Scope);

            Counter<int> counter = meter.CreateCounter<int>("MyCounter");

            InstrumentRecorder<int> recorder1 = new InstrumentRecorder<int>(counter);
            Assert.Same(counter, recorder1.Instrument);

            InstrumentRecorder<int> recorder2 = new InstrumentRecorder<int>(meter, "MyCounter");
            Assert.Same(counter, recorder2.Instrument);

            InstrumentRecorder<int> recorder3 = new InstrumentRecorder<int>(scopeFilter: meterFactory, "MyMeter", "MyCounter");
            Assert.Same(counter, recorder3.Instrument);

            counter.Add(100, new KeyValuePair<string, object?>("k", "v"));
            Assert.Equal(1, recorder1.GetMeasurements().Count());
            Assert.Equal(1, recorder2.GetMeasurements().Count());
            Assert.Equal(1, recorder3.GetMeasurements().Count());

            Assert.Equal(100, recorder1.GetMeasurements().ElementAt(0).Value);
            Assert.Equal(100, recorder2.GetMeasurements().ElementAt(0).Value);
            Assert.Equal(100, recorder2.GetMeasurements().ElementAt(0).Value);

            KeyValuePair<string, object?>[] tags = new KeyValuePair<string, object?>[] { new KeyValuePair<string, object?>("k", "v") };
            Assert.Equal(tags, recorder1.GetMeasurements().ElementAt(0).Tags.ToArray());
            Assert.Equal(tags, recorder2.GetMeasurements().ElementAt(0).Tags.ToArray());
            Assert.Equal(tags, recorder3.GetMeasurements().ElementAt(0).Tags.ToArray());
        }

        [Fact]
        public void CustomMeterFactoryTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.TryAddSingleton<IMeterFactory, NoCachingMeterFactory>();
            var sp = services.BuildServiceProvider();
            using IMeterFactory meterFactory = sp.GetRequiredService<IMeterFactory>();
            Assert.True(meterFactory is NoCachingMeterFactory);

            MeterOptions options = new MeterOptions("name")
            {
                Version = "version",
                Tags = new TagList() { { "key1", "value1" }, { "key2", "value2" } }
            };

            Meter meter1 = meterFactory.Create(options);
            Meter meter2 = meterFactory.Create(options);

            Meter meter3 = meterFactory.Create(options.Name, options.Version, options.Tags);

            Assert.NotSame(meter1, meter2);
            Assert.NotSame(meter1, meter3);
            Assert.NotSame(meter2, meter3);
        }

        public class NoCachingMeterFactory : IMeterFactory
        {
            List<Meter> _meterList = new List<Meter>();

            public Meter Create(MeterOptions options)
            {
                var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
                _meterList.Add(meter);
                return meter;
            }

            public void Dispose()
            {
                foreach (var meter in _meterList)
                {
                    meter.Dispose();
                }

                _meterList.Clear();
            }
        }
    }
}
