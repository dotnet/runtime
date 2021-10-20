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
// Policy decisions implemented here:
//
//   * One counter per stack frame, regardless of the number of patchpoints.
//   * Shared counter value initialized to zero in prolog.
//   * Patchpoint trees fully expanded into jit IR. Deferring expansion could
//       lead to more compact code and lessen size overhead for Tier0.
//
// Workarounds and limitations:
//
//   * no patchpoints in handler regions
//   * no patchpoints for localloc methods
//   * no patchpoints for synchronized methods (workaround)
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
        if (compiler->fgFirstBB->bbFlags & BBF_PATCHPOINT)
        {
            compiler->fgEnsureFirstBBisScratch();
        }

        int count = 0;
        for (BasicBlock* const block : compiler->Blocks(compiler->fgFirstBB->bbNext))
        {
            if (block->bbFlags & BBF_PATCHPOINT)
            {
                // Clear the patchpoint flag.
                //
                block->bbFlags &= ~BBF_PATCHPOINT;

                // If block is in a handler region, don't insert a patchpoint.
                // We can't OSR from funclets.
                //
                // TODO: check this earlier, somehow, and fall back to fully
                // optimizing the method (ala QJFL=0).
                if (compiler->ehGetBlockHndDsc(block) != nullptr)
                {
                    JITDUMP("Patchpoint: skipping patchpoint for " FMT_BB " as it is in a handler\n", block->bbNum);
                    continue;
                }

                JITDUMP("Patchpoint: instrumenting " FMT_BB "\n", block->bbNum);
                assert(block != compiler->fgFirstBB);
                TransformBlock(block);
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
    BasicBlock* CreateAndInsertBasicBlock(BBjumpKinds jumpKind, BasicBlock* insertAfter)
    {
        BasicBlock* block = compiler->fgNewBBafter(jumpKind, insertAfter, true);
        block->bbFlags |= BBF_IMPORTED;
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
        BasicBlock* helperBlock    = CreateAndInsertBasicBlock(BBJ_NONE, block);

        // Update flow and flags
        block->bbJumpKind = BBJ_COND;
        block->bbJumpDest = remainderBlock;
        helperBlock->bbFlags |= BBF_BACKWARD_JUMP;
        block->bbFlags |= BBF_INTERNAL;

        // Update weights
        remainderBlock->inheritWeight(block);
        helperBlock->inheritWeightPercentage(block, 100 - HIGH_PROBABILITY);

        // Fill in test block
        //
        // --ppCounter;
        GenTree* ppCounterBefore = compiler->gtNewLclvNode(ppCounterLclNum, TYP_INT);
        GenTree* ppCounterAfter  = compiler->gtNewLclvNode(ppCounterLclNum, TYP_INT);
        GenTree* one             = compiler->gtNewIconNode(1, TYP_INT);
        GenTree* ppCounterSub    = compiler->gtNewOperNode(GT_SUB, TYP_INT, ppCounterBefore, one);
        GenTree* ppCounterAsg    = compiler->gtNewOperNode(GT_ASG, TYP_INT, ppCounterAfter, ppCounterSub);

        compiler->fgNewStmtAtEnd(block, ppCounterAsg);

        // if (ppCounter > 0), bypass helper call
        GenTree* ppCounterUpdated = compiler->gtNewLclvNode(ppCounterLclNum, TYP_INT);
        GenTree* zero             = compiler->gtNewIconNode(0, TYP_INT);
        GenTree* compare          = compiler->gtNewOperNode(GT_GT, TYP_INT, ppCounterUpdated, zero);
        GenTree* jmp              = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, compare);

        compiler->fgNewStmtAtEnd(block, jmp);

        // Fill in helper block
        //
        // call PPHelper(&ppCounter, ilOffset)
        GenTree*          ilOffsetNode  = compiler->gtNewIconNode(ilOffset, TYP_INT);
        GenTree*          ppCounterRef  = compiler->gtNewLclvNode(ppCounterLclNum, TYP_INT);
        GenTree*          ppCounterAddr = compiler->gtNewOperNode(GT_ADDR, TYP_I_IMPL, ppCounterRef);
        GenTreeCall::Use* helperArgs    = compiler->gtNewCallArgs(ppCounterAddr, ilOffsetNode);
        GenTreeCall*      helperCall    = compiler->gtNewHelperCallNode(CORINFO_HELP_PATCHPOINT, TYP_VOID, helperArgs);

        compiler->fgNewStmtAtEnd(helperBlock, helperCall);
    }

    //  ppCounter = <initial value>
    void TransformEntry(BasicBlock* block)
    {
        assert((block->bbFlags & BBF_PATCHPOINT) == 0);

        int initialCounterValue = JitConfig.TC_OnStackReplacement_InitialCounter();

        if (initialCounterValue < 0)
        {
            initialCounterValue = 0;
        }

        GenTree* initialCounterNode = compiler->gtNewIconNode(initialCounterValue, TYP_INT);
        GenTree* ppCounterRef       = compiler->gtNewLclvNode(ppCounterLclNum, TYP_INT);
        GenTree* ppCounterAsg       = compiler->gtNewOperNode(GT_ASG, TYP_INT, ppCounterRef, initialCounterNode);

        compiler->fgNewStmtNearEnd(block, ppCounterAsg);
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
    if (!doesMethodHavePatchpoints())
    {
        JITDUMP("\n -- no patchpoints to transform\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // We should only be adding patchpoints at Tier0, so should not be in an inlinee
    assert(!compIsForInlining());

    // We currently can't do OSR in methods with localloc.
    // Such methods don't have a fixed relationship between frame and stack pointers.
    //
    // This is true whether or not the localloc was executed in the original method.
    //
    // TODO: handle this case, or else check this earlier and fall back to fully
    // optimizing the method (ala QJFL=0).
    if (compLocallocUsed)
    {
        JITDUMP("\n -- unable to handle methods with localloc\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    // We currently can't do OSR in synchronized methods. We need to alter
    // the logic in fgAddSyncMethodEnterExit for OSR to not try and obtain the
    // monitor (since the original method will have done so) and set the monitor
    // obtained flag to true (or reuse the original method slot value).
    if ((info.compFlags & CORINFO_FLG_SYNCH) != 0)
    {
        JITDUMP("\n -- unable to handle synchronized methods\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (opts.IsReversePInvoke())
    {
        JITDUMP(" -- unable to handle Reverse P/Invoke\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    PatchpointTransformer ppTransformer(this);
    int                   count = ppTransformer.Run();
    JITDUMP("\n -- %d patchpoints transformed\n", count);
    return (count == 0) ? PhaseStatus::MODIFIED_NOTHING : PhaseStatus::MODIFIED_EVERYTHING;
}
