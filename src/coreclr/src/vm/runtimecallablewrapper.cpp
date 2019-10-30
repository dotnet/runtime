// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "winrttypenameconverter.h"
#include "../md/compiler/custattr.h"
#include "olevariant.h"
#include "interopconverter.h"
#include "typestring.h"
#include "caparser.h"
#include "classnames.h"
#include "objectnative.h"
#include "rcwwalker.h"
#include "finalizerthread.h"

// static
SLIST_HEADER RCW::s_RCWStandbyList;

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

#ifndef CROSSGEN_COMPILE

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
    if (m_pwszServer)
    {
        // Set up the COSERVERINFO struct.
        COSERVERINFO ServerInfo;
        memset(&ServerInfo, 0, sizeof(COSERVERINFO));
        ServerInfo.pwszName = m_pwszServer;
                
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
        if (m_pwszServer == NULL)            
            COMPlusThrowHR(hr, IDS_EE_LOCAL_COGETCLASSOBJECT_FAILED, strHRHex, strClsid, strHRDescription.GetUnicode());
        else
            COMPlusThrowHR(hr, IDS_EE_REMOTE_COGETCLASSOBJECT_FAILED, strHRHex, strClsid, m_pwszServer, strHRDescription.GetUnicode());
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
#endif //#ifndef CROSSGEN_COMPILE

//--------------------------------------------------------------
// Init the ComClassFactory.
void ComClassFactory::Init(__in_opt WCHAR* pwszProgID, __in_opt WCHAR* pwszServer, MethodTable* pClassMT)
{
    LIMITED_METHOD_CONTRACT;

    m_pwszProgID = pwszProgID;
    m_pwszServer = pwszServer;  
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
    
    if (m_pwszProgID != NULL)
        delete [] m_pwszProgID;

    if (m_pwszServer != NULL)
        delete [] m_pwszServer;

    delete this;
}

#if defined(FEATURE_APPX) && !defined(CROSSGEN_COMPILE)
//-------------------------------------------------------------
// Create instance using CoCreateIntanceFromApp
// CoCreateInstanceFromApp is a new Windows 8 API that only
// allow creating COM objects (not WinRT objects) in the allow
// list
// Note: We don't QI for IClassFactory2 in this case as it is not
// supported in ModernSDK
IUnknown *AppXComClassFactory::CreateInstanceInternal(IUnknown *pOuter, BOOL *pfDidContainment)
{
    CONTRACT(IUnknown *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pOuter, NULL_OK));
        PRECONDITION(CheckPointer(pfDidContainment, NULL_OK));
        PRECONDITION(AppX::IsAppXProcess());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    GCX_PREEMP();

    MULTI_QI multiQI;
    ::ZeroMemory(&multiQI, sizeof(MULTI_QI));
    multiQI.pIID = &IID_IUnknown;
    
    HRESULT hr;

#ifdef FEATURE_CORESYSTEM
    // This works around a bug in the Windows 7 loader that prevents us from loading the
    // forwarder for this function
    typedef HRESULT (*CoCreateInstanceFromAppFnPtr) (REFCLSID rclsid, IUnknown *punkOuter, DWORD dwClsCtx,
        void *reserved, DWORD dwCount, MULTI_QI *pResults);

    static CoCreateInstanceFromAppFnPtr CoCreateInstanceFromApp = NULL;
    if (NULL == CoCreateInstanceFromApp)
    {
        HMODULE hmod = LoadLibraryExW(W("api-ms-win-core-com-l1-1-1.dll"), NULL, 0);
    
        if (hmod)
            CoCreateInstanceFromApp = (CoCreateInstanceFromAppFnPtr)GetProcAddress(hmod, "CoCreateInstanceFromApp");
    }

    if (NULL == CoCreateInstanceFromApp)
    {
        // This shouldn't happen
        _ASSERTE(false);
        IfFailThrow(E_FAIL);
    }
#endif
    
    if (m_pwszServer)
    {
        //
        // Remote server activation
        //
        COSERVERINFO ServerInfo;
        ::ZeroMemory(&ServerInfo, sizeof(COSERVERINFO));
        ServerInfo.pwszName = m_pwszServer;
        
        hr = CoCreateInstanceFromApp(
            m_rclsid,
            pOuter,
            CLSCTX_REMOTE_SERVER,
            &ServerInfo,
            1,
            &multiQI);
        if (FAILED(hr) && pOuter)
        {
            //
            // Aggregation attempt failed. Retry containment
            //
            hr = CoCreateInstanceFromApp(
                m_rclsid,
                NULL,
                CLSCTX_REMOTE_SERVER,
                &ServerInfo,
                1,
                &multiQI);
            if (pfDidContainment)
                *pfDidContainment = TRUE;    
        }
     }
    else
    {
        //
        // Normal activation
        //
        hr = CoCreateInstanceFromApp(
            m_rclsid,
            pOuter,
            CLSCTX_SERVER,
            NULL,
            1,
            &multiQI);
        if (FAILED(hr) && pOuter)
        {
            //
            // Aggregation attempt failed. Retry containment
            //
            hr = CoCreateInstanceFromApp(
                m_rclsid,
                NULL,
                CLSCTX_SERVER,
                NULL,
                1,
                &multiQI);
            if (pfDidContainment)
                *pfDidContainment = TRUE;            
        }
    }

    if (FAILED(hr))
        ThrowHRMsg(hr, IDS_EE_CREATEINSTANCEFROMAPP_FAILED);
    if (FAILED(multiQI.hr))
        ThrowHRMsg(multiQI.hr, IDS_EE_CREATEINSTANCEFROMAPP_FAILED);

    RETURN multiQI.pItf;
}
#endif //FEATURE_APPX

//-------------------------------------------------------------
MethodTable *WinRTClassFactory::GetTypeFromAttribute(IMDInternalImport *pImport, mdCustomAttribute tkAttribute)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // get raw custom attribute
    const BYTE  *pbAttr = NULL;
    ULONG cbAttr = 0;
    IfFailThrowBF(pImport->GetCustomAttributeAsBlob(tkAttribute, (const void **)&pbAttr, &cbAttr), BFA_INVALID_TOKEN, m_pClassMT->GetModule());
    
    CustomAttributeParser cap(pbAttr, cbAttr);
    IfFailThrowBF(cap.ValidateProlog(), BFA_BAD_CA_HEADER, m_pClassMT->GetModule());

    // retrieve the factory interface name
    LPCUTF8 szName;
    ULONG   cbName;
    IfFailThrow(cap.GetNonNullString(&szName, &cbName));

    // copy the name to a temporary buffer and NULL terminate it
    StackSString ss(SString::Utf8, szName, cbName);

    // load the factory interface
    return TypeName::GetTypeUsingCASearchRules(ss.GetUnicode(), m_pClassMT->GetAssembly()).GetMethodTable();
}

//-------------------------------------------------------------
// Returns true if the first parameter of the CA's method ctor is a System.Type
static BOOL AttributeFirstParamIsSystemType(mdCustomAttribute tkAttribute, IMDInternalImport *pImport)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pImport));
    }
    CONTRACTL_END;

    mdToken ctorToken;
    IfFailThrow(pImport->GetCustomAttributeProps(tkAttribute, &ctorToken));

    LPCSTR ctorName;
    PCCOR_SIGNATURE ctorSig;
    ULONG cbCtorSig;

    if (TypeFromToken(ctorToken) == mdtMemberRef)
    {
        IfFailThrow(pImport->GetNameAndSigOfMemberRef(ctorToken, &ctorSig, &cbCtorSig, &ctorName));
    }    
    else if (TypeFromToken(ctorToken) == mdtMethodDef)
    {
        IfFailThrow(pImport->GetNameAndSigOfMethodDef(ctorToken, &ctorSig, &cbCtorSig, &ctorName));
    }
    else
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    SigParser sigParser(ctorSig, cbCtorSig);
    
    ULONG callingConvention;
    IfFailThrow(sigParser.GetCallingConvInfo(&callingConvention));
    if (callingConvention != IMAGE_CEE_CS_CALLCONV_HASTHIS)
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    ULONG cParameters;
    IfFailThrow(sigParser.GetData(&cParameters));
    if (cParameters < 1)
    {
        return FALSE;
    }

    BYTE returnElmentType;
    IfFailThrow(sigParser.GetByte(&returnElmentType));
    if (returnElmentType != ELEMENT_TYPE_VOID)
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    BYTE paramElementType;
    IfFailThrow(sigParser.GetByte(&paramElementType));
    if (paramElementType != ELEMENT_TYPE_CLASS)
    {
        return FALSE;
    }

    mdToken paramTypeToken;
    IfFailThrow(sigParser.GetToken(&paramTypeToken));

    if (TypeFromToken(paramTypeToken) != mdtTypeRef)
    {
        return FALSE;
    }

    LPCSTR paramTypeNamespace;
    LPCSTR paramTypeName;
    IfFailThrow(pImport->GetNameOfTypeRef(paramTypeToken, &paramTypeNamespace, &paramTypeName));
    if (strcmp("System", paramTypeNamespace) != 0 || strcmp("Type", paramTypeName) != 0)
    {
        return FALSE;
    }

    return TRUE;
}

