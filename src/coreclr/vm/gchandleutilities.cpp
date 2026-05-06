// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gchandleutilities.h"

IGCHandleManager* g_pGCHandleManager = nullptr;

void ValidateHandleAssignment(OBJECTHANDLE handle, OBJECTREF objRef)
{
#ifdef _DEBUG_IMPL
    _ASSERTE(handle);

#ifdef DEBUG_DestroyedHandleValue
    // Verify that we are not trying to access a freed handle.
    _ASSERTE("Attempt to access destroyed handle." && *(_UNCHECKED_OBJECTREF*)handle != DEBUG_DestroyedHandleValue);
#endif
    VALIDATEOBJECTREF(objRef);
#endif // _DEBUG_IMPL
}

void DiagHandleCreated(OBJECTHANDLE handle, OBJECTREF objRef)
{
#ifdef GC_PROFILING
    BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC());
    (&g_profControlBlock)->HandleCreated((uintptr_t)handle, (ObjectID)OBJECTREF_TO_UNCHECKED_OBJECTREF(objRef));
    END_PROFILER_CALLBACK();
#else
    UNREFERENCED_PARAMETER(handle);
    UNREFERENCED_PARAMETER(objRef);
#endif // GC_PROFILING
}

void DiagHandleDestroyed(OBJECTHANDLE handle)
{
#ifdef GC_PROFILING
    BEGIN_PROFILER_CALLBACK(CORProfilerTrackGC());
    (&g_profControlBlock)->HandleDestroyed((uintptr_t)handle);
    END_PROFILER_CALLBACK();
#else
    UNREFERENCED_PARAMETER(handle);
#endif // GC_PROFILING
}
