// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*strcpy_s.c - contains wcscpy_s()
*

*
*Purpose:
*   wcscpy_s() copies one string onto another.
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
#define _FUNC_NAME wcscpy_s
#define _CHAR char16_t
#define _DEST _Dst
#define _SIZE _SizeInWords
#define _SRC _Src

#include "tcscpy_s.inl"