//-------------------------------------------------------------
void WinRTClassFactory::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr;
    IMDInternalImport *pImport = m_pClassMT->GetMDImport();

    {
        // Sealed classes may have Windows.Foundation.Activatable attributes.  Such classes must be sealed, because we'd
        // have no way to use their ctor from a derived class (no composition)
        // Unsealed classes may have Windows.Foundation.Composable attributes.  These are currently mutually exclusive, but we
        // may need to relax this in the future for versioning reasons (so a class can be unsealed in a new version without
        // being binary breaking).
        // Note that we just ignore activation attributes if they occur on the wrong type of class
        LPCSTR attributeName;
        if (IsComposition())
        {
            attributeName = g_WindowsFoundationComposableAttributeClassName;
        }
        else
        {
            attributeName = g_WindowsFoundationActivatableAttributeClassName;
        }

        MDEnumHolder hEnum(pImport);
    
        // find and parse all WindowsFoundationActivatableAttribute/WindowsFoundationComposableAttribute attributes
        hr = pImport->EnumCustomAttributeByNameInit(m_pClassMT->GetCl(), attributeName, &hEnum);
        IfFailThrow(hr);
    
        if (hr == S_OK) // there are factory interfaces
        {    
            mdCustomAttribute tkAttribute;
            while (pImport->EnumNext(&hEnum, &tkAttribute))
            {
                if (!AttributeFirstParamIsSystemType(tkAttribute, pImport))
                {
                    // The first parameter of the Composable/Activatable attribute is not a System.Type
                    // and therefore the attribute does not specify a factory interface so we ignore the attribute
                    continue;
                }
                // get raw custom attribute
                const BYTE  *pbAttr = NULL;
                ULONG cbAttr = 0;
                IfFailThrowBF(pImport->GetCustomAttributeAsBlob(tkAttribute, (const void **)&pbAttr, &cbAttr), BFA_INVALID_TOKEN, m_pClassMT->GetModule());
                CustomAttributeParser cap(pbAttr, cbAttr);
                IfFailThrowBF(cap.ValidateProlog(), BFA_BAD_CA_HEADER, m_pClassMT->GetModule());

                // The activation factory interface is stored in the attribute by type name
                LPCUTF8 szFactoryInterfaceName;
                ULONG cbFactoryInterfaceName;
                IfFailThrow(cap.GetNonNullString(&szFactoryInterfaceName, &cbFactoryInterfaceName));

                StackSString strFactoryInterface(SString::Utf8, szFactoryInterfaceName, cbFactoryInterfaceName);
                MethodTable *pMTFactoryInterface = LoadWinRTType(&strFactoryInterface, /* bThrowIfNotFound = */ TRUE).GetMethodTable();

                _ASSERTE(pMTFactoryInterface);
                m_factoryInterfaces.Append(pMTFactoryInterface);
            }
        }
    }

    {
        // find and parse all Windows.Foundation.Static attributes
        MDEnumHolder hEnum(pImport);
        hr = pImport->EnumCustomAttributeByNameInit(m_pClassMT->GetCl(), g_WindowsFoundationStaticAttributeClassName, &hEnum);
        IfFailThrow(hr);
    
        if (hr == S_OK) // there are static interfaces
        {    
            mdCustomAttribute tkAttribute;
            while (pImport->EnumNext(&hEnum, &tkAttribute))
            {
                if (!AttributeFirstParamIsSystemType(tkAttribute, pImport))
                {
                    // The first parameter of the Static attribute is not a System.Type
                    // and therefore the attribute does not specify a factory interface so we ignore the attribute
                    continue;
                }

                const BYTE  *pbAttr = NULL;
                ULONG cbAttr = 0;
                IfFailThrowBF(pImport->GetCustomAttributeAsBlob(tkAttribute, (const void **)&pbAttr, &cbAttr), BFA_INVALID_TOKEN, m_pClassMT->GetModule());
                
                CustomAttributeParser cap(pbAttr, cbAttr);
                IfFailThrowBF(cap.ValidateProlog(), BFA_BAD_CA_HEADER, m_pClassMT->GetModule());
            
                // retrieve the factory interface name
                LPCUTF8 szName;
                ULONG   cbName;
                IfFailThrow(cap.GetNonNullString(&szName, &cbName));
            
                // copy the name to a temporary buffer and NULL terminate it
                StackSString ss(SString::Utf8, szName, cbName);
                TypeHandle th = LoadWinRTType(&ss, /* bThrowIfNotFound = */ TRUE);

                MethodTable *pMTStaticInterface = th.GetMethodTable();
                m_staticInterfaces.Append(pMTStaticInterface);
            }
        }
    }

    {

		// Special case (not pretty): WinMD types requires you to put DefaultAttribute on interfaceImpl to 
		// mark the interface as default interface. But C# doesn't allow you to do that so we have
		// to do it manually here.
		MethodTable* pAsyncTracingEventArgsMT = MscorlibBinder::GetClass(CLASS__ASYNC_TRACING_EVENT_ARGS);
		if(pAsyncTracingEventArgsMT == m_pClassMT)
		{ 
			m_pDefaultItfMT = MscorlibBinder::GetClass(CLASS__IASYNC_TRACING_EVENT_ARGS);
		}
		else
		{
			// parse the DefaultAttribute to figure out the default interface of the class
			HENUMInternalHolder hEnumInterfaceImpl(pImport);
			hEnumInterfaceImpl.EnumInit(mdtInterfaceImpl, m_pClassMT->GetCl());
        
        DWORD cInterfaces = pImport->EnumGetCount(&hEnumInterfaceImpl);
        if (cInterfaces != 0)
        {
            mdInterfaceImpl ii;
            while (pImport->EnumNext(&hEnumInterfaceImpl, &ii))
            {
                const BYTE *pbAttr;
                ULONG cbAttr;
                HRESULT hr = pImport->GetCustomAttributeByName(ii, g_WindowsFoundationDefaultClassName, (const void **)&pbAttr, &cbAttr);
                IfFailThrow(hr);
                if (hr == S_OK)
                {
                    mdToken typeRefOrDefOrSpec;
                    IfFailThrow(pImport->GetTypeOfInterfaceImpl(ii, &typeRefOrDefOrSpec));
                    
                    TypeHandle th = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(
                        m_pClassMT->GetModule(),
                        typeRefOrDefOrSpec,
                        NULL,
                        ClassLoader::ThrowIfNotFound,
                        ClassLoader::FailIfUninstDefOrRef,
                        ClassLoader::LoadTypes, CLASS_LOAD_EXACTPARENTS);

                    m_pDefaultItfMT = th.GetMethodTable();
                    break;
                }
            }
        }
    }
    }

    // initialize m_hClassName
    InlineSString<DEFAULT_NONSTACK_CLASSNAME_SIZE> ssClassName;
    m_pClassMT->_GetFullyQualifiedNameForClass(ssClassName);

#ifndef CROSSGEN_COMPILE
    if (!GetAppDomain()->IsCompilationDomain())
    {
        // don't bother creating the HSTRING when NGENing - we may run on downlevel
        IfFailThrow(WindowsCreateString(ssClassName.GetUnicode(), ssClassName.GetCount(), &m_hClassName));
    }
#endif

    if (ssClassName.BeginsWith(SL(W("Windows."))))
    {
        // parse the GCPressureAttribute only on first party runtime classes
        const BYTE *pVal = NULL;
        ULONG cbVal = 0;

        if (S_OK == pImport->GetCustomAttributeByName(m_pClassMT->GetCl(), g_WindowsFoundationGCPressureAttributeClassName, (const void **)&pVal, &cbVal))
        {
            CustomAttributeParser cap(pVal, cbVal);
            CaNamedArg namedArgs[1];

            // First, the void constructor
            IfFailThrow(ParseKnownCaArgs(cap, NULL, 0));
            
            // Then, find the named argument
            namedArgs[0].InitI4FieldEnum("amount", "Windows.Foundation.Metadata.GCPressureAmount", -1);
            
            IfFailThrow(ParseKnownCaNamedArgs(cap, namedArgs, lengthof(namedArgs)));

            static_assert(RCW::GCPressureSize_WinRT_Medium == RCW::GCPressureSize_WinRT_Low    + 1, "RCW::GCPressureSize does not match Windows.Foundation.Metadata.GCPressureAmount");
            static_assert(RCW::GCPressureSize_WinRT_High   == RCW::GCPressureSize_WinRT_Medium + 1, "RCW::GCPressureSize does not match Windows.Foundation.Metadata.GCPressureAmount");

            int amount = namedArgs[0].val.i4;
            if (amount >= 0 && amount < (RCW::GCPressureSize_COUNT - RCW::GCPressureSize_WinRT_Low))
            {
                m_GCPressure = (RCW::GCPressureSize)(amount + RCW::GCPressureSize_WinRT_Low);
            }
        }
    }
}

//-------------------------------------------------------------
MethodDesc *WinRTClassFactory::FindFactoryMethod(PCCOR_SIGNATURE pSig, DWORD cSig, Module *pModule)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pSig));
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    COUNT_T count = m_factoryInterfaces.GetCount();
    for (UINT i = 0; i < count; i++)
    {
        MethodTable *pMT = m_factoryInterfaces[i];
        
        MethodDesc *pMD = MemberLoader::FindMethod(pMT, "", pSig, cSig, pModule, MemberLoader::FM_IgnoreName);
        if (pMD != NULL)
        {
            return pMD;
        }
    }

    return NULL;
}

//-------------------------------------------------------------
MethodDesc *WinRTClassFactory::FindStaticMethod(LPCUTF8 pszName, PCCOR_SIGNATURE pSig, DWORD cSig, Module *pModule)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pszName));
        PRECONDITION(CheckPointer(pSig));
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;
    
    COUNT_T count = m_staticInterfaces.GetCount();
    for (UINT i = 0; i < count; i++)
    {
        MethodTable *pMT = m_staticInterfaces[i];
        
        MethodDesc *pMD = MemberLoader::FindMethod(pMT, pszName, pSig, cSig, pModule);
        if (pMD != NULL)
        {
            return pMD;
        }
    }

    return NULL;
}

//-------------------------------------------------------------
void WinRTClassFactory::Cleanup()
{
    LIMITED_METHOD_CONTRACT;

    if (m_hClassName != NULL)
    {
        // HSTRING has been created, which means combase should have been loaded.
        // Delay load will not fail.
        _ASSERTE(WszGetModuleHandle(W("combase.dll")) != NULL);
        CONTRACT_VIOLATION(ThrowsViolation);

#ifndef CROSSGEN_COMPILE
        WindowsDeleteString(m_hClassName);
#endif
    }
    delete m_pWinRTOverrideInfo;
    delete this;
}

//-------------------------------------------------------------
void WinRTManagedClassFactory::Cleanup()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pCCWTemplate != NULL)
    {
        m_pCCWTemplate->Release();
        m_pCCWTemplate = NULL;
    }

    WinRTClassFactory::Cleanup(); // deletes 'this'
}
#ifndef CROSSGEN_COMPILE
//-------------------------------------------------------------
ComCallWrapperTemplate *WinRTManagedClassFactory::GetOrCreateComCallWrapperTemplate(MethodTable *pFactoryMT)
{
    CONTRACT (ComCallWrapperTemplate *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pFactoryMT));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (m_pCCWTemplate == NULL)
    {
        ComCallWrapperTemplate::CreateTemplate(TypeHandle(pFactoryMT), this);
    }

    RETURN m_pCCWTemplate;
}
#endif // CROSSGEN_COMPILE

#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

#ifndef CROSSGEN_COMPILE
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
// context (including Jupiter RCWs) or all the wrappers in the cache if the pCtxCookie is null.
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
                // are in that context, including non-FTM regular RCWs, and FTM Jupiter objects 
                // Otherwise clean up all the wrappers.
                // Ignore RCWs that aggregate the FTM if we are cleaning up context
                // specific RCWs (note that we rely on this behavior in WinRT factory cache code)
                // Note that Jupiter RCWs are special and they are considered to be context-bound
                if (!pCtxCookie || ((pWrap->GetWrapperCtxCookie() == pCtxCookie) && (pWrap->IsJupiterObject() || !pWrap->IsFreeThreaded())))
                {
                    if (!pWrap->IsURTAggregated())
                        CleanupList.AddWrapper_NoLock(pWrap);
                    else
                        AggregatedCleanupList.AddWrapper_NoLock(pWrap);

                    pWrap->DecoupleFromObject();                
                    RemoveWrapper(pWrap);
                }
                else if (!pWrap->IsFreeThreaded())
                {
                    // We have a non-zero pCtxCookie but this RCW was not created in that context. We still
                    // need to take a closer look at the RCW because its interface pointer cache may contain
                    // pointers acquired in the given context - and those need to be released here.
                    if (pWrap->m_pAuxiliaryData != NULL)
                    {
                        RCWAuxiliaryData::InterfaceEntryIterator it = pWrap->m_pAuxiliaryData->IterateInterfacePointers();
                        while (it.Next())
                        {
                            InterfaceEntry *pEntry = it.GetEntry();
                            if (!pEntry->IsFree() && it.GetCtxCookie() == pCtxCookie)
                            {
                                RCWInterfacePointer intfPtr;
                                intfPtr.m_pUnk = pEntry->m_pUnknown;
                                intfPtr.m_pRCW = pWrap;
                                intfPtr.m_pCtxEntry = it.GetCtxEntryNoAddRef();
                                
                                if (!pWrap->IsURTAggregated())
                                    InterfacePointerList.Push(intfPtr);
                                else
                                    AggregatedInterfacePointerList.Push(intfPtr);

                                // Reset the CtxEntry first, so we don't race with RCWAuxiliaryData::CacheInterfacePointer
                                // which may try to reuse the InterfaceEntry for another (pUnk, MT, CtxEntry) triplet.
                                it.ResetCtxEntry();
                                pEntry->Free();
                            }
                        }
                    }
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

                if (pRCW->IsJupiterObject())
                    RCWWalker::BeforeJupiterRCWDestroyed(pRCW);
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
        Thread *pThread = GetThread();
        if (pThread && !FinalizerThread::IsCurrentThreadFinalizer())
            pThread->SetThreadStateNC(Thread::TSNC_UnsafeSkipEnterCooperative);
    }


    // Make sure we're in the right context / apartment.
    // Also - if we've already transitioned once, we don't want to do so again.
    //  If the cookie exists in multiple MTA apartments, and the STA has gone away 
    //  (leaving the old STA thread as unknown state with a context value equal to 
    //  the MTA context), we will infinitely loop.  So, we short circuit this with ctxTried.

    Thread *pHeadThread = pHead->GetSTAThread();
    BOOL fCorrectThread = (pHeadThread == NULL) ? TRUE : (pHeadThread == GetThread());
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
        Thread *pThread = GetThread();
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

// Destroys RCWAuxiliaryData. Note that we do not release interface pointers stored in the
// auxiliary interface pointer cache here. That needs to be done in the right COM context
// (see code:RCW::ReleaseAuxInterfacesCallBack).
RCWAuxiliaryData::~RCWAuxiliaryData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_prVariantInterfaces != NULL)
    {
        delete m_prVariantInterfaces;
    }

    InterfaceEntryEx *pEntry = m_pInterfaceCache;
    while (pEntry)
    {
        InterfaceEntryEx *pNextEntry = pEntry->m_pNext;

        delete pEntry;
        pEntry = pNextEntry;
    }

    if (VARIANCE_STUB_TARGET_IS_HANDLE(m_ohObjectVariantCallTarget_IEnumerable))
    {
        DestroyHandle(m_ohObjectVariantCallTarget_IEnumerable);
    }
    if (VARIANCE_STUB_TARGET_IS_HANDLE(m_ohObjectVariantCallTarget_IReadOnlyList))
    {
        DestroyHandle(m_ohObjectVariantCallTarget_IReadOnlyList);
    }
}

