// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

/*============================================================
**
** Class:  RCWWalker
**
**
** Purpose: The implementation of RCWWalker class which walks
** RCW objects
===========================================================*/

#include "common.h"

#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#include "rcwwalker.h"
#include "olecontexthelpers.h"
#include "rcwrefcache.h"
#include "cominterfacemarshaler.h"
#include "excep.h"
#include "finalizerthread.h"
#include "interoputil.inl"

const IID IID_ICLRServices = __uuidof(ICLRServices);

const IID IID_ICCW = __uuidof(ICCW);

const IID IID_IJupiterObject = __uuidof(IJupiterObject);

const IID IID_IJupiterGCManager = __uuidof(IJupiterGCManager);

const IID IID_IFindDependentWrappersCallback = __uuidof(IFindDependentWrappersCallback);

VolatilePtr<IJupiterGCManager>  RCWWalker::s_pGCManager         = NULL;     // Global GC manager pointer
BOOL                            RCWWalker::s_bGCStarted         = FALSE;    // Has GC started?
SVAL_IMPL_INIT(BOOL,            RCWWalker, s_bIsGlobalPeggingOn, TRUE);     // Do we need to peg every jupiter CCW?

#ifndef DACCESS_COMPILE

// Our implementation of ICLRServices provided to Jupiter via IJupiterGCManager::SetReferenceTrackerHost.
class CLRServicesImpl : public IUnknownCommon<ICLRServices, IID_ICLRServices>
{
private:
    // flags for DisconnectUnusedReferenceSources(DWORD dwFlags)
    enum {
        GC_FOR_APPX_SUSPEND = 0x00000001
    };
public:
    STDMETHOD(DisconnectUnusedReferenceSources)(DWORD dwFlags);
    STDMETHOD(ReleaseDisconnectedReferenceSources)();
    STDMETHOD(NotifyEndOfReferenceTrackingOnThread)();
    STDMETHOD(GetTrackerTarget)(IUnknown *pJupiterObject, ICCW **ppNewReference);
    STDMETHOD(AddMemoryPressure)(UINT64 bytesAllocated);
    STDMETHOD(RemoveMemoryPressure)(UINT64 bytesAllocated);
};

#pragma warning(push)
#pragma warning(disable : 4702) // Disable unreachable code warning for RCWWalker_UnhandledExceptionFilter

//
// We never expect exceptions to be thrown outside of RCWWalker
// So make sure we fail fast here, instead of going through normal
// exception processing and fail later
// This will make analyzing dumps much easier
//
inline LONG RCWWalker_UnhandledExceptionFilter(EXCEPTION_POINTERS* pExceptionPointers, PVOID pv)
{
    WRAPPER_NO_CONTRACT;

    if ((pExceptionPointers->ExceptionRecord->ExceptionCode == STATUS_BREAKPOINT) ||
         (pExceptionPointers->ExceptionRecord->ExceptionCode == STATUS_SINGLE_STEP))
    {
        // We don't want to fail fast on debugger exceptions
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Exceptions here are considered fatal - just fail fast
    EEPolicy::HandleFatalError(COR_E_EXECUTIONENGINE, (UINT_PTR)GetIP(pExceptionPointers->ContextRecord), NULL, pExceptionPointers);

    // We may trigger C4702 warning as we'll never reach here
    // I've temporarily disabled the warning. See #pragma above
    UNREACHABLE();

    return EXCEPTION_EXECUTE_HANDLER;
}

#pragma warning(pop)

//
// Release context-bound RCWs and Jupiter RCWs (which are free-threaded but context-bound)
// in the current apartment
//
STDMETHODIMP CLRServicesImpl::NotifyEndOfReferenceTrackingOnThread()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        ReleaseRCWsInCaches(GetCurrentCtxCookie());
    }
    END_EXTERNAL_ENTRYPOINT;
    return hr;
}

