// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        private int InitialClientStreamWindowSize { get; } = 65535;

        private class Http2StreamWindowManager
        {
            public const int StreamWindowUpdateRatio = 8;
            protected int _delivered;
            protected int _streamWindowSize;

            protected readonly Http2Connection _connection;
            protected readonly Http2Stream _stream;

            public Http2StreamWindowManager(Http2Connection connection, Http2Stream stream)
            {
                _connection = connection;

                _stream = stream;
                _streamWindowSize = connection.InitialClientStreamWindowSize;
                _stream.TraceFlowControl($"InitialClientStreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}");
            }

            internal int StreamWindowSize => _streamWindowSize;

            internal int StreamWindowThreshold => _streamWindowSize / StreamWindowUpdateRatio;

            public virtual void AdjustWindow(int bytesConsumed)
            {
                Debug.Assert(bytesConsumed > 0);
                Debug.Assert(_delivered < StreamWindowThreshold);

                if (!_stream.ExpectResponseData)
                {
                    // We are not expecting any more data (because we've either completed or aborted).
                    // So no need to send any more WINDOW_UPDATEs.
                    return;
                }

                _delivered += bytesConsumed;
                if (_delivered < StreamWindowThreshold)
                {
                    return;
                }

                int windowUpdateSize = _delivered;
                _delivered = 0;

                Task sendWindowUpdateTask = _connection.SendWindowUpdateAsync(_stream.StreamId, windowUpdateSize);
                _connection.LogExceptions(sendWindowUpdateTask);
            }
        }

        private class DynamicHttp2StreamWindowManager : Http2StreamWindowManager
        {
            private static readonly double StopWatchToTimesSpan = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

            private long _lastWindowUpdate;

            private double _windowScaleThresholdMultiplier;

            public DynamicHttp2StreamWindowManager(Http2Connection connection, Http2Stream stream)
                : base(connection, stream)
            {
                _windowScaleThresholdMultiplier = connection._pool.Settings._http2StreamWindowScaleThresholdMultiplier;
                _stream.TraceFlowControl($" http2StreamWindowScaleThresholdMultiplier: {_windowScaleThresholdMultiplier}");
                _lastWindowUpdate = Stopwatch.GetTimestamp();
            }

            public override void AdjustWindow(int bytesConsumed)
            {
                _delivered += bytesConsumed;
                if (_delivered < StreamWindowThreshold)
                {
                    return;
                }

                int windowSizeIncrement = _delivered;
                long currentTime = Stopwatch.GetTimestamp();

                if (_connection._rttEstimator!.MinRtt > TimeSpan.Zero)
                {
                    TimeSpan rtt = _connection._rttEstimator.MinRtt;
                    TimeSpan dt = StopwatchTicksToTimeSpan(currentTime - _lastWindowUpdate);

                    if (_delivered * rtt.Ticks > _streamWindowSize * dt.Ticks * _windowScaleThresholdMultiplier)
                    {
                        windowSizeIncrement += _streamWindowSize;
                        _streamWindowSize *= 2;

                        _stream.TraceFlowControl($"Updated Stream Window. StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}");
                    }
                }

                Task sendWindowUpdateTask = _connection.SendWindowUpdateAsync(_stream.StreamId, windowSizeIncrement);
                _connection.LogExceptions(sendWindowUpdateTask);

                _delivered = 0;
                _lastWindowUpdate = currentTime;
            }

            private static TimeSpan StopwatchTicksToTimeSpan(long stopwatchTicks)
            {
                long ticks = (long)(StopWatchToTimesSpan * stopwatchTicks);
                return new TimeSpan(ticks);
            }
        }

        private class RttEstimator
        {
            private enum Status
            {
                Init,
                Waiting,
                PingSent,
                Terminating
            }

            private const double PingIntervalInSeconds = 1;
            private static readonly long PingIntervalInTicks =(long)(PingIntervalInSeconds * Stopwatch.Frequency);

            private Http2Connection _connection;

            private Status _status;
            private long _pingSentTimestamp;
            private long _pingCounter = -1;
            private int _initialBurst = 4;

            public TimeSpan MinRtt { get; private set; }

            public RttEstimator(Http2Connection connection)
            {
                _connection = connection;
            }

            internal void OnInitialSettingsSent()
            {
                _connection.TraceFlowControl("Initial SETTINGS sent");
                _pingSentTimestamp = Stopwatch.GetTimestamp();
            }

            internal void OnInitialSettingsAckReceived()
            {
                _connection.TraceFlowControl("Initial SETTINGS ACK received");
                RefreshRtt();
                _status = Status.Waiting;
            }

            internal void OnDataOrHeadersReceived()
            {
                if (_status == Status.Waiting)
                {
                    long now = Stopwatch.GetTimestamp();
                    bool initial = Interlocked.Decrement(ref _initialBurst) >= 0;
                    if (initial || now - _pingSentTimestamp > PingIntervalInTicks)
                    {
                        if (_initialBurst > 0) Interlocked.Decrement(ref _initialBurst);

                        // Send a PING
                        long payload = Interlocked.Decrement(ref _pingCounter);
                        _connection.TraceFlowControl("Sending PING in response to DATA.");
                        _connection.LogExceptions(_connection.SendPingAsync(payload, isAck: false));
                        _pingSentTimestamp = now;
                        _status = Status.PingSent;
                    }
                }
            }

            internal void OnPingAckReceived(long payload)
            {
                Debug.Assert(payload < 0);
                if (_status != Status.PingSent) return;

                if (Interlocked.Read(ref _pingCounter) != payload)
                    ThrowProtocolError();
                RefreshRtt();
                _status = Status.Waiting;
            }

            internal void OnGoAwayReceived()
            {
                _status = Status.Terminating;
            }

            private void RefreshRtt()
            {
                long elapsedTicks = Stopwatch.GetTimestamp() - _pingSentTimestamp;
                TimeSpan prevRtt = MinRtt == TimeSpan.Zero ? TimeSpan.MaxValue : MinRtt;
                TimeSpan currentRtt = TimeSpan.FromSeconds(elapsedTicks / (double)Stopwatch.Frequency);
                MinRtt = new TimeSpan(Math.Min(prevRtt.Ticks, currentRtt.Ticks));
                _connection.TraceFlowControl($"Updated MinRtt: {MinRtt.TotalMilliseconds} ms || prevRtt:{prevRtt.TotalMilliseconds} ms, currentRtt:{currentRtt.TotalMilliseconds} ms)");
            }
        }
    }
}
