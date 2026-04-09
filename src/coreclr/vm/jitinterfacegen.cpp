// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "clrtypes.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "comdelegate.h"
#include "field.h"
#include "ecall.h"
#include "writebarriermanager.h"

void InitJITAllocationHelpers()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(g_SystemInfo.dwNumberOfProcessors != 0);

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
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, RhpNewArrayFast);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_PTR, RhpNewPtrArrayFast);

#if defined(FEATURE_64BIT_ALIGNMENT)
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, RhpNewFastAlign8);
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8_VC, RhpNewFastMisalign);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_ALIGN8, RhpNewArrayFastAlign8);
#endif

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(RhNewString), ECall::FastAllocateString);
        }
        else
        {
#if defined(TARGET_WINDOWS) && (defined(TARGET_AMD64) || defined(TARGET_X86))
            // Replace the 1p slow allocation helpers with faster version
            //
            // When we're running Workstation GC on a single proc box we don't have
            // InlineGetThread versions because there is no need to call GetThread
            SetJitHelperFunction(CORINFO_HELP_NEWSFAST, RhpNewFast_UP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, RhpNewArrayFast_UP);
            SetJitHelperFunction(CORINFO_HELP_NEWARR_1_PTR, RhpNewPtrArrayFast_UP);

            ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(RhNewString_UP), ECall::FastAllocateString);
#else
            _ASSERTE(!"Expected to use ThreadAllocationContexts");
#endif
        }
    }
}
