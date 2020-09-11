// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "allocacheck.h" // for alloca

void Compiler::fgInitArgInfoHelp(GenTreeCall* call, NonStandardArgs* nonStandardArgs, unsigned* numArgs)
{
#if defined(TARGET_ARM)
    // The arm32 CORINFO_HELP_INIT_PINVOKE_FRAME helper has a custom calling convention.
    // Set the argument registers correctly here.
    if (call->IsHelperCall(this, CORINFO_HELP_INIT_PINVOKE_FRAME))
    {
        GenTreeCall::Use* args = call->gtCallArgs;
        GenTree*          arg1 = args->GetNode();
        assert(arg1 != nullptr);
        nonStandardArgs->Add(arg1, REG_PINVOKE_FRAME);
    }

    // A non-standard calling convention using wrapper delegate invoke is used on ARM, only, for wrapper
    // delegates. It is used for VSD delegate calls where the VSD custom calling convention ABI requires passing
    // R4, a callee-saved register, with a special value. Since R4 is a callee-saved register, its value needs
    // to be preserved. Thus, the VM uses a wrapper delegate IL stub, which preserves R4 and also sets up R4
    // correctly for the VSD call. The VM is simply reusing an existing mechanism (wrapper delegate IL stub)
    // to achieve its goal for delegate VSD call. See COMDelegate::NeedsWrapperDelegate() in the VM for details.
    else if (call->gtCallMoreFlags & GTF_CALL_M_WRAPPER_DELEGATE_INV)
    {
        GenTree* arg = call->gtCallThisArg->GetNode();
        if (arg->OperIsLocal())
        {
            arg = gtClone(arg, true);
        }
        else
        {
            GenTree* tmp = fgInsertCommaFormTemp(&arg);
            call->gtCallThisArg->SetNode(arg);
            call->gtFlags |= GTF_ASG;
            arg = tmp;
        }
        noway_assert(arg != nullptr);

        GenTree* newArg = new (this, GT_ADDR)
            GenTreeAddrMode(TYP_BYREF, arg, nullptr, 0, eeGetEEInfo()->offsetOfWrapperDelegateIndirectCell);

        // Append newArg as the last arg
        GenTreeCall::Use** insertionPoint = &call->gtCallArgs;
        for (; *insertionPoint != nullptr; insertionPoint = &((*insertionPoint)->NextRef()))
        {
        }
        *insertionPoint = gtNewCallArgs(newArg);

        *numArgs++;
        nonStandardArgs->Add(newArg, virtualStubParamInfo->GetReg());
    }
#endif // defined(TARGET_ARM)
}
