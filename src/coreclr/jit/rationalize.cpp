// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "sideeffects.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

// RewriteNodeAsCall : Replace the given tree node by a GT_CALL.
//
// Arguments:
//    use                - A pointer-to-a-pointer for the tree node
//    sig                - The signature info for callHnd
//    parents            - A pointer to tree walk data providing the context
//    callHnd            - The method handle of the call to be generated
//    entryPoint         - The method entrypoint of the call to be generated
//    operands           - The operand  list of the call to be generated
//    operandCount       - The number of operands in the operand list
//    isSpecialIntrinsic - true if the GT_CALL should be marked as a special intrinsic
//
// Return Value:
//    None.
//
void Rationalizer::RewriteNodeAsCall(GenTree**             use,
                                     CORINFO_SIG_INFO*     sig,
                                     ArrayStack<GenTree*>& parents,
                                     CORINFO_METHOD_HANDLE callHnd,
#if defined(FEATURE_READYTORUN)
                                     CORINFO_CONST_LOOKUP entryPoint,
#endif // FEATURE_READYTORUN
                                     GenTree** operands,
                                     size_t    operandCount,
                                     bool      isSpecialIntrinsic)
{
    GenTree* const tree           = *use;
    GenTree* const treeFirstNode  = comp->fgGetFirstNode(tree);
    GenTree* const insertionPoint = treeFirstNode->gtPrev;

    BlockRange().Remove(treeFirstNode, tree);

    // Create the call node
    GenTreeCall* call = comp->gtNewCallNode(CT_USER_FUNC, callHnd, tree->TypeGet());

    if (isSpecialIntrinsic)
    {
#if defined(TARGET_XARCH)
        // Mark this as having been a special intrinsic node
        //
        // This is used on xarch to track that it may need vzeroupper inserted to
        // avoid the perf penalty on some hardware.

        call->gtCallMoreFlags |= GTF_CALL_M_SPECIAL_INTRINSIC;
#endif // TARGET_XARCH
    }

    assert(sig != nullptr);
    var_types retType = JITtype2varType(sig->retType);

    if (varTypeIsStruct(retType))
    {
        call->gtRetClsHnd = sig->retTypeClass;
        retType           = comp->impNormStructType(sig->retTypeClass);

        if (retType != call->gtType)
        {
            assert(varTypeIsSIMD(retType));
            call->ChangeType(retType);
        }

#if FEATURE_MULTIREG_RET
        call->InitializeStructReturnType(comp, sig->retTypeClass, call->GetUnmanagedCallConv());
#endif // FEATURE_MULTIREG_RET

        Compiler::structPassingKind howToReturnStruct;
        var_types                   returnType =
            comp->getReturnTypeForStruct(sig->retTypeClass, call->GetUnmanagedCallConv(), &howToReturnStruct);

        if (howToReturnStruct == Compiler::SPK_ByReference)
        {
            assert(returnType == TYP_UNKNOWN);
            call->gtCallMoreFlags |= GTF_CALL_M_RETBUFFARG;
        }
    }

    CORINFO_ARG_LIST_HANDLE sigArg   = sig->args;
    size_t                  firstArg = 0;

    if (sig->hasThis())
    {
        GenTree*   operand = operands[0];
        NewCallArg arg     = NewCallArg::Primitive(operand).WellKnown(WellKnownArg::ThisPointer);

        call->gtArgs.PushBack(comp, arg);
        call->gtFlags |= operand->gtFlags & GTF_ALL_EFFECT;

        firstArg++;
    }

    for (size_t i = firstArg; i < operandCount; i++)
    {
        GenTree* operand = operands[i];

        CORINFO_CLASS_HANDLE clsHnd = NO_CLASS_HANDLE;
        CorInfoType          corTyp = strip(comp->info.compCompHnd->getArgType(sig, sigArg, &clsHnd));
        var_types            sigTyp = JITtype2varType(corTyp);

        NewCallArg arg;

        if (varTypeIsStruct(sigTyp))
        {
            // GenTreeFieldList should not have been introduced
            // for intrinsics that get rewritten back to user calls
            assert(!operand->OperIsFieldList());

            sigTyp = comp->impNormStructType(clsHnd);

            if (varTypeIsMask(operand->TypeGet()))
            {
#if defined(FEATURE_MASKED_HW_INTRINSICS)
                // No managed call takes TYP_MASK, so convert it back to a TYP_SIMD

                unsigned    simdSize;
                CorInfoType simdBaseJitType = comp->getBaseJitTypeAndSizeOfSIMDType(clsHnd, &simdSize);
                assert(simdSize != 0);

                GenTree* cvtNode = comp->gtNewSimdCvtMaskToVectorNode(sigTyp, operand, simdBaseJitType, simdSize);
                BlockRange().InsertAfter(operand, LIR::Range(comp->fgSetTreeSeq(cvtNode), cvtNode));
                operand = cvtNode;
#else
                unreached();
#endif // FEATURE_HW_INTRINSICS
            }
            arg = NewCallArg::Struct(operand, sigTyp, comp->typGetObjLayout(clsHnd));
        }
        else
        {
            arg = NewCallArg::Primitive(operand, sigTyp);
        }

        call->gtArgs.PushBack(comp, arg);
        call->gtFlags |= operand->gtFlags & GTF_ALL_EFFECT;

        sigArg = comp->info.compCompHnd->getArgNext(sigArg);
    }

#if defined(FEATURE_READYTORUN)
    call->AsCall()->setEntryPoint(entryPoint);
#endif // FEATURE_READYTORUN

    unsigned tmpNum = BAD_VAR_NUM;

    if (call->ShouldHaveRetBufArg())
    {
        assert(call->ShouldHaveRetBufArg());

        tmpNum = comp->lvaGrabTemp(true DEBUGARG("return buffer for hwintrinsic"));
        comp->lvaSetStruct(tmpNum, sig->retTypeClass, false);

        GenTree*   destAddr = comp->gtNewLclVarAddrNode(tmpNum, TYP_I_IMPL);
        NewCallArg newArg   = NewCallArg::Primitive(destAddr).WellKnown(WellKnownArg::RetBuffer);

        call->gtArgs.InsertAfterThisOrFirst(comp, newArg);
        call->gtType = TYP_VOID;
    }

    call = comp->fgMorphArgs(call);

    GenTree* result = call;

    // Replace "tree" with "call"
    if (parents.Height() > 1)
    {
        if (tmpNum != BAD_VAR_NUM)
        {
            result = comp->gtNewLclvNode(tmpNum, retType);
        }

        if (varTypeIsMask(tree->TypeGet()))
        {
#if defined(FEATURE_MASKED_HW_INTRINSICS)
            // No managed call returns TYP_MASK, so convert it from a TYP_SIMD

            unsigned    simdSize;
            CorInfoType simdBaseJitType = comp->getBaseJitTypeAndSizeOfSIMDType(call->gtRetClsHnd, &simdSize);
            assert(simdSize != 0);

            result = comp->gtNewSimdCvtVectorToMaskNode(TYP_MASK, result, simdBaseJitType, simdSize);

            if (tmpNum == BAD_VAR_NUM)
            {
                // Propagate flags of "call" to its parent.
                result->gtFlags |= (call->gtFlags & GTF_ALL_EFFECT) | GTF_CALL;
            }
#else
            unreached();
#endif // FEATURE_HW_INTRINSICS
        }

        parents.Top(1)->ReplaceOperand(use, result);

        if (tmpNum != BAD_VAR_NUM)
        {
            // We have a return buffer, so we need to insert both the result and the call
            // since they are independent trees. If we have a convert node, it will indirectly
            // insert the local node.

            comp->gtSetEvalOrder(result);
            BlockRange().InsertAfter(insertionPoint, LIR::Range(comp->fgSetTreeSeq(result), result));

            comp->gtSetEvalOrder(call);
            BlockRange().InsertAfter(insertionPoint, LIR::Range(comp->fgSetTreeSeq(call), call));
        }
        else
        {
            // We don't have a return buffer, so we only need to insert the result, which
            // will indirectly insert the call in the case we have a convert node as well.

            comp->gtSetEvalOrder(result);
            BlockRange().InsertAfter(insertionPoint, LIR::Range(comp->fgSetTreeSeq(result), result));
        }
    }
    else
    {
        // If there's no parent, the tree being replaced is the root of the
        // statement (and no special handling is necessary).
        *use = result;

        comp->gtSetEvalOrder(call);
        BlockRange().InsertAfter(insertionPoint, LIR::Range(comp->fgSetTreeSeq(call), call));
    }

    if (tmpNum == BAD_VAR_NUM)
    {
        // Propagate flags of "call" to its parents.
        GenTreeFlags callFlags = (call->gtFlags & GTF_ALL_EFFECT) | GTF_CALL;

        // 0 is current node, so start at 1
        for (int i = 1; i < parents.Height(); i++)
        {
            parents.Top(i)->gtFlags |= callFlags;
        }
    }
    else
    {
        // Normally the call replaces the node in pre-order, so we automatically continue visiting the call.
        // However, when we have a retbuf the node is replaced by a local with the call inserted before it,
        // so we need to make sure we visit it here.
        RationalizeVisitor visitor(*this);
        GenTree*           node = call;
        visitor.WalkTree(&node, nullptr);
        assert(node == call);
    }

    // Since "tree" is replaced with "result", pop "tree" node (i.e the current node)
    // and replace it with "result" on parent stack.
    assert(parents.Top() == tree);
    (void)parents.Pop();
    parents.Push(result);
}

