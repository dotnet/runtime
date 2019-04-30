// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                        ARM/ARM64 Code Generator Common Code               XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#ifdef _TARGET_ARMARCH_ // This file is ONLY used for ARM and ARM64 architectures

#include "codegen.h"
#include "lower.h"
#include "gcinfo.h"
#include "emit.h"

//------------------------------------------------------------------------
// genStackPointerConstantAdjustment: add a specified constant value to the stack pointer.
// No probe is done.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative or zero.
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerConstantAdjustment(ssize_t spDelta)
{
    assert(spDelta < 0);

    // We assert that the SP change is less than one page. If it's greater, you should have called a
    // function that does a probe, which will in turn call this function.
    assert((target_size_t)(-spDelta) <= compiler->eeGetPageSize());

    inst_RV_IV(INS_sub, REG_SPBASE, -spDelta, EA_PTRSIZE);
}

//------------------------------------------------------------------------
// genStackPointerConstantAdjustmentWithProbe: add a specified constant value to the stack pointer,
// and probe the stack as appropriate. Should only be called as a helper for
// genStackPointerConstantAdjustmentLoopWithProbe.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative or zero. If zero, the probe happens,
//                              but the stack pointer doesn't move.
//    regTmp                  - temporary register to use as target for probe load instruction
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerConstantAdjustmentWithProbe(ssize_t spDelta, regNumber regTmp)
{
    getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regTmp, REG_SP, 0);
    genStackPointerConstantAdjustment(spDelta);
}

//------------------------------------------------------------------------
// genStackPointerConstantAdjustmentLoopWithProbe: Add a specified constant value to the stack pointer,
// and probe the stack as appropriate. Generates one probe per page, up to the total amount required.
// This will generate a sequence of probes in-line.
//
// Arguments:
//    spDelta                 - the value to add to SP. Must be negative.
//    regTmp                  - temporary register to use as target for probe load instruction
//
// Return Value:
//    Offset in bytes from SP to last probed address.
//
target_ssize_t CodeGen::genStackPointerConstantAdjustmentLoopWithProbe(ssize_t spDelta, regNumber regTmp)
{
    assert(spDelta < 0);

    const target_size_t pageSize = compiler->eeGetPageSize();

    ssize_t spRemainingDelta = spDelta;
    do
    {
        ssize_t spOneDelta = -(ssize_t)min((target_size_t)-spRemainingDelta, pageSize);
        genStackPointerConstantAdjustmentWithProbe(spOneDelta, regTmp);
        spRemainingDelta -= spOneDelta;
    } while (spRemainingDelta < 0);

    // What offset from the final SP was the last probe? This depends on the fact that
    // genStackPointerConstantAdjustmentWithProbe() probes first, then does "SUB SP".
    target_size_t lastTouchDelta = (target_size_t)(-spDelta) % pageSize;
    if ((lastTouchDelta == 0) || (lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize))
    {
        // We haven't probed almost a complete page. If lastTouchDelta==0, then spDelta was an exact
        // multiple of pageSize, which means we last probed exactly one page back. Otherwise, we probed
        // the page, but very far from the end. If the next action on the stack might subtract from SP
        // first, before touching the current SP, then we do one more probe at the very bottom. This can
        // happen on x86, for example, when we copy an argument to the stack using a "SUB ESP; REP MOV"
        // strategy.

        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regTmp, REG_SP, 0);
        lastTouchDelta = 0;
    }

    return lastTouchDelta;
}

//------------------------------------------------------------------------
// genCodeForTreeNode Generate code for a single node in the tree.
//
// Preconditions:
//    All operands have been evaluated.
//
void CodeGen::genCodeForTreeNode(GenTree* treeNode)
{
    regNumber targetReg  = treeNode->gtRegNum;
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = getEmitter();

#ifdef DEBUG
    // Validate that all the operands for the current node are consumed in order.
    // This is important because LSRA ensures that any necessary copies will be
    // handled correctly.
    lastConsumedNode = nullptr;
    if (compiler->verbose)
    {
        unsigned seqNum = treeNode->gtSeqNum; // Useful for setting a conditional break in Visual Studio
        compiler->gtDispLIRNode(treeNode, "Generating: ");
    }
#endif // DEBUG

#ifdef _TARGET_ARM64_ // TODO-ARM: is this applicable to ARM32?
    // Is this a node whose value is already in a register?  LSRA denotes this by
    // setting the GTF_REUSE_REG_VAL flag.
    if (treeNode->IsReuseRegVal())
    {
        // For now, this is only used for constant nodes.
        assert((treeNode->OperGet() == GT_CNS_INT) || (treeNode->OperGet() == GT_CNS_DBL));
        JITDUMP("  TreeNode is marked ReuseReg\n");
        return;
    }
#endif // _TARGET_ARM64_

    // contained nodes are part of their parents for codegen purposes
    // ex : immediates, most LEAs
    if (treeNode->isContained())
    {
        return;
    }

    switch (treeNode->gtOper)
    {
        case GT_START_NONGC:
            getEmitter()->emitDisableGC();
            break;

        case GT_START_PREEMPTGC:
            // Kill callee saves GC registers, and create a label
            // so that information gets propagated to the emitter.
            gcInfo.gcMarkRegSetNpt(RBM_INT_CALLEE_SAVED);
            genDefineTempLabel(genCreateTempLabel());
            break;

        case GT_PROF_HOOK:
            // We should be seeing this only if profiler hook is needed
            noway_assert(compiler->compIsProfilerHookNeeded());

#ifdef PROFILING_SUPPORTED
            // Right now this node is used only for tail calls. In future if
            // we intend to use it for Enter or Leave hooks, add a data member
            // to this node indicating the kind of profiler hook. For example,
            // helper number can be used.
            genProfilingLeaveCallback(CORINFO_HELP_PROF_FCN_TAILCALL);
#endif // PROFILING_SUPPORTED
            break;

        case GT_LCLHEAP:
            genLclHeap(treeNode);
            break;

        case GT_CNS_INT:
        case GT_CNS_DBL:
            genSetRegToConst(targetReg, targetType, treeNode);
            genProduceReg(treeNode);
            break;

        case GT_NOT:
        case GT_NEG:
            genCodeForNegNot(treeNode);
            break;

        case GT_MOD:
        case GT_UMOD:
        case GT_DIV:
        case GT_UDIV:
            genCodeForDivMod(treeNode->AsOp());
            break;

        case GT_OR:
        case GT_XOR:
        case GT_AND:
            assert(varTypeIsIntegralOrI(treeNode));

            __fallthrough;

#if !defined(_TARGET_64BIT_)
        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
#endif // !defined(_TARGET_64BIT_)

        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
            genConsumeOperands(treeNode->AsOp());
            genCodeForBinary(treeNode->AsOp());
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        // case GT_ROL: // No ROL instruction on ARM; it has been lowered to ROR.
        case GT_ROR:
            genCodeForShift(treeNode);
            break;

#if !defined(_TARGET_64BIT_)

        case GT_LSH_HI:
        case GT_RSH_LO:
            genCodeForShiftLong(treeNode);
            break;

#endif // !defined(_TARGET_64BIT_)

        case GT_CAST:
            genCodeForCast(treeNode->AsOp());
            break;

        case GT_BITCAST:
        {
            GenTree* op1 = treeNode->gtOp.gtOp1;
            if (varTypeIsFloating(treeNode) != varTypeIsFloating(op1))
            {
#ifdef _TARGET_ARM64_
                inst_RV_RV(INS_fmov, targetReg, genConsumeReg(op1), targetType);
#else  // !_TARGET_ARM64_
                if (varTypeIsFloating(treeNode))
                {
                    // GT_BITCAST on ARM is only used to cast floating-point arguments to integer
                    // registers. Nobody generates GT_BITCAST from int to float currently.
                    NYI_ARM("GT_BITCAST from 'int' to 'float'");
                }
                else
                {
                    assert(varTypeIsFloating(op1));

                    if (op1->TypeGet() == TYP_FLOAT)
                    {
                        inst_RV_RV(INS_vmov_f2i, targetReg, genConsumeReg(op1), targetType);
                    }
                    else
                    {
                        assert(op1->TypeGet() == TYP_DOUBLE);
                        regNumber otherReg = treeNode->AsMultiRegOp()->gtOtherReg;
                        assert(otherReg != REG_NA);
                        inst_RV_RV_RV(INS_vmov_d2i, targetReg, otherReg, genConsumeReg(op1), EA_8BYTE);
                    }
                }
#endif // !_TARGET_ARM64_
            }
            else
            {
                inst_RV_RV(ins_Copy(targetType), targetReg, genConsumeReg(op1), targetType);
            }
        }
        break;

        case GT_LCL_FLD_ADDR:
        case GT_LCL_VAR_ADDR:
            genCodeForLclAddr(treeNode);
            break;

        case GT_LCL_FLD:
            genCodeForLclFld(treeNode->AsLclFld());
            break;

        case GT_LCL_VAR:
            genCodeForLclVar(treeNode->AsLclVar());
            break;

        case GT_STORE_LCL_FLD:
            genCodeForStoreLclFld(treeNode->AsLclFld());
            break;

        case GT_STORE_LCL_VAR:
            genCodeForStoreLclVar(treeNode->AsLclVar());
            break;

        case GT_RETFILT:
        case GT_RETURN:
            genReturn(treeNode);
            break;

        case GT_LEA:
            // If we are here, it is the case where there is an LEA that cannot be folded into a parent instruction.
            genLeaInstruction(treeNode->AsAddrMode());
            break;

        case GT_INDEX_ADDR:
            genCodeForIndexAddr(treeNode->AsIndexAddr());
            break;

        case GT_IND:
            genCodeForIndir(treeNode->AsIndir());
            break;

#ifdef _TARGET_ARM_
        case GT_MUL_LONG:
            genCodeForMulLong(treeNode->AsMultiRegOp());
            break;
#endif // _TARGET_ARM_

#ifdef _TARGET_ARM64_

        case GT_MULHI:
            genCodeForMulHi(treeNode->AsOp());
            break;

        case GT_SWAP:
            genCodeForSwap(treeNode->AsOp());
            break;
#endif // _TARGET_ARM64_

        case GT_JMP:
            genJmpMethod(treeNode);
            break;

        case GT_CKFINITE:
            genCkfinite(treeNode);
            break;

        case GT_INTRINSIC:
            genIntrinsic(treeNode);
            break;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            genSIMDIntrinsic(treeNode->AsSIMD());
            break;
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWIntrinsic:
            genHWIntrinsic(treeNode->AsHWIntrinsic());
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_CMP:
#ifdef _TARGET_ARM64_
        case GT_TEST_EQ:
        case GT_TEST_NE:
#endif // _TARGET_ARM64_
            genCodeForCompare(treeNode->AsOp());
            break;

        case GT_JTRUE:
            genCodeForJumpTrue(treeNode->AsOp());
            break;

#ifdef _TARGET_ARM64_
        case GT_JCMP:
            genCodeForJumpCompare(treeNode->AsOp());
            break;
#endif // _TARGET_ARM64_

        case GT_JCC:
            genCodeForJcc(treeNode->AsCC());
            break;

        case GT_SETCC:
            genCodeForSetcc(treeNode->AsCC());
            break;

        case GT_RETURNTRAP:
            genCodeForReturnTrap(treeNode->AsOp());
            break;

        case GT_STOREIND:
            genCodeForStoreInd(treeNode->AsStoreInd());
            break;

        case GT_COPY:
            // This is handled at the time we call genConsumeReg() on the GT_COPY
            break;

        case GT_LIST:
        case GT_FIELD_LIST:
            // Should always be marked contained.
            assert(!"LIST, FIELD_LIST nodes should always be marked contained.");
            break;

        case GT_PUTARG_STK:
            genPutArgStk(treeNode->AsPutArgStk());
            break;

        case GT_PUTARG_REG:
            genPutArgReg(treeNode->AsOp());
            break;

#if FEATURE_ARG_SPLIT
        case GT_PUTARG_SPLIT:
            genPutArgSplit(treeNode->AsPutArgSplit());
            break;
#endif // FEATURE_ARG_SPLIT

        case GT_CALL:
            genCallInstruction(treeNode->AsCall());
            break;

        case GT_MEMORYBARRIER:
            instGen_MemoryBarrier();
            break;

#ifdef _TARGET_ARM64_
        case GT_XCHG:
        case GT_XADD:
            genLockedInstructions(treeNode->AsOp());
            break;

        case GT_CMPXCHG:
            genCodeForCmpXchg(treeNode->AsCmpXchg());
            break;
#endif // _TARGET_ARM64_

        case GT_RELOAD:
            // do nothing - reload is just a marker.
            // The parent node will call genConsumeReg on this which will trigger the unspill of this node's child
            // into the register specified in this node.
            break;

        case GT_NOP:
            break;

        case GT_NO_OP:
            instGen(INS_nop);
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            genRangeCheck(treeNode);
            break;

        case GT_PHYSREG:
            genCodeForPhysReg(treeNode->AsPhysReg());
            break;

        case GT_NULLCHECK:
            genCodeForNullCheck(treeNode->AsOp());
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
            genPendingCallLabel = genCreateTempLabel();
            emit->emitIns_R_L(INS_adr, EA_PTRSIZE, genPendingCallLabel, targetReg);
            break;

        case GT_STORE_OBJ:
        case GT_STORE_DYN_BLK:
        case GT_STORE_BLK:
            genCodeForStoreBlk(treeNode->AsBlk());
            break;

        case GT_JMPTABLE:
            genJumpTable(treeNode);
            break;

        case GT_SWITCH_TABLE:
            genTableBasedSwitch(treeNode);
            break;

        case GT_ARR_INDEX:
            genCodeForArrIndex(treeNode->AsArrIndex());
            break;

        case GT_ARR_OFFSET:
            genCodeForArrOffset(treeNode->AsArrOffs());
            break;

#ifdef _TARGET_ARM_

        case GT_CLS_VAR_ADDR:
            emit->emitIns_R_C(INS_lea, EA_PTRSIZE, targetReg, treeNode->gtClsVar.gtClsVarHnd, 0);
            genProduceReg(treeNode);
            break;

        case GT_LONG:
            assert(treeNode->isUsedFromReg());
            genConsumeRegs(treeNode);
            break;

#endif // _TARGET_ARM_

        case GT_IL_OFFSET:
            // Do nothing; these nodes are simply markers for debug info.
            break;

        default:
        {
#ifdef DEBUG
            char message[256];
            _snprintf_s(message, _countof(message), _TRUNCATE, "NYI: Unimplemented node type %s",
                        GenTree::OpName(treeNode->OperGet()));
            NYIRAW(message);
#else
            NYI("unimplemented node");
#endif
        }
        break;
    }
}

