// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace TestUtilities;

/// <summary>
/// Logging helper for tests.
/// Logs event source events into test output.
/// Example usage:
///   // Put the following line into your test method:
///   using var listener = new TestEventListener(_output, TestEventListener.NetworkingEvents);
/// </summary>
public sealed class TestEventListener : EventListener
{
    public static string[] NetworkingEvents => new[]
    {
        "System.Net.Http",
        "System.Net.NameResolution",
        "System.Net.Sockets",
        "System.Net.Security",
        "System.Net.TestLogging",
        "Private.InternalDiagnostics.System.Net.Http",
        "Private.InternalDiagnostics.System.Net.NameResolution",
        "Private.InternalDiagnostics.System.Net.Sockets",
        "Private.InternalDiagnostics.System.Net.Security",
        "Private.InternalDiagnostics.System.Net.Quic",
        "Private.InternalDiagnostics.System.Net.WebSockets",
        "Private.InternalDiagnostics.System.Net.Http.WinHttpHandler",
        "Private.InternalDiagnostics.System.Net.HttpListener",
        "Private.InternalDiagnostics.System.Net.Mail",
        "Private.InternalDiagnostics.System.Net.NetworkInformation",
        "Private.InternalDiagnostics.System.Net.Primitives",
        "Private.InternalDiagnostics.System.Net.Requests",
    };

    private readonly Action<string> _writeFunc;
    private readonly HashSet<string> _sourceNames;
    private readonly bool _enableActivityId;

    // Until https://github.com/dotnet/runtime/issues/63979 is solved.
    private List<EventSource> _eventSources = new List<EventSource>();

    public TestEventListener(TextWriter output, params string[] sourceNames)
        : this(output.WriteLine, sourceNames)
    { }

    public TestEventListener(ITestOutputHelper output, params string[] sourceNames)
        : this(output.WriteLine, sourceNames)
    { }

    public TestEventListener(Action<string> writeFunc, params string[] sourceNames)
        : this(writeFunc, enableActivityId: false, sourceNames)
    { }

    public TestEventListener(Action<string> writeFunc, bool enableActivityId, params string[] sourceNames)
    {
        List<EventSource> eventSources = _eventSources;

        lock (this)
        {
            _writeFunc = writeFunc;
            _sourceNames = new HashSet<string>(sourceNames);
            _enableActivityId = enableActivityId;
            _eventSources = null;
        }

        // eventSources were populated in the base ctor and are now owned by this thread, enable them now.
        foreach (EventSource eventSource in eventSources)
        {
            EnableEventSource(eventSource);
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // We're likely called from base ctor, if so, just save the event source for later initialization.
        if (_sourceNames is null)
        {
            lock (this)
            {
                if (_sourceNames is null)
                {
                    _eventSources.Add(eventSource);
                    return;
                }
            }
        }

        // Second pass called after our ctor, allow logging for specified source names.
        EnableEventSource(eventSource);
    }

    private void EnableEventSource(EventSource eventSource)
    {
        if (_sourceNames.Contains(eventSource.Name))
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
        else if (_enableActivityId && eventSource.Name == "System.Threading.Tasks.TplEventSource")
        {
            EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80 /* TasksFlowActivityIds */);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        StringBuilder sb = new StringBuilder();

#if NET || NETSTANDARD2_1_OR_GREATER
            sb.Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}");
            if (_enableActivityId)
            {
                if (eventData.ActivityId != Guid.Empty)
                {
                    string activityId = ActivityHelpers.ActivityPathString(eventData.ActivityId);
                    sb.Append($" {activityId} {new string('-', activityId.Length / 2 - 1 )} ");
                }
                else
                {
                    sb.Append(" /  ");
                }
            }
#endif
            sb.Append($"[{eventData.EventName}] ");

        for (int i = 0; i < eventData.Payload?.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
        }
        try
        {
            _writeFunc?.Invoke(sb.ToString());
        }
        catch { }
    }

    // From https://gist.github.com/MihaZupan/cc63ee68b4146892f2e5b640ed57bc09
    private static class ActivityHelpers
    {
        private enum NumberListCodes : byte
        {
            End = 0x0,
            LastImmediateValue = 0xA,
            PrefixCode = 0xB,
            MultiByte1 = 0xC,
        }

        public static unsafe bool IsActivityPath(Guid guid)
        {
            uint* uintPtr = (uint*)&guid;
            uint sum = uintPtr[0] + uintPtr[1] + uintPtr[2] + 0x599D99AD;
            return ((sum & 0xFFF00000) == (uintPtr[3] & 0xFFF00000));
        }

        public static unsafe string ActivityPathString(Guid guid)
            => IsActivityPath(guid) ? CreateActivityPathString(guid) : guid.ToString();

        internal static unsafe string CreateActivityPathString(Guid guid)
        {
            Debug.Assert(IsActivityPath(guid));

            StringBuilder sb = new StringBuilder();

            byte* bytePtr = (byte*)&guid;
            byte* endPtr = bytePtr + 12;
            char separator = '/';
            while (bytePtr < endPtr)
            {
                uint nibble = (uint)(*bytePtr >> 4);
                bool secondNibble = false;
            NextNibble:
                if (nibble == (uint)NumberListCodes.End)
                {
                    break;
                }
                if (nibble <= (uint)NumberListCodes.LastImmediateValue)
                {
                    sb.Append('/').Append(nibble);
                    if (!secondNibble)
                    {
                        nibble = (uint)(*bytePtr & 0xF);
                        secondNibble = true;
                        goto NextNibble;
                    }
                    bytePtr++;
                    continue;
                }
                else if (nibble == (uint)NumberListCodes.PrefixCode)
                {
                    if (!secondNibble)
                    {
                        nibble = (uint)(*bytePtr & 0xF);
                    }
                    else
                    {
                        bytePtr++;
                        if (endPtr <= bytePtr)
                        {
                            break;
                        }
                        nibble = (uint)(*bytePtr >> 4);
                    }
                    if (nibble < (uint)NumberListCodes.MultiByte1)
                    {
                        return guid.ToString();
                    }
                    separator = '$';
                }
                Debug.Assert((uint)NumberListCodes.MultiByte1 <= nibble);
                uint numBytes = nibble - (uint)NumberListCodes.MultiByte1;
                uint value = 0;
                if (!secondNibble)
                {
                    value = (uint)(*bytePtr & 0xF);
                }
                bytePtr++;
                numBytes++;
                if (endPtr < bytePtr + numBytes)
                {
                    break;
                }
                for (int i = (int)numBytes - 1; 0 <= i; --i)
                {
                    value = (value << 8) + bytePtr[i];
                }
                sb.Append(separator).Append(value);

                bytePtr += numBytes;
            }

            sb.Append('/');
            return sb.ToString();
        }
    }
}
