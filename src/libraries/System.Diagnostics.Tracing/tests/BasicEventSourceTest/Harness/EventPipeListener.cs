// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Xunit;

namespace BasicEventSourceTests
{
    /// <summary>
    /// Implementation of Listener for EventPipe (in-process collection using DiagnosticsClient + EventPipeEventSource).
    /// </summary>
    internal sealed class EventPipeListener : Listener
    {
        private readonly List<(string eventSourceName, EventCommand command, FilteringOptions options)> _pendingCommands = new();
        private readonly Dictionary<string, FilteringOptions> _enabled = new(StringComparer.Ordinal);
        private EventPipeSession _session;
        private Task _processingTask;
        private bool _disposed;

        public override bool IsDynamicConfigChangeSupported => false;

        /// <summary>
        /// EventPipe NetTrace V5 format can't emit the metadata for a Boolean8 HasValue field that self-describing events use.
        /// </summary>
        public override bool IsSelfDescribingNullableSupported => false;

        public override bool IsEventPipe => true;

        public EventPipeListener() { }

        public override void EventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options = null)
        {
            if (eventSourceName is null)
            {
                throw new ArgumentNullException(nameof(eventSourceName));
            }

            if (_session != null)
            {
                throw new InvalidOperationException("EventPipeEventListener does not support dynamic configuration changes after Start().");
            }
            _pendingCommands.Add((eventSourceName, command, options));
        }

        public override void Start()
        {
            if (_session != null)
            {
                return; // already started
            }

            // Build provider enable list from pending commands
            foreach (var (eventSourceName, command, options) in _pendingCommands)
            {
                if (command == EventCommand.Enable)
                {
                    var effective = options ?? new FilteringOptions();
                    _enabled[eventSourceName] = effective;
                }
                else if (command == EventCommand.Disable)
                {
                    _enabled.Remove(eventSourceName);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var providers = new List<EventPipeProvider>();
            foreach (var kvp in _enabled)
            {
                var opt = kvp.Value;
                providers.Add(new EventPipeProvider(kvp.Key, (EventLevel)opt.Level, (long)opt.Keywords, opt.Args));
            }

            var client = new DiagnosticsClient(Environment.ProcessId);
            _session = client.StartEventPipeSession(providers, false);

            _processingTask = Task.Factory.StartNew(() => ProcessEvents(_session), TaskCreationOptions.LongRunning);
        }

        private void ProcessEvents(EventPipeSession session)
        {
            using var source = new EventPipeEventSource(session.EventStream);
            source.Dynamic.All += traceEvent =>
            {
                // EventPipe adds extra events we didn't ask for, ignore them.
                if (traceEvent.ProviderName == "Microsoft-DotNETCore-EventPipe")
                {
                    return;
                }

                OnEvent?.Invoke(new EventPipeEvent(traceEvent));
            };
            source.Process();
        }

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _disposed = true;
                _session?.Stop();

                if (_processingTask != null && !_processingTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    // If the session is still streaming data then session.Dispose() below will disconnect the stream 
                    // and likely cause the thread running source.Process() to throw.
                    Assert.Fail("EventPipeEventListener processing task failed to complete in 5 seconds.");
                }
            }
            finally
            {
                _session?.Dispose();
            }
        }

        public override string ToString() => "EventPipeListener";

        /// <summary>
        /// Wrapper mapping TraceEvent (EventPipe) to harness Event abstraction.
        /// </summary>
        private sealed class EventPipeEvent : Event
        {
            private readonly TraceEvent _data;
            private readonly IList<string> _payloadNames;
            private readonly IList<object> _payloadValues;

            public EventPipeEvent(TraceEvent data)
            {
                _data = data;
                // EventPipe has a discrepancy with ETW for self-describing events - it exposes a single top-level object whereas ETW considers each of the fields within
                // that object as top-level named fields. To workaround that we unwrap any top-level object at payload index 0.
                if(data.PayloadNames.Length > 0 && data.PayloadValue(0) is IDictionary<string,object> d)
                {
                    _payloadNames = d.Select(kv => kv.Key).ToList();
                    _payloadValues = d.Select(kv => kv.Value).ToList();
                }
                else
                {
                    _payloadNames = data.PayloadNames;
                    _payloadValues = new List<object>();
                    for(int i = 0; i < _payloadNames.Count; i++)
                    {
                        _payloadValues.Add(data.PayloadValue(i));
                    }
                }
            }

            public override string ProviderName => _data.ProviderName;
            public override string EventName => _data.EventName;
            public override int PayloadCount => _payloadNames.Count;
            public override IList<string> PayloadNames => _payloadNames;

            public override object PayloadValue(int propertyIndex, string propertyName)
            {
                if (propertyName != null)
                {
                    Assert.Equal(propertyName, _payloadNames[propertyIndex]);
                }
                return _payloadValues[propertyIndex];
            }
        }
    }
}
