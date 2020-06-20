// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;

/// <summary>
/// Tests that the System.Diagnostics.DiagnosticSourceEventSource.TransformSpec.PropertyFetch.FetcherForProperty
/// method works as expected when used in a trimmed application.
/// </summary>
internal class Program
{
    public static int Main()
    {
        using var eventListener = new DiagnosticSourceEventListener();
        using var diagnosticListener = new DiagnosticListener("MySource");
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
            ";ActivityParentSpanId=*Activity.ParentSpanId" +
            ";ActivityRootId=*Activity.RootId" +
            ";ActivityTags=*Activity.Tags" +
            ";ActivityLinks=*Activity.Links" +
            ";ActivityIsAllDataRequested=*Activity.IsAllDataRequested" +
            ";ActivityKind=*Activity.Kind" +
            ";ActivityDisplayName=*Activity.DisplayName" +
            ";ActivitySource=*Activity.Source" +
            ";ActivityParent=*Activity.Parent" +
            ";ActivityId=*Activity.Id" +
            ";ActivityRecorded=*Activity.Recorded" +
            ";ActivityEvents=*Activity.Events" +
            ";ActivityBaggage=*Activity.Baggage" +
            ";ActivityContext=*Activity.Context" +
            ";ActivityActivityTraceFlags=*Activity.ActivityTraceFlags";
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
        return eventListener.EventCount == 1 ? 100 : -1;
    }

    /// <summary>
    /// A helper class that listens to Diagnostic sources and send events to the 'EventWritten' callback.
    /// </summary>
    internal class DiagnosticSourceEventListener : EventListener
    {
        public DiagnosticSourceEventListener()
        {
            EventWritten += UpdateLastEvent;
        }

        public int EventCount;
        public DiagnosticSourceEvent LastEvent;

        /// <summary>
        /// Will be called when a DiagnosticSource event is fired.
        /// </summary>
        public new event Action<DiagnosticSourceEvent> EventWritten;

        /// <summary>
        /// It is possible that there are other events besides those that are being forwarded from
        /// the DiagnosticSources. These come here.
        /// </summary>
        public event Action<EventWrittenEventArgs> OtherEventWritten;

        public void Enable(string filterAndPayloadSpecs, EventKeywords keywords = EventKeywords.All)
        {
            var args = new Dictionary<string, string>();
            if (filterAndPayloadSpecs != null)
                args.Add("FilterAndPayloadSpecs", filterAndPayloadSpecs);
            EnableEvents(_diagnosticSourceEventSource, EventLevel.Verbose, keywords, args);
        }

        /// <summary>
        /// Cleans this class up.  Among other things disables the DiagnosticSources being listened to.
        /// </summary>
        public override void Dispose()
        {
            if (_diagnosticSourceEventSource != null)
            {
                DisableEvents(_diagnosticSourceEventSource);
                _diagnosticSourceEventSource = null;
            }
        }

        #region private
        private void UpdateLastEvent(DiagnosticSourceEvent anEvent)
        {
            EventCount++;
            LastEvent = anEvent;
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            bool wroteEvent = false;
            var eventWritten = EventWritten;
            if (eventWritten != null)
            {
                if (eventData.Payload.Count == 3 && (eventData.EventName == "Event" || eventData.EventName.Contains("Activity")))
                {
                    Debug.Assert(eventData.PayloadNames[0] == "SourceName");
                    Debug.Assert(eventData.PayloadNames[1] == "EventName");
                    Debug.Assert(eventData.PayloadNames[2] == "Arguments");

                    var anEvent = new DiagnosticSourceEvent
                    {
                        SourceName = eventData.Payload[0].ToString(),
                        EventName = eventData.Payload[1].ToString(),
                        Arguments = new Dictionary<string, string>()
                    };

                    var asKeyValueList = eventData.Payload[2] as IEnumerable<object>;
                    if (asKeyValueList != null)
                    {
                        foreach (IDictionary<string, object> keyvalue in asKeyValueList)
                        {
                            keyvalue.TryGetValue("Key", out object key);
                            keyvalue.TryGetValue("Value", out object value);
                            if (key != null && value != null)
                                anEvent.Arguments[key.ToString()] = value.ToString();
                        }
                    }
                    eventWritten(anEvent);
                    wroteEvent = true;
                }
            }

            if (eventData.EventName == "EventSourceMessage" && 0 < eventData.Payload.Count)
                Debug.WriteLine("EventSourceMessage: " + eventData.Payload[0].ToString());

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

    /// <summary>
    /// Represents a single DiagnosticSource event.
    /// </summary>
    internal sealed class DiagnosticSourceEvent
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
}