// Inserts variant interfaces to the cache.
void RCWAuxiliaryData::CacheVariantInterface(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder ch(&m_VarianceCacheCrst);

    if (m_prVariantInterfaces == NULL)
    {
        m_prVariantInterfaces = new ArrayList();
    }

    if (pMT->HasVariance() && m_prVariantInterfaces->FindElement(0, pMT) == ArrayList::NOT_FOUND)
    {
        m_prVariantInterfaces->Append(pMT);
    }

    // check implemented interfaces as well
    MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
    while (it.Next())
    {
        MethodTable *pItfMT = it.GetInterface();
        if (pItfMT->HasVariance() && m_prVariantInterfaces->FindElement(0, pItfMT) == ArrayList::NOT_FOUND)
        {
            m_prVariantInterfaces->Append(pItfMT);
        }
    }
}

// Inserts an interface pointer in the cache.
void RCWAuxiliaryData::CacheInterfacePointer(MethodTable *pMT, IUnknown *pUnk, LPVOID pCtxCookie)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    InterfaceEntryEx *pEntry = NULL;
    
    // first, try to find a free entry to reuse
    InterfaceEntryIterator it = IterateInterfacePointers();    
    while (it.Next())
    {
        InterfaceEntry *pEntry = it.GetEntry();
        if (pEntry->IsFree() && pEntry->Init(pMT, pUnk))
        {
            // setting the cookie after "publishing" the entry is fine, at worst
            // we may miss the cache if someone looks for this pMT concurrently
            _ASSERTE_MSG(it.GetCtxCookie() == NULL, "Race condition detected, we are supposed to own the InterfaceEntry at this point");
            it.SetCtxCookie(pCtxCookie);
            return;
        }
    }

    // create a new entry if a free one was not found
    InterfaceEntryEx *pEntryEx = new InterfaceEntryEx();
    ZeroMemory(pEntryEx, sizeof(InterfaceEntryEx));

    pEntryEx->m_BaseEntry.Init(pMT, pUnk);
    
    if (pCtxCookie != NULL)
    {
        pEntryEx->m_pCtxEntry = CtxEntryCache::GetCtxEntryCache()->FindCtxEntry(pCtxCookie, GetThread());
    }
    else
    {
        pEntryEx->m_pCtxEntry = NULL;
    }

    // and insert it into the linked list (the interlocked operation ensures that
    // the list is walkable by other threads at all times)
    InterfaceEntryEx *pNext;
    do
    {
        pNext = VolatileLoad(&m_pInterfaceCache); // our candidate "next"
        pEntryEx->m_pNext = pNext;
    }
    while (FastInterlockCompareExchangePointer(&m_pInterfaceCache, pEntryEx, pNext) != pNext);
}

// Returns a cached interface pointer or NULL if there was no match.
IUnknown *RCWAuxiliaryData::FindInterfacePointer(MethodTable *pMT, LPVOID pCtxCookie)
{
    LIMITED_METHOD_CONTRACT;

    InterfaceEntryIterator it = IterateInterfacePointers();
    while (it.Next())
    {
        InterfaceEntry *pEntry = it.GetEntry();
        if (!pEntry->IsFree() && pEntry->m_pMT == (IE_METHODTABLE_PTR)pMT && it.GetCtxCookie() == pCtxCookie)
        {
            return pEntry->m_pUnknown;
        }
    }

    return NULL;
}

const int RCW::s_rGCPressureTable[GCPressureSize_COUNT] = 
{
    0,                           // GCPressureSize_None
    GC_PRESSURE_PROCESS_LOCAL,   // GCPressureSize_ProcessLocal
    GC_PRESSURE_MACHINE_LOCAL,   // GCPressureSize_MachineLocal
    GC_PRESSURE_REMOTE,          // GCPressureSize_Remote
    GC_PRESSURE_WINRT_BASE,      // GCPressureSize_WinRT_Base
    GC_PRESSURE_WINRT_LOW,       // GCPressureSize_WinRT_Low
    GC_PRESSURE_WINRT_MEDIUM,    // GCPressureSize_WinRT_Medium
    GC_PRESSURE_WINRT_HIGH,      // GCPressureSize_WinRT_High
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
// The IUnknown passed in is AddRef'ed if we succeed in creating the wrapper unless
// the CF_SuppressAddRef flag is set.
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

    // No exception after this point    
    if (pRCW->IsJupiterObject())
        RCWWalker::AfterJupiterRCWCreated(pRCW);
    
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
    if((flags & CF_QueryForIdentity) ||
       (pAppDomain && pAppDomain->GetDisableInterfaceCache()))
    {
        IUnknown *pUnkTemp = NULL;
        HRESULT hr = SafeQueryInterfacePreemp(pUnk, IID_IUnknown, &pUnkTemp);
        LogInteropQI(pUnk, IID_IUnknown, hr, "QI for IID_IUnknown in RCW::CreateRCW");
        if(SUCCEEDED(hr))
        {
            pUnk = pUnkTemp;

        }
    }
    else
    {
        ULONG cbRef = SafeAddRefPreemp(pUnk);
        LogInteropAddRef(pUnk, cbRef, "RCWCache::CreateRCW: Addref pUnk because creating new RCW");
    }
    
    // Make sure we release AddRef-ed pUnk in case of exceptions
    SafeComHolderPreemp<IUnknown> pUnkHolder = pUnk;
    
    // Log the creation
    LogRCWCreate(pWrap, pUnk);
    
    // Remember that the object is known to support IInspectable
    pWrap->m_Flags.m_fSupportsIInspectable = !!(flags & CF_SupportsIInspectable);

    // Initialize wrapper
    pWrap->Initialize(pUnk, dwSyncBlockIndex, pClassMT);

    if (flags & CF_SupportsIInspectable)
    {
        // WinRT objects always apply some GC pressure
        GCPressureSize pressureSize = GCPressureSize_WinRT_Base;

        // if we have a strongly-typed non-delegate RCW, we may have read the GC pressure amount from metadata
        if (pClassMT->IsProjectedFromWinRT() && !pClassMT->IsDelegate())
        {
            WinRTClassFactory *pFactory = GetComClassFactory(pClassMT)->AsWinRTClassFactory();
            pressureSize = pFactory->GetGCPressure();
        }

        pWrap->AddMemoryPressure(pressureSize);
    }

    // Check to see if this is a DCOM proxy if either we've been explicitly asked to, or if
    // we're talking to a non-WinRT object and we need to add memory pressure
    const bool checkForDCOMProxy =  (flags & CF_DetectDCOMProxy) ||
                                   !(flags & CF_SupportsIInspectable);

    if (checkForDCOMProxy)
    {
        // If the object is a DCOM proxy...       
        SafeComHolderPreemp<IRpcOptions> pRpcOptions = NULL;
        GCPressureSize pressureSize = GCPressureSize_None;
        HRESULT hr = pWrap->SafeQueryInterfaceRemoteAware(IID_IRpcOptions, (IUnknown**)&pRpcOptions);
        LogInteropQI(pUnk, IID_IRpcOptions, hr, "QI for IRpcOptions");
        if (S_OK == hr)
        {
            ULONG_PTR dwValue = 0;
            hr = pRpcOptions->Query(pUnk, COMBND_SERVER_LOCALITY, &dwValue);

            if (SUCCEEDED(hr))
            {
                if (dwValue == SERVER_LOCALITY_MACHINE_LOCAL || dwValue == SERVER_LOCALITY_REMOTE)
                {
                    pWrap->m_Flags.m_fIsDCOMProxy = 1;
                }

                // Only add memory pressure for proxies for non-WinRT objects
                if (!(flags & CF_SupportsIInspectable))
                {
                    switch(dwValue)
                    {
                        case SERVER_LOCALITY_PROCESS_LOCAL:
                            pressureSize = GCPressureSize_ProcessLocal;
                            break;
                        case SERVER_LOCALITY_MACHINE_LOCAL:
                            pressureSize = GCPressureSize_MachineLocal;
                            break;
                        case SERVER_LOCALITY_REMOTE:
                            pressureSize = GCPressureSize_Remote;
                            break;
                        default:
                            pressureSize = GCPressureSize_None;
                            break;
                    }
                }
            }
        }

        // ...add the appropriate amount of memory pressure to the GC.
        if (pressureSize != GCPressureSize_None)
        {
            pWrap->AddMemoryPressure(pressureSize);
        }
    }

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
    _ASSERTE(m_pCreatorThread != NULL);

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

    // Check if this object is a Jupiter object (only for WinRT scenarios)
    _ASSERTE(m_Flags.m_fIsJupiterObject == 0);
    if (SupportsIInspectable())
    {
        SafeComHolderPreemp<IJupiterObject> pJupiterObject = NULL;
        HRESULT hr = SafeQueryInterfacePreemp(pUnk, IID_IJupiterObject, (IUnknown **)&pJupiterObject);
        LogInteropQI(pUnk, IID_IJupiterObject, hr, "QI for IJupiterObject");
        
        if (SUCCEEDED(hr))
        {   
            // A Jupiter object that is not free threaded is not allowed
            if (!IsFreeThreaded())
            {
                StackSString ssObjClsName;
                StackSString ssDestClsName;
                
                pClassMT->_GetFullyQualifiedNameForClass(ssObjClsName);
                                
                COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST,
                             ssObjClsName.GetUnicode(), W("IAgileObject"));
            }

            RCWWalker::OnJupiterRCWCreated(this, pJupiterObject);

            SetJupiterObject(pJupiterObject);
                
            if (!IsURTAggregated())
            {
                pJupiterObject.SuppressRelease();
            }
        }
    }
    
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

    if (!m_Flags.m_fURTAggregated && m_Flags.m_fIsJupiterObject)
    {
        // Notify Jupiter that we are about to release IJupiterObject
        RCWWalker::BeforeInterfaceRelease(this);

        // If we mark this RCW as aggregated and we've done a QI for IJupiterObject,
        // release it to account for the extra ref
        // Note that this is a quick fix for PDC-2 and eventually we should replace
        // this with a better fix
        SafeRelease(GetJupiterObject());
    }
    
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
    
    if (pressureSize >= GCPressureSize_WinRT_Base)
    {
        // use the new implementation for WinRT RCWs
        GCInterface::NewAddMemoryPressure(pressure);
    }
    else
    {
        // use the old implementation for classic COM interop
        GCInterface::AddMemoryPressure(pressure);
    }

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

    if (m_Flags.m_GCPressure >= GCPressureSize_WinRT_Base)
    {
        // use the new implementation for WinRT RCWs
        GCInterface::NewRemoveMemoryPressure(pressure);
    }
    else
    {
        // use the old implementation for classic COM interop
        GCInterface::RemoveMemoryPressure(pressure);
    }

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

    if (IsJupiterObject() && !IsDetached())
        RCWWalker::BeforeJupiterRCWDestroyed(this);
    
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

    if (m_pAuxiliaryData != NULL)
    {
        delete m_pAuxiliaryData;
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
        if (SupportsIInspectable())
            flags |= CF_SupportsIInspectable;

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
    if (AppX::IsAppXProcess())
    { 
        COMPlusThrow(kPlatformNotSupportedException, IDS_EE_ERROR_IDISPATCH);
    }

    WRAPPER_NO_CONTRACT;
    return (IDispatch *)GetWellKnownInterface(IID_IDispatch);
}

