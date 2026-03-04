// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO.Hashing
{
    /// <summary>
    ///   Provides an implementation of the Adler-32 checksum algorithm, as specified in
    ///   <see href="https://www.rfc-editor.org/rfc/rfc1950">RFC 1950</see>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This algorithm produces a 32-bit checksum and is commonly used in
    ///     data compression formats such as zlib. It is not suitable for cryptographic purposes.
    ///   </para>
    /// </remarks>
    public sealed partial class Adler32 : NonCryptographicHashAlgorithm
    {
        private const uint InitialState = 1u;
        private const int Size = sizeof(uint);
        private uint _adler = InitialState;

        /// <summary>Largest prime smaller than 65536.</summary>
        internal const uint ModBase = 65521;
        /// <summary>NMax is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) &lt;= 2^32-1</summary>
        private const int NMax = 5552;

        /// <summary>
        /// Initializes a new instance of the <see cref="Adler32"/> class.
        /// </summary>
        public Adler32()
            : base(Size)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Adler32"/> class using the state from another instance.
        /// </summary>
        private Adler32(uint adler)
            : base(Size)
            => _adler = adler;

        /// <summary>
        /// Returns a clone of the current instance, with a copy of the current instance's internal state.
        /// </summary>
        /// <returns>
        /// A new instance that will produce the same sequence of values as the current instance.
        /// </returns>
        public Adler32 Clone()
            => new(_adler);

        /// <summary>
        /// Appends the contents of <paramref name="source"/> to the data already
        /// processed for the current hash computation.
        /// </summary>
        /// <param name="source">The data to process.</param>
        public override void Append(ReadOnlySpan<byte> source)
            => _adler = Update(_adler, source);

        /// <summary>
        /// Resets the hash computation to the initial state.
        /// </summary>
        public override void Reset()
            => _adler = InitialState;

        /// <summary>
        /// Writes the computed hash value to <paramref name="destination"/>
        /// without modifying accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        protected override void GetCurrentHashCore(Span<byte> destination)
            => BinaryPrimitives.WriteUInt32BigEndian(destination, _adler);

        /// <summary>
        /// Writes the computed hash value to <paramref name="destination"/>
        /// then clears the accumulated state.
        /// </summary>
        protected override void GetHashAndResetCore(Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, _adler);
            _adler = InitialState;
        }

        /// <summary>
        /// Gets the current computed hash value without modifying accumulated state.
        /// </summary>
        /// <returns>
        /// The hash value for the data already provided.
        /// </returns>
        [CLSCompliant(false)]
        public uint GetCurrentHashAsUInt32()
            => _adler;

        /// <summary>
        /// Computes the Adler-32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The Adler-32 hash of the provided data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] Hash(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return Hash(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Computes the Adler-32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The Adler-32 hash of the provided data.</returns>
        public static byte[] Hash(ReadOnlySpan<byte> source)
        {
            byte[] ret = new byte[Size];
            uint hash = HashToUInt32(source);
            BinaryPrimitives.WriteUInt32BigEndian(ret, hash);
            return ret;
        }

        /// <summary>
        /// Attempts to compute the Adler-32 hash of the provided data into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="bytesWritten">
        /// On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="destination"/> is long enough to receive
        /// the computed hash value (4 bytes); otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < Size)
            {
                bytesWritten = 0;
                return false;
            }

            uint hash = HashToUInt32(source);
            BinaryPrimitives.WriteUInt32BigEndian(destination, hash);
            bytesWritten = Size;
            return true;
        }

        /// <summary>
        /// Computes the Adler-32 hash of the provided data into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        public static int Hash(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < Size)
            {
                ThrowDestinationTooShort();
            }

            uint hash = HashToUInt32(source);
            BinaryPrimitives.WriteUInt32BigEndian(destination, hash);
            return Size;
        }

        /// <summary>
        /// Computes the Adler-32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>
        /// The computed Adler-32 hash.
        /// </returns>
        [CLSCompliant(false)]
        public static uint HashToUInt32(ReadOnlySpan<byte> source)
            => Update(InitialState, source);

        private static uint Update(uint adler, ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty)
            {
                return adler;
            }

#if NET
            if (IsVectorizable(source))
            {
                return UpdateVectorized(adler, source);
            }
#endif

            return UpdateScalar(adler, source);
        }

        private static uint UpdateScalar(uint adler, ReadOnlySpan<byte> source)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;
            Debug.Assert(!source.IsEmpty);

            do
            {
                int k = source.Length < NMax ? source.Length : NMax;
                foreach (byte b in source.Slice(0, k))
                {
                    s1 += b;
                    s2 += s1;
                }

                s1 %= ModBase;
                s2 %= ModBase;
                source = source.Slice(k);
            }
            while (source.Length > 0);

            return (s2 << 16) | s1;
        }
    }
}