//------------------------------------------------------------------------
// genSetRegToIcon: Generate code that will set the given register to the integer constant.
//
void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type, insFlags flags)
{
    // Reg cannot be a FP reg
    assert(!genIsValidFloatReg(reg));

    // The only TYP_REF constant that can come this path is a managed 'null' since it is not
    // relocatable.  Other ref type constants (e.g. string objects) go through a different
    // code path.
    noway_assert(type != TYP_REF || val == 0);

    instGen_Set_Reg_To_Imm(emitActualTypeSize(type), reg, val, flags);
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
void CodeGen::genIntrinsic(GenTree* treeNode)
{
    assert(treeNode->OperIs(GT_INTRINSIC));

    // Both operand and its result must be of the same floating point type.
    GenTree* srcNode = treeNode->gtOp.gtOp1;
    assert(varTypeIsFloating(srcNode));
    assert(srcNode->TypeGet() == treeNode->TypeGet());

    // Right now only Abs/Ceiling/Floor/Round/Sqrt are treated as math intrinsics.
    //
    switch (treeNode->gtIntrinsic.gtIntrinsicId)
    {
        case CORINFO_INTRINSIC_Abs:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_ABS, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;

#ifdef _TARGET_ARM64_
        case CORINFO_INTRINSIC_Ceiling:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_frintp, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;

        case CORINFO_INTRINSIC_Floor:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_frintm, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;

        case CORINFO_INTRINSIC_Round:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_frintn, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;
#endif // _TARGET_ARM64_

        case CORINFO_INTRINSIC_Sqrt:
            genConsumeOperands(treeNode->AsOp());
            getEmitter()->emitInsBinary(INS_SQRT, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;

        default:
            assert(!"genIntrinsic: Unsupported intrinsic");
            unreached();
    }

    genProduceReg(treeNode);
}

//---------------------------------------------------------------------
// genPutArgStk - generate code for a GT_PUTARG_STK node
//
// Arguments
//    treeNode - the GT_PUTARG_STK node
//
// Return value:
//    None
//
void CodeGen::genPutArgStk(GenTreePutArgStk* treeNode)
{
    assert(treeNode->OperIs(GT_PUTARG_STK));
    GenTree*  source     = treeNode->gtOp1;
    var_types targetType = genActualType(source->TypeGet());
    emitter*  emit       = getEmitter();

    // This is the varNum for our store operations,
    // typically this is the varNum for the Outgoing arg space
    // When we are generating a tail call it will be the varNum for arg0
    unsigned varNumOut    = (unsigned)-1;
    unsigned argOffsetMax = (unsigned)-1; // Records the maximum size of this area for assert checks

    // Get argument offset to use with 'varNumOut'
    // Here we cross check that argument offset hasn't changed from lowering to codegen since
    // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
    unsigned argOffsetOut = treeNode->gtSlotNum * TARGET_POINTER_SIZE;

#ifdef DEBUG
    fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(treeNode->gtCall, treeNode);
    assert(curArgTabEntry);
    assert(argOffsetOut == (curArgTabEntry->slotNum * TARGET_POINTER_SIZE));
#endif // DEBUG

    // Whether to setup stk arg in incoming or out-going arg area?
    // Fast tail calls implemented as epilog+jmp = stk arg is setup in incoming arg area.
    // All other calls - stk arg is setup in out-going arg area.
    if (treeNode->putInIncomingArgArea())
    {
        varNumOut    = getFirstArgWithStackSlot();
        argOffsetMax = compiler->compArgSize;
#if FEATURE_FASTTAILCALL
        // This must be a fast tail call.
        assert(treeNode->gtCall->IsFastTailCall());

        // Since it is a fast tail call, the existence of first incoming arg is guaranteed
        // because fast tail call requires that in-coming arg area of caller is >= out-going
        // arg area required for tail call.
        LclVarDsc* varDsc = &(compiler->lvaTable[varNumOut]);
        assert(varDsc != nullptr);
#endif // FEATURE_FASTTAILCALL
    }
    else
    {
        varNumOut    = compiler->lvaOutgoingArgSpaceVar;
        argOffsetMax = compiler->lvaOutgoingArgSpaceSize;
    }

    bool isStruct = (targetType == TYP_STRUCT) || (source->OperGet() == GT_FIELD_LIST);

    if (!isStruct) // a normal non-Struct argument
    {
        if (varTypeIsSIMD(targetType))
        {
            assert(!source->isContained());

            regNumber srcReg = genConsumeReg(source);

            emitAttr storeAttr = emitTypeSize(targetType);

            assert((srcReg != REG_NA) && (genIsValidFloatReg(srcReg)));
            emit->emitIns_S_R(INS_str, storeAttr, srcReg, varNumOut, argOffsetOut);

            argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
            assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            return;
        }

        instruction storeIns  = ins_Store(targetType);
        emitAttr    storeAttr = emitTypeSize(targetType);

        // If it is contained then source must be the integer constant zero
        if (source->isContained())
        {
#ifdef _TARGET_ARM64_
            assert(source->OperGet() == GT_CNS_INT);
            assert(source->AsIntConCommon()->IconValue() == 0);

            emit->emitIns_S_R(storeIns, storeAttr, REG_ZR, varNumOut, argOffsetOut);
#else  // !_TARGET_ARM64_
            // There is no zero register on ARM32
            unreached();
#endif // !_TARGET_ARM64
        }
        else
        {
            genConsumeReg(source);
            emit->emitIns_S_R(storeIns, storeAttr, source->gtRegNum, varNumOut, argOffsetOut);
#ifdef _TARGET_ARM_
            if (targetType == TYP_LONG)
            {
                // This case currently only occurs for double types that are passed as TYP_LONG;
                // actual long types would have been decomposed by now.
                assert(source->IsCopyOrReload());
                regNumber otherReg = (regNumber)source->AsCopyOrReload()->GetRegNumByIdx(1);
                assert(otherReg != REG_NA);
                argOffsetOut += EA_4BYTE;
                emit->emitIns_S_R(storeIns, storeAttr, otherReg, varNumOut, argOffsetOut);
            }
#endif // _TARGET_ARM_
        }
        argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
        assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
    }
    else // We have some kind of a struct argument
    {
        assert(source->isContained()); // We expect that this node was marked as contained in Lower

        if (source->OperGet() == GT_FIELD_LIST)
        {
            genPutArgStkFieldList(treeNode, varNumOut);
        }
        else // We must have a GT_OBJ or a GT_LCL_VAR
        {
            noway_assert((source->OperGet() == GT_LCL_VAR) || (source->OperGet() == GT_OBJ));

            var_types targetType = source->TypeGet();
            noway_assert(varTypeIsStruct(targetType));

            // We will copy this struct to the stack, possibly using a ldp/ldr instruction
            // in ARM64/ARM
            // Setup loReg (and hiReg) from the internal registers that we reserved in lower.
            //
            regNumber loReg = treeNode->ExtractTempReg();
#ifdef _TARGET_ARM64_
            regNumber hiReg = treeNode->GetSingleTempReg();
#endif // _TARGET_ARM64_
            regNumber addrReg = REG_NA;

            GenTreeLclVarCommon* varNode  = nullptr;
            GenTree*             addrNode = nullptr;

            if (source->OperGet() == GT_LCL_VAR)
            {
                varNode = source->AsLclVarCommon();
            }
            else // we must have a GT_OBJ
            {
                assert(source->OperGet() == GT_OBJ);

                addrNode = source->gtOp.gtOp1;

                // addrNode can either be a GT_LCL_VAR_ADDR or an address expression
                //
                if (addrNode->OperGet() == GT_LCL_VAR_ADDR)
                {
                    // We have a GT_OBJ(GT_LCL_VAR_ADDR)
                    //
                    // We will treat this case the same as above
                    // (i.e if we just had this GT_LCL_VAR directly as the source)
                    // so update 'source' to point this GT_LCL_VAR_ADDR node
                    // and continue to the codegen for the LCL_VAR node below
                    //
                    varNode  = addrNode->AsLclVarCommon();
                    addrNode = nullptr;
                }
                else // addrNode is used
                {
                    // Generate code to load the address that we need into a register
                    genConsumeAddress(addrNode);
                    addrReg = addrNode->gtRegNum;

#ifdef _TARGET_ARM64_
                    // If addrReg equal to loReg, swap(loReg, hiReg)
                    // This reduces code complexity by only supporting one addrReg overwrite case
                    if (loReg == addrReg)
                    {
                        loReg = hiReg;
                        hiReg = addrReg;
                    }
#endif // _TARGET_ARM64_
                }
            }

            // Either varNode or addrNOde must have been setup above,
            // the xor ensures that only one of the two is setup, not both
            assert((varNode != nullptr) ^ (addrNode != nullptr));

            BYTE  gcPtrArray[MAX_ARG_REG_COUNT] = {}; // TYPE_GC_NONE = 0
            BYTE* gcPtrs                        = gcPtrArray;

            unsigned gcPtrCount; // The count of GC pointers in the struct
            unsigned structSize;
            bool     isHfa;

#ifdef _TARGET_ARM_
            // On ARM32, size of reference map can be larger than MAX_ARG_REG_COUNT
            gcPtrs     = treeNode->gtGcPtrs;
            gcPtrCount = treeNode->gtNumberReferenceSlots;
#endif
            // Setup the structSize, isHFa, and gcPtrCount
            if (source->OperGet() == GT_LCL_VAR)
            {
                assert(varNode != nullptr);
                LclVarDsc* varDsc = compiler->lvaGetDesc(varNode);

                // This struct also must live in the stack frame
                // And it can't live in a register (SIMD)
                assert(varDsc->lvType == TYP_STRUCT);
                assert(varDsc->lvOnFrame && !varDsc->lvRegister);

                structSize = varDsc->lvSize(); // This yields the roundUp size, but that is fine
                                               // as that is how much stack is allocated for this LclVar
                isHfa = varDsc->lvIsHfa();
#ifdef _TARGET_ARM64_
                gcPtrCount = varDsc->lvStructGcCount;
                for (unsigned i = 0; i < gcPtrCount; ++i)
                    gcPtrs[i]   = varDsc->lvGcLayout[i];
#endif // _TARGET_ARM_
            }
            else // we must have a GT_OBJ
            {
                assert(source->OperGet() == GT_OBJ);

                // If the source is an OBJ node then we need to use the type information
                // it provides (size and GC layout) even if the node wraps a lclvar. Due
                // to struct reinterpretation (e.g. Unsafe.As<X, Y>) it is possible that
                // the OBJ node has a different type than the lclvar.
                CORINFO_CLASS_HANDLE objClass = source->gtObj.gtClass;

                structSize = compiler->info.compCompHnd->getClassSize(objClass);

                // The codegen code below doesn't have proper support for struct sizes
                // that are not multiple of the slot size. Call arg morphing handles this
                // case by copying non-local values to temporary local variables.
                // More generally, we can always round up the struct size when the OBJ node
                // wraps a local variable because the local variable stack allocation size
                // is also rounded up to be a multiple of the slot size.
                if (varNode != nullptr)
                {
                    structSize = roundUp(structSize, TARGET_POINTER_SIZE);
                }
                else
                {
                    assert((structSize % TARGET_POINTER_SIZE) == 0);
                }

                isHfa = compiler->IsHfa(objClass);

#ifdef _TARGET_ARM64_
                // On ARM32, Lowering places the correct GC layout information in the
                // GenTreePutArgStk node and the code above already use that. On ARM64,
                // this information is not available (in order to keep GenTreePutArgStk
                // nodes small) and we need to retrieve it from the VM here.
                gcPtrCount = compiler->info.compCompHnd->getClassGClayout(objClass, &gcPtrs[0]);
#endif
            }

            // If we have an HFA we can't have any GC pointers,
            // if not then the max size for the the struct is 16 bytes
            if (isHfa)
            {
                noway_assert(gcPtrCount == 0);
            }
#ifdef _TARGET_ARM64_
            else
            {
                noway_assert(structSize <= 2 * TARGET_POINTER_SIZE);
            }

            noway_assert(structSize <= MAX_PASS_MULTIREG_BYTES);
#endif // _TARGET_ARM64_

            int      remainingSize = structSize;
            unsigned structOffset  = 0;
            unsigned nextIndex     = 0;

#ifdef _TARGET_ARM64_
            // For a >= 16-byte structSize we will generate a ldp and stp instruction each loop
            //             ldp     x2, x3, [x0]
            //             stp     x2, x3, [sp, #16]

            while (remainingSize >= 2 * TARGET_POINTER_SIZE)
            {
                var_types type0 = compiler->getJitGCType(gcPtrs[nextIndex + 0]);
                var_types type1 = compiler->getJitGCType(gcPtrs[nextIndex + 1]);

                if (varNode != nullptr)
                {
                    // Load from our varNumImp source
                    emit->emitIns_R_R_S_S(INS_ldp, emitTypeSize(type0), emitTypeSize(type1), loReg, hiReg,
                                          varNode->GetLclNum(), structOffset);
                }
                else
                {
                    // check for case of destroying the addrRegister while we still need it
                    assert(loReg != addrReg);
                    noway_assert((remainingSize == 2 * TARGET_POINTER_SIZE) || (hiReg != addrReg));

                    // Load from our address expression source
                    emit->emitIns_R_R_R_I(INS_ldp, emitTypeSize(type0), loReg, hiReg, addrReg, structOffset,
                                          INS_OPTS_NONE, emitTypeSize(type0));
                }

                // Emit stp instruction to store the two registers into the outgoing argument area
                emit->emitIns_S_S_R_R(INS_stp, emitTypeSize(type0), emitTypeSize(type1), loReg, hiReg, varNumOut,
                                      argOffsetOut);
                argOffsetOut += (2 * TARGET_POINTER_SIZE); // We stored 16-bytes of the struct
                assert(argOffsetOut <= argOffsetMax);      // We can't write beyound the outgoing area area

                remainingSize -= (2 * TARGET_POINTER_SIZE); // We loaded 16-bytes of the struct
                structOffset += (2 * TARGET_POINTER_SIZE);
                nextIndex += 2;
            }
#else  // _TARGET_ARM_
            // For a >= 4 byte structSize we will generate a ldr and str instruction each loop
            //             ldr     r2, [r0]
            //             str     r2, [sp, #16]
            while (remainingSize >= TARGET_POINTER_SIZE)
            {
                var_types type = compiler->getJitGCType(gcPtrs[nextIndex]);

                if (varNode != nullptr)
                {
                    // Load from our varNumImp source
                    emit->emitIns_R_S(INS_ldr, emitTypeSize(type), loReg, varNode->GetLclNum(), structOffset);
                }
                else
                {
                    // check for case of destroying the addrRegister while we still need it
                    assert(loReg != addrReg || remainingSize == TARGET_POINTER_SIZE);

                    // Load from our address expression source
                    emit->emitIns_R_R_I(INS_ldr, emitTypeSize(type), loReg, addrReg, structOffset);
                }

                // Emit str instruction to store the register into the outgoing argument area
                emit->emitIns_S_R(INS_str, emitTypeSize(type), loReg, varNumOut, argOffsetOut);
                argOffsetOut += TARGET_POINTER_SIZE;  // We stored 4-bytes of the struct
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area

                remainingSize -= TARGET_POINTER_SIZE; // We loaded 4-bytes of the struct
                structOffset += TARGET_POINTER_SIZE;
                nextIndex += 1;
            }
#endif // _TARGET_ARM_

            // For a 12-byte structSize we will we will generate two load instructions
            //             ldr     x2, [x0]
            //             ldr     w3, [x0, #8]
            //             str     x2, [sp, #16]
            //             str     w3, [sp, #24]

            while (remainingSize > 0)
            {
                if (remainingSize >= TARGET_POINTER_SIZE)
                {
                    var_types nextType = compiler->getJitGCType(gcPtrs[nextIndex]);
                    emitAttr  nextAttr = emitTypeSize(nextType);
                    remainingSize -= TARGET_POINTER_SIZE;

                    if (varNode != nullptr)
                    {
                        // Load from our varNumImp source
                        emit->emitIns_R_S(ins_Load(nextType), nextAttr, loReg, varNode->GetLclNum(), structOffset);
                    }
                    else
                    {
                        assert(loReg != addrReg);

                        // Load from our address expression source
                        emit->emitIns_R_R_I(ins_Load(nextType), nextAttr, loReg, addrReg, structOffset);
                    }
                    // Emit a store instruction to store the register into the outgoing argument area
                    emit->emitIns_S_R(ins_Store(nextType), nextAttr, loReg, varNumOut, argOffsetOut);
                    argOffsetOut += EA_SIZE_IN_BYTES(nextAttr);
                    assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area

                    structOffset += TARGET_POINTER_SIZE;
                    nextIndex++;
                }
                else // (remainingSize < TARGET_POINTER_SIZE)
                {
                    int loadSize  = remainingSize;
                    remainingSize = 0;

                    // We should never have to do a non-pointer sized load when we have a LclVar source
                    assert(varNode == nullptr);

                    // the left over size is smaller than a pointer and thus can never be a GC type
                    assert(varTypeIsGC(compiler->getJitGCType(gcPtrs[nextIndex])) == false);

                    var_types loadType = TYP_UINT;
                    if (loadSize == 1)
                    {
                        loadType = TYP_UBYTE;
                    }
                    else if (loadSize == 2)
                    {
                        loadType = TYP_USHORT;
                    }
                    else
                    {
                        // Need to handle additional loadSize cases here
                        noway_assert(loadSize == 4);
                    }

                    instruction loadIns  = ins_Load(loadType);
                    emitAttr    loadAttr = emitAttr(loadSize);

                    assert(loReg != addrReg);

                    emit->emitIns_R_R_I(loadIns, loadAttr, loReg, addrReg, structOffset);

                    // Emit a store instruction to store the register into the outgoing argument area
                    emit->emitIns_S_R(ins_Store(loadType), loadAttr, loReg, varNumOut, argOffsetOut);
                    argOffsetOut += EA_SIZE_IN_BYTES(loadAttr);
                    assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
                }
            }
        }
    }
}

//---------------------------------------------------------------------
// genPutArgReg - generate code for a GT_PUTARG_REG node
//
// Arguments
//    tree - the GT_PUTARG_REG node
//
// Return value:
//    None
//
void CodeGen::genPutArgReg(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_PUTARG_REG));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->gtRegNum;

    assert(targetType != TYP_STRUCT);

    GenTree* op1 = tree->gtOp1;
    genConsumeReg(op1);

    // If child node is not already in the register we need, move it
    if (targetReg != op1->gtRegNum)
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, op1->gtRegNum, targetType);
    }

    genProduceReg(tree);
}

