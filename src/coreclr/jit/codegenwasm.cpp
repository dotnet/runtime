// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"
#include "regallocwasm.h"
#include "fgwasm.h"

static const int LINEAR_MEMORY_INDEX = 0;

#ifdef TARGET_64BIT
static const instruction INS_I_load  = INS_i64_load;
static const instruction INS_I_store = INS_i64_store;
static const instruction INS_I_const = INS_i64_const;
static const instruction INS_I_add   = INS_i64_add;
static const instruction INS_I_mul   = INS_i64_mul;
static const instruction INS_I_sub   = INS_i64_sub;
static const instruction INS_I_le_u  = INS_i64_le_u;
static const instruction INS_I_ge_u  = INS_i64_ge_u;
static const instruction INS_I_gt_u  = INS_i64_gt_u;
#else  // !TARGET_64BIT
static const instruction INS_I_load  = INS_i32_load;
static const instruction INS_I_store = INS_i32_store;
static const instruction INS_I_const = INS_i32_const;
static const instruction INS_I_add   = INS_i32_add;
static const instruction INS_I_mul   = INS_i32_mul;
static const instruction INS_I_sub   = INS_i32_sub;
static const instruction INS_I_le_u  = INS_i32_le_u;
static const instruction INS_I_ge_u  = INS_i32_ge_u;
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
void CodeGen::genBeginFnProlog()
{
    unsigned localsCount = 0;
    GetEmitter()->emitIns_I(INS_local_cnt, EA_8BYTE, WasmLocalsDecls.size());
    for (WasmLocalsDecl& decl : WasmLocalsDecls)
    {
        GetEmitter()->emitIns_I_Ty(INS_local_decl, decl.Count, decl.Type, localsCount);
        localsCount += decl.Count;
    }
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
    assert(m_compiler->compGeneratingProlog);
    regNumber spReg = GetStackPointerReg();
    if (spReg == REG_NA)
    {
        assert(!isFramePointerUsed());
        return;
    }

    // TODO-WASM: reverse pinvoke frame allocation
    //
    if (!m_compiler->lvaGetDesc(m_compiler->lvaWasmSpArg)->lvIsParam)
    {
        NYI_WASM("alloc local frame for reverse pinvoke");
    }

    unsigned initialSPLclIndex =
        WasmRegToIndex(m_compiler->lvaGetParameterABIInfo(m_compiler->lvaWasmSpArg).Segment(0).GetRegister());
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

        LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
        if (varDsc->lvTracked && !VarSetOps::IsMember(m_compiler, m_compiler->fgFirstBB->bbLiveIn, varDsc->lvVarIndex))
        {
            return;
        }

        if (varDsc->lvOnFrame && (!varDsc->lvIsInReg() || varDsc->lvLiveInOutOfHndlr))
        {
            LclVarDsc* paramVarDsc = m_compiler->lvaGetDesc(paramLclNum);
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

    for (unsigned lclNum = 0; lclNum < m_compiler->info.compArgsCount; lclNum++)
    {
        LclVarDsc*                   lclDsc  = m_compiler->lvaGetDesc(lclNum);
        const ABIPassingInformation& abiInfo = m_compiler->lvaGetParameterABIInfo(lclNum);

        for (const ABIPassingSegment& segment : abiInfo.Segments())
        {
            if (!segment.IsPassedInRegister())
            {
                continue;
            }

            const ParameterRegisterLocalMapping* mapping =
                m_compiler->FindParameterRegisterLocalMappingByRegister(segment.GetRegister());

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

    ScopedSetVariable<bool> _setGeneratingEpilog(&m_compiler->compGeneratingEpilog, true);

#ifdef DEBUG
    if (m_compiler->opts.dspCode)
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
    if (block->IsLast() || m_compiler->bbIsFuncletBeg(block->Next()))
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
    BasicBlock* const sourceBlock = m_compiler->compCurBB;
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
    if (wasmCursor < m_compiler->fgWasmIntervals->size())
    {
        WasmInterval* interval = m_compiler->fgWasmIntervals->at(wasmCursor);
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
                BasicBlock* const endBlock = m_compiler->fgIndexToBlockMap[interval->End()];

                if (!endBlock->HasFlag(BBF_HAS_LABEL))
                {
                    endBlock->SetFlags(BBF_HAS_LABEL);
                    genDefineTempLabel(endBlock);
                }
            }

            if (wasmCursor >= m_compiler->fgWasmIntervals->size())
            {
                break;
            }

            interval = m_compiler->fgWasmIntervals->at(wasmCursor);
            chain    = interval->Chain();
        }
    }
}

//------------------------------------------------------------------------
// WasmProduceReg: Produce a register and update liveness for an emitted node.
//
// Wrapper over "genProduceReg". Does two additional things:
// 1. Emits "local.tee"s for nodes that produce temporary registers so that
//    they can be used multiple times.
// 2. Emits "drop" for unused values.
//
// Arguments:
//    node - The emitted node
//
void CodeGen::WasmProduceReg(GenTree* node)
{
    assert(!genIsRegCandidateLocal(node)); // Candidate liveness is handled in "genConsumeReg".
    if (genIsValidReg(node->GetRegNum()))
    {
        GetEmitter()->emitIns_I(INS_local_tee, emitActualTypeSize(node), WasmRegToIndex(node->GetRegNum()));
    }
    genProduceReg(node);
    if (node->IsUnusedValue())
    {
        GetEmitter()->emitIns(INS_drop);
    }
}

//------------------------------------------------------------------------
// GetMultiUseOperandReg: Get the register of a multi-use operand.
//
// If the operand is a candidate, we use that candidate's current register.
// Otherwise it must have been allocated into a temporary register initialized
// in 'WasmProduceReg'. To do this, set the LIR::Flags::MultiplyUsed flag during
// lowering or other pre-regalloc phases, and ensure that regalloc is updated to
// call CollectReferences on the node(s) that need to be used multiple times.
//
// Arguments:
//    operand - The operand node
//
// Return Value:
//    The register to use for 'operand'.
//
regNumber CodeGen::GetMultiUseOperandReg(GenTree* operand)
{
    if (genIsRegCandidateLocal(operand))
    {
        LclVarDsc* varDsc = m_compiler->lvaGetDesc(operand->AsLclVar());
        assert(varDsc->lvIsInReg());
        return varDsc->GetRegNum();
    }

    regNumber reg = operand->GetRegNum();
    assert(genIsValidReg(reg));
    return reg;
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
    if (m_compiler->verbose)
    {
        m_compiler->gtDispLIRNode(treeNode, "Generating: ");
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

        case GT_PHYSREG:
            genCodeForPhysReg(treeNode->AsPhysReg());
            break;

        case GT_JTRUE:
            genCodeForJTrue(treeNode->AsOp());
            break;

        case GT_SWITCH:
            genTableBasedSwitch(treeNode);
            break;

        case GT_RETURN:
        case GT_RETFILT:
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

        case GT_BITCAST:
            genCodeForBitCast(treeNode->AsOp());
            break;

        case GT_NEG:
        case GT_NOT:
            genCodeForNegNot(treeNode->AsOp());
            break;

        case GT_IND:
            genCodeForIndir(treeNode->AsIndir());
            break;

        case GT_STOREIND:
            genCodeForStoreInd(treeNode->AsStoreInd());
            break;

        case GT_CALL:
            genCall(treeNode->AsCall());
            break;

        case GT_NULLCHECK:
            genCodeForNullCheck(treeNode->AsIndir());
            break;

        case GT_BOUNDS_CHECK:
            genRangeCheck(treeNode);
            break;

        case GT_KEEPALIVE:
            // TODO-WASM-RA: remove KEEPALIVE after we've produced the GC info.
            genConsumeRegs(treeNode->AsOp()->gtOp1);
            GetEmitter()->emitIns(INS_drop);
            break;

        case GT_INDEX_ADDR:
            genCodeForIndexAddr(treeNode->AsIndexAddr());
            break;

        case GT_LEA:
            genLeaInstruction(treeNode->AsAddrMode());
            break;

        case GT_STORE_BLK:
            genCodeForStoreBlk(treeNode->AsBlk());
            break;

        case GT_MEMORYBARRIER:
            // No-op for single-threaded wasm.
            assert(!WASM_THREAD_SUPPORT);
            JITDUMP("Ignoring GT_MEMORYBARRIER; single-threaded codegen\n");
            break;

        case GT_INTRINSIC:
            genIntrinsic(treeNode->AsIntrinsic());
            break;

        case GT_PINVOKE_PROLOG:
            // TODO-WASM-CQ re-establish the global stack pointer here?
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
    BasicBlock* const block = m_compiler->compCurBB;
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
    BasicBlock* const block = m_compiler->compCurBB;
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
    GenIntCastDesc desc(cast);

    if (desc.CheckKind() != GenIntCastDesc::CHECK_NONE)
    {
        GenTree*  castValue = cast->gtGetOp1();
        regNumber castReg   = GetMultiUseOperandReg(castValue);
        genIntCastOverflowCheck(cast, desc, castReg);
    }

    var_types   toType     = genActualType(cast->CastToType());
    var_types   fromType   = genActualType(cast->CastOp());
    int         extendSize = desc.ExtendSrcSize();
    instruction ins        = INS_none;
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
    WasmProduceReg(cast);
}

//------------------------------------------------------------------------
// genIntCastOverflowCheck: Generate overflow checking code for an integer cast.
//
// Arguments:
//    cast - The GT_CAST node
//    desc - The cast description
//    reg  - The register containing the value to check
//
void CodeGen::genIntCastOverflowCheck(GenTreeCast* cast, const GenIntCastDesc& desc, regNumber reg)
{
    bool const     is64BitSrc = (desc.CheckSrcSize() == 8);
    emitAttr const srcSize    = is64BitSrc ? EA_8BYTE : EA_4BYTE;

    GetEmitter()->emitIns_I(INS_local_get, srcSize, WasmRegToIndex(reg));

    switch (desc.CheckKind())
    {
        case GenIntCastDesc::CHECK_POSITIVE:
        {
            // INT or LONG to ULONG
            GetEmitter()->emitIns_I(is64BitSrc ? INS_i64_const : INS_i32_const, srcSize, 0);
            GetEmitter()->emitIns(is64BitSrc ? INS_i64_lt_s : INS_i32_lt_s);
            genJumpToThrowHlpBlk(SCK_OVERFLOW);
            break;
        }

        case GenIntCastDesc::CHECK_UINT_RANGE:
        {
            // (U)LONG to UINT
            assert(is64BitSrc);
            GetEmitter()->emitIns_I(INS_i64_const, srcSize, UINT32_MAX);
            // We can re-interpret LONG as ULONG
            // Then negative values will be larger than UINT32_MAX
            GetEmitter()->emitIns(INS_i64_gt_u);
            genJumpToThrowHlpBlk(SCK_OVERFLOW);
            break;
        }

        case GenIntCastDesc::CHECK_POSITIVE_INT_RANGE:
        {
            // ULONG to INT
            GetEmitter()->emitIns_I(INS_i64_const, srcSize, INT32_MAX);
            GetEmitter()->emitIns(INS_i64_gt_u);
            genJumpToThrowHlpBlk(SCK_OVERFLOW);
            break;
        }

        case GenIntCastDesc::CHECK_INT_RANGE:
        {
            // LONG to INT
            GetEmitter()->emitIns(INS_i64_extend32_s);
            GetEmitter()->emitIns_I(INS_local_get, srcSize, WasmRegToIndex(reg));
            GetEmitter()->emitIns(INS_i64_ne);
            genJumpToThrowHlpBlk(SCK_OVERFLOW);
            break;
        }

        case GenIntCastDesc::CHECK_SMALL_INT_RANGE:
        {
            // (U)(INT|LONG) to Small INT
            const int castMaxValue = desc.CheckSmallIntMax();
            const int castMinValue = desc.CheckSmallIntMin();

            if (castMinValue == 0)
            {
                // When the minimum is 0, a single unsigned upper-bound check is sufficient.
                // For signed sources, negative values become large unsigned values and
                // thus also trigger the overflow via the same comparison.
                GetEmitter()->emitIns_I(is64BitSrc ? INS_i64_const : INS_i32_const, srcSize, castMaxValue);
                GetEmitter()->emitIns(is64BitSrc ? INS_i64_gt_u : INS_i32_gt_u);
            }
            else
            {
                // We need to check a range around zero, eg [-128, 127] for 8-bit signed.
                // Do two compares and combine the results: (src > max) | (src < min).
                assert(!cast->IsUnsigned());
                GetEmitter()->emitIns_I(is64BitSrc ? INS_i64_const : INS_i32_const, srcSize, castMaxValue);
                GetEmitter()->emitIns(is64BitSrc ? INS_i64_gt_s : INS_i32_gt_s);
                GetEmitter()->emitIns_I(INS_local_get, srcSize, WasmRegToIndex(reg));
                GetEmitter()->emitIns_I(is64BitSrc ? INS_i64_const : INS_i32_const, srcSize, castMinValue);
                GetEmitter()->emitIns(is64BitSrc ? INS_i64_lt_s : INS_i32_lt_s);
                GetEmitter()->emitIns(INS_i32_or);
            }
            genJumpToThrowHlpBlk(SCK_OVERFLOW);
            break;
        }

        default:
            unreached();
    }
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
    WasmProduceReg(tree);
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
    WasmProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForBinary: Generate code for a binary arithmetic operator
//
// Arguments:
//    treeNode - The binary operation for which we are generating code.
//
void CodeGen::genCodeForBinary(GenTreeOp* treeNode)
{
    if (treeNode->gtOverflow())
    {
        genCodeForBinaryOverflow(treeNode);
        return;
    }

    genConsumeOperands(treeNode);

    instruction ins;
    switch (PackOperAndType(treeNode->OperGet(), treeNode->TypeGet()))
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

        case PackOperAndType(GT_SUB, TYP_INT):
            ins = INS_i32_sub;
            break;
        case PackOperAndType(GT_SUB, TYP_LONG):
            ins = INS_i64_sub;
            break;
        case PackOperAndType(GT_SUB, TYP_FLOAT):
            ins = INS_f32_sub;
            break;
        case PackOperAndType(GT_SUB, TYP_DOUBLE):
            ins = INS_f64_sub;
            break;

        case PackOperAndType(GT_MUL, TYP_INT):
            ins = INS_i32_mul;
            break;
        case PackOperAndType(GT_MUL, TYP_LONG):
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
    WasmProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForBinaryOverflow: Generate code for a binary arithmetic operator
//   with overflow checking
//
// Arguments:
//    treeNode - The binary operation for which we are generating code.
//
void CodeGen::genCodeForBinaryOverflow(GenTreeOp* treeNode)
{
    assert(treeNode->gtOverflow());
    assert(varTypeIsIntegral(treeNode->TypeGet()));

    // TODO-WASM-CQ: consider using helper calls for all these cases

    genConsumeOperands(treeNode);

    const bool    is64BitOp = treeNode->TypeIs(TYP_LONG);
    InternalRegs* regs      = internalRegisters.GetAll(treeNode);
    regNumber     op1Reg    = GetMultiUseOperandReg(treeNode->gtGetOp1());
    regNumber     op2Reg    = GetMultiUseOperandReg(treeNode->gtGetOp2());

    switch (treeNode->OperGet())
    {
        case GT_ADD:
        {
            // We require an internal register.
            assert(regs->Count() == 1);
            regNumber resultReg = regs->Extract();
            assert(WasmRegToType(resultReg) == TypeToWasmValueType(treeNode->TypeGet()));

            // Add and save the sum
            GetEmitter()->emitIns(is64BitOp ? INS_i64_add : INS_i32_add);
            GetEmitter()->emitIns_I(INS_local_set, emitActualTypeSize(treeNode), WasmRegToIndex(resultReg));
            // See if addends had the same sign. XOR leaves a non-negative result if they had the same sign.
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(op1Reg));
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(op2Reg));
            GetEmitter()->emitIns(is64BitOp ? INS_i64_xor : INS_i32_xor);

            // TODO-WASM-CQ: consider branchless alternative here (and for sub)
            GetEmitter()->emitIns_I(is64BitOp ? INS_i64_const : INS_i32_const, emitActualTypeSize(treeNode), 0);
            GetEmitter()->emitIns(is64BitOp ? INS_i64_ge_s : INS_i32_ge_s);
            GetEmitter()->emitIns(INS_if);
            {
                // Operands have the same sign. If the sum has a different sign, then the add overflowed.
                GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(resultReg));
                GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(op1Reg));
                GetEmitter()->emitIns(is64BitOp ? INS_i64_xor : INS_i32_xor);
                GetEmitter()->emitIns_I(is64BitOp ? INS_i64_const : INS_i32_const, emitActualTypeSize(treeNode), 0);
                GetEmitter()->emitIns(is64BitOp ? INS_i64_lt_s : INS_i32_lt_s);
                genJumpToThrowHlpBlk(SCK_OVERFLOW);
            }
            GetEmitter()->emitIns(INS_end);
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(resultReg));
            break;
        }

        case GT_SUB:
        {
            // We require an internal register.
            assert(regs->Count() == 1);
            regNumber resultReg = regs->Extract();
            assert(WasmRegToType(resultReg) == TypeToWasmValueType(treeNode->TypeGet()));

            // Subtract and save the difference
            GetEmitter()->emitIns(is64BitOp ? INS_i64_sub : INS_i32_sub);
            GetEmitter()->emitIns_I(INS_local_set, emitActualTypeSize(treeNode), WasmRegToIndex(resultReg));
            // See if operands had a different sign. XOR leaves a negative result if they had different signs.
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(op1Reg));
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(op2Reg));
            GetEmitter()->emitIns(is64BitOp ? INS_i64_xor : INS_i32_xor);
            GetEmitter()->emitIns_I(is64BitOp ? INS_i64_const : INS_i32_const, emitActualTypeSize(treeNode), 0);
            GetEmitter()->emitIns(is64BitOp ? INS_i64_lt_s : INS_i32_lt_s);
            GetEmitter()->emitIns(INS_if);
            {
                // Operands have different signs. If the difference has a different sign than op1, then the subtraction
                // overflowed.
                GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(resultReg));
                GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(op1Reg));
                GetEmitter()->emitIns(is64BitOp ? INS_i64_xor : INS_i32_xor);
                GetEmitter()->emitIns_I(is64BitOp ? INS_i64_const : INS_i32_const, emitActualTypeSize(treeNode), 0);
                GetEmitter()->emitIns(is64BitOp ? INS_i64_lt_s : INS_i32_lt_s);
                genJumpToThrowHlpBlk(SCK_OVERFLOW);
            }
            GetEmitter()->emitIns(INS_end);
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(resultReg));
            break;
        }

        case GT_MUL:
        {
            if (is64BitOp)
            {
                assert(!"64 bit multiply with overflow should have been transformed into a helper call by morph");
            }

            // We require an I64 internal register
            assert(regs->Count() == 1);
            regNumber wideReg = regs->Extract();
            assert(WasmRegToType(wideReg) == WasmValueType::I64);

            // 32 bit multiply... check by doing a 64 bit multiply and then range-checking the result
            const bool isUnsigned = treeNode->IsUnsigned();
            // Both operands are on the stack as I32. Drop the second, extend the first, then extend the second.
            //
            // TODO-WASM-CQ: consider transforming this to a (u)long multiply plus a checked cast, either in morph or
            // lower.
            GetEmitter()->emitIns(INS_drop);
            GetEmitter()->emitIns(isUnsigned ? INS_i64_extend_u_i32 : INS_i64_extend_s_i32);
            GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(treeNode), WasmRegToIndex(op2Reg));
            GetEmitter()->emitIns(isUnsigned ? INS_i64_extend_u_i32 : INS_i64_extend_s_i32);
            GetEmitter()->emitIns(INS_i64_mul);

            // Save the wide result, and then overflow check it.
            GetEmitter()->emitIns_I(INS_local_tee, EA_8BYTE, WasmRegToIndex(wideReg));

            if (isUnsigned)
            {
                // For unsigned multiply, we just need to check if the result is greater than UINT32_MAX.
                GetEmitter()->emitIns_I(INS_i64_const, EA_8BYTE, UINT32_MAX);
                GetEmitter()->emitIns(INS_i64_gt_u);
                genJumpToThrowHlpBlk(SCK_OVERFLOW);
            }
            else
            {
                GetEmitter()->emitIns(INS_i64_extend32_s);
                GetEmitter()->emitIns_I(INS_local_get, EA_8BYTE, WasmRegToIndex(wideReg));
                GetEmitter()->emitIns(INS_i64_ne);
                genJumpToThrowHlpBlk(SCK_OVERFLOW);
            }

            // If the check succeeds, the multiplication result is in range for a 32-bit int.
            // We just need to return the low 32 bits of the result.
            GetEmitter()->emitIns_I(INS_local_get, EA_8BYTE, WasmRegToIndex(wideReg));
            GetEmitter()->emitIns(INS_i32_wrap_i64);

            break;
        }

        default:
            unreached();
            break;
    }

    WasmProduceReg(treeNode);
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

    if (!varTypeIsFloating(treeNode->TypeGet()))
    {
        ExceptionSetFlags exSetFlags = treeNode->OperExceptions(m_compiler);
        bool              is64BitOp  = treeNode->TypeIs(TYP_LONG);
        emitAttr          size       = is64BitOp ? EA_8BYTE : EA_4BYTE;

        // (AnyVal / 0) => DivideByZeroException.
        GenTree*  divisor    = treeNode->gtGetOp2();
        regNumber divisorReg = REG_NA;
        if ((exSetFlags & ExceptionSetFlags::DivideByZeroException) != ExceptionSetFlags::None)
        {
            divisorReg = GetMultiUseOperandReg(divisor);
            GetEmitter()->emitIns_I(INS_local_get, size, WasmRegToIndex(divisorReg));
            GetEmitter()->emitIns(is64BitOp ? INS_i64_eqz : INS_i32_eqz);
            genJumpToThrowHlpBlk(SCK_DIV_BY_ZERO);
        }

        // (MinInt / -1) => ArithmeticException.
        if ((exSetFlags & ExceptionSetFlags::ArithmeticException) != ExceptionSetFlags::None)
        {
            if (divisorReg == REG_NA)
            {
                divisorReg = GetMultiUseOperandReg(divisor);
            }
            GetEmitter()->emitIns_I(INS_local_get, size, WasmRegToIndex(divisorReg));
            GetEmitter()->emitIns_I(is64BitOp ? INS_i64_const : INS_i32_const, size, -1);
            GetEmitter()->emitIns(is64BitOp ? INS_i64_eq : INS_i32_eq);

            regNumber dividendReg = GetMultiUseOperandReg(treeNode->gtGetOp1());
            GetEmitter()->emitIns_I(INS_local_get, size, WasmRegToIndex(dividendReg));
            GetEmitter()->emitIns_I(is64BitOp ? INS_i64_const : INS_i32_const, size, is64BitOp ? INT64_MIN : INT32_MIN);
            GetEmitter()->emitIns(is64BitOp ? INS_i64_eq : INS_i32_eq);

            GetEmitter()->emitIns(is64BitOp ? INS_i64_and : INS_i32_and);
            genJumpToThrowHlpBlk(SCK_ARITH_EXCPN);
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
            unreached();
    }

    GetEmitter()->emitIns(ins);
    WasmProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForConstant: Generate code for an integer or floating point constant
//
// Arguments:
//    treeNode - The constant.
//
void CodeGen::genCodeForConstant(GenTree* treeNode)
{
    instruction    ins  = INS_none;
    cnsval_ssize_t bits = 0;
    var_types      type = treeNode->TypeIs(TYP_REF, TYP_BYREF) ? TYP_I_IMPL : treeNode->TypeGet();
    static_assert(sizeof(cnsval_ssize_t) >= sizeof(double));

    GenTreeIntConCommon* icon = nullptr;
    if ((type == TYP_INT) || (type == TYP_LONG))
    {
        icon = treeNode->AsIntConCommon();
        if (icon->ImmedValNeedsReloc(m_compiler))
        {
            // WASM-TODO: Generate reloc for this handle
            ins  = INS_I_const;
            bits = 0;
        }
        else
        {
            bits = icon->IntegralValue();
        }
    }

    if (ins == INS_none)
    {
        switch (type)
        {
            case TYP_INT:
            {
                ins = INS_i32_const;
                assert(FitsIn<INT32>(bits));
                break;
            }
            case TYP_LONG:
            {
                ins = INS_i64_const;
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
    }

    // The IF_ for the selected instruction, i.e. IF_F64, determines how these bits are emitted
    GetEmitter()->emitIns_I(ins, emitTypeSize(treeNode), bits);
    WasmProduceReg(treeNode);
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
    WasmProduceReg(treeNode);
}

//----------------------------------------------------------------------
// genCodeForBitCast - Generate code for a GT_BITCAST that is not contained
//
// Arguments
//    tree - the GT_BITCAST for which we're generating code
//
void CodeGen::genCodeForBitCast(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_BITCAST));
    genConsumeOperands(tree);

    var_types toType   = tree->TypeGet();
    var_types fromType = genActualType(tree->gtGetOp1()->TypeGet());
    assert(toType == genActualType(tree));

    instruction ins = INS_none;
    switch (PackTypes(toType, fromType))
    {
        case PackTypes(TYP_INT, TYP_FLOAT):
            ins = INS_i32_reinterpret_f32;
            break;
        case PackTypes(TYP_FLOAT, TYP_INT):
            ins = INS_f32_reinterpret_i32;
            break;
        case PackTypes(TYP_LONG, TYP_DOUBLE):
            ins = INS_i64_reinterpret_f64;
            break;
        case PackTypes(TYP_DOUBLE, TYP_LONG):
            ins = INS_f64_reinterpret_i64;
            break;
        default:
            unreached();
            break;
    }

    GetEmitter()->emitIns(ins);
    WasmProduceReg(tree);
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
    WasmProduceReg(tree);
}

//---------------------------------------------------------------------
// genJumpToThrowHlpBlk - generate code to invoke a throw helper call
//
// Arguments:
//    codeKind -- kind of throw helper call needed
//
// Notes:
//    On entry the predicate for the throw helper is the only item on the Wasm stack.
//    An exception is thrown if the predicate is true.
//
void CodeGen::genJumpToThrowHlpBlk(SpecialCodeKind codeKind)
{
    if (m_compiler->fgUseThrowHelperBlocks())
    {
        Compiler::AddCodeDsc* const add = m_compiler->fgGetExcptnTarget(codeKind, m_compiler->compCurBB);
        assert(add != nullptr);
        assert(add->acdUsed);
        inst_JMP(EJ_jmpif, add->acdDstBlk);
    }
    else
    {
        GetEmitter()->emitIns(INS_if);
        // Throw helper arity is (i (sp)) -> (void).
        // Push SP here as the arg for the call.
        GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetStackPointerReg()));
        genEmitHelperCall(m_compiler->acdHelper(codeKind), 0, EA_UNKNOWN);
        GetEmitter()->emitIns(INS_end);
    }
}

