// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stddef.h>
#include <stdint.h>

//
// Wide character (UTF-16) abstraction layer.
//

size_t u16_strlen(const WCHAR* str);
int u16_strcmp(const WCHAR* str1, const WCHAR* str2);
int u16_strncmp(const WCHAR* str1, const WCHAR* str2, size_t count);
WCHAR* u16_strcat_s(WCHAR* dst, size_t dstLen, const WCHAR* src);
WCHAR* u16_strcpy_s(WCHAR* dst, size_t dstLen, const WCHAR* src);
WCHAR* u16_strncpy_s(WCHAR* dst, size_t dstLen, const WCHAR* src, size_t count);
const WCHAR* u16_strstr(const WCHAR* str, const WCHAR* strCharSet);
const WCHAR* u16_strchr(const WCHAR* str, WCHAR ch);
const WCHAR* u16_strrchr(const WCHAR* str, WCHAR ch);
uint32_t u16_strtoul(const WCHAR* nptr, WCHAR** endptr, int base);
uint64_t u16_strtoui64(const WCHAR* nptr, WCHAR** endptr, int base);
double u16_strtod(const WCHAR* nptr, WCHAR** endptr);