// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                      VN-Based Assertion Prop                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#include "rangecheck.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------------
// optVNBasedFoldExpr_Call_Memset: Unrolls NI_System_SpanHelpers_Fill for constant length.
//
// Arguments:
//    call - NI_System_SpanHelpers_Fill call to unroll
//
// Return Value:
//    Returns a new tree or nullptr if nothing is changed.
//
GenTree* Compiler::optVNBasedFoldExpr_Call_Memset(GenTreeCall* call)
{
    assert(call->IsSpecialIntrinsic(this, NI_System_SpanHelpers_Fill));

    CallArg* dstArg = call->gtArgs.GetUserArgByIndex(0);
    CallArg* lenArg = call->gtArgs.GetUserArgByIndex(1);
    CallArg* valArg = call->gtArgs.GetUserArgByIndex(2);

    var_types valType     = valArg->GetSignatureType();
    unsigned  lengthScale = genTypeSize(valType);

    if (lengthScale == 1)
    {
        // Lower expands it slightly better.
        JITDUMP("...value's type is byte - leave it for lower to expand.\n");
        return nullptr;
    }

    if (varTypeIsStruct(valType) || varTypeIsGC(valType))
    {
        JITDUMP("...value's type is not supported - bail out.\n");
        return nullptr;
    }

    ValueNum lenVN = vnStore->VNConservativeNormalValue(lenArg->GetNode()->gtVNPair);
    if (!vnStore->IsVNConstant(lenVN))
    {
        JITDUMP("...length is not a constant - bail out.\n");
        return nullptr;
    }

    size_t len = vnStore->CoercedConstantValue<size_t>(lenVN);
    if ((len > getUnrollThreshold(Memset)) ||
        // The first condition prevents the overflow in the second condition.
        // since both len and lengthScale are expected to be small at this point.
        (len * lengthScale) > getUnrollThreshold(Memset))
    {
        JITDUMP("...length is too big to unroll - bail out.\n");
        return nullptr;
    }

    // Some arbitrary threshold if the value is not a constant,
    // since it is unlikely that we can optimize it further.
    if (!valArg->GetNode()->OperIsConst() && (len >= 8))
    {
        JITDUMP("...length is too big to unroll for non-constant value - bail out.\n");
        return nullptr;
    }

    // Spill the side effects directly in the args, we're going to
    // pick them up in the following gtExtractSideEffList
    GenTree* dst = fgMakeMultiUse(&dstArg->NodeRef());
    GenTree* val = fgMakeMultiUse(&valArg->NodeRef());

    GenTree* result = nullptr;
    gtExtractSideEffList(call, &result, GTF_ALL_EFFECT, true);

    for (size_t offset = 0; offset < len; offset++)
    {
        // Clone dst and add offset if necessary.
        GenTree*         offsetNode = gtNewIconNode((ssize_t)(offset * lengthScale), TYP_I_IMPL);
        GenTree*         currDst    = gtNewOperNode(GT_ADD, dst->TypeGet(), gtCloneExpr(dst), offsetNode);
        GenTreeStoreInd* storeInd =
            gtNewStoreIndNode(valType, currDst, gtCloneExpr(val), GTF_IND_UNALIGNED | GTF_IND_ALLOW_NON_ATOMIC);

        // Merge with the previous result.
        result = result == nullptr ? storeInd : gtNewOperNode(GT_COMMA, TYP_VOID, result, storeInd);
    }

    JITDUMP("...optimized into STOREIND(s):\n");
    DISPTREE(result);
    return result;
}

