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
            internal ulong MaxData { get; set; }

            /// <summary>
            ///     Maximum number of streams the endpoint can open.
            /// </summary>
            internal ulong MaxStreams { get; set; }

            /// <summary>
            ///     Maximum number of bidirectional streams the endpoint is allowed to open.
            /// </summary>
            internal ulong MaxStreamsBidi { get; set; }

            /// <summary>
            ///     Maximum number of unidirectional streams the endpoint is allowed to open.
            /// </summary>
            internal ulong MaxStreamsUni { get; set; }
        }

        private ManagedQuicStream GetStream(ulong streamId)
        {
            List<ManagedQuicStream> streamList = _streams[(int)StreamHelpers.GetStreamType((long)streamId)];
            // TODO-RZ: Create stream if it does not exist yet
            return streamList[(int)(streamId >> 2)];
        }

        private ManagedQuicStream OpenStream(bool unidirectional)
        {
            var type = StreamHelpers.GetOutboundType(_isServer, unidirectional);
            var streamList = _streams[(int)type];
            long streamId = StreamHelpers.ComposeStreamId(type, streamList.Count);

            // use initial flow control limits
            OutboundBuffer outboundBuffer = new OutboundBuffer(unidirectional
                ? _peerTransportParameters.InitialMaxStreamDataUni
                : _peerTransportParameters.InitialMaxStreamDataBidiRemote);

            InboundBuffer? inboundBuffer = unidirectional
                ? null
                : new InboundBuffer(_localTransportParameters.InitialMaxStreamDataBidiLocal);

            var stream = new ManagedQuicStream(streamId, inboundBuffer, outboundBuffer);
            streamList.Add(stream);
            return stream;
        }
    }
}
