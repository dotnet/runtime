// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicEventSourceTests;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Tracing.Tests.Common;
using Xunit;

[EventSource(Name = "Test.EventSourceNull")]
class EventSourceNullTest : EventSource
{
    [Event(1)]
    public void EventNullString(string str, int i, float f, long l)
    {
        WriteEvent(1, str, i, f, l);
    }

    [Event(2)]
    public void EventNullByteArray(byte[] bytes, int i, float f, long l)
    {
        WriteEvent(2, bytes, i, f, l);
    }
}


namespace Tracing.Tests.ETWNullEvent
{
    public class ProviderValidation
    {
        public static void Main(string[] args)
        {
            List<EventPipeProvider> providers = new List<EventPipeProvider>
            {
                new EventPipeProvider("Test.EventSourceNull", EventLevel.Verbose)
            };

            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            using (EventPipeSession session = client.StartEventPipeSession(providers, /* requestRunDown */ false))
            {
                using (var log = new EventSourceNullTest())
                {
                    using (var el = new LoudListener(log))
                    {
                        log.EventNullString(null, 10, 11, 12);
                        Assert.Equal(1, LoudListener.t_lastEvent.EventId);
                        Assert.Equal(4, LoudListener.t_lastEvent.Payload.Count);
                        Assert.Equal("", (string)LoudListener.t_lastEvent.Payload[0]);
                        Assert.Equal(10, (int)LoudListener.t_lastEvent.Payload[1]);
                        Assert.Equal(11, (float)LoudListener.t_lastEvent.Payload[2]);
                        Assert.Equal(12, (long)LoudListener.t_lastEvent.Payload[3]);

                        log.EventNullByteArray(null, 10, 11, 12);
                        Assert.Equal(2, LoudListener.t_lastEvent.EventId);
                        Assert.Equal(4, LoudListener.t_lastEvent.Payload.Count);
                        Assert.Equal(new byte[0], (byte[])LoudListener.t_lastEvent.Payload[0]);
                        Assert.Equal(10, (int)LoudListener.t_lastEvent.Payload[1]);
                        Assert.Equal(11, (float)LoudListener.t_lastEvent.Payload[2]);
                        Assert.Equal(12, (long)LoudListener.t_lastEvent.Payload[3]);

                        var events = new EventPipeEventSource(session.EventStream);
                        events.Process();
                    }
                    session.Stop();
                }
            }
        }
    }
}
