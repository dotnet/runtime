// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/utf8.h>

#include <errno.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include <assert.h>
#include "minipalconfig.h"

#ifdef _MSC_VER
#include <intrin.h>
#endif

#define HIGH_SURROGATE_START 0xd800
#define LOW_SURROGATE_START 0xdc00
#define REPLACEMENT_CHAR 0xfffd

// Test if the wide character is a high surrogate
static bool IsHighSurrogate(CHAR16_T c)
{
    return (c & 0xFC00) == HIGH_SURROGATE_START;
}

// Test if the wide character is a low surrogate
static bool IsLowSurrogate(CHAR16_T c)
{
    return (c & 0xFC00) == LOW_SURROGATE_START;
}

// Test if the wide character is a surrogate half
static bool IsSurrogate(CHAR16_T c)
{
    return (c & 0xF800) == HIGH_SURROGATE_START;
}

static bool IsContinuationByte(unsigned char b)
{
    return (b & 0xC0) == 0x80;
}

// Well-formed sequences per the Unicode standard (Table 3-7)
static bool IsValidUtf8Sequence2(const unsigned char* p, size_t available)
{
    return available >= 2 && p[0] >= 0xC2 && p[0] <= 0xDF && IsContinuationByte(p[1]);
}

static bool IsValidUtf8Sequence3(const unsigned char* p, size_t available)
{
    return available >= 3 && (p[0] & 0xF0) == 0xE0 && IsContinuationByte(p[1]) && IsContinuationByte(p[2]) &&
        (p[0] != 0xE0 || p[1] >= 0xA0) && (p[0] != 0xED || p[1] <= 0x9F);
}

static bool IsValidUtf8Sequence4(const unsigned char* p, size_t available)
{
    return available >= 4 && p[0] >= 0xF0 && p[0] <= 0xF4 && IsContinuationByte(p[1]) && IsContinuationByte(p[2]) &&
        IsContinuationByte(p[3]) && (p[0] != 0xF0 || p[1] >= 0x90) && (p[0] != 0xF4 || p[1] <= 0x8F);
}

// Decodes the sequence at p, which has at least one byte, and returns how many bytes it takes. An ill-formed
// sequence decodes to -1 and takes the bytes that the replacement character stands for: an invalid lead byte on
// its own, a lead byte with an out of range second byte, or a lead byte with the continuation bytes that follow it
// before the input ends or a byte that isn't a continuation byte.
static size_t DecodeUtf8(const unsigned char* p, size_t available, int32_t* codePoint)
{
    unsigned char lead = p[0];
    unsigned char low = 0x80;
    unsigned char high = 0xBF;
    size_t length;
    int32_t value;

    if (lead < 0x80)
    {
        *codePoint = lead;
        return 1;
    }

    if (lead >= 0xC2 && lead <= 0xDF)
    {
        length = 2;
        value = lead & 0x1F;
    }
    else if (lead >= 0xE0 && lead <= 0xEF)
    {
        length = 3;
        value = lead & 0x0F;
        if (lead == 0xE0)
            low = 0xA0;
        else if (lead == 0xED)
            high = 0x9F;
    }
    else if (lead >= 0xF0 && lead <= 0xF4)
    {
        length = 4;
        value = lead & 0x07;
        if (lead == 0xF0)
            low = 0x90;
        else if (lead == 0xF4)
            high = 0x8F;
    }
    else
    {
        *codePoint = -1;
        return 1;
    }

    for (size_t i = 1; i < length; i++)
    {
        if (i >= available || !IsContinuationByte(p[i]))
        {
            *codePoint = -1;
            return i;
        }

        if (i == 1 && (p[1] < low || p[1] > high))
        {
            *codePoint = -1;
            return 2;
        }

        value = (value << 6) | (p[i] & 0x3F);
    }

    *codePoint = value;
    return length;
}

