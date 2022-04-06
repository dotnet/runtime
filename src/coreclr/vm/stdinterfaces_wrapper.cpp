// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//---------------------------------------------------------------------------------
// stdinterfaces_wrapper.cpp
//
// Defines various standard com interfaces

//---------------------------------------------------------------------------------


#include "common.h"

#include <ole2.h>
#include <guidfromname.h>
#include <olectl.h>
#include <objsafe.h>    // IID_IObjectSafe
#include "vars.hpp"
#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "runtimecallablewrapper.h"
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
#include "stdinterfaces.h"
#include "stdinterfaces_internal.h"
#include "interoputil.inl"


interface IEnumConnectionPoints;

// IUnknown is part of IDispatch
// Common vtables for well-known COM interfaces
// shared by all COM+ callable wrappers.

// All Com+ created vtables have well known IUnknown methods, which is used to identify
// the type of the interface
// For e.g. all com+ created tear-offs have the same QI method in their IUnknown portion
//          Unknown_QueryInterface is the QI method for all the tear-offs created from COM+
//
//  Tearoff interfaces created for std. interfaces such as IProvideClassInfo, IErrorInfo etc.
//  have the AddRef & Release function point to Unknown_AddRefSpecial & Unknown_ReleaseSpecial
//
//  Inner unknown, or the original unknown for a wrapper has
//  AddRef & Release point to a Unknown_AddRefInner & Unknown_ReleaseInner

// global inner Unknown vtable
const StdInterfaceDesc<3> g_InnerUnknown =
{
    enum_InnerUnknown,
    {
        (UINT_PTR*)Unknown_QueryInterface,
        (UINT_PTR*)Unknown_AddRefInner,             // special addref to distinguish inner unk
        (UINT_PTR*)Unknown_ReleaseInner,            // special release to distinguish inner unknown
    }
};

// global IProvideClassInfo vtable
const StdInterfaceDesc<4> g_IProvideClassInfo =
{
    enum_IProvideClassInfo,
    {
        (UINT_PTR*)Unknown_QueryInterface,          // don't change this
        (UINT_PTR*)Unknown_AddRefSpecial,           // special addref for std. interface
        (UINT_PTR*)Unknown_ReleaseSpecial,          // special release for std. interface
        (UINT_PTR*)ClassInfo_GetClassInfo_Wrapper   // GetClassInfo
    }
};

// global IMarshal vtable
const StdInterfaceDesc<9> g_IMarshal =
{
    enum_IMarshal,
    {
        (UINT_PTR*)Unknown_QueryInterface,
        (UINT_PTR*)Unknown_AddRefSpecial,
        (UINT_PTR*)Unknown_ReleaseSpecial,
        (UINT_PTR*)Marshal_GetUnmarshalClass_Wrapper,
        (UINT_PTR*)Marshal_GetMarshalSizeMax_Wrapper,
        (UINT_PTR*)Marshal_MarshalInterface_Wrapper,
        (UINT_PTR*)Marshal_UnmarshalInterface_Wrapper,
        (UINT_PTR*)Marshal_ReleaseMarshalData_Wrapper,
        (UINT_PTR*)Marshal_DisconnectObject_Wrapper
    }
};

// global ISupportsErrorInfo vtable
const StdInterfaceDesc<4> g_ISupportsErrorInfo =
{
    enum_ISupportsErrorInfo,
    {
        (UINT_PTR*)Unknown_QueryInterface,
        (UINT_PTR*)Unknown_AddRefSpecial,
        (UINT_PTR*)Unknown_ReleaseSpecial,
        (UINT_PTR*)SupportsErroInfo_IntfSupportsErrorInfo_Wrapper
    }
};

// global IErrorInfo vtable
const StdInterfaceDesc<8> g_IErrorInfo =
{
    enum_IErrorInfo,
    {
        (UINT_PTR*)Unknown_QueryInterface_IErrorInfo,
        (UINT_PTR*)Unknown_AddRefSpecial,
        (UINT_PTR*)Unknown_ReleaseSpecial_IErrorInfo,
        (UINT_PTR*)ErrorInfo_GetGUID_Wrapper,
        (UINT_PTR*)ErrorInfo_GetSource_Wrapper,
        (UINT_PTR*)ErrorInfo_GetDescription_Wrapper,
        (UINT_PTR*)ErrorInfo_GetHelpFile_Wrapper,
        (UINT_PTR*)ErrorInfo_GetHelpContext_Wrapper
    }
};

// global IConnectionPointContainer vtable
const StdInterfaceDesc<5> g_IConnectionPointContainer =
{
    enum_IConnectionPointContainer,
    {
        (UINT_PTR*)Unknown_QueryInterface,
        (UINT_PTR*)Unknown_AddRefSpecial,
        (UINT_PTR*)Unknown_ReleaseSpecial,
        (UINT_PTR*)ConnectionPointContainer_EnumConnectionPoints_Wrapper,
        (UINT_PTR*)ConnectionPointContainer_FindConnectionPoint_Wrapper
    }
};

// global IObjectSafety vtable
const StdInterfaceDesc<5> g_IObjectSafety =
{
    enum_IObjectSafety,
    {
        (UINT_PTR*)Unknown_QueryInterface,
        (UINT_PTR*)Unknown_AddRefSpecial,
        (UINT_PTR*)Unknown_ReleaseSpecial,
        (UINT_PTR*)ObjectSafety_GetInterfaceSafetyOptions_Wrapper,
        (UINT_PTR*)ObjectSafety_SetInterfaceSafetyOptions_Wrapper
    }
};

// global IDispatchEx vtable
const StdInterfaceDesc<15> g_IDispatchEx =
{
    enum_IDispatchEx,
    {
        (UINT_PTR*)Unknown_QueryInterface,
        (UINT_PTR*)Unknown_AddRefSpecial,
        (UINT_PTR*)Unknown_ReleaseSpecial,
        (UINT_PTR*)DispatchEx_GetTypeInfoCount_Wrapper,
        (UINT_PTR*)DispatchEx_GetTypeInfo_Wrapper,
        (UINT_PTR*)DispatchEx_GetIDsOfNames_Wrapper,
        (UINT_PTR*)DispatchEx_Invoke_Wrapper,
        (UINT_PTR*)DispatchEx_GetDispID_Wrapper,
        (UINT_PTR*)DispatchEx_InvokeEx_Wrapper,
        (UINT_PTR*)DispatchEx_DeleteMemberByName_Wrapper,
        (UINT_PTR*)DispatchEx_DeleteMemberByDispID_Wrapper,
        (UINT_PTR*)DispatchEx_GetMemberProperties_Wrapper,
        (UINT_PTR*)DispatchEx_GetMemberName_Wrapper,
        (UINT_PTR*)DispatchEx_GetNextDispID_Wrapper,
        (UINT_PTR*)DispatchEx_GetNameSpaceParent_Wrapper
    }
};