STDMETHODIMP CLRServicesImpl::DisconnectUnusedReferenceSources(DWORD dwFlags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());
        if (dwFlags & GC_FOR_APPX_SUSPEND) {
            GCHeapUtilities::GetGCHeap()->GarbageCollect(2, true, collection_blocking | collection_optimized);
        }
        else
            GCHeapUtilities::GetGCHeap()->GarbageCollect();
    }
    END_EXTERNAL_ENTRYPOINT;
    return hr;
}

STDMETHODIMP CLRServicesImpl::AddMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    HRESULT hr = S_OK;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCInterface::NewAddMemoryPressure(bytesAllocated);
    }
    END_EXTERNAL_ENTRYPOINT;
    return hr;
}

STDMETHODIMP CLRServicesImpl::RemoveMemoryPressure(UINT64 bytesAllocated)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    HRESULT hr = S_OK;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCInterface::NewRemoveMemoryPressure(bytesAllocated);
    }
    END_EXTERNAL_ENTRYPOINT;
    return hr;
}


STDMETHODIMP CLRServicesImpl::ReleaseDisconnectedReferenceSources()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        FinalizerThread::FinalizerThreadWait();
    }
    END_EXTERNAL_ENTRYPOINT;
    return hr;
}

//
// Creates a proxy object that points to the given RCW
// The proxy
// 1. Has a managed reference pointing to the RCW, and therefore forms a cycle that can be resolved by GC
// 2. Forwards data binding requests
// For example:
//
// Grid <---- RCW             Grid <------RCW
// | ^                         |              ^
// | |             Becomes     |              |
// v |                         v              |
// Rectangle                  Rectangle ----->Proxy
//
// Arguments
//   pTarget        - The identity IUnknown* where a RCW points to (Grid, in this case)
//                    Note that
//                    1) we can either create a new RCW or get back an old one from cache
//                    2) This pTarget could be a regular WinRT object (such as WinRT collection) for data binding
//  ppNewReference  - The ICCW* for the proxy created
//                    Jupiter will call ICCW to establish a jupiter reference
//
STDMETHODIMP CLRServicesImpl::GetTrackerTarget(IUnknown *pTarget, ICCW **ppNewReference)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pTarget));
        PRECONDITION(CheckPointer(ppNewReference));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        //
        // QI for IUnknown to get the identity unknown
        //
        SafeComHolderPreemp<IUnknown> pIdentity;
        IfFailThrow(SafeQueryInterfacePreemp(pTarget, IID_IUnknown, &pIdentity));

        //
        // Get RCW for pJupiterObject
        //
        COMInterfaceMarshaler marshaler;
        marshaler.Init(
            pIdentity,
            g_pBaseCOMObject,
            GET_THREAD(),
            RCW::CF_SupportsIInspectable            // Returns a WinRT RCW
            );

        //
        // Then create a proxy based on the RCW
        //
        {
            GCX_COOP();

            struct _gc {
                    OBJECTREF  TargetObj;
                    OBJECTREF  RetVal;
            } gc;
            ZeroMemory(&gc, sizeof(gc));

            GCPROTECT_BEGIN(gc);

            gc.TargetObj = marshaler.FindOrCreateObjectRef(&pTarget);

            //
            // Figure out the right IVector<T1>/IVectorView<T2>
            //
            MethodTable *pMT = gc.TargetObj->GetMethodTable();

            TypeHandle thArgs[2];

            //
            // This RCW could be strongly typed - figure out T1/T2 using metadata
            //
            MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
            while (it.Next())
            {
                MethodTable *pItfMT = it.GetInterface();
                if (thArgs[0].IsNull() && pItfMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__ILISTGENERIC)))
                {
                    thArgs[0] = pItfMT->GetInstantiation()[0];

                    // Are we done?
                    if (!thArgs[1].IsNull())
                        break;
                }

                if (thArgs[1].IsNull() && pItfMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IREADONLYLISTGENERIC)))
                {
                    thArgs[1] = pItfMT->GetInstantiation()[0];

                    // Are we done?
                    if (!thArgs[0].IsNull())
                        break;
                }
            }

            if (thArgs[0].IsNull() || thArgs[1].IsNull())
            {
                //
                // Try the RCW cache if didn't find match for both types and this is a RCW
                //
                if (pMT->IsComObjectType())
                {
                    RCWHolder pRCW(GET_THREAD());
                    pRCW.Init(gc.TargetObj);

                    RCW::CachedInterfaceEntryIterator it = pRCW->IterateCachedInterfacePointers();
                    while (it.Next())
                    {
                        MethodTable *pItfMT = (MethodTable *)it.GetEntry()->m_pMT;

                        // Unfortunately the iterator could return NULL entry
                        if (pItfMT == NULL) continue;

                        if (thArgs[0].IsNull() && pItfMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__ILISTGENERIC)))
                        {
                            thArgs[0] = pItfMT->GetInstantiation()[0];

                            // Are we done?
                            if (!thArgs[1].IsNull())
                                break;
                        }

                        if (thArgs[1].IsNull() && pItfMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IREADONLYLISTGENERIC)))
                        {
                            thArgs[1] = pItfMT->GetInstantiation()[0];

                            // Are we done?
                            if (!thArgs[0].IsNull())
                                break;
                        }
                    }
                }
            }

            //
            // If not found, use object (IInspectable*) as the last resort
            //
            if (thArgs[0].IsNull())
                thArgs[0] = TypeHandle(g_pObjectClass);
            if (thArgs[1].IsNull())
                thArgs[1] = TypeHandle(g_pObjectClass);

            //
            // Instantiate ICustomPropertyProviderProxy<T1, T2>.CreateInstance
            //
            TypeHandle thCustomPropertyProviderProxy = TypeHandle(MscorlibBinder::GetClass(CLASS__ICUSTOMPROPERTYPROVIDERPROXY));

            MethodTable *pthCustomPropertyProviderProxyExactMT = thCustomPropertyProviderProxy.Instantiate(Instantiation(thArgs, 2)).GetMethodTable();

            MethodDesc *pCreateInstanceMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
                MscorlibBinder::GetMethod(METHOD__ICUSTOMPROPERTYPROVIDERPROXY__CREATE_INSTANCE),
                pthCustomPropertyProviderProxyExactMT,
                FALSE,
                Instantiation(),
                FALSE);

            //
            // Call ICustomPropertyProviderProxy.CreateInstance
            //
            PREPARE_NONVIRTUAL_CALLSITE_USING_METHODDESC(pCreateInstanceMD);
            DECLARE_ARGHOLDER_ARRAY(args, 1);
            args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(gc.TargetObj);

            CALL_MANAGED_METHOD_RETREF(gc.RetVal, OBJECTREF, args);

            CCWHolder pCCWHold = ComCallWrapper::InlineGetWrapper(&gc.RetVal);
            *ppNewReference = (ICCW *)ComCallWrapper::GetComIPFromCCW(pCCWHold, IID_ICCW, /* pIntfMT = */ NULL);
            GCPROTECT_END();
        }
    }
    END_EXTERNAL_ENTRYPOINT;
    return hr;
}

