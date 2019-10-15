// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    tracepointprovider.cpp

Abstract:

    Trace point provider support

Revision History:

--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/process.h"
#include "pal/module.h"
#include "pal/malloc.hpp"
#include "pal/stackstring.hpp"

#include <errno.h>
#include <unistd.h> 
#include <pthread.h>
#include <dlfcn.h>

SET_DEFAULT_DEBUG_CHANNEL(MISC);

/*++

Initialization logic for LTTng tracepoint providers.

--*/
#if defined(__linux__)

static const char tpLibName[] = "libcoreclrtraceptprovider.so";


/*++

NOTE: PAL_InitializeTracing MUST NOT depend on anything in the PAL itself
as it is called prior to PAL initialization.

Constructor priority is set to 200, which allows for constructors to
guarantee that they run before or after this constructor by setting
their priority appropriately.

Priority values must be greater than 100.  The lower the value,
the higher the priority.

--*/
__attribute__((__unused__))
__attribute__((constructor (200)))
static void
PAL_InitializeTracing(void)
{
    int fShouldLoad = 1;
    // Check if loading the LTTng providers should be disabled.
    // Note: this env var is formally declared in clrconfigvalues.h, but
    // this code is executed too early to use the mechanics that come with that definition.
    char *disableValue = getenv("COMPlus_LTTng");
    if (disableValue != NULL)
    {
        fShouldLoad = strtol(disableValue, NULL, 10);
    }

    // Get the path to the currently executing shared object (libcoreclr.so).
    Dl_info info;
    int succeeded = dladdr((void *)PAL_InitializeTracing, &info);
    if(!succeeded)
    {
        return;
    }

    // Copy the path and modify the shared object name to be the tracepoint provider.
    PathCharString tpProvPath;
    int pathLen = strlen(info.dli_fname);

    // Find the length of the full path without the shared object name, including the trailing slash.
    int lastTrailingSlashLen = -1;
    for(int i=pathLen-1; i>=0; i--)
    {
        if(info.dli_fname[i] == '/')
        {
            lastTrailingSlashLen = i+1;
            break;
        }
    }

    // Make sure we found the last trailing slash.
    if(lastTrailingSlashLen == -1)
    {
        return;
    }
    
    SIZE_T tpLibNameLen = strlen(tpLibName);

    if( !tpProvPath.Reserve(tpLibNameLen + lastTrailingSlashLen) ||  
    // Copy the path without the shared object name.
        !tpProvPath.Append(info.dli_fname, lastTrailingSlashLen) ||
    // Append the shared object name for the tracepoint provider.
        !tpProvPath.Append(tpLibName, tpLibNameLen))
    {
        return;
    }
    
    
    if (fShouldLoad)
    {
        // Load the tracepoint provider.
        // It's OK if this fails - that just means that tracing dependencies aren't available.
        dlopen(tpProvPath, RTLD_NOW | RTLD_GLOBAL);
    }
}

#endif
