// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX            Code Generation Support Methods for Linear Codegen             XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "emit.h"
#include "codegen.h"

//------------------------------------------------------------------------
// genInitializeRegisterState: Initialize the register state contained in 'regSet'.
//
// Assumptions:
//    On exit the "rsModifiedRegsMask" (in "regSet") holds all the registers' masks hosting an argument on the function
//    and elements of "rsSpillDesc" (in "regSet") are setted to nullptr.
//
// Notes:
//    This method is intended to be called only from initializeStructuresBeforeBlockCodeGeneration.
void CodeGen::genInitializeRegisterState()
{
    // Initialize the spill tracking logic

    regSet.rsSpillBeg();

    // If any arguments live in registers, mark those regs as such

    unsigned   varNum;
    LclVarDsc* varDsc;

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        // Is this variable a parameter assigned to a register?
        if (!varDsc->lvIsParam || !varDsc->lvRegister)
        {
            continue;
        }

        // Is the argument live on entry to the method?
        if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            continue;
        }

        if (varDsc->IsAddressExposed())
        {
            continue;
        }

        // Mark the register as holding the variable
        regNumber reg = varDsc->GetRegNum();
        if (genIsValidIntReg(reg))
        {
            regSet.verifyRegUsed(reg);
        }
    }
}

//------------------------------------------------------------------------
// genInitialize: Initialize Scopes, registers, gcInfo and current liveness variables structures
// used in the generation of blocks' code before.
//
// Assumptions:
//    -The pointer logic in "gcInfo" for pointers on registers and variable is cleaned.
//    -"compiler->compCurLife" becomes an empty set
//    -"compiler->compCurLife" are set to be a clean set
//    -If there is local var info siScopes scope logic in codegen is initialized in "siInit()"
//
// Notes:
//    This method is intended to be called when code generation for blocks happens, and before the list of blocks is
//    iterated.
void CodeGen::genInitialize()
{
    // Initialize the line# tracking logic
    if (compiler->opts.compScopeInfo)
    {
        siInit();
    }

#ifdef USING_VARIABLE_LIVE_RANGE
    initializeVariableLiveKeeper();
#endif //  USING_VARIABLE_LIVE_RANGE

    genPendingCallLabel = nullptr;

    // Initialize the pointer tracking code

    gcInfo.gcRegPtrSetInit();
    gcInfo.gcVarPtrSetInit();

    // Initialize the register set logic

    genInitializeRegisterState();

    // Make sure a set is allocated for compiler->compCurLife (in the long case), so we can set it to empty without
    // allocation at the start of each basic block.
    VarSetOps::AssignNoCopy(compiler, compiler->compCurLife, VarSetOps::MakeEmpty(compiler));

    // We initialize the stack level before first "BasicBlock" code is generated in case we need to report stack
    // variable needs home and so its stack offset.
    SetStackLevel(0);
}

//------------------------------------------------------------------------
// genCodeForBBlist: Generate code for all the blocks in a method
//
// Arguments:
//    None
//
// Notes:
//    This is the main method for linear codegen. It calls genCodeForTreeNode
//    to generate the code for each node in each BasicBlock, and handles BasicBlock
//    boundaries and branches.
//
void CodeGen::genCodeForBBlist()
{
    unsigned savedStkLvl;

#ifdef DEBUG
    genInterruptibleUsed = true;

    // You have to be careful if you create basic blocks from now on
    compiler->fgSafeBasicBlockCreation = false;
#endif // DEBUG

#if defined(DEBUG) && defined(TARGET_X86)

    // Check stack pointer on call stress mode is not compatible with fully interruptible GC. REVIEW: why?
    //
    if (GetInterruptible() && compiler->opts.compStackCheckOnCall)
    {
        compiler->opts.compStackCheckOnCall = false;
    }

#endif // defined(DEBUG) && defined(TARGET_X86)

#if defined(DEBUG) && defined(TARGET_XARCH)

    // Check stack pointer on return stress mode is not compatible with fully interruptible GC. REVIEW: why?
    // It is also not compatible with any function that makes a tailcall: we aren't smart enough to only
    // insert the SP check in the non-tailcall returns.
    //
    if ((GetInterruptible() || compiler->compTailCallUsed) && compiler->opts.compStackCheckOnRet)
    {
        compiler->opts.compStackCheckOnRet = false;
    }

#endif // defined(DEBUG) && defined(TARGET_XARCH)

    genMarkLabelsForCodegen();

    assert(!compiler->fgFirstBBScratch ||
           compiler->fgFirstBB == compiler->fgFirstBBScratch); // compiler->fgFirstBBScratch has to be first.

    /* Initialize structures used in the block list iteration */
    genInitialize();

    /*-------------------------------------------------------------------------
     *
     *  Walk the basic blocks and generate code for each one
     *
     */

    BasicBlock* block;

    for (block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
    {

#ifdef DEBUG
        if (compiler->verbose)
        {
            printf("\n=============== Generating ");
            block->dspBlockHeader(compiler, true, true);
            compiler->fgDispBBLiveness(block);
        }
#endif // DEBUG

        assert(LIR::AsRange(block).CheckLIR(compiler));

        // Figure out which registers hold variables on entry to this block

        regSet.ClearMaskVars();
        gcInfo.gcRegGCrefSetCur = RBM_NONE;
        gcInfo.gcRegByrefSetCur = RBM_NONE;

        compiler->m_pLinearScan->recordVarLocationsAtStartOfBB(block);

        // Updating variable liveness after last instruction of previous block was emitted
        // and before first of the current block is emitted
        genUpdateLife(block->bbLiveIn);

        // Even if liveness didn't change, we need to update the registers containing GC references.
        // genUpdateLife will update the registers live due to liveness changes. But what about registers that didn't
        // change? We cleared them out above. Maybe we should just not clear them out, but update the ones that change
        // here. That would require handling the changes in recordVarLocationsAtStartOfBB().

        regMaskTP newLiveRegSet  = RBM_NONE;
        regMaskTP newRegGCrefSet = RBM_NONE;
        regMaskTP newRegByrefSet = RBM_NONE;
#ifdef DEBUG
        VARSET_TP removedGCVars(VarSetOps::MakeEmpty(compiler));
        VARSET_TP addedGCVars(VarSetOps::MakeEmpty(compiler));
#endif
        VarSetOps::Iter iter(compiler, block->bbLiveIn);
        unsigned        varIndex = 0;
        while (iter.NextElem(&varIndex))
        {
            LclVarDsc* varDsc = compiler->lvaGetDescByTrackedIndex(varIndex);

            if (varDsc->lvIsInReg())
            {
                newLiveRegSet |= varDsc->lvRegMask();
                if (varDsc->lvType == TYP_REF)
                {
                    newRegGCrefSet |= varDsc->lvRegMask();
                }
                else if (varDsc->lvType == TYP_BYREF)
                {
                    newRegByrefSet |= varDsc->lvRegMask();
                }
                if (!varDsc->IsAlwaysAliveInMemory())
                {
#ifdef DEBUG
                    if (verbose && VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varIndex))
                    {
                        VarSetOps::AddElemD(compiler, removedGCVars, varIndex);
                    }
#endif // DEBUG
                    VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varIndex);
                }
            }
            if ((!varDsc->lvIsInReg() || varDsc->IsAlwaysAliveInMemory()) && compiler->lvaIsGCTracked(varDsc))
            {
#ifdef DEBUG
                if (verbose && !VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varIndex))
                {
                    VarSetOps::AddElemD(compiler, addedGCVars, varIndex);
                }
#endif // DEBUG
                VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varIndex);
            }
        }

        regSet.SetMaskVars(newLiveRegSet);

#ifdef DEBUG
        if (compiler->verbose)
        {
            if (!VarSetOps::IsEmpty(compiler, addedGCVars))
            {
                printf("\t\t\t\t\t\t\tAdded GCVars: ");
                dumpConvertedVarSet(compiler, addedGCVars);
                printf("\n");
            }
            if (!VarSetOps::IsEmpty(compiler, removedGCVars))
            {
                printf("\t\t\t\t\t\t\tRemoved GCVars: ");
                dumpConvertedVarSet(compiler, removedGCVars);
                printf("\n");
            }
        }
#endif // DEBUG

        gcInfo.gcMarkRegSetGCref(newRegGCrefSet DEBUGARG(true));
        gcInfo.gcMarkRegSetByref(newRegByrefSet DEBUGARG(true));

        /* Blocks with handlerGetsXcptnObj()==true use GT_CATCH_ARG to
           represent the exception object (TYP_REF).
           We mark REG_EXCEPTION_OBJECT as holding a GC object on entry
           to the block,  it will be the first thing evaluated
           (thanks to GTF_ORDER_SIDEEFF).
         */

        if (handlerGetsXcptnObj(block->bbCatchTyp))
        {
            for (GenTree* node : LIR::AsRange(block))
            {
                if (node->OperGet() == GT_CATCH_ARG)
                {
                    gcInfo.gcMarkRegSetGCref(RBM_EXCEPTION_OBJECT);
                    break;
                }
            }
        }

#if defined(TARGET_ARM)
        genInsertNopForUnwinder(block);
#endif

        /* Start a new code output block */

        genUpdateCurrentFunclet(block);

        genLogLabel(block);

        // Tell everyone which basic block we're working on

        compiler->compCurBB = block;

        block->bbEmitCookie = nullptr;

        // If this block is a jump target or it requires a label then set 'needLabel' to true,
        //
        bool needLabel = (block->bbFlags & BBF_HAS_LABEL) != 0;

        if (block == compiler->fgFirstColdBlock)
        {
#ifdef DEBUG
            if (compiler->verbose)
            {
                printf("\nThis is the start of the cold region of the method\n");
            }
#endif
            // We should never have a block that falls through into the Cold section
            noway_assert(!block->bbPrev->bbFallsThrough());

            needLabel = true;
        }

        // We also want to start a new Instruction group by calling emitAddLabel below,
        // when we need accurate bbWeights for this block in the emitter.  We force this
        // whenever our previous block was a BBJ_COND and it has a different weight than us.
        //
        // Note: We need to have set compCurBB before calling emitAddLabel
        //
        if ((block->bbPrev != nullptr) && (block->bbPrev->bbJumpKind == BBJ_COND) &&
            (block->bbWeight != block->bbPrev->bbWeight))
        {
            JITDUMP("Adding label due to BB weight difference: BBJ_COND " FMT_BB " with weight " FMT_WT
                    " different from " FMT_BB " with weight " FMT_WT "\n",
                    block->bbPrev->bbNum, block->bbPrev->bbWeight, block->bbNum, block->bbWeight);
            needLabel = true;
        }

#if FEATURE_LOOP_ALIGN
        if (GetEmitter()->emitEndsWithAlignInstr())
        {
            // Force new label if current IG ends with an align instruction.
            needLabel = true;
        }
#endif

        if (needLabel)
        {
            // Mark a label and update the current set of live GC refs

            block->bbEmitCookie = GetEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                             gcInfo.gcRegByrefSetCur, false DEBUG_ARG(block));
        }

        if (block == compiler->fgFirstColdBlock)
        {
            // We require the block that starts the Cold section to have a label
            noway_assert(block->bbEmitCookie);
            GetEmitter()->emitSetFirstColdIGCookie(block->bbEmitCookie);
        }

        // Both stacks are always empty on entry to a basic block.
        assert(genStackLevel == 0);
        genAdjustStackLevel(block);
        savedStkLvl = genStackLevel;

        // Needed when jitting debug code
        siBeginBlock(block);

        // BBF_INTERNAL blocks don't correspond to any single IL instruction.
        if (compiler->opts.compDbgInfo && (block->bbFlags & BBF_INTERNAL) &&
            !compiler->fgBBisScratch(block)) // If the block is the distinguished first scratch block, then no need to
                                             // emit a NO_MAPPING entry, immediately after the prolog.
        {
            genIPmappingAdd(IPmappingDscKind::NoMapping, DebugInfo(), true);
        }

        bool firstMapping = true;

#if defined(FEATURE_EH_FUNCLETS)
        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            genReserveFuncletProlog(block);
        }
