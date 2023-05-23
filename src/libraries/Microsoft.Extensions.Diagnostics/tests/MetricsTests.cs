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
        }

        [Fact]
        public void NegativeTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMetrics();
            var sp = services.BuildServiceProvider();
            using IMeterFactory meterFactory = sp.GetRequiredService<IMeterFactory>();

            Assert.Throws<ArgumentNullException>(() => meterFactory.Create(name: null));
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
