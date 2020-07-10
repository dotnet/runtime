// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    modulename.cpp

Abstract:

    Implementation of internal functions to get module names



--*/

#include "pal/thread.hpp"
#include "pal/malloc.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/modulename.h"

#include <dlfcn.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(LOADER);

/*++
    PAL_dladdr

    Internal wrapper for dladder used only to get module name

Parameters:
    LPVOID ProcAddress: a pointer to a function in a shared library

Return value:
    Pointer to string with the fullpath to the shared library containing
    ProcAddress.

    NULL if error occurred.

Notes:
    The string returned by this function is owned by the OS.
    If you need to keep it, strdup() it, because it is unknown how long
    this ptr will point at the string you want (over the lifetime of
    the system running)  It is only safe to use it immediately after calling
    this function.
--*/
const char *PAL_dladdr(LPVOID ProcAddress)
{
    Dl_info dl_info;
    if (!dladdr(ProcAddress, &dl_info))
    {
        WARN("dladdr() call failed!\n");
        /* If we get an error, return NULL */
        return (NULL);
    }
    else
    {
        /* Return the module name */
        return dl_info.dli_fname;
    }
}

