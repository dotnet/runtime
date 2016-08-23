// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        ARM Code Generator                                 XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifndef LEGACY_BACKEND // This file is ONLY used for the RyuJIT backend that uses the linear scan register allocator

#ifdef _TARGET_ARM_
#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "emit.h"

#ifndef JIT32_GCENCODER
#include "gcinfoencoder.h"
#endif

// Get the register assigned to the given node

regNumber CodeGenInterface::genGetAssignedReg(GenTreePtr tree)
{
    return tree->gtRegNum;
}

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

void CodeGen::genSpillVar(GenTreePtr tree)
{
    regMaskTP  regMask;
    unsigned   varNum = tree->gtLclVarCommon.gtLclNum;
    LclVarDsc* varDsc = &(compiler->lvaTable[varNum]);

    // We don't actually need to spill if it is already living in memory
    bool needsSpill = ((tree->gtFlags & GTF_VAR_DEF) == 0 && varDsc->lvIsInReg());
    if (needsSpill)
    {
        bool restoreRegVar = false;
        if (tree->gtOper == GT_REG_VAR)
        {
            tree->SetOper(GT_LCL_VAR);
            restoreRegVar = true;
        }

        // mask off the flag to generate the right spill code, then bring it back
        tree->gtFlags &= ~GTF_REG_VAL;

        instruction storeIns = ins_Store(tree->TypeGet());

        if (varTypeIsMultiReg(tree))
        {
            assert(varDsc->lvRegNum == genRegPairLo(tree->gtRegPair));
            assert(varDsc->lvOtherReg == genRegPairHi(tree->gtRegPair));
            regNumber regLo = genRegPairLo(tree->gtRegPair);
            regNumber regHi = genRegPairHi(tree->gtRegPair);
            inst_TT_RV(storeIns, tree, regLo);
            inst_TT_RV(storeIns, tree, regHi, 4);
        }
        else
        {
            assert(varDsc->lvRegNum == tree->gtRegNum);
            inst_TT_RV(storeIns, tree, tree->gtRegNum);
        }
        tree->gtFlags |= GTF_REG_VAL;

        if (restoreRegVar)
        {
            tree->SetOper(GT_REG_VAR);
        }

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
}

// inline
void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTreePtr tree)
{
    assert(tree->OperIsScalarLocal() || (tree->gtOper == GT_COPY));
    varDsc->lvRegNum = tree->gtRegNum;
}

/*****************************************************************************
 *
 *  Generate code that will set the given register to the integer constant.
 */

void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type, insFlags flags)
{
    // Reg cannot be a FP reg
    assert(!genIsValidFloatReg(reg));

    // The only TYP_REF constant that can come this path is a managed 'null' since it is not
    // relocatable.  Other ref type constants (e.g. string objects) go through a different
    // code path.
    noway_assert(type != TYP_REF || val == 0);

    if (val == 0)
    {
        instGen_Set_Reg_To_Zero(emitActualTypeSize(type), reg, flags);
    }
    else
    {
        // TODO-CQ: needs all the optimized cases
        getEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(type), reg, val);
    }
}

/*****************************************************************************
 *
 *   Generate code to check that the GS cookie wasn't thrashed by a buffer
 *   overrun.  If pushReg is true, preserve all registers around code sequence.
 *   Otherwise, ECX maybe modified.
 */
void CodeGen::genEmitGSCookieCheck(bool pushReg)
{
    NYI("ARM genEmitGSCookieCheck is not yet implemented for protojit");
}

/*****************************************************************************
 *
 *  Generate code for all the basic blocks in the function.
 */

void CodeGen::genCodeForBBlist()
{
    unsigned   varNum;
    LclVarDsc* varDsc;

    unsigned savedStkLvl;

#ifdef DEBUG
    genInterruptibleUsed = true;

    // You have to be careful if you create basic blocks from now on
    compiler->fgSafeBasicBlockCreation = false;

    // This stress mode is not comptible with fully interruptible GC
    if (genInterruptible && compiler->opts.compStackCheckOnCall)
    {
        compiler->opts.compStackCheckOnCall = false;
    }

    // This stress mode is not comptible with fully interruptible GC
    if (genInterruptible && compiler->opts.compStackCheckOnRet)
    {
        compiler->opts.compStackCheckOnRet = false;
    }
#endif

    // Prepare the blocks for exception handling codegen: mark the blocks that needs labels.
    genPrepForEHCodegen();

    assert(!compiler->fgFirstBBScratch ||
           compiler->fgFirstBB == compiler->fgFirstBBScratch); // compiler->fgFirstBBScratch has to be first.

    /* Initialize the spill tracking logic */

    regSet.rsSpillBeg();

#ifdef DEBUGGING_SUPPORT
    /* Initialize the line# tracking logic */

    if (compiler->opts.compScopeInfo)
    {
        siInit();
    }
#endif

    if (compiler->opts.compDbgEnC)
    {
        noway_assert(isFramePointerUsed());
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
    }

    /* If we have any pinvoke calls, we might potentially trash everything */
    if (compiler->info.compCallUnmanaged)
    {
        noway_assert(isFramePointerUsed()); // Setup of Pinvoke frame currently requires an EBP style frame
        regSet.rsSetRegsModified(RBM_INT_CALLEE_SAVED & ~RBM_FPBASE);
    }

    genPendingCallLabel = nullptr;

    /* Initialize the pointer tracking code */

    gcInfo.gcRegPtrSetInit();
    gcInfo.gcVarPtrSetInit();

    /* If any arguments live in registers, mark those regs as such */

    for (varNum = 0, varDsc = compiler->lvaTable; varNum < compiler->lvaCount; varNum++, varDsc++)
    {
        /* Is this variable a parameter assigned to a register? */

        if (!varDsc->lvIsParam || !varDsc->lvRegister)
            continue;

        /* Is the argument live on entry to the method? */

        if (!VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
            continue;

        /* Is this a floating-point argument? */

        if (varDsc->IsFloatRegType())
            continue;

        noway_assert(!varTypeIsFloating(varDsc->TypeGet()));

        /* Mark the register as holding the variable */

        regTracker.rsTrackRegLclVar(varDsc->lvRegNum, varNum);
    }

    unsigned finallyNesting = 0;

    // Make sure a set is allocated for compiler->compCurLife (in the long case), so we can set it to empty without
    // allocation at the start of each basic block.
    VarSetOps::AssignNoCopy(compiler, compiler->compCurLife, VarSetOps::MakeEmpty(compiler));

    /*-------------------------------------------------------------------------
     *
     *  Walk the basic blocks and generate code for each one
     *
     */

    BasicBlock* block;
    BasicBlock* lblk; /* previous block */

    for (lblk = NULL, block = compiler->fgFirstBB; block != NULL; lblk = block, block = block->bbNext)
    {
#ifdef DEBUG
        if (compiler->verbose)
        {
            printf("\n=============== Generating ");
            block->dspBlockHeader(compiler, true, true);
            compiler->fgDispBBLiveness(block);
        }
#endif // DEBUG

        /* Figure out which registers hold variables on entry to this block */

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
        VARSET_ITER_INIT(compiler, iter, block->bbLiveIn, varIndex);
        while (iter.NextElem(compiler, &varIndex))
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
            }
            else if (varDsc->lvType == TYP_REF || varDsc->lvType == TYP_BYREF)
            {
                VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varIndex);
            }
        }

        regSet.rsMaskVars = newLiveRegSet;
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

        /* Start a new code output block */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_EH_FUNCLETS
