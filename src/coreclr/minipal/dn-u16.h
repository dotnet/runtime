// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stddef.h>
#include <stdint.h>

//
// Wide character (UTF-16) abstraction layer.
//

size_t strlen_u16(const WCHAR* str);
int strcmp_u16(const WCHAR* str1, const WCHAR* str2);
int strncmp_u16(const WCHAR* str1, const WCHAR* str2, size_t count);
WCHAR* strcat_u16(WCHAR* dst, size_t dstLen, const WCHAR* src);
WCHAR* strcpy_u16(WCHAR* dst, size_t dstLen, const WCHAR* src);
WCHAR* strncpy_u16(WCHAR* dst, size_t dstLen, const WCHAR* src, size_t count);
const WCHAR* strstr_u16(const WCHAR* str, const WCHAR* strCharSet);
const WCHAR* strchr_u16(const WCHAR* str, WCHAR ch);
const WCHAR* strrchr_u16(const WCHAR* str, WCHAR ch);
uint32_t strtoul_u16(const WCHAR* nptr, WCHAR** endptr, int base);
uint64_t strtoui64_u16(const WCHAR* nptr, WCHAR** endptr, int base);
double strtod_u16(const WCHAR* nptr, WCHAR** endptr);