static CHAR16_T SwapBytesIf(CHAR16_T c, bool swap)
{
    return swap ? (CHAR16_T)((c << 8) | (c >> 8)) : c;
}

// Decodes the character at p, which has at least one code unit, and returns how many code units it takes.
// A lone surrogate decodes to the replacement character.
static size_t DecodeUtf16(const CHAR16_T* p, size_t available, bool swap, uint32_t* codePoint)
{
    CHAR16_T ch = SwapBytesIf(p[0], swap);
    if (IsHighSurrogate(ch) && available >= 2)
    {
        CHAR16_T low = SwapBytesIf(p[1], swap);
        if (IsLowSurrogate(low))
        {
            *codePoint = 0x10000 + ((((uint32_t)ch - HIGH_SURROGATE_START) << 10) | ((uint32_t)low - LOW_SURROGATE_START));
            return 2;
        }
    }

    *codePoint = IsSurrogate(ch) ? REPLACEMENT_CHAR : ch;
    return 1;
}

static size_t Utf8Length(uint32_t codePoint)
{
    return codePoint < 0x80 ? 1 : codePoint < 0x800 ? 2 : codePoint < 0x10000 ? 3 : 4;
}

static void EncodeUtf8(uint32_t codePoint, unsigned char* p)
{
    if (codePoint < 0x80)
    {
        p[0] = (unsigned char)codePoint;
    }
    else if (codePoint < 0x800)
    {
        p[0] = (unsigned char)(0xC0 | (codePoint >> 6));
        p[1] = (unsigned char)(0x80 | (codePoint & 0x3F));
    }
    else if (codePoint < 0x10000)
    {
        p[0] = (unsigned char)(0xE0 | (codePoint >> 12));
        p[1] = (unsigned char)(0x80 | ((codePoint >> 6) & 0x3F));
        p[2] = (unsigned char)(0x80 | (codePoint & 0x3F));
    }
    else
    {
        p[0] = (unsigned char)(0xF0 | (codePoint >> 18));
        p[1] = (unsigned char)(0x80 | ((codePoint >> 12) & 0x3F));
        p[2] = (unsigned char)(0x80 | ((codePoint >> 6) & 0x3F));
        p[3] = (unsigned char)(0x80 | (codePoint & 0x3F));
    }
}

// Fast paths for well-formed input. Each one stops at the first character it doesn't handle (an ill-formed
// sequence, a lone surrogate, or a character that doesn't fit in the destination), which the caller deals with
// before calling it again.

#define ASCII_MASK_UTF8 UINT64_C(0x8080808080808080)
#define ASCII_MASK_UTF16 UINT64_C(0xFF80FF80FF80FF80)

// Index, counting from the lowest address, of the first byte of the word that has any of the given bits set
static size_t FirstByteWithBitsSet(uint64_t bits)
{
    assert(bits != 0);
#if BIGENDIAN
    return (size_t)__builtin_clzll(bits) / 8;
#elif defined(_MSC_VER) && (defined(_M_X64) || defined(_M_ARM64))
    unsigned long index;
    _BitScanForward64(&index, bits);
    return index / 8;
#elif defined(_MSC_VER)
    unsigned long index;
    if (_BitScanForward(&index, (unsigned long)bits))
        return index / 8;
    _BitScanForward(&index, (unsigned long)(bits >> 32));
    return 4 + index / 8;
#else
    return (size_t)__builtin_ctzll(bits) / 8;
#endif
}

