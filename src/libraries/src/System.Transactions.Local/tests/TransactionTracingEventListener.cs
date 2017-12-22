// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace System.Transactions.Tests
{
    public class TransactionsTracingEventListener : EventListener
    {
        private List<EventWrittenEventArgs> _recordedEvents = new List<EventWrittenEventArgs>();

        public List<EventWrittenEventArgs> RecordedEvents { get { return _recordedEvents; } }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            _recordedEvents.Add(eventData);
        }
    }

    public sealed class TestEventListener : EventListener
    {
        private readonly string _targetSourceName;
        private readonly Guid _targetSourceGuid;
        private readonly EventLevel _level;

        private Action<EventWrittenEventArgs> _eventWritten;
        private List<EventSource> _tmpEventSourceList = new List<EventSource>();

        public TestEventListener(string targetSourceName, EventLevel level)
        {
            // Store the arguments
            _targetSourceName = targetSourceName;
            _level = level;

            LoadSourceList();
        }

        public TestEventListener(Guid targetSourceGuid, EventLevel level)
        {
            // Store the arguments
            _targetSourceGuid = targetSourceGuid;
            _level = level;

            LoadSourceList();
        }

        private void LoadSourceList()
        {
            // The base constructor, which is called before this constructor,
            // will invoke the virtual OnEventSourceCreated method for each
            // existing EventSource, which means OnEventSourceCreated will be
            // called before _targetSourceGuid and _level have been set.  As such,
            // we store a temporary list that just exists from the moment this instance
            // is created (instance field initializers run before the base constructor)
            // and until we finish construction... in that window, OnEventSourceCreated
            // will store the sources into the list rather than try to enable them directly,
            // and then here we can enumerate that list, then clear it out.
            List<EventSource> sources;
            lock (_tmpEventSourceList)
            {
                sources = _tmpEventSourceList;
                _tmpEventSourceList = null;
            }
            foreach (EventSource source in sources)
            {
                EnableSourceIfMatch(source);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            List<EventSource> tmp = _tmpEventSourceList;
            if (tmp != null)
            {
                lock (tmp)
                {
                    if (_tmpEventSourceList != null)
                    {
                        _tmpEventSourceList.Add(eventSource);
                        return;
                    }
                }
            }

            EnableSourceIfMatch(eventSource);
        }

        private void EnableSourceIfMatch(EventSource source)
        {
            if (source.Name.Equals(_targetSourceName) ||
                source.Guid.Equals(_targetSourceGuid))
            {
                EnableEvents(source, _level);
            }
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

