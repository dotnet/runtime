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

    if (!ISMETHOD("Test"))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

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
    OptRangePatternDsc(Compiler* comp)
    {
        m_comp = comp;
    }

    static const int m_sizePatterns             = 64; // The size of the patterns array
    int              m_patterns[m_sizePatterns] = {}; // Reserved patterns
    int              m_numFoundPatterns         = 0;  // The number of patterns found

    int m_minPattern   = 0; // The minimum pattern
    int m_maxPattern   = 0; // The maximum pattern
    int m_rangePattern = 0; // The range of values in patterns[]
    unsigned int bitmapPatterns  = 0; // The bitmap of patterns found
    GenTree* m_minOp = nullptr; // The CNS_INT node with the minimum pattern

    unsigned int m_firstPatternBBNum = 0;       // BB that the first pattern is found
    BasicBlock*  m_optFirstBB        = nullptr; // The first BB of the range pattern
    BasicBlock*  m_trueJmpBB         = nullptr; // The BB to jump to in case of true condition
    BasicBlock*  m_falseJmpBB        = nullptr; // The BB to jump to in case of false condition

    unsigned m_bbCodeOffs = 0;
    unsigned m_bbCodeOffsEnd = 0;
    BasicBlock* switchBBdesc = nullptr;

private:
    Compiler* m_comp = nullptr; // The pointer to the Compiler instance

public:
    bool optMakeSwitchBBdesc();
    bool optUpdateBlocks();
};

//-----------------------------------------------------------------------------
// optUpdateBlocks: Remove pattern blocks and update predecessors of jump target blocks
//
bool OptRangePatternDsc::optUpdateBlocks()
{
    if (ISMETHOD("FooNum2"))        // TODO clean up code
    {
        printf("optUpdateBlocks()\n");
    }

    if (m_rangePattern == 0 || m_numFoundPatterns > m_rangePattern || m_rangePattern > m_sizePatterns)
    {
        return false;
    }

    // Special args to fgAddRefPred
    FlowEdge* const oldEdge = nullptr;
    BasicBlock*     currBb  = m_optFirstBB;

    // Update reference count of jump target blocks and remove pattern blocks
    for (int idxPattern = 0; idxPattern < m_numFoundPatterns && currBb != nullptr; idxPattern++)
    {
        // Skip to add reference count for the Swithc block because it already has a link to jump block
        if (idxPattern == 0)
        {
            currBb = currBb->bbNext;
            continue;
        }

        // Reference count updates of the jump target blocks
        auto operTree = currBb->lastStmt()->GetRootNode()->gtGetOp1();
        if (operTree->OperGet() == GT_EQ)
        {
            m_comp->fgAddRefPred(m_trueJmpBB, switchBBdesc, oldEdge);
        }
        else if (operTree->OperGet() == GT_NE)
        {
            m_comp->fgAddRefPred(m_falseJmpBB, switchBBdesc, oldEdge);
        }
        else
        {
            return false;
        }

        // Remove the current block
        BasicBlock* prevBlock = currBb->bbPrev;
        BasicBlock* nextBlock = currBb->bbNext;

        // Unlink to the previous block and link the next block to the previous block
        m_comp->fgRemoveRefPred(currBb, prevBlock);
        m_comp->fgAddRefPred(nextBlock, prevBlock, oldEdge);
        
        m_comp->fgRemoveBlock(currBb, /* unreachable */ true);

        prevBlock->bbNext = nextBlock;
        currBb = nextBlock;
    }

    // Update the reference count of false jump block
    int numNotFound = m_rangePattern - m_numFoundPatterns;
    for (int idxFalse = 0; idxFalse < numNotFound; idxFalse++)
    {
        m_comp->fgAddRefPred(m_falseJmpBB, switchBBdesc, oldEdge);
    }

    return true;
}

