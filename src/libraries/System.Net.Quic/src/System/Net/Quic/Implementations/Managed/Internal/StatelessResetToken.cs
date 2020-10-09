// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Token used to authorize a Connection id when attempting to connect to a known host.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct StatelessResetToken : IEquatable<StatelessResetToken>
    {
        private static readonly Random _random = new Random();

        /// <summary>
        ///     Length of the reset token.
        /// </summary>
        internal const int Length = 128/8;

        /// <summary>
        ///     First 8 bytes of the reset token.
        /// </summary>
        internal readonly long LowerHalf;

        /// <summary>
        ///     Second 8 bytes of the reset token.
        /// </summary>
        internal readonly long UpperHalf;

        public StatelessResetToken(long lowerHalf, long upperHalf)
        {
            LowerHalf = lowerHalf;
            UpperHalf = upperHalf;
        }

        internal static StatelessResetToken Random()
        {
            Span<byte> bytes = stackalloc byte[16];
            lock (_random)
            {
                _random.NextBytes(bytes);
            }

            return FromSpan(bytes);
        }

        internal static StatelessResetToken FromSpan(ReadOnlySpan<byte> token)
        {
            return MemoryMarshal.Read<StatelessResetToken>(token);
        }

        internal static void ToSpan(Span<byte> destination, in StatelessResetToken token)
        {
            MemoryMarshal.AsRef<StatelessResetToken>(destination) = token;
        }

        public bool Equals(StatelessResetToken other) => LowerHalf == other.LowerHalf && UpperHalf == other.UpperHalf;

        public override bool Equals(object? obj) => obj is StatelessResetToken other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(LowerHalf, UpperHalf);

        public static bool operator ==(in StatelessResetToken left, in StatelessResetToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatelessResetToken left, StatelessResetToken right)
        {
            return !(left == right);
        }
    }
}