//-----------------------------------------------------------------
// Get the IInspectable pointer for the wrapper
IInspectable *RCW::GetIInspectable()
{
    WRAPPER_NO_CONTRACT;
    return (IInspectable *)GetWellKnownInterface(IID_IInspectable);
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
        if (IsJupiterObject() && !IsDetached())
            RCWWalker::BeforeJupiterRCWDestroyed(this);
            
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
        GetThread()->SetApartment(Thread::AS_InMTA, FALSE);
    
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

#endif //#ifndef CROSSGEN_COMPILE

//-----------------------------------------------------------------
// Returns a redirected collection interface corresponding to a given ICollection/ICollection<T> or NULL
// if the given interface is not ICollection/ICollection<T>. This also works for IReadOnlyCollection<T>.
// The BOOL parameters help resolve the ambiguity around ICollection<KeyValuePair<K, V>>.
// static
MethodTable *RCW::ResolveICollectionInterface(MethodTable *pItfMT, BOOL fPreferIDictionary, BOOL *pfChosenIDictionary)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItfMT));
        PRECONDITION(CheckPointer(pfChosenIDictionary, NULL_OK));
    }
    CONTRACTL_END;

    if (pfChosenIDictionary != NULL)
        *pfChosenIDictionary = FALSE;

    // Casting/calling via ICollection<T> means QI/calling through IVector<T>, casting/calling via ICollection<KeyValuePair<K, V>> means
    // QI/calling via IMap<K, V> OR IVector<IKeyValuePair<K, V>>. See which case it is.
    if (pItfMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__ICOLLECTIONGENERIC)))
    {
        Instantiation inst = pItfMT->GetInstantiation();
        TypeHandle arg = inst[0];

        if (fPreferIDictionary)
        {
            if (!arg.IsTypeDesc() && arg.GetMethodTable()->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__KEYVALUEPAIRGENERIC)))
            {
                // ICollection<KeyValuePair<K, V>> -> IDictionary<K, V>
                if (pfChosenIDictionary != NULL)
                    *pfChosenIDictionary = TRUE;

                pItfMT = GetAppDomain()->GetRedirectedType(WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IDictionary);
                return TypeHandle(pItfMT).Instantiate(arg.GetInstantiation()).GetMethodTable();
            }
        }

        // ICollection<T> -> IList<T>
        pItfMT = GetAppDomain()->GetRedirectedType(WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IList);
        return TypeHandle(pItfMT).Instantiate(inst).GetMethodTable();
    }

    // Casting/calling via IReadOnlyCollection<T> means QI/calling through IVectorView<T>, casting/calling via IReadOnlyCollection<KeyValuePair<K, V>> means
    // QI/calling via IMapView<K, V> OR IVectorView<IKeyValuePair<K, V>>. See which case it is.
    if (pItfMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IREADONLYCOLLECTIONGENERIC)))
    {
        Instantiation inst = pItfMT->GetInstantiation();
        TypeHandle arg = inst[0];

        if (fPreferIDictionary)
        {
            if (!arg.IsTypeDesc() && arg.GetMethodTable()->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__KEYVALUEPAIRGENERIC)))
            {
                // IReadOnlyCollection<KeyValuePair<K, V>> -> IReadOnlyDictionary<K, V>
                if (pfChosenIDictionary != NULL)
                    *pfChosenIDictionary = TRUE;

                pItfMT = GetAppDomain()->GetRedirectedType(WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IReadOnlyDictionary);
                return TypeHandle(pItfMT).Instantiate(arg.GetInstantiation()).GetMethodTable();
            }
        }

        // IReadOnlyCollection<T> -> IReadOnlyList<T>
        pItfMT = GetAppDomain()->GetRedirectedType(WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_IReadOnlyList);
        return TypeHandle(pItfMT).Instantiate(inst).GetMethodTable();
    }

    // Casting/calling via ICollection means QI/calling through IBindableVector (projected to IList).
    if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__ICOLLECTION))
    {
        return MscorlibBinder::GetExistingClass(CLASS__ILIST);
    }

    // none of the above
    return NULL;
}

// Helper method to allow us to compare a MethodTable against a known method table
// from mscorlib.  If the mscorlib type isn't loaded, we don't load it because we 
// know that it can't be the MethodTable we're curious about.
static bool MethodTableHasSameTypeDefAsMscorlibClass(MethodTable* pMT, BinderClassID classId)
{
    CONTRACTL
    {
        GC_NOTRIGGER; 
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodTable* pMT_MscorlibClass = MscorlibBinder::GetClassIfExist(classId);
    if (pMT_MscorlibClass == NULL)
        return false;
    
    return (pMT->HasSameTypeDefAs(pMT_MscorlibClass) != FALSE);
}

// Returns an interface with variance corresponding to pMT or NULL if pMT does not support variance.
// The reason why we don't just call HasVariance() is that we also deal with the WinRT interfaces
// like IIterable<T> which do not (and cannot) have variance from .NET type system point of view.
// static
MethodTable *RCW::GetVariantMethodTable(MethodTable *pMT)
{
    CONTRACT(MethodTable *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(RETVAL == NULL || RETVAL->HasVariance());
    }
    CONTRACT_END;

    RCWPerTypeData *pData = pMT->GetRCWPerTypeData();
    if (pData == NULL)
    {
        // if this type has no RCW data allocated, we know for sure that pMT has no
        // corresponding MethodTable with variance
        _ASSERTE(ComputeVariantMethodTable(pMT) == NULL);
        RETURN NULL;
    }

    if ((pData->m_dwFlags & RCWPerTypeData::VariantTypeInited) == 0)
    {
        pData->m_pVariantMT = ComputeVariantMethodTable(pMT);
        FastInterlockOr(&pData->m_dwFlags, RCWPerTypeData::VariantTypeInited);
    }
    RETURN pData->m_pVariantMT;
}

// static
MethodTable *RCW::ComputeVariantMethodTable(MethodTable *pMT)
{
    CONTRACT(MethodTable *)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(RETVAL == NULL || RETVAL->HasVariance());
    }
    CONTRACT_END;

    if (!pMT->IsProjectedFromWinRT() && !WinRTTypeNameConverter::ResolveRedirectedType(pMT, NULL))
    {
        RETURN NULL;
    }

    if (pMT->HasVariance())
    {
        RETURN pMT;
    }

    // IIterable and IVectorView are not marked as covariant. Check them explicitly and
    // return the corresponding IEnumerable / IReadOnlyList instantiation.
    if (MethodTableHasSameTypeDefAsMscorlibClass(pMT, CLASS__IITERABLE))
    {
        RETURN TypeHandle(MscorlibBinder::GetExistingClass(CLASS__IENUMERABLEGENERIC)).
               Instantiate(pMT->GetInstantiation()).AsMethodTable();
    }
    if (MethodTableHasSameTypeDefAsMscorlibClass(pMT, CLASS__IVECTORVIEW))
    {
        RETURN TypeHandle(MscorlibBinder::GetExistingClass(CLASS__IREADONLYLISTGENERIC)).
               Instantiate(pMT->GetInstantiation()).AsMethodTable();
    }

    // IIterator is not marked as covariant either. Return the covariant IEnumerator.
    DefineFullyQualifiedNameForClassW();
    if (MethodTableHasSameTypeDefAsMscorlibClass(pMT, CLASS__IITERATOR) ||
        wcscmp(GetFullyQualifiedNameForClassW_WinRT(pMT), g_WinRTIIteratorClassNameW) == 0)
    {
        RETURN TypeHandle(MscorlibBinder::GetClass(CLASS__IENUMERATORGENERIC)).
               Instantiate(pMT->GetInstantiation()).AsMethodTable();
    }

    RETURN NULL;
}

#ifndef CROSSGEN_COMPILE
//-----------------------------------------------------------------
// Determines the interface that should be QI'ed for when the RCW is cast to pItfMT.
RCW::InterfaceRedirectionKind RCW::GetInterfaceForQI(MethodTable *pItfMT, MethodTable **pNewItfMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItfMT));
        PRECONDITION(CheckPointer(pNewItfMT));
    }
    CONTRACTL_END;

    // We don't want to be redirecting interfaces if the underlying COM object is not a WinRT type
    if (SupportsIInspectable() || pItfMT->IsWinRTRedirectedDelegate())
    {
        MethodTable *pNewItfMT1;
        MethodTable *pNewItfMT2;
        InterfaceRedirectionKind redirectionKind = GetInterfacesForQI(pItfMT, &pNewItfMT1, &pNewItfMT2);

        //
        // IEnumerable may need three QI attempts:
        // 1. IEnumerable/IDispatch+DISPID_NEWENUM
        // 2. IBindableIterable
        // 3. IIterable<T> for a T
        //
        // Is this 3rd attempt on IEnumerable (non-generic)?
        if (redirectionKind == InterfaceRedirection_Other_RetryOnFailure &&
            pItfMT != *pNewItfMT && *pNewItfMT != NULL &&
            pItfMT == MscorlibBinder::GetExistingClass(CLASS__IENUMERABLE))
        {
            // Yes - we are at 3rd attempt; 
            // QI for IEnumerable/IDispatch+DISPID_NEWENUM and for IBindableIterable failed 
            // and we are about to see if we know of an IIterable<T> to use.

            MethodDesc *pMD = GetGetEnumeratorMethod();
            if (pMD != NULL)
            {
                // we have already determined what casting to IEnumerable means for this RCW
                TypeHandle th = TypeHandle(MscorlibBinder::GetClass(CLASS__IITERABLE));
                *pNewItfMT = th.Instantiate(pMD->GetClassInstantiation()).GetMethodTable();
                return InterfaceRedirection_IEnumerable;
            }

            // The last attempt failed, this is an error.
            return InterfaceRedirection_UnresolvedIEnumerable;
        }

        if ((redirectionKind != InterfaceRedirection_IEnumerable_RetryOnFailure &&
             redirectionKind != InterfaceRedirection_Other_RetryOnFailure) || *pNewItfMT == NULL)
        {
            // First attempt - use pNewItfMT1
            *pNewItfMT = pNewItfMT1;
            return redirectionKind;
        }
        else
        {
            // Second attempt - use pNewItfMT2
            *pNewItfMT = pNewItfMT2;

            if (redirectionKind == InterfaceRedirection_IEnumerable_RetryOnFailure)
                return InterfaceRedirection_IEnumerable;

            // Get ready for the 3rd attmpt if 2nd attempt fails
            // This only happens for non-generic IEnumerable
            if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__IENUMERABLE))
                return InterfaceRedirection_IEnumerable_RetryOnFailure;

            return InterfaceRedirection_IEnumerable;
        }
    }

    *pNewItfMT = pItfMT;
    return InterfaceRedirection_None;
}
#endif // !CROSSGEN_COMPILE

// static
RCW::InterfaceRedirectionKind RCW::GetInterfacesForQI(MethodTable *pItfMT, MethodTable **ppNewItfMT1, MethodTable **ppNewItfMT2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItfMT));
        PRECONDITION(CheckPointer(ppNewItfMT1));
        PRECONDITION(CheckPointer(ppNewItfMT2));
    }
    CONTRACTL_END;

    RCWPerTypeData *pData = pItfMT->GetRCWPerTypeData();
    if (pData == NULL)
    {
#ifdef _DEBUG
        // verify that if the per-type data is NULL, the type has indeed no redirection
        MethodTable *pNewItfMT1;
        MethodTable *pNewItfMT2;
        _ASSERTE(ComputeInterfacesForQI(pItfMT, &pNewItfMT1, &pNewItfMT2) == InterfaceRedirection_None);
#endif // _DEBUG

        *ppNewItfMT1 = pItfMT;
        *ppNewItfMT2 = NULL;
        return InterfaceRedirection_None;
    }
    else
    {
        if ((pData->m_dwFlags & RCWPerTypeData::RedirectionInfoInited) == 0)
        {
            pData->m_RedirectionKind = ComputeInterfacesForQI(pItfMT, &pData->m_pMTForQI1, &pData->m_pMTForQI2);
            FastInterlockOr(&pData->m_dwFlags, RCWPerTypeData::RedirectionInfoInited);
        }

        *ppNewItfMT1 = pData->m_pMTForQI1;
        *ppNewItfMT2 = pData->m_pMTForQI2;
        return pData->m_RedirectionKind;
    }
}

