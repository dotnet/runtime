// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Class:  RCW
**
**
** Purpose: The implementation of the ComObject class
**

===========================================================*/

#include "common.h"

#include <ole2.h>
#include <inspectable.h>

class Object;
#include "vars.hpp"
#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "threads.h"
#include "field.h"
#include "runtimecallablewrapper.h"
#include "hash.h"
#include "interoputil.h"
#include "comcallablewrapper.h"
#include "eeconfig.h"
#include "comdelegate.h"
#include "comcache.h"
#include "notifyexternals.h"
#include "../md/compiler/custattr.h"
#include "olevariant.h"
#include "interopconverter.h"
#include "typestring.h"
#include "caparser.h"
#include "classnames.h"
#include "objectnative.h"
#include "finalizerthread.h"

// static
SLIST_HEADER RCW::s_RCWStandbyList;

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
#include "interoplibinterface.h"


void ComClassFactory::ThrowHRMsg(HRESULT hr, DWORD dwMsgResID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    SString strMessage;
    SString strResource;
    WCHAR strClsid[39];
    SString strHRDescription;

    // Obtain the textual representation of the HRESULT.
    StringFromGUID2(m_rclsid, strClsid, sizeof(strClsid) / sizeof(WCHAR));

    SString strHRHex;
    strHRHex.Printf("%.8x", hr);

    // Obtain the description of the HRESULT.
    GetHRMsg(hr, strHRDescription);

    // Load the appropriate resource and throw
    COMPlusThrowHR(hr, dwMsgResID, strHRHex, strClsid, strHRDescription.GetUnicode());
}

//-------------------------------------------------------------
// Common code for licensing
//
IUnknown *ComClassFactory::CreateInstanceFromClassFactory(IClassFactory *pClassFact, IUnknown *punkOuter, BOOL *pfDidContainment)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pClassFact));
        PRECONDITION(CheckPointer(punkOuter, NULL_OK));
        PRECONDITION(CheckPointer(pfDidContainment, NULL_OK));
        PRECONDITION(CheckPointer(m_pClassMT, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    HRESULT hr = S_OK;
    SafeComHolder<IClassFactory2> pClassFact2 = NULL;
    SafeComHolder<IUnknown> pUnk = NULL;
    BSTRHolder bstrKey = NULL;

    // If the class doesn't support licensing or if it is missing a managed
    // type to use for querying a license, just use IClassFactory.
    if (FAILED(SafeQueryInterface(pClassFact, IID_IClassFactory2, (IUnknown**)&pClassFact2))
        || m_pClassMT == NULL)
    {
        FrameWithCookie<DebuggerExitFrame> __def;
        {
            GCX_PREEMP();
            hr = pClassFact->CreateInstance(punkOuter, IID_IUnknown, (void **)&pUnk);
            if (FAILED(hr) && punkOuter)
            {
                hr = pClassFact->CreateInstance(NULL, IID_IUnknown, (void**)&pUnk);
                if (pfDidContainment)
                    *pfDidContainment = TRUE;
            }
        }
        __def.Pop();
    }
    else
    {
        _ASSERTE(m_pClassMT != NULL);

        // Get the type to query for licensing.
        TypeHandle rth = TypeHandle(m_pClassMT);

        struct
        {
            OBJECTREF pProxy;
            OBJECTREF pType;
        } gc;
        gc.pProxy = NULL; // LicenseInteropProxy
        gc.pType = NULL;

        GCPROTECT_BEGIN(gc);

        // Create an instance of the object
        MethodDescCallSite createObj(METHOD__LICENSE_INTEROP_PROXY__CREATE);
        gc.pProxy = createObj.Call_RetOBJECTREF(NULL);
        gc.pType = rth.GetManagedClassObject();

        // Query the current licensing context
        MethodDescCallSite getCurrentContextInfo(METHOD__LICENSE_INTEROP_PROXY__GETCURRENTCONTEXTINFO, &gc.pProxy);
        CLR_BOOL fDesignTime = FALSE;
        ARG_SLOT args[4];
        args[0] = ObjToArgSlot(gc.pProxy);
        args[1] = ObjToArgSlot(gc.pType);
        args[2] = (ARG_SLOT)&fDesignTime;
        args[3] = (ARG_SLOT)(BSTR*)&bstrKey;

        getCurrentContextInfo.Call(args);

        if (fDesignTime)
        {
            // If designtime, we're supposed to obtain the runtime license key
            // from the component and save it away in the license context.
            // (the design tool can then grab it and embedded it into the
            //  app it is creating)
            if (bstrKey != NULL)
            {
                // It's illegal for our helper to return a non-null bstrKey
                // when the context is design-time. But we'll try to do the
                // right thing anyway.
                _ASSERTE(!"We're not supposed to get here, but we'll try to cope anyway.");
                SysFreeString(bstrKey);
                bstrKey = NULL;
            }

            {
                GCX_PREEMP();
                hr = pClassFact2->RequestLicKey(0, &bstrKey);
            }

            // E_NOTIMPL is not a true failure. It simply indicates that
            // the component doesn't support a runtime license key.
            if (hr == E_NOTIMPL)
                hr = S_OK;

            // Store the requested license key
            if (SUCCEEDED(hr))
            {
                MethodDescCallSite saveKeyInCurrentContext(METHOD__LICENSE_INTEROP_PROXY__SAVEKEYINCURRENTCONTEXT, &gc.pProxy);

                args[0] = ObjToArgSlot(gc.pProxy);
                args[1] = (ARG_SLOT)(BSTR)bstrKey;
                saveKeyInCurrentContext.Call(args);
            }
        }

        // Create the instance
        if (SUCCEEDED(hr))
        {
            FrameWithCookie<DebuggerExitFrame> __def;
            {
                GCX_PREEMP();
                if (fDesignTime || bstrKey == NULL)
                {
                    // Either it's design time, or the current context doesn't
                    // supply a runtime license key.
                    hr = pClassFact->CreateInstance(punkOuter, IID_IUnknown, (void **)&pUnk);
                    if (FAILED(hr) && punkOuter)
                    {
                        hr = pClassFact->CreateInstance(NULL, IID_IUnknown, (void**)&pUnk);
                        if (pfDidContainment)
                            *pfDidContainment = TRUE;
                    }
                }
                else
                {
                    // It is runtime and we have a license key.
                    _ASSERTE(bstrKey != NULL);
                    hr = pClassFact2->CreateInstanceLic(punkOuter, NULL, IID_IUnknown, bstrKey, (void**)&pUnk);
                    if (FAILED(hr) && punkOuter)
                    {
                        hr = pClassFact2->CreateInstanceLic(NULL, NULL, IID_IUnknown, bstrKey, (void**)&pUnk);
                        if (pfDidContainment)
                            *pfDidContainment = TRUE;
                    }
                }
            }
            __def.Pop();
        }

        GCPROTECT_END();
    }

    if (FAILED(hr))
    {
        if (bstrKey == NULL)
            ThrowHRMsg(hr, IDS_EE_CREATEINSTANCE_FAILED);
        else
            ThrowHRMsg(hr, IDS_EE_CREATEINSTANCE_LIC_FAILED);
    }

    // If the activated COM class has a CCW, mark the
    // CCW as being activated via COM.
    ComCallWrapper *ccw = GetCCWFromIUnknown(pUnk);
    if (ccw != NULL)
        ccw->MarkComActivated();

    ComWrappersNative::MarkWrapperAsComActivated(pUnk);

    pUnk.SuppressRelease();
    RETURN pUnk;
}


//-------------------------------------------------------------
// ComClassFactory::CreateAggregatedInstance(MethodTable* pMTClass)
// create a COM+ instance that aggregates a COM instance

OBJECTREF ComClassFactory::CreateAggregatedInstance(MethodTable* pMTClass, BOOL ForManaged)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMTClass));
    }
    CONTRACTL_END;

    BOOL fDidContainment = FALSE;

#ifdef _DEBUG
    // verify the class extends a COM import class
    MethodTable * pMT = pMTClass;
    do
    {
        pMT = pMT->GetParentMethodTable();
    }
    while (pMT == NULL || pMT->IsComImport());
    _ASSERTE(pMT != NULL);