#endif // FEATURE_EH_FUNCLETS

        // Clear compCurStmt and compCurLifeTree.
        compiler->compCurStmt     = nullptr;
        compiler->compCurLifeTree = nullptr;

        // Emit poisoning into scratch BB that comes right after prolog.
        // We cannot emit this code in the prolog as it might make the prolog too large.
        if (compiler->compShouldPoisonFrame() && compiler->fgBBisScratch(block))
        {
            genPoisonFrame(newLiveRegSet);
        }

        // Traverse the block in linear order, generating code for each node as we
        // as we encounter it.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        // Set the use-order numbers for each node.
        {
            int useNum = 0;
            for (GenTree* node : LIR::AsRange(block))
            {
                assert((node->gtDebugFlags & GTF_DEBUG_NODE_CG_CONSUMED) == 0);

                node->gtUseNum = -1;
                if (node->isContained() || node->IsCopyOrReload())
                {
                    continue;
                }

                for (GenTree* operand : node->Operands())
                {
                    genNumberOperandUse(operand, useNum);
                }
            }
        }

        bool addPreciseMappings =
            (JitConfig.JitDumpPreciseDebugInfoFile() != nullptr) || (JitConfig.JitDisasmWithDebugInfo() != 0);
#endif // DEBUG

        DebugInfo currentDI;
        for (GenTree* node : LIR::AsRange(block))
        {
            // Do we have a new IL offset?
            if (node->OperGet() == GT_IL_OFFSET)
            {
                GenTreeILOffset* ilOffset = node->AsILOffset();
                DebugInfo        rootDI   = ilOffset->gtStmtDI.GetRoot();
                if (rootDI.IsValid())
                {
                    genEnsureCodeEmitted(currentDI);
                    currentDI = rootDI;
                    genIPmappingAdd(IPmappingDscKind::Normal, currentDI, firstMapping);
                    firstMapping = false;
                }

#ifdef DEBUG
                if (addPreciseMappings && ilOffset->gtStmtDI.IsValid())
                {
                    genAddPreciseIPMappingHere(ilOffset->gtStmtDI);
                }

                assert(ilOffset->gtStmtLastILoffs <= compiler->info.compILCodeSize ||
                       ilOffset->gtStmtLastILoffs == BAD_IL_OFFSET);

                if (compiler->opts.dspCode && compiler->opts.dspInstrs && ilOffset->gtStmtLastILoffs != BAD_IL_OFFSET)
                {
                    while (genCurDispOffset <= ilOffset->gtStmtLastILoffs)
                    {
                        genCurDispOffset += dumpSingleInstr(compiler->info.compCode, genCurDispOffset, ">    ");
                    }
                }

#endif // DEBUG
            }

            genCodeForTreeNode(node);
            if (node->gtHasReg(compiler) && node->IsUnusedValue())
            {
                genConsumeReg(node);
            }
        } // end for each node in block

#ifdef DEBUG
        // The following set of register spill checks and GC pointer tracking checks used to be
        // performed at statement boundaries. Now, with LIR, there are no statements, so they are
        // performed at the end of each block.
        // TODO: could these checks be performed more frequently? E.g., at each location where
        // the register allocator says there are no live non-variable registers. Perhaps this could
        // be done by using the map maintained by LSRA (operandToLocationInfoMap) to mark a node
        // somehow when, after the execution of that node, there will be no live non-variable registers.

        regSet.rsSpillChk();

        /* Make sure we didn't bungle pointer register tracking */

        regMaskTP ptrRegs       = gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur;
        regMaskTP nonVarPtrRegs = ptrRegs & ~regSet.GetMaskVars();

        // If return is a GC-type, clear it.  Note that if a common
        // epilog is generated (genReturnBB) it has a void return
        // even though we might return a ref.  We can't use the compRetType
        // as the determiner because something we are tracking as a byref
        // might be used as a return value of a int function (which is legal)
        GenTree* blockLastNode = block->lastNode();
        if ((blockLastNode != nullptr) && (blockLastNode->gtOper == GT_RETURN) &&
            (varTypeIsGC(compiler->info.compRetType) ||
             (blockLastNode->AsOp()->gtOp1 != nullptr && varTypeIsGC(blockLastNode->AsOp()->gtOp1->TypeGet()))))
        {
            nonVarPtrRegs &= ~RBM_INTRET;
        }

        if (nonVarPtrRegs)
        {
            printf("Regset after " FMT_BB " gcr=", block->bbNum);
            printRegMaskInt(gcInfo.gcRegGCrefSetCur & ~regSet.GetMaskVars());
            compiler->GetEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur & ~regSet.GetMaskVars());
            printf(", byr=");
            printRegMaskInt(gcInfo.gcRegByrefSetCur & ~regSet.GetMaskVars());
            compiler->GetEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur & ~regSet.GetMaskVars());
            printf(", regVars=");
            printRegMaskInt(regSet.GetMaskVars());
            compiler->GetEmitter()->emitDispRegSet(regSet.GetMaskVars());
            printf("\n");
        }

        noway_assert(nonVarPtrRegs == RBM_NONE);
#endif // DEBUG

#if defined(DEBUG)
        if (block->bbNext == nullptr)
        {
// Unit testing of the emitter: generate a bunch of instructions into the last block
// (it's as good as any, but better than the prologue, which can only be a single instruction
// group) then use COMPlus_JitLateDisasm=* to see if the late disassembler
// thinks the instructions are the same as we do.
#if defined(TARGET_AMD64) && defined(LATE_DISASM)
            genAmd64EmitterUnitTests();
#elif defined(TARGET_ARM64)
            genArm64EmitterUnitTests();
#endif // TARGET_ARM64
        }
#endif // defined(DEBUG)

        // It is possible to reach the end of the block without generating code for the current IL offset.
        // For example, if the following IR ends the current block, no code will have been generated for
        // offset 21:
        //
        //          (  0,  0) [000040] ------------                il_offset void   IL offset: 21
        //
        //     N001 (  0,  0) [000039] ------------                nop       void
        //
        // This can lead to problems when debugging the generated code. To prevent these issues, make sure
        // we've generated code for the last IL offset we saw in the block.
        genEnsureCodeEmitted(currentDI);

        /* Is this the last block, and are there any open scopes left ? */

        bool isLastBlockProcessed = (block->bbNext == nullptr);
        if (block->isBBCallAlwaysPair())
        {
            isLastBlockProcessed = (block->bbNext->bbNext == nullptr);
        }

#ifdef USING_VARIABLE_LIVE_RANGE
        if (compiler->opts.compDbgInfo && isLastBlockProcessed)
        {
            varLiveKeeper->siEndAllVariableLiveRange(compiler->compCurLife);
        }
#endif // USING_VARIABLE_LIVE_RANGE

        if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
        {
            siEndBlock(block);

#ifdef USING_SCOPE_INFO
            if (isLastBlockProcessed && siOpenScopeList.scNext)
            {
                /* This assert no longer holds, because we may insert a throw
                   block to demarcate the end of a try or finally region when they
                   are at the end of the method.  It would be nice if we could fix
                   our code so that this throw block will no longer be necessary. */

                // noway_assert(block->bbCodeOffsEnd != compiler->info.compILCodeSize);

                siCloseAllOpenScopes();
            }
#endif // USING_SCOPE_INFO
        }

        SubtractStackLevel(savedStkLvl);

#ifdef DEBUG
        // compCurLife should be equal to the liveOut set, except that we don't keep
        // it up to date for vars that are not register candidates
        // (it would be nice to have a xor set function)

        VARSET_TP mismatchLiveVars(VarSetOps::Diff(compiler, block->bbLiveOut, compiler->compCurLife));
        VarSetOps::UnionD(compiler, mismatchLiveVars,
                          VarSetOps::Diff(compiler, compiler->compCurLife, block->bbLiveOut));
        VarSetOps::Iter mismatchLiveVarIter(compiler, mismatchLiveVars);
        unsigned        mismatchLiveVarIndex  = 0;
        bool            foundMismatchedRegVar = false;
        while (mismatchLiveVarIter.NextElem(&mismatchLiveVarIndex))
        {
            LclVarDsc* varDsc = compiler->lvaGetDescByTrackedIndex(mismatchLiveVarIndex);
            if (varDsc->lvIsRegCandidate())
            {
                if (!foundMismatchedRegVar)
                {
                    JITDUMP("Mismatched live reg vars after " FMT_BB ":", block->bbNum);
                    foundMismatchedRegVar = true;
                }
                JITDUMP(" V%02u", compiler->lvaTrackedIndexToLclNum(mismatchLiveVarIndex));
            }
        }
        if (foundMismatchedRegVar)
        {
            JITDUMP("\n");
            assert(!"Found mismatched live reg var(s) after block");
        }
#endif

        /* Both stacks should always be empty on exit from a basic block */
        noway_assert(genStackLevel == 0);

#ifdef TARGET_AMD64
        // On AMD64, we need to generate a NOP after a call that is the last instruction of the block, in several
        // situations, to support proper exception handling semantics. This is mostly to ensure that when the stack
        // walker computes an instruction pointer for a frame, that instruction pointer is in the correct EH region.
        // The document "X64 and ARM ABIs.docx" has more details. The situations:
        // 1. If the call instruction is in a different EH region as the instruction that follows it.
        // 2. If the call immediately precedes an OS epilog. (Note that what the JIT or VM consider an epilog might
        //    be slightly different from what the OS considers an epilog, and it is the OS-reported epilog that matters
        //    here.)
        // We handle case #1 here, and case #2 in the emitter.
        if (GetEmitter()->emitIsLastInsCall())
        {
            // Ok, the last instruction generated is a call instruction. Do any of the other conditions hold?
            // Note: we may be generating a few too many NOPs for the case of call preceding an epilog. Technically,
            // if the next block is a BBJ_RETURN, an epilog will be generated, but there may be some instructions
            // generated before the OS epilog starts, such as a GS cookie check.
            if ((block->bbNext == nullptr) || !BasicBlock::sameEHRegion(block, block->bbNext))
            {
                // We only need the NOP if we're not going to generate any more code as part of the block end.

                switch (block->bbJumpKind)
                {
                    case BBJ_ALWAYS:
                    case BBJ_THROW:
                    case BBJ_CALLFINALLY:
                    case BBJ_EHCATCHRET:
                    // We're going to generate more code below anyway, so no need for the NOP.

                    case BBJ_RETURN:
                    case BBJ_EHFINALLYRET:
                    case BBJ_EHFILTERRET:
                        // These are the "epilog follows" case, handled in the emitter.

                        break;

                    case BBJ_NONE:
                        if (block->bbNext == nullptr)
                        {
                            // Call immediately before the end of the code; we should never get here    .
                            instGen(INS_BREAKPOINT); // This should never get executed
                        }
                        else
                        {
                            // We need the NOP
                            instGen(INS_nop);
                        }
                        break;

                    case BBJ_COND:
                    case BBJ_SWITCH:
                    // These can't have a call as the last instruction!

                    default:
                        noway_assert(!"Unexpected bbJumpKind");
                        break;
                }
            }
        }
#endif // TARGET_AMD64

        /* Do we need to generate a jump or return? */

        switch (block->bbJumpKind)
        {
            case BBJ_RETURN:
                genExitCode(block);
                break;

            case BBJ_THROW:
                // If we have a throw at the end of a function or funclet, we need to emit another instruction
                // afterwards to help the OS unwinder determine the correct context during unwind.
                // We insert an unexecuted breakpoint instruction in several situations
                // following a throw instruction:
                // 1. If the throw is the last instruction of the function or funclet. This helps
                //    the OS unwinder determine the correct context during an unwind from the
                //    thrown exception.
                // 2. If this is this is the last block of the hot section.
                // 3. If the subsequent block is a special throw block.
                // 4. On AMD64, if the next block is in a different EH region.
                if ((block->bbNext == nullptr) || (block->bbNext->bbFlags & BBF_FUNCLET_BEG) ||
                    !BasicBlock::sameEHRegion(block, block->bbNext) ||
                    (!isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block->bbNext)) ||
                    block->bbNext == compiler->fgFirstColdBlock)
                {
                    instGen(INS_BREAKPOINT); // This should never get executed
                }
                // Do likewise for blocks that end in DOES_NOT_RETURN calls
                // that were not caught by the above rules. This ensures that
                // gc register liveness doesn't change across call instructions
                // in fully-interruptible mode.
                else
                {
                    GenTree* call = block->lastNode();

                    if ((call != nullptr) && (call->gtOper == GT_CALL))
                    {
                        if ((call->AsCall()->gtCallMoreFlags & GTF_CALL_M_DOES_NOT_RETURN) != 0)
                        {
                            instGen(INS_BREAKPOINT); // This should never get executed
                        }
                    }
                }

                break;

            case BBJ_CALLFINALLY:
                block = genCallFinally(block);
                break;

