// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//---------------------------------------------------------------------------------
// stdinterfaces.cpp
//
// Defines various standard com interfaces

//---------------------------------------------------------------------------------


#include "common.h"

#include <ole2.h>
#include <guidfromname.h>
#include <olectl.h>
#include <objsafe.h>    // IID_IObjectSafety
#include "vars.hpp"
#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "runtimecallablewrapper.h"
#include "stdinterfaces.h"
#include "comcallablewrapper.h"
#include "field.h"
#include "threads.h"
#include "interoputil.h"
#include "comdelegate.h"
#include "olevariant.h"
#include "eeconfig.h"
#include "typehandle.h"
#include "posterror.h"
#include <corerror.h>
#include <mscoree.h>
#include "mtx.h"
#include "cgencpu.h"
#include "interopconverter.h"
#include "cominterfacemarshaler.h"
#include "eecontract.h"
#include "stdinterfaces_internal.h"
#include "interoputil.inl"


//------------------------------------------------------------------------------------------
//      Definitions used by the IDispatchEx implementation

// The names of the properties that are accessed on the managed member info's
#define MEMBER_INFO_NAME_PROP           "Name"
#define MEMBER_INFO_TYPE_PROP           "MemberType"
#define PROPERTY_INFO_CAN_READ_PROP     "CanRead"
#define PROPERTY_INFO_CAN_WRITE_PROP    "CanWrite"


// {00020430-0000-0000-C000-000000000046}
static const GUID LIBID_STDOLE2 = { 0x00020430, 0x0000, 0x0000, { 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };

// Until the Windows SDK is updated, just hard-code the IAgileObject IID
#ifndef __IAgileObject_INTERFACE_DEFINED__
EXTERN_C const GUID IID_IAgileObject = { 0x94ea2b94, 0xe9cc, 0x49e0, { 0xc0, 0xff, 0xee, 0x64, 0xca, 0x8f, 0x5b, 0x90 } };
#endif // !__IAgileObject_INTERFACE_DEFINED__

// Until the Windows SDK is updated, just hard-code the INoMarshal IID
#ifndef __INoMarshal_INTERFACE_DEFINED__
static const GUID IID_INoMarshal = {0xecc8691b, 0xc1db, 0x4dc0, { 0x85, 0x5e, 0x65, 0xf6, 0xc5, 0x51, 0xaf, 0x49 } };
#endif // !__INoMarshal_INTERFACE_DEFINED__

// NOTE: In the following vtables, QI points to the same function
//       this is because, during marshalling between COM & COM+ we want a fast way to
//       check if a COM IP is a tear-off that we created.

// array of vtable pointers for std. interfaces such as IProvideClassInfo etc.
const SLOT * const g_rgStdVtables[] =
{
    (SLOT*)&g_InnerUnknown.m_vtable,
    (SLOT*)&g_IProvideClassInfo.m_vtable,
    (SLOT*)&g_IMarshal.m_vtable,
    (SLOT*)&g_ISupportsErrorInfo.m_vtable,
    (SLOT*)&g_IErrorInfo.m_vtable,
    (SLOT*)&g_IConnectionPointContainer.m_vtable,
    (SLOT*)&g_IObjectSafety.m_vtable,
    (SLOT*)&g_IDispatchEx.m_vtable,
    (SLOT*)&g_IAgileObject.m_vtable
};

//------------------------------------------------------------------------------------------
//      IUnknown methods for CLR objects


HRESULT
Unknown_QueryInterface_Internal(ComCallWrapper* pWrap, IUnknown* pUnk, REFIID riid, void** ppv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsInProcCCWTearOff(pUnk));
        PRECONDITION(CheckPointer(ppv, NULL_OK));
        PRECONDITION(CheckPointer(pWrap));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SafeComHolderPreemp<IUnknown> pDestItf = NULL;

    // Validate the arguments.
    if (!ppv)
        return E_POINTER;

    // Initialize the returned interface pointer to NULL before we start.
    *ppv = NULL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        // Initialize the HRESULT to E_NOINTERFACE. This must be done after the
        // BEGIN_EXTERNAL_ENTRYPOINT since otherwise it will be reset to S_OK by
        // BEGIN_EXTERNAL_ENTRYPOINT.
        hr = E_NOINTERFACE;

        // Check for QIs on inner unknown
        if (!IsInnerUnknown(pUnk))
        {
            // Aggregation support, delegate to the outer unknown if non null.
            IUnknown *pOuter = pWrap->GetSimpleWrapper()->GetOuter();
            if (pOuter != NULL)
            {
                hr = SafeQueryInterfacePreemp(pOuter, riid, &pDestItf);
                LogInteropQI(pOuter, riid, hr, "QI to outer Unknown");
                IfFailGo(hr);
            }
        }
        else
        {
            // Assert the component has been aggregated
            _ASSERTE(pWrap->GetSimpleWrapper()->GetOuter() != NULL);

            // Okay special case IUnknown
            if (IsEqualIID(riid, IID_IUnknown))
            {
                SafeAddRefPreemp(pUnk);
                pDestItf = pUnk;
            }
        }

        // If we haven't found the IP or if we haven't looked yet (because we aren't
        // being aggregated), now look on the managed object to see if it supports the interface.
        if (pDestItf == NULL)
        {
            EX_TRY
            {
                pDestItf = ComCallWrapper::GetComIPFromCCW(pWrap, riid, NULL, GetComIPFromCCW::CheckVisibility);
            }
            EX_CATCH
            {
                Exception *e = GET_EXCEPTION();
                hr = e->GetHR();
            }
            EX_END_CATCH(RethrowTerminalExceptions)
        }

ErrExit:
        // If we succeeded in obtaining the requested IP then return S_OK.
        if (pDestItf != NULL)
            hr = S_OK;
    }
    END_EXTERNAL_ENTRYPOINT;

    if (SUCCEEDED(hr))
    {
        // If we succeeded in obtaining the requested IP, set ppv to the interface.
        _ASSERTE(pDestItf != NULL);
        *ppv = pDestItf;
        pDestItf.SuppressRelease();
    }

    return hr;
}  // Unknown_QueryInterface_Internal


ULONG __stdcall
Unknown_AddRefInner_Internal(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    SimpleComCallWrapper* pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);
    ComCallWrapper* pWrap = pSimpleWrap->GetMainWrapper();

    // Assert the component has been aggregated
    _ASSERTE(pSimpleWrap->GetOuter() != NULL);

    // We are guaranteed to be in the right domain here, so can always get the oref
    // w/o fear of the handle having been deleted.
    return pWrap->AddRef();
} // Unknown_AddRef


ULONG __stdcall
Unknown_AddRef_Internal(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    ComCallWrapper* pWrap = ComCallWrapper::GetWrapperFromIP(pUnk);

    // check for aggregation
    IUnknown *pOuter;
    SimpleComCallWrapper* pSimpleWrap = pWrap->GetSimpleWrapper();
    if (pSimpleWrap  && (pOuter = pSimpleWrap->GetOuter()) != NULL)
    {
        // If we are in process detach, we cannot safely call release on our outer.
        if (g_fProcessDetach)
            return 1;

        ULONG cbRef = pOuter->AddRef();
        LogInteropAddRef(pOuter, cbRef, "Delegate to outer");
        return cbRef;
    }
    // are guaranteed to be in the right domain here, so can always get the oref
    // w/o fear of the handle having been deleted.
    return pWrap->AddRef();
} // Unknown_AddRef


ULONG __stdcall
Unknown_ReleaseInner_Internal(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    ULONG cbRef = -1;

    SimpleComCallWrapper* pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);
    ComCallWrapper* pWrap = pSimpleWrap->GetMainWrapper();

    // Assert the component has been aggregated
    _ASSERTE(pSimpleWrap->GetOuter() != NULL);

    // We know for sure this wrapper is a start wrapper let us pass this information in
    cbRef = pWrap->Release();

    return cbRef;
} // Unknown_Release

