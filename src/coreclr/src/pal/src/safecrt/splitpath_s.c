//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*splitpath_s.c - break down path name into components
*

*
*Purpose:
*   To provide support for accessing the individual components of an
*   arbitrary path name
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define _FUNC_PROLOGUE
#define _FUNC_NAME _splitpath_s
#define _CHAR char
#define _TCSNCPY_S strncpy_s
#define _T(_Character) _Character

#define _MBS_SUPPORT 0

#include "tsplitpath_s.inl"
