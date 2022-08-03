// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// Simple Forward Substitution
//
// This phase tries to reconnect trees that were split early on by
// phases like the importer and inlining. We run it before morph
// to provide more context for morph's tree based optimizations, and
// we run it after the local address visitor because that phase sets
// address exposure for locals and computes (early) ref counts.
//
// The general pattern we look for is
//
//  Statement(n):
//    GT_ASG(lcl, tree)
//  Statement(n+1):
//    ... use of lcl ...
//
// where those are the only appearances of lcl and lcl is not address
// exposed.
//
// The "optimization" here transforms this to
//
//  ~~Statement(n)~~ (removed)
//  Statement(n+1):
//    ... use of tree ...
//
// As always our main concerns are throughput, legality, profitability,
// and ensuring downstream phases do not get confused.
//
// For throughput, we try and early out on illegal or unprofitable cases
// before doing the more costly bits of analysis. We only scan a limited
// amount of IR and just give up if we can't find what we are looking for.
//
// If we're successful we will backtrack a bit, to try and catch cases like
//
// Statement(n):
//    lcl1 = tree1
// Statement(n+1):
//    lcl2 = tree2
// Statement(n+2):
//    use ...  lcl1 ... use ... lcl2 ...
//
// If we can forward sub tree2, then the def and use of lcl1 become
// adjacent.
//
// For legality we must show that evaluating "tree" at its new position
// can't change any observable behavior. This largely means running an
// interference analysis between tree and the portion of Statement(n+1)
// that will evaluate before "tree". This analysis is complicated by some
// missing flags on trees, in particular modelling the potential uses
// of exposed locals. We run supplementary scans looking for those.
//
// Ideally we'd update the tree with our findings, or better yet ensure
// that upstream phases didn't leave the wrong flags.
//
// For profitability we first try and avoid code growth. We do this
// by only substituting in cases where lcl has exactly one def and one use.
// This info is computed for us but the RCS_Early ref counting done during
// the immediately preceding fgMarkAddressExposedLocals phase.
//
// Because of this, once we've substituted "tree" we know that lcl is dead
// and we can remove the assignment statement.
//
// Even with ref count screening, we don't know for sure where the
// single use of local might be, so we have to seach for it.
//
// We also take pains not to create overly large trees as the recursion
// done by morph incorporates a lot of state; deep trees may lead to
// stack overflows.
//
// There are a fair number of ad-hoc restrictions on what can be
// substituted where; these reflect various blemishes or implicit
// contracts in our IR shapes that we should either remove or mandate.
//
// Possible enhancements:
// * Allow fwd sub of "simple, cheap" trees when there's more than one use.
// * Search more widely for the use.
// * Use height/depth to avoid blowing morph's recursion, rather than tree size.
// * Sub across a block boundary if successor block is unique, join-free,
//   and in the same EH region.
// * Rerun this later, after we have built SSA, and handle single-def single-use
//   from SSA perspective.
// * Fix issue in morph that can unsoundly reorder call args, and remove
//   extra effects computation from ForwardSubVisitor.
// * We can be more aggressive with GTF_IND_INVARIANT / GTF_IND_NONFAULTING
//   nodes--even though they may be marked GTF_GLOB_REF, they can be freely
//   reordered. See if this offers any benefit.
//
//------------------------------------------------------------------------

