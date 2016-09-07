// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*wsplitpath_s.c - break down path name into components
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
#define _FUNC_NAME _wsplitpath_s
#define _CHAR wchar_t
#define _TCSNCPY_S wcsncpy_s
#define _T(_Character) L##_Character
#define _MBS_SUPPORT 0

#include "tsplitpath_s.inl"