//------------------------------------------------------------------------------
// optVNBasedFoldExpr_Call_Memmove: Unrolls NI_System_SpanHelpers_Memmove/CORINFO_HELP_MEMCPY
//    if possible. This function effectively duplicates LowerCallMemmove.
//    However, unlike LowerCallMemmove, it is able to optimize src into constants with help of VN.
//
// Arguments:
//    call - NI_System_SpanHelpers_Memmove/CORINFO_HELP_MEMCPY call to unroll
//
// Return Value:
//    Returns a new tree or nullptr if nothing is changed.
//
GenTree* Compiler::optVNBasedFoldExpr_Call_Memmove(GenTreeCall* call)
{
    JITDUMP("See if we can optimize NI_System_SpanHelpers_Memmove with help of VN...\n")
    assert(call->IsSpecialIntrinsic(this, NI_System_SpanHelpers_Memmove) ||
           call->IsHelperCall(this, CORINFO_HELP_MEMCPY));

    CallArg* dstArg = call->gtArgs.GetUserArgByIndex(0);
    CallArg* srcArg = call->gtArgs.GetUserArgByIndex(1);
    CallArg* lenArg = call->gtArgs.GetUserArgByIndex(2);
    ValueNum lenVN  = vnStore->VNConservativeNormalValue(lenArg->GetNode()->gtVNPair);
    if (!vnStore->IsVNConstant(lenVN))
    {
        JITDUMP("...length is not a constant - bail out.\n");
        return nullptr;
    }

    size_t len = vnStore->CoercedConstantValue<size_t>(lenVN);
    if (len == 0)
    {
        // Memmove(dst, src, 0) -> no-op.
        // Memmove doesn't dereference src/dst pointers if length is 0.
        JITDUMP("...length is 0 -> optimize to no-op.\n");
        return gtWrapWithSideEffects(gtNewNothingNode(), call, GTF_ALL_EFFECT, true);
    }

    if (len > getUnrollThreshold(Memcpy))
    {
        JITDUMP("...length is too big to unroll - bail out.\n");
        return nullptr;
    }

    // if GetImmutableDataFromAddress returns true, it means that the src is a read-only constant.
    // Thus, dst and src do not overlap (if they do - it's an UB).
    uint8_t* buffer = new (this, CMK_AssertionProp) uint8_t[len];
    if (!GetImmutableDataFromAddress(srcArg->GetNode(), (int)len, buffer))
    {
        JITDUMP("...src is not a constant - fallback to LowerCallMemmove.\n");
        return nullptr;
    }

    // if dstArg is not simple, we replace the arg directly with a temp assignment and
    // continue using that temp - it allows us reliably extract all side effects.
    GenTree* dst = fgMakeMultiUse(&dstArg->NodeRef());

    // Now we're going to emit a chain of STOREIND via COMMA nodes.
    // the very first tree is expected to be side-effects from the original call (including all args)
    GenTree* result = nullptr;
    gtExtractSideEffList(call, &result, GTF_ALL_EFFECT, true);

    unsigned lenRemaining = (unsigned)len;
    while (lenRemaining > 0)
    {
        const ssize_t offset = (ssize_t)len - (ssize_t)lenRemaining;

        // Clone dst and add offset if necessary.
        GenTree* currDst = gtCloneExpr(dst);
        if (offset != 0)
        {
            currDst = gtNewOperNode(GT_ADD, dst->TypeGet(), currDst, gtNewIconNode(offset, TYP_I_IMPL));
        }

        // Create an unaligned STOREIND node using the largest possible word size.
        var_types        type     = roundDownMaxType(lenRemaining);
        GenTree*         srcCns   = gtNewGenericCon(type, buffer + offset);
        GenTreeStoreInd* storeInd = gtNewStoreIndNode(type, currDst, srcCns, GTF_IND_UNALIGNED);
        fgUpdateConstTreeValueNumber(srcCns);

        // Merge with the previous result.
        result = result == nullptr ? storeInd : gtNewOperNode(GT_COMMA, TYP_VOID, result, storeInd);

        lenRemaining -= genTypeSize(type);
    }

    JITDUMP("...optimized into STOREIND(s)!:\n");
    DISPTREE(result);
    return result;
}

