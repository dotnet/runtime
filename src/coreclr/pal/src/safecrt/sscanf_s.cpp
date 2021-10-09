// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*sscanf_s.c - read formatted data from string
*

*
*Purpose:
*       defines scanf() - reads formatted data from string
*
*******************************************************************************/

#include <string.h>
#include <errno.h>
#include <limits.h>
#include "internal_securecrt.h"

#include "mbusafecrt_internal.h"

typedef int (*INPUTFN)(miniFILE *, const unsigned char*, va_list);
typedef int (*WINPUTFN)(miniFILE *, const char16_t*, va_list);
extern size_t PAL_wcsnlen(const WCHAR* inString, size_t inMaxSize);

/***
*static int v[nw]scan_fn([w]inputfn, string, [count], format, ...)
*
*Purpose:
*       this is a helper function which is called by the other functions
*       in this file - sscanf/swscanf/snscanf etc. It calls either _(w)input or
*       _(w)input_s depending on the first parameter.
*
*******************************************************************************/

static int __cdecl vscan_fn (
        INPUTFN inputfn,
        const char *string,
        const char *format,
        va_list arglist
        )
{
        miniFILE str;
        miniFILE *infile = &str;
        int retval;
        size_t count = strlen(string);

        _VALIDATE_RETURN( (string != NULL), EINVAL, EOF);
        _VALIDATE_RETURN( (format != NULL), EINVAL, EOF);

        infile->_flag = _IOREAD|_IOSTRG|_IOMYBUF;
        infile->_ptr = infile->_base = (char *) string;

        if(count>(INT_MAX/sizeof(char)))
        {
            /* old-style functions allow any large value to mean unbounded */
            infile->_cnt = INT_MAX;
        }
        else
        {
            infile->_cnt = (int)count*sizeof(char);
        }

        retval = (inputfn(infile, ( const unsigned char* )format, arglist));

        return(retval);
}

static int __cdecl vwscan_fn (
        WINPUTFN inputfn,
        const char16_t *string,
        const char16_t *format,
        va_list arglist
        )
{
        miniFILE str;
        miniFILE *infile = &str;
        int retval;
        size_t count = PAL_wcsnlen(string, INT_MAX);

        _VALIDATE_RETURN( (string != NULL), EINVAL, EOF);
        _VALIDATE_RETURN( (format != NULL), EINVAL, EOF);

        infile->_flag = _IOREAD|_IOSTRG|_IOMYBUF;
        infile->_ptr = infile->_base = (char *) string;

        if(count>(INT_MAX/sizeof(char16_t)))
        {
            /* old-style functions allow any large value to mean unbounded */
            infile->_cnt = INT_MAX;
        }
        else
        {
            infile->_cnt = (int)count*sizeof(char16_t);
        }

        retval = (inputfn(infile, format, arglist));

        return(retval);
}

/***
*int sscanf_s(string, format, ...)
*   Same as sscanf above except that it calls _input_s to do the real work.
*
*int snscanf_s(string, size, format, ...)
*   Same as snscanf above except that it calls _input_s to do the real work.
*
*   _input_s has a size check for array parameters.
*
*******************************************************************************/

DLLEXPORT int __cdecl sscanf_s (
        const char *string,
        const char *format,
        ...
        )
{
        int ret;
        va_list arglist;
        va_start(arglist, format);
        ret = vscan_fn(__tinput_s, string, format, arglist);
        va_end(arglist);
        return ret;
}

int __cdecl swscanf_s (
        const char16_t *string,
        const char16_t *format,
        ...
        )
{
        int ret;
        va_list arglist;
        va_start(arglist, format);
        ret = vwscan_fn(__twinput_s, string, format, arglist);
        va_end(arglist);
        return ret;
}
