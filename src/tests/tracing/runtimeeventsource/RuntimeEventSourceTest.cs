// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

AppContext.SetSwitch("appContextSwitch", true);
AppDomain.CurrentDomain.SetData("appContextBoolData", true); // Not loggeed, bool key
AppDomain.CurrentDomain.SetData("appContextBoolAsStringData", "true");
AppDomain.CurrentDomain.SetData("appContextStringData", "myString"); // Not logged, string does not parse as bool
AppDomain.CurrentDomain.SetData("appContextSwitch", "false"); // should not override the SetSwitch above

// Create an EventListener.
using (var myListener = new RuntimeEventListener())
{
    await Task.Delay(10);
    if (myListener.Verify())
    {
        Console.WriteLine("Test passed");
        return 100;
    }
    else
    {
        Console.WriteLine($"Test Failed - did not see one or more of the expected runtime counters.");
        return 1;
    }
}

public class RuntimeEventListener : EventListener
{
    private readonly Dictionary<string, bool> observedEvents = new Dictionary<string, bool>() {
        { "appContextSwitch", false },
        { "appContextBoolAsStringData", false },
    };

    private static readonly string[] s_unexpectedEvents = new[] {
        "appContextBoolData",
        "appContextStringData",
    };

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name.Equals("System.Runtime"))
        {
            EnableEvents(source, EventLevel.Informational, (EventKeywords)1 /* RuntimeEventSource.Keywords.AppContext */);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Check AppContext switches
        if (eventData is { EventName: "LogAppContextSwitch",
                           Payload: { Count: 2 } } &&
            eventData.Payload[0] is string switchName)
        {
            observedEvents[switchName] = ((int)eventData.Payload[1]) == 1;
            return;
        }
    }

    public bool Verify()
    {
        foreach (string counterName in observedEvents.Keys)
        {
            if (!observedEvents[counterName])
            {
                return false;
            }
            else
            {
                Console.WriteLine($"Saw {counterName}");
            }
        }

        foreach (var key in s_unexpectedEvents)
        {
            if (observedEvents.ContainsKey(key))
            {
                Console.WriteLine($"Should not have seen {key}");
                return false;
            }
        }
        return true;
    }
}