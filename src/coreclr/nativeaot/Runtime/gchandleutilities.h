// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCHANDLEUTILITIES_H_
#define _GCHANDLEUTILITIES_H_

#include "gcinterface.h"

extern "C" IGCHandleManager* g_pGCHandleManager;
extern "C" IGCHandleStore* g_pGlobalHandleStore;

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

    // Retrieves the global GC handle store.
    static IGCHandleStore* GetGlobalHandleStore()
    {
        LIMITED_METHOD_CONTRACT;

        assert(g_pGlobalHandleStore != nullptr);
        return g_pGlobalHandleStore;
    }

private:
    // This class should never be instantiated.
    GCHandleUtilities() = delete;
};

// Given a handle, returns an OBJECTREF for the object it refers to.
inline OBJECTREF ObjectFromHandle(OBJECTHANDLE handle)
{
    _ASSERTE(handle);

    // Wrap the raw OBJECTREF and return it
    return UNCHECKED_OBJECTREF_TO_OBJECTREF(*PTR_UNCHECKED_OBJECTREF(handle));
}

#endif // _GCHANDLEUTILITIES_H_