//------------------------------------------------------------------------------
// optVNBasedFoldExpr_Call: Folds given call using VN to a simpler tree.
//
// Arguments:
//    block  -  The block containing the tree.
//    parent -  The parent node of the tree.
//    call   -  The call to fold
//
// Return Value:
//    Returns a new tree or nullptr if nothing is changed.
//
GenTree* Compiler::optVNBasedFoldExpr_Call(BasicBlock* block, GenTree* parent, GenTreeCall* call)
{
    switch (call->GetHelperNum())
    {
        case CORINFO_HELP_CHKCASTARRAY:
        case CORINFO_HELP_CHKCASTANY:
        case CORINFO_HELP_CHKCASTINTERFACE:
        case CORINFO_HELP_CHKCASTCLASS:
        case CORINFO_HELP_ISINSTANCEOFARRAY:
        case CORINFO_HELP_ISINSTANCEOFCLASS:
        case CORINFO_HELP_ISINSTANCEOFANY:
        case CORINFO_HELP_ISINSTANCEOFINTERFACE:
        {
            CallArg* castClsCallArg = call->gtArgs.GetUserArgByIndex(0);
            CallArg* castObjCallArg = call->gtArgs.GetUserArgByIndex(1);
            GenTree* castClsArg     = castClsCallArg->GetNode();
            GenTree* castObjArg     = castObjCallArg->GetNode();

            // If object has the same VN as the cast, then the cast is effectively a no-op.
            //
            if (castObjArg->gtVNPair == call->gtVNPair)
            {
                // if castObjArg is not simple, we replace the arg with a temp assignment and
                // continue using that temp - it allows us reliably extract all side effects
                castObjArg = fgMakeMultiUse(&castObjCallArg->NodeRef());
                return gtWrapWithSideEffects(castObjArg, call, GTF_ALL_EFFECT, true);
            }

            // Let's see if gtGetClassHandle may help us to fold the cast (since VNForCast did not).
            if (castClsArg->IsIconHandle(GTF_ICON_CLASS_HDL))
            {
                bool                 isExact;
                bool                 isNonNull;
                CORINFO_CLASS_HANDLE castFrom = gtGetClassHandle(castObjArg, &isExact, &isNonNull);
                if (castFrom != NO_CLASS_HANDLE)
                {
                    CORINFO_CLASS_HANDLE castTo = gtGetHelperArgClassHandle(castClsArg);
                    // Constant prop may fail to propagate compile time class handles, so verify we have
                    // a handle before invoking the runtime.
                    if ((castTo != NO_CLASS_HANDLE) &&
                        info.compCompHnd->compareTypesForCast(castFrom, castTo) == TypeCompareState::Must)
                    {
                        // if castObjArg is not simple, we replace the arg with a temp assignment and
                        // continue using that temp - it allows us reliably extract all side effects
                        castObjArg = fgMakeMultiUse(&castObjCallArg->NodeRef());
                        return gtWrapWithSideEffects(castObjArg, call, GTF_ALL_EFFECT, true);
                    }
                }
            }
        }
        break;

        default:
            break;
    }

    if (call->IsSpecialIntrinsic(this, NI_System_SpanHelpers_Memmove) || call->IsHelperCall(this, CORINFO_HELP_MEMCPY))
    {
        return optVNBasedFoldExpr_Call_Memmove(call);
    }

    if (call->IsSpecialIntrinsic(this, NI_System_SpanHelpers_Fill))
    {
        return optVNBasedFoldExpr_Call_Memset(call);
    }

    return nullptr;
}

//------------------------------------------------------------------------------
// optVNBasedFoldExpr: Folds given tree using VN to a constant or a simpler tree.
//
// Arguments:
//    block  -  The block containing the tree.
//    parent -  The parent node of the tree.
//    tree   -  The tree to fold.
//
// Return Value:
//    Returns a new tree or nullptr if nothing is changed.
//
GenTree* Compiler::optVNBasedFoldExpr(BasicBlock* block, GenTree* parent, GenTree* tree)
{
    // First, attempt to fold it to a constant if possible.
    GenTree* foldedToCns = optVNBasedFoldConstExpr(block, parent, tree);
    if (foldedToCns != nullptr)
    {
        return foldedToCns;
    }

    switch (tree->OperGet())
    {
        case GT_CALL:
            return optVNBasedFoldExpr_Call(block, parent, tree->AsCall());

            // We can add more VN-based foldings here.

        default:
            break;
    }
    return nullptr;
}