#endif

    SafeComHolder<IUnknown>         pOuter      = NULL;
    SafeComHolder<IClassFactory>    pClassFact  = NULL;
    SafeComHolder<IUnknown>         pUnk        = NULL;

    HRESULT hr = S_OK;
    NewRCWHolder pNewRCW;
    BOOL bUseDelegate = FALSE;

    MethodTable *pCallbackMT = NULL;

    OBJECTREF oref = NULL;
    COMOBJECTREF cref = NULL;
    GCPROTECT_BEGIN(cref)
    {
        cref = (COMOBJECTREF)ComObject::CreateComObjectRef(pMTClass);

        //get wrapper for the object, this could enable GC
        CCWHolder pComWrap =  ComCallWrapper::InlineGetWrapper((OBJECTREF *)&cref);

        // Make sure the ClassInitializer has run, since the user might have
        // wanted to set up a COM object creation callback.
        pMTClass->CheckRunClassInitThrowing();

        // If the user is going to use a delegate to allocate the COM object
        // (rather than CoCreateInstance), we need to know now, before we enable
        // preemptive GC mode (since we touch object references in the
        // determination).
        // We don't just check the current class to see if it has a cllabck
        // registered, we check up the class chain to see if any of our parents
        // did.

        pCallbackMT = pMTClass;
        while ((pCallbackMT != NULL) &&
               (pCallbackMT->GetObjCreateDelegate() == NULL) &&
               !pCallbackMT->IsComImport())
        {
            pCallbackMT = pCallbackMT->GetParentMethodTable();
        }

        if (pCallbackMT && !pCallbackMT->IsComImport())
            bUseDelegate = TRUE;

        FrameWithCookie<DebuggerExitFrame> __def;

        // get the IUnknown interface for the managed object
        pOuter = ComCallWrapper::GetComIPFromCCW(pComWrap, IID_IUnknown, NULL);
        _ASSERTE(pOuter != NULL);

        // If the user has set a delegate to allocate the COM object, use it.
        // Otherwise we just CoCreateInstance it.
        if (bUseDelegate)
        {
            ARG_SLOT args[2];

            OBJECTREF orDelegate = pCallbackMT->GetObjCreateDelegate();
            MethodDesc *pMeth = COMDelegate::GetMethodDesc(orDelegate);

            GCPROTECT_BEGIN(orDelegate)
            {
                _ASSERTE(pMeth);
                MethodDescCallSite  delegateMethod(pMeth, &orDelegate);

                // Get the OR on which we are going to invoke the method and set it
                //  as the first parameter in arg above.
                args[0] = (ARG_SLOT)OBJECTREFToObject(COMDelegate::GetTargetObject(orDelegate));

                // Pass the IUnknown of the aggregator as the second argument.
                args[1] = (ARG_SLOT)(IUnknown*)pOuter;

                // Call the method...
                pUnk = (IUnknown *)delegateMethod.Call_RetArgSlot(args);
                if (!pUnk)
                    COMPlusThrowHR(E_FAIL);
            }
            GCPROTECT_END();
        }
        else
        {
            _ASSERTE(m_pClassMT);
            pUnk = CreateInstanceInternal(pOuter, &fDidContainment);
        }

        __def.Pop();

        // give up the extra addref that we did in our QI and suppress the auto-release.
        pComWrap->Release();
        pComWrap.SuppressRelease();

        // Here's the scary part.  If we are doing a managed 'new' of the aggregator,
        // then COM really isn't involved.  We should not be counting for our caller
        // because our caller relies on GC references rather than COM reference counting
        // to keep us alive.
        //
        // Drive the instances count down to 0 -- and rely on the GCPROTECT to keep us
        // alive until we get back to our caller.
        if (ForManaged)
            pComWrap->Release();

        RCWCache* pCache = RCWCache::GetRCWCache();

        _ASSERTE(cref->GetSyncBlock()->IsPrecious()); // the object already has a CCW
        DWORD dwSyncBlockIndex = cref->GetSyncBlockIndex();

        // create a wrapper for this COM object
        pNewRCW = RCW::CreateRCW(pUnk, dwSyncBlockIndex, RCW::CF_None, pMTClass);

        RCWHolder pRCW(GetThread());
        pRCW.InitNoCheck(pNewRCW);

        // we used containment
        // we need to store this wrapper in our hash table
        {
            RCWCache::LockHolder lh(pCache);

            GCX_FORBID();

            BOOL fInserted = pCache->FindOrInsertWrapper_NoLock(pUnk, &pRCW, /* fAllowReInit = */ FALSE);
            if (!fInserted)
            {
                // OK. Looks like the factory returned a singleton on us and the cache already
                // has an entry for this pIdentity. This should always happen in containment
                // scenario, not for aggregation.
                // In this case, we should insert this new RCW into cache as a unique RCW,
                // because these are separate objects, and we need two separate RCWs with
                // different flags (should be contained, impossible to be aggregated) pointing
                // to them, separately
                pNewRCW->m_pIdentity = pNewRCW;

                fInserted = pCache->FindOrInsertWrapper_NoLock((IUnknown*)pNewRCW->m_pIdentity, &pRCW, /* fAllowReInit = */ FALSE);
                _ASSERTE(fInserted);
            }
        }

        if (fDidContainment)
        {
            // mark the wrapper as contained
            pRCW->MarkURTContained();
        }
        else
        {
            // mark the wrapper as aggregated
            pRCW->MarkURTAggregated();
        }

            // pUnk has to be released inside GC-protected block and before oref get assigned value
            // because it could trigger GC
            SafeRelease(pUnk);
            pUnk.SuppressRelease();

        // If the object was created successfully then we need to copy the OBJECTREF
        // to oref because the GCPROTECT_END() will destroy the contents of cref.
        oref = ObjectToOBJECTREF(*(Object **)&cref);
    }
    GCPROTECT_END();

    if (oref != NULL)
    {
        pOuter.SuppressRelease();
        pClassFact.SuppressRelease();
        pNewRCW.SuppressRelease();
    }

    return oref;
}

//--------------------------------------------------------------
// Create instance using IClassFactory
// Overridable
IUnknown *ComClassFactory::CreateInstanceInternal(IUnknown *pOuter, BOOL *pfDidContainment)
{
    CONTRACT(IUnknown *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOuter, NULL_OK));
        PRECONDITION(CheckPointer(pfDidContainment, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    SafeComHolder<IClassFactory> pClassFactory = GetIClassFactory();
    RETURN CreateInstanceFromClassFactory(pClassFactory, pOuter, pfDidContainment);
}

IClassFactory *ComClassFactory::GetIClassFactory()
{
    CONTRACT(IClassFactory *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    HRESULT hr = S_OK;
    IClassFactory *pClassFactory = NULL;

    GCX_PREEMP();

    // If a server name is specified, then first try CLSCTX_REMOTE_SERVER.
    if (m_wszServer)
    {
        // Set up the COSERVERINFO struct.
        COSERVERINFO ServerInfo;
        memset(&ServerInfo, 0, sizeof(COSERVERINFO));
        ServerInfo.pwszName = (LPWSTR)m_wszServer;

        // Try to retrieve the IClassFactory passing in CLSCTX_REMOTE_SERVER.
        hr = CoGetClassObject(m_rclsid, CLSCTX_REMOTE_SERVER, &ServerInfo, IID_IClassFactory, (void**)&pClassFactory);
    }
    else
    {
        // No server name is specified so we use CLSCTX_SERVER.
        if (pClassFactory == NULL)
            hr = CoGetClassObject(m_rclsid, CLSCTX_SERVER, NULL, IID_IClassFactory, (void**)&pClassFactory);
    }

    // If we failed to obtain the IClassFactory, throw an exception with rich information
    // explaining the failure.
    if (FAILED(hr))
    {
        SString strMessage;
        SString strResource;
        WCHAR strClsid[39];
        SString strHRDescription;

        // Obtain the textual representation of the HRESULT.
        StringFromGUID2(m_rclsid, strClsid, sizeof(strClsid) / sizeof(WCHAR));

        SString strHRHex;
        strHRHex.Printf("%.8x", hr);

        // Obtain the description of the HRESULT.
        GetHRMsg(hr, strHRDescription);

        // Throw the actual exception indicating we couldn't find the class factory.
        if (m_wszServer == NULL)
            COMPlusThrowHR(hr, IDS_EE_LOCAL_COGETCLASSOBJECT_FAILED, strHRHex, strClsid, strHRDescription.GetUnicode());
        else
            COMPlusThrowHR(hr, IDS_EE_REMOTE_COGETCLASSOBJECT_FAILED, strHRHex, strClsid, m_wszServer, strHRDescription.GetUnicode());
    }

    RETURN pClassFactory;
}

//-------------------------------------------------------------
// ComClassFactory::CreateInstance()
// create instance, calls IClassFactory::CreateInstance
OBJECTREF ComClassFactory::CreateInstance(MethodTable* pMTClass, BOOL ForManaged)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMTClass, NULL_OK));
    }
    CONTRACTL_END;

    // Check for aggregates
    if (pMTClass != NULL && !pMTClass->IsComImport())
        return CreateAggregatedInstance(pMTClass, ForManaged);

    HRESULT hr = S_OK;
    OBJECTREF coref = NULL;
    OBJECTREF RetObj = NULL;

    GCPROTECT_BEGIN(coref)
    {
        {
            SafeComHolder<IUnknown> pUnk = NULL;
            SafeComHolder<IClassFactory> pClassFact = NULL;

            // Create the instance
            pUnk = CreateInstanceInternal(NULL, NULL);

            // Even though we just created the object, it's possible that we got back a context
            // wrapper from the COM side.  For instance, it could have been an existing object
            // or it could have been created in a different context than we are running in.

            // pMTClass is the class that wraps the com ip
            // if a class was passed in use it
            // otherwise use the class that we know
            if (pMTClass == NULL)
                pMTClass = m_pClassMT;

            GetObjectRefFromComIP(&coref, pUnk, pMTClass);

            if (coref == NULL)
                COMPlusThrowOM();
        }

        // Set the value of the return object after the COM guys are cleaned up.
        RetObj = coref;
    }
    GCPROTECT_END();

    return RetObj;
}

//--------------------------------------------------------------
// Init the ComClassFactory.
void ComClassFactory::Init(_In_opt_ PCWSTR wszServer, MethodTable* pClassMT)
{
    LIMITED_METHOD_CONTRACT;

    m_wszServer = wszServer;
    m_pClassMT = pClassMT;
}

//-------------------------------------------------------------
void ComClassFactory::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_bManagedVersion)
        return;

    if (m_wszServer != NULL)
        delete [] m_wszServer;

    delete this;
}

#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

//---------------------------------------------------------------------
// RCW cache, act as the manager for the RCWs
// uses a hash table to map IUnknown to the corresponding wrappers
//---------------------------------------------------------------------

// Obtain the appropriate wrapper cache from the current context.
RCWCache* RCWCache::GetRCWCache()
{
    CONTRACT (RCWCache*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    AppDomain * pDomain = GetAppDomain();
    RETURN (pDomain ? pDomain->GetRCWCache() : NULL);
}

RCWCache* RCWCache::GetRCWCacheNoCreate()
{
    CONTRACT (RCWCache*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    AppDomain * pDomain = GetAppDomain();
    RETURN (pDomain ? pDomain->GetRCWCacheNoCreate() : NULL);
}


//---------------------------------------------------------------------
// Constructor.  Note we init the global RCW cleanup list in here too.
RCWCache::RCWCache(AppDomain *pDomain)
    : m_lock(CrstRCWCache, CRST_UNSAFE_COOPGC)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDomain));
    }
    CONTRACTL_END;

    m_pDomain = pDomain;
}

// Look up to see if we already have an valid wrapper in cache for this IUnk
// DOES NOT hold a lock inside the function - locking in the caller side IS REQUIRED
// If pfMadeWrapperStrong is TRUE upon return, you NEED to call AddRef on pIdentity
void RCWCache::FindWrapperInCache_NoLock(IUnknown* pIdentity, RCWHolder* pRCW)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pIdentity));
        PRECONDITION(CheckPointer(pRCW));
    }
    CONTRACTL_END;

    // lookup in our hash table
    LookupWrapper(pIdentity, pRCW);

    // check if we found the wrapper,
    if (!pRCW->IsNull())
    {
        if ((*pRCW)->IsValid())
        {
            if ((*pRCW)->IsDetached())
            {
                _ASSERTE((LPVOID)pIdentity != (LPVOID)pRCW->GetRawRCWUnsafe()); // we should never find "unique" RCWs

                // remove and re-insert the RCW using its unique identity
                RemoveWrapper(pRCW);
                (*pRCW)->m_pIdentity = (LPVOID)pRCW->GetRawRCWUnsafe();
                InsertWrapper(pRCW);

                pRCW->UnInit();
            }
            else
            {
                // addref the wrapper
                (*pRCW)->AddRef(this);
            }
        }
        else
        {
            pRCW->UnInit();
        }
    }

    return;
}

