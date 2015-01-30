//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*safecrt_input_s.c - implementation of the _input family for safecrt.lib
*

*
*Purpose:
*       This file contains the implementation of the _input family for safecrt.lib.
*
*Revision History:
*       07/19/04  AC    Created
*
****/

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

typedef char            _TCHAR;
typedef char            TCHAR;
typedef unsigned char   _TUCHAR;
#define _T(x)       x
#define _TEOF       EOF

#define _gettc_nolock(x)        _getc_nolock(x)
#define _ungettc_nolock(x,y)    _ungetc_nolock(x,y)

#include "input.inl"
