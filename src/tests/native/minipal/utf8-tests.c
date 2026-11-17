// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "test.h"

#include <errno.h>
#include <stdint.h>
#include <minipal/utf8.h>

#define W(str) ((const CHAR16_T*)u##str)

#define CHECK(call) do { RESULT check_result = (call); if (check_result != OK) return check_result; } while (0)

// A value the conversions never set, to see that they clear errno when they succeed
#define ERRNO_SENTINEL 12345

#define REPLACEMENT_CHAR 0xFFFD

static const CHAR16_T Utf16Canary = 0xBEEF;
static const char Utf8Canary = '\xA5';

typedef struct
{
    const char* utf8;
    const CHAR16_T* utf16;
} StringPair;

typedef struct
{
    const char* utf8;
    size_t utf8Length;
    const CHAR16_T* utf16;
    size_t utf16Length;
} CountedStringPair;

// Strings that convert both ways
static const StringPair valid_strings[] =
{
    // Empty string
    { "", W("") },
    // 1, 2 and 3 byte encoded 1 character long strings
    { "A", W("A") },
    { "\xC2\x80", W("\x0080") },
    { "\xE0\xA0\x80", W("\x0800") },
    // 1 byte encoded characters only
    { "ABCDEFGHIJKLMNOPQRSTUVWXYZ", W("ABCDEFGHIJKLMNOPQRSTUVWXYZ") },
    // 2 byte encoded characters only
    { "\xC2\x80\xC3\xBF\xC7\x81\xDF\xBF", W("\x0080\x00FF\x01C1\x07FF") },
    // 3 byte encoded characters only
    { "\xE0\xA0\x80\xE1\xB6\x88\xE1\x80\x80\xEF\xBF\xBF", W("\x0800\x1D88\x1000\xFFFF") },
    // 1 and 2 byte encoded characters interleaved 1:1, starting and ending with either
    { "\x41\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF\x45", W("\x0041\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF\x0045") },
    { "\x41\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF", W("\x0041\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF") },
    { "\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF\x45", W("\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF\x0045") },
    { "\xC2\x80\x42\xC3\xBF\x43\xC7\x81\x44\xDF\xBF", W("\x0080\x0042\x00FF\x0043\x01C1\x0044\x07FF") },
    // 1 and 2 byte encoded characters interleaved 2:2, starting and ending with either
    { "\x41\x42\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF\x45\x46", W("\x0041\x0042\x0080\x00FF\x0043\x0044\x01C1\x07FF\x0045\x0046") },
    { "\x41\x42\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF", W("\x0041\x0042\x0080\x00FF\x0043\x0044\x01C1\x07FF") },
    { "\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF\x45\x46", W("\x0080\x00FF\x0043\x0044\x01C1\x07FF\x0045\x0046") },
    { "\xC2\x80\xC3\xBF\x43\x44\xC7\x81\xDF\xBF", W("\x0080\x00FF\x0043\x0044\x01C1\x07FF") },
    // 4 byte encoded characters, which are surrogate pairs in UTF-16
    { "\xF0\x90\x80\x80\xF0\x90\x89\x80\xF3\x80\x8E\xB0\xF4\x8F\xBF\xBF", W("\xD800\xDC00\xD800\xDE40\xDAC0\xDFB0\xDBFF\xDFFF") },
};

// Ill-formed UTF-8 and the UTF-16 it converts to
static const StringPair invalid_utf8_strings[] =
{
    // Incomplete 2, 3 and 4 byte encoded characters standalone
    { "\xC2", W("\xFFFD") },
    { "\xE0\xA0", W("\xFFFD") },
    { "\xE0", W("\xFFFD") },
    { "\xF0\x90\x80", W("\xFFFD") },
    { "\xF0\x90", W("\xFFFD") },
    { "\xF0", W("\xFFFD") },
    // Continuation byte with no lead byte standalone
    { "\x80", W("\xFFFD") },
    // The same between, before and after 1 byte encoded characters
    { "\x41\xC2\x42", W("\x0041\xFFFD\x0042") },
    { "\x41\xE0\xA0\x42", W("\x0041\xFFFD\x0042") },
    { "\x41\xE0\x42", W("\x0041\xFFFD\x0042") },
    { "\x41\x80\x42", W("\x0041\xFFFD\x0042") },
    { "\xC2\x42", W("\xFFFD\x0042") },
    { "\xE0\xA0\x42", W("\xFFFD\x0042") },
    { "\xE0\x42", W("\xFFFD\x0042") },
    { "\x80\x42", W("\xFFFD\x0042") },
    { "\x41\xC2", W("\x0041\xFFFD") },
    { "\x41\xE0\xA0", W("\x0041\xFFFD") },
    { "\x41\xE0", W("\x0041\xFFFD") },
    { "\x41\x80", W("\x0041\xFFFD") },
    // ... and between 2 byte encoded characters
    { "\xC2\x80\xC2\xC3\xBF", W("\x0080\xFFFD\x00FF") },
    { "\xC2\x80\xE0\xA0\xC3\xBF", W("\x0080\xFFFD\x00FF") },
    { "\xC2\x80\xE0\xC3\xBF", W("\x0080\xFFFD\x00FF") },
    { "\xC2\x80\x80\xC3\xBF", W("\x0080\xFFFD\x00FF") },
    // Non-shortest form encodings
    { "\xC0\x80", W("\xFFFD\xFFFD") },
    { "\xE0\x80\x80", W("\xFFFD\xFFFD") },
    { "\xF0\x80\x80\x80", W("\xFFFD\xFFFD\xFFFD") },
    // ... of the largest character of each shorter form
    { "\xC1\xBF", W("\xFFFD\xFFFD") },
    { "\xE0\x9F\xBF", W("\xFFFD\xFFFD") },
    { "\xF0\x8F\xBF\xBF", W("\xFFFD\xFFFD\xFFFD") },
    // Encoded high and low surrogates, a pair of them (CESU-8), and one between 1 byte encoded characters
    { "\xED\xA0\x80", W("\xFFFD\xFFFD") },
    { "\xED\xBF\xBF", W("\xFFFD\xFFFD") },
    { "\xED\xA0\x80\xED\xB0\x80", W("\xFFFD\xFFFD\xFFFD\xFFFD") },
    { "\x41\xED\xBF\xBF\x42", W("\x0041\xFFFD\xFFFD\x0042") },
    // 4 byte encoded character above U+10FFFF, and lead bytes that can only encode such characters
    { "\xF4\x90\x80\x80", W("\xFFFD\xFFFD\xFFFD") },
    { "\xF5\x80\x80\x80", W("\xFFFD\xFFFD\xFFFD\xFFFD") },
    { "\xF7\xBF\xBF\xBF", W("\xFFFD\xFFFD\xFFFD\xFFFD") },
    // 5 and 6 byte encodings
    { "\xF8\x88\x80\x80\x80", W("\xFFFD\xFFFD\xFFFD\xFFFD\xFFFD") },
    { "\xFC\x84\x80\x80\x80\x80", W("\xFFFD\xFFFD\xFFFD\xFFFD\xFFFD\xFFFD") },
    // Bytes that never appear in UTF-8
    { "\xFE", W("\xFFFD") },
    { "\xFF", W("\xFFFD") },
    { "\x41\xFE\x42\xFF\x43", W("\x0041\xFFFD\x0042\xFFFD\x0043") },
};