ULONG __stdcall
Unknown_Release_Internal(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    ULONG cbRef = -1;

    // check for aggregation
    ComCallWrapper* pWrap = ComCallWrapper::GetWrapperFromIP(pUnk);
    SimpleComCallWrapper* pSimpleWrap = pWrap->GetSimpleWrapper();
    IUnknown *pOuter;
    if (pSimpleWrap  && (pOuter = pSimpleWrap->GetOuter()) != NULL)
    {
        // If we are in process detach, we cannot safely call release on our outer.
        if (g_fProcessDetach)
            cbRef = 1;

        cbRef = SafeReleasePreemp(pOuter);
        LogInteropRelease(pOuter, cbRef, "Delegate Release to outer");
    }
    else
    {
        cbRef = pWrap->Release();
    }

    return cbRef;
} // Unknown_Release


// ---------------------------------------------------------------------------
//  for simple tearoffs
// ---------------------------------------------------------------------------
ULONG __stdcall
Unknown_AddRefSpecial_Internal(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
    }
    CONTRACTL_END;

    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);
    return pSimpleWrap->AddRefWithAggregationCheck();
} // Unknown_AddRefSpecial

// ---------------------------------------------------------------------------
// for simplecomcall wrappers, stdinterfaces such as IProvideClassInfo etc.
// ---------------------------------------------------------------------------
ULONG __stdcall
Unknown_ReleaseSpecial_Internal(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    ULONG cbRef = -1;

    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

    // aggregation check
    IUnknown *pOuter = pSimpleWrap->GetOuter();
    if (pOuter != NULL)
    {
        cbRef = SafeReleasePreemp(pOuter);
    }
    else
    {
        cbRef = pSimpleWrap->Release();
    }

    return cbRef;
} // Unknown_Release


HRESULT __stdcall
Unknown_QueryInterface_IErrorInfo_Simple(IUnknown* pUnk, REFIID riid, void** ppv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsInProcCCWTearOff(pUnk));
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!ppv)
        return E_POINTER;
    *ppv = NULL;

    EX_TRY
    {
        hr = E_NOINTERFACE;

        _ASSERTE(!IsInnerUnknown(pUnk) && IsSimpleTearOff(pUnk));

        SimpleComCallWrapper* pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

        // we must not switch to cooperative GC mode here, so respond only to the
        // two interfaces we always support
        if (riid == IID_IUnknown || riid == IID_IErrorInfo)
        {
            *ppv = pUnk;
            pSimpleWrap->AddRef();
            hr = S_OK;
        }
    }
    EX_CATCH_HRESULT_NO_ERRORINFO(hr);

    return hr;
}  // Unknown_QueryInterface_IErrorInfo_Simple

// ---------------------------------------------------------------------------
ULONG __stdcall
Unknown_ReleaseSpecial_IErrorInfo_Internal(IUnknown* pUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
    }
    CONTRACTL_END;

    ULONG cbRef = -1;

    EX_TRY
        SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);
        cbRef = pSimpleWrap->Release();
    EX_CATCH
    EX_END_CATCH(SwallowAllExceptions)

    return cbRef;
}


// ---------------------------------------------------------------------------
//  Interface IProvideClassInfo
// ---------------------------------------------------------------------------
HRESULT __stdcall
ClassInfo_GetClassInfo(IUnknown* pUnk, ITypeInfo** ppTI)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppTI));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        _ASSERTE(IsSimpleTearOff(pUnk));

        SimpleComCallWrapper *pWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

        // If this is an extensible RCW then we need to check to see if the CLR part of the
        // herarchy is visible to COM.
        if (pWrap->IsExtendsCOMObject())
        {
            // Retrieve the wrapper template for the class.
            ComCallWrapperTemplate *pTemplate = ComCallWrapperTemplate::GetTemplate(pWrap->GetMethodTable());

            // Find the first COM visible IClassX starting at ComMethodTable passed in and
            // walking up the hierarchy.
            ComMethodTable *pComMT = NULL;
            if (pTemplate->SupportsIClassX())
            {
                for (pComMT = pTemplate->GetClassComMT(); pComMT && !pComMT->IsComVisible(); pComMT = pComMT->GetParentClassComMT());
            }

            // If the CLR part of the object is not visible then delegate the call to the
            // base COM object if it implements IProvideClassInfo.
            if (!pComMT || pComMT->GetMethodTable()->ParentEquals(g_pObjectClass))
            {
                IProvideClassInfo *pProvClassInfo = NULL;

                SyncBlock* pBlock = pWrap->GetSyncBlock();
                _ASSERTE(pBlock);

                RCWHolder pRCW(GetThread());
                RCWPROTECT_BEGIN(pRCW, pBlock);

                hr = pRCW->SafeQueryInterfaceRemoteAware(IID_IProvideClassInfo, (IUnknown**)&pProvClassInfo);
                if (SUCCEEDED(hr))
                {
                    hr = pProvClassInfo->GetClassInfo(ppTI);
                    ULONG cbRef = SafeRelease(pProvClassInfo);
                    LogInteropRelease(pProvClassInfo, cbRef, "ClassInfo_GetClassInfo");
                    IfFailThrow(hr);
                }

                RCWPROTECT_END(pRCW);
            }
        }

        MethodTable* pClass = pWrap->GetMethodTable();
        IfFailThrow(GetITypeInfoForEEClass(pClass, ppTI, true /* bClassInfo */));
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

//------------------------------------------------------------------------------------------
HRESULT GetDefaultInterfaceForCoclass(ITypeInfo *pTI, ITypeInfo **ppTIDef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pTI));
        PRECONDITION(CheckPointer(ppTIDef));
    }
    CONTRACTL_END;

    HRESULT     hr;
    TYPEATTRHolder pAttr(pTI); // Attributes on the first TypeInfo.

    IfFailRet(pTI->GetTypeAttr(&pAttr));
    if (pAttr->typekind != TKIND_COCLASS)
        return TYPE_E_ELEMENTNOTFOUND;

    int flags;

    // If no impltype has the default flag, use 0.
    int defaultInterface = 0;
    for (int i = 0; i < pAttr->cImplTypes; ++i)
    {
        IfFailRet(pTI->GetImplTypeFlags(i, &flags));
        if (flags & IMPLTYPEFLAG_FDEFAULT)
        {
            defaultInterface = i;
            break;
        }
    }

    HREFTYPE href;
    IfFailRet(pTI->GetRefTypeOfImplType(defaultInterface, &href));
    IfFailRet(pTI->GetRefTypeInfo(href, ppTIDef));

    return S_OK;
} // HRESULT GetDefaultInterfaceForCoclass()

