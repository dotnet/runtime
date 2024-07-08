// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(TestSubData))]
        public void LogData(TestData data)
        {
            Write("LogData", data);
        }
    }

    [EventSource(Name = EventSourceName)]
    private class PrimitiveOnlyEventSource : EventSource
    {
        public const string EventSourceName = "PrimitiveOnly";
        public readonly static PrimitiveOnlyEventSource Log = new PrimitiveOnlyEventSource();

        [Event(1)]
        public void LogBoolInt(bool b1, int i1)
        {
            WriteEvent(1, b1, i1);
        }

        [Event(2)]
        public void LogStringIntStringInt(string s1, int i1, string s2, int i2)
        {
            WriteEvent(2, s1, i1, s2, i2);
        }
    }

    private class TestEventListener : EventListener
    {
        public ReadOnlyCollection<object> LogDataPayload { get; set; }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            switch (eventSource.Name)
            {
                case TestEventSource.EventSourceName:
                case PrimitiveOnlyEventSource.EventSourceName:
                    EnableEvents(eventSource, EventLevel.Verbose);
                    break;
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            LogDataPayload = eventData.Payload;
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

            if (listener.LogDataPayload?.Count != 2 ||
                (int)listener.LogDataPayload[0] != testData.TestInt ||
                (int)((IDictionary<string, object>)listener.LogDataPayload[1])["SubInt"] != testData.SubData.SubInt)
            {
                return -1;
            }

            PrimitiveOnlyEventSource.Log.LogStringIntStringInt("a", 1, "b", 2);
            if (listener.LogDataPayload?.Count != 4 ||
                (string)listener.LogDataPayload[0] != "a" ||
                (int)listener.LogDataPayload[1] != 1 ||
                (string)listener.LogDataPayload[2] != "b" ||
                (int)listener.LogDataPayload[3] != 2)
            {
                return -2;
            }
        }
        return 100;
    }
}
