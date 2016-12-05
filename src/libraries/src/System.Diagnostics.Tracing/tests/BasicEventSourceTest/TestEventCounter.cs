// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
#if USE_ETW // TODO: Enable when TraceEvent is available on CoreCLR. GitHub issue #4864.
using Microsoft.Diagnostics.Tracing.Session;
#endif
using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BasicEventSourceTests
{
#if USE_ETW // TODO: Enable when TraceEvent is available on CoreCLR. GitHub issue #4864.
    public class TestEventCounter
    {
        private sealed class MyEventSource : EventSource
        {
            private EventCounter _requestCounter;
            private EventCounter _errorCounter;

            public MyEventSource()
            {
                _requestCounter = new EventCounter("Request", this);
                _errorCounter = new EventCounter("Error", this);
            }

            public void Request(float elapsed)
            {
                _requestCounter.WriteMetric(elapsed);
            }

            public void Error()
            {
                _errorCounter.WriteMetric(1);
            }
        }

        [Fact]
        public void Test_Write_Metric_EventListener()
        {
            using (var listener = new EventListenerListener())
            {
                Test_Write_Metric(listener);
            }
        }

        [Fact]
        public void Test_Write_Metric_ETW()
        {
            using (var listener = new EtwListener())
            {
                Test_Write_Metric(listener);
            }
        }

        private void Test_Write_Metric(Listener listener)
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");

            using (var logger = new MyEventSource())
            {
                var tests = new List<SubTest>();
                /*************************************************************************/
                tests.Add(new SubTest("Log 1 event",
                    delegate ()
                    {
                        listener.EnableTimer(logger, 1); /* Poll every 1 s */
                        logger.Request(37);
                        Thread.Sleep(1500); // Sleep for 1.5 seconds
                        listener.EnableTimer(logger, 0);
                    },
                    delegate (List<Event> evts)
                    {
                        Assert.Equal(2, evts.Count);
                        ValidateSingleEventCounter(evts[0], "Request", 1, 37, 0, 37, 37);
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Log 2 event in single period",
                    delegate ()
                    {
                        listener.EnableTimer(logger, 1); /* Poll every 1 s */
                        logger.Request(37);
                        logger.Request(25);
                        Thread.Sleep(1500); // Sleep for 1.5 seconds
                        listener.EnableTimer(logger, 0);
                    },
                    delegate (List<Event> evts)
                    {
                        Assert.Equal(2, evts.Count);
                        ValidateSingleEventCounter(evts[0], "Request", 2, 31, 6, 25, 37);
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Log 2 event in two periods",
                    delegate ()
                    {
                        listener.EnableTimer(logger, 1); /* Poll every 1 s */
                        logger.Request(37);
                        Thread.Sleep(1500); // Sleep for 1.5 seconds
                        logger.Request(25);
                        Thread.Sleep(1000); // Sleep for 1 seconds (at time = 2.5 second exactly two messages should be received)
                        listener.EnableTimer(logger, 0);
                    },
                    delegate (List<Event> evts)
                    {
                        Assert.Equal(4, evts.Count);
                        ValidateSingleEventCounter(evts[0], "Request", 1, 37, 0, 37, 37);
                        ValidateSingleEventCounter(evts[2], "Request", 1, 25, 0, 25, 25);
                    }));
                /*************************************************************************/
                tests.Add(new SubTest("Log 2 different events in a period",
                    delegate ()
                    {
                        listener.EnableTimer(logger, 1); /* Poll every 1 s */
                        logger.Request(25);
                        logger.Error();
                        Thread.Sleep(1500); // Sleep for 1.5 seconds
                        listener.EnableTimer(logger, 0);
                    },
                    delegate (List<Event> evts)
                    {
                        Assert.Equal(2, evts.Count);
                        ValidateSingleEventCounter(evts[0], "Request", 1, 25, 0, 25, 25);
                        ValidateSingleEventCounter(evts[1], "Error", 1, 1, 0, 1, 1);
                    }));
                /*************************************************************************/
                EventTestHarness.RunTests(tests, listener, logger);
            }
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        private static void ValidateSingleEventCounter(Event evt, string counterName, int count, float sum, float sumSquared, float min, float max)
        {
            object payload = ValidateEventHeaderAndGetPayload(evt);
            var payloadContent = payload as IDictionary<string, object>;
            Assert.NotNull(payloadContent);
            ValidateEventCounter(counterName, count, sum, sumSquared, min, max, payloadContent);
        }

        private static object ValidateEventHeaderAndGetPayload(Event evt)
        {
            Assert.Equal("EventCounters", evt.EventName);
            List<string> payloadNames = evt.PayloadNames.ToList();
            Assert.Equal(1, payloadNames.Count);
            Assert.Equal("Payload", payloadNames[0]);
            object rawPayload = evt.PayloadValue(0, "Payload");
            return rawPayload;
        }

        private static void ValidateEventCounter(string counterName, int count, float mean, float standardDeviation, float min, float max, IDictionary<string, object> payloadContent)
        {
            var payloadContentValue = new List<KeyValuePair<string, object>>();
            foreach (var payloadContentEntry in payloadContent)
            {
                payloadContentValue.Add(payloadContentEntry);
            }

            Assert.Equal(7, payloadContentValue.Count);
            ValidatePayloadEntry("Name", counterName, payloadContentValue[0]);
            ValidatePayloadEntry("Mean", mean, payloadContentValue[1]);
            ValidatePayloadEntry("StandardDeviation", standardDeviation, payloadContentValue[2]);
            ValidatePayloadEntry("Count", count, payloadContentValue[3]);
            ValidatePayloadEntry("Min", min, payloadContentValue[4]);
            ValidatePayloadEntry("Max", max, payloadContentValue[5]);
        }

        private static void ValidatePayloadEntry(string name, object value, KeyValuePair<string, object> payloadEntry)
        {
            Assert.Equal(name, payloadEntry.Key);
            Assert.Equal(value, payloadEntry.Value);
        }
    }
#endif //USE_ETW
}
