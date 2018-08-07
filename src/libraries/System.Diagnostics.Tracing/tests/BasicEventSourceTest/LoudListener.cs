// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

namespace BasicEventSourceTests
{
    internal class LoudListener : EventListener
    {
        [ThreadStatic]
        public static EventWrittenEventArgs t_lastEvent;

        public LoudListener(EventSource eventSource)
        {
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0xffffffff);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            t_lastEvent = eventData;

            Debug.Write(string.Format("Event {0} ", eventData.EventId));
            Debug.Write(string.Format(" (activity {0}{1}) ", eventData.ActivityId, eventData.RelatedActivityId != Guid.Empty ? "->" + eventData.RelatedActivityId : ""));
            Debug.WriteLine(string.Format(" ({0}).", eventData.Payload != null ? string.Join(", ", eventData.Payload) : ""));
        }

        public static EventWrittenEventArgs LastEvent
        {
            get { return t_lastEvent; }
        }
    }
}
