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
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesWithEmptyMetadata()
        {
            RemoteExecutor.Invoke(async static () =>
            {
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("fi-FI");

                using Meter meter = new Meter(new MeterOptions("TestMeter1")
                {
                    Version = null,
                    Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                    Scope = new object(),
                    TelemetrySchemaUrl = "https://example.com"
                });
                Counter<int> c = meter.CreateCounter<int>("counter1");
                Gauge<int> g = meter.CreateGauge<int>("gauge1");
                int counterState = 3;
                ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
                int gaugeState = 0;
                ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
                Histogram<int> h = meter.CreateHistogram<int>("histogram1");
                UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1");
                int upDownCounterState = 0;
                ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState -= 11; return upDownCounterState; });

                EventWrittenEventArgs[] events;
                using (MetricsEventListener listener = new MetricsEventListener(NullTestOutputHelper.Instance, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter1"))
                {
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c.Add(5);
                    h.Record(19);
                    udc.Add(-33);
                    g.Record(200);
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(12);
                    h.Record(26);
                    udc.Add(-40);
                    g.Record(-200);
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events = listener.Events.ToArray();
                }

                AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
                AssertInitialEnumerationCompleteEventPresent(events);
                AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
                AssertGaugeEventsPresent(events, meter.Name, g.Name, "", "", "200", "-200");
                AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "10"), ("7", "17"));
                AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
                AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
                AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("-33", "-33"), ("-40", "-73"));
                AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "-11"), ("-11", "-22"));
                AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
            }).Dispose();
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourcePublishesTimeSeriesWithMetadata()
        {
            using Meter meter = new Meter("TestMeter2");
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            Gauge<int> g = meter.CreateGauge<int>("gauge1", "C", "Temperature", new TagList() { { "Ck1", "Cv1" } });
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe", new TagList() { { "oCk1", "oCv1" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!", new TagList() { { "ogk1", null } });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description", new TagList() { { "udCk1", "udCv1" }, { "udCk2", "udCv2" } });
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter2"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(77);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(-177);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", g.Unit, "77", "-177");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourcePublishesTimeSeriesForLateMeter()
        {
            // this ensures the MetricsEventSource exists when the listener tries to query
            using Meter dummy = new Meter("dummy");
            Meter meter = null;
            try
            {
                Counter<int> c;
                ObservableCounter<int> oc;
                ObservableGauge<int> og;
                Histogram<int> h;
                UpDownCounter<int> udc;
                ObservableUpDownCounter<int> oudc;
                Gauge<int> g;

                EventWrittenEventArgs[] events;
                using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter3"))
                {
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                    // the Meter is created after the EventSource was already monitoring
                    meter = new Meter("TestMeter3");
                    c = meter.CreateCounter<int>("counter1");
                    g = meter.CreateGauge<int>("gauge1");

                    int counterState = 3;
                    oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
                    int gaugeState = 0;
                    og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
                    h = meter.CreateHistogram<int>("histogram1");
                    udc = meter.CreateUpDownCounter<int>("upDownCounter1");
                    int upDownCounterState = 0;
                    oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState -= 11; return upDownCounterState; });

                    c.Add(5);
                    h.Record(19);
                    udc.Add(33);
                    g.Record(1);
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(12);
                    h.Record(26);
                    udc.Add(40);
                    g.Record(-1);
                    await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events = listener.Events.ToArray();
                }

                AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
                AssertInitialEnumerationCompleteEventPresent(events);
                AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
                AssertGaugeEventsPresent(events, meter.Name, g.Name, "", "", "1", "-1");
                AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "10"), ("7", "17"));
                AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
                AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
                AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("33", "33"), ("40", "73"));
                AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "-11"), ("-11", "-22"));
                AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
            }
            finally
            {
                meter?.Dispose();
            }
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourcePublishesTimeSeriesForLateInstruments()
        {
            // this ensures the MetricsEventSource exists when the listener tries to query
            using Meter meter = new Meter("TestMeter4");
            Counter<int> c;
            ObservableCounter<int> oc;
            ObservableGauge<int> og;
            Histogram<int> h;
            UpDownCounter<int> udc;
            ObservableUpDownCounter<int> oudc;
            Gauge<int> g;

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter4"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                // Instruments are created after the EventSource was already monitoring
                c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
                g = meter.CreateGauge<int>("gauge1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
                int counterState = 3;
                oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
                int gaugeState = 0;
                og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
                h = meter.CreateHistogram<int>("histogram1");
                udc = meter.CreateUpDownCounter<int>("upDownCounter1", null, null, new TagList() { { "udCk1", "udCv1" } });
                int upDownCounterState = 0;
                oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; });

                c.Add(5);
                h.Record(19);
                udc.Add(-33);
                g.Record(-1000);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(-40);
                g.Record(2000);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", "", "-1000", "2000");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "10"), ("7", "17"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("-33", "-33"), ("-40", "-73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "11"), ("11", "22"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourcePublishesTimeSeriesWithTags()
        {
            using Meter meter = new Meter("TestMeter5");
            Counter<int> c = meter.CreateCounter<int>("counter1");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () =>
            {
                counterState += 7;
                return new Measurement<int>[]
                {
                    new Measurement<int>(counterState,   new KeyValuePair<string,object?>("Color", "red"),  new KeyValuePair<string,object?>("Size", 19) ),
                    new Measurement<int>(2*counterState, new KeyValuePair<string,object?>("Color", "blue"), new KeyValuePair<string,object?>("Size", 4 ) )
                };
            });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () =>
            {
                gaugeState += 9;
                return new Measurement<int>[]
                {
                    new Measurement<int>(gaugeState,   new KeyValuePair<string,object?>("Color", "red"),  new KeyValuePair<string,object?>("Size", 19) ),
                    new Measurement<int>(2*gaugeState, new KeyValuePair<string,object?>("Color", "blue"), new KeyValuePair<string,object?>("Size", 4 ) )
                };
            });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1");
            Gauge<int> g = meter.CreateGauge<int>("gauge1");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () =>
            {
                upDownCounterState -= 11;
                return new Measurement<int>[]
                {
                    new Measurement<int>(upDownCounterState,   new KeyValuePair<string,object?>("Color", "red"),  new KeyValuePair<string,object?>("Size", 19) ),
                    new Measurement<int>(2*upDownCounterState, new KeyValuePair<string,object?>("Color", "blue"), new KeyValuePair<string,object?>("Size", 4 ) )
                };
            });

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter5"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                c.Add(5, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(6, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(19, new KeyValuePair<string, object?>("Size", 123));
                h.Record(20, new KeyValuePair<string, object?>("Size", 124));
                udc.Add(-33, new KeyValuePair<string, object?>("Color", "red"));
                udc.Add(-34, new KeyValuePair<string, object?>("Color", "blue"));
                g.Record(1, new KeyValuePair<string, object?>("Color", "black"));
                g.Record(2, new KeyValuePair<string, object?>("Color", "white"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                c.Add(12, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(13, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(26, new KeyValuePair<string, object?>("Size", 123));
                h.Record(27, new KeyValuePair<string, object?>("Size", 124));
                udc.Add(40, new KeyValuePair<string, object?>("Color", "red"));
                udc.Add(41, new KeyValuePair<string, object?>("Color", "blue"));
                g.Record(3, new KeyValuePair<string, object?>("Color", "black"));
                g.Record(4, new KeyValuePair<string, object?>("Color", "white"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=red", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=blue", "", ("6", "6"), ("13", "19"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "Color=black", "", "1", "3");
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "Color=white", "", "2", "4");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "Color=red,Size=19", "", ("", "10"), ("7", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "Color=blue,Size=4", "", ("", "20"), ("14", "34"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "Color=red,Size=19", "", "9", "18");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "Color=blue,Size=4", "", "18", "36");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Size=123", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Size=124", "", ("0.5=20;0.95=20;0.99=20", "1", "20"), ("0.5=27;0.95=27;0.99=27", "1", "27"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "Color=red", "", ("-33", "-33"), ("40", "7"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "Color=blue", "", ("-34", "-34"), ("41", "7"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "Color=red,Size=19", "", ("", "-11"), ("-11", "-22"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "Color=blue,Size=4", "", ("", "-22"), ("-22", "-44"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }


        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/79749", TargetFrameworkMonikers.NetFramework)]
        public async Task EventSourceFiltersInstruments()
        {
            object scope = new object();
            using Meter meterA = new Meter("TestMeterA", null, new TagList() { { "1Mk1", null } }, scope);
            using Meter meterB = new Meter(new MeterOptions("TestMeterB")
            {
                Version = null,
                Tags = new TagList() { { "2Mk1", "" } },
                Scope = scope,
                TelemetrySchemaUrl = "https://example.com"
            });
            using Meter meterC = new Meter("TestMeterC", null, new TagList() { { "3Mk1", "Mv1" }, { "3Mk2", "Mv2" } }, scope);
            Counter<int> c1a = meterA.CreateCounter<int>("counter1");
            Counter<int> c2a = meterA.CreateCounter<int>("counter2");
            Counter<int> c3a = meterA.CreateCounter<int>("counter3");
            Counter<int> c1b = meterB.CreateCounter<int>("counter1");
            Counter<int> c2b = meterB.CreateCounter<int>("counter2");
            Counter<int> c3b = meterB.CreateCounter<int>("counter3");
            Counter<int> c1c = meterC.CreateCounter<int>("counter1");
            Counter<int> c2c = meterC.CreateCounter<int>("counter2");
            Counter<int> c3c = meterC.CreateCounter<int>("counter3");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs,
                "TestMeterA\\counter3;TestMeterB\\counter1;TestMeterC\\counter2;TestMeterB;TestMeterC\\counter3"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                c1a.Add(1);
                c2a.Add(1);
                c3a.Add(1);
                c1b.Add(1);
                c2b.Add(1);
                c3b.Add(1);
                c1c.Add(1);
                c2c.Add(1);
                c3c.Add(1);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                c1a.Add(2);
                c2a.Add(2);
                c3a.Add(2);
                c1b.Add(2);
                c2b.Add(2);
                c3b.Add(2);
                c1c.Add(2);
                c2c.Add(2);
                c3c.Add(2);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c3a, c1b, c2b, c3b, c2c, c3c);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meterA.Name, c3a.Name, "", "", ("1", "1"), ("2", "3"));
            AssertCounterEventsPresent(events, meterB.Name, c1b.Name, "", "", ("1", "1"), ("2", "3"));
            AssertCounterEventsPresent(events, meterB.Name, c2b.Name, "", "", ("1", "1"), ("2", "3"));
            AssertCounterEventsPresent(events, meterB.Name, c3b.Name, "", "", ("1", "1"), ("2", "3"));
            AssertCounterEventsPresent(events, meterC.Name, c3c.Name, "", "", ("1", "1"), ("2", "3"));
            AssertCounterEventsPresent(events, meterC.Name, c3c.Name, "", "", ("1", "1"), ("2", "3"));
            AssertCounterEventsNotPresent(events, meterA.Name, c1a.Name, "");
            AssertCounterEventsNotPresent(events, meterA.Name, c2a.Name, "");
            AssertCounterEventsNotPresent(events, meterC.Name, c1c.Name, "");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourcePublishesMissingDataPoints()
        {
            using Meter meter = new Meter("TestMeter6", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            int counterState = 3;
            int counterCollectInterval = 0;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () =>
            {
                counterState += 7;
                counterCollectInterval++;
                if ((counterCollectInterval % 2) == 0)
                {
                    return new Measurement<int>[] { new Measurement<int>(counterState) };
                }
                else
                {
                    return new Measurement<int>[0];
                }
            });

            int gaugeState = 0;
            int gaugeCollectInterval = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () =>
            {
                gaugeState += 9;
                gaugeCollectInterval++;
                if ((gaugeCollectInterval % 2) == 0)
                {
                    return new Measurement<int>[] { new Measurement<int>(gaugeState) };
                }
                else
                {
                    return new Measurement<int>[0];
                }
            });

            Histogram<int> h = meter.CreateHistogram<int>("histogram1");

            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1");
            Gauge<int> g = meter.CreateGauge<int>("gauge1");
            int upDownCounterState = 0;
            int upDownCounterCollectInterval = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () =>
            {
                upDownCounterState += 11;
                upDownCounterCollectInterval++;
                if ((upDownCounterCollectInterval % 2) == 0)
                {
                    return new Measurement<int>[] { new Measurement<int>(upDownCounterState) };
                }
                else
                {
                    return new Measurement<int>[0];
                }
            });

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter6"))
            {
                // no measurements in interval 1
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(-123);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                // no measurements in interval 3
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(123);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                // no measurements in interval 5
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 5);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("0", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", "", "-123", "-123", "123", "123");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "17"), ("0", "17"), ("14", "31"), ("0", "31"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "18", "", "36", "");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("", "0", "0"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("", "0", "0"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("33", "33"), ("0", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "22"), ("0", "22"), ("22", "44"), ("0", "44"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 5);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourcePublishesEndEventsOnMeterDispose()
        {
            object scope = new object();
            using Meter meterA = new Meter("TestMeter8", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", null } }, scope);
            using Meter meterB = new Meter(new MeterOptions("TestMeter9")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", null }, { "Mk2", "Mv2" } },
                Scope = scope,
                TelemetrySchemaUrl = "https://example.com"
            });
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
            Gauge<int> g = meterA.CreateGauge<int>("gauge1", "C", "Temperature");
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" }, { "hk3", "hv3" } });
            UpDownCounter<int> udc = meterA.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meterA.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter8;TestMeter9"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                g.Record(9);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                g.Record(90);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                meterA.Dispose();
                await listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 3);

                h.Record(21);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meterA.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meterA.Name, g.Name, "", g.Unit, "9", "90");
            AssertCounterEventsPresent(events, meterA.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meterA.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meterB.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("0.5=21;0.95=21;0.99=21", "1", "21"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
            AssertEndInstrumentReportingEventsPresent(events, c, oc, og, udc, oudc, g);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesInstruments()
        {
            RemoteExecutor.Invoke(async static () =>
            {

                object scope = new object();

                using Meter meterA = new Meter("TestMeter-10", null, null, scope);
                using Meter meterB = new Meter(new MeterOptions("TestMeter-11")
                {
                    Version = null,
                    Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", null } },
                    Scope = scope,
                    TelemetrySchemaUrl = "https://example.com"
                });
                Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
                Gauge<int> g = meterA.CreateGauge<int>("gauge1", "C", "Temperature");
                int counterState = 3;
                ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe",
                                                new TagList() { { "ock1", "ocv1" }, { "ock2", "ocv2" }, { "ock3", "ocv3" } });
                int gaugeState = 0;
                ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!",
                                                new TagList() { { "ogk1", "ogv1" } });
                Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "" }, { "hk3", null } });
                UpDownCounter<int> udc = meterA.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description", new TagList() { { "udk1", "udv1" } });
                int upDownCounterState = 0;
                ObservableUpDownCounter<int> oudc = meterA.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

                EventWrittenEventArgs[] events;
                using (MetricsEventListener listener = new MetricsEventListener(NullTestOutputHelper.Instance, MetricsEventListener.InstrumentPublishing, null, ""))
                {
                    await listener.WaitForEnumerationComplete(s_waitForEventTimeout);
                    events = listener.Events.ToArray();
                }

                AssertInstrumentPublishingEventsPresent(events, ["TestMeter-10", "TestMeter-11"], c, oc, og, h, udc, oudc, g);
                AssertInitialEnumerationCompleteEventPresent(events);
            }).Dispose();
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourcePublishesAllDataTypes()
        {
            using Meter meter = new Meter("TestMeter12");
            Counter<int> i = meter.CreateCounter<int>("counterInt");
            Counter<short> s = meter.CreateCounter<short>("counterShort");
            Counter<byte> b = meter.CreateCounter<byte>("counterByte");
            Counter<long> l = meter.CreateCounter<long>("counterLong");
            Counter<decimal> dec = meter.CreateCounter<decimal>("counterDecimal");
            Counter<float> f = meter.CreateCounter<float>("counterFloat");
            Counter<double> d = meter.CreateCounter<double>("counterDouble");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter12"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                i.Add(1_234_567);
                s.Add(21_432);
                b.Add(1);
                l.Add(123_456_789_012);
                dec.Add(123_456_789_012_345);
                f.Add(123_456.789F);
                d.Add(5.25);

                i.Add(1);
                s.Add(1);
                b.Add(1);
                l.Add(1);
                dec.Add(1);
                f.Add(1);
                d.Add(1);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                i.Add(1_234_567);
                s.Add(21_432);
                b.Add(1);
                l.Add(123_456_789_012);
                dec.Add(123_456_789_012_345);
                f.Add(123_456.789F);
                d.Add(5.25);

                i.Add(1);
                s.Add(1);
                b.Add(1);
                l.Add(1);
                dec.Add(1);
                f.Add(1);
                d.Add(1);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, i, s, b, l, dec, f, d);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, i.Name, "", "", ("1234568", "1234568"), ("1234568", "2469136"));
            AssertCounterEventsPresent(events, meter.Name, s.Name, "", "", ("21433", "21433"), ("21433", "42866"));
            AssertCounterEventsPresent(events, meter.Name, b.Name, "", "", ("2", "2"), ("2", "4"));
            AssertCounterEventsPresent(events, meter.Name, l.Name, "", "", ("123456789013", "123456789013"), ("123456789013", "246913578026"));
            AssertCounterEventsPresent(events, meter.Name, dec.Name, "", "", ("123456789012346", "123456789012346"), ("123456789012346", "246913578024692"));
            AssertCounterEventsPresent(events, meter.Name, f.Name, "", "", ("123457.7890625", "123457.7890625"), ("123457.7890625", "246915.578125"));
            AssertCounterEventsPresent(events, meter.Name, d.Name, "", "", ("6.25", "6.25"), ("6.25", "12.5"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourceEnforcesTimeSeriesLimit()
        {
            using Meter meter = new Meter("TestMeter13");
            Counter<int> c = meter.CreateCounter<int>("counter1");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, 2, 50, "TestMeter13"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                c.Add(5, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(6, new KeyValuePair<string, object?>("Color", "blue"));
                c.Add(7, new KeyValuePair<string, object?>("Color", "green"));
                c.Add(8, new KeyValuePair<string, object?>("Color", "yellow"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                c.Add(12, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(13, new KeyValuePair<string, object?>("Color", "blue"));
                c.Add(14, new KeyValuePair<string, object?>("Color", "green"));
                c.Add(15, new KeyValuePair<string, object?>("Color", "yellow"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=red", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=blue", "", ("6", "6"), ("13", "19"));
            AssertTimeSeriesLimitPresent(events);
            AssertCounterEventsNotPresent(events, meter.Name, c.Name, "Color=green");
            AssertCounterEventsNotPresent(events, meter.Name, c.Name, "Color=yellow");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourceEnforcesHistogramLimit()
        {
            using Meter meter = new Meter("TestMeter14");
            Histogram<int> h = meter.CreateHistogram<int>("histogram1");


            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, 50, 2, "TestMeter14"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                h.Record(5, new KeyValuePair<string, object?>("Color", "red"));
                h.Record(6, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(7, new KeyValuePair<string, object?>("Color", "green"));
                h.Record(8, new KeyValuePair<string, object?>("Color", "yellow"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                h.Record(12, new KeyValuePair<string, object?>("Color", "red"));
                h.Record(13, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(14, new KeyValuePair<string, object?>("Color", "green"));
                h.Record(15, new KeyValuePair<string, object?>("Color", "yellow"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=red", "", ("0.5=5;0.95=5;0.99=5", "1", "5"), ("0.5=12;0.95=12;0.99=12", "1", "12"));
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=blue", "", ("0.5=6;0.95=6;0.99=6", "1", "6"), ("0.5=13;0.95=13;0.99=13", "1", "13"));
            AssertHistogramLimitPresent(events);
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=green");
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=yellow");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourceHandlesObservableCallbackException()
        {
            using Meter meter = new Meter("TestMeter15");
            Counter<int> c = meter.CreateCounter<int>("counter1");
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1",
                (Func<int>)(() => { throw new Exception("Example user exception"); }));

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter15"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertObservableCallbackErrorPresent(events);
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourceWorksWithSequentialListeners()
        {
            using Meter meter = new Meter("TestMeter16");
            Counter<int> c = meter.CreateCounter<int>("counter1");
            Gauge<int> g = meter.CreateGauge<int>("gauge1");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; });

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter16"))
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
                g.Record(10);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", "", "-10", "10");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "10"), ("7", "17"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "11"), ("11", "22"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);

            // Now create a new listener and do everything a 2nd time. Because the listener above has been disposed the source should be
            // free to accept a new connection.
            events = null;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter16"))
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
                g.Record(10);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, g);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertGaugeEventsPresent(events, meter.Name, g.Name, "", "", "-10", "10");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "31"), ("7", "38"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "36", "45");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "44"), ("11", "55"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task EventSourceEnforcesHistogramLimitAndNotMaxTimeSeries()
        {
            using Meter meter = new Meter(new MeterOptions("TestMeter17")
            {
                Version = null,
                Tags = new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } },
                Scope = null,
                TelemetrySchemaUrl = "https://example.com"
            });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", null, null, new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" } });

            EventWrittenEventArgs[] events;
            // MaxTimeSeries = 3, MaxHistograms = 2
            // HistogramLimitReached should be raised when Record(tags: "Color=green"), but TimeSeriesLimitReached should not be raised
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, 3, 2, "TestMeter17"))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                h.Record(5, new KeyValuePair<string, object?>("Color", "red"));
                h.Record(6, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(7, new KeyValuePair<string, object?>("Color", "green"));
                h.Record(8, new KeyValuePair<string, object?>("Color", "yellow"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                h.Record(12, new KeyValuePair<string, object?>("Color", "red"));
                h.Record(13, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(14, new KeyValuePair<string, object?>("Color", "green"));
                h.Record(15, new KeyValuePair<string, object?>("Color", "yellow"));
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=red", "", ("0.5=5;0.95=5;0.99=5", "1", "5"), ("0.5=12;0.95=12;0.99=12", "1", "12"));
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=blue", "", ("0.5=6;0.95=6;0.99=6", "1", "6"), ("0.5=13;0.95=13;0.99=13", "1", "13"));
            AssertHistogramLimitPresent(events);
            AssertTimeSeriesLimitNotPresent(events);
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=green");
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=yellow");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        public static IEnumerable<object[]> DifferentMetersAndInstrumentsData()
        {
            yield return new object[] { new Meter("M1").CreateCounter<int>("C1"), new Meter("M2").CreateCounter<int>("C2"), false };

            var counter = new Meter("M2").CreateCounter<int>("C3");
            yield return new object[] { counter, counter.Meter.CreateCounter<int>("C4"), false };

            // Same counters
            counter = new Meter("M3").CreateCounter<int>("C5");
            yield return new object[] { counter, counter, true };

            var scope = new object();
            yield return new object[]
            {
                new Meter("M4", "v1", new TagList { { "k1", "v1" } }, scope).CreateCounter<int>("C6", "u1", "d1", new TagList { { "k2", "v2" } } ),
                new Meter("M5", "v1", new TagList { { "k1", "v1" } }, scope).CreateCounter<int>("C7", "u1", "d1", new TagList { { "k2", "v2" } } ),
                false, // Same Instrument
            };

            Meter meter = new Meter("M6", "v1", new TagList { { "k1", "v1" } }, scope);
            yield return new object[] { meter.CreateCounter<int>("C8", "u1", "d1", new TagList { { "k2", "v2" } }), meter.CreateCounter<int>("C9", "u1", "d1", new TagList { { "k2", "v2" } }), false };
        }

        [Theory]
        [OuterLoop("Slow and has lots of console spew")]
        [MemberData(nameof(DifferentMetersAndInstrumentsData))]
        public async Task TestDifferentMetersAndInstruments(Counter<int> counter1, Counter<int> counter2, bool isSameCounters)
        {
            Assert.Equal(object.ReferenceEquals(counter1, counter2), isSameCounters);

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, counter1.Meter.Name, counter2.Meter.Name))
            {
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                counter1.Add(1);
                counter2.Add(1);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                events = listener.Events.ToArray();
            }

            var counterEvents = events.Where(e => e.EventName == "CounterRateValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Rate = e.Payload[6].ToString(),
                    Value = e.Payload[7].ToString(),
                    InstrumentId = (int)(e.Payload[8])
                }).ToArray();

            if (isSameCounters)
            {
                Assert.Equal(1, counterEvents.Length);
            }
            else
            {
                Assert.Equal(2, counterEvents.Length);
                Assert.NotEqual(counterEvents[0].InstrumentId, counterEvents[1].InstrumentId);
            }
        }

        [Fact]
        [OuterLoop("Slow and has lots of console spew")]
        public async Task HistogramBase2AggregationTest()
        {
            using Meter meter = new Meter("TestMeter20");
            Histogram<double> h = meter.CreateHistogram<double>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" }, { "hk3", "hv3" } });
            var labels = new KeyValuePair<string, object?>("Color", "red");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, new HistogramConfig(scale: 20, maxBuckets: 10, reportDeltas: false), "TestMeter20"))
            {
                // positive value
                h.Record(5.0, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                // small value
                h.Record(0.1, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                // large value
                h.Record(123456789.99, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                // zero value
                h.Record(0, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 4);

                // another zero value
                h.Record(0, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 5);

                // negative value
                h.Record(-5.0, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 6);

                h.Record(double.PositiveInfinity, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 7);

                h.Record(double.NegativeInfinity, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 8);

                h.Record(double.NaN, labels);
                await listener.WaitForCollectionStop(s_waitForEventTimeout, 9);

                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 9);

            AssertBase2HistogramEventsPresent(events, meter.Name, h.Name, "Color=red", h.Unit,
                        // (int Scale, double Sum, int Count, long ZeroCount, double Minimum, double Maximum, string Buckets)
                        [
                            (20, 5, 1, 0, 5, 5, "1"), // Record(5)
                            (0, 5.1, 2, 0, 0.1, 5, "1, 0, 0, 0, 0, 0, 1"), // Record(0.1)
                            (-2, 5.1 + 123456789.99, 3, 0, 0.1, 123456789.99, "1, 1, 0, 0, 0, 0, 0, 1"), // Record(123456789.99)
                            (-2, 5.1 + 123456789.99, 4, 1, 0, 123456789.99, "1, 1, 0, 0, 0, 0, 0, 1"), // Record(0)
                            (-2, 5.1 + 123456789.99, 5, 2, 0, 123456789.99, "1, 1, 0, 0, 0, 0, 0, 1"), // Record(0)
                            (-2, 5.1 + 123456789.99, 5, 2, 0, 123456789.99, "1, 1, 0, 0, 0, 0, 0, 1"), // Record(-5.0)
                            (-2, 5.1 + 123456789.99, 5, 2, 0, 123456789.99, "1, 1, 0, 0, 0, 0, 0, 1"), // Record(PositiveInfinity)
                            (-2, 5.1 + 123456789.99, 5, 2, 0, 123456789.99, "1, 1, 0, 0, 0, 0, 0, 1"), // Record(NegativeInfinity)
                            (-2, 5.1 + 123456789.99, 5, 2, 0, 123456789.99, "1, 1, 0, 0, 0, 0, 0, 1"), // Record(NaN)
                        ]);

            // Report Deltas = true
            using (MetricsEventListener listener1 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, new HistogramConfig(scale: 10, maxBuckets: 10, reportDeltas: true), "TestMeter20"))
            {
                h.Record(1.0, labels);
                h.Record(2.0, labels);
                h.Record(3.0, labels);
                h.Record(0, labels);
                h.Record(1234.5678, labels);
                h.Record(double.PositiveInfinity, labels);
                h.Record(double.NegativeInfinity, labels);
                h.Record(double.NaN, labels);
                await listener1.WaitForCollectionStop(s_waitForEventTimeout, 1);

                h.Record(1.0, labels);
                h.Record(2.0, labels);
                h.Record(3.0, labels);
                h.Record(0, labels);
                h.Record(1234.5678, labels);
                h.Record(double.PositiveInfinity, labels);
                h.Record(double.NegativeInfinity, labels);
                h.Record(double.NaN, labels);
                await listener1.WaitForCollectionStop(s_waitForEventTimeout, 2);

                events = listener1.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 2);

            AssertBase2HistogramEventsPresent(events, meter.Name, h.Name, "Color=red", h.Unit,
                        // (int Scale, double Sum, int Count, long ZeroCount, double Minimum, double Maximum, string Buckets)
                        [
                            (-1, 1240.5678, 5, 1, 0, 1234.5678, "1, 2, 0, 0, 0, 0, 1"), // Record(1.0, 2.0, 3.0, 0, 1234.5678, PositiveInfinity, NegativeInfinity, NaN)
                            (-1, 1240.5678, 5, 1, 0, 1234.5678, "1, 2, 0, 0, 0, 0, 1"), // Record(1.0, 2.0, 3.0, 0, 1234.5678, PositiveInfinity, NegativeInfinity, NaN)
                        ]);
        }
    }
}