//------------------------------------------------------------------------------
// optVNBasedFoldConstExpr: Substitutes tree with an evaluated constant while
//                          managing side-effects.
//
// Arguments:
//    block  -  The block containing the tree.
//    parent -  The parent node of the tree.
//    tree   -  The tree node whose value is known at compile time.
//              The tree should have a constant value number.
//
// Return Value:
//    Returns a potentially new or a transformed tree node.
//    Returns nullptr when no transformation is possible.
//
// Description:
//    Transforms a tree node if its result evaluates to a constant. The
//    transformation can be a "ChangeOper" to a constant or a new constant node
//    with extracted side-effects.
//
//    Before replacing or substituting the "tree" with a constant, extracts any
//    side effects from the "tree" and creates a comma separated side effect list
//    and then appends the transformed node at the end of the list.
//    This comma separated list is then returned.
//
//    For JTrue nodes, side effects are not put into a comma separated list. If
//    the relop will evaluate to "true" or "false" statically, then the side-effects
//    will be put into new statements, presuming the JTrue will be folded away.
//
GenTree* Compiler::optVNBasedFoldConstExpr(BasicBlock* block, GenTree* parent, GenTree* tree)
{
    if (tree->OperIs(GT_JTRUE))
    {
        // Treat JTRUE separately to extract side effects into respective statements rather
        // than using a COMMA separated op1.
        return optVNConstantPropOnJTrue(block, tree);
    }
    // If relop is part of JTRUE, this should be optimized as part of the parent JTRUE.
    // Or if relop is part of QMARK or anything else, we simply bail here.
    else if (tree->OperIsCompare() && (tree->gtFlags & GTF_RELOP_JMP_USED))
    {
        return nullptr;
    }

    // We want to use the Normal ValueNumber when checking for constants.
    ValueNumPair vnPair = tree->gtVNPair;
    ValueNum     vnCns  = vnStore->VNConservativeNormalValue(vnPair);

    // Check if node evaluates to a constant
    if (!vnStore->IsVNConstant(vnCns))
    {
        // Last chance - propagate VNF_PtrToLoc(lcl, offset) as GT_LCL_ADDR node
        VNFuncApp funcApp;
        if (((tree->gtFlags & GTF_SIDE_EFFECT) == 0) && vnStore->GetVNFunc(vnCns, &funcApp) &&
            (funcApp.m_func == VNF_PtrToLoc))
        {
            unsigned lcl  = (unsigned)vnStore->CoercedConstantValue<size_t>(funcApp.m_args[0]);
            unsigned offs = (unsigned)vnStore->CoercedConstantValue<size_t>(funcApp.m_args[1]);
            return gtNewLclAddrNode(lcl, offs, tree->TypeGet());
        }

        return nullptr;
    }

    GenTree* conValTree = nullptr;
    switch (vnStore->TypeOfVN(vnCns))
    {
        case TYP_FLOAT:
        {
            float value = vnStore->ConstantValue<float>(vnCns);

            if (tree->TypeIs(TYP_INT))
            {
                // Same sized reinterpretation of bits to integer
                conValTree = gtNewIconNode(*(reinterpret_cast<int*>(&value)));
            }
            else
            {
                // Implicit conversion to float or double
                assert(varTypeIsFloating(tree->TypeGet()));
                conValTree = gtNewDconNode(FloatingPointUtils::convertToDouble(value), tree->TypeGet());
            }
            break;
        }

        case TYP_DOUBLE:
        {
            double value = vnStore->ConstantValue<double>(vnCns);

            if (tree->TypeIs(TYP_LONG))
            {
                conValTree = gtNewLconNode(*(reinterpret_cast<INT64*>(&value)));
            }
            else
            {
                // Implicit conversion to float or double
                assert(varTypeIsFloating(tree->TypeGet()));
                conValTree = gtNewDconNode(value, tree->TypeGet());
            }
            break;
        }

        case TYP_LONG:
        {
            INT64 value = vnStore->ConstantValue<INT64>(vnCns);

#ifdef TARGET_64BIT
            if (vnStore->IsVNHandle(vnCns))
            {
                // Don't perform constant folding that involves a handle that needs
                // to be recorded as a relocation with the VM.
                if (!opts.compReloc)
                {
                    conValTree = gtNewIconHandleNode(value, vnStore->GetHandleFlags(vnCns));
                }
            }
            else
#endif
            {
                switch (tree->TypeGet())
                {
                    case TYP_INT:
                        // Implicit conversion to smaller integer
                        conValTree = gtNewIconNode(static_cast<int>(value));
                        break;

                    case TYP_LONG:
                        // Same type no conversion required
                        conValTree = gtNewLconNode(value);
                        break;

                    case TYP_FLOAT:
                        // No implicit conversions from long to float and value numbering will
                        // not propagate through memory reinterpretations of different size.
                        unreached();
                        break;

                    case TYP_DOUBLE:
                        // Same sized reinterpretation of bits to double
                        conValTree = gtNewDconNodeD(*(reinterpret_cast<double*>(&value)));
                        break;

                    default:
                        // Do not support such optimization.
                        break;
                }
            }
        }
        break;

        case TYP_REF:
        {
            if (tree->TypeIs(TYP_REF))
            {
                const size_t value = vnStore->ConstantValue<size_t>(vnCns);
                if (value == 0)
                {
                    conValTree = gtNewNull();
                }
                else
                {
                    assert(vnStore->IsVNObjHandle(vnCns));
                    conValTree = gtNewIconHandleNode(value, GTF_ICON_OBJ_HDL);
                }
            }
        }
        break;

        case TYP_INT:
        {
            int value = vnStore->ConstantValue<int>(vnCns);
#ifndef TARGET_64BIT
            if (vnStore->IsVNHandle(vnCns))
            {
                // Don't perform constant folding that involves a handle that needs
                // to be recorded as a relocation with the VM.
                if (!opts.compReloc)
                {
                    conValTree = gtNewIconHandleNode(value, vnStore->GetHandleFlags(vnCns));
                }
            }
            else
#endif
            {
                switch (tree->TypeGet())
                {
                    case TYP_REF:
                    case TYP_INT:
                        // Same type no conversion required
                        conValTree = gtNewIconNode(value);
                        break;

                    case TYP_LONG:
                        // Implicit conversion to larger integer
                        conValTree = gtNewLconNode(value);
                        break;

                    case TYP_FLOAT:
                        // Same sized reinterpretation of bits to float
                        conValTree = gtNewDconNodeF(BitOperations::UInt32BitsToSingle((uint32_t)value));
                        break;

                    case TYP_DOUBLE:
                        // No implicit conversions from int to double and value numbering will
                        // not propagate through memory reinterpretations of different size.
                        unreached();
                        break;

                    case TYP_BYTE:
                    case TYP_UBYTE:
                    case TYP_SHORT:
                    case TYP_USHORT:
                        assert(FitsIn(tree->TypeGet(), value));
                        conValTree = gtNewIconNode(value);
                        break;

                    default:
                        // Do not support (e.g. byref(const int)).
                        break;
                }
            }
        }
        break;

#if defined(FEATURE_SIMD)
        case TYP_SIMD8:
        {
            simd8_t value = vnStore->ConstantValue<simd8_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet());
            memcpy(&vecCon->gtSimdVal, &value, sizeof(simd8_t));

            conValTree = vecCon;
            break;
        }

        case TYP_SIMD12:
        {
            simd12_t value = vnStore->ConstantValue<simd12_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet());
            memcpy(&vecCon->gtSimdVal, &value, sizeof(simd12_t));

            conValTree = vecCon;
            break;
        }

        case TYP_SIMD16:
        {
            simd16_t value = vnStore->ConstantValue<simd16_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet());
            memcpy(&vecCon->gtSimdVal, &value, sizeof(simd16_t));

            conValTree = vecCon;
            break;
        }

