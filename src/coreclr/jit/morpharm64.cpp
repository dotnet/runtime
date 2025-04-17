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
// HasAllMaskVariant: Does this intrinsic have a variant where all of it's operands
//                    are mask types?
//
// Return Value:
//     true if an all-mask variant exists for the intrinsic, else false.
//
bool GenTreeHWIntrinsic::HasAllMaskVariant()
{
    switch (GetHWIntrinsicId())
    {
        // ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        // ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        // UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        // UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        // TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        // TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        // REV  <Pd>.<T>, <Pn>.<T>
        case NI_Sve_ZipHigh:
        case NI_Sve_ZipLow:
        case NI_Sve_UnzipOdd:
        case NI_Sve_UnzipEven:
        case NI_Sve_TransposeEven:
        case NI_Sve_TransposeOdd:
        case NI_Sve_ReverseElement:
            return true;

        default:
            return false;
    }
}

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
        GenTree* mask = gtNewSimdAllFalseMaskNode(parent->GetSimdSize());
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
    if (node->HasAllMaskVariant() && canMorphAllVectorOperandsToMasks(node))
    {
        for (size_t i = 1; i <= node->GetOperandCount(); i++)
        {
            node->Op(i) = doMorphVectorOperandToMask(node->Op(i), node);
        }

        node->gtType = TYP_MASK;
        return node;
    }

    if (node->OperIsHWIntrinsic(NI_Sve_ConditionalSelect))
    {
        GenTree* mask  = node->Op(1);
        GenTree* left  = node->Op(2);
        GenTree* right = node->Op(3);

        if (left->OperIsHWIntrinsic())
        {
            assert(canMorphVectorOperandToMask(mask));

            if (canMorphAllVectorOperandsToMasks(left->AsHWIntrinsic()))
            {
                // At this point we know the 'left' node is a HWINTRINSIC node and all of its operands look like
                // mask nodes.
                //
                // The ConditionalSelect could be substituted for the named intrinsic in it's 'left' operand and
                // transformed to a mask-type operation for some named intrinsics. Doing so will encourage codegen
                // to emit predicate variants of instructions rather than vector variants, and we can lose some
                // unnecessary mask->vector conversion nodes.
                GenTreeHWIntrinsic* actualOp = left->AsHWIntrinsic();

                switch (actualOp->GetHWIntrinsicId())
                {
                    // AND <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B
                    // BIC <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B
                    // EOR <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B
                    // ORR <Pd>.B, <Pg>/Z, <Pn>.B, <Pm>.B
                    case NI_Sve_And:
                    case NI_Sve_BitwiseClear:
                    case NI_Sve_Xor:
                    case NI_Sve_Or:
                        if (right->IsVectorZero())
                        {
                            // The operation is equivalent for all lane arrangements, because it is a bitwise operation.
                            // It's safe to bash the type to 8-bit required to assemble the instruction.
                            actualOp->SetSimdBaseJitType(CORINFO_TYPE_BYTE);

                            actualOp->ResetHWIntrinsicId(actualOp->GetHWIntrinsicId(), this,
                                                         doMorphVectorOperandToMask(mask, actualOp),
                                                         doMorphVectorOperandToMask(actualOp->Op(1), actualOp),
                                                         doMorphVectorOperandToMask(actualOp->Op(2), actualOp));
                            actualOp->gtType = TYP_MASK;
                            return actualOp;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        // If we got this far, then there was no match on any predicated operation.
        // ConditionalSelect itself can be a mask operation for 8-bit lane types, using
        // SEL <Pd>.B, <Pg>, <Pn>.B, <Pm>.B
        if (canMorphAllVectorOperandsToMasks(node))
        {
            for (size_t i = 1; i <= node->GetOperandCount(); i++)
            {
                node->Op(i) = doMorphVectorOperandToMask(node->Op(i), node);
            }

            // Again this operation is bitwise, so the lane arrangement doesn't matter.
            // We can bash the type to 8-bit.
            node->SetSimdBaseJitType(CORINFO_TYPE_BYTE);

            node->gtType = TYP_MASK;
            return node;
        }
    }

    return nullptr;
}

#endif // FEATURE_MASKED_HW_INTRINSICS
