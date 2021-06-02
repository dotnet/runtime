// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        private int InitialStreamWindowSize { get; } = 65535;

        private class Http2StreamWindowManager
        {
            // See comment on ConnectionWindowThreshold.
            protected int _streamWindowUpdateRatio;
            protected int _delivered;
            protected int _streamWindowSize;

            protected readonly Http2Connection _connection;
            protected readonly Http2Stream _stream;

            public Http2StreamWindowManager(Http2Connection connection, Http2Stream stream)
            {
                _connection = connection;

                _stream = stream;
                _streamWindowSize = connection.InitialStreamWindowSize;
                _streamWindowUpdateRatio = _connection._pool.Settings._streamWindowUpdateRatio;
                _stream.TraceFlowControl($"StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}, streamWindowUpdateRatio: {_streamWindowUpdateRatio}");
            }

            internal int StreamWindowSize => _streamWindowSize;

            internal int StreamWindowThreshold => _streamWindowSize / _streamWindowUpdateRatio;

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
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private TimeSpan _lastWindowUpdate;

            private long _magic = 1;

            public DynamicHttp2StreamWindowManager(Http2Connection connection, Http2Stream stream)
                : base(connection, stream)
            {
                _magic = connection._pool.Settings._streamWindowMagicMultiplier;
                _stream.TraceFlowControl($" magic:{_magic} | Stopwatch: IsHighResolution={Stopwatch.IsHighResolution}, Frequency={Stopwatch.Frequency}");
                _lastWindowUpdate = _stopwatch.Elapsed;
            }

            public override void AdjustWindow(int bytesConsumed)
            {
                _delivered += bytesConsumed;
                if (_delivered < StreamWindowThreshold)
                {
                    return;
                }

                int windowSizeIncrement = _delivered;
                TimeSpan currentTime = _stopwatch.Elapsed;

                if (_connection._rttEstimator.Rtt > TimeSpan.Zero)
                {
                    TimeSpan rtt = _connection._rttEstimator.Rtt;
                    TimeSpan dt = currentTime - _lastWindowUpdate;

                    if (_magic * _delivered * rtt.Ticks > StreamWindowThreshold * dt.Ticks)
                    {
                        windowSizeIncrement += _streamWindowSize;
                        _streamWindowSize *= 2;

                        _stream.TraceFlowControl($"Updated StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold} \n | {GetDiagnostics()}");
                    }
                    else
                    {
                        string msg = "No adjustment! |" + GetDiagnostics();
                        _stream.TraceFlowControl(msg);
                    }

                    string GetDiagnostics()
                    {
                        return $"RTT={rtt.TotalMilliseconds} ms || dt={dt.TotalMilliseconds} ms || " +
                            $"Magic*_delivered/dt = {_magic * _delivered / dt.TotalSeconds} bytes/sec || StreamWindowThreshold/RTT = {StreamWindowThreshold / rtt.TotalSeconds} bytes/sec";
                    }
                }

                Task sendWindowUpdateTask = _connection.SendWindowUpdateAsync(_stream.StreamId, windowSizeIncrement);
                _connection.LogExceptions(sendWindowUpdateTask);

                _delivered = 0;
                _lastWindowUpdate = currentTime;
            }
        }

        private class RttEstimator
        {
            private enum Status
            {
                NotReady,
                Waiting,
                PingSent
            }

            private const double PingIntervalInSeconds = .5;
            private static readonly long PingIntervalInTicks =(long)(PingIntervalInSeconds * Stopwatch.Frequency);

            private Http2Connection _connection;

            private Status _status;
            private long _pingSentTimestamp;
            private long _pingCounter = -1;

            public TimeSpan Rtt { get; private set; }

            private readonly TimeSpan? _staticRtt;

            public RttEstimator(Http2Connection connection, TimeSpan? staticRtt)
            {
                _connection = connection;
                _staticRtt = staticRtt;
                if (_staticRtt.HasValue)
                {
                    Rtt = _staticRtt.Value;
                    _connection.TraceFlowControl($"Using static RTT: {Rtt.TotalMilliseconds} ms");
                }
            }

            internal void Update()
            {
                if (_staticRtt.HasValue) return;

                if (_status == Status.NotReady || _status == Status.Waiting)
                {
                    long now = Stopwatch.GetTimestamp();

                    if (now - _pingSentTimestamp > PingIntervalInTicks) // also true if _status == NotReady
                    {
                        // Send a PING
                        long payload = Interlocked.Decrement(ref _pingCounter);
                        _connection.LogExceptions(_connection.SendPingAsync(payload, isAck: false));
                        _pingSentTimestamp = now;
                        _status = Status.PingSent;
                    }
                }
            }

            internal void OnPingAck(long payload)
            {
                Debug.Assert(payload < 0);
                if (_staticRtt.HasValue) return;

                if (Interlocked.Read(ref _pingCounter) != payload)
                    ThrowProtocolError();

                long elapsedTicks = Stopwatch.GetTimestamp() - _pingSentTimestamp;
                Rtt = TimeSpan.FromSeconds(elapsedTicks / (double)Stopwatch.Frequency);
                _connection.TraceFlowControl($"Updated RTT: {Rtt.TotalMilliseconds} ms");
                _status = Status.Waiting;
            }
        }
    }
}