// static
RCW::InterfaceRedirectionKind RCW::ComputeInterfacesForQI(MethodTable *pItfMT, MethodTable **ppNewItfMT1, MethodTable **ppNewItfMT2)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pItfMT));
        PRECONDITION(CheckPointer(ppNewItfMT1));
        PRECONDITION(CheckPointer(ppNewItfMT2));
    }
    CONTRACTL_END;

    if (pItfMT->IsProjectedFromWinRT())
    {
        // If we're casting to IIterable<T> directly, then while we do want to QI IIterable<T>, also
        // make a note that it is redirected from IEnumerable<T>
        if (pItfMT->HasInstantiation() && pItfMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IITERABLE)))
        {
            *ppNewItfMT1 = pItfMT;
            return InterfaceRedirection_IEnumerable;
        }
    }
    else
    {
        WinMDAdapter::RedirectedTypeIndex redirectedInterfaceIndex;            
        RCW::InterfaceRedirectionKind redirectionKind = InterfaceRedirection_None;

        BOOL fChosenIDictionary;
        MethodTable *pResolvedItfMT = ResolveICollectionInterface(pItfMT, TRUE, &fChosenIDictionary);
        if (pResolvedItfMT == NULL)
        {
            pResolvedItfMT = pItfMT;
            // Let ResolveRedirectedType convert IDictionary/IList to the corresponding WinRT type as usual
        }

        if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pResolvedItfMT, &redirectedInterfaceIndex))
        {
            TypeHandle th = WinRTInterfaceRedirector::GetWinRTTypeForRedirectedInterfaceIndex(redirectedInterfaceIndex);

            if (th.HasInstantiation())
            {
                *ppNewItfMT1 = th.Instantiate(pResolvedItfMT->GetInstantiation()).GetMethodTable();
                if (pItfMT->CanCastToInterface(MscorlibBinder::GetClass(CLASS__IENUMERABLE)))
                {
                    redirectionKind = InterfaceRedirection_IEnumerable;
                }
                else
                {
                    _ASSERTE(!fChosenIDictionary);
                    redirectionKind = InterfaceRedirection_Other;
                }
            }
            else
            {
                // pItfMT is a non-generic redirected interface - for compat reasons do QI for the interface first,
                // and if it fails, use redirection
                *ppNewItfMT1 = pItfMT;
                *ppNewItfMT2 = th.GetMethodTable();
                redirectionKind = InterfaceRedirection_Other_RetryOnFailure;
            }
        }

        if (fChosenIDictionary)
        {
            // pItfMT is the ambiguous ICollection<KeyValuePair<K, V>> and *ppNewItfMT1 at this point is the
            // corresponding IMap<K, V>, now we are going to assign IVector<IKeyValuePair<K, V>> to *ppNewItfMT2
            pResolvedItfMT = ResolveICollectionInterface(pItfMT, FALSE, NULL);

            VERIFY(WinRTInterfaceRedirector::ResolveRedirectedInterface(pResolvedItfMT, &redirectedInterfaceIndex));
            TypeHandle th = WinRTInterfaceRedirector::GetWinRTTypeForRedirectedInterfaceIndex(redirectedInterfaceIndex);

            *ppNewItfMT2 = th.Instantiate(pItfMT->GetInstantiation()).GetMethodTable();
            redirectionKind = InterfaceRedirection_IEnumerable_RetryOnFailure;
        }

        if (redirectionKind != InterfaceRedirection_None)
        {
            return redirectionKind;
        }

        if (WinRTDelegateRedirector::ResolveRedirectedDelegate(pItfMT, &redirectedInterfaceIndex))
        {
            TypeHandle th = TypeHandle(WinRTDelegateRedirector::GetWinRTTypeForRedirectedDelegateIndex(redirectedInterfaceIndex));
            
            if (pItfMT->HasInstantiation())
            {
                th = th.Instantiate(pItfMT->GetInstantiation());
            }

            *ppNewItfMT1 = th.GetMethodTable();
            return InterfaceRedirection_Other;
        }
    }

    *ppNewItfMT1 = pItfMT;
    return InterfaceRedirection_None;
}

#ifndef CROSSGEN_COMPILE
//-----------------------------------------------------------------
// Returns a known working IEnumerable<T>::GetEnumerator to be used in lieu of the non-generic
// IEnumerable::GetEnumerator.
MethodDesc *RCW::GetGetEnumeratorMethod()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pAuxiliaryData == NULL || m_pAuxiliaryData->m_pGetEnumeratorMethod == NULL)
    {
        MethodTable *pClsMT;
        {
            GCX_COOP();
            pClsMT = GetExposedObject()->GetMethodTable();
        }

        SetGetEnumeratorMethod(pClsMT);
    }

    return (m_pAuxiliaryData == NULL ? NULL : m_pAuxiliaryData->m_pGetEnumeratorMethod);
}

//-----------------------------------------------------------------
// Sets the first "known" GetEnumerator method on the RCW if not set already.
void RCW::SetGetEnumeratorMethod(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pAuxiliaryData != NULL && m_pAuxiliaryData->m_pGetEnumeratorMethod != NULL)
        return;

    // Retrieve cached GetEnumerator method or compute the right one for this pMT
    MethodDesc *pMD = GetOrComputeGetEnumeratorMethodForType(pMT);

    if (pMD != NULL)
    {
        // We successfully got a GetEnumerator method - cache it in the RCW
        // We can have multiple casts going on concurrently, make sure that
        // the result of this method is stable.
        InterlockedCompareExchangeT(&GetOrCreateAuxiliaryData()->m_pGetEnumeratorMethod, pMD, NULL);
    }    
}

// Retrieve cached GetEnumerator method or compute the right one for a specific type
MethodDesc *RCW::GetOrComputeGetEnumeratorMethodForType(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc *pMD = NULL;
    
    RCWPerTypeData *pData = pMT->GetRCWPerTypeData();
    if (pData != NULL)
    {
        if ((pData->m_dwFlags & RCWPerTypeData::GetEnumeratorInited) == 0)
        {
            pData->m_pGetEnumeratorMethod = ComputeGetEnumeratorMethodForType(pMT);
            FastInterlockOr(&pData->m_dwFlags, RCWPerTypeData::GetEnumeratorInited);
        }

        pMD = pData->m_pGetEnumeratorMethod;
    }
    else
    {
        pMD = ComputeGetEnumeratorMethodForType(pMT);
    }

    return pMD;
}

// Compute the first GetEnumerator for a specific type
MethodDesc *RCW::ComputeGetEnumeratorMethodForType(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc *pMD = ComputeGetEnumeratorMethodForTypeInternal(pMT);
    
    // Walk the interface impl and use these interfaces to compute
    MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
    while (pMD == NULL && it.Next())
    {
        pMT = it.GetInterface();
        pMD = GetOrComputeGetEnumeratorMethodForType(pMT);
    }

    return pMD;
}

// Get the GetEnumerator method for IEnumerable<T> or IIterable<T>
MethodDesc *RCW::ComputeGetEnumeratorMethodForTypeInternal(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IENUMERABLEGENERIC)))
    {
        // If we have an IIterable<T>, we want to get the enumerator for the equivalent
        // instantiation of IEnumerable<T>
        if (pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IITERABLE)))
        {
            TypeHandle thEnumerable = TypeHandle(MscorlibBinder::GetExistingClass(CLASS__IENUMERABLEGENERIC));
            pMT = thEnumerable.Instantiate(pMT->GetInstantiation()).GetMethodTable();
        }
        else
        {
            return NULL;
        }
    }

    MethodDesc *pMD = pMT->GetMethodDescForSlot(0);
    _ASSERTE(strcmp(pMD->GetName(), "GetEnumerator") == 0);

    if (pMD->IsSharedByGenericInstantiations())
    {
        pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
            pMD,
            pMT,
            FALSE,           // forceBoxedEntryPoint
            Instantiation(), // methodInst
            FALSE,           // allowInstParam
            TRUE);           // forceRemotableMethod
    }

    return pMD;
}


//-----------------------------------------------------------------
// Notifies the RCW of an interface that is known to be supported by the COM object.
// pItfMT is the type which the object directly supports, originalInst is the instantiation
// that we asked for. I.e. we know that the object supports pItfMT<originalInst> via
// variance because the QI for IID(pItfMT) succeeded.
void RCW::SetSupportedInterface(MethodTable *pItfMT, Instantiation originalInst)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL fIsEnumerable = (pItfMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IENUMERABLEGENERIC)) ||
                          pItfMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IITERABLE)));

    if (fIsEnumerable || pItfMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IREADONLYLISTGENERIC)) ||
                         pItfMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IVECTORVIEW)))
    {
        WinRTInterfaceRedirector::WinRTLegalStructureBaseType baseType;
        if (!originalInst.IsEmpty())
        {
            // use the original instantiation if available
            baseType = WinRTInterfaceRedirector::GetStructureBaseType(originalInst);
        }
        else
        {
            baseType = WinRTInterfaceRedirector::GetStructureBaseType(pItfMT->GetInstantiation());
        }

        switch (baseType)
        {
            case WinRTInterfaceRedirector::BaseType_Object:
            {
                OBJECTHANDLE *pohHandleField = fIsEnumerable ?
                    &GetOrCreateAuxiliaryData()->m_ohObjectVariantCallTarget_IEnumerable :
                    &GetOrCreateAuxiliaryData()->m_ohObjectVariantCallTarget_IReadOnlyList;

                if (*pohHandleField != NULL)
                {
                    // we've already established the behavior so we can skip the code below
                    break;
                }

                if (!originalInst.IsEmpty())
                {
                    MethodTable *pInstArgMT = pItfMT->GetInstantiation()[0].GetMethodTable();
                    
                    if (pInstArgMT == g_pStringClass)
                    {
                        // We are casting the RCW to IEnumerable<string> or IReadOnlyList<string> - we special-case this common case
                        // so we don't have to create the delegate.
                        FastInterlockCompareExchangePointer<OBJECTHANDLE>(pohHandleField, VARIANCE_STUB_TARGET_USE_STRING, NULL);
                    }
                    else if (pInstArgMT == g_pExceptionClass ||
                             pInstArgMT == MscorlibBinder::GetClass(CLASS__TYPE) ||
                             pInstArgMT->IsArray() ||
                             pInstArgMT->IsDelegate())
                    {
                        // We are casting the RCW to IEnumerable<T> or IReadOnlyList<T> where T is Type/Exception/an array/a delegate
                        // i.e. an unbounded set of types. We'll create a delegate pointing to the right stub and cache it on the RCW
                        // so we can handle the calls via GetEnumerator/Indexer_Get as fast as possible.

                        MethodDesc *pTargetMD = MscorlibBinder::GetMethod(fIsEnumerable ?
                            METHOD__ITERABLE_TO_ENUMERABLE_ADAPTER__GET_ENUMERATOR_STUB :
                            METHOD__IVECTORVIEW_TO_IREADONLYLIST_ADAPTER__INDEXER_GET);

                        pTargetMD = MethodDesc::FindOrCreateAssociatedMethodDesc(
                            pTargetMD,
                            pTargetMD->GetMethodTable(),
                            FALSE,                      // forceBoxedEntryPoint
                            pItfMT->GetInstantiation(), // methodInst
                            FALSE,                      // allowInstParam
                            TRUE);                      // forceRemotableMethod

                        MethodTable *pMT = MscorlibBinder::GetClass(fIsEnumerable ?
                            CLASS__GET_ENUMERATOR_DELEGATE :
                            CLASS__INDEXER_GET_DELEGATE);

                        pMT = TypeHandle(pMT).Instantiate(pItfMT->GetInstantiation()).AsMethodTable();

                        GCX_COOP();

                        DELEGATEREF pDelObj = NULL;
                        GCPROTECT_BEGIN(pDelObj);
                            
                        pDelObj = (DELEGATEREF)AllocateObject(pMT);
                        pDelObj->SetTarget(GetExposedObject());
                        pDelObj->SetMethodPtr(pTargetMD->GetMultiCallableAddrOfCode());

                        OBJECTHANDLEHolder oh = GetAppDomain()->CreateHandle(pDelObj);
                        if (FastInterlockCompareExchangePointer<OBJECTHANDLE>(pohHandleField, oh, NULL) == NULL)
                        {
                            oh.SuppressRelease();
                        }

                        GCPROTECT_END();
                    }
                }

                // the default is "use T", i.e. handle the call as normal
                if (*pohHandleField == NULL)
                {
                    FastInterlockCompareExchangePointer<OBJECTHANDLE>(pohHandleField, VARIANCE_STUB_TARGET_USE_T, NULL);
                }
                break;
            }

            case WinRTInterfaceRedirector::BaseType_IEnumerable:
            case WinRTInterfaceRedirector::BaseType_IEnumerableOfChar:
            {
                // The only WinRT-legal type that implements IEnumerable<IEnumerable> or IEnumerable<IEnumerable<char>> or
                // IReadOnlyList<IEnumerable> or IReadOnlyList<IEnumerable<char>> AND is not an IInspectable on the WinRT
                // side is string. We'll use a couple of flags here since the number of options is small.

                InterfaceVarianceBehavior varianceBehavior = (fIsEnumerable ? IEnumerableSupported : IReadOnlyListSupported);

                if (!originalInst.IsEmpty())
                {
                    MethodTable *pInstArgMT = pItfMT->GetInstantiation()[0].GetMethodTable();
                    if (pInstArgMT == g_pStringClass)
                    {
                        varianceBehavior = (InterfaceVarianceBehavior)
                            (varianceBehavior | (fIsEnumerable ? IEnumerableSupportedViaStringInstantiation : IReadOnlyListSupportedViaStringInstantiation));
                    }
                    
                    RCWAuxiliaryData::RCWAuxFlags newAuxFlags = { 0 };

                    if (baseType == WinRTInterfaceRedirector::BaseType_IEnumerable)
                    {
                        newAuxFlags.m_InterfaceVarianceBehavior_OfIEnumerable = varianceBehavior;
                    }
                    else
                    {
                        _ASSERTE(baseType == WinRTInterfaceRedirector::BaseType_IEnumerableOfChar);
                        newAuxFlags.m_InterfaceVarianceBehavior_OfIEnumerableOfChar = varianceBehavior;
                    }

                    RCWAuxiliaryData *pAuxData = GetOrCreateAuxiliaryData();
                    FastInterlockOr(&pAuxData->m_AuxFlags.m_dwFlags, newAuxFlags.m_dwFlags);
                }
            }
        }
    }
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
    MethodTable *pCastToMT = pMT;
    MethodTable *pCOMItfMT = NULL;
    InterfaceRedirectionKind redirection = InterfaceRedirection_None;

    if (!inst.IsEmpty())
    {
        pMT = TypeHandle(pMT).Instantiate(inst).GetMethodTable();
    }

    do
    {
        redirection = GetInterfaceForQI(pMT, &pCOMItfMT);

        if (redirection == InterfaceRedirection_UnresolvedIEnumerable)
        {
            // We just say no in this case. If we threw an exception, we would make the "as" operator
            // throwing which would be ECMA violation.
            return E_NOINTERFACE;
        }

        // To avoid throwing BadImageFormatException later in ComputeGuidForGenericTypes we must fail early if this is a generic type and not a legal WinRT type.
        if (pCOMItfMT->SupportsGenericInterop(TypeHandle::Interop_NativeToManaged, MethodTable::modeProjected) && !pCOMItfMT->IsLegalNonArrayWinRTType())
        {
            return E_NOINTERFACE;
        }
        else
        {
            // Retrieve the IID of the interface.
            pCOMItfMT->GetGuid(piid, TRUE);
        }

        // QI for the interface.
        hr = SafeQueryInterfaceRemoteAware(*piid, ppUnk);
    }
    while (hr == E_NOINTERFACE &&   // Terminate the loop if the QI failed for some other reasons (for example, context transition failure)
           (redirection == InterfaceRedirection_IEnumerable_RetryOnFailure || redirection == InterfaceRedirection_Other_RetryOnFailure));

    if (SUCCEEDED(hr))
    {
        if (redirection == InterfaceRedirection_IEnumerable)
        {
            // remember the first IEnumerable<T> interface we successfully QI'ed for
            SetGetEnumeratorMethod(pMT);
        }

        // remember successful QI's for interesting interfaces passing the original instantiation so we know that variance was involved
        SetSupportedInterface(pCOMItfMT, pCastToMT->GetInstantiation());
    }

    return hr;
}

