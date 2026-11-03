// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Threading.Tasks;
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

        /// <summary>
        /// Validates that disposing an EventListener subscribed to the runtime provider releases its native
        /// EventPipe session. EventPipe has a fixed number of sessions, so leaking one per listener would
        /// eventually stop new subscriptions from receiving events.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
        public async Task RuntimeProviderListener_RepeatedSubscriptions_DoNotLeakSessions()
        {
            const int Iterations = 80; // more than the 64 native session slots
            const EventKeywords GCKeyword = (EventKeywords)0x1;

            for (int i = 0; i < Iterations; i++)
            {
                using var listener = new RuntimeProviderListener(GCKeyword);
                if (i % 2 == 0)
                {
                    await Task.Delay(1);
                }
            }

            await Task.Delay(500);

            using var finalListener = new RuntimeProviderListener(GCKeyword);
            for (int attempt = 0; attempt < 50 && !finalListener.ReceivedEvent; attempt++)
            {
                GC.Collect();
                await Task.Delay(100);
            }

            Assert.True(finalListener.ReceivedEvent, "A new runtime-provider subscription received no events.");
        }

        private sealed class RuntimeProviderListener : EventListener
        {
            private readonly EventKeywords _keywords;

            public volatile bool ReceivedEvent;

            public RuntimeProviderListener(EventKeywords keywords)
            {
                _keywords = keywords;
                // Sources created before the listener are delivered from the base constructor, before _keywords is set.
                foreach (EventSource source in EventSource.GetSources())
                {
                    OnEventSourceCreated(source);
                }
            }

            protected override void OnEventSourceCreated(EventSource source)
            {
                if (source.Name == "Microsoft-Windows-DotNETRuntime" && _keywords != 0)
                {
                    EnableEvents(source, EventLevel.Informational, _keywords);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData) => ReceivedEvent = true;
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