static size_t CountValidUtf8(const unsigned char** source, const unsigned char* sourceEnd)
{
    const unsigned char* pSrc = *source;
    size_t count = 0;

    while (pSrc < sourceEnd)
    {
        unsigned char ch = *pSrc;
        size_t available = (size_t)(sourceEnd - pSrc);

        if (ch < 0x80)
        {
            // ASCII runs, along with the 2 byte sequences that often separate them in mostly-ASCII text
            const unsigned char* start = pSrc++;
            size_t sequences2 = 0;
            for (;;)
            {
                // 16 bytes per iteration, but checking the first 8 on their own, since ASCII runs in
                // mostly-ASCII text are often shorter than 16 bytes
                while (sourceEnd - pSrc >= 16)
                {
                    uint64_t word;
                    memcpy(&word, pSrc, sizeof(word));
                    if (word & ASCII_MASK_UTF8)
                        break;
                    memcpy(&word, pSrc + 8, sizeof(word));
                    pSrc += 8;
                    if (word & ASCII_MASK_UTF8)
                        break;
                    pSrc += 8;
                }

                if (sourceEnd - pSrc >= 8)
                {
                    uint64_t word;
                    memcpy(&word, pSrc, sizeof(word));
                    if ((word & ASCII_MASK_UTF8) == 0)
                        pSrc += 8;
                }

                while (pSrc < sourceEnd && *pSrc < 0x80)
                    pSrc++;

                if (!IsValidUtf8Sequence2(pSrc, (size_t)(sourceEnd - pSrc)))
                    break;
                pSrc += 2;
                sequences2++;
            }

            count += (size_t)(pSrc - start) - sequences2;
        }
        else if (IsValidUtf8Sequence2(pSrc, available))
        {
            // stay here for the rest of a run of 2 byte sequences
            do
            {
                pSrc += 2;
                count++;
            }
            while (IsValidUtf8Sequence2(pSrc, (size_t)(sourceEnd - pSrc)));
        }
        else if (IsValidUtf8Sequence3(pSrc, available))
        {
            // stay here for the rest of a run of 3 byte sequences
            do
            {
                pSrc += 3;
                count++;
            }
            while (IsValidUtf8Sequence3(pSrc, (size_t)(sourceEnd - pSrc)));
        }
        else if (IsValidUtf8Sequence4(pSrc, available))
        {
            // stay here for the rest of a run of 4 byte sequences
            do
            {
                pSrc += 4;
                count += 2;
            }
            while (IsValidUtf8Sequence4(pSrc, (size_t)(sourceEnd - pSrc)));
        }
        else
        {
            break;
        }
    }

    *source = pSrc;
    return count;
}

