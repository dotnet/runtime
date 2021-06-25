// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        private struct Http2StreamWindowManager
        {
            public const int StreamWindowUpdateRatio = 8;
            private static readonly double StopWatchToTimesSpan = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

            private readonly Http2Connection _connection;
            private readonly Http2Stream _stream;

            private readonly double _windowScaleThresholdMultiplier;
            private readonly int _maxStreamWindowSize;
            private bool _windowScalingEnabled;

            private int _deliveredBytes;
            private int _streamWindowSize;
            private long _lastWindowUpdate;

            public Http2StreamWindowManager(Http2Connection connection, Http2Stream stream)
            {
                _connection = connection;
                _stream = stream;
                HttpConnectionSettings settings = connection._pool.Settings;
                _streamWindowSize = settings._initialHttp2StreamWindowSize;
                _windowScalingEnabled = !settings._disableDynamicHttp2WindowSizing;
                _maxStreamWindowSize = settings._maxHttp2StreamWindowSize;
                _windowScaleThresholdMultiplier = settings._http2StreamWindowScaleThresholdMultiplier;
                _lastWindowUpdate = Stopwatch.GetTimestamp();
                _deliveredBytes = 0;

                if (NetEventSource.Log.IsEnabled()) _stream.Trace($"[FlowControl] InitialClientStreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}, WindowScaleThresholdMultiplier: {_windowScaleThresholdMultiplier}");
            }

            internal int StreamWindowSize => _streamWindowSize;

            internal int StreamWindowThreshold => _streamWindowSize / StreamWindowUpdateRatio;

            public void AdjustWindow(int bytesConsumed)
            {
                Debug.Assert(bytesConsumed > 0);
                Debug.Assert(_deliveredBytes < StreamWindowThreshold);

                if (!_stream.ExpectResponseData)
                {
                    // We are not expecting any more data (because we've either completed or aborted).
                    // So no need to send any more WINDOW_UPDATEs.
                    return;
                }

                if (_windowScalingEnabled)
                {
                    AdjustWindowDynamic(bytesConsumed);
                }
                else
                {
                    AjdustWindowStatic(bytesConsumed);
                }
            }

            private void AjdustWindowStatic(int bytesConsumed)
            {
                _deliveredBytes += bytesConsumed;
                if (_deliveredBytes < StreamWindowThreshold)
                {
                    return;
                }

                int windowUpdateIncrement = _deliveredBytes;
                _deliveredBytes = 0;

                Task sendWindowUpdateTask = _connection.SendWindowUpdateAsync(_stream.StreamId, windowUpdateIncrement);
                _connection.LogExceptions(sendWindowUpdateTask);
            }

            private void AdjustWindowDynamic(int bytesConsumed)
            {
                _deliveredBytes += bytesConsumed;

                if (_deliveredBytes < StreamWindowThreshold)
                {
                    return;
                }

                int windowUpdateIncrement = _deliveredBytes;
                long currentTime = Stopwatch.GetTimestamp();

                if (_connection._rttEstimator.MinRtt > TimeSpan.Zero)
                {
                    TimeSpan rtt = _connection._rttEstimator.MinRtt;
                    TimeSpan dt = StopwatchTicksToTimeSpan(currentTime - _lastWindowUpdate);

                    // We are detecting bursts in the amount of data consumed within a single 'dt' window update period.
                    // The value "_deliveredBytes / dt" correlates with the bandwidth of the connection.
                    // We need to extend the window, if the bandwidth-delay product grows over the current window size.
                    // To enable empirical fine tuning, we apply a configurable multiplier (_windowScaleThresholdMultiplier) to the window size, which defaults to 1.0
                    //
                    // The condition to extend the window is:
                    // (_deliveredBytes / dt) * rtt > _streamWindowSize * _windowScaleThresholdMultiplier
                    //
                    // Which is reordered into the form below, to avoid the division:
                    if (_deliveredBytes * rtt.Ticks > _streamWindowSize * dt.Ticks * _windowScaleThresholdMultiplier)
                    {
                        int extendedWindowSize = Math.Min(_maxStreamWindowSize, _streamWindowSize * 2);
                        windowUpdateIncrement += extendedWindowSize - _streamWindowSize;
                        _streamWindowSize = extendedWindowSize;

                        if (NetEventSource.Log.IsEnabled()) _stream.Trace($"[FlowControl] Updated Stream Window. StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}");

                        Debug.Assert(_streamWindowSize <= _maxStreamWindowSize);
                        if (_streamWindowSize == _maxStreamWindowSize)
                        {
                            if (NetEventSource.Log.IsEnabled()) _stream.Trace($"[FlowControl] StreamWindowSize reached the configured maximum of {_maxStreamWindowSize}.");

                            // We are at maximum configured window, stop further scaling:
                            _windowScalingEnabled = false;
                            _connection._rttEstimator.MaximumWindowReached();
                        }
                    }
                }

                _deliveredBytes = 0;

                Task sendWindowUpdateTask = _connection.SendWindowUpdateAsync(_stream.StreamId, windowUpdateIncrement);
                _connection.LogExceptions(sendWindowUpdateTask);

                _lastWindowUpdate = currentTime;
            }

            private static TimeSpan StopwatchTicksToTimeSpan(long stopwatchTicks)
            {
                long ticks = (long)(StopWatchToTimesSpan * stopwatchTicks);
                return new TimeSpan(ticks);
            }
        }

        private struct RttEstimator
        {
            private enum State
            {
                Disabled,
                Init,
                Waiting,
                PingSent
            }

            private const double PingIntervalInSeconds = 1;
            private static readonly long PingIntervalInTicks =(long)(PingIntervalInSeconds * Stopwatch.Frequency);

            private Http2Connection _connection;

            private State _state;
            private long _pingSentTimestamp;
            private long _pingCounter;
            private int _initialBurst;
            private volatile bool _terminating;

            public TimeSpan MinRtt;

            public RttEstimator(Http2Connection connection)
            {
                _connection = connection;
                _state = connection._pool.Settings._disableDynamicHttp2WindowSizing ? State.Disabled : State.Init;
                _pingCounter = 0;
                _initialBurst = 4;
                _pingSentTimestamp = default;
                MinRtt = default;
                _terminating = false;
            }

            internal void OnInitialSettingsSent()
            {
                if (_state == State.Disabled) return;
                _pingSentTimestamp = Stopwatch.GetTimestamp();
            }

            internal void OnInitialSettingsAckReceived()
            {
                if (_state == State.Disabled) return;
                RefreshRtt();
                _state = State.Waiting;
            }

            internal void OnDataOrHeadersReceived()
            {
                if (_state != State.Waiting) return;

                if (_terminating)
                {
                    _state = State.Disabled;
                    return;
                }

                long now = Stopwatch.GetTimestamp();
                bool initial = _initialBurst > 0;
                if (initial || now - _pingSentTimestamp > PingIntervalInTicks)
                {
                    if (initial) _initialBurst--;

                    // Send a PING
                    _pingCounter--;
                    _connection.LogExceptions(_connection.SendPingAsync(_pingCounter, isAck: false));
                    _pingSentTimestamp = now;
                    _state = State.PingSent;
                }
            }

            internal void OnPingAckReceived(long payload)
            {
                if (_state != State.PingSent)
                {
                    ThrowProtocolError();
                }
                Debug.Assert(payload < 0);

                if (_pingCounter != payload)
                    ThrowProtocolError();

                RefreshRtt();
                _state = State.Waiting;
            }

            internal void OnGoAwayReceived()
            {
                _state = State.Disabled;
            }

            internal void MaximumWindowReached()
            {
                // This method is being called from a thread other than the rest of the methods, so do not mess with _state.
                // OnDataOrHeadersReceived will eventually flip it to State.Disabled.
                _terminating = true;
            }

            private void RefreshRtt()
            {
                long elapsedTicks = Stopwatch.GetTimestamp() - _pingSentTimestamp;
                TimeSpan prevRtt = MinRtt == TimeSpan.Zero ? TimeSpan.MaxValue : MinRtt;
                TimeSpan currentRtt = TimeSpan.FromSeconds(elapsedTicks / (double)Stopwatch.Frequency);
                MinRtt = new TimeSpan(Math.Min(prevRtt.Ticks, currentRtt.Ticks));
                if (NetEventSource.Log.IsEnabled()) _connection.Trace($"[FlowControl] Updated MinRtt: {MinRtt.TotalMilliseconds} ms");
            }
        }
    }
}
