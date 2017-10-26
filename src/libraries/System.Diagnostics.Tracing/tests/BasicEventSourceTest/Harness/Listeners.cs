// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if USE_ETW // TODO: Enable when TraceEvent is available on CoreCLR. GitHub issue #4864.
using Microsoft.Diagnostics.Tracing.Session;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BasicEventSourceTests
{
    /// <summary>
    /// A listener can represent an out of process ETW listener (real time or not) or an EventListener
    /// </summary>
    public abstract class Listener : IDisposable
    {
        public Action<Event> OnEvent;           // Called when you get events.  
        public abstract void Dispose();
        /// <summary>
        /// Send a command to an eventSource.   Be careful this is async.  You may wish to do a WaitForEnable 
        /// </summary>
        public abstract void EventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options = null);

        public void EventSourceSynchronousEnable(EventSource eventSource, FilteringOptions options = null)
        {
            EventSourceCommand(eventSource.Name, EventCommand.Enable, options);
            WaitForEnable(eventSource);
        }
        public void WaitForEnable(EventSource logger)
        {
            if (!SpinWait.SpinUntil(() => logger.IsEnabled(), TimeSpan.FromSeconds(10)))
            {
                throw new InvalidOperationException("EventSource not enabled after 5 seconds");
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

        public override string ToString()
        {
            return string.Format("<Options Keywords='{0}' Level'{1}' ArgsCount='{2}'",
                ((ulong)Keywords).ToString("x"), Level, Args.Count);
        }
    }

    /// <summary>
    /// Because events can be written to a EventListener as well as to ETW, we abstract what the result
    /// of an event coming out of the pipe.   Basically there are properties that fetch the name
    /// and the payload values, and we subclass this for the ETW case and for the EventListener case. 
    /// </summary>
    public abstract class Event
    {
        public virtual bool IsEtw { get { return false; } }
        public virtual bool IsEventListener { get { return false; } }
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
                foreach (var key in asDict.Keys)
                {
                    if (!first)
                        sb.Append(",");
                    first = false;
                    var value = asDict[key];
                    sb.Append(key).Append(":").Append(value != null ? value.ToString() : "NULL");
                }
                sb.Append("}");
                return sb.ToString();
            }
            if (obj != null)
                return obj.ToString();
            return "";
        }
        public abstract IList<string> PayloadNames { get; }

#if DEBUG
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
#endif

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ProviderName).Append('/').Append(EventName).Append('(');
            for (int i = 0; i < PayloadCount; i++)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(PayloadString(i, null));
            }
            sb.Append(')');
            return sb.ToString();
        }
    }

