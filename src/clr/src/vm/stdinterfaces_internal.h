//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


#ifndef _H_INTERNAL_STDINTERFACES
#define _H_INTERNAL_STDINTERFACES

#ifndef FEATURE_COMINTEROP
#error FEATURE_COMINTEROP is required for this file
#endif // FEATURE_COMINTEROP

// ---------------------------------------------------------------------------
// prototypes IUnknown methods
HRESULT Unknown_QueryInterface_Internal (
                        ComCallWrapper* pWrap, IUnknown* pUnk, REFIID riid, void** ppv);
HRESULT __stdcall   Unknown_QueryInterface_IErrorInfo_Simple (
                        IUnknown* pUnk, REFIID riid, void** ppv);

ULONG __stdcall     Unknown_AddRef_Internal(IUnknown* pUnk);
ULONG __stdcall     Unknown_Release_Internal(IUnknown* pUnk);
ULONG __stdcall     Unknown_AddRefInner_Internal(IUnknown* pUnk);
ULONG __stdcall     Unknown_ReleaseInner_Internal(IUnknown* pUnk);

// for std interfaces such as IProvideClassInfo
ULONG __stdcall     Unknown_AddRefSpecial_Internal(IUnknown* pUnk);
ULONG __stdcall     Unknown_ReleaseSpecial_Internal(IUnknown* pUnk);
ULONG __stdcall     Unknown_ReleaseSpecial_IErrorInfo_Internal(IUnknown* pUnk);

// ---------------------------------------------------------------------------
//  Interface ISupportsErrorInfo

// %%Function: SupportsErroInfo_IntfSupportsErrorInfo,
// ---------------------------------------------------------------------------
HRESULT __stdcall 
SupportsErroInfo_IntfSupportsErrorInfo(IUnknown* pUnk, REFIID riid);

// ---------------------------------------------------------------------------
//  Interface IErrorInfo

// %%Function: ErrorInfo_GetDescription,   
HRESULT __stdcall 
ErrorInfo_GetDescription(IUnknown* pUnk, BSTR* pbstrDescription);

// %%Function: ErrorInfo_GetGUID,    
HRESULT __stdcall ErrorInfo_GetGUID(IUnknown* pUnk, GUID* pguid);

// %%Function: ErrorInfo_GetHelpContext, 
HRESULT _stdcall ErrorInfo_GetHelpContext(IUnknown* pUnk, DWORD* pdwHelpCtxt);

// %%Function: ErrorInfo_GetHelpFile,    
HRESULT __stdcall ErrorInfo_GetHelpFile(IUnknown* pUnk, BSTR* pbstrHelpFile);

// %%Function: ErrorInfo_GetSource,    
HRESULT __stdcall ErrorInfo_GetSource(IUnknown* pUnk, BSTR* pbstrSource);


//------------------------------------------------------------------------------------------
//      IDispatch methods for COM+ objects. These methods dispatch to the appropriate 
//      implementation based on the flags of the class that implements them.


// IDispatch::GetTypeInfoCount 
HRESULT __stdcall   Dispatch_GetTypeInfoCount (
                                    IDispatch* pDisp,
                                    unsigned int *pctinfo);


// IDispatch::GetTypeInfo
HRESULT __stdcall   Dispatch_GetTypeInfo (
                                    IDispatch* pDisp,
                                    unsigned int itinfo,
                                    LCID lcid,
                                    ITypeInfo **pptinfo);

