// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class ConnectionIdCollection
    {
        // array of connection ids kept sorted by sequence number
        private ImmutableArray<ConnectionId> _connectionIds;

        public ConnectionIdCollection()
        {
            _connectionIds = ImmutableArray<ConnectionId>.Empty;
        }

        public void Add(ConnectionId connectionId)
        {
            var originalValue = _connectionIds;
            for (int i = 0; i < originalValue.Length; i++)
            {
                if (originalValue[i].Data.AsSpan().StartsWith(connectionId.Data))
                {
                    throw new InvalidOperationException("New connection id must not be a prefix of an existing one");
                }
            }

            bool success;
            do
            {
                int index = originalValue.BinarySearch(connectionId, ConnectionId.SequenceNumberComparer);
                Debug.Assert(index < 0);
                var newValue = originalValue.Insert(~index, connectionId);
                var interlockedResult =
                    ImmutableInterlocked.InterlockedCompareExchange(ref _connectionIds, newValue, originalValue);
                success = interlockedResult == originalValue;
                originalValue = interlockedResult;
            } while (!success);
        }

        public ConnectionId? FindBySequenceNumber(long sequenceNumber)
        {
            var ids = _connectionIds;
            int index = ids.BinarySearch(new ConnectionId(Array.Empty<byte>(), sequenceNumber, default), ConnectionId.SequenceNumberComparer);
            return index < 0 ? null : ids[index];
        }

        /// <summary>
        ///     Checks if the provided connection id is present in the collection.
        /// </summary>
        /// <param name="dcidSpan"></param>
        /// <returns></returns>
        public ConnectionId? Find(in ReadOnlySpan<byte> dcidSpan)
        {
            // TODO-RZ Aho-Corassick might be more efficient here
            var ids = _connectionIds;
            for (int i = 0; i < ids.Length; i++)
            {
                var connectionId = ids[i];
                if (dcidSpan.StartsWith(connectionId.Data))
                {
                    return connectionId;
                }
            }

            return null;
        }

        public void Remove(ConnectionId connectionId)
        {
            var originalValue = _connectionIds;

            bool success;
            do
            {
                int index = originalValue.BinarySearch(connectionId, ConnectionId.SequenceNumberComparer);
                if (index < 0)
                {
                    return;
                }

                var newValue = originalValue.RemoveAt(index);
                var interlockedResult =
                    ImmutableInterlocked.InterlockedCompareExchange(ref _connectionIds, newValue, originalValue);
                success = interlockedResult == originalValue;
                originalValue = interlockedResult;
            } while (!success);
        }
    }
}
