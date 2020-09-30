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

#define MAX_UTF8_CHAR_SIZE 3

//!!! Must be kept in sync with ArrayWithOffset class layout.
struct ArrayWithOffsetData
{
    BASEARRAYREF    m_Array;
    INT32           m_cbOffset;
    INT32           m_cbCount;
};


#ifdef FEATURE_COMINTEROP
enum ComMemberType
{
    CMT_Method              = 0,
    CMT_PropGet             = 1,
    CMT_PropSet             = 2
};
#endif // FEATURE_COMINTEROP

class MarshalNative
{
public:
    static INT32 QCALLTYPE NumParamBytes(MethodDesc * pMD);
    static VOID QCALLTYPE Prelink(MethodDesc * pMD);

    //====================================================================
    // These methods convert between an HR and and a managed exception.
    //====================================================================
    static FCDECL2(Object *, GetExceptionForHR, INT32 errorCode, LPVOID errorInfo);
    static FCDECL1(int, GetHRForException, Object* eUNSAFE);

    static FCDECL2(UINT32, SizeOfClass, ReflectClassBaseObject* refClass, CLR_BOOL throwIfNotMarshalable);

    static FCDECL1(UINT32, OffsetOfHelper, ReflectFieldObject* pFieldUNSAFE);
    static FCDECL0(int, GetLastWin32Error);
    static FCDECL1(void, SetLastWin32Error, int error);

    static FCDECL3(VOID, StructureToPtr, Object* pObjUNSAFE, LPVOID ptr, CLR_BOOL fDeleteOld);
    static FCDECL3(VOID, PtrToStructureHelper, LPVOID ptr, Object* pObjIn, CLR_BOOL allowValueClasses);
    static FCDECL2(VOID, DestroyStructure, LPVOID ptr, ReflectClassBaseObject* refClassUNSAFE);

    static FCDECL1(FC_BOOL_RET, IsPinnable, Object* obj);

    static FCDECL2(LPVOID, GCHandleInternalAlloc, Object *obj, int type);
    static FCDECL1(VOID, GCHandleInternalFree, OBJECTHANDLE handle);
    static FCDECL1(LPVOID, GCHandleInternalGet, OBJECTHANDLE handle);
    static FCDECL2(VOID, GCHandleInternalSet, OBJECTHANDLE handle, Object *obj);
    static FCDECL3(Object*, GCHandleInternalCompareExchange, OBJECTHANDLE handle, Object *obj, Object* oldObj);

    static FCDECL2(Object*, GetDelegateForFunctionPointerInternal, LPVOID FPtr, ReflectClassBaseObject* refTypeUNSAFE);
    static FCDECL1(LPVOID, GetFunctionPointerForDelegateInternal, Object* refDelegateUNSAFE);

#ifdef FEATURE_COMINTEROP
    //====================================================================
    // return the IUnknown* for an Object
    //====================================================================
    static FCDECL2(IUnknown*, GetIUnknownForObjectNative, Object* orefUNSAFE, CLR_BOOL fOnlyInContext);

    //====================================================================
    // return the raw IUnknown* for a COM Object not related to current
    // context
    // Does not AddRef the returned pointer
    //====================================================================
    static FCDECL1(IUnknown*, GetRawIUnknownForComObjectNoAddRef, Object* orefUNSAFE);

    //====================================================================
    // return the IDispatch* for an Object
    //====================================================================
    static FCDECL2(IDispatch*, GetIDispatchForObjectNative, Object* orefUNSAFE, CLR_BOOL fOnlyInContext);

    //====================================================================
    // return the IUnknown* representing the interface for the Object
    // Object o should support Type T
    //====================================================================
    static FCDECL4(IUnknown*, GetComInterfaceForObjectNative, Object* orefUNSAFE, ReflectClassBaseObject* refClassUNSAFE, CLR_BOOL fOnlyInContext, CLR_BOOL bEnableCustomizedQueryInterface);

    //====================================================================
    // return an Object for IUnknown
    //====================================================================
    static FCDECL1(Object*, GetObjectForIUnknownNative, IUnknown* pUnk);

