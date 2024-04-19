// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*xtoa_s.c - convert integers to ASCII strings
*

*
*Purpose:
*   defines _*tox_s() functions - convert integers to ASCII strings
*
*******************************************************************************/
#include "pal/palinternal.h"
#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

//#define __int64 long long

#define _SECURE_ITOA

#define TCHAR char
#define _T(x)       x
#include "xtox_s.inl"
