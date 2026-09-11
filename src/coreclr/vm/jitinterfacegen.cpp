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

static void SetJitHelperAuxiliarySymbol(CorInfoHelpFunc ftnNum, const char* name)
{
    LIMITED_METHOD_CONTRACT;

    VMHELPDEF const& helperDef = hlpFuncTable[ftnNum];
    PCODE pfnHelper = helperDef.pfnHelper;
    DynamicCorInfoHelpFunc dynamicFtnNum;
    if (helperDef.IsDynamicHelper(&dynamicFtnNum))
    {
        pfnHelper = hlpDynamicFuncTable[dynamicFtnNum].pfnHelper;
    }

    if (pfnHelper != (PCODE)NULL)
    {
        SetAuxiliarySymbol((void*)pfnHelper, name);
    }
}

void InitJITAllocationHelpers()
{
    STANDARD_VM_CONTRACT;

    // Allocation helpers, faster but non-logging
    if (!(TrackAllocationsEnabled() || LoggingOn(LF_GCALLOC, LL_INFO10)))
    {
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST, RhpNewFast);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_VC, RhpNewArrayFast);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_PTR, RhpNewPtrArrayFast);

#if defined(FEATURE_2XPTR_ALIGNMENT)
#if defined(TARGET_ARM) || defined(TARGET_WASM) || defined(TARGET_AMD64) || defined(TARGET_ARM64)
        // These fast inline allocation stubs handle the alignment fixup themselves. On 32-bit
        // ARM/WASM and on amd64/arm64 the scalar boxed/reference align helpers get an inline fast
        // path; the array align helper only exists on ARM/WASM. Where a dedicated stub isn't
        // installed the align helpers keep their portable default backing (RhpNew /
        // RhpNewVariableSizeObject), which route through RhpGcAlloc -> AllocateObject /
        // AllocateSzArray and derive GC_ALLOC_ALIGN_2XPTR from the MethodTable, so alignment is
        // honored without a stub.
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN_2XPTR, RhpNewFastAlign2xPtr);
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN_2XPTR_VC, RhpNewFastMisalign);
#if defined(TARGET_ARM) || defined(TARGET_WASM)
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_ALIGN_2XPTR, RhpNewArrayFastAlign2xPtr);
#endif
#endif
#endif

        ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(RhNewString), ECall::FastAllocateString);
    }

// Debugger depends on new helper names starting with CORINFO_HELP_NEW
#define SET_NEW_HELPER_AUXILIARY_SYMBOL(code) SetJitHelperAuxiliarySymbol(code, #code);
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWFAST)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWFAST_MAYBEFROZEN)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWSFAST)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWSFAST_ALIGN_2XPTR)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWSFAST_ALIGN_2XPTR_VC)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_DIRECT)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_MAYBEFROZEN)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_PTR)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_VC)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_ALIGN_2XPTR)
#undef SET_NEW_HELPER_AUXILIARY_SYMBOL
}
