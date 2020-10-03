// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*makepath_s.c - create path name from components
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
#define _FUNC_NAME _makepath_s
#define _CHAR char
#define _DEST _Dst
#define _SIZE _SizeInBytes
#define _T(_Character) _Character

#include "tmakepath_s.inl"
