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

// On some platforms, we can detect whether the current thread holds the loader
// lock.  It is unsafe to execute managed code when this is the case
BOOL ShouldCheckLoaderLock()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
#ifdef FEATURE_CORESYSTEM
    // CoreSystem does not support this.
    return FALSE;
#else
    // Because of how C++ generates code, we must use default initialization to
    // 0 here.  Any explicit initialization will result in thread-safety problems.
    static BOOL fInited;
    static BOOL fShouldCheck;

    if (VolatileLoad(&fInited) == FALSE)
    {
        fShouldCheck = AuxUlibInitialize();      // may fail

        VolatileStore(&fInited, TRUE);
    }
    return (fShouldCheck);
#endif // FEATURE_CORESYSTEM
}
