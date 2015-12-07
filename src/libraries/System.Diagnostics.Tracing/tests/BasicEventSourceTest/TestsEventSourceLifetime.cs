// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace BasicEventSourceTests
{
    public class TestsEventSourceLifetime
    {
        /// <summary>
        /// Validates that the EventProvider AppDomain.ProcessExit handler does not keep the EventProvider instance
        /// alive.
        /// </summary>
        [ActiveIssue(4871, PlatformID.AnyUnix)]
        [Fact]
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

            Assert.Equal(null, wrEventSource.Target);
            Assert.Equal(null, wrProvider.Target);
            TestUtilities.CheckNoEventSourcesRunning("Stop");
        }

        private void ExerciseEventSource(WeakReference wrProvider, WeakReference wrEventSource)
        {
            using (var es = new LifetimeTestEventSource())
            {
                FieldInfo field = es.GetType().GetTypeInfo().BaseType.GetField("m_provider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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