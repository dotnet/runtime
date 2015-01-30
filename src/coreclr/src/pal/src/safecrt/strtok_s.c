//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*strtok_s.c - tokenize a string with given delimiters
*

*
*Purpose:
*   defines strtok_s() - breaks string into series of token
*   via repeated calls.
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#define _FUNC_PROLOGUE
#define _FUNC_NAME strtok_s
#define _CHAR char

#include "tcstok_s.inl"
