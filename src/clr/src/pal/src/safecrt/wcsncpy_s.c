//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*wcsncpy_s.c - copy at most n characters of wide-character string
*

*
*Purpose:
*   defines wcsncpy_s() - copy at most n characters of wchar_t string
*
*******************************************************************************/

#define _SECURECRT_FILL_BUFFER 1
#define _SECURECRT_FILL_BUFFER_THRESHOLD ((size_t)8)

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define _FUNC_PROLOGUE
#define _FUNC_NAME wcsncpy_s
#define _CHAR wchar_t
#define _DEST _Dst
#define _SIZE _SizeInWords
#define _SRC _Src
#define _COUNT _Count

#include "tcsncpy_s.inl"

