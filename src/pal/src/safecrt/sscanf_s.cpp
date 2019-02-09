// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
typedef int (*WINPUTFN)(miniFILE *, const wchar_t*, va_list);
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

static int __cdecl vnscan_fn (
        INPUTFN inputfn,
        const char *string,
        size_t count,
        const char *format,
        va_list arglist
        )
{
        miniFILE str;
        miniFILE *infile = &str;
        int retval;
        size_t length = strlen(string);

        _VALIDATE_RETURN( (string != NULL), EINVAL, EOF);
        _VALIDATE_RETURN( (format != NULL), EINVAL, EOF);

        infile->_flag = _IOREAD|_IOSTRG|_IOMYBUF;
        infile->_ptr = infile->_base = (char *) string;

        if ( count > length )
        {
            count = length;
        }

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
        const wchar_t *string,
        const wchar_t *format,
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

        if(count>(INT_MAX/sizeof(wchar_t)))
        {
            /* old-style functions allow any large value to mean unbounded */
            infile->_cnt = INT_MAX;
        }
        else
        {
            infile->_cnt = (int)count*sizeof(wchar_t);
        }

        retval = (inputfn(infile, format, arglist));

        return(retval);
}

static int __cdecl vnwscan_fn (
        WINPUTFN inputfn,
        const wchar_t *string,
        size_t count,
        const wchar_t *format,
        va_list arglist
        )
{
        miniFILE str;
        miniFILE *infile = &str;
        int retval;
        size_t length = PAL_wcsnlen(string, INT_MAX);

        _VALIDATE_RETURN( (string != NULL), EINVAL, EOF);
        _VALIDATE_RETURN( (format != NULL), EINVAL, EOF);

        infile->_flag = _IOREAD|_IOSTRG|_IOMYBUF;
        infile->_ptr = infile->_base = (char *) string;

        if ( count > length )
        {
            count = length;
        }

        if(count>(INT_MAX/sizeof(wchar_t)))
        {
            /* old-style functions allow any large value to mean unbounded */
            infile->_cnt = INT_MAX;
        }
        else
        {
            infile->_cnt = (int)count*sizeof(wchar_t);
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

int __cdecl _snscanf_s (
        const char *string,
        size_t count,
        const char *format,
        ...
        )
{
        int ret;
        va_list arglist;
        va_start(arglist, format);
        ret = vnscan_fn(__tinput_s, string, count, format, arglist);
        va_end(arglist);
        return ret;
}

int __cdecl swscanf_s (
        const wchar_t *string,
        const wchar_t *format,
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

int __cdecl _snwscanf_s (
        const wchar_t *string,
        size_t count,
        const wchar_t *format,
        ...
        )
{
        int ret;
        va_list arglist;
        va_start(arglist, format);
        ret = vnwscan_fn(__twinput_s, string, count, format, arglist);
        va_end(arglist);
        return ret;
}

