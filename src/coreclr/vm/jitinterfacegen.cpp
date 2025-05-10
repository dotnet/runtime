// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: JITinterfaceGen.CPP
//
// This contains the AMD64 version of InitJITHelpers1().
//
// ===========================================================================


#include "common.h"
#include "clrtypes.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "comdelegate.h"
#include "field.h"
#include "ecall.h"

#ifdef HOST_64BIT

#ifdef TARGET_AMD64
extern WriteBarrierManager g_WriteBarrierManager;
#endif // TARGET_AMD64

#endif // HOST_64BIT

/*********************************************************************/
// Initialize the part of the JIT helpers that require very little of
// EE infrastructure to be in place.
/*********************************************************************/
#ifndef TARGET_X86

void InitJITHelpers1()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 0);

#if defined(TARGET_AMD64)

    g_WriteBarrierManager.Initialize();

    // Allocation helpers, faster but non-logging
    if (!((TrackAllocationsEnabled()) ||
        (LoggingOn(LF_GCALLOC, LL_INFO10))
#ifdef _DEBUG
        || (g_pConfig->ShouldInjectFault(INJECTFAULT_GCHEAP) != 0)
#endif // _DEBUG
        ))
    {
        // if (multi-proc || server GC || non-Windows)
        if (GCHeapUtilities::UseThreadAllocationContexts())
        {
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, RhpNewFast);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, RhpNewFast);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, RhpNewArrayFast);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, RhpNewArrayFast);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(RhNewString), ECall::FastAllocateString);
        }
#ifdef TARGET_WINDOWS
        else
        {
            // Replace the 1p slow allocation helpers with faster version
            //
            // When we're running Workstation GC on a single proc box we don't have
            // InlineGetThread versions because there is no need to call GetThread
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, RhpNewFast_UP);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, RhpNewFast_UP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, RhpNewArrayFast_UP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_OBJ, RhpNewArrayFast_UP);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(RhNewString_UP), ECall::FastAllocateString);
        }
#endif // TARGET_WINDOWS
    }
#endif // TARGET_AMD64
}

#endif // !TARGET_X86
