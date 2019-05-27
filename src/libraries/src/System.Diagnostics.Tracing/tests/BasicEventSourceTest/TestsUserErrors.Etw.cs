// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Xunit;

namespace BasicEventSourceTests
{
    /// <summary>
    /// Tests the user experience for common user errors.  
    /// </summary>
    public partial class TestsUserErrors
    {
        /// <summary>
        /// Test the 
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // ActiveIssue: https://github.com/dotnet/corefx/issues/29754
        public void Test_BadEventSource_MismatchedIds_WithEtwListener()
        {
            // We expect only one session to be on when running the test but if a ETW session was left
            // hanging, it will confuse the EventListener tests.
            if(TestUtilities.IsProcessElevated)
            {
                EtwListener.EnsureStopped();
            }

            TestUtilities.CheckNoEventSourcesRunning("Start");
            var onStartups = new bool[] { false, true };

            var listenerGenerators = new List<Func<Listener>> { () => new EventListenerListener() };

            if(TestUtilities.IsProcessElevated)
            {
                listenerGenerators.Add(() => new EtwListener());
            }

            var settings = new EventSourceSettings[] { EventSourceSettings.Default, EventSourceSettings.EtwSelfDescribingEventFormat };

            // For every interesting combination, run the test and see that we get a nice failure message.  
            foreach (bool onStartup in onStartups)
            {
                foreach (Func<Listener> listenerGenerator in listenerGenerators)
                {
                    foreach (EventSourceSettings setting in settings)
                    {
                        Test_Bad_EventSource_Startup(onStartup, listenerGenerator(), setting);
                    }
                }
            }

            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }
    }
}
