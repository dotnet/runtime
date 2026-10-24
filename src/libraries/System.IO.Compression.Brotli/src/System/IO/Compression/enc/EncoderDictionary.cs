// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/encoder_dict.h and encoder_dict.c.

namespace System.IO.Compression;

/// <summary>
/// Dictionary data (words and transforms) used by the encoder. Only the RFC 7932 built-in static dictionary
/// and transforms are supported, so this is the C <c>BrotliEncoderDictionary</c> as initialized by
/// <c>InitEncoderDictionary</c>; custom, contextual and compound dictionaries are not supported.
/// </summary>
internal static class EncoderDictionary
{
    /// <summary>Number of RFC 7932 transforms.</summary>
    public const uint NumTransforms = 121;

    /// <summary>Cut off for the fast encoder.</summary>
    public const uint CutoffTransformsCount = 10;

    //   0,  12,   27,    23,    42,    63,    56,    48,    59,    64
    // 0+0, 4+8, 8+19, 12+11, 16+26, 20+43, 24+32, 28+20, 32+27, 36+28
    public const ulong CutoffTransforms = 0x071B520ADA2D3200;

    /// <summary>Hash table words from dictionary_hash.h (for the fast encoder).</summary>
    public static ReadOnlySpan<ushort> HashTableWords => DictionaryHash.Words;

    /// <summary>Hash table lengths from dictionary_hash.h (for the fast encoder).</summary>
    public static ReadOnlySpan<byte> HashTableLengths => DictionaryHash.Lengths;

    /// <summary>Buckets from static_dict_lut.h (for the slow encoder).</summary>
    public static ReadOnlySpan<ushort> Buckets => StaticDictLut.Buckets;

    /// <summary>Packed <c>DictWord</c> items from static_dict_lut.h (for the slow encoder).</summary>
    public static ReadOnlySpan<uint> DictWords => StaticDictLut.Words;
}
