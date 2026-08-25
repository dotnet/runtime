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

extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_OffsetOf(FieldDesc* pFD, SIZE_T* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_Prelink(MethodDesc * pMD);
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_IsBuiltInComSupported(BOOL* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_HasLayout(QCall::TypeHandle t, BOOL* pIsBlittable, DWORD* pNativeSize, BOOL* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_SizeOfHelper(QCall::TypeHandle t, BOOL throwIfNotMarshalable, INT32* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetDelegateForFunctionPointerInternal(PVOID FPtr, QCall::TypeHandle t, QCall::ObjectHandleOnStack retDelegate);
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetFunctionPointerForDelegateInternal(QCall::ObjectHandleOnStack delegate, PVOID* pReturnValue);

//====================================================================
// These methods convert between an HR and and a managed exception.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetExceptionForHR(INT32 errorCode, LPVOID errorInfo, QCall::ObjectHandleOnStack obj);
#ifdef FEATURE_COMINTEROP
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetHRForException(QCall::ObjectHandleOnStack obj, int32_t* pReturnValue);
#endif // FEATURE_COMINTEROP

extern "C" QCallExceptionStatus QCALLTYPE GCHandle_InternalAllocWithGCTransition(QCall::ObjectHandleOnStack obj, int type, OBJECTHANDLE* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE GCHandle_InternalFreeWithGCTransition(OBJECTHANDLE handle);
#ifdef FEATURE_JAVAMARSHAL
extern "C" QCallExceptionStatus QCALLTYPE GCHandle_InternalGetBridgeWait(OBJECTHANDLE handle, QCall::ObjectHandleOnStack result);
#endif

#ifdef _DEBUG
using IsInCooperativeGCMode_fn = BOOL(STDMETHODCALLTYPE*)(void);
extern "C" IsInCooperativeGCMode_fn QCALLTYPE MarshalNative_GetIsInCooperativeGCModeFunctionPointer();
#endif

#ifdef FEATURE_COMINTEROP
//====================================================================
// Create type for given CLSID.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetTypeFromCLSID(REFCLSID clsid, PCWSTR wszServer, QCall::ObjectHandleOnStack retType);

//====================================================================
// return the IUnknown* for an Object
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetIUnknownForObject(QCall::ObjectHandleOnStack o, IUnknown** pReturnValue);

//====================================================================
// return the IDispatch* for an Object
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetIDispatchForObject(QCall::ObjectHandleOnStack o, IDispatch** pReturnValue);

//====================================================================
// return the IUnknown* or IDispatch* for an Object.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetIUnknownOrIDispatchForObject(QCall::ObjectHandleOnStack o, BOOL* isIDispatch, void** pReturnValue);

//====================================================================
// return the IUnknown* representing the interface for the Object
// Object o should support Type T
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetComInterfaceForObject(QCall::ObjectHandleOnStack o, QCall::TypeHandle t, BOOL bEnableCustomizedQueryInterface, IUnknown** pReturnValue);

//====================================================================
// return an Object for IUnknown
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetObjectForIUnknown(IUnknown* pUnk, QCall::ObjectHandleOnStack retObject);

//====================================================================
// return a unique cacheless Object for IUnknown
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetUniqueObjectForIUnknown(IUnknown* pUnk, QCall::ObjectHandleOnStack retObject);

//====================================================================
// return an Object for IUnknown, using the Type T,
//	NOTE:
//	Type T should be either a COM imported Type or a sub-type of COM imported Type
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetTypedObjectForIUnknown(IUnknown* pUnk, QCall::TypeHandle t, QCall::ObjectHandleOnStack retObject);

//====================================================================
// Create an object and aggregate it, then return the inner unknown.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_CreateAggregatedObject(IUnknown* pOuter, QCall::ObjectHandleOnStack o, IUnknown** pReturnValue);

//====================================================================
// Free unused RCWs in the current CLR context.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_CleanupUnusedObjectsInCurrentContext();

//====================================================================
// free the COM component and zombie this object
// further usage of this Object might throw an exception,
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_ReleaseComObject(QCall::ObjectHandleOnStack objUNSAFE, INT32* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_FinalReleaseComObject(QCall::ObjectHandleOnStack objUNSAFE);

//====================================================================
// This method takes the given COM object and wraps it in an object
// of the specified type. The type must be derived from __ComObject.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_InternalCreateWrapperOfType(QCall::ObjectHandleOnStack o, QCall::TypeHandle t, QCall::ObjectHandleOnStack retObject);

//====================================================================
// check if the type is visible from COM.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_IsTypeVisibleFromCom(QCall::TypeHandle t, BOOL* pReturnValue);

//====================================================================
// These methods convert OLE variants to and from objects.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetNativeVariantForObject(QCall::ObjectHandleOnStack ObjUNSAFE, LPVOID pDestNativeVariant);
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetObjectForNativeVariant(LPVOID pSrcNativeVariant, QCall::ObjectHandleOnStack retObject);
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetObjectsForNativeVariants(VARIANT* aSrcNativeVariant, int cVars, QCall::ObjectHandleOnStack retArray);

//====================================================================
// These methods are used to map COM slots to method info's.
//====================================================================
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetStartComSlot(QCall::TypeHandle t, INT32* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_GetEndComSlot(QCall::TypeHandle t, INT32* pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE MarshalNative_ChangeWrapperHandleStrength(QCall::ObjectHandleOnStack otp, BOOL fIsWeak);
#endif // FEATURE_COMINTEROP

#endif
