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
#else  // !TARGET_64BIT
static const instruction INS_I_const = INS_i32_const;
static const instruction INS_I_add   = INS_i32_add;
#endif // !TARGET_64BIT

void CodeGen::genMarkLabelsForCodegen()
{
    // No work needed here for now.
    // We mark labels as needed in genEmitStartBlock.
}

//------------------------------------------------------------------------
// genWasmLocals: generate wasm local declarations
//
// TODO-WASM: pre-declare all "register" locals
void CodeGen::genWasmLocals()
{
    // TODO-WASM: proper local count, local declarations, and shadow stack maintenance
    GetEmitter()->emitIns_I(INS_local_cnt, EA_8BYTE, (unsigned)WasmValueType::Count - 1);
    // Emit 1 local of each supported value type to ensure
    // the declarations can be encoded.
    // TODO-WASM: remove and declare locals based on RA assignments once this is supported.
    int localOffset  = 0;
    int countPerType = 1;
    for (unsigned i = (uint8_t)WasmValueType::Invalid + 1; i < (unsigned)WasmValueType::Count; i++)
    {
        GetEmitter()->emitIns_I_Ty(INS_local_decl, countPerType, static_cast<WasmValueType>(i), localOffset);
        localOffset += countPerType;
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

        case GT_NEG:
        case GT_NOT:
            genCodeForNegNot(treeNode->AsOp());
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

    GetEmitter()->emitIns_I(INS_br_table, EA_4BYTE, caseCount);

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

    instruction ins;
    switch (PackOperAndType(treeNode))
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
    switch (PackOperAndType(tree))
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

//------------------------------------------------------------------------
// genCodeForLclAddr: Generates the code for GT_LCL_ADDR.
//
// Arguments:
//    lclAddrNode - the node.
//
void CodeGen::genCodeForLclAddr(GenTreeLclFld* lclAddrNode)
{
    assert(lclAddrNode->OperIs(GT_LCL_ADDR));

    GetEmitter()->emitIns_I(INS_local_get, EA_PTRSIZE, WasmRegToIndex(GetFramePointerReg()));
    GetEmitter()->emitIns_S(INS_I_const, EA_PTRSIZE, lclAddrNode->GetLclNum(), lclAddrNode->GetLclOffs());
    GetEmitter()->emitIns(INS_I_add);
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
