// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*safecrt_woutput_s.c - implementation of the _woutput family for safercrt.lib
*

*
*Purpose:
*       This file contains the implementation of the _woutput family for safercrt.lib.
*
*Revision History:
*   07-08-04   SJ   Stub module created.
*   07-13-04   AC   Added support for floating-point types.
*   07-29-04   AC   Added macros for a safecrt version of mctowc and wctomb, which target ntdll.dll or msvcrt.dll
*                   based on the _NTSUBSET_ #define
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
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#ifndef _UNICODE    /* CRT flag */
#define _UNICODE 1
#endif

#ifndef UNICODE     /* NT flag */
#define UNICODE 1
#endif

#define FORMAT_VALIDATIONS
#if defined(_NTSUBSET_)
#define _MBTOWC _safecrt_mbtowc
#endif
#define _WCTOMB_S _safecrt_wctomb_s
#define _CFLTCVT _safecrt_cfltcvt
#define _CLDCVT _safecrt_cldcvt

#define _TCHAR CRT_TCHAR
#define TCHAR CRTTCHAR

typedef wchar_t     _TCHAR;
typedef wchar_t     TCHAR;
#define _T(x)       L##x

#include "output.inl"
