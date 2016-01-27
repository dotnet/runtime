// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;


namespace BasicEventSourceTests
{
    internal class LoudListener : EventListener
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

            Debug.Write(String.Format("Event {0} ", eventData.EventId));
            Debug.Write(String.Format(" (activity {0}{1}) ", eventData.ActivityId, eventData.RelatedActivityId != null ? "->" + eventData.RelatedActivityId : ""));
            Debug.WriteLine(String.Format(" ({0}).", eventData.Payload != null ? string.Join(", ", eventData.Payload) : ""));
        }
    }
}