// Performs QI for interfaces that are castable to pMT using co-/contra-variance.
HRESULT RCW::CallQueryInterfaceUsingVariance(MethodTable *pMT, IUnknown **ppUnk)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = E_NOINTERFACE;

    // see if pMT is an interface with variance, if not we return NULL
    MethodTable *pVariantMT = GetVariantMethodTable(pMT);
    
    if (pVariantMT != NULL)
    {
        MethodTable *pItfMT = NULL;
        IID variantIid;

        MethodTable *pClassMT;
        
        {
            GCX_COOP();
            pClassMT = GetExposedObject()->GetMethodTable();
        }
        
        // Try interfaces that we know about from metadata
        if (pClassMT != NULL && pClassMT != g_pBaseCOMObject)
        {
            MethodTable::InterfaceMapIterator it = pClassMT->IterateInterfaceMap();
            while (FAILED(hr) && it.Next())
            {
                pItfMT = GetVariantMethodTable(it.GetInterface());
                if (pItfMT != NULL && pItfMT->CanCastByVarianceToInterfaceOrDelegate(pVariantMT, NULL))
                {
                    hr = CallQueryInterface(pMT, pItfMT->GetInstantiation(), &variantIid, ppUnk);
                }
            }
        }

        // Then try the interface pointer cache
        CachedInterfaceEntryIterator it = IterateCachedInterfacePointers();
        while (FAILED(hr) && it.Next())
        {
            MethodTable *pCachedItfMT = (MethodTable *)it.GetEntry()->m_pMT;
            if (pCachedItfMT != NULL)
            {
                pItfMT = GetVariantMethodTable(pCachedItfMT);
                if (pItfMT != NULL && pItfMT->CanCastByVarianceToInterfaceOrDelegate(pVariantMT, NULL))
                {
                    hr = CallQueryInterface(pMT, pItfMT->GetInstantiation(), &variantIid, ppUnk);
                }

                // The cached interface may not support variance, but one of its base interfaces can
                if (FAILED(hr))
                {
                    MethodTable::InterfaceMapIterator it = pCachedItfMT->IterateInterfaceMap();
                    while (FAILED(hr) && it.Next())
                    {
                        pItfMT = GetVariantMethodTable(it.GetInterface());
                        if (pItfMT != NULL && pItfMT->CanCastByVarianceToInterfaceOrDelegate(pVariantMT, NULL))
                        {
                            hr = CallQueryInterface(pMT, pItfMT->GetInstantiation(), &variantIid, ppUnk);
                        }
                    }
                }
            }
        }

        // If we still haven't succeeded, enumerate the variant interface cache
        if (FAILED(hr) && m_pAuxiliaryData != NULL && m_pAuxiliaryData->m_prVariantInterfaces != NULL)
        {
            // make a copy of the cache under the lock
            ArrayList rVariantInterfacesCopy;
            {
                CrstHolder ch(&m_pAuxiliaryData->m_VarianceCacheCrst);
                
                ArrayList::Iterator it = m_pAuxiliaryData->m_prVariantInterfaces->Iterate();
                while (it.Next())
                {
                    rVariantInterfacesCopy.Append(it.GetElement());
                }
            }

            ArrayList::Iterator it = rVariantInterfacesCopy.Iterate();
            while (FAILED(hr) && it.Next())
            {
                pItfMT = (MethodTable *)it.GetElement();
                if (pItfMT->CanCastByVarianceToInterfaceOrDelegate(pVariantMT, NULL))
                {
                    hr = CallQueryInterface(pMT, pItfMT->GetInstantiation(), &variantIid, ppUnk);
                }
            }
        }
    }

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

    if (m_pAuxiliaryData != NULL)
    {
        pUnk = m_pAuxiliaryData->FindInterfacePointer(pMT, (IsFreeThreaded() ? NULL : pCtxCookie));
        if (pUnk != NULL)
        {
            cbRef = SafeAddRef(pUnk);
            LogInteropAddRef(pUnk, cbRef, "RCW::GetComIPForMethodTableFromCache: Addref because returning pUnk fetched from auxiliary interface pointer cache");
            RETURN pUnk;
        }
    }

    // We're going to be making some COM calls, better initialize COM.
    EnsureComStarted();

    // First, try to QI for the interface that we were asked for
    hr = CallQueryInterface(pMT, Instantiation(), &iid, &pUnk);

    // If that failed and the interface has variance, we'll try to find another instantiation
    bool fVarianceUsed = false;
    if (FAILED(hr))
    {
        hr = CallQueryInterfaceUsingVariance(pMT, &pUnk);
        if (pUnk != NULL)
        {
            fVarianceUsed = true;
        }
    }

    if (pUnk == NULL)
        RETURN NULL;

    // See if we should cache the result in the fast inline cache. This cache can only store interface pointers
    // returned from QI's in the same context where we created the RCW.
    bool fAllowCache = true;
    bool fAllowOutOfContextCache = true;

    if (!pMT->IsProjectedFromWinRT() && !pMT->IsWinRTRedirectedInterface(TypeHandle::Interop_ManagedToNative) && !pMT->IsWinRTRedirectedDelegate())
    {
        AppDomain *pAppDomain = GetAppDomain();
        if (pAppDomain && pAppDomain->GetDisableInterfaceCache())
        {
            // Caching is disabled in this AD
            fAllowCache = false;
        }
        else
        {
            // This is not a WinRT interface and we could in theory use the out-of-context auxiliary cache,
            // at worst we would just do
            // fAllowOutOfContextCache = !IsURTAggregated()
            // however such a change has some breaking potential (COM proxies would live much longer) and is
            // considered to risky for an in-place release.
            
            fAllowOutOfContextCache = false;
        }
    }

    // try to cache the interface pointer in the inline cache
    bool fInterfaceCached = false;
    if (fAllowCache)
    {
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
                        
                        // Notify Jupiter we have done a AddRef
                        // We should do this *after* we made a AddRef because we should never
                        // be in a state where report refs > actual refs
                        RCWWalker::AfterInterfaceAddRef(this);
                    }
                   
                    fInterfaceCached = true;
                    break;
                }
            }
        }

        if (!fInterfaceCached && fAllowOutOfContextCache)
        {
            // We couldn't insert into the inline cache, either because it didn't fit, or because
            // we are in a wrong COM context. We'll use the RCWAuxiliaryData structure.
            GetOrCreateAuxiliaryData()->CacheInterfacePointer(pMT, pUnk, (IsFreeThreaded() ? NULL : pCtxCookie));

            // If the component is not aggregated then we need to ref-count
            if (!IsURTAggregated())
            {
                // Get an extra addref to hold this reference alive in our cache
                cbRef = SafeAddRef(pUnk);
                LogInteropAddRef(pUnk, cbRef, "RCW::GetComIPForMethodTableFromCache: Addref because storing pUnk in the auxiliary interface pointer cache");

                // Notify Jupiter we have done a AddRef
                // We should do this *after* we made a AddRef because we should never
                // be in a state where report refs > actual refs
                RCWWalker::AfterInterfaceAddRef(this);
            }

            fInterfaceCached = true;
        }
    }

    // Make sure we cache successful QI's for variant interfaces. This is so we can cast an RCW for
    // example to IEnumerable<object> if we previously successfully QI'ed for IEnumerable<IFoo>. We
    // only need to do this if we actually didn't use variance for this QI.
    if (!fVarianceUsed)
    {
        MethodTable *pVariantMT = GetVariantMethodTable(pMT);

        // We can also skip the potentially expensive CacheVariantInterface call if we already inserted
        // the variant interface into our interface pointer cache.
        if (pVariantMT != NULL && (!fInterfaceCached || pVariantMT != pMT))
        {
            GetOrCreateAuxiliaryData()->CacheVariantInterface(pVariantMT);
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

    // If the client has called CoEEShutdownCOM, then we should always try to
    // clean up RCWs, even if they have previously opted out by calling
    // DisableComObjectEagerCleanup. There's no way for clients to re-enable
    // eager cleanup so, if we don't clean up now, they will be leaked. After
    // shutting down COM, clients would not expect any RCWs to be left over.
    if( g_fShutDownCOM )
    {
        return TRUE;
    }

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
// Callback called to release the interfaces in the auxiliary cache.
HRESULT __stdcall RCW::ReleaseAuxInterfacesCallBack(LPVOID pData)
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

    LPVOID pCurrentCtxCookie = GetCurrentCtxCookie();
    _ASSERTE(pCurrentCtxCookie != NULL);
    
    RCW_VTABLEPTR(pWrap);

    // we don't come here for free-threaded RCWs
    _ASSERTE(!pWrap->IsFreeThreaded());

    // we don't come here if there are no interfaces in the aux cache
    _ASSERTE(pWrap->m_pAuxiliaryData != NULL);

    RCWAuxiliaryData::InterfaceEntryIterator it = pWrap->m_pAuxiliaryData->IterateInterfacePointers();
    while (it.Next())
    {
        InterfaceEntry *pEntry = it.GetEntry();
        if (!pEntry->IsFree())
        {
            if (pCurrentCtxCookie == it.GetCtxCookie())
            {
                IUnknown *pUnk = it.GetEntry()->m_pUnknown;

                // make sure we never try to clean this up again
                pEntry->Free();
                SafeReleasePreemp(pUnk, pWrap);
            }
        }
    }

    RETURN S_OK;
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

    // Free auxiliary interface entries if this is not an extensible RCW
    if (!pWrap->IsURTAggregated() && pWrap->m_pAuxiliaryData != NULL)
    {
        RCWAuxiliaryData::InterfaceEntryIterator it = pWrap->m_pAuxiliaryData->IterateInterfacePointers();
        while (it.Next())
        {
            InterfaceEntry *pEntry = it.GetEntry();
            if (!pEntry->IsFree())
            {
                IUnknown *pUnk = it.GetEntry()->m_pUnknown;

                if (pCurrentCtxCookie == NULL || pCurrentCtxCookie == it.GetCtxCookie() || pWrap->IsFreeThreaded())
                {
                    // Notify Jupiter we are about to do a Release() for every cached interface pointer
                    // This needs to be made before call Release because we should never be in a 
                    // state that we report more than the actual ref                        
                    RCWWalker::BeforeInterfaceRelease(pWrap);
                    
                    // make sure we never try to clean this up again
                    pEntry->Free();
                    SafeReleasePreemp(pUnk, pWrap);
                }
                else
                {
                    _ASSERTE(!pWrap->IsJupiterObject());
                    
                    // Retrieve the addref'ed context entry that the wrapper lives in.
                    CtxEntryHolder pCtxEntry = it.GetCtxEntry();

                    // Transition into the context to release the interfaces.
                    HRESULT hr = pCtxEntry->EnterContext(ReleaseAuxInterfacesCallBack, pWrap);
                    if (FAILED(hr))
                    {
                        // The context is disconnected so we cannot transition into it to clean up.
                        // The only option we have left is to try and release the interfaces from
                        // the current context. This will work for context agile object's since we have
                        // a pointer to them directly. It will however fail for others since we only
                        // have a pointer to a proxy which is no longer attached to the object.

                        // make sure we never try to clean this up again
                        pEntry->Free();
                        SafeReleasePreemp(pUnk, pWrap);
                    }
                }
            }
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

    // Notify Jupiter we are about to do a Release() for IUnknown
    // This needs to be made before call Release because we should never be in a 
    // state that we report more than the actual ref                        
    RCWWalker::BeforeInterfaceRelease(this);                        
    
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
                // Notify Jupiter we are about to do a Release() for every cached interface pointer
                // This needs to be made before call Release because we should never be in a 
                // state that we report more than the actual ref                        
                RCWWalker::BeforeInterfaceRelease(this);                        
            
                DWORD cbRef = SafeReleasePreemp(m_aInterfaceEntries[i].m_pUnknown, this);
                LogInteropRelease(m_aInterfaceEntries[i].m_pUnknown, cbRef, "RCW::ReleaseAllInterfaces: Releasing ref from InterfaceEntry table");
            }
        }
    }
}

