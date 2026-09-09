// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System
{
    public partial class String
    {
        // xxHash primes are used as fixed mixing constants, not its hashing algorithm.
        private const uint HashPrime1 = 0x9E3779B1u;
        private const uint HashPrime2 = 0x85EBCA77u;
        private const uint HashPrime3 = 0xC2B2AE3Du;
        private const ulong HashSeed1 = 0x9E3779B185EBCA87;
        private const ulong HashSeed2 = 0xC2B2AE3D27D4EB4F;

        // Below this the eight-lane setup and fold cost more than the extra width saves.
        private const int WideHashThreshold = 256;

        // Use only when collision attacks are otherwise mitigated. String and span entrypoints
        // must agree; all reads are bounded by the span and do not include a string terminator.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNonRandomizedHashCode() =>
            GetNonRandomizedHashCode(this.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetNonRandomizedHashCode(ReadOnlySpan<char> span) =>
            span.Length <= int.MaxValue / sizeof(char) ? GetNonRandomizedHashCode(MemoryMarshal.AsBytes(span)) :
            GetNonRandomizedHashCodeVeryLarge<CaseSensitiveHashing>(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetNonRandomizedHashCode(ReadOnlySpan<byte> span) =>
            GetNonRandomizedHashCodeCore<CaseSensitiveHashing>(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetNonRandomizedHashCodeOrdinalIgnoreCase() =>
            GetNonRandomizedHashCodeOrdinalIgnoreCase(this.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetNonRandomizedHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> span) =>
            span.Length <= int.MaxValue / sizeof(char) ? GetNonRandomizedHashCodeCore<IgnoreCaseHashing>(MemoryMarshal.AsBytes(span)) :
            GetNonRandomizedHashCodeVeryLarge<IgnoreCaseHashing>(span);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetNonRandomizedHashCodeVeryLarge<TCasing>(ReadOnlySpan<char> span) where TCasing : struct, IHashCasing
        {
            const int ChunkLength = 1024;
            uint hash = (uint)span.Length;
            while (!span.IsEmpty)
            {
                int length = Math.Min(span.Length, ChunkLength);
                // A byte span cannot represent the whole input; keep ordinal casing pairs in the same chunk.
                if (length < span.Length && char.IsHighSurrogate(span[length - 1]) && char.IsLowSurrogate(span[length]))
                    length--;
                hash = RoundNonRandomizedHash(hash, (uint)GetNonRandomizedHashCodeCore<TCasing>(
                    MemoryMarshal.AsBytes(span.Slice(0, length))));
                span = span.Slice(length);
            }
            return (int)hash;
        }

        private interface IHashCasing
        {
            static abstract ulong NonAsciiMask { get; }
            static abstract ulong LowercaseMask { get; }
        }
        private readonly struct CaseSensitiveHashing : IHashCasing
        {
            public static ulong NonAsciiMask => 0;
            public static ulong LowercaseMask => 0;
        }
        private readonly struct IgnoreCaseHashing : IHashCasing
        {
            public static ulong NonAsciiMask => 0xFF80FF80FF80FF80;
            // Non-letters are also folded; equality resolves the additional collisions.
            public static ulong LowercaseMask => 0x0020002000200020;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNonRandomizedHashCodeCore<TCasing>(ReadOnlySpan<byte> span) where TCasing : struct, IHashCasing
        {
            Debug.Assert(TCasing.NonAsciiMask == 0 || (span.Length & 1) == 0);
            int length = span.Length;
            if (length > 8)
            {
                if (length > 32)
                    return GetNonRandomizedHashCodeLarge<TCasing>(span);

                // Two overlapping halves cover every byte, so keep 9..32 - the range that
                // holds most dictionary keys - free of calls.
                ulong a = BitConverter.ToUInt64(span);
                ulong b = BitConverter.ToUInt64(span.Slice(length - 8));
                if (((a | b) & TCasing.NonAsciiMask) != 0)
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
                a = (uint)length * HashPrime1 + (a | TCasing.LowercaseMask);
                b |= TCasing.LowercaseMask;
                if (length > 16)
                {
                    // Mix overlapping loads at different bit offsets.
                    ulong first = BitConverter.ToUInt64(span.Slice(8)), last = BitConverter.ToUInt64(span.Slice(length - 16));
                    if (((first | last) & TCasing.NonAsciiMask) != 0)
                        return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
                    a += BitOperations.RotateLeft(first | TCasing.LowercaseMask, 23);
                    b ^= BitOperations.RotateLeft(last | TCasing.LowercaseMask, 25);
                }
                return MixNonRandomizedHash(a, b);
            }

            if (length >= 4)
            {
                uint first = BitConverter.ToUInt32(span);
                if (length > 4)
                {
                    uint last = BitConverter.ToUInt32(span.Slice(length - 4));
                    if (((first | last) & (uint)TCasing.NonAsciiMask) != 0)
                        return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
                    first |= (uint)TCasing.LowercaseMask;
                    last |= (uint)TCasing.LowercaseMask;
                    // Spread the length before mixing it in. Combining it linearly cancels
                    // against a leading or trailing char whose delta matches the length
                    // delta, which collides whole families such as "600"/"8000".
                    return (int)((first ^ ((uint)length * HashPrime1) ^ HashPrime2) + (last ^ HashPrime1) * HashPrime3);
                }
                if ((first & (uint)TCasing.NonAsciiMask) != 0)
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
                first |= (uint)TCasing.LowercaseMask;
                return (int)((first ^ (uint)length) * HashPrime1);
            }
            if (length >= 2)
            {
                uint first = BitConverter.ToUInt16(span);
                if ((first & (ushort)TCasing.NonAsciiMask) != 0)
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
                first |= (ushort)TCasing.LowercaseMask;
                if (length == 3)
                    return (int)(((first | ((uint)span[2] << 16)) ^ 3u) * HashPrime1);
                return (int)(first + HashPrime2);
            }
            return length == 0 ? 0 : (int)(span[0] + HashPrime1);
        }

        // Keep the less common sizes out of callers' inlining budgets.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetNonRandomizedHashCodeLarge<TCasing>(ReadOnlySpan<byte> span) where TCasing : struct, IHashCasing
        {
            int length = span.Length;
            Debug.Assert(length > 32);
            if (length > 64)
                return GetNonRandomizedHashCodeLong<TCasing>(span, length);
            ulong a = BitConverter.ToUInt64(span);
            ulong b = BitConverter.ToUInt64(span.Slice(length - 8));
            if (((a | b) & TCasing.NonAsciiMask) != 0)
                return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
            a = (uint)length * HashPrime1 + (a | TCasing.LowercaseMask);
            b |= TCasing.LowercaseMask;
            // The first guard is redundant for a caller-checked length, but it keeps the
            // slice bounds provable so the loads stay check-free.
            if (length > 16)
            {
                // Mix overlapping loads at different bit offsets.
                ulong first = BitConverter.ToUInt64(span.Slice(8)), last = BitConverter.ToUInt64(span.Slice(length - 16));
                if (((first | last) & TCasing.NonAsciiMask) != 0)
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
                a += BitOperations.RotateLeft(first | TCasing.LowercaseMask, 23);
                b ^= BitOperations.RotateLeft(last | TCasing.LowercaseMask, 25);
            }
            if (length > 32)
            {
                ulong first = BitConverter.ToUInt64(span.Slice(16)), last = BitConverter.ToUInt64(span.Slice(length - 24));
                ulong next = BitConverter.ToUInt64(span.Slice(24)), previous = BitConverter.ToUInt64(span.Slice(length - 32));
                if (((first | last | next | previous) & TCasing.NonAsciiMask) != 0)
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
                a += BitOperations.RotateLeft(first | TCasing.LowercaseMask, 37);
                b += BitOperations.RotateLeft(last | TCasing.LowercaseMask, 37);
                a += BitOperations.RotateLeft(next | TCasing.LowercaseMask, 49);
                b += BitOperations.RotateLeft(previous | TCasing.LowercaseMask, 49);
            }
            return MixNonRandomizedHash(a, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MixNonRandomizedHash(ulong a, ulong b)
        {
            // Folding b in at two rotations before a single multiply spreads the low bits
            // as well as two multiplies did, and needs only one 64-bit constant.
            ulong hash = ((a ^ BitOperations.RotateLeft(b, 27)) + BitOperations.RotateLeft(b, 41)) * HashSeed1;
            return (int)(hash ^ (hash >> 32));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RoundNonRandomizedHash(uint hash, uint value) =>
            (BitOperations.RotateLeft(hash, 5) + hash) ^ value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RoundNonRandomizedHash64(ulong hash, ulong value) =>
            (hash + (hash << 5)) ^ value;

        // Absorbs 64 bytes at a time into eight 64-bit lanes, then folds them back into
        // four. Lane i always takes the ulong at byte offset 8 * i, so every width here
        // and the scalar loop produce the same hash. Returns false if a non-ASCII block
        // was reached, leaving span at that block.
        // Must stay inlined: the loop state is passed by reference, so an out-of-line call
        // would keep the accumulators and the span in memory across every iteration.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AbsorbWideNonRandomizedHash<TCasing>(ref ReadOnlySpan<byte> span,
            ref ulong h0, ref ulong h1, ref ulong h2, ref ulong h3) where TCasing : struct, IHashCasing
        {
            ulong h4 = BitOperations.RotateLeft(h0, 32), h5 = BitOperations.RotateLeft(h1, 32);
            ulong h6 = BitOperations.RotateLeft(h2, 32), h7 = BitOperations.RotateLeft(h3, 32);
            bool ascii = true;
            if (Vector512.IsHardwareAccelerated)
            {
                Vector512<ulong> hash = Vector512.Create(h0, h1, h2, h3, h4, h5, h6, h7);
                while (span.Length >= 64)
                {
                    Vector512<ulong> value = Vector512.Create(span).AsUInt64();
                    if (!Vector512.EqualsAll(value & Vector512.Create(TCasing.NonAsciiMask), Vector512<ulong>.Zero))
                    {
                        ascii = false;
                        break;
                    }
                    hash = (hash + (hash << 5)) ^ (value | Vector512.Create(TCasing.LowercaseMask));
                    span = span.Slice(64);
                }
                h0 = hash.GetElement(0);
                h1 = hash.GetElement(1);
                h2 = hash.GetElement(2);
                h3 = hash.GetElement(3);
                h4 = hash.GetElement(4);
                h5 = hash.GetElement(5);
                h6 = hash.GetElement(6);
                h7 = hash.GetElement(7);
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                Vector256<ulong> hashLow = Vector256.Create(h0, h1, h2, h3);
                Vector256<ulong> hashHigh = Vector256.Create(h4, h5, h6, h7);
                while (span.Length >= 64)
                {
                    Vector256<ulong> low = Vector256.Create(span).AsUInt64();
                    Vector256<ulong> high = Vector256.Create(span.Slice(32)).AsUInt64();
                    if (!Vector256.EqualsAll((low | high) & Vector256.Create(TCasing.NonAsciiMask), Vector256<ulong>.Zero))
                    {
                        ascii = false;
                        break;
                    }
                    hashLow = (hashLow + (hashLow << 5)) ^ (low | Vector256.Create(TCasing.LowercaseMask));
                    hashHigh = (hashHigh + (hashHigh << 5)) ^ (high | Vector256.Create(TCasing.LowercaseMask));
                    span = span.Slice(64);
                }
                h0 = hashLow.GetElement(0);
                h1 = hashLow.GetElement(1);
                h2 = hashLow.GetElement(2);
                h3 = hashLow.GetElement(3);
                h4 = hashHigh.GetElement(0);
                h5 = hashHigh.GetElement(1);
                h6 = hashHigh.GetElement(2);
                h7 = hashHigh.GetElement(3);
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                Vector128<ulong> hash0 = Vector128.Create(h0, h1), hash1 = Vector128.Create(h2, h3);
                Vector128<ulong> hash2 = Vector128.Create(h4, h5), hash3 = Vector128.Create(h6, h7);
                while (span.Length >= 64)
                {
                    Vector128<ulong> v0 = Vector128.Create(span).AsUInt64();
                    Vector128<ulong> v1 = Vector128.Create(span.Slice(16)).AsUInt64();
                    Vector128<ulong> v2 = Vector128.Create(span.Slice(32)).AsUInt64();
                    Vector128<ulong> v3 = Vector128.Create(span.Slice(48)).AsUInt64();
                    if (!Vector128.EqualsAll((v0 | v1 | v2 | v3) & Vector128.Create(TCasing.NonAsciiMask), Vector128<ulong>.Zero))
                    {
                        ascii = false;
                        break;
                    }
                    Vector128<ulong> lowercase = Vector128.Create(TCasing.LowercaseMask);
                    hash0 = (hash0 + (hash0 << 5)) ^ (v0 | lowercase);
                    hash1 = (hash1 + (hash1 << 5)) ^ (v1 | lowercase);
                    hash2 = (hash2 + (hash2 << 5)) ^ (v2 | lowercase);
                    hash3 = (hash3 + (hash3 << 5)) ^ (v3 | lowercase);
                    span = span.Slice(64);
                }
                h0 = hash0.GetElement(0);
                h1 = hash0.GetElement(1);
                h2 = hash1.GetElement(0);
                h3 = hash1.GetElement(1);
                h4 = hash2.GetElement(0);
                h5 = hash2.GetElement(1);
                h6 = hash3.GetElement(0);
                h7 = hash3.GetElement(1);
            }
            else
            {
                while (span.Length >= 64)
                {
                    ulong v0 = BitConverter.ToUInt64(span), v1 = BitConverter.ToUInt64(span.Slice(8));
                    ulong v2 = BitConverter.ToUInt64(span.Slice(16)), v3 = BitConverter.ToUInt64(span.Slice(24));
                    ulong v4 = BitConverter.ToUInt64(span.Slice(32)), v5 = BitConverter.ToUInt64(span.Slice(40));
                    ulong v6 = BitConverter.ToUInt64(span.Slice(48)), v7 = BitConverter.ToUInt64(span.Slice(56));
                    if (((v0 | v1 | v2 | v3 | v4 | v5 | v6 | v7) & TCasing.NonAsciiMask) != 0)
                    {
                        ascii = false;
                        break;
                    }
                    h0 = RoundNonRandomizedHash64(h0, v0 | TCasing.LowercaseMask);
                    h1 = RoundNonRandomizedHash64(h1, v1 | TCasing.LowercaseMask);
                    h2 = RoundNonRandomizedHash64(h2, v2 | TCasing.LowercaseMask);
                    h3 = RoundNonRandomizedHash64(h3, v3 | TCasing.LowercaseMask);
                    h4 = RoundNonRandomizedHash64(h4, v4 | TCasing.LowercaseMask);
                    h5 = RoundNonRandomizedHash64(h5, v5 | TCasing.LowercaseMask);
                    h6 = RoundNonRandomizedHash64(h6, v6 | TCasing.LowercaseMask);
                    h7 = RoundNonRandomizedHash64(h7, v7 | TCasing.LowercaseMask);
                    span = span.Slice(64);
                }
            }
            h0 ^= BitOperations.RotateLeft(h4, 17);
            h1 ^= BitOperations.RotateLeft(h5, 23);
            h2 ^= BitOperations.RotateLeft(h6, 29);
            h3 ^= BitOperations.RotateLeft(h7, 37);
            return ascii;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNonRandomizedHashCodeLong<TCasing>(ReadOnlySpan<byte> span, int byteLength,
            ulong h0 = 0, ulong h1 = 0, ulong h2 = 0, ulong h3 = 0, uint tailHash = 0) where TCasing : struct, IHashCasing
        {
            ulong length = (uint)byteLength;
            int initialLength = span.Length;
            if (initialLength == byteLength)
            {
                h0 = length + HashSeed1;
                h1 = length + HashSeed2;
                h2 = length + ~HashSeed1;
                h3 = length + ~HashSeed2;

                // Eight lanes absorb twice the bytes per dependent step, which is what this
                // loop is limited by. Seeding and folding eight lanes only pays off once
                // there are enough blocks to amortise it. Continuations stay four wide so
                // that the carried state does.
                if (span.Length >= WideHashThreshold &&
                    !AbsorbWideNonRandomizedHash<TCasing>(ref span, ref h0, ref h1, ref h2, ref h3))
                {
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, byteLength, h0, h1, h2, h3);
                }
            }
            if (Vector128.IsHardwareAccelerated)
            {
                Vector128<ulong> hash0 = Vector128.Create(h0, h1);
                Vector128<ulong> hash1 = Vector128.Create(h2, h3);
                while (span.Length >= 32)
                {
                    Vector128<ulong> value0 = Vector128.Create(span).AsUInt64();
                    Vector128<ulong> value1 = Vector128.Create(span.Slice(16)).AsUInt64();
                    if (!Vector128.EqualsAll((value0 | value1) & Vector128.Create(TCasing.NonAsciiMask), Vector128<ulong>.Zero))
                        return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, byteLength, hash0.GetElement(0), hash0.GetElement(1), hash1.GetElement(0), hash1.GetElement(1));
                    value0 |= Vector128.Create(TCasing.LowercaseMask);
                    value1 |= Vector128.Create(TCasing.LowercaseMask);
                    hash0 = (hash0 + (hash0 << 5)) ^ value0;
                    hash1 = (hash1 + (hash1 << 5)) ^ value1;
                    span = span.Slice(32);
                }
                h0 = hash0.GetElement(0);
                h1 = hash0.GetElement(1);
                h2 = hash1.GetElement(0);
                h3 = hash1.GetElement(1);
            }
            else
            {
                while (span.Length >= 32)
                {
                    ulong first = BitConverter.ToUInt64(span), second = BitConverter.ToUInt64(span.Slice(8));
                    ulong third = BitConverter.ToUInt64(span.Slice(16)), fourth = BitConverter.ToUInt64(span.Slice(24));
                    if (((first | second | third | fourth) & TCasing.NonAsciiMask) != 0)
                        return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, byteLength, h0, h1, h2, h3);
                    h0 = RoundNonRandomizedHash64(h0, first | TCasing.LowercaseMask);
                    h1 = RoundNonRandomizedHash64(h1, second | TCasing.LowercaseMask);
                    h2 = RoundNonRandomizedHash64(h2, third | TCasing.LowercaseMask);
                    h3 = RoundNonRandomizedHash64(h3, fourth | TCasing.LowercaseMask);
                    span = span.Slice(32);
                }
            }
            uint result = initialLength >= 32 ? (uint)MixNonRandomizedHash(
                h0 + BitOperations.RotateLeft(h1, 23), h2 + BitOperations.RotateLeft(h3, 37)) : tailHash;
            while (span.Length >= 4)
            {
                uint value = BitConverter.ToUInt32(span);
                if ((value & (uint)TCasing.NonAsciiMask) != 0)
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, byteLength, tailHash: result);
                result = RoundNonRandomizedHash(result, value | (uint)TCasing.LowercaseMask);
                span = span.Slice(4);
            }
            if (span.Length >= 2)
            {
                uint value = BitConverter.ToUInt16(span);
                if ((value & (ushort)TCasing.NonAsciiMask) != 0)
                    return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, byteLength, tailHash: result);
                result = RoundNonRandomizedHash(result, value | (ushort)TCasing.LowercaseMask);
                span = span.Slice(2);
            }
            if (!span.IsEmpty)
                result = RoundNonRandomizedHash(result, span[0]);
            return (int)(result ^ (result >> 16));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(ReadOnlySpan<byte> remaining, int byteLength,
            ulong h0 = 0, ulong h1 = 0, ulong h2 = 0, ulong h3 = 0, uint tailHash = 0)
        {
            int length = remaining.Length / sizeof(char);
            char[]? borrowedSource = null, borrowedScratch = null;
            Span<char> source = (uint)length < 256 ? stackalloc char[256] :
                (borrowedSource = ArrayPool<char>.Shared.Rent(length));
            try
            {
                // Separate buffers avoid a 2 * length allocation exceeding Array.MaxLength.
                Span<char> scratch = (uint)length < 256 ? stackalloc char[256] :
                    (borrowedScratch = ArrayPool<char>.Shared.Rent(length));
                source = source.Slice(0, length);
                scratch = scratch.Slice(0, length);
                remaining.CopyTo(MemoryMarshal.AsBytes(source));
                int charsWritten = Ordinal.ToUpperOrdinal(source, scratch);
                Debug.Assert(charsWritten == length);
                for (int i = 0; i < charsWritten; i++)
                    scratch[i] |= (char)0x20;
                ReadOnlySpan<byte> normalized = MemoryMarshal.AsBytes(scratch.Slice(0, charsWritten));
                return byteLength <= 64 ? GetNonRandomizedHashCodeCore<CaseSensitiveHashing>(normalized) :
                    GetNonRandomizedHashCodeLong<CaseSensitiveHashing>(normalized, byteLength, h0, h1, h2, h3, tailHash);
            }
            finally
            {
                if (borrowedScratch != null)
                    ArrayPool<char>.Shared.Return(borrowedScratch);
                if (borrowedSource != null)
                    ArrayPool<char>.Shared.Return(borrowedSource);
            }
        }
    }
}
