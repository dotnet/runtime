// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Threading;

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
        private PollingCounter? _currentLookupsCounter;
        private EventCounter? _lookupsDuration;

        private long _lookupsRequested;
        private long _currentLookups;

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // The cumulative number of name resolution requests started since events were enabled
                _lookupsRequestedCounter ??= new PollingCounter("dns-lookups-requested", this, () => Interlocked.Read(ref _lookupsRequested))
                {
                    DisplayName = "DNS Lookups Requested"
                };

                // Current number of DNS requests pending
                _currentLookupsCounter ??= new PollingCounter("current-dns-lookups", this, () => Interlocked.Read(ref _currentLookups))
                {
                    DisplayName = "Current DNS Lookups"
                };

                _lookupsDuration ??= new EventCounter("dns-lookups-duration", this)
                {
                    DisplayName = "Average DNS Lookup Duration",
                    DisplayUnits = "ms"
                };
            }
        }

        [Event(ResolutionStartEventId, Level = EventLevel.Informational)]
        private void ResolutionStart(string hostNameOrAddress) => WriteEvent(ResolutionStartEventId, hostNameOrAddress);

        [Event(ResolutionStopEventId, Level = EventLevel.Informational)]
        private void ResolutionStop() => WriteEvent(ResolutionStopEventId);

        [Event(ResolutionFailedEventId, Level = EventLevel.Informational)]
        private void ResolutionFailed() => WriteEvent(ResolutionFailedEventId);


        [NonEvent]
        public long BeforeResolution(object hostNameOrAddress)
        {
            if (IsEnabled())
            {
                Interlocked.Increment(ref _lookupsRequested);
                Interlocked.Increment(ref _currentLookups);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    string host = GetHostnameFromStateObject(hostNameOrAddress);

                    ResolutionStart(host);
                }

                return Stopwatch.GetTimestamp();
            }

            return NameResolutionMetrics.IsEnabled() ? Stopwatch.GetTimestamp() : 0;
        }

        [NonEvent]
        public void AfterResolution(object hostNameOrAddress, long? startingTimestamp, Exception? exception = null)
        {
            Debug.Assert(startingTimestamp.HasValue);
            if (startingTimestamp == 0)
            {
                return;
            }

            TimeSpan duration = Stopwatch.GetElapsedTime(startingTimestamp.Value);

            if (IsEnabled())
            {
                Interlocked.Decrement(ref _currentLookups);

                _lookupsDuration?.WriteMetric(duration.TotalMilliseconds);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    if (exception is not null)
                    {
                        ResolutionFailed();
                    }

                    ResolutionStop();
                }
            }

            if (NameResolutionMetrics.IsEnabled())
            {
                NameResolutionMetrics.AfterResolution(duration, GetHostnameFromStateObject(hostNameOrAddress), exception);
            }
        }

        private static string GetHostnameFromStateObject(object hostNameOrAddress)
        {
            Debug.Assert(hostNameOrAddress is not null);

            string host = hostNameOrAddress switch
            {
                string h => h,
                KeyValuePair<string, AddressFamily> t => t.Key,
                IPAddress a => a.ToString(),
                KeyValuePair<IPAddress, AddressFamily> t => t.Key.ToString(),
                _ => null!
            };

            Debug.Assert(host is not null, $"Unknown hostNameOrAddress type: {hostNameOrAddress.GetType().Name}");

            return host;
        }
    }
}
