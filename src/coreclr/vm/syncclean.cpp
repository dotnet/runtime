// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"

#include "syncclean.hpp"
#include "virtualcallstub.h"
#include "threadsuspend.h"

#ifdef FEATURE_INTERPRETER
#include "interpexec.h"
#endif

void SyncClean::CleanUp()
{
    LIMITED_METHOD_CONTRACT;

    // Only GC thread can call this.
    _ASSERTE (IsAtProcessExit() ||
              IsGCSpecialThread() ||
              (GCHeapUtilities::IsGCInProgress()  && GetThreadNULLOk() == ThreadSuspend::GetSuspensionThread()));

    // Give others we want to reclaim during the GC sync point a chance to do it
    VirtualCallStubManager::ReclaimAll();

#ifdef FEATURE_INTERPRETER
    // Reclaim dead interpreter dispatch cache entries
    InterpDispatchCache_ReclaimAll();
#endif
}
