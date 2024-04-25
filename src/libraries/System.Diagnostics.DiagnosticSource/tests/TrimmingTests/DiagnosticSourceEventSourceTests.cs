// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

/// <summary>
/// Tests that writing to a DiagnosticSource writes the correct payloads
/// to the DiagnosticSourceEventSource.
/// </summary>
internal class Program
{
    private class TestEventListener : EventListener
    {
        public ReadOnlyCollection<object> LogDataPayload { get; set; }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Diagnostics-DiagnosticSource")
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, new Dictionary<string, string>
                {
                    { "FilterAndPayloadSpecs", "TestDiagnosticListener/Test.Start@Activity2Start:-Id;Ints.*Enumerate"}
                });
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "Activity2Start")
            {
                LogDataPayload = eventData.Payload;
            }

            base.OnEventWritten(eventData);
        }
    }

    public static int Main()
    {
        DiagnosticSource diagnosticSource = new DiagnosticListener("TestDiagnosticListener");
        using (var listener = new TestEventListener())
        {
            var data = new EventData()
            {
                Id = Guid.NewGuid(),
            };

            Write(diagnosticSource, "Test.Start", data);

            if (!(listener.LogDataPayload?.Count == 3 &&
                (string)listener.LogDataPayload[0] == "TestDiagnosticListener" &&
                (string)listener.LogDataPayload[1] == "Test.Start"))
            {
                return -1;
            }

            object[] args = (object[])listener.LogDataPayload[2];
            if (args.Length != 2)
            {
                return -2;
            }

            IDictionary<string, object> arg = (IDictionary<string, object>)args[0];
            if (!((string)arg["Key"] == "Id" && (string)arg["Value"] == data.Id.ToString()))
            {
                return -3;
            }

            arg = (IDictionary<string, object>)args[1];
            if (!((string)arg["Key"] == "*Enumerate" && (string)arg["Value"] == "1,2,3"))
            {
                return -4;
            }

            return 100;
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "The value being passed into Write has the necessary properties being preserved with DynamicallyAccessedMembers.")]
    private static void Write<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        DiagnosticSource diagnosticSource,
        string name,
        T value)
    {
        diagnosticSource.Write(name, value);
    }

    public class EventData
    {
        public Guid Id { get; set; }

        public IEnumerable<int> Ints
        {
            get
            {
                yield return 1;
                yield return 2;
                yield return 3;
            }
        }
    }
}
