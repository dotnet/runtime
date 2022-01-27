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
#include "weakreference.h"
#include "common.h"

// Until the Windows SDK is updated, just hard-code the INoMarshal IID
#ifndef __INoMarshal_INTERFACE_DEFINED__
DEFINE_GUID(IID_INoMarshal,0xecc8691b,0xc1db,0x4dc0,0x85,0x5e,0x65,0xf6,0xc5,0x51,0xaf,0x49);
MIDL_INTERFACE("ecc8691b-c1db-4dc0-855e-65f6c551af49")
INoMarshal : public IUnknown
{
public:
};
#endif // !__INoMarshal_INTERFACE_DEFINED__


class Assembly;
class Module;
class MethodTable;

typedef HRESULT (__stdcall* PCOMFN)(void);

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


//-------------------------------------------------------------------------
// IProvideClassInfo methods
HRESULT __stdcall ClassInfo_GetClassInfo_Wrapper(IUnknown* pUnk,
                         ITypeInfo** ppTI); //Address of output variable that receives the type info.

// ---------------------------------------------------------------------------
//  Interface ISupportsErrorInfo

// %%Function: SupportsErroInfo_IntfSupportsErrorInfo,
// ---------------------------------------------------------------------------
HRESULT __stdcall
SupportsErroInfo_IntfSupportsErrorInfo_Wrapper(IUnknown* pUnk, REFIID riid);

// ---------------------------------------------------------------------------
//  Interface IErrorInfo

// %%Function: ErrorInfo_GetDescription,
HRESULT __stdcall ErrorInfo_GetDescription_Wrapper(IUnknown* pUnk, BSTR* pbstrDescription);

// %%Function: ErrorInfo_GetGUID,
HRESULT __stdcall ErrorInfo_GetGUID_Wrapper(IUnknown* pUnk, GUID* pguid);

// %%Function: ErrorInfo_GetHelpContext,
HRESULT _stdcall ErrorInfo_GetHelpContext_Wrapper(IUnknown* pUnk, DWORD* pdwHelpCtxt);

// %%Function: ErrorInfo_GetHelpFile,
HRESULT __stdcall ErrorInfo_GetHelpFile_Wrapper(IUnknown* pUnk, BSTR* pbstrHelpFile);

// %%Function: ErrorInfo_GetSource,
HRESULT __stdcall ErrorInfo_GetSource_Wrapper(IUnknown* pUnk, BSTR* pbstrSource);

//------------------------------------------------------------------------------------------
//      IDispatch methods for COM+ objects. These methods dispatch to the appropriate
//      implementation based on the flags of the class that implements them.


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
//      IDispatchEx methods for COM+ objects


// %%Function: IDispatchEx::GetTypeInfoCount
HRESULT __stdcall   DispatchEx_GetTypeInfoCount_Wrapper (
                                    IDispatchEx* pDisp,
                                    unsigned int *pctinfo);


//  %%Function: IDispatch::GetTypeInfo
HRESULT __stdcall   DispatchEx_GetTypeInfo_Wrapper (
                                    IDispatchEx* pDisp,
                                    unsigned int itinfo,
                                    LCID lcid,
                                    ITypeInfo **pptinfo);