// global IAgileObject vtable
const StdInterfaceDesc<3> g_IAgileObject =
{
    enum_IAgileObject,
    {
        (UINT_PTR*)Unknown_QueryInterface,
        (UINT_PTR*)Unknown_AddRefSpecial,
        (UINT_PTR*)Unknown_ReleaseSpecial
    }
};

// Generic helper to check if AppDomain matches and perform a DoCallBack otherwise
inline BOOL IsCurrentDomainValid(ComCallWrapper* pWrap, Thread* pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    _ASSERTE(pWrap != NULL);
    PREFIX_ASSUME(pWrap != NULL);

    // If we are finalizing all alive objects, or after this stage, we do not allow
    // a thread to enter EE.
    if ((g_fEEShutDown & ShutDown_Finalize2) || g_fForbidEnterEE)
        return FALSE;

    return TRUE;
}

BOOL IsCurrentDomainValid(ComCallWrapper* pWrap)
{
    CONTRACTL { NOTHROW; GC_TRIGGERS; MODE_ANY; } CONTRACTL_END;

    return IsCurrentDomainValid(pWrap, GetThread());
}

struct AppDomainSwitchToPreemptiveHelperArgs
{
    ADCallBackFcnType pRealCallback;
    void* pRealArgs;
};

VOID __stdcall AppDomainSwitchToPreemptiveHelper(LPVOID pv)
{
    AppDomainSwitchToPreemptiveHelperArgs* pArgs = (AppDomainSwitchToPreemptiveHelperArgs*)pv;

    CONTRACTL
    {
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pv));

        VOID __stdcall Dispatch_Invoke_CallBack(LPVOID ptr);
        if (pArgs->pRealCallback == Dispatch_Invoke_CallBack) THROWS; else NOTHROW;
    }
    CONTRACTL_END;

    GCX_PREEMP();
    pArgs->pRealCallback(pArgs->pRealArgs);
}

VOID AppDomainDoCallBack(ComCallWrapper* pWrap, ADCallBackFcnType pTarget, LPVOID pArgs, HRESULT* phr)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pWrap));
        PRECONDITION(CheckPointer(pTarget));
        PRECONDITION(CheckPointer(pArgs));
        PRECONDITION(CheckPointer(phr));
    }
    CONTRACTL_END;

    // If we are finalizing all alive objects, or after this stage, we do not allow
    // a thread to enter EE.
    if ((g_fEEShutDown & ShutDown_Finalize2) || g_fForbidEnterEE)
    {
        *phr = E_FAIL;
        return;
    }

    BEGIN_EXTERNAL_ENTRYPOINT(phr)
    {
        // make the call directly not forgetting to switch to preemptive GC mode
        GCX_PREEMP();
        ((ADCallBackFcnType)pTarget)(pArgs);
    }
    END_EXTERNAL_ENTRYPOINT;
}

//-------------------------------------------------------------------------
// IUnknown methods

struct QIArgs
{
    ComCallWrapper* pWrap;
    IUnknown* pUnk;
    const IID* riid;
    void**  ppv;
    HRESULT* hr;
};

VOID __stdcall Unknown_QueryInterface_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    QIArgs* pArgs = (QIArgs*)ptr;

    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Unknown_QueryInterface_Internal(pArgs->pWrap, pArgs->pUnk, *pArgs->riid, pArgs->ppv);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Unknown_QueryInterface_CallBack, pArgs, pArgs->hr);;
    }
}

HRESULT __stdcall Unknown_QueryInterface(IUnknown* pUnk, REFIID riid, void** ppv)
{
    SetupThreadForComCall(E_OUTOFMEMORY);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppv, NULL_OK));
    }
    CONTRACTL_END;

    ComCallWrapper* pWrap = MapIUnknownToWrapper(pUnk);
    if (IsCurrentDomainValid(pWrap, GET_THREAD()))
    {
        return Unknown_QueryInterface_Internal(pWrap, pUnk, riid, ppv);
    }
    else
    {
        HRESULT hr = S_OK;
        QIArgs args = {pWrap, pUnk, &riid, ppv, &hr};
        Unknown_QueryInterface_CallBack(&args);
        return hr;
    }
}

struct AddRefReleaseArgs
{
    IUnknown* pUnk;
    ULONG* pLong;
    HRESULT* hr;
};

ULONG __stdcall Unknown_AddRef(IUnknown* pUnk)
{
    // Ensure the Thread is available for contracts and other users of the Thread, but don't do any of
    // the other "entering managed code" work like checking for reentrancy.
    // We don't really need to "enter" the runtime to do an interlocked increment on a refcount, so
    // all of that stuff should be isolated to rare paths here.
    SetupThreadForComCall(-1);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // Allow addrefs to go through, coz we are allowing
    // all releases to go through, otherwise we would
    // have a mismatch of ref-counts
    return Unknown_AddRef_Internal(pUnk);
}

ULONG __stdcall Unknown_Release(IUnknown* pUnk)
{
    // Ensure the Thread is available for contracts and other users of the Thread, but don't do any of
    // the other "entering managed code" work like checking for reentrancy.
    // We don't really need to "enter" the runtime to do an interlocked decrement on a refcount, so
    // all of that stuff should be isolated to rare paths here.
    SetupThreadForComCall(-1);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // Don't switch domains since we need to allow release calls to go through
    // even after the AD has been unlaoded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_Release_Internal(pUnk);
}

