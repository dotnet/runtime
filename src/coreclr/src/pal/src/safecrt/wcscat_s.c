//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*wcscat_s.c - contains wcscat_s()
*

*
*Purpose:
*   wcscat_s() appends one wchar_t string onto another.
*
*   wcscat() concatenates (appends) a copy of the source string to the
*   end of the destination string.
*   Strings are wide-character strings.
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
#define _FUNC_NAME wcscat_s
#define _CHAR wchar_t
#define _DEST _Dst
#define _SIZE _SizeInBytes
#define _SRC _Src

#include "tcscat_s.inl"
