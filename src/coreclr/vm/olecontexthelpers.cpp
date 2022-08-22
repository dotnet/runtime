// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT

#include "olecontexthelpers.h"
#include "oletls.h"

HRESULT GetCurrentObjCtx(IUnknown **ppObjCtx)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS; // This can occur if IMallocSpy is implemented in managed code.
        MODE_ANY;
        PRECONDITION(CheckPointer(ppObjCtx));
#ifdef FEATURE_COMINTEROP
        PRECONDITION(TRUE == g_fComStarted);
#endif // FEATURE_COMINTEROP
    }
    CONTRACTL_END;

    return CoGetObjectContext(IID_IUnknown, (void **)ppObjCtx);
}

//=====================================================================
// LPVOID SetupOleContext()
LPVOID SetupOleContext()
{
    CONTRACT (LPVOID)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    IUnknown* pObjCtx = NULL;

    BEGIN_ENTRYPOINT_VOIDRET;

#ifdef FEATURE_COMINTEROP
    if (g_fComStarted)
    {
        HRESULT hr = GetCurrentObjCtx(&pObjCtx);
        if (hr == S_OK)
        {
            SOleTlsData* _pData = (SOleTlsData *) ClrTeb::GetOleReservedPtr();
            if (_pData && _pData->pCurrentCtx == NULL)
            {
                _pData->pCurrentCtx = (CObjectContext*)pObjCtx;   // no release !!!!
            }
            else
            {
                // We can't call SafeRelease here since that would transition
                // to preemptive GC mode which is bad since SetupOleContext is called
                // from places where we can't take a GC.
                ULONG cbRef = pObjCtx->Release();
            }
        }
    }
#endif // FEATURE_COMINTEROP

    END_ENTRYPOINT_VOIDRET;

    RETURN pObjCtx;
}

//================================================================
// LPVOID GetCurrentCtxCookie()
LPVOID GetCurrentCtxCookie()
{
    CONTRACT (LPVOID)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

#ifdef FEATURE_COMINTEROP
    // check if com is started
    if (!g_fComStarted)
        RETURN NULL;
#endif // FEATURE_COMINTEROP

    ULONG_PTR ctxptr = 0;

    if (CoGetContextToken(&ctxptr) != S_OK)
        ctxptr = 0;

    RETURN (LPVOID)ctxptr;
}

//+-------------------------------------------------------------------------
//
//  HRESULT GetCurrentThreadTypeNT5(THDTYPE* pType)
//
HRESULT GetCurrentThreadTypeNT5(THDTYPE* pType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pType));
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

    IObjectContext *pObjCurrCtx = (IObjectContext *)GetCurrentCtxCookie();
    if(pObjCurrCtx)
    {
        GCX_PREEMP();

        SafeComHolderPreemp<IComThreadingInfo> pThreadInfo;
        hr = SafeQueryInterface(pObjCurrCtx, IID_IComThreadingInfo, (IUnknown **)&pThreadInfo);
        if(hr == S_OK)
        {
            _ASSERTE(pThreadInfo);
            hr = pThreadInfo->GetCurrentThreadType(pType);
        }
    }
    return hr;
}

//+-------------------------------------------------------------------------
//
//  HRESULT GetCurrentApartmentTypeNT5(APTTYPE* pType)
//
HRESULT GetCurrentApartmentTypeNT5(IObjectContext *pObjCurrCtx, APTTYPE* pType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pType));
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;
    if(pObjCurrCtx)
    {
        GCX_PREEMP();

        SafeComHolderPreemp<IComThreadingInfo> pThreadInfo;
        hr = SafeQueryInterface(pObjCurrCtx, IID_IComThreadingInfo, (IUnknown **)&pThreadInfo);
        if(hr == S_OK)
        {
            _ASSERTE(pThreadInfo);
            hr = pThreadInfo->GetCurrentApartmentType(pType);
        }
    }
    return hr;
}

#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

