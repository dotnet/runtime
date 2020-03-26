using System.Collections.Generic;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class ConnectionId
    {
        public ConnectionId(byte[] data)
        {
            Data = data;
        }

        internal byte[] Data { get; }
    }

    internal class ConnectionIdCollection
    {
        private readonly List<ConnectionId> _connectionIds;

        public ConnectionIdCollection()
        {
            _connectionIds = new List<ConnectionId>();
        }

        public void Add(byte[] connectionId)
        {
            foreach (var id in _connectionIds)
            {
                if (id.Data.AsSpan().StartsWith(connectionId))
                {
                    throw new InvalidOperationException("New connection id must not be a prefix of an existing one");
                }
            }

            _connectionIds.Add(new ConnectionId(connectionId));
        }

        /// <summary>
        ///     Checks if the provided connection id is present in the collection.
        /// </summary>
        /// <param name="dcidSpan"></param>
        /// <returns></returns>
        public ConnectionId? FindConnectionId(in ReadOnlySpan<byte> dcidSpan)
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
    }
}