// Ill-formed UTF-16 and the UTF-8 it converts to
static const StringPair invalid_utf16_strings[] =
{
    // Lone high and low surrogates standalone, after a character and between two characters
    { "\xEF\xBF\xBD", W("\xD800") },
    { "\xEF\xBF\xBD", W("\xDC00") },
    { "\x41\xEF\xBF\xBD", W("\x0041\xD800") },
    { "\x41\xEF\xBF\xBD", W("\x0041\xDC00") },
    { "\x41\xEF\xBF\xBD\x42", W("\x0041\xD800\x0042") },
    { "\x41\xEF\xBF\xBD\x42", W("\x0041\xDC00\x0042") },
    // Low surrogate followed by high surrogate, standalone and between two characters
    { "\xEF\xBF\xBD\xEF\xBF\xBD", W("\xDC00\xD800") },
    { "\x41\xEF\xBF\xBD\xEF\xBF\xBD\x42", W("\x0041\xDC00\xD800\x0042") },
    // High surrogate followed by a surrogate pair, and a surrogate pair followed by a low surrogate
    { "\xEF\xBF\xBD\xF0\x90\x80\x80", W("\xD800\xD800\xDC00") },
    { "\xF0\x90\x80\x80\xEF\xBF\xBD", W("\xD800\xDC00\xDC00") },
};

// Explicit lengths that include NULs, which convert like any other character
static const CountedStringPair utf8_strings_with_nuls[] =
{
    { "\0", 1, W("\0"), 1 },
    { "\0\0", 2, W("\0\0"), 2 },
    { "a\0b", 3, W("a\0b"), 3 },
    // Incomplete 2 byte encoded character before a NUL, and a continuation byte after it
    { "\xC2\0\x80", 3, W("\xFFFD\0\xFFFD"), 3 },
    // Incomplete 2 byte encoded character at the end, after a NUL
    { "a\0\xC2", 3, W("a\0\xFFFD"), 3 },
    // 4 byte encoded character interrupted by a NUL, and one after a NUL
    { "\xF0\x90\0\x80\x80", 5, W("\xFFFD\0\xFFFD\xFFFD"), 4 },
    { "A\0\xF0\x90\x80\x80", 6, W("A\0\xD800\xDC00"), 4 },
};

static const CountedStringPair utf16_strings_with_nuls[] =
{
    { "\0", 1, W("\0"), 1 },
    { "a\0b", 3, W("a\0b"), 3 },
    // Surrogate pair interrupted by a NUL, and one after a NUL
    { "\xEF\xBF\xBD\0\xEF\xBF\xBD", 7, W("\xD800\0\xDC00"), 3 },
    { "\0\xF0\x9F\x98\x80", 5, W("\0\xD83D\xDE00"), 3 },
    // High surrogate before a NUL at the end
    { "\xEF\xBF\xBD\0", 4, W("\xD800\0"), 2 },
};

static bool is_big_endian(void)
{
    const uint16_t one = 1;
    return *(const uint8_t*)&one == 0;
}

static CHAR16_T swap_bytes(CHAR16_T c)
{
    return (CHAR16_T)((c << 8) | (c >> 8));
}

static size_t utf16_length(const CHAR16_T* s)
{
    size_t length = 0;
    while (s[length] != 0)
        length++;
    return length;
}

static bool has_replacement_char(const CHAR16_T* s, size_t length)
{
    for (size_t i = 0; i < length; i++)
    {
        if (s[i] == REPLACEMENT_CHAR)
            return true;
    }
    return false;
}

static void fill_utf16(CHAR16_T* buffer, size_t length)
{
    for (size_t i = 0; i < length; i++)
        buffer[i] = Utf16Canary;
}

static bool is_high_surrogate(uint32_t c)
{
    return c >= 0xD800 && c <= 0xDBFF;
}

static bool is_low_surrogate(uint32_t c)
{
    return c >= 0xDC00 && c <= 0xDFFF;
}

// A character at a time model of the conversions, which the optimized paths must agree with

static size_t reference_encode_utf8(uint32_t codePoint, unsigned char* p)
{
    if (codePoint < 0x80)
    {
        p[0] = (unsigned char)codePoint;
        return 1;
    }
    if (codePoint < 0x800)
    {
        p[0] = (unsigned char)(0xC0 | (codePoint >> 6));
        p[1] = (unsigned char)(0x80 | (codePoint & 0x3F));
        return 2;
    }
    if (codePoint < 0x10000)
    {
        p[0] = (unsigned char)(0xE0 | (codePoint >> 12));
        p[1] = (unsigned char)(0x80 | ((codePoint >> 6) & 0x3F));
        p[2] = (unsigned char)(0x80 | (codePoint & 0x3F));
        return 3;
    }
    p[0] = (unsigned char)(0xF0 | (codePoint >> 18));
    p[1] = (unsigned char)(0x80 | ((codePoint >> 12) & 0x3F));
    p[2] = (unsigned char)(0x80 | ((codePoint >> 6) & 0x3F));
    p[3] = (unsigned char)(0x80 | (codePoint & 0x3F));
    return 4;
}