BOOL RCWCache::FindOrInsertWrapper_NoLock(IUnknown* pIdentity, RCWHolder* pRCW, BOOL fAllowReinit)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pIdentity));
        PRECONDITION(pIdentity != (IUnknown*)-1);
        PRECONDITION(CheckPointer(pRCW));
        PRECONDITION(CheckPointer(pRCW->GetRawRCWUnsafe()));
    }
    CONTRACTL_END;

    BOOL fInserted = FALSE;

    // we have created a wrapper, let us insert it into the hash table
    // but we need to check if somebody beat us to it
    {
        // see if somebody beat us to it
        // perf: unfold LookupWrapper to avoid creating RCWHolder in common cases
        RCW *pRawRCW = LookupWrapperUnsafe(pIdentity);
        if (pRawRCW == NULL)
        {
            InsertWrapper(pRCW);
            fInserted = TRUE;
        }
        else
        {
            RCWHolder pTempRCW(GetThread());

            // Assume that we already have a sync block for this object.
            pTempRCW.InitNoCheck(pRawRCW);

            // if we didn't find a valid wrapper, Insert our own wrapper
            if (pTempRCW.IsNull() || !pTempRCW->IsValid())
            {
                // if we found a bogus wrapper, let us get rid of it
                // so that when we insert we insert a valid wrapper, instead of duplicate
                if (!pTempRCW.IsNull())
                {
                    _ASSERTE(!pTempRCW->IsValid());
                    RemoveWrapper(&pTempRCW);
                }

                InsertWrapper(pRCW);
                fInserted = TRUE;
            }
            else
            {
                _ASSERTE(!pTempRCW.IsNull() && pTempRCW->IsValid());
                // okay we found a valid wrapper,

                if (pTempRCW->IsDetached())
                {
                    _ASSERTE((LPVOID)pIdentity != (LPVOID)pTempRCW.GetRawRCWUnsafe()); // we should never find "unique" RCWs

                    // remove and re-insert the RCW using its unique identity
                    RemoveWrapper(&pTempRCW);
                    pTempRCW->m_pIdentity = (LPVOID)pTempRCW.GetRawRCWUnsafe();
                    InsertWrapper(&pTempRCW);

                    // and insert the new incoming RCW
                    InsertWrapper(pRCW);
                    fInserted = TRUE;
                }
                else if (fAllowReinit)
                {
                    // addref the wrapper
                    pTempRCW->AddRef(this);

                    // Initialize the holder with the rcw we're going to return.
                    OBJECTREF objref = pTempRCW->GetExposedObject();
                    pTempRCW.UnInit();
                    pRCW->UnInit();
                    pRCW->InitNoCheck(objref);
                }
            }
        }
    }

    return fInserted;
}

//--------------------------------------------------------------------------------
// ULONG RCWCache::ReleaseWrappers()
// Helper to release the complus wrappers in the cache that lives in the specified
// context or all the wrappers in the cache if the pCtxCookie is null.
void RCWCache::ReleaseWrappersWorker(LPVOID pCtxCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCtxCookie, NULL_OK));
    }
    CONTRACTL_END;

    RCWCleanupList CleanupList;
    RCWCleanupList AggregatedCleanupList;

    struct RCWInterfacePointer
    {
        IUnknown *m_pUnk;
        RCW      *m_pRCW;
        CtxEntry *m_pCtxEntry;
    };

    // Arrays of individual interface pointers to call Release on
    CQuickArrayList<RCWInterfacePointer> InterfacePointerList;
    CQuickArrayList<RCWInterfacePointer> AggregatedInterfacePointerList;

    // Switch to cooperative GC mode before we take the lock.
    GCX_COOP();
    {
        {
            RCWCache::LockHolder lh(this);

            // Go through the hash table and add the wrappers to the cleanup lists.
            for (SHash<RCWCacheTraits>::Iterator it = m_HashMap.Begin(); it != m_HashMap.End(); it++)
            {
                RCW *pWrap = *it;
                _ASSERTE(pWrap != NULL);

                // If a context cookie was specified, then only clean up wrappers that
                // are in that context, including non-FTM regular RCWs
                // Otherwise clean up all the wrappers.
                // Ignore RCWs that aggregate the FTM if we are cleaning up context
                // specific RCWs
                if (!pCtxCookie || ((pWrap->GetWrapperCtxCookie() == pCtxCookie) && !pWrap->IsFreeThreaded()))
                {
                    if (!pWrap->IsURTAggregated())
                        CleanupList.AddWrapper_NoLock(pWrap);
                    else
                        AggregatedCleanupList.AddWrapper_NoLock(pWrap);

                    pWrap->DecoupleFromObject();
                    RemoveWrapper(pWrap);
                }
            }
        }

        // Clean up the non URT aggregated RCW's first then clean up the URT aggregated RCW's.
        CleanupList.CleanupAllWrappers();

        for (SIZE_T i = 0; i < InterfacePointerList.Size(); i++)
        {
            RCWInterfacePointer &intfPtr = InterfacePointerList[i];

            RCW_VTABLEPTR(intfPtr.m_pRCW);
            SafeRelease(intfPtr.m_pUnk, intfPtr.m_pRCW);

            intfPtr.m_pCtxEntry->Release();
        }

        AggregatedCleanupList.CleanupAllWrappers();

        for (SIZE_T i = 0; i < AggregatedInterfacePointerList.Size(); i++)
        {
            RCWInterfacePointer &intfPtr = AggregatedInterfacePointerList[i];

            RCW_VTABLEPTR(intfPtr.m_pRCW);
            SafeRelease(intfPtr.m_pUnk, intfPtr.m_pRCW);

            intfPtr.m_pCtxEntry->Release();
        }

    }

    if (!CleanupList.IsEmpty() || !AggregatedCleanupList.IsEmpty())
    {
        _ASSERTE(!"Cannot cleanup RCWs in cleanup list. Most likely because the RCW is disabled for eager cleanup.");
        LOG((LF_INTEROP, LL_INFO1000, "Cannot cleanup RCWs in cleanup list. Most likely because the RCW is disabled for eager cleanup."));
    }
}

//--------------------------------------------------------------------------------
// ULONG RCWCache::DetachWrappersWorker()
// Helper to mark RCWs that are not GC-promoted at this point as detached.
class DetachWrappersFunctor
{
public:
    FORCEINLINE void operator() (RCW *pRCW)
    {
        LIMITED_METHOD_CONTRACT;

        if (pRCW->IsValid())
        {
            if (!GCHeapUtilities::GetGCHeap()->IsPromoted(OBJECTREFToObject(pRCW->GetExposedObject())) &&
                !pRCW->IsDetached())
            {
                // No need to use InterlockedOr here since every other place that modifies the flags
                // runs in cooperative GC mode (i.e. definitely not concurrently with this function).
                pRCW->m_Flags.m_Detached = 1;
            }
        }
    }
};

void RCWCache::DetachWrappersWorker()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(GCHeapUtilities::IsGCInProgress()); // GC is in progress and the runtime is suspended
    }
    CONTRACTL_END;

    DetachWrappersFunctor functor;
    m_HashMap.ForEach(functor);
}

VOID RCWCleanupList::AddWrapper(RCW* pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // For the global cleanup list, this is called only from the finalizer thread
    _ASSERTE(this != g_pRCWCleanupList || GetThread() == FinalizerThread::GetFinalizerThread());

    {
        CrstHolder ch(&m_lock);

        AddWrapper_NoLock(pRCW);
    }
}

VOID RCWCleanupList::AddWrapper_NoLock(RCW* pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Traverse the list for match - when found, insert as the matching bucket head.
    RCW *pBucket = m_pFirstBucket;
    RCW *pPrevBucket = NULL;
    while (pBucket != NULL)
    {
        if (pRCW->MatchesCleanupBucket(pBucket))
        {
            // Insert as bucket head.
            pRCW->m_pNextRCW = pBucket;
            pRCW->m_pNextCleanupBucket = pBucket->m_pNextCleanupBucket;

            // Not necessary but makes it clearer that pBucket is no longer a bucket head.
            pBucket->m_pNextCleanupBucket = NULL;
            break;
        }
        pPrevBucket = pBucket;
        pBucket = pBucket->m_pNextCleanupBucket;
    }

    // If we didn't find a match, insert as a new bucket.
    if (pBucket == NULL)
    {
        pRCW->m_pNextRCW = NULL;
        pRCW->m_pNextCleanupBucket = NULL;
    }

    // pRCW is now a bucket head - the only thing missing is a link from the previous bucket head.
    if (pPrevBucket != NULL)
        pPrevBucket->m_pNextCleanupBucket = pRCW;
    else
        m_pFirstBucket = pRCW;
}

VOID RCWCleanupList::CleanupAllWrappers()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;

        // For the global cleanup list, this is called only from the finalizer thread
        PRECONDITION( (this != g_pRCWCleanupList) || (GetThread() == FinalizerThread::GetFinalizerThread()));
    }
    CONTRACTL_END;

    RemovedBuckets NonSTABuckets;
    RemovedBuckets STABuckets;

    // We sweep the cleanup list once and remove all MTA/Free-Threaded buckets as well as STA buckets, leaving only
    // those with disabled eager cleanup in the list. Then we drop the lock, walk the removed buckets,
    // and perform the actual release. We cannot be releasing during the initial sweep because we would
    // need to drop and reacquire the lock for each bucket which would invalidate the entire linked
    // list so we would need to start the enumeration over after each bucket.
    {
        // Take the lock
        CrstHolder ch(&m_lock);

        RCW *pBucket = m_pFirstBucket;
        RCW *pPrevBucket = NULL;
        while (pBucket != NULL)
        {
            RCW *pNextBucket = pBucket->m_pNextCleanupBucket;
            Thread *pSTAThread = pBucket->GetSTAThread();

            if (pSTAThread == NULL || pBucket->AllowEagerSTACleanup())
            {
                // Remove the list from the CleanupList structure
                if (pPrevBucket != NULL)
                    pPrevBucket->m_pNextCleanupBucket = pBucket->m_pNextCleanupBucket;
                else
                    m_pFirstBucket = pBucket->m_pNextCleanupBucket;

                if (pSTAThread == NULL)
                {
                    // and add it to the local MTA/Free-Threaded chain
                    NonSTABuckets.Append(pBucket);
                }
                else
                {
                    // or to the local STA chain
                    STABuckets.Append(pBucket);
                }
            }
            else
            {
                // move the 'previous' pointer only if we didn't remove the current bucket
                pPrevBucket = pBucket;
            }
            pBucket = pNextBucket;
        }
        // Release the lock so we can correctly transition to cleanup.
    }

    // Request help from other threads
    m_doCleanupInContexts = TRUE;

    // First, cleanup the MTA/Free-Threaded buckets
    RCW *pRCWToCleanup;
    while ((pRCWToCleanup = NonSTABuckets.PopHead()) != NULL)
    {
        ReleaseRCWList_Args args;
        args.pHead = pRCWToCleanup;
        args.ctxTried = FALSE;
        args.ctxBusy = FALSE;

        ReleaseRCWListInCorrectCtx(&args);
    }

    // Now, cleanup the STA buckets
    while ((pRCWToCleanup = STABuckets.PopHead()) != NULL)
    {
        //
        // CAUTION: DONOT access pSTAThread fields here as pSTAThread
        // could've already been deleted, if
        // 1) the RCW is a free threaded RCW
        // 2) the RCW is a regular RCW, and marked by GC but not finalized yet
        // Only pointer comparison is allowed.
        //
        ReleaseRCWList_Args args;
        args.pHead = pRCWToCleanup;
        args.ctxTried = FALSE;
        args.ctxBusy = FALSE;

        // Advertise the fact that we're cleaning up this thread.
        m_pCurCleanupThread = pRCWToCleanup->GetSTAThread();
        _ASSERTE(pRCWToCleanup->GetSTAThread() != NULL);

        ReleaseRCWListInCorrectCtx(&args);

        // Done cleaning this thread for now...reset
        m_pCurCleanupThread = NULL;
    }

    // No more stuff for other threads to help with
    m_doCleanupInContexts = FALSE;
}