//------------------------------------------------------------------------
// fgForwardSub: run forward substitution in this method
//
// Returns:
//   suitable phase status
//
PhaseStatus Compiler::fgForwardSub()
{
    if (!opts.OptimizationEnabled())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

#if defined(DEBUG)
    if (JitConfig.JitNoForwardSub() > 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    bool changed = false;

    for (BasicBlock* const block : Blocks())
    {
        JITDUMP("\n\n===> " FMT_BB "\n", block->bbNum);
        changed |= fgForwardSubBlock(block);
    }

    return changed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// fgForwardSubBlock: run forward substitution in this block
//
// Arguments:
//    block -- block to process
//
// Returns:
//    true if any IR was modified
//
bool Compiler::fgForwardSubBlock(BasicBlock* block)
{
    Statement* stmt     = block->firstStmt();
    Statement* lastStmt = block->lastStmt();
    bool       changed  = false;

    while (stmt != lastStmt)
    {
        Statement* const prevStmt    = stmt->GetPrevStmt();
        Statement* const nextStmt    = stmt->GetNextStmt();
        bool const       substituted = fgForwardSubStatement(stmt);

        if (substituted)
        {
            fgRemoveStmt(block, stmt);
            changed = true;
        }

        // Try backtracking if we substituted.
        //
        if (substituted && (prevStmt != lastStmt) && prevStmt->GetRootNode()->OperIs(GT_ASG))
        {
            // Yep, bactrack.
            //
            stmt = prevStmt;
        }
        else
        {
            // Move on to the next.
            //
            stmt = nextStmt;
        }
    }

    return changed;
}

//------------------------------------------------------------------------
// ForwardSubVisitor: tree visitor to locate uses of a local in a tree
//
// Also computes the set of side effects that happen "before" the use,
// and counts the size of the tree.
//
// Effects accounting is complicated by missing flags and by the need
// to avoid introducing interfering call args.
//
class ForwardSubVisitor final : public GenTreeVisitor<ForwardSubVisitor>
{
public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true
    };

    ForwardSubVisitor(Compiler* compiler, unsigned lclNum)
        : GenTreeVisitor<ForwardSubVisitor>(compiler)
        , m_use(nullptr)
        , m_node(nullptr)
        , m_parentNode(nullptr)
        , m_lclNum(lclNum)
        , m_useCount(0)
        , m_useFlags(GTF_EMPTY)
        , m_accumulatedFlags(GTF_EMPTY)
        , m_treeSize(0)
    {
    }

    Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        m_treeSize++;
        GenTree* const node = *use;

        if (node->OperIs(GT_LCL_VAR))
        {
            unsigned const lclNum = node->AsLclVarCommon()->GetLclNum();

            if (lclNum == m_lclNum)
            {
                m_useCount++;

                // Screen out contextual "uses"
                //
                GenTree* const parent = user;
                bool const     isDef  = parent->OperIs(GT_ASG) && (parent->gtGetOp1() == node);
                bool const     isAddr = parent->OperIs(GT_ADDR);

                bool isCallTarget = false;

                // Quirk:
                //
                // fgGetStubAddrArg cannot handle complex trees (it calls gtClone)
                //
                if (parent->IsCall())
                {
                    GenTreeCall* const parentCall = parent->AsCall();
                    isCallTarget = (parentCall->gtCallType == CT_INDIRECT) && (parentCall->gtCallAddr == node);
                }

                if (!isDef && !isAddr && !isCallTarget)
                {
                    m_node       = node;
                    m_use        = use;
                    m_useFlags   = m_accumulatedFlags;
                    m_parentNode = parent;
                }
            }
        }

        if (node->OperIsLocal())
        {
            unsigned const lclNum = node->AsLclVarCommon()->GetLclNum();

            // Uses of address-exposed locals are modelled as global refs.
            //
            LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

            if (varDsc->IsAddressExposed())
            {
                m_accumulatedFlags |= GTF_GLOB_REF;
            }
        }

        m_accumulatedFlags |= (node->gtFlags & GTF_GLOB_EFFECT);

        return fgWalkResult::WALK_CONTINUE;
    }

    unsigned GetUseCount() const
    {
        return m_useCount;
    }

    GenTree* GetNode() const
    {
        return m_node;
    }

    GenTree** GetUse() const
    {
        return m_use;
    }

    GenTree* GetParentNode() const
    {
        return m_parentNode;
    }

    GenTreeFlags GetFlags() const
    {
        return m_useFlags;
    }

    bool IsCallArg() const
    {
        return m_parentNode->IsCall();
    }

    unsigned GetComplexity() const
    {
        return m_treeSize;
    }

private:
    GenTree**    m_use;
    GenTree*     m_node;
    GenTree*     m_parentNode;
    unsigned     m_lclNum;
    unsigned     m_useCount;
    GenTreeFlags m_useFlags;
    GenTreeFlags m_accumulatedFlags;
    unsigned     m_treeSize;
};