// RewriteIntrinsicAsUserCall : Rewrite an intrinsic operator as a GT_CALL to the original method.
//
// Arguments:
//    use     - A pointer-to-a-pointer for the intrinsic node
//    parents - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
//
// Some intrinsics, such as operation Sqrt, are rewritten back to calls, and some are not.
// The ones that are not being rewritten here must be handled in Codegen.
// Conceptually, the lower is the right place to do the rewrite. Keeping it in rationalization is
// mainly for throughput issue.
void Rationalizer::RewriteIntrinsicAsUserCall(GenTree** use, ArrayStack<GenTree*>& parents)
{
    GenTreeIntrinsic* intrinsic = (*use)->AsIntrinsic();

    GenTree* operands[2];
    size_t   operandCount = 0;

    operands[0] = intrinsic->gtGetOp1();

    if (operands[0] != nullptr)
    {
        operandCount++;
    }

    operands[1] = intrinsic->gtGetOp2();

    if (operands[1] != nullptr)
    {
        operandCount++;
    }

    CORINFO_METHOD_HANDLE callHnd = intrinsic->gtMethodHandle;

    CORINFO_SIG_INFO sigInfo;
    comp->eeGetMethodSig(callHnd, &sigInfo);

    // Regular Intrinsics often have their fallback in native and so
    // should be treated as "special" once they become calls.
    bool isSpecialIntrinsic = true;

    RewriteNodeAsCall(use, &sigInfo, parents, callHnd,
#if defined(FEATURE_READYTORUN)
                      intrinsic->gtEntryPoint,
#endif // FEATURE_READYTORUN
                      operands, operandCount, isSpecialIntrinsic);
}

//
// Pre-order rewriting for HW Intrinsics
//
#if defined(FEATURE_HW_INTRINSICS)
// RewriteHWIntrinsicAsUserCall : Rewrite a hwintrinsic node as a GT_CALL to the original method.
//
// Arguments:
//    use     - A pointer-to-a-pointer for the intrinsic node
//    parents - A pointer to tree walk data providing the context
//
// Return Value:
//    None.
void Rationalizer::RewriteHWIntrinsicAsUserCall(GenTree** use, ArrayStack<GenTree*>& parents)
{
    GenTreeHWIntrinsic* hwintrinsic     = (*use)->AsHWIntrinsic();
    NamedIntrinsic      intrinsicId     = hwintrinsic->GetHWIntrinsicId();
    CorInfoType         simdBaseJitType = hwintrinsic->GetSimdBaseJitType();
    var_types           simdBaseType    = hwintrinsic->GetSimdBaseType();
    uint32_t            simdSize        = hwintrinsic->GetSimdSize();
    var_types           retType         = hwintrinsic->TypeGet();

    GenTree** operands     = hwintrinsic->GetOperandArray();
    size_t    operandCount = hwintrinsic->GetOperandCount();

    CORINFO_METHOD_HANDLE callHnd = hwintrinsic->GetMethodHandle();

    CORINFO_SIG_INFO sigInfo;
    comp->eeGetMethodSig(callHnd, &sigInfo);

    GenTree* result = nullptr;

    switch (intrinsicId)
    {
#if defined(TARGET_XARCH)
        case NI_AVX_Compare:
        case NI_AVX_CompareScalar:
        case NI_AVX512_CompareMask:
        {
            assert(operandCount == 3);

            GenTree* op1 = operands[0];
            GenTree* op2 = operands[1];
            GenTree* op3 = operands[2];

            if (!op3->IsCnsIntOrI())
            {
                break;
            }

            FloatComparisonMode mode = static_cast<FloatComparisonMode>(op3->AsIntConCommon()->IntegralValue());
            NamedIntrinsic      id =
                HWIntrinsicInfo::lookupIdForFloatComparisonMode(intrinsicId, mode, simdBaseType, simdSize);

            if (id == intrinsicId)
            {
                break;
            }

            result = comp->gtNewSimdHWIntrinsicNode(retType, op1, op2, id, simdBaseJitType, simdSize);
            break;
        }
#endif // TARGET_XARCH

        case NI_Vector128_Shuffle:
        case NI_Vector128_ShuffleNative:
        case NI_Vector128_ShuffleNativeFallback:
#if defined(TARGET_XARCH)
        case NI_Vector256_Shuffle:
        case NI_Vector256_ShuffleNative:
        case NI_Vector256_ShuffleNativeFallback:
        case NI_Vector512_Shuffle:
        case NI_Vector512_ShuffleNative:
        case NI_Vector512_ShuffleNativeFallback:
#elif defined(TARGET_ARM64)
        case NI_Vector64_Shuffle:
        case NI_Vector64_ShuffleNative:
        case NI_Vector64_ShuffleNativeFallback:
#endif
        {
            assert(operandCount == 2);
#if defined(TARGET_XARCH)
            assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));
#else
            assert((simdSize == 8) || (simdSize == 16));
#endif
            assert(((*use)->gtFlags & GTF_REVERSE_OPS) == 0); // gtNewSimdShuffleNode with reverse ops is not supported

            GenTree* op1 = operands[0];
            GenTree* op2 = operands[1];

            bool isShuffleNative = intrinsicId != NI_Vector128_Shuffle;
#if defined(TARGET_XARCH)
            isShuffleNative =
                isShuffleNative && (intrinsicId != NI_Vector256_Shuffle) && (intrinsicId != NI_Vector512_Shuffle);
#elif defined(TARGET_ARM64)
            isShuffleNative = isShuffleNative && (intrinsicId != NI_Vector64_Shuffle);
#endif

            // Check if the required intrinsics to emit are available.
            if (!comp->IsValidForShuffle(op2, simdSize, simdBaseType, nullptr, isShuffleNative))
            {
                break;
            }

            result = comp->gtNewSimdShuffleNode(retType, op1, op2, simdBaseJitType, simdSize, isShuffleNative);
            break;
        }

#if defined(TARGET_XARCH)
        case NI_Vector128_ExtractMostSignificantBits:
        {
            // We want to keep this as is, because we'll rewrite it in post-order
            assert(varTypeIsShort(simdBaseType));
            return;
        }