ULONG __stdcall Unknown_AddRefInner(IUnknown* pUnk)
{
    // Ensure the Thread is available for contracts and other users of the Thread, but don't do any of
    // the other "entering managed code" work like checking for reentrancy.
    // We don't really need to "enter" the runtime to do an interlocked increment on a refcount, so
    // all of that stuff should be isolated to rare paths here.
    SetupThreadForComCall(-1);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // Allow addrefs to go through, coz we are allowing
    // all releases to go through, otherwise we would
    // have a mismatch of ref-counts
    return Unknown_AddRefInner_Internal(pUnk);
}

ULONG __stdcall Unknown_ReleaseInner(IUnknown* pUnk)
{
    // Ensure the Thread is available for contracts and other users of the Thread, but don't do any of
    // the other "entering managed code" work like checking for reentrancy.
    // We don't really need to "enter" the runtime to do an interlocked decrement on a refcount, so
    // all of that stuff should be isolated to rare paths here.
    SetupThreadForComCall(-1);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // Don't switch domains since we need to allow release calls to go through
    // even after the AD has been unlaoded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_ReleaseInner_Internal(pUnk);
}

ULONG __stdcall Unknown_AddRefSpecial(IUnknown* pUnk)
{
    // Ensure the Thread is available for contracts and other users of the Thread, but don't do any of
    // the other "entering managed code" work like checking for reentrancy.
    // We don't really need to "enter" the runtime to do an interlocked increment on a refcount, so
    // all of that stuff should be isolated to rare paths here.
    SetupThreadForComCall(-1);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // Allow addrefs to go through, coz we are allowing
    // all releases to go through, otherwise we would
    // have a mismatch of ref-counts
    return Unknown_AddRefSpecial_Internal(pUnk);
}

ULONG __stdcall Unknown_ReleaseSpecial(IUnknown* pUnk)
{
    // Ensure the Thread is available for contracts and other users of the Thread, but don't do any of
    // the other "entering managed code" work like checking for reentrancy.
    // We don't really need to "enter" the runtime to do an interlocked decrement on a refcount, so
    // all of that stuff should be isolated to rare paths here.
    SetupThreadForComCall(-1);

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // Don't switch domains since we need to allow release calls to go through
    // even after the AD has been unlaoded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_ReleaseSpecial_Internal(pUnk);
}

HRESULT __stdcall Unknown_QueryInterface_IErrorInfo(IUnknown* pUnk, REFIID riid, void** ppv)
{
    SetupForComCallHR();

    WRAPPER_NO_CONTRACT;

    // otherwise do a regular QI
    return Unknown_QueryInterface(pUnk, riid, ppv);
}

// ---------------------------------------------------------------------------
// Release for IErrorInfo that takes into account that this can be called
// while holding the loader lock
// ---------------------------------------------------------------------------
ULONG __stdcall Unknown_ReleaseSpecial_IErrorInfo(IUnknown* pUnk)
{
    SetupForComCallDWORD();

    WRAPPER_NO_CONTRACT;

    CONTRACT_VIOLATION(GCViolation);

    // Don't switch domains since we need to allow release calls to go through
    // even after the AD has been unlaoded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_ReleaseSpecial_IErrorInfo_Internal(pUnk);
}


//-------------------------------------------------------------------------
// IProvideClassInfo methods

struct GetClassInfoArgs
{
    IUnknown* pUnk;
    ITypeInfo** ppTI; //Address of output variable that receives the type info.
    HRESULT* hr;
};

VOID __stdcall ClassInfo_GetClassInfo_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetClassInfoArgs* pArgs = (GetClassInfoArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);

    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ClassInfo_GetClassInfo(pArgs->pUnk, pArgs->ppTI);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ClassInfo_GetClassInfo_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall ClassInfo_GetClassInfo_Wrapper(IUnknown* pUnk, ITypeInfo** ppTI)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppTI, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetClassInfoArgs args = {pUnk, ppTI, &hr};
    ClassInfo_GetClassInfo_CallBack(&args);
    return hr;
}


// ---------------------------------------------------------------------------
//  Interface ISupportsErrorInfo

struct IntfSupportsErrorInfoArgs
{
    IUnknown* pUnk;
    const IID* riid;
    HRESULT* hr;
};

VOID __stdcall SupportsErroInfo_IntfSupportsErrorInfo_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    IntfSupportsErrorInfoArgs* pArgs = (IntfSupportsErrorInfoArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = SupportsErroInfo_IntfSupportsErrorInfo(pArgs->pUnk, *pArgs->riid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, SupportsErroInfo_IntfSupportsErrorInfo_CallBack, pArgs, pArgs->hr);;
    }
}

HRESULT __stdcall
SupportsErroInfo_IntfSupportsErrorInfo_Wrapper(IUnknown* pUnk, REFIID riid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    IntfSupportsErrorInfoArgs args = {pUnk, &riid, &hr};
    SupportsErroInfo_IntfSupportsErrorInfo_CallBack(&args);
    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IErrorInfo

struct GetDescriptionArgs
{
    IUnknown* pUnk;
    BSTR*   pbstDescription;
    HRESULT* hr;
};

VOID __stdcall ErrorInfo_GetDescription_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetDescriptionArgs* pArgs = (GetDescriptionArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ErrorInfo_GetDescription(pArgs->pUnk, pArgs->pbstDescription);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ErrorInfo_GetDescription_CallBack, pArgs, pArgs->hr);;
    }
}

HRESULT __stdcall ErrorInfo_GetDescription_Wrapper(IUnknown* pUnk, BSTR* pbstrDescription)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pbstrDescription, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetDescriptionArgs args = {pUnk, pbstrDescription, &hr};
    ErrorInfo_GetDescription_CallBack(&args);
    return hr;
}

struct GetGUIDArgs
{
    IUnknown* pUnk;
    GUID* pguid;
    HRESULT* hr;
};