VOID RCWCleanupList::CleanupWrappersInCurrentCtxThread(BOOL fWait, BOOL fManualCleanupRequested, BOOL bIgnoreComObjectEagerCleanupSetting)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!m_doCleanupInContexts && !fManualCleanupRequested)
        return;

    // Find out our STA (if any)
    Thread *pThread = GetThread();
    LPVOID pCurrCtxCookie = GetCurrentCtxCookie();

    Thread::ApartmentState aptState = pThread->GetApartment();

    RemovedBuckets BucketsToCleanup;

    {
        // Take the lock
        CrstHolder ch(&m_lock);

        RCW *pBucket = m_pFirstBucket;
        RCW *pPrevBucket = NULL;
        while (pBucket != NULL)
        {
            BOOL fMatch = FALSE;
            RCW *pNextBucket = pBucket->m_pNextCleanupBucket;

            if (aptState != Thread::AS_InSTA)
            {
                // If we're in an MTA, just look for a matching contexts (including free-threaded and non-free threaded)
                if (pBucket->GetSTAThread() == NULL &&
                    (pCurrCtxCookie == NULL || pBucket->GetWrapperCtxCookie() == pCurrCtxCookie))
                {
                    fMatch = TRUE;
                }
            }
            else
            {
                // If we're in an STA, clean all matching STA contexts (including free-threaded and non-free threaded)
                if (pBucket->GetWrapperCtxCookie() == pCurrCtxCookie &&
                    (bIgnoreComObjectEagerCleanupSetting || pBucket->AllowEagerSTACleanup()))
                {
                    fMatch = TRUE;
                }
            }

            if (fMatch)
            {
                // Remove the list from the CleanupList structure
                if (pPrevBucket != NULL)
                    pPrevBucket->m_pNextCleanupBucket = pBucket->m_pNextCleanupBucket;
                else
                    m_pFirstBucket = pBucket->m_pNextCleanupBucket;

                // and add it to the local cleanup chain
                BucketsToCleanup.Append(pBucket);
            }
            else
            {
                // move the 'previous' pointer only if we didn't remove the current bucket
                pPrevBucket = pBucket;
            }
            pBucket = pNextBucket;
        }
    }

    // Clean it up
    RCW *pRCWToCleanup;
    while ((pRCWToCleanup = BucketsToCleanup.PopHead()) != NULL)
    {
        if (pRCWToCleanup->GetSTAThread() == NULL)
        {
            // We're already in the correct context, just clean it.
            ReleaseRCWListRaw(pRCWToCleanup);
        }
        else
        {
            ReleaseRCWList_Args args;
            args.pHead = pRCWToCleanup;
            args.ctxTried = FALSE;
            args.ctxBusy = FALSE;

            ReleaseRCWListInCorrectCtx(&args);
        }
    }

    if (aptState == Thread::AS_InSTA)
    {
        if (fWait && m_pCurCleanupThread == pThread)
        {
            // The finalizer thread may be trying to enter our STA -
            // make sure it can get in.

            LOG((LF_INTEROP, LL_INFO1000, "Thread %p: Yielding to finalizer thread.\n", pThread));

            // Do a noop wait just to make sure we are cooperating
            // with the finalizer thread
            pThread->Join(1, TRUE);
        }
    }
}

BOOL RCWCleanupList::IsEmpty()
{
    LIMITED_METHOD_CONTRACT;
    return (m_pFirstBucket == NULL);
}

// static
HRESULT RCWCleanupList::ReleaseRCWListInCorrectCtx(LPVOID pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pData));
    }
    CONTRACTL_END;

    ReleaseRCWList_Args* args = (ReleaseRCWList_Args*)pData;


    RCW* pHead = (RCW *)args->pHead;

    LPVOID pCurrCtxCookie = GetCurrentCtxCookie();

    // If we are releasing our IP's as a result of shutdown, we MUST not transition
    // into cooperative GC mode. This "fix" will prevent us from doing so.
    if (g_fEEShutDown & ShutDown_Finalize2)
    {
        Thread *pThread = GetThreadNULLOk();
        if (pThread && !FinalizerThread::IsCurrentThreadFinalizer())
            pThread->SetThreadStateNC(Thread::TSNC_UnsafeSkipEnterCooperative);
    }


    // Make sure we're in the right context / apartment.
    // Also - if we've already transitioned once, we don't want to do so again.
    //  If the cookie exists in multiple MTA apartments, and the STA has gone away
    //  (leaving the old STA thread as unknown state with a context value equal to
    //  the MTA context), we will infinitely loop.  So, we short circuit this with ctxTried.

    Thread *pHeadThread = pHead->GetSTAThread();
    BOOL fCorrectThread = (pHeadThread == NULL) ? TRUE : (pHeadThread == GetThreadNULLOk());
    BOOL fCorrectCookie = (pCurrCtxCookie == NULL) ? TRUE : (pHead->GetWrapperCtxCookie() == pCurrCtxCookie);

    if ( pHead->IsFreeThreaded() || // Avoid context transition if the list is for free threaded RCW
        (fCorrectThread && fCorrectCookie) || args->ctxTried )
    {
        ReleaseRCWListRaw(pHead);
    }
    else
    {
        // Mark that we're trying a context transition
        args->ctxTried = TRUE;

        // Transition into the context to release the interfaces.
        HRESULT hr = pHead->EnterContext(ReleaseRCWListInCorrectCtx, args);
        if (FAILED(hr) || args->ctxBusy)
        {
            // We are having trouble transitioning into the context (typically because the context is disconnected)
            // or the context is busy so we cannot transition into it to clean up.
            // The only option we have left is to try and clean up the RCW's from the current context.
            ReleaseRCWListRaw(pHead);
        }
    }

    // Reset the bit indicating we cannot transition into cooperative GC mode.
    if (g_fEEShutDown & ShutDown_Finalize2)
    {
        Thread *pThread = GetThreadNULLOk();
        if (pThread && !FinalizerThread::IsCurrentThreadFinalizer())
            pThread->ResetThreadStateNC(Thread::TSNC_UnsafeSkipEnterCooperative);
    }

    return S_OK;
}

// static
VOID RCWCleanupList::ReleaseRCWListRaw(RCW* pRCW)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pRCW));
    }
    CONTRACTL_END;

    // Release all these RCWs
    RCW* pNext = NULL;
    while (pRCW != NULL)
    {
        pNext = pRCW->m_pNextRCW;
        pRCW->Cleanup();
        pRCW = pNext;
    }
}

const int RCW::s_rGCPressureTable[GCPressureSize_COUNT] =
{
    0,                           // GCPressureSize_None
    GC_PRESSURE_PROCESS_LOCAL,   // GCPressureSize_ProcessLocal
    GC_PRESSURE_MACHINE_LOCAL,   // GCPressureSize_MachineLocal
    GC_PRESSURE_REMOTE,          // GCPressureSize_Remote
};

