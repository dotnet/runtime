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
#include <mtx.h>
#include "cgencpu.h"
#include "interopconverter.h"
#include "cominterfacemarshaler.h"
#include "stdinterfaces.h"
#include "stdinterfaces_internal.h"
#include "interoputil.inl"

struct IEnumConnectionPoints;

// IUnknown is part of IDispatch
// Common vtables for well-known COM interfaces
// shared by all COM+ callable wrappers.

//-------------------------------------------------------------------------
// IUnknown methods


HRESULT STDMETHODCALLTYPE Unknown_QueryInterface(IUnknown* pUnk, REFIID riid, void** ppv)
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
    return Unknown_QueryInterface_Internal(pWrap, pUnk, riid, ppv);
}

ULONG STDMETHODCALLTYPE Unknown_AddRef(IUnknown* pUnk)
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

    // Allow addrefs to go through, because we are allowing
    // all releases to go through, otherwise we would
    // have a mismatch of ref-counts
    return Unknown_AddRef_Internal(pUnk);
}

ULONG STDMETHODCALLTYPE Unknown_Release(IUnknown* pUnk)
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
    // even after the AD has been unloaded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_Release_Internal(pUnk);
}

ULONG STDMETHODCALLTYPE Unknown_AddRefInner(IUnknown* pUnk)
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

    // Allow addrefs to go through, because we are allowing
    // all releases to go through, otherwise we would
    // have a mismatch of ref-counts
    return Unknown_AddRefInner_Internal(pUnk);
}

ULONG STDMETHODCALLTYPE Unknown_ReleaseInner(IUnknown* pUnk)
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
    // even after the AD has been unloaded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_ReleaseInner_Internal(pUnk);
}

ULONG STDMETHODCALLTYPE Unknown_AddRefSpecial(IUnknown* pUnk)
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

    // Allow addrefs to go through, because we are allowing
    // all releases to go through, otherwise we would
    // have a mismatch of ref-counts
    return Unknown_AddRefSpecial_Internal(pUnk);
}

ULONG STDMETHODCALLTYPE Unknown_ReleaseSpecial(IUnknown* pUnk)
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
    // even after the AD has been unloaded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_ReleaseSpecial_Internal(pUnk);
}

HRESULT STDMETHODCALLTYPE Unknown_QueryInterface_IErrorInfo(IUnknown* pUnk, REFIID riid, void** ppv)
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
ULONG STDMETHODCALLTYPE Unknown_ReleaseSpecial_IErrorInfo(IUnknown* pUnk)
{
    SetupForComCallDWORD();

    WRAPPER_NO_CONTRACT;

    CONTRACT_VIOLATION(GCViolation);

    // Don't switch domains since we need to allow release calls to go through
    // even after the AD has been unloaded. Furthermore release doesn't require
    // us to transition into the domain to work properly.
    return Unknown_ReleaseSpecial_IErrorInfo_Internal(pUnk);
}

// ---------------------------------------------------------------------------
//  Interface IDispatch
//
//      IDispatch methods for COM+ objects. These methods dispatch's to the
//      appropriate implementation based on the flags of the class that
//      implements them.

HRESULT STDMETHODCALLTYPE Dispatch_GetTypeInfoCount_Wrapper(IDispatch* pDisp, unsigned int *pctinfo)
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
    
    return Dispatch_GetTypeInfoCount(pDisp, pctinfo);
}

HRESULT STDMETHODCALLTYPE Dispatch_GetTypeInfo_Wrapper(IDispatch* pDisp, unsigned int itinfo, LCID lcid, ITypeInfo **pptinfo)
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

    return Dispatch_GetTypeInfo(pDisp, itinfo, lcid, pptinfo);
}

HRESULT STDMETHODCALLTYPE Dispatch_GetIDsOfNames_Wrapper(IDispatch* pDisp, REFIID riid, _In_reads_(cNames) OLECHAR **rgszNames,
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

    return Dispatch_GetIDsOfNames(pDisp, riid, rgszNames, cNames, lcid, rgdispid);
}

HRESULT STDMETHODCALLTYPE InternalDispatchImpl_GetIDsOfNames_Wrapper(IDispatch* pDisp, REFIID riid, _In_reads_(cNames) OLECHAR **rgszNames,
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
    
    return InternalDispatchImpl_GetIDsOfNames(pDisp, riid, rgszNames, cNames, lcid, rgdispid);
}