#if defined(FEATURE_EH_FUNCLETS)

            case BBJ_EHCATCHRET:
                genEHCatchRet(block);
                FALLTHROUGH;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genReserveFuncletEpilog(block);
                break;

#else // !FEATURE_EH_FUNCLETS

            case BBJ_EHCATCHRET:
                noway_assert(!"Unexpected BBJ_EHCATCHRET"); // not used on x86
                break;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genEHFinallyOrFilterRet(block);
                break;

#endif // !FEATURE_EH_FUNCLETS

            case BBJ_NONE:
            case BBJ_SWITCH:
                break;

            case BBJ_ALWAYS:
                inst_JMP(EJ_jmp, block->bbJumpDest);
                FALLTHROUGH;

            case BBJ_COND:

#if FEATURE_LOOP_ALIGN
                // This is the last place where we operate on blocks and after this, we operate
                // on IG. Hence, if we know that the destination of "block" is the first block
                // of a loop and needs alignment (it has BBF_LOOP_ALIGN), then "block" represents
                // end of the loop. Propagate that information on the IG through "igLoopBackEdge".
                //
                // During emitter, this information will be used to calculate the loop size.
                // Depending on the loop size, decision of whether to align a loop or not will be taken.
                //
                // In the emitter, we need to calculate the loop size from `block->bbJumpDest` through
                // `block` (inclusive). Thus, we need to ensure there is a label on the lexical fall-through
                // block, even if one is not otherwise needed, to be able to calculate the size of this
                // loop (loop size is calculated by walking the instruction groups; see emitter::getLoopSize()).

                if (block->bbJumpDest->isLoopAlign())
                {
                    GetEmitter()->emitSetLoopBackEdge(block->bbJumpDest);

                    if (block->bbNext != nullptr)
                    {
                        JITDUMP("Mark " FMT_BB " as label: alignment end-of-loop\n", block->bbNext->bbNum);
                        block->bbNext->bbFlags |= BBF_HAS_LABEL;
                    }
                }
#endif // FEATURE_LOOP_ALIGN

                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

#if FEATURE_LOOP_ALIGN
        if (block->hasAlign())
        {
            // If this block has 'align' instruction in the end (identified by BBF_HAS_ALIGN),
            // then need to add align instruction in the current "block".
            //
            // For non-adaptive alignment, add alignment instruction of size depending on the
            // compJitAlignLoopBoundary.
            // For adaptive alignment, alignment instruction will always be of 15 bytes for xarch
            // and 16 bytes for arm64.
            assert(ShouldAlignLoops());

            GetEmitter()->emitLoopAlignment(DEBUG_ARG1(block->bbJumpKind == BBJ_ALWAYS));
        }

        if ((block->bbNext != nullptr) && (block->bbNext->isLoopAlign()))
        {
            if (compiler->opts.compJitHideAlignBehindJmp)
            {
                // The current IG is the one that is just before the IG having loop start.
                // Establish a connection of recent align instruction emitted to the loop
                // it actually is aligning using 'idaLoopHeadPredIG'.
                GetEmitter()->emitConnectAlignInstrWithCurIG();
            }
        }
#endif

#if defined(DEBUG) && defined(USING_VARIABLE_LIVE_RANGE)
        if (compiler->verbose)
        {
            varLiveKeeper->dumpBlockVariableLiveRanges(block);
        }
#endif // defined(DEBUG) && defined(USING_VARIABLE_LIVE_RANGE)

        INDEBUG(compiler->compCurBB = nullptr);

    } //------------------ END-FOR each block of the method -------------------

    // There could be variables alive at this point. For example see lvaKeepAliveAndReportThis.
    // This call is for cleaning the GC refs
    genUpdateLife(VarSetOps::MakeEmpty(compiler));

    /* Finalize the spill  tracking logic */

    regSet.rsSpillEnd();

    /* Finalize the temp   tracking logic */

    regSet.tmpEnd();

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\n# ");
        printf("compCycleEstimate = %6d, compSizeEstimate = %5d ", compiler->compCycleEstimate,
               compiler->compSizeEstimate);
        printf("%s\n", compiler->info.compFullName);
    }
#endif
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                         Register Management                               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

//------------------------------------------------------------------------
// genSpillVar: Spill a local variable
//
// Arguments:
//    tree      - the lclVar node for the variable being spilled
//
// Return Value:
//    None.
//
// Assumptions:
//    The lclVar must be a register candidate (lvRegCandidate)

void CodeGen::genSpillVar(GenTree* tree)
{
    unsigned   varNum = tree->AsLclVarCommon()->GetLclNum();
    LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

    assert(varDsc->lvIsRegCandidate());

    // We don't actually need to spill if it is already living in memory
    bool needsSpill = ((tree->gtFlags & GTF_VAR_DEF) == 0 && varDsc->lvIsInReg());
    if (needsSpill)
    {
        // In order for a lclVar to have been allocated to a register, it must not have been aliasable, and can
        // therefore be store-normalized (rather than load-normalized). In fact, not performing store normalization
        // can lead to problems on architectures where a lclVar may be allocated to a register that is not
        // addressable at the granularity of the lclVar's defined type (e.g. x86).
        var_types lclType = varDsc->GetStackSlotHomeType();
        emitAttr  size    = emitTypeSize(lclType);

        // If this is a write-thru or a single-def variable, we don't actually spill at a use,
        // but we will kill the var in the reg (below).
        if (!varDsc->IsAlwaysAliveInMemory())
        {
            instruction storeIns = ins_Store(lclType, compiler->isSIMDTypeLocalAligned(varNum));
            assert(varDsc->GetRegNum() == tree->GetRegNum());
            inst_TT_RV(storeIns, size, tree, tree->GetRegNum());
        }

        // We should only have both GTF_SPILL (i.e. the flag causing this method to be called) and
        // GTF_SPILLED on a write-thru/single-def def, for which we should not be calling this method.
        assert((tree->gtFlags & GTF_SPILLED) == 0);

        // Remove the live var from the register.
        genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(tree));
        gcInfo.gcMarkRegSetNpt(varDsc->lvRegMask());

        if (VarSetOps::IsMember(compiler, gcInfo.gcTrkStkPtrLcls, varDsc->lvVarIndex))
        {
#ifdef DEBUG
            if (!VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
            {
                JITDUMP("\t\t\t\t\t\t\tVar V%02u becoming live\n", varNum);
            }
            else
            {
                JITDUMP("\t\t\t\t\t\t\tVar V%02u continuing live\n", varNum);
            }
#endif
            VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
        }
    }

    tree->gtFlags &= ~GTF_SPILL;
    // If this is NOT a write-thru, reset the var location.
    if ((tree->gtFlags & GTF_SPILLED) == 0)
    {
        varDsc->SetRegNum(REG_STK);
        if (varTypeIsMultiReg(tree))
        {
            varDsc->SetOtherReg(REG_STK);
        }
    }
    else
    {
        // We only have 'GTF_SPILL' and 'GTF_SPILLED' on a def of a write-thru lclVar
        // or a single-def var that is to be spilled at its definition.
        assert((varDsc->IsAlwaysAliveInMemory()) && ((tree->gtFlags & GTF_VAR_DEF) != 0));
    }

#ifdef USING_VARIABLE_LIVE_RANGE
    if (needsSpill)
    {
        // We need this after "lvRegNum" has change because now we are sure that varDsc->lvIsInReg() is false.
        // "SiVarLoc" constructor uses the "LclVarDsc" of the variable.
        varLiveKeeper->siUpdateVariableLiveRange(varDsc, varNum);
    }
#endif // USING_VARIABLE_LIVE_RANGE
}

//------------------------------------------------------------------------
// genUpdateVarReg: Update the current register location for a multi-reg lclVar
//
// Arguments:
//    varDsc   - the LclVarDsc for the lclVar
//    tree     - the lclVar node
//    regIndex - the index of the register in the node
//
// inline
void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree, int regIndex)
{
    // This should only be called for multireg lclVars.
    assert(compiler->lvaEnregMultiRegVars);
    assert(tree->IsMultiRegLclVar() || (tree->gtOper == GT_COPY));
    varDsc->SetRegNum(tree->GetRegByIndex(regIndex));
}

//------------------------------------------------------------------------
// genUpdateVarReg: Update the current register location for a lclVar
//
// Arguments:
//    varDsc - the LclVarDsc for the lclVar
//    tree   - the lclVar node
//
// inline
void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree)
{
    // This should not be called for multireg lclVars.
    assert((tree->OperIsScalarLocal() && !tree->IsMultiRegLclVar()) || (tree->gtOper == GT_COPY));
    varDsc->SetRegNum(tree->GetRegNum());
}

//------------------------------------------------------------------------
// sameRegAsDst: Return the child that has the same reg as the dst (if any)
//
// Arguments:
//    tree  - the node of interest
//    other - an out parameter to return the other child
//
// Notes:
//    If 'tree' has a child with the same assigned register as its target reg,
//    that child will be returned, and 'other' will contain the non-matching child.
//    Otherwise, both other and the return value will be nullptr.
//
GenTree* sameRegAsDst(GenTree* tree, GenTree*& other /*out*/)
{
    if (tree->GetRegNum() == REG_NA)
    {
        other = nullptr;
        return nullptr;
    }

    GenTree* op1 = tree->AsOp()->gtOp1;
    GenTree* op2 = tree->AsOp()->gtOp2;
    if (op1->GetRegNum() == tree->GetRegNum())
    {
        other = op2;
        return op1;
    }
    if (op2->GetRegNum() == tree->GetRegNum())
    {
        other = op1;
        return op2;
    }
    else
    {
        other = nullptr;
        return nullptr;
    }
}

