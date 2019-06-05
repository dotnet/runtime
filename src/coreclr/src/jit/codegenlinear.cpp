// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        // Is this a floating-point argument?
        if (varDsc->IsFloatRegType())
        {
            continue;
        }

        noway_assert(!varTypeIsFloating(varDsc->TypeGet()));

        // Mark the register as holding the variable
        assert(varDsc->lvRegNum != REG_STK);
        if (!varDsc->lvAddrExposed)
        {
            regSet.verifyRegUsed(varDsc->lvRegNum);
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

    // The current implementation of switch tables requires the first block to have a label so it
    // can generate offsets to the switch label targets.
    // TODO-CQ: remove this when switches have been re-implemented to not use this.
    if (compiler->fgHasSwitch)
    {
        compiler->fgFirstBB->bbFlags |= BBF_JMP_TARGET;
    }

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

#if defined(DEBUG) && defined(_TARGET_X86_)

    // Check stack pointer on call stress mode is not compatible with fully interruptible GC. REVIEW: why?
    //
    if (genInterruptible && compiler->opts.compStackCheckOnCall)
    {
        compiler->opts.compStackCheckOnCall = false;
    }

#endif // defined(DEBUG) && defined(_TARGET_X86_)

#if defined(DEBUG) && defined(_TARGET_XARCH_)

    // Check stack pointer on return stress mode is not compatible with fully interruptible GC. REVIEW: why?
    // It is also not compatible with any function that makes a tailcall: we aren't smart enough to only
    // insert the SP check in the non-tailcall returns.
    //
    if ((genInterruptible || compiler->compTailCallUsed) && compiler->opts.compStackCheckOnRet)
    {
        compiler->opts.compStackCheckOnRet = false;
    }

#endif // defined(DEBUG) && defined(_TARGET_XARCH_)

    // Prepare the blocks for exception handling codegen: mark the blocks that needs labels.
    genPrepForEHCodegen();

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
            unsigned   varNum = compiler->lvaTrackedToVarNum[varIndex];
            LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

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
#ifdef DEBUG
                if (verbose && VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varIndex))
                {
                    VarSetOps::AddElemD(compiler, removedGCVars, varIndex);
                }
#endif // DEBUG
                VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varIndex);
            }
            else if (compiler->lvaIsGCTracked(varDsc))
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

        regSet.rsMaskVars = newLiveRegSet;

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

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
        genInsertNopForUnwinder(block);
#endif

        /* Start a new code output block */

        genUpdateCurrentFunclet(block);

#ifdef _TARGET_XARCH_
        if (genAlignLoops && block->bbFlags & BBF_LOOP_HEAD)
        {
            getEmitter()->emitLoopAlign();
        }
#endif

#ifdef DEBUG
        if (compiler->opts.dspCode)
        {
            printf("\n      L_M%03u_" FMT_BB ":\n", Compiler::s_compMethodsCount, block->bbNum);
        }
#endif

        block->bbEmitCookie = nullptr;

        if (block->bbFlags & (BBF_JMP_TARGET | BBF_HAS_LABEL))
        {
            /* Mark a label and update the current set of live GC refs */

            block->bbEmitCookie = getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur,
                                                             gcInfo.gcRegByrefSetCur, FALSE);
        }

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

            // We require the block that starts the Cold section to have a label
            noway_assert(block->bbEmitCookie);
            getEmitter()->emitSetFirstColdIGCookie(block->bbEmitCookie);
        }

        /* Both stacks are always empty on entry to a basic block */

        SetStackLevel(0);
        genAdjustStackLevel(block);
        savedStkLvl = genStackLevel;

        /* Tell everyone which basic block we're working on */

        compiler->compCurBB = block;

        // Needed when jitting debug code
        siBeginBlock(block);

        // BBF_INTERNAL blocks don't correspond to any single IL instruction.
        if (compiler->opts.compDbgInfo && (block->bbFlags & BBF_INTERNAL) &&
            !compiler->fgBBisScratch(block)) // If the block is the distinguished first scratch block, then no need to
                                             // emit a NO_MAPPING entry, immediately after the prolog.
        {
            genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::NO_MAPPING, true);
        }

        bool firstMapping = true;

#if FEATURE_EH_FUNCLETS
        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            genReserveFuncletProlog(block);
        }