//---------------------------------------------------------------------
// genCodeForNullCheck - generate code for a GT_NULLCHECK node
//
// Arguments:
//    tree - the GT_NULLCHECK node
//
void CodeGen::genCodeForNullCheck(GenTreeIndir* tree)
{
    genConsumeAddress(tree->Addr());
    genEmitNullCheck(REG_NA);
}

//---------------------------------------------------------------------
// genEmitNullCheck - generate code for a null check
//
// Arguments:
//    regNum - register to check, or REG_NA if value to check is on the stack
//
void CodeGen::genEmitNullCheck(regNumber reg)
{
    if (reg != REG_NA)
    {
        GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(reg));
    }

    GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, m_compiler->compMaxUncheckedOffsetForNullObject);
    GetEmitter()->emitIns(INS_I_le_u);
    genJumpToThrowHlpBlk(SCK_NULL_CHECK);
}

//------------------------------------------------------------------------
// genRangeCheck - generate code for a GT_BOUNDS_CHECK node
//
// Arguments:
//    tree - the GT_BOUNDS_CHECK node
//
// Notes:
//    Incoming stack args are index; length (tos).
//
void CodeGen::genRangeCheck(GenTree* tree)
{
    assert(tree->OperIs(GT_BOUNDS_CHECK));
    genConsumeOperands(tree->AsOp());
    GetEmitter()->emitIns(INS_I_ge_u);
    genJumpToThrowHlpBlk(SCK_RNGCHK_FAIL);
}

