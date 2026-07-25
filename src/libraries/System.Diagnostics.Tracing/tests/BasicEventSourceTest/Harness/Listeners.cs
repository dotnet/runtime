// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading;
using Xunit;

namespace BasicEventSourceTests
{
    /// <summary>
    /// A listener can represent an out of process ETW listener (real time or not), an EventPipe listener, or an EventListener
    /// </summary>
    public abstract class Listener : IDisposable
    {
        public Action<Event> OnEvent;           // Called when you get events.
        public abstract void Dispose();

        /// <summary>
        /// Send a command to an eventSource. If this is called before Start(), the command will be queued. If called after Start()
        /// it will throw if !IsDynamicConfigChangeSupported
        /// </summary>
        public abstract void EventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options = null);

        /// <summary>
        /// Start listening for events
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// True if this listener supports dynamic config changes (i.e. EventSourceCommand) after Start() has been called.
        /// ETW and EventListener support this, EventPipe does not.
        /// </summary>
        public abstract bool IsDynamicConfigChangeSupported { get; }

        /// <summary>
        /// Does this listener support nullable types in event payloads for self-describing EventSources?
        /// Ideally all of them would but EventPipe NetTrace V5 format can't emit the metadata for a Boolean8 HasValue field that self-describing events use.
        /// </summary>
        public virtual bool IsSelfDescribingNullableSupported { get { return true; } }

        /// <summary>
        /// The TraceLogging serializer doesn't support null arguments in self-describing events.
        /// (Sigh, ideally all of them would behave the same way but EventListener does support this and for backwards compatibility we aren't going to change it)
        /// </summary>
        public virtual bool IsSelfDescribingNullArgSupported { get { return false; } }

        public virtual bool IsEventPipe { get { return false; } }

        public void EventSourceSynchronousEnable(EventSource eventSource, FilteringOptions options = null)
        {
            if (!IsDynamicConfigChangeSupported)
            {
                throw new InvalidOperationException("This listener does not support dynamic config changes");
            }
            EventSourceCommand(eventSource.Name, EventCommand.Enable, options);
            WaitForEventSourceStateChange(eventSource, true);
        }

        public void EventSourceSynchronousDisable(EventSource eventSource)
        {
            if (!IsDynamicConfigChangeSupported)
            {
                throw new InvalidOperationException("This listener does not support dynamic config changes");
            }
            EventSourceCommand(eventSource.Name, EventCommand.Disable);
            WaitForEventSourceStateChange(eventSource, false);
        }

        public void WaitForEventSourceStateChange(EventSource logger, bool targetState)
        {
            if (!SpinWait.SpinUntil(() => logger.IsEnabled() == targetState, TimeSpan.FromSeconds(10)))
            {
                throw new InvalidOperationException("EventSource not enabled after 10 seconds");
            }
        }

