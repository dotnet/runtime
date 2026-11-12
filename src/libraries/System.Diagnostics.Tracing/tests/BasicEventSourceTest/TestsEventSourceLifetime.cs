// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnMono("Runtime-provider events are dispatched by CoreCLR.")]
        [InlineData(1)]
        [InlineData(8)]
        public void RuntimeProviderListener_DelayedDispatchStart_ReceivesEvents(int iterations)
        {
            RemoteExecutor.Invoke(async iterationCount =>
            {
                const EventKeywords GCKeyword = (EventKeywords)0x1;
                // The old sessions must not be able to deliver the GC events expected by the final listener.
                const EventKeywords ThreadingKeyword = (EventKeywords)0x10000;
                int count = int.Parse(iterationCount, CultureInfo.InvariantCulture);
                var dispatchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var releaseDispatch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var delayDispatch = new AsyncLocal<bool>(args =>
                {
                    // Context restoration runs before the dispatch task's delegate reads its cancellation token.
                    if (args.ThreadContextChanged && args.CurrentValue)
                    {
                        dispatchStarted.TrySetResult();
                        releaseDispatch.Task.GetAwaiter().GetResult();
                    }
                });

                try
                {
                    delayDispatch.Value = true;
                    for (int i = 0; i < count; i++)
                    {
                        using var listener = new RuntimeProviderListener(ThreadingKeyword);
                    }

                    delayDispatch.Value = false;
                    await dispatchStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));

                    using var finalListener = new RuntimeProviderListener(GCKeyword);
                    releaseDispatch.SetResult();

                    for (int attempt = 0; attempt < 50 && !finalListener.ReceivedEvent; attempt++)
                    {
                        GC.Collect();
                        await Task.Delay(100);
                    }

                    Assert.True(finalListener.ReceivedEvent, "A new runtime-provider subscription received no events after delayed dispatch startup.");
                }
                finally
                {
                    delayDispatch.Value = false;
                    releaseDispatch.TrySetResult();
                }
            }, iterations.ToString(CultureInfo.InvariantCulture)).Dispose();
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
