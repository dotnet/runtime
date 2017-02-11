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
    
#ifdef FEATURE_CORESYSTEM
    // CoreSystem does not support this.
    return FALSE;
#else
    // Because of how C++ generates code, we must use default initialization to
    // 0 here.  Any explicit initialization will result in thread-safety problems.
    static BOOL fInited;
    static BOOL fShouldCheck;
    static BOOL fShouldCheck_ForMDA;

    if (VolatileLoad(&fInited) == FALSE)
    {
        fShouldCheck_ForMDA = FALSE;

        fShouldCheck = AuxUlibInitialize();      // may fail

#ifdef MDA_SUPPORTED
        if (fShouldCheck)
        {
            MdaLoaderLock* pProbe = MDA_GET_ASSISTANT(LoaderLock);
            if (pProbe)
                fShouldCheck_ForMDA = TRUE;
        }
#endif // MDA_SUPPORTED
        VolatileStore(&fInited, TRUE);
    }
    return (fForMDA ? fShouldCheck_ForMDA : fShouldCheck);
#endif // FEATURE_CORESYSTEM
}
