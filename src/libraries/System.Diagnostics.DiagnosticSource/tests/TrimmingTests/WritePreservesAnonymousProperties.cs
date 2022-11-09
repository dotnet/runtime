// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;

/// <summary>
/// Tests that writing an anonymous type to a DiagnosticSource preserves the anonymous type's properties
/// correctly, so they are written to the EventSource correctly.
/// </summary>
internal class Program
{
    private class TestEventListener : EventListener
    {
        public ReadOnlyCollection<object> LogDataPayload { get; set; }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Diagnostics-DiagnosticSource")
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, new Dictionary<string, string>
                {
                    { "FilterAndPayloadSpecs", "TestDiagnosticListener/Test.Start@Activity2Start:-Id;Name"}
                });
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "Activity2Start")
            {
                LogDataPayload = eventData.Payload;
            }

            base.OnEventWritten(eventData);
        }
    }

    public static int Main()
    {
        DiagnosticSource diagnosticSource = new DiagnosticListener("TestDiagnosticListener");
        using (var listener = new TestEventListener())
        {
            var data = new 
            {
                Id = Guid.NewGuid(),
                Name = "EventName"
            };

            diagnosticSource.Write("Test.Start", data);

            if (!(listener.LogDataPayload?.Count == 3 &&
                (string)listener.LogDataPayload[0] == "TestDiagnosticListener" &&
                (string)listener.LogDataPayload[1] == "Test.Start"))
            {
                return -1;
            }

            object[] args = (object[])listener.LogDataPayload[2];
            if (args.Length != 2)
            {
                return -2;
            }

            IDictionary<string, object> arg = (IDictionary<string, object>)args[0];
            if (!((string)arg["Key"] == "Id" && (string)arg["Value"] == data.Id.ToString()))
            {
                return -3;
            }

            arg = (IDictionary<string, object>)args[1];
            if (!((string)arg["Key"] == "Name" && (string)arg["Value"] == "EventName"))
            {
                return -4;
            }

            return 100;
        }
    }
}