//-----------------------------------------------------------------------------
// optMakeSwitchBBdesc: Create a switch block descriptor for the range pattern optimization
//
bool OptRangePatternDsc::optMakeSwitchBBdesc()
{

    if (ISMETHOD("FooNum2"))            // TODO clean up code
    {
        printf("\nStart optMakeSwitchBlock()\n");
    }

    if (m_rangePattern == 0 || m_numFoundPatterns > m_rangePattern || m_rangePattern > m_sizePatterns)
    {
        return false;
    }

    BasicBlock*     prevBb       = nullptr;
    int             patternIndex = 0;
    BBswtDesc*      swtDsc       = nullptr;
    BBjumpKinds     jmpKind      = BBJ_NONE;

    BasicBlockFlags bbFlags = BBF_EMPTY;
    unsigned        curBBoffs = 0;
    unsigned        nxtBBoffs = 0;
    unsigned jmpCnt = 0; // # of switch cases (excluding default)

    BasicBlock** jmpTab = nullptr;
    BasicBlock** jmpPtr = nullptr;
    BasicBlock* currBb = m_optFirstBB;
    bool         tailCall = false;

    // Special args to fgAddRefPred so it will use the initialization fast path
    FlowEdge* const oldEdge           = nullptr;
    bool const      initializingPreds = true;

    // Make a Switch descriptor and jump table for the range of patterns
    for (int idxRng = 0; idxRng < m_rangePattern; idxRng++)
    {
        // Create switch descriptor and its jump table
        if (idxRng == 0)
        {
            // Allocate the switch descriptor
            swtDsc = new (m_comp, CMK_BasicBlock) BBswtDesc;

            // Allocate the jump table
            jmpCnt = m_rangePattern;
            jmpPtr = jmpTab = new (m_comp, CMK_BasicBlock) BasicBlock*[jmpCnt + 1];
        }

        // Fill in the jump table with the jump code offset
        // If the pattern is found, jump to true target BB. If not, to false target BB.
        bool reservedPattern = bitmapPatterns & (1 << idxRng);
        if (reservedPattern)
        {
            *jmpPtr     = (BasicBlock*)(size_t)m_trueJmpBB->bbCodeOffs;
            *(jmpPtr++) = m_trueJmpBB;
        }
        else
        {
            *jmpPtr     = (BasicBlock*)(size_t)m_falseJmpBB->bbCodeOffs;
            *(jmpPtr++) = m_falseJmpBB;
        }
    }

    // Append the default label to the jump table
    *jmpPtr = (BasicBlock*)(size_t)m_falseJmpBB->bbCodeOffs;
    *(jmpPtr++) = m_falseJmpBB;

    // Make sure we found the right number of labels
    noway_assert(jmpPtr == jmpTab + jmpCnt + 1);

    // Fill in the remaining fields of the switch descriptor
    swtDsc->bbsCount  = jmpCnt + 1;
    swtDsc->bbsDstTab = jmpTab;
    jmpKind           = BBJ_SWITCH;

    m_comp->fgHasSwitch = true;

    if (m_comp->opts.compProcedureSplitting)
    {
        // TODO-CQ: We might need to create a switch table; we won't know for sure until much later.
        // However, switch tables don't work with hot/cold splitting, currently. The switch table data needs
        // a relocation such that if the base (the first block after the prolog) and target of the switch
        // branch are put in different sections, the difference stored in the table is updated. However, our
        // relocation implementation doesn't support three different pointers (relocation address, base, and
        // target). So, we need to change our switch table implementation to be more like
        // JIT64: put the table in the code section, in the same hot/cold section as the switch jump itself
        // (maybe immediately after the switch jump), and make the "base" address be also in that section,
        // probably the address after the switch jump.
        m_comp->opts.compProcedureSplitting = false;
        JITDUMP("Turning off procedure splitting for this method, as it might need switch tables; "
                "implementation limitation.\n");
    }

    tailCall = false;

    // Change `if` basic block to `Switch` basic block
    switchBBdesc = m_optFirstBB;
    switchBBdesc->bbJumpKind = BBJ_SWITCH;

    switchBBdesc->bbCodeOffs    = m_bbCodeOffs;
    switchBBdesc->bbCodeOffsEnd = m_bbCodeOffsEnd;
    switchBBdesc->bbJumpSwt     = swtDsc;
    switchBBdesc->bbFlags |= bbFlags;
    if (m_comp->compRationalIRForm)
    {
        switchBBdesc->bbFlags |= BBF_IS_LIR;
    }

    // Print bbNum of each jmpTab
    for (unsigned i = 0; i < swtDsc->bbsCount; i++)
    {
        printf("%c" FMT_BB, (i == 0) ? ' ' : ',', jmpTab[i]->bbNum);
    }
    printf("\n");

    // Change from GT_JTRUE to GT_SWITCH
    switchBBdesc->lastStmt()->GetRootNode()->ChangeOper(GT_SWITCH, GenTree::PRESERVE_VN);
    switchBBdesc->lastStmt()->GetRootNode()->gtFlags &= ~GTF_ASG;       // TODO check the right value to set

    // Change from GT_EQ or GT_NE to SUB
    //      tree: SUB
    //      op1: LCL_VAR
    //      op2: GT_CNS_INT
    GenTree* tree = switchBBdesc->lastStmt()->GetRootNode()->gtGetOp1(); // GT_EQ or GT_NE node
    tree->ChangeOper(GT_SUB, GenTree::PRESERVE_VN);
    //tree->gtFlags &= ~GTF_ASG;            // TODO check the right value to set

    // get LCL_VAR node in case of COMMA node
    if (tree->gtGetOp1()->OperIs(GT_COMMA))
    {
        GenTree* commaNode  = tree->gtGetOp1();          // COMMA node to be removed
        GenTree* op1        = commaNode->gtGetOp1();     // op1 of COMMA node to be removed
        GenTree* op2        = commaNode->gtGetOp2();     // LCL_VAR node

        tree->AsOp()->gtOp1 = op2;                       // Set LCL_VAR node to op1 of GT_SUB tree

        // TODO: Update the statement m_tree list. It causes assert during fgDebugCheckStmtsList >
        // fgDebugCheckNodeLinks()

        DEBUG_DESTROY_NODE(op1);
        DEBUG_DESTROY_NODE(commaNode); // Destroy COMMA node
    }

    // Change CNS_INT node value if siwtch tree does not have the mininum pattern

    GenTree* operTree = switchBBdesc->lastStmt()->GetRootNode()->gtGetOp1();  // GT_SUB node
    assert(operTree->gtGetOp2() != nullptr);
    if (operTree->gtGetOp2()->AsIntCon()->IconValue() != m_minPattern)
    {
        operTree->AsOp()->gtOp2 = m_minOp;

        // TODO: Update the statement m_tree list. It causes assert during fgDebugCheckStmtsList >
        // fgDebugCheckNodeLinks()
    }

    return true;
}

