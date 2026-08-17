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
    FCDECL0(static int, GetLastPInvokeError);
    FCDECL1(static void, SetLastPInvokeError, int error);

    FCDECL2(static LPVOID, GCHandleInternalAlloc, Object *obj, int type);
    FCDECL1(static FC_BOOL_RET, GCHandleInternalFree, OBJECTHANDLE handle);
    FCDECL1(static LPVOID, GCHandleInternalGet, OBJECTHANDLE handle);
#ifdef FEATURE_JAVAMARSHAL
    FCDECL2(static FC_BOOL_RET, GCHandleInternalTryGetBridgeWait, OBJECTHANDLE handle, Object** pObjResult);
#endif
    FCDECL2(static VOID, GCHandleInternalSet, OBJECTHANDLE handle, Object *obj);
    FCDECL3(static Object*, GCHandleInternalCompareExchange, OBJECTHANDLE handle, Object *obj, Object* oldObj);

#ifdef FEATURE_COMINTEROP
    //====================================================================
    // Checks whether there are RCWs from any context available for cleanup.
    //====================================================================
    FCDECL0(static FC_BOOL_RET, AreComObjectsAvailableForCleanup);
#endif // FEATURE_COMINTEROP
};

extern "C" SIZE_T QCALLTYPE MarshalNative_OffsetOf(FieldDesc* pFD, QCallException* qcallError);

extern "C" VOID QCALLTYPE MarshalNative_Prelink(MethodDesc * pMD, QCallException* qcallError);
extern "C" BOOL QCALLTYPE MarshalNative_IsBuiltInComSupported(QCallException* qcallError);

extern "C" BOOL QCALLTYPE MarshalNative_HasLayout(QCall::TypeHandle t, BOOL* pIsBlittable, DWORD* pNativeSize, QCallException* qcallError);
extern "C" INT32 QCALLTYPE MarshalNative_SizeOfHelper(QCall::TypeHandle t, BOOL throwIfNotMarshalable, QCallException* qcallError);

extern "C" void QCALLTYPE MarshalNative_GetDelegateForFunctionPointerInternal(PVOID FPtr, QCall::TypeHandle t, QCall::ObjectHandleOnStack retDelegate, QCallException* qcallError);
extern "C" PVOID QCALLTYPE MarshalNative_GetFunctionPointerForDelegateInternal(QCall::ObjectHandleOnStack delegate, QCallException* qcallError);

//====================================================================
// These methods convert between an HR and and a managed exception.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetExceptionForHR(INT32 errorCode, LPVOID errorInfo, QCall::ObjectHandleOnStack obj, QCallException* qcallError);
#ifdef FEATURE_COMINTEROP
extern "C" int32_t QCALLTYPE MarshalNative_GetHRForException(QCall::ObjectHandleOnStack obj, QCallException* qcallError);
#endif // FEATURE_COMINTEROP

extern "C" OBJECTHANDLE QCALLTYPE GCHandle_InternalAllocWithGCTransition(QCall::ObjectHandleOnStack obj, int type, QCallException* qcallError);
extern "C" void QCALLTYPE GCHandle_InternalFreeWithGCTransition(OBJECTHANDLE handle, QCallException* qcallError);
#ifdef FEATURE_JAVAMARSHAL
extern "C" void QCALLTYPE GCHandle_InternalGetBridgeWait(OBJECTHANDLE handle, QCall::ObjectHandleOnStack result, QCallException* qcallError);
#endif

#ifdef _DEBUG
using IsInCooperativeGCMode_fn = BOOL(STDMETHODCALLTYPE*)(void);
extern "C" IsInCooperativeGCMode_fn QCALLTYPE MarshalNative_GetIsInCooperativeGCModeFunctionPointer(QCallException* qcallError);
#endif

#ifdef FEATURE_COMINTEROP
//====================================================================
// Create type for given CLSID.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetTypeFromCLSID(REFCLSID clsid, PCWSTR wszServer, QCall::ObjectHandleOnStack retType, QCallException* qcallError);

//====================================================================
// return the IUnknown* for an Object
//====================================================================
extern "C" IUnknown* QCALLTYPE MarshalNative_GetIUnknownForObject(QCall::ObjectHandleOnStack o, QCallException* qcallError);