#if defined(_TARGET_ARM_)
        // If this block is the target of a finally return, we need to add a preceding NOP, in the same EH region,
        // so the unwinder doesn't get confused by our "movw lr, xxx; movt lr, xxx; b Lyyy" calling convention that
        // calls the funclet during non-exceptional control flow.
        if (block->bbFlags & BBF_FINALLY_TARGET)
        {
            assert(block->bbFlags & BBF_JMP_TARGET);

#ifdef DEBUG
            if (compiler->verbose)
            {
                printf("\nEmitting finally target NOP predecessor for BB%02u\n", block->bbNum);
            }
#endif
            // Create a label that we'll use for computing the start of an EH region, if this block is
            // at the beginning of such a region. If we used the existing bbEmitCookie as is for
            // determining the EH regions, then this NOP would end up outside of the region, if this
            // block starts an EH region. If we pointed the existing bbEmitCookie here, then the NOP
            // would be executed, which we would prefer not to do.

            block->bbUnwindNopEmitCookie =
                getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur);

            instGen(INS_nop);
        }
#endif // defined(_TARGET_ARM_)

        genUpdateCurrentFunclet(block);
#endif // FEATURE_EH_FUNCLETS

#ifdef _TARGET_XARCH_
        if (genAlignLoops && block->bbFlags & BBF_LOOP_HEAD)
        {
            getEmitter()->emitLoopAlign();
        }
#endif

#ifdef DEBUG
        if (compiler->opts.dspCode)
            printf("\n      L_M%03u_BB%02u:\n", Compiler::s_compMethodsCount, block->bbNum);
#endif

        block->bbEmitCookie = NULL;

        if (block->bbFlags & (BBF_JMP_TARGET | BBF_HAS_LABEL))
        {
            /* Mark a label and update the current set of live GC refs */

            block->bbEmitCookie =
                getEmitter()->emitAddLabel(gcInfo.gcVarPtrSetCur, gcInfo.gcRegGCrefSetCur, gcInfo.gcRegByrefSetCur,
                                           /*isFinally*/ block->bbFlags & BBF_FINALLY_TARGET);
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
            noway_assert(!lblk->bbFallsThrough());

            // We require the block that starts the Cold section to have a label
            noway_assert(block->bbEmitCookie);
            getEmitter()->emitSetFirstColdIGCookie(block->bbEmitCookie);
        }

        /* Both stacks are always empty on entry to a basic block */

        genStackLevel = 0;

#if !FEATURE_FIXED_OUT_ARGS
        /* Check for inserted throw blocks and adjust genStackLevel */

        if (!isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block))
        {
            noway_assert(block->bbFlags & BBF_JMP_TARGET);

            genStackLevel = compiler->fgThrowHlpBlkStkLevel(block) * sizeof(int);

            if (genStackLevel)
            {
                NYI("Need emitMarkStackLvl()");
            }
        }
#endif // !FEATURE_FIXED_OUT_ARGS

        savedStkLvl = genStackLevel;

        /* Tell everyone which basic block we're working on */

        compiler->compCurBB = block;

#ifdef DEBUGGING_SUPPORT
        siBeginBlock(block);

        // BBF_INTERNAL blocks don't correspond to any single IL instruction.
        if (compiler->opts.compDbgInfo && (block->bbFlags & BBF_INTERNAL) && block != compiler->fgFirstBB)
            genIPmappingAdd((IL_OFFSETX)ICorDebugInfo::NO_MAPPING, true);

        bool firstMapping = true;
#endif // DEBUGGING_SUPPORT

        /*---------------------------------------------------------------------
         *
         *  Generate code for each statement-tree in the block
         *
         */
        CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_EH_FUNCLETS
        if (block->bbFlags & BBF_FUNCLET_BEG)
        {
            genReserveFuncletProlog(block);
        }
#endif // FEATURE_EH_FUNCLETS

        // Clear compCurStmt and compCurLifeTree.
        compiler->compCurStmt     = nullptr;
        compiler->compCurLifeTree = nullptr;

#ifdef DEBUG
        bool pastProfileUpdate = false;
#endif

// Traverse the block in linear order, generating code for each node as we
// as we encounter it.
#ifdef DEBUGGING_SUPPORT
        IL_OFFSETX currentILOffset = BAD_IL_OFFSET;
