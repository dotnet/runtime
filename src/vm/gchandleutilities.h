// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCHANDLEUTILITIES_H_
#define _GCHANDLEUTILITIES_H_

#include "gcinterface.h"

#ifdef FEATURE_COMINTEROP
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

void ValidateObjectAndAppDomain(OBJECTREF objRef, ADIndex appDomainIndex);
void ValidateHandleAssignment(OBJECTHANDLE handle, OBJECTREF objRef);

// Given a handle, returns an OBJECTREF for the object it refers to.
inline OBJECTREF ObjectFromHandle(OBJECTHANDLE handle)
{
    _ASSERTE(handle);

#ifdef _DEBUG_IMPL
    DWORD context = (DWORD)GCHandleUtilities::GetGCHandleManager()->GetHandleContext(handle);
    OBJECTREF objRef = ObjectToOBJECTREF(*(Object**)handle);

    ValidateObjectAndAppDomain(objRef, ADIndex(context));
#endif // _DEBUG_IMPL

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

inline OBJECTHANDLE CreateHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_DEFAULT);
}

inline OBJECTHANDLE CreateWeakHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_DEFAULT);
}

inline OBJECTHANDLE CreateShortWeakHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_SHORT);
}

inline OBJECTHANDLE CreateLongWeakHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_LONG);
}

inline OBJECTHANDLE CreateStrongHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_STRONG);
}

inline OBJECTHANDLE CreatePinningHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_PINNED);
}

inline OBJECTHANDLE CreateAsyncPinningHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_ASYNCPINNED);
}

inline OBJECTHANDLE CreateRefcountedHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_REFCOUNTED);
}

inline OBJECTHANDLE CreateSizedRefHandle(IGCHandleStore* store, OBJECTREF object)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_SIZEDREF);
}

inline OBJECTHANDLE CreateSizedRefHandle(IGCHandleStore* store, OBJECTREF object, int heapToAffinitizeTo)
{
    return store->CreateHandleOfType(OBJECTREFToObject(object), HNDTYPE_SIZEDREF, heapToAffinitizeTo);
}

// Global handle creation convenience functions

inline OBJECTHANDLE CreateGlobalHandle(OBJECTREF object)
{
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);
    return GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_DEFAULT);
}

inline OBJECTHANDLE CreateGlobalWeakHandle(OBJECTREF object)
{
    return GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_DEFAULT);
}

inline OBJECTHANDLE CreateGlobalShortWeakHandle(OBJECTREF object)
{
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);
    return GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_SHORT);
}

inline OBJECTHANDLE CreateGlobalLongWeakHandle(OBJECTREF object)
{
    return GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_LONG);
}

inline OBJECTHANDLE CreateGlobalStrongHandle(OBJECTREF object)
{
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);
    return GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_STRONG);
}

inline OBJECTHANDLE CreateGlobalPinningHandle(OBJECTREF object)
{
    return GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_PINNED);
}

inline OBJECTHANDLE CreateGlobalRefcountedHandle(OBJECTREF object)
{
    return GCHandleUtilities::GetGCHandleManager()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_REFCOUNTED);
}

// Special handle creation convenience functions

#ifdef FEATURE_COMINTEROP
inline OBJECTHANDLE CreateWinRTWeakHandle(IGCHandleStore* store, OBJECTREF object, IWeakReference* pWinRTWeakReference)
{
    return store->CreateHandleWithExtraInfo(OBJECTREFToObject(object), HNDTYPE_WEAK_WINRT, (void*)pWinRTWeakReference);
}
#endif // FEATURE_COMINTEROP

// Creates a variable-strength handle
inline OBJECTHANDLE CreateVariableHandle(IGCHandleStore* store, OBJECTREF object, uint32_t type)
{
    return store->CreateHandleWithExtraInfo(OBJECTREFToObject(object), HNDTYPE_VARIABLE, (void*)((uintptr_t)type));
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

inline void DestroyHandle(OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_DEFAULT);
}

inline void DestroyWeakHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_WEAK_DEFAULT);
}

inline void DestroyShortWeakHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_WEAK_SHORT);
}

inline void DestroyLongWeakHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_WEAK_LONG);
}

inline void DestroyStrongHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_STRONG);
}

inline void DestroyPinningHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_PINNED);
}

inline void DestroyAsyncPinningHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_ASYNCPINNED);
}

inline void DestroyRefcountedHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_REFCOUNTED);
}

inline void DestroyDependentHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_DEPENDENT);
}

inline void  DestroyVariableHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_VARIABLE);
}

inline void DestroyGlobalHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_DEFAULT);
}

inline void DestroyGlobalWeakHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_WEAK_DEFAULT);
}

inline void DestroyGlobalShortWeakHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_WEAK_SHORT);
}

inline void DestroyGlobalLongWeakHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_WEAK_LONG);
}

inline void DestroyGlobalStrongHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_STRONG);
}

inline void DestroyGlobalPinningHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_PINNED);
}

inline void DestroyGlobalRefcountedHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_REFCOUNTED);
}

inline void DestroyTypedHandle(OBJECTHANDLE handle)
{
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfUnknownType(handle);
}

#ifdef FEATURE_COMINTEROP
inline void DestroyWinRTWeakHandle(OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CAN_TAKE_LOCK;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    // Release the WinRT weak reference if we have one. We're assuming that this will not reenter the
    // runtime, since if we are pointing at a managed object, we should not be using HNDTYPE_WEAK_WINRT
    // but rather HNDTYPE_WEAK_SHORT or HNDTYPE_WEAK_LONG.
    void* pExtraInfo = GCHandleUtilities::GetGCHandleManager()->GetExtraInfoFromHandle(handle);
    IWeakReference* pWinRTWeakReference = reinterpret_cast<IWeakReference*>(pExtraInfo);
    if (pWinRTWeakReference != nullptr)
    {
        pWinRTWeakReference->Release();
    }

    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, HNDTYPE_WEAK_WINRT);
}
#endif

// Handle holders/wrappers

#ifndef FEATURE_REDHAWK
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

#endif // !FEATURE_REDHAWK

#endif // !DACCESS_COMPILE

#endif // _GCHANDLEUTILITIES_H_

