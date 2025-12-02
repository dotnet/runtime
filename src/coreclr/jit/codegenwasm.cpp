// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"
#include "fgwasm.h"

void CodeGen::genMarkLabelsForCodegen()
{
    // TODO-WASM: serialize fgWasmControlFlow results into codegen-level metadata/labels
    // (or use them directly and leave this empty).
}

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif // DEBUG

    ScopedSetVariable<bool> _setGeneratingEpilog(&compiler->compGeneratingEpilog, true);

#ifdef DEBUG
    if (compiler->opts.dspCode)
        printf("\n__epilog:\n");
#endif // DEBUG

    bool jmpEpilog = block->HasFlag(BBF_HAS_JMP);

    if (jmpEpilog)
    {
        NYI_WASM("genFnEpilog: jmpEpilog");
    }

    // TODO-WASM: shadow stack maintenance
    // TODO-WASM-CQ: do not emit "return" in case this is the last block

    instGen(INS_return);
}

void CodeGen::genCaptureFuncletPrologEpilogInfo()
{
}

void CodeGen::genFuncletProlog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletProlog()\n");
    }
#endif

    NYI_WASM("genFuncletProlog");
}

void CodeGen::genFuncletEpilog()
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFuncletEpilog()\n");
    }
#endif

    NYI_WASM("genFuncletEpilog");
}

//------------------------------------------------------------------------
// getBlockIndex: return the index of this block in the linear block
//   order
//
// Arguments:
//   block - block in question
//
// Returns:
//   index of block
//
static unsigned getBlockIndex(BasicBlock* block)
{
    return block->bbPreorderNum;
}

//------------------------------------------------------------------------
// findTargetDepth: find the depth of a target block in the wasm control flow stack
//
// Arguments:
//   targetBlock - block to branch to
//   (implicit) compCurBB -- block to branch from
//
// Returns:
//   depth of target block in control stack
//
unsigned CodeGen::findTargetDepth(BasicBlock* targetBlock)
{
    BasicBlock* const sourceBlock = compiler->compCurBB;
    int const         h           = wasmControlFlowStack->Height();

    const unsigned targetIndex = getBlockIndex(targetBlock);
    const unsigned sourceIndex = getBlockIndex(sourceBlock);
    const bool     isBackedge  = targetIndex <= sourceIndex;

    for (int i = 0; i < h; i++)
    {
        WasmInterval* const ii    = wasmControlFlowStack->Top(i);
        unsigned            match = 0;

        if (isBackedge)
        {
            // loops bind to start
            match = ii->Start();
        }
        else
        {
            // blocks bind to end
            match = ii->End();
        }

        if ((match == targetIndex) && (isBackedge == ii->IsLoop()))
        {
            return i;
        }
    }

    JITDUMP("Could not find " FMT_BB "[%u]%s in active control stack\n", targetBlock->bbNum, targetIndex,
            isBackedge ? " (backedge)" : "");
    assert(!"Can't find target in control stack");

    return ~0;
}

//------------------------------------------------------------------------
// genEmitStartBlock: prepare for codegen in a block
//
// Arguments:
//   block - block to prepare for
//
// Notes:
//   Updates the wasm control flow stack
//
void CodeGen::genEmitStartBlock(BasicBlock* block)
{
    const unsigned cursor = getBlockIndex(block);

    // Pop control flow intervals that end here (at most two, block and/or loop)
    // and emit wasm END instructions for them.
    //
    while (!wasmControlFlowStack->Empty() && (wasmControlFlowStack->Top()->End() == cursor))
    {
        instGen(INS_end);
        wasmControlFlowStack->Pop();
    }

    // Push control flow for intervals that start here or earlier, and emit
    // Wasm BLOCK or LOOP instruction
    //
    if (wasmCursor < compiler->fgWasmIntervals->size())
    {
        WasmInterval* interval = compiler->fgWasmIntervals->at(wasmCursor);
        WasmInterval* chain    = interval->Chain();

        while (chain->Start() <= cursor)
        {
            if (interval->IsLoop())
            {
                instGen(INS_loop);
            }
            else
            {
                instGen(INS_block);
            }

            wasmCursor++;
            wasmControlFlowStack->Push(interval);

            if (wasmCursor >= compiler->fgWasmIntervals->size())
            {
                break;
            }

            interval = compiler->fgWasmIntervals->at(wasmCursor);
            chain    = interval->Chain();
        }
    }
}