#endif
        for (GenTree* node : LIR::AsRange(block))
        {
#ifdef DEBUGGING_SUPPORT
            // Do we have a new IL offset?
            if (node->OperGet() == GT_IL_OFFSET)
            {
                genEnsureCodeEmitted(currentILOffset);

                currentILOffset = node->gtStmt.gtStmtILoffsx;

                genIPmappingAdd(currentILOffset, firstMapping);
                firstMapping = false;
            }
#endif // DEBUGGING_SUPPORT

#ifdef DEBUG
            if (node->OperGet() == GT_IL_OFFSET)
            {
                noway_assert(node->gtStmt.gtStmtLastILoffs <= compiler->info.compILCodeSize ||
                             node->gtStmt.gtStmtLastILoffs == BAD_IL_OFFSET);

                if (compiler->opts.dspCode && compiler->opts.dspInstrs &&
                    node->gtStmt.gtStmtLastILoffs != BAD_IL_OFFSET)
                {
                    while (genCurDispOffset <= node->gtStmt.gtStmtLastILoffs)
                    {
                        genCurDispOffset += dumpSingleInstr(compiler->info.compCode, genCurDispOffset, ">    ");
                    }
                }
            }
#endif // DEBUG

            genCodeForTreeNode(node);
            if (node->gtHasReg() && node->gtLsraInfo.isLocalDefUse)
            {
                genConsumeReg(node);
            }

#ifdef DEBUG
            regSet.rsSpillChk();

            assert((node->gtFlags & GTF_SPILL) == 0);

            /* Make sure we didn't bungle pointer register tracking */

            regMaskTP ptrRegs       = (gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur);
            regMaskTP nonVarPtrRegs = ptrRegs & ~regSet.rsMaskVars;

            // If return is a GC-type, clear it.  Note that if a common
            // epilog is generated (genReturnBB) it has a void return
            // even though we might return a ref.  We can't use the compRetType
            // as the determiner because something we are tracking as a byref
            // might be used as a return value of a int function (which is legal)
            if (node->gtOper == GT_RETURN && (varTypeIsGC(compiler->info.compRetType) ||
                                              (node->gtOp.gtOp1 != 0 && varTypeIsGC(node->gtOp.gtOp1->TypeGet()))))
            {
                nonVarPtrRegs &= ~RBM_INTRET;
            }

            // When profiling, the first few nodes in a catch block will be an update of
            // the profile count (does not interfere with the exception object).
            if (((compiler->opts.eeFlags & CORJIT_FLG_BBINSTR) != 0) && handlerGetsXcptnObj(block->bbCatchTyp))
            {
                pastProfileUpdate = pastProfileUpdate || node->OperGet() == GT_CATCH_ARG;
                if (!pastProfileUpdate)
                {
                    nonVarPtrRegs &= ~RBM_EXCEPTION_OBJECT;
                }
            }

            if (nonVarPtrRegs)
            {
                printf("Regset after node=");
                Compiler::printTreeID(node);
                printf(" BB%02u gcr=", block->bbNum);
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

            noway_assert(nonVarPtrRegs == 0);
#endif // DEBUG
        }

#ifdef DEBUGGING_SUPPORT
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

        if (compiler->opts.compScopeInfo && (compiler->info.compVarScopesCount > 0))
        {
            siEndBlock(block);

            /* Is this the last block, and are there any open scopes left ? */

            bool isLastBlockProcessed = (block->bbNext == NULL);
            if (block->isBBCallAlwaysPair())
            {
                isLastBlockProcessed = (block->bbNext->bbNext == NULL);
            }

            if (isLastBlockProcessed && siOpenScopeList.scNext)
            {
                /* This assert no longer holds, because we may insert a throw
                   block to demarcate the end of a try or finally region when they
                   are at the end of the method.  It would be nice if we could fix
                   our code so that this throw block will no longer be necessary. */

                // noway_assert(block->bbCodeOffsEnd != compiler->info.compILCodeSize);

                siCloseAllOpenScopes();
            }
        }

#endif // DEBUGGING_SUPPORT

        genStackLevel -= savedStkLvl;

#ifdef DEBUG
        // compCurLife should be equal to the liveOut set, except that we don't keep
        // it up to date for vars that are not register candidates
        // (it would be nice to have a xor set function)

        VARSET_TP VARSET_INIT_NOCOPY(extraLiveVars, VarSetOps::Diff(compiler, block->bbLiveOut, compiler->compCurLife));
        VarSetOps::UnionD(compiler, extraLiveVars, VarSetOps::Diff(compiler, compiler->compCurLife, block->bbLiveOut));
        VARSET_ITER_INIT(compiler, extraLiveVarIter, extraLiveVars, extraLiveVarIndex);
        while (extraLiveVarIter.NextElem(compiler, &extraLiveVarIndex))
        {
            unsigned   varNum = compiler->lvaTrackedToVarNum[extraLiveVarIndex];
            LclVarDsc* varDsc = compiler->lvaTable + varNum;
            assert(!varDsc->lvIsRegCandidate());
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
#endif //_TARGET_AMD64_

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
                if ((block->bbNext == NULL)
#if FEATURE_EH_FUNCLETS
                    || (block->bbNext->bbFlags & BBF_FUNCLET_BEG)
#endif // FEATURE_EH_FUNCLETS
#ifdef _TARGET_AMD64_
                    || !BasicBlock::sameEHRegion(block, block->bbNext)
#endif // _TARGET_AMD64_
                    || (!isFramePointerUsed() && compiler->fgIsThrowHlpBlk(block->bbNext)) ||
                    block->bbNext == compiler->fgFirstColdBlock)
                {
                    instGen(INS_BREAKPOINT); // This should never get executed
                }

                break;

            case BBJ_CALLFINALLY:

                // Now set REG_LR to the address of where the finally funclet should
                // return to directly.

                BasicBlock* bbFinallyRet;
                bbFinallyRet = NULL;

                // We don't have retless calls, since we use the BBJ_ALWAYS to point at a NOP pad where
                // we would have otherwise created retless calls.
                assert(block->isBBCallAlwaysPair());

                assert(block->bbNext != NULL);
                assert(block->bbNext->bbJumpKind == BBJ_ALWAYS);
                assert(block->bbNext->bbJumpDest != NULL);
                assert(block->bbNext->bbJumpDest->bbFlags & BBF_FINALLY_TARGET);

                bbFinallyRet = block->bbNext->bbJumpDest;
                bbFinallyRet->bbFlags |= BBF_JMP_TARGET;

#if 0
            // TODO-ARM-CQ:
            // We don't know the address of finally funclet yet.  But adr requires the offset
            // to finally funclet from current IP is within 4095 bytes. So this code is disabled
            // for now.
            getEmitter()->emitIns_J_R (INS_adr,
                                     EA_4BYTE,
                                     bbFinallyRet,
                                     REG_LR);
#else  // !0
                // Load the address where the finally funclet should return into LR.
                // The funclet prolog/epilog will do "push {lr}" / "pop {pc}" to do
                // the return.
                getEmitter()->emitIns_R_L(INS_movw, EA_4BYTE_DSP_RELOC, bbFinallyRet, REG_LR);
                getEmitter()->emitIns_R_L(INS_movt, EA_4BYTE_DSP_RELOC, bbFinallyRet, REG_LR);
#endif // !0

                // Jump to the finally BB
                inst_JMP(EJ_jmp, block->bbJumpDest);

                // The BBJ_ALWAYS is used because the BBJ_CALLFINALLY can't point to the
                // jump target using bbJumpDest - that is already used to point
                // to the finally block. So just skip past the BBJ_ALWAYS unless the
                // block is RETLESS.
                if (!(block->bbFlags & BBF_RETLESS_CALL))
                {
                    assert(block->isBBCallAlwaysPair());

                    lblk  = block;
                    block = block->bbNext;
                }
                break;

#ifdef _TARGET_ARM_

            case BBJ_EHCATCHRET:
                // set r0 to the address the VM should return to after the catch
                getEmitter()->emitIns_R_L(INS_movw, EA_4BYTE_DSP_RELOC, block->bbJumpDest, REG_R0);
                getEmitter()->emitIns_R_L(INS_movt, EA_4BYTE_DSP_RELOC, block->bbJumpDest, REG_R0);

                __fallthrough;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genReserveFuncletEpilog(block);
                break;

#elif defined(_TARGET_AMD64_)

            case BBJ_EHCATCHRET:
                // Set EAX to the address the VM should return to after the catch.
                // Generate a RIP-relative
                //         lea reg, [rip + disp32] ; the RIP is implicit
                // which will be position-indepenent.
                // TODO-ARM-Bug?: For ngen, we need to generate a reloc for the displacement (maybe EA_PTR_DSP_RELOC).
                getEmitter()->emitIns_R_L(INS_lea, EA_PTRSIZE, block->bbJumpDest, REG_INTRET);
                __fallthrough;

            case BBJ_EHFINALLYRET:
            case BBJ_EHFILTERRET:
                genReserveFuncletEpilog(block);
                break;

#endif // _TARGET_AMD64_

            case BBJ_NONE:
            case BBJ_COND:
            case BBJ_SWITCH:
                break;

            default:
                noway_assert(!"Unexpected bbJumpKind");
                break;
        }

#ifdef DEBUG
        compiler->compCurBB = 0;
#endif

    } //------------------ END-FOR each block of the method -------------------

    /* Nothing is live at this point */
    genUpdateLife(VarSetOps::MakeEmpty(compiler));

    /* Finalize the spill  tracking logic */

    regSet.rsSpillEnd();

    /* Finalize the temp   tracking logic */

    compiler->tmpEnd();

#ifdef DEBUG
    if (compiler->verbose)
    {
        printf("\n# ");
        printf("compCycleEstimate = %6d, compSizeEstimate = %5d ", compiler->compCycleEstimate, compiler->compSizeEstimate);
        printf("%s\n", compiler->info.compFullName);
    }
#endif
}

// return the child that has the same reg as the dst (if any)
// other child returned (out param) in 'other'
GenTree* sameRegAsDst(GenTree* tree, GenTree*& other /*out*/)
{
    if (tree->gtRegNum == REG_NA)
    {
        other = nullptr;
        return NULL;
    }

    GenTreePtr op1 = tree->gtOp.gtOp1->gtEffectiveVal();
    GenTreePtr op2 = tree->gtOp.gtOp2->gtEffectiveVal();
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
        return NULL;
    }
}

//  move an immediate value into an integer register