//
// Called when Jupiter RCW is being created
// We do one-time initialization for RCWWalker related stuff here
// This could throw
//
void RCWWalker::OnJupiterRCWCreated(RCW *pRCW, IJupiterObject *pJupiterObject)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pRCW));
        PRECONDITION(CheckPointer(pJupiterObject));
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::OnJupiterRCWCreated (RCW = 0x%p) BEGINS -----\n", pRCW));

    //
    // Retrieve IJupiterGCManager
    //
    if (!s_pGCManager)
    {
        SafeComHolderPreemp<IJupiterGCManager> pGCManager;
        HRESULT hr = pJupiterObject->GetReferenceTrackerManager(&pGCManager);
        if (SUCCEEDED(hr))
        {
            if (pGCManager == NULL)
            {
                LOG((LF_INTEROP, LL_INFO100, "\t[RCW Walker] ERROR: Failed to Retrieve IGCManager, IGCManager = NULL\n"));
                COMPlusThrowHR(E_POINTER);
            }

            //
            // Perform all operation that could fail here
            //
            NewHolder<CLRServicesImpl> pCLRServicesImpl = new CLRServicesImpl();
            ReleaseHolder<ICLRServices> pCLRServices;
            IfFailThrow(pCLRServicesImpl->QueryInterface(IID_ICLRServices, (void **)&pCLRServices));

            // Temporarily switch back to coop and disable GC to avoid racing with the very first RCW walk
            GCX_COOP();
            GCX_FORBID();

            if (FastInterlockCompareExchangePointer((IJupiterGCManager **)&s_pGCManager, (IJupiterGCManager *)pGCManager, NULL) == NULL)
            {
                //
                // OK. It is time to do our RCWWalker initialization
                // It's safe to do it here because we are in COOP and only one thread wins the race
                //
                LOG((LF_INTEROP, LL_INFO100, "\t[RCW Walker] Assigning RCWWalker::s_pIGCManager = 0x%p\n", (void *)pGCManager));

                pGCManager.SuppressRelease();
                pCLRServicesImpl.SuppressRelease();
                pCLRServices.SuppressRelease();

                LOG((LF_INTEROP, LL_INFO100, "\t[RCW Walker] Calling IGCManager::SetReferenceTrackerHost(0x%p)\n", (void *)pCLRServices));
                pGCManager->SetReferenceTrackerHost(pCLRServices);
            }
        }
        else
        {
            LOG((LF_INTEROP, LL_INFO100, "\t[RCW Walker] ERROR: Failed to Retrieve IGCManager, hr = 0x%x\n", hr));
            COMPlusThrowHR(hr);
        }
    }

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::OnJupiterRCWCreated (RCW = 0x%p) ENDS   ----- \n", pRCW));
}

