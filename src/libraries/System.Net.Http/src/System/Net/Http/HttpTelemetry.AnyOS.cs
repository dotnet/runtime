// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Net.Http
{
    internal sealed partial class HttpTelemetry
    {
        private IncrementingPollingCounter? _startedRequestsPerSecondCounter;
        private IncrementingPollingCounter? _failedRequestsPerSecondCounter;
        private PollingCounter? _startedRequestsCounter;
        private PollingCounter? _currentRequestsCounter;
        private PollingCounter? _failedRequestsCounter;
        private PollingCounter? _totalHttp11ConnectionsCounter;
        private PollingCounter? _totalHttp20ConnectionsCounter;
        private PollingCounter? _totalHttp30ConnectionsCounter;
        private EventCounter? _http11RequestsQueueDurationCounter;
        private EventCounter? _http20RequestsQueueDurationCounter;
        private EventCounter? _http30RequestsQueueDurationCounter;

        [NonEvent]
        public void Http11RequestLeftQueue(double timeOnQueueMilliseconds)
        {
            _http11RequestsQueueDurationCounter?.WriteMetric(timeOnQueueMilliseconds);
            RequestLeftQueue(timeOnQueueMilliseconds, versionMajor: 1, versionMinor: 1);
        }

        [NonEvent]
        public void Http20RequestLeftQueue(double timeOnQueueMilliseconds)
        {
            _http20RequestsQueueDurationCounter?.WriteMetric(timeOnQueueMilliseconds);
            RequestLeftQueue(timeOnQueueMilliseconds, versionMajor: 2, versionMinor: 0);
        }

        [NonEvent]
        public void Http30RequestLeftQueue(double timeOnQueueMilliseconds)
        {
            _http30RequestsQueueDurationCounter?.WriteMetric(timeOnQueueMilliseconds);
            RequestLeftQueue(timeOnQueueMilliseconds, versionMajor: 3, versionMinor: 0);
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
                // They aren't disabled afterwards...

                // The cumulative number of HTTP requests started since the process started.
                _startedRequestsCounter ??= new PollingCounter("requests-started", this, () => Interlocked.Read(ref _startedRequests))
                {
                    DisplayName = "Requests Started",
                };

                // The number of HTTP requests started per second since the process started.
                _startedRequestsPerSecondCounter ??= new IncrementingPollingCounter("requests-started-rate", this, () => Interlocked.Read(ref _startedRequests))
                {
                    DisplayName = "Requests Started Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                // The cumulative number of HTTP requests failed since the process started.
                // Failed means that an exception occurred during the handler's Send(Async) call as a result of a connection related error, timeout, or explicitly cancelled.
                // In case of using HttpClient's SendAsync(and friends) with buffering, this includes exceptions that occurred while buffering the response content
                // In case of using HttpClient's helper methods (GetString/ByteArray/Stream), this includes responses with non-success status codes
                _failedRequestsCounter ??= new PollingCounter("requests-failed", this, () => Interlocked.Read(ref _failedRequests))
                {
                    DisplayName = "Requests Failed"
                };

                // The number of HTTP requests failed per second since the process started.
                _failedRequestsPerSecondCounter ??= new IncrementingPollingCounter("requests-failed-rate", this, () => Interlocked.Read(ref _failedRequests))
                {
                    DisplayName = "Requests Failed Rate",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                // The current number of active HTTP requests that have started but not yet completed or failed.
                // Use (-_stoppedRequests + _startedRequests) to avoid returning a negative value if _stoppedRequests is
                // incremented after reading _startedRequests due to race conditions with completing the HTTP request.
                _currentRequestsCounter ??= new PollingCounter("current-requests", this, () => -Interlocked.Read(ref _stoppedRequests) + Interlocked.Read(ref _startedRequests))
                {
                    DisplayName = "Current Requests"
                };

                _totalHttp11ConnectionsCounter ??= new PollingCounter("http11-connections-current-total", this, () => Interlocked.Read(ref _openedHttp11Connections))
                {
                    DisplayName = "Current Http 1.1 Connections"
                };

                _totalHttp20ConnectionsCounter ??= new PollingCounter("http20-connections-current-total", this, () => Interlocked.Read(ref _openedHttp20Connections))
                {
                    DisplayName = "Current Http 2.0 Connections"
                };

                _totalHttp30ConnectionsCounter ??= new PollingCounter("http30-connections-current-total", this, () => Interlocked.Read(ref _openedHttp30Connections))
                {
                    DisplayName = "Current Http 3.0 Connections"
                };

                _http11RequestsQueueDurationCounter ??= new EventCounter("http11-requests-queue-duration", this)
                {
                    DisplayName = "HTTP 1.1 Requests Queue Duration",
                    DisplayUnits = "ms"
                };

                _http20RequestsQueueDurationCounter ??= new EventCounter("http20-requests-queue-duration", this)
                {
                    DisplayName = "HTTP 2.0 Requests Queue Duration",
                    DisplayUnits = "ms"
                };

                _http30RequestsQueueDurationCounter ??= new EventCounter("http30-requests-queue-duration", this)
                {
                    DisplayName = "HTTP 3.0 Requests Queue Duration",
                    DisplayUnits = "ms"
                };
            }
        }
    }
}
