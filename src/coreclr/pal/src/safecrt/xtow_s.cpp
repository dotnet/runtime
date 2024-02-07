// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*xtow_s.c - convert integers to UTF-16 strings
*

*
*Purpose:
*   defines _*tox_s() functions - convert integers to UTF-16 strings
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