        internal void EnableTimer(EventSource eventSource, double pollingTime)
        {
            FilteringOptions options = new FilteringOptions();
            options.Args = new Dictionary<string, string>();
            options.Args.Add("EventCounterIntervalSec", pollingTime.ToString());
            EventSourceCommand(eventSource.Name, EventCommand.Enable, options);
        }
    }

    /// <summary>
    /// Used to control what options the harness sends to the EventSource when turning it on.   If not given
    /// it turns on all keywords, Verbose level, and no args.
    /// </summary>
    public class FilteringOptions
    {
        public FilteringOptions() { Keywords = EventKeywords.All; Level = EventLevel.Verbose; }
        public EventKeywords Keywords;
        public EventLevel Level;
        public IDictionary<string, string> Args;

        public override string ToString() => $"<Options Keywords='{(ulong)Keywords:x}' Level'{Level}' ArgsCount='{Args.Count}'";
    }

    /// <summary>
    /// Because events can be written to a EventListener as well as to ETW, we abstract what the result
    /// of an event coming out of the pipe.   Basically there are properties that fetch the name
    /// and the payload values, and we subclass this for the ETW case and for the EventListener case.
    /// </summary>
    public abstract class Event
    {
        public virtual bool IsEventListener { get { return false; } }
        // Note: Observationally I am seeing that EventListener events treat enum values differently in the WriteEvent vs. Write case but
        // I'm not sure whether that is the determining factor or its just correlated with other details of how the tests are emitting the events.
        public virtual bool IsEnumValueStronglyTyped(bool selfDescribing, bool writeEvent) => false;

        public virtual bool IsSizeAndPointerCoallescedIntoSingleArg => false;
        public abstract string ProviderName { get; }
        public abstract string EventName { get; }
        public abstract object PayloadValue(int propertyIndex, string propertyName);
        public abstract int PayloadCount { get; }
        public virtual string PayloadString(int propertyIndex, string propertyName)
        {
            var obj = PayloadValue(propertyIndex, propertyName);
            var asDict = obj as IDictionary<string, object>;
            if (asDict != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                bool first = true;
                foreach (var keyValue in asDict)
                {
                    if (!first)
                        sb.Append(",");
                    first = false;
                    var value = keyValue.Value;
                    sb.Append(keyValue.Key).Append(":").Append(value != null ? value.ToString() : "NULL");
                }
                sb.Append("}");
                return sb.ToString();
            }
            if (obj != null)
                return obj.ToString();
            return "";
        }
        public abstract IList<string> PayloadNames { get; }

        /// <summary>
        /// This is a convenience function for the debugger.   It is not used typically
        /// </summary>
        public List<object> PayloadValues
        {
            get
            {
                var ret = new List<object>();
                for (int i = 0; i < PayloadCount; i++)
                    ret.Add(PayloadValue(i, null));
                return ret;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ProviderName).Append('/').Append(EventName).Append('(');
            for (int i = 0; i < PayloadCount; i++)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(PayloadString(i, PayloadNames[i]));
            }
            sb.Append(')');
            return sb.ToString();
        }
    }

    public class EventListenerListener : Listener
    {
        private EventListener _listener;
        private Action<EventSource> _onEventSourceCreated;

        public override bool IsSelfDescribingNullArgSupported => true;

        public event EventHandler<EventSourceCreatedEventArgs> EventSourceCreated
        {
            add
            {
                if (this._listener != null)
                    this._listener.EventSourceCreated += value;
            }
            remove
            {
                if (this._listener != null)
                    this._listener.EventSourceCreated -= value;
            }
        }

        public event EventHandler<EventWrittenEventArgs> EventWritten
        {
            add
            {
                if (this._listener != null)
                    this._listener.EventWritten += value;
            }
            remove
            {
                if (this._listener != null)
                    this._listener.EventWritten -= value;
            }
        }

        public EventListenerListener(bool useEventsToListen = false)
        {
            _useEventsToListen = useEventsToListen;
            _pendingCommands = new List<(string eventSourceName, EventCommand command, FilteringOptions options)>();
        }

        public override void Start()
        {
            if (_useEventsToListen)
            {
                _listener = new HelperEventListener(null);
                _listener.EventSourceCreated += (sender, eventSourceCreatedEventArgs)
                    => _onEventSourceCreated?.Invoke(eventSourceCreatedEventArgs.EventSource);
                _listener.EventWritten += mListenerEventWritten;
            }
            else
            {
                _listener = new HelperEventListener(this);
            }
            foreach (var cmd in _pendingCommands)
            {
                ApplyEventSourceCommand(cmd.eventSourceName, cmd.command, cmd.options);
            }
        }

        public override bool IsDynamicConfigChangeSupported => true;

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventTestHarness.LogWriteLine("Disposing Listener");
            _listener.Dispose();
        }

        private void DoCommand(EventSource source, EventCommand command, FilteringOptions options)
        {
            if (command == EventCommand.Enable)
                _listener.EnableEvents(source, options.Level, options.Keywords, options.Args);
            else if (command == EventCommand.Disable)
                _listener.DisableEvents(source);
            else
                throw new NotImplementedException();
        }

        public override void EventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options = null)
        {
            if (_listener == null)
            {
                _pendingCommands.Add((eventSourceName, command, options));
            }
            else
            {
                ApplyEventSourceCommand(eventSourceName, command, options);
            }
        }

        private void ApplyEventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options = null)
        {
            EventTestHarness.LogWriteLine("Sending command {0} to EventSource {1} Options {2}", eventSourceName, command, options);

            if (options == null)
                options = new FilteringOptions();

            foreach (EventSource source in EventSource.GetSources())
            {
                if (source.Name == eventSourceName)
                {
                    DoCommand(source, command, options);
                    return;
                }
            }

            _onEventSourceCreated += delegate (EventSource sourceBeingCreated)
            {
                if (eventSourceName != null && eventSourceName == sourceBeingCreated.Name)
                {
                    DoCommand(sourceBeingCreated, command, options);
                    eventSourceName = null;         // so we only do it once.
                }
            };
        }

        public override string ToString() => $"EventListener(UseEventsToListen={_useEventsToListen})";

        private void mListenerEventWritten(object sender, EventWrittenEventArgs eventData)
        {
            OnEvent?.Invoke(new EventListenerEvent(eventData));
        }

        private class HelperEventListener : EventListener
        {
            private readonly EventListenerListener _forwardTo;

            public HelperEventListener(EventListenerListener forwardTo)
            {
                _forwardTo = forwardTo;
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                base.OnEventSourceCreated(eventSource);

                _forwardTo?._onEventSourceCreated?.Invoke(eventSource);
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                // OnEventWritten is abstract in .NET Framework <= 461
                base.OnEventWritten(eventData);
                _forwardTo?.OnEvent?.Invoke(new EventListenerEvent(eventData));
            }
        }

        /// <summary>
        /// EtwEvent implements the 'Event' abstraction for TraceListene events (it has a EventWrittenEventArgs in it)
        /// </summary>
        internal class EventListenerEvent : Event
        {
            internal EventWrittenEventArgs Data { get; }

            internal EventListenerEvent(EventWrittenEventArgs data) => Data = data;

            public override bool IsEventListener { get { return true; } }
            public override bool IsEnumValueStronglyTyped(bool selfDescribing, bool isWriteEvent) => !isWriteEvent;

            public override string ProviderName { get { return Data.EventSource.Name; } }

            public override string EventName { get { return Data.EventName; } }

            public override IList<string> PayloadNames { get { return Data.PayloadNames; } }

            public override int PayloadCount
            {
                get { return Data.Payload?.Count ?? 0; }
            }

            public override object PayloadValue(int propertyIndex, string propertyName)
            {
                if (propertyName != null)
                    Assert.Equal(propertyName, Data.PayloadNames[propertyIndex]);

                return Data.Payload[propertyIndex];
            }
        }

        private List<(string eventSourceName, EventCommand command, FilteringOptions options)> _pendingCommands;
        private bool _useEventsToListen = false;
        private bool _disposed;
    }
}
