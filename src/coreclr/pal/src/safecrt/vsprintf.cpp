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

typedef int (*OUTPUTFN)(miniFILE *, const char *, va_list);

static int _vsnprintf_helper( OUTPUTFN outfn, char *string, size_t count, const char *format, va_list ap );
static int _vscprintf_helper ( OUTPUTFN outfn, const char *format, va_list ap);

/***
*ifndef _COUNT_
*int vsprintf(string, format, ap) - print formatted data to string from arg ptr
*else
*int _vsnprintf(string, cnt, format, ap) - print formatted data to string from arg ptr
*endif
*
*Purpose:
*       Prints formatted data, but to a string and gets data from an argument
*       pointer.
*       Sets up a FILE so file i/o operations can be used, make string look
*       like a huge buffer to it, but _flsbuf will refuse to flush it if it
*       fills up. Appends '\0' to make it a true string.
*
*       Allocate the 'fake' _iob[] entryit statically instead of on
*       the stack so that other routines can assume that _iob[] entries are in
*       are in DGROUP and, thus, are near.
*
*ifdef _COUNT_
*       The _vsnprintf() flavor takes a count argument that is
*       the max number of bytes that should be written to the
*       user's buffer.
*endif
*
*       Multi-thread: (1) Since there is no stream, this routine must never try
*       to get the stream lock (i.e., there is no stream lock either).  (2)
*       Also, since there is only one statically allocated 'fake' iob, we must
*       lock/unlock to prevent collisions.
*
*Entry:
*       char *string - place to put destination string
*ifdef _COUNT_
*       size_t count - max number of bytes to put in buffer
*endif
*       char *format - format string, describes format of data
*       va_list ap - varargs argument pointer
*
*Exit:
*       returns number of characters in string
*       returns -2 if the string has been truncated (only in _vsnprintf_helper)
*       returns -1 in other error cases
*
*Exceptions:
*
*******************************************************************************/

int __cdecl _vsnprintf_helper (
        OUTPUTFN outfn,
        char *string,
        size_t count,
        const char *format,
        va_list ap
        )
{
        miniFILE str;
        miniFILE *outfile = &str;
        int retval;

        _VALIDATE_RETURN( (format != NULL), EINVAL, -1);

        _VALIDATE_RETURN( (count == 0) || (string != NULL), EINVAL, -1 );

        if(count>INT_MAX)
        {
            /* old-style functions allow any large value to mean unbounded */
            outfile->_cnt = INT_MAX;
        }
        else
        {
            outfile->_cnt = (int)count;
        }

        outfile->_flag = _IOWRT|_IOSTRG;
        outfile->_ptr = outfile->_base = string;

        retval = outfn(outfile, format, ap );

        if ( string==NULL)
            return(retval);

        if((retval >= 0) && (_putc_nolock('\0',outfile) != EOF))
            return(retval);

        string[count - 1] = 0;

        if (outfile->_cnt < 0)
        {
            /* the buffer was too small; we return -2 to indicate truncation */
            return -2;
        }
        return -1;
}

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

    retvalue = _vsnprintf_helper(_output_s, string, sizeInBytes, format, ap);
    if (retvalue < 0)
    {
        string[0] = 0;
        _SECURECRT__FILL_STRING(string, sizeInBytes, 1);
    }
    if (retvalue == -2)
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
        retvalue = _vsnprintf_helper(_output_s, string, count + 1, format, ap);
        if (retvalue == -2)
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
        retvalue = _vsnprintf_helper(_output_s, string, sizeInBytes, format, ap);
        string[sizeInBytes - 1] = 0;
        /* we allow truncation if count == _TRUNCATE */
        if (retvalue == -2 && count == _TRUNCATE)
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
        string[0] = 0;
        _SECURECRT__FILL_STRING(string, sizeInBytes, 1);
        if (retvalue == -2)
        {
            _VALIDATE_RETURN(("Buffer too small" && 0), ERANGE, -1);
        }
        return -1;
    }

    _SECURECRT__FILL_STRING(string, sizeInBytes, retvalue + 1);

    return (retvalue < 0 ? -1 : retvalue);
}

/***
* _vscprintf() - counts the number of character needed to print the formatted
* data
*
*Purpose:
*       Counts the number of characters in the fotmatted data.
*
*Entry:
*       char *format - format string, describes format of data
*       va_list ap - varargs argument pointer
*
*Exit:
*       returns number of characters needed to print formatted data.
*
*Exceptions:
*
*******************************************************************************/

#ifndef _COUNT_

int __cdecl _vscprintf_helper (
        OUTPUTFN outfn,
        const char *format,
        va_list ap
        )
{
        miniFILE str;
        miniFILE *outfile = &str;
        int retval;

        _VALIDATE_RETURN( (format != NULL), EINVAL, -1);

        outfile->_cnt = INT_MAX; //MAXSTR;
        outfile->_flag = _IOWRT|_IOSTRG;
        outfile->_ptr = outfile->_base = NULL;

        retval = outfn(outfile, format, ap);
        return(retval);
}

int __cdecl _vscprintf (
        const char *format,
        va_list ap
        )
{
        return _vscprintf_helper(_output, format, ap);
}

#endif  /* _COUNT_ */
