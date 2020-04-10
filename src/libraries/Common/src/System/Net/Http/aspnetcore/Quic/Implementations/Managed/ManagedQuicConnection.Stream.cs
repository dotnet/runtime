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
            internal ulong MaxData { get; private set; }

            internal void UpdateMaxData(ulong value)
            {
                MaxData = Math.Max(MaxData, value);
            }

            /// <summary>
            ///     Maximum number of bidirectional streams the endpoint is allowed to open.
            /// </summary>
            internal ulong MaxStreamsBidi { get; private set; }

            internal void UpdateMaxStreamsBidi(ulong value)
            {
                MaxStreamsBidi = Math.Max(MaxStreamsBidi, value);
            }

            /// <summary>
            ///     Maximum number of unidirectional streams the endpoint is allowed to open.
            /// </summary>
            internal ulong MaxStreamsUni { get; private set; }

            internal void UpdateMaxStreamsUni(ulong value)
            {
                MaxStreamsUni = Math.Max(MaxStreamsUni, value);
            }
        }

        internal ManagedQuicStream OpenStream(bool unidirectional)
        {
            // TODO-RZ assert that we really can open the stream (respect the limits)
            var type = StreamHelpers.GetLocallyInitiatedType(_isServer, unidirectional);
            return _streams.CreateOutboundStream(_isServer, unidirectional, _localTransportParameters, _peerTransportParameters);
        }

        internal ManagedQuicStream AcceptStream()
        {
            _streams.IncomingStreams.Reader.TryRead(out var stream);
            return stream;
        }
    }
}
