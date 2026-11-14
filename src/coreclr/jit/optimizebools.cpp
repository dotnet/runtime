// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                              Optimizer                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif
#include "modularrange.h"

/*****************************************************************************/

//-----------------------------------------------------------------------------
// OptTestInfo:     Member of OptBoolsDsc struct used to test if a GT_JTRUE or return node
//                  is a boolean comparison
//
struct OptTestInfo
{
    Statement* testStmt; // Last statement of the basic block
    GenTree*   testTree; // The root node of the testStmt (GT_JTRUE or GT_RETURN/GT_SWIFT_ERROR_RET).
    GenTree*   compTree; // The compare node (i.e. GT_EQ or GT_NE node) of the testTree
    bool       isBool;   // If the compTree is boolean expression

    GenTree* GetTestOp() const
    {
        assert(testTree != nullptr);

        if (testTree->OperIs(GT_JTRUE))
        {
            return testTree->gtGetOp1();
        }

        assert(testTree->OperIs(GT_RETURN, GT_SWIFT_ERROR_RET));
        return testTree->AsOp()->GetReturnValue();
    }

    void SetTestOp(GenTree* const op)
    {
        assert(testTree != nullptr);

        if (testTree->OperIs(GT_JTRUE))
        {
            testTree->AsOp()->gtOp1 = op;
        }
        else
        {
            assert(testTree->OperIs(GT_RETURN, GT_SWIFT_ERROR_RET));
            testTree->AsOp()->SetReturnValue(op);
        }
    }
};

//-----------------------------------------------------------------------------
// OptBoolsDsc:     Descriptor used for Boolean Optimization
//
class OptBoolsDsc
{
public:
    OptBoolsDsc(BasicBlock* b1, BasicBlock* b2, Compiler* comp)
    {
        m_b1       = b1;
        m_b2       = b2;
        m_compiler = comp;
    }

private:
    BasicBlock* m_b1; // The first basic block with the BBJ_COND conditional jump type
    BasicBlock* m_b2; // The next basic block of m_b1. BBJ_COND type

    Compiler* m_compiler; // The pointer to the Compiler instance

    OptTestInfo m_testInfo1; // The first test info
    OptTestInfo m_testInfo2; // The second test info

    GenTree* m_c1; // The first operand of m_testInfo1.compTree
    GenTree* m_c2; // The first operand of m_testInfo2.compTree

    bool m_sameTarget; // if m_b1 and m_b2 jumps to the same destination

    genTreeOps m_foldOp;   // The fold operator (e.g., GT_AND or GT_OR)
    var_types  m_foldType; // The type of the folded tree
    genTreeOps m_cmpOp;    // The comparison operator (e.g., GT_EQ or GT_NE)

public:
    bool optOptimizeBoolsCondBlock();
    bool optOptimizeCompareChainCondBlock();
    bool optOptimizeRangeTests();
#ifdef DEBUG
    void optOptimizeBoolsGcStress();
#endif

private:
    Statement* optOptimizeBoolsChkBlkCond();
    GenTree*   optIsBoolComp(OptTestInfo* pOptTest);
    bool       optOptimizeBoolsChkTypeCostCond();
    void       optOptimizeBoolsUpdateTrees();
    bool       FindCompareChain(GenTree* condition, bool* isTestCondition);
};

//-----------------------------------------------------------------------------
//  optOptimizeBoolsCondBlock:  Optimize boolean when bbKind of both m_b1 and m_b2 are BBJ_COND
//
//  Returns:
//      true if boolean optimization is done and m_b1 and m_b2 are folded into m_b1, else false.
//
//  Notes:
//      m_b1 and m_b2 are set on entry.
//
//      Case 1: if b1->TargetIs(b2->GetTarget()), it transforms
//          B1 : brtrue(t1, Bx)
//          B2 : brtrue(t2, Bx)
//          B3 :
//      to
//          B1 : brtrue(t1|t2, BX)
//          B3 :
//
//      Case 2: if B2->FalseTargetIs(B1->GetTarget()), it transforms
//          B1 : brtrue(t1, B3)
//          B2 : brtrue(t2, Bx)
//          B3 :
//      to
//          B1 : brtrue((!t1) && t2, Bx)
//          B3 :
//
bool OptBoolsDsc::optOptimizeBoolsCondBlock()
{
    assert(m_b1 != nullptr && m_b2 != nullptr);

    // Check if m_b1 and m_b2 jump to the same target and get back pointers to m_testInfo1 and t2 tree nodes
    // Check if m_b1 and m_b2 have the same target

    if (m_b1->TrueTargetIs(m_b2->GetTrueTarget()))
    {
        // Given the following sequence of blocks :
        //        B1: brtrue(t1, BX)
        //        B2: brtrue(t2, BX)
        //        B3:
        // we will try to fold it to :
        //        B1: brtrue(t1|t2, BX)
        //        B3:

        m_sameTarget = true;
    }
    else if (m_b2->FalseTargetIs(m_b1->GetTrueTarget()))
    {
        // Given the following sequence of blocks :
        //        B1: brtrue(t1, B3)
        //        B2: brtrue(t2, BX)
        //        B3:
        // we will try to fold it to :
        //        B1: brtrue((!t1)&&t2, BX)
        //        B3:

        m_sameTarget = false;
    }
    else
    {
        return false;
    }

    Statement* const s1 = optOptimizeBoolsChkBlkCond();
    if (s1 == nullptr)
    {
        return false;
    }

    // Find the branch conditions of m_b1 and m_b2

    m_c1 = optIsBoolComp(&m_testInfo1);
    if (m_c1 == nullptr)
    {
        return false;
    }

    m_c2 = optIsBoolComp(&m_testInfo2);
    if (m_c2 == nullptr)
    {
        return false;
    }

    // Find the type and cost conditions of m_testInfo1 and m_testInfo2

    if (!optOptimizeBoolsChkTypeCostCond())
    {
        return false;
    }

    // Get the fold operator and the comparison operator

    genTreeOps foldOp;
    genTreeOps cmpOp;
    var_types  foldType = genActualType(m_c1);
    if (varTypeIsGC(foldType))
    {
        foldType = TYP_I_IMPL;
    }

    assert(m_testInfo1.compTree->OperIs(GT_EQ, GT_NE, GT_LT, GT_GT, GT_GE, GT_LE));

    if (m_sameTarget)
    {
        if (m_c1->OperIs(GT_LCL_VAR) && m_c2->OperIs(GT_LCL_VAR) &&
            m_c1->AsLclVarCommon()->GetLclNum() == m_c2->AsLclVarCommon()->GetLclNum())
        {
            // The folded comparisons below are signed (e.g. "c1 <= 0", "c1 >= 0"), so
            // bail if either input has GTF_UNSIGNED.
            if (m_testInfo1.GetTestOp()->IsUnsigned() || m_testInfo2.GetTestOp()->IsUnsigned())
            {
                return false;
            }

            if ((m_testInfo1.compTree->OperIs(GT_LT) && m_testInfo2.compTree->OperIs(GT_EQ)) ||
                (m_testInfo1.compTree->OperIs(GT_EQ) && m_testInfo2.compTree->OperIs(GT_LT)))
            {
                // Case: t1:c1<0 t2:c1==0
                // So we will branch to BX if c1<=0
                //
                // Case: t1:c1==0 t2:c1<0
                // So we will branch to BX if c1<=0
                cmpOp = GT_LE;
            }
            else if ((m_testInfo1.compTree->OperIs(GT_GT) && m_testInfo2.compTree->OperIs(GT_EQ)) ||
                     (m_testInfo1.compTree->OperIs(GT_EQ) && m_testInfo2.compTree->OperIs(GT_GT)))
            {
                // Case: t1:c1>0 t2:c1==0
                // So we will branch to BX if c1>=0
                //
                // Case: t1:c1==0 t2:c1>0
                // So we will branch to BX if c1>=0
                cmpOp = GT_GE;
            }
            else
            {
                return false;
            }

            foldOp = GT_NONE;
        }
        else if (m_testInfo1.compTree->OperIs(GT_EQ) && m_testInfo2.compTree->OperIs(GT_EQ))
        {
            // t1:c1==0 t2:c2==0 ==> Branch to BX if either value is 0
            // So we will branch to BX if (c1&c2)==0

            foldOp = GT_AND;
            cmpOp  = GT_EQ;
        }
        else if (m_testInfo1.compTree->OperIs(GT_LT) && m_testInfo2.compTree->OperIs(GT_LT) &&
                 (!m_testInfo1.GetTestOp()->IsUnsigned() && !m_testInfo2.GetTestOp()->IsUnsigned()))
        {
            // t1:c1<0 t2:c2<0 ==> Branch to BX if either value < 0
            // So we will branch to BX if (c1|c2)<0

            foldOp = GT_OR;
            cmpOp  = GT_LT;
        }
        else if (m_testInfo1.compTree->OperIs(GT_NE) && m_testInfo2.compTree->OperIs(GT_NE))
        {
            // t1:c1!=0 t2:c2!=0 ==> Branch to BX if either value is non-0
            // So we will branch to BX if (c1|c2)!=0

            foldOp = GT_OR;
            cmpOp  = GT_NE;
        }
        else
        {
            return false;
        }
    }
    else
    {
        if (m_c1->OperIs(GT_LCL_VAR) && m_c2->OperIs(GT_LCL_VAR) &&
            m_c1->AsLclVarCommon()->GetLclNum() == m_c2->AsLclVarCommon()->GetLclNum())
        {
            // The folded comparisons below are signed (e.g. "c1 > 0", "c1 < 0"), so
            // bail if either input has GTF_UNSIGNED.
            if (m_testInfo1.GetTestOp()->IsUnsigned() || m_testInfo2.GetTestOp()->IsUnsigned())
            {
                return false;
            }

            if ((m_testInfo1.compTree->OperIs(GT_LT) && m_testInfo2.compTree->OperIs(GT_NE)) ||
                (m_testInfo1.compTree->OperIs(GT_EQ) && m_testInfo2.compTree->OperIs(GT_GE)))
            {
                // Case: t1:c1<0 t2:c1!=0
                // So we will branch to BX if c1>0
                //
                // Case: t1:c1==0 t2:c1>=0
                // So we will branch to BX if c1>0
                cmpOp = GT_GT;
            }
            else if ((m_testInfo1.compTree->OperIs(GT_GT) && m_testInfo2.compTree->OperIs(GT_NE)) ||
                     (m_testInfo1.compTree->OperIs(GT_EQ) && m_testInfo2.compTree->OperIs(GT_LE)))
            {
                // Case: t1:c1>0 t2:c1!=0
                // So we will branch to BX if c1<0
                //
                // Case: t1:c1==0 t2:c1<=0
                // So we will branch to BX if c1<0
                cmpOp = GT_LT;
            }
            else
            {
                return false;
            }

            foldOp = GT_NONE;
        }
        else if (m_testInfo1.compTree->OperIs(GT_EQ) && m_testInfo2.compTree->OperIs(GT_NE))
        {
            // t1:c1==0 t2:c2!=0 ==> Branch to BX if both values are non-0
            // So we will branch to BX if (c1&c2)!=0

            foldOp = GT_AND;
            cmpOp  = GT_NE;
        }
        else if (m_testInfo1.compTree->OperIs(GT_LT) && m_testInfo2.compTree->OperIs(GT_GE) &&
                 (!m_testInfo1.GetTestOp()->IsUnsigned() && !m_testInfo2.GetTestOp()->IsUnsigned()))
        {
            // t1:c1<0 t2:c2>=0 ==> Branch to BX if both values >= 0
            // So we will branch to BX if (c1|c2)>=0

            foldOp = GT_OR;
            cmpOp  = GT_GE;
        }
        else if (m_testInfo1.compTree->OperIs(GT_NE) && m_testInfo2.compTree->OperIs(GT_EQ))
        {
            // t1:c1!=0 t2:c2==0 ==> Branch to BX if both values are 0
            // So we will branch to BX if (c1|c2)==0

            foldOp = GT_OR;
            cmpOp  = GT_EQ;
        }
        else
        {
            return false;
        }
    }

    // Anding requires both values to be 0 or 1

    if ((foldOp == GT_AND) && (!m_testInfo1.isBool || !m_testInfo2.isBool))
    {
        return false;
    }

    //
    // Now update the trees
    //

    m_foldOp   = foldOp;
    m_foldType = foldType;
    m_cmpOp    = cmpOp;

    optOptimizeBoolsUpdateTrees();

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        printf("Folded %sboolean conditions of " FMT_BB " and " FMT_BB " to :\n", m_c2->OperIsLeaf() ? "" : "non-leaf ",
               m_b1->bbNum, m_b2->bbNum);
        m_compiler->gtDispStmt(s1);
        printf("\n");
    }