//------------------------------------------------------------------------
// genUnspillLocal: Reload a register candidate local into a register, if needed.
//
// Arguments:
//     varNum    - The variable number of the local to be reloaded (unspilled).
//                 It may be a local field.
//     type      - The type of the local.
//     lclNode   - The node being unspilled. Note that for a multi-reg local,
//                 the gtLclNum will be that of the parent struct.
//     regNum    - The register that 'varNum' should be loaded to.
//     reSpill   - True if it will be immediately spilled after use.
//     isLastUse - True if this is a last use of 'varNum'.
//
// Notes:
//     The caller must have determined that this local needs to be unspilled.
void CodeGen::genUnspillLocal(
    unsigned varNum, var_types type, GenTreeLclVar* lclNode, regNumber regNum, bool reSpill, bool isLastUse)
{
    LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
    inst_set_SV_var(lclNode);
    instruction ins = ins_Load(type, compiler->isSIMDTypeLocalAligned(varNum));
    GetEmitter()->emitIns_R_S(ins, emitTypeSize(type), regNum, varNum, 0);

    // TODO-Review: We would like to call:
    //      genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(tree));
    // instead of the following code, but this ends up hitting this assert:
    //      assert((regSet.GetMaskVars() & regMask) == 0);
    // due to issues with LSRA resolution moves.
    // So, just force it for now. This probably indicates a condition that creates a GC hole!
    //
    // Extra note: I think we really want to call something like gcInfo.gcUpdateForRegVarMove,
    // because the variable is not really going live or dead, but that method is somewhat poorly
    // factored because it, in turn, updates rsMaskVars which is part of RegSet not GCInfo.
    // TODO-Cleanup: This code exists in other CodeGen*.cpp files, and should be moved to CodeGenCommon.cpp.

    // Don't update the variable's location if we are just re-spilling it again.

    if (!reSpill)
    {
        varDsc->SetRegNum(regNum);

#ifdef USING_VARIABLE_LIVE_RANGE
        // We want "VariableLiveRange" inclusive on the beginning and exclusive on the ending.
        // For that we shouldn't report an update of the variable location if is becoming dead
        // on the same native offset.
        if (!isLastUse)
        {
            // Report the home change for this variable
            varLiveKeeper->siUpdateVariableLiveRange(varDsc, varNum);
        }
#endif // USING_VARIABLE_LIVE_RANGE

        if (!varDsc->IsAlwaysAliveInMemory())
        {
#ifdef DEBUG
            if (VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
            {
                JITDUMP("\t\t\t\t\t\t\tRemoving V%02u from gcVarPtrSetCur\n", varNum);
            }
#endif // DEBUG
            VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
        }

#ifdef DEBUG
        if (compiler->verbose)
        {
            printf("\t\t\t\t\t\t\tV%02u in reg ", varNum);
            varDsc->PrintVarReg();
            printf(" is becoming live  ");
            compiler->printTreeID(lclNode);
            printf("\n");
        }
#endif // DEBUG

        regSet.AddMaskVars(genGetRegMask(varDsc));
    }

    gcInfo.gcMarkRegPtrVal(regNum, type);
}

//------------------------------------------------------------------------
// genUnspillRegIfNeeded: Reload a MultiReg source value into a register, if needed
//
// Arguments:
//    tree          - the MultiReg node of interest.
//    multiRegIndex - the index of the value to reload, if needed.
//
// Notes:
//    It must *not* be a GT_LCL_VAR (those are handled separately).
//    In the normal case, the value will be reloaded into the register it
//    was originally computed into. However, if that register is not available,
//    the register allocator will have allocated a different register, and
//    inserted a GT_RELOAD to indicate the register into which it should be
//    reloaded.
//
void CodeGen::genUnspillRegIfNeeded(GenTree* tree, unsigned multiRegIndex)
{
    GenTree* unspillTree = tree;
    assert(unspillTree->IsMultiRegNode());

    if (tree->gtOper == GT_RELOAD)
    {
        unspillTree = tree->AsOp()->gtOp1;
    }

    // In case of multi-reg node, GTF_SPILLED flag on it indicates that
    // one or more of its result regs are spilled.  Individual spill flags need to be
    // queried to determine which specific result regs need to be unspilled.
    if ((unspillTree->gtFlags & GTF_SPILLED) == 0)
    {
        return;
    }
    GenTreeFlags spillFlags = unspillTree->GetRegSpillFlagByIdx(multiRegIndex);
    if ((spillFlags & GTF_SPILLED) == 0)
    {
        return;
    }

    regNumber dstReg = tree->GetRegByIndex(multiRegIndex);
    if (dstReg == REG_NA)
    {
        assert(tree->IsCopyOrReload());
        dstReg = unspillTree->GetRegByIndex(multiRegIndex);
    }
    if (tree->IsMultiRegLclVar())
    {
        GenTreeLclVar* lclNode     = tree->AsLclVar();
        unsigned       fieldVarNum = compiler->lvaGetDesc(lclNode)->lvFieldLclStart + multiRegIndex;
        bool           reSpill     = ((spillFlags & GTF_SPILL) != 0);
        bool           isLastUse   = lclNode->IsLastUse(multiRegIndex);
        genUnspillLocal(fieldVarNum, compiler->lvaGetDesc(fieldVarNum)->TypeGet(), lclNode, dstReg, reSpill, isLastUse);
    }
    else
    {
        var_types dstType        = unspillTree->GetRegTypeByIndex(multiRegIndex);
        regNumber unspillTreeReg = unspillTree->GetRegByIndex(multiRegIndex);
        TempDsc*  t              = regSet.rsUnspillInPlace(unspillTree, unspillTreeReg, multiRegIndex);
        emitAttr  emitType       = emitActualTypeSize(dstType);
        GetEmitter()->emitIns_R_S(ins_Load(dstType), emitType, dstReg, t->tdTempNum(), 0);
        regSet.tmpRlsTemp(t);
        gcInfo.gcMarkRegPtrVal(dstReg, dstType);
    }
}

//------------------------------------------------------------------------
// genUnspillRegIfNeeded: Reload the value into a register, if needed
//
// Arguments:
//    tree - the node of interest.
//
// Notes:
//    In the normal case, the value will be reloaded into the register it
//    was originally computed into. However, if that register is not available,
//    the register allocator will have allocated a different register, and
//    inserted a GT_RELOAD to indicate the register into which it should be
//    reloaded.
//
//    A GT_RELOAD never has a reg candidate lclVar or multi-reg lclVar as its child.
//    This is because register candidates locals always have distinct tree nodes
//    for uses and definitions. (This is unlike non-register candidate locals which
//    may be "defined" by a GT_LCL_VAR node that loads it into a register. It may
//    then have a GT_RELOAD inserted if it needs a different register, though this
//    is unlikely to happen except in stress modes.)
//
void CodeGen::genUnspillRegIfNeeded(GenTree* tree)
{
    GenTree* unspillTree = tree;
    if (tree->gtOper == GT_RELOAD)
    {
        unspillTree = tree->AsOp()->gtOp1;
    }

    if ((unspillTree->gtFlags & GTF_SPILLED) != 0)
    {
        if (genIsRegCandidateLocal(unspillTree))
        {
            // We never have a GT_RELOAD for this case.
            assert(tree == unspillTree);

            // Reset spilled flag, since we are going to load a local variable from its home location.
            unspillTree->gtFlags &= ~GTF_SPILLED;

            GenTreeLclVar* lcl       = unspillTree->AsLclVar();
            LclVarDsc*     varDsc    = compiler->lvaGetDesc(lcl);
            var_types      spillType = varDsc->GetRegisterType(lcl);
            assert(spillType != TYP_UNDEF);

// TODO-Cleanup: The following code could probably be further merged and cleaned up.
#if defined(TARGET_XARCH) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
            // Load local variable from its home location.
            // Never allow truncating the locals here, otherwise a subsequent
            // use of the local with a wider type would see the truncated
            // value. We do allow wider loads as those can be efficient even
            // when unaligned and might be smaller encoding wise (on xarch).
            var_types lclLoadType = varDsc->lvNormalizeOnLoad() ? varDsc->TypeGet() : varDsc->GetStackSlotHomeType();
            assert(lclLoadType != TYP_UNDEF);
            if (genTypeSize(spillType) < genTypeSize(lclLoadType))
            {
                spillType = lclLoadType;
            }

#if defined(TARGET_LOONGARCH64)
            if (varTypeIsFloating(spillType) && emitter::isGeneralRegister(tree->GetRegNum()))
            {
                spillType = spillType == TYP_FLOAT ? TYP_INT : TYP_LONG;
            }
#endif
#elif defined(TARGET_ARM)
// No normalizing for ARM
#else
            NYI("Unspilling not implemented for this target architecture.");
#endif
            bool reSpill   = ((unspillTree->gtFlags & GTF_SPILL) != 0);
            bool isLastUse = lcl->IsLastUse(0);
            genUnspillLocal(lcl->GetLclNum(), spillType, lcl->AsLclVar(), tree->GetRegNum(), reSpill, isLastUse);
        }
        else if (unspillTree->IsMultiRegLclVar())
        {
            // We never have a GT_RELOAD for this case.
            assert(tree == unspillTree);

            GenTreeLclVar* lclNode  = unspillTree->AsLclVar();
            LclVarDsc*     varDsc   = compiler->lvaGetDesc(lclNode);
            unsigned       regCount = varDsc->lvFieldCnt;

            for (unsigned i = 0; i < regCount; ++i)
            {
                GenTreeFlags spillFlags = lclNode->GetRegSpillFlagByIdx(i);
                if ((spillFlags & GTF_SPILLED) != 0)
                {
                    regNumber reg         = lclNode->GetRegNumByIdx(i);
                    unsigned  fieldVarNum = varDsc->lvFieldLclStart + i;
                    bool      reSpill     = ((spillFlags & GTF_SPILL) != 0);
                    bool      isLastUse   = lclNode->IsLastUse(i);
                    genUnspillLocal(fieldVarNum, compiler->lvaGetDesc(fieldVarNum)->TypeGet(), lclNode, reg, reSpill,
                                    isLastUse);
                }
            }
        }
        else if (unspillTree->IsMultiRegNode())
        {
            // Here we may have a GT_RELOAD, and we will need to use that node ('tree') to
            // do the unspilling if needed. However, that tree doesn't have the register
            // count, so we use 'unspillTree' for that.
            unsigned regCount = unspillTree->GetMultiRegCount(compiler);
            for (unsigned i = 0; i < regCount; ++i)
            {
                genUnspillRegIfNeeded(tree, i);
            }
            unspillTree->gtFlags &= ~GTF_SPILLED;
        }
        else
        {
            // Here we may have a GT_RELOAD.
            // The spill temp allocated for it is associated with the original tree that defined the
            // register that it was spilled from.
            // So we use 'unspillTree' to recover that spill temp.
            TempDsc* t        = regSet.rsUnspillInPlace(unspillTree, unspillTree->GetRegNum());
            emitAttr emitType = emitActualTypeSize(unspillTree->TypeGet());
            // Reload into the register specified by 'tree' which may be a GT_RELOAD.
            regNumber dstReg = tree->GetRegNum();
            GetEmitter()->emitIns_R_S(ins_Load(unspillTree->gtType), emitType, dstReg, t->tdTempNum(), 0);
            regSet.tmpRlsTemp(t);

            unspillTree->gtFlags &= ~GTF_SPILLED;
            gcInfo.gcMarkRegPtrVal(dstReg, unspillTree->TypeGet());
        }
    }
}

//------------------------------------------------------------------------
// genCopyRegIfNeeded: Copy the given node into the specified register
//
// Arguments:
//    node - The node that has been evaluated (consumed).
//    needReg - The register in which its value is needed.
//
// Notes:
//    This must be a node that has a register.
//
void CodeGen::genCopyRegIfNeeded(GenTree* node, regNumber needReg)
{
    assert((node->GetRegNum() != REG_NA) && (needReg != REG_NA));
    assert(!node->isUsedFromSpillTemp());
    inst_Mov(node->TypeGet(), needReg, node->GetRegNum(), /* canSkip */ true);
}

// Do Liveness update for a subnodes that is being consumed by codegen
// including the logic for reload in case is needed and also takes care
// of locating the value on the desired register.
void CodeGen::genConsumeRegAndCopy(GenTree* node, regNumber needReg)
{
    if (needReg == REG_NA)
    {
        return;
    }
    genConsumeReg(node);
    genCopyRegIfNeeded(node, needReg);
}

// Check that registers are consumed in the right order for the current node being generated.
#ifdef DEBUG
void CodeGen::genNumberOperandUse(GenTree* const operand, int& useNum) const
{
    assert(operand != nullptr);

    // Ignore argument placeholders.
    if (operand->OperGet() == GT_ARGPLACE)
    {
        return;
    }

    assert(operand->gtUseNum == -1);

    if (!operand->isContained() && !operand->IsCopyOrReload())
    {
        operand->gtUseNum = useNum;
        useNum++;
    }
    else
    {
        for (GenTree* op : operand->Operands())
        {
            genNumberOperandUse(op, useNum);
        }
    }
}

void CodeGen::genCheckConsumeNode(GenTree* const node)
{
    assert(node != nullptr);

    if (verbose)
    {
        if (node->gtUseNum == -1)
        {
            // nothing wrong if the node was not consumed
        }
        else if ((node->gtDebugFlags & GTF_DEBUG_NODE_CG_CONSUMED) != 0)
        {
            printf("Node was consumed twice:\n");
            compiler->gtDispTree(node, nullptr, nullptr, true);
        }
        else if ((lastConsumedNode != nullptr) && (node->gtUseNum < lastConsumedNode->gtUseNum))
        {
            printf("Nodes were consumed out-of-order:\n");
            compiler->gtDispTree(lastConsumedNode, nullptr, nullptr, true);
            compiler->gtDispTree(node, nullptr, nullptr, true);
        }
    }

    assert((node->OperGet() == GT_CATCH_ARG) || ((node->gtDebugFlags & GTF_DEBUG_NODE_CG_CONSUMED) == 0));
    assert((lastConsumedNode == nullptr) || (node->gtUseNum == -1) || (node->gtUseNum > lastConsumedNode->gtUseNum));

    node->gtDebugFlags |= GTF_DEBUG_NODE_CG_CONSUMED;
    lastConsumedNode = node;
}
#endif // DEBUG

//--------------------------------------------------------------------
// genConsumeReg: Do liveness update for a single register of a multireg child node
//                that is being consumed by codegen.
//
// Arguments:
//    tree          - GenTree node
//    multiRegIndex - The index of the register to be consumed
//
// Return Value:
//    Returns the reg number for the given multiRegIndex.
//
regNumber CodeGen::genConsumeReg(GenTree* tree, unsigned multiRegIndex)
{
    regNumber reg = tree->GetRegByIndex(multiRegIndex);
    if (tree->OperIs(GT_COPY))
    {
        reg = genRegCopy(tree, multiRegIndex);
    }
    else if (reg == REG_NA)
    {
        assert(tree->OperIs(GT_RELOAD));
        reg = tree->gtGetOp1()->GetRegByIndex(multiRegIndex);
        assert(reg != REG_NA);
    }
    genUnspillRegIfNeeded(tree, multiRegIndex);

    // UpdateLifeFieldVar() will return true if local var should be spilled.
    if (tree->IsMultiRegLclVar() && treeLifeUpdater->UpdateLifeFieldVar(tree->AsLclVar(), multiRegIndex))
    {
        GenTreeLclVar* lcl = tree->AsLclVar();
        genSpillLocal(lcl->GetLclNum(), lcl->GetFieldTypeByIndex(compiler, multiRegIndex), lcl,
                      lcl->GetRegByIndex(multiRegIndex));
    }

    if (tree->gtSkipReloadOrCopy()->OperIs(GT_LCL_VAR))
    {
        assert(compiler->lvaEnregMultiRegVars);

        GenTreeLclVar* lcl = tree->gtSkipReloadOrCopy()->AsLclVar();
        assert(lcl->IsMultiReg());

        LclVarDsc* varDsc = compiler->lvaGetDesc(lcl);
        assert(varDsc->lvPromoted);
        assert(multiRegIndex < varDsc->lvFieldCnt);
        unsigned   fieldVarNum = varDsc->lvFieldLclStart + multiRegIndex;
        LclVarDsc* fldVarDsc   = compiler->lvaGetDesc(fieldVarNum);
        assert(fldVarDsc->lvLRACandidate);

        if (fldVarDsc->GetRegNum() == REG_STK)
        {
            // We have loaded this into a register only temporarily
            gcInfo.gcMarkRegSetNpt(genRegMask(reg));
        }
        else if (lcl->IsLastUse(multiRegIndex))
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(fldVarDsc->GetRegNum()));
        }
    }
    else
    {
        gcInfo.gcMarkRegSetNpt(tree->gtGetRegMask());
    }
    return reg;
}

