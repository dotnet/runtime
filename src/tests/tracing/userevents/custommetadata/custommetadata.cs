// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Tracing.UserEvents.Tests.Common;
using Microsoft.Diagnostics.Tracing;

namespace Tracing.UserEvents.Tests.CustomMetadata
{
    public static class CustomMetadata
    {
        public static void CustomMetadataTracee()
        {
            CustomMetadataEventSource.Log.WorkItem(1, "Item1");
        }

        private static readonly Func<EventPipeEventSource, bool> s_traceValidator = source =>
        {
            bool anyMatching = false;

            source.Dynamic.All += (TraceEvent e) =>
            {
                if (!string.Equals(e.ProviderName, "DemoCustomMetadata", StringComparison.Ordinal))
                {
                    return;
                }

                if (!string.Equals(e.EventName, "WorkItem", StringComparison.Ordinal))
                {
                    return;
                }

                try
                {
                    object? idObj = e.PayloadByName("id");
                    object? nameObj = e.PayloadByName("name");
                    int id = idObj is null ? -1 : Convert.ToInt32(idObj);
                    string? name = nameObj as string;

                    if (id != 1 || !string.Equals(name, "Item1", StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine($"Unexpected payload values: id={id}, name={name}");
                    }
                    else
                    {
                        anyMatching = true;
                        Console.WriteLine($"CustomMetadata event: Id={id}, Name={name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Exception while reading CustomMetadata payload: {ex}");
                }
            };

            source.Process();

            if (!anyMatching)
            {
                Console.Error.WriteLine("The trace did not contain the expected CustomMetadata event.");
            }

            return anyMatching;
        };

        public static int Main(string[] args)
        {
            return UserEventsTestRunner.Run(
                args,
                "custommetadata",
                typeof(CustomMetadata).Assembly.Location,
                CustomMetadataTracee,
                s_traceValidator);
        }
    }

    [EventSource(Name = "DemoCustomMetadata")]
    public sealed class CustomMetadataEventSource : EventSource
    {
        public static readonly CustomMetadataEventSource Log = new CustomMetadataEventSource();

        private CustomMetadataEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat) {}

        [Event(1, Level = EventLevel.Informational)]
        public void WorkItem(int id, string name)
        {
            WriteEvent(1, id, name);
        }
    }
}
