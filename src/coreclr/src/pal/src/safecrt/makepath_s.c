//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

#define _MBS_SUPPORT 0

#include "tmakepath_s.inl"
