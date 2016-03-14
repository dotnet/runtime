using System;
using System.Collections.Generic;
using Xunit;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.Text;

// Turn off parallel execution of tests.   
// TODO in theory can run the tests in parallel, however there has been some failures when we do this.   
// Needs investigation.  traced by https://github.com/dotnet/corefx/issues/6872
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]

namespace System.Diagnostics.Tests
{
    public class DiagnosticSourceEventSourceBridgeTests
    {
        /// <summary>
        /// Tests the basic functionality of turning on specific EventSources and specifying 
        /// the events you want.
        /// </summary>
        [Fact]
        public void TestSpecificEvents()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener = new DiagnosticListener("TestSpecificEventsSource"))
            {
                Assert.Equal(0, eventSourceListener.EventCount);

                // Turn on events with both implicit and explicit types You can have whitespace 
                // before and after each spec.  
                eventSourceListener.Enable(
                    "  TestSpecificEventsSource/TestEvent1:cls_Point_X=cls.Point.X;cls_Point_Y=cls.Point.Y\r\n" +
                    "  TestSpecificEventsSource/TestEvent2:cls_Url=cls.Url\r\n"
                    );

                /***************************************************************************************/
                // Emit an event that matches the first pattern. 
                MyClass val = new MyClass() { Url = "MyUrl", Point = new MyPoint() { X = 3, Y = 5 } };
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestSpecificEventsSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(4, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("hi", eventSourceListener.LastEvent.Arguments["propStr"]);
                Assert.Equal("4", eventSourceListener.LastEvent.Arguments["propInt"]);
                Assert.Equal("3", eventSourceListener.LastEvent.Arguments["cls_Point_X"]);
                Assert.Equal("5", eventSourceListener.LastEvent.Arguments["cls_Point_Y"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                /***************************************************************************************/
                // Emit an event that matches the second pattern. 
                if (diagnosticSourceListener.IsEnabled("TestEvent2"))
                    diagnosticSourceListener.Write("TestEvent2", new { prop2Str = "hello", prop2Int = 8, cls = val });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.  
                Assert.Equal("TestSpecificEventsSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent2", eventSourceListener.LastEvent.EventName);
                Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("hello", eventSourceListener.LastEvent.Arguments["prop2Str"]);
                Assert.Equal("8", eventSourceListener.LastEvent.Arguments["prop2Int"]);
                Assert.Equal("MyUrl", eventSourceListener.LastEvent.Arguments["cls_Url"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                // Emit an event that does not match either pattern.  (thus will be filtered out)
                if (diagnosticSourceListener.IsEnabled("TestEvent3"))
                    diagnosticSourceListener.Write("TestEvent3", new { propStr = "prop3", });
                Assert.Equal(0, eventSourceListener.EventCount);        // No Event should be fired.  

                /***************************************************************************************/
                // Emit an event from another diagnostic source with the same event name.  
                // It will be filtered out.  
                using (var diagnosticSourceListener2 = new DiagnosticListener("TestSpecificEventsSource2"))
                {
                    if (diagnosticSourceListener2.IsEnabled("TestEvent1"))
                        diagnosticSourceListener2.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val });
                }
                Assert.Equal(0, eventSourceListener.EventCount);        // No Event should be fired.  

                // Disable all the listener and insure that no more events come through.  
                eventSourceListener.Disable();

                diagnosticSourceListener.Write("TestEvent1", null);
                diagnosticSourceListener.Write("TestEvent2", null);

                Assert.Equal(0, eventSourceListener.EventCount);        // No Event should be received.  
            }

            // Make sure that there are no Diagnostic Listeners left over.  
            DiagnosticListener.AllListeners.Subscribe(DiagnosticSourceTest.MakeObserver(delegate (DiagnosticListener listen)
            {
                Assert.True(!listen.Name.StartsWith("BuildTestSource"));
            }));
        }

        /// <summary>
        /// Test that things work properly for Linux newline conventions. 
        /// </summary>
        [Fact]
        public void LinuxNewLineConventions()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener = new DiagnosticListener("LinuxNewLineConventionsSource"))
            {
                Assert.Equal(0, eventSourceListener.EventCount);

                // Turn on events with both implicit and explicit types You can have whitespace 
                // before and after each spec.   Use \n rather than \r\n 
                eventSourceListener.Enable(
                    "  LinuxNewLineConventionsSource/TestEvent1:-cls_Point_X=cls.Point.X\n" + 
                    "  LinuxNewLineConventionsSource/TestEvent2:-cls_Url=cls.Url\n"
                    );

                /***************************************************************************************/
                // Emit an event that matches the first pattern. 
                MyClass val = new MyClass() { Url = "MyUrl", Point = new MyPoint() { X = 3, Y = 5 } };
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("LinuxNewLineConventionsSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("3", eventSourceListener.LastEvent.Arguments["cls_Point_X"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                /***************************************************************************************/
                // Emit an event that matches the second pattern. 
                if (diagnosticSourceListener.IsEnabled("TestEvent2"))
                    diagnosticSourceListener.Write("TestEvent2", new { prop2Str = "hello", prop2Int = 8, cls = val });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.  
                Assert.Equal("LinuxNewLineConventionsSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent2", eventSourceListener.LastEvent.EventName);
                Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("MyUrl", eventSourceListener.LastEvent.Arguments["cls_Url"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                // Emit an event that does not match either pattern.  (thus will be filtered out)
                if (diagnosticSourceListener.IsEnabled("TestEvent3"))
                    diagnosticSourceListener.Write("TestEvent3", new { propStr = "prop3", });
                Assert.Equal(0, eventSourceListener.EventCount);        // No Event should be fired.  
            }

            // Make sure that there are no Diagnostic Listeners left over.  
            DiagnosticListener.AllListeners.Subscribe(DiagnosticSourceTest.MakeObserver(delegate (DiagnosticListener listen)
            {
                Assert.True(!listen.Name.StartsWith("BuildTestSource"));
            }));
        }

        /// <summary>
        /// Tests what happens when you wildcard the source name (empy string)
        /// </summary>
        [Fact]
        public void TestWildCardSourceName()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener1 = new DiagnosticListener("TestWildCardSourceName1"))
            using (var diagnosticSourceListener2 = new DiagnosticListener("TestWildCardSourceName2"))
            {
                eventSourceListener.Filter = (DiagnosticSourceEvent evnt) => evnt.SourceName.StartsWith("TestWildCardSourceName");

                // Turn On Everything.  Note that because of concurent testing, we may get other sources as well.
                // but we filter them out because we set eventSourceListener.Filter.   
                eventSourceListener.Enable("");

                Assert.True(diagnosticSourceListener1.IsEnabled("TestEvent1"));
                Assert.True(diagnosticSourceListener1.IsEnabled("TestEvent2"));
                Assert.True(diagnosticSourceListener2.IsEnabled("TestEvent1"));
                Assert.True(diagnosticSourceListener2.IsEnabled("TestEvent2"));

                Assert.Equal(0, eventSourceListener.EventCount);

                diagnosticSourceListener1.Write("TestEvent1", new { prop111 = "prop111Val", prop112 = 112 });
                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestWildCardSourceName1", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("prop111Val", eventSourceListener.LastEvent.Arguments["prop111"]);
                Assert.Equal("112", eventSourceListener.LastEvent.Arguments["prop112"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                diagnosticSourceListener1.Write("TestEvent2", new { prop121 = "prop121Val", prop122 = 122 });
                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestWildCardSourceName1", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent2", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("prop121Val", eventSourceListener.LastEvent.Arguments["prop121"]);
                Assert.Equal("122", eventSourceListener.LastEvent.Arguments["prop122"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                diagnosticSourceListener2.Write("TestEvent1", new { prop211 = "prop211Val", prop212 = 212 });
                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestWildCardSourceName2", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("prop211Val", eventSourceListener.LastEvent.Arguments["prop211"]);
                Assert.Equal("212", eventSourceListener.LastEvent.Arguments["prop212"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                diagnosticSourceListener2.Write("TestEvent2", new { prop221 = "prop221Val", prop222 = 122 });
                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestWildCardSourceName2", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent2", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("prop221Val", eventSourceListener.LastEvent.Arguments["prop221"]);
                Assert.Equal("122", eventSourceListener.LastEvent.Arguments["prop222"]);
                eventSourceListener.ResetEventCountAndLastEvent();
            }
        }

        /// <summary>
        /// Tests what happens when you wildcard event name (but not the source name) 
        /// </summary>
        [Fact]
        public void TestWildCardEventName()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener = new DiagnosticListener("TestWildCardEventNameSource"))
            {
                Assert.Equal(0, eventSourceListener.EventCount);

                // Turn on events with both implicit and explicit types 
                eventSourceListener.Enable("TestWildCardEventNameSource");

                /***************************************************************************************/
                // Emit an event, check that all implicit properties are generated
                MyClass val = new MyClass() { Url = "MyUrl", Point = new MyPoint() { X = 3, Y = 5 } };
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestWildCardEventNameSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("hi", eventSourceListener.LastEvent.Arguments["propStr"]);
                Assert.Equal("4", eventSourceListener.LastEvent.Arguments["propInt"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                /***************************************************************************************/
                // Emit the same event, with a different set of implicit properties 
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { propStr2 = "hi2", cls = val });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestWildCardEventNameSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("hi2", eventSourceListener.LastEvent.Arguments["propStr2"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                /***************************************************************************************/
                // Emit an event from another diagnostic source with the same event name.  
                // It will be filtered out.  
                using (var diagnosticSourceListener2 = new DiagnosticListener("TestWildCardEventNameSource2"))
                {
                    if (diagnosticSourceListener2.IsEnabled("TestEvent1"))
                        diagnosticSourceListener2.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val });
                }
                Assert.Equal(0, eventSourceListener.EventCount);        // No Event should be fired.  
            }
        }

        /// <summary>
        /// Test what happens when there are nulls passed in the event payloads
        /// Bascially strings get turned into empty strings and other nulls are typically
        /// ignored.  
        /// </summary>
        [Fact]
        public void TestNulls()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener = new DiagnosticListener("TestNullsTestSource"))
            {
                Assert.Equal(0, eventSourceListener.EventCount);

                // Turn on events with both implicit and explicit types 
                eventSourceListener.Enable("TestNullsTestSource/TestEvent1:cls.Url;cls_Point_X=cls.Point.X");

                /***************************************************************************************/
                // Emit a null arguments object. 

                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", null);

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestNullsTestSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(0, eventSourceListener.LastEvent.Arguments.Count);
                eventSourceListener.ResetEventCountAndLastEvent();

                /***************************************************************************************/
                // Emit an arguments object with nulls in it.   

                MyClass val = null;
                string strVal = null;
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { cls = val, propStr = "propVal1", propStrNull = strVal });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestNullsTestSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("propVal1", eventSourceListener.LastEvent.Arguments["propStr"]);
                Assert.Equal("", eventSourceListener.LastEvent.Arguments["propStrNull"]);   // null strings get turned into empty strings
                eventSourceListener.ResetEventCountAndLastEvent();

                /***************************************************************************************/
                // Emit an arguments object that points at null things

                MyClass val1 = new MyClass() { Url = "myUrlVal", Point = null };
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { cls = val1, propStr = "propVal1" });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestNullsTestSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("propVal1", eventSourceListener.LastEvent.Arguments["propStr"]);
                Assert.Equal("myUrlVal", eventSourceListener.LastEvent.Arguments["Url"]);
                eventSourceListener.ResetEventCountAndLastEvent();

                /***************************************************************************************/
                // Emit an arguments object that points at null things (variation 2)

                MyClass val2 = new MyClass() { Url = null, Point = new MyPoint() { X = 8, Y = 9 } };
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { cls = val2, propStr = "propVal1" });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestNullsTestSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("propVal1", eventSourceListener.LastEvent.Arguments["propStr"]);
                Assert.Equal("8", eventSourceListener.LastEvent.Arguments["cls_Point_X"]);
                eventSourceListener.ResetEventCountAndLastEvent();
            }
        }

        /// <summary>
        /// Tests the feature that supresses the implicit inclusion of serialable properties 
        /// of the payload object.  
        /// </summary>
        [Fact]
        public void TestNoImplicitTransforms()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener = new DiagnosticListener("TestNoImplictTransformsSource"))
            {
                Assert.Equal(0, eventSourceListener.EventCount);

                // use the - prefix to supress the implicit properties.  Thus you should only get propStr and Url.  
                eventSourceListener.Enable("TestNoImplictTransformsSource/TestEvent1:-propStr;cls.Url");

                /***************************************************************************************/
                // Emit an event that matches the first pattern. 
                MyClass val = new MyClass() { Url = "MyUrl", Point = new MyPoint() { X = 3, Y = 5 } };
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val, propStr2 = "there" });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestNoImplictTransformsSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("hi", eventSourceListener.LastEvent.Arguments["propStr"]);
                Assert.Equal("MyUrl", eventSourceListener.LastEvent.Arguments["Url"]);
                eventSourceListener.ResetEventCountAndLastEvent();
            }
        }

        /// <summary>
        /// Tests what happens when wacky characters are used in property specs.  
        /// </summary>
        [Fact]
        public void TestBadProperties()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener = new DiagnosticListener("TestBadPropertiesSource"))
            {
                Assert.Equal(0, eventSourceListener.EventCount);

                // This has a syntax error in the Url case, so it should be ignored.  
                eventSourceListener.Enable("TestBadPropertiesSource/TestEvent1:cls.Ur-+l");

                /***************************************************************************************/
                // Emit an event that matches the first pattern. 
                MyClass val = new MyClass() { Url = "MyUrl", Point = new MyPoint() { X = 3, Y = 5 } };
                if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                    diagnosticSourceListener.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val });

                Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                Assert.Equal("TestBadPropertiesSource", eventSourceListener.LastEvent.SourceName);
                Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                Assert.Equal("hi", eventSourceListener.LastEvent.Arguments["propStr"]);
                Assert.Equal("4", eventSourceListener.LastEvent.Arguments["propInt"]);
                eventSourceListener.ResetEventCountAndLastEvent();
            }
        }

        // Tests that messages about DiagnosticSourceEventSource make it out.  
        [Fact]
        public void TestMessages()
        {
            using (var eventSourceListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticSourceListener = new DiagnosticListener("TestMessagesSource"))
            {
                Assert.Equal(0, eventSourceListener.EventCount);

                // This is just to make debugging easier.  
                var messages = new List<string>();

                eventSourceListener.OtherEventWritten += delegate(EventWrittenEventArgs evnt)
                {
                    if (evnt.EventName == "Message")
                    {
                        var message = (string)evnt.Payload[0];
                        messages.Add(message);
                    }
                };

                // This has a syntax error in the Url case, so it should be ignored.  
                eventSourceListener.Enable("TestMessagesSource/TestEvent1:-cls.Url");
                Assert.Equal(0, eventSourceListener.EventCount);
                Assert.True(3 <= messages.Count);
            }
        }
    }

    /****************************************************************************/
    // classes to make up data for the tests.  

    /// <summary>
    /// classes for test data. 
    /// </summary>
    internal class MyClass
    {
        public string Url { get; set; }
        public MyPoint Point { get; set; }
    }

    /// <summary>
    /// classes for test data. 
    /// </summary>
    internal class MyPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /****************************************************************************/
    // Harness infrastructure
    /// <summary>
    /// TestDiagnosticSourceEventListener installs a EventWritten callback that updates EventCount and LastEvent.  
    /// </summary>
    class TestDiagnosticSourceEventListener : DiagnosticSourceEventListener
    {
        public TestDiagnosticSourceEventListener()
        {
            EventWritten += UpdateLastEvent;
        }

        public int EventCount;
        public DiagnosticSourceEvent LastEvent;
        public DiagnosticSourceEvent SecondLast;
        public DiagnosticSourceEvent ThirdLast;

        /// <summary>
        /// Sets the EventCount to 0 and LastEvent to null
        /// </summary>
        public void ResetEventCountAndLastEvent()
        {
            EventCount = 0;
            LastEvent = null;
            SecondLast = null;
            ThirdLast = null;
        }

        /// <summary>
        /// If present, will ignore events that don't cause this filter predicate to return true.  
        /// </summary>
        public Predicate<DiagnosticSourceEvent> Filter;

        #region private 
        private void UpdateLastEvent(DiagnosticSourceEvent anEvent)
        {
            if (Filter != null && !Filter(anEvent))
                return;

            ThirdLast = SecondLast;
            SecondLast = LastEvent;

            EventCount++;
            LastEvent = anEvent;
        }
        #endregion
    }

    /// <summary>
    /// Represents a single DiagnosticSource event.  
    /// </summary>
    class DiagnosticSourceEvent
    {
        public string SourceName;
        public string EventName;
        public Dictionary<string, string> Arguments;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  SourceName: \"").Append(SourceName ?? "").Append("\",").AppendLine();
            sb.Append("  EventName: \"").Append(EventName ?? "").Append("\",").AppendLine();
            sb.Append("  Arguments: ").Append("[").AppendLine();
            bool first = true;
            foreach (var keyValue in Arguments)
            {
                if (!first)
                    sb.Append(",").AppendLine();
                first = false;
                sb.Append("    ").Append(keyValue.Key).Append(": \"").Append(keyValue.Value).Append("\"");
            }
            sb.AppendLine().AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// A helper class that listens to Diagnostic sources and send events to the 'EventWritten'
    /// callback.   Standard use is to create the class set up the events of interested and
    /// use 'Enable'.  .   
    /// </summary>
    class DiagnosticSourceEventListener : EventListener
    {
        public DiagnosticSourceEventListener() { }

        /// <summary>
        /// Will be called when a DiagnosticSource event is fired. 
        /// </summary>
        public event Action<DiagnosticSourceEvent> EventWritten;

        /// <summary>
        /// It is possible that there are other events besides those that are being forwarded from 
        /// the DiagnosticSources.   These come here.   
        /// </summary>
        public event Action<EventWrittenEventArgs> OtherEventWritten;

        public void Enable(string filterAndPayloadSpecs)
        {
            var args = new Dictionary<string, string>();
            args.Add("FilterAndPayloadSpecs", filterAndPayloadSpecs);
            EnableEvents(_diagnosticSourceEventSource, EventLevel.Verbose, EventKeywords.All, args);
        }

        public void Disable()
        {
            DisableEvents(_diagnosticSourceEventSource);
        }

        /// <summary>
        /// Cleans this class up.  Among other things diables the DiagnosticSources being listened to.  
        /// </summary>
        public override void Dispose()
        {
            if (_diagnosticSourceEventSource != null)
            {
                Disable();
                _diagnosticSourceEventSource = null;
            }
        }

        #region private 
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            bool wroteEvent = false;
            var eventWritten = EventWritten;
            if (eventWritten != null)
            {
                if (eventData.EventName == "Event" && eventData.Payload.Count == 3)
                {
                    Debug.Assert(eventData.PayloadNames[0] == "SourceName");
                    Debug.Assert(eventData.PayloadNames[1] == "EventName");
                    Debug.Assert(eventData.PayloadNames[2] == "Arguments");

                    var anEvent = new DiagnosticSourceEvent();
                    anEvent.SourceName = eventData.Payload[0].ToString();
                    anEvent.EventName = eventData.Payload[1].ToString();
                    anEvent.Arguments = new Dictionary<string, string>();

                    var asKeyValueList = eventData.Payload[2] as IEnumerable<object>;
                    if (asKeyValueList != null)
                    {
                        foreach (IDictionary<string, object> keyvalue in asKeyValueList)
                        {
                            object key;
                            keyvalue.TryGetValue("Key", out key);
                            object value;
                            keyvalue.TryGetValue("Value", out value);
                            if (key != null && value != null)
                                anEvent.Arguments[key.ToString()] = value.ToString();
                        }
                    }
                    eventWritten(anEvent);
                    wroteEvent = true;
                }
            }

            var otherEventWritten = OtherEventWritten;
            if (otherEventWritten != null && !wroteEvent)
                otherEventWritten(eventData);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Diagnostics-DiagnosticSource")
                _diagnosticSourceEventSource = eventSource;
        }

        EventSource _diagnosticSourceEventSource;
        #endregion
    }
}