//-----------------------------------------------------------------------------
//  optFindSpecificPattern: Optimize range check for if (A || B || C || D) pattern.
//
//  Returns:
//      MODIFIED_NOTHING if no optimization is performed.
//      MODIFIED_EVERYTHING otherwise.
//
//  Notes:
//      Detect if (a == val1 || a == val2 || a == val3 || a == val4) pattern and change it to switch tree to reduce
//      jumps and perform bit operation instead.
//
PhaseStatus Compiler::optFindSpecificPattern()
{
    if (!ISMETHOD("FooNum2"))           // TODO clean up code
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    OptRangePatternDsc optRngPattern(this);

    bool        printed                = false;
    int         patternIndex           = 0; // The index of the pattern in the array
    bool        foundPattern           = false;
    BasicBlock* prevBb                 = fgFirstBB;

    if (fgFirstBB->bbNext == nullptr)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    for (BasicBlock* currBb = fgFirstBB->bbNext; currBb != nullptr; currBb = currBb->bbNext)
    {
        if ((currBb->KindIs(BBJ_COND) || currBb->KindIs(BBJ_RETURN)) && prevBb != nullptr && prevBb->KindIs(BBJ_COND))
        {
            if (!printed)
            {
                printf("Display Basic Blocks for optFindSpecificPattern:\n");
                fgDispBasicBlocks(true);
                printed = true;
            }

            if (currBb->lastStmt() == currBb->firstStmt() && prevBb->lastStmt() == prevBb->firstStmt())
            {
                auto condition1 = currBb->lastStmt()->GetRootNode()->gtGetOp1();
                auto condition2 = prevBb->lastStmt()->GetRootNode()->gtGetOp1();
                if (condition1->OperIsCompare() && condition2->OperIsCompare())
                {
                    if (currBb->bbJumpDest == prevBb->bbJumpDest ||
                        (condition1->OperIs(GT_NE) && condition2->OperIs(GT_EQ) && currBb->bbNext == prevBb->bbJumpDest))
                    {
                        // Check both conditions to have constant on the right side
                        if (condition1->gtGetOp2()->IsIntegralConst() && condition2->gtGetOp2()->IsIntegralConst())
                        {
                            auto leftCondition1 = condition1->gtGetOp1(); // op1 of condition1 from currBb
                            auto leftCondition2 = condition2->gtGetOp1(); // op1 of condition2 from prevBb
                            if (leftCondition1->IsLocal() &&
                                ((leftCondition2->IsLocal() && leftCondition1->AsLclVarCommon()->GetLclNum() ==
                                                                   leftCondition2->AsLclVarCommon()->GetLclNum()) ||
                                 (leftCondition2->OperIs(GT_COMMA) &&
                                  leftCondition1->AsLclVarCommon()->GetLclNum() ==
                                      leftCondition2->gtEffectiveVal(/* commaOnly */ true)
                                          ->AsLclVarCommon()
                                          ->GetLclNum())))
                            {
                                printf("\nFound pattern (Prev vs Curr):\n");
                                gtDispTree(condition2);
                                printf("\n");
                                gtDispTree(condition1);
                                printf("\n\n\n");

                                // Store the found pattern to the patterns
                                if (patternIndex >= optRngPattern.m_sizePatterns)
                                {
                                    printf("Too many patterns found (> 64), no optimization done.\n");
                                    return PhaseStatus::MODIFIED_NOTHING;
                                }

                                // Previous pattern
                                if (!foundPattern)  // First pattern found
                                {
                                    optRngPattern.m_patterns[patternIndex] =
                                        (int)condition2->gtGetOp2()->AsIntCon()->IconValue();
                                    optRngPattern.m_firstPatternBBNum = prevBb->bbNum;
                                    optRngPattern.m_optFirstBB        = prevBb;

                                    assert(condition2->OperIs(GT_EQ));
                                    optRngPattern.m_trueJmpBB = prevBb->bbJumpDest;

                                    // min and max patterns
                                    optRngPattern.m_minPattern = optRngPattern.m_patterns[patternIndex];
                                    optRngPattern.m_maxPattern = optRngPattern.m_patterns[patternIndex++];

                                    // Update the code offset range
                                    optRngPattern.m_bbCodeOffs    = prevBb->bbCodeOffs;
                                    optRngPattern.m_bbCodeOffsEnd = prevBb->bbCodeOffsEnd;
                                }

                                // Current pattern
                                optRngPattern.m_patterns[patternIndex] =
                                    (int)condition1->gtGetOp2()->AsIntCon()->IconValue();

                                // False jump
                                if (condition1->OperIs(GT_NE))
                                {
                                    optRngPattern.m_falseJmpBB = currBb->bbJumpDest;
                                }

                                // Update min and max patterns
                                if (optRngPattern.m_patterns[patternIndex] < optRngPattern.m_minPattern)
                                {
                                    optRngPattern.m_minPattern = optRngPattern.m_patterns[patternIndex];
                                    optRngPattern.m_minOp      = condition1->gtGetOp2();
                                }
                                else if (optRngPattern.m_patterns[patternIndex] > optRngPattern.m_maxPattern)
                                {
                                    optRngPattern.m_maxPattern = optRngPattern.m_patterns[patternIndex];
                                }
                                patternIndex++;

                                // Update the code offset range
                                optRngPattern.m_bbCodeOffsEnd = currBb->bbCodeOffsEnd;

                                foundPattern = true;
                            }
                        }
                    }
                }
            }
        }

        // Stop searching if pattern(s) has been found in previous BBs, but the current BB does not have a pattern
        if (foundPattern && patternIndex < (int)(currBb->bbNum - optRngPattern.m_firstPatternBBNum + 1))
        {
            break;
        }

        prevBb = currBb;
    }

    if (foundPattern)
    {
        optRngPattern.m_numFoundPatterns = patternIndex;

        printf("Reserved values:\n");
        for (int idx = 0; idx < optRngPattern.m_numFoundPatterns; idx++)
        {
            printf("%d ", optRngPattern.m_patterns[idx]);
        }
        printf("\n\n");

        // Find range of pattern values
        printf("Min pattern value: %d\n", optRngPattern.m_minPattern);
        printf("Max pattern value: %d\n", optRngPattern.m_maxPattern);
        optRngPattern.m_rangePattern =
            optRngPattern.m_maxPattern - optRngPattern.m_minPattern + 1; // min 0 max 2 -> range 3
        printf("Range of pattern values: %d\n", optRngPattern.m_rangePattern);
        if (optRngPattern.m_rangePattern > optRngPattern.m_sizePatterns) // TODO: item 0 is 1 and item 1 is 100, rnage
                                                                         // is 64 but only 2 items
        {
            printf("Range of pattern values is too big (> %d): %d\n", optRngPattern.m_sizePatterns,
                   optRngPattern.m_rangePattern);
            return PhaseStatus::MODIFIED_NOTHING;
        }

        //auto bitmapPatterns = 0;
        for (int idxPattern = 0; idxPattern < optRngPattern.m_numFoundPatterns; idxPattern++)
        {
            optRngPattern.bitmapPatterns |= (1 << (optRngPattern.m_patterns[idxPattern] - optRngPattern.m_minPattern));
        }
        printf("Bitmap of pattern values: %d\n", optRngPattern.bitmapPatterns);

        // Replace BBs with a Switch tree
        if (!optRngPattern.optMakeSwitchBBdesc())
        {
            return PhaseStatus::MODIFIED_NOTHING;
        }
        if (optRngPattern.optUpdateBlocks())
        {
            fgDispBasicBlocks(true);
            return PhaseStatus::MODIFIED_EVERYTHING;
        }
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
