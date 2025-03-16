// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "jitpch.h"
#include "rangecheck.h"

//------------------------------------------------------------------------
// rangeCheckPhase: optimize bounds checks via range analysis
//
// Returns:
//    Suitable phase status
//
PhaseStatus Compiler::rangeCheckPhase()
{
    if (!doesMethodHaveBoundsChecks() || (fgSsaPassesCompleted == 0))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    const bool madeChanges = GetRangeCheck()->OptimizeRangeChecks();
    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// GetRangeCheck: get the RangeCheck instance
//
// Returns:
//    The range check object
//
RangeCheck* Compiler::GetRangeCheck()
{
    if (optRangeCheck == nullptr)
    {
        optRangeCheck = new (this, CMK_Generic) RangeCheck(this);
    }
    return optRangeCheck;
}

// Max stack depth (path length) in walking the UD chain.
static const int MAX_SEARCH_DEPTH = 100;

// Max nodes to visit in the UD chain for the current method being compiled.
static const int MAX_VISIT_BUDGET = 8192;

// RangeCheck constructor.
RangeCheck::RangeCheck(Compiler* pCompiler)
    : m_preferredBound(ValueNumStore::NoVN)
    , m_pOverflowMap(nullptr)
    , m_pRangeMap(nullptr)
    , m_pSearchPath(nullptr)
    , m_pCompiler(pCompiler)
    , m_alloc(pCompiler->getAllocator(CMK_RangeCheck))
    , m_nVisitBudget(MAX_VISIT_BUDGET)
    , m_updateStmt(false)
{
}

bool RangeCheck::IsOverBudget()
{
    return (m_nVisitBudget <= 0);
}

// Get the range map in which computed ranges are cached.
RangeCheck::RangeMap* RangeCheck::GetRangeMap()
{
    if (m_pRangeMap == nullptr)
    {
        m_pRangeMap = new (m_alloc) RangeMap(m_alloc);
    }
    return m_pRangeMap;
}

void RangeCheck::ClearRangeMap()
{
    if (m_pRangeMap != nullptr)
    {
        m_pRangeMap->RemoveAll();
    }
}

// Get the overflow map in which computed overflows are cached.
RangeCheck::OverflowMap* RangeCheck::GetOverflowMap()
{
    if (m_pOverflowMap == nullptr)
    {
        m_pOverflowMap = new (m_alloc) OverflowMap(m_alloc);
    }
    return m_pOverflowMap;
}

void RangeCheck::ClearOverflowMap()
{
    if (m_pOverflowMap != nullptr)
    {
        m_pOverflowMap->RemoveAll();
    }
}

RangeCheck::SearchPath* RangeCheck::GetSearchPath()
{
    if (m_pSearchPath == nullptr)
    {
        m_pSearchPath = new (m_alloc) SearchPath(m_alloc);
    }
    return m_pSearchPath;
}

void RangeCheck::ClearSearchPath()
{
    if (m_pSearchPath != nullptr)
    {
        m_pSearchPath->RemoveAll();
    }
}

// Get the length of the array vn, if it is new.
int RangeCheck::GetArrLength(ValueNum vn)
{
    ValueNum arrRefVN = m_pCompiler->vnStore->GetArrForLenVn(vn);
    int      size;
    return m_pCompiler->vnStore->TryGetNewArrSize(arrRefVN, &size) ? size : 0;
}

//------------------------------------------------------------------------
// BetweenBounds: Check if the computed range is within bounds
//
// Arguments:
//    Range - the range to check if in bounds
//    upper - the array length vn
//    arrSize - the length of the array if known, or <= 0
//
// Return Value:
//    True iff range is between [0 and vn - 1] or [0, arrSize - 1]
//
// notes:
//    This function assumes that the lower range is resolved and upper range is symbolic as in an
//    increasing loop.
//
// TODO-CQ: This is not general enough.
//
bool RangeCheck::BetweenBounds(Range& range, GenTree* upper, int arrSize)
{
#ifdef DEBUG
    if (m_pCompiler->verbose)
    {
        printf("%s BetweenBounds <%d, ", range.ToString(m_pCompiler), 0);
        Compiler::printTreeID(upper);
        printf(">\n");
    }
#endif // DEBUG

    ValueNumStore* vnStore = m_pCompiler->vnStore;

    // Get the VN for the upper limit.
    ValueNum uLimitVN = vnStore->VNConservativeNormalValue(upper->gtVNPair);

#ifdef DEBUG
    JITDUMP(FMT_VN " upper bound is: ", uLimitVN);
    if (m_pCompiler->verbose)
    {
        vnStore->vnDump(m_pCompiler, uLimitVN);
    }
    JITDUMP("\n");
#endif

    if ((arrSize <= 0) && !vnStore->IsVNCheckedBound(uLimitVN))
    {
        // If we don't know the array size and the upper limit is not known, then bail.
        return false;
    }

    JITDUMP("Array size is: %d\n", arrSize);

    // Upper limit: len + ucns (upper limit constant).
    if (range.UpperLimit().IsBinOpArray())
    {
        if (range.UpperLimit().vn != uLimitVN)
        {
            return false;
        }

        int ucns = range.UpperLimit().GetConstant();

        // Upper limit: Len + [0..n]
        if (ucns >= 0)
        {
            return false;
        }

        // Since upper limit is bounded by the array, return true if lower bound is good.
        if (range.LowerLimit().IsConstant() && range.LowerLimit().GetConstant() >= 0)
        {
            return true;
        }

        // Check if we have the array size allocated by new.
        if (arrSize <= 0)
        {
            return false;
        }

        // At this point,
        // upper limit = len + ucns. ucns < 0
        // lower limit = len + lcns.
        if (range.LowerLimit().IsBinOpArray())
        {
            int lcns = range.LowerLimit().GetConstant();
            if (lcns >= 0 || -lcns > arrSize)
            {
                return false;
            }
            return (range.LowerLimit().vn == uLimitVN && lcns <= ucns);
        }
    }
    // If upper limit is constant
    else if (range.UpperLimit().IsConstant())
    {
        if (arrSize <= 0)
        {
            return false;
        }
        int ucns = range.UpperLimit().GetConstant();
        if (ucns >= arrSize)
        {
            return false;
        }
        if (range.LowerLimit().IsConstant())
        {
            int lcns = range.LowerLimit().GetConstant();
            // Make sure lcns < ucns which is already less than arrSize.
            return (lcns >= 0 && lcns <= ucns);
        }
        if (range.LowerLimit().IsBinOpArray())
        {
            int lcns = range.LowerLimit().GetConstant();
            // len + lcns, make sure we don't subtract too much from len.
            if (lcns >= 0 || -lcns > arrSize)
            {
                return false;
            }
            // Make sure a.len + lcns <= ucns.
            return (range.LowerLimit().vn == uLimitVN && (arrSize + lcns) <= ucns);
        }
    }

    return false;
}

void RangeCheck::OptimizeRangeCheck(BasicBlock* block, Statement* stmt, GenTree* treeParent)
{
    // Check if we are dealing with a bounds check node.
    bool isComma        = treeParent->OperIs(GT_COMMA);
    bool isTopLevelNode = treeParent == stmt->GetRootNode();
    if (!(isComma || isTopLevelNode))
    {
        return;
    }

    // If we are not looking at array bounds check, bail.
    GenTree* tree = isComma ? treeParent->AsOp()->gtOp1 : treeParent;
    if (!tree->OperIs(GT_BOUNDS_CHECK))
    {
        return;
    }

    GenTree*          comma   = treeParent->OperIs(GT_COMMA) ? treeParent : nullptr;
    GenTreeBoundsChk* bndsChk = tree->AsBoundsChk();
    m_preferredBound          = m_pCompiler->vnStore->VNConservativeNormalValue(bndsChk->GetArrayLength()->gtVNPair);
    GenTree* treeIndex        = bndsChk->GetIndex();

    // Take care of constant index first, like a[2], for example.
    ValueNum idxVn    = m_pCompiler->vnStore->VNConservativeNormalValue(treeIndex->gtVNPair);
    ValueNum arrLenVn = m_pCompiler->vnStore->VNConservativeNormalValue(bndsChk->GetArrayLength()->gtVNPair);
    int      arrSize  = 0;

    if (m_pCompiler->vnStore->IsVNConstant(arrLenVn))
    {
        ssize_t      constVal  = -1;
        GenTreeFlags iconFlags = GTF_EMPTY;

        if (m_pCompiler->optIsTreeKnownIntValue(true, bndsChk->GetArrayLength(), &constVal, &iconFlags))
        {
            arrSize = (int)constVal;
        }
    }
    else
    {
        arrSize = GetArrLength(arrLenVn);

        // if we can't find the array length, see if there
        // are any assertions about the array size we can use to get a minimum length
        if (arrSize <= 0)
        {
            JITDUMP("Looking for array size assertions for: " FMT_VN "\n", arrLenVn);
            Range arrLength = Range(Limit(Limit::keDependent));
            MergeEdgeAssertions(m_pCompiler, arrLenVn, arrLenVn, block->bbAssertionIn, &arrLength);
            if (arrLength.lLimit.IsConstant())
            {
                arrSize = arrLength.lLimit.GetConstant();
            }
        }
    }

    JITDUMP("ArrSize for lengthVN:%03X = %d\n", arrLenVn, arrSize);
    if (m_pCompiler->vnStore->IsVNConstant(idxVn) && (arrSize > 0))
    {
        ssize_t      idxVal    = -1;
        GenTreeFlags iconFlags = GTF_EMPTY;
        if (!m_pCompiler->optIsTreeKnownIntValue(true, treeIndex, &idxVal, &iconFlags))
        {
            return;
        }

        JITDUMP("[RangeCheck::OptimizeRangeCheck] Is index %d in <0, arrLenVn " FMT_VN " sz:%d>.\n", idxVal, arrLenVn,
                arrSize);
        if ((idxVal < arrSize) && (idxVal >= 0))
        {
            JITDUMP("Removing range check\n");
            m_pCompiler->optRemoveRangeCheck(bndsChk, comma, stmt);
            m_updateStmt = true;
            return;
        }
    }

    // Special case: arr[arr.Length - CNS] if we know that arr.Length >= CNS
    // We assume that SUB(x, CNS) is canonized into ADD(x, -CNS)
    VNFuncApp funcApp;
    if (m_pCompiler->vnStore->GetVNFunc(idxVn, &funcApp) && funcApp.m_func == (VNFunc)GT_ADD)
    {
        bool     isArrlenAddCns = false;
        ValueNum cnsVN          = {};
        if ((arrLenVn == funcApp.m_args[1]) && m_pCompiler->vnStore->IsVNInt32Constant(funcApp.m_args[0]))
        {
            // ADD(cnsVN, arrLenVn);
            isArrlenAddCns = true;
            cnsVN          = funcApp.m_args[0];
        }
        else if ((arrLenVn == funcApp.m_args[0]) && m_pCompiler->vnStore->IsVNInt32Constant(funcApp.m_args[1]))
        {
            // ADD(arrLenVn, cnsVN);
            isArrlenAddCns = true;
            cnsVN          = funcApp.m_args[1];
        }

        if (isArrlenAddCns)
        {
            // Calculate range for arrLength from assertions, e.g. for
            //
            //   bool result = (arr.Length == 0) || (arr[arr.Length - 1] == 0);
            //
            // here for the array access we know that arr.Length >= 1
            Range arrLenRange = GetRangeWorker(block, bndsChk->GetArrayLength(), false DEBUGARG(0));
            if (arrLenRange.LowerLimit().IsConstant())
            {
                // Lower known limit of ArrLen:
                const int lenLowerLimit = arrLenRange.LowerLimit().GetConstant();

                // Negative delta in the array access (ArrLen + -CNS)
                const int delta = m_pCompiler->vnStore->GetConstantInt32(cnsVN);
                if ((lenLowerLimit > 0) && (delta < 0) && (delta > -CORINFO_Array_MaxLength) &&
                    (lenLowerLimit >= -delta))
                {
                    JITDUMP("[RangeCheck::OptimizeRangeCheck] Between bounds\n");
                    m_pCompiler->optRemoveRangeCheck(bndsChk, comma, stmt);
                    m_updateStmt = true;
                    return;
                }
            }
        }
    }

    if (m_pCompiler->vnStore->GetVNFunc(idxVn, &funcApp) && (funcApp.m_func == (VNFunc)GT_UMOD))
    {
        // We can always omit bound checks for Arr[X u% Arr.Length] pattern (unsigned MOD).
        //
        // if arr.Length is 0 we technically should keep the bounds check, but since the expression
        // has to throw DividedByZeroException anyway - no special handling needed.
        if (funcApp.m_args[1] == arrLenVn)
        {
            JITDUMP("[RangeCheck::OptimizeRangeCheck] UMOD(X, ARR_LEN) is always between bounds\n");
            m_pCompiler->optRemoveRangeCheck(bndsChk, comma, stmt);
            m_updateStmt = true;
            return;
        }
    }

    // Get the range for this index.
    Range range = Range(Limit(Limit::keUndef));
    if (!TryGetRange(block, treeIndex, &range))
    {
        JITDUMP("Failed to get range\n");
        return;
    }

    // If upper or lower limit is found to be unknown (top), or it was found to
    // be unknown because of over budget or a deep search, then return early.
    if (range.UpperLimit().IsUnknown() || range.LowerLimit().IsUnknown())
    {
        // Note: If we had stack depth too deep in the GetRangeWorker call, we'd be
        // too deep even in the DoesOverflow call. So return early.
        return;
    }

    JITDUMP("Range value %s\n", range.ToString(m_pCompiler));
    ClearSearchPath();
    Widen(block, treeIndex, &range);

    // If upper or lower limit is unknown, then return.
    if (range.UpperLimit().IsUnknown() || range.LowerLimit().IsUnknown())
    {
        return;
    }

    // Is the range between the lower and upper bound values.
    if (BetweenBounds(range, bndsChk->GetArrayLength(), arrSize))
    {
        JITDUMP("[RangeCheck::OptimizeRangeCheck] Between bounds\n");
        m_pCompiler->optRemoveRangeCheck(bndsChk, comma, stmt);
        m_updateStmt = true;
    }
}

void RangeCheck::Widen(BasicBlock* block, GenTree* tree, Range* pRange)
{
#ifdef DEBUG
    if (m_pCompiler->verbose)
    {
        printf("[RangeCheck::Widen] " FMT_BB ", \n", block->bbNum);
        Compiler::printTreeID(tree);
        printf("\n");
    }
#endif // DEBUG

    Range& range = *pRange;

    // Try to deduce the lower bound, if it is not known already.
    if (range.LowerLimit().IsDependent() || range.LowerLimit().IsUnknown())
    {
        // To determine the lower bound, ask if the loop increases monotonically.
        bool increasing = IsMonotonicallyIncreasing(tree, false);
        if (increasing)
        {
            JITDUMP("[%06d] is monotonically increasing.\n", Compiler::dspTreeID(tree));
            ClearRangeMap();
            *pRange = GetRangeWorker(block, tree, true DEBUGARG(0));
        }
    }
}

bool RangeCheck::IsBinOpMonotonicallyIncreasing(GenTreeOp* binop)
{
    assert(binop->OperIs(GT_ADD, GT_MUL, GT_LSH));

    GenTree* op1 = binop->gtGetOp1();
    GenTree* op2 = binop->gtGetOp2();

    JITDUMP("[RangeCheck::IsBinOpMonotonicallyIncreasing] [%06d], [%06d]\n", Compiler::dspTreeID(op1),
            Compiler::dspTreeID(op2));

    // Check if we have a var + const or var * const.
    if (binop->OperIs(GT_ADD, GT_MUL) && op2->OperGet() == GT_LCL_VAR)
    {
        std::swap(op1, op2);
    }

    if (op1->OperGet() != GT_LCL_VAR)
    {
        JITDUMP("Not monotonically increasing because op1 is not lclVar.\n");
        return false;
    }
    switch (op2->OperGet())
    {
        case GT_LCL_VAR:
            // When adding/multiplying/shifting two local variables, we also must ensure that any constant is
            // non-negative.
            return IsMonotonicallyIncreasing(op1, true) && IsMonotonicallyIncreasing(op2, true);

        case GT_CNS_INT:
            if (op2->AsIntConCommon()->IconValue() < 0)
            {
                JITDUMP("Not monotonically increasing because of encountered negative constant\n");
                return false;
            }

            return IsMonotonicallyIncreasing(op1, false);

        default:
            JITDUMP("Not monotonically increasing because expression is not recognized.\n");
            return false;
    }
}

// The parameter rejectNegativeConst is true when we are adding two local vars (see above)
bool RangeCheck::IsMonotonicallyIncreasing(GenTree* expr, bool rejectNegativeConst)
{
    JITDUMP("[RangeCheck::IsMonotonicallyIncreasing] [%06d]\n", Compiler::dspTreeID(expr));

    // Add hashtable entry for expr.
    bool alreadyPresent = GetSearchPath()->Set(expr, nullptr, SearchPath::Overwrite);
    if (alreadyPresent)
    {
        return true;
    }

    // Remove hashtable entry for expr when we exit the present scope.
    auto code = [this, expr] {
        GetSearchPath()->Remove(expr);
    };
    jitstd::utility::scoped_code<decltype(code)> finally(code);

    if (GetSearchPath()->GetCount() > MAX_SEARCH_DEPTH)
    {
        return false;
    }

    // If expr is constant, then it is not part of the dependency
    // loop which has to increase monotonically.
    ValueNum vn = expr->gtVNPair.GetConservative();
    if (m_pCompiler->vnStore->IsVNInt32Constant(vn))
    {
        if (rejectNegativeConst)
        {
            int cons = m_pCompiler->vnStore->ConstantValue<int>(vn);
            return (cons >= 0);
        }
        else
        {
            return true;
        }
    }
    // If the expr is local, then try to find the def of the local.
    else if (expr->IsLocal())
    {
        LclSsaVarDsc* ssaDef = GetSsaDefStore(expr->AsLclVarCommon());
        return (ssaDef != nullptr) && IsMonotonicallyIncreasing(ssaDef->GetDefNode()->Data(), rejectNegativeConst);
    }
    else if (expr->OperIs(GT_ADD, GT_MUL, GT_LSH))
    {
        return IsBinOpMonotonicallyIncreasing(expr->AsOp());
    }
    else if (expr->OperGet() == GT_PHI)
    {
        for (GenTreePhi::Use& use : expr->AsPhi()->Uses())
        {
            // If the arg is already in the path, skip.
            if (GetSearchPath()->Lookup(use.GetNode()))
            {
                continue;
            }
            if (!IsMonotonicallyIncreasing(use.GetNode(), rejectNegativeConst))
            {
                JITDUMP("Phi argument not monotonically increasing\n");
                return false;
            }
        }
        return true;
    }
    else if (expr->OperGet() == GT_COMMA)
    {
        return IsMonotonicallyIncreasing(expr->gtEffectiveVal(), rejectNegativeConst);
    }
    JITDUMP("Unknown tree type\n");
    return false;
}

// Given a lclvar use, try to find the lclvar's defining store and its containing block.
LclSsaVarDsc* RangeCheck::GetSsaDefStore(GenTreeLclVarCommon* lclUse)
{
    unsigned ssaNum = lclUse->GetSsaNum();

    if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
    {
        return nullptr;
    }

    unsigned      lclNum = lclUse->GetLclNum();
    LclVarDsc*    varDsc = m_pCompiler->lvaGetDesc(lclNum);
    LclSsaVarDsc* ssaDef = varDsc->GetPerSsaData(ssaNum);

    // RangeCheck does not care about uninitialized variables.
    if (ssaDef->GetDefNode() == nullptr)
    {
        // Parameters are expected to be defined in fgFirstBB if FIRST_SSA_NUM is set
        if (varDsc->lvIsParam && (ssaNum == SsaConfig::FIRST_SSA_NUM))
        {
            assert(ssaDef->GetBlock() == m_pCompiler->fgFirstBB);
        }
        return nullptr;
    }

    // RangeCheck does not understand definitions generated by LCL_FLD nodes
    // nor definitions generated by indirect stores to local variables, nor
    // stores through parent structs.
    GenTreeLclVarCommon* defStore = ssaDef->GetDefNode();
    if (!defStore->OperIs(GT_STORE_LCL_VAR) || !defStore->HasSsaName())
    {
        return nullptr;
    }

    return ssaDef;
}

//------------------------------------------------------------------------
// MergeEdgeAssertions: Merge assertions on the edge flowing into the block about a variable
//
// Arguments:
//    GenTreeLclVarCommon - the variable to look for assertions for
//    assertions - the assertions to use
//    pRange - the range to tighten with assertions
//
void RangeCheck::MergeEdgeAssertions(GenTreeLclVarCommon* lcl, ASSERT_VALARG_TP assertions, Range* pRange)
{
    if (lcl->GetSsaNum() == SsaConfig::RESERVED_SSA_NUM)
    {
        return;
    }

    LclSsaVarDsc* ssaData     = m_pCompiler->lvaGetDesc(lcl)->GetPerSsaData(lcl->GetSsaNum());
    ValueNum      normalLclVN = m_pCompiler->vnStore->VNConservativeNormalValue(ssaData->m_vnPair);
    MergeEdgeAssertions(m_pCompiler, normalLclVN, m_preferredBound, assertions, pRange);
}

//------------------------------------------------------------------------
// TryGetRangeFromAssertions: Cheaper version of TryGetRange that is based purely on assertions
//    and does not require a full range analysis based on SSA.
//
// Arguments:
//    comp             - the compiler instance
//    num              - the value number to analyze range for
//    assertions       - the assertions to use
//    pRange           - the range to tighten with assertions
//
// Return Value:
//    True if the range was successfully computed
//
bool RangeCheck::TryGetRangeFromAssertions(Compiler* comp, ValueNum num, ASSERT_VALARG_TP assertions, Range* pRange)
{
    MergeEdgeAssertions(comp, num, num, assertions, pRange);
    return !pRange->LowerLimit().IsUnknown() || !pRange->UpperLimit().IsUnknown();
}

//------------------------------------------------------------------------
// MergeEdgeAssertions: Merge assertions on the edge flowing into the block about a variable
//
// Arguments:
//    comp             - the compiler instance
//    normalLclVN      - the value number to look for assertions for
//    preferredBoundVN - when this VN is set, it will be given preference over constant limits
//    assertions       - the assertions to use
//    pRange           - the range to tighten with assertions
//
void RangeCheck::MergeEdgeAssertions(
    Compiler* comp, ValueNum normalLclVN, ValueNum preferredBoundVN, ASSERT_VALARG_TP assertions, Range* pRange)
{
    Range assertedRange = Range(Limit(Limit::keUnknown));
    if (BitVecOps::IsEmpty(comp->apTraits, assertions))
    {
        return;
    }

    if (normalLclVN == ValueNumStore::NoVN)
    {
        return;
    }

    // Walk through the "assertions" to check if they apply.
    BitVecOps::Iter iter(comp->apTraits, assertions);
    unsigned        index = 0;
    while (iter.NextElem(&index))
    {
        AssertionIndex assertionIndex = GetAssertionIndex(index);

        Compiler::AssertionDsc* curAssertion = comp->optGetAssertion(assertionIndex);

        Limit      limit(Limit::keUndef);
        genTreeOps cmpOper             = GT_NONE;
        bool       isConstantAssertion = false;
        bool       isUnsigned          = false;

        // Current assertion is of the form (i < len - cns) != 0
        if (curAssertion->IsCheckedBoundArithBound())
        {
            ValueNumStore::CompareCheckedBoundArithInfo info;

            // Get i, len, cns and < as "info."
            comp->vnStore->GetCompareCheckedBoundArithInfo(curAssertion->op1.vn, &info);

            // If we don't have the same variable we are comparing against, bail.
            if (normalLclVN != info.cmpOp)
            {
                continue;
            }

            if ((info.arrOper != GT_ADD) && (info.arrOper != GT_SUB))
            {
                continue;
            }

            // If the operand that operates on the bound is not constant, then done.
            if (!comp->vnStore->IsVNInt32Constant(info.arrOp))
            {
                continue;
            }

            int cons = comp->vnStore->ConstantValue<int>(info.arrOp);
            limit    = Limit(Limit::keBinOpArray, info.vnBound, info.arrOper == GT_SUB ? -cons : cons);
            cmpOper  = (genTreeOps)info.cmpOper;
        }
        // Current assertion is of the form (i < len) != 0
        else if (curAssertion->IsCheckedBoundBound())
        {
            ValueNumStore::CompareCheckedBoundArithInfo info;

            // Get the info as "i", "<" and "len"
            comp->vnStore->GetCompareCheckedBound(curAssertion->op1.vn, &info);

            // If we don't have the same variable we are comparing against, bail.
            if (normalLclVN == info.cmpOp)
            {
                cmpOper = (genTreeOps)info.cmpOper;
                limit   = Limit(Limit::keBinOpArray, info.vnBound, 0);
            }
            else if (normalLclVN == info.vnBound)
            {
                cmpOper = GenTree::SwapRelop((genTreeOps)info.cmpOper);
                limit   = Limit(Limit::keBinOpArray, info.cmpOp, 0);
            }
            else
            {
                continue;
            }
        }
        // Current assertion is of the form (i < 100) != 0
        else if (curAssertion->IsConstantBound() || curAssertion->IsConstantBoundUnsigned())
        {
            ValueNumStore::ConstantBoundInfo info;

            // Get the info as "i", "<" and "100"
            comp->vnStore->GetConstantBoundInfo(curAssertion->op1.vn, &info);

            // If we don't have the same variable we are comparing against, bail.
            if (normalLclVN != info.cmpOpVN)
            {
                continue;
            }

            limit      = Limit(Limit::keConstant, info.constVal);
            cmpOper    = (genTreeOps)info.cmpOper;
            isUnsigned = info.isUnsigned;
        }
        // Current assertion is of the form i == 100
        else if (curAssertion->IsConstantInt32Assertion())
        {
            if (curAssertion->op1.vn != normalLclVN)
            {
                continue;
            }

            // Ignore GC values/NULL caught by IsConstantInt32Assertion assertion (may happen on 32bit)
            if (varTypeIsGC(comp->vnStore->TypeOfVN(curAssertion->op2.vn)))
            {
                continue;
            }

            int cnstLimit = (int)curAssertion->op2.u1.iconVal;
            assert(cnstLimit == comp->vnStore->CoercedConstantValue<int>(curAssertion->op2.vn));

            if ((cnstLimit == 0) && (curAssertion->assertionKind == Compiler::OAK_NOT_EQUAL) &&
                comp->vnStore->IsVNCheckedBound(curAssertion->op1.vn))
            {
                // we have arr.Len != 0, so the length must be atleast one
                limit   = Limit(Limit::keConstant, 1);
                cmpOper = GT_GE;
            }
            else if (curAssertion->assertionKind == Compiler::OAK_EQUAL)
            {
                limit   = Limit(Limit::keConstant, cnstLimit);
                cmpOper = GT_EQ;
            }
            else
            {
                // We have a != assertion, but it doesn't tell us much about the interval. So just skip it.
                continue;
            }

            isConstantAssertion = true;
        }
        // Current assertion asserts a bounds check does not throw
        else if (curAssertion->IsBoundsCheckNoThrow())
        {
            ValueNum indexVN = curAssertion->op1.bnd.vnIdx;
            ValueNum lenVN   = curAssertion->op1.bnd.vnLen;
            if (normalLclVN == indexVN)
            {
                isUnsigned = true;
                cmpOper    = GT_LT;
                limit      = Limit(Limit::keBinOpArray, lenVN, 0);
            }
            else if ((normalLclVN == lenVN) && comp->vnStore->IsVNInt32Constant(indexVN))
            {
                // We have "Const < arr.Length" assertion, it means that "arr.Length >= Const"
                int indexCns = comp->vnStore->GetConstantInt32(indexVN);
                if (indexCns >= 0)
                {
                    cmpOper = GT_GE;
                    limit   = Limit(Limit::keConstant, indexCns);
                }
                else
                {
                    continue;
                }
            }
            else
            {
                continue;
            }
        }
        // Current assertion is not supported, ignore it
        else
        {
            continue;
        }

        assert(limit.IsBinOpArray() || limit.IsConstant());

        // Make sure the assertion is of the form != 0 or == 0 if it isn't a constant assertion.
        if (!isConstantAssertion && (curAssertion->assertionKind != Compiler::OAK_NO_THROW) &&
            (curAssertion->op2.vn != comp->vnStore->VNZeroForType(TYP_INT)))
        {
            continue;
        }
#ifdef DEBUG
        if (comp->verbose)
        {
            comp->optPrintAssertion(curAssertion, assertionIndex);
        }
#endif

        // Limits are sometimes made with the form vn + constant, where vn is a known constant
        // see if we can simplify this to just a constant
        if (limit.IsBinOpArray() && comp->vnStore->IsVNInt32Constant(limit.vn))
        {
            Limit tempLimit = Limit(Limit::keConstant, comp->vnStore->ConstantValue<int>(limit.vn));
            if (tempLimit.AddConstant(limit.cns))
            {
                limit = tempLimit;
            }
        }

        ValueNum arrLenVN = preferredBoundVN;

        if (comp->vnStore->IsVNConstant(arrLenVN))
        {
            // Set arrLenVN to NoVN; this will make it match the "vn" recorded on
            // constant limits (where we explicitly track the constant and don't
            // redundantly store its VN in the "vn" field).
            arrLenVN = ValueNumStore::NoVN;
        }

        // During assertion prop we add assertions of the form:
        //
        //      (i < length) == 0
        //      (i < length) != 0
        //      (i < 100) == 0
        //      (i < 100) != 0
        //      i == 100
        //
        // At this point, we have detected that either op1.vn is (i < length) or (i < length + cns) or
        // (i < 100) and the op2.vn is 0 or that op1.vn is i and op2.vn is a known constant.
        //
        // Now, let us check if we are == 0 (i.e., op1 assertion is false) or != 0 (op1 assertion
        // is true.).
        //
        // If we have a non-constant assertion of the form == 0 (i.e., equals false), then reverse relop.
        // The relop has to be reversed because we have: (i < length) is false which is the same
        // as (i >= length).
        if ((curAssertion->assertionKind == Compiler::OAK_EQUAL) && !isConstantAssertion)
        {
            cmpOper = GenTree::ReverseRelop(cmpOper);
        }

        assert(cmpOper != GT_NONE);

        // Bounds are inclusive, so add -1 for upper bound when "<". But make sure we won't underflow.
        if (cmpOper == GT_LT && !limit.AddConstant(-1))
        {
            continue;
        }
        // Bounds are inclusive, so add +1 for lower bound when ">". But make sure we won't overflow.
        if (cmpOper == GT_GT && !limit.AddConstant(1))
        {
            continue;
        }

        // cmpOp (loop index i) cmpOper len +/- cns
        switch (cmpOper)
        {
            case GT_LT:
            case GT_LE:
                assertedRange.uLimit = limit;
                if (isUnsigned)
                {
                    assertedRange.lLimit = Limit(Limit::keConstant, 0);
                }
                break;

            case GT_GT:
            case GT_GE:
                // GT/GE being unsigned creates a non-contiguous range which we can't represent
                // using single Range object.
                if (!isUnsigned)
                {
                    assertedRange.lLimit = limit;
                }
                break;

            case GT_EQ:
                assertedRange.uLimit = limit;
                assertedRange.lLimit = limit;
                break;

            default:
                // All other 'cmpOper' kinds leave lLimit/uLimit unchanged
                break;
        }

        // We have two ranges - we need to merge (tighten) them.

        auto tightenLimit = [](Limit l1, Limit l2, ValueNum preferredBound, bool isLower) -> Limit {
            // 1) One of the limits is undef, unknown or dependent
            if (l1.IsUndef() || l2.IsUndef())
            {
                // Anything is better than undef.
                return l1.IsUndef() ? l2 : l1;
            }
            if (l1.IsUnknown() || l2.IsUnknown())
            {
                // Anything is better than unknown.
                return l1.IsUnknown() ? l2 : l1;
            }
            if (l1.IsDependent() || l2.IsDependent())
            {
                // Anything is better than dependent.
                return l1.IsDependent() ? l2 : l1;
            }

            // 2) Both limits are constants
            if (l1.IsConstant() && l2.IsConstant())
            {
                //  isLower: whatever is higher is better.
                // !isLower: whatever is lower is better.
                return isLower ? (l1.cns > l2.cns ? l1 : l2) : (l1.cns < l2.cns ? l1 : l2);
            }

            // 3) Both limits are BinOpArray (which is "arrLen + cns")
            if (l1.IsBinOpArray() && l2.IsBinOpArray())
            {
                // If one of them is preferredBound and the other is not, use the preferredBound.
                if (preferredBound != ValueNumStore::NoVN)
                {
                    if ((l1.vn == preferredBound) && (l2.vn != preferredBound))
                    {
                        return l1;
                    }
                    if ((l2.vn == preferredBound) && (l1.vn != preferredBound))
                    {
                        return l2;
                    }
                }

                // Otherwise, just use the one with the higher/lower constant.
                // even if they use different arrLen.
                return isLower ? (l1.cns > l2.cns ? l1 : l2) : (l1.cns < l2.cns ? l1 : l2);
            }

            // 4) One of the limits is a constant and the other is BinOpArray
            if ((l1.IsConstant() && l2.IsBinOpArray()) || (l2.IsConstant() && l1.IsBinOpArray()))
            {
                // l1 - BinOpArray, l2 - constant
                if (l1.IsConstant())
                {
                    std::swap(l1, l2);
                }

                if (((preferredBound == ValueNumStore::NoVN) || (l1.vn != preferredBound)))
                {
                    // if we don't have a preferred bound,
                    // or it doesn't match l1.vn, use the constant (l2).
                    return l2;
                }

                // Otherwise, prefer the BinOpArray(preferredBound) over the constant.
                return l1;
            }
            unreached();
        };

        JITDUMP("Tightening pRange: [%s] with assertedRange: [%s] into ", pRange->ToString(comp),
                assertedRange.ToString(comp));

        pRange->lLimit = tightenLimit(assertedRange.lLimit, pRange->lLimit, preferredBoundVN, true);
        pRange->uLimit = tightenLimit(assertedRange.uLimit, pRange->uLimit, preferredBoundVN, false);

        JITDUMP("[%s]\n", pRange->ToString(comp));
    }
}

// Merge assertions from the pred edges of the block, i.e., check for any assertions about "op's" value numbers for phi
// arguments. If not a phi argument, check if we have assertions about local variables.
void RangeCheck::MergeAssertion(BasicBlock* block, GenTree* op, Range* pRange DEBUGARG(int indent))
{
    JITDUMP("Merging assertions from pred edges of " FMT_BB " for op [%06d] " FMT_VN "\n", block->bbNum,
            Compiler::dspTreeID(op), m_pCompiler->vnStore->VNConservativeNormalValue(op->gtVNPair));
    ASSERT_TP assertions = BitVecOps::UninitVal();

    // If we have a phi arg, we can get to the block from it and use its assertion out.
    if (op->OperIs(GT_PHI_ARG))
    {
        const BasicBlock* pred = op->AsPhiArg()->gtPredBB;
        assertions             = m_pCompiler->optGetEdgeAssertions(block, pred);
        if (!BitVecOps::MayBeUninit(assertions))
        {
            JITDUMP("Merge assertions created by " FMT_BB " for " FMT_BB "\n", pred->bbNum, block->bbNum);
            Compiler::optDumpAssertionIndices(assertions, "\n");
        }
    }
    // Get assertions from bbAssertionIn.
    else if (op->IsLocal())
    {
        assertions = block->bbAssertionIn;
    }

    if (!BitVecOps::MayBeUninit(assertions) && (m_pCompiler->GetAssertionCount() > 0))
    {
        // Perform the merge step to fine tune the range value.
        MergeEdgeAssertions(op->AsLclVarCommon(), assertions, pRange);
    }
}

// Compute the range for a binary operation.
Range RangeCheck::ComputeRangeForBinOp(BasicBlock* block, GenTreeOp* binop, bool monIncreasing DEBUGARG(int indent))
{
    assert(binop->OperIs(GT_ADD, GT_AND, GT_RSH, GT_RSZ, GT_LSH, GT_UMOD, GT_MUL));

    GenTree* op1 = binop->gtGetOp1();
    GenTree* op2 = binop->gtGetOp2();

    ValueNum op1VN = op1->gtVNPair.GetConservative();
    ValueNum op2VN = op2->gtVNPair.GetConservative();

    ValueNumStore* vnStore = m_pCompiler->vnStore;

    bool op1IsCns = vnStore->IsVNConstant(op1VN);
    bool op2IsCns = vnStore->IsVNConstant(op2VN);

    if (binop->OperIsCommutative() && op1IsCns && !op2IsCns)
    {
        // Normalize constants to the right for commutative operators.
        std::swap(op1, op2);
        std::swap(op1VN, op2VN);
        std::swap(op1IsCns, op2IsCns);
    }

    // Special cases for binops where op2 is a constant
    if (binop->OperIs(GT_AND, GT_RSH, GT_RSZ, GT_LSH, GT_UMOD))
    {
        if (!op2IsCns)
        {
            // only cns is supported for op2 at the moment for &,%,<<,>> operators
            return Range(Limit::keUnknown);
        }

        ssize_t op2Cns = vnStore->CoercedConstantValue<ssize_t>(op2VN);
        if (!FitsIn<int>(op2Cns))
        {
            return Range(Limit::keUnknown);
        }

        int op1op2Cns = 0;
        int icon      = -1;
        if (binop->OperIs(GT_AND))
        {
            // x & cns -> [0..cns]
            icon = static_cast<int>(op2Cns);
        }
        else if (binop->OperIs(GT_UMOD))
        {
            // x % cns -> [0..cns-1]
            icon = static_cast<int>(op2Cns) - 1;
        }
        else if (binop->OperIs(GT_RSH, GT_LSH) && op1->OperIs(GT_AND) &&
                 vnStore->IsVNIntegralConstant<int>(op1->AsOp()->gtGetOp2()->gtVNPair.GetConservative(), &op1op2Cns))
        {
            // (x & cns1) >> cns2 -> [0..cns1>>cns2]
            int icon1 = op1op2Cns;
            int icon2 = static_cast<int>(op2Cns);
            if ((icon1 >= 0) && (icon2 >= 0) && (icon2 < 32))
            {
                icon = binop->OperIs(GT_RSH) ? (icon1 >> icon2) : (icon1 << icon2);
            }
        }
        else if (binop->OperIs(GT_RSZ))
        {
            // (x u>> cns) -> [0..(x's max value >> cns)]
            int shiftBy = static_cast<int>(op2->AsIntCon()->IconValue());
            if (shiftBy < 0)
            {
                return Range(Limit::keUnknown);
            }

            int op1Width = (int)(genTypeSize(op1) * BITS_PER_BYTE);
            if (shiftBy >= op1Width)
            {
                return Range(Limit(Limit::keConstant, 0));
            }

            // Calculate max possible value of op1, e.g. UINT_MAX for TYP_INT/TYP_UINT
            uint64_t maxValue = (1ULL << op1Width) - 1;
            icon              = (int)(maxValue >> static_cast<int>(op2->AsIntCon()->IconValue()));
        }

        if (icon >= 0)
        {
            Range range(Limit(Limit::keConstant, 0), Limit(Limit::keConstant, icon));
            JITDUMP("Limit range to %s\n", range.ToString(m_pCompiler));
            return range;
        }
        // Generalized range computation not implemented for these operators
        else if (binop->OperIs(GT_AND, GT_UMOD))
        {
            return Range(Limit::keUnknown);
        }
    }

    // other operators are expected to be handled above.
    assert(binop->OperIs(GT_ADD, GT_MUL, GT_LSH, GT_RSH, GT_RSZ));

    Range* op1RangeCached = nullptr;
    Range  op1Range       = Limit(Limit::keUndef);
    // Check if the range value is already cached.
    if (!GetRangeMap()->Lookup(op1, &op1RangeCached))
    {
        // If we already have the op in the path, then, just rely on assertions, else
        // find the range.
        if (GetSearchPath()->Lookup(op1))
        {
            op1Range = Range(Limit(Limit::keDependent));
        }
        else
        {
            op1Range = GetRangeWorker(block, op1, monIncreasing DEBUGARG(indent));
        }
        MergeAssertion(block, op1, &op1Range DEBUGARG(indent + 1));
    }
    else
    {
        op1Range = *op1RangeCached;
    }

    Range* op2RangeCached;
    Range  op2Range = Limit(Limit::keUndef);
    // Check if the range value is already cached.
    if (!GetRangeMap()->Lookup(op2, &op2RangeCached))
    {
        // If we already have the op in the path, then, just rely on assertions, else
        // find the range.
        if (GetSearchPath()->Lookup(op2))
        {
            op2Range = Range(Limit(Limit::keDependent));
        }
        else
        {
            op2Range = GetRangeWorker(block, op2, monIncreasing DEBUGARG(indent));
        }
        MergeAssertion(block, op2, &op2Range DEBUGARG(indent + 1));
    }
    else
    {
        op2Range = *op2RangeCached;
    }

    Range r = Range(Limit::keUnknown);
    if (binop->OperIs(GT_ADD))
    {
        r = RangeOps::Add(op1Range, op2Range);
        JITDUMP("BinOp add ranges %s %s = %s\n", op1Range.ToString(m_pCompiler), op2Range.ToString(m_pCompiler),
                r.ToString(m_pCompiler));
    }
    else if (binop->OperIs(GT_MUL))
    {
        r = RangeOps::Multiply(op1Range, op2Range);
        JITDUMP("BinOp multiply ranges %s %s = %s\n", op1Range.ToString(m_pCompiler), op2Range.ToString(m_pCompiler),
                r.ToString(m_pCompiler));
    }
    else if (binop->OperIs(GT_LSH))
    {
        // help the next step a bit, convert the LSH rhs to a multiply
        Range convertedOp2Range = RangeOps::ConvertShiftToMultiply(op2Range);
        r                       = RangeOps::Multiply(op1Range, convertedOp2Range);
        JITDUMP("BinOp multiply ranges %s %s = %s\n", op1Range.ToString(m_pCompiler),
                convertedOp2Range.ToString(m_pCompiler), r.ToString(m_pCompiler));
    }
    else if (binop->OperIs(GT_RSH))
    {
        r = RangeOps::ShiftRight(op1Range, op2Range);
        JITDUMP("Right shift range: %s >> %s = %s\n", op1Range.ToString(m_pCompiler), op2Range.ToString(m_pCompiler),
                r.ToString(m_pCompiler));
    }
    return r;
}

//------------------------------------------------------------------------
// GetRangeFromType: Compute the range from the given type
//
// Arguments:
//   type - input type
//
// Return value:
//   range that represents the values given type allows
//
Range RangeCheck::GetRangeFromType(var_types type)
{
    switch (type)
    {
        case TYP_UBYTE:
            return Range(Limit(Limit::keConstant, 0), Limit(Limit::keConstant, BYTE_MAX));
        case TYP_BYTE:
            return Range(Limit(Limit::keConstant, INT8_MIN), Limit(Limit::keConstant, INT8_MAX));
        case TYP_USHORT:
            return Range(Limit(Limit::keConstant, 0), Limit(Limit::keConstant, UINT16_MAX));
        case TYP_SHORT:
            return Range(Limit(Limit::keConstant, INT16_MIN), Limit(Limit::keConstant, INT16_MAX));
        default:
            return Range(Limit(Limit::keUnknown));
    }
}

// Compute the range for a local var definition.
Range RangeCheck::ComputeRangeForLocalDef(BasicBlock*          block,
                                          GenTreeLclVarCommon* lcl,
                                          bool monIncreasing   DEBUGARG(int indent))
{
    LclSsaVarDsc* ssaDef = GetSsaDefStore(lcl);
    if (ssaDef == nullptr)
    {
        return Range(Limit(Limit::keUnknown));
    }
#ifdef DEBUG
    if (m_pCompiler->verbose)
    {
        JITDUMP("----------------------------------------------------\n");
        m_pCompiler->gtDispTree(ssaDef->GetDefNode());
        JITDUMP("----------------------------------------------------\n");
    }
#endif
    Range range = GetRangeWorker(ssaDef->GetBlock(), ssaDef->GetDefNode()->Data(), monIncreasing DEBUGARG(indent));
    if (!BitVecOps::MayBeUninit(block->bbAssertionIn) && (m_pCompiler->GetAssertionCount() > 0))
    {
        JITDUMP("Merge assertions from " FMT_BB ": ", block->bbNum);
        Compiler::optDumpAssertionIndices(block->bbAssertionIn, " ");
        JITDUMP("for definition [%06d]\n", Compiler::dspTreeID(ssaDef->GetDefNode()))

        MergeEdgeAssertions(ssaDef->GetDefNode(), block->bbAssertionIn, &range);
        JITDUMP("done merging\n");
    }
    return range;
}

// Get the limit's maximum possible value.
bool RangeCheck::GetLimitMax(Limit& limit, int* pMax)
{
    int& max1 = *pMax;
    switch (limit.type)
    {
        case Limit::keConstant:
            max1 = limit.GetConstant();
            break;

        case Limit::keBinOpArray:
        {
            int tmp = GetArrLength(limit.vn);
            if (tmp <= 0)
            {
                // If we can't figure out the array length, use the maximum array length,
                // CORINFO_Array_MaxLength (0x7FFFFFC7). However, we get here also when
                // we can't find a Span/ReadOnlySpan bounds check length, and these have
                // a maximum length of INT_MAX (0x7FFFFFFF). If limit.vn refers to a
                // GT_ARR_LENGTH node, then it's an array length, otherwise use the INT_MAX value.

                if (m_pCompiler->vnStore->IsVNArrLen(limit.vn))
                {
                    tmp = CORINFO_Array_MaxLength;
                }
                else
                {
                    const int MaxSpanLength = 0x7FFFFFFF;
                    tmp                     = MaxSpanLength;
                }
            }
            if (IntAddOverflows(tmp, limit.GetConstant()))
            {
                return false;
            }
            max1 = tmp + limit.GetConstant();
        }
        break;

        default:
            return false;
    }
    return true;
}

// Check if the arithmetic overflows.
bool RangeCheck::AddOverflows(Limit& limit1, Limit& limit2)
{
    int max1;
    if (!GetLimitMax(limit1, &max1))
    {
        return true;
    }

    int max2;
    if (!GetLimitMax(limit2, &max2))
    {
        return true;
    }

    return IntAddOverflows(max1, max2);
}

// Check if the arithmetic overflows.
bool RangeCheck::MultiplyOverflows(Limit& limit1, Limit& limit2)
{
    int max1;
    if (!GetLimitMax(limit1, &max1))
    {
        return true;
    }

    int max2;
    if (!GetLimitMax(limit2, &max2))
    {
        return true;
    }

    return CheckedOps::MulOverflows(max1, max2, CheckedOps::Signed);
}

// Does the bin operation overflow.
bool RangeCheck::DoesBinOpOverflow(BasicBlock* block, GenTreeOp* binop, const Range& range)
{
    GenTree* op1 = binop->gtGetOp1();
    GenTree* op2 = binop->gtGetOp2();

    if (!GetSearchPath()->Lookup(op1) && DoesOverflow(block, op1, range))
    {
        return true;
    }

    if (!GetSearchPath()->Lookup(op2) && DoesOverflow(block, op2, range))
    {
        return true;
    }

    // Get the cached ranges of op1
    Range* op1Range = nullptr;
    if (!GetRangeMap()->Lookup(op1, &op1Range))
    {
        return true;
    }
    // Get the cached ranges of op2
    Range* op2Range = nullptr;
    if (!GetRangeMap()->Lookup(op2, &op2Range))
    {
        return true;
    }

    JITDUMP("Checking bin op overflow %s %s %s\n", GenTree::OpName(binop->OperGet()), op1Range->ToString(m_pCompiler),
            op2Range->ToString(m_pCompiler));

    if (binop->OperIs(GT_ADD))
    {
        return AddOverflows(op1Range->UpperLimit(), op2Range->UpperLimit());
    }
    else if (binop->OperIs(GT_MUL))
    {
        return MultiplyOverflows(op1Range->UpperLimit(), op2Range->UpperLimit());
    }

    return true;
}

// Check if the var definition the rhs involves arithmetic that overflows.
bool RangeCheck::DoesVarDefOverflow(BasicBlock* block, GenTreeLclVarCommon* lcl, const Range& range)
{
    LclSsaVarDsc* ssaDef = GetSsaDefStore(lcl);
    if (ssaDef == nullptr)
    {
        if ((lcl->GetSsaNum() == SsaConfig::FIRST_SSA_NUM) && m_pCompiler->lvaIsParameter(lcl->GetLclNum()))
        {
            // Parameter definitions that come from outside the method could not have overflown.
            return false;
        }
        return true;
    }

    // We can use intermediate assertions about the local to prove that any
    // overflow on this path does not matter for the range computed.
    Range assertionRange = Range(Limit(Limit::keUnknown));
    MergeAssertion(block, lcl, &assertionRange DEBUGARG(0));

    // But only if the range from the assertion is more strict than the global
    // range computed; otherwise we might still have used the def's value to
    // tighten the range of the global range.
    Range merged = RangeOps::Merge(range, assertionRange, false);
    if (merged.LowerLimit().Equals(range.LowerLimit()) && merged.UpperLimit().Equals(range.UpperLimit()))
    {
        return false;
    }

    return DoesOverflow(ssaDef->GetBlock(), ssaDef->GetDefNode()->Data(), range);
}

bool RangeCheck::DoesPhiOverflow(BasicBlock* block, GenTree* expr, const Range& range)
{
    for (GenTreePhi::Use& use : expr->AsPhi()->Uses())
    {
        GenTree* arg = use.GetNode();
        if (GetSearchPath()->Lookup(arg))
        {
            continue;
        }
        if (DoesOverflow(block, arg, range))
        {
            return true;
        }
    }
    return false;
}

//------------------------------------------------------------------------
// DoesOverflow: Check if the computation of "expr" may have overflowed.
//
// Arguments:
//   block - the block that contains `expr`
//   expr  - expression to check overflow of
//   range - range that we believe "expr" to be in without accounting for
//           overflow; used to ignore potential overflow on paths where
//           we can prove the value is in this range regardless.
//
// Return value:
//   True if the computation may have involved an impactful overflow.
//
bool RangeCheck::DoesOverflow(BasicBlock* block, GenTree* expr, const Range& range)
{
    bool overflows = false;
    if (!GetOverflowMap()->Lookup(expr, &overflows))
    {
        overflows = ComputeDoesOverflow(block, expr, range);
    }
    return overflows;
}

bool RangeCheck::ComputeDoesOverflow(BasicBlock* block, GenTree* expr, const Range& range)
{
    JITDUMP("Does overflow [%06d]?\n", Compiler::dspTreeID(expr));
    GetSearchPath()->Set(expr, block, SearchPath::Overwrite);

    bool overflows = true;

    if (GetSearchPath()->GetCount() > MAX_SEARCH_DEPTH)
    {
        overflows = true;
    }
    // If the definition chain resolves to a constant, it doesn't overflow.
    else if (m_pCompiler->vnStore->IsVNConstant(expr->gtVNPair.GetConservative()))
    {
        overflows = false;
    }
    else if (expr->OperIs(GT_IND))
    {
        overflows = false;
    }
    else if (expr->OperIs(GT_COMMA))
    {
        overflows = ComputeDoesOverflow(block, expr->gtEffectiveVal(), range);
    }
    // Check if the var def has rhs involving arithmetic that overflows.
    else if (expr->IsLocal())
    {
        overflows = DoesVarDefOverflow(block, expr->AsLclVarCommon(), range);
    }
    // Check if add overflows.
    else if (expr->OperIs(GT_ADD, GT_MUL))
    {
        overflows = DoesBinOpOverflow(block, expr->AsOp(), range);
    }
    // These operators don't overflow.
    // Actually, GT_LSH can overflow so it depends on the analysis done in ComputeRangeForBinOp
    else if (expr->OperIs(GT_AND, GT_RSH, GT_RSZ, GT_LSH, GT_UMOD, GT_NEG))
    {
        overflows = false;
    }
    // Walk through phi arguments to check if phi arguments involve arithmetic that overflows.
    else if (expr->OperIs(GT_PHI))
    {
        overflows = DoesPhiOverflow(block, expr, range);
    }
    else if (expr->OperIs(GT_CAST))
    {
        overflows = ComputeDoesOverflow(block, expr->gtGetOp1(), range);
    }
    GetOverflowMap()->Set(expr, overflows, OverflowMap::Overwrite);
    GetSearchPath()->Remove(expr);
    JITDUMP("[%06d] %s\n", Compiler::dspTreeID(expr), ((overflows) ? "overflows" : "does not overflow"));
    return overflows;
}

//------------------------------------------------------------------------
// ComputeRange: Compute the range recursively by asking for the range of each variable in the dependency chain.
//
// Arguments:
//   block - the block that contains `expr`;
//   expr - expression to compute the range for;
//   monIncreasing - true if `expr` is proven to be monotonically increasing;
//   indent - debug printing indent.
//
// Return value:
//   'expr' range as lower and upper limits.
//
// Notes:
//   eg.: c = a + b; ask range of "a" and "b" and add the results.
//   If the result cannot be determined i.e., the dependency chain does not terminate in a value,
//   but continues to loop, which will happen with phi nodes we end the looping by calling the
//   value as "dependent" (dep).
//   If the loop is proven to be "monIncreasing", then make liberal decisions for the lower bound
//   while merging phi node. eg.: merge((0, dep), (dep, dep)) = (0, dep),
//   merge((0, 1), (dep, dep)) = (0, dep), merge((0, 5), (dep, 10)) = (0, 10).
//
Range RangeCheck::ComputeRange(BasicBlock* block, GenTree* expr, bool monIncreasing DEBUGARG(int indent))
{
    bool  newlyAdded = !GetSearchPath()->Set(expr, block, SearchPath::Overwrite);
    Range range      = Limit(Limit::keUndef);

    ValueNum vn = m_pCompiler->vnStore->VNConservativeNormalValue(expr->gtVNPair);

    // If we just added 'expr' in the current search path, then reduce the budget.
    if (newlyAdded)
    {
        // Assert that we are not re-entrant for a node which has been
        // visited and resolved before and not currently on the search path.
        noway_assert(!GetRangeMap()->Lookup(expr));
        m_nVisitBudget--;
    }
    // Prevent quadratic behavior.
    if (IsOverBudget())
    {
        // Set to unknown, since an Unknown range resolution, will stop further
        // searches. This is because anything that merges with Unknown will
        // yield Unknown. Unknown is lattice top.
        range = Range(Limit(Limit::keUnknown));
        JITDUMP("GetRangeWorker not tractable within max node visit budget.\n");
    }
    // Prevent unbounded recursion.
    else if (GetSearchPath()->GetCount() > MAX_SEARCH_DEPTH)
    {
        // Unknown is lattice top, anything that merges with Unknown will yield Unknown.
        range = Range(Limit(Limit::keUnknown));
        JITDUMP("GetRangeWorker not tractable within max stack depth.\n");
    }
    // TYP_LONG is not supported anyway.
    else if (expr->TypeGet() == TYP_LONG)
    {
        range = Range(Limit(Limit::keUnknown));
        JITDUMP("GetRangeWorker long, setting to unknown value.\n");
    }
    // If VN is constant return range as constant.
    else if (m_pCompiler->vnStore->IsVNConstant(vn))
    {
        range = (m_pCompiler->vnStore->TypeOfVN(vn) == TYP_INT)
                    ? Range(Limit(Limit::keConstant, m_pCompiler->vnStore->ConstantValue<int>(vn)))
                    : Limit(Limit::keUnknown);
    }
    // If local, find the definition from the def map and evaluate the range for rhs.
    else if (expr->IsLocal())
    {
        range = ComputeRangeForLocalDef(block, expr->AsLclVarCommon(), monIncreasing DEBUGARG(indent + 1));
        MergeAssertion(block, expr, &range DEBUGARG(indent + 1));
    }
    // compute the range for binary operation
    else if (expr->OperIs(GT_ADD, GT_AND, GT_RSH, GT_RSZ, GT_LSH, GT_UMOD, GT_MUL))
    {
        range = ComputeRangeForBinOp(block, expr->AsOp(), monIncreasing DEBUGARG(indent + 1));
    }
    else if (expr->OperIs(GT_NEG))
    {
        // Compute range for negation, e.g.: [0..8] -> [-8..0]
        Range op1Range = GetRangeWorker(block, expr->gtGetOp1(), monIncreasing DEBUGARG(indent + 1));
        range          = RangeOps::Negate(op1Range);
    }
    // If phi, then compute the range for arguments, calling the result "dependent" when looping begins.
    else if (expr->OperIs(GT_PHI))
    {
        for (GenTreePhi::Use& use : expr->AsPhi()->Uses())
        {
            Range argRange = Range(Limit(Limit::keUndef));
            if (GetSearchPath()->Lookup(use.GetNode()))
            {
                JITDUMP("PhiArg [%06d] is already being computed\n", Compiler::dspTreeID(use.GetNode()));
                argRange = Range(Limit(Limit::keDependent));
            }
            else
            {
                argRange = GetRangeWorker(block, use.GetNode(), monIncreasing DEBUGARG(indent + 1));
            }
            assert(!argRange.LowerLimit().IsUndef());
            assert(!argRange.UpperLimit().IsUndef());
            MergeAssertion(block, use.GetNode(), &argRange DEBUGARG(indent + 1));
            JITDUMP("Merging ranges %s %s:", range.ToString(m_pCompiler), argRange.ToString(m_pCompiler));
            range = RangeOps::Merge(range, argRange, monIncreasing);
            JITDUMP("%s\n", range.ToString(m_pCompiler));
        }
    }
    else if (varTypeIsSmall(expr))
    {
        range = GetRangeFromType(expr->TypeGet());
        JITDUMP("%s\n", range.ToString(m_pCompiler));
    }
    else if (expr->OperIs(GT_COMMA))
    {
        range = GetRangeWorker(block, expr->gtEffectiveVal(), monIncreasing DEBUGARG(indent + 1));
    }
    else if (expr->OperIs(GT_CAST))
    {
        // TODO: consider computing range for CastOp and intersect it with this.
        range = GetRangeFromType(expr->AsCast()->CastToType());
    }
    else
    {
        // The expression is not recognized, so the result is unknown.
        range = Range(Limit(Limit::keUnknown));
    }

    GetRangeMap()->Set(expr, new (m_alloc) Range(range), RangeMap::Overwrite);
    GetSearchPath()->Remove(expr);
    return range;
}

#ifdef DEBUG
void Indent(int indent)
{
    for (int i = 0; i < indent; ++i)
    {
        JITDUMP("   ");
    }
}
#endif

//------------------------------------------------------------------------
// TryGetRange: Try to obtain the range of an expression.
//
// Arguments:
//    block  - the block that contains `expr`;
//    expr   - expression to compute the range for;
//    pRange - [Out] range of the expression;
//
// Return Value:
//    false if the range is unknown or determined to overflow.
//
bool RangeCheck::TryGetRange(BasicBlock* block, GenTree* expr, Range* pRange)
{
    // Reset the maps.
    ClearRangeMap();
    ClearOverflowMap();
    ClearSearchPath();

    Range range = GetRangeWorker(block, expr, false DEBUGARG(0));
    if (range.UpperLimit().IsUnknown() && range.LowerLimit().IsUnknown())
    {
        JITDUMP("Range is completely unknown.\n");
        return false;
    }

    if (DoesOverflow(block, expr, range))
    {
        JITDUMP("Range determined to overflow.\n");
        return false;
    }

    *pRange = range;
    return true;
}

//------------------------------------------------------------------------
// GetRangeWorker: Internal worker for TryGetRange. Does not reset the internal state
//    needed to obtain cached ranges quickly.
//
// Arguments:
//    block         - the block that contains `expr`;
//    expr          - expression to compute the range for;
//    monIncreasing - true if `expr` is proven to be monotonically increasing;
//    indent        - debug printing indent.
//
// Return Value:
//    expr's range
//
Range RangeCheck::GetRangeWorker(BasicBlock* block, GenTree* expr, bool monIncreasing DEBUGARG(int indent))
{
#ifdef DEBUG
    if (m_pCompiler->verbose)
    {
        Indent(indent);
        JITDUMP("[RangeCheck::GetRangeWorker] " FMT_BB " ", block->bbNum);
        m_pCompiler->gtDispTree(expr);
        Indent(indent);
        JITDUMP("{\n", expr);
    }
#endif

    Range* pRange = nullptr;
    Range  range =
        GetRangeMap()->Lookup(expr, &pRange) ? *pRange : ComputeRange(block, expr, monIncreasing DEBUGARG(indent));

#ifdef DEBUG
    if (m_pCompiler->verbose)
    {
        Indent(indent);
        JITDUMP("   %s Range [%06d] => %s\n", (pRange == nullptr) ? "Computed" : "Cached", Compiler::dspTreeID(expr),
                range.ToString(m_pCompiler));
        Indent(indent);
        JITDUMP("}\n", expr);
    }
#endif
    return range;
}

// Entry point to range check optimizations.
bool RangeCheck::OptimizeRangeChecks()
{
    // Reset the budget in case of JitOptRepeat.
    m_nVisitBudget   = MAX_VISIT_BUDGET;
    m_preferredBound = ValueNumStore::NoVN;

    bool madeChanges = false;

    // Walk through trees looking for arrBndsChk node and check if it can be optimized.
    for (BasicBlock* const block : m_pCompiler->Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            m_updateStmt = false;

            for (GenTree* const tree : stmt->TreeList())
            {
                if (IsOverBudget() && !m_updateStmt)
                {
                    return madeChanges;
                }

                if (tree->OperIs(GT_BOUNDS_CHECK))
                {
                    // Leave a hint for optRangeCheckCloning to improve the JIT TP.
                    // NOTE: it doesn't have to be precise and being properly maintained
                    // during transformations, it's just a hint.
                    block->SetFlags(BBF_MAY_HAVE_BOUNDS_CHECKS);
                }

                OptimizeRangeCheck(block, stmt, tree);
            }

            if (m_updateStmt)
            {
                m_pCompiler->gtSetStmtInfo(stmt);
                m_pCompiler->fgSetStmtSeq(stmt);
                madeChanges = true;
            }
        }
    }

    return madeChanges;
}