//---------------------------------------------------------------------
// Returns RCWAuxiliaryData associated with this RCW.
PTR_RCWAuxiliaryData RCW::GetOrCreateAuxiliaryData()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pAuxiliaryData == NULL)
    {
        NewHolder<RCWAuxiliaryData> pData = new RCWAuxiliaryData();
        if (InterlockedCompareExchangeT(&m_pAuxiliaryData, pData.GetValue(), NULL) == NULL)
        {
            pData.SuppressRelease();
        }
    }
    return m_pAuxiliaryData;
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
        if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__IENUMERABLE))
        {
            SafeComHolder<IDispatch> pDisp = NULL;
            if  (!AppX::IsAppXProcess())
            {
                 // Get the IDispatch on the current thread.
                 pDisp = GetIDispatch();
            }
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

//---------------------------------------------------------------------
// Determines whether a call through the given interface should use new
// WinRT interop (as opposed to classic COM).
TypeHandle::CastResult RCW::SupportsWinRTInteropInterfaceNoGC(MethodTable *pItfMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    WinMDAdapter::RedirectedTypeIndex index;

    // @TODO: Make this nicer?
    RedirectionBehavior redirectionBehavior;
    if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__IENUMERABLE))
        redirectionBehavior = (RedirectionBehavior)m_Flags.m_RedirectionBehavior_IEnumerable;
    else if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__ICOLLECTION))
        redirectionBehavior = (RedirectionBehavior)m_Flags.m_RedirectionBehavior_ICollection;
    else if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__ILIST))
        redirectionBehavior = (RedirectionBehavior)m_Flags.m_RedirectionBehavior_IList;
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_INotifyCollectionChanged)
        redirectionBehavior = (RedirectionBehavior)m_Flags.m_RedirectionBehavior_INotifyCollectionChanged;
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_INotifyPropertyChanged)
        redirectionBehavior = (RedirectionBehavior)m_Flags.m_RedirectionBehavior_INotifyPropertyChanged;
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_Windows_Input_ICommand)
        redirectionBehavior = (RedirectionBehavior)m_Flags.m_RedirectionBehavior_ICommand;
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_IDisposable)
        redirectionBehavior = (RedirectionBehavior)m_Flags.m_RedirectionBehavior_IDisposable;
    else
    {
        UNREACHABLE_MSG("Unknown redirected interface");
    }

    if ((redirectionBehavior & RedirectionBehaviorComputed) == 0)
    {
        // we don't know yet what the behavior should be
        return TypeHandle::MaybeCast;
    }

    return ((redirectionBehavior & RedirectionBehaviorEnabled) == 0 ?
        TypeHandle::CannotCast :
        TypeHandle::CanCast);
}

//---------------------------------------------------------------------
// This is a GC-triggering variant of code:SupportsWinRTInteropInterfaceNoGC.
bool RCW::SupportsWinRTInteropInterface(MethodTable *pItfMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    TypeHandle::CastResult result = SupportsWinRTInteropInterfaceNoGC(pItfMT);
    switch (result)
    {
        case TypeHandle::CanCast: return true;
        case TypeHandle::CannotCast: return false;
    }

    WinMDAdapter::RedirectedTypeIndex index;
    bool fLegacySupported;

    // @TODO: Make this nicer?
    RedirectionBehavior redirectionBehavior;
    RCWFlags newFlags = { 0 };

    if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__IENUMERABLE))
    {
        redirectionBehavior = ComputeRedirectionBehavior(pItfMT, &fLegacySupported);
        newFlags.m_RedirectionBehavior_IEnumerable = redirectionBehavior;
        newFlags.m_RedirectionBehavior_IEnumerable_LegacySupported = fLegacySupported;
    }
    else if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__ICOLLECTION))
    {
        redirectionBehavior = ComputeRedirectionBehavior(pItfMT, &fLegacySupported);
        newFlags.m_RedirectionBehavior_ICollection = redirectionBehavior;
    }
    else if (pItfMT == MscorlibBinder::GetExistingClass(CLASS__ILIST))
    {
        redirectionBehavior = ComputeRedirectionBehavior(pItfMT, &fLegacySupported);
        newFlags.m_RedirectionBehavior_IList = redirectionBehavior;
    }
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_INotifyCollectionChanged)
    {
        redirectionBehavior = ComputeRedirectionBehavior(pItfMT, &fLegacySupported);
        newFlags.m_RedirectionBehavior_INotifyCollectionChanged = redirectionBehavior;
    }
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_INotifyPropertyChanged)
    {
        redirectionBehavior = ComputeRedirectionBehavior(pItfMT, &fLegacySupported);
        newFlags.m_RedirectionBehavior_INotifyPropertyChanged = redirectionBehavior;
    }
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_Windows_Input_ICommand)
    {
        redirectionBehavior = ComputeRedirectionBehavior(pItfMT, &fLegacySupported);
        newFlags.m_RedirectionBehavior_ICommand = redirectionBehavior;
    }
    else if (WinRTInterfaceRedirector::ResolveRedirectedInterface(pItfMT, &index) && index == WinMDAdapter::RedirectedTypeIndex_System_IDisposable)
    {
        redirectionBehavior = ComputeRedirectionBehavior(pItfMT, &fLegacySupported);
        newFlags.m_RedirectionBehavior_IDisposable = redirectionBehavior;
    }
    else
    {
        UNREACHABLE_MSG("Unknown redirected interface");
    }

    // Use interlocked operation so we don't race with other threads trying to set some other flags on the RCW.
    // Note that since we are in cooperative mode, we don't race with RCWCache::DetachWrappersWorker here.
    FastInterlockOr(&m_Flags.m_dwFlags, newFlags.m_dwFlags);

    _ASSERTE((redirectionBehavior & RedirectionBehaviorComputed) != 0);
    return ((redirectionBehavior & RedirectionBehaviorEnabled) != 0);
}

//---------------------------------------------------------------------
// Computes the result of code:SupportsWinRTInteropInterface.
RCW::RedirectionBehavior RCW::ComputeRedirectionBehavior(MethodTable *pItfMT, bool *pfLegacySupported)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    *pfLegacySupported = false;

    // @TODO: It may be possible to take advantage of metadata (e.g. non-WinRT ComImport class says it implements ICollection -> use classic COM)
    // and/or the interface cache but for now we'll just QI.

    IID iid;
    pItfMT->GetGuid(&iid, TRUE, TRUE);

    SafeComHolder<IUnknown> pUnk;
    if (SUCCEEDED(SafeQueryInterfaceRemoteAware(iid, &pUnk)))
    {
        // if the object supports the legacy COM interface we don't use redirection
        *pfLegacySupported = true;
        return RedirectionBehaviorComputed;
    }

    if (SupportsMngStdInterface(pItfMT))
    {
        // if the object supports the corresponding "managed std" interface we don't use redirection
        *pfLegacySupported = true;
        return RedirectionBehaviorComputed;
    }

    COMOBJECTREF oref = GetExposedObject();
    if (ComObject::SupportsInterface(oref, pItfMT))
    {
        // the cast succeeded but we know that the legacy COM interface is not implemented
        // -> we know for sure that the object supports the WinRT redirected interface
        return (RedirectionBehavior)(RedirectionBehaviorComputed | RedirectionBehaviorEnabled);
    }

    // The object does not support anything which means that we are in a failure case and an
    // exception will be thrown. For back compat we want the exception message to include the
    // classic COM IID so we'll return the "no redirection" result.
    return RedirectionBehaviorComputed;
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
                    if (!pMT->IsWinRTObjectType())
                    {
                        // Check if the object supports all of these interfaces only if this is a classic COM interop
                        // scenario. This is a perf optimization (no need to QI for base interfaces if we don't really
                        // need them just yet) and also has a usability aspect. If this SupportsInterface call failed
                        // because one of the base interfaces is not supported, the exception we'd throw would contain
                        // only the name of the "top level" interface which would confuse the developer.
                        MethodTable::InterfaceMapIterator it = pIntfTable->IterateInterfaceMap();
                        while (it.Next())                        
                        {
                            bSupportsItf = Object::SupportsInterface(oref, it.GetInterface());
                            if (!bSupportsItf)
                                break;
                        }
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
        MethodTable *pCOMItfMT = NULL;
        if (pRCW->GetInterfaceForQI(thCastTo.GetMethodTable(), &pCOMItfMT) == RCW::InterfaceRedirection_UnresolvedIEnumerable)
        {
            // A special exception message for the case where we are unable to figure out the
            // redirected interface because we haven't seen a cast to a generic IEnumerable yet.
            COMPlusThrow(kInvalidCastException, IDS_EE_WINRT_IENUMERABLE_BAD_CAST);
        }

        if (pCOMItfMT->IsProjectedFromWinRT())
        {
            // pCOMItfMT could be a generic WinRT-illegal interface in which case GetGuid would throw a confusing BadImageFormatException
            // so we swallow the exception and throw the generic InvalidCastException instead
            if (FAILED(pCOMItfMT->GetGuidNoThrow(&iid, FALSE)))
            {
                COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST, strComObjClassName.GetUnicode(), strCastToName.GetUnicode());
            }
        }
        else
        {
            // keep calling the throwing GetGuid for non-WinRT interfaces (back compat)
            pCOMItfMT->GetGuid(&iid, TRUE);
        }

        // Query for the interface to determine the failure HRESULT.
        hr = pRCW->SafeQueryInterfaceRemoteAware(iid, (IUnknown**)&pItf);

        // If this function was called, it means the QI call failed in the past. If it 
        // no longer fails now, we still need to throw, so throw a generic invalid cast exception.
        if (SUCCEEDED(hr) || 
            // Also throw the generic exception if the QI failed with E_NOINTERFACE and this is
            // a WinRT scenario - the user is very likely not interested in details like IID and
            // HRESULT, they just want to get the "managed" experience.
            (hr == E_NOINTERFACE && (thClass.GetMethodTable()->IsWinRTObjectType() || pCOMItfMT->IsProjectedFromWinRT())))
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
        else if (thCastTo == TypeHandle(MscorlibBinder::GetClass(CLASS__IENUMERABLE)))
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

        if (thClass.GetMethodTable()->IsWinRTObjectType() || thCastTo.IsProjectedFromWinRT() || thCastTo.GetMethodTable()->IsWinRTObjectType())
        {
            // don't mention any "COM components" in the exception if we failed to cast a WinRT object or
            // to a WinRT object, throw the simple generic InvalidCastException instead
            COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST, strComObjClassName.GetUnicode(), strCastToName.GetUnicode());
        }
    
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

