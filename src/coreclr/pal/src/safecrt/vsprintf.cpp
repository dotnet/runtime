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

#if defined(__OpenBSD__)
// OpenBSD's libc doesn't support the legacy %C/%S wide specifiers (glibc and the
// other BSDs do). Rewrite them to %lc/%ls. Returns a malloc'd copy, or NULL on
// failure (callers fall back to the original format).
static char *PAL_TranslateWideFormatForOpenBSD(const char *format)
{
    size_t length = strlen(format);
    // Each rewrite adds one 'l', so doubling is always enough.
    char *translated = (char *)malloc(length * 2 + 1);
    if (translated == NULL)
    {
        return NULL;
    }

    const char *in = format;
    char *out = translated;
    while (*in != '\0')
    {
        if (*in != '%')
        {
            *out++ = *in++;
            continue;
        }

        *out++ = *in++;
        if (*in == '%')
        {
            *out++ = *in++; // literal "%%"
            continue;
        }

        // Skip flags, width, precision and length modifiers.
        while (*in != '\0' && strchr("-+ #0123456789.*hlLwIqjzt", *in) != NULL)
        {
            *out++ = *in++;
        }

        if (*in == 'C')
        {
            *out++ = 'l';
            *out++ = 'c';
            in++;
        }
        else if (*in == 'S')
        {
            *out++ = 'l';
            *out++ = 's';
            in++;
        }
        else if (*in != '\0')
        {
            *out++ = *in++;
        }
    }
    *out = '\0';

    return translated;
}

static int PAL_safecrt_vsnprintf(char *string, size_t count, const char *format, va_list ap)
{
    char *translated = PAL_TranslateWideFormatForOpenBSD(format);
    int retvalue = vsnprintf(string, count, translated != NULL ? translated : format, ap);
    free(translated);
    return retvalue;
}

#define PAL_SAFECRT_VSNPRINTF PAL_safecrt_vsnprintf
#else // !defined(__OpenBSD__)
#define PAL_SAFECRT_VSNPRINTF vsnprintf
#endif // defined(__OpenBSD__)

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

    retvalue = PAL_SAFECRT_VSNPRINTF(string, sizeInBytes, format, ap);
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
        retvalue = PAL_SAFECRT_VSNPRINTF(string, count + 1, format, ap);
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
        retvalue = PAL_SAFECRT_VSNPRINTF(string, sizeInBytes, format, ap);
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
