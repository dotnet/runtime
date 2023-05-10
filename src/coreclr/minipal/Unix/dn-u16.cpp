// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

typedef char16_t WCHAR;

#include <dn-u16.h>
#include <string.h>

size_t dn_wcslen(const WCHAR* str)
{
    if (str == NULL)
        return 0;

    size_t nChar = 0;
    while (*str++)
        nChar++;

    return nChar;
}

int dn_wcscmp(const WCHAR* str1, const WCHAR* str2)
{
    return dn_wcsncmp(str1, str2, 0x7fffffff);
}

int dn_wcsncmp(const WCHAR* str1, const WCHAR* str2, size_t count)
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

WCHAR* dn_wcscat(WCHAR* dst, const WCHAR* src)
{
    if (dst == NULL || src == NULL)
    {
        return NULL;
    }

    WCHAR* start = dst;

    // find end of source string
    while (*dst)
    {
        dst++;
    }

    // concatenate new string
    size_t srcLength = dn_wcslen(src);
    size_t loopCount = 0;
    while (*src && loopCount < srcLength)
    {
        *dst++ = *src++;
        loopCount++;
    }

    // add terminating null
    *dst = (WCHAR)'\0';
    return start;
}

WCHAR* dn_wcscpy(WCHAR* dst, const WCHAR* src)
{
    if (dst == NULL || src == NULL)
    {
        return NULL;
    }

    WCHAR* start = dst;

    // copy source string to destination string
    while (*src)
    {
        *dst++ = *src++;
    }

    // add terminating null
    *dst = (WCHAR)'\0';
    return start;
}

WCHAR* dn_wcsncpy(WCHAR* dst, const WCHAR* src, size_t count)
{
    size_t length = sizeof(WCHAR) * count;

    memset(dst, 0, length);
    size_t srcLength = dn_wcslen(src);
    length = (count < srcLength) ? count : srcLength;
    memcpy(dst, src, length * sizeof(WCHAR));
    return dst;
}

const WCHAR* dn_wcsstr(const WCHAR *str, const WCHAR *strCharSet)
{
    if (str == NULL || strCharSet == NULL)
    {
        return NULL;
    }

    // No characters to examine
    if (dn_wcslen(strCharSet) == 0)
        return str;

    const WCHAR* ret = NULL;
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
                ret = NULL;
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

WCHAR* dn_wcschr(const WCHAR* str, WCHAR ch)
{
    while (*str)
    {
        if (*str == ch)
            return (WCHAR*)str;
        str++;
    }

    // Check if the comparand was \000
    if (*str == ch)
        return (WCHAR*)str;

    return NULL;
}

WCHAR* dn_wcsrchr(const WCHAR* str, WCHAR ch)
{
    WCHAR* last = NULL;
    while (*str)
    {
        if (*str == ch)
            last = (WCHAR*)str;
        str++;
    }

    return last;
}

// Forward declare PAL function
extern "C" uint32_t PAL_wcstoul(const WCHAR* nptr, WCHAR** endptr, int base);
extern "C" uint64_t PAL__wcstoui64(const WCHAR* nptr, WCHAR** endptr, int base);
extern "C" double PAL_wcstod(const WCHAR* nptr, WCHAR** endptr);

uint32_t dn_wcstoul(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return PAL_wcstoul(nptr, endptr, base);
}

uint64_t dn_wcstoui64(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return PAL__wcstoui64(nptr, endptr, base);
}

double dn_wcstod(const WCHAR* nptr, WCHAR** endptr)
{
    return PAL_wcstod(nptr, endptr);
}