// Decodes one sequence and returns its length in bytes. An ill-formed sequence decodes to -1, and is an invalid lead
// byte, a lead byte with an out of range second byte, or a lead byte with the continuation bytes that follow it before
// the input ends or a byte that isn't a continuation byte. The ranges are the well-formed sequences of the Unicode
// standard (Table 3-7).
static size_t reference_decode_utf8(const unsigned char* p, size_t available, int32_t* codePoint)
{
    static const struct
    {
        unsigned char firstLead;
        unsigned char lastLead;
        unsigned char length;
        unsigned char secondLow;
        unsigned char secondHigh;
    } sequences[] =
    {
        { 0x00, 0x7F, 1, 0, 0 },
        { 0xC2, 0xDF, 2, 0x80, 0xBF },
        { 0xE0, 0xE0, 3, 0xA0, 0xBF },
        { 0xE1, 0xEC, 3, 0x80, 0xBF },
        { 0xED, 0xED, 3, 0x80, 0x9F },
        { 0xEE, 0xEF, 3, 0x80, 0xBF },
        { 0xF0, 0xF0, 4, 0x90, 0xBF },
        { 0xF1, 0xF3, 4, 0x80, 0xBF },
        { 0xF4, 0xF4, 4, 0x80, 0x8F },
    };

    for (size_t i = 0; i < ARRAY_SIZE(sequences); i++)
    {
        if (p[0] < sequences[i].firstLead || p[0] > sequences[i].lastLead)
            continue;

        int32_t value = sequences[i].length == 1 ? p[0] : p[0] & (0x7F >> sequences[i].length);
        for (size_t j = 1; j < sequences[i].length; j++)
        {
            if (j >= available || (p[j] & 0xC0) != 0x80)
            {
                *codePoint = -1;
                return j;
            }

            if (j == 1 && (p[1] < sequences[i].secondLow || p[1] > sequences[i].secondHigh))
            {
                *codePoint = -1;
                return 2;
            }

            value = (value << 6) | (p[j] & 0x3F);
        }

        *codePoint = value;
        return sequences[i].length;
    }

    *codePoint = -1;
    return 1;
}

static size_t reference_utf8_to_utf16(const char* source, size_t sourceLength, CHAR16_T* destination, bool* invalid)
{
    const unsigned char* p = (const unsigned char*)source;
    size_t written = 0;

    *invalid = false;
    for (size_t i = 0; i < sourceLength;)
    {
        int32_t codePoint;
        i += reference_decode_utf8(p + i, sourceLength - i, &codePoint);
        if (codePoint < 0)
        {
            *invalid = true;
            codePoint = REPLACEMENT_CHAR;
        }

        if (codePoint >= 0x10000)
        {
            destination[written++] = (CHAR16_T)(0xD800 + ((codePoint - 0x10000) >> 10));
            destination[written++] = (CHAR16_T)(0xDC00 + (codePoint & 0x3FF));
        }
        else
        {
            destination[written++] = (CHAR16_T)codePoint;
        }
    }

    return written;
}

// A lone surrogate converts to the replacement character
static size_t reference_utf16_to_utf8(const CHAR16_T* source, size_t sourceLength, char* destination)
{
    unsigned char* p = (unsigned char*)destination;
    size_t written = 0;

    for (size_t i = 0; i < sourceLength; i++)
    {
        uint32_t codePoint = source[i];
        if (is_high_surrogate(codePoint) && i + 1 < sourceLength && is_low_surrogate(source[i + 1]))
        {
            codePoint = 0x10000 + ((codePoint - 0xD800) << 10) + (source[i + 1] - 0xDC00u);
            i++;
        }
        else if (is_high_surrogate(codePoint) || is_low_surrogate(codePoint))
        {
            codePoint = REPLACEMENT_CHAR;
        }

        written += reference_encode_utf8(codePoint, p + written);
    }

    return written;
}

