// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

/*============================================================
**
** Header:  RCWWalker.h
**
**
** Purpose: Solve native/manage cyclic reference issue by
** walking RCW objects
**
==============================================================*/

#ifndef _H_RCWWALKER_
#define _H_RCWWALKER_

#ifdef FEATURE_COMINTEROP

#include "internalunknownimpl.h"
#include "utilcode.h"
#include "runtimecallablewrapper.h"


//
// RCW Walker
// Walks jupiter RCW objects and create references from RCW to referenced CCW (in native side)
//
class RCWWalker
{
    friend struct _DacGlobals;

private :
    static VolatilePtr<IJupiterGCManager>  s_pGCManager;            // The one and only GCManager instance
    static BOOL                     s_bGCStarted;                   // Has GC started?
    SVAL_DECL(BOOL,                 s_bIsGlobalPeggingOn);           // Do we need to peg every CCW?

public :
#ifndef DACCESS_COMPILE
    static void OnJupiterRCWCreated(RCW *pRCW, IJupiterObject *pJupiterObject);
    static void AfterJupiterRCWCreated(RCW *pRCW);
    static void BeforeJupiterRCWDestroyed(RCW *pRCW);
    static void OnEEShutdown();

    //
    // Send out a AddRefFromTrackerSource callback to notify Jupiter we've done a AddRef
    // We should do this *after* we made a AddRef because we should never
    // be in a state where reported refs > actual refs
    //
    FORCEINLINE static void AfterInterfaceAddRef(RCW *pRCW)
    {

        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        IJupiterObject *pJupiterObject = pRCW->GetJupiterObject();
        if (pJupiterObject)
        {
            STRESS_LOG2(LF_INTEROP, LL_INFO100, "[RCW Walker] Calling IJupiterObject::AddRefFromTrackerSource (IJupiterObject = 0x%p, RCW = 0x%p)\n", pJupiterObject, pRCW);
            pJupiterObject->AddRefFromTrackerSource();
        }
    }

    //
    // Send out ReleaseFromTrackerSource callback for every cached interface pointer
    // This needs to be made before call Release because we should never be in a
    // state that reported refs > actual refs
    //
    FORCEINLINE static void BeforeInterfaceRelease(RCW *pRCW)
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        IJupiterObject *pJupiterObject = pRCW->GetJupiterObject();
        if (pJupiterObject)
        {
            STRESS_LOG2(LF_INTEROP, LL_INFO100, "[RCW Walker] Calling IJupiterObject::ReleaseFromTrackerSource before Release (IJupiterObject = 0x%p, RCW = 0x%p)\n", pJupiterObject, pRCW);
            pJupiterObject->ReleaseFromTrackerSource();
        }
    }


#endif // !DACCESS_COMPILE


public :
    //
    // Called in ComCallableWrapper::IsWrapperActive
    // Used to override the individual pegging flag on CCWs and force pegging every jupiter referenced CCW
    // See IsWrapperActive for more details
    //
    static FORCEINLINE BOOL IsGlobalPeggingOn()
    {
        // We need this weird cast because s_bIsGlobalPeggingOn is used in DAC and defined as
        // __GlobalVal in DAC build
        // C++'s operator magic didn't work if two levels of operator overloading are involved...
        return VolatileLoad((BOOL *)&s_bIsGlobalPeggingOn);
    }

#ifndef DACCESS_COMPILE
    //
    // Tells GC whether walking all the Jupiter RCW is necessary, which only should happen
    // if we have seen jupiter RCWs
    //
    static FORCEINLINE BOOL NeedToWalkRCWs()
    {
        LIMITED_METHOD_CONTRACT;

        return (((IJupiterGCManager *)s_pGCManager) != NULL);
    }

    //
    // Whether a GC has been started and we need to RCW walk
    //
    static FORCEINLINE BOOL HasGCStarted()
    {
        return s_bGCStarted;
    }

    //
    // Called when GC started
    // We do most of our work here
    //
    static void OnGCStarted(int nCondemnedGeneration);

    //
    // Called when GC finished
    //
    static void OnGCFinished(int nCondemnedGeneration);

private :
    static void OnGCStartedWorker();
    static void OnGCFinishedWorker();
    static void WalkRCWs();
    static HRESULT WalkOneRCW(RCW *pRCW, RCWRefCache *pRCWRefCache);
#endif // DACCESS_COMPILE
};

#endif // FEATURE_COMINTEROP

#endif // _H_RCWWALKER_
