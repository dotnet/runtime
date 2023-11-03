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
        private int _timerStarted;
        private IObservableInstrumentsSource? _source;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        internal TextWriter? _textWriter; // For testing
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        public DebugConsoleMetricListener()
        {
            _timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public string Name => ConsoleMetrics.DebugListenerName;

        public bool InstrumentPublished(Instrument instrument, out object? userState)
        {
            // Start the timer if this is the first observable instrument.
            if (instrument.IsObservable && Interlocked.Exchange(ref _timerStarted, 1) == 0)
            {
                _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }

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
            var writer = _textWriter ?? Console.Out;
            lock (writer)
            {
                writer.WriteLine(output);
            }
        }

        private void OnTimer(object? _)
        {
            _source?.RecordObservableInstruments();
        }

        public void Dispose() => _timer.Dispose();
    }
}
