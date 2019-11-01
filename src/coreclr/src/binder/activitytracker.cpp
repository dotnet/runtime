// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLYLOADCONTEXT__START_ASSEMBLY_LOAD);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0] = PTR_TO_ARGHOLDER(activityId);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(relatedActivityId);

    CALL_MANAGED_METHOD_NORET(args)
}

void ActivityTracker::Stop(/*out*/ GUID *activityId)
{
    GCX_COOP();

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__ASSEMBLYLOADCONTEXT__STOP_ASSEMBLY_LOAD);
    DECLARE_ARGHOLDER_ARRAY(args, 1);
    args[ARGNUM_0] = PTR_TO_ARGHOLDER(activityId);

    CALL_MANAGED_METHOD_NORET(args)
}