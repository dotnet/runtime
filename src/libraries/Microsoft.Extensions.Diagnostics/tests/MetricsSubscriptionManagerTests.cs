// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
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
    }
}
