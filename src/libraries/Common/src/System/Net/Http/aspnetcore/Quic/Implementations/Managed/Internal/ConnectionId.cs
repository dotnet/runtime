#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class ConnectionId : IEquatable<ConnectionId>
    {
        /// <summary>
        ///     Maximum connection id length for the non-version negotiation packets. (Future versions may allow larger
        ///     connection ids)
        /// </summary>
        internal const int MaximumLength = 20;

        public static readonly Comparer<ConnectionId> SequenceNumberComparer = Comparer<ConnectionId>.Create((l,r) => l.SequenceNumber.CompareTo(r.SequenceNumber));

        private static Random _random = new Random(41);

        internal const int DefaultCidSize = MaximumLength;

        public static ConnectionId Random(int length)
        {
            Debug.Assert((uint) length <= 20, "Maximum connection id length is 20");
            var bytes = new byte[length];

            lock (_random)
            {
                _random.NextBytes(bytes);
            }

            // TODO-RZ: generate stateless reset deterministically so it can be checked without state
            return new ConnectionId(bytes, 0, StatelessResetToken.Random());
        }

        public ConnectionId(byte[] data, long sequenceNumber, StatelessResetToken statelessResetToken)
        {
            Data = data;
            SequenceNumber = sequenceNumber;
            StatelessResetToken = statelessResetToken;
        }

        /// <summary>
        ///     Raw data of the connection id.
        /// </summary>
        internal byte[] Data { get; }

        /// <summary>
        ///     The sequence number of the connection id.
        /// </summary>
        internal long SequenceNumber { get; }

        /// <summary>
        ///     Stateless reset token to be used for this connection id.
        /// </summary>
        internal StatelessResetToken StatelessResetToken { get; }

        public bool Equals(ConnectionId? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Data.AsSpan().SequenceEqual(other.Data);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((ConnectionId) obj);
        }

        public override int GetHashCode() => ((IStructuralEquatable)Data).GetHashCode(EqualityComparer<byte>.Default);
    }
}
