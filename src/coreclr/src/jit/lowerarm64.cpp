// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for ARM64                              XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the ARM64         XX
XX  architecture.  For a more detailed view of what is lowering, please      XX
XX  take a look at Lower.cpp                                                 XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM64_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"

// returns true if the tree can use the read-modify-write memory instruction form
bool Lowering::isRMWRegOper(GenTreePtr tree)
{
    return false;
}

bool Lowering::IsCallTargetInRange(void* addr)
{
    // TODO-ARM64-CQ:  This is a workaround to unblock the JIT from getting calls working.
    // Currently, we'll be generating calls using blr and manually loading an absolute
    // call target in a register using a sequence of load immediate instructions.
    //
    // As you can expect, this is inefficient and it's not the recommended way as per the
    // ARM64 ABI Manual but will get us getting things done for now.
    // The work to get this right would be to implement PC-relative calls, the bl instruction
    // can only address things -128 + 128MB away, so this will require getting some additional
    // code to get jump thunks working.
    return true;
}

// return true if the immediate can be folded into an instruction, for example small enough and non-relocatable
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (varTypeIsFloating(parentNode->TypeGet()))
    {
        // We can contain a floating point 0.0 constant in a compare instruction
        switch (parentNode->OperGet())
        {
            default:
                return false;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
                if (childNode->IsIntegralConst(0))
                    return true;
                break;
        }
    }
    else
    {
        // Make sure we have an actual immediate
        if (!childNode->IsCnsIntOrI())
            return false;
        if (childNode->IsIconHandle() && comp->opts.compReloc)
            return false;

        ssize_t  immVal = childNode->gtIntCon.gtIconVal;
        emitAttr attr   = emitActualTypeSize(childNode->TypeGet());
        emitAttr size   = EA_SIZE(attr);

        switch (parentNode->OperGet())
        {
            default:
                return false;

            case GT_ADD:
            case GT_SUB:
                if (emitter::emitIns_valid_imm_for_add(immVal, size))
                    return true;
                break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
                if (emitter::emitIns_valid_imm_for_cmp(immVal, size))
                    return true;
                break;

            case GT_AND:
            case GT_OR:
            case GT_XOR:
                if (emitter::emitIns_valid_imm_for_alu(immVal, size))
                    return true;
                break;

            case GT_STORE_LCL_VAR:
                if (immVal == 0)
                    return true;
                break;
        }
    }

    return false;
}

#endif // _TARGET_ARM64_

#endif // !LEGACY_BACKEND
