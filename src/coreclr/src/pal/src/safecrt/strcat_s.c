//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*strcat_s.c - contains strcat_s()
*

*
*Purpose:
*   strcat_s() concatenates (appends) a copy of the source string to the
*   end of the destination string.
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
#define _FUNC_NAME strcat_s
#define _CHAR char
#define _DEST _Dst
#define _SIZE _SizeInBytes
#define _SRC _Src

#include "tcscat_s.inl"
