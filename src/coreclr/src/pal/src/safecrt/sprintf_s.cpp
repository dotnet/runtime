// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*sprintf_s.c - print formatted to string
*

*
*Purpose:
*       defines sprintf_s() and _snprintf_s() - print formatted data to string
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"


/***
*ifndef _COUNT_
*int sprintf_s(string, format, ...) - print formatted data to string
*else
*int _snprintf_s(string, cnt, format, ...) - print formatted data to string
*endif
*
*Purpose:
*       Prints formatted data to the using the format string to
*       format data and getting as many arguments as called for
*       Sets up a FILE so file i/o operations can be used, make
*       string look like a huge buffer to it, but _flsbuf will
*       refuse to flush it if it fills up.  Appends '\0' to make
*       it a true string. _output does the real work here
*
*       Allocate the 'fake' _iob[] entry statically instead of on
*       the stack so that other routines can assume that _iob[]
*       entries are in are in DGROUP and, thus, are near.
*
*ifdef _COUNT_
*       The _snprintf_s() flavor takes a count argument that is
*       the max number of bytes that should be written to the
*       user's buffer.
*endif
*
*       Multi-thread: (1) Since there is no stream, this routine must
*       never try to get the stream lock (i.e., there is no stream
*       lock either). (2) Also, since there is only one statically
*       allocated 'fake' iob, we must lock/unlock to prevent collisions.
*
*Entry:
*       char *string - pointer to place to put output
*ifdef _COUNT_
*       size_t count - max number of bytes to put in buffer
*endif
*       char *format - format string to control data format/number
*       of arguments followed by list of arguments, number and type
*       controlled by format string
*
*Exit:
*       returns number of characters printed
*
*Exceptions:
*
*******************************************************************************/
DLLEXPORT int sprintf_s (
        char *string,
        size_t sizeInBytes,
        const char *format,
        ...
        )
{
        int ret;
        va_list arglist;
        va_start(arglist, format);
        ret = vsprintf_s(string, sizeInBytes, format, arglist);
        va_end(arglist);
        return ret;
}

DLLEXPORT int _snprintf_s (
        char *string,
        size_t sizeInBytes,
        size_t count,
        const char *format,
        ...
        )
{
        int ret;
        va_list arglist;
        va_start(arglist, format);
        ret = _vsnprintf_s(string, sizeInBytes, count, format, arglist);
        va_end(arglist);
        return ret;
}