//
// Create override information based on interface lookup
//
WinRTOverrideInfo::WinRTOverrideInfo(EEClass *pClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pClass));
    }
    CONTRACTL_END;

    ::ZeroMemory(this, sizeof(WinRTOverrideInfo));

    MethodTable *pMT = pClass->GetMethodTable();

    _ASSERTE(IsTdClass(pClass->GetAttrClass()));
    //
    // Iterate through each implemented interface
    // Note that the interface map is laid from parent to child
    // So we start from the most derived class, and climb our way up to parent, instead of
    // inspecting interface map directly
    //
    while (pMT != g_pBaseCOMObject)
    {
        MethodTable *pParentMT = pMT->GetParentMethodTable();
        unsigned dwParentInterfaces = 0;
        if (pParentMT)
            dwParentInterfaces = pParentMT->GetNumInterfaces();

        DWORD dwFound = 0;
        
        //
        // Scanning only current class only if the current class have more interface than parent
        //
        if (pMT->GetNumInterfaces() > dwParentInterfaces)
        {
            MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMapFrom(dwParentInterfaces);
            while (!it.Finished())
            {
                MethodTable *pImplementedIntfMT = it.GetInterface();

                // Only check private interfaces as they are exclusive
                if (IsTdNotPublic(pImplementedIntfMT->GetAttrClass()) && pImplementedIntfMT->IsProjectedFromWinRT())
                {
                    if (m_pToStringMD == NULL)
                    {
                        m_pToStringMD = MemberLoader::FindMethod(
                            pImplementedIntfMT, 
                            "ToString",
                            &gsig_IM_RetStr);
                        if (m_pToStringMD != NULL)
                            dwFound++;                    
                    }

                    if (m_pGetHashCodeMD == NULL)
                    {
                        m_pGetHashCodeMD = MemberLoader::FindMethod(
                            pImplementedIntfMT, 
                            "GetHashCode",
                            &gsig_IM_RetInt);
                        if (m_pGetHashCodeMD != NULL)
                            dwFound++;                    
                    }

                    if (m_pEqualsMD == NULL)
                    {
                        m_pEqualsMD = MemberLoader::FindMethod(
                            pImplementedIntfMT, 
                            "Equals",
                            &gsig_IM_Obj_RetBool);                
                        if (m_pEqualsMD != NULL)
                            dwFound++;                    
                    }

                    if (dwFound == 3)
                        return;
                }
                
                it.Next();
            }
        }

        //
        // Parent has no more interfaces (including parents of parent). We are done
        //
        if (dwParentInterfaces == 0)
            break;
        
        pMT = pParentMT;
    }
}

//
// If WinRTOverrideInfo is not created, create one. Otherwise return existing one
//
WinRTOverrideInfo *WinRTOverrideInfo::GetOrCreateWinRTOverrideInfo(MethodTable *pMT)
{
    CONTRACT (WinRTOverrideInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        CAN_TAKE_LOCK;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));        
    }
    CONTRACT_END;

    _ASSERTE(pMT != NULL);

    EEClass *pClass = pMT->GetClass();

    //
    // Retrieve the WinRTOverrideInfo from WinRT class factory
    // It is kind of sub-optimal but saves a EEClass field
    //
    WinRTClassFactory *pClassFactory = GetComClassFactory(pMT)->AsWinRTClassFactory();
        
    WinRTOverrideInfo *pOverrideInfo = pClassFactory->GetWinRTOverrideInfo();
    if (pOverrideInfo == NULL)
    {
        //
        // Create the override information
        //            
        NewHolder<WinRTOverrideInfo> pNewOverrideInfo = new WinRTOverrideInfo(pClass);
        
        if (pNewOverrideInfo->m_pEqualsMD       == NULL &&
            pNewOverrideInfo->m_pGetHashCodeMD  == NULL &&
            pNewOverrideInfo->m_pToStringMD     == NULL)
        {
            // Special optimization for where there is no override found
            pMT->SetSkipWinRTOverride();
            
            RETURN NULL;
        }
        else
        {
            if (pClassFactory->SetWinRTOverrideInfo(pNewOverrideInfo))
            {
                // We win the race
                pNewOverrideInfo.SuppressRelease();
                RETURN pNewOverrideInfo;
            }
            else
            {
                // Lost the race - retrieve again
                RETURN pClassFactory->GetWinRTOverrideInfo();
            }
        }
    }

    RETURN pOverrideInfo;
}

//
// Redirection for ToString
//
NOINLINE static MethodDesc *GetRedirectedToStringMDHelper(Object *pThisUNSAFE, MethodTable *pMT)
{
    FC_INNER_PROLOG(ComObject::GetRedirectedToStringMD);

    MethodDesc *pRetMD = NULL;
    
    // Creates helper frame for GetOrCreateWinRTOverrideInfo (which throws)    
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    WinRTOverrideInfo *pOverrideInfo = WinRTOverrideInfo::GetOrCreateWinRTOverrideInfo(pMT);
    if (pOverrideInfo && pOverrideInfo->m_pToStringMD != NULL)
    {
        pRetMD = pOverrideInfo->m_pToStringMD;
    }    
    
    HELPER_METHOD_FRAME_END();
    
    FC_INNER_EPILOG();
    
    return pRetMD;
}

FCIMPL1(MethodDesc *, ComObject::GetRedirectedToStringMD, Object *pThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pThisUNSAFE));
    }
    CONTRACTL_END;

    MethodTable *pMT = pThisUNSAFE->GetMethodTable();
    if (pMT->IsSkipWinRTOverride())
        return NULL;

    FC_INNER_RETURN(MethodDesc*, ::GetRedirectedToStringMDHelper(pThisUNSAFE, pMT));    
}
FCIMPLEND

FCIMPL2(StringObject *, ComObject::RedirectToString, Object *pThisUNSAFE, MethodDesc *pToStringMD)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF refThis = ObjectToOBJECTREF(pThisUNSAFE);
    STRINGREF refString = NULL;
    
    HELPER_METHOD_FRAME_BEGIN_RET_2(refThis, refString);

    // Note that this has to be virtual. Consider this case
    //
    // interface INativeA
    // {

    //     string ToString();
    // }
    // 
    // class NativeA : INativeA
    // {
    //     protected override ToString()
    //     {
    //         .override IA.ToString()
    //     }
    // }
    // 
    // class Managed : NativeA
    // {
    //     override ToString();            
    // }
    //
    // If we call IA.ToString virtually, we'll land on INativeA.ToString() which is not correct.
    // Calling it virtually will solve this problem    
    PREPARE_VIRTUAL_CALLSITE_USING_METHODDESC(pToStringMD, refThis);

    DECLARE_ARGHOLDER_ARRAY(ToStringArgs, 1);
    ToStringArgs[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(refThis);

    CALL_MANAGED_METHOD_RETREF(refString, STRINGREF, ToStringArgs);

    HELPER_METHOD_FRAME_END();
    
    return STRINGREFToObject(refString);
}
FCIMPLEND

//
// Redirection for GetHashCode
//
NOINLINE static MethodDesc *GetRedirectedGetHashCodeMDHelper(Object *pThisUNSAFE, MethodTable *pMT)
{
    FC_INNER_PROLOG(ComObject::GetRedirectedGetHashCodeMD);
    
    MethodDesc *pRetMD = NULL;
    
    // Creates helper frame for GetOrCreateWinRTOverrideInfo (which throws)    
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);
    
    WinRTOverrideInfo *pOverrideInfo = WinRTOverrideInfo::GetOrCreateWinRTOverrideInfo(pMT);
    if (pOverrideInfo && pOverrideInfo->m_pGetHashCodeMD != NULL)
    {
        pRetMD = pOverrideInfo->m_pGetHashCodeMD;
    }    
    
    HELPER_METHOD_FRAME_END();
    
    FC_INNER_EPILOG();
    
    return pRetMD;
}

FCIMPL1(MethodDesc *, ComObject::GetRedirectedGetHashCodeMD, Object *pThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pThisUNSAFE));
    }
    CONTRACTL_END;

    MethodTable *pMT = pThisUNSAFE->GetMethodTable();
    if (pMT->IsSkipWinRTOverride())
        return NULL;

    FC_INNER_RETURN(MethodDesc*, ::GetRedirectedGetHashCodeMDHelper(pThisUNSAFE, pMT));    
}
FCIMPLEND

FCIMPL2(int, ComObject::RedirectGetHashCode, Object *pThisUNSAFE, MethodDesc *pGetHashCodeMD)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF refThis = ObjectToOBJECTREF(pThisUNSAFE);
    int hash = 0; 
    
    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);
    
    // Note that this has to be virtual. See RedirectToString for more details
    PREPARE_VIRTUAL_CALLSITE_USING_METHODDESC(pGetHashCodeMD, refThis);

    DECLARE_ARGHOLDER_ARRAY(GetHashCodeArgs, 1);
    GetHashCodeArgs[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(refThis);

    CALL_MANAGED_METHOD(hash, int, GetHashCodeArgs);

    HELPER_METHOD_FRAME_END();
    
    return hash;
}
FCIMPLEND

NOINLINE static MethodDesc *GetRedirectedEqualsMDHelper(Object *pThisUNSAFE, MethodTable *pMT)
{
    FC_INNER_PROLOG(ComObject::GetRedirectedEqualsMD);

    MethodDesc *pRetMD = NULL;

    // Creates helper frame for GetOrCreateWinRTOverrideInfo (which throws)    
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    WinRTOverrideInfo *pOverrideInfo = WinRTOverrideInfo::GetOrCreateWinRTOverrideInfo(pMT);
    if (pOverrideInfo && pOverrideInfo->m_pEqualsMD!= NULL)
    {
        pRetMD = pOverrideInfo->m_pEqualsMD;
    }    
    
    HELPER_METHOD_FRAME_END();
    
    FC_INNER_EPILOG();
    
    return pRetMD;
}

FCIMPL1(MethodDesc *, ComObject::GetRedirectedEqualsMD, Object *pThisUNSAFE)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pThisUNSAFE));
    }
    CONTRACTL_END;

    MethodTable *pMT = pThisUNSAFE->GetMethodTable();
    if (pMT->IsSkipWinRTOverride())
        return NULL;

    FC_INNER_RETURN(MethodDesc*, ::GetRedirectedEqualsMDHelper(pThisUNSAFE, pMT));    
}
FCIMPLEND

FCIMPL3(FC_BOOL_RET, ComObject::RedirectEquals, Object *pThisUNSAFE, Object *pOtherUNSAFE, MethodDesc *pEqualsMD)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pThisUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF refThis = ObjectToOBJECTREF(pThisUNSAFE);
    OBJECTREF refOther = ObjectToOBJECTREF(pOtherUNSAFE);
    
    CLR_BOOL ret = FALSE;
    
    HELPER_METHOD_FRAME_BEGIN_RET_2(refThis, refOther);
    
    // Note that this has to be virtual. See RedirectToString for more details
    PREPARE_VIRTUAL_CALLSITE_USING_METHODDESC(pEqualsMD, refThis);

    DECLARE_ARGHOLDER_ARRAY(EqualArgs, 2);
    EqualArgs[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(refThis);
    EqualArgs[ARGNUM_1] = OBJECTREF_TO_ARGHOLDER(refOther);

    CALL_MANAGED_METHOD(ret, CLR_BOOL, EqualArgs);

    HELPER_METHOD_FRAME_END();
    
    FC_RETURN_BOOL(ret);
}
FCIMPLEND

#endif // #ifndef DACCESS_COMPILE

#endif //#ifndef CROSSGEN_COMPILE

