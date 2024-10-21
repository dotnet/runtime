// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// RewriteNodeAsCall : Replace the given tree node by a GT_CALL.
//
// Arguments:
//    use          - A pointer-to-a-pointer for the tree node
//    sig          - The signature info for callHnd
//    parents      - A pointer to tree walk data providing the context
//    callHnd      - The method handle of the call to be generated
//    entryPoint   - The method entrypoint of the call to be generated
//    operands     - The operand  list of the call to be generated
//    operandCount - The number of operands in the operand list
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
                                     size_t    operandCount)
{
    GenTree* const tree           = *use;
    GenTree* const treeFirstNode  = comp->fgGetFirstNode(tree);
    GenTree* const insertionPoint = treeFirstNode->gtPrev;

    BlockRange().Remove(treeFirstNode, tree);

    // Create the call node
    GenTreeCall* call = comp->gtNewCallNode(CT_USER_FUNC, callHnd, tree->TypeGet());

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
            arg = NewCallArg::Struct(operand, sigTyp, clsHnd);
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

        GenTree*   destAddr = comp->gtNewLclVarAddrNode(tmpNum, TYP_BYREF);
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
//    ppTree      - A pointer-to-a-pointer for the intrinsic node
//    fgWalkData  - A pointer to tree walk data providing the context
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

    RewriteNodeAsCall(use, &sigInfo, parents, callHnd,
#if defined(FEATURE_READYTORUN)
                      intrinsic->gtEntryPoint,
#endif // FEATURE_READYTORUN
                      operands, operandCount);
}

#if defined(FEATURE_HW_INTRINSICS)
// RewriteHWIntrinsicAsUserCall : Rewrite a hwintrinsic node as a GT_CALL to the original method.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the intrinsic node
//    fgWalkData  - A pointer to tree walk data providing the context
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
        case NI_Vector128_Shuffle:
#if defined(TARGET_XARCH)
        case NI_Vector256_Shuffle:
        case NI_Vector512_Shuffle:
#elif defined(TARGET_ARM64)
        case NI_Vector64_Shuffle:
#endif
        {
            assert(operandCount == 2);
#if defined(TARGET_XARCH)
            assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));
#else
            assert((simdSize == 8) || (simdSize == 16));
#endif

            GenTree* op1 = operands[0];
            GenTree* op2 = operands[1];

            if (op2->IsCnsVec() && comp->IsValidForShuffle(op2->AsVecCon(), simdSize, simdBaseType))
            {
                result = comp->gtNewSimdShuffleNode(retType, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case NI_Vector128_WithElement:
#if defined(TARGET_XARCH)
        case NI_Vector256_WithElement:
        case NI_Vector512_WithElement:
#elif defined(TARGET_ARM64)
        case NI_Vector64_WithElement:
#endif
        {
            assert(operandCount == 3);

            GenTree* op1 = operands[0];
            GenTree* op2 = operands[1];
            GenTree* op3 = operands[2];

            if (op2->OperIsConst())
            {
                ssize_t imm8  = op2->AsIntCon()->IconValue();
                ssize_t count = simdSize / genTypeSize(simdBaseType);

                if ((imm8 >= count) || (imm8 < 0))
                {
                    // Using software fallback if index is out of range (throw exception)
                    break;
                }

#if defined(TARGET_XARCH)
                if (varTypeIsIntegral(simdBaseType))
                {
                    if (varTypeIsLong(simdBaseType))
                    {
                        if (!comp->compOpportunisticallyDependsOn(InstructionSet_SSE41_X64))
                        {
                            break;
                        }
                    }
                    else if (!varTypeIsShort(simdBaseType))
                    {
                        if (!comp->compOpportunisticallyDependsOn(InstructionSet_SSE41))
                        {
                            break;
                        }
                    }
                }
#endif // TARGET_XARCH

                result = comp->gtNewSimdWithElementNode(retType, op1, op2, op3, simdBaseJitType, simdSize);
                break;
            }
            break;
        }

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

            CORINFO_CLASS_HANDLE op1ClsHnd = NO_CLASS_HANDLE;
            CORINFO_CLASS_HANDLE op2ClsHnd = NO_CLASS_HANDLE;
            CORINFO_CLASS_HANDLE op3ClsHnd = NO_CLASS_HANDLE;

            size_t argCount = operandCount - (sigInfo.hasThis() ? 1 : 0);

            if (argCount > 0)
            {
                CORINFO_ARG_LIST_HANDLE args = sigInfo.args;
                comp->info.compCompHnd->getArgType(&sigInfo, args, &op1ClsHnd);

                if (argCount > 1)
                {
                    args = comp->info.compCompHnd->getArgNext(args);
                    comp->info.compCompHnd->getArgType(&sigInfo, args, &op2ClsHnd);

                    if (argCount > 2)
                    {
                        args = comp->info.compCompHnd->getArgNext(args);
                        comp->info.compCompHnd->getArgType(&sigInfo, args, &op3ClsHnd);
                    }
                }
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
                comp->getHWIntrinsicImmTypes(intrinsicId, &sigInfo, 2, simdBaseType, simdBaseJitType, op1ClsHnd,
                                             op2ClsHnd, op3ClsHnd, &immSimdSize, &immSimdBaseType);
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

            comp->getHWIntrinsicImmTypes(intrinsicId, &sigInfo, 1, simdBaseType, simdBaseJitType, op1ClsHnd, op2ClsHnd,
                                         op3ClsHnd, &immSimdSize, &immSimdBaseType);
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

    RewriteNodeAsCall(use, &sigInfo, parents, callHnd,
#if defined(FEATURE_READYTORUN)
                      hwintrinsic->GetEntryPoint(),
#endif // FEATURE_READYTORUN
                      operands, operandCount);
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
            // Intrinsics should have already been rewritten back into user calls.
            assert(!node->AsHWIntrinsic()->IsUserCall());
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_CAST:
            if (node->AsCast()->CastOp()->OperIsSimple())
            {
                comp->fgSimpleLowerCastOfSmpOp(BlockRange(), node->AsCast());
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

    if (node->OperGet() == GT_INTRINSIC)
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