#endif // TARGET_XARCH

        default:
        {
            if (sigInfo.numArgs == 0)
            {
                break;
            }

            GenTree* immOp1 = nullptr;
            GenTree* immOp2 = nullptr;

            int  immLowerBound   = 0;
            int  immUpperBound   = 0;
            bool hasFullRangeImm = false;
            bool mustExpand      = false;
            bool useFallback     = false;

#if defined(TARGET_XARCH)
            immOp1 = operands[operandCount - 1];

            if (!HWIntrinsicInfo::isImmOp(intrinsicId, immOp1))
            {
                break;
            }

            immUpperBound   = HWIntrinsicInfo::lookupImmUpperBound(intrinsicId);
            hasFullRangeImm = HWIntrinsicInfo::HasFullRangeImm(intrinsicId);
#elif defined(TARGET_ARM64)
            if (!HWIntrinsicInfo::HasImmediateOperand(intrinsicId))
            {
                break;
            }

            // Position of the immediates from top of stack
            int imm1Pos = -1;
            int imm2Pos = -1;

            HWIntrinsicInfo::GetImmOpsPositions(intrinsicId, &sigInfo, &imm1Pos, &imm2Pos);

            if (imm1Pos >= 0)
            {
                immOp1 = operands[operandCount - (1 + imm1Pos)];
                assert(HWIntrinsicInfo::isImmOp(intrinsicId, immOp1));
            }

            if (imm2Pos >= 0)
            {
                immOp2 = operands[operandCount - (1 + imm2Pos)];
                assert(HWIntrinsicInfo::isImmOp(intrinsicId, immOp2));
            }

            unsigned  immSimdSize     = simdSize;
            var_types immSimdBaseType = simdBaseType;

            if (immOp2 != nullptr)
            {
                comp->getHWIntrinsicImmTypes(intrinsicId, &sigInfo, 2, &immSimdSize, &immSimdBaseType);
                HWIntrinsicInfo::lookupImmBounds(intrinsicId, immSimdSize, immSimdBaseType, 2, &immLowerBound,
                                                 &immUpperBound);

                if (comp->CheckHWIntrinsicImmRange(intrinsicId, simdBaseJitType, immOp2, mustExpand, immLowerBound,
                                                   immUpperBound, hasFullRangeImm, &useFallback))
                {
                    // Set this as nullptr so we stay an intrinsic if both immediates are constant and in range
                    immOp2 = nullptr;
                }

                immSimdSize     = simdSize;
                immSimdBaseType = simdBaseType;
            }

            comp->getHWIntrinsicImmTypes(intrinsicId, &sigInfo, 1, &immSimdSize, &immSimdBaseType);
            HWIntrinsicInfo::lookupImmBounds(intrinsicId, immSimdSize, immSimdBaseType, 1, &immLowerBound,
                                             &immUpperBound);
#endif

            if ((immOp2 == nullptr) && (immOp1 != nullptr))
            {
                if (comp->CheckHWIntrinsicImmRange(intrinsicId, simdBaseJitType, immOp1, mustExpand, immLowerBound,
                                                   immUpperBound, hasFullRangeImm, &useFallback))
                {
                    // We're already in the right shape, so just stop tracking ourselves as a user call
                    hwintrinsic->gtFlags &= ~(GTF_HW_USER_CALL | GTF_EXCEPT | GTF_CALL);
                    return;
                }
            }
            break;
        }
    }

    if (result != nullptr)
    {
        GenTree* const hwintrinsicFirstNode = comp->fgGetFirstNode(hwintrinsic);
        GenTree* const insertionPoint       = hwintrinsicFirstNode->gtPrev;

        BlockRange().Remove(hwintrinsicFirstNode, hwintrinsic);

        // Replace "tree" with "call"
        if (parents.Height() > 1)
        {
            parents.Top(1)->ReplaceOperand(use, result);
        }
        else
        {
            // If there's no parent, the tree being replaced is the root of the
            // statement (and no special handling is necessary).
            *use = result;
        }

        comp->gtSetEvalOrder(result);
        BlockRange().InsertAfter(insertionPoint, LIR::Range(comp->fgSetTreeSeq(result), result));

        // Since "hwintrinsic" is replaced with "result", pop "hwintrinsic" node (i.e the current node)
        // and replace it with "result" on parent stack.
        assert(parents.Top() == hwintrinsic);
        (void)parents.Pop();
        parents.Push(result);

        return;
    }

    // Hardware Intrinsics have their fallback in managed and so
    // shouldn't be treated as "special" once they become calls.
    bool isSpecialIntrinsic = false;

    RewriteNodeAsCall(use, &sigInfo, parents, callHnd,
#if defined(FEATURE_READYTORUN)
                      hwintrinsic->GetEntryPoint(),
#endif // FEATURE_READYTORUN
                      operands, operandCount, isSpecialIntrinsic);
}
#endif // FEATURE_HW_INTRINSICS

//
// Post-order rewriting for HW Intrinsics
//
#if defined(FEATURE_HW_INTRINSICS)
// RewriteHWIntrinsic: Rewrite a hwintrinsic node
//
// Arguments:
//    use     - A pointer to the hwintrinsic node
//    parents - A reference to tree walk data providing the context
//
void Rationalizer::RewriteHWIntrinsic(GenTree** use, Compiler::GenTreeStack& parents)
{
    GenTreeHWIntrinsic* node = (*use)->AsHWIntrinsic();

    // Intrinsics should have already been rewritten back into user calls.
    assert(!node->IsUserCall());

    NamedIntrinsic intrinsic = node->GetHWIntrinsicId();

    switch (intrinsic)
    {
#if defined(TARGET_XARCH)
        case NI_AVX512_BlendVariableMask:
        {
            RewriteHWIntrinsicBlendv(use, parents);
            break;
        }

        case NI_AVX512_ConvertMaskToVector:
        case NI_AVX512_MoveMask:
        {
            RewriteHWIntrinsicMaskOp(use, parents);
            break;
        }
#endif // TARGET_XARCH

#if defined(TARGET_ARM64)
        case NI_Vector64_ExtractMostSignificantBits:
#elif defined(TARGET_XARCH)
        case NI_Vector256_ExtractMostSignificantBits:
#endif
        case NI_Vector128_ExtractMostSignificantBits:
        {
            RewriteHWIntrinsicExtractMsb(use, parents);
            break;
        }

        default:
        {
            break;
        }
    }
}

#if defined(TARGET_XARCH)
//----------------------------------------------------------------------------------------------
// RewriteHWIntrinsicBlendv: Rewrites a hwintrinsic blendv operation
//
// Arguments:
//    use     - A pointer to the hwintrinsic node
//    parents - A reference to tree walk data providing the context
//
void Rationalizer::RewriteHWIntrinsicBlendv(GenTree** use, Compiler::GenTreeStack& parents)
{
    GenTreeHWIntrinsic* node = (*use)->AsHWIntrinsic();

    // We normalize all comparisons to be of TYP_MASK on import. However, if we
    // get to rationalization and we cannot take advantage of embedded masking
    // then we want to rewrite things to just directly produce TYP_SIMD instead.

    NamedIntrinsic intrinsic       = node->GetHWIntrinsicId();
    var_types      retType         = node->TypeGet();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();

    if (simdSize == 64)
    {
        return;
    }

    GenTree* op2 = node->Op(2);

    // We're in the post-order visit and are traversing in execution order, so
    // everything between op2 and node will have already been rewritten to LIR
    // form and doing the IsInvariantInRange check is safe. This allows us to
    // catch cases where something is embedded masking compatible but where we
    // could never actually contain it and so we want to rewrite it to the non-mask
    // variant
    SideEffectSet scratchSideEffects;

    if (scratchSideEffects.IsLirInvariantInRange(comp, op2, node))
    {
        unsigned    tgtMaskSize        = simdSize / genTypeSize(simdBaseType);
        CorInfoType tgtSimdBaseJitType = CORINFO_TYPE_UNDEF;

        if (op2->isEmbeddedMaskingCompatible(comp, tgtMaskSize, tgtSimdBaseJitType))
        {
            // We are going to utilize the embedded mask, so we don't need to rewrite. However,
            // we want to fixup the simdBaseJitType here since it simplifies lowering and allows
            // both embedded broadcast and the mask to be live simultaneously.

            if (tgtSimdBaseJitType != CORINFO_TYPE_UNDEF)
            {
                op2->AsHWIntrinsic()->SetSimdBaseJitType(tgtSimdBaseJitType);
            }
            return;
        }
    }

    GenTree*& op3 = node->Op(3);

    if (!ShouldRewriteToNonMaskHWIntrinsic(op3))
    {
        return;
    }

    parents.Push(op3);
    RewriteHWIntrinsicToNonMask(&op3, parents);
    (void)parents.Pop();

    if (simdSize == 32)
    {
        if (varTypeIsIntegral(simdBaseType))
        {
            intrinsic = NI_AVX2_BlendVariable;
        }
        else
        {
            intrinsic = NI_AVX_BlendVariable;
        }
    }
    else
    {
        intrinsic = NI_X86Base_BlendVariable;
    }

    if (HWIntrinsicInfo::NeedsNormalizeSmallTypeToInt(intrinsic) && varTypeIsSmall(simdBaseType))
    {
        node->SetSimdBaseJitType(varTypeIsUnsigned(simdBaseType) ? CORINFO_TYPE_UINT : CORINFO_TYPE_INT);
    }
    node->ChangeHWIntrinsicId(intrinsic);
}

