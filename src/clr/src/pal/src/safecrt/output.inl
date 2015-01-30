//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*output.c - printf style output to a FILE
*

*
*Purpose:
*       This file contains the code that does all the work for the
*       printf family of functions.  It should not be called directly, only
*       by the *printf functions.  We don't make any assumtions about the
*       sizes of ints, longs, shorts, or long doubles, but if types do overlap,
*       we also try to be efficient.  We do assume that pointers are the same
*       size as either ints or longs.
*       If CPRFLAG is defined, defines _cprintf instead.
*       **** DOESN'T CURRENTLY DO MTHREAD LOCKING ****
*
*Note:
*       this file is included in safecrt.lib build directly, plese refer
*       to safecrt_[w]output_s.c
*
*******************************************************************************/


//typedef __int64_t __int64;


#define _SAFECRT_IMPL
#define FORMAT_VALIDATIONS

typedef double  _CRT_DOUBLE;

//typedef int* intptr_t;

/*
Buffer size required to be passed to _gcvt, fcvt and other fp conversion routines
*/
#define _CVTBUFSIZE (309+40) /* # of digits in max. dp value + slop */

/* temporary work-around for compiler without 64-bit support */
#ifndef _SAFECRT_IMPL
extern "C"
{
#endif  /* _SAFECRT_IMPL */
#ifndef _INTEGRAL_MAX_BITS
#define _INTEGRAL_MAX_BITS  64
#endif  /* _INTEGRAL_MAX_BITS */

#ifdef POSITIONAL_PARAMETERS
#define FORMAT_VALIDATIONS
#endif  /* POSITIONAL_PARAMETERS */

//#include <mtdll.h>
//#include <cruntime.h>
//#include <limits.h>
//#include <string.h>
//#include <stddef.h>
//#include <crtdefs.h>
//#include <stdio.h>
//#include <stdarg.h>
//#include <cvt.h>
//#include <conio.h>
//#include <internal.h>
//#include <fltintrn.h>
//#include <stdlib.h>
//#include <ctype.h>
//#include <dbgint.h>
//#include <setlocal.h>

#define _MBTOWC(x,y,z) _minimal_chartowchar( x, y )

#ifndef _WCTOMB_S
#define _WCTOMB_S wctomb_s
#endif  /* _WCTOMB_S */

#ifdef _SAFECRT_IMPL

#undef _malloc_crt
#define _malloc_crt malloc

#undef _free_crt
#define _free_crt free

/* Wrapper for _output_s so that we do not expose FILE in the _output_s signature.
 * Always ensure null-termination. Returns the number of written chars, not including the terminating null.
 * Returns -1 if something went wrong during the formatting (in _output_s), e.g. mbcs conversions.
 * Returns -2 if the string has been truncated.
 * _output_s calls _invalid_parameter (and returns -1, possibly) if the format string is malformed.
 */
#ifndef _UNICODE
int __cdecl _soutput_s(char *_Dst, size_t _Size, const char *_Format, va_list _ArgList)
#else  /* _UNICODE */
int __cdecl _swoutput_s(wchar_t *_Dst, size_t _Size, const wchar_t *_Format, va_list _ArgList)
#endif  /* _UNICODE */
{
    miniFILE stream;
    miniFILE *outfile = &stream;
    int written = -1;

    /* validation section */
#ifndef _UNICODE
    if(_Size==SIZE_MAX)
    {
        /* user is attempting to make us unbounded, but we don't fit that much */
        outfile->_cnt = INT_MAX;
    }
    else
    {
        _VALIDATE_RETURN(_Size <= INT_MAX, EINVAL, -1);
        outfile->_cnt = (int)_Size;
    }
    outfile->_ptr = outfile->_base = _Dst;
#else  /* _UNICODE */
    if(_Size==SIZE_MAX)
    {
        /* user is attempting to make us unbounded, but we don't fit that much */
        outfile->_cnt = INT_MAX;
    }
    else if(_Size>(INT_MAX/sizeof(wchar_t)))
    {
        /* we can't represent the amount of output the user asked for */
        _VALIDATE_RETURN( 0 /* FALSE */, EINVAL, -1 );
    }
    else
    {
        outfile->_cnt = (int)(_Size*sizeof(wchar_t));
    }
    outfile->_ptr = outfile->_base = (char*)_Dst;
#endif  /* _UNICODE */
    outfile->_flag = _IOWRT | _IOSTRG;

#ifndef _UNICODE
    written = _output_s(outfile, _Format, _ArgList);
#else  /* _UNICODE */
    written = _woutput_s(outfile, _Format, _ArgList);
#endif  /* _UNICODE */
    _Dst[_Size - 1] = 0;
    if (written < 0)
    {
        if (outfile->_cnt < 0)
        {
            /* the buffer was too small; we return -2 to indicate truncation */
            return -2;
        }
        /* otherwise, something else failed: we reset the string and we return */
        if (_Dst != NULL && _Size > 0)
        {
            *_Dst = 0;
        }
        return written;
    }

#ifndef _UNICODE
    if ((_putc_nolock('\0', outfile) != EOF))
#else  /* _UNICODE */
    if ((_putc_nolock('\0', outfile) != EOF) && (_putc_nolock('\0', outfile) != EOF))
#endif  /* _UNICODE */
    {
        return written;
    }
    /* the last putc failed, so it means there is not enough space in the buffer */
    return -2;
}

#endif  /* _SAFECRT_IMPL */

#ifndef _CFLTCVT
#define _CFLTCVT _cfltcvt
#endif  /* _CFLTCVT */

#ifndef _CLDCVT
#define _CLDCVT _cldcvt
#endif  /* _CLDCVT */

#ifdef _MBCS
#undef  _MBCS
#endif  /* _MBCS */
//#include <tchar.h>

/* this macro defines a function which is private and as fast as possible: */
/* for example, in C 6.0, it might be static _fastcall <type> near. */
#define LOCAL(x) static x __cdecl

/* int/long/short/pointer sizes */

/* the following should be set depending on the sizes of various types */
#if __LP64__
    #define LONG_IS_INT     0
    CASSERT(sizeof(long) > sizeof(int));
#else
    #define LONG_IS_INT     1       /* 1 means long is same size as int */
    CASSERT(sizeof(long) == sizeof(int));
#endif

#if (defined(__DARWIN_LONG_DOUBLE_IS_DOUBLE) && !__DARWIN_LONG_DOUBLE_IS_DOUBLE) || defined(__LINUX__)
    #ifdef MBUSAFECRT_SUPPORTS_LONGDOUBLE   // we do not have _cldcvt function to convert long double.
        #define LONGDOUBLE_IS_DOUBLE 0  /* 1 means long double is same as double */
        CASSERT(sizeof(long double) != sizeof(double));
    #else
        #define LONGDOUBLE_IS_DOUBLE 1
    #endif
#else
    #define LONGDOUBLE_IS_DOUBLE 1  /* 1 means long double is same as double */
    CASSERT(sizeof(long double) == sizeof(double));
#endif

#define SHORT_IS_INT     0      /* 1 means short is same size as int */
#define LONGDOUBLE_IS_DOUBLE 1  /* 1 means long double is same as double */
#define LONGLONG_IS_INT64 1     /* 1 means long long is same as int64 */
#if defined (_WIN64)
    #define PTR_IS_INT       0      /* 1 means ptr is same size as int */
    CASSERT(sizeof(void *) != sizeof(int));
    #if __LP64__
        #define PTR_IS_LONG      1      /* 1 means ptr is same size as long */
        CASSERT(sizeof(void *) == sizeof(long));
    #else
        #define PTR_IS_LONG      0      /* 1 means ptr is same size as long */
        CASSERT(sizeof(void *) != sizeof(long));
    #endif
    #define PTR_IS_INT64     1      /* 1 means ptr is same size as int64 */
    CASSERT(sizeof(void *) == sizeof(int64_t));
#else  /* defined (_WIN64) */
    #define PTR_IS_INT       1      /* 1 means ptr is same size as int */
    CASSERT(sizeof(void *) == sizeof(int));
    #define PTR_IS_LONG      1      /* 1 means ptr is same size as long */
    CASSERT(sizeof(void *) == sizeof(long));
    #define PTR_IS_INT64     0      /* 1 means ptr is same size as int64 */
    CASSERT(sizeof(void *) != sizeof(int64_t));
#endif  /* defined (_WIN64) */

#ifndef __GNUC_VA_LIST
#if LONGLONG_IS_INT64
    #define get_long_long_arg(x) (long long)get_int64_arg(x)
#endif  /* LONGLONG_IS_INT64 */

#if LONG_IS_INT
    #define get_long_arg(x) (long)get_int_arg(x)
#endif  /* LONG_IS_INT */

#if PTR_IS_INT
    #define get_ptr_arg(x) (void *)(intptr_t)get_int_arg(x)
#elif PTR_IS_LONG
    #define get_ptr_arg(x) (void *)(intptr_t)get_long_arg(x)
#elif PTR_IS_INT64
    #define get_ptr_arg(x) (void *)get_int64_arg(x)
#else  /* PTR_IS_INT64 */
    #error Size of pointer must be same as size of int or long
#endif  /* PTR_IS_INT64 */

#endif // __GNUC_VA_LIST

/* CONSTANTS */

/* size of conversion buffer (ANSI-specified minimum is 509) */

#define BUFFERSIZE    512
#define MAXPRECISION  BUFFERSIZE

#if BUFFERSIZE < _CVTBUFSIZE + 6
/*
 * Buffer needs to be big enough for default minimum precision
 * when converting floating point needs bigger buffer, and malloc
 * fails
 */
#error Conversion buffer too small for max double.
#endif  /* BUFFERSIZE < _CVTBUFSIZE + 6 */

/* flag definitions */
#define FL_SIGN       0x00001   /* put plus or minus in front */
#define FL_SIGNSP     0x00002   /* put space or minus in front */
#define FL_LEFT       0x00004   /* left justify */
#define FL_LEADZERO   0x00008   /* pad with leading zeros */
#define FL_LONG       0x00010   /* long value given */
#define FL_SHORT      0x00020   /* short value given */
#define FL_SIGNED     0x00040   /* signed data given */
#define FL_ALTERNATE  0x00080   /* alternate form requested */
#define FL_NEGATIVE   0x00100   /* value is negative */
#define FL_FORCEOCTAL 0x00200   /* force leading '0' for octals */
#define FL_LONGDOUBLE 0x00400   /* long double value given */
#define FL_WIDECHAR   0x00800   /* wide characters */
#define FL_LONGLONG   0x01000   /* long long value given */
#define FL_I64        0x08000   /* __int64 value given */
#ifdef POSITIONAL_PARAMETERS
/* We set this flag if %I is passed without I32 or I64 */
#define FL_PTRSIZE 0x10000   /* platform dependent number */
#endif  /* POSITIONAL_PARAMETERS */

/* state definitions */
enum STATE {
    ST_NORMAL,          /* normal state; outputting literal chars */
    ST_PERCENT,         /* just read '%' */
    ST_FLAG,            /* just read flag character */
    ST_WIDTH,           /* just read width specifier */
    ST_DOT,             /* just read '.' */
    ST_PRECIS,          /* just read precision specifier */
    ST_SIZE,            /* just read size specifier */
    ST_TYPE             /* just read type specifier */
#ifdef FORMAT_VALIDATIONS
    ,ST_INVALID           /* Invalid format */
#endif  /* FORMAT_VALIDATIONS */

};

#ifdef FORMAT_VALIDATIONS
#define NUMSTATES (ST_INVALID + 1)
#else  /* FORMAT_VALIDATIONS */
#define NUMSTATES (ST_TYPE + 1)
#endif  /* FORMAT_VALIDATIONS */

/* character type values */
enum CHARTYPE {
    CH_OTHER,           /* character with no special meaning */
    CH_PERCENT,         /* '%' */
    CH_DOT,             /* '.' */
    CH_STAR,            /* '*' */
    CH_ZERO,            /* '0' */
    CH_DIGIT,           /* '1'..'9' */
    CH_FLAG,            /* ' ', '+', '-', '#' */
    CH_SIZE,            /* 'h', 'l', 'L', 'N', 'F', 'w' */
    CH_TYPE             /* type specifying character */
};

/* static data (read only, since we are re-entrant) */
//#if defined (_UNICODE) || defined (CPRFLAG) || defined (FORMAT_VALIDATIONS)
//extern const char __nullstring[];  /* string to print on null ptr */
//extern const wchar_t __wnullstring[];  /* string to print on null ptr */
//#else  /* defined (_UNICODE) || defined (CPRFLAG) || defined (FORMAT_VALIDATIONS) */
static const char __nullstring[] = "(null)";  /* string to print on null ptr */
static const wchar_t __wnullstring[] = {'(', 'n', 'u', 'l', 'l', ')', '\0'};/* string to print on null ptr */
//#endif  /* defined (_UNICODE) || defined (CPRFLAG) || defined (FORMAT_VALIDATIONS) */

/* The state table.  This table is actually two tables combined into one. */
/* The lower nybble of each byte gives the character class of any         */
/* character; while the uper nybble of the byte gives the next state      */
/* to enter.  See the macros below the table for details.                 */
/*                                                                        */
/* The table is generated by maketabc.c -- use this program to make       */
/* changes.                                                               */

#ifndef FORMAT_VALIDATIONS

//#if defined (_UNICODE) || defined (CPRFLAG)
//extern const char __lookuptable[];
//#else  /* defined (_UNICODE) || defined (CPRFLAG) */
extern const char __lookuptable[] = {
 /* ' ' */  0x06,
 /* '!' */  0x00,
 /* '"' */  0x00,
 /* '#' */  0x06,
 /* '$' */  0x00,
 /* '%' */  0x01,
 /* '&' */  0x00,
 /* ''' */  0x00,
 /* '(' */  0x10,
 /* ')' */  0x00,
 /* '*' */  0x03,
 /* '+' */  0x06,
 /* ',' */  0x00,
 /* '-' */  0x06,
 /* '.' */  0x02,
 /* '/' */  0x10,
 /* '0' */  0x04,
 /* '1' */  0x45,
 /* '2' */  0x45,
 /* '3' */  0x45,
 /* '4' */  0x05,
 /* '5' */  0x05,
 /* '6' */  0x05,
 /* '7' */  0x05,
 /* '8' */  0x05,
 /* '9' */  0x35,
 /* ':' */  0x30,
 /* ';' */  0x00,
 /* '<' */  0x50,
 /* '=' */  0x00,
 /* '>' */  0x00,
 /* '?' */  0x00,
 /* '@' */  0x00,
#if defined (_SAFECRT_IMPL)
 /* 'A' */  0x20,       // Disable %A format
#else  /* defined (_SAFECRT_IMPL) */
 /* 'A' */  0x28,
#endif  /* defined (_SAFECRT_IMPL) */
 /* 'B' */  0x20,
 /* 'C' */  0x38,
 /* 'D' */  0x50,
 /* 'E' */  0x58,
 /* 'F' */  0x07,
 /* 'G' */  0x08,
 /* 'H' */  0x00,
 /* 'I' */  0x37,
 /* 'J' */  0x30,
 /* 'K' */  0x30,
 /* 'L' */  0x57,
 /* 'M' */  0x50,
 /* 'N' */  0x07,
 /* 'O' */  0x00,
 /* 'P' */  0x00,
 /* 'Q' */  0x20,
 /* 'R' */  0x20,
 /* 'S' */  0x08,
 /* 'T' */  0x00,
 /* 'U' */  0x00,
 /* 'V' */  0x00,
 /* 'W' */  0x00,
 /* 'X' */  0x08,
 /* 'Y' */  0x60,
 /* 'Z' */  0x68,
 /* '[' */  0x60,
 /* '\' */  0x60,
 /* ']' */  0x60,
 /* '^' */  0x60,
 /* '_' */  0x00,
 /* '`' */  0x00,
#if defined (_SAFECRT_IMPL)
 /* 'a' */  0x70,       // Disable %a format
#else  /* defined (_SAFECRT_IMPL) */
 /* 'a' */  0x78,
#endif  /* defined (_SAFECRT_IMPL) */
 /* 'b' */  0x70,
 /* 'c' */  0x78,
 /* 'd' */  0x78,
 /* 'e' */  0x78,
 /* 'f' */  0x78,
 /* 'g' */  0x08,
 /* 'h' */  0x07,
 /* 'i' */  0x08,
 /* 'j' */  0x00,
 /* 'k' */  0x00,
 /* 'l' */  0x07,
 /* 'm' */  0x00,
#if defined (_SAFECRT_IMPL)
 /* 'n' */  0x00,       // Disable %n format
#else  /* defined (_SAFECRT_IMPL) */
 /* 'n' */  0x08,
#endif  /* defined (_SAFECRT_IMPL) */
 /* 'o' */  0x08,
 /* 'p' */  0x08,
 /* 'q' */  0x00,
 /* 'r' */  0x00,
 /* 's' */  0x08,
 /* 't' */  0x00,
 /* 'u' */  0x08,
 /* 'v' */  0x00,
 /* 'w' */  0x07,
 /* 'x' */  0x08
};

//#endif  /* defined (_UNICODE) || defined (CPRFLAG) */

#else  /* FORMAT_VALIDATIONS */

//#if defined (_UNICODE) || defined (CPRFLAG) || defined (POSITIONAL_PARAMETERS)
//extern const unsigned char __lookuptable_s[];
//#else  /* defined (_UNICODE) || defined (CPRFLAG) || defined (POSITIONAL_PARAMETERS) */
static const unsigned char __lookuptable_s[] = {
 /* ' ' */  0x06,
 /* '!' */  0x80,
 /* '"' */  0x80,
 /* '#' */  0x86,
 /* '$' */  0x80,
 /* '%' */  0x81,
 /* '&' */  0x80,
 /* ''' */  0x00,
 /* '(' */  0x00,
 /* ')' */  0x10,
 /* '*' */  0x03,
 /* '+' */  0x86,
 /* ',' */  0x80,
 /* '-' */  0x86,
 /* '.' */  0x82,
 /* '/' */  0x80,
 /* '0' */  0x14,
 /* '1' */  0x05,
 /* '2' */  0x05,
 /* '3' */  0x45,
 /* '4' */  0x45,
 /* '5' */  0x45,
 /* '6' */  0x85,
 /* '7' */  0x85,
 /* '8' */  0x85,
 /* '9' */  0x05,
 /* ':' */  0x00,
 /* ';' */  0x00,
 /* '<' */  0x30,
 /* '=' */  0x30,
 /* '>' */  0x80,
 /* '?' */  0x50,
 /* '@' */  0x80,
#if defined (_SAFECRT_IMPL)
 /* 'A' */  0x80,       // Disable %A format
#else  /* defined (_SAFECRT_IMPL) */
 /* 'A' */  0x88,
#endif  /* defined (_SAFECRT_IMPL) */
 /* 'B' */  0x00,
 /* 'C' */  0x08,
 /* 'D' */  0x00,
 /* 'E' */  0x28,
 /* 'F' */  0x27,
 /* 'G' */  0x38,
 /* 'H' */  0x50,
 /* 'I' */  0x57,
 /* 'J' */  0x80,
 /* 'K' */  0x00,
 /* 'L' */  0x07,
 /* 'M' */  0x00,
 /* 'N' */  0x37,
 /* 'O' */  0x30,
 /* 'P' */  0x30,
 /* 'Q' */  0x50,
 /* 'R' */  0x50,
 /* 'S' */  0x88,
 /* 'T' */  0x00,
 /* 'U' */  0x00,
 /* 'V' */  0x00,
 /* 'W' */  0x20,
 /* 'X' */  0x28,
 /* 'Y' */  0x80,
 /* 'Z' */  0x88,
 /* '[' */  0x80,
 /* '\' */  0x80,
 /* ']' */  0x00,
 /* '^' */  0x00,
 /* '_' */  0x00,
 /* '`' */  0x60,
#if defined (_SAFECRT_IMPL)
 /* 'a' */  0x60,       // Disable %a format
#else  /* defined (_SAFECRT_IMPL) */
 /* 'a' */  0x68,
#endif  /* defined (_SAFECRT_IMPL) */
 /* 'b' */  0x60,
 /* 'c' */  0x68,
 /* 'd' */  0x68,
 /* 'e' */  0x68,
 /* 'f' */  0x08,
 /* 'g' */  0x08,
 /* 'h' */  0x07,
 /* 'i' */  0x78,
 /* 'j' */  0x70,
 /* 'k' */  0x70,
 /* 'l' */  0x77,
 /* 'm' */  0x70,
 /* 'n' */  0x70,
 /* 'o' */  0x08,
 /* 'p' */  0x08,
 /* 'q' */  0x00,
 /* 'r' */  0x00,
 /* 's' */  0x08,
 /* 't' */  0x00,
 /* 'u' */  0x08,
 /* 'v' */  0x00,
 /* 'w' */  0x07,
 /* 'x' */  0x08
};
//#endif  /* defined (_UNICODE) || defined (CPRFLAG) || defined (POSITIONAL_PARAMETERS) */

#endif  /* FORMAT_VALIDATIONS */

#ifdef POSITIONAL_PARAMETERS

#define POSITION_CHAR _T('$')

#define FORMAT_POSSCAN_PASS 0
#define FORMAT_OUTPUT_PASS 1

#define FMT_TYPE_NOTSET -1
#define FMT_TYPE_NONPOSITIONAL 0
#define FMT_TYPE_POSITIONAL 1

enum ARG_TYPE{
eblank,
e_int_arg,
#ifndef _UNICODE
e_short_arg,
#endif  /* _UNICODE */
e_ptr_arg,
e_int64_arg,
e_long_long_arg,
e_long_arg,
e_longdouble_arg,
e_double_arg
};

struct positional_param
{
    enum ARG_TYPE arg_type;
    va_list arg_ptr;
    TCHAR type;
    int flags;
};



#ifndef _UNICODE
#define _IS_FMTTYPE_WIDE(fl) ((fl & (FL_LONG|FL_WIDECHAR)) != 0)
#else  /* _UNICODE */
#define _IS_FMTTYPE_WIDE(fl) ((fl & FL_SHORT) == 0)
#endif  /* _UNICODE */

#define _IS_FMTTYPE_PTRSIZE(fl) ((fl & FL_PTRSIZE) != 0)
#define _IS_FMTTYPE_SHORT(fl) ((fl & FL_SHORT) != 0)

#define _IS_FMTTYPE_POINTER(type) (type == _T('p'))
#define _IS_FMTTYPE_STRING(type) ( (type == _T('s')) || (type == _T('S')) )
#define _IS_FMTTYPE_NUMERIC(type) ( (type == _T('d')) || (type == _T('i')) || (type == _T('o')) || \
                              (type == _T('u')) || (type == _T('x')) || (type == _T('X')) )

