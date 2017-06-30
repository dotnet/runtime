// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;
using System;
using System.Collections.Generic;

namespace BasicEventSourceTests
{
    /// <summary>
    /// The EventTestHarness class knows how to run a set of single-event tests on either
    /// the ETW pipeline or the EventListener pipeline.  
    /// 
    /// Basically you make up a bunch of SubTests and then hand it to this harness
    /// to run them in bulk.   
    /// </summary>
    public static class EventTestHarness
    {
        /// <summary>
        /// Runs a series of tests 'tests' using the listener (either an ETWListener or an EventListenerListener) on 
        /// an EventSource 'source' passing it the filter parameters=options (by default source turn on completely
        /// 
        /// Note that this routine calls Dispose on the listener, so it can't be used after that.  
        /// </summary>
        public static void RunTests(List<SubTest> tests, Listener listener, EventSource source, FilteringOptions options = null)
        {
            int expectedTestNumber = 0;
            SubTest currentTest = null;
            List<Event> replies = new List<Event>(2);

            // Wire up the callback to handle the validation when the listener receives events. 
            listener.OnEvent = delegate (Event data)
            {
                if (data.ProviderName == "TestHarnessEventSource")
                {
                    Assert.Equal(data.EventName, "StartTest");

                    int testNumber = (int)data.PayloadValue(1, "testNumber");
                    Assert.Equal(expectedTestNumber, testNumber);

                    // Validate that the events that came in during the test are correct. 
                    if (currentTest != null)
                    {
                        // You can use the currentTest.Name to set a filter in the harness 
                        // tests = tests.FindAll(test => Regex.IsMatch(test.Name, "Write/Basic/EventII")
                        // so the test only runs this one sub-test.   Then you can set 
                        // breakpoints to watch the events be generated.  
                        //
                        // All events from EventSource infrastructure should be coming from
                        // the ReportOutOfBand, so placing a breakpoint there is typically useful. 
                        if (currentTest.EventListValidator != null)
                            currentTest.EventListValidator(replies);
                        else
                        {
                            // we only expect exactly one reply
                            Assert.Equal(replies.Count, 1);
                            currentTest.EventValidator(replies[0]);
                        }
                    }
                    replies.Clear();

                    if (testNumber < tests.Count)
                    {
                        currentTest = tests[testNumber];
                        Assert.Equal(currentTest.Name, data.PayloadValue(0, "name"));
                        expectedTestNumber++;
                    }
                    else
                    {
                        Assert.NotNull(currentTest);
                        Assert.Equal("", data.PayloadValue(0, "name"));
                        Assert.Equal(tests.Count, testNumber);
                        currentTest = null;
                    }
                }
                else
                {
                    // If expectedTestNumber is 0 then this is before the first test
                    // If expectedTestNumber is count then it is after the last test
                    Assert.NotNull(currentTest);
                    replies.Add(data);
                }
            };

            // Run the tests. collecting and validating the results. 
            using (TestHarnessEventSource testHarnessEventSource = new TestHarnessEventSource())
            {
                // Turn on the test EventSource.  
                listener.EventSourceSynchronousEnable(source, options);
                // And the harnesses's EventSource. 
                listener.EventSourceSynchronousEnable(testHarnessEventSource);

                // Generate events for all the tests, surrounded by events that tell us we are starting a test.  
                int testNumber = 0;
                foreach (var test in tests)
                {
                    testHarnessEventSource.StartTest(test.Name, testNumber);
                    test.EventGenerator();
                    testNumber++;
                }
                testHarnessEventSource.StartTest("", testNumber);        // Empty test marks the end of testing. 

                // Disable the listeners.  
                listener.EventSourceCommand(source.Name, EventCommand.Disable);
                listener.EventSourceCommand(testHarnessEventSource.Name, EventCommand.Disable);

                // Send something that should be ignored.  
                testHarnessEventSource.IgnoreEvent();
            }

            listener.Dispose();         // Indicate we are done listening.  For the ETW file based cases, we do all the processing here

            // expectedTetst number are the number of tests we successfully ran.  
            Assert.Equal(expectedTestNumber, tests.Count);
        }

        /// <summary>
        /// This eventSource I use to emit events to separate tests from each other.  
        /// </summary>
        private class TestHarnessEventSource : EventSource
        {
            public void StartTest(string name, int testNumber) { WriteEvent(1, name, testNumber); }
            /// <summary>
            /// Sent to make sure the listener is ignoring when it should be.  
            /// </summary>
            public void IgnoreEvent() { WriteEvent(2); }
        }
    }

    /// <summary>
    /// A boring typed container that holds information about a test of single EventSource event
    /// It holds the
    ///     name, 
    ///     the code to generate an event to test (EventGenerator)
    ///     the code to validate that the event is correct (EventValidator)
    ///     OR the code to validate that the List of events is (EventListValidator) (when the output is not a single event)
    /// </summary>
    public class SubTest : IEquatable<SubTest>
    {
        public SubTest(string name, Action eventGenerator, Action<Event> eventValidator)
        {
            Name = name;
            EventGenerator = eventGenerator;
            EventValidator = eventValidator;
        }
        /// <summary>
        /// If a single event does not produce a single response (if you expect additional error messages)
        /// use this constructor to validate the response.  
        /// </summary>
        public SubTest(string name, Action eventGenerator, Action<List<Event>> eventListValidator)
        {
            Name = name;
            EventGenerator = eventGenerator;
            EventListValidator = eventListValidator;
        }

        // This action cause the eventSource to emit an event (it is the test)
        public Action EventGenerator { get; private set; }
        // This action is given the resulting event and should Assert that it is correct
        public Action<Event> EventValidator { get; private set; }
        public Action<List<Event>> EventListValidator { get; private set; }
        public string Name { get; private set; }
        public bool Equals(SubTest other)
        {
            return Name == other.Name;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