//----------------------------------------------------------------------------------------------
// RewriteHWIntrinsicMaskOp: Rewrites a hwintrinsic mask operation
//
// Arguments:
//    use     - A pointer to the hwintrinsic node
//    parents - A reference to tree walk data providing the context
//
void Rationalizer::RewriteHWIntrinsicMaskOp(GenTree** use, Compiler::GenTreeStack& parents)
{
    GenTreeHWIntrinsic* node = (*use)->AsHWIntrinsic();

    // We normalize all comparisons to be of TYP_MASK on import. However, if we
    // get to rationalization and we're just converting that back to TYP_SIMD,
    // then we want to rewrite things to just directly produce TYP_SIMD instead.

    NamedIntrinsic intrinsic       = node->GetHWIntrinsicId();
    var_types      retType         = node->TypeGet();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();

    if (simdSize == 64)
    {
        // we must always use the evex encoding
        return;
    }

    if ((intrinsic == NI_AVX512_MoveMask) && varTypeIsShort(simdBaseType))
    {
        // we need to keep the evex form as it's more efficient
        return;
    }

    GenTree*& op1 = node->Op(1);

    if (!ShouldRewriteToNonMaskHWIntrinsic(op1))
    {
        return;
    }

    parents.Push(op1);
    RewriteHWIntrinsicToNonMask(&op1, parents);
    (void)parents.Pop();

    if (intrinsic == NI_AVX512_ConvertMaskToVector)
    {
        if (parents.Height() > 1)
        {
            parents.Top(1)->ReplaceOperand(use, op1);
        }
        else
        {
            *use = op1;
        }
        BlockRange().Remove(node);

        // Adjust the parent stack
        assert(parents.Top() == node);
        (void)parents.Pop();
        parents.Push(op1);
    }
    else
    {
        assert(intrinsic == NI_AVX512_MoveMask);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                intrinsic = (simdSize == 32) ? NI_AVX2_MoveMask : NI_X86Base_MoveMask;
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            case TYP_FLOAT:
            {
                simdBaseJitType = CORINFO_TYPE_FLOAT;
                intrinsic       = (simdSize == 32) ? NI_AVX_MoveMask : NI_X86Base_MoveMask;
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            case TYP_DOUBLE:
            {
                simdBaseJitType = CORINFO_TYPE_DOUBLE;
                intrinsic       = (simdSize == 32) ? NI_AVX_MoveMask : NI_X86Base_MoveMask;
                break;
            }

            default:
            {
                unreached();
            }
        }

        node->SetSimdBaseJitType(simdBaseJitType);
        node->ChangeHWIntrinsicId(intrinsic);
    }
}

//----------------------------------------------------------------------------------------------
// RewriteHWIntrinsicToNonMask: Rewrites a hwintrinsic to its non-mask form
//
//  Arguments:
//    use     - A pointer to the hwintrinsic node
//    parents - A reference to tree walk data providing the context
//
void Rationalizer::RewriteHWIntrinsicToNonMask(GenTree** use, Compiler::GenTreeStack& parents)
{
    GenTreeHWIntrinsic* node = (*use)->AsHWIntrinsic();

    assert(node->TypeIs(TYP_MASK));
    assert(ShouldRewriteToNonMaskHWIntrinsic(node));

    NamedIntrinsic intrinsic = node->GetHWIntrinsicId();

    switch (intrinsic)
    {
        case NI_AVX512_AndMask:
        {
            RewriteHWIntrinsicBitwiseOpToNonMask(use, parents, GT_AND);
            break;
        }

        case NI_AVX512_AndNotMask:
        {
            RewriteHWIntrinsicBitwiseOpToNonMask(use, parents, GT_AND_NOT);
            break;
        }

        case NI_AVX512_NotMask:
        {
            RewriteHWIntrinsicBitwiseOpToNonMask(use, parents, GT_NOT);
            break;
        }

        case NI_AVX512_OrMask:
        {
            RewriteHWIntrinsicBitwiseOpToNonMask(use, parents, GT_OR);
            break;
        }

        case NI_AVX512_XorMask:
        {
            RewriteHWIntrinsicBitwiseOpToNonMask(use, parents, GT_XOR);
            break;
        }

        case NI_AVX512_XnorMask:
        {
            CorInfoType simdBaseJitType = node->GetSimdBaseJitType();
            unsigned    simdSize        = node->GetSimdSize();
            var_types   simdType        = Compiler::getSIMDTypeForSize(simdSize);

            GenTree* op1 =
                comp->gtNewSimdBinOpNode(GT_XOR, simdType, node->Op(1), node->Op(2), simdBaseJitType, simdSize);
            BlockRange().InsertBefore(node, op1);
            node->Op(1) = op1;

            GenTree* op2 = comp->gtNewAllBitsSetConNode(simdType);
            BlockRange().InsertBefore(node, op2);
            node->Op(2) = op2;

            RewriteHWIntrinsicBitwiseOpToNonMask(use, parents, GT_XOR);
            break;
        }

        case NI_AVX512_CompareMask:
        case NI_AVX512_CompareEqualMask:
        case NI_AVX512_CompareGreaterThanMask:
        case NI_AVX512_CompareGreaterThanOrEqualMask:
        case NI_AVX512_CompareLessThanMask:
        case NI_AVX512_CompareLessThanOrEqualMask:
        case NI_AVX512_CompareNotEqualMask:
        case NI_AVX512_CompareNotGreaterThanMask:
        case NI_AVX512_CompareNotGreaterThanOrEqualMask:
        case NI_AVX512_CompareNotLessThanMask:
        case NI_AVX512_CompareNotLessThanOrEqualMask:
        case NI_AVX512_CompareOrderedMask:
        case NI_AVX512_CompareUnorderedMask:
        {
            var_types simdBaseType = node->GetSimdBaseType();
            unsigned  simdSize     = node->GetSimdSize();
            var_types simdType     = Compiler::getSIMDTypeForSize(simdSize);

            switch (intrinsic)
            {
                case NI_AVX512_CompareMask:
                {
                    intrinsic = NI_AVX_Compare;
                    break;
                }

                case NI_AVX512_CompareEqualMask:
                {
                    if (simdSize == 32)
                    {
                        if (varTypeIsIntegral(simdBaseType))
                        {
                            intrinsic = NI_AVX2_CompareEqual;
                        }
                        else
                        {
                            intrinsic = NI_AVX_CompareEqual;
                        }
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareEqual;
                    }
                    break;
                }

                case NI_AVX512_CompareGreaterThanMask:
                {
                    if (simdSize == 32)
                    {
                        if (varTypeIsIntegral(simdBaseType))
                        {
                            intrinsic = NI_AVX2_CompareGreaterThan;
                        }
                        else
                        {
                            intrinsic = NI_AVX_CompareGreaterThan;
                        }
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareGreaterThan;
                    }
                    break;
                }

                case NI_AVX512_CompareGreaterThanOrEqualMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareGreaterThanOrEqual;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareGreaterThanOrEqual;
                    }
                    break;
                }

                case NI_AVX512_CompareLessThanMask:
                {
                    if (simdSize == 32)
                    {
                        if (varTypeIsIntegral(simdBaseType))
                        {
                            intrinsic = NI_AVX2_CompareLessThan;
                        }
                        else
                        {
                            intrinsic = NI_AVX_CompareLessThan;
                        }
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareLessThan;
                    }
                    break;
                }

                case NI_AVX512_CompareLessThanOrEqualMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareLessThanOrEqual;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareLessThanOrEqual;
                    }
                    break;
                }

                case NI_AVX512_CompareNotEqualMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareNotEqual;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareNotEqual;
                    }
                    break;
                }

                case NI_AVX512_CompareNotGreaterThanMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareNotGreaterThan;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareNotGreaterThan;
                    }
                    break;
                }

                case NI_AVX512_CompareNotGreaterThanOrEqualMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareNotGreaterThanOrEqual;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareNotGreaterThanOrEqual;
                    }
                    break;
                }

                case NI_AVX512_CompareNotLessThanMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareNotLessThan;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareNotLessThan;
                    }
                    break;
                }

                case NI_AVX512_CompareNotLessThanOrEqualMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareNotLessThanOrEqual;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareNotLessThanOrEqual;
                    }
                    break;
                }

                case NI_AVX512_CompareOrderedMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareOrdered;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareOrdered;
                    }
                    break;
                }

                case NI_AVX512_CompareUnorderedMask:
                {
                    if (simdSize == 32)
                    {
                        intrinsic = NI_AVX_CompareUnordered;
                    }
                    else
                    {
                        intrinsic = NI_X86Base_CompareUnordered;
                    }
                    break;
                }

                default:
                {
                    unreached();
                }
            }

            node->gtType = simdType;
            node->ChangeHWIntrinsicId(intrinsic);

            break;
        }

        case NI_AVX512_ConvertVectorToMask:
        {
            GenTree* op1 = node->Op(1);

            if (parents.Height() > 1)
            {
                parents.Top(1)->ReplaceOperand(use, op1);
            }
            else
            {
                *use = op1;
            }
            BlockRange().Remove(node);

            // Adjust the parent stack
            assert(parents.Top() == node);
            (void)parents.Pop();
            parents.Push(op1);

            break;
        }

        default:
        {
            unreached();
        }
    }
}