//------------------------------------------------------------------------
// genEmitEndBlock: finish up codegen in a block
//
// Arguments:
//   block - block to finish up
//
// Returns:
//   Updated block to process (if not block->Next()) or nullptr.
//
BasicBlock* CodeGen::genEmitEndBlock(BasicBlock* block)
{
    // Generate Wasm control flow instructions for block ends
    //
    switch (block->GetKind())
    {
        case BBJ_RETURN:
            genExitCode(block);
            break;

        case BBJ_THROW:
            // TODO-WASM-CQ: only emit this when needed.
            instGen(INS_unreachable);
            break;

        case BBJ_CALLFINALLY:
            // TODO-WASM: EH model
            genCallFinally(block);
            if (!block->isBBCallFinallyPair())
            {
                instGen(INS_unreachable);
            }
            break;

        case BBJ_EHCATCHRET:
            // TODO-WASM: EH model
            genEHCatchRet(block);
            FALLTHROUGH;

        case BBJ_EHFINALLYRET:
        case BBJ_EHFAULTRET:
        case BBJ_EHFILTERRET:
            // TODO-WASM: EH model
            genReserveFuncletEpilog(block);
            break;

        case BBJ_SWITCH:
        {
            BBswtDesc* const desc      = block->GetSwitchTargets();
            unsigned const   caseCount = desc->GetCaseCount();

            assert(!desc->HasDefaultCase());

            if (caseCount == 0)
            {
                break;
            }

            GetEmitter()->emitIns_I(INS_br_table, EA_4BYTE, caseCount);

            for (unsigned caseNum = 0; caseNum < caseCount; caseNum++)
            {
                BasicBlock* const caseTarget = desc->GetCase(caseNum)->getDestinationBlock();
                unsigned          depth      = findTargetDepth(caseTarget);

                GetEmitter()->emitIns_I(INS_label, EA_4BYTE, depth);
            }

            JITDUMP("\n");
            break;
        }

        case BBJ_CALLFINALLYRET:
        case BBJ_ALWAYS:
        {
            BasicBlock* const target = block->GetTarget();

            // If this block jumps to the next one, we might be able to skip emitting the jump
            //
            if (block->CanRemoveJumpToNext(compiler))
            {
                assert(getBlockIndex(target) == (getBlockIndex(block) + 1));
                break;
            }

            inst_JMP(EJ_jmp, target);
            break;
        }

        case BBJ_COND:
        {
            BasicBlock* const trueTarget  = block->GetTrueTarget();
            BasicBlock* const falseTarget = block->GetFalseTarget();

            // We don't expect degenerate BBJ_COND
            //
            assert(trueTarget != falseTarget);

            // We don't expect the true target to be the next block.
            //
            assert(trueTarget != block->Next());

            // br_if for true target
            //
            inst_JMP(EJ_jmpif, trueTarget);

            // br for false target, if not fallthrough
            //
            if (falseTarget == block->Next())
            {
                break;
            }
            inst_JMP(EJ_jmp, falseTarget);

            break;
        }

        default:
            noway_assert(!"Unexpected bbKind");
            break;
    }

    // Indicate codgen should just proceed to the next block.
    //
    return nullptr;
}

//------------------------------------------------------------------------
// genCodeForTreeNode: codegen for a particular tree node
//
// Arguments:
//   treeNode - node to generate code for
//
void CodeGen::genCodeForTreeNode(GenTree* treeNode)
{
#ifdef DEBUG
    lastConsumedNode = nullptr;
    if (compiler->verbose)
    {
        compiler->gtDispLIRNode(treeNode, "Generating: ");
    }
#endif // DEBUG

    assert(!treeNode->IsReuseRegVal()); // TODO-WASM-CQ: enable.

    // Contained nodes are part of the parent for codegen purposes.
    if (treeNode->isContained())
    {
        return;
    }

    switch (treeNode->OperGet())
    {
        case GT_ADD:
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            genCodeForCompare(treeNode->AsOp());
            break;

        case GT_LCL_VAR:
            genCodeForLclVar(treeNode->AsLclVar());
            break;

        case GT_JTRUE:
            // Codegen handled by genEmitEndBlock
            genConsumeOperands(treeNode->AsOp());
            break;

        case GT_RETURN:
            genReturn(treeNode);
            break;

        case GT_IL_OFFSET:
            // Do nothing; this node is a marker for debug info.
            break;

        default:
#ifdef DEBUG
            NYIRAW(GenTree::OpName(treeNode->OperGet()));
#else
            NYI_WASM("Opcode not implemented");
#endif
            break;
    }
}

