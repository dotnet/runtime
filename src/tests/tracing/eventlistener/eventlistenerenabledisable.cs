// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics.Tracing;
using System.Threading;
using Tracing.Tests.Common;

namespace Tracing.Tests
{
    [EventSource(Name = "Tracing.Tests.EnableDisableEventSource")]
    class EnableDisableEventSource : EventSource
    {
        internal int _enables;
        internal int _disables;

        public EnableDisableEventSource() : base(true) { }

        [Event(1)]
        internal void TestEvent() { this.WriteEvent(1); }

        [NonEvent]
        protected override OnEventCommand(EventCommandEventArgs command)
        {
            base.OnEventCommand(command);

            if (command.Command == EventCommand.Enable)
            {
                Interlocked.Increment(ref _enables);
            }
            else if (command.Command == EventCommand.Disable)
            {
                Interlocked.Increment(ref _disables);
            }
            else
            {
                throw new Exception($"Saw unexpected command {command.Command} in OnEventCommand");
            }
        }
    }
    
    internal sealed class EnableDisableListener : EventListener
    {
        private EventSource? _target;

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("Tracing.Tests.EnableDisableEventSource"))
            {
                _target = source;
                EnableEvents(_target, EventLevel.Verbose);
            }
        }

        public void Disable()
        {
            DisableEvents(_target);
        }

        public bool Dispose(bool disableEvents)
        {
            if (disableEvents)
            {
                Disable();
            }

            base.Dispose();
        }
    }

    class EventPipeSmoke
    {
        private static int messageIterations = 100;
        private static readonly DateTime ThePast = DateTime.UtcNow;

        static int Main()
        {
            bool pass = false;
            using(var source = new EnableDisableEventSource()
            {
                using (var listener1 = new EnableDisableListener())
                using (var listener2 = new EnableDisableListener())
                {
                    var listener3 = new EnableDisableListener();
                    source.TestEvent();
                    listener3.Dispose(true);

                    listener2.Disable();
                    listener1.Disable();
                }

                if (source._enables == 3 && source._disables == 3)
                {
                    return 100;
                }

                Console.WriteLine($"Unexpected enable/disable count _enables={source._enables} _disables={source._disables}");
                return -1;
            }
        }
    }
}
