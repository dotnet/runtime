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
            private static double WindowScaleThresholdMultiplier => GlobalHttpSettings.SocketsHttpHandler.Http2StreamWindowScaleThresholdMultiplier;
            private static int MaxStreamWindowSize => GlobalHttpSettings.SocketsHttpHandler.MaxHttp2StreamWindowSize;
            private static bool WindowScalingEnabled => !GlobalHttpSettings.SocketsHttpHandler.DisableDynamicHttp2WindowSizing;

            private int _deliveredBytes;
            private int _streamWindowSize;
            private long _lastWindowUpdate;

            public Http2StreamWindowManager(Http2Connection connection, Http2Stream stream)
            {
                HttpConnectionSettings settings = connection._pool.Settings;
                _streamWindowSize = settings._initialHttp2StreamWindowSize;
                _lastWindowUpdate = Stopwatch.GetTimestamp();
                _deliveredBytes = 0;

                if (NetEventSource.Log.IsEnabled()) stream.Trace($"[FlowControl] InitialClientStreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}, WindowScaleThresholdMultiplier: {WindowScaleThresholdMultiplier}");
            }

            internal int StreamWindowSize => _streamWindowSize;

            internal int StreamWindowThreshold => _streamWindowSize / StreamWindowUpdateRatio;

            public void AdjustWindow(int bytesConsumed, Http2Stream stream)
            {
                Debug.Assert(bytesConsumed > 0);
                Debug.Assert(_deliveredBytes < StreamWindowThreshold);

                if (!stream.ExpectResponseData)
                {
                    // We are not expecting any more data (because we've either completed or aborted).
                    // So no need to send any more WINDOW_UPDATEs.
                    return;
                }

                if (WindowScalingEnabled)
                {
                    AdjustWindowDynamic(bytesConsumed, stream);
                }
                else
                {
                    AjdustWindowStatic(bytesConsumed, stream);
                }
            }

            private void AjdustWindowStatic(int bytesConsumed, Http2Stream stream)
            {
                _deliveredBytes += bytesConsumed;
                if (_deliveredBytes < StreamWindowThreshold)
                {
                    return;
                }

                int windowUpdateIncrement = _deliveredBytes;
                _deliveredBytes = 0;

                Http2Connection connection = stream.Connection;
                Task sendWindowUpdateTask = connection.SendWindowUpdateAsync(stream.StreamId, windowUpdateIncrement);
                connection.LogExceptions(sendWindowUpdateTask);
            }

            private void AdjustWindowDynamic(int bytesConsumed, Http2Stream stream)
            {
                _deliveredBytes += bytesConsumed;

                if (_deliveredBytes < StreamWindowThreshold)
                {
                    return;
                }

                int windowUpdateIncrement = _deliveredBytes;
                long currentTime = Stopwatch.GetTimestamp();
                Http2Connection connection = stream.Connection;

                if (connection._rttEstimator.MinRtt > TimeSpan.Zero)
                {
                    TimeSpan rtt = connection._rttEstimator.MinRtt;
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
                    if (_deliveredBytes * (double)rtt.Ticks > _streamWindowSize * dt.Ticks * WindowScaleThresholdMultiplier)
                    {
                        int extendedWindowSize = Math.Min(MaxStreamWindowSize, _streamWindowSize * 2);
                        windowUpdateIncrement += extendedWindowSize - _streamWindowSize;
                        _streamWindowSize = extendedWindowSize;

                        if (NetEventSource.Log.IsEnabled()) stream.Trace($"[FlowControl] Updated Stream Window. StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}");

                        Debug.Assert(_streamWindowSize <= MaxStreamWindowSize);
                        if (_streamWindowSize == MaxStreamWindowSize)
                        {
                            if (NetEventSource.Log.IsEnabled()) stream.Trace($"[FlowControl] StreamWindowSize reached the configured maximum of {MaxStreamWindowSize}.");
                        }
                    }
                }

                _deliveredBytes = 0;

                Task sendWindowUpdateTask = connection.SendWindowUpdateAsync(stream.StreamId, windowUpdateIncrement);
                connection.LogExceptions(sendWindowUpdateTask);

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

            private const double PingIntervalInSeconds = 2;
            private const int InitialBurstCount = 4;
            private static readonly long PingIntervalInTicks =(long)(PingIntervalInSeconds * Stopwatch.Frequency);

            private State _state;
            private long _pingSentTimestamp;
            private long _pingCounter;
            private int _initialBurst;
            private long _minRtt;

            public TimeSpan MinRtt => new TimeSpan(_minRtt);

            public static RttEstimator Create()
            {
                RttEstimator e = default;
                e._state = GlobalHttpSettings.SocketsHttpHandler.DisableDynamicHttp2WindowSizing ? State.Disabled : State.Init;
                e._initialBurst = InitialBurstCount;
                return e;
            }

            internal void OnInitialSettingsSent()
            {
                if (_state == State.Disabled) return;
                _pingSentTimestamp = Stopwatch.GetTimestamp();
            }

            internal void OnInitialSettingsAckReceived(Http2Connection connection)
            {
                if (_state == State.Disabled) return;
                RefreshRtt(connection);
                _state = State.Waiting;
            }

            internal void OnDataOrHeadersReceived(Http2Connection connection)
            {
                if (_state != State.Waiting) return;

                long now = Stopwatch.GetTimestamp();
                bool initial = _initialBurst > 0;
                if (initial || now - _pingSentTimestamp > PingIntervalInTicks)
                {
                    if (initial) _initialBurst--;

                    // Send a PING
                    _pingCounter--;
                    connection.LogExceptions(connection.SendPingAsync(_pingCounter, isAck: false));
                    _pingSentTimestamp = now;
                    _state = State.PingSent;
                }
            }

            internal void OnPingAckReceived(long payload, Http2Connection connection)
            {
                if (_state != State.PingSent)
                {
                    ThrowProtocolError();
                }

                //RTT PINGs always carry negavie payload, positive values indicate a response to KeepAlive PING.
                Debug.Assert(payload < 0);

                if (_pingCounter != payload)
                    ThrowProtocolError();

                RefreshRtt(connection);
                _state = State.Waiting;
            }

            internal void OnGoAwayReceived()
            {
                _state = State.Disabled;
            }

            private void RefreshRtt(Http2Connection connection)
            {
                long elapsedTicks = Stopwatch.GetTimestamp() - _pingSentTimestamp;
                long prevRtt = _minRtt == 0 ? long.MaxValue : _minRtt;
                TimeSpan currentRtt = TimeSpan.FromSeconds(elapsedTicks / (double)Stopwatch.Frequency);
                long minRtt = Math.Min(prevRtt, currentRtt.Ticks);

                Interlocked.Exchange(ref _minRtt, minRtt); // MinRtt is being queried from another thread

                if (NetEventSource.Log.IsEnabled()) connection.Trace($"[FlowControl] Updated MinRtt: {MinRtt.TotalMilliseconds} ms");
            }
        }
    }
}
