// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    [EventSource(Name = "Microsoft-Extensions-DependencyInjection")]
    internal sealed class DependencyInjectionEventSource : EventSource
    {
        public static readonly DependencyInjectionEventSource Log = new DependencyInjectionEventSource();

        private DependencyInjectionEventSource()
        {
        }

        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.
        // - Avoid renaming methods or parameters marked with EventAttribute. EventSource uses these to form the event object.

        [Event(1, Level = EventLevel.Verbose)]
        private void CallSiteBuilt(string serviceType, string callSite)
        {
            WriteEvent(1, serviceType, callSite);
        }

        [Event(2, Level = EventLevel.Verbose)]
        public void ServiceResolved(string serviceType)
        {
            WriteEvent(2, serviceType);
        }

        [NonEvent]
        public void ServiceResolved(Type serviceType)
        {
            if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                ServiceResolved(serviceType.ToString());
            }
        }

        [NonEvent]
        public void CallSiteBuilt(Type serviceType, ServiceCallSite callSite)
        {
            if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
            {
                CallSiteBuilt(serviceType.ToString(), CallSiteJsonFormatter.Instance.Format(callSite));
            }
        }
    }
}