#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        {
            simd32_t value = vnStore->ConstantValue<simd32_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet());
            memcpy(&vecCon->gtSimdVal, &value, sizeof(simd32_t));

            conValTree = vecCon;
            break;
        }

        case TYP_SIMD64:
        {
            simd64_t value = vnStore->ConstantValue<simd64_t>(vnCns);

            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet());
            memcpy(&vecCon->gtSimdVal, &value, sizeof(simd64_t));

            conValTree = vecCon;
            break;
        }
        break;

#endif // TARGET_XARCH
#endif // FEATURE_SIMD

#if defined(FEATURE_MASKED_HW_INTRINSICS)
        case TYP_MASK:
        {
            simdmask_t value = vnStore->ConstantValue<simdmask_t>(vnCns);

            GenTreeMskCon* mskCon = gtNewMskConNode(tree->TypeGet());
            memcpy(&mskCon->gtSimdMaskVal, &value, sizeof(simdmask_t));

            conValTree = mskCon;
            break;
        }
        break;
#endif // FEATURE_MASKED_HW_INTRINSICS

        case TYP_BYREF:
            // Do not support const byref optimization.
            break;

        default:
            // We do not record constants of other types.
            unreached();
            break;
    }

    if (conValTree != nullptr)
    {
        if (!optIsProfitableToSubstitute(tree, block, parent, conValTree))
        {
            // Not profitable to substitute
            return nullptr;
        }

        // Were able to optimize.
        conValTree->gtVNPair = vnPair;
        return gtWrapWithSideEffects(conValTree, tree, GTF_SIDE_EFFECT, true);
    }
    else
    {
        // Was not able to optimize.
        return nullptr;
    }
}