//----------------------------------------------------------------------------------------------
// RewriteHWIntrinsicBitwiseOpToNonMask: Rewrites hwintrinsic bitwise operation to its non-mask form
//
//  Arguments:
//    use     - A pointer to the hwintrinsic node
//    parents - A reference to tree walk data providing the context
//    oper    - The operation represented by the hwintrinsic
//
void Rationalizer::RewriteHWIntrinsicBitwiseOpToNonMask(GenTree** use, Compiler::GenTreeStack& parents, genTreeOps oper)
{
    GenTreeHWIntrinsic* node = (*use)->AsHWIntrinsic();
    assert((node->GetOperandCount() == 1) || (node->GetOperandCount() == 2));

    assert(node->TypeIs(TYP_MASK));
    assert(oper != GT_NONE);

    NamedIntrinsic intrinsic    = NI_Illegal;
    var_types      simdBaseType = node->GetSimdBaseType();
    unsigned       simdSize     = node->GetSimdSize();
    var_types      simdType     = Compiler::getSIMDTypeForSize(simdSize);
    const bool     isScalar     = false;

    GenTree*& op1 = node->Op(1);

    parents.Push(op1);
    RewriteHWIntrinsicToNonMask(&op1, parents);
    (void)parents.Pop();

    if (node->GetOperandCount() == 1)
    {
        assert(oper == GT_NOT);

        GenTree* op2 = comp->gtNewAllBitsSetConNode(simdType);
        BlockRange().InsertBefore(node, op2);

        intrinsic =
            GenTreeHWIntrinsic::GetHWIntrinsicIdForBinOp(comp, GT_XOR, op1, op2, simdBaseType, simdSize, isScalar);

        node->gtType = simdType;
        node->ResetHWIntrinsicId(intrinsic, comp, op1, op2);
    }
    else
    {
        GenTree*& op2 = node->Op(2);

        parents.Push(op2);
        RewriteHWIntrinsicToNonMask(&op2, parents);
        (void)parents.Pop();

        intrinsic =
            GenTreeHWIntrinsic::GetHWIntrinsicIdForBinOp(comp, oper, op1, op2, simdBaseType, simdSize, isScalar);

        node->gtType = simdType;
        node->ChangeHWIntrinsicId(intrinsic);
    }
}

//----------------------------------------------------------------------------------------------
// ShouldRewriteToNonMaskHWIntrinsic: Determines if a node is a hwintrinsic that should be rewritten
//                                    to its non-mask form
//
//  Arguments:
//     node - The node to check
//
// Returns:
//     true if node is a hardware intrinsic node and should be converted to its non-mask form; otherwise false
//
bool Rationalizer::ShouldRewriteToNonMaskHWIntrinsic(GenTree* node)
{
    assert(node->TypeIs(TYP_MASK));

    if (!node->OperIsHWIntrinsic())
    {
        // Nothing to optimize if we don't have a hwintrinsic
        return false;
    }

    GenTreeHWIntrinsic* hwNode    = node->AsHWIntrinsic();
    NamedIntrinsic      intrinsic = hwNode->GetHWIntrinsicId();

    if (hwNode->GetSimdSize() == 64)
    {
        // TYP_SIMD64 comparisons always produce a TYP_MASK
        return false;
    }

    switch (intrinsic)
    {
        case NI_AVX512_AndMask:
        case NI_AVX512_AndNotMask:
        case NI_AVX512_OrMask:
        case NI_AVX512_XorMask:
        case NI_AVX512_XnorMask:
        {
            // binary bitwise operations should be optimized if both inputs can
            assert(hwNode->GetOperandCount() == 2);
            return ShouldRewriteToNonMaskHWIntrinsic(hwNode->Op(1)) && ShouldRewriteToNonMaskHWIntrinsic(hwNode->Op(2));
        }

        case NI_AVX512_NotMask:
        {
            // unary bitwise operations should be optimized if the input can
            assert(hwNode->GetOperandCount() == 1);
            return ShouldRewriteToNonMaskHWIntrinsic(hwNode->Op(1));
        }

        case NI_AVX512_CompareMask:
        case NI_AVX512_CompareEqualMask:
        case NI_AVX512_CompareGreaterThanMask:
        case NI_AVX512_CompareGreaterThanOrEqualMask:
        case NI_AVX512_CompareLessThanMask:
        case NI_AVX512_CompareLessThanOrEqualMask:
        case NI_AVX512_CompareNotEqualMask:
        case NI_AVX512_CompareNotGreaterThanMask:
        case NI_AVX512_CompareNotGreaterThanOrEqualMask:
        case NI_AVX512_CompareNotLessThanMask:
        case NI_AVX512_CompareNotLessThanOrEqualMask:
        case NI_AVX512_CompareOrderedMask:
        case NI_AVX512_CompareUnorderedMask:
        {
            assert((hwNode->GetOperandCount() == 2) || (hwNode->GetOperandCount() == 3));
            var_types simdBaseType = hwNode->GetSimdBaseType();

            if (varTypeIsFloating(simdBaseType))
            {
                // floating-point comparisons can always be optimized
                return true;
            }

            if (intrinsic == NI_AVX512_CompareEqualMask)
            {
                // equals comparisons can always be optimized
                return true;
            }

            if (varTypeIsUnsigned(simdBaseType))
            {
                // unsigned integer relational comparisons cannot be optimized
                return false;
            }

            if (intrinsic == NI_AVX512_CompareGreaterThanMask)
            {
                // signed integer greater-than comparisons can always be optimized
                return true;
            }

            if (intrinsic == NI_AVX512_CompareLessThanMask)
            {
                // signed integer less-than comparisons can always be optimized
                return true;
            }
            break;
        }

        case NI_AVX512_ConvertVectorToMask:
        {
            return true;
        }

        default:
        {
            break;
        }
    }

    // Other cases cannot be optimized
    return false;
}
#endif // TARGET_XARCH

