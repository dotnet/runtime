// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Numerics;

namespace SuperPmi;

// MurmurHash3 (x64, 128-bit, seed 0), written by Austin Appleby and placed in the public domain.
// https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
public static class Hash
{
    public const int MM3_HASH_BYTE_SIZE = 16;
    public const int MM3_HASH_BUFFER_SIZE = 33; // 32 hex characters plus a null terminator

    private static ulong FMix64(ulong k)
    {
        k ^= k >> 33;
        k *= 0xff51afd7ed558ccdUL;
        k ^= k >> 33;
        k *= 0xc4ceb9fe1a85ec53UL;
        k ^= k >> 33;
        return k;
    }

    public static void MurmurHash3_128(ReadOnlySpan<byte> data, uint seed, Span<byte> output)
    {
        const ulong c1 = 0x87c37b91114253d5UL;
        const ulong c2 = 0x4cf5ad432745937fUL;

        int nblocks = data.Length / MM3_HASH_BYTE_SIZE;
        ulong h1 = seed;
        ulong h2 = seed;

        for (int i = 0; i < nblocks; i++)
        {
            ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(i * 16));
            ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(i * 16 + 8));

            k1 *= c1; k1 = BitOperations.RotateLeft(k1, 31); k1 *= c2; h1 ^= k1;
            h1 = BitOperations.RotateLeft(h1, 27); h1 += h2; h1 = h1 * 5 + 0x52dce729;
            k2 *= c2; k2 = BitOperations.RotateLeft(k2, 33); k2 *= c1; h2 ^= k2;
            h2 = BitOperations.RotateLeft(h2, 31); h2 += h1; h2 = h2 * 5 + 0x38495ab5;
        }

        ReadOnlySpan<byte> tail = data.Slice(nblocks * MM3_HASH_BYTE_SIZE);
        ulong t1 = 0;
        ulong t2 = 0;
        for (int i = tail.Length - 1; i >= 8; i--)
            t2 ^= (ulong)tail[i] << ((i - 8) * 8);
        for (int i = Math.Min(tail.Length, 8) - 1; i >= 0; i--)
            t1 ^= (ulong)tail[i] << (i * 8);

        if (tail.Length > 8)
        {
            t2 *= c2; t2 = BitOperations.RotateLeft(t2, 33); t2 *= c1; h2 ^= t2;
        }
        if (tail.Length > 0)
        {
            t1 *= c1; t1 = BitOperations.RotateLeft(t1, 31); t1 *= c2; h1 ^= t1;
        }

        h1 ^= (ulong)data.Length;
        h2 ^= (ulong)data.Length;
        h1 += h2;
        h2 += h1;
        h1 = FMix64(h1);
        h2 = FMix64(h2);
        h1 += h2;
        h2 += h1;

        BinaryPrimitives.WriteUInt64LittleEndian(output, h1);
        BinaryPrimitives.WriteUInt64LittleEndian(output.Slice(8), h2);
    }

    // Hashes 'data' and returns the 32-character upper-case hexadecimal text of the hash.
    public static string HashBuffer(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[MM3_HASH_BYTE_SIZE];
        MurmurHash3_128(data, 0, hash);
        return Convert.ToHexString(hash);
    }
}