static void ConvertValidUtf8(const unsigned char** source, const unsigned char* sourceEnd, CHAR16_T** destination, CHAR16_T* destinationEnd)
{
    const unsigned char* pSrc = *source;
    CHAR16_T* pDst = *destination;

    while (pSrc < sourceEnd && pDst < destinationEnd)
    {
        unsigned char ch = *pSrc;
        size_t available = (size_t)(sourceEnd - pSrc);

        if (ch < 0x80)
        {
            *pDst++ = ch;
            pSrc++;

            // ASCII runs, along with the 2 byte sequences that often separate them in mostly-ASCII text
            for (;;)
            {
                size_t words = (size_t)(sourceEnd - pSrc);
                if ((size_t)(destinationEnd - pDst) < words)
                    words = (size_t)(destinationEnd - pDst);
                for (words /= 8; words != 0; words--)
                {
                    uint64_t word;
                    memcpy(&word, pSrc, sizeof(word));
                    uint64_t nonAscii = word & ASCII_MASK_UTF8;
                    if (nonAscii == 0)
                    {
                        for (int i = 0; i < 8; i++)
                            pDst[i] = pSrc[i];
                        pSrc += 8;
                        pDst += 8;
                        continue;
                    }

                    size_t ascii = FirstByteWithBitsSet(nonAscii);
                    for (size_t i = 0; i < ascii; i++)
                        pDst[i] = pSrc[i];
                    pSrc += ascii;
                    pDst += ascii;
                    break;
                }

                while (pSrc < sourceEnd && pDst < destinationEnd && *pSrc < 0x80)
                    *pDst++ = *pSrc++;

                if (pDst >= destinationEnd || !IsValidUtf8Sequence2(pSrc, (size_t)(sourceEnd - pSrc)))
                    break;
                *pDst++ = (CHAR16_T)(((pSrc[0] & 0x1F) << 6) | (pSrc[1] & 0x3F));
                pSrc += 2;
            }
        }
        else if (IsValidUtf8Sequence2(pSrc, available))
        {
            // stay here for the rest of a run of 2 byte sequences
            do
            {
                *pDst++ = (CHAR16_T)(((pSrc[0] & 0x1F) << 6) | (pSrc[1] & 0x3F));
                pSrc += 2;
            }
            while (pDst < destinationEnd && IsValidUtf8Sequence2(pSrc, (size_t)(sourceEnd - pSrc)));
        }
        else if (IsValidUtf8Sequence3(pSrc, available))
        {
            // stay here for the rest of a run of 3 byte sequences
            do
            {
                *pDst++ = (CHAR16_T)(((pSrc[0] & 0x0F) << 12) | ((pSrc[1] & 0x3F) << 6) | (pSrc[2] & 0x3F));
                pSrc += 3;
            }
            while (pDst < destinationEnd && IsValidUtf8Sequence3(pSrc, (size_t)(sourceEnd - pSrc)));
        }
        else if (IsValidUtf8Sequence4(pSrc, available) && destinationEnd - pDst >= 2)
        {
            // stay here for the rest of a run of 4 byte sequences
            do
            {
                uint32_t codePoint = (((uint32_t)pSrc[0] & 0x07) << 18) | (((uint32_t)pSrc[1] & 0x3F) << 12) |
                    (((uint32_t)pSrc[2] & 0x3F) << 6) | ((uint32_t)pSrc[3] & 0x3F);
                pDst[0] = (CHAR16_T)(HIGH_SURROGATE_START + ((codePoint - 0x10000) >> 10));
                pDst[1] = (CHAR16_T)(LOW_SURROGATE_START + (codePoint & 0x3FF));
                pDst += 2;
                pSrc += 4;
            }
            while (destinationEnd - pDst >= 2 && IsValidUtf8Sequence4(pSrc, (size_t)(sourceEnd - pSrc)));
        }
        else
        {
            break;
        }
    }

    *source = pSrc;
    *destination = pDst;
}

#define LANES16_ONE UINT64_C(0x0001000100010001)
#define LANES16_BELOW_0800_MASK UINT64_C(0xF800F800F800F800)
#define LANES16_SURROGATE UINT64_C(0xD800D800D800D800)

// Sets bit 0 of every nonzero 16-bit lane and clears the rest
static uint64_t NonZeroLanes16(uint64_t lanes)
{
    return ((((lanes & UINT64_C(0x7FFF7FFF7FFF7FFF)) + UINT64_C(0x7FFF7FFF7FFF7FFF)) | lanes) >> 15) & LANES16_ONE;
}

// Adds up the 16-bit lanes, each of which must be small enough that the sum fits in 16 bits
static size_t SumLanes16(uint64_t lanes)
{
    return (size_t)((lanes * LANES16_ONE) >> 48);
}