// Deletes all items in code:s_RCWStandbyList.
void RCW::FlushStandbyList()
{
    LIMITED_METHOD_CONTRACT;

    PSLIST_ENTRY pEntry = InterlockedFlushSList(&RCW::s_RCWStandbyList);
    while (pEntry)
    {
        PSLIST_ENTRY pNextEntry = pEntry->Next;
        delete (RCW *)pEntry;
        pEntry = pNextEntry;
    }
}
//--------------------------------------------------------------------------------
// The IUnknown passed in is AddRef'ed if we succeed in creating the wrapper.
RCW* RCW::CreateRCW(IUnknown *pUnk, DWORD dwSyncBlockIndex, DWORD flags, MethodTable *pClassMT)
{
    CONTRACT (RCW*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACT_END;

    RCW *pRCW = NULL;

    {
        GCX_PREEMP();
        pRCW = RCW::CreateRCWInternal(pUnk, dwSyncBlockIndex, flags, pClassMT);
    }

    RETURN pRCW;
}

RCW* RCW::CreateRCWInternal(IUnknown *pUnk, DWORD dwSyncBlockIndex, DWORD flags, MethodTable *pClassMT)
{
    CONTRACT (RCW*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(dwSyncBlockIndex != 0);
        PRECONDITION(CheckPointer(pClassMT));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // now allocate the wrapper
    RCW *pWrap = (RCW *)InterlockedPopEntrySList(&RCW::s_RCWStandbyList);
    if (pWrap != NULL)
    {
        // cache hit - reinitialize the data structure
        new (pWrap) RCW();
    }
    else
    {
        pWrap = new RCW();
    }

    AppDomain * pAppDomain = GetAppDomain();
    ULONG cbRef = SafeAddRefPreemp(pUnk);
    LogInteropAddRef(pUnk, cbRef, "RCWCache::CreateRCW: Addref pUnk because creating new RCW");

    // Make sure we release AddRef-ed pUnk in case of exceptions
    SafeComHolderPreemp<IUnknown> pUnkHolder = pUnk;

    // Log the creation
    LogRCWCreate(pWrap, pUnk);

    // Initialize wrapper
    pWrap->Initialize(pUnk, dwSyncBlockIndex, pClassMT);

    pUnkHolder.SuppressRelease();

    RETURN pWrap;
}

//----------------------------------------------------------
// Init IUnknown and IDispatch cookies with the pointers, and assocaiate the COMOBJECTREF with this RCW
void RCW::Initialize(IUnknown* pUnk, DWORD dwSyncBlockIndex, MethodTable *pClassMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(dwSyncBlockIndex != 0);
        PRECONDITION(CheckPointer(pClassMT));
    }
    CONTRACTL_END;

    m_cbRefCount = 1;

    // Start with use count 1 (this is counteracted in RCW::Cleanup)
    m_cbUseCount = 1;

    // Cache the IUnk and thread
    m_pIdentity = pUnk;

    // Remember the VTable pointer of the COM IP.
    //  This is very helpful for tracking down early released COM objects
    //  that AV when you call IUnknown::Release.
    m_vtablePtr = *(LPVOID*)pUnk;

    // track the thread that created this wrapper
    // if this thread is an STA thread, then when the STA dies
    // we need to cleanup this wrapper
    m_pCreatorThread  = GetThread();

    m_pRCWCache = RCWCache::GetRCWCache();

    m_Flags.m_MarshalingType = GetMarshalingType(pUnk, pClassMT);

    // Initialize the IUnkEntry
    m_UnkEntry.Init(pUnk, IsFreeThreaded(), m_pCreatorThread DEBUGARG(this));

    // Determine AllowEagerSTACleanup setting right now
    // We don't want to access the thread object to get the status
    // as m_pCreatorThread could be already dead at RCW cleanup time
    //
    // Free threaded RCWs created from STA "survives" even after the pSTAThread is terminated
    // and destroyed. For free threaded objects, there will be no pumping at all and user should always
    // expect the object to be accessed concurrently.
    //
    // So, only disallow eager STA cleanup for non free-threaded RCWs. Free threaded RCWs
    // should be cleaned up regardless of the setting on thread.
    bool disableEagerCleanup = m_pCreatorThread->IsDisableComObjectEagerCleanup();

    if (disableEagerCleanup && !IsFreeThreaded())
        m_Flags.m_fAllowEagerSTACleanup = 0;
    else
        m_Flags.m_fAllowEagerSTACleanup = 1;

    // store the wrapper in the sync block, that is the only way we can get cleaned up
    // the syncblock is guaranteed to be present
    SyncBlock *pSyncBlock = g_pSyncTable[(int)dwSyncBlockIndex].m_SyncBlock;
    InteropSyncBlockInfo *pInteropInfo = pSyncBlock->GetInteropInfo();
    pInteropInfo->SetRawRCW(this);

    // Store the sync block index.
    m_SyncBlockIndex = dwSyncBlockIndex;

    // Log the wrapper initialization.
    LOG((LF_INTEROP, LL_INFO100, "Initializing RCW %p with SyncBlock index %d\n", this, dwSyncBlockIndex));

    // To help combat finalizer thread starvation, we check to see if there are any wrappers
    // scheduled to be cleaned up for our context.  If so, we'll do them here to avoid making
    // the finalizer thread do a transition.
    // @perf: This may need a bit of tuning.
    // Note: This will enter a message pump in order to synchronize with the finalizer thread.

    // We can't safely pump here for Releasing (or directly release)
    // if we're currently in a SendMessage.
    // Also, clients can opt out of this. The option is is a per-thread flag which they can
    // set by calling DisableComEagerCleanup on the appropriate thread. Why would they
    // want to opt out? Because pumping can lead to re-entrancy in in unexpected places.
    // If a client decides to opt out, they are required to cleanup RCWs themselves by
    // calling Marshal.CleanupUnusedObjectsInCurrentContext periodically. The best place
    // to make that call is within their own message pump.
    if (!disableEagerCleanup
       )
    {
        _ASSERTE(g_pRCWCleanupList != NULL);
        g_pRCWCleanupList->CleanupWrappersInCurrentCtxThread();
    }
}

VOID RCW::MarkURTAggregated()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_Flags.m_fURTContained == 0);
    }
    CONTRACTL_END;

    m_Flags.m_fURTAggregated = 1;
}

RCW::MarshalingType RCW::GetMarshalingType(IUnknown* pUnk, MethodTable *pClassMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pClassMT));
    }
    CONTRACTL_END;

    PTR_EEClass pClass = pClassMT->GetClass();

    // Skip attributes on interfaces as any object could implement those interface
    if (!pClass->IsInterface() && pClass->IsMarshalingTypeSet())
    {
        MarshalingType mType;
        ( pClass ->IsMarshalingTypeFreeThreaded() ) ? mType = MarshalingType_FreeThreaded
            : (pClass->IsMarshalingTypeInhibit() ? mType = MarshalingType_Inhibit
            : mType = MarshalingType_Standard);
        return mType;
    }
    // MarshalingBehavior is not set and hence we will have to find the behavior using the QI
    else
    {
        // Check whether the COM object can be marshaled. Hence we query for INoMarshal
        SafeComHolderPreemp<INoMarshal> pNoMarshal;
        HRESULT hr = SafeQueryInterfacePreemp(pUnk, IID_INoMarshal, (IUnknown**)&pNoMarshal);
        LogInteropQI(pUnk, IID_INoMarshal, hr, "RCW::GetMarshalingType: QI for INoMarshal");

        if (SUCCEEDED(hr))
            return MarshalingType_Inhibit;
        if (IUnkEntry::IsComponentFreeThreaded(pUnk))
            return MarshalingType_FreeThreaded;
    }
    return MarshalingType_Unknown;
}

void RCW::AddMemoryPressure(GCPressureSize pressureSize)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    int pressure = s_rGCPressureTable[pressureSize];
    GCInterface::AddMemoryPressure(pressure);

    // Remember the pressure we set.
    m_Flags.m_GCPressure = pressureSize;
}

void RCW::RemoveMemoryPressure()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION((GetThread()->m_StateNC & Thread::TSNC_UnsafeSkipEnterCooperative) == 0);
    }
    CONTRACTL_END;

    if (GCPressureSize_None == m_Flags.m_GCPressure)
        return;

    int pressure = s_rGCPressureTable[m_Flags.m_GCPressure];
    GCInterface::RemoveMemoryPressure(pressure);

    m_Flags.m_GCPressure = GCPressureSize_None;
}


//--------------------------------------------------------------------------------
// Addref is called only from within the runtime, when we lookup a wrapper in our hash
// table
LONG RCW::AddRef(RCWCache* pWrapCache)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrapCache));
        PRECONDITION(pWrapCache->LOCKHELD());
    }
    CONTRACTL_END;

    LONG cbRef = ++m_cbRefCount;
    return cbRef;
}

AppDomain* RCW::GetDomain()
{
    CONTRACT (AppDomain*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    RETURN m_pRCWCache->GetDomain();
}

//--------------------------------------------------------------------------------
// Used to facilitate the ReleaseComObject API.
//  Ensures that the RCW is not in use before attempting to release it.
//
INT32 RCW::ExternalRelease(OBJECTREF* pObjPROTECTED)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pObjPROTECTED != NULL);
        PRECONDITION(*pObjPROTECTED != NULL);
    }
    CONTRACTL_END;

    COMOBJECTREF* cref = (COMOBJECTREF*)pObjPROTECTED;

    INT32 cbRef = -1;
    BOOL fCleanupWrapper = FALSE;
    RCW* pRCW = NULL;

    // Lock
    RCWCache* pCache = RCWCache::GetRCWCache();
    _ASSERTE(pCache);

    {
        RCWCache::LockHolder lh(pCache);

        // now to see if the wrapper is valid
        // if there is another ReleaseComObject on this object
        // of if an STA thread death decides to cleanup this wrapper
        // then the object will be disconnected from the wrapper
        pRCW = (*cref)->GetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();

        if (pRCW)
        {
            // check for invalid case
            if ((LONG)pRCW->m_cbRefCount > 0)
            {
                cbRef = (INT32) (--(pRCW->m_cbRefCount));
                if (cbRef == 0)
                {
                    pCache->RemoveWrapper(pRCW);
                    fCleanupWrapper = TRUE;
                }
            }
        }
    }

    // do cleanup after releasing the lock
    if (fCleanupWrapper)
    {
        // Release all the data associated with the __ComObject.
        ComObject::ReleaseAllData(pRCW->GetExposedObject());

        pRCW->DecoupleFromObject();
        pRCW->Cleanup();
    }

    return cbRef;
}


//--------------------------------------------------------------------------------
// Used to facilitate the FinalReleaseComObject API.
//  Ensures that the RCW is not in use before attempting to release it.
//
void RCW::FinalExternalRelease(OBJECTREF* pObjPROTECTED)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pObjPROTECTED != NULL);
        PRECONDITION(*pObjPROTECTED != NULL);
    }
    CONTRACTL_END;

    COMOBJECTREF* cref = (COMOBJECTREF*)pObjPROTECTED;
    BOOL fCleanupWrapper = FALSE;
    RCW* pRCW = NULL;

     // Lock
    RCWCache* pCache = RCWCache::GetRCWCache();
    _ASSERTE(pCache);

    {
        RCWCache::LockHolder lh(pCache);

        pRCW = (*cref)->GetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();

        if (pRCW && pRCW->m_cbRefCount > 0)
        {
            pRCW->m_cbRefCount = 0;
            pCache->RemoveWrapper(pRCW);
            fCleanupWrapper = TRUE;
        }
    }

    // do cleanup after releasing the lock
    if (fCleanupWrapper)
    {
        // Release all the data associated with the __ComObject.
        ComObject::ReleaseAllData(pRCW->GetExposedObject());

        pRCW->DecoupleFromObject();
        pRCW->Cleanup();
    }
}


//--------------------------------------------------------------------------------
// schedule to free all interface pointers, called during GC to
// do minimal work
void RCW::MinorCleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(GCHeapUtilities::IsGCInProgress() || ( (g_fEEShutDown & ShutDown_SyncBlock) && g_fProcessDetach ));
    }
    CONTRACTL_END;

    // Log the wrapper minor cleanup.
    LogRCWMinorCleanup(this);

    // remove the wrapper from the cache, so that
    // other threads won't find this invalid wrapper
    // NOTE: we don't need to LOCK because we make sure
    // the rest of the folks touch this hash table
    // with thier GC mode pre-emptiveGCDisabled
    RCWCache* pCache = m_pRCWCache;
    _ASSERTE(pCache);

    // On server build, multiple threads will be removing
    // wrappers from wrapper cache,
    pCache->RemoveWrapper(this);

    // Clear the SyncBlockIndex as the object is being GC'd and the index will become
    // invalid as soon as the object is collected.
    m_SyncBlockIndex = 0;
}

//--------------------------------------------------------------------------------
// Cleanup free all interface pointers
void RCW::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Log the destruction of the RCW.
    LogRCWDestroy(this);

    // If we can't switch to cooperative mode, then we need to skip the check to
    // if the wrapper is still in the cache.  Also, if we can't switch to coop mode,
    // we're guaranteed to have already decoupled the RCW from its object.
