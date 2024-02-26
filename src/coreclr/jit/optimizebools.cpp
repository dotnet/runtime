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

/*****************************************************************************/

//-----------------------------------------------------------------------------
// OptTestInfo:     Member of OptBoolsDsc struct used to test if a GT_JTRUE or GT_RETURN node
//                  is a boolean comparison
//
struct OptTestInfo
{
    Statement* testStmt; // Last statement of the basic block
    GenTree*   testTree; // The root node of the testStmt (GT_JTRUE or GT_RETURN).
    GenTree*   compTree; // The compare node (i.e. GT_EQ or GT_NE node) of the testTree
    bool       isBool;   // If the compTree is boolean expression
};

//-----------------------------------------------------------------------------
// OptBoolsDsc:     Descriptor used for Boolean Optimization
//
class OptBoolsDsc
{
public:
    OptBoolsDsc(BasicBlock* b1, BasicBlock* b2, Compiler* comp)
    {
        m_b1   = b1;
        m_b2   = b2;
        m_b3   = nullptr;
        m_comp = comp;
    }

private:
    BasicBlock* m_b1; // The first basic block with the BBJ_COND conditional jump type
    BasicBlock* m_b2; // The next basic block of m_b1. Either BBJ_COND or BBJ_RETURN type
    BasicBlock* m_b3; // m_b1's target block. Null if m_b2 is not a return block.

    Compiler* m_comp; // The pointer to the Compiler instance

    OptTestInfo m_testInfo1; // The first test info
    OptTestInfo m_testInfo2; // The second test info
    GenTree*    m_t3;        // The root node of the first statement of m_b3

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
    bool optOptimizeBoolsReturnBlock(BasicBlock* b3);
#ifdef DEBUG
    void optOptimizeBoolsGcStress();
#endif

private:
    Statement* optOptimizeBoolsChkBlkCond();
    GenTree* optIsBoolComp(OptTestInfo* pOptTest);
    bool optOptimizeBoolsChkTypeCostCond();
    void optOptimizeBoolsUpdateTrees();
    bool FindCompareChain(GenTree* condition, bool* isTestCondition);
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
//          For example, (x == 0 && y == 0 && z == 0) generates
//              B1: GT_JTRUE (BBJ_COND), jump to B4
//              B2: GT_JTRUE (BBJ_COND), jump to B4
//              B3: GT_RETURN (BBJ_RETURN)
//              B4: GT_RETURN (BBJ_RETURN)
//          and B1 and B2 are folded into B1:
//              B1: GT_JTRUE (BBJ_COND), jump to B4
//              B3: GT_RETURN (BBJ_RETURN)
//              B4: GT_RETURN (BBJ_RETURN)
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
    assert(m_b1 != nullptr && m_b2 != nullptr && m_b3 == nullptr);

    // Check if m_b1 and m_b2 jump to the same target and get back pointers to m_testInfo1 and t2 tree nodes

