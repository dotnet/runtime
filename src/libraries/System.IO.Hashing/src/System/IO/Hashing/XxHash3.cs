// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on the XXH3 implementation from https://github.com/Cyan4973/xxHash.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.IO.Hashing.XxHashShared;

namespace System.IO.Hashing
{
    /// <summary>Provides an implementation of the XXH3 hash algorithm for generating a 64-bit hash.</summary>
    /// <remarks>
    /// For methods that persist the computed numerical hash value as bytes,
    /// the value is written in the Big Endian byte order.
    /// </remarks>
    public sealed unsafe partial class XxHash3 : NonCryptographicHashAlgorithm
    {
        /// <summary>XXH3 produces 8-byte hashes.</summary>
        private new const int HashLengthInBytes = 8;

        private State _state;

        /// <summary>Initializes a new instance of the <see cref="XxHash3"/> class using the default seed value 0.</summary>
        public XxHash3() : this(0)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="XxHash3"/> class using the specified seed.</summary>
        public XxHash3(long seed) : base(HashLengthInBytes)
        {
            Initialize(ref _state, (ulong)seed);
        }

        /// <summary>Initializes a new instance of the <see cref="XxHash3"/> class using the state from another instance.</summary>
        private XxHash3(State state) : base(HashLengthInBytes)
        {
            _state = state;
        }

        /// <summary>Returns a clone of the current instance, with a copy of the current instance's internal state.</summary>
        /// <returns>A new instance that will produce the same sequence of values as the current instance.</returns>
        public XxHash3 Clone() => new(_state);

        /// <summary>Computes the XXH3 hash of the provided <paramref name="source"/> data.</summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The XXH3 64-bit hash code of the provided data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static byte[] Hash(byte[] source) => Hash(source, seed: 0);

        /// <summary>Computes the XXH3 hash of the provided data using the provided seed.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation.</param>
        /// <returns>The XXH3 64-bit hash code of the provided data.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static byte[] Hash(byte[] source, long seed)
        {
            ArgumentNullException.ThrowIfNull(source);

            return Hash(new ReadOnlySpan<byte>(source), seed);
        }

        /// <summary>Computes the XXH3 hash of the provided <paramref name="source"/> data using the optionally provided <paramref name="seed"/>.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>The XXH3 64-bit hash code of the provided data.</returns>
        public static byte[] Hash(ReadOnlySpan<byte> source, long seed = 0)
        {
            byte[] result = new byte[HashLengthInBytes];
            ulong hash = HashToUInt64(source, seed);
            BinaryPrimitives.WriteUInt64BigEndian(result, hash);
            return result;
        }

        /// <summary>Computes the XXH3 hash of the provided <paramref name="source"/> data into the provided <paramref name="destination"/> using the optionally provided <paramref name="seed"/>.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed 64-bit hash code.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <see cref="HashLengthInBytes"/> (8 bytes).</exception>
        public static int Hash(ReadOnlySpan<byte> source, Span<byte> destination, long seed = 0)
        {
            if (!TryHash(source, destination, out int bytesWritten, seed))
            {
                ThrowDestinationTooShort();
            }

            return bytesWritten;
        }

        /// <summary>Attempts to compute the XXH3 hash of the provided <paramref name="source"/> data into the provided <paramref name="destination"/> using the optionally provided <paramref name="seed"/>.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer that receives the computed 64-bit hash code.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="seed">The seed value for this hash computation. The default is zero.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> is long enough to receive the computed hash value (8 bytes); otherwise, <see langword="false"/>.</returns>
        public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, long seed = 0)
        {
            if (destination.Length >= sizeof(long))
            {
                ulong hash = HashToUInt64(source, seed);

                if (BitConverter.IsLittleEndian)
                {
                    hash = BinaryPrimitives.ReverseEndianness(hash);
                }
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), hash);

                bytesWritten = HashLengthInBytes;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>Resets the hash computation to the initial state.</summary>
        public override void Reset()
        {
            XxHashShared.Reset(ref _state);
        }

        /// <summary>Appends the contents of <paramref name="source"/> to the data already processed for the current hash computation.</summary>
        /// <param name="source">The data to process.</param>
        public override void Append(ReadOnlySpan<byte> source)
        {
            XxHashShared.Append(ref _state, source);
        }

        /// <summary>Writes the computed 64-bit hash value to <paramref name="destination"/> without modifying accumulated state.</summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        protected override void GetCurrentHashCore(Span<byte> destination)
        {
            ulong hash = GetCurrentHashAsUInt64();
            BinaryPrimitives.WriteUInt64BigEndian(destination, hash);
        }

        /// <summary>Gets the current computed hash value without modifying accumulated state.</summary>
        /// <returns>The hash value for the data already provided.</returns>
        [CLSCompliant(false)]
        public ulong GetCurrentHashAsUInt64()
        {
            ulong current;

            if (_state.TotalLength > MidSizeMaxBytes)
            {
                // Digest on a local copy to ensure the accumulators remain unaltered.
                ulong* accumulators = stackalloc ulong[AccumulatorCount];
                CopyAccumulators(ref _state, accumulators);

                fixed (byte* secret = _state.Secret)
                {
                    DigestLong(ref _state, accumulators, secret);
                    current = MergeAccumulators(accumulators, secret + SecretMergeAccsStartBytes, _state.TotalLength * Prime64_1);
                }
            }
            else
            {
                fixed (byte* buffer = _state.Buffer)
                {
                    current = HashToUInt64(new ReadOnlySpan<byte>(buffer, (int)_state.TotalLength), (long)_state.Seed);
                }
            }

            return current;
        }
    }
}