//====================================================================
// return the IDispatch* for an Object
//====================================================================
extern "C" IDispatch* QCALLTYPE MarshalNative_GetIDispatchForObject(QCall::ObjectHandleOnStack o, QCallException* qcallError);

//====================================================================
// return the IUnknown* or IDispatch* for an Object.
//====================================================================
extern "C" void* QCALLTYPE MarshalNative_GetIUnknownOrIDispatchForObject(QCall::ObjectHandleOnStack o, BOOL* isIDispatch, QCallException* qcallError);

//====================================================================
// return the IUnknown* representing the interface for the Object
// Object o should support Type T
//====================================================================
extern "C" IUnknown* QCALLTYPE MarshalNative_GetComInterfaceForObject(QCall::ObjectHandleOnStack o, QCall::TypeHandle t, BOOL bEnableCustomizedQueryInterface, QCallException* qcallError);

//====================================================================
// return an Object for IUnknown
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetObjectForIUnknown(IUnknown* pUnk, QCall::ObjectHandleOnStack retObject, QCallException* qcallError);

//====================================================================
// return a unique cacheless Object for IUnknown
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetUniqueObjectForIUnknown(IUnknown* pUnk, QCall::ObjectHandleOnStack retObject, QCallException* qcallError);

//====================================================================
// return an Object for IUnknown, using the Type T,
//	NOTE:
//	Type T should be either a COM imported Type or a sub-type of COM imported Type
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetTypedObjectForIUnknown(IUnknown* pUnk, QCall::TypeHandle t, QCall::ObjectHandleOnStack retObject, QCallException* qcallError);

//====================================================================
// Create an object and aggregate it, then return the inner unknown.
//====================================================================
extern "C" IUnknown* QCALLTYPE MarshalNative_CreateAggregatedObject(IUnknown* pOuter, QCall::ObjectHandleOnStack o, QCallException* qcallError);

//====================================================================
// Free unused RCWs in the current CLR context.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_CleanupUnusedObjectsInCurrentContext(QCallException* qcallError);

//====================================================================
// free the COM component and zombie this object
// further usage of this Object might throw an exception,
//====================================================================
extern "C" INT32 QCALLTYPE MarshalNative_ReleaseComObject(QCall::ObjectHandleOnStack objUNSAFE, QCallException* qcallError);
extern "C" void QCALLTYPE MarshalNative_FinalReleaseComObject(QCall::ObjectHandleOnStack objUNSAFE, QCallException* qcallError);

//====================================================================
// This method takes the given COM object and wraps it in an object
// of the specified type. The type must be derived from __ComObject.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_InternalCreateWrapperOfType(QCall::ObjectHandleOnStack o, QCall::TypeHandle t, QCall::ObjectHandleOnStack retObject, QCallException* qcallError);

//====================================================================
// check if the type is visible from COM.
//====================================================================
extern "C" BOOL QCALLTYPE MarshalNative_IsTypeVisibleFromCom(QCall::TypeHandle t, QCallException* qcallError);

//====================================================================
// These methods convert OLE variants to and from objects.
//====================================================================
extern "C" void QCALLTYPE MarshalNative_GetNativeVariantForObject(QCall::ObjectHandleOnStack ObjUNSAFE, LPVOID pDestNativeVariant, QCallException* qcallError);
extern "C" void QCALLTYPE MarshalNative_GetObjectForNativeVariant(LPVOID pSrcNativeVariant, QCall::ObjectHandleOnStack retObject, QCallException* qcallError);
extern "C" void QCALLTYPE MarshalNative_GetObjectsForNativeVariants(VARIANT* aSrcNativeVariant, int cVars, QCall::ObjectHandleOnStack retArray, QCallException* qcallError);

//====================================================================
// These methods are used to map COM slots to method info's.
//====================================================================
extern "C" INT32 QCALLTYPE MarshalNative_GetStartComSlot(QCall::TypeHandle t, QCallException* qcallError);
extern "C" INT32 QCALLTYPE MarshalNative_GetEndComSlot(QCall::TypeHandle t, QCallException* qcallError);

extern "C" VOID QCALLTYPE MarshalNative_ChangeWrapperHandleStrength(QCall::ObjectHandleOnStack otp, BOOL fIsWeak, QCallException* qcallError);
#endif // FEATURE_COMINTEROP

#endif
