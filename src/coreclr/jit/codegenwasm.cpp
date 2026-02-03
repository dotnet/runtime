// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"
#include "fgwasm.h"

#ifdef TARGET_64BIT
static const instruction INS_I_const = INS_i64_const;
static const instruction INS_I_add   = INS_i64_add;
static const instruction INS_I_sub   = INS_i64_sub;
static const instruction INS_I_le_u  = INS_i64_le_u;
static const instruction INS_I_gt_u  = INS_i64_gt_u;
#else  // !TARGET_64BIT
static const instruction INS_I_const = INS_i32_const;
static const instruction INS_I_add   = INS_i32_add;
static const instruction INS_I_sub   = INS_i32_sub;
static const instruction INS_I_le_u  = INS_i32_le_u;
static const instruction INS_I_gt_u  = INS_i32_gt_u;
#endif // !TARGET_64BIT

void CodeGen::genMarkLabelsForCodegen()
{
    // No work needed here for now.
    // We mark labels as needed in genEmitStartBlock.
}

//------------------------------------------------------------------------
// genBeginFnProlog: generate wasm local declarations
//
// TODO-WASM: pre-declare all "register" locals
void CodeGen::genBeginFnProlog()
{
    // TODO-WASM: proper local count, local declarations, and shadow stack maintenance
    GetEmitter()->emitIns_I(INS_local_cnt, EA_8BYTE, 0);
}

//------------------------------------------------------------------------
// genPushCalleeSavedRegisters: no-op since we don't need to save anything.
//
void CodeGen::genPushCalleeSavedRegisters()
{
}

//------------------------------------------------------------------------
// genAllocLclFrame: initialize the SP and FP locals.
//
// Arguments:
//    frameSize         - Size of the frame to establish
//    initReg           - Unused
//    pInitRegZeroed    - Unused
//    maskArgRegsLiveIn - Unused
//
void CodeGen::genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn)
{
    assert(compiler->compGeneratingProlog);
    regNumber spReg = GetStackPointerReg();
    if (spReg == REG_NA)
    {
        assert(!isFramePointerUsed());
        return;
    }

    // TODO-WASM: reverse pinvoke frame allocation
    //
    if (compiler->lvaWasmSpArg == BAD_VAR_NUM)
    {
        NYI_WASM("alloc local frame for reverse pinvoke");
    }

    unsigned initialSPLclIndex =
        WasmRegToIndex(compiler->lvaGetParameterABIInfo(compiler->lvaWasmSpArg).Segment(0).GetRegister());
    unsigned spLclIndex = WasmRegToIndex(spReg);
    assert(initialSPLclIndex == spLclIndex);
    if (frameSize != 0)
    {
        GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, initialSPLclIndex);
        GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, frameSize);
        GetEmitter()->emitIns(INS_I_sub);
        GetEmitter()->emitIns_I(INS_local_set, EA_PTRSIZE, spLclIndex);
    }
    regNumber fpReg = GetFramePointerReg();
    if ((fpReg != REG_NA) && (fpReg != spReg))
    {
        GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, spLclIndex);
        GetEmitter()->emitIns_I(INS_local_set, EA_PTRSIZE, WasmRegToIndex(fpReg));
    }
}

//------------------------------------------------------------------------
// genEnregisterOSRArgsAndLocals: enregister OSR args and locals.
//
void CodeGen::genEnregisterOSRArgsAndLocals()
{
    unreached(); // OSR not supported on WASM.
}

