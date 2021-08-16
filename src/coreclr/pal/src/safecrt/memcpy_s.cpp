// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*memcpy_s.c - contains memcpy_s routine
*

*
*Purpose:
*       memcpy_s() copies a source memory buffer to a destination buffer.
*       Overlapping buffers are not treated specially, so propagation may occur.
*
*Revision History:
*       10-07-03  AC    Module created.
*       03-10-04  AC    Return ERANGE when buffer is too small
*       01-14-05  AC    Prefast (espx) fixes
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <assert.h>
#include "internal_securecrt.h"
#include "mbusafecrt_internal.h"

/***
*memcpy_s - Copy source buffer to destination buffer
*
*Purpose:
*       memcpy_s() copies a source memory buffer to a destination memory buffer.
*       This routine does NOT recognize overlapping buffers, and thus can lead
*       to propagation.
*
*       For cases where propagation must be avoided, memmove_s() must be used.
*
*Entry:
*       void *dst = pointer to destination buffer
*       size_t sizeInBytes = size in bytes of the destination buffer
*       const void *src = pointer to source buffer
*       size_t count = number of bytes to copy
*
*Exit:
*       Returns 0 if everything is ok, else return the error code.
*
*Exceptions:
*       Input parameters are validated. Refer to the validation section of the function.
*       On error, the error code is returned and the destination buffer is zeroed.
*
*******************************************************************************/

DLLEXPORT errno_t __cdecl memcpy_s(
    void * dst,
    size_t sizeInBytes,
    const void * src,
    size_t count
) THROW_DECL
{
    if (count == 0)
    {
        /* nothing to do */
        return 0;
    }

    /* validation section */
    _VALIDATE_RETURN_ERRCODE(dst != NULL, EINVAL);
    if (src == NULL || sizeInBytes < count)
    {
        /* zeroes the destination buffer */
        memset(dst, 0, sizeInBytes);

        _VALIDATE_RETURN_ERRCODE(src != NULL, EINVAL);
        _VALIDATE_RETURN_ERRCODE(sizeInBytes >= count, ERANGE);
        /* useless, but prefast is confused */
        return EINVAL;
    }

    UINT_PTR x = (UINT_PTR)dst, y = (UINT_PTR)src;
    assert((x + count <= y) || (y + count <= x));
    memcpy(dst, src, count);
    return 0;
}
