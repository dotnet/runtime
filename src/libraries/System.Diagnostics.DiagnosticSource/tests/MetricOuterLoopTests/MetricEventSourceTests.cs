// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Diagnostics.Metrics.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/95210", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
    public partial class MetricEventSourceTests
    {
        [Fact]
        public void GetInstanceMethodIsReflectable()
        {
            // The startup code in System.Private.CoreLib needs to be able to get the MetricsEventSource instance via reflection. See EventSource.InitializeDefaultEventSources() in
            // the System.Private.CoreLib source.
            // Even though the the type isn't public this test ensures the GetInstance() API isn't removed or renamed.
            Type? metricsEventSourceType = Type.GetType("System.Diagnostics.Metrics.MetricsEventSource, System.Diagnostics.DiagnosticSource", throwOnError: false);
            Assert.True(metricsEventSourceType != null, "Unable to get MetricsEventSource type via reflection");

            MethodInfo? getInstanceMethod = metricsEventSourceType.GetMethod("GetInstance", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            Assert.True(getInstanceMethod != null, "Unable to get MetricsEventSource.GetInstance method via reflection");

            object? o = getInstanceMethod.Invoke(null, null);
            Assert.True(o != null, "Expected non-null result invoking MetricsEventSource.GetInstance() via reflection");
            Assert.True(o is EventSource, "Expected object returned from MetricsEventSource.GetInstance() to be assignable to EventSource");
        }

        // Tests that version event from MetricsEventSource is fired.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestVersion()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using var meter = new Meter("test"); // we need this to ensure MetricsEventSource.Logger creation.

                using (var eventSourceListener = new MetricsEventListener(NullTestOutputHelper.Instance, EventKeywords.All, 60))
                {
                    var versionEvents = eventSourceListener.Events.Where(e => e.EventName == "Version");

                    Assert.Single(versionEvents);

                    var versionEvent = versionEvents.First();

                    var version = new Version(
                        (int)versionEvent.Payload[0],
                        (int)versionEvent.Payload[1],
                        (int)versionEvent.Payload[2]);

                    Assert.NotNull(version);
                    Assert.Equal(
                        new Version(typeof(Meter).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "0.0.0").ToString(3),
                        version.ToString());
                }
            }).Dispose();
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_DifferentCounters()
        {
            using Meter meter = new Meter("TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter(new MeterOptions("TestMeter2")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c2 = meter2.CreateCounter<int>("counter2");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c2.Add(5);
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c2.Add(12);
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events2 = listener2.Events.ToArray();
                }
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);

            AssertBeginInstrumentReportingEventsPresent(events2, c, c2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events2, meter2.Name, c2.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCollectStartStopEventsPresent(events2, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_ReuseCounter()
        {
            using Meter meter = new Meter("TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter(new MeterOptions("TestMeter2")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c2 = meter2.CreateCounter<int>("counter2", null, null, new TagList() { { "cCk1", "cCv1" }, { "cCk2", "cCv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1", "TestMeter2"))
                {
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c.Add(6);
                    c2.Add(5);
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(13);
                    c2.Add(12);
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events2 = listener2.Events.ToArray();
                }
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);

            AssertBeginInstrumentReportingEventsPresent(events2, c, c2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events2, meter.Name, c.Name, "", "", ("0", "17"), ("6", "23"), ("13", "36"));
            AssertCounterEventsPresent(events2, meter2.Name, c2.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCollectStartStopEventsPresent(events2, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_CollectAfterDisableListener()
        {
            using Meter meter = new Meter("TestMeter1", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter(new MeterOptions("TestMeter2")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c2 = meter2.CreateCounter<int>("counter2", null, null, new TagList() { { "cCk1", "cCv1" }, { "cCk2", "cCv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c2.Add(5);
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c2.Add(12);
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events2 = listener2.Events.ToArray();
                }

                await listener.WaitForCollectionStop(s_waitForEventTimeout, 7);
                c.Add(6);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 8);
                c.Add(13);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 9);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, c, c2);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"), ("0", "17"), ("0", "17"), ("0", "17"), ("0", "17"), ("6", "23"), ("13", "36"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 9);

            AssertBeginInstrumentReportingEventsPresent(events2, c, c2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events2, meter2.Name, c2.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCollectStartStopEventsPresent(events2, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_ThreeCounters()
        {
            using Meter meter = new Meter("TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1");

            using Meter meter2 = new Meter(new MeterOptions("TestMeter2")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c2 = meter2.CreateCounter<int>("counter2");

            using Meter meter3 = new Meter("TestMeter3", null, new TagList() { { "MMk1", null }, { "MMk2", null } }, new object());
            Counter<int> c3 = meter3.CreateCounter<int>("counter3");

            EventWrittenEventArgs[] events, events2, events3;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                events = listener.Events.ToArray();

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c2.Add(6);
                    await listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    events2 = listener2.Events.ToArray();

                    using (MetricsEventListener listener3 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter3"))
                    {
                        await listener3.WaitForCollectionStop(s_waitForEventTimeout, 1);
                        c3.Add(7);
                        await listener3.WaitForCollectionStop(s_waitForEventTimeout, 2);
                        events3 = listener3.Events.ToArray();
                    }
                }
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 2);

            AssertBeginInstrumentReportingEventsPresent(events2, c, c2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events2, meter2.Name, c2.Name, "", "", ("6", "6"));
            AssertCollectStartStopEventsPresent(events2, IntervalSecs, 2);

            AssertBeginInstrumentReportingEventsPresent(events3, c, c2, c3);
            AssertInitialEnumerationCompleteEventPresent(events3);
            AssertCounterEventsPresent(events3, meter3.Name, c3.Name, "", "", ("7", "7"));
            AssertCollectStartStopEventsPresent(events3, IntervalSecs, 2);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task SingleListener_Wildcard()
        {
            using Meter meter = new Meter("Test.TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1");

            using Meter meter2 = new Meter(new MeterOptions("Test.TestMeter2")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c2 = meter2.CreateCounter<int>("counter2");

            using Meter meter3 = new Meter("Test.TestMeter3", null, new TagList() { { "MMk1", null }, { "MMk2", null } }, new object());
            Counter<int> c3 = meter3.CreateCounter<int>("counter3");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "*"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                c2.Add(10);
                c3.Add(20);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                events = listener.Events.ToArray();
            }

            // Note: Need to exclude System.Runtime metrics any anything else in platform
            events = events.Where(e => e.EventName != "BeginInstrumentReporting"
                || (e.Payload[1] as string)?.StartsWith("Test.") == true)
                .ToArray();

            AssertBeginInstrumentReportingEventsPresent(events, c, c2, c3);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"));
            AssertCounterEventsPresent(events, meter2.Name, c2.Name, "", "", ("10", "10"));
            AssertCounterEventsPresent(events, meter3.Name, c3.Name, "", "", ("20", "20"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 2);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task SingleListener_Prefix()
        {
            using Meter meter = new Meter("Company1.TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1");

            using Meter meter2 = new Meter(new MeterOptions("Company1.TestMeter2")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c2 = meter2.CreateCounter<int>("counter2");

            using Meter meter3 = new Meter("Company2.TestMeter3", null, new TagList() { { "MMk1", null }, { "MMk2", null } }, new object());
            Counter<int> c3 = meter3.CreateCounter<int>("counter3");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "Company1*"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                c2.Add(10);
                c3.Add(20);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, c2);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"));
            AssertCounterEventsPresent(events, meter2.Name, c2.Name, "", "", ("10", "10"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 2);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_OverlappingListeners()
        {
            using Meter meter = new Meter("TestMeter1", null, new TagList() { { "Mk1", "Mv1" } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter(new MeterOptions("TestMeter2")
            {
                Version = null,
                Tags = null,
                Scope = null,
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c2 = meter2.CreateCounter<int>("counter2", null, null, new TagList() { { "cCk1", "cCv1" }, { "cCk2", "cCv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c.Add(5);
                    c2.Add(6);
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(12);
                    c2.Add(13);
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events = listener.Events.ToArray();
                    events2 = listener2.Events.ToArray();
                }
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, c, c2);
            AssertBeginInstrumentReportingEventsPresent(events2, c, c2);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter2.Name, c2.Name, "", "", ("6", "6"), ("13", "19"));
            AssertCounterEventsPresent(events2, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events2, meter2.Name, c2.Name, "", "", ("6", "6"), ("13", "19"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
            AssertCollectStartStopEventsPresent(events2, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_UnsharedSessionRejectsUnsharedListener()
        {
            using Meter meter = new Meter(new MeterOptions("TestMeter7")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe", new TagList() { { "ock1", "ocv1" }, { "ock2", "ocv2" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!", new TagList() { { "ogk1", "ogv1" } });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description", new TagList() { { "udck1", "udcv1" }, { "udck2", "udcv2" } });
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");
            Gauge<int> g = meter.CreateGauge<int>("gauge1", "C", "Temperature", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(-10);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(9);

                using MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7");
                await listener2.WaitForMultipleSessionsNotSupportedError(s_waitForEventTimeout);

                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", g.Unit, "-10", "9");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_UnsharedSessionRejectsSharedListener()
        {
            using Meter meter = new Meter(new MeterOptions("TestMeter7")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () =>
                    { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            Gauge<int> g = meter.CreateGauge<int>("gauge1", "C", "Temperature", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(-1);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(32);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter7"))
                {
                    await listener2.WaitForMultipleSessionsNotSupportedError(s_waitForEventTimeout);
                }

                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", g.Unit, "-1", "32");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_SharedSessionRejectsUnsharedListener()
        {
            using Meter meter = new Meter(new MeterOptions("TestMeter7")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!", new TagList());
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");
            Gauge<int> g = meter.CreateGauge<int>("gauge1", "C", "Temperature");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter7"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(100);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(-70);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7"))
                {
                    await listener2.WaitForMultipleSessionsNotSupportedError(s_waitForEventTimeout);
                }

                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", g.Unit, "100", "-70");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_SharedSessionRejectsListenerWithDifferentArgs()
        {
            using Meter meter = new Meter(new MeterOptions("TestMeter7")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                Scope = null,
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, 10, 12, "TestMeter7"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, 11, 13, "TestMeter7"))
                {
                    await listener2.WaitForMultipleSessionsConfiguredIncorrectlyError(s_waitForEventTimeout);
                    events2 = listener2.Events.ToArray();
                    AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(events2, "12", "13", "10", "11", IntervalSecs.ToString(), IntervalSecs.ToString());
                }

                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                c.Add(19);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"), ("19", "36"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        [ActiveIssue("This test appears to interfere with the others due to the session not being shut down.")]
        public async Task MultipleListeners_SharedSessionWithoutClientIdRejectsSharedListenerWithDifferentArgsAfterListenerDisposed()
        {
            using Meter meter = new Meter(new MeterOptions("TestMeter7")
            {
                Version = null,
                Tags = null,
                Scope = null,
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, string.Empty, isShared: true, IntervalSecs, 10, 12, "TestMeter7"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                c.Add(19);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, 11, 13, "TestMeter7"))
            {
                await listener2.WaitForMultipleSessionsConfiguredIncorrectlyError(s_waitForEventTimeout);
                events2 = listener2.Events.ToArray();
                AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(events2, "12", "13", "10", "11", IntervalSecs.ToString(), IntervalSecs.ToString());
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"), ("19", "36"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_SharedSessionRejectsListenerWithDifferentInterval()
        {
            using Meter meter = new Meter(new MeterOptions("TestMeter7")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", null }, { "Mk2", null } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe", new TagList() { { "Ck1", null }, { "Ck2", "" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" }, { "hk3", "hv3" } });
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");
            Gauge<int> g = meter.CreateGauge<int>("gauge1", "C", "Temperature");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter7"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(10);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs + 1, "TestMeter7"))
                {
                    await listener2.WaitForMultipleSessionsConfiguredIncorrectlyError(s_waitForEventTimeout);
                    events2 = listener2.Events.ToArray();
                    AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(events2, MetricsEventListener.HistogramLimit.ToString(), MetricsEventListener.HistogramLimit.ToString(),
                        MetricsEventListener.TimeSeriesLimit.ToString(), MetricsEventListener.TimeSeriesLimit.ToString(), IntervalSecs.ToString(), (IntervalSecs + 1).ToString());
                }

                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", g.Unit, "5", "10");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_DisposeMeterBeforeSecondListener()
        {
            using Meter meterA = new Meter("TestMeter8", null, null, new object());
            using Meter meterB = new Meter(new MeterOptions("TestMeter9")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                Scope = null,
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" } });
            UpDownCounter<int> udc = meterA.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meterA.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");
            Gauge<int> g = meterA.CreateGauge<int>("gauge1", "C", "Temperature");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter8;TestMeter9"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(-100);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(100);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                meterA.Dispose();
                await listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 3);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter8"))
                {
                    events2 = listener2.Events.ToArray();
                }

                h.Record(21);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, h, g); // only h occurs twice because meterA is disposed before listener2 is created
            AssertBeginInstrumentReportingEventsPresent(events2, h);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events, meterA.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meterA.Name, g.Name, "", g.Unit, "-100", "100");
            AssertCounterEventsPresent(events, meterA.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meterA.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meterB.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("0.5=21;0.95=21;0.99=21", "1", "21"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
            AssertEndInstrumentReportingEventsPresent(events, c, oc, og, udc, oudc, g);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_DisposeMetersDuringAndAfterSecondListener()
        {
            using Meter meterA = new Meter("TestMeter8", null, new TagList() { { "1Mk1", "1Mv1" }, { "1Mk2", "Mv2" } });
            using Meter meterB = new Meter(new MeterOptions("TestMeter9")
            {
                Version = null,
                Tags = new TagList() { { "2Mk1", "2Mv1" } },
                Scope = new object(),
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" } });
            Gauge<int> g = meterA.CreateGauge<int>("gauge1", "C", "Temperature", new TagList() { { "Ck1", "Cv1" } });
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meterA.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description", new TagList() { { "udCk1", "udCv1" }, { "udCk2", "udCv2" } });
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meterA.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter8;TestMeter9"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(-10);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(9);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter8;TestMeter9"))
                {
                    meterA.Dispose();
                    await listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 3);

                    events2 = listener2.Events.ToArray();
                }

                h.Record(21);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 4);

                meterB.Dispose();
                await listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 5);

                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g, c, oc, og, h, udc, oudc, g);
            AssertBeginInstrumentReportingEventsPresent(events2, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events, meterA.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meterA.Name, g.Name, "", g.Unit, "-10", "9");
            AssertCounterEventsPresent(events, meterA.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meterA.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meterB.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("0.5=21;0.95=21;0.99=21", "1", "21"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
            AssertEndInstrumentReportingEventsPresent(events, c, oc, og, udc, oudc, h, g);
            AssertEndInstrumentReportingEventsPresent(events2, c, oc, og, udc, oudc, g);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))] // time sensitive test
        [OuterLoop("Slow and has lots of console spew")]
        public async Task MultipleListeners_PublishingInstruments()
        {
            using Meter meterA = new Meter(new MeterOptions("TestMeter10")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" }, { "Mk3", null } },
                Scope = null,
                TelemetrySchemaUrl = "https://example.com"
            });
            using Meter meterB = new Meter("TestMeter11", null, null, new object());
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" } });
            Gauge<int> g = meterA.CreateGauge<int>("gauge1", "C", "Temperature", new TagList() { { "Ck1", "Cv1" } });
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meterA.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meterA.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.InstrumentPublishing, isShared: true, null, ""))
            {
                await listener.WaitForEnumerationComplete(s_waitForEventTimeout);
                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.InstrumentPublishing, isShared: true, null, ""))
                {
                    await listener2.WaitForEnumerationComplete(s_waitForEventTimeout);
                    events = listener.Events.ToArray();
                    events2 = listener2.Events.ToArray();
                }
            }

            AssertInstrumentPublishingEventsPresent(events, ["TestMeter10", "TestMeter11"], c, oc, og, h, udc, oudc, g, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertInstrumentPublishingEventsPresent(events2, ["TestMeter10", "TestMeter11"], c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events2);
        }
    }
}
