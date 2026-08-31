// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GCHANDLEUTILITIES_H_
#define _GCHANDLEUTILITIES_H_

#include "gcinterface.h"

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

// Given a handle, returns an OBJECTREF for the object it refers to.
inline OBJECTREF ObjectFromHandle(OBJECTHANDLE handle)
{
    _ASSERTE(handle);

    // Wrap the raw OBJECTREF and return it
    return UNCHECKED_OBJECTREF_TO_OBJECTREF(*PTR_UNCHECKED_OBJECTREF(handle));
}

#endif // _GCHANDLEUTILITIES_H_
