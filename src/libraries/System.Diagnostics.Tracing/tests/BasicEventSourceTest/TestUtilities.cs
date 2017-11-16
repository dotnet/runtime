// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Diagnostics;
using Xunit;
using System;

namespace BasicEventSourceTests
{
    internal class TestUtilities
    {
        /// <summary>
        /// Confirms that there are no EventSources running.  
        /// </summary>
        /// <param name="message">Will be printed as part of the Assert</param>
        public static void CheckNoEventSourcesRunning(string message = "")
        {
            var eventSources = EventSource.GetSources();

            string eventSourceNames = "";
            foreach (var eventSource in EventSource.GetSources())
            {
                // Exempt sources built in to the framework that might be used by types involved in the tests
                if (eventSource.Name != "System.Threading.Tasks.TplEventSource" &&
                    eventSource.Name != "System.Diagnostics.Eventing.FrameworkEventSource" &&
                    eventSource.Name != "System.Buffers.ArrayPoolEventSource" &&
                    eventSource.Name != "System.Threading.SynchronizationEventSource" &&
                    eventSource.Name != "System.Runtime.InteropServices.InteropEventProvider" &&
                    eventSource.Name != "System.Reflection.Runtime.Tracing"
                    )
                {
                    eventSourceNames += eventSource.Name + " ";
                }
            }

            Debug.WriteLine(message);
            Assert.Equal("", eventSourceNames);
        }
    }
}
