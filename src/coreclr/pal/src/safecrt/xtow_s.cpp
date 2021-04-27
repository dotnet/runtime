// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*strtok_s.c - tokenize a string with given delimiters
*

*
*Purpose:
*   defines strtok_s() - breaks string into series of token
*   via repeated calls.
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define _SECURE_ITOA

#define _UNICODE
#define TCHAR char16_t
#define _T(x)       L##x
#include "xtox_s.inl"