static size_t CountValidUtf16(const CHAR16_T** source, const CHAR16_T* sourceEnd)
{
    const CHAR16_T* pSrc = *source;
    size_t count = 0;

    while (pSrc < sourceEnd)
    {
        const CHAR16_T* start = pSrc;
        while (sourceEnd - pSrc >= 8)
        {
            uint64_t first;
            uint64_t second;
            memcpy(&first, pSrc, sizeof(first));
            memcpy(&second, pSrc + 4, sizeof(second));
            if ((first | second) & ASCII_MASK_UTF16)
                break;
            pSrc += 8;
        }
        count += (size_t)(pSrc - start);

        // 8 characters at a time while they are all below U+0800: one byte each, plus one more from U+0080.
        // Adding 0x7F80 to a lane below U+0800 sets its bit 15 exactly when the lane is from U+0080. The
        // per-lane counts are added up at least every 4096 blocks, so that their sum fits in 16 bits.
        while (sourceEnd - pSrc >= 8)
        {
            size_t blocks = (size_t)(sourceEnd - pSrc) / 8;
            if (blocks > 4096)
                blocks = 4096;

            start = pSrc;
            uint64_t extra = 0;
            do
            {
                uint64_t first;
                uint64_t second;
                memcpy(&first, pSrc, sizeof(first));
                memcpy(&second, pSrc + 4, sizeof(second));
                if ((first | second) & LANES16_BELOW_0800_MASK)
                    break;
                extra += (((first + UINT64_C(0x7F807F807F807F80)) >> 15) & LANES16_ONE) +
                    (((second + UINT64_C(0x7F807F807F807F80)) >> 15) & LANES16_ONE);
                pSrc += 8;
            }
            while (--blocks != 0);

            count += (size_t)(pSrc - start) + SumLanes16(extra);
            if (blocks != 0)
                break;
        }

        // 4 characters at a time while none of them is a surrogate: one byte each, plus one more from
        // U+0080 and another from U+0800
        while (sourceEnd - pSrc >= 4)
        {
            uint64_t word;
            memcpy(&word, pSrc, sizeof(word));
            if (NonZeroLanes16((word & LANES16_BELOW_0800_MASK) ^ LANES16_SURROGATE) != LANES16_ONE)
                break;
            count += 4 + SumLanes16(NonZeroLanes16(word & ASCII_MASK_UTF16) + NonZeroLanes16(word & LANES16_BELOW_0800_MASK));
            pSrc += 4;
        }

        if (pSrc >= sourceEnd)
            break;

        CHAR16_T ch = *pSrc;

        if (ch < 0x80)
        {
            pSrc++;
            count++;
        }
        else if (ch < 0x800)
        {
            pSrc++;
            count += 2;
        }
        else if (!IsSurrogate(ch))
        {
            pSrc++;
            count += 3;
        }
        else if (IsHighSurrogate(ch) && sourceEnd - pSrc >= 2 && IsLowSurrogate(pSrc[1]))
        {
            // stay here for the rest of a run of surrogate pairs
            do
            {
                pSrc += 2;
                count += 4;
            }
            while (sourceEnd - pSrc >= 2 && IsHighSurrogate(pSrc[0]) && IsLowSurrogate(pSrc[1]));
        }
        else
        {
            break;
        }
    }

    *source = pSrc;
    return count;
}