    m_t3 = nullptr;

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
        if (m_c1->gtOper == GT_LCL_VAR && m_c2->gtOper == GT_LCL_VAR &&
            m_c1->AsLclVarCommon()->GetLclNum() == m_c2->AsLclVarCommon()->GetLclNum())
        {
            if ((m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_EQ) ||
                (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_LT))
            {
                // Case: t1:c1<0 t2:c1==0
                // So we will branch to BX if c1<=0
                //
                // Case: t1:c1==0 t2:c1<0
                // So we will branch to BX if c1<=0
                cmpOp = GT_LE;
            }
            else if ((m_testInfo1.compTree->gtOper == GT_GT && m_testInfo2.compTree->gtOper == GT_EQ) ||
                     (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_GT))
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
        else if (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_EQ)
        {
            // t1:c1==0 t2:c2==0 ==> Branch to BX if either value is 0
            // So we will branch to BX if (c1&c2)==0

            foldOp = GT_AND;
            cmpOp  = GT_EQ;
        }
        else if (m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_LT &&
                 (!m_testInfo1.testTree->AsOp()->gtOp1->IsUnsigned() &&
                  !m_testInfo2.testTree->AsOp()->gtOp1->IsUnsigned()))
        {
            // t1:c1<0 t2:c2<0 ==> Branch to BX if either value < 0
            // So we will branch to BX if (c1|c2)<0

            foldOp = GT_OR;
            cmpOp  = GT_LT;
        }
        else if (m_testInfo1.compTree->gtOper == GT_NE && m_testInfo2.compTree->gtOper == GT_NE)
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
        if (m_c1->gtOper == GT_LCL_VAR && m_c2->gtOper == GT_LCL_VAR &&
            m_c1->AsLclVarCommon()->GetLclNum() == m_c2->AsLclVarCommon()->GetLclNum())
        {
            if ((m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_NE) ||
                (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_GE))
            {
                // Case: t1:c1<0 t2:c1!=0
                // So we will branch to BX if c1>0
                //
                // Case: t1:c1==0 t2:c1>=0
                // So we will branch to BX if c1>0
                cmpOp = GT_GT;
            }
            else if ((m_testInfo1.compTree->gtOper == GT_GT && m_testInfo2.compTree->gtOper == GT_NE) ||
                     (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_LE))
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
        else if (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_NE)
        {
            // t1:c1==0 t2:c2!=0 ==> Branch to BX if both values are non-0
            // So we will branch to BX if (c1&c2)!=0

            foldOp = GT_AND;
            cmpOp  = GT_NE;
        }
        else if (m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_GE &&
                 (!m_testInfo1.testTree->AsOp()->gtOp1->IsUnsigned() &&
                  !m_testInfo2.testTree->AsOp()->gtOp1->IsUnsigned()))
        {
            // t1:c1<0 t2:c2>=0 ==> Branch to BX if both values >= 0
            // So we will branch to BX if (c1|c2)>=0

            foldOp = GT_OR;
            cmpOp  = GT_GE;
        }
        else if (m_testInfo1.compTree->gtOper == GT_NE && m_testInfo2.compTree->gtOper == GT_EQ)
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
    if (m_comp->verbose)
    {
        printf("Folded %sboolean conditions of " FMT_BB " and " FMT_BB " to :\n", m_c2->OperIsLeaf() ? "" : "non-leaf ",
               m_b1->bbNum, m_b2->bbNum);
        m_comp->gtDispStmt(s1);
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
// GetIntersection: Given two ranges, return true if they intersect and form a closed range.
//    Examples:
//      >10 and <=20 -> [11,20]
//      >10 and >100 -> false
//      <10 and >10  -> false
//
// Arguments:
//    type        - The type of the compare nodes.
//    cmp1        - The first compare node.
//    cmp2        - The second compare node.
//    cns1        - The constant value of the first compare node (always RHS).
//    cns2        - The constant value of the second compare node (always RHS).
//    pRangeStart - [OUT] The start of the intersection range (inclusive).
//    pRangeEnd   - [OUT] The end of the intersection range (inclusive).
//
// Returns:
//    true if the ranges intersect and form a closed range.
//
static bool GetIntersection(var_types  type,
                            genTreeOps cmp1,
                            genTreeOps cmp2,
                            ssize_t    cns1,
                            ssize_t    cns2,
                            ssize_t*   pRangeStart,
                            ssize_t*   pRangeEnd)
{
    if ((cns1 < 0) || (cns2 < 0))
    {
        // We don't yet support negative ranges.
        return false;
    }

    // Convert to a canonical form with GT_GE or GT_LE (inclusive).
    auto normalize = [](genTreeOps* cmp, ssize_t* cns) {
        if (*cmp == GT_GT)
        {
            // "X > cns" -> "X >= cns + 1"
            *cns = *cns + 1;
            *cmp = GT_GE;
        }
        if (*cmp == GT_LT)
        {
            // "X < cns" -> "X <= cns - 1"
            *cns = *cns - 1;
            *cmp = GT_LE;
        }
        // whether these overflow or not is checked below.
    };
    normalize(&cmp1, &cns1);
    normalize(&cmp2, &cns2);

    if (cmp1 == cmp2)
    {
        // Ranges have the same direction (we don't yet support that yet).
        return false;
    }

    if (cmp1 == GT_GE)
    {
        *pRangeStart = cns1;
        *pRangeEnd   = cns2;
    }
    else
    {
        assert(cmp1 == GT_LE);
        *pRangeStart = cns2;
        *pRangeEnd   = cns1;
    }

    if ((*pRangeStart >= *pRangeEnd) || (*pRangeStart < 0) || (*pRangeEnd < 0) || !FitsIn(type, *pRangeStart) ||
        !FitsIn(type, *pRangeEnd))
    {
        // TODO: If ranges don't intersect we might be able to fold the condition to true/false.
        // Also, check again if any of the ranges are negative (in case of overflow after normalization)
        // and fits into the given type.
        return false;
    }

    return true;
}

//------------------------------------------------------------------------------
// IsConstantRangeTest: Does the given compare node represent a constant range test? E.g.
//    "X relop CNS" or "CNS relop X" where relop is [<, <=, >, >=]
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
    if (tree->OperIs(GT_LE, GT_LT, GT_GE, GT_GT) && !tree->IsUnsigned())
    {
        GenTree* op1 = tree->gtGetOp1();
        GenTree* op2 = tree->gtGetOp2();
        if (varTypeIsIntegral(op1) && varTypeIsIntegral(op2) && op1->TypeIs(op2->TypeGet()))
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
// FoldRangeTests: Given two compare nodes (cmp1 && cmp2) where cmp1 is X >= 0
//    and cmp2 is X < NN (NeverNegative), try to fold the range test into a single
//    X u< NN (unsigned) compare node.
//
// Arguments:
//    compiler       - compiler instance
//    cmp1           - first compare node
//    cmp1IsReversed - true if cmp1 is in fact reversed
//    cmp2           - second compare node
//    cmp2IsReversed - true if cmp2 is in fact reversed
//
// Returns:
//    true if cmp1 now represents the folded range check and cmp2 can be removed.
//
bool FoldNeverNegativeRangeTest(
    Compiler* comp, GenTreeOp* cmp1, bool cmp1IsReversed, GenTreeOp* cmp2, bool cmp2IsReversed)
{
    GenTree*       var1Node;
    GenTreeIntCon* cns1Node;
    genTreeOps     cmp1Op;

    // First cmp has to be "X >= 0" (or "0 <= X")
    // TODO: handle "X < NN && X >= 0" (where the 2nd comparison is the lower bound)
    // It seems to be a rare case, so we don't handle it for now.
    if (!IsConstantRangeTest(cmp1, &var1Node, &cns1Node, &cmp1Op))
    {
        return false;
    }

    // Now, reverse the comparison if necessary depending on cmp1IsReversed and cmp2IsReversed
    // so we'll get a canonical form of "X >= 0 && X </<= NN"
    cmp1Op            = cmp1IsReversed ? GenTree::ReverseRelop(cmp1Op) : cmp1Op;
    genTreeOps cmp2Op = cmp2IsReversed ? GenTree::ReverseRelop(cmp2->OperGet()) : cmp2->OperGet();

    if ((cmp1Op != GT_GE) || (!cns1Node->IsIntegralConst(0)))
    {
        // Lower bound check has to be "X >= 0".
        // We already re-ordered the comparison so that the constant is always on the right side.
        return false;
    }

    // Upper bound check has to be "X relop NN" or "NN relop X" (NN = NeverNegative)
    // We allow var1Node to be a GT_COMMA node, so we need to call gtEffectiveVal() to get the actual variable
    // since it's guaranteed to be evaluated first.
    GenTree* upperBound;
    if (cmp2->gtGetOp1()->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
        GenTree::Compare(var1Node->gtEffectiveVal(), cmp2->gtGetOp1()))
    {
        // "X relop NN"
        upperBound = cmp2->gtGetOp2();
    }
    else if (cmp2->gtGetOp2()->OperIs(GT_LCL_VAR, GT_LCL_FLD) &&
             GenTree::Compare(var1Node->gtEffectiveVal(), cmp2->gtGetOp2()))
    {
        // "NN relop X"
        upperBound = cmp2->gtGetOp1();
        // Normalize to "X relop NN"
        cmp2Op = GenTree::SwapRelop(cmp2Op);
    }
    else
    {
        return false;
    }

    // Check that our upper bound is known to be never negative (e.g. GT_ARR_LENGTH or Span.Length, etc.)
    if (!upperBound->IsNeverNegative(comp) || !upperBound->TypeIs(var1Node->TypeGet()))
    {
        return false;
    }

    if ((upperBound->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        // We can't fold "X >= 0 && X < NN" to "X u< NN" if NN has side effects.
        return false;
    }

    if ((cmp2Op != GT_LT) && (cmp2Op != GT_LE))
    {
        // Upper bound check has to be "X < NN" or "X <= NN" (normalized form).
        return false;
    }

    cmp1->gtOp1 = var1Node;
    cmp1->gtOp2 = upperBound;
    cmp1->SetOper(cmp2IsReversed ? GenTree::ReverseRelop(cmp2Op) : cmp2Op);
    cmp1->SetUnsigned();
    return true;
}

//------------------------------------------------------------------------------
// FoldRangeTests: Given two compare nodes (cmp1 && cmp2) that represent a range check,
//    fold them into a single compare node if possible, e.g.:
//      1) "X >= 10 && X <= 100" -> "(X - 10) u<= 90"
//      2) "X >= 0 && X <= 100"  -> "X u<= 100"
//    where 'u' stands for unsigned comparison. cmp1 is used as the target node for folding.
//    It's also guaranteed to be first in the execution order (so can allow some side effects).
//
// Arguments:
//    compiler       - compiler instance
//    cmp1           - first compare node
//    cmp1IsReversed - true if cmp1 is in fact reversed
//    cmp2           - second compare node
//    cmp2IsReversed - true if cmp2 is in fact reversed
//
// Returns:
//    true if cmp1 now represents the folded range check and cmp2 can be removed.
//
bool FoldRangeTests(Compiler* comp, GenTreeOp* cmp1, bool cmp1IsReversed, GenTreeOp* cmp2, bool cmp2IsReversed)
{
    GenTree*       var1Node;
    GenTree*       var2Node;
    GenTreeIntCon* cns1Node;
    GenTreeIntCon* cns2Node;
    genTreeOps     cmp1Op;
    genTreeOps     cmp2Op;

    // Make sure both conditions are constant range checks, e.g. "X > CNS"
    if (!IsConstantRangeTest(cmp1, &var1Node, &cns1Node, &cmp1Op) ||
        !IsConstantRangeTest(cmp2, &var2Node, &cns2Node, &cmp2Op))
    {
        // Give FoldNeverNegativeRangeTest a try if both conditions are not constant range checks.
        return FoldNeverNegativeRangeTest(comp, cmp1, cmp1IsReversed, cmp2, cmp2IsReversed);
    }

    // Reverse the comparisons if necessary so we'll get a canonical form "cond1 == true && cond2 == true" -> InRange.
    cmp1Op = cmp1IsReversed ? GenTree::ReverseRelop(cmp1Op) : cmp1Op;
    cmp2Op = cmp2IsReversed ? GenTree::ReverseRelop(cmp2Op) : cmp2Op;

    // Make sure variables are the same:
    if (!var2Node->OperIs(GT_LCL_VAR) || !GenTree::Compare(var1Node->gtEffectiveVal(), var2Node))
    {
        // Variables don't match in two conditions
        // We use gtEffectiveVal() for the first block's variable to ignore COMMAs, e.g.
        //
        // m_b1:
        //   *  JTRUE     void
        //   \--*  LT        int
        //      +--*  COMMA     int
        //      |  +--*  STORE_LCL_VAR int    V03 cse0
        //      |  |  \--*  CAST      int <- ushort <- int
        //      |  |     \--*  LCL_VAR   int    V01 arg1
        //      |  \--*  LCL_VAR   int    V03 cse0
        //      \--*  CNS_INT   int    97
        //
        // m_b2:
        //   *  JTRUE     void
        //   \--*  GT        int
        //      +--*  LCL_VAR   int    V03 cse0
        //      \--*  CNS_INT   int    122
        //
        // For the m_b2 we require the variable to be just a local with no side-effects (hence, no statements)
        return false;
    }

    ssize_t rangeStart;
    ssize_t rangeEnd;
    if (!GetIntersection(var1Node->TypeGet(), cmp1Op, cmp2Op, cns1Node->IconValue(), cns2Node->IconValue(), &rangeStart,
                         &rangeEnd))
    {
        // The range we test via two conditions is not a closed range
        // TODO: We should support overlapped ranges here, e.g. "X > 10 && x > 100" -> "X > 100"
        return false;
    }
    assert(rangeStart < rangeEnd);

    if (rangeStart == 0)
    {
        // We don't need to subtract anything, it's already 0-based
        cmp1->gtOp1 = var1Node;
    }
    else
    {
        // We need to subtract the rangeStartIncl from the variable to make the range start from 0
        cmp1->gtOp1 = comp->gtNewOperNode(GT_SUB, var1Node->TypeGet(), var1Node,
                                          comp->gtNewIconNode(rangeStart, var1Node->TypeGet()));
    }
    cmp1->gtOp2->BashToConst(rangeEnd - rangeStart, var1Node->TypeGet());
    cmp1->SetOper(cmp2IsReversed ? GT_GT : GT_LE);
    cmp1->SetUnsigned();
    return true;
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
    assert((m_b1 != nullptr) && (m_b2 != nullptr) && (m_b3 == nullptr));
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
    }
    else
    {
        // Unknown shape
        return false;
    }

    if (!m_b2->hasSingleStmt() || (m_b2->GetUniquePred(m_comp) != m_b1))
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

    if (!FoldRangeTests(m_comp, cmp1, cmp1IsReversed, cmp2, cmp2IsReversed))
    {
        return false;
    }
    
    // Re-direct firstBlock to jump to inRangeBb
    FlowEdge* const newEdge = m_comp->fgAddRefPred(inRangeBb, m_b1);

    if (!cmp2IsReversed)
    {
        m_b1->SetFalseEdge(m_b1->GetTrueEdge());
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
    m_comp->fgRemoveRefPred(m_b2, m_b1);
    m_comp->fgRemoveBlock(m_b2, true);

    Statement* stmt = m_b1->lastStmt();
    m_comp->gtSetStmtInfo(stmt);
    m_comp->fgSetStmtSeq(stmt);
    m_comp->gtUpdateStmtSideEffects(stmt);
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
    assert((m_b1 != nullptr) && (m_b2 != nullptr) && (m_b3 == nullptr));
    m_t3 = nullptr;

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

    // Ensure there are no additional side effects.
    if ((cond1->gtFlags & (GTF_SIDE_EFFECT | GTF_ORDER_SIDEEFF)) != 0 ||
        (cond2->gtFlags & (GTF_SIDE_EFFECT | GTF_ORDER_SIDEEFF)) != 0)
    {
        return false;
    }

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
    if (!m_comp->compStressCompile(Compiler::STRESS_OPT_BOOLS_COMPARE_CHAIN_COST, 25))
    {
        int op1Cost = cond1->GetCostEx();
        int op2Cost = cond2->GetCostEx();
        // The cost of combing three simple conditions is 32.
        int maxOp1Cost = op1IsCondChain ? 31 : 7;
        int maxOp2Cost = op2IsCondChain ? 31 : 7;

        // Cost to allow for chain size of three.
        if (op1Cost > maxOp1Cost || op2Cost > maxOp2Cost)
        {
            JITDUMP("Skipping CompareChainCond that will evaluate conditions unconditionally at costs %d,%d\n", op1Cost,
                    op2Cost);
            return false;
        }
    }

    // Remove the first JTRUE statement.
    constexpr bool isUnlink = true;
    m_comp->fgRemoveStmt(m_b1, s1 DEBUGARG(isUnlink));

    // Invert the condition.
    if (foundEndOfOrConditions)
    {
        GenTree* revCond = m_comp->gtReverseCond(cond1);
        assert(cond1 == revCond); // Ensure `gtReverseCond` did not create a new node.
    }

    // Join the two conditions together
    genTreeOps chainedOper       = foundEndOfOrConditions ? GT_AND : GT_OR;
    GenTree*   chainedConditions = m_comp->gtNewOperNode(chainedOper, TYP_INT, cond1, cond2);
    cond1->gtFlags &= ~GTF_RELOP_JMP_USED;
    cond2->gtFlags &= ~GTF_RELOP_JMP_USED;
    chainedConditions->gtFlags |= (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

    // Add a test condition onto the front of the chain
    GenTree* testcondition =
        m_comp->gtNewOperNode(GT_NE, TYP_INT, chainedConditions, m_comp->gtNewZeroConNode(TYP_INT));

    // Wire the chain into the second block
    m_testInfo2.testTree->AsOp()->gtOp1 = testcondition;
    m_testInfo2.testTree->AsOp()->gtFlags |= (testcondition->gtFlags & GTF_ALL_EFFECT);
    m_comp->gtSetEvalOrder(m_testInfo2.testTree);
    m_comp->fgSetStmtSeq(s2);

    // Update the flow.
    m_comp->fgRemoveRefPred(m_b1->GetTrueEdge());
    m_b1->SetKindAndTargetEdge(BBJ_ALWAYS, m_b1->GetFalseEdge());
    m_b1->SetFlags(BBF_NONE_QUIRK);

    // Fixup flags.
    m_b2->CopyFlags(m_b1, BBF_COPY_PROPAGATE);

    // Join the two blocks. This is done now to ensure that additional conditions can be chained.
    if (m_b1->NextIs(m_b2) && m_comp->fgCanCompactBlocks(m_b1, m_b2))
    {
        m_comp->fgCompactBlocks(m_b1, m_b2);
    }

#ifdef DEBUG
    if (m_comp->verbose)
    {
        JITDUMP("\nCombined conditions " FMT_BB " and " FMT_BB " into %s chain :\n", m_b1->bbNum, m_b2->bbNum,
                GenTree::OpName(chainedOper));
        m_comp->fgDumpBlock(m_b1);
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
//      and checks if tree operators are of the right type, e.g, GT_JTRUE, GT_RETURN.
//
//      On entry, m_b1, m_b2 are set and m_b3 is set for cond/return/return case.
//      If it passes all the conditions, m_testInfo1.testTree, m_testInfo2.testTree and m_t3 are set
//      to the root nodes of m_b1, m_b2 and m_b3 each.
//      SameTarget is also updated to true if m_b1 and m_b2 jump to the same destination.
//
Statement* OptBoolsDsc::optOptimizeBoolsChkBlkCond()
{
    assert(m_b1 != nullptr && m_b2 != nullptr);

    bool optReturnBlock = false;
    if (m_b3 != nullptr)
    {
        optReturnBlock = true;
    }

    // Find the block conditions of m_b1 and m_b2

    if (m_b2->countOfInEdges() > 1 || (optReturnBlock && m_b3->countOfInEdges() > 1))
    {
        return nullptr;
    }

    // Find the condition for the first block

    Statement* s1 = m_b1->lastStmt();

    GenTree* testTree1 = s1->GetRootNode();
    assert(testTree1->gtOper == GT_JTRUE);

    // The second and the third block must contain a single statement

    Statement* s2 = m_b2->firstStmt();
    if (s2->GetPrevStmt() != s2)
    {
        return nullptr;
    }

    GenTree* testTree2 = s2->GetRootNode();

    if (!optReturnBlock)
    {
        assert(testTree2->gtOper == GT_JTRUE);
    }
    else
    {
        if (testTree2->gtOper != GT_RETURN)
        {
            return nullptr;
        }

        Statement* s3 = m_b3->firstStmt();
        if (s3->GetPrevStmt() != s3)
        {
            return nullptr;
        }

        GenTree* testTree3 = s3->GetRootNode();
        if (testTree3->gtOper != GT_RETURN)
        {
            return nullptr;
        }

        if (!varTypeIsIntegral(testTree2->TypeGet()) || !varTypeIsIntegral(testTree3->TypeGet()))
        {
            return nullptr;
        }

        // The third block is Return with "CNS_INT int 0/1"
        if (testTree3->AsOp()->gtOp1->gtOper != GT_CNS_INT)
        {
            return nullptr;
        }

        if (testTree3->AsOp()->gtOp1->gtType != TYP_INT)
        {
            return nullptr;
        }

        m_t3 = testTree3;
    }

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

    bool optReturnBlock = false;
    if (m_b3 != nullptr)
    {
        optReturnBlock = true;
    }

    assert(m_cmpOp != NULL && m_c1 != nullptr && m_c2 != nullptr);

    GenTree* cmpOp1 = m_foldOp == GT_NONE ? m_c1 : m_comp->gtNewOperNode(m_foldOp, m_foldType, m_c1, m_c2);
    if (m_testInfo1.isBool && m_testInfo2.isBool)
    {
        // When we 'OR'/'AND' two booleans, the result is boolean as well
        cmpOp1->gtFlags |= GTF_BOOLEAN;
    }

    GenTree* t1Comp = m_testInfo1.compTree;
    t1Comp->SetOper(m_cmpOp);
    t1Comp->AsOp()->gtOp1         = cmpOp1;
    t1Comp->AsOp()->gtOp2->gtType = m_foldType; // Could have been varTypeIsGC()
    if (optReturnBlock)
    {
        // Update tree when m_b1 is BBJ_COND and m_b2 and m_b3 are GT_RETURN (BBJ_RETURN)
        t1Comp->AsOp()->gtOp2->AsIntCon()->gtIconVal = 0;
        m_testInfo1.testTree->gtOper                 = GT_RETURN;
        m_testInfo1.testTree->gtType                 = m_testInfo2.testTree->gtType;

        // Update the return count of flow graph
        assert(m_comp->fgReturnCount >= 2);
        --m_comp->fgReturnCount;
    }

#if FEATURE_SET_FLAGS
    // For comparisons against zero we will have the GTF_SET_FLAGS set
    // and this can cause an assert to fire in fgMoveOpsLeft(GenTree* tree)
    // during the CSE phase.
    //
    // So make sure to clear any GTF_SET_FLAGS bit on these operations
    // as they are no longer feeding directly into a comparisons against zero

    // Make sure that the GTF_SET_FLAGS bit is cleared.
    // Fix 388436 ARM JitStress WP7
    m_c1->gtFlags &= ~GTF_SET_FLAGS;
    m_c2->gtFlags &= ~GTF_SET_FLAGS;

    // The new top level node that we just created does feed directly into
    // a comparison against zero, so set the GTF_SET_FLAGS bit so that
    // we generate an instruction that sets the flags, which allows us
    // to omit the cmp with zero instruction.

    // Request that the codegen for cmpOp1 sets the condition flags
    // when it generates the code for cmpOp1.
    //
    cmpOp1->gtRequestSetFlags();
#endif

    // Recost/rethread the tree if necessary
    //
    if (m_comp->fgNodeThreading != NodeThreading::None)
    {
        m_comp->gtSetStmtInfo(m_testInfo1.testStmt);
        m_comp->fgSetStmtSeq(m_testInfo1.testStmt);
    }

    if (!optReturnBlock)
    {
        // Update edges if m_b1: BBJ_COND and m_b2: BBJ_COND

        FlowEdge* edge1 = m_b1->GetTrueEdge();
        FlowEdge* edge2;

        if (m_sameTarget)
        {
            edge2 = m_b2->GetTrueEdge();
        }
        else
        {
            edge2 = m_b2->GetFalseEdge();
            m_comp->fgRemoveRefPred(m_b1->GetTrueEdge());
            FlowEdge* const newEdge = m_comp->fgAddRefPred(m_b2->GetTrueTarget(), m_b1);
            m_b1->SetTrueEdge(newEdge);
        }

        assert(edge1 != nullptr);
        assert(edge2 != nullptr);

        weight_t edgeSumMin = edge1->edgeWeightMin() + edge2->edgeWeightMin();
        weight_t edgeSumMax = edge1->edgeWeightMax() + edge2->edgeWeightMax();
        if ((edgeSumMax >= edge1->edgeWeightMax()) && (edgeSumMax >= edge2->edgeWeightMax()))
        {
            edge1->setEdgeWeights(edgeSumMin, edgeSumMax, m_b1->GetTrueTarget());
        }
        else
        {
            edge1->setEdgeWeights(BB_ZERO_WEIGHT, BB_MAX_WEIGHT, m_b1->GetTrueTarget());
        }
    }

    /* Modify the target of the conditional jump and update bbRefs and bbPreds */

    if (optReturnBlock)
    {
        assert(m_b1->KindIs(BBJ_COND));
        assert(m_b2->KindIs(BBJ_RETURN));
        assert(m_b1->FalseTargetIs(m_b2));
        assert(m_b3 != nullptr);
        m_b1->SetKindAndTargetEdge(BBJ_RETURN);
    }
    else
    {
        assert(m_b1->KindIs(BBJ_COND));
        assert(m_b2->KindIs(BBJ_COND));
        assert(m_b1->TrueTargetIs(m_b2->GetTrueTarget()));
        assert(m_b1->FalseTargetIs(m_b2));
        assert(!m_b2->IsLast());
    }

    if (!optReturnBlock)
    {
        // Update bbRefs and bbPreds
        //
        // Replace pred 'm_b2' for m_b2's false target with 'm_b1'
        // Remove  pred 'm_b2' for m_b2's true target
        FlowEdge* falseEdge = m_b2->GetFalseEdge();
        m_comp->fgReplacePred(falseEdge, m_b1);
        m_comp->fgRemoveRefPred(m_b2->GetTrueEdge());
        m_b1->SetFalseEdge(falseEdge);
    }

    // Get rid of the second block

    m_comp->fgUnlinkBlockForRemoval(m_b2);
    m_b2->SetFlags(BBF_REMOVED);
    // If m_b2 was the last block of a try or handler, update the EH table.
    m_comp->ehUpdateForDeletedBlock(m_b2);

    if (optReturnBlock)
    {
        // Get rid of the third block
        m_comp->fgUnlinkBlockForRemoval(m_b3);
        m_b3->SetFlags(BBF_REMOVED);
        // If m_b3 was the last block of a try or handler, update the EH table.
        m_comp->ehUpdateForDeletedBlock(m_b3);
    }

    // Update IL range of first block
    m_b1->bbCodeOffsEnd = optReturnBlock ? m_b3->bbCodeOffsEnd : m_b2->bbCodeOffsEnd;
}

//-----------------------------------------------------------------------------
//  optOptimizeBoolsReturnBlock: Optimize boolean when m_b1 is BBJ_COND and m_b2 and m_b3 are BBJ_RETURN
//
// Arguments:
//      b3:    Pointer to basic block b3
//
//  Returns:
//      true if boolean optimization is done and m_b1, m_b2 and m_b3 are folded into m_b1, else false.
//
//  Notes:
//      m_b1, m_b2 and m_b3 of OptBoolsDsc are set on entry.
//
//      if B1->TargetIs(b3), it transforms
//          B1 : brtrue(t1, B3)
//          B2 : ret(t2)
//          B3 : ret(0)
//      to
//          B1 : ret((!t1) && t2)
//
//          For example, (x==0 && y==0) generates:
//              B1: GT_JTRUE (BBJ_COND), jumps to B3
//              B2: GT_RETURN (BBJ_RETURN)
//              B3: GT_RETURN (BBJ_RETURN),
//          and it is folded into
//              B1: GT_RETURN (BBJ_RETURN)
//
bool OptBoolsDsc::optOptimizeBoolsReturnBlock(BasicBlock* b3)
{
    assert(m_b1 != nullptr && m_b2 != nullptr);

    // m_b3 is set for cond/return/return case
    m_b3 = b3;

    m_sameTarget        = false;
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

    // Get the fold operator (m_foldOp, e.g., GT_OR/GT_AND) and
    // the comparison operator (m_cmpOp, e.g., GT_EQ/GT_NE/GT_GE/GT_LT)

    var_types foldType = genActualType(m_c1->TypeGet());
    if (varTypeIsGC(foldType))
    {
        foldType = TYP_I_IMPL;
    }
    m_foldType = foldType;

    m_foldOp = GT_NONE;
    m_cmpOp  = GT_NONE;

    genTreeOps foldOp;
    genTreeOps cmpOp;

    ssize_t it1val = m_testInfo1.compTree->AsOp()->gtOp2->AsIntCon()->gtIconVal;
    ssize_t it2val = m_testInfo2.compTree->AsOp()->gtOp2->AsIntCon()->gtIconVal;
    ssize_t it3val = m_t3->AsOp()->gtOp1->AsIntCon()->gtIconVal;

    if (m_c1->gtOper == GT_LCL_VAR && m_c2->gtOper == GT_LCL_VAR &&
        m_c1->AsLclVarCommon()->GetLclNum() == m_c2->AsLclVarCommon()->GetLclNum())
    {
        if (((m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_EQ) ||
             (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_LT)) &&
            it3val == 1)
        {
            // Case: x < 0 || x == 0
            //      t1:c1<0 t2:c2==0 t3:c3==1
            //      ==> true if c1<=0
            //
            // Case: x == 0 || x < 0
            //      t1:c1==0 t2:c2<0 t3:c3==1
            //      ==> true if c1 <= 0
            cmpOp = GT_LE;
        }
        else if (((m_testInfo1.compTree->gtOper == GT_GT && m_testInfo2.compTree->gtOper == GT_EQ) ||
                  (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_GT)) &&
                 it3val == 1)
        {
            // Case: x > 0 || x == 0
            //      t1:c1<0 t2:c2==0 t3:c3==1
            //      ==> true if c1>=0
            //
            // Case: x == 0 || x > 0
            //      t1:c1==0 t2:c2>0 t3:c3==1
            //      ==> true if c1 >= 0
            cmpOp = GT_GE;
        }
        else if (((m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_NE) ||
                  (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_GE)) &&
                 it3val == 0)
        {
            // Case: x >= 0 && x != 0
            //      t1:c1<0 t2:c2==0 t3:c3==0
            //      ==> true if c1>0
            //
            // Case: x != 0 && x >= 0
            //      t1:c1==0 t2:c2>=0 t3:c3==0
            //      ==> true if c1>0
            cmpOp = GT_GT;
        }
        else if (((m_testInfo1.compTree->gtOper == GT_GT && m_testInfo2.compTree->gtOper == GT_NE) ||
                  (m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_LE)) &&
                 it3val == 0)
        {
            // Case: x <= 0 && x != 0
            //      t1:c1<0 t2:c2==0 t3:c3==0
            //      ==> true if c1<0
            //
            // Case: x != 0 && x <= 0
            //      t1:c1==0 t2:c2<=0 t3:c3==0
            //      ==> true if c1<0
            cmpOp = GT_LT;
        }
        else
        {
            return false;
        }

        foldOp = GT_NONE;
    }
    else if ((m_testInfo1.compTree->gtOper == GT_NE && m_testInfo2.compTree->gtOper == GT_EQ) &&
             (it1val == 0 && it2val == 0 && it3val == 0))
    {
        // Case: x == 0 && y == 0
        //      t1:c1!=0 t2:c2==0 t3:c3==0
        //      ==> true if (c1|c2)==0
        foldOp = GT_OR;
        cmpOp  = GT_EQ;
    }
    else if ((m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_NE) &&
             (it1val == 0 && it2val == 0 && it3val == 0))
    {
        // Case: x == 1 && y ==1
        //      t1:c1!=1 t2:c2==1 t3:c3==0 is reversed from optIsBoolComp() to: t1:c1==0 t2:c2!=0 t3:c3==0
        //      ==> true if (c1&c2)!=0
        foldOp = GT_AND;
        cmpOp  = GT_NE;
    }
    else if ((m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_GE) &&
             (it1val == 0 && it2val == 0 && it3val == 0) &&
             (!m_testInfo1.testTree->AsOp()->gtOp1->IsUnsigned() && !m_testInfo2.testTree->AsOp()->gtOp1->IsUnsigned()))
    {
        // Case: x >= 0 && y >= 0
        //      t1:c1<0 t2:c2>=0 t3:c3==0
        //      ==> true if (c1|c2)>=0

        foldOp = GT_OR;
        cmpOp  = GT_GE;
    }
    else if ((m_testInfo1.compTree->gtOper == GT_EQ && m_testInfo2.compTree->gtOper == GT_EQ) &&
             (it1val == 0 && it2val == 0 && it3val == 1))
    {
        // Case: x == 0 || y == 0
        //      t1:c1==0 t2:c2==0 t3:c3==1
        //      ==> true if (c1&c2)==0
        foldOp = GT_AND;
        cmpOp  = GT_EQ;
    }
    else if ((m_testInfo1.compTree->gtOper == GT_NE && m_testInfo2.compTree->gtOper == GT_NE) &&
             (it1val == 0 && it2val == 0 && it3val == 1))
    {
        // Case: x == 1 || y == 1
        //      t1:c1==1 t2:c2==1 t3:c3==1 is reversed from optIsBoolComp() to: t1:c1!=0 t2:c2!=0 t3:c3==1
        //      ==> true if (c1|c2)!=0
        foldOp = GT_OR;
        cmpOp  = GT_NE;
    }
    else if ((m_testInfo1.compTree->gtOper == GT_LT && m_testInfo2.compTree->gtOper == GT_LT) &&
             (it1val == 0 && it2val == 0 && it3val == 1) &&
             (!m_testInfo1.testTree->AsOp()->gtOp1->IsUnsigned() && !m_testInfo2.testTree->AsOp()->gtOp1->IsUnsigned()))
    {
        // Case: x < 0 || y < 0
        //      t1:c1<0 t2:c2<0 t3:c3==1
        //      ==> true if (c1|c2)<0

        foldOp = GT_OR;
        cmpOp  = GT_LT;
    }
    else
    {
        // Require NOT operation for operand(s). Do Not fold.
        return false;
    }

    if ((foldOp == GT_AND || (cmpOp == GT_NE && foldOp != GT_OR)) && (!m_testInfo1.isBool || !m_testInfo2.isBool))
    {
        // x == 1 && y == 1: Skip cases where x or y is greater than 1, e.g., x=3, y=1
        // x == 0 || y == 0: Skip cases where x and y have opposite bits set, e.g., x=2, y=1
        // x == 1 || y == 1: Skip cases where either x or y is greater than 1, e.g., x=2, y=0
        return false;
    }

    m_foldOp = foldOp;
    m_cmpOp  = cmpOp;

    // Now update the trees

    optOptimizeBoolsUpdateTrees();

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("Folded %sboolean conditions of " FMT_BB ", " FMT_BB " and " FMT_BB " to :\n",
               m_c2->OperIsLeaf() ? "" : "non-leaf ", m_b1->bbNum, m_b2->bbNum, m_b3->bbNum);
        m_comp->gtDispStmt(s1);
        printf("\n");
    }
#endif

    // Return true to continue the bool optimization for the rest of the BB chain
    return true;
}

//-----------------------------------------------------------------------------
//  optOptimizeBoolsGcStress: Replace x==null with (x|x)==0 if x is a GC-type.
//                            This will stress code-gen and the emitter to make sure they support such trees.
//
#ifdef DEBUG

void OptBoolsDsc::optOptimizeBoolsGcStress()
{
    if (!m_comp->compStressCompile(m_comp->STRESS_OPT_BOOLS_GC, 20))
    {
        return;
    }

    assert(m_b1->KindIs(BBJ_COND));
    Statement* const stmt = m_b1->lastStmt();
    GenTree* const   cond = stmt->GetRootNode();

    assert(cond->gtOper == GT_JTRUE);

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

    GenTree* comparandClone = m_comp->gtCloneExpr(comparand);

    noway_assert(relop->AsOp()->gtOp1 == comparand);
    genTreeOps oper      = m_comp->compStressCompile(m_comp->STRESS_OPT_BOOLS_GC, 50) ? GT_OR : GT_AND;
    relop->AsOp()->gtOp1 = m_comp->gtNewOperNode(oper, TYP_I_IMPL, comparand, comparandClone);

    // Comparand type is already checked, and we have const int, there is no harm
    // morphing it into a TYP_I_IMPL.
    noway_assert(relop->AsOp()->gtOp2->gtOper == GT_CNS_INT);
    relop->AsOp()->gtOp2->gtType = TYP_I_IMPL;

    // Recost/rethread the tree if necessary
    //
    if (m_comp->fgNodeThreading != NodeThreading::None)
    {
        m_comp->gtSetStmtInfo(test.testStmt);
        m_comp->fgSetStmtSeq(test.testStmt);
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
//      Given a GT_JTRUE or GT_RETURN node, this method checks if it is a boolean comparison
//      of the form "if (boolVal ==/!=/>=/<  0/1)".This is translated into
//      a GT_EQ/GT_NE/GT_GE/GT_LT node with "opr1" being a boolean lclVar and "opr2" the const 0/1.
//
//      When isBool == true, if the comparison was against a 1 (i.e true)
//      then we morph the tree by reversing the GT_EQ/GT_NE/GT_GE/GT_LT and change the 1 to 0.
//
GenTree* OptBoolsDsc::optIsBoolComp(OptTestInfo* pOptTest)
{
    pOptTest->isBool = false;

    assert(pOptTest->testTree->gtOper == GT_JTRUE || pOptTest->testTree->gtOper == GT_RETURN);
    GenTree* cond = pOptTest->testTree->AsOp()->gtOp1;

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

    if (opr2->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    if (!opr2->IsIntegralConst(0) && !opr2->IsIntegralConst(1))
    {
        return nullptr;
    }

    ssize_t ival2 = opr2->AsIntCon()->gtIconVal;

    // Is the value a boolean?
    // We can either have a boolean expression (marked GTF_BOOLEAN) or a constant 0/1.

    if (opr1->gtFlags & GTF_BOOLEAN)
    {
        pOptTest->isBool = true;
    }
    else if ((opr1->gtOper == GT_CNS_INT) && (opr1->IsIntegralConst(0) || opr1->IsIntegralConst(1)))
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
            m_comp->gtReverseCond(cond);
            opr2->AsIntCon()->gtIconVal = 0;
        }
        else
        {
            return nullptr;
        }
    }

    return opr1;
}

//-----------------------------------------------------------------------------
// optOptimizeBools:    Folds boolean conditionals for GT_JTRUE/GT_RETURN nodes
//
// Returns:
//    suitable phase status
//
// Notes:
//      If the operand of GT_JTRUE/GT_RETURN node is GT_EQ/GT_NE/GT_GE/GT_LE/GT_GT/GT_LT of the form
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
    bool     change    = false;
    bool     retry     = false;
    unsigned numCond   = 0;
    unsigned numReturn = 0;
    unsigned numPasses = 0;
    unsigned stress    = false;

    do
    {
        numPasses++;
        change = false;

        for (BasicBlock* b1 = fgFirstBB; b1 != nullptr; b1 = retry ? b1 : b1->Next())
        {
            retry = false;

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
#endif
            }
            else if (b2->KindIs(BBJ_RETURN))
            {
                // Set b3 to b1 jump destination
                BasicBlock* b3 = b1->GetTrueTarget();

                // b3 must not be marked as BBF_DONT_REMOVE

                if (b3->HasFlag(BBF_DONT_REMOVE))
                {
                    continue;
                }

                // b3 must be RETURN type

                if (!b3->KindIs(BBJ_RETURN))
                {
                    continue;
                }

                if (optBoolsDsc.optOptimizeBoolsReturnBlock(b3))
                {
                    change = true;
                    numReturn++;
                }
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

    JITDUMP("\noptimized %u BBJ_COND cases, %u BBJ_RETURN cases in %u passes\n", numCond, numReturn, numPasses);

    const bool modified = stress || ((numCond + numReturn) > 0);
    return modified ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
