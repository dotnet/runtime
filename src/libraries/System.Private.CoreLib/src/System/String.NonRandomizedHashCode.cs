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
        private const uint HashPrime4 = 0x27D4EB2Fu;
        private const ulong HashSeed1 = 0x9E3779B185EBCA87;
        private const ulong HashSeed2 = 0xC2B2AE3D27D4EB4F;

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
            if (length <= 8)
            {
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
                        return (int)((first ^ (uint)length ^ HashPrime2) + (last ^ HashPrime1) * HashPrime3);
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
            if (length > 16)
                return GetNonRandomizedHashCodeLarge<TCasing>(span);

            ulong a = BitConverter.ToUInt64(span);
            ulong b = BitConverter.ToUInt64(span.Slice(length - 8));
            if (((a | b) & TCasing.NonAsciiMask) != 0)
                return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
            a |= TCasing.LowercaseMask;
            b |= TCasing.LowercaseMask;
            a += (uint)length;
            ulong hash = (a ^ BitOperations.RotateLeft(b, 27)) * HashSeed1;
            return (int)(hash ^ (hash >> 32));
        }

        // Keep the less common sizes out of callers' inlining budgets.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetNonRandomizedHashCodeLarge<TCasing>(ReadOnlySpan<byte> span) where TCasing : struct, IHashCasing
        {
            int length = span.Length;
            if (length > 64)
                return GetNonRandomizedHashCodeLong<TCasing>(span, length);
            ulong a = BitConverter.ToUInt64(span);
            ulong b = BitConverter.ToUInt64(span.Slice(length - 8));
            if (((a | b) & TCasing.NonAsciiMask) != 0)
                return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, length);
            a = (uint)length + (a | TCasing.LowercaseMask);
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
            ulong hash = (a ^ HashSeed1) * HashSeed2 +
                BitOperations.RotateLeft((b ^ HashSeed2) * HashSeed1, 27);
            return (int)(hash ^ (hash >> 32));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RoundNonRandomizedHash(uint hash, uint value) =>
            (BitOperations.RotateLeft(hash, 5) + hash) ^ value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNonRandomizedHashCodeLong<TCasing>(ReadOnlySpan<byte> span, int byteLength,
            uint h0 = 0, uint h1 = 0, uint h2 = 0, uint h3 = 0, uint tailHash = 0) where TCasing : struct, IHashCasing
        {
            uint length = (uint)byteLength;
            int initialLength = span.Length;
            if (Vector128.IsHardwareAccelerated)
            {
                Vector128<uint> hash = initialLength == byteLength
                    ? Vector128.Create(length) + Vector128.Create(HashPrime1, HashPrime2, HashPrime3, HashPrime4)
                    : Vector128.Create(h0, h1, h2, h3);
                while (span.Length >= 16)
                {
                    Vector128<uint> value = Vector128.Create(span).AsUInt32();
                    if (!Vector128.EqualsAll(value & Vector128.Create((uint)TCasing.NonAsciiMask), Vector128<uint>.Zero))
                        return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, byteLength, hash.GetElement(0), hash.GetElement(1), hash.GetElement(2), hash.GetElement(3));
                    value |= Vector128.Create((uint)TCasing.LowercaseMask);
                    hash = (((hash << 5) | (hash >> 27)) + hash) ^ value;
                    span = span.Slice(16);
                }
                h0 = hash.GetElement(0);
                h1 = hash.GetElement(1);
                h2 = hash.GetElement(2);
                h3 = hash.GetElement(3);
            }
            else
            {
                if (initialLength == byteLength)
                {
                    h0 = length + HashPrime1;
                    h1 = length + HashPrime2;
                    h2 = length + HashPrime3;
                    h3 = length + HashPrime4;
                }
                while (span.Length >= 16)
                {
                    ulong first = BitConverter.ToUInt64(span), second = BitConverter.ToUInt64(span.Slice(8));
                    if (((first | second) & TCasing.NonAsciiMask) != 0)
                        return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(span, byteLength, h0, h1, h2, h3);
                    first |= TCasing.LowercaseMask;
                    second |= TCasing.LowercaseMask;
                    if (!BitConverter.IsLittleEndian)
                    {
                        first = BitOperations.RotateLeft(first, 32);
                        second = BitOperations.RotateLeft(second, 32);
                    }
                    h0 = RoundNonRandomizedHash(h0, (uint)first);
                    h1 = RoundNonRandomizedHash(h1, (uint)(first >> 32));
                    h2 = RoundNonRandomizedHash(h2, (uint)second);
                    h3 = RoundNonRandomizedHash(h3, (uint)(second >> 32));
                    span = span.Slice(16);
                }
            }
            uint result = initialLength >= 16 ? h0 + BitOperations.RotateLeft(h1, 7) +
                BitOperations.RotateLeft(h2, 13) + BitOperations.RotateLeft(h3, 21) : tailHash;
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
            uint h0 = 0, uint h1 = 0, uint h2 = 0, uint h3 = 0, uint tailHash = 0)
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