#endif

    // Return true to continue the bool optimization for the rest of the BB chain
    return true;
}

//-----------------------------------------------------------------------------
//  FindCompareChain:  Check if the given condition is a compare chain.
//
// Arguments:
//      condition:        Condition to check.
//      isTestCondition:  Returns true if condition is but is not a compare chain.
//
//  Returns:
//      true if chain optimization is a compare chain.
//
//  Assumptions:
//      m_b1 and m_b2 are set on entry.
//

bool OptBoolsDsc::FindCompareChain(GenTree* condition, bool* isTestCondition)
{
    GenTree* condOp1 = condition->gtGetOp1();
    GenTree* condOp2 = condition->gtGetOp2();

    *isTestCondition = false;

    if (condition->OperIs(GT_EQ, GT_NE) && condOp2->IsIntegralConst())
    {
        ssize_t condOp2Value = condOp2->AsIntCon()->IconValue();

        if (condOp2Value == 0)
        {
            // Found a EQ/NE(...,0). Does it contain a compare chain (ie - conditions that have
            // previously been combined by optOptimizeCompareChainCondBlock) or is it a test condition
            // that will be optimised to cbz/cbnz during lowering?

            if (condOp1->OperIs(GT_AND, GT_OR))
            {
                // Check that the second operand of AND/OR ends with a compare operation, as this will be
                // the condition the new link in the chain will connect with.
                if (condOp1->gtGetOp2()->OperIsCmpCompare() && varTypeIsIntegralOrI(condOp1->gtGetOp2()->gtGetOp1()))
                {
                    return true;
                }
            }

            *isTestCondition = true;
        }
        else if (condOp1->OperIs(GT_AND) && isPow2(static_cast<target_size_t>(condOp2Value)) &&
                 condOp1->gtGetOp2()->IsIntegralConst(condOp2Value))
        {
            // Found a EQ/NE(AND(...,n),n) which will be optimized to tbz/tbnz during lowering.
            *isTestCondition = true;
        }
    }

    return false;
}

//------------------------------------------------------------------------------
// IsConstantRangeTest: Does the given compare node represent a constant range test? E.g.
//    "X relop CNS" or "CNS relop X" where relop is [<, <=, >, >=, ==, !=]
//
// Arguments:
//    tree - compare node
//    varNode - [OUT] this will be set to the variable part of the constant range test
//    cnsNode - [OUT] this will be set to the constant part of the constant range test
//    cmp     - [OUT] this will be set to a normalized compare operator so that the constant
//                    is always on the right hand side of the compare.
//
// Returns:
//    true if the compare node represents a constant range test.
//
bool IsConstantRangeTest(GenTreeOp* tree, GenTree** varNode, GenTreeIntCon** cnsNode, genTreeOps* cmp)
{
    if (tree->OperIs(GT_LE, GT_LT, GT_GE, GT_GT, GT_EQ, GT_NE))
    {
        GenTree* op1 = tree->gtGetOp1();
        GenTree* op2 = tree->gtGetOp2();
        // A small-typed value is compared at int width, where its type bounds it.
        if (varTypeIsIntegral(op1) && varTypeIsIntegral(op2) &&
            (op1->TypeIs(op2->TypeGet()) || (varTypeIsSmall(op1) && op2->TypeIs(TYP_INT)) ||
             (varTypeIsSmall(op2) && op1->TypeIs(TYP_INT))))
        {
            if (op2->IsCnsIntOrI())
            {
                // X relop CNS
                *varNode = op1;
                *cnsNode = op2->AsIntCon();
                *cmp     = tree->OperGet();
                return true;
            }
            if (op1->IsCnsIntOrI())
            {
                // CNS relop X
                *varNode = op2;
                *cnsNode = op1->AsIntCon();

                // Normalize to "X relop CNS"
                *cmp = GenTree::SwapRelop(tree->OperGet());
                return true;
            }
        }
    }
    return false;
}

//------------------------------------------------------------------------------
// GetComparisonBase: Match a local, optionally behind an unchecked constant offset.
//    COMMAs of the first comparison are skipped. The caller retains their effects.
//
static bool GetComparisonBase(GenTree* value, GenTree** base, target_size_t* offset)
{
    *offset       = 0;
    GenTree* node = value->gtEffectiveVal();
    if (node->OperIs(GT_ADD, GT_SUB))
    {
        GenTree* constant = node->gtGetOp2();
        if (node->gtOverflow() || !constant->IsCnsIntOrI() || constant->IsIconHandle() ||
            !node->gtGetOp1()->TypeIs(value->TypeGet()) || !constant->TypeIs(value->TypeGet()))
        {
            return false;
        }
        *offset = static_cast<target_size_t>(constant->AsIntCon()->IconValue());
        if (node->OperIs(GT_SUB))
        {
            *offset = 0 - *offset;
        }
        node = node->gtGetOp1();
    }
    *base = node;
    if (!node->TypeIs(value->TypeGet()))
    {
        return false;
    }
    if (node->OperIs(GT_LCL_VAR, GT_LCL_FLD))
    {
        *base = node;
        return true;
    }

    // A load the two tests share. The first test evaluates it; the second would only read the same
    // location again, with nothing in between that could store to it. It must not sit under effects
    // that would have to be kept apart from it, and a volatile load is a read that must happen.
    if (node->OperIs(GT_IND, GT_ARR_LENGTH) && (node == value) &&
        ((node->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) == 0) &&
        !(node->OperIs(GT_IND) && node->AsIndir()->IsVolatile()))
    {
        *base = node;
        return true;
    }
    return false;
}

static GenTreeIntCon* NewRangeConstant(Compiler* comp, target_size_t bits, var_types type)
{
    ssize_t value = type == TYP_INT ? static_cast<int32_t>(bits) : static_cast<ssize_t>(bits);
    return comp->gtNewIconNode(value, type);
}

//------------------------------------------------------------------------------
// ConstantCost: The instructions a comparison constant costs beyond the instruction that consumes it.
//
// Arguments:
//    constant - the constant node, with its costs set
//    forCcmp  - whether a ccmp consumes it, which takes a five bit immediate, where cmp and sub take
//               twelve bits
//
// Return Value:
//    Zero when the instruction takes the constant as an immediate, the instructions that put it in a
//    register otherwise.
//
static unsigned ConstantCost(GenTree* constant, bool forCcmp)
{
#ifdef TARGET_ARM64
    if (constant->IsCnsIntOrI() && !constant->IsIconHandle())
    {
        INT64 const    value = constant->AsIntCon()->IconValue();
        emitAttr const size  = emitActualTypeSize(constant);
        if (forCcmp ? emitter::emitIns_valid_imm_for_ccmp(value) : emitter::emitIns_valid_imm_for_add(value, size))
        {
            return 0;
        }
        if (emitter::emitIns_valid_imm_for_mov(value, size))
        {
            return 1;
        }

        // One movz or movn and a movk per further halfword that differs from what it filled
        unsigned movzCount = 0;
        unsigned movnCount = 0;
        for (unsigned shift = 0; shift < ((size == EA_8BYTE) ? 64u : 32u); shift += 16)
        {
            unsigned const halfword = static_cast<unsigned>(value >> shift) & 0xFFFF;
            movzCount += (halfword != 0);
            movnCount += (halfword != 0xFFFF);
        }
        return min(movzCount, movnCount);
    }
#endif
    return constant->GetCostEx();
}

