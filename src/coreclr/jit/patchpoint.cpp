// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// PatchpointTransformer
//
// Insert patchpoint checks into Tier0 methods, based on locations identified
// during importation (see impImportBlockCode).
//
// There are now two different types of patchpoints:
//   * loop based: enable OSR transitions in loops
//   * partial compilation: allows partial compilation of original method
//
// Loop patchpoint policy decisions implemented here:
//   * One counter per stack frame, regardless of the number of patchpoints.
//   * Shared counter value initialized to a constant in the prolog.
//   * Patchpoint trees fully expanded into jit IR. Deferring expansion could
//       lead to more compact code and lessen size overhead for Tier0.
//
// Workarounds and limitations:
//
//   * no patchpoints in handler regions
//   * no patchpoints for localloc methods
//
class PatchpointTransformer
{
    const int HIGH_PROBABILITY = 99;
    unsigned  ppCounterLclNum;
    Compiler* compiler;

public:
    PatchpointTransformer(Compiler* compiler) : ppCounterLclNum(BAD_VAR_NUM), compiler(compiler)
    {
    }

    //------------------------------------------------------------------------
    // Run: run transformation for each block.
    //
    // Returns:
    //   Number of patchpoints transformed.
    int Run()
    {
        // If the first block is a patchpoint, insert a scratch block.
        if (compiler->fgFirstBB->HasFlag(BBF_PATCHPOINT))
        {
            compiler->fgEnsureFirstBBisScratch();
        }

        int count = 0;
        for (BasicBlock* const block : compiler->Blocks(compiler->fgFirstBB->Next()))
        {
            if (block->HasFlag(BBF_PATCHPOINT))
            {
                // We can't OSR from funclets.
                //
                assert(!block->hasHndIndex());

                // Clear the patchpoint flag.
                //
                block->RemoveFlags(BBF_PATCHPOINT);

                JITDUMP("Patchpoint: regular patchpoint in " FMT_BB "\n", block->bbNum);
                TransformBlock(block);
                count++;
            }
            else if (block->HasFlag(BBF_PARTIAL_COMPILATION_PATCHPOINT))
            {
                // We can't OSR from funclets.
                // Also, we don't import the IL for these blocks.
                //
                assert(!block->hasHndIndex());

                // If we're instrumenting, we should not have decided to
                // put class probes here, as that is driven by looking at IL.
                //
                assert(!block->HasFlag(BBF_HAS_HISTOGRAM_PROFILE));

                // Clear the partial comp flag.
                //
                block->RemoveFlags(BBF_PARTIAL_COMPILATION_PATCHPOINT);

                JITDUMP("Patchpoint: partial compilation patchpoint in " FMT_BB "\n", block->bbNum);
                TransformPartialCompilation(block);
                count++;
            }
        }

        return count;
    }

private:
    //------------------------------------------------------------------------
    // CreateAndInsertBasicBlock: ask compiler to create new basic block.
    // and insert in into the basic block list.
    //
    // Arguments:
    //    jumpKind - jump kind for the new basic block
    //    insertAfter - basic block, after which compiler has to insert the new one.
    //
    // Return Value:
    //    new basic block.
    BasicBlock* CreateAndInsertBasicBlock(BBKinds jumpKind, BasicBlock* insertAfter)
    {
        BasicBlock* block = compiler->fgNewBBafter(jumpKind, insertAfter, true);
        block->SetFlags(BBF_IMPORTED);
        return block;
    }

    //------------------------------------------------------------------------
    // TransformBlock: expand current block to include patchpoint logic.
    //
    //  S;
    //
    //  ==>
    //
    //  if (--ppCounter <= 0)
    //  {
    //     ppHelper(&ppCounter, ilOffset);
    //  }
    //  S;
    //
    void TransformBlock(BasicBlock* block)
    {
        // If we haven't allocated the counter temp yet, set it up
        if (ppCounterLclNum == BAD_VAR_NUM)
        {
            ppCounterLclNum                            = compiler->lvaGrabTemp(true DEBUGARG("patchpoint counter"));
            compiler->lvaTable[ppCounterLclNum].lvType = TYP_INT;

            // and initialize in the entry block
            TransformEntry(compiler->fgFirstBB);
        }

        // Capture the IL offset
        IL_OFFSET ilOffset = block->bbCodeOffs;
        assert(ilOffset != BAD_IL_OFFSET);

        // Current block now becomes the test block
        BasicBlock* remainderBlock = compiler->fgSplitBlockAtBeginning(block);
        BasicBlock* helperBlock    = CreateAndInsertBasicBlock(BBJ_ALWAYS, block);

        // Update flow and flags
        block->SetFlags(BBF_INTERNAL);
        helperBlock->SetFlags(BBF_BACKWARD_JUMP | BBF_NONE_QUIRK);

        FlowEdge* const falseEdge = compiler->fgAddRefPred(helperBlock, block);
        FlowEdge* const trueEdge  = compiler->fgGetPredForBlock(remainderBlock, block);
        trueEdge->setLikelihood(HIGH_PROBABILITY / 100.0);
        falseEdge->setLikelihood((100 - HIGH_PROBABILITY) / 100.0);
        block->SetCond(trueEdge, falseEdge);

        FlowEdge* const newEdge = compiler->fgAddRefPred(remainderBlock, helperBlock);
        newEdge->setLikelihood(1.0);
        helperBlock->SetTargetEdge(newEdge);

        // Update weights
        remainderBlock->inheritWeight(block);
        helperBlock->inheritWeightPercentage(block, 100 - HIGH_PROBABILITY);

        // Fill in test block
        //
        // --ppCounter;
        GenTree* ppCounterBefore = compiler->gtNewLclvNode(ppCounterLclNum, TYP_INT);
        GenTree* one             = compiler->gtNewIconNode(1, TYP_INT);
        GenTree* ppCounterSub    = compiler->gtNewOperNode(GT_SUB, TYP_INT, ppCounterBefore, one);
        GenTree* ppCounterUpdate = compiler->gtNewStoreLclVarNode(ppCounterLclNum, ppCounterSub);

        compiler->fgNewStmtAtEnd(block, ppCounterUpdate);

        // if (ppCounter > 0), bypass helper call
        GenTree* ppCounterUpdated = compiler->gtNewLclvNode(ppCounterLclNum, TYP_INT);
        GenTree* zero             = compiler->gtNewIconNode(0, TYP_INT);
        GenTree* compare          = compiler->gtNewOperNode(GT_GT, TYP_INT, ppCounterUpdated, zero);
        GenTree* jmp              = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, compare);

