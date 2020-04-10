using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Threading.Channels;

namespace System.Net.Quic.Implementations.Managed
{
    /// <summary>
    ///     Collection of Quic streams.
    /// </summary>
    internal sealed class StreamCollection
    {
        /// <summary>
        ///     All streams organized by the stream type.
        /// </summary>
        private readonly List<ManagedQuicStream?>[] _streams =
        {
            new List<ManagedQuicStream?>(),
            new List<ManagedQuicStream?>(),
            new List<ManagedQuicStream?>(),
            new List<ManagedQuicStream?>()
        };

        /// <summary>
        ///     All streams which are flushable (have data to send).
        /// </summary>
        private readonly HashSet<ManagedQuicStream> _flushable = new HashSet<ManagedQuicStream>();

        /// <summary>
        ///     Channel of streams that were opened by the peer but not yet accepted by this endpoint.
        /// </summary>
        internal Channel<ManagedQuicStream> IncomingStreams { get; } =
            Channel.CreateUnbounded<ManagedQuicStream>(new UnboundedChannelOptions()
            {
                SingleReader = true, SingleWriter = true
            });

        internal ManagedQuicStream this[ulong streamId] =>
            _streams[(int)StreamHelpers.GetStreamType((long)streamId)][(int)StreamHelpers.GetStreamIndex((long) streamId)]!;


        internal ManagedQuicStream? GetFirstFlushableStream()
        {
            return _flushable.FirstOrDefault();
        }

        internal ManagedQuicStream GetOrCreateStream(ulong streamId, in TransportParameters localParams,
            in TransportParameters remoteParams, bool isServer)
        {
            var type = StreamHelpers.GetStreamType((long) streamId);
            // TODO-RZ: allow for long indices
            int index = (int) StreamHelpers.GetStreamIndex((long)streamId);
            bool unidirectional = !StreamHelpers.IsBidirectional((long)streamId);
            bool isLocal = isServer && StreamHelpers.IsServerInitiated((long)streamId);

            var streamList = _streams[(int)type];

            // reserve space in the list
            while (streamList.Count <= index)
            {
                streamList.Add(null);
            }

            var stream = streamList[index];
            if (stream == null)
            {
                stream = streamList[index] ??= CreateStream(streamId, isLocal, unidirectional, localParams, remoteParams);

                if (!isLocal)
                {
                    bool success = IncomingStreams.Writer.TryWrite(stream);
                    // reserving space should be assured by connection stream limits
                    Debug.Assert(success, "Failed to write into IncomingStreams");
                }
            }

            return stream;
        }

        private ManagedQuicStream CreateStream(ulong streamId,
            bool isLocal, bool unidirectional, TransportParameters localParams, TransportParameters remoteParams)
        {
            // use initial flow control limits
            (ulong? maxDataInbound, ulong? maxDataOutbound) = (isLocal, unidirectional) switch
            {
                // local unidirectional
                (true, true) => ((ulong?)null, (ulong?)remoteParams.InitialMaxStreamDataUni),
                // local bidirectional
                (true, false) => ((ulong?)localParams.InitialMaxStreamDataBidiLocal, (ulong?)remoteParams.InitialMaxStreamDataBidiRemote),
                // remote unidirectional
                (false, true) => ((ulong?)localParams.InitialMaxStreamDataUni, (ulong?)null),
                // remote bidirectional
                (false, false) => ((ulong?)localParams.InitialMaxStreamDataBidiRemote, (ulong?)remoteParams.InitialMaxStreamDataBidiLocal),
            };

            InboundBuffer? inboundBuffer = maxDataInbound != null
                ? new InboundBuffer(maxDataInbound.Value)
                : null;

            OutboundBuffer? outboundBuffer = maxDataOutbound != null
                ? new OutboundBuffer(maxDataOutbound.Value)
                : null;

            return new ManagedQuicStream((long) streamId, inboundBuffer, outboundBuffer, this);
        }

        internal ManagedQuicStream CreateOutboundStream(bool isServer, bool unidirectional, in TransportParameters localParams, in TransportParameters remoteParams)
        {
            var type = StreamHelpers.GetLocallyInitiatedType(isServer, unidirectional);

            var streamList = _streams[(int)type];
            long streamId = StreamHelpers.ComposeStreamId(type, streamList.Count);

            var stream = CreateStream((ulong)streamId, true, unidirectional, localParams, remoteParams);
            streamList.Add(stream);
            return stream;
        }

        public void MarkFlushable(ManagedQuicStream stream, bool flushable)
        {
            if (flushable)
                _flushable.Add(stream);
            else
                _flushable.Remove(stream);
        }
    }
}
