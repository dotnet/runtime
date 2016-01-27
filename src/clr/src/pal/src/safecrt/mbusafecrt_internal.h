// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*   mbusafecrt_internal.h - internal declarations for SafeCRT functions
*

*
*   Purpose:
*       This file contains the internal declarations SafeCRT
*       functions ported to MacOS. These are the safe versions of
*       functions standard functions banned by SWI
****/

/* shields! */

#ifndef MBUSAFECRT_INTERNAL_H
#define MBUSAFECRT_INTERNAL_H

#include "pal_char16.h"
#include "pal_mstypes.h"

typedef __builtin_va_list va_list;

// The ifdef below are to accommodate Unix build
// that complains about them being declared in stdarg.h already.
#ifndef va_start
#define va_start __builtin_va_start
#endif
#ifndef va_end
#define va_end __builtin_va_end
#endif

#include "mbusafecrt.h"

#ifdef EOF
#undef EOF
#endif
#define EOF -1

#ifdef WEOF
#undef WEOF
#endif
#define WEOF -1

#define CASSERT(p) extern int sanity_check_dummy[1+((!(p))*(-2))];

extern tSafeCRT_AssertFuncPtr sMBUSafeCRTAssertFunc;

typedef struct miniFILE_struct
{
    char* _ptr;
    int _cnt;
    char* _base;
    int _flag;
} miniFILE;

#define _IOSTRG 1
#define _IOWRT 2
#define _IOREAD 4
#define _IOMYBUF 8

int _putc_nolock( char inChar, miniFILE* inStream );
int _putwc_nolock( wchar_t inChar, miniFILE* inStream );
int _getc_nolock( miniFILE* inStream );
int _getwc_nolock( miniFILE* inStream );
int _ungetc_nolock( char inChar, miniFILE* inStream );
int _ungetwc_nolock( wchar_t inChar, miniFILE* inStream );

errno_t _safecrt_cfltcvt(double *arg, char *buffer, size_t sizeInBytes, int type, int precision, int flags);

void _safecrt_fassign(int flag, void* argument, char * number );
void _safecrt_wfassign(int flag, void* argument, wchar_t * number );

int _minimal_chartowchar( wchar_t* outWChar, const char* inChar );

int _output_s( miniFILE* outfile, const char* _Format, va_list _ArgList);
int _woutput_s( miniFILE* outfile, const wchar_t* _Format, va_list _ArgList);
int _output( miniFILE *outfile, const char* _Format, va_list _ArgList);

int _soutput_s( char *_Dst, size_t _Size, const char *_Format, va_list _ArgList );
int _swoutput_s( wchar_t *_Dst, size_t _Size, const wchar_t *_Format, va_list _ArgList );

int __tinput_s( miniFILE* inFile, const unsigned char * inFormat, va_list inArgList );
int __twinput_s( miniFILE* inFile, const wchar_t * inFormat, va_list inArgList );

#endif  /* MBUSAFECRT_INTERNAL_H */