//------------------------------------------------------------------------
// genCodeForIndexAddr: Produce code for a GT_INDEX_ADDR node.
//
// Arguments:
//    tree - the GT_INDEX_ADDR node
//
void CodeGen::genCodeForIndexAddr(GenTreeIndexAddr* node)
{
    genConsumeOperands(node);

    GenTree* const base  = node->Arr();
    GenTree* const index = node->Index();

    assert(varTypeIsIntegral(index->TypeGet()));

    // Generate the bounds check if necessary.
    if (node->IsBoundsChecked())
    {
        // We need internal registers for this case.
        NYI_WASM("GT_INDEX_ADDR with bounds check");
    }

    // Zero extend index if necessary.
    if (genTypeSize(index->TypeGet()) < TARGET_POINTER_SIZE)
    {
        assert(TARGET_POINTER_SIZE == 8);
        GetEmitter()->emitIns(INS_i64_extend_u_i32);
    }

    // Result is the address of the array element.
    unsigned const scale = node->gtElemSize;

    if (scale > 1)
    {
        GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, scale);
        GetEmitter()->emitIns(INS_I_mul);
    }
    GetEmitter()->emitIns(INS_I_add);
    GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, node->gtElemOffset);
    GetEmitter()->emitIns(INS_I_add);
    WasmProduceReg(node);
}

