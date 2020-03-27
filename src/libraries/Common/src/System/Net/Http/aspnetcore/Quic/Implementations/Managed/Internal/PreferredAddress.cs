using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Structure representing servers preferred address.
    /// </summary>
    internal readonly struct PreferredAddress
    {
        /// <summary>
        ///     The server's IPv4 address. May be <see cref="IPAddress.Any"/> if server does not wish to advertise IPv4 address.
        /// </summary>
        internal readonly IPEndPoint IPv4Address;

        /// <summary>
        ///     The server's IPv6 address. May be <see cref="IPAddress.IPv6Any"/> if server does not wish to advertise IPv4 address.
        /// </summary>
        internal readonly IPEndPoint IPv6Address;

        /// <summary>
        ///     Connection Id to be used as the destination connection id to packets sent to the preferred address.
        /// </summary>
        internal readonly byte[] ConnectionId;

        /// <summary>
        ///     Stateless reset token to use when connecting to the preferred address.
        /// </summary>
        internal readonly StatelessResetToken StatelessResetToken;

        public PreferredAddress(IPEndPoint pv4Address, IPEndPoint pv6Address, byte[] connectionId, StatelessResetToken statelessResetToken)
        {
            IPv4Address = pv4Address;
            IPv6Address = pv6Address;
            ConnectionId = connectionId;
            StatelessResetToken = statelessResetToken;
        }

        internal static bool Read(QuicReader reader, out PreferredAddress address)
        {
            if (!reader.TryReadSpan(4, out var ipv4) ||
                !reader.TryReadUInt16(out ushort ipv4Port) ||
                !reader.TryReadSpan(16, out var ipv6) ||
                !reader.TryReadUInt16(out ushort ipv6Port) ||
                !reader.TryReadUInt8(out byte cidLength) ||
                !reader.TryReadSpan(cidLength, out var cid) ||
                !reader.TryReadSpan(Frames.StatelessResetToken.Length, out var token))
            {
                address = default;
                return false;
            }

            address = new PreferredAddress(
                new IPEndPoint(new IPAddress(ipv4), ipv4Port),
                new IPEndPoint(new IPAddress(ipv6), ipv6Port),
                cid.ToArray(),
                StatelessResetToken.FromSpan(token));
            return true;
        }

        internal static void Write(QuicWriter writer, in PreferredAddress address)
        {
            address.IPv4Address.Address.TryWriteBytes(writer.GetWritableSpan(4), out int written);
            if (written != 4) throw new InvalidOperationException("Error writing preferred address");
            writer.WriteUInt16((ushort) address.IPv4Address.Port);

            address.IPv6Address.Address.TryWriteBytes(writer.GetWritableSpan(16), out written);
            if (written != 4) throw new InvalidOperationException("Error writing preferred address");
            writer.WriteUInt16((ushort) address.IPv4Address.Port);

            Debug.Assert(address.ConnectionId.Length <= byte.MaxValue);
            writer.WriteUInt8((byte) address.ConnectionId.Length);
            writer.WriteSpan(address.ConnectionId);

            StatelessResetToken.ToSpan(writer.GetWritableSpan(StatelessResetToken.Length), address.StatelessResetToken);
        }
    }
}
