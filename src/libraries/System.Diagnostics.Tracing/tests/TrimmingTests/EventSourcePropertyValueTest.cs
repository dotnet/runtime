// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;

/// <summary>
/// Tests that using EventSource with a reference type EventData works on a
/// trimmed application.
/// See System.Diagnostics.Tracing.PropertyValue.GetReferenceTypePropertyGetter.
/// </summary>
internal class Program
{
    [EventData]
    private class TestData
    {
        public int TestInt { get; set; }
        public TestSubData SubData { get; set; }
    }

    [EventData]
    private class TestSubData
    {
        public int SubInt { get; set; }
    }

    [EventSource(Name = EventSourceName)]
    private class TestEventSource : EventSource
    {
        public const string EventSourceName = "MyTest";
        public static TestEventSource Log = new TestEventSource();

        public TestEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat) { }

        [Event(1)]
        public void LogData(TestData data)
        {
            Write("LogData", data);
        }
    }

    private class TestEventListener : EventListener
    {
        public ReadOnlyCollection<object> LogDataPayload { get; set; }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == TestEventSource.EventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Verbose);
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "LogData")
            {
                LogDataPayload = eventData.Payload;
            }

            base.OnEventWritten(eventData);
        }
    }

    public static int Main()
    {
        using (var listener = new TestEventListener())
        {
            var testData = new TestData()
            {
                TestInt = 5,
                SubData = new TestSubData()
                {
                    SubInt = 6
                }
            };
            TestEventSource.Log.LogData(testData);

            if (listener.LogDataPayload?.Count == 2 &&
                (int)listener.LogDataPayload[0] == testData.TestInt &&
                (int)((IDictionary<string, object>)listener.LogDataPayload[1])["SubInt"] == testData.SubData.SubInt)
            {
                return 100;
            }

            return -1;
        }
    }
}