#if FEATURE_ARG_SPLIT
//---------------------------------------------------------------------
// genPutArgSplit - generate code for a GT_PUTARG_SPLIT node
//
// Arguments
//    tree - the GT_PUTARG_SPLIT node
//
// Return value:
//    None
//
void CodeGen::genPutArgSplit(GenTreePutArgSplit* treeNode)
{
    assert(treeNode->OperIs(GT_PUTARG_SPLIT));

    GenTree* source       = treeNode->gtOp1;
    emitter* emit         = getEmitter();
    unsigned varNumOut    = compiler->lvaOutgoingArgSpaceVar;
    unsigned argOffsetMax = compiler->lvaOutgoingArgSpaceSize;
    unsigned argOffsetOut = treeNode->gtSlotNum * TARGET_POINTER_SIZE;

    if (source->OperGet() == GT_FIELD_LIST)
    {
        // Evaluate each of the GT_FIELD_LIST items into their register
        // and store their register into the outgoing argument area
        unsigned regIndex = 0;
        for (GenTreeFieldList* fieldListPtr = source->AsFieldList(); fieldListPtr != nullptr;
             fieldListPtr                   = fieldListPtr->Rest())
        {
            GenTree*  nextArgNode = fieldListPtr->gtGetOp1();
            regNumber fieldReg    = nextArgNode->gtRegNum;
            genConsumeReg(nextArgNode);

            if (regIndex >= treeNode->gtNumRegs)
            {
                var_types type = nextArgNode->TypeGet();
                emitAttr  attr = emitTypeSize(type);

                // Emit store instructions to store the registers produced by the GT_FIELD_LIST into the outgoing
                // argument area
                emit->emitIns_S_R(ins_Store(type), attr, fieldReg, varNumOut, argOffsetOut);
                argOffsetOut += EA_SIZE_IN_BYTES(attr);
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            }
            else
            {
                var_types type   = treeNode->GetRegType(regIndex);
                regNumber argReg = treeNode->GetRegNumByIdx(regIndex);
#ifdef _TARGET_ARM_
                if (type == TYP_LONG)
                {
                    // We should only see long fields for DOUBLEs passed in 2 integer registers, via bitcast.
                    // All other LONGs should have been decomposed.
                    // Handle the first INT, and then handle the 2nd below.
                    assert(nextArgNode->OperIs(GT_BITCAST));
                    type = TYP_INT;
                    if (argReg != fieldReg)
                    {
                        inst_RV_RV(ins_Copy(type), argReg, fieldReg, type);
                    }
                    // Now set up the next register for the 2nd INT
                    argReg = REG_NEXT(argReg);
                    regIndex++;
                    assert(argReg == treeNode->GetRegNumByIdx(regIndex));
                    fieldReg = nextArgNode->AsMultiRegOp()->GetRegNumByIdx(1);
                }
#endif // _TARGET_ARM_

                // If child node is not already in the register we need, move it
                if (argReg != fieldReg)
                {
                    inst_RV_RV(ins_Copy(type), argReg, fieldReg, type);
                }
                regIndex++;
            }
        }
    }
    else
    {
        var_types targetType = source->TypeGet();
        assert(source->OperGet() == GT_OBJ);
        assert(varTypeIsStruct(targetType));

        regNumber baseReg = treeNode->ExtractTempReg();
        regNumber addrReg = REG_NA;

        GenTreeLclVarCommon* varNode  = nullptr;
        GenTree*             addrNode = nullptr;

        addrNode = source->gtOp.gtOp1;

        // addrNode can either be a GT_LCL_VAR_ADDR or an address expression
        //
        if (addrNode->OperGet() == GT_LCL_VAR_ADDR)
        {
            // We have a GT_OBJ(GT_LCL_VAR_ADDR)
            //
            // We will treat this case the same as above
            // (i.e if we just had this GT_LCL_VAR directly as the source)
            // so update 'source' to point this GT_LCL_VAR_ADDR node
            // and continue to the codegen for the LCL_VAR node below
            //
            varNode  = addrNode->AsLclVarCommon();
            addrNode = nullptr;
        }

        // Either varNode or addrNOde must have been setup above,
        // the xor ensures that only one of the two is setup, not both
        assert((varNode != nullptr) ^ (addrNode != nullptr));

        // Setup the structSize, isHFa, and gcPtrCount
        BYTE*    gcPtrs     = treeNode->gtGcPtrs;
        unsigned gcPtrCount = treeNode->gtNumberReferenceSlots; // The count of GC pointers in the struct
        int      structSize = treeNode->getArgSize();

        // This is the varNum for our load operations,
        // only used when we have a struct with a LclVar source
        unsigned srcVarNum = BAD_VAR_NUM;

        if (varNode != nullptr)
        {
            srcVarNum = varNode->gtLclNum;
            assert(srcVarNum < compiler->lvaCount);

            // handle promote situation
            LclVarDsc* varDsc = compiler->lvaTable + srcVarNum;

            // This struct also must live in the stack frame
            // And it can't live in a register (SIMD)
            assert(varDsc->lvType == TYP_STRUCT);
            assert(varDsc->lvOnFrame && !varDsc->lvRegister);

            // We don't split HFA struct
            assert(!varDsc->lvIsHfa());
        }
        else // addrNode is used
        {
            assert(addrNode != nullptr);

            // Generate code to load the address that we need into a register
            genConsumeAddress(addrNode);
            addrReg = addrNode->gtRegNum;

            // If addrReg equal to baseReg, we use the last target register as alternative baseReg.
            // Because the candidate mask for the internal baseReg does not include any of the target register,
            // we can ensure that baseReg, addrReg, and the last target register are not all same.
            assert(baseReg != addrReg);

            // We don't split HFA struct
            assert(!compiler->IsHfa(source->gtObj.gtClass));
        }

        // Put on stack first
        unsigned nextIndex     = treeNode->gtNumRegs;
        unsigned structOffset  = nextIndex * TARGET_POINTER_SIZE;
        int      remainingSize = structSize - structOffset;

        // remainingSize is always multiple of TARGET_POINTER_SIZE
        assert(remainingSize % TARGET_POINTER_SIZE == 0);
        while (remainingSize > 0)
        {
            var_types type = compiler->getJitGCType(gcPtrs[nextIndex]);

            if (varNode != nullptr)
            {
                // Load from our varNumImp source
                emit->emitIns_R_S(INS_ldr, emitTypeSize(type), baseReg, srcVarNum, structOffset);
            }
            else
            {
                // check for case of destroying the addrRegister while we still need it
                assert(baseReg != addrReg);

                // Load from our address expression source
                emit->emitIns_R_R_I(INS_ldr, emitTypeSize(type), baseReg, addrReg, structOffset);
            }

            // Emit str instruction to store the register into the outgoing argument area
            emit->emitIns_S_R(INS_str, emitTypeSize(type), baseReg, varNumOut, argOffsetOut);
            argOffsetOut += TARGET_POINTER_SIZE;  // We stored 4-bytes of the struct
            assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            remainingSize -= TARGET_POINTER_SIZE; // We loaded 4-bytes of the struct
            structOffset += TARGET_POINTER_SIZE;
            nextIndex += 1;
        }

        // We set up the registers in order, so that we assign the last target register `baseReg` is no longer in use,
        // in case we had to reuse the last target register for it.
        structOffset = 0;
        for (unsigned idx = 0; idx < treeNode->gtNumRegs; idx++)
        {
            regNumber targetReg = treeNode->GetRegNumByIdx(idx);
            var_types type      = treeNode->GetRegType(idx);

            if (varNode != nullptr)
            {
                // Load from our varNumImp source
                emit->emitIns_R_S(INS_ldr, emitTypeSize(type), targetReg, srcVarNum, structOffset);
            }
            else
            {
                // check for case of destroying the addrRegister while we still need it
                if (targetReg == addrReg && idx != treeNode->gtNumRegs - 1)
                {
                    assert(targetReg != baseReg);
                    emit->emitIns_R_R(INS_mov, emitActualTypeSize(type), baseReg, addrReg);
                    addrReg = baseReg;
                }

                // Load from our address expression source
                emit->emitIns_R_R_I(INS_ldr, emitTypeSize(type), targetReg, addrReg, structOffset);
            }
            structOffset += TARGET_POINTER_SIZE;
        }
    }
    genProduceReg(treeNode);
}
#endif // FEATURE_ARG_SPLIT

//----------------------------------------------------------------------------------
// genMultiRegCallStoreToLocal: store multi-reg return value of a call node to a local
//
// Arguments:
//    treeNode  -  Gentree of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
// Assumption:
//    The child of store is a multi-reg call node.
//    genProduceReg() on treeNode is made by caller of this routine.
//
void CodeGen::genMultiRegCallStoreToLocal(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_STORE_LCL_VAR);

#if defined(_TARGET_ARM_)
    // Longs are returned in two return registers on Arm32.
    // Structs are returned in four registers on ARM32 and HFAs.
    assert(varTypeIsLong(treeNode) || varTypeIsStruct(treeNode));
#elif defined(_TARGET_ARM64_)
    // Structs of size >=9 and <=16 are returned in two return registers on ARM64 and HFAs.
    assert(varTypeIsStruct(treeNode));
#endif // _TARGET_*

    // Assumption: current implementation requires that a multi-reg
    // var in 'var = call' is flagged as lvIsMultiRegRet to prevent it from
    // being promoted.
    unsigned   lclNum = treeNode->AsLclVarCommon()->gtLclNum;
    LclVarDsc* varDsc = &(compiler->lvaTable[lclNum]);
    noway_assert(varDsc->lvIsMultiRegRet);

    GenTree*     op1       = treeNode->gtGetOp1();
    GenTree*     actualOp1 = op1->gtSkipReloadOrCopy();
    GenTreeCall* call      = actualOp1->AsCall();
    assert(call->HasMultiRegRetVal());

    genConsumeRegs(op1);

    ReturnTypeDesc* pRetTypeDesc = call->GetReturnTypeDesc();
    unsigned        regCount     = pRetTypeDesc->GetReturnRegCount();

    if (treeNode->gtRegNum != REG_NA)
    {
        // Right now the only enregistrable multi-reg return types supported are SIMD types.
        assert(varTypeIsSIMD(treeNode));
        assert(regCount != 0);

        regNumber dst = treeNode->gtRegNum;

        // Treat dst register as a homogenous vector with element size equal to the src size
        // Insert pieces in reverse order
        for (int i = regCount - 1; i >= 0; --i)
        {
            var_types type = pRetTypeDesc->GetReturnRegType(i);
            regNumber reg  = call->GetRegNumByIdx(i);
            if (op1->IsCopyOrReload())
            {
                // GT_COPY/GT_RELOAD will have valid reg for those positions
                // that need to be copied or reloaded.
                regNumber reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(i);
                if (reloadReg != REG_NA)
                {
                    reg = reloadReg;
                }
            }

            assert(reg != REG_NA);
            if (varTypeIsFloating(type))
            {
                // If the register piece was passed in a floating point register
                // Use a vector mov element instruction
                // src is not a vector, so it is in the first element reg[0]
                // mov dst[i], reg[0]
                // This effectively moves from `reg[0]` to `dst[i]`, leaving other dst bits unchanged till further
                // iterations
                // For the case where reg == dst, if we iterate so that we write dst[0] last, we eliminate the need for
                // a temporary
                getEmitter()->emitIns_R_R_I_I(INS_mov, emitTypeSize(type), dst, reg, i, 0);
            }
            else
            {
                // If the register piece was passed in an integer register
                // Use a vector mov from general purpose register instruction
                // mov dst[i], reg
                // This effectively moves from `reg` to `dst[i]`
                getEmitter()->emitIns_R_R_I(INS_mov, emitTypeSize(type), dst, reg, i);
            }
        }

        genProduceReg(treeNode);
    }
    else
    {
        // Stack store
        int offset = 0;
        for (unsigned i = 0; i < regCount; ++i)
        {
            var_types type = pRetTypeDesc->GetReturnRegType(i);
            regNumber reg  = call->GetRegNumByIdx(i);
            if (op1->IsCopyOrReload())
            {
                // GT_COPY/GT_RELOAD will have valid reg for those positions
                // that need to be copied or reloaded.
                regNumber reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(i);
                if (reloadReg != REG_NA)
                {
                    reg = reloadReg;
                }
            }

            assert(reg != REG_NA);
            getEmitter()->emitIns_S_R(ins_Store(type), emitTypeSize(type), reg, lclNum, offset);
            offset += genTypeSize(type);
        }

        genUpdateLife(treeNode);
        varDsc->lvRegNum = REG_STK;
    }
}

//------------------------------------------------------------------------
// genRangeCheck: generate code for GT_ARR_BOUNDS_CHECK node.
//
void CodeGen::genRangeCheck(GenTree* oper)
{
    noway_assert(oper->OperIsBoundsCheck());
    GenTreeBoundsChk* bndsChk = oper->AsBoundsChk();

    GenTree* arrLen    = bndsChk->gtArrLen;
    GenTree* arrIndex  = bndsChk->gtIndex;
    GenTree* arrRef    = NULL;
    int      lenOffset = 0;

    GenTree*     src1;
    GenTree*     src2;
    emitJumpKind jmpKind;

    genConsumeRegs(arrIndex);
    genConsumeRegs(arrLen);

    if (arrIndex->isContainedIntOrIImmed())
    {
        // To encode using a cmp immediate, we place the
        //  constant operand in the second position
        src1    = arrLen;
        src2    = arrIndex;
        jmpKind = EJ_ls;
    }
    else
    {
        src1    = arrIndex;
        src2    = arrLen;
        jmpKind = EJ_hs;
    }

    var_types bndsChkType = genActualType(src2->TypeGet());
#if DEBUG
    // Bounds checks can only be 32 or 64 bit sized comparisons.
    assert(bndsChkType == TYP_INT || bndsChkType == TYP_LONG);

    // The type of the bounds check should always wide enough to compare against the index.
    assert(emitTypeSize(bndsChkType) >= emitActualTypeSize(src1->TypeGet()));
#endif // DEBUG

    getEmitter()->emitInsBinary(INS_cmp, emitActualTypeSize(bndsChkType), src1, src2);
    genJumpToThrowHlpBlk(jmpKind, bndsChk->gtThrowKind, bndsChk->gtIndRngFailBB);
}

//---------------------------------------------------------------------
// genCodeForPhysReg - generate code for a GT_PHYSREG node
//
// Arguments
//    tree - the GT_PHYSREG node
//
// Return value:
//    None
//
void CodeGen::genCodeForPhysReg(GenTreePhysReg* tree)
{
    assert(tree->OperIs(GT_PHYSREG));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->gtRegNum;

    if (targetReg != tree->gtSrcReg)
    {
        inst_RV_RV(ins_Copy(targetType), targetReg, tree->gtSrcReg, targetType);
        genTransferRegGCState(targetReg, tree->gtSrcReg);
    }

    genProduceReg(tree);
}

//---------------------------------------------------------------------
// genCodeForNullCheck - generate code for a GT_NULLCHECK node
//
// Arguments
//    tree - the GT_NULLCHECK node
//
// Return value:
//    None
//
void CodeGen::genCodeForNullCheck(GenTreeOp* tree)
{
    assert(tree->OperIs(GT_NULLCHECK));
    assert(!tree->gtOp1->isContained());
    regNumber addrReg = genConsumeReg(tree->gtOp1);

#ifdef _TARGET_ARM64_
    regNumber targetReg = REG_ZR;
#else
    regNumber targetReg = tree->GetSingleTempReg();
#endif

    getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, targetReg, addrReg, 0);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLowerBound: Returns the offset from the Array object to the
//   lower bound for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayLowerBound(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * (dimension + rank);
}

//------------------------------------------------------------------------
// genOffsetOfMDArrayLength: Returns the offset from the Array object to the
//   size for the given dimension.
//
// Arguments:
//    elemType  - the element type of the array
//    rank      - the rank of the array
//    dimension - the dimension for which the lower bound offset will be returned.
//
// Return Value:
//    The offset.
// TODO-Cleanup: move to CodeGenCommon.cpp

// static
unsigned CodeGen::genOffsetOfMDArrayDimensionSize(var_types elemType, unsigned rank, unsigned dimension)
{
    // Note that the lower bound and length fields of the Array object are always TYP_INT
    return compiler->eeGetArrayDataOffset(elemType) + genTypeSize(TYP_INT) * dimension;
}

//------------------------------------------------------------------------
// genCodeForArrIndex: Generates code to bounds check the index for one dimension of an array reference,
//                     producing the effective index by subtracting the lower bound.
//
// Arguments:
//    arrIndex - the node for which we're generating code
//
// Return Value:
//    None.
//
void CodeGen::genCodeForArrIndex(GenTreeArrIndex* arrIndex)
{
    emitter*  emit      = getEmitter();
    GenTree*  arrObj    = arrIndex->ArrObj();
    GenTree*  indexNode = arrIndex->IndexExpr();
    regNumber arrReg    = genConsumeReg(arrObj);
    regNumber indexReg  = genConsumeReg(indexNode);
    regNumber tgtReg    = arrIndex->gtRegNum;
    noway_assert(tgtReg != REG_NA);

    // We will use a temp register to load the lower bound and dimension size values.

    regNumber tmpReg = arrIndex->GetSingleTempReg();
    assert(tgtReg != tmpReg);

    unsigned  dim      = arrIndex->gtCurrDim;
    unsigned  rank     = arrIndex->gtArrRank;
    var_types elemType = arrIndex->gtArrElemType;
    unsigned  offset;

    offset = genOffsetOfMDArrayLowerBound(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_PTRSIZE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R_R(INS_sub, EA_4BYTE, tgtReg, indexReg, tmpReg);

    offset = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);
    emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_PTRSIZE, tmpReg, arrReg, offset); // a 4 BYTE sign extending load
    emit->emitIns_R_R(INS_cmp, EA_4BYTE, tgtReg, tmpReg);

    genJumpToThrowHlpBlk(EJ_hs, SCK_RNGCHK_FAIL);

    genProduceReg(arrIndex);
}

//------------------------------------------------------------------------
// genCodeForArrOffset: Generates code to compute the flattened array offset for
//    one dimension of an array reference:
//        result = (prevDimOffset * dimSize) + effectiveIndex
//    where dimSize is obtained from the arrObj operand
//
// Arguments:
//    arrOffset - the node for which we're generating code
//
// Return Value:
//    None.
//
// Notes:
//    dimSize and effectiveIndex are always non-negative, the former by design,
//    and the latter because it has been normalized to be zero-based.

