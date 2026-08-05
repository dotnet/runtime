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
        /// Validates that disposing an EventSource from its own OnEventCommand callback is rejected.
        /// </summary>
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "DiagnosticsClient IPC is not available on browser")]
        public void Test_EventSource_DisposeInOnEventCommand_Throws()
        {
            using var callbackCompleted = new ManualResetEventSlim(false);
            using var source = new DisposeInCallbackEventSource(callbackCompleted);

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

            bool completed = callbackCompleted.Wait(TimeSpan.FromSeconds(30));

            session.Stop();
            readerTask.Wait(TimeSpan.FromSeconds(5));

            Assert.True(completed, "The EventSource callback did not complete.");
            Assert.IsType<InvalidOperationException>(source._disposeException);
        }

        [EventSource(Name = "TestsEventSourceCallbacks.DisposeInCallbackEventSource")]
        private class DisposeInCallbackEventSource : EventSource
        {
            private readonly ManualResetEventSlim _callbackCompleted;
            internal InvalidOperationException? _disposeException;

            internal DisposeInCallbackEventSource(ManualResetEventSlim callbackCompleted)
            {
                _callbackCompleted = callbackCompleted;
            }

            protected override void OnEventCommand(EventCommandEventArgs command)
            {
                if (command.Command == EventCommand.Enable)
                {
                    try
                    {
                        Dispose();
                    }
                    catch (InvalidOperationException ex)
                    {
                        _disposeException = ex;
                    }
                    finally
                    {
                        _callbackCompleted.Set();
                    }
                }
            }
        }

        [Fact]
        public void Test_EventSource_ConcurrentDisposeWaitsForCallback()
        {
            using var callbackEntered = new ManualResetEventSlim(false);
            using var callbackRelease = new ManualResetEventSlim(false);
            using var disposeStarted = new ManualResetEventSlim(false);
            using var source = new BlockingCallbackEventSource(callbackEntered, callbackRelease);
            using var listener = new PassiveListener();

            Task enableTask = Task.Run(() => listener.EnableEvents(source, EventLevel.Verbose));
            Task? disposeTask = null;
            try
            {
                Assert.True(callbackEntered.Wait(TimeSpan.FromSeconds(30)));

                disposeTask = Task.Run(() =>
                {
                    disposeStarted.Set();
                    source.Dispose();
                });
                Assert.True(disposeStarted.Wait(TimeSpan.FromSeconds(30)));
                Assert.False(disposeTask.Wait(TimeSpan.FromMilliseconds(100)));
            }
            finally
            {
                callbackRelease.Set();
                Assert.True(disposeTask is null
                    ? enableTask.Wait(TimeSpan.FromSeconds(30))
                    : Task.WaitAll(new[] { enableTask, disposeTask }, TimeSpan.FromSeconds(30)));
            }
        }

        private sealed class PassiveListener : EventListener
        {
        }

        [EventSource(Name = "TestsEventSourceCallbacks.BlockingCallbackEventSource")]
        private sealed class BlockingCallbackEventSource : EventSource
        {
            private readonly ManualResetEventSlim _callbackEntered;
            private readonly ManualResetEventSlim _callbackRelease;

            internal BlockingCallbackEventSource(
                ManualResetEventSlim callbackEntered,
                ManualResetEventSlim callbackRelease)
            {
                _callbackEntered = callbackEntered;
                _callbackRelease = callbackRelease;
            }

            protected override void OnEventCommand(EventCommandEventArgs command)
            {
                if (command.Command == EventCommand.Enable)
                {
                    _callbackEntered.Set();
                    _callbackRelease.Wait();
                }
            }
        }
    }
}
