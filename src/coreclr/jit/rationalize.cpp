// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// return op that is the store equivalent of the given load opcode
genTreeOps storeForm(genTreeOps loadForm)
{
    switch (loadForm)
    {
        case GT_LCL_VAR:
            return GT_STORE_LCL_VAR;
        case GT_LCL_FLD:
            return GT_STORE_LCL_FLD;
        default:
            noway_assert(!"not a data load opcode\n");
            unreached();
    }
}

// return op that is the addr equivalent of the given load opcode
genTreeOps addrForm(genTreeOps loadForm)
{
    switch (loadForm)
    {
        case GT_LCL_VAR:
            return GT_LCL_VAR_ADDR;
        case GT_LCL_FLD:
            return GT_LCL_FLD_ADDR;
        default:
            noway_assert(!"not a data load opcode\n");
            unreached();
    }
}

// copy the flags determined by mask from src to dst
void copyFlags(GenTree* dst, GenTree* src, GenTreeFlags mask)
{
    dst->gtFlags &= ~mask;
    dst->gtFlags |= (src->gtFlags & mask);
}

// RewriteIndir: Rewrite an indirection and clear the flags that should not be set after rationalize.
//
// Arguments:
//    use - A use of an indirection node.
//
void Rationalizer::RewriteIndir(LIR::Use& use)
{
    GenTreeIndir* indir = use.Def()->AsIndir();
    assert(indir->OperIs(GT_IND, GT_BLK, GT_OBJ));

    // Clear the `GTF_IND_ASG_LHS` flag, which overlaps with `GTF_IND_REQ_ADDR_IN_REG`.
    indir->gtFlags &= ~GTF_IND_ASG_LHS;

    if (indir->OperIs(GT_IND))
    {
        if (varTypeIsSIMD(indir))
        {
            RewriteSIMDIndir(use);
        }
        else
        {
            // Due to promotion of structs containing fields of type struct with a
            // single scalar type field, we could potentially see IR nodes of the
            // form GT_IND(GT_ADD(lclvarAddr, 0)) where 0 is an offset representing
            // a field-seq. These get folded here.
            //
            // TODO: This code can be removed once JIT implements recursive struct
            // promotion instead of lying about the type of struct field as the type
            // of its single scalar field.
            GenTree* addr = indir->Addr();
            if (addr->OperGet() == GT_ADD && addr->gtGetOp1()->OperGet() == GT_LCL_VAR_ADDR &&
                addr->gtGetOp2()->IsIntegralConst(0))
            {
                GenTreeLclVarCommon* lclVarNode = addr->gtGetOp1()->AsLclVarCommon();
                const LclVarDsc*     varDsc     = comp->lvaGetDesc(lclVarNode);
                if (indir->TypeGet() == varDsc->TypeGet())
                {
                    JITDUMP("Rewriting GT_IND(GT_ADD(LCL_VAR_ADDR,0)) to LCL_VAR\n");
                    lclVarNode->SetOper(GT_LCL_VAR);
                    lclVarNode->gtType = indir->TypeGet();
                    use.ReplaceWith(lclVarNode);
                    BlockRange().Remove(addr);
                    BlockRange().Remove(addr->gtGetOp2());
                    BlockRange().Remove(indir);
                }
            }
        }
    }
    else if (indir->OperIs(GT_OBJ))
    {
        assert((indir->TypeGet() == TYP_STRUCT) || !use.User()->OperIsInitBlkOp());
        if (varTypeIsSIMD(indir->TypeGet()))
        {
            indir->SetOper(GT_IND);
            RewriteSIMDIndir(use);
        }
    }
    else
    {
        assert(indir->OperIs(GT_BLK));
        // We should only see GT_BLK for TYP_STRUCT or for InitBlocks.
        assert((indir->TypeGet() == TYP_STRUCT) || use.User()->OperIsInitBlkOp());
    }
}

