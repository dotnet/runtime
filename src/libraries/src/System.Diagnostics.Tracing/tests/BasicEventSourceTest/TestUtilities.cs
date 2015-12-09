// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                   && eventSource.Name != "System.Diagnostics.Eventing.FrameworkEventSource")
                {
                    eventSourceNames += eventSource.Name + " ";
                }
            }

            Debug.WriteLine(message);
            Assert.Equal("", eventSourceNames);
        }
    }
}
