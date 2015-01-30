//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    thread.c

Abstract:

    Implementation of the threads/process functions in the C runtime library
    that are Windows specific.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/init.h"

SET_DEFAULT_DEBUG_CHANNEL(CRT);

void
PAL_exit(int status)
{
    PERF_ENTRY(exit);
    ENTRY ("exit(status=%d)\n", status);

    /* should also clean up any resources allocated by pal/cruntime, if any */
    ExitProcess(status);

    LOGEXIT ("exit returns void");
    PERF_EXIT(exit);
}

int
PAL_atexit(void (__cdecl *function)(void))
{
    int ret;
    
    PERF_ENTRY(atexit);
    ENTRY ("atexit(function=%p)\n", function);
    ret = atexit(function);
    LOGEXIT ("atexit returns int %d", ret);
    PERF_EXIT(atexit);
    return ret;
}