//
// Called after Jupiter RCW has been created
// This should never throw
//
void RCWWalker::AfterJupiterRCWCreated(RCW *pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRCW));
        PRECONDITION(pRCW->IsJupiterObject());
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::AfterJupiterRCWCreated (RCW = 0x%p) BEGINS ----- \n", pRCW));

    IJupiterObject *pJupiterObject = pRCW->GetJupiterObject();

    //
    // Notify Jupiter that we've created a new RCW for this Jupiter object
    // To avoid surprises, we should notify them before we fire the first AddRefFromTrackerSource
    //
    STRESS_LOG2(LF_INTEROP, LL_INFO100, "[RCW Walker] Calling IJupiterObject::ConnectFromTrackerSource (IJupiterObject = 0x%p, RCW = 0x%p)\n", pJupiterObject, pRCW);
    pJupiterObject->ConnectFromTrackerSource();

    //
    // Send out AddRefFromTrackerSource callbacks to notify Jupiter we've done AddRef for certain interfaces
    // We should do this *after* we made a AddRef because we should never
    // be in a state where report refs > actual refs
    //

    // Send out AddRefFromTrackerSource for cached IUnknown
    RCWWalker::AfterInterfaceAddRef(pRCW);

    if (!pRCW->IsURTAggregated())
    {
        // Send out AddRefFromTrackerSource for cached IJupiterObject
        RCWWalker::AfterInterfaceAddRef(pRCW);
    }

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::AfterJupiterRCWCreated (RCW = 0x%p) ENDS   ----- \n", pRCW));
}

