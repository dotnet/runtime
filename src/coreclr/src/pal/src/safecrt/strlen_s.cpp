// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*strlen_s.c - contains strnlen() routine
*

*
*Purpose:
*   strnlen returns the length of a null-terminated string,
*   not including the null byte itself, up to the specified max size
*
*******************************************************************************/


#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

/***
*strnlen - return the length of a null-terminated string
*
*Purpose:
*   Finds the length in bytes of the given string, not including
*   the final null character. Only the first maxsize characters
*   are inspected: if the null character is not found, maxsize is
*   returned.
*
*Entry:
*   const char * str - string whose length is to be computed
*   size_t maxsize
*
*Exit:
*   Length of the string "str", exclusive of the final null byte, or
*   maxsize if the null character is not found.
*
*Exceptions:
*
*******************************************************************************/

size_t __cdecl PAL_strnlen(const char *str, size_t maxsize)
{
    size_t n;

    /* Note that we do not check if str == NULL, because we do not
     * return errno_t...
     */

    for (n = 0; n < maxsize && *str; n++, str++)
        ;

    return n;
}

