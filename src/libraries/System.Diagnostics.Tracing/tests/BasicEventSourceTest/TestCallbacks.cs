// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System.Reflection;

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
    }
}
