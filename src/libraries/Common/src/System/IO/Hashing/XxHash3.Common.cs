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
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif
    sealed unsafe partial class XxHash3
    {
        /// <summary>Computes the XXH3 hash of the provided data.</summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="seed">The seed value for this hash computation.</param>
        /// <returns>The computed XXH3 hash.</returns>
#if !SYSTEM_PRIVATE_CORELIB
        [CLSCompliant(false)]
#endif
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


#if SYSTEM_PRIVATE_CORELIB
        public static int NonRandomizedHashToInt32(byte* sourcePtr, uint length)
        {
            // We currently use this function only for large inputs from corelib's String.GetNonRandomizedHashCode,
            // so we don't need the fast path for small inputs.
            //
            // if (length <= 16)
            // {
            //     return HashLength0To16(sourcePtr, length, (ulong)seed);
            // }
            Debug.Assert(length > 16);

            if (length <= 128)
            {
                return (int)HashLength17To128(sourcePtr, length, (ulong)seed);
            }

            if (length <= MidSizeMaxBytes)
            {
                return (int)HashLength129To240(sourcePtr, length, (ulong)seed);
            }

            return (int)HashLengthOver240(sourcePtr, length, (ulong)seed);
        }
#endif

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
            return Avalanche(seed ^ SecretXor);
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
            return Avalanche(combined ^ (SecretXor + seed));
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

            return FastAvalanche(
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

            return FastAvalanche(hash);
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

            hash = FastAvalanche(hash);

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

            return FastAvalanche(hash);
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