//------------------------------------------------------------------------
// genHomeRegisterParams: place register arguments into their RA-assigned locations.
//
// For the WASM RA, we have a much simplified (compared to LSRA) contract of:
// - If an argument is live on entry in a set of registers, then the RA will
//   assign those registers to that argument on entry.
// This means we never need to do any copying or cycle resolution here.
//
// The main motivation for this (along with the obvious CQ implications) is
// obviating the need to adapt the general "RegGraph"-based algorithm to
// !HAS_FIXED_REGISTER_SET constraints (no reg masks).
//
// Arguments:
//    initReg            - Unused
//    initRegStillZeroed - Unused
//
void CodeGen::genHomeRegisterParams(regNumber initReg, bool* initRegStillZeroed)
{
    JITDUMP("*************** In genHomeRegisterParams()\n");

    auto spillParam = [this](unsigned lclNum, unsigned offset, unsigned paramLclNum, const ABIPassingSegment& segment) {
        assert(segment.IsPassedInRegister());

        LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);
        if (varDsc->lvTracked && !VarSetOps::IsMember(compiler, compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            return;
        }

        if (varDsc->lvOnFrame && (!varDsc->lvIsInReg() || varDsc->lvLiveInOutOfHndlr))
        {
            LclVarDsc* paramVarDsc = compiler->lvaGetDesc(paramLclNum);
            var_types  storeType   = genParamStackType(paramVarDsc, segment);
            if (!varDsc->TypeIs(TYP_STRUCT) && (genTypeSize(genActualType(varDsc)) < genTypeSize(storeType)))
            {
                // Can happen for struct fields due to padding.
                storeType = genActualType(varDsc);
            }

            GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(storeType),
                                    WasmRegToIndex(segment.GetRegister()));
            GetEmitter()->emitIns_S(ins_Store(storeType), emitActualTypeSize(storeType), lclNum, offset);
        }

        if (varDsc->lvIsInReg())
        {
            assert(varDsc->GetRegNum() == segment.GetRegister());
        }
    };

    for (unsigned lclNum = 0; lclNum < compiler->info.compArgsCount; lclNum++)
    {
        LclVarDsc*                   lclDsc  = compiler->lvaGetDesc(lclNum);
        const ABIPassingInformation& abiInfo = compiler->lvaGetParameterABIInfo(lclNum);

        for (const ABIPassingSegment& segment : abiInfo.Segments())
        {
            if (!segment.IsPassedInRegister())
            {
                continue;
            }

            const ParameterRegisterLocalMapping* mapping =
                compiler->FindParameterRegisterLocalMappingByRegister(segment.GetRegister());

            bool spillToBaseLocal = true;
            if (mapping != nullptr)
            {
                spillParam(mapping->LclNum, mapping->Offset, lclNum, segment);

                // If home is shared with base local, then skip spilling to the base local.
                if (lclDsc->lvPromoted)
                {
                    spillToBaseLocal = false;
                }
            }

            if (spillToBaseLocal)
            {
                spillParam(lclNum, segment.Offset, lclNum, segment);
            }
        }
    }
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
    // TODO-WASM: we need to handle the end-of-function case if we reach the end of a codegen for a function
    // and do NOT have an epilog. In those cases we currently will not emit an end instruction.
    if (block->IsLast() || compiler->bbIsFuncletBeg(block->Next()))
    {
        instGen(INS_end);
    }
    else
    {
        instGen(INS_return);
    }
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
        WasmInterval* interval = wasmControlFlowStack->Pop();
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

            if (interval->IsLoop())
            {
                if (!block->HasFlag(BBF_HAS_LABEL))
                {
                    block->SetFlags(BBF_HAS_LABEL);
                    genDefineTempLabel(block);
                }
            }
            else
            {
                BasicBlock* const endBlock = compiler->fgIndexToBlockMap[interval->End()];

                if (!endBlock->HasFlag(BBF_HAS_LABEL))
                {
                    endBlock->SetFlags(BBF_HAS_LABEL);
                    genDefineTempLabel(endBlock);
                }
            }

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
        case GT_SUB:
        case GT_MUL:
        case GT_OR:
        case GT_XOR:
        case GT_AND:
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_DIV:
        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
            genCodeForDivMod(treeNode->AsOp());
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
            genCodeForShift(treeNode);
            break;

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
            genCodeForCompare(treeNode->AsOp());
            break;

        case GT_LCL_ADDR:
            genCodeForLclAddr(treeNode->AsLclFld());
            break;

        case GT_LCL_FLD:
            genCodeForLclFld(treeNode->AsLclFld());
            break;

        case GT_LCL_VAR:
            genCodeForLclVar(treeNode->AsLclVar());
            break;

        case GT_STORE_LCL_VAR:
            genCodeForStoreLclVar(treeNode->AsLclVar());
            break;

        case GT_JTRUE:
            genCodeForJTrue(treeNode->AsOp());
            break;

        case GT_SWITCH:
            genTableBasedSwitch(treeNode);
            break;

        case GT_RETURN:
            genReturn(treeNode);
            break;

        case GT_IL_OFFSET:
            // Do nothing; this node is a marker for debug info.
            break;

        case GT_NOP:
            break;

        case GT_NO_OP:
            instGen(INS_nop);
            break;

        case GT_CNS_INT:
        case GT_CNS_LNG:
        case GT_CNS_DBL:
            genCodeForConstant(treeNode);
            break;

        case GT_CAST:
            genCodeForCast(treeNode->AsOp());
            break;

        case GT_NEG:
        case GT_NOT:
            genCodeForNegNot(treeNode->AsOp());
            break;

        case GT_NULLCHECK:
            genCodeForNullCheck(treeNode->AsIndir());
            break;

        case GT_IND:
            genCodeForIndir(treeNode->AsIndir());
            break;

        case GT_STOREIND:
            genCodeForStoreInd(treeNode->AsStoreInd());
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
// genCodeForJTrue: emit Wasm br_if
//
// Arguments:
//    treeNode - predicate value
//
void CodeGen::genCodeForJTrue(GenTreeOp* jtrue)
{
    BasicBlock* const block = compiler->compCurBB;
    assert(block->KindIs(BBJ_COND));

    genConsumeOperands(jtrue);

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
    if (falseTarget != block->Next())
    {
        inst_JMP(EJ_jmp, falseTarget);
    }
}

