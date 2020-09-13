// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "allocacheck.h" // for alloca

void Compiler::fgInitArgInfoHelp(GenTreeCall* call, NonStandardArgs* nonStandardArgs, unsigned* numArgs)
{
#if defined(TARGET_X86)
    // The x86 CORINFO_HELP_INIT_PINVOKE_FRAME helper has a custom calling convention.
    // Set the argument registers correctly here.
    if (call->IsHelperCall(this, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        GenTreeCall::Use* args = call->gtCallArgs;
        GenTree*          arg1 = args->GetNode();
        assert(arg1 != nullptr);
        nonStandardArgs->Add(arg1, REG_PINVOKE_FRAME);
    }

    // The x86 shift helpers have custom calling conventions and expect the lo part of the long to be in EAX and the
    // hi part to be in EDX. This sets the argument registers up correctly.
    if (call->IsHelperCall(this, CORINFO_HELP_LLSH) || call->IsHelperCall(this, CORINFO_HELP_LRSH) ||
        call->IsHelperCall(this, CORINFO_HELP_LRSZ))
    {
        GenTreeCall::Use* args = call->gtCallArgs;
        GenTree*          arg1 = args->GetNode();
        assert(arg1 != nullptr);
        nonStandardArgs->Add(arg1, REG_LNGARG_LO);

        args          = args->GetNext();
        GenTree* arg2 = args->GetNode();
        assert(arg2 != nullptr);
        nonStandardArgs->Add(arg2, REG_LNGARG_HI);
    }
#endif // TARGET_X86
}