// Checks the size query, an exactly sized destination, destinations that are too small, MINIPAL_MB_NO_REPLACE_INVALID_CHARS
// and MINIPAL_TREAT_AS_LITTLE_ENDIAN for one input. invalid tells whether the input has ill-formed sequences. The buffers
// have room for capacity code units, more than expectedLength.
static RESULT check_utf8_to_utf16_in(const char* name, size_t index, size_t variant,
    const char* source, size_t sourceLength, const CHAR16_T* expected, size_t expectedLength, bool invalid,
    CHAR16_T* native, CHAR16_T* little, size_t capacity)
{
    size_t count;
    int error;

    errno = ERRNO_SENTINEL;
    count = minipal_get_length_utf8_to_utf16(source, sourceLength, 0);
    error = errno;
    if (count != expectedLength || error != 0)
    {
        return FAILED("%s %zu.%zu: minipal_get_length_utf8_to_utf16 returned %zu with errno %d, expected %zu",
            name, index, variant, count, error, expectedLength);
    }

    // An exactly sized destination gets the whole string and nothing past it
    fill_utf16(native, capacity);
    errno = ERRNO_SENTINEL;
    count = minipal_convert_utf8_to_utf16(source, sourceLength, native, expectedLength, 0);
    error = errno;
    if (count != expectedLength || error != 0 || memcmp(native, expected, expectedLength * sizeof(CHAR16_T)) != 0 ||
        native[expectedLength] != Utf16Canary)
    {
        return FAILED("%s %zu.%zu: minipal_convert_utf8_to_utf16 returned %zu with errno %d, expected %zu, or produced an unexpected string",
            name, index, variant, count, error, expectedLength);
    }

    // Destinations that are too small fail without writing past their end
    if (expectedLength != 0)
    {
        const size_t sizes[] = { expectedLength - 1, 0 };
        for (size_t s = 0; s < ARRAY_SIZE(sizes); s++)
        {
            fill_utf16(native, capacity);
            count = minipal_convert_utf8_to_utf16(source, sourceLength, native, sizes[s], 0);
            error = errno;
            if (count != 0 || error != MINIPAL_ERROR_INSUFFICIENT_BUFFER || native[sizes[s]] != Utf16Canary)
            {
                return FAILED("%s %zu.%zu: minipal_convert_utf8_to_utf16 into %zu elements returned %zu with errno %d, expected 0 with MINIPAL_ERROR_INSUFFICIENT_BUFFER and nothing written past them",
                    name, index, variant, sizes[s], count, error);
            }
        }
    }

    // MINIPAL_MB_NO_REPLACE_INVALID_CHARS fails on any ill-formed sequence, including one at the end of the input
    const size_t strictLength = invalid ? 0 : expectedLength;
    const int strictError = invalid ? (int)MINIPAL_ERROR_NO_UNICODE_TRANSLATION : 0;
    errno = ERRNO_SENTINEL;
    count = minipal_get_length_utf8_to_utf16(source, sourceLength, MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
    error = errno;
    if (count != strictLength || error != strictError)
    {
        return FAILED("%s %zu.%zu: minipal_get_length_utf8_to_utf16 with MINIPAL_MB_NO_REPLACE_INVALID_CHARS returned %zu with errno %d, expected %zu with errno %d",
            name, index, variant, count, error, strictLength, strictError);
    }

    fill_utf16(native, capacity);
    errno = ERRNO_SENTINEL;
    count = minipal_convert_utf8_to_utf16(source, sourceLength, native, capacity, MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
    error = errno;
    if (count != strictLength || error != strictError || memcmp(native, expected, strictLength * sizeof(CHAR16_T)) != 0)
    {
        return FAILED("%s %zu.%zu: minipal_convert_utf8_to_utf16 with MINIPAL_MB_NO_REPLACE_INVALID_CHARS returned %zu with errno %d, expected %zu with errno %d",
            name, index, variant, count, error, strictLength, strictError);
    }

    // MINIPAL_TREAT_AS_LITTLE_ENDIAN gives the native result with each code unit byte-swapped on big-endian platforms,
    // and the native result elsewhere, for any destination size
    const bool bigEndian = is_big_endian();
    const unsigned int flagSets[] = { 0, MINIPAL_MB_NO_REPLACE_INVALID_CHARS };
    const size_t sizes[] = { expectedLength, expectedLength != 0 ? expectedLength - 1 : 0, capacity };
    for (size_t f = 0; f < ARRAY_SIZE(flagSets); f++)
    {
        const unsigned int flags = flagSets[f];

        errno = ERRNO_SENTINEL;
        size_t nativeCount = minipal_get_length_utf8_to_utf16(source, sourceLength, flags);
        int nativeError = errno;
        errno = ERRNO_SENTINEL;
        size_t littleCount = minipal_get_length_utf8_to_utf16(source, sourceLength, flags | MINIPAL_TREAT_AS_LITTLE_ENDIAN);
        int littleError = errno;
        if (littleCount != nativeCount || littleError != nativeError)
        {
            return FAILED("%s %zu.%zu: minipal_get_length_utf8_to_utf16 with flags 0x%x returned %zu with errno %d for little-endian, %zu with errno %d otherwise",
                name, index, variant, flags, littleCount, littleError, nativeCount, nativeError);
        }

        for (size_t s = 0; s < ARRAY_SIZE(sizes); s++)
        {
            fill_utf16(native, capacity);
            fill_utf16(little, capacity);
            errno = ERRNO_SENTINEL;
            nativeCount = minipal_convert_utf8_to_utf16(source, sourceLength, native, sizes[s], flags);
            nativeError = errno;
            errno = ERRNO_SENTINEL;
            littleCount = minipal_convert_utf8_to_utf16(source, sourceLength, little, sizes[s], flags | MINIPAL_TREAT_AS_LITTLE_ENDIAN);
            littleError = errno;
            if (littleCount != nativeCount || littleError != nativeError)
            {
                return FAILED("%s %zu.%zu: minipal_convert_utf8_to_utf16 with flags 0x%x into %zu elements returned %zu with errno %d for little-endian, %zu with errno %d otherwise",
                    name, index, variant, flags, sizes[s], littleCount, littleError, nativeCount, nativeError);
            }

            for (size_t i = 0; i < nativeCount; i++)
            {
                const CHAR16_T expectedUnit = bigEndian ? swap_bytes(native[i]) : native[i];
                if (little[i] != expectedUnit)
                {
                    return FAILED("%s %zu.%zu: minipal_convert_utf8_to_utf16 with flags 0x%x into %zu elements wrote 0x%04x at %zu for little-endian, expected 0x%04x",
                        name, index, variant, flags, sizes[s], (unsigned)little[i], i, (unsigned)expectedUnit);
                }
            }
        }
    }

    return OK;
}

static RESULT check_utf8_to_utf16(const char* name, size_t index, size_t variant,
    const char* source, size_t sourceLength, const CHAR16_T* expected, size_t expectedLength, bool invalid)
{
    const size_t capacity = expectedLength + 16;
    CHAR16_T* native = (CHAR16_T*)malloc(capacity * sizeof(CHAR16_T));
    CHAR16_T* little = (CHAR16_T*)malloc(capacity * sizeof(CHAR16_T));
    RESULT result = native != NULL && little != NULL
        ? check_utf8_to_utf16_in(name, index, variant, source, sourceLength, expected, expectedLength, invalid, native, little, capacity)
        : FAILED("%s %zu.%zu: out of memory", name, index, variant);
    free(native);
    free(little);
    return result;
}

// Converts a NUL-terminated string without and with its terminator
static RESULT check_utf8_string(const char* name, size_t index, size_t variant, const char* utf8, const CHAR16_T* utf16)
{
    const size_t utf8Length = strlen(utf8);
    const size_t utf16Length = utf16_length(utf16);
    const bool invalid = has_replacement_char(utf16, utf16Length);

    CHECK(check_utf8_to_utf16(name, index, variant, utf8, utf8Length, utf16, utf16Length, invalid));
    return check_utf8_to_utf16(name, index, variant, utf8, utf8Length + 1, utf16, utf16Length + 1, invalid);
}

// Embeds the string between ASCII runs long enough to take the fast paths, at every offset modulo 8
static RESULT check_utf8_string_embedded(const char* name, size_t index, const char* utf8, const CHAR16_T* utf16)
{
    const size_t suffix = 16;
    const size_t utf8Length = strlen(utf8);
    const size_t utf16Length = utf16_length(utf16);
    char longUtf8[128];
    CHAR16_T longUtf16[128];

    for (size_t prefix = 14; prefix < 22; prefix++)
    {
        for (size_t j = 0; j < prefix; j++)
        {
            longUtf8[j] = (char)('a' + j % 26);
            longUtf16[j] = (CHAR16_T)('a' + j % 26);
        }

        memcpy(longUtf8 + prefix, utf8, utf8Length);
        memcpy(longUtf16 + prefix, utf16, utf16Length * sizeof(CHAR16_T));

        for (size_t j = 0; j < suffix; j++)
        {
            longUtf8[prefix + utf8Length + j] = (char)('A' + j);
            longUtf16[prefix + utf16Length + j] = (CHAR16_T)('A' + j);
        }

        longUtf8[prefix + utf8Length + suffix] = '\0';
        longUtf16[prefix + utf16Length + suffix] = 0;

        CHECK(check_utf8_string(name, index, prefix, longUtf8, longUtf16));
    }

    return OK;
}

// Checks the size query, an exactly sized destination, destinations that are too small, MINIPAL_MB_NO_REPLACE_INVALID_CHARS
// and MINIPAL_TREAT_AS_LITTLE_ENDIAN for one input. The buffers have room for capacity bytes, more than expectedLength,
// and swapped for sourceLength code units.
static RESULT check_utf16_to_utf8_in(const char* name, size_t index, size_t variant,
    const CHAR16_T* source, size_t sourceLength, const char* expected, size_t expectedLength,
    char* native, char* little, CHAR16_T* swapped, size_t capacity)
{
    size_t count;
    int error;

    errno = ERRNO_SENTINEL;
    count = minipal_get_length_utf16_to_utf8(source, sourceLength, 0);
    error = errno;
    if (count != expectedLength || error != 0)
    {
        return FAILED("%s %zu.%zu: minipal_get_length_utf16_to_utf8 returned %zu with errno %d, expected %zu",
            name, index, variant, count, error, expectedLength);
    }

    // An exactly sized destination gets the whole string and nothing past it
    memset(native, Utf8Canary, capacity);
    errno = ERRNO_SENTINEL;
    count = minipal_convert_utf16_to_utf8(source, sourceLength, native, expectedLength, 0);
    error = errno;
    if (count != expectedLength || error != 0 || memcmp(native, expected, expectedLength) != 0 || native[expectedLength] != Utf8Canary)
    {
        return FAILED("%s %zu.%zu: minipal_convert_utf16_to_utf8 returned %zu with errno %d, expected %zu, or produced an unexpected string",
            name, index, variant, count, error, expectedLength);
    }

    // Destinations that are too small fail without writing past their end
    if (expectedLength != 0)
    {
        const size_t sizes[] = { expectedLength - 1, 0 };
        for (size_t s = 0; s < ARRAY_SIZE(sizes); s++)
        {
            memset(native, Utf8Canary, capacity);
            count = minipal_convert_utf16_to_utf8(source, sourceLength, native, sizes[s], 0);
            error = errno;
            if (count != 0 || error != MINIPAL_ERROR_INSUFFICIENT_BUFFER || native[sizes[s]] != Utf8Canary)
            {
                return FAILED("%s %zu.%zu: minipal_convert_utf16_to_utf8 into %zu bytes returned %zu with errno %d, expected 0 with MINIPAL_ERROR_INSUFFICIENT_BUFFER and nothing written past them",
                    name, index, variant, sizes[s], count, error);
            }
        }
    }

    // MINIPAL_MB_NO_REPLACE_INVALID_CHARS only applies to UTF-8 input, lone surrogates are always replaced
    errno = ERRNO_SENTINEL;
    count = minipal_get_length_utf16_to_utf8(source, sourceLength, MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
    error = errno;
    if (count != expectedLength || error != 0)
    {
        return FAILED("%s %zu.%zu: minipal_get_length_utf16_to_utf8 with MINIPAL_MB_NO_REPLACE_INVALID_CHARS returned %zu with errno %d, expected %zu",
            name, index, variant, count, error, expectedLength);
    }

    memset(native, Utf8Canary, capacity);
    errno = ERRNO_SENTINEL;
    count = minipal_convert_utf16_to_utf8(source, sourceLength, native, capacity, MINIPAL_MB_NO_REPLACE_INVALID_CHARS);
    error = errno;
    if (count != expectedLength || error != 0 || memcmp(native, expected, expectedLength) != 0)
    {
        return FAILED("%s %zu.%zu: minipal_convert_utf16_to_utf8 with MINIPAL_MB_NO_REPLACE_INVALID_CHARS returned %zu with errno %d, expected %zu",
            name, index, variant, count, error, expectedLength);
    }

    // With MINIPAL_TREAT_AS_LITTLE_ENDIAN, the input with each code unit byte-swapped on big-endian platforms converts
    // like the native input, and the flag changes nothing elsewhere, for any destination size
    const bool bigEndian = is_big_endian();
    for (size_t i = 0; i < sourceLength; i++)
        swapped[i] = bigEndian ? swap_bytes(source[i]) : source[i];

    const unsigned int flagSets[] = { 0, MINIPAL_MB_NO_REPLACE_INVALID_CHARS };
    const size_t sizes[] = { expectedLength, expectedLength != 0 ? expectedLength - 1 : 0, capacity };
    for (size_t f = 0; f < ARRAY_SIZE(flagSets); f++)
    {
        const unsigned int flags = flagSets[f];

        errno = ERRNO_SENTINEL;
        size_t nativeCount = minipal_get_length_utf16_to_utf8(source, sourceLength, flags);
        int nativeError = errno;
        errno = ERRNO_SENTINEL;
        size_t littleCount = minipal_get_length_utf16_to_utf8(swapped, sourceLength, flags | MINIPAL_TREAT_AS_LITTLE_ENDIAN);
        int littleError = errno;
        if (littleCount != nativeCount || littleError != nativeError)
        {
            return FAILED("%s %zu.%zu: minipal_get_length_utf16_to_utf8 with flags 0x%x returned %zu with errno %d for little-endian, %zu with errno %d otherwise",
                name, index, variant, flags, littleCount, littleError, nativeCount, nativeError);
        }

        for (size_t s = 0; s < ARRAY_SIZE(sizes); s++)
        {
            memset(native, Utf8Canary, capacity);
            memset(little, Utf8Canary, capacity);
            errno = ERRNO_SENTINEL;
            nativeCount = minipal_convert_utf16_to_utf8(source, sourceLength, native, sizes[s], flags);
            nativeError = errno;
            errno = ERRNO_SENTINEL;
            littleCount = minipal_convert_utf16_to_utf8(swapped, sourceLength, little, sizes[s], flags | MINIPAL_TREAT_AS_LITTLE_ENDIAN);
            littleError = errno;
            if (littleCount != nativeCount || littleError != nativeError || memcmp(little, native, nativeCount) != 0)
            {
                return FAILED("%s %zu.%zu: minipal_convert_utf16_to_utf8 with flags 0x%x into %zu bytes returned %zu with errno %d for little-endian, %zu with errno %d otherwise, or different bytes",
                    name, index, variant, flags, sizes[s], littleCount, littleError, nativeCount, nativeError);
            }
        }
    }

    return OK;
}

static RESULT check_utf16_to_utf8(const char* name, size_t index, size_t variant,
    const CHAR16_T* source, size_t sourceLength, const char* expected, size_t expectedLength)
{
    const size_t capacity = expectedLength + 16;
    char* native = (char*)malloc(capacity);
    char* little = (char*)malloc(capacity);
    CHAR16_T* swapped = (CHAR16_T*)malloc((sourceLength + 1) * sizeof(CHAR16_T));
    RESULT result = native != NULL && little != NULL && swapped != NULL
        ? check_utf16_to_utf8_in(name, index, variant, source, sourceLength, expected, expectedLength, native, little, swapped, capacity)
        : FAILED("%s %zu.%zu: out of memory", name, index, variant);
    free(native);
    free(little);
    free(swapped);
    return result;
}

// Converts a NUL-terminated string without and with its terminator
static RESULT check_utf16_string(const char* name, size_t index, size_t variant, const CHAR16_T* utf16, const char* utf8)
{
    const size_t utf16Length = utf16_length(utf16);
    const size_t utf8Length = strlen(utf8);

    CHECK(check_utf16_to_utf8(name, index, variant, utf16, utf16Length, utf8, utf8Length));
    return check_utf16_to_utf8(name, index, variant, utf16, utf16Length + 1, utf8, utf8Length + 1);
}

// Embeds the string between ASCII runs long enough to take the fast paths, at every offset modulo 8
static RESULT check_utf16_string_embedded(const char* name, size_t index, const CHAR16_T* utf16, const char* utf8)
{
    const size_t suffix = 16;
    const size_t utf16Length = utf16_length(utf16);
    const size_t utf8Length = strlen(utf8);
    CHAR16_T longUtf16[128];
    char longUtf8[128];

    for (size_t prefix = 14; prefix < 22; prefix++)
    {
        for (size_t j = 0; j < prefix; j++)
        {
            longUtf16[j] = (CHAR16_T)('a' + j % 26);
            longUtf8[j] = (char)('a' + j % 26);
        }

        memcpy(longUtf16 + prefix, utf16, utf16Length * sizeof(CHAR16_T));
        memcpy(longUtf8 + prefix, utf8, utf8Length);

        for (size_t j = 0; j < suffix; j++)
        {
            longUtf16[prefix + utf16Length + j] = (CHAR16_T)('A' + j);
            longUtf8[prefix + utf8Length + j] = (char)('A' + j);
        }

        longUtf16[prefix + utf16Length + suffix] = 0;
        longUtf8[prefix + utf8Length + suffix] = '\0';

        CHECK(check_utf16_string(name, index, prefix, longUtf16, longUtf8));
    }

    return OK;
}

static uint32_t next_random(uint32_t* state)
{
    uint32_t x = *state;
    x ^= x << 13;
    x ^= x >> 17;
    x ^= x << 5;
    return *state = x;
}

static uint32_t random_code_point(uint32_t* state, uint32_t kind)
{
    uint32_t r = next_random(state);
    switch (kind % 4)
    {
        case 0:
            return r % 0x80;
        case 1:
            return 0x80 + r % (0x800 - 0x80);
        case 2:
        {
            uint32_t codePoint = 0x800 + r % (0x10000 - 0x800 - 0x800);
            return codePoint >= 0xD800 ? codePoint + 0x800 : codePoint;
        }
        default:
            return 0x10000 + r % 0x100000;
    }
}

#define RANDOM_PIECES 24
#define RANDOM_RUN 24
#define RANDOM_UTF8_CAPACITY (RANDOM_PIECES * RANDOM_RUN * 4)

// Runs of characters of one length, which the fast paths stay in, mixed with the kinds of ill-formed sequences that
// make them stop
static size_t random_utf8(uint32_t* state, char* buffer)
{
    static const unsigned char invalidLeads[] = { 0xC0, 0xC1, 0xE0, 0xED, 0xF0, 0xF4, 0xF5, 0xF8, 0xFE, 0xFF };
    unsigned char* p = (unsigned char*)buffer;
    size_t length = 0;
    size_t pieces = 1 + next_random(state) % RANDOM_PIECES;

    for (size_t piece = 0; piece < pieces; piece++)
    {
        uint32_t r = next_random(state);
        size_t run = 1 + (r >> 8) % RANDOM_RUN;
        switch (r % 8)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                // a run of 1, 2, 3 or 4 byte encoded characters
                for (size_t i = 0; i < run; i++)
                    length += reference_encode_utf8(random_code_point(state, r), p + length);
                break;
            case 4:
                // characters of any length
                for (size_t i = 0; i < run; i++)
                    length += reference_encode_utf8(random_code_point(state, next_random(state)), p + length);
                break;
            case 5:
                // a byte that isn't ASCII
                p[length++] = (unsigned char)(0x80 | next_random(state));
                break;
            case 6:
            {
                // an incomplete 2, 3 or 4 byte encoded character
                unsigned char sequence[4];
                size_t sequenceLength = reference_encode_utf8(random_code_point(state, 1 + next_random(state) % 3), sequence);
                size_t keep = 1 + next_random(state) % (sequenceLength - 1);
                memcpy(p + length, sequence, keep);
                length += keep;
                break;
            }
            default:
                // a lead byte that is invalid or can start ill-formed sequences, followed by continuation bytes
                p[length++] = invalidLeads[next_random(state) % ARRAY_SIZE(invalidLeads)];
                for (size_t i = 0; i < 3; i++)
                    p[length++] = (unsigned char)(0x80 | (next_random(state) % 0x40));
                break;
        }
    }

    return length;
}

#define RANDOM_UTF16_CAPACITY (RANDOM_PIECES * RANDOM_RUN * 2)

// Runs of characters of one UTF-8 length, which the fast paths stay in, mixed with lone surrogates
static size_t random_utf16(uint32_t* state, CHAR16_T* buffer)
{
    size_t length = 0;
    size_t pieces = 1 + next_random(state) % RANDOM_PIECES;

    for (size_t piece = 0; piece < pieces; piece++)
    {
        uint32_t r = next_random(state);
        size_t run = 1 + (r >> 8) % RANDOM_RUN;
        switch (r % 8)
        {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
                // a run of characters of one UTF-8 length, or of any length
                for (size_t i = 0; i < run; i++)
                {
                    uint32_t codePoint = random_code_point(state, r % 8 == 4 ? next_random(state) : r);
                    if (codePoint >= 0x10000)
                    {
                        buffer[length++] = (CHAR16_T)(0xD800 + ((codePoint - 0x10000) >> 10));
                        buffer[length++] = (CHAR16_T)(0xDC00 + (codePoint & 0x3FF));
                    }
                    else
                    {
                        buffer[length++] = (CHAR16_T)codePoint;
                    }
                }
                break;
            case 5:
                // a lone high surrogate
                buffer[length++] = (CHAR16_T)(0xD800 + next_random(state) % 0x400);
                break;
            case 6:
                // a lone low surrogate
                buffer[length++] = (CHAR16_T)(0xDC00 + next_random(state) % 0x400);
                break;
            default:
                // a run of lone high surrogates
                for (size_t i = 0; i < run; i++)
                    buffer[length++] = (CHAR16_T)(0xD800 + next_random(state) % 0x400);
                break;
        }
    }

    return length;
}

// Long runs of one kind of character, around the lengths where the UTF-16 counting flushes its per-lane counts
static const size_t long_run_lengths[] = { 4095, 4096, 4097, 16383, 16384, 16385, 32767, 32768, 32769, 32776, 65537 };
#define LONG_RUN_KINDS 7

static void make_long_run(size_t kind, CHAR16_T* buffer, size_t length)
{
    for (size_t i = 0; i < length; i++)
    {
        switch (kind)
        {
            case 0: buffer[i] = (CHAR16_T)('a' + i % 26); break;
            case 1: buffer[i] = 0x0080; break;
            case 2: buffer[i] = (i % 2 == 0) ? (CHAR16_T)'A' : (CHAR16_T)0x07FF; break;
            case 3: buffer[i] = 0x0800; break;
            case 4: buffer[i] = (i % 2 == 0) ? (CHAR16_T)0xD83D : (CHAR16_T)0xDE00; break;
            case 5: buffer[i] = (i % 3 == 0) ? (CHAR16_T)'A' : (i % 3 == 1) ? (CHAR16_T)0x07FF : (CHAR16_T)0xFFFF; break;
            default: buffer[i] = (CHAR16_T)((i % 1000 == 999) ? 0xD800 : 0x00E9); break;
        }
    }
}

static RESULT test_utf8_to_utf16_valid_strings(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(valid_strings); i++)
        CHECK(check_utf8_string("valid string", i, 0, valid_strings[i].utf8, valid_strings[i].utf16));

    return OK;
}

static RESULT test_utf8_to_utf16_invalid_strings(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(invalid_utf8_strings); i++)
        CHECK(check_utf8_string("invalid string", i, 0, invalid_utf8_strings[i].utf8, invalid_utf8_strings[i].utf16));

    return OK;
}

