// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          Arm64 Specific Morph                             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef FEATURE_MASKED_HW_INTRINSICS

//------------------------------------------------------------------------
// canMorphVectorOperandToMask: Can this vector operand be converted to a
//                              node with type TYP_MASK easily?
//
bool Compiler::canMorphVectorOperandToMask(GenTree* node)
{
    return varTypeIsMask(node) || node->OperIsConvertMaskToVector() || node->IsVectorZero();
}

//------------------------------------------------------------------------
// canMorphAllVectorOperandsToMasks: Can all vector operands to this node
//                                   be converted to a node with type
//                                   TYP_MASK easily?
//
bool Compiler::canMorphAllVectorOperandsToMasks(GenTreeHWIntrinsic* node)
{
    bool allMaskConversions = true;
    for (size_t i = 1; i <= node->GetOperandCount() && allMaskConversions; i++)
    {
        allMaskConversions &= canMorphVectorOperandToMask(node->Op(i));
    }

    return allMaskConversions;
}

//------------------------------------------------------------------------
// doMorphVectorOperandToMask: Morph a vector node that is close to a mask
//                             node into a mask node.
//
// Return value:
//      The morphed tree, or nullptr if the transform is not applicable.
//
GenTree* Compiler::doMorphVectorOperandToMask(GenTree* node, GenTreeHWIntrinsic* parent)
{
    if (varTypeIsMask(node))
    {
        // Already a mask, nothing to do.
        return node;
    }
    else if (node->OperIsConvertMaskToVector())
    {
        // Replace node with op1.
        return node->AsHWIntrinsic()->Op(1);
    }
    else if (node->IsVectorZero())
    {
        // Morph the vector of zeroes into mask of zeroes.
        GenTree* mask = gtNewSimdFalseMaskByteNode(parent->GetSimdSize());
        mask->SetMorphed(this);
        return mask;
    }

    return nullptr;
}

//-----------------------------------------------------------------------------------------------------
// fgMorphTryUseAllMaskVariant: For NamedIntrinsics that have a variant where all operands are
//                              mask nodes. If all operands to this node are 'suggesting' that they
//                              originate closely from a mask, but are of vector types, then morph the
//                              operands as appropriate to use mask types instead. 'Suggesting'
//                              is defined by the canMorphVectorOperandToMask function.
//
// Arguments:
//    tree - The HWIntrinsic to try and optimize.
//
// Return Value:
//    The fully morphed tree if a change was made, else nullptr.
//
GenTree* Compiler::fgMorphTryUseAllMaskVariant(GenTreeHWIntrinsic* node)
{
    if (HWIntrinsicInfo::HasAllMaskVariant(node->GetHWIntrinsicId()))
    {
        NamedIntrinsic maskVariant = HWIntrinsicInfo::GetMaskVariant(node->GetHWIntrinsicId());

        // As some intrinsics have many variants, check that the count of operands on the node
        // matches the number of operands required for the mask variant of the intrinsic. The mask
        // variant of the intrinsic must have a fixed number of operands.
        int numArgs = HWIntrinsicInfo::lookupNumArgs(maskVariant);
        assert(numArgs >= 0);
        if (node->GetOperandCount() == (size_t)numArgs)
        {
            // We're sure it will work at this point, so perform the pattern match on operands.
            if (canMorphAllVectorOperandsToMasks(node))
            {
                switch (node->GetOperandCount())
                {
                    case 1:
                        node->ResetHWIntrinsicId(maskVariant, doMorphVectorOperandToMask(node->Op(1), node));
                        break;
                    case 2:
                        node->ResetHWIntrinsicId(maskVariant, doMorphVectorOperandToMask(node->Op(1), node),
                                                 doMorphVectorOperandToMask(node->Op(2), node));
                        break;
                    case 3:
                        node->ResetHWIntrinsicId(maskVariant, this, doMorphVectorOperandToMask(node->Op(1), node),
                                                 doMorphVectorOperandToMask(node->Op(2), node),
                                                 doMorphVectorOperandToMask(node->Op(3), node));
                        break;
                    default:
                        unreached();
                }

                node->gtType = TYP_MASK;
                return node;
            }
        }
    }

    return nullptr;
}

#endif // FEATURE_MASKED_HW_INTRINSICS
