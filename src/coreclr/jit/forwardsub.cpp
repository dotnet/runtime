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
// to provide more context for morph's tree based optimizations and
// it makes use of early liveness to know which uses are last uses.
//
// The general pattern we look for is
//
//  Statement(n):
//    lcl = tree
//  Statement(n+1):
//    ... use of lcl ...
//
// where the use of lcl is a last use.
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
// We use the linked lists of locals to quickly find out if there is a
// candidate last use in the next statement.
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
// by only substituting in cases where we can see a def followed by
// a single (last) use, i.e. we do not allow substituting multiple
// uses.
//
// Once we've substituted "tree" we know that lcl is dead (since the use
// was a last use) and we can remove the store statement.
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

    if (!fgDidEarlyLiveness)
    {
        JITDUMP("Liveness information not available, skipping forward sub\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

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
        if (substituted && (prevStmt != lastStmt) && prevStmt->GetRootNode()->OperIs(GT_STORE_LCL_VAR))
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
// Effects accounting is complicated by missing flags.
//
class ForwardSubVisitor final : public GenTreeVisitor<ForwardSubVisitor>
{
public:
    enum
    {
        DoPostOrder       = true,
        UseExecutionOrder = true
    };

    ForwardSubVisitor(Compiler* compiler, unsigned lclNum) : GenTreeVisitor(compiler), m_lclNum(lclNum)
    {
        LclVarDsc* dsc = compiler->lvaGetDesc(m_lclNum);
        if (dsc->lvIsStructField)
        {
            m_parentLclNum = dsc->lvParentLcl;
        }
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
                // Screen out contextual "uses"
                //
                GenTree* const parent = user;

                // Quirk:
                //
                // fgGetStubAddrArg cannot handle complex trees (it calls gtClone)
                //
                bool isCallTarget = false;
                if (parent->IsCall())
                {
                    GenTreeCall* const parentCall = parent->AsCall();
                    isCallTarget = (parentCall->gtCallType == CT_INDIRECT) && (parentCall->gtCallAddr == node);
                }

                if (!isCallTarget && IsLastUse(node->AsLclVar()))
                {
                    m_node          = node;
                    m_use           = use;
                    m_useFlags      = m_accumulatedFlags;
                    m_useExceptions = m_accumulatedExceptions;
                    m_parentNode    = parent;
                }
            }
        }

        // Stores to and uses of address-exposed locals are modelled as global refs.
        //
        if (node->OperIsLocal())
        {
#ifdef DEBUG
            if (IsUse(node->AsLclVarCommon()))
            {
                m_useCount++;
            }
#endif
            if (m_compiler->lvaGetDesc(node->AsLclVarCommon())->IsAddressExposed())
            {
                m_accumulatedFlags |= GTF_GLOB_REF;
            }
        }

        m_accumulatedFlags |= (node->gtFlags & GTF_GLOB_EFFECT);
        if ((node->gtFlags & GTF_CALL) != 0)
        {
            m_accumulatedExceptions = ExceptionSetFlags::All;
        }
        else if ((node->gtFlags & GTF_EXCEPT) != 0)
        {
            // We can never reorder in the face of different exception types,
            // so stop calling 'OperExceptions' once we've seen more than one
            // different exception type.
            if (genCountBits(static_cast<uint32_t>(m_accumulatedExceptions)) <= 1)
            {
                m_accumulatedExceptions |= node->OperExceptions(m_compiler);
            }
        }

        return fgWalkResult::WALK_CONTINUE;
    }

#ifdef DEBUG
    unsigned GetUseCount() const
    {
        return m_useCount;
    }
#endif

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

    //------------------------------------------------------------------------
    // GetExceptions: Get precise exceptions thrown by the trees executed
    // before the use.
    //
    // Returns:
    //   Exception set.
    //
    // Remarks:
    //   The visitor stops tracking precise exceptions once it finds that 2 or
    //   more different exceptions can be thrown, so this set cannot be used
    //   for determining the precise different exceptions thrown in that case.
    //
    ExceptionSetFlags GetExceptions() const
    {
        return m_useExceptions;
    }

    bool IsCallArg() const
    {
        return m_parentNode->IsCall();
    }

    unsigned GetComplexity() const
    {
        return m_treeSize;
    }

    //------------------------------------------------------------------------
    // IsUse: Check if a local is considered a use of the forward sub candidate
    // while taking promotion into account.
    //
    // Arguments:
    //    lcl - the local
    //
    // Returns:
    //    true if the node is a use of the local candidate or any of its fields.
    //
    bool IsUse(GenTreeLclVarCommon* lcl)
    {
        unsigned lclNum = lcl->GetLclNum();
        if ((lclNum == m_lclNum) || (lclNum == m_parentLclNum))
        {
            return true;
        }

        LclVarDsc* dsc = m_compiler->lvaGetDesc(lclNum);
        return dsc->lvIsStructField && (dsc->lvParentLcl == m_lclNum);
    }

    //------------------------------------------------------------------------
    // IsLastUse: Check if the local node is a last use. The local node is expected
    // to be a GT_LCL_VAR of the local being forward subbed.
    //
    // Arguments:
    //    lcl - the GT_LCL_VAR of the current local.
    //
    // Returns:
    //    true if the expression is a last use of the local; otherwise false.
    //
    bool IsLastUse(GenTreeLclVar* lcl)
    {
        assert(lcl->OperIs(GT_LCL_VAR) && (lcl->GetLclNum() == m_lclNum));

        LclVarDsc*   dsc        = m_compiler->lvaGetDesc(lcl);
        GenTreeFlags deathFlags = dsc->FullDeathFlags();
        return (lcl->gtFlags & deathFlags) == deathFlags;
    }

private:
    GenTree** m_use        = nullptr;
    GenTree*  m_node       = nullptr;
    GenTree*  m_parentNode = nullptr;
    unsigned  m_lclNum;
    unsigned  m_parentLclNum = BAD_VAR_NUM;
#ifdef DEBUG
    unsigned m_useCount = 0;
#endif
    GenTreeFlags m_useFlags         = GTF_EMPTY;
    GenTreeFlags m_accumulatedFlags = GTF_EMPTY;
    // Precise exceptions thrown by the nodes that were visited so far. Note
    // that we stop updating this field once we find that two or more separate
    // exceptions.
    ExceptionSetFlags m_accumulatedExceptions = ExceptionSetFlags::None;
    ExceptionSetFlags m_useExceptions         = ExceptionSetFlags::None;
    unsigned          m_treeSize              = 0;
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
    GenTree* const defNode = stmt->GetRootNode();

    if (!defNode->OperIs(GT_STORE_LCL_VAR))
    {
        return false;
    }

    JITDUMP("    [%06u]: ", dspTreeID(defNode))

    unsigned const   lclNum = defNode->AsLclVarCommon()->GetLclNum();
    LclVarDsc* const varDsc = lvaGetDesc(lclNum);

    // Leave pinned locals alone.
    // This is just a perf opt -- we shouldn't find any uses.
    //
    if (varDsc->lvPinned)
    {
        JITDUMP(" pinned local\n");
        return false;
    }

    // Cannot forward sub without liveness information.
    //
    assert(fgDidEarlyLiveness);

    // And local is unalised
    //
    if (varDsc->IsAddressExposed())
    {
        JITDUMP(" not store (unaliased single-use lcl)\n");
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
    GenTree* fwdSubNode = defNode->AsLclVarCommon()->Data();

    // Can't substitute GT_CATCH_ARG.
    // Can't substitute GT_LCLHEAP.
    //
    // Don't substitute a no return call (trips up morph in some cases).
    if (fwdSubNode->OperIs(GT_CATCH_ARG, GT_LCLHEAP))
    {
        JITDUMP(" tree to sub is catch arg, or lcl heap\n");
        return false;
    }

    if (fwdSubNode->IsCall() && fwdSubNode->AsCall()->IsNoReturn())
    {
        JITDUMP(" tree to sub is a 'no return' call\n");
        return false;
    }

    // Bail if sub node has embedded stores.
    //
    if ((fwdSubNode->gtFlags & GTF_ASG) != 0)
    {
        JITDUMP(" tree to sub has effects\n");
        return false;
    }

    // Bail if sub node has mismatched types.
    // Might be able to tolerate these by retyping.
    //
    if (defNode->TypeGet() != fwdSubNode->TypeGet())
    {
        JITDUMP(" mismatched types (store)\n");
        return false;
    }

    // Local and tree to substitute seem suitable.
    // See if the next statement contains the one and only use.
    //
    Statement* const nextStmt = stmt->GetNextStmt();

    ForwardSubVisitor fsv(this, lclNum);
    // Do a quick scan through the linked locals list to see if there is a last use.
    bool found = false;
    for (GenTreeLclVarCommon* lcl : nextStmt->LocalsTreeList())
    {
        if (lcl->OperIs(GT_LCL_VAR) && (lcl->GetLclNum() == lclNum))
        {
            if (fsv.IsLastUse(lcl->AsLclVar()))
            {
                found = true;
                break;
            }
        }

        if (fsv.IsUse(lcl))
        {
            JITDUMP(" next stmt has non-last use\n");
            return false;
        }
    }

    if (!found)
    {
        JITDUMP(" no next stmt use\n");
        return false;
    }

    // Don't fwd sub overly large trees.
    // Size limit here is ad-hoc. Need to tune.
    //
    // Consider instead using the height of the fwdSubNode.
    //
    unsigned const nodeLimit = 16;

    if (gtComplexityExceeds(fwdSubNode, nodeLimit))
    {
        JITDUMP(" tree to sub has more than %u nodes\n", nodeLimit);
        return false;
    }

    // We often see stale flags, eg call flags after inlining.
    // Try and clean these up.
    //
    gtUpdateStmtSideEffects(nextStmt);
    gtUpdateStmtSideEffects(stmt);

    // Scan for the (last) use.
    //
    fsv.WalkTree(nextStmt->GetRootNodePointer(), nullptr);

    // The visitor has more contextual information and may not actually deem
    // the use we found above as a valid forward sub destination so we must
    // recheck it here.
    if (fsv.GetNode() == nullptr)
    {
        JITDUMP(" no next stmt use\n");
        return false;
    }

    JITDUMP(" [%06u] is last use of [%06u] (V%02u) ", dspTreeID(fsv.GetNode()), dspTreeID(defNode), lclNum);

    // Qmarks must replace top-level uses. Also, restrict to STORE_LCL_VAR.
    // And also to where neither local is normalize on store, otherwise
    // something downstream may add a cast over the qmark.
    //
    GenTree* const nextRootNode = nextStmt->GetRootNode();
    if (fwdSubNode->OperIs(GT_QMARK))
    {
        if ((fsv.GetParentNode() != nextRootNode) || !nextRootNode->OperIs(GT_STORE_LCL_VAR))
        {
            JITDUMP(" can't fwd sub qmark as use is not top level STORE_LCL_VAR\n");
            return false;
        }

        if (varDsc->lvNormalizeOnStore())
        {
            JITDUMP(" can't fwd sub qmark as V%02u is normalize on store\n", lclNum);
            return false;
        }

        const unsigned   dstLclNum = nextRootNode->AsLclVarCommon()->GetLclNum();
        LclVarDsc* const dstVarDsc = lvaGetDesc(dstLclNum);

        if (dstVarDsc->lvNormalizeOnStore())
        {
            JITDUMP(" can't fwd sub qmark as V%02u is normalize on store\n", dstLclNum);
            return false;
        }
    }

    // If next statement already has a large tree, hold off
    // on making it even larger.
    //
    // We use total node count. Consider instead using the depth of the use and the
    // height of the fwdSubNode.
    //
    unsigned const nextTreeLimit = 200;
    if ((fsv.GetComplexity() > nextTreeLimit) && gtComplexityExceeds(fwdSubNode, 1))
    {
        JITDUMP(" next stmt tree is too large (%u)\n", fsv.GetComplexity());
        return false;
    }

    // Next statement seems suitable.
    // See if we can forward sub without changing semantics.
    //
    if (genActualType(fsv.GetNode()) != genActualType(fwdSubNode))
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
    if (((fwdSubNode->gtFlags & GTF_CALL) != 0) && ((fsv.GetFlags() & GTF_ALL_EFFECT) != 0))
    {
        JITDUMP(" cannot reorder call with any side effect\n");
        return false;
    }
    if (((fwdSubNode->gtFlags & GTF_GLOB_REF) != 0) && ((fsv.GetFlags() & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
    {
        JITDUMP(" cannot reorder global reference with persistent side effects\n");
        return false;
    }
    if (((fwdSubNode->gtFlags & GTF_ORDER_SIDEEFF) != 0) &&
        ((fsv.GetFlags() & (GTF_GLOB_REF | GTF_ORDER_SIDEEFF)) != 0))
    {
        JITDUMP(" cannot reorder ordering side effect with global reference/ordering side effect\n");
        return false;
    }
    if ((fwdSubNode->gtFlags & GTF_EXCEPT) != 0)
    {
        if ((fsv.GetFlags() & GTF_PERSISTENT_SIDE_EFFECTS) != 0)
        {
            JITDUMP(" cannot reorder exception with persistent side effect\n");
            return false;
        }

        if ((fsv.GetFlags() & GTF_EXCEPT) != 0)
        {
            assert(fsv.GetExceptions() != ExceptionSetFlags::None);
            if (genCountBits(static_cast<uint32_t>(fsv.GetExceptions())) > 1)
            {
                JITDUMP(" cannot reorder different thrown exceptions\n");
                return false;
            }

            ExceptionSetFlags fwdSubNodeExceptions = gtCollectExceptions(fwdSubNode);
            assert(fwdSubNodeExceptions != ExceptionSetFlags::None);
            if (fwdSubNodeExceptions != fsv.GetExceptions())
            {
                JITDUMP(" cannot reorder different thrown exceptions\n");
                return false;
            }
        }
    }

    // If we're relying on purity of fwdSubNode for legality of forward sub,
    // do some extra checks for global uses that might not be reflected in the flags.
    //
    // TODO: remove this once we can trust upstream phases and/or gtUpdateStmtSideEffects
    // to set GTF_GLOB_REF properly.
    //
    if ((fsv.GetFlags() & GTF_PERSISTENT_SIDE_EFFECTS) != 0)
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

    // Quirks:
    //
    // Don't substitute nodes args morphing doesn't handle into struct args.
    //
    if (fsv.IsCallArg() && fsv.GetNode()->TypeIs(TYP_STRUCT) &&
        !fwdSubNode->OperIs(GT_BLK, GT_LCL_VAR, GT_LCL_FLD, GT_MKREFANY))
    {
        JITDUMP(" use is a struct arg; fwd sub node is not OBJ/LCL_VAR/LCL_FLD/MKREFANY\n");
        return false;
    }

    // A "CanBeReplacedWithItsField" SDSU can serve as a sort of "BITCAST<primitive>(struct)"
    // device, forwarding it risks forcing things to memory.
    //
    if (fwdSubNode->IsCall() && varDsc->CanBeReplacedWithItsField(this))
    {
        JITDUMP(" fwd sub local is 'CanBeReplacedWithItsField'\n");
        return false;
    }

    // There are implicit assumptions downstream on where/how multi-reg ops
    // can appear.
    //
    // Eg if fwdSubNode is a multi-reg call, parent node must be STORE_LCL_VAR
    // and the local being defined must be specially marked up.
    //
    if (varTypeIsStruct(fwdSubNode) && fwdSubNode->IsMultiRegNode())
    {
        GenTree* const parentNode = fsv.GetParentNode();

        if (!parentNode->OperIs(GT_STORE_LCL_VAR))
        {
            JITDUMP(" multi-reg struct node, parent not STORE_LCL_VAR\n");
            return false;
        }

        unsigned const   dstLclNum = parentNode->AsLclVar()->GetLclNum();
        LclVarDsc* const dstVarDsc = lvaGetDesc(dstLclNum);

        JITDUMP(" [marking V%02u as multi-reg-ret]", dstLclNum);
        dstVarDsc->lvIsMultiRegRet = true;
    }

    // If a method returns a multi-reg type, only forward sub locals,
    // and ensure the local and operand have the required markup.
    // (see eg impFixupStructReturnType).
    //
    // TODO-Cleanup: this constraint only exists for multi-reg **struct**
    // returns, it does not exist for LONGs. However, enabling substitution
    // for them on all 32 bit targets is a CQ regression due to some bad
    // interaction between decomposition and RA.
    //
    if (compMethodReturnsMultiRegRetType() && fsv.GetParentNode()->OperIs(GT_RETURN))
    {
#if defined(TARGET_X86)
        if (fwdSubNode->TypeGet() == TYP_LONG)
        {
            JITDUMP(" TYP_LONG fwd sub node, target is x86\n");
            return false;
        }
#endif // defined(TARGET_X86)

        if (!fwdSubNode->OperIs(GT_LCL_VAR))
        {
#ifdef TARGET_ARM
            if (!fwdSubNode->TypeIs(TYP_LONG))
#endif // TARGET_ARM
            {
                JITDUMP(" parent is multi-reg struct return, fwd sub node is not lcl var\n");
                return false;
            }
        }
        else if (varTypeIsStruct(fwdSubNode))
        {
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
            fwdSubNodeLocal->gtFlags |= GTF_DONT_CSE;
        }
    }

    // If the value is being roundtripped through a small-typed local then we
    // may need to insert an explicit cast to emulate normalize-on-load/store.
    //
    if (varTypeIsSmall(varDsc) && fgCastNeeded(fwdSubNode, varDsc->TypeGet()))
    {
        JITDUMP(" [adding cast for small-typed local]");
        fwdSubNode = gtNewCastNode(TYP_INT, fwdSubNode, false, varDsc->TypeGet());
    }

    // Looks good, forward sub!
    //
    GenTree**            use    = fsv.GetUse();
    GenTreeLclVarCommon* useLcl = (*use)->AsLclVarCommon();
    *use                        = fwdSubNode;

    // We expect the last local in the statement is the defined local and
    // replace the use of it with the rest from the statement.
    assert(defNode->gtNext == nullptr);

    GenTreeLclVarCommon* firstLcl = *stmt->LocalsTreeList().begin();

    if (firstLcl == defNode)
    {
        nextStmt->LocalsTreeList().Remove(useLcl);
    }
    else
    {
        nextStmt->LocalsTreeList().Replace(useLcl, useLcl, firstLcl, defNode->gtPrev->AsLclVarCommon());

        fgForwardSubUpdateLiveness(firstLcl, defNode->gtPrev);
    }

    if ((fwdSubNode->gtFlags & GTF_ALL_EFFECT) != 0)
    {
        gtUpdateStmtSideEffects(nextStmt);
    }

    JITDUMP(" -- fwd subbing [%06u]; new next stmt is\n", dspTreeID(fwdSubNode));
    DISPSTMT(nextStmt);

    return true;
}

//------------------------------------------------------------------------
// fgForwardSubUpdateLiveness: correct liveness after performing a forward
// substitution that added a new sub list of locals in a statement.
//
// Arguments:
//    newSubListFirst - the first local in the new sub list.
//    newSubListLast - the last local in the new sub list.
//
// Remarks:
//    Forward substitution may add new uses of other locals; these may be
//    inserted at arbitrary points in the statement, so previous last uses may
//    be invalidated. This function will conservatively unmark last uses that
//    may no longer be correct.
//
//    The function is not as precise as it could be, in particular it does not
//    mark any of the new later uses as a last use, and it does not care about
//    defs. However, currently the only user of last use information after
//    forward sub is last-use copy omission, and diffs indicate that being
//    conservative here does not have a large impact.
//
void Compiler::fgForwardSubUpdateLiveness(GenTree* newSubListFirst, GenTree* newSubListLast)
{
    for (GenTree* node = newSubListFirst->gtPrev; node != nullptr; node = node->gtPrev)
    {
        if ((node->gtFlags & GTF_VAR_DEATH_MASK) == 0)
        {
            continue;
        }

        unsigned   lclNum = node->AsLclVarCommon()->GetLclNum();
        LclVarDsc* dsc    = lvaGetDesc(lclNum);

        unsigned parentLclNum = dsc->lvIsStructField ? dsc->lvParentLcl : BAD_VAR_NUM;

        GenTree* candidate = newSubListFirst;
        while (true)
        {
            unsigned newUseLclNum = candidate->AsLclVarCommon()->GetLclNum();
            if (dsc->lvPromoted)
            {
                // Is the parent struct being used?
                if (newUseLclNum == lclNum)
                {
                    // Then all fields are not dying.
                    node->gtFlags &= ~GTF_VAR_DEATH_MASK;
                    break;
                }

                // Otherwise, is one single field being used?
                if ((newUseLclNum >= dsc->lvFieldLclStart) && (newUseLclNum < dsc->lvFieldLclStart + dsc->lvFieldCnt))
                {
                    node->ClearLastUse(newUseLclNum - dsc->lvFieldLclStart);

                    if ((node->gtFlags & GTF_VAR_DEATH_MASK) == 0)
                    {
                        break;
                    }
                }
            }
            else
            {
                // See if a new instance of this local or its parent appeared.
                if ((newUseLclNum == lclNum) || (newUseLclNum == parentLclNum))
                {
                    node->gtFlags &= ~GTF_VAR_DEATH;
                    break;
                }
            }

            if (candidate == newSubListLast)
            {
                break;
            }

            candidate = candidate->gtNext;
        }
    }
}