//------------------------------------------------------------------------------------------
// Helper to get the ITypeLib* for a Assembly.
HRESULT GetITypeLibForAssembly(_In_ Assembly *pAssembly, _Outptr_ ITypeLib **ppTlb)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(CheckPointer(ppTlb));
    }
    CONTRACTL_END;

    // If the module wasn't imported from COM, fail. In .NET Framework the runtime
    // would generate a ITypeLib instance, but .NET Core doesn't support that.
    if (!pAssembly->IsImportedFromTypeLib())
        return COR_E_NOTSUPPORTED;

    HRESULT hr;

    // Check for cached copy.
    ITypeLib *pTlb = pAssembly->GetTypeLib();
    if (pTlb != nullptr)
    {
        // If the cached value is the invalid sentinal, an attempt was already made but failed.
        if (pTlb == Assembly::InvalidTypeLib)
            return TLBX_E_LIBNOTREGISTERED;

        *ppTlb = pTlb;
        return S_OK;
    }

    // Retrieve the guid for the assembly.
    GUID assemblyGuid;
    IfFailRet(GetTypeLibGuidForAssembly(pAssembly, &assemblyGuid));

    // Retrieve the major and minor version number.
    USHORT wMajor;
    USHORT wMinor;
    IfFailRet(GetTypeLibVersionForAssembly(pAssembly, &wMajor, &wMinor));

    // Attempt to load the exact TypeLib
    hr = LoadRegTypeLib(assemblyGuid, wMajor, wMinor, &pTlb);
    if (FAILED(hr))
    {
        // Try just the Assembly version
        IfFailRet(pAssembly->GetVersion(&wMajor, &wMinor, nullptr, nullptr));
        hr = LoadRegTypeLib(assemblyGuid, wMajor, wMinor, &pTlb);
        if (FAILED(hr))
        {
            // Try loading the highest registered version.
            hr = LoadRegTypeLib(assemblyGuid, -1, -1, &pTlb);
            if (FAILED(hr))
                pTlb = Assembly::InvalidTypeLib;
        }
    }

    bool setCache = pAssembly->TrySetTypeLib(pTlb);
    if (!setCache)
    {
        // Release the TypeLib that isn't going to be used
        if (pTlb != Assembly::InvalidTypeLib)
            pTlb->Release();

        // This call lost the race to set the TypeLib so recusively call
        // this function again to get the one that is set.
        return GetITypeLibForAssembly(pAssembly, ppTlb);
    }

    if (FAILED(hr))
    {
        // Pass the HRESULT on if it is any error other than "TypeLib not registered".
        return (hr == TYPE_E_LIBNOTREGISTERED) ? TLBX_E_LIBNOTREGISTERED : hr;
    }

    *ppTlb = pTlb;
    return S_OK;
} // HRESULT GetITypeLibForAssembly()

HRESULT GetITypeInfoForEEClass(MethodTable *pClass, ITypeInfo **ppTI, bool bClassInfo)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(return E_OUTOFMEMORY);
    }
    CONTRACTL_END;

    GUID clsid;
    GUID ciid;
    ComMethodTable *pComMT              = NULL;
    HRESULT                 hr          = S_OK;
    SafeComHolder<ITypeLib> pITLB       = NULL;
    SafeComHolder<ITypeInfo> pTI        = NULL;
    SafeComHolder<ITypeInfo> pTIDef     = NULL;  // Default typeinfo of a coclass.
    ComCallWrapperTemplate *pTemplate   = NULL;

    GCX_PREEMP();

    // Get the typeinfo.
    if (bClassInfo || pClass->IsInterface() || pClass->IsValueType() || pClass->IsEnum())
    {
        // If the class is not an interface then find the first COM visible IClassX in the hierarchy.
        if (!pClass->IsInterface() && !pClass->IsComImport())
        {
            {
                // Retrieve the ComCallWrapperTemplate from the type.
                GCX_COOP();
                OBJECTREF pThrowable = NULL;
                GCPROTECT_BEGIN(pThrowable);
                {
                    EX_TRY
                    {
                        pTemplate = ComCallWrapperTemplate::GetTemplate(pClass);
                        if (pTemplate->SupportsIClassX())
                        {
                            // Find the first COM visible IClassX starting at ComMethodTable passed in and
                            // walking up the hierarchy.
                            pComMT = pTemplate->GetClassComMT();
                            while (pComMT && !pComMT->IsComVisible())
                                pComMT = pComMT->GetParentClassComMT();
                        }
                    }
                    EX_CATCH
                    {
                        pThrowable = GET_THROWABLE();
                    }
                    EX_END_CATCH(SwallowAllExceptions);

                    if (pThrowable != NULL)
                        hr = SetupErrorInfo(pThrowable);
                }
                GCPROTECT_END();
            }

            if (hr != S_OK)
                goto ReturnHR;

            if (!pTemplate)
            {
                hr = E_OUTOFMEMORY;
                goto ReturnHR;
            }

            // If we haven't managed to find any visible IClassX's then return TYPE_E_ELEMENTNOTFOUND.
            if (!pComMT)
            {
                hr = TYPE_E_ELEMENTNOTFOUND;
                goto ReturnHR;
            }

            // Use the type of the first visible IClassX.
            pClass = pComMT->GetMethodTable();
        }

        // Retrieve the ITypeLib for the assembly containing the type.
        IfFailGo(GetITypeLibForAssembly(pClass->GetAssembly(), &pITLB));

        // Get the GUID of the desired TypeRef.
        IfFailGo(pClass->GetGuidNoThrow(&clsid, TRUE));

        // Retrieve the ITypeInfo from the ITypeLib.
        IfFailGo(pITLB->GetTypeInfoOfGuid(clsid, ppTI));
    }
    else if (pClass->IsComImport())
    {
        // This is a COM imported class, with no IClassX.  Get default interface.
        IfFailGo(GetITypeLibForAssembly(pClass->GetAssembly(), &pITLB));
        IfFailGo(pClass->GetGuidNoThrow(&clsid, TRUE));
        IfFailGo(pITLB->GetTypeInfoOfGuid(clsid, &pTI));
        IfFailGo(GetDefaultInterfaceForCoclass(pTI, &pTIDef));

        *ppTI = pTIDef;
        pTIDef.SuppressRelease();
    }
    else
    {
        // We are attempting to retrieve an ITypeInfo for the default interface on a class.
        TypeHandle hndDefItfClass;
        DefaultInterfaceType DefItfType;
        IfFailGo(TryGetDefaultInterfaceForClass(TypeHandle(pClass), &hndDefItfClass, &DefItfType));
        switch (DefItfType)
        {
            case DefaultInterfaceType_Explicit:
            {
                _ASSERTE(!hndDefItfClass.IsNull());
                _ASSERTE(hndDefItfClass.IsInterface());
                hr = GetITypeInfoForEEClass(hndDefItfClass.GetMethodTable(), ppTI, false /* bClassInfo */);
                break;
            }

            case DefaultInterfaceType_AutoDispatch:
            case DefaultInterfaceType_AutoDual:
            {
                _ASSERTE(!hndDefItfClass.IsNull());
                _ASSERTE(!hndDefItfClass.IsInterface());

                PTR_MethodTable itfClassMT = hndDefItfClass.GetMethodTable();

                // Retrieve the ITypeLib for the assembly containing the type.
                IfFailGo(GetITypeLibForAssembly(itfClassMT->GetAssembly(), &pITLB));

                // Get the GUID of the desired TypeRef.
                IfFailGo(itfClassMT->GetGuidNoThrow(&clsid, TRUE));

                // Generate the IClassX IID from the class.
                TryGenerateClassItfGuid(hndDefItfClass, &ciid);

                hr = pITLB->GetTypeInfoOfGuid(ciid, ppTI);
                break;
            }

            case DefaultInterfaceType_IUnknown:
            case DefaultInterfaceType_BaseComClass:
            {
                // @PERF: Optimize this.
                IfFailGo(LoadRegTypeLib(LIBID_STDOLE2, -1, -1, 0, &pITLB));
                IfFailGo(pITLB->GetTypeInfoOfGuid(IID_IUnknown, ppTI));
                hr = S_USEIUNKNOWN;
                break;
            }

            default:
            {
                _ASSERTE(!"Invalid default interface type!");
                hr = E_FAIL;
                break;
            }
        }
    }

ErrExit:
    if (*ppTI == NULL)
    {
        if (!FAILED(hr))
            hr = E_FAIL;
    }

ReturnHR:
    return hr;
} // HRESULT GetITypeInfoForEEClass()

// Returns a NON-ADDREF'd ITypeInfo.
HRESULT GetITypeInfoForMT(ComMethodTable *pMT, ITypeInfo **ppTI)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(ppTI));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;              // A result.
    ITypeInfo   *pTI;                   // The ITypeInfo.

    pTI = pMT->GetITypeInfo();

    if (pTI == 0)
    {
        MethodTable *pClass = pMT->GetMethodTable();

        hr = GetITypeInfoForEEClass(pClass, &pTI);

        if (SUCCEEDED(hr))
        {
            pMT->SetITypeInfo(pTI);
            SafeReleasePreemp(pTI);
        }
    }

    *ppTI = pTI;
    return hr;
}

