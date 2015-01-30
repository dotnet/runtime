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

#define _SECURE_ITOA

#define _UNICODE
#define TCHAR wchar_t
#define _T(x)       L##x
#include "xtox_s.inl"
