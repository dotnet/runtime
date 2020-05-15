#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Threading;
using System.Threading.Channels;

namespace System.Net.Quic.Implementations.Managed
{
    /// <summary>
    ///     Collection of Quic streams.
    /// </summary>
    internal sealed class StreamCollection
    {
        /// <summary>
        ///     All streams by their id;
        /// </summary>
        private ImmutableDictionary<long, ManagedQuicStream> _streams = ImmutableDictionary<long, ManagedQuicStream>.Empty;

        /// <summary>
        ///     Number of total streams by their type.
        /// </summary>
        private readonly int[] _streamCounts = new int[4];

        /// <summary>
        ///     All streams which are flushable (have data to send).
        /// </summary>
        private readonly LinkedList<ManagedQuicStream> _flushable = new LinkedList<ManagedQuicStream>();

        /// <summary>
        ///     All streams which require updating flow control bounds.
        /// </summary>
        private readonly LinkedList<ManagedQuicStream> _updateQueue = new LinkedList<ManagedQuicStream>();

        /// <summary>
        ///     Channel of streams that were opened by the peer but not yet accepted by this endpoint.
        /// </summary>
        internal Channel<ManagedQuicStream> IncomingStreams { get; } =
            Channel.CreateUnbounded<ManagedQuicStream>(new UnboundedChannelOptions()
            {
                SingleReader = true, SingleWriter = true
            });

        internal ManagedQuicStream this[long streamId] => _streams[streamId];


        internal ManagedQuicStream? GetFirstFlushableStream()
        {
            lock (_flushable)
            {
                var first = _flushable.First;
                if (first == null)
                {
                    return null;
                }

                _flushable.RemoveFirst();
                return first.Value;
            }
        }

        internal ManagedQuicStream? GetFirstStreamForUpdate()
        {
            lock (_updateQueue)
            {
                var first = _updateQueue.First;
                if (first == null)
                {
                    return null;
                }

                _updateQueue.RemoveFirst();
                return first.Value;
            }
        }

        internal ManagedQuicStream GetOrCreateStream(long streamId, in TransportParameters localParams,
            in TransportParameters remoteParams, bool isLocal, ManagedQuicConnection connection)
        {
            var type = StreamHelpers.GetStreamType(streamId);
            long index = StreamHelpers.GetStreamIndex(streamId);
            bool unidirectional = !StreamHelpers.IsBidirectional(streamId);

            // create also all lower-numbered streams
            while (_streamCounts[(int)type] <= index)
            {
                long nextId = StreamHelpers.ComposeStreamId(type, _streamCounts[(int)type]);

                var stream = CreateStream(nextId, isLocal, unidirectional, localParams, remoteParams, connection);
                if (ImmutableInterlocked.TryAdd(ref _streams, nextId, stream))
                {
                    Interlocked.Increment(ref _streamCounts[(int)type]);
                }

                if (!isLocal)
                {
                    bool success = IncomingStreams.Writer.TryWrite(stream);
                    // reserving space should be assured by connection stream limits
                    Debug.Assert(success, "Failed to write into IncomingStreams");
                }
            }

            return _streams[streamId];
        }

        private ManagedQuicStream CreateStream(long streamId,
            bool isLocal, bool unidirectional, TransportParameters localParams, TransportParameters remoteParams,
            ManagedQuicConnection connection)
        {
            // use initial flow control limits
            (long? maxDataInbound, long? maxDataOutbound) = (isLocal, unidirectional) switch
            {
                // local unidirectional
                (true, true) => ((long?)null, (long?)remoteParams.InitialMaxStreamDataUni),
                // local bidirectional
                (true, false) => ((long?)localParams.InitialMaxStreamDataBidiLocal, (long?)remoteParams.InitialMaxStreamDataBidiRemote),
                // remote unidirectional
                (false, true) => ((long?)localParams.InitialMaxStreamDataUni, (long?)null),
                // remote bidirectional
                (false, false) => ((long?)localParams.InitialMaxStreamDataBidiRemote, (long?)remoteParams.InitialMaxStreamDataBidiLocal),
            };

            InboundBuffer? inboundBuffer = maxDataInbound != null
                ? new InboundBuffer(maxDataInbound.Value)
                : null;

            OutboundBuffer? outboundBuffer = maxDataOutbound != null
                ? new OutboundBuffer(maxDataOutbound.Value)
                : null;

            return new ManagedQuicStream(streamId, inboundBuffer, outboundBuffer, connection);
        }

        internal void MarkFlushable(ManagedQuicStream stream)
        {
            Debug.Assert(stream.CanWrite);

            AddToListSynchronized(_flushable, stream._flushableListNode);
        }

        internal void MarkForUpdate(ManagedQuicStream stream)
        {
            AddToListSynchronized(_updateQueue, stream._updateQueueListNode);
        }

        private static void AddToListSynchronized(LinkedList<ManagedQuicStream> list, LinkedListNode<ManagedQuicStream> node)
        {
            // use double checking to prevent frequent locking
            if (node.List == null)
            {
                lock (list)
                {
                    if (node.List == null)
                        list.AddLast(node);
                }
            }
        }

        internal long GetStreamCount(StreamType type)
        {
            return _streamCounts[(int)type];
        }

        internal IEnumerable<ManagedQuicStream> AllStreams => _streams.Values;
    }
}