    //====================================================================
    // return a unique cacheless Object for IUnknown
    //====================================================================
    static FCDECL1(Object*, GetUniqueObjectForIUnknownNative, IUnknown* pUnk);

    //====================================================================
    // return a unique cacheless Object for IUnknown
    //====================================================================
    static FCDECL1(Object*, GetUniqueObjectForIUnknownWithoutUnboxing, IUnknown* pUnk);

    //====================================================================
    // return an Object for IUnknown, using the Type T,
    //	NOTE:
    //	Type T should be either a COM imported Type or a sub-type of COM imported Type
    //====================================================================
    static FCDECL2(Object*, GetTypedObjectForIUnknown, IUnknown* pUnk, ReflectClassBaseObject* refClassUNSAFE);

    //====================================================================
    // Free unused RCWs in the current COM+ context.
    //====================================================================
    static FCDECL0(void, CleanupUnusedObjectsInCurrentContext);

    //====================================================================
    // Checks whether there are RCWs from any context available for cleanup.
    //====================================================================
    static FCDECL0(FC_BOOL_RET, AreComObjectsAvailableForCleanup);

    //====================================================================
    // Create an object and aggregate it, then return the inner unknown.
    //====================================================================
    static FCDECL2(IUnknown*, CreateAggregatedObject, IUnknown* pOuter, Object* refObjUNSAFE);

    //====================================================================
    // check if the object is classic COM component
    //====================================================================
    static FCDECL1(FC_BOOL_RET, IsComObject, Object* objUNSAFE);

    //====================================================================
    // free the COM component and zombie this object
    // further usage of this Object might throw an exception,
    //====================================================================
    static FCDECL1(INT32, ReleaseComObject, Object* objUNSAFE);
    static FCDECL1(void, FinalReleaseComObject, Object* objUNSAFE);

    //====================================================================
    // This method takes the given COM object and wraps it in an object
    // of the specified type. The type must be derived from __ComObject.
    //====================================================================
    static FCDECL2(Object*, InternalCreateWrapperOfType, Object* objUNSAFE, ReflectClassBaseObject* refClassUNSAFE);

    //====================================================================
    // check if the type is visible from COM.
    //====================================================================
    static FCDECL1(FC_BOOL_RET, IsTypeVisibleFromCom, ReflectClassBaseObject* refClassUNSAFE);

    //====================================================================
    // These methods convert OLE variants to and from objects.
    //====================================================================
    static FCDECL2(void, GetNativeVariantForObject, Object* ObjUNSAFE, LPVOID pDestNativeVariant);
    static FCDECL1(Object*, GetObjectForNativeVariant, LPVOID pSrcNativeVariant);
    static FCDECL2(Object*, GetObjectsForNativeVariants, VARIANT* aSrcNativeVariant, int cVars);

    //====================================================================
    // These methods are used to map COM slots to method info's.
    //====================================================================
    static FCDECL1(int, GetStartComSlot, ReflectClassBaseObject* tUNSAFE);
    static FCDECL1(int, GetEndComSlot, ReflectClassBaseObject* tUNSAFE);

    static FCDECL1(Object*, WrapIUnknownWithComObject, IUnknown* pUnk);

    static FCDECL2(void, ChangeWrapperHandleStrength, Object* orefUNSAFE, CLR_BOOL fIsWeak);

    //====================================================================
    // Create type for given CLSID.
    //====================================================================
    static void QCALLTYPE GetTypeFromCLSID(REFCLSID clsid, PCWSTR wszServer, QCall::ObjectHandleOnStack retType);

private:
    static int GetComSlotInfo(MethodTable *pMT, MethodTable **ppDefItfMT);
    static BOOL IsObjectInContext(OBJECTREF *pObj);
#endif // FEATURE_COMINTEROP
};

// Check that the supplied object is valid to put in a pinned handle,
// throwing an exception if not.
void ValidatePinnedObject(OBJECTREF obj);

#endif
