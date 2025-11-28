// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Diagnostics.Metrics.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/95210", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]
    public partial class MetricEventSourceTests
    {
        ITestOutputHelper _output;
        const double IntervalSecs = 10;
        static readonly TimeSpan s_waitForEventTimeout = TimeSpan.FromSeconds(60);

        private const string RuntimeMeterName = "System.Runtime";

        public MetricEventSourceTests(ITestOutputHelper output)
        {
            _output = output;
        }
        private static void AssertBeginInstrumentReportingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var beginReportEvents = events.Where(e => e.EventName == "BeginInstrumentReporting").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString(),
                    InstrumentTags = e.Payload[7].ToString(),
                    MeterTags = e.Payload[8].ToString(),
                    ScopeHash = e.Payload[9].ToString(),
                    InstrumentId = (int)(e.Payload[10]),
                    TelemetrySchemaUrl = e.Payload[11].ToString(),
                }).ToArray();

            foreach (Instrument i in expectedInstruments)
            {
                var e = beginReportEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a BeginInstrumentReporting event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
                Assert.Equal(Helpers.FormatTags(i.Tags), e.InstrumentTags);
                Assert.Equal(Helpers.FormatTags(i.Meter.Tags), e.MeterTags);
                Assert.Equal(Helpers.FormatObjectHash(i.Meter.Scope), e.ScopeHash);
                Assert.Equal(i.Meter.TelemetrySchemaUrl ?? "", e.TelemetrySchemaUrl);
                Assert.True(e.InstrumentId > 0);
            }

            Assert.Equal(expectedInstruments.Length, beginReportEvents.Length);
        }

        private static void AssertEndInstrumentReportingEventsPresent(EventWrittenEventArgs[] events, params Instrument[] expectedInstruments)
        {
            var beginReportEvents = events.Where(e => e.EventName == "EndInstrumentReporting").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString(),
                    InstrumentTags = e.Payload[7].ToString(),
                    MeterTags = e.Payload[8].ToString(),
                    ScopeHash = e.Payload[9].ToString(),
                    InstrumentId = (int)(e.Payload[10]),
                    TelemetrySchemaUrl = e.Payload[11].ToString(),
                }).ToArray();

            foreach (Instrument i in expectedInstruments)
            {
                var e = beginReportEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a EndInstrumentReporting event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
                Assert.Equal(Helpers.FormatTags(i.Tags), e.InstrumentTags);
                Assert.Equal(Helpers.FormatTags(i.Meter.Tags), e.MeterTags);
                Assert.Equal(Helpers.FormatObjectHash(i.Meter.Scope), e.ScopeHash);
                Assert.Equal(i.Meter.TelemetrySchemaUrl ?? "", e.TelemetrySchemaUrl);
                Assert.True(e.InstrumentId > 0);
            }

            Assert.Equal(expectedInstruments.Length, beginReportEvents.Length);
        }

        private static void AssertInitialEnumerationCompleteEventPresent(EventWrittenEventArgs[] events, int eventsCount = 1)
        {
            Assert.Equal(eventsCount, events.Where(e => e.EventName == "InitialInstrumentEnumerationComplete").Count());
        }

        private static void AssertTimeSeriesLimitPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(1, events.Where(e => e.EventName == "TimeSeriesLimitReached").Count());
        }

        private static void AssertTimeSeriesLimitNotPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(0, events.Where(e => e.EventName == "TimeSeriesLimitReached").Count());
        }

        private static void AssertHistogramLimitPresent(EventWrittenEventArgs[] events)
        {
            Assert.Equal(1, events.Where(e => e.EventName == "HistogramLimitReached").Count());
        }

        private static void AssertInstrumentPublishingEventsPresent(EventWrittenEventArgs[] events, string[] meterNamesFilters, params Instrument[] expectedInstruments)
        {
            var publishEvents = events.Where(e => e.EventName == "InstrumentPublished" && meterNamesFilters.Contains(e.Payload[1].ToString())).Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentType = e.Payload[4].ToString(),
                    Unit = e.Payload[5].ToString(),
                    Description = e.Payload[6].ToString(),
                    InstrumentTags = e.Payload[7].ToString(),
                    MeterTags = e.Payload[8].ToString(),
                    ScopeHash = e.Payload[9].ToString(),
                    InstrumentId = (int)(e.Payload[10]),
                    TelemetrySchemaUrl = e.Payload[11].ToString(),
                }).ToArray();

            foreach (Instrument i in expectedInstruments)
            {
                var e = publishEvents.Where(ev => ev.InstrumentName == i.Name && ev.MeterName == i.Meter.Name).FirstOrDefault();
                Assert.True(e != null, "Expected to find a InstrumentPublished event for " + i.Meter.Name + "\\" + i.Name);
                Assert.Equal(i.Meter.Version ?? "", e.MeterVersion);
                Assert.Equal(i.GetType().Name, e.InstrumentType);
                Assert.Equal(i.Unit ?? "", e.Unit);
                Assert.Equal(i.Description ?? "", e.Description);
                Assert.Equal(Helpers.FormatTags(i.Tags), e.InstrumentTags);
                Assert.Equal(Helpers.FormatTags(i.Meter.Tags), e.MeterTags);
                Assert.Equal(Helpers.FormatObjectHash(i.Meter.Scope), e.ScopeHash);
                Assert.Equal(i.Meter.TelemetrySchemaUrl ?? "", e.TelemetrySchemaUrl);
                Assert.True(e.InstrumentId >= 0); // It is possible getting Id 0 with InstrumentPublished event when measurements are not enabling  (e.g. CounterRateValuePublished event)
            }

            Assert.Equal(expectedInstruments.Length, publishEvents.Length);
        }

        private static void AssertCounterEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string)[] expected)
        {
            AssertGenericCounterEventsPresent("CounterRateValuePublished", events, meterName, instrumentName, tags, expectedUnit, expected);
        }

        private static void AssertUpDownCounterEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string)[] expected)
        {
            AssertGenericCounterEventsPresent("UpDownCounterRateValuePublished", events, meterName, instrumentName, tags, expectedUnit, expected);
        }

        private static void AssertGenericCounterEventsPresent(string eventName, EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string)[] expected)
        {
            var counterEvents = events.Where(e => e.EventName == eventName).Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Rate = e.Payload[6].ToString(),
                    Value = e.Payload[7].ToString(),
                    InstrumentId = (int)(e.Payload[8]),
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expectedUnit, filteredEvents[i].Unit);
                Assert.Equal(expected[i].Item1, filteredEvents[i].Rate);
                Assert.Equal(expected[i].Item2, filteredEvents[i].Value);
                Assert.True(filteredEvents[i].InstrumentId > 0);
            }
        }

        private static void AssertCounterEventsNotPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags)
        {
            var counterEvents = events.Where(e => e.EventName == "CounterRateValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Tags = e.Payload[5].ToString()
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.Equal(0, filteredEvents.Length);
        }

        private static void AssertGaugeEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params string[] expectedValues)
        {
            var counterEvents = events.Where(e => e.EventName == "GaugeValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Value = e.Payload[6].ToString(),
                    InstrumentId = (int)(e.Payload[7]),
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expectedValues.Length);

            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.Equal(expectedUnit, filteredEvents[i].Unit);
                Assert.Equal(expectedValues[i], filteredEvents[i].Value);
                Assert.True(filteredEvents[i].InstrumentId > 0);
            }
        }

        private static void AssertHistogramEventsPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags,
            string expectedUnit, params (string, string, string)[] expected)
        {
            var counterEvents = events.Where(e => e.EventName == "HistogramValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Unit = e.Payload[4].ToString(),
                    Tags = e.Payload[5].ToString(),
                    Quantiles = (string)e.Payload[6],
                    Count = e.Payload[7].ToString(),
                    Sum = e.Payload[8].ToString(),
                    InstrumentId = (int)(e.Payload[9])
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.True(filteredEvents.Length >= expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(filteredEvents[i].Unit, expectedUnit);
                Assert.Equal(expected[i].Item1, filteredEvents[i].Quantiles);
                Assert.Equal(expected[i].Item2, filteredEvents[i].Count);
                Assert.Equal(expected[i].Item3, filteredEvents[i].Sum);
                Assert.True(filteredEvents[i].InstrumentId > 0);
            }
        }

        private static void AssertBase2HistogramEventsPresent(
                                EventWrittenEventArgs[] events,
                                string meterName,
                                string instrumentName,
                                string tags,
                                string expectedUnit,
                                params (int Scale, double Sum, long Count, long ZeroCount, double Minimum, double Maximum, string Buckets)[] expected)
        {
            var counterEvents = events.Where(e => e.EventName == "Base2ExponentialHistogramValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    InstrumentId = (int)(e.Payload[4]),
                    Unit = e.Payload[5].ToString(),
                    Tags = e.Payload[6].ToString(),
                    Scale = (int)e.Payload[7],
                    Sum = (double)e.Payload[8],
                    Count = (long)e.Payload[9],
                    ZeroCount = (long)e.Payload[10],
                    Minimum = (double)e.Payload[11],
                    Maximum = (double)e.Payload[12],
                    Buckets = e.Payload[13].ToString()
                }).ToArray();

            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();

            Assert.True(filteredEvents.Length >= expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(filteredEvents[i].Unit, expectedUnit);
                Assert.Equal(expected[i].Scale, filteredEvents[i].Scale);
                Assert.Equal(expected[i].Sum, filteredEvents[i].Sum);
                Assert.Equal(expected[i].Count, filteredEvents[i].Count);
                Assert.Equal(expected[i].ZeroCount, filteredEvents[i].ZeroCount);
                Assert.Equal(expected[i].Minimum, filteredEvents[i].Minimum);
                Assert.Equal(expected[i].Maximum, filteredEvents[i].Maximum);
                Assert.Equal(expected[i].Buckets, filteredEvents[i].Buckets);
                Assert.True(filteredEvents[i].InstrumentId > 0);
            }
        }

        private static void AssertHistogramEventsNotPresent(EventWrittenEventArgs[] events, string meterName, string instrumentName, string tags)
        {
            var counterEvents = events.Where(e => e.EventName == "HistogramValuePublished").Select(e =>
                new
                {
                    MeterName = e.Payload[1].ToString(),
                    MeterVersion = e.Payload[2].ToString(),
                    InstrumentName = e.Payload[3].ToString(),
                    Tags = e.Payload[5].ToString()
                }).ToArray();
            var filteredEvents = counterEvents.Where(e => e.MeterName == meterName && e.InstrumentName == instrumentName && e.Tags == tags).ToArray();
            Assert.Equal(0, filteredEvents.Length);
        }

        private static void AssertCollectStartStopEventsPresent(EventWrittenEventArgs[] events, double expectedIntervalSecs, int expectedPairs)
        {
            int startEventsSeen = 0;
            int stopEventsSeen = 0;
            for (int i = 0; i < events.Length; i++)
            {
                EventWrittenEventArgs e = events[i];
                if (e.EventName == "CollectionStart")
                {
                    Assert.True(startEventsSeen == stopEventsSeen, "Unbalanced CollectionStart event");
                    startEventsSeen++;
                }
                else if (e.EventName == "CollectionStop")
                {
                    Assert.True(startEventsSeen == stopEventsSeen + 1, "Unbalanced CollectionStop event");
                    stopEventsSeen++;
                }
                else if (e.EventName == "CounterRateValuePublished" ||
                    e.EventName == "GaugeValuePublished" ||
                    e.EventName == "HistogramValuePublished")
                {
                    Assert.True(startEventsSeen == stopEventsSeen + 1, "Instrument value published outside collection interval");
                }
            }

            Assert.Equal(expectedPairs, startEventsSeen);
            Assert.Equal(expectedPairs, stopEventsSeen);
        }

        private static void AssertObservableCallbackErrorPresent(EventWrittenEventArgs[] events)
        {
            var errorEvents = events.Where(e => e.EventName == "ObservableInstrumentCallbackError").Select(e =>
                new
                {
                    ErrorText = e.Payload[1].ToString(),
                }).ToArray();
            Assert.NotEmpty(errorEvents);
            Assert.Contains("Example user exception", errorEvents[0].ErrorText);
        }

        private static void AssertMultipleSessionsConfiguredIncorrectlyErrorEventsPresent(EventWrittenEventArgs[] events,
            string expectedMaxHistograms, string actualMaxHistograms, string expectedMaxTimeSeries, string actualMaxTimeSeries,
            string expectedRefreshInterval, string actualRefreshInterval)
        {
            var counterEvents = events.Where(e => e.EventName == "MultipleSessionsConfiguredIncorrectlyError").Select(e =>
                new
                {
                    ExpectedMaxHistograms = e.Payload[1].ToString(),
                    ActualMaxHistograms = e.Payload[2].ToString(),
                    ExpectedMaxTimeSeries = e.Payload[3].ToString(),
                    ActualMaxTimeSeries = e.Payload[4].ToString(),
                    ExpectedRefreshInterval = e.Payload[5].ToString(),
                    ActualRefreshInterval = e.Payload[6].ToString(),
                }).ToArray();
            var filteredEvents = counterEvents;
            Assert.Single(filteredEvents);

            Assert.Equal(expectedMaxHistograms, filteredEvents[0].ExpectedMaxHistograms);
            Assert.Equal(expectedMaxTimeSeries, filteredEvents[0].ExpectedMaxTimeSeries);
            Assert.Equal(expectedRefreshInterval, filteredEvents[0].ExpectedRefreshInterval);
            Assert.Equal(actualMaxHistograms, filteredEvents[0].ActualMaxHistograms);
            Assert.Equal(actualMaxTimeSeries, filteredEvents[0].ActualMaxTimeSeries);
            Assert.Equal(actualRefreshInterval, filteredEvents[0].ActualRefreshInterval);
        }

        private sealed class NullTestOutputHelper : ITestOutputHelper
        {
            public static NullTestOutputHelper Instance { get; } = new();
            public void WriteLine(string message) { }
            public void WriteLine(string format, params object[] args) { }
        }
    }

    class MetricsEventListener : EventListener
    {
        public const EventKeywords MessagesKeyword = (EventKeywords)0x1;
        public const EventKeywords TimeSeriesValues = (EventKeywords)0x2;
        public const EventKeywords InstrumentPublishing = (EventKeywords)0x4;
        public const int TimeSeriesLimit = 50;
        public const int HistogramLimit = 50;
        public const string SharedSessionId = "SHARED";

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval, params string[]? instruments) :
            this(output, keywords, refreshInterval, TimeSeriesLimit, HistogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval, HistogramConfig? histogramConfig, params string[]? instruments) :
            this(output, keywords, refreshInterval, TimeSeriesLimit, HistogramLimit, histogramConfig, instruments)
        {
        }


        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, bool isShared, double? refreshInterval, params string[]? instruments) :
            this(output, keywords, isShared, refreshInterval, TimeSeriesLimit, HistogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, false, refreshInterval, timeSeriesLimit, histogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, HistogramConfig? histogramConfig, params string[]? instruments) :
            this(output, keywords, false, refreshInterval, timeSeriesLimit, histogramLimit, histogramConfig, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, bool isShared, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, HistogramConfig? histogramConfig, params string[]? instruments) :
            this(output, keywords, Guid.NewGuid().ToString(), isShared, refreshInterval, timeSeriesLimit, histogramLimit, histogramConfig, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, bool isShared, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, Guid.NewGuid().ToString(), isShared, refreshInterval, timeSeriesLimit, histogramLimit, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, string sessionId, bool isShared, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, params string[]? instruments) :
            this(output, keywords, sessionId, isShared, refreshInterval, timeSeriesLimit, histogramLimit, histogramConfig: null, instruments)
        {
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, string sessionId, bool isShared, double? refreshInterval,
            int timeSeriesLimit, int histogramLimit, HistogramConfig? histogramConfig, params string[]? instruments) :
            this(output, keywords, sessionId, isShared,
            FormatArgDictionary(refreshInterval, timeSeriesLimit, histogramLimit, instruments, sessionId, isShared, histogramConfig))
        {
        }

        private static Dictionary<string, string> FormatArgDictionary(double? refreshInterval, int? timeSeriesLimit, int? histogramLimit, string?[]? instruments, string? sessionId, bool shared, HistogramConfig? histogramConfig)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();

            if (instruments is not null)
            {
                d.Add("Metrics", string.Join(",", instruments));
            }

            if (refreshInterval.HasValue)
            {
                d.Add("RefreshInterval", refreshInterval.ToString());
            }

            if (sessionId is not null)
            {
                if (shared)
                {
                    d.Add("SessionId", SharedSessionId);
                    d.Add("ClientId", sessionId);
                }
                else
                {
                    d.Add("SessionId", sessionId);
                }
            }

            if (timeSeriesLimit is not null)
            {
                d.Add("MaxTimeSeries", timeSeriesLimit.ToString());
            }

            if (histogramLimit is not null)
            {
                d.Add("MaxHistograms", histogramLimit.ToString());
            }

            if (histogramConfig is not null)
            {
                d.Add("Base2ExponentialHistogram", $"scale={histogramConfig.Scale}; maxBuckets={histogramConfig.MaxBuckets}; reportDeltas={histogramConfig.ReportDeltas}");
            }

            return d;
        }

        public MetricsEventListener(ITestOutputHelper output, EventKeywords keywords, string sessionId, bool shared, Dictionary<string, string> arguments)
        {
            _output = output;
            _keywords = keywords;
            _sessionId = shared ? SharedSessionId : sessionId;
            _arguments = arguments;
            if (_source != null)
            {
                _output.WriteLine($"[{DateTime.Now:hh:mm:ss:fffff}] Enabling EventSource");
                EnableEvents(_source, EventLevel.Informational, _keywords, _arguments);
            }
        }

        ITestOutputHelper _output;
        EventKeywords _keywords;
        string _sessionId;
        Dictionary<string, string> _arguments;
        EventSource _source;
        AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        public List<EventWrittenEventArgs> Events { get; } = new List<EventWrittenEventArgs>();

        public string SessionId => _sessionId;

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Diagnostics.Metrics")
            {
                _source = eventSource;
                if (_keywords != 0)
                {
                    _output.WriteLine($"[{DateTime.Now:hh:mm:ss:fffff}] Enabling EventSource");
                    EnableEvents(_source, EventLevel.Informational, _keywords, _arguments);
                }
            }
        }

        public override void Dispose()
        {
            if (_source != null)
            {
                // workaround for https://github.com/dotnet/runtime/issues/56378
                DisableEvents(_source);
            }
            base.Dispose();
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            string sessionId = eventData.Payload[0].ToString();
            if (eventData.EventName != "MultipleSessionsNotSupportedError"
                && eventData.EventName != "MultipleSessionsConfiguredIncorrectlyError"
                && eventData.EventName != "Version"
                && sessionId != ""
                && sessionId != _sessionId)
            {
                return;
            }
            lock (this)
            {
                Events.Add(eventData);
            }
            _output.WriteLine($"[{DateTime.Now:hh:mm:ss:fffff}] Event {eventData.EventName}");
            for (int i = 0; i < eventData.Payload.Count; i++)
            {
                if (eventData.Payload[i] is DateTime)
                {
                    _output.WriteLine($"  {eventData.PayloadNames[i]}: {((DateTime)eventData.Payload[i]).ToLocalTime():hh:mm:ss:fffff}");
                }
                else
                {
                    _output.WriteLine($"  {eventData.PayloadNames[i]}: {eventData.Payload[i]}");
                }

            }
            _autoResetEvent.Set();
        }

        public Task WaitForCollectionStop(TimeSpan timeout, int numEvents) => WaitForEvent(timeout, numEvents, "CollectionStop");

        public Task WaitForEndInstrumentReporting(TimeSpan timeout, int numEvents) => WaitForEvent(timeout, numEvents, "EndInstrumentReporting");

        public Task WaitForEnumerationComplete(TimeSpan timeout) => WaitForEvent(timeout, 1, "InitialInstrumentEnumerationComplete");

        public Task WaitForMultipleSessionsNotSupportedError(TimeSpan timeout) => WaitForEvent(timeout, 1, "MultipleSessionsNotSupportedError");

        public Task WaitForMultipleSessionsConfiguredIncorrectlyError(TimeSpan timeout) => WaitForEvent(timeout, 1, "MultipleSessionsConfiguredIncorrectlyError");

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async Task WaitForEvent(TimeSpan timeout, int numEvents, string eventName)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            DateTime startTime = DateTime.Now;
            DateTime stopTime = startTime + timeout;
            int initialEventCount = GetCountEvents(eventName);
            while (true)
            {
                if (GetCountEvents(eventName) >= numEvents)
                {
                    return;
                }
                TimeSpan remainingTime = stopTime - DateTime.Now;
                if (remainingTime.TotalMilliseconds < 0)
                {
                    int currentEventCount = GetCountEvents(eventName);
                    throw new TimeoutException($"Timed out waiting for a {eventName} event. " +
                        $"StartTime={startTime} stopTime={stopTime} initialEventCount={initialEventCount} currentEventCount={currentEventCount} targetEventCount={numEvents}");
                }
#if OS_ISBROWSER_SUPPORT
                if (OperatingSystem.IsBrowser())
                {
                    // in the single-threaded browser environment, we need to yield to the browser to allow the event to be processed
                    // we also can't block with WaitOne
                    await Task.Delay(10);
                }
                else
#endif
                {
                    _autoResetEvent.WaitOne(remainingTime);
                }
            }
        }

        private void AssertOnError()
        {
            lock (this)
            {
                var errorEvent = Events.Where(e => e.EventName == "Error").FirstOrDefault();
                if (errorEvent != null)
                {
                    string message = errorEvent.Payload[1].ToString();
                    Assert.True(errorEvent == null, "Unexpected Error event: " + message);
                }
            }
        }

        private int GetCountEvents(string eventName)
        {
            lock (this)
            {
                AssertOnError();
                return Events.Where(e => e.EventName == eventName).Count();
            }
        }
    }

    public sealed class HistogramConfig
    {
        public HistogramConfig(int scale, int maxBuckets, bool reportDeltas)
        {
            Scale = scale;
            MaxBuckets = maxBuckets;
            ReportDeltas = reportDeltas;
        }

        public int Scale { get; }
        public int MaxBuckets { get; }
        public bool ReportDeltas { get; }
    }
}