//------------------------------------------------------------------------------------------
// helper function to locate error info (if any) after a call, and make sure
// that the error info comes from that call

IErrorInfo *GetSupportedErrorInfo(IUnknown *iface, REFIID riid)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(iface));
    }
    CONTRACTL_END;

    IErrorInfo *pRetErrorInfo = NULL;
    BOOL bUseThisErrorInfo = FALSE;

    // This function must run in preemptive GC mode.
    {
        GCX_PREEMP();
        HRESULT hr = S_OK;
        SafeComHolderPreemp<IErrorInfo> pErrorInfo;

        // See if we have any error info.  (Also this clears out the error info,
        // we want to do this whether it is a recent error or not.)
        hr = SafeGetErrorInfo(&pErrorInfo);
        IfFailThrow(hr);

        // If we successfully retrieved an IErrorInfo, we need to verify if
        // it is for the specifed interface.
        if (hr == S_OK)
        {
            // Make sure that the object we called follows the error info protocol,
            // otherwise the error may be stale, so we just throw it away.
            SafeComHolderPreemp<ISupportErrorInfo> pSupport;
            hr = SafeQueryInterfacePreemp(iface, IID_ISupportErrorInfo, (IUnknown **) &pSupport);
            LogInteropQI(iface, IID_ISupportErrorInfo, hr, "ISupportErrorInfo");
            if (SUCCEEDED(hr))
            {
                hr = pSupport->InterfaceSupportsErrorInfo(riid);
                if (hr == S_OK)
                {
                    // The IErrorInfo is indeed for the specified interface so return it.
                    bUseThisErrorInfo = TRUE;
                }
            }
        }
        if (bUseThisErrorInfo)
        {
            pRetErrorInfo = pErrorInfo;
            pErrorInfo.SuppressRelease();
            pErrorInfo = NULL;
        }
    }

    return pRetErrorInfo;
}

// ---------------------------------------------------------------------------
//  Interface ISupportsErrorInfo
/// ---------------------------------------------------------------------------
HRESULT __stdcall
SupportsErroInfo_IntfSupportsErrorInfo(IUnknown* pUnk, REFIID riid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
    }
    CONTRACTL_END;

    // All interfaces support ErrorInfo
    return S_OK;
}


