using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;


namespace BasicEventSourceTests
{
    class LoudListener : EventListener
    {
        [ThreadStatic]
        public static EventWrittenEventArgs LastEvent;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0xffffffff);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            LastEvent = eventData;

            Console.Write("Event {0} ", eventData.EventId);
            // TODO enable when implicit activities are implemented.
            //Console.Write(" (activity {0}{1}) ", eventData.ActivityId, eventData.RelatedActivityId != null ? "->" + eventData.RelatedActivityId : "");
            Console.WriteLine(" ({0}).", eventData.Payload != null ? string.Join(", ", eventData.Payload) : "");
        }
    }
}
