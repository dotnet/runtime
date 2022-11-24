// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on the XXH3 implementation from https://github.com/Cyan4973/xxHash.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace System.IO.Hashing
{
    /// <summary>Provides an implementation of the XXH3 hash algorithm.</summary>
    /// <remarks>
    /// For methods that persist the computed numerical hash value as bytes,
    /// the value is written in the Big Endian byte order.
    /// </remarks>
#if NET5_0_OR_GREATER
    [SkipLocalsInit]
#endif
    public sealed unsafe partial class XxHash3 : NonCryptographicHashAlgorithm
    {
        /// <summary>XXH3 produces 8-byte hashes.</summary>
        private new const int HashLengthInBytes = 8;
        private const int StripeLengthBytes = 64;
        private const int SecretLengthBytes = 192;
        private const int SecretLastAccStartBytes = 7;
        private const int SecretConsumeRateBytes = 8;
        private const int SecretMergeAccsStartBytes = 11;
        private const int NumStripesPerBlock = (SecretLengthBytes - StripeLengthBytes) / SecretConsumeRateBytes;
        private const int AccumulatorCount = StripeLengthBytes / sizeof(ulong);
        private const int MidSizeMaxBytes = 240;
        private const int InternalBufferStripes = InternalBufferLengthBytes / StripeLengthBytes;
        private const int InternalBufferLengthBytes = 256;

        /// <summary>The default secret for when no seed is provided.</summary>
        /// <remarks>This is the same as a custom secret derived from a seed of 0.</remarks>
        private static ReadOnlySpan<byte> DefaultSecret => new byte[]
        {
            0xb8, 0xfe, 0x6c, 0x39, 0x23, 0xa4, 0x4b, 0xbe, 0x7c, 0x01, 0x81, 0x2c, 0xf7, 0x21, 0xad, 0x1c,
            0xde, 0xd4, 0x6d, 0xe9, 0x83, 0x90, 0x97, 0xdb, 0x72, 0x40, 0xa4, 0xa4, 0xb7, 0xb3, 0x67, 0x1f,
            0xcb, 0x79, 0xe6, 0x4e, 0xcc, 0xc0, 0xe5, 0x78, 0x82, 0x5a, 0xd0, 0x7d, 0xcc, 0xff, 0x72, 0x21,
            0xb8, 0x08, 0x46, 0x74, 0xf7, 0x43, 0x24, 0x8e, 0xe0, 0x35, 0x90, 0xe6, 0x81, 0x3a, 0x26, 0x4c,
            0x3c, 0x28, 0x52, 0xbb, 0x91, 0xc3, 0x00, 0xcb, 0x88, 0xd0, 0x65, 0x8b, 0x1b, 0x53, 0x2e, 0xa3,
            0x71, 0x64, 0x48, 0x97, 0xa2, 0x0d, 0xf9, 0x4e, 0x38, 0x19, 0xef, 0x46, 0xa9, 0xde, 0xac, 0xd8,
            0xa8, 0xfa, 0x76, 0x3f, 0xe3, 0x9c, 0x34, 0x3f, 0xf9, 0xdc, 0xbb, 0xc7, 0xc7, 0x0b, 0x4f, 0x1d,
            0x8a, 0x51, 0xe0, 0x4b, 0xcd, 0xb4, 0x59, 0x31, 0xc8, 0x9f, 0x7e, 0xc9, 0xd9, 0x78, 0x73, 0x64,
            0xea, 0xc5, 0xac, 0x83, 0x34, 0xd3, 0xeb, 0xc3, 0xc5, 0x81, 0xa0, 0xff, 0xfa, 0x13, 0x63, 0xeb,
            0x17, 0x0d, 0xdd, 0x51, 0xb7, 0xf0, 0xda, 0x49, 0xd3, 0x16, 0x55, 0x26, 0x29, 0xd4, 0x68, 0x9e,
            0x2b, 0x16, 0xbe, 0x58, 0x7d, 0x47, 0xa1, 0xfc, 0x8f, 0xf8, 0xb8, 0xd1, 0x7a, 0xd0, 0x31, 0xce,
            0x45, 0xcb, 0x3a, 0x8f, 0x95, 0x16, 0x04, 0x28, 0xaf, 0xd7, 0xfb, 0xca, 0xbb, 0x4b, 0x40, 0x7e,
        };

#if DEBUG
        static XxHash3()
        {
            // Make sure DefaultSecret is the custom secret derived from a seed of 0.
            byte* secret = stackalloc byte[SecretLengthBytes];
            DeriveSecretFromSeed(secret, 0);
            Debug.Assert(new Span<byte>(secret, SecretLengthBytes).SequenceEqual(DefaultSecret));

            // Validate some relationships.
            Debug.Assert(InternalBufferLengthBytes % StripeLengthBytes == 0);
        }
#endif

        private State _state;

        /// <summary>Initializes a new instance of the <see cref="XxHash3"/> class using the default seed value 0.</summary>
        public XxHash3() : this(0)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="XxHash3"/> class using the specified seed.</summary>
        public XxHash3(long seed) : base(HashLengthInBytes)
        {
            _state.Seed = (ulong)seed;

            fixed (byte* secret = _state.Secret)
            {
                if (seed == 0)
                {
                    DefaultSecret.CopyTo(new Span<byte>(secret, SecretLengthBytes));
                }
                else
                {
                    DeriveSecretFromSeed(secret, (ulong)seed);
                }
            }

            Reset();
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
#if NET6_0_OR_GREATER
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
            _state.BufferedCount = 0;
            _state.StripesProcessedInCurrentBlock = 0;
            _state.TotalLength = 0;

            fixed (ulong* accumulators = _state.Accumulators)
            {
                InitializeAccumulators(accumulators);
            }
        }

        /// <summary>Appends the contents of <paramref name="source"/> to the data already processed for the current hash computation.</summary>
        /// <param name="source">The data to process.</param>
        public override void Append(ReadOnlySpan<byte> source)
        {
            Debug.Assert(_state.BufferedCount <= InternalBufferLengthBytes);

            _state.TotalLength += (uint)source.Length;

            fixed (byte* buffer = _state.Buffer)
            {
                // Small input: just copy the data to the buffer.
                if (source.Length <= InternalBufferLengthBytes - _state.BufferedCount)
                {
                    source.CopyTo(new Span<byte>(buffer + _state.BufferedCount, source.Length));
                    _state.BufferedCount += (uint)source.Length;
                    return;
                }

                fixed (byte* secret = _state.Secret)
                fixed (ulong* accumulators = _state.Accumulators)
                fixed (byte* sourcePtr = &MemoryMarshal.GetReference(source))
                {
                    // Internal buffer is partially filled (always, except at beginning). Complete it, then consume it.
                    int sourceIndex = 0;
                    if (_state.BufferedCount != 0)
                    {
                        int loadSize = InternalBufferLengthBytes - (int)_state.BufferedCount;

                        source.Slice(0, loadSize).CopyTo(new Span<byte>(buffer + _state.BufferedCount, loadSize));
                        sourceIndex = loadSize;

                        ConsumeStripes(accumulators, ref _state.StripesProcessedInCurrentBlock, NumStripesPerBlock, buffer, InternalBufferStripes, secret);
                        _state.BufferedCount = 0;
                    }
                    Debug.Assert(sourceIndex < source.Length);

                    // Large input to consume: ingest per full block.
                    if (source.Length - sourceIndex > NumStripesPerBlock * StripeLengthBytes)
                    {
                        ulong stripes = (ulong)(source.Length - sourceIndex - 1) / StripeLengthBytes;
                        Debug.Assert(NumStripesPerBlock >= _state.StripesProcessedInCurrentBlock);

                        // Join to current block's end.
                        ulong stripesToEnd = NumStripesPerBlock - _state.StripesProcessedInCurrentBlock;
                        Debug.Assert(stripesToEnd <= stripes);
                        Accumulate(accumulators, sourcePtr + sourceIndex, secret + ((int)_state.StripesProcessedInCurrentBlock * SecretConsumeRateBytes), (int)stripesToEnd);
                        ScrambleAccumulators(accumulators, secret + (SecretLengthBytes - StripeLengthBytes));
                        _state.StripesProcessedInCurrentBlock = 0;
                        sourceIndex += (int)stripesToEnd * StripeLengthBytes;
                        stripes -= stripesToEnd;

                        // Consume entire blocks.
                        while (stripes >= NumStripesPerBlock)
                        {
                            Accumulate(accumulators, sourcePtr + sourceIndex, secret, NumStripesPerBlock);
                            ScrambleAccumulators(accumulators, secret + (SecretLengthBytes - StripeLengthBytes));
                            sourceIndex += NumStripesPerBlock * StripeLengthBytes;
                            stripes -= NumStripesPerBlock;
                        }

                        // Consume complete stripes in the last partial block.
                        Accumulate(accumulators, sourcePtr + sourceIndex, secret, (int)stripes);
                        sourceIndex += (int)stripes * StripeLengthBytes;
                        Debug.Assert(sourceIndex < source.Length);  // at least some bytes left
                        _state.StripesProcessedInCurrentBlock = stripes;

                        // Copy the last stripe into the end of the buffer so it is available to GetCurrentHashCore when processing the "stripe from the end".
                        source.Slice(sourceIndex - StripeLengthBytes, StripeLengthBytes).CopyTo(new Span<byte>(buffer + InternalBufferLengthBytes - StripeLengthBytes, StripeLengthBytes));
                    }
                    else if (source.Length - sourceIndex > InternalBufferLengthBytes)
                    {
                        // Content to consume <= block size. Consume source by a multiple of internal buffer size.
                        do
                        {
                            ConsumeStripes(accumulators, ref _state.StripesProcessedInCurrentBlock, NumStripesPerBlock, sourcePtr + sourceIndex, InternalBufferStripes, secret);
                            sourceIndex += InternalBufferLengthBytes;
                        }
                        while (source.Length - sourceIndex > InternalBufferLengthBytes);

                        // Copy the last stripe into the end of the buffer so it is available to GetCurrentHashCore when processing the "stripe from the end".
                        source.Slice(sourceIndex - StripeLengthBytes, StripeLengthBytes).CopyTo(new Span<byte>(buffer + InternalBufferLengthBytes - StripeLengthBytes, StripeLengthBytes));
                    }

                    // Buffer the remaining input.
                    Span<byte> remaining = new Span<byte>(buffer, source.Length - sourceIndex);
                    Debug.Assert(sourceIndex < source.Length);
                    Debug.Assert(remaining.Length <= InternalBufferLengthBytes);
                    Debug.Assert(_state.BufferedCount == 0);
                    source.Slice(sourceIndex).CopyTo(remaining);
                    _state.BufferedCount = (uint)remaining.Length;
                }
            }
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
                fixed (ulong* stateAccumulators = _state.Accumulators)
                {
#if NET7_0_OR_GREATER
                    if (Vector256.IsHardwareAccelerated)
                    {
                        Vector256.Store(Vector256.Load(stateAccumulators), accumulators);
                        Vector256.Store(Vector256.Load(stateAccumulators + 4), accumulators + 4);
                    }
                    else if (Vector128.IsHardwareAccelerated)
                    {
                        Vector128.Store(Vector128.Load(stateAccumulators), accumulators);
                        Vector128.Store(Vector128.Load(stateAccumulators + 2), accumulators + 2);
                        Vector128.Store(Vector128.Load(stateAccumulators + 4), accumulators + 4);
                        Vector128.Store(Vector128.Load(stateAccumulators + 6), accumulators + 6);
                    }
                    else
#endif
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            accumulators[i] = stateAccumulators[i];
                        }
                    }
                }

                fixed (byte* secret = _state.Secret)
                {
                    DigestLong(accumulators, secret);
                    current = MergeAccumulators(accumulators, secret + SecretMergeAccsStartBytes, _state.TotalLength * XxHash64.Prime64_1);
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

            void DigestLong(ulong* accumulators, byte* secret)
            {
                Debug.Assert(_state.BufferedCount > 0);

                fixed (byte* buffer = _state.Buffer)
                {
                    byte* accumulateData;
                    if (_state.BufferedCount >= StripeLengthBytes)
                    {
                        uint stripes = (_state.BufferedCount - 1) / StripeLengthBytes;
                        ulong stripesSoFar = _state.StripesProcessedInCurrentBlock;

                        ConsumeStripes(accumulators, ref stripesSoFar, NumStripesPerBlock, buffer, stripes, secret);

                        accumulateData = buffer + _state.BufferedCount - StripeLengthBytes;
                    }
                    else
                    {
                        byte* lastStripe = stackalloc byte[StripeLengthBytes];
                        int catchupSize = StripeLengthBytes - (int)_state.BufferedCount;

                        new ReadOnlySpan<byte>(buffer + InternalBufferLengthBytes - catchupSize, catchupSize).CopyTo(new Span<byte>(lastStripe, StripeLengthBytes));
                        new ReadOnlySpan<byte>(buffer, (int)_state.BufferedCount).CopyTo(new Span<byte>(lastStripe + catchupSize, (int)_state.BufferedCount));

                        accumulateData = lastStripe;
                    }

                    Accumulate512(accumulators, accumulateData, secret + (SecretLengthBytes - StripeLengthBytes - SecretLastAccStartBytes));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            return XxHash64.Avalanche(seed ^ 0x8726F9105DC21DDC); // DefaultSecretUInt64[7] ^ DefaultSecretUInt64[8]
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

            return XxHash64.Avalanche(combined ^ (0x87275A9B + seed)); // DefaultSecretUInt32[0] ^ DefaultSecretUInt32[1]
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashLength4To8(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 4 && length <= 8);

            seed ^= (ulong)BinaryPrimitives.ReverseEndianness((uint)seed) << 32;

            uint inputLow = ReadUInt32LE(source);
            uint inputHigh = ReadUInt32LE(source + length - sizeof(uint));

            ulong bitflip = 0xC73AB174C5ECD5A2 - seed; // DefaultSecretUInt64[1] ^ DefaultSecretUInt64[2]
            ulong input64 = inputHigh + (((ulong)inputLow) << 32);

            return Rrmxmx(input64 ^ bitflip, length);
        }

        /// <summary>"This is a stronger avalanche, preferable when input has not been previously mixed."</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rrmxmx(ulong hash, uint length)
        {
            hash ^= BitOperations.RotateLeft(hash, 49) ^ BitOperations.RotateLeft(hash, 24);
            hash *= 0x9FB21C651E98DF25;
            hash ^= (hash >> 35) + length;
            hash *= 0x9FB21C651E98DF25;
            return XorShift(hash, 28);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong HashLength9To16(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 9 && length <= 16);

            ulong bitflipLow = 0x6782737BEA4239B9 + seed; // DefaultSecretUInt64[3] ^ DefaultSecretUInt64[4]
            ulong bitflipHigh = 0xAF56BC3B0996523A - seed; // DefaultSecretUInt64[5] ^ DefaultSecretUInt64[6]

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

            ulong hash = length * XxHash64.Prime64_1;

            switch ((length - 1) / 32)
            {
                default: // case 3
                    hash += Mix16Bytes(source + 48, 0x3F349CE33F76FAA8, 0x1D4F0BC7C7BBDCF9, seed); // DefaultSecretUInt64[12], DefaultSecretUInt64[13]
                    hash += Mix16Bytes(source + length - 64, 0x3159B4CD4BE0518A, 0x647378D9C97E9FC8, seed); // DefaultSecretUInt64[14], DefaultSecretUInt64[15]
                    goto case 2;
                case 2:
                    hash += Mix16Bytes(source + 32, 0xCB00C391BB52283C, 0xA32E531B8B65D088, seed); // DefaultSecretUInt64[8], DefaultSecretUInt64[9]
                    hash += Mix16Bytes(source + length - 48, 0x4EF90DA297486471, 0xD8ACDEA946EF1938, seed); // DefaultSecretUInt64[10], DefaultSecretUInt64[11]
                    goto case 1;
                case 1:
                    hash += Mix16Bytes(source + 16, 0x78E5C0CC4EE679CB, 0x2172FFCC7DD05A82, seed); // DefaultSecretUInt64[4], DefaultSecretUInt64[5]
                    hash += Mix16Bytes(source + length - 32, 0x8E2443F7744608B8, 0x4C263A81E69035E0, seed); // DefaultSecretUInt64[6], DefaultSecretUInt64[7]
                    goto case 0;
                case 0:
                    hash += Mix16Bytes(source, 0xBE4BA423396CFEB8, 0x1CAD21F72C81017C, seed); // DefaultSecretUInt64[0], DefaultSecretUInt64[1]
                    hash += Mix16Bytes(source + length - 16, 0xDB979083E96DD4DE, 0x1F67B3B7A4A44072, seed); // DefaultSecretUInt64[2], DefaultSecretUInt64[3]
                    break;
            }

            return Avalanche(hash);
        }

        private static ulong HashLength129To240(byte* source, uint length, ulong seed)
        {
            Debug.Assert(length >= 129 && length <= 240);

            ulong hash = length * XxHash64.Prime64_1;

            hash += Mix16Bytes(source + (16 * 0), 0xBE4BA423396CFEB8, 0x1CAD21F72C81017C, seed); // DefaultSecretUInt64[0], DefaultSecretUInt64[1]
            hash += Mix16Bytes(source + (16 * 1), 0xDB979083E96DD4DE, 0x1F67B3B7A4A44072, seed); // DefaultSecretUInt64[2], DefaultSecretUInt64[3]
            hash += Mix16Bytes(source + (16 * 2), 0x78E5C0CC4EE679CB, 0x2172FFCC7DD05A82, seed); // DefaultSecretUInt64[4], DefaultSecretUInt64[5]
            hash += Mix16Bytes(source + (16 * 3), 0x8E2443F7744608B8, 0x4C263A81E69035E0, seed); // DefaultSecretUInt64[6], DefaultSecretUInt64[7]
            hash += Mix16Bytes(source + (16 * 4), 0xCB00C391BB52283C, 0xA32E531B8B65D088, seed); // DefaultSecretUInt64[8], DefaultSecretUInt64[9]
            hash += Mix16Bytes(source + (16 * 5), 0x4EF90DA297486471, 0xD8ACDEA946EF1938, seed); // DefaultSecretUInt64[10], DefaultSecretUInt64[11]
            hash += Mix16Bytes(source + (16 * 6), 0x3F349CE33F76FAA8, 0x1D4F0BC7C7BBDCF9, seed); // DefaultSecretUInt64[12], DefaultSecretUInt64[13]
            hash += Mix16Bytes(source + (16 * 7), 0x3159B4CD4BE0518A, 0x647378D9C97E9FC8, seed); // DefaultSecretUInt64[14], DefaultSecretUInt64[15]

            hash = Avalanche(hash);

            switch ((length - (16 * 8)) / 16)
            {
                default: // case 7
                    Debug.Assert((length - 16 * 8) / 16 == 7);
                    hash += Mix16Bytes(source + (16 * 14), 0xBBDCF93F349CE33F, 0xE0518A1D4F0BC7C7, seed); //  Read<ulong>(ref DefaultSecret[99]),  Read<ulong>(ref DefaultSecret[107])
                    goto case 6;
                case 6:
                    hash += Mix16Bytes(source + (16 * 13), 0xEF19384EF90DA297, 0x76FAA8D8ACDEA946, seed); //  Read<ulong>(ref DefaultSecret[83]),  Read<ulong>(ref DefaultSecret[91])
                    goto case 5;
                case 5:
                    hash += Mix16Bytes(source + (16 * 12), 0x65D088CB00C391BB, 0x486471A32E531B8B, seed); //  Read<ulong>(ref DefaultSecret[67]),  Read<ulong>(ref DefaultSecret[75])
                    goto case 4;
                case 4:
                    hash += Mix16Bytes(source + (16 * 11), 0x9035E08E2443F774, 0x52283C4C263A81E6, seed); //  Read<ulong>(ref DefaultSecret[51]),  Read<ulong>(ref DefaultSecret[59])
                    goto case 3;
                case 3:
                    hash += Mix16Bytes(source + (16 * 10), 0xD05A8278E5C0CC4E, 0x4608B82172FFCC7D, seed); //  Read<ulong>(ref DefaultSecret[35]),  Read<ulong>(ref DefaultSecret[43])
                    goto case 2;
                case 2:
                    hash += Mix16Bytes(source + (16 * 9), 0xA44072DB979083E9, 0xE679CB1F67B3B7A4, seed); //  Read<ulong>(ref DefaultSecret[19]),  Read<ulong>(ref DefaultSecret[27])
                    goto case 1;
                case 1:
                    hash += Mix16Bytes(source + (16 * 8), 0x81017CBE4BA42339, 0x6DD4DE1CAD21F72C, seed); // Read<ulong>(ref DefaultSecret[3]),  Read<ulong>(ref DefaultSecret[11])
                    goto case 0;
                case 0:
                    break;
            }

            // Handle the last 16 bytes.
            hash += Mix16Bytes(source + length - 16, 0x7378D9C97E9FC831, 0xEBD33483ACC5EA64, seed); // DefaultSecret[119], DefaultSecret[127]
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

                const int StripesPerBlock = (SecretLengthBytes - StripeLengthBytes) / SecretConsumeRateBytes;
                const int BlockLen = StripeLengthBytes * StripesPerBlock;
                int blocksNum = (int)((length - 1) / BlockLen);

                Accumulate(accumulators, source, secret, StripesPerBlock, true, blocksNum);
                int offset = BlockLen * blocksNum;

                int stripesNumber = (int)((length - 1 - offset) / StripeLengthBytes);
                Accumulate(accumulators, source + offset, secret, stripesNumber);
                Accumulate512(accumulators, source + length - StripeLengthBytes, secret + (SecretLengthBytes - StripeLengthBytes - SecretLastAccStartBytes));

                return MergeAccumulators(accumulators, secret + 11, length * XxHash64.Prime64_1);
            }
        }

        private static void ConsumeStripes(ulong* accumulators, ref ulong stripesSoFar, ulong stripesPerBlock, byte* source, ulong stripes, byte* secret)
        {
            Debug.Assert(stripes <= stripesPerBlock); // can handle max 1 scramble per invocation
            Debug.Assert(stripesSoFar < stripesPerBlock);

            ulong stripesToEndOfBlock = stripesPerBlock - stripesSoFar;
            if (stripesToEndOfBlock <= stripes)
            {
                // need a scrambling operation
                ulong stripesAfterBlock = stripes - stripesToEndOfBlock;
                Accumulate(accumulators, source, secret + ((int)stripesSoFar * SecretConsumeRateBytes), (int)stripesToEndOfBlock);
                ScrambleAccumulators(accumulators, secret + (SecretLengthBytes - StripeLengthBytes));
                Accumulate(accumulators, source + ((int)stripesToEndOfBlock * StripeLengthBytes), secret, (int)stripesAfterBlock);
                stripesSoFar = stripesAfterBlock;
            }
            else
            {
                Accumulate(accumulators, source, secret + ((int)stripesSoFar * SecretConsumeRateBytes), (int)stripes);
                stripesSoFar += stripes;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InitializeAccumulators(ulong* accumulators)
        {
#if NET7_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                Vector256.Store(Vector256.Create(XxHash32.Prime32_3, XxHash64.Prime64_1, XxHash64.Prime64_2, XxHash64.Prime64_3), accumulators);
                Vector256.Store(Vector256.Create(XxHash64.Prime64_4, XxHash32.Prime32_2, XxHash64.Prime64_5, XxHash32.Prime32_1), accumulators + 4);
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                Vector128.Store(Vector128.Create(XxHash32.Prime32_3, XxHash64.Prime64_1), accumulators);
                Vector128.Store(Vector128.Create(XxHash64.Prime64_2, XxHash64.Prime64_3), accumulators + 2);
                Vector128.Store(Vector128.Create(XxHash64.Prime64_4, XxHash32.Prime32_2), accumulators + 4);
                Vector128.Store(Vector128.Create(XxHash64.Prime64_5, XxHash32.Prime32_1), accumulators + 6);
            }
            else
#endif
            {
                accumulators[0] = XxHash32.Prime32_3;
                accumulators[1] = XxHash64.Prime64_1;
                accumulators[2] = XxHash64.Prime64_2;
                accumulators[3] = XxHash64.Prime64_3;
                accumulators[4] = XxHash64.Prime64_4;
                accumulators[5] = XxHash32.Prime32_2;
                accumulators[6] = XxHash64.Prime64_5;
                accumulators[7] = XxHash32.Prime32_1;
            }
        }

        private static ulong MergeAccumulators(ulong* accumulators, byte* secret, ulong start)
        {
            ulong result64 = start;

            result64 += Multiply64To128ThenFold(accumulators[0] ^ ReadUInt64LE(secret), accumulators[1] ^ ReadUInt64LE(secret + 8));
            result64 += Multiply64To128ThenFold(accumulators[2] ^ ReadUInt64LE(secret + 16), accumulators[3] ^ ReadUInt64LE(secret + 24));
            result64 += Multiply64To128ThenFold(accumulators[4] ^ ReadUInt64LE(secret + 32), accumulators[5] ^ ReadUInt64LE(secret + 40));
            result64 += Multiply64To128ThenFold(accumulators[6] ^ ReadUInt64LE(secret + 48), accumulators[7] ^ ReadUInt64LE(secret + 56));

            return Avalanche(result64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Mix16Bytes(byte* source, ulong secretLow, ulong secretHigh, ulong seed) =>
            Multiply64To128ThenFold(
                ReadUInt64LE(source) ^ (secretLow + seed),
                ReadUInt64LE(source + sizeof(ulong)) ^ (secretHigh - seed));

        /// <summary>Calculates a 32-bit to 64-bit long multiply.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Multiply32To64(ulong v1, ulong v2) => (uint)v1 * (ulong)(uint)v2;

        /// <summary>"This is a fast avalanche stage, suitable when input bits are already partially mixed."</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Avalanche(ulong hash)
        {
            hash = XorShift(hash, 37);
            hash *= 0x165667919E3779F9;
            hash = XorShift(hash, 32);
            return hash;
        }

        /// <summary>Calculates a 64-bit to 128-bit multiply, then XOR folds it.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Multiply64To128ThenFold(ulong left, ulong right)
        {
#if NET5_0_OR_GREATER
            ulong upper = Math.BigMul(left, right, out ulong lower);
#else
            ulong lowerLow = Multiply32To64(left & 0xFFFFFFFF, right & 0xFFFFFFFF);
            ulong higherLow = Multiply32To64(left >> 32, right & 0xFFFFFFFF);
            ulong lowerHigh = Multiply32To64(left & 0xFFFFFFFF, right >> 32);
            ulong higherHigh = Multiply32To64(left >> 32, right >> 32);

            ulong cross = (lowerLow >> 32) + (higherLow & 0xFFFFFFFF) + lowerHigh;
            ulong upper = (higherLow >> 32) + (cross >> 32) + higherHigh;
            ulong lower = (cross << 32) | (lowerLow & 0xFFFFFFFF);
#endif
            return lower ^ upper;
        }

        private static void DeriveSecretFromSeed(byte* destinationSecret, ulong seed)
        {
            fixed (byte* defaultSecret = &MemoryMarshal.GetReference(DefaultSecret))
            {
#if NET7_0_OR_GREATER
                if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
                {
                    Vector256<ulong> seedVec = Vector256.Create(seed, 0u - seed, seed, 0u - seed);
                    for (int i = 0; i < SecretLengthBytes; i += Vector256<byte>.Count)
                    {
                        Vector256.Store(Vector256.Load((ulong*)(defaultSecret + i)) + seedVec, (ulong*)(destinationSecret + i));
                    }
                }
                else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
                {
                    Vector128<ulong> seedVec = Vector128.Create(seed, 0u - seed);
                    for (int i = 0; i < SecretLengthBytes; i += Vector128<byte>.Count)
                    {
                        Vector128.Store(Vector128.Load((ulong*)(defaultSecret + i)) + seedVec, (ulong*)(destinationSecret + i));
                    }
                }
                else
#endif
                {
                    for (int i = 0; i < SecretLengthBytes; i += sizeof(ulong) * 2)
                    {
                        WriteUInt64LE(destinationSecret + i, ReadUInt64LE(defaultSecret + i) + seed);
                        WriteUInt64LE(destinationSecret + i + 8, ReadUInt64LE(defaultSecret + i + 8) - seed);
                    }
                }
            }
        }

        /// <summary>Optimized version of looping over <see cref="Accumulate512"/>.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Accumulate(ulong* accumulators, byte* source, byte* secret, int stripesToProcess, bool scramble = false, int blockCount = 1)
        {
            byte* secretForAccumulate = secret;
            byte* secretForScramble = secret + (SecretLengthBytes - StripeLengthBytes);

#if NET7_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                Vector256<ulong> acc1 = Vector256.Load(accumulators);
                Vector256<ulong> acc2 = Vector256.Load(accumulators + Vector256<ulong>.Count);

                for (int j = 0; j < blockCount; j++)
                {
                    secret = secretForAccumulate;
                    for (int i = 0; i < stripesToProcess; i++)
                    {
                        Vector256<uint> secretVal = Vector256.Load((uint*)secret);
                        acc1 = Accumulate256(acc1, source, secretVal);
                        source += Vector256<byte>.Count;

                        secretVal = Vector256.Load((uint*)secret + Vector256<uint>.Count);
                        acc2 = Accumulate256(acc2, source, secretVal);
                        source += Vector256<byte>.Count;

                        secret += SecretConsumeRateBytes;
                    }

                    if (scramble)
                    {
                        acc1 = ScrambleAccumulator256(acc1, Vector256.Load((ulong*)secretForScramble));
                        acc2 = ScrambleAccumulator256(acc2, Vector256.Load((ulong*)secretForScramble + Vector256<ulong>.Count));
                    }
                }

                Vector256.Store(acc1, accumulators);
                Vector256.Store(acc2, accumulators + Vector256<ulong>.Count);
            }
            else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                Vector128<ulong> acc1 = Vector128.Load(accumulators);
                Vector128<ulong> acc2 = Vector128.Load(accumulators + Vector128<ulong>.Count);
                Vector128<ulong> acc3 = Vector128.Load(accumulators + Vector128<ulong>.Count * 2);
                Vector128<ulong> acc4 = Vector128.Load(accumulators + Vector128<ulong>.Count * 3);

                for (int j = 0; j < blockCount; j++)
                {
                    secret = secretForAccumulate;
                    for (int i = 0; i < stripesToProcess; i++)
                    {
                        Vector128<uint> secretVal = Vector128.Load((uint*)secret);
                        acc1 = Accumulate128(acc1, source, secretVal);
                        source += Vector128<byte>.Count;

                        secretVal = Vector128.Load((uint*)secret + Vector128<uint>.Count);
                        acc2 = Accumulate128(acc2, source, secretVal);
                        source += Vector128<byte>.Count;

                        secretVal = Vector128.Load((uint*)secret + Vector128<uint>.Count * 2);
                        acc3 = Accumulate128(acc3, source, secretVal);
                        source += Vector128<byte>.Count;

                        secretVal = Vector128.Load((uint*)secret + Vector128<uint>.Count * 3);
                        acc4 = Accumulate128(acc4, source, secretVal);
                        source += Vector128<byte>.Count;

                        secret += SecretConsumeRateBytes;
                    }

                    if (scramble)
                    {
                        acc1 = ScrambleAccumulator128(acc1, Vector128.Load((ulong*)secretForScramble));
                        acc2 = ScrambleAccumulator128(acc2, Vector128.Load((ulong*)secretForScramble + Vector128<ulong>.Count));
                        acc3 = ScrambleAccumulator128(acc3, Vector128.Load((ulong*)secretForScramble + Vector128<ulong>.Count * 2));
                        acc4 = ScrambleAccumulator128(acc4, Vector128.Load((ulong*)secretForScramble + Vector128<ulong>.Count * 3));
                    }
                }

                Vector128.Store(acc1, accumulators);
                Vector128.Store(acc2, accumulators + Vector128<ulong>.Count);
                Vector128.Store(acc3, accumulators + Vector128<ulong>.Count * 2);
                Vector128.Store(acc4, accumulators + Vector128<ulong>.Count * 3);
            }
            else
#endif
            {
                for (int j = 0; j < blockCount; j++)
                {
                    for (int i = 0; i < stripesToProcess; i++)
                    {
                        Accumulate512Inlined(accumulators, source, secret + (i * SecretConsumeRateBytes));
                        source += StripeLengthBytes;
                    }

                    if (scramble)
                    {
                        ScrambleAccumulators(accumulators, secretForScramble);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Accumulate512(ulong* accumulators, byte* source, byte* secret)
        {
            Accumulate512Inlined(accumulators, source, secret);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Accumulate512Inlined(ulong* accumulators, byte* source, byte* secret)
        {
#if NET7_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < AccumulatorCount / Vector256<ulong>.Count; i++)
                {
                    Vector256<ulong> accVec = Accumulate256(Vector256.Load(accumulators), source, Vector256.Load((uint*)secret));
                    Vector256.Store(accVec, accumulators);

                    accumulators += Vector256<ulong>.Count;
                    secret += Vector256<byte>.Count;
                    source += Vector256<byte>.Count;
                }
            }
            else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < AccumulatorCount / Vector128<ulong>.Count; i++)
                {
                    Vector128<ulong> accVec = Accumulate128(Vector128.Load(accumulators), source, Vector128.Load((uint*)secret));
                    Vector128.Store(accVec, accumulators);

                    accumulators += Vector128<ulong>.Count;
                    secret += Vector128<byte>.Count;
                    source += Vector128<byte>.Count;
                }
            }
            else
#endif
            {
                for (int i = 0; i < AccumulatorCount; i++)
                {
                    ulong sourceVal = ReadUInt64LE(source + (8 * i));
                    ulong sourceKey = sourceVal ^ ReadUInt64LE(secret + (i * 8));

                    accumulators[i ^ 1] += sourceVal; // swap adjacent lanes
                    accumulators[i] += Multiply32To64(sourceKey & 0xFFFFFFFF, sourceKey >> 32);
                }
            }
        }

#if NET7_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> Accumulate256(Vector256<ulong> accVec, byte* source, Vector256<uint> secret)
        {
            Vector256<uint> sourceVec = Vector256.Load((uint*)source);
            Vector256<uint> sourceKey = sourceVec ^ secret;

            // TODO: Figure out how to unwind this shuffle and just use Vector256.Multiply
            Vector256<uint> sourceKeyLow = Vector256.Shuffle(sourceKey, Vector256.Create(1u, 0, 3, 0, 5, 0, 7, 0));
            Vector256<uint> sourceSwap = Vector256.Shuffle(sourceVec, Vector256.Create(2u, 3, 0, 1, 6, 7, 4, 5));
            Vector256<ulong> sum = accVec + sourceSwap.AsUInt64();
            Vector256<ulong> product = Avx2.IsSupported ?
                Avx2.Multiply(sourceKey, sourceKeyLow) :
                (sourceKey & Vector256.Create(~0u, 0u, ~0u, 0u, ~0u, 0u, ~0u, 0u)).AsUInt64() * (sourceKeyLow & Vector256.Create(~0u, 0u, ~0u, 0u, ~0u, 0u, ~0u, 0u)).AsUInt64();

            accVec = product + sum;
            return accVec;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> Accumulate128(Vector128<ulong> accVec, byte* source, Vector128<uint> secret)
        {
            Vector128<uint> sourceVec = Vector128.Load((uint*)source);
            Vector128<uint> sourceKey = sourceVec ^ secret;

            // TODO: Figure out how to unwind this shuffle and just use Vector128.Multiply
            Vector128<uint> sourceSwap = Vector128.Shuffle(sourceVec, Vector128.Create(2u, 3, 0, 1));
            Vector128<ulong> sum = accVec + sourceSwap.AsUInt64();

            Vector128<ulong> product = MultiplyWideningLower(sourceKey);
            accVec = product + sum;
            return accVec;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> MultiplyWideningLower(Vector128<uint> source)
        {
            if (AdvSimd.IsSupported)
            {
                Vector64<uint> sourceLow = Vector128.Shuffle(source, Vector128.Create(0u, 2, 0, 0)).GetLower();
                Vector64<uint> sourceHigh = Vector128.Shuffle(source, Vector128.Create(1u, 3, 0, 0)).GetLower();
                return AdvSimd.MultiplyWideningLower(sourceLow, sourceHigh);
            }
            else
            {
                Vector128<uint> sourceLow = Vector128.Shuffle(source, Vector128.Create(1u, 0, 3, 0));
                return Sse2.IsSupported ?
                    Sse2.Multiply(source, sourceLow) :
                    (source & Vector128.Create(~0u, 0u, ~0u, 0u)).AsUInt64() * (sourceLow & Vector128.Create(~0u, 0u, ~0u, 0u)).AsUInt64();
            }
        }
#endif

        private static void ScrambleAccumulators(ulong* accumulators, byte* secret)
        {
#if NET7_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < AccumulatorCount / Vector256<ulong>.Count; i++)
                {
                    Vector256<ulong> accVec = ScrambleAccumulator256(Vector256.Load(accumulators), Vector256.Load((ulong*)secret));
                    Vector256.Store(accVec, accumulators);

                    accumulators += Vector256<ulong>.Count;
                    secret += Vector256<byte>.Count;
                }
            }
            else if (Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < AccumulatorCount / Vector128<ulong>.Count; i++)
                {
                    Vector128<ulong> accVec = ScrambleAccumulator128(Vector128.Load(accumulators), Vector128.Load((ulong*)secret));
                    Vector128.Store(accVec, accumulators);

                    accumulators += Vector128<ulong>.Count;
                    secret += Vector128<byte>.Count;
                }
            }
            else
#endif
            {
                for (int i = 0; i < AccumulatorCount; i++)
                {
                    ulong xorShift = XorShift(*accumulators, 47);
                    ulong xorWithKey = xorShift ^ ReadUInt64LE(secret);
                    *accumulators = xorWithKey * XxHash32.Prime32_1;

                    accumulators++;
                    secret += sizeof(ulong);
                }
            }
        }

#if NET7_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> ScrambleAccumulator256(Vector256<ulong> accVec, Vector256<ulong> secret)
        {
            Vector256<ulong> xorShift = accVec ^ Vector256.ShiftRightLogical(accVec, 47);
            Vector256<ulong> xorWithKey = xorShift ^ secret;
            accVec = xorWithKey * Vector256.Create((ulong)XxHash32.Prime32_1);
            return accVec;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> ScrambleAccumulator128(Vector128<ulong> accVec, Vector128<ulong> secret)
        {
            Vector128<ulong> xorShift = accVec ^ Vector128.ShiftRightLogical(accVec, 47);
            Vector128<ulong> xorWithKey = xorShift ^ secret;
            accVec = xorWithKey * Vector128.Create((ulong)XxHash32.Prime32_1);
            return accVec;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong XorShift(ulong value, int shift)
        {
            Debug.Assert(shift >= 0 && shift < 64);
            return value ^ (value >> shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReadUInt32LE(byte* data) =>
            BitConverter.IsLittleEndian ?
                Unsafe.ReadUnaligned<uint>(data) :
                BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadUInt64LE(byte* data) =>
            BitConverter.IsLittleEndian ?
                Unsafe.ReadUnaligned<ulong>(data) :
                BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(data));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteUInt64LE(byte* data, ulong value)
        {
            if (!BitConverter.IsLittleEndian)
            {
                value = BinaryPrimitives.ReverseEndianness(value);
            }
            Unsafe.WriteUnaligned(data, value);
        }

        [StructLayout(LayoutKind.Auto)]
        private struct State
        {
            /// <summary>The accumulators. Length is <see cref="AccumulatorCount"/>.</summary>
            internal fixed ulong Accumulators[AccumulatorCount];

            /// <summary>Used to store a custom secret generated from a seed. Length is <see cref="SecretLengthBytes"/>.</summary>
            internal fixed byte Secret[SecretLengthBytes];

            /// <summary>The internal buffer. Length is <see cref="InternalBufferLengthBytes"/>.</summary>
            internal fixed byte Buffer[InternalBufferLengthBytes];

            /// <summary>The amount of memory in <see cref="Buffer"/>.</summary>
            internal uint BufferedCount;

            /// <summary>Number of stripes processed in the current block.</summary>
            internal ulong StripesProcessedInCurrentBlock;

            /// <summary>Total length hashed.</summary>
            internal ulong TotalLength;

            /// <summary>The seed employed (possibly 0).</summary>
            internal ulong Seed;
        };
    }
}
