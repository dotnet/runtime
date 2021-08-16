// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*safecrt_output_s.c - implementation of the _output family for safercrt.lib
*

*
*Purpose:
*       This file contains the implementation of the _output family for safercrt.lib.
*
*Revision History:
*       07-08-04   SJ   Stub module created.
*       07-13-04   AC   Added support for floating-point types.
*       07-29-04   AC   Added macros for a safecrt version of mctowc and wctomb, which target ntdll.dll or msvcrt.dll
*                       based on the _NTSUBSET_ #define
*       09-24-04  MSL   Prefix disallow NULL deref
*
****/

#define _SAFECRT_IMPL

#define __STDC_LIMIT_MACROS
#include "pal/palinternal.h"
#include <string.h>
#include <errno.h>
#include <limits.h>
#include <stdlib.h>
#include <stdarg.h>
#include <inttypes.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define FORMAT_VALIDATIONS
#define _CFLTCVT _safecrt_cfltcvt

#define _TCHAR CRT_TCHAR
#define TCHAR CRTTCHAR

typedef char        _TCHAR;
typedef char        TCHAR;
#define _T(x)       x

#include "output.inl"