static RESULT test_utf8_to_utf16_long_strings(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(valid_strings); i++)
        CHECK(check_utf8_string_embedded("long valid string", i, valid_strings[i].utf8, valid_strings[i].utf16));

    for (size_t i = 0; i < ARRAY_SIZE(invalid_utf8_strings); i++)
        CHECK(check_utf8_string_embedded("long invalid string", i, invalid_utf8_strings[i].utf8, invalid_utf8_strings[i].utf16));

    return OK;
}

static RESULT test_utf8_to_utf16_embedded_nuls(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(utf8_strings_with_nuls); i++)
    {
        const CountedStringPair* pair = &utf8_strings_with_nuls[i];
        CHECK(check_utf8_to_utf16("string with NULs", i, 0, pair->utf8, pair->utf8Length, pair->utf16, pair->utf16Length,
            has_replacement_char(pair->utf16, pair->utf16Length)));
    }

    return OK;
}

static RESULT test_utf8_to_utf16_long_runs(void)
{
    const size_t maxLength = long_run_lengths[ARRAY_SIZE(long_run_lengths) - 1];
    CHAR16_T* utf16 = (CHAR16_T*)malloc(maxLength * sizeof(CHAR16_T));
    CHAR16_T* expected = (CHAR16_T*)malloc(maxLength * sizeof(CHAR16_T));
    char* utf8 = (char*)malloc(maxLength * 3);
    RESULT result = utf16 != NULL && expected != NULL && utf8 != NULL ? OK : FAILED("out of memory");

    for (size_t kind = 0; kind < LONG_RUN_KINDS && result == OK; kind++)
    {
        for (size_t i = 0; i < ARRAY_SIZE(long_run_lengths) && result == OK; i++)
        {
            bool invalid;
            make_long_run(kind, utf16, long_run_lengths[i]);
            size_t utf8Length = reference_utf16_to_utf8(utf16, long_run_lengths[i], utf8);
            size_t expectedLength = reference_utf8_to_utf16(utf8, utf8Length, expected, &invalid);
            result = check_utf8_to_utf16("long run", kind, long_run_lengths[i], utf8, utf8Length, expected, expectedLength, invalid);
        }
    }

    free(utf16);
    free(expected);
    free(utf8);
    return result;
}

