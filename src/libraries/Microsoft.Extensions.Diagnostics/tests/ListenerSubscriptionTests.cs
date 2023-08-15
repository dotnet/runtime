// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Tests
{
    public class ListenerSubscriptionTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SubscriptionReceivesNewGlobalMetersAndInstruments()
        {
            RemoteExecutor.Invoke(() =>
            {
                var publishedTcs = new TaskCompletionSource<Instrument>();
                var completedTcs = new TaskCompletionSource<Instrument>();
                var measurementTcs = new TaskCompletionSource<int>();

                var fakeListener = new FakeMetricListener();
                fakeListener.OnPublished = (instrument) =>
                {
                    Assert.Null(instrument.Meter.Scope); // Global
                    publishedTcs.TrySetResult(instrument);
                    return (true, null);
                };
                fakeListener.OnCompleted = (instrument, state) =>
                {
                    completedTcs.TrySetResult(instrument);
                };
                fakeListener.OnMeasurement = (instrument, measurement, tags, state) =>
                {
                    measurementTcs.TrySetResult((int)measurement);
                };

                var subscription = new ListenerSubscription(fakeListener, new FakeMeterFactory());
                Assert.Null(fakeListener.Source);

                subscription.Initialize();
                Assert.Same(subscription, fakeListener.Source);

                // No rules yet, so we shouldn't get any notifications.
                var meter = new Meter("TestMeter");
                var counter = meter.CreateCounter<int>("counter", "blip", "I count blips");
                counter.Add(1);

                Assert.False(publishedTcs.Task.IsCompleted);
                Assert.False(measurementTcs.Task.IsCompleted);

                // Add a rule that matches the counter.
                subscription.UpdateRules(new[] { new InstrumentRule("TestMeter", "counter", null, MeterScope.Global, enable: true) });

                Assert.True(publishedTcs.Task.IsCompleted);

                counter.Add(2);
                Assert.True(measurementTcs.Task.IsCompleted);
                Assert.Equal(2, measurementTcs.Task.Result);

                Assert.False(completedTcs.Task.IsCompleted);

                // Disable
                subscription.UpdateRules(new[] { new InstrumentRule("TestMeter", "counter", null, MeterScope.Global, enable: false) });

                Assert.True(completedTcs.Task.IsCompleted);

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SubscriptionReceivesNewLocalMetersAndInstruments()
        {
            RemoteExecutor.Invoke(() =>
            {
                var publishedTcs = new TaskCompletionSource<Instrument>();
                var completedTcs = new TaskCompletionSource<Instrument>();
                var measurementTcs = new TaskCompletionSource<int>();

                using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
                var factory = services.GetRequiredService<IMeterFactory>();

                var fakeListener = new FakeMetricListener();
                fakeListener.OnPublished = (instrument) =>
                {
                    Assert.Equal(factory, instrument.Meter.Scope); // Local
                    publishedTcs.TrySetResult(instrument);
                    return (true, null);
                };
                fakeListener.OnCompleted = (instrument, state) =>
                {
                    completedTcs.TrySetResult(instrument);
                };
                fakeListener.OnMeasurement = (instrument, measurement, tags, state) =>
                {
                    measurementTcs.TrySetResult((int)measurement);
                };

                var subscription = new ListenerSubscription(fakeListener, factory);
                Assert.Null(fakeListener.Source);

                subscription.Initialize();
                Assert.Same(subscription, fakeListener.Source);

                // No rules yet, so we shouldn't get any notifications.
                var meter = factory.Create("TestMeter");
                var counter = meter.CreateCounter<int>("counter", "blip", "I count blips");
                counter.Add(1);

                Assert.False(publishedTcs.Task.IsCompleted);
                Assert.False(measurementTcs.Task.IsCompleted);

                // Add a rule that matches the counter.
                subscription.UpdateRules(new[] { new InstrumentRule("TestMeter", "counter", null, MeterScope.Local, enable: true) });

                Assert.True(publishedTcs.Task.IsCompleted);

                counter.Add(2);
                Assert.True(measurementTcs.Task.IsCompleted);
                Assert.Equal(2, measurementTcs.Task.Result);

                Assert.False(completedTcs.Task.IsCompleted);

                // Disable
                subscription.UpdateRules(new[] { new InstrumentRule("TestMeter", "counter", null, MeterScope.Local, enable: false) });

                Assert.True(completedTcs.Task.IsCompleted);

            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void RuleCanBeTurnedOffAndOnAgain()
        {
            RemoteExecutor.Invoke(() =>
            {
                var publishCalled = 0;
                var onCompletedCalled = 0;
                var measurements = new List<int>();

                using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
                var factory = services.GetRequiredService<IMeterFactory>();

                var fakeListener = new FakeMetricListener();
                fakeListener.OnPublished = (instrument) =>
                {
                    publishCalled++;
                    Assert.Equal(factory, instrument.Meter.Scope); // Local
                    return (true, fakeListener);
                };
                fakeListener.OnCompleted = (instrument, state) =>
                {
                    onCompletedCalled++;
                    Assert.Equal(fakeListener, state);
                };
                fakeListener.OnMeasurement = (instrument, measurement, tags, state) =>
                {
                    measurements.Add((int)measurement);
                    Assert.Equal(fakeListener, state);
                };

                var subscription = new ListenerSubscription(fakeListener, factory);
                Assert.Null(fakeListener.Source);

                subscription.Initialize();
                Assert.Same(subscription, fakeListener.Source);

                // No rules yet, so we shouldn't get any notifications.
                var meter = factory.Create("TestMeter");
                var counter = meter.CreateCounter<int>("counter", "blip", "I count blips");
                counter.Add(1);

                Assert.Equal(0, measurements.Count);
                Assert.Equal(0, publishCalled);
                Assert.Equal(0, onCompletedCalled);

                // Add a rule that matches the counter.
                subscription.UpdateRules(new[] { new InstrumentRule("TestMeter", "counter", null, MeterScope.Local, enable: true) });

                Assert.Equal(1, publishCalled);

                counter.Add(2);
                Assert.Equal(1, measurements.Count);
                Assert.Equal(2, measurements[0]);
                Assert.Equal(0, onCompletedCalled);

                // Disable
                subscription.UpdateRules(new[] { new InstrumentRule("TestMeter", "counter", null, MeterScope.Local, enable: false) });

                Assert.Equal(1, onCompletedCalled);
                counter.Add(3);
                // Not received
                Assert.Equal(1, measurements.Count);

                // Re-enable
                subscription.UpdateRules(new[] { new InstrumentRule("TestMeter", "counter", null, MeterScope.Local, enable: true) });
                Assert.Equal(2, publishCalled);

                counter.Add(4);
                Assert.Equal(2, measurements.Count);
                Assert.Equal(4, measurements[1]);

                services.Dispose();
                Assert.Equal(2, onCompletedCalled);
            }).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        // [InlineData(null, null, null)] // RemoteExecutor can't handle nulls
        [InlineData("", "", "")]
        [InlineData("*", "", "")]
        [InlineData("lonG", "", "")]
        [InlineData("lonG.", "", "")]
        [InlineData("lonG*", "", "")]
        [InlineData("lonG.*", "", "")]
        [InlineData("lonG.sil", "", "")]
        [InlineData("lonG.sil*", "", "")]
        [InlineData("lonG.sillY.meteR", "", "")]
        [InlineData("lonG.sillY.meteR*", "", "")]
        [InlineData("lonG.sillY.meteR.*", "", "")]
        [InlineData("*namE", "", "")]
        [InlineData("*.namE", "", "")]
        [InlineData("*.sillY.meteR.Name", "", "")]
        [InlineData("long*Name", "", "")]
        [InlineData("lonG.sillY.meter*MeteR.namE", "", "")] // Shouldn't match, but does, left for compatibility with Logging.
        [InlineData("lonG.sillY.meteR.namE", "", "")]
        [InlineData("", "instrumenTnamE", "")]
        [InlineData("lonG.sillY.meteR.namE", "instrumenTnamE", "")]
        [InlineData("", "", "listeneRnamE")]
        [InlineData("lonG.sillY.meteR.namE", "", "listeneRnamE")]
        [InlineData("lonG.sillY.meteR.namE", "instrumenTnamE", "listeneRnamE")]
        public void RuleMatchesTest(string meterName, string instrumentName, string listenerName)
        {
            RemoteExecutor.Invoke((string m, string i, string l) => {
                var rule = new InstrumentRule(m, i, l, MeterScope.Global, enable: true);
                var meter = new Meter("Long.Silly.Meter.Name");
                var instrument = meter.CreateCounter<int>("InstrumentName");
                Assert.True(ListenerSubscription.RuleMatches(rule, instrument, "ListenerName", new FakeMeterFactory()));
            }, meterName, instrumentName, listenerName).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("", "*", "")]
        [InlineData("", "", "*")]
        [InlineData("sillY.meteR.namE", "", "")]
        [InlineData(".*", "", "")]
        [InlineData("*.", "", "")]
        [InlineData("lonG.sillY.meteR.namE.*", "", "")]
        [InlineData("namE", "", "")]
        [InlineData("wrongMeter", "", "")]
        [InlineData("wrongMeter", "InstrumentName", "")]
        [InlineData("wrongMeter", "", "ListenerName")]
        [InlineData("", "wrongInstrument", "")]
        [InlineData("", "", "wrongListener")]
        public void RuleMatchesNegativeTest(string meterName, string instrumentName, string listenerName)
        {
            RemoteExecutor.Invoke((string m, string i, string l) => {
                var rule = new InstrumentRule(m, i, l, MeterScope.Global, enable: true);
                var meter = new Meter("Long.Silly.Meter.Name");
                var instrument = meter.CreateCounter<int>("InstrumentName");
                Assert.False(ListenerSubscription.RuleMatches(rule, instrument, "ListenerName", new FakeMeterFactory()));
            }, meterName, instrumentName, listenerName).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MultipleWildcardsThrows()
        {
            RemoteExecutor.Invoke(() => {
                var rule = new InstrumentRule("*.*", null, null, MeterScope.Global, enable: true);
                var meter = new Meter("Long.Silly.Meter.Name");
                var instrument = meter.CreateCounter<int>("InstrumentName");
                Assert.Throws< InvalidOperationException>(() => ListenerSubscription.RuleMatches(rule, instrument, "ListenerName", new FakeMeterFactory()));
            }).Dispose();
        }

        [Theory]
        [MemberData(nameof(IsMoreSpecificTestData))]
        public void IsMoreSpecificTest(InstrumentRule rule, InstrumentRule? best, bool isLocalScope)
        {
            Assert.True(ListenerSubscription.IsMoreSpecific(rule, best, isLocalScope));

            if (best != null)
            {
                Assert.False(ListenerSubscription.IsMoreSpecific(best, rule, isLocalScope));
            }
        }

        public static IEnumerable<object[]> IsMoreSpecificTestData() => new object[][]
        {
            // Anything is better than null
            new object[] { new InstrumentRule(null, null, null, MeterScope.Global, true), null, false },

            // Any field is better than empty
            new object[] { new InstrumentRule("meterName", null, null, MeterScope.Global, true),
                new InstrumentRule(null, null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule(null, "instrumentName", null, MeterScope.Global, true),
                new InstrumentRule(null, null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule(null, null, "listenerName", MeterScope.Global, true),
                new InstrumentRule(null, null, null, MeterScope.Global, true), false },

            // Listener > Meter > Instrument
            new object[] { new InstrumentRule(null, null, "listenerName", MeterScope.Global, true),
                new InstrumentRule("meterName", null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule(null, "instrumentName", "listenerName", MeterScope.Global, true),
                new InstrumentRule("meterName", null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule(null, null, "listenerName", MeterScope.Global, true),
                new InstrumentRule(null, "instrumentName", null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName", null, null, MeterScope.Global, true),
                new InstrumentRule(null, "instrumentName", null, MeterScope.Global, true), false },

            // Multiple fields are better than one.
            new object[] { new InstrumentRule("meterName", "instrumentName", null, MeterScope.Global, true),
                new InstrumentRule("meterName", null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName", null, "listenerName", MeterScope.Global, true),
                new InstrumentRule("meterName", null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName", "instrumentName", "listenerName", MeterScope.Global, true),
                new InstrumentRule("meterName", null, null, MeterScope.Global, true), false },

            new object[] { new InstrumentRule("meterName", "instrumentName", null, MeterScope.Global, true),
                new InstrumentRule(null, "instrumentName", null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName", null, "listenerName", MeterScope.Global, true),
                new InstrumentRule(null, "instrumentName", null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName", "instrumentName", "listenerName", MeterScope.Global, true),
                new InstrumentRule(null, "instrumentName", null, MeterScope.Global, true), false },

            // Except Listener wins regardless
            new object[] { new InstrumentRule(null, null, "listenerName", MeterScope.Global, true),
                new InstrumentRule("meterName", "instrumentName", null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName", null, "listenerName", MeterScope.Global, true),
                new InstrumentRule(null, null, "listenerName", MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName", "instrumentName", "listenerName", MeterScope.Global, true),
                new InstrumentRule(null, null, "listenerName", MeterScope.Global, true), false },

            // Longer Meter Name is better
            new object[] { new InstrumentRule("meterName", null, null, MeterScope.Global, true),
                new InstrumentRule("*", null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meterName.*", null, null, MeterScope.Global, true),
                new InstrumentRule("meterName", null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meter.Name", null, null, MeterScope.Global, true),
                new InstrumentRule("meter", null, null, MeterScope.Global, true), false },
            new object[] { new InstrumentRule("meter.Name", null, null, MeterScope.Global, true),
                new InstrumentRule("meter.*", null, null, MeterScope.Global, true), false },

            // Scopes: Local > Global+Local, Global > Global+Local
            new object[] { new InstrumentRule(null, null, null, MeterScope.Local, true),
                new InstrumentRule(null, null, null, MeterScope.Global | MeterScope.Local, true), true },
            new object[] { new InstrumentRule(null, null, null, MeterScope.Global, true),
                new InstrumentRule(null, null, null, MeterScope.Global | MeterScope.Local, true), false },
        };

        [Fact]
        public void EqualMatchRulesTakeLastTest()
        {
            var emptyTrue = new InstrumentRule(null, null, null, MeterScope.Global, true);
            var emptyFalse = new InstrumentRule(null, null, null, MeterScope.Global, false);
            Assert.True(ListenerSubscription.IsMoreSpecific(emptyFalse, emptyTrue, isLocalScope: false));
            Assert.True(ListenerSubscription.IsMoreSpecific(emptyTrue, emptyFalse, isLocalScope: false));
        }

        public delegate void FakeMeasurementCallback(Instrument instrument, object measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state);
        private class FakeMetricListener : IMetricsListener
        {
            public string Name => "FakeListener";

            public Func<Instrument, (bool, object?)> OnPublished { get; set; } = (_) => (true, null);
            public Action<Instrument, object?> OnCompleted { get; set; } = (_, _) => { };
            public IObservableInstrumentsSource? Source { get; set; }
            public FakeMeasurementCallback OnMeasurement { get; set; }

            public MeasurementHandlers GetMeasurementHandlers() => new MeasurementHandlers
            {
                ByteHandler = CallOnMeasurement,
                ShortHandler = CallOnMeasurement,
                IntHandler = CallOnMeasurement,
                LongHandler = CallOnMeasurement,
                FloatHandler = CallOnMeasurement,
                DoubleHandler = CallOnMeasurement,
                DecimalHandler = CallOnMeasurement,
            };

            private void CallOnMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
            {
                OnMeasurement(instrument, measurement, tags, state);
            }

            public bool InstrumentPublished(Instrument instrument, out object? userState)
            {
                (var published, userState) = OnPublished(instrument);
                return published;
            }

            public void MeasurementsCompleted(Instrument instrument, object? userState) => OnCompleted(instrument, userState);
            public void Initialize(IObservableInstrumentsSource source) => Source = source;
        }

        private class FakeMeterFactory : IMeterFactory
        {
            public Meter Create(MeterOptions options) => throw new NotImplementedException();
            public void Dispose() => throw new NotImplementedException();
        }
    }
}

internal class SR
{
    public static string MoreThanOneWildcard => "More than one wildcard is not allowed in a rule.";
}
