// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCHANDLEUTILITIES_H_
#define _GCHANDLEUTILITIES_H_

#include "gcinterface.h"

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
#include <weakreference.h>
#endif

extern "C" IGCHandleManager* g_pGCHandleManager;

class GCHandleUtilities
{
public:
    // Retrieves the GC handle table.
    static IGCHandleManager* GetGCHandleManager()
    {
        LIMITED_METHOD_CONTRACT;

        assert(g_pGCHandleManager != nullptr);
        return g_pGCHandleManager;
    }

private:
    // This class should never be instantiated.
    GCHandleUtilities() = delete;
};

void ValidateHandleAssignment(OBJECTHANDLE handle, OBJECTREF objRef);
void DiagHandleCreated(OBJECTHANDLE handle, OBJECTREF object);
void DiagHandleDestroyed(OBJECTHANDLE handle);

// Given a handle, returns an OBJECTREF for the object it refers to.
inline OBJECTREF ObjectFromHandle(OBJECTHANDLE handle)
{
    _ASSERTE(handle);

#if defined(_DEBUG_IMPL) && !defined(DACCESS_COMPILE)
    OBJECTREF objRef = ObjectToOBJECTREF(*(Object**)handle);

    VALIDATEOBJECTREF(objRef);
#endif // defined(_DEBUG_IMPL) && !defined(DACCESS_COMPILE)

    // Wrap the raw OBJECTREF and return it
    return UNCHECKED_OBJECTREF_TO_OBJECTREF(*PTR_UNCHECKED_OBJECTREF(handle));
}

// Quick inline check for whether a handle is null
inline BOOL IsHandleNullUnchecked(OBJECTHANDLE handle)
{
    LIMITED_METHOD_CONTRACT;

    return (handle == NULL || (*(_UNCHECKED_OBJECTREF *)handle) == NULL);
}

inline BOOL ObjectHandleIsNull(OBJECTHANDLE handle)
{
    LIMITED_METHOD_CONTRACT;

    return *(Object **)handle == NULL;
}

#ifndef DACCESS_COMPILE

// Handle creation convenience functions
inline OBJECTHANDLE CreateHandleCommon(IGCHandleStore* store, OBJECTREF object, HandleType type)
{
    OBJECTHANDLE hnd = store->CreateHandleOfType(OBJECTREFToObject(object), type);
    if (!hnd)
    {
        COMPlusThrowOM();
    }

    DiagHandleCreated(hnd, object);
    return hnd;
}

inline OBJECTHANDLE CreateHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_DEFAULT);
}

inline OBJECTHANDLE CreateWeakHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_WEAK_DEFAULT);
}

inline OBJECTHANDLE CreateShortWeakHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_WEAK_SHORT);
}

inline OBJECTHANDLE CreateLongWeakHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_WEAK_LONG);
}

inline OBJECTHANDLE CreateStrongHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_STRONG);
}

inline OBJECTHANDLE CreatePinningHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_PINNED);
}

inline OBJECTHANDLE CreateAsyncPinningHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_ASYNCPINNED);
}

inline OBJECTHANDLE CreateRefcountedHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_REFCOUNTED);
}

inline OBJECTHANDLE CreateSizedRefHandle(IGCHandleStore* store, OBJECTREF object)
{
    return CreateHandleCommon(store, object, HNDTYPE_SIZEDREF);
}

inline OBJECTHANDLE CreateSizedRefHandle(IGCHandleStore* store, OBJECTREF object, int heapToAffinitizeTo)
{
    OBJECTHANDLE hnd = store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_SIZEDREF, heapToAffinitizeTo);
    if (!hnd)
    {
        COMPlusThrowOM();
    }

    DiagHandleCreated(hnd, object);
    return hnd;
}

inline OBJECTHANDLE CreateDependentHandle(IGCHandleStore* store, OBJECTREF primary, OBJECTREF secondary)
{
    OBJECTHANDLE hnd = store->CreateDependentHandle(OBJECTREFToObject(primary), OBJECTREFToObject(secondary));
    if (!hnd)
    {
        COMPlusThrowOM();
    }

    DiagHandleCreated(hnd, primary);
    return hnd;
}

// Global handle creation convenience functions
inline OBJECTHANDLE CreateGlobalHandleCommon(OBJECTREF object, HandleType type)
{
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);
    OBJECTHANDLE hnd = GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), type);
    if (!hnd)
    {
        COMPlusThrowOM();
    }

    DiagHandleCreated(hnd, object);
    return hnd;

}

inline OBJECTHANDLE CreateGlobalHandle(OBJECTREF object)
{
    return CreateGlobalHandleCommon(object, HNDTYPE_DEFAULT);
}

inline OBJECTHANDLE CreateGlobalWeakHandle(OBJECTREF object)
{
    return CreateGlobalHandleCommon(object, HNDTYPE_WEAK_DEFAULT);
}

inline OBJECTHANDLE CreateGlobalShortWeakHandle(OBJECTREF object)
{
    return CreateGlobalHandleCommon(object, HNDTYPE_WEAK_SHORT);
}

inline OBJECTHANDLE CreateGlobalLongWeakHandle(OBJECTREF object)
{
    return CreateGlobalHandleCommon(object, HNDTYPE_WEAK_LONG);
}

inline OBJECTHANDLE CreateGlobalStrongHandle(OBJECTREF object)
{
    return CreateGlobalHandleCommon(object, HNDTYPE_STRONG);
}