// RewriteSIMDIndir: Rewrite a SIMD indirection as a simple lclVar if possible.
//
// Arguments:
//    use - A use of a GT_IND node of SIMD type
//
// TODO-1stClassStructs: These should be eliminated earlier, once we can handle
// lclVars in all the places that used to have GT_OBJ.
//
void Rationalizer::RewriteSIMDIndir(LIR::Use& use)
{
#ifdef FEATURE_SIMD
    GenTreeIndir* indir = use.Def()->AsIndir();
    assert(indir->OperIs(GT_IND));
    var_types simdType = indir->TypeGet();
    assert(varTypeIsSIMD(simdType));

    GenTree* addr = indir->Addr();

    if (addr->OperIs(GT_LCL_VAR_ADDR) && comp->lvaGetDesc(addr->AsLclVar())->lvSIMDType)
    {
        // If we have GT_IND(GT_LCL_VAR_ADDR) and the var is a SIMD type,
        // replace the expression by GT_LCL_VAR or GT_LCL_FLD.
        BlockRange().Remove(indir);

        const GenTreeLclVar* lclAddr = addr->AsLclVar();
        const unsigned       lclNum  = lclAddr->GetLclNum();
        LclVarDsc*           varDsc  = comp->lvaGetDesc(lclNum);

        var_types lclType = varDsc->TypeGet();

        if (lclType == simdType)
        {
            addr->SetOper(GT_LCL_VAR);
        }
        else
        {
            addr->SetOper(GT_LCL_FLD);
            addr->AsLclFld()->SetLclOffs(0);
            addr->AsLclFld()->SetFieldSeq(FieldSeqStore::NotAField());

            if (((addr->gtFlags & GTF_VAR_DEF) != 0) && (genTypeSize(simdType) < genTypeSize(lclType)))
            {
                addr->gtFlags |= GTF_VAR_USEASG;
            }

            comp->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::LocalField));
        }
        if (varDsc->lvPromotedStruct())
        {
            comp->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
        }

        addr->gtType = simdType;
        use.ReplaceWith(addr);
    }
    else if (addr->OperIs(GT_ADDR) && addr->AsUnOp()->gtGetOp1()->OperIsSimdOrHWintrinsic())
    {
        // If we have IND(ADDR(SIMD)) then we can keep only the SIMD node.
        // This is a special tree created by impNormStructVal to preserve the class layout
        // needed by call morphing on an OBJ node. This information is no longer needed at
        // this point (and the address of a SIMD node can't be obtained anyway).

        BlockRange().Remove(indir);
        BlockRange().Remove(addr);

        use.ReplaceWith(addr->AsUnOp()->gtGetOp1());
    }
#endif // FEATURE_SIMD
}

// RewriteNodeAsCall : Replace the given tree node by a GT_CALL.
//
// Arguments:
//    ppTree      - A pointer-to-a-pointer for the tree node
//    fgWalkData  - A pointer to tree walk data providing the context
//    callHnd     - The method handle of the call to be generated
//    entryPoint  - The method entrypoint of the call to be generated
//    args        - The argument list of the call to be generated
//
// Return Value:
//    None.
//