void CodeGen::instGen_Set_Reg_To_Imm(emitAttr size, regNumber reg, ssize_t imm, insFlags flags)
{
    // reg cannot be a FP register
    assert(!genIsValidFloatReg(reg));

    if (!compiler->opts.compReloc)
    {
        size = EA_SIZE(size); // Strip any Reloc flags from size if we aren't doing relocs
    }

    if ((imm == 0) && !EA_IS_RELOC(size))
    {
        instGen_Set_Reg_To_Zero(size, reg, flags);
    }
    else
    {
#ifdef _TARGET_AMD64_
        if (AddrShouldUsePCRel(imm))
        {
            getEmitter()->emitIns_R_AI(INS_lea, EA_PTR_DSP_RELOC, reg, imm);
        }
        else
#endif // _TARGET_AMD64_
        {
            getEmitter()->emitIns_R_I(INS_mov, size, reg, imm);
        }
    }
    regTracker.rsTrackRegIntCns(reg, imm);
}

/*****************************************************************************
 *
 * Generate code to set a register 'targetReg' of type 'targetType' to the constant
 * specified by the constant (GT_CNS_INT or GT_CNS_DBL) in 'tree'. This does not call
 * genProduceReg() on the target register.
 */
void CodeGen::genSetRegToConst(regNumber targetReg, var_types targetType, GenTreePtr tree)
{
    switch (tree->gtOper)
    {
        case GT_CNS_INT:
        {
            // relocatable values tend to come down as a CNS_INT of native int type
            // so the line between these two opcodes is kind of blurry
            GenTreeIntConCommon* con    = tree->AsIntConCommon();
            ssize_t              cnsVal = con->IconValue();

            bool needReloc = compiler->opts.compReloc && tree->IsIconHandle();
            if (needReloc)
            {
                instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, targetReg, cnsVal);
                regTracker.rsTrackRegTrash(targetReg);
            }
            else
            {
                genSetRegToIcon(targetReg, cnsVal, targetType);
            }
        }
        break;

        case GT_CNS_DBL:
        {
            NYI("GT_CNS_DBL");
        }
        break;

        default:
            unreached();
    }
}

/*****************************************************************************
 *
 * Generate code for a single node in the tree.
 * Preconditions: All operands have been evaluated
 *
 */
