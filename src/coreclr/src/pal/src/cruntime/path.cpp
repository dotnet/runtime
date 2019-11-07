// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    path.c

Abstract:

    Implementation of path functions part of Windows runtime library.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/printfcpp.hpp"

#include <string.h>
#include <stdlib.h>
#include <sys/param.h>
#include <errno.h>
#include <limits.h>

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:
  _fullpath

See MSDN doc.

--*/
char *
__cdecl
_fullpath(
          char *absPath,
          const char *relPath,
          size_t maxLength)
{
    char realpath_buf[PATH_MAX+1];
    char path_copy[PATH_MAX+1];
    char *retval = NULL;
    DWORD cPathCopy = sizeof(path_copy)/sizeof(path_copy[0]);
    size_t min_length;
    BOOL fBufAllocated = FALSE;

    PERF_ENTRY(_fullpath);
    ENTRY("_fullpath (absPath=%p, relPath=%p (%s), maxLength = %lu)\n",
          absPath, relPath ? relPath:"NULL", relPath ? relPath:"NULL", maxLength);

    if (strncpy_s(path_copy, sizeof(path_copy), relPath ? relPath : ".", cPathCopy) != SAFECRT_SUCCESS)
    {
        TRACE("_fullpath: strncpy_s failed!\n");
        goto fullpathExit;
    }

    FILEDosToUnixPathA(path_copy);

    if(NULL == realpath(path_copy, realpath_buf))
    {
        ERROR("realpath() failed; problem path is '%s'. errno is %d (%s)\n",
                realpath_buf, errno, strerror(errno));
        goto fullpathExit;
    }

    TRACE("real path is %s\n", realpath_buf);
    min_length = strlen(realpath_buf)+1; // +1 for the NULL terminator

    if(NULL == absPath)
    {
        absPath = static_cast<char *>(
            PAL_malloc(_MAX_PATH * sizeof(char)));
        if (!absPath)
        {
            ERROR("PAL_malloc failed with error %d\n", errno);
            goto fullpathExit;
        }
        maxLength = _MAX_PATH;
        fBufAllocated = TRUE;
    }

    if(min_length > maxLength)
    {
        ERROR("maxLength is %lu, we need at least %lu\n",
                maxLength, min_length);
        if (fBufAllocated)
        {
            PAL_free(absPath);
            fBufAllocated = FALSE;
        }
        goto fullpathExit;
    }

    strcpy_s(absPath, maxLength, realpath_buf);
    retval = absPath;

fullpathExit:
    LOGEXIT("_fullpath returns char * %p\n", retval);
    PERF_EXIT(_fullpath);
    return retval;
}