//
// Called before Jupiter RCW is about to be destroyed (the same lifetime as short weak handle)
//
void RCWWalker::BeforeJupiterRCWDestroyed(RCW *pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pRCW));
        PRECONDITION(pRCW->IsJupiterObject());
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::BeforeJupiterRCWDestroyed (RCW = 0x%p) BEGINS ----- \n", pRCW));

    IJupiterObject *pJupiterObject = pRCW->GetJupiterObject();
    _ASSERTE(pJupiterObject != NULL);

    //
    // Notify Jupiter that we are about to destroy a RCW (same timing as short weak handle)
    // for this Jupiter object.
    // They need this information to disconnect weak refs and stop firing events,
    // so that they can avoid resurrecting the Jupiter object (not the RCW - we prevent that)
    // We only call this inside GC, so don't need to switch to preemptive here
    // Ignore the failure as there is no way we can handle that failure during GC
    //
    STRESS_LOG2(LF_INTEROP, LL_INFO100, "[RCW Walker] Calling IJupiterObject::DisconnectFromTrackerSource (IJupiterObject = 0x%p, RCW = 0x%p)\n", pJupiterObject, pRCW);
    pJupiterObject->DisconnectFromTrackerSource();

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::BeforeJupiterRCWDestroyed (RCW = 0x%p) ENDS   ----- \n", pRCW));
}

//
// Walk all the jupiter RCWs in all AppDomains and build references from RCW -> CCW as we go
//
void RCWWalker::WalkRCWs()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    BOOL bWalkFailed = FALSE;

    HRESULT hr = S_OK;
    EX_TRY
    {
        {
            AppDomain *pDomain = ::GetAppDomain(); // There is only actually 1 AppDomain in CoreCLR, so no iterator

            RCWRefCache *pRCWRefCache = pDomain->GetRCWRefCache();
            _ASSERTE(pRCWRefCache != NULL);

            STRESS_LOG2(LF_INTEROP, LL_INFO100, "[RCW Walker] Walking all Jupiter RCWs in AppDomain 0x%p, RCWRefCache 0x%p\n", pDomain, pRCWRefCache);

            //
            // Reset the cache
            //
            pRCWRefCache->ResetDependentHandles();

            //
            // Enumerate all Jupiter RCWs in that AppDomain
            //
            hr = pRCWRefCache->EnumerateAllJupiterRCWs(RCWWalker::WalkOneRCW, pRCWRefCache);

            //
            // Shrink the dependent handle cache if necessary and clear unused handles.
            //
            pRCWRefCache->ShrinkDependentHandles();
        }
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    if (FAILED(hr))
    {
        // Remember the fact that we've failed and stop walking
        STRESS_LOG1(LF_INTEROP, LL_INFO100, "[RCW Walker] RCW walk failed, hr = 0x%p\n", hr);
        bWalkFailed = TRUE;

        STRESS_LOG0(LF_INTEROP, LL_INFO100, "[RCW Walker] Turning on global pegging flag as fail-safe\n");
        VolatileStore(&s_bIsGlobalPeggingOn, TRUE);
    }

    //
    // Let Jupiter know RCW walk is done and they need to:
    // 1. Unpeg all CCWs if the CCW needs to be unpegged (when the CCW is only reachable by other jupiter RCWs)
    // 2. Peg all CCWs if the CCW needs to be pegged (when the above condition is not true)
    // 3. Unlock reference cache when they are done
    //
    // If the walk has failed - Jupiter doesn't need to do anything and could just return immediately
    //
    // Note: IGCManager should be free-threaded as it will be called on arbitary threads
    //
    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] Calling IGCManager::FindTrackerTargetsCompleted on 0x%p, bWalkFailed = %d\n", s_pGCManager, bWalkFailed));
    _ASSERTE(s_pGCManager);
    s_pGCManager->FindTrackerTargetsCompleted(bWalkFailed);

    STRESS_LOG0 (LF_INTEROP, LL_INFO100, "[RCW Walker] RCW Walk finished\n");
}

//
// Callback implementation of IFindDependentWrappersCallback
//
class CFindDependentWrappersCallback : public IFindDependentWrappersCallback
{
public :
    CFindDependentWrappersCallback(RCW *pRCW, RCWRefCache*pRCWRefCache)
        :m_pRCW(pRCW), m_pRCWRefCache(pRCWRefCache)
    {
#ifdef _DEBUG
        m_hr = S_OK;
        m_dwCreatedRefs = 0;
#endif // _DEBUG
    }

