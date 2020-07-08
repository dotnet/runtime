// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    stringtls.cpp

Abstract:

    Implementation of the string functions in the C runtime library that
    are Windows specific and depend on per-thread data



--*/

#include "pal/thread.hpp"
#include "pal/dbgmsg.h"

#include <string.h>
#include <ctype.h>
#include <pthread.h>
#include <limits.h>
#include <unistd.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:
   PAL_strtok

Finds the next token in a string.

Return value:

A pointer to the next token found in strToken.  Returns NULL when no more
tokens are found.  Each call modifies strToken by substituting a NULL
character for each delimiter that is encountered.

Parameters:
strToken        String cotaining token(s)
strDelimit      Set of delimiter characters

Remarks:
In FreeBSD, strtok is not re-entrant, strtok_r is.  It manages re-entrancy
by using a passed-in context pointer (which will be stored in thread local
storage)  According to the strtok MSDN documentation, "Calling these functions
simultaneously from multiple threads does not have undesirable effects", so
we need to use strtok_r.
--*/
char *
__cdecl
PAL_strtok(char *strToken, const char *strDelimit)
{
    CPalThread *pThread = NULL;
    char *retval=NULL;

    PERF_ENTRY(strtok);
    ENTRY("strtok (strToken=%p (%s), strDelimit=%p (%s))\n",
          strToken?strToken:"NULL",
          strToken?strToken:"NULL", strDelimit?strDelimit:"NULL", strDelimit?strDelimit:"NULL");

    pThread = InternalGetCurrentThread();

    retval = strtok_r(strToken, strDelimit, &pThread->crtInfo.strtokContext);

    LOGEXIT("strtok returns %p (%s)\n", retval?retval:"NULL", retval?retval:"NULL");
    PERF_EXIT(strtok);

    return retval;
}
