// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class MetricsSubscriptionManagerTests
    {
        [Fact]
        public void AddMetrics_InitializesListeners()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMetrics(); // Duplicate call, should not add things twice.
            serviceCollection.AddMetrics(l => l.AddListener<FakeListener>());
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Make sure the subscription manager is started.
            serviceProvider.GetRequiredService<IStartupValidator>().Validate();

            var listeners = serviceProvider.GetRequiredService<IEnumerable<IMetricsListener>>();

            var listener = Assert.Single(listeners);
            var fakeListener = Assert.IsType<FakeListener>(listener);
            Assert.Equal(1, fakeListener.InitializeCount);
        }

        private class FakeListener : IMetricsListener
        {
            public string Name => "Fake";
            public int InitializeCount { get; private set; }
            public MeasurementHandlers GetMeasurementHandlers() => new MeasurementHandlers();
            public void Initialize(IObservableInstrumentsSource source) => InitializeCount++;
            public bool InstrumentPublished(Instrument instrument, out object? userState) => throw new NotImplementedException();
            public void MeasurementsCompleted(Instrument instrument, object? userState) => throw new NotImplementedException();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestSubscriptionManagerDisposal()
        {
            var meter = new Meter("TestMeter");
            Counter<long> counter = null;

            var hostBuilder = new HostBuilder();

            hostBuilder.ConfigureServices(services =>
            {
                services.AddMetrics(metrics => metrics
                    .EnableMetrics("TestMeter")
                    .AddListener<TestMetricsListener>());
            });

            using (var host = hostBuilder.Build())
            {
                host.Start();
                counter = meter.CreateCounter<long>("TestCounter");
                Assert.Equal(1, TestMetricsListener.PublishCount);
            }

            // the host has been disposed. This should have signaled MeasurementsCompleted,
            // stopped the listener from receiving new notifications, and make the counter
            // report that it is no longer enabled
            Assert.False(counter.Enabled);
            Assert.Equal(1, TestMetricsListener.CompleteCount);
            var counter2 = meter.CreateCounter<long>("TestCounter2");
            Assert.Equal(1, TestMetricsListener.PublishCount);
        }

        private class TestMetricsListener : IMetricsListener
        {
            public static int PublishCount = 0;
            public static int CompleteCount = 0;
            public string Name => nameof(TestMetricsListener);

            public MeasurementHandlers GetMeasurementHandlers() => new();

            public void Initialize(IObservableInstrumentsSource source) { }

            public bool InstrumentPublished(Instrument instrument, out object userState)
            {
                PublishCount++;
                userState = null;
                return true;
            }

            public void MeasurementsCompleted(Instrument instrument, object userState)
            {
                CompleteCount++;
            }
        }
    }
}