        compiler->fgNewStmtAtEnd(block, jmp);

        // Fill in helper block
        //
        // call PPHelper(&ppCounter, ilOffset)
        GenTree*     ilOffsetNode  = compiler->gtNewIconNode(ilOffset, TYP_INT);
        GenTree*     ppCounterAddr = compiler->gtNewLclVarAddrNode(ppCounterLclNum);
        GenTreeCall* helperCall =
            compiler->gtNewHelperCallNode(CORINFO_HELP_PATCHPOINT, TYP_VOID, ppCounterAddr, ilOffsetNode);

        compiler->fgNewStmtAtEnd(helperBlock, helperCall);
    }

    //  ppCounter = <initial value>
    void TransformEntry(BasicBlock* block)
    {
        assert(!block->HasFlag(BBF_PATCHPOINT));

        int initialCounterValue = JitConfig.TC_OnStackReplacement_InitialCounter();

        if (initialCounterValue < 0)
        {
            initialCounterValue = 0;
        }

        GenTree* initialCounterNode = compiler->gtNewIconNode(initialCounterValue, TYP_INT);
        GenTree* ppCounterStore     = compiler->gtNewStoreLclVarNode(ppCounterLclNum, initialCounterNode);

        compiler->fgNewStmtNearEnd(block, ppCounterStore);
    }

    //------------------------------------------------------------------------
    // TransformPartialCompilation: delete all the statements in the block and insert
    //     a call to the partial compilation patchpoint helper
    //
    //  S0; S1; S2; ... SN;
    //
    //  ==>
    //
    //  ~~{ S0; ... SN; }~~ (deleted)
    //  call JIT_PARTIAL_COMPILATION_PATCHPOINT(ilOffset)
    //
    // Note S0 -- SN are not forever lost -- they will appear in the OSR version
    // of the method created when the patchpoint is hit. Also note the patchpoint
    // helper call will not return control to this method.
    //
    void TransformPartialCompilation(BasicBlock* block)
    {
        // Capture the IL offset
        IL_OFFSET ilOffset = block->bbCodeOffs;
        assert(ilOffset != BAD_IL_OFFSET);

        // Remove all statements from the block.
        for (Statement* stmt : block->Statements())
        {
            compiler->fgRemoveStmt(block, stmt);
        }

        // Update flow
        block->SetKindAndTargetEdge(BBJ_THROW);

        // Add helper call
        //
        // call PartialCompilationPatchpointHelper(ilOffset)
        //
        GenTree*     ilOffsetNode = compiler->gtNewIconNode(ilOffset, TYP_INT);
        GenTreeCall* helperCall =
            compiler->gtNewHelperCallNode(CORINFO_HELP_PARTIAL_COMPILATION_PATCHPOINT, TYP_VOID, ilOffsetNode);

        compiler->fgNewStmtAtEnd(block, helperCall);
    }
};

//------------------------------------------------------------------------
// fgTransformPatchpoints: expansion of patchpoints into control flow.
//
// Notes:
//
// Patchpoints are placed in the JIT IR during importation, and get expanded
// here into normal JIT IR.
//
// Returns:
//   phase status indicating if changes were made
//
PhaseStatus Compiler::fgTransformPatchpoints()
{
    if (!doesMethodHavePatchpoints() && !doesMethodHavePartialCompilationPatchpoints())
    {
        JITDUMP("\n -- no patchpoints to transform\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // We should only be adding patchpoints at Tier0, so should not be in an inlinee
    assert(!compIsForInlining());

    // We should be allowed to have patchpoints in this method.
    assert(compCanHavePatchpoints());

    PatchpointTransformer ppTransformer(this);
    int                   count = ppTransformer.Run();
    JITDUMP("\n -- %d patchpoints transformed\n", count);
    return (count == 0) ? PhaseStatus::MODIFIED_NOTHING : PhaseStatus::MODIFIED_EVERYTHING;
}
