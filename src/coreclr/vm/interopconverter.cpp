// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "vars.hpp"
#include "excep.h"
#include "interoputil.h"
#include "interopconverter.h"
#include "olevariant.h"
#include "comcallablewrapper.h"

#ifdef FEATURE_COMINTEROP

#include "stdinterfaces.h"
#include "runtimecallablewrapper.h"
#include "cominterfacemarshaler.h"
#include "binder.h"
#include <interoplibinterface.h>
#include "typestring.h"

namespace
{
    bool TryGetComIPFromObjectRefUsingComWrappers(
        _In_ OBJECTREF instance,
        _Outptr_ IUnknown** wrapperRaw)
    {
#ifdef FEATURE_COMWRAPPERS
        return GlobalComWrappersForMarshalling::TryGetOrCreateComInterfaceForObject(instance, (void**)wrapperRaw);
#else
        return false;
#endif // FEATURE_COMWRAPPERS
    }

    bool TryGetObjectRefFromComIPUsingComWrappers(
        _In_ IUnknown* pUnknown,
        _In_ DWORD dwFlags,
        _Out_ OBJECTREF *pObjOut)
    {
#ifdef FEATURE_COMWRAPPERS
        return GlobalComWrappersForMarshalling::TryGetOrCreateObjectForComInstance(pUnknown, dwFlags, pObjOut);
#else
        return false;
#endif // FEATURE_COMWRAPPERS
    }

    void EnsureObjectRefIsValidForSpecifiedClass(
        _In_ OBJECTREF *obj,
        _In_ DWORD dwFlags,
        _In_ MethodTable *pMTClass)
    {
        _ASSERTE(*obj != NULL);
        _ASSERTE(pMTClass != NULL);

        if ((dwFlags & ObjFromComIP::CLASS_IS_HINT) != 0)
            return;

        // make sure we can cast to the specified class
        FAULT_NOT_FATAL();

        // Bad format exception thrown for backward compatibility
        THROW_BAD_FORMAT_MAYBE(pMTClass->IsArray() == FALSE, BFA_UNEXPECTED_ARRAY_TYPE, pMTClass);

        if (CanCastComObject(*obj, pMTClass))
            return;

        StackSString ssObjClsName;
        StackSString ssDestClsName;

        (*obj)->GetMethodTable()->_GetFullyQualifiedNameForClass(ssObjClsName);
        pMTClass->_GetFullyQualifiedNameForClass(ssDestClsName);

        COMPlusThrow(kInvalidCastException, IDS_EE_CANNOTCAST,
            ssObjClsName.GetUnicode(), ssDestClsName.GetUnicode());
    }
}

//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, ...)
// Convert ObjectRef to a COM IP, based on MethodTable* pMT.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, MethodTable *pMT, BOOL bEnableCustomizedQueryInterface)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(poref));
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");
        POSTCONDITION((*poref) != NULL ? CheckPointer(RETVAL) : CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    BOOL        fReleaseWrapper     = false;
    HRESULT     hr                  = E_NOINTERFACE;
    SafeComHolder<IUnknown> pUnk    = NULL;
    size_t      ul                  = 0;

    if (*poref == NULL)
        RETURN NULL;

    if (TryGetComIPFromObjectRefUsingComWrappers(*poref, &pUnk))
    {
        GUID iid;
        pMT->GetGuid(&iid, /*bGenerateIfNotFound*/ FALSE, /*bClassic*/ FALSE);

        IUnknown* pvObj;
        hr = SafeQueryInterface(pUnk, iid, &pvObj);
        if (FAILED(hr))
            COMPlusThrowHR(hr);

        RETURN pvObj;
    }

    if (!g_pConfig->IsBuiltInCOMSupported())
        COMPlusThrow(kNotSupportedException, W("NotSupported_COM"));

    SyncBlock* pBlock = (*poref)->GetSyncBlock();

    InteropSyncBlockInfo* pInteropInfo = pBlock->GetInteropInfo();

    // If we have a CCW, or a NULL CCW but the RCW field was never used,
    //  get the pUnk from the ComCallWrapper, otherwise from the RCW
    if ((NULL != pInteropInfo->GetCCW()) || (!pInteropInfo->RCWWasUsed()))
    {
        CCWHolder pCCWHold = ComCallWrapper::InlineGetWrapper(poref);

        GetComIPFromCCW::flags flags = GetComIPFromCCW::None;
        if (!bEnableCustomizedQueryInterface)   { flags |= GetComIPFromCCW::SuppressCustomizedQueryInterface; }

        pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, GUID_NULL, pMT, flags);
    }
    else
    {
        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, pBlock);

        // The interface will be returned addref'ed.
        pUnk = pRCW->GetComIPFromRCW(pMT);

        RCWPROTECT_END(pRCW);
    }

    // If we failed to retrieve an IP then throw an exception.
    if (pUnk == NULL)
        COMPlusThrowHR(hr);

    pUnk.SuppressRelease();
    RETURN pUnk;
}