//--------------------------------------------------------------------
// genConsumeReg: Do liveness update for a subnode that is being
// consumed by codegen.
//
// Arguments:
//    tree - GenTree node
//
// Return Value:
//    Returns the reg number of tree.
//    In case of multi-reg call node returns the first reg number
//    of the multi-reg return.
//
regNumber CodeGen::genConsumeReg(GenTree* tree)
{
    if (tree->OperGet() == GT_COPY)
    {
        genRegCopy(tree);
    }

    // Handle the case where we have a lclVar that needs to be copied before use (i.e. because it
    // interferes with one of the other sources (or the target, if it's a "delayed use" register)).
    // TODO-Cleanup: This is a special copyReg case in LSRA - consider eliminating these and
    // always using GT_COPY to make the lclVar location explicit.
    // Note that we have to do this before calling genUpdateLife because otherwise if we spill it
    // the lvRegNum will be set to REG_STK and we will lose track of what register currently holds
    // the lclVar (normally when a lclVar is spilled it is then used from its former register
    // location, which matches the GetRegNum() on the node).
    // (Note that it doesn't matter if we call this before or after genUnspillRegIfNeeded
    // because if it's on the stack it will always get reloaded into tree->GetRegNum()).
    if (genIsRegCandidateLocal(tree))
    {
        GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
        LclVarDsc*           varDsc = compiler->lvaGetDesc(lcl);
        if (varDsc->GetRegNum() != REG_STK)
        {
            var_types regType = varDsc->GetRegisterType(lcl);
            inst_Mov(regType, tree->GetRegNum(), varDsc->GetRegNum(), /* canSkip */ true);
        }
    }

    genUnspillRegIfNeeded(tree);

    // genUpdateLife() will also spill local var if marked as GTF_SPILL by calling CodeGen::genSpillVar
    genUpdateLife(tree);

    // there are three cases where consuming a reg means clearing the bit in the live mask
    // 1. it was not produced by a local
    // 2. it was produced by a local that is going dead
    // 3. it was produced by a local that does not live in that reg (like one allocated on the stack)

    if (genIsRegCandidateLocal(tree))
    {
        assert(tree->gtHasReg(compiler));

        GenTreeLclVarCommon* lcl    = tree->AsLclVar();
        LclVarDsc*           varDsc = compiler->lvaGetDesc(lcl);
        assert(varDsc->lvLRACandidate);

        if (varDsc->GetRegNum() == REG_STK)
        {
            // We have loaded this into a register only temporarily
            gcInfo.gcMarkRegSetNpt(genRegMask(tree->GetRegNum()));
        }
        else if ((tree->gtFlags & GTF_VAR_DEATH) != 0)
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(varDsc->GetRegNum()));
        }
    }
    else if (tree->gtSkipReloadOrCopy()->IsMultiRegLclVar())
    {
        assert(compiler->lvaEnregMultiRegVars);
        GenTreeLclVar* lcl              = tree->gtSkipReloadOrCopy()->AsLclVar();
        LclVarDsc*     varDsc           = compiler->lvaGetDesc(lcl);
        unsigned       firstFieldVarNum = varDsc->lvFieldLclStart;
        for (unsigned i = 0; i < varDsc->lvFieldCnt; ++i)
        {
            LclVarDsc* fldVarDsc = compiler->lvaGetDesc(firstFieldVarNum + i);
            assert(fldVarDsc->lvLRACandidate);
            regNumber reg;
            if (tree->OperIs(GT_COPY, GT_RELOAD) && (tree->AsCopyOrReload()->GetRegByIndex(i) != REG_NA))
            {
                reg = tree->AsCopyOrReload()->GetRegByIndex(i);
            }
            else
            {
                reg = lcl->AsLclVar()->GetRegNumByIdx(i);
            }

            if (fldVarDsc->GetRegNum() == REG_STK)
            {
                // We have loaded this into a register only temporarily
                gcInfo.gcMarkRegSetNpt(genRegMask(reg));
            }
            else if (lcl->IsLastUse(i))
            {
                gcInfo.gcMarkRegSetNpt(genRegMask(fldVarDsc->GetRegNum()));
            }
        }
    }
    else
    {
        gcInfo.gcMarkRegSetNpt(tree->gtGetRegMask());
    }

    genCheckConsumeNode(tree);
    return tree->GetRegNum();
}

// Do liveness update for an address tree: one of GT_LEA, GT_LCL_VAR, or GT_CNS_INT (for call indirect).
void CodeGen::genConsumeAddress(GenTree* addr)
{
    if (!addr->isContained())
    {
        genConsumeReg(addr);
    }
    else if (addr->OperGet() == GT_LEA)
    {
        genConsumeAddrMode(addr->AsAddrMode());
    }
}

// do liveness update for a subnode that is being consumed by codegen
void CodeGen::genConsumeAddrMode(GenTreeAddrMode* addr)
{
    genConsumeOperands(addr);
}

void CodeGen::genConsumeRegs(GenTree* tree)
{
#if !defined(TARGET_64BIT)
    if (tree->OperGet() == GT_LONG)
    {
        genConsumeRegs(tree->gtGetOp1());
        genConsumeRegs(tree->gtGetOp2());
        return;
    }
#endif // !defined(TARGET_64BIT)

    if (tree->isUsedFromSpillTemp())
    {
        // spill temps are un-tracked and hence no need to update life
    }
    else if (tree->isContained())
    {
        if (tree->OperIsIndir())
        {
            genConsumeAddress(tree->AsIndir()->Addr());
        }
        else if (tree->OperIs(GT_LEA))
        {
            genConsumeAddress(tree);
        }
#ifdef TARGET_ARM64
        else if (tree->OperIs(GT_BFIZ))
        {
            // Can be contained as part of LEA on ARM64
            GenTreeCast* cast = tree->gtGetOp1()->AsCast();
            assert(cast->isContained());
            genConsumeAddress(cast->CastOp());
        }
        else if (tree->OperIs(GT_CAST))
        {
            // Can be contained as part of LEA on ARM64
            GenTreeCast* cast = tree->AsCast();
            assert(cast->isContained());
            genConsumeAddress(cast->CastOp());
        }
#endif
        else if (tree->OperIsLocalRead())
        {
            // A contained lcl var must be living on stack and marked as reg optional, or not be a
            // register candidate.
            unsigned   varNum = tree->AsLclVarCommon()->GetLclNum();
            LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);

            noway_assert(varDsc->GetRegNum() == REG_STK);
            noway_assert(tree->IsRegOptional() || !varDsc->lvLRACandidate);

            // Update the life of the lcl var.
            genUpdateLife(tree);
        }
#ifdef TARGET_XARCH
#ifdef FEATURE_HW_INTRINSICS
        else if (tree->OperIs(GT_HWINTRINSIC))
        {
            // Only load/store HW intrinsics can be contained (and the address may also be contained).
            HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(tree->AsHWIntrinsic()->GetHWIntrinsicId());
            assert((category == HW_Category_MemoryLoad) || (category == HW_Category_MemoryStore));
            size_t numArgs = tree->AsHWIntrinsic()->GetOperandCount();
            genConsumeAddress(tree->AsHWIntrinsic()->Op(1));
            if (category == HW_Category_MemoryStore)
            {
                assert(numArgs == 2);

                GenTree* op2 = tree->AsHWIntrinsic()->Op(2);
                assert(op2->isContained());

                genConsumeReg(op2);
            }
            else
            {
                assert(numArgs == 1);
            }
        }
#endif // FEATURE_HW_INTRINSICS
#endif // TARGET_XARCH
        else if (tree->OperIs(GT_BITCAST, GT_NEG, GT_CAST, GT_LSH))
        {
            genConsumeRegs(tree->gtGetOp1());
        }
        else if (tree->OperIs(GT_MUL))
        {
            genConsumeRegs(tree->gtGetOp1());
            genConsumeRegs(tree->gtGetOp2());
        }
        else
        {
#ifdef FEATURE_SIMD
            // (In)Equality operation that produces bool result, when compared
            // against Vector zero, marks its Vector Zero operand as contained.
            assert(tree->OperIsLeaf() || tree->IsSIMDZero() || tree->IsVectorZero());
#else
            assert(tree->OperIsLeaf());
#endif
        }
    }
    else
    {
        genConsumeReg(tree);
    }
}

//------------------------------------------------------------------------
// genConsumeOperands: Do liveness update for the operands of a unary or binary tree
//
// Arguments:
//    tree - the GenTreeOp whose operands will have their liveness updated.
//
// Return Value:
//    None.
//
void CodeGen::genConsumeOperands(GenTreeOp* tree)
{
    GenTree* firstOp  = tree->gtOp1;
    GenTree* secondOp = tree->gtOp2;

    if (firstOp != nullptr)
    {
        genConsumeRegs(firstOp);
    }
    if (secondOp != nullptr)
    {
        genConsumeRegs(secondOp);
    }
}

#if defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)
//------------------------------------------------------------------------
// genConsumeOperands: Do liveness update for the operands of a multi-operand node,
//                     currently GT_SIMD or GT_HWINTRINSIC
//
// Arguments:
//    tree - the GenTreeMultiOp whose operands will have their liveness updated.
//
// Return Value:
//    None.
//
void CodeGen::genConsumeMultiOpOperands(GenTreeMultiOp* tree)
{
    for (GenTree* operand : tree->Operands())
    {
        genConsumeRegs(operand);
    }
}
#endif // defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)

