// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    cruntime/misctls.ccpp

Abstract:

    Implementation of C runtime functions that don't fit anywhere else
    and depend on per-thread data



--*/

#include "pal/thread.hpp"
#include "pal/palinternal.h"

extern "C"
{
#include "pal/dbgmsg.h"
#include "pal/misc.h"
}

#include <errno.h>
/* <stdarg.h> needs to be included after "palinternal.h" to avoid name
   collision for va_start and va_end */
#include <stdarg.h>
#include <time.h>
#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif  // HAVE_CRT_EXTERNS_H

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:

    localtime

See MSDN for more details.
--*/

struct PAL_tm *
__cdecl
PAL_localtime(const PAL_time_t *clock)
{
    CPalThread *pThread = NULL;
    struct tm tmpResult;
    struct PAL_tm *result = NULL;

    PERF_ENTRY(localtime);
    ENTRY( "localtime( clock=%p )\n",clock );

    /* Get the per-thread buffer from the thread structure. */
    pThread = InternalGetCurrentThread();

    result = &pThread->crtInfo.localtimeBuffer;

    localtime_r(reinterpret_cast<const time_t*>(clock), &tmpResult);

    // Copy the result into the Windows struct.
    result->tm_sec = tmpResult.tm_sec;
    result->tm_min = tmpResult.tm_min;
    result->tm_hour = tmpResult.tm_hour;
    result->tm_mday = tmpResult.tm_mday;
    result->tm_mon  = tmpResult.tm_mon;
    result->tm_year = tmpResult.tm_year;
    result->tm_wday = tmpResult.tm_wday;
    result->tm_yday = tmpResult.tm_yday;
    result->tm_isdst = tmpResult.tm_isdst;

    LOGEXIT( "localtime returned %p\n", result );
    PERF_EXIT(localtime);

    return result;
}

/*++
Function:

    ctime

    There appears to be a difference between the FreeBSD and windows
    implementations.  FreeBSD gives Wed Dec 31 18:59:59 1969 for a
    -1 param, and Windows returns NULL

See MSDN for more details.
--*/
char *
__cdecl
PAL_ctime( const PAL_time_t *clock )
{
    CPalThread *pThread = NULL;
    char * retval = NULL;

    PERF_ENTRY(ctime);
    ENTRY( "ctime( clock=%p )\n",clock );
    if(*clock < 0)
    {
        /*If the input param is less than zero the value
         *returned is less than the Unix epoch
         *1st of January 1970*/
        WARN("The input param is less than zero");
        goto done;
    }

    /* Get the per-thread buffer from the thread structure. */
    pThread = InternalGetCurrentThread();

    retval = pThread->crtInfo.ctimeBuffer;

    ctime_r(reinterpret_cast<const time_t*>(clock),retval);

done:

    LOGEXIT( "ctime() returning %p (%s)\n",retval,retval);
    PERF_EXIT(ctime);

    return retval;
}