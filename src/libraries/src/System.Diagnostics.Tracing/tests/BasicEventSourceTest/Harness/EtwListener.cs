// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BasicEventSourceTests
{
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
                throw new Exception("Need to be elevated to run. ");
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
            if(_disposed)
            {
                return;
            }

            _disposed = true;
            _session.Flush();
            Thread.Sleep(1010);      // Let it drain.
            _session.Dispose();     // This also will kill the real time thread 

            if (_dataFileName != null)
            {
                using (var traceEventSource = new ETWTraceEventSource(_dataFileName))
                {
                    Debug.WriteLine("Processing data file " + Path.GetFullPath(_dataFileName));

                    // Parse all the events as best we can, and also send unhandled events there as well.  
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
            // Ignore EventTrace events.
            if (data.ProviderGuid == EventTraceProviderID)
                return;

            // Ignore kernel events.
            if (data.ProviderGuid == KernelProviderID)
                return;

            // Ignore manifest events. 
            if ((int)data.ID == 0xFFFE)
                return;
            this.OnEvent(new EtwEvent(data));
        }

        private static readonly Guid EventTraceProviderID = new Guid("9e814aad-3204-11d2-9a82-006008a86939");
        private static readonly Guid KernelProviderID = new Guid("9e814aad-3204-11d2-9a82-006008a86939");

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

        private bool _disposed;
        private string _dataFileName;
        private volatile TraceEventSession _session;
    #endregion

    }
}