#ifdef _UNICODE
#define _tvalidate_param_reuse _validate_param_reuseW
#else  /* _UNICODE */
#define _tvalidate_param_reuse _validate_param_reuseA
#endif  /* _UNICODE */


/***
*int _tvalidate_param_reuse(struct positional_param * pos, enum ARG_TYPE get_fn_type, TCHAR cur_type, int cur_flags)
*
*Purpose:
*   Validates if the reused positional parameter is doesn't breaking any rules.
*   a) %p - shouldn't be mixed with other format types.
*   b) Wide & Ansi strings shouldn't be mixed
*   c) Numeric types shouldn't mix if their sizes are different
*   d) %I (without I32 or I64) shouldn't mix with other numeric types
*
*Entry:
*   struct positional_param * pos
*   enum ARG_TYPE get_fn_type
*   TCHAR cur_type
*   int cur_flags
*
*Exit:
*   Returns 0 if any non-allowed reuse is happening otherwise 1
*
*******************************************************************************/

#ifndef CPRFLAG
int _tvalidate_param_reuse(struct positional_param * pos, enum ARG_TYPE get_fn_type, TCHAR cur_type, int cur_flags)
{
    int stored_value = 0 ;
    int new_value = 0 ;

    if(_IS_FMTTYPE_POINTER(pos->type) || _IS_FMTTYPE_POINTER(cur_type))
    {
        /* %p param shouldn't be mixed with other types. If both are %p,
        then we don't need to do any more validations */
        return (pos->type == cur_type) ;
    }

    stored_value = _IS_FMTTYPE_STRING(pos->type);
    new_value = _IS_FMTTYPE_STRING(cur_type);

    if(stored_value || new_value)
    {
        /* If one type is a string, then the other also should be a string. Also
           Wide & Ansi String parameters shouldn't be interchangebly used.
           If these conditions are met, we don't need to do any more validations.
        */
        return ( (stored_value == new_value) && (_IS_FMTTYPE_WIDE(pos->flags) == _IS_FMTTYPE_WIDE(cur_flags)) );
    }

    if( _IS_FMTTYPE_NUMERIC(pos->type) || _IS_FMTTYPE_NUMERIC(cur_type))
    {
        /* If one type is numeric, then the other should also be numeric. */
        if (_IS_FMTTYPE_NUMERIC(pos->type) != _IS_FMTTYPE_NUMERIC(cur_type))
            return 0;

        /* For Numeric Types, we should allow mixing only if the size is the
        same. Also, we shouldn't allow %I (without 32/64) which is platform
        dependent to mix with other fixed numeric types */
        if( (_IS_FMTTYPE_PTRSIZE(pos->flags) != _IS_FMTTYPE_PTRSIZE(cur_flags)) ||
            (_IS_FMTTYPE_SHORT(pos->flags) != _IS_FMTTYPE_SHORT(cur_flags)) )
        {
            return 0;
        }
    }

    return (pos->arg_type == get_fn_type);
}
#else  /* CPRFLAG */
int _tvalidate_param_reuse(struct positional_param * pos, enum ARG_TYPE get_fn_type, TCHAR cur_type, int cur_flags);
#endif  /* CPRFLAG */

