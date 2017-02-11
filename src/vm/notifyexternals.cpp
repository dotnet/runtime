// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: notifyexternals.cpp
// 

// ===========================================================================


#include "common.h"

#include "excep.h"
#include "interoputil.h"
#include "comcache.h"

#include "notifyexternals.h"
#include "mdaassistants.h"

// On some platforms, we can detect whether the current thread holds the loader
// lock.  It is unsafe to execute managed code when this is the case
BOOL ShouldCheckLoaderLock(BOOL fForMDA /*= TRUE*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
    // CoreSystem does not support this.
    return FALSE;
}
