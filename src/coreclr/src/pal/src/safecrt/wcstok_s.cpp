// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*wcstok_s.c - tokenize a wide-character string with given delimiters
*

*
*Purpose:
*   defines wcstok_s() - breaks wide-character string into series of token
*   via repeated calls.
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define _FUNC_PROLOGUE
#define _FUNC_NAME wcstok_s
#define _CHAR wchar_t

#include "tcstok_s.inl"
