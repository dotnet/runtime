// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

// Implemented from the specification at
// https://github.com/Cyan4973/xxHash/blob/f9155bd4c57e2270a4ffbb176485e5d713de1c9b/doc/xxhash_spec.md

namespace System.IO.Hashing
{
    /// <summary>
    ///   Provides an implementation of the XxHash32 algorithm.
    /// </summary>
    public sealed partial class XxHash32 : NonCryptographicHashAlgorithm
    {
        private const int HashSize = sizeof(uint);
        private const int StripeSize = 4 * sizeof(uint);

        private readonly uint _seed;
        private State _state;
        private byte[]? _holdback;
        private int _length;

        /// <summary>
        ///   Initializes a new instance of the <see cref="XxHash32"/> class.
        /// </summary>
        /// <remarks>
        ///   The XxHash32 algorithm supports an optional seed value.
        ///   Instances created with this constructor use the default seed, zero.
        /// </remarks>
        public XxHash32()
            : this(0)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="XxHash32"/> class with
        ///   a specified seed.
        /// </summary>
        /// <param name="seed">
        ///   The hash seed value for computations from this instance.
        /// </param>
        public XxHash32(int seed)
            : base(HashSize)
        {
            _seed = (uint)seed;
            Reset();
        }

        /// <summary>
        ///   Resets the hash computation to the initial state.
        /// </summary>
        public override void Reset()
        {
            _state = new State(_seed);
            _length = 0;
        }

        /// <summary>
        ///   Appends the contents of <paramref name="source"/> to the data already
        ///   processed for the current hash computation.
        /// </summary>
        /// <param name="source">The data to process.</param>
        public override void Append(ReadOnlySpan<byte> source)
        {
            // Every time we've read 16 bytes, process the stripe.
            // Data that isn't perfectly mod-16 gets stored in a holdback
            // buffer.

            int held = _length & 0x0F;

            if (held != 0)
            {
                int remain = StripeSize - held;

                if (source.Length > remain)
                {
                    source.Slice(0, remain).CopyTo(_holdback.AsSpan(held));
                    _state.ProcessStripe(_holdback);

                    source = source.Slice(remain);
                    _length += remain;
                }
                else
                {
                    source.CopyTo(_holdback.AsSpan(held));
                    _length += source.Length;
                    return;
                }
            }

            while (source.Length >= StripeSize)
            {
                _state.ProcessStripe(source);
                source = source.Slice(StripeSize);
                _length += StripeSize;
            }

            if (source.Length > 0)
            {
                _holdback ??= new byte[StripeSize];
                source.CopyTo(_holdback);
                _length += source.Length;
            }
        }

        /// <summary>
        ///   Writes the computed hash value to <paramref name="destination"/>
        ///   without modifying accumulated state.
        /// </summary>
        protected override void GetCurrentHashCore(Span<byte> destination)
        {
            int remainingLength = _length & 0x0F;
            ReadOnlySpan<byte> remaining = ReadOnlySpan<byte>.Empty;

            if (remainingLength > 0)
            {
                remaining = new ReadOnlySpan<byte>(_holdback, 0, remainingLength);
            }

            uint acc = _state.Complete(_length, remaining);
            BinaryPrimitives.WriteUInt32BigEndian(destination, acc);
        }

        /// <summary>
        ///   Computes the XxHash32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The XxHash32 hash of the provided data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] Hash(byte[] source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Hash(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Computes the XxHash32 hash of the provided data using the provided seed.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation.</param>
        /// <returns>The XxHash32 hash of the provided data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public static byte[] Hash(byte[] source, int seed)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return Hash(new ReadOnlySpan<byte>(source), seed);
        }

        /// <summary>
        ///   Computes the XxHash32 hash of the provided data.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>The XxHash32 hash of the provided data.</returns>
        public static byte[] Hash(ReadOnlySpan<byte> source, int seed = 0)
        {
            byte[] ret = new byte[HashSize];
            StaticHash(source, ret, seed);
            return ret;
        }

        /// <summary>
        ///   Attempts to compute the XxHash32 hash of the provided data into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is long enough to receive
        ///   the computed hash value (4 bytes); otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int seed = 0)
        {
            if (destination.Length < HashSize)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = StaticHash(source, destination, seed);
            return true;
        }

        /// <summary>
        ///   Computes the XxHash32 hash of the provided data into the provided destination.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        public static int Hash(ReadOnlySpan<byte> source, Span<byte> destination, int seed = 0)
        {
            if (destination.Length < HashSize)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            return StaticHash(source, destination, seed);
        }

        private static int StaticHash(ReadOnlySpan<byte> source, Span<byte> destination, int seed)
        {
            int totalLength = source.Length;
            State state = new State((uint)seed);

            while (source.Length >= StripeSize)
            {
                state.ProcessStripe(source);
                source = source.Slice(StripeSize);
            }

            uint val = state.Complete(totalLength, source);
            BinaryPrimitives.WriteUInt32BigEndian(destination, val);
            return HashSize;
        }
    }
}