//------------------------------------------------------------------------
// PackOperAndType: Pack a genTreeOps and var_types into a uint32_t
//
// Arguments:
//    oper - a genTreeOps to pack
//    type - a var_types to pack
//
// Return Value:
//    oper and type packed into an integer that can be used as a switch value/case
//
static constexpr uint32_t PackOperAndType(genTreeOps oper, var_types type)
{
    if (type == TYP_BYREF)
    {
        type = TYP_I_IMPL;
    }
    static_assert((ssize_t)GT_COUNT > (ssize_t)TYP_COUNT);
    return ((uint32_t)oper << (ConstLog2<GT_COUNT>::value + 1)) | ((uint32_t)type);
}

//------------------------------------------------------------------------
// PackOperAndType: Pack a GenTreeOp* into a uint32_t
//
// Arguments:
//    treeNode - a GenTreeOp to extract oper and type from
//
// Return Value:
//    the node's oper and type packed into an integer that can be used as a switch value
//
static uint32_t PackOperAndType(GenTreeOp* treeNode)
{
    return PackOperAndType(treeNode->OperGet(), treeNode->TypeGet());
}

//------------------------------------------------------------------------
// genCodeForBinary: Generate code for a binary arithmetic operator
//
// Arguments:
//    treeNode - The binary operation for which we are generating code.
//
void CodeGen::genCodeForBinary(GenTreeOp* treeNode)
{
    genConsumeOperands(treeNode);

    instruction ins;
    switch (PackOperAndType(treeNode))
    {
        case PackOperAndType(GT_ADD, TYP_INT):
            ins = INS_i32_add;
            break;
        case PackOperAndType(GT_ADD, TYP_LONG):
            ins = INS_i64_add;
            break;
        case PackOperAndType(GT_ADD, TYP_FLOAT):
            ins = INS_f32_add;
            break;
        case PackOperAndType(GT_ADD, TYP_DOUBLE):
            ins = INS_f64_add;
            break;
        default:
            ins = INS_none;
            NYI_WASM("genCodeForBinary");
            break;
    }

    GetEmitter()->emitIns(ins);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForLclVar: Produce code for a GT_LCL_VAR node.
//
// Arguments:
//    tree - the GT_LCL_VAR node
//
void CodeGen::genCodeForLclVar(GenTreeLclVar* tree)
{
    assert(tree->OperIs(GT_LCL_VAR) && !tree->IsMultiReg());
    LclVarDsc* varDsc = compiler->lvaGetDesc(tree);

    // Unlike other targets, we can't "reload at the point of use", since that would require inserting instructions
    // into the middle of an already-emitted instruction group. Instead, we order the nodes in a way that obeys the
    // value stack constraints of WASM precisely. However, the liveness tracking is done in the same way as for other
    // targets, hence "genProduceReg" is only called for non-candidates.
    if (!varDsc->lvIsRegCandidate())
    {
        var_types type = varDsc->GetRegisterType(tree);
        // TODO-WASM: actually local.get the frame base local here.
        GetEmitter()->emitIns_S(ins_Load(type), emitTypeSize(tree), tree->GetLclNum(), 0);
        genProduceReg(tree);
    }
    else
    {
        assert(genIsValidReg(varDsc->GetRegNum()));
        unsigned wasmLclIndex = UnpackWasmReg(varDsc->GetRegNum());
        GetEmitter()->emitIns_I(INS_local_get, emitTypeSize(tree), wasmLclIndex);
    }
}

//------------------------------------------------------------------------
// genCodeForCompare: Produce code for a GT_EQ/GT_NE/GT_LT/GT_LE/GT_GE/GT_GT node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForCompare(GenTreeOp* tree)
{
    assert(tree->OperIsCmpCompare());

    GenTree* const  op1     = tree->gtGetOp1();
    var_types const op1Type = op1->TypeGet();

    if (varTypeIsFloating(op1Type))
    {
        genCompareFloat(tree);
    }
    else
    {
        genCompareInt(tree);
    }
}

//------------------------------------------------------------------------
// genCompareInt: Generate code for comparing ints or longs
//
// Arguments:
//    treeNode - the compare tree
//
void CodeGen::genCompareInt(GenTreeOp* treeNode)
{
    assert(treeNode->OperIsCmpCompare());
    genConsumeOperands(treeNode);

    instruction ins;
    switch (PackOperAndType(treeNode->OperGet(), genActualType(treeNode->gtGetOp1()->TypeGet())))
    {
        case PackOperAndType(GT_EQ, TYP_INT):
            ins = INS_i32_eq;
            break;
        case PackOperAndType(GT_EQ, TYP_LONG):
            ins = INS_i64_eq;
            break;
        case PackOperAndType(GT_NE, TYP_INT):
            ins = INS_i32_ne;
            break;
        case PackOperAndType(GT_NE, TYP_LONG):
            ins = INS_i64_ne;
            break;
        case PackOperAndType(GT_LT, TYP_INT):
            ins = treeNode->IsUnsigned() ? INS_i32_lt_u : INS_i32_lt_s;
            break;
        case PackOperAndType(GT_LT, TYP_LONG):
            ins = treeNode->IsUnsigned() ? INS_i64_lt_u : INS_i64_lt_s;
            break;
        case PackOperAndType(GT_LE, TYP_INT):
            ins = treeNode->IsUnsigned() ? INS_i32_le_u : INS_i32_le_s;
            break;
        case PackOperAndType(GT_LE, TYP_LONG):
            ins = treeNode->IsUnsigned() ? INS_i64_le_u : INS_i64_le_s;
            break;
        case PackOperAndType(GT_GE, TYP_INT):
            ins = treeNode->IsUnsigned() ? INS_i32_ge_u : INS_i32_ge_s;
            break;
        case PackOperAndType(GT_GE, TYP_LONG):
            ins = treeNode->IsUnsigned() ? INS_i64_ge_u : INS_i64_ge_s;
            break;
        case PackOperAndType(GT_GT, TYP_INT):
            ins = treeNode->IsUnsigned() ? INS_i32_gt_u : INS_i32_gt_s;
            break;
        case PackOperAndType(GT_GT, TYP_LONG):
            ins = treeNode->IsUnsigned() ? INS_i64_gt_u : INS_i64_gt_s;
            break;
        default:
            unreached();
    }

    GetEmitter()->emitIns(ins);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCompareFloat: Generate code for comparing floats
//
// Arguments:
//    treeNode - the compare tree
//
void CodeGen::genCompareFloat(GenTreeOp* treeNode)
{
    assert(treeNode->OperIsCmpCompare());

    if ((treeNode->gtFlags & GTF_RELOP_NAN_UN) != 0)
    {
        NYI_WASM("genCompareFloat: unordered compares");
    }

    genConsumeOperands(treeNode);

    instruction ins;
    switch (PackOperAndType(treeNode->OperGet(), treeNode->gtOp1->TypeGet()))
    {
        case PackOperAndType(GT_EQ, TYP_FLOAT):
            ins = INS_f32_eq;
            break;
        case PackOperAndType(GT_EQ, TYP_DOUBLE):
            ins = INS_f64_eq;
            break;
        case PackOperAndType(GT_NE, TYP_FLOAT):
            ins = INS_f32_ne;
            break;
        case PackOperAndType(GT_NE, TYP_DOUBLE):
            ins = INS_f64_ne;
            break;
        case PackOperAndType(GT_LT, TYP_FLOAT):
            ins = INS_f32_lt;
            break;
        case PackOperAndType(GT_LT, TYP_DOUBLE):
            ins = INS_f64_lt;
            break;
        case PackOperAndType(GT_LE, TYP_FLOAT):
            ins = INS_f32_le;
            break;
        case PackOperAndType(GT_LE, TYP_DOUBLE):
            ins = INS_f64_le;
            break;
        case PackOperAndType(GT_GE, TYP_FLOAT):
            ins = INS_f32_ge;
            break;
        case PackOperAndType(GT_GE, TYP_DOUBLE):
            ins = INS_f64_ge;
            break;
        case PackOperAndType(GT_GT, TYP_FLOAT):
            ins = INS_f32_gt;
            break;
        case PackOperAndType(GT_GT, TYP_DOUBLE):
            ins = INS_f64_gt;
            break;
        default:
            unreached();
    }

    GetEmitter()->emitIns(ins);
    genProduceReg(treeNode);
}

BasicBlock* CodeGen::genCallFinally(BasicBlock* block)
{
    assert(block->KindIs(BBJ_CALLFINALLY));
    NYI_WASM("genCallFinally");
    return nullptr;
}

void CodeGen::genEHCatchRet(BasicBlock* block)
{
    NYI_WASM("genEHCatchRet");
}

void CodeGen::genStructReturn(GenTree* treeNode)
{
    NYI_WASM("genStructReturn");
}

void CodeGen::genEmitGSCookieCheck(bool tailCall)
{
    // TODO-WASM: GS cookie checks have limited utility on WASM since they can only help
    // with detecting linear memory stack corruption. Decide if we want them anyway.
    NYI_WASM("genEmitGSCookieCheck");
}

void CodeGen::genProfilingLeaveCallback(unsigned helper)
{
    NYI_WASM("genProfilingLeaveCallback");
}

void CodeGen::genSpillVar(GenTree* tree)
{
    NYI_WASM("Put all spillng to memory under '#if HAS_FIXED_REGISTER_SET'");
}

//------------------------------------------------------------------------
// inst_JMP: Emit a jump instruction.
//
// Arguments:
//   jmp      - kind of jump to emit
//   tgtBlock - target of the jump
//
void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
    instruction    instr = emitter::emitJumpKindToIns(jmp);
    unsigned const depth = findTargetDepth(tgtBlock);
    GetEmitter()->emitIns_I(instr, EA_4BYTE, depth);
}

void CodeGen::genCreateAndStoreGCInfo(unsigned codeSize, unsigned prologSize, unsigned epilogSize DEBUGARG(void* code))
{
    // GCInfo not captured/created by codegen.
}

void CodeGen::genReportEH()
{
    // EHInfo not captured/created by codegen.
}

//---------------------------------------------------------------------
// genTotalFrameSize - return the total size of the linear memory stack frame.
//
// Return value:
//    Total linear memory frame size
//
int CodeGenInterface::genTotalFrameSize() const
{
    assert(compiler->compLclFrameSize >= 0);
    return compiler->compLclFrameSize;
}

//---------------------------------------------------------------------
// genSPtoFPdelta - return the offset from SP to the frame pointer.
// This number is going to be positive, since SP must be at the lowest
// address.
//
// There must be a frame pointer to call this function!
int CodeGenInterface::genSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    NYI_WASM("genSPtoFPdelta");
    return 0;
}

//---------------------------------------------------------------------
// genCallerSPtoFPdelta - return the offset from Caller-SP to the frame pointer.
// This number is going to be negative, since the Caller-SP is at a higher
// address than the frame pointer.
//
// There must be a frame pointer to call this function!
int CodeGenInterface::genCallerSPtoFPdelta() const
{
    assert(isFramePointerUsed());
    NYI_WASM("genCallerSPtoFPdelta");
    return 0;
}

//---------------------------------------------------------------------
// genCallerSPtoInitialSPdelta - return the offset from Caller-SP to Initial SP.
//
// This number will be negative.
int CodeGenInterface::genCallerSPtoInitialSPdelta() const
{
    NYI_WASM("genCallerSPtoInitialSPdelta");
    return 0;
}

void CodeGenInterface::genUpdateVarReg(LclVarDsc* varDsc, GenTree* tree, int regIndex)
{
    NYI_WASM("Move genUpdateVarReg from codegenlinear.cpp to codegencommon.cpp shared code");
}

void RegSet::verifyRegUsed(regNumber reg)
{
}
