// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

namespace BasicEventSourceTests
{
    public class TestsEventSourceCallbacks
    {
        /// <summary>
        /// Validates that the EventProvider AppDomain.ProcessExit handler does not keep the EventProvider instance
        /// alive.
        /// </summary>
        [Fact]
        public void Test_EventSource_Lifetime()
        {
            using (var source = new CallbacksTestEventSource())
            {
                bool isDisabledInDelegate = false;
                source.EventCommandExecuted += (sender, args) =>
                {
                    if (args.Command == EventCommand.Disable)
                    {
                        EventSource eventSource = (EventSource)sender;
                        isDisabledInDelegate = !eventSource.IsEnabled();
                    }
                };

                using (var listener = new CallbacksEventListener())
                {
                    source.Event();
                }

                if (!source._isDisabledInCallback)
                {
                    Assert.Fail("EventSource was still enabled in OnEventCommand callback");
                }

                if (!isDisabledInDelegate)
                {
                    Assert.Fail("EventSource was still enabled in EventCommandExecuted delegate");
                }
            }
        }

        private class CallbacksEventListener : EventListener
        {
            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                base.OnEventSourceCreated(eventSource);

                if (eventSource.Name.Equals("TestsEventSourceCallbacks.CallbacksTestEventSource"))
                {
                    EnableEvents(eventSource, EventLevel.Verbose);
                }
            }
        }

        [EventSource(Name = "TestsEventSourceCallbacks.CallbacksTestEventSource")]
        private class CallbacksTestEventSource : EventSource
        {
            internal bool _isDisabledInCallback;

            [Event(1)]
            public void Event()
            {
                WriteEvent(1);
            }

            [NonEvent]
            protected override void OnEventCommand(EventCommandEventArgs command)
            {
                base.OnEventCommand(command);

                _isDisabledInCallback = !IsEnabled();
            }
        }

        /// <summary>
        /// Validates that calling Dispose() on an EventSource from within OnEventCommand
        /// (triggered by an EventPipe session enabling the provider) does not deadlock.
        /// See https://github.com/dotnet/runtime/issues/106087
        /// </summary>
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "DiagnosticsClient IPC is not available on browser")]
        public void Test_EventSource_DisposeInOnEventCommand_DoesNotDeadlock()
        {
            using var disposeCompleted = new ManualResetEventSlim(false);
            using var source = new DisposeInCallbackEventSource(disposeCompleted);

            var providers = new[] { new EventPipeProvider(source.Name, System.Diagnostics.Tracing.EventLevel.Verbose, long.MaxValue) };
            var client = new DiagnosticsClient(Environment.ProcessId);
            using var session = client.StartEventPipeSession(providers, requestRundown: false);

            // Drain the event stream in a background thread so the runtime's buffer doesn't fill up
            // and so session.Stop() can complete.
            Task readerTask = Task.Run(() =>
            {
                try
                {
                    using var eventPipeSource = new Microsoft.Diagnostics.Tracing.EventPipeEventSource(session.EventStream);
                    eventPipeSource.Process();
                }
                catch (Exception) { }  // Stream is closed when session stops. The exact exception type
                                       // varies by TraceEvent version/platform, so catch broadly here.
            });

            // Wait for Dispose() to complete; if it deadlocks, this will timeout.
            bool disposed = disposeCompleted.Wait(TimeSpan.FromSeconds(30));

            session.Stop();
            readerTask.Wait(TimeSpan.FromSeconds(5));

            Assert.True(disposed, "EventSource.Dispose() called from within OnEventCommand did not complete. Possible deadlock.");
        }

        [EventSource(Name = "TestsEventSourceCallbacks.DisposeInCallbackEventSource")]
        private class DisposeInCallbackEventSource : EventSource
        {
            private readonly ManualResetEventSlim _disposeCompleted;

            internal DisposeInCallbackEventSource(ManualResetEventSlim disposeCompleted)
            {
                _disposeCompleted = disposeCompleted;
            }

            protected override void OnEventCommand(EventCommandEventArgs command)
            {
                if (command.Command == EventCommand.Enable)
                {
                    Dispose();
                    _disposeCompleted.Set();
                }
            }
        }
    }
}
