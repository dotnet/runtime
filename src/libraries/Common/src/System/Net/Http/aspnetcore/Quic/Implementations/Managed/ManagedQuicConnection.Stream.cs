using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Threading;

namespace System.Net.Quic.Implementations.Managed
{
    internal partial class ManagedQuicConnection
    {
        private struct ConnectionFlowControlLimits
        {
            private long _maxData;

            /// <summary>
            ///     Maximum amount of data the endpoint is allowed to send.
            /// </summary>
            internal long MaxData => _maxData;

            internal void UpdateMaxData(long value)
            {
                _maxData = Math.Max(_maxData, value);
            }

            internal void AddMaxData(long value)
            {
                Interlocked.Add(ref _maxData, value);
            }

            /// <summary>
            ///     Maximum number of bidirectional streams the endpoint is allowed to open.
            /// </summary>
            internal long MaxStreamsBidi { get; private set; }

            internal void UpdateMaxStreamsBidi(long value)
            {
                MaxStreamsBidi = Math.Max(MaxStreamsBidi, value);
            }

            /// <summary>
            ///     Maximum number of unidirectional streams the endpoint is allowed to open.
            /// </summary>
            internal long MaxStreamsUni { get; private set; }

            internal void UpdateMaxStreamsUni(long value)
            {
                MaxStreamsUni = Math.Max(MaxStreamsUni, value);
            }
        }

        /// <summary>
        ///     True if packet with <see cref="MaxDataFrame"/> is waiting for acknowledgement.
        /// </summary>
        private bool MaxDataFrameSent { get; set; }

        /// <summary>
        ///     Sum of maximum offsets of data sent across all streams.
        /// </summary>
        private long SentData { get; set; }

        /// <summary>
        ///     Sum of maximum offsets of data received across all streams.
        /// </summary>
        private long ReceivedData { get; set; }

        /// <summary>
        ///     Opens a new outbound stream with lowest available stream id.
        /// </summary>
        /// <param name="unidirectional">True if the stream should be unidirectional.</param>
        /// <returns></returns>
        internal ManagedQuicStream OpenStream(bool unidirectional)
        {
            var type = StreamHelpers.GetLocallyInitiatedType(_isServer, unidirectional);
            long limit = unidirectional ? _peerLimits.MaxStreamsUni : _peerLimits.MaxStreamsBidi;

            if (_streams.GetStreamCount(type) == limit)
            {
                // TODO-RZ: use messages from string resources
                throw new QuicException("Cannot open stream");
            }

            return _streams.CreateOutboundStream(type, _localTransportParameters, _peerTransportParameters, this);
        }

        /// <summary>
        ///     Gets a stream with given id. Use in cases where you are sure the stream exists.
        /// </summary>
        /// <param name="streamId">Id of the stream.</param>
        /// <returns>The stream associated with provided id.</returns>
        private ManagedQuicStream GetStream(long streamId)
        {
            return _streams[streamId];
        }

        /// <summary>
        ///     Tries to get the stream with given id. Creates also all streams of the same type with lower id. Returns
        ///     false if creating the remote initiated stream would violate stream limits imposed by this endpoint.
        /// </summary>
        /// <param name="streamId">Id of the stream to get or create.</param>
        /// <param name="stream">Contains the result stream, or null if operation failed.</param>
        /// <returns></returns>
        private bool TryGetOrCreateStream(long streamId, out ManagedQuicStream? stream)
        {
            // check whether the stream can be opened based on local limits
            if (!StreamHelpers.IsLocallyInitiated(_isServer, streamId))
            {
                long index = StreamHelpers.GetStreamIndex(streamId);
                long limit = StreamHelpers.IsBidirectional(streamId)
                    ? _localLimits.MaxStreamsBidi
                    : _localLimits.MaxStreamsUni;

                if (index > limit)
                {
                    stream = null;
                    return false;
                }
            }

            stream = _streams.GetOrCreateStream(streamId, _localTransportParameters, _peerTransportParameters, _isServer, this);
            return true;
        }

        internal ManagedQuicStream? AcceptStream()
        {
            _streams.IncomingStreams.Reader.TryRead(out var stream);
            return stream;
        }

        internal void OnStreamDataWritten(ManagedQuicStream stream)
        {
            _streams.MarkFlushable(stream);
            _socketContext.Ping();
        }

        internal void OnStreamDataRead(ManagedQuicStream stream, int bytesRead)
        {
            _localLimits.AddMaxData(bytesRead);
            _streams.MarkForFlowControlUpdate(stream);
        }
    }
}
