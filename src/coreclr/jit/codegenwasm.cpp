// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"

void CodeGen::genMarkLabelsForCodegen()
{
    // TODO-WASM: serialize fgWasmControlFlow results into codegen-level metadata/labels
    // (or use them directly and leave this empty).
}

void CodeGen::genFnProlog()
{
    NYI_WASM("Uncomment CodeGen::genFnProlog and proceed from there");
}

void CodeGen::genFnEpilog(BasicBlock* block)
{
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In genFnEpilog()\n");
    }
#endif // DEBUG

    NYI_WASM("genFnEpilog");
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
        case GT_DIV:
        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
        case GT_OR:
        case GT_XOR:
        case GT_AND:
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
            genCodeForShiftOrRotate(treeNode->AsOp());
            break;

        case GT_LCL_VAR:
            genCodeForLclVar(treeNode->AsLclVar());
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
// genCodeForShiftOrRotate: Generate code for a shift or rotate operator
//
// Arguments:
//    treeNode - The shift or rotate operation for which we are generating code.
//
void CodeGen::genCodeForShiftOrRotate(GenTreeOp* treeNode)
{
    genConsumeOperands(treeNode);

    // TODO-WASM: Zero-extend the 2nd operand for shifts and rotates as needed when the 1st and 2nd operand are
    // different types. The shift operand width in IR is always TYP_INT; the WASM operations have the same widths
    // for both the shift and shiftee. So the shift may need to be extended (zero-extended) for TYP_LONG.

    instruction ins;
    switch (PackOperAndType(treeNode))
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
            NYI_WASM("genCodeForShiftOrRotate");
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
// inst_JMP: Generate a jump instruction.
//
void CodeGen::inst_JMP(emitJumpKind jmp, BasicBlock* tgtBlock)
{
    NYI_WASM("inst_JMP");
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
