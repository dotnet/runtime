// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/params.h and c/enc/quality.h.

namespace System.IO.Compression;

internal enum EncoderMode
{
    Generic = 0,
    Text = 1,
    Font = 2,
}

internal struct HasherParams
{
    public int Type;
    public int BucketBits;
    public int BlockBits;
    public int NumLastDistancesToCheck;
}

internal struct DistanceParams
{
    public uint DistancePostfixBits;
    public uint NumDirectDistanceCodes;
    public uint AlphabetSizeMax;
    public uint AlphabetSizeLimit;
    public nuint MaxDistance;
}

/// <summary>Encoding parameters.</summary>
internal struct EncoderParams
{
    public EncoderMode Mode;
    public int Quality;
    public int Lgwin;
    public int Lgblock;
    public nuint StreamOffset;
    public nuint SizeHint;
    public bool DisableLiteralContextModeling;
    public bool LargeWindow;
    public HasherParams Hasher;
    public DistanceParams Dist;
}

/// <summary>Constants and functions for encoder quality levels.</summary>
internal static class Quality
{
    public const int FastOnePassCompressionQuality = 0;
    public const int FastTwoPassCompressionQuality = 1;
    public const int ZopflificationQuality = 10;
    public const int HqZopflificationQuality = 11;

    public const int MaxQualityForStaticEntropyCodes = 2;
    public const int MinQualityForBlockSplit = 4;
    public const int MinQualityForNonzeroDistanceParams = 4;
    public const int MinQualityForOptimizeHistograms = 4;
    public const int MinQualityForExtensiveReferenceSearch = 5;
    public const int MinQualityForContextModeling = 5;
    public const int MinQualityForHqContextModeling = 7;
    public const int MinQualityForHqBlockSplitting = 10;

    // The SIMD hashers (H58, H68) are used for qualities up to this value; they produce the same output as H5/H6.
    public const int MaxSimdQuality = 7;

    /// <summary>
    /// For quality below MIN_QUALITY_FOR_BLOCK_SPLIT there is no block splitting,
    /// so we buffer at most this much literals and commands.
    /// </summary>
    public const int MaxNumDelayedSymbols = 0x2FFF;

    public const int MaxZopfliLenQuality10 = 150;
    public const int MaxZopfliLenQuality11 = 325;

    public const int LongCopyQuickStep = 16384;

    /// <summary>Returns hash-table size for quality levels 0 and 1.</summary>
    public static nuint MaxHashTableSize(int quality) =>
        quality == FastOnePassCompressionQuality ? (nuint)1 << 15 : (nuint)1 << 17;

    public static nuint MaxZopfliLen(in EncoderParams p) =>
        p.Quality <= 10 ? (nuint)MaxZopfliLenQuality10 : (nuint)MaxZopfliLenQuality11;

    /// <summary>Number of best candidates to evaluate to expand Zopfli chain.</summary>
    public static nuint MaxZopfliCandidates(in EncoderParams p) => p.Quality <= 10 ? 1u : 5u;

    public static void SanitizeParams(ref EncoderParams p)
    {
        p.Quality = Math.Min(BrotliConstants.MaxQuality, Math.Max(BrotliConstants.MinQuality, p.Quality));
        if (p.Quality <= MaxQualityForStaticEntropyCodes)
        {
            p.LargeWindow = false;
        }
        if (p.Lgwin < BrotliConstants.MinWindowBits)
        {
            p.Lgwin = BrotliConstants.MinWindowBits;
        }
        else
        {
            int maxLgwin = p.LargeWindow ? BrotliConstants.LargeMaxWBits : BrotliConstants.MaxWindowBits;
            if (p.Lgwin > maxLgwin)
            {
                p.Lgwin = maxLgwin;
            }
        }
    }

    /// <summary>Returns optimized lg_block value.</summary>
    public static int ComputeLgBlock(in EncoderParams p)
    {
        int lgblock = p.Lgblock;
        if (p.Quality == FastOnePassCompressionQuality || p.Quality == FastTwoPassCompressionQuality)
        {
            lgblock = p.Lgwin;
        }
        else if (p.Quality < MinQualityForBlockSplit)
        {
            lgblock = 14;
        }
        else if (lgblock == 0)
        {
            lgblock = 16;
            if (p.Quality >= 9 && p.Lgwin > lgblock)
            {
                lgblock = Math.Min(18, p.Lgwin);
            }
        }
        else
        {
            lgblock = Math.Min(BrotliConstants.MaxInputBlockBits, Math.Max(BrotliConstants.MinInputBlockBits, lgblock));
        }
        return lgblock;
    }

    /// <summary>
    /// Returns log2 of the size of main ring buffer area.
    /// Allocate at least lgwin + 1 bits for the ring buffer so that the newly
    /// added block fits there completely and we still get lgwin bits and at least
    /// read_block_size_bits + 1 bits because the copy tail length needs to be
    /// smaller than ring-buffer size.
    /// </summary>
    public static int ComputeRbBits(in EncoderParams p) => 1 + Math.Max(p.Lgwin, p.Lgblock);

    public static nuint MaxMetablockSize(in EncoderParams p)
    {
        int bits = Math.Min(ComputeRbBits(p), BrotliConstants.MaxInputBlockBits);
        return (nuint)1 << bits;
    }

    /// <summary>
    /// When searching for backward references and have not seen matches for a long
    /// time, we can skip some match lookups. Unsuccessful match lookups are very
    /// expensive and this kind of a heuristic speeds up compression quite a lot.
    /// At first 8 byte strides are taken and every second byte is put to hasher.
    /// After 4x more literals stride by 16 bytes, every put 4-th byte to hasher.
    /// Applied only to qualities 2 to 9.
    /// </summary>
    public static nuint LiteralSpreeLengthForSparseSearch(in EncoderParams p) => p.Quality < 9 ? 64u : 512u;

    public static void ChooseHasher(in EncoderParams p, ref HasherParams hparams)
    {
        if (p.Quality > 9)
        {
            hparams.Type = 10;
        }
        else if (p.Quality == 4 && p.SizeHint >= (1 << 20))
        {
            hparams.Type = 54;
        }
        else if (p.Quality < 5)
        {
            hparams.Type = p.Quality;
        }
        else if (p.Lgwin <= 16)
        {
            hparams.Type = p.Quality < 7 ? 40 : p.Quality < 9 ? 41 : 42;
        }
        else if (p.SizeHint >= (1 << 20) && p.Lgwin >= 19)
        {
            hparams.Type = p.Quality <= MaxSimdQuality ? 68 : 6;
            hparams.BlockBits = p.Quality - 1;
            hparams.BucketBits = 15;
            hparams.NumLastDistancesToCheck = p.Quality < 7 ? 4 : p.Quality < 9 ? 10 : 16;
        }
        else
        {
            hparams.Type = p.Quality <= MaxSimdQuality ? 58 : 5;
            hparams.BlockBits = p.Quality - 1;
            hparams.BucketBits = p.Quality < 7 ? 14 : 15;
            hparams.NumLastDistancesToCheck = p.Quality < 7 ? 4 : p.Quality < 9 ? 10 : 16;
        }
        // Large window brotli (lgwin > 24) is not supported, so the composite hashers (H35, H55, H65) are never used.
    }
}
