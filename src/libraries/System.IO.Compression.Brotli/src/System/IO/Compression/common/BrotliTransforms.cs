// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/common/transform.h and transform.c.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Compression;

internal enum BrotliWordTransformType
{
    Identity = 0,
    OmitLast1 = 1,
    OmitLast2 = 2,
    OmitLast3 = 3,
    OmitLast4 = 4,
    OmitLast5 = 5,
    OmitLast6 = 6,
    OmitLast7 = 7,
    OmitLast8 = 8,
    OmitLast9 = 9,
    UppercaseFirst = 10,
    UppercaseAll = 11,
    OmitFirst1 = 12,
    OmitFirst2 = 13,
    OmitFirst3 = 14,
    OmitFirst4 = 15,
    OmitFirst5 = 16,
    OmitFirst6 = 17,
    OmitFirst7 = 18,
    OmitFirst8 = 19,
    OmitFirst9 = 20,
    ShiftFirst = 21,
    ShiftAll = 22,
}

/// <summary>
/// The RFC 7932 dictionary word transforms (the only transform set supported by this implementation;
/// custom transforms from shared dictionaries, and hence the SHIFT transforms, are not supported).
/// </summary>
internal static class BrotliTransforms
{
    public const int MaxCutOff = (int)BrotliWordTransformType.OmitLast9;

    /// <summary>Number of transforms (RFC 7932, Appendix B).</summary>
    public const int NumTransforms = 121;

    /// <summary>
    /// RFC 7932 transforms string data: a sequence of length-prefixed strings; the last character is 0.
    /// </summary>
    public static ReadOnlySpan<byte> PrefixSuffix =>
    [
        1, 32, 2, 44, 32, 8, 32, 111, 102, 32, 116, 104, 101, 32, 4, 32,
        111, 102, 32, 2, 115, 32, 1, 46, 5, 32, 97, 110, 100, 32, 4, 32,
        105, 110, 32, 1, 34, 4, 32, 116, 111, 32, 2, 34, 62, 1, 10, 2,
        46, 32, 1, 93, 5, 32, 102, 111, 114, 32, 3, 32, 97, 32, 6, 32,
        116, 104, 97, 116, 32, 1, 39, 6, 32, 119, 105, 116, 104, 32, 6, 32,
        102, 114, 111, 109, 32, 4, 32, 98, 121, 32, 1, 40, 6, 46, 32, 84,
        104, 101, 32, 4, 32, 111, 110, 32, 4, 32, 97, 115, 32, 4, 32, 105,
        115, 32, 4, 105, 110, 103, 32, 2, 10, 9, 1, 58, 3, 101, 100, 32,
        2, 61, 34, 4, 32, 97, 116, 32, 3, 108, 121, 32, 1, 44, 2, 61,
        39, 5, 46, 99, 111, 109, 47, 7, 46, 32, 84, 104, 105, 115, 32, 5,
        32, 110, 111, 116, 32, 3, 101, 114, 32, 3, 97, 108, 32, 4, 102, 117,
        108, 32, 4, 105, 118, 101, 32, 5, 108, 101, 115, 115, 32, 4, 101, 115,
        116, 32, 4, 105, 122, 101, 32, 2, 194, 160, 4, 111, 117, 115, 32, 5,
        32, 116, 104, 101, 32, 2, 101, 32, 0,
    ];
    public static ReadOnlySpan<ushort> PrefixSuffixMap =>
    [
        0x00, 0x02, 0x05, 0x0E, 0x13, 0x16, 0x18, 0x1E, 0x23, 0x25,
        0x2A, 0x2D, 0x2F, 0x32, 0x34, 0x3A, 0x3E, 0x45, 0x47, 0x4E,
        0x55, 0x5A, 0x5C, 0x63, 0x68, 0x6D, 0x72, 0x77, 0x7A, 0x7C,
        0x80, 0x83, 0x88, 0x8C, 0x8E, 0x91, 0x97, 0x9F, 0xA5, 0xA9,
        0xAD, 0xB2, 0xB7, 0xBD, 0xC2, 0xC7, 0xCA, 0xCF, 0xD5, 0xD8,
    ];

