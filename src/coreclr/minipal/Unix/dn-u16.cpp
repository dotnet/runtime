// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

typedef char16_t WCHAR;

#include <dn-u16.h>
#include <string.h>

size_t u16_strlen(const WCHAR* str)
{
    size_t nChar = 0;
    while (*str++)
        nChar++;
    return nChar;
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

// Forward declare PAL function
extern "C" uint32_t PAL_wcstoul(const WCHAR* nptr, WCHAR** endptr, int base);
extern "C" uint64_t PAL__wcstoui64(const WCHAR* nptr, WCHAR** endptr, int base);
extern "C" double PAL_wcstod(const WCHAR* nptr, WCHAR** endptr);

uint32_t u16_strtoul(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return PAL_wcstoul(nptr, endptr, base);
}

uint64_t u16_strtoui64(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return PAL__wcstoui64(nptr, endptr, base);
}

double u16_strtod(const WCHAR* nptr, WCHAR** endptr)
{
    return PAL_wcstod(nptr, endptr);
}