// Random input checked against the reference model
static RESULT test_utf8_to_utf16_random(void)
{
    uint32_t state = 0x2545F491;
    char* source = (char*)malloc(RANDOM_UTF8_CAPACITY);
    CHAR16_T* expected = (CHAR16_T*)malloc(RANDOM_UTF8_CAPACITY * sizeof(CHAR16_T));
    RESULT result = source != NULL && expected != NULL ? OK : FAILED("out of memory");

    for (size_t i = 0; i < 2000 && result == OK; i++)
    {
        bool invalid;
        size_t sourceLength = random_utf8(&state, source);
        size_t expectedLength = reference_utf8_to_utf16(source, sourceLength, expected, &invalid);
        result = check_utf8_to_utf16("random string", i, 0, source, sourceLength, expected, expectedLength, invalid);
    }

    free(source);
    free(expected);
    return result;
}

static RESULT test_utf16_to_utf8_valid_strings(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(valid_strings); i++)
        CHECK(check_utf16_string("valid string", i, 0, valid_strings[i].utf16, valid_strings[i].utf8));

    return OK;
}

static RESULT test_utf16_to_utf8_invalid_strings(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(invalid_utf16_strings); i++)
        CHECK(check_utf16_string("invalid string", i, 0, invalid_utf16_strings[i].utf16, invalid_utf16_strings[i].utf8));

    return OK;
}

