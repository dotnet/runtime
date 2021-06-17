// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    //Complex types are not supported on EventSource for .NET 4.5
    public class DiagnosticSourceEventSourceBridgeTests
    {
        // To avoid interactions between tests when they are run in parallel, we run all these tests in their
        // own sub-process using RemoteExecutor.Invoke()  However this makes it very inconvenient to debug the test.
        // By seting this #if to true you stub out RemoteInvoke and the code will run in-proc which is useful
        // in debugging.
#if false
        class NullDispose : IDisposable
        {
            public void Dispose()
            {
            }
        }
        static IDisposable RemoteExecutor.Invoke(Action a)
        {
            a();
            return new NullDispose();
        }
#endif

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestEnableAllActivitySourcesAllEvents()
        {
            RemoteExecutor.Invoke(() =>
            {
                using TestDiagnosticSourceEventListener eventSourceListener = new TestDiagnosticSourceEventListener();
                ActivitySource [] sources = new ActivitySource[10];
                for (int i = 0; i < 10; i++)
                {
                    sources[i] = new ActivitySource($"Source{i}");
                }

                int eventsCount = 0;
                Assert.Equal(eventsCount, eventSourceListener.EventCount);
                eventSourceListener.Enable("  [AS]*  \r\n"); // All Sources + All Events

                Activity [] activities = new Activity[10];

                for (int i = 0; i < 10; i++)
                {
                    activities[i] = sources[i].StartActivity($"activity{i}");
                    Assert.NotNull(activities[i]);
                    Assert.Equal(++eventsCount, eventSourceListener.EventCount);
                    ValidateActivityEvents(eventSourceListener, "ActivityStart", sources[i].Name, activities[i].OperationName);
                    Assert.True(activities[i].IsAllDataRequested);
                    Assert.Equal(ActivityTraceFlags.Recorded, activities[i].ActivityTraceFlags);
                }

                for (int i = 0; i < 10; i++)
                {
                    activities[i].Dispose();
                    Assert.Equal(++eventsCount, eventSourceListener.EventCount);
                    ValidateActivityEvents(eventSourceListener, "ActivityStop", sources[i].Name, activities[i].OperationName);
                    sources[i].Dispose();
                }

            }).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("Start")]
        [InlineData("Stop")]
        [InlineData("start")]
        [InlineData("stop")]
        public void TestEnableAllActivitySourcesWithOneEvent(string eventName)
        {
            RemoteExecutor.Invoke((eventname) =>
            {
                using TestDiagnosticSourceEventListener eventSourceListener = new TestDiagnosticSourceEventListener();
                ActivitySource [] sources = new ActivitySource[10];
                for (int i = 0; i < 10; i++)
                {
                    sources[i] = new ActivitySource($"Source{i}");
                }

                int eventsCount = 0;
                Assert.Equal(eventsCount, eventSourceListener.EventCount);
                eventSourceListener.Enable($"  [AS]* / {eventname}  \r\n"); // All Sources + one Event

                Activity [] activities = new Activity[10];

                for (int i = 0; i < 10; i++)
                {
                    activities[i] = sources[i].StartActivity($"activity{i}");
                    Assert.NotNull(activities[i]);

                    if (eventname.Equals("Start", StringComparison.OrdinalIgnoreCase))
                    {
                        eventsCount++;
                        ValidateActivityEvents(eventSourceListener, "ActivityStart", sources[i].Name, activities[i].OperationName);
                    }
                    Assert.Equal(eventsCount, eventSourceListener.EventCount);
                    Assert.True(activities[i].IsAllDataRequested);
                    Assert.Equal(ActivityTraceFlags.Recorded, activities[i].ActivityTraceFlags);
                }

                for (int i = 0; i < 10; i++)
                {
                    activities[i].Dispose();

                    if (eventname.Equals("Stop", StringComparison.OrdinalIgnoreCase))
                    {
                        eventsCount++;
                        ValidateActivityEvents(eventSourceListener, "ActivityStop", sources[i].Name, activities[i].OperationName);
                    }

                    Assert.Equal(eventsCount, eventSourceListener.EventCount);
                    sources[i].Dispose();
                }
            }, eventName).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("Propagate", false, ActivityTraceFlags.None)]
        [InlineData("PROPAGATE", false, ActivityTraceFlags.None)]
        [InlineData("Record",    true,  ActivityTraceFlags.None)]
        [InlineData("recorD",    true,  ActivityTraceFlags.None)]
        [InlineData("",          true,  ActivityTraceFlags.Recorded)]
        public void TestEnableAllActivitySourcesWithSpeciifcSamplingResult(string samplingResult, bool alldataRequested, ActivityTraceFlags activityTraceFlags)
        {
            RemoteExecutor.Invoke((result, dataRequested, traceFlags) =>
            {
                using TestDiagnosticSourceEventListener eventSourceListener = new TestDiagnosticSourceEventListener();
                using ActivitySource source = new ActivitySource("SamplingSource");

                Assert.Equal(0, eventSourceListener.EventCount);
                eventSourceListener.Enable($"  [AS]* /- {result}  \r\n"); // All Sources + one Event


                Activity activity = source.StartActivity($"samplingActivity");
                Assert.NotNull(activity);
                Assert.Equal(1, eventSourceListener.EventCount);
                ValidateActivityEvents(eventSourceListener, "ActivityStart", source.Name, activity.OperationName);

                Assert.Equal(bool.Parse(dataRequested), activity.IsAllDataRequested);
                Assert.Equal(traceFlags, activity.ActivityTraceFlags.ToString());

                activity.Dispose();

                Assert.Equal(2, eventSourceListener.EventCount);
                ValidateActivityEvents(eventSourceListener, "ActivityStop", source.Name, activity.OperationName);
            }, samplingResult, alldataRequested.ToString(), activityTraceFlags.ToString()).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("",      "",          true)]
        [InlineData("Start", "",          true)]
        [InlineData("stop",  "",          true)]
        [InlineData("",      "Propagate", false)]
        [InlineData("",      "Record",    true)]
        [InlineData("Start", "Propagate", false)]
        [InlineData("Stop",  "Record",    true)]
        public void TestDefaultActivitySource(string eventName, string samplingResult, bool alldataRequested)
        {
            RemoteExecutor.Invoke((eventname, result, dataRequested) =>
            {
                using TestDiagnosticSourceEventListener eventSourceListener = new TestDiagnosticSourceEventListener();
                Activity a = new Activity(""); // we need this to ensure DiagnosticSourceEventSource.Logger creation.

                Assert.Equal(0, eventSourceListener.EventCount);
                eventSourceListener.Enable($"  [AS]/{eventname}-{result}\r\n");
                Assert.Equal("", a.Source.Name);

                a = a.Source.StartActivity("newOne");

                Assert.Equal(bool.Parse(dataRequested), a.IsAllDataRequested);
                // All Activities created with "new Activity(...)" will have ActivityTraceFlags is `None`;
                Assert.Equal(result.Length == 0 ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None, a.ActivityTraceFlags);

                a.Dispose();

                int eCount = eventname.Length == 0 ? 2 : 1;
                Assert.Equal(eCount, eventSourceListener.EventCount);

                // None Default Source
                ActivitySource source = new ActivitySource("NoneDefault"); // not the default ActivitySource
                Activity activity = source.StartActivity($"ActivityFromNoneDefault"); // Shouldn't fire any event
                Assert.Equal(eCount, eventSourceListener.EventCount);
                Assert.Null(activity);
            }, eventName, samplingResult, alldataRequested.ToString()).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("[AS]*\r\n [AS]Specific/-Propagate\r\n [AS]*/-Propagate\r\n", false, true)]
        [InlineData("[AS]AnotherSource/-Propagate\r\n [AS]*/-Propagate\r\n [AS]Specific/-Record\r\n [AS]*\r\n", true, true)]
        [InlineData("[AS]*/-Propagate\r\n  [AS]*/-Propagate\r\n [AS]Specific/-Propagate\r\n[AS]Specific/-Record\r\n", true, false)]
        [InlineData("[AS]*/-Propagate\r\n", false, false)]
        [InlineData("[AS]*/-Record\r\n", true, true)]
        [InlineData("[AS]Specific/-Propagate\r\n [AS]NoneSpecific/-Record\r\n", false, true)]
        [InlineData("[AS]Specific/-Record\r\n [AS]NoneSpecific/-Propagate\r\n", true, false)]
        [InlineData("[AS]Specific\r\n [AS]NoneSpecific\r\n", true, true)]
        public void TestMultipleSpecs(string spec, bool isAlldataRequestedFromSpecif, bool alldataRequestedFromNoneSpecific)
        {
            RemoteExecutor.Invoke((specString, specificAllData, noneSpecificAllData) =>
            {
                using TestDiagnosticSourceEventListener eventSourceListener = new TestDiagnosticSourceEventListener();
                using ActivitySource aSource1 = new ActivitySource("Specific");
                eventSourceListener.Enable(specString);

                Assert.Equal(0, eventSourceListener.EventCount);

                Activity a1 = aSource1.StartActivity("a1");
                Assert.NotNull(a1);
                Assert.Equal(1, eventSourceListener.EventCount);
                Assert.Equal(bool.Parse(specificAllData), a1.IsAllDataRequested);
                a1.Dispose();
                Assert.Equal(2, eventSourceListener.EventCount);

                using ActivitySource aSource2 = new ActivitySource("NoneSpecific");
                Activity a2 = aSource2.StartActivity("a2");
                Assert.NotNull(a2);
                Assert.Equal(3, eventSourceListener.EventCount);
                Assert.Equal(bool.Parse(noneSpecificAllData), a2.IsAllDataRequested);
                a2.Dispose();
                Assert.Equal(4, eventSourceListener.EventCount);

            }, spec, isAlldataRequestedFromSpecif.ToString(), alldataRequestedFromNoneSpecific.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestTransformSpecs()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (TestDiagnosticSourceEventListener eventSourceListener = new TestDiagnosticSourceEventListener())
                {
                    using ActivitySource aSource1 = new ActivitySource("TransSpecsSource");
                    eventSourceListener.Enable("[AS]*");

                    Activity a = aSource1.StartActivity("TransSpecs");
                    ValidateActivityEvents(eventSourceListener, "ActivityStart", "TransSpecsSource", "TransSpecs");

                    var list1 = eventSourceListener.LastEvent.Arguments;
                    Assert.Equal(a.OperationName, list1["OperationName"]);

                    a.Dispose();
                    ValidateActivityEvents(eventSourceListener, "ActivityStop", "TransSpecsSource", "TransSpecs");

                    var list2 = eventSourceListener.LastEvent.Arguments;
                    Assert.Equal(a.OperationName, list2["OperationName"]);

                    Assert.Equal(list1.Count, list2.Count);
                    foreach (string key in list1.Keys)
                    {
                        Assert.NotNull(list2[key]);
                    }

                    // Suppress all
                    eventSourceListener.Enable("[AS]*:-");
                    a = aSource1.StartActivity("TransSpecs");
                    Assert.Equal(0, eventSourceListener.LastEvent.Arguments.Count);
                    a.Stop();
                    Assert.Equal(0, eventSourceListener.LastEvent.Arguments.Count);

                    // Suppress all except ActivitySource name
                    eventSourceListener.Enable("[AS]*:-ActivitySourceName=Source.Name");
                    a = aSource1.StartActivity("TransSpecs");
                    Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(aSource1.Name, eventSourceListener.LastEvent.Arguments["ActivitySourceName"]);

                    a.Stop();
                    Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(aSource1.Name, eventSourceListener.LastEvent.Arguments["ActivitySourceName"]);

                    // Collect TraceId, SpanId, and ParentSpanId only
                    eventSourceListener.Enable("[AS]*:-TraceId;SpanId;ParentSpanId");
                    a = aSource1.StartActivity("ActivityData");
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);

                    Assert.Equal(a.SpanId.ToString(), eventSourceListener.LastEvent.Arguments["SpanId"]);
                    Assert.Equal(a.TraceId.ToString(), eventSourceListener.LastEvent.Arguments["TraceId"]);
                    Assert.Equal(a.ParentSpanId.ToString(), eventSourceListener.LastEvent.Arguments["ParentSpanId"]);

                    a.Stop();
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(a.SpanId.ToString(), eventSourceListener.LastEvent.Arguments["SpanId"]);
                    Assert.Equal(a.TraceId.ToString(), eventSourceListener.LastEvent.Arguments["TraceId"]);
                    Assert.Equal(a.ParentSpanId.ToString(), eventSourceListener.LastEvent.Arguments["ParentSpanId"]);
                }

            }).Dispose();
        }


        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestFilteringWithActivityName()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (TestDiagnosticSourceEventListener eventSourceListener = new TestDiagnosticSourceEventListener())
                {
                    using ActivitySource aSource1 = new ActivitySource("Source1");
                    using ActivitySource aSource2 = new ActivitySource("Source2");

                    eventSourceListener.Enable("[AS]*+MyActivity");
                    Assert.Equal(0, eventSourceListener.EventCount);

                    Activity a = aSource1.StartActivity("NotMyActivity"); // Shouldn't get created because nt matching MyActivity
                    Assert.Null(a);
                    Assert.Equal(0, eventSourceListener.EventCount);

                    a = aSource1.StartActivity("MyActivity");
                    Assert.NotNull(a);
                    Assert.Equal(1, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);
                    a.Stop();
                    Assert.Equal(2, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);

                    a = aSource2.StartActivity("MyActivity");
                    Assert.NotNull(a);
                    Assert.Equal(3, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);
                    a.Stop();
                    Assert.Equal(4, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);

                    //
                    // Now test with specific ActivitySource and ActivityName
                    //

                    eventSourceListener.Enable("[AS]MySource+MyActivity");
                    a = aSource1.StartActivity("MyActivity"); // Not from MySource
                    Assert.Null(a);
                    Assert.Equal(4, eventSourceListener.EventCount);
                    using ActivitySource aSource3 = new ActivitySource("MySource");
                    a = aSource3.StartActivity("NotMyActivity"); // from MySource but NoMyActivity
                    Assert.Null(a);
                    Assert.Equal(4, eventSourceListener.EventCount);

                    a = aSource3.StartActivity("MyActivity"); // from MySource and MyActivity
                    Assert.NotNull(a);
                    Assert.Equal(5, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);

                    a.Stop();
                    Assert.Equal(6, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);

                    //
                    // test with full query
                    //
                    eventSourceListener.Enable("[AS]MySource+MyActivity/Start-Propagate");
                    a = aSource3.StartActivity("MyActivity"); // from MySource and MyActivity
                    Assert.NotNull(a);
                    Assert.Equal(7, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);
                    Assert.False(a.IsAllDataRequested);

                    a.Stop(); // shouldn't fire
                    Assert.Equal(7, eventSourceListener.EventCount);

                    //
                    // test with default Source
                    //
                    eventSourceListener.Enable("[AS]+MyActivity");
                    a = new Activity("MyActivity");
                    a.Start();
                    Assert.Equal(8, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);
                    Assert.True(a.IsAllDataRequested);

                    a.Stop();
                    Assert.Equal(9, eventSourceListener.EventCount);
                    Assert.Equal(a.OperationName, eventSourceListener.LastEvent.Arguments["OperationName"]);

                    a = new Activity("NotMyActivity");
                    a.Start(); // nothing fire
                    Assert.Equal(9, eventSourceListener.EventCount);

                    //
                    // Test with Empty ActivityName
                    //
                    eventSourceListener.Enable("[AS]+");
                    a = new Activity("");
                    a.Start();
                    Assert.Equal(10, eventSourceListener.EventCount);
                    a.Stop();
                    Assert.Equal(11, eventSourceListener.EventCount);

                    a = aSource3.StartActivity("");
                    Assert.Null(a);
                }

            }).Dispose();
        }

        internal void ValidateActivityEvents(TestDiagnosticSourceEventListener eventSourceListener, string eventName, string sourceName, string activityName)
        {
            Assert.Equal(eventName, eventSourceListener.LastEvent.EventSourceEventName);
            Assert.Equal(sourceName, eventSourceListener.LastEvent.SourceName);
            Assert.Equal(activityName, eventSourceListener.LastEvent.EventName);
        }

        /// <summary>
        /// Tests the basic functionality of turning on specific EventSources and specifying
        /// the events you want.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestSpecificEvents()
        {
            RemoteExecutor.Invoke(() =>
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
                    Assert.Equal(5, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
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
                    Assert.Equal(4, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
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
            }).Dispose();
        }

        /// <summary>
        /// Test that things work properly for Linux newline conventions.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void LinuxNewLineConventions()
        {
            RemoteExecutor.Invoke(() =>
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
            }).Dispose();
        }

        /// <summary>
        /// Tests what happens when you wildcard the source name (empty string)
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestWildCardSourceName()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventSourceListener = new TestDiagnosticSourceEventListener())
                using (var diagnosticSourceListener1 = new DiagnosticListener("TestWildCardSourceName1"))
                using (var diagnosticSourceListener2 = new DiagnosticListener("TestWildCardSourceName2"))
                {
                    eventSourceListener.Filter = (DiagnosticSourceEvent evnt) => evnt.SourceName.StartsWith("TestWildCardSourceName");

                    // Turn On Everything.  Note that because of concurrent testing, we may get other sources as well.
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
            }).Dispose();
        }

        /// <summary>
        /// Tests what happens when you wildcard event name (but not the source name)
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestWildCardEventName()
        {
            RemoteExecutor.Invoke(() =>
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
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
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
                    Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
                    Assert.Equal("hi2", eventSourceListener.LastEvent.Arguments["propStr2"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    /***************************************************************************************/
                    // Emit an event with the same schema as the first event.   (uses first-event cache)
                    val = new MyClass() { };
                    if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                        diagnosticSourceListener.Write("TestEvent1", new { propStr = "hiThere", propInt = 5, cls = val });

                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("TestWildCardEventNameSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
                    Assert.Equal("hiThere", eventSourceListener.LastEvent.Arguments["propStr"]);
                    Assert.Equal("5", eventSourceListener.LastEvent.Arguments["propInt"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    /***************************************************************************************/
                    // Emit an event with the same schema as the second event.  (uses dictionary cache)
                    if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                        diagnosticSourceListener.Write("TestEvent1", new { propStr2 = "hi3", cls = val });

                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("TestWildCardEventNameSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
                    Assert.Equal("hi3", eventSourceListener.LastEvent.Arguments["propStr2"]);
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
            }).Dispose();
        }

        public class PropertyThrow
        {
            public string property1 => "P1";
            public string property2 => throw new Exception("Always throw!");
            public string property3 => "P3";
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestWithPropertyThrowing()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventSourceListener = new TestDiagnosticSourceEventListener())
                using (var diagnosticSourceListener = new DiagnosticListener("TestThrows"))
                {
                    eventSourceListener.Enable("TestThrows/TestEvent1");

                    if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                        diagnosticSourceListener.Write("TestEvent1", new PropertyThrow());

                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("TestThrows", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("P1", eventSourceListener.LastEvent.Arguments["property1"]);
                    Assert.Equal("P3", eventSourceListener.LastEvent.Arguments["property3"]);
                    Assert.Equal("", eventSourceListener.LastEvent.Arguments["property2"]);
                    eventSourceListener.ResetEventCountAndLastEvent();
                }
            }).Dispose();
        }

        /// <summary>
        /// Test what happens when there are nulls passed in the event payloads
        /// Basically strings get turned into empty strings and other nulls are typically
        /// ignored.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestNulls()
        {
            RemoteExecutor.Invoke(() =>
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
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("", eventSourceListener.LastEvent.Arguments["cls"]);           // Tostring() on a null end up as an empty string.
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
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val1.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
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
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val2.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
                    Assert.Equal("propVal1", eventSourceListener.LastEvent.Arguments["propStr"]);
                    Assert.Equal("8", eventSourceListener.LastEvent.Arguments["cls_Point_X"]);
                    eventSourceListener.ResetEventCountAndLastEvent();
                }
            }).Dispose();
        }

        /// <summary>
        /// Tests the feature that suppresses the implicit inclusion of serialable properties
        /// of the payload object.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestNoImplicitTransforms()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventSourceListener = new TestDiagnosticSourceEventListener())
                using (var diagnosticSourceListener = new DiagnosticListener("TestNoImplicitTransformsSource"))
                {
                    Assert.Equal(0, eventSourceListener.EventCount);

                    // use the - prefix to suppress the implicit properties.  Thus you should only get propStr and Url.
                    eventSourceListener.Enable("TestNoImplicitTransformsSource/TestEvent1:-propStr;cls.Url");

                    /***************************************************************************************/
                    // Emit an event that matches the first pattern.
                    MyClass val = new MyClass() { Url = "MyUrl", Point = new MyPoint() { X = 3, Y = 5 } };
                    if (diagnosticSourceListener.IsEnabled("TestEvent1"))
                        diagnosticSourceListener.Write("TestEvent1", new { propStr = "hi", propInt = 4, cls = val, propStr2 = "there" });

                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("TestNoImplicitTransformsSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestEvent1", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(2, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("hi", eventSourceListener.LastEvent.Arguments["propStr"]);
                    Assert.Equal("MyUrl", eventSourceListener.LastEvent.Arguments["Url"]);
                    eventSourceListener.ResetEventCountAndLastEvent();
                }
            }).Dispose();
        }

        /// <summary>
        /// Tests what happens when wacky characters are used in property specs.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestBadProperties()
        {
            RemoteExecutor.Invoke(() =>
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
                    Assert.Equal(3, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal(val.GetType().FullName, eventSourceListener.LastEvent.Arguments["cls"]);  // ToString on cls is the class name
                    Assert.Equal("hi", eventSourceListener.LastEvent.Arguments["propStr"]);
                    Assert.Equal("4", eventSourceListener.LastEvent.Arguments["propInt"]);
                    eventSourceListener.ResetEventCountAndLastEvent();
                }
            }).Dispose();
        }

        // Tests that messages about DiagnosticSourceEventSource make it out.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestMessages()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventSourceListener = new TestDiagnosticSourceEventListener())
                using (var diagnosticSourceListener = new DiagnosticListener("TestMessagesSource"))
                {
                    Assert.Equal(0, eventSourceListener.EventCount);

                    // This is just to make debugging easier.
                    var messages = new List<string>();

                    eventSourceListener.OtherEventWritten += delegate (EventWrittenEventArgs evnt)
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
            }).Dispose();
        }

        /// <summary>
        /// Tests the feature to send the messages as EventSource Activities.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestActivities()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventSourceListener = new TestDiagnosticSourceEventListener())
                using (var diagnosticSourceListener = new DiagnosticListener("TestActivitiesSource"))
                {
                    Assert.Equal(0, eventSourceListener.EventCount);
                    eventSourceListener.Enable(
                        "TestActivitiesSource/TestActivity1Start@Activity1Start\r\n" +
                        "TestActivitiesSource/TestActivity1Stop@Activity1Stop\r\n" +
                        "TestActivitiesSource/TestActivity2Start@Activity2Start\r\n" +
                        "TestActivitiesSource/TestActivity2Stop@Activity2Stop\r\n" +
                        "TestActivitiesSource/TestEvent\r\n"
                        );

                    // Start activity 1
                    diagnosticSourceListener.Write("TestActivity1Start", new { propStr = "start" });
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity1Start", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("TestActivitiesSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestActivity1Start", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("start", eventSourceListener.LastEvent.Arguments["propStr"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    // Start nested activity 2
                    diagnosticSourceListener.Write("TestActivity2Start", new { propStr = "start" });
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity2Start", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("TestActivitiesSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestActivity2Start", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("start", eventSourceListener.LastEvent.Arguments["propStr"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    // Send a normal event
                    diagnosticSourceListener.Write("TestEvent", new { propStr = "event" });
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Event", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("TestActivitiesSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestEvent", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("event", eventSourceListener.LastEvent.Arguments["propStr"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    // Stop nested activity 2
                    diagnosticSourceListener.Write("TestActivity2Stop", new { propStr = "stop" });
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity2Stop", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("TestActivitiesSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestActivity2Stop", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("stop", eventSourceListener.LastEvent.Arguments["propStr"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    // Stop activity 1
                    diagnosticSourceListener.Write("TestActivity1Stop", new { propStr = "stop" });
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity1Stop", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("TestActivitiesSource", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("TestActivity1Stop", eventSourceListener.LastEvent.EventName);
                    Assert.Equal(1, eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("stop", eventSourceListener.LastEvent.Arguments["propStr"]);
                    eventSourceListener.ResetEventCountAndLastEvent();
                }
            }).Dispose();
        }

        /// <summary>
        /// Tests that keywords that define shortcuts work.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestShortcutKeywords()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventSourceListener = new TestDiagnosticSourceEventListener())
                // These are look-alikes for the real ones.
                using (var aspNetCoreSource = new DiagnosticListener("Microsoft.AspNetCore"))
                using (var entityFrameworkCoreSource = new DiagnosticListener("Microsoft.EntityFrameworkCore"))
                {
                    // Sadly we have a problem where if something else has turned on Microsoft-Diagnostics-DiagnosticSource then
                    // its keywords are ORed with our and because the shortcuts require that IgnoreShortCutKeywords is OFF
                    // Something outside this test (the debugger seems to do this), will cause the test to fail.
                    // Currently we simply give up in that case (but it really is a deeper problem.
                    var IgnoreShortCutKeywords = (EventKeywords)0x0800;
                    foreach (var eventSource in EventSource.GetSources())
                    {
                        if (eventSource.Name == "Microsoft-Diagnostics-DiagnosticSource")
                        {
                            if (eventSource.IsEnabled(EventLevel.Informational, IgnoreShortCutKeywords))
                                return; // Don't do the testing.
                        }
                    }

                    // These are from DiagnosticSourceEventListener.
                    var Messages = (EventKeywords)0x1;
                    var Events = (EventKeywords)0x2;
                    var AspNetCoreHosting = (EventKeywords)0x1000;
                    var EntityFrameworkCoreCommands = (EventKeywords)0x2000;

                    // Turn on listener using just the keywords
                    eventSourceListener.Enable(null, Messages | Events | AspNetCoreHosting | EntityFrameworkCoreCommands);

                    Assert.Equal(0, eventSourceListener.EventCount);

                    // Start a ASP.NET Request
                    aspNetCoreSource.Write("Microsoft.AspNetCore.Hosting.BeginRequest",
                        new
                        {
                            httpContext = new
                            {
                                Request = new
                                {
                                    Method = "Get",
                                    Host = "MyHost",
                                    Path = "MyPath",
                                    QueryString = "MyQuery"
                                }
                            }
                        });
                    // Check that the morphs work as expected.
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity1Start", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("Microsoft.AspNetCore", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.BeginRequest", eventSourceListener.LastEvent.EventName);
                    Assert.True(4 <= eventSourceListener.LastEvent.Arguments.Count);
                    Debug.WriteLine("Arg Keys = " + string.Join(" ", eventSourceListener.LastEvent.Arguments.Keys));
                    Debug.WriteLine("Arg Values = " + string.Join(" ", eventSourceListener.LastEvent.Arguments.Values));
                    Assert.Equal("Get", eventSourceListener.LastEvent.Arguments["Method"]);
                    Assert.Equal("MyHost", eventSourceListener.LastEvent.Arguments["Host"]);
                    Assert.Equal("MyPath", eventSourceListener.LastEvent.Arguments["Path"]);
                    Assert.Equal("MyQuery", eventSourceListener.LastEvent.Arguments["QueryString"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    // Start a SQL command
                    entityFrameworkCoreSource.Write("Microsoft.EntityFrameworkCore.BeforeExecuteCommand",
                        new
                        {
                            Command = new
                            {
                                Connection = new
                                {
                                    DataSource = "MyDataSource",
                                    Database = "MyDatabase",
                                },
                                CommandText = "MyCommand"
                            }
                        });
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity2Start", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("Microsoft.EntityFrameworkCore", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("Microsoft.EntityFrameworkCore.BeforeExecuteCommand", eventSourceListener.LastEvent.EventName);
                    Assert.True(3 <= eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("MyDataSource", eventSourceListener.LastEvent.Arguments["DataSource"]);
                    Assert.Equal("MyDatabase", eventSourceListener.LastEvent.Arguments["Database"]);
                    Assert.Equal("MyCommand", eventSourceListener.LastEvent.Arguments["CommandText"]);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    // Stop the SQL command
                    entityFrameworkCoreSource.Write("Microsoft.EntityFrameworkCore.AfterExecuteCommand", null);
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity2Stop", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("Microsoft.EntityFrameworkCore", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("Microsoft.EntityFrameworkCore.AfterExecuteCommand", eventSourceListener.LastEvent.EventName);
                    eventSourceListener.ResetEventCountAndLastEvent();

                    // Stop the ASP.NET request.
                    aspNetCoreSource.Write("Microsoft.AspNetCore.Hosting.EndRequest",
                        new
                        {
                            httpContext = new
                            {
                                Response = new
                                {
                                    StatusCode = "200"
                                },
                                TraceIdentifier = "MyTraceId"
                            }
                        });
                    Assert.Equal(1, eventSourceListener.EventCount); // Exactly one more event has been emitted.
                    Assert.Equal("Activity1Stop", eventSourceListener.LastEvent.EventSourceEventName);
                    Assert.Equal("Microsoft.AspNetCore", eventSourceListener.LastEvent.SourceName);
                    Assert.Equal("Microsoft.AspNetCore.Hosting.EndRequest", eventSourceListener.LastEvent.EventName);
                    Assert.True(2 <= eventSourceListener.LastEvent.Arguments.Count);
                    Assert.Equal("MyTraceId", eventSourceListener.LastEvent.Arguments["TraceIdentifier"]);
                    Assert.Equal("200", eventSourceListener.LastEvent.Arguments["StatusCode"]);
                    eventSourceListener.ResetEventCountAndLastEvent();
                }
            }).Dispose();
        }

        [OuterLoop("Runs for several seconds")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Stress_WriteConcurrently_DoesntCrash()
        {
            const int StressTimeSeconds = 4;
            RemoteExecutor.Invoke(() =>
            {
                using (new TurnOnAllEventListener())
                using (var source = new DiagnosticListener("testlistener"))
                {
                    var ce = new CountdownEvent(Environment.ProcessorCount * 2);
                    for (int i = 0; i < ce.InitialCount; i++)
                    {
                        new Thread(() =>
                        {
                            DateTime end = DateTime.UtcNow.Add(TimeSpan.FromSeconds(StressTimeSeconds));
                            while (DateTime.UtcNow < end)
                            {
                                source.Write("event1", Tuple.Create(1));
                                source.Write("event2", Tuple.Create(1, 2));
                                source.Write("event3", Tuple.Create(1, 2, 3));
                                source.Write("event4", Tuple.Create(1, 2, 3, 4));
                                source.Write("event5", Tuple.Create(1, 2, 3, 4, 5));
                            }
                            ce.Signal();
                        })
                        { IsBackground = true }.Start();
                    }
                    ce.Wait();
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IndexGetters_DontThrow()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventListener = new TestDiagnosticSourceEventListener())
                using (var diagnosticListener = new DiagnosticListener("MySource"))
                {
                    eventListener.Enable(
                        "MySource/MyEvent"
                    );
                    // The type MyEvent only declares 3 Properties, but actually
                    // has 4 due to the implicit Item property from having the index
                    // operator implemented. The Getter for this Item property
                    // is unusual for Property getters because it takes
                    // an int32 as an input. This test ensures that this
                    // implicit Property isn't implicitly serialized by
                    // DiagnosticSourceEventSource.
                    diagnosticListener.Write(
                        "MyEvent",
                        new MyEvent
                        {
                            Number = 1,
                            OtherNumber = 2
                        }
                    );
                    Assert.Equal(1, eventListener.EventCount);
                    Assert.Equal("MySource", eventListener.LastEvent.SourceName);
                    Assert.Equal("MyEvent", eventListener.LastEvent.EventName);
                    Assert.True(eventListener.LastEvent.Arguments.Count <= 3);
                    Assert.Equal("1", eventListener.LastEvent.Arguments["Number"]);
                    Assert.Equal("2", eventListener.LastEvent.Arguments["OtherNumber"]);
                    Assert.Equal("2", eventListener.LastEvent.Arguments["Count"]);
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ActivityObjectsAreInspectable()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var eventListener = new TestDiagnosticSourceEventListener())
                using (var diagnosticListener = new DiagnosticListener("MySource"))
                {
                    string activityProps =
                        "-DummyProp" +
                        ";ActivityId=*Activity.Id" +
                        ";ActivityStartTime=*Activity.StartTimeUtc.Ticks" +
                        ";ActivityDuration=*Activity.Duration.Ticks" +
                        ";ActivityOperationName=*Activity.OperationName" +
                        ";ActivityIdFormat=*Activity.IdFormat" +
                        ";ActivityParentId=*Activity.ParentId" +
                        ";ActivityTags=*Activity.Tags.*Enumerate" +
                        ";ActivityTraceId=*Activity.TraceId" +
                        ";ActivitySpanId=*Activity.SpanId" +
                        ";ActivityTraceStateString=*Activity.TraceStateString" +
                        ";ActivityParentSpanId=*Activity.ParentSpanId";
                    eventListener.Enable(
                        "MySource/TestActivity1.Start@Activity1Start:" + activityProps + "\r\n" +
                        "MySource/TestActivity1.Stop@Activity1Stop:" + activityProps + "\r\n" +
                        "MySource/TestActivity2.Start@Activity2Start:" + activityProps + "\r\n" +
                        "MySource/TestActivity2.Stop@Activity2Stop:" + activityProps + "\r\n"
                        );

                    Activity activity1 = new Activity("TestActivity1");
                    activity1.SetIdFormat(ActivityIdFormat.W3C);
                    activity1.TraceStateString = "hi_there";
                    activity1.AddTag("one", "1");
                    activity1.AddTag("two", "2");

                    diagnosticListener.StartActivity(activity1, new { DummyProp = "val" });
                    Assert.Equal(1, eventListener.EventCount);
                    AssertActivityMatchesEvent(activity1, eventListener.LastEvent, isStart: true);

                    Activity activity2 = new Activity("TestActivity2");
                    diagnosticListener.StartActivity(activity2, new { DummyProp = "val" });
                    Assert.Equal(2, eventListener.EventCount);
                    AssertActivityMatchesEvent(activity2, eventListener.LastEvent, isStart: true);

                    diagnosticListener.StopActivity(activity2, new { DummyProp = "val" });
                    Assert.Equal(3, eventListener.EventCount);
                    AssertActivityMatchesEvent(activity2, eventListener.LastEvent, isStart: false);

                    diagnosticListener.StopActivity(activity1, new { DummyProp = "val" });
                    Assert.Equal(4, eventListener.EventCount);
                    AssertActivityMatchesEvent(activity1, eventListener.LastEvent, isStart: false);

                }
            }).Dispose();
        }

        private void AssertActivityMatchesEvent(Activity a, DiagnosticSourceEvent e, bool isStart)
        {
            Assert.Equal("MySource", e.SourceName);
            Assert.Equal(a.OperationName + (isStart ? ".Start" : ".Stop"), e.EventName);
            Assert.Equal("val", e.Arguments["DummyProp"]);
            Assert.Equal(a.Id, e.Arguments["ActivityId"]);
            Assert.Equal(a.StartTimeUtc.Ticks.ToString(), e.Arguments["ActivityStartTime"]);
            if (!isStart)
            {
                Assert.Equal(a.Duration.Ticks.ToString(), e.Arguments["ActivityDuration"]);
            }
            Assert.Equal(a.OperationName, e.Arguments["ActivityOperationName"]);
            if (a.ParentId == null)
            {
                Assert.True(!e.Arguments.ContainsKey("ActivityParentId"));
            }
            else
            {
                Assert.Equal(a.ParentId, e.Arguments["ActivityParentId"]);
            }
            Assert.Equal(a.IdFormat.ToString(), e.Arguments["ActivityIdFormat"]);
            if (a.IdFormat == ActivityIdFormat.W3C)
            {
                Assert.Equal(a.TraceId.ToString(), e.Arguments["ActivityTraceId"]);
                Assert.Equal(a.SpanId.ToString(), e.Arguments["ActivitySpanId"]);
                Assert.Equal(a.TraceStateString, e.Arguments["ActivityTraceStateString"]);
                if(a.ParentSpanId != default)
                {
                    Assert.Equal(a.ParentSpanId.ToString(), e.Arguments["ActivityParentSpanId"]);
                }
            }
            Assert.Equal(string.Join(',', a.Tags), e.Arguments["ActivityTags"]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50924", TestPlatforms.Android)]
        public void NoExceptionThrownWhenProcessingStaticActivityProperties()
        {
            // Ensures that no exception is thrown when static properties on the Activity type are passed to EventListener.

            using (var eventListener = new TestDiagnosticSourceEventListener())
            using (var diagnosticListener = new DiagnosticListener("MySource"))
            {
                string activityProps =
                "-DummyProp" +
                ";ActivityEvents=*Activity.DefaultIdFormat" +
                ";ActivityBaggage=*Activity.Current" +
                ";ActivityContext=*Activity.ForceDefaultIdFormat";
                eventListener.Enable(
                    "MySource/TestActivity1.Start@Activity1Start:" + activityProps + "\r\n" +
                    "MySource/TestActivity1.Stop@Activity1Stop:" + activityProps + "\r\n" +
                    "MySource/TestActivity2.Start@Activity2Start:" + activityProps + "\r\n" +
                    "MySource/TestActivity2.Stop@Activity2Stop:" + activityProps + "\r\n"
                    );

                Activity activity1 = new Activity("TestActivity1");
                activity1.SetIdFormat(ActivityIdFormat.W3C);
                activity1.TraceStateString = "hi_there";
                activity1.AddTag("one", "1");
                activity1.AddTag("two", "2");

                var obj = new { DummyProp = "val" };

                diagnosticListener.StartActivity(activity1, obj);
                Assert.Equal(1, eventListener.EventCount);
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

    /// <summary>
    /// classes for test data
    /// </summary>
    internal class MyEvent
    {
        public int Number { get; set; }
        public int OtherNumber { get; set; }
        public int Count => 2;
        public KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(Number), Number),
            1 => new KeyValuePair<string, object>(nameof(OtherNumber), OtherNumber),
            _ => throw new IndexOutOfRangeException()
        };
    }

    /****************************************************************************/
    // Harness infrastructure
    /// <summary>
    /// TestDiagnosticSourceEventListener installs a EventWritten callback that updates EventCount and LastEvent.
    /// </summary>
    internal sealed class TestDiagnosticSourceEventListener : DiagnosticSourceEventListener
    {
        public TestDiagnosticSourceEventListener()
        {
            EventWritten += UpdateLastEvent;
        }

        public int EventCount;
        public DiagnosticSourceEvent LastEvent;

        // Here just for debugging.  Lets you see the last 3 events that were sent.
        public DiagnosticSourceEvent SecondLast;
        public DiagnosticSourceEvent ThirdLast;

        /// <summary>
        /// Sets the EventCount to 0 and LastEvents to null
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
    internal sealed class DiagnosticSourceEvent
    {
        public string SourceName;
        public string EventName;
        public Dictionary<string, string> Arguments;

        // Not typically important.
        public string EventSourceEventName;    // This is the name of the EventSourceEvent that carried the data. Only important for activities.

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
    internal class DiagnosticSourceEventListener : EventListener
    {
        public DiagnosticSourceEventListener() { }

        /// <summary>
        /// Will be called when a DiagnosticSource event is fired.
        /// </summary>
        public new event Action<DiagnosticSourceEvent> EventWritten;

        /// <summary>
        /// It is possible that there are other events besides those that are being forwarded from
        /// the DiagnosticSources.   These come here.
        /// </summary>
        public event Action<EventWrittenEventArgs> OtherEventWritten;

        public void Enable(string filterAndPayloadSpecs, EventKeywords keywords = EventKeywords.All)
        {
            var args = new Dictionary<string, string>();
            if (filterAndPayloadSpecs != null)
                args.Add("FilterAndPayloadSpecs", filterAndPayloadSpecs);
            EnableEvents(_diagnosticSourceEventSource, EventLevel.Verbose, keywords, args);
        }

        public void Disable()
        {
            DisableEvents(_diagnosticSourceEventSource);
        }

        /// <summary>
        /// Cleans this class up.  Among other things disables the DiagnosticSources being listened to.
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
                if (eventData.Payload.Count == 3 && (eventData.EventName == "Event" || eventData.EventName.Contains("Activity")))
                {
                    Debug.Assert(eventData.PayloadNames[0] == "SourceName");
                    Debug.Assert(eventData.PayloadNames[1] == "EventName" || eventData.PayloadNames[1] == "ActivityName");
                    Debug.Assert(eventData.PayloadNames[2] == "Arguments");

                    var anEvent = new DiagnosticSourceEvent();
                    anEvent.EventSourceEventName = eventData.EventName;
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

            if (eventData.EventName == "EventSourceMessage" && 0 < eventData.Payload.Count)
                System.Diagnostics.Debug.WriteLine("EventSourceMessage: " + eventData.Payload[0].ToString());

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

    internal sealed class TurnOnAllEventListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource) => EnableEvents(eventSource, EventLevel.LogAlways);
        protected override void OnEventWritten(EventWrittenEventArgs eventData) { }
    }
}