#if FEATURE_PUT_STRUCT_ARG_STK
//------------------------------------------------------------------------
// genConsumePutStructArgStk: Do liveness update for the operands of a PutArgStk node.
//                      Also loads in the right register the addresses of the
//                      src/dst for rep mov operation.
//
// Arguments:
//    putArgNode - the PUTARG_STK tree.
//    dstReg     - the dstReg for the rep move operation.
//    srcReg     - the srcReg for the rep move operation.
//    sizeReg    - the sizeReg for the rep move operation.
//
// Return Value:
//    None.
//
// Notes:
//    sizeReg can be REG_NA when this function is used to consume the dstReg and srcReg
//    for copying on the stack a struct with references.
//    The source address/offset is determined from the address on the GT_OBJ node, while
//    the destination address is the address contained in 'm_stkArgVarNum' plus the offset
//    provided in the 'putArgNode'.
//    m_stkArgVarNum must be set to  the varnum for the local used for placing the "by-value" args on the stack.

void CodeGen::genConsumePutStructArgStk(GenTreePutArgStk* putArgNode,
                                        regNumber         dstReg,
                                        regNumber         srcReg,
                                        regNumber         sizeReg)
{
    // The putArgNode children are always contained. We should not consume any registers.
    assert(putArgNode->gtGetOp1()->isContained());

    // Get the source address.
    GenTree* src = putArgNode->gtGetOp1();
    assert(varTypeIsStruct(src));
    assert((src->gtOper == GT_OBJ) || ((src->gtOper == GT_IND && varTypeIsSIMD(src))));
    GenTree* srcAddr = src->gtGetOp1();

    assert(dstReg != REG_NA);
    assert(srcReg != REG_NA);

    // Consume the registers only if they are not contained or set to REG_NA.
    if (srcAddr->GetRegNum() != REG_NA)
    {
        genConsumeReg(srcAddr);
    }

    // If the op1 is already in the dstReg - nothing to do.
    // Otherwise load the op1 (GT_ADDR) into the dstReg to copy the struct on the stack by value.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_X86
    assert(dstReg != REG_SPBASE);
    inst_Mov(TYP_I_IMPL, dstReg, REG_SPBASE, /* canSkip */ false);
#else  // !TARGET_X86
    GenTree* dstAddr = putArgNode;
    if (dstAddr->GetRegNum() != dstReg)
    {
        // Generate LEA instruction to load the stack of the outgoing var + SlotNum offset (or the incoming arg area
        // for tail calls) in RDI.
        // Destination is always local (on the stack) - use EA_PTRSIZE.
        assert(m_stkArgVarNum != BAD_VAR_NUM);
        GetEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, dstReg, m_stkArgVarNum, putArgNode->getArgOffset());
    }
#endif // !TARGET_X86

    if (srcAddr->OperIsLocalAddr())
    {
        // The OperLocalAddr is always contained.
        assert(srcAddr->isContained());
        const GenTreeLclVarCommon* lclNode = srcAddr->AsLclVarCommon();

        // Generate LEA instruction to load the LclVar address in RSI.
        // Source is known to be on the stack. Use EA_PTRSIZE.
        unsigned int offset = lclNode->GetLclOffs();
        GetEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, srcReg, lclNode->GetLclNum(), offset);
    }
    else
    {
        assert(srcAddr->GetRegNum() != REG_NA);
        // Source is not known to be on the stack. Use EA_BYREF.
        GetEmitter()->emitIns_Mov(INS_mov, EA_BYREF, srcReg, srcAddr->GetRegNum(), /* canSkip */ true);
    }

    if (sizeReg != REG_NA)
    {
        unsigned size = putArgNode->GetStackByteSize();
        inst_RV_IV(INS_mov, sizeReg, size, EA_PTRSIZE);
    }
}
#endif // FEATURE_PUT_STRUCT_ARG_STK

#if FEATURE_ARG_SPLIT
//------------------------------------------------------------------------
// genConsumeArgRegSplit: Consume register(s) in Call node to set split struct argument.
//
// Arguments:
//    putArgNode - the PUTARG_STK tree.
//
// Return Value:
//    None.
//
void CodeGen::genConsumeArgSplitStruct(GenTreePutArgSplit* putArgNode)
{
    assert(putArgNode->OperGet() == GT_PUTARG_SPLIT);
    assert(putArgNode->gtHasReg(compiler));

    genUnspillRegIfNeeded(putArgNode);

    gcInfo.gcMarkRegSetNpt(putArgNode->gtGetRegMask());

    genCheckConsumeNode(putArgNode);
}
#endif // FEATURE_ARG_SPLIT

//------------------------------------------------------------------------
// genPutArgStkFieldList: Generate code for a putArgStk whose source is a GT_FIELD_LIST
//
// Arguments:
//    putArgStk    - The putArgStk node
//    outArgVarNum - The lclVar num for the argument
//
// Notes:
//    The x86 version of this is in codegenxarch.cpp, and doesn't take an
//    outArgVarNum, as it pushes its args onto the stack.
//
#ifndef TARGET_X86
void CodeGen::genPutArgStkFieldList(GenTreePutArgStk* putArgStk, unsigned outArgVarNum)
{
    assert(putArgStk->gtOp1->OperIs(GT_FIELD_LIST));

    // Evaluate each of the GT_FIELD_LIST items into their register
    // and store their register into the outgoing argument area.
    const unsigned argOffset = putArgStk->getArgOffset();
    for (GenTreeFieldList::Use& use : putArgStk->gtOp1->AsFieldList()->Uses())
    {
        GenTree* nextArgNode = use.GetNode();
        genConsumeReg(nextArgNode);

        regNumber reg             = nextArgNode->GetRegNum();
        var_types type            = use.GetType();
        unsigned  thisFieldOffset = argOffset + use.GetOffset();

// Emit store instructions to store the registers produced by the GT_FIELD_LIST into the outgoing
// argument area.

#if defined(FEATURE_SIMD) && defined(TARGET_ARM64)
        // storing of TYP_SIMD12 (i.e. Vector3) argument.
        if (compMacOsArm64Abi() && (type == TYP_SIMD12))
        {
            // Need an additional integer register to extract upper 4 bytes from data.
            regNumber tmpReg = nextArgNode->GetSingleTempReg();
            GetEmitter()->emitStoreSIMD12ToLclOffset(outArgVarNum, thisFieldOffset, reg, tmpReg);
        }
        else
#endif // FEATURE_SIMD
        {
            emitAttr attr = emitTypeSize(type);
            GetEmitter()->emitIns_S_R(ins_Store(type), attr, reg, outArgVarNum, thisFieldOffset);
        }

// We can't write beyond the arg area unless this is a tail call, in which case we use
// the first stack arg as the base of the incoming arg area.
#ifdef DEBUG
        unsigned areaSize = compiler->lvaLclSize(outArgVarNum);
#if FEATURE_FASTTAILCALL
        if (putArgStk->gtCall->IsFastTailCall())
        {
            areaSize = compiler->info.compArgStackSize;
        }
#endif

        assert((thisFieldOffset + genTypeSize(type)) <= areaSize);
#endif
    }
}
#endif // !TARGET_X86

//------------------------------------------------------------------------
// genSetBlockSize: Ensure that the block size is in the given register
//
// Arguments:
//    blkNode - The block node
//    sizeReg - The register into which the block's size should go
//

void CodeGen::genSetBlockSize(GenTreeBlk* blkNode, regNumber sizeReg)
{
    if (sizeReg != REG_NA)
    {
        unsigned blockSize = blkNode->Size();
        if (!blkNode->OperIs(GT_STORE_DYN_BLK))
        {
            assert((blkNode->gtRsvdRegs & genRegMask(sizeReg)) != 0);
            instGen_Set_Reg_To_Imm(EA_4BYTE, sizeReg, blockSize);
        }
        else
        {
            GenTree* sizeNode = blkNode->AsStoreDynBlk()->gtDynamicSize;
            inst_Mov(sizeNode->TypeGet(), sizeReg, sizeNode->GetRegNum(), /* canSkip */ true);
        }
    }
}

//------------------------------------------------------------------------
// genConsumeBlockSrc: Consume the source address register of a block node, if any.
//
// Arguments:
//    blkNode - The block node

void CodeGen::genConsumeBlockSrc(GenTreeBlk* blkNode)
{
    GenTree* src = blkNode->Data();
    if (blkNode->OperIsCopyBlkOp())
    {
        // For a CopyBlk we need the address of the source.
        assert(src->isContained());
        if (src->OperGet() == GT_IND)
        {
            src = src->AsOp()->gtOp1;
        }
        else
        {
            // This must be a local.
            // For this case, there is no source address register, as it is a
            // stack-based address.
            assert(src->OperIsLocal());
            return;
        }
    }
    else
    {
        if (src->OperIsInitVal())
        {
            src = src->gtGetOp1();
        }
    }
    genConsumeReg(src);
}

//------------------------------------------------------------------------
// genSetBlockSrc: Ensure that the block source is in its allocated register.
//
// Arguments:
//    blkNode - The block node
//    srcReg  - The register in which to set the source (address or init val).
//
void CodeGen::genSetBlockSrc(GenTreeBlk* blkNode, regNumber srcReg)
{
    GenTree* src = blkNode->Data();
    if (blkNode->OperIsCopyBlkOp())
    {
        // For a CopyBlk we need the address of the source.
        if (src->OperGet() == GT_IND)
        {
            src = src->AsOp()->gtOp1;
        }
        else
        {
            // This must be a local struct.
            // Load its address into srcReg.
            unsigned varNum = src->AsLclVarCommon()->GetLclNum();
            unsigned offset = src->AsLclVarCommon()->GetLclOffs();
            GetEmitter()->emitIns_R_S(INS_lea, EA_BYREF, srcReg, varNum, offset);
            return;
        }
    }
    else
    {
        if (src->OperIsInitVal())
        {
            src = src->gtGetOp1();
        }
    }
    genCopyRegIfNeeded(src, srcReg);
}

//------------------------------------------------------------------------
// genConsumeBlockOp: Ensure that the block's operands are enregistered
//                    as needed.
// Arguments:
//    blkNode - The block node
//
// Notes:
//    This ensures that the operands are consumed in the proper order to
//    obey liveness modeling.

void CodeGen::genConsumeBlockOp(GenTreeBlk* blkNode, regNumber dstReg, regNumber srcReg, regNumber sizeReg)
{
    // We have to consume the registers, and perform any copies, in the actual execution order: dst, src, size.
    //
    // Note that the register allocator ensures that the registers ON THE NODES will not interfere
    // with one another if consumed (i.e. reloaded or moved to their ASSIGNED reg) in execution order.
    // Further, it ensures that they will not interfere with one another if they are then copied
    // to the REQUIRED register (if a fixed register requirement) in execution order.  This requires,
    // then, that we first consume all the operands, then do any necessary moves.

    GenTree* const dstAddr = blkNode->Addr();

    // First, consume all the sources in order, and verify that registers have been allocated appropriately,
    // based on the 'gtBlkOpKind'.

    // The destination is always in a register; 'genConsumeReg' asserts that.
    genConsumeReg(dstAddr);
    // The source may be a local or in a register; 'genConsumeBlockSrc' will check that.
    genConsumeBlockSrc(blkNode);
    // 'genSetBlockSize' (called below) will ensure that a register has been reserved as needed
    // in the case where the size is a constant (i.e. it is not GT_STORE_DYN_BLK).
    if (blkNode->OperGet() == GT_STORE_DYN_BLK)
    {
        genConsumeReg(blkNode->AsStoreDynBlk()->gtDynamicSize);
    }

    // Next, perform any necessary moves.
    genCopyRegIfNeeded(dstAddr, dstReg);
    genSetBlockSrc(blkNode, srcReg);
    genSetBlockSize(blkNode, sizeReg);
}

