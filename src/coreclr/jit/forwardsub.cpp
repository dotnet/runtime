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
// before doing the more costly bits of analysis. We use the linked lists
// of locals to quickly find out if there is a candidate last use.
//
// The block walk maintains a set of candidate def statements. For each
// statement we check if any candidate's local has a last use there and
// attempt substitution. After a successful substitution we re-process
// the same statement to handle cascading cases like:
//
// Statement(n):
//    lcl1 = tree1
// Statement(n+1):
//    lcl2 = tree2
// Statement(n+2):
//    use ...  lcl1 ... use ... lcl2 ...
//
// If we can forward sub tree2, then the def and use of lcl1 become
// adjacent and we can try to sub tree1 as well.
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

    ForwardSubVisitor(Compiler* compiler, unsigned lclNum)
        : GenTreeVisitor(compiler)
        , m_lclNum(lclNum)
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
                if ((parent != nullptr) && parent->IsCall())
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
        if ((node->gtFlags & GTF_EXCEPT) != 0)
        {
            // We can never reorder in the face of different or unknown
            // exception types, so stop calling 'OperExceptions' once we've
            // seen more than one different exception type.
            if ((genCountBits(static_cast<uint32_t>(m_accumulatedExceptions)) <= 1) &&
                ((m_accumulatedExceptions & ExceptionSetFlags::UnknownException) == ExceptionSetFlags::None))
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
        return (m_parentNode != nullptr) && m_parentNode->IsCall();
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

    EffectsVisitor(Compiler* compiler)
        : GenTreeVisitor<EffectsVisitor>(compiler)
        , m_flags(GTF_EMPTY)
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

    GenTreeFlags GetFlags() const
    {
        return m_flags;
    }

private:
    GenTreeFlags m_flags;
};

//------------------------------------------------------------------------
// ForwardSubCandidate: tracks a def statement that may be forward
// substituted into a later statement.
//
struct ForwardSubCandidate
{
    Statement*        defStmt;
    unsigned          lclNum;
    GenTree*          fwdSubNode;
    GenTreeFlags      fwdSubFlags;
    GenTreeFlags      intermediateFlags;
    ExceptionSetFlags intermediateExcepts;
};

//------------------------------------------------------------------------
// fgForwardSubIsDefCandidate: check if a statement is a valid forward
// sub candidate (def-side checks only).
//
// Arguments:
//    stmt       - statement to check
//    fwdSubNode - [out] the tree to substitute if this is a candidate
//    lclNum     - [out] the local being defined
//
// Returns:
//    true if the statement is a valid forward sub def candidate.
//
bool Compiler::fgForwardSubIsDefCandidate(Statement* stmt, GenTree** fwdSubNode, unsigned* lclNum)
{
    GenTree* const defNode = stmt->GetRootNode();

    if (!defNode->OperIs(GT_STORE_LCL_VAR))
    {
        return false;
    }

    // Clean up stale flags before checking.
    //
    gtUpdateStmtSideEffects(stmt);

    JITDUMP("    [%06u]: ", dspTreeID(defNode))

    *lclNum                 = defNode->AsLclVarCommon()->GetLclNum();
    LclVarDsc* const varDsc = lvaGetDesc(*lclNum);

    if (varDsc->lvPinned)
    {
        JITDUMP(" pinned local\n");
        return false;
    }

    if (varDsc->IsAddressExposed())
    {
        JITDUMP(" address-exposed local\n");
        return false;
    }

    if (lvaIsImplicitByRefLocal(*lclNum))
    {
        JITDUMP(" implicit by-ref local\n");
        return false;
    }

    GenTree* node = defNode->AsLclVarCommon()->Data();

    if (node->OperIs(GT_CATCH_ARG, GT_LCLHEAP, GT_ASYNC_CONTINUATION))
    {
        JITDUMP(" tree to sub is %s\n", GenTree::OpName(node->OperGet()));
        return false;
    }

    if (gtTreeContainsAsyncCall(node))
    {
        JITDUMP(" tree has an async call\n");
        return false;
    }

    if ((node->gtFlags & GTF_ASG) != 0)
    {
        JITDUMP(" tree to sub has effects\n");
        return false;
    }

    if (genActualType(defNode->TypeGet()) != genActualType(node->TypeGet()))
    {
        JITDUMP(" mismatched types (store)\n");
        return false;
    }

    // If fwdSubNode is an address-exposed local, forwarding it may lose
    // optimizations due to GLOB_REF "poisoning" the tree. CQ analysis shows
    // this to not be a problem with structs.
    if (node->OperIs(GT_LCL_VAR))
    {
        unsigned const   fwdLclNum = node->AsLclVarCommon()->GetLclNum();
        LclVarDsc* const fwdVarDsc = lvaGetDesc(fwdLclNum);

        if (!varTypeIsStruct(fwdVarDsc) && fwdVarDsc->IsAddressExposed())
        {
            JITDUMP(" V%02u is address exposed\n", fwdLclNum);
            return false;
        }
    }

    // A "CanBeReplacedWithItsField" SDSU can serve as a sort of
    // "BITCAST<primitive>(struct)" device, forwarding it risks forcing
    // things to memory.
    if (node->IsCall() && varDsc->CanBeReplacedWithItsField(this))
    {
        JITDUMP(" fwd sub local is 'CanBeReplacedWithItsField'\n");
        return false;
    }

    unsigned const nodeLimit = 16;
    auto           countNode = [](GenTree* tree) -> unsigned {
        return 1;
    };

    if (gtComplexityExceeds(node, nodeLimit, countNode))
    {
        JITDUMP(" tree to sub has more than %u nodes\n", nodeLimit);
        return false;
    }

    JITDUMP(" fwd sub candidate for V%02u\n", *lclNum);
    *fwdSubNode = node;
    return true;
}

