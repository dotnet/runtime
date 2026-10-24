// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/utf8_util.c and utf8_util.h.

namespace System.IO.Compression;

/// <summary>Heuristics for deciding about the UTF8-ness of strings.</summary>
internal static class Utf8Util
{
    public const double MinUtf8Ratio = 0.75;

    private static nuint ParseAsUtf8(out int symbol, ReadOnlySpan<byte> input, nuint size)
    {
        // ASCII
        if ((input[0] & 0x80) == 0)
        {
            symbol = input[0];
            if (symbol > 0)
            {
                return 1;
            }
        }
        // 2-byte UTF8
        if (size > 1u &&
            (input[0] & 0xE0) == 0xC0 &&
            (input[1] & 0xC0) == 0x80)
        {
            symbol = ((input[0] & 0x1F) << 6) |
                     (input[1] & 0x3F);
            if (symbol > 0x7F)
            {
                return 2;
            }
        }
        // 3-byte UFT8
        if (size > 2u &&
            (input[0] & 0xF0) == 0xE0 &&
            (input[1] & 0xC0) == 0x80 &&
            (input[2] & 0xC0) == 0x80)
        {
            symbol = ((input[0] & 0x0F) << 12) |
                     ((input[1] & 0x3F) << 6) |
                     (input[2] & 0x3F);
            if (symbol > 0x7FF)
            {
                return 3;
            }
        }
        // 4-byte UFT8
        if (size > 3u &&
            (input[0] & 0xF8) == 0xF0 &&
            (input[1] & 0xC0) == 0x80 &&
            (input[2] & 0xC0) == 0x80 &&
            (input[3] & 0xC0) == 0x80)
        {
            symbol = ((input[0] & 0x07) << 18) |
                     ((input[1] & 0x3F) << 12) |
                     ((input[2] & 0x3F) << 6) |
                     (input[3] & 0x3F);
            if (symbol > 0xFFFF && symbol <= 0x10FFFF)
            {
                return 4;
            }
        }
        // Not UTF8, emit a special symbol above the UTF8-code space
        symbol = 0x110000 | input[0];
        return 1;
    }

    /// <summary>
    /// Returns true if at least min_fraction of the bytes between pos and
    /// pos + length in the (data, mask) ring-buffer is UTF8-encoded.
    /// Like in C, up to 3 bytes past the masked position may be read without masking (the ring buffer
    /// keeps a copy of its beginning after its end), so <paramref name="data"/> must cover that tail.
    /// </summary>
    public static bool IsMostlyUtf8(ReadOnlySpan<byte> data, nuint pos, nuint mask, nuint length, double minFraction)
    {
        nuint sizeUtf8 = 0;
        nuint i = 0;
        while (i < length)
        {
            nuint bytesRead = ParseAsUtf8(out int symbol, data.Slice((int)((pos + i) & mask)), length - i);
            i += bytesRead;
            if (symbol < 0x110000)
            {
                sizeUtf8 += bytesRead;
            }
        }
        return (double)sizeUtf8 > minFraction * (double)length;
    }
}
