// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#ifdef TARGET_ARMARCH // This file is ONLY used for ARM and ARM64 architectures

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
//    regTmp                  - an available temporary register that is used if 'spDelta' cannot be encoded by
//                              'sub sp, sp, #spDelta' instruction.
//                              Can be REG_NA if the caller knows for certain that 'spDelta' fits into the immediate
//                              value range.
//
// Return Value:
//    None.
//
void CodeGen::genStackPointerConstantAdjustment(ssize_t spDelta, regNumber regTmp)
{
    assert(spDelta < 0);

    // We assert that the SP change is less than one page. If it's greater, you should have called a
    // function that does a probe, which will in turn call this function.
    assert((target_size_t)(-spDelta) <= compiler->eeGetPageSize());

#ifdef TARGET_ARM64
    genInstrWithConstant(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, -spDelta, regTmp);
#else
    genInstrWithConstant(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, -spDelta, INS_FLAGS_DONT_CARE, regTmp);
#endif
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
    GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regTmp, REG_SP, 0);
    genStackPointerConstantAdjustment(spDelta, regTmp);
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

        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, regTmp, REG_SP, 0);
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
    regNumber targetReg  = treeNode->GetRegNum();
    var_types targetType = treeNode->TypeGet();
    emitter*  emit       = GetEmitter();

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

    // Is this a node whose value is already in a register?  LSRA denotes this by
    // setting the GTF_REUSE_REG_VAL flag.
    if (treeNode->IsReuseRegVal())
    {
        // For now, this is only used for constant nodes.
        assert((treeNode->OperGet() == GT_CNS_INT) || (treeNode->OperGet() == GT_CNS_DBL));
        JITDUMP("  TreeNode is marked ReuseReg\n");
        return;
    }

    // contained nodes are part of their parents for codegen purposes
    // ex : immediates, most LEAs
    if (treeNode->isContained())
    {
        return;
    }

    switch (treeNode->gtOper)
    {
        case GT_START_NONGC:
            GetEmitter()->emitDisableGC();
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

#if defined(TARGET_ARM64)
        case GT_BSWAP:
        case GT_BSWAP16:
            genCodeForBswap(treeNode);
            break;
#endif // defined(TARGET_ARM64)

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

            FALLTHROUGH;

#if !defined(TARGET_64BIT)
        case GT_ADD_LO:
        case GT_ADD_HI:
        case GT_SUB_LO:
        case GT_SUB_HI:
#endif // !defined(TARGET_64BIT)

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

#if !defined(TARGET_64BIT)

        case GT_LSH_HI:
        case GT_RSH_LO:
            genCodeForShiftLong(treeNode);
            break;

#endif // !defined(TARGET_64BIT)

        case GT_CAST:
            genCodeForCast(treeNode->AsOp());
            break;

        case GT_BITCAST:
            genCodeForBitCast(treeNode->AsOp());
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

#ifdef TARGET_ARM
        case GT_MUL_LONG:
            genCodeForMulLong(treeNode->AsMultiRegOp());
            break;
#endif // TARGET_ARM

#ifdef TARGET_ARM64

        case GT_INC_SATURATE:
            genCodeForIncSaturate(treeNode);
            break;

        case GT_MULHI:
            genCodeForMulHi(treeNode->AsOp());
            break;

        case GT_SWAP:
            genCodeForSwap(treeNode->AsOp());
            break;
#endif // TARGET_ARM64

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
        case GT_HWINTRINSIC:
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
#ifdef TARGET_ARM64
        case GT_TEST_EQ:
        case GT_TEST_NE:
#endif // TARGET_ARM64
            genCodeForCompare(treeNode->AsOp());
            break;

        case GT_JTRUE:
            genCodeForJumpTrue(treeNode->AsOp());
            break;

#ifdef TARGET_ARM64
        case GT_JCMP:
            genCodeForJumpCompare(treeNode->AsOp());
            break;
#endif // TARGET_ARM64

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
        {
            CodeGen::BarrierKind barrierKind =
                treeNode->gtFlags & GTF_MEMORYBARRIER_LOAD ? BARRIER_LOAD_ONLY : BARRIER_FULL;

            instGen_MemoryBarrier(barrierKind);
            break;
        }

#ifdef TARGET_ARM64
        case GT_XCHG:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
            genLockedInstructions(treeNode->AsOp());
            break;

        case GT_CMPXCHG:
            genCodeForCmpXchg(treeNode->AsCmpXchg());
            break;
#endif // TARGET_ARM64

        case GT_RELOAD:
            // do nothing - reload is just a marker.
            // The parent node will call genConsumeReg on this which will trigger the unspill of this node's child
            // into the register specified in this node.
            break;

        case GT_NOP:
            break;

        case GT_KEEPALIVE:
            if (treeNode->AsOp()->gtOp1->isContained())
            {
                // For this case we simply need to update the lifetime of the local.
                genUpdateLife(treeNode->AsOp()->gtOp1);
            }
            else
            {
                genConsumeReg(treeNode->AsOp()->gtOp1);
            }
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
            genCodeForNullCheck(treeNode->AsIndir());
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

#ifdef PSEUDORANDOM_NOP_INSERTION
            // the runtime side requires the codegen here to be consistent
            emit->emitDisableRandomNops();
#endif // PSEUDORANDOM_NOP_INSERTION
            break;

        case GT_LABEL:
            genPendingCallLabel = genCreateTempLabel();
#if defined(TARGET_ARM)
            genMov32RelocatableDisplacement(genPendingCallLabel, targetReg);
#else
            emit->emitIns_R_L(INS_adr, EA_PTRSIZE, genPendingCallLabel, targetReg);
#endif
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

#ifdef TARGET_ARM

        case GT_CLS_VAR_ADDR:
            emit->emitIns_R_C(INS_lea, EA_PTRSIZE, targetReg, treeNode->AsClsVar()->gtClsVarHnd, 0);
            genProduceReg(treeNode);
            break;

        case GT_LONG:
            assert(treeNode->isUsedFromReg());
            genConsumeRegs(treeNode);
            break;

#endif // TARGET_ARM

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
void CodeGen::genSetRegToIcon(regNumber reg, ssize_t val, var_types type, insFlags flags DEBUGARG(GenTreeFlags gtFlags))
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
// genSetGSSecurityCookie: Set the "GS" security cookie in the prolog.
//
// Arguments:
//     initReg        - register to use as a scratch register
//     pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'false' if and only if
//                      this call sets 'initReg' to a non-zero value.
//
// Return Value:
//     None
//
void CodeGen::genSetGSSecurityCookie(regNumber initReg, bool* pInitRegZeroed)
{
    assert(compiler->compGeneratingProlog);

    if (!compiler->getNeedsGSSecurityCookie())
    {
        return;
    }

    if (compiler->gsGlobalSecurityCookieAddr == nullptr)
    {
        noway_assert(compiler->gsGlobalSecurityCookieVal != 0);
        // initReg = #GlobalSecurityCookieVal; [frame.GSSecurityCookie] = initReg
        genSetRegToIcon(initReg, compiler->gsGlobalSecurityCookieVal, TYP_I_IMPL);
        GetEmitter()->emitIns_S_R(INS_str, EA_PTRSIZE, initReg, compiler->lvaGSSecurityCookie, 0);
    }
    else
    {
        instGen_Set_Reg_To_Imm(EA_PTR_DSP_RELOC, initReg, (ssize_t)compiler->gsGlobalSecurityCookieAddr,
                               INS_FLAGS_DONT_CARE DEBUGARG((size_t)THT_SetGSCookie) DEBUGARG(GTF_EMPTY));
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_PTRSIZE, initReg, initReg, 0);
        regSet.verifyRegUsed(initReg);
        GetEmitter()->emitIns_S_R(INS_str, EA_PTRSIZE, initReg, compiler->lvaGSSecurityCookie, 0);
    }

    *pInitRegZeroed = false;
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
    GenTree* srcNode = treeNode->AsOp()->gtOp1;
    assert(varTypeIsFloating(srcNode));
    assert(srcNode->TypeGet() == treeNode->TypeGet());

    // Right now only Abs/Ceiling/Floor/Round/Sqrt are treated as math intrinsics.
    //
    switch (treeNode->AsIntrinsic()->gtIntrinsicName)
    {
        case NI_System_Math_Abs:
            genConsumeOperands(treeNode->AsOp());
            GetEmitter()->emitInsBinary(INS_ABS, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;

#ifdef TARGET_ARM64
        case NI_System_Math_Ceiling:
            genConsumeOperands(treeNode->AsOp());
            GetEmitter()->emitInsBinary(INS_frintp, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;

        case NI_System_Math_Floor:
            genConsumeOperands(treeNode->AsOp());
            GetEmitter()->emitInsBinary(INS_frintm, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;

        case NI_System_Math_Round:
            genConsumeOperands(treeNode->AsOp());
            GetEmitter()->emitInsBinary(INS_frintn, emitActualTypeSize(treeNode), treeNode, srcNode);
            break;
#endif // TARGET_ARM64

        case NI_System_Math_Sqrt:
            genConsumeOperands(treeNode->AsOp());
            GetEmitter()->emitInsBinary(INS_SQRT, emitActualTypeSize(treeNode), treeNode, srcNode);
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
    GenTree* source = treeNode->gtOp1;
#if !defined(OSX_ARM64_ABI)
    var_types targetType = genActualType(source->TypeGet());
#else
    var_types targetType = source->TypeGet();
#endif
    emitter* emit = GetEmitter();

    // This is the varNum for our store operations,
    // typically this is the varNum for the Outgoing arg space
    // When we are generating a tail call it will be the varNum for arg0
    unsigned varNumOut    = (unsigned)-1;
    unsigned argOffsetMax = (unsigned)-1; // Records the maximum size of this area for assert checks

    // Get argument offset to use with 'varNumOut'
    // Here we cross check that argument offset hasn't changed from lowering to codegen since
    // we are storing arg slot number in GT_PUTARG_STK node in lowering phase.
    unsigned argOffsetOut = treeNode->getArgOffset();

#ifdef DEBUG
    fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(treeNode->gtCall, treeNode);
    assert(curArgTabEntry != nullptr);
    DEBUG_ARG_SLOTS_ASSERT(argOffsetOut == (curArgTabEntry->slotNum * TARGET_POINTER_SIZE));
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
            assert((srcReg != REG_NA) && (genIsValidFloatReg(srcReg)));

#if !defined(OSX_ARM64_ABI)
            assert(treeNode->GetStackByteSize() % TARGET_POINTER_SIZE == 0);
#else  // OSX_ARM64_ABI
            if (treeNode->GetStackByteSize() == 12)
            {
                regNumber tmpReg = treeNode->GetSingleTempReg();
                GetEmitter()->emitStoreSIMD12ToLclOffset(varNumOut, argOffsetOut, srcReg, tmpReg);
                argOffsetOut += 12;
            }
            else
#endif // OSX_ARM64_ABI
            {
                emitAttr storeAttr = emitTypeSize(targetType);
                emit->emitIns_S_R(INS_str, storeAttr, srcReg, varNumOut, argOffsetOut);
                argOffsetOut += EA_SIZE_IN_BYTES(storeAttr);
            }
            assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area
            return;
        }

#if defined(OSX_ARM64_ABI)
        switch (treeNode->GetStackByteSize())
        {
            case 1:
                targetType = TYP_BYTE;
                break;
            case 2:
                targetType = TYP_SHORT;
                break;
            default:
                assert(treeNode->GetStackByteSize() >= 4);
                break;
        }
#endif

        instruction storeIns  = ins_Store(targetType);
        emitAttr    storeAttr = emitTypeSize(targetType);

        // If it is contained then source must be the integer constant zero
        if (source->isContained())
        {
#ifdef TARGET_ARM64
            assert(source->OperGet() == GT_CNS_INT);
            assert(source->AsIntConCommon()->IconValue() == 0);

            emit->emitIns_S_R(storeIns, storeAttr, REG_ZR, varNumOut, argOffsetOut);
#else  // !TARGET_ARM64
            // There is no zero register on ARM32
            unreached();
#endif // !TARGET_ARM64
        }
        else
        {
            genConsumeReg(source);
            emit->emitIns_S_R(storeIns, storeAttr, source->GetRegNum(), varNumOut, argOffsetOut);
#ifdef TARGET_ARM
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
#endif // TARGET_ARM
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
            noway_assert(source->OperIs(GT_LCL_VAR, GT_OBJ));

            var_types targetType = source->TypeGet();
            noway_assert(varTypeIsStruct(targetType));

            // We will copy this struct to the stack, possibly using a ldp/ldr instruction
            // in ARM64/ARM
            // Setup loReg (and hiReg) from the internal registers that we reserved in lower.
            //
            regNumber loReg = treeNode->ExtractTempReg();
#ifdef TARGET_ARM64
            regNumber hiReg = treeNode->GetSingleTempReg();
#endif // TARGET_ARM64
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

                addrNode = source->AsOp()->gtOp1;

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
                    assert(addrNode->isContained());
                    varNode  = addrNode->AsLclVarCommon();
                    addrNode = nullptr;
                }
                else // addrNode is used
                {
                    // TODO-Cleanup: `Lowering::NewPutArg` marks only `LCL_VAR_ADDR` as contained nowadays,
                    // but we use `genConsumeAddress` as a precaution, use `genConsumeReg()` instead.
                    assert(!addrNode->isContained());
                    // Generate code to load the address that we need into a register
                    genConsumeAddress(addrNode);
                    addrReg = addrNode->GetRegNum();

#ifdef TARGET_ARM64
                    // If addrReg equal to loReg, swap(loReg, hiReg)
                    // This reduces code complexity by only supporting one addrReg overwrite case
                    if (loReg == addrReg)
                    {
                        loReg = hiReg;
                        hiReg = addrReg;
                    }
#endif // TARGET_ARM64
                }
            }

            // Either varNode or addrNOde must have been setup above,
            // the xor ensures that only one of the two is setup, not both
            assert((varNode != nullptr) ^ (addrNode != nullptr));

            ClassLayout* layout;
            unsigned     srcSize;
            bool         isHfa;

            // Setup the srcSize, isHFa, and gcPtrCount
            if (source->OperGet() == GT_LCL_VAR)
            {
                assert(varNode != nullptr);
                LclVarDsc* varDsc = compiler->lvaGetDesc(varNode);

                // This struct also must live in the stack frame
                // And it can't live in a register (SIMD)
                assert(varDsc->lvType == TYP_STRUCT);
                assert(varDsc->lvOnFrame && !varDsc->lvRegister);

                srcSize = varDsc->lvSize();
                isHfa   = varDsc->lvIsHfa();
                layout  = varDsc->GetLayout();
            }
            else // we must have a GT_OBJ
            {
                assert(source->OperGet() == GT_OBJ);

                // If the source is an OBJ node then we need to use the type information
                // it provides (size and GC layout) even if the node wraps a lclvar. Due
                // to struct reinterpretation (e.g. Unsafe.As<X, Y>) it is possible that
                // the OBJ node has a different type than the lclvar.
                layout  = source->AsObj()->GetLayout();
                srcSize = layout->GetSize();
                isHfa   = compiler->IsHfa(layout->GetClassHandle());
            }

            // If we have an HFA we can't have any GC pointers,
            // if not then the max size for the the struct is 16 bytes
            if (isHfa)
            {
                noway_assert(!layout->HasGCPtr());
            }
#ifdef TARGET_ARM64
            else
            {
                noway_assert(srcSize <= 2 * TARGET_POINTER_SIZE);
            }

            noway_assert(srcSize <= MAX_PASS_MULTIREG_BYTES);
#endif // TARGET_ARM64

            unsigned structSize;

            unsigned dstSize = treeNode->GetStackByteSize();
            if (dstSize != srcSize)
            {
                // We can generate a smaller code if store size is a multiple of TARGET_POINTER_SIZE.
                // The dst size can be rounded up to PUTARG_STK size.
                // The src size can be rounded up if it reads a local variable slot because the local
                // variable stack allocation size is rounded up to be a multiple of the TARGET_POINTER_SIZE.
                // The exception  is arm64 apple arguments because they can be passed without padding.
                if (varNode != nullptr)
                {
                    // If we have a varNode, even if it was casted using `OBJ`, we can read its original memory size.
                    const LclVarDsc* varDsc       = compiler->lvaGetDesc(varNode);
                    const unsigned   varStackSize = varDsc->lvSize();
                    if (varStackSize >= srcSize)
                    {
                        srcSize = varStackSize;
                    }
                }
            }
            if (dstSize == srcSize)
            {
                structSize = dstSize;
            }
            else
            {
                // With Unsafe object cast we can have different strange combinations:
                // PutArgStk<8>(Obj<16>(LclVar<8>)) -> copy 8 bytes;
                // PutArgStk<16>(Obj<16>(LclVar<8>)) -> copy 16 bytes, reading undefined memory after the local.
                structSize = min(dstSize, srcSize);
            }

            int      remainingSize = structSize;
            unsigned structOffset  = 0;
            unsigned nextIndex     = 0;

#ifdef TARGET_ARM64
            // For a >= 16-byte structSize we will generate a ldp and stp instruction each loop
            //             ldp     x2, x3, [x0]
            //             stp     x2, x3, [sp, #16]

            while (remainingSize >= 2 * TARGET_POINTER_SIZE)
            {
                var_types type0 = layout->GetGCPtrType(nextIndex + 0);
                var_types type1 = layout->GetGCPtrType(nextIndex + 1);

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
#else  // TARGET_ARM
            // For a >= 4 byte structSize we will generate a ldr and str instruction each loop
            //             ldr     r2, [r0]
            //             str     r2, [sp, #16]
            while (remainingSize >= TARGET_POINTER_SIZE)
            {
                var_types type = layout->GetGCPtrType(nextIndex);

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
#endif // TARGET_ARM

            // For a 12-byte structSize we will generate two load instructions
            //             ldr     x2, [x0]
            //             ldr     w3, [x0, #8]
            //             str     x2, [sp, #16]
            //             str     w3, [sp, #24]

            while (remainingSize > 0)
            {
                var_types type;

                if (remainingSize >= TARGET_POINTER_SIZE)
                {
                    type = layout->GetGCPtrType(nextIndex);
                }
                else // (remainingSize < TARGET_POINTER_SIZE)
                {
                    // the left over size is smaller than a pointer and thus can never be a GC type
                    assert(!layout->IsGCPtr(nextIndex));

                    if (remainingSize == 1)
                    {
                        type = TYP_UBYTE;
                    }
                    else if (remainingSize == 2)
                    {
                        type = TYP_USHORT;
                    }
                    else
                    {
                        assert(remainingSize == 4);
                        type = TYP_UINT;
                    }
                }
                const emitAttr attr     = emitTypeSize(type);
                const unsigned moveSize = genTypeSize(type);
                assert(EA_SIZE_IN_BYTES(attr) == moveSize);

                remainingSize -= moveSize;

                instruction loadIns = ins_Load(type);
                if (varNode != nullptr)
                {
                    // Load from our varNumImp source
                    emit->emitIns_R_S(loadIns, attr, loReg, varNode->GetLclNum(), structOffset);
                }
                else
                {
                    assert(loReg != addrReg);
                    // Load from our address expression source
                    emit->emitIns_R_R_I(loadIns, attr, loReg, addrReg, structOffset);
                }

                // Emit a store instruction to store the register into the outgoing argument area
                instruction storeIns = ins_Store(type);
                emit->emitIns_S_R(storeIns, attr, loReg, varNumOut, argOffsetOut);
                argOffsetOut += moveSize;
                assert(argOffsetOut <= argOffsetMax); // We can't write beyound the outgoing area area

                structOffset += moveSize;
                nextIndex++;
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
    regNumber targetReg  = tree->GetRegNum();

    assert(targetType != TYP_STRUCT);

    GenTree* op1 = tree->gtOp1;
    genConsumeReg(op1);

    // If child node is not already in the register we need, move it
    inst_Mov(targetType, targetReg, op1->GetRegNum(), /* canSkip */ true);

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
    emitter* emit         = GetEmitter();
    unsigned varNumOut    = compiler->lvaOutgoingArgSpaceVar;
    unsigned argOffsetMax = compiler->lvaOutgoingArgSpaceSize;
    unsigned argOffsetOut = treeNode->getArgOffset();

    if (source->OperGet() == GT_FIELD_LIST)
    {
        // Evaluate each of the GT_FIELD_LIST items into their register
        // and store their register into the outgoing argument area
        unsigned regIndex = 0;
        for (GenTreeFieldList::Use& use : source->AsFieldList()->Uses())
        {
            GenTree*  nextArgNode = use.GetNode();
            regNumber fieldReg    = nextArgNode->GetRegNum();
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
#ifdef TARGET_ARM
                if (type == TYP_LONG)
                {
                    // We should only see long fields for DOUBLEs passed in 2 integer registers, via bitcast.
                    // All other LONGs should have been decomposed.
                    // Handle the first INT, and then handle the 2nd below.
                    assert(nextArgNode->OperIs(GT_BITCAST));
                    type = TYP_INT;
                    inst_Mov(type, argReg, fieldReg, /* canSkip */ true);

                    // Now set up the next register for the 2nd INT
                    argReg = REG_NEXT(argReg);
                    regIndex++;
                    assert(argReg == treeNode->GetRegNumByIdx(regIndex));
                    fieldReg = nextArgNode->AsMultiRegOp()->GetRegNumByIdx(1);
                }
#endif // TARGET_ARM

                // If child node is not already in the register we need, move it
                inst_Mov(type, argReg, fieldReg, /* canSkip */ true);

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

        addrNode = source->AsOp()->gtOp1;

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

        // This is the varNum for our load operations,
        // only used when we have a struct with a LclVar source
        unsigned srcVarNum = BAD_VAR_NUM;

        if (varNode != nullptr)
        {
            assert(varNode->isContained());
            srcVarNum = varNode->GetLclNum();
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
            // TODO-Cleanup: `Lowering::NewPutArg` marks only `LCL_VAR_ADDR` as contained nowadays,
            // but we use `genConsumeAddress` as a precaution, use `genConsumeReg()` instead.
            assert(!addrNode->isContained());

            // Generate code to load the address that we need into a register
            genConsumeAddress(addrNode);
            addrReg = addrNode->GetRegNum();

            // If addrReg equal to baseReg, we use the last target register as alternative baseReg.
            // Because the candidate mask for the internal baseReg does not include any of the target register,
            // we can ensure that baseReg, addrReg, and the last target register are not all same.
            assert(baseReg != addrReg);

            // We don't split HFA struct
            assert(!compiler->IsHfa(source->AsObj()->GetLayout()->GetClassHandle()));
        }

        ClassLayout* layout = source->AsObj()->GetLayout();

        // Put on stack first
        unsigned nextIndex     = treeNode->gtNumRegs;
        unsigned structOffset  = nextIndex * TARGET_POINTER_SIZE;
        int      remainingSize = treeNode->GetStackByteSize();

        // remainingSize is always multiple of TARGET_POINTER_SIZE
        assert(remainingSize % TARGET_POINTER_SIZE == 0);
        while (remainingSize > 0)
        {
            var_types type = layout->GetGCPtrType(nextIndex);

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
                    emit->emitIns_Mov(INS_mov, emitActualTypeSize(type), baseReg, addrReg, /* canSkip */ false);
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

#ifdef FEATURE_SIMD
//----------------------------------------------------------------------------------
// genMultiRegStoreToSIMDLocal: store multi-reg value to a single-reg SIMD local
//
// Arguments:
//    lclNode  -  GentreeLclVar of GT_STORE_LCL_VAR
//
// Return Value:
//    None
//
void CodeGen::genMultiRegStoreToSIMDLocal(GenTreeLclVar* lclNode)
{
    regNumber dst       = lclNode->GetRegNum();
    GenTree*  op1       = lclNode->gtGetOp1();
    GenTree*  actualOp1 = op1->gtSkipReloadOrCopy();
    unsigned  regCount =
        actualOp1->IsMultiRegLclVar() ? actualOp1->AsLclVar()->GetFieldCount(compiler) : actualOp1->GetMultiRegCount();
    assert(op1->IsMultiRegNode());
    genConsumeRegs(op1);

    // Treat dst register as a homogenous vector with element size equal to the src size
    // Insert pieces in reverse order
    for (int i = regCount - 1; i >= 0; --i)
    {
        var_types type = op1->gtSkipReloadOrCopy()->GetRegTypeByIndex(i);
        regNumber reg  = actualOp1->GetRegByIndex(i);
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
            GetEmitter()->emitIns_R_R_I_I(INS_mov, emitTypeSize(type), dst, reg, i, 0);
        }
        else
        {
            // If the register piece was passed in an integer register
            // Use a vector mov from general purpose register instruction
            // mov dst[i], reg
            // This effectively moves from `reg` to `dst[i]`
            GetEmitter()->emitIns_R_R_I(INS_mov, emitTypeSize(type), dst, reg, i);
        }
    }

    genProduceReg(lclNode);
}
#endif // FEATURE_SIMD

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

    GetEmitter()->emitInsBinary(INS_cmp, emitActualTypeSize(bndsChkType), src1, src2);
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
    regNumber targetReg  = tree->GetRegNum();

    inst_Mov(targetType, targetReg, tree->gtSrcReg, /* canSkip */ true);
    genTransferRegGCState(targetReg, tree->gtSrcReg);

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
void CodeGen::genCodeForNullCheck(GenTreeIndir* tree)
{
#ifdef TARGET_ARM
    assert(!"GT_NULLCHECK isn't supported for Arm32; use GT_IND.");
#else
    assert(tree->OperIs(GT_NULLCHECK));
    GenTree* op1 = tree->gtOp1;

    genConsumeRegs(op1);
    regNumber targetReg = REG_ZR;

    GetEmitter()->emitInsLoadStoreOp(INS_ldr, EA_4BYTE, targetReg, tree);
#endif
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
    emitter*  emit      = GetEmitter();
    GenTree*  arrObj    = arrIndex->ArrObj();
    GenTree*  indexNode = arrIndex->IndexExpr();
    regNumber arrReg    = genConsumeReg(arrObj);
    regNumber indexReg  = genConsumeReg(indexNode);
    regNumber tgtReg    = arrIndex->GetRegNum();
    noway_assert(tgtReg != REG_NA);

    // We will use a temp register to load the lower bound and dimension size values.

    regNumber tmpReg = arrIndex->GetSingleTempReg();
    assert(tgtReg != tmpReg);

    unsigned  dim      = arrIndex->gtCurrDim;
    unsigned  rank     = arrIndex->gtArrRank;
    var_types elemType = arrIndex->gtArrElemType;
    unsigned  offset;

    offset = genOffsetOfMDArrayLowerBound(elemType, rank, dim);
    emit->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, arrReg, offset);
    emit->emitIns_R_R_R(INS_sub, EA_4BYTE, tgtReg, indexReg, tmpReg);

    offset = genOffsetOfMDArrayDimensionSize(elemType, rank, dim);
    emit->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, arrReg, offset);
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
    regNumber tgtReg     = arrOffset->GetRegNum();

    noway_assert(tgtReg != REG_NA);

    if (!offsetNode->IsIntegralConst(0))
    {
        emitter*  emit      = GetEmitter();
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
        emit->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, arrReg, offset);
        emit->emitIns_R_R_R_R(INS_MULADD, EA_PTRSIZE, tgtReg, tmpReg, offsetReg, indexReg);
    }
    else
    {
        regNumber indexReg = genConsumeReg(indexNode);
        inst_Mov(TYP_INT, tgtReg, indexReg, /* canSkip */ true);
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

    assert(tree->GetRegNum() != REG_NA);

    genConsumeOperands(tree->AsOp());

    GenTree* operand = tree->gtGetOp1();
    GenTree* shiftBy = tree->gtGetOp2();
    if (!shiftBy->IsCnsIntOrI())
    {
        GetEmitter()->emitIns_R_R_R(ins, size, tree->GetRegNum(), operand->GetRegNum(), shiftBy->GetRegNum());
    }
    else
    {
        unsigned immWidth   = emitter::getBitWidth(size); // For ARM64, immWidth will be set to 32 or 64
        unsigned shiftByImm = (unsigned)shiftBy->AsIntCon()->gtIconVal & (immWidth - 1);

        GetEmitter()->emitIns_R_R_I(ins, size, tree->GetRegNum(), operand->GetRegNum(), shiftByImm);
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
    regNumber targetReg  = tree->GetRegNum();

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
    regNumber targetReg  = tree->GetRegNum();
    emitter*  emit       = GetEmitter();

    NYI_IF(targetType == TYP_STRUCT, "GT_LCL_FLD: struct load local field not supported");
    assert(targetReg != REG_NA);

    unsigned offs   = tree->GetLclOffs();
    unsigned varNum = tree->GetLclNum();
    assert(varNum < compiler->lvaCount);

#ifdef TARGET_ARM
    if (tree->IsOffsetMisaligned())
    {
        // Arm supports unaligned access only for integer types,
        // load the floating data as 1 or 2 integer registers and convert them to float.
        regNumber addr = tree->ExtractTempReg();
        emit->emitIns_R_S(INS_lea, EA_PTRSIZE, addr, varNum, offs);

        if (targetType == TYP_FLOAT)
        {
            regNumber floatAsInt = tree->GetSingleTempReg();
            emit->emitIns_R_R(INS_ldr, EA_4BYTE, floatAsInt, addr);
            emit->emitIns_Mov(INS_vmov_i2f, EA_4BYTE, targetReg, floatAsInt, /* canSkip */ false);
        }
        else
        {
            regNumber halfdoubleAsInt1 = tree->ExtractTempReg();
            regNumber halfdoubleAsInt2 = tree->GetSingleTempReg();
            emit->emitIns_R_R_I(INS_ldr, EA_4BYTE, halfdoubleAsInt1, addr, 0);
            emit->emitIns_R_R_I(INS_ldr, EA_4BYTE, halfdoubleAsInt2, addr, 4);
            emit->emitIns_R_R_R(INS_vmov_i2d, EA_8BYTE, targetReg, halfdoubleAsInt1, halfdoubleAsInt2);
        }
    }
    else
#endif // TARGET_ARM
    {
        emitAttr    attr = emitActualTypeSize(targetType);
        instruction ins  = ins_Load(targetType);
        emit->emitIns_R_S(ins, attr, targetReg, varNum, offs);
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

    gcInfo.gcMarkRegPtrVal(base->GetRegNum(), base->TypeGet());
    assert(!varTypeIsGC(index->TypeGet()));

    // The index is never contained, even if it is a constant.
    assert(index->isUsedFromReg());

    const regNumber tmpReg = node->GetSingleTempReg();

    // Generate the bounds check if necessary.
    if ((node->gtFlags & GTF_INX_RNGCHK) != 0)
    {
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, base->GetRegNum(), node->gtLenOffset);
        GetEmitter()->emitIns_R_R(INS_cmp, emitActualTypeSize(index->TypeGet()), index->GetRegNum(), tmpReg);
        genJumpToThrowHlpBlk(EJ_hs, SCK_RNGCHK_FAIL, node->gtIndRngFailBB);
    }

    // Can we use a ScaledAdd instruction?
    //
    if (isPow2(node->gtElemSize) && (node->gtElemSize <= 32768))
    {
        DWORD scale;
        BitScanForward(&scale, node->gtElemSize);

        // dest = base + index * scale
        genScaledAdd(emitActualTypeSize(node), node->GetRegNum(), base->GetRegNum(), index->GetRegNum(), scale);
    }
    else // we have to load the element size and use a MADD (multiply-add) instruction
    {
        // tmpReg = element size
        CodeGen::genSetRegToIcon(tmpReg, (ssize_t)node->gtElemSize, TYP_INT);

        // dest = index * tmpReg + base
        GetEmitter()->emitIns_R_R_R_R(INS_MULADD, emitActualTypeSize(node), node->GetRegNum(), index->GetRegNum(),
                                      tmpReg, base->GetRegNum());
    }

    // dest = dest + elemOffs
    GetEmitter()->emitIns_R_R_I(INS_add, emitActualTypeSize(node), node->GetRegNum(), node->GetRegNum(),
                                node->gtElemOffset);

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

#ifdef FEATURE_SIMD
    // Handling of Vector3 type values loaded through indirection.
    if (tree->TypeGet() == TYP_SIMD12)
    {
        genLoadIndTypeSIMD12(tree);
        return;
    }
#endif // FEATURE_SIMD

    var_types   type      = tree->TypeGet();
    instruction ins       = ins_Load(type);
    regNumber   targetReg = tree->GetRegNum();

    genConsumeAddress(tree->Addr());

    bool emitBarrier = false;

    if ((tree->gtFlags & GTF_IND_VOLATILE) != 0)
    {
#ifdef TARGET_ARM64
        bool addrIsInReg   = tree->Addr()->isUsedFromReg();
        bool addrIsAligned = ((tree->gtFlags & GTF_IND_UNALIGNED) == 0);

        if ((ins == INS_ldrb) && addrIsInReg)
        {
            ins = INS_ldarb;
        }
        else if ((ins == INS_ldrh) && addrIsInReg && addrIsAligned)
        {
            ins = INS_ldarh;
        }
        else if ((ins == INS_ldr) && addrIsInReg && addrIsAligned && genIsValidIntReg(targetReg))
        {
            ins = INS_ldar;
        }
        else
#endif // TARGET_ARM64
        {
            emitBarrier = true;
        }
    }

    GetEmitter()->emitInsLoadStoreOp(ins, emitActualTypeSize(type), targetReg, tree);

    if (emitBarrier)
    {
        // when INS_ldar* could not be used for a volatile load,
        // we use an ordinary load followed by a load barrier.
        instGen_MemoryBarrier(BARRIER_LOAD_ONLY);
    }

    genProduceReg(tree);
}

//----------------------------------------------------------------------------------
// genCodeForCpBlkHelper - Generate code for a CpBlk node by the means of the VM memcpy helper call
//
// Arguments:
//    cpBlkNode - the GT_STORE_[BLK|OBJ|DYN_BLK]
//
// Preconditions:
//   The register assignments have been set appropriately.
//   This is validated by genConsumeBlockOp().
//
void CodeGen::genCodeForCpBlkHelper(GenTreeBlk* cpBlkNode)
{
    // Destination address goes in arg0, source address goes in arg1, and size goes in arg2.
    // genConsumeBlockOp takes care of this for us.
    genConsumeBlockOp(cpBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile CpBlk operation
        instGen_MemoryBarrier();
    }

    genEmitHelperCall(CORINFO_HELP_MEMCPY, 0, EA_UNKNOWN);

    if (cpBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a load barrier after a volatile CpBlk operation
        instGen_MemoryBarrier(BARRIER_LOAD_ONLY);
    }
}

//----------------------------------------------------------------------------------
// genCodeForInitBlkUnroll: Generate unrolled block initialization code.
//
// Arguments:
//    node - the GT_STORE_BLK node to generate code for
//
void CodeGen::genCodeForInitBlkUnroll(GenTreeBlk* node)
{
    assert(node->OperIs(GT_STORE_BLK));

    unsigned  dstLclNum      = BAD_VAR_NUM;
    regNumber dstAddrBaseReg = REG_NA;
    int       dstOffset      = 0;
    GenTree*  dstAddr        = node->Addr();

    if (!dstAddr->isContained())
    {
        dstAddrBaseReg = genConsumeReg(dstAddr);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        assert(!dstAddr->AsAddrMode()->HasIndex());

        dstAddrBaseReg = genConsumeReg(dstAddr->AsAddrMode()->Base());
        dstOffset      = dstAddr->AsAddrMode()->Offset();
    }
    else
    {
        assert(dstAddr->OperIsLocalAddr());
        dstLclNum = dstAddr->AsLclVarCommon()->GetLclNum();
        dstOffset = dstAddr->AsLclVarCommon()->GetLclOffs();
    }

    regNumber srcReg;
    GenTree*  src = node->Data();

    if (src->OperIs(GT_INIT_VAL))
    {
        assert(src->isContained());
        src = src->gtGetOp1();
    }

    if (!src->isContained())
    {
        srcReg = genConsumeReg(src);
    }
    else
    {
#ifdef TARGET_ARM64
        assert(src->IsIntegralConst(0));
        srcReg = REG_ZR;
#else
        unreached();
#endif
    }

    if (node->IsVolatile())
    {
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();
    unsigned size = node->GetLayout()->GetSize();

    assert(size <= INT32_MAX);
    assert(dstOffset < INT32_MAX - static_cast<int>(size));

#ifdef TARGET_ARM64
    for (unsigned regSize = 2 * REGSIZE_BYTES; size >= regSize; size -= regSize, dstOffset += regSize)
    {
        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_S_R_R(INS_stp, EA_8BYTE, EA_8BYTE, srcReg, srcReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_R_R_R_I(INS_stp, EA_8BYTE, srcReg, srcReg, dstAddrBaseReg, dstOffset);
        }
    }
#endif

    for (unsigned regSize = REGSIZE_BYTES; size > 0; size -= regSize, dstOffset += regSize)
    {
        while (regSize > size)
        {
            regSize /= 2;
        }

        instruction storeIns;
        emitAttr    attr;

        switch (regSize)
        {
            case 1:
                storeIns = INS_strb;
                attr     = EA_4BYTE;
                break;
            case 2:
                storeIns = INS_strh;
                attr     = EA_4BYTE;
                break;
            case 4:
#ifdef TARGET_ARM64
            case 8:
#endif
                storeIns = INS_str;
                attr     = EA_ATTR(regSize);
                break;
            default:
                unreached();
        }

        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(storeIns, attr, srcReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_R_R_I(storeIns, attr, srcReg, dstAddrBaseReg, dstOffset);
        }
    }
}

//----------------------------------------------------------------------------------
// genCodeForCpBlkUnroll: Generate unrolled block copy code.
//
// Arguments:
//    node - the GT_STORE_BLK node to generate code for
//
void CodeGen::genCodeForCpBlkUnroll(GenTreeBlk* node)
{
    assert(node->OperIs(GT_STORE_BLK));

    unsigned  dstLclNum      = BAD_VAR_NUM;
    regNumber dstAddrBaseReg = REG_NA;
    int       dstOffset      = 0;
    GenTree*  dstAddr        = node->Addr();

    if (!dstAddr->isContained())
    {
        dstAddrBaseReg = genConsumeReg(dstAddr);
    }
    else if (dstAddr->OperIsAddrMode())
    {
        assert(!dstAddr->AsAddrMode()->HasIndex());

        dstAddrBaseReg = genConsumeReg(dstAddr->AsAddrMode()->Base());
        dstOffset      = dstAddr->AsAddrMode()->Offset();
    }
    else
    {
        // TODO-ARM-CQ: If the local frame offset is too large to be encoded, the emitter automatically
        // loads the offset into a reserved register (see CodeGen::rsGetRsvdReg()). If we generate
        // multiple store instructions we'll also generate multiple offset loading instructions.
        // We could try to detect such cases, compute the base destination address in this reserved
        // and use it in all store instructions we generate. This would effectively undo the effect
        // of local address containment done by lowering.
        //
        // The same issue also occurs in source address case below and in genCodeForInitBlkUnroll.

        assert(dstAddr->OperIsLocalAddr());
        dstLclNum = dstAddr->AsLclVarCommon()->GetLclNum();
        dstOffset = dstAddr->AsLclVarCommon()->GetLclOffs();
    }

    unsigned  srcLclNum      = BAD_VAR_NUM;
    regNumber srcAddrBaseReg = REG_NA;
    int       srcOffset      = 0;
    GenTree*  src            = node->Data();

    assert(src->isContained());

    if (src->OperIs(GT_LCL_VAR, GT_LCL_FLD))
    {
        srcLclNum = src->AsLclVarCommon()->GetLclNum();
        srcOffset = src->AsLclVarCommon()->GetLclOffs();
    }
    else
    {
        assert(src->OperIs(GT_IND));
        GenTree* srcAddr = src->AsIndir()->Addr();

        if (!srcAddr->isContained())
        {
            srcAddrBaseReg = genConsumeReg(srcAddr);
        }
        else if (srcAddr->OperIsAddrMode())
        {
            srcAddrBaseReg = genConsumeReg(srcAddr->AsAddrMode()->Base());
            srcOffset      = srcAddr->AsAddrMode()->Offset();
        }
        else
        {
            assert(srcAddr->OperIsLocalAddr());
            srcLclNum = srcAddr->AsLclVarCommon()->GetLclNum();
            srcOffset = srcAddr->AsLclVarCommon()->GetLclOffs();
        }
    }

    if (node->IsVolatile())
    {
        // issue a full memory barrier before a volatile CpBlk operation
        instGen_MemoryBarrier();
    }

    emitter* emit = GetEmitter();
    unsigned size = node->GetLayout()->GetSize();

    assert(size <= INT32_MAX);
    assert(srcOffset < INT32_MAX - static_cast<int>(size));
    assert(dstOffset < INT32_MAX - static_cast<int>(size));

    regNumber tempReg = node->ExtractTempReg(RBM_ALLINT);

#ifdef TARGET_ARM64
    if (size >= 2 * REGSIZE_BYTES)
    {
        regNumber tempReg2 = node->ExtractTempReg(RBM_ALLINT);

        for (unsigned regSize = 2 * REGSIZE_BYTES; size >= regSize;
             size -= regSize, srcOffset += regSize, dstOffset += regSize)
        {
            if (srcLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_R_R_S_S(INS_ldp, EA_8BYTE, EA_8BYTE, tempReg, tempReg2, srcLclNum, srcOffset);
            }
            else
            {
                emit->emitIns_R_R_R_I(INS_ldp, EA_8BYTE, tempReg, tempReg2, srcAddrBaseReg, srcOffset);
            }

            if (dstLclNum != BAD_VAR_NUM)
            {
                emit->emitIns_S_S_R_R(INS_stp, EA_8BYTE, EA_8BYTE, tempReg, tempReg2, dstLclNum, dstOffset);
            }
            else
            {
                emit->emitIns_R_R_R_I(INS_stp, EA_8BYTE, tempReg, tempReg2, dstAddrBaseReg, dstOffset);
            }
        }
    }
#endif

    for (unsigned regSize = REGSIZE_BYTES; size > 0; size -= regSize, srcOffset += regSize, dstOffset += regSize)
    {
        while (regSize > size)
        {
            regSize /= 2;
        }

        instruction loadIns;
        instruction storeIns;
        emitAttr    attr;

        switch (regSize)
        {
            case 1:
                loadIns  = INS_ldrb;
                storeIns = INS_strb;
                attr     = EA_4BYTE;
                break;
            case 2:
                loadIns  = INS_ldrh;
                storeIns = INS_strh;
                attr     = EA_4BYTE;
                break;
            case 4:
#ifdef TARGET_ARM64
            case 8:
#endif
                loadIns  = INS_ldr;
                storeIns = INS_str;
                attr     = EA_ATTR(regSize);
                break;
            default:
                unreached();
        }

        if (srcLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_R_S(loadIns, attr, tempReg, srcLclNum, srcOffset);
        }
        else
        {
            emit->emitIns_R_R_I(loadIns, attr, tempReg, srcAddrBaseReg, srcOffset);
        }

        if (dstLclNum != BAD_VAR_NUM)
        {
            emit->emitIns_S_R(storeIns, attr, tempReg, dstLclNum, dstOffset);
        }
        else
        {
            emit->emitIns_R_R_I(storeIns, attr, tempReg, dstAddrBaseReg, dstOffset);
        }
    }

    if (node->IsVolatile())
    {
        // issue a load barrier after a volatile CpBlk operation
        instGen_MemoryBarrier(BARRIER_LOAD_ONLY);
    }
}

//------------------------------------------------------------------------
// genCodeForInitBlkHelper - Generate code for an InitBlk node by the means of the VM memcpy helper call
//
// Arguments:
//    initBlkNode - the GT_STORE_[BLK|OBJ|DYN_BLK]
//
// Preconditions:
//   The register assignments have been set appropriately.
//   This is validated by genConsumeBlockOp().
//
void CodeGen::genCodeForInitBlkHelper(GenTreeBlk* initBlkNode)
{
    // Size goes in arg2, source address goes in arg1, and size goes in arg2.
    // genConsumeBlockOp takes care of this for us.
    genConsumeBlockOp(initBlkNode, REG_ARG_0, REG_ARG_1, REG_ARG_2);

    if (initBlkNode->gtFlags & GTF_BLK_VOLATILE)
    {
        // issue a full memory barrier before a volatile initBlock Operation
        instGen_MemoryBarrier();
    }

    genEmitHelperCall(CORINFO_HELP_MEMSET, 0, EA_UNKNOWN);
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
    for (GenTreeCall::Use& use : call->LateArgs())
    {
        GenTree* argNode = use.GetNode();

        fgArgTabEntry* curArgTabEntry = compiler->gtArgEntryByNode(call, argNode);
        assert(curArgTabEntry);

        // GT_RELOAD/GT_COPY use the child node
        argNode = argNode->gtSkipReloadOrCopy();

        if (curArgTabEntry->GetRegNum() == REG_STK)
            continue;

        // Deal with multi register passed struct args.
        if (argNode->OperGet() == GT_FIELD_LIST)
        {
            regNumber argReg = curArgTabEntry->GetRegNum();
            for (GenTreeFieldList::Use& use : argNode->AsFieldList()->Uses())
            {
                GenTree* putArgRegNode = use.GetNode();
                assert(putArgRegNode->gtOper == GT_PUTARG_REG);

                genConsumeReg(putArgRegNode);
                inst_Mov_Extend(putArgRegNode->TypeGet(), /* srcInReg */ true, argReg, putArgRegNode->GetRegNum(),
                                /* canSkip */ true, emitActualTypeSize(TYP_I_IMPL));

                argReg = genRegArgNext(argReg);

#if defined(TARGET_ARM)
                // A double register is modelled as an even-numbered single one
                if (putArgRegNode->TypeGet() == TYP_DOUBLE)
                {
                    argReg = genRegArgNext(argReg);
                }
#endif // TARGET_ARM
            }
        }
#if FEATURE_ARG_SPLIT
        else if (curArgTabEntry->IsSplit())
        {
            assert(curArgTabEntry->numRegs >= 1);
            genConsumeArgSplitStruct(argNode->AsPutArgSplit());
            for (unsigned idx = 0; idx < curArgTabEntry->numRegs; idx++)
            {
                regNumber argReg   = (regNumber)((unsigned)curArgTabEntry->GetRegNum() + idx);
                regNumber allocReg = argNode->AsPutArgSplit()->GetRegNumByIdx(idx);
                inst_Mov_Extend(argNode->TypeGet(), /* srcInReg */ true, argReg, allocReg, /* canSkip */ true,
                                emitActualTypeSize(TYP_I_IMPL));
            }
        }
#endif // FEATURE_ARG_SPLIT
        else
        {
            regNumber argReg = curArgTabEntry->GetRegNum();
            genConsumeReg(argNode);
            inst_Mov_Extend(argNode->TypeGet(), /* srcInReg */ true, argReg, argNode->GetRegNum(), /* canSkip */ true,
                            emitActualTypeSize(TYP_I_IMPL));
        }
    }

    // Insert a null check on "this" pointer if asked.
    if (call->NeedsNullCheck())
    {
        const regNumber regThis = genGetThisArgReg(call);

#if defined(TARGET_ARM)
        const regNumber tmpReg = call->ExtractTempReg();
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, tmpReg, regThis, 0);
#elif defined(TARGET_ARM64)
        GetEmitter()->emitIns_R_R_I(INS_ldr, EA_4BYTE, REG_ZR, regThis, 0);
#endif // TARGET*
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

        if (target != nullptr)
        {
            // Indirect fast tail calls materialize call target either in gtControlExpr or in gtCallAddr.
            genConsumeReg(target);

            // Use IP0 on ARM64 and R12 on ARM32 as the call target register.
            inst_Mov(TYP_I_IMPL, REG_FASTTAILCALL_TARGET, target->GetRegNum(), /* canSkip */ true);
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
    const ReturnTypeDesc* pRetTypeDesc  = call->GetReturnTypeDesc();
    emitAttr              retSize       = EA_PTRSIZE;
    emitAttr              secondRetSize = EA_UNKNOWN;

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
        assert(genIsValidIntReg(target->GetRegNum()));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), ilOffset, target->GetRegNum());
    }
    else if (call->IsR2ROrVirtualStubRelativeIndir())
    {
        // Generate a direct call to a non-virtual user defined or helper method
        assert(callType == CT_HELPER || callType == CT_USER_FUNC);
#ifdef FEATURE_READYTORUN_COMPILER
        assert(((call->IsR2RRelativeIndir()) && (call->gtEntryPoint.accessType == IAT_PVALUE)) ||
               ((call->IsVirtualStubRelativeIndir()) && (call->gtEntryPoint.accessType == IAT_VALUE)));
#endif // FEATURE_READYTORUN_COMPILER
        assert(call->gtControlExpr == nullptr);
        assert(!call->IsTailCall());

        regNumber tmpReg = call->GetSingleTempReg();
        GetEmitter()->emitIns_R_R(ins_Load(TYP_I_IMPL), emitActualTypeSize(TYP_I_IMPL), tmpReg, REG_R2R_INDIRECT_PARAM);

        // We have now generated code for gtControlExpr evaluating it into `tmpReg`.
        // We just need to emit "call tmpReg" in this case.
        //
        assert(genIsValidIntReg(tmpReg));

        genEmitCall(emitter::EC_INDIR_R, methHnd,
                    INDEBUG_LDISASM_COMMA(sigInfo) nullptr, // addr
                    retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), ilOffset, tmpReg);
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
#ifdef TARGET_ARM
        if (!arm_Valid_Imm_For_BL((ssize_t)addr))
        {
            regNumber tmpReg = call->GetSingleTempReg();
            instGen_Set_Reg_To_Imm(EA_HANDLE_CNS_RELOC, tmpReg, (ssize_t)addr);
            genEmitCall(emitter::EC_INDIR_R, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) NULL, retSize, ilOffset, tmpReg);
        }
        else
#endif // TARGET_ARM
        {
            genEmitCall(emitter::EC_FUNC_TOKEN, methHnd, INDEBUG_LDISASM_COMMA(sigInfo) addr,
                        retSize MULTIREG_HAS_SECOND_GC_RET_ONLY_ARG(secondRetSize), ilOffset);
        }

#if 0 && defined(TARGET_ARM64)
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
        genDefineInlineTempLabel(genPendingCallLabel);
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
                inst_Mov(regType, allocatedReg, returnReg, /* canSkip */ true);
            }
        }
        else
        {
#ifdef TARGET_ARM
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
#endif // TARGET_ARM
                if (varTypeUsesFloatArgReg(returnType))
            {
                returnReg = REG_FLOATRET;
            }
            else
            {
                returnReg = REG_INTRET;
            }

            if (call->GetRegNum() != returnReg)
            {
#ifdef TARGET_ARM
                if (compiler->opts.compUseSoftFP && returnType == TYP_DOUBLE)
                {
                    inst_RV_RV_RV(INS_vmov_i2d, call->GetRegNum(), returnReg, genRegArgNext(returnReg), EA_8BYTE);
                }
                else if (compiler->opts.compUseSoftFP && returnType == TYP_FLOAT)
                {
                    inst_Mov(returnType, call->GetRegNum(), returnReg, /* canSkip */ false);
                }
                else
#endif
                {
                    inst_Mov(returnType, call->GetRegNum(), returnReg, /* canSkip */ false);
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

        if (varDsc->lvIsRegArg && (varDsc->GetRegNum() != REG_STK))
        {
            // Skip reg args which are already in its right register for jmp call.
            // If not, we will spill such args to their stack locations.
            //
            // If we need to generate a tail call profiler hook, then spill all
            // arg regs to free them up for the callback.
            if (!compiler->compIsProfilerHookNeeded() && (varDsc->GetRegNum() == varDsc->GetArgReg()))
                continue;
        }
        else if (varDsc->GetRegNum() == REG_STK)
        {
            // Skip args which are currently living in stack.
            continue;
        }

        // If we came here it means either a reg argument not in the right register or
        // a stack argument currently living in a register.  In either case the following
        // assert should hold.
        assert(varDsc->GetRegNum() != REG_STK);
        assert(varDsc->TypeGet() != TYP_STRUCT);
        var_types storeType = genActualType(varDsc->TypeGet());
        emitAttr  storeSize = emitActualTypeSize(storeType);

#ifdef TARGET_ARM
        if (varDsc->TypeGet() == TYP_LONG)
        {
            // long - at least the low half must be enregistered
            GetEmitter()->emitIns_S_R(INS_str, EA_4BYTE, varDsc->GetRegNum(), varNum, 0);

            // Is the upper half also enregistered?
            if (varDsc->GetOtherReg() != REG_STK)
            {
                GetEmitter()->emitIns_S_R(INS_str, EA_4BYTE, varDsc->GetOtherReg(), varNum, sizeof(int));
            }
        }
        else
#endif // TARGET_ARM
        {
            GetEmitter()->emitIns_S_R(ins_Store(storeType), storeSize, varDsc->GetRegNum(), varNum, 0);
        }
        // Update lvRegNum life and GC info to indicate lvRegNum is dead and varDsc stack slot is going live.
        // Note that we cannot modify varDsc->GetRegNum() here because another basic block may not be expecting it.
        // Therefore manually update life of varDsc->GetRegNum().
        regMaskTP tempMask = genRegMask(varDsc->GetRegNum());
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
        regNumber argReg     = varDsc->GetArgReg(); // incoming arg register
        regNumber argRegNext = REG_NA;

#ifdef TARGET_ARM64
        if (varDsc->GetRegNum() != argReg)
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
                    GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, fieldReg, varNum, ofs);
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
                    loadType = varDsc->GetLayout()->GetGCPtrType(0);
                }
                else
                {
                    loadType = compiler->mangleVarArgsType(genActualType(varDsc->TypeGet()));
                }
                emitAttr loadSize = emitActualTypeSize(loadType);
                GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argReg, varNum, 0);

                // Update argReg life and GC Info to indicate varDsc stack slot is dead and argReg is going live.
                // Note that we cannot modify varDsc->GetRegNum() here because another basic block may not be
                // expecting it. Therefore manually update life of argReg.  Note that GT_JMP marks the end of
                // the basic block and after which reg life and gc info will be recomputed for the new block
                // in genCodeForBBList().
                regSet.AddMaskVars(genRegMask(argReg));
                gcInfo.gcMarkRegPtrVal(argReg, loadType);

                if (compiler->lvaIsMultiregStruct(varDsc, compiler->info.compIsVarArgs))
                {
                    // Restore the second register.
                    argRegNext = genRegArgNext(argReg);

                    loadType = varDsc->GetLayout()->GetGCPtrType(1);
                    loadSize = emitActualTypeSize(loadType);
                    GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, argRegNext, varNum, TARGET_POINTER_SIZE);

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

#else  // !TARGET_ARM64

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

            if (varDsc->GetRegNum() != argReg)
            {
                GetEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, argReg, varNum, 0);
                GetEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, argRegNext, varNum, REGSIZE_BYTES);
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
                if (varDsc->GetRegNum() != argReg)
                {
                    GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, fieldReg, varNum, ofs);
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
                loadType     = varDsc->GetLayout()->GetGCPtrType(idx);

                if (varDsc->GetRegNum() != argReg)
                {
                    emitAttr loadSize = emitActualTypeSize(loadType);

                    GetEmitter()->emitIns_R_S(ins_Load(loadType), loadSize, slotReg, varNum, ofs);
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

            if (varDsc->GetRegNum() != argReg)
            {
                GetEmitter()->emitIns_R_S(ins_Load(loadType), emitTypeSize(loadType), argReg, varNum, 0);
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
#endif // !TARGET_ARM64
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
            GetEmitter()->emitDisableGC();
            for (int argNum = 0, argOffset = 0; argNum < MAX_REG_ARG; ++argNum)
            {
                regNumber argReg     = intArgRegs[argNum];
                regMaskTP argRegMask = genRegMask(argReg);

                if ((remainingIntArgMask & argRegMask) != 0)
                {
                    remainingIntArgMask &= ~argRegMask;
                    GetEmitter()->emitIns_R_S(INS_ldr, EA_PTRSIZE, argReg, firstArgVarNum, argOffset);
                }

                argOffset += REGSIZE_BYTES;
            }
            GetEmitter()->emitEnableGC();
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
            GetEmitter()->emitIns_R_I(INS_cmp, EA_ATTR(desc.CheckSrcSize()), reg, 0);
            genJumpToThrowHlpBlk(EJ_lt, SCK_OVERFLOW);
            break;

#ifdef TARGET_64BIT
        case GenIntCastDesc::CHECK_UINT_RANGE:
            // We need to check if the value is not greater than 0xFFFFFFFF but this value
            // cannot be encoded in the immediate operand of CMP. Use TST instead to check
            // if the upper 32 bits are zero.
            GetEmitter()->emitIns_R_I(INS_tst, EA_8BYTE, reg, 0xFFFFFFFF00000000LL);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            break;

        case GenIntCastDesc::CHECK_POSITIVE_INT_RANGE:
            // We need to check if the value is not greater than 0x7FFFFFFF but this value
            // cannot be encoded in the immediate operand of CMP. Use TST instead to check
            // if the upper 33 bits are zero.
            GetEmitter()->emitIns_R_I(INS_tst, EA_8BYTE, reg, 0xFFFFFFFF80000000LL);
            genJumpToThrowHlpBlk(EJ_ne, SCK_OVERFLOW);
            break;

        case GenIntCastDesc::CHECK_INT_RANGE:
        {
            const regNumber tempReg = cast->GetSingleTempReg();
            assert(tempReg != reg);
            instGen_Set_Reg_To_Imm(EA_8BYTE, tempReg, INT32_MAX);
            GetEmitter()->emitIns_R_R(INS_cmp, EA_8BYTE, reg, tempReg);
            genJumpToThrowHlpBlk(EJ_gt, SCK_OVERFLOW);
            instGen_Set_Reg_To_Imm(EA_8BYTE, tempReg, INT32_MIN);
            GetEmitter()->emitIns_R_R(INS_cmp, EA_8BYTE, reg, tempReg);
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
                GetEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMaxValue + 1);
                genJumpToThrowHlpBlk((castMinValue == 0) ? EJ_hs : EJ_ge, SCK_OVERFLOW);
            }
            else
            {
                GetEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMaxValue);
                genJumpToThrowHlpBlk((castMinValue == 0) ? EJ_hi : EJ_gt, SCK_OVERFLOW);
            }

            if (castMinValue != 0)
            {
                GetEmitter()->emitIns_R_I(INS_cmp, EA_SIZE(desc.CheckSrcSize()), reg, castMinValue);
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

    const regNumber srcReg = cast->gtGetOp1()->GetRegNum();
    const regNumber dstReg = cast->GetRegNum();

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
#ifdef TARGET_64BIT
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

        GetEmitter()->emitIns_Mov(ins, EA_ATTR(insSize), dstReg, srcReg, /* canSkip */ false);
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

    regNumber targetReg = treeNode->GetRegNum();
    assert(genIsValidFloatReg(targetReg));

    GenTree* op1 = treeNode->AsOp()->gtOp1;
    assert(!op1->isContained());                  // Cannot be contained
    assert(genIsValidFloatReg(op1->GetRegNum())); // Must be a valid float reg.

    var_types dstType = treeNode->CastToType();
    var_types srcType = op1->TypeGet();
    assert(varTypeIsFloating(srcType) && varTypeIsFloating(dstType));

    genConsumeOperands(treeNode->AsOp());

    // treeNode must be a reg
    assert(!treeNode->isContained());

#if defined(TARGET_ARM)

    if (srcType != dstType)
    {
        instruction insVcvt = (srcType == TYP_FLOAT) ? INS_vcvt_f2d  // convert Float to Double
                                                     : INS_vcvt_d2f; // convert Double to Float

        GetEmitter()->emitIns_R_R(insVcvt, emitTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum());
    }
    else
    {
        GetEmitter()->emitIns_Mov(INS_vmov, emitTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum(),
                                  /* canSkip */ true);
    }

#elif defined(TARGET_ARM64)

    if (srcType != dstType)
    {
        insOpts cvtOption = (srcType == TYP_FLOAT) ? INS_OPTS_S_TO_D  // convert Single to Double
                                                   : INS_OPTS_D_TO_S; // convert Double to Single

        GetEmitter()->emitIns_R_R(INS_fcvt, emitActualTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum(),
                                  cvtOption);
    }
    else
    {
        // If double to double cast or float to float cast. Emit a move instruction.
        GetEmitter()->emitIns_Mov(INS_mov, emitActualTypeSize(treeNode), treeNode->GetRegNum(), op1->GetRegNum(),
                                  /* canSkip */ true);
    }

#endif // TARGET*

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

#ifdef TARGET_ARM64

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

#endif // TARGET_ARM64

    if (compiler->opts.IsReversePInvoke())
    {
        unsigned reversePInvokeFrameVarNumber = compiler->lvaReversePInvokeFrameVar;
        assert(reversePInvokeFrameVarNumber != BAD_VAR_NUM && reversePInvokeFrameVarNumber < compiler->lvaRefCount);
        LclVarDsc& reversePInvokeFrameVar = compiler->lvaTable[reversePInvokeFrameVarNumber];
        gcInfoEncoder->SetReversePInvokeFrameSlot(reversePInvokeFrameVar.GetStackOffset());
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

#ifdef TARGET_ARM64
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

    GetEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(type), dstReg, 0);

    BasicBlock* labelNext = genCreateTempLabel();
    GetEmitter()->emitIns_J(INS_b, labelNext);

    genDefineTempLabel(labelTrue);
    GetEmitter()->emitIns_R_I(INS_mov, emitActualTypeSize(type), dstReg, 1);
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

    if (blkOp->OperIs(GT_STORE_OBJ))
    {
        assert(!blkOp->gtBlkOpGcUnsafe);
        assert(blkOp->OperIsCopyBlkOp());
        assert(blkOp->AsObj()->GetLayout()->HasGCPtr());
        genCodeForCpObj(blkOp->AsObj());
        return;
    }

    bool isCopyBlk = blkOp->OperIsCopyBlkOp();

    switch (blkOp->gtBlkOpKind)
    {
        case GenTreeBlk::BlkOpKindHelper:
            assert(!blkOp->gtBlkOpGcUnsafe);
            if (isCopyBlk)
            {
                genCodeForCpBlkHelper(blkOp);
            }
            else
            {
                genCodeForInitBlkHelper(blkOp);
            }
            break;

        case GenTreeBlk::BlkOpKindUnroll:
            if (isCopyBlk)
            {
                if (blkOp->gtBlkOpGcUnsafe)
                {
                    GetEmitter()->emitDisableGC();
                }
                genCodeForCpBlkUnroll(blkOp);
                if (blkOp->gtBlkOpGcUnsafe)
                {
                    GetEmitter()->emitEnableGC();
                }
            }
            else
            {
                assert(!blkOp->gtBlkOpGcUnsafe);
                genCodeForInitBlkUnroll(blkOp);
            }
            break;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// genScaledAdd: A helper for genLeaInstruction.
//
void CodeGen::genScaledAdd(emitAttr attr, regNumber targetReg, regNumber baseReg, regNumber indexReg, int scale)
{
    emitter* emit = GetEmitter();
    if (scale == 0)
    {
        // target = base + index
        GetEmitter()->emitIns_R_R_R(INS_add, attr, targetReg, baseReg, indexReg);
    }
    else
    {
// target = base + index<<scale
#if defined(TARGET_ARM)
        emit->emitIns_R_R_R_I(INS_add, attr, targetReg, baseReg, indexReg, scale, INS_FLAGS_DONT_CARE, INS_OPTS_LSL);
#elif defined(TARGET_ARM64)
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
    emitter* emit   = GetEmitter();
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
            bool useLargeOffsetSeq = compiler->GetInterruptible() && (size == EA_BYREF);

            if (!useLargeOffsetSeq && emitter::emitIns_valid_imm_for_add(offset))
            {
                // Generate code to set tmpReg = base + index*scale
                genScaledAdd(size, tmpReg, memBase->GetRegNum(), index->GetRegNum(), scale);

                // Then compute target reg from [tmpReg + offset]
                emit->emitIns_R_R_I(INS_add, size, lea->GetRegNum(), tmpReg, offset);
            }
            else // large offset sequence
            {
                noway_assert(tmpReg != index->GetRegNum());
                noway_assert(tmpReg != memBase->GetRegNum());

                // First load/store tmpReg with the offset constant
                //      rTmp = imm
                instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

                // Then add the scaled index register
                //      rTmp = rTmp + index*scale
                genScaledAdd(EA_PTRSIZE, tmpReg, tmpReg, index->GetRegNum(), scale);

                // Then compute target reg from [base + tmpReg ]
                //      rDst = base + rTmp
                emit->emitIns_R_R_R(INS_add, size, lea->GetRegNum(), memBase->GetRegNum(), tmpReg);
            }
        }
        else
        {
            // Then compute target reg from [base + index*scale]
            genScaledAdd(size, lea->GetRegNum(), memBase->GetRegNum(), index->GetRegNum(), scale);
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
                emit->emitIns_R_R_I(INS_add, size, lea->GetRegNum(), memBase->GetRegNum(), offset);
            }
            else // offset is zero
            {
                emit->emitIns_Mov(INS_mov, size, lea->GetRegNum(), memBase->GetRegNum(), /* canSkip */ true);
            }
        }
        else
        {
            // We require a tmpReg to hold the offset
            regNumber tmpReg = lea->GetSingleTempReg();

            // First load tmpReg with the large offset constant
            instGen_Set_Reg_To_Imm(EA_PTRSIZE, tmpReg, offset);

            // Then compute target reg from [memBase + tmpReg]
            emit->emitIns_R_R_R(INS_add, size, lea->GetRegNum(), memBase->GetRegNum(), tmpReg);
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

#ifdef FEATURE_SIMD
//------------------------------------------------------------------------
// genSIMDSplitReturn: Generates code for returning a fixed-size SIMD type that lives
//                     in a single register, but is returned in multiple registers.
//
// Arguments:
//    src         - The source of the return
//    retTypeDesc - The return type descriptor.
//
void CodeGen::genSIMDSplitReturn(GenTree* src, ReturnTypeDesc* retTypeDesc)
{
    assert(varTypeIsSIMD(src));
    assert(src->isUsedFromReg());
    regNumber srcReg = src->GetRegNum();

    // Treat src register as a homogenous vector with element size equal to the reg size
    // Insert pieces in order
    unsigned regCount = retTypeDesc->GetReturnRegCount();
    for (unsigned i = 0; i < regCount; ++i)
    {
        var_types type = retTypeDesc->GetReturnRegType(i);
        regNumber reg  = retTypeDesc->GetABIReturnReg(i);
        if (varTypeIsFloating(type))
        {
            // If the register piece is to be passed in a floating point register
            // Use a vector mov element instruction
            // reg is not a vector, so it is in the first element reg[0]
            // mov reg[0], src[i]
            // This effectively moves from `src[i]` to `reg[0]`, upper bits of reg remain unchanged
            // For the case where src == reg, since we are only writing reg[0], as long as we iterate
            // so that src[0] is consumed before writing reg[0], we do not need a temporary.
            GetEmitter()->emitIns_R_R_I_I(INS_mov, emitTypeSize(type), reg, srcReg, 0, i);
        }
        else
        {
            // If the register piece is to be passed in an integer register
            // Use a vector mov to general purpose register instruction
            // mov reg, src[i]
            // This effectively moves from `src[i]` to `reg`
            GetEmitter()->emitIns_R_R_I(INS_mov, emitTypeSize(type), reg, srcReg, i);
        }
    }
}
#endif // FEATURE_SIMD

//------------------------------------------------------------------------
// genPushCalleeSavedRegisters: Push any callee-saved registers we have used.
//
// Arguments (arm64):
//    initReg        - A scratch register (that gets set to zero on some platforms).
//    pInitRegZeroed - OUT parameter. *pInitRegZeroed is set to 'true' if this method sets initReg register to zero,
//                     'false' if initReg was set to a non-zero value, and left unchanged if initReg was not touched.
//
#if defined(TARGET_ARM64)
void CodeGen::genPushCalleeSavedRegisters(regNumber initReg, bool* pInitRegZeroed)
#else
void CodeGen::genPushCalleeSavedRegisters()
#endif
{
    assert(compiler->compGeneratingProlog);

#ifdef TARGET_ARM64
    // Probe large frames now, if necessary, since genPushCalleeSavedRegisters() will allocate the frame. Note that
    // for arm64, genAllocLclFrame only probes the frame; it does not actually allocate it (it does not change SP).
    // For arm64, we are probing the frame before the callee-saved registers are saved. The 'initReg' might have
    // been calculated to be one of the callee-saved registers (say, if all the integer argument registers are
    // in use, and perhaps with other conditions being satisfied). This is ok in other cases, after the callee-saved
    // registers have been saved. So instead of letting genAllocLclFrame use initReg as a temporary register,
    // always use REG_SCRATCH. We don't care if it trashes it, so ignore the initRegZeroed output argument.
    bool ignoreInitRegZeroed = false;
    genAllocLclFrame(compiler->compLclFrameSize, REG_SCRATCH, &ignoreInitRegZeroed,
                     intRegState.rsCalleeRegArgMaskLiveIn);
#endif

    regMaskTP rsPushRegs = regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;

#if ETW_EBP_FRAMED
    if (!isFramePointerUsed() && regSet.rsRegsModified(RBM_FPBASE))
    {
        noway_assert(!"Used register RBM_FPBASE as a scratch register!");
    }
#endif

#ifdef TARGET_ARMARCH
    // On ARM we push the FP (frame-pointer) here along with all other callee saved registers
    if (isFramePointerUsed())
        rsPushRegs |= RBM_FPBASE;

    //
    // It may be possible to skip pushing/popping lr for leaf methods. However, such optimization would require
    // changes in GC suspension architecture.
    //
    // We would need to guarantee that a tight loop calling a virtual leaf method can be suspended for GC. Today, we
    // generate partially interruptible code for both the method that contains the tight loop with the call and the leaf
    // method. GC suspension depends on return address hijacking in this case. Return address hijacking depends
    // on the return address to be saved on the stack. If we skipped pushing/popping lr, the return address would never
    // be saved on the stack and the GC suspension would time out.
    //
    // So if we wanted to skip pushing pushing/popping lr for leaf frames, we would also need to do one of
    // the following to make GC suspension work in the above scenario:
    // - Make return address hijacking work even when lr is not saved on the stack.
    // - Generate fully interruptible code for loops that contains calls
    // - Generate fully interruptible code for leaf methods
    //
    // Given the limited benefit from this optimization (<10k for CoreLib NGen image), the extra complexity
    // is not worth it.
    //
    rsPushRegs |= RBM_LR; // We must save the return address (in the LR register)

    regSet.rsMaskCalleeSaved = rsPushRegs;
#endif // TARGET_ARMARCH

#ifdef DEBUG
    if (compiler->compCalleeRegsPushed != genCountBits(rsPushRegs))
    {
        printf("Error: unexpected number of callee-saved registers to push. Expected: %d. Got: %d ",
               compiler->compCalleeRegsPushed, genCountBits(rsPushRegs));
        dspRegMask(rsPushRegs);
        printf("\n");
        assert(compiler->compCalleeRegsPushed == genCountBits(rsPushRegs));
    }
#endif // DEBUG

#if defined(TARGET_ARM)
    regMaskTP maskPushRegsFloat = rsPushRegs & RBM_ALLFLOAT;
    regMaskTP maskPushRegsInt   = rsPushRegs & ~maskPushRegsFloat;

    maskPushRegsInt |= genStackAllocRegisterMask(compiler->compLclFrameSize, maskPushRegsFloat);

    assert(FitsIn<int>(maskPushRegsInt));
    inst_IV(INS_push, (int)maskPushRegsInt);
    compiler->unwindPushMaskInt(maskPushRegsInt);

    if (maskPushRegsFloat != 0)
    {
        genPushFltRegs(maskPushRegsFloat);
        compiler->unwindPushMaskFloat(maskPushRegsFloat);
    }
#elif defined(TARGET_ARM64)
    // See the document "ARM64 JIT Frame Layout" and/or "ARM64 Exception Data" for more details or requirements and
    // options. Case numbers in comments here refer to this document. See also Compiler::lvaAssignFrameOffsets()
    // for pictures of the general frame layouts, and CodeGen::genFuncletProlog() implementations (per architecture)
    // for pictures of the funclet frame layouts.
    //
    // For most frames, generate, e.g.:
    //      stp fp,  lr,  [sp,-0x80]!   // predecrement SP with full frame size, and store FP/LR pair.
    //      stp r19, r20, [sp, 0x60]    // store at positive offset from SP established above, into callee-saved area
    //                                  // at top of frame (highest addresses).
    //      stp r21, r22, [sp, 0x70]
    //
    // Notes:
    // 1. We don't always need to save FP. If FP isn't saved, then LR is saved with the other callee-saved registers
    //    at the top of the frame.
    // 2. If we save FP, then the first store is FP, LR.
    // 3. General-purpose registers are 8 bytes, floating-point registers are 16 bytes, but FP/SIMD registers only
    //    preserve their lower 8 bytes, by calling convention.
    // 4. For frames with varargs, we spill the integer register arguments to the stack, so all the arguments are
    //    consecutive, and at the top of the frame.
    // 5. We allocate the frame here; no further changes to SP are allowed (except in the body, for localloc).
    //
    // For functions with GS and localloc, we change the frame so the frame pointer and LR are saved at the top
    // of the frame, just under the varargs registers (if any). Note that the funclet frames must follow the same
    // rule, and both main frame and funclet frames (if any) must put PSPSym in the same offset from Caller-SP.
    // Since this frame type is relatively rare, we force using it via stress modes, for additional coverage.
    //
    // The frames look like the following (simplified to only include components that matter for establishing the
    // frames). See also Compiler::lvaAssignFrameOffsets().
    //
    // Frames with FP, LR saved at bottom of frame (above outgoing argument space):
    //
    //      |                       |
    //      |-----------------------|
    //      |  incoming arguments   |
    //      +=======================+ <---- Caller's SP
    //      |  Varargs regs space   | // Only for varargs functions; 64 bytes
    //      |-----------------------|
    //      |Callee saved registers | // not including FP/LR; multiple of 8 bytes
    //      |-----------------------|
    //      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
    //      |-----------------------|
    //      | locals, temps, etc.   |
    //      |-----------------------|
    //      |  possible GS cookie   |
    //      |-----------------------|
    //      |      Saved LR         | // 8 bytes
    //      |-----------------------|
    //      |      Saved FP         | // 8 bytes
    //      |-----------------------|
    //      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
    //      |-----------------------| <---- Ambient SP
    //      |       |               |
    //      ~       | Stack grows   ~
    //      |       | downward      |
    //              V
    //
    // Frames with FP, LR saved at top of frame (below saved varargs incoming arguments):
    //
    //      |                       |
    //      |-----------------------|
    //      |  incoming arguments   |
    //      +=======================+ <---- Caller's SP
    //      |  Varargs regs space   | // Only for varargs functions; 64 bytes
    //      |-----------------------|
    //      |      Saved LR         | // 8 bytes
    //      |-----------------------|
    //      |      Saved FP         | // 8 bytes
    //      |-----------------------|
    //      |Callee saved registers | // not including FP/LR; multiple of 8 bytes
    //      |-----------------------|
    //      |        PSP slot       | // 8 bytes (omitted in CoreRT ABI)
    //      |-----------------------|
    //      | locals, temps, etc.   |
    //      |-----------------------|
    //      |  possible GS cookie   |
    //      |-----------------------|
    //      |   Outgoing arg space  | // multiple of 8 bytes; if required (i.e., #outsz != 0)
    //      |-----------------------| <---- Ambient SP
    //      |       |               |
    //      ~       | Stack grows   ~
    //      |       | downward      |
    //              V
    //

    int totalFrameSize = genTotalFrameSize();

    int offset; // This will be the starting place for saving the callee-saved registers, in increasing order.

    regMaskTP maskSaveRegsFloat = rsPushRegs & RBM_ALLFLOAT;
    regMaskTP maskSaveRegsInt   = rsPushRegs & ~maskSaveRegsFloat;

#ifdef DEBUG
    if (verbose)
    {
        printf("Save float regs: ");
        dspRegMask(maskSaveRegsFloat);
        printf("\n");
        printf("Save int   regs: ");
        dspRegMask(maskSaveRegsInt);
        printf("\n");
    }
#endif // DEBUG

    // The frameType number is arbitrary, is defined below, and corresponds to one of the frame styles we
    // generate based on various sizes.
    int frameType = 0;

    // The amount to subtract from SP before starting to store the callee-saved registers. It might be folded into the
    // first save instruction as a "predecrement" amount, if possible.
    int calleeSaveSPDelta = 0;

    if (isFramePointerUsed())
    {
        // We need to save both FP and LR.

        assert((maskSaveRegsInt & RBM_FP) != 0);
        assert((maskSaveRegsInt & RBM_LR) != 0);

        // If we need to generate a GS cookie, we need to make sure the saved frame pointer and return address
        // (FP and LR) are protected from buffer overrun by the GS cookie. If FP/LR are at the lowest addresses,
        // then they are safe, since they are lower than any unsafe buffers. And the GS cookie we add will
        // protect our caller's frame. If we have a localloc, however, that is dynamically placed lower than our
        // saved FP/LR. In that case, we save FP/LR along with the rest of the callee-saved registers, above
        // the GS cookie.
        //
        // After the frame is allocated, the frame pointer is established, pointing at the saved frame pointer to
        // create a frame pointer chain.
        //
        // Do we need another frame pointer register to get good code quality in the case of having the frame pointer
        // point high in the frame, so we can take advantage of arm64's preference for positive offsets? C++ native
        // code dedicates callee-saved x19 to this, so generates:
        //      mov x19, sp
        // in the prolog, then uses x19 for local var accesses. Given that this case is so rare, we currently do
        // not do this. That means that negative offsets from FP might need to use the reserved register to form
        // the local variable offset for an addressing mode.

        if (((compiler->lvaOutgoingArgSpaceSize == 0) && (totalFrameSize <= 504)) &&
            !genSaveFpLrWithAllCalleeSavedRegisters)
        {
            // Case #1.
            //
            // Generate:
            //      stp fp,lr,[sp,#-framesz]!
            //
            // The (totalFrameSize <= 504) condition ensures that both the pre-index STP instruction
            // used in the prolog, and the post-index LDP instruction used in the epilog, can be generated.
            // Note that STP and the unwind codes can handle -512, but LDP with a positive post-index value
            // can only handle up to 504, and we want our prolog and epilog to match.
            //
            // After saving callee-saved registers, we establish the frame pointer with:
            //      mov fp,sp
            // We do this *after* saving callee-saved registers, so the prolog/epilog unwind codes mostly match.

            JITDUMP("Frame type 1. #outsz=0; #framesz=%d; LclFrameSize=%d\n", totalFrameSize,
                    compiler->compLclFrameSize);

            frameType = 1;

            assert(totalFrameSize <= STACK_PROBE_BOUNDARY_THRESHOLD_BYTES);

            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE, -totalFrameSize,
                                          INS_OPTS_PRE_INDEX);
            compiler->unwindSaveRegPairPreindexed(REG_FP, REG_LR, -totalFrameSize);

            maskSaveRegsInt &= ~(RBM_FP | RBM_LR);                        // We've already saved FP/LR
            offset = (int)compiler->compLclFrameSize + 2 * REGSIZE_BYTES; // 2 for FP/LR
        }
        else if (totalFrameSize <= 512)
        {
            // Case #2.
            //
            // The (totalFrameSize <= 512) condition ensures the callee-saved registers can all be saved using STP
            // with signed offset encoding. The maximum positive STP offset is 504, but when storing a pair of
            // 8 byte registers, the largest actual offset we use would be 512 - 8 * 2 = 496. And STR with positive
            // offset has a range 0 to 32760.
            //
            // After saving callee-saved registers, we establish the frame pointer with:
            //      add fp,sp,#outsz
            // We do this *after* saving callee-saved registers, so the prolog/epilog unwind codes mostly match.

            if (genSaveFpLrWithAllCalleeSavedRegisters)
            {
                JITDUMP("Frame type 4 (save FP/LR at top). #outsz=%d; #framesz=%d; LclFrameSize=%d\n",
                        unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, compiler->compLclFrameSize);

                frameType = 4;

                // The frame will be allocated below, when the callee-saved registers are saved. This might mean a
                // separate SUB instruction or the SP adjustment might be folded in to the first STP if there is
                // no outgoing argument space AND no local frame space, that is, if the only thing the frame does
                // is save callee-saved registers (and possibly varargs argument registers).
                calleeSaveSPDelta = totalFrameSize;

                offset = (int)compiler->compLclFrameSize;
            }
            else
            {
                JITDUMP("Frame type 2 (save FP/LR at bottom). #outsz=%d; #framesz=%d; LclFrameSize=%d\n",
                        unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, compiler->compLclFrameSize);

                frameType = 2;

                // Generate:
                //      sub sp,sp,#framesz
                //      stp fp,lr,[sp,#outsz]   // note that by necessity, #outsz <= #framesz - 16, so #outsz <= 496.

                assert(totalFrameSize - compiler->lvaOutgoingArgSpaceSize <= STACK_PROBE_BOUNDARY_THRESHOLD_BYTES);

                GetEmitter()->emitIns_R_R_I(INS_sub, EA_PTRSIZE, REG_SPBASE, REG_SPBASE, totalFrameSize);
                compiler->unwindAllocStack(totalFrameSize);

                assert(compiler->lvaOutgoingArgSpaceSize + 2 * REGSIZE_BYTES <= (unsigned)totalFrameSize);

                GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, REG_FP, REG_LR, REG_SPBASE,
                                              compiler->lvaOutgoingArgSpaceSize);
                compiler->unwindSaveRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize);

                maskSaveRegsInt &= ~(RBM_FP | RBM_LR);                        // We've already saved FP/LR
                offset = (int)compiler->compLclFrameSize + 2 * REGSIZE_BYTES; // 2 for FP/LR
            }
        }
        else
        {
            // Case 5 or 6.
            //
            // First, the callee-saved registers will be saved, and the callee-saved register code must use
            // pre-index to subtract from SP as the first instruction. It must also leave space for varargs
            // registers to be stored. For example:
            //      stp r19,r20,[sp,#-96]!
            //      stp d8,d9,[sp,#16]
            //      ... save varargs incoming integer registers ...
            // Note that all SP alterations must be 16-byte aligned. We have already calculated any alignment to be
            // lower on the stack than the callee-saved registers (see lvaAlignFrame() for how we calculate
            // alignment). So, if there is an odd number of callee-saved registers, we use (for example, with just
            // one saved register):
            //      sub sp,sp,#16
            //      str r19,[sp,#8]
            // This is one additional instruction, but it centralizes the aligned space. Otherwise, it might be
            // possible to have two 8-byte alignment padding words, one below the callee-saved registers, and one
            // above them. If that is preferable, we could implement it.
            //
            // Note that any varargs saved space will always be 16-byte aligned, since there are 8 argument
            // registers.
            //
            // Then, define #remainingFrameSz = #framesz - (callee-saved size + varargs space + possible alignment
            // padding from above). Note that #remainingFrameSz must not be zero, since we still need to save FP,SP.
            //
            // Generate:
            //      sub sp,sp,#remainingFrameSz
            // or, for large frames:
            //      mov rX, #remainingFrameSz // maybe multiple instructions
            //      sub sp,sp,rX
            //
            // followed by:
            //      stp fp,lr,[sp,#outsz]
            //      add fp,sp,#outsz
            //
            // However, we need to handle the case where #outsz is larger than the constant signed offset encoding
            // can handle. And, once again, we might need to deal with #outsz that is not aligned to 16-bytes (i.e.,
            // STACK_ALIGN). So, in the case of large #outsz we will have an additional SP adjustment, using one of
            // the following sequences:
            //
            // Define #remainingFrameSz2 = #remainingFrameSz - #outsz.
            //
            //      sub sp,sp,#remainingFrameSz2  // if #remainingFrameSz2 is 16-byte aligned
            //      stp fp,lr,[sp]
            //      mov fp,sp
            //      sub sp,sp,#outsz    // in this case, #outsz must also be 16-byte aligned
            //
            // Or:
            //
            //      sub sp,sp,roundUp(#remainingFrameSz2,16) // if #remainingFrameSz2 is not 16-byte aligned (it is
            //                                               // always guaranteed to be 8 byte aligned).
            //      stp fp,lr,[sp,#8]                        // it will always be #8 in the unaligned case
            //      add fp,sp,#8
            //      sub sp,sp,#outsz - #8
            //
            // (As usual, for a large constant "#outsz - #8", we might need multiple instructions:
            //      mov rX, #outsz - #8 // maybe multiple instructions
            //      sub sp,sp,rX
            // )
            //
            // Note that even if we align the SP alterations, that does not imply that we are creating empty alignment
            // slots. In fact, we are not; any empty alignment slots were calculated in
            // Compiler::lvaAssignFrameOffsets() and its callees.

            int calleeSaveSPDeltaUnaligned = totalFrameSize - compiler->compLclFrameSize;
            if (genSaveFpLrWithAllCalleeSavedRegisters)
            {
                JITDUMP("Frame type 5 (save FP/LR at top). #outsz=%d; #framesz=%d; LclFrameSize=%d\n",
                        unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, compiler->compLclFrameSize);

                // This case is much simpler, because we allocate space for the callee-saved register area, including
                // FP/LR. Note the SP adjustment might be SUB or be folded into the first store as a predecrement.
                // Then, we use a single SUB to establish the rest of the frame. We need to be careful about where
                // to establish the frame pointer, as there is a limit of 2040 bytes offset from SP to FP in the
                // unwind codes when FP is established.
                frameType = 5;
            }
            else
            {
                JITDUMP("Frame type 3 (save FP/LR at bottom). #outsz=%d; #framesz=%d; LclFrameSize=%d\n",
                        unsigned(compiler->lvaOutgoingArgSpaceSize), totalFrameSize, compiler->compLclFrameSize);

                frameType = 3;

                calleeSaveSPDeltaUnaligned -= 2 * REGSIZE_BYTES; // 2 for FP, LR which we'll save later.

                // We'll take care of these later, but callee-saved regs code shouldn't see them.
                maskSaveRegsInt &= ~(RBM_FP | RBM_LR);
            }

            assert(calleeSaveSPDeltaUnaligned >= 0);
            assert((calleeSaveSPDeltaUnaligned % 8) == 0); // It better at least be 8 byte aligned.
            calleeSaveSPDelta = AlignUp((UINT)calleeSaveSPDeltaUnaligned, STACK_ALIGN);

            offset = calleeSaveSPDelta - calleeSaveSPDeltaUnaligned;

            JITDUMP("    calleeSaveSPDelta=%d, offset=%d\n", calleeSaveSPDelta, offset);

            // At most one alignment slot between SP and where we store the callee-saved registers.
            assert((offset == 0) || (offset == REGSIZE_BYTES));
        }
    }
    else
    {
        // No frame pointer (no chaining).
        assert((maskSaveRegsInt & RBM_FP) == 0);
        assert((maskSaveRegsInt & RBM_LR) != 0);

        // Note that there is no pre-indexed save_lrpair unwind code variant, so we can't allocate the frame using
        // 'stp' if we only have one callee-saved register plus LR to save.

        NYI("Frame without frame pointer");
        offset = 0;
    }

    assert(frameType != 0);

    JITDUMP("    offset=%d, calleeSaveSPDelta=%d\n", offset, calleeSaveSPDelta);
    genSaveCalleeSavedRegistersHelp(maskSaveRegsInt | maskSaveRegsFloat, offset, -calleeSaveSPDelta);

    offset += genCountBits(maskSaveRegsInt | maskSaveRegsFloat) * REGSIZE_BYTES;

    // For varargs, home the incoming arg registers last. Note that there is nothing to unwind here,
    // so we just report "NOP" unwind codes. If there's no more frame setup after this, we don't
    // need to add codes at all.

    if (compiler->info.compIsVarArgs)
    {
        JITDUMP("    compIsVarArgs=true\n");

        // There are 8 general-purpose registers to home, thus 'offset' must be 16-byte aligned here.
        assert((offset % 16) == 0);
        for (regNumber reg1 = REG_ARG_FIRST; reg1 < REG_ARG_LAST; reg1 = REG_NEXT(REG_NEXT(reg1)))
        {
            regNumber reg2 = REG_NEXT(reg1);
            // stp REG, REG + 1, [SP, #offset]
            GetEmitter()->emitIns_R_R_R_I(INS_stp, EA_PTRSIZE, reg1, reg2, REG_SPBASE, offset);
            compiler->unwindNop();
            offset += 2 * REGSIZE_BYTES;
        }
    }

    // By default, we'll establish the frame pointer chain. (Note that currently frames without FP are NYI.)
    bool establishFramePointer = true;

    // If we do establish the frame pointer, what is the amount we add to SP to do so?
    unsigned offsetSpToSavedFp = 0;

    if (frameType == 1)
    {
        assert(!genSaveFpLrWithAllCalleeSavedRegisters);
        assert(offsetSpToSavedFp == 0);
    }
    else if (frameType == 2)
    {
        assert(!genSaveFpLrWithAllCalleeSavedRegisters);

        offsetSpToSavedFp = compiler->lvaOutgoingArgSpaceSize;
    }
    else if (frameType == 3)
    {
        assert(!genSaveFpLrWithAllCalleeSavedRegisters);

        int remainingFrameSz = totalFrameSize - calleeSaveSPDelta;
        assert(remainingFrameSz > 0);
        assert((remainingFrameSz % 16) == 0); // this is guaranteed to be 16-byte aligned because each component --
                                              // totalFrameSize and calleeSaveSPDelta -- is 16-byte aligned.

        if (compiler->lvaOutgoingArgSpaceSize > 504)
        {
            // We can't do "stp fp,lr,[sp,#outsz]" because #outsz is too big.
            // If compiler->lvaOutgoingArgSpaceSize is not aligned, we need to align the SP adjustment.
            assert(remainingFrameSz > (int)compiler->lvaOutgoingArgSpaceSize);
            int spAdjustment2Unaligned = remainingFrameSz - compiler->lvaOutgoingArgSpaceSize;
            int spAdjustment2          = (int)roundUp((unsigned)spAdjustment2Unaligned, STACK_ALIGN);
            int alignmentAdjustment2   = spAdjustment2 - spAdjustment2Unaligned;
            assert((alignmentAdjustment2 == 0) || (alignmentAdjustment2 == 8));

            JITDUMP("    spAdjustment2=%d\n", spAdjustment2);

            genPrologSaveRegPair(REG_FP, REG_LR, alignmentAdjustment2, -spAdjustment2, false, initReg, pInitRegZeroed);
            offset += spAdjustment2;

            // Now subtract off the #outsz (or the rest of the #outsz if it was unaligned, and the above "sub"
            // included some of it)

            int spAdjustment3 = compiler->lvaOutgoingArgSpaceSize - alignmentAdjustment2;
            assert(spAdjustment3 > 0);
            assert((spAdjustment3 % 16) == 0);

            JITDUMP("    alignmentAdjustment2=%d\n", alignmentAdjustment2);
            genEstablishFramePointer(alignmentAdjustment2, /* reportUnwindData */ true);

            // We just established the frame pointer chain; don't do it again.
            establishFramePointer = false;

            JITDUMP("    spAdjustment3=%d\n", spAdjustment3);

            // We've already established the frame pointer, so no need to report the stack pointer change to unwind
            // info.
            genStackPointerAdjustment(-spAdjustment3, initReg, pInitRegZeroed, /* reportUnwindData */ false);
            offset += spAdjustment3;
        }
        else
        {
            genPrologSaveRegPair(REG_FP, REG_LR, compiler->lvaOutgoingArgSpaceSize, -remainingFrameSz, false, initReg,
                                 pInitRegZeroed);
            offset += remainingFrameSz;

            offsetSpToSavedFp = compiler->lvaOutgoingArgSpaceSize;
        }
    }
    else if (frameType == 4)
    {
        assert(genSaveFpLrWithAllCalleeSavedRegisters);
        offsetSpToSavedFp = calleeSaveSPDelta - (compiler->info.compIsVarArgs ? MAX_REG_ARG * REGSIZE_BYTES : 0) -
                            2 * REGSIZE_BYTES; // -2 for FP, LR
    }
    else if (frameType == 5)
    {
        assert(genSaveFpLrWithAllCalleeSavedRegisters);

        offsetSpToSavedFp = calleeSaveSPDelta - (compiler->info.compIsVarArgs ? MAX_REG_ARG * REGSIZE_BYTES : 0) -
                            2 * REGSIZE_BYTES; // -2 for FP, LR
        JITDUMP("    offsetSpToSavedFp=%d\n", offsetSpToSavedFp);
        genEstablishFramePointer(offsetSpToSavedFp, /* reportUnwindData */ true);

        // We just established the frame pointer chain; don't do it again.
        establishFramePointer = false;

        int remainingFrameSz = totalFrameSize - calleeSaveSPDelta;
        assert(remainingFrameSz > 0);
        assert((remainingFrameSz % 16) == 0); // this is guaranteed to be 16-byte aligned because each component --
                                              // totalFrameSize and calleeSaveSPDelta -- is 16-byte aligned.

        JITDUMP("    remainingFrameSz=%d\n", remainingFrameSz);

        // We've already established the frame pointer, so no need to report the stack pointer change to unwind info.
        genStackPointerAdjustment(-remainingFrameSz, initReg, pInitRegZeroed, /* reportUnwindData */ false);
        offset += remainingFrameSz;
    }
    else
    {
        unreached();
    }

    if (establishFramePointer)
    {
        JITDUMP("    offsetSpToSavedFp=%d\n", offsetSpToSavedFp);
        genEstablishFramePointer(offsetSpToSavedFp, /* reportUnwindData */ true);
    }

    assert(offset == totalFrameSize);
#endif // TARGET_ARM64
}

#endif // TARGET_ARMARCH