#ifdef _DEBUG
    if (!(GetThread()->m_StateNC & Thread::TSNC_UnsafeSkipEnterCooperative))
    {
        GCX_COOP();

        // make sure this wrapper is not in the hash table
        RCWCache::LockHolder lh(m_pRCWCache);
        _ASSERTE(m_pRCWCache->LookupWrapperUnsafe(m_pIdentity) != this);
    }
#endif

    // Switch to preemptive GC mode before we release the interfaces.
    {
        GCX_PREEMP();

        // Release the IUnkEntry and the InterfaceEntries.
        ReleaseAllInterfacesCallBack(this);

        // Remove the memory pressure caused by this RCW (if present)
        // If we're in a shutdown situation, we can ignore the memory pressure.
        if ((GetThread()->m_StateNC & Thread::TSNC_UnsafeSkipEnterCooperative) == 0 && !g_fForbidEnterEE)
            RemoveMemoryPressure();
    }

#ifdef _DEBUG
    m_cbRefCount = 0;
    m_SyncBlockIndex = 0;
#endif

    // If there's no thread currently working with the RCW, this call will release helper fields on IUnkEntry
    // and recycle the entire RCW structure, i.e. insert it in the standby list to be reused or free the memory.
    // If a thread still keeps a ref-count on the RCW, it will release it when it's done. Keeping the structure
    // and the helper fields alive reduces the chances of memory corruption in race scenarios.
    DecrementUseCount();
}


//--------------------------------------------------------------------------------
// Create a new wrapper for a different method table that represents the same
// COM object as the original wrapper.
void RCW::CreateDuplicateWrapper(MethodTable *pNewMT, RCWHolder* pNewRCW)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pNewMT));
        PRECONDITION(pNewMT->IsComObjectType());
        PRECONDITION(CheckPointer(pNewRCW));
        //POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACTL_END;

    NewRCWHolder pNewWrap;

    // Validate that there exists a default constructor for the new wrapper class.
    if (!pNewMT->HasDefaultConstructor())
        COMPlusThrow(kArgumentException, IDS_EE_WRAPPER_MUST_HAVE_DEF_CONS);

    // Allocate the wrapper COM object.
    COMOBJECTREF NewWrapperObj = (COMOBJECTREF)ComObject::CreateComObjectRef(pNewMT);
    GCPROTECT_BEGIN(NewWrapperObj)
    {
        SafeComHolder<IUnknown> pAutoUnk = NULL;

        // Retrieve the RCWCache to use.
        RCWCache* pCache = RCWCache::GetRCWCache();

        // Create the new RCW associated with the COM object. We need
        // to set the identity to some default value so we don't remove the original
        // wrapper from the hash table when this wrapper goes away.
        pAutoUnk = GetIUnknown();

        DWORD flags = 0;

        // make sure we "pin" the syncblock before switching to preemptive mode
        SyncBlock *pSB = NewWrapperObj->GetSyncBlock();
        pSB->SetPrecious();
        DWORD dwSyncBlockIndex = pSB->GetSyncBlockIndex();

        pNewWrap = RCW::CreateRCW((IUnknown *)pAutoUnk, dwSyncBlockIndex, flags, pNewMT);

        // Reset the Identity to be the RCW* as we don't want to create a duplicate entry
        pNewWrap->m_pIdentity = (LPVOID)pNewWrap;

        // Run the class constructor if it has not run yet.
        pNewMT->CheckRunClassInitThrowing();

        CallDefaultConstructor(ObjectToOBJECTREF(NewWrapperObj));

        pNewRCW->InitNoCheck(NewWrapperObj);

        // Insert the wrapper into the hashtable. The wrapper will be a duplicate however we
        // we fix the identity to ensure there is no collison in the hash table & it is required
        // since the hashtable is used on appdomain unload to determine what RCW's need to released.
        {
            RCWCache::LockHolder lh(pCache);
            pCache->InsertWrapper(pNewRCW);
        }
    }
    GCPROTECT_END();

    pNewWrap.SuppressRelease();
}

//--------------------------------------------------------------------------------
// Calling this is relatively slow since it can't take advantage of the cache
// since there is no longer any way to go from an IID to a MethodTable.
// If at all possible you should use the version that takes a MethodTable.
// This usually means calling GetComIPFromObjectRef passing in a MethodTable
// instead of an IID.
IUnknown* RCW::GetComIPFromRCW(REFIID iid)
{
    CONTRACT(IUnknown *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    SafeComHolder<IUnknown> pRet = NULL;
    HRESULT hr = S_OK;

    hr = SafeQueryInterfaceRemoteAware(iid, (IUnknown**)&pRet);
    if (hr != E_NOINTERFACE)
    {
        // We simply return NULL on E_NOINTERFACE which is much better for perf than throwing exceptions. Note
        // that we can hit this code path often in aggregation scenarios where we forward QI's to the COM base class.
        IfFailThrow(hr);
    }
    else
    {
        // Clear the return value in case we got E_NOINTERFACE but a non-NULL pUnk.
        pRet.Clear();
    }

    pRet.SuppressRelease();
    RETURN pRet;
}

//--------------------------------------------------------------------------------
// check the local cache, out of line cache
// if not found QI for the interface and store it
IUnknown* RCW::GetComIPFromRCW(MethodTable* pMT)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    if (pMT == NULL || pMT->IsObjectClass())
    {
        // give out the IUnknown or IDispatch
        IUnknown *result = GetIUnknown();
        _ASSERTE(result != NULL);
        RETURN result;
    }

    // returns an AddRef'ed IP
    RETURN GetComIPForMethodTableFromCache(pMT);
}


//-----------------------------------------------------------------
// Get the IUnknown pointer for the wrapper
// make sure it is on the right thread
IUnknown* RCW::GetIUnknown()
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Try to retrieve the IUnknown in the current context.
    RETURN m_UnkEntry.GetIUnknownForCurrContext(false);
}

//-----------------------------------------------------------------
// Get the IUnknown pointer for the wrapper, non-AddRef'ed.
// Generally this will work only if we are on the right thread,
// otherwise NULL will be returned.
IUnknown* RCW::GetIUnknown_NoAddRef()
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // Retrieve the IUnknown in the current context.
    RETURN m_UnkEntry.GetIUnknownForCurrContext(true);
}

IUnknown *RCW::GetWellKnownInterface(REFIID riid)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    IUnknown *pUnk = NULL;

    // QI for riid.
    HRESULT hr = SafeQueryInterfaceRemoteAware(riid, &pUnk);
    if ( S_OK !=  hr )
    {
        // If anything goes wrong simply set pUnk to NULL to indicate that
        // the wrapper does not support given riid.
        pUnk = NULL;
    }

    // Return the IDispatch that is guaranteed to be valid on the current thread.
    RETURN pUnk;
}

//-----------------------------------------------------------------
// Get the IUnknown pointer for the wrapper
// make sure it is on the right thread
IDispatch *RCW::GetIDispatch()
{
    WRAPPER_NO_CONTRACT;
    return (IDispatch *)GetWellKnownInterface(IID_IDispatch);
}

//-----------------------------------------------
// Free GC handle and remove SyncBlock entry
void RCW::DecoupleFromObject()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (m_SyncBlockIndex != 0)
    {
        // remove reference to wrapper from sync block
        SyncBlock* pSB = GetSyncBlock();
        _ASSERTE(pSB);

        InteropSyncBlockInfo* pInteropInfo = pSB->GetInteropInfoNoCreate();
        _ASSERTE(pInteropInfo);

        pInteropInfo->SetRawRCW(NULL);

        m_SyncBlockIndex = 0;
    }
}

HRESULT RCW::SafeQueryInterfaceRemoteAware(REFIID iid, IUnknown** ppResUnk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SafeComHolder<IUnknown> pUnk(GetIUnknown_NoAddRef(), /*takeOwnership =*/ FALSE);
    if (pUnk == NULL)
    {
        // if we are not on the right thread we get a proxy which we need to keep AddRef'ed
        pUnk = GetIUnknown();
    }

    RCW_VTABLEPTR(this);

    HRESULT hr = SafeQueryInterface(pUnk, iid, ppResUnk);
    LogInteropQI(pUnk, iid, hr, "QI for interface in SafeQueryInterfaceRemoteAware");

    if (hr == CO_E_OBJNOTCONNECTED || hr == RPC_E_INVALID_OBJECT || hr == RPC_E_INVALID_OBJREF || hr == CO_E_OBJNOTREG)
    {
        // set apartment state
        GetThread()->SetApartment(Thread::AS_InMTA);

        // Release the stream of the IUnkEntry to force UnmarshalIUnknownForCurrContext
        // to remarshal to the stream.
        m_UnkEntry.ReleaseStream();

        // Unmarshal again to the current context to get a valid proxy.
        IUnknown *pTmpUnk = m_UnkEntry.UnmarshalIUnknownForCurrContext();

        // Try to QI for the interface again.
        hr = SafeQueryInterface(pTmpUnk, iid, ppResUnk);
        LogInteropQI(pTmpUnk, iid, hr, "SafeQIRemoteAware - QI for Interface after lost");

        // release our ref-count on pTmpUnk
        int cbRef = SafeRelease(pTmpUnk);
        LogInteropRelease(pTmpUnk, cbRef, "SafeQIRemoteAware - Release for Interface after lost");
    }

    return hr;
}


// Performs QI for the given interface, optionally instantiating it with the given generic args.
HRESULT RCW::CallQueryInterface(MethodTable *pMT, Instantiation inst, IID *piid, IUnknown **ppUnk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr;

    if (!inst.IsEmpty())
    {
        pMT = TypeHandle(pMT).Instantiate(inst).GetMethodTable();
    }

    pMT->GetGuid(piid, TRUE);
    hr = SafeQueryInterfaceRemoteAware(*piid, ppUnk);
    return hr;
}