void CodeGen::genCodeForTreeNode(GenTreePtr treeNode)
{
    regNumber targetReg  = treeNode->gtRegNum;
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = getEmitter();

    JITDUMP("Generating: ");
    DISPNODE(treeNode);

    // contained nodes are part of their parents for codegen purposes
    // ex : immediates, most LEAs
    if (treeNode->isContained())
    {
        return;
    }

    switch (treeNode->gtOper)
    {
        case GT_CNS_INT:
        case GT_CNS_DBL:
            genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
            break;

        case GT_NEG:
        case GT_NOT:
        {
            NYI("GT_NEG and GT_NOT");
        }
            genProduceReg(treeNode);
            break;

        case GT_OR:
        case GT_XOR:
        case GT_AND:
            assert(varTypeIsIntegralOrI(treeNode));
            __fallthrough;

        case GT_ADD:
        case GT_SUB:
        {
            const genTreeOps oper = treeNode->OperGet();
            if ((oper == GT_ADD || oper == GT_SUB) && treeNode->gtOverflow())
            {
                // This is also checked in the importer.
                NYI("Overflow not yet implemented");
            }

            GenTreePtr  op1 = treeNode->gtGetOp1();
            GenTreePtr  op2 = treeNode->gtGetOp2();
            instruction ins = genGetInsForOper(treeNode->OperGet(), targetType);

            // The arithmetic node must be sitting in a register (since it's not contained)
            noway_assert(targetReg != REG_NA);

            regNumber op1reg = op1->gtRegNum;
            regNumber op2reg = op2->gtRegNum;

            GenTreePtr dst;
            GenTreePtr src;

            genConsumeIfReg(op1);
            genConsumeIfReg(op2);

            // This is the case of reg1 = reg1 op reg2
            // We're ready to emit the instruction without any moves
            if (op1reg == targetReg)
            {
                dst = op1;
                src = op2;
            }
            // We have reg1 = reg2 op reg1
            // In order for this operation to be correct
            // we need that op is a commutative operation so
            // we can convert it into reg1 = reg1 op reg2 and emit
            // the same code as above
            else if (op2reg == targetReg)
            {
                noway_assert(GenTree::OperIsCommutative(treeNode->OperGet()));
                dst = op2;
                src = op1;
            }
            // dest, op1 and op2 registers are different:
            // reg3 = reg1 op reg2
            // We can implement this by issuing a mov:
            // reg3 = reg1
            // reg3 = reg3 op reg2
            else
            {
                inst_RV_RV(ins_Move_Extend(targetType, true), targetReg, op1reg, op1->gtType);
                regTracker.rsTrackRegCopy(targetReg, op1reg);
                gcInfo.gcMarkRegPtrVal(targetReg, targetType);
                dst = treeNode;
                src = op2;
            }

            regNumber r = emit->emitInsBinary(ins, emitTypeSize(treeNode), dst, src);
            noway_assert(r == targetReg);
        }
            genProduceReg(treeNode);
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
            genCodeForShift(treeNode);
            // genCodeForShift() calls genProduceReg()
            break;

        case GT_CAST:
            // Cast is never contained (?)
            noway_assert(targetReg != REG_NA);

            // Overflow conversions from float/double --> int types go through helper calls.
            if (treeNode->gtOverflow() && !varTypeIsFloating(treeNode->gtOp.gtOp1))
                NYI("Unimplmented GT_CAST:int <--> int with overflow");

            if (varTypeIsFloating(targetType) && varTypeIsFloating(treeNode->gtOp.gtOp1))
            {
                // Casts float/double <--> double/float
                genFloatToFloatCast(treeNode);
            }
            else if (varTypeIsFloating(treeNode->gtOp.gtOp1))
            {
                // Casts float/double --> int32/int64
                genFloatToIntCast(treeNode);
            }
            else if (varTypeIsFloating(targetType))
            {
                // Casts int32/uint32/int64/uint64 --> float/double
                genIntToFloatCast(treeNode);
            }
            else
            {
                // Casts int <--> int
                genIntToIntCast(treeNode);
            }
            // The per-case functions call genProduceReg()
            break;

        case GT_LCL_VAR:
        {
            GenTreeLclVarCommon* lcl = treeNode->AsLclVarCommon();
            // lcl_vars are not defs
            assert((treeNode->gtFlags & GTF_VAR_DEF) == 0);

            bool isRegCandidate = compiler->lvaTable[lcl->gtLclNum].lvIsRegCandidate();

            if (isRegCandidate && !(treeNode->gtFlags & GTF_VAR_DEATH))
            {
                assert((treeNode->InReg()) || (treeNode->gtFlags & GTF_SPILLED));
            }

            // If this is a register candidate that has been spilled, genConsumeReg() will
            // reload it at the point of use.  Otherwise, if it's not in a register, we load it here.

            if (!treeNode->InReg() && !(treeNode->gtFlags & GTF_SPILLED))
            {
                assert(!isRegCandidate);
                emit->emitIns_R_S(ins_Load(treeNode->TypeGet()), emitTypeSize(treeNode), treeNode->gtRegNum,
                                  lcl->gtLclNum, 0);
                genProduceReg(treeNode);
            }
        }
        break;

        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR_ADDR:
        {
            // Address of a local var.  This by itself should never be allocated a register.
            // If it is worth storing the address in a register then it should be cse'ed into
            // a temp and that would be allocated a register.
            noway_assert(targetType == TYP_BYREF);
            noway_assert(!treeNode->InReg());

            inst_RV_TT(INS_lea, targetReg, treeNode, 0, EA_BYREF);
        }
            genProduceReg(treeNode);
            break;

        case GT_LCL_FLD:
        {
            NYI_IF(targetType == TYP_STRUCT, "GT_LCL_FLD: struct load local field not supported");
            NYI_IF(treeNode->gtRegNum == REG_NA, "GT_LCL_FLD: load local field not into a register is not supported");

            emitAttr size   = emitTypeSize(targetType);
            unsigned offs   = treeNode->gtLclFld.gtLclOffs;
            unsigned varNum = treeNode->gtLclVarCommon.gtLclNum;
            assert(varNum < compiler->lvaCount);

            emit->emitIns_R_S(ins_Move_Extend(targetType, treeNode->InReg()), size, targetReg, varNum, offs);
        }
            genProduceReg(treeNode);
            break;

        case GT_STORE_LCL_FLD:
        {
            NYI_IF(targetType == TYP_STRUCT, "GT_STORE_LCL_FLD: struct store local field not supported");
            noway_assert(!treeNode->InReg());

            GenTreePtr op1 = treeNode->gtOp.gtOp1->gtEffectiveVal();
            genConsumeIfReg(op1);
            emit->emitInsBinary(ins_Store(targetType), emitTypeSize(treeNode), treeNode, op1);
        }
        break;

        case GT_STORE_LCL_VAR:
        {
            NYI_IF(targetType == TYP_STRUCT, "struct store local not supported");

            GenTreePtr op1 = treeNode->gtOp.gtOp1->gtEffectiveVal();
            genConsumeIfReg(op1);
            if (treeNode->gtRegNum == REG_NA)
            {
                // stack store
                emit->emitInsMov(ins_Store(targetType), emitTypeSize(treeNode), treeNode);
                compiler->lvaTable[treeNode->AsLclVarCommon()->gtLclNum].lvRegNum = REG_STK;
            }
            else if (op1->isContained())
            {
                // Currently, we assume that the contained source of a GT_STORE_LCL_VAR writing to a register
                // must be a constant. However, in the future we might want to support a contained memory op.
                // This is a bit tricky because we have to decide it's contained before register allocation,
                // and this would be a case where, once that's done, we need to mark that node as always
                // requiring a register - which we always assume now anyway, but once we "optimize" that
                // we'll have to take cases like this into account.
                assert((op1->gtRegNum == REG_NA) && op1->OperIsConst());
                genSetRegToConst(treeNode->gtRegNum, targetType, op1);
            }
            else if (op1->gtRegNum != treeNode->gtRegNum)
            {
                assert(op1->gtRegNum != REG_NA);
                emit->emitInsBinary(ins_Move_Extend(targetType, true), emitTypeSize(treeNode), treeNode, op1);
            }
            if (treeNode->gtRegNum != REG_NA)
                genProduceReg(treeNode);
        }
        break;

        case GT_RETFILT:
            // A void GT_RETFILT is the end of a finally. For non-void filter returns we need to load the result in
            // the return register, if it's not already there. The processing is the same as GT_RETURN.
            if (targetType != TYP_VOID)
            {
                // For filters, the IL spec says the result is type int32. Further, the only specified legal values
                // are 0 or 1, with the use of other values "undefined".
                assert(targetType == TYP_INT);
            }

            __fallthrough;

        case GT_RETURN:
        {
            GenTreePtr op1 = treeNode->gtOp.gtOp1;
            if (targetType == TYP_VOID)
            {
                assert(op1 == nullptr);
                break;
            }
            assert(op1 != nullptr);
            op1 = op1->gtEffectiveVal();

            NYI_IF(op1->gtRegNum == REG_NA, "GT_RETURN: return of a value not in register");
            genConsumeReg(op1);

            regNumber retReg = varTypeIsFloating(op1) ? REG_FLOATRET : REG_INTRET;
            if (op1->gtRegNum != retReg)
            {
                inst_RV_RV(ins_Move_Extend(targetType, true), retReg, op1->gtRegNum, targetType);
            }
        }
        break;

        case GT_LEA:
        {
            // if we are here, it is the case where there is an LEA that cannot
            // be folded into a parent instruction
            GenTreeAddrMode* lea = treeNode->AsAddrMode();
            genLeaInstruction(lea);
        }
        // genLeaInstruction calls genProduceReg()
        break;

        case GT_IND:
            emit->emitInsMov(ins_Load(treeNode->TypeGet()), emitTypeSize(treeNode), treeNode);
            genProduceReg(treeNode);
            break;

        case GT_MUL:
        {
            NYI("GT_MUL");
        }
            genProduceReg(treeNode);
            break;

        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
            // We shouldn't be seeing GT_MOD on float/double args as it should get morphed into a
            // helper call by front-end.  Similarly we shouldn't be seeing GT_UDIV and GT_UMOD
            // on float/double args.
            noway_assert(!varTypeIsFloating(treeNode));
            __fallthrough;

        case GT_DIV:
        {
            NYI("GT_DIV");
        }
            genProduceReg(treeNode);
            break;

        case GT_INTRINSIC:
        {
            NYI("GT_INTRINSIC");
        }
            genProduceReg(treeNode);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        {
            // TODO-ARM-CQ: Check if we can use the currently set flags.
            // TODO-ARM-CQ: Check for the case where we can simply transfer the carry bit to a register
            //         (signed < or >= where targetReg != REG_NA)

            GenTreeOp* tree = treeNode->AsOp();
            GenTreePtr op1  = tree->gtOp1->gtEffectiveVal();
            GenTreePtr op2  = tree->gtOp2->gtEffectiveVal();

            genConsumeIfReg(op1);
            genConsumeIfReg(op2);

            instruction ins = INS_cmp;
            emitAttr    cmpAttr;
            if (varTypeIsFloating(op1))
            {
                NYI("Floating point compare");

                bool isUnordered = ((treeNode->gtFlags & GTF_RELOP_NAN_UN) != 0);
                switch (tree->OperGet())
                {
                    case GT_EQ:
                        ins = INS_beq;
                    case GT_NE:
                        ins = INS_bne;
                    case GT_LT:
                        ins = isUnordered ? INS_blt : INS_blo;
                    case GT_LE:
                        ins = isUnordered ? INS_ble : INS_bls;
                    case GT_GE:
                        ins = isUnordered ? INS_bpl : INS_bge;
                    case GT_GT:
                        ins = isUnordered ? INS_bhi : INS_bgt;
                    default:
                        unreached();
                }
            }
            else
            {
                var_types op1Type = op1->TypeGet();
                var_types op2Type = op2->TypeGet();
                assert(!varTypeIsFloating(op2Type));
                ins = INS_cmp;
                if (op1Type == op2Type)
                {
                    cmpAttr = emitTypeSize(op1Type);
                }
                else
                {
                    var_types cmpType    = TYP_INT;
                    bool      op1Is64Bit = (varTypeIsLong(op1Type) || op1Type == TYP_REF);
                    bool      op2Is64Bit = (varTypeIsLong(op2Type) || op2Type == TYP_REF);
                    NYI_IF(op1Is64Bit || op2Is64Bit, "Long compare");
                    assert(!op1->isContainedMemoryOp() || op1Type == op2Type);
                    assert(!op2->isContainedMemoryOp() || op1Type == op2Type);
                    cmpAttr = emitTypeSize(cmpType);
                }
            }
            emit->emitInsBinary(ins, cmpAttr, op1, op2);

            // Are we evaluating this into a register?
            if (targetReg != REG_NA)
            {
                genSetRegToCond(targetReg, tree);
                genProduceReg(tree);
            }
        }
        break;

        case GT_JTRUE:
        {
            GenTree* cmp = treeNode->gtOp.gtOp1->gtEffectiveVal();
            assert(cmp->OperIsCompare());
            assert(compiler->compCurBB->bbJumpKind == BBJ_COND);

            // Get the "kind" and type of the comparison.  Note that whether it is an unsigned cmp
            // is governed by a flag NOT by the inherent type of the node
            // TODO-ARM-CQ: Check if we can use the currently set flags.
            CompareKind compareKind = ((cmp->gtFlags & GTF_UNSIGNED) != 0) ? CK_UNSIGNED : CK_SIGNED;

            emitJumpKind jmpKind   = genJumpKindForOper(cmp->gtOper, compareKind);
            BasicBlock*  jmpTarget = compiler->compCurBB->bbJumpDest;

            inst_JMP(jmpKind, jmpTarget);
        }
        break;

        case GT_RETURNTRAP:
        {
            // this is nothing but a conditional call to CORINFO_HELP_STOP_FOR_GC
            // based on the contents of 'data'

            GenTree* data = treeNode->gtOp.gtOp1->gtEffectiveVal();
            genConsumeIfReg(data);
            GenTreeIntCon cns = intForm(TYP_INT, 0);
            emit->emitInsBinary(INS_cmp, emitTypeSize(TYP_INT), data, &cns);

            BasicBlock* skipLabel = genCreateTempLabel();

            emitJumpKind jmpEqual = genJumpKindForOper(GT_EQ, CK_SIGNED);
            inst_JMP(jmpEqual, skipLabel);
            // emit the call to the EE-helper that stops for GC (or other reasons)

            genEmitHelperCall(CORINFO_HELP_STOP_FOR_GC, 0, EA_UNKNOWN);
            genDefineTempLabel(skipLabel);
        }
        break;

        case GT_STOREIND:
        {
            NYI("GT_STOREIND");
        }
        break;

        case GT_COPY:
        {
            assert(treeNode->gtOp.gtOp1->IsLocal());
            GenTreeLclVarCommon* lcl    = treeNode->gtOp.gtOp1->AsLclVarCommon();
            LclVarDsc*           varDsc = &compiler->lvaTable[lcl->gtLclNum];
            inst_RV_RV(ins_Move_Extend(targetType, true), targetReg, genConsumeReg(treeNode->gtOp.gtOp1), targetType,
                       emitTypeSize(targetType));

            // The old location is dying
            genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(treeNode->gtOp.gtOp1));

            gcInfo.gcMarkRegSetNpt(genRegMask(treeNode->gtOp.gtOp1->gtRegNum));

            genUpdateVarReg(varDsc, treeNode);

            // The new location is going live
            genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
        }
            genProduceReg(treeNode);
            break;

        case GT_LIST:
        case GT_ARGPLACE:
            // Nothing to do
            break;

        case GT_PUTARG_STK:
        {
            NYI_IF(targetType == TYP_STRUCT, "GT_PUTARG_STK: struct support not implemented");

            // Get argument offset on stack.
            // Here we cross check that argument offset hasn't changed from lowering to codegen since
            // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
            int argOffset = treeNode->AsPutArgStk()->gtSlotNum * TARGET_POINTER_SIZE;
#ifdef DEBUG
            fgArgTabEntryPtr curArgTabEntry = compiler->gtArgEntryByNode(treeNode->AsPutArgStk()->gtCall, treeNode);
            assert(curArgTabEntry);
            assert(argOffset == (int)curArgTabEntry->slotNum * TARGET_POINTER_SIZE);
#endif

            GenTreePtr data = treeNode->gtOp.gtOp1->gtEffectiveVal();
            if (data->isContained())
            {
                emit->emitIns_S_I(ins_Store(targetType), emitTypeSize(targetType), compiler->lvaOutgoingArgSpaceVar,
                                  argOffset, (int)data->AsIntConCommon()->IconValue());
            }
            else
            {
                genConsumeReg(data);
                emit->emitIns_S_R(ins_Store(targetType), emitTypeSize(targetType), data->gtRegNum,
                                  compiler->lvaOutgoingArgSpaceVar, argOffset);
            }
        }
        break;

        case GT_PUTARG_REG:
        {
            NYI_IF(targetType == TYP_STRUCT, "GT_PUTARG_REG: struct support not implemented");

            // commas show up here commonly, as part of a nullchk operation
            GenTree* op1 = treeNode->gtOp.gtOp1->gtEffectiveVal();
            // If child node is not already in the register we need, move it
            genConsumeReg(op1);
            if (treeNode->gtRegNum != op1->gtRegNum)
            {
                inst_RV_RV(ins_Move_Extend(targetType, true), treeNode->gtRegNum, op1->gtRegNum, targetType);
            }
        }
            genProduceReg(treeNode);
            break;

        case GT_CALL:
            genCallInstruction(treeNode);
            break;

        case GT_LOCKADD:
        case GT_XCHG:
        case GT_XADD:
            genLockedInstructions(treeNode);
            break;

        case GT_CMPXCHG:
        {
            NYI("GT_CMPXCHG");
        }
            genProduceReg(treeNode);
            break;

        case GT_RELOAD:
            // do nothing - reload is just a marker.
            // The parent node will call genConsumeReg on this which will trigger the unspill of this node's child
            // into the register specified in this node.
            break;

        case GT_NOP:
            break;

        case GT_NO_OP:
            NYI("GT_NO_OP");
            break;

        case GT_ARR_BOUNDS_CHECK:
            genRangeCheck(treeNode);
            break;

        case GT_PHYSREG:
            if (treeNode->gtRegNum != treeNode->AsPhysReg()->gtSrcReg)
            {
                inst_RV_RV(INS_mov, treeNode->gtRegNum, treeNode->AsPhysReg()->gtSrcReg, targetType);

                genTransferRegGCState(treeNode->gtRegNum, treeNode->AsPhysReg()->gtSrcReg);
            }
            break;

        case GT_PHYSREGDST:
            break;

        case GT_NULLCHECK:
        {
            assert(!treeNode->gtOp.gtOp1->isContained());
            regNumber reg = genConsumeReg(treeNode->gtOp.gtOp1);
            emit->emitIns_AR_R(INS_cmp, EA_4BYTE, reg, reg, 0);
        }
        break;

        case GT_CATCH_ARG:

            noway_assert(handlerGetsXcptnObj(compiler->compCurBB->bbCatchTyp));

            /* Catch arguments get passed in a register. genCodeForBBlist()
               would have marked it as holding a GC object, but not used. */

            noway_assert(gcInfo.gcRegGCrefSetCur & RBM_EXCEPTION_OBJECT);
            genConsumeReg(treeNode);
            break;

        case GT_PINVOKE_PROLOG:
            noway_assert(((gcInfo.gcRegGCrefSetCur | gcInfo.gcRegByrefSetCur) & ~fullIntArgRegMask()) == 0);

            // the runtime side requires the codegen here to be consistent
            emit->emitDisableRandomNops();
            break;

        case GT_LABEL:
            genPendingCallLabel       = genCreateTempLabel();
            treeNode->gtLabel.gtLabBB = genPendingCallLabel;
            emit->emitIns_R_L(INS_lea, EA_PTRSIZE, genPendingCallLabel, treeNode->gtRegNum);
            break;

        default:
        {
#ifdef DEBUG
            char message[256];
            sprintf(message, "NYI: Unimplemented node type %s\n", GenTree::NodeName(treeNode->OperGet()));
            notYetImplemented(message, __FILE__, __LINE__);
#else
            NYI("unimplemented node");
#endif
        }
        break;
    }
}

