// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//---------------------------------------------------------------------------------
// stdinterfaces.h
//
// Defines various standard com interfaces , refer to stdinterfaces.cpp for more documentation

//---------------------------------------------------------------------------------

#ifndef _H_STDINTERFACES_
#define _H_STDINTERFACES_

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

#include "dispex.h"
#include "common.h"

class Assembly;
class Module;
class MethodTable;

//------------------------------------------------------------------------------------------
// HRESULT's returned by GetITypeInfoForEEClass.
#define S_USEIUNKNOWN   (HRESULT)2
#define S_USEIDISPATCH  (HRESULT)3

// make sure to keep the following enum and the g_stdVtables array in sync
enum Enum_StdInterfaces
{
    enum_InnerUnknown   = 0,
    enum_IProvideClassInfo,
    enum_IMarshal,
    enum_ISupportsErrorInfo,
    enum_IErrorInfo,
    enum_IConnectionPointContainer,
    enum_IObjectSafety,
    enum_IDispatchEx,
    enum_IAgileObject,
    // add your favorite std interface here
    enum_LastStdVtable,

    enum_IUnknown = 0xff, // special enum for std unknown
};

// array of vtable pointers for std. interfaces such as IProvideClassInfo etc.
extern const SLOT * const g_rgStdVtables[];

template <size_t nVtableEntries>
struct StdInterfaceDesc
{
    // This is a self-describing vtable pointer.
    Enum_StdInterfaces          m_StdInterfaceKind;
    UINT_PTR const * const      m_vtable[nVtableEntries];
};

typedef DPTR(StdInterfaceDesc<1>)   PTR_StdInterfaceDesc;
typedef VPTR(IUnknown)              PTR_IUnknown;

inline static Enum_StdInterfaces GetStdInterfaceKind(PTR_IUnknown pUnk)
{
    LIMITED_METHOD_DAC_CONTRACT;

    PTR_SLOT pVtable = dac_cast<PTR_SLOT>(*(dac_cast<PTR_TADDR>(pUnk)));
    PTR_StdInterfaceDesc pDesc = dac_cast<PTR_StdInterfaceDesc>(dac_cast<PTR_BYTE>(pVtable) - offsetof(StdInterfaceDesc<1>, m_vtable));

#ifndef DACCESS_COMPILE
    // Make sure the interface kind is the right one
    // Only do this in non-DAC build as I don't want to bring in g_rgStdVtables global variable
    _ASSERTE(g_rgStdVtables[pDesc->m_StdInterfaceKind] == pVtable);
#endif // !DACCESS_COMPILE

    return pDesc->m_StdInterfaceKind;
}


// IUnknown is part of IDispatch
// Common vtables for well-known COM interfaces
// shared by all COM+ callable wrappers.
extern const StdInterfaceDesc<3>  g_InnerUnknown;
extern const StdInterfaceDesc<4>  g_IProvideClassInfo;
extern const StdInterfaceDesc<9>  g_IMarshal;
extern const StdInterfaceDesc<4>  g_ISupportsErrorInfo;
extern const StdInterfaceDesc<8>  g_IErrorInfo;
extern const StdInterfaceDesc<5>  g_IConnectionPointContainer;
extern const StdInterfaceDesc<5>  g_IObjectSafety;
extern const StdInterfaceDesc<15> g_IDispatchEx;
extern const StdInterfaceDesc<3>  g_IAgileObject;

// enum class types
enum ComClassType
{
    enum_UserDefined = 0,
    enum_Collection,
    enum_Exception,
    enum_Event,
    enum_Delegate,
    enum_Control,
    enum_Last,
};

// IUNKNOWN wrappers

// prototypes IUnknown methods
HRESULT __stdcall   Unknown_QueryInterface(IUnknown* pUnk, REFIID riid, void** ppv);

ULONG __stdcall     Unknown_AddRef(IUnknown* pUnk);
ULONG __stdcall     Unknown_Release(IUnknown* pUnk);
ULONG __stdcall     Unknown_AddRefInner(IUnknown* pUnk);
ULONG __stdcall     Unknown_ReleaseInner(IUnknown* pUnk);

// for std interfaces such as IProvideClassInfo
HRESULT __stdcall   Unknown_QueryInterface_IErrorInfo(IUnknown* pUnk, REFIID riid, void** ppv);
ULONG __stdcall     Unknown_AddRefSpecial(IUnknown* pUnk);
ULONG __stdcall     Unknown_ReleaseSpecial(IUnknown* pUnk);
ULONG __stdcall     Unknown_ReleaseSpecial_IErrorInfo(IUnknown* pUnk);

// IDISPATCH wrappers

// %%Function: IDispatch::GetTypeInfoCount
HRESULT __stdcall   Dispatch_GetTypeInfoCount_Wrapper (
                                     IDispatch* pDisp,
                                     unsigned int *pctinfo);


//  %%Function: IDispatch::GetTypeInfo
HRESULT __stdcall   Dispatch_GetTypeInfo_Wrapper (
                                    IDispatch* pDisp,
                                    unsigned int itinfo,
                                    LCID lcid,
                                    ITypeInfo **pptinfo);

//  %%Function: IDispatch::GetIDsofNames
HRESULT __stdcall   Dispatch_GetIDsOfNames_Wrapper (
                                    IDispatch* pDisp,
                                    REFIID riid,
                                    _In_reads_(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid);

//  %%Function: IDispatch::Invoke
HRESULT __stdcall   Dispatch_Invoke_Wrapper (
                                    IDispatch* pDisp,
                                    DISPID dispidMember,
                                    REFIID riid,
                                    LCID lcid,
                                    unsigned short wFlags,
                                    DISPPARAMS *pdispparams,
                                    VARIANT *pvarResult,
                                    EXCEPINFO *pexcepinfo,
                                    unsigned int *puArgErr
                                    );

//  %%Function: IDispatch::GetIDsofNames
HRESULT __stdcall   InternalDispatchImpl_GetIDsOfNames_Wrapper (
                                    IDispatch* pDisp,
                                    REFIID riid,
                                    _In_reads_(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid);

//  %%Function: IDispatch::Invoke
HRESULT __stdcall   InternalDispatchImpl_Invoke_Wrapper (
                                    IDispatch* pDisp,
                                    DISPID dispidMember,
                                    REFIID riid,
                                    LCID lcid,
                                    unsigned short wFlags,
                                    DISPPARAMS *pdispparams,
                                    VARIANT *pvarResult,
                                    EXCEPINFO *pexcepinfo,
                                    unsigned int *puArgErr
                                    );

//------------------------------------------------------------------------------------------
// Helper to get the current IErrorInfo if the specified interface supports it.
IErrorInfo *GetSupportedErrorInfo(IUnknown *iface, REFIID riid);

//------------------------------------------------------------------------------------------
// Helpers to get the ITypeInfo* for a type.
HRESULT GetITypeInfoForEEClass(MethodTable *pMT, ITypeInfo **ppTI, bool bClassInfo = false);

// Gets the MethodTable for the associated IRecordInfo.
MethodTable* GetMethodTableForRecordInfo(IRecordInfo* recInfo);

#endif
