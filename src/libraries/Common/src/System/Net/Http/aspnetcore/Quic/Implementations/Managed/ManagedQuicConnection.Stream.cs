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

        internal ManagedQuicStream? AcceptStream()
        {
            _streams.IncomingStreams.Reader.TryRead(out var stream);
            return stream;
        }
    }
}