static RESULT test_utf16_to_utf8_long_strings(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(valid_strings); i++)
        CHECK(check_utf16_string_embedded("long valid string", i, valid_strings[i].utf16, valid_strings[i].utf8));

    for (size_t i = 0; i < ARRAY_SIZE(invalid_utf16_strings); i++)
        CHECK(check_utf16_string_embedded("long invalid string", i, invalid_utf16_strings[i].utf16, invalid_utf16_strings[i].utf8));

    return OK;
}

static RESULT test_utf16_to_utf8_embedded_nuls(void)
{
    for (size_t i = 0; i < ARRAY_SIZE(utf16_strings_with_nuls); i++)
    {
        const CountedStringPair* pair = &utf16_strings_with_nuls[i];
        CHECK(check_utf16_to_utf8("string with NULs", i, 0, pair->utf16, pair->utf16Length, pair->utf8, pair->utf8Length));
    }

    return OK;
}

static RESULT test_utf16_to_utf8_long_runs(void)
{
    const size_t maxLength = long_run_lengths[ARRAY_SIZE(long_run_lengths) - 1];
    CHAR16_T* utf16 = (CHAR16_T*)malloc(maxLength * sizeof(CHAR16_T));
    char* expected = (char*)malloc(maxLength * 3);
    RESULT result = utf16 != NULL && expected != NULL ? OK : FAILED("out of memory");

    for (size_t kind = 0; kind < LONG_RUN_KINDS && result == OK; kind++)
    {
        for (size_t i = 0; i < ARRAY_SIZE(long_run_lengths) && result == OK; i++)
        {
            make_long_run(kind, utf16, long_run_lengths[i]);
            size_t expectedLength = reference_utf16_to_utf8(utf16, long_run_lengths[i], expected);
            result = check_utf16_to_utf8("long run", kind, long_run_lengths[i], utf16, long_run_lengths[i], expected, expectedLength);
        }
    }

    free(utf16);
    free(expected);
    return result;
}

