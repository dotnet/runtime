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

                startingTimestamp = startingTimestamp is not 0 ? startingTimestamp : Stopwatch.GetTimestamp();
            }

            startingTimestamp = startingTimestamp is not 0 ? startingTimestamp : NameResolutionMetrics.IsEnabled() ? Stopwatch.GetTimestamp() : 0;
            return new NameResolutionActivity(hostNameOrAddress, startingTimestamp);
        }

        [NonEvent]
        public void AfterResolution(object hostNameOrAddress, in NameResolutionActivity activity, object? answer, Exception? exception = null)
        {
            if (!activity.Stop(answer, exception, out TimeSpan duration))
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

        [NonEvent]
        internal static string GetHostnameFromStateObject(object hostNameOrAddress)
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

        [NonEvent]
        internal static string GetErrorType(Exception exception) => (exception as SocketException)?.SocketErrorCode switch
        {
            SocketError.HostNotFound => "host_not_found",
            SocketError.TryAgain => "try_again",
            SocketError.AddressFamilyNotSupported => "address_family_not_supported",
            SocketError.NoRecovery => "no_recovery",

            _ => exception.GetType().FullName!
        };
    }

    /// <summary>
    /// Encapsulates the starting timestamp together with an optional Activity, to represent the name resolution span for various telemetry pillars.
    /// </summary>
    internal readonly struct NameResolutionActivity
    {
        private const string ActivitySourceName = "Experimental.System.Net.NameResolution";
        private const string ActivityName = ActivitySourceName + ".DnsLookup";
        private static readonly ActivitySource s_activitySource = new ActivitySource(ActivitySourceName);

        // _startingTimestamp == 0 means NameResolutionTelemetry and NameResolutionMetrics are both disabled.
        private readonly long _startingTimestamp;
        private readonly Activity? _activity;

        public NameResolutionActivity(object hostNameOrAddress, long startingTimestamp)
        {
            _startingTimestamp = startingTimestamp;
            _activity = s_activitySource.StartActivity(ActivityName);
            if (_activity is not null)
            {
                string host = NameResolutionTelemetry.GetHostnameFromStateObject(hostNameOrAddress);
                _activity.DisplayName = hostNameOrAddress is IPAddress ? $"DNS reverse lookup {host}" : $"DNS lookup {host}";
                if (_activity.IsAllDataRequested)
                {
                    _activity.SetTag("dns.question.name", host);
                }
            }
        }

        public static bool IsTracingEnabled() => s_activitySource.HasListeners();

        // Returns true if either NameResolutionTelemetry or NameResolutionMetrics is enabled.
        public bool Stop(object? answer, Exception? exception, out TimeSpan duration)
        {
            if (_activity is not null)
            {
                if (_activity.IsAllDataRequested)
                {
                    if (answer is not null)
                    {
                        string[]? answerValues = answer switch
                        {
                            string h => [h],
                            IPAddress[] addresses => GetStringValues(addresses),
                            IPHostEntry entry => GetStringValues(entry.AddressList),
                            _ => null
                        };

                        Debug.Assert(answerValues is not null);
                        _activity.SetTag("dns.answers", answerValues);
                    }
                    else
                    {
                        Debug.Assert(exception is not null);
                        string errorType = NameResolutionTelemetry.GetErrorType(exception);
                        _activity.SetTag("error.type", errorType);
                    }
                }

                if (exception is not null)
                {
                    _activity.SetStatus(ActivityStatusCode.Error);
                }

                _activity.Stop();
            }

            if (_startingTimestamp == 0)
            {
                duration = default;
                return false;
            }

            duration = Stopwatch.GetElapsedTime(_startingTimestamp);
            return true;

            static string[] GetStringValues(IPAddress[] addresses)
            {
                string[] result = new string[addresses.Length];
                for (int i = 0; i < addresses.Length; i++)
                {
                    result[i] = addresses[i].ToString();
                }
                return result;
            }
        }
    }
}