//----------------------------------------------------------------------------------------------
// RewriteHWIntrinsicExtractMsb: Rewrites a hwintrinsic ExtractMostSignificantBytes operation
//
// Arguments:
//    use     - A pointer to the hwintrinsic node
//    parents - A reference to tree walk data providing the context
//
void Rationalizer::RewriteHWIntrinsicExtractMsb(GenTree** use, Compiler::GenTreeStack& parents)
{
    GenTreeHWIntrinsic* node = (*use)->AsHWIntrinsic();

    NamedIntrinsic intrinsic       = node->GetHWIntrinsicId();
    CorInfoType    simdBaseJitType = node->GetSimdBaseJitType();
    var_types      simdBaseType    = node->GetSimdBaseType();
    unsigned       simdSize        = node->GetSimdSize();
    var_types      simdType        = Compiler::getSIMDTypeForSize(simdSize);

    GenTree* op1 = node->Op(1);

#if defined(TARGET_ARM64)
    // ARM64 doesn't have a single instruction that performs the behavior so we'll emulate it instead.
    // To do this, we effectively perform the following steps:
    // 1. tmp = input & 0x80         ; and the input to clear all but the most significant bit
    // 2. tmp = tmp >> index         ; right shift each element by its index
    // 3. tmp = sum(tmp)             ; sum the elements together

    GenTreeVecCon* vecCon2 = comp->gtNewVconNode(simdType);
    GenTreeVecCon* vecCon3 = comp->gtNewVconNode(simdType);

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        {
            simdBaseType    = TYP_UBYTE;
            simdBaseJitType = CORINFO_TYPE_UBYTE;

            vecCon2->gtSimdVal.u64[0] = 0x8080808080808080;
            vecCon3->gtSimdVal.u64[0] = 0x00FFFEFDFCFBFAF9;

            if (simdSize == 16)
            {
                vecCon2->gtSimdVal.u64[1] = 0x8080808080808080;
                vecCon3->gtSimdVal.u64[1] = 0x00FFFEFDFCFBFAF9;
            }
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            simdBaseType    = TYP_USHORT;
            simdBaseJitType = CORINFO_TYPE_USHORT;

            vecCon2->gtSimdVal.u64[0] = 0x8000800080008000;
            vecCon3->gtSimdVal.u64[0] = 0xFFF4FFF3FFF2FFF1;

            if (simdSize == 16)
            {
                vecCon2->gtSimdVal.u64[1] = 0x8000800080008000;
                vecCon3->gtSimdVal.u64[1] = 0xFFF8FFF7FFF6FFF5;
            }
            break;
        }

        case TYP_INT:
        case TYP_UINT:
        case TYP_FLOAT:
        {
            simdBaseType    = TYP_INT;
            simdBaseJitType = CORINFO_TYPE_INT;

            vecCon2->gtSimdVal.u64[0] = 0x8000000080000000;
            vecCon3->gtSimdVal.u64[0] = 0xFFFFFFE2FFFFFFE1;

            if (simdSize == 16)
            {
                vecCon2->gtSimdVal.u64[1] = 0x8000000080000000;
                vecCon3->gtSimdVal.u64[1] = 0xFFFFFFE4FFFFFFE3;
            }
            break;
        }

        case TYP_LONG:
        case TYP_ULONG:
        case TYP_DOUBLE:
        {
            simdBaseType    = TYP_LONG;
            simdBaseJitType = CORINFO_TYPE_LONG;

            vecCon2->gtSimdVal.u64[0] = 0x8000000000000000;
            vecCon3->gtSimdVal.u64[0] = 0xFFFFFFFFFFFFFFC1;

            if (simdSize == 16)
            {
                vecCon2->gtSimdVal.u64[1] = 0x8000000000000000;
                vecCon3->gtSimdVal.u64[1] = 0xFFFFFFFFFFFFFFC2;
            }
            break;
        }

        default:
        {
            unreached();
        }
    }

    BlockRange().InsertAfter(op1, vecCon2);
    GenTree* tmp = comp->gtNewSimdBinOpNode(GT_AND, simdType, op1, vecCon2, simdBaseJitType, simdSize);
    BlockRange().InsertAfter(vecCon2, tmp);
    op1 = tmp;

    if ((simdSize == 8) && varTypeIsLong(simdBaseType))
    {
        intrinsic = NI_AdvSimd_ShiftLogicalScalar;
    }
    else
    {
        intrinsic = NI_AdvSimd_ShiftLogical;
    }

    BlockRange().InsertAfter(op1, vecCon3);
    tmp = comp->gtNewSimdHWIntrinsicNode(simdType, op1, vecCon3, intrinsic, simdBaseJitType, simdSize);
    BlockRange().InsertAfter(vecCon3, tmp);
    op1 = tmp;

    if (varTypeIsByte(simdBaseType) && (simdSize == 16))
    {
        // For byte/sbyte, we also need to handle the fact that we can only shift by up to 8
        // but for Vector128, we have 16 elements to handle. In that scenario, we will widen
        // to ushort and combine the lower/upper halves.

        LIR::Use op1Use;
        LIR::Use::MakeDummyUse(BlockRange(), op1, &op1Use);

        op1Use.ReplaceWithLclVar(comp);
        op1 = op1Use.Def();

        GenTree* op2 = comp->gtClone(op1);
        BlockRange().InsertAfter(op1, op2);

        tmp = comp->gtNewSimdHWIntrinsicNode(simdType, op1, NI_AdvSimd_ZeroExtendWideningUpper, simdBaseJitType, 16);
        BlockRange().InsertBefore(op2, tmp);
        op1 = tmp;

        GenTree* icon = comp->gtNewIconNode(8);
        BlockRange().InsertBefore(op2, icon);

        tmp = comp->gtNewSimdBinOpNode(GT_LSH, simdType, op1, icon, CORINFO_TYPE_USHORT, simdSize);
        BlockRange().InsertBefore(op2, tmp);
        op1 = tmp;

        tmp = comp->gtNewSimdGetLowerNode(TYP_SIMD8, op2, simdBaseJitType, 16);
        BlockRange().InsertAfter(op2, tmp);
        op2 = tmp;

        tmp = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, NI_AdvSimd_AddWideningLower, simdBaseJitType, 8);
        BlockRange().InsertAfter(op2, tmp);
        op1 = tmp;

        simdBaseType    = TYP_USHORT;
        simdBaseJitType = CORINFO_TYPE_USHORT;
    }

    // Sum the elements

    if (!varTypeIsLong(simdBaseType))
    {
        if ((simdSize == 8) && ((simdBaseType == TYP_INT) || (simdBaseType == TYP_UINT)))
        {
            LIR::Use op1Use;
            LIR::Use::MakeDummyUse(BlockRange(), op1, &op1Use);

            op1Use.ReplaceWithLclVar(comp);
            op1 = op1Use.Def();

            GenTree* op2 = comp->gtClone(op1);
            BlockRange().InsertAfter(op1, op2);

            tmp =
                comp->gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, op2, NI_AdvSimd_AddPairwise, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op2, tmp);
            op1 = tmp;
        }
        else
        {
            tmp = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType, simdSize);
            BlockRange().InsertAfter(op1, tmp);
            op1 = tmp;
        }
    }
    else if (simdSize == 16)
    {
        tmp = comp->gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_AddPairwiseScalar, simdBaseJitType,
                                             simdSize);
        BlockRange().InsertAfter(op1, tmp);
        op1 = tmp;
    }

    if (simdSize == 8)
    {
        intrinsic = NI_Vector64_ToScalar;
    }
    else
    {
        intrinsic = NI_Vector128_ToScalar;
    }

    node->gtType = genActualType(simdBaseType);
    node->ChangeHWIntrinsicId(intrinsic);
    node->SetSimdSize(8);
    node->SetSimdBaseJitType(simdBaseJitType);
    node->Op(1) = op1;

    if ((simdBaseType != TYP_INT) && (simdBaseType != TYP_UINT))
    {
        GenTree* castNode = comp->gtNewCastNode(TYP_INT, node, /* isUnsigned */ true, TYP_INT);
        BlockRange().InsertAfter(node, castNode);

        if (parents.Height() > 1)
        {
            parents.Top(1)->ReplaceOperand(use, castNode);
        }
        else
        {
            *use = castNode;
        }

        // Adjust the parent stack
        assert(parents.Top() == node);
        (void)parents.Pop();
        parents.Push(castNode);
    }
