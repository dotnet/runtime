// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Diagnostics.Metrics.Tests
{
    public class MetricEventSourceTests
    {
        ITestOutputHelper _output;
        const double IntervalSecs = 5;
        static readonly TimeSpan s_waitForEventTimeout = TimeSpan.FromSeconds(60);

        public MetricEventSourceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesWithEmptyMetadata()
        {
            using Meter meter = new Meter("TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter1"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", "5", "12");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", "", "7");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesWithMetadata()
        {
            using Meter meter = new Meter("TestMeter2");
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; } , "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter2"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, "5", "12");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, "", "7", "7");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesForLateMeter()
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

                EventWrittenEventArgs[] events;
                using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter3"))
                {
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                    // the Meter is created after the EventSource was already monitoring
                    meter = new Meter("TestMeter3");
                    c = meter.CreateCounter<int>("counter1");
                    int counterState = 3;
                    oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
                    int gaugeState = 0;
                    og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
                    h = meter.CreateHistogram<int>("histogram1");

                    c.Add(5);
                    h.Record(19);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(12);
                    h.Record(26);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events = listener.Events.ToArray();
                }

                AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
                AssertInitialEnumerationCompleteEventPresent(events);
                AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", "5", "12");
                AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", "", "7");
                AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
                AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
                AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
            }
            finally
            {
                meter?.Dispose();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesForLateInstruments()
        {
            // this ensures the MetricsEventSource exists when the listener tries to query
            using Meter meter = new Meter("TestMeter4");
            Counter<int> c;
            ObservableCounter<int> oc;
            ObservableGauge<int> og;
            Histogram<int> h;

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter4"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                // Instruments are created after the EventSource was already monitoring
                c = meter.CreateCounter<int>("counter1");
                int counterState = 3;
                oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
                int gaugeState = 0;
                og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
                h = meter.CreateHistogram<int>("histogram1");

                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", "5", "12");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", "", "7");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesWithTags()
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

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter5"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                c.Add(5, new KeyValuePair<string,object?>("Color", "red"));
                c.Add(6, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(19, new KeyValuePair<string, object?>("Size", 123));
                h.Record(20, new KeyValuePair<string, object?>("Size", 124));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                c.Add(12, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(13, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(26, new KeyValuePair<string, object?>("Size", 123));
                h.Record(27, new KeyValuePair<string, object?>("Size", 124));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=red", "", "5", "12");
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=blue", "", "6", "13");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "Color=red,Size=19", "", "", "7");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "Color=blue,Size=4", "", "", "14");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "Color=red,Size=19", "", "9", "18");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "Color=blue,Size=4", "", "18", "36");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Size=123", "", "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Size=124", "", "0.5=20;0.95=20;0.99=20", "0.5=27;0.95=27;0.99=27");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourceFiltersInstruments()
        {
            using Meter meterA = new Meter("TestMeterA");
            using Meter meterB = new Meter("TestMeterB");
            using Meter meterC = new Meter("TestMeterC");
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                c1a.Add(1);
                c2a.Add(1);
                c3a.Add(1);
                c1b.Add(1);
                c2b.Add(1);
                c3b.Add(1);
                c1c.Add(1);
                c2c.Add(1);
                c3c.Add(1);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                c1a.Add(2);
                c2a.Add(2);
                c3a.Add(2);
                c1b.Add(2);
                c2b.Add(2);
                c3b.Add(2);
                c1c.Add(2);
                c2c.Add(2);
                c3c.Add(2);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c3a, c1b, c2b, c3b, c2c, c3c);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meterA.Name, c3a.Name, "", "", "1", "2");
            AssertCounterEventsPresent(events, meterB.Name, c1b.Name, "", "", "1", "2");
            AssertCounterEventsPresent(events, meterB.Name, c2b.Name, "", "", "1", "2");
            AssertCounterEventsPresent(events, meterB.Name, c3b.Name, "", "", "1", "2");
            AssertCounterEventsPresent(events, meterC.Name, c3c.Name, "", "", "1", "2");
            AssertCounterEventsPresent(events, meterC.Name, c3c.Name, "", "", "1", "2");
            AssertCounterEventsNotPresent(events, meterA.Name, c1a.Name, "");
            AssertCounterEventsNotPresent(events, meterA.Name, c2a.Name, "");
            AssertCounterEventsNotPresent(events, meterC.Name, c1c.Name, "");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesMissingDataPoints()
        {
            using Meter meter = new Meter("TestMeter6");
            Counter<int> c = meter.CreateCounter<int>("counter1");
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

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter6"))
            {
                // no measurements in interval 1
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                // no measurements in interval 3
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                c.Add(12);
                h.Record(26);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                // no measurements in interval 5
                listener.WaitForCollectionStop(s_waitForEventTimeout, 5);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", "5", "0", "12");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", "", "0", "14", "0");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "18", "", "36", "");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", "0.5=19;0.95=19;0.99=19", "", "0.5=26;0.95=26;0.99=26", "");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 5);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourceRejectsNewListener()
        {
            using Meter meter = new Meter("TestMeter7");
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);

                // some alternate listener attempts to listen in the middle
                using MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "ADifferentMeter");
                listener2.WaitForMultipleSessionsNotSupportedError(s_waitForEventTimeout);


                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, "5", "12");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, "", "7", "7");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesEndEventsOnMeterDispose()
        {
            using Meter meterA = new Meter("TestMeter8");
            using Meter meterB = new Meter("TestMeter9");
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter8;TestMeter9"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                meterA.Dispose();
                listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 3);

                h.Record(21);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meterA.Name, c.Name, "", c.Unit, "5", "12");
            AssertCounterEventsPresent(events, meterA.Name, oc.Name, "", oc.Unit, "", "7", "7");
            AssertGaugeEventsPresent(events, meterA.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meterB.Name, h.Name, "", h.Unit, "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26", "0.5=21;0.95=21;0.99=21");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
            AssertEndInstrumentReportingEventsPresent(events, c, oc, og);
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesInstruments()
        {
            using Meter meterA = new Meter("TestMeter10");
            using Meter meterB = new Meter("TestMeter11");
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.InstrumentPublishing, null, ""))
            {
                listener.WaitForEnumerationComplete(s_waitForEventTimeout);
                events = listener.Events.ToArray();
            }

            AssertInstrumentPublishingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesAllDataTypes()
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                i.Add(1_234_567);
                s.Add(21_432);
                b.Add(1);
                l.Add(123_456_789_012);
                dec.Add(123_456_789_012_345);
                f.Add(123_456.789F);
                d.Add(87_654_321_987_654.4);

                i.Add(1);
                s.Add(1);
                b.Add(1);
                l.Add(1);
                dec.Add(1);
                f.Add(1);
                d.Add(1);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                i.Add(1_234_567);
                s.Add(21_432);
                b.Add(1);
                l.Add(123_456_789_012);
                dec.Add(123_456_789_012_345);
                f.Add(123_456.789F);
                d.Add(87_654_321_987_654.4);

                i.Add(1);
                s.Add(1);
                b.Add(1);
                l.Add(1);
                dec.Add(1);
                f.Add(1);
                d.Add(1);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, i, s, b, l, dec, f, d);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, i.Name, "", "", "1234568", "1234568");
            AssertCounterEventsPresent(events, meter.Name, s.Name, "", "", "21433", "21433");
            AssertCounterEventsPresent(events, meter.Name, b.Name, "", "", "2", "2");
            AssertCounterEventsPresent(events, meter.Name, l.Name, "", "", "123456789013", "123456789013");
            AssertCounterEventsPresent(events, meter.Name, dec.Name, "", "", "123456789012346", "123456789012346");
            AssertCounterEventsPresent(events, meter.Name, f.Name, "", "", "123457.7890625", "123457.7890625");
            AssertCounterEventsPresent(events, meter.Name, d.Name, "", "", "87654321987655.4", "87654321987655.4");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourceEnforcesTimeSeriesLimit()
        {
            using Meter meter = new Meter("TestMeter13");
            Counter<int> c = meter.CreateCounter<int>("counter1");
            

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, 2, 50, "TestMeter13"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                c.Add(5, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(6, new KeyValuePair<string, object?>("Color", "blue"));
                c.Add(7, new KeyValuePair<string, object?>("Color", "green"));
                c.Add(8, new KeyValuePair<string, object?>("Color", "yellow"));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                c.Add(12, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(13, new KeyValuePair<string, object?>("Color", "blue"));
                c.Add(14, new KeyValuePair<string, object?>("Color", "green"));
                c.Add(15, new KeyValuePair<string, object?>("Color", "yellow"));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=red", "", "5", "12");
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=blue", "", "6", "13");
            AssertTimeSeriesLimitPresent(events);
            AssertCounterEventsNotPresent(events, meter.Name, c.Name, "Color=green");
            AssertCounterEventsNotPresent(events, meter.Name, c.Name, "Color=yellow");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourceEnforcesHistogramLimit()
        {
            using Meter meter = new Meter("TestMeter14");
            Histogram<int> h = meter.CreateHistogram<int>("histogram1");


            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, 50, 2, "TestMeter14"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                h.Record(5, new KeyValuePair<string, object?>("Color", "red"));
                h.Record(6, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(7, new KeyValuePair<string, object?>("Color", "green"));
                h.Record(8, new KeyValuePair<string, object?>("Color", "yellow"));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                h.Record(12, new KeyValuePair<string, object?>("Color", "red"));
                h.Record(13, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(14, new KeyValuePair<string, object?>("Color", "green"));
                h.Record(15, new KeyValuePair<string, object?>("Color", "yellow"));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=red", "", "0.5=5;0.95=5;0.99=5", "0.5=12;0.95=12;0.99=12");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=blue", "", "0.5=6;0.95=6;0.99=6", "0.5=13;0.95=13;0.99=13");
            AssertHistogramLimitPresent(events);
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=green");
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=yellow");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourceHandlesObservableCallbackException()
        {
            using Meter meter = new Meter("TestMeter15");
            Counter<int> c = meter.CreateCounter<int>("counter1");
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1",
                (Func<int>)(() => { throw new Exception("Example user exception"); }));

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter15"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", "5", "12");
            AssertObservableCallbackErrorPresent(events);
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourceWorksWithSequentialListeners()
        {
            using Meter meter = new Meter("TestMeter16");
            Counter<int> c = meter.CreateCounter<int>("counter1");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter16"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", "5", "12");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", "", "7");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);

            // Now create a new listener and do everything a 2nd time. Because the listener above has been disposed the source should be
            // free to accept a new connection.
            events = null;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter16"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", "5", "12");
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", "", "7");
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "36", "45");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", "0.5=19;0.95=19;0.99=19", "0.5=26;0.95=26;0.99=26");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        private void AssertBeginInstrumentReportingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var beginReportEvents = events.Where(e => e.EventName == "BeginInstrumentReporting").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString()
                }).ToArray();

            foreach(Instrument i in expectedInstruments)
            {
                var e = beginReportEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a BeginInstrumentReporting event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
            }

            Assert.Equal(expectedInstruments.Length, beginReportEvents.Length);
        }

        private void AssertEndInstrumentReportingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var beginReportEvents = events.Where(e => e.EventName == "EndInstrumentReporting").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString()
                }).ToArray();

            foreach (Instrument i in expectedInstruments)
            {
                var e = beginReportEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a EndInstrumentReporting event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
            }

            Assert.Equal(expectedInstruments.Length, beginReportEvents.Length);
        }

        private void AssertInitialEnumerationCompleteEventPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(1, events.Where(e => e.EventName == "InitialInstrumentEnumerationComplete").Count());
        }

        private void AssertTimeSeriesLimitPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(1, events.Where(e => e.EventName == "TimeSeriesLimitReached").Count());
        }

        private void AssertHistogramLimitPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(1, events.Where(e => e.EventName == "HistogramLimitReached").Count());
        }

        private void AssertInstrumentPublishingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var publishEvents = events.Where(e => e.EventName == "InstrumentPublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString()
                }).ToArray();

            foreach (Instrument i in expectedInstruments)
            {
                var e = publishEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a InstrumentPublished event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
            }

            Assert.Equal(expectedInstruments.Length, publishEvents.Length);
        }

        private void AssertCounterEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params string[] expectedRates)
        {
            var counterEvents = events.Where(e => e.EventName == "CounterRateValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Rate = e.Payload[6].ToString()
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expectedRates.Length);
            for (int i = 0; i < expectedRates.Length; i++)
            {
                Assert.Equal(expectedUnit, filteredEvents[i].Unit);
                Assert.Equal(expectedRates[i], filteredEvents[i].Rate);
            }
        }

        private void AssertCounterEventsNotPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags)
        {
            var counterEvents = events.Where(e => e.EventName == "CounterRateValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Tags = e.Payload[5].ToString()
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.Equal(0, filteredEvents.Length);
        }

        private void AssertGaugeEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params string[] expectedValues)
        {
            var counterEvents = events.Where(e => e.EventName == "GaugeValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Value = e.Payload[6].ToString(),
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expectedValues.Length);
            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedUnit, filteredEvents[i].Unit);
                Assert.Equal(expectedValues[i], filteredEvents[i].Value);
            }
        }

        private void AssertHistogramEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params string[] expectedQuantiles)
        {
            var counterEvents = events.Where(e => e.EventName == "HistogramValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Quantiles = (string)e.Payload[6]
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expectedQuantiles.Length);
            for (int i = 0; i < expectedQuantiles.Length; i++)
            {
                Assert.Equal(filteredEvents[i].Unit, expectedUnit);
                Assert.Equal(expectedQuantiles[i], filteredEvents[i].Quantiles);
            }
        }

        private void AssertHistogramEventsNotPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags)
        {
            var counterEvents = events.Where(e => e.EventName == "HistogramValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Tags = e.Payload[5].ToString()
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.Equal(0, filteredEvents.Length);
        }
        private void AssertCollectStartStopEventsPresent(EventWrittenEventArgs[] events, double expectedIntervalSecs, int expectedPairs)
        {
            int startEventsSeen = 0;
            int stopEventsSeen = 0;
            for(int i = 0; i < events.Length; i++)
            {
                EventWrittenEventArgs e = events[i];
                if(e.EventName == "CollectionStart")
                {
                    Assert.True(startEventsSeen == stopEventsSeen, "Unbalanced CollectionStart event");
                    startEventsSeen++;
                }
                else if(e.EventName == "CollectionStop")
                {
                    Assert.True(startEventsSeen == stopEventsSeen + 1, "Unbalanced CollectionStop event");
                    stopEventsSeen++;
                }
                else if (e.EventName == "CounterRateValuePublished" ||
                    e.EventName == "GaugeValuePublished" ||
                    e.EventName == "HistogramValuePublished")
                {
                    Assert.True(startEventsSeen == stopEventsSeen + 1, "Instrument value published outside collection interval");
                }
            }

            Assert.Equal(expectedPairs, startEventsSeen);
            Assert.Equal(expectedPairs, stopEventsSeen);
        }

        private void AssertObservableCallbackErrorPresent(EventWrittenEventArgs[] events)
        {
            var errorEvents = events.Where(e => e.EventName == "ObservableInstrumentCallbackError").Select(e =>
                new
                {
                    ErrorText = e.Payload[1].ToString(),
                }).ToArray();
            Assert.NotEmpty(errorEvents);
            Assert.Contains("Example user exception", errorEvents[0].ErrorText);
        }
    }

    class MetricsEventListener : EventListener
    {

        public const EventKeywords MessagesKeyword = (EventKeywords)0x1;
        public const EventKeywords TimeSeriesValues = (EventKeywords)0x2;
        public const EventKeywords InstrumentPublishing = (EventKeywords)0x4;


        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval, params string[]? instruments) :
            this(output, keywords, Guid.NewGuid().ToString(), refreshInterval, 50, 50, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, Guid.NewGuid().ToString(), refreshInterval, timeSeriesLimit, histogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, string sessionId, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, sessionId,
            FormatArgDictionary(refreshInterval,timeSeriesLimit, histogramLimit, instruments, sessionId))
        {
        }

        private static Dictionary<string,string> FormatArgDictionary(double? refreshInterval, int? timeSeriesLimit, int? histogramLimit, string?[]? instruments, string? sessionId)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            if(instruments != null)
            {
                d.Add("Metrics", string.Join(",", instruments));
            }
            if(refreshInterval.HasValue)
            {
                d.Add("RefreshInterval", refreshInterval.ToString());
            }
            if(sessionId != null)
            {
                d.Add("SessionId", sessionId);
            }
            if(timeSeriesLimit != null)
            {
                d.Add("MaxTimeSeries", timeSeriesLimit.ToString());
            }
            if (histogramLimit != null)
            {
                d.Add("MaxHistograms", histogramLimit.ToString());
            }
            return d;
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, string sessionId, Dictionary<string,string> arguments)
        {
            _output = output;
            _keywords = keywords;
            _sessionId = sessionId;
            _arguments = arguments;
            if (_source != null)
            {
                _output.WriteLine($"[{DateTime.Now:hh:mm:ss:fffff}] Enabling EventSource");
                EnableEvents(_source, EventLevel.Informational, _keywords, _arguments);
            }
        }

        ITestOutputHelper _output;
        EventKeywords _keywords;
        string _sessionId;
        Dictionary<string,string> _arguments;
        EventSource _source;
        AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        public List<EventWrittenEventArgs> Events { get; } = new List<EventWrittenEventArgs>();

        public string SessionId => _sessionId;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Diagnostics.Metrics")
            {
                _source = eventSource;
                if(_keywords != 0)
                {
                    _output.WriteLine($"[{DateTime.Now:hh:mm:ss:fffff}] Enabling EventSource");
                    EnableEvents(_source, EventLevel.Informational, _keywords, _arguments);
                }
            }
        }

        public override void Dispose()
        {
            if (_source != null)
            {
                // workaround for https://github.com/dotnet/runtime/issues/56378
                DisableEvents(_source);
            }
            base.Dispose();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            string sessionId = eventData.Payload[0].ToString();
            if (eventData.EventName != "MultipleSessionsNotSupportedError" && sessionId != "" && sessionId != _sessionId)
            {
                return;
            }
            lock (this)
            {
                Events.Add(eventData);
            }
            _output.WriteLine($"[{DateTime.Now:hh:mm:ss:fffff}] Event {eventData.EventName}");
            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                if(eventData.Payload[i] is DateTime)
                {
                    _output.WriteLine($"  {eventData.PayloadNames[i]}: {((DateTime)eventData.Payload[i]).ToLocalTime():hh:mm:ss:fffff}");
                }
                else
                {
                    _output.WriteLine($"  {eventData.PayloadNames[i]}: {eventData.Payload[i]}");
                }
                
            }
            _autoResetEvent.Set();
        }

        public void WaitForCollectionStop(TimeSpan timeout, int numEvents) => WaitForEvent(timeout, numEvents, "CollectionStop");

        public void WaitForEndInstrumentReporting(TimeSpan timeout, int numEvents) => WaitForEvent(timeout, numEvents, "EndInstrumentReporting");

        public void WaitForEnumerationComplete(TimeSpan timeout) => WaitForEvent(timeout, 1, "InitialInstrumentEnumerationComplete");

        public void WaitForMultipleSessionsNotSupportedError(TimeSpan timeout) => WaitForEvent(timeout, 1, "MultipleSessionsNotSupportedError");

        void WaitForEvent(TimeSpan timeout, int numEvents, string eventName)
        {
            DateTime startTime = DateTime.Now;
            DateTime stopTime = startTime + timeout;
            int initialEventCount = GetCountEvents(eventName);
            while (true)
            {
                if (GetCountEvents(eventName) >= numEvents)
                {
                    return;
                }
                TimeSpan remainingTime = stopTime - DateTime.Now;
                if (remainingTime.TotalMilliseconds < 0 || !_autoResetEvent.WaitOne(remainingTime))
                {
                    int currentEventCount = GetCountEvents(eventName);
                    throw new TimeoutException($"Timed out waiting for a {eventName} event. " +
                        $"StartTime={startTime} stopTime={stopTime} initialEventCount={initialEventCount} currentEventCount={currentEventCount} targetEventCount={numEvents}");
                }
            }
        }



        private void AssertOnError()
        {
            lock (this)
            {
                var errorEvent = Events.Where(e => e.EventName == "Error").FirstOrDefault();
                if (errorEvent != null)
                {
                    string message = errorEvent.Payload[1].ToString();
                    Assert.True(errorEvent == null, "Unexpected Error event: " + message);
                }
            }
        }

        private int GetCountEvents(string eventName)
        {
            lock (this)
            {
                AssertOnError();
                return Events.Where(e => e.EventName == eventName).Count();
            }
        }
    }
}
