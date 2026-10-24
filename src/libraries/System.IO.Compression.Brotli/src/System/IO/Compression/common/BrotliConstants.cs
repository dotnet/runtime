// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/common/constants.h.

namespace System.IO.Compression;

/// <summary>Constants of the Brotli format (RFC 7932).</summary>
internal static class BrotliConstants
{
    // Specification: 7.3. Encoding of the context map
    public const int ContextMapMaxRle = 16;

    // Specification: 2. Compressed representation overview
    public const int MaxNumberOfBlockTypes = 256;

    // Specification: 3.3. Alphabet sizes: insert-and-copy length
    public const int NumLiteralSymbols = 256;
    public const int NumCommandSymbols = 704;
    public const int NumBlockLenSymbols = 26;
    public const int MaxContextMapSymbols = MaxNumberOfBlockTypes + ContextMapMaxRle;
    public const int MaxBlockTypeSymbols = MaxNumberOfBlockTypes + 2;

    // Specification: 3.5. Complex prefix codes
    public const int RepeatPreviousCodeLength = 16;
    public const int RepeatZeroCodeLength = 17;
    public const int CodeLengthCodes = RepeatZeroCodeLength + 1;
    // "code length of 8 is repeated"
    public const int InitialRepeatedCodeLength = 8;

    // "Large Window Brotli" (not supported by this implementation, but needed to compute alphabet sizes).
    public const uint LargeMaxDistanceBits = 62;
    public const int LargeMinWBits = 10;
    public const int LargeMaxWBits = 30;

    // Specification: 4. Encoding of distances
    public const int NumDistanceShortCodes = 16;
    public const int MaxNPostfix = 3;
    public const int MaxNDirect = 120;
    public const uint MaxDistanceBits = 24;

    public static uint DistanceAlphabetSize(uint npostfix, uint ndirect, uint maxnbits) =>
        NumDistanceShortCodes + ndirect + (maxnbits << ((int)npostfix + 1));

    // BROTLI_DISTANCE_ALPHABET_SIZE(BROTLI_MAX_NDIRECT, BROTLI_MAX_NPOSTFIX, BROTLI_LARGE_MAX_DISTANCE_BITS)
    public const int NumDistanceSymbols = NumDistanceShortCodes + MaxNDirect + ((int)LargeMaxDistanceBits << (MaxNPostfix + 1));

    // Maximal distance expressible with NPOSTFIX = 0 and NDIRECT = 0 in a regular (non-large-window) stream.
    public const int MaxDistance = 0x3FFFFFC;

    // Maximal distance that is safe for 32-bit signed arithmetic.
    public const int MaxAllowedDistance = 0x7FFFFFFC;

    // 7.1. Context modes and context ID lookup for literals
    // "context IDs for literals are in the range of 0..63"
    public const int LiteralContextBits = 6;

    // 7.2. Context ID for distances
    public const int DistanceContextBits = 2;

    // 9.1. Format of the Stream Header
    // Number of slack bytes for window size. Don't confuse with BROTLI_NUM_DISTANCE_SHORT_CODES.
    public const int WindowGap = 16;

    public static nuint MaxBackwardLimit(int windowBits) => ((nuint)1 << windowBits) - WindowGap;

    public const int NumInsCopyCodes = 24;

    // Specification: 9.1 window bits limits
    public const int MinWindowBits = 10;
    public const int MaxWindowBits = 24;
    public const int MinInputBlockBits = 16;
    public const int MaxInputBlockBits = 24;
    public const int MinQuality = 0;
    public const int MaxQuality = 11;

    /// <summary>
    /// Calculates the alphabet size and maximal distance that can be expressed with the given
    /// distance parameters, such that decoders are safe with 32-bit arithmetic.
    /// </summary>
    public static (uint MaxAlphabetSize, uint MaxDistance) CalculateDistanceCodeLimit(uint maxDistance, uint npostfix, uint ndirect)
    {
        if (maxDistance <= ndirect)
        {
            // This case never happens / exists only for the sake of completeness.
            return (maxDistance + NumDistanceShortCodes, maxDistance);
        }

        // The first prohibited value.
        uint forbiddenDistance = maxDistance + 1;
        // Subtract "directly" encoded region.
        uint offset = forbiddenDistance - ndirect - 1;
        uint ndistbits = 0;
        uint tmp;
        uint half;
        uint group;
        // Postfix for the last dcode in the group.
        uint postfix = (1u << (int)npostfix) - 1;
        uint extra;
        uint start;
        // Remove postfix and "head-start".
        offset = (offset >> (int)npostfix) + 4;
        // Calculate the number of distance bits.
        tmp = offset / 2;
        // Poor-man's log2floor, to avoid extra dependencies.
        while (tmp != 0)
        {
            ndistbits++;
            tmp >>= 1;
        }
        // One bit is covered with subrange addressing ("half").
        ndistbits--;
        // Find subrange.
        half = (offset >> (int)ndistbits) & 1;
        // Calculate the "group" part of dcode.
        group = ((ndistbits - 1) << 1) | half;
        // Calculated "group" covers the prohibited distance value.
        if (group == 0)
        {
            // This case is added for correctness; does not occur for limit > 128.
            return (ndirect + NumDistanceShortCodes, ndirect);
        }
        // Decrement "group", so it is the last permitted "group".
        group--;
        // After group was decremented, ndistbits and half must be recalculated.
        ndistbits = (group >> 1) + 1;
        // The last available distance in the subrange has all extra bits set.
        extra = (1u << (int)ndistbits) - 1;
        // Calculate region start. NB: ndistbits >= 1.
        start = (1u << (int)(ndistbits + 1)) - 4;
        // Move to subregion.
        start += (group & 1) << (int)ndistbits;
        // Calculate the alphabet size.
        uint maxAlphabetSize = ((group << (int)npostfix) | postfix) + ndirect + NumDistanceShortCodes + 1;
        // Calculate the maximal distance representable by alphabet.
        uint maxDist = ((start + extra) << (int)npostfix) + postfix + ndirect + 1;
        return (maxAlphabetSize, maxDist);
    }
}