    STDMETHOD_(ULONG, AddRef)()
    {

        // Lifetime maintained by stack - we don't care about ref counts
        return 1;
    }

    STDMETHOD_(ULONG, Release)()
    {
        // Lifetime maintained by stack - we don't care about ref counts
        return 1;
    }

    STDMETHOD(QueryInterface)(REFIID riid, void **ppvObject)
    {
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IFindDependentWrappersCallback))
        {
            *ppvObject = this;
            return S_OK;
        }
        else
        {
            *ppvObject = NULL;
            return E_NOINTERFACE;
        }
    }


    STDMETHOD(FoundTrackerTarget)(ICCW *pUnk)
    {
#ifdef _DEBUG
        _ASSERTE(SUCCEEDED(m_hr) && W("Should not receive FoundTrackerTarget again if failed"));
#endif // _DEBUG
        _ASSERTE(pUnk != NULL);

        ComCallWrapper *pCCW = MapIUnknownToWrapper(pUnk);
        _ASSERTE(pCCW != NULL);

        LOG((LF_INTEROP, LL_INFO1000, "\t[RCW Walker] IFindDependentWrappersCallback::FoundTrackerTarget being called: RCW 0x%p, CCW 0x%p\n", m_pRCW, pCCW));

        //
        // Skip dependent handle creation if RCW/CCW points to the same managed object
        //
        if (m_pRCW->GetSyncBlock() == pCCW->GetSyncBlock())
            return S_OK;

        //
        // Jupiter might return CCWs with outstanding references that are either :
        // 1. Neutered - in this case it is unsafe to touch m_ppThis
        // 2. RefCounted handle NULLed out by GC
        //
        // Skip those to avoid crashes
        //
        if (pCCW->GetSimpleWrapper()->IsNeutered() ||
            pCCW->GetObjectRef() == NULL)
            return S_OK;

        //
        // Add a reference from pRCW -> pCCW so that GC knows about this reference
        //
        STRESS_LOG4(
            LF_INTEROP, LL_INFO1000,
            "\t[RCW Walker] Adding reference: RCW 0x%p (Managed Object = 0x%p) -> CCW 0x%p (Managed Object = 0x%p)\n",
            m_pRCW, OBJECTREFToObject(m_pRCW->GetExposedObject()), pCCW, OBJECTREFToObject(pCCW->GetObjectRef())
            );

        HRESULT hr = m_pRCWRefCache->AddReferenceFromRCWToCCW(m_pRCW, pCCW);

#ifdef _DEBUG
        m_dwCreatedRefs++;
#endif // _DEBUG

        if (FAILED(hr))
        {
#ifdef _DEBUG
            m_hr = hr;
#endif // _DEBUG
            STRESS_LOG1(LF_INTEROP, LL_INFO1000, "[RCW Walker] Adding reference failed, hr = 0x%x", hr);

            return E_FAIL;
        }

        return S_OK;
    }

#ifdef _DEBUG
    HRESULT GetHRESULT()
    {

        return m_hr;
    }

    DWORD GetCreatedRefs()
    {

        return m_dwCreatedRefs;
    }
#endif // _DEBUG

private :
    RCW         *m_pRCW;
    RCWRefCache *m_pRCWRefCache;

#ifdef _DEBUG
    HRESULT     m_hr;               // Holds the last failed HRESULT to make sure our contract with Jupiter is correctly honored
    DWORD       m_dwCreatedRefs;    // Total number of refs created from this RCW
#endif // _DEBUG
};