void Rationalizer::RewriteNodeAsCall(GenTree**             use,
                                     ArrayStack<GenTree*>& parents,
                                     CORINFO_METHOD_HANDLE callHnd,
#ifdef FEATURE_READYTORUN
                                     CORINFO_CONST_LOOKUP entryPoint,
#endif
                                     GenTreeCall::Use* args)
{
    GenTree* const tree           = *use;
    GenTree* const treeFirstNode  = comp->fgGetFirstNode(tree);
    GenTree* const insertionPoint = treeFirstNode->gtPrev;

    BlockRange().Remove(treeFirstNode, tree);

    // Create the call node
    GenTreeCall* call = comp->gtNewCallNode(CT_USER_FUNC, callHnd, tree->gtType, args);

#if DEBUG
    CORINFO_SIG_INFO sig;
    comp->eeGetMethodSig(callHnd, &sig);
    assert(JITtype2varType(sig.retType) == tree->gtType);
#endif // DEBUG

#ifdef FEATURE_READYTORUN
    call->AsCall()->setEntryPoint(entryPoint);
#endif

    call = comp->fgMorphArgs(call);

    // Replace "tree" with "call"
    if (parents.Height() > 1)
    {
        parents.Top(1)->ReplaceOperand(use, call);
    }
    else
    {
        // If there's no parent, the tree being replaced is the root of the
        // statement (and no special handling is necessary).
        *use = call;
    }

    comp->gtSetEvalOrder(call);
    BlockRange().InsertAfter(insertionPoint, LIR::Range(comp->fgSetTreeSeq(call), call));

    // Propagate flags of "call" to its parents.
    // 0 is current node, so start at 1
    for (int i = 1; i < parents.Height(); i++)
    {
        parents.Top(i)->gtFlags |= (call->gtFlags & GTF_ALL_EFFECT) | GTF_CALL;
    }

    // Since "tree" is replaced with "call", pop "tree" node (i.e the current node)
    // and replace it with "call" on parent stack.
    assert(parents.Top() == tree);
    (void)parents.Pop();
    parents.Push(call);
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

    GenTreeCall::Use* args;
    if (intrinsic->AsOp()->gtOp2 == nullptr)
    {
        args = comp->gtNewCallArgs(intrinsic->gtGetOp1());
    }
    else
    {
        args = comp->gtNewCallArgs(intrinsic->gtGetOp1(), intrinsic->gtGetOp2());
    }

    RewriteNodeAsCall(use, parents, intrinsic->gtMethodHandle,
#ifdef FEATURE_READYTORUN
                      intrinsic->gtEntryPoint,
#endif
                      args);
}

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

            for (GenTree* const tree : stmt->TreeList())
            {
                // QMARK and PUT_ARG_TYPE nodes should have been removed before this phase.
                assert(!tree->OperIs(GT_QMARK, GT_PUTARG_TYPE));

                if (tree->OperGet() == GT_ASG)
                {
                    if (tree->gtGetOp1()->OperGet() == GT_LCL_VAR)
                    {
                        assert(tree->gtGetOp1()->gtFlags & GTF_VAR_DEF);
                    }
                    else if (tree->gtGetOp2()->OperGet() == GT_LCL_VAR)
                    {
                        assert(!(tree->gtGetOp2()->gtFlags & GTF_VAR_DEF));
                    }
                }
            }
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

static void RewriteAssignmentIntoStoreLclCore(GenTreeOp* assignment,
                                              GenTree*   location,
                                              GenTree*   value,
                                              genTreeOps locationOp)
{
    assert(assignment != nullptr);
    assert(assignment->OperGet() == GT_ASG);
    assert(location != nullptr);
    assert(value != nullptr);

    genTreeOps storeOp = storeForm(locationOp);

#ifdef DEBUG
    JITDUMP("rewriting asg(%s, X) to %s(X)\n", GenTree::OpName(locationOp), GenTree::OpName(storeOp));
#endif // DEBUG

    assignment->SetOper(storeOp);
    GenTreeLclVarCommon* store = assignment->AsLclVarCommon();

    GenTreeLclVarCommon* var = location->AsLclVarCommon();
    store->SetLclNum(var->GetLclNum());
    store->SetSsaNum(var->GetSsaNum());

    if (locationOp == GT_LCL_FLD)
    {
        store->AsLclFld()->SetLclOffs(var->AsLclFld()->GetLclOffs());
        store->AsLclFld()->SetFieldSeq(var->AsLclFld()->GetFieldSeq());
    }

    copyFlags(store, var, (GTF_LIVENESS_MASK | GTF_VAR_MULTIREG));
    store->gtFlags &= ~GTF_REVERSE_OPS;

    store->gtType = var->TypeGet();
    store->gtOp1  = value;

    DISPNODE(store);
    JITDUMP("\n");
}

