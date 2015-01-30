//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 
// File: MarshalNative.h
//

//
// FCall's for the Marshal class
//


#ifndef __MARSHALNATIVE_H__
#define __MARSHALNATIVE_H__

#include "fcall.h"

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
    static FCDECL2(void, ThrowExceptionForHR, INT32 errorCode, LPVOID errorInfo);
    static FCDECL2(Object *, GetExceptionForHR, INT32 errorCode, LPVOID errorInfo);
    static FCDECL1(int, GetHRForException, Object* eUNSAFE);
    static FCDECL1(int, GetHRForException_WinRT, Object* eUNSAFE);

    static FCDECL4(void, CopyToNative, Object* psrcUNSAFE, INT32 startindex, LPVOID pdst, INT32 length);
    static FCDECL4(void, CopyToManaged, LPVOID psrc, Object* pdstUNSAFE, INT32 startindex, INT32 length);
    static FCDECL2(UINT32, SizeOfClass, ReflectClassBaseObject* refClass, CLR_BOOL throwIfNotMarshalable);

    static FCDECL2(LPVOID, FCUnsafeAddrOfPinnedArrayElement, ArrayBase *arr, INT32 index);

    static FCDECL1(UINT32, OffsetOfHelper, ReflectFieldObject* pFieldUNSAFE);
    static FCDECL0(int, GetLastWin32Error);
    static FCDECL1(void, SetLastWin32Error, int error);
    static FCDECL1(INT32, CalculateCount, ArrayWithOffsetData* pRef);
    
    static FCDECL3(VOID, StructureToPtr, Object* pObjUNSAFE, LPVOID ptr, CLR_BOOL fDeleteOld);
    static FCDECL3(VOID, PtrToStructureHelper, LPVOID ptr, Object* pObjIn, CLR_BOOL allowValueClasses);
    static FCDECL2(VOID, DestroyStructure, LPVOID ptr, ReflectClassBaseObject* refClassUNSAFE);

    //====================================================================
    // map a fiber cookie from the hosting APIs into a managed Thread object
    //====================================================================
    static FCDECL1(THREADBASEREF, GetThreadFromFiberCookie, int cookie);

    static FCDECL3(LPVOID, GetUnmanagedThunkForManagedMethodPtr, LPVOID pfnMethodToWrap, PCCOR_SIGNATURE pbSignature, ULONG cbSignature);
    static FCDECL3(LPVOID, GetManagedThunkForUnmanagedMethodPtr, LPVOID pfnMethodToWrap, PCCOR_SIGNATURE pbSignature, ULONG cbSignature);

    static FCDECL0(UINT32, GetSystemMaxDBCSCharSize);

    static FCDECL2(LPVOID, GCHandleInternalAlloc, Object *obj, int type);
    static FCDECL1(VOID, GCHandleInternalFree, OBJECTHANDLE handle);
    static FCDECL1(LPVOID, GCHandleInternalGet, OBJECTHANDLE handle);
    static FCDECL3(VOID, GCHandleInternalSet, OBJECTHANDLE handle, Object *obj, CLR_BOOL isPinned);
    static FCDECL4(Object*, GCHandleInternalCompareExchange, OBJECTHANDLE handle, Object *obj, Object* oldObj, CLR_BOOL isPinned);
    static FCDECL1(LPVOID, GCHandleInternalAddrOfPinnedObject, OBJECTHANDLE handle);
    static FCDECL1(VOID, GCHandleInternalCheckDomain, OBJECTHANDLE handle);
    static FCDECL1(INT32, GCHandleInternalGetHandleType, OBJECTHANDLE handle);

    static FCDECL2(Object*, GetDelegateForFunctionPointerInternal, LPVOID FPtr, ReflectClassBaseObject* refTypeUNSAFE);
    static FCDECL1(LPVOID, GetFunctionPointerForDelegateInternal, Object* refDelegateUNSAFE);

