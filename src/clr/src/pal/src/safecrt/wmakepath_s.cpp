// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*wmakepath_s.c - create path name from components
*

*
*Purpose:
*   To provide support for creation of full path names from components
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define _FUNC_PROLOGUE
#define _FUNC_NAME _wmakepath_s
#define _CHAR char16_t
#define _DEST _Dst
#define _SIZE _SizeInWords
#define _T(_Character) L##_Character
#define _MBS_SUPPORT 0

#include "tmakepath_s.inl"
