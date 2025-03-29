// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Reflection;
using Xunit;

namespace BasicEventSourceTests
{
    public class TestsEventSourceLifetime
    {
        /// <summary>
        /// Validates that the EventProvider AppDomain.ProcessExit handler does not keep the EventProvider instance
        /// alive.
        /// </summary>
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // non-Windows EventSources don't have lifetime
        public void Test_EventSource_Lifetime()
        {
            TestUtilities.CheckNoEventSourcesRunning("Start");
            WeakReference wrProvider = new WeakReference(null);
            WeakReference wrEventSource = new WeakReference(null);

            // Need to call separate method (ExerciseEventSource) to reference the event source
            // in order to avoid the debug JIT lifetimes (extended to the end of the current method)
            ExerciseEventSource(wrProvider, wrEventSource);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.Null(wrEventSource.Target);
            Assert.Null(wrProvider.Target);
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        private void ExerciseEventSource(WeakReference wrProvider, WeakReference wrEventSource)
        {
            using (var es = new LifetimeTestEventSource())
            {
                FieldInfo field = es.GetType().GetTypeInfo().BaseType.GetField("m_provider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    field = es.GetType().GetTypeInfo().BaseType.GetField("m_etwProvider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                object provider = field.GetValue(es);
                wrProvider.Target = provider;
                wrEventSource.Target = es;
                es.Event0();
            }
        }

        private class LifetimeTestEventSource : EventSource
        {
            [Event(1)]
            public void Event0()
            { WriteEvent(1); }
        }
    }
}
