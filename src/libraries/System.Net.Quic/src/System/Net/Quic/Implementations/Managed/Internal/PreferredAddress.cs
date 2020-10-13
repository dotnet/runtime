// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Structure representing servers preferred address.
    /// </summary>
    internal readonly struct PreferredAddress
    {
        internal const int MinimumSerializedLength = 41;

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

        internal static bool Read(ReadOnlySpan<byte> buffer, out PreferredAddress address)
        {
            // the only non-fixed length field is the connection id the rest of the parameter is 41 bytes
            if (buffer.Length < 43)
            {
                address = default;
                return false;
            }

            var ipv4 = buffer.Slice(0, 4);
            buffer = buffer.Slice(ipv4.Length);

            ushort ipv4Port = BinaryPrimitives.ReadUInt16BigEndian(buffer);
            buffer = buffer.Slice(sizeof(ushort));

            var ipv6 = buffer.Slice(0, 16);
            buffer = buffer.Slice(ipv6.Length);

            ushort ipv6Port = BinaryPrimitives.ReadUInt16BigEndian(buffer);
            buffer = buffer.Slice(sizeof(ushort));

            int cidLength = buffer[0];
            buffer.Slice(1);

            // the expected length is now known
            if (buffer.Length != cidLength + Internal.StatelessResetToken.Length)
            {
                address = default;
                return false;
            }

            var cid = buffer.Slice(0, cidLength);
            var token = buffer.Slice(cidLength);

            address = new PreferredAddress(
                new IPEndPoint(new IPAddress(ipv4), ipv4Port),
                new IPEndPoint(new IPAddress(ipv6), ipv6Port),
                cid.ToArray(),
                StatelessResetToken.FromSpan(token));
            return true;
        }

        internal static int Write(Span<byte> buffer, in PreferredAddress address)
        {
            int written = 0;

            // IPv4 address
            address.IPv4Address.Address.TryWriteBytes(buffer, out int w);
            written += w;
            if (w != 4) throw new InvalidOperationException("Error writing preferred address");
            BinaryPrimitives.WriteInt16BigEndian(buffer.Slice(written), (short)address.IPv4Address.Port);
            written += sizeof(short);

            // IPv6 address
            address.IPv6Address.Address.TryWriteBytes(buffer.Slice(written), out w);
            written += w;
            if (w != 16) throw new InvalidOperationException("Error writing preferred address");
            BinaryPrimitives.WriteInt16BigEndian(buffer.Slice(written), (short)address.IPv6Address.Port);
            written += sizeof(short);

            // connection id
            Debug.Assert(address.ConnectionId.Length <= byte.MaxValue);
            buffer[written++] = (byte) address.ConnectionId.Length;
            address.ConnectionId.AsSpan().CopyTo(buffer.Slice(written));
            written += address.ConnectionId.Length;

            // Stateless reset token
            StatelessResetToken.ToSpan(buffer.Slice(written), address.StatelessResetToken);
            written += StatelessResetToken.Length;

            return written;
        }
    }
}