//-------------------------------------------------------------------------
// genSpillLocal: Generate the actual spill of a local var.
//
// Arguments:
//     varNum    - The variable number of the local to be spilled.
//                 It may be a local field.
//     type      - The type of the local.
//     lclNode   - The node being spilled. Note that for a multi-reg local,
//                 the gtLclNum will be that of the parent struct.
//     regNum    - The register that 'varNum' is currently in.
//
// Return Value:
//     None.
//
void CodeGen::genSpillLocal(unsigned varNum, var_types type, GenTreeLclVar* lclNode, regNumber regNum)
{
    const LclVarDsc* varDsc = compiler->lvaGetDesc(varNum);
    assert(!varDsc->lvNormalizeOnStore() || (type == varDsc->GetStackSlotHomeType()));

    // We have a register candidate local that is marked with GTF_SPILL.
    // This flag generally means that we need to spill this local.
    // The exception is the case of a use of an EH/spill-at-single-def var use that is being "spilled"
    // to the stack, indicated by GTF_SPILL (note that all EH lclVar defs are always
    // spilled, i.e. write-thru. Likewise, single-def vars that are spilled at its definitions).
    // An EH or single-def var use is always valid on the stack (so we don't need to actually spill it),
    // but the GTF_SPILL flag records the fact that the register value is going dead.
    if (((lclNode->gtFlags & GTF_VAR_DEF) != 0) || (!varDsc->IsAlwaysAliveInMemory()))
    {
        // Store local variable to its home location.
        // Ensure that lclVar stores are typed correctly.
        GetEmitter()->emitIns_S_R(ins_Store(type, compiler->isSIMDTypeLocalAligned(varNum)), emitTypeSize(type), regNum,
                                  varNum, 0);
    }
}

//-------------------------------------------------------------------------
// genProduceReg: do liveness update for register produced by the current
// node in codegen after code has been emitted for it.
//
// Arguments:
//     tree   -  Gentree node
//
// Return Value:
//     None.
void CodeGen::genProduceReg(GenTree* tree)
{
#ifdef DEBUG
    assert((tree->gtDebugFlags & GTF_DEBUG_NODE_CG_PRODUCED) == 0);
    tree->gtDebugFlags |= GTF_DEBUG_NODE_CG_PRODUCED;
#endif

    if (tree->gtFlags & GTF_SPILL)
    {
        // Code for GT_COPY node gets generated as part of consuming regs by its parent.
        // A GT_COPY node in turn produces reg result and it should never be marked to
        // spill.
        //
        // Similarly GT_RELOAD node gets generated as part of consuming regs by its
        // parent and should never be marked for spilling.
        noway_assert(!tree->IsCopyOrReload());

        if (genIsRegCandidateLocal(tree))
        {
            GenTreeLclVar*   lclNode   = tree->AsLclVar();
            const LclVarDsc* varDsc    = compiler->lvaGetDesc(lclNode);
            const unsigned   varNum    = lclNode->GetLclNum();
            const var_types  spillType = varDsc->GetRegisterType(lclNode);
            genSpillLocal(varNum, spillType, lclNode, tree->GetRegNum());
        }
        else if (tree->IsMultiRegLclVar())
        {
            assert(compiler->lvaEnregMultiRegVars);

            GenTreeLclVar*   lclNode  = tree->AsLclVar();
            const LclVarDsc* varDsc   = compiler->lvaGetDesc(lclNode);
            const unsigned   regCount = lclNode->GetFieldCount(compiler);

            for (unsigned i = 0; i < regCount; ++i)
            {
                GenTreeFlags flags = lclNode->GetRegSpillFlagByIdx(i);
                if ((flags & GTF_SPILL) != 0)
                {
                    const regNumber reg         = lclNode->GetRegNumByIdx(i);
                    const unsigned  fieldVarNum = varDsc->lvFieldLclStart + i;
                    const var_types spillType   = compiler->lvaGetDesc(fieldVarNum)->GetRegisterType();
                    genSpillLocal(fieldVarNum, spillType, lclNode, reg);
                }
            }
        }
        else
        {
            // In case of multi-reg call node, spill flag on call node
            // indicates that one or more of its allocated regs need to
            // be spilled.  Call node needs to be further queried to
            // know which of its result regs needs to be spilled.
            if (tree->IsMultiRegCall())
            {
                GenTreeCall*          call        = tree->AsCall();
                const ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
                const unsigned        regCount    = retTypeDesc->GetReturnRegCount();

                for (unsigned i = 0; i < regCount; ++i)
                {
                    GenTreeFlags flags = call->GetRegSpillFlagByIdx(i);
                    if ((flags & GTF_SPILL) != 0)
                    {
                        regNumber reg = call->GetRegNumByIdx(i);
                        regSet.rsSpillTree(reg, call, i);
                        gcInfo.gcMarkRegSetNpt(genRegMask(reg));
                    }
                }
            }
#if FEATURE_ARG_SPLIT
            else if (tree->OperIsPutArgSplit())
            {
                assert(compFeatureArgSplit());
                GenTreePutArgSplit* argSplit = tree->AsPutArgSplit();
                unsigned            regCount = argSplit->gtNumRegs;

                for (unsigned i = 0; i < regCount; ++i)
                {
                    GenTreeFlags flags = argSplit->GetRegSpillFlagByIdx(i);
                    if ((flags & GTF_SPILL) != 0)
                    {
                        regNumber reg = argSplit->GetRegNumByIdx(i);
                        regSet.rsSpillTree(reg, argSplit, i);
                        gcInfo.gcMarkRegSetNpt(genRegMask(reg));
                    }
                }
            }
#ifdef TARGET_ARM
            else if (compFeatureArgSplit() && tree->OperIsMultiRegOp())
            {
                GenTreeMultiRegOp* multiReg = tree->AsMultiRegOp();
                unsigned           regCount = multiReg->GetRegCount();

                for (unsigned i = 0; i < regCount; ++i)
                {
                    GenTreeFlags flags = multiReg->GetRegSpillFlagByIdx(i);
                    if ((flags & GTF_SPILL) != 0)
                    {
                        regNumber reg = multiReg->GetRegNumByIdx(i);
                        regSet.rsSpillTree(reg, multiReg, i);
                        gcInfo.gcMarkRegSetNpt(genRegMask(reg));
                    }
                }
            }
#endif // TARGET_ARM
#endif // FEATURE_ARG_SPLIT
            else
            {
                regSet.rsSpillTree(tree->GetRegNum(), tree);
                gcInfo.gcMarkRegSetNpt(genRegMask(tree->GetRegNum()));
            }

            tree->gtFlags |= GTF_SPILLED;
            tree->gtFlags &= ~GTF_SPILL;

            return;
        }
    }

    // Updating variable liveness after instruction was emitted
    genUpdateLife(tree);

    // If we've produced a register, mark it as a pointer, as needed.
    if (tree->gtHasReg(compiler))
    {
        // We only mark the register in the following cases:
        // 1. It is not a register candidate local. In this case, we're producing a
        //    register from a local, but the local is not a register candidate. Thus,
        //    we must be loading it as a temp register, and any "last use" flag on
        //    the register wouldn't be relevant.
        // 2. The register candidate local is going dead. There's no point to mark
        //    the register as live, with a GC pointer, if the variable is dead.
        if (!genIsRegCandidateLocal(tree) || ((tree->gtFlags & GTF_VAR_DEATH) == 0))
        {
            // Multi-reg nodes will produce more than one register result.
            // Mark all the regs produced by the node.
            if (tree->IsMultiRegCall())
            {
                const GenTreeCall*    call        = tree->AsCall();
                const ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
                const unsigned        regCount    = retTypeDesc->GetReturnRegCount();

                for (unsigned i = 0; i < regCount; ++i)
                {
                    regNumber reg  = call->GetRegNumByIdx(i);
                    var_types type = retTypeDesc->GetReturnRegType(i);
                    gcInfo.gcMarkRegPtrVal(reg, type);
                }
            }
            else if (tree->IsCopyOrReloadOfMultiRegCall())
            {
                // we should never see reload of multi-reg call here
                // because GT_RELOAD gets generated in reg consuming path.
                noway_assert(tree->OperGet() == GT_COPY);

                // A multi-reg GT_COPY node produces those regs to which
                // copy has taken place.
                const GenTreeCopyOrReload* copy        = tree->AsCopyOrReload();
                const GenTreeCall*         call        = copy->gtGetOp1()->AsCall();
                const ReturnTypeDesc*      retTypeDesc = call->GetReturnTypeDesc();
                const unsigned             regCount    = retTypeDesc->GetReturnRegCount();

                for (unsigned i = 0; i < regCount; ++i)
                {
                    var_types type  = retTypeDesc->GetReturnRegType(i);
                    regNumber toReg = copy->GetRegNumByIdx(i);

                    if (toReg != REG_NA)
                    {
                        gcInfo.gcMarkRegPtrVal(toReg, type);
                    }
                }
            }
            else if (tree->IsMultiRegLclVar())
            {
                assert(compiler->lvaEnregMultiRegVars);
                GenTreeLclVar* lclNode  = tree->AsLclVar();
                LclVarDsc*     varDsc   = compiler->lvaGetDesc(lclNode);
                unsigned       regCount = varDsc->lvFieldCnt;
                for (unsigned i = 0; i < regCount; i++)
                {
                    if (!lclNode->IsLastUse(i))
                    {
                        regNumber reg = lclNode->GetRegByIndex(i);
                        if (reg != REG_NA)
                        {
                            var_types type = compiler->lvaGetDesc(varDsc->lvFieldLclStart + i)->TypeGet();
                            gcInfo.gcMarkRegPtrVal(reg, type);
                        }
                    }
                }
            }
            else
            {
                gcInfo.gcMarkRegPtrVal(tree->GetRegNum(), tree->TypeGet());
            }
        }
    }
}

// transfer gc/byref status of src reg to dst reg
void CodeGen::genTransferRegGCState(regNumber dst, regNumber src)
{
    regMaskTP srcMask = genRegMask(src);
    regMaskTP dstMask = genRegMask(dst);

    if (gcInfo.gcRegGCrefSetCur & srcMask)
    {
        gcInfo.gcMarkRegSetGCref(dstMask);
    }
    else if (gcInfo.gcRegByrefSetCur & srcMask)
    {
        gcInfo.gcMarkRegSetByref(dstMask);
    }
    else
    {
        gcInfo.gcMarkRegSetNpt(dstMask);
    }
}

// generates an ip-relative call or indirect call via reg ('call reg')
//     pass in 'addr' for a relative call or 'base' for a indirect register call
//     methHnd - optional, only used for pretty printing
//     retSize - emitter type of return for GC purposes, should be EA_BYREF, EA_GCREF, or EA_PTRSIZE(not GC)
//
// clang-format off
void CodeGen::genEmitCall(int                   callType,
                          CORINFO_METHOD_HANDLE methHnd,
                          INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo)
                          void*                 addr
                          X86_ARG(int argSize),
                          emitAttr              retSize
                          MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                          const DebugInfo& di,
                          regNumber             base,
                          bool                  isJump)
{
#if !defined(TARGET_X86)
    int argSize = 0;
#endif // !defined(TARGET_X86)

    // This should have been put in volatile registers to ensure it does not
    // get overridden by epilog sequence during tailcall.
    noway_assert(!isJump || (base == REG_NA) || ((RBM_INT_CALLEE_TRASH & genRegMask(base)) != 0));

    GetEmitter()->emitIns_Call(emitter::EmitCallType(callType),
                               methHnd,
                               INDEBUG_LDISASM_COMMA(sigInfo)
                               addr,
                               argSize,
                               retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                               gcInfo.gcVarPtrSetCur,
                               gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur,
                               di, base, REG_NA, 0, 0, isJump);
}
// clang-format on

// generates an indirect call via addressing mode (call []) given an indir node
//     methHnd - optional, only used for pretty printing
//     retSize - emitter type of return for GC purposes, should be EA_BYREF, EA_GCREF, or EA_PTRSIZE(not GC)
//
// clang-format off
void CodeGen::genEmitCallIndir(int                   callType,
                               CORINFO_METHOD_HANDLE methHnd,
                               INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo)
                               GenTreeIndir*         indir
                               X86_ARG(int argSize),
                               emitAttr              retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                               const DebugInfo&      di,
                               bool                  isJump)
{
#if !defined(TARGET_X86)
    int argSize = 0;
#endif // !defined(TARGET_X86)

    regNumber iReg = (indir->Base()  != nullptr) ? indir->Base()->GetRegNum() : REG_NA;
    regNumber xReg = (indir->Index() != nullptr) ? indir->Index()->GetRegNum() : REG_NA;

    // These should have been put in volatile registers to ensure they do not
    // get overridden by epilog sequence during tailcall.
    noway_assert(!isJump || (iReg == REG_NA) || ((RBM_CALLEE_TRASH & genRegMask(iReg)) != 0));
    noway_assert(!isJump || (xReg == REG_NA) || ((RBM_CALLEE_TRASH & genRegMask(xReg)) != 0));

    GetEmitter()->emitIns_Call(emitter::EmitCallType(callType),
                               methHnd,
                               INDEBUG_LDISASM_COMMA(sigInfo)
                               nullptr,
                               argSize,
                               retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                               gcInfo.gcVarPtrSetCur,
                               gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur,
                               di,
                               iReg,
                               xReg,
                               indir->Scale(),
                               indir->Offset(),
                               isJump);
}
// clang-format on

