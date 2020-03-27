namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class ConnectionId : IEquatable<ConnectionId>
    {
        public ConnectionId(byte[] data)
        {
            Data = data;
        }

        internal byte[] Data { get; }

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

        public override int GetHashCode() => Data.GetHashCode();
    }
}