// Random input checked against the reference model
static RESULT test_utf16_to_utf8_random(void)
{
    uint32_t state = 0x9E3779B9;
    CHAR16_T* source = (CHAR16_T*)malloc(RANDOM_UTF16_CAPACITY * sizeof(CHAR16_T));
    char* expected = (char*)malloc(RANDOM_UTF16_CAPACITY * 3);
    RESULT result = source != NULL && expected != NULL ? OK : FAILED("out of memory");

    for (size_t i = 0; i < 2000 && result == OK; i++)
    {
        size_t sourceLength = random_utf16(&state, source);
        size_t expectedLength = reference_utf16_to_utf8(source, sourceLength, expected);
        result = check_utf16_to_utf8("random string", i, 0, source, sourceLength, expected, expectedLength);
    }

    free(source);
    free(expected);
    return result;
}

static Test minipal_utf8_tests[] =
{
    {"test_utf8_to_utf16_valid_strings", test_utf8_to_utf16_valid_strings},
    {"test_utf8_to_utf16_invalid_strings", test_utf8_to_utf16_invalid_strings},
    {"test_utf8_to_utf16_long_strings", test_utf8_to_utf16_long_strings},
    {"test_utf8_to_utf16_embedded_nuls", test_utf8_to_utf16_embedded_nuls},
    {"test_utf8_to_utf16_long_runs", test_utf8_to_utf16_long_runs},
    {"test_utf8_to_utf16_random", test_utf8_to_utf16_random},
    {"test_utf16_to_utf8_valid_strings", test_utf16_to_utf8_valid_strings},
    {"test_utf16_to_utf8_invalid_strings", test_utf16_to_utf8_invalid_strings},
    {"test_utf16_to_utf8_long_strings", test_utf16_to_utf8_long_strings},
    {"test_utf16_to_utf8_embedded_nuls", test_utf16_to_utf8_embedded_nuls},
    {"test_utf16_to_utf8_long_runs", test_utf16_to_utf8_long_runs},
    {"test_utf16_to_utf8_random", test_utf16_to_utf8_random},
    {NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(minipal_utf8_tests_init, minipal_utf8_tests)
