// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: MarshalNative.h
//

//
// FCall's for the Marshal class
//


#ifndef __MARSHALNATIVE_H__
#define __MARSHALNATIVE_H__

#include "fcall.h"

class MarshalNative
{
public:
    static FCDECL0(int, GetLastPInvokeError);
    static FCDECL1(void, SetLastPInvokeError, int error);

    static FCDECL2(LPVOID, GCHandleInternalAlloc, Object *obj, int type);
    static FCDECL1(FC_BOOL_RET, GCHandleInternalFree, OBJECTHANDLE handle);
    static FCDECL1(LPVOID, GCHandleInternalGet, OBJECTHANDLE handle);
    static FCDECL2(VOID, GCHandleInternalSet, OBJECTHANDLE handle, Object *obj);
    static FCDECL3(Object*, GCHandleInternalCompareExchange, OBJECTHANDLE handle, Object *obj, Object* oldObj);

#ifdef FEATURE_COMINTEROP
    //====================================================================
    // Checks whether there are RCWs from any context available for cleanup.
    //====================================================================
    static FCDECL0(FC_BOOL_RET, AreComObjectsAvailableForCleanup);
#endif // FEATURE_COMINTEROP
};

extern "C" SIZE_T QCALLTYPE MarshalNative_OffsetOf(FieldDesc* pFD);

extern "C" VOID QCALLTYPE MarshalNative_Prelink(MethodDesc * pMD);
extern "C" BOOL QCALLTYPE MarshalNative_IsBuiltInComSupported();

extern "C" BOOL QCALLTYPE MarshalNative_TryGetStructMarshalStub(void* enregisteredTypeHandle, PCODE* pStructMarshalStub, SIZE_T* pSize);
extern "C" INT32 QCALLTYPE MarshalNative_SizeOfHelper(QCall::TypeHandle t, BOOL throwIfNotMarshalable);

extern "C" void QCALLTYPE MarshalNative_GetDelegateForFunctionPointerInternal(PVOID FPtr, QCall::TypeHandle t, QCall::ObjectHandleOnStack retDelegate);
extern "C" PVOID QCALLTYPE MarshalNative_GetFunctionPointerForDelegateInternal(QCall::ObjectHandleOnStack delegate);

//====================================================================
// These methods convert between an HR and and a managed exception.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetExceptionForHR(INT32 errorCode, LPVOID errorInfo, QCall::ObjectHandleOnStack obj);
#ifdef FEATURE_COMINTEROP
extern "C" int32_t QCALLTYPE MarshalNative_GetHRForException(QCall::ObjectHandleOnStack obj);
#endif // FEATURE_COMINTEROP

extern "C" OBJECTHANDLE QCALLTYPE GCHandle_InternalAllocWithGCTransition(QCall::ObjectHandleOnStack obj, int type);
extern "C" void QCALLTYPE GCHandle_InternalFreeWithGCTransition(OBJECTHANDLE handle);

#ifdef _DEBUG
using IsInCooperativeGCMode_fn = BOOL(STDMETHODCALLTYPE*)(void);
extern "C" IsInCooperativeGCMode_fn QCALLTYPE MarshalNative_GetIsInCooperativeGCModeFunctionPointer();
#endif

#ifdef FEATURE_COMINTEROP
//====================================================================
// Create type for given CLSID.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetTypeFromCLSID(REFCLSID clsid, PCWSTR wszServer, QCall::ObjectHandleOnStack retType);

//====================================================================
// return the IUnknown* for an Object
//====================================================================
extern "C" IUnknown* QCALLTYPE MarshalNative_GetIUnknownForObject(QCall::ObjectHandleOnStack o);

//====================================================================
// return the IDispatch* for an Object
//====================================================================
extern "C" IDispatch* QCALLTYPE MarshalNative_GetIDispatchForObject(QCall::ObjectHandleOnStack o);

//====================================================================
// return the IUnknown* representing the interface for the Object
// Object o should support Type T
//====================================================================
extern "C" IUnknown* QCALLTYPE MarshalNative_GetComInterfaceForObject(QCall::ObjectHandleOnStack o, QCall::TypeHandle t, BOOL bEnableCustomizedQueryInterface);

//====================================================================
// return an Object for IUnknown
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetObjectForIUnknown(IUnknown* pUnk, QCall::ObjectHandleOnStack retObject);

//====================================================================
// return a unique cacheless Object for IUnknown
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetUniqueObjectForIUnknown(IUnknown* pUnk, QCall::ObjectHandleOnStack retObject);

//====================================================================
// return an Object for IUnknown, using the Type T,
//	NOTE:
//	Type T should be either a COM imported Type or a sub-type of COM imported Type
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetTypedObjectForIUnknown(IUnknown* pUnk, QCall::TypeHandle t, QCall::ObjectHandleOnStack retObject);

//====================================================================
// Create an object and aggregate it, then return the inner unknown.
//====================================================================
extern "C" IUnknown* QCALLTYPE MarshalNative_CreateAggregatedObject(IUnknown* pOuter, QCall::ObjectHandleOnStack o);

//====================================================================
// Free unused RCWs in the current COM+ context.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_CleanupUnusedObjectsInCurrentContext();

//====================================================================
// free the COM component and zombie this object
// further usage of this Object might throw an exception,
//====================================================================
extern "C" INT32 QCALLTYPE MarshalNative_ReleaseComObject(QCall::ObjectHandleOnStack objUNSAFE);
extern "C" void QCALLTYPE MarshalNative_FinalReleaseComObject(QCall::ObjectHandleOnStack objUNSAFE);

//====================================================================
// This method takes the given COM object and wraps it in an object
// of the specified type. The type must be derived from __ComObject.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_InternalCreateWrapperOfType(QCall::ObjectHandleOnStack o, QCall::TypeHandle t, QCall::ObjectHandleOnStack retObject);

//====================================================================
// check if the type is visible from COM.
//====================================================================
extern "C" BOOL QCALLTYPE MarshalNative_IsTypeVisibleFromCom(QCall::TypeHandle t);

//====================================================================
// These methods convert OLE variants to and from objects.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetNativeVariantForObject(QCall::ObjectHandleOnStack ObjUNSAFE, LPVOID pDestNativeVariant);
extern "C" void QCALLTYPE MarshalNative_GetObjectForNativeVariant(LPVOID pSrcNativeVariant, QCall::ObjectHandleOnStack retObject);
extern "C" void QCALLTYPE MarshalNative_GetObjectsForNativeVariants(VARIANT* aSrcNativeVariant, int cVars, QCall::ObjectHandleOnStack retArray);

//====================================================================
// These methods are used to map COM slots to method info's.
//====================================================================
extern "C" INT32 QCALLTYPE MarshalNative_GetStartComSlot(QCall::TypeHandle t);
extern "C" INT32 QCALLTYPE MarshalNative_GetEndComSlot(QCall::TypeHandle t);

extern "C" VOID QCALLTYPE MarshalNative_ChangeWrapperHandleStrength(QCall::ObjectHandleOnStack otp, BOOL fIsWeak);
#endif // FEATURE_COMINTEROP

#endif
