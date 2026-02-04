// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                   Double NOT Optimizer                                    XX
XX                                                                           XX
XX  This phase identifies and eliminates double bitwise-NOT patterns that    XX
XX  arise from boolean operations and branching.                             XX
XX                                                                           XX
XX  IMPORTANT: This optimization requires JitOptRepeat to be effective.      XX
XX  On the first optimization pass, assertion propagation typically has      XX
XX  PHI nodes that prevent this optimization from matching the pattern.      XX
XX  On the second pass, assertion prop has simplified IR and valid SSA,      XX
XX  enabling the double NOT pattern to be recognized and optimized.          XX
XX                                                                           XX
XX  Pattern matched (second opt-repeat iteration):                           XX
XX    BB01: V03 = ~V04         // First NOT                                  XX
XX    BB01: V05 = ~V03         // Second NOT on same value                   XX
XX                                                                           XX
XX  Optimized to:                                                            XX
XX    BB01: V05 = V04          // Direct use of original value               XX
XX                                                                           XX
XX  Common sources:                                                          XX
XX    - Boolean ternaries: (x ? !a : a)                                      XX
XX    - Redundant boolean inversions from control flow                       XX
XX    - Spill clique stores with inverted conditions                         XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// optOptimizeDoubleNots: Eliminate double bitwise-NOT patterns using SSA
//
// Returns:
//    Phase status indicating whether any optimizations were performed
//
// Notes:
//    This optimization relies on SSA to track value flow across blocks.
//    It only handles cases where both NOTs are in the same block or where
//    the definition block's SSA metadata is accurate.
//
//    CRITICAL DEPENDENCY ON JitOptRepeat:
//    This optimization typically does NOT work on the first optimization pass
//    because IR still has PHI nodes. The pattern becomes visible on the
//    second opt-repeat iteration when:
//    1. PHI nodes have been eliminated
//    2. SSA form is valid and accurately tracks definitions
//    3. The double NOT pattern is exposed in a linear form within a single block
//
//    Without JitOptRepeat, this phase will rarely find optimizable patterns.
//
//    Known limitations:
//    - Does not handle PHI nodes with unreachable predecessors
//    - Requires single-block or accurately tracked cross-block patterns
//    - May miss opportunities if SSA metadata is stale after block compaction
//
// Pattern matched:
//   BB01: V03 = ~V04   // First NOT on def of V03
//   BB01: V05 = ~V03   // Second NOT on last use of V03
//
// Optimized to:
//   BB01: V05 = V04    // Eliminate both NOTs
//
PhaseStatus Compiler::optOptimizeDoubleNots()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In optOptimizeDoubleNots()\n");
    }
#endif

    // This optimization requires JitOptRepeat and should only run on the second iteration or later
    // when assertion propagation has simplified PHI nodes
    assert(opts.optRepeat && (opts.optRepeatIteration >= 2));

    PhaseStatus modified = PhaseStatus::MODIFIED_NOTHING;

    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            GenTree* tree = stmt->GetRootNode();
            if (tree->OperIs(GT_STORE_LCL_VAR, GT_RETURN))
            {
                GenTree* value = tree->OperIs(GT_STORE_LCL_VAR) ? tree->AsOp()->gtOp1 : tree->AsUnOp()->gtOp1;

                if (value != nullptr && value->OperIs(GT_NOT))
                {
                    GenTree* innerOp = value->AsOp()->gtOp1;

                    // Check if innerOp is a local that was assigned from NOT
                    // Note: On first opt-repeat pass, this local often comes from a PHI
                    // which prevents us from finding the defining NOT in the same block
                    if (innerOp->OperIs(GT_LCL_VAR))
                    {
                        unsigned lclNum = innerOp->AsLclVar()->GetLclNum();
                        unsigned ssaNum = innerOp->AsLclVar()->GetSsaNum();

                        JITDUMP("  Checking NOT(V%02u), SSA#%u\n", lclNum, ssaNum);

                        if (ssaNum != SsaConfig::RESERVED_SSA_NUM)
                        {
                            LclVarDsc* varDsc = lvaGetDesc(lclNum);

                            // Validate SSA data availability
                            if (varDsc->lvPerSsaData.GetCount() == 0)
                            {
                                JITDUMP("  No SSA data for V%02u\n", lclNum);
                                continue;
                            }

                            // Validate SSA number is in range
                            if (!varDsc->lvPerSsaData.IsValidSsaNum(ssaNum))
                            {
                                JITDUMP("  Invalid SSA#%u for V%02u (count=%u)\n", ssaNum, lclNum,
                                        varDsc->lvPerSsaData.GetCount());
                                continue;
                            }

                            LclSsaVarDsc* ssaDef   = varDsc->GetPerSsaData(ssaNum);
                            GenTree*      defNode  = ssaDef->GetDefNode();
                            BasicBlock*   defBlock = ssaDef->GetBlock();

                            JITDUMP("  DefNode: %s, DefBlock: " FMT_BB "\n",
                                    defNode ? GenTree::OpName(defNode->OperGet()) : "null",
                                    defBlock ? defBlock->bbNum : 0);

                            // Find the definition in the CURRENT block
                            Statement* defStmt = nullptr;
                            if (defNode != nullptr)
                            {
                                for (Statement* const candidateStmt : block->Statements())
                                {
                                    if (candidateStmt->GetRootNode() == defNode)
                                    {
                                        defStmt  = candidateStmt;
                                        defNode  = defStmt->GetRootNode(); // Update defNode to current Node
                                        defBlock = block;                  // Update defBlock to current block
                                        JITDUMP("  Found definition in current block!\n");
                                        break;
                                    }
                                }
                            }

                            // If not found in current block, skip
                            if (defStmt == nullptr)
                            {
                                JITDUMP("  Definition not in current block - skipping\n");
                                continue;
                            }

                            if (defNode && defNode->OperIs(GT_STORE_LCL_VAR))
                            {
                                GenTree* defRhs = defNode->AsOp()->gtOp1;
                                if (defRhs->OperIs(GT_NOT))
                                {
                                    // Found double NOT: eliminate it
                                    JITDUMP("  FOUND DOUBLE NOT PATTERN! Optimizing...\n");
                                    GenTree* baseExpr = defRhs->AsOp()->gtOp1;
                                    if (baseExpr == nullptr)
                                    {
                                        continue;
                                    }
                                    GenTree* replacement = gtCloneExpr(baseExpr);

                                    if (tree->OperIs(GT_STORE_LCL_VAR))
                                    {
                                        tree->AsOp()->gtOp1 = replacement;
                                    }
                                    else
                                    {
                                        tree->AsUnOp()->gtOp1 = replacement;
                                    }

                                    // Update use statements in use block
                                    if (fgNodeThreading != NodeThreading::None)
                                    {
                                        gtSetStmtInfo(stmt);
                                        fgSetStmtSeq(stmt);
                                    }

                                    modified = PhaseStatus::MODIFIED_EVERYTHING;

                                    JITDUMP("Removed double NOT in " FMT_BB "\n", block->bbNum);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    JITDUMP("\nCompleted optimize DOUBLE NOTs Phase\n");

    return modified;
}