//------------------------------------------------------------------------
// genLeaInstruction: Produce code for a GT_LEA node.
//
// Arguments:
//    lea - the node
//
void CodeGen::genLeaInstruction(GenTreeAddrMode* lea)
{
    genConsumeOperands(lea);
    assert(lea->HasIndex() || lea->HasBase());

    if (lea->HasIndex())
    {
        unsigned const scale = lea->gtScale;

        if (scale > 1)
        {
            GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, scale);
            GetEmitter()->emitIns(INS_I_mul);
        }

        if (lea->HasBase())
        {
            GetEmitter()->emitIns(INS_I_add);
        }
    }

    const int offset = lea->Offset();

    if (offset != 0)
    {
        GetEmitter()->emitIns_I(INS_I_const, EA_PTRSIZE, offset);
        GetEmitter()->emitIns(INS_I_add);
    }

    WasmProduceReg(lea);
}

//------------------------------------------------------------------------
// PackIntrinsicAndType: Pack a intrinsic and var_types into a uint32_t
//
// Arguments:
//    ni - a NamedIntrinsic to pack
//    type - a var_types to pack
//
// Return Value:
//    intrinsic and type packed into an integer that can be used as a switch value/case
//
static constexpr uint32_t PackIntrinsicAndType(NamedIntrinsic ni, var_types type)
{
    if ((type == TYP_BYREF) || (type == TYP_REF))
    {
        type = TYP_I_IMPL;
    }
    const int shift1 = ConstLog2<TYP_COUNT>::value + 1;
    return ((uint32_t)ni << shift1) | ((uint32_t)type);
}

