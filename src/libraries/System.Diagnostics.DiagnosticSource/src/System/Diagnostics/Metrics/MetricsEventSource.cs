// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;

namespace System.Diagnostics.Metrics
{
    [EventSource(Name = "System.Diagnostics.Metrics")]
    internal class MetricsEventSource : EventSource
    {
        public static readonly MetricsEventSource Logger = new();

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

        private CommandHandler _handler;

        private MetricsEventSource()
        {
            _handler = new CommandHandler();
        }

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
        public void CollectionStart(string sessionId, DateTime intervalStartTime, double collectionIntervalSecs)
        {
            WriteEvent(2, sessionId, intervalStartTime, collectionIntervalSecs);
        }

        [Event(3, Keywords = Keywords.TimeSeriesValues)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void CollectionStop(string sessionId, DateTime intervalStartTime, double collectionIntervalSecs)
        {
            WriteEvent(3, sessionId, intervalStartTime, collectionIntervalSecs);
        }

        [Event(4, Keywords = Keywords.TimeSeriesValues)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "This calls WriteEvent with all primitive arguments which is safe. Primitives are always serialized properly.")]
        public void CounterRateValuePublished(string sessionId, string meterName, string? meterVersion, string instrumentName, string? unit, string tags, string rate)
        {
            WriteEvent(4, sessionId, meterName, meterVersion ?? "", instrumentName, unit ?? "", tags, rate);
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

        [Event(9, Keywords = Keywords.TimeSeriesValues)]
        public void CollectionError(string sessionId, string errorMessage, string errorStack)
        {
            WriteEvent(9, sessionId, errorMessage, errorStack);
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

        /// <summary>
        /// Called when the EventSource gets a command from a EventListener or ETW.
        /// </summary>
        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            lock (this)
            {
                _handler.OnEventCommand(command);
            }
        }

        // EventSource assumes that every method defined on it represents an event.
        // Methods that are declared explicitly can use the [NonEvent] attribute to opt-out but
        // lambdas can't. Putting all the command handling logic in this nested class
        // is a simpler way to opt everything out in bulk.
        private class CommandHandler
        {
            private AggregationManager? _aggregationManager;
            private string _sessionId = "";

            public void OnEventCommand(EventCommandEventArgs command)
            {
#if OS_SUPPORT_ATTRTIBUTES
                if (OperatingSystem.IsBrowser())
                {
                    // AggregationManager uses a dedicated thread to avoid losing data for apps experiencing threadpool starvation
                    // and browser doesn't support Thread.Start()
                    //
                    // This limitation shouldn't really matter because browser also doesn't support out-of-proc EventSource communication
                    // which is the intended scenario for this EventSource. If it matters in the future AggregationManager can be
                    // modified to have some other fallback path that works for browser.
                    Logger.Message("System.Diagnostics.Metrics EventSource not supported on browser");
                    return;
                }
#endif
                if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable ||
                    command.Command == EventCommand.Enable)
                {
                    if (_aggregationManager != null)
                    {
                        _aggregationManager.Dispose();
                        _aggregationManager = null;
                        Logger.Message($"Previous session with id {_sessionId} is stopped");
                    }
                    _sessionId = "";
                }
                if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) &&
                    command.Arguments != null)
                {
                    if (command.Arguments!.TryGetValue("SessionId", out string? id))
                    {
                        _sessionId = id!;
                        Logger.Message($"SessionId argument received: {_sessionId}");
                    }
                    else
                    {
                        _sessionId = System.Guid.NewGuid().ToString();
                        Logger.Message($"New session started. SessionId auto-generated: {_sessionId}");
                    }


                    double defaultIntervalSecs = 1;
                    double refreshIntervalSecs = defaultIntervalSecs;
                    if (command.Arguments!.TryGetValue("RefreshInterval", out string? refreshInterval))
                    {
                        Logger.Message("RefreshInterval filter argument received: " + refreshInterval);
                        if (!double.TryParse(refreshInterval, out refreshIntervalSecs))
                        {
                            Logger.Message($"Failed to parse RefreshInterval. Using default {defaultIntervalSecs}s.");
                            refreshIntervalSecs = defaultIntervalSecs;
                        }
                        else if (refreshIntervalSecs < AggregationManager.MinCollectionTime)
                        {
                            Logger.Message($"RefreshInterval too small. Using minimum interval {AggregationManager.MinCollectionTime} seconds.");
                            refreshIntervalSecs = AggregationManager.MinCollectionTime;
                        }
                    }
                    else
                    {
                        Logger.Message($"No RefreshInterval filter argument received. Using default {defaultIntervalSecs}s.");
                        refreshIntervalSecs = defaultIntervalSecs;
                    }


                    string sessionId = _sessionId;
                    _aggregationManager = new AggregationManager(
                        (i, s) => TransmitMetricValue(i, s, sessionId),
                        startIntervalTime => Logger.CollectionStart(sessionId, startIntervalTime, refreshIntervalSecs),
                        startIntervalTime => Logger.CollectionStop(sessionId, startIntervalTime, refreshIntervalSecs),
                        i => Logger.BeginInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description),
                        i => Logger.EndInstrumentReporting(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description),
                        i => Logger.InstrumentPublished(sessionId, i.Meter.Name, i.Meter.Version, i.Name, i.GetType().Name, i.Unit, i.Description),
                        () => Logger.InitialInstrumentEnumerationComplete(sessionId),
                        e => Logger.CollectionError(sessionId, e.Message, e.StackTrace?.ToString() ?? ""));

