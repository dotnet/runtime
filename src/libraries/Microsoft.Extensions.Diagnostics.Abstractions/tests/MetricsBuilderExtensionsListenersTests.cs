// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class MetricsBuilderExtensionsListenersTests
    {
        [Fact]
        public void CanAddListenersByType()
        {
            var services = new ServiceCollection();
            var builder = new FakeBuilder(services);

            builder.AddListener<FakeListenerA>();
            var container = services.BuildServiceProvider();
            Assert.IsType<FakeListenerA>(Assert.Single(container.GetServices<IMetricsListener>()));

            builder.AddListener<FakeListenerB>();
            container = services.BuildServiceProvider();
            Assert.Equal(2, container.GetServices<IMetricsListener>().Count());
        }

        [Fact]
        public void CanAddListenersByInstance()
        {
            var services = new ServiceCollection();
            var builder = new FakeBuilder(services);

            var instanceA = new FakeListenerA();
            builder.AddListener(instanceA);
            var container = services.BuildServiceProvider();
            Assert.Same(instanceA, Assert.Single(container.GetServices<IMetricsListener>()));

            var instanceB = new FakeListenerB();
            builder.AddListener(instanceB);
            container = services.BuildServiceProvider();
            var listeners = container.GetServices<IMetricsListener>().ToList();
            Assert.Equal(2, listeners.Count);
            Assert.Same(instanceA, listeners[0]);
            Assert.Same(instanceB, listeners[1]);
        }

        [Fact]
        public void CanClearListeners()
        {
            var services = new ServiceCollection();
            var builder = new FakeBuilder(services);

            builder.AddListener<FakeListenerA>();
            builder.AddListener(new FakeListenerB());
            var container = services.BuildServiceProvider();
            Assert.Equal(2, container.GetServices<IMetricsListener>().Count());

            builder.ClearListeners();
            container = services.BuildServiceProvider();
            Assert.Empty(container.GetServices<IMetricsListener>());
        }

        private class FakeBuilder(IServiceCollection services) : IMetricsBuilder
        {
            public IServiceCollection Services { get; } = services;
        }

        private class FakeListenerA : IMetricsListener
        {
            public string Name => "Fake";

            public MeasurementHandlers GetMeasurementHandlers() => throw new NotImplementedException();
            public void Initialize(IObservableInstrumentsSource source) => throw new NotImplementedException();
            public bool InstrumentPublished(Instrument instrument, out object? userState) => throw new NotImplementedException();
            public void MeasurementsCompleted(Instrument instrument, object? userState) => throw new NotImplementedException();
        }

        private class FakeListenerB : IMetricsListener
        {
            public string Name => "Fake";

            public MeasurementHandlers GetMeasurementHandlers() => throw new NotImplementedException();
            public void Initialize(IObservableInstrumentsSource source) => throw new NotImplementedException();
            public bool InstrumentPublished(Instrument instrument, out object? userState) => throw new NotImplementedException();
            public void MeasurementsCompleted(Instrument instrument, object? userState) => throw new NotImplementedException();
        }
    }
}