// generate code for the locked operations:
// GT_LOCKADD, GT_XCHG, GT_XADD
void CodeGen::genLockedInstructions(GenTree* treeNode)
{
    NYI("genLockedInstructions");
}

// generate code for GT_ARR_BOUNDS_CHECK node
void CodeGen::genRangeCheck(GenTreePtr oper)
{
    noway_assert(oper->OperGet() == GT_ARR_BOUNDS_CHECK);
    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTreePtr arrLen    = bndsChk->gtArrLen->gtEffectiveVal();
    GenTreePtr arrIdx    = bndsChk->gtIndex->gtEffectiveVal();
    GenTreePtr arrRef    = NULL;
    int        lenOffset = 0;

    GenTree *    src1, *src2;
    emitJumpKind jmpKind;

    if (arrIdx->isContainedIntOrIImmed())
    {
        // To encode using a cmp immediate, we place the
        //  constant operand in the second position
        src1    = arrLen;
        src2    = arrIdx;
        jmpKind = genJumpKindForOper(GT_LE, CK_UNSIGNED);
    }
    else
    {
        src1    = arrIdx;
        src2    = arrLen;
        jmpKind = genJumpKindForOper(GT_GE, CK_UNSIGNED);
    }

    genConsumeIfReg(src1);
    genConsumeIfReg(src2);

    getEmitter()->emitInsBinary(INS_cmp, emitAttr(TYP_INT), src1, src2);
    genJumpToThrowHlpBlk(jmpKind, SCK_RNGCHK_FAIL, bndsChk->gtIndRngFailBB);
}

