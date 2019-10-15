// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

/*============================================================
**
** Header:  IJupiterObject.h
**
**
** Purpose: Defines headers used in RCW walk
** 
==============================================================*/

#ifndef _H_JUPITER_OBJECT_
#define _H_JUPITER_OBJECT_

#ifdef FEATURE_COMINTEROP

#include "internalunknownimpl.h"

class ICCW;

// Windows.UI.Xaml.Hosting.IReferenceTrackerHost
class DECLSPEC_UUID("29a71c6a-3c42-4416-a39d-e2825a07a773") ICLRServices : public IUnknown
{
public :
    STDMETHOD(GarbageCollect)(DWORD dwFlags) = 0;
    STDMETHOD(FinalizerThreadWait)() = 0;
    STDMETHOD(DisconnectRCWsInCurrentApartment)() = 0;
    STDMETHOD(CreateManagedReference)(IUnknown *pJupiterObject, ICCW **ppNewReference) = 0;
    STDMETHOD(AddMemoryPressure)(UINT64 bytesAllocated) = 0;
    STDMETHOD(RemoveMemoryPressure)(UINT64 bytesAllocated) = 0;
};

// Windows.UI.Xaml.Hosting.IReferenceTrackerManager
class DECLSPEC_UUID("3cf184b4-7ccb-4dda-8455-7e6ce99a3298") IJupiterGCManager : public IUnknown
{
public :
    STDMETHOD(OnGCStarted)() = 0;
    STDMETHOD(OnRCWWalkFinished)(BOOL bWalkFailed) = 0;
    STDMETHOD(OnGCFinished)() = 0;
    STDMETHOD(SetCLRServices)(/* [in] */ ICLRServices *pCLRServices) = 0;
};

// Windows.UI.Xaml.Hosting.IReferenceTrackerTarget
class DECLSPEC_UUID("64bd43f8-bfee-4ec4-b7eb-2935158dae21") ICCW : public IUnknown
{

public :
    STDMETHOD_(ULONG, AddReferenceFromJupiter)() = 0;    
    STDMETHOD_(ULONG, ReleaseFromJupiter)() = 0;
    STDMETHOD(Peg)() = 0;
    STDMETHOD(Unpeg)() = 0;
};

// Windows.UI.Xaml.Hosting.IFindReferenceTargetsCallback
class DECLSPEC_UUID("04b3486c-4687-4229-8d14-505ab584dd88") IFindDependentWrappersCallback : public IUnknown
{

public :
    STDMETHOD(OnFoundDependentWrapper)(/* [in] */ ICCW *pCCW) = 0;
};

// Windows.UI.Xaml.Hosting.IReferenceTracker
class DECLSPEC_UUID("11d3b13a-180e-4789-a8be-7712882893e6") IJupiterObject : public IUnknown
{
public :
    STDMETHOD(Connect)() = 0;           // Notify Jupiter that we've created a new RCW
    STDMETHOD(Disconnect)() = 0;        // Notify Jupiter that we are about to destroy this RCW
    STDMETHOD(FindDependentWrappers)(/* [in] */ IFindDependentWrappersCallback *pCallback) = 0;
                                        // Find list of dependent CCWs for this RCW
    STDMETHOD(GetJupiterGCManager)(/* [out, retval] */ IJupiterGCManager **ppJupiterGCManager) = 0;
                                        // Retrieve IJupiterGCManager interface
    STDMETHOD(AfterAddRef)() = 0;       // Notify Jupiter that we've done a AddRef()
    STDMETHOD(BeforeRelease)() = 0;     // Notify Jupiter that we are about to do a Release()
    STDMETHOD(Peg)() = 0;               // Notify Jupiter that we've returned this IJupiterObject as
                                        // [out] parameter and this IJupiterObject object including 
                                        // all its reachable CCWs should be pegged
};

extern const IID IID_ICCW;
extern const IID IID_IJupiterObject;
extern const IID IID_IJupiterGCManager;
extern const IID IID_IFindDependentWrappersCallback;

#endif // FEATURE_COMINTEROP

#endif // _H_JUPITER_OBJECT_