void CodeGen::genCodeForArrOffset(GenTreeArrOffs* arrOffset)
{
    GenTree*  offsetNode = arrOffset->gtOffset;
    GenTree*  indexNode  = arrOffset->gtIndex;
    regNumber tgtReg     = arrOffset->gtRegNum;

    noway_assert(tgtReg != REG_NA);

    if (!offsetNode->IsIntegralConst(0))
    {
        emitter*  emit      = getEmitter();
        regNumber offsetReg = genConsumeReg(offsetNode);
        regNumber indexReg  = genConsumeReg(indexNode);
        regNumber arrReg    = genConsumeReg(arrOffset->gtArrObj);
        noway_assert(offsetReg != REG_NA);
        noway_assert(indexReg != REG_NA);
        noway_assert(arrReg != REG_NA);

        regNumber tmpReg = arrOffset->GetSingleTempReg();

        unsigned  dim      = arrOffset->gtCurrDim;
        unsigned  rank     = arrOffset->gtArrRank;
        var_types elemType = arrOffset->gtArrElemType;
        unsigned  offset   = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);

        // Load tmpReg with the dimension size and evaluate
        // tgtReg = offsetReg*tmpReg + indexReg.
        emit->emitIns_R_R_I(ins_Load(TYP_INT), EA_PTRSIZE, tmpReg, arrReg, offset);
        emit->emitIns_R_R_R_R(INS_MULADD, EA_PTRSIZE, tgtReg, tmpReg, offsetReg, indexReg);
    }
    else
    {
        regNumber indexReg = genConsumeReg(indexNode);
        if (indexReg != tgtReg)
        {
            inst_RV_RV(INS_mov, tgtReg, indexReg, TYP_INT);
        }
    }
    genProduceReg(arrOffset);
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
void CodeGen::genCodeForShift(GenTree* tree)
{
    var_types   targetType = tree->TypeGet();
    genTreeOps  oper       = tree->OperGet();
    instruction ins        = genGetInsForOper(oper, targetType);
    emitAttr    size       = emitActualTypeSize(tree);

    assert(tree->gtRegNum != REG_NA);

    genConsumeOperands(tree->AsOp());

    GenTree* operand = tree->gtGetOp1();
    GenTree* shiftBy = tree->gtGetOp2();
    if (!shiftBy->IsCnsIntOrI())
    {
        getEmitter()->emitIns_R_R_R(ins, size, tree->gtRegNum, operand->gtRegNum, shiftBy->gtRegNum);
    }
    else
    {
        unsigned immWidth   = emitter::getBitWidth(size); // For ARM64, immWidth will be set to 32 or 64
        unsigned shiftByImm = (unsigned)shiftBy->gtIntCon.gtIconVal & (immWidth - 1);

        getEmitter()->emitIns_R_R_I(ins, size, tree->gtRegNum, operand->gtRegNum, shiftByImm);
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForLclAddr: Generates the code for GT_LCL_FLD_ADDR/GT_LCL_VAR_ADDR.
//
// Arguments:
//    tree - the node.
//
void CodeGen::genCodeForLclAddr(GenTree* tree)
{
    assert(tree->OperIs(GT_LCL_FLD_ADDR, GT_LCL_VAR_ADDR));

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->gtRegNum;

    // Address of a local var.
    noway_assert((targetType == TYP_BYREF) || (targetType == TYP_I_IMPL));

    emitAttr size = emitTypeSize(targetType);

    inst_RV_TT(INS_lea, targetReg, tree, 0, size);
    genProduceReg(tree);
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

    var_types targetType = tree->TypeGet();
    regNumber targetReg  = tree->gtRegNum;
    emitter*  emit       = getEmitter();

    NYI_IF(targetType == TYP_STRUCT, "GT_LCL_FLD: struct load local field not supported");
    assert(targetReg != REG_NA);

    emitAttr size   = emitTypeSize(targetType);
    unsigned offs   = tree->gtLclOffs;
    unsigned varNum = tree->gtLclNum;
    assert(varNum < compiler->lvaCount);

    if (varTypeIsFloating(targetType) || varTypeIsSIMD(targetType))
    {
        emit->emitIns_R_S(ins_Load(targetType), size, targetReg, varNum, offs);
    }
    else
    {
#ifdef _TARGET_ARM64_
        size = EA_SET_SIZE(size, EA_8BYTE);
#endif // _TARGET_ARM64_
        emit->emitIns_R_S(ins_Move_Extend(targetType, false), size, targetReg, varNum, offs);
    }

    genProduceReg(tree);
}

//------------------------------------------------------------------------
// genCodeForIndexAddr: Produce code for a GT_INDEX_ADDR node.
//
// Arguments:
//    tree - the GT_INDEX_ADDR node
//
void CodeGen::genCodeForIndexAddr(GenTreeIndexAddr* node)
{
    GenTree* const base  = node->Arr();
    GenTree* const index = node->Index();

    genConsumeReg(base);
    genConsumeReg(index);

    // NOTE: `genConsumeReg` marks the consumed register as not a GC pointer, as it assumes that the input registers
    // die at the first instruction generated by the node. This is not the case for `INDEX_ADDR`, however, as the
    // base register is multiply-used. As such, we need to mark the base register as containing a GC pointer until
    // we are finished generating the code for this node.

    gcInfo.gcMarkRegPtrVal(base->gtRegNum, base->TypeGet());
    assert(!varTypeIsGC(index->TypeGet()));

    const regNumber tmpReg = node->GetSingleTempReg();

    // Generate the bounds check if necessary.
    if ((node->gtFlags & GTF_INX_RNGCHK) != 0)
    {
        // Create a GT_IND(GT_LEA)) tree for the array length access and load the length into a register.
        GenTreeAddrMode arrLenAddr(base->TypeGet(), base, nullptr, 0, static_cast<unsigned>(node->gtLenOffset));
        arrLenAddr.gtRegNum = REG_NA;
        arrLenAddr.SetContained();

        GenTreeIndir arrLen = indirForm(TYP_INT, &arrLenAddr);
        arrLen.gtRegNum     = tmpReg;
        arrLen.ClearContained();

        getEmitter()->emitInsLoadStoreOp(ins_Load(TYP_INT), emitTypeSize(TYP_INT), arrLen.gtRegNum, &arrLen);

#ifdef _TARGET_64BIT_
        // The CLI Spec allows an array to be indexed by either an int32 or a native int.  In the case that the index
        // is a native int on a 64-bit platform, we will need to widen the array length and the compare.
        if (index->TypeGet() == TYP_I_IMPL)
        {
            // Extend the array length as needed.
            getEmitter()->emitIns_R_R(ins_Move_Extend(TYP_INT, true), EA_8BYTE, arrLen.gtRegNum, arrLen.gtRegNum);
        }
#endif

        // Generate the range check.
        getEmitter()->emitInsBinary(INS_cmp, emitActualTypeSize(TYP_I_IMPL), index, &arrLen);
        genJumpToThrowHlpBlk(EJ_hs, SCK_RNGCHK_FAIL, node->gtIndRngFailBB);
    }

    // Can we use a ScaledAdd instruction?
    //
    if (isPow2(node->gtElemSize) && (node->gtElemSize <= 32768))
    {
        DWORD scale;
        BitScanForward(&scale, node->gtElemSize);

        // dest = base + index * scale
        genScaledAdd(emitActualTypeSize(node), node->gtRegNum, base->gtRegNum, index->gtRegNum, scale);
    }
    else // we have to load the element size and use a MADD (multiply-add) instruction
    {
        // tmpReg = element size
        CodeGen::genSetRegToIcon(tmpReg, (ssize_t)node->gtElemSize, TYP_INT);

        // dest = index * tmpReg + base
        getEmitter()->emitIns_R_R_R_R(INS_MULADD, emitActualTypeSize(node), node->gtRegNum, index->gtRegNum, tmpReg,
                                      base->gtRegNum);
    }

    // dest = dest + elemOffs
    getEmitter()->emitIns_R_R_I(INS_add, emitActualTypeSize(node), node->gtRegNum, node->gtRegNum, node->gtElemOffset);

    gcInfo.gcMarkRegSetNpt(base->gtGetRegMask());

    genProduceReg(node);
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

    var_types   targetType = tree->TypeGet();
    regNumber   targetReg  = tree->gtRegNum;
    emitter*    emit       = getEmitter();
    emitAttr    attr       = emitTypeSize(tree);
    instruction ins        = ins_Load(targetType);

#ifdef FEATURE_SIMD
    // Handling of Vector3 type values loaded through indirection.
    if (tree->TypeGet() == TYP_SIMD12)
    {
        genLoadIndTypeSIMD12(tree);
        return;
    }
#endif // FEATURE_SIMD

    genConsumeAddress(tree->Addr());
    if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
    {
        bool isAligned = ((tree->gtFlags & GTF_IND_UNALIGNED) == 0);

        assert((attr != EA_1BYTE) || isAligned);

#ifdef _TARGET_ARM64_
        GenTree* addr           = tree->Addr();
        bool     useLoadAcquire = genIsValidIntReg(targetReg) && !addr->isContained() &&
                              (varTypeIsUnsigned(targetType) || varTypeIsI(targetType)) &&
                              !(tree->gtFlags & GTF_IND_UNALIGNED);

        if (useLoadAcquire)
        {
            switch (EA_SIZE(attr))
            {
                case EA_1BYTE:
                    assert(ins == INS_ldrb);
                    ins = INS_ldarb;
                    break;
                case EA_2BYTE:
                    assert(ins == INS_ldrh);
                    ins = INS_ldarh;
                    break;
                case EA_4BYTE:
                case EA_8BYTE:
                    assert(ins == INS_ldr);
                    ins = INS_ldar;
                    break;
                default:
                    assert(false); // We should not get here
            }
        }

        emit->emitInsLoadStoreOp(ins, attr, targetReg, tree);

        if (!useLoadAcquire) // issue a INS_BARRIER_OSHLD after a volatile LdInd operation
            instGen_MemoryBarrier(INS_BARRIER_OSHLD);
#else
        emit->emitInsLoadStoreOp(ins, attr, targetReg, tree);

        // issue a full memory barrier after a volatile LdInd operation
        instGen_MemoryBarrier();
#endif // _TARGET_ARM64_
    }
    else
    {
        emit->emitInsLoadStoreOp(ins, attr, targetReg, tree);
    }

    genProduceReg(tree);
}

// Generate code for a CpBlk node by the means of the VM memcpy helper call
// Preconditions:
// a) The size argument of the CpBlk is not an integer constant
// b) The size argument is a constant but is larger than CPBLK_MOVS_LIMIT bytes.
void CodeGen::genCodeForCpBlk(GenTreeBlk* cpBlkNode)
{
    // Make sure we got the arguments of the cpblk operation in the right registers
    unsigned blockSize = cpBlkNode->Size();
    GenTree* dstAddr   = cpBlkNode->Addr();
    assert(!dstAddr->isContained());

    genConsumeBlockOp(cpBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);

#ifdef _TARGET_ARM64_
    if (blockSize != 0)
    {
        assert(blockSize > CPBLK_UNROLL_LIMIT);
    }
#endif // _TARGET_ARM64_

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile CpBlk operation
        instGen_MemoryBarrier();
    }

    genEmitHelperCall(CORINFO_HELP_MEMCPY, 0, EA_UNKNOWN);

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
#ifdef _TARGET_ARM64_
        // issue a INS_BARRIER_ISHLD after a volatile CpBlk operation
        instGen_MemoryBarrier(INS_BARRIER_ISHLD);
#else
        // issue a full memory barrier after a volatile CpBlk operation
        instGen_MemoryBarrier();
#endif // _TARGET_ARM64_
    }
}

//----------------------------------------------------------------------------------
// genCodeForCpBlkUnroll: Generates CpBlk code by performing a loop unroll
//
// Arguments:
//    cpBlkNode  -  Copy block node
//
// Return Value:
//    None
//
// Assumption:
//  The size argument of the CpBlk node is a constant and <= CPBLK_UNROLL_LIMIT bytes.
//
void CodeGen::genCodeForCpBlkUnroll(GenTreeBlk* cpBlkNode)
{
    // Make sure we got the arguments of the cpblk operation in the right registers
    unsigned size    = cpBlkNode->Size();
    GenTree* dstAddr = cpBlkNode->Addr();
    GenTree* source  = cpBlkNode->Data();
    GenTree* srcAddr = nullptr;

    assert((size != 0) && (size <= CPBLK_UNROLL_LIMIT));

    emitter* emit = getEmitter();

    if (dstAddr->isUsedFromReg())
    {
        genConsumeReg(dstAddr);
    }

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile CpBlkUnroll operation
        instGen_MemoryBarrier();
    }

    if (source->gtOper == GT_IND)
    {
        srcAddr = source->gtGetOp1();
        if (srcAddr->isUsedFromReg())
        {
            genConsumeReg(srcAddr);
        }
    }
    else
    {
        noway_assert(source->IsLocal());
        // TODO-Cleanup: Consider making the addrForm() method in Rationalize public, e.g. in GenTree.
        // OR: transform source to GT_IND(GT_LCL_VAR_ADDR)
        if (source->OperGet() == GT_LCL_VAR)
        {
            source->SetOper(GT_LCL_VAR_ADDR);
        }
        else
        {
            assert(source->OperGet() == GT_LCL_FLD);
            source->SetOper(GT_LCL_FLD_ADDR);
        }
        srcAddr = source;
    }

    unsigned offset = 0;

    // Grab the integer temp register to emit the loads and stores.
    regNumber tmpReg = cpBlkNode->ExtractTempReg(RBM_ALLINT);

#ifdef _TARGET_ARM64_
    if (size >= 2 * REGSIZE_BYTES)
    {
        regNumber tmp2Reg = cpBlkNode->ExtractTempReg(RBM_ALLINT);

        size_t slots = size / (2 * REGSIZE_BYTES);

        while (slots-- > 0)
        {
            // Load
            genCodeForLoadPairOffset(tmpReg, tmp2Reg, srcAddr, offset);
            // Store
            genCodeForStorePairOffset(tmpReg, tmp2Reg, dstAddr, offset);
            offset += 2 * REGSIZE_BYTES;
        }
    }

    // Fill the remainder (15 bytes or less) if there's one.
    if ((size & 0xf) != 0)
    {
        if ((size & 8) != 0)
        {
            genCodeForLoadOffset(INS_ldr, EA_8BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_str, EA_8BYTE, tmpReg, dstAddr, offset);
            offset += 8;
        }
        if ((size & 4) != 0)
        {
            genCodeForLoadOffset(INS_ldr, EA_4BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_str, EA_4BYTE, tmpReg, dstAddr, offset);
            offset += 4;
        }
        if ((size & 2) != 0)
        {
            genCodeForLoadOffset(INS_ldrh, EA_2BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_strh, EA_2BYTE, tmpReg, dstAddr, offset);
            offset += 2;
        }
        if ((size & 1) != 0)
        {
            genCodeForLoadOffset(INS_ldrb, EA_1BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_strb, EA_1BYTE, tmpReg, dstAddr, offset);
        }
    }
#else  // !_TARGET_ARM64_
    size_t slots = size / REGSIZE_BYTES;
    while (slots-- > 0)
    {
        genCodeForLoadOffset(INS_ldr, EA_4BYTE, tmpReg, srcAddr, offset);
        genCodeForStoreOffset(INS_str, EA_4BYTE, tmpReg, dstAddr, offset);
        offset += REGSIZE_BYTES;
    }

    // Fill the remainder (3 bytes or less) if there's one.
    if ((size & 0x03) != 0)
    {
        if ((size & 2) != 0)
        {
            genCodeForLoadOffset(INS_ldrh, EA_2BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_strh, EA_2BYTE, tmpReg, dstAddr, offset);
            offset += 2;
        }
        if ((size & 1) != 0)
        {
            genCodeForLoadOffset(INS_ldrb, EA_1BYTE, tmpReg, srcAddr, offset);
            genCodeForStoreOffset(INS_strb, EA_1BYTE, tmpReg, dstAddr, offset);
        }
    }
#endif // !_TARGET_ARM64_

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
#ifdef _TARGET_ARM64_
        // issue a INS_BARRIER_ISHLD after a volatile CpBlkUnroll operation
        instGen_MemoryBarrier(INS_BARRIER_ISHLD);
#else
        // issue a full memory barrier after a volatile CpBlk operation
        instGen_MemoryBarrier();
#endif // !_TARGET_ARM64_
    }
}

// Generates code for InitBlk by calling the VM memset helper function.
// Preconditions:
// a) The size argument of the InitBlk is not an integer constant.
// b) The size argument of the InitBlk is >= INITBLK_STOS_LIMIT bytes.
void CodeGen::genCodeForInitBlk(GenTreeBlk* initBlkNode)
{
    unsigned size    = initBlkNode->Size();
    GenTree* dstAddr = initBlkNode->Addr();
    GenTree* initVal = initBlkNode->Data();
    if (initVal->OperIsInitVal())
    {
        initVal = initVal->gtGetOp1();
    }

    assert(!dstAddr->isContained());
    assert(!initVal->isContained());

#ifdef _TARGET_ARM64_
    if (size != 0)
    {
        assert((size > INITBLK_UNROLL_LIMIT) || !initVal->IsCnsIntOrI());
    }
#endif // _TARGET_ARM64_

    genConsumeBlockOp(initBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);

    if (initBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile initBlock Operation
        instGen_MemoryBarrier();
    }

    genEmitHelperCall(CORINFO_HELP_MEMSET, 0, EA_UNKNOWN);
}

