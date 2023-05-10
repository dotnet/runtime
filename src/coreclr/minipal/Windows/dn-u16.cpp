// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <dn-u16.h>
#include <wchar.h>

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

WCHAR* dn_wcscpy(WCHAR* dst, const WCHAR* src)
{
    return ::wcscpy(dst, src);
}

WCHAR* dn_wcscat(WCHAR* dst, const WCHAR* src)
{
    return ::wcscat(dst, src);
}

WCHAR* dn_wcsncpy(WCHAR* dst, const WCHAR* src, size_t count)
{
    return ::wcsncpy(dst, src, count);
}

const WCHAR* dn_wcsstr(const WCHAR *str, const WCHAR *strCharSet)
{
    return ::wcsstr(str, strCharSet);
}

WCHAR* dn_wcschr(const WCHAR* str, WCHAR ch)
{
    return ::wcschr(str, ch);
}

WCHAR* dn_wcsrchr(const WCHAR* str, WCHAR ch)
{
    return ::wcsrchr(str, ch);
}

ULONG dn_wcstoul(const WCHAR* nptr, WCHAR** endptr, int base)
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