// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

typedef char16_t WCHAR;

#include <dn-u16.h>
#include <ctype.h>
#include <errno.h>
#include <limits.h>
#include <stdlib.h>
#include <string.h>
#include <wctype.h>
#include <minipal/strings.h>

size_t u16_strlen(const WCHAR* str)
{
    return minipal_u16_strlen((CHAR16_T*)str);
}

int u16_strcmp(const WCHAR* str1, const WCHAR* str2)
{
    return u16_strncmp(str1, str2, 0x7fffffff);
}

int u16_strncmp(const WCHAR* str1, const WCHAR* str2, size_t count)
{
    int diff = 0;
    for (size_t i = 0; i < count; i++)
    {
        diff = str1[i] - str2[i];
        if (diff != 0)
            break;

        // stop if we reach the end of the string
        if(str1[i] == (WCHAR)'\0')
            break;
    }
    return diff;
}

int u16_stricmp(const WCHAR* str1, const WCHAR* str2)
{
    return u16_strnicmp(str1, str2, 0x7fffffff);
}

int u16_strnicmp(const WCHAR* str1, const WCHAR* str2, size_t count)
{
    int diff = 0;
    for (size_t i = 0; i < count; i++)
    {
        diff = (int)towlower(str1[i]) - (int)towlower(str2[i]);
        if (diff != 0)
            break;

        // stop if we reach the end of the string
        if (str1[i] == (WCHAR)'\0')
            break;
    }
    return diff;
}

WCHAR* u16_strcat_s(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (dst == nullptr || src == nullptr)
    {
        return nullptr;
    }

    WCHAR* start = dst;
    WCHAR* end = dst + dstLen;

    // find end of source string
    while (*dst)
    {
        dst++;
        if (dst >= end)
            return nullptr;
    }

    // concatenate new string
    size_t srcLength = u16_strlen(src);
    size_t loopCount = 0;
    while (*src && loopCount < srcLength)
    {
        *dst++ = *src++;
        if (dst >= end)
            return nullptr;
        loopCount++;
    }

    // add terminating null
    *dst = (WCHAR)'\0';
    return start;
}

WCHAR* u16_strcpy_s(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (dst == nullptr || src == nullptr)
    {
        return nullptr;
    }

    WCHAR* start = dst;
    WCHAR* end = dst + dstLen;

    // copy source string to destination string
    while (*src)
    {
        *dst++ = *src++;
        if (dst >= end)
            return nullptr;
    }

    // add terminating null
    *dst = (WCHAR)'\0';
    return start;
}

WCHAR* u16_strncpy_s(WCHAR* dst, size_t dstLen, const WCHAR* src, size_t count)
{
    ::memset(dst, 0, dstLen * sizeof(WCHAR));

    size_t srcLength = u16_strlen(src);
    size_t length = (count < srcLength) ? count : srcLength;
    if (length > dstLen)
        return nullptr;

    ::memcpy(dst, src, length * sizeof(WCHAR));
    return dst;
}

const WCHAR* u16_strstr(const WCHAR *str, const WCHAR *strCharSet)
{
    if (str == nullptr || strCharSet == nullptr)
    {
        return nullptr;
    }

    // No characters to examine
    if (u16_strlen(strCharSet) == 0)
        return str;

    const WCHAR* ret = nullptr;
    int i;
    while (*str != (WCHAR)'\0')
    {
        i = 0;
        while (true)
        {
            if (*(strCharSet + i) == (WCHAR)'\0')
            {
                ret = str;
                goto LEAVE;
            }
            else if (*(str + i) == (WCHAR)'\0')
            {
                ret = nullptr;
                goto LEAVE;
            }
            else if (*(str + i) != *(strCharSet + i))
            {
                break;
            }
            i++;
        }
        str++;
    }
 LEAVE:
    return ret;
}

const WCHAR* u16_strchr(const WCHAR* str, WCHAR ch)
{
    while (*str)
    {
        if (*str == ch)
            return str;
        str++;
    }

    // Check if the comparand was \000
    if (*str == ch)
        return str;

    return nullptr;
}

const WCHAR* u16_strrchr(const WCHAR* str, WCHAR ch)
{
    const WCHAR* last = nullptr;
    while (*str)
    {
        if (*str == ch)
            last = str;
        str++;
    }

    return last;
}

static bool is_ascii(WCHAR ch)
{
    return ch < 0x80;
}

static bool is_floating_point_char(WCHAR ch)
{
    return ch == '+' || ch == '-' || ch == '.' || (ch >= '0' && ch <= '9') ||
           ch == 'e' || ch == 'E' || ch == 'd' || ch == 'D';
}

// Narrows the leading characters of str that satisfy the predicate into a malloc-ed buffer.
// The characters are ASCII, so offsets into the buffer are also offsets into str.
static char* narrow_prefix(const WCHAR* str, bool (*predicate)(WCHAR))
{
    size_t length = 0;
    while (str[length] != (WCHAR)'\0' && predicate(str[length]))
        length++;

    char* buffer = (char*)malloc(length + 1);
    if (buffer == nullptr)
        return nullptr;

    for (size_t i = 0; i < length; i++)
        buffer[i] = (char)str[i];

    buffer[length] = '\0';
    return buffer;
}

uint32_t u16_strtoul(const WCHAR* nptr, WCHAR** endptr, int base)
{
    char* buffer = narrow_prefix(nptr, is_ascii);
    if (buffer == nullptr)
    {
        if (endptr != nullptr)
            *endptr = (WCHAR*)nptr;
        return 0;
    }

    char* end;
    unsigned long value = strtoul(buffer, &end, base);

#if ULONG_MAX > UINT32_MAX
    // Windows' unsigned long is 32-bit, so positive values that don't fit in it saturate.
    // Negative values are truncated instead, regardless of their magnitude.
    if (value > UINT32_MAX)
    {
        const char* p = buffer;
        while (isspace((unsigned char)*p))
            p++;

        if (*p != '-')
        {
            value = UINT32_MAX;
            errno = ERANGE;
        }
    }
#endif

    if (endptr != nullptr)
        *endptr = (WCHAR*)nptr + (end - buffer);

    free(buffer);
    return (uint32_t)value;
}

uint64_t u16_strtoui64(const WCHAR* nptr, WCHAR** endptr, int base)
{
    char* buffer = narrow_prefix(nptr, is_ascii);
    if (buffer == nullptr)
    {
        if (endptr != nullptr)
            *endptr = (WCHAR*)nptr;
        return 0;
    }

    char* end;
    unsigned long long value = strtoull(buffer, &end, base);

    if (endptr != nullptr)
        *endptr = (WCHAR*)nptr + (end - buffer);

    free(buffer);
    return value;
}

// Only decimal notation is accepted; the C runtime strtod would also accept hex, "inf" and "nan".
// The end pointer is set past all the characters that can be part of a decimal number, even
// the ones strtod didn't consume, e.g. past the "e" of "1.5e".
double u16_strtod(const WCHAR* nptr, WCHAR** endptr)
{
    if (nptr == nullptr)
        return 0.0;

    const WCHAR* start = nptr;
    while (iswspace(*start))
        start++;

    double value = 0.0;
    const WCHAR* stop = nptr;

    char* buffer = narrow_prefix(start, is_floating_point_char);
    if (buffer != nullptr && buffer[0] != '\0')
    {
        char* end;
        value = strtod(buffer, &end);
        if (value != 0.0 || end != buffer)
            stop = start + strlen(buffer);
    }

    if (endptr != nullptr)
        *endptr = (WCHAR*)stop;

    free(buffer);
    return value;
}