static void ConvertValidUtf16(const CHAR16_T** source, const CHAR16_T* sourceEnd, unsigned char** destination, unsigned char* destinationEnd)
{
    const CHAR16_T* pSrc = *source;
    unsigned char* pDst = *destination;

    while (pSrc < sourceEnd && pDst < destinationEnd)
    {
        CHAR16_T ch = *pSrc;
        ptrdiff_t room = destinationEnd - pDst;

        if (ch < 0x80)
        {
            *pDst++ = (unsigned char)ch;
            pSrc++;

            // ASCII runs, along with the 2 byte characters that often separate them in mostly-ASCII text
            for (;;)
            {
                size_t words = (size_t)(sourceEnd - pSrc);
                if ((size_t)(destinationEnd - pDst) < words)
                    words = (size_t)(destinationEnd - pDst);
                for (words /= 4; words != 0; words--)
                {
                    uint64_t word;
                    memcpy(&word, pSrc, sizeof(word));
                    if (word & ASCII_MASK_UTF16)
                        break;
                    pDst[0] = (unsigned char)pSrc[0];
                    pDst[1] = (unsigned char)pSrc[1];
                    pDst[2] = (unsigned char)pSrc[2];
                    pDst[3] = (unsigned char)pSrc[3];
                    pSrc += 4;
                    pDst += 4;
                }

                while (pSrc < sourceEnd && pDst < destinationEnd && *pSrc < 0x80)
                    *pDst++ = (unsigned char)*pSrc++;

                if (pSrc >= sourceEnd || destinationEnd - pDst < 2 || (ch = *pSrc) < 0x80 || ch >= 0x800)
                    break;
                pDst[0] = (unsigned char)(0xC0 | (ch >> 6));
                pDst[1] = (unsigned char)(0x80 | (ch & 0x3F));
                pDst += 2;
                pSrc++;
            }
        }
        else if (ch < 0x800)
        {
            if (room < 2)
                break;

            // stay here for the rest of a run of 2 byte characters
            do
            {
                pDst[0] = (unsigned char)(0xC0 | (ch >> 6));
                pDst[1] = (unsigned char)(0x80 | (ch & 0x3F));
                pDst += 2;
                pSrc++;
            }
            while (pSrc < sourceEnd && destinationEnd - pDst >= 2 && (ch = *pSrc) >= 0x80 && ch < 0x800);
        }
        else if (!IsSurrogate(ch))
        {
            if (room < 3)
                break;

            // stay here for the rest of a run of 3 byte characters
            do
            {
                pDst[0] = (unsigned char)(0xE0 | (ch >> 12));
                pDst[1] = (unsigned char)(0x80 | ((ch >> 6) & 0x3F));
                pDst[2] = (unsigned char)(0x80 | (ch & 0x3F));
                pDst += 3;
                pSrc++;
            }
            while (pSrc < sourceEnd && destinationEnd - pDst >= 3 && (ch = *pSrc) >= 0x800 && !IsSurrogate(ch));
        }
        else if (IsHighSurrogate(ch) && sourceEnd - pSrc >= 2 && IsLowSurrogate(pSrc[1]) && room >= 4)
        {
            // stay here for the rest of a run of surrogate pairs
            do
            {
                uint32_t codePoint = 0x10000 + ((((uint32_t)pSrc[0] - HIGH_SURROGATE_START) << 10) | ((uint32_t)pSrc[1] - LOW_SURROGATE_START));
                pDst[0] = (unsigned char)(0xF0 | (codePoint >> 18));
                pDst[1] = (unsigned char)(0x80 | ((codePoint >> 12) & 0x3F));
                pDst[2] = (unsigned char)(0x80 | ((codePoint >> 6) & 0x3F));
                pDst[3] = (unsigned char)(0x80 | (codePoint & 0x3F));
                pDst += 4;
                pSrc += 2;
            }
            while (sourceEnd - pSrc >= 2 && destinationEnd - pDst >= 4 && IsHighSurrogate(pSrc[0]) && IsLowSurrogate(pSrc[1]));
        }
        else
        {
            break;
        }
    }

    *source = pSrc;
    *destination = pDst;
}

size_t minipal_get_length_utf8_to_utf16(const char* source, size_t sourceLength, unsigned int flags)
{
    const unsigned char* pSrc = (const unsigned char*)source;
    const unsigned char* pEnd = pSrc + sourceLength;
    size_t count = 0;

    errno = 0;

    for (;;)
    {
        count += CountValidUtf8(&pSrc, pEnd);
        if (pSrc == pEnd)
            return count;

        // CountValidUtf8 only stops at ill-formed sequences
        int32_t codePoint;
        pSrc += DecodeUtf8(pSrc, (size_t)(pEnd - pSrc), &codePoint);

        if (flags & MINIPAL_MB_NO_REPLACE_INVALID_CHARS)
        {
            errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
            return 0;
        }

        count++;
    }
}

size_t minipal_get_length_utf16_to_utf8(const CHAR16_T* source, size_t sourceLength, unsigned int flags)
{
    const CHAR16_T* pSrc = source;
    const CHAR16_T* pEnd = source + sourceLength;
    size_t count = 0;
#if BIGENDIAN
    bool swap = (flags & MINIPAL_TREAT_AS_LITTLE_ENDIAN) != 0;
#else
    bool swap = false;
    (void)flags; // unused
#endif

    errno = 0;

    while (pSrc < pEnd)
    {
        if (!swap)
        {
            count += CountValidUtf16(&pSrc, pEnd);
            if (pSrc == pEnd)
                break;
        }

        // a lone surrogate, which becomes the replacement character, or any character when swapping bytes
        uint32_t codePoint;
        pSrc += DecodeUtf16(pSrc, (size_t)(pEnd - pSrc), swap, &codePoint);
        count += Utf8Length(codePoint);
    }

    return count;
}