// IDispatch::GetIDsofNames
HRESULT __stdcall   Dispatch_GetIDsOfNames (
                                    IDispatch* pDisp,
                                    REFIID riid,
                                    __in_ecount(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid);

// IDispatch::Invoke
HRESULT __stdcall   Dispatch_Invoke (
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
//      IDispatch methods for COM+ objects that use our OleAut's implementation.


// IDispatch::GetIDsofNames
HRESULT __stdcall   OleAutDispatchImpl_GetIDsOfNames (
                                    IDispatch* pDisp,
                                    REFIID riid,
                                    __in_ecount(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid);

// IDispatch::Invoke
HRESULT __stdcall   OleAutDispatchImpl_Invoke (
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
//      IDispatch methods for COM+ objects that use our internal implementation.


// IDispatch::GetIDsofNames
HRESULT __stdcall   InternalDispatchImpl_GetIDsOfNames (
                                    IDispatch* pDisp,
                                    REFIID riid,
                                    __in_ecount(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid);

// IDispatch::Invoke
HRESULT __stdcall   InternalDispatchImpl_Invoke (
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
//      IDispatchEx methods for COM+ objects


// IDispatchEx::GetTypeInfoCount 
HRESULT __stdcall   DispatchEx_GetTypeInfoCount (
                                    IDispatch* pDisp,
                                    unsigned int *pctinfo);


// IDispatchEx::GetTypeInfo
HRESULT __stdcall   DispatchEx_GetTypeInfo (
                                    IDispatch* pDisp,
                                    unsigned int itinfo,
                                    LCID lcid,
                                    ITypeInfo **pptinfo);

// IDispatchEx::GetIDsofNames
HRESULT __stdcall   DispatchEx_GetIDsOfNames (
                                    IDispatchEx* pDisp,
                                    REFIID riid,
                                    __in_ecount(cNames) OLECHAR **rgszNames,
                                    unsigned int cNames,
                                    LCID lcid,
                                    DISPID *rgdispid);

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
                                    unsigned int *puArgErr);

// IDispatchEx::DeleteMemberByDispID
HRESULT __stdcall   DispatchEx_DeleteMemberByDispID (
                                    IDispatchEx* pDisp,
                                    DISPID id);

// IDispatchEx::DeleteMemberByName
HRESULT __stdcall   DispatchEx_DeleteMemberByName (
                                    IDispatchEx* pDisp,
                                    BSTR bstrName,
                                    DWORD grfdex);

// IDispatchEx::GetDispID
HRESULT __stdcall   DispatchEx_GetDispID (
                                    IDispatchEx* pDisp,
                                    BSTR bstrName,
                                    DWORD grfdex,
                                    DISPID *pid);

// IDispatchEx::GetMemberName
HRESULT __stdcall   DispatchEx_GetMemberName (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    BSTR *pbstrName);

// IDispatchEx::GetMemberProperties
HRESULT __stdcall   DispatchEx_GetMemberProperties (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    DWORD grfdexFetch,
                                    DWORD *pgrfdex);

// IDispatchEx::GetNameSpaceParent
HRESULT __stdcall   DispatchEx_GetNameSpaceParent (
                                    IDispatchEx* pDisp,
                                    IUnknown **ppunk);

// IDispatchEx::GetNextDispID
HRESULT __stdcall   DispatchEx_GetNextDispID (
                                    IDispatchEx* pDisp,
                                    DWORD grfdex,
                                    DISPID id,
                                    DISPID *pid);

// IDispatchEx::InvokeEx
HRESULT __stdcall   DispatchEx_InvokeEx (
                                    IDispatchEx* pDisp,
                                    DISPID id,
                                    LCID lcid,
                                    WORD wFlags,
                                    DISPPARAMS *pdp,
                                    VARIANT *pVarRes, 
                                    EXCEPINFO *pei, 
                                    IServiceProvider *pspCaller);

//------------------------------------------------------------------------------------------
//      IInspectable methods for managed objects

// IInspectable::GetIIDs
HRESULT __stdcall Inspectable_GetIIDs (
                                    IInspectable *pInsp,
                                    ULONG *iidCount,
                                    IID **iids);

HRESULT __stdcall Inspectable_GetRuntimeClassName (
                                    IInspectable *pInsp,
                                    HSTRING *className);

//------------------------------------------------------------------------------------------
//      IWeakReferenceSource methods for managed objects

// IWeakReferenceSource::GetWeakReference
HRESULT __stdcall WeakReferenceSource_GetWeakReference (
                                    IWeakReferenceSource *pRefSrc,
                                    IWeakReference **weakReference);

//------------------------------------------------------------------------------------------
//      ICustomPropertyProvider methods for Jupiter data binding
HRESULT __stdcall ICustomPropertyProvider_GetProperty(IUnknown *pPropertyProvider, 
                                                      HSTRING hstrName, 
                                                      /* [out, retval] */ IUnknown **ppProperty);

HRESULT __stdcall ICustomPropertyProvider_GetIndexedProperty(IUnknown *pPropertyProvider, 
                                                             HSTRING hstrName, 
                                                             TypeNameNative indexedParamType,
                                                             /* [out, retval] */ IUnknown **ppProperty);

HRESULT __stdcall ICustomPropertyProvider_GetStringRepresentation(IUnknown *pPropertyProvider, 
                                                                  /* [out, retval] */ HSTRING *phstrStringRepresentation);
HRESULT __stdcall ICustomPropertyProvider_GetType(IUnknown *pPropertyProvider, 
                                                  /* [out, retval] */ TypeNameNative *pTypeIdentifier);

HRESULT __stdcall IStringable_ToString(IUnknown* pStringable,
                                               /* [out, retval] */ HSTRING* pResult);

//------------------------------------------------------------------------------------------
//      IMarshal methods for COM+ objects

HRESULT __stdcall Marshal_GetUnmarshalClass (
                                    IMarshal* pMarsh,
                                    REFIID riid, void * pv, ULONG dwDestContext, 
                                    void * pvDestContext, ULONG mshlflags, 
                                    LPCLSID pclsid);

HRESULT __stdcall Marshal_GetMarshalSizeMax (
                                    IMarshal* pMarsh,
                                    REFIID riid, void * pv, ULONG dwDestContext, 
                                    void * pvDestContext, ULONG mshlflags, 
                                    ULONG * pSize);

HRESULT __stdcall Marshal_MarshalInterface (
                                    IMarshal* pMarsh,
                                    LPSTREAM pStm, REFIID riid, void * pv,
                                    ULONG dwDestContext, LPVOID pvDestContext,
                                    ULONG mshlflags);

HRESULT __stdcall Marshal_UnmarshalInterface (
                                    IMarshal* pMarsh,
                                    LPSTREAM pStm, REFIID riid, 
                                    void ** ppvObj);

HRESULT __stdcall Marshal_ReleaseMarshalData (IMarshal* pMarsh, LPSTREAM pStm);

HRESULT __stdcall Marshal_DisconnectObject (IMarshal* pMarsh, ULONG dwReserved);


//------------------------------------------------------------------------------------------
//      IManagedObject methods for COM+ objects

interface IManagedObject;


HRESULT __stdcall ManagedObject_RemoteDispatchAutoDone(IManagedObject *pManaged, BSTR bstr,
                                                   BSTR* pBStrRet);
                                                   
HRESULT __stdcall ManagedObject_RemoteDispatchNotAutoDone(IManagedObject *pManaged, BSTR bstr,
                                                   BSTR* pBStrRet);
                                                   
HRESULT __stdcall ManagedObject_GetObjectIdentity(IManagedObject *pManaged, 
                                    BSTR* pBSTRGUID, DWORD* pAppDomainID,
                                    void** pCCW); 


HRESULT __stdcall ManagedObject_GetSerializedBuffer(IManagedObject *pManaged,
                                    BSTR* pBStr);

//------------------------------------------------------------------------------------------
//      IConnectionPointContainer methods for COM+ objects

interface IEnumConnectionPoints;

HRESULT __stdcall ConnectionPointContainer_EnumConnectionPoints(IUnknown* pUnk, 
                                    IEnumConnectionPoints **ppEnum);

HRESULT __stdcall ConnectionPointContainer_FindConnectionPoint(IUnknown* pUnk, 
                                    REFIID riid,
                                    IConnectionPoint **ppCP);

//------------------------------------------------------------------------------------------
//      IObjectSafety methods for COM+ objects

interface IObjectSafety;

HRESULT __stdcall ObjectSafety_GetInterfaceSafetyOptions(IUnknown* pUnk,
                                                         REFIID riid,
                                                         DWORD *pdwSupportedOptions,
                                                         DWORD *pdwEnabledOptions);

HRESULT __stdcall ObjectSafety_SetInterfaceSafetyOptions(IUnknown* pUnk,
                                                         REFIID riid,
                                                         DWORD dwOptionSetMask,
                                                         DWORD dwEnabledOptions);
//-------------------------------------------------------------------------
// IProvideClassInfo methods
HRESULT __stdcall ClassInfo_GetClassInfo(IUnknown* pUnk, 
                         ITypeInfo** ppTI  //Address of output variable that receives the type info.
                        );
//-------------------------------------------------------------------------
// ICCW methods
ULONG __stdcall ICCW_AddRefFromJupiter(IUnknown* pUnk);

ULONG __stdcall ICCW_ReleaseFromJupiter(IUnknown* pUnk);

HRESULT __stdcall ICCW_Peg(IUnknown* pUnk);

HRESULT __stdcall ICCW_Unpeg(IUnknown* pUnk);


#endif