VOID __stdcall ErrorInfo_GetGUID_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetGUIDArgs* pArgs = (GetGUIDArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ErrorInfo_GetGUID(pArgs->pUnk, pArgs->pguid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ErrorInfo_GetGUID_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall ErrorInfo_GetGUID_Wrapper(IUnknown* pUnk, GUID* pguid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pguid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetGUIDArgs args = {pUnk, pguid, &hr};
    ErrorInfo_GetGUID_CallBack(&args);
    return hr;
}

struct GetHelpContextArgs
{
    IUnknown* pUnk;
    DWORD* pdwHelpCtxt;
    HRESULT* hr;
};

VOID _stdcall ErrorInfo_GetHelpContext_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetHelpContextArgs* pArgs = (GetHelpContextArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ErrorInfo_GetHelpContext(pArgs->pUnk, pArgs->pdwHelpCtxt);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ErrorInfo_GetHelpContext_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT _stdcall ErrorInfo_GetHelpContext_Wrapper(IUnknown* pUnk, DWORD* pdwHelpCtxt)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pdwHelpCtxt, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetHelpContextArgs args = {pUnk, pdwHelpCtxt, &hr};
    ErrorInfo_GetHelpContext_CallBack(&args);
    return hr;
}

struct GetHelpFileArgs
{
    IUnknown* pUnk;
    BSTR* pbstrHelpFile;
    HRESULT* hr;
};

VOID __stdcall ErrorInfo_GetHelpFile_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetHelpFileArgs* pArgs = (GetHelpFileArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ErrorInfo_GetHelpFile(pArgs->pUnk, pArgs->pbstrHelpFile);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ErrorInfo_GetHelpFile_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall ErrorInfo_GetHelpFile_Wrapper(IUnknown* pUnk, BSTR* pbstrHelpFile)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pbstrHelpFile, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetHelpFileArgs args = {pUnk, pbstrHelpFile, &hr};
    ErrorInfo_GetHelpFile_CallBack(&args);
    return hr;
}

struct GetSourceArgs
{
    IUnknown* pUnk;
    BSTR* pbstrSource;
    HRESULT* hr;
};

VOID __stdcall ErrorInfo_GetSource_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetSourceArgs* pArgs = (GetSourceArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ErrorInfo_GetSource(pArgs->pUnk, pArgs->pbstrSource);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ErrorInfo_GetSource_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall ErrorInfo_GetSource_Wrapper(IUnknown* pUnk, BSTR* pbstrSource)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pbstrSource, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetSourceArgs args = {pUnk, pbstrSource, &hr};
    ErrorInfo_GetSource_CallBack(&args);
    return hr;
}


// ---------------------------------------------------------------------------
//  Interface IDispatch
//
//      IDispatch methods for COM+ objects. These methods dispatch's to the
//      appropriate implementation based on the flags of the class that
//      implements them.

struct GetTypeInfoCountArgs
{
    IDispatch* pUnk;
    unsigned int *pctinfo;
    HRESULT* hr;
};

VOID __stdcall Dispatch_GetTypeInfoCount_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetTypeInfoCountArgs* pArgs = (GetTypeInfoCountArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Dispatch_GetTypeInfoCount(pArgs->pUnk, pArgs->pctinfo);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Dispatch_GetTypeInfoCount_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Dispatch_GetTypeInfoCount_Wrapper(IDispatch* pDisp, unsigned int *pctinfo)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pctinfo, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetTypeInfoCountArgs args = {pDisp, pctinfo, &hr};
    Dispatch_GetTypeInfoCount_CallBack(&args);
    return hr;
}

struct GetTypeInfoArgs
{
    IDispatch* pUnk;
    unsigned int itinfo;
    LCID lcid;
    ITypeInfo **pptinfo;
    HRESULT* hr;
};

VOID __stdcall Dispatch_GetTypeInfo_CallBack (LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetTypeInfoArgs* pArgs = (GetTypeInfoArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Dispatch_GetTypeInfo(pArgs->pUnk, pArgs->itinfo, pArgs->lcid, pArgs->pptinfo);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Dispatch_GetTypeInfo_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Dispatch_GetTypeInfo_Wrapper(IDispatch* pDisp, unsigned int itinfo, LCID lcid, ITypeInfo **pptinfo)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pptinfo, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetTypeInfoArgs args = {pDisp, itinfo, lcid, pptinfo, &hr};
    Dispatch_GetTypeInfo_CallBack(&args);
    return hr;
}

struct GetIDsOfNamesArgs
{
    IDispatch* pUnk;
    const IID* riid;
    OLECHAR **rgszNames;
    unsigned int cNames;
    LCID lcid;
    DISPID *rgdispid;
    HRESULT* hr;
};

VOID __stdcall Dispatch_GetIDsOfNames_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetIDsOfNamesArgs* pArgs = (GetIDsOfNamesArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Dispatch_GetIDsOfNames(pArgs->pUnk, *pArgs->riid, pArgs->rgszNames,
                                    pArgs->cNames, pArgs->lcid, pArgs->rgdispid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Dispatch_GetIDsOfNames_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Dispatch_GetIDsOfNames_Wrapper(IDispatch* pDisp, REFIID riid, _In_reads_(cNames) OLECHAR **rgszNames,
                               unsigned int cNames, LCID lcid, DISPID *rgdispid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(rgszNames, NULL_OK));
        PRECONDITION(CheckPointer(rgdispid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetIDsOfNamesArgs args = {pDisp, &riid, rgszNames, cNames, lcid, rgdispid, &hr};
    Dispatch_GetIDsOfNames_CallBack(&args);
    return hr;
}

VOID __stdcall InternalDispatchImpl_GetIDsOfNames_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetIDsOfNamesArgs* pArgs = (GetIDsOfNamesArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = InternalDispatchImpl_GetIDsOfNames(pArgs->pUnk, *pArgs->riid, pArgs->rgszNames,
                                                          pArgs->cNames, pArgs->lcid, pArgs->rgdispid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, InternalDispatchImpl_GetIDsOfNames_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall InternalDispatchImpl_GetIDsOfNames_Wrapper(IDispatch* pDisp, REFIID riid, _In_reads_(cNames) OLECHAR **rgszNames,
                                           unsigned int cNames, LCID lcid, DISPID *rgdispid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(rgszNames, NULL_OK));
        PRECONDITION(CheckPointer(rgdispid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetIDsOfNamesArgs args = {pDisp, &riid, rgszNames, cNames, lcid, rgdispid, &hr};
    InternalDispatchImpl_GetIDsOfNames_CallBack(&args);
    return hr;
}

struct InvokeArgs
{
    IDispatch* pUnk;
    DISPID dispidMember;
    const IID* riid;
    LCID lcid;
    unsigned short wFlags;
    DISPPARAMS *pdispparams;
    VARIANT *pvarResult;
    EXCEPINFO *pexcepinfo;
    unsigned int *puArgErr;
    HRESULT* hr;
};

VOID __stdcall Dispatch_Invoke_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        THROWS; // Dispatch_Invoke can throw
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    InvokeArgs* pArgs = (InvokeArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Dispatch_Invoke(pArgs->pUnk, pArgs->dispidMember, *pArgs->riid,
                                    pArgs->lcid, pArgs->wFlags, pArgs->pdispparams, pArgs->pvarResult,
                                    pArgs->pexcepinfo, pArgs->puArgErr);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Dispatch_Invoke_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Dispatch_Invoke_Wrapper(IDispatch* pDisp, DISPID dispidMember, REFIID riid, LCID lcid, unsigned short wFlags,
                        DISPPARAMS *pdispparams, VARIANT *pvarResult, EXCEPINFO *pexcepinfo, unsigned int *puArgErr)
{
    HRESULT hrRetVal = S_OK;

    SetupForComCallHR();

    CONTRACTL
    {
        THROWS; // Dispatch_Invoke_CallBack can throw
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pdispparams, NULL_OK));
        PRECONDITION(CheckPointer(pvarResult, NULL_OK));
        PRECONDITION(CheckPointer(pexcepinfo, NULL_OK));
        PRECONDITION(CheckPointer(puArgErr, NULL_OK));
    }
    CONTRACTL_END;

    InvokeArgs args = {pDisp, dispidMember, &riid, lcid, wFlags, pdispparams,
                       pvarResult, pexcepinfo, puArgErr, &hrRetVal};
    Dispatch_Invoke_CallBack(&args);

    return hrRetVal;
}

VOID __stdcall InternalDispatchImpl_Invoke_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    InvokeArgs* pArgs = (InvokeArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = InternalDispatchImpl_Invoke(pArgs->pUnk, pArgs->dispidMember, *pArgs->riid,
                                    pArgs->lcid, pArgs->wFlags, pArgs->pdispparams, pArgs->pvarResult,
                                    pArgs->pexcepinfo, pArgs->puArgErr);
    }
    else
    {
        AppDomainDoCallBack(pWrap, InternalDispatchImpl_Invoke_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall InternalDispatchImpl_Invoke_Wrapper(IDispatch* pDisp, DISPID dispidMember, REFIID riid, LCID lcid,
                                    unsigned short wFlags, DISPPARAMS *pdispparams, VARIANT *pvarResult,
                                    EXCEPINFO *pexcepinfo, unsigned int *puArgErr)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pdispparams, NULL_OK));
        PRECONDITION(CheckPointer(pvarResult, NULL_OK));
        PRECONDITION(CheckPointer(pexcepinfo, NULL_OK));
        PRECONDITION(CheckPointer(puArgErr, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    InvokeArgs args = {pDisp, dispidMember, &riid, lcid, wFlags, pdispparams,
                       pvarResult, pexcepinfo, puArgErr, &hr};
    InternalDispatchImpl_Invoke_CallBack(&args);
    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IDispatchEx

struct GetTypeInfoCountExArgs
{
    IDispatchEx* pUnk;
    unsigned int *pctinfo;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetTypeInfoCount_CallBack (LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetTypeInfoCountExArgs* pArgs = (GetTypeInfoCountExArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetTypeInfoCount(pArgs->pUnk, pArgs->pctinfo);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetTypeInfoCount_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetTypeInfoCount_Wrapper(IDispatchEx* pDisp, unsigned int *pctinfo)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pctinfo, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetTypeInfoCountExArgs args = {pDisp, pctinfo, &hr};
    DispatchEx_GetTypeInfoCount_CallBack(&args);
    return hr;
}

struct GetTypeInfoExArgs
{
    IDispatch* pUnk;
    unsigned int itinfo;
    LCID lcid;
    ITypeInfo **pptinfo;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetTypeInfo_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetTypeInfoExArgs* pArgs = (GetTypeInfoExArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetTypeInfo(pArgs->pUnk, pArgs->itinfo, pArgs->lcid, pArgs->pptinfo);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetTypeInfo_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetTypeInfo_Wrapper(IDispatchEx* pDisp, unsigned int itinfo, LCID lcid, ITypeInfo **pptinfo)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pptinfo, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetTypeInfoExArgs args = {pDisp, itinfo, lcid, pptinfo, &hr};
    DispatchEx_GetTypeInfo_CallBack(&args);
    return hr;
}

struct GetIDsOfNamesExArgs
{
    IDispatchEx* pUnk;
    const IID* riid;
    OLECHAR **rgszNames;
    unsigned int cNames;
    LCID lcid;
    DISPID *rgdispid;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetIDsOfNames_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetIDsOfNamesExArgs* pArgs = (GetIDsOfNamesExArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetIDsOfNames(pArgs->pUnk, *pArgs->riid, pArgs->rgszNames,
                                    pArgs->cNames, pArgs->lcid, pArgs->rgdispid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetIDsOfNames_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetIDsOfNames_Wrapper(IDispatchEx* pDisp, REFIID riid, _In_reads_(cNames) OLECHAR **rgszNames,
                                 unsigned int cNames, LCID lcid, DISPID *rgdispid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(rgszNames, NULL_OK));
        PRECONDITION(CheckPointer(rgdispid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetIDsOfNamesExArgs args = {pDisp, &riid, rgszNames, cNames, lcid, rgdispid, &hr};
    DispatchEx_GetIDsOfNames_CallBack(&args);
    return hr;
}

struct DispExInvokeArgs
{
    IDispatchEx* pUnk;
    DISPID dispidMember;
    const IID* riid;
    LCID lcid;
    unsigned short wFlags;
    DISPPARAMS *pdispparams;
    VARIANT *pvarResult;
    EXCEPINFO *pexcepinfo;
    unsigned int *puArgErr;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_Invoke_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    DispExInvokeArgs* pArgs = (DispExInvokeArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_Invoke(pArgs->pUnk, pArgs->dispidMember, *pArgs->riid,
                                    pArgs->lcid, pArgs->wFlags, pArgs->pdispparams, pArgs->pvarResult,
                                    pArgs->pexcepinfo, pArgs->puArgErr);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_Invoke_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_Invoke_Wrapper(IDispatchEx* pDisp, DISPID dispidMember, REFIID riid, LCID lcid,
                          unsigned short wFlags, DISPPARAMS *pdispparams, VARIANT *pvarResult,
                          EXCEPINFO *pexcepinfo, unsigned int *puArgErr)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pdispparams, NULL_OK));
        PRECONDITION(CheckPointer(pvarResult, NULL_OK));
        PRECONDITION(CheckPointer(pexcepinfo, NULL_OK));
        PRECONDITION(CheckPointer(puArgErr, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DispExInvokeArgs args = {pDisp, dispidMember, &riid, lcid, wFlags, pdispparams,
                                pvarResult, pexcepinfo, puArgErr, &hr};
    DispatchEx_Invoke_CallBack(&args);
    return hr;
}

struct DeleteMemberByDispIDArgs
{
    IDispatchEx* pDisp;
    DISPID id;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_DeleteMemberByDispID_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    DeleteMemberByDispIDArgs* pArgs = (DeleteMemberByDispIDArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_DeleteMemberByDispID(pArgs->pDisp, pArgs->id);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_DeleteMemberByDispID_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_DeleteMemberByDispID_Wrapper(IDispatchEx* pDisp, DISPID id)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DeleteMemberByDispIDArgs args = {pDisp, id, &hr};
    DispatchEx_DeleteMemberByDispID_CallBack(&args);
    return hr;
}

struct DeleteMemberByNameArgs
{
    IDispatchEx* pDisp;
    BSTR bstrName;
    DWORD grfdex;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_DeleteMemberByName_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    DeleteMemberByNameArgs* pArgs = (DeleteMemberByNameArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_DeleteMemberByName(pArgs->pDisp, pArgs->bstrName, pArgs->grfdex);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_DeleteMemberByName_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_DeleteMemberByName_Wrapper(IDispatchEx* pDisp, BSTR bstrName, DWORD grfdex)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DeleteMemberByNameArgs args = {pDisp, bstrName, grfdex, &hr};
    DispatchEx_DeleteMemberByName_CallBack(&args);
    return hr;
}

struct GetMemberNameArgs
{
    IDispatchEx* pDisp;
    DISPID id;
    BSTR *pbstrName;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetMemberName_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetMemberNameArgs* pArgs = (GetMemberNameArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetMemberName(pArgs->pDisp, pArgs->id, pArgs->pbstrName);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetMemberName_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetMemberName_Wrapper(IDispatchEx* pDisp, DISPID id, BSTR *pbstrName)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pbstrName, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetMemberNameArgs args = {pDisp, id, pbstrName, &hr};
    DispatchEx_GetMemberName_CallBack(&args);
    return hr;
}

struct GetDispIDArgs
{
    IDispatchEx* pDisp;
    BSTR bstrName;
    DWORD grfdex;
    DISPID *pid;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetDispID_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetDispIDArgs* pArgs = (GetDispIDArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetDispID(pArgs->pDisp, pArgs->bstrName, pArgs->grfdex, pArgs->pid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetDispID_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetDispID_Wrapper(IDispatchEx* pDisp, BSTR bstrName, DWORD grfdex, DISPID *pid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetDispIDArgs args = {pDisp, bstrName, grfdex, pid, &hr};
    DispatchEx_GetDispID_CallBack(&args);
    return hr;
}

struct GetMemberPropertiesArgs
{
    IDispatchEx* pDisp;
    DISPID id;
    DWORD grfdexFetch;
    DWORD *pgrfdex;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetMemberProperties_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetMemberPropertiesArgs* pArgs = (GetMemberPropertiesArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetMemberProperties(pArgs->pDisp, pArgs->id, pArgs->grfdexFetch,
                                    pArgs->pgrfdex);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetMemberProperties_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetMemberProperties_Wrapper(IDispatchEx* pDisp, DISPID id, DWORD grfdexFetch, DWORD *pgrfdex)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pgrfdex, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetMemberPropertiesArgs args = {pDisp, id, grfdexFetch, pgrfdex, &hr};
    DispatchEx_GetMemberProperties_CallBack(&args);
    return hr;
}

struct GetNameSpaceParentArgs
{
    IDispatchEx* pDisp;
    IUnknown **ppunk;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetNameSpaceParent_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetNameSpaceParentArgs* pArgs = (GetNameSpaceParentArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetNameSpaceParent(pArgs->pDisp, pArgs->ppunk);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetNameSpaceParent_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetNameSpaceParent_Wrapper(IDispatchEx* pDisp, IUnknown **ppunk)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(ppunk, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetNameSpaceParentArgs args = {pDisp, ppunk, &hr};
    DispatchEx_GetNameSpaceParent_CallBack(&args);
    return hr;
}

struct GetNextDispIDArgs
{
    IDispatchEx* pDisp;
    DWORD grfdex;
    DISPID id;
    DISPID *pid;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_GetNextDispID_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetNextDispIDArgs* pArgs = (GetNextDispIDArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_GetNextDispID(pArgs->pDisp, pArgs->grfdex, pArgs->id, pArgs->pid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_GetNextDispID_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_GetNextDispID_Wrapper(IDispatchEx* pDisp, DWORD grfdex, DISPID id, DISPID *pid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetNextDispIDArgs args = {pDisp, grfdex, id, pid, &hr};
    DispatchEx_GetNextDispID_CallBack(&args);
    return hr;
}

struct DispExInvokeExArgs
{
    IDispatchEx* pDisp;
    DISPID id;
    LCID lcid;
    WORD wFlags;
    DISPPARAMS *pdp;
    VARIANT *pVarRes;
    EXCEPINFO *pei;
    IServiceProvider *pspCaller;
    HRESULT* hr;
};

VOID __stdcall DispatchEx_InvokeEx_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    DispExInvokeExArgs* pArgs = (DispExInvokeExArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pDisp);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = DispatchEx_InvokeEx(pArgs->pDisp, pArgs->id,
                                    pArgs->lcid, pArgs->wFlags, pArgs->pdp, pArgs->pVarRes,
                                    pArgs->pei, pArgs->pspCaller);
    }
    else
    {
        AppDomainDoCallBack(pWrap, DispatchEx_InvokeEx_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall DispatchEx_InvokeEx_Wrapper(IDispatchEx* pDisp, DISPID id, LCID lcid, WORD wFlags, DISPPARAMS *pdp,
                            VARIANT *pVarRes, EXCEPINFO *pei, IServiceProvider *pspCaller)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pDisp));
        PRECONDITION(CheckPointer(pdp, NULL_OK));
        PRECONDITION(CheckPointer(pVarRes, NULL_OK));
        PRECONDITION(CheckPointer(pei, NULL_OK));
        PRECONDITION(CheckPointer(pspCaller, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DispExInvokeExArgs args = {pDisp, id, lcid, wFlags, pdp, pVarRes, pei, pspCaller, &hr};
    DispatchEx_InvokeEx_CallBack(&args);
    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IMarshal

struct GetUnmarshalClassArgs
{
    IMarshal* pUnk;
    const IID* riid;
    void * pv;
    ULONG dwDestContext;
    void * pvDestContext;
    ULONG mshlflags;
    LPCLSID pclsid;
    HRESULT* hr;

};

VOID __stdcall Marshal_GetUnmarshalClass_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetUnmarshalClassArgs* pArgs = (GetUnmarshalClassArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Marshal_GetUnmarshalClass(pArgs->pUnk, *(pArgs->riid), pArgs->pv,
                                    pArgs->dwDestContext, pArgs->pvDestContext, pArgs->mshlflags,
                                    pArgs->pclsid);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Marshal_GetUnmarshalClass_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Marshal_GetUnmarshalClass_Wrapper(IMarshal* pMarsh, REFIID riid, void * pv, ULONG dwDestContext,
                                  void * pvDestContext, ULONG mshlflags, LPCLSID pclsid)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(CheckPointer(pv, NULL_OK));
        PRECONDITION(CheckPointer(pvDestContext, NULL_OK));
        PRECONDITION(CheckPointer(pclsid, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetUnmarshalClassArgs args = {pMarsh, &riid, pv, dwDestContext, pvDestContext,
                                        mshlflags, pclsid, &hr};
    Marshal_GetUnmarshalClass_CallBack(&args);
    return hr;
}

struct GetMarshalSizeMaxArgs
{
    IMarshal* pUnk;
    const IID* riid;
    void * pv;
    ULONG dwDestContext;
    void * pvDestContext;
    ULONG mshlflags;
    ULONG * pSize;
    HRESULT* hr;

};

VOID __stdcall Marshal_GetMarshalSizeMax_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetMarshalSizeMaxArgs* pArgs = (GetMarshalSizeMaxArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Marshal_GetMarshalSizeMax(pArgs->pUnk, *(pArgs->riid), pArgs->pv,
                                    pArgs->dwDestContext, pArgs->pvDestContext, pArgs->mshlflags,
                                    pArgs->pSize);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Marshal_GetMarshalSizeMax_CallBack, pArgs, pArgs->hr);
    }
}


HRESULT __stdcall Marshal_GetMarshalSizeMax_Wrapper(IMarshal* pMarsh, REFIID riid, void * pv, ULONG dwDestContext,
                                  void * pvDestContext, ULONG mshlflags, ULONG * pSize)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(CheckPointer(pv, NULL_OK));
        PRECONDITION(CheckPointer(pvDestContext, NULL_OK));
        PRECONDITION(CheckPointer(pSize, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetMarshalSizeMaxArgs args = {pMarsh, &riid, pv, dwDestContext, pvDestContext,
                                  mshlflags, pSize, &hr};
    Marshal_GetMarshalSizeMax_CallBack(&args);
    return hr;
}

struct MarshalInterfaceArgs
{
    IMarshal* pUnk;
    LPSTREAM pStm;
    const IID* riid;
    void * pv;
    ULONG dwDestContext;
    void * pvDestContext;
    ULONG mshlflags;
    HRESULT* hr;

};

VOID __stdcall Marshal_MarshalInterface_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    MarshalInterfaceArgs* pArgs = (MarshalInterfaceArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Marshal_MarshalInterface(pArgs->pUnk, pArgs->pStm, *(pArgs->riid), pArgs->pv,
                                    pArgs->dwDestContext, pArgs->pvDestContext, pArgs->mshlflags);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Marshal_MarshalInterface_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Marshal_MarshalInterface_Wrapper(IMarshal* pMarsh, LPSTREAM pStm, REFIID riid, void * pv,
                                 ULONG dwDestContext, LPVOID pvDestContext, ULONG mshlflags)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(CheckPointer(pv, NULL_OK));
        PRECONDITION(CheckPointer(pvDestContext, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    MarshalInterfaceArgs args = {pMarsh, pStm, &riid, pv, dwDestContext, pvDestContext,
                                        mshlflags, &hr};
    Marshal_MarshalInterface_CallBack(&args);
    return hr;
}

struct UnmarshalInterfaceArgs
{
    IMarshal* pUnk;
    LPSTREAM pStm;
    const IID* riid;
    void ** ppvObj;
    HRESULT* hr;

};

VOID __stdcall Marshal_UnmarshalInterface_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    UnmarshalInterfaceArgs* pArgs = (UnmarshalInterfaceArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Marshal_UnmarshalInterface(pArgs->pUnk, pArgs->pStm, *(pArgs->riid), pArgs->ppvObj);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Marshal_UnmarshalInterface_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Marshal_UnmarshalInterface_Wrapper(IMarshal* pMarsh, LPSTREAM pStm, REFIID riid, void ** ppvObj)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(CheckPointer(pStm, NULL_OK));
        PRECONDITION(CheckPointer(ppvObj, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    UnmarshalInterfaceArgs args = {pMarsh, pStm, &riid, ppvObj, &hr};
    Marshal_UnmarshalInterface_CallBack(&args);
    return hr;
}

struct ReleaseMarshalDataArgs
{
    IMarshal* pUnk;
    LPSTREAM pStm;
    HRESULT* hr;

};

VOID __stdcall Marshal_ReleaseMarshalData_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    ReleaseMarshalDataArgs* pArgs = (ReleaseMarshalDataArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Marshal_ReleaseMarshalData(pArgs->pUnk, pArgs->pStm);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Marshal_ReleaseMarshalData_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Marshal_ReleaseMarshalData_Wrapper(IMarshal* pMarsh, LPSTREAM pStm)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
        PRECONDITION(CheckPointer(pStm, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    ReleaseMarshalDataArgs args = {pMarsh, pStm, &hr};
    Marshal_ReleaseMarshalData_CallBack(&args);
    return hr;
}

struct DisconnectObjectArgs
{
    IMarshal* pUnk;
    ULONG dwReserved;
    HRESULT* hr;
};

VOID __stdcall Marshal_DisconnectObject_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    DisconnectObjectArgs* pArgs = (DisconnectObjectArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = Marshal_DisconnectObject(pArgs->pUnk, pArgs->dwReserved);
    }
    else
    {
        AppDomainDoCallBack(pWrap, Marshal_DisconnectObject_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall Marshal_DisconnectObject_Wrapper(IMarshal* pMarsh, ULONG dwReserved)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pMarsh));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    DisconnectObjectArgs args = {pMarsh, dwReserved, &hr};
    Marshal_DisconnectObject_CallBack(&args);
    return hr;
}

// ---------------------------------------------------------------------------
//  Interface IConnectionPointContainer

struct EnumConnectionPointsArgs
{
    IUnknown* pUnk;
    IEnumConnectionPoints **ppEnum;
    HRESULT*        hr;
};

VOID __stdcall ConnectionPointContainer_EnumConnectionPoints_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    EnumConnectionPointsArgs* pArgs = (EnumConnectionPointsArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ConnectionPointContainer_EnumConnectionPoints(pArgs->pUnk, pArgs->ppEnum);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ConnectionPointContainer_EnumConnectionPoints_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall ConnectionPointContainer_EnumConnectionPoints_Wrapper(IUnknown* pUnk, IEnumConnectionPoints **ppEnum)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppEnum, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    EnumConnectionPointsArgs args = {pUnk, ppEnum, &hr};
    ConnectionPointContainer_EnumConnectionPoints_CallBack(&args);
    return hr;
}

struct FindConnectionPointArgs
{
    IUnknown* pUnk;
    const IID* riid;
    IConnectionPoint **ppCP;
    HRESULT*    hr;
};

VOID __stdcall ConnectionPointContainer_FindConnectionPoint_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    FindConnectionPointArgs* pArgs = (FindConnectionPointArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ConnectionPointContainer_FindConnectionPoint(pArgs->pUnk, *(pArgs->riid),
                                                            pArgs->ppCP);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ConnectionPointContainer_FindConnectionPoint_CallBack, pArgs, pArgs->hr);
    }
}

HRESULT __stdcall ConnectionPointContainer_FindConnectionPoint_Wrapper(IUnknown* pUnk, REFIID riid, IConnectionPoint **ppCP)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(ppCP, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    FindConnectionPointArgs args = {pUnk, &riid, ppCP, &hr};
    ConnectionPointContainer_FindConnectionPoint_CallBack(&args);
    return hr;
}


//------------------------------------------------------------------------------------------
//      IObjectSafety methods for COM+ objects

struct GetInterfaceSafetyArgs
{
    IUnknown* pUnk;
    const IID* riid;
    DWORD *pdwSupportedOptions;
    DWORD *pdwEnabledOptions;
    HRESULT*    hr;
};

VOID __stdcall ObjectSafety_GetInterfaceSafetyOptions_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    GetInterfaceSafetyArgs* pArgs = (GetInterfaceSafetyArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ObjectSafety_GetInterfaceSafetyOptions(pArgs->pUnk, *(pArgs->riid),
                                                              pArgs->pdwSupportedOptions,
                                                              pArgs->pdwEnabledOptions);
    }
    else
    {
        AppDomainDoCallBack(pWrap, ObjectSafety_GetInterfaceSafetyOptions_CallBack, pArgs, pArgs->hr);
    }
}


HRESULT __stdcall ObjectSafety_GetInterfaceSafetyOptions_Wrapper(IUnknown* pUnk, REFIID riid,
                                               DWORD *pdwSupportedOptions, DWORD *pdwEnabledOptions)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
        PRECONDITION(CheckPointer(pdwSupportedOptions, NULL_OK));
        PRECONDITION(CheckPointer(pdwEnabledOptions, NULL_OK));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    GetInterfaceSafetyArgs args = {pUnk, &riid, pdwSupportedOptions, pdwEnabledOptions, &hr};
    ObjectSafety_GetInterfaceSafetyOptions_CallBack(&args);
    return hr;
}

struct SetInterfaceSafetyArgs
{
    IUnknown* pUnk;
    const IID* riid;
    DWORD dwOptionSetMask;
    DWORD dwEnabledOptions;
    HRESULT*    hr;
};

VOID __stdcall ObjectSafety_SetInterfaceSafetyOptions_CallBack(LPVOID ptr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(ptr));
    }
    CONTRACTL_END;

    SetInterfaceSafetyArgs* pArgs = (SetInterfaceSafetyArgs*)ptr;
    ComCallWrapper* pWrap = MapIUnknownToWrapper(pArgs->pUnk);
    if (IsCurrentDomainValid(pWrap))
    {
        *(pArgs->hr) = ObjectSafety_SetInterfaceSafetyOptions(pArgs->pUnk, *(pArgs->riid),
                                                              pArgs->dwOptionSetMask,
                                                              pArgs->dwEnabledOptions
                                                              );
    }
    else
    {
        AppDomainDoCallBack(pWrap, ObjectSafety_SetInterfaceSafetyOptions_CallBack, pArgs, pArgs->hr);
    }
}


HRESULT __stdcall ObjectSafety_SetInterfaceSafetyOptions_Wrapper(IUnknown* pUnk, REFIID riid,
                                               DWORD dwOptionSetMask, DWORD dwEnabledOptions)
{
    SetupForComCallHR();

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(CheckPointer(pUnk));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    SetInterfaceSafetyArgs args = {pUnk, &riid, dwOptionSetMask, dwEnabledOptions, &hr};
    ObjectSafety_SetInterfaceSafetyOptions_CallBack(&args);
    return hr;
}
