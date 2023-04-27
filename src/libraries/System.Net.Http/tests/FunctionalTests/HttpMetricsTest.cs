// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpMetricsTest
    {
        [Fact]
        public async Task SendAsync_CurrentRequests_Success()
        {
            var invoker = new HttpMessageInvoker(new MockHandler());
            var currentRequestsRecorder = new InstrumentRecorder<long>(invoker.Meter, "current-requests");

            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://localhost/test"), CancellationToken.None);

            Assert.Collection(currentRequestsRecorder.GetMeasurements(),
                m => AssertCurrentRequest(m, 1, "https", "localhost"),
                m => AssertCurrentRequest(m, -1, "https", "localhost"));
        }

        [Fact]
        public void Send_CurrentRequests_Success()
        {
            var invoker = new HttpMessageInvoker(new MockHandler());
            var currentRequestsRecorder = new InstrumentRecorder<long>(invoker.Meter, "current-requests");

            var response = invoker.Send(new HttpRequestMessage(HttpMethod.Get, "https://localhost/test"), CancellationToken.None);

            Assert.Collection(currentRequestsRecorder.GetMeasurements(),
                m => AssertCurrentRequest(m, 1, "https", "localhost"),
                m => AssertCurrentRequest(m, -1, "https", "localhost"));
        }

        [Fact]
        public async Task SendAsync_RequestDuration_Success()
        {
            var invoker = new HttpMessageInvoker(new MockHandler());
            var currentRequestsRecorder = new InstrumentRecorder<double>(invoker.Meter, "request-duration");

            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://localhost/test"), CancellationToken.None);

            Assert.Collection(currentRequestsRecorder.GetMeasurements(),
                m => AssertRequestDuration(m, "https", "localhost", "HTTP/1.1", 200));
        }

        [Fact]
        public async Task SendAsync_RequestDuration_CustomTags()
        {
            var invoker = new HttpMessageInvoker(new MockHandler());
            var currentRequestsRecorder = new InstrumentRecorder<double>(invoker.Meter, "request-duration");

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");
            request.MetricsTags.Add(new KeyValuePair<string, object>("route", "/test"));
            var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Collection(currentRequestsRecorder.GetMeasurements(),
                m =>
                {
                    AssertRequestDuration(m, "http", "localhost", "HTTP/1.1", 200);
                    Assert.Equal("/test", m.Tags.ToArray().Single(t => t.Key == "route").Value);
                });
        }

        private static void AssertCurrentRequest(Measurement<long> measurement, long expectedValue, string scheme, string host, int? port = null)
        {
            Assert.Equal(expectedValue, measurement.Value);
            Assert.Equal(scheme, measurement.Tags.ToArray().Single(t => t.Key == "scheme").Value);
            Assert.Equal(host, measurement.Tags.ToArray().Single(t => t.Key == "host").Value);
            if (port is null)
            {
                Assert.DoesNotContain(measurement.Tags.ToArray(), t => t.Key == "port");
            }
            else
            {
                Assert.Equal(port, (int)measurement.Tags.ToArray().Single(t => t.Key == "port").Value);
            }
        }

        private static void AssertRequestDuration(Measurement<double> measurement, string scheme, string host, string protocol, int statusCode, int? port = null)
        {
            Assert.True(measurement.Value > 0);
            Assert.Equal(scheme, measurement.Tags.ToArray().Single(t => t.Key == "scheme").Value);
            Assert.Equal(host, measurement.Tags.ToArray().Single(t => t.Key == "host").Value);
            if (port is null)
            {
                Assert.DoesNotContain(measurement.Tags.ToArray(), t => t.Key == "port");
            }
            else
            {
                Assert.Equal(port, (int)measurement.Tags.ToArray().Single(t => t.Key == "port").Value);
            }
            Assert.Equal(protocol, measurement.Tags.ToArray().Single(t => t.Key == "protocol").Value);
            Assert.Equal(statusCode, (int)measurement.Tags.ToArray().Single(t => t.Key == "status-code").Value);
        }

        #region Helpers

        private class MockHandler : HttpMessageHandler
        {
            public int DisposeCount { get; private set; }
            public int SendAsyncCount { get; private set; }
            public int SendCount { get; private set; }

            public MockHandler()
            {
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                SendAsyncCount++;

                return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage());
            }

            protected override HttpResponseMessage Send(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                SendCount++;

                return new HttpResponseMessage();
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }
        }

        #endregion Helepers
    }

    // TODO: Remove when Metrics DI intergration package is available https://github.com/dotnet/aspnetcore/issues/47618
    internal sealed class InstrumentRecorder<T> : IDisposable where T : struct
    {
        private readonly object _lock = new object();
        private readonly string _meterName;
        private readonly string _instrumentName;
        private readonly MeterListener _meterListener;
        private readonly List<Measurement<T>> _values;
        private readonly List<Action<Measurement<T>>> _callbacks;

        public InstrumentRecorder(Meter meter, string instrumentName, object? state = null) : this(new TestMeterRegistry(new List<Meter> { meter }), meter.Name, instrumentName, state)
        {
        }

        public InstrumentRecorder(IMeterRegistry registry, string meterName, string instrumentName, object? state = null)
        {
            _meterName = meterName;
            _instrumentName = instrumentName;
            _callbacks = new List<Action<Measurement<T>>>();
            _values = new List<Measurement<T>>();
            _meterListener = new MeterListener();
            _meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == _meterName && registry.Contains(instrument.Meter) && instrument.Name == _instrumentName)
                {
                    listener.EnableMeasurementEvents(instrument, state);
                }
            };
            _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
            _meterListener.Start();
        }

        private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            lock (_lock)
            {
                var m = new Measurement<T>(measurement, tags);
                _values.Add(m);

                // Should this happen in the lock?
                // Is there a better way to notify listeners that there are new measurements?
                foreach (var callback in _callbacks)
                {
                    callback(m);
                }
            }
        }

        public void Register(Action<Measurement<T>> callback)
        {
            _callbacks.Add(callback);
        }

        public IReadOnlyList<Measurement<T>> GetMeasurements()
        {
            lock (_lock)
            {
                return _values.ToArray();
            }
        }

        public void Dispose()
        {
            _meterListener.Dispose();
        }
    }

    internal interface IMeterRegistry
    {
        void Add(Meter meter);
        bool Contains(Meter meter);
    }

    internal class TestMeterRegistry : IMeterRegistry
    {
        private readonly List<Meter> _meters;

        public TestMeterRegistry() : this(new List<Meter>())
        {
        }

        public TestMeterRegistry(List<Meter> meters)
        {
            _meters = meters;
        }

        public void Add(Meter meter) => _meters.Add(meter);

        public bool Contains(Meter meter) => _meters.Contains(meter);
    }
}