//------------------------------------------------------------------------
// genCodeForCast: Generates the code for GT_CAST.
//
// Arguments:
//    tree - the GT_CAST node.
//
void CodeGen::genCodeForCast(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_CAST));

    var_types targetType = tree->TypeGet();

    if (varTypeIsFloating(targetType) && varTypeIsFloating(tree->gtOp1))
    {
        // Casts float/double <--> double/float
        genFloatToFloatCast(tree);
    }
    else if (varTypeIsFloating(tree->gtOp1))
    {
        // Casts float/double --> int32/int64
        genFloatToIntCast(tree);
    }
    else if (varTypeIsFloating(targetType))
    {
        // Casts int32/uint32/int64/uint64 --> float/double
        genIntToFloatCast(tree);
    }
#ifndef TARGET_64BIT
    else if (varTypeIsLong(tree->gtOp1))
    {
        genLongToIntCast(tree);
    }
#endif // !TARGET_64BIT
    else
    {
        // Casts int <--> int
        genIntToIntCast(tree->AsCast());
    }
    // The per-case functions call genProduceReg()
}

CodeGen::GenIntCastDesc::GenIntCastDesc(GenTreeCast* cast)
{
    const var_types srcType      = genActualType(cast->gtGetOp1()->TypeGet());
    const bool      srcUnsigned  = cast->IsUnsigned();
    const unsigned  srcSize      = genTypeSize(srcType);
    const var_types castType     = cast->gtCastType;
    const bool      castUnsigned = varTypeIsUnsigned(castType);
    const unsigned  castSize     = genTypeSize(castType);
    const var_types dstType      = genActualType(cast->TypeGet());
    const unsigned  dstSize      = genTypeSize(dstType);
    const bool      overflow     = cast->gtOverflow();

    assert((srcSize == 4) || (srcSize == genTypeSize(TYP_I_IMPL)));
    assert((dstSize == 4) || (dstSize == genTypeSize(TYP_I_IMPL)));

    assert(dstSize == genTypeSize(genActualType(castType)));

    if (castSize < 4) // Cast to small int type
    {
        if (overflow)
        {
            m_checkKind    = CHECK_SMALL_INT_RANGE;
            m_checkSrcSize = srcSize;
            // Since these are small int types we can compute the min and max
            // values of the castType without risk of integer overflow.
            const int castNumBits = (castSize * 8) - (castUnsigned ? 0 : 1);
            m_checkSmallIntMax    = (1 << castNumBits) - 1;
            m_checkSmallIntMin    = (castUnsigned | srcUnsigned) ? 0 : (-m_checkSmallIntMax - 1);

            m_extendKind    = COPY;
            m_extendSrcSize = dstSize;
        }
        else
        {
            m_checkKind = CHECK_NONE;

            // Casting to a small type really means widening from that small type to INT/LONG.
            m_extendKind    = castUnsigned ? ZERO_EXTEND_SMALL_INT : SIGN_EXTEND_SMALL_INT;
            m_extendSrcSize = castSize;
        }
    }
#ifdef TARGET_64BIT
    // castType cannot be (U)LONG on 32 bit targets, such casts should have been decomposed.
    // srcType cannot be a small int type since it's the "actual type" of the cast operand.
    // This means that widening casts do not occur on 32 bit targets.
    else if (castSize > srcSize) // (U)INT to (U)LONG widening cast
    {
        assert((srcSize == 4) && (castSize == 8));

        if (overflow && !srcUnsigned && castUnsigned)
        {
            // Widening from INT to ULONG, check if the value is positive
            m_checkKind    = CHECK_POSITIVE;
            m_checkSrcSize = 4;

            // This is the only overflow checking cast that requires changing the
            // source value (by zero extending), all others copy the value as is.
            assert((srcType == TYP_INT) && (castType == TYP_ULONG));
            m_extendKind    = ZERO_EXTEND_INT;
            m_extendSrcSize = 4;
        }
        else
        {
            m_checkKind = CHECK_NONE;

            m_extendKind    = srcUnsigned ? ZERO_EXTEND_INT : SIGN_EXTEND_INT;
            m_extendSrcSize = 4;
        }
    }
    else if (castSize < srcSize) // (U)LONG to (U)INT narrowing cast
    {
        assert((srcSize == 8) && (castSize == 4));

        if (overflow)
        {
            if (castUnsigned) // (U)LONG to UINT cast
            {
                m_checkKind = CHECK_UINT_RANGE;
            }
            else if (srcUnsigned) // ULONG to INT cast
            {
                m_checkKind = CHECK_POSITIVE_INT_RANGE;
            }
            else // LONG to INT cast
            {
                m_checkKind = CHECK_INT_RANGE;
            }

            m_checkSrcSize = 8;
        }
        else
        {
            m_checkKind = CHECK_NONE;
        }

#ifdef TARGET_LOONGARCH64
        // For LoongArch64's ISA which is same with the MIPS64 ISA, even the instructions of 32bits operation need
        // the upper 32bits be sign-extended to 64 bits.
        m_extendKind = SIGN_EXTEND_INT;
#else
        m_extendKind = COPY;
#endif
        m_extendSrcSize = 4;
    }
#endif
    else // if (castSize == srcSize) // Sign changing or same type cast
    {
        assert(castSize == srcSize);

        if (overflow && (srcUnsigned != castUnsigned))
        {
            m_checkKind    = CHECK_POSITIVE;
            m_checkSrcSize = srcSize;
        }
        else
        {
            m_checkKind = CHECK_NONE;
        }

        m_extendKind    = COPY;
        m_extendSrcSize = srcSize;
    }
}

#if !defined(TARGET_64BIT)
//------------------------------------------------------------------------
// genStoreLongLclVar: Generate code to store a non-enregistered long lclVar
//
// Arguments:
//    treeNode - A TYP_LONG lclVar node.
//
// Return Value:
//    None.
//
// Assumptions:
//    'treeNode' must be a TYP_LONG lclVar node for a lclVar that has NOT been promoted.
//    Its operand must be a GT_LONG node.
//
void CodeGen::genStoreLongLclVar(GenTree* treeNode)
{
    emitter* emit = GetEmitter();

    GenTreeLclVarCommon* lclNode = treeNode->AsLclVarCommon();
    unsigned             lclNum  = lclNode->GetLclNum();
    LclVarDsc*           varDsc  = compiler->lvaGetDesc(lclNum);
    assert(varDsc->TypeGet() == TYP_LONG);
    assert(!varDsc->lvPromoted);
    GenTree* op1 = treeNode->AsOp()->gtOp1;

    // A GT_LONG is always contained, so it cannot have RELOAD or COPY inserted between it and its consumer,
    // but a MUL_LONG may.
    noway_assert(op1->OperIs(GT_LONG) || op1->gtSkipReloadOrCopy()->OperIs(GT_MUL_LONG));
    genConsumeRegs(op1);

    if (op1->OperGet() == GT_LONG)
    {
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();

        noway_assert((loVal->GetRegNum() != REG_NA) && (hiVal->GetRegNum() != REG_NA));

        emit->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, loVal->GetRegNum(), lclNum, 0);
        emit->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, hiVal->GetRegNum(), lclNum, genTypeSize(TYP_INT));
    }
    else
    {
        assert((op1->gtSkipReloadOrCopy()->gtFlags & GTF_MUL_64RSLT) != 0);
        // This is either a multi-reg MUL_LONG, or a multi-reg reload or copy.
        assert(op1->IsMultiRegNode() && (op1->GetMultiRegCount(compiler) == 2));

        // Stack store
        emit->emitIns_S_R(ins_Store(TYP_INT), emitTypeSize(TYP_INT), op1->GetRegByIndex(0), lclNum, 0);
        emit->emitIns_S_R(ins_Store(TYP_INT), emitTypeSize(TYP_INT), op1->GetRegByIndex(1), lclNum,
                          genTypeSize(TYP_INT));
    }
}
#endif // !defined(TARGET_64BIT)

#ifndef TARGET_LOONGARCH64
//------------------------------------------------------------------------
// genCodeForJumpTrue: Generate code for a GT_JTRUE node.
//
// Arguments:
//    jtrue - The node
//
void CodeGen::genCodeForJumpTrue(GenTreeOp* jtrue)
{
    assert(compiler->compCurBB->bbJumpKind == BBJ_COND);
    assert(jtrue->OperIs(GT_JTRUE));

    GenTreeOp*   relop     = jtrue->gtGetOp1()->AsOp();
    GenCondition condition = GenCondition::FromRelop(relop);

    if (condition.PreferSwap())
    {
        condition = GenCondition::Swap(condition);
    }

#if defined(TARGET_XARCH)
    if ((condition.GetCode() == GenCondition::FNEU) &&
        (relop->gtGetOp1()->GetRegNum() == relop->gtGetOp2()->GetRegNum()) &&
        !relop->gtGetOp1()->isUsedFromSpillTemp() && !relop->gtGetOp2()->isUsedFromSpillTemp())
    {
        // For floating point, `x != x` is a common way of
        // checking for NaN. So, in the case where both
        // operands are the same, we can optimize codegen
        // to only do a single check.

        condition = GenCondition(GenCondition::P);
    }

    if (relop->MarkedForSignJumpOpt())
    {
        // If relop was previously marked for a signed jump check optimization because of SF flag
        // reuse, replace jge/jl with jns/js.

        assert(relop->OperGet() == GT_LT || relop->OperGet() == GT_GE);
        condition = (relop->OperGet() == GT_LT) ? GenCondition(GenCondition::S) : GenCondition(GenCondition::NS);
    }

#endif

    inst_JCC(condition, compiler->compCurBB->bbJumpDest);
}

//------------------------------------------------------------------------
// genCodeForJcc: Generate code for a GT_JCC node.
//
// Arguments:
//    jcc - The node
//
void CodeGen::genCodeForJcc(GenTreeCC* jcc)
{
    assert(compiler->compCurBB->bbJumpKind == BBJ_COND);
    assert(jcc->OperIs(GT_JCC));

    inst_JCC(jcc->gtCondition, compiler->compCurBB->bbJumpDest);
}

//------------------------------------------------------------------------
// inst_JCC: Generate a conditional branch instruction sequence.
//
// Arguments:
//   condition - The branch condition
//   target    - The basic block to jump to when the condition is true
//
void CodeGen::inst_JCC(GenCondition condition, BasicBlock* target)
{
    const GenConditionDesc& desc = GenConditionDesc::Get(condition);

    if (desc.oper == GT_NONE)
    {
        inst_JMP(desc.jumpKind1, target);
    }
    else if (desc.oper == GT_OR)
    {
        inst_JMP(desc.jumpKind1, target);
        inst_JMP(desc.jumpKind2, target);
    }
    else // if (desc.oper == GT_AND)
    {
        BasicBlock* labelNext = genCreateTempLabel();
        inst_JMP(emitter::emitReverseJumpKind(desc.jumpKind1), labelNext);
        inst_JMP(desc.jumpKind2, target);
        genDefineTempLabel(labelNext);
    }
}

//------------------------------------------------------------------------
// genCodeForSetcc: Generate code for a GT_SETCC node.
//
// Arguments:
//    setcc - The node
//
void CodeGen::genCodeForSetcc(GenTreeCC* setcc)
{
    assert(setcc->OperIs(GT_SETCC));

    inst_SETCC(setcc->gtCondition, setcc->TypeGet(), setcc->GetRegNum());
    genProduceReg(setcc);
}
#endif // !TARGET_LOONGARCH64
