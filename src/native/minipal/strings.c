// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "strings.h"
#include <errno.h>
#include <string.h>

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