//-----------------------------------------------------------------
// Retrieve correct COM IP for the method table
// for the current apartment, use the cache and update the cache on miss
IUnknown* RCW::GetComIPForMethodTableFromCache(MethodTable* pMT)
{
    CONTRACT(IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ULONG cbRef;
    IUnknown* pUnk = 0;
    IID iid;
    HRESULT hr;
    int i;

    LPVOID pCtxCookie = GetCurrentCtxCookie();
    _ASSERTE(pCtxCookie != NULL);

    RCW_VTABLEPTR(this);

    // Check whether we can satisfy this request from our cache.
    if (pCtxCookie == GetWrapperCtxCookie() || IsFreeThreaded())
    {
       for (i = 0; i < INTERFACE_ENTRY_CACHE_SIZE; i++)
       {
           if (m_aInterfaceEntries[i].m_pMT == (IE_METHODTABLE_PTR)pMT)
           {
                _ASSERTE(!m_aInterfaceEntries[i].IsFree());

                pUnk = m_aInterfaceEntries[i].m_pUnknown;
                _ASSERTE(pUnk != NULL);

                cbRef = SafeAddRef(pUnk);
                LogInteropAddRef(pUnk, cbRef, "RCW::GetComIPForMethodTableFromCache: Addref because returning pUnk fetched from InterfaceEntry cache");
                RETURN pUnk;
            }
        }
    }

    // We're going to be making some COM calls, better initialize COM.
    EnsureComStarted();

    // First, try to QI for the interface that we were asked for
    hr = CallQueryInterface(pMT, Instantiation(), &iid, &pUnk);

    if (pUnk == NULL)
        RETURN NULL;

    // try to cache the interface pointer in the inline cache. This cache can only store interface pointers
    // returned from QI's in the same context where we created the RCW.
    if (GetWrapperCtxCookie() == pCtxCookie || IsFreeThreaded())
    {
        for (i = 0; i < INTERFACE_ENTRY_CACHE_SIZE; i++)
        {
            if (m_aInterfaceEntries[i].IsFree() && m_aInterfaceEntries[i].Init(pMT, pUnk))
            {
                // If the component is not aggregated then we need to ref-count
                if (!IsURTAggregated())
                {
                    // Get an extra addref to hold this reference alive in our cache
                    cbRef = SafeAddRef(pUnk);
                    LogInteropAddRef(pUnk, cbRef, "RCW::GetComIPForMethodTableFromCache: Addref because storing pUnk in InterfaceEntry cache");
                }

                break;
            }
        }
    }

    RETURN pUnk;
}

//----------------------------------------------------------
// Determine if the COM object supports IProvideClassInfo.
BOOL RCW::SupportsIProvideClassInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL bSupportsIProvideClassInfo = FALSE;
    SafeComHolder<IUnknown> pProvClassInfo = NULL;

    // QI for IProvideClassInfo on the COM object.
    HRESULT hr = SafeQueryInterfaceRemoteAware(IID_IProvideClassInfo, &pProvClassInfo);

    // Check to see if the QI for IProvideClassInfo succeeded.
    if (SUCCEEDED(hr))
    {
        _ASSERTE(pProvClassInfo);
        bSupportsIProvideClassInfo = TRUE;
    }

    return bSupportsIProvideClassInfo;
}

BOOL RCW::AllowEagerSTACleanup()
{
    LIMITED_METHOD_CONTRACT;

    // We only consider STA threads. MTA threads should have been dealt
    // with before calling this.
    _ASSERTE(GetSTAThread() != NULL);

    return m_Flags.m_fAllowEagerSTACleanup;
}

HRESULT RCW::EnterContext(PFNCTXCALLBACK pCallbackFunc, LPVOID pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!IsFreeThreaded());
        PRECONDITION(GetWrapperCtxEntryNoRef() != NULL);
    }
    CONTRACTL_END;

    CtxEntryHolder pCtxEntry = GetWrapperCtxEntry();
    return pCtxEntry->EnterContext(pCallbackFunc, pData);
}

//---------------------------------------------------------------------
// Callback called to release the IUnkEntry and the Interface entries.
HRESULT __stdcall RCW::ReleaseAllInterfacesCallBack(LPVOID pData)
{
    CONTRACT(HRESULT)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pData));
        POSTCONDITION(SUCCEEDED(RETVAL));
    }
    CONTRACT_END;

    RCW* pWrap = (RCW*)pData;

    RCW_VTABLEPTR(pWrap);

    LPVOID pCurrentCtxCookie = GetCurrentCtxCookie();
    if (pCurrentCtxCookie == NULL || pCurrentCtxCookie == pWrap->GetWrapperCtxCookie() || pWrap->IsFreeThreaded())
    {
        pWrap->ReleaseAllInterfaces();
    }
    else
    {
        // Transition into the context to release the interfaces.
        HRESULT hr = pWrap->EnterContext(ReleaseAllInterfacesCallBack, pWrap);
        if (FAILED(hr))
        {
            // The context is disconnected so we cannot transition into it to clean up.
            // The only option we have left is to try and release the interfaces from
            // the current context. This will work for context agile object's since we have
            // a pointer to them directly. It will however fail for others since we only
            // have a pointer to a proxy which is no longer attached to the object.

            pWrap->ReleaseAllInterfaces();
        }
    }

    RETURN S_OK;
}

//---------------------------------------------------------------------
// Helper function called from ReleaseAllInterfacesCallBack do do the
// actual releases.
void RCW::ReleaseAllInterfaces()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    RCW_VTABLEPTR(this);

    // Release the pUnk held by IUnkEntry
    m_UnkEntry.ReleaseInterface(this);

    // If this wrapper is not an Extensible RCW, free all the interface entries that have been allocated.
    if (!IsURTAggregated())
    {
        for (int i = m_Flags.m_iEntryToRelease; i < INTERFACE_ENTRY_CACHE_SIZE; i++)
        {
            // Make sure we never try to clean this up again (so if we bail, we'll leak it).
            m_Flags.m_iEntryToRelease++;

            if (!m_aInterfaceEntries[i].IsFree())
            {
                DWORD cbRef = SafeReleasePreemp(m_aInterfaceEntries[i].m_pUnknown, this);
                LogInteropRelease(m_aInterfaceEntries[i].m_pUnknown, cbRef, "RCW::ReleaseAllInterfaces: Releasing ref from InterfaceEntry table");
            }
        }
    }
}

//---------------------------------------------------------------------
// Returns true if the RCW supports given "standard managed" interface.
bool RCW::SupportsMngStdInterface(MethodTable *pItfMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pItfMT));
    }
    CONTRACTL_END;

    //
    // Handle casts to normal managed standard interfaces.
    //

    // Check to see if the interface is a managed standard interface.
    IID *pNativeIID = MngStdInterfaceMap::GetNativeIIDForType(pItfMT);
    if (pNativeIID != NULL)
    {
        // It is a managed standard interface so we need to check to see if the COM component
        // implements the native interface associated with it.
        SafeComHolder<IUnknown> pNativeItf = NULL;

        // QI for the native interface.
        SafeQueryInterfaceRemoteAware(*pNativeIID, &pNativeItf);

        // If the component supports the native interface then we can say it implements the
        // standard interface.
        if (pNativeItf)
            return true;
    }
    else
    {
        //
        // Handle casts to IEnumerable.
        //

        // If the requested interface is IEnumerable then we need to check to see if the
        // COM object implements IDispatch and has a member with DISPID_NEWENUM.
        if (pItfMT == CoreLibBinder::GetClass(CLASS__IENUMERABLE))
        {
            SafeComHolder<IDispatch> pDisp = GetIDispatch();
            if (pDisp)
            {
                DISPPARAMS DispParams = {0, 0, NULL, NULL};
                VariantHolder VarResult;

                // Initialize the return variant.
                SafeVariantInit(&VarResult);

                HRESULT hr = E_FAIL;
                {
                    // We are about to make a call to COM so switch to preemptive GC.
                    GCX_PREEMP();

                    // Call invoke with DISPID_NEWENUM to see if such a member exists.
                    hr = pDisp->Invoke(
                                        DISPID_NEWENUM,
                                        IID_NULL,
                                        LOCALE_USER_DEFAULT,
                                        DISPATCH_METHOD | DISPATCH_PROPERTYGET,
                                        &DispParams,
                                        &VarResult,
                                        NULL,
                                        NULL
                                      );
                }

                // If the invoke succeeded then the component has a member DISPID_NEWENUM
                // so we can expose it as an IEnumerable.
                if (SUCCEEDED(hr))
                    return true;
            }
        }
    }

    return false;
}

//--------------------------------------------------------------------------------
// OBJECTREF ComObject::CreateComObjectRef(MethodTable* pMT)
//  returns NULL for out of memory scenarios
OBJECTREF ComObject::CreateComObjectRef(MethodTable* pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(pMT->IsComObjectType());
    }
    CONTRACTL_END;

    if (pMT != g_pBaseCOMObject)
    {
        pMT->CheckRestore();
        pMT->EnsureInstanceActive();
        pMT->CheckRunClassInitThrowing();
    }

    return AllocateObject(pMT, false);
}