#endif // FEATURE_EH_FUNCLETS

        // Clear compCurStmt and compCurLifeTree.
        compiler->compCurStmt     = nullptr;
        compiler->compCurLifeTree = nullptr;

        // Traverse the block in linear order, generating code for each node as we
        // as we encounter it.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        // Set the use-order numbers for each node.
        {
            int useNum = 0;
            for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
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
#endif // DEBUG

        IL_OFFSETX currentILOffset = BAD_IL_OFFSET;
        for (GenTree* node : LIR::AsRange(block).NonPhiNodes())
        {
            // Do we have a new IL offset?
            if (node->OperGet() == GT_IL_OFFSET)
            {
                GenTreeStmt* ilOffset = node->AsStmt();
                genEnsureCodeEmitted(currentILOffset);
                currentILOffset = ilOffset->gtStmtILoffsx;
                genIPmappingAdd(currentILOffset, firstMapping);
                firstMapping = false;
#ifdef DEBUG
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
            if (node->gtHasReg() && node->IsUnusedValue())
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
        regMaskTP nonVarPtrRegs = ptrRegs & ~regSet.rsMaskVars;

        // If return is a GC-type, clear it.  Note that if a common
        // epilog is generated (genReturnBB) it has a void return
        // even though we might return a ref.  We can't use the compRetType
        // as the determiner because something we are tracking as a byref
        // might be used as a return value of a int function (which is legal)
        GenTree* blockLastNode = block->lastNode();
        if ((blockLastNode != nullptr) && (blockLastNode->gtOper == GT_RETURN) &&
            (varTypeIsGC(compiler->info.compRetType) ||
             (blockLastNode->gtOp.gtOp1 != nullptr && varTypeIsGC(blockLastNode->gtOp.gtOp1->TypeGet()))))
        {
            nonVarPtrRegs &= ~RBM_INTRET;
        }

        if (nonVarPtrRegs)
        {
            printf("Regset after " FMT_BB " gcr=", block->bbNum);
            printRegMaskInt(gcInfo.gcRegGCrefSetCur & ~regSet.rsMaskVars);
            compiler->getEmitter()->emitDispRegSet(gcInfo.gcRegGCrefSetCur & ~regSet.rsMaskVars);
            printf(", byr=");
            printRegMaskInt(gcInfo.gcRegByrefSetCur & ~regSet.rsMaskVars);
            compiler->getEmitter()->emitDispRegSet(gcInfo.gcRegByrefSetCur & ~regSet.rsMaskVars);
            printf(", regVars=");
            printRegMaskInt(regSet.rsMaskVars);
            compiler->getEmitter()->emitDispRegSet(regSet.rsMaskVars);
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
#if defined(_TARGET_AMD64_) && defined(LATE_DISASM)
            genAmd64EmitterUnitTests();
#elif defined(_TARGET_ARM64_)
            genArm64EmitterUnitTests();
#endif // _TARGET_ARM64_
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
        genEnsureCodeEmitted(currentILOffset);

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
            unsigned   varNum = compiler->lvaTrackedToVarNum[mismatchLiveVarIndex];
            LclVarDsc* varDsc = compiler->lvaTable + varNum;
            if (varDsc->lvIsRegCandidate())
            {
                if (!foundMismatchedRegVar)
                {
                    JITDUMP("Mismatched live reg vars after BB%02u:", block->bbNum);
                    foundMismatchedRegVar = true;
                }
                JITDUMP(" V%02u", varNum);
            }
        }
        if (foundMismatchedRegVar)
        {
            assert(!"Found mismatched live reg var(s) after block");
            JITDUMP("\n");
        }
#endif

        /* Both stacks should always be empty on exit from a basic block */
        noway_assert(genStackLevel == 0);

#ifdef _TARGET_AMD64_
        // On AMD64, we need to generate a NOP after a call that is the last instruction of the block, in several
        // situations, to support proper exception handling semantics. This is mostly to ensure that when the stack
        // walker computes an instruction pointer for a frame, that instruction pointer is in the correct EH region.
        // The document "X64 and ARM ABIs.docx" has more details. The situations:
        // 1. If the call instruction is in a different EH region as the instruction that follows it.
        // 2. If the call immediately precedes an OS epilog. (Note that what the JIT or VM consider an epilog might
        //    be slightly different from what the OS considers an epilog, and it is the OS-reported epilog that matters
        //    here.)
        // We handle case #1 here, and case #2 in the emitter.
        if (getEmitter()->emitIsLastInsCall())
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
#endif // _TARGET_AMD64_

        /* Do we need to generate a jump or return? */

        switch (block->bbJumpKind)
        {
            case BBJ_ALWAYS:
                inst_JMP(EJ_jmp, block->bbJumpDest);
                break;

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
                        if ((call->gtCall.gtCallMoreFlags & GTF_CALL_M_DOES_NOT_RETURN) != 0)
                        {
                            instGen(INS_BREAKPOINT); // This should never get executed
                        }
                    }
                }

                break;

            case BBJ_CALLFINALLY:
                block = genCallFinally(block);
                break;

#if FEATURE_EH_FUNCLETS

            case BBJ_EHCATCHRET:
                genEHCatchRet(block);
                __fallthrough;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genReserveFuncletEpilog(block);
                break;

#else // !FEATURE_EH_FUNCLETS

            case BBJ_EHCATCHRET:
                noway_assert(!"Unexpected BBJ_EHCATCHRET"); // not used on x86

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genEHFinallyOrFilterRet(block);
                break;

#endif // !FEATURE_EH_FUNCLETS

            case BBJ_NONE:
            case BBJ_COND:
            case BBJ_SWITCH:
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

#if defined(DEBUG) && defined(USING_VARIABLE_LIVE_RANGE)
        if (compiler->verbose)
        {
            varLiveKeeper->dumpBlockVariableLiveRanges(block);
        }
#endif // defined(DEBUG) && defined(USING_VARIABLE_LIVE_RANGE)

        INDEBUG(compiler->compCurBB = nullptr);

    } //------------------ END-FOR each block of the method -------------------

    /* Nothing is live at this point */
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
    unsigned   varNum = tree->gtLclVarCommon.gtLclNum;
    LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

    assert(varDsc->lvIsRegCandidate());

    // We don't actually need to spill if it is already living in memory
    bool needsSpill = ((tree->gtFlags & GTF_VAR_DEF) == 0 && varDsc->lvIsInReg());
    if (needsSpill)
    {
        // In order for a lclVar to have been allocated to a register, it must not have been aliasable, and can
        // therefore be store-normalized (rather than load-normalized). In fact, not performing store normalization
        // can lead to problems on architectures where a lclVar may be allocated to a register that is not
        // addressable at the granularity of the lclVar's defined type (e.g. x86).
        var_types lclTyp = genActualType(varDsc->TypeGet());
        emitAttr  size   = emitTypeSize(lclTyp);

        instruction storeIns = ins_Store(lclTyp, compiler->isSIMDTypeLocalAligned(varNum));
        assert(varDsc->lvRegNum == tree->gtRegNum);
        inst_TT_RV(storeIns, tree, tree->gtRegNum, 0, size);

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
    varDsc->lvRegNum = REG_STK;
    if (varTypeIsMultiReg(tree))
    {
        varDsc->lvOtherReg = REG_STK;
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
// genUpdateVarReg: Update the current register location for a lclVar
//
// Arguments:
//    varDsc - the LclVarDsc for the lclVar
//    tree   - the lclVar node
//
// inline
void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree)
{
    assert(tree->OperIsScalarLocal() || (tree->gtOper == GT_COPY));
    varDsc->lvRegNum = tree->gtRegNum;
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
    if (tree->gtRegNum == REG_NA)
    {
        other = nullptr;
        return nullptr;
    }

    GenTree* op1 = tree->gtOp.gtOp1;
    GenTree* op2 = tree->gtOp.gtOp2;
    if (op1->gtRegNum == tree->gtRegNum)
    {
        other = op2;
        return op1;
    }
    if (op2->gtRegNum == tree->gtRegNum)
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
void CodeGen::genUnspillRegIfNeeded(GenTree* tree)
{
    regNumber dstReg      = tree->gtRegNum;
    GenTree*  unspillTree = tree;

    if (tree->gtOper == GT_RELOAD)
    {
        unspillTree = tree->gtOp.gtOp1;
    }

    if ((unspillTree->gtFlags & GTF_SPILLED) != 0)
    {
        if (genIsRegCandidateLocal(unspillTree))
        {
            // Reset spilled flag, since we are going to load a local variable from its home location.
            unspillTree->gtFlags &= ~GTF_SPILLED;

            GenTreeLclVarCommon* lcl    = unspillTree->AsLclVarCommon();
            LclVarDsc*           varDsc = &compiler->lvaTable[lcl->gtLclNum];

// TODO-Cleanup: The following code could probably be further merged and cleaned up.
#ifdef _TARGET_XARCH_
            // Load local variable from its home location.
            // In most cases the tree type will indicate the correct type to use for the load.
            // However, if it is NOT a normalizeOnLoad lclVar (i.e. NOT a small int that always gets
            // widened when loaded into a register), and its size is not the same as genActualType of
            // the type of the lclVar, then we need to change the type of the tree node when loading.
            // This situation happens due to "optimizations" that avoid a cast and
            // simply retype the node when using long type lclVar as an int.
            // While loading the int in that case would work for this use of the lclVar, if it is
            // later used as a long, we will have incorrectly truncated the long.
            // In the normalizeOnLoad case ins_Load will return an appropriate sign- or zero-
            // extending load.

            var_types treeType = unspillTree->TypeGet();
            if (treeType != genActualType(varDsc->lvType) && !varTypeIsGC(treeType) && !varDsc->lvNormalizeOnLoad())
            {
                assert(!varTypeIsGC(varDsc));
                var_types spillType = genActualType(varDsc->lvType);
                unspillTree->gtType = spillType;
                inst_RV_TT(ins_Load(spillType, compiler->isSIMDTypeLocalAligned(lcl->gtLclNum)), dstReg, unspillTree);
                unspillTree->gtType = treeType;
            }
            else
            {
                inst_RV_TT(ins_Load(treeType, compiler->isSIMDTypeLocalAligned(lcl->gtLclNum)), dstReg, unspillTree);
            }
#elif defined(_TARGET_ARM64_)
            var_types targetType = unspillTree->gtType;
            if (targetType != genActualType(varDsc->lvType) && !varTypeIsGC(targetType) && !varDsc->lvNormalizeOnLoad())
            {
                assert(!varTypeIsGC(varDsc));
                targetType = genActualType(varDsc->lvType);
            }
            instruction ins  = ins_Load(targetType, compiler->isSIMDTypeLocalAligned(lcl->gtLclNum));
            emitAttr    attr = emitActualTypeSize(targetType);
            emitter*    emit = getEmitter();

            // Load local variable from its home location.
            inst_RV_TT(ins, dstReg, unspillTree, 0, attr);
#elif defined(_TARGET_ARM_)
            var_types   targetType = unspillTree->gtType;
            instruction ins        = ins_Load(targetType, compiler->isSIMDTypeLocalAligned(lcl->gtLclNum));
            emitAttr    attr       = emitTypeSize(targetType);

            // Load local variable from its home location.
            inst_RV_TT(ins, dstReg, unspillTree, 0, attr);
#else
            NYI("Unspilling not implemented for this target architecture.");
#endif

            // TODO-Review: We would like to call:
            //      genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(tree));
            // instead of the following code, but this ends up hitting this assert:
            //      assert((regSet.rsMaskVars & regMask) == 0);
            // due to issues with LSRA resolution moves.
            // So, just force it for now. This probably indicates a condition that creates a GC hole!
            //
            // Extra note: I think we really want to call something like gcInfo.gcUpdateForRegVarMove,
            // because the variable is not really going live or dead, but that method is somewhat poorly
            // factored because it, in turn, updates rsMaskVars which is part of RegSet not GCInfo.
            // TODO-Cleanup: This code exists in other CodeGen*.cpp files, and should be moved to CodeGenCommon.cpp.

            // Don't update the variable's location if we are just re-spilling it again.

            if ((unspillTree->gtFlags & GTF_SPILL) == 0)
            {
                genUpdateVarReg(varDsc, tree);

#ifdef USING_VARIABLE_LIVE_RANGE
                // We want "VariableLiveRange" inclusive on the beginbing and exclusive on the ending.
                // For that we shouldn't report an update of the variable location if is becoming dead
                // on the same native offset.
                if ((unspillTree->gtFlags & GTF_VAR_DEATH) == 0)
                {
                    // Report the home change for this variable
                    varLiveKeeper->siUpdateVariableLiveRange(varDsc, lcl->gtLclNum);
                }
#endif // USING_VARIABLE_LIVE_RANGE

#ifdef DEBUG
                if (VarSetOps::IsMember(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex))
                {
                    JITDUMP("\t\t\t\t\t\t\tRemoving V%02u from gcVarPtrSetCur\n", lcl->gtLclNum);
                }
#endif // DEBUG
                VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);

#ifdef DEBUG
                if (compiler->verbose)
                {
                    printf("\t\t\t\t\t\t\tV%02u in reg ", lcl->gtLclNum);
                    varDsc->PrintVarReg();
                    printf(" is becoming live  ");
                    compiler->printTreeID(unspillTree);
                    printf("\n");
                }
#endif // DEBUG

                regSet.AddMaskVars(genGetRegMask(varDsc));
            }

            gcInfo.gcMarkRegPtrVal(dstReg, unspillTree->TypeGet());
        }
        else if (unspillTree->IsMultiRegCall())
        {
            GenTreeCall*         call        = unspillTree->AsCall();
            ReturnTypeDesc*      retTypeDesc = call->GetReturnTypeDesc();
            unsigned             regCount    = retTypeDesc->GetReturnRegCount();
            GenTreeCopyOrReload* reloadTree  = nullptr;
            if (tree->OperGet() == GT_RELOAD)
            {
                reloadTree = tree->AsCopyOrReload();
            }

            // In case of multi-reg call node, GTF_SPILLED flag on it indicates that
            // one or more of its result regs are spilled.  Call node needs to be
            // queried to know which specific result regs to be unspilled.
            for (unsigned i = 0; i < regCount; ++i)
            {
                unsigned flags = call->GetRegSpillFlagByIdx(i);
                if ((flags & GTF_SPILLED) != 0)
                {
                    var_types dstType        = retTypeDesc->GetReturnRegType(i);
                    regNumber unspillTreeReg = call->GetRegNumByIdx(i);

                    if (reloadTree != nullptr)
                    {
                        dstReg = reloadTree->GetRegNumByIdx(i);
                        if (dstReg == REG_NA)
                        {
                            dstReg = unspillTreeReg;
                        }
                    }
                    else
                    {
                        dstReg = unspillTreeReg;
                    }

                    TempDsc* t = regSet.rsUnspillInPlace(call, unspillTreeReg, i);
                    getEmitter()->emitIns_R_S(ins_Load(dstType), emitActualTypeSize(dstType), dstReg, t->tdTempNum(),
                                              0);
                    regSet.tmpRlsTemp(t);
                    gcInfo.gcMarkRegPtrVal(dstReg, dstType);
                }
            }

            unspillTree->gtFlags &= ~GTF_SPILLED;
        }
#if FEATURE_ARG_SPLIT
        else if (unspillTree->OperIsPutArgSplit())
        {
            GenTreePutArgSplit* splitArg = unspillTree->AsPutArgSplit();
            unsigned            regCount = splitArg->gtNumRegs;

            // In case of split struct argument node, GTF_SPILLED flag on it indicates that
            // one or more of its result regs are spilled.  Call node needs to be
            // queried to know which specific result regs to be unspilled.
            for (unsigned i = 0; i < regCount; ++i)
            {
                unsigned flags = splitArg->GetRegSpillFlagByIdx(i);
                if ((flags & GTF_SPILLED) != 0)
                {
                    BYTE*     gcPtrs  = splitArg->gtGcPtrs;
                    var_types dstType = splitArg->GetRegType(i);
                    regNumber dstReg  = splitArg->GetRegNumByIdx(i);

                    TempDsc* t = regSet.rsUnspillInPlace(splitArg, dstReg, i);
                    getEmitter()->emitIns_R_S(ins_Load(dstType), emitActualTypeSize(dstType), dstReg, t->tdTempNum(),
                                              0);
                    regSet.tmpRlsTemp(t);
                    gcInfo.gcMarkRegPtrVal(dstReg, dstType);
                }
            }

            unspillTree->gtFlags &= ~GTF_SPILLED;
        }
#ifdef _TARGET_ARM_
        else if (unspillTree->OperIsMultiRegOp())
        {
            GenTreeMultiRegOp* multiReg = unspillTree->AsMultiRegOp();
            unsigned           regCount = multiReg->GetRegCount();

            // In case of split struct argument node, GTF_SPILLED flag on it indicates that
            // one or more of its result regs are spilled.  Call node needs to be
            // queried to know which specific result regs to be unspilled.
            for (unsigned i = 0; i < regCount; ++i)
            {
                unsigned flags = multiReg->GetRegSpillFlagByIdx(i);
                if ((flags & GTF_SPILLED) != 0)
                {
                    var_types dstType = multiReg->GetRegType(i);
                    regNumber dstReg  = multiReg->GetRegNumByIdx(i);

                    TempDsc* t = regSet.rsUnspillInPlace(multiReg, dstReg, i);
                    getEmitter()->emitIns_R_S(ins_Load(dstType), emitActualTypeSize(dstType), dstReg, t->tdTempNum(),
                                              0);
                    regSet.tmpRlsTemp(t);
                    gcInfo.gcMarkRegPtrVal(dstReg, dstType);
                }
            }

            unspillTree->gtFlags &= ~GTF_SPILLED;
        }
#endif //_TARGET_ARM_
#endif // FEATURE_ARG_SPLIT
        else
        {
            TempDsc* t = regSet.rsUnspillInPlace(unspillTree, unspillTree->gtRegNum);
            getEmitter()->emitIns_R_S(ins_Load(unspillTree->gtType), emitActualTypeSize(unspillTree->TypeGet()), dstReg,
                                      t->tdTempNum(), 0);
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
    assert((node->gtRegNum != REG_NA) && (needReg != REG_NA));
    assert(!node->isUsedFromSpillTemp());
    if (node->gtRegNum != needReg)
    {
        inst_RV_RV(INS_mov, needReg, node->gtRegNum, node->TypeGet());
    }
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
    // location, which matches the gtRegNum on the node).
    // (Note that it doesn't matter if we call this before or after genUnspillRegIfNeeded
    // because if it's on the stack it will always get reloaded into tree->gtRegNum).
    if (genIsRegCandidateLocal(tree))
    {
        GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
        LclVarDsc*           varDsc = &compiler->lvaTable[lcl->GetLclNum()];
        if (varDsc->lvRegNum != REG_STK && varDsc->lvRegNum != tree->gtRegNum)
        {
            inst_RV_RV(ins_Copy(tree->TypeGet()), tree->gtRegNum, varDsc->lvRegNum);
        }
    }

    genUnspillRegIfNeeded(tree);

    // genUpdateLife() will also spill local var if marked as GTF_SPILL by calling CodeGen::genSpillVar
    genUpdateLife(tree);

    assert(tree->gtHasReg());

    // there are three cases where consuming a reg means clearing the bit in the live mask
    // 1. it was not produced by a local
    // 2. it was produced by a local that is going dead
    // 3. it was produced by a local that does not live in that reg (like one allocated on the stack)

    if (genIsRegCandidateLocal(tree))
    {
        GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
        LclVarDsc*           varDsc = &compiler->lvaTable[lcl->GetLclNum()];
        assert(varDsc->lvLRACandidate);

        if ((tree->gtFlags & GTF_VAR_DEATH) != 0)
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(varDsc->lvRegNum));
        }
        else if (varDsc->lvRegNum == REG_STK)
        {
            // We have loaded this into a register only temporarily
            gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
        }
    }
    else
    {
        gcInfo.gcMarkRegSetNpt(tree->gtGetRegMask());
    }

    genCheckConsumeNode(tree);
    return tree->gtRegNum;
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
#if !defined(_TARGET_64BIT_)
    if (tree->OperGet() == GT_LONG)
    {
        genConsumeRegs(tree->gtGetOp1());
        genConsumeRegs(tree->gtGetOp2());
        return;
    }
#endif // !defined(_TARGET_64BIT_)

    if (tree->isUsedFromSpillTemp())
    {
        // spill temps are un-tracked and hence no need to update life
    }
    else if (tree->isContained())
    {
        if (tree->isIndir())
        {
            genConsumeAddress(tree->AsIndir()->Addr());
        }
#ifdef _TARGET_XARCH_
        else if (tree->OperIsLocalRead())
        {
            // A contained lcl var must be living on stack and marked as reg optional, or not be a
            // register candidate.
            unsigned   varNum = tree->AsLclVarCommon()->GetLclNum();
            LclVarDsc* varDsc = compiler->lvaTable + varNum;

            noway_assert(varDsc->lvRegNum == REG_STK);
            noway_assert(tree->IsRegOptional() || !varDsc->lvLRACandidate);

            // Update the life of the lcl var.
            genUpdateLife(tree);
        }
#ifdef FEATURE_HW_INTRINSICS
        else if (tree->OperIs(GT_HWIntrinsic))
        {
            // Only load/store HW intrinsics can be contained (and the address may also be contained).
            HWIntrinsicCategory category = HWIntrinsicInfo::lookupCategory(tree->AsHWIntrinsic()->gtHWIntrinsicId);
            assert((category == HW_Category_MemoryLoad) || (category == HW_Category_MemoryStore));
            int numArgs = HWIntrinsicInfo::lookupNumArgs(tree->AsHWIntrinsic());
            genConsumeAddress(tree->gtGetOp1());
            if (category == HW_Category_MemoryStore)
            {
                assert((numArgs == 2) && !tree->gtGetOp2()->isContained());
                genConsumeReg(tree->gtGetOp2());
            }
            else
            {
                assert(numArgs == 1);
            }
        }
#endif // FEATURE_HW_INTRINSICS
#endif // _TARGET_XARCH_
        else if (tree->OperIsInitVal())
        {
            genConsumeReg(tree->gtGetOp1());
        }
        else
        {
#ifdef FEATURE_SIMD
            // (In)Equality operation that produces bool result, when compared
            // against Vector zero, marks its Vector Zero operand as contained.
            assert(tree->OperIsLeaf() || tree->IsIntegralConstVector(0));
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

#ifdef FEATURE_HW_INTRINSICS
//------------------------------------------------------------------------
// genConsumeHWIntrinsicOperands: Do liveness update for the operands of a GT_HWIntrinsic node
//
// Arguments:
//    node - the GenTreeHWIntrinsic node whose operands will have their liveness updated.
//
// Return Value:
//    None.
//

void CodeGen::genConsumeHWIntrinsicOperands(GenTreeHWIntrinsic* node)
{
    int      numArgs = HWIntrinsicInfo::lookupNumArgs(node);
    GenTree* op1     = node->gtGetOp1();
    if (op1 == nullptr)
    {
        assert((numArgs == 0) && (node->gtGetOp2() == nullptr));
        return;
    }
    if (op1->OperIs(GT_LIST))
    {
        int foundArgs = 0;
        assert(node->gtGetOp2() == nullptr);
        for (GenTreeArgList* list = op1->AsArgList(); list != nullptr; list = list->Rest())
        {
            GenTree* operand = list->Current();
            genConsumeRegs(operand);
            foundArgs++;
        }
        assert(foundArgs == numArgs);
    }
    else
    {
        genConsumeRegs(op1);
        GenTree* op2 = node->gtGetOp2();
        if (op2 != nullptr)
        {
            genConsumeRegs(op2);
            assert(numArgs == 2);
        }
        else
        {
            assert(numArgs == 1);
        }
    }
}
#endif // FEATURE_HW_INTRINSICS

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

    unsigned int size = putArgNode->getArgSize();

    assert(dstReg != REG_NA);
    assert(srcReg != REG_NA);

    // Consume the registers only if they are not contained or set to REG_NA.
    if (srcAddr->gtRegNum != REG_NA)
    {
        genConsumeReg(srcAddr);
    }

    // If the op1 is already in the dstReg - nothing to do.
    // Otherwise load the op1 (GT_ADDR) into the dstReg to copy the struct on the stack by value.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_X86_
    assert(dstReg != REG_SPBASE);
    inst_RV_RV(INS_mov, dstReg, REG_SPBASE);
#else  // !_TARGET_X86_
    GenTree* dstAddr = putArgNode;
    if (dstAddr->gtRegNum != dstReg)
    {
        // Generate LEA instruction to load the stack of the outgoing var + SlotNum offset (or the incoming arg area
        // for tail calls) in RDI.
        // Destination is always local (on the stack) - use EA_PTRSIZE.
        assert(m_stkArgVarNum != BAD_VAR_NUM);
        getEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, dstReg, m_stkArgVarNum, putArgNode->getArgOffset());
    }
#endif // !_TARGET_X86_

    if (srcAddr->gtRegNum != srcReg)
    {
        if (srcAddr->OperIsLocalAddr())
        {
            // The OperLocalAddr is always contained.
            assert(srcAddr->isContained());
            GenTreeLclVarCommon* lclNode = srcAddr->AsLclVarCommon();

            // Generate LEA instruction to load the LclVar address in RSI.
            // Source is known to be on the stack. Use EA_PTRSIZE.
            unsigned int offset = 0;
            if (srcAddr->OperGet() == GT_LCL_FLD_ADDR)
            {
                offset = srcAddr->AsLclFld()->gtLclOffs;
            }
            getEmitter()->emitIns_R_S(INS_lea, EA_PTRSIZE, srcReg, lclNode->gtLclNum, offset);
        }
        else
        {
            assert(srcAddr->gtRegNum != REG_NA);
            // Source is not known to be on the stack. Use EA_BYREF.
            getEmitter()->emitIns_R_R(INS_mov, EA_BYREF, srcReg, srcAddr->gtRegNum);
        }
    }

    if (sizeReg != REG_NA)
    {
        inst_RV_IV(INS_mov, sizeReg, size, EA_PTRSIZE);
    }
}
#endif // FEATURE_PUT_STRUCT_ARG_STK

#if FEATURE_ARG_SPLIT
//------------------------------------------------------------------------
// genConsumeArgRegSplit: Consume register(s) in Call node to set split struct argument.
//                        Liveness update for the PutArgSplit node is not needed
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
    assert(putArgNode->gtHasReg());

    genUnspillRegIfNeeded(putArgNode);

    // Skip updating GC info
    // GC info for all argument registers will be cleared in caller

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
#ifndef _TARGET_X86_
void CodeGen::genPutArgStkFieldList(GenTreePutArgStk* putArgStk, unsigned outArgVarNum)
{
    assert(putArgStk->gtOp1->OperIs(GT_FIELD_LIST));

    // Evaluate each of the GT_FIELD_LIST items into their register
    // and store their register into the outgoing argument area.
    unsigned argOffset = putArgStk->getArgOffset();
    for (GenTreeFieldList* fieldListPtr = putArgStk->gtOp1->AsFieldList(); fieldListPtr != nullptr;
         fieldListPtr                   = fieldListPtr->Rest())
    {
        GenTree* nextArgNode = fieldListPtr->gtOp.gtOp1;
        genConsumeReg(nextArgNode);

        regNumber reg  = nextArgNode->gtRegNum;
        var_types type = nextArgNode->TypeGet();
        emitAttr  attr = emitTypeSize(type);

        // Emit store instructions to store the registers produced by the GT_FIELD_LIST into the outgoing
        // argument area.
        unsigned thisFieldOffset = argOffset + fieldListPtr->gtFieldOffset;
        getEmitter()->emitIns_S_R(ins_Store(type), attr, reg, outArgVarNum, thisFieldOffset);

        // We can't write beyond the arg area
        assert((thisFieldOffset + EA_SIZE_IN_BYTES(attr)) <= compiler->lvaLclSize(outArgVarNum));
    }
}
#endif // !_TARGET_X86_

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
            genSetRegToIcon(sizeReg, blockSize);
        }
        else
        {
            GenTree* sizeNode = blkNode->AsDynBlk()->gtDynamicSize;
            if (sizeNode->gtRegNum != sizeReg)
            {
                inst_RV_RV(INS_mov, sizeReg, sizeNode->gtRegNum, sizeNode->TypeGet());
            }
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
            src = src->gtOp.gtOp1;
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
            src = src->gtOp.gtOp1;
        }
        else
        {
            // This must be a local struct.
            // Load its address into srcReg.
            inst_RV_TT(INS_lea, srcReg, src, 0, EA_BYREF);
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
        genConsumeReg(blkNode->AsDynBlk()->gtDynamicSize);
    }

    // Next, perform any necessary moves.
    genCopyRegIfNeeded(dstAddr, dstReg);
    genSetBlockSrc(blkNode, srcReg);
    genSetBlockSize(blkNode, sizeReg);
}

//-------------------------------------------------------------------------
// genProduceReg: do liveness update for register produced by the current
// node in codegen.
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
            // Store local variable to its home location.
            // Ensure that lclVar stores are typed correctly.
            unsigned varNum = tree->gtLclVarCommon.gtLclNum;
            assert(!compiler->lvaTable[varNum].lvNormalizeOnStore() ||
                   (tree->TypeGet() == genActualType(compiler->lvaTable[varNum].TypeGet())));
            inst_TT_RV(ins_Store(tree->gtType, compiler->isSIMDTypeLocalAligned(varNum)), tree, tree->gtRegNum);
        }
        else
        {
            // In case of multi-reg call node, spill flag on call node
            // indicates that one or more of its allocated regs need to
            // be spilled.  Call node needs to be further queried to
            // know which of its result regs needs to be spilled.
            if (tree->IsMultiRegCall())
            {
                GenTreeCall*    call        = tree->AsCall();
                ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
                unsigned        regCount    = retTypeDesc->GetReturnRegCount();

                for (unsigned i = 0; i < regCount; ++i)
                {
                    unsigned flags = call->GetRegSpillFlagByIdx(i);
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
                GenTreePutArgSplit* argSplit = tree->AsPutArgSplit();
                unsigned            regCount = argSplit->gtNumRegs;

                for (unsigned i = 0; i < regCount; ++i)
                {
                    unsigned flags = argSplit->GetRegSpillFlagByIdx(i);
                    if ((flags & GTF_SPILL) != 0)
                    {
                        regNumber reg = argSplit->GetRegNumByIdx(i);
                        regSet.rsSpillTree(reg, argSplit, i);
                        gcInfo.gcMarkRegSetNpt(genRegMask(reg));
                    }
                }
            }
#ifdef _TARGET_ARM_
            else if (tree->OperIsMultiRegOp())
            {
                GenTreeMultiRegOp* multiReg = tree->AsMultiRegOp();
                unsigned           regCount = multiReg->GetRegCount();

                for (unsigned i = 0; i < regCount; ++i)
                {
                    unsigned flags = multiReg->GetRegSpillFlagByIdx(i);
                    if ((flags & GTF_SPILL) != 0)
                    {
                        regNumber reg = multiReg->GetRegNumByIdx(i);
                        regSet.rsSpillTree(reg, multiReg, i);
                        gcInfo.gcMarkRegSetNpt(genRegMask(reg));
                    }
                }
            }
#endif // _TARGET_ARM_
#endif // FEATURE_ARG_SPLIT
            else
            {
                regSet.rsSpillTree(tree->gtRegNum, tree);
                gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
            }

            tree->gtFlags |= GTF_SPILLED;
            tree->gtFlags &= ~GTF_SPILL;

            return;
        }
    }

    genUpdateLife(tree);

    // If we've produced a register, mark it as a pointer, as needed.
    if (tree->gtHasReg())
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
            // Multi-reg call node will produce more than one register result.
            // Mark all the regs produced by call node.
            if (tree->IsMultiRegCall())
            {
                GenTreeCall*    call        = tree->AsCall();
                ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
                unsigned        regCount    = retTypeDesc->GetReturnRegCount();

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
                GenTreeCopyOrReload* copy        = tree->AsCopyOrReload();
                GenTreeCall*         call        = copy->gtGetOp1()->AsCall();
                ReturnTypeDesc*      retTypeDesc = call->GetReturnTypeDesc();
                unsigned             regCount    = retTypeDesc->GetReturnRegCount();

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
            else
            {
                gcInfo.gcMarkRegPtrVal(tree->gtRegNum, tree->TypeGet());
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
                          IL_OFFSETX            ilOffset,
                          regNumber             base,
                          bool                  isJump)
{
#if !defined(_TARGET_X86_)
    int argSize = 0;
#endif // !defined(_TARGET_X86_)
    getEmitter()->emitIns_Call(emitter::EmitCallType(callType),
                               methHnd,
                               INDEBUG_LDISASM_COMMA(sigInfo)
                               addr,
                               argSize,
                               retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                               gcInfo.gcVarPtrSetCur,
                               gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur,
                               ilOffset, base, REG_NA, 0, 0, isJump);
}
// clang-format on

// generates an indirect call via addressing mode (call []) given an indir node
//     methHnd - optional, only used for pretty printing
//     retSize - emitter type of return for GC purposes, should be EA_BYREF, EA_GCREF, or EA_PTRSIZE(not GC)
//
// clang-format off
void CodeGen::genEmitCall(int                   callType,
                          CORINFO_METHOD_HANDLE methHnd,
                          INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo)
                          GenTreeIndir*         indir
                          X86_ARG(int argSize),
                          emitAttr              retSize
                          MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(emitAttr secondRetSize),
                          IL_OFFSETX            ilOffset)
{
#if !defined(_TARGET_X86_)
    int argSize = 0;
#endif // !defined(_TARGET_X86_)
    genConsumeAddress(indir->Addr());

    getEmitter()->emitIns_Call(emitter::EmitCallType(callType),
                               methHnd,
                               INDEBUG_LDISASM_COMMA(sigInfo)
                               nullptr,
                               argSize,
                               retSize
                               MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize),
                               gcInfo.gcVarPtrSetCur,
                               gcInfo.gcRegGCrefSetCur,
                               gcInfo.gcRegByrefSetCur,
                               ilOffset,
                               (indir->Base()  != nullptr) ? indir->Base()->gtRegNum  : REG_NA,
                               (indir->Index() != nullptr) ? indir->Index()->gtRegNum : REG_NA,
                               indir->Scale(),
                               indir->Offset());
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
#ifndef _TARGET_64BIT_
    else if (varTypeIsLong(tree->gtOp1))
    {
        genLongToIntCast(tree);
    }
