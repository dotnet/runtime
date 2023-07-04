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
    BasicBlock* m_b3; // m_b1->bbJumpDest. Null if m_b2 is not a return block.

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
//  optOptimizeBoolsCondBlock:  Optimize boolean when bbJumpKind of both m_b1 and m_b2 are BBJ_COND
//
//  Returns:
//      true if boolean optimization is done and m_b1 and m_b2 are folded into m_b1, else false.
//
//  Notes:
//      m_b1 and m_b2 are set on entry.
//
//      Case 1: if b1.bbJumpDest == b2.bbJumpDest, it transforms
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
//      Case 2: if B1.bbJumpDest == B2->bbNext, it transforms
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

    // Check if m_b1 and m_b2 have the same bbJumpDest

    if (m_b1->bbJumpDest == m_b2->bbJumpDest)
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
    else if (m_b1->bbJumpDest == m_b2->bbNext)
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
    var_types  foldType = m_c1->TypeGet();
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

//-----------------------------------------------------------------------------
//  optOptimizeCompareChainCondBlock:  Create a chain when when both m_b1 and m_b2 are BBJ_COND.
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
    if ((m_b1->bbNext == m_b2) && (m_b1->bbJumpDest == m_b2->bbNext))
    {
        // Found the end of two (or more) conditions being ORed together.
        // The final condition has been inverted.
        foundEndOfOrConditions = true;
    }
    else if ((m_b1->bbNext == m_b2) && (m_b1->bbJumpDest == m_b2->bbJumpDest))
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
    m_comp->fgRemoveRefPred(m_b1->bbJumpDest, m_b1);
    m_b1->bbJumpKind = BBJ_NONE;

    // Fixup flags.
    m_b2->bbFlags |= (m_b1->bbFlags & BBF_COPY_PROPAGATE);

    // Join the two blocks. This is done now to ensure that additional conditions can be chained.
    if (m_comp->fgCanCompactBlocks(m_b1, m_b2))
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
//                              update the edges, unlink removed blocks and update loop table
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

        FlowEdge* edge1 = m_comp->fgGetPredForBlock(m_b1->bbJumpDest, m_b1);
        FlowEdge* edge2;

        if (m_sameTarget)
        {
            edge2 = m_comp->fgGetPredForBlock(m_b2->bbJumpDest, m_b2);
        }
        else
        {
            edge2 = m_comp->fgGetPredForBlock(m_b2->bbNext, m_b2);

            m_comp->fgRemoveRefPred(m_b1->bbJumpDest, m_b1);

            m_b1->bbJumpDest = m_b2->bbJumpDest;

            m_comp->fgAddRefPred(m_b2->bbJumpDest, m_b1);
        }

        assert(edge1 != nullptr);
        assert(edge2 != nullptr);

        weight_t edgeSumMin = edge1->edgeWeightMin() + edge2->edgeWeightMin();
        weight_t edgeSumMax = edge1->edgeWeightMax() + edge2->edgeWeightMax();
        if ((edgeSumMax >= edge1->edgeWeightMax()) && (edgeSumMax >= edge2->edgeWeightMax()))
        {
            edge1->setEdgeWeights(edgeSumMin, edgeSumMax, m_b1->bbJumpDest);
        }
        else
        {
            edge1->setEdgeWeights(BB_ZERO_WEIGHT, BB_MAX_WEIGHT, m_b1->bbJumpDest);
        }
    }

    /* Modify the target of the conditional jump and update bbRefs and bbPreds */

    if (optReturnBlock)
    {
        m_b1->bbJumpDest = nullptr;
        m_b1->bbJumpKind = BBJ_RETURN;
#ifdef DEBUG
        m_b1->bbJumpSwt = m_b2->bbJumpSwt;
#endif
        assert(m_b2->bbJumpKind == BBJ_RETURN);
        assert(m_b1->bbNext == m_b2);
        assert(m_b3 != nullptr);
    }
    else
    {
        assert(m_b1->bbJumpKind == BBJ_COND);
        assert(m_b2->bbJumpKind == BBJ_COND);
        assert(m_b1->bbJumpDest == m_b2->bbJumpDest);
        assert(m_b1->bbNext == m_b2);
        assert(m_b2->bbNext != nullptr);
    }

    if (!optReturnBlock)
    {
        // Update bbRefs and bbPreds
        //
        // Replace pred 'm_b2' for 'm_b2->bbNext' with 'm_b1'
        // Remove  pred 'm_b2' for 'm_b2->bbJumpDest'
        m_comp->fgReplacePred(m_b2->bbNext, m_b2, m_b1);
        m_comp->fgRemoveRefPred(m_b2->bbJumpDest, m_b2);
    }

    // Get rid of the second block

    m_comp->fgUnlinkBlock(m_b2);
    m_b2->bbFlags |= BBF_REMOVED;
    // If m_b2 was the last block of a try or handler, update the EH table.
    m_comp->ehUpdateForDeletedBlock(m_b2);

    if (optReturnBlock)
    {
        // Get rid of the third block
        m_comp->fgUnlinkBlock(m_b3);
        m_b3->bbFlags |= BBF_REMOVED;
        // If m_b3 was the last block of a try or handler, update the EH table.
        m_comp->ehUpdateForDeletedBlock(m_b3);
    }

    // Update loop table
    m_comp->fgUpdateLoopsAfterCompacting(m_b1, m_b2);
    if (optReturnBlock)
    {
        m_comp->fgUpdateLoopsAfterCompacting(m_b1, m_b3);
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
//      if B1.bbJumpDest == b3, it transforms
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

    var_types foldType = m_c1->TypeGet();
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

    assert(m_b1->bbJumpKind == BBJ_COND);
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
    // We can either have a boolean expression (marked GTF_BOOLEAN) or
    // a local variable that is marked as being boolean (lvIsBoolean)

    if (opr1->gtFlags & GTF_BOOLEAN)
    {
        pOptTest->isBool = true;
    }
    else if ((opr1->gtOper == GT_CNS_INT) && (opr1->IsIntegralConst(0) || opr1->IsIntegralConst(1)))
    {
        pOptTest->isBool = true;
    }
    else if (opr1->gtOper == GT_LCL_VAR)
    {
        // is it a boolean local variable?

        unsigned lclNum = opr1->AsLclVarCommon()->GetLclNum();
        noway_assert(lclNum < m_comp->lvaCount);

        if (m_comp->lvaTable[lclNum].lvIsBoolean)
        {
            pOptTest->isBool = true;
        }
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

        for (BasicBlock* b1 = fgFirstBB; b1 != nullptr; b1 = retry ? b1 : b1->bbNext)
        {
            retry = false;

            // We're only interested in conditional jumps here

            if (b1->bbJumpKind != BBJ_COND)
            {
                continue;
            }

            // If there is no next block, we're done

            BasicBlock* b2 = b1->bbNext;
            if (b2 == nullptr)
            {
                break;
            }

            // The next block must not be marked as BBF_DONT_REMOVE
            if (b2->bbFlags & BBF_DONT_REMOVE)
            {
                continue;
            }

            OptBoolsDsc optBoolsDsc(b1, b2, this);

            // The next block needs to be a condition or return block.

            if (b2->bbJumpKind == BBJ_COND)
            {
                if ((b1->bbJumpDest != b2->bbJumpDest) && (b1->bbJumpDest != b2->bbNext))
                {
                    continue;
                }

                // When it is conditional jumps

                if (optBoolsDsc.optOptimizeBoolsCondBlock())
                {
                    change = true;
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
            else if (b2->bbJumpKind == BBJ_RETURN)
            {
                // Set b3 to b1 jump destination
                BasicBlock* b3 = b1->bbJumpDest;

                // b3 must not be marked as BBF_DONT_REMOVE

                if (b3->bbFlags & BBF_DONT_REMOVE)
                {
                    continue;
                }

                // b3 must be RETURN type

                if (b3->bbJumpKind != BBJ_RETURN)
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

//-----------------------------------------------------------------------------
// OptRangePatternDsc:     Descriptor used for `if` range pattern optimization
//
class OptRangePatternDsc
{
public:
    static const int m_sizePatterns = 64; // The size of the patterns array

private:
    Compiler*     m_comp = nullptr; // The pointer to the Compiler instance
    CompAllocator m_allocator;      // The memory allocator
    typedef JitHashTable<ssize_t, JitLargePrimitiveKeyFuncs<ssize_t>, BasicBlock*> PatternToBlockMap;
    PatternToBlockMap m_patternToBlockMap;

    CompAllocator m_allocatorTarget; // The memory allocator
    typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, int> BlockToIntMap;
    BlockToIntMap m_blockToIntMap;

    BasicBlock* switchBBdesc = nullptr; // The switch basic block descriptor
    BBswtDesc*  swtDsc       = nullptr; // The switch descriptor

    ssize_t m_minPattern   = 0; // The minimum pattern
    ssize_t m_maxPattern   = 0; // The maximum pattern
    int     m_rangePattern = 0; // The range of values in patterns[]

    GenTree*    m_minOp        = nullptr; // The CNS_INT node with the minimum pattern
    BasicBlock* m_optFirstBB   = nullptr; // The first BB of the range pattern
    BasicBlock* m_optLastBB    = nullptr; // The last BB of the range pattern
    BasicBlock* m_defaultJmpBB = nullptr; // The Switch default jump target

    unsigned m_bbCodeOffs    = 0; // IL code offset of the switch basic block
    unsigned m_bbCodeOffsEnd = 0; // IL code offset end of the switch basic block

    BasicBlock* optGetDefaultJmpBB();
    void optSetDefaultJmpBB(BasicBlock* defaultJmpBB);
    void optSetMinOp(GenTree* minOpNode);
    void optSetMinPattern(ssize_t patternVal, GenTree* node);
    void optSetMaxPattern(ssize_t patternVal);
    void optSetBbCodeOffs(IL_OFFSET bbCodeOffs);
    void optSetBbCodeOffsEnd(IL_OFFSET bbCodeOffsEnd);
    bool optBlockIsPred(BasicBlock* block, BasicBlock* blockPred);
    BasicBlock* optGetJumpTargetBB(BasicBlock* block);
    void optSetBlockToIntMap(BasicBlock* targetBb);
    bool optMakeSwitchDesc();

public:
    OptRangePatternDsc(Compiler* comp)
        : m_comp(comp)
        , m_allocator(comp->getAllocator(CMK_Generic))
        , m_patternToBlockMap(m_allocator)
        , m_allocatorTarget(comp->getAllocator(CMK_Generic))
        , m_blockToIntMap(m_allocatorTarget)
    {
    }

    void optInitializeRngPattern(BasicBlock* firstBB, ssize_t patternVal);
    bool optSetPattern(int idxPattern, ssize_t patternVal, BasicBlock* block);
    int     optGetPatternCount();
    void    optPrintPatterns();
    bool    optJumpsToPatternBlock();
    ssize_t optGetMinPattern();
    ssize_t optGetMaxPattern();
    void optSetRangePattern(int rangeVal);
    bool optChangeToSwitch();
};

//-----------------------------------------------------------------------------
// optBlockIsPred: Check if blockPred is the predecessor of block
//
// Return Value:
//    True if blockPred is the predecessor of block. False otherwise.
//
// Arguments:
//    block - the block to check.
//    blockPred - the block to check if it is the predecessor of block.
//
bool OptRangePatternDsc::optBlockIsPred(BasicBlock* block, BasicBlock* blockPred)
{
    // Check if blockPred is the predecessor of block
    assert(block != nullptr && blockPred != nullptr);
    FlowEdge** ptrToPred;
    FlowEdge*  predBb = m_comp->fgGetPredForBlock(block, blockPred, &ptrToPred);
    if (predBb != nullptr)
        return true;
    else
        return false;
}

//-----------------------------------------------------------------------------
// optSetMinPattern: Sets the min pattern value
//
// Arguments:
//    patternVal - the pattern value to set
//    node - the node that contains the patternVal
//
void OptRangePatternDsc::optSetMinPattern(ssize_t patternVal, GenTree* node)
{
    if (patternVal < m_minPattern)
    {
        m_minPattern = patternVal;

        // Update minOp to the tree with the min pattern
        optSetMinOp(node);
    }
}

void OptRangePatternDsc::optSetMaxPattern(ssize_t patternVal)
{
    if (patternVal > m_maxPattern)
    {
        m_maxPattern = patternVal;
    }
}

void OptRangePatternDsc::optSetMinOp(GenTree* minOpNode)
{
    assert(minOpNode != nullptr && minOpNode->IsIntegralConst());
    m_minOp = minOpNode;
}

//-----------------------------------------------------------------------------
// optGetJumpTargetBB:  Get jumpTargetBB for the pattern
//
// Arguments:
//    block - the basic block to get its jump target.
//
// Return Value:
//    The jump target BB for the pattern
//
// Notes:
//   If compare operator is GT_EQ, the switch jump target (true case) is bbJumpDest.
//   If compare operator is GT_NE,it is bbNext.
//
BasicBlock* OptRangePatternDsc::optGetJumpTargetBB(BasicBlock* block)
{
    assert(block != nullptr && (block->bbJumpKind == BBJ_SWITCH || block->bbJumpKind == BBJ_COND));
    assert(block->lastStmt()->GetRootNode()->gtGetOp1() != nullptr);

    GenTree* op1  = block->lastStmt()->GetRootNode()->gtGetOp1();
    auto     oper = op1->OperGet();
    assert(oper == GT_EQ || oper == GT_NE);

    if (oper == GT_EQ)
    {
        return block->bbJumpDest;
    }
    else if (oper == GT_NE)
    {
        return block->bbNext;
    }

    return nullptr;
}

//-----------------------------------------------------------------------------
// optSetDefaultJmpBB:  Get Switch Default jump target
//
// Arguments:
//    defaultJmpBB - the basic block to set its jump target.
//
// Return Value:
//    true if the m_defaultJmpBB is set, false otherwise.
//
// Notes:
//   For GT_EQ, Swithc Default jump target (for false case) is set to bbNext.
//   For GT_NE, Swithc Default jump target (for false case) is set to bbJumpDest.
//
void OptRangePatternDsc::optSetDefaultJmpBB(BasicBlock* block)
{
    assert(block != nullptr && block->bbJumpKind == BBJ_COND);
    assert(block->lastStmt()->GetRootNode()->gtGetOp1() != nullptr);

    auto oper = block->lastStmt()->GetRootNode()->gtGetOp1()->OperGet();
    assert(oper == GT_EQ || oper == GT_NE);

    if (oper == GT_EQ)
    {
        m_defaultJmpBB = block->bbNext;
    }
    else if (oper == GT_NE)
    {
        m_defaultJmpBB = block->bbJumpDest;
    }
}

//-----------------------------------------------------------------------------
// optGetDefaultJmpBB:  Get Switch Default jump target
//
// Return Value:
//    The default jump target BB of SWITCH block
//
BasicBlock* OptRangePatternDsc::optGetDefaultJmpBB()
{
    return m_defaultJmpBB;
}

//-----------------------------------------------------------------------------
// optSetBbCodeOffs:  Set the code offset of the basic block
//
void OptRangePatternDsc::optSetBbCodeOffs(IL_OFFSET bbCodeOffs)
{
    assert(bbCodeOffs >= 0 && bbCodeOffs <= UINT_MAX);
    if (optGetPatternCount() == 0)
    {
        m_bbCodeOffs = bbCodeOffs;
    }
}

//-----------------------------------------------------------------------------
// optSetBbCodeOffsEnd:  Set the code offset of the basic block end
//
void OptRangePatternDsc::optSetBbCodeOffsEnd(IL_OFFSET bbCodeOffsEnd)
{
    assert(bbCodeOffsEnd >= 0 && bbCodeOffsEnd <= UINT_MAX);
    m_bbCodeOffsEnd = bbCodeOffsEnd;
}

//-----------------------------------------------------------------------------
// optInitializeRngPattern:     Initializes the range pattern descriptor
//
// Arguments:
//  firstBB - The first basic block of the range pattern
//  firstVal - The first value of the range pattern
//
void OptRangePatternDsc::optInitializeRngPattern(BasicBlock* firstBB, ssize_t firstVal)
{
    assert(firstBB != nullptr && firstBB->bbJumpKind == BBJ_COND &&
           firstBB->lastStmt()->GetRootNode()->OperIs(GT_JTRUE));

    m_optFirstBB = firstBB;
    m_optLastBB  = firstBB;

    // Initialize min pattern and max pattern to the first pattern value
    m_minPattern = firstVal;
    m_maxPattern = firstVal;

    // Initialize the code offset range from the first block
    optSetBbCodeOffs(firstBB->bbCodeOffs);
}

//-----------------------------------------------------------------------------
// optSetPattern: Save pattern value and basic block
//
// Arguments:
//    idxPattern - the index of the pattern to set
//    patternVal - the value of the pattern
//    block - the basic block to set its jump target.
//
// Return Value:
//    true if the pattern is set, false otherwise.
//
bool OptRangePatternDsc::optSetPattern(int idxPattern, ssize_t patternVal, BasicBlock* block)
{
    if (idxPattern < 0 || idxPattern >= m_sizePatterns)
    {
#ifdef DEBUG
        if (m_comp->verbose)
        {
            printf("idxPattern out of range");
        }
#endif // DEBUG

        return false;
    }

    // Set the pattern value if it does not already exists in the map
    if (m_patternToBlockMap.Lookup(patternVal, nullptr)) // Pattern already exists
    {
        return false;
    }

    m_patternToBlockMap.Set(patternVal, block);

    // Set min and max pattern
    optSetMinPattern(patternVal, block->lastStmt()->GetRootNode()->gtGetOp1()->gtGetOp2());
    optSetMaxPattern(patternVal);

    // Set default jump target of the basic block
    optSetDefaultJmpBB(block);

    // Update the code offset end range
    optSetBbCodeOffsEnd(block->bbCodeOffsEnd);

    // Set the last basic block of the range pattern
    m_optLastBB = block;

    return true;
}

//-----------------------------------------------------------------------------
// optGetPatternCount: Get the number of patterns
//
// Return Value:
//    The number of reserved patterns
//
int OptRangePatternDsc::optGetPatternCount()
{
    return m_patternToBlockMap.GetCount();
}

//-----------------------------------------------------------------------------
// optPrintPatterns:     Prints the patterns from m_patternToBlockMap
//
void OptRangePatternDsc::optPrintPatterns()
{
    // print patterns from m_patternToBlockMap using key iterator
    PatternToBlockMap* patternToBlockMap = &m_patternToBlockMap;
    for (ssize_t patternVal : PatternToBlockMap::KeyIteration(patternToBlockMap))
    {
        BasicBlock* mappedBlock;
        if (patternToBlockMap->Lookup(patternVal, &mappedBlock))
        {
            printf("patternVal = %d, block = %d\n", patternVal, mappedBlock->bbNum);
        }
    }
}

//-----------------------------------------------------------------------------
// optJumpsToPatternBlock:     Checks if any pattern block jumps to one of the blocks
//                             within m_patternToBlockMap
//
// Arguments:
//    None
//
// Return Value:
//   True if any of m_patternToBlockMap block jumps to one of the blocks within m_patternToBlockMap
//   False otherwise.
//
bool OptRangePatternDsc::optJumpsToPatternBlock()
{
    PatternToBlockMap* patternToBlockMap = &m_patternToBlockMap;
    const unsigned int maxPatternBbNum   = m_optFirstBB->bbNum + optGetPatternCount() - 1;

    for (ssize_t patternVal : PatternToBlockMap::KeyIteration(patternToBlockMap))
    {
        BasicBlock* sourceBlock;
        if (patternToBlockMap->Lookup(patternVal, &sourceBlock))
        {
            BasicBlock* destBlock = sourceBlock->bbJumpDest;
            assert(destBlock != nullptr);

            if (destBlock->bbNum >= m_optFirstBB->bbNum && destBlock->bbNum <= maxPatternBbNum)
            {
                return true;
            }
        }
    }

    return false;
}

//-----------------------------------------------------------------------------
// optGetMinPattern: Gets the min pattern value
//
// Return Value:
//    The min pattern value
//
ssize_t OptRangePatternDsc::optGetMinPattern()
{
    return m_minPattern;
}

ssize_t OptRangePatternDsc::optGetMaxPattern()
{
    return m_maxPattern;
}

void OptRangePatternDsc::optSetRangePattern(int rangeVal)
{
    assert(rangeVal >= 0 && rangeVal <= m_sizePatterns);
    m_rangePattern = rangeVal;
}

//-----------------------------------------------------------------------------
// optSetBlockToIntMap: Set the map for unique target blocks
//
// Arguments:
//    targetBb - the target basic block
//
void OptRangePatternDsc::optSetBlockToIntMap(BasicBlock* targetBb)
{
    if (!m_blockToIntMap.Lookup(targetBb, nullptr))
    {
        m_blockToIntMap.Set(targetBb, 1);
    }
}

//-----------------------------------------------------------------------------
// optMakeSwitchDesc: Make a Switch descriptor with a switch jump table
//
// Returns:
//    true if the switch descriptor is created successfully with the switch jump tables. False otherwise.
//
// Notes:
//    It creates switch descriptor with a jump table for each range pattern and default case.
//    If the value is part of the reserved patterns, the switch jump table is set to the jump target
//    for true case
//          If GT_EQ, jump target is block's bbJumpDest. If GT_NE, jump target is block's bbNext.
//    Otherwise, the switch jump table is set to the default jump target.
//    No tree is updated in this method.
//
bool OptRangePatternDsc::optMakeSwitchDesc()
{
#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("\n*************** In optMakeSwitchDesc()\n");
    }
#endif // DEBUG

    if (optGetPatternCount() > m_rangePattern || m_rangePattern < 2 || optGetPatternCount() > m_rangePattern ||
        m_rangePattern > m_sizePatterns)
    {
        return false;
    }

    BasicBlock* prevBb       = nullptr;
    int         patternIndex = 0;

    BasicBlockFlags bbFlags   = BBF_EMPTY;
    unsigned        curBBoffs = 0;
    unsigned        nxtBBoffs = 0;
    unsigned        jmpCnt    = 0; // # of switch cases (including default case)

    BasicBlock** jmpTab          = nullptr;
    BasicBlock** jmpPtr          = nullptr;
    unsigned     uniqueTargetCnt = 0; // # of unique jump targets
    bool         tailCall        = false;
    ssize_t      minVal          = optGetMinPattern();

    // Allocate the jump table
    jmpCnt = m_rangePattern + 1;
    jmpPtr = jmpTab = new (m_comp, CMK_BasicBlock) BasicBlock*[jmpCnt];

    // Make a jump table for the range of patterns
    // If reserved pattern, jump to jump target of source block. If not, jump to default target.
    for (int idxRng = 0; idxRng < m_rangePattern; idxRng++)
    {
        // Find a mapped block from a pattern
        ssize_t     key         = minVal + idxRng;
        BasicBlock* mappedBlock = nullptr;
        if (m_patternToBlockMap.Lookup(key, &mappedBlock)) // A mapped block is found
        {
            BasicBlock* jumpTargetBb = optGetJumpTargetBB(mappedBlock);
            *jmpPtr                  = (BasicBlock*)(size_t)(jumpTargetBb->bbCodeOffs);
            *(jmpPtr)                = jumpTargetBb;
            // Update the target basic block to count map
            optSetBlockToIntMap(jumpTargetBb);
        }
        else
        {
            BasicBlock* defaultJmpBb = optGetDefaultJmpBB();
            *jmpPtr                  = (BasicBlock*)(size_t)(defaultJmpBb->bbCodeOffs);
            *(jmpPtr)                = defaultJmpBb;
            // Update the target basic block to count map
            optSetBlockToIntMap(defaultJmpBb);
        }
        jmpPtr++;
    }

    // Append the default label to the jump table
    *jmpPtr   = (BasicBlock*)(size_t)(optGetDefaultJmpBB()->bbCodeOffs);
    *(jmpPtr) = optGetDefaultJmpBB();
    optSetBlockToIntMap(optGetDefaultJmpBB());
    jmpPtr++;

    // Make sure we found the right number of labels
    noway_assert(jmpPtr == jmpTab + jmpCnt);

    //
    // Check if it is profitable to use a switch instead of a series of conditional branches
    //

    // If the number of unique target counts is 1, it has only default case. Not profitable.
    // If it is >= 3, it it not converted to a bit test in Lowering. So, skip it.
    uniqueTargetCnt = m_blockToIntMap.GetCount();
    if (uniqueTargetCnt != 2)
    {
        return false;
    }

    noway_assert(jmpCnt >= 2);

    // If all jumps to the same target block, BBJ_NONE or BBJ_ALWAYS is better.
    BasicBlock* uniqueSucc = nullptr;
    if (uniqueTargetCnt == 2)
    {
        uniqueSucc = jmpTab[0];
        noway_assert(jmpCnt >= 2);
        for (unsigned i = 1; i < jmpCnt - 1; i++)
        {
            if (jmpTab[i] != uniqueSucc)
            {
                uniqueSucc = nullptr;
                break;
            }
        }
    }
    if (uniqueSucc != nullptr)
    {
        return false;
    }

    // Check if it is better to use Jmp instead of Switch
    BasicBlock* defaultBB   = jmpTab[jmpCnt - 1];
    BasicBlock* followingBB = m_optLastBB->bbNext;

    // Is the number of cases right for a jump switch?
    const bool firstCaseFollows = (followingBB == jmpTab[0]);
    const bool defaultFollows   = (followingBB == defaultBB);

    unsigned minSwitchTabJumpCnt = 2; // table is better than just 2 cmp/jcc

    // This means really just a single cmp/jcc (aka a simple if/else)
    if (firstCaseFollows || defaultFollows)
    {
        minSwitchTabJumpCnt++;
    }

#if defined(TARGET_ARM)
    // On ARM for small switch tables we will
    // generate a sequence of compare and branch instructions
    // because the code to load the base of the switch
    // table is huge and hideous due to the relocation... :(
    minSwitchTabJumpCnt += 2;
#endif // TARGET_ARM

    bool useJumpSequence = jmpCnt < minSwitchTabJumpCnt;

    if (TargetOS::IsUnix && TargetArchitecture::IsArm32)
    {
        useJumpSequence = useJumpSequence || m_comp->IsTargetAbi(CORINFO_NATIVEAOT_ABI);
    }

    if (useJumpSequence) // It is better to use a series of compare and branch IR trees.
    {
        return false;
    }

    // One of the case blocks has to follow the switch block. All the pattern blocks will be removed and the first
    // pattern block will be converted to Switch, so it has to follow the last pattern block.
    BasicBlock* bbCase0           = nullptr;
    BasicBlock* bbCase1           = jmpTab[0];
    BasicBlock* nextBbAfterSwitch = m_optLastBB->bbNext;

    for (unsigned tabIdx = 1; tabIdx < (jmpCnt - 1); tabIdx++)
    {
        if (jmpTab[tabIdx] != bbCase1 && bbCase0 == nullptr)
        {
            bbCase0 = jmpTab[tabIdx];
            break;
        }
    }
    if ((nextBbAfterSwitch != bbCase0) && (nextBbAfterSwitch != bbCase1))
    {
        return false;
    }

    // Allocate the switch descriptor
    swtDsc = new (m_comp, CMK_BasicBlock) BBswtDesc;

    // Fill in the remaining fields of the switch descriptor
    swtDsc->bbsCount  = jmpCnt;
    swtDsc->bbsDstTab = jmpTab;

    m_comp->fgHasSwitch = true;

    if (m_comp->opts.compProcedureSplitting)
    {
        m_comp->opts.compProcedureSplitting = false;
        JITDUMP("Turning off procedure splitting for this method, as it might need switch tables; "
                "implementation limitation.\n");
    }

    tailCall = false;

#ifdef DEBUG
    if (m_comp->verbose)
    {
        //  Print bbNum of each jmpTab
        printf("------Switch " FMT_BB " jumps to:    ", m_optFirstBB->bbNum);
        for (unsigned i = 0; i < swtDsc->bbsCount; i++)
        {
            printf("%c" FMT_BB, (i == 0) ? ' ' : ',', jmpTab[i]->bbNum);
        }
        printf("\n\n");
    }
#endif // DEBUG

    return true;
}

//-----------------------------------------------------------------------------
// optChangeToSwitch: Change the first pattern block to SWITCH block and remove other pattern blocks
//
// Return Value:
//    true if the blocks were successfully updated, false otherwise.
//
// Notes:
//    Leave Switch basic block only and remove all other blocks in the range pattern.
//    Update reference count of jump target blocks.
//    If the CNS_INT node does not have a min pattern, replace it with the CNS_INT node with the min value.
//
//    Tree before the optimization:
//    ```
//      JTRUE
//        \EQ
//          \LCL_VAR
//          \CNS_INT
//    ```
//
//    Tree after the optimization:
//    ```
//      SWITCH
//        \SUB
//          \LCL_VAR
//          \CNS_INT (min pattern)
//     ```
bool OptRangePatternDsc::optChangeToSwitch()
{
#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("\n*************** In optChangeToSwitch()\n");
    }
#endif // DEBUG

    assert(m_optFirstBB != nullptr && m_optFirstBB->KindIs(BBJ_COND) && m_optFirstBB->bbNext != nullptr);
    assert(optGetPatternCount() <= m_rangePattern && m_rangePattern >= 2 && optGetPatternCount() <= m_rangePattern &&
           m_rangePattern <= m_sizePatterns);

    // Make Switch descriptor with a jump table
    if (!optMakeSwitchDesc())
    {
        return false;
    }

    bool updatedBlocks = false;
    switchBBdesc       = m_optFirstBB;
    int patternCount   = optGetPatternCount();

    // Update the Switch basic block

    // Change `JTRUE` basic block to `Switch` basic block
    switchBBdesc = m_optFirstBB;

    // Change BBJ_COND to BBJ_SWITCH
    switchBBdesc->bbJumpKind = BBJ_SWITCH;
    switchBBdesc->bbJumpDest = nullptr;

    switchBBdesc->bbCodeOffs    = m_bbCodeOffs;
    switchBBdesc->bbCodeOffsEnd = m_bbCodeOffsEnd;
    switchBBdesc->bbJumpSwt     = swtDsc;
    GenTree* rootTree           = switchBBdesc->lastStmt()->GetRootNode();
    assert(rootTree->OperIs(GT_JTRUE));
    assert(!(rootTree->gtFlags & GTF_SIDE_EFFECT)); // JTRUE node should not have side effects

    // Change from GT_JTRUE to GT_SWITCH
    rootTree->ChangeOper(GT_SWITCH);

    // Special args to fgAddRefPred
    FlowEdge* const oldEdge = nullptr;

    // Remove non-switch pattern blocks. Update the reference count of jump target block from the removed block.
    BasicBlock* currBb = switchBBdesc->bbNext;
    for (int idxPattern = 1; idxPattern < patternCount && currBb != nullptr; idxPattern++)
    {
        assert(!(currBb->bbFlags & BBF_DONT_REMOVE));

        BasicBlock* prevBlock = currBb->bbPrev;
        BasicBlock* nextBlock = currBb->bbNext;
        BasicBlock* jumpBlock = optGetJumpTargetBB(currBb);

        // Unlink the current block and its pred block
        assert(optBlockIsPred(currBb, prevBlock));
        m_comp->fgRemoveRefPred(currBb, prevBlock);

        // Link Switch block and current block's jump target block
        m_comp->fgAddRefPred(jumpBlock, switchBBdesc, oldEdge);

        // Link Switch block and the next block:
        //      if GT_EQ and currBb is the last pattern block, skip because bbNext is already linked as default jump
        //      target if GT_NE, it is already linked when linking its jump target block to Switch block
        if (currBb->lastStmt()->GetRootNode()->gtGetOp1()->OperIs(GT_EQ) && idxPattern != (patternCount - 1))
        {
            m_comp->fgAddRefPred(nextBlock, switchBBdesc, oldEdge);
        }

        m_comp->fgRemoveBlock(currBb, /* unreachable */ true);

        updatedBlocks = true;
        currBb        = nextBlock;
    }

    // Update the reference count of the default jump block
    int numNotFound = m_rangePattern - patternCount + 1; // +1 for switch default case
    for (int idxFalse = 0; idxFalse < numNotFound; idxFalse++)
    {
        m_comp->fgAddRefPred(optGetDefaultJmpBB(), switchBBdesc, oldEdge);
    }

    // Continue to transform Switch node

    Statement* stmt = switchBBdesc->lastStmt();

    // Change from GT_EQ or GT_NE to GT_SUB
    //      tree: SUB
    //      op1: LCL_VAR
    //      op2: GT_CNS_INT or GT_CNS_LNG
    GenTree* tree = rootTree->gtGetOp1(); // GT_EQ or GT_NE node to chnage to GT_SUB
    tree->ChangeOper(GT_SUB);
    assert(tree->gtGetOp1() != nullptr && tree->gtGetOp1()->OperIs(GT_LCL_VAR));

    // Change constant node if siwtch tree does not have the mininum pattern
    if (tree->gtGetOp2() != nullptr && tree->gtGetOp2()->AsIntCon()->IconValue() != optGetMinPattern())
    {
        GenTree* op2        = tree->gtGetOp2(); // GT_CNS_INT or GT_CNS_LNG node
        tree->AsOp()->gtOp2 = m_minOp;

        m_comp->gtSetStmtInfo(stmt);
        m_comp->fgSetStmtSeq(stmt);

        DEBUG_DESTROY_NODE(op2);
    }

    m_comp->gtUpdateStmtSideEffects(stmt);

    return updatedBlocks;
}

//-----------------------------------------------------------------------------
//  optFindSpecificPattern: Optimize range check for if (A || B || C || D) pattern.
//
//  Returns:
//      MODIFIED_NOTHING if no optimization is performed.
//      MODIFIED_EVERYTHING otherwise.
//
//  Notes:
//      Detect if (a == val1 || a == val2 || a == val3 || ...) pattern and change it to switch tree
//      to reduce jumps and perform bit operation instead.
//
PhaseStatus Compiler::optFindSpecificPattern()
{
#ifdef DEBUG
    if (this->verbose)
    {
        printf("\n*************** In optFindSpecificPattern()\n");
    }
#endif // DEBUG

    OptRangePatternDsc optRngPattern(this);

    bool         printed           = false;
    int          patternIndex      = 0; // The index of the pattern in the array
    bool         foundPattern      = false;
    unsigned int firstPatternBBNum = 0; // Basic block number of the first pattern found
    BasicBlock*  prevBb            = fgFirstBB;

    if (fgFirstBB->bbNext == nullptr)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    for (BasicBlock* currBb = fgFirstBB->bbNext; currBb != nullptr; currBb = currBb->bbNext)
    {
        if (currBb->KindIs(BBJ_COND) && prevBb != nullptr && prevBb->KindIs(BBJ_COND))
        {
#ifdef DEBUG
            if (this->verbose)
            {
                if (!printed)
                {
                    printf("Display Basic Blocks for optFindSpecificPattern:\n");
                    fgDispBasicBlocks(true);
                    printed = true;
                }
            }
#endif // DEBUG

            // Check if prevBb is the predecessor of currBb and currBb has only one predecessor
            FlowEdge** ptrToPred;
            FlowEdge*  pred = fgGetPredForBlock(currBb, prevBb, &ptrToPred);
            if (pred == nullptr || currBb->bbRefs != 1 || currBb->bbFlags & BBF_DONT_REMOVE)
            {
                if (foundPattern)
                    break; // Stop searching if patterns are not from consecutive basic blocks
                else
                    continue;
            }

            // Basic block must have only one statement
            if (currBb->lastStmt() == currBb->firstStmt() && prevBb->lastStmt() == prevBb->firstStmt())
            {
                // Skip if there is any side effect
                assert(currBb->lastStmt()->GetRootNode()->OperIs(GT_JTRUE)); // JTRUE node
                if (currBb->lastStmt()->GetRootNode()->gtFlags & GTF_SIDE_EFFECT)
                {
                    // Stop searching if patterns are not from consecutive basic blocks
                    if (foundPattern)
                        break;
                    else
                        continue;
                }

                auto currCmpOp = currBb->lastStmt()->GetRootNode()->gtGetOp1(); // GT_EQ or GT_NE node
                auto prevCmpOp = prevBb->lastStmt()->GetRootNode()->gtGetOp1();
                assert(currCmpOp != nullptr && prevCmpOp != nullptr);

                // Compare operator is GT_EQ. If it is GT_NE, it is the end of the pattern check.
                if ((prevCmpOp->OperIs(GT_EQ) && currCmpOp->OperIs(GT_EQ)) ||
                    (prevCmpOp->OperIs(GT_EQ) && currCmpOp->OperIs(GT_NE)))
                {
                    if (prevBb->bbJumpDest == currBb)
                    {
                        if (foundPattern)
                            break;
                        else
                            continue;
                    }

                    // Check both conditions to have constant on the right side (optimize GT_CNS_INT only)
                    if (currCmpOp->gtGetOp2()->IsCnsIntOrI() && prevCmpOp->gtGetOp2()->IsCnsIntOrI())
                    {
                        // Check both conditions to have the same local variable number
                        if (prevCmpOp->gtGetOp1()->OperIs(GT_LCL_VAR) && currCmpOp->gtGetOp1()->OperIs(GT_LCL_VAR) &&
                            prevCmpOp->gtGetOp1()->AsLclVar()->GetLclNum() ==
                                currCmpOp->gtGetOp1()->AsLclVar()->GetLclNum())
                        {
#ifdef DEBUG
                            if (this->verbose)
                            {
                                printf("\nFound pattern (Prev vs Curr):\n");
                                gtDispTree(prevCmpOp);
                                printf("\n");
                                gtDispTree(currCmpOp);
                                printf("\n\n");
                            }
#endif // DEBUG

                            // No optimization if the number of patterns is greater than 64.
                            if (patternIndex >= optRngPattern.m_sizePatterns)
                            {
#ifdef DEBUG
                                if (this->verbose)
                                {
                                    printf("Too many patterns found (> 64), no optimization done.\n");
                                }
#endif // DEBUG
                                return PhaseStatus::MODIFIED_NOTHING;
                            }

                            // First pattern found
                            if (!foundPattern)
                            {
                                assert(patternIndex == 0 && prevCmpOp->OperIs(GT_EQ));
                                ssize_t firstPatternVal = prevCmpOp->gtGetOp2()->AsIntCon()->IconValue();

                                // Initialize the pattern range
                                optRngPattern.optInitializeRngPattern(prevBb, firstPatternVal);

                                // Save the first pattern
                                if (!optRngPattern.optSetPattern(patternIndex, firstPatternVal, prevBb))
                                {
                                    break;
                                }

                                firstPatternBBNum = prevBb->bbNum;
                                patternIndex++;
                            }

                            // Current pattern

                            // Save the pattern and Switch default jump target for the pattern (false case)
                            ssize_t currentPatternVal = currCmpOp->gtGetOp2()->AsIntCon()->IconValue();
                            if (!optRngPattern.optSetPattern(patternIndex, currentPatternVal, currBb))
                            {
                                break;
                            }

                            patternIndex++;
                            foundPattern = true;

                            // Stop searching if the current BB is GT_NE. It is the last pattern.
                            if (currCmpOp->OperIs(GT_NE))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Optimize only when patterns are found in consecutive BBs.
        // Stop searching if patterns have been found in previous BBs, but the current BB does not have a pattern
        if (foundPattern && patternIndex < (int)(currBb->bbNum - firstPatternBBNum + 1))
        {
            break;
        }

        prevBb = currBb;
    }

    if (foundPattern)
    {
        int patternCount = optRngPattern.optGetPatternCount();
        if (patternCount <= 1 || patternCount > optRngPattern.m_sizePatterns)
        {
            return PhaseStatus::MODIFIED_NOTHING;
        }

#ifdef DEBUG
        if (verbose)
        {
            printf("Reserved patterns:\n");
            optRngPattern.optPrintPatterns();
        }
#endif // DEBUG

        // Check if blocks jump to any of the pattern block
        if (optRngPattern.optJumpsToPatternBlock())
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("A pattern block jumps to another pattern block, no optimization done.\n");
            }
#endif // DEBUG

            return PhaseStatus::MODIFIED_NOTHING;
        }

        // Find range of pattern values
        ssize_t minPattern   = optRngPattern.optGetMinPattern();
        ssize_t maxPattern   = optRngPattern.optGetMaxPattern();
        int     rangePattern = (int)(maxPattern - minPattern + 1);
        if (patternCount > rangePattern || rangePattern < 2 || rangePattern > optRngPattern.m_sizePatterns)
        {
#ifdef DEBUG
            if (verbose)
            {
                printf("Range of pattern values is too small (< 2) or too big (> %d): %d\n",
                       optRngPattern.m_sizePatterns, rangePattern);
            }
#endif // DEBUG

            return PhaseStatus::MODIFIED_NOTHING;
        }
        optRngPattern.optSetRangePattern(rangePattern);

#ifdef DEBUG
        if (verbose)
        {
#ifdef TARGET_64BIT
            printf("Min Max Range: %lld, %lld, %d\n", minPattern, maxPattern, rangePattern);
#else  // !TARGET_64BIT
            printf("Min Max Range Bitmap: %d, %d, %d\n", minPattern, maxPattern, rangePattern);
#endif // !TARGET_64BIT
        }
#endif // DEBUG

        // Replace "JTRUE" block with a "Switch" block and remove other pattern blocks
        if (optRngPattern.optChangeToSwitch())
        {
#ifdef DEBUG
            if (verbose)
            {
                fgDispBasicBlocks(true);
            }
#endif // DEBUG

            return PhaseStatus::MODIFIED_EVERYTHING;
        }
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
