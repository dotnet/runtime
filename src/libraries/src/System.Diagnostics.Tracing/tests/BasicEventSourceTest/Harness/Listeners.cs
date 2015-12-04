using System.Diagnostics.Tracing;
#if USE_ETW
using Microsoft.Diagnostics.Tracing.Session;
#endif
using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            int i = 0;
            while (!logger.IsEnabled())     // This is mostly for the debugger case.  The command above is async
            {
                Thread.Sleep(100);
                i++;
                if (50 <= i)
                {
                    throw new InvalidOperationException("EventSource not enabled after 5 seconds");
                }
            }
        }

        internal void EnableTimer(EventSource eventSource, int pollingTime)
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
    /// Because events can to to a EventListener as well as to ETW, we abstract what the result
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
            return PayloadValue(propertyIndex, propertyName).ToString();
        }
        public abstract IEnumerable<string> PayloadNames { get; }

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

#if USE_ETW
    /**************************************************************************/
    /* Concrete implementation of the Listener abstraction */

    /// <summary>
    /// Implementation of the Listener abstraction for ETW.  
    /// </summary>
    public class EtwListener : Listener
    {
        internal static void InsureStopped()
        {
            using (var session = new TraceEventSession("EventSourceTestSession", "EventSourceTestData.etl"))
                session.Stop();
        }

        public EtwListener(string dataFileName = "EventSourceTestData.etl", string sessionName = "EventSourceTestSession")
        {
            m_dataFileName = dataFileName;

            // Today you have to be Admin to turn on ETW events (anyone can write ETW events).   
            if (TraceEventSession.IsElevated() != true)
            {
                Assert.Inconclusive("Need to be admin elevated to run this test.");
                throw new ApplicationException("Need to be elevated to run. ");
            }

            if (dataFileName == null)
            {
                Console.WriteLine("Creating a real time session " + sessionName);

                Task.Factory.StartNew(delegate()
                {
                    var session = new TraceEventSession(sessionName, dataFileName);
                    session.Source.AllEvents += OnEventHelper;
                    Console.WriteLine("Listening for real time events");
                    m_session = session;    // Indicate that we are alive.  
                    m_session.Source.Process();
                    Console.WriteLine("Real time listening stopping.");
                });

                while (m_session != null)   // Wait for real time thread to wake up. 
                    Thread.Sleep(1);
            }
            else
            {
                // Normalize to a full path name.  
                dataFileName = Path.GetFullPath(dataFileName);
                Console.WriteLine("Creating ETW data file " + Path.GetFullPath(dataFileName));
                m_session = new TraceEventSession(sessionName, dataFileName);
            }
        }
        public override void EventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options=null)
        {
            if (command == EventCommand.Enable)
            {
                if (options == null)
                    options = new FilteringOptions();

                m_session.EnableProvider(eventSourceName, (TraceEventLevel)options.Level, (ulong)options.Keywords,
                    new TraceEventProviderOptions() { Arguments = options.Args });
            }
            else if (command == EventCommand.Disable)
            {
                m_session.DisableProvider(TraceEventProviders.GetEventSourceGuidFromName(eventSourceName));
            }
            else
                throw new NotImplementedException();
            Thread.Sleep(200);          // Calls are async, give them time to work.  
        }
        public override void Dispose()
        {
            m_session.Flush();
            Thread.Sleep(1010);      // Let it drain.  TODO not clear this is needed.  
            m_session.Dispose();     // This also will kill the real time thread 

            if (m_dataFileName != null)
            {
                using (var traceEventSource = new ETWTraceEventSource(m_dataFileName))
                {
                    Console.WriteLine("Processing data file " + Path.GetFullPath(m_dataFileName));

                    // Parse all the events as as best we can, and also send unhandled events there as well.  
                    traceEventSource.Registered.All += OnEventHelper;
                    traceEventSource.Dynamic.All += OnEventHelper;
                    traceEventSource.UnhandledEvents += OnEventHelper;
                    // Process all the events in the file.  
                    traceEventSource.Process();
                    Console.WriteLine("Done processing data file " + Path.GetFullPath(m_dataFileName));
                }
            }
        }

#region private
        void OnEventHelper(TraceEvent data)
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
            public override string ProviderName { get { return m_data.ProviderName; } }
            public override string EventName { get { return m_data.EventName; } }
            public override object PayloadValue(int propertyIndex, string propertyName)
            {
                if (propertyName != null)
                    Assert.Equal(propertyName, m_data.PayloadNames[propertyIndex]);
                return m_data.PayloadValue(propertyIndex);
            }
            public override string PayloadString(int propertyIndex, string propertyName)
            {
                Assert.Equal(propertyName, m_data.PayloadNames[propertyIndex]);
                return m_data.PayloadString(propertyIndex);
            }
            public override int PayloadCount { get { return m_data.PayloadNames.Length; } }
            public override IEnumerable<string> PayloadNames { get { return m_data.PayloadNames; }}