void Rationalizer::RewriteAssignmentIntoStoreLcl(GenTreeOp* assignment)
{
    assert(assignment != nullptr);
    assert(assignment->OperGet() == GT_ASG);

    GenTree* location = assignment->gtGetOp1();
    GenTree* value    = assignment->gtGetOp2();

    RewriteAssignmentIntoStoreLclCore(assignment, location, value, location->OperGet());
}

void Rationalizer::RewriteAssignment(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTreeOp* assignment = use.Def()->AsOp();
    assert(assignment->OperGet() == GT_ASG);

    GenTree* location = assignment->gtGetOp1();
    GenTree* value    = assignment->gtGetOp2();

    genTreeOps locationOp = location->OperGet();

    if (assignment->OperIsBlkOp())
    {
#ifdef FEATURE_SIMD
        if (varTypeIsSIMD(location) && assignment->OperIsInitBlkOp())
        {
            if (location->OperIs(GT_LCL_VAR))
            {
                var_types   simdType        = location->TypeGet();
                GenTree*    initVal         = assignment->AsOp()->gtOp2;
                CorInfoType simdBaseJitType = comp->getBaseJitTypeOfSIMDLocal(location);
                if (simdBaseJitType == CORINFO_TYPE_UNDEF)
                {
                    // Lie about the type if we don't know/have it.
                    simdBaseJitType = CORINFO_TYPE_FLOAT;
                }
                GenTreeSIMD* simdTree =
                    comp->gtNewSIMDNode(simdType, initVal, SIMDIntrinsicInit, simdBaseJitType, genTypeSize(simdType));
                assignment->gtOp2 = simdTree;
                value             = simdTree;

                BlockRange().InsertAfter(initVal, simdTree);
            }
        }
#endif // FEATURE_SIMD
    }

    switch (locationOp)
    {
        case GT_LCL_VAR:
        case GT_LCL_FLD:
            RewriteAssignmentIntoStoreLclCore(assignment, location, value, locationOp);
            BlockRange().Remove(location);
            break;

        case GT_IND:
        {
            GenTreeStoreInd* store =
                new (comp, GT_STOREIND) GenTreeStoreInd(location->TypeGet(), location->gtGetOp1(), value);

            copyFlags(store, assignment, GTF_ALL_EFFECT);
            copyFlags(store, location, GTF_IND_FLAGS);

            // TODO: JIT dump

            // Remove the GT_IND node and replace the assignment node with the store
            BlockRange().Remove(location);
            BlockRange().InsertBefore(assignment, store);
            use.ReplaceWith(store);
            BlockRange().Remove(assignment);
        }
        break;

        case GT_CLS_VAR:
        {
            bool isVolatile = (location->gtFlags & GTF_CLS_VAR_VOLATILE) != 0;

            location->gtFlags &= ~GTF_CLS_VAR_VOLATILE;
            location->SetOper(GT_CLS_VAR_ADDR);
            location->gtType = TYP_BYREF;

            assignment->SetOper(GT_STOREIND);
            assignment->AsStoreInd()->SetRMWStatusDefault();
            if (isVolatile)
            {
                assignment->gtFlags |= GTF_IND_VOLATILE;
            }

            // TODO: JIT dump
        }
        break;

        case GT_BLK:
        case GT_OBJ:
        {
            assert(varTypeIsStruct(location));
            GenTreeBlk* storeBlk = location->AsBlk();
            genTreeOps  storeOper;
            switch (location->gtOper)
            {
                case GT_BLK:
                    storeOper = GT_STORE_BLK;
                    break;
                case GT_OBJ:
                    storeOper = GT_STORE_OBJ;
                    break;
                default:
                    unreached();
            }
            JITDUMP("Rewriting GT_ASG(%s(X), Y) to %s(X,Y):\n", GenTree::OpName(location->gtOper),
                    GenTree::OpName(storeOper));
            storeBlk->SetOperRaw(storeOper);
            storeBlk->gtFlags &= ~GTF_DONT_CSE;
            storeBlk->gtFlags |=
                (assignment->gtFlags & (GTF_ALL_EFFECT | GTF_BLK_VOLATILE | GTF_BLK_UNALIGNED | GTF_DONT_CSE));
            storeBlk->AsBlk()->Data() = value;

            // Remove the block node from its current position and replace the assignment node with it
            // (now in its store form).
            BlockRange().Remove(storeBlk);
            BlockRange().InsertBefore(assignment, storeBlk);
            use.ReplaceWith(storeBlk);
            BlockRange().Remove(assignment);
            DISPTREERANGE(BlockRange(), use.Def());
            JITDUMP("\n");
        }
        break;

        default:
            unreached();
            break;
    }
}