//------------------------------------------------------------------------------
// optIsProfitableToSubstitute: Checks if value worth substituting to dest
//
// Arguments:
//    dest       - destination to substitute value to
//    destBlock  - Basic block of destination
//    destParent - Parent of destination
//    value      - value we plan to substitute
//
// Returns:
//    False if it's likely not profitable to do substitution, True otherwise
//
bool Compiler::optIsProfitableToSubstitute(GenTree* dest, BasicBlock* destBlock, GenTree* destParent, GenTree* value)
{
    // Giving up on these kinds of handles demonstrated size improvements
    if (value->IsIconHandle(GTF_ICON_STATIC_HDL, GTF_ICON_CLASS_HDL))
    {
        return false;
    }

    // A simple heuristic: If the constant is defined outside of a loop (not far from its head)
    // and is used inside it - don't propagate.
    //
    // TODO: Extend on more kinds of trees

    if (!dest->OperIs(GT_LCL_VAR))
    {
        return true;
    }

    const GenTreeLclVar* lcl = dest->AsLclVar();

    if (value->IsCnsVec())
    {
#if defined(FEATURE_HW_INTRINSICS)
        // Many hwintrinsics can't benefit from constant prop because they don't support
        // constant folding nor do they support any specialized encodings. So, we want to
        // skip constant prop and preserve any user-defined locals in that scenario.
        //
        // However, if the local is only referenced once then we want to allow propagation
        // regardless since we can then contain the only actual usage and save a needless
        // instruction.
        //
        // To determine number of uses, we prefer checking SSA first since it is more exact
        // and can account for patterns where a local is reassigned later. However, if we
        // can't find an SSA then we fallback to the naive ref count of the local, noting
        // that we need to check for greater than 2 since it includes both the def and use.

        bool inspectIntrinsic = false;

        if ((destParent != nullptr) && destParent->OperIsHWIntrinsic())
        {
            LclVarDsc* varDsc = lvaGetDesc(lcl);

            if (lcl->HasSsaName())
            {
                inspectIntrinsic = varDsc->GetPerSsaData(lcl->GetSsaNum())->GetNumUses() > 1;
            }
            else
            {
                inspectIntrinsic = varDsc->lvRefCnt() > 2;
            }
        }

        if (inspectIntrinsic)
        {
            GenTreeHWIntrinsic* parent      = destParent->AsHWIntrinsic();
            NamedIntrinsic      intrinsicId = parent->GetHWIntrinsicId();

            if (!HWIntrinsicInfo::CanBenefitFromConstantProp(intrinsicId))
            {
                return false;
            }

            // For several of the scenarios we may skip the costing logic
            // since we know that the operand is always containable and therefore
            // is always cost effective to propagate.

            return parent->ShouldConstantProp(dest, value->AsVecCon());
        }
#endif // FEATURE_HW_INTRINSICS
    }
    else if (!value->IsCnsFltOrDbl() && !value->IsCnsMsk())
    {
        return true;
    }

    gtPrepareCost(value);

    if ((value->GetCostEx() > 1) && (value->GetCostSz() > 1))
    {
        // Try to find the block this constant was originally defined in
        if (lcl->HasSsaName())
        {
            BasicBlock* defBlock = lvaGetDesc(lcl)->GetPerSsaData(lcl->GetSsaNum())->GetBlock();
            if (defBlock != nullptr)
            {
                // Avoid propagating if the weighted use cost is significantly greater than the def cost.
                // NOTE: this currently does not take "a float living across a call" case into account
                // where we might end up with spill/restore on ABIs without callee-saved registers
                const weight_t defBlockWeight = defBlock->getBBWeight(this);
                const weight_t lclblockWeight = destBlock->getBBWeight(this);

                if ((defBlockWeight > 0) && ((lclblockWeight / defBlockWeight) >= BB_LOOP_WEIGHT_SCALE))
                {
                    JITDUMP("Constant propagation inside loop " FMT_BB " is not profitable\n", destBlock->bbNum);
                    return false;
                }
            }
        }
    }
    return true;
}

// Callback data for the VN based constant prop visitor.
struct VNAssertionPropVisitorInfo
{
    Compiler*   m_compiler;
    Statement*  stmt;
    BasicBlock* block;
    VNAssertionPropVisitorInfo(Compiler* pThis, BasicBlock* block, Statement* stmt)
        : m_compiler(pThis)
        , stmt(stmt)
        , block(block)
    {
    }
};

