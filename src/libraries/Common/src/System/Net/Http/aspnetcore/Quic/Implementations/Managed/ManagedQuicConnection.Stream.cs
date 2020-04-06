using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;

namespace System.Net.Quic.Implementations.Managed
{
    internal partial class ManagedQuicConnection
    {
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

            // use initial outbound limits
            OutboundBuffer outboundBuffer = new OutboundBuffer(unidirectional
                ? _peerTransportParameters.InitialMaxStreamDataUni
                : _peerTransportParameters.InitialMaxStreamDataBidiRemote);

            var stream = new ManagedQuicStream(streamId, unidirectional ? null : new InboundBuffer(), outboundBuffer);
            streamList.Add(stream);


            return stream;
        }
    }
}
