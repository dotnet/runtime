// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
    internal sealed class ConsoleEventListener : EventListener
    {
        private readonly string _eventFilter;

        public ConsoleEventListener() : this(string.Empty) { }

        public ConsoleEventListener(string filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            _eventFilter = filter;

            foreach (EventSource source in EventSource.GetSources())
                EnableEvents(source, EventLevel.LogAlways);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (Console.Out)
            {
                string text = $"[{eventData.EventSource.Name}-{eventData.EventId}]{(eventData.Payload != null ? $" ({string.Join(", ", eventData.Payload)})." : "")}";
                if (_eventFilter != null && text.Contains(_eventFilter))
                {
                    ConsoleColor origForeground = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(text);
                    Console.ForegroundColor = origForeground;
                }
            }
        }
    }
}