#endif // !_TARGET_64BIT_
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
#ifdef _TARGET_64BIT_
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

        m_extendKind    = COPY;
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

#if !defined(_TARGET_64BIT_)
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
    emitter* emit = getEmitter();

    GenTreeLclVarCommon* lclNode = treeNode->AsLclVarCommon();
    unsigned             lclNum  = lclNode->gtLclNum;
    LclVarDsc*           varDsc  = &(compiler->lvaTable[lclNum]);
    assert(varDsc->TypeGet() == TYP_LONG);
    assert(!varDsc->lvPromoted);
    GenTree* op1 = treeNode->gtOp.gtOp1;

    // A GT_LONG is always contained, so it cannot have RELOAD or COPY inserted between it and its consumer,
    // but a MUL_LONG may.
    noway_assert(op1->OperIs(GT_LONG) || op1->gtSkipReloadOrCopy()->OperIs(GT_MUL_LONG));
    genConsumeRegs(op1);

    if (op1->OperGet() == GT_LONG)
    {
        GenTree* loVal = op1->gtGetOp1();
        GenTree* hiVal = op1->gtGetOp2();

        noway_assert((loVal->gtRegNum != REG_NA) && (hiVal->gtRegNum != REG_NA));

        emit->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, loVal->gtRegNum, lclNum, 0);
        emit->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, hiVal->gtRegNum, lclNum, genTypeSize(TYP_INT));
    }
    else
    {
        assert((op1->gtSkipReloadOrCopy()->gtFlags & GTF_MUL_64RSLT) != 0);
        // This is either a multi-reg MUL_LONG, or a multi-reg reload or copy.
        assert(op1->IsMultiRegNode() && (op1->GetMultiRegCount() == 2));

        // Stack store
        emit->emitIns_S_R(ins_Store(TYP_INT), emitTypeSize(TYP_INT), op1->GetRegByIndex(0), lclNum, 0);
        emit->emitIns_S_R(ins_Store(TYP_INT), emitTypeSize(TYP_INT), op1->GetRegByIndex(1), lclNum,
                          genTypeSize(TYP_INT));
    }
}
#endif // !defined(_TARGET_64BIT_)

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

    GenCondition condition = GenCondition::FromRelop(jtrue->gtGetOp1());

    if (condition.PreferSwap())
    {
        condition = GenCondition::Swap(condition);
    }

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

    inst_SETCC(setcc->gtCondition, setcc->TypeGet(), setcc->gtRegNum);
    genProduceReg(setcc);
}
