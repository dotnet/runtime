// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
