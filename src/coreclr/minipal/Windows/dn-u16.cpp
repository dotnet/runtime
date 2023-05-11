// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <wchar.h>

#include <dn-u16.h>

size_t strlen_u16(const WCHAR* str)
{
    return ::wcslen(str);
}

int strcmp_u16(const WCHAR* str1, const WCHAR* str2)
{
    return ::wcscmp(str1, str2);
}

int strncmp_u16(const WCHAR* str1, const WCHAR* str2, size_t count)
{
    return ::wcsncmp(str1, str2, count);
}

WCHAR* strcpy_u16(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (0 != ::wcscpy_s(dst, dstLen, src))
        return nullptr;
    return dst;
}

WCHAR* strcat_u16(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (0 != ::wcscat_s(dst, dstLen, src))
        return nullptr;
    return dst;
}

WCHAR* strncpy_u16(WCHAR* dst, size_t dstLen, const WCHAR* src, size_t count)
{
    if (0 != ::wcsncpy_s(dst, dstLen, src, count))
        return nullptr;
    return dst;
}

const WCHAR* strstr_u16(const WCHAR *str, const WCHAR *strCharSet)
{
    return ::wcsstr(str, strCharSet);
}

const WCHAR* strchr_u16(const WCHAR* str, WCHAR ch)
{
    return ::wcschr(str, ch);
}

const WCHAR* strrchr_u16(const WCHAR* str, WCHAR ch)
{
    return ::wcsrchr(str, ch);
}

uint32_t strtoul_u16(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return ::wcstoul(nptr, endptr, base);
}

uint64_t strtoui64_u16(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return ::_wcstoui64(nptr, endptr, base);
}

double strtod_u16(const WCHAR* nptr, WCHAR** endptr)
{
    return ::wcstod(nptr, endptr);
}