//---------------------------------------------------------------------
// genIntrinsic - generate code for a given intrinsic
//
// Arguments
//    treeNode - the GT_INTRINSIC node
//
// Return value:
//    None
//
void CodeGen::genIntrinsic(GenTreeIntrinsic* treeNode)
{
    genConsumeOperands(treeNode);

    // Handle intrinsics that can be implemented by target-specific instructions
    instruction ins = INS_invalid;

    switch (PackIntrinsicAndType(treeNode->gtIntrinsicName, treeNode->TypeGet()))
    {
        case PackIntrinsicAndType(NI_System_Math_Abs, TYP_FLOAT):
            ins = INS_f32_abs;
            break;
        case PackIntrinsicAndType(NI_System_Math_Abs, TYP_DOUBLE):
            ins = INS_f64_abs;
            break;

        case PackIntrinsicAndType(NI_System_Math_Ceiling, TYP_FLOAT):
            ins = INS_f32_ceil;
            break;
        case PackIntrinsicAndType(NI_System_Math_Ceiling, TYP_DOUBLE):
            ins = INS_f64_ceil;
            break;

        case PackIntrinsicAndType(NI_System_Math_Floor, TYP_FLOAT):
            ins = INS_f32_floor;
            break;
        case PackIntrinsicAndType(NI_System_Math_Floor, TYP_DOUBLE):
            ins = INS_f64_floor;
            break;

        case PackIntrinsicAndType(NI_System_Math_Max, TYP_FLOAT):
        case PackIntrinsicAndType(NI_System_Math_MaxNative, TYP_FLOAT):
            ins = INS_f32_max;
            break;
        case PackIntrinsicAndType(NI_System_Math_Max, TYP_DOUBLE):
        case PackIntrinsicAndType(NI_System_Math_MaxNative, TYP_DOUBLE):
            ins = INS_f64_max;
            break;

        case PackIntrinsicAndType(NI_System_Math_Min, TYP_FLOAT):
        case PackIntrinsicAndType(NI_System_Math_MinNative, TYP_FLOAT):
            ins = INS_f32_min;
            break;
        case PackIntrinsicAndType(NI_System_Math_Min, TYP_DOUBLE):
        case PackIntrinsicAndType(NI_System_Math_MinNative, TYP_DOUBLE):
            ins = INS_f64_min;
            break;

        case PackIntrinsicAndType(NI_System_Math_Round, TYP_FLOAT):
            ins = INS_f32_nearest;
            break;
        case PackIntrinsicAndType(NI_System_Math_Round, TYP_DOUBLE):
            ins = INS_f64_nearest;
            break;

        case PackIntrinsicAndType(NI_System_Math_Sqrt, TYP_FLOAT):
            ins = INS_f32_sqrt;
            break;
        case PackIntrinsicAndType(NI_System_Math_Sqrt, TYP_DOUBLE):
            ins = INS_f64_sqrt;
            break;

        case PackIntrinsicAndType(NI_System_Math_Truncate, TYP_FLOAT):
            ins = INS_f32_trunc;
            break;
        case PackIntrinsicAndType(NI_System_Math_Truncate, TYP_DOUBLE):
            ins = INS_f64_trunc;
            break;

        case PackIntrinsicAndType(NI_PRIMITIVE_LeadingZeroCount, TYP_INT):
            ins = INS_i32_clz;
            break;
        case PackIntrinsicAndType(NI_PRIMITIVE_LeadingZeroCount, TYP_LONG):
            ins = INS_i64_clz;
            break;

        case PackIntrinsicAndType(NI_PRIMITIVE_TrailingZeroCount, TYP_INT):
            ins = INS_i32_ctz;
            break;
        case PackIntrinsicAndType(NI_PRIMITIVE_TrailingZeroCount, TYP_LONG):
            ins = INS_i64_ctz;
            break;

        case PackIntrinsicAndType(NI_PRIMITIVE_PopCount, TYP_INT):
            ins = INS_i32_popcnt;
            break;
        case PackIntrinsicAndType(NI_PRIMITIVE_PopCount, TYP_LONG):
            ins = INS_i64_popcnt;
            break;

        default:
            assert(!"genIntrinsic: Unsupported intrinsic");
            unreached();
    }

    GetEmitter()->emitIns(ins);

    WasmProduceReg(treeNode);
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
    if ((lclOffset != 0) || (m_compiler->lvaFrameAddress(lclNum, &FPBased) != 0))
    {
        GetEmitter()->emitIns_S(INS_I_const, EA_PTRSIZE, lclNum, lclOffset);
        GetEmitter()->emitIns(INS_I_add);
    }
    WasmProduceReg(lclAddrNode);
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
    LclVarDsc* varDsc = m_compiler->lvaGetDesc(tree);

    GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
    GetEmitter()->emitIns_S(ins_Load(tree->TypeGet()), emitTypeSize(tree), tree->GetLclNum(), tree->GetLclOffs());
    WasmProduceReg(tree);
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
    LclVarDsc* varDsc = m_compiler->lvaGetDesc(tree);

    // Unlike other targets, we can't "reload at the point of use", since that would require inserting instructions
    // into the middle of an already-emitted instruction group. Instead, we order the nodes in a way that obeys the
    // value stack constraints of WASM precisely. However, the liveness tracking is done in the same way as for other
    // targets, hence "WasmProduceReg" is only called for non-candidates.
    if (!varDsc->lvIsRegCandidate())
    {
        var_types type = varDsc->GetRegisterType(tree);
        GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
        GetEmitter()->emitIns_S(ins_Load(type), emitTypeSize(tree), tree->GetLclNum(), 0);
        WasmProduceReg(tree);
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
    LclVarDsc* varDsc    = m_compiler->lvaGetDesc(tree);
    regNumber  targetReg = tree->GetRegNum();
    assert(genIsValidReg(targetReg) && varDsc->lvIsRegCandidate());

    GetEmitter()->emitIns_I(INS_local_set, emitTypeSize(tree), WasmRegToIndex(targetReg));
    genUpdateLifeStore(tree, targetReg, varDsc);
}

//------------------------------------------------------------------------
// genCodeForPhysReg: Produce code for a PHYSREG node.
//
// Arguments:
//    tree - the GT_PHYSREG node
//
void CodeGen::genCodeForPhysReg(GenTreePhysReg* tree)
{
    assert(genIsValidReg(tree->gtSrcReg));
    GetEmitter()->emitIns_I(INS_local_get, emitActualTypeSize(tree), WasmRegToIndex(tree->gtSrcReg));
    WasmProduceReg(tree);
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

    WasmProduceReg(tree);
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

    // We must consume the operands in the proper execution order,
    // so that liveness is updated appropriately.
    genConsumeAddress(addr);
    genConsumeRegs(data);

    GCInfo::WriteBarrierForm writeBarrierForm = gcInfo.gcIsWriteBarrierCandidate(tree);
    if (writeBarrierForm != GCInfo::WBF_NoBarrier)
    {
        genGCWriteBarrier(tree, writeBarrierForm);
    }
    else // A normal store, not a WriteBarrier store
    {
        var_types   type = tree->TypeGet();
        instruction ins  = ins_Store(type);

        // TODO-WASM: Memory barriers

        GetEmitter()->emitIns_I(ins, emitActualTypeSize(type), 0);
    }

    genUpdateLife(tree);
}

//------------------------------------------------------------------------
// genCall: Produce code for a GT_CALL node
//
// Arguments:
//    call - the GT_CALL node
//
void CodeGen::genCall(GenTreeCall* call)
{
    regNumber thisReg = REG_NA;

    if (call->NeedsNullCheck())
    {
        CallArg* thisArg  = call->gtArgs.GetThisArg();
        GenTree* thisNode = thisArg->GetNode();
        thisReg           = GetMultiUseOperandReg(thisNode);
    }

    for (CallArg& arg : call->gtArgs.EarlyArgs())
    {
        genConsumeReg(arg.GetEarlyNode());
    }

    for (CallArg& arg : call->gtArgs.LateArgs())
    {
        genConsumeReg(arg.GetLateNode());
    }

    if (call->NeedsNullCheck())
    {
        genEmitNullCheck(thisReg);
    }

    genCallInstruction(call);
    WasmProduceReg(call);
}

//------------------------------------------------------------------------
// genCallInstruction - Generate instructions necessary to transfer control to the call.
//
// Arguments:
//    call - the GT_CALL node
//
void CodeGen::genCallInstruction(GenTreeCall* call)
{
    EmitCallParams params;
    params.isJump      = call->IsFastTailCall();
    params.hasAsyncRet = call->IsAsync();

    // We need to propagate the debug information to the call instruction, so we can emit
    // an IL to native mapping record for the call, to support managed return value debugging.
    // We don't want tail call helper calls that were converted from normal calls to get a record,
    // so we skip this hash table lookup logic in that case.
    if (m_compiler->opts.compDbgInfo && m_compiler->genCallSite2DebugInfoMap != nullptr && !call->IsTailCall())
    {
        DebugInfo di;
        (void)m_compiler->genCallSite2DebugInfoMap->Lookup(call, &di);
        params.debugInfo = di;
    }

#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (!call->IsHelperCall())
    {
        params.sigInfo = call->callSig;
    }
#endif // DEBUG
    GenTree* target = getCallTarget(call, &params.methHnd);

    if (target != nullptr)
    {
        // Codegen should have already evaluated our target node (last) and pushed it onto the stack,
        //  ready for call_indirect. Consume it.
        genConsumeReg(target);

        params.callType = EC_INDIR_R;
        genEmitCallWithCurrentGC(params);
    }
    else
    {
        // If we have no target and this is a call with indirection cell then
        // we do an optimization where we load the call address directly from
        // the indirection cell instead of duplicating the tree. In BuildCall
        // we ensure that get an extra register for the purpose. Note that for
        // CFG the call might have changed to
        // CORINFO_HELP_DISPATCH_INDIRECT_CALL in which case we still have the
        // indirection cell but we should not try to optimize.
        WellKnownArg indirectionCellArgKind = WellKnownArg::None;
        if (!call->IsHelperCall(m_compiler, CORINFO_HELP_DISPATCH_INDIRECT_CALL))
        {
            indirectionCellArgKind = call->GetIndirectionCellArgKind();
        }

        if (indirectionCellArgKind != WellKnownArg::None)
        {
            assert(call->IsR2ROrVirtualStubRelativeIndir());

            params.callType = EC_INDIR_R;
            // params.ireg     = targetAddrReg;
            genEmitCallWithCurrentGC(params);
        }
        else
        {
            // Generate a direct call to a non-virtual user defined or helper method
            assert(call->IsHelperCall() || (call->gtCallType == CT_USER_FUNC));

            assert(call->gtEntryPoint.addr == NULL);

            if (call->IsHelperCall())
            {
                assert(!call->IsFastTailCall());
                CorInfoHelpFunc helperNum = m_compiler->eeGetHelperNum(params.methHnd);
                noway_assert(helperNum != CORINFO_HELP_UNDEF);
                CORINFO_CONST_LOOKUP helperLookup = m_compiler->compGetHelperFtn(helperNum);
                assert(helperLookup.accessType == IAT_VALUE);
                params.addr = helperLookup.addr;
            }
            else
            {
                // Direct call to a non-virtual user function.
                params.addr = call->gtDirectCallAddress;
            }

            params.callType = EC_FUNC_TOKEN;
            genEmitCallWithCurrentGC(params);
        }
    }
}

//------------------------------------------------------------------------
// genEmitHelperCall: emit a call to a runtime helper
//
// Arguments:
//   hgelper -- helper call index (CorinfoHelpFunc enum value)
//   argSize -- ignored
//   retSize -- ignored
//   callTargetReg -- ignored
//
// Notes:
//   Wasm helper calls use the managed calling convention.
//   SP arg must be first, on the stack below any arguments.
//
void CodeGen::genEmitHelperCall(unsigned helper, int argSize, emitAttr retSize, regNumber callTargetReg /*= REG_NA */)
{
    EmitCallParams params;

    CORINFO_CONST_LOOKUP helperFunction = m_compiler->compGetHelperFtn((CorInfoHelpFunc)helper);
    params.ireg                         = callTargetReg;

    if (helperFunction.accessType == IAT_VALUE)
    {
        params.callType = EC_FUNC_TOKEN;
        params.addr     = helperFunction.addr;
    }
    else
    {
        params.addr = nullptr;
        assert(helperFunction.accessType == IAT_PVALUE);
        void* pAddr = helperFunction.addr;

        // Push indirection cell address onto stack for genEmitCall to dereference
        GetEmitter()->emitIns_I(INS_i32_const_address, EA_HANDLE_CNS_RELOC, (cnsval_ssize_t)pAddr);

        params.callType = EC_INDIR_R;
    }

    params.methHnd = m_compiler->eeFindHelper(helper);
    params.argSize = argSize;
    params.retSize = retSize;

    genEmitCallWithCurrentGC(params);
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
    WasmProduceReg(treeNode);
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

    WasmProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCodeForStoreBlk: Produce code for a GT_STORE_BLK node.
//
// Arguments:
//    blkOp - the node
//
void CodeGen::genCodeForStoreBlk(GenTreeBlk* blkOp)
{
    assert(blkOp->OperIs(GT_STORE_BLK));

    bool isCopyBlk = blkOp->OperIsCopyBlkOp();

    switch (blkOp->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindCpObjUnroll:
            genCodeForCpObj(blkOp->AsBlk());
            break;

        case GenTreeBlk::BlkOpKindLoop:
            assert(!isCopyBlk);
            genCodeForInitBlkLoop(blkOp);
            break;

        case GenTreeBlk::BlkOpKindNativeOpcode:
            genConsumeOperands(blkOp);
            // Emit the size constant expected by the memory.copy and memory.fill opcodes
            GetEmitter()->emitIns_I(INS_i32_const, EA_4BYTE, blkOp->Size());
            GetEmitter()->emitIns_I(isCopyBlk ? INS_memory_copy : INS_memory_fill, EA_8BYTE, LINEAR_MEMORY_INDEX);
            break;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// genCodeForCpObj: Produce code for a GT_STORE_BLK node that represents a cpobj operation.
//
// Arguments:
//    cpObjNode - the node
//
void CodeGen::genCodeForCpObj(GenTreeBlk* cpObjNode)
{
    GenTree*  dstAddr     = cpObjNode->Addr();
    GenTree*  source      = cpObjNode->Data();
    var_types srcAddrType = TYP_BYREF;

    assert(source->isContained());
    if (source->OperIs(GT_IND))
    {
        source = source->gtGetOp1();
        assert(!source->isContained());
        srcAddrType = source->TypeGet();
    }

    noway_assert(source->IsLocal());
    noway_assert(dstAddr->IsLocal());

    // If the destination is on the stack we don't need the write barrier.
    bool dstOnStack = cpObjNode->IsAddressNotOnHeap(m_compiler);

#ifdef DEBUG
    assert(!dstAddr->isContained());

    // This GenTree node has data about GC pointers, this means we're dealing
    // with CpObj.
    assert(cpObjNode->GetLayout()->HasGCPtr());
#endif // DEBUG

    genConsumeOperands(cpObjNode);

    ClassLayout* layout = cpObjNode->GetLayout();
    unsigned     slots  = layout->GetSlotCount();

    regNumber srcReg = GetMultiUseOperandReg(source);
    regNumber dstReg = GetMultiUseOperandReg(dstAddr);

    if (cpObjNode->IsVolatile())
    {
        // TODO-WASM: Memory barrier
    }

    emitter* emit = GetEmitter();

    emitAttr attrSrcAddr = emitActualTypeSize(srcAddrType);
    emitAttr attrDstAddr = emitActualTypeSize(dstAddr->TypeGet());

    unsigned gcPtrCount = cpObjNode->GetLayout()->GetGCPtrCount();

    unsigned i = 0, offset = 0;
    while (i < slots)
    {
        // Copy the pointer-sized non-gc-pointer slots one at a time (and GC pointer slots if the destination is stack)
        //  using regular I-sized load/store pairs.
        if (dstOnStack || !layout->IsGCPtr(i))
        {
            // Do a pointer-sized load+store pair at the appropriate offset relative to dest and source
            emit->emitIns_I(INS_local_get, attrDstAddr, WasmRegToIndex(dstReg));
            emit->emitIns_I(INS_local_get, attrSrcAddr, WasmRegToIndex(srcReg));
            emit->emitIns_I(INS_I_load, EA_PTRSIZE, offset);
            emit->emitIns_I(INS_I_store, EA_PTRSIZE, offset);
        }
        else
        {
            // Compute the actual dest/src of the slot being copied to pass to the helper.
            emit->emitIns_I(INS_local_get, attrDstAddr, WasmRegToIndex(dstReg));
            emit->emitIns_I(INS_I_const, attrDstAddr, offset);
            emit->emitIns(INS_I_add);
            emit->emitIns_I(INS_local_get, attrSrcAddr, WasmRegToIndex(srcReg));
            emit->emitIns_I(INS_I_const, attrSrcAddr, offset);
            emit->emitIns(INS_I_add);
            // Call the byref assign helper. On other targets this updates the dst/src regs but here it won't,
            //  so we have to do the local.get+i32.const+i32.add dance every time.
            genEmitHelperCall(CORINFO_HELP_ASSIGN_BYREF, 0, EA_PTRSIZE);
            gcPtrCount--;
        }
        ++i;
        offset += TARGET_POINTER_SIZE;
    }

    assert(dstOnStack || (gcPtrCount == 0));

    if (cpObjNode->IsVolatile())
    {
        // TODO-WASM: Memory barrier
    }

    WasmProduceReg(cpObjNode);
}

//------------------------------------------------------------------------
// genCodeForInitBlkLoop - Generate code for an InitBlk using an inlined for-loop.
//    It's needed for cases when size is too big to unroll and we're not allowed
//    to use memset call due to atomicity requirements.
//
// Arguments:
//    blkOp - the GT_STORE_BLK node
//
void CodeGen::genCodeForInitBlkLoop(GenTreeBlk* blkOp)
{
    // TODO-WASM: In multi-threaded wasm we will need to generate a for loop that atomically zeroes one GC ref
    //  at a time. Right now we're single-threaded, so we can just use memory.fill.
    assert(!WASM_THREAD_SUPPORT);

    genConsumeOperands(blkOp);
    // Emit the value constant expected by the memory.fill opcode (zero)
    GetEmitter()->emitIns_I(INS_i32_const, EA_4BYTE, 0);
    // Emit the size constant expected by the memory.copy and memory.fill opcodes
    GetEmitter()->emitIns_I(INS_i32_const, EA_4BYTE, blkOp->Size());
    GetEmitter()->emitIns_I(INS_memory_fill, EA_8BYTE, LINEAR_MEMORY_INDEX);
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
    LclVarDsc* varDsc = m_compiler->lvaGetDesc(lclNum);
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
    assert(m_compiler->compLclFrameSize >= 0);
    return m_compiler->compLclFrameSize;
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