//--------------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, ComIpType ReqIpType, ComIpType *pFetchedIpType);
// Convert ObjectRef to a COM IP of the requested type.
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, ComIpType ReqIpType, ComIpType *pFetchedIpType)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION((ReqIpType & (ComIpType_Dispatch | ComIpType_Unknown)) != 0);
        PRECONDITION(CheckPointer(poref));
        PRECONDITION(ReqIpType != 0);
        POSTCONDITION((*poref) != NULL ? CheckPointer(RETVAL) : CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // COM had better be started up at this point.
    _ASSERTE(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");

    BOOL        fReleaseWrapper = false;
    HRESULT     hr              = E_NOINTERFACE;
    IUnknown*   pUnk            = NULL;
    size_t      ul              = 0;
    ComIpType   FetchedIpType   = ComIpType_None;

    if (*poref == NULL)
        RETURN NULL;

    if (TryGetComIPFromObjectRefUsingComWrappers(*poref, &pUnk))
    {
        hr = S_OK;

        IUnknown* pvObj;
        if (ReqIpType & ComIpType_Dispatch)
        {
            hr = SafeQueryInterface(pUnk, IID_IDispatch, &pvObj);
            pUnk->Release();
        }
        else
        {
            pvObj = pUnk;
        }

        if (FAILED(hr))
            COMPlusThrowHR(hr);

        if (pFetchedIpType != NULL)
            *pFetchedIpType = ReqIpType;

        RETURN pvObj;
    }

    if (!g_pConfig->IsBuiltInCOMSupported())
        COMPlusThrow(kNotSupportedException, W("NotSupported_COM"));

    MethodTable *pMT = (*poref)->GetMethodTable();

    SyncBlock* pBlock = (*poref)->GetSyncBlock();

    InteropSyncBlockInfo* pInteropInfo = pBlock->GetInteropInfo();

    if ( (NULL != pInteropInfo->GetCCW()) || (!pInteropInfo->RCWWasUsed()) )
    {
        CCWHolder pCCWHold = ComCallWrapper::InlineGetWrapper(poref);

        // If the user requested IDispatch, then check for IDispatch first.
        if (ReqIpType & ComIpType_Dispatch)
        {
            pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, IID_IDispatch, NULL);
            if (pUnk)
                FetchedIpType = ComIpType_Dispatch;
        }

        // If the ObjectRef doesn't support IDispatch and the caller also accepts
        // an IUnknown pointer, then check for IUnknown.
        if (!pUnk && (ReqIpType & ComIpType_Unknown))
        {
            if (ReqIpType & ComIpType_OuterUnknown)
            {
                // check if the object is aggregated
                SimpleComCallWrapper* pSimpleWrap = pCCWHold->GetSimpleWrapper();
                if (pSimpleWrap)
                {
                    pUnk = pSimpleWrap->GetOuter();
                    if (pUnk)
                        SafeAddRef(pUnk);
                }
            }
            if (!pUnk)
                pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, IID_IUnknown, NULL);
            if (pUnk)
                FetchedIpType = ComIpType_Unknown;
        }
    }
    else
    {
        RCWHolder pRCW(GetThread());

        // This code is hot, use a simple RCWHolder check (i.e. don't increment the use count on the RCW).
        // @TODO: Cache IDispatch so we don't have to QI every time we come here.
        pRCW.InitFastCheck(pBlock);

        // If the user requested IDispatch, then check for IDispatch first.
        if (ReqIpType & ComIpType_Dispatch)
        {
            pUnk = pRCW->GetIDispatch();
            if (pUnk)
                FetchedIpType = ComIpType_Dispatch;
        }

        // If the ObjectRef doesn't support IDispatch and the caller also accepts
        // an IUnknown pointer, then check for IUnknown.
        if (!pUnk && (ReqIpType & ComIpType_Unknown))
        {
            pUnk = pRCW->GetIUnknown();
            if (pUnk)
                FetchedIpType = ComIpType_Unknown;
        }
    }

    // If we failed to retrieve an IP then throw an exception.
    if (pUnk == NULL)
        COMPlusThrowHR(hr);

    // If the caller wants to know the fetched IP type, then set pFetchedIpType
    // to the type of the IP.
    if (pFetchedIpType)
        *pFetchedIpType = FetchedIpType;

    RETURN pUnk;
}