//--------------------------------------------------------------------------------
// SupportsInterface
BOOL ComObject::SupportsInterface(OBJECTREF oref, MethodTable* pIntfTable)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(oref != NULL);
        PRECONDITION(CheckPointer(pIntfTable));
    }
    CONTRACTL_END

    SafeComHolder<IUnknown> pUnk = NULL;
    HRESULT hr;
    BOOL bSupportsItf = FALSE;

    GCPROTECT_BEGIN(oref);

    // Make sure the interface method table has been restored.
    pIntfTable->CheckRestore();

    if (pIntfTable->GetComInterfaceType() == ifInspectable)
    {
        COMPlusThrow(kPlatformNotSupportedException, IDS_EE_NO_IINSPECTABLE);
    }

    // Check to see if the static class definition indicates we implement the interface.
    MethodTable *pMT = oref->GetMethodTable();
    if (pMT->CanCastToInterface(pIntfTable))
    {
        bSupportsItf = TRUE;
    }
    else
    {
        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, oref);

        // This should not be called for interfaces that are in the normal portion of the
        // interface map for this class. The only interfaces that are in the interface map
        // but are not in the normal portion are the dynamic interfaces on extensible RCW's.
        _ASSERTE(!oref->GetMethodTable()->ImplementsInterface(pIntfTable));


        //
        // First QI the object to see if it implements the specified interface.
        //

        pUnk = pRCW->GetComIPFromRCW(pIntfTable);
        if (pUnk)
        {
            bSupportsItf = true;
        }
        else if (pIntfTable->IsComEventItfType())
        {
            MethodTable *pSrcItfClass = NULL;
            MethodTable *pEvProvClass = NULL;
            GUID SrcItfIID;
            SafeComHolder<IConnectionPointContainer> pCPC = NULL;
            SafeComHolder<IConnectionPoint> pCP = NULL;

            // Retrieve the IID of the source interface associated with this
            // event interface.
            pIntfTable->GetEventInterfaceInfo(&pSrcItfClass, &pEvProvClass);
            pSrcItfClass->GetGuid(&SrcItfIID, TRUE);

            // QI for IConnectionPointContainer.
            hr = pRCW->SafeQueryInterfaceRemoteAware(IID_IConnectionPointContainer, (IUnknown**)&pCPC);

            // If the component implements IConnectionPointContainer, then check
            // to see if it handles the source interface.
            if (SUCCEEDED(hr))
            {
                GCX_PREEMP();   // make sure we switch to preemptive mode before calling the external COM object
                hr = pCPC->FindConnectionPoint(SrcItfIID, &pCP);
                if (SUCCEEDED(hr))
                {
                    // The component handles the source interface so we can succeed the QI call.
                    bSupportsItf = true;
                }
            }
        }
        else if (pRCW->SupportsMngStdInterface(pIntfTable))
        {
            bSupportsItf = true;
        }

        if (bSupportsItf)
        {
            // If the object has a dynamic interface map then we have extra work to do.
            MethodTable *pMT = oref->GetMethodTable();
            if (pMT->HasDynamicInterfaceMap())
            {
                // First, make sure we haven't already added this.
                if (!pMT->FindDynamicallyAddedInterface(pIntfTable))
                {
                    // It's not there.
                    // Check if the object supports all of these interfaces only if this is a classic COM interop
                    // scenario. This is a perf optimization (no need to QI for base interfaces if we don't really
                    // need them just yet) and also has a usability aspect. If this SupportsInterface call failed
                    // because one of the base interfaces is not supported, the exception we'd throw would contain
                    // only the name of the "top level" interface which would confuse the developer.
                    MethodTable::InterfaceMapIterator it = pIntfTable->IterateInterfaceMap();
                    while (it.Next())
                    {
                        MethodTable *pItf = it.GetInterfaceApprox();
                        if (pItf->HasInstantiation() || pItf->IsGenericTypeDefinition())
                            continue;

                        bSupportsItf = Object::SupportsInterface(oref, pItf);
                        if (!bSupportsItf)
                            break;
                    }

                    // If the object supports all these interfaces, attempt to add the interface table
                    //  to the cache.
                    if (bSupportsItf)
                    {
                        {
                            // Take the wrapper cache lock before we start playing with the interface map.
                            RCWCache::LockHolder lh(RCWCache::GetRCWCache());

                            // Check again with the lock.
                            if (!pMT->FindDynamicallyAddedInterface(pIntfTable))
                            {
                                // Add it to the dynamic interface table.
                                pMT->AddDynamicInterface(pIntfTable);
                            }
                        }
                    }
                }
            }
        }

        RCWPROTECT_END(pRCW);
    }

    GCPROTECT_END();

    return bSupportsItf;

}

//--------------------------------------------------------------------
// ThrowInvalidCastException
void ComObject::ThrowInvalidCastException(OBJECTREF *pObj, MethodTable *pCastToMT)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pObj != NULL);
        PRECONDITION(*pObj != NULL);
        PRECONDITION(IsProtectedByGCFrame (pObj));
        POSTCONDITION(!"This function should never return!");
    }
    CONTRACT_END;

    SafeComHolder<IUnknown> pItf = NULL;
    HRESULT hr = S_OK;
    IID *pNativeIID = NULL;
    GUID iid;

    // Use an InlineSString with a size of MAX_CLASSNAME_LENGTH + 1 to prevent
    // TypeHandle::GetName from having to allocate a new block of memory. This
    // significantly improves the performance of throwing an InvalidCastException.
    InlineSString<MAX_CLASSNAME_LENGTH + 1> strComObjClassName;
    InlineSString<MAX_CLASSNAME_LENGTH + 1> strCastToName;

    TypeHandle thClass = (*pObj)->GetTypeHandle();
    TypeHandle thCastTo = TypeHandle(pCastToMT);

    thClass.GetName(strComObjClassName);
    thCastTo.GetName(strCastToName);

    if (thCastTo.IsInterface())
    {
        RCWHolder pRCW(GetThread());
        pRCW.Init(*pObj);

        // Retrieve the IID of the interface.
        MethodTable *pCOMItfMT = thCastTo.GetMethodTable();

        // keep calling the throwing GetGuid (back compat)
        pCOMItfMT->GetGuid(&iid, TRUE);

        // Query for the interface to determine the failure HRESULT.
        hr = pRCW->SafeQueryInterfaceRemoteAware(iid, (IUnknown**)&pItf);

        // If this function was called, it means the QI call failed in the past. If it
        // no longer fails now, we still need to throw, so throw a generic invalid cast exception.
        if (SUCCEEDED(hr))
        {
            COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST, strComObjClassName.GetUnicode(), strCastToName.GetUnicode());
        }

        // Convert the IID to a string.
        WCHAR strIID[39];
        StringFromGUID2(iid, strIID, sizeof(strIID) / sizeof(WCHAR));

        // Obtain the textual description of the HRESULT.
        SString strHRDescription;
        GetHRMsg(hr, strHRDescription);

        if (thCastTo.IsComEventItfType())
        {
            GUID SrcItfIID;
            MethodTable *pSrcItfClass = NULL;
            MethodTable *pEvProvClass = NULL;

            // Retrieve the IID of the source interface associated with this event interface.
            thCastTo.GetMethodTable()->GetEventInterfaceInfo(&pSrcItfClass, &pEvProvClass);
            pSrcItfClass->GetGuid(&SrcItfIID, TRUE);

            // Convert the source interface IID to a string.
            WCHAR strSrcItfIID[39];
            StringFromGUID2(SrcItfIID, strSrcItfIID, sizeof(strSrcItfIID) / sizeof(WCHAR));

            COMPlusThrow(kInvalidCastException, IDS_EE_RCW_INVALIDCAST_EVENTITF, strHRDescription.GetUnicode(), strComObjClassName.GetUnicode(),
                strCastToName.GetUnicode(), strIID, strSrcItfIID);
        }
        else if (thCastTo == TypeHandle(CoreLibBinder::GetClass(CLASS__IENUMERABLE)))
        {
            COMPlusThrow(kInvalidCastException, IDS_EE_RCW_INVALIDCAST_IENUMERABLE,
                strHRDescription.GetUnicode(), strComObjClassName.GetUnicode(), strCastToName.GetUnicode(), strIID);
        }
        else if ((pNativeIID = MngStdInterfaceMap::GetNativeIIDForType(thCastTo)) != NULL)
        {
            // Convert the source interface IID to a string.
            WCHAR strNativeItfIID[39];
            StringFromGUID2(*pNativeIID, strNativeItfIID, sizeof(strNativeItfIID) / sizeof(WCHAR));

            // Query for the interface to determine the failure HRESULT.
            HRESULT hr2 = pRCW->SafeQueryInterfaceRemoteAware(iid, (IUnknown**)&pItf);

            // If this function was called, it means the QI call failed in the past. If it
            // no longer fails now, we still need to throw, so throw a generic invalid cast exception.
            if (SUCCEEDED(hr2))
                COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST, strComObjClassName.GetUnicode(), strCastToName.GetUnicode());

            // Obtain the textual description of the 2nd HRESULT.
            SString strHR2Description;
            GetHRMsg(hr2, strHR2Description);

            COMPlusThrow(kInvalidCastException, IDS_EE_RCW_INVALIDCAST_MNGSTDITF, strHRDescription.GetUnicode(), strComObjClassName.GetUnicode(),
                strCastToName.GetUnicode(), strIID, strNativeItfIID, strHR2Description.GetUnicode());
        }
        else
        {
            COMPlusThrow(kInvalidCastException, IDS_EE_RCW_INVALIDCAST_ITF,
                strHRDescription.GetUnicode(), strComObjClassName.GetUnicode(), strCastToName.GetUnicode(), strIID);
        }
    }
    else
    {
        // Validate that this function wasn't erroneously called.
        _ASSERTE(!thClass.CanCastTo(thCastTo));

        if (thCastTo.IsComObjectType())
        {
            if (IsComObjectClass(thClass))
            {
                // An attempt was made to cast an __ComObject to ComImport metadata defined type.
                COMPlusThrow(kInvalidCastException, IDS_EE_RCW_INVALIDCAST_COMOBJ_TO_MD,
                    strComObjClassName.GetUnicode(), strCastToName.GetUnicode());
            }
            else
            {
                // An attempt was made to cast an instance of a ComImport metadata defined type to
                // a different non ComImport metadata defined type.
                COMPlusThrow(kInvalidCastException, IDS_EE_RCW_INVALIDCAST_MD_TO_MD,
                    strComObjClassName.GetUnicode(), strCastToName.GetUnicode());
            }
        }
        else
        {
            // An attempt was made to cast this RCW to a non ComObjectType class.
            COMPlusThrow(kInvalidCastException, IDS_EE_RCW_INVALIDCAST_TO_NON_COMOBJTYPE,
                strComObjClassName.GetUnicode(), strCastToName.GetUnicode());
        }
    }

    RETURN;
}

//--------------------------------------------------------------------------------
// Release all the data associated with the __ComObject.
void ComObject::ReleaseAllData(OBJECTREF oref)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(oref != NULL);
        PRECONDITION(oref->GetMethodTable()->IsComObjectType());
    }
    CONTRACTL_END;

    GCPROTECT_BEGIN(oref)
    {
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COM_OBJECT__RELEASE_ALL_DATA);

        DECLARE_ARGHOLDER_ARRAY(ReleaseAllDataArgs, 1);
        ReleaseAllDataArgs[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(oref);

        CALL_MANAGED_METHOD_NORET(ReleaseAllDataArgs);
    }
    GCPROTECT_END();
}

#ifndef DACCESS_COMPILE
//--------------------------------------------------------------------------
// Wrapper around code:RCW.GetComIPFromRCW
// static
IUnknown *ComObject::GetComIPFromRCW(OBJECTREF *pObj, MethodTable* pIntfTable)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame(pObj));
        PRECONDITION(CheckPointer(pIntfTable, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK)); // NULL if we couldn't find match
    }
    CONTRACT_END;

    SafeComHolder<IUnknown> pIUnk;

    RCWHolder pRCW(GetThread());
    RCWPROTECT_BEGIN(pRCW, *pObj);

    pIUnk = pRCW->GetComIPFromRCW(pIntfTable);

    RCWPROTECT_END(pRCW);
    RETURN pIUnk.Extract();
}

//--------------------------------------------------------------------------
// Wrapper around code:ComObject.GetComIPFromRCW that throws InvalidCastException
// static
IUnknown *ComObject::GetComIPFromRCWThrowing(OBJECTREF *pObj, MethodTable* pIntfTable)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(IsProtectedByGCFrame(pObj));
        PRECONDITION(CheckPointer(pIntfTable, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    IUnknown* pIUnk = GetComIPFromRCW(pObj, pIntfTable);

    if (pIUnk == NULL)
        ThrowInvalidCastException(pObj, pIntfTable);

    RETURN pIUnk;
}
#endif // #ifndef DACCESS_COMPILE


