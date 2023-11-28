// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <wchar.h>

#include <dn-u16.h>

size_t u16_strlen(const WCHAR* str)
{
    return ::wcslen(str);
}

int u16_strcmp(const WCHAR* str1, const WCHAR* str2)
{
    return ::wcscmp(str1, str2);
}

int u16_strncmp(const WCHAR* str1, const WCHAR* str2, size_t count)
{
    return ::wcsncmp(str1, str2, count);
}

WCHAR* u16_strcpy_s(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (0 != ::wcscpy_s(dst, dstLen, src))
        return nullptr;
    return dst;
}

WCHAR* u16_strcat_s(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (0 != ::wcscat_s(dst, dstLen, src))
        return nullptr;
    return dst;
}

WCHAR* u16_strncpy_s(WCHAR* dst, size_t dstLen, const WCHAR* src, size_t count)
{
    if (0 != ::wcsncpy_s(dst, dstLen, src, count))
        return nullptr;
    return dst;
}

const WCHAR* u16_strstr(const WCHAR *str, const WCHAR *strCharSet)
{
    return ::wcsstr(str, strCharSet);
}

const WCHAR* u16_strchr(const WCHAR* str, WCHAR ch)
{
    return ::wcschr(str, ch);
}

const WCHAR* u16_strrchr(const WCHAR* str, WCHAR ch)
{
    return ::wcsrchr(str, ch);
}

uint32_t u16_strtoul(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return ::wcstoul(nptr, endptr, base);
}

uint64_t u16_strtoui64(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return ::_wcstoui64(nptr, endptr, base);
}

double u16_strtod(const WCHAR* nptr, WCHAR** endptr)
{
    return ::wcstod(nptr, endptr);
}