//+----------------------------------------------------------------------------
// IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, REFIID iid);
// convert ComIP to an ObjectRef, based on riid
//+----------------------------------------------------------------------------
IUnknown *GetComIPFromObjectRef(OBJECTREF *poref, REFIID iid, bool throwIfNoComIP /* = true */)
{
    CONTRACT (IUnknown*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(poref));
        POSTCONDITION((*poref) != NULL ? CheckPointer(RETVAL, throwIfNoComIP ? NULL_NOT_OK : NULL_OK) : CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    ASSERT_PROTECTED(poref);

    // COM had better be started up at this point.
    _ASSERTE(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");

    BOOL        fReleaseWrapper = false;
    HRESULT     hr              = E_NOINTERFACE;
    IUnknown*   pUnk            = NULL;
    size_t      ul              = 0;

    if (*poref == NULL)
        RETURN NULL;

    if (TryGetComIPFromObjectRefUsingComWrappers(*poref, &pUnk))
    {
        IUnknown* pvObj;
        hr = SafeQueryInterface(pUnk, iid, &pvObj);
        pUnk->Release();
        if (FAILED(hr))
            COMPlusThrowHR(hr);

        RETURN pvObj;
    }

    MethodTable *pMT = (*poref)->GetMethodTable();

    SyncBlock* pBlock = (*poref)->GetSyncBlock();

    InteropSyncBlockInfo* pInteropInfo = pBlock->GetInteropInfo();

    if ((NULL != pInteropInfo->GetCCW()) || (!pInteropInfo->RCWWasUsed()))
    {
        CCWHolder pCCWHold = ComCallWrapper::InlineGetWrapper(poref);
        pUnk = ComCallWrapper::GetComIPFromCCW(pCCWHold, iid, NULL);
    }
    else
    {
        SafeComHolder<IUnknown> pUnkHolder;

        RCWHolder pRCW(GetThread());
        RCWPROTECT_BEGIN(pRCW, pBlock);

        // The interface will be returned addref'ed.
        pUnkHolder = pRCW->GetComIPFromRCW(iid);

        RCWPROTECT_END(pRCW);

        pUnk = pUnkHolder.Extract();
    }

    if (throwIfNoComIP && pUnk == NULL)
        COMPlusThrowHR(hr);

    RETURN pUnk;
}


//+----------------------------------------------------------------------------
// GetObjectRefFromComIP
// pUnk : input IUnknown
// pMTClass : specifies the type of instance to be returned
// NOTE:**  As per COM Rules, the IUnknown passed in shouldn't be AddRef'ed
//+----------------------------------------------------------------------------
void GetObjectRefFromComIP(OBJECTREF* pObjOut, IUnknown **ppUnk, MethodTable *pMTClass, MethodTable *pItfMT, DWORD dwFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(ppUnk));
        PRECONDITION(CheckPointer(*ppUnk, NULL_OK));
        PRECONDITION(CheckPointer(pMTClass, NULL_OK));
        PRECONDITION(IsProtectedByGCFrame(pObjOut));
        PRECONDITION(pItfMT == NULL || pItfMT->IsInterface());
    }
    CONTRACTL_END;

    // COM had better be started up at this point.
    _ASSERTE(g_fComStarted && "COM has not been started up, make sure EnsureComStarted is called before any COM objects are used!");

    IUnknown *pUnk = *ppUnk;
    *pObjOut = NULL;

    if (TryGetObjectRefFromComIPUsingComWrappers(pUnk, dwFlags, pObjOut))
    {
        if (pMTClass != NULL)
            EnsureObjectRefIsValidForSpecifiedClass(pObjOut, dwFlags, pMTClass);

        return;
    }

    if (!g_pConfig->IsBuiltInCOMSupported())
        COMPlusThrow(kNotSupportedException, W("NotSupported_COM"));

    Thread * pThread = GetThread();

    IUnknown* pOuter = pUnk;
    SafeComHolder<IUnknown> pAutoOuterUnk = NULL;

    if (pUnk != NULL)
    {
        // get CCW for IUnknown
        ComCallWrapper *ccw = GetCCWFromIUnknown(pUnk);
        if (ccw == NULL)
        {
            // could be aggregated scenario
            HRESULT hr = SafeQueryInterface(pUnk, IID_IUnknown, &pOuter);
            LogInteropQI(pUnk, IID_IUnknown, hr, "GetObjectRefFromComIP: QI for Outer");
            IfFailThrow(hr);

            // store the outer in the auto pointer
            pAutoOuterUnk = pOuter;
            ccw = GetCCWFromIUnknown(pOuter);
        }

        // If the CCW was activated via COM, do not unwrap it.
        // Unwrapping a CCW would deliver the underlying OBJECTREF,
        // but when a managed class is activated via COM it should
        // remain a COM object and adhere to COM rules.
        if (ccw != NULL
            && !ccw->IsComActivated())
        {
            *pObjOut = ccw->GetObjectRef();
        }

        if (*pObjOut == NULL)
        {
            // Only pass in the class method table to the interface marshaler if
            // it is a COM import (or COM import derived) class.
            MethodTable *pComClassMT = NULL;
            if (pMTClass)
            {
                if (pMTClass->IsComObjectType())
                {
                    pComClassMT = pMTClass;
                }
            }

            DWORD flags = RCW::CreationFlagsFromObjForComIPFlags((ObjFromComIP::flags)dwFlags);

            // Convert the IP to an OBJECTREF.
            COMInterfaceMarshaler marshaler;

            marshaler.Init(pOuter, pComClassMT, pThread, flags);
            *pObjOut = marshaler.FindOrCreateObjectRef(pUnk, pItfMT);
        }
    }


    if ((0 == (dwFlags & ObjFromComIP::CLASS_IS_HINT)) && (*pObjOut != NULL))
    {
        // make sure we can cast to the specified class
        if (pMTClass != NULL)
        {
            EnsureObjectRefIsValidForSpecifiedClass(pObjOut, dwFlags, pMTClass);
        }
    }
}
#endif // FEATURE_COMINTEROP