#if USE_ETW // TODO: Enable when TraceEvent is available on CoreCLR. GitHub issue #4864.
    /**************************************************************************/
    /* Concrete implementation of the Listener abstraction */

    /// <summary>
    /// Implementation of the Listener abstraction for ETW.  
    /// </summary>
    public class EtwListener : Listener
    {
        internal static void EnsureStopped()
        {
            using (var session = new TraceEventSession("EventSourceTestSession", "EventSourceTestData.etl"))
                session.Stop();
        }

        public EtwListener(string dataFileName = "EventSourceTestData.etl", string sessionName = "EventSourceTestSession")
        {
            _dataFileName = dataFileName;

            // Today you have to be Admin to turn on ETW events (anyone can write ETW events).   
            if (TraceEventSession.IsElevated() != true)
            {
                throw new ApplicationException("Need to be elevated to run. ");
            }

            if (dataFileName == null)
            {
                Debug.WriteLine("Creating a real time session " + sessionName);

                Task.Factory.StartNew(delegate ()
                {
                    var session = new TraceEventSession(sessionName, dataFileName);
                    session.Source.AllEvents += OnEventHelper;
                    Debug.WriteLine("Listening for real time events");
                    _session = session;    // Indicate that we are alive.  
                    _session.Source.Process();
                    Debug.WriteLine("Real time listening stopping.");
                });

                SpinWait.SpinUntil(() => _session != null); // Wait for real time thread to wake up. 
            }
            else
            {
                // Normalize to a full path name.  
                dataFileName = Path.GetFullPath(dataFileName);
                Debug.WriteLine("Creating ETW data file " + Path.GetFullPath(dataFileName));
                _session = new TraceEventSession(sessionName, dataFileName);
            }
        }

        public override void EventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options = null)
        {
            if (command == EventCommand.Enable)
            {
                if (options == null)
                    options = new FilteringOptions();

                _session.EnableProvider(eventSourceName, (TraceEventLevel)options.Level, (ulong)options.Keywords,
                    new TraceEventProviderOptions() { Arguments = options.Args });
            }
            else if (command == EventCommand.Disable)
            {
                _session.DisableProvider(TraceEventProviders.GetEventSourceGuidFromName(eventSourceName));
            }
            else
                throw new NotImplementedException();
            Thread.Sleep(200);          // Calls are async, give them time to work.  
        }

        public override void Dispose()
        {
            _session.Flush();
            Thread.Sleep(1010);      // Let it drain.
            _session.Dispose();     // This also will kill the real time thread 

            if (_dataFileName != null)
            {
                using (var traceEventSource = new ETWTraceEventSource(_dataFileName))
                {
                    Debug.WriteLine("Processing data file " + Path.GetFullPath(_dataFileName));

                    // Parse all the events as best we can, and also send unhandled events there as well.  
                    traceEventSource.Registered.All += OnEventHelper;
                    traceEventSource.Dynamic.All += OnEventHelper;
                    traceEventSource.UnhandledEvents += OnEventHelper;
                    // Process all the events in the file.  
                    traceEventSource.Process();
                    Debug.WriteLine("Done processing data file " + Path.GetFullPath(_dataFileName));
                }
            }
        }

    #region private
        private void OnEventHelper(TraceEvent data)
        {
            // Ignore manifest events. 
            if ((int)data.ID == 0xFFFE)
                return;
            this.OnEvent(new EtwEvent(data));
        }

        /// <summary>
        /// EtwEvent implements the 'Event' abstraction for ETW events (it has a TraceEvent in it) 
        /// </summary>
        internal class EtwEvent : Event
        {
            public override bool IsEtw { get { return true; } }
            public override string ProviderName { get { return _data.ProviderName; } }
            public override string EventName { get { return _data.EventName; } }
            public override object PayloadValue(int propertyIndex, string propertyName)
            {
                if (propertyName != null)
                    Assert.Equal(propertyName, _data.PayloadNames[propertyIndex]);
                return _data.PayloadValue(propertyIndex);
            }
            public override string PayloadString(int propertyIndex, string propertyName)
            {
                Assert.Equal(propertyName, _data.PayloadNames[propertyIndex]);
                return _data.PayloadString(propertyIndex);
            }
            public override int PayloadCount { get { return _data.PayloadNames.Length; } }
            public override IList<string> PayloadNames { get { return _data.PayloadNames; } }

    #region private
            internal EtwEvent(TraceEvent data) { _data = data.Clone(); }

            private TraceEvent _data;
    #endregion
        }

        private string _dataFileName;
        private volatile TraceEventSession _session;
    #endregion

    }
#endif //USE_ETW

    public class EventListenerListener : Listener
    {
        private EventListener _listener;
        private Action<EventSource> _onEventSourceCreated;

#if FEATURE_ETLEVENTS
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
#endif

        public EventListenerListener(bool useEventsToListen = false)
        {
#if FEATURE_ETLEVENTS
            if (useEventsToListen)
            {
                _listener = new HelperEventListener(null);
                _listener.EventSourceCreated += (sender, eventSourceCreatedEventArgs)
                    => _onEventSourceCreated?.Invoke(eventSourceCreatedEventArgs.EventSource);
                _listener.EventWritten += mListenerEventWritten;
            }
            else
#endif
            {
                _listener = new HelperEventListener(this);
            }
        }

        public override void Dispose()
        {
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

        private void mListenerEventWritten(object sender, EventWrittenEventArgs eventData)
        {
            OnEvent(new EventListenerEvent(eventData));
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
#if FEATURE_ETLEVENTS
                // OnEventWritten is abstract in netfx <= 461
                base.OnEventWritten(eventData);
#endif
                _forwardTo?.OnEvent?.Invoke(new EventListenerEvent(eventData));
            }
        }

        /// <summary>
        /// EtwEvent implements the 'Event' abstraction for TraceListene events (it has a EventWrittenEventArgs in it) 
        /// </summary>
        internal class EventListenerEvent : Event
        {
            private readonly EventWrittenEventArgs _data;

            public override bool IsEventListener { get { return true; } }

            public override string ProviderName { get { return _data.EventSource.Name; } }

            public override string EventName { get { return _data.EventName; } }

            public override IList<string> PayloadNames { get { return _data.PayloadNames; } }

            public override int PayloadCount
            {
                get { return _data.Payload?.Count ?? 0; }
            }

            internal EventListenerEvent(EventWrittenEventArgs data)
            {
                _data = data;
            }

            public override object PayloadValue(int propertyIndex, string propertyName)
            {
                if (propertyName != null)
                    Assert.Equal(propertyName, _data.PayloadNames[propertyIndex]);

                return _data.Payload[propertyIndex];
            }
        }
    }
}
