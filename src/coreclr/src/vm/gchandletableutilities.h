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

#ifndef DACCESS_COMPILE

// Handle creation convenience functions

inline OBJECTHANDLE CreateHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_DEFAULT);
}

inline OBJECTHANDLE CreateWeakHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_WEAK_DEFAULT);
}

inline OBJECTHANDLE CreateShortWeakHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_WEAK_SHORT);
}

inline OBJECTHANDLE CreateLongWeakHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_WEAK_LONG);
}

inline OBJECTHANDLE CreateStrongHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_STRONG);
}

inline OBJECTHANDLE CreatePinningHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_PINNED);
}

inline OBJECTHANDLE CreateSizedRefHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_SIZEDREF);
}

inline OBJECTHANDLE CreateAsyncPinningHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_ASYNCPINNED);
}

inline OBJECTHANDLE CreateRefcountedHandle(HHANDLETABLE table, OBJECTREF object)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleOfType(table, OBJECTREFToObject(object), HNDTYPE_REFCOUNTED);
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
inline OBJECTHANDLE CreateWinRTWeakHandle(HHANDLETABLE table, OBJECTREF object, IWeakReference* pWinRTWeakReference)
{
    return GCHandleTableUtilities::GetGCHandleTable()->CreateHandleWithExtraInfo(table, OBJECTREFToObject(object), HNDTYPE_WEAK_WINRT, (void*)pWinRTWeakReference);
}
#endif // FEATURE_COMINTEROP

#endif // !DACCESS_COMPILE

#endif // _GCHANDLETABLEUTILITIES_H_

