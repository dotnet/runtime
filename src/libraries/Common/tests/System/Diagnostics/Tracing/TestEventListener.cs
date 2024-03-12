// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing
{
    /// <summary>Simple event listener than invokes a callback for each event received.</summary>
    internal sealed class TestEventListener : EventListener
    {
        private class Settings
        {
            public EventLevel Level;
            public EventKeywords Keywords;
        }

        private readonly Dictionary<string, Settings> _names = new Dictionary<string, Settings>();
        private readonly Dictionary<Guid, Settings> _guids = new Dictionary<Guid, Settings>();

        private readonly double? _eventCounterInterval;

        private Action<EventWrittenEventArgs> _eventWritten;
        private readonly List<EventSource> _eventSourceList = new List<EventSource>();

        public TestEventListener(double? eventCounterInterval = null)
        {
            _eventCounterInterval = eventCounterInterval;
        }

        public TestEventListener(string targetSourceName, EventLevel level, double? eventCounterInterval = null)
            : this(eventCounterInterval)
        {
            AddSource(targetSourceName, level);
        }

        public TestEventListener(Guid targetSourceGuid, EventLevel level, double? eventCounterInterval = null)
            : this(eventCounterInterval)
        {
            AddSource(targetSourceGuid, level);
        }

        public void AddSource(string name, EventLevel level, EventKeywords keywords = EventKeywords.All) =>
            AddSource(name, null, level, keywords);

        public void AddSource(Guid guid, EventLevel level, EventKeywords keywords = EventKeywords.All) =>
            AddSource(null, guid, level, keywords);

        private void AddSource(string name, Guid? guid, EventLevel level, EventKeywords keywords)
        {
            EventSource sourceToEnable = null;
            lock (_eventSourceList)
            {
                var settings = new Settings()
                {
                    Level = level,
                    Keywords = keywords
                };

                if (name is not null)
                    _names.Add(name, settings);

                if (guid.HasValue)
                    _guids.Add(guid.Value, settings);

                foreach (EventSource source in _eventSourceList)
                {
                    if (name == source.Name || guid == source.Guid)
                    {
                        sourceToEnable = source;
                        break;
                    }
                }
            }

            if (sourceToEnable != null)
            {
                EnableEventSource(sourceToEnable, level, keywords);
            }
        }

        public void AddActivityTracking() =>
            AddSource("System.Threading.Tasks.TplEventSource", EventLevel.Informational, (EventKeywords)0x80 /* TasksFlowActivityIds */);

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            bool shouldEnable = false;
            Settings settings;
            lock (_eventSourceList)
            {
                _eventSourceList.Add(eventSource);
                shouldEnable = _names.TryGetValue(eventSource.Name, out settings) || _guids.TryGetValue(eventSource.Guid, out settings);
            }

            if (shouldEnable)
            {
                EnableEventSource(eventSource, settings.Level, settings.Keywords);
            }
        }

        private void EnableEventSource(EventSource source, EventLevel level, EventKeywords keywords)
        {
            var args = new Dictionary<string, string>();

            if (_eventCounterInterval != null)
            {
                args.Add("EventCounterIntervalSec", _eventCounterInterval.ToString());
            }

            EnableEvents(source, level, keywords, args);
        }

        public void RunWithCallback(Action<EventWrittenEventArgs> handler, Action body)
        {
            _eventWritten = handler;
            try { body(); }
            finally { _eventWritten = null; }
        }

        public async Task RunWithCallbackAsync(Action<EventWrittenEventArgs> handler, Func<Task> body)
        {
            _eventWritten = handler;
            try { await body().ConfigureAwait(false); }
            finally { _eventWritten = null; }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            _eventWritten?.Invoke(eventData);
        }
    }

}
