// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal sealed class ConsoleMetricListener : IMetricsListener, IDisposable
    {
        private readonly Timer _timer;
        internal TextWriter _textWriter = Console.Out;
        private IMetricsSource? _source;

        public ConsoleMetricListener()
        {
            _timer = new Timer(OnTimer, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public string Name => "Console";

        public object? InstrumentPublished(Instrument instrument) => null;

        public void MeasurementsCompleted(Instrument instrument, object? userState) { }

        public void SetSource(IMetricsSource source) => _source = source;
        public MeasurementCallback<T> GetMeasurementHandler<T>() where T : struct => MeasurementHandler;

        private void MeasurementHandler<T>(Instrument instrument, T measurement, ReadOnlySpan<System.Collections.Generic.KeyValuePair<string, object?>> tags, object? state) where T : struct
        {
            _textWriter.WriteLine($"{instrument.Meter.Name}-{instrument.Name} {measurement} {instrument.Unit}");
        }

        private void OnTimer(object? _)
        {
            _source?.RecordObservableInstruments();
        }

        public void Dispose() => _timer.Dispose();
    }
}
