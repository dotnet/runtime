#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

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
            var originalValue = Volatile.Read(ref _connectionIds);
            foreach (var id in originalValue)
            {
                if (id.Data.AsSpan().StartsWith(connectionId.Data))
                {
                    throw new InvalidOperationException("New connection id must not be a prefix of an existing one");
                }
            }

            bool success;
            do
            {
                var updated = originalValue.Add(connectionId);
                var interlockedResult = Interlocked.CompareExchange(ref _connectionIds, updated, originalValue);
                success = originalValue == interlockedResult;
                originalValue = interlockedResult;
            } while (!success);
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
            var originalValue = Volatile.Read(ref _connectionIds);

            bool success;
            do
            {
                var updated = originalValue.Remove(connectionId);
                var interlockedResult = Interlocked.CompareExchange(ref _connectionIds, updated, originalValue);
                success = originalValue == interlockedResult;
                originalValue = interlockedResult;
            } while (!success);
        }
    }
}
