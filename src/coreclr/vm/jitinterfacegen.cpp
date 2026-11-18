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

#if defined(FEATURE_64BIT_ALIGNMENT)
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8, RhpNewFastAlign8);
        SetJitHelperFunction(CORINFO_HELP_NEWSFAST_ALIGN8_VC, RhpNewFastMisalign);
        SetJitHelperFunction(CORINFO_HELP_NEWARR_1_ALIGN8, RhpNewArrayFastAlign8);
#endif

        ECall::DynamicallyAssignFCallImpl(GetEEFuncEntryPoint(RhNewString), ECall::FastAllocateString);
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