// Generate code for a load from some address + offset
//   base: tree node which can be either a local address or arbitrary node
//   offset: distance from the base from which to load
void CodeGen::genCodeForLoadOffset(instruction ins, emitAttr size, regNumber dst, GenTree* base, unsigned offset)
{
    emitter* emit = getEmitter();

    if (base->OperIsLocalAddr())
    {
        if (base->gtOper == GT_LCL_FLD_ADDR)
            offset += base->gtLclFld.gtLclOffs;
        emit->emitIns_R_S(ins, size, dst, base->gtLclVarCommon.gtLclNum, offset);
    }
    else
    {
        emit->emitIns_R_R_I(ins, size, dst, base->gtRegNum, offset);
    }
}

// Generate code for a store to some address + offset
//   base: tree node which can be either a local address or arbitrary node
//   offset: distance from the base from which to load
void CodeGen::genCodeForStoreOffset(instruction ins, emitAttr size, regNumber src, GenTree* base, unsigned offset)
{
    emitter* emit = getEmitter();

    if (base->OperIsLocalAddr())
    {
        if (base->gtOper == GT_LCL_FLD_ADDR)
            offset += base->gtLclFld.gtLclOffs;
        emit->emitIns_S_R(ins, size, src, base->gtLclVarCommon.gtLclNum, offset);
    }
    else
    {
        emit->emitIns_R_R_I(ins, size, src, base->gtRegNum, offset);
    }
}

//------------------------------------------------------------------------
// genRegCopy: Produce code for a GT_COPY node.
//
// Arguments:
//    tree - the GT_COPY node
//
// Notes:
//    This will copy the register(s) produced by this node's source, to
//    the register(s) allocated to this GT_COPY node.
//    It has some special handling for these cases:
//    - when the source and target registers are in different register files
//      (note that this is *not* a conversion).
//    - when the source is a lclVar whose home location is being moved to a new
//      register (rather than just being copied for temporary use).
//
void CodeGen::genRegCopy(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_COPY);
    GenTree* op1 = treeNode->gtOp.gtOp1;

    regNumber sourceReg = genConsumeReg(op1);

    if (op1->IsMultiRegNode())
    {
        noway_assert(!op1->IsCopyOrReload());
        unsigned regCount = op1->GetMultiRegCount();
        for (unsigned i = 0; i < regCount; i++)
        {
            regNumber srcReg  = op1->GetRegByIndex(i);
            regNumber tgtReg  = treeNode->AsCopyOrReload()->GetRegNumByIdx(i);
            var_types regType = op1->GetRegTypeByIndex(i);
            inst_RV_RV(ins_Copy(regType), tgtReg, srcReg, regType);
        }
    }
    else
    {
        var_types targetType = treeNode->TypeGet();
        regNumber targetReg  = treeNode->gtRegNum;
        assert(targetReg != REG_NA);
        assert(targetType != TYP_STRUCT);

        // Check whether this node and the node from which we're copying the value have the same
        // register type.
        // This can happen if (currently iff) we have a SIMD vector type that fits in an integer
        // register, in which case it is passed as an argument, or returned from a call,
        // in an integer register and must be copied if it's in a floating point register.

        bool srcFltReg = (varTypeIsFloating(op1) || varTypeIsSIMD(op1));
        bool tgtFltReg = (varTypeIsFloating(treeNode) || varTypeIsSIMD(treeNode));
        if (srcFltReg != tgtFltReg)
        {
#ifdef _TARGET_ARM64_
            inst_RV_RV(INS_fmov, targetReg, sourceReg, targetType);
#else  // !_TARGET_ARM64_
            if (varTypeIsFloating(treeNode))
            {
                // GT_COPY from 'int' to 'float' currently can't happen. Maybe if ARM SIMD is implemented
                // it will happen, according to the comment above?
                NYI_ARM("genRegCopy from 'int' to 'float'");
            }
            else
            {
                assert(varTypeIsFloating(op1));

                if (op1->TypeGet() == TYP_FLOAT)
                {
                    inst_RV_RV(INS_vmov_f2i, targetReg, genConsumeReg(op1), targetType);
                }
                else
                {
                    regNumber otherReg = (regNumber)treeNode->AsCopyOrReload()->gtOtherRegs[0];
                    assert(otherReg != REG_NA);
                    inst_RV_RV_RV(INS_vmov_d2i, targetReg, otherReg, genConsumeReg(op1), EA_8BYTE);
                }
            }
#endif // !_TARGET_ARM64_
        }
        else
        {
            inst_RV_RV(ins_Copy(targetType), targetReg, sourceReg, targetType);
        }
    }

    if (op1->IsLocal())
    {
        // The lclVar will never be a def.
        // If it is a last use, the lclVar will be killed by genConsumeReg(), as usual, and genProduceReg will
        // appropriately set the gcInfo for the copied value.
        // If not, there are two cases we need to handle:
        // - If this is a TEMPORARY copy (indicated by the GTF_VAR_DEATH flag) the variable
        //   will remain live in its original register.
        //   genProduceReg() will appropriately set the gcInfo for the copied value,
        //   and genConsumeReg will reset it.
        // - Otherwise, we need to update register info for the lclVar.

        GenTreeLclVarCommon* lcl = op1->AsLclVarCommon();
        assert((lcl->gtFlags & GTF_VAR_DEF) == 0);

        if ((lcl->gtFlags & GTF_VAR_DEATH) == 0 && (treeNode->gtFlags & GTF_VAR_DEATH) == 0)
        {
            LclVarDsc* varDsc = &compiler->lvaTable[lcl->gtLclNum];

            // If we didn't just spill it (in genConsumeReg, above), then update the register info
            if (varDsc->lvRegNum != REG_STK)
            {
                // The old location is dying
                genUpdateRegLife(varDsc, /*isBorn*/ false, /*isDying*/ true DEBUGARG(op1));

                gcInfo.gcMarkRegSetNpt(genRegMask(op1->gtRegNum));

                genUpdateVarReg(varDsc, treeNode);

#ifdef USING_VARIABLE_LIVE_RANGE
                // Report the home change for this variable
                varLiveKeeper->siUpdateVariableLiveRange(varDsc, lcl->gtLclNum);
#endif // USING_VARIABLE_LIVE_RANGE

                // The new location is going live
                genUpdateRegLife(varDsc, /*isBorn*/ true, /*isDying*/ false DEBUGARG(treeNode));
            }
        }
    }

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCallInstruction: Produce code for a GT_CALL node
//
void CodeGen::genCallInstruction(GenTreeCall* call)
{
    gtCallTypes callType = (gtCallTypes)call->gtCallType;

    IL_OFFSETX ilOffset = BAD_IL_OFFSET;

    // all virtuals should have been expanded into a control expression
    assert(!call->IsVirtual() || call->gtControlExpr || call->gtCallAddr);

    // Consume all the arg regs
    for (GenTree* list = call->gtCallLateArgs; list; list = list->MoveNext())
    {
        assert(list->OperIsList());

        GenTree* argNode = list->Current();

        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        // GT_RELOAD/GT_COPY use the child node
        argNode = argNode->gtSkipReloadOrCopy();

        if (curArgTabEntry->regNum == REG_STK)
            continue;

        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            GenTreeArgList* argListPtr   = argNode->AsArgList();
            unsigned        iterationNum = 0;
            regNumber       argReg       = curArgTabEntry->regNum;
            for (; argListPtr != nullptr; argListPtr = argListPtr->Rest(), iterationNum++)
            {
                GenTree* putArgRegNode = argListPtr->gtOp.gtOp1;
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                genConsumeReg(putArgRegNode);

                if (putArgRegNode->gtRegNum != argReg)
                {
                    inst_RV_RV(ins_Move_Extend(putArgRegNode->TypeGet(), true), argReg, putArgRegNode->gtRegNum);
                }

                argReg = genRegArgNext(argReg);

#if defined(_TARGET_ARM_)
                // A double register is modelled as an even-numbered single one
                if (putArgRegNode->TypeGet() == TYP_DOUBLE)
                {
                    argReg = genRegArgNext(argReg);
                }
#endif // _TARGET_ARM_
            }
        }
#if FEATURE_ARG_SPLIT
        else if (curArgTabEntry->isSplit)
        {
            assert(curArgTabEntry->numRegs >= 1);
            genConsumeArgSplitStruct(argNode->AsPutArgSplit());
            for (unsigned idx = 0; idx < curArgTabEntry->numRegs; idx++)
            {
                regNumber argReg   = (regNumber)((unsigned)curArgTabEntry->regNum + idx);
                regNumber allocReg = argNode->AsPutArgSplit()->GetRegNumByIdx(idx);
                if (argReg != allocReg)
                {
                    inst_RV_RV(ins_Move_Extend(argNode->TypeGet(), true), argReg, allocReg);
                }
            }
        }
#endif // FEATURE_ARG_SPLIT
        else
        {
            regNumber argReg = curArgTabEntry->regNum;
            genConsumeReg(argNode);
            if (argNode->gtRegNum != argReg)
            {
                inst_RV_RV(ins_Move_Extend(argNode->TypeGet(), true), argReg, argNode->gtRegNum);
            }
        }
    }

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);

#if defined(_TARGET_ARM_)
        const regNumber tmpReg = call->ExtractTempReg();
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, regThis, 0);
#elif defined(_TARGET_ARM64_)
        getEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, regThis, 0);
#endif // _TARGET_*
    }

    // Either gtControlExpr != null or gtCallAddr != null or it is a direct non-virtual call to a user or helper
    // method.
    CORINFO_METHOD_HANDLE methHnd;
    GenTree*              target = call->gtControlExpr;
    if (callType == CT_INDIRECT)
    {
        assert(target == nullptr);
        target  = call->gtCallAddr;
        methHnd = nullptr;
    }
    else
    {
        methHnd = call->gtCallMethHnd;
    }

    CORINFO_SIG_INFO* sigInfo = nullptr;
#ifdef DEBUG
    // Pass the call signature information down into the emitter so the emitter can associate
    // native call sites with the signatures they were generated from.
    if (callType != CT_HELPER)
    {
        sigInfo = call->callSig;
    }
#endif // DEBUG

    // If fast tail call, then we are done.  In this case we setup the args (both reg args
    // and stack args in incoming arg area) and call target.  Epilog sequence would
    // generate "br <reg>".
    if (call->IsFastTailCall())
    {
        // Don't support fast tail calling JIT helpers
        assert(callType != CT_HELPER);

        // Fast tail calls materialize call target either in gtControlExpr or in gtCallAddr.
        assert(target != nullptr);

        genConsumeReg(target);

        // Use IP0 on ARM64 and R12 on ARM32 as the call target register.
        if (target->gtRegNum != REG_FASTTAILCALL_TARGET)
        {
            inst_RV_RV(INS_mov, REG_FASTTAILCALL_TARGET, target->gtRegNum);
        }

        return;
    }

    // For a pinvoke to unmanaged code we emit a label to clear
    // the GC pointer state before the callsite.
    // We can't utilize the typical lazy killing of GC pointers
    // at (or inside) the callsite.
    if (compiler->killGCRefs(call))
    {
        genDefineTempLabel(genCreateTempLabel());
    }

    // Determine return value size(s).
    ReturnTypeDesc* pRetTypeDesc  = call->GetReturnTypeDesc();
    emitAttr        retSize       = EA_PTRSIZE;
    emitAttr        secondRetSize = EA_UNKNOWN;

    if (call->HasMultiRegRetVal())
    {
        retSize       = emitTypeSize(pRetTypeDesc->GetReturnRegType(0));
        secondRetSize = emitTypeSize(pRetTypeDesc->GetReturnRegType(1));
    }
    else
    {
        assert(call->gtType != TYP_STRUCT);

        if (call->gtType == TYP_REF)
        {
            retSize = EA_GCREF;
        }
        else if (call->gtType == TYP_BYREF)
        {
            retSize = EA_BYREF;
        }
    }

    // We need to propagate the IL offset information to the call instruction, so we can emit
    // an IL to native mapping record for the call, to support managed return value debugging.
    // We don't want tail call helper calls that were converted from normal calls to get a record,
    // so we skip this hash table lookup logic in that case.
    if (compiler->opts.compDbgInfo && compiler->genCallSite2ILOffsetMap != nullptr && !call->IsTailCall())
    {
        (void)compiler->genCallSite2ILOffsetMap->Lookup(call, &ilOffset);
    }

    if (target != nullptr)
    {
        // A call target can not be a contained indirection
        assert(!target->isContainedIndir());

        genConsumeReg(target);

        // We have already generated code for gtControlExpr evaluating it into a register.
        // We just need to emit "call reg" in this case.
        //
        assert(genIsValidIntReg(target->gtRegNum));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), ilOffset, target->gtRegNum);
    }
    else
    {
        // Generate a direct call to a non-virtual user defined or helper method
        assert(callType == CT_HELPER || callType == CT_USER_FUNC);

        void* addr = nullptr;
#ifdef FEATURE_READYTORUN_COMPILER
        if (call->gtEntryPoint.addr != NULL)
        {
            assert(call->gtEntryPoint.accessType == IAT_VALUE);
            addr = call->gtEntryPoint.addr;
        }
        else
#endif // FEATURE_READYTORUN_COMPILER
            if (callType == CT_HELPER)
        {
            CorInfoHelpFunc helperNum = compiler->eeGetHelperNum(methHnd);
            noway_assert(helperNum != CORINFO_HELP_UNDEF);

            void* pAddr = nullptr;
            addr        = compiler->compGetHelperFtn(helperNum, (void**)&pAddr);
            assert(pAddr == nullptr);
        }
        else
        {
            // Direct call to a non-virtual user function.
            addr = call->gtDirectCallAddress;
        }

        assert(addr != nullptr);

// Non-virtual direct call to known addresses
#ifdef _TARGET_ARM_
        if (!arm_Valid_Imm_For_BL((ssize_t)addr))
        {
            regNumber tmpReg = call->GetSingleTempReg();
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, tmpReg, (ssize_t)addr);
            genEmitCall(emitter::EC_INDIR_R, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) NULL, retSize, ilOffset, tmpReg);
        }
        else
#endif // _TARGET_ARM_
        {
            genEmitCall(emitter::EC_FUNC_TOKEN, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr,
                        retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), ilOffset);
        }

#if 0 && defined(_TARGET_ARM64_)
        // Use this path if you want to load an absolute call target using 
        //  a sequence of movs followed by an indirect call (blr instruction)
        // If this path is enabled, we need to ensure that REG_IP0 is assigned during Lowering.

        // Load the call target address in x16
        instGen_Set_Reg_To_Imm(EA_8BYTE, REG_IP0, (ssize_t) addr);

        // indirect call to constant address in IP0
        genEmitCall(emitter::EC_INDIR_R,
                    methHnd, 
                    INDEBUG_LDISASM_COMMA(sigInfo)
                    nullptr, //addr
                    retSize,
                    secondRetSize,
                    ilOffset,
                    REG_IP0);
