// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    printfcpp.cpp

Abstract:

    Implementation of suspension safe printf functions.

Revision History:



--*/

#include "pal/corunix.hpp"
#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/file.hpp"
#include "pal/printfcpp.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/cruntime.h"

#include <errno.h>

SET_DEFAULT_DEBUG_CHANNEL(CRT);

extern "C"
{

// Forward declare functions that are in header files we can't include yet
int vfprintf(FILE* stream, const char* format, va_list ap);

/*++
Function:
  PAL_fprintf

See MSDN doc.
--*/
int
__cdecl
PAL_fprintf(PAL_FILE *stream,const char *format,...)
{
    LONG Length = 0;
    va_list ap;

    PERF_ENTRY(fprintf);
    ENTRY("PAL_fprintf(stream=%p,format=%p (%s))\n",stream, format, format);

    va_start(ap, format);
    Length = vfprintf(stream->bsdFilePtr, format, ap);
    va_end(ap);

    LOGEXIT("PAL_fprintf returns int %d\n", Length);
    PERF_EXIT(fprintf);
    return Length;
}

/*******************************************************************************
Function:
  PAL_vfprintf

Parameters:
  stream
    - out stream
  Format
    - format string
  ap
    - stdarg parameter list
*******************************************************************************/

int __cdecl PAL_vfprintf(PAL_FILE *stream, const char *format, va_list ap)
{
    return vfprintf(stream->bsdFilePtr, format, ap);
}

} // end extern "C"
