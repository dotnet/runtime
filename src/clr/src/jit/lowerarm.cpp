// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Lowering for ARM                                XX
XX                                                                           XX
XX  This encapsulates all the logic for lowering trees for the ARM           XX
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

#ifdef _TARGET_ARM_

#include "jit.h"
#include "sideeffects.h"
#include "lower.h"
#include "lsra.h"

//------------------------------------------------------------------------
// IsCallTargetInRange: Can a call target address be encoded in-place?
//
// Return Value:
//    True if the addr fits into the range.
//
bool Lowering::IsCallTargetInRange(void* addr)
{
    return comp->codeGen->validImmForBL((ssize_t)addr);
}

//------------------------------------------------------------------------
// IsContainableImmed: Is an immediate encodable in-place?
//
// Return Value:
//    True if the immediate can be folded into an instruction,
//    for example small enough and non-relocatable.
bool Lowering::IsContainableImmed(GenTree* parentNode, GenTree* childNode)
{
    if (varTypeIsFloating(parentNode->TypeGet()))
    {
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
                {
                    // TODO-ARM-Cleanup: not tested yet.
                    NYI_ARM("ARM IsContainableImmed for floating point type");
                    // We can contain a floating point 0.0 constant in a compare instruction
                    return true;
                }
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
                if (emitter::emitIns_valid_imm_for_add(immVal, INS_FLAGS_DONT_CARE))
                    return true;
                break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
            case GT_AND:
            case GT_OR:
            case GT_XOR:
                if (emitter::emitIns_valid_imm_for_alu(immVal))
                    return true;
                break;
        }
    }

    return false;
}

#endif // _TARGET_ARM_

#endif // !LEGACY_BACKEND
