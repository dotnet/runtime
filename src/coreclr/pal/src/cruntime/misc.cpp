// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    cruntime/misc.cpp

Abstract:

    Implementation of C runtime functions that don't fit anywhere else.



--*/

#include "pal/thread.hpp"
#include "pal/threadsusp.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/misc.h"

#include <errno.h>
/* <stdarg.h> needs to be included after "palinternal.h" to avoid name
   collision for va_start and va_end */
#include <stdarg.h>
#include <time.h>
#include <limits.h>

#if defined(HOST_AMD64) || defined(_x86_)
#include <xmmintrin.h>
#endif // defined(HOST_AMD64) || defined(_x86_)
#if defined(_DEBUG)
#include <assert.h>
#endif //defined(_DEBUG)

SET_DEFAULT_DEBUG_CHANNEL(CRT);

using namespace CorUnix;

/*++
Function:
  _gcvt_s

See MSDN doc.
--*/
char *
__cdecl
_gcvt_s( char * buffer, int iSize, double value, int digits )
{
    PERF_ENTRY(_gcvt);
    ENTRY( "_gcvt( value:%f digits=%d, buffer=%p )\n", value, digits, buffer );

    if ( !buffer )
    {
        ERROR( "buffer was an invalid pointer.\n" );
    }

    switch ( digits )
    {
    case 7 :
        /* Fall through */
    case 8 :
        /* Fall through */
    case 15 :
        /* Fall through */
    case 17 :

        sprintf_s( buffer, iSize, "%.*g", digits, value );
        break;

    default :
        ASSERT( "Only the digits 7, 8, 15, and 17 are valid.\n" );
        *buffer = '\0';
    }

    LOGEXIT( "_gcvt returns %p (%s)\n", buffer , buffer );
    PERF_EXIT(_gcvt);
    return buffer;
}


/*++
Function :

    __iscsym

See MSDN for more details.
--*/
int
__cdecl
__iscsym( int c )
{
    PERF_ENTRY(__iscsym);
    ENTRY( "__iscsym( c=%d )\n", c );

    if ( isalnum( c ) || c == '_'  )
    {
        LOGEXIT( "__iscsym returning 1\n" );
        PERF_EXIT(__iscsym);
        return 1;
    }

    LOGEXIT( "__iscsym returning 0\n" );
    PERF_EXIT(__iscsym);
    return 0;
}
/*++

PAL forwarders for standard macro headers.

--*/
PALIMPORT DLLEXPORT int * __cdecl PAL_errno()
{
    return &errno;
}

extern "C" PALIMPORT DLLEXPORT FILE* __cdecl PAL_stdout()
{
    return stdout;
}

extern "C" PALIMPORT DLLEXPORT FILE* __cdecl PAL_stdin()
{
    return stdin;
}

extern "C" PALIMPORT DLLEXPORT FILE* __cdecl PAL_stderr()
{
    return stderr;
}