inline OBJECTHANDLE CreateGlobalPinningHandle(OBJECTREF object)
{
    return CreateGlobalHandleCommon(object, HNDTYPE_PINNED);
}

inline OBJECTHANDLE CreateGlobalRefcountedHandle(OBJECTREF object)
{
    return CreateGlobalHandleCommon(object, HNDTYPE_REFCOUNTED);
}

// Creates a variable-strength handle
inline OBJECTHANDLE CreateVariableHandle(IGCHandleStore* store, OBJECTREF object, uint32_t type)
{
    OBJECTHANDLE hnd = store->CreateHandleWithExtraInfo(OBJECTREFToObject(object), HNDTYPE_VARIABLE, (void*)((uintptr_t)type));
    if (!hnd)
    {
        COMPlusThrowOM();
    }

    DiagHandleCreated(hnd, object);
    return hnd;
}

// Handle object manipulation convenience functions

inline void StoreObjectInHandle(OBJECTHANDLE handle, OBJECTREF object)
{
    ValidateHandleAssignment(handle, object);

    GCHandleUtilities::GetGCHandleManager()->StoreObjectInHandle(handle, OBJECTREFToObject(object));
}

inline bool StoreFirstObjectInHandle(OBJECTHANDLE handle, OBJECTREF object)
{
    ValidateHandleAssignment(handle, object);

    return GCHandleUtilities::GetGCHandleManager()->StoreObjectInHandleIfNull(handle, OBJECTREFToObject(object));
}

inline void* InterlockedCompareExchangeObjectInHandle(OBJECTHANDLE handle, OBJECTREF object, OBJECTREF comparandObject)
{
    ValidateHandleAssignment(handle, object);

    return GCHandleUtilities::GetGCHandleManager()->InterlockedCompareExchangeObjectInHandle(handle, OBJECTREFToObject(object), OBJECTREFToObject(comparandObject));
}

inline void ResetOBJECTHANDLE(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->StoreObjectInHandle(handle, NULL);
}

// Handle destruction convenience functions
inline void DestroyHandleCommon(OBJECTHANDLE handle, HandleType type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

    DiagHandleDestroyed(handle);
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, type);
}

inline void DestroyHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_DEFAULT);
}

inline void DestroyWeakHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_WEAK_DEFAULT);
}

inline void DestroyShortWeakHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_WEAK_SHORT);
}

inline void DestroyLongWeakHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_WEAK_LONG);
}

inline void DestroyStrongHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_STRONG);
}

inline void DestroyPinningHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_PINNED);
}

inline void DestroyAsyncPinningHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_ASYNCPINNED);
}

inline void DestroyRefcountedHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_REFCOUNTED);
}

inline void DestroyDependentHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_DEPENDENT);
}

inline void  DestroyVariableHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_VARIABLE);
}

inline void DestroyGlobalHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_DEFAULT);
}

inline void DestroyGlobalWeakHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_WEAK_DEFAULT);
}

inline void DestroyGlobalShortWeakHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_WEAK_SHORT);
}

inline void DestroyGlobalLongWeakHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_WEAK_LONG);
}

inline void DestroyGlobalStrongHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_STRONG);
}

inline void DestroyGlobalPinningHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_PINNED);
}

inline void DestroyGlobalRefcountedHandle(OBJECTHANDLE handle)
{
    DestroyHandleCommon(handle, HNDTYPE_REFCOUNTED);
}

inline void DestroyTypedHandle(OBJECTHANDLE handle)
{
    DiagHandleDestroyed(handle);
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfUnknownType(handle);
}

// Handle holders/wrappers

#ifndef FEATURE_NATIVEAOT
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyHandle>                   OHWrapper;
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyPinningHandle, NULL>      PinningHandleHolder;
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyAsyncPinningHandle, NULL> AsyncPinningHandleHolder;
typedef Wrapper<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyRefcountedHandle>         RefCountedOHWrapper;

typedef Holder<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyLongWeakHandle>            LongWeakHandleHolder;
typedef Holder<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyGlobalStrongHandle>        GlobalStrongHandleHolder;
typedef Holder<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, DestroyGlobalShortWeakHandle>     GlobalShortWeakHandleHolder;
typedef Holder<OBJECTHANDLE, DoNothing<OBJECTHANDLE>, ResetOBJECTHANDLE>                ObjectInHandleHolder;

class RCOBJECTHANDLEHolder : public RefCountedOHWrapper
{
public:
    FORCEINLINE RCOBJECTHANDLEHolder(OBJECTHANDLE p = NULL) : RefCountedOHWrapper(p)
    {
        LIMITED_METHOD_CONTRACT;
    }
    FORCEINLINE void operator=(OBJECTHANDLE p)
    {
        WRAPPER_NO_CONTRACT;

        RefCountedOHWrapper::operator=(p);
    }
};

class OBJECTHANDLEHolder : public OHWrapper
{
public:
    FORCEINLINE OBJECTHANDLEHolder(OBJECTHANDLE p = NULL) : OHWrapper(p)
    {
        LIMITED_METHOD_CONTRACT;
    }
    FORCEINLINE void operator=(OBJECTHANDLE p)
    {
        WRAPPER_NO_CONTRACT;

        OHWrapper::operator=(p);
    }
};

#endif // !FEATURE_NATIVEAOT

#endif // !DACCESS_COMPILE

#endif // _GCHANDLEUTILITIES_H_

