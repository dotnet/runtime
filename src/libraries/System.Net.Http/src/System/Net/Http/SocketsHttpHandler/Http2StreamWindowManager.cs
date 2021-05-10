// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        private const int StreamWindowUpdateRatio = 8;
        private const int InitialStreamWindowSize = 65535;

        private class Http2StreamWindowManager
        {
            // See comment on ConnectionWindowThreshold.
            internal int StreamWindowThreshold => StreamWindowSize / StreamWindowUpdateRatio;

            protected int _delivered;
            protected int _streamWindowSize;

            protected readonly Http2Connection _connection;
            protected readonly Http2Stream _stream;

            public Http2StreamWindowManager(Http2Connection connection, Http2Stream stream)
            {
                _connection = connection;
                _stream = stream;
                _streamWindowSize = InitialStreamWindowSize;

                _stream.Trace($"StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}");
            }

            internal int StreamWindowSize => _streamWindowSize;

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
            private DateTime _lastWindowUpdate = DateTime.Now;

            private const long Magic = 10_000;

            public DynamicHttp2StreamWindowManager(Http2Connection connection, Http2Stream stream)
                : base(connection, stream)
            {
            }

            public override void AdjustWindow(int bytesConsumed)
            {
                _delivered += bytesConsumed;
                if (_delivered < StreamWindowThreshold)
                {
                    return;
                }

                TimeSpan rtt = _connection._rttEstimator!.Rtt;
                TimeSpan dt = DateTime.Now - _lastWindowUpdate;

                int windowSizeIncrement = _delivered;

                if (Magic * _delivered * rtt.Ticks > StreamWindowThreshold * dt.Ticks)
                {
                    int newWindowSize = _streamWindowSize * 2;
                    windowSizeIncrement += newWindowSize - _streamWindowSize;

                    _streamWindowSize = newWindowSize;

                    _stream.Trace($"Updated StreamWindowSize: {StreamWindowSize}, StreamWindowThreshold: {StreamWindowThreshold}");
                }
                else
                {
                    string msg =
                        $"No adjustment! | RTT={rtt.TotalMilliseconds} ms || dt={dt.TotalMilliseconds} ms || " +
                        //$"_delivered * rtt.Ticks = {_delivered * rtt.Ticks} || StreamWindowThreshold * dt.Ticks = {StreamWindowThreshold * dt.Ticks} ||" +
                        $"Magic*_delivered/dt = {Magic* _delivered / dt.TotalSeconds} bytes/sec || StreamWindowThreshold/RTT = {StreamWindowThreshold / rtt.TotalSeconds} bytes/sec";
                    _stream.Trace(msg);
                }

                Task sendWindowUpdateTask = _connection.SendWindowUpdateAsync(_stream.StreamId, windowSizeIncrement);
                _connection.LogExceptions(sendWindowUpdateTask);

                _delivered = 0;
                _lastWindowUpdate = DateTime.Now;
            }
        }

        private class RttEstimator
        {
            public TimeSpan Rtt { get; }
            public RttEstimator(TimeSpan fakeRtt)
            {
                Rtt = fakeRtt;
            }
        }
    }
}
