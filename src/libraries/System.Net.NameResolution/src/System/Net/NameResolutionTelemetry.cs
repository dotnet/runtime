// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Threading;

namespace System.Net
{
    internal readonly struct NameResolutionActivity
    {
        private const string ActivitySourceName = "System.Net.NameResolution";
        private const string ActivityName = ActivitySourceName + ".DsnLookup";
        private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

        // _startingTimestamp == 0 means NameResolutionTelemetry and NameResolutionMetrics are both disabled.
        private readonly long _startingTimestamp;
        private readonly Activity? _activity;

        public NameResolutionActivity(long startingTimestamp)
        {
            _startingTimestamp = startingTimestamp;
            _activity = s_activitySource.StartActivity(ActivityName, ActivityKind.Client);
        }

        public static bool IsTracingEnabled() => s_activitySource.HasListeners();

        // Returns true if either NameResolutionTelemetry or NameResolutionMetrics is enabled.
        public bool Stop(out TimeSpan duration)
        {
            _activity?.Stop();
            if (_startingTimestamp == 0)
            {
                duration = default;
                return false;
            }
            duration = Stopwatch.GetElapsedTime(_startingTimestamp);
            return true;
        }
    }

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
        public static bool AnyDiagnosticsEnabled() => Log.IsEnabled() || NameResolutionMetrics.IsEnabled() || NameResolutionActivity.IsTracingEnabled();

        [NonEvent]
        public NameResolutionActivity BeforeResolution(object hostNameOrAddress, long startingTimestamp = 0)
        {
            if (!AnyDiagnosticsEnabled())
            {
                return default;
            }

            if (IsEnabled())
            {
                Interlocked.Increment(ref _lookupsRequested);
                Interlocked.Increment(ref _currentLookups);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    string host = GetHostnameFromStateObject(hostNameOrAddress);

                    ResolutionStart(host);
                }

                return new(startingTimestamp is not 0 ? startingTimestamp : Stopwatch.GetTimestamp());
            }

            return new(startingTimestamp is not 0 ? startingTimestamp : NameResolutionMetrics.IsEnabled() ? Stopwatch.GetTimestamp() : 0);
        }

        [NonEvent]
        public void AfterResolution(object hostNameOrAddress, in NameResolutionActivity activity, Exception? exception = null)
        {
            if (!activity.Stop(out TimeSpan duration))
            {
                // We stopped the System.Diagnostics.Activity at this point and neither metrics nor EventSource is enabled.
                return;
            }

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