// IDispatchEx::GetIDsofNames
HRESULT __stdcall   DispatchEx_GetIDsOfNames_Wrapper (
                                    IDispatchEx* pDisp,
                                    REFIID riid,
                                    _In_reads_(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid);

// IDispatchEx::Invoke
HRESULT __stdcall   DispatchEx_Invoke_Wrapper (
                                    IDispatchEx* pDisp,
                                    DISPID dispidMember,
                                    REFIID riid,
                                    LCID lcid,
                                    unsigned short wFlags,
                                    DISPPARAMS *pdispparams,
                                    VARIANT *pvarResult,
                                    EXCEPINFO *pexcepinfo,
                                    unsigned int *puArgErr);

// IDispatchEx::DeleteMemberByDispID
HRESULT __stdcall   DispatchEx_DeleteMemberByDispID_Wrapper (
                                    IDispatchEx* pDisp,
                                    DISPID id);

// IDispatchEx::DeleteMemberByName
HRESULT __stdcall   DispatchEx_DeleteMemberByName_Wrapper (
                                    IDispatchEx* pDisp,
                                    BSTR bstrName,
                                    DWORD grfdex);


// IDispatchEx::GetDispID
HRESULT __stdcall   DispatchEx_GetDispID_Wrapper (
                                    IDispatchEx* pDisp,
                                    BSTR bstrName,
                                    DWORD grfdex,
                                    DISPID *pid);


// IDispatchEx::GetMemberName
HRESULT __stdcall   DispatchEx_GetMemberName_Wrapper (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    BSTR *pbstrName);

// IDispatchEx::GetMemberProperties
HRESULT __stdcall   DispatchEx_GetMemberProperties_Wrapper (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    DWORD grfdexFetch,
                                    DWORD *pgrfdex);

// IDispatchEx::GetNameSpaceParent
HRESULT __stdcall   DispatchEx_GetNameSpaceParent_Wrapper (
                                    IDispatchEx* pDisp,
                                    IUnknown **ppunk);

// IDispatchEx::GetNextDispID
HRESULT __stdcall   DispatchEx_GetNextDispID_Wrapper (
                                    IDispatchEx* pDisp,
                                    DWORD grfdex,
                                    DISPID id,
                                    DISPID *pid);

// IDispatchEx::InvokeEx
HRESULT __stdcall   DispatchEx_InvokeEx_Wrapper (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    LCID lcid,
                                    WORD wFlags,
                                    DISPPARAMS *pdp,
                                    VARIANT *pVarRes,
                                    EXCEPINFO *pei,
                                    IServiceProvider *pspCaller);

//------------------------------------------------------------------------------------------
//      IMarshal methods for COM+ objects

HRESULT __stdcall Marshal_GetUnmarshalClass_Wrapper (
                                    IMarshal* pMarsh,
                                    REFIID riid, void * pv, ULONG dwDestContext,
                                    void * pvDestContext, ULONG mshlflags,
                                    LPCLSID pclsid);

HRESULT __stdcall Marshal_GetMarshalSizeMax_Wrapper (
                                    IMarshal* pMarsh,
                                    REFIID riid, void * pv, ULONG dwDestContext,
                                    void * pvDestContext, ULONG mshlflags,
                                    ULONG * pSize);

HRESULT __stdcall Marshal_MarshalInterface_Wrapper (
                                    IMarshal* pMarsh,
                                    LPSTREAM pStm, REFIID riid, void * pv,
                                    ULONG dwDestContext, LPVOID pvDestContext,
                                    ULONG mshlflags);

HRESULT __stdcall Marshal_UnmarshalInterface_Wrapper (
                                    IMarshal* pMarsh,
                                    LPSTREAM pStm, REFIID riid,
                                    void ** ppvObj);

HRESULT __stdcall Marshal_ReleaseMarshalData_Wrapper (IMarshal* pMarsh, LPSTREAM pStm);

HRESULT __stdcall Marshal_DisconnectObject_Wrapper (IMarshal* pMarsh, ULONG dwReserved);


//------------------------------------------------------------------------------------------
//      IConnectionPointContainer methods for COM+ objects

interface IEnumConnectionPoints;

HRESULT __stdcall ConnectionPointContainer_EnumConnectionPoints_Wrapper(IUnknown* pUnk,
                                    IEnumConnectionPoints **ppEnum);

HRESULT __stdcall ConnectionPointContainer_FindConnectionPoint_Wrapper(IUnknown* pUnk,
                                    REFIID riid,
                                    IConnectionPoint **ppCP);


//------------------------------------------------------------------------------------------
//      IObjectSafety methods for COM+ objects

interface IObjectSafety;

HRESULT __stdcall ObjectSafety_GetInterfaceSafetyOptions_Wrapper(IUnknown* pUnk,
                                                         REFIID riid,
                                                         DWORD *pdwSupportedOptions,
                                                         DWORD *pdwEnabledOptions);

HRESULT __stdcall ObjectSafety_SetInterfaceSafetyOptions_Wrapper(IUnknown* pUnk,
                                                         REFIID riid,
                                                         DWORD dwOptionSetMask,
                                                         DWORD dwEnabledOptions);

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


// special idispatch methods

HRESULT __stdcall
InternalDispatchImpl_GetIDsOfNames (
    IDispatch* pDisp,
    REFIID riid,
    _In_reads_(cNames) OLECHAR **rgszNames,
    unsigned int cNames,
    LCID lcid,
    DISPID *rgdispid);


HRESULT __stdcall
InternalDispatchImpl_Invoke (
    IDispatch* pDisp,
    DISPID dispidMember,
    REFIID riid,
    LCID lcid,
    unsigned short wFlags,
    DISPPARAMS *pdispparams,
    VARIANT *pvarResult,
    EXCEPINFO *pexcepinfo,
    unsigned int *puArgErr);

//------------------------------------------------------------------------------------------
// Helper to get the current IErrorInfo if the specified interface supports it.
IErrorInfo *GetSupportedErrorInfo(IUnknown *iface, REFIID riid);

//------------------------------------------------------------------------------------------
// Helpers to get the ITypeInfo* for a type.
HRESULT GetITypeInfoForEEClass(MethodTable *pMT, ITypeInfo **ppTI, bool bClassInfo = false);

#endif
