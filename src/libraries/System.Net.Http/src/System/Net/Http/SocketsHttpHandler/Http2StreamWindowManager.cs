// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
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

                TimeSpan rtt = _connection._rttEstimator!.Rtt;
                TimeSpan currentTime = _stopwatch.Elapsed;
                TimeSpan dt = currentTime - _lastWindowUpdate;

                int windowSizeIncrement = _delivered;

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

                Task sendWindowUpdateTask = _connection.SendWindowUpdateAsync(_stream.StreamId, windowSizeIncrement);
                _connection.LogExceptions(sendWindowUpdateTask);

                _delivered = 0;
                _lastWindowUpdate = currentTime;

                string GetDiagnostics()
                {
                    return "RTT={rtt.TotalMilliseconds} ms || dt={dt.TotalMilliseconds} ms || " +
                        $"Magic*_delivered/dt = {_magic * _delivered / dt.TotalSeconds} bytes/sec || StreamWindowThreshold/RTT = {StreamWindowThreshold / rtt.TotalSeconds} bytes/sec";
                }
            }
        }

        private class RttEstimator
        {
            private readonly TimeSpan _initialRtt;
            private Http2Connection _connection;
            private readonly Socket? _socket;
            public TimeSpan Rtt { get; private set; }
            public RttEstimator(Http2Connection connection, TimeSpan? fakeRtt, Socket? socket)
            {
                _connection = connection;
                if (fakeRtt.HasValue)
                {
                    Rtt = fakeRtt.Value;
                    _initialRtt = Rtt;
                }
                _socket = socket;
                UpdateEstimation();
            }

#if WINDOWS
            internal void UpdateEstimation()
            {
                if (_socket == null) return;

                if (Interop.Winsock.GetTcpInfoV0(_socket.SafeHandle, out Interop.Winsock._TCP_INFO_v0 tcpInfo) == SocketError.Success)
                {
                    Rtt = TimeSpan.FromTicks(10 * tcpInfo.RttUs);
                    _connection.TraceFlowControl($"Rtt estimation updated: Rtt={Rtt} || (initial fake:{_initialRtt} difference:{Rtt - _initialRtt}");
                }
            }

#else
            internal void UpdateEstimation()
            {
            }
#endif
        }
    }
}