// ---------------------------------------------------------------------------
//  Interface IErrorInfo
// %%Function: ErrorInfo_GetDescription
// ---------------------------------------------------------------------------
HRESULT __stdcall
ErrorInfo_GetDescription(IUnknown* pUnk, BSTR* pbstrDescription)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
        PRECONDITION(CheckPointer(pbstrDescription, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SimpleComCallWrapper *pWrap = NULL;

    if (pbstrDescription == NULL)
        IfFailGo(E_POINTER);

    pWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        *pbstrDescription = pWrap->IErrorInfo_bstrDescription();
    }
    END_EXTERNAL_ENTRYPOINT;

ErrExit:
    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IErrorInfo
// %%Function: ErrorInfo_GetGUID
// ---------------------------------------------------------------------------
HRESULT __stdcall ErrorInfo_GetGUID(IUnknown* pUnk, GUID* pguid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
        PRECONDITION(CheckPointer(pguid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SimpleComCallWrapper *pWrap = NULL;

    if (pguid == NULL)
        return E_POINTER;

    pWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

    *pguid = pWrap->IErrorInfo_guid();

    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IErrorInfo
// %%Function: ErrorInfo_GetHelpContext
// ---------------------------------------------------------------------------
HRESULT _stdcall ErrorInfo_GetHelpContext(IUnknown* pUnk, DWORD* pdwHelpCtxt)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
        PRECONDITION(CheckPointer(pdwHelpCtxt, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SimpleComCallWrapper *pWrap = NULL;

    if (pdwHelpCtxt == NULL)
        return E_POINTER;

    pWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        *pdwHelpCtxt = pWrap->IErrorInfo_dwHelpContext();
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IErrorInfo
// %%Function: ErrorInfo_GetHelpFile
// ---------------------------------------------------------------------------
HRESULT __stdcall ErrorInfo_GetHelpFile(IUnknown* pUnk, BSTR* pbstrHelpFile)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
        PRECONDITION(CheckPointer(pbstrHelpFile, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SimpleComCallWrapper *pWrap = NULL;

    if (pbstrHelpFile == NULL)
        return E_POINTER;

    pWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        *pbstrHelpFile = pWrap->IErrorInfo_bstrHelpFile();
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IErrorInfo
// %%Function: ErrorInfo_GetSource
// ---------------------------------------------------------------------------
HRESULT __stdcall ErrorInfo_GetSource(IUnknown* pUnk, BSTR* pbstrSource)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
        PRECONDITION(CheckPointer(pbstrSource, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SimpleComCallWrapper *pWrap = NULL;

    if (pbstrSource == NULL)
        return E_POINTER;

    pWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        *pbstrSource = pWrap->IErrorInfo_bstrSource();
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}


//------------------------------------------------------------------------------------------
//  IDispatch methods that forward to the right implementation based on the flags set
//  on the IClassX COM method table.

HRESULT __stdcall
Dispatch_GetTypeInfoCount(IDispatch* pDisp, unsigned int *pctinfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
        PRECONDITION(CheckPointer(pctinfo, NULL_OK));
    }
    CONTRACTL_END;

    if (!pctinfo)
        return E_POINTER;

    *pctinfo = 0;

    ComMethodTable *pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
    if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
        if (pCMT->HasInvisibleParent())
            return E_NOTIMPL;

    ITypeInfo *pTI;
    HRESULT hr = GetITypeInfoForMT(pCMT, &pTI);

    if (SUCCEEDED(hr))
    {
        hr = S_OK;
        *pctinfo = 1;
    }

    return hr;
}

HRESULT __stdcall
Dispatch_GetTypeInfo(IDispatch* pDisp, unsigned int itinfo, LCID lcid, ITypeInfo **pptinfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
        PRECONDITION(CheckPointer(pptinfo, NULL_OK));
    }
    CONTRACTL_END;

    if (!pptinfo)
        return E_POINTER;

    *pptinfo = NULL;

    ComMethodTable *pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
    if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
        if (pCMT->HasInvisibleParent())
            return E_NOTIMPL;

    if (NULL != itinfo)
    {
        return DISP_E_BADINDEX;
    }

    HRESULT hr = GetITypeInfoForMT(pCMT, pptinfo);
    if (SUCCEEDED(hr))
    {
        // GetITypeInfoForMT() can return other success codes besides S_OK so
        // we need to convert them to S_OK.
        hr = S_OK;
        SafeAddRefPreemp(*pptinfo);
    }

    return hr;
}

HRESULT __stdcall
Dispatch_GetIDsOfNames(IDispatch* pDisp, REFIID riid, __in_ecount(cNames) OLECHAR **rgszNames, unsigned int cNames, LCID lcid, DISPID *rgdispid)
{
    CONTRACTL
{
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(return E_OUTOFMEMORY);
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
        PRECONDITION(CheckPointer(rgszNames, NULL_OK));
    }
    CONTRACTL_END;

    // Get the CMT that matches the interface passed in.
    ComMethodTable *pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
    if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
        if (pCMT->HasInvisibleParent())
            return E_NOTIMPL;

    ComCallWrapperTemplate *pTemplate = MapIUnknownToWrapper(pDisp)->GetComCallWrapperTemplate();
    if (pTemplate->IsUseOleAutDispatchImpl())
    {
        return OleAutDispatchImpl_GetIDsOfNames(pDisp, riid, rgszNames, cNames, lcid, rgdispid);
    }

    return InternalDispatchImpl_GetIDsOfNames(pDisp, riid, rgszNames, cNames, lcid, rgdispid);
}

HRESULT __stdcall
Dispatch_Invoke
    (
    IDispatch* pDisp,
    DISPID dispidMember,
    REFIID riid,
    LCID lcid,
    unsigned short wFlags,
    DISPPARAMS *pdispparams,
    VARIANT *pvarResult,
    EXCEPINFO *pexcepinfo,
    unsigned int *puArgErr
    )
{
    CONTRACTL
    {
        THROWS; // InternalDispatchImpl_Invoke can throw if it encounters CE
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(return E_OUTOFMEMORY);
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
    }
    CONTRACTL_END;

    // Get the CMT that matches the interface passed in.
    ComMethodTable *pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
    if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
        if (pCMT->HasInvisibleParent())
            return E_NOTIMPL;

    ComCallWrapperTemplate *pTemplate = MapIUnknownToWrapper(pDisp)->GetComCallWrapperTemplate();
    if (pTemplate->IsUseOleAutDispatchImpl())
    {
        return OleAutDispatchImpl_Invoke(pDisp, dispidMember, riid, lcid, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
    }

    return InternalDispatchImpl_Invoke(pDisp, dispidMember, riid, lcid, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
}


//------------------------------------------------------------------------------------------
//  IDispatch methods for COM+ objects implemented internally using reflection.


HRESULT __stdcall
OleAutDispatchImpl_GetIDsOfNames
(
    IDispatch* pDisp,
    REFIID riid,
    __in_ecount(cNames) OLECHAR **rgszNames,
    unsigned int cNames,
    LCID lcid,
    DISPID *rgdispid
)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        INJECT_FAULT(return E_OUTOFMEMORY);
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
        PRECONDITION(CheckPointer(rgszNames));
    }
    CONTRACTL_END;

    // Make sure that riid is IID_NULL.
    if (riid != IID_NULL)
        return DISP_E_UNKNOWNINTERFACE;

    // Retrieve the COM method table from the IP.
    ComMethodTable *pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
    if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
        if (pCMT->HasInvisibleParent())
            return E_NOTIMPL;

    ITypeInfo *pTI;
    HRESULT hr = GetITypeInfoForMT(pCMT, &pTI);
    if (FAILED(hr))
        return (hr);

    hr = pTI->GetIDsOfNames(rgszNames, cNames, rgdispid);
    return hr;
}

HRESULT __stdcall
OleAutDispatchImpl_Invoke
    (
    IDispatch* pDisp,
    DISPID dispidMember,
    REFIID riid,
    LCID lcid,
    unsigned short wFlags,
    DISPPARAMS *pdispparams,
    VARIANT *pvarResult,
    EXCEPINFO *pexcepinfo,
    unsigned int *puArgErr
    )
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Make sure that riid is IID_NULL.
    if (riid != IID_NULL)
        return DISP_E_UNKNOWNINTERFACE;

    // Retrieve the COM method table from the IP.
    ComMethodTable *pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
    if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
        if (pCMT->HasInvisibleParent())
            return E_NOTIMPL;

    ITypeInfo *pTI;
    hr = GetITypeInfoForMT(pCMT, &pTI);
    if (FAILED(hr))
        return hr;

    EX_TRY
    {
        // If we have a basic or IClassX interface then we're going to invoke through
        //  the class interface.
        if (pCMT->IsIClassXOrBasicItf())
        {
            CCWHolder pCCW = ComCallWrapper::GetWrapperFromIP(pDisp);
            pDisp = (IDispatch*)pCCW->GetIClassXIP();
        }

        hr = pTI->Invoke(pDisp, dispidMember, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}

HRESULT __stdcall
InternalDispatchImpl_GetIDsOfNames (
    IDispatch* pDisp,
    REFIID riid,
    __in_ecount(cNames) OLECHAR **rgszNames,
    unsigned int cNames,
    LCID lcid,
    DISPID *rgdispid)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DispatchInfo *pDispInfo;
    SimpleComCallWrapper *pSimpleWrap;

    // Validate the arguments.
    if (!rgdispid)
        return E_POINTER;

    if (riid != IID_NULL)
        return DISP_E_UNKNOWNINTERFACE;

    if (cNames < 1)
        return S_OK;
    else if (!rgszNames)
        return E_POINTER;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // This call is coming thru an interface that inherits from IDispatch.
        ComCallWrapper* pCCW = ComCallWrapper::GetStartWrapperFromIP(pDisp);

        ComMethodTable* pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
        if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
            pCMT->CheckParentComVisibility(FALSE);

        pSimpleWrap = pCCW->GetSimpleWrapper();
        pDispInfo = ComMethodTable::ComMethodTableFromIP(pDisp)->GetDispatchInfo();

        // Attempt to find the member in the DispatchEx information.
        SString sName(rgszNames[0]);
        DispatchMemberInfo *pDispMemberInfo = pDispInfo->FindMember(sName, FALSE);

        // Check to see if the member has been found.
        if (pDispMemberInfo)
        {
            // Get the DISPID of the member.
            rgdispid[0] = pDispMemberInfo->m_DispID;

            // Get the ID's of the named arguments.
            if (cNames > 1)
                hr = pDispMemberInfo->GetIDsOfParameters(rgszNames + 1, cNames - 1, rgdispid + 1, FALSE);
        }
        else
        {
            rgdispid[0] = DISPID_UNKNOWN;
            hr = DISP_E_UNKNOWNNAME;
        }
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}


HRESULT __stdcall
InternalDispatchImpl_Invoke
    (
    IDispatch* pDisp,
    DISPID dispidMember,
    REFIID riid,
    LCID lcid,
    unsigned short wFlags,
    DISPPARAMS *pdispparams,
    VARIANT *pvarResult,
    EXCEPINFO *pexcepinfo,
    unsigned int *puArgErr
    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsInProcCCWTearOff(pDisp));
    }
    CONTRACTL_END;

    DispatchInfo *pDispInfo;
    SimpleComCallWrapper *pSimpleWrap;
    HRESULT hr = S_OK;

    // Check for valid input args that are not covered by DispatchInfo::InvokeMember.
    if (riid != IID_NULL)
        return DISP_E_UNKNOWNINTERFACE;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // This call is coming thru an interface that inherits form IDispatch.
        ComCallWrapper* pCCW = ComCallWrapper::GetStartWrapperFromIP(pDisp);

        ComMethodTable* pCMT = ComMethodTable::ComMethodTableFromIP(pDisp);
        if (pCMT->IsIClassXOrBasicItf() && pCMT->GetClassInterfaceType() != clsIfNone)
            pCMT->CheckParentComVisibility(FALSE);

        pSimpleWrap = pCCW->GetSimpleWrapper();

        // Invoke the member.
        pDispInfo = ComMethodTable::ComMethodTableFromIP(pDisp)->GetDispatchInfo();
        hr = pDispInfo->InvokeMember(pSimpleWrap, dispidMember, lcid, wFlags, pdispparams, pvarResult, pexcepinfo, NULL, puArgErr);

    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}


//------------------------------------------------------------------------------------------
//      IDispatchEx methods for COM+ objects

// IDispatchEx::GetTypeInfoCount
HRESULT __stdcall   DispatchEx_GetTypeInfoCount(IDispatch* pDisp, unsigned int *pctinfo)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pctinfo, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    ITypeInfo *pTI = NULL;

    // Validate the arguments.
    if (!pctinfo)
        return E_POINTER;

    // Initialize the count of type info's to 0.
    *pctinfo = 0;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

        // Retrieve the class ComMethodTable.
        ComMethodTable *pComMT = ComCallWrapperTemplate::SetupComMethodTableForClass(pSimpleWrap->GetMethodTable(), FALSE);

        // Retrieve the ITypeInfo for the ComMethodTable.
        IfFailThrow(GetITypeInfoForMT(pComMT, &pTI));

        // GetITypeInfoForMT() can return other success codes besides S_OK so
        // we need to convert them to S_OK.
        hr = S_OK;
        *pctinfo = 1;
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::GetTypeInfo
HRESULT __stdcall   DispatchEx_GetTypeInfo (
                                    IDispatch* pDisp,
                                    unsigned int itinfo,
                                    LCID lcid,
                                    ITypeInfo **pptinfo
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pptinfo, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Validate the arguments.
    if (!pptinfo)
        return E_POINTER;

    // Initialize the ITypeInfo pointer to NULL.
    *pptinfo = NULL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

        // Retrieve the class ComMethodTable.
        ComMethodTable *pComMT = ComCallWrapperTemplate::SetupComMethodTableForClass(pSimpleWrap->GetMethodTable(), FALSE);

        // Retrieve the ITypeInfo for the ComMethodTable.
        IfFailThrow(GetITypeInfoForMT(pComMT, pptinfo));

        // GetITypeInfoForMT() can return other success codes besides S_OK so
        // we need to convert them to S_OK.
        hr = S_OK;
        SafeAddRefPreemp(*pptinfo);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::GetIDsofNames
HRESULT __stdcall   DispatchEx_GetIDsOfNames (
                                    IDispatchEx* pDisp,
                                    REFIID riid,
                                    __in_ecount(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(rgdispid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Validate the arguments.
    if (!rgdispid)
        return E_POINTER;

    if (riid != IID_NULL)
        return DISP_E_UNKNOWNINTERFACE;

    if (cNames < 1)
        return S_OK;
    else if (!rgszNames)
        return E_POINTER;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // Attempt to find the member in the DispatchEx information.
        DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();

        SString sName(rgszNames[0]);
        DispatchMemberInfo *pDispMemberInfo = pDispExInfo->SynchFindMember(sName, FALSE);

        // Check to see if the member has been found.
        if (pDispMemberInfo)
        {
            // Get the DISPID of the member.
            rgdispid[0] = pDispMemberInfo->m_DispID;

            // Get the ID's of the named arguments.
            if (cNames > 1)
                hr = pDispMemberInfo->GetIDsOfParameters(rgszNames + 1, cNames - 1, rgdispid + 1, FALSE);
        }
        else
        {
            rgdispid[0] = DISPID_UNKNOWN;
            hr = DISP_E_UNKNOWNNAME;
        }
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::Invoke
HRESULT __stdcall   DispatchEx_Invoke (
                                    IDispatchEx* pDisp,
                                    DISPID dispidMember,
                                    REFIID riid,
                                    LCID lcid,
                                    unsigned short wFlags,
                                    DISPPARAMS *pdispparams,
                                    VARIANT *pvarResult,
                                    EXCEPINFO *pexcepinfo,
                                    unsigned int *puArgErr
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pdispparams, NULL_OK));
        PRECONDITION(CheckPointer(pvarResult, NULL_OK));
        PRECONDITION(CheckPointer(pexcepinfo, NULL_OK));
        PRECONDITION(CheckPointer(puArgErr, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Check for valid input args that are not covered by DispatchInfo::InvokeMember.
    if (riid != IID_NULL)
        return DISP_E_UNKNOWNINTERFACE;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // Invoke the member.
        DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();
        hr = pDispExInfo->SynchInvokeMember(pSimpleWrap, dispidMember, lcid, wFlags, pdispparams, pvarResult, pexcepinfo, NULL, puArgErr);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::DeleteMemberByDispID
HRESULT __stdcall   DispatchEx_DeleteMemberByDispID (
                                    IDispatchEx* pDisp,
                                    DISPID id
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

    DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();

    // If the member does not support expando operations then we cannot remove the member.
    if (!pDispExInfo->SupportsExpando())
        return E_NOTIMPL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // Delete the member from the IExpando. This method takes care of synchronizing with
        // the managed view to make sure the member gets deleted.
        pDispExInfo->DeleteMember(id);
        hr = S_OK;
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::DeleteMemberByName
HRESULT __stdcall   DispatchEx_DeleteMemberByName (
                                    IDispatchEx* pDisp,
                                    BSTR bstrName,
                                    DWORD grfdex
                                    )
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DISPID DispID;

    if (!bstrName)
        return E_POINTER;

    // The only two supported flags are fdexNameCaseSensitive and fdexNameCaseInsensitive.
    if (grfdex & ~(fdexNameCaseSensitive | fdexNameCaseInsensitive))
        return E_INVALIDARG;

    // Ensure both fdexNameCaseSensitive and fdexNameCaseInsensitive aren't both set.
    if ((grfdex & (fdexNameCaseSensitive | fdexNameCaseInsensitive)) == (fdexNameCaseSensitive | fdexNameCaseInsensitive))
        return E_INVALIDARG;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);
    DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();

    // If the member does not support expando operations then we cannot remove the member.
    if (!pDispExInfo->SupportsExpando())
        return E_NOTIMPL;

    // Simply find the associated DISPID and delegate the call to DeleteMemberByDispID.
    hr = DispatchEx_GetDispID(pDisp, bstrName, grfdex, &DispID);
    if (SUCCEEDED(hr))
        hr = DispatchEx_DeleteMemberByDispID(pDisp, DispID);

    return hr;
}

// IDispatchEx::GetDispID
HRESULT __stdcall   DispatchEx_GetDispID (
                                    IDispatchEx* pDisp,
                                    BSTR bstrName,
                                    DWORD grfdex,
                                    DISPID *pid
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(bstrName, NULL_OK));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SimpleComCallWrapper *pSimpleWrap;
    DispatchExInfo *pDispExInfo;

    // Validate the arguments.
    if (!pid || !bstrName)
        return E_POINTER;

    // We don't support fdexNameImplicit, but let the search continue anyway

    // Ensure both fdexNameCaseSensitive and fdexNameCaseInsensitive aren't both set.
    if ((grfdex & (fdexNameCaseSensitive | fdexNameCaseInsensitive)) == (fdexNameCaseSensitive | fdexNameCaseInsensitive))
        return E_INVALIDARG;

    // Initialize the pid to DISPID_UNKNOWN before we start.
    *pid = DISPID_UNKNOWN;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // Attempt to find the member in the DispatchEx information.
        pDispExInfo = pSimpleWrap->GetDispatchExInfo();

        SString sName(bstrName);
        DispatchMemberInfo *pDispMemberInfo = pDispExInfo->SynchFindMember(sName, grfdex & fdexNameCaseSensitive);

        // If we still have not found a match and the fdexNameEnsure flag is set then we
        // need to add the member to the expando object.
        if (!pDispMemberInfo)
        {
            if (grfdex & fdexNameEnsure)
            {
                if (pDispExInfo->SupportsExpando())
                {
                    pDispMemberInfo = pDispExInfo->AddMember(sName, grfdex);
                    if (!pDispMemberInfo)
                        hr = E_UNEXPECTED;
                }
                else
                {
                    hr = E_NOTIMPL;
                }
            }
            else
            {
                hr = DISP_E_UNKNOWNNAME;
            }
        }

        // Set the return DISPID if the member has been found.
        if (pDispMemberInfo)
            *pid = pDispMemberInfo->m_DispID;
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::GetMemberName
HRESULT __stdcall   DispatchEx_GetMemberName (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    BSTR *pbstrName
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pbstrName, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Validate the arguments.
    if (!pbstrName)
        return E_POINTER;

    // Initialize the pbstrName to NULL before we start.
    *pbstrName = NULL;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // Do a lookup in the hashtable to find the DispatchMemberInfo for the DISPID.
        DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();
        DispatchMemberInfo *pDispMemberInfo = pDispExInfo->SynchFindMember(id);

        // If the member does not exist then we return DISP_E_MEMBERNOTFOUND.
        if (!pDispMemberInfo || !pDispMemberInfo->GetMemberInfoObject())
        {
            hr = DISP_E_MEMBERNOTFOUND;
        }
        else
        {
            // Copy the name into the output string.
            *pbstrName = SysAllocString(pDispMemberInfo->m_strName);
        }
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::GetMemberProperties
HRESULT __stdcall   DispatchEx_GetMemberProperties (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    DWORD grfdexFetch,
                                    DWORD *pgrfdex
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pgrfdex, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Validate the arguments.
    if (!pgrfdex)
        return E_POINTER;

    // Initialize the return properties to 0.
    *pgrfdex = 0;

    EnumMemberTypes MemberType;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();
        OBJECTREF MemberInfoObj = NULL;
        GCPROTECT_BEGIN(MemberInfoObj)
        {
            // Do a lookup in the hashtable to find the DispatchMemberInfo for the DISPID.
            DispatchMemberInfo *pDispMemberInfo = pDispExInfo->SynchFindMember(id);

            // If the member does not exist then we return DISP_E_MEMBERNOTFOUND.
            if (!pDispMemberInfo || (MemberInfoObj = pDispMemberInfo->GetMemberInfoObject()) == NULL)
            {
                hr = DISP_E_MEMBERNOTFOUND;
            }
            else
            {
                // Retrieve the type of the member.
                MemberType = pDispMemberInfo->GetMemberType();

                // Retrieve the member properties based on the type of the member.
                switch (MemberType)
                {
                    case Field:
                    {
                        *pgrfdex = fdexPropCanGet |
                                   fdexPropCanPut |
                                   fdexPropCannotPutRef |
                                   fdexPropCannotCall |
                                   fdexPropCannotConstruct |
                                   fdexPropCannotSourceEvents;
                        break;
                    }

                    case Property:
                    {
                        BOOL bCanRead = FALSE;
                        BOOL bCanWrite = FALSE;

                        // Find the MethodDesc's for the CanRead property.
                        MethodDesc *pCanReadMD = MemberLoader::FindPropertyMethod(MemberInfoObj->GetMethodTable(), PROPERTY_INFO_CAN_READ_PROP, PropertyGet);
                        PREFIX_ASSUME_MSG((pCanReadMD != NULL), "Unable to find getter method for property PropertyInfo::CanRead");
                        MethodDescCallSite canRead(pCanReadMD, &MemberInfoObj);

                        // Find the MethodDesc's for the CanWrite property.
                        MethodDesc *pCanWriteMD = MemberLoader::FindPropertyMethod(MemberInfoObj->GetMethodTable(), PROPERTY_INFO_CAN_WRITE_PROP, PropertyGet);
                        PREFIX_ASSUME_MSG((pCanWriteMD != NULL), "Unable to find setter method for property PropertyInfo::CanWrite");
                        MethodDescCallSite canWrite(pCanWriteMD, &MemberInfoObj);

                        // Check to see if the property can be read.
                        ARG_SLOT CanReadArgs[] =
                        {
                            ObjToArgSlot(MemberInfoObj)
                        };

                        bCanRead = canRead.Call_RetBool(CanReadArgs);

                        // Check to see if the property can be written to.
                        ARG_SLOT CanWriteArgs[] =
                        {
                            ObjToArgSlot(MemberInfoObj)
                        };

                        bCanWrite = canWrite.Call_RetBool(CanWriteArgs);

                        *pgrfdex = (bCanRead ? fdexPropCanGet : fdexPropCannotGet) |
                                   (bCanWrite ? fdexPropCanPut : fdexPropCannotPut) |
                                   fdexPropCannotPutRef |
                                   fdexPropCannotCall |
                                   fdexPropCannotConstruct |
                                   fdexPropCannotSourceEvents;
                        break;
                    }

                    case Method:
                    {
                        *pgrfdex = fdexPropCannotGet |
                                   fdexPropCannotPut |
                                   fdexPropCannotPutRef |
                                   fdexPropCanCall |
                                   fdexPropCannotConstruct |
                                   fdexPropCannotSourceEvents;
                        break;
                    }

                    default:
                    {
                        hr = E_UNEXPECTED;
                        break;
                    }
                }

                // Mask out the unwanted properties.
                *pgrfdex &= grfdexFetch;
            }
        }
        GCPROTECT_END();
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// IDispatchEx::GetNameSpaceParent
HRESULT __stdcall   DispatchEx_GetNameSpaceParent (
                                    IDispatchEx* pDisp,
                                    IUnknown **ppunk
                                    )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(ppunk, NULL_OK));
    }
    CONTRACTL_END;

    // Validate the arguments.
    if (!ppunk)
        return E_POINTER;

    // @TODO (DM): Implement this.
    *ppunk = NULL;
    return E_NOTIMPL;
}


// IDispatchEx::GetNextDispID
HRESULT __stdcall   DispatchEx_GetNextDispID (
                                    IDispatchEx* pDisp,
                                    DWORD grfdex,
                                    DISPID id,
                                    DISPID *pid
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pid, NULL_OK));
    }
    CONTRACTL_END;

    DispatchMemberInfo *pNextMember = NULL;
    HRESULT hr = S_OK;

    // Validate the arguments.
    if (!pid)
        return E_POINTER;

    // The only two supported flags are fdexEnumDefault and fdexEnumAll.
    if (grfdex & ~(fdexEnumDefault | fdexEnumAll))
        return E_INVALIDARG;

    // Initialize the pid to DISPID_UNKNOWN.
    *pid = DISPID_UNKNOWN;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();
        // Retrieve either the first member or the next based on the DISPID.
        if (id == DISPID_STARTENUM)
            pNextMember = pDispExInfo->GetFirstMember();
        else
            pNextMember = pDispExInfo->GetNextMember(id);

        // If we have found a member that has not been deleted then return its DISPID.
        if (pNextMember)
        {
            *pid = pNextMember->m_DispID;
            hr = S_OK;
        }
        else
        {
            hr = S_FALSE;
        }
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}


// IDispatchEx::InvokeEx
HRESULT __stdcall   DispatchEx_InvokeEx (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    LCID lcid,
                                    WORD wFlags,
                                    DISPPARAMS *pdp,
                                    VARIANT *pVarRes,
                                    EXCEPINFO *pei,
                                    IServiceProvider *pspCaller
                                    )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(IsSimpleTearOff(pDisp));
        PRECONDITION(CheckPointer(pdp, NULL_OK));
        PRECONDITION(CheckPointer(pVarRes, NULL_OK));
        PRECONDITION(CheckPointer(pei, NULL_OK));
        PRECONDITION(CheckPointer(pspCaller, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Retrieve the dispatch info and the simpler wrapper for this IDispatchEx.
    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pDisp);
    DispatchExInfo *pDispExInfo = pSimpleWrap->GetDispatchExInfo();

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        // Invoke the member.
        hr = pDispExInfo->SynchInvokeMember(pSimpleWrap, id, lcid, wFlags, pdp, pVarRes, pei, pspCaller, NULL);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// Helper to setup IMarshal
HRESULT GetSpecialMarshaler(IMarshal* pMarsh, SimpleComCallWrapper* pSimpleWrap, ULONG dwDestContext, IMarshal **ppMarshalRet)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh, NULL_OK));
        PRECONDITION(CheckPointer(pSimpleWrap));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // In case of CoreCLR, we always use the standard marshaller.

    SafeComHolderPreemp<IUnknown> pMarshalerObj = NULL;
    IfFailRet(CoCreateFreeThreadedMarshaler(NULL, &pMarshalerObj));
    return SafeQueryInterfacePreemp(pMarshalerObj, IID_IMarshal, (IUnknown**)ppMarshalRet);
}


//------------------------------------------------------------------------------------------
//      IMarshal methods for COM+ objects

//------------------------------------------------------------------------------------------

HRESULT __stdcall Marshal_GetUnmarshalClass (
                            IMarshal* pMarsh,
                            REFIID riid, void * pv, ULONG dwDestContext,
                            void * pvDestContext, ULONG mshlflags,
                            LPCLSID pclsid)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(IsSimpleTearOff(pMarsh));
        PRECONDITION(CheckPointer(pv, NULL_OK));
        PRECONDITION(CheckPointer(pvDestContext, NULL_OK));
        PRECONDITION(CheckPointer(pclsid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pMarsh);

    // Prevent access to reflection over DCOM
    if(dwDestContext != MSHCTX_INPROC)
    {
        if(!pSimpleWrap->GetComCallWrapperTemplate()->IsSafeTypeForMarshalling())
        {
            LogInterop(W("Unmarshal class blocked for reflection types."));
            hr = E_NOINTERFACE;
            return hr;
        }
    }

    SafeComHolderPreemp<IMarshal> pMsh = NULL;
    hr = GetSpecialMarshaler(pMarsh, pSimpleWrap, dwDestContext, (IMarshal **)&pMsh);
    if (FAILED(hr))
        return hr;

    return pMsh->GetUnmarshalClass (riid, pv, dwDestContext, pvDestContext, mshlflags, pclsid);
}

HRESULT __stdcall Marshal_GetMarshalSizeMax (
                                IMarshal* pMarsh,
                                REFIID riid, void * pv, ULONG dwDestContext,
                                void * pvDestContext, ULONG mshlflags,
                                ULONG * pSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(IsSimpleTearOff(pMarsh));
        PRECONDITION(CheckPointer(pv, NULL_OK));
        PRECONDITION(CheckPointer(pvDestContext, NULL_OK));
        PRECONDITION(CheckPointer(pSize, NULL_OK));
    }
    CONTRACTL_END;

    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pMarsh);

    SafeComHolderPreemp<IMarshal> pMsh = NULL;
    HRESULT hr = GetSpecialMarshaler(pMarsh, pSimpleWrap, dwDestContext, (IMarshal **)&pMsh);
    if (FAILED(hr))
        return hr;

    return pMsh->GetMarshalSizeMax (riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
}

HRESULT __stdcall Marshal_MarshalInterface (
                        IMarshal* pMarsh,
                        LPSTREAM pStm, REFIID riid, void * pv,
                        ULONG dwDestContext, LPVOID pvDestContext,
                        ULONG mshlflags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(IsSimpleTearOff(pMarsh));
        PRECONDITION(CheckPointer(pv));
        PRECONDITION(CheckPointer(pvDestContext, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pMarsh);

    // Prevent access to reflection over DCOM
    if(dwDestContext != MSHCTX_INPROC)
    {
        if(!pSimpleWrap->GetComCallWrapperTemplate()->IsSafeTypeForMarshalling())
        {
            LogInterop(W("Marshal interface blocked for reflection types."));
            hr = E_NOINTERFACE;
            return hr;
        }
    }

    SafeComHolderPreemp<IMarshal> pMsh = NULL;
    hr = GetSpecialMarshaler(pMarsh, pSimpleWrap, dwDestContext, (IMarshal **)&pMsh);
    if (FAILED(hr))
        return hr;

    return pMsh->MarshalInterface (pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
}

HRESULT __stdcall Marshal_UnmarshalInterface (
                        IMarshal* pMarsh,
                        LPSTREAM pStm, REFIID riid,
                        void ** ppvObj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(IsSimpleTearOff(pMarsh));
        PRECONDITION(CheckPointer(pStm, NULL_OK));
        PRECONDITION(CheckPointer(ppvObj, NULL_OK));
    }
    CONTRACTL_END;

    // Unmarshal side only.
    return E_NOTIMPL;
}

HRESULT __stdcall Marshal_ReleaseMarshalData (IMarshal* pMarsh, LPSTREAM pStm)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(IsSimpleTearOff(pMarsh));
        PRECONDITION(CheckPointer(pStm, NULL_OK));
    }
    CONTRACTL_END;

    // Unmarshal side only.
    return E_NOTIMPL;
}

HRESULT __stdcall Marshal_DisconnectObject (IMarshal* pMarsh, ULONG dwReserved)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(IsSimpleTearOff(pMarsh));
    }
    CONTRACTL_END;

    // Nothing we can (or need to) do here. The client is using a raw IP to
    // access this server, so the server shouldn't go away until the client
    // Release()'s it.
    return S_OK;
}

