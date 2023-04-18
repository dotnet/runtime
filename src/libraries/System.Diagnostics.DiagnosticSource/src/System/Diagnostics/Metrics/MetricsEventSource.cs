// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
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
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void CollectionStart(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
        {
            WriteEvent(2, sessionId, intervalStartTime, intervalEndTime);
        }

        [Event(3, Keywords = Keywords.TimeSeriesValues)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void CollectionStop(string sessionId, DateTime intervalStartTime, DateTime intervalEndTime)
        {
            WriteEvent(3, sessionId, intervalStartTime, intervalEndTime);
        }

        [Event(4, Keywords = Keywords.TimeSeriesValues, Version=1)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void CounterRateValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string rate, string value)
        {
            WriteEvent(4, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate, value);
        }

        [Event(5, Keywords = Keywords.TimeSeriesValues)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void GaugeValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string lastValue)
        {
            WriteEvent(5, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, lastValue);
        }

        [Event(6, Keywords = Keywords.TimeSeriesValues)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void HistogramValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string quantiles)
        {
            WriteEvent(6, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, quantiles);
        }

        // Sent when we begin to monitor the value of a intrument, either because new session filter arguments changed subscriptions
        // or because an instrument matching the pre-existing filter has just been created. This event precedes all *MetricPublished events
        // for the same named instrument.
        [Event(7, Keywords = Keywords.TimeSeriesValues)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void BeginInstrumentReporting(string sessionId, string meterName, string? meterVersion, string instrumentName, string instrumentType, string? unit, string? description)
        {
            WriteEvent(7, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "");
        }

        // Sent when we stop monitoring the value of a intrument, either because new session filter arguments changed subscriptions
        // or because the Meter has been disposed.
        [Event(8, Keywords = Keywords.TimeSeriesValues)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void EndInstrumentReporting(string sessionId, string meterName, string? meterVersion, string instrumentName, string instrumentType, string? unit, string? description)
        {
            WriteEvent(8, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "");
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

        [Event(11, Keywords = Keywords.InstrumentPublishing)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void InstrumentPublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string instrumentType, string? unit, string? description)
        {
            WriteEvent(11, sessionId, meterName, meterVersion ?? "", instrumentName, instrumentType, unit ?? "", description ?? "");
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
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void UpDownCounterRateValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string rate, string value)
        {
            WriteEvent(16, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate, value);
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

            public CommandHandler(MetricsEventSource parent)
            {
                Parent = parent;
            }

            public MetricsEventSource Parent { get; private set;}

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
                    if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable ||
                        command.Command == EventCommand.Enable)
                    {
                        if (_aggregationManager != null)
                        {
                            if (command.Command == EventCommand.Enable || command.Command == EventCommand.Update)
                            {
                                // trying to add more sessions is not supported
                                // EventSource doesn't provide an API that allows us to enumerate the listeners'
                                // filter arguments independently or to easily track them ourselves. For example
                                // removing a listener still shows up as EventCommand.Enable as long as at least
                                // one other listener is active. In the future we might be able to figure out how
                                // to infer the changes from the info we do have or add a better API but for now
                                // I am taking the simple route  and not supporting it.
                                Parent.MultipleSessionsNotSupportedError(_sessionId);
                                return;
                            }

                            _aggregationManager.Dispose();
                            _aggregationManager = null;
                            Parent.Message($"Previous session with id {_sessionId} is stopped");
                        }
                        _sessionId = "";
                    }
                    if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) &&
                        command.Arguments != null)
                    {
                        if (command.Arguments!.TryGetValue("SessionId", out string? id))
                        {
                            _sessionId = id!;
                            Parent.Message($"SessionId argument received: {_sessionId}");
                        }
                        else
                        {
                            _sessionId = System.Guid.NewGuid().ToString();
                            Parent.Message($"New session started. SessionId auto-generated: {_sessionId}");
                        }


                        double defaultIntervalSecs = 1;
                        Debug.Assert(AggregationManager.MinCollectionTimeSecs <= defaultIntervalSecs);
                        double refreshIntervalSecs;
                        if (command.Arguments!.TryGetValue("RefreshInterval", out string? refreshInterval))
                        {
                            Parent.Message($"RefreshInterval argument received: {refreshInterval}");
                            if (!double.TryParse(refreshInterval, out refreshIntervalSecs))
                            {
                                Parent.Message($"Failed to parse RefreshInterval. Using default {defaultIntervalSecs}s.");
                                refreshIntervalSecs = defaultIntervalSecs;
                            }
                            else if (refreshIntervalSecs < AggregationManager.MinCollectionTimeSecs)
                            {
                                Parent.Message($"RefreshInterval too small. Using minimum interval {AggregationManager.MinCollectionTimeSecs} seconds.");
                                refreshIntervalSecs = AggregationManager.MinCollectionTimeSecs;
                            }
                        }
                        else
                        {
                            Parent.Message($"No RefreshInterval argument received. Using default {defaultIntervalSecs}s.");
                            refreshIntervalSecs = defaultIntervalSecs;
                        }

                        int defaultMaxTimeSeries = 1000;
                        int maxTimeSeries;
                        if (command.Arguments!.TryGetValue("MaxTimeSeries", out string? maxTimeSeriesString))
                        {
                            Parent.Message($"MaxTimeSeries argument received: {maxTimeSeriesString}");
                            if (!int.TryParse(maxTimeSeriesString, out maxTimeSeries))
                            {
                                Parent.Message($"Failed to parse MaxTimeSeries. Using default {defaultMaxTimeSeries}");
                                maxTimeSeries = defaultMaxTimeSeries;
                            }
                        }
                        else
                        {
                            Parent.Message($"No MaxTimeSeries argument received. Using default {defaultMaxTimeSeries}");
                            maxTimeSeries = defaultMaxTimeSeries;
                        }

                        int defaultMaxHistograms = 20;
                        int maxHistograms;
                        if (command.Arguments!.TryGetValue("MaxHistograms", out string? maxHistogramsString))
                        {
                            Parent.Message($"MaxHistograms argument received: {maxHistogramsString}");
                            if (!int.TryParse(maxHistogramsString, out maxHistograms))
                            {
                                Parent.Message($"Failed to parse MaxHistograms. Using default {defaultMaxHistograms}");
                                maxHistograms = defaultMaxHistograms;
                            }
                        }
                        else
                        {
                            Parent.Message($"No MaxHistogram argument received. Using default {defaultMaxHistograms}");
                            maxHistograms = defaultMaxHistograms;
                        }

                        string sessionId = _sessionId;
                        _aggregationManager = new AggregationManager(
                            maxTimeSeries,
                            maxHistograms,
                            (i, s) => TransmitMetricValue(i, s, sessionId),
                            (startIntervalTime, endIntervalTime) => Parent.CollectionStart(sessionId, startIntervalTime, endIntervalTime),
                            (startIntervalTime, endIntervalTime) => Parent.CollectionStop(sessionId, startIntervalTime, endIntervalTime),
                            i => Parent.BeginInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description),
                            i => Parent.EndInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description),
                            i => Parent.InstrumentPublished(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description),
                            () => Parent.InitialInstrumentEnumerationComplete(sessionId),
                            e => Parent.Error(sessionId, e.ToString()),
                            () => Parent.TimeSeriesLimitReached(sessionId),
                            () => Parent.HistogramLimitReached(sessionId),
                            e => Parent.ObservableInstrumentCallbackError(sessionId, e.ToString()));

                        _aggregationManager.SetCollectionPeriod(TimeSpan.FromSeconds(refreshIntervalSecs));

                        if (command.Arguments!.TryGetValue("Metrics", out string? metricsSpecs))
                        {
                            Parent.Message($"Metrics argument received: {metricsSpecs}");
                            ParseSpecs(metricsSpecs);
                        }
                        else
                        {
                            Parent.Message("No Metrics argument received");
                        }

                        _aggregationManager.Start();
                    }
                }
                catch (Exception e) when (LogError(e))
                {
                    // this will never run
                }
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
                    Log.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), FormatQuantiles(histogramStats.Quantiles));
                }
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
                    sb.Append(quantiles[i].Quantile).Append('=').Append(quantiles[i].Value);
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