#elif defined(TARGET_XARCH)
    simdBaseJitType = varTypeIsUnsigned(simdBaseType) ? CORINFO_TYPE_UBYTE : CORINFO_TYPE_BYTE;

    // We want to tightly pack the most significant byte of each short/ushort
    // and then zero the tightly packed least significant bytes
    //
    // The most significant bit being set means zero the value

    simd_t simdVal = {};

    simdVal.u64[0] = 0x0F0D0B0907050301;
    simdVal.u64[1] = 0x8080808080808080;

    NamedIntrinsic shuffleIntrinsic = NI_Illegal;

    if (simdSize == 32)
    {
        // Vector256 works on 2x128-bit lanes, so repeat the same indices for the upper lane

        simdVal.u64[2] = 0x0F0D0B0907050301;
        simdVal.u64[3] = 0x8080808080808080;

        shuffleIntrinsic = NI_AVX2_Shuffle;
    }
    else
    {
        shuffleIntrinsic = NI_X86Base_Shuffle;
    }

    GenTree* op2 = comp->gtNewVconNode(simdType);
    memcpy(&op2->AsVecCon()->gtSimdVal, &simdVal, simdSize);
    BlockRange().InsertAfter(op1, op2);

    GenTree* tmp = comp->gtNewSimdHWIntrinsicNode(simdType, op1, op2, shuffleIntrinsic, simdBaseJitType, simdSize);
    BlockRange().InsertAfter(op2, tmp);
    op1 = tmp;

    if (simdSize == 32)
    {
        CorInfoType simdOtherJitType;

        // Since Vector256 is 2x128-bit lanes we need a full width permutation so we get the lower
        // 64-bits of each lane next to eachother. The upper bits should be zero, but also don't
        // matter so we can also then simplify down to a 128-bit move mask.

        simdOtherJitType = (simdBaseType == TYP_UBYTE) ? CORINFO_TYPE_ULONG : CORINFO_TYPE_LONG;

        GenTree* icon = comp->gtNewIconNode(0xD8);
        BlockRange().InsertAfter(op1, icon);

        tmp = comp->gtNewSimdHWIntrinsicNode(simdType, op1, icon, NI_AVX2_Permute4x64, simdOtherJitType, simdSize);
        BlockRange().InsertAfter(icon, tmp);
        op1 = tmp;

        simdType = TYP_SIMD16;

        tmp = comp->gtNewSimdGetLowerNode(simdType, op1, simdBaseJitType, simdSize);
        BlockRange().InsertAfter(op1, tmp);
        op1 = tmp;

        simdSize = 16;
    }

    node->ChangeHWIntrinsicId(NI_X86Base_MoveMask);
    node->SetSimdSize(simdSize);
    node->SetSimdBaseJitType(simdBaseJitType);
    node->Op(1) = op1;
#else
    unreached();
#endif
}
#endif // FEATURE_HW_INTRINSICS

#ifdef TARGET_ARM64
// RewriteSubLshDiv: Possibly rewrite a SubLshDiv node into a Mod.
//
// Arguments:
//    use - A use of a node.
//
// Transform: a - (a / cns) << shift  =>  a % cns
//            where cns is a signed integer constant that is a power of 2.
// We do this transformation because Lowering has a specific optimization
// for 'a % cns' that is not easily reduced by other means.
//
void Rationalizer::RewriteSubLshDiv(GenTree** use)
{
    if (!comp->opts.OptimizationEnabled())
        return;

    GenTree* const node = *use;

    if (!node->OperIs(GT_SUB))
        return;

    GenTree* op1 = node->gtGetOp1();
    GenTree* op2 = node->gtGetOp2();

    if (!(node->TypeIs(TYP_INT, TYP_LONG) && op1->OperIs(GT_LCL_VAR)))
        return;

    if (!op2->OperIs(GT_LSH))
        return;

    GenTree* lsh   = op2;
    GenTree* div   = lsh->gtGetOp1();
    GenTree* shift = lsh->gtGetOp2();
    if (div->OperIs(GT_DIV) && shift->IsIntegralConst())
    {
        GenTree* a   = div->gtGetOp1();
        GenTree* cns = div->gtGetOp2();
        if (a->OperIs(GT_LCL_VAR) && cns->IsIntegralConstPow2() &&
            op1->AsLclVar()->GetLclNum() == a->AsLclVar()->GetLclNum())
        {
            size_t shiftValue = shift->AsIntConCommon()->IntegralValue();
            size_t cnsValue   = cns->AsIntConCommon()->IntegralValue();
            if ((cnsValue >> shiftValue) == 1)
            {
                node->ChangeOper(GT_MOD);
                node->AsOp()->gtOp2 = cns;
                BlockRange().Remove(lsh);
                BlockRange().Remove(div);
                BlockRange().Remove(a);
                BlockRange().Remove(shift);
            }
        }
    }
}
#endif

#ifdef DEBUG

void Rationalizer::ValidateStatement(Statement* stmt, BasicBlock* block)
{
    DBEXEC(TRUE, JitTls::GetCompiler()->fgDebugCheckNodeLinks(block, stmt));
}

// sanity checks that apply to all kinds of IR
void Rationalizer::SanityCheck()
{
    // TODO: assert(!IsLIR());
    for (BasicBlock* const block : comp->Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            ValidateStatement(stmt, block);
        }
    }
}

void Rationalizer::SanityCheckRational()
{
    // TODO-Cleanup : check that the tree is rational here
    // then do normal checks
    SanityCheck();
}

#endif // DEBUG