#region private
            internal EtwEvent(TraceEvent data) { m_data = data.Clone(); }

            TraceEvent m_data;
#endregion
        }

        string m_dataFileName;
        volatile TraceEventSession m_session;
#endregion

    }
#endif //USE_ETW

    public class EventListenerListener : Listener
    {
#if false // TODO: Enable when we ship the events
        public event EventHandler<EventSourceCreatedEventArgs> EventSourceCreated
        {
            add
            {
                if(this.m_listener != null)
                {
                    this.m_listener.EventSourceCreated += value;
                }
            }
            remove
            {
                if (this.m_listener != null)
                {
                    this.m_listener.EventSourceCreated -= value;
                }
            }
        }
        
        public event EventHandler<EventWrittenEventArgs> EventWritten
        {
            add
            {
                if (this.m_listener != null)
                {
                    this.m_listener.EventWritten += value;
                }
            }
            remove
            {
                if (this.m_listener != null)
                {
                    this.m_listener.EventWritten -= value;
                }
            }
        }
#endif // false
        public EventListenerListener(bool useEventsToListen = false)
        {
#if false // TODO: enable when we ship the events
            if (useEventsToListen)
            {
                m_listener = new HelperEventListener(null);
                m_listener.EventSourceCreated += mListenerEventSourceCreated;
                m_listener.EventWritten += mListenerEventWritten;
            }
            else
#else // false
                Console.WriteLine("EventListener events are not supported yet.");
#endif // false
            {
                m_listener = new HelperEventListener(this);
            }
        }

        private void mListenerEventWritten(object sender, EventWrittenEventArgs eventData)
        {
            OnEvent(new EventListenerEvent(eventData));
        }

#if false // TODO: enable when we ship the events
        private void mListenerEventSourceCreated(object sender, EventSourceCreatedEventArgs eventSource)
        {
            if(m_OnEventSourceCreated != null)
            {
                m_OnEventSourceCreated(eventSource.EventSource);
            }
        }
#endif // false

        public override void EventSourceCommand(string eventSourceName, EventCommand command, FilteringOptions options=null)
        {
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
            m_OnEventSourceCreated += delegate(EventSource sourceBeingCreated)
            {
                if (eventSourceName != null && eventSourceName == sourceBeingCreated.Name)
                {
                    DoCommand(sourceBeingCreated, command, options);
                    eventSourceName = null;         // so we only do it once.  
                }
            };
        }

        public override void Dispose()
        {
            m_listener.Dispose();
        }

#region private
        private void DoCommand(EventSource source, EventCommand command, FilteringOptions options)
        {
            if (command == EventCommand.Enable)
                m_listener.EnableEvents(source, options.Level, options.Keywords, options.Args);
            else if (command == EventCommand.Disable)
                m_listener.DisableEvents(source);
            else
                throw new NotImplementedException();
        }

        class HelperEventListener : EventListener
        {
            public HelperEventListener(EventListenerListener forwardTo) { m_forwardTo = forwardTo; }
            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
#if false // TODO: EventListener events are not enabled in coreclr.
                base.OnEventWritten(eventData);
#endif // false

                if (m_forwardTo != null && m_forwardTo.OnEvent != null)
                {
                    m_forwardTo.OnEvent(new EventListenerEvent(eventData));
                }
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                base.OnEventSourceCreated(eventSource);

                if (m_forwardTo != null && m_forwardTo.m_OnEventSourceCreated != null)
                    m_forwardTo.m_OnEventSourceCreated(eventSource);
            }

            EventListenerListener m_forwardTo;
        }

        /// <summary>
        /// EtwEvent implements the 'Event' abstraction for TraceListene events (it has a EventWrittenEventArgs in it) 
        /// </summary>
        internal class EventListenerEvent : Event
        {
            public override bool IsEventListener { get { return true; } }
            public override string ProviderName { get { return m_data.EventSource.Name; } }
            public override string EventName { get { return m_data.EventName; } }
            public override object PayloadValue(int propertyIndex, string propertyName)
            {
                if (propertyName != null)
                    Assert.Equal(propertyName, m_data.PayloadNames[propertyIndex]);
                return m_data.Payload[propertyIndex];
            }
            public override int PayloadCount
            {
                get
                {
                    if (m_data.Payload == null)
                        return 0;
                    return m_data.Payload.Count;
                }
            }
            public override IEnumerable<string> PayloadNames { get { return m_data.PayloadNames; }}


#region private
            internal EventListenerEvent(EventWrittenEventArgs data) { m_data = data; }
            EventWrittenEventArgs m_data;
#endregion
        }

        EventListener m_listener;
        Action<EventSource> m_OnEventSourceCreated;
#endregion
    }
}