// make a temporary indir we can feed to pattern matching routines
// in cases where we don't want to instantiate all the indirs that happen
//
GenTreeIndir CodeGen::indirForm(var_types type, GenTree* base)
{
    GenTreeIndir i(GT_IND, type, base, nullptr);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

// make a temporary int we can feed to pattern matching routines
// in cases where we don't want to instantiate
//
GenTreeIntCon CodeGen::intForm(var_types type, ssize_t value)
{
    GenTreeIntCon i(type, value);
    i.gtRegNum = REG_NA;
    // has to be nonnull (because contained nodes can't be the last in block)
    // but don't want it to be a valid pointer
    i.gtNext = (GenTree*)(-1);
    return i;
}

instruction CodeGen::genGetInsForOper(genTreeOps oper, var_types type)
{
    instruction ins;

    if (varTypeIsFloating(type))
        return CodeGen::ins_MathOp(oper, type);

    switch (oper)
    {
        case GT_ADD:
            ins = INS_add;
            break;
        case GT_AND:
            ins = INS_AND;
            break;
        case GT_MUL:
            ins = INS_MUL;
            break;
        case GT_LSH:
            ins = INS_SHIFT_LEFT_LOGICAL;
            break;
        case GT_NEG:
            ins = INS_rsb;
            break;
        case GT_NOT:
            ins = INS_NOT;
            break;
        case GT_OR:
            ins = INS_OR;
            break;
        case GT_RSH:
            ins = INS_SHIFT_RIGHT_ARITHM;
            break;
        case GT_RSZ:
            ins = INS_SHIFT_RIGHT_LOGICAL;
            break;
        case GT_SUB:
            ins = INS_sub;
            break;
        case GT_XOR:
            ins = INS_XOR;
            break;
        default:
            unreached();
            break;
    }
    return ins;
}

//------------------------------------------------------------------------
// genCodeForShift: Generates the code sequence for a GenTree node that
// represents a bit shift or rotate operation (<<, >>, >>>, rol, ror).
//
// Arguments:
//    tree - the bit shift node (that specifies the type of bit shift to perform).
//
// Assumptions:
//    a) All GenTrees are register allocated.
//
void CodeGen::genCodeForShift(GenTreePtr tree)
{
    NYI("genCodeForShift");
}

void CodeGen::genUnspillRegIfNeeded(GenTree* tree)
{
    regNumber dstReg = tree->gtRegNum;

    GenTree* unspillTree = tree;
    if (tree->gtOper == GT_RELOAD)
    {
        unspillTree = tree->gtOp.gtOp1;
    }
    if (unspillTree->gtFlags & GTF_SPILLED)
    {
        if (genIsRegCandidateLocal(unspillTree))
        {
            // Reset spilled flag, since we are going to load a local variable from its home location.
            unspillTree->gtFlags &= ~GTF_SPILLED;

            // Load local variable from its home location.
            inst_RV_TT(ins_Load(unspillTree->gtType), dstReg, unspillTree);

            unspillTree->SetInReg();

            GenTreeLclVarCommon* lcl    = unspillTree->AsLclVarCommon();
            LclVarDsc*           varDsc = &compiler->lvaTable[lcl->gtLclNum];

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

            genUpdateVarReg(varDsc, tree);
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
                Compiler::printTreeID(unspillTree);
                printf("\n");
            }
#endif // DEBUG

            regSet.AddMaskVars(genGetRegMask(varDsc));
        }
        else
        {
            TempDsc* t = regSet.rsUnspillInPlace(unspillTree, unspillTree->gtRegNum);
            compiler->tmpRlsTemp(t);
            getEmitter()->emitIns_R_S(ins_Load(unspillTree->gtType), emitActualTypeSize(unspillTree->gtType), dstReg,
                                      t->tdTempNum(), 0);

            unspillTree->SetInReg();
        }

        gcInfo.gcMarkRegPtrVal(dstReg, unspillTree->TypeGet());
    }
}

// do liveness update for a subnode that is being consumed by codegen
regNumber CodeGen::genConsumeReg(GenTree* tree)
{
    genUnspillRegIfNeeded(tree);

    // genUpdateLife() will also spill local var if marked as GTF_SPILL by calling CodeGen::genSpillVar
    genUpdateLife(tree);
    assert(tree->gtRegNum != REG_NA);

    // there are three cases where consuming a reg means clearing the bit in the live mask
    // 1. it was not produced by a local
    // 2. it was produced by a local that is going dead
    // 3. it was produced by a local that does not live in that reg (like one allocated on the stack)

    if (genIsRegCandidateLocal(tree))
    {
        GenTreeLclVarCommon* lcl    = tree->AsLclVarCommon();
        LclVarDsc*           varDsc = &compiler->lvaTable[lcl->GetLclNum()];

        if (varDsc->lvRegNum == tree->gtRegNum && ((tree->gtFlags & GTF_VAR_DEATH) != 0))
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
        }
        else if (!varDsc->lvLRACandidate)
        {
            gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
        }
    }
    else
    {
        gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
    }

    return tree->gtRegNum;
}

// Do liveness update for an address tree: one of GT_LEA, GT_LCL_VAR, or GT_CNS_INT (for call indirect).
void CodeGen::genConsumeAddress(GenTree* addr)
{
    if (addr->OperGet() == GT_LEA)
    {
        genConsumeAddrMode(addr->AsAddrMode());
    }
    else
    {
        assert(!addr->isContained());
        genConsumeReg(addr);
    }
}

