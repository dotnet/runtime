// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "gchandleutilities.h"

IGCHandleManager* g_pGCHandleManager = nullptr;

// Debug-only validation for handle.

void ValidateObjectAndAppDomain(OBJECTREF objRef, ADIndex appDomainIndex)
{
#ifdef _DEBUG_IMPL
    VALIDATEOBJECTREF(objRef);

    AppDomain *domain = SystemDomain::GetAppDomainAtIndex(appDomainIndex);

    // Access to a handle in an unloaded domain is not allowed
    assert(domain != nullptr);
    assert(!domain->NoAccessToHandleTable());

#endif // _DEBUG_IMPL
}

void ValidateHandleAssignment(OBJECTHANDLE handle, OBJECTREF objRef)
{
#ifdef _DEBUG_IMPL
    _ASSERTE(handle);

#ifdef DEBUG_DestroyedHandleValue
    // Verify that we are not trying to access a freed handle.
    _ASSERTE("Attempt to access destroyed handle." && *(_UNCHECKED_OBJECTREF*)handle != DEBUG_DestroyedHandleValue);
#endif

    IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
    ADIndex appDomainIndex = ADIndex(reinterpret_cast<DWORD>(mgr->GetHandleContext(handle)));

    AppDomain *unloadingDomain = SystemDomain::AppDomainBeingUnloaded();
    if (unloadingDomain && unloadingDomain->GetIndex() == appDomainIndex && unloadingDomain->NoAccessToHandleTable())
    {
        _ASSERTE (!"Access to a handle in unloaded domain is not allowed");
    }

    ValidateObjectAndAppDomain(objRef, appDomainIndex);
#endif // _DEBUG_IMPL
}

void DiagHandleCreated(OBJECTHANDLE handle, OBJECTREF objRef)
{
#ifdef GC_PROFILING
    BEGIN_PIN_PROFILER(CORProfilerTrackGC());
    g_profControlBlock.pProfInterface->HandleCreated((uintptr_t)handle, (ObjectID)OBJECTREF_TO_UNCHECKED_OBJECTREF(objRef));
    END_PIN_PROFILER();
#else
    UNREFERENCED_PARAMETER(handle);
    UNREFERENCED_PARAMETER(objRef);
#endif // GC_PROFILING
}

void DiagHandleDestroyed(OBJECTHANDLE handle)
{
#ifdef GC_PROFILING
    BEGIN_PIN_PROFILER(CORProfilerTrackGC());
    g_profControlBlock.pProfInterface->HandleDestroyed((uintptr_t)handle);
    END_PIN_PROFILER();
#else
    UNREFERENCED_PARAMETER(handle);
#endif // GC_PROFILING
}