//------------------------------------------------------------------------
// fgForwardSubCanReorderPast: check if a forward sub candidate can be
// safely reordered past a statement's effects.
//
// Arguments:
//    candidate   - the forward sub candidate
//    stmtFlags   - the effects flags of the statement to reorder past
//
// Returns:
//    true if the candidate can be safely reordered past the statement.
//
// static
bool Compiler::fgForwardSubCanReorderPast(ForwardSubCandidate* candidate, GenTreeFlags stmtFlags)
{
    GenTreeFlags const fwdFlags = candidate->fwdSubFlags;

    if (((fwdFlags & GTF_CALL) != 0) && ((stmtFlags & GTF_ALL_EFFECT) != 0))
    {
        return false;
    }
    if (((stmtFlags & GTF_CALL) != 0) && ((fwdFlags & GTF_ALL_EFFECT) != 0))
    {
        return false;
    }
    if (((fwdFlags & GTF_GLOB_REF) != 0) && ((stmtFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
    {
        return false;
    }
    if (((stmtFlags & GTF_GLOB_REF) != 0) && ((fwdFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
    {
        return false;
    }
    if (((fwdFlags & GTF_ORDER_SIDEEFF) != 0) && ((stmtFlags & (GTF_GLOB_REF | GTF_ORDER_SIDEEFF)) != 0))
    {
        return false;
    }
    if (((stmtFlags & GTF_ORDER_SIDEEFF) != 0) && ((fwdFlags & (GTF_GLOB_REF | GTF_ORDER_SIDEEFF)) != 0))
    {
        return false;
    }
    if (((fwdFlags & GTF_EXCEPT) != 0) && ((stmtFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
    {
        return false;
    }
    if (((stmtFlags & GTF_EXCEPT) != 0) && ((fwdFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
    {
        return false;
    }
    if (((fwdFlags & GTF_EXCEPT) != 0) && ((stmtFlags & GTF_EXCEPT) != 0))
    {
        return false;
    }

    return true;
}

//------------------------------------------------------------------------
// fgForwardSubHasStoreInterferenceWithCandidate: check if any stores in
// a statement conflict with locals read by the forward sub candidate.
//
// Arguments:
//    candidate - the forward sub candidate
//    stmt      - the statement to check for conflicting stores
//
// Returns:
//    true if there is interference.
//
bool Compiler::fgForwardSubHasStoreInterferenceWithCandidate(ForwardSubCandidate* candidate, Statement* stmt)
{
    Statement*           defStmt = candidate->defStmt;
    GenTreeLclVarCommon* defNode = defStmt->GetRootNode()->AsLclVarCommon();

    for (GenTreeLclVarCommon* defStmtLcl : defStmt->LocalsTreeList())
    {
        if (defStmtLcl == defNode)
        {
            break;
        }

        unsigned   defStmtLclNum       = defStmtLcl->GetLclNum();
        LclVarDsc* defStmtLclDsc       = lvaGetDesc(defStmtLclNum);
        unsigned   defStmtParentLclNum = BAD_VAR_NUM;
        if (defStmtLclDsc->lvIsStructField)
        {
            defStmtParentLclNum = defStmtLclDsc->lvParentLcl;
        }

        for (GenTreeLclVarCommon* stmtLcl : stmt->LocalsTreeList())
        {
            if (!stmtLcl->OperIsLocalStore())
            {
                continue;
            }

            if ((stmtLcl->GetLclNum() == defStmtLclNum) || (stmtLcl->GetLclNum() == defStmtParentLclNum))
            {
                return true;
            }

            // Check if the store target is a field of a local read by the candidate.
            LclVarDsc* const stmtLclDsc = lvaGetDesc(stmtLcl->GetLclNum());
            if (stmtLclDsc->lvIsStructField && (stmtLclDsc->lvParentLcl == defStmtLclNum))
            {
                return true;
            }
        }
    }

    return false;
}

//------------------------------------------------------------------------
// fgForwardSubStatement: forward substitute a candidate's computation
// into a use statement, if legal and profitable
//
// Arguments:
//    candidate - the forward sub candidate
//    useStmt   - the statement containing the use
//
// Returns:
//    true if substitution was performed.
//    caller is responsible for removing the now-dead def statement.
//
bool Compiler::fgForwardSubStatement(ForwardSubCandidate* candidate, Statement* useStmt)
{
    Statement* const defStmt    = candidate->defStmt;
    GenTree* const   defNode    = defStmt->GetRootNode();
    unsigned const   lclNum     = candidate->lclNum;
    LclVarDsc* const varDsc     = lvaGetDesc(lclNum);
    GenTree*         fwdSubNode = candidate->fwdSubNode;

    JITDUMP("  Trying to fwd sub [%06u] (V%02u) into [%06u]\n", dspTreeID(defNode), lclNum,
            dspTreeID(useStmt->GetRootNode()));

    // We often see stale flags, eg call flags after inlining.
    // Try and clean these up.
    //
    gtUpdateStmtSideEffects(useStmt);
    gtUpdateStmtSideEffects(defStmt);

    // Scan for the (last) use.
    //
    ForwardSubVisitor fsv(this, lclNum);
    fsv.WalkTree(useStmt->GetRootNodePointer(), nullptr);

    // The visitor may not find a valid forward sub destination.
    if (fsv.GetNode() == nullptr)
    {
        JITDUMP("    no valid use found in use stmt\n");
        return false;
    }

    JITDUMP("    [%06u] is last use of [%06u] (V%02u) ", dspTreeID(fsv.GetNode()), dspTreeID(defNode), lclNum);

    // Qmarks must replace top-level uses. Also, restrict to STORE_LCL_VAR.
    // And also to where neither local is normalize on store, otherwise
    // something downstream may add a cast over the qmark.
    //
    GenTree* const useRootNode = useStmt->GetRootNode();
    if (fwdSubNode->OperIs(GT_QMARK))
    {
        if ((fsv.GetParentNode() != useRootNode) || !useRootNode->OperIs(GT_STORE_LCL_VAR))
        {
            JITDUMP(" can't fwd sub qmark as use is not top level STORE_LCL_VAR\n");
            return false;
        }

        if (varDsc->lvNormalizeOnStore())
        {
            JITDUMP(" can't fwd sub qmark as V%02u is normalize on store\n", lclNum);
            return false;
        }

        const unsigned   dstLclNum = useRootNode->AsLclVarCommon()->GetLclNum();
        LclVarDsc* const dstVarDsc = lvaGetDesc(dstLclNum);

        if (dstVarDsc->lvNormalizeOnStore())
        {
            JITDUMP(" can't fwd sub qmark as V%02u is normalize on store\n", dstLclNum);
            return false;
        }
    }

    // If use statement already has a large tree, hold off
    // on making it even larger.
    //
    auto countNode = [](GenTree* tree) -> unsigned {
        return 1;
    };

    unsigned const nextTreeLimit = 200;
    if ((fsv.GetComplexity() > nextTreeLimit) && gtComplexityExceeds(fwdSubNode, 1, countNode))
    {
        JITDUMP(" use stmt tree is too large (%u)\n", fsv.GetComplexity());
        return false;
    }

    // See if we can forward sub without changing semantics.
    //
    if (genActualType(fsv.GetNode()) != genActualType(fwdSubNode))
    {
        JITDUMP(" mismatched types (substitution)\n");
        return false;
    }

    // The effects we need to check against include both the effects within
    // the use statement (before the use point) and any accumulated
    // intermediate effects from statements between the def and use.
    //
    GenTreeFlags const      useFlags   = fsv.GetFlags() | candidate->intermediateFlags;
    ExceptionSetFlags const useExcepts = fsv.GetExceptions() | candidate->intermediateExcepts;

    if (((useFlags & GTF_ASG) != 0) && fgForwardSubHasStoreInterference(defStmt, useStmt, fsv.GetNode()))
    {
        JITDUMP(" cannot reorder with potential interfering store\n");
        return false;
    }
    if (((fwdSubNode->gtFlags & GTF_CALL) != 0) && ((useFlags & GTF_ALL_EFFECT) != 0))
    {
        JITDUMP(" cannot reorder call with any side effect\n");
        return false;
    }
    if (((fwdSubNode->gtFlags & GTF_GLOB_REF) != 0) && ((useFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0))
    {
        JITDUMP(" cannot reorder global reference with persistent side effects\n");
        return false;
    }
    if (((fwdSubNode->gtFlags & GTF_ORDER_SIDEEFF) != 0) && ((useFlags & (GTF_GLOB_REF | GTF_ORDER_SIDEEFF)) != 0))
    {
        JITDUMP(" cannot reorder ordering side effect with global reference/ordering side effect\n");
        return false;
    }
    if ((fwdSubNode->gtFlags & GTF_EXCEPT) != 0)
    {
        if ((useFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0)
        {
            JITDUMP(" cannot reorder exception with persistent side effect\n");
            return false;
        }

        if ((useFlags & GTF_EXCEPT) != 0)
        {
            if ((genCountBits(static_cast<uint32_t>(useExcepts)) > 1) ||
                (((useExcepts & ExceptionSetFlags::UnknownException) != ExceptionSetFlags::None)))
            {
                JITDUMP(" cannot reorder different/unknown thrown exceptions\n");
                return false;
            }

            ExceptionSetFlags fwdSubNodeExceptions = gtCollectExceptions(fwdSubNode);
            assert(fwdSubNodeExceptions != ExceptionSetFlags::None);
            if (fwdSubNodeExceptions != useExcepts)
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
    if ((useFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0)
    {
        EffectsVisitor ev(this);
        ev.WalkTree(&fwdSubNode, nullptr);

        if ((ev.GetFlags() & GTF_GLOB_REF) != 0)
        {
            JITDUMP(" potentially interacting effects (AX locals)\n");
            return false;
        }
    }

    // Quirks:
    //
    // Don't substitute nodes args morphing doesn't handle into struct args.
    //
    if (fsv.IsCallArg() && fsv.GetNode()->TypeIs(TYP_STRUCT) && !fwdSubNode->OperIs(GT_BLK, GT_LCL_VAR, GT_LCL_FLD))
    {
        JITDUMP(" use is a struct arg; fwd sub node is not BLK/LCL_VAR/LCL_FLD\n");
        return false;
    }

    // There are implicit assumptions downstream on where/how multi-reg ops
    // can appear.
    //
    if (varTypeIsStruct(fwdSubNode) && fwdSubNode->IsMultiRegNode())
    {
        GenTree* const parentNode = fsv.GetParentNode();

        if ((parentNode == nullptr) || !parentNode->OperIs(GT_STORE_LCL_VAR))
        {
            JITDUMP(" multi-reg struct node, parent not STORE_LCL_VAR\n");
            return false;
        }

        unsigned const   dstLclNum = parentNode->AsLclVar()->GetLclNum();
        LclVarDsc* const dstVarDsc = lvaGetDesc(dstLclNum);

        JITDUMP(" [marking V%02u as multi-reg-dest]", dstLclNum);
        dstVarDsc->SetIsMultiRegDest();
    }

    // If a method returns a multi-reg type, only forward sub locals,
    // and ensure the local and operand have the required markup.
    //
    if (compMethodReturnsMultiRegRetType() && (fsv.GetParentNode() != nullptr) &&
        fsv.GetParentNode()->OperIs(GT_RETURN, GT_SWIFT_ERROR_RET))
    {
#if defined(TARGET_X86)
        if (fwdSubNode->TypeIs(TYP_LONG))
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

            if (lvaIsImplicitByRefLocal(fwdLclNum))
            {
                JITDUMP(" parent is multi-reg return; fwd sub node is implicit byref\n");
                return false;
            }

            LclVarDsc* const fwdVarDsc = lvaGetDesc(fwdLclNum);

            JITDUMP(" [marking V%02u as multi-reg-ret]", fwdLclNum);
            // TODO-Quirk: Only needed for heuristics
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

    assert(defNode->gtNext == nullptr);

    GenTreeLclVarCommon* firstLcl = *defStmt->LocalsTreeList().begin();

    if (firstLcl == defNode)
    {
        useStmt->LocalsTreeList().Remove(useLcl);
    }
    else
    {
        useStmt->LocalsTreeList().Replace(useLcl, useLcl, firstLcl, defNode->gtPrev->AsLclVarCommon());

        fgForwardSubUpdateLiveness(firstLcl, defNode->gtPrev);
    }

    if ((fwdSubNode->gtFlags & GTF_ALL_EFFECT) != 0)
    {
        gtUpdateStmtSideEffects(useStmt);
    }

    JITDUMP(" -- fwd subbing [%06u]; new use stmt is\n", dspTreeID(fwdSubNode));
    DISPSTMT(useStmt);

    return true;
}

//------------------------------------------------------------------------
// fgForwardSubStatement: convenience wrapper that tries to forward
// substitute a def statement into the immediately following statement.
//
// Arguments:
//    stmt - the def statement
//
// Returns:
//    true if substitution was performed.
//
bool Compiler::fgForwardSubStatement(Statement* stmt)
{
    GenTree* fwdSubNode = nullptr;
    unsigned lclNum     = BAD_VAR_NUM;

    if (!fgForwardSubIsDefCandidate(stmt, &fwdSubNode, &lclNum))
    {
        return false;
    }

    Statement* const nextStmt = stmt->GetNextStmt();
    if (nextStmt == nullptr)
    {
        return false;
    }

    // Quick scan for a last use in the next statement.
    //
    ForwardSubVisitor fsv(this, lclNum);
    bool              found = false;
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
            return false;
        }
    }

    if (!found)
    {
        return false;
    }

    ForwardSubCandidate candidate;
    candidate.defStmt             = stmt;
    candidate.lclNum              = lclNum;
    candidate.fwdSubNode          = fwdSubNode;
    candidate.fwdSubFlags         = fwdSubNode->gtFlags;
    candidate.intermediateFlags   = GTF_EMPTY;
    candidate.intermediateExcepts = ExceptionSetFlags::None;

    return fgForwardSubStatement(&candidate, nextStmt);
}

//------------------------------------------------------------------------
// fgForwardSubHasStoreInterference: Check if a forward sub candidate
// interferes with stores in the statement it may be substituted into.
//
// Arguments:
//    defStmt     - The statement with the def
//    nextStmt    - The statement that is being substituted into
//    nextStmtUse - Use of the local being substituted in the next statement
//
// Returns:
//   True if there is interference.
//
// Remarks:
//   We expect the caller to have checked for GTF_ASG before doing the precise
//   check here.
//
bool Compiler::fgForwardSubHasStoreInterference(Statement* defStmt, Statement* nextStmt, GenTree* nextStmtUse)
{
    assert(defStmt->GetRootNode()->OperIsLocalStore());
    assert(nextStmtUse->OperIsLocalRead());

    GenTreeLclVarCommon* defNode = defStmt->GetRootNode()->AsLclVarCommon();

    for (GenTreeLclVarCommon* defStmtLcl : defStmt->LocalsTreeList())
    {
        if (defStmtLcl == defNode)
        {
            break;
        }

        unsigned   defStmtLclNum       = defStmtLcl->GetLclNum();
        LclVarDsc* defStmtLclDsc       = lvaGetDesc(defStmtLclNum);
        unsigned   defStmtParentLclNum = BAD_VAR_NUM;
        if (defStmtLclDsc->lvIsStructField)
        {
            defStmtParentLclNum = defStmtLclDsc->lvParentLcl;
        }

        for (GenTreeLclVarCommon* useStmtLcl : nextStmt->LocalsTreeList())
        {
            if (useStmtLcl == nextStmtUse)
            {
                break;
            }

            if (!useStmtLcl->OperIsLocalStore())
            {
                continue;
            }

            if ((useStmtLcl->GetLclNum() == defStmtLclNum) || (useStmtLcl->GetLclNum() == defStmtParentLclNum))
            {
                return true;
            }

            // Check if the store target is a field of a local read by the candidate.
            LclVarDsc* const useStmtLclDsc = lvaGetDesc(useStmtLcl->GetLclNum());
            if (useStmtLclDsc->lvIsStructField && (useStmtLclDsc->lvParentLcl == defStmtLclNum))
            {
                return true;
            }
        }
    }

    return false;
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

//------------------------------------------------------------------------
// fgForwardSubBlock: run forward substitution in this block
//
// Arguments:
//    block -- block to process
//
// Returns:
//    true if any IR was modified
//
// Remarks:
//    Walks statements first to last, maintaining a candidate set of def
//    statements whose trees may be forward substituted into later
//    statements. For each statement we check for last uses of candidate
//    locals, attempt substitution, remove interfered candidates, and
//    add new candidates.
//
bool Compiler::fgForwardSubBlock(BasicBlock* block)
{
    bool changed = false;

    ArrayStack<ForwardSubCandidate> candidates(getAllocator(CMK_Generic));

    for (Statement* stmt = block->firstStmt(); stmt != nullptr;)
    {
        Statement* const nextStmt = stmt->GetNextStmt();

        // Try to forward sub any candidate whose local has a last use
        // in this statement.
        //
        bool substituted = false;
        for (int i = 0; i < candidates.Height(); i++)
        {
            ForwardSubCandidate* candidate = &candidates.BottomRef(i);
            unsigned const       candLcl   = candidate->lclNum;

            // Quick scan through the linked locals list for a last use.
            //
            ForwardSubVisitor fsv(this, candLcl);
            bool              hasLastUse = false;
            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                if (lcl->OperIs(GT_LCL_VAR) && (lcl->GetLclNum() == candLcl))
                {
                    if (fsv.IsLastUse(lcl->AsLclVar()))
                    {
                        hasLastUse = true;
                        break;
                    }
                }

                if (fsv.IsUse(lcl))
                {
                    // Non-last use: this candidate can never succeed.
                    hasLastUse = false;
                    break;
                }
            }

            if (!hasLastUse)
            {
                continue;
            }

            // Found a last use. Attempt the substitution.
            //
            if (fgForwardSubStatement(candidate, stmt))
            {
                fgRemoveStmt(block, candidate->defStmt);
                changed = true;

                // Remove this candidate from the set.
                //
                candidates.BottomRef(i) = candidates.BottomRef(candidates.Height() - 1);
                candidates.Pop(1);

                // Re-process this statement since it changed.
                //
                substituted = true;
                break;
            }
            else
            {
                // Forward sub failed; this candidate won't succeed later.
                //
                JITDUMP("    fwd sub of V%02u failed, removing candidate\n", candLcl);
                candidates.BottomRef(i) = candidates.BottomRef(candidates.Height() - 1);
                candidates.Pop(1);
                i--;
            }
        }

        if (substituted)
        {
            // Stay on the same statement to handle cascading substitutions.
            //
            continue;
        }

        // No substitution happened. Check interference and remove
        // candidates that can't be reordered past this statement.
        //
        gtUpdateStmtSideEffects(stmt);
        EffectsVisitor ev(this);
        ev.WalkTree(stmt->GetRootNodePointer(), nullptr);
        GenTreeFlags const stmtFlags = ev.GetFlags();

        for (int i = 0; i < candidates.Height(); i++)
        {
            ForwardSubCandidate* candidate = &candidates.BottomRef(i);

            // If this statement uses the candidate local, the def must
            // remain alive for that use and forward sub is impossible.
            //
            bool             interferes    = false;
            unsigned const   candLcl       = candidate->lclNum;
            LclVarDsc* const candDsc       = lvaGetDesc(candLcl);
            unsigned const   candParentLcl = candDsc->lvIsStructField ? candDsc->lvParentLcl : BAD_VAR_NUM;

            for (GenTreeLclVarCommon* lcl : stmt->LocalsTreeList())
            {
                unsigned const lclNum = lcl->GetLclNum();

                if ((lclNum == candLcl) || (lclNum == candParentLcl))
                {
                    interferes = true;
                    break;
                }

                LclVarDsc* const lclDsc = lvaGetDesc(lclNum);
                if (lclDsc->lvIsStructField && (lclDsc->lvParentLcl == candLcl))
                {
                    interferes = true;
                    break;
                }
            }

            if (!interferes)
            {
                interferes = !fgForwardSubCanReorderPast(candidate, stmtFlags);
            }

            if (!interferes && ((stmtFlags & GTF_ASG) != 0))
            {
                interferes = fgForwardSubHasStoreInterferenceWithCandidate(candidate, stmt);
            }

            if (interferes)
            {
                JITDUMP("    V%02u interferes with [%06u], removing candidate\n", candidate->lclNum,
                        dspTreeID(stmt->GetRootNode()));
                candidates.BottomRef(i) = candidates.BottomRef(candidates.Height() - 1);
                candidates.Pop(1);
                i--;
            }
            else
            {
                candidate->intermediateFlags |= stmtFlags;
                if ((stmtFlags & GTF_EXCEPT) != 0)
                {
                    candidate->intermediateExcepts = ExceptionSetFlags::UnknownException;
                }
            }
        }

        // If this statement defines a local, check if it's a valid
        // forward sub candidate. Cap the candidate set size to bound
        // the O(N*M) work in the main loop.
        //
        unsigned const maxCandidates = 16;
        GenTree*       fwdSubNode    = nullptr;
        unsigned       lclNum        = BAD_VAR_NUM;

        if ((candidates.Height() < (int)maxCandidates) && fgForwardSubIsDefCandidate(stmt, &fwdSubNode, &lclNum))
        {
            ForwardSubCandidate newCandidate;
            newCandidate.defStmt             = stmt;
            newCandidate.lclNum              = lclNum;
            newCandidate.fwdSubNode          = fwdSubNode;
            newCandidate.fwdSubFlags         = fwdSubNode->gtFlags;
            newCandidate.intermediateFlags   = GTF_EMPTY;
            newCandidate.intermediateExcepts = ExceptionSetFlags::None;
            candidates.Push(newCandidate);
        }

        stmt = nextStmt;
    }

    return changed;
}
