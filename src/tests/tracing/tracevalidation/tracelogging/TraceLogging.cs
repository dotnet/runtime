// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using Tracing.Tests.Common;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Tracing.Tests
{
    [EventSource(Name = "ManifestEventSource")]
    public class ManifestEventSource : EventSource
    {
        public static ManifestEventSource Log = new ManifestEventSource();

        private ManifestEventSource()
            : base(true)
        {
        }

        [Event(1)]
        public void EmptyEvent()
        {
            WriteEvent(1);
        }

        [Event(2)]
        public void IntStringEvent(int i, string s)
        {
            WriteEvent(2, i, s);
        }
    }

    [EventSource(Name = "TraceLoggingEventSource")]
    public class TraceLoggingEventSource : EventSource
    {
        public static TraceLoggingEventSource Log = new TraceLoggingEventSource();

        private TraceLoggingEventSource()
            : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        [Event(1)]
        public void EmptyEvent()
        {
            WriteEvent(1);
        }

        [Event(2)]
        public void IntStringEvent(int i, string s)
        {
            WriteEvent(2, i, s);
        }
    }

    public static class TraceLogging
    {
        public static int Main(string[] args)
        {
            using (NetPerfFile file = NetPerfFile.Create(args))
            {
                EventSourceTestSuite suite = new EventSourceTestSuite(file);
                suite.AddEventSource(ManifestEventSource.Log);
                suite.AddEventSource(TraceLoggingEventSource.Log);

                suite.AddTest(new EventSourceTest("ManifestEmptyEvent",
                    delegate()
                    {
                        ManifestEventSource.Log.EmptyEvent();
                    },
                    delegate(TraceEvent eventData)
                    {
                        Assert.Equal("ProviderName", ManifestEventSource.Log.Name, eventData.ProviderName);
                        Assert.Equal("EventName", "EmptyEvent", eventData.EventName);
                        Assert.Equal("PayloadCount", 0, eventData.PayloadNames.Length);
                    }));

                suite.AddTest(new EventSourceTest("TraceLoggingEmptyEvent",
                    delegate()
                    {
                        TraceLoggingEventSource.Log.EmptyEvent();
                    },
                    delegate(TraceEvent eventData)
                    {
                        Assert.Equal("ProviderName", TraceLoggingEventSource.Log.Name, eventData.ProviderName);
                        Assert.Equal("EventName", "EmptyEvent", eventData.EventName);
                        Assert.Equal("PayloadCount", 0, eventData.PayloadNames.Length);
                    }));

                suite.AddTest(new EventSourceTest("ManifestIntString",
                    delegate()
                    {
                        ManifestEventSource.Log.IntStringEvent(42, "Hello World!");
                    },
                    delegate(TraceEvent eventData)
                    {
                        Assert.Equal("ProviderName", ManifestEventSource.Log.Name, eventData.ProviderName);
                        Assert.Equal("EventName", "IntStringEvent", eventData.EventName);
                        Assert.Equal("PayloadCount", 2, eventData.PayloadNames.Length);
                        Assert.Equal("i", 42, (int)eventData.PayloadValue(0));
                        Assert.Equal("s", "Hello World!", (string)eventData.PayloadValue(1));
                    }));

                suite.AddTest(new EventSourceTest("TraceLoggingIntString",
                    delegate()
                    {
                        TraceLoggingEventSource.Log.IntStringEvent(42, "Hello World!");
                    },
                    delegate(TraceEvent eventData)
                    {
                        Assert.Equal("ProviderName", TraceLoggingEventSource.Log.Name, eventData.ProviderName);
                        Assert.Equal("EventName", "IntStringEvent", eventData.EventName);
                        Assert.Equal("PayloadCount", 2, eventData.PayloadNames.Length);
                        Assert.Equal("i", 42, (int)eventData.PayloadValue(0));
                        Assert.Equal("s", "Hello World!", (string)eventData.PayloadValue(1));
                    }));

                suite.RunTests();
            }

            return 100;
        }
    }
}
