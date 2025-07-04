// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "strings.h"
#include <errno.h>
#include <string.h>
#include <stdio.h>
#include <stdarg.h>

#ifdef HOST_WINDOWS
#include <wchar.h>
#endif

/**
* @see strings.h
*/
size_t minipal_u16_strlen(const CHAR16_T* str)
{
#ifdef HOST_WINDOWS
    return wcslen((const wchar_t*)str);
#else
    size_t len = 0;
    while (*str++)
    {
        len++;
    }
    return len;
#endif
}

/**
* @see strings.h
*/
int minipal_sprintf_s(char* buffer, size_t count, const char* format, ...)
{
    va_list args;
    va_start(args, format);

#if HAVE_VSPRINTF_S
    int result = vsprintf_s(buffer, count, format, args);
#else
    int result = vsnprintf(buffer, count, format, args);
#endif

    va_end(args);
    return result;
}

/**
* @see strings.h
*/
int minipal_strncasecmp(const char* str1, const char* str2, size_t count)
{
#if HAVE_STRNCASECMP
    return strncasecmp(str1, str2, count);
#elif HOST_WINDOWS
    return _strnicmp(str1, str2, count);
#else
    if (str1 == NULL || str2 == NULL || count == 0)
    {
        return 0;
    }

    for (size_t i = 0; i < count; ++i)
    {
        char c1 = str1[i];
        char c2 = str2[i];
        if (c1 == '\0' || c2 == '\0')
        {
            return (unsigned char)c1 - (unsigned char)c2;
        }

        if (c1 >= 'A' && c1 <= 'Z')
        {
            c1 += ('a' - 'A');
        }

        if (c2 >= 'A' && c2 <= 'Z')
        {
            c2 += ('a' - 'A');
        }

        if (c1 != c2)
        {
            return (unsigned char)c1 - (unsigned char)c2;
        }
    }

    return 0;
#endif
}

/**
* @see strings.h
*/
char * minipal_strdup(const char* str)
{
#ifdef HOST_WINDOWS
    return _strdup(str);
#else
    return strdup(str);
#endif
}

/**
* @see strings.h
*/
int minipal_strcpy_s(char* dest, size_t destsz, const char* src)
{
#if HAVE_STRCPY_S
    return strcpy_s(dest, destsz, src);
#else
    if (dest == NULL || src == NULL || destsz == 0)
    {
        if (dest && destsz > 0)
        {
            dest[0] = '\0';
        }

        return EINVAL;
    }

    size_t src_len = strlen(src);
    if (src_len + 1 > destsz)
    {
        dest[0] = '\0';
        return ERANGE;
    }

    memcpy(dest, src, src_len + 1);
    return 0;
#endif
}

/**
* @see strings.h
*/
int minipal_strncpy_s(char* dest, size_t destsz, const char* src, size_t count)
{
#if HAVE_STRNCPY_S
    return strncpy_s(dest, destsz, src, count);
#else
    if (dest == NULL || src == NULL || destsz == 0)
    {
        if (dest && destsz > 0)
        {
            dest[0] = '\0';
        }

        return EINVAL;
    }

    if (count >= destsz)
    {
        dest[0] = '\0';
        return ERANGE;
    }

    size_t i = 0;
    for (; i < count && i < destsz - 1 && src[i] != '\0'; ++i)
    {
        dest[i] = src[i];
    }

    dest[i] = '\0';
    return 0;
#endif
}

/**
* @see strings.h
*/
int minipal_strcat_s(char* dest, size_t destsz, const char* src)
{
#if HAVE_STRCAT_S
    return strcat_s(dest, destsz, src);
#else
    if (dest == NULL || src == NULL || destsz == 0)
    {
        if (dest && destsz > 0)
        {
            dest[0] = '\0';
        }

        return EINVAL;
    }

    size_t dest_len = 0;
    for (; dest_len < destsz; ++dest_len)
    {
        if (dest[dest_len] == '\0')
        {
            break;
        }
    }

    if (dest_len == destsz)
    {
        dest[0] = '\0';
        return ERANGE;
    }

    size_t src_len = strlen(src);
    if (dest_len + src_len + 1 > destsz)
    {
        dest[0] = '\0';
        return ERANGE;
    }

    memcpy(dest + dest_len, src, src_len + 1);
    return 0;
#endif
}
