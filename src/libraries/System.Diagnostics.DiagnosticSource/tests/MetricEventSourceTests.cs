// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Diagnostics.Metrics.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/95210", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
    public class MetricEventSourceTests
    {
        ITestOutputHelper _output;
        const double IntervalSecs = 10;
        static readonly TimeSpan s_waitForEventTimeout = TimeSpan.FromSeconds(60);

        public MetricEventSourceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_DifferentCounters()
        {
            using Meter meter = new Meter("TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter("TestMeter2", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
            Counter<int> c2 = meter2.CreateCounter<int>("counter2");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c2.Add(5);
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c2.Add(12);
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 3);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_ReuseCounter()
        {
            using Meter meter = new Meter("TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter("TestMeter2", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
            Counter<int> c2 = meter2.CreateCounter<int>("counter2", null, null, new TagList() { { "cCk1", "cCv1" }, { "cCk2", "cCv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1", "TestMeter2"))
                {
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c.Add(6);
                    c2.Add(5);
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(13);
                    c2.Add(12);
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 3);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_CollectAfterDisableListener()
        {
            using Meter meter = new Meter("TestMeter1", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter("TestMeter2", null, new TagList() { { "MMk1", "MMv1" } }, new object());
            Counter<int> c2 = meter2.CreateCounter<int>("counter2", null, null, new TagList() { { "cCk1", "cCv1" }, { "cCk2", "cCv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c2.Add(5);
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c2.Add(12);
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events2 = listener2.Events.ToArray();
                }

                listener.WaitForCollectionStop(s_waitForEventTimeout, 7);
                c.Add(6);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 8);
                c.Add(13);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 9);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_ThreeCounters()
        {
            using Meter meter = new Meter("TestMeter1");
            Counter<int> c = meter.CreateCounter<int>("counter1");

            using Meter meter2 = new Meter("TestMeter2", null, new TagList() { { "Mk1", "Mv1" } }, new object());
            Counter<int> c2 = meter2.CreateCounter<int>("counter2");

            using Meter meter3 = new Meter("TestMeter3", null, new TagList() { { "MMk1", null }, { "MMk2", null } }, new object());
            Counter<int> c3 = meter3.CreateCounter<int>("counter3");

            EventWrittenEventArgs[] events, events2, events3;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                events = listener.Events.ToArray();

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c2.Add(6);
                    listener2.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    events2 = listener2.Events.ToArray();

                    using (MetricsEventListener listener3 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter3"))
                    {
                        listener3.WaitForCollectionStop(s_waitForEventTimeout, 1);
                        c3.Add(7);
                        listener3.WaitForCollectionStop(s_waitForEventTimeout, 2);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_OverlappingListeners()
        {
            using Meter meter = new Meter("TestMeter1", null, new TagList() { { "Mk1", "Mv1" } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            using Meter meter2 = new Meter("TestMeter2");
            Counter<int> c2 = meter2.CreateCounter<int>("counter2", null, null, new TagList() { { "cCk1", "cCv1" }, { "cCk2", "cCv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter1"))
            {
                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter2"))
                {
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c.Add(5);
                    c2.Add(6);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(12);
                    c2.Add(13);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_UnsharedSessionRejectsUnsharedListener()
        {
            using Meter meter = new Meter("TestMeter7", null, new TagList() { { "Mk1", "Mv1" } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe", new TagList() { { "ock1", "ocv1" }, { "ock2", "ocv2" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!", new TagList() { { "ogk1", "ogv1" } });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description", new TagList() { { "udck1", "udcv1" }, { "udck2", "udcv2" } });
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);

                using MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7");
                listener2.WaitForMultipleSessionsNotSupportedError(s_waitForEventTimeout);


                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_UnsharedSessionRejectsSharedListener()
        {
            using Meter meter = new Meter("TestMeter7", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
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

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter7"))
                {
                    listener2.WaitForMultipleSessionsNotSupportedError(s_waitForEventTimeout);
                }

                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_SharedSessionRejectsUnsharedListener()
        {
            using Meter meter = new Meter("TestMeter7", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe");
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!", new TagList());
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter7"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter7"))
                {
                    listener2.WaitForMultipleSessionsNotSupportedError(s_waitForEventTimeout);
                }

                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_SharedSessionRejectsListenerWithDifferentArgs()
        {
            using Meter meter = new Meter("TestMeter7", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } });
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, 10, 12, "TestMeter7"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, 11, 13, "TestMeter7"))
                {
                    listener2.WaitForMultipleSessionsConfiguredIncorrectlyError(s_waitForEventTimeout);
                    events2 = listener2.Events.ToArray();
                    AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(events2, "12", "13", "10", "11", IntervalSecs.ToString(), IntervalSecs.ToString());
                }

                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                c.Add(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"), ("19", "36"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        [ActiveIssue("This test appears to interfere with the others due to the session not being shut down.")]
        public void MultipleListeners_SharedSessionWithoutClientIdRejectsSharedListenerWithDifferentArgsAfterListenerDisposed()
        {
            using Meter meter = new Meter("TestMeter7");
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, string.Empty, isShared: true, IntervalSecs, 10, 12, "TestMeter7"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                c.Add(19);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, 11, 13, "TestMeter7"))
            {
                listener2.WaitForMultipleSessionsConfiguredIncorrectlyError(s_waitForEventTimeout);
                events2 = listener2.Events.ToArray();
                AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(events2, "12", "13", "10", "11", IntervalSecs.ToString(), IntervalSecs.ToString());
            }

            AssertBeginInstrumentReportingEventsPresent(events, c);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"), ("19", "36"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_SharedSessionRejectsListenerWithDifferentInterval()
        {
            using Meter meter = new Meter("TestMeter7", null, new TagList() { { "Mk1", null }, { "Mk2", null } }, new object());
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe", new TagList() { { "Ck1", null }, { "Ck2", "" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" }, { "hk3", "hv3" } });
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter7"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs + 1, "TestMeter7"))
                {
                    listener2.WaitForMultipleSessionsConfiguredIncorrectlyError(s_waitForEventTimeout);
                    events2 = listener2.Events.ToArray();
                    AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(events2, MetricsEventListener.HistogramLimit.ToString(), MetricsEventListener.HistogramLimit.ToString(),
                        MetricsEventListener.TimeSeriesLimit.ToString(), MetricsEventListener.TimeSeriesLimit.ToString(), IntervalSecs.ToString(), (IntervalSecs + 1).ToString());
                }

                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_DisposeMeterBeforeSecondListener()
        {
            using Meter meterA = new Meter("TestMeter8", null, null, new object());
            using Meter meterB = new Meter("TestMeter9", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } });
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!");
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" } });
            UpDownCounter<int> udc = meterA.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meterA.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events, events2;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter8;TestMeter9"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                meterA.Dispose();
                listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 3);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter8"))
                {
                    events2 = listener2.Events.ToArray();
                }

                h.Record(21);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, h); // only h occurs twice because meterA is disposed before listener2 is created
            AssertBeginInstrumentReportingEventsPresent(events2, h);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events, meterA.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meterA.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meterA.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meterB.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("0.5=21;0.95=21;0.99=21", "1", "21"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
            AssertEndInstrumentReportingEventsPresent(events, c, oc, og, udc, oudc);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_DisposeMetersDuringAndAfterSecondListener()
        {
            using Meter meterA = new Meter("TestMeter8", null, new TagList() { { "1Mk1", "1Mv1" }, { "1Mk2", "Mv2" } });
            using Meter meterB = new Meter("TestMeter9", null, new TagList() { { "2Mk1", "2Mv1" } }, new object());
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" } });
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, isShared: true, IntervalSecs, "TestMeter8;TestMeter9"))
                {
                    meterA.Dispose();
                    listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 3);

                    events2 = listener2.Events.ToArray();
                }

                h.Record(21);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);

                meterB.Dispose();
                listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 5);

                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc, c, oc, og, h, udc, oudc);
            AssertBeginInstrumentReportingEventsPresent(events2, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertInitialEnumerationCompleteEventPresent(events2);
            AssertCounterEventsPresent(events, meterA.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meterA.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meterA.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meterB.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("0.5=21;0.95=21;0.99=21", "1", "21"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
            AssertEndInstrumentReportingEventsPresent(events, c, oc, og, udc, oudc, h);
            AssertEndInstrumentReportingEventsPresent(events2, c, oc, og, udc, oudc);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void MultipleListeners_PublishingInstruments()
        {
            using Meter meterA = new Meter("TestMeter10", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2"}, { "Mk3", null }});
            using Meter meterB = new Meter("TestMeter11", null, null, new object());
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" } });
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
                listener.WaitForEnumerationComplete(s_waitForEventTimeout);
                using (MetricsEventListener listener2 = new MetricsEventListener(_output, MetricsEventListener.InstrumentPublishing, isShared: true, null, ""))
                {
                    listener2.WaitForEnumerationComplete(s_waitForEventTimeout);
                    events = listener.Events.ToArray();
                    events2 = listener2.Events.ToArray();
                }
            }

            AssertInstrumentPublishingEventsPresent(events, c, oc, og, h, udc, oudc, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events, 2);
            AssertInstrumentPublishingEventsPresent(events2, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events2);
        }

        public static bool IsNotBrowserAndRemoteExecuteSupported => PlatformDetection.IsNotBrowser && RemoteExecutor.IsSupported;

        [ConditionalFact(typeof(MetricEventSourceTests), nameof(IsNotBrowserAndRemoteExecuteSupported))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesWithEmptyMetadata()
        {
            RemoteExecutor.Invoke(static () =>
            {
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("fi-FI");

                using Meter meter = new Meter("TestMeter1", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } }, new object());
                Counter<int> c = meter.CreateCounter<int>("counter1");
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
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                    c.Add(5);
                    h.Record(19);
                    udc.Add(-33);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(12);
                    h.Record(26);
                    udc.Add(-40);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events = listener.Events.ToArray();
                }

                AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
                AssertInitialEnumerationCompleteEventPresent(events);
                AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
                AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "10"), ("7", "17"));
                AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
                AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
                AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("-33", "-33"), ("-40", "-73"));
                AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "-11"), ("-11", "-22"));
                AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
            }).Dispose();
        }

        private sealed class NullTestOutputHelper : ITestOutputHelper
        {
            public static NullTestOutputHelper Instance { get; } = new();
            public void WriteLine(string message) { }
            public void WriteLine(string format, params object[] args) { }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesTimeSeriesWithMetadata()
        {
            using Meter meter = new Meter("TestMeter2");
            Counter<int> c = meter.CreateCounter<int>("counter1", "hat", "Fooz!!", new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
            int counterState = 3;
            ObservableCounter<int> oc = meter.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; } , "MB", "Size of universe", new TagList() { { "oCk1", "oCv1" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meter.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!", new TagList() { { "ogk1", null } });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", "a unit", "the description");
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description", new TagList() { { "udCk1", "udCv1" }, { "udCk2", "udCv2" } });
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter2"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
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
                UpDownCounter<int> udc;
                ObservableUpDownCounter<int> oudc;

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
                    udc = meter.CreateUpDownCounter<int>("upDownCounter1");
                    int upDownCounterState = 0;
                    oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState -= 11; return upDownCounterState; });

                    c.Add(5);
                    h.Record(19);
                    udc.Add(33);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                    c.Add(12);
                    h.Record(26);
                    udc.Add(40);
                    listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                    events = listener.Events.ToArray();
                }

                AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
                AssertInitialEnumerationCompleteEventPresent(events);
                AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
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
            UpDownCounter<int> udc;
            ObservableUpDownCounter<int> oudc;

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter4"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                // Instruments are created after the EventSource was already monitoring
                c = meter.CreateCounter<int>("counter1", null, null, new TagList() { { "Ck1", "Cv1" }, { "Ck2", "Cv2" } });
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(-40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "10"), ("7", "17"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "9", "18");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("-33", "-33"), ("-40", "-73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "11"), ("11", "22"));
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
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1");
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);

                c.Add(5, new KeyValuePair<string,object?>("Color", "red"));
                c.Add(6, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(19, new KeyValuePair<string, object?>("Size", 123));
                h.Record(20, new KeyValuePair<string, object?>("Size", 124));
                udc.Add(-33, new KeyValuePair<string, object?>("Color", "red"));
                udc.Add(-34, new KeyValuePair<string, object?>("Color", "blue"));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);

                c.Add(12, new KeyValuePair<string, object?>("Color", "red"));
                c.Add(13, new KeyValuePair<string, object?>("Color", "blue"));
                h.Record(26, new KeyValuePair<string, object?>("Size", 123));
                h.Record(27, new KeyValuePair<string, object?>("Size", 124));
                udc.Add(40, new KeyValuePair<string, object?>("Color", "red"));
                udc.Add(41, new KeyValuePair<string, object?>("Color", "blue"));
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=red", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=blue", "", ("6", "6"), ("13", "19"));
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


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/79749", TargetFrameworkMonikers.NetFramework)]
        public void EventSourceFiltersInstruments()
        {
            object scope = new object();
            using Meter meterA = new Meter("TestMeterA", null, new TagList() { { "1Mk1", null } }, scope);
            using Meter meterB = new Meter("TestMeterB", null, new TagList() { { "2Mk1", "" }}, scope);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesMissingDataPoints()
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                // no measurements in interval 3
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                // no measurements in interval 5
                listener.WaitForCollectionStop(s_waitForEventTimeout, 5);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("0", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("",  "17"), ("0", "17"), ("14", "31"), ("0", "31"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "18", "", "36", "");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("", "0", "0"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("", "0", "0"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("33", "33"), ("0", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "22"), ("0", "22"), ("22", "44"), ("0", "44"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 5);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesEndEventsOnMeterDispose()
        {
            object scope = new object();
            using Meter meterA = new Meter("TestMeter8", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", null } }, scope);
            using Meter meterB = new Meter("TestMeter9", null, new TagList() { { "Mk1", null }, { "Mk2", "Mv2" } }, scope);
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);

                meterA.Dispose();
                listener.WaitForEndInstrumentReporting(s_waitForEventTimeout, 3);

                h.Record(21);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 4);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meterA.Name, c.Name, "", c.Unit, ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meterA.Name, oc.Name, "", oc.Unit, ("", "10"), ("7", "17"), ("7", "24"));
            AssertGaugeEventsPresent(events, meterA.Name, og.Name, "", og.Unit, "9", "18", "27");
            AssertHistogramEventsPresent(events, meterB.Name, h.Name, "", h.Unit, ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"), ("0.5=21;0.95=21;0.99=21", "1", "21"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, udc.Name, "", udc.Unit, ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meterA.Name, oudc.Name, "", oudc.Unit, ("", "11"), ("11", "22"), ("11", "33"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 4);
            AssertEndInstrumentReportingEventsPresent(events, c, oc, og, udc, oudc);
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourcePublishesInstruments()
        {
            object scope = new object();

            using Meter meterA = new Meter("TestMeter10", null, null, scope);
            using Meter meterB = new Meter("TestMeter11", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", null } }, scope);
            Counter<int> c = meterA.CreateCounter<int>("counter1", "hat", "Fooz!!");
            int counterState = 3;
            ObservableCounter<int> oc = meterA.CreateObservableCounter<int>("observableCounter1", () => { counterState += 7; return counterState; }, "MB", "Size of universe",
                                            new TagList() { { "ock1", "ocv1" }, { "ock2", "ocv2" }, { "ock3", "ocv3" } });
            int gaugeState = 0;
            ObservableGauge<int> og = meterA.CreateObservableGauge<int>("observableGauge1", () => { gaugeState += 9; return gaugeState; }, "12394923 asd [],;/", "junk!",
                                            new TagList() { { "ogk1", "ogv1" } });
            Histogram<int> h = meterB.CreateHistogram<int>("histogram1", "a unit", "the description", new TagList() { { "hk1", "hv1" }, { "hk2", "" }, {"hk3", null } });
            UpDownCounter<int> udc = meterA.CreateUpDownCounter<int>("upDownCounter1", "udc unit", "udc description", new TagList() { { "udk1", "udv1" } });
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meterA.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; }, "oudc unit", "oudc description");

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.InstrumentPublishing, null, ""))
            {
                listener.WaitForEnumerationComplete(s_waitForEventTimeout);
                events = listener.Events.ToArray();
            }

            AssertInstrumentPublishingEventsPresent(events, c, oc, og, h, udc, oudc);
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
                d.Add(5.25);

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
                d.Add(5.25);

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
            AssertCounterEventsPresent(events, meter.Name, i.Name, "", "", ("1234568", "1234568"), ("1234568", "2469136"));
            AssertCounterEventsPresent(events, meter.Name, s.Name, "", "", ("21433", "21433"), ("21433", "42866"));
            AssertCounterEventsPresent(events, meter.Name, b.Name, "", "", ("2", "2"), ("2", "4"));
            AssertCounterEventsPresent(events, meter.Name, l.Name, "", "", ("123456789013", "123456789013"), ("123456789013", "246913578026"));
            AssertCounterEventsPresent(events, meter.Name, dec.Name, "", "", ("123456789012346", "123456789012346"), ("123456789012346", "246913578024692"));
            AssertCounterEventsPresent(events, meter.Name, f.Name, "", "", ("123457.7890625", "123457.7890625"), ("123457.7890625", "246915.578125"));
            AssertCounterEventsPresent(events, meter.Name, d.Name, "", "", ("6.25", "6.25"), ("6.25", "12.5"));
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
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=red", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, c.Name, "Color=blue", "", ("6", "6"), ("13", "19"));
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
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=red", "", ("0.5=5;0.95=5;0.99=5", "1", "5"), ("0.5=12;0.95=12;0.99=12", "1", "12"));
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=blue", "", ("0.5=6;0.95=6;0.99=6", "1", "6"), ("0.5=13;0.95=13;0.99=13", "1", "13"));
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
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
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
            UpDownCounter<int> udc = meter.CreateUpDownCounter<int>("upDownCounter1");
            int upDownCounterState = 0;
            ObservableUpDownCounter<int> oudc = meter.CreateObservableUpDownCounter<int>("observableUpDownCounter1", () => { upDownCounterState += 11; return upDownCounterState; });

            EventWrittenEventArgs[] events;
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, "TestMeter16"))
            {
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
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
                listener.WaitForCollectionStop(s_waitForEventTimeout, 1);
                c.Add(5);
                h.Record(19);
                udc.Add(33);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 2);
                c.Add(12);
                h.Record(26);
                udc.Add(40);
                listener.WaitForCollectionStop(s_waitForEventTimeout, 3);
                events = listener.Events.ToArray();
            }

            AssertBeginInstrumentReportingEventsPresent(events, c, oc, og, h, udc, oudc);
            AssertInitialEnumerationCompleteEventPresent(events);
            AssertCounterEventsPresent(events, meter.Name, c.Name, "", "", ("5", "5"), ("12", "17"));
            AssertCounterEventsPresent(events, meter.Name, oc.Name, "", "", ("", "31"), ("7", "38"));
            AssertGaugeEventsPresent(events, meter.Name, og.Name, "", "", "36", "45");
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "", "", ("0.5=19;0.95=19;0.99=19", "1", "19"), ("0.5=26;0.95=26;0.99=26", "1", "26"));
            AssertUpDownCounterEventsPresent(events, meter.Name, udc.Name, "", "", ("33", "33"), ("40", "73"));
            AssertUpDownCounterEventsPresent(events, meter.Name, oudc.Name, "", "", ("", "44"), ("11", "55"));
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [OuterLoop("Slow and has lots of console spew")]
        public void EventSourceEnforcesHistogramLimitAndNotMaxTimeSeries()
        {
            using Meter meter = new Meter("TestMeter17", null, new TagList() { { "Mk1", "Mv1" }, { "Mk2", "Mv2" } });
            Histogram<int> h = meter.CreateHistogram<int>("histogram1", null, null, new TagList() { { "hk1", "hv1" }, { "hk2", "hv2" } });

            EventWrittenEventArgs[] events;
            // MaxTimeSeries = 3, MaxHistograms = 2
            // HistogramLimitReached should be raised when Record(tags: "Color=green"), but TimeSeriesLimitReached should not be raised
            using (MetricsEventListener listener = new MetricsEventListener(_output, MetricsEventListener.TimeSeriesValues, IntervalSecs, 3, 2, "TestMeter17"))
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
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=red", "", ("0.5=5;0.95=5;0.99=5", "1", "5"), ("0.5=12;0.95=12;0.99=12", "1", "12"));
            AssertHistogramEventsPresent(events, meter.Name, h.Name, "Color=blue", "", ("0.5=6;0.95=6;0.99=6", "1", "6"), ("0.5=13;0.95=13;0.99=13", "1", "13"));
            AssertHistogramLimitPresent(events);
            AssertTimeSeriesLimitNotPresent(events);
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=green");
            AssertHistogramEventsNotPresent(events, meter.Name, h.Name, "Color=yellow");
            AssertCollectStartStopEventsPresent(events, IntervalSecs, 3);
        }

        private static string FormatScopeHash(object? scope) =>
            scope is null ? string.Empty : RuntimeHelpers.GetHashCode(scope).ToString(CultureInfo.InvariantCulture);

        private static string FormatTags(IEnumerable<KeyValuePair<string, object?>>? tags)
        {
            if (tags is null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }

                sb.Append(tag.Key).Append('=');

                if (tag.Value is not null)
                {
                    sb.Append(tag.Value.ToString());
                }
            }
            return sb.ToString();
        }

        private static void AssertBeginInstrumentReportingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var beginReportEvents = events.Where(e => e.EventName == "BeginInstrumentReporting").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString(),
                    InstrumentTags = e.Payload[7].ToString(),
                    MeterTags = e.Payload[8].ToString(),
                    ScopeHash = e.Payload[9].ToString()
                }).ToArray();

            foreach(Instrument i in expectedInstruments)
            {
                var e = beginReportEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a BeginInstrumentReporting event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
                Assert.Equal(FormatTags(i.Tags), e.InstrumentTags);
                Assert.Equal(FormatTags(i.Meter.Tags), e.MeterTags);
                Assert.Equal(FormatScopeHash(i.Meter.Scope), e.ScopeHash);
            }

            Assert.Equal(expectedInstruments.Length, beginReportEvents.Length);
        }

        private static void AssertEndInstrumentReportingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var beginReportEvents = events.Where(e => e.EventName == "EndInstrumentReporting").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString(),
                    InstrumentTags = e.Payload[7].ToString(),
                    MeterTags = e.Payload[8].ToString(),
                    ScopeHash = e.Payload[9].ToString()
                }).ToArray();

            foreach (Instrument i in expectedInstruments)
            {
                var e = beginReportEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a EndInstrumentReporting event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
                Assert.Equal(FormatTags(i.Tags), e.InstrumentTags);
                Assert.Equal(FormatTags(i.Meter.Tags), e.MeterTags);
                Assert.Equal(FormatScopeHash(i.Meter.Scope), e.ScopeHash);
            }

            Assert.Equal(expectedInstruments.Length, beginReportEvents.Length);
        }

        private static void AssertInitialEnumerationCompleteEventPresent(EventWrittenEventArgs[] events, int eventsCount = 1)
        {
            Assert.Equal(eventsCount, events.Where(e => e.EventName == "InitialInstrumentEnumerationComplete").Count());
        }

        private static void AssertTimeSeriesLimitPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(1, events.Where(e => e.EventName == "TimeSeriesLimitReached").Count());
        }

        private static void AssertTimeSeriesLimitNotPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(0, events.Where(e => e.EventName == "TimeSeriesLimitReached").Count());
        }

        private static void AssertHistogramLimitPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(1, events.Where(e => e.EventName == "HistogramLimitReached").Count());
        }

        private static void AssertInstrumentPublishingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var publishEvents = events.Where(e => e.EventName == "InstrumentPublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString(),
                    InstrumentTags = e.Payload[7].ToString(),
                    MeterTags = e.Payload[8].ToString(),
                    ScopeHash = e.Payload[9].ToString()
                }).ToArray();

            foreach (Instrument i in expectedInstruments)
            {
                var e = publishEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a InstrumentPublished event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
                Assert.Equal(FormatTags(i.Tags), e.InstrumentTags);
                Assert.Equal(FormatTags(i.Meter.Tags), e.MeterTags);
                Assert.Equal(FormatScopeHash(i.Meter.Scope), e.ScopeHash);
            }

            Assert.Equal(expectedInstruments.Length, publishEvents.Length);
        }

        private static void AssertCounterEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string)[] expected)
        {
            AssertGenericCounterEventsPresent("CounterRateValuePublished", events, meterName, instrumentName, tags, expectedUnit, expected);
        }

        private static void AssertUpDownCounterEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string)[] expected)
        {
            AssertGenericCounterEventsPresent("UpDownCounterRateValuePublished", events, meterName, instrumentName, tags, expectedUnit, expected);
        }

        private static void AssertGenericCounterEventsPresent(string eventName, EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string)[] expected)
        {
            var counterEvents = events.Where(e => e.EventName == eventName).Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Rate = e.Payload[6].ToString(),
                    Value = e.Payload[7].ToString()
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expectedUnit, filteredEvents[i].Unit);
                Assert.Equal(expected[i].Item1, filteredEvents[i].Rate);
                Assert.Equal(expected[i].Item2, filteredEvents[i].Value);
            }
        }

        private static void AssertCounterEventsNotPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags)
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

        private static void AssertGaugeEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
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

        private static void AssertHistogramEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string, string)[] expected)
        {
            var counterEvents = events.Where(e => e.EventName == "HistogramValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Quantiles = (string)e.Payload[6],
                    Count = e.Payload[7].ToString(),
                    Sum = e.Payload[8].ToString()
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expected.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(filteredEvents[i].Unit, expectedUnit);
                Assert.Equal(expected[i].Item1, filteredEvents[i].Quantiles);
                Assert.Equal(expected[i].Item2, filteredEvents[i].Count);
                Assert.Equal(expected[i].Item3, filteredEvents[i].Sum);
            }
        }

        private static void AssertHistogramEventsNotPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags)
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
        private static void AssertCollectStartStopEventsPresent(EventWrittenEventArgs[] events, double expectedIntervalSecs, int expectedPairs)
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

        private static void AssertObservableCallbackErrorPresent(EventWrittenEventArgs[] events)
        {
            var errorEvents = events.Where(e => e.EventName == "ObservableInstrumentCallbackError").Select(e =>
                new
                {
                    ErrorText = e.Payload[1].ToString(),
                }).ToArray();
            Assert.NotEmpty(errorEvents);
            Assert.Contains("Example user exception", errorEvents[0].ErrorText);
        }

        private static void AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(EventWrittenEventArgs[] events,
            string expectedMaxHistograms, string actualMaxHistograms, string expectedMaxTimeSeries, string actualMaxTimeSeries,
            string expectedRefreshInterval, string actualRefreshInterval)
        {
            var counterEvents = events.Where(e => e.EventName == "MultipleSessionsConfiguredIncorrectlyError").Select(e =>
                new
                {
                    ExpectedMaxHistograms = e.Payload[1].ToString(),
                    ActualMaxHistograms = e.Payload[2].ToString(),
                    ExpectedMaxTimeSeries = e.Payload[3].ToString(),
                    ActualMaxTimeSeries = e.Payload[4].ToString(),
                    ExpectedRefreshInterval = e.Payload[5].ToString(),
                    ActualRefreshInterval = e.Payload[6].ToString(),
                }).ToArray();
            var filteredEvents = counterEvents;
            Assert.Single(filteredEvents);

            Assert.Equal(expectedMaxHistograms, filteredEvents[0].ExpectedMaxHistograms);
            Assert.Equal(expectedMaxTimeSeries, filteredEvents[0].ExpectedMaxTimeSeries);
            Assert.Equal(expectedRefreshInterval, filteredEvents[0].ExpectedRefreshInterval);
            Assert.Equal(actualMaxHistograms, filteredEvents[0].ActualMaxHistograms);
            Assert.Equal(actualMaxTimeSeries, filteredEvents[0].ActualMaxTimeSeries);
            Assert.Equal(actualRefreshInterval, filteredEvents[0].ActualRefreshInterval);
        }
    }

    class MetricsEventListener : EventListener
    {
        public const EventKeywords MessagesKeyword = (EventKeywords)0x1;
        public const EventKeywords TimeSeriesValues = (EventKeywords)0x2;
        public const EventKeywords InstrumentPublishing = (EventKeywords)0x4;
        public const int TimeSeriesLimit = 50;
        public const int HistogramLimit = 50;
        public const string SharedSessionId = "SHARED";

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval, params string[]? instruments) :
            this(output, keywords, refreshInterval, TimeSeriesLimit, HistogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, bool isShared, double? refreshInterval, params string[]? instruments) :
            this(output, keywords, isShared, refreshInterval, TimeSeriesLimit, HistogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, false, refreshInterval, timeSeriesLimit, histogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, bool isShared, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, Guid.NewGuid().ToString(), isShared, refreshInterval, timeSeriesLimit, histogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, string sessionId, bool isShared, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, sessionId, isShared,
            FormatArgDictionary(refreshInterval,timeSeriesLimit, histogramLimit, instruments, sessionId, isShared))
        {
        }

        private static Dictionary<string,string> FormatArgDictionary(double? refreshInterval, int? timeSeriesLimit, int? histogramLimit, string?[]? instruments, string? sessionId, bool shared)
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
                if (shared)
                {
                    d.Add("SessionId", SharedSessionId);
                    d.Add("ClientId", sessionId);
                }
                else
                {
                    d.Add("SessionId", sessionId);
                }
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

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, string sessionId, bool shared, Dictionary<string,string> arguments)
        {
            _output = output;
            _keywords = keywords;
            _sessionId = shared ? SharedSessionId : sessionId;
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
            if (eventData.EventName != "MultipleSessionsNotSupportedError" && eventData.EventName != "MultipleSessionsConfiguredIncorrectlyError" && sessionId != "" && sessionId != _sessionId)
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

        public void WaitForMultipleSessionsConfiguredIncorrectlyError(TimeSpan timeout) => WaitForEvent(timeout, 1, "MultipleSessionsConfiguredIncorrectlyError");

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