#ifdef FEATURE_COMINTEROP
    //====================================================================
    // map GUID to Type
    //====================================================================	
    static FCDECL1(Object*, GetLoadedTypeForGUID, GUID* pGuid);

    //====================================================================
    // map Type to ITypeInfo*
    //====================================================================
    static FCDECL1(ITypeInfo*, GetITypeInfoForType, ReflectClassBaseObject* refClassUNSAFE);

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
    static FCDECL1(Object*, GetObjectForIUnknown, IUnknown* pUnk);

    //====================================================================
    // return a unique cacheless Object for IUnknown
    //====================================================================
    static FCDECL1(Object*, GetUniqueObjectForIUnknown, IUnknown* pUnk);

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
    // IUnknown Helpers
    //====================================================================
    static FCDECL3(HRESULT, QueryInterface, IUnknown* pUnk, REFGUID iid, void** ppv);
    static FCDECL1(ULONG, AddRef, IUnknown* pUnk);
    static FCDECL1(ULONG, Release, IUnknown* pUnk);

    //====================================================================
    // These methods convert OLE variants to and from objects.
    //====================================================================
    static FCDECL2(void, GetNativeVariantForObject, Object* ObjUNSAFE, LPVOID pDestNativeVariant);
    static FCDECL1(Object*, GetObjectForNativeVariant, LPVOID pSrcNativeVariant);
    static FCDECL2(Object*, GetObjectsForNativeVariants, VARIANT* aSrcNativeVariant, int cVars);

    //====================================================================
    // This method generates a guid for the specified type.
    //====================================================================
    static FCDECL2(void, DoGenerateGuidForType, GUID * result, ReflectClassBaseObject* refTypeUNSAFE);

    //====================================================================
    // Methods to retrieve information from TypeLibs and TypeInfos.
    //====================================================================
    static FCDECL2(void, DoGetTypeLibGuid, GUID * result, Object* refTlbUNSAFE);
    static FCDECL1(LCID, GetTypeLibLcid, Object* refTlbUNSAFE);
    static FCDECL3(void, GetTypeLibVersion, Object* refTlbUNSAFE, int *pMajor, int *pMinor);
    static FCDECL2(void, DoGetTypeInfoGuid, GUID * result, Object* refTypeInfoUNSAFE);

    //====================================================================
    // Given a assembly, return the TLBID that will be generated for the
    // typelib exported from the assembly.
    //====================================================================
    static FCDECL2(void, DoGetTypeLibGuidForAssembly, GUID * result, AssemblyBaseObject* refAsmUNSAFE);

    //====================================================================
    // Given a assembly, return the version number of the type library
    // that would be exported from the assembly.
    //====================================================================
    static FCDECL3(void, GetTypeLibVersionForAssembly, AssemblyBaseObject* refAsmUNSAFE, INT32 *pMajorVersion, INT32 *pMinorVersion);

    //====================================================================
    // These methods are used to map COM slots to method info's.
    //====================================================================
    static FCDECL1(int, GetStartComSlot, ReflectClassBaseObject* tUNSAFE);
    static FCDECL1(int, GetEndComSlot, ReflectClassBaseObject* tUNSAFE);
    static FCDECL3(Object*, GetMethodInfoForComSlot, ReflectClassBaseObject* tUNSAFE, INT32 slot, ComMemberType* pMemberType);

    static FCDECL1(int, GetComSlotForMethodInfo, ReflectMethodObject* pMethodUNSAFE);

    static FCDECL2(FC_BOOL_RET, SwitchCCW, Object* oldtpUNSAFE, Object* newtpUNSAFE);
    static FCDECL1(Object*, WrapIUnknownWithComObject, IUnknown* pUnk);

    static FCDECL2(void, ChangeWrapperHandleStrength, Object* orefUNSAFE, CLR_BOOL fIsWeak);
    static FCDECL2(void, InitializeWrapperForWinRT, Object *unsafe_pThis, IUnknown **ppUnk);
    static FCDECL2(void, InitializeManagedWinRTFactoryObject, Object *unsafe_pThis, ReflectClassBaseObject *unsafe_pType);
    static FCDECL1(Object *, MarshalNative::GetNativeActivationFactory, ReflectClassBaseObject *unsafe_pType);
    static void QCALLTYPE GetInspectableIIDs(QCall::ObjectHandleOnStack hobj, QCall::ObjectHandleOnStack retArrayGuids);
    static void QCALLTYPE GetCachedWinRTTypes(QCall::ObjectHandleOnStack hadObj, int * epoch, QCall::ObjectHandleOnStack retArrayMT);
    static void QCALLTYPE GetCachedWinRTTypeByIID(QCall::ObjectHandleOnStack hadObj, GUID iid, void * * ppMT);

private:
    static int GetComSlotInfo(MethodTable *pMT, MethodTable **ppDefItfMT);
    static BOOL IsObjectInContext(OBJECTREF *pObj);
#endif // FEATURE_COMINTEROP
};

#endif
