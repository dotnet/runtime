// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "constantreuse.h"

#if defined(TARGET_ARM64) && defined(FEATURE_MASKED_HW_INTRINSICS)

namespace ConstantReuse
{

//-----------------------------------------------------------------------------
// TryGetSvePTrueOpt: Get the SVE ptrue arrangement for a SIMD base type.
//
// Arguments:
//    baseType - The SIMD base type
//    opt      - [out] The ptrue instruction option for the type
//
// Return Value:
//    True if the base type has a corresponding ptrue arrangement.
//
bool TryGetSvePTrueOpt(var_types baseType, insOpts* opt)
{
    switch (baseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
            *opt = INS_OPTS_SCALABLE_B;
            return true;

        case TYP_SHORT:
        case TYP_USHORT:
            *opt = INS_OPTS_SCALABLE_H;
            return true;

        case TYP_INT:
        case TYP_UINT:
        case TYP_FLOAT:
            *opt = INS_OPTS_SCALABLE_S;
            return true;

        case TYP_LONG:
        case TYP_ULONG:
        case TYP_DOUBLE:
            *opt = INS_OPTS_SCALABLE_D;
            return true;

        default:
            return false;
    }
}

//-----------------------------------------------------------------------------
// PTrueReusePattern: Normalize an SVE mask pattern for constant-mask reuse.
//
// Arguments:
//    pattern - The SVE mask pattern
//
// Return Value:
//    The reuse key pattern for a ptrue candidate.
//
int PTrueReusePattern(SveMaskPattern pattern)
{
    if (pattern == SveMaskPatternLargestPowerOf2)
    {
        pattern = SveMaskPatternAll;
    }

    return static_cast<int>(pattern);
}

struct ConstantMaskCandidateUse
{
    GenTree* node;
    int      groupIndex;
#ifdef DEBUG
    GenTree* user;
    bool     canReuse;
#endif
};

struct ConstantReuseGroup
{
    ConstantMaskCandidate candidate;
    unsigned              count;
    bool                  clean;
    unsigned              temp;
    unsigned              firstIndex;
    unsigned              lastIndex;
    bool                  crossesCall;
};

#ifdef DEBUG
//-----------------------------------------------------------------------------
// DumpConstantMaskCandidateKey: Dump the key for a constant-mask reuse candidate.
//
// Arguments:
//    compiler  - Compiler instance
//    candidate - The candidate key to dump
//
static void DumpConstantMaskCandidateKey(Compiler* compiler, const ConstantMaskCandidate& candidate)
{
    if (candidate.pattern == PFalseReusePattern)
    {
        JITDUMP("pfalse");
        return;
    }

    JITDUMPEXEC(compiler->GetEmitter()->emitDispArrangement(candidate.opt));
    JITDUMP(" pattern=%d", candidate.pattern);
}

//-----------------------------------------------------------------------------
// CandidateUserName: Get a display name for a candidate's user node.
//
// Arguments:
//    user - The user node, or nullptr if the candidate is unused
//
// Return Value:
//    The intrinsic or GenTree operator name to print in JIT dumps.
//
static const char* CandidateUserName(GenTree* user)
{
    if (user == nullptr)
    {
        return "<unused>";
    }

    if (user->OperIsHWIntrinsic())
    {
        return HWIntrinsicInfo::lookupName(user->AsHWIntrinsic()->GetHWIntrinsicId());
    }

    return GenTree::OpName(user->OperGet());
}

//-----------------------------------------------------------------------------
// DumpConstantMaskCandidates: Dump the collected constant-mask candidates and groups.
//
// Arguments:
//    compiler   - Compiler instance
//    block      - The block containing the candidates
//    candidates - The candidate uses found in the block
//    groups     - The reuse groups built from the candidates
//
static void DumpConstantMaskCandidates(Compiler*                             compiler,
                                       BasicBlock*                           block,
                                       ArrayStack<ConstantMaskCandidateUse>& candidates,
                                       ArrayStack<ConstantReuseGroup>&       groups)
{
    if (!compiler->verbose)
    {
        return;
    }

    JITDUMP("SVE constant mask candidates in " FMT_BB ":\n", block->bbNum);

    for (int i = 0; i < candidates.Height(); i++)
    {
        ConstantMaskCandidateUse& candidateUse = candidates.BottomRef(i);
        ConstantReuseGroup&       group        = groups.BottomRef(candidateUse.groupIndex);
        GenTree*                  user         = candidateUse.user;
        const unsigned            userId       = (user != nullptr) ? Compiler::dspTreeID(user) : 0;

        JITDUMP("  candidate ");
        JITDUMPEXEC(compiler->gtDispTree(candidateUse.node, nullptr, nullptr, true, true));
        JITDUMP("    key ");
        DumpConstantMaskCandidateKey(compiler, group.candidate);
        JITDUMP(" group=%d user=[%06u] %s%s%s\n", candidateUse.groupIndex, userId, CandidateUserName(user),
                candidateUse.node->isContained() ? " contained" : "", candidateUse.canReuse ? "" : " not-reusable");
    }

    JITDUMP("SVE constant mask groups in " FMT_BB ":\n", block->bbNum);

    for (int i = 0; i < groups.Height(); i++)
    {
        ConstantReuseGroup& group = groups.BottomRef(i);
        JITDUMP("  group %d key ", i);
        DumpConstantMaskCandidateKey(compiler, group.candidate);
        JITDUMP(" count=%u %s\n", group.count, group.clean ? "clean" : "blocked");
    }

    JITDUMP("\n");
}
#endif // DEBUG

//-----------------------------------------------------------------------------
// TryGetConstantMaskCandidate: Try to identify a node as a reusable constant-mask candidate.
//
// Arguments:
//    node      - The node to inspect
//    candidate - [out] The candidate key for the node
//
// Return Value:
//    True if the node represents a supported constant mask.
//
bool TryGetConstantMaskCandidate(GenTree* node, ConstantMaskCandidate* candidate)
{
    if (node->OperIs(GT_CNS_MSK))
    {
        GenTreeMskCon* mask = node->AsMskCon();

        if (mask->IsZero())
        {
            candidate->opt     = INS_OPTS_SCALABLE_B;
            candidate->pattern = PFalseReusePattern;
            return true;
        }

        const struct
        {
            var_types type;
            insOpts   opt;
        } candidates[] = {
            {TYP_BYTE, INS_OPTS_SCALABLE_B},
            {TYP_SHORT, INS_OPTS_SCALABLE_H},
            {TYP_INT, INS_OPTS_SCALABLE_S},
            {TYP_LONG, INS_OPTS_SCALABLE_D},
        };

        // A constant mask may lower to ptrue when it is one of the SVE mask
        // patterns. Try each ptrue element size and keep the exact match.
        for (const auto& current : candidates)
        {
            SveMaskPattern pattern = EvaluateSimdMaskToPattern<simd16_t>(current.type, mask->gtSimdMaskVal);
            if (pattern != SveMaskPatternNone)
            {
                candidate->opt     = current.opt;
                candidate->pattern = PTrueReusePattern(pattern);
                return true;
            }
        }

        return false;
    }

    if (!node->OperIsHWIntrinsic())
    {
        return false;
    }

    GenTreeHWIntrinsic* hwintrinsic = node->AsHWIntrinsic();
    NamedIntrinsic      intrinsicId = hwintrinsic->GetHWIntrinsicId();

    if (intrinsicId == NI_Sve_ConversionTrueMask)
    {
        if (!TryGetSvePTrueOpt(hwintrinsic->GetSimdBaseType(), &candidate->opt))
        {
            return false;
        }

        candidate->pattern = static_cast<int>(SveMaskPatternAll);
        return true;
    }

    if (HWIntrinsicInfo::IsSveCreateTrueMask(intrinsicId))
    {
        GenTree* patternOp = hwintrinsic->Op(1);
        if ((patternOp == nullptr) || !patternOp->IsCnsIntOrI())
        {
            return false;
        }

        if (!TryGetSvePTrueOpt(hwintrinsic->GetSimdBaseType(), &candidate->opt))
        {
            return false;
        }

        candidate->pattern =
            PTrueReusePattern(static_cast<SveMaskPattern>(patternOp->AsIntConCommon()->IntegralValue()));
        return true;
    }

    return false;
}

//-----------------------------------------------------------------------------
// CanReuseConstantMaskCandidate: Check if a candidate node can be replaced with a temp use.
//
// Arguments:
//    node - The candidate node to check
//
// Return Value:
//    True if the node can be directly replaced.
//
bool CanReuseConstantMaskCandidate(GenTree* node)
{
    return !node->isContained();
}

//-----------------------------------------------------------------------------
// IsAllTrueMaskCandidate: Check if a candidate is an all-true mask.
//
// Arguments:
//    candidate - The candidate key to check
//
// Return Value:
//    True if the candidate is an all-true ptrue mask.
//
bool IsAllTrueMaskCandidate(const ConstantMaskCandidate& candidate)
{
    return candidate.pattern == static_cast<int>(SveMaskPatternAll);
}

//-----------------------------------------------------------------------------
// IsLiteralTrueMaskUseProfitable: Check if an all-true mask should remain literal for a use.
//
// Arguments:
//    node - The all-true mask candidate node
//    user - The candidate's user node, or nullptr if no user was found
//
// Return Value:
//    True if replacing the literal mask with a temp remains profitable.
//
bool IsLiteralTrueMaskUseProfitable(GenTree* node, GenTree* user)
{
    if ((user == nullptr) || !user->OperIsHWIntrinsic(NI_Sve_ConditionalSelect))
    {
        return true;
    }

    GenTreeHWIntrinsic* conditionalSelect = user->AsHWIntrinsic();
    GenTree*            mask              = conditionalSelect->Op(1);
    GenTree*            trueValue         = conditionalSelect->Op(2);
    GenTree*            falseValue        = conditionalSelect->Op(3);

    if ((mask != node) || !trueValue->OperIsHWIntrinsic() || !trueValue->IsEmbMaskOp())
    {
        return true;
    }

    // Codegen has special handling for ConditionalSelect(AllTrue, embedded-op, zero):
    // it can emit the embedded operation directly without a movprfx. If we hide
    // the literal all-true mask behind a temp, codegen has to preserve inactive
    // lanes and may introduce a movprfx.
    return !falseValue->IsVectorZero();
}

//-----------------------------------------------------------------------------
// ReuseConstantMaskCandidates: Reuse duplicate constant masks within a block.
//
// Arguments:
//    compiler - Compiler instance
//    block    - The block to optimize
//
// Return Value:
//    True if the block was changed.
//
bool ReuseConstantMaskCandidates(Compiler* compiler, BasicBlock* block)
{
    if (!compiler->compEnregLocals())
    {
        // Reuse materializes the first candidate into a temp. If locals are
        // not enregistered, that temp will be stack-homed and later uses become
        // stack loads, which is generally worse than rematerializing the mask.
        return false;
    }

    auto sameCandidateKey = [](const ConstantMaskCandidate& left, const ConstantMaskCandidate& right) {
        return (left.opt == right.opt) && (left.pattern == right.pattern);
    };

    LIR::Range& range = LIR::AsRange(block);

    ArrayStack<ConstantReuseGroup>       groups(compiler->getAllocator(CMK_ArrayStack));
    ArrayStack<ConstantMaskCandidateUse> candidates(compiler->getAllocator(CMK_ArrayStack));

    // First pass: count exact-match candidates in this block. If any candidate
    // for a key is not cleanly reusable, leave the whole key alone for now.
    unsigned nodeIndex = 0;
    for (GenTree* node : range)
    {
        ConstantMaskCandidate candidate;
        if (!TryGetConstantMaskCandidate(node, &candidate))
        {
            nodeIndex++;
            continue;
        }

        LIR::Use use;
        GenTree* user = range.TryGetUse(node, &use) ? use.User() : nullptr;

        // Contained masks cannot be replaced directly, but recording them lets
        // the group reject partial rewrites for this key.
        bool canReuse = CanReuseConstantMaskCandidate(node);
        if (canReuse && IsAllTrueMaskCandidate(candidate) && !IsLiteralTrueMaskUseProfitable(node, user))
        {
            canReuse = false;
        }

        ConstantReuseGroup* group      = nullptr;
        int                 groupIndex = -1;
        for (int i = 0; i < groups.Height(); i++)
        {
            ConstantReuseGroup& current = groups.BottomRef(i);
            if (sameCandidateKey(current.candidate, candidate))
            {
                group      = &current;
                groupIndex = i;
                break;
            }
        }

        if (group == nullptr)
        {
            groupIndex                  = groups.Height();
            ConstantReuseGroup newGroup = {candidate, 0, canReuse, BAD_VAR_NUM, nodeIndex, nodeIndex, false};
            groups.Push(newGroup);
            group = &groups.TopRef();
        }

        // Keep the original candidate order so the first reusable node becomes
        // the defining temp and later candidates become loads from that temp.
        group->count++;
        group->lastIndex = nodeIndex;

        if (!canReuse)
        {
            group->clean = false;
        }

        ConstantMaskCandidateUse candidateUse = {node, groupIndex DEBUGARG(user) DEBUGARG(canReuse)};
        candidates.Push(candidateUse);
        nodeIndex++;
    }

    if (groups.Empty())
    {
        return false;
    }

    nodeIndex = 0;
    for (GenTree* node : range)
    {
        if (node->OperIs(GT_CALL))
        {
            for (int i = 0; i < groups.Height(); i++)
            {
                ConstantReuseGroup& group = groups.BottomRef(i);
                group.crossesCall |= (group.firstIndex < nodeIndex) && (nodeIndex < group.lastIndex);
            }
        }

        nodeIndex++;
    }

    JITDUMP("\nSVE constant mask reuse before " FMT_BB ":\n", block->bbNum);
    JITDUMPEXEC(compiler->fgDumpBlock(block));
    INDEBUG(DumpConstantMaskCandidates(compiler, block, candidates, groups));

    bool madeChanges = false;

    // Second pass: materialize the first candidate into a temp and replace
    // later equivalent candidates with loads of that temp. The temp is visible
    // to post-lowering liveness/ref-counting before LSRA, so predicate clobbers
    // are modeled by normal local-var lifetimes.
    for (int i = 0; i < candidates.Height(); i++)
    {
        ConstantMaskCandidateUse& candidateUse = candidates.BottomRef(i);
        ConstantReuseGroup&       group        = groups.BottomRef(candidateUse.groupIndex);
        GenTree*                  node         = candidateUse.node;

        if (!group.clean || (group.count <= 1))
        {
            continue;
        }

        // Reusing all-true masks can introduce a live predicate temp. Avoid
        // short live ranges and live ranges that cross calls, as these are
        // likely to spill and are generally worse than rematerializing ptrue.
        if (IsAllTrueMaskCandidate(group.candidate) &&
            ((group.count < 3) || group.crossesCall || (block->HasFlag(BBF_BACKWARD_JUMP) && (group.count < 6))))
        {
            continue;
        }

        LIR::Use use;
        if (!range.TryGetUse(node, &use) || !CanReuseConstantMaskCandidate(node))
        {
            continue;
        }

        if (group.temp == BAD_VAR_NUM)
        {
            GenTree* store = nullptr;
            group.temp     = use.ReplaceWithLclVar(compiler, BAD_VAR_NUM, &store);
            assert(store != nullptr);
            madeChanges = true;
            JITDUMP("SVE constant mask reuse: created temp V%02u for [%06u]\n", group.temp, Compiler::dspTreeID(node));
        }
        else
        {
            GenTree* load = compiler->gtNewLclvNode(group.temp, node->TypeGet());
            range.InsertBefore(use.User(), load);
            use.ReplaceWith(load);
            range.Remove(node);
            madeChanges = true;

            JITDUMP("SVE constant mask reuse: replaced [%06u] with temp V%02u\n", Compiler::dspTreeID(node),
                    group.temp);
        }
    }

    if (madeChanges)
    {
        JITDUMP("\nSVE constant mask reuse after " FMT_BB ":\n", block->bbNum);
        JITDUMPEXEC(compiler->fgDumpBlock(block));
    }

    return madeChanges;
}

} // namespace ConstantReuse

#endif // TARGET_ARM64 && FEATURE_MASKED_HW_INTRINSICS

//------------------------------------------------------------------------
// fgOptimizeConstantReuse: Run the target-specific, post-lowering LIR constant reuse phase.
//
// Returns:
//    PhaseStatus indicating whether the IR was changed.
//
PhaseStatus Compiler::fgOptimizeConstantReuse()
{
#if defined(TARGET_ARM64) && defined(FEATURE_MASKED_HW_INTRINSICS)
    bool madeChanges = false;

    for (BasicBlock* const block : Blocks())
    {
        compCurBB = block;

        madeChanges |= ConstantReuse::ReuseConstantMaskCandidates(this, block);

        assert(LIR::AsRange(block).CheckLIR(this, true));
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
#else
    return PhaseStatus::MODIFIED_NOTHING;
#endif
}
