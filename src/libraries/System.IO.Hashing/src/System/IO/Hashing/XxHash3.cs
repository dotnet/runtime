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
#if NET
    [SkipLocalsInit]
#endif
    public sealed unsafe class XxHash3 : NonCryptographicHashAlgorithm
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
#if NET
            ArgumentNullException.ThrowIfNull(source);
#else
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
#endif

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

        /// <summary>Computes the XXH3 hash of the provided data.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation.</param>
        /// <returns>The computed XXH3 hash.</returns>
        [CLSCompliant(false)]
        public static ulong HashToUInt64(ReadOnlySpan<byte> source, long seed = 0)
        {
            uint length = (uint)source.Length;
            fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
            {
                if (length <= 16)
                {
                    return HashLength0To16(sourcePtr, length, (ulong)seed);
                }

                if (length <= 128)
                {
                    return HashLength17To128(sourcePtr, length, (ulong)seed);
                }

                if (length <= MidSizeMaxBytes)
                {
                    return HashLength129To240(sourcePtr, length, (ulong)seed);
                }

                return HashLengthOver240(sourcePtr, length, (ulong)seed);
            }
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

        private static ulong HashLength0To16(byte* source, uint length, ulong seed)
        {
            if (length > 8)
            {
                return HashLength9To16(source, length, seed);
            }

            if (length >= 4)
            {
                return HashLength4To8(source, length, seed);
            }

            if (length != 0)
            {
                return HashLength1To3(source, length, seed);
            }

            const ulong SecretXor = DefaultSecretUInt64_7 ^ DefaultSecretUInt64_8;
            return XxHash64.Avalanche(seed ^ SecretXor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashLength1To3(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 1 && length <= 3);

            // When source.Length == 1, c1 == source[0], c2 == source[0], c3 == source[0]
            // When source.Length == 2, c1 == source[0], c2 == source[1], c3 == source[1]
            // When source.Length == 3, c1 == source[0], c2 == source[1], c3 == source[2]
            byte c1 = *source;
            byte c2 = source[length >> 1];
            byte c3 = source[length - 1];

            uint combined = ((uint)c1 << 16) | ((uint)c2 << 24) | c3 | (length << 8);

            const uint SecretXor = unchecked((uint)DefaultSecretUInt64_0) ^ (uint)(DefaultSecretUInt64_0 >> 32);
            return XxHash64.Avalanche(combined ^ (SecretXor + seed));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashLength4To8(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 4 && length <= 8);

            seed ^= (ulong)BinaryPrimitives.ReverseEndianness((uint)seed) << 32;

            uint inputLow = ReadUInt32LE(source);
            uint inputHigh = ReadUInt32LE(source + length - sizeof(uint));

            const ulong SecretXor = DefaultSecretUInt64_1 ^ DefaultSecretUInt64_2;
            ulong bitflip = SecretXor - seed;
            ulong input64 = inputHigh + (((ulong)inputLow) << 32);

            return Rrmxmx(input64 ^ bitflip, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashLength9To16(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 9 && length <= 16);

            const ulong SecretXorL = DefaultSecretUInt64_3 ^ DefaultSecretUInt64_4;
            const ulong SecretXorR = DefaultSecretUInt64_5 ^ DefaultSecretUInt64_6;
            ulong bitflipLow = SecretXorL + seed;
            ulong bitflipHigh = SecretXorR - seed;

            ulong inputLow = ReadUInt64LE(source) ^ bitflipLow;
            ulong inputHigh = ReadUInt64LE(source + length - sizeof(ulong)) ^ bitflipHigh;

            return Avalanche(
                length +
                BinaryPrimitives.ReverseEndianness(inputLow) +
                inputHigh +
                Multiply64To128ThenFold(inputLow, inputHigh));
        }

        private static ulong HashLength17To128(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 17 && length <= 128);

            ulong hash = length * Prime64_1;

            switch ((length - 1) / 32)
            {
                default: // case 3
                    hash += Mix16Bytes(source + 48, DefaultSecretUInt64_12, DefaultSecretUInt64_13, seed);
                    hash += Mix16Bytes(source + length - 64, DefaultSecretUInt64_14, DefaultSecretUInt64_15, seed);
                    goto case 2;
                case 2:
                    hash += Mix16Bytes(source + 32, DefaultSecretUInt64_8, DefaultSecretUInt64_9, seed);
                    hash += Mix16Bytes(source + length - 48, DefaultSecretUInt64_10, DefaultSecretUInt64_11, seed);
                    goto case 1;
                case 1:
                    hash += Mix16Bytes(source + 16, DefaultSecretUInt64_4, DefaultSecretUInt64_5, seed);
                    hash += Mix16Bytes(source + length - 32, DefaultSecretUInt64_6, DefaultSecretUInt64_7, seed);
                    goto case 0;
                case 0:
                    hash += Mix16Bytes(source, DefaultSecretUInt64_0, DefaultSecretUInt64_1, seed);
                    hash += Mix16Bytes(source + length - 16, DefaultSecretUInt64_2, DefaultSecretUInt64_3, seed);
                    break;
            }

            return Avalanche(hash);
        }

        private static ulong HashLength129To240(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 129 && length <= 240);

            ulong hash = length * Prime64_1;

            hash += Mix16Bytes(source + (16 * 0), DefaultSecretUInt64_0, DefaultSecretUInt64_1, seed);
            hash += Mix16Bytes(source + (16 * 1), DefaultSecretUInt64_2, DefaultSecretUInt64_3, seed);
            hash += Mix16Bytes(source + (16 * 2), DefaultSecretUInt64_4, DefaultSecretUInt64_5, seed);
            hash += Mix16Bytes(source + (16 * 3), DefaultSecretUInt64_6, DefaultSecretUInt64_7, seed);
            hash += Mix16Bytes(source + (16 * 4), DefaultSecretUInt64_8, DefaultSecretUInt64_9, seed);
            hash += Mix16Bytes(source + (16 * 5), DefaultSecretUInt64_10, DefaultSecretUInt64_11, seed);
            hash += Mix16Bytes(source + (16 * 6), DefaultSecretUInt64_12, DefaultSecretUInt64_13, seed);
            hash += Mix16Bytes(source + (16 * 7), DefaultSecretUInt64_14, DefaultSecretUInt64_15, seed);

            hash = Avalanche(hash);

            switch ((length - (16 * 8)) / 16)
            {
                default: // case 7
                    Debug.Assert((length - 16 * 8) / 16 == 7);
                    hash += Mix16Bytes(source + (16 * 14), DefaultSecret3UInt64_12, DefaultSecret3UInt64_13, seed);
                    goto case 6;
                case 6:
                    hash += Mix16Bytes(source + (16 * 13), DefaultSecret3UInt64_10, DefaultSecret3UInt64_11, seed);
                    goto case 5;
                case 5:
                    hash += Mix16Bytes(source + (16 * 12), DefaultSecret3UInt64_8, DefaultSecret3UInt64_9, seed);
                    goto case 4;
                case 4:
                    hash += Mix16Bytes(source + (16 * 11), DefaultSecret3UInt64_6, DefaultSecret3UInt64_7, seed);
                    goto case 3;
                case 3:
                    hash += Mix16Bytes(source + (16 * 10), DefaultSecret3UInt64_4, DefaultSecret3UInt64_5, seed);
                    goto case 2;
                case 2:
                    hash += Mix16Bytes(source + (16 * 9), DefaultSecret3UInt64_2, DefaultSecret3UInt64_3, seed);
                    goto case 1;
                case 1:
                    hash += Mix16Bytes(source + (16 * 8), DefaultSecret3UInt64_0, DefaultSecret3UInt64_1, seed);
                    goto case 0;
                case 0:
                    hash += Mix16Bytes(source + length - 16, 0x7378D9C97E9FC831, 0xEBD33483ACC5EA64, seed); // DefaultSecret[119], DefaultSecret[127]
                    break;
            }

            return Avalanche(hash);
        }

        private static ulong HashLengthOver240(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length > 240);

            fixed (byte* defaultSecret = &MemoryMarshal.GetReference(DefaultSecret))
            {
                byte* secret = defaultSecret;
                if (seed != 0)
                {
                    byte* customSecret = stackalloc byte[SecretLengthBytes];
                    DeriveSecretFromSeed(customSecret, seed);
                    secret = customSecret;
                }

                ulong* accumulators = stackalloc ulong[AccumulatorCount];
                InitializeAccumulators(accumulators);

                HashInternalLoop(accumulators, source, length, secret);

                return MergeAccumulators(accumulators, secret + 11, length * Prime64_1);
            }
        }
    }
}
