// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        // Maintains a dynamically-sized stream receive window, and sends WINDOW_UPDATE frames to the server.
        private struct Http2StreamWindowManager
        {
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
                _deliveredBytes = 0;
                _lastWindowUpdate = default;

                if (NetEventSource.Log.IsEnabled()) stream.Trace($"[FlowControl] InitialClientStreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}, WindowScaleThresholdMultiplier: {WindowScaleThresholdMultiplier}");
            }

            // We hold off on sending WINDOW_UPDATE until we hit the minimum threshold.
            // This value is somewhat arbitrary; the intent is to ensure it is much smaller than
            // the window size itself, or we risk stalling the server because it runs out of window space.
            public const int StreamWindowUpdateRatio = 8;
            internal int StreamWindowThreshold => _streamWindowSize / StreamWindowUpdateRatio;

            internal int StreamWindowSize => _streamWindowSize;

            public void Start()
            {
                _lastWindowUpdate = Stopwatch.GetTimestamp();
            }

            public void AdjustWindow(int bytesConsumed, Http2Stream stream)
            {
                Debug.Assert(_lastWindowUpdate != default); // Make sure Start() has been invoked, otherwise we should not be receiving DATA.
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

                TimeSpan rtt = connection._rttEstimator.MinRtt;
                if (rtt > TimeSpan.Zero && _streamWindowSize < MaxStreamWindowSize)
                {
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

        // Estimates Round Trip Time between the client and the server by sending PING frames, and measuring the time interval until a PING ACK is received.
        // Assuming that the network characteristics of the connection wouldn't change much within its lifetime, we are maintaining a running minimum value.
        // The more PINGs we send, the more accurate is the estimation of MinRtt, however we should be careful not to send too many of them,
        // to avoid triggering the server's PING flood protection which may result in an unexpected GOAWAY.
        // With most servers we are fine to send PINGs, as long as we are reading their data, this rule is well formalized for gRPC:
        // https://github.com/grpc/proposal/blob/master/A8-client-side-keepalive.md
        // As a rule of thumb, we can send send a PING whenever we receive DATA or HEADERS, however, there are some servers which allow receiving only
        // a limited amount of PINGs within a given timeframe.
        // To deal with the conflicting requirements:
        // - We send an initial burst of 'InitialBurstCount' PINGs, to get a relatively good estimation fast
        // - Afterwards, we send PINGs with the maximum frequency of 'PingIntervalInSeconds' PINGs per second
        //
        // Threading:
        // OnInitialSettingsSent() is called during initialization, all other methods are triggered by HttpConnection.ProcessIncomingFramesAsync(),
        // therefore the assumption is that the invocation of RttEstimator's methods is sequential, and there is no race beetween them.
        // Http2StreamWindowManager is reading MinRtt from another concurrent thread, therefore its value has to be changed atomically.
        private struct RttEstimator
        {
            private enum State
            {
                Disabled,
                Init,
                Waiting,
                PingSent,
                TerminatingMayReceivePingAck
            }

            private const double PingIntervalInSeconds = 2;
            private const int InitialBurstCount = 4;
            private static readonly long PingIntervalInTicks = (long)(PingIntervalInSeconds * Stopwatch.Frequency);

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
                if (_state != State.PingSent && _state != State.TerminatingMayReceivePingAck)
                {
                    ThrowProtocolError();
                }

                if (_state == State.TerminatingMayReceivePingAck)
                {
                    _state = State.Disabled;
                    return;
                }

                // RTT PINGs always carry negative payload, positive values indicate a response to KeepAlive PING.
                Debug.Assert(payload < 0);

                if (_pingCounter != payload)
                    ThrowProtocolError();

                RefreshRtt(connection);
                _state = State.Waiting;
            }

            internal void OnGoAwayReceived()
            {
                if (_state == State.PingSent)
                {
                    // We may still receive a PING ACK, but we should not send anymore PING:
                    _state = State.TerminatingMayReceivePingAck;
                }
                else
                {
                    _state = State.Disabled;
                }
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