#endif
    }

    // if it was a pinvoke we may have needed to get the address of a label
    if (genPendingCallLabel)
    {
        assert(call->IsUnmanaged());
        genDefineTempLabel(genPendingCallLabel);
        genPendingCallLabel = nullptr;
    }

    // Update GC info:
    // All Callee arg registers are trashed and no longer contain any GC pointers.
    // TODO-Bug?: As a matter of fact shouldn't we be killing all of callee trashed regs here?
    // For now we will assert that other than arg regs gc ref/byref set doesn't contain any other
    // registers from RBM_CALLEE_TRASH
    assert((gcInfo.gcRegGCrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    assert((gcInfo.gcRegByrefSetCur & (RBM_CALLEE_TRASH & ~RBM_ARG_REGS)) == 0);
    gcInfo.gcRegGCrefSetCur &= ~RBM_ARG_REGS;
    gcInfo.gcRegByrefSetCur &= ~RBM_ARG_REGS;

    var_types returnType = call->TypeGet();
    if (returnType != TYP_VOID)
    {
        regNumber returnReg;

        if (call->HasMultiRegRetVal())
        {
            assert(pRetTypeDesc != nullptr);
            unsigned regCount = pRetTypeDesc->GetReturnRegCount();

            // If regs allocated to call node are different from ABI return
            // regs in which the call has returned its result, move the result
            // to regs allocated to call node.
            for (unsigned i = 0; i < regCount; ++i)
            {
                var_types regType      = pRetTypeDesc->GetReturnRegType(i);
                returnReg              = pRetTypeDesc->GetABIReturnReg(i);
                regNumber allocatedReg = call->GetRegNumByIdx(i);
                if (returnReg != allocatedReg)
                {
                    inst_RV_RV(ins_Copy(regType), allocatedReg, returnReg, regType);
                }
            }
        }
        else
        {
#ifdef _TARGET_ARM_
            if (call->IsHelperCall(compiler, CORINFO_HELP_INIT_PINVOKE_FRAME))
            {
                // The CORINFO_HELP_INIT_PINVOKE_FRAME helper uses a custom calling convention that returns with
                // TCB in REG_PINVOKE_TCB. fgMorphCall() sets the correct argument registers.
                returnReg = REG_PINVOKE_TCB;
            }
            else if (compiler->opts.compUseSoftFP)
            {
                returnReg = REG_INTRET;
            }
            else
#endif // _TARGET_ARM_
                if (varTypeUsesFloatArgReg(returnType))
            {
                returnReg = REG_FLOATRET;
            }
            else
            {
                returnReg = REG_INTRET;
            }

            if (call->gtRegNum != returnReg)
            {
#ifdef _TARGET_ARM_
                if (compiler->opts.compUseSoftFP && returnType == TYP_DOUBLE)
                {
                    inst_RV_RV_RV(INS_vmov_i2d, call->gtRegNum, returnReg, genRegArgNext(returnReg), EA_8BYTE);
                }
                else if (compiler->opts.compUseSoftFP && returnType == TYP_FLOAT)
                {
                    inst_RV_RV(INS_vmov_i2f, call->gtRegNum, returnReg, returnType);
                }
                else
#endif
                {
                    inst_RV_RV(ins_Copy(returnType), call->gtRegNum, returnReg, returnType);
                }
            }
        }

        genProduceReg(call);
    }

    // If there is nothing next, that means the result is thrown away, so this value is not live.
    // However, for minopts or debuggable code, we keep it live to support managed return value debugging.
    if ((call->gtNext == nullptr) && !compiler->opts.MinOpts() && !compiler->opts.compDbgCode)
    {
        gcInfo.gcMarkRegSetNpt(RBM_INTRET);
    }
}

// Produce code for a GT_JMP node.
// The arguments of the caller needs to be transferred to the callee before exiting caller.
// The actual jump to callee is generated as part of caller epilog sequence.
// Therefore the codegen of GT_JMP is to ensure that the callee arguments are correctly setup.
void CodeGen::genJmpMethod(GenTree* jmp)
{
    assert(jmp->OperGet() == GT_JMP);
    assert(compiler->compJmpOpUsed);

    // If no arguments, nothing to do
    if (compiler->info.compArgsCount == 0)
    {
        return;
    }

    // Make sure register arguments are in their initial registers
    // and stack arguments are put back as well.
    unsigned   varNum;
    LclVarDsc* varDsc;

    // First move any en-registered stack arguments back to the stack.
    // At the same time any reg arg not in correct reg is moved back to its stack location.
    //
    // We are not strictly required to spill reg args that are not in the desired reg for a jmp call
    // But that would require us to deal with circularity while moving values around.  Spilling
    // to stack makes the implementation simple, which is not a bad trade off given Jmp calls
    // are not frequent.
    for (varNum = 0; (varNum < compiler->info.compArgsCount); varNum++)
    {
        varDsc = compiler->lvaTable + varNum;

        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaTable + fieldVarNum;
        }
        noway_assert(varDsc->lvIsParam);

        if (varDsc->lvIsRegArg && (varDsc->lvRegNum != REG_STK))
        {
            // Skip reg args which are already in its right register for jmp call.
            // If not, we will spill such args to their stack locations.
            //
            // If we need to generate a tail call profiler hook, then spill all
            // arg regs to free them up for the callback.
            if (!compiler->compIsProfilerHookNeeded() && (varDsc->lvRegNum == varDsc->lvArgReg))
                continue;
        }
        else if (varDsc->lvRegNum == REG_STK)
        {
            // Skip args which are currently living in stack.
            continue;
        }

        // If we came here it means either a reg argument not in the right register or
        // a stack argument currently living in a register.  In either case the following
        // assert should hold.
        assert(varDsc->lvRegNum != REG_STK);
        assert(varDsc->TypeGet() != TYP_STRUCT);
        var_types storeType = genActualType(varDsc->TypeGet());
        emitAttr  storeSize = emitActualTypeSize(storeType);

#ifdef _TARGET_ARM_
        if (varDsc->TypeGet() == TYP_LONG)
        {
            // long - at least the low half must be enregistered
            getEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, varDsc->lvRegNum, varNum, 0);

            // Is the upper half also enregistered?
            if (varDsc->lvOtherReg != REG_STK)
            {
                getEmitter()->emitIns_S_R(ins_Store(TYP_INT), EA_4BYTE, varDsc->lvOtherReg, varNum, sizeof(int));
            }
        }
        else
#endif // _TARGET_ARM_
        {
            getEmitter()->emitIns_S_R(ins_Store(storeType), storeSize, varDsc->lvRegNum, varNum, 0);
        }
        // Update lvRegNum life and GC info to indicate lvRegNum is dead and varDsc stack slot is going live.
        // Note that we cannot modify varDsc->lvRegNum here because another basic block may not be expecting it.
        // Therefore manually update life of varDsc->lvRegNum.
        regMaskTP tempMask = genRegMask(varDsc->lvRegNum);
        regSet.RemoveMaskVars(tempMask);
        gcInfo.gcMarkRegSetNpt(tempMask);
        if (compiler->lvaIsGCTracked(varDsc))
        {
            VarSetOps::AddElemD(compiler, gcInfo.gcVarPtrSetCur, varNum);
        }
    }

#ifdef PROFILING_SUPPORTED
    // At this point all arg regs are free.
    // Emit tail call profiler callback.
    genProfilingLeaveCallback(CORINFO_HELP_PROF_FCN_TAILCALL);
#endif

    // Next move any un-enregistered register arguments back to their register.
    regMaskTP fixedIntArgMask = RBM_NONE;    // tracks the int arg regs occupying fixed args in case of a vararg method.
    unsigned  firstArgVarNum  = BAD_VAR_NUM; // varNum of the first argument in case of a vararg method.
    for (varNum = 0; (varNum < compiler->info.compArgsCount); varNum++)
    {
        varDsc = compiler->lvaTable + varNum;
        if (varDsc->lvPromoted)
        {
            noway_assert(varDsc->lvFieldCnt == 1); // We only handle one field here

            unsigned fieldVarNum = varDsc->lvFieldLclStart;
            varDsc               = compiler->lvaTable + fieldVarNum;
        }
        noway_assert(varDsc->lvIsParam);

        // Skip if arg not passed in a register.
        if (!varDsc->lvIsRegArg)
            continue;

        // Register argument
        noway_assert(isRegParamType(genActualType(varDsc->TypeGet())));

        // Is register argument already in the right register?
        // If not load it from its stack location.
        regNumber argReg     = varDsc->lvArgReg; // incoming arg register
        regNumber argRegNext = REG_NA;

#ifdef _TARGET_ARM64_
        if (varDsc->lvRegNum != argReg)
        {
            var_types loadType = TYP_UNDEF;

            if (varDsc->lvIsHfaRegArg())
            {
                // Note that for HFA, the argument is currently marked address exposed so lvRegNum will always be
                // REG_STK. We home the incoming HFA argument registers in the prolog. Then we'll load them back
                // here, whether they are already in the correct registers or not. This is such a corner case that
                // it is not worth optimizing it.

                assert(!compiler->info.compIsVarArgs);

                loadType           = varDsc->GetHfaType();
                regNumber fieldReg = argReg;
                emitAttr  loadSize = emitActualTypeSize(loadType);
                unsigned  cSlots   = varDsc->lvHfaSlots();

                for (unsigned ofs = 0, cSlot = 0; cSlot < cSlots; cSlot++, ofs += (unsigned)loadSize)
                {
                    getEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, fieldReg, varNum, ofs);
                    assert(genIsValidFloatReg(fieldReg)); // No GC register tracking for floating point registers.
                    fieldReg = regNextOfType(fieldReg, loadType);
                }
            }
            else
            {
                if (varTypeIsStruct(varDsc))
                {
                    // Must be <= 16 bytes or else it wouldn't be passed in registers, except for HFA,
                    // which can be bigger (and is handled above).
                    noway_assert(EA_SIZE_IN_BYTES(varDsc->lvSize()) <= 16);
                    loadType = compiler->getJitGCType(varDsc->lvGcLayout[0]);
                }
                else
                {
                    loadType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));
                }
                emitAttr loadSize = emitActualTypeSize(loadType);
                getEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argReg, varNum, 0);

                // Update argReg life and GC Info to indicate varDsc stack slot is dead and argReg is going live.
                // Note that we cannot modify varDsc->lvRegNum here because another basic block may not be expecting it.
                // Therefore manually update life of argReg.  Note that GT_JMP marks the end of the basic block
                // and after which reg life and gc info will be recomputed for the new block in genCodeForBBList().
                regSet.AddMaskVars(genRegMask(argReg));
                gcInfo.gcMarkRegPtrVal(argReg, loadType);

                if (compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
                {
                    // Restore the second register.
                    argRegNext = genRegArgNext(argReg);

                    loadType = compiler->getJitGCType(varDsc->lvGcLayout[1]);
                    loadSize = emitActualTypeSize(loadType);
                    getEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argRegNext, varNum, TARGET_POINTER_SIZE);

                    regSet.AddMaskVars(genRegMask(argRegNext));
                    gcInfo.gcMarkRegPtrVal(argRegNext, loadType);
                }

                if (compiler->lvaIsGCTracked(varDsc))
                {
                    VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
                }
            }
        }

        if (compiler->info.compIsVarArgs)
        {
            // In case of a jmp call to a vararg method ensure only integer registers are passed.
            assert((genRegMask(argReg) & (RBM_ARG_REGS | RBM_ARG_RET_BUFF)) != RBM_NONE);
            assert(!varDsc->lvIsHfaRegArg());

            fixedIntArgMask |= genRegMask(argReg);

            if (compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
            {
                assert(argRegNext != REG_NA);
                fixedIntArgMask |= genRegMask(argRegNext);
            }

            if (argReg == REG_ARG_0)
            {
                assert(firstArgVarNum == BAD_VAR_NUM);
                firstArgVarNum = varNum;
            }
        }

#else  // !_TARGET_ARM64_

        bool      twoParts = false;
        var_types loadType = TYP_UNDEF;
        if (varDsc->TypeGet() == TYP_LONG)
        {
            twoParts = true;
        }
        else if (varDsc->TypeGet() == TYP_DOUBLE)
        {
            if (compiler->info.compIsVarArgs || compiler->opts.compUseSoftFP)
            {
                twoParts = true;
            }
        }

        if (twoParts)
        {
            argRegNext = genRegArgNext(argReg);

            if (varDsc->lvRegNum != argReg)
            {
                getEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, argReg, varNum, 0);
                getEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, argRegNext, varNum, REGSIZE_BYTES);
            }

            if (compiler->info.compIsVarArgs)
            {
                fixedIntArgMask |= genRegMask(argReg);
                fixedIntArgMask |= genRegMask(argRegNext);
            }
        }
        else if (varDsc->lvIsHfaRegArg())
        {
            loadType           = varDsc->GetHfaType();
            regNumber fieldReg = argReg;
            emitAttr  loadSize = emitActualTypeSize(loadType);
            unsigned  maxSize  = min(varDsc->lvSize(), (LAST_FP_ARGREG + 1 - argReg) * REGSIZE_BYTES);

            for (unsigned ofs = 0; ofs < maxSize; ofs += (unsigned)loadSize)
            {
                if (varDsc->lvRegNum != argReg)
                {
                    getEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, fieldReg, varNum, ofs);
                }
                assert(genIsValidFloatReg(fieldReg)); // we don't use register tracking for FP
                fieldReg = regNextOfType(fieldReg, loadType);
            }
        }
        else if (varTypeIsStruct(varDsc))
        {
            regNumber slotReg = argReg;
            unsigned  maxSize = min(varDsc->lvSize(), (REG_ARG_LAST + 1 - argReg) * REGSIZE_BYTES);

            for (unsigned ofs = 0; ofs < maxSize; ofs += REGSIZE_BYTES)
            {
                unsigned idx = ofs / REGSIZE_BYTES;
                loadType     = compiler->getJitGCType(varDsc->lvGcLayout[idx]);

                if (varDsc->lvRegNum != argReg)
                {
                    emitAttr loadSize = emitActualTypeSize(loadType);

                    getEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, slotReg, varNum, ofs);
                }

                regSet.AddMaskVars(genRegMask(slotReg));
                gcInfo.gcMarkRegPtrVal(slotReg, loadType);
                if (genIsValidIntReg(slotReg) && compiler->info.compIsVarArgs)
                {
                    fixedIntArgMask |= genRegMask(slotReg);
                }

                slotReg = genRegArgNext(slotReg);
            }
        }
        else
        {
            loadType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));

            if (varDsc->lvRegNum != argReg)
            {
                getEmitter()->emitIns_R_S(ins_Load(loadType), emitTypeSize(loadType), argReg, varNum, 0);
            }

            regSet.AddMaskVars(genRegMask(argReg));
            gcInfo.gcMarkRegPtrVal(argReg, loadType);

            if (genIsValidIntReg(argReg) && compiler->info.compIsVarArgs)
            {
                fixedIntArgMask |= genRegMask(argReg);
            }
        }

        if (compiler->lvaIsGCTracked(varDsc))
        {
            VarSetOps::RemoveElemD(compiler, gcInfo.gcVarPtrSetCur, varDsc->lvVarIndex);
        }
#endif // !_TARGET_ARM64_
    }

    // Jmp call to a vararg method - if the method has fewer than fixed arguments that can be max size of reg,
    // load the remaining integer arg registers from the corresponding
    // shadow stack slots.  This is for the reason that we don't know the number and type
    // of non-fixed params passed by the caller, therefore we have to assume the worst case
    // of caller passing all integer arg regs that can be max size of reg.
    //
    // The caller could have passed gc-ref/byref type var args.  Since these are var args
    // the callee no way of knowing their gc-ness.  Therefore, mark the region that loads
    // remaining arg registers from shadow stack slots as non-gc interruptible.
    if (fixedIntArgMask != RBM_NONE)
    {
        assert(compiler->info.compIsVarArgs);
        assert(firstArgVarNum != BAD_VAR_NUM);

        regMaskTP remainingIntArgMask = RBM_ARG_REGS & ~fixedIntArgMask;
        if (remainingIntArgMask != RBM_NONE)
        {
            getEmitter()->emitDisableGC();
            for (int argNum = 0, argOffset = 0; argNum < MAX_REG_ARG; ++argNum)
            {
                regNumber argReg     = intArgRegs[argNum];
                regMaskTP argRegMask = genRegMask(argReg);

                if ((remainingIntArgMask & argRegMask) != 0)
                {
                    remainingIntArgMask &= ~argRegMask;
                    getEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, argReg, firstArgVarNum, argOffset);
                }

                argOffset += REGSIZE_BYTES;
            }
            getEmitter()->emitEnableGC();
        }
    }
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
    switch (desc.CheckKind())
    {
        case GenIntCastDesc::CHECK_POSITIVE:
            getEmitter()->emitIns_R_I(INS_cmp, EA_ATTR(desc.CheckSrcSize()), reg, 0);
            genJumpToThrowHlpBlk(EJ_lt, SCK_OVERFLOW);
            break;

#ifdef _TARGET_64BIT_
        case GenIntCastDesc::CHECK_UINT_RANGE:
            // We need to check if the value is not greater than 0xFFFFFFFF but this value
            // cannot be encoded in the immediate operand of CMP. Use TST instead to check
            // if the upper 32 bits are zero.
            getEmitter()->emitIns_R_I(INS_tst, EA_8BYTE, reg, 0xFFFFFFFF00000000LL);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            break;

        case GenIntCastDesc::CHECK_POSITIVE_INT_RANGE:
            // We need to check if the value is not greater than 0x7FFFFFFF but this value
            // cannot be encoded in the immediate operand of CMP. Use TST instead to check
            // if the upper 33 bits are zero.
            getEmitter()->emitIns_R_I(INS_tst, EA_8BYTE, reg, 0xFFFFFFFF80000000LL);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            break;

        case GenIntCastDesc::CHECK_INT_RANGE:
        {
            const regNumber tempReg = cast->GetSingleTempReg();
            assert(tempReg != reg);
            instGen_Set_Reg_To_Imm(EA_8BYTE, tempReg, INT32_MAX);
            getEmitter()->emitIns_R_R(INS_cmp, EA_8BYTE, reg, tempReg);
            genJumpToThrowHlpBlk(EJ_gt, SCK_OVERFLOW);
            instGen_Set_Reg_To_Imm(EA_8BYTE, tempReg, INT32_MIN);
            getEmitter()->emitIns_R_R(INS_cmp, EA_8BYTE, reg, tempReg);
            genJumpToThrowHlpBlk(EJ_lt, SCK_OVERFLOW);
        }
        break;
#endif

        default:
        {
            assert(desc.CheckKind() == GenIntCastDesc::CHECK_SMALL_INT_RANGE);
            const int castMaxValue = desc.CheckSmallIntMax();
            const int castMinValue = desc.CheckSmallIntMin();

            // Values greater than 255 cannot be encoded in the immediate operand of CMP.
            // Replace (x > max) with (x >= max + 1) where max + 1 (a power of 2) can be
            // encoded. We could do this for all max values but on ARM32 "cmp r0, 255"
            // is better than "cmp r0, 256" because it has a shorter encoding.
            if (castMaxValue > 255)
            {
                assert((castMaxValue == 32767) || (castMaxValue == 65535));
                getEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMaxValue + 1);
                genJumpToThrowHlpBlk((castMinValue == 0) ? EJ_hs : EJ_ge, SCK_OVERFLOW);
            }
            else
            {
                getEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMaxValue);
                genJumpToThrowHlpBlk((castMinValue == 0) ? EJ_hi : EJ_gt, SCK_OVERFLOW);
            }

            if (castMinValue != 0)
            {
                getEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMinValue);
                genJumpToThrowHlpBlk(EJ_lt, SCK_OVERFLOW);
            }
        }
        break;
    }
}

