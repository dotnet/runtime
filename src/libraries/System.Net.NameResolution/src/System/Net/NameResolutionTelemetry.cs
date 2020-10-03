// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace System.Net
{
    [EventSource(Name = "System.Net.NameResolution")]
    internal sealed class NameResolutionTelemetry : EventSource
    {
        public static readonly NameResolutionTelemetry Log = new NameResolutionTelemetry();

        private const int ResolutionStartEventId = 1;
        private const int ResolutionStopEventId = 2;
        private const int ResolutionFailedEventId = 3;

        private PollingCounter? _lookupsRequestedCounter;
        private EventCounter? _lookupsDuration;

        private long _lookupsRequested;

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // The cumulative number of name resolution requests started since events were enabled
                _lookupsRequestedCounter ??= new PollingCounter("dns-lookups-requested", this, () => Interlocked.Read(ref _lookupsRequested))
                {
                    DisplayName = "DNS Lookups Requested"
                };

                _lookupsDuration ??= new EventCounter("dns-lookups-duration", this)
                {
                    DisplayName = "Average DNS Lookup Duration",
                    DisplayUnits = "ms"
                };
            }
        }


        private const int MaxIPFormattedLength = 128;

        [Event(ResolutionStartEventId, Level = EventLevel.Informational)]
        private void ResolutionStart(string hostNameOrAddress) => WriteEvent(ResolutionStartEventId, hostNameOrAddress);

        [Event(ResolutionStopEventId, Level = EventLevel.Informational)]
        private void ResolutionStop() => WriteEvent(ResolutionStopEventId);

        [Event(ResolutionFailedEventId, Level = EventLevel.Informational)]
        private void ResolutionFailed() => WriteEvent(ResolutionFailedEventId);


        [NonEvent]
        public ValueStopwatch BeforeResolution(string hostNameOrAddress)
        {
            Debug.Assert(hostNameOrAddress != null);

            if (IsEnabled())
            {
                Interlocked.Increment(ref _lookupsRequested);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    ResolutionStart(hostNameOrAddress);
                }

                return ValueStopwatch.StartNew();
            }

            return default;
        }

        [NonEvent]
        public ValueStopwatch BeforeResolution(IPAddress address)
        {
            Debug.Assert(address != null);

            if (IsEnabled())
            {
                Interlocked.Increment(ref _lookupsRequested);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    WriteEvent(ResolutionStartEventId, FormatIPAddressNullTerminated(address, stackalloc char[MaxIPFormattedLength]));
                }

                return ValueStopwatch.StartNew();
            }

            return default;
        }

        [NonEvent]
        public void AfterResolution(ValueStopwatch stopwatch, bool successful)
        {
            if (stopwatch.IsActive)
            {
                _lookupsDuration!.WriteMetric(stopwatch.GetElapsedTime().TotalMilliseconds);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    if (!successful)
                    {
                        ResolutionFailed();
                    }

                    ResolutionStop();
                }
            }
        }


        [NonEvent]
        private static Span<char> FormatIPAddressNullTerminated(IPAddress address, Span<char> destination)
        {
            Debug.Assert(address != null);

            bool success = address.TryFormat(destination, out int charsWritten);
            Debug.Assert(success);

            Debug.Assert(charsWritten < destination.Length);
            destination[charsWritten] = '\0';

            return destination.Slice(0, charsWritten + 1);
        }


        // WriteEvent overloads taking Span<char> are imitating string arguments
        // Span arguments are expected to be null-terminated

        [NonEvent]
        private unsafe void WriteEvent(int eventId, Span<char> arg1)
        {
            Debug.Assert(!arg1.IsEmpty && arg1.IndexOf('\0') == arg1.Length - 1, "Expecting a null-terminated ROS<char>");

            if (IsEnabled())
            {
                fixed (char* arg1Ptr = &MemoryMarshal.GetReference(arg1))
                {
                    EventData descr = new EventData
                    {
                        DataPointer = (IntPtr)(arg1Ptr),
                        Size = arg1.Length * sizeof(char)
                    };

                    WriteEventCore(eventId, eventDataCount: 1, &descr);
                }
            }
        }
    }
}