//------------------------------------------------------------------------
// genTableBasedSwitch: emit Wasm br_table
//
// Arguments:
//    treeNode - value to switch on
//
void CodeGen::genTableBasedSwitch(GenTree* treeNode)
{
    BasicBlock* const block = compiler->compCurBB;
    assert(block->KindIs(BBJ_SWITCH));

    genConsumeOperands(treeNode->AsOp());

    BBswtDesc* const desc      = block->GetSwitchTargets();
    unsigned const   caseCount = desc->GetCaseCount();

    // We don't expect degenerate or default-less switches
    //
    assert(caseCount > 0);
    assert(desc->HasDefaultCase());

    // br_table list (labelidx*) labelidx
    // list is prefixed with length, which is caseCount - 1
    //
    GetEmitter()->emitIns_I(INS_br_table, EA_4BYTE, caseCount - 1);

    // Emit the list case targets, then default case target
    // (which is always the last case in the desc).
    //
    for (unsigned caseNum = 0; caseNum < caseCount; caseNum++)
    {
        BasicBlock* const caseTarget = desc->GetCase(caseNum)->getDestinationBlock();
        unsigned          depth      = findTargetDepth(caseTarget);

        GetEmitter()->emitIns_J(INS_label, EA_4BYTE, depth, caseTarget);
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
    if ((type == TYP_BYREF) || (type == TYP_REF))
    {
        type = TYP_I_IMPL;
    }
    const int shift1 = ConstLog2<TYP_COUNT>::value + 1;
    return ((uint32_t)oper << shift1) | ((uint32_t)type);
}

// ------------------------------------------------------------------------
// PackTypes: Pack two var_types together into a uint32_t

// Arguments:
//    toType - a var_types to pack
//    fromType - a var_types to pack
//
// Return Value:
//    The two types packed together into an integer that can be used as a switch/value,
//    the primary use case being the handling of operations with two-type variants such
//    as casts.
//
static constexpr uint32_t PackTypes(var_types toType, var_types fromType)
{
    if (toType == TYP_BYREF || toType == TYP_REF)
    {
        toType = TYP_I_IMPL;
    }
    if (fromType == TYP_BYREF || fromType == TYP_REF)
    {
        fromType = TYP_I_IMPL;
    }
    const int shift1 = ConstLog2<TYP_COUNT>::value + 1;
    return ((uint32_t)toType) | ((uint32_t)fromType << shift1);
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer to integer cast
//
// Arguments:
//    cast - The GT_CAST node for the integer cast operation
//
// Notes:
//    Handles casts to and from small int, int, and long types
//    including proper sign extension and truncation as needed.
//
void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    if (cast->gtOverflow())
    {
        NYI_WASM("Overflow checks");
    }

    GenIntCastDesc desc(cast);
    var_types      toType     = genActualType(cast->CastToType());
    var_types      fromType   = genActualType(cast->CastOp());
    int            extendSize = desc.ExtendSrcSize();
    instruction    ins        = INS_none;
    assert(fromType == TYP_INT || fromType == TYP_LONG);

    genConsumeOperands(cast);

    // TODO-WASM: Handle load containment GenIntCastDesc::LOAD_* cases once we mark containment for loads
    switch (desc.ExtendKind())
    {
        case GenIntCastDesc::COPY:
        {
            if (toType == TYP_INT && fromType == TYP_LONG)
            {
                ins = INS_i32_wrap_i64;
            }
            else
            {
                assert(toType == fromType);
                ins = INS_none;
            }
            break;
        }
        case GenIntCastDesc::ZERO_EXTEND_SMALL_INT:
        {
            int andAmount = extendSize == 1 ? 255 : 65535;
            if (fromType == TYP_LONG)
            {
                GetEmitter()->emitIns(INS_i32_wrap_i64);
            }
            GetEmitter()->emitIns_I(INS_i32_const, EA_4BYTE, andAmount);
            ins = INS_i32_and;
            break;
        }
        case GenIntCastDesc::SIGN_EXTEND_SMALL_INT:
        {
            if (fromType == TYP_LONG)
            {
                GetEmitter()->emitIns(INS_i32_wrap_i64);
            }
            ins = (extendSize == 1) ? INS_i32_extend8_s : INS_i32_extend16_s;

            break;
        }
        case GenIntCastDesc::ZERO_EXTEND_INT:
        {
            ins = INS_i64_extend_u_i32;
            break;
        }
        case GenIntCastDesc::SIGN_EXTEND_INT:
        {
            ins = INS_i64_extend_s_i32;
            break;
        }
        default:
            unreached();
    }

    if (ins != INS_none)
    {
        GetEmitter()->emitIns(ins);
    }
    genProduceReg(cast);
}

//------------------------------------------------------------------------
// genFloatToIntCast: Generate code for a floating point to integer cast
//
// Arguments:
//    tree - The GT_CAST node for the float-to-int cast operation
//
// Notes:
//    Handles casts from TYP_FLOAT/TYP_DOUBLE to TYP_INT/TYP_LONG.
//    Uses saturating truncation instructions (trunc_sat) which clamp
//    out-of-range values rather than trapping.
//
void CodeGen::genFloatToIntCast(GenTree* tree)
{
    if (tree->gtOverflow())
    {
        NYI_WASM("Overflow checks");
    }

    var_types   toType     = tree->TypeGet();
    var_types   fromType   = tree->AsCast()->CastOp()->TypeGet();
    bool        isUnsigned = varTypeIsUnsigned(tree->AsCast()->CastToType());
    instruction ins        = INS_none;
    assert(varTypeIsFloating(fromType) && (toType == TYP_INT || toType == TYP_LONG));

    genConsumeOperands(tree->AsCast());

    switch (PackTypes(fromType, toType))
    {
        case PackTypes(TYP_FLOAT, TYP_INT):
            ins = isUnsigned ? INS_i32_trunc_sat_f32_u : INS_i32_trunc_sat_f32_s;
            break;
        case PackTypes(TYP_DOUBLE, TYP_INT):
            ins = isUnsigned ? INS_i32_trunc_sat_f64_u : INS_i32_trunc_sat_f64_s;
            break;
        case PackTypes(TYP_FLOAT, TYP_LONG):
            ins = isUnsigned ? INS_i64_trunc_sat_f32_u : INS_i64_trunc_sat_f32_s;
            break;
        case PackTypes(TYP_DOUBLE, TYP_LONG):
            ins = isUnsigned ? INS_i64_trunc_sat_f64_u : INS_i64_trunc_sat_f64_s;
            break;
        default:
            unreached();
    }

    GetEmitter()->emitIns(ins);
    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genIntToFloatCast: Generate code for an integer to floating point cast
//
// Arguments:
//    tree - The GT_CAST node for the int-to-float cast operation
//
// Notes:
//    Handles casts from TYP_INT/TYP_LONG to TYP_FLOAT/TYP_DOUBLE.
//    Currently not implemented (NYI_WASM).
//
void CodeGen::genIntToFloatCast(GenTree* tree)
{
    NYI_WASM("genIntToFloatCast");
}

//------------------------------------------------------------------------
// genFloatToFloatCast: Generate code for a float to float cast
//
// Arguments:
//    tree - The GT_CAST node for the float-to-float cast operation
//
void CodeGen::genFloatToFloatCast(GenTree* tree)
{
    var_types   toType   = tree->TypeGet();
    var_types   fromType = tree->AsCast()->CastOp()->TypeGet();
    instruction ins      = INS_none;

    genConsumeOperands(tree->AsCast());

    switch (PackTypes(toType, fromType))
    {
        case PackTypes(TYP_FLOAT, TYP_DOUBLE):
            ins = INS_f32_demote_f64;
            break;
        case PackTypes(TYP_DOUBLE, TYP_FLOAT):
            ins = INS_f64_promote_f32;
            break;
        case PackTypes(TYP_FLOAT, TYP_FLOAT):
        case PackTypes(TYP_DOUBLE, TYP_DOUBLE):
            ins = INS_none;
            break;
        default:
            unreached();
    }

    if (ins != INS_none)
    {
        GetEmitter()->emitIns(ins);
    }
    genProduceReg(tree);
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
    switch (PackOperAndType(treeNode->OperGet(), treeNode->TypeGet()))
    {
        case PackOperAndType(GT_ADD, TYP_INT):
            if (treeNode->gtOverflow())
                NYI_WASM("Overflow checks");
            ins = INS_i32_add;
            break;
        case PackOperAndType(GT_ADD, TYP_LONG):
            if (treeNode->gtOverflow())
                NYI_WASM("Overflow checks");
            ins = INS_i64_add;
            break;
        case PackOperAndType(GT_ADD, TYP_FLOAT):
            ins = INS_f32_add;
            break;
        case PackOperAndType(GT_ADD, TYP_DOUBLE):
            ins = INS_f64_add;
            break;

        case PackOperAndType(GT_SUB, TYP_INT):
            if (treeNode->gtOverflow())
                NYI_WASM("Overflow checks");
            ins = INS_i32_sub;
            break;
        case PackOperAndType(GT_SUB, TYP_LONG):
            if (treeNode->gtOverflow())
                NYI_WASM("Overflow checks");
            ins = INS_i64_sub;
            break;
        case PackOperAndType(GT_SUB, TYP_FLOAT):
            ins = INS_f32_sub;
            break;
        case PackOperAndType(GT_SUB, TYP_DOUBLE):
            ins = INS_f64_sub;
            break;

        case PackOperAndType(GT_MUL, TYP_INT):
            if (treeNode->gtOverflow())
                NYI_WASM("Overflow checks");
            ins = INS_i32_mul;
            break;
        case PackOperAndType(GT_MUL, TYP_LONG):
            if (treeNode->gtOverflow())
                NYI_WASM("Overflow checks");
            ins = INS_i64_mul;
            break;
        case PackOperAndType(GT_MUL, TYP_FLOAT):
            ins = INS_f32_mul;
            break;
        case PackOperAndType(GT_MUL, TYP_DOUBLE):
            ins = INS_f64_mul;
            break;

        case PackOperAndType(GT_AND, TYP_INT):
            ins = INS_i32_and;
            break;
        case PackOperAndType(GT_AND, TYP_LONG):
            ins = INS_i64_and;
            break;

        case PackOperAndType(GT_OR, TYP_INT):
            ins = INS_i32_or;
            break;
        case PackOperAndType(GT_OR, TYP_LONG):
            ins = INS_i64_or;
            break;

        case PackOperAndType(GT_XOR, TYP_INT):
            ins = INS_i32_xor;
            break;
        case PackOperAndType(GT_XOR, TYP_LONG):
            ins = INS_i64_xor;
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
// genCodeForDivMod: Generate code for a division or modulus operator
//
// Arguments:
//    treeNode - The division or modulus operation for which we are generating code.
//
void CodeGen::genCodeForDivMod(GenTreeOp* treeNode)
{
    genConsumeOperands(treeNode);

    // wasm stack is
    // divisor (top)
    // dividend (next)
    // ...
    // TODO-WASM: To check for exception, we will have to spill these to
    // internal registers along the way, like so:
    //
    // ... push dividend
    // tee.local $temp1
    // ... push divisor
    // tee.local $temp2
    // ... exception checks (using $temp1 and $temp2; will introduce flow)
    // div/mod op

    if (!varTypeIsFloating(treeNode->TypeGet()))
    {
        ExceptionSetFlags exSetFlags = treeNode->OperExceptions(compiler);

        // TODO-WASM:(AnyVal / 0) => DivideByZeroException
        //
        if ((exSetFlags & ExceptionSetFlags::DivideByZeroException) != ExceptionSetFlags::None)
        {
        }

        // TODO-WASM: (MinInt / -1) => ArithmeticException
        //
        if ((exSetFlags & ExceptionSetFlags::ArithmeticException) != ExceptionSetFlags::None)
        {
        }
    }

    instruction ins;
    switch (PackOperAndType(treeNode->OperGet(), treeNode->TypeGet()))
    {
        case PackOperAndType(GT_DIV, TYP_INT):
            ins = INS_i32_div_s;
            break;
        case PackOperAndType(GT_DIV, TYP_LONG):
            ins = INS_i64_div_s;
            break;
        case PackOperAndType(GT_DIV, TYP_FLOAT):
            ins = INS_f32_div;
            break;
        case PackOperAndType(GT_DIV, TYP_DOUBLE):
            ins = INS_f64_div;
            break;

        case PackOperAndType(GT_UDIV, TYP_INT):
            ins = INS_i32_div_u;
            break;
        case PackOperAndType(GT_UDIV, TYP_LONG):
            ins = INS_i64_div_u;
            break;

        case PackOperAndType(GT_MOD, TYP_INT):
            ins = INS_i32_rem_s;
            break;
        case PackOperAndType(GT_MOD, TYP_LONG):
            ins = INS_i64_rem_s;
            break;

        case PackOperAndType(GT_UMOD, TYP_INT):
            ins = INS_i32_rem_u;
            break;
        case PackOperAndType(GT_UMOD, TYP_LONG):
            ins = INS_i64_rem_u;
            break;

        default:
            ins = INS_none;
            NYI_WASM("genCodeForDivMod");
            break;
    }

    GetEmitter()->emitIns(ins);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForConstant: Generate code for an integer or floating point constant
//
// Arguments:
//    treeNode - The constant.
//
void CodeGen::genCodeForConstant(GenTree* treeNode)
{
    instruction    ins;
    cnsval_ssize_t bits;
    var_types      type = treeNode->TypeIs(TYP_REF, TYP_BYREF) ? TYP_I_IMPL : treeNode->TypeGet();
    static_assert(sizeof(cnsval_ssize_t) >= sizeof(double));

    switch (type)
    {
        case TYP_INT:
        {
            ins                      = INS_i32_const;
            GenTreeIntConCommon* con = treeNode->AsIntConCommon();
            bits                     = con->IntegralValue();
            break;
        }
        case TYP_LONG:
        {
            ins                      = INS_i64_const;
            GenTreeIntConCommon* con = treeNode->AsIntConCommon();
            bits                     = con->IntegralValue();
            break;
        }
        case TYP_FLOAT:
        {
            ins                  = INS_f32_const;
            GenTreeDblCon* con   = treeNode->AsDblCon();
            double         value = con->DconValue();
            memcpy(&bits, &value, sizeof(double));
            break;
        }
        case TYP_DOUBLE:
        {
            ins                  = INS_f64_const;
            GenTreeDblCon* con   = treeNode->AsDblCon();
            double         value = con->DconValue();
            memcpy(&bits, &value, sizeof(double));
            break;
        }
        default:
            unreached();
    }

    // The IF_ for the selected instruction, i.e. IF_F64, determines how these bits are emitted
    GetEmitter()->emitIns_I(ins, emitTypeSize(treeNode), bits);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForShift: Generate code for a shift or rotate operator
//
// Arguments:
//    tree - The shift or rotate operation for which we are generating code.
//
void CodeGen::genCodeForShift(GenTree* tree)
{
    assert(tree->OperIsShiftOrRotate());

    GenTreeOp* treeNode = tree->AsOp();
    genConsumeOperands(treeNode);

    // TODO-WASM: Zero-extend the 2nd operand for shifts and rotates as needed when the 1st and 2nd operand are
    // different types. The shift operand width in IR is always TYP_INT; the WASM operations have the same widths
    // for both the shift and shiftee. So the shift may need to be extended (zero-extended) for TYP_LONG.

    instruction ins;
    switch (PackOperAndType(treeNode->OperGet(), treeNode->TypeGet()))
    {
        case PackOperAndType(GT_LSH, TYP_INT):
            ins = INS_i32_shl;
            break;
        case PackOperAndType(GT_LSH, TYP_LONG):
            ins = INS_i64_shl;
            break;

        case PackOperAndType(GT_RSH, TYP_INT):
            ins = INS_i32_shr_s;
            break;
        case PackOperAndType(GT_RSH, TYP_LONG):
            ins = INS_i64_shr_s;
            break;

        case PackOperAndType(GT_RSZ, TYP_INT):
            ins = INS_i32_shr_u;
            break;
        case PackOperAndType(GT_RSZ, TYP_LONG):
            ins = INS_i64_shr_u;
            break;

        case PackOperAndType(GT_ROL, TYP_INT):
            ins = INS_i32_rotl;
            break;
        case PackOperAndType(GT_ROL, TYP_LONG):
            ins = INS_i64_rotl;
            break;

        case PackOperAndType(GT_ROR, TYP_INT):
            ins = INS_i32_rotr;
            break;
        case PackOperAndType(GT_ROR, TYP_LONG):
            ins = INS_i64_rotr;
            break;

        default:
            ins = INS_none;
            NYI_WASM("genCodeForShift");
            break;
    }

    GetEmitter()->emitIns(ins);
    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForNegNot: Generate code for a neg/not
//
// Arguments:
//    tree - neg/not tree node
//
void CodeGen::genCodeForNegNot(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_NEG, GT_NOT));
    genConsumeOperands(tree);

    instruction ins;
    switch (PackOperAndType(tree->OperGet(), tree->TypeGet()))
    {
        case PackOperAndType(GT_NOT, TYP_INT):
            GetEmitter()->emitIns_I(INS_i32_const, emitTypeSize(tree), -1);
            ins = INS_i32_xor;
            break;
        case PackOperAndType(GT_NOT, TYP_LONG):
            GetEmitter()->emitIns_I(INS_i64_const, emitTypeSize(tree), -1);
            ins = INS_i64_xor;
            break;
        case PackOperAndType(GT_NOT, TYP_FLOAT):
        case PackOperAndType(GT_NOT, TYP_DOUBLE):
            unreached();
            break;
        case PackOperAndType(GT_NEG, TYP_INT):
        case PackOperAndType(GT_NEG, TYP_LONG):
            // We cannot easily emit i32.sub(0, x) here since x is already on the stack.
            // So we transform these to SUB in lower.
            unreached();
            break;
        case PackOperAndType(GT_NEG, TYP_FLOAT):
            ins = INS_f32_neg;
            break;
        case PackOperAndType(GT_NEG, TYP_DOUBLE):
            ins = INS_f64_neg;
            break;

        default:
            unreached();
            break;
    }

    GetEmitter()->emitIns(ins);
    genProduceReg(tree);
}

//---------------------------------------------------------------------
// genCodeForNullCheck - generate code for a GT_NULLCHECK node
//
// Arguments:
//    tree - the GT_NULLCHECK node
//
// Notes:
//    If throw helper calls are being emitted inline, we need
//    to wrap the resulting codegen in a block/end pair.
//
void CodeGen::genCodeForNullCheck(GenTreeIndir* tree)
{
    genConsumeAddress(tree->Addr());

    // TODO-WASM: refactor once we have implemented other cases invoking throw helpers
    if (compiler->fgUseThrowHelperBlocks())
    {
        Compiler::AddCodeDsc* const add = compiler->fgGetExcptnTarget(SCK_NULL_CHECK, compiler->compCurBB);

        if (add == nullptr)
        {
            NYI_WASM("Missing null check demand");
        }

        assert(add != nullptr);
        assert(add->acdUsed);
        GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, compiler->compMaxUncheckedOffsetForNullObject);
        GetEmitter()->emitIns(INS_I_le_u);
        inst_JMP(EJ_jmpif, add->acdDstBlk);
    }
    else
    {
        GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, compiler->compMaxUncheckedOffsetForNullObject);
        GetEmitter()->emitIns(INS_I_le_u);
        GetEmitter()->emitIns(INS_if);
        // TODO-WASM: codegen for the call instead of unreachable
        // genEmitHelperCall(compiler->acdHelper(SCK_NULL_CHECK), 0, EA_UNKNOWN);
        GetEmitter()->emitIns(INS_unreachable);
        GetEmitter()->emitIns(INS_end);
    }
}

//------------------------------------------------------------------------
// genCodeForLclAddr: Generates the code for GT_LCL_ADDR.
//
// Arguments:
//    lclAddrNode - the node.
//
void CodeGen::genCodeForLclAddr(GenTreeLclFld* lclAddrNode)
{
    assert(lclAddrNode->OperIs(GT_LCL_ADDR));
    bool     FPBased;
    unsigned lclNum    = lclAddrNode->GetLclNum();
    unsigned lclOffset = lclAddrNode->GetLclOffs();

    GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
    if ((lclOffset != 0) || (compiler->lvaFrameAddress(lclNum, &FPBased) != 0))
    {
        GetEmitter()->emitIns_S(INS_I_const, EA_PTRSIZE, lclNum, lclOffset);
        GetEmitter()->emitIns(INS_I_add);
    }
    genProduceReg(lclAddrNode);
}

//------------------------------------------------------------------------
// genCodeForLclFld: Produce code for a GT_LCL_FLD node.
//
// Arguments:
//    tree - the GT_LCL_FLD node
//
void CodeGen::genCodeForLclFld(GenTreeLclFld* tree)
{
    assert(tree->OperIs(GT_LCL_FLD));
    LclVarDsc* varDsc = compiler->lvaGetDesc(tree);

    GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
    GetEmitter()->emitIns_S(ins_Load(tree->TypeGet()), emitTypeSize(tree), tree->GetLclNum(), tree->GetLclOffs());
    genProduceReg(tree);
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
        GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
        GetEmitter()->emitIns_S(ins_Load(type), emitTypeSize(tree), tree->GetLclNum(), 0);
        genProduceReg(tree);
    }
    else
    {
        assert(genIsValidReg(varDsc->GetRegNum()));
        unsigned wasmLclIndex = WasmRegToIndex(varDsc->GetRegNum());
        GetEmitter()->emitIns_I(INS_local_get, emitTypeSize(tree), wasmLclIndex);
        // In this case, the resulting tree type may be different from the local var type where the value originates,
        // and so we need an explicit conversion since we can't "load"
        // the value with a different type like we can if the value is on the shadow stack.
        if (tree->TypeIs(TYP_INT) && varDsc->TypeIs(TYP_LONG))
        {
            GetEmitter()->emitIns(INS_i32_wrap_i64);
        }
    }
}

//------------------------------------------------------------------------
// genCodeForStoreLclVar: Produce code for a GT_STORE_LCL_VAR node.
//
// Arguments:
//    tree - the GT_STORE_LCL_VAR node
//
void CodeGen::genCodeForStoreLclVar(GenTreeLclVar* tree)
{
    assert(tree->OperIs(GT_STORE_LCL_VAR));

    GenTree* const op1 = tree->gtGetOp1();
    assert(!op1->IsMultiRegNode());
    genConsumeRegs(op1);

    // We rewrite all stack stores to STOREIND because the address must be first on the operand stack, so here only
    // enregistered locals need to be handled.
    LclVarDsc* varDsc    = compiler->lvaGetDesc(tree);
    regNumber  targetReg = varDsc->GetRegNum();
    assert(genIsValidReg(targetReg) && varDsc->lvIsRegCandidate());

    unsigned wasmLclIndex = WasmRegToIndex(targetReg);
    GetEmitter()->emitIns_I(INS_local_set, emitTypeSize(tree), wasmLclIndex);
    genUpdateLifeStore(tree, targetReg, varDsc);
}

//------------------------------------------------------------------------
// genCodeForIndir: Produce code for a GT_IND node.
//
// Arguments:
//    tree - the GT_IND node
//
void CodeGen::genCodeForIndir(GenTreeIndir* tree)
{
    assert(tree->OperIs(GT_IND));

    var_types   type = tree->TypeGet();
    instruction ins  = ins_Load(type);

    genConsumeAddress(tree->Addr());

    // TODO-WASM: Memory barriers

    GetEmitter()->emitIns_I(ins, emitActualTypeSize(type), 0);

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForStoreInd: Produce code for a GT_STOREIND node.
//
// Arguments:
//    tree - the GT_STOREIND node
//
void CodeGen::genCodeForStoreInd(GenTreeStoreInd* tree)
{
    GenTree* data = tree->Data();
    GenTree* addr = tree->Addr();

    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(tree);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        NYI_WASM("write barriers in StoreInd");
    }
    else // A normal store, not a WriteBarrier store
    {
        // We must consume the operands in the proper execution order,
        // so that liveness is updated appropriately.
        genConsumeAddress(addr);
        genConsumeRegs(data);

        var_types   type = tree->TypeGet();
        instruction ins  = ins_Store(type);

        // TODO-WASM: Memory barriers

        GetEmitter()->emitIns_I(ins, emitActualTypeSize(type), 0);
    }

    genUpdateLife(tree);
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

    genTreeOps op          = treeNode->OperGet();
    bool       invertSense = false;

    if ((treeNode->gtFlags & GTF_RELOP_NAN_UN) != 0)
    {
        // We don't expect to see GT_EQ unordered,
        // since CIL doesn't have this mode.
        //
        assert(op != GT_EQ);

        // Wasm float comparisons other than "fne" return false for NaNs.
        // Our unordered float compares need to return true for NaNs.
        //
        // So we can re-express say GT_GE (UN) as !GT_LT
        //
        if (op != GT_NE)
        {
            op          = GenTree::ReverseRelop(op);
            invertSense = true;
        }
    }
    else
    {
        // We don't expect to see GT_NE ordered,
        // since CIL doesn't have this mode.
        //
        assert(op != GT_NE);
    }

    genConsumeOperands(treeNode);

    instruction ins;
    switch (PackOperAndType(op, treeNode->gtOp1->TypeGet()))
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

    if (invertSense)
    {
        GetEmitter()->emitIns(INS_i32_eqz);
    }

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
// genLoadLocalIntoReg: set the register to "load(local on stack)".
//
// Arguments:
//    targetReg - The register to load into
//    lclNum    - The local on stack to load from
//
void CodeGen::genLoadLocalIntoReg(regNumber targetReg, unsigned lclNum)
{
    LclVarDsc* varDsc = compiler->lvaGetDesc(lclNum);
    var_types  type   = varDsc->GetRegisterType();
    GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
    GetEmitter()->emitIns_S(ins_Load(type), emitTypeSize(type), lclNum, 0);
    GetEmitter()->emitIns_I(INS_local_set, emitTypeSize(type), WasmRegToIndex(targetReg));
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
    GetEmitter()->emitIns_J(instr, EA_4BYTE, depth, tgtBlock);
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