HRESULT STDMETHODCALLTYPE Dispatch_Invoke_Wrapper(IDispatch* pDisp, DISPID dispidMember, REFIID riid, LCID lcid, unsigned short wFlags,
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
    
    return Dispatch_Invoke(pDisp, dispidMember, riid, lcid, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
}

HRESULT STDMETHODCALLTYPE InternalDispatchImpl_Invoke_Wrapper(IDispatch* pDisp, DISPID dispidMember, REFIID riid, LCID lcid,
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
    
    return InternalDispatchImpl_Invoke(pDisp, dispidMember, riid, lcid, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
}

namespace
{
    //-------------------------------------------------------------------------
    // IProvideClassInfo methods

    HRESULT STDMETHODCALLTYPE ClassInfo_GetClassInfo_Wrapper(IUnknown* pUnk, ITypeInfo** ppTI)
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
    
        return ClassInfo_GetClassInfo(pUnk, ppTI);
    }


    // ---------------------------------------------------------------------------
    //  Interface ISupportsErrorInfo

    HRESULT STDMETHODCALLTYPE
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
    
        return SupportsErroInfo_IntfSupportsErrorInfo(pUnk, riid);
    }

    // ---------------------------------------------------------------------------
    //  Interface IErrorInfo
    HRESULT STDMETHODCALLTYPE ErrorInfo_GetDescription_Wrapper(IUnknown* pUnk, BSTR* pbstrDescription)
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
    
        return ErrorInfo_GetDescription(pUnk, pbstrDescription);
    }

    HRESULT STDMETHODCALLTYPE ErrorInfo_GetGUID_Wrapper(IUnknown* pUnk, GUID* pguid)
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
    
        return ErrorInfo_GetGUID(pUnk, pguid);
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
    
        return ErrorInfo_GetHelpContext(pUnk, pdwHelpCtxt);
    }

    HRESULT STDMETHODCALLTYPE ErrorInfo_GetHelpFile_Wrapper(IUnknown* pUnk, BSTR* pbstrHelpFile)
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
    
        return ErrorInfo_GetHelpFile(pUnk, pbstrHelpFile);
    }

    HRESULT STDMETHODCALLTYPE ErrorInfo_GetSource_Wrapper(IUnknown* pUnk, BSTR* pbstrSource)
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
    
        return ErrorInfo_GetSource(pUnk, pbstrSource);
    }

    // ---------------------------------------------------------------------------
    //  Interface IDispatchEx

    HRESULT STDMETHODCALLTYPE DispatchEx_GetTypeInfoCount_Wrapper(IDispatchEx* pDisp, unsigned int *pctinfo)
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
    
        return DispatchEx_GetTypeInfoCount(pDisp, pctinfo);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_GetTypeInfo_Wrapper(IDispatchEx* pDisp, unsigned int itinfo, LCID lcid, ITypeInfo **pptinfo)
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
    
        return DispatchEx_GetTypeInfo(pDisp, itinfo, lcid, pptinfo);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_GetIDsOfNames_Wrapper(IDispatchEx* pDisp, REFIID riid, _In_reads_(cNames) OLECHAR **rgszNames,
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
    
        return DispatchEx_GetIDsOfNames(pDisp, riid, rgszNames, cNames, lcid, rgdispid);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_Invoke_Wrapper(IDispatchEx* pDisp, DISPID dispidMember, REFIID riid, LCID lcid,
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

        return DispatchEx_Invoke(pDisp, dispidMember, riid, lcid, wFlags, pdispparams, pvarResult, pexcepinfo, puArgErr);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_DeleteMemberByDispID_Wrapper(IDispatchEx* pDisp, DISPID id)
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
    
        return DispatchEx_DeleteMemberByDispID(pDisp, id);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_DeleteMemberByName_Wrapper(IDispatchEx* pDisp, BSTR bstrName, DWORD grfdex)
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

        return DispatchEx_DeleteMemberByName(pDisp, bstrName, grfdex);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_GetMemberName_Wrapper(IDispatchEx* pDisp, DISPID id, BSTR *pbstrName)
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
    
        return DispatchEx_GetMemberName(pDisp, id, pbstrName);
    }


    HRESULT STDMETHODCALLTYPE DispatchEx_GetDispID_Wrapper(IDispatchEx* pDisp, BSTR bstrName, DWORD grfdex, DISPID *pid)
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

        return DispatchEx_GetDispID(pDisp, bstrName, grfdex, pid);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_GetMemberProperties_Wrapper(IDispatchEx* pDisp, DISPID id, DWORD grfdexFetch, DWORD *pgrfdex)
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
    
        return DispatchEx_GetMemberProperties(pDisp, id, grfdexFetch, pgrfdex);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_GetNameSpaceParent_Wrapper(IDispatchEx* pDisp, IUnknown **ppunk)
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
    
        return DispatchEx_GetNameSpaceParent(pDisp, ppunk);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_GetNextDispID_Wrapper(IDispatchEx* pDisp, DWORD grfdex, DISPID id, DISPID *pid)
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
    
        return DispatchEx_GetNextDispID(pDisp, grfdex, id, pid);
    }

    HRESULT STDMETHODCALLTYPE DispatchEx_InvokeEx_Wrapper(IDispatchEx* pDisp, DISPID id, LCID lcid, WORD wFlags, DISPPARAMS *pdp,
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
    
        return DispatchEx_InvokeEx(pDisp, id, lcid, wFlags, pdp, pVarRes, pei, pspCaller);
    }

    // ---------------------------------------------------------------------------
    //  Interface IMarshal

    HRESULT STDMETHODCALLTYPE Marshal_GetUnmarshalClass_Wrapper(IMarshal* pMarsh, REFIID riid, void * pv, ULONG dwDestContext,
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
    
        return Marshal_GetUnmarshalClass(pMarsh, riid, pv, dwDestContext, pvDestContext, mshlflags, pclsid);
    }

    HRESULT STDMETHODCALLTYPE Marshal_GetMarshalSizeMax_Wrapper(IMarshal* pMarsh, REFIID riid, void * pv, ULONG dwDestContext,
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
    
        return Marshal_GetMarshalSizeMax(pMarsh, riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
    }

    HRESULT STDMETHODCALLTYPE Marshal_MarshalInterface_Wrapper(IMarshal* pMarsh, LPSTREAM pStm, REFIID riid, void * pv,
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
    
        return Marshal_MarshalInterface(pMarsh, pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
    }

    HRESULT STDMETHODCALLTYPE Marshal_UnmarshalInterface_Wrapper(IMarshal* pMarsh, LPSTREAM pStm, REFIID riid, void ** ppvObj)
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
    
        return Marshal_UnmarshalInterface(pMarsh, pStm, riid, ppvObj);
    }

    HRESULT STDMETHODCALLTYPE Marshal_ReleaseMarshalData_Wrapper(IMarshal* pMarsh, LPSTREAM pStm)
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
    
        return Marshal_ReleaseMarshalData(pMarsh, pStm);
    }

    HRESULT STDMETHODCALLTYPE Marshal_DisconnectObject_Wrapper(IMarshal* pMarsh, ULONG dwReserved)
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
    
        return Marshal_DisconnectObject(pMarsh, dwReserved);
    }

    // ---------------------------------------------------------------------------
    //  Interface IConnectionPointContainer

    HRESULT STDMETHODCALLTYPE ConnectionPointContainer_EnumConnectionPoints_Wrapper(IUnknown* pUnk, IEnumConnectionPoints **ppEnum)
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
    
        return ConnectionPointContainer_EnumConnectionPoints(pUnk, ppEnum);
    }

    HRESULT STDMETHODCALLTYPE ConnectionPointContainer_FindConnectionPoint_Wrapper(IUnknown* pUnk, REFIID riid, IConnectionPoint **ppCP)
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
    
        return ConnectionPointContainer_FindConnectionPoint(pUnk, riid, ppCP);
    }


    //------------------------------------------------------------------------------------------
    //      IObjectSafety methods for COM+ objects

    HRESULT STDMETHODCALLTYPE ObjectSafety_GetInterfaceSafetyOptions_Wrapper(IUnknown* pUnk, REFIID riid,
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
    
        return ObjectSafety_GetInterfaceSafetyOptions(pUnk, riid, pdwSupportedOptions, pdwEnabledOptions);
    }

    HRESULT STDMETHODCALLTYPE ObjectSafety_SetInterfaceSafetyOptions_Wrapper(IUnknown* pUnk, REFIID riid,
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
    
        return ObjectSafety_SetInterfaceSafetyOptions(pUnk, riid, dwOptionSetMask, dwEnabledOptions);
    }
}

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