//------------------------------------------------------------------------------
// SameBase: Are the two comparison bases the same value?
//    The second test's load is the first's again, so it is marked non-faulting once the first has
//    taken the null check; that difference does not make it another load.
//
static bool SameBase(GenTree* base1, GenTree* base2)
{
    if (GenTree::Compare(base1, base2))
    {
        return true;
    }
    if (base1->OperIs(GT_IND) && base2->OperIs(GT_IND) && base1->TypeIs(base2->TypeGet()) &&
        (((base1->gtFlags ^ base2->gtFlags) & GTF_IND_FLAGS & ~GTF_IND_NONFAULTING) == 0))
    {
        return GenTree::Compare(base1->AsIndir()->Addr(), base2->AsIndir()->Addr());
    }
    if (base1->OperIs(GT_ARR_LENGTH) && base2->OperIs(GT_ARR_LENGTH) &&
        (base1->AsArrLen()->ArrLenOffset() == base2->AsArrLen()->ArrLenOffset()))
    {
        return GenTree::Compare(base1->AsArrLen()->ArrRef(), base2->AsArrLen()->ArrRef());
    }
    return false;
}

//------------------------------------------------------------------------------
// FoldComparisons: Fold two comparisons of the same local into one.
//    Each comparison becomes the range of values that satisfy it. The ranges are intersected
//    and the result is emitted as a constant or a single comparison when it has such a form.
//    The effects of the first comparison are kept. The second must have none.
//
static GenTree* FoldComparisons(
    Compiler* comp, GenTreeOp* cmp1, bool cmp1IsReversed, GenTreeOp* cmp2, bool cmp2IsReversed)
{
    GenTree*       value1;
    GenTree*       value2;
    GenTreeIntCon* bound1;
    GenTreeIntCon* bound2 = nullptr;
    genTreeOps     op1;
    genTreeOps     op2;
    if (!IsConstantRangeTest(cmp1, &value1, &bound1, &op1) || !varTypeIsIntOrI(genActualType(value1)) ||
        bound1->IsIconHandle())
    {
        return nullptr;
    }

    GenTree*      base1;
    target_size_t offset1;
    if (!GetComparisonBase(value1, &base1, &offset1))
    {
        return nullptr;
    }

    target_size_t mask = (genActualType(value1) == TYP_INT) ? UINT32_MAX : static_cast<target_size_t>(-1);
    op1                = cmp1IsReversed ? GenTree::ReverseRelop(op1) : op1;
    ModularRange first = ModularRange::FromComparison(op1, static_cast<target_size_t>(bound1->IconValue()),
                                                      cmp1->IsUnsigned(), mask, offset1);
    // The value's type and shape bound it, a byte to [0, 255], a cast or a shift to its result range.
    // Narrowing the first test to that interval makes a pair fold that would otherwise straddle it.
    {
        IntegralRange const typeRange = IntegralRange::ForNode(base1, comp);
        IntegralRange const fullRange = IntegralRange::ForType(genActualType(value1));
        if (!typeRange.Equals(fullRange))
        {
            target_size_t const lo =
                static_cast<target_size_t>(IntegralRange::SymbolicToRealValue(typeRange.GetLowerBound())) & mask;
            target_size_t const hi =
                static_cast<target_size_t>(IntegralRange::SymbolicToRealValue(typeRange.GetUpperBound())) & mask;
            ModularRange const               bounded(lo, hi);
            ModularRange                     narrowed;
            ModularRange::Intersection const kind = first.Intersect(bounded, mask, &narrowed);
            if (kind == ModularRange::Intersection::Single)
            {
                first = narrowed;
            }
            else if (kind == ModularRange::Intersection::Empty)
            {
                first = ModularRange();
            }
        }
    }
    // A never-negative value takes only the nonnegative half of the ring. Narrowing the first test to
    // it lets a pair whose result would otherwise straddle the sign boundary fold to one test with
    // small constants.
    if (base1->IsNeverNegative(comp))
    {
        ModularRange const               nonNegative(0, mask >> 1);
        ModularRange                     narrowed;
        ModularRange::Intersection const kind = first.Intersect(nonNegative, mask, &narrowed);
        if (kind == ModularRange::Intersection::Single)
        {
            first = narrowed;
        }
        else if (kind == ModularRange::Intersection::Empty)
        {
            first = ModularRange();
        }
    }

    ModularRange second;
    GenTree*     symbolicBound = nullptr;
    if (IsConstantRangeTest(cmp2, &value2, &bound2, &op2))
    {
        GenTree*      base2;
        target_size_t offset2;
        // The second value is dropped, so it may carry no effect of its own; a load's exception is
        // the one the first test's identical load already decided.
        if (!value2->TypeIs(value1->TypeGet()) || bound2->IsIconHandle() ||
            ((value2->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0) || !GetComparisonBase(value2, &base2, &offset2) ||
            !SameBase(base1, base2))
        {
            return nullptr;
        }
        op2    = cmp2IsReversed ? GenTree::ReverseRelop(op2) : op2;
        second = ModularRange::FromComparison(op2, static_cast<target_size_t>(bound2->IconValue()), cmp2->IsUnsigned(),
                                              mask, offset2);
    }
    else
    {
        op2 = cmp2IsReversed ? GenTree::ReverseRelop(cmp2->OperGet()) : cmp2->OperGet();
        if (SameBase(base1, cmp2->gtGetOp1()))
        {
            symbolicBound = cmp2->gtGetOp2();
        }
        else if (SameBase(base1, cmp2->gtGetOp2()))
        {
            symbolicBound = cmp2->gtGetOp1();
            op2           = GenTree::SwapRelop(op2);
        }
        else
        {
            return nullptr;
        }

        if (((op2 != GT_LT) && (op2 != GT_LE)) || !symbolicBound->TypeIs(value1->TypeGet()) ||
            !symbolicBound->IsNeverNegative(comp) ||
            ((symbolicBound->gtFlags & (GTF_SIDE_EFFECT | GTF_ORDER_SIDEEFF)) != 0))
        {
            // The fold makes the bound evaluate unconditionally, so it must be safe to speculate.
            // GTF_ORDER_SIDEEFF covers e.g. "a[X]" whose bounds check was removed via "X >= 0".
            return nullptr;
        }
    }

    ModularRange range;
    bool         boundedBySymbolic = false;
    if (symbolicBound == nullptr)
    {
        ModularRange::Intersection intersection = first.Intersect(second, mask, &range);
        if ((intersection == ModularRange::Intersection::Multiple) ||
            (intersection == ModularRange::Intersection::Unknown))
        {
            return nullptr;
        }
    }
    else
    {
        // The symbolic bound is nonnegative, so it only constrains the nonnegative half of the
        // domain. Bounding [0, MAX] by it is an unsigned test against the bound, which keeps its
        // strictness because the bound can be zero. Bounding [MIN, -1] excludes nothing for a
        // signed test and everything for an unsigned one.
        target_size_t signBit = (mask >> 1) + 1;
        if (first.Equals(ModularRange(0, signBit - 1), mask))
        {
            boundedBySymbolic = true;
        }
        else if (first.Equals(ModularRange(signBit, mask), mask))
        {
            range = cmp2->IsUnsigned() ? ModularRange() : first;
        }
        else
        {
            return nullptr;
        }
    }

    GenTree*   value = base1;
    GenTree*   newBound;
    genTreeOps oper;
    bool       isUnsigned = false;
    if (boundedBySymbolic)
    {
        oper       = op2;
        newBound   = symbolicBound;
        isUnsigned = true;
    }
    else if (range.IsEmpty() || range.IsFull(mask))
    {
        // Only the first expression is guaranteed to execute in the original chain.
        bool result = range.IsFull(mask) != cmp2IsReversed;
        return comp->gtWrapWithSideEffects(comp->gtNewIconNode(result ? 1 : 0), value1, GTF_OBS_EFFECT);
    }
    else
    {
        var_types     type = genActualType(value1);
        target_size_t constant;
        target_size_t lower   = range.Lower();
        target_size_t upper   = range.Upper();
        target_size_t signBit = (mask >> 1) + 1;
        if (lower == upper)
        {
            oper     = GT_EQ;
            constant = lower;
        }
        else if (range.Width(mask) == mask - 1)
        {
            oper     = GT_NE;
            constant = (lower - 1) & mask;
        }
        else if (upper == signBit - 1)
        {
            oper     = GT_GE;
            constant = lower;
        }
        else if (lower == signBit)
        {
            oper     = GT_LE;
            constant = upper;
        }
        else if (lower == 0)
        {
            oper       = GT_LE;
            constant   = upper;
            isUnsigned = true;
        }
        else if (upper == mask)
        {
            oper       = GT_GE;
            constant   = lower;
            isUnsigned = true;
        }
        else
        {
            // Four tests describe the interval through "(X - offset) relop bound": the unsigned test of
            // the interval or of its complement, and the signed test of either, which starts the interval
            // at the minimum value. They differ only in their constants, so take the one whose constants
            // cost least beyond the sub and cmp that take them; an interval of exactly half the ring is
            // a sign test.
            target_size_t const width      = range.Width(mask);
            target_size_t const complLower = (upper + 1) & mask;
            target_size_t const complWidth = mask - width - 1;

            struct Encoding
            {
                target_size_t offset;
                target_size_t bound;
                genTreeOps    oper;
                bool          isUnsigned;
            };
            Encoding encodings[] = {
                {lower, width, GT_LE, true},
                {complLower, complWidth, GT_GT, true},
                {(lower + signBit) & mask, (width + 1 - signBit) & mask, GT_LT, false},
                {(complLower + signBit) & mask, (complWidth + 1 - signBit) & mask, GT_GE, false},
            };
            if (width == signBit - 1)
            {
                encodings[0] = {lower, 0, GT_GE, false};
            }
            if (complWidth == signBit - 1)
            {
                encodings[1] = {complLower, 0, GT_LT, false};
            }

            // Among equally priced constants the smallest wins, since it fits the most immediate forms,
            // such as the five bits of a ccmp that a later chain may need.
            auto magnitude = [type](target_size_t bits) {
                ssize_t const value = (type == TYP_INT) ? static_cast<int32_t>(bits) : static_cast<ssize_t>(bits);
                return static_cast<target_size_t>((value < 0) ? -value : value);
            };

            Encoding*     best          = nullptr;
            unsigned      bestCost      = UINT_MAX;
            unsigned      bestSize      = UINT_MAX;
            target_size_t bestMagnitude = ~static_cast<target_size_t>(0);
            for (Encoding& encoding : encodings)
            {
                unsigned            cost = 0;
                unsigned            size = 0;
                target_size_t const mag  = max(magnitude(encoding.offset), magnitude(encoding.bound));
                if (encoding.offset != 0)
                {
                    GenTree* const offsetNode = NewRangeConstant(comp, encoding.offset, type);
                    comp->gtSetEvalOrder(offsetNode);
                    cost += ConstantCost(offsetNode, false);
                    size += offsetNode->GetCostSz();
                }
                GenTree* const boundNode = NewRangeConstant(comp, encoding.bound, type);
                comp->gtSetEvalOrder(boundNode);
                cost += ConstantCost(boundNode, false);
                size += boundNode->GetCostSz();
                if ((cost < bestCost) || ((cost == bestCost) && (size < bestSize)) ||
                    ((cost == bestCost) && (size == bestSize) && (mag < bestMagnitude)))
                {
                    best          = &encoding;
                    bestCost      = cost;
                    bestSize      = size;
                    bestMagnitude = mag;
                }
            }

#ifdef TARGET_ARM64
            // Left alone, a pair of compares on locals becomes a cmp and ccmp chain, as many arithmetic
            // instructions as the fold's sub and cmp, so it competes on its constants like the encodings
            // do: the first bound in the cmp and the second in the ccmp. A tie goes to the fold.
            if ((symbolicBound == nullptr) && value1->gtEffectiveVal()->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
                value2->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
                (bestCost > (ConstantCost(bound1, false) + ConstantCost(bound2, true))))
            {
                return nullptr;
            }
#endif

            if (best->offset != 0)
            {
                value = comp->gtNewOperNode(GT_SUB, type, value, NewRangeConstant(comp, best->offset, type));
            }
            constant   = best->bound;
            oper       = best->oper;
            isUnsigned = best->isUnsigned;
        }

        newBound = NewRangeConstant(comp, constant, type);
    }
    // A load base is not wrapped: it is the first test's own load, kept in place.
    cmp1->gtOp1 =
        (value1 == value1->gtEffectiveVal()) ? value : comp->gtWrapWithSideEffects(value, value1, GTF_OBS_EFFECT);
    cmp1->gtOp2 = newBound;
    cmp1->SetOper(cmp2IsReversed ? GenTree::ReverseRelop(oper) : oper);
    if (isUnsigned)
        cmp1->SetUnsigned();
    else
        cmp1->ClearUnsigned();
    return cmp1;
}

//------------------------------------------------------------------------------
// optOptimizeRangeTests : Optimize two conditional blocks representing a constant range test.
//    E.g. "X >= 10 && X <= 100" is optimized to "(X - 10) <= 90".
//
// Return Value:
//    True if m_b1 and m_b2 are merged.
//
bool OptBoolsDsc::optOptimizeRangeTests()
{
    // At this point we have two consecutive conditional blocks (BBJ_COND): m_b1 and m_b2
    assert((m_b1 != nullptr) && (m_b2 != nullptr));
    assert(m_b1->KindIs(BBJ_COND) && m_b2->KindIs(BBJ_COND) && m_b1->FalseTargetIs(m_b2));

    if (m_b2->isRunRarely())
    {
        // We don't want to make the first comparison to be slightly slower
        // if the 2nd one is rarely executed.
        return false;
    }

    if (!BasicBlock::sameEHRegion(m_b1, m_b2) || m_b2->HasFlag(BBF_DONT_REMOVE))
    {
        // Conditions aren't in the same EH region or m_b2 can't be removed
        return false;
    }

    if (m_b1->TrueTargetIs(m_b1) || m_b1->TrueTargetIs(m_b2) || m_b2->TrueTargetIs(m_b2) || m_b2->TrueTargetIs(m_b1))
    {
        // Ignoring weird cases like a condition jumping to itself or when JumpDest == Next
        return false;
    }

    // We're interested in just two shapes for e.g. "X > 10 && X < 100" range test:
    //
    BasicBlock* notInRangeBb = m_b1->GetTrueTarget();
    BasicBlock* inRangeBb;
    weight_t    inRangeLikelihood = m_b1->GetFalseEdge()->getLikelihood();

    if (m_b2->TrueTargetIs(notInRangeBb))
    {
        // Shape 1: both conditions jump to NotInRange
        //
        // if (X <= 10)
        //     goto NotInRange;
        //
        // if (X >= 100)
        //     goto NotInRange
        //
        // InRange:
        // ...
        inRangeBb = m_b2->GetFalseTarget();
        inRangeLikelihood *= m_b2->GetFalseEdge()->getLikelihood();
    }
    else if (m_b2->FalseTargetIs(notInRangeBb))
    {
        // Shape 2: 2nd block jumps to InRange
        //
        // if (X <= 10)
        //     goto NotInRange;
        //
        // if (X > 100)
        //     goto InRange
        //
        // NotInRange:
        // ...
        inRangeBb = m_b2->GetTrueTarget();
        inRangeLikelihood *= m_b2->GetTrueEdge()->getLikelihood();
    }
    else
    {
        // Unknown shape
        return false;
    }

    if (!m_b2->hasSingleStmt() || (m_b2->GetUniquePred(m_compiler) != m_b1))
    {
        // The 2nd block has to be single-statement to avoid side-effects between the two conditions.
        // Also, make sure m_b2 has no other predecessors.
        return false;
    }

    // m_b1 and m_b2 are both BBJ_COND blocks with GT_JTRUE(cmp) root nodes
    GenTreeOp* cmp1 = m_b1->lastStmt()->GetRootNode()->gtGetOp1()->AsOp();
    GenTreeOp* cmp2 = m_b2->lastStmt()->GetRootNode()->gtGetOp1()->AsOp();

    // cmp1 is always reversed (see shape1 and shape2 above)
    const bool cmp1IsReversed = true;

    // cmp2 can be either reversed or not
    const bool cmp2IsReversed = m_b2->TrueTargetIs(notInRangeBb);

    GenTree* folded = FoldComparisons(m_compiler, cmp1, cmp1IsReversed, cmp2, cmp2IsReversed);
    if (folded != nullptr)
    {
        m_b1->lastStmt()->GetRootNode()->AsOp()->gtOp1 = folded;
    }
    else
    {
        return false;
    }

    // Re-direct firstBlock to jump to inRangeBb
    FlowEdge* const newEdge      = m_compiler->fgAddRefPred(inRangeBb, m_b1);
    FlowEdge* const oldFalseEdge = m_b1->GetFalseEdge();
    FlowEdge* const oldTrueEdge  = m_b1->GetTrueEdge();
    newEdge->setHeuristicBased(oldTrueEdge->isHeuristicBased());
    newEdge->setLikelihood(inRangeLikelihood);
    oldTrueEdge->setLikelihood(1.0 - inRangeLikelihood);

    if (!cmp2IsReversed)
    {
        m_b1->SetFalseEdge(oldTrueEdge);
        m_b1->SetTrueEdge(newEdge);
        assert(m_b1->TrueTargetIs(inRangeBb));
        assert(m_b1->FalseTargetIs(notInRangeBb));
    }
    else
    {
        m_b1->SetFalseEdge(newEdge);
        assert(m_b1->TrueTargetIs(notInRangeBb));
        assert(m_b1->FalseTargetIs(inRangeBb));
    }

    // Remove the 2nd condition block as we no longer need it
    m_compiler->fgRemoveRefPred(oldFalseEdge);
    m_compiler->fgRemoveBlock(m_b2, true);

    // Update profile
    if (m_b1->hasProfileWeight())
    {
        BasicBlock* const trueTarget  = m_b1->GetTrueTarget();
        BasicBlock* const falseTarget = m_b1->GetFalseTarget();
        trueTarget->setBBProfileWeight(trueTarget->computeIncomingWeight());
        falseTarget->setBBProfileWeight(falseTarget->computeIncomingWeight());

        if ((trueTarget->NumSucc() > 0) || (falseTarget->NumSucc() > 0))
        {
            JITDUMP("optOptimizeRangeTests: Profile needs to be propagated through " FMT_BB
                    "'s successors. Data %s inconsistent.\n",
                    m_b1->bbNum, m_compiler->fgPgoConsistent ? "is now" : "was already");
            m_compiler->fgPgoConsistent = false;
        }
    }

    m_compiler->fgFoldConditional(m_b1);
    Statement* const stmt = m_b1->lastStmt();
    if (stmt != nullptr)
    {
        m_compiler->gtSetStmtInfo(stmt);
        m_compiler->fgSetStmtSeq(stmt);
        m_compiler->gtUpdateStmtSideEffects(stmt);
    }
    return true;
}

//-----------------------------------------------------------------------------
//  optOptimizeCompareChainCondBlock:  Create a chain when both m_b1 and m_b2 are BBJ_COND.
//
//  Returns:
//      true if chain optimization is done and m_b1 and m_b2 are folded into m_b1, else false.
//
//  Assumptions:
//      m_b1 and m_b2 are set on entry.
//
//  Notes:
//
//      This aims to reduced the number of conditional jumps by joining cases when multiple
//      conditions gate the execution of a block.
//
//      Example 1:
//          If ( a > b || c == d) { x = y; }
//
//      Will be represented in IR as:
//
//      ------------ BB01 -> BB03 (cond), succs={BB02,BB03}
//      *  JTRUE (GT a,b)
//
//      ------------ BB02 -> BB04 (cond), preds={BB01} succs={BB03,BB04}
//      *  JTRUE (NE c,d)
//
//      ------------ BB03, preds={BB01, BB02} succs={BB04}
//      *  STORE_LCL_VAR<x>(y)
//
//      These operands will be combined into a single AND in the first block (with the first
//      condition inverted), wrapped by the test condition (NE(...,0)). Giving:
//
//      ------------ BB01 -> BB03 (cond), succs={BB03,BB04}
//      *  JTRUE (NE (AND (LE a,b), (NE c,d)), 0)
//
//      ------------ BB03, preds={BB01} succs={BB04}
//      *  STORE_LCL_VAR<x>(y)
//
//
//      Example 2:
//          If ( a > b && c == d) { x = y; } else { x = z; }
//
//      Here the && conditions are connected via an OR. After the pass:
//
//      ------------ BB01 -> BB03 (cond), succs={BB03,BB04}
//      *  JTRUE (NE (OR (LE a,b), (NE c,d)), 0)
//
//      ------------ BB03, preds={BB01} succs={BB05}
//      *  STORE_LCL_VAR<x>(y)
//
//      ------------ BB04, preds={BB01} succs={BB05}
//      *  STORE_LCL_VAR<x>(z)
//
//
//      Example 3:
//          If ( a > b || c == d || e < f ) { x = y; }
//      The first pass of the optimization will combine two of the conditions. The
//      second pass will then combine remaining condition the earlier chain.
//
//      ------------ BB01 -> BB03 (cond), succs={BB03,BB04}
//      *  JTRUE (NE (OR ((NE (OR (NE c,d), (GE e,f)), 0), (LE a,b))), 0)
//
//      ------------ BB03, preds={BB01} succs={BB04}
//      *  STORE_LCL_VAR<x>(y)
//
//
//     This optimization means that every condition within the IF statement is always evaluated,
//     as opposed to stopping at the first positive match.
//     Theoretically there is no maximum limit on the size of the generated chain. Therefore cost
//     checking is used to limit the maximum number of conditions that can be chained together.
//
bool OptBoolsDsc::optOptimizeCompareChainCondBlock()
{
    assert((m_b1 != nullptr) && (m_b2 != nullptr));

    bool foundEndOfOrConditions = false;
    if (m_b1->FalseTargetIs(m_b2) && m_b2->FalseTargetIs(m_b1->GetTrueTarget()))
    {
        // Found the end of two (or more) conditions being ORed together.
        // The final condition has been inverted.
        foundEndOfOrConditions = true;
    }
    else if (m_b1->FalseTargetIs(m_b2) && m_b1->TrueTargetIs(m_b2->GetTrueTarget()))
    {
        // Found two conditions connected together.
    }
    else
    {
        return false;
    }

    Statement* const s1 = optOptimizeBoolsChkBlkCond();
    if (s1 == nullptr)
    {
        return false;
    }
    Statement* s2 = m_b2->firstStmt();

    assert(m_testInfo1.testTree->OperIs(GT_JTRUE));
    GenTree* cond1 = m_testInfo1.testTree->gtGetOp1();
    assert(m_testInfo2.testTree->OperIs(GT_JTRUE));
    GenTree* cond2 = m_testInfo2.testTree->gtGetOp1();

    // Ensure both conditions are suitable.
    if (!cond1->OperIsCompare() || !cond2->OperIsCompare())
    {
        return false;
    }

    // The second condition will be evaluated unconditionally. The first already
    // executes on every path, so its operands can be evaluated before the chain.
    if ((cond2->gtFlags & GTF_OBS_EFFECT) != 0)
    {
        return false;
    }

    bool splitFirst = (cond1->gtFlags & GTF_OBS_EFFECT) != 0;

    // Integer compares only for now (until support for Arm64 fccmp instruction is added)
    if (varTypeIsFloating(cond1->gtGetOp1()) || varTypeIsFloating(cond2->gtGetOp1()))
    {
        return false;
    }

    // Check for previously optimized compare chains.
    bool op1IsTestCond;
    bool op2IsTestCond;
    bool op1IsCondChain = FindCompareChain(cond1, &op1IsTestCond);
    bool op2IsCondChain = FindCompareChain(cond2, &op2IsTestCond);

    // Avoid cases where optimizations in lowering will produce better code than optimizing here.
    if (op1IsTestCond || op2IsTestCond)
    {
        return false;
    }

    // Combining conditions means that all conditions are always fully evaluated.
    // Put a limit on the max size that can be combined.
    if (!m_compiler->compStressCompile(Compiler::STRESS_OPT_BOOLS_COMPARE_CHAIN_COST, 25))
    {
        int op1Cost = cond1->GetCostEx();
        int op2Cost = cond2->GetCostEx();
        // The cost of combing three simple conditions is 32.
        int maxOp1Cost = op1IsCondChain ? 31 : 7;
        int maxOp2Cost = op2IsCondChain ? 31 : 7;

        // Cost to allow for chain size of three. If we split the first condition,
        // its operand work stays outside the chain and executes as before.
        if ((!splitFirst && (op1Cost > maxOp1Cost)) || (op2Cost > maxOp2Cost))
        {
            JITDUMP("Skipping CompareChainCond that will evaluate conditions unconditionally at costs %d,%d\n", op1Cost,
                    op2Cost);
            return false;
        }
    }

#ifdef TARGET_ARM64
    // The chain turns the second compare into a ccmp, whose immediate is five bits where cmp has
    // twelve. The branch the chain removes pays for one instruction to materialize a constant that
    // no longer fits, more is a loss.
    GenTree* const cond2Constant = cond2->gtGetOp2();
    if (cond2Constant->IsCnsIntOrI() && (ConstantCost(cond2Constant, true) > (ConstantCost(cond2Constant, false) + 1)))
    {
        JITDUMP("Skipping CompareChainCond whose constant costs more in a ccmp than the branch it saves\n");
        return false;
    }
#endif

    if (splitFirst)
    {
        // In particular, CSE can embed a definition in the first comparison.
        // Split its operands in execution order, preserving their effects while
        // leaving a pure comparison that can participate in the chain.
        Statement* firstNewStmt;
        GenTree**  splitUse;
        m_compiler->gtSplitTree(m_b1, s1, cond1, &firstNewStmt, &splitUse);
        m_compiler->gtUpdateStmtSideEffects(s1);
        for (Statement* stmt = firstNewStmt; (stmt != nullptr) && (stmt != s1); stmt = stmt->GetNextStmt())
        {
            m_compiler->gtSetStmtInfo(stmt);
            m_compiler->fgSetStmtSeq(stmt);
        }
        m_compiler->gtSetStmtInfo(s1);
        m_compiler->fgSetStmtSeq(s1);
        assert((cond1->gtFlags & GTF_OBS_EFFECT) == 0);
    }

    // Remove the first JTRUE statement.
    constexpr bool isUnlink = true;
    m_compiler->fgRemoveStmt(m_b1, s1 DEBUGARG(isUnlink));

    // Invert the condition.
    if (foundEndOfOrConditions)
    {
        GenTree* revCond = m_compiler->gtReverseCond(cond1);
        assert(cond1 == revCond); // Ensure `gtReverseCond` did not create a new node.
    }

    // Join the two conditions together
    genTreeOps chainedOper       = foundEndOfOrConditions ? GT_AND : GT_OR;
    GenTree*   chainedConditions = m_compiler->gtNewOperNode(chainedOper, TYP_INT, cond1, cond2);
    cond1->gtFlags &= ~GTF_RELOP_JMP_USED;
    cond2->gtFlags &= ~GTF_RELOP_JMP_USED;
    chainedConditions->gtFlags |= (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

    // Add a test condition onto the front of the chain
    GenTree* testcondition =
        m_compiler->gtNewOperNode(GT_NE, TYP_INT, chainedConditions, m_compiler->gtNewZeroConNode(TYP_INT));

    // Wire the chain into the second block
    m_testInfo2.SetTestOp(testcondition);
    m_testInfo2.testTree->AsOp()->gtFlags |= (testcondition->gtFlags & GTF_ALL_EFFECT);
    m_compiler->gtSetEvalOrder(m_testInfo2.testTree);
    m_compiler->fgSetStmtSeq(s2);

    // Update the flow.
    FlowEdge* const removedEdge  = m_b1->GetTrueEdge();
    FlowEdge* const retainedEdge = m_b1->GetFalseEdge();

    // Will need to re-adjust the likelihoods of the outgoing edges of the combined b1+b2 block
    const weight_t    removedEdgeLikelihood = removedEdge->getLikelihood();
    const weight_t    fallthroughLikelihood = retainedEdge->getLikelihood();
    BasicBlock* const b1RemovedTarget       = removedEdge->getDestinationBlock();

    // b1 is b2's only predecessor and b1->b2 becomes unconditional, so the combined block, which
    // "fgCompactBlock" gives b2's weight, runs as often as b1. Don't decrement the removed-edge-target's
    // weight because the same control flow still reaches that block.
    m_b2->inheritWeight(m_b1);

    m_compiler->fgRemoveRefPred(removedEdge);
    m_b1->SetKindAndTargetEdge(BBJ_ALWAYS, retainedEdge);

    // The combined b1+b2 block reuses b2's edges, and their likelihoods must be prorated
    // based on likelihood of b1->b2 (fallthrough) control flow.
    // The edge to shared target (b1RemovedTarget) must also take into account b1->shared
    // edge likelihood
    FlowEdge* const b2Edges[] = {m_b2->GetTrueEdge(), m_b2->GetFalseEdge()};
    for (FlowEdge* const b2Edge : b2Edges)
    {
        weight_t combined = fallthroughLikelihood * b2Edge->getLikelihood();
        if (b2Edge->getDestinationBlock() == b1RemovedTarget)
        {
            combined += removedEdgeLikelihood;
        }
        b2Edge->setLikelihood(min(1.0, combined));
    }

    // Fixup flags.
    m_b2->CopyFlags(m_b1, BBF_COPY_PROPAGATE);

    // Join the two blocks. This is done now to ensure that additional conditions can be chained.
    if (m_compiler->fgCanCompactBlock(m_b1))
    {
        m_compiler->fgCompactBlock(m_b1);
    }

#ifdef DEBUG
    if (m_compiler->verbose)
    {
        JITDUMP("\nCombined conditions " FMT_BB " and " FMT_BB " into %s chain :\n", m_b1->bbNum, m_b2->bbNum,
                GenTree::OpName(chainedOper));
        m_compiler->fgDumpBlock(m_b1);
        JITDUMP("\n");
    }
#endif

    return true;
}

//-----------------------------------------------------------------------------
// optOptimizeBoolsChkBlkCond: Checks block conditions if it can be boolean optimized
//
// Return:
//      If all conditions pass, returns the last statement of m_b1, else return nullptr.
//
// Notes:
//      This method checks if the second (and third block for cond/return/return case) contains only one statement,
//      and checks if tree operators are of the right type, e.g, GT_JTRUE, GT_RETURN, GT_SWIFT_ERROR_RET.
//
//      On entry, m_b1, m_b2 are set and m_b3 is set for cond/return/return case.
//      If it passes all the conditions, m_testInfo1.testTree, m_testInfo2.testTree and m_t3 are set
//      to the root nodes of m_b1, m_b2 and m_b3 each.
//      SameTarget is also updated to true if m_b1 and m_b2 jump to the same destination.
//
Statement* OptBoolsDsc::optOptimizeBoolsChkBlkCond()
{
    assert(m_b1 != nullptr && m_b2 != nullptr);

    // Find the block conditions of m_b1 and m_b2

    if (m_b2->countOfInEdges() > 1)
    {
        return nullptr;
    }

    // Find the condition for the first block

    Statement* s1 = m_b1->lastStmt();

    GenTree* testTree1 = s1->GetRootNode();
    assert(testTree1->OperIs(GT_JTRUE));

    // The second and the third block must contain a single statement

    Statement* s2 = m_b2->firstStmt();
    if (s2->GetPrevStmt() != s2)
    {
        return nullptr;
    }

    GenTree* testTree2 = s2->GetRootNode();
    assert(testTree2->OperIs(GT_JTRUE));

    m_testInfo1.testStmt = s1;
    m_testInfo1.testTree = testTree1;
    m_testInfo2.testStmt = s2;
    m_testInfo2.testTree = testTree2;

    return s1;
}

//-----------------------------------------------------------------------------
// optOptimizeBoolsChkTypeCostCond: Checks if type conditions meet the folding condition, and
//                                  if cost to fold is not too expensive
//
// Return:
//      True if it meets type conditions and cost conditions.	Else false.
//
bool OptBoolsDsc::optOptimizeBoolsChkTypeCostCond()
{
    assert(m_testInfo1.compTree->OperIs(GT_EQ, GT_NE, GT_LT, GT_GT, GT_GE, GT_LE) &&
           m_testInfo1.compTree->AsOp()->gtOp1 == m_c1);
    assert(m_testInfo2.compTree->OperIs(GT_EQ, GT_NE, GT_LT, GT_GT, GT_GE, GT_LE) &&
           m_testInfo2.compTree->AsOp()->gtOp1 == m_c2);

    //
    // Leave out floats where the bit-representation is more complicated
    // - there are two representations for 0.
    //
    if (varTypeIsFloating(m_c1->TypeGet()) || varTypeIsFloating(m_c2->TypeGet()))
    {
        return false;
    }

    // Make sure the types involved are of the same sizes
    if (genTypeSize(m_c1->TypeGet()) != genTypeSize(m_c2->TypeGet()))
    {
        return false;
    }
    if (genTypeSize(m_testInfo1.compTree->TypeGet()) != genTypeSize(m_testInfo2.compTree->TypeGet()))
    {
        return false;
    }
#ifdef TARGET_ARMARCH
    // Skip the small operand which we cannot encode.
    if (varTypeIsSmall(m_c1->TypeGet()))
        return false;
#endif
    // The second condition must not contain side effects
    //
    if (m_c2->gtFlags & GTF_GLOB_EFFECT)
    {
        return false;
    }

    // The second condition must not be too expensive
    //
    if (m_c2->GetCostEx() > 12)
    {
        return false;
    }

    return true;
}

//-----------------------------------------------------------------------------
// optOptimizeBoolsUpdateTrees: Fold the trees based on fold type and comparison type,
//                              update the edges, and unlink removed blocks
//
void OptBoolsDsc::optOptimizeBoolsUpdateTrees()
{
    assert(m_b1 != nullptr && m_b2 != nullptr);
    assert(m_cmpOp != GT_NONE && m_c1 != nullptr && m_c2 != nullptr);

    GenTree* cmpOp1 = m_foldOp == GT_NONE ? m_c1 : m_compiler->gtNewOperNode(m_foldOp, m_foldType, m_c1, m_c2);

    GenTree* t1Comp = m_testInfo1.compTree;
    t1Comp->SetOper(m_cmpOp);
    t1Comp->AsOp()->gtOp1         = cmpOp1;
    t1Comp->AsOp()->gtOp2->gtType = m_foldType; // Could have been varTypeIsGC()

    // Recost/rethread the tree if necessary
    //
    if (m_compiler->fgNodeThreading != NodeThreading::None)
    {
        m_compiler->gtSetStmtInfo(m_testInfo1.testStmt);
        m_compiler->fgSetStmtSeq(m_testInfo1.testStmt);
        m_compiler->gtUpdateStmtSideEffects(m_testInfo1.testStmt);
    }

    /* Modify the target of the conditional jump and update bbRefs and bbPreds */

    {
        // Modify b1, if necessary, so it has the same
        // true target as b2.
        //
        FlowEdge* const origB1TrueEdge  = m_b1->GetTrueEdge();
        FlowEdge* const origB2TrueEdge  = m_b2->GetTrueEdge();
        FlowEdge* const origB2FalseEdge = m_b2->GetFalseEdge();

        weight_t const origB1TrueLikelihood = origB1TrueEdge->getLikelihood();
        weight_t       newB1TrueLikelihood  = 0;

        if (m_sameTarget)
        {
            // We originally reached B2's true target via
            // B1 true OR B1 false B2 true.
            //
            newB1TrueLikelihood = origB1TrueLikelihood + (1.0 - origB1TrueLikelihood) * origB2TrueEdge->getLikelihood();
        }
        else
        {
            // We originally reached B2's true target only via
            // B1 false, then B2 true.
            //
            // We will now reach via B1 true.
            // Modify flow for true side of B1
            //
            m_compiler->fgRedirectEdge(m_b1->TrueEdgeRef(), m_b2->GetTrueTarget());
            origB1TrueEdge->setHeuristicBased(origB2TrueEdge->isHeuristicBased());

            newB1TrueLikelihood = (1.0 - origB1TrueLikelihood) * origB2TrueEdge->getLikelihood();
        }

        // Fix B1 true edge likelihood
        //
        origB1TrueEdge->setLikelihood(newB1TrueLikelihood);

        assert(m_b1->KindIs(BBJ_COND));
        assert(m_b2->KindIs(BBJ_COND));
        assert(m_b1->TrueTargetIs(m_b2->GetTrueTarget()));
        assert(m_b1->FalseTargetIs(m_b2));

        // We now reach B2's false target via B1 false.
        //
        m_compiler->fgReplacePred(origB2FalseEdge, m_b1);
        m_compiler->fgRemoveRefPred(origB2TrueEdge);
        FlowEdge* const newB1FalseEdge = origB2FalseEdge;
        m_b1->SetFalseEdge(newB1FalseEdge);

        // Fix B1 false edge likelihood
        //
        newB1FalseEdge->setLikelihood(1.0 - newB1TrueLikelihood);
        newB1FalseEdge->setHeuristicBased(origB1TrueEdge->isHeuristicBased());

        // Update profile
        if (m_b1->hasProfileWeight())
        {
            BasicBlock* const trueTarget  = origB1TrueEdge->getDestinationBlock();
            BasicBlock* const falseTarget = newB1FalseEdge->getDestinationBlock();
            trueTarget->setBBProfileWeight(trueTarget->computeIncomingWeight());
            falseTarget->setBBProfileWeight(falseTarget->computeIncomingWeight());

            if ((trueTarget->NumSucc() > 0) || (falseTarget->NumSucc() > 0))
            {
                JITDUMP("optOptimizeRangeTests: Profile needs to be propagated through " FMT_BB
                        "'s successors. Data %s inconsistent.\n",
                        m_b1->bbNum, m_compiler->fgPgoConsistent ? "is now" : "was already");
                m_compiler->fgPgoConsistent = false;
            }
        }
    }

    // Get rid of the second block

    m_compiler->fgUnlinkBlockForRemoval(m_b2);
    m_b2->SetFlags(BBF_REMOVED);
    // If m_b2 was the last block of a try or handler, update the EH table.
    m_compiler->ehUpdateForDeletedBlock(m_b2);

    // Update IL range of first block
    m_b1->bbCodeOffsEnd = m_b2->bbCodeOffsEnd;
}

//-----------------------------------------------------------------------------
//  optOptimizeBoolsGcStress: Replace x==null with (x|x)==0 if x is a GC-type.
//                            This will stress code-gen and the emitter to make sure they support such trees.
//
#ifdef DEBUG

void OptBoolsDsc::optOptimizeBoolsGcStress()
{
    if (!m_compiler->compStressCompile(m_compiler->STRESS_OPT_BOOLS_GC, 20))
    {
        return;
    }

    assert(m_b1->KindIs(BBJ_COND));
    Statement* const stmt = m_b1->lastStmt();
    GenTree* const   cond = stmt->GetRootNode();

    assert(cond->OperIs(GT_JTRUE));

    OptTestInfo test;
    test.testStmt = stmt;
    test.testTree = cond;

    GenTree* comparand = optIsBoolComp(&test);

    if (comparand == nullptr || !varTypeIsGC(comparand->TypeGet()))
    {
        return;
    }
    GenTree* relop  = test.compTree;
    bool     isBool = test.isBool;

    if (comparand->gtFlags & (GTF_ASG | GTF_CALL | GTF_ORDER_SIDEEFF))
    {
        return;
    }

    GenTree* comparandClone = m_compiler->gtCloneExpr(comparand);

    noway_assert(relop->AsOp()->gtOp1 == comparand);
    genTreeOps oper      = m_compiler->compStressCompile(m_compiler->STRESS_OPT_BOOLS_GC, 50) ? GT_OR : GT_AND;
    relop->AsOp()->gtOp1 = m_compiler->gtNewOperNode(oper, TYP_I_IMPL, comparand, comparandClone);

    // Comparand type is already checked, and we have const int, there is no harm
    // morphing it into a TYP_I_IMPL.
    noway_assert(relop->AsOp()->gtOp2->OperIs(GT_CNS_INT));
    relop->AsOp()->gtOp2->gtType = TYP_I_IMPL;

    // Recost/rethread the tree if necessary
    //
    if (m_compiler->fgNodeThreading != NodeThreading::None)
    {
        m_compiler->gtSetStmtInfo(test.testStmt);
        m_compiler->fgSetStmtSeq(test.testStmt);
    }
}

#endif

//-----------------------------------------------------------------------------
// optIsBoolComp:   Function used by folding of boolean conditionals
//
// Arguments:
//      pOptTest    The test info for the test tree
//
// Return:
//      On success, return the first operand (gtOp1) of compTree, else return nullptr.
//
// Notes:
//      On entry, testTree is set.
//      On success, compTree is set to the compare node (i.e. GT_EQ or GT_NE or GT_LT or GT_GE) of the testTree.
//      isBool is set to true if the comparand (i.e., operand 1 of compTree is boolean. Otherwise, false.
//
//      Given a GT_JTRUE or GT_RETURN/GT_SWIFT_ERROR_RET node, this method checks if it is a boolean comparison
//      of the form "if (boolVal ==/!=/>=/<  0/1)".This is translated into
//      a GT_EQ/GT_NE/GT_GE/GT_LT node with "opr1" being a boolean lclVar and "opr2" the const 0/1.
//
//      When isBool == true, if the comparison was against a 1 (i.e true)
//      then we morph the tree by reversing the GT_EQ/GT_NE/GT_GE/GT_LT and change the 1 to 0.
//
GenTree* OptBoolsDsc::optIsBoolComp(OptTestInfo* pOptTest)
{
    pOptTest->isBool = false;

    assert(pOptTest->testTree->OperIs(GT_JTRUE, GT_RETURN, GT_SWIFT_ERROR_RET));
    GenTree* cond = pOptTest->GetTestOp();

    // The condition must be "!= 0" or "== 0" or >=0 or <= 0 or > 0 or < 0
    if (!cond->OperIs(GT_EQ, GT_NE, GT_LT, GT_GT, GT_GE, GT_LE))
    {
        return nullptr;
    }

    // Return the compare node to the caller

    pOptTest->compTree = cond;

    // Get hold of the comparands

    GenTree* opr1 = cond->AsOp()->gtOp1;
    GenTree* opr2 = cond->AsOp()->gtOp2;

    if (!opr2->OperIs(GT_CNS_INT))
    {
        return nullptr;
    }

    if (!opr2->IsIntegralConst(0) && !opr2->IsIntegralConst(1))
    {
        return nullptr;
    }

    ssize_t ival2 = opr2->AsIntCon()->IconValue();

    // Is the value a boolean?
    // Currently we only recognize constant 0/1.

    if (opr1->IsIntegralConst(0) || opr1->IsIntegralConst(1))
    {
        pOptTest->isBool = true;
    }

    // Was our comparison against the constant 1 (i.e. true)
    if (ival2 == 1)
    {
        // If this is a boolean expression tree we can reverse the relop
        // and change the true to false.
        if (pOptTest->isBool)
        {
            m_compiler->gtReverseCond(cond);
            opr2->AsIntCon()->SetIconValue(0);
        }
        else
        {
            return nullptr;
        }
    }

    return opr1;
}

//-----------------------------------------------------------------------------
// optOptimizeBools:    Folds boolean conditionals for GT_JTRUE/GT_RETURN/GT_SWIFT_ERROR_RET nodes
//
// Returns:
//    suitable phase status
//
// Notes:
//      If the operand of GT_JTRUE/GT_RETURN/GT_SWIFT_ERROR_RET node is GT_EQ/GT_NE/GT_GE/GT_LE/GT_GT/GT_LT of the form
//      "if (boolVal ==/!=/>=/<  0/1)", the GT_EQ/GT_NE/GT_GE/GT_LE/GT_GT/GT_LT nodes are translated into a
//      GT_EQ/GT_NE/GT_GE/GT_LE/GT_GT/GT_LT node with
//          "op1" being a boolean GT_OR/GT_AND lclVar and
//          "op2" the const 0/1.
//      For example, the folded tree for the below boolean optimization is shown below:
//      Case 1:     (x == 0 && y ==0) => (x | y) == 0
//          *  RETURN   int
//          \--*  EQ        int
//             +--*  OR         int
//             |  +--*  LCL_VAR     int     V00 arg0
//             |  \--*  LCL_VAR     int     V01 arg1
//             \--*  CNS_INT    int     0
//
//      Case 2:     (x == null && y == null) ==> (x | y) == 0
//          *  RETURN    int
//          \-- * EQ        int
//              + -- * OR        long
//              |    +-- * LCL_VAR   ref    V00 arg0
//              |    \-- * LCL_VAR   ref    V01 arg1
//              \-- * CNS_INT   long   0
//
//      Case 3:     (x == 0 && y == 0 && z == 0) ==> ((x | y) | z) == 0
//          *  RETURN    int
//          \-- * EQ        int
//              + -- * OR        int
//              |    +-- * OR        int
//              |    |   +-- * LCL_VAR   int    V00 arg0
//              |    |   \-- * LCL_VAR   int    V01 arg1
//              |    \-- * LCL_VAR   int    V02 arg2
//              \-- * CNS_INT   int    0
//
//      Case 4:     (x == 0 && y == 0 && z == 0 && w == 0) ==> (((x | y) | z) | w) == 0
//          *  RETURN    int
//          \-- *  EQ        int
//              +  *  OR        int
//              |  +--*  OR        int
//              |  |  +--*  OR        int
//              |  |  |  +--*  LCL_VAR   int    V00 arg0
//              |  |  |  \--*  LCL_VAR   int    V01 arg1
//              |  |  \--*  LCL_VAR   int    V02 arg2
//              |  \--*  LCL_VAR   int    V03 arg3
//              \--*  CNS_INT   int    0
//
//      Case 5:     (x != 0 && y != 0) => (x | y) != 0
//          *  RETURN   int
//          \--*  NE        int
//             +--*  OR         int
//             |  +--*  LCL_VAR     int     V00 arg0
//             |  \--*  LCL_VAR     int     V01 arg1
//             \--*  CNS_INT    int     0
//
//      Case 6:     (x >= 0 && y >= 0) => (x | y) >= 0
//          *  RETURN   int
//          \--*  GE        int
//             +--*  OR         int
//             |  +--*  LCL_VAR     int     V00 arg0
//             |  \--*  LCL_VAR     int     V01 arg1
//             \--*  CNS_INT    int     0
//
//      Case 7:     (x < 0 || y < 0) => (x & y) < 0
//          *  RETURN   int
//          \--*  LT        int
//             +--*  AND         int
//             |  +--*  LCL_VAR     int     V00 arg0
//             |  \--*  LCL_VAR     int     V01 arg1
//             \--*  CNS_INT    int     0
//
//      Case 8:     (x < 0 || x == 0) => x <= 0
//          *  RETURN   int
//          \--*  LE        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Case 9:     (x == 0 || x < 0) => x <= 0
//          *  RETURN   int
//          \--*  LE        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Case 10:     (x > 0 || x == 0) => x >= 0
//          *  RETURN   int
//          \--*  GE        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Case 11:     (x == 0 || x > 0) => x >= 0
//          *  RETURN   int
//          \--*  GE        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Case 12:     (x >= 0 && x != 0) => x > 0
//          *  RETURN   int
//          \--*  GT        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Case 13:     (x != 0 && x >= 0) => x > 0
//          *  RETURN   int
//          \--*  GT        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Case 14:     (x <= 0 && x != 0) => x < 0
//          *  RETURN   int
//          \--*  LT        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Case 15:     (x != 0 && x <= 0) => x < 0
//          *  RETURN   int
//          \--*  LT        int
//             +--*  LCL_VAR    int     V00 arg0
//             \--*  CNS_INT    int     0
//
//      Patterns that are not optimized include (x == 1 && y == 1), (x == 1 || y == 1),
//      (x == 0 || y == 0) because currently their comptree is not marked as boolean expression.
//      When m_foldOp == GT_AND or m_cmpOp == GT_NE, both compTrees must be boolean expression
//      in order to skip below cases when compTree is not boolean expression:
//          - x == 1 && y == 1 ==> (x&y)!=0: Skip cases where x or y is greater than 1, e.g., x=3, y=1
//          - x == 1 || y == 1 ==> (x|y)!=0: Skip cases where either x or y is greater than 1, e.g., x=2, y=0
//          - x == 0 || y == 0 ==> (x&y)==0: Skip cases where x and y have opposite bits set, e.g., x=2, y=1
//
PhaseStatus Compiler::optOptimizeBools()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optOptimizeBools()\n");
    }
#endif
    bool         change    = false;
    bool         retry     = false;
    unsigned     numCond   = 0;
    unsigned     numPasses = 0;
    unsigned     stress    = false;
    BitVecTraits ccmpTraits(fgBBNumMax + 1, this);
    BitVec       ccmpVec = BitVecOps::MakeEmpty(&ccmpTraits);

    do
    {
        numPasses++;
        change = false;

        for (BasicBlock* b1 = fgFirstBB; b1 != nullptr; b1 = retry ? b1 : b1->Next())
        {
            retry = false;
            if (b1->KindIs(BBJ_COND) && fgFoldCondToReturnBlock(b1))
            {
                change = true;
                numCond++;
            }

            // We're only interested in conditional jumps here

            if (!b1->KindIs(BBJ_COND))
            {
                continue;
            }

            // If there is no next block, we're done

            BasicBlock* b2 = b1->GetFalseTarget();
            if (b2 == nullptr)
            {
                break;
            }

            // The next block must not be marked as BBF_DONT_REMOVE
            if (b2->HasFlag(BBF_DONT_REMOVE))
            {
                continue;
            }

            OptBoolsDsc optBoolsDsc(b1, b2, this);

            // The next block needs to be a condition or return block.

            if (b2->KindIs(BBJ_COND))
            {
                if (!b1->TrueTargetIs(b2->GetTrueTarget()) && !b2->FalseTargetIs(b1->GetTrueTarget()))
                {
                    continue;
                }

                // When it is conditional jumps

                if (optBoolsDsc.optOptimizeBoolsCondBlock())
                {
                    change = true;
                    numCond++;
                }
                else if (optBoolsDsc.optOptimizeRangeTests())
                {
                    change = true;
                    retry  = true;
                    numCond++;
                }
#ifdef TARGET_ARM64
                else if (optBoolsDsc.optOptimizeCompareChainCondBlock())
                {
                    // The optimization will have merged b1 and b2. Retry the loop so that
                    // b1 and b2->bbNext can be tested.
                    change = true;
                    retry  = true;
                    numCond++;
                }
#elif defined(TARGET_AMD64)
                // todo-xarch-apx: when we have proper CPUID (hardware) support, we can switch the below from an OR
                // condition to an AND, for now, `JitConfig.JitEnableApxIfConv` will drive whether the optimization
                // trigger or not
                // else if ((compOpportunisticallyDependsOn(InstructionSet_APX) || JitConfig.JitEnableApxIfConv()) &&
                // optBoolsDsc.optOptimizeCompareChainCondBlock())
                else if (canUseApxEvexEncoding() && JitConfig.EnableApxConditionalChaining() &&
                         !optSwitchDetectAndConvert(b1, true, &ccmpVec) &&
                         optBoolsDsc.optOptimizeCompareChainCondBlock())
                {
                    // The optimization will have merged b1 and b2. Retry the loop so that
                    // b1 and b2->bbNext can be tested.
                    change = true;
                    retry  = true;
                    numCond++;
                }

#endif
            }
            else
            {
#ifdef DEBUG
                optBoolsDsc.optOptimizeBoolsGcStress();
                stress = true;
#endif
            }
        }
    } while (change);

    JITDUMP("\noptimized %u BBJ_COND cases in %u passes\n", numCond, numPasses);

    const bool modified = stress || (numCond > 0);
    return modified ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//-------------------------------------------------------------
// fgFoldCondToReturnBlock: Folds BBJ_COND <relop> into BBJ_RETURN <relop>
//   This operation is the opposite of what fgDedupReturnComparison does.
//   We don't fold such conditionals if both return blocks have multiple predecessors.
//
// Arguments:
//    block - the BBJ_COND block to convert into BBJ_RETURN <relop>
//
// Returns:
//    true if the block was converted into BBJ_RETURN <relop>
//
bool Compiler::fgFoldCondToReturnBlock(BasicBlock* block)
{
    bool modified = false;

    assert(block->KindIs(BBJ_COND));

#ifdef JIT32_GCENCODER
    // JIT32_GCENCODER has a hard limit on the number of epilogues.
    return modified;
#endif

    // Early out if the current method is not returning a boolean.
    if ((info.compRetType != TYP_UBYTE))
    {
        return modified;
    }

    // Both edges must be BBJ_RETURN
    BasicBlock* const retFalseBb = block->GetFalseTarget();
    BasicBlock* const retTrueBb  = block->GetTrueTarget();

    // We might want to compact BBJ_ALWAYS blocks first,
    // but don't compact the conditional block away in the process
    if (fgCanCompactBlock(retTrueBb) && !retTrueBb->TargetIs(block))
    {
        fgCompactBlock(retTrueBb);
        modified = true;
    }
    // By the time we get to the retFalseBb, it might be removed by fgCompactBlock()
    // so we need to check if it is still valid.
    if (!retFalseBb->HasFlag(BBF_REMOVED) && fgCanCompactBlock(retFalseBb) && !retFalseBb->TargetIs(block))
    {
        fgCompactBlock(retFalseBb);
        modified = true;
    }
    // Same here - bail out if the block is no longer BBJ_COND after compacting.
    if (!block->KindIs(BBJ_COND))
    {
        return modified;
    }

    assert(block->TrueTargetIs(retTrueBb));
    assert(block->FalseTargetIs(retFalseBb));
    if (!retTrueBb->KindIs(BBJ_RETURN) || !retFalseBb->KindIs(BBJ_RETURN) ||
        !BasicBlock::sameEHRegion(block, retTrueBb) || !BasicBlock::sameEHRegion(block, retFalseBb) ||
        (retTrueBb == genReturnBB) || (retFalseBb == genReturnBB))
    {
        // Both edges must be BBJ_RETURN
        return modified;
    }

    // The last statement has to be either JTRUE(cond) or JTRUE(comma(cond)),
    // but let's be resilient just in case.
    assert(block->lastStmt() != nullptr);
    GenTree* node = block->lastStmt()->GetRootNode();
    GenTree* cond = node->gtGetOp1();
    if (!cond->OperIsCompare())
    {
        return modified;
    }
    assert(cond->TypeIs(TYP_INT));

    if ((retTrueBb->GetUniquePred(this) == nullptr) && (retFalseBb->GetUniquePred(this) == nullptr))
    {
        // Both return blocks have multiple predecessors - bail out.
        // We don't want to introduce a new epilogue.
        return modified;
    }

    // Is block a BBJ_RETURN(1/0) ?
    auto isReturnBool = [](const BasicBlock* block, bool value) {
        if (block->KindIs(BBJ_RETURN) && (block->lastStmt() != nullptr))
        {
            GenTree* node = block->lastStmt()->GetRootNode();
            if (!(node->OperIs(GT_RETURN) && node->gtGetOp1()->IsIntegralConst(value ? 1 : 0)))
            {
                return false;
            }
            // Allow preceding statements if they have no globally visible side effects
            // (e.g., dead local stores left over from inlining).
            for (Statement* const stmt : block->Statements())
            {
                if (GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(stmt->GetRootNode()->gtFlags))
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    };

    // Make sure we deal with true/false return blocks (or false/true)
    bool retTrueFalse = isReturnBool(retTrueBb, true) && isReturnBool(retFalseBb, false);
    bool retFalseTrue = isReturnBool(retTrueBb, false) && isReturnBool(retFalseBb, true);
    if (!retTrueFalse && !retFalseTrue)
    {
        return modified;
    }

    // Reverse the condition if we jump to "return false" on true.
    if (retFalseTrue)
    {
        gtReverseCond(cond);
    }
    modified = true;

    // Decrease the weight of the return blocks since we no longer have edges to them.
    // Although one might still be reachable from other blocks.
    if (retTrueBb->hasProfileWeight())
    {
        retTrueBb->decreaseBBProfileWeight(block->GetTrueEdge()->getLikelyWeight());
    }
    if (retFalseBb->hasProfileWeight())
    {
        retFalseBb->decreaseBBProfileWeight(block->GetFalseEdge()->getLikelyWeight());
    }

    // Unlink the return blocks
    fgRemoveRefPred(block->GetTrueEdge());
    fgRemoveRefPred(block->GetFalseEdge());
    block->SetKindAndTargetEdge(BBJ_RETURN);
    node->ChangeOper(GT_RETURN);
    node->ChangeType(TYP_INT);
    cond->gtFlags &= ~GTF_RELOP_JMP_USED;

    block->bbCodeOffsEnd = max(retTrueBb->bbCodeOffsEnd, retFalseBb->bbCodeOffsEnd);
    gtSetStmtInfo(block->lastStmt());
    fgSetStmtSeq(block->lastStmt());
    gtUpdateStmtSideEffects(block->lastStmt());

    JITDUMP("fgFoldCondToReturnBlock: folding " FMT_BB " from BBJ_COND into BBJ_RETURN:", block->bbNum);
    DISPBLOCK(block)
    return modified;
}
