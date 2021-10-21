// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Diagnostics;
using Xunit;
using System;
using System.Collections.Generic;

namespace BasicEventSourceTests
{
    internal class TestUtilities
    {
        // Specifies whether the process is elevated or not.
        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(() => AdminHelpers.IsProcessElevated());
        internal static bool IsProcessElevated => s_isElevated.Value;

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
                    eventSource.Name != "System.Collections.Concurrent.ConcurrentCollectionsEventSource" &&
                    eventSource.Name != "System.Runtime.InteropServices.InteropEventProvider" &&
                    eventSource.Name != "System.Reflection.Runtime.Tracing" &&
                    eventSource.Name != "Microsoft-Windows-DotNETRuntime" &&
                    eventSource.Name != "System.Runtime" &&

                    // These event sources show up when hosted in the VS test runner
                    eventSource.Name != "System.Net.Sockets" &&
                    eventSource.Name != "Private.InternalDiagnostics.System.Net.Sockets" &&
                    eventSource.Name != "TestPlatform"
                    )
                {
                    eventSourceNames += eventSource.Name + " ";
                }
            }

            Debug.WriteLine(message);
            Assert.Equal("", eventSourceNames);
        }

        /// <summary>
        /// Unwraps a nullable returned from either ETW or EventListener
        /// </summary>
        /// <typeparam name="T">The type to unwrap</typeparam>
        /// <param name="wrappedValue">Value returned from event payload</param>
        /// <returns></returns>
        public static T? UnwrapNullable<T>(object wrappedValue) where T : struct
        {
            // ETW will return a collection of key/value pairs
            if (wrappedValue is IDictionary<string, object> dict)
            {
                Assert.True(dict.ContainsKey("HasValue"));
                Assert.True(dict.ContainsKey("Value"));

                if ((bool)dict["HasValue"])
                {
                    return (T)dict["Value"];
                }
            }
            // EventListener will return the boxed value of the nullable, which will either be a value or null object reference
            else if (wrappedValue != null)
            {
                Assert.IsType<T>(wrappedValue);
                return (T?)wrappedValue;
            }

            // wrappedValue is null or wrappedValue is IDictionary, but HasValue is false
            return null;
        }
    }
}