Compiler::fgWalkResult Rationalizer::RewriteNode(GenTree** useEdge, Compiler::GenTreeStack& parentStack)
{
    assert(useEdge != nullptr);

    GenTree* node = *useEdge;
    assert(node != nullptr);

    // Clear the REVERSE_OPS flag on the current node.
    node->gtFlags &= ~GTF_REVERSE_OPS;

    LIR::Use use;
    if (parentStack.Height() < 2)
    {
        LIR::Use::MakeDummyUse(BlockRange(), *useEdge, &use);
    }
    else
    {
        use = LIR::Use(BlockRange(), useEdge, parentStack.Top(1));
    }

    assert(node == use.Def());
    switch (node->OperGet())
    {
        case GT_CALL:
            // In linear order we no longer need to retain the stores in early
            // args as these have now been sequenced.
            for (CallArg& arg : node->AsCall()->gtArgs.EarlyArgs())
            {
                if (arg.GetLateNode() != nullptr)
                {
                    if (arg.GetEarlyNode()->IsValue())
                    {
                        arg.GetEarlyNode()->SetUnusedValue();
                    }
                    arg.SetEarlyNode(nullptr);
                }
            }

#ifdef DEBUG
            // The above means that all argument nodes are now true arguments.
            for (CallArg& arg : node->AsCall()->gtArgs.Args())
            {
                assert((arg.GetEarlyNode() == nullptr) != (arg.GetLateNode() == nullptr));
            }
#endif
            break;

        case GT_BOX:
        case GT_ARR_ADDR:
            // BOX/ARR_ADDR are "passthrough" nodes,
            // and at this point we no longer need them.
            if (node->gtGetOp1() != nullptr)
            {
                use.ReplaceWith(node->gtGetOp1());
                BlockRange().Remove(node);
                node = node->gtGetOp1();
            }
            break;

        case GT_GCPOLL:
        {
            // GCPOLL is essentially a no-op, we used it as a hint for fgCreateGCPoll
            node->gtBashToNOP();
            return Compiler::WALK_CONTINUE;
        }

        case GT_COMMA:
        {
            GenTree*           op1         = node->gtGetOp1();
            bool               isClosed    = false;
            unsigned           sideEffects = 0;
            LIR::ReadOnlyRange lhsRange    = BlockRange().GetTreeRange(op1, &isClosed, &sideEffects);

            if ((sideEffects & GTF_ALL_EFFECT) == 0)
            {
                // The LHS has no side effects. Remove it.
                // All transformations on pure trees keep their operands in LIR
                // and should not violate tree order.
                assert(isClosed);

                BlockRange().Delete(comp, m_block, std::move(lhsRange));
            }
            else if (op1->IsValue())
            {
                op1->SetUnusedValue();
            }

            BlockRange().Remove(node);

            GenTree* replacement = node->gtGetOp2();
            if (!use.IsDummyUse())
            {
                use.ReplaceWith(replacement);
                node = replacement;
            }
            else
            {
                // This is a top-level comma. If the RHS has no side effects we can remove
                // it as well.
                bool               isClosed    = false;
                unsigned           sideEffects = 0;
                LIR::ReadOnlyRange rhsRange    = BlockRange().GetTreeRange(replacement, &isClosed, &sideEffects);

                if ((sideEffects & GTF_ALL_EFFECT) == 0)
                {
                    // All transformations on pure trees keep their operands in
                    // LIR and should not violate tree order.
                    assert(isClosed);

                    BlockRange().Delete(comp, m_block, std::move(rhsRange));
                }
                else
                {
                    node = replacement;
                }
            }
        }
        break;

        case GT_INTRINSIC:
            // Non-target intrinsics should have already been rewritten back into user calls.
            assert(comp->IsTargetIntrinsic(node->AsIntrinsic()->gtIntrinsicName));
            break;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            RewriteHWIntrinsic(useEdge, parentStack);
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_CAST:
            if (node->AsCast()->CastOp()->OperIsSimple())
            {
                comp->fgSimpleLowerCastOfSmpOp(BlockRange(), node->AsCast());
            }
            break;

        case GT_BSWAP16:
            if (node->gtGetOp1()->OperIs(GT_CAST))
            {
                comp->fgSimpleLowerBswap16(BlockRange(), node);
            }
            break;

        default:
            // Check that we don't have nodes not allowed in HIR here.
            assert((node->DebugOperKind() & DBK_NOTHIR) == 0);
            break;
    }

    // Do some extra processing on top-level nodes to remove unused local reads.
    if (node->OperIsLocalRead())
    {
        if (use.IsDummyUse())
        {
            BlockRange().Remove(node);
        }
        else
        {
            // Local reads are side-effect-free; clear any flags leftover from frontend transformations.
            node->gtFlags &= ~GTF_ALL_EFFECT;
        }
    }
    else
    {
        if (node->IsValue() && use.IsDummyUse())
        {
            node->SetUnusedValue();
        }

        if (node->TypeIs(TYP_LONG))
        {
            comp->compLongUsed = true;
        }
    }

    return Compiler::WALK_CONTINUE;
}

// Rewrite intrinsics that are not supported by the target back into user calls.
// This needs to be done before the transition to LIR because it relies on the use
// of fgMorphArgs, which is designed to operate on HIR. Once this is done for a
// particular statement, link that statement's nodes into the current basic block.
Compiler::fgWalkResult Rationalizer::RationalizeVisitor::PreOrderVisit(GenTree** use, GenTree* user)
{
    GenTree* const node = *use;

    if (node->OperIs(GT_INTRINSIC))
    {
        if (m_rationalizer.comp->IsIntrinsicImplementedByUserCall(node->AsIntrinsic()->gtIntrinsicName))
        {
            m_rationalizer.RewriteIntrinsicAsUserCall(use, this->m_ancestors);
        }
    }
#if defined(FEATURE_HW_INTRINSICS)
    else if (node->OperIsHWIntrinsic())
    {
        if (node->AsHWIntrinsic()->IsUserCall())
        {
            m_rationalizer.RewriteHWIntrinsicAsUserCall(use, this->m_ancestors);
        }
    }
#endif // FEATURE_HW_INTRINSICS

#ifdef TARGET_ARM64
    if (node->OperIs(GT_SUB))
    {
        m_rationalizer.RewriteSubLshDiv(use);
    }
#endif

    return Compiler::WALK_CONTINUE;
}

// Rewrite HIR nodes into LIR nodes.
Compiler::fgWalkResult Rationalizer::RationalizeVisitor::PostOrderVisit(GenTree** use, GenTree* user)
{
    return m_rationalizer.RewriteNode(use, this->m_ancestors);
}

//------------------------------------------------------------------------
// DoPhase: Run the rationalize over the method IR.
//
// Returns:
//    PhaseStatus indicating, what, if anything, was modified
//
PhaseStatus Rationalizer::DoPhase()
{
    DBEXEC(TRUE, SanityCheck());

    comp->compCurBB = nullptr;
    comp->fgOrder   = Compiler::FGOrderLinear;

    RationalizeVisitor visitor(*this);
    for (BasicBlock* const block : comp->Blocks())
    {
        comp->compCurBB = block;
        m_block         = block;

        block->MakeLIR(nullptr, nullptr);

        // Establish the first and last nodes for the block. This is necessary in order for the LIR
        // utilities that hang off the BasicBlock type to work correctly.
        Statement* firstStatement = block->firstStmt();
        if (firstStatement == nullptr)
        {
            // No statements in this block; skip it.
            continue;
        }

        for (Statement* const statement : block->Statements())
        {
            assert(statement->GetTreeList() != nullptr);
            assert(statement->GetTreeList()->gtPrev == nullptr);
            assert(statement->GetRootNode() != nullptr);
            assert(statement->GetRootNode()->gtNext == nullptr);

            if (!statement->IsPhiDefnStmt()) // Note that we get rid of PHI nodes here.
            {
                BlockRange().InsertAtEnd(LIR::Range(statement->GetTreeList(), statement->GetRootNode()));

                // If this statement has correct debug information, change it
                // into a debug info node and insert it into the LIR. Note that
                // we are currently reporting root info only back to the EE, so
                // if the leaf debug info is invalid we still attach it.
                // Note that we would like to have the invariant di.IsValid()
                // => parent.IsValid() but it is currently not the case for
                // NEWOBJ IL instructions where the debug info ends up attached
                // to the allocation instead of the constructor call.
                DebugInfo di = statement->GetDebugInfo();
                if (di.IsValid() || di.GetRoot().IsValid())
                {
                    GenTreeILOffset* ilOffset =
                        new (comp, GT_IL_OFFSET) GenTreeILOffset(di DEBUGARG(statement->GetLastILOffset()));
                    BlockRange().InsertBefore(statement->GetTreeList(), ilOffset);
                }

                m_block = block;
                visitor.WalkTree(statement->GetRootNodePointer(), nullptr);
            }
        }

        block->bbStmtList = nullptr;

        assert(BlockRange().CheckLIR(comp, true));
    }

    comp->compRationalIRForm = true;

    return PhaseStatus::MODIFIED_EVERYTHING;
}