// do liveness update for a subnode that is being consumed by codegen
void CodeGen::genConsumeAddrMode(GenTreeAddrMode* addr)
{
    if (addr->Base())
        genConsumeReg(addr->Base());
    if (addr->Index())
        genConsumeReg(addr->Index());
}

// do liveness update for register produced by the current node in codegen
void CodeGen::genProduceReg(GenTree* tree)
{
    if (tree->gtFlags & GTF_SPILL)
    {
        if (genIsRegCandidateLocal(tree))
        {
            // Store local variable to its home location.
            tree->gtFlags &= ~GTF_REG_VAL;
            inst_TT_RV(ins_Store(tree->gtType), tree, tree->gtRegNum);
        }
        else
        {
            tree->SetInReg();
            regSet.rsSpillTree(tree->gtRegNum, tree);
            tree->gtFlags |= GTF_SPILLED;
            tree->gtFlags &= ~GTF_SPILL;
            gcInfo.gcMarkRegSetNpt(genRegMask(tree->gtRegNum));
            return;
        }
    }

    genUpdateLife(tree);

    // If we've produced a register, mark it as a pointer, as needed.
    // Except in the case of a dead definition of a lclVar.
    if (tree->gtHasReg() && (!tree->IsLocal() || (tree->gtFlags & GTF_VAR_DEATH) == 0))
    {
        gcInfo.gcMarkRegPtrVal(tree->gtRegNum, tree->TypeGet());
    }
    tree->SetInReg();
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

// Produce code for a GT_CALL node
void CodeGen::genCallInstruction(GenTreePtr node)
{
    NYI("Call not implemented");
}

// produce code for a GT_LEA subnode
void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    if (lea->Base() && lea->Index())
    {
        regNumber baseReg  = genConsumeReg(lea->Base());
        regNumber indexReg = genConsumeReg(lea->Index());
        getEmitter()->emitIns_R_ARX(INS_lea, EA_BYREF, lea->gtRegNum, baseReg, indexReg, lea->gtScale, lea->gtOffset);
    }
    else if (lea->Base())
    {
        getEmitter()->emitIns_R_AR(INS_lea, EA_BYREF, lea->gtRegNum, genConsumeReg(lea->Base()), lea->gtOffset);
    }

    genProduceReg(lea);
}

// Generate code to materialize a condition into a register
// (the condition codes must already have been appropriately set)

void CodeGen::genSetRegToCond(regNumber dstReg, GenTreePtr tree)
{
    NYI("genSetRegToCond");
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer cast
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    The treeNode must have an assigned register.
//    For a signed convert from byte, the source must be in a byte-addressable register.
//    Neither the source nor target type can be a floating point type.
//
void CodeGen::genIntToIntCast(GenTreePtr treeNode)
{
    NYI("Cast");
}

//------------------------------------------------------------------------
// genFloatToFloatCast: Generate code for a cast between float and double
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    The cast is between float and double.
//
void CodeGen::genFloatToFloatCast(GenTreePtr treeNode)
{
    NYI("Cast");
}

//------------------------------------------------------------------------
// genIntToFloatCast: Generate code to cast an int/long to float/double
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    SrcType= int32/uint32/int64/uint64 and DstType=float/double.
//
void CodeGen::genIntToFloatCast(GenTreePtr treeNode)
{
    NYI("Cast");
}

//------------------------------------------------------------------------
// genFloatToIntCast: Generate code to cast float/double to int/long
//
// Arguments:
//    treeNode - The GT_CAST node
//
// Return Value:
//    None.
//
// Assumptions:
//    Cast is a non-overflow conversion.
//    The treeNode must have an assigned register.
//    SrcType=float/double and DstType= int32/uint32/int64/uint64
//
void CodeGen::genFloatToIntCast(GenTreePtr treeNode)
{
    NYI("Cast");
}

/*****************************************************************************
 *
 *  Create and record GC Info for the function.
 */
#ifdef JIT32_GCENCODER
void*
#else
void
#endif
CodeGen::genCreateAndStoreGCInfo(unsigned codeSize, unsigned prologSize, unsigned epilogSize DEBUGARG(void* codePtr))
{
#ifdef JIT32_GCENCODER
    return genCreateAndStoreGCInfoJIT32(codeSize, prologSize, epilogSize DEBUGARG(codePtr));
#else
    genCreateAndStoreGCInfoX64(codeSize, prologSize DEBUGARG(codePtr));
#endif
}

// TODO-ARM-Cleanup: It seems that the ARM JIT (classic and otherwise) uses this method, so it seems to be
// inappropriately named?

void CodeGen::genCreateAndStoreGCInfoX64(unsigned codeSize, unsigned prologSize DEBUGARG(void* codePtr))
{
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) AllowZeroAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder);

    // Follow the code pattern of the x86 gc info encoder (genCreateAndStoreGCInfoJIT32).
    gcInfo.gcInfoBlockHdrSave(gcInfoEncoder, codeSize, prologSize);

    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS);
    // Now we've requested all the slots we'll need; "finalize" these (make more compact data structures for them).
    gcInfoEncoder->FinalizeSlotIds();
    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK);

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
}

/*****************************************************************************
 *  Emit a call to a helper function.
 */

void CodeGen::genEmitHelperCall(unsigned helper,
                                int      argSize,
                                emitAttr retSize
#ifndef LEGACY_BACKEND
                                ,
                                regNumber callTargetReg /*= REG_NA */
#endif                                                  // !LEGACY_BACKEND
                                )
{
    NYI("Helper call");
}

/*****************************************************************************/
#ifdef DEBUGGING_SUPPORT
/*****************************************************************************
 *                          genSetScopeInfo
 *
 * Called for every scope info piece to record by the main genSetScopeInfo()
 */

void CodeGen::genSetScopeInfo(unsigned            which,
                              UNATIVE_OFFSET      startOffs,
                              UNATIVE_OFFSET      length,
                              unsigned            varNum,
                              unsigned            LVnum,
                              bool                avail,
                              Compiler::siVarLoc& varLoc)
{
    /* We need to do some mapping while reporting back these variables */

    unsigned ilVarNum = compiler->compMap2ILvarNum(varNum);
    noway_assert((int)ilVarNum != ICorDebugInfo::UNKNOWN_ILNUM);

    VarName name = nullptr;

#ifdef DEBUG

    for (unsigned scopeNum = 0; scopeNum < compiler->info.compVarScopesCount; scopeNum++)
    {
        if (LVnum == compiler->info.compVarScopes[scopeNum].vsdLVnum)
        {
            name = compiler->info.compVarScopes[scopeNum].vsdName;
        }
    }

    // Hang on to this compiler->info.

    TrnslLocalVarInfo& tlvi = genTrnslLocalVarInfo[which];

    tlvi.tlviVarNum    = ilVarNum;
    tlvi.tlviLVnum     = LVnum;
    tlvi.tlviName      = name;
    tlvi.tlviStartPC   = startOffs;
    tlvi.tlviLength    = length;
    tlvi.tlviAvailable = avail;
    tlvi.tlviVarLoc    = varLoc;

#endif // DEBUG

    compiler->eeSetLVinfo(which, startOffs, length, ilVarNum, LVnum, name, avail, varLoc);
}
#endif // DEBUGGING_SUPPORT

#endif // _TARGET_ARM_

#endif // !LEGACY_BACKEND
