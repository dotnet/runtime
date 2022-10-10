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

    if (varTypeIsSIMD(indir))
    {
        if (indir->OperIs(GT_BLK, GT_OBJ))
        {
            indir->SetOper(GT_IND);
        }

        RewriteSIMDIndir(use);
    }
}

// RewriteSIMDIndir: Rewrite a SIMD indirection as a simple lclVar if possible.
//
// Arguments:
//    use - A use of a GT_IND node of SIMD type
//
// TODO-ADDR: delete this once block morphing stops taking addresses of locals
// under COMMAs.
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

            if (((addr->gtFlags & GTF_VAR_DEF) != 0) && (genTypeSize(simdType) < genTypeSize(lclType)))
            {
                addr->gtFlags |= GTF_VAR_USEASG;
            }

            comp->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::LocalField));
        }
        if (varDsc->lvPromoted)
        {
            comp->lvaSetVarDoNotEnregister(lclNum DEBUGARG(DoNotEnregisterReason::BlockOp));
        }

        addr->gtType = simdType;
        use.ReplaceWith(addr);
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
                                     GenTree* arg1,
                                     GenTree* arg2)
{
    GenTree* const tree           = *use;
    GenTree* const treeFirstNode  = comp->fgGetFirstNode(tree);
    GenTree* const insertionPoint = treeFirstNode->gtPrev;

    BlockRange().Remove(treeFirstNode, tree);

    // Create the call node
    GenTreeCall* call = comp->gtNewCallNode(CT_USER_FUNC, callHnd, tree->gtType);

    if (arg2 != nullptr)
    {
        call->gtArgs.PushFront(comp, NewCallArg::Primitive(arg2));
        call->gtFlags |= arg2->gtFlags & GTF_ALL_EFFECT;
    }

    if (arg1 != nullptr)
    {
        call->gtArgs.PushFront(comp, NewCallArg::Primitive(arg1));
        call->gtFlags |= arg1->gtFlags & GTF_ALL_EFFECT;
    }

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

    GenTree* arg1 = intrinsic->gtGetOp1();
    GenTree* arg2 = intrinsic->gtGetOp2();
    RewriteNodeAsCall(use, parents, intrinsic->gtMethodHandle,
#ifdef FEATURE_READYTORUN
                      intrinsic->gtEntryPoint,
#endif
                      arg1, arg2);
}

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

            for (GenTree* const tree : stmt->TreeList())
            {
                // QMARK nodes should have been removed before this phase.
                assert(!tree->OperIs(GT_QMARK));

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
        store->AsLclFld()->SetLayout(var->AsLclFld()->GetLayout());
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

    if (varTypeIsSIMD(location) && assignment->OperIsInitBlkOp())
    {
        var_types simdType = location->TypeGet();
        GenTree*  initVal  = assignment->AsOp()->gtOp2;
        GenTree*  zeroCon  = comp->gtNewZeroConNode(simdType);
        noway_assert(initVal->IsIntegralConst(0)); // All SIMD InitBlks are zero inits.

        assignment->gtOp2 = zeroCon;
        value             = zeroCon;

        BlockRange().InsertAfter(initVal, zeroCon);
        BlockRange().Remove(initVal);
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
    else if (location->OperIsIndir())
    {
        use.ReplaceWith(location->gtGetOp1());
        BlockRange().Remove(location);
        BlockRange().Remove(address);

        JITDUMP("Rewriting GT_ADDR(GT_IND(X)) to X:\n");
    }
    else
    {
        unreached();
    }

    DISPTREERANGE(BlockRange(), use.Def());
    JITDUMP("\n");
}

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
        case GT_ASG:
            RewriteAssignment(use);
            break;

        case GT_ADDR:
            RewriteAddress(use);
            break;

        case GT_IND:
        case GT_BLK:
        case GT_OBJ:
            RewriteIndir(use);
            break;

        case GT_CALL:
            // In linear order we no longer need to retain the stores in early
            // args as these have now been sequenced.
            for (CallArg& arg : node->AsCall()->gtArgs.EarlyArgs())
            {
                if (!arg.GetEarlyNode()->IsValue())
                {
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

        case GT_NOP:
        case GT_BOX:
        case GT_ARR_ADDR:
            // "optNarrowTree" sometimes inserts NOP nodes between defs and uses.
            // In this case, remove the NOP. BOX/ARR_ADDR are such "passthrough"
            // nodes by design, and at this point we no longer need them.
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

#ifdef FEATURE_SIMD
        case GT_SIMD:
        {
            GenTreeSIMD* simdNode = node->AsSIMD();
            unsigned     simdSize = simdNode->GetSimdSize();
            var_types    simdType = comp->getSIMDTypeForSize(simdSize);

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
        }
        break;
#endif // FEATURE_SIMD

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

#ifdef TARGET_ARM64
            if (node->OperIs(GT_SUB))
            {
                m_rationalizer.RewriteSubLshDiv(use);
            }
#endif

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