void Rationalizer::RewriteAddress(LIR::Use& use)
{
    assert(use.IsInitialized());

    GenTreeUnOp* address = use.Def()->AsUnOp();
    assert(address->OperGet() == GT_ADDR);

    GenTree*   location   = address->gtGetOp1();
    genTreeOps locationOp = location->OperGet();

    if (location->IsLocal())
    {
// We are changing the child from GT_LCL_VAR TO GT_LCL_VAR_ADDR.
// Therefore gtType of the child needs to be changed to a TYP_BYREF
#ifdef DEBUG
        if (locationOp == GT_LCL_VAR)
        {
            JITDUMP("Rewriting GT_ADDR(GT_LCL_VAR) to GT_LCL_VAR_ADDR:\n");
        }
        else
        {
            assert(locationOp == GT_LCL_FLD);
            JITDUMP("Rewriting GT_ADDR(GT_LCL_FLD) to GT_LCL_FLD_ADDR:\n");
        }
#endif // DEBUG

        location->SetOper(addrForm(locationOp));
        location->gtType = TYP_BYREF;
        copyFlags(location, address, GTF_ALL_EFFECT);

        use.ReplaceWith(location);
        BlockRange().Remove(address);
    }
    else if (locationOp == GT_CLS_VAR)
    {
        location->SetOper(GT_CLS_VAR_ADDR);
        location->gtType = TYP_BYREF;
        copyFlags(location, address, GTF_ALL_EFFECT);

        use.ReplaceWith(location);
        BlockRange().Remove(address);

        JITDUMP("Rewriting GT_ADDR(GT_CLS_VAR) to GT_CLS_VAR_ADDR:\n");
    }
    else if (location->OperIsIndir())
    {
        use.ReplaceWith(location->gtGetOp1());
        BlockRange().Remove(location);
        BlockRange().Remove(address);

        JITDUMP("Rewriting GT_ADDR(GT_IND(X)) to X:\n");
    }

    DISPTREERANGE(BlockRange(), use.Def());
    JITDUMP("\n");
}