//
// Ask Jupiter all the CCWs referenced (through native code) by this RCW and build reference for RCW -> CCW
// so that GC knows about this reference
//
HRESULT RCWWalker::WalkOneRCW(RCW *pRCW, RCWRefCache *pRCWRefCache)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pRCW));
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO1000, "\t[RCW Walker] ----- RCWWalker::WalkOneRCW (RCW = 0x%p) BEGINS ----- \n", pRCW));

    _ASSERTE(pRCW->IsJupiterObject());

    HRESULT hr = S_OK;

    // Get IJupiterObject * from RCW - we can call IJupiterObject* from any thread and it won't be a proxy
    IJupiterObject *pJupiterObject = pRCW->GetJupiterObject();
    _ASSERTE(pJupiterObject);

    _ASSERTE(pRCW->GetExposedObject() != NULL);

    CFindDependentWrappersCallback callback(pRCW, pRCWRefCache);

    STRESS_LOG2 (LF_INTEROP, LL_INFO1000, "\t[RCW Walker] Walking RCW 0x%p (Managed Object = 0x%p)\n", pRCW, OBJECTREFToObject(pRCW->GetExposedObject()));

    LOG((LF_INTEROP, LL_INFO1000, "\t[RCW Walker] Calling IJupiterObject::FindTrackerTargets\n", pRCW));
    hr = pJupiterObject->FindTrackerTargets(&callback);

#ifdef _DEBUG
    if (FAILED(callback.GetHRESULT()))
    {
        _ASSERTE(callback.GetHRESULT() == hr && W("FindDepedentWrappers should return the failed result from the callback method FoundTrackerTarget"));
    }

    LOG((LF_INTEROP, LL_INFO1000, "\t[RCW Walker] Total %d refs created for RCW 0x%p\n", callback.GetCreatedRefs(), pRCW));
#endif // _DEBUG

    LOG((LF_INTEROP, LL_INFO1000, "\t[RCW Walker] ----- RCWWalker::WalkOneRCW (RCW = 0x%p) ENDS   -----\n", pRCW));
    return hr;
}

typedef void (*OnGCEventProc)();
inline void SetupFailFastFilterAndCall(OnGCEventProc pGCEventProc)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_ANY;

    //
    // Use RCWWalker_UnhandledExceptionFilter to fail fast and early in case any exception is thrown
    // See code:RCWWalker_UnhandledExceptionFilter for more details why we need this
    //
    PAL_TRY_NAKED
    {
        // Call the internal worker function which has the runtime contracts
        pGCEventProc();
    }
    PAL_EXCEPT_FILTER_NAKED(RCWWalker_UnhandledExceptionFilter, NULL)
    {
        _ASSERT(!W("Should not get here"));
    }
    PAL_ENDTRY_NAKED
}

//
// Called when GC started
// We do most of our work here
//
// Note that we could get nested GCStart/GCEnd calls, such as :
// GCStart for Gen 2 background GC
//    GCStart for Gen 0/1 foregorund GC
//    GCEnd   for Gen 0/1 foreground GC
//    ....
// GCEnd for Gen 2 background GC
//
// The nCondemnedGeneration >= 2 check takes care of this nesting problem
//
void RCWWalker::OnGCStarted(int nCondemnedGeneration)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::OnGCStarted (nCondemnedGeneration = %d) BEGINS ----- \n", nCondemnedGeneration));

    if (RCWWalker::NeedToWalkRCWs())    // Have we seen Jupiter RCWs?
    {
        if (nCondemnedGeneration >= 2)  // We are only doing walk in Gen2 GC
        {
            // Make sure we fail fast if anything goes wrong when we interact with Jupiter
            SetupFailFastFilterAndCall(RCWWalker::OnGCStartedWorker);
        }
        else
        {
            LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] GC skipped: Not a Gen2 GC \n"));
        }
    }
    else
    {

        LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] GC skipped: No Jupiter RCWs seen \n"));
    }

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::OnGCStarted (nCondemnedGeneration = %d) ENDS   -----\n", nCondemnedGeneration));
}