//------------------------------------------------------------------------------------------
//      IConnectionPointContainer methods for COM+ objects
//------------------------------------------------------------------------------------------

// Enumerate all the connection points supported by the component.
HRESULT __stdcall ConnectionPointContainer_EnumConnectionPoints(IUnknown* pUnk,
                                                                IEnumConnectionPoints **ppEnum)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppEnum, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!ppEnum)
        return E_POINTER;

    *ppEnum = NULL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        _ASSERTE(IsSimpleTearOff(pUnk));
        SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);
        pSimpleWrap->EnumConnectionPoints(ppEnum);
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}

// Find a specific connection point based on the IID of the event interface.
HRESULT __stdcall ConnectionPointContainer_FindConnectionPoint(IUnknown* pUnk,
                                                               REFIID riid,
                                                               IConnectionPoint **ppCP)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppCP, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!ppCP)
        return E_POINTER;

    *ppCP = NULL;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        _ASSERTE(IsSimpleTearOff(pUnk));
        SimpleComCallWrapper *pSimpleWrap = SimpleComCallWrapper::GetWrapperFromIP(pUnk);
        if (!pSimpleWrap->FindConnectionPoint(riid, ppCP))
            hr = CONNECT_E_NOCONNECTION;
    }
    END_EXTERNAL_ENTRYPOINT;

    return hr;
}