Compiler::fgWalkResult Rationalizer::RewriteNode(GenTree** useEdge, Compiler::GenTreeStack& parentStack)
{
    assert(useEdge != nullptr);

    GenTree* node = *useEdge;
    assert(node != nullptr);

#ifdef DEBUG
    const bool isLateArg = (node->gtFlags & GTF_LATE_ARG) != 0;
#endif

    // Clear the REVERSE_OPS flag on the current node.
    node->gtFlags &= ~GTF_REVERSE_OPS;

    LIR::Use use;
    if (parentStack.Height() < 2)
    {
        use = LIR::Use::GetDummyUse(BlockRange(), *useEdge);
    }
    else
    {
        use = LIR::Use(BlockRange(), useEdge, parentStack.Top(1));
    }

    assert(node == use.Def());
    switch (node->OperGet())
    {
        case GT_ASG:
            RewriteAssignment(use);
            break;

        case GT_BOX:
        case GT_ARR_ADDR:
            // BOX/ARR_ADDR at this level are just NOPs.
            use.ReplaceWith(node->gtGetOp1());
            BlockRange().Remove(node);
            break;

        case GT_ADDR:
            RewriteAddress(use);
            break;

        case GT_IND:
        case GT_BLK:
        case GT_OBJ:
            RewriteIndir(use);
            break;

        case GT_NOP:
            // fgMorph sometimes inserts NOP nodes between defs and uses
            // supposedly 'to prevent constant folding'. In this case, remove the
            // NOP.
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
                // None of the transforms performed herein violate tree order, so isClosed
                // should always be true.
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
                    // None of the transforms performed herein violate tree order, so isClosed
                    // should always be true.
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

        case GT_ARGPLACE:
            // Remove argplace and list nodes from the execution order.
            BlockRange().Remove(node);
            break;

#if defined(TARGET_XARCH) || defined(TARGET_ARM)
        case GT_CLS_VAR:
        {
            // Class vars that are the target of an assignment will get rewritten into
            // GT_STOREIND(GT_CLS_VAR_ADDR, val) by RewriteAssignment. This check is
            // not strictly necessary--the GT_IND(GT_CLS_VAR_ADDR) pattern that would
            // otherwise be generated would also be picked up by RewriteAssignment--but
            // skipping the rewrite here saves an allocation and a bit of extra work.
            const bool isLHSOfAssignment = (use.User()->OperGet() == GT_ASG) && (use.User()->gtGetOp1() == node);
            if (!isLHSOfAssignment)
            {
                GenTree* ind = comp->gtNewOperNode(GT_IND, node->TypeGet(), node);
                if ((node->gtFlags & GTF_CLS_VAR_VOLATILE) != 0)
                {
                    ind->gtFlags |= GTF_IND_VOLATILE;
                }

                node->gtFlags &= ~GTF_CLS_VAR_VOLATILE;
                node->SetOper(GT_CLS_VAR_ADDR);
                node->gtType = TYP_BYREF;

                BlockRange().InsertAfter(node, ind);
                use.ReplaceWith(ind);

                // TODO: JIT dump
            }
        }
        break;
#endif // TARGET_XARCH

        case GT_INTRINSIC:
            // Non-target intrinsics should have already been rewritten back into user calls.
            assert(comp->IsTargetIntrinsic(node->AsIntrinsic()->gtIntrinsicName));
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
        {
            GenTreeSIMD* simdNode = node->AsSIMD();
            unsigned     simdSize = simdNode->GetSimdSize();
            var_types    simdType = comp->getSIMDTypeForSize(simdSize);

            // TODO-1stClassStructs: This should be handled more generally for enregistered or promoted
            // structs that are passed or returned in a different register type than their enregistered
            // type(s).
            if (simdNode->gtType == TYP_I_IMPL && simdNode->GetSimdSize() == TARGET_POINTER_SIZE)
            {
                // This happens when it is consumed by a GT_RET_EXPR.
                // It can only be a Vector2f or Vector2i.
                assert(genTypeSize(simdNode->GetSimdBaseType()) == 4);
                simdNode->gtType = TYP_SIMD8;
            }
            // Certain SIMD trees require rationalizing.
            if (simdNode->AsSIMD()->GetSIMDIntrinsicId() == SIMDIntrinsicInitArray)
            {
                // Rewrite this as an explicit load.
                JITDUMP("Rewriting GT_SIMD array init as an explicit load:\n");
                unsigned int baseTypeSize = genTypeSize(simdNode->GetSimdBaseType());

                GenTree* base    = simdNode->Op(1);
                GenTree* index   = (simdNode->GetOperandCount() == 2) ? simdNode->Op(2) : nullptr;
                GenTree* address = new (comp, GT_LEA)
                    GenTreeAddrMode(TYP_BYREF, base, index, baseTypeSize, OFFSETOF__CORINFO_Array__data);
                GenTree* ind = comp->gtNewOperNode(GT_IND, simdType, address);

                BlockRange().InsertBefore(simdNode, address, ind);
                use.ReplaceWith(ind);
                BlockRange().Remove(simdNode);

                DISPTREERANGE(BlockRange(), use.Def());
                JITDUMP("\n");
            }
            else
            {
                // This code depends on the fact that NONE of the SIMD intrinsics take vector operands
                // of a different width.  If that assumption changes, we will EITHER have to make these type
                // transformations during importation, and plumb the types all the way through the JIT,
                // OR add a lot of special handling here.

                // TODO-Review: the comment above seems outdated. TYP_SIMDs have been "plumbed through" the Jit.
                // It may be that this code is actually dead.
                for (GenTree* operand : simdNode->Operands())
                {
                    if (operand->TypeIs(TYP_STRUCT))
                    {
                        operand->ChangeType(simdType);
                    }
                }
            }
        }
        break;
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
        {
            GenTreeHWIntrinsic* hwIntrinsicNode = node->AsHWIntrinsic();

            if (!hwIntrinsicNode->isSIMD())
            {
                break;
            }

            // TODO-1stClassStructs: This should be handled more generally for enregistered or promoted
            // structs that are passed or returned in a different register type than their enregistered
            // type(s).
            if ((hwIntrinsicNode->gtType == TYP_I_IMPL) && (hwIntrinsicNode->GetSimdSize() == TARGET_POINTER_SIZE))
            {
#ifdef TARGET_ARM64
                // Special case for GetElement/ToScalar because they take Vector64<T> and return T
                // and T can be long or ulong.
                if (!((hwIntrinsicNode->GetHWIntrinsicId() == NI_Vector64_GetElement) ||
                      (hwIntrinsicNode->GetHWIntrinsicId() == NI_Vector64_ToScalar)))
#endif
                {
                    // This happens when it is consumed by a GT_RET_EXPR.
                    // It can only be a Vector2f or Vector2i.
                    assert(genTypeSize(hwIntrinsicNode->GetSimdBaseType()) == 4);
                    hwIntrinsicNode->gtType = TYP_SIMD8;
                }
            }
            break;
        }
#endif // FEATURE_HW_INTRINSICS

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
        if (!node->OperIsStore())
        {
            // Clear the GTF_ASG flag for all nodes but stores
            node->gtFlags &= ~GTF_ASG;
        }

        if (!node->IsCall())
        {
            // Clear the GTF_CALL flag for all nodes but calls
            node->gtFlags &= ~GTF_CALL;
        }

        if (node->IsValue() && use.IsDummyUse())
        {
            node->SetUnusedValue();
        }

        if (node->TypeGet() == TYP_LONG)
        {
            comp->compLongUsed = true;
        }
    }

    assert(isLateArg == ((use.Def()->gtFlags & GTF_LATE_ARG) != 0));

    return Compiler::WALK_CONTINUE;
}