    /// <summary>RFC 7932 transforms: each entry is a [prefix_id, transform type, suffix_id] triplet.</summary>
    public static ReadOnlySpan<byte> TransformsData =>
    [
        49, 0, 49,
        49, 0, 0,
        0, 0, 0,
        49, 12, 49,
        49, 10, 0,
        49, 0, 47,
        0, 0, 49,
        4, 0, 0,
        49, 0, 3,
        49, 10, 49,
        49, 0, 6,
        49, 13, 49,
        49, 1, 49,
        1, 0, 0,
        49, 0, 1,
        0, 10, 0,
        49, 0, 7,
        49, 0, 9,
        48, 0, 0,
        49, 0, 8,
        49, 0, 5,
        49, 0, 10,
        49, 0, 11,
        49, 3, 49,
        49, 0, 13,
        49, 0, 14,
        49, 14, 49,
        49, 2, 49,
        49, 0, 15,
        49, 0, 16,
        0, 10, 49,
        49, 0, 12,
        5, 0, 49,
        0, 0, 1,
        49, 15, 49,
        49, 0, 18,
        49, 0, 17,
        49, 0, 19,
        49, 0, 20,
        49, 16, 49,
        49, 17, 49,
        47, 0, 49,
        49, 4, 49,
        49, 0, 22,
        49, 11, 49,
        49, 0, 23,
        49, 0, 24,
        49, 0, 25,
        49, 7, 49,
        49, 1, 26,
        49, 0, 27,
        49, 0, 28,
        0, 0, 12,
        49, 0, 29,
        49, 20, 49,
        49, 18, 49,
        49, 6, 49,
        49, 0, 21,
        49, 10, 1,
        49, 8, 49,
        49, 0, 31,
        49, 0, 32,
        47, 0, 3,
        49, 5, 49,
        49, 9, 49,
        0, 10, 1,
        49, 10, 8,
        5, 0, 21,
        49, 11, 0,
        49, 10, 10,
        49, 0, 30,
        0, 0, 5,
        35, 0, 49,
        47, 0, 2,
        49, 10, 17,
        49, 0, 36,
        49, 0, 33,
        5, 0, 0,
        49, 10, 21,
        49, 10, 5,
        49, 0, 37,
        0, 0, 30,
        49, 0, 38,
        0, 11, 0,
        49, 0, 39,
        0, 11, 49,
        49, 0, 34,
        49, 11, 8,
        49, 10, 12,
        0, 0, 21,
        49, 0, 40,
        0, 10, 12,
        49, 0, 41,
        49, 0, 42,
        49, 11, 17,
        49, 0, 43,
        0, 10, 5,
        49, 11, 10,
        0, 0, 34,
        49, 10, 33,
        49, 0, 44,
        49, 11, 5,
        45, 0, 49,
        0, 0, 33,
        49, 10, 30,
        49, 11, 30,
        49, 0, 46,
        49, 11, 1,
        49, 10, 34,
        0, 10, 33,
        0, 11, 30,
        0, 11, 1,
        49, 11, 33,
        49, 11, 21,
        49, 11, 12,
        0, 11, 5,
        49, 11, 34,
        0, 11, 12,
        0, 10, 30,
        0, 11, 34,
        0, 10, 34,
    ];

    /// <summary>
    /// Indices of transforms like ["", OmitLast#, ""]. 0-th element corresponds to ["", Identity, ""].
    /// </summary>
    public static ReadOnlySpan<short> CutOffTransforms => [0, 12, 27, 23, 42, 63, 56, 48, 59, 64];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte PrefixId(int transformIdx) => TransformsData[transformIdx * 3];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte TransformType(int transformIdx) => TransformsData[transformIdx * 3 + 1];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte SuffixId(int transformIdx) => TransformsData[transformIdx * 3 + 2];

    private static int ToUpperCase(Span<byte> p)
    {
        if (p[0] < 0xC0)
        {
            if (p[0] >= 'a' && p[0] <= 'z')
            {
                p[0] ^= 32;
            }
            return 1;
        }

        // An overly simplified uppercasing model for UTF-8.
        if (p[0] < 0xE0)
        {
            p[1] ^= 32;
            return 2;
        }

        // An arbitrary transform for three byte characters.
        p[2] ^= 5;
        return 3;
    }

    /// <summary>
    /// Writes the transformed dictionary word to <paramref name="dst"/> and returns the number of bytes written.
    /// </summary>
    /// <param name="dst">Destination; may be written past the returned length (C relies on ring buffer slack).</param>
    /// <param name="word">The dictionary word (starting at its first byte).</param>
    /// <param name="len">Length of the dictionary word.</param>
    /// <param name="transformIdx">Index of the transform, 0..<see cref="NumTransforms"/>-1.</param>
    public static int TransformDictionaryWord(Span<byte> dst, ReadOnlySpan<byte> word, int len, int transformIdx)
    {
        ReadOnlySpan<byte> prefixSuffix = PrefixSuffix;
        int prefix = PrefixSuffixMap[PrefixId(transformIdx)];
        int type = TransformType(transformIdx);
        int suffix = PrefixSuffixMap[SuffixId(transformIdx)];
        int idx;
        {
            int prefixLen = prefixSuffix[prefix];
            prefixSuffix.Slice(prefix + 1, prefixLen).CopyTo(dst);
            idx = prefixLen;
        }

        {
            int t = type;
            int wordStart = 0;
            if (t <= (int)BrotliWordTransformType.OmitLast9)
            {
                len -= t;
            }
            else if (t >= (int)BrotliWordTransformType.OmitFirst1 && t <= (int)BrotliWordTransformType.OmitFirst9)
            {
                int skip = t - ((int)BrotliWordTransformType.OmitFirst1 - 1);
                wordStart = skip;
                len -= skip;
            }

            if (len > 0)
            {
                word.Slice(wordStart, len).CopyTo(dst.Slice(idx));
                idx += len;
            }

            if (t == (int)BrotliWordTransformType.UppercaseFirst)
            {
                ToUpperCase(dst.Slice(idx - len));
            }
            else if (t == (int)BrotliWordTransformType.UppercaseAll)
            {
                int uppercase = idx - len;
                while (len > 0)
                {
                    int step = ToUpperCase(dst.Slice(uppercase));
                    uppercase += step;
                    len -= step;
                }
            }
            else
            {
                // SHIFT_FIRST / SHIFT_ALL transforms only exist in custom transform sets (not supported).
                Debug.Assert(t != (int)BrotliWordTransformType.ShiftFirst && t != (int)BrotliWordTransformType.ShiftAll);
            }
        }

        {
            int suffixLen = prefixSuffix[suffix];
            prefixSuffix.Slice(suffix + 1, suffixLen).CopyTo(dst.Slice(idx));
            return idx + suffixLen;
        }
    }
}