//
// Called when GC finished
//
// Note that we could get nested GCStart/GCEnd calls, such as :
// GCStart for Gen 2 background GC
//    GCStart for Gen 0/1 foregorund GC
//    GCEnd   for Gen 0/1 foreground GC
//    ....
// GCEnd for Gen 2 background GC
//
// The nCondemnedGeneration >= 2 check takes care of this nesting problem
//
void RCWWalker::OnGCFinished(int nCondemnedGeneration)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::OnGCFinished(nCondemnedGeneration = %d) BEGINS ----- \n", nCondemnedGeneration));

    //
    // Note that we need to check in both OnGCFinished and OnGCStarted
    // As there could be multiple OnGCFinished with nCondemnedGeneration < 2 in the case of Gen 2 GC
    //
    // Also, if this is background GC, the NeedToWalkRCWs predicate may change from FALSE to TRUE while
    // the GC is running. We don't want to do any work if it's the case (i.e. if s_bGCStarted is FALSE).
    //
    if (RCWWalker::NeedToWalkRCWs() &&      // Have we seen Jupiter RCWs?
        s_bGCStarted &&                     // Had we seen Jupiter RCWs when the GC started?
        nCondemnedGeneration >= 2           // We are only doing walk in Gen2 GC
        )
    {
       // Make sure we fail fast if anything goes wrong when we interact with Jupiter
       SetupFailFastFilterAndCall(RCWWalker::OnGCFinishedWorker);
    }

    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] ----- RCWWalker::OnGCFinished(nCondemnedGeneration = %d) ENDS   ----- \n", nCondemnedGeneration));
}

void RCWWalker::OnGCStartedWorker()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    STRESS_LOG0 (LF_INTEROP, LL_INFO100, "[RCW Walker] Gen 2 GC Started - Ready to walk Jupiter RCWs\n");

    // Due to the nesting GCStart/GCEnd pairs (see comment for this function), we need to check
    // those flags inside nCondemnedGeneration >= 2 check
    _ASSERTE(!s_bGCStarted);
    _ASSERTE(VolatileLoad(&s_bIsGlobalPeggingOn));

    s_bGCStarted = TRUE;

    _ASSERTE(s_pGCManager);

    //
    // Let Jupiter know we are about to walk RCWs so that they can lock their reference cache
    // Note that Jupiter doesn't need to unpeg all CCWs at this point and they can do the pegging/unpegging in FindTrackerTargetsCompleted
    //
    // Note: IGCManager should be free-threaded as it will be called on arbitary threads
    //
    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] Calling IGCManager::ReferenceTrackingStarted on 0x%p\n", s_pGCManager));
    s_pGCManager->ReferenceTrackingStarted();

    // From this point, jupiter decides whether a CCW should be pegged or not as global pegging flag is now off
    s_bIsGlobalPeggingOn = FALSE;
    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] Global pegging flag is off\n"));

    //
    // OK. Time to walk all the Jupiter RCWs
    //
    WalkRCWs();
}

void RCWWalker::OnGCFinishedWorker()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    //
    // Let Jupiter know RCW walk is done and they need to:
    // 1. Unpeg all CCWs if the CCW needs to be unpegged (when the CCW is only reachable by other jupiter RCWs)
    // 2. Peg all CCWs if the CCW needs to be pegged (when the above condition is not true)
    // 3. Unlock reference cache when they are done
    //
    // If the walk has failed - Jupiter doesn't need to do anything and could just return immediately
    //
    // Note: We can IJupiterGCManager from any thread and it is guaranteed by Jupiter
    //
    _ASSERTE(s_pGCManager);
    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] Calling IGCManager::ReferenceTrackingCompleted on 0x%p\n", s_pGCManager));
    s_pGCManager->ReferenceTrackingCompleted();

    s_bIsGlobalPeggingOn = TRUE;
    LOG((LF_INTEROP, LL_INFO100, "[RCW Walker] Global pegging flag is on\n"));

    s_bGCStarted = FALSE;

    STRESS_LOG0 (LF_INTEROP, LL_INFO100, "[RCW Walker] Gen 2 GC Finished\n");
}

#endif // DACCESS_COMPILE