//------------------------------------------------------------------------
// DoPhase: Run the rationalize over the method IR.
//
// Returns:
//    PhaseStatus indicating, what, if anything, was modified
//
PhaseStatus Rationalizer::DoPhase()
{
    class RationalizeVisitor final : public GenTreeVisitor<RationalizeVisitor>
    {
        Rationalizer& m_rationalizer;

    public:
        enum
        {
            ComputeStack      = true,
            DoPreOrder        = true,
            DoPostOrder       = true,
            UseExecutionOrder = true,
        };

        RationalizeVisitor(Rationalizer& rationalizer)
            : GenTreeVisitor<RationalizeVisitor>(rationalizer.comp), m_rationalizer(rationalizer)
        {
        }

        // Rewrite intrinsics that are not supported by the target back into user calls.
        // This needs to be done before the transition to LIR because it relies on the use
        // of fgMorphArgs, which is designed to operate on HIR. Once this is done for a
        // particular statement, link that statement's nodes into the current basic block.
        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* const node = *use;
            if (node->OperGet() == GT_INTRINSIC &&
                m_rationalizer.comp->IsIntrinsicImplementedByUserCall(node->AsIntrinsic()->gtIntrinsicName))
            {
                m_rationalizer.RewriteIntrinsicAsUserCall(use, this->m_ancestors);
            }

            return Compiler::WALK_CONTINUE;
        }

        // Rewrite HIR nodes into LIR nodes.
        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            return m_rationalizer.RewriteNode(use, this->m_ancestors);
        }
    };

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