size_t minipal_convert_utf8_to_utf16(const char* source, size_t sourceLength, CHAR16_T* destination, size_t destinationLength, unsigned int flags)
{
    const unsigned char* pSrc = (const unsigned char*)source;
    const unsigned char* pEnd = pSrc + sourceLength;
    CHAR16_T* pDst = destination;
    CHAR16_T* pDstEnd = destination + destinationLength;
#if BIGENDIAN
    bool swap = (flags & MINIPAL_TREAT_AS_LITTLE_ENDIAN) != 0;
#else
    bool swap = false;
#endif

    errno = 0;

    while (pSrc < pEnd)
    {
        if (!swap)
        {
            ConvertValidUtf8(&pSrc, pEnd, &pDst, pDstEnd);
            if (pSrc == pEnd)
                break;
        }

        // an ill-formed sequence, a character that doesn't fit, or any character when swapping bytes
        int32_t codePoint;
        size_t length = DecodeUtf8(pSrc, (size_t)(pEnd - pSrc), &codePoint);
        if (codePoint < 0)
        {
            if (flags & MINIPAL_MB_NO_REPLACE_INVALID_CHARS)
            {
                errno = MINIPAL_ERROR_NO_UNICODE_TRANSLATION;
                return 0;
            }
            codePoint = REPLACEMENT_CHAR;
        }

        if (pDstEnd - pDst < (codePoint >= 0x10000 ? 2 : 1))
        {
            errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
            return 0;
        }

        if (codePoint >= 0x10000)
        {
            *pDst++ = SwapBytesIf((CHAR16_T)(HIGH_SURROGATE_START + ((codePoint - 0x10000) >> 10)), swap);
            *pDst++ = SwapBytesIf((CHAR16_T)(LOW_SURROGATE_START + (codePoint & 0x3FF)), swap);
        }
        else
        {
            *pDst++ = SwapBytesIf((CHAR16_T)codePoint, swap);
        }

        pSrc += length;
    }

    return (size_t)(pDst - destination);
}

size_t minipal_convert_utf16_to_utf8(const CHAR16_T* source, size_t sourceLength, char* destination, size_t destinationLength, unsigned int flags)
{
    const CHAR16_T* pSrc = source;
    const CHAR16_T* pEnd = source + sourceLength;
    unsigned char* pDst = (unsigned char*)destination;
    unsigned char* pDstEnd = pDst + destinationLength;
#if BIGENDIAN
    bool swap = (flags & MINIPAL_TREAT_AS_LITTLE_ENDIAN) != 0;
#else
    bool swap = false;
    (void)flags; // unused
#endif

    errno = 0;

    while (pSrc < pEnd)
    {
        if (!swap)
        {
            ConvertValidUtf16(&pSrc, pEnd, &pDst, pDstEnd);
            if (pSrc == pEnd)
                break;
        }

        // a lone surrogate, which becomes the replacement character, a character that doesn't fit, or any
        // character when swapping bytes
        uint32_t codePoint;
        size_t units = DecodeUtf16(pSrc, (size_t)(pEnd - pSrc), swap, &codePoint);
        size_t length = Utf8Length(codePoint);
        if ((size_t)(pDstEnd - pDst) < length)
        {
            errno = MINIPAL_ERROR_INSUFFICIENT_BUFFER;
            return 0;
        }

        EncodeUtf8(codePoint, pDst);
        pDst += length;
        pSrc += units;
    }

    return (size_t)(pDst - (unsigned char*)destination);
}
