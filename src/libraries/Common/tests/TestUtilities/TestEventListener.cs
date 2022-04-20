// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;
using Xunit.Abstractions;

namespace TestUtilities;

/// <summary>
/// Logging helper for tests.
/// Logs event source events into test output.
/// </summary>
public sealed class TestEventListener : EventListener
{
    private readonly ITestOutputHelper _output;
    private readonly HashSet<string> _sourceNames;

    // Until https://github.com/dotnet/runtime/issues/63979 is solved.
    private List<EventSource> _eventSources = new List<EventSource>();

    public TestEventListener(ITestOutputHelper output, params string[] sourceNames)
    {
        _output = output;
        _sourceNames = new HashSet<string>(sourceNames);
        foreach (var eventSource in _eventSources)
        {
            OnEventSourceCreated(eventSource);
        }
        _eventSources = null;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // We're called from base ctor, just save the event source for later initialization.
        if (_sourceNames is null)
        {
            _eventSources.Add(eventSource);
            return;
        }

        // Second pass called from our ctor, allow logging for specified source names.
        if (_sourceNames.Contains(eventSource.Name))
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var sb = new StringBuilder().Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}[{eventData.EventName}] ");
        for (int i = 0; i < eventData.Payload?.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
        }
        try
        {
            _output.WriteLine(sb.ToString());
        }
        catch { }
    }
}
