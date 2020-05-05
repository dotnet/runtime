#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class ConnectionIdCollection
    {
        // _lookup of connection ids based on the sequence number
        private ImmutableDictionary<long, ConnectionId> _connectionIds;

        public ConnectionIdCollection()
        {
            _connectionIds = ImmutableDictionary<long, ConnectionId>.Empty;
        }

        public void Add(ConnectionId connectionId)
        {
            var originalValue = Volatile.Read(ref _connectionIds);
            foreach (var (_, id) in originalValue)
            {
                if (id.Data.AsSpan().StartsWith(connectionId.Data))
                {
                    throw new InvalidOperationException("New connection id must not be a prefix of an existing one");
                }
            }

            ImmutableInterlocked.TryAdd(ref _connectionIds, connectionId.SequenceNumber, connectionId);
        }

        public ConnectionId? FindBySequenceNumber(long sequenceNumber)
        {
            _connectionIds.TryGetValue(sequenceNumber, out var connectionId);
            return connectionId;
        }

        /// <summary>
        ///     Checks if the provided connection id is present in the collection.
        /// </summary>
        /// <param name="dcidSpan"></param>
        /// <returns></returns>
        public ConnectionId? Find(in ReadOnlySpan<byte> dcidSpan)
        {
            // TODO-RZ Aho-Corassick might be more efficient here
            foreach (var (_, connectionId) in _connectionIds)
            {
                if (dcidSpan.StartsWith(connectionId.Data))
                {
                    return connectionId;
                }
            }

            return null;
        }

        public void Remove(ConnectionId connectionId)
        {
            ImmutableInterlocked.TryRemove(ref _connectionIds, connectionId.SequenceNumber, out var removed);
            Debug.Assert(removed.Equals(connectionId));
        }
    }
}
