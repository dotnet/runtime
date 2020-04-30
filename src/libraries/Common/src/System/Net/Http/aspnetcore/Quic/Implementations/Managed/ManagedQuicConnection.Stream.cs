using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;

namespace System.Net.Quic.Implementations.Managed
{
    internal partial class ManagedQuicConnection
    {
        private struct ConnectionFlowControlLimits
        {
            /// <summary>
            ///     Maximum amount of data the endpoint is allowed to send.
            /// </summary>
            internal long MaxData { get; private set; }

            internal void UpdateMaxData(long value)
            {
                MaxData = Math.Max(MaxData, value);
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
        ///     Opens a new outbound stream with lowest available stream id.
        /// </summary>
        /// <param name="unidirectional">True if the stream should be unidirectional.</param>
        /// <returns></returns>
        /// <exception cref="QuicException"></exception>
        internal ManagedQuicStream OpenStream(bool unidirectional)
        {
            var type = StreamHelpers.GetLocallyInitiatedType(_isServer, unidirectional);
            long limit = unidirectional ? _peerLimits.MaxStreamsUni : _peerLimits.MaxStreamsBidi;

            if (_streams.GetStreamCount(type) == limit)
            {
                // TODO-RZ: use messages from string resources
                throw new QuicException("Cannot open stream");
            }

            return _streams.CreateOutboundStream(type, _localTransportParameters, _peerTransportParameters, _socketContext);
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

            stream = _streams.GetOrCreateStream(streamId, _localTransportParameters, _peerTransportParameters, _isServer,
                _socketContext);
            return true;
        }

        internal ManagedQuicStream? AcceptStream()
        {
            _streams.IncomingStreams.Reader.TryRead(out var stream);
            return stream;
        }
    }
}
