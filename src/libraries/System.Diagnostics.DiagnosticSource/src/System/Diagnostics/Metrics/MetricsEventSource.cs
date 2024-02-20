// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// This EventSource is intended to let out-of-process tools (such as dotnet-counters) do
    /// ad-hoc monitoring for the new Instrument APIs. This source only supports one listener
    /// at a time. Each new listener will overwrite the configuration about which metrics
    /// are being collected and the time interval for the collection. In the future it would
    /// be nice to have support for multiple concurrent out-of-proc tools but EventSource's
    /// handling of filter arguments doesn't make that easy right now.
    ///
    /// Configuration - The EventSource accepts the following filter arguments:
    ///   - SessionId - An arbitrary opaque string that will be sent back to the listener in
    ///   many event payloads. If listener B reconfigures the EventSource while listener A
    ///   is still running it is possible that each of them will observe some of the events
    ///   that were generated using the other's requested configuration. Filtering on sessionId
    ///   allows each listener to ignore those events.
    ///   - RefreshInterval - The frequency in seconds for sending the metric time series data.
    ///   The format is anything parsable using double.TryParse(). Any
    ///   value less than AggregationManager.MinCollectionTimeSecs (currently 0.1 sec) is rounded
    ///   up to the minimum. If not specified the default interval is 1 second.
    ///   - Metrics - A semicolon separated list. Each item in the list is either the name of a
    ///   Meter or 'meter_name\instrument_name'. For example "Foo;System.Runtime\gc-gen0-size"
    ///   would include all instruments in the 'Foo' meter and the single 'gc-gen0-size' instrument
    ///   in the 'System.Runtime' meter.
    ///   - MaxTimeSeries - An integer that sets an upper bound on the number of time series
    ///   this event source will track. Because instruments can have unbounded sets of tags
    ///   even specifying a single metric could create unbounded load without this limit.
    ///   - MaxHistograms - An integer that sets an upper bound on the number of histograms
    ///   this event source will track. This allows setting a tighter bound on histograms
    ///   than time series in general given that histograms use considerably more memory.
    /// </summary>
    [EventSource(Name = "System.Diagnostics.Metrics")]
    internal sealed class MetricsEventSource : EventSource
    {
        public static readonly MetricsEventSource Log = new();

        private const string SharedSessionId = "SHARED";
        private const string ClientIdKey = "ClientId";
        private const string MaxHistogramsKey = "MaxHistograms";
        private const string MaxTimeSeriesKey = "MaxTimeSeries";
        private const string RefreshIntervalKey = "RefreshInterval";
        private const string DefaultValueDescription = "default";
        private const string SharedValueDescription = "shared value";

        public static class Keywords
        {
            /// <summary>
            /// Indicates diagnostics messages from MetricsEventSource should be included.
            /// </summary>
            public const EventKeywords Messages = (EventKeywords)0x1;
            /// <summary>
            /// Indicates that all the time series data points should be included
            /// </summary>
            public const EventKeywords TimeSeriesValues = (EventKeywords)0x2;
            /// <summary>
            /// Indicates that instrument published notifications should be included
            /// </summary>
            public const EventKeywords InstrumentPublishing = (EventKeywords)0x4;
        }

        private CommandHandler? _handler;

        private CommandHandler Handler
        {
            get
            {
                if (_handler == null)
                {
                    Interlocked.CompareExchange(ref _handler, new CommandHandler(this), null);
                }
                return _handler;
            }
        }

        private MetricsEventSource() { }

        /// <summary>
        /// Used to send ad-hoc diagnostics to humans.
        /// </summary>
        [Event(1, Keywords = Keywords.Messages)]
        public void Message(string? Message)
        {
            WriteEvent(1, Message);
        }

        [Event(2, Keywords = Keywords.TimeSeriesValues)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void CollectionStart(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
        {
            WriteEvent(2, sessionId, intervalStartTime, intervalEndTime);
        }

        [Event(3, Keywords = Keywords.TimeSeriesValues)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void CollectionStop(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
        {
            WriteEvent(3, sessionId, intervalStartTime, intervalEndTime);
        }

        [Event(4, Keywords = Keywords.TimeSeriesValues, Version=1)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void CounterRateValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string rate, string value)
        {
            WriteEvent(4, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate, value);
        }

        [Event(5, Keywords = Keywords.TimeSeriesValues)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void GaugeValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string lastValue)
        {
            WriteEvent(5, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, lastValue);
        }

        [Event(6, Keywords = Keywords.TimeSeriesValues, Version=1)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void HistogramValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string quantiles, int count, double sum)
        {
            WriteEvent(6, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, quantiles, count, sum);
        }

        // Sent when we begin to monitor the value of a instrument, either because new session filter arguments changed subscriptions
        // or because an instrument matching the pre-existing filter has just been created. This event precedes all *MetricPublished events
        // for the same named instrument.
        [Event(7, Keywords = Keywords.TimeSeriesValues, Version = 1)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void BeginInstrumentReporting(
                        string sessionId,
                        string meterName,
                        string? meterVersion,
                        string instrumentName,
                        string instrumentType,
                        string? unit,
                        string? description,
                        string instrumentTags,
                        string meterTags,
                        string meterScopeHash)
        {
            WriteEvent(7, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "",
                    instrumentTags, meterTags, meterScopeHash);
        }

        // Sent when we stop monitoring the value of a instrument, either because new session filter arguments changed subscriptions
        // or because the Meter has been disposed.
        [Event(8, Keywords = Keywords.TimeSeriesValues, Version = 1)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void EndInstrumentReporting(
                        string sessionId,
                        string meterName,
                        string? meterVersion,
                        string instrumentName,
                        string instrumentType,
                        string? unit,
                        string? description,
                        string instrumentTags,
                        string meterTags,
                        string meterScopeHash)
        {
            WriteEvent(8, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "",
                    instrumentTags, meterTags, meterScopeHash);
        }

        [Event(9, Keywords = Keywords.TimeSeriesValues | Keywords.Messages | Keywords.InstrumentPublishing)]
        public void Error(string sessionId, string errorMessage)
        {
            WriteEvent(9, sessionId, errorMessage);
        }

        [Event(10, Keywords = Keywords.TimeSeriesValues | Keywords.InstrumentPublishing)]
        public void InitialInstrumentEnumerationComplete(string sessionId)
        {
            WriteEvent(10, sessionId);
        }

        [Event(11, Keywords = Keywords.InstrumentPublishing, Version = 1)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void InstrumentPublished(
                        string sessionId,
                        string meterName,
                        string? meterVersion,
                        string instrumentName,
                        string instrumentType,
                        string? unit,
                        string? description,
                        string instrumentTags,
                        string meterTags,
                        string meterScopeHash)
        {
            WriteEvent(11, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "",
                    instrumentTags, meterTags, meterScopeHash);
        }

        [Event(12, Keywords = Keywords.TimeSeriesValues)]
        public void TimeSeriesLimitReached(string sessionId)
        {
            WriteEvent(12, sessionId);
        }

        [Event(13, Keywords = Keywords.TimeSeriesValues)]
        public void HistogramLimitReached(string sessionId)
        {
            WriteEvent(13, sessionId);
        }

        [Event(14, Keywords = Keywords.TimeSeriesValues)]
        public void ObservableInstrumentCallbackError(string sessionId, string errorMessage)
        {
            WriteEvent(14, sessionId, errorMessage);
        }

        [Event(15, Keywords = Keywords.TimeSeriesValues | Keywords.Messages | Keywords.InstrumentPublishing)]
        public void MultipleSessionsNotSupportedError(string runningSessionId)
        {
            WriteEvent(15, runningSessionId);
        }

        [Event(16, Keywords = Keywords.TimeSeriesValues, Version=1)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                      Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void UpDownCounterRateValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string rate, string value)
        {
            WriteEvent(16, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate, value);
        }

        [Event(17, Keywords = Keywords.TimeSeriesValues)]
#if !NET8_0_OR_GREATER
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
#endif
        public void MultipleSessionsConfiguredIncorrectlyError(string clientId, string expectedMaxHistograms, string actualMaxHistograms, string expectedMaxTimeSeries, string actualMaxTimeSeries, string expectedRefreshInterval, string actualRefreshInterval)
        {
            WriteEvent(17, clientId, expectedMaxHistograms, actualMaxHistograms, expectedMaxTimeSeries, actualMaxTimeSeries, expectedRefreshInterval, actualRefreshInterval);
        }

        /// <summary>
        /// Called when the EventSource gets a command from a EventListener or ETW.
        /// </summary>
        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            lock (this)
            {
                Handler.OnEventCommand(command);
            }
        }

        // EventSource assumes that every method defined on it represents an event.
        // Methods that are declared explicitly can use the [NonEvent] attribute to opt-out but
        // lambdas can't. Putting all the command handling logic in this nested class
        // is a simpler way to opt everything out in bulk.
        private sealed class CommandHandler
        {
            private AggregationManager? _aggregationManager;
            private string _sessionId = "";
            private HashSet<string> _sharedSessionClientIds = new HashSet<string>();
            private int _sharedSessionRefCount;
            private bool _disabledRefCount;

            public CommandHandler(MetricsEventSource parent)
            {
                Parent = parent;
            }

            public MetricsEventSource Parent { get; private set;}

            public bool IsSharedSession(string commandSessionId)
            {
                // commandSessionId may be null if it's the disable command
                return _sessionId.Equals(SharedSessionId) && (string.IsNullOrEmpty(commandSessionId) || commandSessionId.Equals(SharedSessionId));
            }

            public void OnEventCommand(EventCommandEventArgs command)
            {
                try
                {
#if OS_ISBROWSER_SUPPORT
                    if (OperatingSystem.IsBrowser())
                    {
                        // AggregationManager uses a dedicated thread to avoid losing data for apps experiencing threadpool starvation
                        // and browser doesn't support Thread.Start()
                        //
                        // This limitation shouldn't really matter because browser also doesn't support out-of-proc EventSource communication
                        // which is the intended scenario for this EventSource. If it matters in the future AggregationManager can be
                        // modified to have some other fallback path that works for browser.
                        Parent.Error("", "System.Diagnostics.Metrics EventSource not supported on browser");
                        return;
                    }
#endif

                    string commandSessionId = GetSessionId(command);

                    if ((command.Command == EventCommand.Update
                        || command.Command == EventCommand.Disable
                        || command.Command == EventCommand.Enable)
                        && _aggregationManager != null)
                    {
                        if (command.Command == EventCommand.Update
                            || command.Command == EventCommand.Enable)
                        {
                            IncrementRefCount(commandSessionId, command);
                        }

                        if (IsSharedSession(commandSessionId))
                        {
                            if (ShouldDisable(command.Command))
                            {
                                Parent.Message($"Previous session with id {_sessionId} is stopped");
                                _aggregationManager.Dispose();
                                _aggregationManager = null;
                                _sessionId = string.Empty;
                                _sharedSessionClientIds.Clear();
                                return;
                            }

                            bool validShared = true;

                            double refreshInterval;
                            lock (_aggregationManager)
                            {
                                validShared = SetSharedRefreshIntervalSecs(command.Arguments!, _aggregationManager.CollectionPeriod.TotalSeconds, out refreshInterval) ? validShared : false;
                            }

                            validShared = SetSharedMaxHistograms(command.Arguments!, _aggregationManager.MaxHistograms, out int maxHistograms) ? validShared : false;
                            validShared = SetSharedMaxTimeSeries(command.Arguments!, _aggregationManager.MaxTimeSeries, out int maxTimeSeries) ? validShared : false;

                            if (command.Command != EventCommand.Disable)
                            {
                                if (validShared)
                                {
                                    if (ParseMetrics(command.Arguments!, out string? metricsSpecs))
                                    {
                                        ParseSpecs(metricsSpecs);
                                        _aggregationManager.Update();
                                    }

                                    return;
                                }
                                else
                                {
                                    // If the clientId protocol is not followed, we can't tell which session is configured incorrectly
                                    if (command.Arguments!.TryGetValue(ClientIdKey, out string? clientId))
                                    {
                                        lock (_aggregationManager)
                                        {
                                            // Use ClientId to identify the session that is not configured correctly (since the sessionId is just SHARED)
                                            Parent.MultipleSessionsConfiguredIncorrectlyError(clientId!, _aggregationManager.MaxHistograms.ToString(), maxHistograms.ToString(), _aggregationManager.MaxTimeSeries.ToString(), maxTimeSeries.ToString(), _aggregationManager.CollectionPeriod.TotalSeconds.ToString(), refreshInterval.ToString());
                                        }
                                    }

                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (command.Command == EventCommand.Enable || command.Command == EventCommand.Update)
                            {
                                // trying to add more sessions is not supported for unshared sessions
                                // EventSource doesn't provide an API that allows us to enumerate the listeners'
                                // filter arguments independently or to easily track them ourselves. For example
                                // removing a listener still shows up as EventCommand.Enable as long as at least
                                // one other listener is active. In the future we might be able to figure out how
                                // to infer the changes from the info we do have or add a better API but for now
                                // I am taking the simple route  and not supporting it.
                                Parent.MultipleSessionsNotSupportedError(_sessionId);
                                return;
                            }
                            else if (ShouldDisable(command.Command))
                            {
                                Parent.Message($"Previous session with id {_sessionId} is stopped");
                                _aggregationManager.Dispose();
                                _aggregationManager = null;
                                _sessionId = string.Empty;
                                _sharedSessionClientIds.Clear();
                                return;
                            }
                        }
                    }
                    if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) &&
                        command.Arguments != null)
                    {
                        IncrementRefCount(commandSessionId, command);

                        _sessionId = commandSessionId;

                        double defaultIntervalSecs = 1;
                        Debug.Assert(AggregationManager.MinCollectionTimeSecs <= defaultIntervalSecs);
                        SetRefreshIntervalSecs(command.Arguments!, AggregationManager.MinCollectionTimeSecs, defaultIntervalSecs, out double refreshIntervalSecs);

                        const int defaultMaxTimeSeries = 1000;
                        SetUniqueMaxTimeSeries(command.Arguments!, defaultMaxTimeSeries, out int maxTimeSeries);

                        const int defaultMaxHistograms = 20;
                        SetUniqueMaxHistograms(command.Arguments!, defaultMaxHistograms, out int maxHistograms);

                        string sessionId = _sessionId;
                        _aggregationManager = new AggregationManager(
                            maxTimeSeries,
                            maxHistograms,
                            (i, s) => TransmitMetricValue(i, s, sessionId),
                            (startIntervalTime, endIntervalTime) => Parent.CollectionStart(sessionId, startIntervalTime, endIntervalTime),
                            (startIntervalTime, endIntervalTime) => Parent.CollectionStop(sessionId, startIntervalTime, endIntervalTime),
                            i => Parent.BeginInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description,
                                    FormatTags(i.Tags), FormatTags(i.Meter.Tags), FormatScopeHash(i.Meter.Scope)),
                            i => Parent.EndInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description,
                                    FormatTags(i.Tags), FormatTags(i.Meter.Tags), FormatScopeHash(i.Meter.Scope)),
                            i => Parent.InstrumentPublished(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description,
                                    FormatTags(i.Tags), FormatTags(i.Meter.Tags), FormatScopeHash(i.Meter.Scope)),
                            () => Parent.InitialInstrumentEnumerationComplete(sessionId),
                            e => Parent.Error(sessionId, e.ToString()),
                            () => Parent.TimeSeriesLimitReached(sessionId),
                            () => Parent.HistogramLimitReached(sessionId),
                            e => Parent.ObservableInstrumentCallbackError(sessionId, e.ToString()));

                        _aggregationManager.SetCollectionPeriod(TimeSpan.FromSeconds(refreshIntervalSecs));

                        if (ParseMetrics(command.Arguments!, out string? metricsSpecs))
                        {
                            ParseSpecs(metricsSpecs);
                        }

                        _aggregationManager.Start();
                    }
                }
                catch (Exception e) when (LogError(e))
                {
                    // this will never run
                }
            }

            private bool ShouldDisable(EventCommand command)
            {
                return command == EventCommand.Disable
                    && ((!_disabledRefCount && Interlocked.Decrement(ref _sharedSessionRefCount) == 0)
                    || !Parent.IsEnabled());
            }

            private bool ParseMetrics(IDictionary<string, string> arguments, out string? metricsSpecs)
            {
                if (arguments.TryGetValue("Metrics", out metricsSpecs))
                {
                    Parent.Message($"Metrics argument received: {metricsSpecs}");
                    return true;
                }

                Parent.Message("No Metrics argument received");
                return false;
            }

            private void InvalidateRefCounting()
            {
                _disabledRefCount = true;
                Parent.Message($"{ClientIdKey} not provided; session will remain active indefinitely.");
            }

            private void IncrementRefCount(string clientId, EventCommandEventArgs command)
            {
                // When creating a SHARED session (i.e. sessionId == SharedSessionId), a randomly-generated clientId
                // should be provided as part of the command arguments. If not, we can't tell which session is
                // configured incorrectly, and ref-counting will be disabled since there is no way to keep track of
                // multiple Enables coming from the same client. This will cause the session to remain active indefinitely.
                if (clientId.Equals(SharedSessionId))
                {
                    if (command.Arguments!.TryGetValue(ClientIdKey, out string? clientIdArg) && !string.IsNullOrEmpty(clientIdArg))
                    {
                        clientId = clientIdArg!;
                    }
                    else
                    {
                        // If ClientId contract is followed, this should never happen.
                        InvalidateRefCounting();
                    }
                }

                if (_sharedSessionClientIds.Add(clientId))
                {
                    Interlocked.Increment(ref _sharedSessionRefCount);
                }
            }

            private bool SetSharedMaxTimeSeries(IDictionary<string, string> arguments, int sharedValue, out int maxTimeSeries)
            {
                return SetMaxValue(arguments, MaxTimeSeriesKey, SharedValueDescription, sharedValue, out maxTimeSeries);
            }

            private void SetUniqueMaxTimeSeries(IDictionary<string, string> arguments, int defaultValue, out int maxTimeSeries)
            {
                _ = SetMaxValue(arguments, MaxTimeSeriesKey, DefaultValueDescription, defaultValue, out maxTimeSeries);
            }

            private bool SetSharedMaxHistograms(IDictionary<string, string> arguments, int sharedValue, out int maxHistograms)
            {
                return SetMaxValue(arguments, MaxHistogramsKey, SharedValueDescription, sharedValue, out maxHistograms);
            }

            private void SetUniqueMaxHistograms(IDictionary<string, string> arguments, int defaultValue, out int maxHistograms)
            {
                _ = SetMaxValue(arguments, MaxHistogramsKey, DefaultValueDescription, defaultValue, out maxHistograms);
            }

            private bool SetMaxValue(IDictionary<string, string> arguments, string argumentsKey, string valueDescriptor, int defaultValue, out int maxValue)
            {
                if (arguments.TryGetValue(argumentsKey, out string? maxString))
                {
                    Parent.Message($"{argumentsKey} argument received: {maxString}");
                    if (!int.TryParse(maxString, out maxValue))
                    {
                        Parent.Message($"Failed to parse {argumentsKey}. Using {valueDescriptor} {defaultValue}");
                        maxValue = defaultValue;
                    }
                    else if (maxValue != defaultValue)
                    {
                        // This is only relevant for shared sessions, where the "default" (provided) value is what is being
                        // used by the existing session.
                        return false;
                    }
                }
                else
                {
                    Parent.Message($"No {argumentsKey} argument received. Using {valueDescriptor} {defaultValue}");
                    maxValue = defaultValue;
                }

                return true;
            }

            private void SetRefreshIntervalSecs(IDictionary<string, string> arguments, double minValue, double defaultValue, out double refreshIntervalSeconds)
            {
                if (GetRefreshIntervalSecs(arguments, DefaultValueDescription, defaultValue, out refreshIntervalSeconds)
                    && refreshIntervalSeconds < minValue)
                {
                    Parent.Message($"{RefreshIntervalKey} too small. Using minimum interval {minValue} seconds.");
                    refreshIntervalSeconds = minValue;
                }
            }

            private bool SetSharedRefreshIntervalSecs(IDictionary<string, string> arguments, double sharedValue, out double refreshIntervalSeconds)
            {
                if (GetRefreshIntervalSecs(arguments, SharedValueDescription, sharedValue, out refreshIntervalSeconds)
                    && refreshIntervalSeconds != sharedValue)
                {
                    return false;
                }

                return true;
            }

            private bool GetRefreshIntervalSecs(IDictionary<string, string> arguments, string valueDescriptor, double defaultValue, out double refreshIntervalSeconds)
            {
                if (arguments!.TryGetValue(RefreshIntervalKey, out string? refreshInterval))
                {
                    Parent.Message($"{RefreshIntervalKey} argument received: {refreshInterval}");
                    if (!double.TryParse(refreshInterval, out refreshIntervalSeconds))
                    {
                        Parent.Message($"Failed to parse {RefreshIntervalKey}. Using {valueDescriptor} {defaultValue}s.");
                        refreshIntervalSeconds = defaultValue;
                        return false;
                    }
                }
                else
                {
                    Parent.Message($"No {RefreshIntervalKey} argument received. Using {valueDescriptor} {defaultValue}s.");
                    refreshIntervalSeconds = defaultValue;
                    return false;
                }

                return true;
            }

            private string GetSessionId(EventCommandEventArgs command)
            {
                if (command.Arguments!.TryGetValue("SessionId", out string? id))
                {
                    Parent.Message($"SessionId argument received: {id!}");
                    return id!;
                }

                string sessionId = string.Empty;

                if (command.Command != EventCommand.Disable)
                {
                    sessionId = Guid.NewGuid().ToString();
                    Parent.Message($"New session started. SessionId auto-generated: {sessionId}");
                }

                return sessionId;
            }

            private bool LogError(Exception e)
            {
                Parent.Error(_sessionId, e.ToString());
                // this code runs as an exception filter
                // returning false ensures the catch handler isn't run
                return false;
            }

            private static readonly char[] s_instrumentSeparators = new char[] { '\r', '\n', ',', ';' };

            [UnsupportedOSPlatform("browser")]
            private void ParseSpecs(string? metricsSpecs)
            {
                if (metricsSpecs == null)
                {
                    return;
                }

                string[] specStrings = metricsSpecs.Split(s_instrumentSeparators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string specString in specStrings)
                {
                    MetricSpec spec = MetricSpec.Parse(specString);
                    Parent.Message($"Parsed metric: {spec}");
                    if (spec.InstrumentName != null)
                    {
                        _aggregationManager!.Include(spec.MeterName, spec.InstrumentName);
                    }
                    else
                    {
                        _aggregationManager!.Include(spec.MeterName);
                    }
                }
            }

            private static void TransmitMetricValue(Instrument instrument, LabeledAggregationStatistics stats, string sessionId)
            {
                if (stats.AggregationStatistics is CounterStatistics rateStats)
                {
                    if (rateStats.IsMonotonic)
                    {
                        Log.CounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
                            rateStats.Delta.HasValue ? rateStats.Delta.Value.ToString(CultureInfo.InvariantCulture) : "",
                            rateStats.Value.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        Log.UpDownCounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
                            rateStats.Delta.HasValue ? rateStats.Delta.Value.ToString(CultureInfo.InvariantCulture) : "",
                            rateStats.Value.ToString(CultureInfo.InvariantCulture));
                    }
                }
                else if (stats.AggregationStatistics is LastValueStatistics lastValueStats)
                {
                    Log.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
                        lastValueStats.LastValue.HasValue ? lastValueStats.LastValue.Value.ToString(CultureInfo.InvariantCulture) : "");
                }
                else if (stats.AggregationStatistics is HistogramStatistics histogramStats)
                {
                    Log.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), FormatQuantiles(histogramStats.Quantiles), histogramStats.Count, histogramStats.Sum);
                }
            }

            private static string FormatScopeHash(object? scope) =>
                scope is null ? string.Empty : RuntimeHelpers.GetHashCode(scope).ToString(CultureInfo.InvariantCulture);

            private static string FormatTags(IEnumerable<KeyValuePair<string, object?>>? tags)
            {
                if (tags is null)
                {
                    return string.Empty;
                }

                StringBuilder sb = new StringBuilder();
                bool first = true;
                foreach (KeyValuePair<string, object?> tag in tags)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append(tag.Key).Append('=');

                    if (tag.Value is not null)
                    {
                        sb.Append(tag.Value.ToString());
                    }
                }
                return sb.ToString();
            }

            private static string FormatTags(KeyValuePair<string, string>[] labels)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < labels.Length; i++)
                {
                    sb.Append(labels[i].Key).Append('=').Append(labels[i].Value);
                    if (i != labels.Length - 1)
                    {
                        sb.Append(',');
                    }
                }
                return sb.ToString();
            }

            private static string FormatQuantiles(QuantileValue[] quantiles)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < quantiles.Length; i++)
                {
#if NETCOREAPP
                    sb.Append(CultureInfo.InvariantCulture, $"{quantiles[i].Quantile}={quantiles[i].Value}");
#else
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", quantiles[i].Quantile, quantiles[i].Value);
#endif
                    if (i != quantiles.Length - 1)
                    {
                        sb.Append(';');
                    }
                }
                return sb.ToString();
            }
        }

        private sealed class MetricSpec
        {
            private const char MeterInstrumentSeparator = '\\';
            public string MeterName { get; private set; }
            public string? InstrumentName { get; private set; }

            public MetricSpec(string meterName, string? instrumentName)
            {
                MeterName = meterName;
                InstrumentName = instrumentName;
            }

            public static MetricSpec Parse(string text)
            {
                int slashIdx = text.IndexOf(MeterInstrumentSeparator);
                if (slashIdx < 0)
                {
                    return new MetricSpec(text.Trim(), null);
                }
                else
                {
                    string meterName = text.AsSpan(0, slashIdx).Trim().ToString();
                    string? instrumentName = text.AsSpan(slashIdx + 1).Trim().ToString();
                    return new MetricSpec(meterName, instrumentName);
                }
            }

            public override string ToString() => InstrumentName != null ?
                MeterName + MeterInstrumentSeparator + InstrumentName :
                MeterName;
        }
    }
}
