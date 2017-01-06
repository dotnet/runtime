// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
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
                if (eventSource.Name != "System.Threading.Tasks.TplEventSource"
                   && eventSource.Name != "System.Diagnostics.Eventing.FrameworkEventSource"
                   && eventSource.Name != "System.Buffers.ArrayPoolEventSource")
                {
                    eventSourceNames += eventSource.Name + " ";
                }
            }

            Debug.WriteLine(message);
            Assert.Equal("", eventSourceNames);
        }
    }
}