                    _aggregationManager.SetCollectionPeriod(TimeSpan.FromSeconds(refreshIntervalSecs));

                    if (command.Arguments!.TryGetValue("Metrics", out string? metricsSpecs))
                    {
                        Logger.Message("Metrics filter argument received: " + metricsSpecs);
                        ParseSpecs(metricsSpecs);
                    }
                    else
                    {
                        Logger.Message("No Metrics filter argument received");
                    }

                    _aggregationManager.Start();
                }
            }

            private static char[] s_instrumentSeperators = new char[] { '\r', '\n', ',', ';' };

#if OS_SUPPORT_ATTRTIBUTES
            [UnsupportedOSPlatform("browser")]
#endif
            private void ParseSpecs(string? metricsSpecs)
            {
                if (metricsSpecs == null)
                {
                    return;
                }
                string[] specStrings = metricsSpecs.Split(s_instrumentSeperators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string specString in specStrings)
                {
                    if (!MetricSpec.TryParse(specString, out MetricSpec spec))
                    {
                        Logger.Message("Failed to parse metric spec: " + specString);
                    }
                    else
                    {
                        Logger.Message("Parsed metric: " + spec.ToString());
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
            }

            private void TransmitMetricValue(Instrument instrument, LabeledAggregationStatistics stats, string sessionId)
            {
                if (stats.AggregationStatistics is RateStatistics rateStats)
                {
                    Logger.CounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
                        rateStats.Delta.HasValue ? rateStats.Delta.Value.ToString() : "");
                }
                else if (stats.AggregationStatistics is LastValueStatistics lastValueStats)
                {
                    Logger.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
                        lastValueStats.LastValue.HasValue ? lastValueStats.LastValue.Value.ToString() : "");
                }
                else if (stats.AggregationStatistics is HistogramStatistics histogramStats)
                {
                    Logger.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), FormatQuantiles(histogramStats.Quantiles));
                }
            }

            private string FormatTags(KeyValuePair<string, string>[] labels)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < labels.Length; i++)
                {
                    sb.AppendFormat("{0}={1}", labels[i].Key, labels[i].Value);
                    if (i != labels.Length - 1)
                    {
                        sb.Append(',');
                    }
                }
                return sb.ToString();
            }

            private string FormatQuantiles(QuantileValue[] quantiles)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < quantiles.Length; i++)
                {
                    sb.AppendFormat("{0}={1}", quantiles[i].Quantile.ToString(CultureInfo.InvariantCulture), quantiles[i].Value.ToString(CultureInfo.InvariantCulture));
                    if (i != quantiles.Length - 1)
                    {
                        sb.Append(';');
                    }
                }
                return sb.ToString();
            }
        }

        private class MetricSpec
        {
            private static char s_meterInstrumentSeparator = '\\';
            public string MeterName { get; private set; }
            public string? InstrumentName { get; private set; }

            public MetricSpec(string meterName, string? instrumentName)
            {
                MeterName = meterName;
                InstrumentName = instrumentName;
            }

            public static bool TryParse(string text, out MetricSpec spec)
            {
                int slashIdx = text.IndexOf(s_meterInstrumentSeparator);
                if (slashIdx == -1)
                {
                    spec = new MetricSpec(text.Trim(), null);
                    return true;
                }
                else
                {
                    string meterName = text.Substring(0, slashIdx).Trim();
                    string? instrumentName = text.Substring(slashIdx + 1).Trim();
                    spec = new MetricSpec(meterName, instrumentName);
                    return true;
                }
            }

            public override string ToString() => MeterName +
                (InstrumentName != null ? s_meterInstrumentSeparator + InstrumentName : "");
        }
    }
}
