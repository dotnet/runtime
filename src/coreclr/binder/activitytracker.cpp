// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// activitytracker.cpp
//


//
// Helpers for interaction with the managed ActivityTracker
//
// ============================================================

#include "common.h"
#include "activitytracker.h"

void ActivityTracker::Start(/*out*/ GUID *activityId, /*out*/ GUID *relatedActivityId)
{
    GCX_COOP();

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    UnmanagedCallersOnlyCaller startAssemblyLoad(METHOD__ASSEMBLYLOADCONTEXT__START_ASSEMBLY_LOAD);
    startAssemblyLoad.InvokeThrowing(activityId, relatedActivityId);
}

void ActivityTracker::Stop(/*out*/ GUID *activityId)
{
    GCX_COOP();

    OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

    UnmanagedCallersOnlyCaller stopAssemblyLoad(METHOD__ASSEMBLYLOADCONTEXT__STOP_ASSEMBLY_LOAD);
    stopAssemblyLoad.InvokeThrowing(activityId);
}