//------------------------------------------------------------------------------
// optVNConstantPropOnJTrue
//    Constant propagate on the JTrue node.
//
// Arguments:
//    block - The block that contains the JTrue.
//    test  - The JTrue node whose relop evaluates to 0 or non-zero value.
//
// Return Value:
//    nullptr if no constant propagation is done, else the modified JTrue node
//    containing "0==0" or "0!=0" relop node
//    (where op1 is wrapped with side effects if any).
//
GenTree* Compiler::optVNConstantPropOnJTrue(BasicBlock* block, GenTree* test)
{
    GenTree* relop = test->gtGetOp1();

    // VN based assertion non-null on this relop has been performed.
    if (!relop->OperIsCompare())
    {
        return nullptr;
    }

    //
    // Make sure GTF_RELOP_JMP_USED flag is set so that we can later skip constant
    // prop'ing a JTRUE's relop child node for a second time in the pre-order
    // tree walk.
    //
    assert((relop->gtFlags & GTF_RELOP_JMP_USED) != 0);

    // We want to use the Normal ValueNumber when checking for constants.
    ValueNum vnCns = vnStore->VNConservativeNormalValue(relop->gtVNPair);
    if (!vnStore->IsVNConstant(vnCns))
    {
        return nullptr;
    }

    GenTree* sideEffects = gtWrapWithSideEffects(gtNewNothingNode(), relop);
    if (!sideEffects->IsNothingNode())
    {
        // Insert side effects before the JTRUE stmt.
        Statement* newStmt = fgNewStmtNearEnd(block, sideEffects);
        fgMorphBlockStmt(block, newStmt DEBUGARG(__FUNCTION__));
    }

    // Let's maintain the invariant that JTRUE's operand is always a relop.
    // and if we have side effects, we wrap one of the operands with them, not the relop.
    const bool evalsToTrue = (vnStore->CoercedConstantValue<INT64>(vnCns) != 0);
    test->AsOp()->gtOp1    = gtNewOperNode(evalsToTrue ? GT_EQ : GT_NE, relop->TypeGet(), gtNewFalse(), gtNewFalse());
    return test;
}

//------------------------------------------------------------------------------
// optVNBasedFoldCurStmt: Performs VN-based folding
//    on the current statement's tree nodes using VN.
//
// Assumption:
//    This function is called as part of a post-order tree walk.
//
// Arguments:
//    tree   - The currently visited tree node.
//    stmt   - The statement node in which the "tree" is present.
//    parent - The parent node of the tree.
//    block  - The block that contains the statement that contains the tree.
//
// Return Value:
//    Returns the standard visitor walk result.
//
Compiler::fgWalkResult Compiler::optVNBasedFoldCurStmt(BasicBlock* block,
                                                       Statement*  stmt,
                                                       GenTree*    parent,
                                                       GenTree*    tree)
{
    // Don't try and fold expressions marked with GTF_DONT_CSE
    // TODO-ASG: delete.
    if (!tree->CanCSE())
    {
        return WALK_CONTINUE;
    }

    // Don't propagate floating-point constants into a TYP_STRUCT LclVar
    // This can occur for HFA return values (see hfa_sf3E_r.exe)
    if (tree->TypeIs(TYP_STRUCT))
    {
        return WALK_CONTINUE;
    }

    switch (tree->OperGet())
    {
        // Make sure we have an R-value.
        case GT_ADD:
        case GT_SUB:
        case GT_DIV:
        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_OR:
        case GT_XOR:
        case GT_AND:
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_NEG:
        case GT_CAST:
        case GT_BITCAST:
        case GT_INTRINSIC:
#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
#endif // FEATURE_HW_INTRINSICS
        case GT_ARR_LENGTH:
            break;

        case GT_BLK:
        case GT_IND:
        {
            const ValueNum vn = tree->GetVN(VNK_Conservative);
            if (vnStore->VNNormalValue(vn) != vn)
            {
                return WALK_CONTINUE;
            }
        }
        break;

        case GT_JTRUE:
            break;

        case GT_MUL:
            // Don't transform long multiplies.
            if (tree->gtFlags & GTF_MUL_64RSLT)
            {
                return WALK_CONTINUE;
            }
            break;

        case GT_LCL_VAR:
        case GT_LCL_FLD:
            // Let's not conflict with CSE (to save the movw/movt).
            if (lclNumIsCSE(tree->AsLclVarCommon()->GetLclNum()))
            {
                return WALK_CONTINUE;
            }
            break;

        case GT_CALL:
            // The checks aren't for correctness, but to avoid unnecessary work.
            if (!tree->AsCall()->IsPure(this) && !tree->AsCall()->IsSpecialIntrinsic())
            {
                return WALK_CONTINUE;
            }
            break;

        default:
            // Unknown node, continue to walk.
            return WALK_CONTINUE;
    }

    // Perform the VN-based folding:
    GenTree* newTree = optVNBasedFoldExpr(block, parent, tree);

    if (newTree == nullptr)
    {
        // Not propagated, keep going.
        return WALK_CONTINUE;
    }

    optAssertionProp_Update(newTree, tree, stmt);

    JITDUMP("After VN-based fold of [%06u]:\n", tree->gtTreeID);
    DBEXEC(VERBOSE, gtDispStmt(stmt));

    return WALK_CONTINUE;
}

