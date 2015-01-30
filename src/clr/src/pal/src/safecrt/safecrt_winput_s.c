//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*safecrt_winput_s.c - implementation of the _winput family for safecrt.lib
*

*
*Purpose:
*       This file contains the implementation of the _winput family for safecrt.lib.
*
*Revision History:
*       07/19/04  AC    Created
*
****/


#ifndef _UNICODE   /* CRT flag */
#define _UNICODE 1
#endif

#ifndef UNICODE    /* NT flag */
#define UNICODE 1
#endif

#define _SAFECRT_IMPL
#define _SECURE_SCANF

#include "pal/palinternal.h"
#include <string.h>
#include <errno.h>
#include <limits.h>
#include <stdlib.h>
#include <locale.h>
#include <stdarg.h>

#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define _TCHAR CRT_TCHAR
#define TCHAR CRTTCHAR

typedef wchar_t         _TCHAR;
typedef wchar_t         TCHAR;
typedef wchar_t         _TUCHAR;
#define _T(x)       x
#define _TEOF       WEOF

#define _gettc_nolock(x)        _getwc_nolock(x)
#define _ungettc_nolock(x,y)    _ungetwc_nolock(x,y)

#include "input.inl"