#define STORE_ARGPTR(pos_struct, get_fn_type, pos, cur_type, cur_flags) \
        if(pos_struct[pos].arg_type == eblank) \
        { \
            pos_struct[pos].arg_type = get_fn_type; \
            pos_struct[pos].type = cur_type; \
            pos_struct[pos].flags = cur_flags; \
        } \
        else \
        { \
            _VALIDATE_RETURN(_tvalidate_param_reuse(&pos_struct[pos], get_fn_type, cur_type, cur_flags), EINVAL, -1); \
        }


#define GET_ARG(get_fn_type, arg_ptr, lhs, type) { va_list lst = arg_ptr ;lhs = type get_fn_type(&lst);}

#endif  /* POSITIONAL_PARAMETERS */

#define FIND_CHAR_CLASS(lookuptbl, c)      \
        ((c) < _T(' ') || (c) > _T('x') ? \
            CH_OTHER            \
            :               \
        (enum CHARTYPE)(lookuptbl[(c)-_T(' ')] & 0xF))

#define FIND_NEXT_STATE(lookuptbl, class, state)   \
        (enum STATE)(lookuptbl[(class) * NUMSTATES + (state)] >> 4)

/*
 * Note: CPRFLAG and _UNICODE cases are currently mutually exclusive.
 */

/* prototypes */

#ifdef CPRFLAG

#define WRITE_CHAR(ch, pnw)         write_char(ch, pnw)
#define WRITE_MULTI_CHAR(ch, num, pnw)  write_multi_char(ch, num, pnw)
#define WRITE_STRING(s, len, pnw)   write_string(s, len, pnw)

LOCAL(void) write_char(_TCHAR ch, int *pnumwritten);
LOCAL(void) write_multi_char(_TCHAR ch, int num, int *pnumwritten);
LOCAL(void) write_string(const _TCHAR *string, int len, int *numwritten);

#else  /* CPRFLAG */

#define WRITE_CHAR(ch, pnw)         write_char(ch, stream, pnw)
#define WRITE_MULTI_CHAR(ch, num, pnw)  write_multi_char(ch, num, stream, pnw)
#define WRITE_STRING(s, len, pnw)   write_string(s, len, stream, pnw)

LOCAL(void) write_char(_TCHAR ch, miniFILE *f, int *pnumwritten);
LOCAL(void) write_multi_char(_TCHAR ch, int num, miniFILE *f, int *pnumwritten);
LOCAL(void) write_string(const _TCHAR *string, int len, miniFILE *f, int *numwritten);

#endif  /* CPRFLAG */

#ifdef __GNUC_VA_LIST

    #define get_int_arg(list)           va_arg(*list, int)
    #define get_long_arg(list)          va_arg(*list, long)
    #define get_long_long_arg(list)     va_arg(*list, long long)
    #define get_int64_arg(list)         va_arg(*list, __int64)
    #define get_longdouble_arg(list)    va_arg(*list, long double)
    #define get_crtdouble_arg(list)     va_arg(*list, _CRT_DOUBLE)
    #define get_ptr_arg(list)           va_arg(*list, void *)

#else   // __GNUC_VA_LIST

__inline int __cdecl get_int_arg(va_list *pargptr);

#if !LONG_IS_INT
__inline long __cdecl get_long_arg(va_list *pargptr);
#endif  /* !LONG_IS_INT */

#if !LONGLONG_IS_INT64
__inline long long __cdecl get_long_long_arg(va_list *pargptr);
#endif  /* !LONGLONG_IS_INT64 */

#if _INTEGRAL_MAX_BITS >= 64   
__inline __int64 __cdecl get_int64_arg(va_list *pargptr);
#endif  /* _INTEGRAL_MAX_BITS >= 64    */

#ifdef POSITIONAL_PARAMETERS
#if !LONGDOUBLE_IS_DOUBLE
__inline _LONGDOUBLE __cdecl get_longdouble_arg(va_list *pargptr);
#else  /* !LONGDOUBLE_IS_DOUBLE */
__inline _CRT_DOUBLE __cdecl get_crtdouble_arg(va_list *pargptr);
#endif  /* !LONGDOUBLE_IS_DOUBLE */
#endif  /* POSITIONAL_PARAMETERS */

#endif // __GNUC_VA_LIST

#ifdef CPRFLAG
LOCAL(int) output(const _TCHAR *, _locale_t , va_list);
_CRTIMP int __cdecl _vtcprintf_l (const _TCHAR *, _locale_t, va_list);
_CRTIMP int __cdecl _vtcprintf_s_l (const _TCHAR *, _locale_t, va_list);
_CRTIMP int __cdecl _vtcprintf_p_l (const _TCHAR *, _locale_t, va_list);


/***
*int _cprintf(format, arglist) - write formatted output directly to console
*
*Purpose:
*   Writes formatted data like printf, but uses console I/O functions.
*
*Entry:
*   char *format - format string to determine data formats
*   arglist - list of POINTERS to where to put data
*
*Exit:
*   returns number of characters written
*
*Exceptions:
*
*******************************************************************************/
#ifndef FORMAT_VALIDATIONS
_CRTIMP int __cdecl _tcprintf_l (
        const _TCHAR * format,
        _locale_t plocinfo,
        ...
        )
#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
_CRTIMP int __cdecl _tcprintf_p_l (
        const _TCHAR * format,
        _locale_t plocinfo,
        ...
        )
#else  /* POSITIONAL_PARAMETERS */
_CRTIMP int __cdecl _tcprintf_s_l (
        const _TCHAR * format,
        _locale_t plocinfo,
        ...
        )
#endif  /* POSITIONAL_PARAMETERS */
#endif  /* FORMAT_VALIDATIONS */

{
        va_list arglist;
        va_start(arglist, plocinfo);

#ifndef FORMAT_VALIDATIONS
        return _vtcprintf_l(format, plocinfo, arglist);
#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
        return _vtcprintf_p_l(format, plocinfo, arglist);
#else  /* POSITIONAL_PARAMETERS */
        return _vtcprintf_s_l(format, plocinfo, arglist);
#endif  /* POSITIONAL_PARAMETERS */

#endif  /* FORMAT_VALIDATIONS */

}

#ifndef FORMAT_VALIDATIONS
_CRTIMP int __cdecl _tcprintf (
        const _TCHAR * format,
        ...
        )
#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
_CRTIMP int __cdecl _tcprintf_p (
        const _TCHAR * format,
        ...
        )
#else  /* POSITIONAL_PARAMETERS */
_CRTIMP int __cdecl _tcprintf_s (
        const _TCHAR * format,
        ...
        )
#endif  /* POSITIONAL_PARAMETERS */
#endif  /* FORMAT_VALIDATIONS */

{
        va_list arglist;

        va_start(arglist, format);

#ifndef FORMAT_VALIDATIONS
        return _vtcprintf_l(format, NULL, arglist);
#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
        return _vtcprintf_p_l(format, NULL, arglist);
#else  /* POSITIONAL_PARAMETERS */
        return _vtcprintf_s_l(format, NULL, arglist);
#endif  /* POSITIONAL_PARAMETERS */

#endif  /* FORMAT_VALIDATIONS */

}

#endif  /* CPRFLAG */


/***
*int _output(stream, format, argptr), static int output(format, argptr)
*
*Purpose:
*   Output performs printf style output onto a stream.  It is called by
*   printf/fprintf/sprintf/vprintf/vfprintf/vsprintf to so the dirty
*   work.  In multi-thread situations, _output assumes that the given
*   stream is already locked.
*
*   Algorithm:
*       The format string is parsed by using a finite state automaton
*       based on the current state and the current character read from
*       the format string.  Thus, looping is on a per-character basis,
*       not a per conversion specifier basis.  Once the format specififying
*       character is read, output is performed.
*
*Entry:
*   FILE *stream   - stream for output
*   char *format   - printf style format string
*   va_list argptr - pointer to list of subsidiary arguments
*
*Exit:
*   Returns the number of characters written, or -1 if an output error
*   occurs.
*ifdef _UNICODE
*   The wide-character flavour returns the number of wide-characters written.
*endif
*
*Exceptions:
*
*******************************************************************************/
#ifdef CPRFLAG
#ifndef FORMAT_VALIDATIONS
_CRTIMP int __cdecl _vtcprintf (
    const _TCHAR *format,
    va_list argptr
    )
{
    return _vtcprintf_l(format, NULL, argptr);
}