//------------------------------------------------------------------------------------------
//      IObjectSafety methods for COM+ objects
//------------------------------------------------------------------------------------------

HRESULT __stdcall ObjectSafety_GetInterfaceSafetyOptions(IUnknown* pUnk,
                                                         REFIID riid,
                                                         DWORD *pdwSupportedOptions,
                                                         DWORD *pdwEnabledOptions)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
        PRECONDITION(CheckPointer(pdwSupportedOptions, NULL_OK));
        PRECONDITION(CheckPointer(pdwEnabledOptions, NULL_OK));
    }
    CONTRACTL_END;

    if (pdwSupportedOptions == NULL || pdwEnabledOptions == NULL)
        return E_POINTER;

    // Make sure the COM+ object implements the requested interface.
    SafeComHolderPreemp<IUnknown> pItf;
    HRESULT hr = SafeQueryInterfacePreemp(pUnk, riid, (IUnknown**)&pItf);
    LogInteropQI(pUnk, riid, hr, "QI to for riid in GetInterfaceSafetyOptions");
    if (SUCCEEDED(hr))
    {
        // We support this interface so set the safety options accordingly
        *pdwSupportedOptions = (INTERFACESAFE_FOR_UNTRUSTED_DATA | INTERFACESAFE_FOR_UNTRUSTED_CALLER);
        *pdwEnabledOptions = (INTERFACESAFE_FOR_UNTRUSTED_DATA | INTERFACESAFE_FOR_UNTRUSTED_CALLER);
        return S_OK;
    }
    else
    {
        // We don't support this interface
        *pdwSupportedOptions = 0;
        *pdwEnabledOptions   = 0;
        return E_NOINTERFACE;
    }
}

HRESULT __stdcall ObjectSafety_SetInterfaceSafetyOptions(IUnknown* pUnk,
                                                         REFIID riid,
                                                         DWORD dwOptionSetMask,
                                                         DWORD dwEnabledOptions)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(IsSimpleTearOff(pUnk));
    }
    CONTRACTL_END;

    // Make sure the COM+ object implements the requested interface.
    SafeComHolderPreemp<IUnknown> pItf;
    HRESULT hr = SafeQueryInterfacePreemp(pUnk, riid, (IUnknown**)&pItf);
    LogInteropQI(pUnk, riid, hr, "QI to for riid in SetInterfaceSafetyOptions");
    if (FAILED(hr))
        return hr;

    if ((dwEnabledOptions & ~(INTERFACESAFE_FOR_UNTRUSTED_DATA | INTERFACESAFE_FOR_UNTRUSTED_CALLER)) != 0)
        return E_FAIL;

    return S_OK;
}
