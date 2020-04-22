#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class ConnectionIdCollection
    {
        private ImmutableList<ConnectionId> _connectionIds;

        public ConnectionIdCollection()
        {
            _connectionIds = ImmutableList<ConnectionId>.Empty;
        }

        public void Add(ConnectionId connectionId)
        {
            var list = _connectionIds;
            foreach (var id in list)
            {
                if (id.Data.AsSpan().StartsWith(connectionId.Data))
                {
                    throw new InvalidOperationException("New connection id must not be a prefix of an existing one");
                }
            }

            _connectionIds = _connectionIds.Add(connectionId);
        }

        /// <summary>
        ///     Checks if the provided connection id is present in the collection.
        /// </summary>
        /// <param name="dcidSpan"></param>
        /// <returns></returns>
        public ConnectionId? Find(in ReadOnlySpan<byte> dcidSpan)
        {
            // TODO-RZ Aho-Corassick might be more efficient
            foreach (var connectionId in _connectionIds)
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
            _connectionIds = _connectionIds.Remove(connectionId);
        }
    }
}