#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
_CRTIMP int __cdecl _vtcprintf_p (
    const _TCHAR *format,
    va_list argptr
    )
{
    return _vtcprintf_p_l(format, NULL, argptr);
}

#else  /* POSITIONAL_PARAMETERS */
_CRTIMP int __cdecl _vtcprintf_s (
    const _TCHAR *format,
    va_list argptr
    )
{
    return _vtcprintf_s_l(format, NULL, argptr);
}

#endif  /* POSITIONAL_PARAMETERS */
#endif  /* FORMAT_VALIDATIONS */
#endif  /* CPRFLAG */

#ifdef CPRFLAG
#ifndef FORMAT_VALIDATIONS
_CRTIMP int __cdecl _vtcprintf_l (
#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
_CRTIMP int __cdecl _vtcprintf_p_l (
#else  /* POSITIONAL_PARAMETERS */
_CRTIMP int __cdecl _vtcprintf_s_l (
#endif  /* POSITIONAL_PARAMETERS */
#endif  /* FORMAT_VALIDATIONS */
#else  /* CPRFLAG */

#ifdef _UNICODE
#ifndef FORMAT_VALIDATIONS
#ifdef _SAFECRT_IMPL
int __cdecl _woutput (
#else  /* _SAFECRT_IMPL */
int __cdecl _woutput_l (
#endif  /* _SAFECRT_IMPL */
    miniFILE *stream,
#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
#ifdef _SAFECRT_IMPL
int __cdecl _woutput_p (
#else  /* _SAFECRT_IMPL */
int __cdecl _woutput_p_l (
#endif  /* _SAFECRT_IMPL */
    miniFILE *stream,
#else  /* POSITIONAL_PARAMETERS */
#ifdef _SAFECRT_IMPL
int __cdecl _woutput_s (
#else  /* _SAFECRT_IMPL */
int __cdecl _woutput_s_l (
#endif  /* _SAFECRT_IMPL */
    miniFILE *stream,
#endif  /* POSITIONAL_PARAMETERS */
#endif  /* FORMAT_VALIDATIONS */
#else  /* _UNICODE */
#ifndef FORMAT_VALIDATIONS
#ifdef _SAFECRT_IMPL
int __cdecl _output (
#else  /* _SAFECRT_IMPL */
int __cdecl _output_l (
#endif  /* _SAFECRT_IMPL */
    miniFILE *stream,
#else  /* FORMAT_VALIDATIONS */
#ifdef POSITIONAL_PARAMETERS
#ifdef _SAFECRT_IMPL
    int __cdecl _output_p (
#else  /* _SAFECRT_IMPL */
    int __cdecl _output_p_l (
#endif  /* _SAFECRT_IMPL */
    miniFILE *stream,
#else  /* POSITIONAL_PARAMETERS */
#ifdef _SAFECRT_IMPL
    int __cdecl _output_s (
#else  /* _SAFECRT_IMPL */
    int __cdecl _output_s_l (
#endif  /* _SAFECRT_IMPL */
    miniFILE *stream,
#endif  /* POSITIONAL_PARAMETERS */

#endif  /* FORMAT_VALIDATIONS */
#endif  /* _UNICODE */

#endif  /* CPRFLAG */
    const _TCHAR *format,
#ifndef _SAFECRT_IMPL
    _locale_t plocinfo,
#endif  /* _SAFECRT_IMPL */
    va_list argptr
    )
{
    int hexadd=0;     /* offset to add to number to get 'a'..'f' */
    TCHAR ch;       /* character just read */
    int flags=0;      /* flag word -- see #defines above for flag values */
    enum STATE state;   /* current state */
    enum CHARTYPE chclass; /* class of current character */
    int radix;      /* current conversion radix */
    int charsout;   /* characters currently written so far, -1 = IO error */
    int fldwidth = 0;   /* selected field width -- 0 means default */
    int precision = 0;  /* selected precision  -- -1 means default */
    TCHAR prefix[2];    /* numeric prefix -- up to two characters */
    int prefixlen=0;  /* length of prefix -- 0 means no prefix */
    int capexp = 0;     /* non-zero = 'E' exponent signifient, zero = 'e' */
    int no_output=0;  /* non-zero = prodcue no output for this specifier */
    union {
        const char *sz;   /* pointer text to be printed, not zero terminated */
        const wchar_t *wz;
        } text;

    int textlen;    /* length of the text in bytes/wchars to be printed.
                       textlen is in multibyte or wide chars if _UNICODE */
    union {
        char sz[BUFFERSIZE];
#ifdef _UNICODE
        wchar_t wz[BUFFERSIZE];
#endif  /* _UNICODE */
        } buffer;
    wchar_t wchar;                      /* temp wchar_t */
    int buffersize;                     /* size of text.sz (used only for the call to _cfltcvt) */
    int bufferiswide=0;         /* non-zero = buffer contains wide chars already */

#ifndef _SAFECRT_IMPL
    _LocaleUpdate _loc_update(plocinfo);
#endif  /* _SAFECRT_IMPL */

#ifdef POSITIONAL_PARAMETERS
    /* Used for parsing the format */
    const _TCHAR * saved_format = NULL;
    _TCHAR * end_pos = NULL;

    /* This is the structure which stores the values corresponding to
    each positional param */
    struct positional_param pos_value[_ARGMAX];

    int pass = 0;        /* Ctr for scanning the format string in diff passes */
    int noofpasses = 0;  /* Set to 2 for positional formats, otherwise 1      */
    int max_pos = -1;    /* Keeping track of the current max positional arg   */
    int type_pos = -1;   /* position of an arg denoting a type                */
    int width_pos = -1;  /* position of an arg denoting width                 */
    int precis_pos = -1; /* position of an arg denoting precision             */
    int format_type = FMT_TYPE_NOTSET; /* type of format string               */
#endif  /* POSITIONAL_PARAMETERS */

#ifndef CPRFLAG
    _VALIDATE_RETURN( (stream != NULL), EINVAL, -1);
#endif  /* CPRFLAG */
    _VALIDATE_RETURN( (format != NULL), EINVAL, -1);

    charsout = 0;       /* no characters written yet */
#ifdef POSITIONAL_PARAMETERS

    saved_format = format;

    for(pass = 0 ; pass < 2; ++pass)
    {

        if((pass == FORMAT_OUTPUT_PASS) && (format_type == FMT_TYPE_NONPOSITIONAL))
        {
            /* If in pass2, we still have format_type isn't positional, it means
            that we do not need a 2nd pass */
            break;
        }
#endif  /* POSITIONAL_PARAMETERS */
    textlen = 0;        /* no text yet */
    state = ST_NORMAL;  /* starting state */
    buffersize = 0;
#ifdef POSITIONAL_PARAMETERS
    max_pos = -1;
    fldwidth = 0;
    precision = 0;
    format = saved_format;
    type_pos = -1;
    width_pos = -1;
    precis_pos = -1;

    /* All chars before the first format specifier get output in the first
    pass itself. Hence we have to reset format_type to FMT_TYPE_NOTSET to ensure
    that they do not get output again in the 2nd pass */
    format_type = FMT_TYPE_NOTSET;
#endif  /* POSITIONAL_PARAMETERS */

    /* main loop -- loop while format character exist and no I/O errors */
    while ((ch = *format++) != _T('\0') && charsout >= 0) {
#ifndef FORMAT_VALIDATIONS
        chclass = FIND_CHAR_CLASS(__lookuptable, ch);  /* find character class */
        state = FIND_NEXT_STATE(__lookuptable, chclass, state); /* find next state */
#else  /* FORMAT_VALIDATIONS */
        chclass = FIND_CHAR_CLASS(__lookuptable_s, ch);  /* find character class */
        state = FIND_NEXT_STATE(__lookuptable_s, chclass, state); /* find next state */

#ifdef POSITIONAL_PARAMETERS
        if((state == ST_PERCENT) && (*format != _T('%')))
        {
            if(format_type == FMT_TYPE_NOTSET)
            {
                /* We set the value of format_type when we hit the first type specifier */
                if(_tcstol(format, &end_pos, 10) > 0 && (*end_pos == POSITION_CHAR))
                {
                    if(pass == FORMAT_POSSCAN_PASS)
                    {
                        memset(pos_value,0,sizeof(pos_value));
                    }
                    format_type = FMT_TYPE_POSITIONAL;
                }
                else
                {
                    format_type = FMT_TYPE_NONPOSITIONAL;
                }
            }

            if(format_type == FMT_TYPE_POSITIONAL)
            {
                type_pos = _tcstol(format, &end_pos, 10) - 1;
                format = end_pos + 1;

                if(pass == FORMAT_POSSCAN_PASS)
                {
                    /* We don't redo the validations in the 2nd pass */
                    _VALIDATE_RETURN(((type_pos >= 0) && (*end_pos == POSITION_CHAR) && (type_pos < _ARGMAX)), EINVAL, -1);

                    /* Update max_pos with the current maximum pos argument */
                    max_pos = type_pos > max_pos ? type_pos : max_pos;
                }
            }

        }
        else
        {
            /* If state is ST_INVALID, that means an invalid format specifier */
            _VALIDATE_RETURN((state != ST_INVALID), EINVAL, -1);
        }
#else  /* POSITIONAL_PARAMETERS */
        _VALIDATE_RETURN((state != ST_INVALID), EINVAL, -1);
#endif  /* POSITIONAL_PARAMETERS */

#endif  /* FORMAT_VALIDATIONS */

        /* execute code for each state */
        switch (state) {

        case ST_NORMAL:

#ifdef POSITIONAL_PARAMETERS
            if(((pass == FORMAT_POSSCAN_PASS) && (format_type == FMT_TYPE_POSITIONAL))
            || ((pass == FORMAT_OUTPUT_PASS) && (format_type == FMT_TYPE_NOTSET)))
            {

                /* Do not output in the 1st pass, if we have already come across
                a positional format specifier. All chars before the first format
                specifier get output in the first pass itself. Hence we need to
                check the format_type to make sure that they don't get output
                again in the 2nd pass */
                    break;
            }
#endif  /* POSITIONAL_PARAMETERS */
        NORMAL_STATE:

            /* normal state -- just write character */
#ifdef _UNICODE
            bufferiswide = 1;
#else  /* _UNICODE */
            bufferiswide = 0;
#endif  /* _UNICODE */
            WRITE_CHAR(ch, &charsout);
            break;

        case ST_PERCENT:
            /* set default value of conversion parameters */
            prefixlen = fldwidth = no_output = capexp = 0;
            flags = 0;
            precision = -1;
            bufferiswide = 0;   /* default */
            break;

        case ST_FLAG:
            /* set flag based on which flag character */
            switch (ch) {
            case _T('-'):
                flags |= FL_LEFT;   /* '-' => left justify */
                break;
            case _T('+'):
                flags |= FL_SIGN;   /* '+' => force sign indicator */
                break;
            case _T(' '):
                flags |= FL_SIGNSP; /* ' ' => force sign or space */
                break;
            case _T('#'):
                flags |= FL_ALTERNATE;  /* '#' => alternate form */
                break;
            case _T('0'):
                flags |= FL_LEADZERO;   /* '0' => pad with leading zeros */
                break;
            }
            break;

        case ST_WIDTH:
            /* update width value */
            if (ch == _T('*')) {
                /* get width from arg list */
#ifdef POSITIONAL_PARAMETERS
                if(format_type == FMT_TYPE_NONPOSITIONAL)
                {
#endif  /* POSITIONAL_PARAMETERS */
                fldwidth = get_int_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                }
                else
                {
                    width_pos = _tcstol(format, &end_pos, 10) - 1;
                    format = end_pos + 1;

                    if(pass == FORMAT_POSSCAN_PASS)
                    {
                        _VALIDATE_RETURN(((width_pos >= 0) && (*end_pos == POSITION_CHAR) && (type_pos < _ARGMAX)), EINVAL, -1);

                        /* Update max_pos with the current maximum pos argument */
                        max_pos = width_pos > max_pos ? width_pos : max_pos;

                        STORE_ARGPTR(pos_value, e_int_arg, width_pos, ch, flags)
                        break;

                    }
                    else
                    {
                        /* get width from arg list */
                        GET_ARG(get_int_arg,pos_value[width_pos].arg_ptr, fldwidth, )
                    }

                }
#endif  /* POSITIONAL_PARAMETERS */
                if (fldwidth < 0) {
                    /* ANSI says neg fld width means '-' flag and pos width */
                    flags |= FL_LEFT;
                    fldwidth = -fldwidth;
                }
            }
            else {
                /* add digit to current field width */
                fldwidth = fldwidth * 10 + (ch - _T('0'));
            }
            break;

        case ST_DOT:
            /* zero the precision, since dot with no number means 0
               not default, according to ANSI */
            precision = 0;
            break;

        case ST_PRECIS:
            /* update precison value */
            if (ch == _T('*')) {
                /* get precision from arg list */
#ifdef POSITIONAL_PARAMETERS
                if(format_type == FMT_TYPE_NONPOSITIONAL)
                {
#endif  /* POSITIONAL_PARAMETERS */
                precision = get_int_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                }
                else
                {
                    precis_pos = _tcstol(format, &end_pos, 10) - 1;
                    format = end_pos + 1;

                    if(pass == FORMAT_POSSCAN_PASS)
                    {
                        _VALIDATE_RETURN(((precis_pos >= 0) && (*end_pos == POSITION_CHAR) && (type_pos < _ARGMAX)), EINVAL, -1);

                        /* Update max_pos with the current maximum pos argument */
                        max_pos = precis_pos > max_pos ? precis_pos : max_pos;

                        STORE_ARGPTR(pos_value, e_int_arg, precis_pos, ch, flags)
                        break;
                    }
                    else
                    {
                        /* get width from arg list */
                        GET_ARG(get_int_arg,pos_value[precis_pos].arg_ptr, precision, )
                    }
                }
#endif  /* POSITIONAL_PARAMETERS */
                if (precision < 0)
                    precision = -1; /* neg precision means default */
            }
            else {
                /* add digit to current precision */
                precision = precision * 10 + (ch - _T('0'));
            }
            break;

        case ST_SIZE:
            /* just read a size specifier, set the flags based on it */
            switch (ch) {
            case _T('l'):
                /*
                 * In order to handle the ll case, we depart from the
                 * simple deterministic state machine.
                 */
                if (*format == _T('l'))
                {
                    ++format;
                    flags |= FL_LONGLONG;   /* 'll' => long long */
                }
                else
                {
                    flags |= FL_LONG;   /* 'l' => long int or wchar_t */
                }
                break;

            case _T('I'):
                /*
                 * In order to handle the I, I32, and I64 size modifiers, we
                 * depart from the simple deterministic state machine. The
                 * code below scans for characters following the 'I',
                 * and defaults to 64 bit on WIN64 and 32 bit on WIN32
                 */
#if PTR_IS_INT64
                flags |= FL_I64;    /* 'I' => __int64 on WIN64 systems */
#endif  /* PTR_IS_INT64 */
                if ( (*format == _T('6')) && (*(format + 1) == _T('4')) )
                {
                    format += 2;
                    flags |= FL_I64;    /* I64 => __int64 */
                }
                else if ( (*format == _T('3')) && (*(format + 1) == _T('2')) )
                {
                    format += 2;
                    flags &= ~FL_I64;   /* I32 => __int32 */
                }
                else if ( (*format == _T('d')) ||
                          (*format == _T('i')) ||
                          (*format == _T('o')) ||
                          (*format == _T('u')) ||
                          (*format == _T('x')) ||
                          (*format == _T('X')) )
                {
#ifdef POSITIONAL_PARAMETERS
    /* %I without 32/64 is platform dependent. We set FL_PTRSIZE to indicate
    this - this is used in the positional parameter reuse validation */
                    flags |= FL_PTRSIZE;
#else  /* POSITIONAL_PARAMETERS */
                   /*
                    * Nothing further needed.  %Id (et al) is
                    * handled just like %d, except that it defaults to 64 bits
                    * on WIN64.  Fall through to the next iteration.
                    */
#endif  /* POSITIONAL_PARAMETERS */
                }
                else {
                    state = ST_NORMAL;
                    goto NORMAL_STATE;
                }
                break;

            case _T('h'):
                flags |= FL_SHORT;  /* 'h' => short int or char */
                break;

            case _T('w'):
                flags |= FL_WIDECHAR;  /* 'w' => wide character */
                break;

            }
            break;

        case ST_TYPE:
            /* we have finally read the actual type character, so we       */
            /* now format and "print" the output.  We use a big switch     */
            /* statement that sets 'text' to point to the text that should */
            /* be printed, and 'textlen' to the length of this text.       */
            /* Common code later on takes care of justifying it and        */
            /* other miscellaneous chores.  Note that cases share code,    */
            /* in particular, all integer formatting is done in one place. */
            /* Look at those funky goto statements!                        */

            switch (ch) {

            case _T('C'):   /* ISO wide character */
                if (!(flags & (FL_SHORT|FL_LONG|FL_WIDECHAR)))
#ifdef _UNICODE
                    flags |= FL_SHORT;
#else  /* _UNICODE */
                    flags |= FL_WIDECHAR;   /* ISO std. */
#endif  /* _UNICODE */
                /* fall into 'c' case */

            case _T('c'): {
                /* print a single character specified by int argument */
#ifdef _UNICODE
                bufferiswide = 1;
#ifdef POSITIONAL_PARAMETERS
                if(format_type == FMT_TYPE_NONPOSITIONAL)
                {
#endif  /* POSITIONAL_PARAMETERS */
                        wchar = (wchar_t) get_int_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                }
                else
                {
                        _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                        if (pass == FORMAT_POSSCAN_PASS)
                        {
                                STORE_ARGPTR(pos_value, e_int_arg, type_pos, ch, flags)
                                break;
                        }
                        else
                        {
                                GET_ARG(get_int_arg,pos_value[type_pos].arg_ptr, wchar, (wchar_t))
                        }
                }
#endif  /* POSITIONAL_PARAMETERS */
                if (flags & FL_SHORT) {
                    /* format multibyte character */
                    /* this is an extension of ANSI */
                    char tempchar[2];
                    {
                        tempchar[0] = (char)(wchar & 0x00ff);
                        tempchar[1] = '\0';
                    }

#ifdef _SAFECRT_IMPL
                    if (_MBTOWC(buffer.wz,tempchar, MB_CUR_MAX) < 0)
#else  /* _SAFECRT_IMPL */
                    if (_mbtowc_l(buffer.wz,
                                  tempchar,
                                  _loc_update.GetLocaleT()->locinfo->mb_cur_max,
                                  _loc_update.GetLocaleT()) < 0)
#endif  /* _SAFECRT_IMPL */
                    {
                        /* ignore if conversion was unsuccessful */
                        no_output = 1;
                    }
                } else {
                    buffer.wz[0] = wchar;
                }
                text.wz = buffer.wz;
                textlen = 1;    /* print just a single character */
#else  /* _UNICODE */
                if (flags & (FL_LONG|FL_WIDECHAR)) {
#ifdef POSITIONAL_PARAMETERS
                    if(format_type == FMT_TYPE_NONPOSITIONAL)
                    {
#endif  /* POSITIONAL_PARAMETERS */
                    wchar = (wchar_t) get_int_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                    }
                    else
                    {
                        _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                        if (pass == FORMAT_POSSCAN_PASS)
                        {
                            STORE_ARGPTR(pos_value, e_short_arg, type_pos, ch, flags)
                            break;
                        }
                        else
                        {
                            GET_ARG(get_int_arg,pos_value[type_pos].arg_ptr, wchar, (wchar_t))
                        }
                    }
#endif  /* POSITIONAL_PARAMETERS */
                    no_output = 1;
                } else {
                    /* format multibyte character */
                    /* this is an extension of ANSI */
                    unsigned short temp;
#ifdef POSITIONAL_PARAMETERS
                    if(format_type == FMT_TYPE_NONPOSITIONAL)
                    {
#endif  /* POSITIONAL_PARAMETERS */
                    wchar = (wchar_t)get_int_arg(&argptr);
                    temp = (unsigned short)wchar;
#ifdef POSITIONAL_PARAMETERS
                    }
                    else
                    {
                        _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                        if (pass == FORMAT_POSSCAN_PASS)
                        {
                            STORE_ARGPTR(pos_value, e_int_arg, type_pos, ch, flags)
                            break;
                        }
                        else
                        {
                            GET_ARG(get_int_arg,pos_value[type_pos].arg_ptr, temp, (unsigned short))
                        }
                    }
#endif  /* POSITIONAL_PARAMETERS */
                    {
                        buffer.sz[0] = (char) temp;
                        textlen = 1;
                    }
                }
                text.sz = buffer.sz;
#endif  /* _UNICODE */
            }
            break;

            case _T('Z'): {
                /* print a Counted String */
                struct _count_string {
                    short Length;
                    short MaximumLength;
                    char *Buffer;
                } *pstr;

#ifdef POSITIONAL_PARAMETERS
                if(format_type == FMT_TYPE_NONPOSITIONAL)
                {
#endif  /* POSITIONAL_PARAMETERS */
                pstr = (struct _count_string *)get_ptr_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                }
                else
                {
                    _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                    if (pass == FORMAT_POSSCAN_PASS)
                    {
                        STORE_ARGPTR(pos_value, e_ptr_arg, type_pos, ch, flags)
                        break;
                    }
                    else
                    {
                        GET_ARG(get_ptr_arg,pos_value[type_pos].arg_ptr, pstr, (struct _count_string *) )
                    }
                }
#endif  /* POSITIONAL_PARAMETERS */
                if (pstr == NULL || pstr->Buffer == NULL) {
                    /* null ptr passed, use special string */
                    text.sz = __nullstring;
                    textlen = (int)strlen(text.sz);
                } else {
                    if (flags & FL_WIDECHAR) {
                        text.wz = (wchar_t *)pstr->Buffer;
                        textlen = pstr->Length / (int)sizeof(wchar_t);
                        bufferiswide = 1;
                    } else {
                        bufferiswide = 0;
                        text.sz = pstr->Buffer;
                        textlen = pstr->Length;
                    }
                }
            }
            break;

            case _T('S'):   /* ISO wide character string */
#ifndef _UNICODE
                if (!(flags & (FL_SHORT|FL_LONG|FL_WIDECHAR)))
                    flags |= FL_WIDECHAR;
#else  /* _UNICODE */
                if (!(flags & (FL_SHORT|FL_LONG|FL_WIDECHAR)))
                    flags |= FL_SHORT;
#endif  /* _UNICODE */

            case _T('s'): {
                /* print a string --                            */
                /* ANSI rules on how much of string to print:   */
                /*   all if precision is default,               */
                /*   min(precision, length) if precision given. */
                /* prints '(null)' if a null string is passed   */

                int i;
                const char *p;       /* temps */
                const wchar_t *pwch;

                /* At this point it is tempting to use strlen(), but */
                /* if a precision is specified, we're not allowed to */
                /* scan past there, because there might be no null   */
                /* at all.  Thus, we must do our own scan.           */

                i = (precision == -1) ? INT_MAX : precision;
#ifdef POSITIONAL_PARAMETERS
                if(format_type == FMT_TYPE_NONPOSITIONAL)
                {
#endif  /* POSITIONAL_PARAMETERS */
                text.sz = (char *)get_ptr_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                }
                else
                {
                    _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                    if (pass == FORMAT_POSSCAN_PASS)
                    {
                        STORE_ARGPTR(pos_value, e_ptr_arg, type_pos, ch, flags)
                        break;
                    }
                    else
                    {
                        GET_ARG(get_ptr_arg,pos_value[type_pos].arg_ptr, text.sz,(char *))
                    }
                }
#endif  /* POSITIONAL_PARAMETERS */

                /* scan for null upto i characters */
#ifdef _UNICODE
                if (flags & FL_SHORT) {
                    if (text.sz == NULL) /* NULL passed, use special string */
                        text.sz = __nullstring;
                    p = text.sz;
                    for (textlen=0; textlen<i && *p; textlen++) {
                        ++p;
                    }
                    /* textlen now contains length in multibyte chars */
                } else {
                    if (text.wz == NULL) /* NULL passed, use special string */
                        text.wz = __wnullstring;
                    bufferiswide = 1;
                    pwch = text.wz;
                    while (i-- && *pwch)
                        ++pwch;
                    textlen = (int)(pwch - text.wz);       /* in wchar_ts */
                    /* textlen now contains length in wide chars */
                }
#else  /* _UNICODE */
                if (flags & (FL_LONG|FL_WIDECHAR)) {
                    if (text.wz == NULL) /* NULL passed, use special string */
                        text.wz = __wnullstring;
                    bufferiswide = 1;
                    pwch = text.wz;
                    while ( i-- && *pwch )
                        ++pwch;
                    textlen = (int)(pwch - text.wz);
                    /* textlen now contains length in wide chars */
                } else {
                    if (text.sz == NULL) /* NULL passed, use special string */
                        text.sz = __nullstring;
                    p = text.sz;
                    while (i-- && *p)
                        ++p;
                    textlen = (int)(p - text.sz);    /* length of the string */
                }

#endif  /* _UNICODE */
            }
            break;


            case _T('n'): {
                /* write count of characters seen so far into */
                /* short/int/long thru ptr read from args */

                void *p;        /* temp */

#ifdef POSITIONAL_PARAMETERS
                if(format_type == FMT_TYPE_NONPOSITIONAL)
                {
#endif  /* POSITIONAL_PARAMETERS */
                p = get_ptr_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                }
                else
                {
                    _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                    if (pass == FORMAT_POSSCAN_PASS)
                    {
                        STORE_ARGPTR(pos_value, e_ptr_arg, type_pos, ch, flags)
                        break;
                    }
                    else
                    {
                        GET_ARG(get_ptr_arg,pos_value[type_pos].arg_ptr,p,)
                    }
                }
#endif  /* POSITIONAL_PARAMETERS */

                /* %n is disabled */
                _VALIDATE_RETURN(("'n' format specifier disabled" && 0), EINVAL, -1);
                break;

                /* store chars out into short/long/int depending on flags */
#if !LONG_IS_INT
                if (flags & FL_LONG)
                    *(long *)p = charsout;
                else
#endif  /* !LONG_IS_INT */

#if !SHORT_IS_INT
                if (flags & FL_SHORT)
                    *(short *)p = (short) charsout;
                else
#endif  /* !SHORT_IS_INT */
                    *(int *)p = charsout;

                no_output = 1;              /* force no output */
            }
            break;

            case _T('E'):
            case _T('G'):
            case _T('A'):
                capexp = 1;                 /* capitalize exponent */
                ch += _T('a') - _T('A');    /* convert format char to lower */
                /* DROP THROUGH */
            case _T('e'):
            case _T('f'):
            case _T('g'):
            case _T('a'): {
                /* floating point conversion -- we call cfltcvt routines */
                /* to do the work for us.                                */
                flags |= FL_SIGNED;         /* floating point is signed conversion */
#ifdef POSITIONAL_PARAMETERS
                if((format_type == FMT_TYPE_POSITIONAL) && (pass == FORMAT_POSSCAN_PASS))
                {
                    _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

#if !LONGDOUBLE_IS_DOUBLE

                    if (flags & FL_LONGDOUBLE)
                    {
                        STORE_ARGPTR(pos_value, e_longdouble_arg, type_pos, ch, flags)
                    }
                    else
#endif  /* !LONGDOUBLE_IS_DOUBLE */
                    {
                        STORE_ARGPTR(pos_value, e_double_arg, type_pos, ch, flags)
                    }

                    break;
                }
#endif  /* POSITIONAL_PARAMETERS */
                text.sz = buffer.sz;        /* put result in buffer */
                buffersize = BUFFERSIZE;

                /* compute the precision value */
                if (precision < 0)
                    precision = 6;          /* default precision: 6 */
                else if (precision == 0 && ch == _T('g'))
                    precision = 1;          /* ANSI specified */
                else if (precision > MAXPRECISION)
                    precision = MAXPRECISION;

                if (precision > BUFFERSIZE - _CVTBUFSIZE) {
                        /* cap precision further */
                        precision = BUFFERSIZE - _CVTBUFSIZE;
                }

#ifdef _SAFECRT_IMPL
                /* for safecrt, we pass along the FL_ALTERNATE flag to _safecrt_cfltcvt */
                if (flags & FL_ALTERNATE)
                {
                    capexp |= FL_ALTERNATE;
                }
#endif  /* _SAFECRT_IMPL */

#if !LONGDOUBLE_IS_DOUBLE
                /* do the conversion */
                if (flags & FL_LONGDOUBLE) {
                    _LONGDOUBLE tmp;
#ifdef POSITIONAL_PARAMETERS
                    if(format_type == FMT_TYPE_NONPOSITIONAL)
                    {
#endif  /* POSITIONAL_PARAMETERS */
                    tmp=va_arg(argptr, _LONGDOUBLE);
#ifdef POSITIONAL_PARAMETERS
                    }
                    else
                    {
                        /* Will get here only for pass == FORMAT_OUTPUT_PASS because
                        pass == FORMAT_POSSCAN_PASS has a break Above */
                        va_list tmp_arg;
                        _ASSERTE(pass == FORMAT_OUTPUT_PASS);
                        tmp_arg = pos_value[type_pos].arg_ptr;
                        tmp=va_arg(tmp_arg, _LONGDOUBLE);
                    }
#endif  /* POSITIONAL_PARAMETERS */
                    /* Note: assumes ch is in ASCII range */
                    _CLDCVT(&tmp, buffer.sz, buffersize, (char)ch, precision, capexp);
                } else
#endif  /* !LONGDOUBLE_IS_DOUBLE */
                {
                    _CRT_DOUBLE tmp;
#ifdef POSITIONAL_PARAMETERS
                    if(format_type == FMT_TYPE_NONPOSITIONAL)
                    {
#endif  /* POSITIONAL_PARAMETERS */
                    tmp=va_arg(argptr, _CRT_DOUBLE);
#ifdef POSITIONAL_PARAMETERS
                    }
                    else
                    {
                        /* Will get here only for pass == FORMAT_OUTPUT_PASS because
                        pass == FORMAT_POSSCAN_PASS has a break Above */
                        va_list tmp_arg;

                        _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                                                _ASSERTE(pass == FORMAT_OUTPUT_PASS);
                        tmp_arg = pos_value[type_pos].arg_ptr;
                        tmp=va_arg(tmp_arg, _CRT_DOUBLE);
                    }
#endif  /* POSITIONAL_PARAMETERS */
                    /* Note: assumes ch is in ASCII range */
                    /* In safecrt, we provide a special version of _cfltcvt which internally calls printf (see safecrt_output_s.c) */
#ifndef _SAFECRT_IMPL
                    _cfltcvt_l(&tmp.x, buffer.sz, buffersize, (char)ch, precision, capexp, _loc_update.GetLocaleT());
#else  /* _SAFECRT_IMPL */
                    _CFLTCVT(&tmp, buffer.sz, buffersize, (char)ch, precision, capexp);
#endif  /* _SAFECRT_IMPL */
                }

#ifndef _SAFECRT_IMPL
                /* For safecrt, this is done already in _safecrt_cfltcvt */

                /* '#' and precision == 0 means force a decimal point */
                if ((flags & FL_ALTERNATE) && precision == 0)
                {
                    _forcdecpt_l(text.sz, _loc_update.GetLocaleT());
                }

                /* 'g' format means crop zero unless '#' given */
                if (ch == _T('g') && !(flags & FL_ALTERNATE))
                {
                    _cropzeros_l(text.sz, _loc_update.GetLocaleT());
                }
#endif  /* _SAFECRT_IMPL */

                /* check if result was negative, save '-' for later */
                /* and point to positive part (this is for '0' padding) */
                if (*text.sz == '-') {
                    flags |= FL_NEGATIVE;
                    ++text.sz;
                }

                textlen = (int)strlen(text.sz);     /* compute length of text */
            }
            break;

            case _T('d'):
            case _T('i'):
                /* signed decimal output */
                flags |= FL_SIGNED;
                radix = 10;
                goto COMMON_INT;

            case _T('u'):
                radix = 10;
                goto COMMON_INT;

            case _T('p'):
                /* write a pointer -- this is like an integer or long */
                /* except we force precision to pad with zeros and */
                /* output in big hex. */

                precision = 2 * sizeof(void *);     /* number of hex digits needed */
#if PTR_IS_INT64
                flags |= FL_I64;                    /* assume we're converting an int64 */
#elif !PTR_IS_INT
                flags |= FL_LONG;                   /* assume we're converting a long */
#endif  /* !PTR_IS_INT */
                /* DROP THROUGH to hex formatting */

            case _T('X'):
                /* unsigned upper hex output */
                hexadd = _T('A') - _T('9') - 1;     /* set hexadd for uppercase hex */
                goto COMMON_HEX;

            case _T('x'):
                /* unsigned lower hex output */
                hexadd = _T('a') - _T('9') - 1;     /* set hexadd for lowercase hex */
                /* DROP THROUGH TO COMMON_HEX */

            COMMON_HEX:
                radix = 16;
                if (flags & FL_ALTERNATE) {
                    /* alternate form means '0x' prefix */
                    prefix[0] = _T('0');
                    prefix[1] = (TCHAR)(_T('x') - _T('a') + _T('9') + 1 + hexadd);  /* 'x' or 'X' */
                    prefixlen = 2;
                }
                goto COMMON_INT;

            case _T('o'):
                /* unsigned octal output */
                radix = 8;
                if (flags & FL_ALTERNATE) {
                    /* alternate form means force a leading 0 */
                    flags |= FL_FORCEOCTAL;
                }
                /* DROP THROUGH to COMMON_INT */

            COMMON_INT: {
                /* This is the general integer formatting routine. */
                /* Basically, we get an argument, make it positive */
                /* if necessary, and convert it according to the */
                /* correct radix, setting text and textlen */
                /* appropriately. */

#if _INTEGRAL_MAX_BITS >= 64       
                __uint64_t number;    /* number to convert */
                int digit;              /* ascii value of digit */
                __int64 l;              /* temp long value */
#else  /* _INTEGRAL_MAX_BITS >= 64        */
                unsigned long number;   /* number to convert */
                int digit;              /* ascii value of digit */
                long l;                 /* temp long value */
#endif  /* _INTEGRAL_MAX_BITS >= 64        */

                /* 1. read argument into l, sign extend as needed */
#if _INTEGRAL_MAX_BITS >= 64       
                if (flags & FL_I64)
#ifdef POSITIONAL_PARAMETERS
                {
                    if(format_type == FMT_TYPE_NONPOSITIONAL)
                    {
#endif  /* POSITIONAL_PARAMETERS */
                    l = get_int64_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                    }
                    else
                    {
                        _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                        if (pass == FORMAT_POSSCAN_PASS)
                        {
                            STORE_ARGPTR(pos_value, e_int64_arg, type_pos, ch, flags)
                            break;
                        }
                        else
                        {
                            GET_ARG(get_int64_arg,pos_value[type_pos].arg_ptr,l,)
                        }
                    }
                }
#endif  /* POSITIONAL_PARAMETERS */
                else
#endif  /* _INTEGRAL_MAX_BITS >= 64        */

                if (flags & FL_LONGLONG)
#ifdef POSITIONAL_PARAMETERS
                {
                    if(format_type == FMT_TYPE_NONPOSITIONAL)
                    {
#endif  /* POSITIONAL_PARAMETERS */
                    l = get_long_long_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                    }
                    else
                    {
                        _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                        if (pass == FORMAT_POSSCAN_PASS)
                        {
                            STORE_ARGPTR(pos_value, e_long_long_arg, type_pos, ch, flags)
                            break;
                        }
                        else
                        {
                            GET_ARG(get_long_long_arg,pos_value[type_pos].arg_ptr,l,)
                        }
                    }
                }
#endif  /* POSITIONAL_PARAMETERS */

                else

#if !LONG_IS_INT
                if (flags & FL_LONG)
#ifdef POSITIONAL_PARAMETERS
                {
                    if(format_type == FMT_TYPE_NONPOSITIONAL)
                    {
#endif  /* POSITIONAL_PARAMETERS */
                    l = get_long_arg(&argptr);
#ifdef POSITIONAL_PARAMETERS
                    }
                    else
                    {
                        _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                        if (pass == FORMAT_POSSCAN_PASS)
                        {
                            STORE_ARGPTR(pos_value, e_long_arg, type_pos, ch, flags)
                            break;
                        }
                        else
                        {
                            l = GETARG(get_long_arg,pos_value[type_pos].arg_ptr);
                        }
                    }
                }
#endif  /* POSITIONAL_PARAMETERS */
                else
#endif  /* !LONG_IS_INT */

#if !SHORT_IS_INT
                if (flags & FL_SHORT) {
                    if (flags & FL_SIGNED)
#ifdef POSITIONAL_PARAMETERS
                    {
                        if(format_type == FMT_TYPE_NONPOSITIONAL)
                        {
#endif  /* POSITIONAL_PARAMETERS */
                        l = (short) get_int_arg(&argptr); /* sign extend */
#ifdef POSITIONAL_PARAMETERS
                        }
                        else
                        {
                            _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                            if (pass == FORMAT_POSSCAN_PASS)
                            {
                                STORE_ARGPTR(pos_value, e_int_arg, type_pos, ch, flags)
                                break;
                            }
                            else
                            {
                                GET_ARG(get_int_arg,pos_value[type_pos].arg_ptr,l, (short)) /* sign extend */
                            }
                        }
                    }
#endif  /* POSITIONAL_PARAMETERS */
                    else
#ifdef POSITIONAL_PARAMETERS
                    {
                        if(format_type == FMT_TYPE_NONPOSITIONAL)
                        {
#endif  /* POSITIONAL_PARAMETERS */
                        l = (unsigned short) get_int_arg(&argptr);    /* zero-extend*/
#ifdef POSITIONAL_PARAMETERS
                        }
                        else
                        {
                            _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                            if (pass == FORMAT_POSSCAN_PASS)
                            {
                                STORE_ARGPTR(pos_value, e_int_arg, type_pos, ch, flags)
                                break;
                            }
                            else
                            {
                                GET_ARG(get_int_arg,pos_value[type_pos].arg_ptr,l, (unsigned short)) /* zero-extend*/
                            }
                        }
                    }
#endif  /* POSITIONAL_PARAMETERS */

                } else
#endif  /* !SHORT_IS_INT */
                {
                    if (flags & FL_SIGNED)
#ifdef POSITIONAL_PARAMETERS
                    {
                        if(format_type == FMT_TYPE_NONPOSITIONAL)
                        {
#endif  /* POSITIONAL_PARAMETERS */
                        l = get_int_arg(&argptr); /* sign extend */
#ifdef POSITIONAL_PARAMETERS
                        }
                        else
                        {
                            _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                            if (pass == FORMAT_POSSCAN_PASS)
                            {
                                STORE_ARGPTR(pos_value, e_int_arg, type_pos, ch, flags)
                                break;
                            }
                            else
                            {
                                GET_ARG(get_int_arg,pos_value[type_pos].arg_ptr, l, ) /* sign extend */
                            }
                        }
                    }
#endif  /* POSITIONAL_PARAMETERS */
                    else
#ifdef POSITIONAL_PARAMETERS
                    {
                        if(format_type == FMT_TYPE_NONPOSITIONAL)
                        {
#endif  /* POSITIONAL_PARAMETERS */
                        l = (unsigned int) get_int_arg(&argptr);    /* zero-extend*/
#ifdef POSITIONAL_PARAMETERS
                        }
                        else
                        {
                            _VALIDATE_RETURN(((type_pos>=0) && (type_pos<_ARGMAX)), EINVAL, -1);

                            if (pass == FORMAT_POSSCAN_PASS)
                            {
                                STORE_ARGPTR(pos_value, e_int_arg, type_pos, ch, flags)
                                break;
                            }
                            else
                            {
                                GET_ARG(get_int_arg,pos_value[type_pos].arg_ptr, l, (unsigned int)) /* zero-extend*/
                            }
                        }
                    }
#endif  /* POSITIONAL_PARAMETERS */

                }

                /* 2. check for negative; copy into number */
                if ( (flags & FL_SIGNED) && l < 0) {
                    number = -l;
                    flags |= FL_NEGATIVE;   /* remember negative sign */
                } else {
                    number = l;
                }

#if _INTEGRAL_MAX_BITS >= 64       
                if ( (flags & FL_I64) == 0 && (flags & FL_LONGLONG) == 0 ) {
                    /*
                     * Unless printing a full 64-bit value, insure values
                     * here are not in cananical longword format to prevent
                     * the sign extended upper 32-bits from being printed.
                     */
                    number &= 0xffffffff;
                }
#endif  /* _INTEGRAL_MAX_BITS >= 64        */

                /* 3. check precision value for default; non-default */
                /*    turns off 0 flag, according to ANSI. */
                if (precision < 0)
                    precision = 1;  /* default precision */
                else {
                    flags &= ~FL_LEADZERO;
                    if (precision > MAXPRECISION)
                        precision = MAXPRECISION;
                }

                /* 4. Check if data is 0; if so, turn off hex prefix */
                if (number == 0)
                    prefixlen = 0;

                /* 5. Convert data to ASCII -- note if precision is zero */
                /*    and number is zero, we get no digits at all.       */

                char *sz;
                sz = &buffer.sz[BUFFERSIZE-1];    /* last digit at end of buffer */

                while (precision-- > 0 || number != 0) {
                    digit = (int)(number % radix) + '0';
                    number /= radix;                /* reduce number */
                    if (digit > '9') {
                        /* a hex digit, make it a letter */
                        digit += hexadd;
                    }
                    *sz-- = (char)digit;       /* store the digit */
                }

                textlen = (int)((char *)&buffer.sz[BUFFERSIZE-1] - sz); /* compute length of number */
                ++sz;          /* text points to first digit now */


                /* 6. Force a leading zero if FORCEOCTAL flag set */
                if ((flags & FL_FORCEOCTAL) && (textlen == 0 || sz[0] != '0')) {
                    *--sz = '0';
                    ++textlen;      /* add a zero */
                }
                
                text.sz = sz;
            }
            break;
            }

#ifdef POSITIONAL_PARAMETERS
            if((format_type == FMT_TYPE_POSITIONAL) && (pass == FORMAT_POSSCAN_PASS))
            {
                break;
            }
#endif  /* POSITIONAL_PARAMETERS */


            /* At this point, we have done the specific conversion, and */
            /* 'text' points to text to print; 'textlen' is length.  Now we */
            /* justify it, put on prefixes, leading zeros, and then */
            /* print it. */

            if (!no_output) {
                int padding;    /* amount of padding, negative means zero */

                if (flags & FL_SIGNED) {
                    if (flags & FL_NEGATIVE) {
                        /* prefix is a '-' */
                        prefix[0] = _T('-');
                        prefixlen = 1;
                    }
                    else if (flags & FL_SIGN) {
                        /* prefix is '+' */
                        prefix[0] = _T('+');
                        prefixlen = 1;
                    }
                    else if (flags & FL_SIGNSP) {
                        /* prefix is ' ' */
                        prefix[0] = _T(' ');
                        prefixlen = 1;
                    }
                }

                /* calculate amount of padding -- might be negative, */
                /* but this will just mean zero */
                padding = fldwidth - textlen - prefixlen;

                /* put out the padding, prefix, and text, in the correct order */

                if (!(flags & (FL_LEFT | FL_LEADZERO))) {
                    /* pad on left with blanks */
                    WRITE_MULTI_CHAR(_T(' '), padding, &charsout);
                }

                /* write prefix */
                WRITE_STRING(prefix, prefixlen, &charsout);

                if ((flags & FL_LEADZERO) && !(flags & FL_LEFT)) {
                    /* write leading zeros */
                    WRITE_MULTI_CHAR(_T('0'), padding, &charsout);
                }

                /* write text */
#ifndef _UNICODE
                if (bufferiswide && (textlen > 0)) {
                    charsout = -1;
                } else {
                    WRITE_STRING(text.sz, textlen, &charsout);
                }
#else  /* _UNICODE */
                if (!bufferiswide && textlen > 0) {
                    const char *p;
                    int retval = 0;
                    int count;

                    p = text.sz;
                    count = textlen;
                    while (count-- > 0) {
#ifdef _SAFECRT_IMPL
                        retval = _MBTOWC(&wchar, p, MB_CUR_MAX);
#else  /* _SAFECRT_IMPL */
                        retval = _mbtowc_l(&wchar,
                                           p,
                                           _loc_update.GetLocaleT()->locinfo->mb_cur_max,
                                           _loc_update.GetLocaleT());
#endif  /* _SAFECRT_IMPL */
                        if (retval <= 0) {
                            charsout = -1;
                            break;
                        }
                        WRITE_CHAR(wchar, &charsout);
                        p += retval;
                    }
                } else {
                    WRITE_STRING(text.wz, textlen, &charsout);
                }
#endif  /* _UNICODE */

                if (charsout >= 0 && (flags & FL_LEFT)) {
                    /* pad on right with blanks */
                    WRITE_MULTI_CHAR(_T(' '), padding, &charsout);
                }

                /* we're done! */
            }
            break;
        case ST_INVALID:
            _VALIDATE_RETURN(0 /* FALSE */, EINVAL, -1);
            break;
        }
    }

#ifdef FORMAT_VALIDATIONS
    /* The format string shouldn't be incomplete - i.e. when we are finished
        with the format string, the last thing we should have encountered
        should have been a regular char to be output or a type specifier. Else
        the format string was incomplete */
    _VALIDATE_RETURN(((state == ST_NORMAL) || (state == ST_TYPE)), EINVAL, -1);
#endif  /* FORMAT_VALIDATIONS */

#ifdef POSITIONAL_PARAMETERS
    if((format_type == FMT_TYPE_POSITIONAL) && (pass == FORMAT_POSSCAN_PASS))
    {
        /* At the end of the 1st pass, we have the types filled into the
        arg_type member of the struct. We now need to get argument pointer on
        the stack & store it into the arg_ptr member */
        for(type_pos = 0; type_pos <= max_pos; ++type_pos)
        {
            switch(pos_value[type_pos].arg_type)
            {
                case e_int_arg :
                    pos_value[type_pos].arg_ptr = argptr;
                    get_int_arg(&argptr);
                    break;
#ifndef _UNICODE
                case e_short_arg:
                    pos_value[type_pos].arg_ptr = argptr;
                    get_int_arg(&argptr);
                    break;
#endif  /* _UNICODE */
                case e_ptr_arg:
                    pos_value[type_pos].arg_ptr = argptr;
                    get_ptr_arg(&argptr);
                    break;

                case e_int64_arg:
                    pos_value[type_pos].arg_ptr = argptr;
                    get_int64_arg(&argptr);
                    break;

                case e_long_long_arg:
                    pos_value[type_pos].arg_ptr = argptr;
                    get_long_long_arg(&argptr);
                    break;

                case e_long_arg:
                    pos_value[type_pos].arg_ptr = argptr;
                    get_long_arg(&argptr);
                    break;

#if !LONGDOUBLE_IS_DOUBLE
                case e_longdouble_arg:
                    pos_value[type_pos].arg_ptr = argptr;
                    get_longdouble_arg(&argptr);
                    break;
#else  /* !LONGDOUBLE_IS_DOUBLE */
                case e_double_arg :
                    pos_value[type_pos].arg_ptr = argptr;
                    get_crtdouble_arg(&argptr);
                    break;
#endif  /* !LONGDOUBLE_IS_DOUBLE */
                default:
                    /* Should never get here */
                    _VALIDATE_RETURN(("Missing position in the format string", 0), EINVAL, -1);
                    break;
            }
        }
    }


    }
#endif  /* POSITIONAL_PARAMETERS */

    return charsout;        /* return value = number of characters written */
}

/*
 *  Future Optimizations for swprintf:
 *  - Don't free the memory used for converting the buffer to wide chars.
 *    Use realloc if the memory is not sufficient.  Free it at the end.
 */

/***
*void write_char(char ch, int *pnumwritten)
*ifdef _UNICODE
*void write_char(wchar_t ch, FILE *f, int *pnumwritten)
*endif
*void write_char(char ch, FILE *f, int *pnumwritten)
*
*Purpose:
*   Writes a single character to the given file/console.  If no error occurs,
*   then *pnumwritten is incremented; otherwise, *pnumwritten is set
*   to -1.
*
*Entry:
*   _TCHAR ch        - character to write
*   FILE *f          - file to write to
*   int *pnumwritten - pointer to integer to update with total chars written
*
*Exit:
*   No return value.
*
*Exceptions:
*
*******************************************************************************/

#ifdef CPRFLAG

LOCAL(void) write_char (
    _TCHAR ch,
    int *pnumwritten
    )
{
#ifdef _UNICODE
    if (_putwch_nolock(ch) == WEOF)
#else  /* _UNICODE */
    if (_putch_nolock(ch) == EOF)
#endif  /* _UNICODE */
        *pnumwritten = -1;
    else
        ++(*pnumwritten);
}

#else  /* CPRFLAG */

LOCAL(void) write_char (
    _TCHAR ch,
    miniFILE *f,
    int *pnumwritten
    )
{
    if ( (f->_flag & _IOSTRG) && f->_base == NULL)
    {
        ++(*pnumwritten);
        return;
    }
#ifdef _UNICODE
    if (_putwc_nolock(ch, f) == WEOF)
#else  /* _UNICODE */
    if (_putc_nolock(ch, f) == EOF)
#endif  /* _UNICODE */
        *pnumwritten = -1;
    else
        ++(*pnumwritten);
}

#endif  /* CPRFLAG */

/***
*void write_multi_char(char ch, int num, int *pnumwritten)
*ifdef _UNICODE
*void write_multi_char(wchar_t ch, int num, FILE *f, int *pnumwritten)
*endif
*void write_multi_char(char ch, int num, FILE *f, int *pnumwritten)
*
*Purpose:
*   Writes num copies of a character to the given file/console.  If no error occurs,
*   then *pnumwritten is incremented by num; otherwise, *pnumwritten is set
*   to -1.  If num is negative, it is treated as zero.
*
*Entry:
*   _TCHAR ch        - character to write
*   int num          - number of times to write the characters
*   FILE *f          - file to write to
*   int *pnumwritten - pointer to integer to update with total chars written
*
*Exit:
*   No return value.
*
*Exceptions:
*
*******************************************************************************/

#ifdef CPRFLAG
LOCAL(void) write_multi_char (
    _TCHAR ch,
    int num,
    int *pnumwritten
    )
{
    while (num-- > 0) {
        write_char(ch, pnumwritten);
        if (*pnumwritten == -1)
            break;
    }
}

#else  /* CPRFLAG */

LOCAL(void) write_multi_char (
    _TCHAR ch,
    int num,
    miniFILE *f,
    int *pnumwritten
    )
{
    while (num-- > 0) {
        write_char(ch, f, pnumwritten);
        if (*pnumwritten == -1)
            break;
    }
}

#endif  /* CPRFLAG */

/***
*void write_string(const char *string, int len, int *pnumwritten)
*void write_string(const char *string, int len, FILE *f, int *pnumwritten)
*ifdef _UNICODE
*void write_string(const wchar_t *string, int len, FILE *f, int *pnumwritten)
*endif
*
*Purpose:
*   Writes a string of the given length to the given file.  If no error occurs,
*   then *pnumwritten is incremented by len; otherwise, *pnumwritten is set
*   to -1.  If len is negative, it is treated as zero.
*
*Entry:
*   _TCHAR *string   - string to write (NOT null-terminated)
*   int len          - length of string
*   FILE *f          - file to write to
*   int *pnumwritten - pointer to integer to update with total chars written
*
*Exit:
*   No return value.
*
*Exceptions:
*
*******************************************************************************/

#ifdef CPRFLAG

LOCAL(void) write_string (
    const _TCHAR *string,
    int len,
    int *pnumwritten
    )
{
    while (len-- > 0) {
        write_char(*string++, pnumwritten);
        if (*pnumwritten == -1)
        {
            if (errno == EILSEQ)
                write_char(_T('?'), pnumwritten);
            else
                break;
        }
    }
}

#else  /* CPRFLAG */

LOCAL(void) write_string (
    const _TCHAR *string,
    int len,
    miniFILE *f,
    int *pnumwritten
    )
{
    if ( (f->_flag & _IOSTRG) && f->_base == NULL)
    {
        (*pnumwritten) += len;
        return;
    }
    while (len-- > 0) {
        write_char(*string++, f, pnumwritten);
        if (*pnumwritten == -1)
        {
            if (errno == EILSEQ)
                write_char(_T('?'), f, pnumwritten);
            else
                break;
        }
    }
}
#endif  /* CPRFLAG */

// For GCC 64 bit, we can't cast to va_list *, so we need to make these functions defines.
#ifndef __GNUC_VA_LIST

/***
*int get_int_arg(va_list *pargptr)
*
*Purpose:
*   Gets an int argument off the given argument list and updates *pargptr.
*
*Entry:
*   va_list *pargptr - pointer to argument list; updated by function
*
*Exit:
*   Returns the integer argument read from the argument list.
*
*Exceptions:
*
*******************************************************************************/

__inline int __cdecl get_int_arg (
    va_list *pargptr
    )
{
    return va_arg(*pargptr, int);
}

/***
*long get_long_arg(va_list *pargptr)
*
*Purpose:
*   Gets an long argument off the given argument list and updates *pargptr.
*
*Entry:
*   va_list *pargptr - pointer to argument list; updated by function
*
*Exit:
*   Returns the long argument read from the argument list.
*
*Exceptions:
*
*******************************************************************************/

#if !LONG_IS_INT
__inline long __cdecl get_long_arg (
    va_list *pargptr
    )
{
    return va_arg(*pargptr, long);
}
#endif  /* !LONG_IS_INT */

#if !LONGLONG_IS_INT64
__inline long long __cdecl get_long_long_arg (
    va_list *pargptr
    )
{
    return va_arg(*pargptr, long long);
}
#endif  /* !LONGLONG_IS_INT64 */

#if _INTEGRAL_MAX_BITS >= 64   
__inline __int64 __cdecl get_int64_arg (
    va_list *pargptr
    )
{
    return va_arg(*pargptr, __int64);
}
#endif  /* _INTEGRAL_MAX_BITS >= 64    */

#ifndef _UNICODE

#ifdef POSITIONAL_PARAMETERS
#if !LONGDOUBLE_IS_DOUBLE
__inline _LONGDOUBLE __cdecl get_longdouble_arg(va_list *pargptr)
{
    return va_arg(*pargptr, _LONGDOUBLE);
}
#else  /* !LONGDOUBLE_IS_DOUBLE */
__inline _CRT_DOUBLE __cdecl get_crtdouble_arg(va_list *pargptr)
{
    return va_arg(*pargptr, _CRT_DOUBLE);
}
#endif  /* !LONGDOUBLE_IS_DOUBLE */
#endif  /* POSITIONAL_PARAMETERS */
#endif  /* _UNICODE */

#endif // __GNUC_VA_LIST

#ifndef _SAFECRT_IMPL
}
#endif  /* _SAFECRT_IMPL */