//------------------------------------------------------------------------
// genIntToIntCast: Generate code for an integer cast, with or without overflow check.
//
// Arguments:
//    cast - The GT_CAST node
//
// Assumptions:
//    The cast node is not a contained node and must have an assigned register.
//    Neither the source nor target type can be a floating point type.
//
// TODO-ARM64-CQ: Allow castOp to be a contained node without an assigned register.
//
void CodeGen::genIntToIntCast(GenTreeCast* cast)
{
    genConsumeRegs(cast->gtGetOp1());

    const regNumber srcReg = cast->gtGetOp1()->gtRegNum;
    const regNumber dstReg = cast->gtRegNum;

    assert(genIsValidIntReg(srcReg));
    assert(genIsValidIntReg(dstReg));

    GenIntCastDesc desc(cast);

    if (desc.CheckKind() != GenIntCastDesc::CHECK_NONE)
    {
        genIntCastOverflowCheck(cast, desc, srcReg);
    }

    if ((desc.ExtendKind() != GenIntCastDesc::COPY) || (srcReg != dstReg))
    {
        instruction ins;
        unsigned    insSize;

        switch (desc.ExtendKind())
        {
            case GenIntCastDesc::ZERO_EXTEND_SMALL_INT:
                ins     = (desc.ExtendSrcSize() == 1) ? INS_uxtb : INS_uxth;
                insSize = 4;
                break;
            case GenIntCastDesc::SIGN_EXTEND_SMALL_INT:
                ins     = (desc.ExtendSrcSize() == 1) ? INS_sxtb : INS_sxth;
                insSize = 4;
                break;
#ifdef _TARGET_64BIT_
            case GenIntCastDesc::ZERO_EXTEND_INT:
                ins     = INS_mov;
                insSize = 4;
                break;
            case GenIntCastDesc::SIGN_EXTEND_INT:
                ins     = INS_sxtw;
                insSize = 8;
                break;
#endif
            default:
                assert(desc.ExtendKind() == GenIntCastDesc::COPY);
                ins     = INS_mov;
                insSize = desc.ExtendSrcSize();
                break;
        }

        getEmitter()->emitIns_R_R(ins, EA_ATTR(insSize), dstReg, srcReg);
    }

    genProduceReg(cast);
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
void CodeGen::genFloatToFloatCast(GenTree* treeNode)
{
    // float <--> double conversions are always non-overflow ones
    assert(treeNode->OperGet() == GT_CAST);
    assert(!treeNode->gtOverflow());

    regNumber targetReg = treeNode->gtRegNum;
    assert(genIsValidFloatReg(targetReg));

    GenTree* op1 = treeNode->gtOp.gtOp1;
    assert(!op1->isContained());               // Cannot be contained
    assert(genIsValidFloatReg(op1->gtRegNum)); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());

    // treeNode must be a reg
    assert(!treeNode->isContained());

#if defined(_TARGET_ARM_)

    if (srcType != dstType)
    {
        instruction insVcvt = (srcType == TYP_FLOAT) ? INS_vcvt_f2d  // convert Float to Double
                                                     : INS_vcvt_d2f; // convert Double to Float

        getEmitter()->emitIns_R_R(insVcvt, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }
    else if (treeNode->gtRegNum != op1->gtRegNum)
    {
        getEmitter()->emitIns_R_R(INS_vmov, emitTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }

#elif defined(_TARGET_ARM64_)

    if (srcType != dstType)
    {
        insOpts cvtOption = (srcType == TYP_FLOAT) ? INS_OPTS_S_TO_D  // convert Single to Double
                                                   : INS_OPTS_D_TO_S; // convert Double to Single

        getEmitter()->emitIns_R_R(INS_fcvt, emitActualTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum, cvtOption);
    }
    else if (treeNode->gtRegNum != op1->gtRegNum)
    {
        // If double to double cast or float to float cast. Emit a move instruction.
        getEmitter()->emitIns_R_R(INS_mov, emitActualTypeSize(treeNode), treeNode->gtRegNum, op1->gtRegNum);
    }

#endif // _TARGET_*

    genProduceReg(treeNode);
}

//------------------------------------------------------------------------
// genCreateAndStoreGCInfo: Create and record GC Info for the function.
//
void CodeGen::genCreateAndStoreGCInfo(unsigned codeSize,
                                      unsigned prologSize,
                                      unsigned epilogSize DEBUGARG(void* codePtr))
{
    IAllocator*    allowZeroAlloc = new (compiler, CMK_GC) CompIAllocator(compiler->getAllocatorGC());
    GcInfoEncoder* gcInfoEncoder  = new (compiler, CMK_GC)
        GcInfoEncoder(compiler->info.compCompHnd, compiler->info.compMethodInfo, allowZeroAlloc, NOMEM);
    assert(gcInfoEncoder != nullptr);

    // Follow the code pattern of the x86 gc info encoder (genCreateAndStoreGCInfoJIT32).
    gcInfo.gcInfoBlockHdrSave(gcInfoEncoder, codeSize, prologSize);

    // We keep the call count for the second call to gcMakeRegPtrTable() below.
    unsigned callCnt = 0;

    // First we figure out the encoder ID's for the stack slots and registers.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_ASSIGN_SLOTS, &callCnt);

    // Now we've requested all the slots we'll need; "finalize" these (make more compact data structures for them).
    gcInfoEncoder->FinalizeSlotIds();

    // Now we can actually use those slot ID's to declare live ranges.
    gcInfo.gcMakeRegPtrTable(gcInfoEncoder, codeSize, prologSize, GCInfo::MAKE_REG_PTR_MODE_DO_WORK, &callCnt);

#ifdef _TARGET_ARM64_

    if (compiler->opts.compDbgEnC)
    {
        // what we have to preserve is called the "frame header" (see comments in VM\eetwain.cpp)
        // which is:
        //  -return address
        //  -saved off RBP
        //  -saved 'this' pointer and bool for synchronized methods

        // 4 slots for RBP + return address + RSI + RDI
        int preservedAreaSize = 4 * REGSIZE_BYTES;

        if (compiler->info.compFlags & CORINFO_FLG_SYNCH)
        {
            if (!(compiler->info.compFlags & CORINFO_FLG_STATIC))
                preservedAreaSize += REGSIZE_BYTES;

            preservedAreaSize += 1; // bool for synchronized methods
        }

        // Used to signal both that the method is compiled for EnC, and also the size of the block at the top of the
        // frame
        gcInfoEncoder->SetSizeOfEditAndContinuePreservedArea(preservedAreaSize);
    }

#endif // _TARGET_ARM64_

    if (compiler->opts.IsReversePInvoke())
    {
        unsigned reversePInvokeFrameVarNumber = compiler->lvaReversePInvokeFrameVar;
        assert(reversePInvokeFrameVarNumber != BAD_VAR_NUM && reversePInvokeFrameVarNumber < compiler->lvaRefCount);
        LclVarDsc& reversePInvokeFrameVar = compiler->lvaTable[reversePInvokeFrameVarNumber];
        gcInfoEncoder->SetReversePInvokeFrameSlot(reversePInvokeFrameVar.lvStkOffs);
    }

    gcInfoEncoder->Build();

    // GC Encoder automatically puts the GC info in the right spot using ICorJitInfo::allocGCInfo(size_t)
    // let's save the values anyway for debugging purposes
    compiler->compInfoBlkAddr = gcInfoEncoder->Emit();
    compiler->compInfoBlkSize = 0; // not exposed by the GCEncoder interface
}

// clang-format off
const CodeGen::GenConditionDesc CodeGen::GenConditionDesc::map[32]
{
    { },       // NONE
    { },       // 1
    { EJ_lt }, // SLT
    { EJ_le }, // SLE
    { EJ_ge }, // SGE
    { EJ_gt }, // SGT
    { EJ_mi }, // S
    { EJ_pl }, // NS

    { EJ_eq }, // EQ
    { EJ_ne }, // NE
    { EJ_lo }, // ULT
    { EJ_ls }, // ULE
    { EJ_hs }, // UGE
    { EJ_hi }, // UGT
    { EJ_hs }, // C
    { EJ_lo }, // NC

    { EJ_eq },                // FEQ
    { EJ_gt, GT_AND, EJ_lo }, // FNE
    { EJ_lo },                // FLT
    { EJ_ls },                // FLE
    { EJ_ge },                // FGE
    { EJ_gt },                // FGT
    { EJ_vs },                // O
    { EJ_vc },                // NO

    { EJ_eq, GT_OR, EJ_vs },  // FEQU
    { EJ_ne },                // FNEU
    { EJ_lt },                // FLTU
    { EJ_le },                // FLEU
    { EJ_hs },                // FGEU
    { EJ_hi },                // FGTU
    { },                      // P
    { },                      // NP
};
// clang-format on

//------------------------------------------------------------------------
// inst_SETCC: Generate code to set a register to 0 or 1 based on a condition.
//
// Arguments:
//   condition - The condition
//   type      - The type of the value to be produced
//   dstReg    - The destination register to be set to 1 or 0
//
void CodeGen::inst_SETCC(GenCondition condition, var_types type, regNumber dstReg)
{
    assert(varTypeIsIntegral(type));
    assert(genIsValidIntReg(dstReg));

#ifdef _TARGET_ARM64_
    const GenConditionDesc& desc = GenConditionDesc::Get(condition);

    inst_SET(desc.jumpKind1, dstReg);

    if (desc.oper != GT_NONE)
    {
        BasicBlock* labelNext = genCreateTempLabel();
        inst_JMP((desc.oper == GT_OR) ? desc.jumpKind1 : emitter::emitReverseJumpKind(desc.jumpKind1), labelNext);
        inst_SET(desc.jumpKind2, dstReg);
        genDefineTempLabel(labelNext);
    }
#else
    // Emit code like that:
    //   ...
    //   bgt True
    //   movs rD, #0
    //   b Next
    // True:
    //   movs rD, #1
    // Next:
    //   ...

    BasicBlock* labelTrue = genCreateTempLabel();
    inst_JCC(condition, labelTrue);

    getEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(type), dstReg, 0);

    BasicBlock* labelNext = genCreateTempLabel();
    getEmitter()->emitIns_J(INS_b, labelNext);

    genDefineTempLabel(labelTrue);
    getEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(type), dstReg, 1);
    genDefineTempLabel(labelNext);
#endif
}

//------------------------------------------------------------------------
// genCodeForStoreBlk: Produce code for a GT_STORE_OBJ/GT_STORE_DYN_BLK/GT_STORE_BLK node.
//
// Arguments:
//    tree - the node
//
void CodeGen::genCodeForStoreBlk(GenTreeBlk* blkOp)
{
    assert(blkOp->OperIs(GT_STORE_OBJ, GT_STORE_DYN_BLK, GT_STORE_BLK));

    if (blkOp->OperIs(GT_STORE_OBJ) && blkOp->OperIsCopyBlkOp())
    {
        assert(blkOp->AsObj()->gtGcPtrCount != 0);
        genCodeForCpObj(blkOp->AsObj());
        return;
    }

    if (blkOp->gtBlkOpGcUnsafe)
    {
        getEmitter()->emitDisableGC();
    }
    bool isCopyBlk = blkOp->OperIsCopyBlkOp();

    switch (blkOp->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindHelper:
            if (isCopyBlk)
            {
                genCodeForCpBlk(blkOp);
            }
            else
            {
                genCodeForInitBlk(blkOp);
            }
            break;

        case GenTreeBlk::BlkOpKindUnroll:
            if (isCopyBlk)
            {
                genCodeForCpBlkUnroll(blkOp);
            }
            else
            {
                genCodeForInitBlkUnroll(blkOp);
            }
            break;

        default:
            unreached();
    }

    if (blkOp->gtBlkOpGcUnsafe)
    {
        getEmitter()->emitEnableGC();
    }
}