//------------------------------------------------------------------------
// EffectsVisitor: tree visitor to compute missing effects of a tree.
//
class EffectsVisitor final : public GenTreeVisitor<EffectsVisitor>
{
public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true
    };

    EffectsVisitor(Compiler* compiler) : GenTreeVisitor<EffectsVisitor>(compiler), m_flags(GTF_EMPTY)
    {
    }

    Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* const node = *use;
        m_flags |= node->gtFlags & GTF_ALL_EFFECT;

        if (node->OperIsLocal())
        {
            unsigned const   lclNum = node->AsLclVarCommon()->GetLclNum();
            LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

            if (varDsc->IsAddressExposed())
            {
                m_flags |= GTF_GLOB_REF;
            }
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    GenTreeFlags GetFlags()
    {
        return m_flags;
    }

private:
    GenTreeFlags m_flags;
};

//------------------------------------------------------------------------
// fgForwardSubStatement: forward substitute this statement's
//  computation to the next statement, if legal and profitable
//
// arguments:
//    stmt - statement in question
//
// Returns:
//    true if statement computation was forwarded.
//    caller is responsible for removing the now-dead statement.
//
bool Compiler::fgForwardSubStatement(Statement* stmt)
{
    // Is this tree a def of a single use, unaliased local?
    //
    GenTree* const rootNode = stmt->GetRootNode();

    if (!rootNode->OperIs(GT_ASG))
    {
        return false;
    }

    GenTree* const lhsNode = rootNode->gtGetOp1();

    if (!lhsNode->OperIs(GT_LCL_VAR))
    {
        return false;
    }

    JITDUMP("    [%06u]: ", dspTreeID(rootNode))

    unsigned const   lclNum = lhsNode->AsLclVarCommon()->GetLclNum();
    LclVarDsc* const varDsc = lvaGetDesc(lclNum);

    // Leave pinned locals alone.
    // This is just a perf opt -- we shouldn't find any uses.
    //
    if (varDsc->lvPinned)
    {
        JITDUMP(" pinned local\n");
        return false;
    }

    // Only fwd sub if we expect no code duplication
    // We expect one def and one use.
    //
    if (varDsc->lvRefCnt(RCS_EARLY) != 2)
    {
        JITDUMP(" not asg (single-use lcl)\n");
        return false;
    }

    // And local is unalised
    //
    if (varDsc->IsAddressExposed())
    {
        JITDUMP(" not asg (unaliased single-use lcl)\n");
        return false;
    }

    // Could handle this case --perhaps-- but we'd want to update ref counts.
    //
    if (lvaIsImplicitByRefLocal(lclNum))
    {
        JITDUMP(" implicit by-ref local\n");
        return false;
    }

    // Check the tree to substitute.
    //
    // We could just extract the value portion and forward sub that,
    // but cleanup would be more complicated.
    //
    GenTree* const rhsNode    = rootNode->gtGetOp2();
    GenTree*       fwdSubNode = rhsNode;

    // Can't substitute a qmark (unless the use is RHS of an assign... could check for this)
    // Can't substitute GT_CATCH_ARG.
    // Can't substitute GT_LCLHEAP.
    //
    // Don't substitute a no return call (trips up morph in some cases).
    //
    if (fwdSubNode->OperIs(GT_QMARK, GT_CATCH_ARG, GT_LCLHEAP))
    {
        JITDUMP(" tree to sub is qmark, catch arg, or lcl heap\n");
        return false;
    }

    if (fwdSubNode->IsCall() && fwdSubNode->AsCall()->IsNoReturn())
    {
        JITDUMP(" tree to sub is a 'no return' call\n");
        return false;
    }

    // Bail if sub node has embedded assignment.
    //
    if ((fwdSubNode->gtFlags & GTF_ASG) != 0)
    {
        JITDUMP(" tree to sub has effects\n");
        return false;
    }

    // Bail if sub node has mismatched types.
    // Might be able to tolerate these by retyping.
    //
    if (lhsNode->TypeGet() != fwdSubNode->TypeGet())
    {
        JITDUMP(" mismatched types (assignment)\n");
        return false;
    }

    // If lhs is mulit-reg, rhs must be too.
    //
    if (lhsNode->IsMultiRegNode() && !fwdSubNode->IsMultiRegNode())
    {
        JITDUMP(" would change multi-reg (assignment)\n");
        return false;
    }

    // Don't fwd sub overly large trees.
    // Size limit here is ad-hoc. Need to tune.
    //
    // Consider instead using the height of the fwdSubNode.
    //
    unsigned const nodeLimit = 16;

    if (gtComplexityExceeds(&fwdSubNode, nodeLimit))
    {
        JITDUMP(" tree to sub has more than %u nodes\n", nodeLimit);
        return false;
    }

    // Local and tree to substitute seem suitable.
    // See if the next statement contains the one and only use.
    //
    Statement* const nextStmt = stmt->GetNextStmt();

    // We often see stale flags, eg call flags after inlining.
    // Try and clean these up.
    //
    gtUpdateStmtSideEffects(nextStmt);
    gtUpdateStmtSideEffects(stmt);

    // Scan for the (single) use.
    //
    ForwardSubVisitor fsv(this, lclNum);
    fsv.WalkTree(nextStmt->GetRootNodePointer(), nullptr);

    // LclMorph (via RCS_Early) said there was just one use.
    // It had better have gotten this right.
    //
    assert(fsv.GetUseCount() <= 1);

    if ((fsv.GetUseCount() == 0) || (fsv.GetNode() == nullptr))
    {
        JITDUMP(" no next stmt use\n");
        return false;
    }

    JITDUMP(" [%06u] is only use of [%06u] (V%02u) ", dspTreeID(fsv.GetNode()), dspTreeID(lhsNode), lclNum);

    // If next statement already has a large tree, hold off
    // on making it even larger.
    //
    // We use total node count. Consider instead using the depth of the use and the
    // height of the fwdSubNode.
    //
    unsigned const nextTreeLimit = 200;
    if ((fsv.GetComplexity() > nextTreeLimit) && gtComplexityExceeds(&fwdSubNode, 1))
    {
        JITDUMP(" next stmt tree is too large (%u)\n", fsv.GetComplexity());
        return false;
    }

    // Next statement seems suitable.
    // See if we can forward sub without changing semantics.
    //
    GenTree* const nextRootNode = nextStmt->GetRootNode();

    // Bail if types disagree.
    // Might be able to tolerate these by retyping.
    //
    if (fsv.GetNode()->TypeGet() != fwdSubNode->TypeGet())
    {
        JITDUMP(" mismatched types (substitution)\n");
        return false;
    }

    // We can forward sub if
    //
    // the value of the fwdSubNode can't change and its evaluation won't cause side effects,
    //
    // or,
    //
    // if the next tree can't change the value of fwdSubNode or be impacted by fwdSubNode effects
    //
    const bool fwdSubNodeInvariant   = ((fwdSubNode->gtFlags & GTF_ALL_EFFECT) == 0);
    const bool nextTreeIsPureUpToUse = ((fsv.GetFlags() & (GTF_EXCEPT | GTF_GLOB_REF | GTF_CALL)) == 0);
    if (!fwdSubNodeInvariant && !nextTreeIsPureUpToUse)
    {
        // Fwd sub may impact global values and or reorder exceptions...
        //
        JITDUMP(" potentially interacting effects\n");
        return false;
    }

    // If we're relying on purity of fwdSubNode for legality of forward sub,
    // do some extra checks for global uses that might not be reflected in the flags.
    //
    // TODO: remove this once we can trust upstream phases and/or gtUpdateStmtSideEffects
    // to set GTF_GLOB_REF properly.
    //
    if (fwdSubNodeInvariant && ((fsv.GetFlags() & (GTF_CALL | GTF_ASG)) != 0))
    {
        EffectsVisitor ev(this);
        ev.WalkTree(&fwdSubNode, nullptr);

        if ((ev.GetFlags() & GTF_GLOB_REF) != 0)
        {
            JITDUMP(" potentially interacting effects (AX locals)\n");
            return false;
        }
    }

    // Finally, profitability checks.
    //
    // These conditions can be checked earlier in the final version to save some throughput.
    // Perhaps allowing for bypass with jit stress.
    //
    // If fwdSubNode is an address-exposed local, forwarding it may lose optimizations due
    // to GLOB_REF "poisoning" the tree. CQ analysis shows this to not be a problem with
    // structs.
    //
    if (fwdSubNode->OperIs(GT_LCL_VAR))
    {
        unsigned const   fwdLclNum = fwdSubNode->AsLclVarCommon()->GetLclNum();
        LclVarDsc* const fwdVarDsc = lvaGetDesc(fwdLclNum);

        if (!varTypeIsStruct(fwdVarDsc) && fwdVarDsc->IsAddressExposed())
        {
            JITDUMP(" V%02u is address exposed\n", fwdLclNum);
            return false;
        }
    }

    // Optimization:
    //
    // If we are about to substitute GT_OBJ, see if we can simplify it first.
    // Not doing so can lead to regressions...
    //
    // Hold off on doing this for call args for now (per issue #51569).
    // Hold off on OBJ(GT_LCL_ADDR).
    //
    if (fwdSubNode->OperIs(GT_OBJ) && !fsv.IsCallArg() && fwdSubNode->gtGetOp1()->OperIs(GT_ADDR))
    {
        const bool     destroyNodes = false;
        GenTree* const optTree      = fgMorphTryFoldObjAsLclVar(fwdSubNode->AsObj(), destroyNodes);
        if (optTree != nullptr)
        {
            JITDUMP(" [folding OBJ(ADDR(LCL...))]");
            fwdSubNode = optTree;
        }
    }

    // Quirks:
    //
    // Don't substitute nodes "AddFinalArgsAndDetermineABIInfo" doesn't handle into struct args.
    //
    if (fsv.IsCallArg() && fsv.GetNode()->TypeIs(TYP_STRUCT) &&
        !fwdSubNode->OperIs(GT_OBJ, GT_LCL_VAR, GT_LCL_FLD, GT_MKREFANY))
    {
        JITDUMP(" use is a struct arg; fwd sub node is not OBJ/LCL_VAR/LCL_FLD/MKREFANY\n");
        return false;
    }

    // We may sometimes lose or change a type handle. Avoid substituting if so.
    //
    // However, we allow free substitution of hardware SIMD types.
    //
    CORINFO_CLASS_HANDLE fwdHnd = gtGetStructHandleIfPresent(fwdSubNode);
    CORINFO_CLASS_HANDLE useHnd = gtGetStructHandleIfPresent(fsv.GetNode());
    if (fwdHnd != useHnd)
    {
        if ((fwdHnd == NO_CLASS_HANDLE) || (useHnd == NO_CLASS_HANDLE))
        {
            JITDUMP(" would add/remove struct handle (substitution)\n");
            return false;
        }

#ifdef FEATURE_SIMD
        const bool bothHWSIMD = isHWSIMDClass(fwdHnd) && isHWSIMDClass(useHnd);
#else
        const bool bothHWSIMD = false;
#endif

        if (!bothHWSIMD)
        {
            JITDUMP(" would change struct handle (substitution)\n");
            return false;
        }
    }

#ifdef FEATURE_SIMD
    // Don't forward sub a SIMD call under a HW intrinsic node.
    // LowerCallStruct is not prepared for this.
    //
    if (fwdSubNode->IsCall() && varTypeIsSIMD(fwdSubNode->TypeGet()) && fsv.GetParentNode()->OperIs(GT_HWINTRINSIC))
    {
        JITDUMP(" simd returning call; hw intrinsic\n");
        return false;
    }
#endif // FEATURE_SIMD

    // There are implicit assumptions downstream on where/how multi-reg ops
    // can appear.
    //
    // Eg if fwdSubNode is a multi-reg call, parent node must be GT_ASG and the
    // local being defined must be specially marked up.
    //
    if (fwdSubNode->IsMultiRegCall())
    {
        GenTree* const parentNode = fsv.GetParentNode();

        if (!parentNode->OperIs(GT_ASG))
        {
            JITDUMP(" multi-reg call, parent not asg\n");
            return false;
        }

        GenTree* const parentNodeLHS = parentNode->gtGetOp1();

        if (!parentNodeLHS->OperIs(GT_LCL_VAR))
        {
            JITDUMP(" multi-reg call, parent not asg(lcl, ...)\n");
            return false;
        }

#if defined(TARGET_X86) || defined(TARGET_ARM)
        if (fwdSubNode->TypeGet() == TYP_LONG)
        {
            JITDUMP(" TYP_LONG fwd sub node, target is x86/arm\n");
            return false;
        }
#endif // defined(TARGET_X86) || defined(TARGET_ARM)

        GenTreeLclVar* const parentNodeLHSLocal = parentNodeLHS->AsLclVar();

        unsigned const   lhsLclNum = parentNodeLHSLocal->GetLclNum();
        LclVarDsc* const lhsVarDsc = lvaGetDesc(lhsLclNum);

        JITDUMP(" [marking V%02u as multi-reg-ret]", lhsLclNum);
        lhsVarDsc->lvIsMultiRegRet = true;
        parentNodeLHSLocal->SetMultiReg();
    }

    // If a method returns a multi-reg type, only forward sub locals,
    // and ensure the local and operand have the required markup.
    //
    // (see eg impFixupStructReturnType)
    //
    if (compMethodReturnsMultiRegRetType() && fsv.GetParentNode()->OperIs(GT_RETURN))
    {
        if (!fwdSubNode->OperIs(GT_LCL_VAR))
        {
            JITDUMP(" parent is multi-reg return, fwd sub node is not lcl var\n");
            return false;
        }

#if defined(TARGET_X86) || defined(TARGET_ARM)
        if (fwdSubNode->TypeGet() == TYP_LONG)
        {
            JITDUMP(" TYP_LONG fwd sub node, target is x86/arm\n");
            return false;
        }
#endif // defined(TARGET_X86) || defined(TARGET_ARM)

        GenTreeLclVar* const fwdSubNodeLocal = fwdSubNode->AsLclVar();
        unsigned const       fwdLclNum       = fwdSubNodeLocal->GetLclNum();

        // These may later turn into indirections and the backend does not support
        // those as sources of multi-reg returns.
        //
        if (lvaIsImplicitByRefLocal(fwdLclNum))
        {
            JITDUMP(" parent is multi-reg return; fwd sub node is implicit byref\n");
            return false;
        }

        LclVarDsc* const fwdVarDsc = lvaGetDesc(fwdLclNum);

        JITDUMP(" [marking V%02u as multi-reg-ret]", fwdLclNum);
        fwdVarDsc->lvIsMultiRegRet = true;
        fwdSubNodeLocal->SetMultiReg();
        fwdSubNodeLocal->gtFlags |= GTF_DONT_CSE;
    }

    // If the use is a multi-reg arg, don't forward sub non-locals.
    //
    if (fsv.GetNode()->IsMultiRegNode() && !fwdSubNode->IsMultiRegNode())
    {
        JITDUMP(" would change multi-reg (substitution)\n");
        return false;
    }

    // If the initial has truncate on store semantics, we need to replicate
    // that here with a cast.
    //
    if (varDsc->lvNormalizeOnStore() && fgCastNeeded(fwdSubNode, varDsc->TypeGet()))
    {
        JITDUMP(" [adding cast for normalize on store]");
        fwdSubNode = gtNewCastNode(TYP_INT, fwdSubNode, false, varDsc->TypeGet());
    }

    // Looks good, forward sub!
    //
    GenTree** use = fsv.GetUse();
    *use          = fwdSubNode;

    if (!fwdSubNodeInvariant)
    {
        gtUpdateStmtSideEffects(nextStmt);
    }

    JITDUMP(" -- fwd subbing [%06u]; new next stmt is\n", dspTreeID(fwdSubNode));
    DISPSTMT(nextStmt);

    return true;
}
