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
#ifdef FEATURE_MANAGED_ALLOC_HELPERS
#include "threadstatics.h"
#endif

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

#ifdef FEATURE_MANAGED_ALLOC_HELPERS
bool g_fManagedAllocHelpers = false;

// AllocHelpers.EEAllocContext in CoreLib mirrors the start of ee_alloc_context.
static_assert(offsetof(ee_alloc_context, m_CombinedLimit) == 0);
static_assert(offsetof(ee_alloc_context, m_GCAllocContext) + offsetof(gc_alloc_context, alloc_ptr) == sizeof(void*));

extern "C" FCIMPL0(void*, AllocHelpers_GetAllocContext)
{
    FCALL_CONTRACT;
    return &t_runtime_thread_locals.alloc_context;
}
FCIMPLEND

extern "C" FCIMPL0(FC_BOOL_RET, AllocHelpersConfig_IsEnabled)
{
    FCALL_CONTRACT;
    FC_RETURN_BOOL(g_fManagedAllocHelpers);
}
FCIMPLEND

// The managed fast paths only pay off where the JIT inlines thread-static access (so the managed fast path has no
// TLS call) while the native fast helpers pay a TLSDESC call per allocation (and, on arm64, a jump stub to reach
// libcoreclr): linux arm64/loongarch64/riscv64. Elsewhere (e.g. x64, where the JIT'd TLS access is itself a
// __tls_get_addr call) the native helpers are faster, so they are kept regardless of the config.
static bool ShouldUseManagedAllocHelpers()
{
    STANDARD_VM_CONTRACT;

#if defined(__linux__) && !defined(TARGET_ANDROID) && (defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64))
    return (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_ManagedAllocHelpers) != 0) && CanJITOptimizeTLSAccess();
#else
    return false;
#endif
}
#endif // FEATURE_MANAGED_ALLOC_HELPERS

void InitJITAllocationHelpers()
{
    STANDARD_VM_CONTRACT;

    // Allocation helpers, faster but non-logging
    if (!(TrackAllocationsEnabled() || LoggingOn(LF_GCALLOC, LL_INFO10)))
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

#ifdef FEATURE_MANAGED_ALLOC_HELPERS
        // Prototype: managed allocation fast paths in CoreLib (AllocHelpers). These are only installed in place of
        // the native fast helpers, i.e. never when allocation tracking/logging is enabled; GCStress (cfg_alloc)
        // selects the slow helpers in getNewHelperStatic/getNewArrHelperStatic so it is unaffected.
        if (ShouldUseManagedAllocHelpers())
        {
            g_fManagedAllocHelpers = true;
            SetJitHelperToILImplementation(DYNAMIC_CORINFO_HELP_NEWSFAST, METHOD__ALLOCHELPERS__NEWSFAST);
            SetJitHelperToILImplementation(DYNAMIC_CORINFO_HELP_NEWARR_1_VC, METHOD__ALLOCHELPERS__NEWARR_1);
            SetJitHelperToILImplementation(DYNAMIC_CORINFO_HELP_NEWARR_1_PTR, METHOD__ALLOCHELPERS__NEWARR_1);
        }
#endif // FEATURE_MANAGED_ALLOC_HELPERS
    }

// Debugger depends on new helper names starting with CORINFO_HELP_NEW
#define SET_NEW_HELPER_AUXILIARY_SYMBOL(code) SetJitHelperAuxiliarySymbol(code, #code);
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWFAST)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWFAST_MAYBEFROZEN)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWSFAST)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWSFAST_ALIGN8)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWSFAST_ALIGN8_VC)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_DIRECT)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_MAYBEFROZEN)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_PTR)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_VC)
    SET_NEW_HELPER_AUXILIARY_SYMBOL(CORINFO_HELP_NEWARR_1_ALIGN8)
#undef SET_NEW_HELPER_AUXILIARY_SYMBOL
}