//------------------------------------------------------------------------
// genScaledAdd: A helper for genLeaInstruction.
//
void CodeGen::genScaledAdd(emitAttr attr, regNumber targetReg, regNumber baseReg, regNumber indexReg, int scale)
{
    emitter* emit = getEmitter();
    if (scale == 0)
    {
        // target = base + index
        getEmitter()->emitIns_R_R_R(INS_add, attr, targetReg, baseReg, indexReg);
    }
    else
    {
// target = base + index<<scale
#if defined(_TARGET_ARM_)
        emit->emitIns_R_R_R_I(INS_add, attr, targetReg, baseReg, indexReg, scale, INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
#elif defined(_TARGET_ARM64_)
        emit->emitIns_R_R_R_I(INS_add, attr, targetReg, baseReg, indexReg, scale, INS_OPTS_LSL);
#endif
    }
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
    emitter* emit   = getEmitter();
    emitAttr size   = emitTypeSize(lea);
    int      offset = lea->Offset();

    // In ARM we can only load addresses of the form:
    //
    // [Base + index*scale]
    // [Base + Offset]
    // [Literal] (PC-Relative)
    //
    // So for the case of a LEA node of the form [Base + Index*Scale + Offset] we will generate:
    // destReg = baseReg + indexReg * scale;
    // destReg = destReg + offset;
    //
    // TODO-ARM64-CQ: The purpose of the GT_LEA node is to directly reflect a single target architecture
    //             addressing mode instruction.  Currently we're 'cheating' by producing one or more
    //             instructions to generate the addressing mode so we need to modify lowering to
    //             produce LEAs that are a 1:1 relationship to the ARM64 architecture.
    if (lea->Base() && lea->Index())
    {
        GenTree* memBase = lea->Base();
        GenTree* index   = lea->Index();

        DWORD scale;

        assert(isPow2(lea->gtScale));
        BitScanForward(&scale, lea->gtScale);

        assert(scale <= 4);

        if (offset != 0)
        {
            regNumber tmpReg = lea->GetSingleTempReg();

            // When generating fully interruptible code we have to use the "large offset" sequence
            // when calculating a EA_BYREF as we can't report a byref that points outside of the object
            //
            bool useLargeOffsetSeq = compiler->genInterruptible && (size == EA_BYREF);

            if (!useLargeOffsetSeq && emitter::emitIns_valid_imm_for_add(offset))
            {
                // Generate code to set tmpReg = base + index*scale
                genScaledAdd(size, tmpReg, memBase->gtRegNum, index->gtRegNum, scale);

                // Then compute target reg from [tmpReg + offset]
                emit->emitIns_R_R_I(INS_add, size, lea->gtRegNum, tmpReg, offset);
            }
            else // large offset sequence
            {
                noway_assert(tmpReg != index->gtRegNum);
                noway_assert(tmpReg != memBase->gtRegNum);

                // First load/store tmpReg with the offset constant
                //      rTmp = imm
                instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

                // Then add the scaled index register
                //      rTmp = rTmp + index*scale
                genScaledAdd(EA_PTRSIZE, tmpReg, tmpReg, index->gtRegNum, scale);

                // Then compute target reg from [base + tmpReg ]
                //      rDst = base + rTmp
                emit->emitIns_R_R_R(INS_add, size, lea->gtRegNum, memBase->gtRegNum, tmpReg);
            }
        }
        else
        {
            // Then compute target reg from [base + index*scale]
            genScaledAdd(size, lea->gtRegNum, memBase->gtRegNum, index->gtRegNum, scale);
        }
    }
    else if (lea->Base())
    {
        GenTree* memBase = lea->Base();

        if (emitter::emitIns_valid_imm_for_add(offset))
        {
            if (offset != 0)
            {
                // Then compute target reg from [memBase + offset]
                emit->emitIns_R_R_I(INS_add, size, lea->gtRegNum, memBase->gtRegNum, offset);
            }
            else // offset is zero
            {
                if (lea->gtRegNum != memBase->gtRegNum)
                {
                    emit->emitIns_R_R(INS_mov, size, lea->gtRegNum, memBase->gtRegNum);
                }
            }
        }
        else
        {
            // We require a tmpReg to hold the offset
            regNumber tmpReg = lea->GetSingleTempReg();

            // First load tmpReg with the large offset constant
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

            // Then compute target reg from [memBase + tmpReg]
            emit->emitIns_R_R_R(INS_add, size, lea->gtRegNum, memBase->gtRegNum, tmpReg);
        }
    }
    else if (lea->Index())
    {
        // If we encounter a GT_LEA node without a base it means it came out
        // when attempting to optimize an arbitrary arithmetic expression during lower.
        // This is currently disabled in ARM64 since we need to adjust lower to account
        // for the simpler instructions ARM64 supports.
        // TODO-ARM64-CQ:  Fix this and let LEA optimize arithmetic trees too.
        assert(!"We shouldn't see a baseless address computation during CodeGen for ARM64");
    }

    genProduceReg(lea);
}

//------------------------------------------------------------------------
// isStructReturn: Returns whether the 'treeNode' is returning a struct.
//
// Arguments:
//    treeNode - The tree node to evaluate whether is a struct return.
//
// Return Value:
//    Returns true if the 'treeNode" is a GT_RETURN node of type struct.
//    Otherwise returns false.
//
bool CodeGen::isStructReturn(GenTree* treeNode)
{
    // This method could be called for 'treeNode' of GT_RET_FILT or GT_RETURN.
    // For the GT_RET_FILT, the return is always
    // a bool or a void, for the end of a finally block.
    noway_assert(treeNode->OperGet() == GT_RETURN || treeNode->OperGet() == GT_RETFILT);
    var_types returnType = treeNode->TypeGet();

#ifdef _TARGET_ARM64_
    return varTypeIsStruct(returnType) && (compiler->info.compRetNativeType == TYP_STRUCT);
#else
    return varTypeIsStruct(returnType);
#endif
}

//------------------------------------------------------------------------
// genStructReturn: Generates code for returning a struct.
//
// Arguments:
//    treeNode - The GT_RETURN tree node.
//
// Return Value:
//    None
//
// Assumption:
//    op1 of GT_RETURN node is either GT_LCL_VAR or multi-reg GT_CALL
void CodeGen::genStructReturn(GenTree* treeNode)
{
    assert(treeNode->OperGet() == GT_RETURN);
    assert(isStructReturn(treeNode));
    GenTree* op1 = treeNode->gtGetOp1();

    if (op1->OperGet() == GT_LCL_VAR)
    {
        GenTreeLclVarCommon* lclVar  = op1->AsLclVarCommon();
        LclVarDsc*           varDsc  = &(compiler->lvaTable[lclVar->gtLclNum]);
        var_types            lclType = genActualType(varDsc->TypeGet());

        assert(varTypeIsStruct(lclType));
        assert(varDsc->lvIsMultiRegRet);

        ReturnTypeDesc retTypeDesc;
        unsigned       regCount;

        retTypeDesc.InitializeStructReturnType(compiler, varDsc->lvVerTypeInfo.GetClassHandle());
        regCount = retTypeDesc.GetReturnRegCount();

        assert(regCount >= 2);

        assert(varTypeIsSIMD(lclType) || op1->isContained());

        if (op1->isContained())
        {
            // Copy var on stack into ABI return registers
            // TODO: It could be optimized by reducing two float loading to one double
            int offset = 0;
            for (unsigned i = 0; i < regCount; ++i)
            {
                var_types type = retTypeDesc.GetReturnRegType(i);
                regNumber reg  = retTypeDesc.GetABIReturnReg(i);
                getEmitter()->emitIns_R_S(ins_Load(type), emitTypeSize(type), reg, lclVar->gtLclNum, offset);
                offset += genTypeSize(type);
            }
        }
        else
        {
            // Handle SIMD genStructReturn case
            NYI_ARM("SIMD genStructReturn");

#ifdef _TARGET_ARM64_
            genConsumeRegs(op1);
            regNumber src = op1->gtRegNum;

            // Treat src register as a homogenous vector with element size equal to the reg size
            // Insert pieces in order
            for (unsigned i = 0; i < regCount; ++i)
            {
                var_types type = retTypeDesc.GetReturnRegType(i);
                regNumber reg  = retTypeDesc.GetABIReturnReg(i);
                if (varTypeIsFloating(type))
                {
                    // If the register piece is to be passed in a floating point register
                    // Use a vector mov element instruction
                    // reg is not a vector, so it is in the first element reg[0]
                    // mov reg[0], src[i]
                    // This effectively moves from `src[i]` to `reg[0]`, upper bits of reg remain unchanged
                    // For the case where src == reg, since we are only writing reg[0], as long as we iterate
                    // so that src[0] is consumed before writing reg[0], we do not need a temporary.
                    getEmitter()->emitIns_R_R_I_I(INS_mov, emitTypeSize(type), reg, src, 0, i);
                }
                else
                {
                    // If the register piece is to be passed in an integer register
                    // Use a vector mov to general purpose register instruction
                    // mov reg, src[i]
                    // This effectively moves from `src[i]` to `reg`
                    getEmitter()->emitIns_R_R_I(INS_mov, emitTypeSize(type), reg, src, i);
                }
            }
#endif // _TARGET_ARM64_
        }
    }
    else // op1 must be multi-reg GT_CALL
    {
        assert(op1->IsMultiRegCall() || op1->IsCopyOrReloadOfMultiRegCall());

        genConsumeRegs(op1);

        GenTree*     actualOp1 = op1->gtSkipReloadOrCopy();
        GenTreeCall* call      = actualOp1->AsCall();

        ReturnTypeDesc* pRetTypeDesc;
        unsigned        regCount;
        unsigned        matchingCount = 0;

        pRetTypeDesc = call->GetReturnTypeDesc();
        regCount     = pRetTypeDesc->GetReturnRegCount();

        var_types regType[MAX_RET_REG_COUNT];
        regNumber returnReg[MAX_RET_REG_COUNT];
        regNumber allocatedReg[MAX_RET_REG_COUNT];
        regMaskTP srcRegsMask       = 0;
        regMaskTP dstRegsMask       = 0;
        bool      needToShuffleRegs = false; // Set to true if we have to move any registers

        for (unsigned i = 0; i < regCount; ++i)
        {
            regType[i]   = pRetTypeDesc->GetReturnRegType(i);
            returnReg[i] = pRetTypeDesc->GetABIReturnReg(i);

            regNumber reloadReg = REG_NA;
            if (op1->IsCopyOrReload())
            {
                // GT_COPY/GT_RELOAD will have valid reg for those positions
                // that need to be copied or reloaded.
                reloadReg = op1->AsCopyOrReload()->GetRegNumByIdx(i);
            }

            if (reloadReg != REG_NA)
            {
                allocatedReg[i] = reloadReg;
            }
            else
            {
                allocatedReg[i] = call->GetRegNumByIdx(i);
            }

            if (returnReg[i] == allocatedReg[i])
            {
                matchingCount++;
            }
            else // We need to move this value
            {
                // We want to move the value from allocatedReg[i] into returnReg[i]
                // so record these two registers in the src and dst masks
                //
                srcRegsMask |= genRegMask(allocatedReg[i]);
                dstRegsMask |= genRegMask(returnReg[i]);

                needToShuffleRegs = true;
            }
        }

        if (needToShuffleRegs)
        {
            assert(matchingCount < regCount);

            unsigned  remainingRegCount = regCount - matchingCount;
            regMaskTP extraRegMask      = treeNode->gtRsvdRegs;

            while (remainingRegCount > 0)
            {
                // set 'available' to the 'dst' registers that are not currently holding 'src' registers
                //
                regMaskTP availableMask = dstRegsMask & ~srcRegsMask;

                regMaskTP dstMask;
                regNumber srcReg;
                regNumber dstReg;
                var_types curType   = TYP_UNKNOWN;
                regNumber freeUpReg = REG_NA;

                if (availableMask == 0)
                {
                    // Circular register dependencies
                    // So just free up the lowest register in dstRegsMask by moving it to the 'extra' register

                    assert(dstRegsMask == srcRegsMask);         // this has to be true for us to reach here
                    assert(extraRegMask != 0);                  // we require an 'extra' register
                    assert((extraRegMask & ~dstRegsMask) != 0); // it can't be part of dstRegsMask

                    availableMask = extraRegMask & ~dstRegsMask;

                    regMaskTP srcMask = genFindLowestBit(srcRegsMask);
                    freeUpReg         = genRegNumFromMask(srcMask);
                }

                dstMask = genFindLowestBit(availableMask);
                dstReg  = genRegNumFromMask(dstMask);
                srcReg  = REG_NA;

                if (freeUpReg != REG_NA)
                {
                    // We will free up the srcReg by moving it to dstReg which is an extra register
                    //
                    srcReg = freeUpReg;

                    // Find the 'srcReg' and set 'curType', change allocatedReg[] to dstReg
                    // and add the new register mask bit to srcRegsMask
                    //
                    for (unsigned i = 0; i < regCount; ++i)
                    {
                        if (allocatedReg[i] == srcReg)
                        {
                            curType         = regType[i];
                            allocatedReg[i] = dstReg;
                            srcRegsMask |= genRegMask(dstReg);
                        }
                    }
                }
                else // The normal case
                {
                    // Find the 'srcReg' and set 'curType'
                    //
                    for (unsigned i = 0; i < regCount; ++i)
                    {
                        if (returnReg[i] == dstReg)
                        {
                            srcReg  = allocatedReg[i];
                            curType = regType[i];
                        }
                    }
                    // After we perform this move we will have one less registers to setup
                    remainingRegCount--;
                }
                assert(curType != TYP_UNKNOWN);

                inst_RV_RV(ins_Copy(curType), dstReg, srcReg, curType);

                // Clear the appropriate bits in srcRegsMask and dstRegsMask
                srcRegsMask &= ~genRegMask(srcReg);
                dstRegsMask &= ~genRegMask(dstReg);

            } // while (remainingRegCount > 0)

        } // (needToShuffleRegs)

    } // op1 must be multi-reg GT_CALL
}

//------------------------------------------------------------------------
// genAllocLclFrame: Probe the stack and allocate the local stack frame: subtract from SP.
//
// Notes:
//      On ARM64, this only does the probing; allocating the frame is done when callee-saved registers are saved.
//      This is done before anything has been pushed. The previous frame might have a large outgoing argument
//      space that has been allocated, but the lowest addresses have not been touched. Our frame setup might
//      not touch up to the first 504 bytes. This means we could miss a guard page. On Windows, however,
//      there are always three guard pages, so we will not miss them all. On Linux, there is only one guard
//      page by default, so we need to be more careful. We do an extra probe if we might not have probed
//      recently enough. That is, if a call and prolog establishment might lead to missing a page. We do this
//      on Windows as well just to be consistent, even though it should not be necessary.
//
//      On ARM32, the first instruction of the prolog is always a push (which touches the lowest address
//      of the stack), either of the LR register or of some argument registers, e.g., in the case of
//      pre-spilling. The LR register is always pushed because we require it to allow for GC return
//      address hijacking (see the comment in CodeGen::genPushCalleeSavedRegisters()). These pushes
//      happen immediately before calling this function, so the SP at the current location has already
//      been touched.
//
void CodeGen::genAllocLclFrame(unsigned frameSize, regNumber initReg, bool* pInitRegZeroed, regMaskTP maskArgRegsLiveIn)
{
    assert(compiler->compGeneratingProlog);

    if (frameSize == 0)
    {
        return;
    }

    const target_size_t pageSize = compiler->eeGetPageSize();

#ifdef _TARGET_ARM64_
    // What offset from the final SP was the last probe? If we haven't probed almost a complete page, and
    // if the next action on the stack might subtract from SP first, before touching the current SP, then
    // we do one more probe at the very bottom. This can happen if we call a function on arm64 that does
    // a "STP fp, lr, [sp-504]!", that is, pre-decrement SP then store. Note that we probe here for arm64,
    // but we don't alter SP.
    target_size_t lastTouchDelta = 0;
#endif // _TARGET_ARM64_

    assert(!compiler->info.compPublishStubParam || (REG_SECRET_STUB_PARAM != initReg));

    if (frameSize < pageSize)
    {
#ifdef _TARGET_ARM_
        inst_RV_IV(INS_sub, REG_SPBASE, frameSize, EA_PTRSIZE);
#endif // _TARGET_ARM_

#ifdef _TARGET_ARM64_
        lastTouchDelta = frameSize;
#endif // _TARGET_ARM64_
    }
    else if (frameSize < compiler->getVeryLargeFrameSize())
    {
#if defined(_TARGET_ARM64_)
        regNumber rTemp = REG_ZR; // We don't need a register for the target of the dummy load
        lastTouchDelta  = frameSize;
#else
        regNumber rTemp = initReg;
#endif

        for (target_size_t probeOffset = pageSize; probeOffset <= frameSize; probeOffset += pageSize)
        {
            // Generate:
            //    movw initReg, -probeOffset
            //    ldr rTemp, [SP + initReg]  // load into initReg on arm32, wzr on ARM64

            instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -(ssize_t)probeOffset);
            getEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, rTemp, REG_SPBASE, initReg);
            regSet.verifyRegUsed(initReg);
            *pInitRegZeroed = false; // The initReg does not contain zero

#ifdef _TARGET_ARM64_
            lastTouchDelta -= pageSize;
#endif // _TARGET_ARM64_
        }

#ifdef _TARGET_ARM64_
        assert(lastTouchDelta == frameSize % pageSize);
        compiler->unwindPadding();
#else  // !_TARGET_ARM64_
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, frameSize);
        compiler->unwindPadding();
        getEmitter()->emitIns_R_R_R(INS_sub, EA_4BYTE, REG_SPBASE, REG_SPBASE, initReg);
#endif // !_TARGET_ARM64_
    }
    else
    {
        assert(frameSize >= compiler->getVeryLargeFrameSize());

        // Emit the following sequence to 'tickle' the pages. Note it is important that stack pointer not change
        // until this is complete since the tickles could cause a stack overflow, and we need to be able to crawl
        // the stack afterward (which means the stack pointer needs to be known).
        //
        // ARM64 needs 2 registers. ARM32 needs 3 registers. See VERY_LARGE_FRAME_SIZE_REG_MASK for how these
        // are reserved.

        regMaskTP availMask = RBM_ALLINT & (regSet.rsGetModifiedRegsMask() | ~RBM_INT_CALLEE_SAVED);
        availMask &= ~maskArgRegsLiveIn;   // Remove all of the incoming argument registers as they are currently live
        availMask &= ~genRegMask(initReg); // Remove the pre-calculated initReg

        regNumber rOffset = initReg;
        regNumber rLimit;
        regMaskTP tempMask;

#if defined(_TARGET_ARM64_)

        regNumber rTemp = REG_ZR; // We don't need a register for the target of the dummy load

#else // _TARGET_ARM_

        regNumber rTemp;

        // We pick the next lowest register number for rTemp
        noway_assert(availMask != RBM_NONE);
        tempMask = genFindLowestBit(availMask);
        rTemp    = genRegNumFromMask(tempMask);
        availMask &= ~tempMask;

#endif // _TARGET_ARM_

        // We pick the next lowest register number for rLimit
        noway_assert(availMask != RBM_NONE);
        tempMask = genFindLowestBit(availMask);
        rLimit   = genRegNumFromMask(tempMask);
        availMask &= ~tempMask;

        // Generate:
        //
        //      mov rOffset, -pageSize    // On arm, this turns out to be "movw r1, 0xf000; sxth r1, r1".
        //                                // We could save 4 bytes in the prolog by using "movs r1, 0" at the
        //                                // runtime expense of running a useless first loop iteration.
        //      mov rLimit, -frameSize
        // loop:
        //      ldr rTemp, [sp + rOffset] // rTemp = wzr on ARM64
        //      sub rOffset, pageSize     // Note that 0x1000 (normal ARM32 pageSize) on ARM32 uses the funky
        //                                // Thumb immediate encoding
        //      cmp rLimit, rOffset
        //      b.ls loop                 // If rLimit is lower or same, we need to probe this rOffset. Note
        //                                // especially that if it is the same, we haven't probed this page.

        noway_assert((ssize_t)(int)frameSize == (ssize_t)frameSize); // make sure framesize safely fits within an int

        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rOffset, -(ssize_t)pageSize);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, rLimit, -(ssize_t)frameSize);

        //
        // Can't have a label inside the ReJIT padding area
        //
        genPrologPadForReJit();

        // There's a "virtual" label here. But we can't create a label in the prolog, so we use the magic
        // `emitIns_J` with a negative `instrCount` to branch back a specific number of instructions.

        getEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, rTemp, REG_SPBASE, rOffset);
#if defined(_TARGET_ARM_)
        regSet.verifyRegUsed(rTemp);
        getEmitter()->emitIns_R_I(INS_sub, EA_PTRSIZE, rOffset, pageSize);
#elif defined(_TARGET_ARM64_)
        getEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, rOffset, rOffset, pageSize);
#endif
        getEmitter()->emitIns_R_R(INS_cmp, EA_PTRSIZE, rLimit, rOffset); // If equal, we need to probe again
        getEmitter()->emitIns_J(INS_bls, NULL, -4);

        *pInitRegZeroed = false; // The initReg does not contain zero

        compiler->unwindPadding();

#ifdef _TARGET_ARM64_
        lastTouchDelta = frameSize % pageSize;
#endif // _TARGET_ARM64_

#ifdef _TARGET_ARM_
        inst_RV_RV(INS_add, REG_SPBASE, rLimit, TYP_I_IMPL);
#endif // _TARGET_ARM_
    }

#ifdef _TARGET_ARM64_
    if (lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES > pageSize)
    {
        assert(lastTouchDelta + STACK_PROBE_BOUNDARY_THRESHOLD_BYTES < 2 * pageSize);
        instGen_Set_Reg_To_Imm(EA_PTRSIZE, initReg, -(ssize_t)frameSize);
        getEmitter()->emitIns_R_R_R(INS_ldr, EA_4BYTE, REG_ZR, REG_SPBASE, initReg);
        compiler->unwindPadding();

        regSet.verifyRegUsed(initReg);
        *pInitRegZeroed = false; // The initReg does not contain zero
    }
#endif // _TARGET_ARM64_

#ifdef _TARGET_ARM_
    compiler->unwindAllocStack(frameSize);
#ifdef USING_SCOPE_INFO
    if (!doubleAlignOrFramePointerUsed())
    {
        psiAdjustStackLevel(frameSize);
    }
#endif // USING_SCOPE_INFO
#endif // _TARGET_ARM_
}

#endif // _TARGET_ARMARCH_
