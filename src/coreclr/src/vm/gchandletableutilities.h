// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _GCHANDLETABLEUTILITIES_H_
#define _GCHANDLETABLEUTILITIES_H_

#include "gcinterface.h"

#ifdef FEATURE_COMINTEROP
#include <weakreference.h>
#endif

extern "C" IGCHandleTable* g_pGCHandleTable;

class GCHandleTableUtilities
{
public:
    // Retrieves the GC handle table.
    static IGCHandleTable* GetGCHandleTable() 
    {
        LIMITED_METHOD_CONTRACT;

        assert(g_pGCHandleTable != nullptr);
        return g_pGCHandleTable;
    }

private:
    // This class should never be instantiated.
    GCHandleTableUtilities() = delete;
};

void ValidateHandleAndAppDomain(OBJECTHANDLE handle);

// Given a handle, returns an OBJECTREF for the object it refers to.
inline OBJECTREF ObjectFromHandle(OBJECTHANDLE handle)
{
    _ASSERTE(handle);

#ifdef _DEBUG_IMPL
    ValidateHandleAndAppDomain(handle);
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

inline OBJECTHANDLE CreateHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_DEFAULT);
}

inline OBJECTHANDLE CreateWeakHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_WEAK_DEFAULT);
}

inline OBJECTHANDLE CreateShortWeakHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_WEAK_SHORT);
}

inline OBJECTHANDLE CreateLongWeakHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_WEAK_LONG);
}

inline OBJECTHANDLE CreateStrongHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_STRONG);
}

inline OBJECTHANDLE CreatePinningHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_PINNED);
}

inline OBJECTHANDLE CreateAsyncPinningHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_ASYNCPINNED);
}

inline OBJECTHANDLE CreateRefcountedHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_REFCOUNTED);
}

inline OBJECTHANDLE CreateSizedRefHandle(void* table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_SIZEDREF);
}

inline OBJECTHANDLE CreateSizedRefHandle(void* table, OBJECTREF object, int heapToAffinitizeTo)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_SIZEDREF, heapToAffinitizeTo);
}

// Global handle creation convenience functions

inline OBJECTHANDLE CreateGlobalHandle(OBJECTREF object)
{
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);
    return GCHandleTableUtilities::GetGCHandleTable()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_DEFAULT);
}

inline OBJECTHANDLE CreateGlobalWeakHandle(OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_DEFAULT);
}

inline OBJECTHANDLE CreateGlobalShortWeakHandle(OBJECTREF object)
{
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);
    return GCHandleTableUtilities::GetGCHandleTable()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_SHORT);
}

inline OBJECTHANDLE CreateGlobalLongWeakHandle(OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_WEAK_LONG);
}

inline OBJECTHANDLE CreateGlobalStrongHandle(OBJECTREF object)
{
    CONDITIONAL_CONTRACT_VIOLATION(ModeViolation, object == NULL);
    return GCHandleTableUtilities::GetGCHandleTable()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_STRONG);
}

inline OBJECTHANDLE CreateGlobalPinningHandle(OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_PINNED);
}

inline OBJECTHANDLE CreateGlobalRefcountedHandle(OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateGlobalHandleOfType(OBJECTREFToObject(object), HNDTYPE_REFCOUNTED);
}

// Special handle creation convenience functions

#ifdef FEATURE_COMINTEROP
inline OBJECTHANDLE CreateWinRTWeakHandle(void* table, OBJECTREF object, IWeakReference* pWinRTWeakReference)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleWithExtraInfo(table,
                                                                                 OBJECTREFToObject(object),
                                                                                 HNDTYPE_WEAK_WINRT,
                                                                                 (void*)pWinRTWeakReference);
}
#endif // FEATURE_COMINTEROP

// Creates a variable-strength handle
inline OBJECTHANDLE CreateVariableHandle(void* table, OBJECTREF object, uint32_t type)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleWithExtraInfo(table,
                                                                                 OBJECTREFToObject(object),
                                                                                 HNDTYPE_VARIABLE,
                                                                                 (void*)((uintptr_t)type));
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

    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_DEFAULT);
}

inline void DestroyWeakHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_WEAK_DEFAULT);
}

inline void DestroyShortWeakHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_WEAK_SHORT);
}

inline void DestroyLongWeakHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_WEAK_LONG);
}

inline void DestroyStrongHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_STRONG);
}

inline void DestroyPinningHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_PINNED);
}

inline void DestroyAsyncPinningHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_ASYNCPINNED);
}

inline void DestroyRefcountedHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_REFCOUNTED);
}

inline void DestroyDependentHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_DEPENDENT);
}

inline void  DestroyVariableHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_VARIABLE);
}

inline void DestroyGlobalHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_DEFAULT);
}

inline void DestroyGlobalWeakHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_WEAK_DEFAULT);
}

inline void DestroyGlobalShortWeakHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_WEAK_SHORT);
}

inline void DestroyGlobalLongWeakHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_WEAK_LONG);
}

inline void DestroyGlobalStrongHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_STRONG);
}

inline void DestroyGlobalPinningHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_PINNED);
}

inline void DestroyGlobalRefcountedHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_REFCOUNTED);
}

inline void DestroyTypedHandle(OBJECTHANDLE handle)
{
    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfUnknownType(handle);
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
    void* pExtraInfo = GCHandleTableUtilities::GetGCHandleTable()->GetExtraInfoFromHandle(handle);
    IWeakReference* pWinRTWeakReference = reinterpret_cast<IWeakReference*>(pExtraInfo);
    if (pWinRTWeakReference != nullptr)
    {
        pWinRTWeakReference->Release();
    }

    GCHandleTableUtilities::GetGCHandleTable()->DestroyHandleOfType(handle, HNDTYPE_WEAK_WINRT);
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

#endif // _GCHANDLETABLEUTILITIES_H_

