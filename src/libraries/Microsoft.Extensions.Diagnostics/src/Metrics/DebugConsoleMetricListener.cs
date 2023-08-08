// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    internal sealed class DebugConsoleMetricListener : IMetricsListener, IDisposable
    {
        private readonly Timer _timer;
        internal TextWriter _textWriter = Console.Out;
        private IObservableInstrumentsSource? _source;

        public DebugConsoleMetricListener()
        {
            _timer = new Timer(OnTimer, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public string Name => ConsoleMetrics.DebugListenerName;

        public bool InstrumentPublished(Instrument instrument, out object? userState)
        {
            WriteLine($"{instrument.Meter.Name}-{instrument.Name} Started; Description: {instrument.Description}.");
            userState = this;
            return true;
        }

        public void MeasurementsCompleted(Instrument instrument, object? userState)
        {
            Debug.Assert(userState == this);
            WriteLine($"{instrument.Meter.Name}-{instrument.Name} Stopped.");
        }

        public void Initialize(IObservableInstrumentsSource source) => _source = source;

        public MeasurementHandlers GetMeasurementHandlers() => new MeasurementHandlers
        {
            ByteHandler = MeasurementHandler,
            ShortHandler = MeasurementHandler,
            IntHandler = MeasurementHandler,
            LongHandler = MeasurementHandler,
            FloatHandler = MeasurementHandler,
            DoubleHandler = MeasurementHandler,
            DecimalHandler = MeasurementHandler,
        };

        private void MeasurementHandler<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) where T : struct
        {
            Debug.Assert(state == this);
            WriteLine($"{instrument.Meter.Name}-{instrument.Name} {measurement} {instrument.Unit}");
        }

        private void WriteLine(string output)
        {
            lock (_textWriter)
            {
                _textWriter.WriteLine(output);
            }
        }

        private void OnTimer(object? _)
        {
            _source?.RecordObservableInstruments();
        }

        public void Dispose() => _timer.Dispose();
    }
}