//------------------------------------------------------------------------------
// optVnNonNullPropCurStmt
//    Performs VN based non-null propagation on the tree node.
//
// Assumption:
//    This function is called as part of a pre-order tree walk.
//
// Arguments:
//    block - The block that contains the statement that contains the tree.
//    stmt  - The statement node in which the "tree" is present.
//    tree  - The currently visited tree node.
//
// Return Value:
//    None.
//
// Description:
//    Performs value number based non-null propagation on GT_CALL and
//    indirections. This is different from flow based assertions and helps
//    unify VN based constant prop and non-null prop in a single pre-order walk.
//
void Compiler::optVnNonNullPropCurStmt(BasicBlock* block, Statement* stmt, GenTree* tree)
{
    ASSERT_TP empty   = BitVecOps::UninitVal();
    GenTree*  newTree = nullptr;
    if (tree->OperIs(GT_CALL))
    {
        newTree = optNonNullAssertionProp_Call(empty, tree->AsCall());
    }
    else if (tree->OperIsIndir())
    {
        newTree = optAssertionProp_Ind(empty, tree, stmt);
    }
    if (newTree)
    {
        assert(newTree == tree);
        optAssertionProp_Update(newTree, tree, stmt);
    }
}

//------------------------------------------------------------------------------
// optVNAssertionPropCurStmtVisitor
//    Unified Value Numbering based assertion propagation visitor.
//
// Assumption:
//    This function is called as part of a post-order tree walk.
//
// Return Value:
//    WALK_RESULTs.
//
// Description:
//    An unified value numbering based assertion prop visitor that
//    performs non-null and constant assertion propagation based on
//    value numbers.
//
/* static */
Compiler::fgWalkResult Compiler::optVNAssertionPropCurStmtVisitor(GenTree** ppTree, fgWalkData* data)
{
    VNAssertionPropVisitorInfo* pData = (VNAssertionPropVisitorInfo*)data->pCallbackData;
    Compiler*                   pThis = pData->m_compiler;

    pThis->optVnNonNullPropCurStmt(pData->block, pData->stmt, *ppTree);

    return pThis->optVNBasedFoldCurStmt(pData->block, pData->stmt, data->parent, *ppTree);
}

/*****************************************************************************
 *
 *   Perform VN based i.e., data flow based assertion prop first because
 *   even if we don't gen new control flow assertions, we still propagate
 *   these first.
 *
 *   Returns the skipped next stmt if the current statement or next few
 *   statements got removed, else just returns the incoming stmt.
 */
Statement* Compiler::optVNAssertionPropCurStmt(BasicBlock* block, Statement* stmt)
{
    // TODO-Review: EH successor/predecessor iteration seems broken.
    // See: SELF_HOST_TESTS_ARM\jit\Directed\ExcepFilters\fault\fault.exe
    if (block->bbCatchTyp == BBCT_FAULT)
    {
        return stmt;
    }

    // Preserve the prev link before the propagation and morph.
    Statement* prev = (stmt == block->firstStmt()) ? nullptr : stmt->GetPrevStmt();

    // Perform VN based assertion prop first, in case we don't find
    // anything in assertion gen.
    optAssertionPropagatedCurrentStmt = false;

    VNAssertionPropVisitorInfo data(this, block, stmt);
    fgWalkTreePost(stmt->GetRootNodePointer(), Compiler::optVNAssertionPropCurStmtVisitor, &data);

    if (optAssertionPropagatedCurrentStmt)
    {
        fgMorphBlockStmt(block, stmt DEBUGARG("optVNAssertionPropCurStmt"));
    }

    // Check if propagation removed statements starting from current stmt.
    // If so, advance to the next good statement.
    Statement* nextStmt = (prev == nullptr) ? block->firstStmt() : prev->GetNextStmt();
    return nextStmt;
}
