// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>

//
// Wide character (UTF-16) abstraction layer.
//

size_t dn_wcslen(const WCHAR* str);
int dn_wcscmp(const WCHAR* str1, const WCHAR* str2);
int dn_wcsncmp(const WCHAR* str1, const WCHAR* str2, size_t count);
WCHAR* dn_wcscat(WCHAR* dst, const WCHAR* src);
WCHAR* dn_wcscpy(WCHAR* dst, const WCHAR* src);
WCHAR* dn_wcsncpy(WCHAR* dst, const WCHAR* src, size_t count);
const WCHAR* dn_wcsstr(const WCHAR *str, const WCHAR *strCharSet);
WCHAR* dn_wcschr(const WCHAR* str, WCHAR ch);
WCHAR* dn_wcsrchr(const WCHAR* str, WCHAR ch);
uint32_t dn_wcstoul(const WCHAR* nptr, WCHAR** endptr, int base);
uint64_t dn_wcstoui64(const WCHAR* nptr, WCHAR** endptr, int base);
double dn_wcstod(const WCHAR* nptr, WCHAR** endptr);