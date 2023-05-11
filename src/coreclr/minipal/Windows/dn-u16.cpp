// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <wchar.h>

#include <dn-u16.h>

size_t dn_wcslen(const WCHAR* str)
{
    if (str == NULL)
        return 0;
    return ::wcslen(str);
}

int dn_wcscmp(const WCHAR* str1, const WCHAR* str2)
{
    return ::wcscmp(str1, str2);
}

int dn_wcsncmp(const WCHAR* str1, const WCHAR* str2, size_t count)
{
    return ::wcsncmp(str1, str2, count);
}

WCHAR* dn_wcscpy(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (0 != ::wcscpy_s(dst, dstLen, src))
        return nullptr;
    return dst;
}

WCHAR* dn_wcscat(WCHAR* dst, size_t dstLen, const WCHAR* src)
{
    if (0 != ::wcscat_s(dst, dstLen, src))
        return nullptr;
    return dst;
}

WCHAR* dn_wcsncpy(WCHAR* dst, size_t dstLen, const WCHAR* src, size_t count)
{
    if (0 != ::wcsncpy_s(dst, dstLen, src, count))
        return nullptr;
    return dst;
}

const WCHAR* dn_wcsstr(const WCHAR *str, const WCHAR *strCharSet)
{
    return ::wcsstr(str, strCharSet);
}

const WCHAR* dn_wcschr(const WCHAR* str, WCHAR ch)
{
    return ::wcschr(str, ch);
}

const WCHAR* dn_wcsrchr(const WCHAR* str, WCHAR ch)
{
    return ::wcsrchr(str, ch);
}

uint32_t dn_wcstoul(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return ::wcstoul(nptr, endptr, base);
}

uint64_t dn_wcstoui64(const WCHAR* nptr, WCHAR** endptr, int base)
{
    return ::_wcstoui64(nptr, endptr, base);
}

double dn_wcstod(const WCHAR* nptr, WCHAR** endptr)
{
    return ::wcstod(nptr, endptr);
}