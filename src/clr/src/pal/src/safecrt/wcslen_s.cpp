// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*wcslen_s.c - contains wcsnlen() routine
*

*
*Purpose:
*   wcslen returns the length of a null-terminated wide-character string,
*   not including the null wchar_t itself.
*
*******************************************************************************/


#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

/***
*wcsnlen - return the length of a null-terminated wide-character string
*
*Purpose:
*   Finds the length in bytes of the given string, not including
*   the final null character. Only the first maxsize characters
*   are inspected: if the null character is not found, maxsize is
*   returned.
*
*Entry:
*   const wchar_t * wcs - string whose length is to be computed
*   size_t maxsize
*
*Exit:
*   Length of the string "wcs", exclusive of the final null byte, or
*   maxsize if the null character is not found.
*
*Exceptions:
*
*******************************************************************************/

size_t __cdecl wcsnlen(const wchar_t *wcs, size_t maxsize)
{
    size_t n;

    /* Note that we do not check if s == NULL, because we do not
     * return errno_t...
     */

    for (n = 0; n < maxsize && *wcs; n++, wcs++)
        ;

    return n;
}

