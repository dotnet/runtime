using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class ConnectionId : IEquatable<ConnectionId>
    {
        // TODO-RZ: remove seed
        private static Random _random = new Random(41);

        public static ConnectionId Random(int length)
        {
            Debug.Assert((uint) length <= 20, "Maximum connection id length is 20");
            var bytes = new byte[length];
            lock (_random)
            {
                _random.NextBytes(bytes);
            }

            return new ConnectionId(bytes);
        }

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
