// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*vsprintf.c - print formatted data into a string from var arg list
*

*
*Purpose:
*       defines vsprintf(), _vsnprintf() and _vsnprintf_s() - print formatted output to
*       a string, get the data from an argument ptr instead of explicit
*       arguments.
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

#include <stdio.h>
#include <stdlib.h>

DLLEXPORT int __cdecl vsprintf_s (
        char *string,
        size_t sizeInBytes,
        const char *format,
        va_list ap
        )
{
    int retvalue = -1;

    /* validation section */
    _VALIDATE_RETURN(format != NULL, EINVAL, -1);
    _VALIDATE_RETURN(string != NULL && sizeInBytes > 0, EINVAL, -1);

    retvalue = vsnprintf(string, sizeInBytes, format, ap);
    if (retvalue < 0)
    {
        string[0] = '\0';
        _SECURECRT__FILL_STRING(string, sizeInBytes, 1);
    }
    if (retvalue > (int)sizeInBytes)
    {
        _VALIDATE_RETURN(("Buffer too small" && 0), ERANGE, -1);
    }
    if (retvalue >= 0)
    {
        _SECURECRT__FILL_STRING(string, sizeInBytes, retvalue + 1);
    }

    return retvalue;
}

DLLEXPORT int __cdecl _vsnprintf_s (
        char *string,
        size_t sizeInBytes,
        size_t count,
        const char *format,
        va_list ap
        )
{
    int retvalue = -1;
    errno_t save_errno = 0;

    /* validation section */
    _VALIDATE_RETURN(format != NULL, EINVAL, -1);
    if (count == 0 && string == NULL && sizeInBytes == 0)
    {
        /* this case is allowed; nothing to do */
        return 0;
    }
    _VALIDATE_RETURN(string != NULL && sizeInBytes > 0, EINVAL, -1);

    if (sizeInBytes > count)
    {
        save_errno = errno;
        retvalue = vsnprintf(string, count + 1, format, ap);
        if (retvalue > (int)(count + 1))
        {
            /* the string has been truncated, return -1 */
            _SECURECRT__FILL_STRING(string, sizeInBytes, count + 1);
            if (errno == ERANGE)
            {
                errno = save_errno;
            }
            return -1;
        }
    }
    else /* sizeInBytes <= count */
    {
        save_errno = errno;
        retvalue = vsnprintf(string, sizeInBytes, format, ap);
        string[sizeInBytes - 1] = '\0';
        /* we allow truncation if count == _TRUNCATE */
        if (retvalue >= (int)sizeInBytes && count == _TRUNCATE)
        {
            if (errno == ERANGE)
            {
                errno = save_errno;
            }
            return -1;
        }
    }

    if (retvalue < 0)
    {
        string[0] = '\0';
        _SECURECRT__FILL_STRING(string, sizeInBytes, 1);
        return -1;
    }

    _SECURECRT__FILL_STRING(string, sizeInBytes, retvalue + 1);

    return (retvalue < 0 ? -1 : retvalue);
}
