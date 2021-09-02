// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BasicEventSourceTests
{
    public class ActivityTracking
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56073", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void StartStopCreatesActivity()
        {
            using ActivityEventListener l = new ActivityEventListener();
            using ActivityEventSource es = new ActivityEventSource();

            Assert.Equal(Guid.Empty, EventSource.CurrentThreadActivityId);
            es.ExampleStart();
            Assert.NotEqual(Guid.Empty, EventSource.CurrentThreadActivityId);
            es.ExampleStop();
            Assert.Equal(Guid.Empty, EventSource.CurrentThreadActivityId);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56073", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task ActivityFlowsAsync()
        {
            using ActivityEventListener l = new ActivityEventListener();
            using ActivityEventSource es = new ActivityEventSource();

            Assert.Equal(Guid.Empty, EventSource.CurrentThreadActivityId);
            es.ExampleStart();
            Assert.NotEqual(Guid.Empty, EventSource.CurrentThreadActivityId);
            await Task.Yield();
            Assert.NotEqual(Guid.Empty, EventSource.CurrentThreadActivityId);
            es.ExampleStop();
            Assert.Equal(Guid.Empty, EventSource.CurrentThreadActivityId);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task ActivityIdIsZeroedOnThreadSwitchOut()
        {
            using ActivityEventListener l = new ActivityEventListener();
            using ActivityEventSource es = new ActivityEventSource();

            // Run tasks on many threads. If an activity id leaks it is likely
            // that the thread will be sheduled to run one of our other tasks
            // and we can detect the non-zero id at the start of the task
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => YieldTwoActivitiesDeep(es)));
            }

            await Task.WhenAll(tasks.ToArray());
        }

        private async Task YieldTwoActivitiesDeep(ActivityEventSource es)
        {
            Assert.Equal(Guid.Empty, EventSource.CurrentThreadActivityId);
            es.ExampleStart();
            es.Example2Start();
            await Task.Yield();
            es.Example2Stop();
            es.ExampleStop();
            Assert.Equal(Guid.Empty, EventSource.CurrentThreadActivityId);
        }

        // I don't know if this behavior is by-design or accidental. For now
        // I am attempting to preserve it to lower back compat risk, but in
        // the future we might decide it wasn't even desirable to begin with.
        // Compare with SetCurrentActivityIdAfterEventDoesNotFlowAsync below.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56073", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task SetCurrentActivityIdBeforeEventFlowsAsync()
        {
            using ActivityEventListener l = new ActivityEventListener();
            using ActivityEventSource es = new ActivityEventSource();
            try
            {
                Guid g = Guid.NewGuid();
                EventSource.SetCurrentThreadActivityId(g);
                Assert.Equal(g, EventSource.CurrentThreadActivityId);
                es.ExampleStart();
                await Task.Yield();
                es.ExampleStop();
                Assert.Equal(g, EventSource.CurrentThreadActivityId);
            }
            finally
            {
                EventSource.SetCurrentThreadActivityId(Guid.Empty);
            }
        }

        // I don't know if this behavior is by-design or accidental. For now
        // I am attempting to preserve it to lower back compat risk, but in
        // the future we might decide it wasn't even desirable to begin with.
        // Compare with SetCurrentActivityIdBeforeEventFlowsAsync above.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56073", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task SetCurrentActivityIdAfterEventDoesNotFlowAsync()
        {
            using ActivityEventListener l = new ActivityEventListener();
            using ActivityEventSource es = new ActivityEventSource();
            try
            {
                es.ExampleStart();
                Guid g = Guid.NewGuid();
                EventSource.SetCurrentThreadActivityId(g);
                Assert.Equal(g, EventSource.CurrentThreadActivityId);
                await Task.Yield();
                Assert.NotEqual(g, EventSource.CurrentThreadActivityId);
                es.ExampleStop();
            }
            finally
            {
                EventSource.SetCurrentThreadActivityId(Guid.Empty);
            }
        }
    }

    [EventSource(Name = "ActivityEventSource")]
    class ActivityEventSource : EventSource
    {
        [Event(1)]
        public void ExampleStart() => WriteEvent(1);

        [Event(2)]
        public void ExampleStop() => WriteEvent(2);

        [Event(3)]
        public void Example2Start() => WriteEvent(3);

        [Event(4)]
        public void Example2Stop() => WriteEvent(4);
    }

    class ActivityEventListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
            {
                EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
            }
            else if (eventSource.Name == "ActivityEventSource")
            {
                EnableEvents(eventSource, EventLevel.Informational);
            }
        }
    }
}
